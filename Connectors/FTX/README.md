# FTX Connector for StockSharp

This folder contains the source code of the **FTX** connector for the [StockSharp](https://github.com/StockSharp/StockSharp) trading platform. The connector allows applications built on top of S# to interact with the former FTX cryptocurrency exchange via both REST and WebSocket APIs.

## Features

- **Market data**
  - Level1 quotes, order books, and trade ticks delivered through WebSocket streams.
  - Time frame candles requested via REST and streamed via WebSocket.
  - Supported candle time frames: 15&nbsp;s, 1&nbsp;min, 5&nbsp;min, 15&nbsp;min, 1&nbsp;hour, 4&nbsp;hours, and 1&nbsp;day.
  - Order book depth up to 100 levels.
  - Security lookup through the REST API.
- **Trading operations**
  - Placing and cancelling market and limit orders.
  - Order status subscriptions with automatic retrieval of active and historical orders.
  - Portfolio and position information for the main account or a specified sub‑account.
- **Connectivity**
  - Asynchronous message adapter built on top of `AsyncMessageAdapter`.
  - Combined REST client for historical data and WebSocket client for real‑time updates.
  - Connection heartbeat interval of one second by default.

## Configuration

The adapter is configured through the following properties:

- `Key` – API key issued by FTX.
- `Secret` – API secret corresponding to the key.
- `SubaccountName` – optional sub‑account name for trading and data requests.

These values can be saved to and loaded from a `SettingsStorage` object. When using transactional features (order management or portfolio information) the key and secret must be specified.

## Usage

Create an instance of `FtxMessageAdapter` and pass it to `Connector` or another component that consumes message adapters:

```csharp
var adapter = new FtxMessageAdapter(transactionIdGenerator)
{
    Key = mySecureApiKey,
    Secret = mySecureApiSecret,
    SubaccountName = "optional-subaccount"
};
```

Once connected, subscribe to the desired market data or send order messages according to the StockSharp API. See the source files in this directory for implementation details.

## Notes

FTX filed for bankruptcy in 2022 and the exchange is no longer operational. The connector is provided for reference and historical purposes.
