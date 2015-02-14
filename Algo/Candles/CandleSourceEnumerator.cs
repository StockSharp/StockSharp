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

			public TSource Source { get; private set; }
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
		private readonly Action<TValue> _processing;
		private readonly Action _stopped;
		private readonly SynchronizedQueue<SourceInfo> _sources = new SynchronizedQueue<SourceInfo>();
		private bool _manualStopped;

		public CandleSourceEnumerator(CandleSeries series, DateTimeOffset from, DateTimeOffset to, IEnumerable<TSource> sources, Action<TValue> processing, Action stopped)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (from >= to)
				throw new ArgumentOutOfRangeException("to", to, LocalizedStrings.Str635Params.Put(from));

			if (sources == null)
				throw new ArgumentNullException("sources");

			if (processing == null)
				throw new ArgumentNullException("processing");

			if (stopped == null)
				throw new ArgumentNullException("stopped");

			var info = new List<SourceInfo>();

			var requestRanges = new List<Range<DateTimeOffset>>(new[] { new Range<DateTimeOffset>(from, to) });

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

			CurrentSource.Start(_series, info.Range.Min, info.Range.Max);
		}

		private void RaiseStop()
		{
			_stopped();
		}

		private void OnProcessing(CandleSeries series, TValue value)
		{
			if (series != _series)
				return;

			_processing(value);
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