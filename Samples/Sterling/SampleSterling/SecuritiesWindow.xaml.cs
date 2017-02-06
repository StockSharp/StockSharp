#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleSterling.SampleSterlingPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleSterling
{
	using System.Windows;
	
	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private readonly SynchronizedDictionary<Security, TradesWindow> _tradesWindows = new SynchronizedDictionary<Security, TradesWindow>();
		private bool _initialized;

		public Security SelectedSecurity => SecurityPicker.SelectedSecurity;

		public SecuritiesWindow()
		{
			InitializeComponent();
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
				},
				SecurityProvider = MainWindow.Instance.Trader,
				MarketDataProvider = MainWindow.Instance.Trader,
				Portfolios = new PortfolioDataSource(MainWindow.Instance.Trader),
				Adapter = MainWindow.Instance.Trader.TransactionAdapter
			};

			if (newOrder.ShowModal(this))
				MainWindow.Instance.Trader.RegisterOrder(newOrder.Order);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			NewOrder.IsEnabled = NewStopOrder.IsEnabled = Trades.IsEnabled = Depth.IsEnabled = security != null;
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			var wnd = new SecurityLookupWindow { Criteria = new Security { Code = "AAPL" } };

			if (!wnd.ShowModal(this))
				return;

			MainWindow.Instance.Trader.LookupSecurities(wnd.Criteria);
		}

		private void TradesClick(object sender, RoutedEventArgs e)
		{
			TryInitialize();

			var window = _tradesWindows.SafeAdd(SelectedSecurity, security =>
			{
				// create tick trades window
				var wnd = new TradesWindow { Title = security.Code + " сделки" };

				// subscribe on tick trades flow
				MainWindow.Instance.Trader.RegisterTrades(security);

				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			TryInitialize();

			var trader = MainWindow.Instance.Trader;

			var window = _quotesWindows.SafeAdd(SelectedSecurity, security =>
			{
				// subscribe on order book flow
				trader.RegisterMarketDepth(security);

				// create order book window
				var wnd = new QuotesWindow { Title = security.Id + " " + LocalizedStrings.MarketDepth };
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void TryInitialize()
		{
			if (!_initialized)
			{
				_initialized = true;

				var trader = MainWindow.Instance.Trader;

				trader.NewTrade += TraderOnNewTrade;
				trader.MarketDepthChanged += TraderOnMarketDepthChanged;

				TraderOnMarketDepthChanged(trader.GetMarketDepth(SecurityPicker.SelectedSecurity));
			}
		}

		private void TraderOnNewTrade(Trade trade)
		{
			var wnd = _tradesWindows.TryGetValue(trade.Security);

			if (wnd != null)
				wnd.TradeGrid.Trades.Add(trade);
		}

		private void TraderOnMarketDepthChanged(MarketDepth depth)
		{
			var wnd = _quotesWindows.TryGetValue(depth.Security);

			if (wnd != null)
				wnd.DepthCtrl.UpdateDepth(depth);
		}
	}
}