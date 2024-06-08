namespace StockSharp.Bitalong.Native;

using System.Security;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

class HttpClient : BaseLogReceiver
{
	private readonly SecureString _key;
	private readonly HashAlgorithm _hasher;

	private readonly string _baseUrl;

	public HttpClient(string domain, SecureString key, SecureString secret)
	{
		_baseUrl = $"https://www.{domain}/api/";
		_key = key;
		_hasher = secret.IsEmpty() ? null : new HMACSHA512(secret.UnSecure().UTF8());
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	// to get readable name after obfuscation
	public override string Name => nameof(Bitalong) + "_" + nameof(HttpClient);

	public async Task<IDictionary<string, Symbol>> GetSymbols(CancellationToken cancellationToken)
	{
		dynamic response = await MakeRequest<object>(CreateUrl("index/marketinfo"), CreateRequest(Method.Get), cancellationToken);

		return ((JToken)response.pairs).DeserializeObject<IDictionary<string, Symbol>>();
	}

	public Task<Ticker> GetTicker(string symbol, CancellationToken cancellationToken)
	{
		return MakeRequest<Ticker>(CreateUrl($"index/ticker/{symbol}"), CreateRequest(Method.Get), cancellationToken);
	}

	public Task<IDictionary<string, Ticker>> GetTickers(CancellationToken cancellationToken)
	{
		return MakeRequest<IDictionary<string, Ticker>>(CreateUrl("index/tickers"), CreateRequest(Method.Get), cancellationToken);
	}

	public Task<OrderBook> GetOrderBook(string symbol, CancellationToken cancellationToken)
	{
		return MakeRequest<OrderBook>(CreateUrl($"index/orderBook/{symbol}"), CreateRequest(Method.Get), cancellationToken);
	}

	public Task<IEnumerable<Trade>> GetTradeHistory(string symbol, CancellationToken cancellationToken)
	{
		return MakeRequest<IEnumerable<Trade>>(CreateUrl($"index/tradeHistory/{symbol}"), CreateRequest(Method.Get), cancellationToken);
	}

	public async Task<(IDictionary<string, double>, IDictionary<string, double>)> GetBalances(CancellationToken cancellationToken)
	{
		dynamic response = await MakeRequest<object>(CreateUrl("private/balances"), ApplySecret(CreateRequest(Method.Post), null), cancellationToken);

		return (((JToken)response.available).DeserializeObject<IDictionary<string, double>>(), ((JToken)response.locked).DeserializeObject<IDictionary<string, double>>());
	}

	public async Task<IEnumerable<Order>> GetOrders(CancellationToken cancellationToken)
	{
		dynamic response = await MakeRequest<object>(CreateUrl("private/openOrders"), ApplySecret(CreateRequest(Method.Post), null), cancellationToken);

		return ((JToken)response.orders).DeserializeObject<IEnumerable<Order>>();
	}

	public async Task<Order> GetOrderInfo(string symbol, long orderId, CancellationToken cancellationToken)
	{
		dynamic response = await MakeRequest<object>(CreateUrl("private/getOrder"), ApplySecret(CreateRequest(Method.Post), new
		{
			currencyPair = symbol,
			orderNumber = orderId.To<string>(),
		}), cancellationToken);

		return ((JToken)response.orders).DeserializeObject<Order>();
	}

	public async Task<long> RegisterOrder(string symbol, string side, decimal price, decimal volume, CancellationToken cancellationToken)
	{
		dynamic response = await MakeRequest<object>(CreateUrl($"private/{side}"), ApplySecret(CreateRequest(Method.Post), new
		{
			currencyPair = symbol,
			rate = price.To<string>(),
			amount = volume.To<string>(),
		}), cancellationToken);

		return (long)response.orderNumber;
	}

	public Task CancelOrder(string symbol, long orderId, CancellationToken cancellationToken)
	{
		return MakeRequest<object>(CreateUrl("private/cancelOrder"), ApplySecret(CreateRequest(Method.Post), new
		{
			currencyPair = symbol,
			orderNumber = orderId.To<string>(),
		}), cancellationToken);
	}

	public Task CancelAllOrders(string symbol, Sides? side, CancellationToken cancellationToken)
	{
		return MakeRequest<object>(CreateUrl("private/cancelAllOrders"), ApplySecret(CreateRequest(Method.Post), new
		{
			currencyPair = symbol,
			type = side == null ? -1 : (side.Value == Sides.Buy ? 1 : 0)
		}), cancellationToken);
	}

	public Task Withdraw(string currency, decimal volume, WithdrawInfo info, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.Type != WithdrawTypes.Crypto)
			throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));

		return MakeRequest<object>(CreateUrl("private/withdraw"), ApplySecret(CreateRequest(Method.Post), new
		{
			currency,
			address = info.CryptoAddress,
			amount = volume.To<string>(),
		}), cancellationToken);
	}

	private Uri CreateUrl(string methodName)
	{
		if (methodName.IsEmpty())
			throw new ArgumentNullException(nameof(methodName));

		return $"{_baseUrl}/{methodName}".To<Uri>();
	}

	private static RestRequest CreateRequest(Method method)
	{
		return new RestRequest((string)null, method);
	}

	private static readonly JsonSerializerSettings _serializerSettings = JsonHelper.CreateJsonSerializerSettings();

	private RestRequest ApplySecret(RestRequest request, object body)
	{
		if (request == null)
			throw new ArgumentNullException(nameof(request));

		var bodyStr = string.Empty;

		if (body != null)
		{
			bodyStr = JsonConvert.SerializeObject(body, _serializerSettings);
			request.AddBodyAsStr(bodyStr);
		}

		var signature = _hasher
		    .ComputeHash(bodyStr.UTF8())
		    .Digest()
		    .ToLowerInvariant();

		request
			.AddHeader("KEY", _key.UnSecure())
			.AddHeader("SIGN", signature);

		return request;
	}

	private async Task<T> MakeRequest<T>(Uri url, RestRequest request, CancellationToken cancellationToken)
	{
		dynamic obj = await request.InvokeAsync(url, this, this.AddVerboseLog, cancellationToken);

		if (obj is JObject)
		{
			if ((bool?)obj.result == false)
				throw new InvalidOperationException((string)obj.message);

			if (obj.data != null)
				obj = obj.data;
		}

		return ((JToken)obj).DeserializeObject<T>();
	}
}