namespace StockSharp.Samples.Strategies.HistoryQuoting;

using System;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class StairsCountertrendStrategy : Strategy
{
	public DataType CandleDataType { get; set; }

	private int _bullLength;
	private int _bearLength;
	public int Length { get; set; } = 3;

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
		if (candle.State != CandleStates.Finished) return;

		if (candle.OpenPrice < candle.ClosePrice)
		{
			_bullLength++;
			_bearLength = 0;
		}
		else
		if (candle.OpenPrice > candle.ClosePrice)
		{
			_bullLength = 0;
			_bearLength++;
		}

		if (_bullLength >= Length && Position >= 0)
		{
			ChildStrategies.ToList().ForEach(s => s.Stop());
			var strategy = new MarketQuotingStrategy
			{
				QuotingSide = Sides.Sell,
				QuotingVolume = 1,
				WaitAllTrades = true
			};
			ChildStrategies.Add(strategy);
		}

		else
		if (_bearLength >= Length && Position <= 0)
		{
			ChildStrategies.ToList().ForEach(s => s.Stop());
			var strategy = new MarketQuotingStrategy
			{
				QuotingSide = Sides.Buy,
				QuotingVolume = 1,
				WaitAllTrades = true
			};
			ChildStrategies.Add(strategy);
		}
	}
}