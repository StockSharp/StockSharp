namespace StockSharp.Samples.Strategies.LiveSpread;

using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;
using Avalonia.Interactivity;

using Ecng.Collections;
using Ecng.Xaml;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples;
using StockSharp.Xaml;
using StockSharp.Xaml.PropertyGrid;

using MonitorControl = StockSharp.Xaml.Monitor;

public partial class MainWindow : Window
{
	private readonly SampleConnectorContext _context = new();
	private readonly GuiDispatcher _uiEvents = new();
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SemaphoreSlim _strategyGate = new(1, 1);
	private readonly EventSubscription _connectorEvents;
	private readonly LogManager _logManager;
	private readonly SecurityEditor _securityEditor;
	private readonly PortfolioEditor _portfolioEditor;
	private readonly CandleDataTypeEdit _candleDataTypeEdit;
	private readonly OrderGrid _orderGrid;
	private readonly MyTradeGrid _myTradeGrid;
	private readonly PortfolioGrid _portfolioGrid;
	private readonly MonitorControl _monitor;
	private readonly Button _settingsButton;
	private readonly Button _connectButton;
	private readonly Button _startButton;
	private readonly Button _stopButton;
	private readonly TextBlock _status;
	private Strategy _strategy;
	private EventSubscription _strategyEvents;
	private bool _connectStarted;
	private bool _isClosing;
	private bool _closeApproved;
	private int _strategyGeneration;

	public MainWindow()
	{
		InitializeComponent();

		_securityEditor = this.FindControl<SecurityEditor>(nameof(SecurityEditor));
		_portfolioEditor = this.FindControl<PortfolioEditor>(nameof(PortfolioEditor));
		_candleDataTypeEdit = this.FindControl<CandleDataTypeEdit>(nameof(CandleDataTypeEdit));
		_orderGrid = this.FindControl<OrderGrid>(nameof(OrderGrid));
		_myTradeGrid = this.FindControl<MyTradeGrid>(nameof(MyTradeGrid));
		_portfolioGrid = this.FindControl<PortfolioGrid>(nameof(PortfolioGrid));
		_monitor = this.FindControl<MonitorControl>(nameof(LogMonitor));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		_connectButton = this.FindControl<Button>(nameof(ConnectButton));
		_startButton = this.FindControl<Button>(nameof(StartButton));
		_stopButton = this.FindControl<Button>(nameof(StopButton));
		_status = this.FindControl<TextBlock>(nameof(StatusText));

		_candleDataTypeEdit.DataType = TimeSpan.FromSeconds(10).TimeFrame();
		_securityEditor.SecurityProvider = _context.Connector;
		_portfolioEditor.PortfolioProvider = _context.Connector;

		_logManager = new LogManager();
		_logManager.Listeners.Add(new GuiLogListener(_monitor));
		_logManager.Sources.Add(_context.Connector);

		_connectorEvents = new(
			() =>
			{
				_context.Connector.Connected += OnConnected;
				_context.Connector.ConnectionError += OnConnectionError;
				_context.Connector.Disconnected += OnDisconnected;
				_context.Connector.SecurityReceived += OnSecurityReceived;
				_context.Connector.PortfolioReceived += OnPortfolioReceived;
				_context.Connector.PositionReceived += OnPositionReceived;
			},
			() =>
			{
				_context.Connector.Connected -= OnConnected;
				_context.Connector.ConnectionError -= OnConnectionError;
				_context.Connector.Disconnected -= OnDisconnected;
				_context.Connector.SecurityReceived -= OnSecurityReceived;
				_context.Connector.PortfolioReceived -= OnPortfolioReceived;
				_context.Connector.PositionReceived -= OnPositionReceived;
			});

		Opened += OnOpened;
		Closing += OnClosing;
	}

