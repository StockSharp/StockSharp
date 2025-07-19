# StockSharp Algo.Analytics.FSharp

## Overview

`Algo.Analytics.FSharp` contains a collection of analytic scripts written in [F#](https://fsharp.org/) for the [StockSharp](https://github.com/StockSharp/StockSharp) trading platform. Each script implements the `IAnalyticsScript` interface and can be executed inside any environment that provides an `IAnalyticsPanel` such as the **S# Designer** application. The scripts demonstrate how to read historical market data from `IStorageRegistry`, perform calculations, and render results using charts, grids, heatmaps, or 3‑D surfaces.

The project is distributed as a .NET 6 library and references the core `Algo.Analytics` project as well as `MathNet.Numerics.FSharp` for numerical calculations.

## Project Structure

- **Algo.Analytics.FSharp.fsproj** – F# project file targeting .NET 6.
- **EmptyAnalyticsScript.fs** – template for custom analytics.
- **PriceVolumeScript.fs** – plots volume distribution by price levels.
- **IndicatorScript.fs** – demonstrates using the Rate Of Change indicator.
- **TimeVolumeScript.fs** – groups volume by hour and displays results in a grid.
- **Chart3DScript.fs** – renders hourly volume distribution for several securities in a 3‑D chart.
- **ChartDrawScript.fs** – showcases drawing of lines and histograms on charts.
- **PearsonCorrelationScript.fs** – calculates the Pearson correlation between securities and displays a heatmap.
- **NormalizePriceScript.fs** – normalizes close prices of different securities.
- **BiggestCandleScript.fs** – finds candles with the largest range and volume.

## Scripts Overview

### EmptyAnalyticsScript
A minimal implementation of `IAnalyticsScript`. It performs no analysis and can be used as a starting point for new scripts.

### PriceVolumeScript
Processes candle data for a single instrument and builds a histogram showing how traded volume is distributed across price levels.

### IndicatorScript
Loads candles for each selected security, calculates the **Rate Of Change (ROC)** indicator, and displays close prices together with ROC values on two separate charts.

### TimeVolumeScript
Aggregates candle data by hour for the first selected instrument and fills a sortable grid with volumes. This highlights periods of the day with the most activity.

### Chart3DScript
Extends the idea of `TimeVolumeScript` to multiple instruments. For every security it groups volumes by hour, then visualizes the result in a 3‑D surface chart where axes represent instruments and hours.

### ChartDrawScript
Demonstrates different drawing styles available in `IAnalyticsChart`. For each security the script plots close prices as a dashed line and volumes as a histogram.

### PearsonCorrelationScript
Uses `MathNet.Numerics.Statistics` to compute the Pearson correlation matrix of close prices between all selected securities. The resulting matrix is rendered as a heatmap via `IAnalyticsPanel`.

### NormalizePriceScript
Shows how to normalize close prices by the first observed value so that several securities can be compared on the same scale in a single chart.

### BiggestCandleScript
Loads candles for each security and identifies the candle with the greatest range and the candle with the highest volume. The results are displayed on two charts using bubble series.

## Building and Running

1. Install the **.NET 6 SDK** and ensure that the `dotnet` command is available.
2. Open `StockSharp.sln` in Visual Studio or run `dotnet build` from the repository root. The project `Algo.Analytics.FSharp` will be built as a class library.
3. To run a script, reference it from your application or load it through the **S# Designer** analytics module, which provides an implementation of `IAnalyticsPanel` for displaying charts and tables.

## License

This project is released under the [Apache 2.0 License](../LICENSE).
