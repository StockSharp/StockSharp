using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.Messages;

namespace Run_strategy_in_real_trading
{
	public class MqStrategy : Strategy
	{
		protected override void OnStarted(DateTimeOffset time)
		{
			Connector.MarketTimeChanged += Connector_MarketTimeChanged;
			Connector_MarketTimeChanged(default);

			base.OnStarted(time);
		}

		private MarketQuotingStrategy _strategy;

		private void Connector_MarketTimeChanged(TimeSpan obj)
		{
			if (_strategy != null && _strategy.ProcessState != ProcessStates.Stopped) return;

			if (Position <= 0)
			{
				_strategy = new MarketQuotingStrategy(Sides.Buy, Volume + Math.Abs(Position))
				{
					Name = "buy " + CurrentTime,
					Volume = 1,
					PriceType = MarketPriceTypes.Following,
				};
				ChildStrategies.Add(_strategy);
			}
			else if (Position > 0)
			{
				_strategy = new MarketQuotingStrategy(Sides.Sell, Volume + Math.Abs(Position))
				{
					Name = "sell " + CurrentTime,
					Volume = 1,
					PriceType = MarketPriceTypes.Following,
				};
				ChildStrategies.Add(_strategy);
			}
		}
	}
}