namespace StockSharp.PrizmBit.Native;

class PusherClient : BaseLogReceiver
{
	// to get readable name after obfuscation
	public override string Name => nameof(PrizmBit) + "_" + nameof(PusherClient);

	public event Func<DateTime, MarketPrice, CancellationToken, ValueTask> MarketPriceChanged;
	public event Func<DateTime, OrderBook, CancellationToken, ValueTask> OrderBookChanged;
	public event Func<DateTime, SocketTrade, CancellationToken, ValueTask> NewTrade;
	public event Func<DateTime, SocketUserTrade, CancellationToken, ValueTask> NewUserTrade;
	public event Func<DateTime, CanceledOrder, CancellationToken, ValueTask> OrderCanceled;
	public event Func<DateTime, UserCanceledOrder, CancellationToken, ValueTask> UserOrderCanceled;
	public event Func<DateTime, Balance, CancellationToken, ValueTask> BalanceChanged;
	public event Func<Exception, CancellationToken, ValueTask> Error;
	public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;

	private readonly WebSocketClient _client;
	private readonly Authenticator _authenticator;

	public PusherClient(Authenticator authenticator, WorkingTime workingTime)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));

		_client = new(
			"wss://wss.prizmbit.com",
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
	}

	protected override void DisposeManaged()
	{
		_client.Dispose();
		base.DisposeManaged();
	}

	public ValueTask ConnectAsync(CancellationToken cancellationToken)
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

		var type = (string)obj.type;
		var timestamp = ((long)obj.timestamp).FromUnix(false);
		var data = (JToken)obj.data;

		switch (type)
		{
			case Channels.MarketPrice:
				await (MarketPriceChanged?.Invoke(timestamp, data.DeserializeObject<MarketPrice>(), cancellationToken) ?? default);
				break;
			case Channels.Trade:
				await (NewTrade?.Invoke(timestamp, data.DeserializeObject<SocketTrade>(), cancellationToken) ?? default);
				break;
			case Channels.UserTrade:
				await (NewUserTrade?.Invoke(timestamp, data.DeserializeObject<SocketUserTrade>(), cancellationToken) ?? default);
				break;
			case Channels.OrderBook:
				await (OrderBookChanged?.Invoke(timestamp, data.DeserializeObject<OrderBook>(), cancellationToken) ?? default);
				break;
			case Channels.CanceledOrder:
				await (OrderCanceled?.Invoke(timestamp, data.DeserializeObject<CanceledOrder>(), cancellationToken) ?? default);
				break;
			case Channels.UserCanceledOrder:
				await (UserOrderCanceled?.Invoke(timestamp, data.DeserializeObject<UserCanceledOrder>(), cancellationToken) ?? default);
				break;
			case Channels.ParaminingUpdate:
				//MarketPriceChanged?.Invoke(timestamp, data.DeserializeObject<MarketPrice>());
				break;
			case Channels.TradingBalance:
				await (BalanceChanged?.Invoke(timestamp, data.DeserializeObject<Balance>(), cancellationToken) ?? default);
				break;
			default:
				this.AddErrorLog(LocalizedStrings.UnknownEvent, type);
				break;
		}

		return;
	}

	private static class Channels
	{
		//public const string Ping = "ping";
		//public const string Pong = "pong";
		public const string MarketPrice = "MarketPrice";
		public const string OrderBook = "OrderBook";
		public const string Trade = "Trade";
		public const string UserTrade = "UserTrade";
		public const string CanceledOrder = "CanceledOrder";
		public const string UserCanceledOrder = "UserCanceledOrder";
		public const string ParaminingUpdate = "ParaminingUpdate";
		public const string TradingBalance = "TradingBalance";
	}

	public ValueTask SubscribeTicker(long marketId, CancellationToken cancellationToken)
	{
		return _client.SendAsync(new { marketId }, cancellationToken);
	}

	public ValueTask SubscribeAccount(CancellationToken cancellationToken)
	{
		return _client.SendAsync(new { clientId = _authenticator.Key.UnSecure() }, cancellationToken);
	}
}