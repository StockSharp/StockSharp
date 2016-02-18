using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Ecng.Common;
using Ecng.Configuration;
using Ecng.Serialization;
using Ecng.Xaml;
using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Localization;
using StockSharp.Logging;
using StockSharp.Messages;
using StockSharp.Studio.Core.Commands;
using StockSharp.Terminal.Properties;
using MoreLinq;
using StockSharp.Xaml;

namespace StockSharp.Terminal.Services
{
	public class ConnectorService
	{
		private readonly TerminalConnector _connector;
		public const string SETTINGS_FILE = "connection.xml";
		private readonly PortfolioDataSource _portfolioDataSource;

		public event Action<bool> ChangeConnectStatusEvent;

		private void OnErrorEvent(string message)
		{
			new ErrorCommand(message).Process(this);
		}

		public bool IsConnected { get; set; }

		public ConnectorService()
		{
			var entityRegistry = ConfigManager.GetService<IEntityRegistry>();
			var storageRegistry = ConfigManager.GetService<IStorageRegistry>();

			_connector = new TerminalConnector(entityRegistry, storageRegistry);

			_portfolioDataSource = new PortfolioDataSource(_connector);

			ConfigManager.GetService<LogManager>().Sources.Add(_connector);

			RegisterServices();
		}

		private void RegisterServices()
		{
			ConfigManager.RegisterService(_portfolioDataSource);
			ConfigManager.RegisterService<ThreadSafeObservableCollection<Portfolio>>(_portfolioDataSource);
			ConfigManager.RegisterService<IConnector>(_connector);
			ConfigManager.RegisterService<IMarketDataProvider>(_connector);
			ConfigManager.RegisterService<ISecurityProvider>(new FilterableSecurityProvider(_connector));
		}

