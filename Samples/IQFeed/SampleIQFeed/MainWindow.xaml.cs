namespace SampleIQFeed
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.IQFeed;
	using StockSharp.Logging;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isInitialized;

		public readonly IQFeedTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			Trader = new IQFeedTrader();
			//{
			//	LogLevel = LogLevels.Debug,
			//	MarketDataAdapter = { LogLevel = LogLevels.Debug }
			//};

			ConfigManager.RegisterService<IConnector>(Trader);

			Level1AddressCtrl.Text = Trader.Level1Address.To<string>();
			Level2AddressCtrl.Text = Trader.Level2Address.To<string>();
			LookupAddressCtrl.Text = Trader.LookupAddress.To<string>();
			AdminAddressCtrl.Text = Trader.AdminAddress.To<string>();

			DownloadSecurityFromSiteCtrl.IsChecked = Trader.IsDownloadSecurityFromSite;

			_securitiesWindow.MakeHideable();
			_newsWindow.MakeHideable();

			Instance = this;

			_logManager.Listeners.Add(new FileLogListener("log.txt"));
			_logManager.Sources.Add(Trader);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_securitiesWindow.DeleteHideable();
			_newsWindow.DeleteHideable();

			_securitiesWindow.Close();
			_newsWindow.Close();

			Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isInitialized)
			{
				_isInitialized = true;

				// подписываемся на событие успешного экспорта
				Trader.ExportStarted += () =>
				{
					Trader.RegisterNews();

					// меняем надпись на Отключиться
					this.GuiAsync(() => ChangeConnectStatus(true));
				};
				Trader.ExportStopped += () => this.GuiAsync(() => ChangeConnectStatus(false));

				// подписываемся на событие разрыва соединения
				Trader.ExportError += error => this.GuiAsync(() =>
				{
					// меняем надпись на Подключиться
					this.GuiAsync(() => ChangeConnectStatus(false));

					MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
				});

				// подписываемся на ошибку обработки данных (транзакций и маркет)
				Trader.ProcessDataError += error =>
					this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

				// подписываемся на ошибку подписки маркет-данных
				Trader.MarketDataSubscriptionFailed += (security, type, error) =>
					this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

				Trader.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
				Trader.NewNews += news => _newsWindow.NewsGrid.News.Add(news);

				// устанавливаем поставщик маркет-данных
				_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

				ShowNews.IsEnabled = ShowSecurities.IsEnabled = true;
			}

			if (Trader.ExportState == ConnectionStates.Disconnected || Trader.ExportState == ConnectionStates.Failed)
			{
				//устанавливаем настройки для подключения
				Trader.Level1Address = Level1AddressCtrl.Text.To<EndPoint>();
				Trader.Level2Address = Level2AddressCtrl.Text.To<EndPoint>();
				Trader.LookupAddress = LookupAddressCtrl.Text.To<EndPoint>();
				Trader.AdminAddress = AdminAddressCtrl.Text.To<EndPoint>();

				Trader.IsDownloadSecurityFromSite = DownloadSecurityFromSiteCtrl.IsChecked == true;

				Trader.StartExport();	
			}
			else
				Trader.StopExport();
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			ConnectBtn.Content = isConnected ? LocalizedStrings.Str2961 : LocalizedStrings.Str2962;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
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