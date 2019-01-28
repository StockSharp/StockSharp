namespace SampleCoinCap
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using StockSharp.Algo.Candles;
	using StockSharp.CoinCap;
	using StockSharp.BusinessEntities;

	public partial class SecuritiesWindow
	{
		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = CoinCapMessageAdapter.AllTimeFrames;
			CandlesPeriods.SelectedIndex = 0;
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Quotes.IsEnabled = security != null;

			TryEnableCandles();
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