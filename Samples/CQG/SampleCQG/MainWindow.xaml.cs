#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleCQG.SampleCQGPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleCQG
{
	using System;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using SampleCQG.Properties;

	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.Cqg.Com;
	using StockSharp.Cqg.Continuum;
	using StockSharp.Logging;
	using StockSharp.Xaml;
	using StockSharp.Localization;

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

		public Connector Connector { get; private set; }

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly StopOrdersWindow _stopOrdersWindow = new StopOrdersWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();

		private readonly LogManager _logManager = new LogManager();

		private static string Username => Settings.Default.Username;

		public MainWindow()
		{
			Instance = this;
			InitializeComponent();

			Title = Title.Put("CQG");

			Closing += OnClosing;

			_ordersWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();

			var guiListener = new GuiLogListener(LogControl);
			_logManager.Listeners.Add(guiListener);
			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "Logs" });

			Application.Current.MainWindow = this;
		}

		private void OnClosing(object sender, CancelEventArgs e)
		{
			Settings.Default.Save();

			_ordersWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_stopOrdersWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();

			_securitiesWindow.Close();
			_stopOrdersWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
			_myTradesWindow.Close();
			_tradesWindow.Close();

			Connector?.Dispose();
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			var pwd = PwdBox.Password;

			if (!IsConnected)
			{
				if (Username.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str3751);
					return;
				}

				if (pwd.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2975);
					return;
				}

				if (Connector == null)
				{
					// create connector

					if (IsCqgContinuum.IsChecked == true)
					{
						Connector = new CqgContinuumTrader
						{
							UserName = Username,
							Password = PwdBox.Password,
							Address = Settings.Default.Address,
						};
					}
					else
					{
						Connector = new CqgComTrader();
					}

					//Connector.LogLevel = LogLevels.Debug;
					_logManager.Sources.Add(Connector);

					// subscribe on connection successfully event
					Connector.Connected += () =>
					{
						this.GuiAsync(() => OnConnectionChanged(true));
					};

					// subscribe on connection error event
					Connector.ConnectionError += error => this.GuiAsync(() =>
					{
						OnConnectionChanged(Connector.ConnectionState == ConnectionStates.Connected);
						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

					Connector.Disconnected += () => this.GuiAsync(() => OnConnectionChanged(false));

					// subscribe on error event
					Connector.Error += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// subscribe on error of market data subscription event
					Connector.MarketDataSubscriptionFailed += (security, msg, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

					Connector.NewSecurity += _securitiesWindow.SecurityPicker.Securities.Add;
					Connector.NewMyTrade += _myTradesWindow.TradeGrid.Trades.Add;
					Connector.NewTrade += _tradesWindow.TradeGrid.Trades.Add;
					Connector.NewOrder += _ordersWindow.OrderGrid.Orders.Add;
					Connector.NewStopOrder += _stopOrdersWindow.OrderGrid.Orders.Add;
					Connector.NewPortfolio += _portfoliosWindow.PortfolioGrid.Portfolios.Add;
					Connector.NewPosition += _portfoliosWindow.PortfolioGrid.Positions.Add;

					// subscribe on error of order registration event
					Connector.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
					// subscribe on error of order cancelling event
					Connector.OrderCancelFailed += OrderFailed;

					// subscribe on error of stop-order registration event
					Connector.StopOrderRegisterFailed += _stopOrdersWindow.OrderGrid.AddRegistrationFail;
					// subscribe on error of stop-order cancelling event
					Connector.StopOrderCancelFailed += OrderFailed;

					Connector.MassOrderCancelFailed += (transId, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;
				}

				Connector.Connect();
			}
			else
			{
				Connector.Disconnect();
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

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}
	}
}