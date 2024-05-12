using System.IO;
using System.Windows;

using Ecng.Serialization;
using Ecng.Configuration;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml;

namespace Multi_connection;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
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


		_connector.NewOrder += OrderGrid.Orders.Add;
		_connector.OrderRegisterFailed += OrderGrid.AddRegistrationFail;
		_connector.NewMyTrade += MyTradeGrid.Trades.Add;
		_connector.Connected += ConnectorOnConnected;
		_connector.Connect();
	}

	private void ConnectorOnConnected()
	{
		// try lookup all securities
		_connector.LookupSecurities(StockSharp.Messages.Extensions.LookupAllCriteriaMessage);

		_connector.SubscribePositions();
	}

	private void Buy_Click(object sender, RoutedEventArgs e)
	{
		var order = new Order
		{
			Security = SecurityEditor1.SelectedSecurity,
			Portfolio = PortfolioEditor1.SelectedPortfolio,
			Price = decimal.Parse(TextBoxPrice1.Text),
			Volume = 1,
			Side = Sides.Buy,
		};

		_connector.RegisterOrder(order);
	}


	private void Sell_Click(object sender, RoutedEventArgs e)
	{
		var order = new Order
		{
			Security = SecurityEditor1.SelectedSecurity,
			Portfolio = PortfolioEditor1.SelectedPortfolio,
			Price = decimal.Parse(TextBoxPrice1.Text),
			Volume = 1,
			Side = Sides.Sell,
		};

		_connector.RegisterOrder(order);
	}

	private void Buy2_Click(object sender, RoutedEventArgs e)
	{
		var order = new Order
		{
			Security = SecurityEditor2.SelectedSecurity,
			Portfolio = PortfolioEditor2.SelectedPortfolio,
			Price = decimal.Parse(TextBoxPrice2.Text),
			Volume = 1,
			Side = Sides.Buy,
		};

		_connector.RegisterOrder(order);
	}

	private void Sell2_Click(object sender, RoutedEventArgs e)
	{
		var order = new Order
		{
			Security = SecurityEditor2.SelectedSecurity,
			Portfolio = PortfolioEditor2.SelectedPortfolio,
			Price = decimal.Parse(TextBoxPrice1.Text),
			Volume = 1,
			Side = Sides.Sell,
		};

		_connector.RegisterOrder(order);
	}
}
