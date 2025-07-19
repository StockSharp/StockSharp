# Charting.Interfaces

Charting.Interfaces contains a set of interface definitions and helper classes used for building and extending chart components within the StockSharp trading platform. These interfaces abstract the visualization layer so that different charting implementations can share a common API.

## Features

- Core interfaces such as `IChart`, `IChartArea`, `IChartAxis`, and `IChartElement` that describe charts, axes, and individual plot elements.
- Enumerations describing drawing modes and annotation types (for example `ChartCandleDrawStyles`, `ChartAnnotationTypes`, and `ChartAxisType`).
- A `DummyChartBuilder` implementation that produces simple placeholder chart parts. This is useful for testing or scenarios where a lightweight chart implementation is required.
- Extension methods in `ChartingInterfacesExtensions` that simplify creation of chart areas and elements, as well as drawing data on a chart.
- Interfaces for specialized chart elements such as orders, trades, indicators, bands, and annotations.
- Support for persistent settings via `IPersistable` so that chart layouts can be saved and restored.


## Usage

The library provides only interfaces and helper types, so it can be referenced from custom chart implementations or from other parts of StockSharp that interact with charts. Typical usage involves obtaining an `IChartBuilder` instance, creating areas and elements, and drawing data through `IChart` or `IThemeableChart` methods. See the StockSharp documentation for examples of integrating chart components with trading strategies.

## Documentation

Detailed documentation for StockSharp, including charting APIs, is available at [https://doc.stocksharp.com](https://doc.stocksharp.com).


