namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Localization;

	/// <summary>
	/// Стандартный контейнер данных.
	/// </summary>
	public class CandleBuilderContainer : Disposable, ICandleBuilderContainer
	{
		private static readonly MemoryStatisticsValue<ICandleBuilderSourceValue> _valuesStat = new MemoryStatisticsValue<ICandleBuilderSourceValue>(LocalizedStrings.CandlesElem);

		static CandleBuilderContainer()
		{
			MemoryStatistics.Instance.Values.Add(_valuesStat);
		}

		private sealed class SeriesInfo
		{
			private readonly SynchronizedDictionary<Candle, SynchronizedLinkedList<ICandleBuilderSourceValue>> _candleValues = new SynchronizedDictionary<Candle, SynchronizedLinkedList<ICandleBuilderSourceValue>>(1000);
			private readonly CandleBuilderContainer _container;

			private long _firstValueTime;
			private long _lastValueTime;

			public SeriesInfo(CandleBuilderContainer container)
			{
				if (container == null)
					throw new ArgumentNullException("container");

				_container = container;
			}

			public void Reset(DateTimeOffset from)
			{
				lock (_candleValues.SyncRoot)
				{
					_firstValueTime = from.UtcTicks;

					_candleValues.Values.ForEach(_valuesStat.Remove);
					_candleValues.Clear();
				}
			}

			public void RecycleValues()
			{
				lock (_candleValues.SyncRoot)
				{
					var diff = _lastValueTime - _firstValueTime;

					if (diff <= _container._maxValuesKeepTime)
						return;

					_firstValueTime = _lastValueTime - _container.ValuesKeepTime.Ticks;

					var deleteKeys = new List<Candle>();

					foreach (var pair in _candleValues)
					{
						if (pair.Key.OpenTime.UtcTicks < _firstValueTime)
						{
							deleteKeys.Add(pair.Key);
							continue;
						}

						pair.Value.SyncDo(list =>
						{
							while (list.Count > 0)
							{
								if (list.First.Value.Time.UtcTicks >= _firstValueTime)
									break;

								_valuesStat.Remove(list.First.Value);
								list.RemoveFirst();
							}
						});
					}

					deleteKeys.ForEach(c => _candleValues.Remove(c));
				}
			}

			public void AddValue(Candle candle, ICandleBuilderSourceValue value)
			{
				if (candle == null)
					throw new ArgumentNullException("candle");

				if (value == null)
					throw new ArgumentNullException("value");

				_candleValues.SafeAdd(candle).AddLast(value);
				_valuesStat.Add(value);

				if (_firstValueTime == 0)
					_firstValueTime = value.Time.UtcTicks;

				_lastValueTime = value.Time.UtcTicks;

				RecycleValues();
			}

			public IEnumerable<ICandleBuilderSourceValue> GetValues(Candle candle)
			{
				if (candle == null)
					throw new ArgumentNullException("candle");

				var trades = _candleValues.TryGetValue(candle);
				return trades != null ? trades.SyncGet(c => c.ToArray()) : Enumerable.Empty<ICandleBuilderSourceValue>();
			}
		}

		private readonly SynchronizedDictionary<CandleSeries, SeriesInfo> _info = new SynchronizedDictionary<CandleSeries, SeriesInfo>();
		private long _maxValuesKeepTime;
		
		/// <summary>
		/// Создать <see cref="CandleBuilderContainer"/>.
		/// </summary>
		public CandleBuilderContainer()
		{
			ValuesKeepTime = TimeSpan.Zero;
		}

		private TimeSpan _valuesKeepTime;

		/// <summary>
		/// Время хранения <see cref="ICandleBuilderSourceValue"/> в памяти. По-умолчанию равно нулю (хранение отсутствует).
		/// </summary>
		public TimeSpan ValuesKeepTime
		{
			get { return _valuesKeepTime; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str652);

				_valuesKeepTime = value;
				_maxValuesKeepTime = (long)(value.Ticks * 1.5);

				_info.SyncDo(d => d.ForEach(p => p.Value.RecycleValues()));
			}
		}

		/// <summary>
		/// Известить контейнер для начале получения данных для серии.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой будут получаться данные.</param>
		/// <param name="to">Конечная дата, до которой будут получаться данные.</param>
		public void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var info = _info.SafeAdd(series, key => new SeriesInfo(this));
			info.Reset(from);
		}

		/// <summary>
		/// Добавить данные для свечи.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча, для которой нужно добавить данные.</param>
		/// <param name="value">Новые данные.</param>
		public void AddValue(CandleSeries series, Candle candle, ICandleBuilderSourceValue value)
		{
			if (_valuesKeepTime == TimeSpan.Zero)
			    return;

			GetInfo(series).AddValue(candle, value);
		}

		/// <summary>
		/// Получить все данные по свече.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="candle">Свеча, по которой нужно найти данные.</param>
		/// <returns>Найденные данные.</returns>
		public IEnumerable<ICandleBuilderSourceValue> GetValues(CandleSeries series, Candle candle)
		{
			return GetInfo(series).GetValues(candle);
		}

		private SeriesInfo GetInfo(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var info = _info.TryGetValue(series);

			if (info == null)
				throw new InvalidOperationException(LocalizedStrings.Str648Params.Put(series));

			return info;
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