# StairsTrend Strategy Explanation

## Overview

This strategy operates by tracking consecutive bullish and bearish candle sequences. If a certain number (defined by `Length`) of consecutive bullish or bearish candles is observed, the strategy assumes the trend will continue in the same direction and takes a position accordingly. This is a trend-following approach that aims to capitalize on momentum in the market.

## Key Components

### Strategy Parameters

The strategy uses configurable parameters to allow for customization:

```csharp
private readonly StrategyParam<int> _lengthParam;
private readonly StrategyParam<DataType> _candleType;

public int Length
{
    get => _lengthParam.Value;
    set => _lengthParam.Value = value;
}

public DataType CandleType
{
    get => _candleType.Value;
    set => _candleType.Value = value;
}

public StairsTrendStrategy()
{
    _lengthParam = Param(nameof(Length), 3)
                   .SetGreaterThanZero()
                   .SetDisplay("Length", "Number of consecutive candles to trigger signal", "Strategy")
                   .SetCanOptimize(true)
                   .SetOptimize(2, 10, 1);

    _candleType = Param(nameof(CandleType), DataType.TimeFrame(TimeSpan.FromMinutes(5)))
                  .SetDisplay("Candle Type", "Type of candles to use", "General");
}
```

- **Length Parameter**: Defines how many consecutive candles in one direction will trigger a signal (default is 3). This parameter can be optimized in a range from 2 to 10 with a step of 1.
- **Candle Type Parameter**: Sets the timeframe for candles (default is 5-minute).
- **Parameter Validation**: The Length parameter is validated to ensure it's greater than zero.

### Working Securities

The strategy implements `GetWorkingSecurities` to clearly indicate which instruments and data types it requires:

```csharp
public override IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities()
{
    return new[] { (Security, CandleType) };
}
```

This helps platforms like Designer properly initialize the required market data.

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
- **High-Level Subscription API**: Uses StockSharp's high-level API for candle subscriptions and processing.
- **Chart Visualization**: Automatically sets up visual elements if running in a graphical environment.

### Candle Processing Logic

This method is the core of the strategy, where the direction of each candle is assessed and trades are potentially triggered:

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

    // Trend following strategy: 
    // Buy after Length consecutive bullish candles
    if (_bullLength >= Length && Position <= 0)
    {
        BuyMarket(Volume + Math.Abs(Position));
    }
    // Sell after Length consecutive bearish candles
    else if (_bearLength >= Length && Position >= 0)
    {
        SellMarket(Volume + Math.Abs(Position));
    }
}
```

- **Candle Validation**: Only processes finished candles.
- **Strategy Readiness Check**: Ensures indicators are formed, system is online, and trading is allowed.
- **Direction Tracking**: 
  - Increments the bullish counter and resets the bearish counter for a bullish candle.
  - Increments the bearish counter and resets the bullish counter for a bearish candle.
- **Trading Logic**:
  - **Buy Signal**: After `Length` consecutive bullish candles, if not already long.
  - **Sell Signal**: After `Length` consecutive bearish candles, if not already short.
- **Position Sizing**: Adjusts order volume to include current position size.

## Strategy Logic

The strategy's trend-following approach is based on the following principles:

1. **Momentum Recognition**: The strategy identifies trend momentum by counting consecutive candles moving in the same direction.
2. **Trend Continuation Assumption**: It assumes that after a certain number of movements in one direction, the trend is likely to continue in that direction.
3. **Adaptive Sensitivity**: The `Length` parameter determines how many consecutive candles are required to confirm a trend, allowing adjustment based on market volatility and characteristics.
4. **Position Management**: The strategy can both enter new positions and add to existing ones when signals align with the current position.

## Conclusion

The `StairsTrend` strategy is an implementation of a trend-following approach that seeks to profit from momentum in directional market moves. Its simplicity makes it accessible while the parameterization allows for customization to different market conditions and instruments.

Key advantages of this implementation include:
- Clean integration with StockSharp's high-level APIs
- Parameter-driven design with optimization support
- Automatic chart visualization
- Proper state management
- Simple yet effective pattern recognition

This strategy can be particularly effective in trending markets where price movements tend to exhibit momentum. However, it may underperform in choppy or range-bound markets where false signals can lead to whipsaw losses. Consider combining it with additional trend confirmation indicators or using it within a broader market context analysis.