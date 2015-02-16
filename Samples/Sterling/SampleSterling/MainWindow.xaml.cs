namespace SampleSterling
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Sterling;
	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Localization;
	using StockSharp.Messages;

	public partial class MainWindow
	{
		public static MainWindow Instance { get; private set; }

		public static readonly DependencyProperty IsConnectedProperty = 
				DependencyProperty.Register("IsConnected", typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

		public bool IsConnected
		{
			get { return (bool)GetValue(IsConnectedProperty); }
			set { SetValue(IsConnectedProperty, value); }
		}

		public SterlingTrader Trader { get; private set; }

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			Instance = this;
			InitializeComponent();

			Closing += OnClosing;

			_ordersWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_newsWindow.MakeHideable();

			var guiListener = new GuiLogListener(LogControl);
			//guiListener.Filters.Add(msg => msg.Level > LogLevels.Debug);
			_logManager.Listeners.Add(guiListener);
			_logManager.Listeners.Add(new FileLogListener("sterling") { LogDirectory = "Logs" });

			Application.Current.MainWindow = this;
		}

		private void OnClosing(object sender, CancelEventArgs cancelEventArgs)
		{
			Properties.Settings.Default.Save();
			_ordersWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_stopOrdersWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_newsWindow.DeleteHideable();

			_securitiesWindow.Close();
			_stopOrdersWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
			_myTradesWindow.Close();
			_newsWindow.Close();

			if (Trader != null)
				Trader.Dispose();
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!IsConnected)
			{
				if (Trader == null)
				{
					// создаем подключение
					Trader = new SterlingTrader { LogLevel = LogLevels.Debug };

					_logManager.Sources.Add(Trader);

					// подписываемся на событие успешного соединения
					Trader.Connected += () =>
					{
						this.GuiAsync(() => OnConnectionChanged(true));
						AddSecurities();
						Trader.StartExport();
					};

					// подписываемся на событие разрыва соединения
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						OnConnectionChanged(Trader.ConnectionState == ConnectionStates.Connected);
						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

					Trader.Disconnected += () => this.GuiAsync(() => OnConnectionChanged(false));

					// подписываемся на ошибку обработки данных (транзакций и маркет)
					Trader.ProcessDataError += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// подписываемся на ошибку подписки маркет-данных
					Trader.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

					Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
					Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
					Trader.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
					Trader.NewStopOrders += orders => this.GuiAsync(() => _stopOrdersWindow.OrderGrid.Orders.AddRange(orders));
					Trader.NewPortfolios += portfolios =>
					{
						// регистрирует портфели на обновление данных
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

					Trader.NewNews += news => _newsWindow.NewsPanel.NewsGrid.News.Add(news);

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

		private void OnConnectionChanged(bool isConnected)
		{
			IsConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void OrdersFailed(IEnumerable<OrderFail> fails)
		{
			this.GuiAsync(() =>
			{
				foreach (var fail in fails)
				{
					var msg = fail.Error.ToString();
					MessageBox.Show(this, msg, LocalizedStrings.Str2960);
				}
			});
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

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowPortfoliosClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_portfoliosWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
		}

		private void ShowStopOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_stopOrdersWindow);
		}

		private void ShowNewsClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_newsWindow);
		}

		private void AddSecurities()
		{
			Trader.MarketDataAdapter.SendInMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "AAPL",
					BoardCode = "BATS",
				},
				Name = "AAPL",
				SecurityType = SecurityTypes.Stock,
			});

			Trader.MarketDataAdapter.SendInMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "AAPL",
					BoardCode = "All",
				},
				Name = "AAPL",
				SecurityType = SecurityTypes.Stock,
			});

			Trader.MarketDataAdapter.SendInMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "IBM",
					BoardCode = "BATS",
				},
				Name = "IBM",
				SecurityType = SecurityTypes.Stock,
			});

			Trader.MarketDataAdapter.SendInMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "IBM",
					BoardCode = "All",
				},
				Name = "IBM",
				SecurityType = SecurityTypes.Stock,
			});
		}

	}
}