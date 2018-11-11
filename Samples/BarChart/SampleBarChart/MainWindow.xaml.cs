#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleBarChart.SampleBarChartPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleBarChart
{
	using System;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;

	using StockSharp.BarChart;
	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private bool _isInitialized;

		public readonly BarChartTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			Trader = new BarChartTrader();
			//{
			//	LogLevel = LogLevels.Debug,
			//	MarketDataAdapter = { LogLevel = LogLevels.Debug }
			//};

			ConfigManager.RegisterService<IConnector>(Trader);

			_securitiesWindow.MakeHideable();

			Instance = this;

			// and file logs.txt
			_logManager.Listeners.Add(new FileLogListener
			{
				FileName = "logs",
			});

			_logManager.Listeners.Add(new FileLogListener("log.txt"));
			_logManager.Listeners.Add(new GuiLogListener(Monitor));

			_logManager.Sources.Add(Trader);
			_logManager.Sources.Add(new TraceSource());
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_securitiesWindow.DeleteHideable();
			_securitiesWindow.Close();

			Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isInitialized)
			{
				_isInitialized = true;

				// subscribe on connection successfully event
				Trader.Connected += () =>
				{
					Trader.RegisterNews();

					// update gui labels
					this.GuiAsync(() => ChangeConnectStatus(true));
				};
				Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

				// subscribe on connection error event
				Trader.ConnectionError += error => this.GuiAsync(() =>
				{
					// update gui labels
					this.GuiAsync(() => ChangeConnectStatus(false));

					MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
				});

				// subscribe on error event
				Trader.Error += error =>
					this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

				// subscribe on error of market data subscription event
				Trader.MarketDataSubscriptionFailed += (security, msg, error) =>
					this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

				Trader.NewSecurity += security => _securitiesWindow.SecurityPicker.Securities.Add(security);

				// set market data provider
				_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

				ShowSecurities.IsEnabled = true;
			}

			if (Trader.ConnectionState == ConnectionStates.Disconnected || Trader.ConnectionState == ConnectionStates.Failed)
			{
				// set connection settings
				Trader.Login = LoginCtrl.Text;
				Trader.Password = PasswordCtrl.Password;

				Trader.Connect();	
			}
			else
				Trader.Disconnect();
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
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