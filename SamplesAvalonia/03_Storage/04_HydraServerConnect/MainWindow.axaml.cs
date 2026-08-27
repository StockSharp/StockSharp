namespace StockSharp.Samples.Storage.HydraServerConnect.Avalonia;

using System;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.IO;
using Ecng.Serialization;
using Ecng.Xaml.Avalonia;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Charting;
using StockSharp.Configuration;
using StockSharp.Fix;
using StockSharp.Localization;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml.Charting.Avalonia;
using StockSharp.Xaml.Grids.Avalonia;
using StockSharp.Xaml.PropertyGrid.Avalonia.Editors;

public partial class MainWindow : Window
{
	private const string _connectorFile = "ConnectorFile.json";

	private readonly SampleConnectorContext _context;
	private readonly EventSubscription _connectorEvents;
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly SecurityPicker _securityPicker;
	private readonly CandleDataTypeEdit _candleDataTypeEdit;
	private readonly DatePicker _datePickerBegin;
	private readonly DatePicker _datePickerEnd;
	private readonly CheckBox _buildFromTicks;
	private readonly Button _settingsButton;
	private readonly IChart _chart;
	private Subscription _subscription;
	private IChartCandleElement _candleElement;
	private bool _connectStarted;

	public MainWindow()
	{
		InitializeComponent();

		_context = new(CreateConnector());
		_securityPicker = this.FindControl<SecurityPicker>(nameof(SecurityPicker));
		_candleDataTypeEdit = this.FindControl<CandleDataTypeEdit>(nameof(CandleDataTypeEdit));
		_datePickerBegin = this.FindControl<DatePicker>(nameof(DatePickerBegin));
		_datePickerEnd = this.FindControl<DatePicker>(nameof(DatePickerEnd));
		_buildFromTicks = this.FindControl<CheckBox>(nameof(BuildFromTicks));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		_chart = this.FindControl<ChartControl>(nameof(Chart));

		_candleDataTypeEdit.DataType = TimeSpan.FromMinutes(5).TimeFrame();
		_datePickerBegin.SelectedDate = Paths.HistoryBeginDate;
		_datePickerEnd.SelectedDate = Paths.HistoryEndDate;
		_securityPicker.SecurityProvider = _context.Connector;
		_securityPicker.MarketDataProvider = _context.Connector;

		_securityPicker.SecuritySelected += OnSecuritySelected;
		Opened += OnOpened;
		Closed += OnClosed;
		_connectorEvents = new(
			() =>
			{
				_context.Connector.ConnectionError += OnConnectionError;
				_context.Connector.CandleReceived += OnCandleReceived;
			},
			() =>
			{
				_context.Connector.ConnectionError -= OnConnectionError;
				_context.Connector.CandleReceived -= OnCandleReceived;
			});
		_connectorEvents.Attach();
	}

	private static Connector CreateConnector()
	{
		var connector = new Connector();
		var fileSystem = Paths.FileSystem;

		if (fileSystem.FileExists(_connectorFile))
			return connector;

		var adapter = new FixMessageAdapter(connector.TransactionIdGenerator)
		{
			Address = RemoteMarketDataDrive.DefaultAddress,
			TargetCompId = RemoteMarketDataDrive.DefaultTargetCompId,
			SenderCompId = "hydra_user",
		};
		adapter.ChangeSupported(false, false);
		connector.Adapter.InnerAdapters.Add(adapter);
		connector.Save().Serialize(fileSystem, _connectorFile);
		return connector;
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
		_context.Connector.Connect();
	}

	private void OnConnectionError(Exception error)
		=> _uiEvents.Dispatch(() => _ = ShowConnectionErrorAsync(error));

	private async Task ShowConnectionErrorAsync(Exception error)
	{
		try
		{
			await new MessageBoxBuilder()
				.Owner(this)
				.Caption(LocalizedStrings.ErrorConnection)
				.Text(error.ToString())
				.Error()
				.ShowAsync(_lifetimeCancellation.Token);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
	}

	private void OnSecuritySelected(Security security)
	{
		if (security is null)
			return;

		UnsubscribeCurrent();

		var subscription = new Subscription(_candleDataTypeEdit.DataType, security)
		{
			From = _datePickerBegin.SelectedDate?.Date,
			To = _datePickerEnd.SelectedDate?.Date,
		};

		if (_buildFromTicks.IsChecked == true)
		{
			subscription.MarketData.BuildMode = MarketDataBuildModes.Build;
			subscription.MarketData.BuildFrom = DataType.Ticks;
		}

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
		_lifetimeCancellation.Cancel();
		UnsubscribeCurrent();
		_securityPicker.Dispose();
		_context.Dispose();
		_lifetimeCancellation.Dispose();
	}
}
