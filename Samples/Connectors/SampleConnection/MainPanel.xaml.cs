namespace SampleConnection
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.Collections;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public partial class MainPanel
	{
		private readonly SecuritiesWindow _securitiesWindow = new();
		private readonly OrdersWindow _ordersWindow = new();
		private readonly PortfoliosWindow _portfoliosWindow = new();
		private readonly MyTradesWindow _myTradesWindow = new();
		private readonly TradesWindow _tradesWindow = new();
		private readonly OrdersLogWindow _orderLogWindow = new();
		private readonly NewsWindow _newsWindow = new();
		private readonly Level1Window _level1Window = new();

		public Connector Connector { get; private set; }

		private bool _isConnected;

		private readonly string _defaultDataPath = "Data";
		private readonly string _settingsFile;

		public MainPanel()
		{
			InitializeComponent();

			_ordersWindow.MakeHideable();
			_myTradesWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();
			_portfoliosWindow.MakeHideable();
			_orderLogWindow.MakeHideable();
			_newsWindow.MakeHideable();
			_level1Window.MakeHideable();

			_defaultDataPath = _defaultDataPath.ToFullPath();

			_settingsFile = Path.Combine(_defaultDataPath, $"connection{Paths.DefaultSettingsExt}");
		}

		public event Func<string, Connector> CreateConnector;

		private void MainPanel_OnLoaded(object sender, RoutedEventArgs e)
		{
			var logManager = new LogManager();
			logManager.Listeners.Add(new FileLogListener { LogDirectory = Path.Combine(_defaultDataPath, "Logs") });
			logManager.Listeners.Add(new GuiLogListener(Monitor));

			Connector = CreateConnector?.Invoke(_defaultDataPath) ?? new Connector();
			logManager.Sources.Add(Connector);

			InitConnector();
		}

		public void Close()
		{
			_ordersWindow.DeleteHideable();
			_myTradesWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			_portfoliosWindow.DeleteHideable();
			_orderLogWindow.DeleteHideable();
			_newsWindow.DeleteHideable();
			_level1Window.DeleteHideable();

			_securitiesWindow.Close();
			_tradesWindow.Close();
			_myTradesWindow.Close();
			_ordersWindow.Close();
			_portfoliosWindow.Close();
			_orderLogWindow.Close();
			_newsWindow.Close();
			_level1Window.Close();

			Connector.Dispose();
		}

		private void InitConnector()
		{
			// subscribe on connection successfully event
			Connector.Connected += () =>
			{
				this.GuiAsync(() => ChangeConnectStatus(true));

				if (Connector.Adapter.IsMarketDataTypeSupported(DataType.News) && !Connector.Adapter.IsSecurityNewsOnly)
				{
					if (Connector.Subscriptions.All(s => s.DataType != DataType.News))
						Connector.SubscribeNews();
				}
			};

			// subscribe on connection error event
			Connector.ConnectionError += error => this.GuiAsync(() =>
			{
				ChangeConnectStatus(false);
				MessageBox.Show(this.GetWindow(), error.ToString(), LocalizedStrings.Str2959);
			});

			Connector.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

			// subscribe on error event
			//Connector.Error += error =>
			//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// subscribe on error of market data subscription event
			Connector.MarketDataSubscriptionFailed += (security, msg, error) =>
				this.GuiAsync(() => MessageBox.Show(this.GetWindow(), error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType2, security)));

			Connector.SecurityReceived += (s, sec) => _securitiesWindow.SecurityPicker.Securities.Add(sec);
			Connector.TickTradeReceived += (s, t) => _tradesWindow.TradeGrid.Trades.Add(t);
			Connector.OrderLogItemReceived += (s, ol) => _orderLogWindow.OrderLogGrid.LogItems.Add(ol);
			Connector.Level1Received += (s, l) => _level1Window.Level1Grid.Messages.Add(l);

			Connector.NewOrder += Connector_OnNewOrder;
			Connector.OrderChanged += Connector_OnOrderChanged;
			Connector.OrderEdited += Connector_OnOrderEdited;

			Connector.NewMyTrade += _myTradesWindow.TradeGrid.Trades.Add;

			Connector.PositionReceived += (sub, p) => _portfoliosWindow.PortfolioGrid.Positions.TryAdd(p);

			// subscribe on error of order registration event
			Connector.OrderRegisterFailed += Connector_OnOrderRegisterFailed;
			// subscribe on error of order cancelling event
			Connector.OrderCancelFailed += Connector_OnOrderCancelFailed;
			// subscribe on error of order edition event
			Connector.OrderEditFailed += Connector_OnOrderEditFailed;

			// set market data provider
			_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;

			// set news provider
			_newsWindow.NewsPanel.NewsProvider = Connector;

			Connector.LookupTimeFramesResult += (message, timeFrames, error) =>
			{
				if (error == null)
					this.GuiAsync(() => _securitiesWindow.UpdateTimeFrames(timeFrames));
			};

			var nativeIdStorage = ServicesRegistry.TryNativeIdStorage;

			if (nativeIdStorage != null)
			{
				Connector.Adapter.NativeIdStorage = nativeIdStorage;

				try
				{
					nativeIdStorage.Init();
				}
				catch (Exception ex)
				{
					MessageBox.Show(this.GetWindow(), ex.ToString());
				}
			}

			if (Connector.StorageAdapter != null)
			{
				LoggingHelper.DoWithLog(ServicesRegistry.EntityRegistry.Init);
				LoggingHelper.DoWithLog(ServicesRegistry.ExchangeInfoProvider.Init);

				Connector.Adapter.StorageSettings.DaysLoad = TimeSpan.FromDays(3);
				Connector.Adapter.StorageSettings.Mode = StorageModes.Snapshot;
				Connector.LookupAll();

				Connector.SnapshotRegistry.Init();
			}

			ConfigManager.RegisterService<IMessageAdapterProvider>(new FullInMemoryMessageAdapterProvider(Connector.Adapter.InnerAdapters));

			try
			{
				if (_settingsFile.IsConfigExists())
				{
					var ctx = new ContinueOnExceptionContext();
					ctx.Error += ex => ex.LogError();

					using (ctx.ToScope())
						Connector.LoadIfNotNull(_settingsFile.Deserialize<SettingsStorage>());
				}
			}
			catch
			{
			}
		}

		private void Connector_OnNewOrder(Order order)
		{
			_ordersWindow.OrderGrid.Orders.Add(order);
			_securitiesWindow.ProcessOrder(order);
		}

		private void Connector_OnOrderChanged(Order order)
		{
			_securitiesWindow.ProcessOrder(order);
		}

		private void Connector_OnOrderEdited(long transactionId, Order order)
		{
			_securitiesWindow.ProcessOrder(order);
		}

		private void Connector_OnOrderRegisterFailed(OrderFail fail)
		{
			_ordersWindow.OrderGrid.AddRegistrationFail(fail);
			_securitiesWindow.ProcessOrderFail(fail);
		}

		private void Connector_OnOrderEditFailed(long transactionId, OrderFail fail)
		{
			_securitiesWindow.ProcessOrderFail(fail);
		}

		private void Connector_OnOrderCancelFailed(OrderFail fail)
		{
			_securitiesWindow.ProcessOrderFail(fail);

			this.GuiAsync(() =>
			{
				MessageBox.Show(this.GetWindow(), fail.Error.ToString(), LocalizedStrings.Str153);
			});
		}

		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			if (Connector.Configure(this.GetWindow()))
				Connector.Save().Serialize(_settingsFile);
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

		private void ThemeSwitchClick(object sender, RoutedEventArgs e)
		{
			ThemeExtensions.Invert();
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

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}

		private void ShowMyTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_myTradesWindow);
		}

		private void ShowOrderLogClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_orderLogWindow);
		}

		private void ShowNewsClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_newsWindow);
		}

		private void ShowLevel1Click(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_level1Window);
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