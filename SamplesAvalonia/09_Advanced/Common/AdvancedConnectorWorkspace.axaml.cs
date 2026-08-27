namespace StockSharp.Samples.Advanced.Avalonia;

using System;
using System.Collections.Generic;
using System.Linq;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml;
using StockSharp.Xaml.Charting.Avalonia;
using StockSharp.Xaml.Grids.Avalonia;

using MonitorControl = StockSharp.Xaml.Windows.Avalonia.Monitor;

internal partial class AdvancedConnectorWorkspace : UserControl, IDisposable
{
	private readonly SampleConnectorContext _context;
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly EventSubscription _events;
	private readonly List<Subscription> _subscriptions = [];
	private readonly LogManager _logManager;
	private readonly SecurityPicker _securityPicker;
	private readonly PortfolioEditor _portfolioEditor;
	private readonly MarketDepthControl _marketDepth;
	private readonly IChart _chart;
	private readonly IChartCandleElement _candleElement;
	private readonly PortfolioGrid _portfolioGrid;
	private readonly OrderGrid _orderGrid;
	private readonly MyTradeGrid _myTradeGrid;
	private readonly TradeGrid _tradeGrid;
	private readonly OrderLogGrid _orderLogGrid;
	private readonly Level1Grid _level1Grid;
	private readonly NewsGrid _newsGrid;
	private readonly MonitorControl _monitor;
	private readonly Button _settingsButton;
	private readonly Button _connectButton;
	private readonly Button[] _securityActions;
	private readonly TextBox _price;
	private readonly TextBox _volume;
	private readonly TextBlock _status;
	private Security _selectedSecurity;
	private Subscription _candleSubscription;
	private bool _connectStarted;
	private bool _connected;
	private bool _disposed;

	public AdvancedConnectorWorkspace(SampleConnectorContext context)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		AvaloniaXamlLoader.Load(this);

		_securityPicker = this.FindControl<SecurityPicker>("SecurityPicker");
		_portfolioEditor = this.FindControl<PortfolioEditor>("PortfolioEditor");
		_marketDepth = this.FindControl<MarketDepthControl>("MarketDepth");
		_chart = this.FindControl<ChartControl>("Chart");
		_portfolioGrid = this.FindControl<PortfolioGrid>("PortfolioGrid");
		_orderGrid = this.FindControl<OrderGrid>("OrderGrid");
		_myTradeGrid = this.FindControl<MyTradeGrid>("MyTradeGrid");
		_tradeGrid = this.FindControl<TradeGrid>("TradeGrid");
		_orderLogGrid = this.FindControl<OrderLogGrid>("OrderLogGrid");
		_level1Grid = this.FindControl<Level1Grid>("Level1Grid");
		_newsGrid = this.FindControl<NewsGrid>("NewsGrid");
		_monitor = this.FindControl<MonitorControl>("LogMonitor");
		_settingsButton = this.FindControl<Button>("SettingsButton");
		_connectButton = this.FindControl<Button>("ConnectButton");
		_price = this.FindControl<TextBox>("PriceTextBox");
		_volume = this.FindControl<TextBox>("VolumeTextBox");
		_status = this.FindControl<TextBlock>("StatusText");
		_securityActions =
		[
			this.FindControl<Button>("Level1Button"),
			this.FindControl<Button>("TicksButton"),
			this.FindControl<Button>("DepthButton"),
			this.FindControl<Button>("OrderLogButton"),
			this.FindControl<Button>("CandlesButton"),
		];

		_securityPicker.SecurityProvider = _context.Connector;
		_securityPicker.MarketDataProvider = _context.Connector;
		_portfolioEditor.PortfolioProvider = _context.Connector;
		_newsGrid.SubscriptionProvider = _context.Connector;

		var area = _chart.AddArea();
		_candleElement = _chart.CreateCandleElement();
		area.Elements.Add(_candleElement);

		_logManager = new LogManager();
		_logManager.Listeners.Add(new GuiLogListener(_monitor));
		_logManager.Sources.Add(_context.Connector);

