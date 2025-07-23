namespace StockSharp.Algo.Candles.Compression;

/// <summary>
/// Candles builder.
/// </summary>
/// <typeparam name="TCandleMessage">The type of candle which the builder will create.</typeparam>
/// <remarks>
/// Initialize <see cref="CandleBuilder{TCandleMessage}"/>.
/// </remarks>
/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
public abstract class CandleBuilder<TCandleMessage>(IExchangeInfoProvider exchangeInfoProvider) : BaseLogReceiver, ICandleBuilder
	where TCandleMessage : CandleMessage
{
	/// <inheritdoc />
	public virtual Type CandleType { get; } = typeof(TCandleMessage);

	/// <summary>
	/// The exchange boards provider.
	/// </summary>
	protected IExchangeInfoProvider ExchangeInfoProvider { get; } = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

	/// <inheritdoc />
	public IEnumerable<CandleMessage> Process(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		if (IsTimeValid(subscription.Message, transform.Time))
		{
			foreach (var candle in OnProcess(subscription, transform))
			{
				subscription.CurrentCandle = candle;
				yield return candle;
			}
		}
	}

	/// <summary>
	/// Get time zone info.
	/// </summary>
	/// <param name="subscription"><see cref="ICandleBuilderSubscription"/></param>
	/// <returns>Info.</returns>
	protected (TimeZoneInfo zone, WorkingTime time) GetTimeZone(ICandleBuilderSubscription subscription)
	{
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		var board = subscription.Message.IsRegularTradingHours == true ? ExchangeInfoProvider.GetOrCreateBoard(subscription.Message.SecurityId.BoardCode) : ExchangeBoard.Associated;

		return (board.TimeZone, board.WorkingTime);
	}

	private bool IsTimeValid(MarketDataMessage message, DateTimeOffset time)
	{
		if (message.IsRegularTradingHours != true)
			return true;

		var board = ExchangeInfoProvider.TryGetExchangeBoard(message.SecurityId.BoardCode);

		if (board == null)
			return true;

		return board.IsTradeTime(time, out _, out _);
	}

	/// <summary>
	/// Add volume to <see cref="CandleMessage.TotalVolume"/>, <see cref="CandleMessage.BuyVolume"/>, <see cref="CandleMessage.SellVolume"/>, <see cref="CandleMessage.RelativeVolume"/>.
	/// </summary>
	/// <param name="candle"><see cref="CandleMessage"/></param>
	/// <param name="volume">The volume to be added to the candle.</param>
	/// <param name="volSide">The side of the volume to be added to the candle.</param>
	protected static void AddVolume(TCandleMessage candle, decimal volume, Sides? volSide)
	{
		candle.TotalVolume += volume;

		switch (volSide)
		{
			case Sides.Buy:
				candle.BuyVolume       = (candle.BuyVolume ?? 0)      + volume;
				candle.RelativeVolume  = (candle.RelativeVolume ?? 0) + volume;
				break;
			case Sides.Sell:
				candle.SellVolume      = (candle.SellVolume ?? 0)     + volume;
				candle.RelativeVolume  = (candle.RelativeVolume ?? 0) - volume;
				break;
		}
	}

	/// <summary>
	/// To process the new data.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="transform">The data source transformation.</param>
	/// <returns>A new candles changes.</returns>
	protected virtual IEnumerable<TCandleMessage> OnProcess(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		if (transform == null)
			throw new ArgumentNullException(nameof(transform));

		var currentCandle = (TCandleMessage)subscription.CurrentCandle;
		var volumeProfile = subscription.VolumeProfile;

		var candle = ProcessValue(subscription, transform);

		if (candle == null)
		{
			// skip the value that cannot be processed
			yield break;
		}

		if (candle == currentCandle)
		{
			if (subscription.Message.IsCalcVolumeProfile)
			{
				if (volumeProfile == null)
					throw new InvalidOperationException();

				volumeProfile.Update(transform);
			}

			//candle.State = CandleStates.Changed;
			yield return candle;
		}
		else
		{
			if (currentCandle != null)
			{
				currentCandle.State = CandleStates.Finished;
				yield return currentCandle;
			}

			if (subscription.Message.IsCalcVolumeProfile)
			{
				subscription.VolumeProfile = volumeProfile = new();
				volumeProfile.Update(transform);

				candle.PriceLevels = subscription.VolumeProfile.PriceLevels;
			}

			candle.State = CandleStates.Active;
			yield return candle;
		}
	}

	/// <summary>
	/// To create a new candle.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="transform">The data source transformation.</param>
	/// <returns>Created candle.</returns>
	protected virtual TCandleMessage CreateCandle(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
		=> throw new NotSupportedException(LocalizedStrings.MethodMustBeOverrided);

	/// <summary>
	/// Whether the candle is created before data adding.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="candle">Candle.</param>
	/// <param name="transform">The data source transformation.</param>
	/// <returns><see langword="true" /> if the candle should be finished. Otherwise, <see langword="false" />.</returns>
	protected virtual bool IsCandleFinishedBeforeChange(ICandleBuilderSubscription subscription, TCandleMessage candle, ICandleBuilderValueTransform transform)
		=> false;

	/// <summary>
	/// To fill in the initial candle settings.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="candle">Candle.</param>
	/// <param name="transform">The data source transformation.</param>
	/// <returns>Candle.</returns>
	protected virtual TCandleMessage FirstInitCandle(ICandleBuilderSubscription subscription, TCandleMessage candle, ICandleBuilderValueTransform transform)
	{
		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		if (transform == null)
			throw new ArgumentNullException(nameof(transform));

		var price = transform.Price;
		var volume = transform.Volume;

		candle.BuildFrom = transform.BuildFrom;
		candle.SecurityId = subscription.Message.SecurityId;

		candle.OpenPrice = price;
		candle.ClosePrice = price;
		candle.LowPrice = price;
		candle.HighPrice = price;

		candle.OpenVolume = volume;
		candle.CloseVolume = volume;
		candle.LowVolume = volume;
		candle.HighVolume = volume;

		if (volume is decimal v)
		{
			candle.TotalPrice = price * v;
			AddVolume(candle, v, transform.Side);
		}

		candle.OpenInterest = transform.OpenInterest;
		candle.LocalTime = transform.Time;

		candle.TotalTicks = 1;

		return candle;
	}

	/// <summary>
	/// To update the candle data.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="candle">Candle.</param>
	/// <param name="transform">The data source transformation.</param>
	protected virtual void UpdateCandle(ICandleBuilderSubscription subscription, TCandleMessage candle, ICandleBuilderValueTransform transform)
	{
		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		if (transform == null)
			throw new ArgumentNullException(nameof(transform));

		var price = transform.Price;
		var time = transform.Time;
		var volume = transform.Volume;

		if (price < candle.LowPrice)
		{
			candle.LowPrice = price;
			candle.LowTime = time;
			candle.LowVolume = volume;
		}

		if (price > candle.HighPrice)
		{
			candle.HighPrice = price;
			candle.HighTime = time;
			candle.HighVolume = volume;
		}

		candle.ClosePrice = price;

		if (volume is decimal v)
		{
			candle.CloseVolume = v;

			candle.TotalPrice += price * v;

			AddVolume(candle, v, transform.Side);
		}

		candle.CloseTime = time;

		candle.OpenInterest = transform.OpenInterest;
		candle.LocalTime = transform.Time;

		IncrementTicks(candle);
	}

	/// <summary>
	/// Increment the number of ticks in the candle.
	/// </summary>
	/// <param name="candle"><see cref="CandleMessage"/></param>
	protected void IncrementTicks(CandleMessage candle)
	{
		if (candle is null)
			throw new ArgumentNullException(nameof(candle));

		if (candle.TotalTicks != null)
			candle.TotalTicks++;
		else
			candle.TotalTicks = 1;
	}

	/// <summary>
	/// To process the new data.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	/// <param name="transform">The data source transformation.</param>
	/// <returns>A new candle. If there is not necessary to create a new candle, then <see cref="ICandleBuilderSubscription.CurrentCandle" /> is returned. If it is impossible to create a new candle (<paramref name="transform" /> cannot be applied to candles), then <see langword="null" /> is returned.</returns>
	protected virtual TCandleMessage ProcessValue(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		var currentCandle = (TCandleMessage)subscription.CurrentCandle;

		if (currentCandle == null || IsCandleFinishedBeforeChange(subscription, currentCandle, transform))
		{
			currentCandle = CreateCandle(subscription, transform);
			LogDebug("NewCandle {0} ForValue {1}", currentCandle, transform);
			return currentCandle;
		}

		UpdateCandle(subscription, currentCandle, transform);

		// TODO performance
		//LogDebug("UpdatedCandle {0} ForValue {1}", currentCandle, value);

		return currentCandle;
	}

	///// <summary>
	///// To finish the candle forcibly.
	///// </summary>
	///// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
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

	/// <summary>
	/// To cut the price, to make it multiple of minimal step, also to limit number of signs after the comma.
	/// </summary>
	/// <param name="price">The price to be made multiple.</param>
	/// <param name="subscription"><see cref="ICandleBuilderSubscription"/></param>
	/// <returns>The multiple price.</returns>
	protected decimal ShrinkPrice(Unit price, ICandleBuilderSubscription subscription)
		=> ShrinkPrice((decimal)price, subscription);

	/// <summary>
	/// To cut the price, to make it multiple of minimal step, also to limit number of signs after the comma.
	/// </summary>
	/// <param name="price">The price to be made multiple.</param>
	/// <param name="subscription"><see cref="ICandleBuilderSubscription"/></param>
	/// <returns>The multiple price.</returns>
	protected decimal ShrinkPrice(decimal price, ICandleBuilderSubscription subscription)
		=> price.ShrinkPrice(subscription.CheckOnNull(nameof(subscription)).Message);

	/// <summary>
	/// Round the price to the specified step.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="step">Step.</param>
	/// <returns>Rounded value.</returns>
	protected decimal Round(decimal price, Unit step)
	{
		if (step is null)
			throw new ArgumentNullException(nameof(step));

		static decimal roundToAbs(decimal price, decimal step)
			=> Math.Round(price / step) * step;

		return step.Type switch
		{
			UnitTypes.Percent => roundToAbs(price, (decimal)((price + step) - price)),
			_ => roundToAbs(price, (decimal)step),
		};
	}
}

/// <summary>
/// The builder of candles of <see cref="TimeFrameCandleMessage"/> type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TimeFrameCandleBuilder"/>.
/// </remarks>
/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
public class TimeFrameCandleBuilder(IExchangeInfoProvider exchangeInfoProvider) : CandleBuilder<TimeFrameCandleMessage>(exchangeInfoProvider)
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
	/// Whether to create empty candles (<see cref="CandleStates.None"/>) in the lack of trades. The default mode is enabled.
	/// </summary>
	public bool GenerateEmptyCandles { get; set; } = true;

	private Unit _timeout = UnitHelper.Percents(10);

	/// <summary>
	/// The time shift from the time frame end after which a signal is sent to close the unclosed candle forcibly. The default is 10% of the time frame.
	/// </summary>
	public Unit Timeout
	{
		get => _timeout;
		set
		{
			if (value == null)
				throw new ArgumentNullException(nameof(value));

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.OffsetValueIncorrect);

			_timeout = value;
		}
	}

	///// <summary>
	///// Reset state.
	///// </summary>
	//public override void Reset()
	//{
	//	base.Reset();

	//	_timeoutInfos.Clear();
	//}

	/// <inheritdoc />
	protected override TimeFrameCandleMessage CreateCandle(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		var timeFrame = subscription.Message.GetTimeFrame();

		var (zone, time) = GetTimeZone(subscription);
		var bounds = timeFrame.GetCandleBounds(transform.Time, zone, time);

		if (transform.Time < bounds.Min)
			return null;

		var openTime = bounds.Min;

		var candle = FirstInitCandle(subscription, new()
		{
			TypedArg = timeFrame,
			OpenTime = openTime,
			HighTime = openTime,
			LowTime = openTime,
			CloseTime = openTime, // реальное окончание свечи определяет по последней сделке
		}, transform);

		return candle;
	}

	/// <inheritdoc />
	protected override bool IsCandleFinishedBeforeChange(ICandleBuilderSubscription subscription, TimeFrameCandleMessage candle, ICandleBuilderValueTransform transform)
	{
		return transform.Time < candle.OpenTime || (candle.OpenTime + candle.TypedArg) <= transform.Time;
	}
}

