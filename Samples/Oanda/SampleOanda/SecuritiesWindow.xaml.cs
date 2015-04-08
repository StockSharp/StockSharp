namespace SampleOanda
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Oanda;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = OandaMessageAdapter.TimeFrames;
			CandlesPeriods.SelectedIndex = 0;
		}

		protected override void OnClosed(EventArgs e)
		{
			var trader = MainWindow.Instance.Trader;
			if (trader != null)
			{
				trader.RegisteredSecurities.ForEach(trader.UnRegisterSecurity);
				trader.RegisteredTrades.ForEach(trader.UnRegisterTrades);
			}

			base.OnClosed(e);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Level1.IsEnabled = NewStopOrder.IsEnabled = NewOrder.IsEnabled = security != null;
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

		private void Level1Click(object sender, RoutedEventArgs e)
		{
			var security = SecurityPicker.SelectedSecurity;
			var trader = MainWindow.Instance.Trader;

			if (trader.RegisteredSecurities.Contains(security))
			{
				trader.UnRegisterSecurity(security);
			}
			else
			{
				trader.RegisterSecurity(security);
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