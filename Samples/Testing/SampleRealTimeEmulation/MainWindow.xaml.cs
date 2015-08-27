namespace SampleRealTimeEmulation
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using System.Net;
	using System.Security;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.IQFeed;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.SmartCom;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private bool _isConnected;
		private CandleManager _candleManager;
		private RealTimeEmulationTrader<IMessageAdapter> _connector;
		private readonly ChartCandleElement _candlesElem;
		private readonly LogManager _logManager;
		private Security _security;
		private readonly SynchronizedList<Candle> _buffer = new SynchronizedList<Candle>(); 

		public MainWindow()
		{
			InitializeComponent();

			_logManager = new LogManager();
			_logManager.Listeners.Add(new GuiLogListener(Log));

			var area = new ChartArea();
			Chart.Areas.Add(area);

			_candlesElem = new ChartCandleElement();
			area.Elements.Add(_candlesElem);

			GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(ProcessCandles);

			Level1AddressCtrl.Text = IQFeedAddresses.DefaultLevel1Address.To<string>();
			Level2AddressCtrl.Text = IQFeedAddresses.DefaultLevel2Address.To<string>();
			LookupAddressCtrl.Text = IQFeedAddresses.DefaultLookupAddress.To<string>();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (_connector != null)
				_connector.Dispose();

			base.OnClosing(e);
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (_connector == null)
				{
					if (SmartCom.IsChecked == true)
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

						// create real-time emu connector
						_connector = new RealTimeEmulationTrader<IMessageAdapter>(new SmartComMessageAdapter(new MillisecondIncrementalIdGenerator())
						{
							Login = Login.Text,
							Password = Password.Password.To<SecureString>(),
							Address = Address.SelectedAddress
						});
					}
					else
					{
						// create real-time emu connector
						_connector = new RealTimeEmulationTrader<IMessageAdapter>(new IQFeedMarketDataMessageAdapter(new MillisecondIncrementalIdGenerator())
						{
							Level1Address = Level1AddressCtrl.Text.To<EndPoint>(),
							Level2Address = Level2AddressCtrl.Text.To<EndPoint>(),
							LookupAddress = LookupAddressCtrl.Text.To<EndPoint>(),
						});
					}

					SecurityEditor.SecurityProvider = new FilterableSecurityProvider(_connector);

					_candleManager = new CandleManager(_connector);

					_logManager.Sources.Add(_connector);
					
					// clear password for security reason
					//Password.Clear();

					// subscribe on connection successfully event
					_connector.Connected += () =>
					{
						// set flag (connection is established)
						_isConnected = true;

						// update gui labels
						this.GuiAsync(() =>
						{
							ChangeConnectStatus(true);
							ConnectBtn.IsEnabled = false;
						});
					};

					// subscribe on connection error event
					_connector.ConnectionError += error => this.GuiAsync(() =>
					{
						// update gui labels
						ChangeConnectStatus(false);

						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);
					});

					_connector.NewMarketDepths += OnDepths;
					_connector.MarketDepthsChanged += OnDepths;

					_connector.NewPortfolios += PortfolioGrid.Portfolios.AddRange;
					_connector.NewPositions += PortfolioGrid.Positions.AddRange;

					_connector.NewOrders += OrderGrid.Orders.AddRange;
					_connector.NewMyTrades += TradeGrid.Trades.AddRange;

					// subscribe on error of order registration event
					_connector.OrdersRegisterFailed += OrdersFailed;

					_candleManager.Processing += (s, candle) =>
					{
						if (candle.State == CandleStates.Finished)
							_buffer.Add(candle);
					};

					// subscribe on error event
					_connector.Error += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// subscribe on error of market data subscription event
					_connector.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));
				}

				_connector.Connect();
			}
			else
			{
				_connector.Disconnect();
			}
		}

		private void OnDepths(IEnumerable<MarketDepth> depths)
		{
			if (_security == null)
				return;

			var depth = depths.FirstOrDefault(d => d.Security == _security);

			if (depth == null)
				return;

			DepthControl.UpdateDepth(depth);
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
			Find.IsEnabled = _isConnected;
		}

		private void ProcessCandles()
		{
			foreach (var candle in _buffer.SyncGet(c => c.CopyAndClear()))
				Chart.Draw(_candlesElem, candle);
		}

		private void SecurityEditor_OnSecuritySelected()
		{
			_security = SecurityEditor.SelectedSecurity;

			Chart.Reset(new[] { _candlesElem });

			_connector.RegisterMarketDepth(_security);
			_connector.RegisterTrades(_security);

			_candleManager.Start(new CandleSeries(typeof(TimeFrameCandle), _security, TimeSpan.FromMinutes(1)));
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
				Connector = _connector,
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
				Connector = _connector,
				Order = order.ReRegisterClone(newVolume: order.Balance),
			};

			if (window.ShowModal(this))
			{
				_connector.ReRegisterOrder(order, window.Order);
			}
		}

		private void FindClick(object sender, RoutedEventArgs e)
		{
			var wnd = new FindSecurityWindow();

			if (!wnd.ShowModal(this))
				return;

			_connector.LookupSecurities(wnd.Criteria);
		}
	}
}