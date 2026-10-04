namespace StockSharp.Samples.Strategies.LiveTerminal;

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Ecng.Common;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Xaml;
using Ecng.Collections;
using Ecng.Logging;
using Ecng.ComponentModel;
using Ecng.IO;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.Configuration;
using StockSharp.Localization;
using StockSharp.Messages;
using StockSharp.Xaml;
using StockSharp.BusinessEntities;

public partial class MainWindow
{
	private bool _isConnected;

	public readonly Connector Connector;
	public readonly LogManager LogManager;

	private readonly SecuritiesWindow _securitiesWindow;
	private readonly OrdersWindow _ordersWindow;
	private readonly PortfoliosWindow _portfoliosWindow;
	private readonly MyTradesWindow _myTradesWindow;
	private readonly StrategiesWindow _strategiesWindow;

	public static MainWindow Instance { get; private set; }

	private readonly string _settingsFile;

	private readonly ChannelExecutor _executor;
	private readonly IEntityRegistry _entityRegistry;
	private readonly ISnapshotRegistry _snapshotRegistry;
	private Task _loading = Task.CompletedTask;
	private bool _isClosing;
	private bool _isReleased;

	public readonly IFileSystem FileSystem = Paths.FileSystem;

	public MainWindow()
	{
		InitializeComponent();
		Instance = this;

		// connecting waits until the stored entities are read
		ConnectBtn.IsEnabled = false;

		Title = Title.Put(LocalizedStrings.Strategies);

		const string path = "Data";

		_settingsFile = Path.Combine(path, $"connection{Paths.DefaultSettingsExt}");

		LogManager = new LogManager();
		LogManager.Listeners.Add(new FileLogListener { LogDirectory = Path.Combine(path, "Logs") });
		LogManager.Listeners.Add(new GuiLogListener(Monitor));

		_executor = TimeSpan.FromSeconds(1).CreateExecutorAndRun(LogManager.Application.AddErrorLog);

		var fs = Paths.FileSystem;

		var entityRegistry = new CsvEntityRegistry(fs, path, _executor);

		ConfigManager.RegisterService<IEntityRegistry>(entityRegistry);

		var exchangeInfoProvider = new StorageExchangeInfoProvider(entityRegistry);
		ConfigManager.RegisterService<IExchangeInfoProvider>(exchangeInfoProvider);
		ConfigManager.RegisterService<IBoardMessageProvider>(exchangeInfoProvider);

		var storageRegistry = new StorageRegistry(exchangeInfoProvider)
		{
			DefaultDrive = new LocalMarketDataDrive(fs, Path.Combine(path, "Storage"))
		};

		var snapshotRegistry = new SnapshotRegistry(fs, Path.Combine(path, "Snapshots"));

		_entityRegistry = entityRegistry;
		_snapshotRegistry = snapshotRegistry;

		Connector = new Connector(entityRegistry.Securities, entityRegistry.PositionStorage, storageRegistry.ExchangeInfoProvider, storageRegistry, snapshotRegistry, new StorageBuffer())
		{
			Adapter =
			{
				StorageSettings =
				{
					Mode = StorageModes.Snapshot,
				}
			},
			CheckSteps = true,
		};
		LogManager.Sources.Add(Connector);

		_securitiesWindow = new SecuritiesWindow();
		_ordersWindow = new OrdersWindow();
		_portfoliosWindow = new PortfoliosWindow();
		_myTradesWindow = new MyTradesWindow();

		InitConnector();

		_strategiesWindow = new StrategiesWindow();

		_ordersWindow.MakeHideable();
		_myTradesWindow.MakeHideable();
		_strategiesWindow.MakeHideable();
		_securitiesWindow.MakeHideable();
		_portfoliosWindow.MakeHideable();
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		_loading = LoadAsync();
		await _loading;
	}

