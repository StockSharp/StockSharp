namespace StockSharp.Btce.Native;

using Ecng.ComponentModel;

class PusherClient : BaseLogReceiver
{
	// to get readable name after obfuscation
	public override string Name => nameof(Btce) + "_" + nameof(PusherClient);

	public event Func<string, PusherTransaction[], CancellationToken, ValueTask> NewTrades;
	public event Func<string, OrderBook, CancellationToken, ValueTask> OrderBookChanged;
	public event Func<Exception, CancellationToken, ValueTask> Error;
	public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;

	private readonly WebSocketClient _client;

	public PusherClient(int attemptsCount, WorkingTime workingTime)
	{
		_client = new(
			"wss://ws-eu.pusher.com/app/ee987526a24ba107824c?client=stocksharp&version=1.0&protocol=7",
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
			WorkingTime = workingTime ?? throw new ArgumentNullException(nameof(workingTime)),
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
		var evt = (string)obj.@event;

		switch (evt)
		{
			case "pusher_internal:subscription_succeeded":
				break;

			case "connection_established":
				break;

			case Channels.OrderBook:
				if (OrderBookChanged is { } obHandler)
					await obHandler(((string)obj.channel).Remove("." + Channels.OrderBook), ((string)obj.data).DeserializeObject<OrderBook>(), cancellationToken);
				break;

			case Channels.Trades:
				if (NewTrades is { } ntHandler)
					await ntHandler(((string)obj.channel).Remove("." + Channels.Trades), ((string)obj.data).DeserializeObject<PusherTransaction[]>(), cancellationToken);
				break;

			default:
				this.AddErrorLog(LocalizedStrings.UnknownEvent, evt);
				break;
		}
	}

	private static class Channels
	{
		public const string Trades = "trades";
		public const string OrderBook = "depth";
	}

	public ValueTask SubscribeTrades(long transId, string currency, CancellationToken cancellationToken)
	{
		return Process(transId, "subscribe", Channels.Trades, currency, cancellationToken);
	}

	public ValueTask UnSubscribeTrades(long originTransId, string currency, CancellationToken cancellationToken)
	{
		return Process(-originTransId, "unsubscribe", Channels.Trades, currency, cancellationToken);
	}

	public ValueTask SubscribeOrderBook(long transId, string currency, CancellationToken cancellationToken)
	{
		return Process(transId, "subscribe", Channels.OrderBook, currency, cancellationToken);
	}

	public ValueTask UnSubscribeOrderBook(long originTransId, string currency, CancellationToken cancellationToken)
	{
		return Process(-originTransId, "unsubscribe", Channels.OrderBook, currency, cancellationToken);
	}

	private ValueTask Process(long subId, string action, string channel, string currency, CancellationToken cancellationToken)
	{
		if (action.IsEmpty())
			throw new ArgumentNullException(nameof(action));

		if (currency.IsEmpty())
			throw new ArgumentNullException(nameof(currency));

		if (channel.IsEmpty())
			throw new ArgumentNullException(nameof(channel));

		return _client.SendAsync(new
		{
			@event = "pusher:" + action,
			data = new
			{
				channel = currency + "." + channel,
			},
		}, cancellationToken, subId);
	}
}