/// <summary>
/// The builder of candles of <see cref="TickCandleMessage"/> type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TickCandleBuilder"/>.
/// </remarks>
/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
public class TickCandleBuilder(IExchangeInfoProvider exchangeInfoProvider) : CandleBuilder<TickCandleMessage>(exchangeInfoProvider)
{
	/// <inheritdoc />
	protected override TickCandleMessage CreateCandle(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		var time = transform.Time;

		return FirstInitCandle(subscription, new()
		{
			TypedArg = subscription.Message.GetArg<int>(),
			OpenTime = time,
			CloseTime = time,
			HighTime = time,
			LowTime = time,
		}, transform);
	}

	/// <inheritdoc />
	protected override bool IsCandleFinishedBeforeChange(ICandleBuilderSubscription subscription, TickCandleMessage candle, ICandleBuilderValueTransform transform)
	{
		return candle.TotalTicks != null && candle.TotalTicks.Value >= candle.TypedArg;
	}
}

/// <summary>
/// The builder of candles of <see cref="VolumeCandleMessage"/> type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="VolumeCandleBuilder"/>.
/// </remarks>
/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
public class VolumeCandleBuilder(IExchangeInfoProvider exchangeInfoProvider) : CandleBuilder<VolumeCandleMessage>(exchangeInfoProvider)
{
	/// <inheritdoc />
	protected override VolumeCandleMessage CreateCandle(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		var time = transform.Time;

		return FirstInitCandle(subscription, new()
		{
			TypedArg = subscription.Message.GetArg<decimal>(),
			OpenTime = time,
			CloseTime = time,
			HighTime = time,
			LowTime = time,
		}, transform);
	}

