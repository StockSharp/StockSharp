
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;
using SciTrader.Model;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using RestSharp;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Ecng.Common;
using Ecng.Net;
using Ecng.Collections;
using RestSharp.Extensions;
using StockSharp.Logging;
using Ecng.Serialization;

namespace SciTader.Network
{
    /// <summary>
    /// Sides.
    /// </summary>
    [Serializable]
    public enum Sides
    {
        /// <summary>
        /// Buy.
        /// </summary>
        Buy,

        /// <summary>
        /// Sell.
        /// </summary>
        Sell,
    }

    public enum OrderTypes
    {
        /// <summary>
        /// Limit.
        /// </summary>
        Limit,

        /// <summary>
        /// Market.
        /// </summary>
        Market,

        /// <summary>
        /// Conditional (stop-loss, take-profit).
        /// </summary>
        Conditional,
    }
    class DarkHorseRestClient : BaseLogReceiver
    {
		private readonly SecureString _key;
		private readonly HMACSHA256 _hasher;

		public DarkHorseRestClient(SecureString key, SecureString secret)
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
		public override string Name => "DarkHorse" + "_" + nameof(HttpClient);

		public Task<List<Market>> GetMarkets(CancellationToken cancellationToken)
		{
			return ProcessRequest<List<Market>>(Method.Get, "api/markets", default, cancellationToken);
		}

		public Task<List<Symbol>> GetSymbols(CancellationToken cancellationToken)
		{
			return ProcessRequest<List<Symbol>>(Method.Get, "api/symbols", default, cancellationToken);
		}

		public Task<List<Trade>> GetMarketTrades(string requestID, string symbol, DateTime start, DateTime end, CancellationToken cancellationToken)
		{
			return ProcessRequest<List<Trade>>(Method.Get, $"api/markets/trades?request_id={requestID}&symbol={symbol}&start_time={start}&end_time={end}", default, cancellationToken);
		}

		public Task<List<Candle>> GetMarketCandles(string symbol, TimeSpan resolution, DateTime start, DateTime end, CancellationToken cancellationToken)
		{
			return ProcessRequest<List<Candle>>(Method.Get, $"api/markets/candles?symbol={symbol}&resolution={resolution.TotalMinutes}&start_time={start}&end_time={end}", default, cancellationToken);
		}

		public async Task<(List<Order> histOrders, bool hasMoreData)> GetMarketOrderHistoryAndHasMoreOrders(string subaccountName, DateTime startTime, CancellationToken cancellationToken)
		{
			var response = await ProcessSignedRequest<List<Order>, DarkHorseRestResponseHasMoreData<List<Order>>>(Method.Get, $"api/orders/history?start_time={startTime}", subaccountName, default, cancellationToken);
			return (response.Result, response.HasMoreData);
		}

		public Task<List<Balance>> GetBalances(string accountName, CancellationToken cancellationToken)
		{
			return ProcessSignedRequest<List<Balance>>(Method.Get, "api/balances", accountName, default, cancellationToken);
		}

		public Task<List<Account>> GetAccounts(string accountName, CancellationToken cancellationToken)
		{
			return ProcessRequest<List<Account>>(Method.Get, $"api/accounts?account={accountName}", default, cancellationToken);
		}

		public Task<List<Balance>> GetPortfolio(string accountName, CancellationToken cancellationToken)
		{
			return ProcessSignedRequest<List<Balance>>(Method.Get, "api/portfolio", accountName, default, cancellationToken);
		}

		public Task<List<Futures>> GetFuturesPositions(string accountName, CancellationToken cancellationToken)
		{
			return ProcessSignedRequest<List<Futures>>(Method.Get, "api/positions", accountName, default, cancellationToken);
		}

