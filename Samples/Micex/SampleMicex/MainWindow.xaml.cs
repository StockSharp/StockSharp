namespace SampleMicex
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Micex;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly MicexTrader Trader = new MicexTrader();
		private bool _initialized;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Micex (TEAP)");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_newsWindow.MakeHideable();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_newsWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
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
					// создаем подключение
					Trader.Addresses = new[] { Address.Text.To<EndPoint>() };
					Trader.Server = Server.Text;
					Trader.Login = Login.Text;
					Trader.CompressionLevel = (CompressionLevels)Compression.SelectedValue;
					Trader.Interface = Interface.Text;
					Trader.OrderBookDepth = Depth.Text.To<int>();

					if (!Password.Password.IsEmpty())
						Trader.Password = Password.Password;
	
					if (!_initialized)
					{
						_initialized = true;

						// инициализируем механизм переподключения
						Trader.ReConnectionSettings.WorkingTime = ExchangeBoard.Micex.WorkingTime;
						Trader.Restored += () => this.GuiAsync(() => MessageBox.Show(this, LocalizedStrings.Str2958));

						// подписываемся на событие успешного соединения
						Trader.Connected += () =>
						{
							this.GuiAsync(() => ChangeConnectStatus(true));

							// запускаем подписку на новости
							Trader.RegisterNews();
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
						Trader.Error += error =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

						// подписываемся на ошибку подписки маркет-данных
						Trader.MarketDataSubscriptionFailed += (security, type, error) =>
							this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

						var ticksSubscribed = false;

						Trader.NewSecurities += securities =>
						{
							// запускаем экспорт всех тиков
							if (!ticksSubscribed)
							{
								Trader.RegisterTrades(securities.First());
								ticksSubscribed = true;
							}

							_securitiesWindow.SecurityPicker.Securities.AddRange(securities);
						};

						Trader.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);
						Trader.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
						Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);

						Trader.NewPortfolios += portfolios => _portfoliosWindow.PortfolioGrid.Portfolios.AddRange(portfolios);
						Trader.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);

						// подписываемся на событие о неудачной регистрации заявок
						Trader.OrdersRegisterFailed += OrdersFailed;

						// подписываемся на событие о неудачном снятии заявок
						Trader.OrdersCancelFailed += OrdersFailed;

						Trader.NewNews += news => _newsWindow.NewsPanel.NewsGrid.News.Add(news);

						// устанавливаем поставщик маркет-данных
						_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

						// set news provider
						_newsWindow.NewsPanel.NewsProvider = Trader;
					}

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

			ShowSecurities.IsEnabled = ShowTrades.IsEnabled = ShowNews.IsEnabled =
            ShowMyTrades.IsEnabled = ShowOrders.IsEnabled =
            ShowPortfolios.IsEnabled = isConnected;
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