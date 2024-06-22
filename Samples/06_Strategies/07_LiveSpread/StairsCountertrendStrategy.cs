namespace StockSharp.Samples.Strategies.LiveSpread;

using System;
using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

public class StairsCountertrendStrategy : Strategy
{
	private readonly Subscription _subscription;

	public StairsCountertrendStrategy(CandleSeries candleSeries)
	{
		_subscription = new(candleSeries);
	}

	private int _bullLength;
	private int _bearLength;
	public int Length { get; set; } = 2;

	protected override void OnStarted(DateTimeOffset time)
	{
		this
			.WhenCandlesFinished(_subscription)
			.Do(CandleManager_Processing)
			.Apply(this);

		Subscribe(_subscription);

		base.OnStarted(time);
	}

	private bool IsRealTime(ICandleMessage candle)
	{
		return (CurrentTime - candle.CloseTime).TotalSeconds < 40;
	}

	private void CandleManager_Processing(ICandleMessage candle)
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

		if (!IsBacktesting && !IsRealTime(candle)) return;

		if (_bullLength >= Length && Position >= 0)
		{
			RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
		}

		else if (_bearLength >= Length && Position <= 0)
		{
			RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
		}
	}
}