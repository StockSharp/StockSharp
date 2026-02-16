namespace StockSharp.Bibox.Native;

using Ecng.IO.Compression;

class PusherClient : BaseLogReceiver
{
	// to get readable name after obfuscation
	public override string Name => nameof(Bibox) + "_" + nameof(PusherClient);

	public event Func<Ticker, CancellationToken, ValueTask> TickerChanged;
	public event Func<string, IEnumerable<Trade>, CancellationToken, ValueTask> NewTrades;
	public event Func<string, bool, OrderBook, CancellationToken, ValueTask> OrderBookChanged;
	public event Func<string, string, IEnumerable<Ohlc>, CancellationToken, ValueTask> NewCandles;
	public event Func<IDictionary<string, Balance>, CancellationToken, ValueTask> BalancesChanged;
	public event Func<Order, CancellationToken, ValueTask> OrderChanged;
	public event Func<Exception, CancellationToken, ValueTask> Error;
	public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;
	//public event Func<string, CancellationToken, ValueTask> TradesSubscribed;
	//public event Func<string, CancellationToken, ValueTask> OrderBooksSubscribed;
	public event Func<string, CancellationToken, ValueTask> PingReceived;

	private readonly WebSocketClient _client;
	private readonly Authenticator _authenticator;

	private const string _subscribe = "SUBSCRIBE";
	private const string _unsubscribe = "UNSUBSCRIBE";

	private DateTime? _nextPing;

