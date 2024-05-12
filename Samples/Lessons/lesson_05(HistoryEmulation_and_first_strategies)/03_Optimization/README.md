# Parallel Strategy Optimization Using Brute Force

## Overview

The implementation demonstrates how to conduct a brute-force optimization for a trading strategy by testing different parameter values in parallel. The objective is to identify the most effective strategy configuration based on the historical data provided.

## Key Components

### Initialization

The constructor initializes the logging mechanisms and UI components, sets default dates for historical data testing, and prepares the `LogManager` to capture and display logs:

```csharp
public MainWindow()
{
    InitializeComponent();
    DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
    DatePickerEnd.SelectedDate = Paths.HistoryEndDate;
    CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromMinutes(5));

    _logManager = new LogManager();
    _logManager.Listeners.Add(new FileLogListener("log.txt"));
    _logManager.Listeners.Add(new GuiLogListener(Monitor));
}
```

### Starting the Optimization

When the `Start` button is clicked, it sets up the security and portfolio, configures the backtesting environment, and initiates the optimization process:

```csharp
private void Start_Click(object sender, RoutedEventArgs e)
{
    // Security and Portfolio Setup
    _security = new Security { Id = "SBER@TQBR", Code = "SBER", PriceStep = 0.01m, Board = ExchangeBoard.Micex };
    _portfolio = new Portfolio { Name = "test account", BeginValue = 1000000 };
    
    var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(_pathHistory) };

    // Setting up the optimizer
    _optimizer = new BruteForceOptimizer(new[] {_security}, new[] {_portfolio}, storageRegistry);
    _logManager.Sources.Add(_optimizer);

    var strategies = new List<(Strategy, IStrategyParam[])>
    {
        CreateStrategy("s1", Colors.Brown, 1),
        CreateStrategy("s2", Colors.Blue, 2),
        CreateStrategy("s3", Colors.Black, 3),
        CreateStrategy("s4", Colors.DarkGreen, 4),
        CreateStrategy("s5", Colors.DarkOrange, 5)
    };

    // Start optimization
    _optimizer.Start(DatePickerBegin.SelectedDate.Value.ChangeKind(DateTimeKind.Utc), 
                     DatePickerEnd.SelectedDate.Value.ChangeKind(DateTimeKind.Utc), 
                     strategies, strategies.Count);
}
```

### Strategy Creation and Configuration

The `CreateStrategy` method creates instances of the countertrend strategy with varying parameters. Each strategy is configured to draw its equity curve on the chart:

```csharp
private (Strategy, IStrategyParam[]) CreateStrategy(string name, Color color, int length)
{
    var candleSeries = CandleSettingsEditor.DataType.ToCandleSeries(_security);
    var strategy = new StairsStrategyCountertrendStrategy(candleSeries)
    {
        Length = length,
        Security = _security,
        Portfolio = _portfolio
    };

    var curveItems = EquityCurveChart.CreateCurve(name, color, ChartIndicatorDrawStyles.Line);
    strategy.PnLChanged += () =>
    {
        var data = new ChartDrawData();
        data.Group(strategy.CurrentTime)
            .Add(curveItems, strategy.PnL);
        EquityCurveChart.Draw(data);
    };
    StrategiesStatisticsPanel.AddStrategy(strategy);

    var parameters = new[] { strategy.Parameters[nameof(strategy.Length)] };
    return (strategy, parameters);
}
```

## Conclusion

This setup is ideal for performing extensive testing of trading strategies by automatically adjusting parameters and analyzing performance across a range of scenarios. The `BruteForceOptimizer` is particularly useful in environments where many potential configurations exist, and finding the optimal setup manually would be impractical. This method ensures that each strategy variation is evaluated objectively based on historical performance, aiding in the decision-making process for selecting the best trading strategy configuration.