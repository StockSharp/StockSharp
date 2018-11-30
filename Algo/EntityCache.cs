#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: EntityCache.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class EntityCache
	{
		private static readonly MemoryStatisticsValue<Trade> _tradeStat = new MemoryStatisticsValue<Trade>(LocalizedStrings.Ticks);

		public class OrderChangeInfo
		{
			public static OrderChangeInfo Create(Order order, bool isNew, bool isChanged)
			{
				if (order == null)
					throw new ArgumentNullException(nameof(order));

				return new OrderChangeInfo
				{
					Order = order,
					IsNew = isNew,
					IsChanged = isChanged,
				};
			}

			public Order Order { get; private set; }
			public bool IsNew { get; private set; }
			public bool IsChanged { get; private set; }
		}

		private sealed class OrderInfo
		{
			private bool _raiseNewOrder;

			public OrderInfo(Order order, bool raiseNewOrder = true)
			{
				Order = order ?? throw new ArgumentNullException(nameof(order));
				_raiseNewOrder = raiseNewOrder;
			}

			public Order Order { get; }

			public OrderChangeInfo ApplyChanges(ExecutionMessage message, bool isCancel)
			{
				var order = Order;

				OrderChangeInfo retVal;

				if (order.State == OrderStates.Done)
				{
					// данные о заявке могут приходить из маркет-дата и транзакционного адаптеров
					retVal = OrderChangeInfo.Create(order, _raiseNewOrder, false);
					_raiseNewOrder = false;
					return retVal;
					//throw new InvalidOperationException("Изменение заявки в состоянии Done невозможно.");
				}
				else if (order.State == OrderStates.Failed)
				{
					// some adapters can resend order's info

					//throw new InvalidOperationException();
					return null;
				}

				var isPending = order.State == OrderStates.Pending;

				if (message.OrderId != null)
					order.Id = message.OrderId.Value;

				if (!message.OrderStringId.IsEmpty())
					order.StringId = message.OrderStringId;

				if (!message.OrderBoardId.IsEmpty())
					order.BoardId = message.OrderBoardId;

				//// некоторые коннекторы не транслируют при отмене отмененный объем
				//// esper. при перерегистрации заявок необходимо обновлять баланс
				//if (message.Balance > 0 || !isCancelled || isReRegisterCancelled)
				//{
				//	// BTCE коннектор не транслирует баланс заявки
				//	if (!(message.OrderState == OrderStates.Active && message.Balance == 0))
				//		order.Balance = message.Balance;
				//}

				if (message.Balance != null)
					order.Balance = message.Balance.Value;

				// IB коннектор не транслирует состояние заявки в одном из своих сообщений
				if (message.OrderState != null)
					order.State = order.State.CheckModification(message.OrderState.Value);

				if (order.Time == DateTimeOffset.MinValue)
					order.Time = message.ServerTime;

				// для новых заявок используем серверное время, 
				// т.к. заявка получена первый раз и не менялась
				// ServerTime для заявки - это время регистрации
				order.LastChangeTime = _raiseNewOrder ? message.ServerTime : message.LocalTime;
				order.LocalTime = message.LocalTime;

				//нулевой объем может быть при перерегистрации
				if (order.Volume == 0 && message.OrderVolume != null)
					order.Volume = message.OrderVolume.Value;

				if (message.Commission != null)
					order.Commission = message.Commission;

				if (!message.CommissionCurrency.IsEmpty())
					order.CommissionCurrency = message.CommissionCurrency;

				if (message.TimeInForce != null)
					order.TimeInForce = message.TimeInForce.Value;

				if (message.Latency != null)
				{
					if (isCancel)
						order.LatencyCancellation = message.Latency.Value;
					else if (isPending)
					{
						if (order.State != OrderStates.Pending)
							order.LatencyRegistration = message.Latency.Value;
					}
				}

				message.CopyExtensionInfo(order);

				retVal = OrderChangeInfo.Create(order, _raiseNewOrder, true);
				_raiseNewOrder = false;
				return retVal;
			}
		}

		private class SecurityData
		{
			public readonly CachedSynchronizedDictionary<Tuple<long, long>, MyTrade> MyTrades = new CachedSynchronizedDictionary<Tuple<long, long>, MyTrade>();
			public readonly CachedSynchronizedDictionary<Tuple<long, bool, bool>, OrderInfo> Orders = new CachedSynchronizedDictionary<Tuple<long, bool, bool>, OrderInfo>();

			public readonly SynchronizedDictionary<long, Trade> TradesById = new SynchronizedDictionary<long, Trade>();
			public readonly SynchronizedDictionary<string, Trade> TradesByStringId = new SynchronizedDictionary<string, Trade>(StringComparer.InvariantCultureIgnoreCase);
			public readonly SynchronizedList<Trade> Trades = new SynchronizedList<Trade>();

			public readonly SynchronizedDictionary<long, Order> OrdersById = new SynchronizedDictionary<long, Order>();
			public readonly SynchronizedDictionary<string, Order> OrdersByStringId = new SynchronizedDictionary<string, Order>(StringComparer.InvariantCultureIgnoreCase);
		}

		private readonly SynchronizedDictionary<Security, SecurityData> _securityData = new SynchronizedDictionary<Security, SecurityData>();

		private SecurityData GetData(Security security)
		{
			return _securityData.SafeAdd(security);
		}

		private readonly CachedSynchronizedList<Trade> _trades = new CachedSynchronizedList<Trade>();

		public IEnumerable<Trade> Trades
		{
			get { return _securityData.SyncGet(d => d.SelectMany(p => p.Value.Trades.SyncGet(t => t.ToArray()).Concat(p.Value.TradesById.SyncGet(t => t.Values.ToArray())).Concat(p.Value.TradesByStringId.SyncGet(t => t.Values.ToArray()))).ToArray()); }
		}

		private readonly SynchronizedDictionary<Tuple<long, bool>, Order> _allOrdersByTransactionId = new SynchronizedDictionary<Tuple<long, bool>, Order>();
		private readonly SynchronizedDictionary<long, Order> _allOrdersById = new SynchronizedDictionary<long, Order>();
		private readonly SynchronizedDictionary<string, Order> _allOrdersByStringId = new SynchronizedDictionary<string, Order>(StringComparer.InvariantCultureIgnoreCase);

		private readonly SynchronizedDictionary<string, News> _newsById = new SynchronizedDictionary<string, News>(StringComparer.InvariantCultureIgnoreCase);
		private readonly SynchronizedList<News> _newsWithoutId = new SynchronizedList<News>();

		private readonly CandlesHolder _candlesHolder = new CandlesHolder();

		public IEnumerable<News> News
		{
			get { return _newsWithoutId.SyncGet(t => t.ToArray()).Concat(_newsById.SyncGet(t => t.Values.ToArray())).ToArray(); }
		}

		private int _tradesKeepCount = 100000;

		public int TradesKeepCount
		{
			get => _tradesKeepCount;
			set
			{
				if (_tradesKeepCount == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.NegativeTickCountStorage);

				_tradesKeepCount = value;
				RecycleTrades();
			}
		}

		private int _ordersKeepCount = 1000;

		public int OrdersKeepCount
		{
			get => _ordersKeepCount;
			set
			{
				if (_ordersKeepCount == value)
					return;

				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.NegativeOrderCountStorage);

				_ordersKeepCount = value;
				RecycleOrders();
			}
		}

		private void AddTrade(Trade trade)
		{
			if (TradesKeepCount == 0)
				return;

			_tradeStat.Add(trade);
			_trades.Add(trade);

			RecycleTrades();
		}

		private void AddOrder(Order order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (OrdersKeepCount > 0)
				_orders.Add(order);

			RecycleOrders();
		}

		private readonly CachedSynchronizedDictionary<string, Portfolio> _portfolios = new CachedSynchronizedDictionary<string, Portfolio>();
		private readonly HashSet<long> _orderStatusTransactions = new HashSet<long>();
		private readonly HashSet<long> _massCancelationTransactions = new HashSet<long>();

		private IEntityFactory _entityFactory = new EntityFactory();

		public IEntityFactory EntityFactory
		{
			get => _entityFactory;
			set => _entityFactory = value ?? throw new ArgumentNullException(nameof(value));
		}

		private IExchangeInfoProvider _exchangeInfoProvider = new InMemoryExchangeInfoProvider();

		public IExchangeInfoProvider ExchangeInfoProvider
		{
			get => _exchangeInfoProvider;
			set => _exchangeInfoProvider = value ?? throw new ArgumentNullException(nameof(value));
		}

		private readonly CachedSynchronizedList<Order> _orders = new CachedSynchronizedList<Order>();

		public IEnumerable<Order> Orders => _orders.Cache;

		private readonly CachedSynchronizedList<MyTrade> _myTrades = new CachedSynchronizedList<MyTrade>();

		public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

		public virtual IEnumerable<Portfolio> Portfolios => _portfolios.CachedValues;

		private readonly CachedSynchronizedSet<ExchangeBoard> _exchangeBoards = new CachedSynchronizedSet<ExchangeBoard>();

		public IEnumerable<ExchangeBoard> ExchangeBoards => _exchangeBoards.Cache;

		private readonly CachedSynchronizedDictionary<string, Security> _securities = new CachedSynchronizedDictionary<string, Security>(StringComparer.InvariantCultureIgnoreCase);

		public IEnumerable<Security> Securities => _securities.CachedValues;

		private readonly SynchronizedList<OrderFail> _orderRegisterFails = new SynchronizedList<OrderFail>();

		public IEnumerable<OrderFail> OrderRegisterFails
		{
			get { return _orderRegisterFails.SyncGet(c => c.ToArray()); }
		}

		private readonly SynchronizedList<OrderFail> _orderCancelFails = new SynchronizedList<OrderFail>();

		public IEnumerable<OrderFail> OrderCancelFails
		{
			get { return _orderCancelFails.SyncGet(c => c.ToArray()); }
		}

		private readonly CachedSynchronizedDictionary<Tuple<Portfolio, Security, string, string, TPlusLimits?>, Position> _positions = new CachedSynchronizedDictionary<Tuple<Portfolio, Security, string, string, TPlusLimits?>, Position>();

		public IEnumerable<Position> Positions => _positions.CachedValues;

		public int SecurityCount => _securities.Count;

		public void Clear()
		{
			_securityData.Clear();

			_allOrdersById.Clear();
			_allOrdersByStringId.Clear();
			_allOrdersByTransactionId.Clear();
			_orders.Clear();

			_newsById.Clear();
			_newsWithoutId.Clear();

			_myTrades.Clear();

			_trades.Clear();
			_tradeStat.Clear(true);

			_orderStatusTransactions.Clear();
			_massCancelationTransactions.Clear();

			_exchangeBoards.Clear();
			_securities.Clear();

			_orderCancelFails.Clear();
			_orderRegisterFails.Clear();

			_positions.Clear();

			_candlesHolder.Clear();
		}

		public void AddOrderStatusTransactionId(long transactionId)
		{
			if (!_orderStatusTransactions.Add(transactionId))
				throw new InvalidOperationException();
		}

		public IEnumerable<Order> GetOrders(Security security, OrderStates state)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return GetData(security).Orders.CachedValues.Select(info => info.Order).Filter(state);
		}

		public void AddMassCancelationId(long transactionId)
		{
			if (!_massCancelationTransactions.Add(transactionId))
				throw new InvalidOperationException();
		}

		public bool IsMassCancelation(long transactionId) => _massCancelationTransactions.Contains(transactionId);
		public bool IsOrderStatusRequest(long transactionId) => _orderStatusTransactions.Contains(transactionId);

		public void AddOrderByCancelationId(Order order, long transactionId)
		{
			AddOrderByTransactionId(order, transactionId, true);
		}

		public void AddOrderByRegistrationId(Order order)
		{
			AddOrder(order);
			AddOrderByTransactionId(order, order.TransactionId, false);
		}

		private void AddOrderByTransactionId(Order order, long transactionId, bool isCancel)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			GetData(order.Security).Orders.Add(CreateOrderKey(order.Type, transactionId, isCancel), new OrderInfo(order, !isCancel));
			_allOrdersByTransactionId.Add(Tuple.Create(transactionId, isCancel), order);
		}

		private void UpdateOrderIds(Order order, SecurityData securityData)
		{
			// так как биржевые идентифиаторы могут повторяться, то переписываем старые заявки новыми как наиболее актуальными

			if (order.Id != null)
			{
				securityData.OrdersById[order.Id.Value] = order;
				_allOrdersById[order.Id.Value] = order;
			}

			if (!order.StringId.IsEmpty())
			{
				securityData.OrdersByStringId[order.StringId] = order;
				_allOrdersByStringId[order.StringId] = order;
			}
		}

		public IEnumerable<OrderChangeInfo> ProcessOrderMessage(Order order, Security security, ExecutionMessage message, long transactionId, out Tuple<Portfolio, bool, bool> pfInfo)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.Error != null)
				throw new ArgumentException(LocalizedStrings.Str714Params.PutEx(message));

			var securityData = GetData(security);

			if (transactionId == 0 && message.OrderId == null && message.OrderStringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str719);

			pfInfo = null;

			var orders = securityData.Orders;

			if (transactionId == 0)
			{
				var info = orders.CachedValues.FirstOrDefault(i =>
				{
					if (order != null)
						return i.Order == order;

					if (message.OrderId != null)
						return i.Order.Id == message.OrderId;
					else
						return i.Order.StringId.CompareIgnoreCase(message.OrderStringId);
				});

				if (info == null)
				{
					return null;
					//throw new InvalidOperationException(LocalizedStrings.Str1156Params.Put(orderId.To<string>() ?? orderStringId));
				}

				var orderInfo = info.ApplyChanges(message, false);
				UpdateOrderIds(info.Order, securityData);
				return new[] { orderInfo };
			}
			else
			{
				var cancelKey = CreateOrderKey(message.OrderType, transactionId, true);
				var registerKey = CreateOrderKey(message.OrderType, transactionId, false);

				var cancelledInfo = orders.TryGetValue(cancelKey);
				var registeredInfo = orders.TryGetValue(registerKey);

				// проверяем не отмененная ли заявка пришла
				if (cancelledInfo != null) // && (cancelledOrder.Id == orderId || (!cancelledOrder.StringId.IsEmpty() && cancelledOrder.StringId.CompareIgnoreCase(orderStringId))))
				{
					var cancellationOrder = cancelledInfo.Order;

					if (registeredInfo == null)
					{
						var i = cancelledInfo.ApplyChanges(message, true);
						UpdateOrderIds(cancellationOrder, securityData);
						return new[] { i };
					}

					var retVal = new List<OrderChangeInfo>();
					var orderState = message.OrderState;

					if (orderState != null && cancellationOrder.State != OrderStates.Done && orderState != OrderStates.None && orderState != OrderStates.Pending)
					{
						cancellationOrder.State = cancellationOrder.State.CheckModification(OrderStates.Done);
						
						if (message.Latency != null)
							cancellationOrder.LatencyCancellation = message.Latency.Value;

						retVal.Add(OrderChangeInfo.Create(cancellationOrder, false, true));
					}

					var isCancelOrder = (message.OrderId != null && message.OrderId == cancellationOrder.Id)
						|| (message.OrderStringId != null && message.OrderStringId == cancellationOrder.StringId)
						|| (message.OrderBoardId != null && message.OrderBoardId == cancellationOrder.BoardId);

					var regOrder = registeredInfo.Order;

					if (!isCancelOrder)
					{
						var replacedInfo = registeredInfo.ApplyChanges(message, false);
						UpdateOrderIds(regOrder, securityData);
						retVal.Add(replacedInfo);
					}

					return retVal;
				}

				if (registeredInfo == null)
				{
					var o = EntityFactory.CreateOrder(security, message.OrderType, registerKey.Item1);

					if (o == null)
						throw new InvalidOperationException(LocalizedStrings.Str720Params.Put(registerKey.Item1));

					o.Time = message.ServerTime;
					o.LastChangeTime = message.ServerTime;
					o.Price = message.OrderPrice;
					o.Volume = message.OrderVolume ?? 0;
					o.Direction = message.Side;
					o.Comment = message.Comment;
					o.ExpiryDate = message.ExpiryDate;
					o.Condition = message.Condition;
					o.UserOrderId = message.UserOrderId;
					o.ClientCode = message.ClientCode;
					o.BrokerCode = message.BrokerCode;
					o.IsMarketMaker = message.IsMarketMaker;
					o.IsMargin = message.IsMargin;
					o.Slippage = message.Slippage;

					if (message.PortfolioName.IsEmpty())
						o.Portfolio = _portfolios.FirstOrDefault().Value;
					else
					{
						pfInfo = ProcessPortfolio(message.PortfolioName);
						o.Portfolio = ProcessPortfolio(message.PortfolioName).Item1;
					}

					if (o.ExtensionInfo == null)
						o.ExtensionInfo = new Dictionary<string, object>();

					AddOrder(o);
					_allOrdersByTransactionId.Add(Tuple.Create(transactionId, false), o);

					registeredInfo = new OrderInfo(o);
					orders.Add(registerKey, registeredInfo);
				}

				var orderInfo = registeredInfo.ApplyChanges(message, false);

				if (orderInfo != null)
				{
					UpdateOrderIds(registeredInfo.Order, securityData);
					return new[] { orderInfo };
				}
				else
					return Enumerable.Empty<OrderChangeInfo>();
			}
		}

		public IEnumerable<Tuple<OrderFail, bool>> ProcessOrderFailMessage(Order order, Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var data = GetData(security);

			var orders = new List<Tuple<Order, bool>>();

			if (message.OriginalTransactionId == 0)
				throw new ArgumentOutOfRangeException(nameof(message), message.OriginalTransactionId, LocalizedStrings.Str715);

			var orderType = message.OrderType;

			if (order == null)
			{
				var cancelledOrder = data.Orders.TryGetValue(CreateOrderKey(orderType, message.OriginalTransactionId, true))?.Order;

				if (cancelledOrder == null && orderType == null)
				{
					cancelledOrder = data.Orders.TryGetValue(CreateOrderKey(OrderTypes.Conditional, message.OriginalTransactionId, true))?.Order;

					if (cancelledOrder != null)
						orderType = OrderTypes.Conditional;
				}

				if (cancelledOrder != null /*&& order.Id == message.OrderId*/)
					orders.Add(Tuple.Create(cancelledOrder, true));

				var registeredOrder = data.Orders.TryGetValue(CreateOrderKey(orderType, message.OriginalTransactionId, false))?.Order;

				if (registeredOrder == null && orderType == null)
					registeredOrder = data.Orders.TryGetValue(CreateOrderKey(OrderTypes.Conditional, message.OriginalTransactionId, false))?.Order;

				if (registeredOrder != null)
					orders.Add(Tuple.Create(registeredOrder, false));

				if (cancelledOrder == null && registeredOrder == null)
				{
					if (!message.OrderStringId.IsEmpty())
					{
						order = data.OrdersByStringId.TryGetValue(message.OrderStringId);

						if (order != null)
						{
							var pair = data.Orders.LastOrDefault(p => p.Value.Order == order);

							if (pair.Key != null)
								orders.Add(Tuple.Create(pair.Value.Order, pair.Key.Item3));
						}
					}
				}
			}
			else
			{
				if (data.Orders.ContainsKey(CreateOrderKey(order.Type, message.OriginalTransactionId, true)))
					orders.Add(Tuple.Create(order, true));

				var registeredOrder = data.Orders.TryGetValue(CreateOrderKey(order.Type, message.OriginalTransactionId, false))?.Order;
				if (registeredOrder != null)
					orders.Add(Tuple.Create(registeredOrder, false));
			}

			if (orders.Count == 0)
				return Enumerable.Empty<Tuple<OrderFail, bool>>();

			return orders.Select(t =>
			{
				var o = t.Item1;
				var isCancelTransaction = t.Item2;

				o.LastChangeTime = message.ServerTime;
				o.LocalTime = message.LocalTime;

				if (message.OrderStatus != null)
					o.Status = message.OrderStatus;

				//для ошибок снятия не надо менять состояние заявки
				if (!isCancelTransaction)
					o.State = o.State.CheckModification(OrderStates.Failed);

				if (message.Commission != null)
					o.Commission = message.Commission;

				if (!message.CommissionCurrency.IsEmpty())
					o.CommissionCurrency = message.CommissionCurrency;

				message.CopyExtensionInfo(o);

				var error = message.Error ?? new InvalidOperationException(isCancelTransaction ? LocalizedStrings.Str716 : LocalizedStrings.Str717);

				var fail = EntityFactory.CreateOrderFail(o, error);
				fail.ServerTime = message.ServerTime;
				fail.LocalTime = message.LocalTime;
				return Tuple.Create(fail, isCancelTransaction);
			});
		}

		public Tuple<MyTrade, bool> ProcessMyTradeMessage(Order order, Security security, ExecutionMessage message, long transactionId)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var securityData = GetData(security);

			if (transactionId == 0 && message.OrderId == null && message.OrderStringId.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(message), transactionId, LocalizedStrings.Str715);

			var myTrade = securityData.MyTrades.TryGetValue(Tuple.Create(transactionId, message.TradeId ?? 0));

			if (myTrade != null)
				return Tuple.Create(myTrade, false);

			if (order == null)
			{
				order = GetOrder(security, transactionId, message.OrderId, message.OrderStringId);

				if (order == null)
					return null;
			}

			var trade = message.ToTrade(EntityFactory.CreateTrade(security, message.TradeId, message.TradeStringId));

			var isNew = false;

			myTrade = securityData.MyTrades.SafeAdd(Tuple.Create(order.TransactionId, trade.Id), key =>
			{
				isNew = true;

				var t = EntityFactory.CreateMyTrade(order, trade);

				if (t.ExtensionInfo == null)
					t.ExtensionInfo = new Dictionary<string, object>();

				if (message.Commission != null)
					t.Commission = message.Commission;

				if (!message.CommissionCurrency.IsEmpty())
					t.CommissionCurrency = message.CommissionCurrency;

				if (message.Slippage != null)
					t.Slippage = message.Slippage;

				if (message.PnL != null)
					t.PnL = message.PnL;

				if (message.Position != null)
					t.Position = message.Position;

				message.CopyExtensionInfo(t);

				_myTrades.Add(t);

				return t;
			});

			return Tuple.Create(myTrade, isNew);
		}

		public Tuple<Trade, bool> ProcessTradeMessage(Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var trade = GetTrade(security, message.TradeId, message.TradeStringId, (id, stringId) =>
			{
				var t = message.ToTrade(EntityFactory.CreateTrade(security, id, stringId));
				t.LocalTime = message.LocalTime;
				t.Time = message.ServerTime;
				message.CopyExtensionInfo(t);
				return t;
			});

			return trade;
		}

		public Tuple<News, bool> ProcessNewsMessage(Security security, NewsMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var isNew = false;

			News news;

			if (!message.Id.IsEmpty())
			{
				news = _newsById.SafeAdd(message.Id, key =>
				{
					isNew = true;
					var n = EntityFactory.CreateNews();
					n.Id = key;
					return n;
				});
			}
			else
			{
				isNew = true;

				news = EntityFactory.CreateNews();
				_newsWithoutId.Add(news);
			}

			if (isNew)
			{
				news.ServerTime = message.ServerTime;
				news.LocalTime = message.LocalTime;
			}

			if (!message.Source.IsEmpty())
				news.Source = message.Source;

			if (!message.Headline.IsEmpty())
				news.Headline = message.Headline;

			if (security != null)
				news.Security = security;

			if (!message.Story.IsEmpty())
				news.Story = message.Story;

			if (!message.BoardCode.IsEmpty())
				news.Board = ExchangeInfoProvider.GetOrCreateBoard(message.BoardCode);

			if (message.Url != null)
				news.Url = message.Url;

			message.CopyExtensionInfo(news);

			return Tuple.Create(news, isNew);
		}

		private static Tuple<long, bool, bool> CreateOrderKey(OrderTypes? type, long transactionId, bool isCancel)
		{
			if (transactionId <= 0)
				throw new ArgumentOutOfRangeException(nameof(transactionId), transactionId, LocalizedStrings.Str718);

			return Tuple.Create(transactionId, type == OrderTypes.Conditional, isCancel);
		}

		public Order GetOrder(Security security, long transactionId, long? orderId, string orderStringId, OrderTypes orderType = OrderTypes.Limit, bool isCancel = false)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (transactionId == 0 && orderId == null && orderStringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str719);

			var data = GetData(security);

			Order order = null;

			if (transactionId != 0)
				order = data.Orders.TryGetValue(CreateOrderKey(orderType, transactionId, isCancel))?.Order;

			if (order != null)
				return order;

			if (orderId != null)
				order = data.OrdersById.TryGetValue(orderId.Value);

			if (order != null)
				return order;

			return orderStringId == null ? null : data.OrdersByStringId.TryGetValue(orderStringId);
		}

		public long GetTransactionId(long originalTransactionId)
		{
			// ExecMsg.OriginalTransactionId == OrderStatMsg.TransactionId when orders info requested by OrderStatMsg
			return IsOrderStatusRequest(originalTransactionId) || IsMassCancelation(originalTransactionId) ? 0 : originalTransactionId;
		}

		public Order GetOrder(ExecutionMessage message, out long transactionId)
		{
			transactionId = message.TransactionId;

			if (transactionId == 0)
				transactionId = GetTransactionId(message.OriginalTransactionId);

			if (transactionId == 0)
			{
				return message.OrderId == null ? null : _allOrdersById.TryGetValue(message.OrderId.Value);
				//return null;
			}

			return _allOrdersByTransactionId.TryGetValue(Tuple.Create(transactionId, true)) ?? _allOrdersByTransactionId.TryGetValue(Tuple.Create(transactionId, false));
		}

		public Tuple<Trade, bool> GetTrade(Security security, long? id, string strId, Func<long?, string, Trade> createTrade)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (createTrade == null)
				throw new ArgumentNullException(nameof(createTrade));

			var isNew = false;

			Trade trade;

			if (TradesKeepCount > 0)
			{
				var securityData = GetData(security);

				if (id != null)
				{
					trade = securityData.TradesById.SafeAdd(id.Value, k =>
					{
						isNew = true;

						var t = createTrade(id.Value, strId);
						AddTrade(t);
						return t;
					});
				}
				else if (!strId.IsEmpty())
				{
					trade = securityData.TradesByStringId.SafeAdd(strId, k =>
					{
						isNew = true;

						var t = createTrade(null, strId);
						AddTrade(t);
						return t;
					});
				}
				else
				{
					isNew = true;

					trade = createTrade(null, null);
					AddTrade(trade);
					securityData.Trades.Add(trade);
				}
			}
			else
			{
				isNew = true;
				trade = createTrade(id, strId);
			}

			return Tuple.Create(trade, isNew);
		}

		public Tuple<Portfolio, bool, bool> ProcessPortfolio(string name, Func<Portfolio, bool> changePortfolio = null)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			var portfolio = _portfolios.SafeAdd(name, key =>
			{
				var p = EntityFactory.CreatePortfolio(key);

				if (p == null)
					throw new InvalidOperationException(LocalizedStrings.Str1104Params.Put(name));

				if (p.ExtensionInfo == null)
					p.ExtensionInfo = new Dictionary<string, object>();

				return p;
			}, out var isNew);

			var isChanged = false;
			if (changePortfolio != null)
				isChanged = changePortfolio(portfolio);

			if (isNew)
				return Tuple.Create(portfolio, true, false);

			if (changePortfolio != null && isChanged)
				return Tuple.Create(portfolio, false, true);

			return Tuple.Create(portfolio, false, false);
		}

		public Security GetSecurityById(string id)
		{
			return _securities.TryGetValue(id);
		}

		public Security TryAddSecurity(string id, Func<string, Tuple<string, ExchangeBoard>> idConvert, out bool isNew)
		{
			if (idConvert == null)
				throw new ArgumentNullException(nameof(idConvert));

			return _securities.SafeAdd(id, key =>
			{
				var s = EntityFactory.CreateSecurity(key);

				if (s == null)
					throw new InvalidOperationException(LocalizedStrings.Str1102Params.Put(key));

				var info = idConvert(key);

				var code = info.Item1;
				var board = info.Item2;

				if (s.Board == null)
					s.Board = board;

				if (s.Code.IsEmpty())
					s.Code = code;

				if (s.Name.IsEmpty())
					s.Name = code;

				//if (s.Class.IsEmpty())
				//	s.Class = board.Code;

				return s;
			}, out isNew);
		}

		public Security TryRemoveSecurity(string id)
		{
			lock (_securities.SyncRoot)
			{
				var security = _securities.TryGetValue(id);
				_securities.Remove(id);
				return security;
			}
		}

		public void TryAddBoard(ExchangeBoard board)
		{
			_exchangeBoards.TryAdd(board);
		}

		public void AddRegisterFail(OrderFail fail)
		{
			_orderRegisterFails.Add(fail);
		}

		public void AddCancelFail(OrderFail fail)
		{
			_orderCancelFails.Add(fail);
		}

		public Position TryAddPosition(Portfolio portfolio, Security security, string clientCode, string depoName, TPlusLimits? limitType, string description, out bool isNew)
		{
			isNew = false;
			Position position;

			lock (_positions.SyncRoot)
			{
				if (depoName == null)
					depoName = string.Empty;

				if (clientCode == null)
					clientCode = string.Empty;

				var key = Tuple.Create(portfolio, security, clientCode, depoName, limitType);

				if (!_positions.TryGetValue(key, out position))
				{
					isNew = true;

					position = EntityFactory.CreatePosition(portfolio, security);
					position.DepoName = depoName;
					position.LimitType = limitType;
					position.Description = description;
					position.ClientCode = clientCode;
					_positions.Add(key, position);
				}
			}

			return position;
		}

		private void RecycleTrades()
		{
			if (TradesKeepCount == 0)
			{
				_trades.Clear();
				_tradeStat.Clear(true);
				_securityData.SyncDo(d => d.Values.ForEach(v =>
				{
					v.Trades.Clear();
					v.TradesById.Clear();
					v.TradesByStringId.Clear();
				}));

				return;
			}
			else if (TradesKeepCount == int.MaxValue)
				return;

			var totalCount = _trades.Count;

			if (totalCount < (1.5 * TradesKeepCount))
				return;

			var countToRemove = totalCount - TradesKeepCount;

			lock (_securityData.SyncRoot)
			{
				var toRemove = _trades.SyncGet(d =>
				{
					var tmp = d.Take(countToRemove).ToArray();
					d.RemoveRange(0, countToRemove);
					return tmp;
				});

				foreach (var trade in toRemove)
				{
					_tradeStat.Remove(trade);

					var data = GetData(trade.Security);

					if (trade.Id != 0)
						data.TradesById.Remove(trade.Id);
					else if (!trade.StringId.IsEmpty())
						data.TradesByStringId.Remove(trade.StringId);
					else
						data.Trades.Remove(trade);
				}
			}
		}

		private void RecycleOrders()
		{
			if (OrdersKeepCount == 0)
			{
				_orders.Clear();

				_allOrdersByTransactionId.Clear();
				_allOrdersById.Clear();
				_allOrdersByStringId.Clear();

				_securityData.SyncDo(d => d.Values.ForEach(v =>
				{
					v.Orders.Clear();
					v.OrdersById.Clear();
					v.OrdersByStringId.Clear();
				}));

				return;
			}
			else if (OrdersKeepCount == int.MaxValue)
				return;

			var totalCount = _orders.Count;

			if (totalCount < (1.5 * OrdersKeepCount))
				return;

			var countToRemove = totalCount - OrdersKeepCount;

			lock (_securityData.SyncRoot)
			{
				var toRemove = _orders.SyncGet(d =>
				{
					var tmp = d.Where(o => o.State == OrderStates.Done || o.State == OrderStates.Failed).Take(countToRemove).ToHashSet();
					d.RemoveRange(tmp);
					return tmp;
				});

				_myTrades.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Order)));
				_allOrdersByTransactionId.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));
				_allOrdersById.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));
				_allOrdersByStringId.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));

				foreach (var pair in _securityData)
				{
					pair.Value.Orders.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value.Order)));
					pair.Value.MyTrades.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value.Order)));
					pair.Value.OrdersById.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));
					pair.Value.OrdersByStringId.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));
				}
			}
		}

		public IEnumerable<CandleSeries> AllCandleSeries => _candlesHolder.AllCandleSeries;

		public void CreateCandleSeries(MarketDataMessage mdMsg, CandleSeries series)
		{
			if (mdMsg == null)
				throw new ArgumentNullException(nameof(mdMsg));

			_candlesHolder.CreateCandleSeries(mdMsg.TransactionId, series);
		}

		public CandleSeries RemoveCandleSeries(long transactionId)
			=> _candlesHolder.RemoveCandleSeries(transactionId);

		public long TryGetTransactionId(CandleSeries series)
			=> _candlesHolder.TryGetTransactionId(series);

		public CandleSeries TryGetCandleSeries(long transactionId)
			=> _candlesHolder.TryGetCandleSeries(transactionId);

		public CandleSeries UpdateCandle(CandleMessage message, out Candle candle)
			=> _candlesHolder.UpdateCandle(message, out candle);
	}
}
