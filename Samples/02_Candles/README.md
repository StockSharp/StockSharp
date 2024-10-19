# StockSharp Candle Data Handling Examples

## Overview

This repository contains two key examples demonstrating how to work with candle data using the StockSharp framework in a .NET application. The examples cover both real-time candle data processing and the integration of historical candle data with real-time updates.

## Examples

### Realtime

This example shows how to set up and subscribe to real-time candle data for a selected security. It includes the setup of a `Connector` to handle live updates and the visualization of these updates using a chart.

- **Key Features**:
  - Real-time data subscription
  - Candle data visualization
  - Dynamic data handling

### CombineHistoryRealtime

This example extends the first by incorporating historical candle data alongside real-time updates. It demonstrates the configuration of storage for historical data and the seamless transition between historical and live data on a chart.

- **Key Features**:
  - Integration of historical and real-time data
  - Binary and CSV storage for historical data
  - Real-time updates and historical data visualization

## Getting Started

To run these examples, you will need to install the .NET and the StockSharp library. Each example can be run independently within a .NET-supported IDE such as Visual Studio. Ensure to restore all dependencies and configure the necessary settings before running the applications.

## Contributions

Contributions to improve or enhance the examples are welcome. Please ensure to follow the existing code style and add comprehensive documentation for any changes.