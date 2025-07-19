# Coinbase Connector

This folder contains the **Coinbase** connector for the [StockSharp](https://github.com/StockSharp/StockSharp) platform. The implementation provides both market data and transactional access to the Coinbase Advanced Trade API. It can be used as a reference for creating your own connectors or as a ready‑to‑use adapter in your trading application.

## Features

- Real‑time data streaming through a websocket connection.
- Historical data and trading operations via REST.
- Supports ticks, order book updates, Level1 information and time frame candles.
- Order support for market, limit and stop orders.
- Withdraw operations via a custom order condition.

## Usage

1. Obtain an API **Key**, **Secret** and **Passphrase** from your Coinbase account.
2. Create a `CoinbaseMessageAdapter` and assign your credentials:

```csharp
var adapter = new CoinbaseMessageAdapter(new IncrementalIdGenerator())
{
    Key = "YOUR_KEY".ToSecureString(),
    Secret = "YOUR_SECRET".ToSecureString(),
    Passphrase = "YOUR_PASSPHRASE".ToSecureString()
};
```
3. Add the adapter to the `Connector` or other S# component and connect as usual.

Market data subscriptions and order commands are handled through the standard S# message model. Live candle updates are available for the 5‑minute timeframe, other periods are built from ticks.

## Implementation Notes

The adapter relies on two helper classes:

- `HttpClient` for REST requests (base URL `https://api.coinbase.com/api`).
- `SocketClient` for websocket streaming (`wss://advanced-trade-ws.coinbase.com`).

Message authentication is done through `Authenticator`, which signs requests using your secret key.

The adapter is associated with the trading board code `CNBS`.

For more information see the [Coinbase connector documentation](https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/coinbase.html).
