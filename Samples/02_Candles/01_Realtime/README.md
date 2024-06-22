# Handling Real-Time Candle Data in a StockSharp Trading Application

## Overview

This documentation section explains how to handle and visualize real-time candle data using the StockSharp trading framework within a .NET application. The functionalities include setting up candle subscriptions based on various criteria and displaying the candle data in a chart.

## Key Components

1. **Candle Series Setup**: Configuration of [candle data](https://doc.stocksharp.com/topics/api/candles.html) types and subscription settings.
2. **Real-Time Data Subscription**: [Subscribing](https://doc.stocksharp.com/topics/api/market_data/subscriptions.html) to candle data and handling incoming data updates.
3. **Data Visualization**: Integrating with [charting components](https://doc.stocksharp.com/topics/api/graphical_user_interface/charts.html) to visualize the data.

## Code Explanation and Usage

### Installation

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Find and add required connector via [NuGet package](https://stocksharp.com/products/nuget_manual/#privateserver)
4. Build and run the application.

### Initialization and Configuration

The constructor initializes components and sets default settings for candle data types:

```csharp
public MainWindow()
{
    InitializeComponent();
    if (File.Exists(_connectorFile))
    {
        _connector.Load(_connectorFile.Deserialize<SettingsStorage>());
    }
    CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));
}
```

### Connection Setup and Data Subscription

Setting up the connection and subscribing to candle data when a security is selected:

```csharp
private void Connect_Click(object sender, RoutedEventArgs e)
{
    SecurityPicker.SecurityProvider = _connector;
    _connector.CandleProcessing += Connector_CandleSeriesProcessing;
    _connector.Connect();
}

private void SecurityPicker_SecuritySelected(Security security)
{
    if (security == null) return;

    if (_subscription != null)
        _connector.UnSubscribe(_subscription);

    // Example of setting up a TimeFrame candle series
    var candleSeries = new CandleSeries(typeof(TimeFrameCandle), security, TimeSpan.FromMinutes(5))
    {
        BuildCandlesMode = MarketDataBuildModes.Build,
        BuildCandlesFrom = MarketDataTypes.Trades,
    };

    _subscription = _connector.SubscribeCandles(candleSeries, DateTime.Today, DateTime.Now);
}
```

### Handling Candle Data

Implementing an event handler for processing received candle data and [updating the chart](https://doc.stocksharp.com/topics/api/candles/chart.html):

```csharp
private void Connector_CandleSeriesProcessing(CandleSeries series, Candle candle)
{
    if (_candleElement == null)
    {
        _candleElement = new ChartCandleElement();
        Chart.AddElement(_candleElement, series);
    }

    Chart.Draw(_candleElement, candle);
}
```

## Conclusion

This setup enables the application to handle real-time candle data for selected securities, offering capabilities to dynamically subscribe to different types of candles and visualize them using integrated chart components. This guide provides clear instructions on how to set up and manage candle data within the application, facilitating the development of advanced trading strategies based on real-time data analysis.

Feel free to adjust the snippets and explanations to better fit the actual implementation details or specific configurations in your project.