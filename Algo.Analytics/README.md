# Algo.Analytics

Algo.Analytics is a small library that exposes a common set of interfaces for running analytics scripts on top of the StockSharp trading platform. The package itself does not contain any user interface but defines abstractions that allow scripts written in different languages to display their results in a uniform way.

The library is a part of the main StockSharp repository and can be found in the `Algo.Analytics` folder. Example scripts that implement these interfaces are located in the sibling folders:

- `Algo.Analytics.CSharp` – C# examples
- `Algo.Analytics.FSharp` – F# examples
- `Algo.Analytics.Python` – Python examples together with helper modules for .NET interoperability

These example scripts showcase the Hydra analytics feature. See the [Hydra documentation](https://doc.stocksharp.com/topics/hydra/analytics.html) for details on executing analytics scripts inside the platform.

## Interfaces

### `IAnalyticsScript`
Entry point for a custom analytic. The `Run` method receives the logging interface, an `IAnalyticsPanel` for output and parameters describing the market data to analyse.

### `IAnalyticsPanel`
Represents a container where a script can place the output. It allows creating:

- tables via `CreateGrid`
- two‑dimensional charts via `CreateChart`
- three‑dimensional charts via `CreateChart` with three generic arguments
- heatmaps via `DrawHeatmap`
- 3D surfaces via `Draw3D`

### `IAnalyticsChart`
A chart produced by `IAnalyticsPanel`. It exposes `Append` methods to add series of points (or bubbles in the 3D case) with the desired drawing style and color.

### `IAnalyticsGrid`
A helper interface to fill tabular data and specify sorting rules.

## Example scripts

The example folders contain several ready‑to‑use scripts demonstrating how to implement different analytics:

- **BiggestCandleScript** – finds the candle with the largest body and volume
- **PriceVolumeScript** – shows distribution of volume by price levels
- **IndicatorScript** – calculates a ROC indicator and plots it on a chart
- **Chart3DScript** and **ChartDrawScript** – demonstrate drawing 3D surfaces and custom chart series
- **NormalizePriceScript**, **PearsonCorrelationScript**, **TimeVolumeScript**, and others

Python examples include a set of helper modules under `common` that simplify working with .NET types from Python code (for example `chart_extensions.py` and `storage_extensions.py`).


