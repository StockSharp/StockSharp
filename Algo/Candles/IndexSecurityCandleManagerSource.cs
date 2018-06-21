#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: IndexSecurityCandleManagerSource.cs
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

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class IndexSecurityCandleManagerSource : Disposable, ICandleSource<Candle>
	{
		private sealed class IndexSeriesInfo : Disposable
		{
			private readonly ICandleManager _candleManager;
			private readonly ISet<CandleSeries> _innerSeries;
			private readonly DateTimeOffset? _from;
			private readonly DateTimeOffset? _to;
			private readonly Action<Candle> _processing;
			private readonly Action _stopped;
			//private int _startedSeriesCount;
			private readonly object _lock = new object();
			private readonly IndexCandleBuilder _builder;

			public IndexSeriesInfo(ICandleManager candleManager, Type candleType, IEnumerable<CandleSeries> innerSeries, DateTimeOffset? from, DateTimeOffset? to, IndexSecurity security, Action<Candle> processing, Action stopped)
			{
				if (innerSeries == null)
					throw new ArgumentNullException(nameof(innerSeries));

				if (security == null)
					throw new ArgumentNullException(nameof(security));

				_candleManager = candleManager ?? throw new ArgumentNullException(nameof(candleManager));
				_innerSeries = innerSeries.ToHashSet();
				_from = from;
				_to = to;
				_processing = processing ?? throw new ArgumentNullException(nameof(processing));
				_stopped = stopped ?? throw new ArgumentNullException(nameof(stopped));

				candleManager.Processing += OnInnerSourceProcessCandle;
				candleManager.Stopped += OnInnerSourceStopped;

				_builder = new IndexCandleBuilder(security, candleType, security.IgnoreErrors);

				//_innerSeries.ForEach(s =>
				//{
				//	s.ProcessCandle += OnInnerSourceProcessCandle;
				//	s.Stopped += OnInnerSourceStopped;

				//	_startedSeriesCount++;
				//});
			}

			public void Start()
			{
				_builder.Reset();

				CandleSeries[] series;

				lock (_lock)
					series = _innerSeries.ToArray();

				series.ForEach(s => _candleManager.Start(s, _from, _to));
			}

			private void OnInnerSourceProcessCandle(CandleSeries series, Candle candle)
			{
				if (candle.State != CandleStates.Finished)
					return;

				lock (_lock)
				{
					if (!_innerSeries.Contains(series))
						return;
				}

				_builder.ProcessCandle(candle).ForEach(_processing);
			}

			private void OnInnerSourceStopped(CandleSeries series)
			{
				lock (_lock)
				{
					if (!_innerSeries.Remove(series))
						return;

					if (_innerSeries.Count > 0)
						return;
				}

				_candleManager.Processing -= OnInnerSourceProcessCandle;
				_candleManager.Stopped -= OnInnerSourceStopped;

				//// отписываемся только после обработки остановки всех серий
				//_innerSeries.ForEach(s => s.Stopped -= OnInnerSourceStopped);

				_stopped();
			}

			protected override void DisposeManaged()
			{
				base.DisposeManaged();

				CandleSeries[] series;

				lock (_lock)
					series = _innerSeries.ToArray();

				series.ForEach(_candleManager.Stop);

				//_innerSeries.ForEach(s =>
				//{
				//	s.Dispose();
				//	s.ProcessCandle -= OnInnerSourceProcessCandle;
				//});
			}
		}

		private readonly ISecurityProvider _securityProvider;
		private readonly DateTimeOffset? _from;
		private readonly DateTimeOffset? _to;
		private readonly SynchronizedDictionary<CandleSeries, IndexSeriesInfo> _info = new SynchronizedDictionary<CandleSeries, IndexSeriesInfo>();
		private readonly ICandleManager _candleManager;

		public IndexSecurityCandleManagerSource(ICandleManager candleManager, ISecurityProvider securityProvider, DateTimeOffset? from, DateTimeOffset? to)
		{
			_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
			_from = from;
			_to = to;
			_candleManager = candleManager ?? throw new ArgumentNullException(nameof(candleManager));
		}

		public int SpeedPriority => 2;

		public event Action<CandleSeries, Candle> Processing;
		public event Action<CandleSeries> Stopped;
		public event Action<Exception> Error;

		IEnumerable<Range<DateTimeOffset>> ICandleSource<Candle>.GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (series.Security is IndexSecurity)
			{
				yield return new Range<DateTimeOffset>(_from ?? DateTimeOffset.MinValue, _to ?? DateTimeOffset.MaxValue);
			}
		}

		void ICandleSource<Candle>.Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var indexSecurity = (IndexSecurity)series.Security;

			var basketInfo = new IndexSeriesInfo(_candleManager,
				series.CandleType,
				indexSecurity
					.GetInnerSecurities(_securityProvider)
					.Select(sec => new CandleSeries(series.CandleType, sec, IndexCandleBuilder.CloneArg(series.Arg, sec))
					{
						WorkingTime = series.WorkingTime.Clone(),
					})
					.ToArray(),
				from, to, indexSecurity,
				c =>
				{
					//if (c.Series == null)
					//{
					//	c.Series = series;
					//	c.Source = this;
					//}

					_candleManager.Container.AddCandle(series, c);
					Processing?.Invoke(series, c);
				},
				() => Stopped?.Invoke(series));

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

		//private static object CloneArg(object arg, Security security)
		//{
		//	if (arg == null)
		//		throw new ArgumentNullException(nameof(arg));

		//	if (security == null)
		//		throw new ArgumentNullException(nameof(security));

		//	var clone = arg;
		//	clone.DoIf<object, ICloneable>(c => clone = c.Clone());
		//	clone.DoIf<object, Unit>(u => u.SetSecurity(security));
		//	return clone;
		//}
	}
}