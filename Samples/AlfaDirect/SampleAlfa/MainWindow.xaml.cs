namespace SampleAlfa
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Media;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

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
			MainWindow.Instance = this;

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
						Trader.MarketDataSubscriptionFailed += (security, type, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

						Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
						Trader.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);
						Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
						Trader.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
						Trader.NewStopOrders += orders => _stopOrdersWindow.OrderGrid.Orders.AddRange(orders);
						Trader.NewPortfolios += portfolios =>
						{
							portfolios.ForEach(Trader.RegisterPortfolio);
							_portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
						};
						Trader.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);

						// подписываемся на событие о неудачной регистрации заявок
						Trader.OrdersRegisterFailed += OrdersFailed;
						// подписываемся на событие о неудачном снятии заявок
						Trader.OrdersCancelFailed += OrdersFailed;

						// подписываемся на событие о неудачной регистрации стоп-заявок
						Trader.StopOrdersRegisterFailed += OrdersFailed;
						// подписываемся на событие о неудачном снятии стоп-заявок
						Trader.StopOrdersCancelFailed += OrdersFailed;

						// устанавливаем поставщик маркет-данных
						_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

						// set news provider
						_newsWindow.NewsPanel.NewsProvider = Trader;

						ShowSecurities.IsEnabled = ShowNews.IsEnabled =
						ShowMyTrades.IsEnabled = ShowOrders.IsEnabled = ShowStopOrders.IsEnabled =
						ShowPortfolios.IsEnabled = true;

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

		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach (var fail in fails)
					MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str2960);
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
				throw new ArgumentNullException("window");

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}
	}
}