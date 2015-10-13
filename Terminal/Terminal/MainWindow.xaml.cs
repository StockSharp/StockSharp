namespace StockSharp.Terminal
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.IO;

	using ActiproSoftware.Windows.Controls.Docking;
	using ActiproSoftware.Windows.Controls.Docking.Serialization;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Xaml;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Configuration;

	public partial class MainWindow
	{
		private class StorageEntityFactory : Algo.EntityFactory
		{
			private readonly ISecurityStorage _securityStorage;
			private readonly Dictionary<string, Security> _securities;

			public StorageEntityFactory(ISecurityStorage securityStorage)
			{
				if (securityStorage == null)
					throw new ArgumentNullException("securityStorage");

				_securityStorage = securityStorage;
				_securities = _securityStorage.LookupAll().ToDictionary(s => s.Id, s => s, StringComparer.InvariantCultureIgnoreCase);
			}

			public override Security CreateSecurity(string id)
			{
				return _securities.SafeAdd(id, key =>
				{
					var s = base.CreateSecurity(id);
					_securityStorage.Save(s);
					return s;
				});
			}
		}

		private readonly SecuritiesView _secView;
		private int _lastChartWindowId;
		private int _lastDepthWindowId;
		private readonly SynchronizedDictionary<Security, MarketDepthControl> _depths = new SynchronizedDictionary<Security, MarketDepthControl>();
		private const string _settingsFolder = "Settings";
		private readonly string _connectionFile;
		private readonly string _layoutFile;
		private bool _isLoaded;

		public MainWindow()
		{
			InitializeComponent();

			ConnectCommand = new DelegateCommand(Connect, CanConnect);
			SettingsCommand = new DelegateCommand(Settings, CanSettings);

			Directory.CreateDirectory(_settingsFolder);

			var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(_settingsFolder) };
			var securityStorage = storageRegistry.GetSecurityStorage();
			
			Connector = new Connector { EntityFactory = new StorageEntityFactory(securityStorage) };
			ConfigManager.RegisterService(new FilterableSecurityProvider(securityStorage));
			ConfigManager.RegisterService<IConnector>(Connector);
			ConfigManager.RegisterService<IMarketDataProvider>(Connector);

			_connectionFile = Path.Combine(_settingsFolder, "connection.xml");
			_layoutFile = Path.Combine(_settingsFolder, "layout.xml");

			if (File.Exists(_connectionFile))
				Connector.Adapter.Load(new XmlSerializer<SettingsStorage>().Deserialize(_connectionFile));

			_secView = new SecuritiesView(this) { SecurityGrid = { MarketDataProvider = Connector } };

			Connector.NewSecurities += _secView.SecurityGrid.Securities.AddRange;

			Connector.MarketDepthsChanged += depths =>
			{
				foreach (var depth in depths)
				{
					var ctrl = _depths.TryGetValue(depth.Security);

					if (ctrl != null)
						ctrl.UpdateDepth(depth);
				}
			};
		}

		public Connector Connector { private set; get; }

		private readonly ObservableCollection<DockingWindow> _toolItems = new ObservableCollection<DockingWindow>();

		public ObservableCollection<DockingWindow> ToolItems
		{
			get { return _toolItems; }
		}

		public DelegateCommand ConnectCommand { private set; get; }

		private void Connect(object obj)
		{
			switch (Connector.ConnectionState)
			{
				case ConnectionStates.Failed:
				case ConnectionStates.Disconnected:
					Connect();
					break;
				case ConnectionStates.Connected:
					Connector.Disconnect();
					break;
			}
		}

		private bool CanConnect(object obj)
		{
			return Connector.Adapter.InnerAdapters.SortedAdapters.Any();
		}

		public DelegateCommand SettingsCommand { private set; get; }

		private void Settings(object obj)
		{
			if (!Connector.Configure(this))
				return;

			new XmlSerializer<SettingsStorage>().Serialize(Connector.Adapter.Save(), _connectionFile);
		}

		private bool CanSettings(object obj)
		{
			return true;
		}

		private void Connect()
		{
			Connector.Connect();
		}

		private void CreateToolWindow(string title, string name, object content, bool canClose = false)
		{
			var wnd = new ToolWindow(DockSite)
			{
				Name = name,
				Title = title,
				Content = content,
				CanClose = canClose
			};
			ToolItems.Add(wnd);
			OpenDockingWindow(wnd);
			//return wnd;
		}

		public void CreateNewChart(Security security)
		{
			if (security == null)
				return;

			_lastChartWindowId++;

			CreateToolWindow(security.Id, "Chart" + _lastChartWindowId, new ChartPanel(), true);
		}

		public void CreateNewMarketDepth(Security security)
		{
			if (security == null)
				return;

			_lastDepthWindowId++;

			if (!Connector.RegisteredMarketDepths.Contains(security))
				Connector.RegisterMarketDepth(security);

			var depthControl = new MarketDepthControl();
			depthControl.UpdateFormat(security);

			_depths.Add(security, depthControl);

			CreateToolWindow(security.Id, "Depth" + _lastDepthWindowId, depthControl, true);
		}

		private void OpenDockingWindow(DockingWindow dockingWindow)
		{
			if (dockingWindow.IsOpen)
				return;

			var toolWindow = dockingWindow as ToolWindow;

			if (toolWindow != null)
				toolWindow.Dock(DockSite, Dock.Top);

			if (!_isLoaded)
				return;

			LayoutSerializer.SaveToFile(_layoutFile, DockSite);
		}

		private void DockSite_OnLoaded(object sender, RoutedEventArgs e)
		{
			var dockSite = sender as DockSite;
			if (dockSite == null)
				return;

			CreateToolWindow(LocalizedStrings.Securities, "Securities", _secView);
			CreateToolWindow(LocalizedStrings.Str972, "Positions", new PortfolioGrid());
			CreateToolWindow(LocalizedStrings.Ticks, "Trades", new TradeGrid());
			CreateToolWindow(LocalizedStrings.Orders, "Orders", new OrderGrid());
			CreateToolWindow(LocalizedStrings.MyTrades, "MyTrades", new MyTradeGrid());
			CreateToolWindow(LocalizedStrings.OrderLog, "OrderLog", new OrderLogGrid());
			CreateToolWindow(LocalizedStrings.News, "News", new NewsGrid());

			_isLoaded = true;
		}

		private void DockSite_OnWindowClosed(object sender, DockingWindowEventArgs e)
		{
			ToolItems.Remove(e.Window);
		}

		protected override void OnClosed(EventArgs e)
		{
			LayoutSerializer.SaveToFile(_layoutFile, DockSite);
			base.OnClosed(e);
		}

		private static DockSiteLayoutSerializer LayoutSerializer
		{
			get
			{
				return new DockSiteLayoutSerializer
				{
					SerializationBehavior = DockSiteSerializationBehavior.All,
					DocumentWindowDeserializationBehavior = DockingWindowDeserializationBehavior.AutoCreate,
					ToolWindowDeserializationBehavior = DockingWindowDeserializationBehavior.LazyLoad
				};
			}
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (File.Exists(_layoutFile))
				LayoutSerializer.LoadFromFile(_layoutFile, DockSite);
		}
	}
}