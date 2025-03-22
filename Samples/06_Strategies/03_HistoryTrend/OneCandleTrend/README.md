# OneCandleTrend Strategy Explanation

## Overview

This strategy is designed to capitalize on the momentum within each trading candle. If a candle closes higher than it opens, suggesting an upward trend, the strategy generates a buy order if the current position is not already long. Conversely, if a candle closes lower than it opens, indicating a downward trend, the strategy issues a sell order if the current position is not already short.

## Key Components

### Strategy Setup and Configuration

The strategy uses a parameter system to allow for easy configuration:

```csharp
private readonly StrategyParam<DataType> _candleType;

public DataType CandleType
{
    get => _candleType.Value;
    set => _candleType.Value = value;
}

public OneCandleTrendStrategy()
{
    _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                 .SetDisplay("Candle Type", "Type of candles to use", "General");
}
```

- **Candle Type Parameter**: This configurable parameter allows the user to set the desired timeframe (default is 5-minute candles).
- **Display Configuration**: The parameter is configured with a friendly name and description for UI display in platforms like Designer.

### Working Securities

The strategy implements the `GetWorkingSecurities` method to clearly indicate which instruments and data types it requires:

```csharp
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
    return new[] { (Security, CandleType) };
}
```

This helps platforms like Designer properly initialize the required market data.

### Strategy Start

When the strategy starts, it sets up data subscriptions and visualization components:

```csharp
protected override void OnStarted(DateTimeOffset time)
{
    base.OnStarted(time);

    // Create subscription
    var subscription = SubscribeCandles(CandleType);
    
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
}
```

- **High-Level API**: The strategy uses StockSharp's high-level API to simplify candle subscription and processing.
- **Visualization Setup**: The strategy automatically configures a chart area with candles and trade markers if a graphical interface is available.

### Candle Processing Logic

This is the core of the strategy, where candle data is analyzed and trading orders are generated based on the direction of the candle:

```csharp
private void ProcessCandle(ICandleMessage candle)
{
    // Check if candle is finished
    if (candle.State != CandleStates.Finished)
        return;

    // Check if strategy is ready to trade
    if (!IsFormedAndOnlineAndAllowTrading())
        return;

    // Trend following strategy: buy on bullish candle, sell on bearish candle
    if (candle.OpenPrice < candle.ClosePrice && Position <= 0)
    {
        // Bullish candle - buy
        BuyMarket(Volume + Math.Abs(Position));
    }
    else if (candle.OpenPrice > candle.ClosePrice && Position >= 0)
    {
        // Bearish candle - sell
        SellMarket(Volume + Math.Abs(Position));
    }
}
```

- **Candle Completion Check**: Ensures that the strategy only processes candles that have completed their formation.
- **Strategy Readiness Check**: Uses the combined check `IsFormedAndOnlineAndAllowTrading()` to ensure the strategy is in a proper state for trading (indicators formed, online data feed active, and trading allowed).
- **Trading Decisions**:
  - **Buy Condition**: If the candle closes higher than it opens (bullish) and the current position is not long (neutral or short), the strategy buys. This follows the momentum of the upward movement.
  - **Sell Condition**: If the candle closes lower than it opens (bearish) and the current position is not short (neutral or long), the strategy sells, following the downward momentum.
- **Position Scaling**: When entering a position, the strategy adjusts the order volume to include any existing position.

### Trading Operations

The strategy uses high-level trading operation methods for order execution:

- **BuyMarket**: Issues a market order to buy the specified volume, handling order creation and registration automatically.
- **SellMarket**: Issues a market order to sell the specified volume, with the same automatic handling.

These methods simplify the trading code by combining order creation and registration into a single call.

## Conclusion

The `OneCandleTrend` strategy exemplifies a simple yet effective approach to trend following in financial markets, using candlestick data to inform trading decisions. It relies on the basic premise that the direction in which a candle closes relative to its opening may indicate the immediate future direction of the market.

Key advantages of this implementation include:
- Clean, concise code using StockSharp's high-level APIs
- Proper parameter configuration for easy strategy customization
- Automatic chart visualization when available
- Comprehensive state checking before trading
- Simplified order execution through high-level methods

This strategy can serve as a foundational model for more complex systems that might include additional rules for risk management, entry/exit conditions, or combined indicators to refine the trading signals.