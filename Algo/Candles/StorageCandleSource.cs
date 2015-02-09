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
	/// Источник свечек для <see cref="ICandleManager"/>, который загружает свечи из внешнего хранилища.
	/// </summary>
	public class StorageCandleSource : BaseCandleSource<Candle>, ICandleManagerSource, IStorageCandleSource
	{
		[DebuggerDisplay("{Series} {Reader}")]
		private sealed class SeriesInfo
		{
			public SeriesInfo(CandleSeries series, DateTimeOffset from, DateTimeOffset to, IMarketDataStorage<Candle> storage)
			{
				if (series == null)
					throw new ArgumentNullException("series");

				if (storage == null)
					throw new ArgumentNullException("storage");

				Series = series;
				Reader = storage.Load(from, to).GetEnumerator();
			}

			public CandleSeries Series { get; private set; }
			public IEnumerator<Candle> Reader { get; private set; }
			public bool IsStopping { get; set; }
		}

		private readonly CachedSynchronizedDictionary<CandleSeries, SeriesInfo> _series = new CachedSynchronizedDictionary<CandleSeries, SeriesInfo>();

		/// <summary>
		/// Создать <see cref="StorageCandleSource"/>.
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
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		public override int SpeedPriority
		{
			get { return 0; }
		}

		/// <summary>
		/// Хранилище данных.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; set; }

		/// <summary>
		/// Хранилище, которое используется по-умолчанию. По умолчанию используется <see cref="IStorageRegistry.DefaultDrive"/>.
		/// </summary>
		public IMarketDataDrive Drive { get; set; }

		/// <summary>
		/// Менеджер свечек, которому принадлежит данный источник.
		/// </summary>
		ICandleManager ICandleManagerSource.CandleManager { get; set; }

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (StorageRegistry == null)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return GetStorage(series).GetRanges();
		}

		/// <summary>
		/// Запросить получение данных.
		/// </summary>
		/// <param name="series">Серия свечек, для которой необходимо начать получать данные.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public override void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var storage = GetStorage(series);

			var range = storage.GetRange(from, to);

			if (range == null)
				return;

			lock (_series.SyncRoot)
			{
				if (_series.ContainsKey(series))
					throw new ArgumentException(LocalizedStrings.Str650Params.Put(series), "series");

				_series.Add(series, new SeriesInfo(series, range.Min, range.Max, storage));

				if (_series.Count == 1)
					Monitor.Pulse(_series.SyncRoot);
			}
		}

		/// <summary>
		/// Прекратить получение данных, запущенное через <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
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
				RaiseProcessDataError(ex);
				_series.CopyAndClear().ForEach(p => RaiseStopped(p.Key));
			}
		}

		/// <summary>
		/// Освободить занятые ресурсы.
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