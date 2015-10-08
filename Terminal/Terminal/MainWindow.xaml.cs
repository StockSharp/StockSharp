namespace Terminal
{
    using System.ComponentModel;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Controls;

    using ActiproSoftware.Windows;
    using ActiproSoftware.Windows.Controls.Docking;

    using Ecng.Xaml;

    using MoreLinq;

    using StockSharp.Algo;
    using StockSharp.BusinessEntities;
    using StockSharp.Localization;
    using StockSharp.Messages;
    using StockSharp.Quik;
    using StockSharp.Xaml;
    using StockSharp.Xaml.Charting;

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private readonly Connector _connector;

        private readonly SecuritiesView _secView;
        private int _lastChartWindowId;

        private int _lastDepthWindowId;

        private DeferrableObservableCollection<DockingWindow> _toolItems;

        public MainWindow()
        {
            InitializeComponent();

            _connector = new QuikTrader();

            _secView = new SecuritiesView(this) { SecurityGrid = { MarketDataProvider = _connector } };
            ToolItems.Add(CreateToolWindow(LocalizedStrings.Securities, "SecuritiesWindow", _secView));

            ConnectCommand = new DelegateCommand(Connect, CanConnect);
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
            switch (_connector.ConnectionState)
            {
                case ConnectionStates.Disconnected:
                    Connect();
                    break;
                case ConnectionStates.Connected:
                    _connector.Disconnect();
                    break;
                default:
                    break;
            }
        }

        private bool CanConnect(object obj)
        {
            return true;
        }

        private void Connect()
        {
            _connector.Connected += () => ConnectButton.Content = LocalizedStrings.Disconnect;

            _connector.Disconnected += () => ConnectButton.Content = LocalizedStrings.Connected;

            _connector.NewSecurities += securities => _secView.SecurityGrid.Securities.AddRange(securities);

            _connector.MarketDepthsChanged += depths =>
            {
                depths.ForEach(depth =>
                {
                    var depthControls = ToolItems.Where(w => w.Content.GetType() == typeof(MarketDepthControl) &&
                                                             w.Title == depth.Security.Id).Select(w => w.Content);

                    this.GuiAsync(() => depthControls.ForEach(dc => ((MarketDepthControl)dc).UpdateDepth(depth)));
                }
                    );
            };

            _connector.Connect();
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

            if (!_connector.RegisteredMarketDepths.Contains(security))
                _connector.RegisterMarketDepth(security);

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