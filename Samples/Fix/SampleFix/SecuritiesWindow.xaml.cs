namespace SampleFix
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Fix;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			var trader = MainWindow.Instance.Trader;
			if (trader != null)
			{
				if (_initialized)
					trader.MarketDepthsChanged -= TraderOnMarketDepthsChanged;

				_quotesWindows.SyncDo(d =>
				{
					foreach (var pair in d)
					{
						trader.UnRegisterMarketDepth(pair.Key);

						pair.Value.DeleteHideable();
						pair.Value.Close();
					}
				});

				trader.RegisteredSecurities.ForEach(trader.UnRegisterSecurity);
				trader.RegisteredTrades.ForEach(trader.UnRegisterTrades);
			}

			base.OnClosed(e);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Quotes.IsEnabled = NewStopOrder.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled = security != null;
		}

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
				Connector = MainWindow.Instance.Trader,
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void NewStopOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderConditionalWindow
			{
				Order = new Order
				{
					Security = SecurityPicker.SelectedSecurity,
					Type = OrderTypes.Conditional,
					Condition = new FixOrderCondition()
				},
				Connector = MainWindow.Instance.Trader,
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			var trader = MainWindow.Instance.Trader;

			var window = _quotesWindows.SafeAdd(SecurityPicker.SelectedSecurity, security =>
			{
				// начинаем получать котировки стакана
				trader.RegisterMarketDepth(security);

				// создаем окно со стаканом
				var wnd = new QuotesWindow { Title = security.Id + " " + LocalizedStrings.MarketDepth };
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();

			if (!_initialized)
			{
				TraderOnMarketDepthsChanged(new[] { trader.GetMarketDepth(SecurityPicker.SelectedSecurity) });
				trader.MarketDepthsChanged += TraderOnMarketDepthsChanged;
				_initialized = true;
			}
		}

		private void QuotesClick(object sender, RoutedEventArgs e)
		{
			var security = SecurityPicker.SelectedSecurity;
			var trader = MainWindow.Instance.Trader;

			if (trader.RegisteredSecurities.Contains(security))
			{
				trader.UnRegisterSecurity(security);
				trader.UnRegisterTrades(security);
			}
			else
			{
				trader.RegisterSecurity(security);
				trader.RegisterTrades(security);
			}
		}

		private void TraderOnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
		{
			foreach (var depth in depths)
			{
				var wnd = _quotesWindows.TryGetValue(depth.Security);

				if (wnd != null)
					wnd.DepthCtrl.UpdateDepth(depth);
			}
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			new FindSecurityWindow().ShowModal(this);
		}
	}
}