#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Terminal.TerminalPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 3:22 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License

using System;
using System.IO;
using System.Linq;
using System.Windows;
using Ecng.Collections;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Xaml;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Terminal.Layout;
using StockSharp.Xaml;
using StockSharp.Terminal.Controls;
using StockSharp.Terminal.Logics;

namespace StockSharp.Terminal
{
	public partial class MainWindow
	{
		private readonly SecuritiesView _secView;

		public readonly SynchronizedDictionary<Security, MarketDepthControl> Depths =
			new SynchronizedDictionary<Security, MarketDepthControl>();

		private const string _settingsFolder = "Settings";
		private readonly string _connectionFile;
		private readonly LayoutManager _layoutManager;

		public MainWindow()
		{
			InitializeComponent();

			//LayoutManager = new LayoutManager(this, ProgrammaticDockSite) { LayoutFile = Path.Combine(_settingsFolder, "layout.xml") };
			_layoutManager = new LayoutManager(DockingManager);
			
			ConnectCommand = new DelegateCommand(Connect, CanConnect);
			SettingsCommand = new DelegateCommand(Settings, CanSettings);

			Directory.CreateDirectory(_settingsFolder);

			var storageRegistry = new StorageRegistry {DefaultDrive = new LocalMarketDataDrive(_settingsFolder)};

			Connector = new Connector();
			var storageAdapter = new StorageMessageAdapter(Connector.Adapter, new EntityRegistry(), storageRegistry);
			ConfigManager.RegisterService<ISecurityProvider>(new FilterableSecurityProvider(storageRegistry.GetSecurityStorage()));
			ConfigManager.RegisterService<IConnector>(Connector);
			ConfigManager.RegisterService<IMarketDataProvider>(Connector);

			_connectionFile = Path.Combine(_settingsFolder, "connection.xml");

			if (File.Exists(_connectionFile))
				Connector.Adapter.Load(new XmlSerializer<SettingsStorage>().Deserialize(_connectionFile));

			_secView = new SecuritiesView(this) {SecurityGrid = {MarketDataProvider = Connector}};

			Connector.MarketDepthsChanged += depths =>
			{
				foreach (var depth in depths)
				{
					var ctrl = Depths.TryGetValue(depth.Security);

					if (ctrl != null)
						ctrl.UpdateDepth(depth);
				}
			};
		}

		public LayoutManager LayoutManager { get; set; }

		public Connector Connector { private set; get; }

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

		private void DockSite_OnLoaded(object sender, RoutedEventArgs e)
		{
			//var dockSite = sender as DockSite;
			//if (dockSite == null)
			//	return;

			//CreateToolWindow(LocalizedStrings.Securities, "Securities", _secView);
			//CreateToolWindow(LocalizedStrings.Str972, "Positions", new PortfolioGrid());
			//CreateToolWindow(LocalizedStrings.Ticks, "Trades", new TradeGrid());
			//CreateToolWindow(LocalizedStrings.Orders, "Orders", new OrderGrid());
			//CreateToolWindow(LocalizedStrings.MyTrades, "MyTrades", new MyTradeGrid());
			//CreateToolWindow(LocalizedStrings.OrderLog, "OrderLog", new OrderLogGrid());
			//CreateToolWindow(LocalizedStrings.News, "News", new NewsGrid());

			//_isLoaded = true;
		}

		//private void ProgrammaticDockSite_OnLoaded(object sender, RoutedEventArgs e)
		//{
		//	var dockSite = sender as DockSite;
		//	if (dockSite == null)
		//		return;

		//	LayoutManager.AddTabbedMdiHost(dockSite);

		//	var docWindow1 = LayoutManager.CreateDocumentWindow(dockSite, LayoutKey.Window, "Chart title", null, new ChartPanel());
		//	docWindow1.Activate(true);

		//	// Top right
		//	var twNews = LayoutManager.CreateToolWindow(LayoutKey.OrderLog, "News", LocalizedStrings.News, new NewsGrid(), true);
		//	LayoutManager.DockToolWindowToDockSite(dockSite, twNews, Dock.Right);

		//	// Bottom left
		//	var twSecurities = LayoutManager.CreateToolWindow(LayoutKey.Security, "Securities", LocalizedStrings.Securities,
		//		_secView, true);
		//	LayoutManager.DockToolWindowToDockSite(dockSite, twSecurities, Dock.Bottom);

