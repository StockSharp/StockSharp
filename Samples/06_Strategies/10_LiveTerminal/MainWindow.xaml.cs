namespace StockSharp.Samples.Strategies.LiveTerminal;

using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

using Ecng.Common;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Xaml;
using Ecng.Collections;
using Ecng.Logging;

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

	public MainWindow()
	{
		InitializeComponent();
		Instance = this;

		Title = Title.Put(LocalizedStrings.Strategies);

		const string path = "Data";

		_settingsFile = Path.Combine(path, $"connection{Paths.DefaultSettingsExt}");

		LogManager = new LogManager();
		LogManager.Listeners.Add(new FileLogListener { LogDirectory = Path.Combine(path, "Logs") });
		LogManager.Listeners.Add(new GuiLogListener(Monitor));

		var entityRegistry = new CsvEntityRegistry(path);

		ConfigManager.RegisterService<IEntityRegistry>(entityRegistry);

		var exchangeInfoProvider = new StorageExchangeInfoProvider(entityRegistry, false);
		ConfigManager.RegisterService<IExchangeInfoProvider>(exchangeInfoProvider);
		ConfigManager.RegisterService<IBoardMessageProvider>(exchangeInfoProvider);

		var storageRegistry = new StorageRegistry(exchangeInfoProvider)
		{
			DefaultDrive = new LocalMarketDataDrive(Path.Combine(path, "Storage"))
		};

		var snapshotRegistry = new SnapshotRegistry(Path.Combine(path, "Snapshots"));

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

		InitConnector(entityRegistry, snapshotRegistry);

		_strategiesWindow = new StrategiesWindow();
		_strategiesWindow.LoadStrategies(path);

		_ordersWindow.MakeHideable();
		_myTradesWindow.MakeHideable();
		_strategiesWindow.MakeHideable();
		_securitiesWindow.MakeHideable();
		_portfoliosWindow.MakeHideable();
	}

	private void InitConnector(IEntityRegistry entityRegistry, SnapshotRegistry snapshotRegistry)
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

		entityRegistry.Init();
		snapshotRegistry.Init();

		Connector.LookupAll();

		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(Connector.Adapter.InnerAdapters));

		try
		{
			if (_settingsFile.IsConfigExists())
			{
				var ctx = new ContinueOnExceptionContext();
				ctx.Error += ex => ex.LogError();

				using (ctx.ToScope())
					Connector.LoadIfNotNull(_settingsFile.Deserialize<SettingsStorage>());
			}
		}
		catch
		{
		}
	}

	protected override void OnClosing(CancelEventArgs e)
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

		Connector.Dispose();

		ServicesRegistry.EntityRegistry.DelayAction.DefaultGroup.WaitFlush(true);

		base.OnClosing(e);
	}

	private void SettingsClick(object sender, RoutedEventArgs e)
	{
		if (Connector.Configure(this))
			Connector.Save().Serialize(_settingsFile);
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