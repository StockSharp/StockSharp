namespace StockSharp.Samples.Advanced.MultiConnect;

using System.ComponentModel;
using System.IO;

using Ecng.Common;
using Ecng.Configuration;

using StockSharp.Messages;
using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.BusinessEntities;

public partial class MainWindow
{
	public MainWindow()
	{
		InitializeComponent();
		Instance = this;

		Title = Title.Put("Connections with storage");
	}

	private Connector MainPanel_OnCreateConnector(string path)
	{
		//HistoryPath.Folder = path;

		var entityRegistry = new CsvEntityRegistry(path);

		var exchangeInfoProvider = new StorageExchangeInfoProvider(entityRegistry, false);
		ConfigManager.RegisterService<IExchangeInfoProvider>(exchangeInfoProvider);
		ConfigManager.RegisterService<IBoardMessageProvider>(exchangeInfoProvider);

		var storageRegistry = new StorageRegistry(exchangeInfoProvider)
		{
			DefaultDrive = new LocalMarketDataDrive(path)
		};

		ConfigManager.RegisterService<IEntityRegistry>(entityRegistry);
		ConfigManager.RegisterService<IStorageRegistry>(storageRegistry);

		INativeIdStorage nativeIdStorage = new CsvNativeIdStorage(Path.Combine(path, "NativeId"))
		{
			DelayAction = entityRegistry.DelayAction
		};
		ConfigManager.RegisterService(nativeIdStorage);

		var snapshotRegistry = new SnapshotRegistry(Path.Combine(path, "Snapshots"));

		return new Connector(entityRegistry.Securities, entityRegistry.PositionStorage, exchangeInfoProvider, storageRegistry, snapshotRegistry, new StorageBuffer());
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		MainPanel.Close();

		ServicesRegistry.EntityRegistry.DelayAction.DefaultGroup.WaitFlush(true);

		base.OnClosing(e);
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