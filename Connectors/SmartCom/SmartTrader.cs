#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.SmartCom.SmartCom
File: SmartTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.SmartCom
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;
	using System.Threading;

	using Ecng.ComponentModel;
	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.SmartCom.Native;
	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение к брокеру IT Invest через SmartCOM.
	/// </summary>
	[Icon("SmartCom_logo.png")]
	public class SmartTrader : Connector, IExternalCandleSource
	{
		private readonly CachedSynchronizedSet<CandleSeries> _realTimeSeries = new CachedSynchronizedSet<CandleSeries>();
		private readonly SynchronizedPairSet<long, CandleSeries> _series = new SynchronizedPairSet<long, CandleSeries>();

		private Timer _realTimeCandlesTimer;

		/// <summary>
		/// Создать <see cref="SmartTrader"/>.
		/// </summary>
		public SmartTrader()
		{
			Adapter.InnerAdapters.Add(new SmartComMessageAdapter(TransactionIdGenerator));
		}

		private SmartComMessageAdapter NativeAdapter => Adapter.InnerAdapters.OfType<SmartComMessageAdapter>().First();

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

			var series = _series[candleMsg.OriginalTransactionId];
			NewCandles.SafeInvoke(series, new[] { candleMsg.ToCandle(series) });

			if (candleMsg.IsFinished)
				Stopped.SafeInvoke(series);
		}

		/// <summary>
		/// Версия SmartCOM API. По-умолчанию равна <see cref="SmartComVersions.V3"/>.
		/// </summary>
		public SmartComVersions Version
		{
			get { return NativeAdapter.Version; }
			set { NativeAdapter.Version = value; }
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
		/// Пароль.
		/// </summary>
		public string Password
		{
			get { return NativeAdapter.Password.To<string>(); }
			set { NativeAdapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Адрес сервера. Значение по-умолчанию равно <see cref="SmartComAddresses.Matrix"/>.
		/// </summary>
		public EndPoint Address
		{
			get { return NativeAdapter.Address; }
			set { NativeAdapter.Address = value; }
		}

		/// <summary>
		/// Настройки конфигурации клиентской части SmartCOM 3.x.
		/// </summary>
		public string ClientSettings
		{
			get { return NativeAdapter.ClientSettings; }
			set { NativeAdapter.ClientSettings = value; }
		}

		/// <summary>
		/// Настройки конфигурации серверной части SmartCOM 3.x.
		/// </summary>
		public string ServerSettings
		{
			get { return NativeAdapter.ServerSettings; }
			set { NativeAdapter.ServerSettings = value; }
		}

		/// <summary>
		/// Временной отступ для нового запроса получение новой свечи. По-умолчанию равен 5 секундам.
		/// </summary>
		/// <remarks>Необходим для того, чтобы сервер успел сформировать данные в своем хранилище свечек.</remarks>
		public TimeSpan RealTimeCandleOffset { get; set; } = TimeSpan.FromSeconds(5);

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// в виде одной транзакции. По-умолчанию включено.
		/// </summary>
		public override bool IsSupportAtomicReRegister => false;

		/// <summary>
		/// Перезапускать службу SmartCOM при подключении.
		/// </summary>
		public bool RestartService { get; set; } = true;

		/// <summary>
		/// Ограничение по времени для перезапуска службы SmartCOM.
		/// Для отключения ограничения по времени необходимо указать <see cref="TimeSpan.Zero"/>.
		/// </summary>
		public TimeSpan RestartServiceTimeOut { get; set; } = TimeSpan.FromSeconds(5);

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источника для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan && SmartComTimeFrames.CanConvert((TimeSpan)series.Arg))
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

			if (series.CandleType != typeof(TimeFrameCandle))
				throw new ArgumentException(LocalizedStrings.NotSupportCandle.Put("SmartCOM", series.CandleType), nameof(series));

			if (!(series.Arg is TimeSpan))
				throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), nameof(series));

			var timeFrame = (TimeSpan)series.Arg;

			using (new Scope<CandleSeries>(series, false))
			{
				to = timeFrame.GetCandleBounds(CurrentTime).Min;

				if (from >= (to - timeFrame))
					from = to - timeFrame;

				RequestCandles(series.Security, timeFrame, new Range<DateTimeOffset>(from, to));

				if (to == DateTimeOffset.MaxValue)
					_realTimeSeries.Add(series);
			}
		}

		/// <summary>
		/// Остановить подписку получения свечек, ранее созданную через <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			_realTimeSeries.Remove(series);
		}

		/// <summary>
		/// Получить тайм-фрейм свечи от сервера SmartCOM.
		/// </summary>
		/// <param name="security">Инструмент, для которого необходимо начать получать исторические свечи.</param>
		/// <param name="timeFrame">Тайм-фрейм.</param>
		/// <param name="range">Диапазон времени, для которого нужно получить свечи.</param>
		public void RequestCandles(Security security, SmartComTimeFrames timeFrame, Range<DateTimeOffset> range)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (timeFrame == null)
				throw new ArgumentNullException(nameof(timeFrame));

			if (range == null)
				throw new ArgumentNullException(nameof(range));

			var count = security.GetTimeFrameCount(range, (TimeSpan)timeFrame);
			RequestCandles(security, timeFrame, range.Max, count, SmartComHistoryDirections.Backward);
		}

		/// <summary>
		/// Получить тайм-фрейм свечи от сервера SmartCOM.
		/// </summary>
		/// <param name="security">Инструмент, для которого необходимо начать получать исторические свечи.</param>
		/// <param name="timeFrame">Тайм-фрейм.</param>
		/// <param name="from">Временная точка отсчета.</param>
		/// <param name="count">Количество свечек.</param>
		/// <param name="direction">Направление поиска относительно параметра from. Если значение равно <see cref="SmartComHistoryDirections.Forward"/>,
		/// то данные ищутся от from в сторону увеличения времени. Если значение равно <see cref="SmartComHistoryDirections.Backward"/>, то свечи
		/// ищутся до from в сторону уменьшения времени.</param>
		public void RequestCandles(Security security, SmartComTimeFrames timeFrame, DateTimeOffset from, long count, SmartComHistoryDirections direction)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (timeFrame == null)
				throw new ArgumentNullException(nameof(timeFrame));

			if (count <= 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, LocalizedStrings.Str1890);

			// http://stocksharp.com/forum/yaf_postst658_provierka-na-vriemia-birzhi-pri-zaghruzkie-istorii.aspx
			//if (from > MarketTime)
			//	throw new ArgumentOutOfRangeException("from", from, "Параметр from не может быть больше текущего времени биржи.");

			//var token = _candleTokens.SafeAdd(new Tuple<Security, SmartTimeFrames>(security, timeFrame), key => new CandleToken(security, timeFrame));

			var transactionId = TransactionIdGenerator.GetNextId();
			var tf = (TimeSpan)timeFrame;

			var scope = Scope<CandleSeries>.Current;
			_series.Add(transactionId, scope == null ? new CandleSeries(typeof(TimeFrameCandle), security, tf) : scope.Value);

			SendInMessage(new MarketDataMessage
			{
				TransactionId = transactionId,
				//SecurityId = GetSecurityId(security),
				From = from,
				DataType = MarketDataTypes.CandleTimeFrame,
				Arg = tf,
				IsSubscribe = true,
				Count = count,
				ExtensionInfo = new Dictionary<object, object>
				{
					{ "Direction", direction },
				}
			}.FillSecurityInfo(this, security));
		}

		/// <summary>
		/// Подключиться к торговой системе.
		/// </summary>
		protected override void OnConnect()
		{
			if (Address == null)
				throw new InvalidOperationException(LocalizedStrings.Str1895);

			if (Login == null)
				throw new InvalidOperationException(LocalizedStrings.Str1896);

			if (Password == null)
				throw new InvalidOperationException(LocalizedStrings.Str1897);

			try
			{
				// SmartCOM 3 не является сервисом и не требует перезапуска
				if (RestartService && Version == SmartComVersions.V2)
					SmartComService.RestartSmartComService(RestartServiceTimeOut);
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}

			base.OnConnect();

			_realTimeCandlesTimer = this.StartRealTime(_realTimeSeries, RealTimeCandleOffset,
				(series, range) => RequestCandles(series.Security, (TimeSpan)series.Arg, range), TimeSpan.FromSeconds(1));
		}

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		protected override void OnDisconnect()
		{
			_realTimeCandlesTimer.Dispose();
			base.OnDisconnect();
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Старая заявка, которую нужно перерегистрировать.</param>
		/// <param name="newOrder">Информация о новой заявке.</param>
		protected override void OnReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (IsSupportAtomicReRegister
			    && oldOrder.Security.Board.IsSupportAtomicReRegister
			    // http://www.itinvest.ru/forum/index.php?showtopic=63720&view=findpost&p=262059
			    && oldOrder.Balance == newOrder.Volume)
				SendInMessage(oldOrder.CreateReplaceMessage(newOrder, GetSecurityId(newOrder.Security)));
			else
				base.OnReRegisterOrder(oldOrder, newOrder);
		}

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="transactionId">Идентификатор транзакции отмены.</param>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, <see langword="false"/> - если только обычный и <see langword="null"/> - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно <see langword="null"/>, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно <see langword="null"/>, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно <see langword="null"/>, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно <see langword="null"/>, то инструмент не попадает в фильтр снятия заявок.</param>
		protected override void OnCancelOrders(long transactionId, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			if (Version == SmartComVersions.V2 && isStopOrder == null && portfolio == null && direction == null && board == null && security == null)
				base.OnCancelOrders(transactionId);
			else
				this.CancelOrders(Orders, isStopOrder, portfolio, direction, board, security);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(RestartService), RestartService);
			storage.SetValue(nameof(RestartServiceTimeOut), RestartServiceTimeOut);
			storage.SetValue(nameof(RealTimeCandleOffset), RealTimeCandleOffset);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			RestartService = storage.GetValue(nameof(RestartService), true);
			RestartServiceTimeOut = storage.GetValue(nameof(RestartServiceTimeOut), TimeSpan.FromSeconds(5));
			RealTimeCandleOffset = storage.GetValue(nameof(RealTimeCandleOffset), TimeSpan.FromSeconds(5));
		}
	}
}