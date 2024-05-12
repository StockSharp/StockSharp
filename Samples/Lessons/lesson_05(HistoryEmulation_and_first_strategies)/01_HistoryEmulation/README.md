# History Market Data Emulation with StockSharp

## Overview

This WPF application uses StockSharp's `HistoryEmulationConnector` to simulate trading activities based on historical data. It visualizes candlestick data and trade elements on a chart, allowing users to analyze historical market movements and test trading strategies.

## Key Components and Functionalities

### Initialization and LogManager Setup

The `MainWindow` constructor initializes the UI components, sets up the log manager, and configures the date pickers for selecting the historical data range:

```csharp
public MainWindow()
{
	InitializeComponent();

	_logManager = new LogManager();
	_logManager.Listeners.Add(new FileLogListener("log.txt"));
	_logManager.Listeners.Add(new GuiLogListener(Monitor));

	DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
	DatePickerEnd.SelectedDate = Paths.HistoryEndDate;

	CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));
}
```

- **LogManager**: Captures and logs all relevant events and operations, facilitating debugging and analysis.
- **Date Pickers**: Allow the user to specify the start and end dates for the historical data simulation.

### Setup and Start Simulation

Triggered by a button click, this method sets up the securities, portfolio, and connector for the simulation:

```csharp
private void Start_Click(object sender, RoutedEventArgs e)
{
	_security = new Security { Id = "SBER@TQBR", Code = "SBER", PriceStep = 0.01m, Board = ExchangeBoard.Micex };
	_portfolio = new Portfolio { Name = "test account", BeginValue = 1000000 };
	var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(_pathHistory) };

	_connector = new HistoryEmulationConnector(new[] { _security }, new[] { _portfolio })
	{
		HistoryMessageAdapter =
		{
			StorageRegistry = storageRegistry,
			StorageFormat = StorageFormats.Binary,
			StartDate = DatePickerBegin.SelectedDate.Value.ChangeKind(DateTimeKind.Utc),
			StopDate = DatePickerEnd.SelectedDate.Value.ChangeKind(DateTimeKind.Utc),
		},
		LogLevel = LogLevels.Info,
	};
	_logManager.Sources.Add(_connector);

	_candleSeries = CandleSettingsEditor.DataType.ToCandleSeries(_security);
	InitChart();

	_connector.CandleProcessing += Connector_CandleSeriesProcessing;
	_connector.OrderBookReceived += (s, b) => MarketDepthControl.UpdateDepth(b);
	_connector.Connected += Connector_Connected;
	_connector.Connect();
}
```

- **HistoryEmulationConnector**: Simulates market data based on historical records.
- **StorageRegistry**: Manages data storage, ensuring historical data is accessible for the simulation.

### Chart Initialization

Sets up the chart area and adds elements for displaying the data:

```csharp
private void InitChart()
{
	Chart.ClearAreas();
	var area = new ChartArea();
	_candleElement = new ChartCandleElement();
	_tradeElement = new ChartTradeElement { FullTitle = "Trade" };
	Chart.AddArea(area);
	Chart.AddElement(area, _candleElement);
	Chart.AddElement(area, _tradeElement);
}
```

- **ChartArea and Elements**: Configures areas within the chart for displaying different types of market data visually.

### Data Subscription and Simulation Start

Once connected, subscribes to candles and begins the simulation:

```csharp
private void Connector_Connected()
{
	// uncomment in case has order book history
	// (will degradate performance)
	//_connector.SubscribeMarketDepth(_security);

	_connector.SubscribeCandles(_candleSeries);
	_connector.Start();
}
```

### Candle Data Processing

Updates the chart with new candle data as it arrives from the simulation:

```csharp
private void Connector_CandleSeriesProcessing(CandleSeries candleSeries, ICandleMessage candle)
{
	Chart.Draw(_candleElement, candle);
}
```

## Conclusion

This application showcases the capabilities of StockSharp for historical market data simulation, providing tools for backtesting trading strategies and analyzing past market behaviors in a controlled environment. The setup allows for detailed logging, interactive date selection, and real-time chart updates, making it a robust tool for financial analysis and trading strategy development.