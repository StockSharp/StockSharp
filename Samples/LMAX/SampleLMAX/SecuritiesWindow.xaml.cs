namespace SampleLMAX
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	
	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.LMAX;
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

			CandlesPeriods.ItemsSource = LmaxSessionHolder.TimeFrames;
			CandlesPeriods.SelectedIndex = 1;
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
			}

			base.OnClosed(e);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Level1.IsEnabled = NewStopOrder.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled = security != null;
			TryEnableCandles();
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

		private void Level1Click(object sender, RoutedEventArgs e)
		{
			var security = SecurityPicker.SelectedSecurity;
			var trader = MainWindow.Instance.Trader;

			if (trader.RegisteredSecurities.Contains(security))
				trader.UnRegisterSecurity(security);
			else
				trader.RegisterSecurity(security);
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

		private void CandlesClick(object sender, RoutedEventArgs e)
		{
			var tf = (TimeSpan)CandlesPeriods.SelectedItem;
			var series = new CandleSeries(typeof(TimeFrameCandle), SecurityPicker.SelectedSecurity, tf);

			new ChartWindow(series, tf.Ticks == 1 ? DateTime.Today : DateTime.Now.Subtract(TimeSpan.FromTicks(tf.Ticks * 100)), DateTime.Now).Show();
		}

		private void CandlesPeriods_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableCandles();
		}

		private void TryEnableCandles()
		{
			Candles.IsEnabled = CandlesPeriods.SelectedItem != null && SecurityPicker.SelectedSecurity != null;
		}
	}
}