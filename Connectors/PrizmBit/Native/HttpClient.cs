namespace StockSharp.PrizmBit.Native;

class HttpClient(Authenticator authenticator, bool isDemo) : BaseLogReceiver
{
	private readonly Authenticator _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
	private readonly bool _isDemo = isDemo;

	private const string _baseUrl = "https://api.prizmbit.com/api/po";

	// to get readable name after obfuscation
	public override string Name => nameof(PrizmBit) + "_" + nameof(HttpClient);

	public Task<IEnumerable<Symbol>> GetSymbolsAsync(CancellationToken cancellationToken)
	{
		return MakeRequestAsync<IEnumerable<Symbol>>(CreateUrl("MarketData/GetSymbols"), CreateRequest(Method.Get), cancellationToken);
	}

	public Task<IEnumerable<HttpCurrency>> GetCurrenciesAsync(CancellationToken cancellationToken)
	{
		return MakeRequestAsync<IEnumerable<HttpCurrency>>(CreateUrl("MarketData/GetCurrenciesInfo"), CreateRequest(Method.Get), cancellationToken);
	}

	public Task<IEnumerable<Trade>> GetTradesAsync(string symbol, long? idFrom, long? idTo, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Get);

		request
			.AddQueryParameter("MarketName", symbol)
			.AddQueryParameter("Desc", "false");

		if (idFrom != null)
			request.AddQueryParameter("idFrom", idFrom.Value.To<string>());

		if (idTo != null)
			request.AddQueryParameter("idTo", idTo.Value.To<string>());

