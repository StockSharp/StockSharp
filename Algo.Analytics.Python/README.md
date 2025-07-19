# Algo.Analytics.Python

This folder contains sample analytics scripts for the [StockSharp](https://stocksharp.com) trading platform written in **IronPython**. Each script implements the `IAnalyticsScript` interface and can be executed inside StockSharp Designer or any application that supports analytics scripts.

These examples illustrate the Hydra analytics feature. See the [Hydra documentation](https://doc.stocksharp.com/topics/hydra/analytics.html) for usage details.

The examples demonstrate how to analyse market data and visualize the results using StockSharp API. They can be used as a starting point for developing your own analytics tools or trading algorithms in Python.

## Requirements

- .NET 6 or higher
- [IronPython 3](https://ironpython.net/) (the scripts are tested with version 3.4.2)
- StockSharp packages, including `StockSharp.Algo.Analytics`

## Directory Structure

```
Algo.Analytics.Python/
├── common/                 # Helper modules used by the scripts
├── biggest_candle_script.py
├── chart3d_script.py
├── chart_draw_script.py
├── empty_analytics_script.py
├── indicator_script.py
├── normalize_price_script.py
├── pearson_correlation_script.py
├── price_volume_script.py
└── time_volume_script.py
```

### `common` helpers

The `common` directory provides a set of helper functions that simplify interaction with StockSharp objects from Python:

- **candle_extensions.py** – utilities for working with candle messages (length, body, shadows, middle price, etc.).
- **chart_extensions.py** – wrappers to create 2D and 3D charts with correct .NET type mapping.
- **datatype_extensions.py** – convenience function to create `TimeFrame` values.
- **indicator_extensions.py** – helpers to process StockSharp indicators from Python code.
- **numpy_extensions.py** – utility methods exposing [NumPy.NET](https://github.com/SciSharp/Numpy.NET) features to IronPython.
- **orderbook_extensions.py** – methods to get best bid/ask from order book messages.
- **storage_extensions.py** – extensions for loading market data from `IStorageRegistry`.

## Example Scripts

Below is a brief description of the provided scripts.

### `empty_analytics_script.py`
Template demonstrating the minimal implementation of an analytics script. Use it as a starting point for your own logic.

### `biggest_candle_script.py`
Finds the candles with the largest volume and range for each selected security and displays them on separate 3D charts.

### `chart3d_script.py`
Aggregates traded volume by hour for several securities and visualizes the results on a 3D chart.

### `chart_draw_script.py`
Shows how to draw different series styles (lines and histograms) on charts using closing prices and volumes.

### `indicator_script.py`
Calculates the Rate of Change (ROC) indicator for the loaded candles and plots both the candle series and indicator values.

### `normalize_price_script.py`
Normalizes closing prices of multiple securities to start from 1, allowing them to be compared on the same chart.

### `pearson_correlation_script.py`
Loads closing prices for multiple securities, computes the Pearson correlation matrix using NumPy.NET, and displays the matrix as a heatmap.

### `price_volume_script.py`
Calculates how trading volume is distributed across price levels for a single security and draws the distribution as a histogram.

### `time_volume_script.py`
Analyses when the largest trading volume occurs by grouping candles by hour and listing the totals in a grid.

## Running Scripts

1. Ensure IronPython 3 is available on your `PATH`.
2. Launch StockSharp Designer or your own host application.
3. Load any of the `.py` files and execute the script. Most scripts expect candle data to be available in your storage; adjust the date range and security identifiers as needed.


