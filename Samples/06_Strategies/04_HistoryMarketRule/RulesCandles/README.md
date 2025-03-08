# SimpleCandleRules Strategy Explanation

## Overview

This strategy is designed to monitor candle data and apply specific rules when conditions related to candle formation and trading volume are met. It uses the event-driven capabilities of StockSharp to perform actions based on real-time market data updates.

## Key Components

### Strategy Initialization

When the strategy starts, it subscribes to candle data for a specific timeframe and sets up market rules based on candle events:

```csharp
protected override void OnStarted(DateTimeOffset time)
{
    _subscription = new(Security.TimeFrame(TimeSpan.FromMinutes(5)))
    {
        // Configuration for market data handling can be specified here.
    };
    Subscribe(_subscription);

    var i = 0;
    var diff = "10%".ToUnit();

    this.WhenCandlesStarted(subscription)
        .Do((candle) =>
        {
            i++;

            this
                .WhenTotalVolumeMore(candle, diff)
                .Do((candle1) =>
                {
                    LogInfo($"The rule WhenCandlesStarted and WhenTotalVolumeMore candle={candle1}");
                    LogInfo($"The rule WhenCandlesStarted and WhenTotalVolumeMore i={i}");
                })
                .Once().Apply(this);

        }).Apply(this);

    base.OnStarted(time);
}
```

### Key Functionalities

- **Subscription to Candle Data**: The strategy subscribes to candle data with a specified timeframe. This sets the basis for monitoring the market at regular intervals defined by the candle duration.
  
- **Market Rules Setup**: 
  - **WhenCandlesStarted**: A rule triggered when a new candle starts. This can be used to perform initial checks or setups specific to each new candle.
  - **WhenTotalVolumeMore**: Nested within the `WhenCandlesStarted` rule, this rule triggers actions when the total volume of a candle exceeds a specified threshold (100,000 units in this case).

### Actions Based on Conditions

- The strategy logs information when the conditions specified by the market rules are met. In this context, it logs the occurrence and the sequence number of the candle that meets the volume condition.

### Strategy Execution Flow

1. **Candle Subscription**: The strategy begins by subscribing to a stream of candle data.
2. **Rule Activation**: As each candle starts, the strategy increments a counter and checks if the total trading volume of the candle exceeds a specified threshold.
3. **Logging**: If the conditions are met, the strategy logs relevant information, which could be used for debugging, monitoring, or decision-making purposes.

## Conclusion

The `SimpleCandleRules` strategy illustrates a basic implementation of rule-based trading actions within the StockSharp framework. This setup allows for real-time monitoring and response to specific market conditions, which is crucial for strategies that rely on volume and price action signals. The flexibility of the StockSharp's rule system enables the development of complex trading logic based on real-time data, providing a powerful tool for algorithmic trading.