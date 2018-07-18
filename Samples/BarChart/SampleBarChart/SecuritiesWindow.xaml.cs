#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleBarChart.SampleBarChartPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleBarChart
{
	using System;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private readonly SynchronizedDictionary<Security, HistoryTicksWindow> _historyTicksWindows = new SynchronizedDictionary<Security, HistoryTicksWindow>();
		private readonly SynchronizedDictionary<Security, HistoryCandlesWindow> _historyCandlesWindows = new SynchronizedDictionary<Security, HistoryCandlesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();
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
				{
					trader.MarketDepthChanged -= TraderOnMarketDepthChanged;
				}

				_quotesWindows.ForEach(pair =>
				{
					trader.UnRegisterMarketDepth(pair.Key);
					DeleteHideableAndClose(pair.Value);
				});

				_historyTicksWindows.ForEach(pair => DeleteHideableAndClose(pair.Value));
				_historyCandlesWindows.ForEach(pair => DeleteHideableAndClose(pair.Value));
			}

			base.OnClosed(e);
		}

		private static void DeleteHideableAndClose(Window window)
		{
			window.DeleteHideable();
			window.Close();
		}

		//public Security SelectedSecurity => SecurityPicker.SelectedSecurity;

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			HistoryTicks.IsEnabled = HistoryCandles.IsEnabled = Depth.IsEnabled = security != null;
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
					_initialized = true;
					trader.MarketDepthChanged += TraderOnMarketDepthChanged;
				}
			}
		}

		private void TraderOnMarketDepthChanged(MarketDepth depth)
		{
			var wnd = _quotesWindows.TryGetValue(depth.Security);

			if (wnd != null)
				wnd.DepthCtrl.UpdateDepth(depth);
		}

		private void HistoryTicksClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var window = _historyTicksWindows.SafeAdd(security, s =>
				{
					// create historical ticks window
					var wnd = new HistoryTicksWindow(security);
					wnd.MakeHideable();
					return wnd;
				});

				if (window.Visibility == Visibility.Visible)
					window.Hide();
				else
					window.Show();
			}
		}

		private void HistoryCandlesClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var window = _historyCandlesWindows.SafeAdd(security, s =>
				{
					// create historical candles window
					var wnd = new HistoryCandlesWindow(security);
					wnd.MakeHideable();
					return wnd;
				});

				if (window.Visibility == Visibility.Visible)
					window.Hide();
				else
					window.Show();
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
	}
}