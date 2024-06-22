namespace StockSharp.Samples.Strategies.LiveSpread;

using System;
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.Messages;

public class MqSpreadStrategy : Strategy
{
	protected override void OnStarted(DateTimeOffset time)
	{
		Connector.MarketTimeChanged += Connector_MarketTimeChanged;
		Connector_MarketTimeChanged(new TimeSpan());
		base.OnStarted(time);
	}

	private MarketQuotingStrategy _strategyBuy;
	private MarketQuotingStrategy _strategySell;

	private void Connector_MarketTimeChanged(TimeSpan obj)
	{
		if (Position != 0) return;
		if (_strategyBuy != null && _strategyBuy.ProcessState != ProcessStates.Stopped) return;
		if (_strategySell != null && _strategySell.ProcessState != ProcessStates.Stopped) return;

		_strategyBuy = new MarketQuotingStrategy(Sides.Buy, Volume)
		{
			Name = "buy " + CurrentTime,
			Volume = 1,
			PriceType = MarketPriceTypes.Following,
			IsSupportAtomicReRegister = false
		};
		ChildStrategies.Add(_strategyBuy);

		_strategySell = new MarketQuotingStrategy(Sides.Sell, Volume)
		{
			Name = "sell " + CurrentTime,
			Volume = 1,
			PriceType = MarketPriceTypes.Following,
			IsSupportAtomicReRegister = false
		};
		ChildStrategies.Add(_strategySell);
	}
}