#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: StorageCandleBuilderSource.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles.Compression
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
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The base data source for <see cref="ICandleBuilder"/>, which receives data from the external storage.
	/// </summary>
	/// <typeparam name="TSourceValue">The source data type (for example, <see cref="Trade"/>).</typeparam>
	public abstract class StorageCandleBuilderSource<TSourceValue> : BaseCandleBuilderSource, IStorageCandleSource
	{
		[DebuggerDisplay("{Series} {Reader}")]
		private sealed class SeriesInfo
		{
			public SeriesInfo(CandleSeries series, IEnumerator<TSourceValue> reader)
			{
				if (series == null)
					throw new ArgumentNullException(nameof(series));

				if (reader == null)
					throw new ArgumentNullException(nameof(reader));

				Series = series;
				Reader = reader;
			}

			public CandleSeries Series { get; }
			public IEnumerator<TSourceValue> Reader { get; }
			public bool IsStopping { get; set; }
		}

		private readonly CachedSynchronizedDictionary<CandleSeries, SeriesInfo> _series = new CachedSynchronizedDictionary<CandleSeries, SeriesInfo>();

		/// <summary>
		/// Initialize <see cref="StorageCandleBuilderSource{T}"/>.
		/// </summary>
		protected StorageCandleBuilderSource()
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
		public override int SpeedPriority => 1;

		/// <summary>
		/// Market data storage.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; set; }

		private IMarketDataDrive _drive;

		/// <summary>
		/// The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.
		/// </summary>
		public IMarketDataDrive Drive
		{
			get
			{
				if (_drive == null)
				{
					if (StorageRegistry != null)
						return StorageRegistry.DefaultDrive;
				}

				return _drive;
			}
			set => _drive = value;
		}

		/// <summary>
		/// To get the data storage <typeparamref name="TSourceValue" />.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Market data storage.</returns>
		protected abstract IMarketDataStorage<TSourceValue> GetStorage(Security security);

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (StorageRegistry == null)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return GetStorage(series.Security).GetRanges();
		}

		/// <summary>
		/// To get data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <returns>Data. If data does not exist for the specified range then <see langword="null" /> will be returned.</returns>
		protected virtual IEnumerable<TSourceValue> GetValues(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
		{
			var storage = GetStorage(series.Security);

			var range = storage.GetRange(from, to);

			if (range == null)
				return null;

			return storage.Load(range.Min, range.Max);
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

			var values = GetValues(series, from, to);

			if (values == null)
				return;

			lock (_series.SyncRoot)
			{
				if (_series.ContainsKey(series))
					throw new ArgumentException(LocalizedStrings.Str650Params.Put(series), nameof(series));

				_series.Add(series, new SeriesInfo(series, values.GetEnumerator()));

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

		/// <summary>
		/// To convert <typeparam ref="TSourceValue"/> to <see cref="ICandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="value">New source data.</param>
		/// <returns>Data in format <see cref="ICandleBuilder"/>.</returns>
		protected abstract ICandleBuilderSourceValue Convert(TSourceValue value);

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
							var values = new List<TSourceValue>(100);

							for (var i = 0; i < 100; i++)
							{
								if (!info.Reader.MoveNext())
								{
									removingSeries.Add(info.Series);
									break;
								}

								values.Add(info.Reader.Current);
							}

							if (values.Count > 0)
								RaiseProcessing(info.Series, values.Select(Convert));
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

	/// <summary>
	/// The data source for <see cref="CandleBuilder{T}"/> getting tick trades from the external storage <see cref="IStorageRegistry"/>.
	/// </summary>
	public class TradeStorageCandleBuilderSource : StorageCandleBuilderSource<Trade>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TradeStorageCandleBuilderSource"/>.
		/// </summary>
		public TradeStorageCandleBuilderSource()
		{
		}

		/// <summary>
		/// To get the storage of tick trades.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>The storage of tick trades.</returns>
		protected override IMarketDataStorage<Trade> GetStorage(Security security)
		{
			return StorageRegistry.GetTradeStorage(security, Drive);
		}

		/// <summary>
		/// To convert <typeparam ref="TSourceValue"/> to <see cref="ICandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="value">New source data.</param>
		/// <returns>Data in format <see cref="ICandleBuilder"/>.</returns>
		protected override ICandleBuilderSourceValue Convert(Trade value)
		{
			return new TradeCandleBuilderSourceValue(value);
		}
	}

	/// <summary>
	/// The data source for <see cref="CandleBuilder{T}"/> getting tick trades from the external storage <see cref="IStorageRegistry"/>.
	/// </summary>
	public class MarketDepthStorageCandleBuilderSource : StorageCandleBuilderSource<MarketDepth>
	{
		private readonly Level1Fields _type;

		/// <summary>
		/// Initializes a new instance of the <see cref="MarketDepthStorageCandleBuilderSource"/>.
		/// </summary>
		/// <param name="type">Type of candle depth based data.</param>
		public MarketDepthStorageCandleBuilderSource(Level1Fields type)
		{
			_type = type;
		}

		/// <summary>
		/// To get the order books storage.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>The order books storage.</returns>
		protected override IMarketDataStorage<MarketDepth> GetStorage(Security security)
		{
			return StorageRegistry.GetMarketDepthStorage(security, Drive);
		}

		/// <summary>
		/// To convert <typeparam ref="TSourceValue"/> to <see cref="ICandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="value">New source data.</param>
		/// <returns>Data in format <see cref="ICandleBuilder"/>.</returns>
		protected override ICandleBuilderSourceValue Convert(MarketDepth value)
		{
			return new DepthCandleBuilderSourceValue(value, _type);
		}
	}

	/// <summary>
	/// The data source for <see cref="CandleBuilder{T}"/> getting tick trades from the external storage <see cref="IStorageRegistry"/>.
	/// </summary>
	public class OrderLogStorageCandleBuilderSource : StorageCandleBuilderSource<Trade>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogStorageCandleBuilderSource"/>.
		/// </summary>
		public OrderLogStorageCandleBuilderSource()
		{
		}

		/// <summary>
		/// The source priority by speed (0 - the best).
		/// </summary>
		public override int SpeedPriority => 2;

		/// <summary>
		/// To get the data storage.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Market data storage.</returns>
		protected override IMarketDataStorage<Trade> GetStorage(Security security)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (StorageRegistry == null)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return StorageRegistry.GetOrderLogStorage(series.Security, Drive).GetRanges();
		}

		/// <summary>
		/// To convert <typeparam ref="TSourceValue"/> to <see cref="ICandleBuilderSourceValue"/>.
		/// </summary>
		/// <param name="value">New source data.</param>
		/// <returns>Data in format <see cref="ICandleBuilder"/>.</returns>
		protected override ICandleBuilderSourceValue Convert(Trade value)
		{
			return new TradeCandleBuilderSourceValue(value);
		}

		/// <summary>
		/// To get data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <returns>Data. If data does not exist for the specified range then <see langword="null" /> will be returned.</returns>
		protected override IEnumerable<Trade> GetValues(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
		{
			var storage = StorageRegistry.GetOrderLogStorage(series.Security, Drive);

			var range = storage.GetRange(from, to);

			if (range == null)
				return null;

			return storage.Load(range.Min, range.Max).ToTrades();
		}
	}
}