		return MakeRequestAsync<IEnumerable<Trade>>(CreateUrl("MarketData/GetTrades"), request, cancellationToken);
	}

	public async Task<IEnumerable<Ohlc>> GetCandlesAsync(string symbol, string period, long from, long to, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Get);

		request
			.AddQueryParameter("MarketName", symbol)
			.AddQueryParameter("Period", period)
			.AddQueryParameter("From", from.To<string>())
			.AddQueryParameter("To", to.To<string>());

		dynamic response = await MakeRequestAsync<object>(CreateUrl("MarketData/GetChart"), request, cancellationToken);

		var o = response.o;
		var h = response.h;
		var l = response.l;
		var c = response.c;
		var v = response.v;
		var t = response.t;
		var cnt = response.cnt;

		var candles = new List<Ohlc>();

		var i = 0;

		foreach (var time in t)
		{
			candles.Add(new Ohlc
			{
				Time = time,

				Open = o[i],
				High = h[i],
				Low = l[i],
				Close = c[i],
				Volume = v[i],
				Count = cnt[i],
			});

			i++;
		}

		return candles;
	}

	//public OrderBook GetOrderBook(int marketId)
	//{
	//	var request = CreateRequest(Method.Get);

	//	request
	//		.AddQueryParameter("marketId", marketId.To<string>())
	//		.AddQueryParameter("Desc", "false");

	//	if (idFrom != null)
	//		request.AddQueryParameter("idFrom", idFrom.Value.To<string>());

	//	if (idTo != null)
	//		request.AddQueryParameter("idTo", idTo.Value.To<string>());

	//	return MakeRequest<IEnumerable<Trade>>(CreateUrl("MarketData/GetOrderBook"), request);
	//}

	public async Task<IEnumerable<Account>> GetAccountsAsync(CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		request.AddQueryParameter("data", null);

		dynamic response = await MakeRequestAsync<object>(CreateUrl("Account/GetUserBalances"), ApplySecret(request), cancellationToken);

		return ((JToken)response.userAccountList).DeserializeObject<IEnumerable<Account>>();
	}

	public Task<IEnumerable<Order>> GetOrdersAsync(CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		request
			.AddQueryParameter("All", "true")
			.AddQueryParameter("Limit", "1000")
			.AddQueryParameter("Offset", "0");

		return MakeRequestAsync<IEnumerable<Order>>(CreateUrl("Account/GetOpenOrders"), ApplySecret(request), cancellationToken);
	}

	public Task<IEnumerable<HttpUserTrade>> GetOwnTradesAsync(CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		request
			.AddQueryParameter("Desc", "true")
			.AddQueryParameter("Limit", "1000")
			.AddQueryParameter("Offset", "0");

		return MakeRequestAsync<IEnumerable<HttpUserTrade>>(CreateUrl("Account/GetUserTrades"), ApplySecret(request), cancellationToken);
	}

	public Task<Order> RegisterOrderAsync(string symbol, string clientId, string type, string side, string accountType,
		decimal price, decimal volume, decimal? trailingStopDistance, decimal? stopPrice, decimal? limitPrice, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		request
			.AddQueryParameter("Market", symbol)
			.AddQueryParameter("CliOrdId", clientId)
			.AddQueryParameter("OrderSide", side)
			.AddQueryParameter("OrderType", type)
			.AddQueryParameter("AccountType", accountType)
			.AddQueryParameter("Price", price.To<string>())
			.AddQueryParameter("Amount", volume.To<string>());

		if (trailingStopDistance != null)
			request.AddQueryParameter("TrailingStopDistance", trailingStopDistance.Value.To<string>());

		if (stopPrice != null)
			request.AddQueryParameter("StopPrice", stopPrice.Value.To<string>());

		if (limitPrice != null)
			request.AddQueryParameter("LimitPrice", limitPrice.Value.To<string>());

		request.AddQueryParameter("IncludeMatching", "true");

		return MakeRequestAsync<Order>(CreateUrl(_isDemo ? "Trade/TestOrder" : "Trade/CreateOrder"), ApplySecret(request), cancellationToken);
	}

	public Task<Order> CancelOrderAsync(long orderId, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		request.AddQueryParameter("OrderId", orderId.To<string>());

		return MakeRequestAsync<Order>(CreateUrl("Trade/CancelOrder"), ApplySecret(request), cancellationToken);
	}

	public Task CancelOrdersAsync(CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		return MakeRequestAsync<Order>(CreateUrl("Trade/CancelOrders"), ApplySecret(request), cancellationToken);
	}

	public Task<Order> ModifyOrderAsync(long orderId, decimal newPrice, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		request
			.AddQueryParameter("OrderId", orderId.To<string>())
			.AddQueryParameter("Price", newPrice.To<string>());

		return MakeRequestAsync<Order>(CreateUrl("Trade/CancelOrder"), ApplySecret(request), cancellationToken);
	}

	public async Task<long> WithdrawAsync(string currency, decimal volume, WithdrawInfo info, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.Type != WithdrawTypes.Crypto)
			throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));

		var request = CreateRequest(Method.Post);

		request
			.AddQueryParameter("Currency", currency)
			.AddQueryParameter("Amount", volume.To<string>())
			.AddQueryParameter("Address", info.CryptoAddress);

		if (!info.Comment.IsEmpty())
			request.AddQueryParameter("Details", info.Comment);

		if (!info.PaymentId.IsEmpty())
			request.AddQueryParameter("TagMessage", info.PaymentId);

		dynamic response = await MakeRequestAsync<object>(CreateUrl("Account/CreateWithdrawal"), ApplySecret(request), cancellationToken);

		return (long)response.id;
	}

	private static Uri CreateUrl(string methodName)
	{
		if (methodName.IsEmpty())
			throw new ArgumentNullException(nameof(methodName));

		return $"{_baseUrl}/{methodName}".To<Uri>();
	}

	private static RestRequest CreateRequest(Method method)
	{
		return new RestRequest((string)null, method);
	}

	private RestRequest ApplySecret(RestRequest request)
	{
		if (request == null)
			throw new ArgumentNullException(nameof(request));

		var encodedArgs = request
			.Parameters
			//.Where(p => p.Type == ParameterType.GetOrPost && p.Value != null)
			//.OrderBy(p => p.Name)
			.ToQueryString();

		var signature = _authenticator.MakeSign(encodedArgs);

		request
			.AddHeader("X-ClientId", _authenticator.Key.UnSecure())
			.AddHeader("X-Signature", signature)
			//.AddHeader("X-SignatureType", _authenticator.SignatureType)
			;

		return request;
	}

	private async Task<T> MakeRequestAsync<T>(Uri url, RestRequest request, CancellationToken cancellationToken)
	{
		dynamic response = await request.InvokeAsync<object>(url, this, this.AddVerboseLog, cancellationToken);

		if (response is JObject && response.error != null)
			throw new InvalidOperationException((string)response.error.ToString());

		return ((JToken)response).DeserializeObject<T>();
	}
}