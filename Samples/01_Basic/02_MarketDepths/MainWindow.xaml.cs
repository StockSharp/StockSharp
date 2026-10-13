namespace StockSharp.Samples.Basic.MarketDepths;

using System.Collections.Generic;
using System.Windows;
using System.IO;

using Ecng.Serialization;
using Ecng.Configuration;
using Ecng.IO;

using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private readonly Connector _connector = new();
	private const string _connectorFile = "ConnectorFile.json";
	private readonly IFileSystem _fileSystem = Paths.FileSystem;

	private readonly List<Subscription> _subscriptions = new();
	private SecurityId? _selectedSecurityId;

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
		SecurityPicker.SecurityProvider = _connector;
		SecurityPicker.MarketDataProvider = _connector;

		_connector.TickTradeReceived += ConnectorOnTickTradeReceived;
		_connector.OrderBookReceived += ConnectorOnMarketDepthReceived;

		_connector.Connect();
	}

	private void ConnectorOnMarketDepthReceived(Subscription sub, IOrderBookMessage depth)
	{
		if (depth.SecurityId == _selectedSecurityId)
			MarketDepthControl.UpdateDepth(depth);
	}

	private void ConnectorOnTickTradeReceived(Subscription sub, ITickTradeMessage trade)
	{
		if (trade.SecurityId == _selectedSecurityId)
			TradeGrid.Trades.Add(trade);
	}

	private void UnsubscribeAll()
	{
		foreach (var sub in _subscriptions)
			_connector.UnSubscribe(sub);

		_subscriptions.Clear();
	}

	private void SecurityPicker_SecuritySelected(Security security)
	{
		// cancel old subscriptions
		UnsubscribeAll();

		_selectedSecurityId = security?.ToSecurityId();

		//-----------------SecurityPicker-----------------------
		if (_selectedSecurityId == null)
			return;

		void subscribe(DataType dt)
		{
			var sub = new Subscription(dt, security);
			_subscriptions.Add(sub);
			_connector.Subscribe(sub);
		}

		subscribe(DataType.Level1);

		//-----------------TradeGrid-----------------------
		subscribe(DataType.Ticks);

		//-----------------MarketDepth--------------------------
		MarketDepthControl.Clear();

		subscribe(DataType.MarketDepth);
	}
}