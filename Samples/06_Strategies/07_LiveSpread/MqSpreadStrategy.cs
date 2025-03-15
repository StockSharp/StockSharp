namespace StockSharp.Samples.Strategies.LiveSpread;

using System;

using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.Messages;

/// <summary>
/// Market quoting spread strategy using QuotingProcessor.
/// Creates buy and sell quotes to maintain a spread.
/// </summary>
public class MqSpreadStrategy : Strategy
{
	private readonly StrategyParam<MarketPriceTypes> _priceType;
	private readonly StrategyParam<Unit> _priceOffset;
	private readonly StrategyParam<Unit> _bestPriceOffset;

	private QuotingProcessor _buyProcessor;
	private QuotingProcessor _sellProcessor;

	/// <summary>
	/// Initializes a new instance of <see cref="MqSpreadStrategy"/>.
	/// </summary>
	public MqSpreadStrategy()
	{
		_priceType = Param(nameof(PriceType), MarketPriceTypes.Following)
			.SetDisplay("Price Type", "Market price type for quoting", "Quoting Settings");

		_priceOffset = Param(nameof(PriceOffset), new Unit())
			.SetDisplay("Price Offset", "Offset from the market price", "Quoting Settings");

		_bestPriceOffset = Param(nameof(BestPriceOffset), new Unit(0.1m, UnitTypes.Percent))
			.SetDisplay("Best Price Offset", "Minimum deviation to update quote", "Quoting Settings");
	}

	/// <summary>
	/// Market price type for quoting.
	/// </summary>
	public MarketPriceTypes PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <summary>
	/// Price offset from the market price.
	/// </summary>
	public Unit PriceOffset
	{
		get => _priceOffset.Value;
		set => _priceOffset.Value = value;
	}

	/// <summary>
	/// Minimum deviation to update quote.
	/// </summary>
	public Unit BestPriceOffset
	{
		get => _bestPriceOffset.Value;
		set => _bestPriceOffset.Value = value;
	}

	/// <summary>
	/// On strategy started.
	/// </summary>
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		// Subscribe to market time changes to update quotes
		Connector.CurrentTimeChanged += Connector_CurrentTimeChanged;
		Connector_CurrentTimeChanged(new TimeSpan());
	}

	/// <summary>
	/// On strategy stopped.
	/// </summary>
	protected override void OnStopped()
	{
		// Unsubscribe to prevent memory leaks
		Connector.CurrentTimeChanged -= Connector_CurrentTimeChanged;

		// Dispose processors if they exist
		_buyProcessor?.Dispose();
		_buyProcessor = null;

		_sellProcessor?.Dispose();
		_sellProcessor = null;

		base.OnStopped();
	}

	private void Connector_CurrentTimeChanged(TimeSpan obj)
	{
		// Only create new processors when position is zero and existing processors are stopped
		if (Position != 0)
			return;

		if (_buyProcessor != null && _buyProcessor.LeftVolume > 0)
			return;

		if (_sellProcessor != null && _sellProcessor.LeftVolume > 0)
			return;

		// Dispose existing processors
		_buyProcessor?.Dispose();
		_buyProcessor = null;

		_sellProcessor?.Dispose();
		_sellProcessor = null;

		// Create market quoting behaviors
		var buyBehavior = new MarketQuotingBehavior(
			PriceOffset,
			BestPriceOffset,
			PriceType
		);

		var sellBehavior = new MarketQuotingBehavior(
			PriceOffset,
			BestPriceOffset,
			PriceType
		);

		// Create buy processor
		_buyProcessor = new QuotingProcessor(
			buyBehavior,
			Security,
			Portfolio,
			Sides.Buy,
			Volume,
			Volume, // Max order volume
			TimeSpan.Zero, // No timeout
			this, // Strategy implements ISubscriptionProvider
			this, // Strategy implements IMarketRuleContainer
			this, // Strategy implements ITransactionProvider
			this, // Strategy implements ITimeProvider
			this, // Strategy implements IMarketDataProvider
			IsFormedAndOnlineAndAllowTrading, // Check if trading is allowed
			true, // Use order book prices
			true  // Use last trade price if order book is empty
		)
		{
			Parent = this
		};

		// Create sell processor
		_sellProcessor = new QuotingProcessor(
			sellBehavior,
			Security,
			Portfolio,
			Sides.Sell,
			Volume,
			Volume, // Max order volume
			TimeSpan.Zero, // No timeout
			this, // Strategy implements ISubscriptionProvider
			this, // Strategy implements IMarketRuleContainer
			this, // Strategy implements ITransactionProvider
			this, // Strategy implements ITimeProvider
			this, // Strategy implements IMarketDataProvider
			IsFormedAndOnlineAndAllowTrading, // Check if trading is allowed
			true, // Use order book prices
			true  // Use last trade price if order book is empty
		)
		{
			Parent = this
		};

		// Log the creation of the new quoting processors
		this.AddInfoLog($"Created buy/sell spread at {CurrentTime}");

		// Subscribe to buy processor events for logging
		_buyProcessor.OrderRegistered += order =>
			this.AddInfoLog($"Buy order {order.TransactionId} registered at price {order.Price}");

		_buyProcessor.OrderFailed += fail =>
			this.AddInfoLog($"Buy order failed: {fail.Error.Message}");

		_buyProcessor.OwnTrade += trade =>
			this.AddInfoLog($"Buy trade executed: {trade.Trade.Volume} at {trade.Trade.Price}");

		_buyProcessor.Finished += isOk => {
			this.AddInfoLog($"Buy quoting finished with success: {isOk}");
			_buyProcessor?.Dispose();
			_buyProcessor = null;
		};

		// Subscribe to sell processor events for logging
		_sellProcessor.OrderRegistered += order =>
			this.AddInfoLog($"Sell order {order.TransactionId} registered at price {order.Price}");

		_sellProcessor.OrderFailed += fail =>
			this.AddInfoLog($"Sell order failed: {fail.Error.Message}");

		_sellProcessor.OwnTrade += trade =>
			this.AddInfoLog($"Sell trade executed: {trade.Trade.Volume} at {trade.Trade.Price}");

		_sellProcessor.Finished += isOk => {
			this.AddInfoLog($"Sell quoting finished with success: {isOk}");
			_sellProcessor?.Dispose();
			_sellProcessor = null;
		};

		// Start both processors
		_buyProcessor.Start();
		_sellProcessor.Start();
	}
}