# SimpleTradeRules Strategy Explanation

## Overview

This strategy listens for tick trades for a specific security and sets up a compound market rule. This rule triggers actions based on the last trade price either rising above or falling below specified price points. The goal is to log information when these conditions are met, which can be critical for strategies sensitive to specific price thresholds.

## Strategy Initialization and Rule Setup

### OnStarted Method
Upon starting, the strategy subscribes to trade ticks for the specified security and sets up a compound rule to monitor trade prices:

```csharp
protected override void OnStarted(DateTimeOffset time)
{
    var sub = new Subscription(DataType.Ticks, Security);

    sub.WhenTickTradeReceived(this).Do(() =>
    {
        new IMarketRule[] { Security.WhenLastTradePriceMore(this, 2), Security.WhenLastTradePriceLess(this, 2) }
            .Or() // or conditions (WhenLastTradePriceMore or WhenLastTradePriceLess)
            .Do(() =>
            {
                LogInfo($"The rule WhenLastTradePriceMore Or WhenLastTradePriceLess candle={Security.LastTick}");
            })
            .Apply(this);
    })
    .Once() // call this rule only once
    .Apply(this);

    // Sending request for subscribe to market data.
    Subscribe(sub);

    base.OnStarted(time);
}
```

### Key Functionalities

- **Trade Subscription**: The strategy begins by subscribing to tick trade updates for the `Security`.
- **Rule Combination Using Or**: It creates an array of `IMarketRule`, combining two rules:
  - `WhenLastTradePriceMore(Connector, 2)`: Triggers if the last trade price exceeds the value 2.
  - `WhenLastTradePriceLess(Connector, 2)`: Triggers if the last trade price falls below the value 2.
  
  These rules are combined using `.Or()`, which means the composite rule triggers if either condition is met.
- **Action Execution**: Upon triggering, the rule logs the last tick (trade) details. This can be essential for monitoring significant price movements and responding accordingly.
- **Rule Application**: The rule is set to trigger only once (`Once()`) and then it applies itself to the strategy. This setup is suitable for scenarios where the rule needs to evaluate a specific condition that is expected to occur but not repeatedly.

## Usage and Implications

This strategy is particularly useful in trading scenarios where price thresholds play a critical role in decision-making:
- **Event-Driven Trading**: For strategies that need to execute or exit trades based on specific price points.
- **Alert Systems**: Can be used to alert traders or automated systems of critical price movements that might require immediate attention.
- **Market Analysis**: Helps in analyzing the market response when key price levels are breached.

## Conclusion

The `SimpleTradeRules` strategy effectively demonstrates the power and flexibility of StockSharp's market rule system, allowing for sophisticated and responsive trading logic based on live market data. This approach enhances the strategy's ability to adapt and react to market conditions, providing a robust framework for building complex trading systems that can operate autonomously or assist human traders in decision-making processes.