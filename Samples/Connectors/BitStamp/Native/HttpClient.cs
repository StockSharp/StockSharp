#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.Native.BitStamp
File: HttpClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp.Native
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;
	using System.Security.Cryptography;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Net;

	using Newtonsoft.Json;
	using Newtonsoft.Json.Linq;

	using RestSharp;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.BitStamp.Native.Model;

	class HttpClient : BaseLogReceiver
	{
		private readonly string _clientId;
		private readonly SecureString _key;
		private readonly bool _authV2;
		private readonly HashAlgorithm _hasher;

		private const string _baseAddr = "www.bitstamp.net";

		private readonly IdGenerator _nonceGen;

		public HttpClient(string clientId, SecureString key, SecureString secret, bool authV2)
		{
			_clientId = clientId;
			_key = key;
			_authV2 = authV2;
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

		public IEnumerable<Symbol> GetPairsInfo()
		{
			return MakeRequest<IEnumerable<Symbol>>(CreateUrl("trading-pairs-info"), CreateRequest(Method.GET));
		}

		public IEnumerable<Transaction> RequestTransactions(string ticker, string interval = null)
		{
			var url = CreateUrl($"transactions/{ticker}");
			var request = CreateRequest(Method.GET);

			if (interval != null)
				request.AddParameter("time", interval);

			return MakeRequest<IEnumerable<Transaction>>(url, request, true);
		}

		public Tuple<Dictionary<string, RefTriple<decimal?, decimal?, decimal?>>, Dictionary<string, decimal>> GetBalances(string ticker = null)
		{
			var url = CreateUrl(ticker.IsEmpty() ? "balance" : $"balance/{ticker}");
			dynamic response = MakeRequest<object>(url, ApplySecret(CreateRequest(Method.POST), url));

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

			return Tuple.Create(balances, fees);
		}

		public UserTransaction[] RequestUserTransactions(string ticker, int? offset, int? limit)
		{
			var request = CreateRequest(Method.POST);

			if (offset != null)
				request.AddParameter("offset", offset.Value);

			if (limit != null)
				request.AddParameter("limit", limit.Value);

			var url = CreateUrl(ticker.IsEmpty() ? "user_transactions" : $"user_transactions/{ticker}");
			return MakeRequest<UserTransaction[]>(url, ApplySecret(request, url));
		}

		public IEnumerable<UserOrder> RequestOpenOrders(string ticker = "all")
		{
			var url = CreateUrl($"open_orders/{ticker}");
			return MakeRequest<IEnumerable<UserOrder>>(url, ApplySecret(CreateRequest(Method.POST), url));
		}

		public UserOrder RegisterOrder(string pair, string side, decimal? price, decimal volume, decimal? stopPrice, bool daily, bool ioc)
		{
			var market = price == null ? "market/" : string.Empty;

			var request = CreateRequest(Method.POST);

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
			return MakeRequest<UserOrder>(url, ApplySecret(request, url));
		}

		public UserOrder CancelOrder(long orderId)
		{
			var url = CreateUrl("cancel_order");
			return MakeRequest<UserOrder>(url, ApplySecret(CreateRequest(Method.POST).AddParameter("id", orderId), url));
		}

		public void CancelAllOrders()
		{
			var url = CreateUrl("cancel_all_orders", string.Empty);
			var result = MakeRequest<bool>(url, ApplySecret(CreateRequest(Method.POST), url));
			
			if (!result)
				throw new InvalidOperationException();
		}

		public long Withdraw(string currency, decimal volume, WithdrawInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			var request = CreateRequest(Method.POST);

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
					dynamic response = MakeRequest<object>(url, ApplySecret(request, url));

					if (response.id == null)
						throw new InvalidOperationException();

					return (long)response.id;
				}
				case WithdrawTypes.Crypto:
				{
					request
						.AddParameter("amount", volume)
						.AddParameter("address", info.CryptoAddress);

					string methodName;
					string version;

					switch (currency.To<CurrencyTypes>())
					{
						case CurrencyTypes.BTC:
						{
							methodName = "bitcoin_withdrawal";
							version = string.Empty;

							request
								.AddParameter("instant", info.Express ? 1 : 0);

							break;
						}
						case CurrencyTypes.LTC:
						case CurrencyTypes.ETH:
						case CurrencyTypes.BCH:
						case CurrencyTypes.XRP:
						{
							methodName = $"{currency}_withdrawal".ToLowerInvariant();
							version = "v2/";

							if (!info.Comment.IsEmpty())
								request.AddParameter("destination_tag", info.Comment);

							break;
						}
						default:
							throw new NotSupportedException(LocalizedStrings.Str1212Params.Put(currency));
					}

					var url = CreateUrl(methodName, version);
					dynamic response = MakeRequest<object>(url, ApplySecret(request, url));

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

		private static IRestRequest CreateRequest(Method method)
		{
			return new RestRequest(method);
		}

		private static readonly JsonSerializerSettings _serializerSettings = NetworkHelper.CreateJsonSerializerSettings();

		private IRestRequest ApplySecret(IRestRequest request, Uri url)
		{
			if (request == null)
				throw new ArgumentNullException(nameof(request));

			var urlStr = url.ToString();

			if (_authV2 && urlStr.ContainsIgnoreCase("/v2/"))
			{
				var apiKey = "BITSTAMP " + _key.UnSecure();
				var version = "v2";
				var nonce = Guid.NewGuid().ToString();
				var timeStamp = ((long)TimeHelper.UnixNowMls).To<string>();
				
				var payload = request
                  .Parameters
                  .Where(p => p.Type == ParameterType.GetOrPost && p.Value != null)
                  .OrderBy(p => p.Name)
                  .Select(p => $"{p.Name}={p.Value}")
                  .Join("&");

				var str = apiKey +
				          request.Method +
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
			}
			else
			{
				var nonce = _nonceGen.GetNextId();

				var signature = _hasher
				                .ComputeHash((nonce + _clientId + _key.UnSecure()).UTF8())
				                .Digest()
				                .ToUpperInvariant();
		
				request
					.AddParameter("key", _key.UnSecure())
					.AddParameter("nonce", nonce)
					.AddParameter("signature", signature);
			}

			return request;
		}

		private T MakeRequest<T>(Uri url, IRestRequest request, bool cookies = false)
		{
			dynamic obj = request.Invoke(url, this, this.AddVerboseLog, client =>
			{
				if (cookies)
					client.CookieContainer = new CookieContainer();
			});

			if (((JToken)obj).Type == JTokenType.Object && obj.status == "error")
				throw new InvalidOperationException((string)obj.reason.ToString());

			return ((JToken)obj).DeserializeObject<T>();
		}
	}
}