namespace StockSharp.FTX.Native;

using System.Security;
using System.Security.Cryptography;

using Ecng.ComponentModel;

using Newtonsoft.Json.Linq;

/// <summary>
/// Subscriber for Websocket "trade" channel
/// </summary>
[Flags]
internal enum WsTradeChannelSubscriber
{
	None = 0,
	Trade = 1 << 0,
	Candles = 1 << 1
}

class FtxWebSocketClient : BaseLogReceiver
{
	private readonly SecureString _key;
	private readonly HMACSHA256 _hasher;

	public event Func<string, List<Trade>, CancellationToken, ValueTask> NewTrade;
	public event Func<string, OrderBook, QuoteChangeStates, CancellationToken, ValueTask> NewOrderBook;
	public event Func<string, Level1, CancellationToken, ValueTask> NewLevel1;
	public event Func<Order, CancellationToken, ValueTask> NewOrder;
	public event Func<Fill, CancellationToken, ValueTask> NewFill;
	public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;
	public event Func<Exception, CancellationToken, ValueTask> Error;

	private DateTime? _nextPing;

	private readonly WebSocketClient _client;

	public FtxWebSocketClient(SecureString key, SecureString secret, string subaccountName, int attemptsCount)
	{
		_key = key;
		_hasher = secret.IsEmpty() ? null : new(secret.UnSecure().UTF8());

		_client = new(
			"wss://ftx.com/ws",
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
			ReconnectAttempts = attemptsCount,
		};
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		_client.Dispose();

		base.DisposeManaged();
	}

	/// <summary>
	/// Connect to the websocket
	/// </summary>
	public ValueTask Connect(CancellationToken cancellationToken)
	{
		_nextPing = null;
		this.AddInfoLog(LocalizedStrings.Connecting);
		return _client.ConnectAsync(cancellationToken);
	}

	/// <summary>
	/// Closes the WebSocket connection
	/// </summary>
	public void Disconnect()
	{
		this.AddInfoLog(LocalizedStrings.Disconnecting);
		_client.Disconnect();
	}

	/// <summary>
	/// Market ping request
	/// </summary>
	public ValueTask ProcessPing(CancellationToken cancellationToken)
	{
		if (_nextPing == null || DateTime.UtcNow < _nextPing.Value)
		{
			return default;
		}
		return SendPingRequest(cancellationToken);
	}

	public ValueTask SubscribeFills(long transId, CancellationToken cancellationToken)
	{
		if (_client == null || !_client.IsConnected)
			return default;

		return SendAsync(transId, new
		{
			op = "subscribe",
			channel = "fills"
		}, cancellationToken);
	}

	public ValueTask SubscribeOrders(long transId, CancellationToken cancellationToken)
	{
		if (_client == null || !_client.IsConnected)
			return default;

		return SendAsync(transId, new
		{
			op = "subscribe",
			channel = "orders"
		}, cancellationToken);
	}

	private readonly SynchronizedList<string> _level1Subs = new();

	public async ValueTask SubscribeLevel1(long transId, string market, CancellationToken cancellationToken)
	{
		var subs = _level1Subs.ToList();

		if (subs.All(f => f != market))
		{
			if (_client == null || !_client.IsConnected)
				return;

			await SendAsync(transId, new
			{
				op = "subscribe",
				channel = "ticker",
				market
			}, cancellationToken);
		}

		_level1Subs.Add(market);
	}

	public async ValueTask UnsubscribeLevel1(long originTransId, string market, CancellationToken cancellationToken)
	{
		var subs = _level1Subs.ToList();

		var subToDel = subs.FirstOrDefault(d => d == market);

		if (subToDel == null)
			return;

		_level1Subs.Remove(market);

		if (_level1Subs.All(d => d != market))
		{
			if (_client == null || !_client.IsConnected)
				return;

			await SendAsync(-originTransId, new
			{
				op = "unsubscribe",
				channel = "ticker",
				market
			}, cancellationToken);
		}
	}

	private WsTradeChannelSubscriber _tradesMarketChannelSubFlags;

	public async ValueTask SubscribeTradesChannel(long transId, string market, WsTradeChannelSubscriber subscriber, CancellationToken cancellationToken)
	{
		if (_tradesMarketChannelSubFlags == WsTradeChannelSubscriber.None)
		{
			if (_client == null || !_client.IsConnected)
				return;

			await SendAsync(transId, new
			{
				op = "subscribe",
				channel = "trades",
				market
			}, cancellationToken);
		}
		_tradesMarketChannelSubFlags |= subscriber;
	}

