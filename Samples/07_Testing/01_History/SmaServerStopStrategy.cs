namespace StockSharp.Samples.Testing.History;

using System;

using Ecng.Common;
using Ecng.ComponentModel;

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
class SmaServerStopStrategy : Strategy
{
	private bool? _isShortLessThenLong;
	private Order _activeStopOrder;

	public SmaServerStopStrategy()
	{
		_longSma = Param(nameof(LongSma), 80)
			.SetCanOptimize(true)
			.SetOptimize(50, 100, 5);

		_shortSma = Param(nameof(ShortSma), 30)
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 1);

		_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent));
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetRequired();

		_candleTimeFrame = Param<TimeSpan?>(nameof(CandleTimeFrame))
			.SetCanOptimize(true)
			.SetOptimize(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(5));

		_buildFrom = Param<DataType>(nameof(BuildFrom));
		_buildField = Param<Level1Fields?>(nameof(BuildField));
	}

	private readonly StrategyParam<TimeSpan?> _candleTimeFrame;

	public TimeSpan? CandleTimeFrame
	{
		get => _candleTimeFrame.Value;
		set => _candleTimeFrame.Value = value;
	}

	private readonly StrategyParam<int> _longSma;

	public int LongSma
	{
		get => _longSma.Value;
		set => _longSma.Value = value;
	}

	private readonly StrategyParam<int> _shortSma;

	public int ShortSma
	{
		get => _shortSma.Value;
		set => _shortSma.Value = value;
	}

	private readonly StrategyParam<DataType> _candleType;

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	private readonly StrategyParam<DataType> _buildFrom;

	public DataType BuildFrom
	{
		get => _buildFrom.Value;
		set => _buildFrom.Value = value;
	}

	private readonly StrategyParam<Level1Fields?> _buildField;

	public Level1Fields? BuildField
	{
		get => _buildField.Value;
		set => _buildField.Value = value;
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

		var dt = CandleTimeFrame is null
			? CandleType
			: DataType.Create(CandleType.MessageType, CandleTimeFrame);

		var subscription = new Subscription(dt, Security)
		{
			MarketData =
			{
				IsFinishedOnly = true,
				BuildFrom = BuildFrom,
				BuildMode = BuildFrom is null ? MarketDataBuildModes.LoadAndBuild : MarketDataBuildModes.Build,
				BuildField = BuildField,
			}
		};

		var longSma = new SMA { Length = LongSma };
		var shortSma = new SMA { Length = ShortSma };

		SubscribeCandles(subscription)
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
			var price = candle.ClosePrice;

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
