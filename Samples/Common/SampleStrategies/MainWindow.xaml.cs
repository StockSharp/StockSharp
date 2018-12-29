#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleMultiConnection.SampleMultiConnectionPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleStrategies
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Storages.Csv;
	using StockSharp.Logging;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly Connector Connector;
		public readonly LogManager LogManager;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly StrategiesWindow _strategiesWindow = new StrategiesWindow();

		public static MainWindow Instance { get; private set; }

		private const string _settingsFile = "connection.xml";

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put(LocalizedStrings.Str1355);

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_strategiesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			LogManager = new LogManager();
			LogManager.Listeners.Add(new FileLogListener("Data\\sample.log"));
			LogManager.Listeners.Add(new GuiLogListener(Monitor));

			var entityRegistry = new CsvEntityRegistry("Data");

			ConfigManager.RegisterService<IEntityRegistry>(entityRegistry);
			// ecng.serialization invoke in several places IStorage obj
			ConfigManager.RegisterService(entityRegistry.Storage);

			var storageRegistry = ServicesRegistry.StorageRegistry;
			var snapshotRegistry = new SnapshotRegistry(Path.Combine("Data", "Snapshots"));

			Connector = new Connector(entityRegistry, storageRegistry, snapshotRegistry);
			LogManager.Sources.Add(Connector);

			InitConnector(entityRegistry, snapshotRegistry);
		}

		private void InitConnector(CsvEntityRegistry entityRegistry, SnapshotRegistry snapshotRegistry)
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
			{
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));
			};

			// subscribe on error of market data subscription event
			Connector.MarketDataSubscriptionFailed += (security, msg, error) =>
			{
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));
			};

			Connector.NewSecurity += security => _securitiesWindow.SecurityPicker.Securities.Add(security);

			Connector.NewOrder += order =>
			{
				_ordersWindow.OrderGrid.Orders.Add(order);
				_securitiesWindow.ProcessOrder(order);
			};

			// display order as own volume in quotes window
			Connector.OrderChanged += _securitiesWindow.ProcessOrder;

			// put the registration error into order's table
			Connector.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;

			Connector.NewMyTrade += _myTradesWindow.TradeGrid.Trades.Add;

			Connector.NewPortfolio += _portfoliosWindow.PortfolioGrid.Portfolios.Add;
			Connector.NewPosition += _portfoliosWindow.PortfolioGrid.Positions.Add;

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

			if (Connector.StorageAdapter == null)
				return;

			entityRegistry.Init();

			Connector.StorageAdapter.DaysLoad = TimeSpan.FromDays(3);
			//Connector.LookupAll();

			snapshotRegistry.Init();

			ConfigManager.RegisterService<IExchangeInfoProvider>(new StorageExchangeInfoProvider(entityRegistry));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_strategiesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();

			_securitiesWindow.Close();
			_strategiesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();

			Connector.Dispose();

			ServicesRegistry.EntityRegistry.DelayAction.DefaultGroup.WaitFlush(true);

			base.OnClosing(e);
		}

		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			if (Connector.Configure(this))
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

		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
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

		private void ShowStrategiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_strategiesWindow);
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
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