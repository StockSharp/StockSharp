# Implementation of Custom and Standard Moving Averages in StockSharp

## Overview

This application illustrates how to integrate StockSharp's charting capabilities with custom and standard indicators for financial market analysis. The main components are two moving averages displayed alongside the candlestick data for the stock "SBER@TQBR".

## Detailed Code Breakdown

### Initialization and Chart Setup

The `MainWindow` constructor initializes the UI components, sets up the chart area, and configures the candle and indicator elements:

```csharp
public MainWindow()
{
    InitializeComponent();

    var chartArea = new ChartArea();
    Chart.AddArea(chartArea);

    var chartCandleElement = new ChartCandleElement();
    Chart.AddElement(chartArea, chartCandleElement);

    var chartIndicatorElement1 = new ChartIndicatorElement
    {
        FullTitle = "SMA",
        Color = Colors.Brown,
        DrawStyle = DrawStyles.Line
    };

    var chartIndicatorElement2 = new ChartIndicatorElement
    {
        FullTitle = "Lazy",
        Color = Colors.DodgerBlue,
        DrawStyle = DrawStyles.Line
    };
    Chart.AddElement(chartArea, chartIndicatorElement1);
    Chart.AddElement(chartArea, chartIndicatorElement2);
}
```

### Data Loading and Indicator Processing

The code loads candle data and processes it through two indicators, drawing results on the chart:

```csharp
var secId = "SBER@TQBR".ToSecurityId();
var candleStorage = new StorageRegistry().GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1), new LocalMarketDataDrive(_pathHistory), StorageFormats.Binary);
var candles = candleStorage.Load(Paths.HistoryBeginDate, Paths.HistoryEndDate);

var indicator1 = new SimpleMovingAverage { Length = 10 };
var indicator2 = new LazyMovingAverage { Length = 10 };

foreach (var candle in candles)
{
    var indicatorValue1 = indicator1.Process(candle);
    var indicatorValue2 = indicator2.Process(candle);
    var chartDrawData = new ChartDrawData();

    chartDrawData.Group(candle.OpenTime)
        .Add(chartCandleElement, candle)
        .Add(chartIndicatorElement1, indicatorValue1)
        .Add(chartIndicatorElement2, indicatorValue2);

    Chart.Draw(chartDrawData);
}
```

### Custom Indicator Definition: LazyMovingAverage

The custom indicator, `LazyMovingAverage`, is defined with a simplified approach to smoothing data points, creating a "lazy" average:

```csharp
internal class LazyMovingAverage : BaseIndicator
{
    public int Length { get; set; } = 32;
    private decimal? _outputValue;

    protected override bool CalcIsFormed() => true;

    protected override IIndicatorValue OnProcess(IIndicatorValue input)
    {
        var value = input.GetValue<decimal>();
        if (_outputValue == null) _outputValue = value;
        _outputValue = _outputValue + (value - _outputValue) / Length;
        return new DecimalIndicatorValue(this, _outputValue.Value);
    }
}
```

This indicator starts with the first value it receives and then gradually adjusts towards new data points, moving towards them at a rate determined by the `Length` property.

## Conclusion

This WPF application provides an effective way to visualize how different types of moving averages respond to the same data set, helping traders or analysts to compare the sensitivity and lag of each indicator. The Lazy Moving Average offers a more gradual adjustment to price changes compared to the more responsive Simple Moving Average. This example can be adapted or extended to include other types of indicators or financial data for more comprehensive analysis.