	/// <inheritdoc />
	protected override bool IsCandleFinishedBeforeChange(ICandleBuilderSubscription subscription, VolumeCandleMessage candle, ICandleBuilderValueTransform transform)
	{
		return candle.TotalVolume >= candle.TypedArg;
	}
}

/// <summary>
/// The builder of candles of <see cref="RangeCandleMessage"/> type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RangeCandleBuilder"/>.
/// </remarks>
/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
public class RangeCandleBuilder(IExchangeInfoProvider exchangeInfoProvider) : CandleBuilder<RangeCandleMessage>(exchangeInfoProvider)
{
	/// <inheritdoc />
	protected override RangeCandleMessage CreateCandle(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		var time = transform.Time;

		return FirstInitCandle(subscription, new RangeCandleMessage
		{
			TypedArg = subscription.Message.GetArg<Unit>(),
			OpenTime = time,
			CloseTime = time,
			HighTime = time,
			LowTime = time,
		}, transform);
	}

	/// <inheritdoc />
	protected override bool IsCandleFinishedBeforeChange(ICandleBuilderSubscription subscription, RangeCandleMessage candle, ICandleBuilderValueTransform transform)
	{
		return (decimal)(candle.LowPrice + candle.TypedArg) <= candle.HighPrice;
	}
}

