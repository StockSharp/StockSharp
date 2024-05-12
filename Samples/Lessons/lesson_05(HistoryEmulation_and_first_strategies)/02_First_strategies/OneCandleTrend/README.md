# OneCandleTrend Strategy Explanation

## Overview

This strategy is designed to capitalize on the momentum within each trading candle. If a candle closes higher than it opens, suggesting an upward trend, the strategy generates a buy order if the current position is not already long. Conversely, if a candle closes lower than it opens, indicating a downward trend, the strategy issues a sell order if the current position is not already short.

## Key Components

### Strategy Constructor

The constructor initializes the strategy with a specific `CandleSeries`, which defines the security and the timeframe for which the strategy should receive and process candle data.

```csharp
public OneCandleTrendStrategy(CandleSeries candleSeries)
{
    _candleSeries = candleSeries;
}
```

### Starting the Strategy

The `OnStarted` method subscribes to the `CandleProcessing` event and ensures that the strategy receives candle data for the specified series.

```csharp
protected override void OnStarted(DateTimeOffset time)
{
    Connector.CandleProcessing += CandleManager_Processing;
    this.SubscribeCandles(_candleSeries);
    base.OnStarted(time);
}
```

### Candle Processing Logic

This is the core of the strategy, where candle data is analyzed and trading orders are generated based on the direction of the candle:

```csharp
private void CandleManager_Processing(CandleSeries candleSeries, ICandleMessage candle)
{
    if (candle.State != CandleStates.Finished) return;

    if (candle.OpenPrice < candle.ClosePrice && Position <= 0)
    {
        RegisterOrder(this.BuyAtMarket(Volume + Math.Abs(Position)));
    }
    else if (candle.OpenPrice > candle.ClosePrice && Position >= 0)
    {
        RegisterOrder(this.SellAtMarket(Volume + Math.Abs(Position)));
    }
}
```

- **Candle Completion Check**: Ensures that the strategy only processes candles that have completed their formation.
- **Trading Decisions**:
  - **Buy Condition**: If the candle closes higher than it opens and the current position is not long (i.e., neutral or short), the strategy buys. This is based on the assumption that the upward trend might continue.
  - **Sell Condition**: If the candle closes lower than it opens and the current position is not short (i.e., neutral or long), the strategy sells, anticipating a continuation of the downward trend.

## Conclusion

The `OneCandleTrend` strategy exemplifies a simple yet effective approach to trend following in financial markets, using candlestick data to inform trading decisions. It relies on the basic premise that the direction in which a candle closes relative to its opening may indicate the immediate future direction of the market. This strategy can serve as a foundational model for more complex systems that might include additional rules for risk management, entry/exit conditions, or combined indicators to refine the trading signals.