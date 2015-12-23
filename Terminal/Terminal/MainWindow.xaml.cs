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
using Xceed.Wpf.AvalonDock.Layout;

namespace StockSharp.Terminal
{
	public partial class MainWindow
	{
		public readonly SynchronizedDictionary<Security, MarketDepthControl> Depths =
			new SynchronizedDictionary<Security, MarketDepthControl>();

		public DelegateCommand SettingsCommand { private set; get; }

		public LayoutManager LayoutManager { get; set; }

		public Connector Connector { private set; get; }

		public DelegateCommand ConnectCommand { private set; get; }

		private const string _settingsFolder = "Settings";
		private readonly string _connectionFile;
		private readonly LayoutManager _layoutManager;

		private int _countWorkArea = 2;

		public MainWindow()
		{
			InitializeComponent();

			//LayoutManager = new LayoutManager(this, ProgrammaticDockSite) { LayoutFile = Path.Combine(_settingsFolder, "layout.xml") };
			_layoutManager = new LayoutManager(DockingManager);
			
			ConnectCommand = new DelegateCommand(Connect, CanConnect);
			SettingsCommand = new DelegateCommand(Settings, CanSettings);

			AddDocumentElement.IsSelectedChanged += AddDocumentElement_IsSelectedChanged;

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

		private void AddDocumentElement_IsSelectedChanged(object sender, EventArgs e)
		{
			var element = (LayoutDocument)sender;

			if (!element.IsSelected)
				return;

			LayoutDocument newWorkArea = new LayoutDocument()
			{
				Title = "Рабочая область " + ++_countWorkArea,
				Content = new WorkAreaControl()
			};

			var offset = LayoutDocuments.Children.Count - 1;
			LayoutDocuments.Children.RemoveAt(offset);

			//var offset = LayoutDocuments.Children.Count - 2;
			LayoutDocuments.Children.Add(newWorkArea);
			LayoutDocuments.Children.Add(element);
			LayoutDocuments.SelectedContentIndex = offset;

			//var offset = LayoutDocuments.IndexOfChild(element);
			//LayoutDocuments.InsertChildAt(offset, newWorkArea);
		}

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
		
        private void DockingManager_OnActiveContentChanged(object sender, EventArgs e)
        {
            DockingManager.ActiveContent.DoIfElse<WorkAreaControl>(editor =>
            {
                //RibbonEmulationTab.DataContext = editor;
                //EmulationRibbonGroup.Visibility = Visibility.Visible;
                //Ribbon.SelectedTabItem = RibbonEmulationTab;
            }, () =>
            {
                //EmulationRibbonGroup.Visibility = Visibility.Collapsed;
                //RibbonEmulationTab.DataContext = null;
            });
        }
		
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
		}
		
		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{

		}
	}
}