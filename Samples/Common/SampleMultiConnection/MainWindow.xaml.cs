namespace SampleMultiConnection
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Algo;
	using StockSharp.Configuration;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly Connector Connector = new Connector();

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();

		private const string _settingsFile = "connection.xml";

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Multi connection");

			_ordersWindow.MakeHideable();
			_securitiesWindow.MakeHideable();

			var logManager = new LogManager();
			logManager.Listeners.Add(new FileLogListener("sample.log"));
			logManager.Sources.Add(Connector);

			InitConnector();
		}

		private void InitConnector()
		{
			// subscribe on connection successfully event
			Connector.Connected += () =>
			{
				this.GuiAsync(() => ChangeConnectStatus(true));
			};

			// subscribe on connection error event
			Connector.ConnectionError += error => this.GuiAsync(() =>
			{
				ChangeConnectStatus(false);
				MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
			});

			Connector.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

			// subscribe on error event
			Connector.Error += error =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// subscribe on error of market data subscription event
			Connector.MarketDataSubscriptionFailed += (security, type, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

			Connector.NewSecurities += securities => _securitiesWindow.SecurityPicker.Securities.AddRange(securities);
			Connector.NewOrders += orders => _ordersWindow.OrderGrid.Orders.AddRange(orders);

			// subscribe on error of order registration event
			Connector.OrdersRegisterFailed += OrdersFailed;
			// subscribe on error of order cancelling event
			Connector.OrdersCancelFailed += OrdersFailed;

			// set market data provider
			_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;

			try
			{
				if (File.Exists(_settingsFile))
					Connector.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));
			}
			catch
			{
			}
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_ordersWindow.Close();

			Connector.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			Connector.Configure(this);

			new XmlSerializer<SettingsStorage>().Serialize(Connector.Save(), _settingsFile);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
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