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
namespace SampleMultiConnection
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
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Configuration;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;

		public readonly Connector Connector;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly OrdersWindow _ordersWindow = new OrdersWindow();
		private readonly StopOrderWindow _stopOrdersWindow = new StopOrderWindow();
		private readonly PortfoliosWindow _portfoliosWindow = new PortfoliosWindow();
		private readonly MyTradesWindow _myTradesWindow = new MyTradesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();

		private const string _settingsFile = "connection.xml";
		private const string _defaultDataPath = "Data";

		public MainWindow()
		{
			InitializeComponent();
			Instance = this;

			Title = Title.Put("Multi connection");

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_stopOrdersWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();

			var logManager = new LogManager();
			logManager.Listeners.Add(new FileLogListener("sample.log"));

			var path = _defaultDataPath.ToFullPath();

			HistoryPath.Folder = path;

			var entityRegistry = new CsvEntityRegistry(path);

			var storageRegistry = new StorageRegistry
			{
				DefaultDrive = new LocalMarketDataDrive(path)
			};

			ConfigManager.RegisterService<IEntityRegistry>(entityRegistry);
			ConfigManager.RegisterService<IStorageRegistry>(storageRegistry);
			// ecng.serialization invoke in several places IStorage obj
			ConfigManager.RegisterService(entityRegistry.Storage);

			var snapshotRegistry = new SnapshotRegistry(Path.Combine(path, "Snapshots"));

			Connector = new Connector(entityRegistry, storageRegistry, snapshotRegistry);
			logManager.Sources.Add(Connector);

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
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// subscribe on error of market data subscription event
			Connector.MarketDataSubscriptionFailed += (security, msg, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));

			Connector.NewSecurity += _securitiesWindow.SecurityPicker.Securities.Add;
			Connector.NewTrade += _tradesWindow.TradeGrid.Trades.Add;

			Connector.NewOrder += _ordersWindow.OrderGrid.Orders.Add;
			Connector.NewStopOrder += _stopOrdersWindow.OrderGrid.Orders.Add;
			Connector.NewMyTrade += _myTradesWindow.TradeGrid.Trades.Add;
			
			Connector.NewPortfolio += _portfoliosWindow.PortfolioGrid.Portfolios.Add;
			Connector.NewPosition += _portfoliosWindow.PortfolioGrid.Positions.Add;

			// subscribe on error of order registration event
			Connector.OrderRegisterFailed += _ordersWindow.OrderGrid.AddRegistrationFail;
			// subscribe on error of order cancelling event
			Connector.OrderCancelFailed += OrderFailed;

			// subscribe on error of stop-order registration event
			Connector.OrderRegisterFailed += _stopOrdersWindow.OrderGrid.AddRegistrationFail;
			// subscribe on error of stop-order cancelling event
			Connector.StopOrderCancelFailed += OrderFailed;

			// set market data provider
			_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;

			try
			{
				if (File.Exists(_settingsFile))
				{
					var ctx = new ContinueOnExceptionContext();
					ctx.Error += ex => ex.LogError();

					using (new Scope<ContinueOnExceptionContext> (ctx))
						Connector.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));
				}
			}
			catch
			{
			}

			if (Connector.StorageAdapter == null)
				return;

			try
			{
				entityRegistry.Init();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, ex.ToString());
			}

			Connector.StorageAdapter.DaysLoad = TimeSpan.FromDays(3);
			Connector.LookupAll();

			snapshotRegistry.Init();

			ConfigManager.RegisterService<IExchangeInfoProvider>(new StorageExchangeInfoProvider(entityRegistry));
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

			Connector.Dispose();

			ServicesRegistry.EntityRegistry.DelayAction.DefaultGroup.WaitFlush(true);

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

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

		private void OrderFailed(OrderFail fail)
		{
			this.GuiAsync(() =>
			{
				MessageBox.Show(this, fail.Error.ToString(), LocalizedStrings.Str153);
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

		private void HistoryPath_OnFolderChanged(string path)
		{
			if (Connector == null)
				return;

			Connector.StorageAdapter.Drive = new LocalMarketDataDrive(path.ToFullPath());
			Connector.LookupAll();
		}
	}
}