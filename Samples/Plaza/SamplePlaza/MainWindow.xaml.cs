#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SamplePlaza.SamplePlazaPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SamplePlaza
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Plaza;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly PlazaTrader Trader = new PlazaTrader();
		private bool _initialized;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly OrdersLogWindow _ordersLogWindow = new OrdersLogWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Plaza II");

			_ordersWindow.MakeHideable();
			_ordersLogWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			AppName.Text = Trader.AppName;

			Tables.SelectedTables = Trader.Tables.Select(t => t.Id);

			_logManager.Sources.Add(Trader);
			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "StockSharp_Plaza" });
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_ordersLogWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_ordersLogWindow.Close();
			_portfoliosWindow.Close();

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
					Trader.Address = Address.Text.To<EndPoint>();
					Trader.IsCGate = IsCGate.IsChecked == true;
					Trader.IsDemo = IsDemo.IsChecked == true;
					Trader.AppName = AppName.Text;
					Trader.TableRegistry.StreamRegistry.IsFastRepl = IsFastRepl.IsChecked == true;

					if (IsAutorization.IsChecked == true)
					{
						Trader.Login = Login.Text;
						Trader.Password = Password.Password;
					}
					else
					{
						Trader.Login = string.Empty;
						Trader.Password = string.Empty;
					}

					if (!_initialized)
					{
						_initialized = true;

						var revisionManager = Trader.StreamManager.RevisionManager;

						//revisionManager.Tables.Add(Trader.TableRegistry.IndexLog);
						revisionManager.Tables.Add(Trader.TableRegistry.TradeFuture);
						revisionManager.Tables.Add(Trader.TableRegistry.TradeOption);

						Trader.Tables.Clear();
						Trader.TableRegistry.SyncTables(Tables.SelectedTables);

						if (Trader.Tables.Contains(Trader.TableRegistry.AnonymousOrdersLog))
						{
							Trader.CreateDepthFromOrdersLog = true;
						}

						Trader.ReConnectionSettings.AttemptCount = -1;
						Trader.Restored += () => this.GuiAsync(() => MessageBox.Show(this, LocalizedStrings.Str2958));

						// подписываемся на событие успешного соединения
						Trader.Connected += () =>
						{
							this.GuiAsync(() => ChangeConnectStatus(true));
						};

						// подписываемся на событие разрыва соединения
						Trader.ConnectionError += error => this.GuiAsync(() =>
						{
							ChangeConnectStatus(false);
							MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
						});

						// подписываемся на событие успешного отключения
						Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

						// подписываемся на ошибку обработки данных (транзакций и маркет)
						//Trader.Error += error =>
						//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), "Ошибка обработки данных"));

						// подписываемся на ошибку подписки маркет-данных
						Trader.MarketDataSubscriptionFailed += (security, msg, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

						Trader.NewSecurity += security => _securitiesWindow.SecurityPicker.Securities.Add(security);
						Trader.NewTrade += trade => _tradesWindow.TradeGrid.Trades.Add(trade);
						Trader.NewOrder += order => _ordersWindow.OrderGrid.Orders.Add(order);
						Trader.NewMyTrade += trade => _myTradesWindow.TradeGrid.Trades.Add(trade);
						Trader.NewOrderLogItem += item => _ordersLogWindow.AddOperation(item);

						Trader.NewPortfolio += portfolio => _portfoliosWindow.PortfolioGrid.Portfolios.Add(portfolio);
						Trader.NewPosition += position => _portfoliosWindow.PortfolioGrid.Positions.Add(position);

						// подписываемся на событие о неудачной регистрации заявок
						Trader.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
						// подписываемся на событие о неудачном снятии заявок
						Trader.OrderCancelFailed += OrderFailed;

						Trader.MassOrderCancelFailed += (transId, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

						// устанавливаем поставщик маркет-данных
						_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;
					}

					Trader.Connect();
				}
				else
				{
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
            
            ShowSecurities.IsEnabled = ShowTrades.IsEnabled =
            ShowMyTrades.IsEnabled = ShowOrders.IsEnabled =
            ShowPortfolios.IsEnabled = isConnected;

			ShowOrdersLog.IsEnabled = isConnected;

			IsCGate.IsEnabled = IsFastRepl.IsEnabled = IsAutorization.IsEnabled = Tables.IsEnabled = !isConnected;
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

		private void ShowOrdersLogClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersLogWindow);
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