/// <summary>
/// The builder of candles of <see cref="PnFCandleMessage"/> type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PnFCandleBuilder"/>.
/// </remarks>
/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
public class PnFCandleBuilder(IExchangeInfoProvider exchangeInfoProvider) : CandleBuilder<PnFCandleMessage>(exchangeInfoProvider)
{
	/// <inheritdoc />
	protected override IEnumerable<PnFCandleMessage> OnProcess(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		var currentPnFCandle = (PnFCandleMessage)subscription.CurrentCandle;

		var pnf = subscription.Message.GetArg<PnFArg>();

		var boxSize = pnf.BoxSize;

		var price = ShrinkPrice(Round(transform.Price, boxSize), subscription);

		var volume = transform.Volume;
		var time = transform.Time;
		var side = transform.Side;
		var oi = transform.OpenInterest;
		var buildFrom = transform.BuildFrom;

		if (currentPnFCandle == null)
		{
			var openPrice = price;
			var highPrice = ShrinkPrice(openPrice + pnf.BoxSize, subscription);

			currentPnFCandle = CreateCandle(subscription, buildFrom, pnf, openPrice, highPrice, openPrice, highPrice, price, volume, side, time, oi);
			yield return currentPnFCandle;
		}
		else
		{
			var isX = currentPnFCandle.OpenPrice <= currentPnFCandle.ClosePrice;

			if (isX)
			{
				if (price > currentPnFCandle.HighPrice)
				{
					currentPnFCandle.HighPrice = currentPnFCandle.ClosePrice = price;
					UpdateCandle(currentPnFCandle, price, volume, time, side, oi, subscription.VolumeProfile);
					yield return currentPnFCandle;
				}
				else if (price <= (currentPnFCandle.HighPrice - pnf.BoxSize * pnf.ReversalAmount))
				{
					currentPnFCandle.State = CandleStates.Finished;
					yield return currentPnFCandle;

					var highPrice = currentPnFCandle.HighPrice;
					var lowPrice = price;

					currentPnFCandle = CreateCandle(subscription, buildFrom, pnf, highPrice, highPrice, lowPrice, lowPrice, price, volume, side, time, oi);
					yield return currentPnFCandle;
				}
				else
				{
					UpdateCandle(currentPnFCandle, price, volume, time, side, oi, subscription.VolumeProfile);
					yield return currentPnFCandle;
				}
			}
			else
			{
				if (price < currentPnFCandle.LowPrice)
				{
					currentPnFCandle.LowPrice = currentPnFCandle.ClosePrice = price;
					UpdateCandle(currentPnFCandle, price, volume, time, side, oi, subscription.VolumeProfile);
					yield return currentPnFCandle;
				}
				else if (price >= (currentPnFCandle.LowPrice + pnf.BoxSize * pnf.ReversalAmount))
				{
					currentPnFCandle.State = CandleStates.Finished;
					yield return currentPnFCandle;

					var highPrice = price;
					var lowPrice = currentPnFCandle.LowPrice;

					currentPnFCandle = CreateCandle(subscription, buildFrom, pnf, lowPrice, highPrice, lowPrice, highPrice, price, volume, side, time, oi);
					yield return currentPnFCandle;
				}
				else
				{
					UpdateCandle(currentPnFCandle, price, volume, time, side, oi, subscription.VolumeProfile);
					yield return currentPnFCandle;
				}
			}
		}
	}

