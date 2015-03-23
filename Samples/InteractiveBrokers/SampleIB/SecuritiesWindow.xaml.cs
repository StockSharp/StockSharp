namespace SampleIB
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.InteractiveBrokers;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private readonly SynchronizedDictionary<CandleSeries, CandlesWindow> _сandles = new SynchronizedDictionary<CandleSeries, CandlesWindow>();
		private readonly SynchronizedSet<Security> _reportSecurities = new SynchronizedSet<Security>();

		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = IBTimeFrames.AllTimeFrames;
			CandlesPeriods.SelectedItem = IBTimeFrames.Hour;
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

		private Security SelectedSecurity
		{
			get { return SecurityPicker.SelectedSecurity; }
		}

		private static IBTrader Trader
		{
			get { return MainWindow.Instance.Trader; }
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Level1.IsEnabled = Reports.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled = HistoryCandles.IsEnabled = RealTimeCandles.IsEnabled = security != null;

			if (security == null)
				return;

			Level1.IsChecked = Trader.RegisteredSecurities.Contains(SelectedSecurity);
			Reports.IsChecked = _reportSecurities.Contains(SelectedSecurity);
			RealTimeCandles.IsChecked = _сandles.Keys.Any(s => s.Security == SelectedSecurity);
			Depth.IsChecked = _quotesWindows.ContainsKey(SelectedSecurity);
		}

		private void Level1Click(object sender, RoutedEventArgs e)
		{
			var security = SecurityPicker.SelectedSecurity;
			var trader = Trader;

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

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			if (Depth.IsChecked == true)
			{
				// создаем окно со стаканом
				var wnd = new QuotesWindow { Title = SelectedSecurity.Id + " " + LocalizedStrings.MarketDepth };
				_quotesWindows.Add(SelectedSecurity, wnd);

				// начинаем получать котировки стакана
				Trader.RegisterMarketDepth(SelectedSecurity);

				wnd.Show();
			}
			else
			{
				Trader.UnRegisterMarketDepth(SelectedSecurity);

				var wnd = _quotesWindows[SelectedSecurity];
				_quotesWindows.Remove(SelectedSecurity);
				wnd.Close();
			}
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			new FindSecurityWindow().ShowModal(this);
		}

		public void AddCandles(CandleSeries series, IEnumerable<Candle> candles)
		{
			var wnd = _сandles.TryGetValue(series);
			if (wnd != null)
				candles.ForEach(wnd.ProcessCandles);
		}

		private void HistoryCandlesClick(object sender, RoutedEventArgs e)
		{
			var series = new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Security = SelectedSecurity,
				Arg = CandlesPeriods.SelectedItem,
			};

			var wnd = new CandlesWindow { Title = series.ToString() };
			_сandles.Add(series, wnd);
			Trader.SubscribeCandles(series, DateTime.Today.Subtract(TimeSpan.FromTicks(((TimeSpan)series.Arg).Ticks * 30)), DateTime.Now);
			wnd.Show();
		}

		private void RealTimeCandlesClick(object sender, RoutedEventArgs e)
		{
			var series = new CandleSeries(typeof(TimeFrameCandle), SelectedSecurity, IBTimeFrames.Second5);

			if (RealTimeCandles.IsChecked == true)
			{
				var wnd = new CandlesWindow { Title = SelectedSecurity.Id + LocalizedStrings.Str2973 };
				_сandles.Add(series, wnd);
				Trader.SubscribeCandles(series, DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
				wnd.Show();
			}
			else
			{
				Trader.UnSubscribeCandles(series);
				_сandles.GetAndRemove(series).Close();
			}
		}

		private void ReportsClick(object sender, RoutedEventArgs e)
		{
			var security = SelectedSecurity;

			var isSubscribe = _reportSecurities.TryAdd(security);

			if (!isSubscribe)
				_reportSecurities.Remove(security);

			foreach (var report in Enumerator.GetValues<FundamentalReports>())
				Trader.SubscribeFundamentalReport(security, report, isSubscribe);
		}
	}
}