#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleMicex.SampleMicexPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleMicex
{
	using System;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			if (_initialized)
			{
				MainWindow.Instance.Trader.MarketDepthChanged -= TraderOnMarketDepthChanged;
			}

			var trader = MainWindow.Instance.Trader;
			if (trader != null)
			{
				if (_initialized)
					trader.MarketDepthChanged -= TraderOnMarketDepthChanged;

				_quotesWindows.SyncDo(d =>
				{
					foreach (var pair in d)
					{
						trader.UnRegisterMarketDepth(pair.Key);

						pair.Value.DeleteHideable();
						pair.Value.Close();
					}
				});

				//trader.RegisteredSecurities.ForEach(trader.UnRegisterSecurity);
				//trader.RegisteredTrades.ForEach(trader.UnRegisterTrades);
			}

			base.OnClosed(e);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			NewOrder.IsEnabled = Quotes.IsEnabled = security != null;
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

		private void QuotesClick(object sender, RoutedEventArgs e)
		{
			var trader = MainWindow.Instance.Trader;

			var window = _quotesWindows.SafeAdd(SecurityPicker.SelectedSecurity, security =>
			{
				// начинаем получать котировки стакана
				trader.RegisterMarketDepth(security);

				// создаем окно со стаканом
				var wnd = new QuotesWindow
				{
					Title = security.Id + " " + LocalizedStrings.MarketDepth,
					DepthCtrl =
					{
						MaxDepth = MainWindow.Instance.Depth.Text.To<int>()
					}
				};
				wnd.MakeHideable();
				return wnd;
			});

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();

			if (!_initialized)
			{
				TraderOnMarketDepthChanged(trader.GetMarketDepth(SecurityPicker.SelectedSecurity));
				trader.MarketDepthChanged += TraderOnMarketDepthChanged;
				_initialized = true;
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