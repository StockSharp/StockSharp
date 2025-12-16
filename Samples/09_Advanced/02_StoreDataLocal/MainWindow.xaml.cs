namespace StockSharp.Samples.Advanced.MultiConnect;

using System.ComponentModel;
using System.IO;
using System.Windows;

using Ecng.Common;
using Ecng.Configuration;
using Ecng.ComponentModel;
using Ecng.Logging;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;

public partial class MainWindow
{
	private readonly ChannelExecutor _executor;

	public MainWindow()
	{
		InitializeComponent();
		Instance = this;

		Title = Title.Put("Connections with storage");

		_executor = new(ex => ex.LogError());
		_ = _executor.RunAsync(default);
	}

	private Connector MainPanel_OnCreateConnector(string path)
	{
		//HistoryPath.Folder = path;

		var entityRegistry = new CsvEntityRegistry(path, _executor);

		var exchangeInfoProvider = new StorageExchangeInfoProvider(entityRegistry);
		ConfigManager.RegisterService<IExchangeInfoProvider>(exchangeInfoProvider);
		ConfigManager.RegisterService<IBoardMessageProvider>(exchangeInfoProvider);

		var storageRegistry = new StorageRegistry(exchangeInfoProvider)
		{
			DefaultDrive = new LocalMarketDataDrive(path)
		};

		ConfigManager.RegisterService<IEntityRegistry>(entityRegistry);
		ConfigManager.RegisterService<IStorageRegistry>(storageRegistry);

		INativeIdStorage nativeIdStorage = new CsvNativeIdStorage(Path.Combine(path, "NativeId"), _executor);
		ConfigManager.RegisterService(nativeIdStorage);

		var snapshotRegistry = new SnapshotRegistry(Path.Combine(path, "Snapshots"));

		return new Connector(entityRegistry.Securities, entityRegistry.PositionStorage, exchangeInfoProvider, storageRegistry, snapshotRegistry, new StorageBuffer());
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		MainPanel.Close();

		AsyncHelper.Run(_executor.DisposeAsync);

		base.OnClosing(e);
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		ThemeExtensions.ApplyDefaultTheme();
	}

	public static MainWindow Instance { get; private set; }

	private void HistoryPath_OnFolderChanged(string path)
	{
		var connector = MainPanel.Connector;

		if (connector == null)
			return;

		connector.Adapter.StorageSettings.Drive = new LocalMarketDataDrive(path.ToFullPath());
		connector.LookupAll();
	}
}