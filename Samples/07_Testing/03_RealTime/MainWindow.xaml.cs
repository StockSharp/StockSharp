namespace StockSharp.Samples.Testing.RealTime;

using System;
using System.ComponentModel;
using System.Windows;
using System.Collections.Generic;

using Ecng.Collections;
using Ecng.Common;
using Ecng.Serialization;
using Ecng.Xaml;
using Ecng.Configuration;
using Ecng.Logging;

using DevExpress.Xpf.Editors;

using StockSharp.Algo;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Localization;
using StockSharp.Messages;
using StockSharp.Xaml;
using StockSharp.Charting;

public partial class MainWindow
{
	private readonly SynchronizedList<ICandleMessage> _buffer = new();
	private readonly SynchronizedList<Order> _bufferOrders = new();
	private readonly HashSet<Order> _fistTimeOrders = new();
	private readonly IChartCandleElement _candlesElem;
	private readonly IChartActiveOrdersElement _ordersElem;
	private readonly LogManager _logManager;
	private Subscription _candlesSubscription;
	private readonly Connector _realConnector = new();
	private RealTimeEmulationTrader<IMessageAdapter> _emuConnector;
	private bool _isConnected;
	private Security _security;
	private SecurityId _securityId;
	private DataType _tempCandleSub; // used to determine if chart settings have changed and new chart is needed

	private static readonly string _settingsFile = $"connection{Paths.DefaultSettingsExt}";

	private readonly Portfolio _emuPf = Portfolio.CreateSimulator();

	public MainWindow()
	{
		InitializeComponent();

		CandleDataTypeEdit.DataType = TimeSpan.FromMinutes(5).TimeFrame();
		CandleDataTypeEdit.EditValueChanged += CandleSettingsChanged;

		_logManager = new LogManager();
		_logManager.Listeners.Add(new GuiLogListener(Log));

		_logManager.Sources.Add(_realConnector);

		var area = Chart.AddArea();

		_candlesElem = Chart.CreateCandleElement();
		area.Elements.Add(_candlesElem);

		_ordersElem = Chart.CreateActiveOrdersElement();
		area.Elements.Add(_ordersElem);

		InitRealConnector();
		InitEmuConnector();

		Chart.OrderCreationMode = true;
		Chart.OrderSettings.Portfolio = _emuPf;

		GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(ProcessCandles);
	}

