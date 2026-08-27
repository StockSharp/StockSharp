namespace StockSharp.Samples.Testing.History.Avalonia;

using System;

using Ecng.Common;
using Ecng.ComponentModel;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

internal sealed class SmaStrategy : Strategy
{
	private bool? _isShortLessThanLong;
	private readonly StrategyParam<TimeSpan?> _candleTimeFrame;
	private readonly StrategyParam<int> _longSma;
	private readonly StrategyParam<int> _shortSma;
	private readonly StrategyParam<DataType> _candleType;
	private readonly StrategyParam<DataType> _buildFrom;
	private readonly StrategyParam<Level1Fields?> _buildField;
	private readonly StrategyParam<Unit> _takeValue;
	private readonly StrategyParam<Unit> _stopValue;

	public SmaStrategy()
	{
		_longSma = Param(nameof(LongSma), 80)
			.SetCanOptimize(true)
			.SetOptimize(50, 100, 5);
		_shortSma = Param(nameof(ShortSma), 30)
			.SetCanOptimize(true)
			.SetOptimize(20, 40, 1);
		_takeValue = Param(nameof(TakeValue), new Unit(0, UnitTypes.Absolute));
		_stopValue = Param(nameof(StopValue), new Unit(2, UnitTypes.Percent));
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(1).TimeFrame()).SetRequired();
		_candleTimeFrame = Param<TimeSpan?>(nameof(CandleTimeFrame))
			.SetCanOptimize(true)
			.SetOptimize(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(5));
		_buildFrom = Param<DataType>(nameof(BuildFrom));
		_buildField = Param<Level1Fields?>(nameof(BuildField));
	}

	public TimeSpan? CandleTimeFrame
	{
		get => _candleTimeFrame.Value;
		set => _candleTimeFrame.Value = value;
	}

	public int LongSma
	{
		get => _longSma.Value;
		set => _longSma.Value = value;
	}

	public int ShortSma
	{
		get => _shortSma.Value;
		set => _shortSma.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public DataType BuildFrom
	{
		get => _buildFrom.Value;
		set => _buildFrom.Value = value;
	}

	public Level1Fields? BuildField
	{
		get => _buildField.Value;
		set => _buildField.Value = value;
	}

	public Unit TakeValue
	{
		get => _takeValue.Value;
		set => _takeValue.Value = value;
	}

	public Unit StopValue
	{
		get => _stopValue.Value;
		set => _stopValue.Value = value;
	}

	protected override void OnReseted()
	{
		base.OnReseted();
		_isShortLessThanLong = null;
	}

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		var candleDataType = CandleTimeFrame is null
			? CandleType
			: DataType.Create(CandleType.MessageType, CandleTimeFrame);
		var subscription = new Subscription(candleDataType, Security)
		{
			MarketData =
			{
				IsFinishedOnly = true,
				BuildFrom = BuildFrom,
				BuildMode = BuildFrom is null ? MarketDataBuildModes.LoadAndBuild : MarketDataBuildModes.Build,
				BuildField = BuildField,
			},
		};

		var longSma = new SMA { Length = LongSma };
		var shortSma = new SMA { Length = ShortSma };
		SubscribeCandles(subscription)
			.Bind(longSma, shortSma, OnProcess)
			.Start();

		var area = CreateChartArea();
		if (area is not null)
		{
			DrawCandles(area, subscription);
			DrawIndicator(area, shortSma, System.Drawing.Color.Coral);
			DrawIndicator(area, longSma);
			DrawOwnTrades(area);
		}

		StartProtection(TakeValue, StopValue);
	}

	private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
	{
		LogInfo(
			LocalizedStrings.SmaNewCandleLog,
			candle.OpenTime,
			candle.OpenPrice,
			candle.HighPrice,
			candle.LowPrice,
			candle.ClosePrice,
			candle.TotalVolume,
			candle.SecurityId);

		if (candle.State != CandleStates.Finished)
			return;

		var isShortLessThanLong = shortValue < longValue;
		if (_isShortLessThanLong is null)
		{
			_isShortLessThanLong = isShortLessThanLong;
			return;
		}

		if (_isShortLessThanLong == isShortLessThanLong)
			return;

		var direction = isShortLessThanLong ? Sides.Sell : Sides.Buy;
		var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;
		if (direction == Sides.Buy)
			BuyLimit(candle.ClosePrice, volume);
		else
			SellLimit(candle.ClosePrice, volume);

		_isShortLessThanLong = isShortLessThanLong;
	}
}
