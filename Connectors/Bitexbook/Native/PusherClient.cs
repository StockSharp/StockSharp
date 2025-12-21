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
			(state, ct) => StateChanged?.Invoke(state, ct) ?? default,
			(error, ct) =>
			{
				this.AddErrorLog(error);
				return Error?.Invoke(error, ct) ?? default;
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
				var handler = NewSymbols;
				if (handler != null)
					await handler(((JToken)obj.data.symbols).DeserializeObject<Symbol[]>(), cancellationToken);
				break;
			}

			case Methods.SymbolsLatestStatistic:
			{
				var handler = TickerChanged;
				if (handler != null)
				{
					foreach (var ticker in data.DeserializeObject<Ticker[]>())
						await handler(ticker, cancellationToken);
				}

				break;
			}

			case Methods.SymbolsChanges:
			{
				var handler = NewTickerChange;
				if (handler != null)
				{
					foreach (var ticker in data.DeserializeObject<IDictionary<string, TickerChange>>())
						await handler(ticker.Value, cancellationToken);
				}

				break;
			}

			case Methods.OrdersLatest:
			{
				var handler = LatestOrders;
				if (handler != null)
					await handler(data.DeserializeObject<Order[]>(), cancellationToken);
				break;
			}

			case Methods.TicketsActive:
			{
				if (obj.data.tickets == null)
					break;

				var handler = TicketsActive;
				if (handler != null)
				{
					var tickets = ((JToken)obj.data.tickets).DeserializeObject<IDictionary<string, Ticket[]>>();

					foreach (var pair in tickets)
					{
						await handler(pair.Value, cancellationToken);
					}
				}

				break;
			}

			case Methods.TicketAdded:
			{
				var handler = TicketAdded;
				if (handler != null)
					await handler(((JToken)obj).DeserializeObject<Ticket>(), cancellationToken);
				break;
			}

			case Methods.TicketCanceled:
			{
				var handler = TicketCanceled;
				if (handler != null)
					await handler(((JToken)obj).DeserializeObject<Ticket>(), cancellationToken);
				break;
			}

			case Methods.TicketExecuted:
			{
				var handler = TicketExecuted;
				if (handler != null)
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