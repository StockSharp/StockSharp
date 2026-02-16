namespace StockSharp.Bibox.Native;

using System.Dynamic;

class HttpClient : BaseLogReceiver
{
	private const string _baseUrl = "https://api.bibox.com/api";
	private const string _version = "v4";

	private readonly Authenticator _authenticator;

	public HttpClient(Authenticator authenticator)
	{
		_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
	}

	// to get readable name after obfuscation
	public override string Name => nameof(Bibox) + "_" + nameof(HttpClient);

	public Task<IEnumerable<Symbol>> GetSymbols(CancellationToken cancellationToken)
	{
		return MakeRequest<IEnumerable<Symbol>>(CreateUrl("cbu/marketdata/pairs"), CreateRequest(Method.Get), cancellationToken);
	}

	public Task<IEnumerable<Ohlc>> GetCandles(string symbol, string period, long after, CancellationToken cancellationToken)
	{
		var url = CreateUrl($"cbu/marketdata/candles?symbol={symbol}&time_frame={period}&after={after}&limit=1000");
		return MakeRequest<IEnumerable<Ohlc>>(url, CreateRequest(Method.Get), cancellationToken);
	}

	public async Task<IEnumerable<Balance>> GetBalances(CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		var cmds = CreateCommand("cbu/userdata/accounts", null);

		dynamic response = await MakeRequest<object>(CreateUrl("cbu/userdata/accounts"), ApplySecret(request, cmds), cancellationToken);

		return ((JToken)response.assets_list).DeserializeObject<IEnumerable<Balance>>();
	}

	public async Task<IEnumerable<Order>> GetOrders(int accountType, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		var body = new
		{
			account_type = accountType,
			page = 1,
			size = 50,
		};

		var cmds = CreateCommand("orderpending/orderPendingList", body);

		dynamic response = await MakeRequest<object>(CreateUrl("orderpending"), ApplySecret(request, cmds), cancellationToken);

		return ((JToken)response.items).DeserializeObject<IEnumerable<Order>>();
	}

	public Task<long> RegisterOrder(long transactionId, string symbol, int accountType, int orderType, int orderSide, decimal price, decimal volume, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		var body = new
		{
			pair = symbol,
			account_type = accountType,
			order_type = orderType,
			order_side = orderSide,
			price,
			amount = volume,
		};

		var cmds = CreateCommand("orderpending/trade", body, transactionId);

		return MakeRequest<long>(CreateUrl("orderpending"), ApplySecret(request, cmds), cancellationToken);
	}

	public Task CancelOrder(long transactionId, long orderId, CancellationToken cancellationToken)
	{
		var request = CreateRequest(Method.Post);

		var cmds = CreateCommand("orderpending/cancelTrade", new { orders_id = orderId }, transactionId);

		return MakeRequest<object>(CreateUrl("orderpending"), ApplySecret(request, cmds), cancellationToken);
	}

	public Task<long> Withdraw(long transactionId, string symbol, decimal volume, WithdrawInfo info, int googleCode, string password, string comment, CancellationToken cancellationToken)
	{
		if (info == null)
			throw new ArgumentNullException(nameof(info));

		if (info.Type != WithdrawTypes.Crypto)
			throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));

		var request = CreateRequest(Method.Post);

		dynamic body = new ExpandoObject();

		body.coin_symbol = symbol;
		body.amount = volume;
		body.totp_code = googleCode;
		body.trade_pwd = password;
		body.addr = info.CryptoAddress;
		body.addr_remark = comment;

		if (!info.Comment.IsEmpty())
			body.memo = info.Comment;

		var cmds = CreateCommand("transfer/transferOut", (object)body, transactionId);

		return MakeRequest<long>(CreateUrl("transfer"), ApplySecret(request, cmds), cancellationToken);
	}

	private static readonly JsonSerializerSettings _serializerSettings = JsonHelper.CreateJsonSerializerSettings();

	private static string CreateCommand(string cmd, object body, long? index = null)
	{
		return JsonConvert.SerializeObject(new[]
		{
			new
			{
				cmd,
				index,
				body
			}
		}, _serializerSettings);
	}

	private static Uri CreateUrl(string methodName)
	{
		if (methodName.IsEmpty())
			throw new ArgumentNullException(nameof(methodName));

		return $"{_baseUrl}/{_version}/{methodName}".To<Uri>();
	}

	private static RestRequest CreateRequest(Method method)
	{
		return new RestRequest((string)null, method);
	}

	private RestRequest ApplySecret(RestRequest request, string cmds)
	{
		if (request == null)
			throw new ArgumentNullException(nameof(request));

		var bodyStr = JsonConvert.SerializeObject(new
		{
			cmds,
			apikey = _authenticator.Key.UnSecure(),
			sign = _authenticator.MakeSign(cmds)
		}, _serializerSettings);

		request.AddBodyAsStr(bodyStr);

		return request;
	}

	private async Task<T> MakeRequest<T>(Uri url, RestRequest request, CancellationToken cancellationToken)
	{
		dynamic obj = await request.InvokeAsync(url, this, this.AddVerboseLog, cancellationToken);

		if (obj is JObject)
		{
			if (obj.error != null)
				throw new InvalidOperationException(((int)obj.error.code).ToErrorText());

			if (obj.result != null)
			{
				obj = obj.result;

				if (obj is JArray arr && arr.Count == 1)
				{
					obj = arr[0];

					if (obj.result != null)
						obj = obj.result;
				}

				if (obj is JObject && obj.error != null)
					throw new InvalidOperationException(((int)obj.error.code).ToErrorText());
			}
		}

		return ((JToken)obj).DeserializeObject<T>();
	}
}