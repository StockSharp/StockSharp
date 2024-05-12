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

namespace MarketDepths_Trades;

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

		_subscriptions.Add(_connector.SubscribeLevel1(security));

		//-----------------TradeGrid-----------------------
		_subscriptions.Add(_connector.SubscribeTrades(security));

		//-----------------MarketDepth--------------------------
		MarketDepthControl.Clear();

		_subscriptions.Add(_connector.SubscribeMarketDepth(security));
	}
}