		public void InitConnector()
		{
			SubscribeConnector();

			try
			{
				if (File.Exists(SETTINGS_FILE))
					_connector.Load(new XmlSerializer<SettingsStorage>().Deserialize(SETTINGS_FILE));
			}
			catch (Exception ex)
			{
				OnErrorEvent($"Ошибка при чтении файла {SETTINGS_FILE}\n{ex}");
				return;
			}

			if (_connector.StorageAdapter == null)
				return;

			if (!File.Exists("StockSharp.db"))
				File.WriteAllBytes("StockSharp.db", Resources.StockSharp);

			_connector.StorageAdapter.DaysLoad = TimeSpan.FromDays(5);
			_connector.StorageAdapter.Load();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();
			cmdSvc.Register<RequestMarketDataCommand>(this, false, cmd => AddExport(cmd.Security, cmd.Type));
			cmdSvc.Register<RefuseMarketDataCommand>(this, false, cmd => RemoveExport(cmd.Security, cmd.Type));

			cmdSvc.Register<LookupSecuritiesCommand>(this, false, LookupSecurities);

			cmdSvc.Register<RegisterOrderCommand>(this, false, cmd =>
			{
				var order = cmd.Order;

				if (order.Security == null)
				{
					OnErrorEvent("Security is not set!");
					return;
				}

				if (order.Portfolio == null)
				{
					OnErrorEvent("Portfolio is not set!");
					return;
				}

				if (order.Type == OrderTypes.Market && !order.Security.Board.IsSupportMarketOrders)
				{
					order.Type = OrderTypes.Limit;
					order.Price = order.Security.GetMarketPrice(_connector, order.Direction) ?? 0;

					if (order.Price == 0)
					{
						OnErrorEvent("Unable to determine market price!");
						return;
					}
				}

				order.ShrinkPrice();

				RaiseOrderCommand(order, OrderActions.Registering);

				_connector.RegisterOrder(order);
			});

			cmdSvc.Register<ReRegisterOrderCommand>(this, false, cmd =>
			{
				if(!CheckOrder(cmd.OldOrder) || !CheckOrder(cmd.NewOrder))
					return;

				_connector.ReRegisterOrder(cmd.OldOrder, cmd.NewOrder);
			});

			cmdSvc.Register<CancelOrderCommand>(this, false, cmd =>
			{
				if (cmd.Mask != null)
					_connector.Orders
						.Where(o => o.Security == cmd.Mask.Security && o.Portfolio == cmd.Mask.Portfolio && o.Price == cmd.Mask.Price && o.State == OrderStates.Active)
						.ForEach(_connector.CancelOrder);
				else
					cmd.Orders.Where(CheckOrder).ForEach(o => _connector.CancelOrder(o));	
			});

			cmdSvc.Register<RevertPositionCommand>(this, false, cmd =>
			{
				if(cmd.Position == null && cmd.Security == null)
					return;

				var pos = cmd.Position ?? _connector.Positions.FirstOrDefault(p => p.Security.Id == cmd.Security.Id);

				if(pos == null || pos.CurrentValue == 0)
					return;

				ClosePosition(pos, pos.CurrentValue.Abs() * 2);
			});

			cmdSvc.Register<ClosePositionCommand>(this, false, cmd =>
			{
				if(cmd.Position == null && cmd.Security == null)
					return;

				var pos = cmd.Position ?? _connector.Positions.FirstOrDefault(p => p.Security.Id == cmd.Security.Id);

				if(pos == null || pos.CurrentValue == 0)
					return;

				ClosePosition(pos, pos.CurrentValue.Abs());
			});

			cmdSvc.Register<CancelAllOrdersCommand>(this, false, cmd => _connector.Orders.Where(o => o.State == OrderStates.Active).ForEach(o => _connector.CancelOrder(o)));

			cmdSvc.Register<SubscribeCandleChartCommand>(this, false, cmd => _connector.SubscribeCandles(cmd.Series));
			cmdSvc.Register<UnsubscribeCandleChartCommand>(this, false, cmd => _connector.UnsubscribeCandles(cmd.Series));

//			cmdSvc.Register<SubscribeTradeElementCommand>(this, false, cmd => Subscribe(_tradeElements, cmd.Security, cmd.Element));
//			cmdSvc.Register<UnSubscribeTradeElementCommand>(this, false, cmd => UnSubscribe(_tradeElements, cmd.Element));
//
//			cmdSvc.Register<SubscribeOrderElementCommand>(this, false, cmd => Subscribe(_orderElements, cmd.Security, cmd.Element));
//			cmdSvc.Register<UnSubscribeOrderElementCommand>(this, false, cmd => UnSubscribe(_orderElements, cmd.Element));

			cmdSvc.Register<RequestTradesCommand>(this, false, cmd => new NewTradesCommand(_connector.Trades).Process(this));

			cmdSvc.Register<RequestPortfoliosCommand>(this, false, cmd => _connector.Portfolios.ForEach(p => new PortfolioCommand(p, true).Process(this)));
			cmdSvc.Register<RequestPositionsCommand>(this, false, cmd => _connector.Positions.ForEach(p => new PositionCommand(p.LastChangeTime, p, true).Process(this)));
		}

		private void ClosePosition(Position position, decimal vol)
		{
			if(position.CurrentValue == 0)
				return;

			var side = position.CurrentValue > 0 ? Sides.Sell : Sides.Buy;
			var security = position.Security;
			var supportMarket = position.Portfolio.Board?.IsSupportMarketOrders ?? false;

			var order = new Order
			{
				Security = security,
				Portfolio = position.Portfolio,
				Direction = side,
				Volume = vol,
				Type = supportMarket ? OrderTypes.Market : OrderTypes.Limit,
			};

			if (order.Type == OrderTypes.Limit)
			{
				order.Price = security.GetMarketPrice(_connector, side) ?? 0;

				if (order.Price == 0)
				{
					OnErrorEvent("Unable to determine market price!");
					return;
				}
			}

			_connector.RegisterOrder(order);
		}

		public void Connect()
		{
			_connector.Connect();
		}

		public void Disconnect()
		{
			_connector.Disconnect();
		}

