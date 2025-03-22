namespace StockSharp.Samples.Strategies.LiveTerminal;

using System;
using System.Linq;
using System.Windows;

using Ecng.Collections;
using Ecng.Xaml;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Xaml;
using StockSharp.Localization;
using StockSharp.Messages;

public partial class SecuritiesWindow
{
	private readonly SynchronizedDictionary<SecurityId, QuotesWindow> _quotesWindows = new();
	private bool _initialized;

	public SecuritiesWindow()
	{
		InitializeComponent();
	}

	private static Connector Connector => MainWindow.Instance.Connector;

	protected override void OnClosed(EventArgs e)
	{
		_quotesWindows.SyncDo(d => d.Values.ForEach(w =>
		{
			w.DeleteHideable();
			w.Close();
		}));

		var connector = Connector;

		if (connector != null)
		{
			if (_initialized)
				connector.OrderBookReceived -= TraderOnMarketDepthReceived;
		}

		base.OnClosed(e);
	}

	private void NewOrderClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		var newOrder = new OrderWindow
		{
			Order = new Order { Security = SecurityPicker.SelectedSecurity },
		}.Init(connector);

		if (newOrder.ShowModal(this))
			connector.RegisterOrder(newOrder.Order);
	}

	private void SecurityPicker_OnSecuritySelected(Security security)
	{
		Quotes.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled = security != null;
	}

	private void DepthClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			var window = _quotesWindows.SafeAdd(security.ToSecurityId(), s =>
			{
				// subscribe on order book flow
				connector.Subscribe(new(DataType.MarketDepth, security));

				// create order book window
				var wnd = new QuotesWindow
				{
					Title = security.Id + " " + LocalizedStrings.MarketDepth
				};
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
			{
				window.Show();
				//window.DepthCtrl.UpdateDepth(connector.GetMarketDepth(security));
			}

			if (!_initialized)
			{
				connector.OrderBookReceived += TraderOnMarketDepthReceived;
				_initialized = true;
			}
		}
	}

	private static Subscription FindSubscription(Security security, DataType dataType)
	{
		return Connector.FindSubscriptions(security, dataType).Where(s => s.SubscriptionMessage.To == null && s.State.IsActive()).FirstOrDefault();
	}

	private void QuotesClick(object sender, RoutedEventArgs e)
	{
		var connector = Connector;

		foreach (var security in SecurityPicker.SelectedSecurities)
		{
			var subscription = FindSubscription(security, DataType.Level1);

			if (subscription != null)
				connector.UnSubscribe(subscription);
			else
				connector.Subscribe(new(DataType.Level1, security));
		}
	}

	private void TraderOnMarketDepthReceived(Subscription subscription, IOrderBookMessage depth)
	{
		var wnd = _quotesWindows.TryGetValue(depth.SecurityId);

		if (wnd != null)
			wnd.DepthCtrl.UpdateDepth(depth);
	}

	private void FindClick(object sender, RoutedEventArgs e)
	{
		var wnd = new SecurityLookupWindow
		{
			ShowAllOption = Connector.Adapter.IsSupportSecuritiesLookupAll(),
			CriteriaMessage = new()
			{
				SecurityId = new()
				{
					BoardCode = "IS"
				}
			}
		};

		if (!wnd.ShowModal(this))
			return;

		Connector.Subscribe(new(wnd.CriteriaMessage));
	}

	public void ProcessOrder(Order order)
	{
		_quotesWindows.TryGetValue(order.Security.ToSecurityId())?.DepthCtrl.ProcessOrder(order, order.Price, order.Balance, order.State);
	}
}