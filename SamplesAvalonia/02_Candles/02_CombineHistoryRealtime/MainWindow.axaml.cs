namespace StockSharp.Samples.Candles.CombineHistoryRealtime.Avalonia;

using System;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml.Charting.Avalonia;
using StockSharp.Xaml.Grids.Avalonia;
using StockSharp.Xaml.PropertyGrid.Avalonia.Editors;

public partial class MainWindow : Window
{
	private readonly CombineHistoryRealtimeRuntime _runtime;
	private readonly SampleConnectorContext _context;
	private readonly EventSubscription _connectorEvents;
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly SecurityPicker _securityPicker;
	private readonly CandleDataTypeEdit _candleDataTypeEdit;
	private readonly Button _settingsButton;
	private readonly IChart _chart;
	private Subscription _subscription;
	private IChartCandleElement _candleElement;
	private bool _connectStarted;

	public MainWindow()
	{
		InitializeComponent();

		_securityPicker = this.FindControl<SecurityPicker>(nameof(SecurityPicker));
		_candleDataTypeEdit = this.FindControl<CandleDataTypeEdit>(nameof(CandleDataTypeEdit));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		_chart = this.FindControl<ChartControl>(nameof(Chart));
		_candleDataTypeEdit.DataType = TimeSpan.FromMinutes(5).TimeFrame();
		_runtime = CombineHistoryRealtimeRuntime.Create();
		_context = _runtime.Context;

		_securityPicker.SecuritySelected += OnSecuritySelected;
		Opened += OnOpened;
		Closed += OnClosed;
		_connectorEvents = new(
			() => _context.Connector.CandleReceived += OnCandleReceived,
			() => _context.Connector.CandleReceived -= OnCandleReceived);
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
		_connectorEvents.Attach();
		_context.Connector.Connect();
	}

	private void OnSecuritySelected(Security security)
	{
		if (security is null)
			return;

		UnsubscribeCurrent();

		var subscription = new Subscription(_candleDataTypeEdit.DataType, security)
		{
			MarketData =
			{
				From = DateTime.Today.AddDays(-720),
				BuildMode = MarketDataBuildModes.LoadAndBuild,
			},
		};

		_chart.ClearAreas();
		var area = _chart.CreateArea();
		var candleElement = _chart.CreateCandleElement();
		_chart.AddArea(area);
		_chart.AddElement(area, candleElement, subscription);

		_subscription = subscription;
		_candleElement = candleElement;
		_context.Connector.Subscribe(subscription);
	}

	private void OnCandleReceived(Subscription subscription, ICandleMessage candle)
		=> _uiEvents.Dispatch(() =>
		{
			if (ReferenceEquals(subscription, _subscription) && _candleElement is { } element)
				_chart.Draw(element, candle);
		});

	private void UnsubscribeCurrent()
	{
		if (_subscription is not { } subscription)
			return;

		_subscription = null;
		_candleElement = null;
		_context.Connector.UnSubscribe(subscription);
	}

	private void OnClosed(object sender, EventArgs e)
	{
		Opened -= OnOpened;
		Closed -= OnClosed;
		_securityPicker.SecuritySelected -= OnSecuritySelected;
		_connectorEvents.Dispose();
		_uiEvents.Dispose();
		UnsubscribeCurrent();
		_securityPicker.Dispose();
		_runtime.Dispose();
	}
}
