namespace StockSharp.BitStamp.Native;

using System.Security;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

class HttpClient : BaseLogReceiver
{
	private readonly SecureString _key;
	private readonly HashAlgorithm _hasher;

	private const string _baseAddr = "www.bitstamp.net";

	private readonly IdGenerator _nonceGen;

	public HttpClient(SecureString key, SecureString secret)
	{
		_key = key;
		_hasher = secret.IsEmpty() ? null : new HMACSHA256(secret.UnSecure().ASCII());

		_nonceGen = new UTCMlsIncrementalIdGenerator();
	}

	protected override void DisposeManaged()
	{
		_hasher?.Dispose();
		base.DisposeManaged();
	}

	// to get readable name after obfuscation
	public override string Name => nameof(BitStamp) + "_" + nameof(HttpClient);

	public ValueTask<IEnumerable<Symbol>> GetPairsInfo(CancellationToken cancellationToken)
	{
		return MakeRequest<IEnumerable<Symbol>>(CreateUrl("trading-pairs-info"), CreateRequest(Method.Get), cancellationToken);
	}

	public ValueTask<IEnumerable<Transaction>> GetTransactions(string ticker, string interval, CancellationToken cancellationToken)
	{
		var url = CreateUrl($"transactions/{ticker}");
		var request = CreateRequest(Method.Get);

		if (interval != null)
			request.AddParameter("time", interval);

		return MakeRequest<IEnumerable<Transaction>>(url, request, cancellationToken);
	}

	public async ValueTask<IEnumerable<Ohlc>> GetOhlc(string ticker, int step, int limit, DateTime start/*, DateTime end*/, CancellationToken cancellationToken)
	{
		var url = CreateUrl($"ohlc/{ticker}");
		var request = CreateRequest(Method.Get);

		request.AddParameter("step", step);
		request.AddParameter("limit", limit);
		request.AddParameter("start", start.ToUnix());
		//request.AddParameter("end", end.ToUnix());
		request.AddParameter("exclude_current_candle", true);

		dynamic result = await MakeRequest<object>(url, request, cancellationToken);

		return ((JToken)result.data.ohlc).DeserializeObject<IEnumerable<Ohlc>>();
	}

	public async ValueTask<(Dictionary<string, RefTriple<decimal?, decimal?, decimal?>>, Dictionary<string, decimal>)> GetBalances(string ticker, CancellationToken cancellationToken)
	{
		var url = CreateUrl(ticker.IsEmpty() ? "balance" : $"balance/{ticker}");
		dynamic response = await MakeRequest<object>(url, ApplySecret(CreateRequest(Method.Post), url), cancellationToken);

		var balances = new Dictionary<string, RefTriple<decimal?, decimal?, decimal?>>(StringComparer.InvariantCultureIgnoreCase);
		var fees = new Dictionary<string, decimal>(StringComparer.InvariantCultureIgnoreCase);

		RefTriple<decimal?, decimal?, decimal?> GetBalance(string symbol)
		{
			return balances.SafeAdd(symbol, key => new RefTriple<decimal?, decimal?, decimal?>());
		}

		foreach (var property in ((JObject)response).Properties())
		{
			var parts = property.Name.Split('_');
			var symbol = parts[0];

			var value = (decimal)property.Value;

			switch (parts[1].ToLowerInvariant())
			{
				case "fee":
					fees.Add(symbol, value);
					break;

				case "available":
					GetBalance(symbol).First = value;
					break;

				case "balance":
					GetBalance(symbol).Second = value;
					break;

				case "reserved":
					GetBalance(symbol).Third = value;
					break;
			}
		}

		return (balances, fees);
	}

	public ValueTask<UserTransaction[]> RequestUserTransactions(string ticker, int? offset, int? limit, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		if (offset != null)
			request.AddParameter("offset", offset.Value);

		if (limit != null)
			request.AddParameter("limit", limit.Value);

		var url = CreateUrl(ticker.IsEmpty() ? "user_transactions" : $"user_transactions/{ticker}");
		return MakeRequest<UserTransaction[]>(url, ApplySecret(request, url), cancellationToken);
	}

	public ValueTask<IEnumerable<UserOrder>> RequestOpenOrders(string ticker, CancellationToken cancellationToken)
	{
		var url = CreateUrl($"open_orders/{ticker}");
		return MakeRequest<IEnumerable<UserOrder>>(url, ApplySecret(CreateRequest(Method.Post), url), cancellationToken);
	}

	public ValueTask<UserOrder> RegisterOrder(string pair, string side, decimal? price, decimal volume, decimal? stopPrice, bool daily, bool ioc, CancellationToken cancellationToken)
	{
		var market = price == null ? "market/" : string.Empty;

		var request = CreateRequest(Method.Post);

		request.AddParameter("amount", volume);

		if (price != null)
			request.AddParameter("price", price.Value);

		if (stopPrice != null)
			request.AddParameter("limit_price", stopPrice.Value);

		if (daily)
			request.AddParameter("daily_order", true);

		if (ioc)
			request.AddParameter("ioc_order", true);

		var url = CreateUrl($"{side}/{market}{pair}");
		return MakeRequest<UserOrder>(url, ApplySecret(request, url), cancellationToken);
	}

