#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleFix.SampleFixPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleFix
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

			CandlesPeriods.ItemsSource = new[]
			{
				TimeSpan.FromTicks(1),
				TimeSpan.FromMinutes(1),
				TimeSpan.FromMinutes(5),
				//TimeSpan.FromMinutes(30),
				TimeSpan.FromHours(1),
				TimeSpan.FromDays(1),
				TimeSpan.FromDays(7),
				TimeSpan.FromDays(30)
			};
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
			Quotes.IsEnabled = NewStopOrder.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled = OrderLog.IsEnabled = security != null;

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
					Condition = new FixOrderCondition()
				},
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader),
				Adapter = MainWindow.Instance.Trader.TransactionAdapter
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
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

		private void OrderLogClick(object sender, RoutedEventArgs e)
		{
			var trader = MainWindow.Instance.Trader;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				if (trader.RegisteredOrderLogs.Contains(security))
				{
					trader.UnRegisterOrderLog(security);
				}
				else
				{
					trader.RegisterOrderLog(security);
				}
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

		private void CandlesClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var tf = (TimeSpan)CandlesPeriods.SelectedItem;
				var series = new CandleSeries(typeof(TimeFrameCandle), security, tf);

				new ChartWindow(series, tf.Ticks == 1 ? DateTime.Today : DateTime.Now.Subtract(TimeSpan.FromTicks(tf.Ticks * 10000))).Show();
			}
		}
	}
}