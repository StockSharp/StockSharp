using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

using ActiproSoftware.Windows.Controls.Docking;
using ActiproSoftware.Windows.Controls.Docking.Serialization;

using Ecng.Collections;
using Ecng.ComponentModel;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Xaml;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Configuration.ConfigManager;
using StockSharp.Localization;
using StockSharp.Logging;
using StockSharp.Messages;
using StockSharp.Terminal.Layout;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting;

namespace StockSharp.Terminal
{
	public partial class MainWindow
	{
		private SecuritiesView _secView;

		public readonly SynchronizedDictionary<Security, MarketDepthControl> Depths =
			new SynchronizedDictionary<Security, MarketDepthControl>();

		private const string _settingsFolder = "Settings";
		private string _connectionFile;

		public MainWindow()
		{
			InitializeComponent();

			ConnectCommand = new DelegateCommand(Connect, CanConnect);
			SettingsCommand = new DelegateCommand(Settings, CanSettings);
		}

	    public ConfigurationManager ConfigurationManager { get; set; }

	    public Connector Connector { private set; get; }

	    public LayoutManager LayoutManager { get; set; }


	    public DelegateCommand ConnectCommand { private set; get; }

	    /// <summary>
	    /// Create connector once dock site is loaded and <see cref="ConfigurationManager"/> has been created.
	    /// </summary>
	    private void CreateConnector(string settingsFolder, string connectionFile)
	    {
	        Directory.CreateDirectory(_settingsFolder);

	        var storageRegistry = new StorageRegistry { DefaultDrive = new LocalMarketDataDrive(settingsFolder) };

	        Connector = new Connector { EntityFactory = new StorageEntityFactory(new EntityRegistry(), storageRegistry) };
	        ConfigManager.RegisterService<ISecurityProvider>(new FilterableSecurityProvider(storageRegistry.GetSecurityStorage()));
	        ConfigManager.RegisterService<IConnector>(Connector);
	        ConfigManager.RegisterService<IMarketDataProvider>(Connector);

	        connectionFile = Path.Combine(settingsFolder, ConfigurationManager.FolderManager.ConnectionFileInfo.Name);

	        if (File.Exists(connectionFile))
	            Connector.Adapter.Load(new XmlSerializer<SettingsStorage>().Deserialize(connectionFile));
            
	        _secView = new SecuritiesView(this) {SecurityGrid = {MarketDataProvider = Connector}};

	        Connector.MarketDepthsChanged += depths =>
	        {
	            foreach (var depth in depths)
	            {
	                var ctrl = Depths.TryGetValue(depth.Security);

	                ctrl?.UpdateDepth(depth);
	            }
	        };
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
			return Connector != null && Connector.Adapter.InnerAdapters.SortedAdapters.Any();
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
		    var name = Assembly.GetExecutingAssembly().GetName().Name;

            ConfigurationManager = new ConfigurationManager(name, DockSite1);
            CreateConnector(ConfigurationManager.FolderManager.SettingsDirectory, ConfigurationManager.FolderManager.ConnectionFileInfo.Name);

		    if (File.Exists(ConfigurationManager.LayoutManager.LayoutFile.FullName))
		    {
		        try
		        {
		            ConfigurationManager.LayoutManager.LoadLayout();
		        }
		        catch (Exception exception)
		        {
		            exception.LogError();
		        }
		    }
		    else
		    {
		        int i = 1;
                // create a default or temp layout
                var wnd = ConfigurationManager.LayoutManager.CreateToolWindow("ToolWindow" + i++);
                wnd.Dock(ConfigurationManager.LayoutManager.DockSite, Direction.None);

                var temp = ConfigurationManager.LayoutManager.CreateToolWindow("ToolWindow" + i++);
		        temp.Name = "ToolWindow" + i;
		        temp.Title = temp.Name;
		        temp.Tag = temp.Name;
		        temp.Header = temp.Name;
                temp.Dock(wnd, Direction.ContentRight);
		        for (int j = 1; j <= 5; j++)
		        {
		            var tw = ConfigurationManager.LayoutManager.CreateToolWindow("ToolWindow" + i++);
		            tw.Name = "ToolWindow" + i;
		            tw.Title = tw.Name;
		            tw.Tag = tw.Name;
		            tw.Header = tw.Name;
                    tw.Dock(temp, j % 2 == 0 ? Direction.ContentTop : Direction.Content);
		            j++;
		        }

		    }

            ConfigurationManager.LayoutManager.Save(new SettingsStorage(), ConfigurationManager.FolderManager.ConnectionFileInfo.Name);
            ConfigurationManager.LayoutManager.Save(new SettingsStorage(), ConfigurationManager.FolderManager.LayoutFileInfo.Name);
            ConfigurationManager.LayoutManager.Save(new SettingsStorage(), ConfigurationManager.FolderManager.LogsFileInfo.Name);
            ConfigurationManager.LayoutManager.Save(new SettingsStorage(), ConfigurationManager.FolderManager.SettingFileInfo.Name);

        }

	    private void DockSite_OnWindowClosed(object sender, DockingWindowEventArgs e)
		{
			//LayoutManager.ToolItems.Remove(e.Window);
		}

		protected override void OnClosed(EventArgs e)
		{
			//LayoutSerializer.SaveToFile(LayoutManager.LayoutFileName, DockSite1);
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
			//if (File.Exists(LayoutManager.LayoutFileName))
			//	LayoutSerializer.LoadFromFile(LayoutManager.LayoutFileName, DockSite1);
		}
	}
}