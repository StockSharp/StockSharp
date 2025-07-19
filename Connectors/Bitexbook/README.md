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

## Building

The connector targets **.NET 6.0** and depends on other StockSharp projects. To build it as part of the whole solution:

1. Clone the repository and open `StockSharp.sln` in Visual Studio or run `dotnet build` from the command line.
2. The project file is `Bitexbook.csproj`, which imports common build settings from `common_connectors_websocket.props`.
3. After a successful build you can reference the resulting assembly from your trading application.

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

## License

This project is distributed under the [Apache 2.0](https://www.apache.org/licenses/LICENSE-2.0) license. See the root `LICENSE` file for details.
