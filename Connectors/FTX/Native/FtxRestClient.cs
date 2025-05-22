namespace StockSharp.FTX.Native;

using System.Net.Http;
using System.Security;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

class FtxRestClient : BaseLogReceiver
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

	public Task<List<Market>> GetMarkets(CancellationToken cancellationToken)
	{
		return ProcessRequest<List<Market>>(Method.Get, "api/markets", default, cancellationToken);
	}

	public Task<List<Trade>> GetMarketTrades(string currency, DateTime start, DateTime end, CancellationToken cancellationToken)
	{
		return ProcessRequest<List<Trade>>(Method.Get, $"api/markets/{currency}/trades?start_time={GetSecondsFromEpochStart(start)}&end_time={GetSecondsFromEpochStart(end)}", default, cancellationToken);
	}

	public Task<List<Candle>> GetMarketCandles(string currency, TimeSpan resolution, DateTime start, DateTime end, CancellationToken cancellationToken)
	{
		return ProcessRequest<List<Candle>>(Method.Get, $"api/markets/{currency}/candles?resolution={resolution.TotalSeconds}&start_time={GetSecondsFromEpochStart(start)}&end_time={GetSecondsFromEpochStart(end)}", default, cancellationToken);
	}

	public async Task<(List<Order> histOrders, bool hasMoreData)> GetMarketOrderHistoryAndHasMoreOrders(string subaccountName, DateTime startTime, CancellationToken cancellationToken)
	{
		var response = await ProcessSignedRequest<List<Order>, FtxRestResponseHasMoreData<List<Order>>>(Method.Get, $"api/orders/history?start_time={GetSecondsFromEpochStart(startTime)}", subaccountName, default, cancellationToken);
		return (response.Result, response.HasMoreData);
	}

	public Task<List<Balance>> GetBalances(string subaccountName, CancellationToken cancellationToken)
	{
		return ProcessSignedRequest<List<Balance>>(Method.Get, "api/wallet/balances", subaccountName, default, cancellationToken);
	}

	public Task<List<Futures>> GetFuturesPositions(string subaccountName, CancellationToken cancellationToken)
	{
		return ProcessSignedRequest<List<Futures>>(Method.Get, "api/positions", subaccountName, default, cancellationToken);
	}

	public Task<Order> RegisterOrder(string marketName, Sides side, decimal? price, OrderTypes orderType, decimal amount, string clientId, string subaccountName, CancellationToken cancellationToken)
	{
		var body =
			$"{{\"market\": \"{marketName}\"," +
			$"\"side\": \"{side.ToString().ToLower()}\"," +
			(price.HasValue ? $"\"price\": {price.Value}," : "\"price\": null,") +
			$"\"type\": \"{orderType.ToString().ToLower()}\"," +
			$"\"size\": {amount}," +
			$"\"clientId\": {(clientId.IsEmpty() ? "null" : $"\"{clientId}\"")}}}";
		return ProcessSignedRequest<Order>(Method.Post, "api/orders", subaccountName, body, cancellationToken);
	}

	public async Task<bool> CancelOrder(long id, string subaccountName, CancellationToken cancellationToken)
	{
		var result = await ProcessSignedRequest<object>(Method.Delete, $"api/orders/{id}", subaccountName, default, cancellationToken);
		return result != null;
	}

	public async Task<bool> CancelAllOrders(string subaccountName, CancellationToken cancellationToken)
	{
		var result = await ProcessSignedRequest<object>(Method.Delete, "api/orders", subaccountName, default, cancellationToken);
		return result != null;
	}

	public Task<List<Order>> GetOpenOrders(string subaccountName, CancellationToken cancellationToken)
	{
		return ProcessSignedRequest<List<Order>>(Method.Get, "api/orders", subaccountName, default, cancellationToken);
	}

	public Task<List<Fill>> GetFills(DateTime start, DateTime end, string subaccountName, CancellationToken cancellationToken)
	{
		return ProcessSignedRequest<List<Fill>>(Method.Get, $"api/fills?start_time={GetSecondsFromEpochStart(start)}&end_time={GetSecondsFromEpochStart(end)}", subaccountName, default, cancellationToken);
	}

	#region Util
	private static long GetMillisecondsFromEpochStart()
	{
		return GetMillisecondsFromEpochStart(DateTime.UtcNow);
	}

	private static long GetMillisecondsFromEpochStart(DateTime time)
	{
		if(time <= DateTime.UnixEpoch)
			return 0;

		return (long)time.ToUnix(false);
	}

	private static long GetSecondsFromEpochStart(DateTime time)
	{
		if(time <= DateTime.UnixEpoch)
			return 0;

		return (long)time.ToUnix();
	}

	private static Uri GetUri(string endpoint)
	{
		return new Uri($"https://ftx.com/{endpoint}");
	}

	private Task<dynamic> ProcessRequest(Method method, string endpoint, string jsonBody, CancellationToken cancellationToken)
	{
		var request = new RestRequest((string)null, method);
		if (!jsonBody.IsEmpty())
		{
			request.AddParameter("json", jsonBody, ParameterType.RequestBody);
		}
		return request.InvokeAsync(GetUri(endpoint), this, this.AddVerboseLog, cancellationToken);
	}

	private async Task<T> ProcessRequest<T>(Method method, string endpoint, string jsonBody, CancellationToken cancellationToken)
		where T : class
	{
		dynamic response = await ProcessRequest(method, endpoint, jsonBody, cancellationToken);
		FtxRestResponse<T> restResponse = Parse<FtxRestResponse<T>>(response);

		if (restResponse == null) return null;
		if (restResponse.Success)
		{
			return restResponse.Result;
		}
		return null;
	}

	private Task<dynamic> ProcessSignedRequest(Method method, string endpoint, string subaccountName, string jsonBody, CancellationToken cancellationToken)
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
			request.AddHeader("FTX-SUBACCOUNT", subaccountName.EncodeUrl());
		}

		return request.InvokeAsync(GetUri(endpoint), this, this.AddVerboseLog, cancellationToken);
	}

	private async Task<T> ProcessSignedRequest<T>(Method method, string endpoint, string subaccountName, string jsonBody, CancellationToken cancellationToken)
		where T : class
	{
		dynamic response = await ProcessSignedRequest(method, endpoint, subaccountName, jsonBody, cancellationToken);
		FtxRestResponse<T> restResponse = Parse<FtxRestResponse<T>>(response);
		if (restResponse == null) return null;

		if (restResponse.Success)
		{
			return restResponse.Result;
		}
		return null;
	}

	private async Task<T1> ProcessSignedRequest<T, T1>(Method method, string endpoint, string subaccountName, string jsonBody, CancellationToken cancellationToken)
		where T : class
		where T1 : FtxRestResponse<T>
	{
		dynamic response = await ProcessSignedRequest(method, endpoint, subaccountName, jsonBody, cancellationToken);
		T1 restResponse = Parse<T1>(response);

		if (restResponse == null) return null;
		if (restResponse.Success)
		{
			return restResponse;
		}
		return null;
	}

	private static T Parse<T>(dynamic obj)
	{
		if (((JToken)obj).Type == JTokenType.Object && obj.status == "error")
			throw new InvalidOperationException((string)obj.reason.ToString());
		return ((JToken)obj).DeserializeObject<T>();
	}
	#endregion
}
