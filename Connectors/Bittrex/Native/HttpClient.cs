namespace StockSharp.Bittrex.Native;

class HttpClient : BaseLogReceiver
{
	private readonly Authenticator _authenticator;

	private readonly UTCIncrementalIdGenerator _nonceGen;

	public HttpClient(Authenticator authenticator)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
		_nonceGen = new UTCIncrementalIdGenerator();
	}

	protected override void DisposeManaged()
	{
		_authenticator?.Dispose();
		base.DisposeManaged();
	}

	// to get readable name after obfuscation
	public override string Name => nameof(Bittrex) + "_" + nameof(HttpClient);

	public Task<IEnumerable<BittrexCurrency>> GetCurrenciesAsync(CancellationToken cancellationToken)
		=> MakeRequestAsync<IEnumerable<BittrexCurrency>>(CreateUrl("public/getcurrencies"), CreateRequest(), cancellationToken);

	public Task<IEnumerable<Market>> GetMarketsAsync(CancellationToken cancellationToken)
		=> MakeRequestAsync<IEnumerable<Market>>(CreateUrl("public/getmarkets"), CreateRequest(), cancellationToken);

	public Task<OrderBook> GetOrderBookAsync(string market, string type, CancellationToken cancellationToken)
	{
		var request = CreateRequest();

		request
			.AddParameter("market", market)
			.AddParameter("type", type);

		return MakeRequestAsync<OrderBook>(CreateUrl("public/getorderbook"), request, cancellationToken);
	}

	public async Task<IEnumerable<MarketSummary>> GetMarketSummariesAsync(CancellationToken cancellationToken)
		=> await MakeRequestAsync<IEnumerable<MarketSummary>>(CreateUrl("public/getmarketsummaries"), CreateRequest(), cancellationToken) ?? [];

	public async Task<IEnumerable<Trade>> GetMarketHistoryAsync(string market, CancellationToken cancellationToken)
		=> await MakeRequestAsync<IEnumerable<Trade>>(CreateUrl("public/getmarkethistory"), CreateRequest().AddParameter("market", market), cancellationToken) ?? [];

	public async Task<IEnumerable<Candle>> GetCandlesAsync(string market, string tickInterval, long? timeStamp, CancellationToken cancellationToken)
	{
		var request = CreateRequest();

		request
			.AddParameter("marketName", market)
			.AddParameter("tickInterval", tickInterval);

		if (timeStamp != null)
			request.AddParameter("_", timeStamp.Value);

		return await MakeRequestAsync<IEnumerable<Candle>>(CreateUrl("pub/market/GetTicks", "v2.0/"), request, cancellationToken) ?? [];
	}

	public async Task<IEnumerable<Balance>> GetBalancesAsync(CancellationToken cancellationToken)
	{
		var url = CreateUrl("account/getbalances");
		return await MakeRequestAsync<IEnumerable<Balance>>(url, ApplySecret(CreateRequest(), url), cancellationToken) ?? [];
	}

	public async Task<IEnumerable<Order>> GetOpenOrdersAsync(string market, CancellationToken cancellationToken)
	{
		var request = CreateRequest();

		if (market != null)
			request.AddParameter("market", market);

		var url = CreateUrl("market/getopenorders");
		return await MakeRequestAsync<IEnumerable<Order>>(url, ApplySecret(request, url), cancellationToken) ?? [];
	}

	public Task<Order> GetOrderAsync(string uuid, CancellationToken cancellationToken)
	{
		var request = CreateRequest();

		request.AddParameter("uuid", uuid);

		var url = CreateUrl("account/getorder");
		return MakeRequestAsync<Order>(url, ApplySecret(request, url), cancellationToken);
	}

	public async Task<string> RegisterOrderAsync(string market, Sides side, decimal price, decimal volume, CancellationToken cancellationToken)
	{
		var request =
			CreateRequest()
				.AddParameter("market", market)
				.AddParameter("quantity", volume)
				.AddParameter("rate", price);

		var url = CreateUrl($"market/{side.To<string>().ToLowerInvariant()}limit");
		var response = await MakeRequestAsync<OrderResponse>(url, ApplySecret(request, url), cancellationToken);
		return response.Uuid;
	}

	public Task CancelOrderAsync(string orderId, CancellationToken cancellationToken)
	{
		var url = CreateUrl("market/cancel");
		return MakeRequestAsync(url, ApplySecret(CreateRequest().AddParameter("uuid", orderId), url), cancellationToken);
	}

	public async Task<string> WithdrawAsync(string currency, decimal volume, WithdrawInfo info, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		switch (info.Type)
		{
			case WithdrawTypes.Crypto:
			{
				var url = CreateUrl("account/withdraw");

				var request = CreateRequest()
						.AddParameter("currency", currency)
						.AddParameter("quantity", volume)
						.AddParameter("address", info.CryptoAddress);

				if (!info.PaymentId.IsEmpty())
					request.AddParameter("paymentid", info.PaymentId);

				var response = await MakeRequestAsync<OrderResponse>(url, ApplySecret(request, url), cancellationToken);
				return response.Uuid;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));
		}
	}

	private static Url CreateUrl(string methodName, string version = "v1.1/")
	{
		if (methodName.IsEmpty())
			throw new ArgumentNullException(nameof(methodName));

		return new Url($"https://bittrex.com/api/{version}{methodName}");
	}

	private static RestRequest CreateRequest()
	{
		return new RestRequest((string)null, Method.Get);
	}

	private RestRequest ApplySecret(RestRequest request, Url url)
	{
		if (request == null)
			throw new ArgumentNullException(nameof(request));

		request
			.AddParameter("apikey", _authenticator.Key.UnSecure())
			.AddParameter("nonce", _nonceGen.GetNextId());

		var qs = request
			.Parameters
			.Where(p => p.Type == ParameterType.GetOrPost && p.Value != null)
			.ToQueryString();

		var signature = _authenticator.MakeSign(url + "?" + qs);
	
		request
			.AddHeader("apisign", signature);

		return request;
	}

	private async Task<T> MakeRequestAsync<T>(Uri url, RestRequest request, CancellationToken cancellationToken)
	{
		var token = await MakeRequestAsync(url, request, cancellationToken);
		return token.ToObject<T>();
	}

	private async Task<JToken> MakeRequestAsync(Uri url, RestRequest request, CancellationToken cancellationToken)
	{
		var bittrexResponse = await request.InvokeAsync<BittrexResponse>(url, this, this.AddVerboseLog, cancellationToken);

		if (bittrexResponse.Success)
			return bittrexResponse.Result;

		throw new InvalidOperationException(bittrexResponse.Message);
	}
}