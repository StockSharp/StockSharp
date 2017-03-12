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
		private readonly SynchronizedDictionary<CandleSeries, CandlesWindow> _historyCandles = new SynchronizedDictionary<CandleSeries, CandlesWindow>();
		private readonly SynchronizedDictionary<CandleSeries, CandlesWindow> _realTimeCandles = new SynchronizedDictionary<CandleSeries, CandlesWindow>();
		private readonly Dictionary<Security, long[]> _reportSecurities = new Dictionary<Security, long[]>();
		private readonly Dictionary<Security, long> _optionSecurities = new Dictionary<Security, long>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = InteractiveBrokersTimeFrames.AllTimeFrames;
			CandlesPeriods.SelectedItem = InteractiveBrokersTimeFrames.Hour;
		}

		protected override void OnClosed(EventArgs e)
		{
			_quotesWindows.SyncDo(d => d.Values.ForEach(w =>
			{
				w.DeleteHideable();
				w.Close();
			}));

			if (Trader != null)
			{
				if (_initialized)
					Trader.MarketDepthChanged -= TraderOnMarketDepthChanged;

				_reportSecurities.SelectMany(p => p.Value).ForEach(Trader.UnSubscribeFundamentalReport);
				_optionSecurities.ForEach(p => Trader.UnSubscribeOptionCalc(p.Value));

				_reportSecurities.Clear();
				_optionSecurities.Clear();
			}

			base.OnClosed(e);
		}

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
				SecurityProvider = Trader,
				MarketDataProvider = Trader,
				Portfolios = new PortfolioDataSource(Trader),
			};

			if (newOrder.ShowModal(this))
				Trader.RegisterOrder(newOrder.Order);
		}

		private Security SelectedSecurity => SecurityPicker.SelectedSecurity;

		private static InteractiveBrokersTrader Trader => MainWindow.Instance.Trader;

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Level1.IsEnabled = Reports.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled = HistoryCandles.IsEnabled = RealTimeCandles.IsEnabled = security != null;

			if (security == null)
				return;

			Level1.IsChecked = Trader.RegisteredSecurities.Contains(SelectedSecurity);
			Reports.IsChecked = _reportSecurities.ContainsKey(SelectedSecurity);
			Options.IsChecked = _optionSecurities.ContainsKey(SelectedSecurity);
			RealTimeCandles.IsChecked = _realTimeCandles.Keys.Any(s => s.Security == SelectedSecurity);
			Depth.IsChecked = _quotesWindows.ContainsKey(SelectedSecurity);
		}

		private void Level1Click(object sender, RoutedEventArgs e)
		{
			var security = SecurityPicker.SelectedSecurity;
			var trader = Trader;

			if (trader.RegisteredSecurities.Contains(security))
			{
				trader.UnRegisterSecurity(security);
				//trader.UnRegisterTrades(security);
			}
			else
			{
				trader.RegisterSecurity(security);
				//trader.RegisterTrades(security);
			}
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			var wnd = _quotesWindows.TryGetValue(SelectedSecurity);

			if (wnd == null)
			{
				// create order book window
				wnd = new QuotesWindow { Title = SelectedSecurity.Id + " " + LocalizedStrings.MarketDepth };
				_quotesWindows.Add(SelectedSecurity, wnd);

				// subscribe on order book flow
				Trader.RegisterMarketDepth(SelectedSecurity);

				wnd.Show();
			}
			else
			{
				Trader.UnRegisterMarketDepth(SelectedSecurity);

				_quotesWindows.Remove(SelectedSecurity);

				wnd.Close();
			}

			if (!_initialized)
			{
				TraderOnMarketDepthChanged(Trader.GetMarketDepth(SecurityPicker.SelectedSecurity));
				Trader.MarketDepthChanged += TraderOnMarketDepthChanged;
				_initialized = true;
			}
		}

		private void TraderOnMarketDepthChanged(MarketDepth depth)
		{
			var wnd = _quotesWindows.TryGetValue(depth.Security);

			if (wnd != null)
				wnd.DepthCtrl.UpdateDepth(depth);
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			var wnd = new SecurityLookupWindow { Criteria = new Security { Code = "AAPL", Type = SecurityTypes.Stock } };

			if (!wnd.ShowModal(this))
				return;

			Trader.LookupSecurities(wnd.Criteria);
		}

		public void AddCandles(CandleSeries series, IEnumerable<Candle> candles)
		{
			var wnd = _realTimeCandles.TryGetValue(series) ?? _historyCandles.TryGetValue(series);

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
			_historyCandles.Add(series, wnd);
			Trader.SubscribeCandles(series, DateTime.Today.Subtract(TimeSpan.FromTicks(((TimeSpan)series.Arg).Ticks * 30)), DateTime.Now);
			wnd.Show();
		}

		private void RealTimeCandlesClick(object sender, RoutedEventArgs e)
		{
			var series = new CandleSeries(typeof(TimeFrameCandle), SelectedSecurity, InteractiveBrokersTimeFrames.Second5);

			if (RealTimeCandles.IsChecked == true)
			{
				Trader.UnSubscribeCandles(series);
				_realTimeCandles.GetAndRemove(series).Close();

				RealTimeCandles.IsChecked = false;
			}
			else
			{
				var wnd = new CandlesWindow { Title = SelectedSecurity.Id + LocalizedStrings.Str2973 };
				_realTimeCandles.Add(series, wnd);
				Trader.SubscribeCandles(series, DateTimeOffset.MinValue, DateTimeOffset.MaxValue);
				wnd.Show();

				RealTimeCandles.IsChecked = true;
			}
		}

		private void ReportsClick(object sender, RoutedEventArgs e)
		{
			var security = SelectedSecurity;

			var ids = _reportSecurities.TryGetValue(security);

			if (ids == null)
			{
				ids = Enumerator.GetValues<FundamentalReports>()
					.Select(report => Trader.SubscribeFundamentalReport(security, report))
					.ToArray();

				_reportSecurities.Add(security, ids);
				Reports.IsChecked = true;
			}
			else
			{
				_reportSecurities.Remove(security);

				ids.ForEach(Trader.UnSubscribeFundamentalReport);

				Reports.IsChecked = false;
			}
		}

		private void OptionsClick(object sender, RoutedEventArgs e)
		{
			var security = SelectedSecurity;

			var id = _optionSecurities.TryGetValue2(security);

			if (id == null)
			{
				var wnd = new OptionWindow();

				if (!wnd.ShowModal(this))
					return;

				Trader.SubscribeOptionCalc(security, wnd.Volatility, wnd.OptionPrice, wnd.AssetPrice);
				Options.IsChecked = true;
			}
			else
			{
				Trader.UnSubscribeOptionCalc(id.Value);
				_optionSecurities.Remove(security);
				Options.IsChecked = false;
			}
		}
	}
}