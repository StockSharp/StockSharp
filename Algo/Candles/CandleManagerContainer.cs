namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// Стандартный контейнер, хранящий данные свечек.
	/// </summary>
	public class CandleManagerContainer : Disposable, ICandleManagerContainer
	{
		private static readonly MemoryStatisticsValue<Candle> _candleStat = new MemoryStatisticsValue<Candle>(LocalizedStrings.Candles);

		static CandleManagerContainer()
		{
			MemoryStatistics.Instance.Values.Add(_candleStat);
		}

		private sealed class SeriesInfo
		{
			private readonly CandleManagerContainer _container;
			private const int _candlesCapacity = 10000;

			private readonly SynchronizedDictionary<DateTimeOffset, SynchronizedSet<Candle>> _byTime = new SynchronizedDictionary<DateTimeOffset, SynchronizedSet<Candle>>(_candlesCapacity);
			private readonly SynchronizedLinkedList<Candle> _allCandles = new SynchronizedLinkedList<Candle>();

			private long _firstCandleTime;
			private long _lastCandleTime;

			public SeriesInfo(CandleManagerContainer container)
			{
				if (container == null)
					throw new ArgumentNullException("container");

				_container = container;
			}

			public int CandleCount
			{
				get { return _allCandles.Count; }
			}

			public void Reset(DateTimeOffset from)
			{
				_firstCandleTime = from.UtcTicks;

				_byTime.Clear();

				lock (_allCandles.SyncRoot)
				{
					_candleStat.Remove(_allCandles);
					_allCandles.Clear();
				}
			}

			public bool AddCandle(Candle candle)
			{
				if (candle == null)
					throw new ArgumentNullException("candle");

				if (!_byTime.SafeAdd(candle.OpenTime).TryAdd(candle))
					return false;

				_allCandles.AddLast(candle);
				_candleStat.Add(candle);

				_lastCandleTime = candle.OpenTime.UtcTicks;

				RecycleCandles();

				return true;
			}

			public void RecycleCandles()
			{
				if (_container.CandlesKeepTime == TimeSpan.Zero)
					return;

				var diff = _lastCandleTime - _firstCandleTime;

				if (diff <= _container._maxCandlesKeepTime)
					return;

				_firstCandleTime = _lastCandleTime - _container.CandlesKeepTime.Ticks;

				_allCandles.SyncDo(list =>
				{
					while (list.Count > 0)
					{
						if (list.First.Value.OpenTime.UtcTicks >= _firstCandleTime)
							break;

						_candleStat.Remove(list.First.Value);
						list.RemoveFirst();
					}
				});

				_byTime.SyncGet(d => d.RemoveWhere(p => p.Key.UtcTicks < _firstCandleTime));
			}

			public IEnumerable<Candle> GetCandles(DateTime time)
			{
				var candles = _byTime.TryGetValue(time);

				return candles != null ? candles.SyncGet(c => c.ToArray()) : Enumerable.Empty<Candle>();
			}

			public IEnumerable<Candle> GetCandles()
			{
				return _allCandles.SyncGet(c => c.ToArray());
			}

			public Candle GetCandle(int candleIndex)
			{
				return _allCandles.SyncGet(c => c.ElementAtFromEndOrDefault(candleIndex));
			}
		}

		private readonly SynchronizedDictionary<CandleSeries, SeriesInfo> _info = new SynchronizedDictionary<CandleSeries, SeriesInfo>();
		private long _maxCandlesKeepTime;
		
		/// <summary>
		/// Создать <see cref="CandleManagerContainer"/>.
		/// </summary>
		public CandleManagerContainer()
		{
			CandlesKeepTime = TimeSpan.FromDays(2);
		}

		private TimeSpan _candlesKeepTime;

		/// <summary>
		/// Время хранения свечек в памяти. По-умолчанию равно 2-ум дням.
		/// </summary>
		/// <remarks>Если значение установлено в <see cref="TimeSpan.Zero"/>, то свечи не будут удаляться.</remarks>
		public TimeSpan CandlesKeepTime
		{
			get { return _candlesKeepTime; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str647);

				_candlesKeepTime = value;
				_maxCandlesKeepTime = (long)(value.Ticks * 1.5);

				_info.SyncDo(d => d.ForEach(p => p.Value.RecycleCandles()));
			}
		}

		/// <summary>
		/// Добавить свечу для серии.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча.</param>
		/// <returns><see langword="true"/>, если свеча не ранее добавлена, иначе, <see langword="false"/>.</returns>
		public bool AddCandle(CandleSeries series, Candle candle)
		{
			var info = GetInfo(series);

			if (info == null)
				throw new InvalidOperationException(LocalizedStrings.Str648Params.Put(series));

			return info.AddCandle(candle);
		}

		/// <summary>
		/// Получить для серии все ассоциированные с ней свечи на период <paramref name="time"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="time">Период свечи.</param>
		/// <returns>Свечи.</returns>
		public IEnumerable<Candle> GetCandles(CandleSeries series, DateTime time)
		{
			var info = GetInfo(series);
			return info != null ? info.GetCandles(time) : Enumerable.Empty<Candle>();
		}

		/// <summary>
		/// Получить для серии все ассоциированные с ней свечи.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Свечи.</returns>
		public IEnumerable<Candle> GetCandles(CandleSeries series)
		{
			var info = GetInfo(series);
			return info != null ? info.GetCandles() : Enumerable.Empty<Candle>();
		}

		/// <summary>
		/// Получить свечу по индексу.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candleIndex">Порядковый номер свечи с конца.</param>
		/// <returns>Найденная свеча. Если свечи не существует, то будет возвращено null.</returns>
		public Candle GetCandle(CandleSeries series, int candleIndex)
		{
			if (candleIndex < 0)
				throw new ArgumentOutOfRangeException("candleIndex");

			var info = GetInfo(series);
			return info != null ? info.GetCandle(candleIndex) : null;
		}

		/// <summary>
		/// Получить свечи по серии и диапазону дат.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="timeRange">Диапазон дат, в которые должны входить свечи. Учитывается значение <see cref="Candle.OpenTime"/>.</param>
		/// <returns>Найденные свечи.</returns>
		public IEnumerable<Candle> GetCandles(CandleSeries series, Range<DateTimeOffset> timeRange)
		{
			return GetCandles(series)
						.Where(c => timeRange.Contains(c.OpenTime))
						.OrderBy(c => c.OpenTime);
		}

		/// <summary>
		/// Получить свечи по серии и общему количеству.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candleCount">Количество свечек, которое необходимо вернуть.</param>
		/// <returns>Найденные свечи.</returns>
		public IEnumerable<Candle> GetCandles(CandleSeries series, int candleCount)
		{
			if (candleCount <= 0)
				throw new ArgumentOutOfRangeException("candleCount");

			return GetCandles(series)
							.OrderByDescending(c => c.OpenTime)
							.Take(candleCount)
							.OrderBy(c => c.OpenTime);
		}

		/// <summary>
		/// Получить количество свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Количество свечек.</returns>
		public int GetCandleCount(CandleSeries series)
		{
			var info = GetInfo(series);
			return info == null ? 0 : info.CandleCount;
		}

		/// <summary>
		/// Известить контейнер для начале получения свечек для серии.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой будут получаться свечи.</param>
		/// <param name="to">Конечная дата, до которой будут получаться свечи.</param>
		public void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var info = _info.SafeAdd(series, key => new SeriesInfo(this));
			info.Reset(from);
		}

		private SeriesInfo GetInfo(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			return _info.TryGetValue(series);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			lock (_info.SyncRoot)
				_info.Values.ForEach(i => i.Reset(default(DateTimeOffset)));

			base.DisposeManaged();
		}
	}
}