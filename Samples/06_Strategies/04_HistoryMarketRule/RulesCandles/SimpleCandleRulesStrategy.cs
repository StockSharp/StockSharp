using System;

using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Algo.Strategies;
using StockSharp.Messages;

namespace StockSharp.Samples.Strategies.HistoryMarketRule
{
	public class SimpleCandleRulesStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			var subscription = new Subscription(TimeSpan.FromMinutes(5).TimeFrame(), Security)
			{
				// ready-to-use candles much faster than compression on fly mode
				// turn off compression to boost optimizer (!!! make sure you have candles)

				//MarketData =
				//{
				//	BuildMode = MarketDataBuildModes.Build,
				//	BuildFrom = DataType.Ticks,
				//}
			};
			Subscribe(subscription);

			var i = 0;
			var diff = "10%".ToUnit();

			this.WhenCandlesStarted(subscription)
				.Do((candle) =>
				{
					i++;

					this
						.WhenTotalVolumeMore(candle, diff)
						.Do((candle1) =>
						{
							LogInfo($"The rule WhenCandlesStarted and WhenTotalVolumeMore candle={candle1}");
							LogInfo($"The rule WhenCandlesStarted and WhenTotalVolumeMore i={i}");
						})
						.Once().Apply(this);

				}).Apply(this);

			base.OnStarted(time);
		}
	}
}