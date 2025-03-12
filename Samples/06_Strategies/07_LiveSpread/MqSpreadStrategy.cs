namespace StockSharp.Samples.Strategies.LiveSpread;

using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.Messages;

public class MqSpreadStrategy : Strategy
{
	public DataType CandleDataType { get; set; }

	protected override void OnStarted(DateTimeOffset time)
	{
		Connector.CurrentTimeChanged += Connector_CurrentTimeChanged;
		Connector_CurrentTimeChanged(new TimeSpan());
		base.OnStarted(time);
	}

	private MarketQuotingStrategy _strategyBuy;
	private MarketQuotingStrategy _strategySell;

	private void Connector_CurrentTimeChanged(TimeSpan obj)
	{
		if (Position != 0) return;
		if (_strategyBuy != null && _strategyBuy.ProcessState != ProcessStates.Stopped) return;
		if (_strategySell != null && _strategySell.ProcessState != ProcessStates.Stopped) return;

		_strategyBuy = new MarketQuotingStrategy
		{
			QuotingSide = Sides.Buy,
			Name = "buy " + CurrentTime,
			Volume = 1,
			PriceType = MarketPriceTypes.Following,
		};
		ChildStrategies.Add(_strategyBuy);

		_strategySell = new MarketQuotingStrategy
		{
			QuotingSide = Sides.Sell,
			Name = "sell " + CurrentTime,
			Volume = 1,
			PriceType = MarketPriceTypes.Following,
		};
		ChildStrategies.Add(_strategySell);
	}
}