	public ValueTask<UserOrder> CancelOrder(long orderId, CancellationToken cancellationToken)
	{
		var url = CreateUrl("cancel_order");
		return MakeRequest<UserOrder>(url, ApplySecret(CreateRequest(Method.Post).AddParameter("id", orderId), url), cancellationToken);
	}

	public async ValueTask CancelAllOrders(CancellationToken cancellationToken)
	{
		var url = CreateUrl("cancel_all_orders", string.Empty);
		var result = await MakeRequest<bool>(url, ApplySecret(CreateRequest(Method.Post), url), cancellationToken);

		if (!result)
			throw new InvalidOperationException();
	}

	public async ValueTask<long> Withdraw(string currency, decimal volume, WithdrawInfo info, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		var request = CreateRequest(Method.Post);

		switch (info.Type)
		{
			case WithdrawTypes.BankWire:
			{
				if (info.BankDetails == null)
					throw new InvalidOperationException(LocalizedStrings.BankDetailsIsMissing);

				request
					.AddParameter("amount", volume)
					.AddParameter("account_currency", info.BankDetails.Currency.To<string>())
					.AddParameter("name", info.BankDetails.AccountName)
					.AddParameter("IBAN", info.BankDetails.Iban)
					.AddParameter("BIC", info.BankDetails.Bic)
					.AddParameter("address", info.CompanyDetails?.Address)
					.AddParameter("postal_code", info.CompanyDetails?.PostalCode)
					.AddParameter("city", info.CompanyDetails?.City)
					.AddParameter("country", info.CompanyDetails?.Country)
					.AddParameter("type", volume)
					.AddParameter("bank_name", info.BankDetails.Name)
					.AddParameter("bank_address", info.BankDetails.Address)
					.AddParameter("bank_postal_code", info.BankDetails.PostalCode)
					.AddParameter("bank_city", info.BankDetails.City)
					.AddParameter("bank_country", info.BankDetails.Country)
					.AddParameter("currency", currency)
					.AddParameter("comment", info.Comment);

				var url = CreateUrl("withdrawal/open");
				dynamic response = await MakeRequest<object>(url, ApplySecret(request, url), cancellationToken);

				if (response.id == null)
					throw new InvalidOperationException();

				return (long)response.id;
			}
			case WithdrawTypes.Crypto:
			{
				request
					.AddParameter("amount", volume)
					.AddParameter("address", info.CryptoAddress);

				if (!info.Comment.IsEmpty())
					request.AddParameter("destination_tag", info.Comment);

				var url = CreateUrl($"{currency}_withdrawal".ToLowerInvariant());
				dynamic response = await MakeRequest<object>(url, ApplySecret(request, url), cancellationToken);

				if (response.id == null)
					throw new InvalidOperationException();

				return (long)response.id;
			}
			default:
				throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));
		}
	}

	private static Uri CreateUrl(string methodName, string version = "v2/")
	{
		if (methodName.IsEmpty())
			throw new ArgumentNullException(nameof(methodName));

		return $"https://{_baseAddr}/api/{version}{methodName}/".To<Uri>();
	}

	private static RestRequest CreateRequest(Method method)
	{
		return new RestRequest((string)null, method);
	}

	private static readonly JsonSerializerSettings _serializerSettings = JsonHelper.CreateJsonSerializerSettings();

	private RestRequest ApplySecret(RestRequest request, Uri url)
	{
		if (request == null)
			throw new ArgumentNullException(nameof(request));

		var urlStr = url.ToString();

		var apiKey = "BITSTAMP " + _key.UnSecure();
		var version = "v2";
		var nonce = Guid.NewGuid().ToString();
		var timeStamp = ((long)TimeHelper.UnixNowMls).To<string>();

		var payload = request
	 		.Parameters
			.Where(p => p.Type == ParameterType.GetOrPost && p.Value != null)
			.OrderBy(p => p.Name)
			.ToQueryString(false);

		var str = apiKey +
			        request.Method.ToString().ToUpperInvariant() +
			        url.Host +
			        url.PathAndQuery.Remove(url.Query, true) +
			        url.Query +
			        "application/json" +
			        nonce +
			        timeStamp +
			        version +
			        payload;

		var signature = _hasher
			            .ComputeHash(str.UTF8())
			            .Digest()
			            .ToUpperInvariant();

		request
			.AddHeader("X-Auth", apiKey)
			.AddHeader("X-Auth-Signature", signature)
			.AddHeader("X-Auth-Nonce", nonce)
			.AddHeader("X-Auth-Timestamp", timeStamp)
			.AddHeader("X-Auth-Version", version);

		return request;
	}

	private async ValueTask<T> MakeRequest<T>(Uri url, RestRequest request, CancellationToken cancellationToken)
	{
		dynamic obj = await request.InvokeAsync(url, this, this.AddVerboseLog, cancellationToken);

		if (((JToken)obj).Type == JTokenType.Object && obj.status == "error")
			throw new InvalidOperationException((string)obj.reason.ToString());

		return ((JToken)obj).DeserializeObject<T>();
	}
}
