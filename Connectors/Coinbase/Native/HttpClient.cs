namespace StockSharp.Coinbase.Native;

using System.Dynamic;

using Newtonsoft.Json.Linq;

class HttpClient : BaseLogReceiver
{
	private readonly Authenticator _authenticator;

	private const string _baseUrl = "https://api.coinbase.com/api";

	public HttpClient(Authenticator authenticator)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
	}

	// to get readable name after obfuscation
	public override string Name => nameof(Coinbase) + "_" + nameof(HttpClient);

	public async Task<IEnumerable<Product>> GetProducts(string type, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Get);
		request.AddParameter("product_type", type);

		dynamic response = await MakeRequest<object>(CreateUrl("brokerage/market/products"), request, cancellationToken);

		return ((JToken)response.products).DeserializeObject<IEnumerable<Product>>();
	}

	public async Task<Ohlc[]> GetCandles(string symbol, long start, long end, string granularity, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Get)
			.AddQueryParameter("start", start)
			.AddQueryParameter("end", end)
			.AddQueryParameter("granularity", granularity)
		;

		dynamic response = await MakeRequest<object>(CreateUrl($"brokerage/market/products/{symbol}/candles"), request, cancellationToken);

		return ((JToken)response.candles).DeserializeObject<Ohlc[]>();
	}

	public async Task<IEnumerable<Trade>> GetTrades(string symbol, long start, long end, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Get)
			.AddQueryParameter("start", start)
			.AddQueryParameter("end", end)
		;

		dynamic response = await MakeRequest<object>(CreateUrl($"brokerage/market/products/{symbol}/ticker"), request, cancellationToken);

		return ((JToken)response.trades).DeserializeObject<IEnumerable<Trade>>();
	}

	public Task<IEnumerable<Account>> GetAccounts(CancellationToken cancellationToken)
	{
		var url = CreateUrl("accounts");
		return MakeRequest<IEnumerable<Account>>(url, ApplySecret(CreateRequest(Method.Get), url), cancellationToken);
	}

	public async Task<IEnumerable<Order>> GetOrders(CancellationToken cancellationToken)
	{
		var url = CreateUrl("brokerage/orders/historical/batch");
		dynamic response = await MakeRequest<object>(url, ApplySecret(CreateRequest(Method.Get), url), cancellationToken);
		return ((JToken)response.orders).DeserializeObject<IEnumerable<Order>>();
	}

	public async Task<IEnumerable<Fill>> GetFills(string orderId, CancellationToken cancellationToken)
	{
		var url = CreateUrl("brokerage/orders/historical/fills");

		var request = CreateRequest(Method.Get);

		if (!orderId.IsEmpty())
			request.AddQueryParameter("order_id", orderId);

		dynamic response = await MakeRequest<object>(url, ApplySecret(request, url), cancellationToken);
		return ((JToken)response.fills).DeserializeObject<IEnumerable<Fill>>();
	}

	public Task<Order> RegisterOrder(string clientOrderId, string symbol, string type, string side, decimal? price, decimal? stopPrice, decimal volume, TimeInForce? timeInForce, DateTimeOffset? tillDate, int? leverage, CancellationToken cancellationToken)
	{
		var url = CreateUrl("brokerage/orders");

		var request = CreateRequest(Method.Post);

		var body = (dynamic)new ExpandoObject();

		body.client_order_id = clientOrderId;
		body.side = side;
		body.product_id = symbol;
		body.size = volume;

		if (!type.IsEmpty())
			body.type = type;

		if (leverage is int l)
			body.leverage = l;

		if (price != null)
			body.price = price.Value;

		if (timeInForce != null)
			body.time_in_force = timeInForce.ToNative(tillDate);

		if (tillDate != null)
			body.cancel_after = tillDate.Value.UtcDateTime;

		if (stopPrice != null)
		{
			body.stop = "loss";
			body.stop_price = stopPrice.Value;
		}

		return MakeRequest<Order>(url, ApplySecret(request, url, (object)body), cancellationToken);
	}

	public Task EditOrder(string orderId, decimal? price, decimal? size, CancellationToken cancellationToken)
	{
		var url = CreateUrl("brokerage/orders/edit");
		var request = CreateRequest(Method.Post);
		var body = new
		{
			order_id = orderId,
			price,
			size,
		};
		return MakeRequest<object>(url, ApplySecret(request, url, body), cancellationToken);
	}

	public Task CancelOrder(string orderId, CancellationToken cancellationToken)
	{
		var url = CreateUrl("brokerage/orders/batch_cancel");
		var request = CreateRequest(Method.Post);
		var body = new
		{
			order_ids = new[] { orderId }
		};
		return MakeRequest<object>(url, ApplySecret(request, url, body), cancellationToken);
	}

	public async Task<string> Withdraw(string currency, decimal volume, WithdrawInfo info, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		Uri url;
		var request = CreateRequest(Method.Post);

		var body = (dynamic)new ExpandoObject();

		body.currency = currency;
		body.amount = volume;

		switch (info.Type)
		{
			case WithdrawTypes.BankWire:
			{
				//if (info.BankDetails == null)
				//	throw new InvalidOperationException(LocalizedStrings.BankDetailsIsMissing);

				url = CreateUrl("withdrawals/payment-method");
				body.payment_method_id = info.PaymentId;
				break;
			}
			case WithdrawTypes.Crypto:
			{
				url = CreateUrl("withdrawals/crypto");
				body.crypto_address = info.CryptoAddress;
				break;
			}
			case WithdrawTypes.BankCard:
			{
				url = CreateUrl("withdrawals/coinbase-account");
				body.coinbase_account_id = info.CardNumber;
				break;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));
		}

		return (await MakeRequest<Withdraw>(url, ApplySecret(request, url, (object)body), cancellationToken)).Id;
	}

	private static Uri CreateUrl(string methodName, string version = "v3")
	{
		if (methodName.IsEmpty())
			throw new ArgumentNullException(nameof(methodName));

		return $"{_baseUrl}/{version}/{methodName}".To<Uri>();
	}

	private static RestRequest CreateRequest(Method method)
	{
		return new RestRequest((string)null, method);
	}

	private RestRequest ApplySecret(RestRequest request, Uri url, object body = null)
	{
		if (request == null)
			throw new ArgumentNullException(nameof(request));

		//var body = new JObject();
		var qs = request
			.Parameters
			.Where(p => p.Type == ParameterType.QueryString)
			.ToQueryString(false);

		var urlStr = url.ToString().Remove(_baseUrl);

		if (!qs.IsEmpty())
		{
			urlStr += "?" + qs;
		}

		var bodyStr = body == null ? string.Empty : JsonConvert.SerializeObject(body, _serializerSettings);

		var signature = _authenticator.MakeSign(urlStr, request.Method, bodyStr, out var timestamp);
	
		request
			.AddHeader("CB-ACCESS-KEY", _authenticator.Key.UnSecure())
			.AddHeader("CB-ACCESS-TIMESTAMP", timestamp)
			.AddHeader("CB-ACCESS-PASSPHRASE", _authenticator.Passphrase.UnSecure())
			.AddHeader("CB-ACCESS-SIGN", signature);

		if (body != null)
		{
			//request.RequestFormat = DataFormat.Json;
			request.AddBodyAsStr(bodyStr);
		}

		return request;
	}

	private static readonly JsonSerializerSettings _serializerSettings = JsonHelper.CreateJsonSerializerSettings();

	private async Task<T> MakeRequest<T>(Uri url, RestRequest request, CancellationToken cancellationToken)
		where T : class
	{
		dynamic obj = await request.InvokeAsync(url, this, this.AddVerboseLog, cancellationToken);

		if (((JToken)obj).Type == JTokenType.Object && (string)obj.type == "error")
			throw new InvalidOperationException((string)obj.message + " " + (string)obj.reason);

		return ((JToken)obj).DeserializeObject<T>();
	}
}