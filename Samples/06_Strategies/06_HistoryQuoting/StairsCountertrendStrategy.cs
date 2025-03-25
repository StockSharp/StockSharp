namespace StockSharp.Samples.Strategies;

using System;
using System.Collections.Generic;

using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Stairs countertrend strategy that takes positions against established trends.
/// </summary>
public class StairsCountertrendStrategy : Strategy
{
	private readonly StrategyParam<DataType> _candleDataType;
	private readonly StrategyParam<int> _length;
	private QuotingProcessor _quotingProcessor;

	private int _bullLength;
	private int _bearLength;

	/// <summary>
	/// Initializes a new instance of <see cref="StairsCountertrendStrategy"/>.
	/// </summary>
	public StairsCountertrendStrategy()
	{
		_candleDataType = Param(nameof(CandleDataType), TimeSpan.FromMinutes(1).TimeFrame())
			.SetDisplay("Candle Type", "Timeframe for strategy calculation", "Base settings");

		_length = Param(nameof(Length), 5)
			.SetGreaterThanZero()
			.SetDisplay("Trend Length", "Number of consecutive candles to identify a trend", "Base settings")
			.SetCanOptimize(true)
			.SetOptimize(2, 10, 1);
	}

	/// <summary>
	/// Candle data type for subscription.
	/// </summary>
	public DataType CandleDataType
	{
		get => _candleDataType.Value;
		set => _candleDataType.Value = value;
	}

	/// <summary>
	/// Number of consecutive candles required to identify a trend.
	/// </summary>
	public int Length
	{
		get => _length.Value;
		set => _length.Value = value;
	}

	/// <summary>
	/// Returns list of strategy's working securities and data types.
	/// </summary>
	public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
	{
		return new[] { (Security, CandleDataType) };
	}

	/// <summary>
	/// On strategy started.
	/// </summary>
	protected override void OnStarted(DateTimeOffset time)
	{
		// Reset counters on start
		_bullLength = 0;
		_bearLength = 0;

		// Create subscription for candles
		var subscription = SubscribeCandles(CandleDataType);

		subscription
			.Bind(ProcessCandle)
			.Start();

		// Setup chart visualization if available
		var area = CreateChartArea();
		if (area != null)
		{
			DrawCandles(area, subscription);
			DrawOwnTrades(area);
		}

		base.OnStarted(time);
	}

	/// <summary>
	/// Process a new candle.
	/// </summary>
	private void ProcessCandle(ICandleMessage candle)
	{
		if (candle.State != CandleStates.Finished)
			return;

		// Identify bullish or bearish candle
		if (candle.OpenPrice < candle.ClosePrice)
		{
			_bullLength++;
			_bearLength = 0;

			this.AddInfoLog($"Bullish candle detected. Streak: {_bullLength}");
		}
		else if (candle.OpenPrice > candle.ClosePrice)
		{
			_bullLength = 0;
			_bearLength++;

			this.AddInfoLog($"Bearish candle detected. Streak: {_bearLength}");
		}

		// Stop existing processor if we need to change direction
		if (_quotingProcessor != null)
		{
			// Check if we need to clear the processor (change in trend or position change)
			var shouldClearProcessor = false;

			// Need to sell if bullish trend and no short position
			if (_bullLength >= Length && Position >= 0)
				shouldClearProcessor = true;
			// Need to buy if bearish trend and no long position
			else if (_bearLength >= Length && Position <= 0)
				shouldClearProcessor = true;

			if (shouldClearProcessor)
			{
				_quotingProcessor?.Dispose();
				_quotingProcessor = null;
			}
		}

		// Create new quoting processor if needed and we're allowed to trade
		if (_quotingProcessor == null && IsFormedAndOnlineAndAllowTrading())
		{
			if (_bullLength >= Length && Position >= 0)
			{
				// Bearish trend - go short
				CreateQuotingProcessor(Sides.Sell);
				this.AddInfoLog($"Starting SELL quoting after {_bullLength} bullish candles");
			}
			else if (_bearLength >= Length && Position <= 0)
			{
				// Bullish trend - go long
				CreateQuotingProcessor(Sides.Buy);
				this.AddInfoLog($"Starting BUY quoting after {_bearLength} bearish candles");
			}
		}
	}

	/// <summary>
	/// Creates a new quoting processor with the specified direction.
	/// </summary>
	private void CreateQuotingProcessor(Sides side)
	{
		// Create behavior for market quoting
		var behavior = new MarketQuotingBehavior(
			0, // No price offset
			new Unit(0.1m, UnitTypes.Percent), // Use 0.1% as minimum deviation
			MarketPriceTypes.Following // Follow the market price
		);

		// Create quoting processor
		_quotingProcessor = new(
			behavior,
			Security,
			Portfolio,
			side,
			Volume, // Quoting volume
			Volume, // Max order volume
			TimeSpan.Zero, // No timeout
			this, // Strategy implements ISubscriptionProvider
			this, // Strategy implements IMarketRuleContainer
			this, // Strategy implements ITransactionProvider
			this, // Strategy implements ITimeProvider
			this, // Strategy implements IMarketDataProvider
			IsFormedAndOnlineAndAllowTrading, // Check if trading is allowed
			true, // Use order book prices
			true // Use last trade price if order book is empty
		)
		{
			Parent = this
		};

		// Subscribe to processor events
		_quotingProcessor.OrderRegistered += order =>
			this.AddInfoLog($"Order {order.TransactionId} registered at price {order.Price}");

		_quotingProcessor.OrderFailed += fail =>
			this.AddInfoLog($"Order failed: {fail.Error.Message}");

		_quotingProcessor.OwnTrade += trade =>
			this.AddInfoLog($"Trade executed: {trade.Trade.Volume} at {trade.Trade.Price}");

		_quotingProcessor.Finished += isOk =>
		{
			_quotingProcessor?.Dispose();
			_quotingProcessor = null;
		};

		// Initialize the processor
		_quotingProcessor.Start();
	}

	/// <summary>
	/// Clean up resources on stop.
	/// </summary>
	protected override void OnStopped()
	{
		_quotingProcessor?.Dispose();
		_quotingProcessor = null;
		base.OnStopped();
	}
}