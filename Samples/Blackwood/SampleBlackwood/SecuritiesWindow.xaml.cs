#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleBlackwood.SampleBlackwoodPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleBlackwood
{
	using System;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Blackwood;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<string, Level1Window> _level1Windows = new SynchronizedDictionary<string, Level1Window>(StringComparer.InvariantCultureIgnoreCase);
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();

			CandlesPeriods.ItemsSource = BlackwoodMessageAdapter.AllTimeFrames;
			CandlesPeriods.SelectedIndex = 1;
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

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Level1.IsEnabled = NewStopOrder.IsEnabled = NewOrder.IsEnabled = Level2.IsEnabled = Depth.IsEnabled = security != null;

			TryEnableCandles();
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
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader),
				Adapter = MainWindow.Instance.Trader.TransactionAdapter
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void ShowLevel1(Security security)
		{
			var window = _level1Windows.SafeAdd(security.Code, s =>
			{
				// create level1 window
				var wnd = new Level1Window
				{
					Title = security + LocalizedStrings.Str3693
				};
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility != Visibility.Visible)
				window.Show();

			if (!_initialized)
			{
				MainWindow.Instance.Trader.NewMessage += TraderOnNewMessage;
				_initialized = true;
			}
		}

		private void TraderOnNewMessage(Message msg)
		{
			if (msg.Type != MessageTypes.Level1Change)
				return;

			var level1Msg = (Level1ChangeMessage)msg;
			var wnd = _level1Windows.TryGetValue(level1Msg.SecurityId.SecurityCode);

			if (wnd != null)
				wnd.Level1Grid.Messages.Add(level1Msg);
		}

		private void Level2Click(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				ShowLevel1(security);

				// subscribe on order book flow
				MainWindow.Instance.Trader.RegisterMarketDepth(security);
			}
		}

		private void Level1Click(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				ShowLevel1(security);

				var trader = MainWindow.Instance.Trader;

				// subscribe on level1 and tick data flow
				trader.RegisterSecurity(security);
				trader.RegisterTrades(security);

				//if (_bidAskSecurities.Contains(security))
				//{
				//	// unsubscribe from level1 and tick data flow
				//	trader.UnRegisterSecurity(security);
				//	trader.UnRegisterTrades(security);

				//	_bidAskSecurities.Remove(security);
				//}
				//else
				//{
				//	// subscribe on level1 and tick data flow
				//	trader.RegisterSecurity(security);
				//	trader.RegisterTrades(security);

				//	_bidAskSecurities.Add(security);
				//}
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

		private void CandlesClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var tf = (TimeSpan)CandlesPeriods.SelectedItem;
				var series = new CandleSeries(typeof(TimeFrameCandle), security, tf);

				new ChartWindow(series, tf.Ticks == 1 ? DateTime.Today : DateTime.Now.Subtract(TimeSpan.FromTicks(tf.Ticks * 10000))).Show();
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

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			var trader = MainWindow.Instance.Trader;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var window = _quotesWindows.SafeAdd(security, s =>
				{
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

		private void TraderOnMarketDepthChanged(MarketDepth depth)
		{
			var wnd = _quotesWindows.TryGetValue(depth.Security);

			if (wnd != null)
				wnd.DepthCtrl.UpdateDepth(depth);
		}
	}
}