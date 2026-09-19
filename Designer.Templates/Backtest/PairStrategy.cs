namespace StockSharp.Designer;

using System;
using System.Linq;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Logging;
using Ecng.Drawing;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Indicators;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Charting;

/// <summary>
/// Sample strategy demonstrating the work with two instruments.
/// 
/// See more examples https://github.com/StockSharp/AlgoTrading
/// </summary>
public class PairStrategy : Strategy
{
	private bool? _isShortLessThenLong;
	private decimal? _lastPrice2;

	public PairStrategy()
	{
		_timeFrame = Param(nameof(TimeFrame), TimeSpan.FromMinutes(5));

		_security1 = Param<Security>(nameof(Security1));
		_security2 = Param<Security>(nameof(Security2));
		_long = Param(nameof(Long), 80);
		_short = Param(nameof(Short), 30);

		_takeValue = Param(nameof(TakeValue), new Unit(0, UnitTypes.Absolute));
		_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent));
	}

	private readonly StrategyParam<TimeSpan> _timeFrame;

	public TimeSpan TimeFrame
	{
		get => _timeFrame.Value;
		set => _timeFrame.Value = value;
	}

	private readonly StrategyParam<Security> _security1;

	public Security Security1
	{
		get => _security1.Value;
		set => _security1.Value = value;
	}

	private readonly StrategyParam<Security> _security2;

	public Security Security2
	{
		get => _security2.Value;
		set => _security2.Value = value;
	}

	private readonly StrategyParam<int> _long;

	public int Long
	{
		get => _long.Value;
		set => _long.Value = value;
	}

	private readonly StrategyParam<int> _short;

	public int Short
	{
		get => _short.Value;
		set => _short.Value = value;
	}

	private readonly StrategyParam<Unit> _takeValue;

	public Unit TakeValue
	{
		get => _takeValue.Value;
		set => _takeValue.Value = value;
	}

	private readonly StrategyParam<Unit> _stopValue;

	public Unit StopValue
	{
		get => _stopValue.Value;
		set => _stopValue.Value = value;
	}

	// to show in Designer what securities and data types are used
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security1, TimeFrame.TimeFrame()), (Security2, TimeFrame.TimeFrame())];

	protected override void OnReseted()
	{
		base.OnReseted();

		_isShortLessThenLong = null;
		_lastPrice2 = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		// ---------- create indicators -----------

		var longSma = new SMA { Length = Long };
		var shortSma = new SMA { Length = Short };

		// ----------------------------------------

		// --- bind candles set and indicators ----

		// the crossing signal is taken from the first leg only
		var subscription1 = SubscribeCandles(TimeFrame, security: Security1)
			// bind indicators to the candles
			.Bind(longSma, shortSma, OnProcess)
			// start processing
			.Start();

		// the second leg is followed for its own price, which the opposite order is quoted at
		var subscription2 = SubscribeCandles(TimeFrame, security: Security2)
			.Bind(candle => _lastPrice2 = candle.ClosePrice)
			.Start();

		// ----------------------------------------

		// ----------- configure chart ------------

		var area1 = CreateChartArea();

		// area can be null in case of no GUI (strategy hosted in Runner or in own console app)
		if (area1 != null)
		{
			DrawCandles(area1, subscription1);

			DrawIndicator(area1, shortSma, System.Drawing.Color.Coral);
			DrawIndicator(area1, longSma);

			DrawOwnTrades(area1);
		}

		// each leg gets its own area, they are priced independently
		var area2 = CreateChartArea();

		if (area2 != null)
		{
			DrawCandles(area2, subscription2);

			DrawOwnTrades(area2);
		}

		// ----------------------------------------

		// ---- configure position protection -----

		// start protection by take profit and-or stop loss
		StartProtection(TakeValue, StopValue);

		// ----------------------------------------
	}

	private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
	{
		LogInfo(LocalizedStrings.SmaNewCandleLog, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId);

		// in case we subscribed on non finished only candles
		if (candle.State != CandleStates.Finished)
			return;

		// the pair is traded as a whole, so nothing is sent until the second leg is priced as well
		if (_lastPrice2 is not decimal price2)
			return;

		// calc new values for short and long
		var isShortLessThenLong = shortValue < longValue;

		if (_isShortLessThenLong == null)
		{
			_isShortLessThenLong = isShortLessThenLong;
		}
		else if (_isShortLessThenLong != isShortLessThenLong)
		{
			// crossing happened

			// if short less than long, the sale, otherwise buy
			var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

			// the pair is market neutral: the signal leg takes the direction, the second one takes the opposite
			RegisterLeg(Security1, direction, candle.ClosePrice);
			RegisterLeg(Security2, direction.Invert(), price2);

			// store current values for short and long
			_isShortLessThenLong = isShortLessThenLong;
		}
	}

	private void RegisterLeg(Security security, Sides direction, decimal lastPrice)
	{
		// each leg carries its own position, so the size is counted per leg
		var position = GetPositionValue(security, Portfolio) ?? 0;

		// calc size for open position or revert
		var volume = position == 0 ? Volume : position.Abs().Min(Volume) * 2;

		var priceStep = security.PriceStep ?? 1;

		// calc order price as a close price + offset
		var price = lastPrice + (direction == Sides.Buy ? priceStep : -priceStep);

		if (direction == Sides.Buy)
			BuyLimit(price, volume, security);
		else
			SellLimit(price, volume, security);
	}
}