		public Task<Order> RegisterOrder(string symbol, Sides side, decimal? price, OrderTypes orderType, decimal amount, string clientId, string accountName, CancellationToken cancellationToken)
		{
			var body =
				$"{{\"market\": \"{symbol}\"," +
				$"\"side\": \"{side.ToString().ToLower()}\"," +
				(price.HasValue ? $"\"price\": {price.Value}," : "\"price\": null,") +
				$"\"type\": \"{orderType.ToString().ToLower()}\"," +
				$"\"size\": {amount}," +
				$"\"clientId\": {(clientId.Length == 0 ? "null" : $"\"{clientId}\"")}}}";
			return ProcessSignedRequest<Order>(Method.Post, "api/orders", accountName, body, cancellationToken);
		}

		public async Task<bool> CancelOrder(long id, string accountName, CancellationToken cancellationToken)
		{
			var result = await ProcessSignedRequest<object>(Method.Delete, $"api/orders/{id}", accountName, default, cancellationToken);
			return result != null;
		}

		public async Task<bool> CancelAllOrders(string accountName, CancellationToken cancellationToken)
		{
			var result = await ProcessSignedRequest<object>(Method.Delete, "api/orders", accountName, default, cancellationToken);
			return result != null;
		}

		public Task<List<Order>> GetOpenOrders(string accountName, CancellationToken cancellationToken)
		{
			return ProcessRequest<List<Order>>(Method.Get, $"api/orders?account={accountName}", default, cancellationToken);
		}

		public Task<List<Fill>> GetFills(DateTime start, DateTime end, string accountName, CancellationToken cancellationToken)
		{
			return ProcessSignedRequest<List<Fill>>(Method.Get, $"api/fills?start_time={GetSecondsFromEpochStart(start)}&end_time={GetSecondsFromEpochStart(end)}", accountName, default, cancellationToken);
		}

		#region Util
		private static long GetMillisecondsFromEpochStart()
		{
			return GetMillisecondsFromEpochStart(DateTime.UtcNow);
		}

		private static long GetMillisecondsFromEpochStart(DateTime time)
		{
			if (time <= TimeHelper.GregorianStart)
				return 0;

			return (long)time.ToUnix(false);
		}

		private static long GetSecondsFromEpochStart(DateTime time)
		{
			if (time <= TimeHelper.GregorianStart)
				return 0;

			return (long)time.ToUnix();
		}

		private static Uri GetUri(string endpoint)
		{
			return new Uri($"http://localhost:80/{endpoint}");
		}

		private Task<dynamic> ProcessRequest(Method method, string endpoint, string jsonBody, CancellationToken cancellationToken)
		{
			var request = new RestRequest((string)null, method);
			// Set timeout to 10 seconds
			TimeSpan timeout = TimeSpan.FromSeconds(10);
			request.Timeout = timeout;
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
			DarkHorseRestResponse<T> restResponse = Parse<DarkHorseRestResponse<T>>(response);

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
			request.AddHeader("SI-KEY", _key.UnSecure());
			request.AddHeader("SI-SIGN", sign);
			request.AddHeader("SI-TS", nonce.ToString());

			if (!subaccountName.IsEmpty())
			{
				request.AddHeader("SI-SUBACCOUNT", subaccountName.EncodeUrl());
			}

			return request.InvokeAsync(GetUri(endpoint), this, this.AddVerboseLog, cancellationToken);
		}

		private async Task<T> ProcessSignedRequest<T>(Method method, string endpoint, string subaccountName, string jsonBody, CancellationToken cancellationToken)
			where T : class
		{
			dynamic response = await ProcessSignedRequest(method, endpoint, subaccountName, jsonBody, cancellationToken);
			DarkHorseRestResponse<T> restResponse = Parse<DarkHorseRestResponse<T>>(response);
			if (restResponse == null) return null;

			if (restResponse.Success)
			{
				return restResponse.Result;
			}
			return null;
		}

		private async Task<T1> ProcessSignedRequest<T, T1>(Method method, string endpoint, string subaccountName, string jsonBody, CancellationToken cancellationToken)
			where T : class
			where T1 : DarkHorseRestResponse<T>
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
}
