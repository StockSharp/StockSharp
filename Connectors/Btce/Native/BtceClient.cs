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
	using System.Text;
	using System.IO;
	using System.Threading;
	using System.Globalization;
	using System.Net;
	using System.Security;
	using System.Security.Cryptography;

	using Newtonsoft.Json;

	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Localization;

	class BtceClient : BaseLogReceiver
	{
		private readonly SecureString _key;
		private readonly HashAlgorithm _hasher;

		public BtceClient(SecureString key, SecureString secret)
		{
			//if (key == null)
			//	throw new ArgumentNullException("key");

			//if (secret == null)
			//	throw new ArgumentNullException("secret");

			_key = key.To<SecureString>();
			_hasher = new HMACSHA512(Encoding.ASCII.GetBytes(secret.To<string>() ?? string.Empty));

			// до 2038 года работать будет
			//_nonce = ( int )(DateTime.UtcNow - new DateTime( 1970, 1, 1 )).TotalSeconds;

			// по умолчанию к одному серверу обращается .DefaultPersistentConnectionLimit(2) запроса
			// остальные ставятся в очередь
			// мы можем одновременно запросить и стаканы и сделки и отменить заявку и т.д и т.п.
			// если никто не менял, то установим в наше значение
			//if (ServicePointManager.DefaultConnectionLimit == ServicePointManager.DefaultPersistentConnectionLimit)
			//	ServicePointManager.DefaultConnectionLimit = 7;

			_nonce = (int)(DateTime.UtcNow - TimeHelper.GregorianStart).TotalSeconds;
		}

		protected override void DisposeManaged()
		{
			_hasher.Dispose();
			base.DisposeManaged();
		}

		private volatile IDictionary<string, InstrumentInfo> _instruments;

		// проверил, BTCE значения больше 4294967295 не принимает
		// если равен -1, значит еще не запрашивали у биржи
		private long _nonce;

		// отдает следующий nonce
		private long NextNonce()
		{
			return (uint)Interlocked.Increment(ref _nonce);
		}

		// количество проблемных запросов.
		// Мы послали запрос с определенным nonce, а сервер сказал, что nonce неправильный.
		// И приходится пересылать запрос еще раз с новым nonce.
		private long _nonceProblems;

		public long NonceProblemCount
		{
			get { return _nonceProblems; }
		}

		#region getInfo

		public InfoReply GetInfo()
		{
			// Должен быть первым запросом к бирже, поскольку настраивает nonce.

			var res = JsonConvert.DeserializeObject<InfoReply>(MakePrivateRequest("method=getInfo"));

			if (!res.Success)
				throw new InvalidOperationException(res.ErrorText);

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
			var args = "method=TransHistory&since={0}".Put((long)(since - TimeHelper.GregorianStart).TotalSeconds);
			var res = JsonConvert.DeserializeObject<TransactionsReply>(MakePrivateRequest(args));

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
			var args = "method=TradeHistory&from_id={0}".Put(fromId);
			var res = JsonConvert.DeserializeObject<MyTradesReply>(MakePrivateRequest(args));

			// заполним идентификаторы сделок
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			if (!res.Success)
			{
				if (res.ErrorText.CompareIgnoreCase("no trades"))
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
			var args = "method=TradeHistory&pair={0}&since={1}"
				.Put(instrument.ToLower(), (long)(since - TimeHelper.GregorianStart).TotalSeconds);

			var res = JsonConvert.DeserializeObject<MyTradesReply>(MakePrivateRequest(args));

			// заполним идентификаторы сделок
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			return res;
		}

		#endregion

		#region orders

		public OrdersReply GetOrders()
		{
			var res = JsonConvert.DeserializeObject<OrdersReply>(MakePrivateRequest("method=ActiveOrders"));

			// заполним идентификаторы заявок
			foreach (var e in res.Items)
				e.Value.Id = e.Key;

			if (!res.Success)
			{
				if (res.ErrorText.CompareIgnoreCase("no orders"))
					res.Success = true;
				else
					throw new InvalidOperationException(res.ErrorText);	
			}

			return res;
		}

		public OrdersReply GetOrders(string instrument)
		{
			var args = "method=ActiveOrders&pair={0}".Put(instrument.ToLower());
			var res = JsonConvert.DeserializeObject<OrdersReply>(MakePrivateRequest(args));

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
				throw new ArgumentOutOfRangeException(nameof(price), price, LocalizedStrings.Str3343);

			if (volume <= 0)
				throw new ArgumentOutOfRangeException(nameof(volume), volume, LocalizedStrings.Str3344);

			var instr = instrument.ToLower();
			//var dir = command.Direction.ToString().ToLower();

			//command.Received = command.Remains = 0;
			//command.Funds.Clear();

			if (_instruments == null)
				throw new InvalidOperationException("Info about instruments not loaded yet.");

			if (!_instruments.ContainsKey(instr))
				throw new ArgumentException("Unknown instrument.");

			var args = "method=Trade&pair={0}&type={1}&rate={2}&amount={3}".Put(
				instr, side,
				price.ToString("F" + _instruments[instr].DecimalDigits, NumberFormatInfo.InvariantInfo),
				volume.ToString("F8", NumberFormatInfo.InvariantInfo));

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
			if (command.OrderId == 0)
				throw new ArgumentException("OrderId");

			command.Received = command.Remains = 0;
			command.Funds.Clear();

			var res = new CommandReply { Command = command };

			var args = "method=CancelOrder&order_id={0}".Put(command.OrderId);
			JsonConvert.PopulateObject(MakePrivateRequest(args), res);

			if (!res.Success)
				throw new InvalidOperationException(res.ErrorText);

			return res;
		}

		#endregion

		#region instruments

		public InstrumentsReply GetInstruments()
		{
			var res = JsonConvert.DeserializeObject<InstrumentsReply>(MakePublicRequest("info?ignore_invalid=1"));

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

			var args = "ticker/{0}?ignore_invalid=1".Put(instruments.Join("-"));
			JsonConvert.PopulateObject(MakePublicRequest(args), res.Items);

			// заполним имена
			foreach (var e in res.Items)
				e.Value.Instrument = e.Key;

			return res;
		}

		#endregion

		#region depth[ N ]

		public DepthsReply GetDepths(int depth, IEnumerable<string> instruments)
		{
			if (depth <= 0)
				throw new ArgumentOutOfRangeException(nameof(depth));

			depth = Math.Min(depth, 2000);

			var res = new DepthsReply();

			var instrs = instruments.Join("-");
			var args = "depth/{0}?limit={1}&ignore_invalid=1".Put(instrs, depth);
			JsonConvert.PopulateObject(MakePublicRequest(args), res.Items);

			return res;
		}

		#endregion

		#region trades[ N ]

		public TradesReply GetTrades(int count, IEnumerable<string> instruments)
		{
			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			count = Math.Min(count, 2000);

			var res = new TradesReply();

			var instrs = instruments.Join("-");
			var args = "trades/{0}?limit={1}&ignore_invalid=1".Put(instrs, count);
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

		#region internals

		private const string _btcePublicApi = @"https://btc-e.com/api/3/";
		private const string _btcePrivateApi = @"https://btc-e.com/tapi";

		private string MakePrivateRequest(string request)
		{
			// выполняем запрос до тех пор, пока не будет проблем с nonce
			// {"success":0,"error":"invalid nonce parameter; on key:50, you sent:0"}
			while (true)
			{
				var wr = WebRequest.Create(_btcePrivateApi);

				wr.Method = "POST";
				wr.Headers.Add("Key", _key.To<string>());
				wr.ContentType = "application/x-www-form-urlencoded";

				// добавим nonce
				var nonce = NextNonce();
				var sreq = request + "&nonce={0}".Put(nonce);
				var bytes = Encoding.UTF8.GetBytes(sreq);

				var shmac = _hasher.ComputeHash(bytes).Digest().ToLowerInvariant();
				wr.Headers.Add("Sign", shmac);

				wr.ContentLength = bytes.Length;

				using (var fout = wr.GetRequestStream())
					fout.Write(bytes, 0, bytes.Length);

				var response = GetResponse(wr, request);

				// если c nonce проблем нет, то вернем результат
				if (CheckNonce(response))
					return response;

				Interlocked.Increment(ref _nonceProblems);
			}
		}

		private bool CheckNonce(string res)
		{
			if (!res.ContainsIgnoreCase("invalid nonce parameter"))
				return true;

			// получим текущий nonce
			var pos = res.IndexOf("on key:", StringComparison.InvariantCultureIgnoreCase);
			// нет значения nonce? что-то пошло не так
			if (pos < 0)
			{
				//Trace.WriteLine("BTCE say that nonce is invalid, but no value of nonce. " + res);
				// попробуем установить текущий unixtime
				Interlocked.Exchange(ref _nonce, (long)(DateTime.UtcNow - TimeHelper.GregorianStart).TotalSeconds);
				return false;
			}

			pos += 7;
			var end = res.IndexOfAny(new[] { ',', '.', ' ', '\t' }, pos);
			var nonce = res.Substring(pos, end - pos).To<long>();

			// если nonce исчерпали, так и скажем
			if (nonce >= uint.MaxValue)
				throw new InvalidOperationException("Overflow Nonce. Create new key & secret for connection in BTCE cabinet.");

			// установим nonce от сервера только если он больше текущего
			while (true)
			{
				var old = Interlocked.Add(ref _nonce, 0);
				if (old > nonce || Interlocked.CompareExchange(ref _nonce, nonce, old) == old)
					break;
			}

			// и скажем, что nonce не был валидным
			return false;
		}

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
				var retVal = Encoding.UTF8.GetString(ms.ToArray());
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