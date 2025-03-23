# Detailed Explanation of Hydra FIX Connection for Market Data Retrieval

## Overview

This `MainWindow` class within a WPF application demonstrates how to establish a connection to Hydra via the FIX protocol to fetch market data, similarly to how one would connect to a conventional trading exchange. The code is structured to handle historical candle data and display it in a chart format, offering functionalities to configure and save connection settings, and dynamically subscribe to different securities' data.

## Key Components and Functionalities

### Initialization and Configuration

The `MainWindow` constructor sets up the initial environment:

```csharp
public MainWindow()
{
    InitializeComponent();

    // registering all connectors
    ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_connector.Adapter.InnerAdapters));

    if (File.Exists(_connectorFile))
    {
        _connector.Load(_connectorFile.Deserialize<SettingsStorage>());
    }
    else
    {
        var adapter = new Fix.FixMessageAdapter(_connector.TransactionIdGenerator)
        {
            Address = RemoteMarketDataDrive.DefaultAddress,
            TargetCompId = RemoteMarketDataDrive.DefaultTargetCompId,

            SenderCompId = "hydra_user",

            //
            // required for non anonymous access
            //
            //Password = "hydra_user".To<SecureString>()

            //
            // uncomment to enable binary mode
            //
            //IsBinaryEnabled = true,
        };

        // turning off the support of the transactional messages
        adapter.ChangeSupported(false, false);

        _connector.Adapter.InnerAdapters.Add(adapter);

        _connector.Save().Serialize(_connectorFile);
    }

    CandleDataTypeEdit.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));

    DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
    DatePickerEnd.SelectedDate = Paths.HistoryEndDate;

    SecurityPicker.SecurityProvider = _connector;
    SecurityPicker.MarketDataProvider = _connector;

    _connector.ConnectionError += Connector_ConnectionError;
    _connector.CandleReceived += Connector_CandleSeriesProcessing;
}
```

- **Connector Setup**: Initializes the `Connector`, which manages connections and data flow.
- **Default Connection Configuration**: Creates a default FIX connection to Hydra with anonymous access by default (password commented out).
- **Transaction Support Disabled**: Explicitly turns off transactional messages support for the adapter.
- **Configuration Loading**: Loads saved configurations if available.
- **UI Components Initialization**: Sets up data types (5-minute timeframe by default) and date pickers for the UI.
- **Security Provider Configuration**: Sets the connector as both the security provider and market data provider.
- **Event Registration**: Subscribes to the CandleReceived and ConnectionError events.

### Error Handling

Provides error handling for connection issues:

```csharp
private void Connector_ConnectionError(Exception error)
{
    this.GuiAsync(() =>
    {
        MessageBox.Show(this.GetWindow(), error.ToString(), LocalizedStrings.ErrorConnection);
    });
}
```

- **GUI Thread Safety**: Uses GuiAsync to ensure the error dialog is shown on the UI thread.
- **User Feedback**: Displays connection errors in a message box with a localized title.

### Configuration Management

Allows modifying connection settings through a configuration UI:

```csharp
private void Setting_Click(object sender, RoutedEventArgs e)
{
    if (_connector.Configure(this))
    {
        _connector.Save().Serialize(_connectorFile);
    }
}
```

- **Settings Dialog**: Opens a configuration dialog for the connector.
- **Configuration Persistence**: Saves changes to the configuration file if modifications are confirmed.

### Connection Establishment

Triggered by a button click, the method initiates a connection to Hydra:

```csharp
private void Connect_Click(object sender, RoutedEventArgs e)
{
    _connector.Connect();
}
```

- **Connection Initiation**: Starts the connection process with the configured settings.

### Data Subscription and Processing

Upon selecting a security, this section subscribes to candle data:

```csharp
private void SecurityPicker_SecuritySelected(Security security)
{
    if (security == null) return;
    if (_subscription != null) _connector.UnSubscribe(_subscription);

    _subscription = new(CandleDataTypeEdit.DataType, security)
    {
        From = DatePickerBegin.SelectedDate,
        To = DatePickerEnd.SelectedDate,
    };

    if (BuildFromTicks.IsChecked == true)
    {
        _subscription.MarketData.BuildMode = MarketDataBuildModes.Build;
        _subscription.MarketData.BuildFrom = DataType.Ticks;
    }

    //------------------------------------------Chart----------------------------------------------------------------------------------------
    Chart.ClearAreas();

    var area = new ChartArea();
    Chart.AddArea(area);

    _candleElement = new ChartCandleElement();
    Chart.AddElement(area, _candleElement, _subscription);

    _connector.Subscribe(_subscription);
}
```

- **Subscription Management**: Unsubscribes from previous data streams and sets up a new subscription for the selected security.
- **Date Range**: Sets the historical data range based on the selected dates.
- **Build Mode Option**: Allows building candles from tick data if the corresponding checkbox is checked.
- **Chart Setup**: Prepares a new chart area and adds a candle element for visualization.
- **Subscription Activation**: Subscribes to the data according to the specified parameters.

### Candle Data Visualization

Handles the drawing of each candle on the chart as data is received:

```csharp
private void Connector_CandleSeriesProcessing(Subscription subscription, ICandleMessage candle)
{
    Chart.Draw(_candleElement, candle);
}
```

- **Chart Update**: Updates the chart with new candle data as it arrives through the subscription.

## Features

- **Persistent Connection Settings**: Saves and loads connection configurations between sessions.
- **Anonymous Access Support**: Configured by default for anonymous access to Hydra.
- **Error Handling**: Provides user feedback for connection issues through UI.
- **No Transaction Support**: Specifically disables transactional message support for data-only connections.
- **Historical Data Retrieval**: Allows fetching candle data for a specific date range.
- **Custom Timeframes**: Supports configuring different timeframes for candle data.
- **Candle Building**: Optional functionality to build candles from tick data.
- **Chart Visualization**: Updates chart as candle data is received.
- **Localization Support**: Uses localized strings for UI elements.

## Conclusion

This example efficiently demonstrates how to use the FIX protocol with the Hydra system to retrieve historical market data. The application follows the "read-only" pattern by explicitly disabling transaction support, making it ideal for market data analysis without trading functionality. The WPF interface allows for dynamic interaction with historical data, selecting securities, adjusting timeframes, and visualizing the results in a chart. The system is designed to be user-friendly with proper error handling and localization support.