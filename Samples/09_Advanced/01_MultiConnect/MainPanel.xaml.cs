namespace StockSharp.Samples.Advanced.MultiConnect;

using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Collections.Generic;

using Ecng.Common;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Xaml;
using Ecng.Collections;
using Ecng.Logging;
using Ecng.ComponentModel;

using Nito.AsyncEx;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Localization;
using StockSharp.Messages;
using StockSharp.Xaml;
using StockSharp.Xaml.GridControl;
using StockSharp.Web.Api.Client;
using StockSharp.Web.Api.Interfaces;
using StockSharp.Studio.Controls;

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

		ConfigManager.RegisterService<ILastDirSelector>(new InMemoryLastDirSelector());

		InitWeb();
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

	/// <summary>
	/// For connectors depends on StockSharp WebAPI.
	/// </summary>
	private void InitWeb()
	{
		IApiServiceProvider webApiProvider = new ApiServiceProvider();
		ConfigManager.RegisterService(webApiProvider);

		ICredentialsProvider credProvider = new DefaultCredentialsProvider();
		ConfigManager.RegisterService(credProvider);

		ConfigManager.ServiceFallback += (type, name) =>
		{
			if (type != typeof(IInstrumentInfoService))
				return null;

			var autoLogon = true;

			if (!credProvider.TryLoad(out var credentials))
			{
				//var clientSvc = WebApiServicesRegistry.GetServiceAsAnonymous<IClientService>();
				var (c, a) = this.GuiSync(() => AsyncContext.Run(() => CredentialsWindow.TryShow(this, credentials)));

				if (c is null)
					return null;

				credentials = c;
				autoLogon = a;
			}

			var token = credentials.Token.UnSecure();

			if (token.IsEmpty())
				throw new InvalidOperationException("Token is empty.");

			credentials.Password = default;
			credProvider.Save(credentials, autoLogon);

			return webApiProvider.GetService<IInstrumentInfoService>(token);
		};
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
					Connector.Subscribe(new(DataType.News));
			}
		};

		// subscribe on connection error event
		Connector.ConnectionError += error => this.GuiAsync(() =>
		{
			ChangeConnectStatus(false);
			MessageBox.Show(this.GetWindow(), error.ToString(), LocalizedStrings.ErrorConnection);
		});

		Connector.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

		Connector.ConnectionLost += a => Connector.AddErrorLog(LocalizedStrings.ConnectionLost);
		Connector.ConnectionRestored += a => Connector.AddInfoLog(LocalizedStrings.ConnectionRestored);

		// subscribe on error event
		//Connector.Error += error =>
		//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.DataProcessError));

		// subscribe on error of market data subscription event
		Connector.SubscriptionFailed += (sub, error, isSubscribe) =>
			this.GuiAsync(() => MessageBox.Show(this.GetWindow(), error.ToString().Truncate(300), LocalizedStrings.ErrorSubDetails.Put(sub.DataType, sub.SecurityId)));

		Connector.SecurityReceived += (s, sec) => _securitiesWindow.SecurityPicker.Securities.Add(sec);
		Connector.TickTradeReceived += (s, t) => _tradesWindow.TradeGrid.Trades.TryAdd(t);
		Connector.OrderLogReceived += (s, ol) => _orderLogWindow.OrderLogGrid.LogItems.Add(ol);
		Connector.Level1Received += (s, l) => _level1Window.Level1Grid.Messages.Add(l);

		Connector.OrderReceived += Connector_OnOrderReceived;
		Connector.OwnTradeReceived += (s, t) => _myTradesWindow.TradeGrid.Trades.TryAdd(t);
		Connector.PositionReceived += (sub, p) => _portfoliosWindow.PortfolioGrid.Positions.TryAdd(p);

		// subscribe on error of order registration event
		Connector.OrderRegisterFailReceived += (s, f) => Connector_OnOrderRegisterFailed(f);
		// subscribe on error of order cancelling event
		Connector.OrderCancelFailReceived += (s, f) => Connector_OnOrderCancelFailed(f);
		// subscribe on error of order edition event
		Connector.OrderEditFailReceived += (s, f) => Connector_OnOrderEditFailed(f);

		// set market data provider
		_securitiesWindow.SecurityPicker.MarketDataProvider = Connector;

		// set news provider
		_newsWindow.NewsPanel.SubscriptionProvider = Connector;

		var timeFrames = new HashSet<TimeSpan>();
		Connector.DataTypeReceived += (s, i) =>
		{
			if (i.IsTFCandles && i.Arg is TimeSpan tf)
				timeFrames.Add(tf);
		};
		Connector.SubscriptionStopped += (s, error) =>
		{
			if (error == null && s.DataType == DataType.DataTypeInfo)
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

			//Connector.Adapter.StorageSettings.DaysLoad = TimeSpan.FromDays(3);
			Connector.Adapter.StorageSettings.Mode = StorageModes.Snapshot;
			Connector.LookupAll();

			Connector.SnapshotRegistry.Init();
		}

		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(Connector.Adapter.InnerAdapters));

		// for show mini chart in SecurityGrid
		_securitiesWindow.SecurityPicker.PriceChartDataProvider = new PriceChartDataProvider(Connector);

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

	private void Connector_OnOrderReceived(Subscription subscription, Order order)
	{
		_ordersWindow.OrderGrid.Orders.TryAdd(order);
		_securitiesWindow.ProcessOrder(order);
	}

	private void Connector_OnOrderRegisterFailed(OrderFail fail)
	{
		_ordersWindow.OrderGrid.AddRegistrationFail(fail);
		_securitiesWindow.ProcessOrderFail(fail);
	}

	private void Connector_OnOrderEditFailed(OrderFail fail)
	{
		_securitiesWindow.ProcessOrderFail(fail);
	}

	private void Connector_OnOrderCancelFailed(OrderFail fail)
	{
		_securitiesWindow.ProcessOrderFail(fail);

		this.GuiAsync(() =>
		{
			MessageBox.Show(this.GetWindow(), fail.Error.ToString(), LocalizedStrings.OrderError);
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