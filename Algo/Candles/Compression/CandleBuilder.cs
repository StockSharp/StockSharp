namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	// mika вынесен за пределы CandleBuilder, так как в дженерик классе статические переменные инициализируются каждый раз для нового параметра
	class Holder
	{
		public static readonly ICandleBuilderSource TradeStorage = new TradeStorageCandleBuilderSource();
		public static readonly ICandleBuilderSource OrderLogStorage = new OrderLogStorageCandleBuilderSource();
	}

	/// <summary>
	/// Построитель свечек. Через источник <see cref="ICandleBuilderSource"/> подключается к событию <see cref="ICandleSource{TValue}.Processing"/>,
	/// и на основе полученных данных строит свечи по заданным критериям.
	/// </summary>
	/// <typeparam name="TCandle">Тип свечи, которую будет формировать построитель.</typeparam>
	public abstract class CandleBuilder<TCandle> : BaseLogReceiver, ICandleBuilder, IStorageCandleSource
		where TCandle : Candle
	{
		private sealed class CandleSeriesInfo
		{
			private readonly CandleSourceEnumerator<ICandleBuilderSource, IEnumerable<ICandleBuilderSourceValue>> _enumerator;

			public CandleSeriesInfo(CandleSeries series, DateTimeOffset from, DateTimeOffset to, IEnumerable<ICandleBuilderSource> sources, Action<CandleSeries, IEnumerable<ICandleBuilderSourceValue>> handler, Action<CandleSeries> stopped)
			{
				if (series == null)
					throw new ArgumentNullException("series");

				if (handler == null)
					throw new ArgumentNullException("handler");

				if (stopped == null)
					throw new ArgumentNullException("stopped");

				_enumerator = new CandleSourceEnumerator<ICandleBuilderSource, IEnumerable<ICandleBuilderSourceValue>>(series, from, to,
					sources, v => handler(series, v), () => stopped(series));
			}

			public Candle CurrentCandle { get; set; }

			public void Start()
			{
				_enumerator.Start();
			}

			public void Stop()
			{
				_enumerator.Stop();
			}
		}

		private sealed class CandleBuilderSourceList : SynchronizedList<ICandleBuilderSource>, ICandleBuilderSourceList
		{
			private readonly CandleBuilder<TCandle> _builder;

			public CandleBuilderSourceList(CandleBuilder<TCandle> builder)
			{
				_builder = builder;
			}

			protected override void OnAdded(ICandleBuilderSource item)
			{
				base.OnAdded(item);
				Subscribe(item);
			}

			protected override bool OnRemoving(ICandleBuilderSource item)
			{
				UnSubscribe(item);
				return base.OnRemoving(item);
			}

			protected override void OnInserted(int index, ICandleBuilderSource item)
			{
				base.OnInserted(index, item);
				Subscribe(item);
			}

			protected override bool OnClearing()
			{
				foreach (var item in this)
					UnSubscribe(item);

				return base.OnClearing();
			}

			private void Subscribe(ICandleBuilderSource source)
			{
				//source.NewValues += _builder.OnNewValues;
				source.ProcessDataError += _builder.RaiseProcessDataError;
			}

			private void UnSubscribe(ICandleBuilderSource source)
			{
				//source.NewValues -= _builder.OnNewValues;
				source.ProcessDataError -= _builder.RaiseProcessDataError;
				source.Dispose();
			}
		}

		private readonly SynchronizedDictionary<CandleSeries, CandleSeriesInfo> _info = new SynchronizedDictionary<CandleSeries, CandleSeriesInfo>();

		/// <summary>
		/// Инициализировать <see cref="CandleBuilder{TCandle}"/>.
		/// </summary>
		protected CandleBuilder()
			: this(new CandleBuilderContainer())
		{
		}

		/// <summary>
		/// Инициализировать <see cref="CandleBuilder{TCandle}"/>.
		/// </summary>
		/// <param name="container">Контейнер данных.</param>
		protected CandleBuilder(ICandleBuilderContainer container)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			Sources = new CandleBuilderSourceList(this) { Holder.TradeStorage, Holder.OrderLogStorage };

			Container = container;
		}

		/// <summary>
		/// Источники данных.
		/// </summary>
		public ICandleBuilderSourceList Sources { get; private set; }

		/// <summary>
		/// Kонтейнер данных.
		/// </summary>
		public ICandleBuilderContainer Container { get; private set; }

		/// <summary>
		/// Менеджер свечек. Заполняется, если построитель является источником внутри <see cref="ICandleManager.Sources"/>.
		/// </summary>
		public ICandleManager CandleManager { get; set; }

		Type ICandleBuilder.CandleType
		{
			get { return typeof(TCandle); }
		}

		private IStorageRegistry _storageRegistry;

		/// <summary>
		/// Хранилище данных. Передается во все источники, реализующие интерфейс <see cref="IStorageCandleSource"/>.
		/// </summary>
		public IStorageRegistry StorageRegistry
		{
			get { return _storageRegistry; }
			set
			{
				_storageRegistry = value;
				Sources.OfType<IStorageCandleSource>().ForEach(s => s.StorageRegistry = value);
			}
		}

		/// <summary>
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		public int SpeedPriority
		{
			get { return 2; }
		}

		/// <summary>
		/// Событие появления нового значения для обработки.
		/// </summary>
		public event Action<CandleSeries, Candle> Processing;

		/// <summary>
		/// Событие окончания обработки серии.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// Событие ошибки формирования свечек.
		/// </summary>
		public event Action<Exception> ProcessDataError;

		#region ICandleSource members

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public virtual IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (series.CandleType != typeof(TCandle))
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return Sources.SelectMany(s => s.GetSupportedRanges(series)).JoinRanges().ToArray();
		}

		/// <summary>
		/// Запустить получение свечек для указанной серии.
		/// </summary>
		/// <param name="series">Серия свечек, для которой необходимо начать получать свечи.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать свечи.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать свечи.</param>
		public virtual void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			CandleSeriesInfo info;

			lock (_info.SyncRoot)
			{
				info = _info.TryGetValue(series);

				if (info != null)
					throw new ArgumentException(LocalizedStrings.Str636Params.Put(series), "series");

				info = new CandleSeriesInfo(series, from, to, Sources, OnNewValues, s =>
				{
					_info.Remove(s);
					OnStopped(s);
				});

				Container.Start(series, from, to);

				_info.Add(series, info);
			}

			info.Start();
		}

		/// <summary>
		/// Остановить получение свечек, запущенное через <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public virtual void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var info = _info.TryGetValue(series);

			if (info == null)
				return;

			info.Stop();
		}

		#endregion

		/// <summary>
		/// Метод-обработчик события <see cref="CandleSeries.Stopped"/>.
		/// </summary>
		/// <param name="series">Серия свечек, для которой был было вызвано событие.</param>
		protected virtual void OnStopped(CandleSeries series)
		{
			_info.Remove(series);
			Stopped.SafeInvoke(series);
		}

		/// <summary>
		/// Обработать новые данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="values">Новые данные.</param>
		protected virtual void OnNewValues(CandleSeries series, IEnumerable<ICandleBuilderSourceValue> values)
		{
			if (values == null)
				throw new ArgumentNullException("values");

			var info = _info.TryGetValue(series);

			if (info == null)
				return;

			foreach (var value in values)
			{
				var valueAdded = false;

				while (true)
				{
					var candle = ProcessValue(series, (TCandle)info.CurrentCandle, value);

					if (candle == null)
					{
						// если значение не может быть обработано, то просто его пропускаем
						break;
						//throw new InvalidOperationException("Фабрика вернула пустую свечу.");
					}

					if (candle == info.CurrentCandle)
					{
						if (!valueAdded)
							Container.AddValue(series, candle, value);

						candle.State = CandleStates.Changed;
						RaiseProcessing(series, candle);

						break;
					}
					else
					{
						if (info.CurrentCandle != null)
						{
							info.CurrentCandle.State = CandleStates.Finished;
							RaiseProcessing(series, info.CurrentCandle);
						}

						info.CurrentCandle = candle;

						Container.AddValue(series, candle, value);
						valueAdded = true;

						candle.State = CandleStates.Started;
						RaiseProcessing(series, candle);
					}
				}
			}
		}

		/// <summary>
		/// Создать новую свечу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="value">Данные, с помощью которых необходимо создать новую свечу.</param>
		/// <returns>Созданная свеча.</returns>
		protected virtual TCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			throw new NotSupportedException(LocalizedStrings.Str637);
		}

		/// <summary>
		/// Сформирована ли свеча до добавления данных.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные, с помощью которых принимается решение о необходимости окончания формирования текущей свечи.</param>
		/// <returns><see langword="true"/>, если свечу необходимо закончить. Иначе, <see langword="false"/>.</returns>
		protected virtual bool IsCandleFinishedBeforeChange(CandleSeries series, TCandle candle, ICandleBuilderSourceValue value)
		{
			return false;
		}

		/// <summary>
		/// Заполнить первоначальные параметры свечи.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные.</param>
		/// <returns>Свеча.</returns>
		protected virtual TCandle FirstInitCandle(CandleSeries series, TCandle candle, ICandleBuilderSourceValue value)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			if (value == null)
				throw new ArgumentNullException("value");

			candle.Security = value.Security;

			candle.OpenPrice = value.Price;
			candle.ClosePrice = value.Price;
			candle.LowPrice = value.Price;
			candle.HighPrice = value.Price;
			//candle.TotalPrice = value.Price;

			candle.OpenVolume = value.Volume;
			candle.CloseVolume = value.Volume;
			candle.LowVolume = value.Volume;
			candle.HighVolume = value.Volume;
			//candle.TotalVolume = value.Volume;

			return candle;
		}

		/// <summary>
		/// Обновить свечу данными.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные.</param>
		protected virtual void UpdateCandle(CandleSeries series, TCandle candle, ICandleBuilderSourceValue value)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			if (value == null)
				throw new ArgumentNullException("value");

			if (value.Price < candle.LowPrice)
			{
				candle.LowPrice = value.Price;
				candle.LowTime = value.Time;
			}

			if (value.Price > candle.HighPrice)
			{
				candle.HighPrice = value.Price;
				candle.HighTime = value.Time;
			}

			candle.ClosePrice = value.Price;
			candle.TotalPrice += value.Price;

			candle.LowVolume = candle.LowVolume.Min(value.Volume);
			candle.HighVolume = candle.HighVolume.Max(value.Volume);
			candle.CloseVolume = value.Volume;
			candle.TotalVolume += value.Volume;

			if (value.OrderDirection != null)
			{
				candle.RelativeVolume += value.OrderDirection == Sides.Buy ? value.Volume : -value.Volume;
			}

			candle.CloseTime = value.Time;

			if (series.IsCalcVolumeProfile)
				candle.VolumeProfileInfo.Update(value);
		}

		/// <summary>
		/// Обработать новые данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="currentCandle">Текущая свеча.</param>
		/// <param name="value">Новые данные, с помощью которых принимается решение о необходимости начала или окончания формирования текущей свечи.</param>
		/// <returns>Новая свеча. Если новую свечу нет необходимости создавать, то возвращается <paramref name="currentCandle"/>.
		/// Если новую свечу создать невозможно (<paramref name="value"/> не может быть применено к свечам), то возвращается null.</returns>
		protected virtual TCandle ProcessValue(CandleSeries series, TCandle currentCandle, ICandleBuilderSourceValue value)
		{
			if (currentCandle == null || IsCandleFinishedBeforeChange(series, currentCandle, value))
			{
				currentCandle = CreateCandle(series, value);
				this.AddDebugLog("NewCandle {0} ForValue {1}", currentCandle, value);
				return currentCandle;
			}

			UpdateCandle(series, currentCandle, value);
			this.AddDebugLog("UpdatedCandle {0} ForValue {1}", currentCandle, value);

			return currentCandle;
		}

		/// <summary>
		/// Вызвать событие <see cref="Processing"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		protected virtual void RaiseProcessing(CandleSeries series, Candle candle)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (candle == null)
				throw new ArgumentNullException("candle");

			// mika: чтобы построение свечек продолжалось, даже если в пользовательских обработчиках ошибки.
			// иначе это может привести к испорченной последовательности последующих вызовов свечек.
			// нашел багу эспер
			try
			{
				Processing.SafeInvoke(series, candle);
			}
			catch (Exception ex)
			{
				RaiseProcessDataError(ex);
			}
		}

		/// <summary>
		/// Вызвать событие <see cref="ProcessDataError"/>.
		/// </summary>
		/// <param name="error">Информация об ошибке.</param>
		protected virtual void RaiseProcessDataError(Exception error)
		{
			ProcessDataError.SafeInvoke(error);
			this.AddErrorLog(error);
		}

		/// <summary>
		/// Принудительно завершить свечу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		protected void ForceFinishCandle(CandleSeries series, Candle candle)
		{
			var info = _info.TryGetValue(series);

			if (info == null)
				return;

			// если успела прийти новая свеча
			if (candle.State == CandleStates.None && info.CurrentCandle != null)
				return;

			if (candle.State != CandleStates.None && info.CurrentCandle != candle)
				return;

			info.CurrentCandle = candle.State == CandleStates.None ? null : candle;

			if (candle.State != CandleStates.None)
				candle.State = CandleStates.Finished;

			RaiseProcessing(series, candle);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Sources.Clear();
			Container.Dispose();
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// Построитель свечек типа <see cref="TimeFrameCandle"/>.
	/// </summary>
	public class TimeFrameCandleBuilder : CandleBuilder<TimeFrameCandle>
	{
		//private sealed class TimeoutInfo : Disposable
		//{
		//	private readonly MarketTimer _timer;
		//	private DateTime _emptyCandleTime;
		//	private readonly TimeSpan _timeFrame;
		//	private readonly TimeSpan _offset;
		//	private DateTime _nextTime;

		//	public TimeoutInfo(CandleSeries series, TimeFrameCandleBuilder builder)
		//	{
		//		if (series == null)
		//			throw new ArgumentNullException("series");

		//		if (series.Security.Connector == null)
		//			throw new ArgumentException("Инструмент {0} не имеет информации о подключении.".Put(series.Security), "series");

		//		if (builder == null)
		//			throw new ArgumentNullException("builder");

		//		_timeFrame = (TimeSpan)series.Arg;
		//		_offset = TimeSpan.FromTicks((long)((decimal)((decimal)_timeFrame.Ticks + builder.Timeout)));

		//		var security = series.Security;
		//		var connector = security.Connector;

		//		var isFirstTime = true;

		//		_timer = new MarketTimer(connector, () =>
		//		{
		//			if (isFirstTime)
		//			{
		//				isFirstTime = false;

		//				var bounds = _timeFrame.GetCandleBounds(security);

		//				_emptyCandleTime = bounds.Min;
		//				_nextTime = GetLimitTime(bounds.Min);

		//				return;
		//			}

		//			if (security.GetMarketTime() >= _nextTime)
		//			{
		//				_nextTime += _timeFrame;

		//				var candle = LastCandle;

		//				if (candle == null)
		//				{
		//					candle = new TimeFrameCandle
		//					{
		//						Security = security,
		//						TimeFrame = _timeFrame,
		//						OpenTime = _emptyCandleTime,
		//						CloseTime = _emptyCandleTime + _timeFrame,
		//					};

		//					_emptyCandleTime += _timeFrame;

		//					if (!builder.GenerateEmptyCandles)
		//						return;
		//				}

		//				builder.ForceFinishCandle(series, candle);
		//			}
		//		}).Interval(_timeFrame).Start();
		//	}

		//	private TimeFrameCandle _lastCandle;

		//	public TimeFrameCandle LastCandle
		//	{
		//		private get { return _lastCandle; }
		//		set
		//		{
		//			_lastCandle = value;
		//			_emptyCandleTime = value.OpenTime + _timeFrame;
		//			_nextTime = GetLimitTime(value.OpenTime);
		//		}
		//	}

		//	private DateTime GetLimitTime(DateTime currentCandleTime)
		//	{
		//		return currentCandleTime + _offset;
		//	}

		//	protected override void DisposeManaged()
		//	{
		//		base.DisposeManaged();
		//		_timer.Dispose();
		//	}
		//}

		//private readonly SynchronizedDictionary<CandleSeries, TimeoutInfo> _timeoutInfos = new SynchronizedDictionary<CandleSeries, TimeoutInfo>();

		/// <summary>
		/// Создать <see cref="TimeFrameCandleBuilder"/>.
		/// </summary>
		public TimeFrameCandleBuilder()
		{
			GenerateEmptyCandles = true;
		}

		/// <summary>
		/// Создать <see cref="TimeFrameCandleBuilder"/>.
		/// </summary>
		/// <param name="container">Контейнер данных.</param>
		public TimeFrameCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
			GenerateEmptyCandles = true;
		}

		private Unit _timeout = 10.Percents();

		/// <summary>
		/// Временной сдвиг от окончания тайм-фрейма, после которого для незакрытой свечи принудительно посылается сигнал на закрытие.
		/// По-умолчанию равно 10% от тайм-фрейма.
		/// </summary>
		public Unit Timeout
		{
			get { return _timeout; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (value < 0)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.OffsetValueIncorrect);

				_timeout = value;
			}
		}

		/// <summary>
		/// Генерировать ли пустые свечи (<see cref="CandleStates.None"/>) при отсутствии сделок.
		/// По-умолчанию режим включен.
		/// </summary>
		public bool GenerateEmptyCandles { get; set; }

		/// <summary>
		/// Запустить получение свечек для указанной серии.
		/// </summary>
		/// <param name="series">Серия свечек, для которой необходимо начать получать свечи.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать свечи.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать свечи.</param>
		public override void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			base.Start(series, from, to);

			if (Timeout == 0)
				return;

			// TODO mika временно выключил, ошибки, нужно протестировать
			//_timeoutInfos.Add(series, new TimeoutInfo(series, this));
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series);

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is TimeSpan))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");

				if ((TimeSpan)series.Arg <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("series", series.Arg, LocalizedStrings.Str640);
			}

			return ranges;
		}

		/// <summary>
		/// Создать новую свечу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="value">Данные, с помощью которых необходимо создать новую свечу.</param>
		/// <returns>Созданная свеча.</returns>
		protected override TimeFrameCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			var timeFrame = (TimeSpan)series.Arg;

			var bounds = timeFrame.GetCandleBounds(value.Time.DateTime, series.WorkingTime);

			if (value.Time < bounds.Min)
				return null;

			//var openTime = new DateTimeOffset(bounds.Min + value.Time.Offset, value.Time.Offset);
			var openTime = new DateTimeOffset(bounds.Min, value.Time.Offset);

			var candle = FirstInitCandle(series, new TimeFrameCandle
			{
				TimeFrame = timeFrame,
				OpenTime = openTime,
				HighTime = openTime,
				LowTime = openTime,
				CloseTime = openTime, // реальное окончание свечи определяет по последней сделке
			}, value);

			//var info = _timeoutInfos.TryGetValue(series);
			//if (info != null)
			//	info.LastCandle = candle;

			return candle;
		}

		/// <summary>
		/// Сформирована ли свеча до добавления данных.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные, с помощью которых принимается решение о необходимости окончания формирования текущей свечи.</param>
		/// <returns><see langword="true"/>, если свечу необходимо закончить. Иначе, <see langword="false"/>.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, TimeFrameCandle candle, ICandleBuilderSourceValue value)
		{
			return value.Time < candle.OpenTime || (candle.OpenTime + candle.TimeFrame) <= value.Time;
		}

		///// <summary>
		///// Метод-обработчик события <see cref="CandleSeries.Stopped"/>.
		///// </summary>
		///// <param name="series">Серия свечек, для которой был было вызвано событие.</param>
		//protected override void OnStopped(CandleSeries series)
		//{
		//	_timeoutInfos.Remove(series);
		//	base.OnStopped(series);
		//}

		///// <summary>
		///// Освободить ресурсы.
		///// </summary>
		//protected override void DisposeManaged()
		//{
		//	_timeoutInfos.SyncDo(d => d.Values.ForEach(v => v.Dispose()));
		//	_timeoutInfos.Clear();

		//	base.DisposeManaged();
		//}
	}

	/// <summary>
	/// Построитель свечек типа <see cref="TickCandle"/>.
	/// </summary>
	public class TickCandleBuilder : CandleBuilder<TickCandle>
	{
		/// <summary>
		/// Создать <see cref="TickCandleBuilder"/>.
		/// </summary>
		public TickCandleBuilder()
		{
		}

		/// <summary>
		/// Создать <see cref="TickCandleBuilder"/>.
		/// </summary>
		/// <param name="container">Контейнер данных.</param>
		public TickCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series);

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is int))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");

				if ((int)series.Arg <= 0)
					throw new ArgumentOutOfRangeException("series", series.Arg, LocalizedStrings.TickCountMustBePositive);
			}

			return ranges;
		}

		/// <summary>
		/// Создать новую свечу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="value">Данные, с помощью которых необходимо создать новую свечу.</param>
		/// <returns>Созданная свеча.</returns>
		protected override TickCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(series, new TickCandle
			{
				MaxTradeCount = (int)series.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Сформирована ли свеча до добавления данных.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные, с помощью которых принимается решение о необходимости окончания формирования текущей свечи.</param>
		/// <returns><see langword="true"/>, если свечу необходимо закончить. Иначе, <see langword="false"/>.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, TickCandle candle, ICandleBuilderSourceValue value)
		{
			return candle.CurrentTradeCount >= candle.MaxTradeCount;
		}

		/// <summary>
		/// Обновить свечу данными.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные.</param>
		protected override void UpdateCandle(CandleSeries series, TickCandle candle, ICandleBuilderSourceValue value)
		{
			base.UpdateCandle(series, candle, value);
			candle.CurrentTradeCount++;
		}
	}

	/// <summary>
	/// Построитель свечек типа <see cref="VolumeCandle"/>.
	/// </summary>
	public class VolumeCandleBuilder : CandleBuilder<VolumeCandle>
	{
		/// <summary>
		/// Создать <see cref="VolumeCandleBuilder"/>.
		/// </summary>
		public VolumeCandleBuilder()
		{
		}

		/// <summary>
		/// Создать <see cref="VolumeCandleBuilder"/>.
		/// </summary>
		/// <param name="container">Контейнер данных.</param>
		public VolumeCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series);

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is decimal))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");

				if ((decimal)series.Arg <= 0)
					throw new ArgumentOutOfRangeException("series", series.Arg, LocalizedStrings.VolumeMustBeGreaterThanZero);
			}

			return ranges;
		}

		/// <summary>
		/// Создать новую свечу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="value">Данные, с помощью которых необходимо создать новую свечу.</param>
		/// <returns>Созданная свеча.</returns>
		protected override VolumeCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(series, new VolumeCandle
			{
				Volume = (decimal)series.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Сформирована ли свеча до добавления данных.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные, с помощью которых принимается решение о необходимости окончания формирования текущей свечи.</param>
		/// <returns><see langword="true"/>, если свечу необходимо закончить. Иначе, <see langword="false"/>.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, VolumeCandle candle, ICandleBuilderSourceValue value)
		{
			return candle.TotalVolume >= candle.Volume;
		}
	}

	/// <summary>
	/// Построитель свечек типа <see cref="RangeCandle"/>.
	/// </summary>
	public class RangeCandleBuilder : CandleBuilder<RangeCandle>
	{
		/// <summary>
		/// Создать <see cref="RangeCandleBuilder"/>.
		/// </summary>
		public RangeCandleBuilder()
		{
		}

		/// <summary>
		/// Создать <see cref="RangeCandleBuilder"/>.
		/// </summary>
		/// <param name="container">Контейнер данных.</param>
		public RangeCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series);

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is Unit))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");

				if ((Unit)series.Arg <= 0)
					throw new ArgumentOutOfRangeException("series", series.Arg, LocalizedStrings.PriceRangeMustBeGreaterThanZero);
			}

			return ranges;
		}

		/// <summary>
		/// Создать новую свечу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="value">Данные, с помощью которых необходимо создать новую свечу.</param>
		/// <returns>Созданная свеча.</returns>
		protected override RangeCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(series, new RangeCandle
			{
				PriceRange = (Unit)series.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Сформирована ли свеча до добавления данных.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные, с помощью которых принимается решение о необходимости окончания формирования текущей свечи.</param>
		/// <returns><see langword="true"/>, если свечу необходимо закончить. Иначе, <see langword="false"/>.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, RangeCandle candle, ICandleBuilderSourceValue value)
		{
			return (decimal)(candle.LowPrice + candle.PriceRange) <= candle.HighPrice;
		}
	}

	/// <summary>
	/// Построитель свечек типа <see cref="PnFCandle"/>.
	/// </summary>
	public class PnFCandleBuilder : CandleBuilder<PnFCandle>
	{
		/// <summary>
		/// Создать <see cref="PnFCandleBuilder"/>.
		/// </summary>
		public PnFCandleBuilder()
		{
		}

		/// <summary>
		/// Создать <see cref="PnFCandleBuilder"/>.
		/// </summary>
		/// <param name="container">Контейнер данных.</param>
		public PnFCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series);

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is PnFArg))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");
			}

			return ranges;
		}

		/// <summary>
		/// Создать новую свечу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="value">Данные, с помощью которых необходимо создать новую свечу.</param>
		/// <returns>Созданная свеча.</returns>
		protected override PnFCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			var arg = (PnFArg)series.Arg;
			var boxSize = arg.BoxSize;

			if (CandleManager == null)
				throw new InvalidOperationException(LocalizedStrings.CandleManagerIsNotSet);

			var pnfCandle = (PnFCandle)CandleManager.Container.GetCandle(series, 0);

			var candle = new PnFCandle
			{
				PnFArg = arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,

				Security = value.Security,

				OpenVolume = value.Volume,
				CloseVolume = value.Volume,
				LowVolume = value.Volume,
				HighVolume = value.Volume,
				TotalVolume = value.Volume,

				TotalPrice = value.Price,

				Type = pnfCandle == null ? PnFTypes.X : (pnfCandle.Type == PnFTypes.X ? PnFTypes.O : PnFTypes.X),
			};

			if (pnfCandle == null)
			{
				candle.OpenPrice = boxSize.AlignPrice(value.Price, value.Price);

				if (candle.Type == PnFTypes.X)
					candle.ClosePrice = (decimal)(candle.OpenPrice + boxSize);
				else
					candle.ClosePrice = (decimal)(candle.OpenPrice - boxSize);
			}
			else
			{
				candle.OpenPrice = (decimal)((pnfCandle.Type == PnFTypes.X)
					? pnfCandle.ClosePrice - boxSize
					: pnfCandle.ClosePrice + boxSize);

				var price = boxSize.AlignPrice(candle.OpenPrice, value.Price);

				if (candle.Type == PnFTypes.X)
					candle.ClosePrice = (decimal)(price + boxSize);
				else
					candle.ClosePrice = (decimal)(price - boxSize);
			}

			if (candle.Type == PnFTypes.X)
			{
				candle.LowPrice = candle.OpenPrice;
				candle.HighPrice = candle.ClosePrice;
			}
			else
			{
				candle.LowPrice = candle.ClosePrice;
				candle.HighPrice = candle.OpenPrice;
			}

			return candle;
		}

		/// <summary>
		/// Сформирована ли свеча до добавления данных.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные, с помощью которых принимается решение о необходимости окончания формирования текущей свечи.</param>
		/// <returns><see langword="true"/>, если свечу необходимо закончить. Иначе, <see langword="false"/>.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, PnFCandle candle, ICandleBuilderSourceValue value)
		{
			var argSize = candle.PnFArg.BoxSize * candle.PnFArg.ReversalAmount;

			if (candle.Type == PnFTypes.X)
				return candle.ClosePrice - argSize > value.Price;
			else
				return candle.ClosePrice + argSize < value.Price;
		}

		/// <summary>
		/// Обновить свечу данными.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <param name="value">Данные.</param>
		protected override void UpdateCandle(CandleSeries series, PnFCandle candle, ICandleBuilderSourceValue value)
		{
			candle.ClosePrice = candle.PnFArg.BoxSize.AlignPrice(candle.ClosePrice, value.Price);

			if (candle.Type == PnFTypes.X)
				candle.HighPrice = candle.ClosePrice;
			else
				candle.LowPrice = candle.ClosePrice;

			candle.TotalPrice += value.Price;

			candle.LowVolume = candle.LowVolume.Min(value.Volume);
			candle.HighVolume = candle.HighVolume.Max(value.Volume);
			candle.CloseVolume = value.Volume;
			candle.TotalVolume += value.Volume;
			candle.CloseTime = value.Time;
		}
	}

	/// <summary>
	/// Построитель свечек типа <see cref="RenkoCandle"/>.
	/// </summary>
	public class RenkoCandleBuilder : CandleBuilder<RenkoCandle>
	{
		/// <summary>
		/// Создать <see cref="RenkoCandleBuilder"/>.
		/// </summary>
		public RenkoCandleBuilder()
		{
		}

		/// <summary>
		/// Создать <see cref="RenkoCandleBuilder"/>.
		/// </summary>
		/// <param name="container">Контейнер данных.</param>
		public RenkoCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series);

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is Unit))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");

				if ((Unit)series.Arg <= 0)
					throw new ArgumentOutOfRangeException("series", series.Arg, LocalizedStrings.Str645);
			}

			return ranges;
		}

		/// <summary>
		/// Обработать новые данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="currentCandle">Текущая свеча.</param>
		/// <param name="value">Новые данные, с помощью которых принимается решение о необходимости начала или окончания формирования текущей свечи.</param>
		/// <returns>Новая свеча. Если новую свечу нет необходимости создавать, то возвращается <paramref name="currentCandle"/>.
		/// Если новую свечу создать невозможно (<paramref name="value"/> не может быть применено к свечам), то возвращается null.</returns>
		protected override RenkoCandle ProcessValue(CandleSeries series, RenkoCandle currentCandle, ICandleBuilderSourceValue value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			if (currentCandle == null)
				return NewCandle(series, value.Price, value.Price, value);

			var delta = currentCandle.BoxSize.Value;

			if (currentCandle.OpenPrice < currentCandle.ClosePrice)
			{
				if ((value.Price - currentCandle.ClosePrice) > delta)
				{
					// New bullish candle
					return NewCandle(series, currentCandle.ClosePrice, currentCandle.ClosePrice + delta, value);
				}

				if ((currentCandle.OpenPrice - value.Price) > delta)
				{
					// New bearish candle
					return NewCandle(series, currentCandle.OpenPrice, currentCandle.OpenPrice - delta, value);
				}
			}
			else
			{
				if ((value.Price - currentCandle.OpenPrice) > delta)
				{
					// New bullish candle
					return NewCandle(series, currentCandle.OpenPrice, currentCandle.OpenPrice + delta, value);
				}

				if ((currentCandle.ClosePrice - value.Price) > delta)
				{
					// New bearish candle
					return NewCandle(series, currentCandle.ClosePrice, currentCandle.ClosePrice - delta, value);
				}
			}

			return UpdateRenkoCandle(currentCandle, value);
		}

		private static RenkoCandle NewCandle(CandleSeries series, decimal openPrice, decimal closePrice, ICandleBuilderSourceValue value)
		{
			return new RenkoCandle
			{
				OpenPrice = openPrice,
				ClosePrice = closePrice,
				HighPrice = Math.Max(openPrice, closePrice),
				LowPrice = Math.Min(openPrice, closePrice),
				TotalPrice = openPrice + closePrice,
				OpenVolume = value.Volume,
				CloseVolume = value.Volume,
				HighVolume = value.Volume,
				LowVolume = value.Volume,
				TotalVolume = value.Volume,
				Security = series.Security,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
				BoxSize = (Unit)series.Arg,
				RelativeVolume = value.OrderDirection == null ? 0 : (value.OrderDirection == Sides.Buy ? value.Volume : -value.Volume)
			};
		}

		private static RenkoCandle UpdateRenkoCandle(RenkoCandle candle, ICandleBuilderSourceValue value)
		{
			candle.HighPrice = Math.Max(candle.HighPrice, value.Price);
			candle.LowPrice = Math.Min(candle.LowPrice, value.Price);

			candle.HighVolume = Math.Max(candle.HighVolume, value.Volume);
			candle.LowVolume = Math.Min(candle.LowVolume, value.Volume);

			candle.CloseVolume = value.Volume;

			candle.TotalPrice += value.Price;
			candle.TotalVolume += value.Volume;

			if (value.OrderDirection != null)
				candle.RelativeVolume += value.OrderDirection == Sides.Buy ? value.Volume : -value.Volume;

			candle.CloseTime = value.Time;

			return candle;
		}
	}
}