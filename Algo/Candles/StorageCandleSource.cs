#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: StorageCandleSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.Localization;

	/// <summary>
	/// The candles source for <see cref="ICandleManager"/> that downloads candles from an external storage.
	/// </summary>
	public class StorageCandleSource : BaseCandleSource<Candle>, IStorageCandleSource
	{
		[DebuggerDisplay("{Series} {Reader}")]
		private sealed class SeriesInfo
		{
			public SeriesInfo(CandleSeries series, DateTimeOffset from, DateTimeOffset to, IMarketDataStorage<Candle> storage)
			{
				if (series == null)
					throw new ArgumentNullException(nameof(series));

				if (storage == null)
					throw new ArgumentNullException(nameof(storage));

				Series = series;
				Reader = storage.Load(from, to).GetEnumerator();
			}

			public CandleSeries Series { get; }
			public IEnumerator<Candle> Reader { get; }
			public bool IsStopping { get; set; }
		}

		private readonly CachedSynchronizedDictionary<CandleSeries, SeriesInfo> _series = new CachedSynchronizedDictionary<CandleSeries, SeriesInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="StorageCandleSource"/>.
		/// </summary>
		public StorageCandleSource()
		{
			ThreadingHelper
					.Thread(OnLoading)
					.Background(true)
					.Name(GetType().Name)
					.Launch();
		}

		/// <summary>
		/// The source priority by speed (0 - the best).
		/// </summary>
		public override int SpeedPriority => 0;

		/// <summary>
		/// Market data storage.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; set; }

		/// <summary>
		/// The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.
		/// </summary>
		public IMarketDataDrive Drive { get; set; }

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (StorageRegistry == null)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return GetStorage(series).GetRanges();
		}

		/// <summary>
		/// To send data request.
		/// </summary>
		/// <param name="series">The candles series for which data receiving should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public override void Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var storage = GetStorage(series);

			var range = storage.GetRange(from, to);

			if (range == null)
				return;

			lock (_series.SyncRoot)
			{
				if (_series.ContainsKey(series))
					throw new ArgumentException(LocalizedStrings.Str650Params.Put(series), nameof(series));

				_series.Add(series, new SeriesInfo(series, range.Min, range.Max, storage));

				if (_series.Count == 1)
					Monitor.Pulse(_series.SyncRoot);
			}
		}

		/// <summary>
		/// To stop data receiving starting through <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public override void Stop(CandleSeries series)
		{
			lock (_series.SyncRoot)
			{
				var info = _series.TryGetValue(series);

				if (info != null)
					info.IsStopping = true;
			}
		}

		private IMarketDataStorage<Candle> GetStorage(CandleSeries series)
		{
			return StorageRegistry.GetCandleStorage(series, Drive ?? StorageRegistry.DefaultDrive);
		}

		private void OnLoading()
		{
			try
			{
				while (!IsDisposed)
				{
					var removingSeries = new List<CandleSeries>();

					foreach (var info in _series.CachedValues)
					{
						if (info.IsStopping)
							removingSeries.Add(info.Series);
						else
						{
							if (info.Reader.MoveNext())
							{
								var candle = info.Reader.Current;

								if (info.Series.CheckTime(candle.OpenTime))
									RaiseProcessing(info.Series, candle);
							}
							else
								removingSeries.Add(info.Series);
						}
					}

					if (removingSeries.Count > 0)
					{
						lock (_series.SyncRoot)
							removingSeries.ForEach(s => _series.Remove(s));

						removingSeries.ForEach(RaiseStopped);
					}

					lock (_series.SyncRoot)
					{
						if (_series.IsEmpty())
							Monitor.Wait(_series.SyncRoot);
					}
				}
			}
			catch (Exception ex)
			{
				RaiseError(ex);
				_series.CopyAndClear().ForEach(p => RaiseStopped(p.Key));
			}
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();

			lock (_series.SyncRoot)
			{
				_series.ForEach(p => p.Value.IsStopping = true);
				Monitor.Pulse(_series.SyncRoot);
			}
		}
	}
}