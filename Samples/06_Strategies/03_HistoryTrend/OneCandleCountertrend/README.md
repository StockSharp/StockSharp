# OneCandleCountertrend Strategy Explanation

## Overview

This strategy aims to take advantage of small reversals or "countertrend" moves in the market by issuing buy or sell orders based on the closing conditions of individual candles compared to their openings. The logic assumes that if a candle closes higher than it opens, the next move might be downward, and vice versa.

## Key Components

### Strategy Setup and Configuration

The strategy uses a parameter system to allow for configuration:

```csharp
private readonly StrategyParam<DataType> _candleType;

public DataType CandleType
{
    get => _candleType.Value;
    set => _candleType.Value = value;
}

public OneCandleCountertrendStrategy()
{
    _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                 .SetDisplay("Candle Type", "Type of candles to use", "General");
}
```

- **Candle Type Parameter**: This allows the timeframe to be configured (default is 5-minute candles).
- **Display Configuration**: The parameter is configured with a friendly name and description for UI display.

### Working Securities

The strategy implements the `GetWorkingSecurities` method to clearly indicate which instruments and data types it requires:

```csharp
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
    return new[] { (Security, CandleType) };
}
```

This helps platforms like Designer properly initialize the required data.

### Strategy Start

When the strategy starts, it sets up subscriptions and visualization:

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

The core logic evaluates each candle once it is complete:

```csharp
private void ProcessCandle(ICandleMessage candle)
{
    // Check if candle is finished
    if (candle.State != CandleStates.Finished)
        return;

    // Check if strategy is ready to trade
    if (!IsFormedAndOnlineAndAllowTrading())
        return;

    // Countertrend strategy: buy on bearish candle, sell on bullish candle
    if (candle.OpenPrice < candle.ClosePrice && Position >= 0)
    {
        // Bullish candle - sell
        SellMarket(Volume + Math.Abs(Position));
    }
    else if (candle.OpenPrice > candle.ClosePrice && Position <= 0)
    {
        // Bearish candle - buy
        BuyMarket(Volume + Math.Abs(Position));
    }
}
```

- **Candle Completion Check**: Only processes candles that have finished forming.
- **Strategy Readiness Check**: Uses the combined check `IsFormedAndOnlineAndAllowTrading()` to ensure the strategy is in a proper state for trading.
- **Countertrend Logic**: 
  - If the candle is bullish (close > open) and the position is long or neutral, it sells.
  - If the candle is bearish (open > close) and the position is short or neutral, it buys.
- **Position Scaling**: When entering a position, the strategy adjusts the order volume to include any existing position.

### Trading Operations

The strategy uses high-level trading operation methods for order execution:

- **SellMarket**: Issues a market order to sell the specified volume.
- **BuyMarket**: Issues a market order to buy the specified volume.

These methods automatically handle order creation and registration, simplifying the code.

## Conclusion

The `OneCandleCountertrend` strategy demonstrates how to implement a reactive trading mechanism based on candlestick data in StockSharp. It showcases the use of high-level APIs for market data subscription, candle processing, and order execution.

Key advantages of this implementation include:
- Clean, concise code using StockSharp's high-level APIs
- Proper parameter configuration for strategy customization
- Automatic chart visualization setup when available
- Comprehensive state checking before trading
- Automatic subscription management

This strategy serves as a good foundation that can be extended with more complex rules or incorporated into a larger trading system with multiple strategies.