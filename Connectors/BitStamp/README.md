# BitStamp Connector

This project implements a StockSharp (S#) message adapter for the [Bitstamp](https://www.bitstamp.net/) cryptocurrency exchange. It allows S# based applications to receive real-time market data and to manage trading operations through Bitstamp's REST and WebSocket APIs.

## Features

- **Market data via WebSocket**: real-time trades, order books and order log updates.
- **Historical data**: OHLC candles and recent trades through HTTP requests.
- **Trading operations**: place market/limit/stop orders, cancel single orders or all active orders, and perform crypto or bank wire withdrawals.
- **Portfolio information**: query balances and commissions, with a configurable balance check interval.
- **Security lookup**: request the list of available trading pairs with detailed parameters.
- **Supported data types**: ticks, market depth, order log and time frame candles.

## Configuration

`BitStampMessageAdapter` requires API credentials for private requests:

- `Key` – your Bitstamp API key.
- `Secret` – the corresponding secret key.
- `BalanceCheckInterval` – optional interval for automatic balance refresh (useful when deposits or withdrawals occur).

## Usage Example

```csharp
var connector = new Connector();
var adapter = new BitStampMessageAdapter(connector.TransactionIdGenerator)
{
    Key = "your_api_key".ToSecureString(),
    Secret = "your_secret".ToSecureString(),
    BalanceCheckInterval = TimeSpan.FromMinutes(1)
};
connector.Adapter.InnerAdapters.Add(adapter);
connector.Connect();
```

After connecting you can subscribe to market data:

```csharp
var btcUsd = new SecurityId { SecurityCode = "BTC/USD", BoardCode = BoardCodes.BitStamp };
connector.SubscribeMarketData(new MarketDataMessage
{
    SecurityId = btcUsd,
    DataType2 = DataType.Ticks,
    IsSubscribe = true
});
```

Trading operations are performed by sending `OrderRegisterMessage` and `OrderCancelMessage` objects.

For more information about StockSharp connectors see the [documentation](https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitstamp.html).