		//	var twMyTrades = LayoutManager.CreateToolWindow(LayoutKey.Trade, "MyTrades", LocalizedStrings.MyTrades,
		//		new MyTradeGrid(), true);
		//	LayoutManager.DockToolWindowToToolWindow(twSecurities, twMyTrades, Direction.Content);

		//	// Bottom right
		//	var twOrders = LayoutManager.CreateToolWindow(LayoutKey.Order, "Orders", LocalizedStrings.Orders, new OrderGrid(),
		//		true);
		//	LayoutManager.DockToolWindowToToolWindow(twSecurities, twOrders, Direction.ContentRight);

		//	var twOrderLog = LayoutManager.CreateToolWindow(LayoutKey.OrderLog, "OrderLog", LocalizedStrings.OrderLog,
		//		new OrderLogGrid(), true);
		//	LayoutManager.DockToolWindowToToolWindow(twOrders, twOrderLog, Direction.Content);

		//	// Right bottom
		//	var twPositions = LayoutManager.CreateToolWindow(LayoutKey.Portfolio, "Positions", LocalizedStrings.Str972,
		//		new PortfolioGrid(), true);
		//	LayoutManager.DockToolWindowToToolWindow(twNews, twPositions, Direction.ContentBottom);

		//	var twTrades = LayoutManager.CreateToolWindow(LayoutKey.Trade, "Trades", LocalizedStrings.Ticks, new TradeGrid(),
		//		true);
		//	LayoutManager.DockToolWindowToToolWindow(twPositions, twTrades, Direction.Content);

		//	LayoutManager.IsLoaded = true;
		//}

        private void DockingManager_OnActiveContentChanged(object sender, EventArgs e)
        {
            DockingManager.ActiveContent.DoIfElse<DiagramEditorControl>(editor =>
            {
                //RibbonDesignerTab.DataContext = editor.Composition;
                //DesignerRibbonGroup.Visibility = Visibility.Visible;
                //Ribbon.SelectedTabItem = RibbonDesignerTab;
                //CompositionNameTextBox.SetBindings(TextBox.TextProperty, editor.Composition.Element, "Name");
            }, () =>
            {
                //DesignerRibbonGroup.Visibility = Visibility.Collapsed;
                //RibbonDesignerTab.DataContext = null;
                //BindingOperations.ClearBinding(CompositionNameTextBox, TextBox.TextProperty);
            });

            DockingManager.ActiveContent.DoIfElse<EmulationControl>(editor =>
            {
                //RibbonEmulationTab.DataContext = editor;
                //EmulationRibbonGroup.Visibility = Visibility.Visible;
                //Ribbon.SelectedTabItem = RibbonEmulationTab;
            }, () =>
            {
                //EmulationRibbonGroup.Visibility = Visibility.Collapsed;
                //RibbonEmulationTab.DataContext = null;
            });

            DockingManager.ActiveContent.DoIfElse<SolutionExplorerControl>(editor =>
            {
                //Ribbon.SelectedTabItem = RibbonCommonTab;
            }, () =>
            {

            });
        }

  //      private void DockSite_OnWindowClosed(object sender, DockingWindowEventArgs e)
		//{
		//	LayoutManager.ToolItems.Remove(e.Window);
		//}

		protected override void OnClosed(EventArgs e)
		{
			//LayoutSerializer.SaveToFile(LayoutManager.LayoutFile, DockSite1);
			base.OnClosed(e);
		}

		//private static DockSiteLayoutSerializer LayoutSerializer
		//{
		//	get
		//	{
		//		return new DockSiteLayoutSerializer
		//		{
		//			SerializationBehavior = DockSiteSerializationBehavior.All,
		//			DocumentWindowDeserializationBehavior = DockingWindowDeserializationBehavior.AutoCreate,
		//			ToolWindowDeserializationBehavior = DockingWindowDeserializationBehavior.LazyLoad
		//		};
		//	}
		//}

		private void OpenComposition(CompositionItem item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			var content = new DiagramEditorControl
			{
				Composition = item
			};

			_layoutManager.OpenDocumentWindow(content);
		}

		private void SolutionExplorer_OnOpen(CompositionItem element)
		{
			OpenComposition(element);
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			//if (File.Exists(LayoutManager.LayoutFile))
			//	LayoutSerializer.LoadFromFile(LayoutManager.LayoutFile, DockSite1);
		}
	}
}