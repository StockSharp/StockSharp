namespace SampleFix
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.Fix;
	using StockSharp.Logging;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isInitialized;

		public FixTrader Trader = new FixTrader();

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();

		private readonly LogManager _logManager = new LogManager();

		private const string _settingsFile = "fix_settings.xml";

		public MainWindow()
		{
			InitializeComponent();

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_newsWindow.MakeHideable();

			if (File.Exists(_settingsFile))
			{
				Trader.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));
			}

			MarketDataSessionSettings.SelectedObject = Trader.MarketDataAdapter;
			TransactionSessionSettings.SelectedObject = Trader.TransactionAdapter;

			Instance = this;

			Trader.LogLevel = LogLevels.Debug;

			_logManager.Sources.Add(Trader);
			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "StockSharp_Fix" });
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_stopOrdersWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_newsWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_stopOrdersWindow.Close();
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
			if (!_isInitialized)
			{
				_isInitialized = true;

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
				};
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
				Trader.NewMyTrades += trades => _myTradesWindow.TradeGrid.Trades.AddRange(trades);
				Trader.NewTrades += trades => _tradesWindow.TradeGrid.Trades.AddRange(trades);
				Trader.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);
				Trader.NewStopOrders += orders => _stopOrdersWindow.OrderGrid.Orders.AddRange(orders);

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

				ShowSecurities.IsEnabled = ShowTrades.IsEnabled = ShowNews.IsEnabled =
				ShowMyTrades.IsEnabled = ShowOrders.IsEnabled =
				ShowPortfolios.IsEnabled = ShowStopOrders.IsEnabled = true;
			}

			if (Trader.ConnectionState == ConnectionStates.Failed || Trader.ConnectionState == ConnectionStates.Disconnected)
			{
				new XmlSerializer<SettingsStorage>().Serialize(Trader.Save(), _settingsFile);

				Trader.Connect();
			}
			else if (Trader.ConnectionState == ConnectionStates.Connected)
			{
				Trader.Disconnect();
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