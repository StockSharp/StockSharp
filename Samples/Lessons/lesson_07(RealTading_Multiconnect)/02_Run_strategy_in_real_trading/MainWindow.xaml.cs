using System;
using System.IO;
using System.Windows;

using Ecng.Serialization;
using Ecng.Configuration;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Logging;
using StockSharp.Messages;
using StockSharp.Xaml;

namespace Run_strategy_in_real_trading;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private Security _security;
	private CandleSeries _candleSeries;
	private Strategy _strategy;
	private readonly LogManager _logManager;
	private readonly Connector _connector = new();
	private const string _connectorFile = "ConnectorFile.json";
	public MainWindow()
	{
		InitializeComponent();

		// registering all connectors
		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_connector.Adapter.InnerAdapters));

		if (File.Exists(_connectorFile))
		{
			_connector.Load(_connectorFile.Deserialize<SettingsStorage>());
		}
		_logManager = new LogManager();
		_logManager.Listeners.Add(new GuiLogListener(Monitor));

		CandleSettingsEditor.DataType = DataType.TimeFrame(TimeSpan.FromSeconds(10));
	}

	private void Setting_Click(object sender, RoutedEventArgs e)
	{
		if (_connector.Configure(this))
		{
			_connector.Save().Serialize(_connectorFile);
		}
	}

	private void Connect_Click(object sender, RoutedEventArgs e)
	{
		SecurityEditor.SecurityProvider = _connector;
		PortfolioEditor.Portfolios = new PortfolioDataSource(_connector);
		_connector.PortfolioReceived += (sub, pf) => PortfolioGrid.Positions.Add(pf);
		_connector.PositionReceived += (sub, pos) => PortfolioGrid.Positions.Add(pos);

		_connector.Connect();
	}

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		if (PortfolioEditor.SelectedPortfolio == null) return;
		if (SecurityEditor.SelectedSecurity == null) return;
		_security = SecurityEditor.SelectedSecurity;

		_candleSeries = CandleSettingsEditor.DataType.ToCandleSeries(_security);
		_candleSeries.BuildCandlesMode = MarketDataBuildModes.Build;
		_candleSeries.BuildCandlesFrom2 = DataType.Ticks;

		// uncomment required strategy
		//_strategy = new MqSpreadStrategy()
		//_strategy = new MqStrategy()
		_strategy = new StairsCountertrendStrategy(_candleSeries)
		{
			Security = _security,
			Connector = _connector,
			Portfolio = PortfolioEditor.SelectedPortfolio,

		};
		_logManager.Sources.Add(_strategy);
		_strategy.NewMyTrade += MyTradeGrid.Trades.Add;
		_strategy.OrderRegistered += OrderGrid.Orders.Add;

		_strategy.Start();
	}

	private void Stop_Click(object sender, RoutedEventArgs e)
	{
		_strategy.Stop();
	}
}