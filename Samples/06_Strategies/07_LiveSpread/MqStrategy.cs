namespace StockSharp.Samples.Strategies.LiveSpread;

using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.Messages;

public class MqStrategy : Strategy
{
	public DataType CandleDataType { get; set; }

	protected override void OnStarted(DateTimeOffset time)
	{
		Connector.CurrentTimeChanged += Connector_CurrentTimeChanged;
		Connector_CurrentTimeChanged(default);

		base.OnStarted(time);
	}

	private MarketQuotingStrategy _strategy;

	private void Connector_CurrentTimeChanged(TimeSpan obj)
	{
		if (_strategy != null && _strategy.ProcessState != ProcessStates.Stopped) return;

		if (Position <= 0)
		{
			_strategy = new MarketQuotingStrategy
			{
				QuotingSide = Sides.Buy,
				QuotingVolume = Volume + Math.Abs(Position),
				Name = "buy " + CurrentTime,
				Volume = 1,
				PriceType = MarketPriceTypes.Following,
			};
			ChildStrategies.Add(_strategy);
		}
		else if (Position > 0)
		{
			_strategy = new MarketQuotingStrategy
			{
				QuotingSide = Sides.Sell,
				QuotingVolume = Volume + Math.Abs(Position),
				Name = "sell " + CurrentTime,
				Volume = 1,
				PriceType = MarketPriceTypes.Following,
			};
			ChildStrategies.Add(_strategy);
		}
	}
}