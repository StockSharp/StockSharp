# StockSharp Cross-Platform Connector Example

## Overview

This console application demonstrates the use of StockSharp, a trading and algorithmic trading platform, to connect to different trading services using cross-platform connectors. It specifically shows how connectors, built on .NET Core, can operate across various environments. This example uses the Binance connector as a case study, highlighting the flexibility of StockSharp in a console application setting to maintain cross-platform compatibility (as opposed to WPF, which is not cross-platform).

## Prerequisites

- .NET 6 or later
- Visual Studio or any compatible .NET Core IDE
- An active internet connection to connect to trading services
- Binance account credentials (API Key and Secret)

## Installation

1. Open the solution in your IDE.
2. Find and add required connector via [NuGet package](https://doc.stocksharp.com/topics/api/setup.html#private-nuget-server)
3. Modify the code lines for init connector. E.g. [Binance setup](https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/binance/adapter_initialization_binance.html)

## Configuration

Before running the application, ensure you provide your Binance API key and secret in the code, as these are essential for authentication. Replace `<Your key>` and `<Your secret>` with your actual Binance credentials:

```csharp
connector.Adapter.InnerAdapters.Add(new Binance.BinanceMessageAdapter(connector.TransactionIdGenerator)
{
    Key = "<Your key>".Secure(),
    Secret = "<Your secret>".Secure(),
    //IsDemo = true
});
```

## Usage

Run the application from your IDE or via the command line:

```sh
dotnet run
```

Follow the on-screen instructions to interact with Binance through the StockSharp platform. The console will guide you through connecting to the service, retrieving securities, subscribing to updates, and placing orders.

## Features

- **Connection Handling**: Connects to Binance and handles connection errors.
- **Security Lookup**: Queries Binance for securities. Default example uses "BTCUSDT" futures.
- **Portfolio Monitoring**: Monitors and displays portfolio and position updates.
- **Market Depth Subscription**: Subscribes to and displays market depth (best bid and ask) for selected securities.
- **Order Placement**: Allows placing buy orders based on the current market depth.

## Workflow

The application follows this sequence:
1. Connects to Binance
2. Looks up the specified security (BTCUSDT futures)
3. Displays portfolio information or subscribes to position updates if no portfolios are available
4. Subscribes to market depth to get bid/ask information
5. Prompts the user to place a buy order (either limit order based on the best bid price or market order)

## Limitations

This example is intended for demonstration purposes and should be adapted with proper error handling and security measures for production use.