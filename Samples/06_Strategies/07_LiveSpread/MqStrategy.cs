namespace StockSharp.Samples.Strategies.LiveSpread;

using System;

using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.Messages;

/// <summary>
/// Market quoting strategy using QuotingProcessor.
/// </summary>
public class MqStrategy : Strategy
{
	private readonly StrategyParam<MarketPriceTypes> _priceType;
	private readonly StrategyParam<Unit> _priceOffset;
	private readonly StrategyParam<Unit> _bestPriceOffset;

	private QuotingProcessor _quotingProcessor;

	/// <summary>
	/// Initializes a new instance of <see cref="MqStrategy"/>.
	/// </summary>
	public MqStrategy()
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
		Connector_CurrentTimeChanged(default);
	}

	/// <summary>
	/// On strategy stopped.
	/// </summary>
	protected override void OnStopped()
	{
		// Unsubscribe to prevent memory leaks
		Connector.CurrentTimeChanged -= Connector_CurrentTimeChanged;

		// Dispose the current processor if it exists
		_quotingProcessor?.Dispose();
		_quotingProcessor = null;

		base.OnStopped();
	}

	private void Connector_CurrentTimeChanged(TimeSpan obj)
	{
		// Only create a new processor if the current one is stopped or doesn't exist
		if (_quotingProcessor != null && _quotingProcessor.LeftVolume > 0)
			return;

		// Dispose the old processor if it exists
		_quotingProcessor?.Dispose();
		_quotingProcessor = null;

		// Determine the quoting side based on current position
		var side = Position <= 0 ? Sides.Buy : Sides.Sell;

		// Create a new quoting behavior
		var behavior = new MarketQuotingBehavior(
			PriceOffset,
			BestPriceOffset,
			PriceType
		);

		// Calculate quoting volume
		var quotingVolume = Volume + Math.Abs(Position);

		// Create and initialize the processor
		_quotingProcessor = new QuotingProcessor(
			behavior,
			Security,
			Portfolio,
			side,
			quotingVolume,
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

		// Log the creation of the new quoting processor
		this.AddInfoLog($"Created {side} quoting processor for {quotingVolume} at {CurrentTime}");

		// Subscribe to processor events for logging
		_quotingProcessor.OrderRegistered += order =>
			this.AddInfoLog($"Order {order.TransactionId} registered at price {order.Price}");

		_quotingProcessor.OrderFailed += fail =>
			this.AddInfoLog($"Order failed: {fail.Error.Message}");

		_quotingProcessor.OwnTrade += trade =>
			this.AddInfoLog($"Trade executed: {trade.Trade.Volume} at {trade.Trade.Price}");

		_quotingProcessor.Finished += isOk => {
			this.AddInfoLog($"Quoting finished with success: {isOk}");
			_quotingProcessor?.Dispose();
			_quotingProcessor = null;
		};

		// Start the processor
		_quotingProcessor.Start();
	}
}