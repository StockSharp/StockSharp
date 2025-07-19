# Bitalong Connector

This directory contains the source code for the **Bitalong** connector used by the [StockSharp](https://github.com/StockSharp/StockSharp) trading platform. The connector implements an `AsyncMessageAdapter` which provides access to market data and trading operations on the Bitalong cryptocurrency exchange.

## Features

- Connection via REST/WebSocket using the `BitalongMessageAdapter` class.
- Level1, tick, and order book market data subscriptions.
- Order management (new, cancel, cancel all) and withdrawal requests.
- Portfolio and balance updates with configurable check interval.
- Automatic conversion between StockSharp data types and Bitalong REST API types located in the `Native` subfolder.


## Configuration

`BitalongMessageAdapter` exposes several properties used to configure the connection:

| Property | Description |
|----------|-------------|
| **Key** | API key issued by Bitalong. Required for trading operations. |
| **Secret** | API secret used to sign requests. |
| **Address** | Exchange domain name. Defaults to `bitalong.com`. |
| **BalanceCheckInterval** | Periodic account balance check interval. Useful when deposits or withdrawals occur. |

### Using in code

```csharp
var adapter = new BitalongMessageAdapter(new IncrementalIdGenerator())
{
    Key = "<api-key>".ToSecureString(),
    Secret = "<api-secret>".ToSecureString(),
    BalanceCheckInterval = TimeSpan.FromMinutes(5)
};
```

The adapter can then be registered in your `Connector` or passed directly to S# components that work with message adapters.

## Folder structure

- `BitalongMessageAdapter*.cs` – implementation split into settings, market data, and transaction logic.
- `BitalongOrderCondition.cs` – custom order condition for withdrawal operations.
- `Native` – lightweight wrappers for the exchange REST API including request helpers and model classes.


