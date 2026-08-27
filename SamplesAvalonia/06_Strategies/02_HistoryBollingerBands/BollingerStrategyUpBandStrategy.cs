namespace StockSharp.Samples.Strategies.HistoryBollingerBands.Avalonia;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class BollingerStrategyUpBandStrategy : Strategy
{
	private readonly StrategyParam<int> _bollingerLength;
	private readonly StrategyParam<decimal> _bollingerDeviation;
	private readonly StrategyParam<DataType> _candleType;
	private BollingerBands _bollingerBands;

	public BollingerStrategyUpBandStrategy()
	{
		_bollingerLength = Param(nameof(BollingerLength), 20)
			.SetGreaterThanZero()
			.SetDisplay("Bollinger Length", "Length of the Bollinger Bands indicator", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(10, 50, 5);
		_bollingerDeviation = Param(nameof(BollingerDeviation), 2m)
			.SetDisplay("Bollinger Deviation", "Standard deviation multiplier for Bollinger Bands", "Indicators")
			.SetCanOptimize(true)
			.SetOptimize(1m, 3m, 0.5m);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public int BollingerLength
	{
		get => _bollingerLength.Value;
		set => _bollingerLength.Value = value;
	}

	public decimal BollingerDeviation
	{
		get => _bollingerDeviation.Value;
		set => _bollingerDeviation.Value = value;
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
		_bollingerBands = new BollingerBands
		{
			Length = BollingerLength,
			Width = BollingerDeviation,
		};

		var subscription = SubscribeCandles(CandleType);
		subscription.BindEx(_bollingerBands, ProcessCandle).Start();

		var area = CreateChartArea();
		if (area is null)
			return;

		DrawCandles(area, subscription);
		DrawIndicator(area, _bollingerBands, System.Drawing.Color.Purple);
		DrawOwnTrades(area);
	}

	private void ProcessCandle(ICandleMessage candle, IIndicatorValue value)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		var bands = (IBollingerBandsValue)value;
		if (candle.ClosePrice >= bands.UpBand && Position == 0)
			BuyMarket(Volume);
		else if (candle.ClosePrice <= bands.MovingAverage && Position > 0)
			SellMarket(Position.Abs());
	}
}
