# SimpleOrderRules Strategy Explanation

## Overview

This strategy is designed to execute orders immediately upon receiving trade ticks and to manage responses to those orders through StockSharp's rule system. The strategy sets up rules to log messages based on whether the orders are successfully registered or if they fail to register.

## Strategy Initialization

### Subscription to Tick Trades

The strategy starts by subscribing to trade ticks for the specified security. This sets the foundation for triggering actions based on live market data:

```csharp
var sub = new Subscription(DataType.Ticks, Security);

Subscribe(sub);
```

### Order Execution and Rule Definition

For each tick trade received, the strategy attempts to execute two orders with specific rules attached to manage their registration statuses:

```csharp
sub.WhenTickTradeReceived(Connector).Do(() =>
{
    var order = this.BuyAtMarket(1); // Attempt to place a small market order
    SetupOrderRules(order, "Order ¹1");
}).Once().Apply(this);

sub.WhenTickTradeReceived(Connector).Do(() =>
{
    var order = this.BuyAtMarket(10000000); // Attempt to place a very large market order
    SetupOrderRules(order, "Order ¹2");
}).Once().Apply(this);
```

### Order Rule Setup

This method encapsulates the logic for creating and applying rules related to order registration and failure scenarios:

```csharp
void SetupOrderRules(Order order, string orderName)
{
    var ruleReg = order.WhenRegistered(Connector);
    var ruleRegFailed = order.WhenRegisterFailed(Connector);

    ruleReg
        .Do(() => LogInfo($"{orderName} Registered"))
        .Once()
        .Apply(this)
        .Exclusive(ruleRegFailed);

    ruleRegFailed
        .Do(() => LogInfo($"{orderName} RegisterFailed"))
        .Once()
        .Apply(this)
        .Exclusive(ruleReg);

    RegisterOrder(order);
}
```

- **Rules for Order Registration**: Checks if the order is registered successfully and logs a corresponding message. The rule is set to trigger only once and is mutually exclusive with the failure rule to ensure that only one outcome is processed.
- **Rules for Order Registration Failure**: Monitors if the order fails to register and logs a message if it does. This rule is also set to trigger only once and is mutually exclusive with the success rule.

## Strategy Logic Flow

1. **Receive Tick Trade**: Each tick triggers the creation and registration of an order.
2. **Order Processing**: Orders are submitted to the market.
3. **Rule Application**: Depending on whether the order is successfully registered or encounters a registration failure, an appropriate log message is added.

## Conclusion

The `SimpleOrderRules` strategy utilizes StockSharp's advanced features for handling real-time market data and order management effectively. By defining explicit actions for different outcomes of order submissions, the strategy provides a robust mechanism for real-time trading oversight. This approach is particularly useful in high-frequency trading environments where the ability to respond quickly to order execution statuses is crucial. This example can be extended or modified to handle more complex trading scenarios, apply different trading strategies, or integrate risk management protocols.