	private void InitRealConnector()
	{
		_realConnector.OrderReceived += (s, o) => OrderGrid.Orders.TryAdd(o);
		_realConnector.OwnTradeReceived += (s, t) => TradeGrid.Trades.TryAdd(t);
		_realConnector.OrderRegisterFailReceived += (s, f) => OrderGrid.AddRegistrationFail(f);

		_realConnector.MassOrderCancelFailed += (transId, error) =>
			this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.ErrorCancelling));

		_realConnector.OrderBookReceived += OnRealDepth;

		//_realConnector.Error += error =>
		//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.DataProcessError));

		ConfigManager.RegisterService<IMessageAdapterProvider>(new InMemoryMessageAdapterProvider(_realConnector.Adapter.InnerAdapters));

		try
		{
			if (_settingsFile.IsConfigExists())
			{
				var ctx = new ContinueOnExceptionContext();
				ctx.Error += ex => ex.LogError();

				using (ctx.ToScope())
					_realConnector.LoadIfNotNull(_settingsFile.Deserialize<SettingsStorage>());
			}
		}
		catch
		{
		}

		SecurityPicker.SecurityProvider = new FilterableSecurityProvider(_realConnector);
	}

	private void InitEmuConnector()
	{
		if (_emuConnector != null)
		{
			_emuConnector.Dispose();
			_logManager.Sources.Remove(_emuConnector);
		}

		_emuConnector = new RealTimeEmulationTrader<IMessageAdapter>(_realConnector.Adapter, _realConnector, _emuPf, false);
		_logManager.Sources.Add(_emuConnector);

		var settings = _emuConnector.EmulationAdapter.Emulator.Settings;
		settings.TimeZone = TimeHelper.Est;
		settings.ConvertTime = true;

		SecurityPicker.MarketDataProvider = _emuConnector;

		// subscribe on connection successfully event
		_emuConnector.Connected += () =>
		{
			// update gui labels
			this.GuiAsync(() => { ChangeConnectStatus(true); });
		};

		// subscribe on disconnection event
		_emuConnector.Disconnected += () =>
		{
			// update gui labels
			this.GuiAsync(() => { ChangeConnectStatus(false); });
		};

		// subscribe on connection error event
		_emuConnector.ConnectionError += error => this.GuiAsync(() =>
		{
			// update gui labels
			ChangeConnectStatus(false);

			//MessageBox.Show(this, error.ToString(), LocalizedStrings.ErrorConnection);
		});

		_emuConnector.OrderBookReceived += OnDepth;

		_emuConnector.PositionReceived += (sub, p) => PortfolioGrid.Positions.TryAdd(p);

		_emuConnector.OwnTradeReceived += (s, t) => TradeGrid.Trades.TryAdd(t);
		_emuConnector.OrderReceived += (s, o) =>
		{
			if (!_fistTimeOrders.Add(o))
				return;

			_bufferOrders.Add(o);
			OrderGrid.Orders.Add(o);
		};

		// subscribe on error of order registration event
		_emuConnector.OrderRegisterFailReceived += (s, f) => OrderGrid.AddRegistrationFail(f);

		_emuConnector.CandleReceived += (s, candle) =>
		{
			if (s == _candlesSubscription)
				_buffer.Add(candle);
		};

		_emuConnector.MassOrderCancelFailed += (transId, error) =>
			this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.ErrorCancelling));

		// subscribe on error event
		//_emuConnector.Error += error =>
		//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.DataProcessError));

		// subscribe on error of market data subscription event
		_emuConnector.SubscriptionFailed += (sub, error, isSubscribe) =>
		{
			if (error == null)
				return;

			this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.ErrorSubDetails.Put(sub.DataType, sub.SecurityId)));
		};
	}

	private void CandleSettingsChanged(object sender, EditValueChangedEventArgs e)
	{
		if (_tempCandleSub == CandleDataTypeEdit.DataType || _candlesSubscription == null)
			return;

		_tempCandleSub = CandleDataTypeEdit.DataType.Clone();
		SecurityPicker_OnSecuritySelected(SecurityPicker.SelectedSecurity);
	}

	protected override void OnClosing(CancelEventArgs e)
	{
		_realConnector?.Disconnect();
		_emuConnector?.Disconnect();

		base.OnClosing(e);
	}

	private void SettingsClick(object sender, RoutedEventArgs e)
	{
		if (!_realConnector.Configure(this))
			return;

		_realConnector.Save().Serialize(_settingsFile);
		InitEmuConnector();
	}

	private void ConnectClick(object sender, RoutedEventArgs e)
	{
		if (!_isConnected)
		{
			ConnectBtn.IsEnabled = false;
			_realConnector.Connect();
			_emuConnector.Connect();
		}
		else
		{
			_realConnector.Disconnect();
			_emuConnector.Disconnect();
		}
	}

	private void OnDepth(Subscription subscription, IOrderBookMessage depth)
	{
		if (depth.SecurityId != _securityId)
			return;

		DepthControl.UpdateDepth(depth, _security);
	}

	private void OnRealDepth(Subscription subscription, IOrderBookMessage depth)
	{
		if (depth.SecurityId != _securityId)
			return;

		RealDepthControl.UpdateDepth(depth, _security);
	}

	private void ChangeConnectStatus(bool isConnected)
	{
		// set flag (connection is established or not)
		_isConnected = isConnected;

		ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		ConnectBtn.IsEnabled = true;

		Find.IsEnabled = _isConnected;
	}

	private void ProcessCandles()
	{
		foreach (var candle in _buffer.SyncGet(c => c.CopyAndClear()))
			Chart.Draw(_candlesElem, candle);

		foreach (var order in _bufferOrders.SyncGet(c => c.CopyAndClear()))
			Chart.Draw(Chart.CreateData().Add(_ordersElem, order));
	}

	private void SecurityPicker_OnSecuritySelected(Security security)
	{
		if (security == null)
			return;

		if (_candlesSubscription != null)
			_emuConnector.UnSubscribe(_candlesSubscription); // give back series memory

		_security = security;
		_securityId = security.ToSecurityId();

		Chart.Reset(new[] { (IChartElement)_candlesElem, _ordersElem });

		Chart.OrderSettings.Security = security;

		_emuConnector.Subscribe(new(DataType.MarketDepth, security));
		_emuConnector.Subscribe(new(DataType.Ticks, security));
		_emuConnector.Subscribe(new(DataType.Level1, security));

		_realConnector.Subscribe(new(DataType.MarketDepth, security));
		
		_candlesSubscription = new(CandleDataTypeEdit.DataType, security)
		{
			From = DateTimeOffset.UtcNow - TimeSpan.FromDays(10),
		};
		_emuConnector.Subscribe(_candlesSubscription);
	}

	private void NewOrder_OnClick(object sender, RoutedEventArgs e)
	{
		OrderGrid_OrderRegistering();
	}

	private void OrderGrid_OrderRegistering()
	{
		var pfDataSource = new PortfolioDataSource(_emuConnector);
		pfDataSource.Add(_emuPf);

		foreach (var pf in _realConnector.Portfolios)
			pfDataSource.Add(pf);

		var newOrder = new OrderWindow
		{
			Order = new Order { Security = _security },
			SecurityProvider = _emuConnector,
			MarketDataProvider = _emuConnector,
			Portfolios = pfDataSource,
		};

		if (newOrder.ShowModal(this))
		{
			var order = newOrder.Order;

			if (order.Portfolio == _emuPf)
				_emuConnector.RegisterOrder(order);
			else
				_realConnector.RegisterOrder(order);
		}
	}

	private void OrderGrid_OnOrderCanceling(Order order)
	{
		if (order.Portfolio == _emuPf)
			_emuConnector.CancelOrder(order);
		else
			_realConnector.CancelOrder(order);
	}

	private void OrderGrid_OnOrderReRegistering(Order order)
	{
		var pfDataSource = new PortfolioDataSource(_emuConnector);
		pfDataSource.Add(order.Portfolio);

		var window = new OrderWindow
		{
			Title = LocalizedStrings.ReregistrationOfOrder.Put(order.TransactionId),
			SecurityProvider = _emuConnector,
			MarketDataProvider = _emuConnector,
			Portfolios = pfDataSource,
			Order = order.ReRegisterClone(newVolume: order.Balance)
		};

		if (window.ShowModal(this))
		{
			if (order.Portfolio == _emuPf)
				_emuConnector.ReRegisterOrder(order, window.Order);
			else
				_realConnector.ReRegisterOrder(order, window.Order);
		}
	}

	private void FindClick(object sender, RoutedEventArgs e)
	{
		var wnd = new SecurityLookupWindow
		{
			ShowAllOption = _emuConnector.MarketDataAdapter.IsSupportSecuritiesLookupAll(),
			CriteriaMessage = new() { SecurityId = new() { SecurityCode = "AAPL" } }
		};

		if (!wnd.ShowModal(this))
			return;

		_emuConnector.Subscribe(new(wnd.CriteriaMessage));
	}

	private void Chart_RegisterOrder(IChartArea area, Order order)
	{
		_emuConnector.RegisterOrder(order);
	}

	private void Chart_CancelOrder(Order order)
	{
		_emuConnector.CancelOrder(order);
	}

	private void Chart_MoveOrder(Order order, decimal newPrice)
	{
		_emuConnector.ReRegisterOrder(order, newPrice, order.Balance);
	}
}