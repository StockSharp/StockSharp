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

	/// <summary>
	/// Объект этого класса отвечает за общение с биржей BTCE.
	/// На одну пару ключ-секрет надо создавать один объект из-за параметра биржи "nonce".
	/// </summary>
	internal class BtceClient : BaseLogReceiver
	{
		#region конструктор

		// приватный Ключ для подписи запросов
		private readonly SecureString _key;
		// подписчик запросов на основе Секрета
		private readonly HashAlgorithm _hasher;

		/// <summary>
		/// Конструктор объекта
		/// </summary>
		/// <param name="key">API-ключ. NB не храните в коде!</param>
		/// <param name="secret">Секрет для подписи запросов. NB не храните в коде!</param>
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
		}

		// после обфускации название типа нечитаемо
		public override string Name
		{
			get { return "BtceClient"; }
		}

		protected override void DisposeManaged()
		{
			_hasher.Dispose();
			base.DisposeManaged();
		}

		private volatile IDictionary<string, InstrumentInfo> _instruments;

		#endregion

		//#region tutty-frutty
		//// на случай, чтобы можно было отличать один аккаунт BTCE от другого
		//public string Key { get { return _key; } }

		//// с объектом можно связать какие-то свои данные
		//public object Tag { get; set; }
		//#endregion

		#region nonce

		// проверил, BTCE значения больше 4294967295 не принимает
		// если равен -1, значит еще не запрашивали у биржи
		private long _nonce = -1;

		// отдает следующий nonce
		private long NextNonce()
		{
			return (uint)Interlocked.Add(ref _nonce, 1);
		}

		private long _nonceProblems;

		/// <summary>
		/// Возвращает количество проблемных запросов.
		/// Мы послали запрос с определенным nonce, а сервер сказал, что nonce неправильный.
		/// И приходится пересылать запрос еще раз с новым nonce.
		/// </summary>
		public long NonceProblemCount
		{
			get { return _nonceProblems; }
		}

		#endregion

		#region getInfo

		/// <summary>
		/// Возвращает ИНФО от биржи.
		/// NB: Должен быть первым запросом к бирже, поскольку настраивает nonce.
		/// </summary>
		public InfoReply GetInfo()
		{
			var res = JsonConvert.DeserializeObject<InfoReply>(MakePrivateRequest("method=getInfo"));

			if (!res.Success)
				throw new InvalidOperationException(res.ErrorText);

			return res;
		}

		#endregion

		#region transactions

		/// <summary>
		/// Возвращает транзакции на счете с последнего полного месяца.
		/// Если сегодня 10 марта, то полный месяц начинается с 1 февраля.
		/// </summary>
		public TransactionsReply GetTransactions()
		{
			var full = FullMonth(DateTime.Now);
			return GetTransactions(full);
		}

		/// <summary>
		/// Возвращает транзакции на счете с некоторой даты.
		/// </summary>
		/// <param name="since">Дата, с которой вернуть транзакции.</param>
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

		///// <summary>
		///// Возвращает сделки на счете по всем инструментам с последнего полного месяца.
		///// </summary>
		//public MyTradesReply GetMyTrades()
		//{
		//	var full = FullMonth(DateTime.Now);
		//	return GetMyTrades(full);
		//}

		/// <summary>
		/// Возвращает свои сделки на счете месяц по всем инструментам c определенной даты.
		/// </summary>
		/// <param name="fromId">Номер сделки, с которой нужны новые сделки.</param>
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

		/// <summary>
		/// Возвращает свои сделки на счете месяц по конкретному инструменту с последнего полного месяца.
		/// </summary>
		/// <param name="instrument">Интересующий инструмент (BTC_RUR).</param>
		public MyTradesReply GetMyTrades(string instrument)
		{
			var full = FullMonth(DateTime.Now);
			return GetMyTrades(instrument, full);
		}

		/// <summary>
		/// Возвращает свои сделки на счете месяц по конкретному инструменту c определенной даты.
		/// </summary>
		/// <param name="instrument">Интересующий инструмент (BTC_RUR).</param>
		/// <param name="since">Дата, с которой нужны сделки.</param>
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

		/// <summary>
		/// Возвращает заявки на счете месяц по всем инструментам.
		/// </summary>
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

		/// <summary>
		/// Возвращает заявки на счете месяц по конкретному инструменту.
		/// </summary>
		/// <param name="instrument">Интересующий инструмент (BTC_RUR).</param>
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

		/// <summary>
		/// Создает заявку новую.
		/// </summary>
		/// <param name="instrument">Инструмент.</param>
		/// <param name="side">Тип: sell или buy.</param>
		/// <param name="price">Цена.</param>
		/// <param name="volume">Объем.</param>
		public CommandReply MakeOrder(string instrument,
			string side,
			decimal price,
			decimal volume)
		{
			if (price < 0)
				throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str3343);

			if (volume <= 0)
				throw new ArgumentOutOfRangeException("volume", volume, LocalizedStrings.Str3344);

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

		/// <summary>
		/// Отменяет заявку.
		/// </summary>
		/// <param name="orderId">Идентификатор заявки.</param>
		public CommandReply CancelOrder(long orderId)
		{
			return CancelOrder(new Command { OrderId = orderId });
		}

		/// <summary>
		/// Отменяет заявку.
		/// </summary>
		/// <param name="command">Параметры отмены, где обязательным является OrderId.</param>
		public CommandReply CancelOrder(Command command)
		{
			if (command == null)
				throw new ArgumentNullException("command");
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
				throw new ArgumentOutOfRangeException("depth");

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
				throw new ArgumentOutOfRangeException("count");

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

		/// <summary>
		/// Запрашивает BTCE приватно, предварительно подписав запрос.
		/// </summary>
		/// <param name="request">Параметры запроса. без nonce, который добавится автоматически.</param>
		/// <returns></returns>
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

		/// <summary>
		/// Запрашивает открытое АПИ BTCE.
		/// </summary>
		/// <param name="request">Параметры запроса</param>
		/// <returns></returns>
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