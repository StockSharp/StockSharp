namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Менеджер свечек.
	/// </summary>
	public class CandleManager : BaseLogReceiver, ICandleManager
	{
		private sealed class CandleManagerSourceList : SynchronizedList<ICandleManagerSource>, ICandleManagerSourceList
		{
			private sealed class SourceInfo : Disposable
			{
				private readonly ICandleManagerSource _source;
				private readonly CandleManager _manager;

				public SourceInfo(ICandleManagerSource source, CandleManager manager)
				{
					if (source == null)
						throw new ArgumentNullException("source");

					_source = source;
					_manager = manager;

					_source.Processing += OnProcessing;
					_source.Error += _manager.RaiseError;
					_source.CandleManager = _manager;
				}

				private void OnProcessing(CandleSeries series, Candle candle)
				{
					if (candle.Series == null)
					{
						candle.Series = series;
						candle.Source = _source;
					}

					_manager.Container.AddCandle(series, candle);
				}

				protected override void DisposeManaged()
				{
					base.DisposeManaged();

					_source.Processing -= OnProcessing;
					_source.Error -= _manager.RaiseError;
					_source.Dispose();
				}
			}

			private readonly SynchronizedDictionary<ICandleManagerSource, SourceInfo> _info = new SynchronizedDictionary<ICandleManagerSource,SourceInfo>();
			private readonly CandleManager _manager;

			public CandleManagerSourceList(CandleManager manager)
			{
				if (manager == null)
					throw new ArgumentNullException("manager");

				_manager = manager;
			}

			protected override void OnAdded(ICandleManagerSource item)
			{
				Subscribe(item);
				base.OnAdded(item);
			}

			protected override bool OnRemoving(ICandleManagerSource item)
			{
				UnSubscribe(item);
				return base.OnRemoving(item);
			}

			protected override void OnInserted(int index, ICandleManagerSource item)
			{
				Subscribe(item);
				base.OnInserted(index, item);
			}

			protected override bool OnClearing()
			{
				foreach (var item in this)
					UnSubscribe(item);

				return base.OnClearing();
			}

			private void Subscribe(ICandleManagerSource source)
			{
				_info.Add(source, new SourceInfo(source, _manager));
			}

			private void UnSubscribe(ICandleManagerSource source)
			{
				lock (_info.SyncRoot)
				{
					var info = _info[source];
					info.Dispose();
					_info.Remove(source);
				}
			}
		}

		private sealed class ExternalCandleSource : Disposable, ICandleManagerSource
		{
			private readonly IExternalCandleSource _source;

			public ExternalCandleSource(IExternalCandleSource source)
			{
				if (source == null)
					throw new ArgumentNullException("source");

				_source = source;
				_source.NewCandles += OnNewCandles;
				_source.Stopped += OnStopped;
			}

			public int SpeedPriority
			{
				get { return 1; }
			}

			public event Action<CandleSeries, Candle> Processing;
			public event Action<CandleSeries> Stopped;

			event Action<Exception> ICandleSource<Candle>.Error
			{
				add { }
				remove { }
			}

			IEnumerable<Range<DateTimeOffset>> ICandleSource<Candle>.GetSupportedRanges(CandleSeries series)
			{
				return _source.GetSupportedRanges(series);
			}

			void ICandleSource<Candle>.Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
			{
				_source.SubscribeCandles(series, from, to);
			}

			void ICandleSource<Candle>.Stop(CandleSeries series)
			{
				_source.UnSubscribeCandles(series);
			}

			private void OnNewCandles(CandleSeries series, IEnumerable<Candle> candles)
			{
				foreach (var c in candles)
				{
					var candle = c.Clone();

					candle.State = CandleStates.Active;
					Processing.SafeInvoke(series, candle);

					candle.State = CandleStates.Finished;
					Processing.SafeInvoke(series, candle);
				}
			}

			private void OnStopped(CandleSeries series)
			{
				Stopped.SafeInvoke(series);
			}

			/// <summary>
			/// Освободить ресурсы.
			/// </summary>
			protected override void DisposeManaged()
			{
				_source.NewCandles -= OnNewCandles;
				_source.Stopped -= OnStopped;

				base.DisposeManaged();
			}

			ICandleManager ICandleManagerSource.CandleManager { get; set; }
		}

		private readonly SynchronizedDictionary<CandleSeries, CandleSourceEnumerator<ICandleManagerSource, Candle>> _series = new SynchronizedDictionary<CandleSeries, CandleSourceEnumerator<ICandleManagerSource, Candle>>();

		/// <summary>
		/// Создать <see cref="CandleManager"/>.
		/// </summary>
		public CandleManager()
		{
			var builderContainer = new CandleBuilderContainer();

			Sources = new CandleManagerSourceList(this)
			{
				new StorageCandleSource(),

				new TimeFrameCandleBuilder(builderContainer),
				new TickCandleBuilder(builderContainer),
				new VolumeCandleBuilder(builderContainer),
				new RangeCandleBuilder(builderContainer),
				new RenkoCandleBuilder(builderContainer),
				new PnFCandleBuilder(builderContainer),
			};
		}

		/// <summary>
		/// Создать <see cref="CandleManager"/>.
		/// </summary>
		/// <param name="builderSource">Источник данных для <see cref="ICandleBuilder"/>.</param>
		public CandleManager(ICandleBuilderSource builderSource)
			: this()
		{
			if (builderSource == null)
				throw new ArgumentNullException("builderSource");

			Sources.OfType<ICandleBuilder>().ForEach(b => b.Sources.Add(builderSource));
		}

		/// <summary>
		/// Создать <see cref="CandleManager"/>.
		/// </summary>
		/// <param name="connector">Подключение к торговой системе для создания источника тиковых сделок по-умолчанию.</param>
		public CandleManager(IConnector connector)
			: this(new TradeCandleBuilderSource(connector))
		{
			var externalSource = connector as IExternalCandleSource;
			
			if (externalSource != null)
				Sources.Add(new ExternalCandleSource(externalSource));
		}

		private ICandleManagerContainer _container = new CandleManagerContainer();

		/// <summary>
		/// Kонтейнер данных.
		/// </summary>
		public ICandleManagerContainer Container
		{
			get { return _container; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (value == _container)
					return;

				_container.Dispose();
				_container = value;
			}
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
		/// Все активные на текущий момент серии свечек, запущенные через <see cref="Start"/>.
		/// </summary>
		public IEnumerable<CandleSeries> Series
		{
			get { return _series.SyncGet(d => d.Keys.ToArray()); }
		}

		/// <summary>
		/// Источники свечек.
		/// </summary>
		public ICandleManagerSourceList Sources { get; private set; }

		/// <summary>
		/// Приоритет источника по скорости (0 - самый оптимальный).
		/// </summary>
		public int SpeedPriority
		{
			get { throw new NotSupportedException(); }
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
		public event Action<Exception> Error;

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public virtual IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			return Sources.SelectMany(s => s.GetSupportedRanges(series)).JoinRanges().ToArray();
		}

		/// <summary>
		/// Запросить получение данных.
		/// </summary>
		/// <param name="series">Серия свечек, для которой необходимо начать получать данные.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public virtual void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			CandleSourceEnumerator<ICandleManagerSource, Candle> enumerator;

			lock (_series.SyncRoot)
			{
				if (_series.ContainsKey(series))
					throw new ArgumentException(LocalizedStrings.Str650Params.Put(series), "series");

				enumerator = new CandleSourceEnumerator<ICandleManagerSource, Candle>(series, from, to,
					series.Security is IndexSecurity ? (IEnumerable<ICandleManagerSource>)new[] { new IndexSecurityCandleManagerSource(this, from, to) } : Sources,
					c => Processing.SafeInvoke(series, c),
					() =>
					{
						//Stop(series);
						_series.Remove(series);
						Stopped.SafeInvoke(series);
					});

				_series.Add(series, enumerator);

				series.CandleManager = this;
				series.From = from;
				series.To = to;

				Container.Start(series, from, to);
			}
			
			enumerator.Start();
		}

		/// <summary>
		/// Прекратить получение данных, запущенное через <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public virtual void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var enumerator = _series.TryGetValue(series);

			if (enumerator == null)
				return;

			enumerator.Stop();
		}

		/// <summary>
		/// Вызвать событие <see cref="Error"/>.
		/// </summary>
		/// <param name="error">Информация об ошибке.</param>
		protected virtual void RaiseError(Exception error)
		{
			Error.SafeInvoke(error);
			this.AddErrorLog(error);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			lock (Sources.SyncRoot)
			{
				Sources.ForEach(s => s.Dispose());
				Sources.Clear();
			}

			Container.Dispose();
			base.DisposeManaged();
		}
	}
}