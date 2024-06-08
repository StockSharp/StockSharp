namespace StockSharp.Bitexbook.Native;

using Newtonsoft.Json.Linq;

class PusherClient : BaseLogReceiver
{
	// to get readable name after obfuscation
	public override string Name => nameof(Bitexbook) + "_" + nameof(PusherClient);

	public event Action<IEnumerable<Symbol>> NewSymbols;
	public event Action<TickerChange> NewTickerChange;
	public event Action<Ticker> TickerChanged;
	public event Action<IEnumerable<Order>> LatestOrders;
	public event Action<IEnumerable<Ticket>> TicketsActive;
	public event Action<Ticket> TicketAdded;
	public event Action<Ticket> TicketCanceled;
	public event Action<Ticket> TicketExecuted;
	public event Action<Exception> Error;
	public event Action Connected;
	public event Action<bool> Disconnected;
	//public event Action<string> TradesSubscribed;
	//public event Action<string> OrderBooksSubscribed;

	private readonly WebSocketClient _client;

	public PusherClient()
	{
		_client = new WebSocketClient(
			() =>
			{
				this.AddInfoLog(LocalizedStrings.Connected);
				Connected?.Invoke();
			},
			expected =>
			{
				if (expected)
					this.AddInfoLog(LocalizedStrings.Disconnected);
				else
					this.AddErrorLog(LocalizedStrings.ErrorConnection);

				Disconnected?.Invoke(expected);
			},
			error =>
			{
				this.AddErrorLog(error);
				Error?.Invoke(error);
			},
			OnProcess,
			(s, a) => this.AddInfoLog(s, a),
			(s, a) => this.AddErrorLog(s, a),
			(s, a) => this.AddVerboseLog(s, a),
			(s) => this.AddVerboseLog(s));
	}

	protected override void DisposeManaged()
	{
		_client.Dispose();
		base.DisposeManaged();
	}

	public ValueTask Connect(CancellationToken cancellationToken)
	{
		this.AddInfoLog(LocalizedStrings.Connecting);
		return _client.ConnectAsync("wss://api.bitexbook.com/api/v2/ws", true, cancellationToken: cancellationToken);
	}

	public void Disconnect()
	{
		this.AddInfoLog(LocalizedStrings.Disconnecting);
		_client.Disconnect();
	}

	private void OnProcess(dynamic obj)
	{
		var method = (string)obj.method;
		var data = (JToken)obj.data;

		switch (method)
		{
			case Methods.Welcome:
			{
				NewSymbols?.Invoke(((JToken)obj.data.symbols).DeserializeObject<Symbol[]>());
				break;
			}

			case Methods.SymbolsLatestStatistic:
			{
				foreach (var ticker in data.DeserializeObject<Ticker[]>())
					TickerChanged?.Invoke(ticker);

				break;
			}

			case Methods.SymbolsChanges:
			{
				foreach (var ticker in data.DeserializeObject<IDictionary<string, TickerChange>>())
					NewTickerChange?.Invoke(ticker.Value);

				break;
			}

			case Methods.OrdersLatest:
			{
				LatestOrders?.Invoke(data.DeserializeObject<Order[]>());
				break;
			}

			case Methods.TicketsActive:
			{
				if (obj.data.tickets == null)
					break;

				var tickets = ((JToken)obj.data.tickets).DeserializeObject<IDictionary<string, Ticket[]>>();
				
				foreach (var pair in tickets)
				{
					TicketsActive?.Invoke(pair.Value);
				}

				break;
			}

			case Methods.TicketAdded:
			{
				TicketAdded?.Invoke(((JToken)obj).DeserializeObject<Ticket>());
				break;
			}

			case Methods.TicketCanceled:
			{
				TicketCanceled?.Invoke(((JToken)obj).DeserializeObject<Ticket>());
				break;
			}

			case Methods.TicketExecuted:
			{
				TicketExecuted?.Invoke(((JToken)obj).DeserializeObject<Ticket>());
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

	public ValueTask SubscribeTicker(string symbol, CancellationToken cancellationToken)
	{
		return Process(Commands.Subscribe, symbol, cancellationToken);
	}

	public ValueTask UnSubscribeTicker(string symbol, CancellationToken cancellationToken)
	{
		return Process(Commands.Unsubscribe, symbol, cancellationToken);
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

	private ValueTask Process(string method, string channel, CancellationToken cancellationToken)
	{
		if (method.IsEmpty())
			throw new ArgumentNullException(nameof(method));

		if (channel.IsEmpty())
			throw new ArgumentNullException(nameof(channel));

		return _client.SendAsync(new
		{
			method,
			data = new { channel },
		}, cancellationToken);
	}
}