namespace StockSharp.Samples.Basic.Orders;

using System.Windows;
using System.IO;

using Ecng.Serialization;
using Ecng.Configuration;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml;

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
		SecurityEditor.SecurityProvider = _connector;
		PortfolioEditor.Portfolios = new PortfolioDataSource(_connector);

		_connector.NewOrder += OrderGrid.Orders.Add;
		_connector.OrderRegisterFailed += OrderGrid.AddRegistrationFail;

		_connector.NewMyTrade += MyTradeGrid.Trades.Add;

		_connector.Connect();
	}

	private void Buy_Click(object sender, RoutedEventArgs e)
	{
		var order = new Order
		{
			Security = SecurityEditor.SelectedSecurity,
			Portfolio = PortfolioEditor.SelectedPortfolio,
			Price = decimal.Parse(TextBoxPrice.Text),
			Volume = 1,
			Side = Sides.Buy,
		};

		_connector.RegisterOrder(order);
	}


	private void Sell_Click(object sender, RoutedEventArgs e)
	{
		var order = new Order
		{
			Security = SecurityEditor.SelectedSecurity,
			Portfolio = PortfolioEditor.SelectedPortfolio,
			Price = decimal.Parse(TextBoxPrice.Text),
			Volume = 1,
			Side = Sides.Sell,
		};

		_connector.RegisterOrder(order);
	}
}