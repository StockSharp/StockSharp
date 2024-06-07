namespace StockSharp.Coinbase.Native
{
	using System.Dynamic;

	using Newtonsoft.Json.Linq;

	class HttpClient : BaseLogReceiver
	{
		private readonly Authenticator _authenticator;

		private const string _baseUrl = "https://api.pro.coinbase.com";

		public HttpClient(Authenticator authenticator)
		{
			_authenticator = authenticator ?? throw new ArgumentNullException(nameof(authenticator));
		}

		// to get readable name after obfuscation
		public override string Name => nameof(Coinbase) + "_" + nameof(HttpClient);

		public IEnumerable<Product> GetProducts()
		{
			return MakeRequest<IEnumerable<Product>>(CreateUrl("products"), CreateRequest(Method.Get));
		}

		public IEnumerable<Ohlc> GetCandles(string currency, DateTimeOffset? start, DateTimeOffset? end, int granularity)
		{
			var request = CreateRequest(Method.Get);

			if (start != null)
				request.AddParameter("start", start.Value.UtcDateTime);

			if (end != null)
				request.AddParameter("end", end.Value.UtcDateTime);

			request.AddParameter("granularity", granularity);

			return MakeRequest<IEnumerable<Ohlc>>(CreateUrl($"/products/{currency}/candles"), request);
		}

		public IEnumerable<Account> GetAccounts()
		{
			var url = CreateUrl("accounts");
			return MakeRequest<IEnumerable<Account>>(url, ApplySecret(CreateRequest(Method.Get), url));
		}

		public IEnumerable<Order> GetOrders(params string[] status)
		{
			if (status == null)
				throw new ArgumentNullException(nameof(status));

			var request = CreateRequest(Method.Get);

			var url = CreateUrl("orders");

			foreach (var s in status)
				request.AddParameter("status", s, ParameterType.QueryString);

			return MakeRequest<IEnumerable<Order>>(url, ApplySecret(request, url));
		}

		public Order GetOrderInfo(string orderId)
		{
			var url = CreateUrl($"orders/{orderId}");

			var request = CreateRequest(Method.Get);

			return MakeRequest<Order>(url, ApplySecret(request, url), true);
		}

		public IEnumerable<Fill> GetFills(string orderId)
		{
			var url = CreateUrl("fills");

			var request = CreateRequest(Method.Get);

			request.AddParameter("order_id", orderId, ParameterType.QueryString);

			return MakeRequest<IEnumerable<Fill>>(url, ApplySecret(request, url)) ?? Enumerable.Empty<Fill>();
		}

		public Order RegisterOrder(string currency, string type, Sides side, decimal? price, decimal? stopPrice, decimal volume, TimeInForce? timeInForce, DateTimeOffset? tillDate/*, long transactionId*/)
		{
			var url = CreateUrl("orders");

			var request = CreateRequest(Method.Post);

			var body = (dynamic)new ExpandoObject();

			//body.client_oid = transactionId.To<string>();
			body.side = side.ToNative();
			body.product_id = currency;
			body.size = volume;

			if (!type.IsEmpty())
				body.type = type;

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

			return MakeRequest<Order>(url, ApplySecret(request, url, (object)body));
		}

		public void CancelOrder(string orderId)
		{
			var url = CreateUrl($"orders/{orderId}");
			MakeRequest<object>(url, ApplySecret(CreateRequest(Method.Delete), url));
		}

		public string[] CancelAllOrders()
		{
			var url = CreateUrl("orders");
			return MakeRequest<string[]>(url, ApplySecret(CreateRequest(Method.Delete), url));
		}

		public string Withdraw(string currency, decimal volume, WithdrawInfo info)
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

			return MakeRequest<Withdraw>(url, ApplySecret(request, url, (object)body)).Id;
		}

		private static Uri CreateUrl(string methodName, string version = "")
		{
			if (methodName.IsEmpty())
				throw new ArgumentNullException(nameof(methodName));

			return $"{_baseUrl}/{version}{methodName}".To<Uri>();
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

		private T MakeRequest<T>(Uri url, RestRequest request, bool is404AsNull = false)
			where T : class
		{
			dynamic obj = request.Invoke(url, this, this.AddVerboseLog);

			if (((JToken)obj).Type == JTokenType.Object && (string)obj.type == "error")
				throw new InvalidOperationException((string)obj.message + " " + (string)obj.reason);

			return ((JToken)obj).DeserializeObject<T>();
		}
	}
}