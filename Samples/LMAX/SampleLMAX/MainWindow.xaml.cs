#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleLMAX.SampleLMAXPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleLMAX
{
	using System;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.LMAX;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public LmaxTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put("LMAX");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			Instance = this;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_stopOrdersWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_stopOrdersWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();

			if (Trader != null)
				Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (Login.Text.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2974);
					return;
				}
				else if (Password.Password.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2975);
					return;
				}

				if (Trader == null)
				{
					// create connector
					Trader = new LmaxTrader();

					Trader.Restored += () => this.GuiAsync(() =>
					{
						// update gui labels
						ChangeConnectStatus(true);
						MessageBox.Show(this, LocalizedStrings.Str2958);
					});

					// subscribe on connection successfully event
					Trader.Connected += () =>
					{
						// set flag (connection is established)
						_isConnected = true;

						// update gui labels
						this.GuiAsync(() => ChangeConnectStatus(true));
					};
					Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

					// subscribe on connection error event
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						// update gui labels
						ChangeConnectStatus(false);

						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

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
					
					Trader.NewPortfolio += _portfoliosWindow.PortfolioGrid.Portfolios.Add;
					Trader.NewPosition += _portfoliosWindow.PortfolioGrid.Positions.Add;

					// subscribe on error of order registration event
					Trader.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
					// subscribe on error of order cancelling event
					Trader.OrderCancelFailed += OrderFailed;

					// subscribe on error of stop-order registration event
					Trader.StopOrderRegisterFailed += OrderFailed;
					// subscribe on error of stop-order cancelling event
					Trader.StopOrderCancelFailed += OrderFailed;

					Trader.MassOrderCancelFailed += (transId, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

					ShowSecurities.IsEnabled = ShowTrades.IsEnabled =
					ShowMyTrades.IsEnabled = ShowOrders.IsEnabled = 
					ShowPortfolios.IsEnabled = ShowStopOrders.IsEnabled = true;
				}

				Trader.Login = Login.Text;
				Trader.Password = Password.Password;
				Trader.IsDemo = IsDemo.IsChecked == true;

				// in sandbox security identifies may be different than uploaded on the site
				if (!Trader.IsDemo)
				{
					Trader.SecuritiesLocation = LmaxLocations.London;
				}

				// clear password box for security reason
				//Password.Clear();

				Trader.Connect();
			}
			else
			{
				Trader.Disconnect();
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

		private void ShowStopOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_stopOrdersWindow);
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
	}
}