	public PusherClient(Authenticator authenticator, WorkingTime workingTime)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));

		_client = new(
			"wss://market-wss.bibox360.com/cbu",
			(state, token) =>
			{
				if (StateChanged is { } handler)
					return handler(state, token);
				return default;
			},
			(error, token) =>
			{
				this.AddErrorLog(error);
				if (Error is { } handler)
					return handler(error, token);
				return default;
			},
			OnProcess,
			(s, a) => this.AddInfoLog(s, a),
			(s, a) => this.AddErrorLog(s, a),
			(s, a) => this.AddVerboseLog(s, a))
		{
			WorkingTime = workingTime ?? throw new ArgumentNullException(nameof(workingTime)),
		};

	protected override void DisposeManaged()
	{
		_client.Dispose();
		base.DisposeManaged();
	}

	public ValueTask Connect(CancellationToken cancellationToken)
	{
		this.AddInfoLog(LocalizedStrings.Connecting);
		return _client.ConnectAsync(cancellationToken);
	}

	public void Disconnect()
	{
		this.AddInfoLog(LocalizedStrings.Disconnecting);
		_client.Disconnect();
	}

	private async ValueTask OnProcess(WebSocketMessage msg, CancellationToken cancellationToken)
	{
		var obj = msg.AsObject();

		if (obj is JObject)
		{
			if (obj.ping != null)
			{
				if (PingReceived is { } pingHandler)
					await pingHandler((string)obj.ping, cancellationToken);
				return;
			}
			else if (obj.pong != null)
				return;

			if (obj.error != null)
				throw new InvalidOperationException(((int)obj.error.code).ToErrorText());
		}

		foreach (var item in obj)
		{
			string channel = item.channel;

			var data = item.data;

			if (item.binary == 1)
			{
				var decoded = ((string)data).Base64().UnGZip();
				this.AddVerboseLog(decoded);

				data = decoded.DeserializeObject<object>();
			}

			channel = channel.Remove("bibox_sub_spot_", true);

			if (channel.EqualsIgnoreCase("ALL_ALL_login"))
			{
				if (data.orderpending != null)
				{
					if (OrderChanged is { } handler)
						await handler(((JToken)data.orderpending).DeserializeObject<Order>(), cancellationToken);
				}
				else if (data.history != null || data.result != null)
				{

				}
				else if (data.assets != null)
				{
					var assets = data.assets;

					if (assets.normal != null)
					{
						var balance = ((JToken)assets.normal).DeserializeObject<IDictionary<string, Balance>>();
						if (BalancesChanged is { } handler)
							await handler(balance, cancellationToken);
					}

					if (assets.credit != null)
					{
						var balance = ((JToken)assets.credit).DeserializeObject<IDictionary<string, Balance>>();
						if (BalancesChanged is { } handler)
							await handler(balance, cancellationToken);
					}
					else
						this.AddErrorLog(LocalizedStrings.UnknownEvent, ((JToken)data).ToString());
				}
				else
					this.AddErrorLog(LocalizedStrings.UnknownEvent, ((JToken)data).ToString());
			}
			else if (channel.ContainsIgnoreCase("_ticker"))
			{
				var ticker = ((JToken)data).DeserializeObject<Ticker>();
				if (TickerChanged is { } handler)
					await handler(ticker, cancellationToken);
			}
			else if (channel.ContainsIgnoreCase("_deals"))
			{
				if (NewTrades is { } handler)
					await handler(channel.Remove("_deals", true), ((JToken)data).DeserializeObject<IEnumerable<Trade>>(), cancellationToken);
			}
			else if (channel.ContainsIgnoreCase("_depth"))
			{
				var book = ((JToken)data).DeserializeObject<OrderBook>();
				if (OrderBookChanged is { } handler)
					await handler(channel.Remove("_depth", true), (int?)data.data_type == 0, book, cancellationToken);
			}
			else if (channel.ContainsIgnoreCase("_kline"))
			{
				var parts = channel.Remove("_kline", true).SplitBySep("_");

				var ticker = parts[0] + "_" + parts[1];
				var tfName = parts[2];

				if (NewCandles is { } handler)
					await handler(ticker, tfName, ((JToken)data).DeserializeObject<IEnumerable<Ohlc>>(), cancellationToken);
			}
			else if (channel.EqualsIgnoreCase(_subscribe))
			{
				// TODO
			}
			else if (channel.EqualsIgnoreCase(_unsubscribe))
			{
				// TODO
			}
			else
				this.AddErrorLog(LocalizedStrings.UnknownEvent, channel);
		}
	}

	private static class Channels
	{
		public const string AccountInfo = "bibox_sub_spot_ALL_ALL_login";
		public const string Ticker = "{0}.ticker";
		public const string Deals = "{0}.trades";
		public const string OrderBook = "{0}.order_book.{1}";
		public const string Candles = "{0}.candles.{1}";
	}

	public ValueTask SubscribeAccount(long transId, CancellationToken cancellationToken)
	{
		return ProcessAuth(transId, _subscribe, Channels.AccountInfo, cancellationToken);
	}

	public ValueTask UnSubscribeAccount(long originTransId, CancellationToken cancellationToken)
	{
		return ProcessAuth(-originTransId, _unsubscribe, Channels.AccountInfo, cancellationToken);
	}

	private ValueTask ProcessAuth(long subId, string @event, string channel, CancellationToken cancellationToken)
	{
		if (@event.IsEmpty())
			throw new ArgumentNullException(nameof(@event));

		if (channel.IsEmpty())
			throw new ArgumentNullException(nameof(channel));

		var msg = JsonConvert.SerializeObject(new
		{
			apikey = _authenticator.Key.UnSecure(),
			channel,
			@event,
		});

		return _client.SendAsync(new
		{
			@event,
			channel,
			apikey = _authenticator.Key.UnSecure(),
			sign = _authenticator.MakeSign(msg),
		}, cancellationToken, subId);
	}

	public ValueTask SubscribeTicker(long transId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, transId, _subscribe, Channels.Ticker.Put(symbol), cancellationToken);
	}

	public ValueTask UnSubscribeTicker(long transId, long originTransId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, -originTransId, _unsubscribe, Channels.Ticker.Put(symbol), cancellationToken);
	}

	public ValueTask SubscribeTrades(long transId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, transId, _subscribe, Channels.Deals.Put(symbol), cancellationToken);
	}

	public ValueTask UnSubscribeTrades(long transId, long originTransId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, -originTransId, _unsubscribe, Channels.Deals.Put(symbol), cancellationToken);
	}

	public ValueTask SubscribeOrderBook(long transId, string symbol, int depth, CancellationToken cancellationToken)
	{
		return Process(transId, transId, _subscribe, Channels.OrderBook.Put(symbol, depth), cancellationToken);
	}

	public ValueTask UnSubscribeOrderBook(long transId, long originTransId, string symbol, int depth, CancellationToken cancellationToken)
	{
		return Process(transId, -originTransId, _unsubscribe, Channels.OrderBook.Put(symbol, depth), cancellationToken);
	}

	public ValueTask SubscribeCandles(long transId, string symbol, string timeFrame, CancellationToken cancellationToken)
	{
		return Process(transId, transId, _subscribe, Channels.Candles.Put(symbol, timeFrame), cancellationToken);
	}

	public ValueTask UnSubscribeCandles(long transId, long originTransId, string symbol, string timeFrame, CancellationToken cancellationToken)
	{
		return Process(transId, -originTransId, _unsubscribe, Channels.Candles.Put(symbol, timeFrame), cancellationToken);
	}

	public ValueTask ProcessPing(CancellationToken cancellationToken)
	{
		var now = DateTime.UtcNow;

		if (_nextPing != null && now < _nextPing.Value)
			return default;

		_nextPing = now.AddSeconds(5);

		return _client.SendAsync(((long)now.ToUnix(false)).ToString(), cancellationToken);
	}

	private ValueTask Process(long id, long subId, string method, object @params, CancellationToken cancellationToken)
	{
		if (method.IsEmpty())
			throw new ArgumentNullException(nameof(method));

		if (@params is null)
			throw new ArgumentNullException(nameof(@params));

		return _client.SendAsync(new
		{
			id,
			method,
			@params,
		}, cancellationToken, subId);
	}
}
