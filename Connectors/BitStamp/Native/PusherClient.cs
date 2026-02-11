namespace StockSharp.BitStamp.Native;

using Ecng.ComponentModel;

using Newtonsoft.Json.Linq;

class PusherClient : BaseLogReceiver
{
	public event Func<string, Trade, CancellationToken, ValueTask> NewTrade;
	public event Func<string, OrderBook, CancellationToken, ValueTask> NewOrderBook;
	public event Func<string, OrderStates, Order, CancellationToken, ValueTask> NewOrderLog;
	public event Func<Exception, CancellationToken, ValueTask> Error;
	public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;
	public event Action<string> TradesSubscribed;
	public event Action<string> OrderBookSubscribed;
	public event Action<string> OrderLogSubscribed;
	public event Action<string> TradesUnSubscribed;
	public event Action<string> OrderBookUnSubscribed;
	public event Action<string> OrderLogUnSubscribed;

	private int _activityTimeout;
	private DateTime? _nextPing;

	private readonly WebSocketClient _client;

	public PusherClient(int attemptsCount, WorkingTime workingTime)
	{
		_client = new(
			"wss://ws.bitstamp.net",
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
		_nextPing = null;
		_activityTimeout = 0;

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
		var evt = (string)obj.@event;
		var data = obj.data;

		//if (data != null && evt != "bts:error")
		//	data = ((string)data).DeserializeObject<object>();

		switch (evt)
		{
			//case "pusher:connection_established":
			//	_activityTimeout = (int)data.activity_timeout;
			//	_nextPing = DateTime.UtcNow.AddSeconds(_activityTimeout);
			//	Connected?.Invoke();
			//	break;

			case "bts:error":
				if (Error is { } errorHandler)
					await errorHandler(new InvalidOperationException((string)data.message), cancellationToken);
				break;

			case "ping":
				await SendPingPong("pong", cancellationToken);
				break;

			case "pong":
				break;

			case "bts:subscription_succeeded":
			{
				if (channel.StartsWith(ChannelNames.OrderBook))
					OrderBookSubscribed?.Invoke(GetPair(channel, ChannelNames.OrderBook));
				else if (channel.StartsWith(ChannelNames.Trades))
					TradesSubscribed?.Invoke(GetPair(channel, ChannelNames.Trades));
				else if (channel.StartsWith(ChannelNames.OrderLog))
					OrderLogSubscribed?.Invoke(GetPair(channel, ChannelNames.OrderLog));
				else
					this.AddErrorLog(LocalizedStrings.UnknownEvent, channel);

				break;
			}

			case "bts:unsubscription_succeeded":
			{
				if (channel.StartsWith(ChannelNames.OrderBook))
					OrderBookUnSubscribed?.Invoke(GetPair(channel, ChannelNames.OrderBook));
				else if (channel.StartsWith(ChannelNames.Trades))
					TradesUnSubscribed?.Invoke(GetPair(channel, ChannelNames.Trades));
				else if (channel.StartsWith(ChannelNames.OrderLog))
					OrderLogUnSubscribed?.Invoke(GetPair(channel, ChannelNames.OrderLog));
				else
					this.AddErrorLog(LocalizedStrings.UnknownEvent, channel);

				break;
			}

			case "trade":
				if (NewTrade is { } tradeHandler)
					await tradeHandler(GetPair(channel, ChannelNames.Trades), ((JToken)data).DeserializeObject<Trade>(), cancellationToken);
				break;

			case "data":
				if (NewOrderBook is { } bookHandler)
					await bookHandler(GetPair(channel, ChannelNames.OrderBook), ((JToken)data).DeserializeObject<OrderBook>(), cancellationToken);
				break;

			case "order_created":
			case "order_changed":
			case "order_deleted":
				if (NewOrderLog is { } logHandler)
					await logHandler(GetPair(channel, ChannelNames.OrderLog), evt == "order_deleted" ? OrderStates.Done : OrderStates.Active, ((JToken)data).DeserializeObject<Order>(), cancellationToken);
				break;

			default:
				this.AddErrorLog(LocalizedStrings.UnknownEvent, evt);
				break;
		}
	}

	private static string GetPair(string channel, string name)
	{
		channel = channel.Remove(name).Remove("_");

		if (channel.IsEmpty())
			channel = "btcusd";

		return channel;
	}

	private static class ChannelNames
	{
		public const string Trades = "live_trades_";
		public const string OrderBook = "order_book_";
		public const string OrderLog = "live_orders_";
	}

	private static class Commands
	{
		public const string Subscribe = "subscribe";
		public const string UnSubscribe = "unsubscribe";
	}

	public ValueTask SubscribeTrades(long transId, string currency, CancellationToken cancellationToken)
		=> Process(transId, Commands.Subscribe, ChannelNames.Trades + currency, cancellationToken);

	public ValueTask UnSubscribeTrades(long originTransId, string currency, CancellationToken cancellationToken)
		=> Process(-originTransId, Commands.UnSubscribe, ChannelNames.Trades + currency, cancellationToken);

	public ValueTask SubscribeOrderBook(long transId, string currency, CancellationToken cancellationToken)
		=> Process(transId, Commands.Subscribe, ChannelNames.OrderBook + currency, cancellationToken);

	public ValueTask UnSubscribeOrderBook(long originTransId, string currency, CancellationToken cancellationToken)
		=> Process(-originTransId, Commands.UnSubscribe, ChannelNames.OrderBook + currency, cancellationToken);

	public ValueTask SubscribeOrderLog(long transId, string currency, CancellationToken cancellationToken)
		=> Process(transId, Commands.Subscribe, ChannelNames.OrderLog + currency, cancellationToken);

	public ValueTask UnSubscribeOrderLog(long originTransId, string currency, CancellationToken cancellationToken)
		=> Process(-originTransId, Commands.UnSubscribe, ChannelNames.OrderLog + currency, cancellationToken);

	private ValueTask Process(long subId, string action, string channel, CancellationToken cancellationToken)
	{
		if (action.IsEmpty())
			throw new ArgumentNullException(nameof(action));

		if (channel.IsEmpty())
			throw new ArgumentNullException(nameof(channel));

		return _client.SendAsync(new
		{
			@event = $"bts:{action}",
			data = new { channel }
		}, cancellationToken, subId);
	}

	public ValueTask ProcessPing(CancellationToken cancellationToken)
	{
		if (_nextPing == null || DateTime.UtcNow < _nextPing.Value)
			return default;

		return SendPingPong("ping", cancellationToken);
	}

	private ValueTask SendPingPong(string action, CancellationToken cancellationToken)
	{
		try
		{
			return _client.SendAsync(new { @event = $"{action}" }, cancellationToken);
		}
		finally
		{
			if (_activityTimeout > 0)
				_nextPing = DateTime.UtcNow.AddSeconds(_activityTimeout);
		}
	}
}