		public void Configure(Window window)
		{
			_connector.Configure(window);
		}

		public SettingsStorage Save()
		{
			return _connector.Save();
		}

		public Connector GetConnector()
		{
			return _connector;
		}

		private void AddExport(Security security, MarketDataTypes type)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if(!IsConnected)
				return;

			if (type != MarketDataTypes.CandleTimeFrame)
				_connector.SubscribeMarketData(security, type);
		}

		private void RemoveExport(Security security, MarketDataTypes type)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if(!IsConnected)
				return;

			if (type != MarketDataTypes.CandleTimeFrame)
				_connector.UnSubscribeMarketData(security, type);
		}

		private void OrdersFailed(IEnumerable<OrderFail> fails, bool register)
		{
			foreach (var fail in fails)
			{
				//OnErrorEvent(fail.Error.ToString(), LocalizedStrings.Str2960);

				new OrderFailCommand(fail, register ? OrderActions.Registering : OrderActions.Canceling).Process(this);
			}
		}

		private void SubscribeConnector()
		{
			_connector.Connected += () => OnConnectionChanged(true);
			_connector.Disconnected += () => OnConnectionChanged(false);
			_connector.Error += error => OnErrorEvent($"{LocalizedStrings.Str2955}\n{error.ToString()}");

			_connector.ConnectionError += error =>
			{
				OnConnectionChanged(false);
				OnErrorEvent($"{LocalizedStrings.Str2959}\n{error.ToString()}");
			};

			_connector.MarketDataSubscriptionFailed += 
				(security, type, error) => OnErrorEvent($"{LocalizedStrings.Str2956Params.Put(type, security)}\n{error.ToString()}");

			_connector.OrdersRegisterFailed += fail => OrdersFailed(fail, true);
			_connector.StopOrdersRegisterFailed += fail => OrdersFailed(fail, true);
			_connector.OrdersCancelFailed += fail => OrdersFailed(fail, false);
			_connector.StopOrdersCancelFailed += fail => OrdersFailed(fail, false);

			_connector.NewTrades += trades => new NewTradesCommand(trades).Process(this);

			_connector.NewOrders += orders => orders.ForEach(o => RaiseOrderCommand(o, OrderActions.Registering));

			_connector.NewMyTrades += trades => new NewMyTradesCommand(trades).Process(this);

			_connector.NewPortfolios += portfolios =>
			{
				GuiDispatcher.GlobalDispatcher.Dispatcher.GuiAsync(() =>
				{
					portfolios.Where(p => !_portfolioDataSource.Contains(p)).ForEach(p => _portfolioDataSource.Add(p));
					portfolios.ForEach(p => new PortfolioCommand(p, true).Process(this));
				});
			};

			//_connector.NewPositions += positions => _portfoliosWindow.PortfolioGrid.Positions.AddRange(positions);
			_connector.NewPositions += positions =>
			{
				positions.ForEach(p => new PositionCommand(p.LastChangeTime, p, true).Process(this));
			};

			_connector.MarketDepthChanged += depth =>
			{
				new UpdateMarketDepthCommand(depth).Process(this);
			};

			_connector.Candles += (series, candles) =>
			{
				new CandleDataCommand(series, candles).Process(this);
			};
		}

		private void OnConnectionChanged(bool isConnected)
		{
			if (isConnected)
				new CandleChartResetCommand().Process(this);

			ChangeConnectStatusEvent?.Invoke(IsConnected = isConnected);
		}

		private void LookupSecurities(LookupSecuritiesCommand cmd)
		{
			if (!IsConnected)
			{
				MessageBox.Show("Not connected!", "Lookup error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			_connector.LookupSecurities(cmd.Criteria);
		}

		private void RaiseOrderCommand(Order order, OrderActions action)
		{
			new OrderCommand(order, action).Process(this);
		}

		private bool CheckOrder(Order o)
		{
			return o.Security != null && o.Portfolio != null;
		}
	}
}
