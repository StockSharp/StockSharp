#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleMultiConnection.SampleMultiConnectionPublic
File: SecuritiesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleMultiConnection
{
	using System;
	using System.Linq;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;

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
			CandlesPeriods.ItemsSource = MainWindow.Instance.Connector.Adapter.TimeFrames;
			CandlesPeriods.SelectedIndex = 0;
		}

		protected override void OnClosed(EventArgs e)
		{
			_quotesWindows.SyncDo(d => d.Values.ForEach(w =>
			{
				w.DeleteHideable();
				w.Close();
			}));

			var connector = MainWindow.Instance.Connector;

			if (connector != null)
			{
				if (_initialized)
					connector.MarketDepthChanged -= TraderOnMarketDepthChanged;
			}

			base.OnClosed(e);
		}

		private void NewOrderClick(object sender, RoutedEventArgs e)
		{
			var connector = MainWindow.Instance.Connector;

			var newOrder = new OrderWindow
			{
				Order = new Order { Security = SecurityPicker.SelectedSecurity },
				SecurityProvider = connector,
				MarketDataProvider = connector,
				Portfolios = new PortfolioDataSource(connector),
			};

			if (newOrder.ShowModal(this))
				connector.RegisterOrder(newOrder.Order);
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			Quotes.IsEnabled = NewOrder.IsEnabled = Depth.IsEnabled = security != null;
		}

		private void DepthClick(object sender, RoutedEventArgs e)
		{
			var connector = MainWindow.Instance.Connector;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var window = _quotesWindows.SafeAdd(security, s =>
				{
					// subscribe on order book flow
					connector.RegisterMarketDepth(security);

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
					window.DepthCtrl.UpdateDepth(connector.GetMarketDepth(security));
				}

				if (!_initialized)
				{
					connector.MarketDepthChanged += TraderOnMarketDepthChanged;
					_initialized = true;
				}
			}
		}

		private void QuotesClick(object sender, RoutedEventArgs e)
		{
			var connector = MainWindow.Instance.Connector;

			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				if (connector.RegisteredSecurities.Contains(security))
					connector.UnRegisterSecurity(security);
				else
					connector.RegisterSecurity(security);
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
				ShowAllOption = MainWindow.Instance.Connector.Adapter.IsSupportSecuritiesLookupAll,
				Criteria = new Security { Code = "IS" }
			};

			if (!wnd.ShowModal(this))
				return;

			MainWindow.Instance.Connector.LookupSecurities(wnd.Criteria);
		}

		private void CandlesClick(object sender, RoutedEventArgs e)
		{
			foreach (var security in SecurityPicker.SelectedSecurities)
			{
				var t = (TimeSpan)CandlesPeriods.SelectedItem;
				var series = new CandleSeries(typeof(TimeFrameCandle), security, t);

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