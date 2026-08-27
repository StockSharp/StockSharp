namespace StockSharp.Samples.Basic.ConnectAndDownloadInstruments.Avalonia;

using System;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml.Grids.Avalonia;

public partial class MainWindow : Window
{
	private readonly SampleConnectorContext _context = new();
	private readonly EventSubscription _connectorEvents;
	private readonly SecurityPicker _securityPicker;
	private readonly Button _settingsButton;
	private bool _connectStarted;

	public MainWindow()
	{
		InitializeComponent();
		_securityPicker = this.FindControl<SecurityPicker>(nameof(SecurityPicker));
		_settingsButton = this.FindControl<Button>(nameof(SettingsButton));
		_securityPicker.SecuritySelected += OnSecuritySelected;
		Opened += OnOpened;
		Closed += OnClosed;

		_connectorEvents = new(
			() => _context.Connector.Connected += OnConnectorConnected,
			() => _context.Connector.Connected -= OnConnectorConnected);
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

	private void OnConnectorConnected()
		=> _context.Connector.Subscribe(new(StockSharp.Messages.Extensions.LookupAllCriteriaMessage));

	private void OnSecuritySelected(Security security)
	{
		if (security is not null)
			_context.Connector.Subscribe(new(DataType.Level1, security));
	}

	private void OnClosed(object sender, EventArgs e)
	{
		Opened -= OnOpened;
		Closed -= OnClosed;
		_securityPicker.SecuritySelected -= OnSecuritySelected;
		_connectorEvents.Dispose();
		_securityPicker.Dispose();
		_context.Dispose();
	}
}
