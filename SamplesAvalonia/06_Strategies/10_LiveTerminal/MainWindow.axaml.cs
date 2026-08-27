namespace StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.Collections;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml;

using MonitorControl = StockSharp.Xaml.Windows.Avalonia.Monitor;

public partial class MainWindow : Window
{
	private readonly TerminalConnectorRuntime _runtime;
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly EventSubscription _connectorEvents;
	private readonly EventSubscription _toolWindowEvents;
	private readonly LogManager _logManager;
	private readonly SecuritiesWindow _securitiesWindow;
	private readonly OrdersWindow _ordersWindow;
	private readonly PortfoliosWindow _portfoliosWindow;
	private readonly MyTradesWindow _myTradesWindow;
	private readonly StrategiesWindow _strategiesWindow;
	private readonly Window[] _toolWindows;
	private readonly MonitorControl _monitor;
	private readonly Button _settingsButton;
	private readonly Button _connectButton;
	private readonly TextBlock _status;
	private Task _initializationTask = Task.CompletedTask;
	private Task _configurationTask = Task.CompletedTask;
	private int _callbackGeneration = 1;
	private bool _initialized;
	private bool _isConnected;
	private bool _connectStarted;
	private bool _isClosing;
	private bool _closeApproved;

	private Connector Connector => _runtime.Context.Connector;

	public MainWindow()
	{
		InitializeComponent();

		_monitor = this.FindControl<MonitorControl>(nameof(LogMonitor));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		_connectButton = this.FindControl<Button>(nameof(ConnectButton));
		_status = this.FindControl<TextBlock>(nameof(StatusText));

		_runtime = TerminalConnectorRuntime.Create();
		_logManager = new LogManager();
		_logManager.Listeners.Add(new FileLogListener
		{
			LogDirectory = Path.Combine(_runtime.DataPath, "Logs"),
		});
		_logManager.Listeners.Add(new GuiLogListener(_monitor));
		_logManager.Sources.Add(Connector);

		var portfolios = new StockSharp.Xaml.PortfolioDataSource(Connector);
		_securitiesWindow = new SecuritiesWindow(Connector, portfolios);
		_ordersWindow = new OrdersWindow(Connector);
		_portfoliosWindow = new PortfoliosWindow();
		_myTradesWindow = new MyTradesWindow();
		_strategiesWindow = new StrategiesWindow(
			Connector,
			portfolios,
			_runtime.ResolveSecurity,
			_runtime.ResolvePortfolio,
			_logManager,
			_runtime.FileSystem,
			_runtime.DataPath);
		_toolWindows =
		[
			_securitiesWindow,
			_ordersWindow,
			_portfoliosWindow,
			_myTradesWindow,
			_strategiesWindow,
		];

		_connectorEvents = new(
			() =>
			{
				Connector.Connected += OnConnected;
				Connector.Disconnected += OnDisconnected;
				Connector.ConnectionError += OnConnectionError;
				Connector.Error += OnConnectorError;
				Connector.SubscriptionFailed += OnSubscriptionFailed;
				Connector.SecurityReceived += OnSecurityReceived;
				Connector.OrderReceived += OnOrderReceived;
				Connector.OrderRegisterFailReceived += OnOrderRegisterFailReceived;
				Connector.OwnTradeReceived += OnOwnTradeReceived;
				Connector.PositionReceived += OnPositionReceived;
			},
			() =>
			{
				Connector.Connected -= OnConnected;
				Connector.Disconnected -= OnDisconnected;
				Connector.ConnectionError -= OnConnectionError;
				Connector.Error -= OnConnectorError;
				Connector.SubscriptionFailed -= OnSubscriptionFailed;
				Connector.SecurityReceived -= OnSecurityReceived;
				Connector.OrderReceived -= OnOrderReceived;
				Connector.OrderRegisterFailReceived -= OnOrderRegisterFailReceived;
				Connector.OwnTradeReceived -= OnOwnTradeReceived;
				Connector.PositionReceived -= OnPositionReceived;
			});

		_toolWindowEvents = new(
			() =>
			{
				foreach (var window in _toolWindows)
					window.Closing += OnToolWindowClosing;
			},
			() =>
			{
				foreach (var window in _toolWindows)
					window.Closing -= OnToolWindowClosing;
			});
		_toolWindowEvents.Attach();

		_connectorEvents.Attach();
		Opened += OnOpened;
		Closing += OnClosing;
	}

	private void OnOpened(object sender, EventArgs e)
		=> _initializationTask = InitializeRuntimeAsync();

	private async Task InitializeRuntimeAsync()
	{
		try
		{
			await _runtime.InitializeAsync(_lifetimeCancellation.Token);
			if (_isClosing)
				return;

			await _strategiesWindow.LoadStrategiesAsync(_lifetimeCancellation.Token);
			if (_isClosing)
				return;

			_initialized = true;
			_settingsButton.IsEnabled = true;
			_connectButton.IsEnabled = true;
			_status.Text = "Disconnected";
			if (_runtime.Context.AutoConnect)
				StartConnect();
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			error.LogError();
			if (!_isClosing)
				_status.Text = $"Initialization failed: {error.Message}";
		}
	}

	private void OnSettingsClick(object sender, RoutedEventArgs e)
	{
		if (_isClosing || !_initialized || !_configurationTask.IsCompleted)
			return;

		_configurationTask = ConfigureAsync();
	}

