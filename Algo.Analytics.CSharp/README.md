# Algo.Analytics.CSharp

This project contains a set of C# analytics scripts designed for the [StockSharp](https://stocksharp.com) trading platform. Each script implements `IAnalyticsScript` and demonstrates how to process historical market data from S# storage and visualize results through charts, heatmaps or grids. The project is delivered as a class library (targeting .NET 6.0) and can be used inside the StockSharp Designer or any custom application that references `StockSharp.Algo.Analytics`.

These examples correspond to the Hydra analytics feature described in the [documentation](https://doc.stocksharp.com/topics/hydra/analytics.html).

## Overview

The project includes several sample scripts illustrating different analytical tasks:

- **EmptyAnalyticsScript** – template script that simply checks input and serves as a starting point for new analytics.
- **BiggestCandleScript** – identifies the largest candles (by volume and by length) for each selected security and displays them on separate charts.
- **ChartDrawScript** – shows how to draw line and histogram series on charts using candle close prices and volumes.
- **Chart3DScript** – demonstrates generation of a 3D chart showing how the biggest trading volume is distributed by hours across multiple securities.
- **IndicatorScript** – uses the Rate of Change (ROC) indicator to illustrate adding technical indicators to candle data.
- **NormalizePriceScript** – normalizes closing prices relative to the first candle and plots the series on a single chart.
- **PearsonCorrelationScript** – computes Pearson correlation between securities (requires MathNet.Numerics) and visualizes the result in a heatmap.
- **PriceVolumeScript** – calculates volume distribution by price levels and draws it as a histogram.
- **TimeVolumeScript** – aggregates traded volume by hour and outputs the information in a sortable grid.

Each script reads candles from `IStorageRegistry` using a specified `IMarketDataDrive`, `StorageFormats` and time frame. The resulting visuals are created through `IAnalyticsPanel` which exposes methods for creating charts, grids, heatmaps and 3‑D plots.

## Usage

1. Load this DLL into your application or the [S# Designer](https://doc.stocksharp.com/topics/designer.html) to run individual scripts.
2. Provide the securities, time range and storage parameters required by each script. The output will be displayed through the analytics panel implementation used (e.g., charts or tables in the Designer).


## Support

For help and discussion, join the [StockSharp community chat](https://t.me/stocksharpchat/361).
