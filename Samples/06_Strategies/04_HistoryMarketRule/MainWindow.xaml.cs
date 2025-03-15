namespace StockSharp.Samples.Strategies.HistoryMarketRule;

using System;
using System.Windows;

using Ecng.Common;
using Ecng.Logging;

using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Xaml;

public partial class MainWindow
{
	private HistoryEmulationConnector _connector;
	private static readonly string _pathHistory = Paths.HistoryDataPath;

	private Security _security;
	private Portfolio _portfolio;
	private Strategy _strategy;
	
	private readonly LogManager _logManager;

	public MainWindow()
	{
		InitializeComponent();

		DatePickerBegin.SelectedDate = Paths.HistoryBeginDate;
		DatePickerEnd.SelectedDate = Paths.HistoryEndDate;

		_logManager = new LogManager();
		_logManager.Listeners.Add(new GuiLogListener(Monitor));
	}

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

		//
		// !!! IMPORTANT !!!
		// Uncomment the desired strategy
		//
		_strategy = new SimpleCandleRulesStrategy
		//_strategy = new SimpleOrderRulesStrategy
		//_strategy = new SimpleRulesStrategy
		//_strategy = new SimpleRulesUntilStrategy
		//_strategy = new SimpleTradeRulesStrategy
		{
			Security = _security,
			Connector = _connector,
			Portfolio = _portfolio,
			LogLevel = LogLevels.Debug
		};
		//_logManager.Sources.Add(_connector);
		_logManager.Sources.Add(_strategy);

		_strategy.Start();
		
		_connector.Connect();
		_connector.Start();
	}
}

