namespace StockSharp.Oanda.Native
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Web;
	using Ecng.Common;

	using Newtonsoft.Json;

	using RestSharp;

	using StockSharp.Localization;
	using StockSharp.Oanda.Native.Communications;
	using StockSharp.Oanda.Native.DataTypes;

	class OandaRestClient
	{
		private readonly SecureString _token;
		private readonly string _restUrl;

		public OandaRestClient(OandaServers server, SecureString token)
		{
			switch (server)
			{
				case OandaServers.Sandbox:
					if (token != null)
						throw new ArgumentException("token");

					_restUrl = "http://api-sandbox.oanda.com";
					break;
				case OandaServers.Practice:
					if (token == null)
						throw new ArgumentNullException("token");

					_restUrl = "https://api-fxpractice.oanda.com";
					break;
				case OandaServers.Real:
					if (token == null)
						throw new ArgumentNullException("token");

					_restUrl = "https://api-fxtrade.oanda.com";
					break;
				default:
					throw new ArgumentOutOfRangeException("server");
			}

			_token = token;
		}

		public IEnumerable<TradeData> GetTrades(int accountId, int? maxId = null)
		{
			var url = CreateUrl("accounts/{0}/trades".Put(accountId));

			if (maxId != null)
				url.QueryString.Append("maxId", maxId);

			return MakeRequest<TradesResponse>(url).Trades;
		}

		public IEnumerable<Order> GetOrders(int accountId, int? maxId = null)
		{
			var url = CreateUrl("accounts/{0}/orders".Put(accountId));

			if (maxId != null)
				url.QueryString.Append("maxId", maxId);

			return MakeRequest<OrdersResponse>(url).Orders;
		}

		public IEnumerable<Transaction> GetTransactions(int accountId, int? minId = null, int? maxId = null)
		{
			var url = CreateUrl("accounts/{0}/transactions".Put(accountId));

			url.QueryString
				.Append("minId", minId)
				.Append("maxId", maxId);

			return MakeRequest<TransactionsResponse>(url).Transactions;
		}

		public IEnumerable<Account> GetAccounts()
		{
			return MakeRequest<AccountsResponse>(CreateUrl("accounts")).Accounts;
		}

		/// <summary>
		/// Gets account specific details for the given account
		/// </summary>
		/// <param name="accountId">the ID of the account to retrieve</param>
		/// <returns>the AccountDetails for the account</returns>
		public AccountDetails GetAccountDetails(int accountId)
		{
			return MakeRequest<AccountDetails>(CreateUrl("accounts/{0}".PutEx(accountId)));
		}

		/// <summary>
		/// Get the current open positions for the account specified
		/// </summary>
		/// <param name="accountId">the ID of the account</param>
		/// <returns>list of positions (or empty list if there are none)</returns>
		public IEnumerable<Position> GetPositions(int accountId)
		{
			return MakeRequest<PositionsResponse>(CreateUrl("accounts/{0}/positions".Put(accountId))).Positions;
		}

		public IEnumerable<Candle> GetCandles(string instrument, string timeFrame, long count, long begin)
		{
			var url = CreateUrl("candles");

			url.QueryString
				.Append("instrument", instrument)
				.Append("granularity", timeFrame)
				.Append("candleFormat", "midpoint");

			if (count > 0)
				url.QueryString.Append("count", count);

			if (begin > 0)
				url.QueryString.Append("start", begin);

			// can raise max candle count (5k) overflow error
			//if (!end.IsEmpty())
			//	url.QueryString.Append("end", end);

			var response = MakeRequest<CandlesResponse>(url);

			return response == null ? Enumerable.Empty<Candle>() : response.Candles;
		}

		public CreateOrderResponse CreateOrder(int accountId, string instrument, int units, string side,
			string type, long expiry, decimal price, decimal? lowerBound, decimal? upperBound,
			decimal? stopLoss, decimal? takeProfit, int? trailingStop)
		{
			var url = CreateUrl("accounts/{0}/orders".Put(accountId));

			return MakeRequest<CreateOrderResponse>(url, Method.POST, r => r
				.AddParameter("instrument", instrument)
				.AddParameter("units", units)
				.AddParameter("side", side)
				.AddParameter("type", type)
				.AddParameter("expiry", expiry)
				.AddParameter("price", price)
				.AddParameterIfNotNull("lowerBound", lowerBound)
				.AddParameterIfNotNull("upperBound", upperBound)
				.AddParameterIfNotNull("stopLoss", stopLoss)
				.AddParameterIfNotNull("takeProfit", takeProfit)
				.AddParameterIfNotNull("trailingStop", trailingStop));
		}

		public Order CloseOrder(int accountId, long orderId)
		{
			return MakeRequest<Order>(CreateUrl("accounts/{0}/orders/{1}".Put(accountId, orderId)), Method.DELETE);
		}

		public Order ModifyOrder(int accountId, long orderId, int units, long expiry, decimal price,
			decimal? lowerBound, decimal? upperBound, decimal? stopLoss, decimal? takeProfit,
			int? trailingStop)
		{
			var url = CreateUrl("accounts/{0}/orders/{1}".Put(accountId, orderId));

			return MakeRequest<Order>(url, Method.PATCH, r => r
				.AddParameter("units", units)
				.AddParameter("expiry", expiry)
				.AddParameter("price", price)
				.AddParameterIfNotNull("lowerBound", lowerBound)
				.AddParameterIfNotNull("upperBound", upperBound)
				.AddParameterIfNotNull("stopLoss", stopLoss)
				.AddParameterIfNotNull("takeProfit", takeProfit)
				.AddParameterIfNotNull("trailingStop", trailingStop));
		}

		/// <summary>
		/// Gets the list of instruments that are available
		/// </summary>
		/// <returns>a list of the available instruments</returns>
		public IEnumerable<Instrument> GetInstruments(int accountId, IEnumerable<string> instruments)
		{
			var url = CreateUrl("instruments");

			url.QueryString.Append("accountId", accountId);

			var instrumentsField = instruments.Join(",");
			
			if (!instrumentsField.IsEmpty())
				url.QueryString.Append("instruments", instrumentsField);

			return MakeRequest<InstrumentsResponse>(url).Instruments;
		}

		/// <summary>
		/// Gets the current rates for the given instruments
		/// </summary>
		/// <param name="instruments">The list of instruments to request</param>
		/// <returns>The list of prices</returns>
		public IEnumerable<Price> GetRates(IEnumerable<string> instruments)
		{
			var url = CreateUrl("prices");

			url.QueryString
				.Append("instruments", instruments.Join(","));

			return MakeRequest<PricesResponse>(url).Prices;
		}

		public IEnumerable<Calendar> GetCalendar(string instrument, int hours)
		{
			var url = CreateUrl("calendar");

			url.QueryString
				.Append("instrument", instrument)
				.Append("period", hours);

			return MakeRequest<CalendarsResponse>(url).Calendars;
		}

		private Url CreateUrl(string name)
		{
			return new Url(_restUrl + "/v1/" + name);
		}

		private T MakeRequest<T>(Uri url, Method method = Method.GET, Action<RestRequest> fillRequest = null)
			where T : class
		{
			var client = new RestClient { BaseUrl = _restUrl };
			var request = new RestRequest(url.PathAndQuery, method);

			// for non-sandbox requests
			if (_token != null)
				request.AddHeader("Authorization", "Bearer " + _token.To<string>());

			request.AddHeader("X-Accept-Datetime-Format", "UNIX");

			fillRequest.SafeInvoke(request);

			var response = client.Execute(request);

			if (response.ErrorException != null)
				throw response.ErrorException;

			if (response.StatusCode == HttpStatusCode.NoContent)
				return null;

			if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
				return JsonConvert.DeserializeObject<T>(response.Content);

			if (response.Content.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.Str3458Params.Put(response.StatusCode));

			var error = JsonConvert.DeserializeObject<ErrorResponse>(response.Content);

			throw new InvalidOperationException(error.Message);
		}
	}
}