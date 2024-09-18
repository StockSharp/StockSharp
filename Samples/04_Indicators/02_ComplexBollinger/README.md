# Bollinger Bands Indicator Visualization in WPF Chart

## Overview

The `MainWindow` class in the WPF application demonstrates how to set up a chart to display financial data (candles) along with a Bollinger Bands indicator, which is commonly used in trading to identify the volatility and price levels over a standard deviation measurement.

## Key Components and Functionalities

### Initialization and Chart Setup

The constructor initializes the chart and configures its areas and elements:

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
		IndicatorPainter = new BollingerBandsPainter(),
		DrawStyle = DrawStyles.StepLine
	};
	Chart.AddElement(chartArea, chartIndicatorElement);
}
```
- **Chart Area**: Defines the visual area where data will be plotted.
- **Candle Element**: Sets up candles to be drawn on the chart.
- **Indicator Element**: Configures how the Bollinger Bands will be displayed, using a specific `IndicatorPainter` designed for this purpose.

### Data Loading and Indicator Calculation

Loads candle data from a local storage and computes the Bollinger Bands:

```csharp
var secId = "SBER@TQBR".ToSecurityId();

var candleStorage = new StorageRegistry().GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1), new LocalMarketDataDrive(_pathHistory), StorageFormats.Binary);
var candles = candleStorage.Load(Paths.HistoryBeginDate, Paths.HistoryEndDate);

var indicator = new BollingerBands();

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
- **Bollinger Bands Calculation**: The `BollingerBands` indicator is updated with each candle.
- **Drawing on Chart**: Groups data by the opening time of the candle and adds both the candle and the computed indicator value to the chart for visualization.

## Conclusion

This implementation effectively demonstrates how to use StockSharp's charting capabilities to visualize financial data along with technical indicators, specifically focusing on Bollinger Bands. The application allows traders or analysts to visually interpret volatility and price levels relative to the moving average, aiding in trading decisions based on historical data. Adjustments can be made to the Bollinger Bands parameters or the visual styling to better suit specific analytical needs or preferences.