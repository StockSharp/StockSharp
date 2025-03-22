# StairsCountertrend Strategy Explanation

## Overview

This strategy operates on the principle of identifying sequences of consecutive bullish or bearish candles. A "stair" is defined as a series of candles moving consistently in one direction (up for bullish, down for bearish). The strategy initiates trades in the opposite direction when these sequences reach a predetermined length, hypothesizing a potential reversal or pullback.

## Key Components

### Strategy Parameters

The strategy uses configurable parameters to allow for customization:

```csharp
private readonly StrategyParam<int> _length;
private readonly StrategyParam<DataType> _candleType;

public int Length
{
    get => _length.Value;
    set => _length.Value = value;
}

public DataType CandleType
{
    get => _candleType.Value;
    set => _candleType.Value = value;
}

public StairsCountertrendStrategy()
{
    _length = Param(nameof(Length), 3)
             .SetGreaterThanZero()
             .SetDisplay("Length", "Number of consecutive candles to trigger signal", "Strategy")
             .SetCanOptimize(true)
             .SetOptimize(2, 10, 1);

    _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                  .SetDisplay("Candle Type", "Type of candles to use", "General");
}
```

- **Length Parameter**: Defines how many consecutive candles in one direction will trigger a signal (default is 3).
- **Candle Type Parameter**: Sets the timeframe for candles (default is 5-minute).
- **Optimization Configuration**: The Length parameter is configured for optimization with a range from 2 to 10 with step 1.

### Working Securities

The strategy implements `GetWorkingSecurities` to clearly indicate which market data it requires:

```csharp
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
    return new[] { (Security, CandleType) };
}
```

This enables platforms like Designer to correctly initialize the required data sources.

### Strategy Initialization

When the strategy starts, it initializes counters and sets up subscriptions:

```csharp
protected override void OnStarted(DateTimeOffset time)
{
    base.OnStarted(time);
    
    // Reset counters
    _bullLength = 0;
    _bearLength = 0;

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

- **Counter Reset**: The bullish and bearish sequence counters are reset on startup.
- **High-Level Subscription API**: Uses StockSharp's high-level API for candle subscriptions.
- **Chart Visualization**: Automatically sets up visual elements if running in a graphical environment.

### Candle Processing

The core logic processes each candle to detect patterns and execute trades:

```csharp
private void ProcessCandle(ICandleMessage candle)
{
    // Check if candle is finished
    if (candle.State != CandleStates.Finished)
        return;

    // Check if strategy is ready to trade
    if (!IsFormedAndOnlineAndAllowTrading())
        return;

    // Update counters based on candle direction
    if (candle.OpenPrice < candle.ClosePrice)
    {
        // Bullish candle
        _bullLength++;
        _bearLength = 0;
    }
    else if (candle.OpenPrice > candle.ClosePrice)
    {
        // Bearish candle
        _bullLength = 0;
        _bearLength++;
    }

    // Countertrend strategy: 
    // Sell after Length consecutive bullish candles
    if (_bullLength >= Length && Position >= 0)
    {
        SellMarket(Volume + Math.Abs(Position));
    }
    // Buy after Length consecutive bearish candles
    else if (_bearLength >= Length && Position <= 0)
    {
        BuyMarket(Volume + Math.Abs(Position));
    }
}
```

- **Candle Validation**: Only processes finished candles.
- **Strategy Readiness Check**: Ensures indicators are formed, system is online, and trading is allowed.
- **Direction Tracking**: 
  - Increments the bullish counter and resets the bearish counter for a bullish candle.
  - Increments the bearish counter and resets the bullish counter for a bearish candle.
- **Trading Logic**:
  - **Sell Signal**: After `Length` consecutive bullish candles, if not already short.
  - **Buy Signal**: After `Length` consecutive bearish candles, if not already long.
- **Position Sizing**: Adjusts order volume to include current position size.

## Strategy Logic

The strategy's countertrend approach is based on the following principles:

1. **Pattern Recognition**: The strategy identifies trends by counting consecutive candles moving in the same direction.
2. **Mean Reversion Philosophy**: It assumes that after a certain number of movements in one direction, the price is more likely to revert.
3. **Adaptive Sensitivity**: The `Length` parameter determines how sensitive the strategy is to trend changes, with higher values requiring stronger trends before triggering a countertrend trade.
4. **Position Management**: The strategy can both enter new positions and add to existing ones when signals align with the current position.

## Conclusion

The `StairsCountertrend` strategy is an implementation of a countertrend trading approach that seeks to profit from potential price reversals after consistent directional moves. Its simplicity makes it accessible while the parameterization allows for customization to different market conditions and instruments.

Key advantages of this implementation include:
- Clean integration with StockSharp's high-level APIs
- Parameter-driven design with optimization support
- Automatic chart visualization
- Proper state management
- Simple yet effective pattern recognition

This strategy can be effective in range-bound or oscillating markets where price movements tend to revert after reaching extremes. However, it may underperform in strongly trending markets where countertrend signals can lead to premature entries against the dominant trend.