	public ValueTask UnsubscribeTradesChannel(long originTransId, string market, WsTradeChannelSubscriber subscriber, CancellationToken cancellationToken)
	{
		_tradesMarketChannelSubFlags = Enumerator.Remove(_tradesMarketChannelSubFlags, subscriber);

		if (_tradesMarketChannelSubFlags == WsTradeChannelSubscriber.None)
		{
			if (_client == null || !_client.IsConnected)
				return default;

			return SendAsync(-originTransId, new
			{
				op = "unsubscribe",
				channel = "trades",
				market
			}, cancellationToken);
		}

		return default;
	}

	public ValueTask SubscribeOrderBook(long transId, string market, CancellationToken cancellationToken)
	{
		if (_client == null || !_client.IsConnected)
			return default;

		return SendAsync(transId, new
		{
			op = "subscribe",
			channel = "orderbook",
			market
		}, cancellationToken);

	}

	public ValueTask UnsubscribeOrderBook(long originTransId, string market, CancellationToken cancellationToken)
	{
		if (_client == null || !_client.IsConnected)
			return default;

		return SendAsync(-originTransId, new
		{
			op = "unsubscribe",
			channel = "orderbook",
			market
		}, cancellationToken);
	}


	private async ValueTask OnProcess(WebSocketMessage msg, CancellationToken cancellationToken)
	{
		var obj = msg.AsObject();
		var channel = (string)obj.channel;
		var type = (string)obj.type;

		if (channel == "ticker")
		{
			if (type != "update") return;

			WebSocketResponse<Level1> level1 = Parse<WebSocketResponse<Level1>>(obj);
			if (level1 != null && level1.Data != null)
			{
				if (NewLevel1 is { } handler)
					await handler(level1.Market, level1.Data, cancellationToken);
			}
		}
		else if (channel == "trades")
		{
			if (type != "update") return;

			WebSocketResponse<List<Trade>> trade = Parse<WebSocketResponse<List<Trade>>>(obj);

			if (trade != null && trade.Data != null)
			{
				if (NewTrade is { } handler)
					await handler(trade.Market, trade.Data, cancellationToken);
			}
		}
		else if (channel == "orderbook")
		{
			if (type != "update" && type != "partial") return;

			WebSocketResponse<OrderBook> ob = Parse<WebSocketResponse<OrderBook>>(obj);
			if (ob != null && ob.Data != null)
			{
				if (NewOrderBook is { } handler)
					await handler(ob.Market, ob.Data, type == "partial" ? QuoteChangeStates.SnapshotComplete : QuoteChangeStates.Increment, cancellationToken);
			}
		}
		else if (channel == "orders")
		{
			if (type != "update") return;

			WebSocketResponse<Order> order = Parse<WebSocketResponse<Order>>(obj);
			if (order != null && order.Data != null)
			{
				if (NewOrder is { } handler)
					await handler(order.Data, cancellationToken);
			}
		}
		else if (channel == "fills")
		{
			if (type != "update") return;

			WebSocketResponse<Fill> fill = Parse<WebSocketResponse<Fill>>(obj);
			if (fill != null && fill.Data != null)
			{
				if (NewFill is { } handler)
					await handler(fill.Data, cancellationToken);
			}
		}
	}

	#region Utils
	private static long GetMillisecondsFromEpochStart()
	{
		return GetMillisecondsFromEpochStart(DateTime.UtcNow);
	}

	private static long GetMillisecondsFromEpochStart(DateTime time)
	{
		return (long)time.ToUnix(false);
	}

	private ValueTask SendAuthRequest(string subaccountName, CancellationToken cancellationToken)
	{
		long time = GetMillisecondsFromEpochStart();
		string sign = GenerateSignature(time);

		if (_client == null || !_client.IsConnected)
			return default;

		if (subaccountName == null)
		{
			return SendAsync(0, new
			{
				op = "login",
				args = new
				{
					key = _key.UnSecure(),
					sign,
					time
				}
			}, cancellationToken);
		}
		else
		{
			return SendAsync(0, new
			{
				op = "login",
				args = new
				{
					key = _key.UnSecure(),
					sign,
					time,
					subaccount = subaccountName
				}
			}, cancellationToken);
		}
	}

	public async ValueTask SendPingRequest(CancellationToken cancellationToken)
	{
		if (_client == null || !_client.IsConnected)
			return;

		await SendAsync(0, new
		{
			op = "ping"
		}, cancellationToken);

		_nextPing = DateTime.UtcNow.AddSeconds(5);
	}

	private ValueTask SendAsync(long subId, object request, CancellationToken cancellationToken)
		=> _client.SendAsync(request, cancellationToken, subId);

	private string GenerateSignature(long time)
	{
		var signature = $"{time}websocket_login";
		return _hasher.ComputeHash(signature.UTF8()).Digest();
	}

	private static T Parse<T>(dynamic obj)
	{
		if (((JToken)obj).Type == JTokenType.Object && obj.status == "error")
			throw new InvalidOperationException((string)obj.reason.ToString());

		return ((JToken)obj).DeserializeObject<T>();
	}
	#endregion
}