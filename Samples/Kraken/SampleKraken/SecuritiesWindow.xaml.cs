namespace SampleKraken
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Kraken;
	using StockSharp.Xaml;
	using StockSharp.Localization;
	using StockSharp.Messages;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = KrakenMessageAdapter.AllTimeFrames;
			CandlesPeriods.SelectedIndex = 1;
		}

		protected override void OnClosed(EventArgs e)
		{
			_quotesWindows.SyncDo(d => d.Values.ForEach(w =>
			{
				w.DeleteHideable();
				w.Close();
			}));

			var trader = MainWindow.Instance.Trader;
			if (trader != null)
			{
				if (_initialized)
					trader.MarketDepthChanged -= TraderOnMarketDepthChanged;
			}

			base.OnClosed(e);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Quotes.IsEnabled = NewOrder.IsEnabled = NewStopOrder.IsEnabled = Depth.IsEnabled = security != null;

			TryEnableCandles();
		}

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader),
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
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader),
				Adapter = MainWindow.Instance.Trader.TransactionAdapter
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void CandlesClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var tf = (TimeSpan)CandlesPeriods.SelectedItem;
				var series = new CandleSeries(typeof(TimeFrameCandle), security, tf);

				new ChartWindow(series, tf.Ticks == 1 ? DateTime.Today : DateTime.Now.Subtract(TimeSpan.FromTicks(tf.Ticks * 100))).Show();
			}
		}

		private void CandlesPeriods_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableCandles();
		}

		private void TryEnableCandles()
		{
			Candles.IsEnabled = CandlesPeriods.SelectedItem != null && SecurityPicker.SelectedSecurity != null;
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			var trader = MainWindow.Instance.Trader;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var window = _quotesWindows.SafeAdd(security, s =>
				{
					// subscribe on order book flow
					trader.RegisterMarketDepth(security);

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
					window.DepthCtrl.UpdateDepth(trader.GetMarketDepth(security));
				}

				if (!_initialized)
				{
					trader.MarketDepthChanged += TraderOnMarketDepthChanged;
					_initialized = true;
				}
			}
		}

		private void QuotesClick(object sender, RoutedEventArgs e)
		{
			var trader = MainWindow.Instance.Trader;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
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
		}

		private void TraderOnMarketDepthChanged(MarketDepth depth)
		{
			var wnd = _quotesWindows.TryGetValue(depth.Security);

			if (wnd != null)
				wnd.DepthCtrl.UpdateDepth(depth);
		}
	}
}