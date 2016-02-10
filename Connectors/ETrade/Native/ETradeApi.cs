#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.Native.ETrade
File: ETradeApi.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade.Native
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Net;
	using System.Security;
	using System.Text;

	using Ecng.Common;

	using RestSharp;
	using RestSharp.Authenticators;
	using RestSharp.Contrib;

	using StockSharp.Algo;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	internal class ETradeApi
	{
		private const string _orderNamespace = "http://order.etws.etrade.com";
		private const string _baseUrl = "https://etws.etrade.com";
		private const string _sandboxBaseUrl = "https://etwssandbox.etrade.com";

		private const string _xmlContentType = "application/xml";

		private const int _maxAllowedPositionsCount = 25;

		private readonly ETradeClient _client;
		private string _sandboxStr;

		private IAuthenticator _authenticator;

		public string ConsumerKey { get; set; }

		public SecureString ConsumerSecret { get; set; }

		private bool _sandbox;

		public bool Sandbox
		{
			get { return _sandbox; }
			set
			{
				_sandbox = value;
				_sandboxStr = _sandbox ? "sandbox/" : "";
			}
		}

		public ETradeApi(ETradeClient client)
		{
			if (client == null)
				throw new ArgumentNullException(nameof(client));

			_client = client;
		}

		RestClient CreateClient()
		{
			return _sandbox ? new RestClient(_sandboxBaseUrl) : new RestClient(_baseUrl);
		}

		#region authorization

		public OAuthToken GetRequestToken()
		{
			if (ConsumerKey.IsEmpty() || ConsumerSecret.IsEmpty())
				throw new ETradeException(LocalizedStrings.Str3345);

			var client = new RestClient(_baseUrl)
			{
				Authenticator = OAuth1Authenticator.ForRequestToken(ConsumerKey, ConsumerSecret.To<string>(), "oob")
			};

			var response = ExecuteRequest(client, new RestRequest("oauth/request_token"));

			if (response.ResponseStatus != ResponseStatus.Completed || response.StatusCode != HttpStatusCode.OK)
			{
				throw new ETradeAuthorizationFailedException(
					LocalizedStrings.Str3346Params.Put(response.StatusCode + ": " + response.StatusDescription));
			}

			var qs = HttpUtility.ParseQueryString(response.Content);

			return new OAuthToken(ConsumerKey, qs["oauth_token"], qs["oauth_token_secret"]);
		}

		public OAuthToken GetAccessToken(OAuthToken reqToken, string verifier)
		{
			var client = new RestClient(_baseUrl)
			{
				Authenticator =
					OAuth1Authenticator.ForAccessToken(ConsumerKey, ConsumerSecret.To<string>(), reqToken.Token, reqToken.Secret, verifier)
			};

			var response = ExecuteRequest(client, new RestRequest("oauth/access_token"));

			if (response.ResponseStatus != ResponseStatus.Completed || response.StatusCode != HttpStatusCode.OK)
			{
				throw new ETradeAuthorizationFailedException(
					LocalizedStrings.Str3347Params.Put(response.StatusCode + ": " + response.StatusDescription));
			}

			var qs = HttpUtility.ParseQueryString(response.Content);

			var token = qs["oauth_token"];
			var secret = qs["oauth_token_secret"];

			if (token.IsEmpty() || secret.IsEmpty())
				throw new ETradeAuthorizationFailedException(LocalizedStrings.Str3348);

			return new OAuthToken(ConsumerKey, token, secret);
		}

		public OAuthToken RenewAccessToken(OAuthToken accessToken)
		{
			var client = new RestClient(_baseUrl)
			{
				Authenticator =
					OAuth1Authenticator.ForAccessTokenRefresh(ConsumerKey, ConsumerSecret.To<string>(), accessToken.Token, accessToken.Secret, null)
			};

			var response = ExecuteRequest(client, new RestRequest("oauth/renew_access_token"));

			if (response.ResponseStatus != ResponseStatus.Completed || response.StatusCode != HttpStatusCode.OK)
			{
				throw new ETradeAuthorizationRenewFailed(
					LocalizedStrings.Str3349Params.Put(response.StatusCode + ": " + response.StatusDescription));
			}

			var qs = HttpUtility.ParseQueryString(response.Content);

			var newToken = qs["oauth_token"];

			return newToken == null ? accessToken : new OAuthToken(ConsumerKey, newToken, qs["oauth_token_secret"]);
		}

		public void SetAuthenticator(OAuthToken accessToken)
		{
			_authenticator = accessToken == null
								 ? null
								 : OAuth1Authenticator.ForProtectedResource(ConsumerKey, ConsumerSecret.To<string>(), accessToken.Token, accessToken.Secret);
		}

		public string GetAuthorizeUrl(OAuthToken requestToken)
		{
			return "https://us.etrade.com/e/t/etws/authorize?key={0}&token={1}".Put(ConsumerKey, requestToken.Token);
		}

		#endregion

		public PlaceEquityOrderResponse2 SendOrder(BusinessEntities.Order order)
		{
			if (_authenticator == null)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3350);

			var data = new EquityOrderRequest
			{
				symbol = order.Security.Code,
				orderAction = order.Direction == Sides.Buy ? "BUY" : "SELL",
				marketSession = "REGULAR",
				routingDestination = "AUTO"
			};

			FillOrderRequest(data, order);

			var req = new RestRequest("order/{0}rest/placeequityorder.json".Put(_sandboxStr));

			var client = CreateClient();
			client.Authenticator = _authenticator;

			req.XmlSerializer.ContentType = _xmlContentType;
			req.Method = Method.POST;
			req.RequestFormat = DataFormat.Xml;
			req.XmlSerializer.RootElement = "PlaceEquityOrder";
			req.RootElement = "PlaceEquityOrderResponse"; // for response parsing

			req.AddBody(data, _orderNamespace);

			var response = ExecuteRequest<PlaceEquityOrderResponse>(client, req);

			CheckResponse(response, "SendOrder");

			return response.Data.EquityOrderResponse;
		}

		public PlaceEquityOrderResponse2 SendOrderChange(int oldOrderId, BusinessEntities.Order newOrder)
		{
			if (_authenticator == null)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3350);

			var data = new changeEquityOrderRequest
			{
				orderNum = oldOrderId
			};

			FillOrderRequest(data, newOrder);

			var req = new RestRequest("order/{0}rest/placechangeequityorder.json".Put(_sandboxStr));

			var client = CreateClient();
			client.Authenticator = _authenticator;

			req.XmlSerializer.ContentType = _xmlContentType;
			req.Method = Method.POST;
			req.RequestFormat = DataFormat.Xml;
			req.XmlSerializer.RootElement = "placeChangeEquityOrder";
			req.RootElement = "placeChangeEquityOrderResponse"; // for response parsing

			req.AddBody(data, _orderNamespace);

			var response = ExecuteRequest<placeChangeEquityOrderResponse>(client, req);

			CheckResponse(response, "SendOrderChange");

			return response.Data.equityOrderResponse;
		}

		public CancelOrderResponse2 CancelOrder(int id, string portfName)
		{
			if (_authenticator == null)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3350);

			var data = new cancelOrderRequest
			{
				orderNum = id,
				accountId = portfName.To<long>()
			};

			var req = new RestRequest("order/{0}rest/cancelorder.json".Put(_sandboxStr));

			var client = CreateClient();
			client.Authenticator = _authenticator;

			req.XmlSerializer.ContentType = _xmlContentType;
			req.Method = Method.POST;
			req.RequestFormat = DataFormat.Xml;
			req.XmlSerializer.RootElement = "cancelOrder";
			req.RootElement = "cancelOrderResponse"; // for response parsing

			req.AddBody(data, _orderNamespace);

			var response = ExecuteRequest<cancelOrderResponse>(client, req);

			CheckResponse(response, "CancelOrder");

			return response.Data.cancelResponse;
		}

		public List<Order> GetOrderList(string portfName, int requestCount, ref string marker)
		{
			if (_authenticator == null)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3350);

			var client = CreateClient();
			client.Authenticator = _authenticator;

			var reqStr = "order/{0}rest/orderlist/{1}.json".Put(_sandboxStr, portfName);
			var result = new List<Order>();

			var req = new RestRequest(reqStr) { RootElement = "GetOrderListResponse" };

			req.AddParameter("count", requestCount);
			if (!marker.IsEmpty())
				req.AddParameter("marker", marker);

			var response = ExecuteRequest<GetOrderListResponse>(client, req);

			CheckResponse(response, "GetOrderList");

			marker = response.Data.orderListResponse.marker;

			if (response.Data.orderListResponse.orderDetails != null)
				result.AddRange(response.Data.orderListResponse.orderDetails.Select(od => od != null ? od.order : null));

			return result;
		}

		public List<ProductInfo> ProductLookup(string criteria)
		{
			if (_authenticator == null)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3350);

			var client = CreateClient();
			client.Authenticator = _authenticator;

			var reqStr = "market/{0}rest/productlookup.json".Put(_sandboxStr);

			var req = new RestRequest(reqStr) { RootElement = "productLookupResponse" };

			req.AddParameter("company", criteria);
			req.AddParameter("type", "EQ");

			var response = ExecuteRequest<productLookupResponse>(client, req);

			CheckResponse(response, "ProductLookup");

			return response.Data.productList ?? new List<ProductInfo>();
		}

		public List<AccountInfo> GetAccounts()
		{
			if (_authenticator == null)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3350);

			var client = CreateClient();
			client.Authenticator = _authenticator;

			var reqStr = "accounts/{0}rest/accountlist.json".Put(_sandboxStr);

			var req = new RestRequest(reqStr) { RootElement = "json.accountListResponse" };

			var response = ExecuteRequest<AccountListResponse>(client, req);

			CheckResponse(response, "GetAccounts");

			return response.Data.response ?? new List<AccountInfo>();
		}

		public List<PositionInfo> GetPositions(string portfName, ref string marker)
		{
			if (_authenticator == null)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3350);

			var client = CreateClient();
			client.Authenticator = _authenticator;

			var reqStr = "accounts/{0}rest/accountpositions/{1}.json".Put(_sandboxStr, portfName);
			var result = new List<PositionInfo>();
			var req = new RestRequest(reqStr) { RootElement = "json.accountPositionsResponse" };

			req.AddParameter("count", _maxAllowedPositionsCount);
			//req.AddParameter("typeCode", "EQ");

			if (!marker.IsEmpty())
				req.AddParameter("marker", marker);

			var response = ExecuteRequest<AccountPositionsResponse>(client, req);

			CheckResponse(response, "AccountPositions");

			marker = response.Data.marker;

			if (response.Data.response != null)
				result.AddRange(response.Data.response);

			return result;
		}

		public RateLimitStatus GetRateLimitStatus(string module)
		{
			if (_authenticator == null)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3350);

			var client = CreateClient();
			client.Authenticator = _authenticator;

			var reqStr = "statuses/{0}rest/limits.json".Put(_sandboxStr);

			var req = new RestRequest(reqStr) { RootElement = "RateLimitStatus" };

			req.AddParameter("module", module);

			var response = ExecuteRequest<RateLimitStatus>(client, req);

			CheckResponse(response, "GetRateLimitStatus");

			return response.Data;
		}

		private void DumpRestData(IRestRequest request, IRestResponse response, Stopwatch sw)
		{
			_client.AddLog(LogLevels.Debug, () =>
			{
				var sb = new StringBuilder();
				sb.AppendFormat("----------{0:HH:mm:ss.fff}-----------\nRequest: {1} {2}", DateTime.Now, request.Method,
								request.Resource).AppendLine();

				if (request.Method == Method.POST)
				{
					var body = request.Parameters.Find(p => p.Name == request.XmlSerializer.ContentType);
					if (body != null)
						sb.AppendFormat("Body({0}):\n", body.Name).Append(body.Value).AppendLine();
				}

				sb.AppendFormat("Response:\nResponse Uri: {0}", response.ResponseUri).AppendLine();
				sb.AppendFormat("Elapsed: {0:F3}sec", sw.Elapsed.TotalSeconds).AppendLine();
				sb.Append("Content type: ").Append(response.ContentType).AppendLine();
				sb.Append("Response status: ").Append(response.ResponseStatus).AppendLine();
				sb.AppendFormat("Status code={0}, description={1}", response.StatusCode, response.StatusDescription).AppendLine();

				if (response.ErrorException != null)
					sb.Append("Error exception: ").Append(response.ErrorException).AppendLine();
				if (!response.ErrorMessage.IsEmpty())
					sb.Append("Error message: ").Append(response.ErrorMessage).AppendLine();

				sb.Append("Content:\n").Append(response.Content).AppendLine().AppendLine();

				return sb.ToString();
			});
		}

		private IRestResponse ExecuteRequest(RestClient client, RestRequest request)
		{
			var sw = Stopwatch.StartNew();
			var resp = client.Execute(request);
			sw.Stop();

			DumpRestData(request, resp, sw);

			return resp;
		}

		private IRestResponse<T> ExecuteRequest<T>(RestClient client, RestRequest request) where T : new()
		{
			var sw = Stopwatch.StartNew();
			var resp = client.Execute<T>(request);
			sw.Stop();

			DumpRestData(request, resp, sw);

			return resp;
		}

		private static void CheckResponse(IRestResponse response, string name)
		{
			if (response.StatusCode == HttpStatusCode.Unauthorized)
				throw new ETradeUnauthorizedException(LocalizedStrings.Str3351, response.ErrorException);

			if (response.ResponseStatus != ResponseStatus.Completed)
				throw new ETradeConnectionFailedException(
					LocalizedStrings.Str3352Params.Put(name, response.ResponseStatus), response.ErrorException);

			if (response.StatusCode != HttpStatusCode.OK)
				throw new ETradeException(LocalizedStrings.Str3353Params.Put(name, response.StatusCode),
										  response.ErrorException);
		}

		private static void FillOrderRequest(OrderRequestBase request, BusinessEntities.Order order)
		{
			request.accountId = order.Portfolio.Name.To<long>();
			request.clientOrderId = order.TransactionId.To<string>();
			request.allOrNone = order.TimeInForce == TimeInForce.MatchOrCancel;
			request.quantity = (int)order.Volume;

			if (order.ExpiryDate == null || order.ExpiryDate.Value.IsGtc())
				request.orderTerm = "GOOD_UNTIL_CANCEL";
			else if (order.ExpiryDate.Value.IsToday())
				request.orderTerm = "GOOD_FOR_DAY";
			else
				throw new InvalidOperationException(LocalizedStrings.Str3354);

			switch (order.Type)
			{
				case OrderTypes.Conditional:
				{
					var cond = (ETradeOrderCondition)order.Condition;

					switch (cond.StopType)
					{
						case ETradeStopTypes.StopLimit:
							request.priceType = "STOP_LIMIT";
							request.limitPrice = (double)order.Price;
							request.stopPrice = (double)cond.StopPrice;
							break;
						case ETradeStopTypes.StopMarket:
							request.priceType = "STOP";
							request.stopPrice = (double)cond.StopPrice;
							break;
					}
					break;
				}
				case OrderTypes.Limit:
					request.limitPrice = (double)order.Price;
					request.priceType = "LIMIT";
					break;
				case OrderTypes.Market:
					request.priceType = "MARKET";
					break;
				default:
					throw new InvalidOperationException(LocalizedStrings.Str1849Params.Put(order.Type));
			}
		}
	}
}