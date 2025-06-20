namespace StockSharp.Algo.Candles;

using Ecng.Configuration;

using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Extension class for candles.
/// </summary>
public static partial class CandleHelper
{
	/// <summary>
	/// Try get suitable market-data type for candles compression.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="subscription">Subscription.</param>
	/// <param name="provider">Candle builders provider.</param>
	/// <returns>Which market-data type is used as a source value. <see langword="null"/> is compression is impossible.</returns>
	public static DataType TryGetCandlesBuildFrom(this IMessageAdapter adapter, MarketDataMessage subscription, CandleBuilderProvider provider)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		if (subscription == null)
			throw new ArgumentNullException(nameof(subscription));

		if (provider == null)
			throw new ArgumentNullException(nameof(provider));

		if (!provider.IsRegistered(subscription.DataType2.MessageType))
			return null;

		if (subscription.BuildMode == MarketDataBuildModes.Load)
			return null;

		var buildFrom = subscription.BuildFrom;

		if (buildFrom is not null && !DataType.CandleSources.Contains(buildFrom))
			buildFrom = null;

		buildFrom ??= adapter.GetSupportedMarketDataTypes(subscription.SecurityId, subscription.From, subscription.To).Intersect(DataType.CandleSources).OrderBy(t =>
		{
			// by priority
			if (t == DataType.Ticks)
				return 0;
			else if (t == DataType.Level1)
				return 1;
			else if (t == DataType.OrderLog)
				return 2;
			else if (t == DataType.MarketDepth)
				return 3;
			else
				return 4;
		}).FirstOrDefault();

		if (buildFrom == null || !adapter.GetSupportedMarketDataTypes(subscription.SecurityId, subscription.From, subscription.To).Contains(buildFrom))
			return null;

