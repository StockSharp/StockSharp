namespace StockSharp.ZB.Native;

class HttpClient(Authenticator authenticator) : BaseLogReceiver
{
	private readonly Authenticator _authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));

	private readonly IdGenerator _nonceGen = new UTCMlsIncrementalIdGenerator();

	// to get readable name after obfuscation
	public override string Name => nameof(ZB) + "_" + nameof(HttpClient);

	public Task<IDictionary<string, Ticker>> GetSymbolsAsync(CancellationToken cancellationToken)
	{
		return MakeRequestAsync<IDictionary<string, Ticker>>(new Url("http://api.zb.cn/data/v1/allTicker"), CreateRequest(Method.Get), cancellationToken);
	}

	public Task<IEnumerable<Ohlc>> GetCandlesAsync(string symbol, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Get);

		return MakeRequestAsync<IEnumerable<Ohlc>>(new Url($"http://api.zb.cn/data/v1/kline?market={symbol}"), request, cancellationToken);
	}

	public async Task<string> WithdrawAsync(string currency, decimal volume, WithdrawInfo info, SecureString password, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.Type != WithdrawTypes.Crypto)
			throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));

		var url = new Url("https://trade.zb.cn/api/withdraw");

		var qs = url.QueryString;

		qs
			.Append("accesskey", _authenticator.Key.UnSecure())
			.Append("amount", volume)
			.Append("currency", currency)
			.Append("method", "withdraw")
			.Append("receiveAddr", info.CryptoAddress)
			.Append("safePwd", password.UnSecure());

		if (info.ChargeFee != null)
			qs.Append("fees", info.ChargeFee.Value);

		qs
			.Append("sign", _authenticator.MakeSign(qs.ToString()))
			.Append("reqTime", _nonceGen.GetNextId());

		var response = await MakeRequestAsync<dynamic>(url, CreateRequest(Method.Get), cancellationToken);
		
		return (string)response.id;
	}

	private static RestRequest CreateRequest(Method method)
	{
		return new RestRequest((string)null, method);
	}

	private async Task<T> MakeRequestAsync<T>(Uri url, RestRequest request, CancellationToken cancellationToken)
	{
		dynamic obj = await request.InvokeAsync(url, this, this.AddVerboseLog, cancellationToken);

		if (obj is JObject)
		{
			if (obj.code != null && obj.code != 1000)
				throw new InvalidOperationException((string)obj.message);

			if (obj.data != null)
				obj = obj.data;
		}

		return ((JToken)obj).DeserializeObject<T>();
	}
}