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
2. Find and add required connector via [NuGet package](https://stocksharp.com/products/nuget_manual/#privateserver)
3. Modify the code lines for init connector. E.g. [Binance setup](https://doc.stocksharp.com/topics/api/connectors/crypto_exchanges/binance/adapter_initialization_binance.html)

## Configuration

Before running the application, ensure you provide your Binance API key and secret in the code, as these are essential for authentication. Replace `<Your key>` and `<Your secret>` with your actual Binance credentials:

```csharp
// var messageAdapter = new StockSharp.Binance.BinanceMessageAdapter(connector.TransactionIdGenerator)
// {
//     Key = "<Your key>".Secure(),
//     Secret = "<Your secret>".Secure(),
//     IsDemo = true
// };
// connector.Adapter.InnerAdapters.Add(messageAdapter);
```

## Usage

Run the application from your IDE or via the command line:

```sh
dotnet run
```

Follow the on-screen instructions to interact with Binance through the StockSharp platform. The console will guide you through connecting to the service, retrieving securities, subscribing to updates, and placing orders.

## Features

- **Connection Handling**: Connects to Binance and handles reconnections and errors.
- **Security Lookup**: Queries Binance for securities like BTCUSD_PERP.
- **Portfolio Monitoring**: Monitors and displays portfolio updates.
- **Market Depth Subscription**: Subscribes to and displays market depth for selected securities.
- **Order Placement**: Handles order input and sends orders to Binance.

## Limitations

This example is intended for demonstration purposes and should be adapted with proper error handling and security measures for production use.