	private async Task ConfigureAsync()
	{
		_settingsButton.IsEnabled = false;
		try
		{
			await _runtime.Context.ConfigureAsync(this, _lifetimeCancellation.Token);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			if (!_isClosing)
				_status.Text = $"Settings failed: {error.Message}";
		}
		finally
		{
			if (!_isClosing && !_isConnected && !_connectStarted)
				_settingsButton.IsEnabled = true;
		}
	}

	private void OnConnectClick(object sender, RoutedEventArgs e)
	{
		if (_isConnected)
		{
			_connectButton.IsEnabled = false;
			_status.Text = "Disconnecting";
			Connector.Disconnect();
		}
		else
			StartConnect();
	}

	private void StartConnect()
	{
		if (!_initialized || _connectStarted || _isClosing)
			return;

		_connectStarted = true;
		_settingsButton.IsEnabled = false;
		_connectButton.IsEnabled = false;
		_status.Text = "Connecting";
		Connector.Connect();
	}

	private void OnConnected()
		=> DispatchCurrent(() =>
		{
			_isConnected = true;
			_connectStarted = false;
			_connectButton.Content = "Disconnect";
			_connectButton.IsEnabled = true;
			_status.Text = "Connected";
		});

	private void OnDisconnected()
		=> DispatchCurrent(() => ResetConnectionUi("Disconnected"));

	private void OnConnectionError(Exception error)
		=> DispatchCurrent(() => ResetConnectionUi($"Connection failed: {error.Message}"));

	private void OnConnectorError(Exception error)
		=> DispatchCurrent(() => _status.Text = $"Data error: {error.Message}");

	private void OnSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
		=> DispatchCurrent(() => _status.Text = $"Subscription failed ({subscription.DataType}): {error.Message}");

	private void ResetConnectionUi(string status)
	{
		_isConnected = false;
		_connectStarted = false;
		_settingsButton.IsEnabled = true;
		_connectButton.IsEnabled = _initialized;
		_connectButton.Content = "Connect";
		_status.Text = status;
	}

	private void OnSecurityReceived(Subscription subscription, Security security)
		=> DispatchCurrent(() => _securitiesWindow.AddSecurity(security));

	private void OnOrderReceived(Subscription subscription, Order order)
		=> DispatchCurrent(() =>
		{
			_ordersWindow.AddOrder(order);
			_securitiesWindow.ProcessOrder(order);
		});

	private void OnOrderRegisterFailReceived(Subscription subscription, OrderFail fail)
		=> DispatchCurrent(() => _ordersWindow.AddRegistrationFail(fail));

	private void OnOwnTradeReceived(Subscription subscription, MyTrade trade)
		=> DispatchCurrent(() => _myTradesWindow.AddTrade(trade));

	private void OnPositionReceived(Subscription subscription, Position position)
		=> DispatchCurrent(() => _portfoliosWindow.AddPosition(position));

	private void DispatchCurrent(Action action)
	{
		var generation = Volatile.Read(ref _callbackGeneration);
		_uiEvents.Dispatch(() =>
		{
			if (!_isClosing && generation == Volatile.Read(ref _callbackGeneration))
				action();
		});
	}

	private void OnShowSecuritiesClick(object sender, RoutedEventArgs e)
		=> ShowOrHide(_securitiesWindow);

	private void OnShowPortfoliosClick(object sender, RoutedEventArgs e)
		=> ShowOrHide(_portfoliosWindow);

	private void OnShowOrdersClick(object sender, RoutedEventArgs e)
		=> ShowOrHide(_ordersWindow);

	private void OnShowStrategiesClick(object sender, RoutedEventArgs e)
		=> ShowOrHide(_strategiesWindow);

	private void OnShowMyTradesClick(object sender, RoutedEventArgs e)
		=> ShowOrHide(_myTradesWindow);

	private void ShowOrHide(Window window)
	{
		if (window.IsVisible)
			window.Hide();
		else
			window.Show(this);
	}

	private void OnToolWindowClosing(object sender, WindowClosingEventArgs e)
	{
		if (_isClosing || sender is not Window window)
			return;

		e.Cancel = true;
		window.Hide();
	}

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;

		e.Cancel = true;
		if (_isClosing)
			return;

		_isClosing = true;
		Interlocked.Increment(ref _callbackGeneration);
		_lifetimeCancellation.Cancel();
		_connectButton.IsEnabled = false;

		try
		{
			await _initializationTask;
			await _configurationTask;
			await _strategiesWindow.StopAllAsync();
		}
		catch (Exception error)
		{
			error.LogError();
		}
		finally
		{
			Closing -= OnClosing;
			Opened -= OnOpened;
			_connectorEvents.Dispose();
			_toolWindowEvents.Dispose();

			foreach (var window in _toolWindows)
			{
				try
				{
					window.Close();
				}
				catch
				{
				}
			}

			TryDispose(_securitiesWindow);
			TryDispose(_ordersWindow);
			TryDispose(_portfoliosWindow);
			TryDispose(_myTradesWindow);
			TryDispose(_strategiesWindow);
			_uiEvents.Dispose();
			TryRemoveLogSource(Connector);
			TryDispose(_logManager);
			TryDispose(_monitor);
			TryDispose(_runtime);
			TryDispose(_lifetimeCancellation);
			_closeApproved = true;
			Close();
		}
	}

	private void TryRemoveLogSource(ILogSource source)
	{
		try
		{
			_logManager.Sources.Remove(source);
		}
		catch
		{
		}
	}

	private static void TryDispose(object value)
	{
		try
		{
			if (value is IDisposable disposable)
				disposable.Dispose();
		}
		catch
		{
		}
	}
}
