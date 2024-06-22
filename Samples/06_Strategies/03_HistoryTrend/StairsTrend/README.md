# StairsTrend Strategy Explanation

## Overview

This strategy operates by tracking consecutive bullish and bearish candle sequences. If a certain number (defined by `Length`) of consecutive bullish or bearish candles is observed, the strategy assumes the trend will continue in the same direction and takes a position accordingly.

## Key Components

### Strategy Constructor

Initializes the strategy with a specific `CandleSeries`, which defines the security and the timeframe for candle data:

```csharp
public StairsTrendStrategy(CandleSeries candleSeries)
{
    _candleSeries = candleSeries;
}
```

### Starting the Strategy

When the strategy starts, it subscribes to the `CandleProcessing` event to receive updates on candle completions:

```csharp
protected override void OnStarted(DateTimeOffset time)
{
    Connector.CandleProcessing += CandleManager_Processing;
    Connector.SubscribeCandles(_candleSeries);
    base.OnStarted(time);
}
```

### Candle Processing Logic

This method is the core of the strategy, where the direction of each candle is assessed and trades are potentially triggered:

```csharp
private void CandleManager_Processing(CandleSeries candleSeries, ICandleMessage candle)
{
    if (candle.State != CandleStates.Finished) return;

    if (candle.OpenPrice < candle.ClosePrice)
    {
        _bullLength++;
        _bearLength = 0;
    }
    else if (candle.OpenPrice > candle.ClosePrice)
    {
        _bullLength = 0;
        _bearLength++;
    }

    if (_bullLength >= Length && Position <= 0)
    {
        RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
    }
    else if (_bearLength >= Length && Position >= 0)
    {
        RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
    }
}
```

- **Direction Determination**: Counts consecutive bullish and bearish candles.
- **Trading Conditions**:
  - **Buy**: If there are `Length` or more consecutive bullish candles and the current position is not long, a buy order is placed.
  - **Sell**: Conversely, if there are `Length` or more consecutive bearish candles and the current position is not short, a sell order is placed.

## Strategy Logic

- The strategy assumes that the market will continue to move in the direction of the trend if a certain number of consecutive candles close in the same direction.
- This trend-following approach is simple but effective, especially in markets or instruments that exhibit strong trending behaviors.
- The `Length` property is critical as it determines the sensitivity of the strategy to price changes. A higher value may result in fewer, but potentially more reliable, trades, while a lower value may increase trading frequency but with a higher risk of false positives.

## Conclusion

The `StairsTrend` strategy provides a foundational example of how to implement a trend-following strategy using the StockSharp library. It is particularly suited for traders who prefer to capitalize on the continuation of established trends rather than anticipating market reversals. The strategy’s effectiveness can be enhanced by integrating additional filters such as volume analysis or incorporating stop-loss and take-profit orders to manage risks more effectively.