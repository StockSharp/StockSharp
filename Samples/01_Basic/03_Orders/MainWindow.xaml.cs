namespace StockSharp.Samples.Basic.Orders;

using System.Windows;
using System.IO;

using Ecng.Serialization;
using Ecng.Configuration;
using Ecng.Collections;
using Ecng.IO;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Xaml;

public partial class MainWindow
{
	private readonly Connector _connector = new();
	private const string _connectorFile = "ConnectorFile.json";
	private readonly IFileSystem _fileSystem = Paths.FileSystem;

	public MainWindow()
	{
		InitializeComponent();

		// registering all connectors
		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_connector.Adapter.InnerAdapters));
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		ThemeExtensions.ApplyDefaultTheme();

		if (_fileSystem.FileExists(_connectorFile))
			await _connector.LoadAsync(await _connectorFile.DeserializeAsync<SettingsStorage>(_fileSystem, default), default);
	}

	private async void Setting_Click(object sender, RoutedEventArgs e)
	{
		if (_connector.Configure(this))
		{
			await (await _connector.SaveAsync(default)).SerializeAsync(_fileSystem, _connectorFile, true, default);
		}
	}

	private void Connect_Click(object sender, RoutedEventArgs e)
	{
		SecurityEditor.SecurityProvider = _connector;
		PortfolioEditor.Portfolios = new PortfolioDataSource(_connector);

		_connector.OrderReceived += (s, o) => OrderGrid.Orders.TryAdd(o);
		_connector.OrderRegisterFailReceived += (s, f) => OrderGrid.AddRegistrationFail(f);
		_connector.OwnTradeReceived += (s, t) => MyTradeGrid.Trades.TryAdd(t);

		_connector.Connect();
	}

	private void Buy_Click(object sender, RoutedEventArgs e)
	{
		var order = new Order
		{
			Security = SecurityEditor.SelectedSecurity,
			Portfolio = PortfolioEditor.SelectedPortfolio,
			Price = TextBoxPrice.Text.To<decimal>(),
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
			Price = TextBoxPrice.Text.To<decimal>(),
			Volume = 1,
			Side = Sides.Sell,
		};

		_connector.RegisterOrder(order);
	}
}