	private async void OnSettingsClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await _context.ConfigureAsync(this, _lifetimeCancellation.Token);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			_status.Text = $"Settings failed: {error.Message}";
		}
	}

	private void OnConnectClick(object sender, RoutedEventArgs e)
		=> StartConnect();

	private async void OnOpened(object sender, EventArgs e)
	{
		await _context.LoadAsync(CancellationToken.None);

		if (_context.IsAutoConnect)
			StartConnect();
	}

	private void StartConnect()
	{
		if (_connectStarted || _isClosing)
			return;

		_connectStarted = true;
		_settingsButton.IsEnabled = false;
		_connectButton.IsEnabled = false;
		_status.Text = "Connecting";
		_connectorEvents.Attach();
		_context.Connector.Connect();
	}

	private void OnConnected()
		=> _uiEvents.TryAddAction(() =>
		{
			if (!_isClosing)
				_status.Text = "Connected";
		});

	private void OnConnectionError(Exception error)
		=> _uiEvents.TryAddAction(() => ResetConnectionUi($"Connection failed: {error.Message}"));

	private void OnDisconnected()
		=> _uiEvents.TryAddAction(() => ResetConnectionUi("Disconnected"));

	private void ResetConnectionUi(string status)
	{
		if (_isClosing)
			return;

		_connectStarted = false;
		_settingsButton.IsEnabled = true;
		_connectButton.IsEnabled = true;
		_status.Text = status;
	}

	private void OnSecurityReceived(Subscription subscription, Security security)
		=> _uiEvents.TryAddAction(() =>
		{
			if (!_isClosing && _securityEditor.SelectedSecurity is null)
				_securityEditor.SelectedSecurity = security;
		});

	private void OnPortfolioReceived(Subscription subscription, Portfolio portfolio)
		=> _uiEvents.TryAddAction(() =>
		{
			if (_isClosing)
				return;

			_portfolioGrid.Positions.TryAdd(portfolio);
			if (_portfolioEditor.SelectedPortfolio is null)
				_portfolioEditor.SelectedPortfolio = portfolio;
		});

	private void OnPositionReceived(Subscription subscription, Position position)
		=> _uiEvents.TryAddAction(() =>
		{
			if (!_isClosing)
				_portfolioGrid.Positions.TryAdd(position);
		});

	private async void OnStartClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await StartStrategyAsync();
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			if (!_isClosing)
				_status.Text = $"Start failed: {error.Message}";
		}
	}

	private async Task StartStrategyAsync()
	{
		await _strategyGate.WaitAsync(_lifetimeCancellation.Token);
		try
		{
			if (_strategy is not null || _isClosing)
				return;

			var security = _securityEditor.SelectedSecurity;
			var portfolio = _portfolioEditor.SelectedPortfolio;
			if (security is null || portfolio is null)
			{
				_status.Text = "Select a security and portfolio.";
				return;
			}

			// Replace with MqSpreadStrategy or MqStrategy to run the other live-spread lessons.
			var strategy = new StockSharp.Samples.Strategies.LiveSpread.StairsCountertrendStrategy
			{
				Security = security,
				Portfolio = portfolio,
				Connector = _context.Connector,
				CandleDataType = _candleDataTypeEdit.DataType,
			};

			_strategy = strategy;
			var generation = ++_strategyGeneration;
			AttachStrategyEvents(strategy, generation);
			_logManager.Sources.Add(strategy);
			SetRunningState(true, "Starting");

			try
			{
				await strategy.StartAsync(_lifetimeCancellation.Token);
				if (IsCurrent(strategy, generation))
					_status.Text = "Running";
			}
			catch
			{
				await TeardownStrategyCoreAsync(strategy);
				throw;
			}
		}
		finally
		{
			_strategyGate.Release();
		}
	}

	private void AttachStrategyEvents(Strategy strategy, int generation)
	{
		Action<Subscription, Order> orderReceived = (_, order)
			=> _uiEvents.TryAddAction(() =>
			{
				if (IsCurrent(strategy, generation))
					_orderGrid.Orders.TryAdd(order);
			});
		Action<Subscription, OrderFail> orderFailed = (_, fail)
			=> _uiEvents.TryAddAction(() =>
			{
				if (IsCurrent(strategy, generation))
					_orderGrid.AddRegistrationFail(fail);
			});
		Action<Subscription, MyTrade> ownTradeReceived = (_, trade)
			=> _uiEvents.TryAddAction(() =>
			{
				if (IsCurrent(strategy, generation))
					_myTradeGrid.Trades.TryAdd(trade);
			});

		_strategyEvents = new(
			() =>
			{
				strategy.OrderReceived += orderReceived;
				strategy.OrderRegisterFailReceived += orderFailed;
				strategy.OwnTradeReceived += ownTradeReceived;
			},
			() =>
			{
				strategy.OrderReceived -= orderReceived;
				strategy.OrderRegisterFailReceived -= orderFailed;
				strategy.OwnTradeReceived -= ownTradeReceived;
			});
		_strategyEvents.Attach();
	}

	private bool IsCurrent(Strategy strategy, int generation)
		=> !_isClosing && ReferenceEquals(_strategy, strategy) && _strategyGeneration == generation;

	private async void OnStopClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await StopStrategyAsync();
		}
		catch (Exception error)
		{
			if (!_isClosing)
				_status.Text = $"Stop failed: {error.Message}";
		}
	}

	private async Task StopStrategyAsync()
	{
		await _strategyGate.WaitAsync();
		try
		{
			if (_strategy is not null)
				await TeardownStrategyCoreAsync(_strategy);
		}
		finally
		{
			_strategyGate.Release();
		}
	}

	private async Task TeardownStrategyCoreAsync(Strategy strategy)
	{
		_strategy = null;
		_strategyGeneration++;
		_strategyEvents?.Dispose();
		_strategyEvents = null;
		TryRemoveLogSource(strategy);

		try
		{
			await strategy.StopAsync();
		}
		finally
		{
			strategy.Dispose();
			if (!_isClosing)
				SetRunningState(false, "Connected");
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

	private void SetRunningState(bool isRunning, string status)
	{
		_startButton.IsEnabled = !isRunning;
		_stopButton.IsEnabled = isRunning;
		_securityEditor.IsEnabled = !isRunning;
		_portfolioEditor.IsEnabled = !isRunning;
		_candleDataTypeEdit.IsEnabled = !isRunning;
		_status.Text = status;
	}

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;

		e.Cancel = true;
		if (_isClosing)
			return;

		_isClosing = true;
		_lifetimeCancellation.Cancel();
		try
		{
			await StopStrategyAsync();
		}
		catch
		{
		}
		finally
		{
			Closing -= OnClosing;
			Opened -= OnOpened;
			_connectorEvents.Dispose();
			_uiEvents.Dispose();
			TryRemoveLogSource(_context.Connector);
			TryDispose(_securityEditor);
			TryDispose(_portfolioEditor);
			TryDispose(_orderGrid);
			TryDispose(_myTradeGrid);
			TryDispose(_portfolioGrid);
			TryDispose(_monitor);
			TryDispose(_logManager);
			TryDispose(_context);
			TryDispose(_lifetimeCancellation);
			TryDispose(_strategyGate);
			_closeApproved = true;
			Close();
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
