namespace SampleSterling
{
	using System.Windows;
	using System.Collections.Generic;
	using System.Linq;
	
	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private readonly SynchronizedDictionary<Security, TradesWindow> _tradesWindows = new SynchronizedDictionary<Security, TradesWindow>();
		private bool _initialized;

		public Security SelectedSecurity
		{
			get { return SecurityPicker.SelectedSecurity; }
		}

		public SecuritiesWindow()
		{
			InitializeComponent();
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
				},
				Connector = MainWindow.Instance.Trader,
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			NewOrder.IsEnabled = NewStopOrder.IsEnabled = Trades.IsEnabled = Depth.IsEnabled = security != null;
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			new FindSecurityWindow().ShowModal(this);
		}

		private void TradesClick(object sender, RoutedEventArgs e)
		{
			TryInitialize();

			var window = _tradesWindows.SafeAdd(SelectedSecurity, security =>
			{
				// создаем окно со сделками
				var wnd = new TradesWindow { Title = security.Code + " сделки" };

				// начинаем получать сделки
				MainWindow.Instance.Trader.RegisterTrades(security);

				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			TryInitialize();

			var trader = MainWindow.Instance.Trader;

			var window = _quotesWindows.SafeAdd(SelectedSecurity, security =>
			{
				// начинаем получать котировки стакана
				trader.RegisterMarketDepth(security);

				// создаем окно со стаканом
				var wnd = new QuotesWindow { Title = security.Id + " стакан" };
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void TryInitialize()
		{
			if (!_initialized)
			{
				_initialized = true;

				var trader = MainWindow.Instance.Trader;

				trader.NewTrades += TraderOnNewTrades;
				trader.MarketDepthsChanged += TraderOnMarketDepthsChanged;

				TraderOnMarketDepthsChanged(new[] { trader.GetMarketDepth(SecurityPicker.SelectedSecurity) });
			}
		}

		private void TraderOnNewTrades(IEnumerable<Trade> trades)
		{
			foreach (var group in trades.GroupBy(t => t.Security))
			{
				var wnd = _tradesWindows.TryGetValue(group.Key);

				if (wnd != null)
					wnd.TradeGrid.Trades.AddRange(group);
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
	}
}