namespace StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.Collections;
using Ecng.ComponentModel;
using Ecng.Logging;
using Ecng.Xaml.Avalonia;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;
using StockSharp.Xaml;
using StockSharp.Xaml.Grids.Avalonia;
using StockSharp.Xaml.Windows.Avalonia;

public partial class SecuritiesWindow : Window, IDisposable
{
	private sealed class DepthSession(
		Security security,
		Subscription subscription,
		QuotesWindow window,
		int generation)
	{
		public Security Security { get; } = security;
		public Subscription Subscription { get; } = subscription;
		public QuotesWindow Window { get; } = window;
		public int Generation { get; } = generation;
		public EventSubscription WindowEvents { get; set; }
	}

	private readonly Connector _connector;
	private readonly PortfolioDataSource _portfolios;
	private readonly SecurityPicker _picker;
	private readonly Button _level1Button;
	private readonly Button _depthButton;
	private readonly Button _newOrderButton;
	private readonly SampleUiEventRouter _uiEvents = new(AvaloniaSampleUiDispatcher.Instance);
	private readonly CancellationTokenSource _lifetimeCancellation = new();
	private readonly EventSubscription _events;
	private readonly Dictionary<SecurityId, DepthSession> _depthSessions = [];
	private readonly object _depthSync = new();
	private int _depthGeneration;
	private bool _disposed;

	public SecuritiesWindow(Connector connector, PortfolioDataSource portfolios)
	{
		_connector = connector ?? throw new ArgumentNullException(nameof(connector));
		_portfolios = portfolios ?? throw new ArgumentNullException(nameof(portfolios));
		InitializeComponent();

		_picker = this.FindControl<SecurityPicker>(nameof(SecurityPicker));
		_level1Button = this.FindControl<Button>(nameof(Level1Button));
		_depthButton = this.FindControl<Button>(nameof(DepthButton));
		_newOrderButton = this.FindControl<Button>(nameof(NewOrderButton));
		_picker.MarketDataProvider = _connector;

		_events = new(
			() =>
			{
				_picker.SecuritySelected += OnSecuritySelected;
				_connector.OrderBookReceived += OnOrderBookReceived;
			},
			() =>
			{
				_picker.SecuritySelected -= OnSecuritySelected;
				_connector.OrderBookReceived -= OnOrderBookReceived;
			});
		_events.Attach();
	}

	public void AddSecurity(Security security)
	{
		if (!_disposed)
			_picker.Securities.Add(security);
	}

	public void ProcessOrder(Order order)
	{
		if (_disposed || order?.Security is null)
			return;

		DepthSession session;
		lock (_depthSync)
			_depthSessions.TryGetValue(order.Security.ToSecurityId(), out session);

		if (session is not null)
			session.Window.ProcessOrder(order);
	}

	private void OnSecuritySelected(Security security)
	{
		var enabled = security is not null;
		_level1Button.IsEnabled = enabled;
		_depthButton.IsEnabled = enabled;
		_newOrderButton.IsEnabled = enabled;
	}

	private async void OnFindClick(object sender, RoutedEventArgs e)
	{
		try
		{
			using var window = new SecurityLookupWindow
			{
				ShowAllOption = _connector.Adapter.IsSupportSecuritiesLookupAll(),
				CriteriaMessage = new SecurityLookupMessage
				{
					SecurityId = new SecurityId { BoardCode = "IS" },
				},
			};
			if (await window.ShowDialogAsync<bool>(this, _lifetimeCancellation.Token))
				_connector.Subscribe(new Subscription(window.CriteriaMessage));
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			error.LogError();
		}
	}

	private async void OnNewOrderClick(object sender, RoutedEventArgs e)
	{
		var security = _picker.SelectedSecurity;
		if (security is null)
			return;

		try
		{
			using var window = new OrderWindow
			{
				Order = new Order { Security = security },
				SecurityProvider = _connector,
				MarketDataProvider = _connector,
				Portfolios = _portfolios,
			};
			if (await window.ShowDialogAsync<bool>(this, _lifetimeCancellation.Token))
				_connector.RegisterOrder(window.Order);
		}
		catch (OperationCanceledException) when (_lifetimeCancellation.IsCancellationRequested)
		{
		}
		catch (Exception error)
		{
			error.LogError();
		}
	}

	private void OnLevel1Click(object sender, RoutedEventArgs e)
	{
		foreach (var security in _picker.SelectedSecurities.ToArray())
		{
			var subscription = _connector
				.FindSubscriptions(security, DataType.Level1)
				.FirstOrDefault(item => item.SubscriptionMessage.To is null && item.State.IsActive());
			if (subscription is null)
				_connector.Subscribe(new Subscription(DataType.Level1, security));
			else
				_connector.UnSubscribe(subscription);
		}
	}

	private void OnDepthClick(object sender, RoutedEventArgs e)
	{
		foreach (var security in _picker.SelectedSecurities.ToArray())
		{
			var securityId = security.ToSecurityId();
			DepthSession session;
			lock (_depthSync)
				_depthSessions.TryGetValue(securityId, out session);

			if (session is null)
			{
				var subscription = new Subscription(DataType.MarketDepth, security);
				var window = new QuotesWindow { Title = $"{security.Id} Market depth" };
				session = new DepthSession(security, subscription, window, ++_depthGeneration);
				session.WindowEvents = new(
					() => window.Closing += OnDepthWindowClosing,
					() => window.Closing -= OnDepthWindowClosing);
				session.WindowEvents.Attach();
				lock (_depthSync)
					_depthSessions.Add(securityId, session);
				_connector.Subscribe(subscription);
			}

			if (session.Window.IsVisible)
				session.Window.Hide();
			else
				session.Window.Show(this);
		}
	}

	private void OnDepthWindowClosing(object sender, WindowClosingEventArgs e)
	{
		if (_disposed || sender is not Window window)
			return;

		e.Cancel = true;
		window.Hide();
	}

	private void OnOrderBookReceived(Subscription subscription, IOrderBookMessage depth)
	{
		DepthSession session;
		lock (_depthSync)
			_depthSessions.TryGetValue(depth.SecurityId, out session);

		if (session is null)
			return;

		var generation = session.Generation;
		_uiEvents.Dispatch(() =>
		{
			DepthSession current;
			lock (_depthSync)
				_depthSessions.TryGetValue(depth.SecurityId, out current);

			if (!_disposed && ReferenceEquals(current, session) && current.Generation == generation)
			{
				session.Window.Update(depth);
			}
		});
	}

	public void Dispose()
	{
		if (_disposed)
			return;

		_disposed = true;
		_lifetimeCancellation.Cancel();
		_events.Dispose();
		_uiEvents.Dispose();
		DepthSession[] sessions;
		lock (_depthSync)
		{
			sessions = [.. _depthSessions.Values];
			_depthSessions.Clear();
		}

		foreach (var session in sessions)
		{
			try
			{
				_connector.UnSubscribe(session.Subscription);
			}
			catch
			{
			}
			session.WindowEvents.Dispose();
			try
			{
				session.Window.Close();
			}
			catch
			{
			}
			session.Window.Dispose();
		}
		_picker.Dispose();
		_lifetimeCancellation.Dispose();
	}
}
