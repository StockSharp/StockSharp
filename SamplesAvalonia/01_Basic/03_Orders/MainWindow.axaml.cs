namespace StockSharp.Samples.Basic.Orders.Avalonia;

using System;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.Collections;
using Ecng.Common;

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
	private readonly SecurityEditor _securityEditor;
	private readonly PortfolioEditor _portfolioEditor;
	private readonly OrderGrid _orderGrid;
	private readonly MyTradeGrid _myTradeGrid;
	private readonly TextBox _priceTextBox;
	private readonly Button _settingsButton;
	private bool _connectStarted;

	public MainWindow()
	{
		InitializeComponent();
		_securityEditor = this.FindControl<SecurityEditor>(nameof(SecurityEditor));
		_portfolioEditor = this.FindControl<PortfolioEditor>(nameof(PortfolioEditor));
		_orderGrid = this.FindControl<OrderGrid>(nameof(OrderGrid));
		_myTradeGrid = this.FindControl<MyTradeGrid>(nameof(MyTradeGrid));
		_priceTextBox = this.FindControl<TextBox>(nameof(PriceTextBox));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		Opened += OnOpened;
		Closed += OnClosed;

		_connectorEvents = new(
			() =>
			{
				_context.Connector.OrderReceived += OnOrderReceived;
				_context.Connector.OrderRegisterFailReceived += OnOrderRegisterFailReceived;
				_context.Connector.OwnTradeReceived += OnOwnTradeReceived;
			},
			() =>
			{
				_context.Connector.OrderReceived -= OnOrderReceived;
				_context.Connector.OrderRegisterFailReceived -= OnOrderRegisterFailReceived;
				_context.Connector.OwnTradeReceived -= OnOwnTradeReceived;
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
		_securityEditor.SecurityProvider = _context.Connector;
		_portfolioEditor.PortfolioProvider = _context.Connector;
		_connectorEvents.Attach();
		_context.Connector.Connect();
	}

	private void OnOrderReceived(Subscription subscription, Order order)
		=> _uiEvents.Dispatch(() => _orderGrid.Orders.TryAdd(order));

	private void OnOrderRegisterFailReceived(Subscription subscription, OrderFail fail)
		=> _uiEvents.Dispatch(() => _orderGrid.AddRegistrationFail(fail));

	private void OnOwnTradeReceived(Subscription subscription, MyTrade trade)
		=> _uiEvents.Dispatch(() => _myTradeGrid.Trades.TryAdd(trade));

	private void OnBuyClick(object sender, RoutedEventArgs e)
		=> RegisterOrder(Sides.Buy);

	private void OnSellClick(object sender, RoutedEventArgs e)
		=> RegisterOrder(Sides.Sell);

	private void RegisterOrder(Sides side)
	{
		var security = _securityEditor.SelectedSecurity;
		var portfolio = _portfolioEditor.SelectedPortfolio;

		if (security is null || portfolio is null)
			return;

		_context.Connector.RegisterOrder(new Order
		{
			Security = security,
			Portfolio = portfolio,
			Price = _priceTextBox.Text.To<decimal>(),
			Volume = 1,
			Side = side,
		});
	}

	private void OnClosed(object sender, EventArgs e)
	{
		Opened -= OnOpened;
		Closed -= OnClosed;
		_connectorEvents.Dispose();
		_uiEvents.Dispose();
		_securityEditor.Dispose();
		_portfolioEditor.Dispose();
		_context.Dispose();
	}
}
