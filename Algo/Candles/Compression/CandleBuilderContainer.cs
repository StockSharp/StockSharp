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
	/// The standard data container.
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
					throw new ArgumentNullException(nameof(container));

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
					throw new ArgumentNullException(nameof(candle));

				if (value == null)
					throw new ArgumentNullException(nameof(value));

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
					throw new ArgumentNullException(nameof(candle));

				var trades = _candleValues.TryGetValue(candle);
				return trades != null ? trades.SyncGet(c => c.ToArray()) : Enumerable.Empty<ICandleBuilderSourceValue>();
			}
		}

		private readonly SynchronizedDictionary<CandleSeries, SeriesInfo> _info = new SynchronizedDictionary<CandleSeries, SeriesInfo>();
		private long _maxValuesKeepTime;
		
		/// <summary>
		/// Initializes a new instance of the <see cref="CandleBuilderContainer"/>.
		/// </summary>
		public CandleBuilderContainer()
		{
			ValuesKeepTime = TimeSpan.Zero;
		}

		private TimeSpan _valuesKeepTime;

		/// <summary>
		/// The time of <see cref="ICandleBuilderSourceValue"/> storage in memory. The default is zero (no storage).
		/// </summary>
		public TimeSpan ValuesKeepTime
		{
			get { return _valuesKeepTime; }
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str652);

				_valuesKeepTime = value;
				_maxValuesKeepTime = (long)(value.Ticks * 1.5);

				_info.SyncDo(d => d.ForEach(p => p.Value.RecycleValues()));
			}
		}

		/// <summary>
		/// To notify the container about the start of the data getting for the series.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which data will be get.</param>
		/// <param name="to">The final date by which data will be get.</param>
		public void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var info = _info.SafeAdd(series, key => new SeriesInfo(this));
			info.Reset(from);
		}

		/// <summary>
		/// To add data for the candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">The candle for which you need to add data.</param>
		/// <param name="value">New data.</param>
		public void AddValue(CandleSeries series, Candle candle, ICandleBuilderSourceValue value)
		{
			if (_valuesKeepTime == TimeSpan.Zero)
			    return;

			GetInfo(series).AddValue(candle, value);
		}

		/// <summary>
		/// To get all data by the candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">The candle for which you need to find data.</param>
		/// <returns>Found data.</returns>
		public IEnumerable<ICandleBuilderSourceValue> GetValues(CandleSeries series, Candle candle)
		{
			return GetInfo(series).GetValues(candle);
		}

		private SeriesInfo GetInfo(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var info = _info.TryGetValue(series);

			if (info == null)
				throw new InvalidOperationException(LocalizedStrings.Str648Params.Put(series));

			return info;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			lock (_info.SyncRoot)
				_info.Values.ForEach(i => i.Reset(default(DateTimeOffset)));

			base.DisposeManaged();
		}
	}
}