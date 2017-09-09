#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleSterling.SampleSterlingPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleSterling
{
	using System;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

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
				DependencyProperty.Register(nameof(IsConnected), typeof(bool), typeof(MainWindow), new PropertyMetadata(default(bool)));

		public bool IsConnected
		{
			get => (bool)GetValue(IsConnectedProperty);
			set => SetValue(IsConnectedProperty, value);
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

			Title = Title.Put("Sterling");

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
			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "Logs" });

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
					// create connector
					Trader = new SterlingTrader { LogLevel = LogLevels.Debug };

					_logManager.Sources.Add(Trader);

					// subscribe on connection successfully event
					Trader.Connected += () =>
					{
						this.GuiAsync(() => OnConnectionChanged(true));
						AddSecurities();
                        Trader.RegisterNews();
					};

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

					Trader.NewNews += news =>  _newsWindow.NewsPanel.NewsGrid.News.Add(news);

					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

					// set news provider
					_newsWindow.NewsPanel.NewsProvider = Trader;
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

		private void OrderFailed(OrderFail fail)
		{
			this.GuiAsync(() =>
			{
				MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str153);
			});
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
			Trader.SendOutMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "AAPL",
					BoardCode = "BATS",
				},
				Name = "AAPL",
				SecurityType = SecurityTypes.Stock,
			});

			Trader.SendOutMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "AAPL",
					BoardCode = MessageAdapter.DefaultAssociatedBoardCode,
				},
				Name = "AAPL",
				SecurityType = SecurityTypes.Stock,
			});

			Trader.SendOutMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "IBM",
					BoardCode = "BATS",
				},
				Name = "IBM",
				SecurityType = SecurityTypes.Stock,
			});

			Trader.SendOutMessage(new SecurityMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = "IBM",
					BoardCode = MessageAdapter.DefaultAssociatedBoardCode,
				},
				Name = "IBM",
				SecurityType = SecurityTypes.Stock,
			});
		}
	}
}