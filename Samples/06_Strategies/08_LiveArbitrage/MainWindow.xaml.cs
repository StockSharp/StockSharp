namespace StockSharp.Samples.Strategies.LiveArbitrage;

using System.IO;
using System.Windows;

using Ecng.Serialization;
using Ecng.Xaml;
using Ecng.Configuration;
using Ecng.Collections;
using Ecng.Logging;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Configuration;
using StockSharp.Xaml;

public partial class MainWindow
{
	private ArbitrageStrategy _strategy;
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
		SecurityEditor1.SecurityProvider = _connector;
		PortfolioEditor1.Portfolios = new PortfolioDataSource(_connector);
		SecurityEditor2.SecurityProvider = _connector;
		PortfolioEditor2.Portfolios = new PortfolioDataSource(_connector);
		_connector.PortfolioReceived += (sub, portfolio) =>
		{
			PortfolioGrid.Positions.TryAdd(portfolio);
		};

		_connector.SecurityReceived += (sub, security) =>
		{
			this.GuiAsync(() =>
			{
				if (SecurityEditor1.SelectedSecurity is null)
					SecurityEditor1.SelectedSecurity = security;
				else if (SecurityEditor2.SelectedSecurity is null)
					SecurityEditor2.SelectedSecurity = security;
			});
		};

		_connector.PositionReceived += (sub, pos) => PortfolioGrid.Positions.TryAdd(pos);

		_connector.Connect();
	}

	private void Start_Click(object sender, RoutedEventArgs e)
	{
		if (PortfolioEditor1.SelectedPortfolio == null) return;
		if (SecurityEditor1.SelectedSecurity == null) return;

		if (PortfolioEditor2.SelectedPortfolio == null) return;
		if (SecurityEditor2.SelectedSecurity == null) return;

		_strategy = new ArbitrageStrategy
		{
			Connector = _connector,
			Security = SecurityEditor1.SelectedSecurity,
			Portfolio = PortfolioEditor1.SelectedPortfolio,

			FutureSecurity = SecurityEditor1.SelectedSecurity,
			StockSecurity = SecurityEditor2.SelectedSecurity,
			FuturePortfolio = PortfolioEditor1.SelectedPortfolio,
			StockPortfolio = PortfolioEditor2.SelectedPortfolio,

			FutureVolume = 1,
			StockVolume = 1,

			ProfitToExit = -0.05m,
			SpreadToGenerateSignal = 0.03m,
			StockMultiplicator = 1.26m
		};

		_strategy.OrderReceived += (s, o) => OrderGrid.Orders.TryAdd(o);
		_strategy.OrderRegisterFailReceived += (s, f) => OrderGrid.AddRegistrationFail(f);
		_strategy.OwnTradeReceived += (s, t) => MyTradeGrid.Trades.TryAdd(t);

		_logManager.Sources.Add(_strategy);

		_strategy.Start();
	}

	private void Stop_Click(object sender, RoutedEventArgs e)
	{
		_strategy.Stop();
	}
}