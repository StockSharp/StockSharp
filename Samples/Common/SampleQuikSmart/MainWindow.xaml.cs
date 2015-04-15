namespace SampleQuikSmart
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Net;
	using System.Security;
	using System.Windows;

	using Ookii.Dialogs.Wpf;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Quik;
	using StockSharp.SmartCom;
	using StockSharp.Algo;
	using StockSharp.Fix;
	using StockSharp.Localization;
	using StockSharp.Quik.Lua;

	public partial class MainWindow
	{
		private bool _isConnected;

		public Connector Connector;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			_ordersWindow.MakeHideable();
			_securitiesWindow.MakeHideable();

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			QuikPath.Text = QuikTerminal.GetDefaultPath();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_ordersWindow.Close();

			if (Connector != null)
				Connector.Dispose();

			base.OnClosing(e);
		}

		private void FindQuikPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!QuikPath.Text.IsEmpty())
				dlg.SelectedPath = QuikPath.Text;

			if (dlg.ShowDialog(this) == true)
			{
				QuikPath.Text = dlg.SelectedPath;
			}
		}

		private Connector InitReconnectionSettings(Connector trader)
		{
			// инициализируем механизм переподключения
			trader.ReConnectionSettings.WorkingTime = ExchangeBoard.Forts.WorkingTime;
			trader.ReConnectionSettings.ConnectionSettings.Restored += () => this.GuiAsync(() =>
			{
				// разблокируем кнопку Экспорт (соединение было восстановлено)
				ChangeConnectStatus(true);
				MessageBox.Show(this, LocalizedStrings.Str2958);
			});

			return trader;
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (Connector == null)
				{
					var isDde = IsDde.IsChecked == true;

					if (SmartLogin.Text.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2965);
						return;
					}
					if (SmartPassword.Password.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2966);
						return;
					}
					if (isDde && QuikPath.Text.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2967);
						return;
					}

					// создаем агрегирующее подключение (+ сразу инициализируем настройки переподключения)
					Connector = InitReconnectionSettings(new Connector());

					// добавляем подключения к SmartCOM и Quik
					var quikTs = new LuaFixTransactionMessageAdapter(Connector.TransactionIdGenerator)
					{
						Login = "quik",
						Password = "quik".To<SecureString>(),
						Address = "localhost:5001".To<EndPoint>(),
						TargetCompId = "StockSharpTS",
						SenderCompId = "quik",
						ExchangeBoard = ExchangeBoard.Forts,
						Version = FixVersions.Fix44,
						RequestAllPortfolios = true,
						MarketData = FixMarketData.None,
						UtcOffset = TimeHelper.Moscow.BaseUtcOffset
					};
					var quikMd = new FixMessageAdapter(Connector.TransactionIdGenerator)
					{
						Login = "quik",
						Password = "quik".To<SecureString>(),
						Address = "localhost:5001".To<EndPoint>(),
						TargetCompId = "StockSharpMD",
						SenderCompId = "quik",
						ExchangeBoard = ExchangeBoard.Forts,
						Version = FixVersions.Fix44,
						RequestAllSecurities = true,
						MarketData = FixMarketData.MarketData,
						UtcOffset = TimeHelper.Moscow.BaseUtcOffset
					};
					Connector.Adapter.InnerAdapters[quikMd] = 1;
					Connector.Adapter.InnerAdapters[quikTs] = 1;

					var smartCom = new SmartComMessageAdapter(Connector.TransactionIdGenerator)
					{
						Login = SmartLogin.Text,
						Password = SmartPassword.Password.To<SecureString>(),
						Address = SmartAddress.SelectedAddress,
					};
					Connector.Adapter.InnerAdapters[smartCom] = 0;

					// очищаем из текстового поля в целях безопасности
					//SmartPassword.Clear();

					var logManager = new LogManager();
					logManager.Listeners.Add(new FileLogListener("sample.log"));
					logManager.Sources.Add(Connector);

					// подписываемся на событие успешного соединения
					Connector.Connected += () =>
					{
						// возводим флаг, что соединение установлено
						_isConnected = true;

						// разблокируем кнопку Экспорт
						this.GuiAsync(() => ChangeConnectStatus(true));
					};

					// подписываемся на событие разрыва соединения
					Connector.ConnectionError += error => this.GuiAsync(() =>
					{
						// заблокируем кнопку Экспорт (так как соединение было потеряно)
						ChangeConnectStatus(false);

						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);	
					});

					// подписываемся на ошибку обработки данных (транзакций и маркет)
					Connector.ProcessDataError += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// подписываемся на ошибку подписки маркет-данных
					Connector.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

					Connector.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
					Connector.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);

					// подписываемся на событие о неудачной регистрации заявок
					Connector.OrdersRegisterFailed += OrdersFailed;
					// подписываемся на событие о неудачном снятии заявок
					Connector.OrdersCancelFailed += OrdersFailed;

					// подписываемся на событие о неудачной регистрации стоп-заявок
					Connector.StopOrdersRegisterFailed += OrdersFailed;
					// подписываемся на событие о неудачном снятии стоп-заявок
					Connector.StopOrdersCancelFailed += OrdersFailed;

					// устанавливаем поставщик маркет-данных
					_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;

					ShowSecurities.IsEnabled = ShowOrders.IsEnabled = true;
				}

				Connector.Connect();
			}
			else
			{
				Connector.Disconnect();
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
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowOrdersClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_ordersWindow);
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