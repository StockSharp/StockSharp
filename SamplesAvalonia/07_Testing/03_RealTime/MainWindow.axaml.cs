namespace StockSharp.Samples.Testing.RealTime.Avalonia;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.Collections;
using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Logging;
using Ecng.Xaml.Avalonia;

using StockSharp.Algo;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting.Avalonia.Controls;
using StockSharp.Xaml.Grids.Avalonia;
using StockSharp.Xaml.PropertyGrid.Avalonia.Editors;
using StockSharp.Xaml.Windows.Avalonia;

public partial class MainWindow : Window
{
	private readonly SampleConnectorContext _realContext = new();
	private readonly Portfolio _emulatedPortfolio = Portfolio.CreateSimulator();
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly HashSet<Order> _firstTimeOrders = [];
	private readonly HashSet<Task> _uiTasks = [];
	private readonly List<Subscription> _emulationMarketSubscriptions = [];
	private readonly Dictionary<Subscription, int> _subscriptionGenerations = [];
	private readonly LogManager _logManager;
	private readonly EventSubscription _realConnectorEvents;
	private readonly SecurityPicker _securityPicker;
	private readonly CandleDataTypeEdit _candleDataTypeEdit;
	private readonly MarketDepthControl _emulatedDepth;
	private readonly MarketDepthControl _realDepth;
	private readonly PortfolioGrid _portfolioGrid;
	private readonly OrderGrid _orderGrid;
	private readonly MyTradeGrid _myTradeGrid;
	private readonly LogControl _logControl;
	private readonly ChartPanel _chart;
	private readonly Button _settingsButton;
	private readonly Button _connectButton;
	private readonly Button _findButton;
	private readonly Button _newOrderButton;
	private readonly TextBlock _status;
	private readonly IChartCandleElement _candlesElement;
	private readonly IChartActiveOrdersElement _ordersElement;
	private RealTimeEmulationTrader<IMessageAdapter> _emulationConnector;
	private EventSubscription _emulationEvents;
	private Subscription _candleSubscription;
	private Subscription _realDepthSubscription;
	private Security _security;
	private SecurityId _securityId;
	private DataType _lastCandleDataType;
	private int _emulationGeneration;
	private int _selectionGeneration;
	private bool _connectStarted;
	private bool _isConnected;
	private bool _isClosing;
	private bool _closeApproved;

	public MainWindow()
	{
		InitializeComponent();

		_securityPicker = this.FindControl<SecurityPicker>(nameof(SecurityPicker));
		_candleDataTypeEdit = this.FindControl<CandleDataTypeEdit>(nameof(CandleDataTypeEdit));
		_emulatedDepth = this.FindControl<MarketDepthControl>(nameof(EmulatedDepth));
		_realDepth = this.FindControl<MarketDepthControl>(nameof(RealDepth));
		_portfolioGrid = this.FindControl<PortfolioGrid>(nameof(PortfolioGrid));
		_orderGrid = this.FindControl<OrderGrid>(nameof(OrderGrid));
		_myTradeGrid = this.FindControl<MyTradeGrid>(nameof(MyTradeGrid));
		_logControl = this.FindControl<LogControl>(nameof(LogControl));
		_chart = this.FindControl<ChartPanel>(nameof(Chart));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		_connectButton = this.FindControl<Button>(nameof(ConnectButton));
		_findButton = this.FindControl<Button>(nameof(FindButton));
		_newOrderButton = this.FindControl<Button>(nameof(NewOrderButton));
		_status = this.FindControl<TextBlock>(nameof(StatusText));

		_candleDataTypeEdit.DataType = TimeSpan.FromMinutes(5).TimeFrame();
		_lastCandleDataType = _candleDataTypeEdit.DataType.Clone();
		_securityPicker.SecurityProvider = _realContext.Connector;

		var area = _chart.CreateArea();
		area.Title = "Candles and emulated active orders";
		_chart.AddArea(area);
		_chart.ActiveArea = area;
		_candlesElement = _chart.CreateCandleElement();
		_candlesElement.FullTitle = "Candles";
		_chart.AddElement(area, _candlesElement);
		_ordersElement = _chart.CreateActiveOrdersElement();
		_ordersElement.FullTitle = "Emulated active orders";
		_chart.AddElement(area, _ordersElement);
		_chart.RegisterOrder += OnChartRegisterOrder;
		_chart.CancelOrder += OnChartCancelOrder;
		_chart.MoveOrder += OnChartMoveOrder;
		_chart.OrderCreationMode = true;
		_chart.OrderSettings.Portfolio = _emulatedPortfolio;
		_chart.IsQuickOrderVisible = true;

		_logManager = new LogManager();
		_logManager.Listeners.Add(new GuiLogListener(_logControl));
		_logManager.Sources.Add(_realContext.Connector);

		_realConnectorEvents = CreateRealConnectorEvents();
		_realConnectorEvents.Attach();
		CreateEmulationConnector();

		_securityPicker.SecuritySelected += OnSecuritySelected;
		_candleDataTypeEdit.DataTypeChanged += OnCandleDataTypeChanged;
		_orderGrid.OrderRegistering += OnOrderRegistering;
		_orderGrid.OrderCanceling += OnOrderCanceling;
		_orderGrid.OrderReRegistering += OnOrderReRegistering;
		Opened += OnOpened;
		Closing += OnClosing;
	}

