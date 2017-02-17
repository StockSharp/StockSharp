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
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.IO;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

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

	public partial class MainWindow
	{
		private readonly SynchronizedList<Candle> _buffer = new SynchronizedList<Candle>();
		private readonly ChartCandleElement _candlesElem;
		private readonly LogManager _logManager;
		private CandleManager _candleManager;
		private CandleSeries _candleSeries;
		private RealTimeEmulationTrader<IMessageAdapter> _connector;
		private bool _isConnected;
		private Security _security;
		private CandleSeries _tempCandleSeries; // used to determine if chart settings have changed and new chart is needed

		private const string _settingsFile = "connection.xml";

		private readonly BasketMessageAdapter _realAdapter = new BasketMessageAdapter(new MillisecondIncrementalIdGenerator());

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

			var area = new ChartArea();
			Chart.Areas.Add(area);

			_candlesElem = new ChartCandleElement();
			area.Elements.Add(_candlesElem);

			InitConnector();

			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(ProcessCandles);
		}

		private void InitConnector()
		{
			_connector?.Dispose();

			try
			{
				if (File.Exists(_settingsFile))
					_realAdapter.Load(new XmlSerializer<SettingsStorage>().Deserialize(_settingsFile));

				_realAdapter.InnerAdapters.ForEach(a => a.RemoveTransactionalSupport());
			}
			catch
			{
			}

			_connector = new RealTimeEmulationTrader<IMessageAdapter>(_realAdapter);
			_logManager.Sources.Add(_connector);

			_connector.EmulationAdapter.Emulator.Settings.TimeZone = TimeHelper.Est;
			_connector.EmulationAdapter.Emulator.Settings.ConvertTime = true;

			SecurityPicker.SecurityProvider = new FilterableSecurityProvider(_connector);

			_candleManager = new CandleManager(_connector);

			_logManager.Sources.Add(_connector);

			// clear password for security reason
			//Password.Clear();

			// subscribe on connection successfully event
			_connector.Connected += () =>
			{
				// update gui labels
				this.GuiAsync(() => { ChangeConnectStatus(true); });
			};

			// subscribe on disconnection event
			_connector.Disconnected += () =>
			{
				// update gui labels
				this.GuiAsync(() => { ChangeConnectStatus(false); });
			};

			// subscribe on connection error event
			_connector.ConnectionError += error => this.GuiAsync(() =>
			{
				// update gui labels
				ChangeConnectStatus(false);

				MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
			});

			_connector.NewMarketDepth += OnDepth;
			_connector.MarketDepthChanged += OnDepth;

			_connector.NewPortfolio += PortfolioGrid.Portfolios.Add;
			_connector.NewPosition += PortfolioGrid.Positions.Add;

			_connector.NewOrder += OrderGrid.Orders.Add;
			_connector.NewMyTrade += TradeGrid.Trades.Add;

			// subscribe on error of order registration event
			_connector.OrderRegisterFailed += OrderGrid.AddRegistrationFail;

			_candleManager.Processing += (s, candle) =>
			{
				if (candle.State == CandleStates.Finished)
					_buffer.Add(candle);
			};

			_connector.MassOrderCancelFailed += (transId, error) =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str716));

			// subscribe on error event
			_connector.Error += error =>
				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

			// subscribe on error of market data subscription event
			_connector.MarketDataSubscriptionFailed += (security, msg, error) =>
			{
				if (error == null)
					return;

				this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(msg.DataType, security)));
			};
		}

		private void CandleSettingsChanged()
		{
			if (_tempCandleSeries == CandleSettingsEditor.Settings || _candleSeries == null)
				return;

			_tempCandleSeries = CandleSettingsEditor.Settings.Clone();
			SecurityPicker_OnSecuritySelected(SecurityPicker.SelectedSecurity);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_connector != null)
				_connector.Dispose();

			base.OnClosing(e);
		}

		private void SettingsClick(object sender, RoutedEventArgs e)
		{
			if (_realAdapter.Configure(this))
				new XmlSerializer<SettingsStorage>().Serialize(_realAdapter.Save(), _settingsFile);

			InitConnector();
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				ConnectBtn.IsEnabled = false;
				_connector.Connect();
			}
			else
			{
				_connector.Disconnect();
			}
		}

		private void OnDepth(MarketDepth depth)
		{
			if (depth.Security != _security)
				return;

			DepthControl.UpdateDepth(depth);
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

			if (_candleSeries != null)
				_candleManager.Stop(_candleSeries); // give back series memory

			_security = security;

			Chart.Reset(new[] { _candlesElem });

			_connector.RegisterMarketDepth(security);
			_connector.RegisterTrades(security);

			_candleSeries = new CandleSeries(CandleSettingsEditor.Settings.CandleType, security, CandleSettingsEditor.Settings.Arg);
			_candleManager.Start(_candleSeries);
		}

		private void NewOrder_OnClick(object sender, RoutedEventArgs e)
		{
			OrderGrid_OrderRegistering();
		}

		private void OrderGrid_OrderRegistering()
		{
			var newOrder = new OrderWindow
			{
				Order = new Order { Security = _security },
				SecurityProvider = _connector,
				MarketDataProvider = _connector,
				Portfolios = new PortfolioDataSource(_connector),
			};

			if (newOrder.ShowModal(this))
				_connector.RegisterOrder(newOrder.Order);
		}

		private void OrderGrid_OnOrderCanceling(IEnumerable<Order> orders)
		{
			orders.ForEach(_connector.CancelOrder);
		}

		private void OrderGrid_OnOrderReRegistering(Order order)
		{
			var window = new OrderWindow
			{
				Title = LocalizedStrings.Str2976Params.Put(order.TransactionId),
				SecurityProvider = _connector,
				MarketDataProvider = _connector,
				Portfolios = new PortfolioDataSource(_connector),
				Order = order.ReRegisterClone(newVolume: order.Balance)
			};

			if (window.ShowModal(this))
				_connector.ReRegisterOrder(order, window.Order);
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			var wnd = new SecurityLookupWindow { Criteria = new Security { Code = "AAPL" } };

			if (!wnd.ShowModal(this))
				return;

			_connector.LookupSecurities(wnd.Criteria);
		}
	}
}