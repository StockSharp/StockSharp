namespace StockSharp.Samples.Strategies.HistoryTrend.Avalonia;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class StairsCountertrendStrategy : Strategy
{
	private readonly StrategyParam<int> _length;
	private readonly StrategyParam<DataType> _candleType;
	private int _bullLength;
	private int _bearLength;

	public StairsCountertrendStrategy()
	{
		_length = Param(nameof(Length), 3)
			.SetGreaterThanZero()
			.SetDisplay("Length", "Number of consecutive candles to trigger signal", "Strategy")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);
		_candleType = Param(nameof(CandleType), TimeSpan.FromMinutes(5).TimeFrame())
			.SetDisplay("Candle Type", "Type of candles to use", "General");
	}

	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
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
		_bullLength = 0;
		_bearLength = 0;
		var subscription = SubscribeCandles(CandleType);
		subscription.Bind(ProcessCandle).Start();

		var area = CreateChartArea();
		if (area is null)
			return;

		DrawCandles(area, subscription);
		DrawOwnTrades(area);
	}

	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished || !IsFormedAndOnlineAndAllowTrading())
			return;

		UpdateDirection(candle);
		if (_bullLength >= Length && Position >= 0)
			SellMarket(Volume + Position.Abs());
		else if (_bearLength >= Length && Position <= 0)
			BuyMarket(Volume + Position.Abs());
	}

	private void UpdateDirection(ICandleMessage candle)
	{
		if (candle.OpenPrice < candle.ClosePrice)
		{
			_bullLength++;
			_bearLength = 0;
		}
		else if (candle.OpenPrice > candle.ClosePrice)
		{
			_bullLength = 0;
			_bearLength++;
		}
	}
}