	private EventSubscription CreateRealConnectorEvents()
	{
		var connector = _realContext.Connector;
		Action<Subscription, Order> orderReceived = (_, order) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentRealConnector(connector))
					_orderGrid.Orders.TryAdd(order);
			});
		Action<Subscription, MyTrade> ownTradeReceived = (_, trade) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentRealConnector(connector))
					_myTradeGrid.Trades.TryAdd(trade);
			});
		Action<Subscription, OrderFail> orderFailed = (_, fail) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentRealConnector(connector))
					_orderGrid.AddRegistrationFail(fail);
			});
		Action<Subscription, IOrderBookMessage> depthReceived = (subscription, depth) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentRealConnector(connector) &&
					ReferenceEquals(subscription, _realDepthSubscription) &&
					_security is { } security && depth.SecurityId == _securityId)
				{
					_realDepth.UpdateDepth(depth, security);
				}
			});
		Action<long, Exception> massCancelFailed = (_, error) =>
			_uiEvents.Dispatch(() => SetStatusIfAlive($"Real order cancellation failed: {error.Message}"));
		Action<Exception> connectionError = error =>
			_uiEvents.Dispatch(() => SetStatusIfAlive($"Real connection failed: {error.Message}"));

		return new(
			() =>
			{
				connector.OrderReceived += orderReceived;
				connector.OwnTradeReceived += ownTradeReceived;
				connector.OrderRegisterFailReceived += orderFailed;
				connector.OrderBookReceived += depthReceived;
				connector.MassOrderCancelFailed += massCancelFailed;
				connector.ConnectionError += connectionError;
			},
			() =>
			{
				connector.OrderReceived -= orderReceived;
				connector.OwnTradeReceived -= ownTradeReceived;
				connector.OrderRegisterFailReceived -= orderFailed;
				connector.OrderBookReceived -= depthReceived;
				connector.MassOrderCancelFailed -= massCancelFailed;
				connector.ConnectionError -= connectionError;
			});
	}

	private void CreateEmulationConnector()
	{
		var connector = new RealTimeEmulationTrader<IMessageAdapter>(
			_realContext.Connector.Adapter,
			_realContext.Connector,
			_emulatedPortfolio,
			ownAdapter: false);
		var generation = ++_emulationGeneration;
		_emulationConnector = connector;

		var settings = connector.EmulationAdapter.Emulator.Settings;
		settings.TimeZone = TimeHelper.Est;
		settings.ConvertTime = true;

		Action connected = () => _uiEvents.Dispatch(() =>
		{
			if (!IsCurrentEmulation(connector, generation))
				return;

			_connectStarted = false;
			_isConnected = true;
			_connectButton.Content = "Disconnect";
			_connectButton.IsEnabled = true;
			_findButton.IsEnabled = true;
			_newOrderButton.IsEnabled = true;
			_status.Text = "Connected (real feed + emulated execution)";
		});
		Action disconnected = () => _uiEvents.Dispatch(() =>
		{
			if (IsCurrentEmulation(connector, generation))
				SetDisconnectedState("Disconnected");
		});
		Action<Exception> connectionError = error => _uiEvents.Dispatch(() =>
		{
			if (IsCurrentEmulation(connector, generation))
				SetDisconnectedState($"Emulation connection failed: {error.Message}");
		});
		Action<Subscription, IOrderBookMessage> depthReceived = (subscription, depth) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentEmulation(connector, generation) &&
					IsCurrentSelection(subscription) &&
					_security is { } security && depth.SecurityId == _securityId)
				{
					_emulatedDepth.UpdateDepth(depth, security);
				}
			});
		Action<Subscription, Position> positionReceived = (_, position) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentEmulation(connector, generation))
					_portfolioGrid.Positions.TryAdd(position);
			});
		Action<Subscription, MyTrade> ownTradeReceived = (_, trade) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentEmulation(connector, generation))
					_myTradeGrid.Trades.TryAdd(trade);
			});
		Action<Subscription, Order> orderReceived = (_, order) =>
			_uiEvents.Dispatch(() =>
			{
				if (!IsCurrentEmulation(connector, generation) || !_firstTimeOrders.Add(order))
					return;

				_orderGrid.Orders.TryAdd(order);
				_chart.Draw(_chart.CreateData().Add(_ordersElement, order));
			});
		Action<Subscription, OrderFail> orderFailed = (_, fail) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentEmulation(connector, generation))
					_orderGrid.AddRegistrationFail(fail);
			});
		Action<Subscription, ICandleMessage> candleReceived = (subscription, candle) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentEmulation(connector, generation) &&
					IsCurrentSelection(subscription) &&
					ReferenceEquals(subscription, _candleSubscription))
				{
					_chart.Draw(_candlesElement, candle);
				}
			});
		Action<long, Exception> massCancelFailed = (_, error) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentEmulation(connector, generation))
					_status.Text = $"Emulated order cancellation failed: {error.Message}";
			});
		Action<Subscription, Exception, bool> subscriptionFailed = (subscription, error, _) =>
			_uiEvents.Dispatch(() =>
			{
				if (IsCurrentEmulation(connector, generation) && error is not null)
					_status.Text = $"Subscription failed ({subscription.DataType}): {error.Message}";
			});

		_emulationEvents = new(
			() =>
			{
				connector.Connected += connected;
				connector.Disconnected += disconnected;
				connector.ConnectionError += connectionError;
				connector.OrderBookReceived += depthReceived;
				connector.PositionReceived += positionReceived;
				connector.OwnTradeReceived += ownTradeReceived;
				connector.OrderReceived += orderReceived;
				connector.OrderRegisterFailReceived += orderFailed;
				connector.CandleReceived += candleReceived;
				connector.MassOrderCancelFailed += massCancelFailed;
				connector.SubscriptionFailed += subscriptionFailed;
			},
			() =>
			{
				connector.Connected -= connected;
				connector.Disconnected -= disconnected;
				connector.ConnectionError -= connectionError;
				connector.OrderBookReceived -= depthReceived;
				connector.PositionReceived -= positionReceived;
				connector.OwnTradeReceived -= ownTradeReceived;
				connector.OrderReceived -= orderReceived;
				connector.OrderRegisterFailReceived -= orderFailed;
				connector.CandleReceived -= candleReceived;
				connector.MassOrderCancelFailed -= massCancelFailed;
				connector.SubscriptionFailed -= subscriptionFailed;
			});
		_emulationEvents.Attach();
		_securityPicker.MarketDataProvider = connector;
		_logManager.Sources.Add(connector);
	}

	private bool IsCurrentRealConnector(Connector connector)
		=> !_isClosing && ReferenceEquals(_realContext.Connector, connector);

	private bool IsCurrentEmulation(RealTimeEmulationTrader<IMessageAdapter> connector, int generation)
		=> !_isClosing && ReferenceEquals(_emulationConnector, connector) && _emulationGeneration == generation;

	private bool IsCurrentSelection(Subscription subscription)
		=> _subscriptionGenerations.TryGetValue(subscription, out var generation) &&
			generation == _selectionGeneration;

	private void OnOpened(object sender, EventArgs e)
	{
		if (_realContext.AutoConnect)
			StartConnect();
	}

	private void OnSettingsClick(object sender, RoutedEventArgs e)
		=> TrackUiTask(ConfigureAsync());

	private async Task ConfigureAsync()
	{
		try
		{
			if (!await _realContext.ConfigureAsync(this, _lifetimeCancellation.Token) || _isClosing)
				return;

			var selectedSecurity = _security;
			DisposeEmulationConnector();
			CreateEmulationConnector();
			if (selectedSecurity is not null)
				SelectSecurity(selectedSecurity);
			_status.Text = "Settings saved; emulation adapter rebuilt";
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			SetStatusIfAlive($"Settings failed: {error.Message}");
		}
	}

	private void OnConnectClick(object sender, RoutedEventArgs e)
	{
		if (_isConnected || _connectStarted)
			StartDisconnect();
		else
			StartConnect();
	}

	private void StartConnect()
	{
		if (_isClosing || _connectStarted || _isConnected || _emulationConnector is null)
			return;

		_connectStarted = true;
		_settingsButton.IsEnabled = false;
		_connectButton.IsEnabled = false;
		_status.Text = "Connecting real feed and emulated execution...";
		try
		{
			_realContext.Connector.Connect();
			_emulationConnector.Connect();
		}
		catch (Exception error)
		{
			TryDisconnect(_emulationConnector);
			TryDisconnect(_realContext.Connector);
			SetDisconnectedState($"Connect failed: {error.Message}");
		}
	}

	private void StartDisconnect()
	{
		if (_isClosing)
			return;

		_connectButton.IsEnabled = false;
		_status.Text = "Disconnecting...";
		TryDisconnect(_emulationConnector);
		TryDisconnect(_realContext.Connector);
		if (_emulationConnector?.ConnectionState == ConnectionStates.Disconnected)
			SetDisconnectedState("Disconnected");
	}

	private void SetDisconnectedState(string status)
	{
		if (_isClosing)
			return;

		_connectStarted = false;
		_isConnected = false;
		_settingsButton.IsEnabled = true;
		_connectButton.Content = "Connect";
		_connectButton.IsEnabled = true;
		_findButton.IsEnabled = false;
		_newOrderButton.IsEnabled = false;
		_status.Text = status;
	}

	private void OnCandleDataTypeChanged(object sender, DataType dataType)
	{
		if (_isClosing || dataType is null || Equals(_lastCandleDataType, dataType))
			return;

		_lastCandleDataType = dataType.Clone();
		if (_security is { } security)
			SelectSecurity(security);
	}

	private void OnSecuritySelected(Security security)
	{
		if (security is not null && !_isClosing)
			SelectSecurity(security);
	}

	private void SelectSecurity(Security security)
	{
		ArgumentNullException.ThrowIfNull(security);
		var emulation = _emulationConnector;
		if (emulation is null)
			return;

		ClearMarketSubscriptions(emulation);
		_security = security;
		_securityId = security.ToSecurityId();
		_selectionGeneration++;
		_firstTimeOrders.Clear();
		_chart.Reset([_candlesElement, _ordersElement]);
		_chart.OrderSettings.Security = security;

		var emulatedDepth = new Subscription(DataType.MarketDepth, security);
		var ticks = new Subscription(DataType.Ticks, security);
		var level1 = new Subscription(DataType.Level1, security);
		var candle = new Subscription(_candleDataTypeEdit.DataType, security)
		{
			MarketData =
			{
				From = DateTime.UtcNow - TimeSpan.FromDays(10),
				BuildMode = MarketDataBuildModes.LoadAndBuild,
			},
		};
		var realDepth = new Subscription(DataType.MarketDepth, security);

		_emulationMarketSubscriptions.Add(emulatedDepth);
		_emulationMarketSubscriptions.Add(ticks);
		_emulationMarketSubscriptions.Add(level1);
		_emulationMarketSubscriptions.Add(candle);
		foreach (var subscription in _emulationMarketSubscriptions)
			_subscriptionGenerations[subscription] = _selectionGeneration;
		_candleSubscription = candle;
		_realDepthSubscription = realDepth;

		foreach (var subscription in _emulationMarketSubscriptions)
			emulation.Subscribe(subscription);
		_realContext.Connector.Subscribe(realDepth);
		_status.Text = $"Subscribed to {security.Id}: real/emulated depth, ticks, level1 and {_candleDataTypeEdit.DataType}";
	}

	private void ClearMarketSubscriptions(RealTimeEmulationTrader<IMessageAdapter> emulation)
	{
		_selectionGeneration++;
		var emulationSubscriptions = _emulationMarketSubscriptions.ToArray();
		_emulationMarketSubscriptions.Clear();
		_subscriptionGenerations.Clear();
		_candleSubscription = null;
		var realDepth = _realDepthSubscription;
		_realDepthSubscription = null;

		foreach (var subscription in emulationSubscriptions)
			TryUnsubscribe(emulation, subscription);
		if (realDepth is not null)
			TryUnsubscribe(_realContext.Connector, realDepth);
	}

	private void OnFindClick(object sender, RoutedEventArgs e)
		=> TrackUiTask(FindSecurityAsync());

	private async Task FindSecurityAsync()
	{
		var connector = _emulationConnector;
		var generation = _emulationGeneration;
		if (connector is null || _isClosing)
			return;

		try
		{
			using var window = new SecurityLookupWindow
			{
				ShowAllOption = connector.MarketDataAdapter.IsSupportSecuritiesLookupAll(),
				CriteriaMessage = new() { SecurityId = new() { SecurityCode = "AAPL" } },
			};
			if (!await window.ShowDialogAsync<bool>(this, _lifetimeCancellation.Token) ||
				!IsCurrentEmulation(connector, generation))
			{
				return;
			}

			var subscription = new Subscription(window.CriteriaMessage);
			_emulationMarketSubscriptions.Add(subscription);
			_subscriptionGenerations[subscription] = _selectionGeneration;
			connector.Subscribe(subscription);
			_status.Text = "Security lookup submitted";
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			SetStatusIfAlive($"Security lookup failed: {error.Message}");
		}
	}

	private void OnNewOrderClick(object sender, RoutedEventArgs e)
		=> OnOrderRegistering();

	private void OnOrderRegistering()
		=> TrackUiTask(RegisterOrderAsync());

	private async Task RegisterOrderAsync()
	{
		var connector = _emulationConnector;
		var generation = _emulationGeneration;
		if (connector is null || _security is null || _isClosing)
		{
			SetStatusIfAlive("Select a security before registering an order");
			return;
		}

		try
		{
			using var portfolios = CreatePortfolioDataSource(connector);
			using var window = new OrderWindow
			{
				Order = new Order { Security = _security },
				SecurityProvider = connector,
				MarketDataProvider = connector,
				Portfolios = portfolios,
			};
			if (!await window.ShowDialogAsync<bool>(this, _lifetimeCancellation.Token) ||
				!IsCurrentEmulation(connector, generation))
			{
				return;
			}

			var order = window.Order;
			if (ReferenceEquals(order.Portfolio, _emulatedPortfolio))
				connector.RegisterOrder(order);
			else
				_realContext.Connector.RegisterOrder(order);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			SetStatusIfAlive($"Order registration failed: {error.Message}");
		}
	}

	private void OnOrderCanceling(Order order)
	{
		if (order is null || _isClosing)
			return;

		if (ReferenceEquals(order.Portfolio, _emulatedPortfolio))
			_emulationConnector?.CancelOrder(order);
		else
			_realContext.Connector.CancelOrder(order);
	}

	private void OnOrderReRegistering(Order order)
		=> TrackUiTask(ReRegisterOrderAsync(order));

	private async Task ReRegisterOrderAsync(Order order)
	{
		var connector = _emulationConnector;
		var generation = _emulationGeneration;
		if (connector is null || order is null || _isClosing)
			return;

		try
		{
			using var portfolios = new PortfolioDataSource(connector);
			if (order.Portfolio is { } portfolio && !portfolios.Contains(portfolio))
				portfolios.Add(portfolio);
			using var window = new OrderWindow
			{
				Title = $"Re-register order {order.TransactionId}",
				SecurityProvider = connector,
				MarketDataProvider = connector,
				Portfolios = portfolios,
				Order = order.ReRegisterClone(newVolume: order.Balance),
			};
			if (!await window.ShowDialogAsync<bool>(this, _lifetimeCancellation.Token) ||
				!IsCurrentEmulation(connector, generation))
			{
				return;
			}

			if (ReferenceEquals(order.Portfolio, _emulatedPortfolio))
				connector.ReRegisterOrder(order, window.Order);
			else
				_realContext.Connector.ReRegisterOrder(order, window.Order);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			SetStatusIfAlive($"Order re-registration failed: {error.Message}");
		}
	}

	private PortfolioDataSource CreatePortfolioDataSource(RealTimeEmulationTrader<IMessageAdapter> connector)
	{
		var portfolios = new PortfolioDataSource(connector);
		if (!portfolios.Contains(_emulatedPortfolio))
			portfolios.Add(_emulatedPortfolio);
		foreach (var portfolio in _realContext.Connector.Portfolios)
		{
			if (!portfolios.Contains(portfolio))
				portfolios.Add(portfolio);
		}
		return portfolios;
	}

	private void OnChartRegisterOrder(IChartArea area, Order order)
		=> _emulationConnector?.RegisterOrder(order);

	private void OnChartCancelOrder(Order order)
		=> _emulationConnector?.CancelOrder(order);

	private void OnChartMoveOrder(Order order, decimal newPrice)
		=> _emulationConnector?.ReRegisterOrder(order, newPrice, order.Balance);

	private void TrackUiTask(Task task)
	{
		if (_isClosing)
			return;

		_uiTasks.Add(task);
		_ = ObserveUiTaskAsync(task);
	}

	private async Task ObserveUiTaskAsync(Task task)
	{
		try
		{
			await task;
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			SetStatusIfAlive(error.Message);
		}
		finally
		{
			_uiTasks.Remove(task);
		}
	}

	private void DisposeEmulationConnector()
	{
		var connector = _emulationConnector;
		if (connector is null)
			return;

		_emulationConnector = null;
		_emulationGeneration++;
		ClearMarketSubscriptions(connector);
		_emulationEvents?.Dispose();
		_emulationEvents = null;
		_securityPicker.MarketDataProvider = null;
		TryRemoveLogSource(connector);
		TryDisconnect(connector);
		TryDispose(connector);
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

	private void SetStatusIfAlive(string status)
	{
		if (!_isClosing)
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
		DetachUiEvents();
		var pendingTasks = _uiTasks.ToArray();
		try
		{
			await Task.WhenAll(pendingTasks);
		}
		catch
		{
		}
		finally
		{
			Closing -= OnClosing;
			Opened -= OnOpened;
			DisposeEmulationConnector();
			_realConnectorEvents.Dispose();
			TryDisconnect(_realContext.Connector);
			_uiEvents.Dispose();
			TryRemoveLogSource(_realContext.Connector);
			TryDispose(_logManager);
			TryDispose(_logControl);
			TryDispose(_securityPicker);
			TryDispose(_emulatedDepth);
			TryDispose(_realDepth);
			TryDispose(_portfolioGrid);
			TryDispose(_orderGrid);
			TryDispose(_myTradeGrid);
			TryDispose(_realContext);
			TryDispose(_lifetimeCancellation);
			_closeApproved = true;
			Close();
		}
	}

	private void DetachUiEvents()
	{
		_securityPicker.SecuritySelected -= OnSecuritySelected;
		_candleDataTypeEdit.DataTypeChanged -= OnCandleDataTypeChanged;
		_orderGrid.OrderRegistering -= OnOrderRegistering;
		_orderGrid.OrderCanceling -= OnOrderCanceling;
		_orderGrid.OrderReRegistering -= OnOrderReRegistering;
		_chart.RegisterOrder -= OnChartRegisterOrder;
		_chart.CancelOrder -= OnChartCancelOrder;
		_chart.MoveOrder -= OnChartMoveOrder;
	}

	private static void TryUnsubscribe(Connector connector, Subscription subscription)
	{
		try
		{
			connector.UnSubscribe(subscription);
		}
		catch
		{
		}
	}

	private static void TryDisconnect(Connector connector)
	{
		try
		{
			if (connector?.ConnectionState != ConnectionStates.Disconnected)
				connector?.Disconnect();
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
