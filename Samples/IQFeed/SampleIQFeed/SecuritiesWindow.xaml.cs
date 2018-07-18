#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIQFeed.SampleIQFeedPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleIQFeed
{
	using System;
	using System.Collections.Generic;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private readonly SynchronizedDictionary<Security, Level1Window> _level1Windows = new SynchronizedDictionary<Security, Level1Window>();
		private readonly SynchronizedDictionary<Security, HistoryLevel1Window> _historyLevel1Windows = new SynchronizedDictionary<Security, HistoryLevel1Window>();
		private readonly SynchronizedDictionary<Security, HistoryCandlesWindow> _historyCandlesWindows = new SynchronizedDictionary<Security, HistoryCandlesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			var trader = MainWindow.Instance.Trader;
			if (trader != null)
			{
				if (_initialized)
				{
					trader.ValuesChanged -= TraderOnValuesChanged;
					trader.MarketDepthChanged -= TraderOnMarketDepthChanged;
				}

				_quotesWindows.ForEach(pair =>
				{
					trader.UnRegisterMarketDepth(pair.Key);
					DeleteHideableAndClose(pair.Value);
				});

				_level1Windows.ForEach(pair =>
				{
					trader.UnRegisterSecurity(pair.Key);
					DeleteHideableAndClose(pair.Value);
				});

				_historyLevel1Windows.ForEach(pair => DeleteHideableAndClose(pair.Value));
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
			HistoryLevel1.IsEnabled = HistoryCandles.IsEnabled = Level1.IsEnabled = Depth.IsEnabled = security != null;
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

				TryInitialize();
			}
		}

		private void Level1Click(object sender, RoutedEventArgs e)
		{
			TryInitialize();

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var window = _level1Windows.SafeAdd(security, s =>
				{
					// create level1 window
					var wnd = new Level1Window
					{
						Title = security.Code + " level1"
					};

					// subscribe on level1
					MainWindow.Instance.Trader.RegisterSecurity(security);

					wnd.MakeHideable();
					return wnd;
				});

				if (window.Visibility == Visibility.Visible)
					window.Hide();
				else
					window.Show();
			}
		}

		private void TryInitialize()
		{
			if (!_initialized)
			{
				_initialized = true;

				var trader = MainWindow.Instance.Trader;

				trader.ValuesChanged += TraderOnValuesChanged;
				trader.MarketDepthChanged += TraderOnMarketDepthChanged;
			}
		}

		private void TraderOnValuesChanged(Security security, IEnumerable<KeyValuePair<Level1Fields, object>> changes, DateTimeOffset serverTime, DateTimeOffset localTime)
		{
			var wnd = _level1Windows.TryGetValue(security);

			if (wnd == null)
				return;

			var msg = new Level1ChangeMessage
			{
				SecurityId = security.ToSecurityId(),
				ServerTime = serverTime,
				LocalTime = localTime
			};
			msg.Changes.AddRange(changes);
			wnd.Level1Grid.Messages.Add(msg);
		}

		private void TraderOnMarketDepthChanged(MarketDepth depth)
		{
			var wnd = _quotesWindows.TryGetValue(depth.Security);

			if (wnd != null)
				wnd.DepthCtrl.UpdateDepth(depth);
		}

		private void HistoryLevel1Click(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var window = _historyLevel1Windows.SafeAdd(security, s =>
				{
					// create historical level1 window
					var wnd = new HistoryLevel1Window(security);
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