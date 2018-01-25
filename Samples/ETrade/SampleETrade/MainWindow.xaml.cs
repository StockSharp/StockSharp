#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleETrade.SampleETradePublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleETrade
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Reflection;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.ETrade;
	using StockSharp.ETrade.Native;
	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		public static MainWindow Instance { get; private set; }

		public static readonly DependencyProperty IsConnectedProperty = DependencyProperty.Register(nameof(IsConnected), typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

		public bool IsConnected
		{
			get => (bool)GetValue(IsConnectedProperty);
			set => SetValue(IsConnectedProperty, value);
		}

		public ETradeTrader Trader { get; private set; }

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();

		//private Security[] _securities;

		private static string ConsumerKey => Properties.Settings.Default.ConsumerKey;

		private static bool IsSandbox => Properties.Settings.Default.Sandbox;

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			Instance = this;
			InitializeComponent();

			Title = Title.Put("E*TRADE");

			Closing += OnClosing;

			_ordersWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_myTradesWindow.MakeHideable();

			var guilistener = new GuiLogListener(LogControl);
			guilistener.Filters.Add(msg => msg.Level > LogLevels.Debug);
			_logManager.Listeners.Add(guilistener);

			var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
			var path = Path.Combine(location ?? "", "ETrade", "restdump_{0:yyyyMMdd-HHmmss}.log".Put(DateTime.Now));

			_logManager.Listeners.Add(new FileLogListener(path));

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

			_securitiesWindow.Close();
			_stopOrdersWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
			_myTradesWindow.Close();

			if (Trader != null)
				Trader.Dispose();
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			var secret = PwdBox.Password;

			if (!IsConnected)
			{
				if (ConsumerKey.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str3689);
					return;
				}
				if (secret.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str3690);
					return;
				}

				if (Trader == null)
				{
					// create connector
					Trader = new ETradeTrader();

					try
					{
						Trader.AccessToken = null;

						var token = OAuthToken.Deserialize(Properties.Settings.Default.AccessToken);
						if (token != null && token.ConsumerKey.ToLowerInvariant() == ConsumerKey.ToLowerInvariant())
							Trader.AccessToken = token;
					}
					catch (Exception ex)
					{
						MessageBox.Show(this, LocalizedStrings.Str3691Params.Put(ex));
					}

					Trader.LogLevel = LogLevels.Debug;

					_logManager.Sources.Add(Trader);

					// subscribe on connection successfully event
					Trader.Connected += () => this.GuiAsync(() =>
					{
						Properties.Settings.Default.AccessToken = Trader.AccessToken.Serialize();
						OnConnectionChanged(true);
					});

					// subscribe on connection error event
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						OnConnectionChanged(Trader.ConnectionState == ConnectionStates.Connected);
						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

					Trader.Disconnected += () => this.GuiAsync(() => OnConnectionChanged(false));

					// subscribe on error event
					Trader.Error += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// subscribe on error of market data subscription event
					Trader.MarketDataSubscriptionFailed += (security, msg, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

					Trader.NewSecurity += _securitiesWindow.SecurityPicker.Securities.Add;
					Trader.NewMyTrade += _myTradesWindow.TradeGrid.Trades.Add;
					Trader.NewOrder += _ordersWindow.OrderGrid.Orders.Add;
					Trader.NewStopOrder += _stopOrdersWindow.OrderGrid.Orders.Add;
					Trader.NewPortfolio += _portfoliosWindow.PortfolioGrid.Portfolios.Add;
					Trader.NewPosition += _portfoliosWindow.PortfolioGrid.Positions.Add;

					// subscribe on error of order registration event
					Trader.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
					// subscribe on error of order cancelling event
					Trader.OrderCancelFailed += OrderFailed;

					// subscribe on error of stop-order registration event
					Trader.StopOrderRegisterFailed += _stopOrdersWindow.OrderGrid.AddRegistrationFail;
					// subscribe on error of stop-order cancelling event
					Trader.StopOrderCancelFailed += OrderFailed;

					Trader.MassOrderCancelFailed += (transId, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;
				}

				Trader.Sandbox = IsSandbox;
				//Trader.SandboxSecurities = IsSandbox ? GetSandboxSecurities() : null;
				Trader.ConsumerKey = ConsumerKey;
				Trader.ConsumerSecret = secret;

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

		private void OrderFailed(OrderFail fail)
		{
			this.GuiAsync(() =>
			{
				MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str153);
			});
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

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}

		//private Security[] GetSandboxSecurities()
		//{
		//	return _securities ?? (_securities = new[]
		//	{
		//		new Security
		//		{
		//			Id = "CSCO@EQ", Code = "CSCO", Name = "CISCO SYS INC", ExchangeBoard = ExchangeBoard.Test,
		//			Decimals = 2, VolumeStep = 1, StepPrice = 0.01m, PriceStep = 0.01m
		//		},
		//		new Security
		//		{
		//			Id = "IBM@EQ", Code = "IBM", Name = "INTERNATIONAL BUSINESS MACHS COM", ExchangeBoard = ExchangeBoard.Test,
		//			Decimals = 2, VolumeStep = 1, StepPrice = 0.01m, PriceStep = 0.01m
		//		},
		//		new Security
		//		{
		//			Id = "MSFT@EQ", Code = "MSFT", Name = "MICROSOFT CORP COM", ExchangeBoard = ExchangeBoard.Test,
		//			Decimals = 2, VolumeStep = 1, StepPrice = 0.01m, PriceStep = 0.01m
		//		}
		//	});
		//}
	}
}
