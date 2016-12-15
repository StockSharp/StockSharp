#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIB.SampleIBPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	using StockSharp.Messages;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private readonly SynchronizedDictionary<CandleSeries, CandlesWindow> _сandles = new SynchronizedDictionary<CandleSeries, CandlesWindow>();
		private readonly SynchronizedSet<Security> _reportSecurities = new SynchronizedSet<Security>();

		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = InteractiveBrokersTimeFrames.AllTimeFrames;
			CandlesPeriods.SelectedItem = InteractiveBrokersTimeFrames.Hour;
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

		private Security SelectedSecurity => SecurityPicker.SelectedSecurity;

		private static InteractiveBrokersTrader Trader => MainWindow.Instance.Trader;

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
				// create order book window
				var wnd = new QuotesWindow { Title = SelectedSecurity.Id + " " + LocalizedStrings.MarketDepth };
				_quotesWindows.Add(SelectedSecurity, wnd);

				// subscribe on order book flow
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
			var wnd = new SecurityLookupWindow { Criteria = new Security { Code = "AAPL", Type = SecurityTypes.Stock } };

			if (!wnd.ShowModal(this))
				return;

			MainWindow.Instance.Trader.LookupSecurities(wnd.Criteria);
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
			var series = new CandleSeries(typeof(TimeFrameCandle), SelectedSecurity, InteractiveBrokersTimeFrames.Second5);

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