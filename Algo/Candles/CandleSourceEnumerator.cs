#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Algo
File: CandleSourceEnumerator.cs
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
	using StockSharp.Localization;

	class CandleSourceEnumerator<TSource, TValue>
		where TSource : class, ICandleSource<TValue>
	{
		private sealed class SourceInfo
		{
			public SourceInfo(TSource source, Range<DateTimeOffset> range)
			{
				Source = source;
				Range = range;
			}

			public TSource Source { get; }
			public Range<DateTimeOffset> Range { get; private set; }

			public override string ToString()
			{
				return Range.ToString();
			}

			public void ExtendRange(Range<DateTimeOffset> additionalRange)
			{
				Range = new Range<DateTimeOffset>(Range.Min, additionalRange.Max);
			}
		}

		private readonly CandleSeries _series;
		private readonly Func<TValue, DateTimeOffset> _processing;
		private readonly Action _stopped;
		private readonly SynchronizedQueue<SourceInfo> _sources = new SynchronizedQueue<SourceInfo>();
		private bool _manualStopped;
		private DateTimeOffset? _nextSourceBegin;

		public CandleSourceEnumerator(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to, IEnumerable<TSource> sources, Func<TValue, DateTimeOffset> processing, Action stopped)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (from >= to)
				throw new ArgumentOutOfRangeException(nameof(to), to, LocalizedStrings.Str635Params.Put(from));

			if (sources == null)
				throw new ArgumentNullException(nameof(sources));

			if (processing == null)
				throw new ArgumentNullException(nameof(processing));

			if (stopped == null)
				throw new ArgumentNullException(nameof(stopped));

			var info = new List<SourceInfo>();

			var requestRanges = new List<Range<DateTimeOffset>>(new[] { new Range<DateTimeOffset>(from ?? DateTimeOffset.MinValue, to ?? DateTimeOffset.MaxValue) });

			foreach (var group in sources.GroupBy(s => s.SpeedPriority).OrderBy(g => g.Key))
			{
				foreach (var source in group)
				{
					foreach (var supportedRange in source.GetSupportedRanges(series))
					{
						var index = 0;

						while (index < requestRanges.Count)
						{
							var requestRange = requestRanges[index];

							var intersectedRange = requestRange.Intersect(supportedRange);

							if (intersectedRange != null)
							{
								info.Add(new SourceInfo(source, intersectedRange));

								requestRanges.Remove(requestRange);

								var results = requestRange.Exclude(supportedRange).ToArray();
								requestRanges.InsertRange(index, results);

								index += results.Length;
							}
							else
								index++;
						}
					}
				}
			}

			SourceInfo prevInfo = null;

			foreach (var i in info.OrderBy(i => i.Range.Min))
			{
				if (prevInfo == null)
				{
					_sources.Enqueue(i);
					prevInfo = i;
				}
				else
				{
					if (prevInfo.Source == i.Source)
						prevInfo.ExtendRange(i.Range);
					else
					{
						_sources.Enqueue(i);
						prevInfo = i;
					}
				}
			}

			_series = series;
			_processing = processing;
			_stopped = stopped;
		}

		public TSource CurrentSource { get; private set; }

		public void Start()
		{
			if (_sources.IsEmpty())
			{
				RaiseStop();
				return;
			}

			var info = _sources.Dequeue();

			CurrentSource = info.Source;

			CurrentSource.Processing += OnProcessing;
			CurrentSource.Stopped += OnStopped;

			var next = _sources.Count > 0 ? _sources.Peek() : null;
			_nextSourceBegin = next?.Range.Min;

			var from = info.Range.Min != DateTimeOffset.MinValue ? info.Range.Min : (DateTimeOffset?)null;
			var to = info.Range.Max != DateTimeOffset.MaxValue ? info.Range.Max : (DateTimeOffset?)null;

			CurrentSource.Start(_series, from, to);
		}

		private void RaiseStop()
		{
			_stopped();
		}

		private void OnProcessing(CandleSeries series, TValue value)
		{
			if (series != _series)
				return;

			var date = _processing(value);

			if (_nextSourceBegin != null && date > _nextSourceBegin)
				CurrentSource.Stop(series);
		}

		private void OnStopped(CandleSeries series)
		{
			if (series != _series)
				return;

			var raiseStop = false;

			lock (_sources.SyncRoot)
			{
				CurrentSource.Processing -= OnProcessing;
				CurrentSource.Stopped -= OnStopped;
				CurrentSource = null;
				_nextSourceBegin = null;

				if (_manualStopped || _sources.IsEmpty())
					raiseStop = true;
				else
					Start();
			}

			if (raiseStop)
				RaiseStop();
		}

		public void Stop()
		{
			var raiseStop = false;

			lock (_sources.SyncRoot)
			{
				if (CurrentSource != null)
				{
					_manualStopped = true;
					CurrentSource.Stop(_series);
				}
				else
					raiseStop = true;
			}

			if (raiseStop)
				RaiseStop();
		}
	}
}