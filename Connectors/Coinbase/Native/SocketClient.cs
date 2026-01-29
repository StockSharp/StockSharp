namespace StockSharp.Coinbase.Native;

using Newtonsoft.Json.Linq;

class SocketClient : BaseLogReceiver
{
	// to get readable name after obfuscation
	public override string Name => nameof(Coinbase) + "_" + nameof(SocketClient);

	public event Func<Heartbeat, CancellationToken, ValueTask> HeartbeatReceived;
	public event Func<Ohlc, CancellationToken, ValueTask> CandleReceived;
	public event Func<Ticker, CancellationToken, ValueTask> TickerReceived;
	public event Func<Trade, CancellationToken, ValueTask> TradeReceived;
	public event Func<string, string, IEnumerable<OrderBookChange>, CancellationToken, ValueTask> OrderBookReceived;
	public event Func<Order, CancellationToken, ValueTask> OrderReceived;
	public event Func<Exception, CancellationToken, ValueTask> Error;
	public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;

	private readonly WebSocketClient _client;
	private readonly Authenticator _authenticator;

	public SocketClient(Authenticator authenticator, int reconnectAttempts)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));

		_client = new(
			"wss://advanced-trade-ws.coinbase.com",
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

	private async ValueTask OnProcess(WebSocketMessage msg, CancellationToken cancellationToken)
	{
		var obj = msg.AsObject();
		var channel = (string)obj.channel;
		var arr = (JArray)obj.events;

		switch (channel)
		{
			case "error":
			{
				if (Error is { } errorHandler)
					await errorHandler(new InvalidOperationException((string)obj.message + " " + (string)obj.reason), cancellationToken);
				break;
			}

			case Channels.Heartbeat:
			{
				if (HeartbeatReceived is { } handler)
				{
					foreach (var item in arr)
						await handler(item.DeserializeObject<Heartbeat>(), cancellationToken);
				}

				break;
			}

			case Channels.Candles:
			{
				if (CandleReceived is { } handler)
				{
					foreach (var item in arr)
					{
						foreach (var candle in item["candles"].DeserializeObject<IEnumerable<Ohlc>>())
							await handler(candle, cancellationToken);
					}
				}

				break;
			}

			case Channels.Ticker:
			{
				if (TickerReceived is { } handler)
				{
					foreach (var item in arr)
					{
						foreach (var ticker in item["tickers"].DeserializeObject<IEnumerable<Ticker>>())
							await handler(ticker, cancellationToken);
					}
				}

				break;
			}

			case "l2_data":
			case Channels.OrderBook:
			{
				if (OrderBookReceived is { } handler)
				{
					foreach (var item in arr)
					{
						var type = (string)item["type"];
						var symbol = (string)item["product_id"];

						await handler(type, symbol, item["updates"].DeserializeObject<IEnumerable<OrderBookChange>>(), cancellationToken);
					}
				}

				break;
			}

			case Channels.Trades:
			{
				if (TradeReceived is { } handler)
				{
					foreach (var item in arr)
					{
						foreach (var trade in item["trades"].DeserializeObject<IEnumerable<Trade>>())
							await handler(trade, cancellationToken);
					}
				}

				break;
			}

			case Channels.Subscriptions:
			case Channels.Status:
				break;

			case Channels.User:
			{
				if (OrderReceived is { } handler)
				{
					foreach (var item in arr)
					{
						foreach (var order in item["orders"].DeserializeObject<IEnumerable<Order>>())
							await handler(order, cancellationToken);
					}
				}

				break;
			}

			default:
				this.AddErrorLog(LocalizedStrings.UnknownEvent, channel);
				break;
		}
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
		return ProcessPrivate(transId, Commands.Subscribe, Channels.User, null, cancellationToken);
	}

	public ValueTask UnSubscribeOrders(long originTransId, CancellationToken cancellationToken)
	{
		return ProcessPrivate(-originTransId, Commands.UnSubscribe, Channels.User, null, cancellationToken);
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

		return _client.SendAsync(new
		{
			type,
			product_ids = symbol == null ? null : new[] { symbol },
			channel,
		}, cancellationToken, subId);
	}

	private ValueTask ProcessPrivate(long subId, string type, string channel, string symbol, CancellationToken cancellationToken)
	{
		if (type.IsEmpty())
			throw new ArgumentNullException(nameof(type));

		if (channel.IsEmpty())
			throw new ArgumentNullException(nameof(channel));

		if (_authenticator.UseLegacyAuth)
		{
			// Legacy HMAC authentication for WebSocket
			var timestamp = DateTime.UtcNow.ToUnix().ToString("F0");
			var signature = _authenticator.MakeHmacSign("/users/self/verify", Method.Get, string.Empty, out _);

			return _client.SendAsync(new
			{
				type,
				product_ids = symbol == null ? null : new[] { symbol },
				channel,
				api_key = _authenticator.Key.UnSecure(),
				timestamp,
				signature,
			}, cancellationToken, subId);
		}
		else
		{
			// CDP JWT authentication for WebSocket
			var jwt = _authenticator.GenerateWebSocketJwt();

			return _client.SendAsync(new
			{
				type,
				product_ids = symbol == null ? null : new[] { symbol },
				channel,
				jwt,
			}, cancellationToken, subId);
		}
	}
}
