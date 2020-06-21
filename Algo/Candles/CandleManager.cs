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
	/// The candles manager.
	/// </summary>
	[Obsolete("Use Connector directly.")]
	public class CandleManager : BaseLogReceiver, ICandleManager
	{
		private sealed class CandleManagerSourceList : SynchronizedList<ICandleSource<Candle>>
		{
			private sealed class SourceInfo : Disposable
			{
				private readonly ICandleSource<Candle> _source;
				private readonly CandleManager _manager;

				public SourceInfo(ICandleSource<Candle> source, CandleManager manager)
				{
					_source = source ?? throw new ArgumentNullException(nameof(source));
					_manager = manager;

					_source.Processing += OnProcessing;
				}

				private void OnProcessing(CandleSeries series, Candle candle)
				{
					_manager.Container.AddCandle(series, candle);
				}

				protected override void DisposeManaged()
				{
					base.DisposeManaged();

					_source.Processing -= OnProcessing;
					_source.Dispose();
				}
			}

			private readonly SynchronizedDictionary<ICandleSource<Candle>, SourceInfo> _info = new SynchronizedDictionary<ICandleSource<Candle>, SourceInfo>();
			private readonly CandleManager _manager;

			public CandleManagerSourceList(CandleManager manager)
			{
				_manager = manager ?? throw new ArgumentNullException(nameof(manager));
			}

			protected override void OnAdded(ICandleSource<Candle> item)
			{
				Subscribe(item);
				base.OnAdded(item);
			}

			protected override bool OnRemoving(ICandleSource<Candle> item)
			{
				UnSubscribe(item);
				return base.OnRemoving(item);
			}

			protected override void OnInserted(int index, ICandleSource<Candle> item)
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

			private void Subscribe(ICandleSource<Candle> source)
			{
				_info.Add(source, new SourceInfo(source, _manager));
			}

			private void UnSubscribe(ICandleSource<Candle> source)
			{
				lock (_info.SyncRoot)
				{
					var info = _info[source];
					info.Dispose();
					_info.Remove(source);
				}
			}
		}

		private sealed class ConnectorCandleSource : Disposable, ICandleSource<Candle>
		{
			private readonly SynchronizedSet<CandleSeries> _candleSeries = new CachedSynchronizedSet<CandleSeries>();

			private readonly Connector _connector;

			public int SpeedPriority => 1;

			public event Action<CandleSeries, Candle> Processing;

			public event Action<CandleSeries> Stopped;

			public ConnectorCandleSource(Connector connector)
			{
				_connector = connector ?? throw new ArgumentNullException(nameof(connector));
				_connector.CandleSeriesProcessing += OnConnectorProcessingCandle;
				_connector.CandleSeriesStopped += OnConnectorCandleSeriesStopped;
			}

			private void OnConnectorProcessingCandle(CandleSeries series, Candle candle)
			{
				if (_candleSeries.Contains(series))
					Processing?.Invoke(series, candle);
			}

			private void OnConnectorCandleSeriesStopped(CandleSeries series)
			{
				_candleSeries.Remove(series);
				Stopped?.Invoke(series);
			}

			IEnumerable<Range<DateTimeOffset>> ICandleSource<Candle>.GetSupportedRanges(CandleSeries series)
			{
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
			}

			void ICandleSource<Candle>.Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
			{
				_candleSeries.Add(series);
				_connector.SubscribeCandles(series, from, to);
			}

			void ICandleSource<Candle>.Stop(CandleSeries series)
			{
				_connector.UnSubscribeCandles(series);
				_candleSeries.Remove(series);
			}

			protected override void DisposeManaged()
			{
				_connector.CandleSeriesProcessing -= OnConnectorProcessingCandle;
				_connector.CandleSeriesStopped -= OnConnectorCandleSeriesStopped;

				base.DisposeManaged();
			}
		}

		private readonly SynchronizedDictionary<CandleSeries, CandleSourceEnumerator<ICandleSource<Candle>, Candle>> _series = new SynchronizedDictionary<CandleSeries, CandleSourceEnumerator<ICandleSource<Candle>, Candle>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleManager"/>.
		/// </summary>
		public CandleManager()
		{
			Sources = new CandleManagerSourceList(this);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleManager"/>.
		/// </summary>
		/// <param name="connector">The connection to trading system to create the source for tick trades by default.</param>
		public CandleManager(Connector connector)
			: this()
		{
			Sources.Add(new ConnectorCandleSource(connector));
		}

		private ICandleManagerContainer _container = new CandleManagerContainer();

		/// <inheritdoc />
		public ICandleManagerContainer Container
		{
			get => _container;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (value == _container)
					return;

				_container.Dispose();
				_container = value;
			}
		}

		/// <inheritdoc />
		public IEnumerable<CandleSeries> Series => _series.SyncGet(d => d.Keys.ToArray());

		/// <inheritdoc />
		public IList<ICandleSource<Candle>> Sources { get; }

		/// <inheritdoc />
		public int SpeedPriority => throw new NotSupportedException();

		/// <inheritdoc />
		public event Action<CandleSeries, Candle> Processing;

		/// <inheritdoc />
		public event Action<CandleSeries> Stopped;

		/// <inheritdoc />
		public virtual IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			return Sources.SelectMany(s => s.GetSupportedRanges(series)).JoinRanges().ToArray();
		}

		/// <inheritdoc />
		public virtual void Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			CandleSourceEnumerator<ICandleSource<Candle>, Candle> enumerator;

			lock (_series.SyncRoot)
			{
				if (_series.ContainsKey(series))
					throw new ArgumentException(LocalizedStrings.Str650Params.Put(series), nameof(series));

				enumerator = new CandleSourceEnumerator<ICandleSource<Candle>, Candle>(series, from, to,
					//series.Security is IndexSecurity ? new[] { new IndexSecurityCandleManagerSource(this, ConfigManager.GetService<ISecurityProvider>(), from, to) } : 
					Sources,
					c =>
					{
						Processing?.Invoke(series, c);
						return c.OpenTime;
					},
					() =>
					{
						//Stop(series);
						_series.Remove(series);
						Stopped?.Invoke(series);
					});

				_series.Add(series, enumerator);

				//series.CandleManager = this;
				if (from != null)
					series.From = from;

				if (to != null)
					series.To = to;

				Container.Start(series, from, to);
			}
			
			enumerator.Start();
		}

		/// <inheritdoc />
		public virtual void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			_series.TryGetValue(series)?.Stop();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			lock (((CandleManagerSourceList)Sources).SyncRoot)
			{
				Sources.ForEach(s => s.Dispose());
				Sources.Clear();
			}

			Container.Dispose();
			base.DisposeManaged();
		}
	}
}