namespace StockSharp.Bitexbook.Native;

using Ecng.ComponentModel;

using Newtonsoft.Json.Linq;

class PusherClient : BaseLogReceiver
{
	// to get readable name after obfuscation
	public override string Name => nameof(Bitexbook) + "_" + nameof(PusherClient);

	public event Func<IEnumerable<Symbol>, CancellationToken, ValueTask> NewSymbols;
	public event Func<TickerChange, CancellationToken, ValueTask> NewTickerChange;
	public event Func<Ticker, CancellationToken, ValueTask> TickerChanged;
	public event Func<IEnumerable<Order>, CancellationToken, ValueTask> LatestOrders;
	public event Func<IEnumerable<Ticket>, CancellationToken, ValueTask> TicketsActive;
	public event Func<Ticket, CancellationToken, ValueTask> TicketAdded;
	public event Func<Ticket, CancellationToken, ValueTask> TicketCanceled;
	public event Func<Ticket, CancellationToken, ValueTask> TicketExecuted;
	public event Func<Exception, CancellationToken, ValueTask> Error;
	public event Func<ConnectionStates, CancellationToken, ValueTask> StateChanged;
	//public event Action<string> TradesSubscribed;
	//public event Action<string> OrderBooksSubscribed;

	private readonly WebSocketClient _client;

	public PusherClient(int attemptsCount)
	{
		_client = new(
			"wss://api.bitexbook.com/api/v2/ws",
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
		var method = (string)obj.method;
		var data = (JToken)obj.data;

		switch (method)
		{
			case Methods.Welcome:
			{
				if (NewSymbols is { } handler)
					await handler(((JToken)obj.data.symbols).DeserializeObject<Symbol[]>(), cancellationToken);

				break;
			}

			case Methods.SymbolsLatestStatistic:
			{
				if (TickerChanged is { } handler)
				{
					foreach (var ticker in data.DeserializeObject<Ticker[]>())
						await handler(ticker, cancellationToken);
				}

				break;
			}

			case Methods.SymbolsChanges:
			{
				if (NewTickerChange is { } handler)
				{
					foreach (var ticker in data.DeserializeObject<IDictionary<string, TickerChange>>())
						await handler(ticker.Value, cancellationToken);
				}

				break;
			}

			case Methods.OrdersLatest:
			{
				if (LatestOrders is { } handler)
					await handler(data.DeserializeObject<Order[]>(), cancellationToken);

				break;
			}

			case Methods.TicketsActive:
			{
				if (obj.data.tickets == null)
					break;

				if (TicketsActive is { } handler)
				{
					var tickets = ((JToken)obj.data.tickets).DeserializeObject<IDictionary<string, Ticket[]>>();

					foreach (var pair in tickets)
						await handler(pair.Value, cancellationToken);
				}

				break;
			}

			case Methods.TicketAdded:
			{
				if (TicketAdded is { } handler)
					await handler(((JToken)obj).DeserializeObject<Ticket>(), cancellationToken);

				break;
			}

			case Methods.TicketCanceled:
			{
				if (TicketCanceled is { } handler)
					await handler(((JToken)obj).DeserializeObject<Ticket>(), cancellationToken);

				break;
			}

			case Methods.TicketExecuted:
			{
				if (TicketExecuted is { } handler)
					await handler(((JToken)obj).DeserializeObject<Ticket>(), cancellationToken);

				break;
			}

			default:
				this.AddErrorLog(LocalizedStrings.UnknownEvent, method);
				break;
		}
	}

	private static class Commands
	{
		public const string Subscribe = "subscribe";
		public const string Unsubscribe = "unsubscribe";
	}

	private static class Methods
	{
		public const string Welcome = "welcome";
		public const string SymbolsLatestStatistic = "symbols_latest_statistic";
		public const string TicketsActive = "tickets_active";
		public const string TicketAdded = "ticket_added";
		public const string TicketCanceled = "ticket_canceled";
		public const string TicketExecuted = "ticket_executed";
		public const string SymbolsChanges = "symbols_changes";
		public const string OrdersLatest = "orders_latest";
	}

	public ValueTask SubscribeTicker(long transId, string symbol, CancellationToken cancellationToken)
	{
		return Process(transId, Commands.Subscribe, symbol, cancellationToken);
	}

	public ValueTask UnSubscribeTicker(long originTransId, string symbol, CancellationToken cancellationToken)
	{
		return Process(-originTransId, Commands.Unsubscribe, symbol, cancellationToken);
	}

	public void SubscribeTrades(string symbol, CancellationToken cancellationToken)
	{
		//Process(Commands.Subscribe, Channels.Deals.Put(symbol));
	}

	public void UnSubscribeTrades(string symbol, CancellationToken cancellationToken)
	{
		//Process(Commands.Unsubscribe, Channels.Deals.Put(symbol));
	}

	public void SubscribeOrderBook(string symbol, CancellationToken cancellationToken)
	{
		//Process(Commands.Subscribe, Channels.OrderBook.Put(symbol));
	}

	public void UnSubscribeOrderBook(string symbol, CancellationToken cancellationToken)
	{
		//Process(Commands.Unsubscribe, Channels.OrderBook.Put(symbol));
	}

	public void SubscribeCandles(string symbol, string timeFrame, CancellationToken cancellationToken)
	{
		//Process(Commands.Subscribe, Channels.Candles.Put(symbol, timeFrame));
	}

	public void UnSubscribeCandles(string symbol, string timeFrame, CancellationToken cancellationToken)
	{
		//Process(Commands.Unsubscribe, Channels.Candles.Put(symbol, timeFrame));
	}

	private ValueTask Process(long subId, string method, string channel, CancellationToken cancellationToken)
	{
		if (method.IsEmpty())
			throw new ArgumentNullException(nameof(method));

		if (channel.IsEmpty())
			throw new ArgumentNullException(nameof(channel));

		return _client.SendAsync(new
		{
			method,
			data = new { channel },
		}, cancellationToken, subId);
	}
}