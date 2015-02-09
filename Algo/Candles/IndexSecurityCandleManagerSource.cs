namespace StockSharp.Algo.Candles
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class IndexSecurityCandleManagerSource : Disposable, ICandleManagerSource
	{
		private sealed class IndexSeriesInfo : Disposable
		{
			private readonly ICandleManager _candleManager;
			private readonly IEnumerable<CandleSeries> _innerSeries;
			private readonly DateTimeOffset _from;
			private readonly DateTimeOffset _to;
			private readonly Action<Candle> _processing;
			private readonly Action _stopped;
			private int _startedSeriesCount;
			private readonly object _lock = new object();
			private readonly IndexCandleBuilder _builder;

			public IndexSeriesInfo(ICandleManager candleManager, IEnumerable<CandleSeries> innerSeries, DateTimeOffset from, DateTimeOffset to, IndexSecurity security, Action<Candle> processing, Action stopped)
			{
				if (candleManager == null)
					throw new ArgumentNullException("candleManager");

				if (innerSeries == null)
					throw new ArgumentNullException("innerSeries");

				if (security == null)
					throw new ArgumentNullException("security");

				if (processing == null)
					throw new ArgumentNullException("processing");

				if (stopped == null)
					throw new ArgumentNullException("stopped");

				_candleManager = candleManager;
				_innerSeries = innerSeries;
				_from = from;
				_to = to;
				_processing = processing;
				_stopped = stopped;

				_builder = new IndexCandleBuilder(security);

				_innerSeries.ForEach(s =>
				{
					s.ProcessCandle += OnInnerSourceProcessCandle;
					s.Stopped += OnInnerSourceStopped;

					_startedSeriesCount++;
				});
			}

			public void Start()
			{
				_builder.Reset();
				_innerSeries.ForEach(s => _candleManager.Start(s, _from, _to));
			}

			private void OnInnerSourceProcessCandle(Candle candle)
			{
				if (candle.State != CandleStates.Finished)
					return;

				_builder.ProcessCandle(candle).ForEach(_processing);
			}

			private void OnInnerSourceStopped()
			{
				lock (_lock)
				{
					if (--_startedSeriesCount > 0)
						return;
				}

				// отписываемся только после обработки остановки всех серий
				_innerSeries.ForEach(s => s.Stopped -= OnInnerSourceStopped);

				_stopped();
			}

			protected override void DisposeManaged()
			{
				base.DisposeManaged();

				_innerSeries.ForEach(s =>
				{
					s.Dispose();
					s.ProcessCandle -= OnInnerSourceProcessCandle;
				});
			}
		}

		private readonly DateTimeOffset _from;
		private readonly DateTimeOffset _to;
		private readonly SynchronizedDictionary<CandleSeries, IndexSeriesInfo> _info = new SynchronizedDictionary<CandleSeries, IndexSeriesInfo>();

		public IndexSecurityCandleManagerSource(ICandleManager candleManager, DateTimeOffset from, DateTimeOffset to)
		{
			_from = from;
			_to = to;
			CandleManager = candleManager;
		}

		public ICandleManager CandleManager { get; set; }

		public int SpeedPriority
		{
			get { return 2; }
		}

		public event Action<CandleSeries, Candle> Processing;
		public event Action<CandleSeries> Stopped;
		public event Action<Exception> ProcessDataError;

		IEnumerable<Range<DateTimeOffset>> ICandleSource<Candle>.GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (series.Security is IndexSecurity)
			{
				yield return new Range<DateTimeOffset>(_from, _to);
			}
		}

		void ICandleSource<Candle>.Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var indexSecurity = (IndexSecurity)series.Security;

			var basketInfo = new IndexSeriesInfo(CandleManager,
				indexSecurity
					.InnerSecurities
					.Select(sec => new CandleSeries(series.CandleType, sec, CloneArg(series.Arg, sec))
					{
						WorkingTime = series.WorkingTime.Clone(),
					}).ToArray(),
				from, to, indexSecurity,
				c =>
				{
					if (c.Series == null)
					{
						c.Series = series;
						c.Source = this;
					}

					CandleManager.Container.AddCandle(series, c);
					Processing.SafeInvoke(series, c);
				},
				() =>
					Stopped.SafeInvoke(series));

			_info.Add(series, basketInfo);

			basketInfo.Start();
		}

		void ICandleSource<Candle>.Stop(CandleSeries series)
		{
			lock (_info.SyncRoot)
			{
				var basketInfo = _info.TryGetValue(series);

				if (basketInfo == null)
					return;

				basketInfo.Dispose();

				_info.Remove(series);
			}
		}

		private static object CloneArg(object arg, Security security)
		{
			if (arg == null)
				throw new ArgumentNullException("arg");

			if (security == null)
				throw new ArgumentNullException("security");

			var clone = arg;
			clone.DoIf<object, ICloneable>(c => clone = c.Clone());
			clone.DoIf<object, Unit>(u => u.SetSecurity(security));
			return clone;
		}
	}
}