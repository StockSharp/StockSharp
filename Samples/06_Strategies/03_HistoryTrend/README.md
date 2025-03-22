# Strategy Testing on Historical Data using StockSharp

## Overview

The application is designed to facilitate the backtesting of multiple trading strategies by replaying historical market data through a `HistoryEmulationConnector`. This setup allows for evaluating the performance of strategies such as `OneCandleCountertrend`, `OneCandleTrend`, `StairsCountertrend`, and `StairsTrend` under historically accurate market conditions.

## Key Components of the Application

### Initialization and Configuration

Upon launching the application, the UI components are initialized, and logging listeners are set up to capture and display relevant information:

```csharp
public MainWindow()
{
    InitializeComponent();

    _logManager = new LogManager();
    _logManager.Listeners.Add(new FileLogListener("log.txt"));
    _logManager.Listeners.Add(new GuiLogListener(Monitor));

    DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
    DatePickerEnd.SelectedDate = Paths.HistoryEndDate;

    CandleDataTypeEdit.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));
}
```

- **LogManager**: Captures logs for debugging and analysis.
- **Date Pickers**: Allows the user to select the period for historical data simulation.
- **Candle Settings**: Configures the default candle data type as 5-minute timeframe candles.

### Strategy Setup and Historical Data Simulation

When the user starts the simulation, the application configures the necessary components for running the selected trading strategy on historical data:

```csharp
private void Start_Click(object sender, RoutedEventArgs e)
{
    _security = new Security
    {
        Id = Paths.HistoryDefaultSecurity,
        PriceStep = 0.01m,
    };
    _portfolio = new Portfolio { Name = "test account", BeginValue = 1000000 };
    var storageRegistry = new StorageRegistry
    {
        DefaultDrive = new LocalMarketDataDrive(_pathHistory),
    };

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

    // ... setup charts and data grids ...

    //
    // !!! IMPORTANT !!!
    // Uncomment the desired strategy
    //
    _strategy = new OneCandleCountertrendStrategy
    //_strategy = new OneCandleTrendStrategy
    //_strategy = new StairsCountertrendStrategy
    //_strategy = new StairsTrendStrategy
    {
        Security = _security,
        Connector = _connector,
        Portfolio = _portfolio,
        CandleType = CandleDataTypeEdit.DataType,
    };
    
    // ... additional strategy setup ...
    
    _strategy.Start();
    _connector.Connect();
    _connector.Start();
}
```

- **Security and Portfolio**: Sets up the trading instrument and portfolio for simulation.
- **HistoryEmulationConnector**: Configures the connector to replay historical market data.
- **Strategy Selection**: Allows for easy switching between different strategy implementations.
- **Strategy Configuration**: Sets the required properties for the selected strategy.

### Visualization and Monitoring

The application includes visualization components to monitor the strategy's performance:

```csharp
private void InitPnLChart()
{
    _pnl = EquityCurveChart.CreateCurve("P&L", Colors.Green, DrawStyles.Area);
    _unrealizedPnL = EquityCurveChart.CreateCurve("unrealized", Colors.Black, DrawStyles.Line);
    _commissionCurve = EquityCurveChart.CreateCurve("commission", Colors.Red, DrawStyles.Line);
}

private void Strategy_PnLChanged()
{
    EquityCurveChart.DrawPnL(_strategy, _pnl, _unrealizedPnL, _commissionCurve);
}
```

- **P&L Chart**: Visualizes the strategy's profit and loss over time.
- **Order and Trade Grids**: Display orders and trades executed by the strategy.
- **Market Depth Control**: Shows the order book when available.

### UI Integration with Strategy

The application integrates the selected strategy with the UI components:

```csharp
_connector.OrderBookReceived += (s, b) => MarketDepthControl.UpdateDepth(b);
_connector.OrderReceived += (s, o) => OrderGrid.Orders.TryAdd(o);
_connector.OrderRegisterFailReceived += (s, f) => OrderGrid.AddRegistrationFail(f);
_connector.OwnTradeReceived += (s, t) => MyTradeGrid.Trades.TryAdd(t);

_strategy.PnLChanged += Strategy_PnLChanged;
_strategy.SetChart(Chart);

StatisticParameterGrid.Parameters.AddRange(_strategy.StatisticManager.Parameters);
```

- **Event Handlers**: Connect market data and trading events to UI updates.
- **Chart Integration**: The strategy directly manages its chart visualization.
- **Statistics**: Strategy performance statistics are displayed in a dedicated grid.

## Using Different Strategies

The application makes it easy to switch between different trading strategies:

1. **OneCandleCountertrendStrategy**: Takes positions contrary to the most recent candle's direction.
2. **OneCandleTrendStrategy**: Takes positions in the same direction as the most recent candle.
3. **StairsCountertrendStrategy**: Takes counter-trend positions after a specified number of consecutive candles in one direction.
4. **StairsTrendStrategy**: Takes trend-following positions after a specified number of consecutive candles in one direction.

To select a strategy, simply uncomment the desired strategy line in the `Start_Click` method and comment out the others.

## Conclusion

This application provides a comprehensive environment for testing various trading strategies using historical market data. It leverages StockSharp's powerful backtesting capabilities and combines them with intuitive visualization tools to help traders evaluate and refine their algorithmic trading strategies.

Key features include:
- Easy switching between different strategy implementations
- Detailed visualization of strategy performance
- Comprehensive logging and statistics
- Configurable testing parameters such as date range and commission rules

This testing environment serves as both a development tool for strategy creators and an educational resource for understanding how different trading approaches perform under historical market conditions.