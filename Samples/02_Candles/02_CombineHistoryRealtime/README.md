# Combining Historical and Real-Time Candle Data in a StockSharp Trading Application

## Overview

This section of the documentation outlines how to manage and visualize both historical and real-time candle data within a .NET application using the StockSharp framework. The code integrates candle data management with charting to provide a continuous view of market changes.

## Key Components

1. **Data Storage**: Utilizes CSV [storage](https://doc.stocksharp.com/topics/api/market_data_storage.html) for historical data and local market [data drives](https://doc.stocksharp.com/topics/api/market_data_storage/api.html).
2. **Real-Time Subscription**: Subscribing to [real-time candle data](https://doc.stocksharp.com/topics/api/candles.html) and [merging it with historical data](https://doc.stocksharp.com/topics/api/candles/gluing_candles_history_real_time.html).
3. **Charting**: Visualization of both historical and real-time data on a [chart](https://doc.stocksharp.com/topics/api/graphical_user_interface.html).

## Code Explanation and Usage

### Installation

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Find and add required connector via [NuGet package](https://doc.stocksharp.com/topics/api/setup.html#private-nuget-server)
4. Build and run the application.

### Initialization and Configuration

The constructor sets up data storage, initializes components, and configures default settings for candle data types:

```csharp
public MainWindow()
{
    InitializeComponent();
    var entityRegistry = new CsvEntityRegistry(_pathHistory);
    var storageRegistry = new StorageRegistry
    {
        DefaultDrive = new LocalMarketDataDrive(_pathHistory)
    };

    _connector = new Connector(entityRegistry.Securities, entityRegistry.PositionStorage, 
                new InMemoryExchangeInfoProvider(), storageRegistry, new SnapshotRegistry("SnapshotRegistry"));

    if (File.Exists(_connectorFile))
    {
        _connector.Load(_connectorFile.Deserialize<SettingsStorage>());
    }
    CandleDataTypeEdit.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));
}
```

### Connecting and Handling Candle Data

The connection setup includes subscribing to candle data streams and setting up event handlers to process received candle data:

```csharp
private void Connect_Click(object sender, RoutedEventArgs e)
{
    SecurityPicker.SecurityProvider = _connector;
    _connector.CandleProcessing += Connector_CandleSeriesProcessing;
    _connector.Connect();
}

private void Connector_CandleSeriesProcessing(Subscription subscription, ICandleMessage candle)
{
    Chart.Draw(_candleElement, candle);
}
```

### Selecting Security and Subscribing to Data

When a security is selected, the application [subscribes](https://doc.stocksharp.com/topics/api/market_data/subscriptions.html) to both historical and real-time candle data, setting up the charting components accordingly:

```csharp
private void SecurityPicker_SecuritySelected(Security security)
{
    if (security == null) return;
    if (_subscription != null)
        _connector.UnSubscribe(_subscription);

    _subscription = new Subscription(CandleDataTypeEdit.DataType, security)
    {
        MarketData = 
        {
            From = DateTime.Today.AddDays(-720),
            BuildMode = MarketDataBuildModes.LoadAndBuild,
        }
    };

    Chart.ClearAreas();
    var area = new ChartArea();
    _candleElement = new ChartCandleElement();
    Chart.AddArea(area);
    Chart.AddElement(area, _candleElement, _subscription);
    _connector.Subscribe(_subscription);
}
```

## Conclusion

This approach allows the application to handle and visualize a comprehensive set of candle data, combining historical insights with the immediacy of real-time updates. This guide provides detailed instructions on how to implement and understand the functionalities for managing candle data in a trading environment.

This documentation should assist users and developers in configuring and extending the application's capabilities to meet specific trading and data analysis needs. Adjustments can be made based on the actual implementation details or specific configurations in your project.