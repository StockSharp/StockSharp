namespace StockSharp.Designer;

using System;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.MatchingEngine;
using StockSharp.Messages;
using StockSharp.Localization;

/// <summary>
/// SMA crossover strategy using server-side stop orders
/// instead of <see cref="Strategy.StartProtection"/>.
/// </summary>
public class SmaServerStopStrategy : Strategy
{
	private bool? _isShortLessThenLong;
	private Order _activeStopOrder;

	public SmaServerStopStrategy()
	{
		_candleTypeParam = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle type", "Candle type for strategy calculation.", "General");

		_long = Param(nameof(Long), 80);
		_short = Param(nameof(Short), 30);

		_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent));
	}

	private readonly StrategyParam<DataType> _candleTypeParam;

	public DataType CandleType
	{
		get => _candleTypeParam.Value;
		set => _candleTypeParam.Value = value;
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

	private readonly StrategyParam<Unit> _stopValue;

	public Unit StopValue
	{
		get => _stopValue.Value;
		set => _stopValue.Value = value;
	}

	protected override void OnReseted()
	{
		base.OnReseted();

		_isShortLessThenLong = null;
		_activeStopOrder = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var longSma = new SMA { Length = Long };
		var shortSma = new SMA { Length = Short };

		var subscription = SubscribeCandles(CandleType);

		subscription
			.Bind(longSma, shortSma, OnProcess)
			.Start();

		var area = CreateChartArea();

		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortSma, System.Drawing.Color.Coral);
			DrawIndicator(area, longSma);
			DrawOwnTrades(area);
		}

		// Track stop order state changes
		OrderReceived += (_, order) => OnOrderChanged(order);
	}

	private void OnOrderChanged(Order order)
	{
		if (_activeStopOrder != null && order.TransactionId == _activeStopOrder.TransactionId)
		{
			if (order.State == OrderStates.Done)
			{
				// Stop order triggered or cancelled
				_activeStopOrder = null;
			}
		}
	}

	private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
	{
		LogInfo(LocalizedStrings.SmaNewCandleLog, candle.OpenTime, candle.OpenPrice, candle.HighPrice, candle.LowPrice, candle.ClosePrice, candle.TotalVolume, candle.SecurityId);

		if (candle.State != CandleStates.Finished)
			return;

		var isShortLessThenLong = shortValue < longValue;

		if (_isShortLessThenLong == null)
		{
			_isShortLessThenLong = isShortLessThenLong;
		}
		else if (_isShortLessThenLong != isShortLessThenLong) // crossing happened
		{
			// Cancel active stop order before reversing
			if (_activeStopOrder is { State: OrderStates.Active })
			{
				CancelOrder(_activeStopOrder);
				_activeStopOrder = null;
			}

			var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;
			var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

			var priceStep = GetSecurity().PriceStep ?? 1;
			var price = candle.ClosePrice + (direction == Sides.Buy ? priceStep : -priceStep);

			if (direction == Sides.Buy)
				BuyLimit(price, volume);
			else
				SellLimit(price, volume);

			_isShortLessThenLong = isShortLessThenLong;
		}

		// Register stop order if position is open and no active stop
		if (Position != 0 && _activeStopOrder == null)
		{
			RegisterStopForPosition(candle.ClosePrice);
		}
	}

	private void RegisterStopForPosition(decimal currentPrice)
	{
		var stopOffset = StopValue.Type == UnitTypes.Percent
			? currentPrice * StopValue.Value / 100m
			: StopValue.Value;

		decimal stopPrice;
		Sides stopSide;

		if (Position > 0)
		{
			stopSide = Sides.Sell;
			stopPrice = currentPrice - stopOffset;
		}
		else
		{
			stopSide = Sides.Buy;
			stopPrice = currentPrice + stopOffset;
		}

		_activeStopOrder = new Order
		{
			Type = OrderTypes.Conditional,
			Condition = new StopOrderCondition { ActivationPrice = stopPrice },
			Side = stopSide,
			Volume = Position.Abs(),
			Security = Security,
			Portfolio = Portfolio,
		};

		RegisterOrder(_activeStopOrder);
	}
}