		_events = new(AttachEvents, DetachEvents);
		_events.Attach();
	}

	public void Open()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_context.AutoConnect)
			StartConnect();
	}

	private void AttachEvents()
	{
		_securityPicker.SecuritySelected += OnSecuritySelected;
		_orderGrid.OrderCanceling += OnOrderCanceling;
		_orderGrid.OrderReRegistering += OnOrderReRegistering;
		_marketDepth.RegisteringOrder += OnDepthRegisteringOrder;
		_marketDepth.MovingOrder += OnDepthMovingOrder;
		_marketDepth.CancelingOrder += OnOrderCanceling;

		var connector = _context.Connector;
		connector.Connected += OnConnected;
		connector.ConnectionError += OnConnectionError;
		connector.Disconnected += OnDisconnected;
		connector.PortfolioReceived += OnPortfolioReceived;
		connector.PositionReceived += OnPositionReceived;
		connector.OrderReceived += OnOrderReceived;
		connector.OrderRegisterFailReceived += OnOrderRegisterFailReceived;
		connector.OrderCancelFailReceived += OnOrderCancelFailReceived;
		connector.OwnTradeReceived += OnOwnTradeReceived;
		connector.TickTradeReceived += OnTickTradeReceived;
		connector.OrderLogReceived += OnOrderLogReceived;
		connector.Level1Received += OnLevel1Received;
		connector.OrderBookReceived += OnOrderBookReceived;
		connector.CandleReceived += OnCandleReceived;
		connector.NewsReceived += OnNewsReceived;
		connector.SubscriptionFailed += OnSubscriptionFailed;
	}

	private void DetachEvents()
	{
		_securityPicker.SecuritySelected -= OnSecuritySelected;
		_orderGrid.OrderCanceling -= OnOrderCanceling;
		_orderGrid.OrderReRegistering -= OnOrderReRegistering;
		_marketDepth.RegisteringOrder -= OnDepthRegisteringOrder;
		_marketDepth.MovingOrder -= OnDepthMovingOrder;
		_marketDepth.CancelingOrder -= OnOrderCanceling;

		var connector = _context.Connector;
		connector.Connected -= OnConnected;
		connector.ConnectionError -= OnConnectionError;
		connector.Disconnected -= OnDisconnected;
		connector.PortfolioReceived -= OnPortfolioReceived;
		connector.PositionReceived -= OnPositionReceived;
		connector.OrderReceived -= OnOrderReceived;
		connector.OrderRegisterFailReceived -= OnOrderRegisterFailReceived;
		connector.OrderCancelFailReceived -= OnOrderCancelFailReceived;
		connector.OwnTradeReceived -= OnOwnTradeReceived;
		connector.TickTradeReceived -= OnTickTradeReceived;
		connector.OrderLogReceived -= OnOrderLogReceived;
		connector.Level1Received -= OnLevel1Received;
		connector.OrderBookReceived -= OnOrderBookReceived;
		connector.CandleReceived -= OnCandleReceived;
		connector.NewsReceived -= OnNewsReceived;
		connector.SubscriptionFailed -= OnSubscriptionFailed;
	}

	private async void OnSettingsClick(object sender, RoutedEventArgs e)
	{
		try
		{
			var owner = TopLevel.GetTopLevel(this) as Window;
			await _context.ConfigureAsync(owner);
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception error)
		{
			_status.Text = $"Settings failed: {error.Message}";
		}
	}

	private void OnConnectClick(object sender, RoutedEventArgs e)
	{
		if (_connected)
		{
			_connectButton.IsEnabled = false;
			_status.Text = "Disconnecting";
			_context.Connector.Disconnect();
		}
		else
		{
			StartConnect();
		}
	}

	private void StartConnect()
	{
		if (_connectStarted || _disposed)
			return;

		_connectStarted = true;
		_settingsButton.IsEnabled = false;
		_connectButton.IsEnabled = false;
		_status.Text = "Connecting";
		_context.Connector.Connect();
	}

	private void OnConnected()
		=> _uiEvents.Dispatch(() =>
		{
			if (_disposed)
				return;
			_connected = true;
			_connectStarted = false;
			_connectButton.IsEnabled = true;
			_connectButton.Content = "Disconnect";
			_status.Text = "Connected";
			_context.Connector.LookupAll();
		});

	private void OnConnectionError(Exception error)
		=> _uiEvents.Dispatch(() => ResetConnectionUi($"Connection failed: {error.Message}"));

	private void OnDisconnected()
		=> _uiEvents.Dispatch(() => ResetConnectionUi("Disconnected"));

	private void ResetConnectionUi(string status)
	{
		if (_disposed)
			return;
		_connected = false;
		_connectStarted = false;
		_settingsButton.IsEnabled = true;
		_connectButton.IsEnabled = true;
		_connectButton.Content = "Connect";
		_status.Text = status;
	}

	private void OnSecuritySelected(Security security)
	{
		UnsubscribeSecurityData();
		_selectedSecurity = security;
		_marketDepth.Clear();
		_chart.Reset([_candleElement]);
		foreach (var action in _securityActions)
			action.IsEnabled = security is not null;
	}

	private void OnLevel1Click(object sender, RoutedEventArgs e)
		=> SubscribeSelected(DataType.Level1);

	private void OnTicksClick(object sender, RoutedEventArgs e)
		=> SubscribeSelected(DataType.Ticks);

	private void OnDepthClick(object sender, RoutedEventArgs e)
		=> SubscribeSelected(DataType.MarketDepth);

	private void OnOrderLogClick(object sender, RoutedEventArgs e)
		=> SubscribeSelected(DataType.OrderLog);

	private void OnCandlesClick(object sender, RoutedEventArgs e)
	{
		if (_selectedSecurity is null)
			return;

		RemoveSubscriptions(subscription => ReferenceEquals(subscription, _candleSubscription));
		_candleSubscription = new(TimeSpan.FromMinutes(5).TimeFrame(), _selectedSecurity)
		{
			From = DateTime.UtcNow - TimeSpan.FromDays(10),
		};
		_subscriptions.Add(_candleSubscription);
		_context.Connector.Subscribe(_candleSubscription);
	}

	private void OnNewsClick(object sender, RoutedEventArgs e)
	{
		RemoveSubscriptions(subscription => subscription.DataType == DataType.News);
		var subscription = new Subscription(DataType.News);
		_subscriptions.Add(subscription);
		_context.Connector.Subscribe(subscription);
	}

	private void SubscribeSelected(DataType dataType)
	{
		if (_selectedSecurity is null)
			return;

		var securityId = _selectedSecurity.ToSecurityId();
		RemoveSubscriptions(subscription => subscription.DataType == dataType && subscription.SecurityId == securityId);
		var next = new Subscription(dataType, _selectedSecurity);
		_subscriptions.Add(next);
		_context.Connector.Subscribe(next);
	}

	private void UnsubscribeSecurityData()
	{
		var selectedId = _selectedSecurity?.ToSecurityId();
		if (selectedId is not null)
			RemoveSubscriptions(subscription => subscription.SecurityId == selectedId.Value);
		_candleSubscription = null;
	}

	private void RemoveSubscriptions(Func<Subscription, bool> predicate)
	{
		foreach (var subscription in _subscriptions.Where(predicate).ToArray())
		{
			_subscriptions.Remove(subscription);
			try
			{
				_context.Connector.UnSubscribe(subscription);
			}
			catch
			{
			}
		}
	}

	private void OnBuyClick(object sender, RoutedEventArgs e)
		=> RegisterOrder(Sides.Buy, _price.Text.To<decimal>());

	private void OnSellClick(object sender, RoutedEventArgs e)
		=> RegisterOrder(Sides.Sell, _price.Text.To<decimal>());

	private void OnDepthRegisteringOrder(Sides side, decimal price)
		=> RegisterOrder(side, price);

	private void RegisterOrder(Sides side, decimal price)
	{
		var security = _selectedSecurity;
		var portfolio = _portfolioEditor.SelectedPortfolio;
		if (security is null || portfolio is null)
		{
			_status.Text = "Select a security and portfolio.";
			return;
		}

		_context.Connector.RegisterOrder(new Order
		{
			Security = security,
			Portfolio = portfolio,
			Side = side,
			Price = price,
			Volume = Math.Max(1m, _volume.Text.To<decimal>()),
		});
	}

	private void OnOrderCanceling(Order order)
		=> _context.Connector.CancelOrder(order);

	private void OnOrderReRegistering(Order order)
	{
		var price = _price.Text.To<decimal>();
		_context.Connector.ReRegisterOrderEx(
			order,
			order.ReRegisterClone(newPrice: price == 0 ? order.Price : price, newVolume: order.Balance));
	}

	private void OnDepthMovingOrder(Order order, decimal newPrice)
		=> _context.Connector.ReRegisterOrderEx(order, order.ReRegisterClone(newPrice: newPrice, newVolume: order.Balance));

	private void OnPortfolioReceived(Subscription subscription, Portfolio portfolio)
		=> Dispatch(() =>
		{
			_portfolioGrid.Positions.TryAdd(portfolio);
			if (_portfolioEditor.SelectedPortfolio is null)
				_portfolioEditor.SelectedPortfolio = portfolio;
		});

	private void OnPositionReceived(Subscription subscription, Position position)
		=> Dispatch(() => _portfolioGrid.Positions.TryAdd(position));

	private void OnOrderReceived(Subscription subscription, Order order)
		=> Dispatch(() =>
		{
			_orderGrid.Orders.TryAdd(order);
			_marketDepth.ProcessOrder(order, order.Price, order.Balance, order.State);
		});

	private void OnOrderRegisterFailReceived(Subscription subscription, OrderFail fail)
		=> Dispatch(() =>
		{
			_orderGrid.AddRegistrationFail(fail);
			_marketDepth.ProcessOrderFail(fail, fail.Order.State);
		});

	private void OnOrderCancelFailReceived(Subscription subscription, OrderFail fail)
		=> Dispatch(() =>
		{
			_status.Text = $"Cancel failed: {fail.Error.Message}";
			_marketDepth.ProcessOrderFail(fail, fail.Order.State);
		});

	private void OnOwnTradeReceived(Subscription subscription, MyTrade trade)
		=> Dispatch(() => _myTradeGrid.Trades.TryAdd(trade));

	private void OnTickTradeReceived(Subscription subscription, ITickTradeMessage trade)
	{
		var snapshot = trade is Message message ? (ITickTradeMessage)message.Clone() : trade;
		Dispatch(() => _tradeGrid.Trades.Add(snapshot));
	}

	private void OnOrderLogReceived(Subscription subscription, IOrderLogMessage item)
	{
		var snapshot = item is Message message ? (IOrderLogMessage)message.Clone() : item;
		Dispatch(() => _orderLogGrid.LogItems.Add(snapshot));
	}

	private void OnLevel1Received(Subscription subscription, Level1ChangeMessage level1)
	{
		var snapshot = (Level1ChangeMessage)level1.Clone();
		Dispatch(() => _level1Grid.Messages.Add(snapshot));
	}

	private void OnOrderBookReceived(Subscription subscription, IOrderBookMessage depth)
	{
		var snapshot = depth is ICloneable cloneable ? (IOrderBookMessage)cloneable.Clone() : depth;
		Dispatch(() =>
		{
			if (snapshot.SecurityId == _selectedSecurity?.ToSecurityId())
				_marketDepth.UpdateDepth(snapshot, _selectedSecurity);
		});
	}

	private void OnCandleReceived(Subscription subscription, ICandleMessage candle)
	{
		if (!ReferenceEquals(subscription, _candleSubscription))
			return;
		var snapshot = candle is Message message ? (ICandleMessage)message.Clone() : candle;
		Dispatch(() => _chart.Draw(_candleElement, snapshot));
	}

	private void OnNewsReceived(Subscription subscription, News news)
		=> Dispatch(() => _newsGrid.News.Add(news));

	private void OnSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
	{
		if (error is not null)
			Dispatch(() => _status.Text = $"Subscription failed: {error.Message}");
	}

	private void Dispatch(Action action)
		=> _uiEvents.Dispatch(() =>
		{
			if (!_disposed)
				action();
		});

	public void Dispose()
	{
		if (_disposed)
			return;
		_disposed = true;
		_events.Dispose();
		RemoveSubscriptions(_ => true);
		_uiEvents.Dispose();
		try
		{
			_logManager.Sources.Remove(_context.Connector);
		}
		catch
		{
		}
		TryDispose(_securityPicker);
		TryDispose(_portfolioEditor);
		TryDispose(_marketDepth);
		TryDispose(_chart);
		TryDispose(_portfolioGrid);
		TryDispose(_orderGrid);
		TryDispose(_myTradeGrid);
		TryDispose(_tradeGrid);
		TryDispose(_orderLogGrid);
		TryDispose(_level1Grid);
		TryDispose(_newsGrid);
		TryDispose(_logManager);
		TryDispose(_monitor);
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
