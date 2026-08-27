namespace StockSharp.Samples.Strategies.HistoryMarketRule.Avalonia;

using System;

using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

public class SimpleCandleRulesStrategy : Strategy
{
	protected override void OnStarted2(DateTime time)
	{
		var subscription = new Subscription(TimeSpan.FromMinutes(5).TimeFrame(), Security);
		Subscribe(subscription);

		var candleNumber = 0;
		var volumeDifference = "10%".ToUnit();

		this.WhenCandlesStarted(subscription)
			.Do(candle =>
			{
				candleNumber++;
				this.WhenTotalVolumeMore(candle, volumeDifference)
					.Do(changedCandle =>
					{
						LogInfo($"The rule WhenCandlesStarted and WhenTotalVolumeMore candle={changedCandle}");
						LogInfo($"The rule WhenCandlesStarted and WhenTotalVolumeMore i={candleNumber}");
					})
					.Once()
					.Apply(this);
			})
			.Apply(this);

		base.OnStarted2(time);
	}
}
