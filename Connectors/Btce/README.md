# BTC-e (WEX) Connector for StockSharp

## Overview

This project contains the source code of the **BtceMessageAdapter**, a connector used to interact with the BTC‑e crypto exchange (later renamed to WEX). The adapter is implemented as part of the StockSharp trading framework and provides both real‑time market data and transactional capabilities.

## Features

- **Market Data**: Receives ticks, order books and level 1 information through a WebSocket connection. Historical data can be downloaded via the REST API.
- **Trading Operations**: Supports registration and cancellation of limit orders. Conditional orders are used to perform withdrawals.
- **Portfolio Information**: The adapter retrieves account balances and updates positions based on trades and wallet changes.
- **Configurable Connection**: API access is configured with a key and secret as well as a domain address (default value is `https://wex.nz/`).
- **Pusher WebSocket Client**: Real‑time market updates are delivered by the internal `PusherClient` class located under `Native`.

## File Structure

- `BtceMessageAdapter.cs` – core adapter implementation and initialization logic.
- `BtceMessageAdapter_MarketData.cs` – market data subscription handling.
- `BtceMessageAdapter_Transaction.cs` – order registration, cancellations and portfolio management.
- `BtceMessageAdapter_Settings.cs` – adapter settings including API key, secret and heartbeat interval.
- `BtceOrderCondition.cs` – order condition class for withdrawal operations.
- `Native/` – low level protocol helpers such as the REST `HttpClient` and WebSocket `PusherClient`.

## Installation

1. Add the StockSharp connectors to your solution via the [StockSharp NuGet feed](https://stocksharp.com/products/nuget_manual/).
2. Include the `Btce` project or compiled assembly in your application.
3. Reference `BtceMessageAdapter` from your trading connector.

## Usage Example

```csharp
var connector = new Connector();
var adapter = new BtceMessageAdapter(connector.TransactionIdGenerator)
{
    Key = "YOUR_API_KEY".ToSecureString(),
    Secret = "YOUR_SECRET".ToSecureString(),
    Address = BtceMessageAdapter.DefaultDomain,
};

connector.Adapter.InnerAdapters.Add(adapter);
connector.Connect();

// request available instruments
connector.LookupSecurities(new SecurityLookupMessage());

// place a limit order
await connector.RegisterOrderAsync(new OrderRegisterMessage
{
    SecurityId = "BTC/USD".ToSecurityId(BoardCodes.Btce),
    Side = Sides.Buy,
    OrderType = OrderTypes.Limit,
    Price = 650m,
    Volume = 0.1m,
});
```

Withdrawal can be performed by registering an order with a `BtceOrderCondition` instance containing the required withdrawal information.

## Documentation

StockSharp documentation provides general instructions on working with message adapters: [Creating your own connector](https://doc.stocksharp.com/topics/api/connectors/creating_own_connector.html).

## Support

Questions and discussion can be posted in the [StockSharp chat](https://t.me/stocksharpchat/361).
