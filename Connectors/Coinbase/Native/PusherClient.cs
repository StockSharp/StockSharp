namespace StockSharp.Coinbase.Native
{
	using Newtonsoft.Json.Linq;

	class PusherClient : BaseLogReceiver
	{
		// to get readable name after obfuscation
		public override string Name => nameof(Coinbase) + "_" + nameof(PusherClient);

		public event Action<Heartbeat> Heartbeat;
		public event Action<Ticker> TickerChanged;
		public event Action<Trade> NewTrade;
		public event Action<OrderBook> OrderBookSnapshot;
		public event Action<OrderBookChanges> OrderBookChanged;
		public event Action<OrderLog> NewOrderLog;
		public event Action<Exception> Error;
		public event Action Connected;
		public event Action<bool> Disconnected;

		private readonly WebSocketClient _client;
		private readonly Authenticator _authenticator;

		public PusherClient(Authenticator authenticator)
		{
			_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));

			_client = new WebSocketClient(
				() =>
				{
					this.AddInfoLog(LocalizedStrings.Connected);
					Connected?.Invoke();
				},
				expected =>
				{
					if (expected)
						this.AddInfoLog(LocalizedStrings.Disconnected);
					else
						this.AddErrorLog(LocalizedStrings.ErrorConnection);

					Disconnected?.Invoke(expected);
				},
				error =>
				{
					this.AddErrorLog(error);
					Error?.Invoke(error);
				},
				OnProcess,
				(s, a) => this.AddInfoLog(s, a),
				(s, a) => this.AddErrorLog(s, a),
				(s, a) => this.AddVerboseLog(s, a),
				(s) => this.AddVerboseLog(s));
		}
		
		protected override void DisposeManaged()
		{
			_client.Dispose();
			base.DisposeManaged();
		}

		public void Connect()
		{
			this.AddInfoLog(LocalizedStrings.Connecting);
			// TODO
			_client.Connect("wss://ws-feed.pro.coinbase.com", true/*!_authenticator.CanSign*/);
		}

		public void Disconnect()
		{
			this.AddInfoLog(LocalizedStrings.Disconnecting);
			_client.Disconnect();
		}

		private void OnProcess(dynamic obj)
		{
			var type = (string)obj.type;

			switch (type)
			{
				case "error":
					Error?.Invoke(new InvalidOperationException((string)obj.message + " " + (string)obj.reason));
					break;

				case "subscriptions":
					break;

				case Channels.Heartbeat:
					Heartbeat?.Invoke(((JToken)obj).DeserializeObject<Heartbeat>());
					break;

				case Channels.Ticker:
					TickerChanged?.Invoke(((JToken)obj).DeserializeObject<Ticker>());
					break;

				case "snapshot":
					OrderBookSnapshot?.Invoke(((JToken)obj).DeserializeObject<OrderBook>());
					break;

				case "l2update":
					OrderBookChanged?.Invoke(((JToken)obj).DeserializeObject<OrderBookChanges>());
					break;

				case "received":
				case "open":
				case "done":
					NewOrderLog?.Invoke(((JToken)obj).DeserializeObject<OrderLog>());
					break;

				case "match":
					NewTrade?.Invoke(((JToken)obj).DeserializeObject<Trade>());
					break;

				case "change":
					// TODO
					break;

				case "margin_profile_update":
					// TODO
					break;

				case "activate":
				case "last_match":
					break;

				case Channels.Status:
					break;

				default:
					this.AddErrorLog(LocalizedStrings.UnknownEvent, type);
					break;
			}
		}

		private static class Channels
		{
			public const string Ticker = "ticker";
			public const string Trades = "matches";
			public const string OrderBook = "level2";
			public const string Heartbeat = "heartbeat";
			public const string OrderLog = "full";
			public const string Status = "status";
		}

		private static class Commands
		{
			public const string Subscribe = "subscribe";
			public const string UnSubscribe = "unsubscribe";
		}

		public void SubscribeStatus()
		{
			Process(Commands.Subscribe, Channels.Status, null);
		}

		public void UnSubscribeStatus()
		{
			Process(Commands.UnSubscribe, Channels.Status, null);
		}

		public void SubscribeTicker(string currency)
		{
			Process(Commands.Subscribe, Channels.Ticker, currency);
		}

		public void UnSubscribeTicker(string currency)
		{
			Process(Commands.UnSubscribe, Channels.Ticker, currency);
		}

		public void SubscribeTrades(string currency)
		{
			Process(Commands.Subscribe, Channels.Trades, currency);
		}

		public void UnSubscribeTrades(string currency)
		{
			Process(Commands.UnSubscribe, Channels.Trades, currency);
		}

		public void SubscribeOrderBook(string currency)
		{
			Process(Commands.Subscribe, Channels.OrderBook, currency);
		}

		public void UnSubscribeOrderBook(string currency)
		{
			Process(Commands.UnSubscribe, Channels.OrderBook, currency);
		}

		public void SubscribeOrderLog(string currency)
		{
			Process(Commands.Subscribe, Channels.OrderLog, currency);
		}

		public void UnSubscribeOrderLog(string currency)
		{
			Process(Commands.UnSubscribe, Channels.OrderLog, currency);
		}

		private void Process(string type, string channel, string currency)
		{
			if (type.IsEmpty())
				throw new ArgumentNullException(nameof(type));

			if (channel.IsEmpty())
				throw new ArgumentNullException(nameof(channel));

			//if (currency.IsEmpty())
			//	throw new ArgumentNullException(nameof(currency));

			_client.Send(new
			{
				type,
				product_ids = currency == null ? null : new[] { currency },
				channels = new[] { channel },
			});
		}
	}
}