	private async Task LoadAsync()
	{
		ThemeExtensions.ApplyDefaultTheme();

		if (Connector.StorageAdapter != null)
		{
			await _entityRegistry.InitAsync(default);
			await _snapshotRegistry.InitAsync(default);

			if (_isClosing)
				return;

			Connector.LookupAll();
		}

		ConnectBtn.IsEnabled = true;

		try
		{
			if (_settingsFile.IsConfigExists(FileSystem))
			{
				var settings = await _settingsFile.DeserializeAsync<SettingsStorage>(FileSystem, default);

				var ctx = new ContinueOnExceptionContext();
				ctx.Error += ex => ex.LogError();

				using (ctx.ToScope())
					if (settings is not null)
						await Connector.LoadAsync(settings, default);
			}
		}
		catch (Exception ex)
		{
			ex.LogError();
		}

		if (_isClosing)
			return;

		await _strategiesWindow.LoadStrategiesAsync(Path.GetDirectoryName(_settingsFile), default);
	}

	private void InitConnector()
	{
		// subscribe on connection successfully event
		Connector.Connected += () =>
		{
			this.GuiAsync(() => ChangeConnectStatus(true));
		};

		// subscribe on connection error event
		Connector.ConnectionError += error => this.GuiAsync(() =>
		{
			ChangeConnectStatus(false);
			MessageBox.Show(this, error.ToString(), LocalizedStrings.ErrorConnection);
		});

		Connector.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

		// subscribe on error event
		Connector.Error += error =>
		{
			this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.DataProcessError));
		};

		// subscribe on error of market data subscription event
		Connector.SubscriptionFailed += (sub, error, isSubscribe) =>
		{
			this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.ErrorSubDetails.Put(sub.DataType, sub.SecurityId)));
		};

		Connector.SecurityReceived += (sub, s) => _securitiesWindow.SecurityPicker.Securities.Add(s);

		Connector.OrderReceived += (s, order) =>
		{
			_ordersWindow.OrderGrid.Orders.TryAdd(order);
			_securitiesWindow.ProcessOrder(order);
		};

		// put the registration error into order's table
		Connector.OrderRegisterFailReceived += (s, f) => _ordersWindow.OrderGrid.AddRegistrationFail(f);

		Connector.OwnTradeReceived += (s, t) => _myTradesWindow.TradeGrid.Trades.TryAdd(t);

		Connector.PositionReceived += (sub, p) => _portfoliosWindow.PortfolioGrid.Positions.TryAdd(p);

		// set market data provider
		_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;

		if (Connector.StorageAdapter == null)
			return;

		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(Connector.Adapter.InnerAdapters));
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

		// what is being loaded has to be over before the connector it loads into is released;
		// a failure of the load is reported by the load itself
		await Task.WhenAny(_loading);

		try
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_strategiesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();

			_securitiesWindow.Close();
			_strategiesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();

			await Connector.DisposeAsync();

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

	private async void SettingsClick(object sender, RoutedEventArgs e)
	{
		if (Connector.Configure(this))
			await (await Connector.SaveAsync(default)).SerializeAsync(FileSystem, _settingsFile, true, default);
	}

	private void ConnectClick(object sender, RoutedEventArgs e)
	{
		if (!_isConnected)
		{
			Connector.Connect();
		}
		else
		{
			Connector.Disconnect();
		}
	}

	private void ChangeConnectStatus(bool isConnected)
	{
		_isConnected = isConnected;
		ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
	}

	private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
	{
		ShowOrHide(_securitiesWindow);
	}

	private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
	{
		ShowOrHide(_portfoliosWindow);
	}

	private void ShowOrdersClick(object sender, RoutedEventArgs e)
	{
		ShowOrHide(_ordersWindow);
	}

	private void ShowStrategiesClick(object sender, RoutedEventArgs e)
	{
		ShowOrHide(_strategiesWindow);
	}

	private void ShowMyTradesClick(object sender, RoutedEventArgs e)
	{
		ShowOrHide(_myTradesWindow);
	}

	private static void ShowOrHide(Window window)
	{
		if (window == null)
			throw new ArgumentNullException(nameof(window));

		if (window.Visibility == Visibility.Visible)
			window.Hide();
		else
			window.Show();
	}
}