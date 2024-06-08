namespace StockSharp.Bitexbook.Native;

using System.Security;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

class HttpClient : BaseLogReceiver
{
	private readonly SecureString _key;
	private readonly SecureString _secret;

	private readonly HashAlgorithm _hasher;

	private const string _baseUrl = "https://api.bitexbook.com/api";
	private const string _version = "v2";

	private readonly IdGenerator _nonceGen;

	public HttpClient(SecureString key, SecureString secret)
	{
		_key = key;
		_secret = secret;

		_hasher = secret.IsEmpty() ? null : new HMACSHA384(secret.UnSecure().UTF8());

		_nonceGen = new UTCMlsIncrementalIdGenerator();
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	// to get readable name after obfuscation
	public override string Name => nameof(Bitexbook) + "_" + nameof(HttpClient);

	public async Task<IEnumerable<Symbol>> GetSymbols(CancellationToken cancellationToken)
	{
		dynamic response = await MakeRequest<object>(CreateUrl("symbols/statistic"), CreateRequest(Method.Get), cancellationToken);
		return ((JToken)response.symbols).DeserializeObject<IEnumerable<Symbol>>();
	}

	public async Task<IEnumerable<Ohlc>> GetCandles(string symbol, string resolution, long? from, long? to, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Get);

		request
			.AddParameter("symbol", symbol)
			.AddParameter("resolution", resolution);

		if (from != null)
			request.AddParameter("from", from.Value);

		if (to != null)
			request.AddParameter("to", to.Value);

		dynamic response = await MakeRequest<object>(CreateUrl("charts/history", string.Empty), request, cancellationToken);

		var candles = new Ohlc[(int)response.c.Count];

		for (var i = 0; i < candles.Length; i++)
		{
			candles[i] = new Ohlc
			{
				Open = (double)response.o[i],
				High = (double)response.h[i],
				Low = (double)response.l[i],
				Close = (double)response.c[i],
				Volume = (double)response.v[i],
				Time = ((long)response.t[i]).FromUnix(),
			};
		}

		return candles;
	}

	public Task<IEnumerable<Order>> GetOrders(CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Get);

		return MakeRequest<IEnumerable<Order>>(CreateUrl("order_info.do"), ApplySecret(request), cancellationToken);
	}

	public async Task<long> RegisterOrder(string symbol, string side, decimal? price, decimal volume, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		request
			.AddParameter("symbol", symbol)
			.AddParameter("type", side)
			.AddParameter("amount", volume);

		if (price != null)
		{
			request.AddParameter("price", price.Value);
		}

		dynamic response = await MakeRequest<object>(CreateUrl("trade.do"), ApplySecret(request), cancellationToken);

		return (long)response.order_id;
	}

	public Task CancelOrder(string symbol, long orderId, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		request
			.AddParameter("symbol", symbol)
			.AddParameter("order_id", orderId);

		return MakeRequest<object>(CreateUrl("cancel_order.do"), ApplySecret(request), cancellationToken);
	}

	public async Task<long> Withdraw(string currency, decimal volume, WithdrawInfo info, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.Type != WithdrawTypes.Crypto)
			throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));

		var request = CreateRequest(Method.Post);

		request.AddParameter("currency", currency);

		if (info.ChargeFee != null)
			request.AddParameter("chargefee", info.ChargeFee.Value);

		var target = info.Comment;

		if (target.IsEmpty())
			target = "address";

		request
			.AddParameter("withdraw_address", info.CryptoAddress)
			.AddParameter("withdraw_amount", volume)
			.AddParameter("target", target);

		dynamic response = await MakeRequest<object>(CreateUrl("withdraw.do"), ApplySecret(request), cancellationToken);

		return (long)response.order_id;
	}

	private static Uri CreateUrl(string methodName, string version = _version)
	{
		if (methodName.IsEmpty())
			throw new ArgumentNullException(nameof(methodName));

		return $"{_baseUrl}/{version}/{methodName}".To<Uri>();
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
			.Where(p => p.Type == ParameterType.GetOrPost && p.Value != null)
			.OrderBy(p => p.Name)
			.ToQueryString();

		encodedArgs += $"nonce={_nonceGen.GetNextId()}&secret_key={_secret.UnSecure()}";

		var signature = _hasher
		    .ComputeHash(encodedArgs.UTF8())
		    .Digest()
		    .ToLowerInvariant();

		request
			.AddHeader("sign", signature);

		return request;
	}

	private async Task<T> MakeRequest<T>(Uri url, RestRequest request, CancellationToken cancellationToken)
	{
		dynamic obj = await request.InvokeAsync(url, this, this.AddVerboseLog, cancellationToken);

		if (obj is JObject)
		{
			if (obj.success == 0)
				throw new InvalidOperationException((string)obj.error);

			if (obj.@return != null)
				obj = obj.@return;
		}

		return ((JToken)obj).DeserializeObject<T>();
	}
}