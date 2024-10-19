# Simple Moving Average Indicator Visualization

## Overview

This `MainWindow` class in a WPF application illustrates how to load historical candle data, compute a simple moving average (SMA) indicator for this data, and visually represent both the candles and the SMA on a chart. The application uses StockSharp's library to manage and display financial market data.

## Detailed Code Explanation

### Initialization and Chart Setup

The constructor initializes components and sets up the chart area and elements:

```csharp
public MainWindow()
{
    InitializeComponent();

    var chartArea = new ChartArea();
    Chart.AddArea(chartArea);

    var chartCandleElement = new ChartCandleElement();
    Chart.AddElement(chartArea, chartCandleElement);

    var chartIndicatorElement = new ChartIndicatorElement()
    {
        Color = Colors.Brown,
        DrawStyle = DrawStyles.Line
    };
    Chart.AddElement(chartArea, chartIndicatorElement);
}
```
- **Chart Area**: Defines the visual area where data will be plotted.
- **Candle Element**: Sets up candles to be drawn on the chart.
- **Indicator Element**: Configures how the SMA indicator will be displayed (color and style).

### Data Loading and Indicator Calculation

Loads candle data from a local storage and computes the SMA:

```csharp
var secId = "SBER@TQBR".ToSecurityId();

var candleStorage = new StorageRegistry().GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1), new LocalMarketDataDrive(_pathHistory), StorageFormats.Binary);
var candles = candleStorage.Load(Paths.HistoryBeginDate, Paths.HistoryEndDate);

var indicator = new SimpleMovingAverage()
{
    Length = 10
};

foreach (var candle in candles)
{
    var indicatorValue = indicator.Process(candle);
    var chartDrawData = new ChartDrawData();

    chartDrawData.Group(candle.OpenTime)
        .Add(chartCandleElement, candle)
        .Add(chartIndicatorElement, indicatorValue);

    Chart.Draw(chartDrawData);
}
```
- **Candle Storage**: Fetches one-minute candles for the security "SBER@TQBR" using the local storage path.
- **SMA Calculation**: The `SimpleMovingAverage` indicator with a length of 10 periods is updated with each candle.
- **Drawing on Chart**: Groups data by the opening time of the candle and adds both the candle and the computed SMA value to the chart for visualization.

## Conclusion

This program efficiently demonstrates how to use StockSharp's charting capabilities to visualize financial data and technical indicators, specifically showing how to superimpose a simple moving average on candlestick data. The application is designed to help traders or analysts visually interpret market trends and the impact of trading strategies based on historical data. Adjustments can be made to the SMA parameters or the visual styling to better suit specific analytical needs or preferences.