		return buildFrom;
	}

	private static IEnumerable<CandleMessage> ToCandles<TSourceMessage>(this IEnumerable<TSourceMessage> messages, MarketDataMessage mdMsg, Func<TSourceMessage, ICandleBuilderValueTransform> createTransform, CandleBuilderProvider candleBuilderProvider = null)
		where TSourceMessage : Message
	{
		if (createTransform is null)
			throw new ArgumentNullException(nameof(createTransform));

		CandleMessage lastActiveCandle = null;

		using var builder = candleBuilderProvider.CreateBuilder(mdMsg.DataType2.MessageType);

		var subscription = new CandleBuilderSubscription(mdMsg);
		var isFinishedOnly = mdMsg.IsFinishedOnly;

		ICandleBuilderValueTransform transform = null;

		foreach (var message in messages)
		{
			transform ??= createTransform(message);

			if (!transform.Process(message))
				continue;

			foreach (var candle in builder.Process(subscription, transform))
			{
				if (candle.State == CandleStates.Finished)
				{
					lastActiveCandle = null;
					yield return candle;
				}
				else
				{
					if (!isFinishedOnly)
						lastActiveCandle = candle;
				}
			}
		}

		if (lastActiveCandle != null)
			yield return lastActiveCandle;
	}

	private static ICandleBuilder CreateBuilder(this CandleBuilderProvider candleBuilderProvider, Type messageType)
	{
		if (messageType is null)
			throw new ArgumentNullException(nameof(messageType));

		candleBuilderProvider ??= ConfigManager.TryGetService<CandleBuilderProvider>() ?? new CandleBuilderProvider(ServicesRegistry.EnsureGetExchangeInfoProvider());

		return candleBuilderProvider.Get(messageType);
	}

	/// <summary>
	/// To create candles from the tick trades collection.
	/// </summary>
	/// <param name="executions">Tick data.</param>
	/// <param name="subscription">Market data subscription.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <returns>Candles.</returns>
	public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<ExecutionMessage> executions, Subscription subscription, CandleBuilderProvider candleBuilderProvider = null)
	{
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		return ToCandles(executions, subscription.MarketData, candleBuilderProvider);
	}

	/// <summary>
	/// To create candles from the tick trades collection.
	/// </summary>
	/// <param name="executions">Tick data.</param>
	/// <param name="mdMsg">Market data subscription.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <returns>Candles.</returns>
	public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<ExecutionMessage> executions, MarketDataMessage mdMsg, CandleBuilderProvider candleBuilderProvider = null)
	{
		return executions.ToCandles(mdMsg, execMsg =>
		{
			if (execMsg.DataType == DataType.Ticks)
				return new TickCandleBuilderValueTransform();
			else if (execMsg.DataType == DataType.OrderLog)
				return new OrderLogCandleBuilderValueTransform();
			else
				throw new ArgumentOutOfRangeException(nameof(execMsg), execMsg.DataType, LocalizedStrings.InvalidValue);
		}, candleBuilderProvider);
	}

	/// <summary>
	/// To create candles from the order books collection.
	/// </summary>
	/// <param name="depths">Market depths.</param>
	/// <param name="subscription">Market data subscription.</param>
	/// <param name="type">Type of candle depth based data.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <returns>Candles.</returns>
	public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<QuoteChangeMessage> depths, Subscription subscription, Level1Fields type = Level1Fields.SpreadMiddle, CandleBuilderProvider candleBuilderProvider = null)
	{
		if (subscription is null)
			throw new ArgumentNullException(nameof(subscription));

		return ToCandles(depths, subscription.MarketData, type, candleBuilderProvider);
	}

	/// <summary>
	/// To create candles from the order books collection.
	/// </summary>
	/// <param name="depths">Market depths.</param>
	/// <param name="mdMsg">Market data subscription.</param>
	/// <param name="type">Type of candle depth based data.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <returns>Candles.</returns>
	public static IEnumerable<CandleMessage> ToCandles(this IEnumerable<QuoteChangeMessage> depths, MarketDataMessage mdMsg, Level1Fields type = Level1Fields.SpreadMiddle, CandleBuilderProvider candleBuilderProvider = null)
	{
		return depths.ToCandles(mdMsg, quoteMsg => new QuoteCandleBuilderValueTransform(mdMsg.PriceStep, mdMsg.VolumeStep) { Type = type }, candleBuilderProvider);
	}

	/// <summary>
	/// To create tick trades from candles.
	/// </summary>
	/// <param name="candles">Candles.</param>
	/// <param name="volumeStep">Volume step.</param>
	/// <returns>Tick trades.</returns>
	public static IEnumerable<ExecutionMessage> ToTrades<TCandle>(this IEnumerable<TCandle> candles, decimal volumeStep)
		where TCandle : ICandleMessage
	{
		return new TradeEnumerable<TCandle>(candles, volumeStep);
	}

	/// <summary>
	/// To get candle time frames relatively to the exchange working hours.
	/// </summary>
	/// <param name="timeFrame">The time frame for which you need to get time range.</param>
	/// <param name="currentTime">The current time within the range of time frames.</param>
	/// <param name="board">The information about the board from which <see cref="ExchangeBoard.WorkingTime"/> working hours will be taken.</param>
	/// <returns>The candle time frames.</returns>
	public static Range<DateTimeOffset> GetCandleBounds(this TimeSpan timeFrame, DateTimeOffset currentTime, ExchangeBoard board)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		return timeFrame.GetCandleBounds(currentTime, board.TimeZone, board.WorkingTime);
	}

	/// <summary>
	/// To get the number of time frames within the specified time range.
	/// </summary>
	/// <param name="range">The specified time range for which you need to get the number of time frames.</param>
	/// <param name="timeFrame">The time frame size.</param>
	/// <param name="board"><see cref="ExchangeBoard"/>.</param>
	/// <returns>The received number of time frames.</returns>
	public static long GetTimeFrameCount(this Range<DateTimeOffset> range, TimeSpan timeFrame, ExchangeBoard board)
	{
		if (board is null)
			throw new ArgumentNullException(nameof(board));

		return range.GetTimeFrameCount(timeFrame, board.WorkingTime, board.TimeZone);
	}

	/// <summary>
	/// The total volume of bids in the <see cref="VolumeProfileBuilder"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The total volume of bids.</returns>
	public static decimal TotalBuyVolume(this VolumeProfileBuilder volumeProfile)
	{
		if (volumeProfile == null)
			throw new ArgumentNullException(nameof(volumeProfile));

		return volumeProfile.PriceLevels.Select(p => p.BuyVolume).Sum();
	}

	/// <summary>
	/// The total volume of asks in the <see cref="VolumeProfileBuilder"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The total volume of asks.</returns>
	public static decimal TotalSellVolume(this VolumeProfileBuilder volumeProfile)
	{
		if (volumeProfile == null)
			throw new ArgumentNullException(nameof(volumeProfile));

		return volumeProfile.PriceLevels.Select(p => p.SellVolume).Sum();
	}

	/// <summary>
	/// The total number of bids in the <see cref="VolumeProfileBuilder"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The total number of bids.</returns>
	public static decimal TotalBuyCount(this VolumeProfileBuilder volumeProfile)
	{
		if (volumeProfile == null)
			throw new ArgumentNullException(nameof(volumeProfile));

		return volumeProfile.PriceLevels.Select(p => p.BuyCount).Sum();
	}

	/// <summary>
	/// The total number of asks in the <see cref="VolumeProfileBuilder"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The total number of asks.</returns>
	public static decimal TotalSellCount(this VolumeProfileBuilder volumeProfile)
	{
		if (volumeProfile == null)
			throw new ArgumentNullException(nameof(volumeProfile));

		return volumeProfile.PriceLevels.Select(p => p.SellCount).Sum();
	}

	/// <summary>
	/// POC (Point Of Control) returns <see cref="CandlePriceLevel"/> which had the maximum volume.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The <see cref="CandlePriceLevel"/> which had the maximum volume.</returns>
	public static CandlePriceLevel PoC(this VolumeProfileBuilder volumeProfile)
	{
		if (volumeProfile == null)
			throw new ArgumentNullException(nameof(volumeProfile));

		var max = volumeProfile.PriceLevels.Select(p => p.BuyVolume + p.SellVolume).Max();
		return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume + p.SellVolume == max);
	}

	/// <summary>
	/// The total volume of bids which was above <see cref="PoC"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The total volume of bids.</returns>
	public static decimal BuyVolAbovePoC(this VolumeProfileBuilder volumeProfile)
	{
		var poc = volumeProfile.PoC();
		return volumeProfile.PriceLevels.Where(p => p.Price > poc.Price).Select(p => p.BuyVolume).Sum();
	}

	/// <summary>
	/// The total volume of bids which was below <see cref="PoC"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The total volume of bids.</returns>
	public static decimal BuyVolBelowPoC(this VolumeProfileBuilder volumeProfile)
	{
		var poc = volumeProfile.PoC();
		return volumeProfile.PriceLevels.Where(p => p.Price < poc.Price).Select(p => p.BuyVolume).Sum();
	}

	/// <summary>
	/// The total volume of asks which was above <see cref="PoC"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The total volume of asks.</returns>
	public static decimal SellVolAbovePoC(this VolumeProfileBuilder volumeProfile)
	{
		var poc = volumeProfile.PoC();
		return volumeProfile.PriceLevels.Where(p => p.Price > poc.Price).Select(p => p.SellVolume).Sum();
	}

	/// <summary>
	/// The total volume of asks which was below <see cref="PoC"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The total volume of asks.</returns>
	public static decimal SellVolBelowPoC(this VolumeProfileBuilder volumeProfile)
	{
		var poc = volumeProfile.PoC();
		return volumeProfile.PriceLevels.Where(p => p.Price < poc.Price).Select(p => p.SellVolume).Sum();
	}

	/// <summary>
	/// The total volume which was above <see cref="PoC"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>Total volume.</returns>
	public static decimal VolumeAbovePoC(this VolumeProfileBuilder volumeProfile)
	{
		return volumeProfile.BuyVolAbovePoC() + volumeProfile.SellVolAbovePoC();
	}

	/// <summary>
	/// The total volume which was below <see cref="PoC"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>Total volume.</returns>
	public static decimal VolumeBelowPoC(this VolumeProfileBuilder volumeProfile)
	{
		return volumeProfile.BuyVolBelowPoC() + volumeProfile.SellVolBelowPoC();
	}

	/// <summary>
	/// The difference between <see cref="TotalBuyVolume"/> and <see cref="TotalSellVolume"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>Delta.</returns>
	public static decimal Delta(this VolumeProfileBuilder volumeProfile)
	{
		return volumeProfile.TotalBuyVolume() - volumeProfile.TotalSellVolume();
	}

	/// <summary>
	/// It returns the price level at which the maximum <see cref="Delta"/> is passed.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns><see cref="CandlePriceLevel"/>.</returns>
	public static CandlePriceLevel PriceLevelOfMaxDelta(this VolumeProfileBuilder volumeProfile)
	{
		if (volumeProfile == null)
			throw new ArgumentNullException(nameof(volumeProfile));

		var delta = volumeProfile.PriceLevels.Select(p => p.BuyVolume - p.SellVolume).Max();
		return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume - p.SellVolume == delta);
	}

	/// <summary>
	/// It returns the price level at which the minimum <see cref="Delta"/> is passed.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>The price level.</returns>
	public static CandlePriceLevel PriceLevelOfMinDelta(this VolumeProfileBuilder volumeProfile)
	{
		if (volumeProfile == null)
			throw new ArgumentNullException(nameof(volumeProfile));

		var delta = volumeProfile.PriceLevels.Select(p => p.BuyVolume - p.SellVolume).Min();
		return volumeProfile.PriceLevels.FirstOrDefault(p => p.BuyVolume - p.SellVolume == delta);
	}

	/// <summary>
	/// The total Delta which was above <see cref="PoC"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>Delta.</returns>
	public static decimal DeltaAbovePoC(this VolumeProfileBuilder volumeProfile)
	{
		return volumeProfile.BuyVolAbovePoC() - volumeProfile.SellVolAbovePoC();
	}

	/// <summary>
	/// The total Delta which was below <see cref="PoC"/>.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <returns>Delta.</returns>
	public static decimal DeltaBelowPoC(this VolumeProfileBuilder volumeProfile)
	{
		return volumeProfile.BuyVolBelowPoC() - volumeProfile.SellVolBelowPoC();
	}

	/// <summary>
	/// To update the profile with new value.
	/// </summary>
	/// <param name="volumeProfile">Volume profile.</param>
	/// <param name="transform">The data source transformation.</param>
	public static void Update(this VolumeProfileBuilder volumeProfile, ICandleBuilderValueTransform transform)
	{
		if (volumeProfile == null)
			throw new ArgumentNullException(nameof(volumeProfile));

		if (transform == null)
			throw new ArgumentNullException(nameof(transform));

		volumeProfile.Update(transform.Price, transform.Volume, transform.Side);
	}

	/// <summary>
	/// To get the candle middle price.
	/// </summary>
	/// <param name="candle">The candle for which you need to get a length.</param>
	/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
	/// <returns>The candle length.</returns>
	public static decimal GetMiddlePrice(this ICandleMessage candle, decimal? priceStep)
	{
		if (candle is null)
			throw new ArgumentNullException(nameof(candle));

		var price = candle.LowPrice + candle.GetLength() / 2;

		if (priceStep is not null)
			price = price.ShrinkPrice(priceStep, priceStep.Value.GetCachedDecimals());

		return price;
	}

	/// <summary>
	/// To get the candle length.
	/// </summary>
	/// <param name="candle">The candle for which you need to get a length.</param>
	/// <returns>The candle length.</returns>
	public static decimal GetLength(this ICandleMessage candle)
	{
		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		return candle.HighPrice - candle.LowPrice;
	}

	/// <summary>
	/// To get the candle body.
	/// </summary>
	/// <param name="candle">The candle for which you need to get the body.</param>
	/// <returns>The candle body.</returns>
	public static decimal GetBody(this ICandleMessage candle)
	{
		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		return (candle.OpenPrice - candle.ClosePrice).Abs();
	}

	/// <summary>
	/// To get the candle upper shadow length.
	/// </summary>
	/// <param name="candle">The candle for which you need to get the upper shadow length.</param>
	/// <returns>The candle upper shadow length. If 0, there is no shadow.</returns>
	public static decimal GetTopShadow(this ICandleMessage candle)
	{
		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		return candle.HighPrice - candle.OpenPrice.Max(candle.ClosePrice);
	}

	/// <summary>
	/// To get the candle lower shadow length.
	/// </summary>
	/// <param name="candle">The candle for which you need to get the lower shadow length.</param>
	/// <returns>The candle lower shadow length. If 0, there is no shadow.</returns>
	public static decimal GetBottomShadow(this ICandleMessage candle)
	{
		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		return candle.OpenPrice.Min(candle.ClosePrice) - candle.LowPrice;
	}

	/// <summary>
	/// To create tick trades from candle.
	/// </summary>
	/// <param name="candleMsg">Candle.</param>
	/// <param name="volumeStep">Volume step.</param>
	/// <param name="decimals">The number of decimal places for the volume.</param>
	/// <param name="ticks">Array to tick trades.</param>
	public static void ConvertToTrades(this ICandleMessage candleMsg, decimal volumeStep, int decimals, (Sides? side, decimal price, decimal volume, DateTimeOffset time)[] ticks)
	{
		if (candleMsg is null)
			throw new ArgumentNullException(nameof(candleMsg));

		if (ticks is null)
			throw new ArgumentNullException(nameof(ticks));

		if (ticks.Length != 4)
			throw new ArgumentOutOfRangeException(nameof(ticks));

		var vol = (candleMsg.TotalVolume / 4).Round(volumeStep, decimals, MidpointRounding.AwayFromZero);
		var isUptrend = candleMsg.ClosePrice >= candleMsg.OpenPrice;
		var time = candleMsg.OpenTime;

		if (candleMsg.OpenPrice == candleMsg.ClosePrice &&
			candleMsg.LowPrice == candleMsg.HighPrice &&
			candleMsg.OpenPrice == candleMsg.LowPrice ||
			candleMsg.TotalVolume == 1)
		{
			// все цены в свече равны или объем равен 1 - считаем ее за один тик
			ticks[0] = (Sides.Buy, candleMsg.OpenPrice, candleMsg.TotalVolume, time);

			ticks[1] = ticks[2] = ticks[3] = default;
		}
		else if (candleMsg.TotalVolume == 2)
		{
			ticks[0] = (Sides.Buy, candleMsg.HighPrice, 1, candleMsg.HighTime == default ? time : candleMsg.HighTime);
			ticks[1] = (Sides.Sell, candleMsg.LowPrice, 1, candleMsg.LowTime == default ? time : candleMsg.LowTime);

			ticks[2] = ticks[3] = default;
		}
		else if (candleMsg.TotalVolume == 3)
		{
			ticks[0] = (isUptrend ? Sides.Buy : Sides.Sell, candleMsg.OpenPrice, 1, time);
			ticks[1] = (Sides.Buy, candleMsg.HighPrice, 1, candleMsg.HighTime == default ? time : candleMsg.HighTime);
			ticks[2] = (Sides.Sell, candleMsg.LowPrice, 1, candleMsg.LowTime == default ? time : candleMsg.LowTime);

			ticks[3] = default;
		}
		else
		{
			ticks[0] = (isUptrend ? Sides.Buy : Sides.Sell, candleMsg.OpenPrice, vol, time);
			ticks[1] = (Sides.Buy, candleMsg.HighPrice, vol, candleMsg.HighTime == default ? time : candleMsg.HighTime);
			ticks[2] = (Sides.Sell, candleMsg.LowPrice, vol, candleMsg.LowTime == default ? time : candleMsg.LowTime);
			ticks[3] = (isUptrend ? Sides.Buy : Sides.Sell, candleMsg.ClosePrice, candleMsg.TotalVolume - 3 * vol, candleMsg.CloseTime == default ? time : candleMsg.CloseTime);
		}
	}

	/// <summary>
	/// Convert tick info into <see cref="ExecutionMessage"/>.
	/// </summary>
	/// <param name="tick">Tick info.</param>
	/// <param name="securityId"><see cref="ExecutionMessage.SecurityId"/></param>
	/// <param name="localTime"><see cref="Message.LocalTime"/></param>
	/// <returns><see cref="ExecutionMessage"/></returns>
	public static ExecutionMessage ToTickMessage(this (Sides? side, decimal price, decimal volume, DateTimeOffset time) tick, SecurityId securityId, DateTimeOffset localTime)
	{
		return new()
		{
			LocalTime = localTime,
			SecurityId = securityId,
			ServerTime = tick.time,
			//TradeId = _tradeIdGenerator.Next,
			TradePrice = tick.price,
			TradeVolume = tick.volume,
			OriginSide = tick.side,
			DataTypeEx = DataType.Ticks,
			//OpenInterest = openInterest
		};
	}

	private class TradeEnumerable<TCandle> : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
		where TCandle : ICandleMessage
	{
		private sealed class TradeEnumerator(IEnumerable<TCandle> candles, decimal volumeStep) : IEnumerator<ExecutionMessage>
		{
			private readonly IEnumerator<TCandle> _valuesEnumerator = candles.GetEnumerator();
			private IEnumerator<ExecutionMessage> _currCandleEnumerator;
			private readonly int _decimals = volumeStep.GetCachedDecimals();
			private readonly (Sides? side, decimal price, decimal volume, DateTimeOffset time)[] _ticks = new (Sides?, decimal, decimal, DateTimeOffset)[4];

			private IEnumerable<ExecutionMessage> ToTicks(TCandle candleMsg)
			{
				candleMsg.ConvertToTrades(volumeStep, _decimals, _ticks);

				foreach (var t in _ticks)
				{
					if (t == default)
						yield break;

					yield return t.ToTickMessage(candleMsg.SecurityId, candleMsg.LocalTime);
				}
			}

			public bool MoveNext()
			{
				if (_currCandleEnumerator == null)
				{
					if (_valuesEnumerator.MoveNext())
					{
						_currCandleEnumerator = ToTicks(_valuesEnumerator.Current).GetEnumerator();
					}
					else
					{
						Current = null;
						return false;
					}
				}

				if (_currCandleEnumerator.MoveNext())
				{
					Current = _currCandleEnumerator.Current;
					return true;
				}

				if (_valuesEnumerator.MoveNext())
				{
					_currCandleEnumerator = ToTicks(_valuesEnumerator.Current).GetEnumerator();

					_currCandleEnumerator.MoveNext();
					Current = _currCandleEnumerator.Current;

					return true;
				}

				Current = null;
				return false;
			}

			public void Reset()
			{
				_valuesEnumerator.Reset();
				Current = null;
			}

			public void Dispose()
			{
				Current = null;
				_valuesEnumerator.Dispose();
			}

			public ExecutionMessage Current { get; private set; }

			object IEnumerator.Current => Current;
		}

		public TradeEnumerable(IEnumerable<TCandle> candles, decimal volumeStep)
			: base(() => new TradeEnumerator(candles, volumeStep))
		{
			if (candles == null)
				throw new ArgumentNullException(nameof(candles));

			//_values = candles;
		}

		//private readonly IEnumerableEx<CandleMessage> _values;

		//public int Count => _values.Count * 4;
	}

	/// <summary>
	/// <see cref="ICandleMessage.PriceLevels"/> with minimum <see cref="CandlePriceLevel.TotalVolume"/>.
	/// </summary>
	/// <param name="candle"><see cref="ICandleMessage"/></param>
	/// <returns><see cref="ICandleMessage.PriceLevels"/> with minimum <see cref="CandlePriceLevel.TotalVolume"/>.</returns>
	public static CandlePriceLevel? MinPriceLevel(this ICandleMessage candle)
		=> candle.CheckOnNull(nameof(candle)).PriceLevels?.OrderBy(l => l.TotalVolume).FirstOr();

	/// <summary>
	/// <see cref="ICandleMessage.PriceLevels"/> with maximum <see cref="CandlePriceLevel.TotalVolume"/>.
	/// </summary>
	/// <param name="candle"><see cref="ICandleMessage"/></param>
	/// <returns><see cref="ICandleMessage.PriceLevels"/> with maximum <see cref="CandlePriceLevel.TotalVolume"/>.</returns>
	public static CandlePriceLevel? MaxPriceLevel(this ICandleMessage candle)
		=> candle.CheckOnNull(nameof(candle)).PriceLevels?.OrderByDescending(l => l.TotalVolume).FirstOr();

	/// <summary>
	/// To get candle time frames relatively to the exchange working hours.
	/// </summary>
	/// <param name="timeFrame">The time frame for which you need to get time range.</param>
	/// <param name="currentTime">The current time within the range of time frames.</param>
	/// <param name="board">The information about the board from which <see cref="BoardMessage.WorkingTime"/> working hours will be taken.</param>
	/// <returns>The candle time frames.</returns>
	public static Range<DateTimeOffset> GetCandleBounds(this TimeSpan timeFrame, DateTimeOffset currentTime, BoardMessage board)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		return timeFrame.GetCandleBounds(currentTime, board.TimeZone, board.WorkingTime);
	}

	/// <summary>
	/// To get the number of time frames within the specified time range.
	/// </summary>
	/// <param name="range">The specified time range for which you need to get the number of time frames.</param>
	/// <param name="timeFrame">The time frame size.</param>
	/// <param name="board"><see cref="BoardMessage"/>.</param>
	/// <returns>The received number of time frames.</returns>
	public static long GetTimeFrameCount(this Range<DateTimeOffset> range, TimeSpan timeFrame, BoardMessage board)
	{
		if (board is null)
			throw new ArgumentNullException(nameof(board));

		return range.GetTimeFrameCount(timeFrame, board.WorkingTime, board.TimeZone);
	}

	/// <summary>
	/// To calculate the area for the candles group.
	/// </summary>
	/// <param name="candles">Candles.</param>
	/// <returns>The area.</returns>
	public static VolumeProfileBuilder GetValueArea<TCandle>(this IEnumerable<TCandle> candles)
		where TCandle : ICandleMessage
	{
		var area = new VolumeProfileBuilder();

		foreach (var candle in candles)
		{
			if (candle.PriceLevels == null)
				continue;

			foreach (var priceLevel in candle.PriceLevels)
			{
				area.Update(priceLevel);
			}
		}

		area.Calculate();
		return area;
	}

	/// <summary>
	/// Compress candles to bigger time-frame candles.
	/// </summary>
	/// <param name="source">Smaller time-frame candles.</param>
	/// <param name="compressor">Compressor of candles from smaller time-frames to bigger.</param>
	/// <param name="includeLastCandle">Output last active candle as finished.</param>
	/// <returns>Bigger time-frame candles.</returns>
	public static IEnumerable<CandleMessage> Compress(this IEnumerable<CandleMessage> source, BiggerTimeFrameCandleCompressor compressor, bool includeLastCandle)
	{
		if (source == null)
			throw new ArgumentNullException(nameof(source));

		if (compressor == null)
			throw new ArgumentNullException(nameof(compressor));

		CandleMessage lastActiveCandle = null;

		foreach (var message in source)
		{
			foreach (var candleMessage in compressor.Process(message))
			{
				if (candleMessage.State == CandleStates.Finished)
				{
					lastActiveCandle = null;
					yield return candleMessage;
				}
				else
					lastActiveCandle = candleMessage;
			}
		}

		if (!includeLastCandle || lastActiveCandle == null)
			yield break;

		lastActiveCandle.State = CandleStates.Finished;
		yield return lastActiveCandle;
	}

	/// <summary>
	/// Filter time-frames to find multiple smaller time-frames.
	/// </summary>
	/// <param name="timeFrames">All time-frames.</param>
	/// <param name="original">Original time-frame.</param>
	/// <returns>Multiple smaller time-frames.</returns>
	public static IEnumerable<TimeSpan> FilterSmallerTimeFrames(this IEnumerable<TimeSpan> timeFrames, TimeSpan original)
	{
		return timeFrames.Where(t => t < original && (original.Ticks % t.Ticks) == 0);
	}

	/// <summary>
	/// Determines the specified candles are same.
	/// </summary>
	/// <typeparam name="TCandle"><see cref="ICandleMessage"/></typeparam>
	/// <param name="candle1">First.</param>
	/// <param name="candle2">Second.</param>
	/// <returns>Check result.</returns>
	public static bool IsSame<TCandle>(this TCandle candle1, TCandle candle2)
		where TCandle : ICandleMessage
#pragma warning disable CS0618 // Type or member is obsolete
		=> candle1 is not null && candle2 is not null && ((candle1 is Candle && ReferenceEquals(candle1, candle2)) || candle1.OpenTime == candle2.OpenTime);
#pragma warning restore CS0618 // Type or member is obsolete
}