namespace StockSharp.FTX.Native;

using System.Web;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

/// <summary>
/// REST API Client of <see cref="FTX"/> adapter
/// </summary>
internal class FtxRestClient : BaseLogReceiver
{
	private readonly SecureString _key;
	private readonly HMACSHA256 _hasher;

	public FtxRestClient(SecureString key, SecureString secret)
	{
		_key = key;
		_hasher = secret.IsEmpty() ? null : new(secret.UnSecure().UTF8());
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	// to get readable name after obfuscation
	public override string Name => nameof(FTX) + "_" + nameof(HttpClient);

	/// <summary>
	/// Get markets API request
	/// </summary>
	/// <returns>API request</returns>
	public List<Market> GetMarkets()
	{
		return ProcessRequest<List<Market>>(Method.Get, "api/markets");
	}

	/// <summary>
	/// Get trades API request
	/// </summary>
	/// <param name="currency">Currency</param>
	/// <param name="start">Start <see cref="DateTime"/></param>
	/// <param name="end">End <see cref="DateTime"/></param>
	/// <returns></returns>
	public List<Trade> GetMarketTrades(string currency, DateTime start, DateTime end)
	{
		return ProcessRequest<List<Trade>>(Method.Get, $"api/markets/{currency}/trades?start_time={GetSecondsFromEpochStart(start)}&end_time={GetSecondsFromEpochStart(end)}");
	}

	/// <summary>
	/// Get candles API request
	/// </summary>
	/// <param name="currency">Currency</param>
	/// <param name="resolution">Time frame</param>
	/// <param name="start">Start <see cref="DateTime"/></param>
	/// <param name="end">End <see cref="DateTime"/></param>
	/// <returns></returns>
	public List<Candle> GetMarketCandles(string currency, TimeSpan resolution, DateTime start, DateTime end)
	{
		return ProcessRequest<List<Candle>>(Method.Get, $"api/markets/{currency}/candles?resolution={resolution.TotalSeconds}&start_time={GetSecondsFromEpochStart(start)}&end_time={GetSecondsFromEpochStart(end)}");
	}


	/// <summary>
	/// Get history orders API request
	/// </summary>
	/// <returns>History orders and flag if history has more data and require next request</returns>
	public (List<Order> histOrders, bool hasMoreData) GetMarketOrderHistoryAndHasMoreOrders(string subaccountName, DateTime startTime)
	{
		var response = ProcessSignedRequest<List<Order>, FtxRestResponseHasMoreData<List<Order>>>(Method.Get, $"api/orders/history?start_time={GetSecondsFromEpochStart(startTime)}", subaccountName);
		return (response.Result, response.HasMoreData);
	}

	/// <summary>
	/// Get balances API request
	/// </summary>
	/// <returns></returns>
	public List<Balance> GetBalances(string subaccountName)
	{
		return ProcessSignedRequest<List<Balance>>(Method.Get, "api/wallet/balances", subaccountName);
	}


	/// <summary>
	/// Get futures API request
	/// </summary>
	/// <returns></returns>
	public List<Futures> GetFuturesPositions(string subaccountName)
	{
		return ProcessSignedRequest<List<Futures>>(Method.Get, "api/positions", subaccountName);
	}

	/// <summary>
	/// Register order API request
	/// </summary>
	/// <param name="marketName"></param>
	/// <param name="side">Side</param>
	/// <param name="price">Price</param>
	/// <param name="orderType">Order type</param>
	/// <param name="amount">Amount</param>
	/// <param name="clientId">Client ID</param>
	/// <param name="subaccountName"></param>
	/// <returns></returns>
	public Order RegisterOrder(string marketName, Sides side, decimal? price, OrderTypes orderType, decimal amount, string clientId, string subaccountName)
	{
		var body =
			$"{{\"market\": \"{marketName}\"," +
			$"\"side\": \"{side.ToString().ToLower()}\"," +
			(price.HasValue ? $"\"price\": {price.Value}," : "\"price\": null,") +
			$"\"type\": \"{orderType.ToString().ToLower()}\"," +
			$"\"size\": {amount}," +
			$"\"clientId\": {(clientId.IsEmpty() ? "null" : $"\"{clientId}\"")}}}";
		return ProcessSignedRequest<Order>(Method.Post, "api/orders", subaccountName, body);
	}

	/// <summary>
	/// Cancel order API request
	/// </summary>
	/// <param name="id">Order ID</param>
	/// <param name="subaccountName"></param>
	/// <returns>Is order cancelled</returns>
	public bool CancelOrder(long id, string subaccountName)
	{
		var result = ProcessSignedRequest<object>(Method.Delete, $"api/orders/{id}", subaccountName);
		return result != null;
	}

	/// <summary>
	/// Cancel all orders API request
	/// </summary>
	/// <returns>Are orders cancelled</returns>
	public bool CancelAllOrders(string subaccountName)
	{
		var result = ProcessSignedRequest<object>(Method.Delete, "api/orders", subaccountName);
		return result != null;
	}

	/// <summary>
	/// Get open orders API request
	/// </summary>
	/// <returns>Opened orders</returns>
	public List<Order> GetOpenOrders(string subaccountName)
	{
		return ProcessSignedRequest<List<Order>>(Method.Get, "api/orders", subaccountName);
	}

	/// <summary>
	/// Get fills API request
	/// </summary>
	/// <param name="start">Start <see cref="DateTime"/></param>
	/// <param name="end">End <see cref="DateTime"/></param>
	/// <param name="subaccountName"></param>
	/// <returns></returns>
	public List<Fill> GetFills(DateTime start, DateTime end, string subaccountName)
	{
		return ProcessSignedRequest<List<Fill>>(Method.Get, $"api/fills?start_time={GetSecondsFromEpochStart(start)}&end_time={GetSecondsFromEpochStart(end)}", subaccountName);
	}

	#region Util
	private static long GetMillisecondsFromEpochStart()
	{
		return GetMillisecondsFromEpochStart(DateTime.UtcNow);
	}

	private static long GetMillisecondsFromEpochStart(DateTime time)
	{
		if(time <= TimeHelper.GregorianStart)
			return 0;

		return (long)time.ToUnix(false);
	}

	private static long GetSecondsFromEpochStart(DateTime time)
	{
		if(time <= TimeHelper.GregorianStart)
			return 0;

		return (long)time.ToUnix();
	}

	private Uri GetUri(string endpoint)
	{
		return new Uri($"https://ftx.com/{endpoint}");
	}

	private dynamic ProcessRequest(Method method, string endpoint, string jsonBody = null)
	{
		var request = new RestRequest((string)null, method);
		if (!jsonBody.IsEmpty())
		{
			request.AddParameter("json", jsonBody, ParameterType.RequestBody);
		}
		return request.Invoke(GetUri(endpoint), this, this.AddVerboseLog);
	}

	private T ProcessRequest<T>(Method method, string endpoint, string jsonBody = null) where T : class
	{
		dynamic response = ProcessRequest(method, endpoint, jsonBody);
		FtxRestResponse<T> restResponse = Parse<FtxRestResponse<T>>(response);

		if (restResponse == null) return null;
		if (restResponse.Success)
		{
			return restResponse.Result;
		}
		return null;
	}

	private dynamic ProcessSignedRequest(Method method, string endpoint, string subaccountName, string jsonBody = null)
	{
		long nonce = GetMillisecondsFromEpochStart();
		var request = new RestRequest((string)null, method);
		string signature = $"{nonce}{method.ToString().ToUpper()}/{endpoint}";
		if (!jsonBody.IsEmpty())
		{
			request.AddParameter("application/json; charset=utf-8", jsonBody, ParameterType.RequestBody);
			signature += jsonBody;
		}
		var hash = _hasher.ComputeHash(signature.UTF8());
		var hashStringBase64 = BitConverter.ToString(hash).Replace("-", string.Empty);
		string sign = hashStringBase64.ToLower();
		request.AddHeader("FTX-KEY", _key.UnSecure());
		request.AddHeader("FTX-SIGN", sign);
		request.AddHeader("FTX-TS", nonce.ToString());

		if (!subaccountName.IsEmpty())
		{
			request.AddHeader("FTX-SUBACCOUNT", HttpUtility.UrlEncode(subaccountName));
		}

		return request.Invoke(GetUri(endpoint), this, this.AddVerboseLog);
	}

	private T ProcessSignedRequest<T>(Method method, string endpoint, string subaccountName, string jsonBody = null) where T : class
	{
		dynamic response = ProcessSignedRequest(method, endpoint, subaccountName, jsonBody);
		FtxRestResponse<T> restResponse = Parse<FtxRestResponse<T>>(response);
		if (restResponse == null) return null;

		if (restResponse.Success)
		{
			return restResponse.Result;
		}
		return null;
	}

	private T1 ProcessSignedRequest<T, T1>(Method method, string endpoint, string subaccountName, string jsonBody = null) where T : class where T1 : FtxRestResponse<T>
	{
		dynamic response = ProcessSignedRequest(method, endpoint, subaccountName, jsonBody);
		T1 restResponse = Parse<T1>(response);

		if (restResponse == null) return null;
		if (restResponse.Success)
		{
			return restResponse;
		}
		return null;
	}
	private T Parse<T>(dynamic obj)
	{
		if (((JToken)obj).Type == JTokenType.Object && obj.status == "error")
			throw new InvalidOperationException((string)obj.reason.ToString());
		return ((JToken)obj).DeserializeObject<T>();
	}
	#endregion
}
