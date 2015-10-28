namespace SampleITCH
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Net;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.ITCH;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private bool _isConnected;
		private bool _initialized;

		public ItchTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly OrdersLogWindow _orderLogWindow = new OrdersLogWindow();

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			_orderLogWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();

			Title = Title.Put("ITCH");

			Instance = this;

			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "StockSharp_ITCH" });
			_logManager.Listeners.Add(new GuiLogListener(Monitor));

			// create connector
			Trader = new ItchTrader
			{
				LogLevel = LogLevels.Debug,
				CreateDepthFromOrdersLog = true
			};

			_logManager.Sources.Add(Trader);

			Trader.Login = "ABCB07";
			Trader.Password = "mit_1234";
			Trader.PrimaryMulticast = new MulticastSourceAddress
			{
				//GroupAddress = "224.4.2.32".To<IPAddress>(),
				GroupAddress = "224.4.4.38".To<IPAddress>(),
				SourceAddress = "194.169.8.200".To<IPAddress>(),
				Port = 61000,
			};
			Trader.RecoveryAddress = "194.169.8.216:54038".To<IPEndPoint>();
			Trader.ReplayAddress = "194.169.8.216:53038".To<IPEndPoint>();
			Trader.SecurityCsvFile = @"LSE_Ref\20151007_XLON_Instrument.csv";
			Trader.SecurityDelayLoad = true;
			Trader.GroupId = 'G';
			Trader.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

			Settings.SelectedObject = Trader.MarketDataAdapter;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_orderLogWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_orderLogWindow.Close();

			if (Trader != null)
				Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (Trader.Login.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2974);
					return;
				}
				else if (Trader.Password.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2975);
					return;
				}

				if (!_initialized)
				{
					_initialized = true;

					// update gui labes
					Trader.ReConnectionSettings.WorkingTime = ExchangeBoard.Forts.WorkingTime;
					Trader.Restored += () => this.GuiAsync(() =>
					{
						ChangeConnectStatus(true);
						MessageBox.Show(this, LocalizedStrings.Str2958);
					});

					// subscribe on connection successfully event
					Trader.Connected += () =>
					{
						// set flag (connection is established)
						_isConnected = true;

						// update gui labes
						this.GuiAsync(() => ChangeConnectStatus(true));
					};
					Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

					// subscribe on connection error event
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						// update gui labes
						ChangeConnectStatus(false);

						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);	
					});

					// subscribe on error event
					Trader.Error += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// subscribe on error of market data subscription event
					Trader.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

					Trader.NewSecurities += _securitiesWindow.SecurityPicker.Securities.AddRange;
					Trader.NewTrades += _tradesWindow.TradeGrid.Trades.AddRange;
					Trader.NewOrderLogItems += _orderLogWindow.OrderLogGrid.LogItems.AddRange;

					var subscribed = false;
					//if (AllDepths.IsChecked == true)
					{
						Trader.LookupSecuritiesResult += securities =>
						{
							if (subscribed)
								return;

							subscribed = true;

							Trader.SendInMessage(new MarketDataMessage
							{
								IsSubscribe = true,
								DataType = MarketDataTypes.OrderLog,
								TransactionId = Trader.TransactionIdGenerator.GetNextId(),
							});
						};	
					}
					
					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

					ShowSecurities.IsEnabled = ShowTrades.IsEnabled = ShowOrdersLog.IsEnabled = true;
				}

				Trader.Connect();
			}
			else
			{
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

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}

		private void ShowOrdersLogClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_orderLogWindow);
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
