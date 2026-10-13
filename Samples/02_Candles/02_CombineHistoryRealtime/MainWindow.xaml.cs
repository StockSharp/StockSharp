namespace StockSharp.Samples.Candles.CombineHistoryRealtime;

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Threading;

using Ecng.Common;
using Ecng.Serialization;
using Ecng.Configuration;
using Ecng.ComponentModel;
using Ecng.Logging;
using Ecng.IO;

using StockSharp.Configuration;
using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;
using StockSharp.Messages;
using StockSharp.Xaml.Charting;
using StockSharp.Charting;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
	private readonly Connector _connector;
	private const string _connectorFile = "ConnectorFile.json";

	private readonly string _pathHistory = Paths.HistoryDataPath;
	private readonly IFileSystem _fileSystem = Paths.FileSystem;

	private Subscription _subscription;
	private ChartCandleElement _candleElement;

	private readonly ChannelExecutor _executor;
	private bool _isClosing;
	private bool _isReleased;

	public MainWindow()
	{
		InitializeComponent();

		_executor = TimeSpan.FromSeconds(1).CreateExecutorAndRun(ex => ex.LogError());

		var entityRegistry = new CsvEntityRegistry(_fileSystem, _pathHistory, _executor);
		var storageRegistry = new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(_fileSystem, _pathHistory)
		};
		_connector = new Connector(entityRegistry.Securities, entityRegistry.PositionStorage, new InMemoryExchangeInfoProvider(), storageRegistry, new SnapshotRegistry(_fileSystem, "SnapshotRegistry"));

		// registering all connectors
		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_connector.Adapter.InnerAdapters));


		CandleDataTypeEdit.DataType = TimeSpan.FromMinutes(5).TimeFrame();
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		ThemeExtensions.ApplyDefaultTheme();

		if (_fileSystem.FileExists(_connectorFile))
			await _connector.LoadAsync(await _connectorFile.DeserializeAsync<SettingsStorage>(_fileSystem, default), default);
	}

	protected override async void OnClosing(CancelEventArgs e)
	{
		if (_isReleased)
		{
			base.OnClosing(e);
			return;
		}

		// the window stays open until the queued writes are on the disk
		e.Cancel = true;

		if (_isClosing)
			return;

		_isClosing = true;
		IsEnabled = false;

		try
		{
			await _executor.DisposeAsync();
		}
		finally
		{
			_isReleased = true;

			// a window refuses Close while it is still handling the first attempt to close it
			await Dispatcher.Yield();
			Close();
		}
	}

    private async void Setting_Click(object sender, RoutedEventArgs e)
	{
		if (_connector.Configure(this))
		{
			await (await _connector.SaveAsync(default)).SerializeAsync(_fileSystem, _connectorFile, true, default);
		}
	}

	private void Connect_Click(object sender, RoutedEventArgs e)
	{
		SecurityPicker.SecurityProvider = _connector;
		_connector.CandleReceived += Connector_CandleReceived;
		_connector.Connect();
	}

	private void Connector_CandleReceived(Subscription subscription, ICandleMessage candle)
	{
		Chart.Draw(_candleElement, candle);
	}

	private void SecurityPicker_SecuritySelected(Security security)
	{
		if (security == null) return;
		if (_subscription != null) _connector.UnSubscribe(_subscription);

		_subscription = new(CandleDataTypeEdit.DataType, security)
		{
			MarketData =
			{
				From = DateTime.UtcNow.Date.AddDays(-720),
				BuildMode = MarketDataBuildModes.LoadAndBuild,
			}
		};

		//-----------------Chart--------------------------------
		Chart.ClearAreas();

		var area = new ChartArea();
		_candleElement = new ChartCandleElement();

		Chart.AddArea(area);
		Chart.AddElement(area, _candleElement, _subscription);

		_connector.Subscribe(_subscription);
	}
}