	private void UpdateCandle(PnFCandleMessage currentPnFCandle, decimal price, decimal? volume, DateTimeOffset time, Sides? side, decimal? oi, VolumeProfileBuilder volumeProfile)
	{
		IncrementTicks(currentPnFCandle);

		if (volume is decimal v)
		{
			currentPnFCandle.TotalPrice += v * price;

			AddVolume(currentPnFCandle, v, side);
		}

		currentPnFCandle.CloseVolume = volume;
		currentPnFCandle.CloseTime = time;

		volumeProfile?.Update(price, volume, side);

		currentPnFCandle.OpenInterest = oi;
	}

	private PnFCandleMessage CreateCandle(ICandleBuilderSubscription subscription, DataType buildFrom, PnFArg pnfArg, decimal openPrice, decimal highPrice, decimal lowPrice, decimal closePrice, decimal price, decimal? volume, Sides? side, DateTimeOffset time, decimal? oi)
	{
		var candle = new PnFCandleMessage
		{
			SecurityId = subscription.Message.SecurityId,
			TypedArg = pnfArg,
			BuildFrom = buildFrom,

			OpenPrice = openPrice,
			ClosePrice = closePrice,
			HighPrice = highPrice,
			LowPrice = lowPrice,
			OpenVolume = volume,
			//CloseVolume = volume,
			HighVolume = volume,
			LowVolume = volume,
			OpenTime = time,
			//CloseTime = time,
			HighTime = time,
			LowTime = time,
			State = CandleStates.Active,
		};

		if (subscription.Message.IsCalcVolumeProfile)
		{
			subscription.VolumeProfile = new();
			candle.PriceLevels = subscription.VolumeProfile.PriceLevels;
		}

		UpdateCandle(candle, price, volume, time, side, oi, subscription.VolumeProfile);

		return candle;
	}
}

