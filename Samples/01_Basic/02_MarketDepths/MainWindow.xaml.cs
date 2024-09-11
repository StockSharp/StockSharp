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
using StockSharp.DarkHorse;
using System.Threading;
using System.Security;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private readonly Connector _connector = new();
	private const string _connectorFile = "ConnectorFile.json";

	private readonly List<Subscription> _subscriptions = new();
	private SecurityId? _selectedSecurityId;

    private DarkHorseMessageAdapter darkhorseMessageAdapter;

    public class DarkHorseIdGenerator : Ecng.Common.IdGenerator
    {
        private long _currentId;

        public DarkHorseIdGenerator()
        {
            _currentId = 1;
        }

        public override long GetNextId()
        {
            return Interlocked.Increment(ref _currentId);
        }
    }

    public MainWindow()
	{
		InitializeComponent();
        InitDarkHorseMessageAdapter();

        // registering all connectors
        ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_connector.Adapter.InnerAdapters));

		if (File.Exists(_connectorFile))
		{
			_connector.Load(_connectorFile.Deserialize<SettingsStorage>());
		}


    }

    private static SecureString ToSecureString(string str)
    {
        var secureString = new SecureString();
        foreach (char c in str)
        {
            secureString.AppendChar(c);
        }
        secureString.MakeReadOnly();
        return secureString;
    }

    private void InitDarkHorseMessageAdapter()
    {
        darkhorseMessageAdapter = new DarkHorseMessageAdapter(new DarkHorseIdGenerator());
        var apiKey = ToSecureString("angelpie"); // Replace with your actual API key
        var apiSecret = ToSecureString("orion"); // Replace with your actual API secret

        darkhorseMessageAdapter.Key = apiKey;
        darkhorseMessageAdapter.Secret = apiSecret;

        // Add the Coinbase adapter to the connector
        _connector.Adapter.InnerAdapters.Add(darkhorseMessageAdapter);
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