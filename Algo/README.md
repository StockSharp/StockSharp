# StockSharp Algo Library

## Overview

StockSharp.Algo is the core algorithmic trading library used throughout the StockSharp platform. It provides the building blocks for creating trading bots, managing market data, handling order routing, and simulating strategies against historical data. The library targets **.NET 6.0** and can be used in desktop, server, or cloud applications.

## Features

- **Connectors and Message Adapters** – unified infrastructure for connecting to exchanges and data feeds. Includes offline adapters for simulation and adapters for incremental order books, snapshots, and extended order information.
- **Strategy Framework** – base classes for building algorithmic strategies with built‑in event model, parameter system, and rule management. Strategies can be composed and executed in parallel or as baskets.
- **Market Data Storages** – tools for storing quotes, trades, candles, and order books in various formats (binary, CSV) with caching and synchronization support.
- **Risk and PnL** – modules for risk management, slippage modeling, commission calculation, and real‑time profit‑and‑loss tracking.
- **Testing and Emulation** – historical emulation connectors and market data generators for backtesting strategies under conditions close to real trading.
- **Services Registry** – helper class to access common services (exchanges, securities, storages) across the application.

## Getting Started

### Prerequisites

- [.NET SDK 6.0](https://dotnet.microsoft.com/) or later.
- Visual Studio 2022 or any compatible IDE.

### Basic Usage

The typical entry point for working with the library is the `Connector` class. Below is a very simplified example that demonstrates how to run a custom strategy using historical data:

```csharp
using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;

var connector = new HistoryEmulationConnector(securityProvider, portfolioProvider, storageRegistry);
var myStrategy = new MyStrategy { Connector = connector, Portfolio = myPortfolio, Security = mySecurity };

connector.NewStrategy += strategy => strategy.Start();
connector.Connect();
```

A full‑fledged application will include market data subscriptions, order registration, and more advanced strategy logic.

## Documentation

Comprehensive documentation for the API and subsystems is available at the [StockSharp documentation website](https://doc.stocksharp.com/).


## Support

Questions and discussions are welcome in the community [chat](https://t.me/stocksharpchat/361).
