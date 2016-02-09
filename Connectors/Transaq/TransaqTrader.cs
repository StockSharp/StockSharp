#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Transaq
File: TransaqTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение к Transaq через TXmlConnector.
	/// </summary>
	[Icon("Transaq_logo.png")]
	public class TransaqTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();

		/// <summary>
		/// Создать <see cref="TransaqTrader"/>.
		/// </summary>
		public TransaqTrader()
		{
			Adapter.InnerAdapters.Add(new TransaqMessageAdapter(TransactionIdGenerator));
		}

		private TransaqMessageAdapter NativeAdapter => Adapter.InnerAdapters.OfType<TransaqMessageAdapter>().First();

		/// <summary>
		/// Пароль.
		/// </summary>
		public string Password
		{
			get { return NativeAdapter.Password.To<string>(); }
			set { NativeAdapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Логин.
		/// </summary>
		public string Login
		{
			get { return NativeAdapter.Login; }
			set { NativeAdapter.Login = value; }
		}

		/// <summary>
		/// Адрес сервера.
		/// </summary>
		public EndPoint Address
		{
			get { return NativeAdapter.Address; }
			set { NativeAdapter.Address = value; }
		}

		/// <summary>
		/// Прокси.
		/// </summary>
		public Proxy Proxy
		{
			get { return NativeAdapter.Proxy; }
			set { NativeAdapter.Proxy = value; }
		}

		/// <summary>
		/// Уровень логирования коннектора. По умолчанию <see cref="ApiLogLevels.Standard"/>.
		/// </summary>
		public ApiLogLevels ApiLogLevel
		{
			get { return NativeAdapter.ApiLogLevel; }
			set { NativeAdapter.ApiLogLevel = value; }
		}

		/// <summary>
		/// Полный путь к dll файлу, содержащее Transaq API. По-умолчанию равно txmlconnector.dll.
		/// </summary>
		public string DllPath
		{
			get { return NativeAdapter.DllPath; }
			set { NativeAdapter.DllPath = value; }
		}

		/// <summary>
		/// Передавать ли данные для фондового рынка.
		/// </summary>
		public bool MicexRegisters
		{
			get { return NativeAdapter.MicexRegisters; }
			set { NativeAdapter.MicexRegisters = value; }
		}

		/// <summary>
		/// Подключаться ли к HFT серверу Финам.
		/// </summary>
		public bool IsHFT
		{
			get { return NativeAdapter.IsHFT; }
			set { NativeAdapter.IsHFT = value; }
		}

		/// <summary>
		/// Период агрегирования данных на сервере Transaq.
		/// </summary>
		public TimeSpan? MarketDataInterval
		{
			get { return NativeAdapter.MarketDataInterval; }
			set { NativeAdapter.MarketDataInterval = value; }
		}

		/// <summary>
		/// Перезаписать файл библиотеки из ресурсов. По-умолчанию файл будет перезаписан.
		/// </summary>
		public bool OverrideDll
		{
			get { return NativeAdapter.OverrideDll; }
			set { NativeAdapter.OverrideDll = value; }
		}

		/// <summary>
		/// Версия коннектора.
		/// </summary>
		public string ConnectorVersion => NativeAdapter.ConnectorVersion;

		/// <summary>
		/// Текущий сервер.
		/// </summary>
		public int CurrentServer => NativeAdapter.CurrentServer;

		/// <summary>
		/// Разница между локальным и серверным временем.
		/// </summary>
		public TimeSpan? ServerTimeDiff => NativeAdapter.ServerTimeDiff;

		/// <summary>
		/// Список доступных периодов свечей.
		/// </summary>
		public IEnumerable<TimeSpan> CandleTimeFrames => NativeAdapter.CandleTimeFrames;

		/// <summary>
		/// Событие инициализации поля <see cref="CandleTimeFrames"/>.
		/// </summary>
		public event Action CandleTimeFramesInitialized
		{
			add { NativeAdapter.CandleTimeFramesInitialized += value; }
			remove { NativeAdapter.CandleTimeFramesInitialized -= value; }
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источника для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan && NativeAdapter.CandleTimeFrames.Contains((TimeSpan)series.Arg))
			{
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
			}
		}

		/// <summary>
		/// Событие появления новых свечек, полученных после подписки через <see cref="SubscribeCandles"/>.
		/// </summary>
		public event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// Событие окончания обработки серии.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// Подписаться на получение свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (_candleSeries.Values.Contains(series))
			{
				SendOutError(new InvalidOperationException(LocalizedStrings.Str3568Params.Put(series)));
				return;
			}

			var transactionId = TransactionIdGenerator.GetNextId();

			_candleSeries[transactionId] = series;

			SendInMessage(new MarketDataMessage
			{
				From = from,
				To = to,
				//SecurityId = GetSecurityId(series.Security),
				DataType = MarketDataTypes.CandleTimeFrame,
				Arg = (TimeSpan)series.Arg,
				TransactionId = transactionId,
				IsSubscribe = true,
				Count = (int)series.Security.GetTimeFrameCount(new Range<DateTimeOffset>(from, to == DateTimeOffset.MaxValue ? DateTimeOffset.Now : to), (TimeSpan)series.Arg)
			}.FillSecurityInfo(this, series.Security));
		}

		/// <summary>
		/// Остановить подписку получения свечек, ранее созданную через <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnProcessMessage(Message message)
		{
			var candleMsg = message as CandleMessage;

			if (candleMsg == null)
			{
				base.OnProcessMessage(message);
				return;
			}

			var series = _candleSeries.TryGetValue(candleMsg.OriginalTransactionId);

			if (series == null)
				return;

			var candle = candleMsg.ToCandle(series);
			NewCandles.SafeInvoke(series, new[] { candle });

			if (candleMsg.IsFinished)
				Stopped.SafeInvoke(series);
		}
	}
}