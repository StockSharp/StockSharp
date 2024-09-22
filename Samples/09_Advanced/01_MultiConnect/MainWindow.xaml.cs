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
using StockSharp.DarkHorse;
using System.Threading;
using System.Security;
using System;

public partial class MainWindow
{
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

        var connector = MainPanel.Connector;

        if (connector == null)
            return;
        // Add the Coinbase adapter to the connector
        connector.Adapter.InnerAdapters.Add(darkhorseMessageAdapter);
    }

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
        InitDarkHorseMessageAdapter();

        connector.Adapter.StorageSettings.Drive = new LocalMarketDataDrive(path.ToFullPath());
        connector.LookupAll();

        var portfolios = connector.Portfolios;
        // Assuming 'connector' is already initialized and contains portfolios
        foreach (var portfolio in connector.Portfolios)
        {
            Console.WriteLine($"Portfolio Name: {portfolio.Name}");
        }
    }
}