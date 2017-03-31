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

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The candles builder.
	/// </summary>
	/// <typeparam name="TCandleMessage">The type of candle which the builder will create.</typeparam>
	public abstract class CandleBuilder<TCandleMessage> : BaseLogReceiver, ICandleBuilder
		where TCandleMessage : CandleMessage
	{
		private sealed class CandleInfo
		{
			public CandleMessage CurrentCandle { get; set; }

			public VolumeProfile VolumeProfile { get; set; }
		}

		private readonly SynchronizedDictionary<MarketDataMessage, CandleInfo> _info = new SynchronizedDictionary<MarketDataMessage, CandleInfo>();

		/// <summary>
		/// The candle type.
		/// </summary>
		public abstract MarketDataTypes CandleType { get; }

		/// <summary>
		/// Initialize <see cref="CandleBuilder{T}"/>.
		/// </summary>
		protected CandleBuilder()
		{
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="value">The new data by which it is decided to start or end the current candle creation.</param>
		/// <returns>A new candles changes.</returns>
		public IEnumerable<CandleMessage> Process(MarketDataMessage message, ICandleBuilderSourceValue value)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var info = _info.SafeAdd(message, k => new CandleInfo());

			if (info == null)
				yield break;

			var currCandle = info.CurrentCandle;

			var candle = ProcessValue(message, (TCandleMessage)currCandle, value);

			if (candle == null)
			{
				// skip the value that cannot be processed
				yield break;
			}

			if (candle == currCandle)
			{
				if (message.IsCalcVolumeProfile)
				{
					if (info.VolumeProfile == null)
						throw new InvalidOperationException();

					info.VolumeProfile.Update(value);
				}

				//candle.State = CandleStates.Changed;
				yield return candle;
			}
			else
			{
				if (currCandle != null)
				{
					info.CurrentCandle = null;
					info.VolumeProfile = null;

					currCandle.State = CandleStates.Finished;
					yield return currCandle;
				}

				info.CurrentCandle = candle;

				if (message.IsCalcVolumeProfile)
				{
					info.VolumeProfile = new VolumeProfile();
					info.VolumeProfile.Update(value);

					candle.PriceLevels = info.VolumeProfile.PriceLevels;
				}

				candle.State = CandleStates.Active;
				yield return candle;
			}
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected virtual TCandleMessage CreateCandle(MarketDataMessage message, TCandleMessage currentCandle, ICandleBuilderSourceValue value)
		{
			throw new NotSupportedException(LocalizedStrings.Str637);
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected virtual bool IsCandleFinishedBeforeChange(MarketDataMessage message, TCandleMessage candle, ICandleBuilderSourceValue value)
		{
			return false;
		}

		/// <summary>
		/// To fill in the initial candle settings.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data.</param>
		/// <returns>Candle.</returns>
		protected virtual TCandleMessage FirstInitCandle(MarketDataMessage message, TCandleMessage candle, ICandleBuilderSourceValue value)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			candle.SecurityId = value.SecurityId;

			candle.OpenPrice = value.Price;
			candle.ClosePrice = value.Price;
			candle.LowPrice = value.Price;
			candle.HighPrice = value.Price;
			//candle.TotalPrice = value.Price;

			candle.OpenVolume = value.Volume;
			candle.CloseVolume = value.Volume;
			candle.LowVolume = value.Volume;
			candle.HighVolume = value.Volume;
			candle.TotalVolume = value.Volume ?? 0;

			return candle;
		}

		/// <summary>
		/// To update the candle data.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data.</param>
		protected virtual void UpdateCandle(MarketDataMessage message, TCandleMessage candle, ICandleBuilderSourceValue value)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			Update(candle, value);
		}

		private static void Update(TCandleMessage candle, ICandleBuilderSourceValue value)
		{
			var price = value.Price;
			var time = value.Time;

			if (price < candle.LowPrice)
			{
				candle.LowPrice = price;
				candle.LowTime = time;
			}

			if (price > candle.HighPrice)
			{
				candle.HighPrice = price;
				candle.HighTime = time;
			}

			candle.ClosePrice = price;

			if (value.Volume != null)
			{
				var volume = value.Volume.Value;

				candle.TotalPrice += price * volume;

				candle.LowVolume = (candle.LowVolume ?? 0m).Min(volume);
				candle.HighVolume = (candle.HighVolume ?? 0m).Max(volume);
				candle.CloseVolume = volume;
				candle.TotalVolume += volume;

				var dir = value.OrderDirection;
				if (dir != null)
					candle.RelativeVolume = (candle.RelativeVolume ?? 0) + (dir.Value == Sides.Buy ? volume : -volume);
			}

			candle.CloseTime = time;
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">The new data by which it is decided to start or end the current candle creation.</param>
		/// <returns>A new candle. If there is not necessary to create a new candle, then <paramref name="currentCandle" /> is returned. If it is impossible to create a new candle (<paramref name="value" /> can not be applied to candles), then <see langword="null" /> is returned.</returns>
		protected virtual TCandleMessage ProcessValue(MarketDataMessage message, TCandleMessage currentCandle, ICandleBuilderSourceValue value)
		{
			if (currentCandle == null || IsCandleFinishedBeforeChange(message, currentCandle, value))
			{
				currentCandle = CreateCandle(message, currentCandle, value);
				this.AddDebugLog("NewCandle {0} ForValue {1}", currentCandle, value);
				return currentCandle;
			}

			UpdateCandle(message, currentCandle, value);

			// TODO performance
			//this.AddDebugLog("UpdatedCandle {0} ForValue {1}", currentCandle, value);

			return currentCandle;
		}

		///// <summary>
		///// To finish the candle forcibly.
		///// </summary>
		///// <param name="message">Market-data message.</param>
		///// <param name="candleMessage">Candle.</param>
		//protected void ForceFinishCandle(MarketDataMessage message, CandleMessage candleMessage)
		//{
		//	var info = _info.TryGetValue(message);

		//	if (info == null)
		//		return;

		//	var isNone = candleMessage.State == CandleStates.None;

		//	// если успела прийти новая свеча
		//	if (isNone && info.CurrentCandle != null)
		//		return;

		//	if (!isNone && info.CurrentCandle != candleMessage)
		//		return;

		//	info.CurrentCandle = isNone ? null : candleMessage;

		//	if (!isNone)
		//		candleMessage.State = CandleStates.Finished;

		//	RaiseProcessing(series, candleMessage);
		//}
	}

	/// <summary>
	/// The builder of candles of <see cref="TimeFrameCandleMessage"/> type.
	/// </summary>
	public class TimeFrameCandleBuilder : CandleBuilder<TimeFrameCandleMessage>
	{
		private readonly IExchangeInfoProvider _exchangeInfoProvider;
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
		//			throw new ArgumentNullException(nameof(series));

		//		if (builder == null)
		//			throw new ArgumentNullException(nameof(builder));

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
		/// The candle type.
		/// </summary>
		public override MarketDataTypes CandleType => MarketDataTypes.CandleTimeFrame;

		/// <summary>
		/// Whether to create empty candles (<see cref="CandleStates.None"/>) in the lack of trades. The default mode is enabled.
		/// </summary>
		public bool GenerateEmptyCandles { get; set; }

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
		/// Initializes a new instance of the <see cref="TimeFrameCandleBuilder"/>.
		/// </summary>
		public TimeFrameCandleBuilder()
			: this(new InMemoryExchangeInfoProvider())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TimeFrameCandleBuilder"/>.
		/// </summary>
		public TimeFrameCandleBuilder(IExchangeInfoProvider exchangeInfoProvider)
		{
			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			_exchangeInfoProvider = exchangeInfoProvider;

			GenerateEmptyCandles = true;
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override TimeFrameCandleMessage CreateCandle(MarketDataMessage message, TimeFrameCandleMessage currentCandle, ICandleBuilderSourceValue value)
		{
			var timeFrame = (TimeSpan)message.Arg;

			var board = _exchangeInfoProvider.GetOrCreateBoard(message.SecurityId.BoardCode);
			var bounds = timeFrame.GetCandleBounds(value.Time, board, board.WorkingTime);

			if (value.Time < bounds.Min)
				return null;

			var openTime = bounds.Min;

			var candle = FirstInitCandle(message, new TimeFrameCandleMessage
			{
				TimeFrame = timeFrame,
				OpenTime = openTime,
				HighTime = openTime,
				LowTime = openTime,
				CloseTime = openTime, // реальное окончание свечи определяет по последней сделке
			}, value);

			return candle;
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(MarketDataMessage message, TimeFrameCandleMessage candle, ICandleBuilderSourceValue value)
		{
			return value.Time < candle.OpenTime || (candle.OpenTime + candle.TimeFrame) <= value.Time;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="TickCandleMessage"/> type.
	/// </summary>
	public class TickCandleBuilder : CandleBuilder<TickCandleMessage>
	{
		/// <summary>
		/// The candle type.
		/// </summary>
		public override MarketDataTypes CandleType => MarketDataTypes.CandleTick;

		/// <summary>
		/// Initializes a new instance of the <see cref="TickCandleBuilder"/>.
		/// </summary>
		public TickCandleBuilder()
		{
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override TickCandleMessage CreateCandle(MarketDataMessage message, TickCandleMessage currentCandle, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(message, new TickCandleMessage
			{
				MaxTradeCount = (int)message.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(MarketDataMessage message, TickCandleMessage candle, ICandleBuilderSourceValue value)
		{
			return candle.CurrentTradeCount >= candle.MaxTradeCount;
		}

		/// <summary>
		/// To update the candle data.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data.</param>
		protected override void UpdateCandle(MarketDataMessage message, TickCandleMessage candle, ICandleBuilderSourceValue value)
		{
			base.UpdateCandle(message, candle, value);
			candle.CurrentTradeCount++;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="VolumeCandleMessage"/> type.
	/// </summary>
	public class VolumeCandleBuilder : CandleBuilder<VolumeCandleMessage>
	{
		/// <summary>
		/// The candle type.
		/// </summary>
		public override MarketDataTypes CandleType => MarketDataTypes.CandleVolume;

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeCandleBuilder"/>.
		/// </summary>
		public VolumeCandleBuilder()
		{
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override VolumeCandleMessage CreateCandle(MarketDataMessage message, VolumeCandleMessage currentCandle, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(message, new VolumeCandleMessage
			{
				Volume = (decimal)message.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(MarketDataMessage message, VolumeCandleMessage candle, ICandleBuilderSourceValue value)
		{
			return candle.TotalVolume >= candle.Volume;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="RangeCandleMessage"/> type.
	/// </summary>
	public class RangeCandleBuilder : CandleBuilder<RangeCandleMessage>
	{
		/// <summary>
		/// The candle type.
		/// </summary>
		public override MarketDataTypes CandleType => MarketDataTypes.CandleRange;

		/// <summary>
		/// Initializes a new instance of the <see cref="RangeCandleBuilder"/>.
		/// </summary>
		public RangeCandleBuilder()
		{
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override RangeCandleMessage CreateCandle(MarketDataMessage message, RangeCandleMessage currentCandle, ICandleBuilderSourceValue value)
		{
			return FirstInitCandle(message, new RangeCandleMessage
			{
				PriceRange = (Unit)message.Arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
			}, value);
		}

		/// <summary>
		/// Whether the candle is created before data adding.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(MarketDataMessage message, RangeCandleMessage candle, ICandleBuilderSourceValue value)
		{
			return (decimal)(candle.LowPrice + candle.PriceRange) <= candle.HighPrice;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="PnFCandleMessage"/> type.
	/// </summary>
	public class PnFCandleBuilder : CandleBuilder<PnFCandleMessage>
	{
		/// <summary>
		/// The candle type.
		/// </summary>
		public override MarketDataTypes CandleType => MarketDataTypes.CandlePnF;

		/// <summary>
		/// Initializes a new instance of the <see cref="PnFCandleBuilder"/>.
		/// </summary>
		public PnFCandleBuilder()
		{
		}

		/// <summary>
		/// To create a new candle.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">Data with which a new candle should be created.</param>
		/// <returns>Created candle.</returns>
		protected override PnFCandleMessage CreateCandle(MarketDataMessage message, PnFCandleMessage currentCandle, ICandleBuilderSourceValue value)
		{
			var arg = (PnFArg)message.Arg;
			var boxSize = arg.BoxSize;

			var candle = new PnFCandleMessage
			{
				PnFArg = arg,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,

				//Security = value.Security,

				OpenVolume = value.Volume,
				CloseVolume = value.Volume,
				LowVolume = value.Volume,
				HighVolume = value.Volume,
				TotalVolume = value.Volume ?? 0,

				TotalPrice = value.Price * (value.Volume ?? 1),

				PnFType = currentCandle == null ? PnFTypes.X : (currentCandle.PnFType == PnFTypes.X ? PnFTypes.O : PnFTypes.X),
			};

			if (currentCandle == null)
			{
				candle.OpenPrice = boxSize.AlignPrice(value.Price, value.Price);

				if (candle.PnFType == PnFTypes.X)
					candle.ClosePrice = (decimal)(candle.OpenPrice + boxSize);
				else
					candle.ClosePrice = (decimal)(candle.OpenPrice - boxSize);
			}
			else
			{
				candle.OpenPrice = (decimal)((currentCandle.PnFType == PnFTypes.X)
					? currentCandle.ClosePrice - boxSize
					: currentCandle.ClosePrice + boxSize);

				var price = boxSize.AlignPrice(candle.OpenPrice, value.Price);

				if (candle.PnFType == PnFTypes.X)
					candle.ClosePrice = (decimal)(price + boxSize);
				else
					candle.ClosePrice = (decimal)(price - boxSize);
			}

			if (candle.PnFType == PnFTypes.X)
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
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data by which it is decided to end the current candle creation.</param>
		/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
		protected override bool IsCandleFinishedBeforeChange(MarketDataMessage message, PnFCandleMessage candle, ICandleBuilderSourceValue value)
		{
			var argSize = candle.PnFArg.BoxSize * candle.PnFArg.ReversalAmount;

			return candle.PnFType == PnFTypes.X
				? candle.ClosePrice - argSize > value.Price
				: candle.ClosePrice + argSize < value.Price;
		}

		/// <summary>
		/// To update the candle data.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="candle">Candle.</param>
		/// <param name="value">Data.</param>
		protected override void UpdateCandle(MarketDataMessage message, PnFCandleMessage candle, ICandleBuilderSourceValue value)
		{
			candle.ClosePrice = candle.PnFArg.BoxSize.AlignPrice(candle.ClosePrice, value.Price);

			if (candle.PnFType == PnFTypes.X)
				candle.HighPrice = candle.ClosePrice;
			else
				candle.LowPrice = candle.ClosePrice;

			if (value.Volume != null)
			{
				var volume = value.Volume.Value;

				candle.TotalPrice += value.Price * volume;

				candle.LowVolume = (candle.LowVolume ?? 0m).Min(volume);
				candle.HighVolume = (candle.HighVolume ?? 0m).Max(volume);
				candle.CloseVolume = volume;
				candle.TotalVolume += volume;
			}

			candle.CloseTime = value.Time;
		}
	}

	/// <summary>
	/// The builder of candles of <see cref="RenkoCandleMessage"/> type.
	/// </summary>
	public class RenkoCandleBuilder : CandleBuilder<RenkoCandleMessage>
	{
		/// <summary>
		/// The candle type.
		/// </summary>
		public override MarketDataTypes CandleType => MarketDataTypes.CandleRenko;

		/// <summary>
		/// Initializes a new instance of the <see cref="RenkoCandleBuilder"/>.
		/// </summary>
		public RenkoCandleBuilder()
		{
		}

		/// <summary>
		/// To process the new data.
		/// </summary>
		/// <param name="message">Market-data message.</param>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="value">The new data by which it is decided to start or end the current candle creation.</param>
		/// <returns>A new candle. If there is not necessary to create a new candle, then <paramref name="currentCandle" /> is returned. If it is impossible to create a new candle (<paramref name="value" /> can not be applied to candles), then <see langword="null" /> is returned.</returns>
		protected override RenkoCandleMessage ProcessValue(MarketDataMessage message, RenkoCandleMessage currentCandle, ICandleBuilderSourceValue value)
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (currentCandle == null)
				return NewCandle(message, value.Price, value.Price, value);

			var delta = currentCandle.BoxSize.Value;

			if (currentCandle.OpenPrice < currentCandle.ClosePrice)
			{
				if ((value.Price - currentCandle.ClosePrice) > delta)
				{
					// New bullish candle
					return NewCandle(message, currentCandle.ClosePrice, currentCandle.ClosePrice + delta, value);
				}

				if ((currentCandle.OpenPrice - value.Price) > delta)
				{
					// New bearish candle
					return NewCandle(message, currentCandle.OpenPrice, currentCandle.OpenPrice - delta, value);
				}
			}
			else
			{
				if ((value.Price - currentCandle.OpenPrice) > delta)
				{
					// New bullish candle
					return NewCandle(message, currentCandle.OpenPrice, currentCandle.OpenPrice + delta, value);
				}

				if ((currentCandle.ClosePrice - value.Price) > delta)
				{
					// New bearish candle
					return NewCandle(message, currentCandle.ClosePrice, currentCandle.ClosePrice - delta, value);
				}
			}

			return UpdateRenkoCandle(currentCandle, value);
		}

		private static RenkoCandleMessage NewCandle(MarketDataMessage message, decimal openPrice, decimal closePrice, ICandleBuilderSourceValue value)
		{
			return new RenkoCandleMessage
			{
				OpenPrice = openPrice,
				ClosePrice = closePrice,
				HighPrice = Math.Max(openPrice, closePrice),
				LowPrice = Math.Min(openPrice, closePrice),
				TotalPrice = openPrice * (value.Volume ?? 1),
				OpenVolume = value.Volume,
				CloseVolume = value.Volume,
				HighVolume = value.Volume,
				LowVolume = value.Volume,
				TotalVolume = value.Volume ?? 1,
				SecurityId = message.SecurityId,
				OpenTime = value.Time,
				CloseTime = value.Time,
				HighTime = value.Time,
				LowTime = value.Time,
				BoxSize = (Unit)message.Arg,
				RelativeVolume = value.OrderDirection == null ? 0 : (value.OrderDirection == Sides.Buy ? value.Volume : -value.Volume)
			};
		}

		private static RenkoCandleMessage UpdateRenkoCandle(RenkoCandleMessage candle, ICandleBuilderSourceValue value)
		{
			candle.HighPrice = Math.Max(candle.HighPrice, value.Price);
			candle.LowPrice = Math.Min(candle.LowPrice, value.Price);

			if (value.Volume != null)
			{
				var volume = value.Volume.Value;

				candle.HighVolume = Math.Max(candle.HighVolume ?? 0m, volume);
				candle.LowVolume = Math.Min(candle.LowVolume ?? 0m, volume);

				candle.CloseVolume = volume;

				candle.TotalPrice += value.Price * volume;
				candle.TotalVolume += volume;
			}

			if (value.OrderDirection != null)
				candle.RelativeVolume += value.OrderDirection == Sides.Buy ? value.Volume : -value.Volume;

			candle.CloseTime = value.Time;

			return candle;
		}
	}
}