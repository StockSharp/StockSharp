# Bitexbook Connector for StockSharp

## Overview

This directory contains the source code of the **Bitexbook** connector for the [StockSharp](https://github.com/StockSharp/StockSharp) trading platform. The connector implements the `BitexbookMessageAdapter` which enables communication with the Bitexbook cryptocurrency exchange via HTTP REST and WebSocket APIs.

The project demonstrates how to integrate StockSharp with Bitexbook in order to obtain market data and perform trading operations. You can use the implementation as a reference when creating your own connectors or include it directly in your applications.

## Features

- Real-time communication over WebSocket for ticker updates and order events.
- HTTP REST client for historical candles and trading requests.
- Support for limit and market orders.
- Balance check interval for monitoring account funds.
- Mapping between StockSharp `SecurityId` and Bitexbook symbols.
- Handles subscriptions for Level1, order log, and candle data.
- Implements deposit and withdrawal operations via a custom order condition.


## Usage

Below is a simplified example of how the connector can be used within a StockSharp `Connector` instance.

```csharp
var adapter = new BitexbookMessageAdapter(new IncrementalIdGenerator())
{
    Key = "YourApiKey".ToSecureString(),
    Secret = "YourSecret".ToSecureString(),
    BalanceCheckInterval = TimeSpan.FromMinutes(1)
};

var connector = new Connector
{
    Adapter = adapter
};

connector.Connect();
```

Once connected you can subscribe to market data or register/cancel orders via the standard StockSharp API.

## Documentation

Detailed instructions for creating custom connectors and using StockSharp can be found in the [official documentation](https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/bitexbook.html).

