namespace StockSharp.Coinbase.Native;

using Newtonsoft.Json.Linq;

class SocketClient : BaseLogReceiver
{
	// to get readable name after obfuscation
	public override string Name => nameof(Coinbase) + "_" + nameof(SocketClient);

	public event Action<Heartbeat> HeartbeatReceived;
	public event Action<Ohlc> CandleReceived;
	public event Action<Ticker> TickerReceived;
	public event Action<Trade> TradeReceived;
	public event Action<string, string, IEnumerable<OrderBookChange>> OrderBookReceived;
	public event Action<Order> OrderReceived;
	public event Action<Exception> Error;
	public event Action<ConnectionStates> StateChanged;

	private readonly WebSocketClient _client;
	private readonly Authenticator _authenticator;

	public SocketClient(Authenticator authenticator, int reconnectAttempts)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));

		_client = new(
			"wss://advanced-trade-ws.coinbase.com",
			state => StateChanged?.Invoke(state),
			error =>
			{
				this.AddErrorLog(error);
				Error?.Invoke(error);
			},
			OnProcess,
			(s, a) => this.AddInfoLog(s, a),
			(s, a) => this.AddErrorLog(s, a),
			(s, a) => this.AddVerboseLog(s, a))
		{
			ReconnectAttempts = reconnectAttempts
		};
	}
	
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

	private ValueTask OnProcess(WebSocketMessage msg, CancellationToken cancellationToken)
	{
		var obj = msg.AsObject();
		var channel = (string)obj.channel;
		var arr = (JArray)obj.events;

		switch (channel)
		{
			case "error":
				Error?.Invoke(new InvalidOperationException((string)obj.message + " " + (string)obj.reason));
				break;

			case Channels.Heartbeat:
			{
				foreach (var item in arr)
					HeartbeatReceived?.Invoke(item.DeserializeObject<Heartbeat>());

				break;
			}

			case Channels.Candles:
			{
				foreach (var item in arr)
				{
					foreach (var candle in item["candles"].DeserializeObject<IEnumerable<Ohlc>>())
						CandleReceived?.Invoke(candle);
				}

				break;
			}

			case Channels.Ticker:
			{
				foreach (var item in arr)
				{
					foreach (var ticker in item["tickers"].DeserializeObject<IEnumerable<Ticker>>())
						TickerReceived?.Invoke(ticker);
				}

				break;
			}

			case "l2_data":
			case Channels.OrderBook:
			{
				foreach (var item in arr)
				{
					var type = (string)item["type"];
					var symbol = (string)item["product_id"];
				
					OrderBookReceived?.Invoke(type, symbol, item["updates"].DeserializeObject<IEnumerable<OrderBookChange>>());
				}

				break;
			}

			case Channels.Trades:
			{
				foreach (var item in arr)
				{
					foreach (var trade in item["trades"].DeserializeObject<IEnumerable<Trade>>())
						TradeReceived?.Invoke(trade);
				}

				break;
			}

			case Channels.Subscriptions:
			case Channels.Status:
				break;

			case Channels.User:
			{
				foreach (var item in arr)
				{
					foreach (var order in item["orders"].DeserializeObject<IEnumerable<Order>>())
						OrderReceived?.Invoke(order);
				}

				break;
			}

			default:
				this.AddErrorLog(LocalizedStrings.UnknownEvent, channel);
				break;
		}

		return default;
	}

	private static class Channels
	{
		public const string Candles = "candles";
		public const string Ticker = "ticker";
		public const string Trades = "market_trades";
		public const string OrderBook = "level2";
		public const string Heartbeat = "heartbeat";
		public const string Status = "status";
		public const string User = "user";
		public const string Subscriptions = "subscriptions";
	}

	private static class Commands
	{
		public const string Subscribe = "subscribe";
		public const string UnSubscribe = "unsubscribe";
	}

	public ValueTask SubscribeOrders(long transId, CancellationToken cancellationToken)
	{
		return Process(transId, Commands.Subscribe, Channels.User, null, cancellationToken);
	}

	public ValueTask UnSubscribeOrders(long originTransId, CancellationToken cancellationToken)
	{
		return Process(-originTransId, Commands.UnSubscribe, Channels.User, null, cancellationToken);
	}

	public ValueTask SubscribeTicker(long transId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, Commands.Subscribe, Channels.Ticker, symbol, cancellationToken);
	}

	public ValueTask UnSubscribeTicker(long originTransId, string symbol, CancellationToken cancellationToken)
	{
		return Process(-originTransId, Commands.UnSubscribe, Channels.Ticker, symbol, cancellationToken);
	}

	public ValueTask SubscribeTrades(long transId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, Commands.Subscribe, Channels.Trades, symbol, cancellationToken);
	}

	public ValueTask UnSubscribeTrades(long originTransId, string symbol, CancellationToken cancellationToken)
	{
		return Process(-originTransId, Commands.UnSubscribe, Channels.Trades, symbol, cancellationToken);
	}

	public ValueTask SubscribeOrderBook(long transId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, Commands.Subscribe, Channels.OrderBook, symbol, cancellationToken);
	}

	public ValueTask UnSubscribeOrderBook(long originTransId, string symbol, CancellationToken cancellationToken)
	{
		return Process(-originTransId, Commands.UnSubscribe, Channels.OrderBook, symbol, cancellationToken);
	}

	public ValueTask SubscribeCandles(long transId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, Commands.Subscribe, Channels.Candles, symbol, cancellationToken);
	}

	public ValueTask UnSubscribeCandles(long originTransId, string symbol, CancellationToken cancellationToken)
	{
		return Process(-originTransId, Commands.UnSubscribe, Channels.Candles, symbol, cancellationToken);
	}

	private ValueTask Process(long subId, string type, string channel, string symbol, CancellationToken cancellationToken)
	{
		if (type.IsEmpty())
			throw new ArgumentNullException(nameof(type));

		if (channel.IsEmpty())
			throw new ArgumentNullException(nameof(channel));

		//if (symbol.IsEmpty())
		//	throw new ArgumentNullException(nameof(symbol));

		return _client.SendAsync(new
		{
			type,
			product_ids = symbol == null ? null : new[] { symbol },
			channel,
		}, cancellationToken, subId);
	}
}