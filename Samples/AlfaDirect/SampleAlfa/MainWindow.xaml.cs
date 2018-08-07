#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleAlfa.SampleAlfaPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleAlfa
{
	using System;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.AlfaDirect;
	using StockSharp.Logging;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public AlfaTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("AlfaDirect");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_newsWindow.MakeHideable();

			_logManager.Listeners.Add(new FileLogListener());
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
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
			_stopOrdersWindow.Close();
			_newsWindow.Close();

			if (Trader != null)
			{
				_logManager.Sources.Remove(Trader);
				Trader.Dispose();
			}

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			try
			{
				if (!_isConnected)
				{
					if (Trader == null)
					{
						// создаем подключение
						Trader = new AlfaTrader { LogLevel = LogLevels.Debug };

						_logManager.Sources.Add(Trader);

						Trader.Restored += () => this.GuiAsync(() =>
						{
							// разблокируем кнопку Экспорт (соединение было восстановлено)
							ChangeConnectStatus(true);
							MessageBox.Show(this, LocalizedStrings.Str2958);
						});

						// подписываемся на событие успешного соединения
						Trader.Connected += () =>
						{
							this.GuiAsync(() => ChangeConnectStatus(true));

							// запускаем подписку на новости
							Trader.RegisterNews();
						};

						// подписываемся на событие успешного отключения
						Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

						// подписываемся на событие разрыва соединения
						Trader.ConnectionError += error => this.GuiAsync(() =>
						{
							// заблокируем кнопку Экспорт (так как соединение было потеряно)
							ChangeConnectStatus(false);

							MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
						});

						// подписываемся на ошибку обработки данных (транзакций и маркет)
						Trader.Error += error =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

						// подписываемся на ошибку подписки маркет-данных
						Trader.MarketDataSubscriptionFailed += (security, msg, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

						Trader.NewSecurity += _securitiesWindow.SecurityPicker.Securities.Add;
						Trader.NewTrade += _tradesWindow.TradeGrid.Trades.Add;
						Trader.NewMyTrade += _myTradesWindow.TradeGrid.Trades.Add;
						Trader.NewOrder += _ordersWindow.OrderGrid.Orders.Add;
						Trader.NewStopOrder += _stopOrdersWindow.OrderGrid.Orders.Add;
						Trader.NewPortfolio += _portfoliosWindow.PortfolioGrid.Portfolios.Add;
						Trader.NewPosition += _portfoliosWindow.PortfolioGrid.Positions.Add;

						// подписываемся на событие о неудачной регистрации заявок
						Trader.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
						// подписываемся на событие о неудачном снятии заявок
						Trader.OrderCancelFailed += OrderFailed;

						// подписываемся на событие о неудачной регистрации стоп-заявок
						Trader.StopOrderRegisterFailed += _stopOrdersWindow.OrderGrid.AddRegistrationFail;
						// подписываемся на событие о неудачном снятии стоп-заявок
						Trader.StopOrderCancelFailed += OrderFailed;

						Trader.MassOrderCancelFailed += (transId, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

						// устанавливаем поставщик маркет-данных
						_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

						// set news provider
						_newsWindow.NewsPanel.NewsProvider = Trader;

						ShowSecurities.IsEnabled = ShowNews.IsEnabled =
						ShowMyTrades.IsEnabled = ShowOrders.IsEnabled = ShowStopOrders.IsEnabled =
						ShowPortfolios.IsEnabled = ShowTrades.IsEnabled = true;

						Trader.NewNews += news => _newsWindow.NewsPanel.NewsGrid.News.Add(news);
					}

					Trader.Login = TextBoxLogin.Text;
					Trader.Password = PasswordBox.Password;
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
			ConnectionStatus.Background = new SolidColorBrush(isConnected ? Colors.LightGreen : Colors.LightPink);
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

		void ShowStopOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_stopOrdersWindow);
		}

		private void ShowNewsClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_newsWindow);
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