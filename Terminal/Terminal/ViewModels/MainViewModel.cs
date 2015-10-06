namespace Terminal
{
    using System;
    using System.IO;
    using System.Windows.Controls;

    using ActiproSoftware.Windows;

    using Ecng.Xaml;
	using Ecng.Serialization;

    using StockSharp.AlfaDirect;
    using StockSharp.BarChart;
    using StockSharp.BitStamp;
    using StockSharp.Blackwood;
    using StockSharp.Btce;
    using StockSharp.CQG;
    using StockSharp.ETrade;
    using StockSharp.Fix;
    using StockSharp.InteractiveBrokers;
    using StockSharp.IQFeed;
    using StockSharp.ITCH;
    using StockSharp.LMAX;
    using StockSharp.Messages;
    using StockSharp.Localization;
    using StockSharp.Micex;
    using StockSharp.Oanda;
    using StockSharp.OpenECry;
    using StockSharp.Plaza;
    using StockSharp.Quik;
    using StockSharp.Quik.Lua;
    using StockSharp.Rithmic;
    using StockSharp.Rss;
    using StockSharp.SmartCom;
    using StockSharp.Sterling;
    using StockSharp.Transaq;
    using StockSharp.Xaml;

	internal class MainViewModel : ViewModelBase
    {
        private readonly Root _root;

        public MainViewModel()
        {
            _root = Root.GetInstance();

            ToolItemViewModel viewModel = new SecuritiesViewModel();
            viewModel.DefaultDock = Dock.Top;
            viewModel.Title = LocalizedStrings.Securities;
            ToolItems.Add(viewModel);

            ConnectCommand = new DelegateCommand(Connect, CanConnect);
			SettingsCommand = new DelegateCommand(Settings, CanSettings);

            _root.Connector.Connected += () => NotifyPropertyChanged("ConnectionState");
            _root.Connector.Disconnected += () => NotifyPropertyChanged("ConnectionState");

			if (File.Exists("connection.xml"))
				_root.Connector.Adapter.Load(new XmlSerializer<SettingsStorage>().Deserialize("connection.xml"));
        }

	    private void Settings(object obj)
	    {
		    var wnd = new ConnectorWindow();

			AddConnectorInfo(wnd, typeof(AlfaDirectMessageAdapter));
			AddConnectorInfo(wnd, typeof(BarChartMessageAdapter));
			AddConnectorInfo(wnd, typeof(BitStampMessageAdapter));
			AddConnectorInfo(wnd, typeof(BlackwoodMessageAdapter));
			AddConnectorInfo(wnd, typeof(BtceMessageAdapter));
			AddConnectorInfo(wnd, typeof(CQGMessageAdapter));
			AddConnectorInfo(wnd, typeof(ETradeMessageAdapter));
			AddConnectorInfo(wnd, typeof(FixMessageAdapter));
			AddConnectorInfo(wnd, typeof(InteractiveBrokersMessageAdapter));
			AddConnectorInfo(wnd, typeof(IQFeedMarketDataMessageAdapter));
			AddConnectorInfo(wnd, typeof(ItchMessageAdapter));
			AddConnectorInfo(wnd, typeof(LmaxMessageAdapter));
			AddConnectorInfo(wnd, typeof(MicexMessageAdapter));
			AddConnectorInfo(wnd, typeof(OandaMessageAdapter));
			AddConnectorInfo(wnd, typeof(OpenECryMessageAdapter));
			AddConnectorInfo(wnd, typeof(PlazaMessageAdapter));
			AddConnectorInfo(wnd, typeof(LuaFixTransactionMessageAdapter));
			AddConnectorInfo(wnd, typeof(LuaFixMarketDataMessageAdapter));
			AddConnectorInfo(wnd, typeof(QuikTrans2QuikAdapter));
			AddConnectorInfo(wnd, typeof(QuikDdeAdapter));
			AddConnectorInfo(wnd, typeof(RithmicMessageAdapter));
			AddConnectorInfo(wnd, typeof(RssMarketDataMessageAdapter));
			AddConnectorInfo(wnd, typeof(SmartComMessageAdapter));
			AddConnectorInfo(wnd, typeof(SterlingMessageAdapter));
			AddConnectorInfo(wnd, typeof(TransaqMessageAdapter));
			wnd.Adapter = _root.Connector.Adapter;

			// TODO
		    if (wnd.ShowModal())
		    {
			    _root.Connector.Adapter.Load(wnd.Adapter.Save());
				new XmlSerializer<SettingsStorage>().Serialize(_root.Connector.Adapter.Save(), "connection.xml");
		    }
	    }

		private static void AddConnectorInfo(ConnectorWindow wnd, Type adapterType)
		{
			wnd.ConnectorsInfo.Add(new ConnectorInfo(adapterType));
		}

	    private bool CanSettings(object obj)
	    {
		    return true;
	    }

	    public DeferrableObservableCollection<ToolItemViewModel> ToolItems
        {
            get { return _root.ToolItems; }
        }

        public ConnectionStates ConnectionState
        {
            get { return _root.Connector.ConnectionState; }
        }

        public DelegateCommand ConnectCommand { private set; get; }
		public DelegateCommand SettingsCommand { private set; get; }

        private void Connect(object obj)
        {
            switch (_root.Connector.ConnectionState)
            {
                case ConnectionStates.Disconnected:
                    _root.Connector.Connect();
                    break;
                case ConnectionStates.Connected:
                    _root.Connector.Disconnect();
                    break;
                case ConnectionStates.Disconnecting:
                    break;
                case ConnectionStates.Connecting:
                    break;
                case ConnectionStates.Failed:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool CanConnect(object obj)
        {
            return true;
        }
    }
}