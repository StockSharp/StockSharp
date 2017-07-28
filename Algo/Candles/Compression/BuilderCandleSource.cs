namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Interface of candles source for <see cref="ICandleManager"/> that builds candles with <see cref="ICandleBuilder"/>.
	/// </summary>
	public interface IBuilderCandleSource : ICandleSource<Candle>
	{
		/// <summary>
		/// Data sources.
		/// </summary>
		ICandleBuilderSourceList Sources { get; }
	}

	/// <summary>
	/// The candles source for <see cref="ICandleManager"/> that builds candles with <see cref="ICandleBuilder"/>.
	/// </summary>
	/// <typeparam name="TBuilder">Type of builder.</typeparam>
	public class BuilderCandleSource<TBuilder> : IBuilderCandleSource
		where TBuilder : ICandleBuilder, new()
	{
		private sealed class CandleSeriesInfo
		{
			private readonly CandleSourceEnumerator<ICandleBuilderSource, IEnumerable<ICandleBuilderSourceValue>> _enumerator;

			public CandleSeries Series { get; }

			public MarketDataMessage Message { get; }

			public Candle CurrentCandle { get; set; }

			public CandleMessage CurrentCandleMessage { get; set; }

			public CandleSeriesInfo(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to, IEnumerable<ICandleBuilderSource> sources, Func<CandleSeriesInfo, IEnumerable<ICandleBuilderSourceValue>, DateTimeOffset> handler, Action<CandleSeries> stopped)
			{
				if (series == null)
					throw new ArgumentNullException(nameof(series));

				if (handler == null)
					throw new ArgumentNullException(nameof(handler));

				if (stopped == null)
					throw new ArgumentNullException(nameof(stopped));

				Series = series;
				Message = series.ToMarketDataMessage(true, from, to);

				_enumerator = new CandleSourceEnumerator<ICandleBuilderSource, IEnumerable<ICandleBuilderSourceValue>>(series, from, to,
					sources, v => handler(this, v), () => stopped(series));
			}

			public void Start()
			{
				_enumerator.Start();
			}

			public void Stop()
			{
				_enumerator.Stop();
			}
		}

		private sealed class CandleBuilderSourceList : SynchronizedList<ICandleBuilderSource>, ICandleBuilderSourceList
		{
			protected override void OnAdded(ICandleBuilderSource item)
			{
				base.OnAdded(item);
				Subscribe(item);
			}

			protected override bool OnRemoving(ICandleBuilderSource item)
			{
				UnSubscribe(item);
				return base.OnRemoving(item);
			}

			protected override void OnInserted(int index, ICandleBuilderSource item)
			{
				base.OnInserted(index, item);
				Subscribe(item);
			}

			protected override bool OnClearing()
			{
				foreach (var item in this)
					UnSubscribe(item);

				return base.OnClearing();
			}

			private void Subscribe(ICandleBuilderSource source)
			{
				//source.NewValues += _builder.OnNewValues;
				//source.Error += _builder.RaiseError;
			}

			private void UnSubscribe(ICandleBuilderSource source)
			{
				//source.NewValues -= _builder.OnNewValues;
				//source.Error -= _builder.RaiseError;
				source.Dispose();
			}
		}

		private readonly SynchronizedDictionary<CandleSeries, CandleSeriesInfo> _info = new SynchronizedDictionary<CandleSeries, CandleSeriesInfo>();

		private readonly TBuilder _builder;

		/// <summary>
		/// The source priority by speed (0 - the best).
		/// </summary>
		public int SpeedPriority => 2;

		/// <summary>
		/// Data sources.
		/// </summary>
		public ICandleBuilderSourceList Sources { get; }

		/// <summary>
		/// A new value for processing occurrence event.
		/// </summary>
		public event Action<CandleSeries, Candle> Processing;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// The data transfer error event.
		/// </summary>
		public event Action<Exception> Error;

		/// <summary>
		/// Initialize <see cref="BuilderCandleSource{TBuilder}"/>.
		/// </summary>
		public BuilderCandleSource()
		{
			_builder = new TBuilder();

			Sources = new CandleBuilderSourceList();
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var dataType = series
				.CandleType
				.ToCandleMessageType()
				.ToCandleMarketDataType();

			if (dataType != _builder.CandleType)
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return Sources.SelectMany(s => s.GetSupportedRanges(series)).JoinRanges().ToArray();
		}

		/// <summary>
		/// To send data request.
		/// </summary>
		/// <param name="series">The candles series for which data receiving should be started.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public void Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			CandleSeriesInfo info;

			lock (_info.SyncRoot)
			{
				info = _info.TryGetValue(series);

				if (info != null)
					throw new ArgumentException(LocalizedStrings.Str636Params.Put(series), nameof(series));

				info = new CandleSeriesInfo(series, from, to, Sources, OnNewValues, s =>
				{
					_info.Remove(s);
					OnStopped(s);
				});

				//Container.Start(series, from, to);

				_info.Add(series, info);
			}

			info.Start();
		}

		/// <summary>
		/// To stop data receiving starting through <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var info = _info.TryGetValue(series);

			info?.Stop();
		}

		private DateTimeOffset OnNewValues(CandleSeriesInfo info, IEnumerable<ICandleBuilderSourceValue> values)
		{
			ICandleBuilderSourceValue lastValue = null;

			foreach (var value in values)
			{
				var messages = _builder.Process(info.Message, info.CurrentCandleMessage, value);

				foreach (var candleMsg in messages)
				{
					info.CurrentCandleMessage = candleMsg;

					if (info.CurrentCandle != null && info.CurrentCandle.OpenTime == candleMsg.OpenTime)
					{
						if (info.CurrentCandle.State == CandleStates.Finished)
							continue;

						info.CurrentCandle.Update(candleMsg);
					}
					else
						info.CurrentCandle = candleMsg.ToCandle(info.Series);

					Processing?.Invoke(info.Series, info.CurrentCandle);

					//if (candleMsg.IsFinished)
					//	OnStopped(info.Series);
				}

				lastValue = value;
			}

			return lastValue?.Time ?? default(DateTimeOffset);
		}

		private void OnStopped(CandleSeries series)
		{
			_info.Remove(series);
			Stopped?.Invoke(series);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			Sources.Clear();
			//Container.Dispose();
		}
	}
}