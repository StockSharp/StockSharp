#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleOEC.SampleOECPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleQuantFeed
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Xaml;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.QuantHouse;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private bool _isConnected;

		public QuantFeedTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly NewsWindow _newsWindow = new NewsWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			Title = Title.Put("QuantFEED");

			_securitiesWindow.MakeHideable();
			_newsWindow.MakeHideable();

			Instance = this;

			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "StockSharp_QuantFEED" });
			_logManager.Listeners.Add(new GuiLogListener(LogControl));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_securitiesWindow.DeleteHideable();
			_newsWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_newsWindow.Close();

			Trader?.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (Login.Text.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2974);
					return;
				}
				else if (Password.Password.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2975);
					return;
				}

				if (Trader == null)
				{
					// create connector
					Trader = new QuantFeedTrader();

					_logManager.Sources.Add(Trader);

					Trader.Restored += () => this.GuiAsync(() =>
					{
						// update gui labels
						ChangeConnectStatus(true);
						MessageBox.Show(this, LocalizedStrings.Str2958);
					});

					// subscribe on connection successfully event
					Trader.Connected += () =>
					{
						// set flag (connection is established)
						_isConnected = true;

						// update gui labels
						this.GuiAsync(() => ChangeConnectStatus(true));
					};

					// subscribe on connection error event
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						// update gui labels
						ChangeConnectStatus(false);

						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);	
					});

					Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

					// subscribe on error event
					Trader.Error += error => this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					Trader.NewSecurity += _securitiesWindow.SecurityPicker.Securities.Add;
					Trader.NewNews += _newsWindow.NewsPanel.NewsGrid.News.Add;

					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

					// set news provider
					_newsWindow.NewsPanel.NewsProvider = Trader;

					ShowSecurities.IsEnabled = ShowNews.IsEnabled = true;
				}

				Trader.Login = Login.Text;
				Trader.Password = Password.SecurePassword;
				Trader.Address = Address.Text.To<EndPoint>();

				// clear password box for security reason
				//Password.Clear();

				Trader.Connect();
			}
			else
			{
				Trader.UnRegisterNews();

				Trader.Disconnect();
			}
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

		private void ShowNewsClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_newsWindow);
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