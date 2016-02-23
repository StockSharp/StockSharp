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
	using System.Net;
	using System.Security;
	using System.Security.Cryptography;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Web;

	using Newtonsoft.Json;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class HttpClient : BaseLogReceiver
	{
		private readonly int _clientId;
		private readonly SecureString _key;
		private readonly HashAlgorithm _hasher;

		private int _nonce;

		private readonly List<DateTime> _requestTimes = new List<DateTime>();
		private const int _maxRequestsPer10Min = 600 - 5;
		private static readonly TimeSpan _10Min = TimeSpan.FromMinutes(10);

		public HttpClient(int clientId, SecureString key, SecureString secret)
		{
			_clientId = clientId;
			_key = key;
			_hasher = new HMACSHA256(Encoding.ASCII.GetBytes(secret.To<string>() ?? string.Empty));
			_nonce = (int)(DateTime.UtcNow - TimeHelper.GregorianStart).TotalSeconds;
		}

		// после обфускации название типа нечитаемо
		public override string Name => "BitstampClient";

		public Ticker RequestBtcUsd()
		{
			return MakeRequest<Ticker>("https://www.bitstamp.net/api/ticker/".To<Uri>());
		}

		public ConversionRate RequestEurUsd()
		{
			return MakeRequest<ConversionRate>("https://www.bitstamp.net/api/eur_usd/".To<Uri>());
		}

		public IEnumerable<Transaction> RequestTransactions()
		{
			return MakeRequest<IEnumerable<Transaction>>("https://www.bitstamp.net/api/transactions/".To<Uri>());
		}

		public Balance RequestBalance()
		{
			return MakeRequest<Balance>(ApplySecret(new Url("https://www.bitstamp.net/api/balance/")));
		}

		public IEnumerable<Order> RequestOpenOrders()
		{
			return MakeRequest<IEnumerable<Order>>(ApplySecret(new Url("https://www.bitstamp.net/api/open_orders/")));
		}

		public Order RegisterOrder(Sides side, decimal price, decimal volume)
		{
			var url = new Url("https://www.bitstamp.net/api/{0}/".Put(side.To<string>().ToLowerInvariant()));
			url.QueryString
				.Append("amount", volume)
				.Append("price", price);

			return MakeRequest<Order>(ApplySecret(url));
		}

		public bool CancelOrder(long orderId)
		{
			var url = new Url("https://www.bitstamp.net/api/cancel_order/");
			url.QueryString.Append("id", orderId);

			return MakeRequest<bool>(ApplySecret(url));
		}

		public IEnumerable<UserTransaction> RequestUserTransactions()
		{
			var url = new Url("https://www.bitstamp.net/api/user_transactions/");
			return MakeRequest<IEnumerable<UserTransaction>>(ApplySecret(url));
		}

		private Url ApplySecret(Url url)
		{
			var signature = _hasher
				.ComputeHash(Encoding.UTF8.GetBytes(_nonce + _clientId + _key.To<string>()))
				.Digest()
				.ToUpperInvariant();
		
			url.QueryString
				.Append("key", _key)
				.Append("signature", signature)
				.Append("nonce", _nonce);

			_nonce++;

			return url;
		}

		private T MakeRequest<T>(Uri url)
		{
			var now = DateTime.Now;

			if (_requestTimes.Count >= _maxRequestsPer10Min)
			{
				var firstTime = _requestTimes[0];

				var elapsed = now - firstTime;
				if (elapsed <= _10Min)
					throw new InvalidOperationException(LocalizedStrings.Str3310Params
						.Put(elapsed.TotalMinutes, _requestTimes.Count));

				_requestTimes.RemoveWhere(t => t < (now - _10Min));
			}

			_requestTimes.Add(now);

			using (var client = new WebClient())
			{
				var txt = client.DownloadString(url);
				this.AddDebugLog("Request '{0}' Response '{1}'.", url, txt);
				return txt.IsEmpty() ? default(T) : JsonConvert.DeserializeObject<T>(txt);
			}
		}
	}
}