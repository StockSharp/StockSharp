#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Candles.Compression.Algo
File: CandleBuilder.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Algo.Storages;
	using StockSharp.Messages;
	using StockSharp.Localization;

	// mika вынесен за пределы CandleBuilder, так как в дженерик классе статические переменные инициализируются каждый раз для нового параметра
	class Holder
	{
		public static readonly ICandleBuilderSource TradeStorage = new TradeStorageCandleBuilderSource();
		public static readonly ICandleBuilderSource OrderLogStorage = new OrderLogStorageCandleBuilderSource();
	}

	/// <summary>
	/// The candles builder. It connects to the <see cref="ICandleSource{T}.Processing"/> event through the <see cref="ICandleBuilderSource"/> source and creates candles on the basis of the data received by specified criteria.
	/// </summary>
	/// <typeparam name="TCandle">The type of candle which the builder will create.</typeparam>
	public abstract class CandleBuilder<TCandle> : BaseLogReceiver, ICandleBuilder, IStorageCandleSource
		where TCandle : Candle
	{
		private sealed class CandleSeriesInfo
		{
			private readonly CandleSourceEnumerator<ICandleBuilderSource, IEnumerable<ICandleBuilderSourceValue>> _enumerator;

			public CandleSeriesInfo(CandleSeries series, DateTimeOffset from, DateTimeOffset to, IEnumerable<ICandleBuilderSource> sources, Func<CandleSeries, IEnumerable<ICandleBuilderSourceValue>, DateTimeOffset> handler, Action<CandleSeries> stopped)
			{
				if (series == null)
					throw new ArgumentNullException(nameof(series));

				if (handler == null)
					throw new ArgumentNullException(nameof(handler));

				if (stopped == null)
					throw new ArgumentNullException(nameof(stopped));

				_enumerator = new CandleSourceEnumerator<ICandleBuilderSource, IEnumerable<ICandleBuilderSourceValue>>(series, from, to,
					sources, v => handler(series, v), () => stopped(series));
			}

			public Candle CurrentCandle { get; set; }
			public VolumeProfile VolumeProfile { get; set; }

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
			private readonly CandleBuilder<TCandle> _builder;

			public CandleBuilderSourceList(CandleBuilder<TCandle> builder)
			{
				_builder = builder;
			}

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
				source.Error += _builder.RaiseError;
			}

			private void UnSubscribe(ICandleBuilderSource source)
			{
				//source.NewValues -= _builder.OnNewValues;
				source.Error -= _builder.RaiseError;
				source.Dispose();
			}
		}

		private readonly SynchronizedDictionary<CandleSeries, CandleSeriesInfo> _info = new SynchronizedDictionary<CandleSeries, CandleSeriesInfo>();

		/// <summary>
		/// Initialize <see cref="CandleBuilder{T}"/>.
		/// </summary>
		protected CandleBuilder()
			: this(new CandleBuilderContainer())
		{
		}

		/// <summary>
		/// Initialize <see cref="CandleBuilder{T}"/>.
		/// </summary>
		/// <param name="container">The data container.</param>
		protected CandleBuilder(ICandleBuilderContainer container)
		{
			if (container == null)
				throw new ArgumentNullException(nameof(container));

			Sources = new CandleBuilderSourceList(this) { Holder.TradeStorage, Holder.OrderLogStorage };

			Container = container;
		}

		/// <summary>
		/// Data sources.
		/// </summary>
		public ICandleBuilderSourceList Sources { get; }

		/// <summary>
		/// The data container.
		/// </summary>
		public ICandleBuilderContainer Container { get; }

		/// <summary>
		/// The candles manager. To be filled in if the builder is a source inside the <see cref="ICandleManager.Sources"/>.
		/// </summary>
		public ICandleManager CandleManager { get; set; }

		Type ICandleBuilder.CandleType => typeof(TCandle);

		private IStorageRegistry _storageRegistry;

		/// <summary>
		/// The data storage. To be sent to all sources that implement the interface <see cref="IStorageCandleSource"/>.
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
		/// The source priority by speed (0 - the best).
		/// </summary>
		public int SpeedPriority => 2;

		/// <summary>
		/// A new value for processing occurrence event.
		/// </summary>
		public event Action<CandleSeries, Candle> Processing;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// The candles creating error event.
		/// </summary>
		public event Action<Exception> Error;

		#region ICandleSource members

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public virtual IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (series.CandleType != typeof(TCandle))
				return Enumerable.Empty<Range<DateTimeOffset>>();

			return Sources.SelectMany(s => s.GetSupportedRanges(series)).JoinRanges().ToArray();
		}

		/// <summary>
		/// To start getting of candles for the specified series.
		/// </summary>
		/// <param name="series">The series of candles for which candles getting should be started.</param>
		/// <param name="from">The initial date from which candles getting should be started.</param>
		/// <param name="to">The final date by which candles should be get.</param>
		public virtual void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
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

				Container.Start(series, from, to);

				_info.Add(series, info);
			}

			info.Start();
		}

		/// <summary>
		/// To stop candles getting started via <see cref="Start"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public virtual void Stop(CandleSeries series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var info = _info.TryGetValue(series);

			info?.Stop();
		}

		#endregion

		private void OnStopped(CandleSeries series)
		{
			_info.Remove(series);
			Stopped?.Invoke(series);
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="values">New data.</param>
		/// <returns>Time of the last item.</returns>
		protected virtual DateTimeOffset OnNewValues(CandleSeries series, IEnumerable<ICandleBuilderSourceValue> values)
		{
			if (values == null)
				throw new ArgumentNullException(nameof(values));

			var info = _info.TryGetValue(series);

			if (info == null)
				return default(DateTimeOffset);

			ICandleBuilderSourceValue lastValue = null;

			foreach (var value in values)
			{
				var valueAdded = false;

				while (true)
				{
					var currCandle = info.CurrentCandle;

					var candle = ProcessValue(series, (TCandle)currCandle, value);

					if (candle == null)
					{
						// skip the value that cannot be processed
						break;
					}

					if (candle == currCandle)
					{
						if (!valueAdded)
						{
							Container.AddValue(series, candle, value);

							if (series.IsCalcVolumeProfile)
							{
								if (info.VolumeProfile == null)
									throw new InvalidOperationException();

								info.VolumeProfile.Update(value);
							}
						}

						//candle.State = CandleStates.Changed;
						RaiseProcessing(series, candle);

						break;
					}
					else
					{
						if (currCandle != null)
						{
							info.CurrentCandle = null;
							info.VolumeProfile = null;

							currCandle.State = CandleStates.Finished;
							RaiseProcessing(series, currCandle);
						}

						info.CurrentCandle = candle;

						if (series.IsCalcVolumeProfile)
						{
							info.VolumeProfile = new VolumeProfile();
							info.VolumeProfile.Update(value);

                            candle.PriceLevels = info.VolumeProfile.PriceLevels;
						}

						Container.AddValue(series, candle, value);
						valueAdded = true;

						candle.State = CandleStates.Active;
						RaiseProcessing(series, candle);
					}
				}

				lastValue = value;
			}

			return lastValue?.Time ?? default(DateTimeOffset);
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected virtual TCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			throw new NotSupportedException(LocalizedStrings.Str637);
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected virtual bool IsCandleFinishedBeforeChange(CandleSeries series, TCandle candle, ICandleBuilderSourceValue value)
		{
			return false;
		}

		/// <summary>
		/// To fill in the initial candle settings.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data.</param>
		/// <returns>Candle.</returns>
		protected virtual TCandle FirstInitCandle(CandleSeries series, TCandle candle, ICandleBuilderSourceValue value)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			candle.Security = value.Security;

			candle.OpenPrice = value.Price;
			candle.ClosePrice = value.Price;
			candle.LowPrice = value.Price;
			candle.HighPrice = value.Price;
			//candle.TotalPrice = value.Price;

			candle.OpenVolume = value.Volume;
			candle.CloseVolume = value.Volume;
			candle.LowVolume = value.Volume;
			candle.HighVolume = value.Volume;
			//candle.TotalVolume = value.Volume;

			return candle;
		}

		/// <summary>
		/// To update the candle data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data.</param>
		protected virtual void UpdateCandle(CandleSeries series, TCandle candle, ICandleBuilderSourceValue value)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (value.Price < candle.LowPrice)
			{
				candle.LowPrice = value.Price;
				candle.LowTime = value.Time;
			}

			if (value.Price > candle.HighPrice)
			{
				candle.HighPrice = value.Price;
				candle.HighTime = value.Time;
			}

			candle.ClosePrice = value.Price;
			candle.TotalPrice += value.Price * value.Volume;

			candle.LowVolume = (candle.LowVolume ?? 0m).Min(value.Volume);
			candle.HighVolume = (candle.HighVolume ?? 0m).Max(value.Volume);
			candle.CloseVolume = value.Volume;
			candle.TotalVolume += value.Volume;

			if (value.OrderDirection != null)
			{
				candle.RelativeVolume = (candle.RelativeVolume ?? 0) + (value.OrderDirection == Sides.Buy ? value.Volume : -value.Volume);
			}

			candle.CloseTime = value.Time;
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">The new data by which it is decided to start or end the current candle creation.</param>
		/// <returns>A new candle. If there is not necessary to create a new candle, then <paramref name="currentCandle" /> is returned. If it is impossible to create a new candle (<paramref name="value" /> can not be applied to candles), then <see langword="null" /> is returned.</returns>
		Candle ICandleBuilder.ProcessValue(CandleSeries series, Candle currentCandle, ICandleBuilderSourceValue value)
		{
			return ProcessValue(series, (TCandle)currentCandle, value);
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">The new data by which it is decided to start or end the current candle creation.</param>
		/// <returns>A new candle. If there is not necessary to create a new candle, then <paramref name="currentCandle" /> is returned. If it is impossible to create a new candle (<paramref name="value" /> can not be applied to candles), then <see langword="null" /> is returned.</returns>
		public virtual TCandle ProcessValue(CandleSeries series, TCandle currentCandle, ICandleBuilderSourceValue value)
		{
			if (currentCandle == null || IsCandleFinishedBeforeChange(series, currentCandle, value))
			{
				currentCandle = CreateCandle(series, value);
				this.AddDebugLog("NewCandle {0} ForValue {1}", currentCandle, value);
				return currentCandle;
			}

			UpdateCandle(series, currentCandle, value);
			this.AddDebugLog("UpdatedCandle {0} ForValue {1}", currentCandle, value);
			return currentCandle;
		}

		/// <summary>
		/// To call the event <see cref="Processing"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		protected virtual void RaiseProcessing(CandleSeries series, Candle candle)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			// mika: чтобы построение свечек продолжалось, даже если в пользовательских обработчиках ошибки.
			// иначе это может привести к испорченной последовательности последующих вызовов свечек.
			// нашел багу эспер
			try
			{
				Processing?.Invoke(series, candle);
			}
			catch (Exception ex)
			{
				RaiseError(ex);
			}
		}

		/// <summary>
		/// To call the event <see cref="Error"/>.
		/// </summary>
		/// <param name="error">Error info.</param>
		protected virtual void RaiseError(Exception error)
		{
			Error?.Invoke(error);
			this.AddErrorLog(error);
		}

		/// <summary>
		/// To finish the candle forcibly.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		protected void ForceFinishCandle(CandleSeries series, Candle candle)
		{
			var info = _info.TryGetValue(series);

			if (info == null)
				return;

			var isNone = candle.State == CandleStates.None;

			// если успела прийти новая свеча
			if (isNone && info.CurrentCandle != null)
				return;

			if (!isNone && info.CurrentCandle != candle)
				return;

			info.CurrentCandle = isNone ? null : candle;

			if (!isNone)
				candle.State = CandleStates.Finished;

			RaiseProcessing(series, candle);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Sources.Clear();
			Container.Dispose();
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="TimeFrameCandle"/> type.
	/// </summary>
	public class TimeFrameCandleBuilder : CandleBuilder<TimeFrameCandle>
	{
		//private sealed class TimeoutInfo : Disposable
		//{
		//	private readonly MarketTimer _timer;
		//	private DateTime _emptyCandleTime;
		//	private readonly TimeSpan _timeFrame;
		//	private readonly TimeSpan _offset;
		//	private DateTime _nextTime;

		//	public TimeoutInfo(CandleSeries series, TimeFrameCandleBuilder builder)
		//	{
		//		if (series == null)
		//			throw new ArgumentNullException("series");

		//		if (series.Security.Connector == null)
		//			throw new ArgumentException("Инструмент {0} не имеет информации о подключении.".Put(series.Security), "series");

		//		if (builder == null)
		//			throw new ArgumentNullException("builder");

		//		_timeFrame = (TimeSpan)series.Arg;
		//		_offset = TimeSpan.FromTicks((long)((decimal)((decimal)_timeFrame.Ticks + builder.Timeout)));

		//		var security = series.Security;
		//		var connector = security.Connector;

		//		var isFirstTime = true;

		//		_timer = new MarketTimer(connector, () =>
		//		{
		//			if (isFirstTime)
		//			{
		//				isFirstTime = false;

		//				var bounds = _timeFrame.GetCandleBounds(security);

		//				_emptyCandleTime = bounds.Min;
		//				_nextTime = GetLimitTime(bounds.Min);

		//				return;
		//			}

		//			if (security.GetMarketTime() >= _nextTime)
		//			{
		//				_nextTime += _timeFrame;

		//				var candle = LastCandle;

		//				if (candle == null)
		//				{
		//					candle = new TimeFrameCandle
		//					{
		//						Security = security,
		//						TimeFrame = _timeFrame,
		//						OpenTime = _emptyCandleTime,
		//						CloseTime = _emptyCandleTime + _timeFrame,
		//					};

		//					_emptyCandleTime += _timeFrame;

		//					if (!builder.GenerateEmptyCandles)
		//						return;
		//				}

		//				builder.ForceFinishCandle(series, candle);
		//			}
		//		}).Interval(_timeFrame).Start();
		//	}

		//	private TimeFrameCandle _lastCandle;

		//	public TimeFrameCandle LastCandle
		//	{
		//		private get { return _lastCandle; }
		//		set
		//		{
		//			_lastCandle = value;
		//			_emptyCandleTime = value.OpenTime + _timeFrame;
		//			_nextTime = GetLimitTime(value.OpenTime);
		//		}
		//	}

		//	private DateTime GetLimitTime(DateTime currentCandleTime)
		//	{
		//		return currentCandleTime + _offset;
		//	}

		//	protected override void DisposeManaged()
		//	{
		//		base.DisposeManaged();
		//		_timer.Dispose();
		//	}
		//}

		//private readonly SynchronizedDictionary<CandleSeries, TimeoutInfo> _timeoutInfos = new SynchronizedDictionary<CandleSeries, TimeoutInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="TimeFrameCandleBuilder"/>.
		/// </summary>
		public TimeFrameCandleBuilder()
		{
			GenerateEmptyCandles = true;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TimeFrameCandleBuilder"/>.
		/// </summary>
		/// <param name="container">The data container.</param>
		public TimeFrameCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
			GenerateEmptyCandles = true;
		}

		private Unit _timeout = 10.Percents();

		/// <summary>
		/// The time shift from the time frame end after which a signal is sent to close the unclosed candle forcibly. The default is 10% of the time frame.
		/// </summary>
		public Unit Timeout
		{
			get { return _timeout; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.OffsetValueIncorrect);

				_timeout = value;
			}
		}

		/// <summary>
		/// Whether to create empty candles (<see cref="CandleStates.None"/>) in the lack of trades. The default mode is enabled.
		/// </summary>
		public bool GenerateEmptyCandles { get; set; }

		/// <summary>
		/// To start getting of candles for the specified series.
		/// </summary>
		/// <param name="series">The series of candles for which candles getting should be started.</param>
		/// <param name="from">The initial date from which candles getting should be started.</param>
		/// <param name="to">The final date by which candles should be get.</param>
		public override void Start(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			base.Start(series, from, to);

			if (Timeout == 0)
				return;

			// TODO mika временно выключил, ошибки, нужно протестировать
			//_timeoutInfos.Add(series, new TimeoutInfo(series, this));
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series).ToArray();

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is TimeSpan))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), nameof(series));

				if ((TimeSpan)series.Arg <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(series), series.Arg, LocalizedStrings.Str640);
			}

			return ranges;
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override TimeFrameCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			var timeFrame = (TimeSpan)series.Arg;

			var bounds = timeFrame.GetCandleBounds(value.Time, series.Security.Board, series.WorkingTime);

			if (value.Time < bounds.Min)
				return null;

			var openTime = bounds.Min;

			var candle = FirstInitCandle(series, new TimeFrameCandle
			{
				TimeFrame = timeFrame,
				OpenTime = openTime,
				HighTime = openTime,
				LowTime = openTime,
				CloseTime = openTime, // реальное окончание свечи определяет по последней сделке
			}, value);

			//var info = _timeoutInfos.TryGetValue(series);
			//if (info != null)
			//	info.LastCandle = candle;

			return candle;
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, TimeFrameCandle candle, ICandleBuilderSourceValue value)
		{
			return value.Time < candle.OpenTime || (candle.OpenTime + candle.TimeFrame) <= value.Time;
		}

		///// <summary>
		///// Метод-обработчик события <see cref="CandleSeries.Stopped"/>.
		///// </summary>
		///// <param name="series">Серия свечек, для которой был было вызвано событие.</param>
		//protected override void OnStopped(CandleSeries series)
		//{
		//	_timeoutInfos.Remove(series);
		//	base.OnStopped(series);
		//}

		///// <summary>
		///// Освободить занятые ресурсы.
		///// </summary>
		//protected override void DisposeManaged()
		//{
		//	_timeoutInfos.SyncDo(d => d.Values.ForEach(v => v.Dispose()));
		//	_timeoutInfos.Clear();

		//	base.DisposeManaged();
		//}
	}

	/// <summary>
	/// The builder of candles of <see cref="TickCandle"/> type.
	/// </summary>
	public class TickCandleBuilder : CandleBuilder<TickCandle>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TickCandleBuilder"/>.
		/// </summary>
		public TickCandleBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TickCandleBuilder"/>.
		/// </summary>
		/// <param name="container">The data container.</param>
		public TickCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series).ToArray();

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is int))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), nameof(series));

				if ((int)series.Arg <= 0)
					throw new ArgumentOutOfRangeException(nameof(series), series.Arg, LocalizedStrings.TickCountMustBePositive);
			}

			return ranges;
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override TickCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(series, new TickCandle
			{
				MaxTradeCount = (int)series.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, TickCandle candle, ICandleBuilderSourceValue value)
		{
			return candle.CurrentTradeCount >= candle.MaxTradeCount;
		}

		/// <summary>
		/// To update the candle data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data.</param>
		protected override void UpdateCandle(CandleSeries series, TickCandle candle, ICandleBuilderSourceValue value)
		{
			base.UpdateCandle(series, candle, value);
			candle.CurrentTradeCount++;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="VolumeCandle"/> type.
	/// </summary>
	public class VolumeCandleBuilder : CandleBuilder<VolumeCandle>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeCandleBuilder"/>.
		/// </summary>
		public VolumeCandleBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeCandleBuilder"/>.
		/// </summary>
		/// <param name="container">The data container.</param>
		public VolumeCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series).ToArray();

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is decimal))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), nameof(series));

				if ((decimal)series.Arg <= 0)
					throw new ArgumentOutOfRangeException(nameof(series), series.Arg, LocalizedStrings.VolumeMustBeGreaterThanZero);
			}

			return ranges;
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override VolumeCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(series, new VolumeCandle
			{
				Volume = (decimal)series.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, VolumeCandle candle, ICandleBuilderSourceValue value)
		{
			return candle.TotalVolume >= candle.Volume;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="RangeCandle"/> type.
	/// </summary>
	public class RangeCandleBuilder : CandleBuilder<RangeCandle>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RangeCandleBuilder"/>.
		/// </summary>
		public RangeCandleBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RangeCandleBuilder"/>.
		/// </summary>
		/// <param name="container">The data container.</param>
		public RangeCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series).ToArray();

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is Unit))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), nameof(series));

				if ((Unit)series.Arg <= 0)
					throw new ArgumentOutOfRangeException(nameof(series), series.Arg, LocalizedStrings.PriceRangeMustBeGreaterThanZero);
			}

			return ranges;
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override RangeCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(series, new RangeCandle
			{
				PriceRange = (Unit)series.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, RangeCandle candle, ICandleBuilderSourceValue value)
		{
			return (decimal)(candle.LowPrice + candle.PriceRange) <= candle.HighPrice;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="PnFCandle"/> type.
	/// </summary>
	public class PnFCandleBuilder : CandleBuilder<PnFCandle>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PnFCandleBuilder"/>.
		/// </summary>
		public PnFCandleBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PnFCandleBuilder"/>.
		/// </summary>
		/// <param name="container">The data container.</param>
		public PnFCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series).ToArray();

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is PnFArg))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), nameof(series));
			}

			return ranges;
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override PnFCandle CreateCandle(CandleSeries series, ICandleBuilderSourceValue value)
		{
			var arg = (PnFArg)series.Arg;
			var boxSize = arg.BoxSize;

			if (CandleManager == null)
				throw new InvalidOperationException(LocalizedStrings.CandleManagerIsNotSet);

			var pnfCandle = (PnFCandle)CandleManager.Container.GetCandle(series, 0);

			var candle = new PnFCandle
			{
				PnFArg = arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,

				Security = value.Security,

				OpenVolume = value.Volume,
				CloseVolume = value.Volume,
				LowVolume = value.Volume,
				HighVolume = value.Volume,
				TotalVolume = value.Volume,

				TotalPrice = value.Price * value.Volume,

				Type = pnfCandle == null ? PnFTypes.X : (pnfCandle.Type == PnFTypes.X ? PnFTypes.O : PnFTypes.X),
			};

			if (pnfCandle == null)
			{
				candle.OpenPrice = boxSize.AlignPrice(value.Price, value.Price);

				if (candle.Type == PnFTypes.X)
					candle.ClosePrice = (decimal)(candle.OpenPrice + boxSize);
				else
					candle.ClosePrice = (decimal)(candle.OpenPrice - boxSize);
			}
			else
			{
				candle.OpenPrice = (decimal)((pnfCandle.Type == PnFTypes.X)
					? pnfCandle.ClosePrice - boxSize
					: pnfCandle.ClosePrice + boxSize);

				var price = boxSize.AlignPrice(candle.OpenPrice, value.Price);

				if (candle.Type == PnFTypes.X)
					candle.ClosePrice = (decimal)(price + boxSize);
				else
					candle.ClosePrice = (decimal)(price - boxSize);
			}

			if (candle.Type == PnFTypes.X)
			{
				candle.LowPrice = candle.OpenPrice;
				candle.HighPrice = candle.ClosePrice;
			}
			else
			{
				candle.LowPrice = candle.ClosePrice;
				candle.HighPrice = candle.OpenPrice;
			}

			return candle;
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(CandleSeries series, PnFCandle candle, ICandleBuilderSourceValue value)
		{
			var argSize = candle.PnFArg.BoxSize * candle.PnFArg.ReversalAmount;

			if (candle.Type == PnFTypes.X)
				return candle.ClosePrice - argSize > value.Price;
			else
				return candle.ClosePrice + argSize < value.Price;
		}

		/// <summary>
		/// To update the candle data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data.</param>
		protected override void UpdateCandle(CandleSeries series, PnFCandle candle, ICandleBuilderSourceValue value)
		{
			candle.ClosePrice = candle.PnFArg.BoxSize.AlignPrice(candle.ClosePrice, value.Price);

			if (candle.Type == PnFTypes.X)
				candle.HighPrice = candle.ClosePrice;
			else
				candle.LowPrice = candle.ClosePrice;

			candle.TotalPrice += value.Price * value.Volume;

			candle.LowVolume = (candle.LowVolume ?? 0m).Min(value.Volume);
			candle.HighVolume = (candle.HighVolume ?? 0m).Max(value.Volume);
			candle.CloseVolume = value.Volume;
			candle.TotalVolume += value.Volume;
			candle.CloseTime = value.Time;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="RenkoCandle"/> type.
	/// </summary>
	public class RenkoCandleBuilder : CandleBuilder<RenkoCandle>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RenkoCandleBuilder"/>.
		/// </summary>
		public RenkoCandleBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RenkoCandleBuilder"/>.
		/// </summary>
		/// <param name="container">The data container.</param>
		public RenkoCandleBuilder(ICandleBuilderContainer container)
			: base(container)
		{
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public override IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			var ranges = base.GetSupportedRanges(series).ToArray();

			if (!ranges.IsEmpty())
			{
				if (!(series.Arg is Unit))
					throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), nameof(series));

				if ((Unit)series.Arg <= 0)
					throw new ArgumentOutOfRangeException(nameof(series), series.Arg, LocalizedStrings.Str645);
			}

			return ranges;
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">The new data by which it is decided to start or end the current candle creation.</param>
		/// <returns>A new candle. If there is not necessary to create a new candle, then <paramref name="currentCandle" /> is returned. If it is impossible to create a new candle (<paramref name="value" /> can not be applied to candles), then <see langword="null" /> is returned.</returns>
		public override RenkoCandle ProcessValue(CandleSeries series, RenkoCandle currentCandle, ICandleBuilderSourceValue value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (currentCandle == null)
				return NewCandle(series, value.Price, value.Price, value);

			var delta = currentCandle.BoxSize.Value;

			if (currentCandle.OpenPrice < currentCandle.ClosePrice)
			{
				if ((value.Price - currentCandle.ClosePrice) > delta)
				{
					// New bullish candle
					return NewCandle(series, currentCandle.ClosePrice, currentCandle.ClosePrice + delta, value);
				}

				if ((currentCandle.OpenPrice - value.Price) > delta)
				{
					// New bearish candle
					return NewCandle(series, currentCandle.OpenPrice, currentCandle.OpenPrice - delta, value);
				}
			}
			else
			{
				if ((value.Price - currentCandle.OpenPrice) > delta)
				{
					// New bullish candle
					return NewCandle(series, currentCandle.OpenPrice, currentCandle.OpenPrice + delta, value);
				}

				if ((currentCandle.ClosePrice - value.Price) > delta)
				{
					// New bearish candle
					return NewCandle(series, currentCandle.ClosePrice, currentCandle.ClosePrice - delta, value);
				}
			}

			return UpdateRenkoCandle(currentCandle, value);
		}

		private static RenkoCandle NewCandle(CandleSeries series, decimal openPrice, decimal closePrice, ICandleBuilderSourceValue value)
		{
			return new RenkoCandle
			{
				OpenPrice = openPrice,
				ClosePrice = closePrice,
				HighPrice = Math.Max(openPrice, closePrice),
				LowPrice = Math.Min(openPrice, closePrice),
				TotalPrice = openPrice * value.Volume,
				OpenVolume = value.Volume,
				CloseVolume = value.Volume,
				HighVolume = value.Volume,
				LowVolume = value.Volume,
				TotalVolume = value.Volume,
				Security = series.Security,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
				BoxSize = (Unit)series.Arg,
				RelativeVolume = value.OrderDirection == null ? 0 : (value.OrderDirection == Sides.Buy ? value.Volume : -value.Volume)
			};
		}

		private static RenkoCandle UpdateRenkoCandle(RenkoCandle candle, ICandleBuilderSourceValue value)
		{
			candle.HighPrice = Math.Max(candle.HighPrice, value.Price);
			candle.LowPrice = Math.Min(candle.LowPrice, value.Price);

			candle.HighVolume = Math.Max(candle.HighVolume ?? 0m, value.Volume);
			candle.LowVolume = Math.Min(candle.LowVolume ?? 0m, value.Volume);

			candle.CloseVolume = value.Volume;

			candle.TotalPrice += value.Price * value.Volume;
			candle.TotalVolume += value.Volume;

			if (value.OrderDirection != null)
				candle.RelativeVolume += value.OrderDirection == Sides.Buy ? value.Volume : -value.Volume;

			candle.CloseTime = value.Time;

			return candle;
		}
	}
}