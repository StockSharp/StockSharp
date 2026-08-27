namespace StockSharp.Samples.Strategies.HistorySMA.Avalonia;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class SmaStrategyMartingaleStrategy : Strategy
{
	private readonly StrategyParam<int> _longSmaLength;
	private readonly StrategyParam<int> _shortSmaLength;
	private readonly StrategyParam<DataType> _candleType;
	private decimal _previousLong;
	private decimal _previousShort;
	private bool _isFirstValue = true;

	public SmaStrategyMartingaleStrategy()
	{
		_longSmaLength = Param(nameof(LongSmaLength), 80)
			.SetGreaterThanZero()
			.SetDisplay("Long SMA Length", "Length of the long SMA indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(40, 120, 10);
		_shortSmaLength = Param(nameof(ShortSmaLength), 30)
			.SetGreaterThanZero()
			.SetDisplay("Short SMA Length", "Length of the short SMA indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public int LongSmaLength
	{
		get => _longSmaLength.Value;
		set => _longSmaLength.Value = value;
	}

	public int ShortSmaLength
	{
		get => _shortSmaLength.Value;
		set => _shortSmaLength.Value = value;
	}

	public DataType CandleType
	{
		get => _candleType.Value;
		set => _candleType.Value = value;
	}

	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
		=> [(Security, CandleType)];

	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);
		_isFirstValue = true;

		var longSma = new SimpleMovingAverage { Length = LongSmaLength };
		var shortSma = new SimpleMovingAverage { Length = ShortSmaLength };
		Indicators.Add(longSma);
		Indicators.Add(shortSma);

		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(longSma, shortSma, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area is null)
			return;

		DrawCandles(area, subscription);
		DrawIndicator(area, longSma, System.Drawing.Color.Blue);
		DrawIndicator(area, shortSma, System.Drawing.Color.Red);
		DrawOwnTrades(area);
	}

	private void ProcessCandle(ICandleMessage candle, decimal longValue, decimal shortValue)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		if (_isFirstValue)
		{
			_previousLong = longValue;
			_previousShort = shortValue;
			_isFirstValue = false;
			return;
		}

		var shortBelowLong = shortValue < longValue;
		var wasShortBelowLong = _previousShort < _previousLong;
		_previousLong = longValue;
		_previousShort = shortValue;

		if (shortBelowLong == wasShortBelowLong)
			return;

		CancelActiveOrders();
		var direction = shortBelowLong ? Sides.Sell : Sides.Buy;
		var volume = Volume + Position.Abs();
		var price = Security.ShrinkPrice(shortValue);
		RegisterOrder(CreateOrder(direction, price, volume));
	}
}
