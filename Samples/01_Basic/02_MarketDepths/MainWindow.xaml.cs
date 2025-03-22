namespace StockSharp.Samples.Basic.MarketDepths;

using System.Collections.Generic;
using System.Windows;
using System.IO;

using Ecng.Serialization;
using Ecng.Configuration;

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

	private readonly List<Subscription> _subscriptions = new();
	private SecurityId? _selectedSecurityId;

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