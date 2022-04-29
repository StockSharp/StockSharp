#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleRealTimeEmulation.SampleRealTimeEmulationPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleRealTimeEmulation
{
	using System;
	using System.ComponentModel;
	using System.IO;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;
	using Ecng.Configuration;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Configuration;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Charting;

	public partial class MainWindow
	{
		private readonly SynchronizedList<Candle> _buffer = new();
		private readonly IChartCandleElement _candlesElem;
		private readonly LogManager _logManager;
		private Subscription _candlesSubscription;
		private readonly Connector _realConnector = new();
		private RealTimeEmulationTrader<IMessageAdapter> _emuConnector;
		private bool _isConnected;
		private Security _security;
		private CandleSeries _tempCandleSeries; // used to determine if chart settings have changed and new chart is needed

		private static readonly string _settingsFile = $"connection{Paths.DefaultSettingsExt}";

		private readonly Portfolio _emuPf = Portfolio.CreateSimulator();

		public MainWindow()
		{
			InitializeComponent();

			CandleSettingsEditor.Settings = new CandleSeries
			{
				CandleType = typeof(TimeFrameCandle),
				Arg = TimeSpan.FromMinutes(5),
			};
            CandleSettingsEditor.SettingsChanged += CandleSettingsChanged;

			_logManager = new LogManager();
			_logManager.Listeners.Add(new GuiLogListener(Log));

			_logManager.Sources.Add(_realConnector);

			var area = Chart.CreateArea();
			Chart.Areas.Add(area);

			_candlesElem = Chart.CreateCandleElement();
			area.Elements.Add(_candlesElem);

			InitRealConnector();
			InitEmuConnector();

			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(ProcessCandles);
		}

		private void InitRealConnector()
		{
			_realConnector.NewOrder += OrderGrid.Orders.Add;
			_realConnector.NewMyTrade += TradeGrid.Trades.Add;
			_realConnector.OrderRegisterFailed += OrderGrid.AddRegistrationFail;

			_realConnector.MassOrderCancelFailed += (transId, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

			//_realConnector.Error += error =>
			//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			ConfigManager.RegisterService<IMessageAdapterProvider>(new FullInMemoryMessageAdapterProvider(_realConnector.Adapter.InnerAdapters));

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

				//MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
			});

			_emuConnector.OrderBookReceived += OnDepth;

			_emuConnector.PositionReceived += (sub, p) => PortfolioGrid.Positions.TryAdd(p);

			_emuConnector.NewOrder += OrderGrid.Orders.Add;
			_emuConnector.NewMyTrade += TradeGrid.Trades.Add;

			// subscribe on error of order registration event
			_emuConnector.OrderRegisterFailed += OrderGrid.AddRegistrationFail;

			_emuConnector.CandleReceived += (s, candle) =>
			{
				if (s == _candlesSubscription)
					_buffer.Add(candle);
			};

			_emuConnector.MassOrderCancelFailed += (transId, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

			// subscribe on error event
			//_emuConnector.Error += error =>
			//	this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// subscribe on error of market data subscription event
			_emuConnector.MarketDataSubscriptionFailed += (security, msg, error) =>
			{
				if (error == null)
					return;

				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType2, security)));
			};
		}

		private void CandleSettingsChanged()
		{
			if (_tempCandleSeries == CandleSettingsEditor.Settings || _candlesSubscription == null)
				return;

			_tempCandleSeries = CandleSettingsEditor.Settings.Clone();
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

		private void OnDepth(Subscription subscription, QuoteChangeMessage depth)
		{
			if (depth.SecurityId != _security.ToSecurityId())
				return;

			DepthControl.UpdateDepth(depth, _security);
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
		}

		private void SecurityPicker_OnSecuritySelected(Security security)
		{
			if (security == null)
				return;

			if (_candlesSubscription != null)
				_emuConnector.UnSubscribe(_candlesSubscription); // give back series memory

			_security = security;

			Chart.Reset(new[] { _candlesElem });

			_emuConnector.SubscribeMarketDepth(security);
			_emuConnector.SubscribeTrades(security);
			_emuConnector.SubscribeLevel1(security);

			_candlesSubscription = _emuConnector.SubscribeCandles(new CandleSeries(CandleSettingsEditor.Settings.CandleType, security, CandleSettingsEditor.Settings.Arg), from: DateTimeOffset.UtcNow - TimeSpan.FromDays(10));
		}

		private void NewOrder_OnClick(object sender, RoutedEventArgs e)
		{
			OrderGrid_OrderRegistering();
		}

		private void OrderGrid_OrderRegistering()
		{
			var pfDataSource = new PortfolioDataSource();
			pfDataSource.Add(_emuPf);
			pfDataSource.AddRange(_realConnector.Portfolios);

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
			var pfDataSource = new PortfolioDataSource();
			pfDataSource.Add(order.Portfolio);

			var window = new OrderWindow
			{
				Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
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
				Criteria = new Security { Code = "AAPL" }
			};

			if (!wnd.ShowModal(this))
				return;

			_emuConnector.LookupSecurities(wnd.CriteriaMessage);
		}
	}
}