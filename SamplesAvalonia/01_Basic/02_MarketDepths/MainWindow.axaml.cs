namespace StockSharp.Samples.Basic.MarketDepths.Avalonia;

using System;
using System.Collections.Generic;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml.Grids.Avalonia;

public partial class MainWindow : Window
{
	private readonly SampleConnectorContext _context = new();
	private readonly EventSubscription _connectorEvents;
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly List<Subscription> _subscriptions = [];
	private readonly SecurityPicker _securityPicker;
	private readonly TradeGrid _tradeGrid;
	private readonly MarketDepthControl _marketDepth;
	private readonly Button _settingsButton;
	private SecurityId? _selectedSecurityId;
	private bool _connectStarted;

	public MainWindow()
	{
		InitializeComponent();
		_securityPicker = this.FindControl<SecurityPicker>(nameof(SecurityPicker));
		_tradeGrid = this.FindControl<TradeGrid>(nameof(TradeGrid));
		_marketDepth = this.FindControl<MarketDepthControl>(nameof(MarketDepthControl));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		_securityPicker.SecuritySelected += OnSecuritySelected;
		Opened += OnOpened;
		Closed += OnClosed;

		_connectorEvents = new(
			() =>
			{
				_context.Connector.TickTradeReceived += OnTickTradeReceived;
				_context.Connector.OrderBookReceived += OnOrderBookReceived;
			},
			() =>
			{
				_context.Connector.TickTradeReceived -= OnTickTradeReceived;
				_context.Connector.OrderBookReceived -= OnOrderBookReceived;
			});
	}

	private async void OnSettingsClick(object sender, RoutedEventArgs e)
	{
		try
		{
			await _context.ConfigureAsync(this);
		}
		catch (OperationCanceledException)
		{
			// Closing the owner cancels its settings dialog.
		}
	}

	private void OnConnectClick(object sender, RoutedEventArgs e)
		=> StartConnect();

	private void OnOpened(object sender, EventArgs e)
	{
		if (_context.AutoConnect)
			StartConnect();
	}

	private void StartConnect()
	{
		if (_connectStarted)
			return;

		_connectStarted = true;
		_settingsButton.IsEnabled = false;
		_securityPicker.SecurityProvider = _context.Connector;
		_securityPicker.MarketDataProvider = _context.Connector;
		_connectorEvents.Attach();
		_context.Connector.Connect();
	}

	private void OnOrderBookReceived(Subscription subscription, IOrderBookMessage depth)
	{
		var snapshot = depth is ICloneable cloneable
			? (IOrderBookMessage)cloneable.Clone()
			: depth;
		_uiEvents.Dispatch(() =>
		{
			if (snapshot.SecurityId == _selectedSecurityId)
				_marketDepth.UpdateDepth(snapshot);
		});
	}

	private void OnTickTradeReceived(Subscription subscription, ITickTradeMessage trade)
	{
		var snapshot = trade is Message message
			? (ITickTradeMessage)message.Clone()
			: trade;
		_uiEvents.Dispatch(() =>
		{
			if (snapshot.SecurityId == _selectedSecurityId)
				_tradeGrid.Trades.Add(snapshot);
		});
	}

	private void OnSecuritySelected(Security security)
	{
		UnsubscribeAll();
		_selectedSecurityId = security?.ToSecurityId();

		if (security is null)
			return;

		Subscribe(DataType.Level1, security);
		Subscribe(DataType.Ticks, security);
		_marketDepth.Clear();
		Subscribe(DataType.MarketDepth, security);
	}

	private void Subscribe(DataType dataType, Security security)
	{
		var subscription = new Subscription(dataType, security);
		_subscriptions.Add(subscription);
		_context.Connector.Subscribe(subscription);
	}

	private void UnsubscribeAll()
	{
		foreach (var subscription in _subscriptions)
			_context.Connector.UnSubscribe(subscription);

		_subscriptions.Clear();
	}

	private void OnClosed(object sender, EventArgs e)
	{
		Opened -= OnOpened;
		Closed -= OnClosed;
		_securityPicker.SecuritySelected -= OnSecuritySelected;
		_connectorEvents.Dispose();
		_uiEvents.Dispose();
		UnsubscribeAll();
		_securityPicker.Dispose();
		_context.Dispose();
	}
}
