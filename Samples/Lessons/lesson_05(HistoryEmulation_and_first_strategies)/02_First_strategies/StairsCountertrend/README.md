# StairsCountertrend Strategy Explanation

## Overview

This strategy operates on the principle of identifying sequences of consecutive bullish or bearish candles. A "stair" is defined as a series of candles moving consistently in one direction (up for bullish, down for bearish). The strategy initiates trades in the opposite direction when these sequences reach a predetermined length, hypothesizing a potential reversal or pullback.

## Key Components

### Constructor

The strategy is initialized with a specific `CandleSeries`, setting the context for which securities and candle timeframe it will operate on:

```csharp
public StairsCountertrendStrategy(CandleSeries candleSeries)
{
    _candleSeries = candleSeries;
}
```

### Strategy Start

When the strategy starts, it subscribes to the `CandleProcessing` event and begins listening for candle updates to process:

```csharp
protected override void OnStarted(DateTimeOffset time)
{
    Connector.CandleProcessing += CandleManager_Processing;
    this.SubscribeCandles(_candleSeries);
    base.OnStarted(time);
}
```

### Candle Processing

The core of the strategy, where each candle's opening and closing prices are compared to determine its direction and count consecutive trends:

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

    if (_bullLength >= Length && Position >= 0)
    {
        RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
    }
    else if (_bearLength >= Length && Position <= 0)
    {
        RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
    }
}
```

- **Direction Assessment**: Increments count for bullish or bearish sequences based on candle closing higher or lower than the opening price.
- **Trade Execution**:
  - **Sell**: If there are at least `Length` consecutive bullish candles and the strategy's position is not short, it places a market sell order, anticipating a downtrend reversal.
  - **Buy**: Conversely, if there are at least `Length` consecutive bearish candles and the position is not long, it places a market buy order, anticipating an uptrend reversal.

## Strategy Logic

- The strategy monitors trends by counting consecutive candles moving in the same direction.
- It assumes that after a certain number of movements in one direction (`Length`), the price is more likely to reverse, which triggers countertrend trades.
- The `Length` property defines the sensitivity of the strategy, with higher values making the strategy slower to react (potentially safer but less responsive).

## Conclusion

The `StairsCountertrend` strategy is a straightforward implementation of a countertrend trading strategy that aims to capitalize on perceived overextensions in price movements. By monitoring consecutive bullish or bearish candles, it attempts to enter the market when a potential reversal is due, thus aiming to profit from the subsequent price corrections. This strategy is especially useful in markets where overextensions and retracements are common, but it requires careful tuning of the `Length` parameter to balance responsiveness with risk.