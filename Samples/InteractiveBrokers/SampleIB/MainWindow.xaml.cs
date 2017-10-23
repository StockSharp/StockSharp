#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleIB.SampleIBPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleIB
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.InteractiveBrokers;
	using StockSharp.Logging;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly InteractiveBrokersTrader Trader = new InteractiveBrokersTrader();
		private bool _initialized;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();
		private readonly ScannerWindow _scannerWindow;

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Interactive Brokers");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_newsWindow.MakeHideable();

			_scannerWindow = new ScannerWindow();
			_scannerWindow.MakeHideable();

			Trader.LogLevel = LogLevels.Debug;
			_logManager.Sources.Add(Trader);
			_logManager.Listeners.Add(new FileLogListener("logs.txt"));

			Tws.IsChecked = true;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_stopOrdersWindow.DeleteHideable();
			_newsWindow.DeleteHideable();
			_scannerWindow.DeleteHideable();

			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
			_stopOrdersWindow.Close();
			_newsWindow.Close();

			if (Trader != null)
				Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isConnected)
				{
					if (!_initialized)
					{
						_initialized = true;

						// subscribe on connection successfully event
						Trader.Connected += () =>
						{
							this.GuiAsync(() => ChangeConnectStatus(true));

							// subscribe on news
							Trader.RegisterNews();
						};

						// subscribe on connection error event
						Trader.ConnectionError += error => this.GuiAsync(() =>
						{
							ChangeConnectStatus(false);
							MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
						});

						Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

						// subscribe on error event
						Trader.Error += error =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

						// subscribe on error of market data subscription event
						Trader.MarketDataSubscriptionFailed += (security, msg, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

						Trader.NewSecurity += _securitiesWindow.SecurityPicker.Securities.Add;
						Trader.NewMyTrade += _myTradesWindow.TradeGrid.Trades.Add;
						Trader.NewTrade += _tradesWindow.TradeGrid.Trades.Add;
						Trader.NewOrder += _ordersWindow.OrderGrid.Orders.Add;
						Trader.NewStopOrder += _stopOrdersWindow.OrderGrid.Orders.Add;
						Trader.CandleSeriesProcessing += _securitiesWindow.AddCandle;

						Trader.NewPortfolio += _portfoliosWindow.PortfolioGrid.Portfolios.Add;
						Trader.NewPosition += _portfoliosWindow.PortfolioGrid.Positions.Add;

						// subscribe on error of order registration event
						Trader.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
						// subscribe on error of order cancelling event
						Trader.OrderCancelFailed += OrderFailed;

						Trader.MassOrderCancelFailed += (transId, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

						Trader.NewNews += news => _newsWindow.NewsPanel.NewsGrid.News.Add(news);

						// set market data provider
						_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

						// set news provider
						_newsWindow.NewsPanel.NewsProvider = Trader;
					}

					Trader.Address = Address.Text.To<EndPoint>();
					Trader.Connect();
				}
				else
				{
					Trader.UnRegisterNews();

					Trader.Disconnect();
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.Message, LocalizedStrings.Str152);
			}
		}

		private void OrderFailed(OrderFail fail)
		{
			this.GuiAsync(() =>
			{
				MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str153);
			});
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
			ConnectionStatus.Content = isConnected ? LocalizedStrings.Connected : LocalizedStrings.Disconnected;

			ShowSecurities.IsEnabled = ShowTrades.IsEnabled = ShowNews.IsEnabled =
            ShowMyTrades.IsEnabled = ShowOrders.IsEnabled = ShowConditionOrders.IsEnabled =
            ShowPortfolios.IsEnabled = ShowScanner.IsEnabled = isConnected;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
		}

		private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_portfoliosWindow);
		}

		private void ShowConditionOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_stopOrdersWindow);
		}

		private void ShowNewsClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_newsWindow);
		}

		private void ShowScannerClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_scannerWindow);
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		private void OnAddrTypeChecked(object sender, RoutedEventArgs e)
		{
			Address.Text = (Tws.IsChecked == true
				? InteractiveBrokersMessageAdapter.DefaultAddress
				: InteractiveBrokersMessageAdapter.DefaultGatewayAddress).To<string>();
		}
	}
}
