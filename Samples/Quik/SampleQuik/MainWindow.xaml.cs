#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleQuik.SampleQuikPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleQuik
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Security;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Quik;
	using StockSharp.Localization;
	using StockSharp.Logging;

	public partial class MainWindow
	{
		public QuikTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrderWindow _stopOrdersWindow = new StopOrderWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("QUIK");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			// попробовать сразу найти месторасположение Quik по запущенному процессу
			QuikPath.Folder = QuikTerminal.GetDefaultPath();

			_logManager.Listeners.Add(new FileLogListener("quik_logs.txt"));
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

		private bool _isConnected;

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				var isLua = IsLua.IsChecked == true;

				if (isLua)
				{
					if (Address.Text.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2977);
						return;
					}

					if (Login.Text.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2978);
						return;
					}

					if (Password.Password.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2979);
						return;
					}
				}
				else
				{
					if (QuikPath.Folder.IsEmpty())
					{
						MessageBox.Show(this, LocalizedStrings.Str2969);
						return;
					}
				}

				if (Trader == null)
				{
					// создаем подключение
					Trader = isLua
						? new QuikTrader
						{
							LuaFixServerAddress = Address.Text.To<EndPoint>(),
							LuaLogin = Login.Text,
							LuaPassword = Password.Password.To<SecureString>()
						}
						: new QuikTrader(QuikPath.Folder) { IsDde = true };

					Trader.LogLevel = LogLevels.Debug;

					_logManager.Sources.Add(Trader);

					// отключение автоматического запроса всех инструментов.
					Trader.RequestAllSecurities = AllSecurities.IsChecked == true;

					// возводим флаг, что соединение установлено
					_isConnected = true;

					// переподключение будет работать только во время работы биржи РТС
					// (чтобы отключить переподключение когда торгов нет штатно, например, ночью)
					Trader.ReConnectionSettings.WorkingTime = ExchangeBoard.Forts.WorkingTime;

					// подписываемся на событие об успешном восстановлении соединения
					Trader.Restored += () => this.GuiAsync(() => MessageBox.Show(this, LocalizedStrings.Str2958));

					// подписываемся на событие разрыва соединения
					Trader.ConnectionError += error => this.GuiAsync(() => MessageBox.Show(this, error.ToString()));

					// подписываемся на ошибку обработки данных (транзакций и маркет)
					//Trader.Error += error =>
					//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), "Ошибка обработки данных"));

					// подписываемся на ошибку подписки маркет-данных
					Trader.MarketDataSubscriptionFailed += (security, msg, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

					Trader.NewSecurity += _securitiesWindow.SecurityPicker.Securities.Add;
					Trader.NewMyTrade += _myTradesWindow.TradeGrid.Trades.Add;
					Trader.NewTrade += _tradesWindow.TradeGrid.Trades.Add;
					Trader.NewOrder += _ordersWindow.OrderGrid.Orders.Add;
					Trader.NewStopOrder += _stopOrdersWindow.OrderGrid.Orders.Add;
					Trader.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
					Trader.StopOrderRegisterFailed += _stopOrdersWindow.OrderGrid.AddRegistrationFail;
					Trader.OrderCancelFailed += fail => this.GuiAsync(() => MessageBox.Show(this, fail.Error.Message, LocalizedStrings.Str2981));
					Trader.StopOrderCancelFailed += fail => this.GuiAsync(() => MessageBox.Show(this, fail.Error.Message, LocalizedStrings.Str2981));
					Trader.NewPortfolio += _portfoliosWindow.PortfolioGrid.Portfolios.Add;
					Trader.NewPosition += _portfoliosWindow.PortfolioGrid.Positions.Add;

					Trader.MassOrderCancelFailed += (transId, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

					// устанавливаем поставщик маркет-данных
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

					ShowSecurities.IsEnabled = ShowTrades.IsEnabled =
						ShowMyTrades.IsEnabled = ShowOrders.IsEnabled =
							ShowPortfolios.IsEnabled = ShowStopOrders.IsEnabled = true;
				}

				Trader.Connect();

				_isConnected = true;
				ConnectBtn.Content = LocalizedStrings.Disconnect;
			}
			else
			{
				Trader.Disconnect();

				_isConnected = false;
				ConnectBtn.Content = LocalizedStrings.Connect;
			}
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
