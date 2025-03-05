namespace StockSharp.Samples.Strategies.LiveSpread;

using System;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;
using StockSharp.BusinessEntities;

public class StairsCountertrendStrategy : Strategy
{
	public DataType CandleDataType { get; set; }

	private int _bullLength;
	private int _bearLength;
	public int Length { get; set; } = 2;

	protected override void OnStarted(DateTimeOffset time)
	{
		var subscription = new Subscription(CandleDataType, Security);

		this
			.WhenCandlesFinished(subscription)
			.Do(Processing)
			.Apply(this);

		Subscribe(subscription);

		base.OnStarted(time);
	}

	private void Processing(ICandleMessage candle)
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

		if (!IsFormedAndOnlineAndAllowTrading()) return;

		if (_bullLength >= Length && Position >= 0)
		{
			SellMarket(Volume + Math.Abs(Position));
		}

		else if (_bearLength >= Length && Position <= 0)
		{
			BuyMarket(Volume + Math.Abs(Position));
		}
	}
}