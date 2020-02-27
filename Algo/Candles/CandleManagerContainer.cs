#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: CandleManagerContainer.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// The standard container that stores candles data.
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

			private readonly SynchronizedDictionary<long, SynchronizedSet<Candle>> _byTime = new SynchronizedDictionary<long, SynchronizedSet<Candle>>(_candlesCapacity);
			private readonly SynchronizedLinkedList<Candle> _allCandles = new SynchronizedLinkedList<Candle>();

			private long _firstCandleTime;
			private long _lastCandleTime;

			public SeriesInfo(CandleManagerContainer container)
			{
				_container = container ?? throw new ArgumentNullException(nameof(container));
			}

			public int CandleCount => _allCandles.Count;

			public void Reset(DateTimeOffset from)
			{
				_firstCandleTime = from.To<long>();

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
					throw new ArgumentNullException(nameof(candle));

				var ticks = candle.OpenTime.To<long>();

				if (!_byTime.SafeAdd(ticks).TryAdd(candle))
					return false;

				_allCandles.AddLast(candle);
				_candleStat.Add(candle);

				_lastCandleTime = ticks;

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
						if (list.First.Value.OpenTime.To<long>() >= _firstCandleTime)
							break;

						_candleStat.Remove(list.First.Value);
						list.RemoveFirst();
					}
				});

				_byTime.SyncGet(d => d.RemoveWhere(p => p.Key < _firstCandleTime));
			}

			public IEnumerable<Candle> GetCandles(DateTimeOffset time)
			{
				var candles = _byTime.TryGetValue(time.To<long>());

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
		/// Initializes a new instance of the <see cref="CandleManagerContainer"/>.
		/// </summary>
		public CandleManagerContainer()
		{
			CandlesKeepTime = TimeSpan.FromDays(2);
		}

		private TimeSpan _candlesKeepTime;

		/// <inheritdoc />
		public TimeSpan CandlesKeepTime
		{
			get => _candlesKeepTime;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str647);

				_candlesKeepTime = value;
				_maxCandlesKeepTime = (long)(value.Ticks * 1.5);

				_info.SyncDo(d => d.ForEach(p => p.Value.RecycleCandles()));
			}
		}

		/// <inheritdoc />
		public bool AddCandle(CandleSeries series, Candle candle)
		{
			var info = GetInfo(series);

			if (info == null)
				throw new InvalidOperationException(LocalizedStrings.Str648Params.Put(series));

			return info.AddCandle(candle);
		}

		/// <inheritdoc />
		public IEnumerable<Candle> GetCandles(CandleSeries series, DateTimeOffset time)
		{
			var info = GetInfo(series);
			return info != null ? info.GetCandles(time) : Enumerable.Empty<Candle>();
		}

		/// <inheritdoc />
		public IEnumerable<Candle> GetCandles(CandleSeries series)
		{
			var info = GetInfo(series);
			return info != null ? info.GetCandles() : Enumerable.Empty<Candle>();
		}

		/// <inheritdoc />
		public Candle GetCandle(CandleSeries series, int candleIndex)
		{
			if (candleIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(candleIndex), candleIndex, LocalizedStrings.Str1219);

			var info = GetInfo(series);
			return info?.GetCandle(candleIndex);
		}

		/// <inheritdoc />
		public IEnumerable<Candle> GetCandles(CandleSeries series, Range<DateTimeOffset> timeRange)
		{
			return GetCandles(series)
						.Where(c => timeRange.Contains(c.OpenTime))
						.OrderBy(c => c.OpenTime);
		}

		/// <inheritdoc />
		public IEnumerable<Candle> GetCandles(CandleSeries series, int candleCount)
		{
			if (candleCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(candleCount), candleCount, LocalizedStrings.Str1219);

			return GetCandles(series)
							.OrderByDescending(c => c.OpenTime)
							.Take(candleCount)
							.OrderBy(c => c.OpenTime);
		}

		/// <inheritdoc />
		public int GetCandleCount(CandleSeries series)
		{
			var info = GetInfo(series);
			return info?.CandleCount ?? 0;
		}

		/// <inheritdoc />
		public void Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var info = _info.SafeAdd(series, key => new SeriesInfo(this));
			info.Reset(from ?? DateTimeOffset.MinValue);
		}

		private SeriesInfo GetInfo(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			return _info.TryGetValue(series);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			lock (_info.SyncRoot)
				_info.Values.ForEach(i => i.Reset(default));

			base.DisposeManaged();
		}
	}
}