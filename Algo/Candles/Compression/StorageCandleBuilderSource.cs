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

	/// <summary>
	/// Базовый источник данных для <see cref="ICandleBuilder"/>, который получает данные из внешнего хранилища.
	/// </summary>
	/// <typeparam name="TSourceValue">Тип исходных данных (например, <see cref="Trade"/>).</typeparam>
	public abstract class StorageCandleBuilderSource<TSourceValue> : ConvertableCandleBuilderSource<TSourceValue>, IStorageCandleSource
	{
		[DebuggerDisplay("{Series} {Reader}")]
		private sealed class SeriesInfo
		{
			public SeriesInfo(CandleSeries series, IEnumerator<TSourceValue> reader)
			{
				if (series == null)
					throw new ArgumentNullException("series");

				if (reader == null)
					throw new ArgumentNullException("reader");

				Series = series;
				Reader = reader;
			}

			public CandleSeries Series { get; private set; }
			public IEnumerator<TSourceValue> Reader { get; private set; }
			public bool IsStopping { get; set; }
		}

		private readonly CachedSynchronizedDictionary<CandleSeries, SeriesInfo> _series = new CachedSynchronizedDictionary<CandleSeries, SeriesInfo>();

		/// <summary>
		/// Инициализировать <see cref="StorageCandleBuilderSource{TSourceValue}"/>.
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
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		public override int SpeedPriority
		{
			get { return 1; }
		}

		/// <summary>
		/// Хранилище данных.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; set; }

		private IMarketDataDrive _drive;

		/// <summary>
		/// Хранилище, которое используется по-умолчанию. По умолчанию используется <see cref="IStorageRegistry.DefaultDrive"/>.
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
			set
			{
				_drive = value;
			}
		}

		/// <summary>
		/// Получить хранилище данных <typeparamref name="TSourceValue"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Хранилище данных.</returns>
		protected abstract IMarketDataStorage<TSourceValue> GetStorage(Security security);

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (StorageRegistry == null)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return GetStorage(series.Security).GetRanges();
		}

		/// <summary>
		/// Получить данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		/// <returns>Данные. Если данных не существует для заданного диапазона, то будет возвращено null.</returns>
		protected virtual IEnumerable<TSourceValue> GetValues(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			var storage = GetStorage(series.Security);

			var range = storage.GetRange(from, to);

			if (range == null)
				return null;

			return storage.Load(range.Min, range.Max);
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

			var values = GetValues(series, from, to);

			if (values == null)
				return;

			lock (_series.SyncRoot)
			{
				if (_series.ContainsKey(series))
					throw new ArgumentException(LocalizedStrings.Str650Params.Put(series), "series");

				_series.Add(series, new SeriesInfo(series, values.GetEnumerator()));

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
								NewSourceValues(info.Series, values);
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

	/// <summary>
	/// Источник данных для <see cref="CandleBuilder{TCandle}"/>, получающий тиковые сделки из внешнего хранилища <see cref="IStorageRegistry"/>.
	/// </summary>
	public class TradeStorageCandleBuilderSource : StorageCandleBuilderSource<Trade>
	{
		/// <summary>
		/// Создать <see cref="TradeStorageCandleBuilderSource"/>.
		/// </summary>
		public TradeStorageCandleBuilderSource()
		{
		}

		/// <summary>
		/// Получить хранилище тиковых сделок.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Хранилище тиковых сделок.</returns>
		protected override IMarketDataStorage<Trade> GetStorage(Security security)
		{
			return StorageRegistry.GetTradeStorage(security, Drive);
		}
	}

	/// <summary>
	/// Источник данных для <see cref="CandleBuilder{TCandle}"/>, получающий тиковые сделки из внешнего хранилища <see cref="IStorageRegistry"/>.
	/// </summary>
	public class MarketDepthStorageCandleBuilderSource : StorageCandleBuilderSource<MarketDepth>
	{
		/// <summary>
		/// Создать <see cref="MarketDepthStorageCandleBuilderSource"/>.
		/// </summary>
		public MarketDepthStorageCandleBuilderSource()
		{
		}

		/// <summary>
		/// Получить хранилище стаканов.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Хранилище стаканов.</returns>
		protected override IMarketDataStorage<MarketDepth> GetStorage(Security security)
		{
			return StorageRegistry.GetMarketDepthStorage(security, Drive);
		}
	}

	/// <summary>
	/// Источник данных для <see cref="CandleBuilder{TCandle}"/>, получающий тиковые сделки из внешнего хранилища <see cref="IStorageRegistry"/>.
	/// </summary>
	public class OrderLogStorageCandleBuilderSource : StorageCandleBuilderSource<Trade>
	{
		/// <summary>
		/// Создать <see cref="OrderLogStorageCandleBuilderSource"/>.
		/// </summary>
		public OrderLogStorageCandleBuilderSource()
		{
		}

		/// <summary>
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		public override int SpeedPriority
		{
			get { return 2; }
		}

		/// <summary>
		/// Получить хранилище данных.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Хранилище данных.</returns>
		protected override IMarketDataStorage<Trade> GetStorage(Security security)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (StorageRegistry == null)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return StorageRegistry.GetOrderLogStorage(series.Security, Drive).GetRanges();
		}

		/// <summary>
		/// Получить данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		/// <returns>Данные. Если данных не существует для заданного диапазона, то будет возвращено null.</returns>
		protected override IEnumerable<Trade> GetValues(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			var storage = StorageRegistry.GetOrderLogStorage(series.Security, Drive);

			var range = storage.GetRange(from, to);

			if (range == null)
				return null;

			return storage.Load(range.Min, range.Max).ToTrades();
		}
	}
}