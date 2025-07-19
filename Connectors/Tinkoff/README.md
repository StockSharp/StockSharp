# StockSharp Tinkoff Connector

## Overview

This project contains the StockSharp (S#) connector for [Tinkoff Invest API](https://tinkoff.github.io/investAPI/). The connector implements a message adapter that communicates with Tinkoff via gRPC and exposes market data and trading capabilities inside the S# infrastructure. Both the live and sandbox (demo) environments are supported.

The source code can be used as a reference for building custom integrations or included directly into your trading applications that rely on StockSharp.

## Features

- **Authentication** using a personal API token
- **Demo mode** via the sandbox API when `IsDemo` is enabled
- **Market data** subscriptions for:
  - Candles
  - Trades (tick data)
  - Level1 quotes (best bid/ask and last trade)
  - Market depth (order book)
- **Historical candle loading** from the history service
- **Security lookup** for stocks, futures, options, currencies and bonds
- **Order management**: place, cancel and replace both regular and stop orders
- **Streaming updates** for orders, trades and portfolios with automatic reconnection
- **Portfolio and position** requests

## Requirements

- [.NET 6.0](https://dotnet.microsoft.com/) or newer
- Access to the Tinkoff Invest API and a valid API token

## Building

To compile only the connector project run:

```bash
dotnet build Connectors/Tinkoff/Tinkoff.csproj
```

It is also included in the main `StockSharp.sln` solution and will be built automatically when that solution is compiled.

## Usage Example

Instantiate `TinkoffMessageAdapter`, supply your token and specify whether the sandbox should be used:

```csharp
var adapter = new TinkoffMessageAdapter(TransactionIdGenerator.Default)
{
    Token = "<YOUR_TOKEN>".ToSecureString(),
    IsDemo = false // true for sandbox trading
};
```

Attach the adapter to a `Connector` instance or another component that works with message adapters. You can then subscribe to market data, send orders and track portfolio changes in the usual StockSharp style.

## Documentation

General StockSharp documentation is available at [doc.stocksharp.com](https://doc.stocksharp.com/). The Russian documentation page for the Tinkoff adapter is located [here](https://doc.stocksharp.ru/topics/api/connectors/russia/tinkoff.html).

## License

Distributed under the [Apache 2.0 license](https://www.apache.org/licenses/LICENSE-2.0). See the root `LICENSE` file for the full license text.
