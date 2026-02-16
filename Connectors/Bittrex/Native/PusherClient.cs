namespace StockSharp.Bittrex.Native;

using Ecng.IO.Compression;

using Microsoft.AspNet.SignalR.Client;

class PusherClient : BaseLogReceiver
{
	private enum Channels
	{
		Summary = 'u' + ('S' << 8), // uS
		LiteSummary = 'u' + ('L' << 8), // uL
		Market = 'u' + ('E' << 8), // uE
		Balance = 'u' + ('B' << 8), // uB
		Order = 'u' + ('O' << 8) // uO
	}

	// to get readable name after obfuscation
	public override string Name => nameof(Bittrex) + "_" + nameof(PusherClient);

	public event Func<WsTicker, CancellationToken, ValueTask> TickerChanged;
	public event Func<string, WsFill, CancellationToken, ValueTask> NewTrade;
	public event Func<WsOrderBook, CancellationToken, ValueTask> OrderBookChanged;
	public event Func<WsBalance, CancellationToken, ValueTask> BalanceChanged;
	public event Func<int, WsOrder, CancellationToken, ValueTask> OrderChanged;
	public event Func<Exception, CancellationToken, ValueTask> Error;
	public event Func<CancellationToken, ValueTask> Connected;
	public event Func<bool, CancellationToken, ValueTask> Disconnected;
	//public event Action<string> TradesSubscribed;
	//public event Action<string> OrderBooksSubscribed;

	private readonly Authenticator _authenticator;
	private readonly bool _canSign;

	private HubConnection _connection;
	private IHubProxy _hub;

	public PusherClient(Authenticator authenticator, bool canSign)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
		_canSign = canSign;
	}

	public async ValueTask ConnectAsync(CancellationToken cancellationToken)
	{
		_connection = new HubConnection("https://socket.bittrex.com/signalr");
		_hub = _connection.CreateHubProxy("c2");

		_hub.On<string>("uS", tzip => Invoke(Channels.Summary, tzip));
		_hub.On<string>("uL", tzip => Invoke(Channels.LiteSummary, tzip));
		_hub.On<string>("uE", tzip => Invoke(Channels.Market, tzip));
		_hub.On<string>("uO", tzip => Invoke(Channels.Order, tzip));
		_hub.On<string>("uB", tzip => Invoke(Channels.Balance, tzip));

		await _connection.Start();

		if (_canSign)
		{
			var auth = await GetAuthContextAsync(_authenticator.Key.UnSecure());

			var sign = _authenticator.MakeSign(auth);
			var isAuthenticated = await AuthenticateAsync(_authenticator.Key.UnSecure(), sign);

			if (!isAuthenticated)
				throw new UnauthorizedAccessException();
		}

		if (Connected is { } handler)
			await handler(cancellationToken);
	}

	public async ValueTask DisconnectAsync(CancellationToken cancellationToken)
	{
		_connection.Stop(TimeSpan.FromSeconds(2));
		_connection.Dispose();
		_connection = null;
		_hub = null;

		if (Disconnected is { } handler)
			await handler(true, cancellationToken);
	}

	private async void Invoke(Channels channel, string tzip)
	{
		try
		{
			var decoded = Decode(tzip);

			this.AddVerboseLog(decoded);

			switch (channel)
			{
				case Channels.Summary:
				{
					var summary = decoded.DeserializeObject<WsMarketSummary>();

					if (summary.Tickers != null)
					{
						foreach (var ticker in summary.Tickers)
						{
							if (TickerChanged is { } handler)
								await handler(ticker, default);
						}
					}

					break;
				}
				case Channels.LiteSummary:
					break;
				case Channels.Market:
				{
					var book = decoded.DeserializeObject<WsOrderBook>();

					if (OrderBookChanged is { } bookHandler)
						await bookHandler(book, default);

					if (book.Fills != null)
					{
						foreach (var fill in book.Fills)
						{
							if (NewTrade is { } tradeHandler)
								await tradeHandler(book.Market, fill, default);
						}
					}

					break;
				}
				case Channels.Balance:
				{
					dynamic payload = decoded.DeserializeObject<object>();

					if (BalanceChanged is { } handler)
						await handler(((JToken)payload.d).DeserializeObject<WsBalance>(), default);

					break;
				}
				case Channels.Order:
				{
					dynamic payload = decoded.DeserializeObject<object>();

					if (OrderChanged is { } handler)
						await handler((int)payload.TY, ((JToken)payload.o).DeserializeObject<WsOrder>(), default);

					break;
				}
				default:
					throw new ArgumentOutOfRangeException(LocalizedStrings.UnknownEvent.Put(channel));
			}
		}
		catch (Exception ex)
		{
			if (Error is { } handler)
				await handler(ex, default);
		}
	}

	private static string Decode(string tzip)
	{
		return tzip.Base64().UnDeflate();
	}

	public Task<bool> SubscribeToSummaryDeltasAsync()
		=> InvokeAsync<bool>("SubscribeToSummaryDeltas");

	public Task<bool> SubscribeToExchangeDeltasAsync(string market)
		=> InvokeAsync<bool>("SubscribeToExchangeDeltas", market);

	public async Task<string> QuerySummaryStateAsync()
	{
		var result = await InvokeAsync<string>("QuerySummaryState");
		return Decode(result);
	}

	public async Task<WsOrderBook> QueryExchangeStateAsync(string market)
	{
		var result = await InvokeAsync<string>("QueryExchangeState", market);
		return Decode(result).DeserializeObject<WsOrderBook>();
	}

	private Task<string> GetAuthContextAsync(string apiKey)
		=> InvokeAsync<string>("GetAuthContext", apiKey);

	private Task<bool> AuthenticateAsync(string apiKey, string signedChallenge)
		=> InvokeAsync<bool>("Authenticate", apiKey, signedChallenge);

	private Task<T> InvokeAsync<T>(string methodName, params object[] args)
		=> _hub.Invoke<T>(methodName, args);

	protected override void DisposeManaged()
	{
		if (_connection != null)
			DisconnectAsync(default).AsTask().Wait();

		base.DisposeManaged();
	}
}