/// <summary>
/// The builder of candles of <see cref="RenkoCandleMessage"/> type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RenkoCandleBuilder"/>.
/// </remarks>
/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
public class RenkoCandleBuilder(IExchangeInfoProvider exchangeInfoProvider) : CandleBuilder<RenkoCandleMessage>(exchangeInfoProvider)
{
	/// <inheritdoc />
	protected override IEnumerable<RenkoCandleMessage> OnProcess(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		var price = transform.Price;
		var volume = transform.Volume;
		var time = transform.Time;
		var side = transform.Side;
		var oi = transform.OpenInterest;
		var buildFrom = transform.BuildFrom;
		var boxSize = subscription.Message.GetArg<Unit>();

		RenkoCandleMessage GenerateNewCandle(decimal openPrice, decimal closePrice)
		{
			var candle = new RenkoCandleMessage
			{
				SecurityId = subscription.Message.SecurityId,
				TypedArg = boxSize,
				BuildFrom = buildFrom,
				OpenTime = time,
				CloseTime = time,
				OpenPrice = openPrice,
				ClosePrice = closePrice,
				HighPrice = openPrice.Max(closePrice),
				LowPrice = openPrice.Min(closePrice),
				State = CandleStates.Active
			};

			if (subscription.Message.IsCalcVolumeProfile)
			{
				subscription.VolumeProfile = new();
				candle.PriceLevels = subscription.VolumeProfile.PriceLevels;
			}

			return candle;
		}

		var currentCandle = (RenkoCandleMessage)subscription.CurrentCandle;

		currentCandle ??= GenerateNewCandle(price, price);

		var priceChange = price - currentCandle.OpenPrice;
		var boxSizeAbs = (decimal)((price + boxSize) - price);
		var boxesMoved = Math.Abs((int)(priceChange / boxSizeAbs));

		if (boxesMoved >= 1)
		{
			var sign = priceChange.Sign();

			for (var i = 0; i < boxesMoved; i++)
			{
				currentCandle.State = CandleStates.Finished;
				subscription.VolumeProfile?.Update(price, volume, side);
				yield return currentCandle;

				var openPrice = ShrinkPrice(currentCandle.OpenPrice + (boxSize * sign), subscription);
				currentCandle = GenerateNewCandle(openPrice, openPrice);
			}
		}

		IncrementTicks(currentCandle);

		currentCandle.ClosePrice = price;
		currentCandle.CloseVolume = volume;
		currentCandle.CloseTime = time;
		currentCandle.OpenInterest = oi;
		currentCandle.HighPrice = currentCandle.HighPrice.Max(price);
		currentCandle.LowPrice = currentCandle.LowPrice.Min(price);

		if (volume is decimal v)
		{
			currentCandle.TotalPrice += v * price;
			AddVolume(currentCandle, v, side);
		}

		subscription.VolumeProfile?.Update(price, volume, side);
		yield return currentCandle;
	}
}

/// <summary>
/// The builder of candles of <see cref="HeikinAshiCandleBuilder"/> type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="HeikinAshiCandleBuilder"/>.
/// </remarks>
/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
public class HeikinAshiCandleBuilder(IExchangeInfoProvider exchangeInfoProvider) : CandleBuilder<HeikinAshiCandleMessage>(exchangeInfoProvider)
{
	/// <inheritdoc />
	protected override HeikinAshiCandleMessage CreateCandle(ICandleBuilderSubscription subscription, ICandleBuilderValueTransform transform)
	{
		var timeFrame = subscription.Message.GetTimeFrame();

		var (zone, time) = GetTimeZone(subscription);
		var bounds = timeFrame.GetCandleBounds(transform.Time, zone, time);

		if (transform.Time < bounds.Min)
			return null;

		var openTime = bounds.Min;

		var candle = FirstInitCandle(subscription, new()
		{
			TypedArg = timeFrame,
			OpenTime = openTime,
			HighTime = openTime,
			LowTime = openTime,
			CloseTime = openTime,
		}, transform);

		var currentCandle = subscription.CurrentCandle;
		if (currentCandle != null)
			candle.OpenPrice = (currentCandle.OpenPrice + currentCandle.ClosePrice) / 2M;

		return candle;
	}

	/// <inheritdoc />
	protected override bool IsCandleFinishedBeforeChange(ICandleBuilderSubscription subscription, HeikinAshiCandleMessage candle, ICandleBuilderValueTransform transform)
	{
		return transform.Time < candle.OpenTime || (candle.OpenTime + candle.TypedArg) <= transform.Time;
	}

	/// <inheritdoc />
	protected override void UpdateCandle(ICandleBuilderSubscription subscription, HeikinAshiCandleMessage candle, ICandleBuilderValueTransform transform)
	{
		base.UpdateCandle(subscription, candle, transform);

		candle.ClosePrice = (candle.OpenPrice + candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 4M;
	}
}
