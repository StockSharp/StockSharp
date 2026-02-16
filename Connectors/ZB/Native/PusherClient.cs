namespace StockSharp.ZB.Native;

class PusherClient : BaseLogReceiver
{
	// to get readable name after obfuscation
	public override string Name => nameof(ZB) + "_" + nameof(PusherClient);

	public event Func<string, DateTime, Ticker, CancellationToken, ValueTask> TickerChanged;
	public event Func<string, IEnumerable<Trade>, CancellationToken, ValueTask> NewTrades;
	public event Func<string, OrderBook, CancellationToken, ValueTask> OrderBookChanged;
	public event Func<long, IEnumerable<Order>, CancellationToken, ValueTask> NewOrders;
	public event Func<long, Order, CancellationToken, ValueTask> OrderChanged;
	public event Func<long, IEnumerable<Balance>, CancellationToken, ValueTask> BalancesChanged;
	public event Func<Exception, CancellationToken, ValueTask> Error;
	public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;
	//public event Action<string> TradesSubscribed;
	//public event Action<string> OrderBooksSubscribed;

	private readonly WebSocketClient _client;
	private readonly Authenticator _authenticator;

	private const string _subscribe = "addChannel";
	private const string _unsubscribe = "removeChannel";

	public PusherClient(Authenticator authenticator, WorkingTime workingTime)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));

		_client = new(
			"wss://api.zb.cn/websocket",
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

		var channel = (string)obj.channel;

		if (channel.EndsWithIgnoreCase(Channels.Ticker))
		{
			if (TickerChanged is { } handler)
				await handler(channel.Remove(Channels.Ticker, true), ((long)obj.date).FromUnix(false), ((JToken)obj.ticker).DeserializeObject<Ticker>(), cancellationToken);
		}
		else if (channel.EndsWithIgnoreCase(Channels.OrderBook))
		{
			if (OrderBookChanged is { } handler)
				await handler(channel.Remove(Channels.OrderBook, true), ((JToken)obj).DeserializeObject<OrderBook>(), cancellationToken);
		}
		else if (channel.EndsWithIgnoreCase(Channels.Trades))
		{
			if (NewTrades is { } handler)
				await handler(channel.Remove(Channels.Trades, true), ((JToken)obj.data).DeserializeObject<IEnumerable<Trade>>(), cancellationToken);
		}
		else if (channel.EqualsIgnoreCase(Channels.GetOrders))
		{
			if (NewOrders is { } handler)
				await handler((long)obj.no, ((JToken)obj.data.coins).DeserializeObject<IEnumerable<Order>>(), cancellationToken);
		}
		else if (channel.EqualsIgnoreCase(Channels.AccountInfo))
		{
			if (BalancesChanged is { } handler)
				await handler((long)obj.no, ((JToken)obj.data.coins).DeserializeObject<IEnumerable<Balance>>(), cancellationToken);
		}
		else
			this.AddErrorLog(LocalizedStrings.UnknownEvent, channel);
	}

	private static class Channels
	{
		public const string Ticker = "_ticker";
		public const string OrderBook = "_depth";
		public const string Trades = "_trades";
		public const string Order = "_order";
		public const string CancelOrder = "_cancelorder";
		public const string GetOrders = "_getorders";
		public const string AccountInfo = "getaccountinfo";
	}

	public ValueTask SubscribeTickerAsync(string symbol, CancellationToken cancellationToken)
	{
		return ProcessAsync(_subscribe, symbol, Channels.Ticker, cancellationToken);
	}

	public ValueTask UnSubscribeTickerAsync(string symbol, CancellationToken cancellationToken)
	{
		return ProcessAsync(_unsubscribe, symbol, Channels.Ticker, cancellationToken);
	}

	public ValueTask SubscribeTradesAsync(string symbol, CancellationToken cancellationToken)
	{
		return ProcessAsync(_subscribe, symbol, Channels.Trades, cancellationToken);
	}

	public ValueTask UnSubscribeTradesAsync(string symbol, CancellationToken cancellationToken)
	{
		return ProcessAsync(_unsubscribe, symbol, Channels.Trades, cancellationToken);
	}

	public ValueTask SubscribeOrderBookAsync(string symbol, CancellationToken cancellationToken)
	{
		return ProcessAsync(_subscribe, symbol, Channels.OrderBook, cancellationToken);
	}

	public ValueTask UnSubscribeOrderBookAsync(string symbol, CancellationToken cancellationToken)
	{
		return ProcessAsync(_unsubscribe, symbol, Channels.OrderBook, cancellationToken);
	}

	private ValueTask ProcessAsync(string @event, string symbol, string channel, CancellationToken cancellationToken)
	{
		if (@event.IsEmpty())
			throw new ArgumentNullException(nameof(@event));

		if (channel.IsEmpty())
			throw new ArgumentNullException(nameof(channel));

		channel = symbol + channel;

		return _client.SendAsync(new
		{
			@event,
			channel,
		}, cancellationToken);
	}

	public ValueTask RegisterOrderAsync(long no, string symbol, int type, decimal? price, decimal amount, CancellationToken cancellationToken)
	{
		return _client.SendAsync(new
		{
			accesskey = _authenticator.Key.UnSecure(),
			channel = symbol + Channels.Order,
			@event = _subscribe,
			price,
			amount,
			type,
			no,
			sign = _authenticator.MakeSign(null),
		}, cancellationToken);
	}

	public ValueTask CancelOrderAsync(long no, string symbol, long orderId, CancellationToken cancellationToken)
	{
		return _client.SendAsync(new
		{
			accesskey = _authenticator.Key.UnSecure(),
			channel = symbol + Channels.CancelOrder,
			@event = _subscribe,
			id = orderId,
			no,
			sign = _authenticator.MakeSign(null),
		}, cancellationToken);
	}

	public ValueTask SubscribeOrdersAsync(long no, string symbol, CancellationToken cancellationToken)
	{
		return _client.SendAsync(new
		{
			accesskey = _authenticator.Key.UnSecure(),
			channel = symbol + Channels.GetOrders,
			@event = _subscribe,
			pageIndex = 1,
			no,
			sign = _authenticator.MakeSign(null),
		}, cancellationToken);
	}

	public ValueTask SubscribeAccountAsync(long no, CancellationToken cancellationToken)
	{
		return _client.SendAsync(new
		{
			accesskey = _authenticator.Key.UnSecure(),
			channel = Channels.AccountInfo,
			@event = _subscribe,
			no,
			sign = _authenticator.MakeSign(null),
		}, cancellationToken);
	}
}