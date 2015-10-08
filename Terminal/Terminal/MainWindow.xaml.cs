namespace Terminal
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.IO;

    using ActiproSoftware.Windows;
    using ActiproSoftware.Windows.Controls.Docking;

    using Ecng.Xaml;
    using Ecng.Serialization;

    using MoreLinq;

    using StockSharp.Algo;
    using StockSharp.BusinessEntities;
    using StockSharp.Localization;
    using StockSharp.Messages;
    using StockSharp.Quik;
    using StockSharp.Xaml;
    using StockSharp.Xaml.Charting;
 
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
    using StockSharp.Micex;
    using StockSharp.Oanda;
    using StockSharp.OpenECry;
    using StockSharp.Plaza;
    using StockSharp.Quik.Lua;
    using StockSharp.Rithmic;
    using StockSharp.Rss;
    using StockSharp.SmartCom;
    using StockSharp.Sterling;
    using StockSharp.Transaq;


    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        private readonly Root _root;

        private readonly SecuritiesView _secView;

        private int _lastChartWindowId;

        private int _lastDepthWindowId;

        private DeferrableObservableCollection<DockingWindow> _toolItems;

        public MainWindow()
        {
            InitializeComponent();

            _root = Root.GetInstance();

            _secView = new SecuritiesView(this) { SecurityGrid = { MarketDataProvider = _root.Connector } };
            ToolItems.Add(CreateToolWindow(LocalizedStrings.Securities, "SecuritiesWindow", _secView));

            ConnectCommand = new DelegateCommand(Connect, CanConnect);
            SettingsCommand = new DelegateCommand(Settings, CanSettings);

            if (File.Exists("connection.xml"))
                _root.Connector.Adapter.Load(new XmlSerializer<SettingsStorage>().Deserialize("connection.xml"));

        }

        public DeferrableObservableCollection<DockingWindow> ToolItems
        {
            get
            {
                if (_toolItems == null)
                    ToolItems = new DeferrableObservableCollection<DockingWindow>();
                return _toolItems;
            }
            set { _toolItems = value; }
        }

        public DelegateCommand ConnectCommand { private set; get; }

        private void Connect(object obj)
        {
            switch (_root.Connector.ConnectionState)
            {
                case ConnectionStates.Disconnected:
                    Connect();
                    break;
                case ConnectionStates.Connected:
                    _root.Connector.Disconnect();
                    break;
                default:
                    break;
            }
        }

        private bool CanConnect(object obj)
        {
            return true;
        }

        public DelegateCommand SettingsCommand { private set; get; }
     
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

		private bool CanSettings(object obj)
	    {
		    return true;
	    }
        private static void AddConnectorInfo(ConnectorWindow wnd, Type adapterType)
        {
            wnd.ConnectorsInfo.Add(new ConnectorInfo(adapterType));
        }

        private void Connect()
        {
            _root.Connector.Connected += () => ConnectButton.Content = LocalizedStrings.Disconnect;

            _root.Connector.Disconnected += () => ConnectButton.Content = LocalizedStrings.Connected;

            _root.Connector.NewSecurities += securities => _secView.SecurityGrid.Securities.AddRange(securities);

            _root.Connector.MarketDepthsChanged += depths =>
            {
                depths.ForEach(depth =>
                {
                    var depthControls = ToolItems.Where(w => w.Content.GetType() == typeof(MarketDepthControl) &&
                                                             w.Title == depth.Security.Id).Select(w => w.Content);

                    this.GuiAsync(() => depthControls.ForEach(dc => ((MarketDepthControl)dc).UpdateDepth(depth)));
                }
                    );
            };

            _root.Connector.Connect();
        }

        public ToolWindow CreateToolWindow(string title, string name, object content)
        {
            return new ToolWindow(DockSite)
            {
                Name = name,
                Title = title,
                Content = content
            };
        }

        public void CreateNewChart(Security security)
        {
            if (security == null)
                return;

            _lastChartWindowId++;

            var wnd = CreateToolWindow(security.Id, string.Format("ChartWindow{0}", _lastChartWindowId), new ChartPanel());

            ToolItems.Add(wnd);

            OpenDockingWindow(wnd);
        }

        public void CreateNewMarketDepth(Security security)
        {
            if (security == null)
                return;

            _lastDepthWindowId++;

            if (! _root.Connector.RegisteredMarketDepths.Contains(security))
                 _root.Connector.RegisterMarketDepth(security);

            var depthControl = new MarketDepthControl();
            depthControl.UpdateFormat(security);

            var wnd = CreateToolWindow(security.Id, string.Format("DepthWindow{0}", _lastDepthWindowId), depthControl);

            ToolItems.Add(wnd);

            OpenDockingWindow(wnd);
        }

        private void OpenDockingWindow(DockingWindow dockingWindow)
        {
            if (dockingWindow.IsOpen)
                return;

            var toolWindow = dockingWindow as ToolWindow;

            if (toolWindow != null)
                toolWindow.Dock(DockSite, Dock.Top);
        }

        private void DockSite_OnLoaded(object sender, RoutedEventArgs e)
        {
            var dockSite = sender as DockSite;
            if (dockSite == null)
                return;
            ToolItems.ForEach(OpenDockingWindow);
        }

        private void DockSite_OnWindowClosed(object sender, DockingWindowEventArgs e)
        {
            if (e.Window.Content.GetType() != typeof(MarketDepthControl) && e.Window.Content.GetType() != typeof(ChartPanel))
                return;
            if (ToolItems.Contains(e.Window))
                ToolItems.Remove(e.Window);
        }


        #region INotifyPropertyChanged releases

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}