namespace SampleTradier
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Xaml;

	using StockSharp.Algo.Candles;
	using StockSharp.Tradier;
	using StockSharp.BusinessEntities;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = TradierMessageAdapter.AllTimeFrames;
			CandlesPeriods.SelectedIndex = 1;
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Quotes.IsEnabled = NewOrder.IsEnabled = security != null;

			TryEnableCandles();
		}

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
			}.Init(MainWindow.Instance.Trader);

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

		private void FindClick(object sender, RoutedEventArgs e)
		{
			var wnd = new SecurityLookupWindow
			{
				ShowAllOption = MainWindow.Instance.Trader.MarketDataAdapter.IsSupportSecuritiesLookupAll,
				Criteria = new Security { Code = "AAPL" }
			};

			if (!wnd.ShowModal(this))
				return;

			MainWindow.Instance.Trader.LookupSecurities(wnd.Criteria);
		}

		private void CandlesPeriods_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			TryEnableCandles();
		}

		private void TryEnableCandles()
		{
			Candles.IsEnabled = CandlesPeriods.SelectedItem != null && SecurityPicker.SelectedSecurity != null;
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
	}
}