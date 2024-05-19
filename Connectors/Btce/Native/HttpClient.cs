#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Btce.Native.Btce
File: BtceClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Btce.Native
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Net;
	using System.Security;
	using System.Security.Cryptography;

	using Newtonsoft.Json;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Localization;
	using StockSharp.Messages;

	class HttpClient : BaseLogReceiver
	{
		private readonly SecureString _key;
		private readonly HashAlgorithm _hasher;

		private readonly UTCIncrementalIdGenerator _nonceGen;

		private readonly string _btcePublicApi;
		private readonly string _btcePrivateApi;

		public HttpClient(string domain, SecureString key, SecureString secret)
		{
			if (!domain.StartsWithIgnoreCase("http"))
				domain = "https://" + domain;

			if (domain.EndsWith("/"))
				domain = domain.Substring(0, domain.Length - 1);

			_btcePublicApi = $"{domain}/api/3/";
			_btcePrivateApi = $"{domain}/tapi";

			_key = key;
			_hasher = secret.IsEmpty() ? null : new HMACSHA512(secret.UnSecure().ASCII());

			_nonceGen = new UTCIncrementalIdGenerator();
		}

		protected override void DisposeManaged()
		{
			_hasher?.Dispose();
			base.DisposeManaged();
		}

		private volatile IDictionary<string, InstrumentInfo> _instruments;

		//// количество проблемных запросов.
		//// Мы послали запрос с определенным nonce, а сервер сказал, что nonce неправильный.
		//// И приходится пересылать запрос еще раз с новым nonce.
		//private long _nonceProblems;

		//public long NonceProblemCount => _nonceProblems;

		#region getInfo

		public InfoReply GetInfo()
		{
			// Должен быть первым запросом к бирже, поскольку настраивает nonce.
			var res = MakePrivateRequest("method=getInfo").DeserializeObject<InfoReply>();

			if (!res.Success)
				throw new InvalidOperationException(res.ErrorText);

			//Debug.Print( $"SrvTime: {res.State.Timestamp} s:{(res.State.Timestamp - Date1970).TotalSeconds}" );
			return res;
		}

		#endregion

		#region transactions

		public TransactionsReply GetTransactions()
		{
			var full = FullMonth(DateTime.Now);
			return GetTransactions(full);
		}

		public TransactionsReply GetTransactions(DateTime since)
		{
			var args = $"method=TransHistory&since={(long)since.ToUnix()}";
			var res = MakePrivateRequest(args).DeserializeObject<TransactionsReply>();

			// запорлним ID
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			return res;
		}

		#endregion

		#region trades

		public MyTradesReply GetMyTrades(long fromId)
		{
			//var unixtime = (long)(since - Converter.GregorianStart).TotalSeconds;
			var args = $"method=TradeHistory&from_id={fromId}";
			var res = MakePrivateRequest(args).DeserializeObject<MyTradesReply>();

			// заполним идентификаторы сделок
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			if (!res.Success)
			{
				if (res.ErrorText.EqualsIgnoreCase("no trades"))
					res.Success = true;
				else
					throw new InvalidOperationException(res.ErrorText);
			}

			return res;
		}

		public MyTradesReply GetMyTrades(string instrument)
		{
			var full = FullMonth(DateTime.Now);
			return GetMyTrades(instrument, full);
		}

		public MyTradesReply GetMyTrades(string instrument, DateTime since)
		{
			var args = $"method=TradeHistory&pair={instrument.ToLower()}&since={(long)since.ToUnix()}";

			var res = MakePrivateRequest(args).DeserializeObject<MyTradesReply>();

			// заполним идентификаторы сделок
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			return res;
		}

		public MyTradesReply GetMyTrades( DateTime since )
		{
			var args = $"method=TradeHistory&since={(long)since.ToUnix()}";

			var res = MakePrivateRequest(args).DeserializeObject<MyTradesReply>();

			// заполним идентификаторы сделок
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			return res;
		}
		#endregion

		#region orders

		public OrdersReply GetActiveOrders()
		{
			var res = MakePrivateRequest("method=ActiveOrders").DeserializeObject<OrdersReply>();

			// заполним идентификаторы заявок
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			if (!res.Success)
			{
				if (res.ErrorText.EqualsIgnoreCase("no orders"))
					res.Success = true;
				else
					throw new InvalidOperationException(res.ErrorText);	
			}

			return res;
		}

		public OrdersReply GetOrders(string instrument)
		{
			var args = $"method=ActiveOrders&pair={instrument.ToLower()}";
			var res = MakePrivateRequest(args).DeserializeObject<OrdersReply>();

			// заполним идентификаторы заявок
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			return res;
		}

		#endregion

		#region make/cancel order

		public CommandReply MakeOrder(string instrument,
			string side,
			decimal price,
			decimal volume)
		{
			if (price < 0)
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.InvalidValue);

			if (volume <= 0)
				throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.InvalidValue);

			var instr = instrument.ToLower();
			//var dir = command.Direction.ToString().ToLower();

			//command.Received = command.Remains = 0;
			//command.Funds.Clear();

			if (_instruments == null)
				throw new InvalidOperationException("Info about instruments not loaded yet.");

			if (!_instruments.ContainsKey(instr))
				throw new ArgumentOutOfRangeException(nameof(instrument), instr, "Unknown instrument.");

			var args = $"method=Trade&pair={instr}&type={side}&rate={price}&amount={volume}";
				//.Put(
				//instr, side,
				//price/*.ToString("F" + _instruments[instr].DecimalDigits, NumberFormatInfo.InvariantInfo)*/,
				//volume/*.ToString("F8", NumberFormatInfo.InvariantInfo)*/);

			var res = new CommandReply { Command = new Command() };

			JsonConvert.PopulateObject(MakePrivateRequest(args), res);

			if (!res.Success)
				throw new InvalidOperationException(res.ErrorText);

			return res;
		}

		public CommandReply CancelOrder(long orderId)
		{
			return CancelOrder(new Command { OrderId = orderId });
		}

		public CommandReply CancelOrder(Command command)
		{
			if (command == null)
				throw new ArgumentNullException(nameof(command));
			//if (command.OrderId == 0)
			//	throw new ArgumentException("OrderId");

			command.Received = command.Remains = 0;
			command.Funds.Clear();

			var res = new CommandReply { Command = command };

			var args = $"method=CancelOrder&order_id={command.OrderId}";
			JsonConvert.PopulateObject(MakePrivateRequest(args), res);

			if (!res.Success)
				throw new InvalidOperationException(res.ErrorText);

			return res;
		}

		#endregion

		#region instruments

		public InstrumentsReply GetInstruments()
		{
			var res = MakePublicRequest("info").DeserializeObject<InstrumentsReply>();

			// заполним имена
			foreach (var e in res.Items)
				e.Value.Name = e.Key;

			// сохраним инфу об инструментах, потому что на основе нее и будут формироваться заявки
			// там используется DecimalDigits для указания цены, иначе сервер может отругать запрос на заявку
			_instruments = res.Items;

			return res;
		}

		#endregion

		#region ticker

		public TickersReply GetTickers(IEnumerable<string> instruments)
		{
			var res = new TickersReply();

			var instrs = instruments.Join("-");
			var args = $"ticker/{instrs}?ignore_invalid=1";
			JsonConvert.PopulateObject(MakePublicRequest(args), res.Items);

			// заполним имена
			foreach (var e in res.Items)
				e.Value.Instrument = e.Key;

			return res;
		}

		#endregion

		#region depth[ N ]

		public DepthsReply GetDepths(IEnumerable<string> instruments, int? depth = null)
		{
			if (instruments == null)
				throw new ArgumentNullException(nameof(instruments));

			if (depth != null)
			{
				if (depth <= 0)
					throw new ArgumentOutOfRangeException(nameof(depth), depth, LocalizedStrings.InvalidValue);

				depth = Math.Min(depth.Value, 5000);
			}

			var res = new DepthsReply();

			var instrs = instruments.Join("-");
			var args = $"depth/{instrs}?ignore_invalid=1";

			if (depth != null)
				args += $"&limit={depth.Value}";

			JsonConvert.PopulateObject(MakePublicRequest(args), res.Items);

			return res;
		}

		#endregion

		#region trades[ N ]

		public TradesReply GetTrades(int count, IEnumerable<string> instruments)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, LocalizedStrings.InvalidValue);

			count = Math.Min(count, 2000);

			var res = new TradesReply();

			var instrs = instruments.Join("-");
			var args = $"trades/{instrs}?limit={count}&ignore_invalid=1";
			JsonConvert.PopulateObject(MakePublicRequest(args), res.Items);

			// заполним имена инструментов
			foreach (var e in res.Items)
			{
				foreach (var i in e.Value)
					i.Instrument = e.Key;
			}

			return res;
		}

		#endregion

		public long Withdraw(string currency, decimal volume, WithdrawInfo info)
		{
			if (info == null)
				throw new ArgumentNullException(nameof(info));

			switch (info.Type)
			{
				case WithdrawTypes.Crypto:
				{
					if (info.BankDetails == null)
						throw new InvalidOperationException(LocalizedStrings.BankDetailsIsMissing);

					var args = $"method=WithdrawCoin&coinName={currency}&amount={volume}&address={info.CryptoAddress}";

					dynamic res = MakePrivateRequest(args).DeserializeObject<object>();

					if ((int)res.success != 1)
						throw new InvalidOperationException((string)res.error);

					return (long)res.@return.tId;
				}
				default:
					throw new NotSupportedException(LocalizedStrings.WithdrawTypeNotSupported.Put(info.Type));
			}
		}

		#region internals

		private string MakePrivateRequest(string request)
		{
			// выполняем запрос до тех пор, пока не будет проблем с nonce
			// {"success":0,"error":"invalid nonce parameter; on key:50, you sent:0"}
			while (true)
			{
				var wr = WebRequest.Create(_btcePrivateApi);

				wr.Method = "POST";
				wr.Headers.Add("Key", _key.UnSecure());
				wr.ContentType = "application/x-www-form-urlencoded";

				// добавим nonce
				var nonce = _nonceGen.GetNextId();
				var sreq = request + $"&nonce={nonce}";
				var bytes = sreq.UTF8();

				var shmac = _hasher.ComputeHash(bytes).Digest().ToLowerInvariant();
				wr.Headers.Add("Sign", shmac);

				wr.ContentLength = bytes.Length;

				using (var fout = wr.GetRequestStream())
					fout.Write(bytes, 0, bytes.Length);

				var response = GetResponse(wr, request);
				//if (request.Contains( "TradeHistory"))
				//	Debug.Print( $"RESP: {response}" );

				//// если c nonce проблем нет, то вернем результат
				//if (CheckNonce(response))
				//	return response;

				//Interlocked.Increment(ref _nonceProblems);

				return response;
			}
		}

		//private bool CheckNonce(string res)
		//{
		//	if (!res.ContainsIgnoreCase("invalid nonce parameter"))
		//		return true;

		//	// получим текущий nonce
		//	var pos = res.IndexOfIgnoreCase("on key:");
		//	// нет значения nonce? что-то пошло не так
		//	if (pos < 0)
		//	{
		//		//Trace.WriteLine("BTCE say that nonce is invalid, but no value of nonce. " + res);
		//		// попробуем установить текущий unixtime
		//		_nonceGen.Current = (long)TimeHelper.UnixNowS;
		//		return false;
		//	}

		//	pos += 7;
		//	var end = res.IndexOfAny(new[] { ',', '.', ' ', '\t' }, pos);
		//	var nonce = res.Substring(pos, end - pos).To<long>();

		//	// если nonce исчерпали, так и скажем
		//	if (nonce >= uint.MaxValue)
		//		throw new InvalidOperationException("Overflow Nonce. Create new key & secret for connection in BTCE cabinet.");

		//	// установим nonce от сервера только если он больше текущего
		//	while (true)
		//	{
		//		var old = Interlocked.Add(ref _nonce, 0);
		//		if (old > nonce || Interlocked.CompareExchange(ref _nonce, nonce, old) == old)
		//			break;
		//	}

		//	// и скажем, что nonce не был валидным
		//	return false;
		//}

		private string MakePublicRequest(string request)
		{
			return GetResponse(WebRequest.Create(_btcePublicApi + request), request);
		}

		private string GetResponse(WebRequest wr, string url)
		{
			using (var resp = wr.GetResponse())
			using (var inet = resp.GetResponseStream())
			using (var ms = new MemoryStream())
			{
				if (inet == null)
					throw new InvalidOperationException();

				inet.CopyTo(ms);
				var retVal = ms.To<byte[]>().UTF8();
				this.AddDebugLog("Request {0} Response {1}", url, retVal);
				return retVal;
			}
		}

		// возвращает дату с 1-ым числом последнего полного месяца
		// если сегодня 10марта, то вернет 1февраля
		private static DateTime FullMonth(DateTime date)
		{
			int year = date.Year, month = date.Month;

			if (--month < 1)
			{
				--year;
				month = 12;
			}

			return new DateTime(year, month, 1);
		}

		#endregion
	}
}