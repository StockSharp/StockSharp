#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleCQG.SampleCQGPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleCQG
{
	using System;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using MoreLinq;

	using Ecng.Collections;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public partial class SecuritiesWindow
	{
		private readonly SynchronizedDictionary<Security, QuotesWindow> _quotesWindows = new SynchronizedDictionary<Security, QuotesWindow>();
		private bool _initialized;

		public SecuritiesWindow()
		{
			InitializeComponent();
		}

		private void SecuritiesWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			CandlesPeriods.ItemsSource = Connector.Adapter.TimeFrames;
		}

		protected override void OnClosed(EventArgs e)
		{
			_quotesWindows.SyncDo(d => d.Values.ForEach(w =>
			{
				w.DeleteHideable();
				w.Close();
			}));

			if (Connector != null)
			{
				if (_initialized)
					Connector.MarketDepthChanged -= OnMarketDepthChanged;
			}

			base.OnClosed(e);
		}

		private static Connector Connector => MainWindow.Instance.Connector;

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
				SecurityProvider = Connector,
				MarketDataProvider = Connector,
				Portfolios = new PortfolioDataSource(Connector)
			};

			if (newOrder.ShowModal(this))
				Connector.RegisterOrder(newOrder.Order);
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
				SecurityProvider = Connector,
				MarketDataProvider = Connector,
				Portfolios = new PortfolioDataSource(Connector),
				Adapter = Connector.TransactionAdapter
			};

			if (newOrder.ShowModal(this))
				Connector.RegisterOrder(newOrder.Order);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Quotes.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled = NewStopOrder.IsEnabled = security != null;
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			var wnd = new SecurityLookupWindow
			{
				ShowAllOption = Connector.MarketDataAdapter.IsSupportSecuritiesLookupAll,
				Criteria = new Security { Code = "ES" }
			};

			if (!wnd.ShowModal(this))
				return;

			Connector.LookupSecurities(wnd.Criteria);
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var window = _quotesWindows.SafeAdd(security, key =>
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
				{
					// unsubscribe from order book flow
					Connector.UnRegisterMarketDepth(security);

					window.Hide();
				}
				else
				{
					// subscribe on order book flow
					Connector.RegisterMarketDepth(security);

					window.Show();

					window.DepthCtrl.UpdateDepth(Connector.GetMarketDepth(security));
				}

				if (!_initialized)
				{
					Connector.MarketDepthChanged += OnMarketDepthChanged;
					_initialized = true;
				}
			}
		}

		private void OnMarketDepthChanged(MarketDepth depth)
		{
			var wnd = _quotesWindows.TryGetValue(depth.Security);

			wnd?.DepthCtrl.UpdateDepth(depth);
		}

		private void QuotesClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				if (Connector.RegisteredSecurities.Contains(security))
				{
					Connector.UnRegisterSecurity(security);
					Connector.UnRegisterTrades(security);
				}
				else
				{
					Connector.RegisterSecurity(security);
					Connector.RegisterTrades(security);
				}
			}
		}

		private void CandlesClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var series = new CandleSeries(typeof(TimeFrameCandle), security, (TimeSpan)CandlesPeriods.SelectedItem);

				new ChartWindow(series).Show();
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
	}
}