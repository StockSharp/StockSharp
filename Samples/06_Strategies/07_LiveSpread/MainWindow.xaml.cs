namespace StockSharp.Samples.Strategies.LiveSpread;

using System;
using System.IO;
using System.Windows;

using Ecng.Serialization;
using Ecng.Configuration;
using Ecng.Collections;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml;

public partial class MainWindow
{
	private Security _security;
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

		CandleDataTypeEdit.DataType = TimeSpan.FromSeconds(10).TimeFrame();
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
		_connector.PortfolioReceived += (sub, pf) => PortfolioGrid.Positions.TryAdd(pf);
		_connector.PositionReceived += (sub, pos) => PortfolioGrid.Positions.TryAdd(pos);

		_connector.Connect();
	}

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		if (PortfolioEditor.SelectedPortfolio == null) return;
		if (SecurityEditor.SelectedSecurity == null) return;
		_security = SecurityEditor.SelectedSecurity;

		// uncomment required strategy
		//_strategy = new MqSpreadStrategy
		//_strategy = new MqStrategy
		_strategy = new StairsCountertrendStrategy
		{
			Security = _security,
			Connector = _connector,
			Portfolio = PortfolioEditor.SelectedPortfolio,
			CandleDataType = CandleDataTypeEdit.DataType,
		};
		_logManager.Sources.Add(_strategy);
		_strategy.OwnTradeReceived += (s, t) => MyTradeGrid.Trades.TryAdd(t);
		_strategy.OrderReceived += (s, o) => OrderGrid.Orders.TryAdd(o);

		_strategy.Start();
	}

	private void Stop_Click(object sender, RoutedEventArgs e)
	{
		_strategy.Stop();
	}
}