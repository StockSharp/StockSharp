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

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	class EntityCache
	{
		private static readonly MemoryStatisticsValue<Trade> _tradeStat = new MemoryStatisticsValue<Trade>(LocalizedStrings.Ticks);

		private sealed class OrderInfo
		{
			public Order Order { get; }

			public bool RaiseNewOrder { get; set; }

			public OrderInfo(Order order, bool raiseNewOrder = true)
			{
				Order = order;
				RaiseNewOrder = raiseNewOrder;
			}

			public static explicit operator Order(OrderInfo info)
			{
				return info?.Order;
			}
		}

		private class SecurityData
		{
			public readonly CachedSynchronizedDictionary<Tuple<long, long>, MyTrade> MyTrades = new CachedSynchronizedDictionary<Tuple<long, long>, MyTrade>();
			public readonly CachedSynchronizedDictionary<Tuple<long, OrderTypes, bool>, OrderInfo> Orders = new CachedSynchronizedDictionary<Tuple<long, OrderTypes, bool>, OrderInfo>();

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
			get
			{
				return _securityData.SyncGet(d => d.SelectMany(p => p.Value.Trades.SyncGet(t => t.ToArray()).Concat(p.Value.TradesById.SyncGet(t => t.Values.ToArray())).Concat(p.Value.TradesByStringId.SyncGet(t => t.Values.ToArray()))).ToArray());
			}
		}

		private readonly SynchronizedDictionary<Tuple<long, bool>, Order> _allOrdersByTransactionId = new SynchronizedDictionary<Tuple<long, bool>, Order>();
		private readonly SynchronizedDictionary<long, Order> _allOrdersById = new SynchronizedDictionary<long, Order>();
		private readonly SynchronizedDictionary<string, Order> _allOrdersByStringId = new SynchronizedDictionary<string, Order>(StringComparer.InvariantCultureIgnoreCase);

		private readonly SynchronizedDictionary<string, News> _newsById = new SynchronizedDictionary<string, News>(StringComparer.InvariantCultureIgnoreCase);
		private readonly SynchronizedList<News> _newsWithoutId = new SynchronizedList<News>();

		public IEnumerable<News> News
		{
			get
			{
				return _newsWithoutId.SyncGet(t => t.ToArray()).Concat(_newsById.SyncGet(t => t.Values.ToArray())).ToArray();
			}
		}

		private int _tradesKeepCount = 100000;

		public int TradesKeepCount
		{
			get { return _tradesKeepCount; }
			set
			{
				if (_tradesKeepCount == value)
					return;

				if (value < -1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.NegativeTickCountStorage);

				_tradesKeepCount = value;
				RecycleTrades();
			}
		}

		private int _ordersKeepCount = 1000;

		public int OrdersKeepCount
		{
			get { return _ordersKeepCount; }
			set
			{
				if (_ordersKeepCount == value)
					return;

				if (value < -1)
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
			if (OrdersKeepCount == 0)
				return;

			_orders.Add(order);
			RecycleOrders();
		}

		private readonly Dictionary<object, Security> _nativeIdSecurities = new Dictionary<object, Security>();
		private readonly CachedSynchronizedDictionary<string, Portfolio> _portfolios = new CachedSynchronizedDictionary<string, Portfolio>();
		private readonly HashSet<long> _orderStatusTransactions = new HashSet<long>();

		private IEntityFactory _entityFactory = new EntityFactory();

		public IEntityFactory EntityFactory
		{
			get { return _entityFactory; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_entityFactory = value;
			}
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

		private readonly CachedSynchronizedDictionary<Tuple<Portfolio, Security, string, TPlusLimits?>, Position> _positions = new CachedSynchronizedDictionary<Tuple<Portfolio, Security, string, TPlusLimits?>, Position>();

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
			_tradeStat.Clear();

			_orderStatusTransactions.Clear();

			_exchangeBoards.Clear();
			_securities.Clear();
			_nativeIdSecurities.Clear();

			_orderCancelFails.Clear();
			_orderRegisterFails.Clear();

			_positions.Clear();
		}

		public void AddOrderStatusTransactionId(long transactionId)
		{
			_orderStatusTransactions.Add(transactionId);
		}

		public IEnumerable<Order> GetOrders(Security security, OrderStates state)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return GetData(security).Orders.CachedValues.Select(info => info.Order).Filter(state);
		}

		public Order GetOrderById(long id)
		{
			return _allOrdersById.TryGetValue(id);
		}

		public Order GetOrderByTransactionId(long transactionId, bool isCancel)
		{
			return _allOrdersByTransactionId.TryGetValue(Tuple.Create(transactionId, isCancel));
		}

		public void AddOrderByCancelTransaction(long transactionId, Order order)
		{
			if (order == null)
				return;

			var key = CreateOrderKey(order.Type, transactionId, true);
			GetData(order.Security).Orders.Add(key, new OrderInfo(order, false));
			_allOrdersByTransactionId.Add(Tuple.Create(transactionId, true), order);
		}

		public bool TryAddOrder(Order order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			bool isNew;
			GetOrderInfo(GetData(order.Security), order.Type, order.TransactionId, null, null, id => order, out isNew);
			return isNew;
		}

		public Tuple<Order, bool, bool> ProcessOrderMessage(Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.Error != null)
				throw new ArgumentException(LocalizedStrings.Str714Params.PutEx(message));

			bool isNew;

			var transactionId = message.TransactionId;

			if (transactionId == 0)
			{
				// ExecMsg.OriginalTransactionId == OrderStatMsg.TransactionId when orders info requested by OrderStatMsg
				transactionId = _orderStatusTransactions.Contains(message.OriginalTransactionId) ? 0 : message.OriginalTransactionId;
			}

			var securityData = GetData(security);

			var orderInfo = GetOrderInfo(securityData, message.OrderType, transactionId, message.OrderId, message.OrderStringId, trId =>
			{
				var o = EntityFactory.CreateOrder(security, message.OrderType, trId);

				o.Time = message.ServerTime;
				o.Price = message.OrderPrice;
				o.Volume = message.OrderVolume ?? 0;
				o.Direction = message.Side;
				o.Comment = message.Comment;
				o.ExpiryDate = message.ExpiryDate;
				o.Condition = message.Condition;
				o.UserOrderId = message.UserOrderId;
				o.Portfolio = message.PortfolioName.IsEmpty()
					? _portfolios.FirstOrDefault().Value
					: ProcessPortfolio(message.PortfolioName).Item1;
				o.ClientCode = message.ClientCode;
				o.BrokerCode = message.BrokerCode;

				return o;
			}, out isNew, true);

			if (orderInfo == null)
				return null;

			var order = orderInfo.Item1;
			var isCancelled = orderInfo.Item2;
			//var isReRegisterCancelled = orderInfo.Item3;
			var raiseNewOrder = orderInfo.Item3;

			var isPending = order.State == OrderStates.Pending;
			//var isPrevIdSet = (order.Id != null || !order.StringId.IsEmpty());

			bool isChanged;

			if (order.State == OrderStates.Done)
			{
				// данные о заявке могут приходить из маркет-дата и транзакционного адаптеров
				isChanged = false;
				//throw new InvalidOperationException("Изменение заявки в состоянии Done невозможно.");
			}
			else
			{
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
					order.State = message.OrderState.Value;

				if (order.Time == DateTimeOffset.MinValue)
					order.Time = message.ServerTime;

				// для новых заявок используем серверное время, 
				// т.к. заявка получена первый раз и не менялась
				// ServerTime для заявки - это время регистрации
				order.LastChangeTime = isNew ? message.ServerTime : message.LocalTime;
				order.LocalTime = message.LocalTime;

				//нулевой объем может быть при перерегистрации
				if (order.Volume == 0 && message.OrderVolume != null)
					order.Volume = message.OrderVolume.Value;

				if (message.Commission != null)
					order.Commission = message.Commission;

				if (message.TimeInForce != null)
					order.TimeInForce = message.TimeInForce.Value;

				if (isPending)
				{
					if (order.State != OrderStates.Pending && message.Latency != null)
						order.LatencyRegistration = message.Latency.Value;
				}
				else if (isCancelled && order.State == OrderStates.Done)
				{
					if (message.Latency != null)
						order.LatencyCancellation = message.Latency.Value;
				}

				isChanged = true;
			}

			//if (isNew || (!isPrevIdSet && (order.Id != null || !order.StringId.IsEmpty())))
			//{

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

			//}

			//if (message.OrderType == OrderTypes.Conditional && (message.DerivedOrderId != null || !message.DerivedOrderStringId.IsEmpty()))
			//{
			//	var derivedOrder = GetOrder(security, 0L, message.DerivedOrderId ?? 0, message.DerivedOrderStringId);

			//	if (order == null)
			//		_orderStopOrderAssociations.Add(Tuple.Create(message.DerivedOrderId ?? 0, message.DerivedOrderStringId), new RefPair<Order, Action<Order, Order>>(order, (s, o) => s.DerivedOrder = o));
			//	else
			//		order.DerivedOrder = derivedOrder;
			//}

			message.CopyExtensionInfo(order);

			return Tuple.Create(order, raiseNewOrder, isChanged);
		}

		public IEnumerable<Tuple<OrderFail, bool>> ProcessOrderFailMessage(Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var data = GetData(security);

			var orders = new List<Tuple<Order, bool>>();

			if (message.OriginalTransactionId == 0)
				throw new ArgumentOutOfRangeException(nameof(message), message.OriginalTransactionId, LocalizedStrings.Str715);

			var o = (Order)data.Orders.TryGetValue(CreateOrderKey(message.OrderType, message.OriginalTransactionId, true));

			if (o != null /*&& order.Id == message.OrderId*/)
			{
				orders.Add(Tuple.Create(o, true));

				// if replace
				var replaced = (Order)data.Orders.TryGetValue(CreateOrderKey(message.OrderType, message.OriginalTransactionId, false));

				if (replaced != null)
					orders.Add(Tuple.Create(replaced, false));
			}
			else
			{
				o = (Order)data.Orders.TryGetValue(CreateOrderKey(message.OrderType, message.OriginalTransactionId, false));
				orders.Add(Tuple.Create(o, false));
			}

			if (o == null)
			{
				if (!message.OrderStringId.IsEmpty())
				{
					o = data.OrdersByStringId.TryGetValue(message.OrderStringId);

					if (o != null)
					{
						var pair = data.Orders.LastOrDefault(p => p.Value.Order == o);

						if (pair.Key != null)
							orders.Add(Tuple.Create(pair.Value.Order, pair.Key.Item3));
					}
				}
			}

			if (orders.Count == 0)
				return Enumerable.Empty<Tuple<OrderFail, bool>>();

			return orders.Select(t =>
			{
				var order = t.Item1;
				var isCancelled = t.Item2;

				order.LastChangeTime = message.ServerTime;
				order.LocalTime = message.LocalTime;

				if (message.OrderStatus != null)
					order.Status = message.OrderStatus;

				//для ошибок снятия не надо менять состояние заявки
				if (!isCancelled)
					order.State = OrderStates.Failed;

				if (message.Commission != null)
					order.Commission = message.Commission;

				message.CopyExtensionInfo(order);

				var error = message.Error ?? new InvalidOperationException(
					isCancelled ? LocalizedStrings.Str716 : LocalizedStrings.Str717);

				var fail = EntityFactory.CreateOrderFail(order, error);
				fail.ServerTime = message.ServerTime;
				fail.LocalTime = message.LocalTime;
				return Tuple.Create(fail, isCancelled);
			});
		}

		public Tuple<MyTrade, bool> ProcessMyTradeMessage(Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var originalTransactionId = _orderStatusTransactions.Contains(message.OriginalTransactionId)
				? 0 : message.OriginalTransactionId;

			if (originalTransactionId == 0 && message.OrderId == null && message.OrderStringId.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(message), originalTransactionId, LocalizedStrings.Str715);

			var securityData = GetData(security);

			var myTrade = securityData.MyTrades.TryGetValue(Tuple.Create(originalTransactionId, message.TradeId ?? 0));

			if (myTrade != null)
				return Tuple.Create(myTrade, false);

			var order = GetOrder(security, originalTransactionId, message.OrderId, message.OrderStringId);

			if (order == null)
				return null;

			var trade = message.ToTrade(EntityFactory.CreateTrade(security, message.TradeId, message.TradeStringId));

			var isNew = false;

			myTrade = securityData.MyTrades.SafeAdd(Tuple.Create(order.TransactionId, trade.Id), key =>
			{
				isNew = true;

				var t = EntityFactory.CreateMyTrade(order, trade);

				if (t.ExtensionInfo == null)
					t.ExtensionInfo = new Dictionary<object, object>();

				if (message.Commission != null)
					t.Commission = message.Commission;

				if (message.Slippage != null)
					t.Slippage = message.Slippage;

				if (message.PnL != null)
					t.PnL = message.PnL;

				if (message.Position != null)
					t.Position = message.Position;

				message.CopyExtensionInfo(t);

				//trades.Add(t);
				_myTrades.Add(t);

				return t;
			});

			return Tuple.Create(myTrade, isNew);

			// mika
			// http://stocksharp.com/forum/yaf_postst1072_Probliemy-so-sdielkami--pozitsiiami.aspx
			// из-за того, что сделки по заявке иногда приходит быстрее события NewOrders, неправильно расчитывается поза по стратегиям

			//var raiseOrderChanged = false;

			//trades.SyncDo(d =>
			//{
			//    var newBalance = order.Volume - d.Sum(t => t.Trade.Volume);

			//    if (order.Balance > newBalance)
			//    {
			//        raiseOrderChanged = true;

			//        order.Balance = newBalance;

			//        if (order.Balance == 0)
			//            order.State = OrderStates.Done;
			//    }
			//});

			//if (raiseOrderChanged)
			//    RaiseOrderChanged(order);
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
				news.Board = ExchangeBoard.GetOrCreateBoard(message.BoardCode);

			if (message.Url != null)
				news.Url = message.Url;

			message.CopyExtensionInfo(news);

			return Tuple.Create(news, isNew);
		}

		private static Tuple<long, OrderTypes, bool> CreateOrderKey(OrderTypes? type, long transactionId, bool isCancel)
		{
			if (transactionId <= 0)
				throw new ArgumentOutOfRangeException(nameof(transactionId), transactionId, LocalizedStrings.Str718);

			return Tuple.Create(transactionId, type ?? OrderTypes.Limit, isCancel);
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
				order = (Order)data.Orders.TryGetValue(CreateOrderKey(orderType, transactionId, isCancel));

			if (order != null)
				return order;

			if (orderId != null)
				order = data.OrdersById.TryGetValue(orderId.Value);

			if (order != null)
				return order;

			return orderStringId == null ? null : data.OrdersByStringId.TryGetValue(orderStringId);
		}

		private Tuple<Order, bool, bool> GetOrderInfo(SecurityData securityData, OrderTypes? type, long transactionId, long? orderId, string orderStringId, Func<long, Order> createOrder, out bool isNew, bool newOrderRaised = false)
		{
			if (createOrder == null)
				throw new ArgumentNullException(nameof(createOrder));

			if (transactionId == 0 && orderId == null && orderStringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str719);

			var isNew2 = false;
			var orders = securityData.Orders;

			OrderInfo info;

			if (transactionId == 0)
			{
				info = orders.CachedValues.FirstOrDefault(i =>
				{
					var order = i.Order;

					if (orderId != null)
						return order.Id == orderId;
					else
						return order.StringId.CompareIgnoreCase(orderStringId);
				});

				if (info == null)
				{
					isNew = false;
					return null;
					//throw new InvalidOperationException(LocalizedStrings.Str1156Params.Put(orderId.To<string>() ?? orderStringId));
				}
			}
			else
			{
				var cancelKey = CreateOrderKey(type, transactionId, true);
				var registerKey = CreateOrderKey(type, transactionId, false);

				var cancelledOrder = (Order)orders.TryGetValue(cancelKey);

				// проверяем не отмененная ли заявка пришла
				if (cancelledOrder != null && (cancelledOrder.Id == orderId || (!cancelledOrder.StringId.IsEmpty() && cancelledOrder.StringId.CompareIgnoreCase(orderStringId))))
				{
					isNew = false;
					return Tuple.Create(cancelledOrder, true/*, (Order)orders.TryGetValue(registerKey) != null*/, false);
				}

				info = orders.SafeAdd(registerKey, key =>
				{
					isNew2 = true;

					var o = createOrder(transactionId);

					if (o == null)
						throw new InvalidOperationException(LocalizedStrings.Str720Params.Put(transactionId));

					if (o.ExtensionInfo == null)
						o.ExtensionInfo = new Dictionary<object, object>();

					AddOrder(o);

					// с таким же идентификатором транзакции может быть заявка по другому инструменту
					_allOrdersByTransactionId.TryAdd(Tuple.Create(transactionId, type == OrderTypes.Conditional), o);

					return new OrderInfo(o);
				});
			}

			var raiseNewOrder = info.RaiseNewOrder;

			if (raiseNewOrder && newOrderRaised)
				info.RaiseNewOrder = false;

			isNew = isNew2;
			return Tuple.Create((Order)info, false, raiseNewOrder);
		}

		public Tuple<Trade, bool> GetTrade(Security security, long? id, string strId, Func<long?, string, Trade> createTrade)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (createTrade == null)
				throw new ArgumentNullException(nameof(createTrade));

			var isNew = false;

			Trade trade;

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

			return Tuple.Create(trade, isNew);
		}

		public Tuple<Portfolio, bool, bool> ProcessPortfolio(string name, Func<Portfolio, bool> changePortfolio = null)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			bool isNew;

			var portfolio = _portfolios.SafeAdd(name, key =>
			{
				var p = EntityFactory.CreatePortfolio(key);

				if (p == null)
					throw new InvalidOperationException(LocalizedStrings.Str1104Params.Put(name));

				if (p.ExtensionInfo == null)
					p.ExtensionInfo = new Dictionary<object, object>();

				return p;
			}, out isNew);

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

		public Security GetSecurityByNativeId(object nativeSecurityId)
		{
			return _nativeIdSecurities.TryGetValue(nativeSecurityId);
		}

		public IEnumerable<Security> GetSecuritiesByCode(string code)
		{
			return _securities.CachedValues.Where(s => s.Code.CompareIgnoreCase(code));
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

				if (s.ExtensionInfo == null)
					s.ExtensionInfo = new Dictionary<object, object>();

				var info = idConvert(key);

				if (s.Board == null)
					s.Board = info.Item2;

				if (s.Code.IsEmpty())
					s.Code = info.Item1;

				if (s.Name.IsEmpty())
					s.Name = info.Item1;

				if (s.Class.IsEmpty())
					s.Class = info.Item2.Code;

				return s;
			}, out isNew);
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

		public Position TryAddPosition(Portfolio portfolio, Security security, string depoName, TPlusLimits? limitType, string description, out bool isNew)
		{
			isNew = false;
			Position position;

			lock (_positions.SyncRoot)
			{
				if (depoName == null)
					depoName = string.Empty;

				var key = Tuple.Create(portfolio, security, depoName, limitType);

				if (!_positions.TryGetValue(key, out position))
				{
					isNew = true;

					position = EntityFactory.CreatePosition(portfolio, security);
					position.DepoName = depoName;
					position.LimitType = limitType;
					position.Description = description;
					_positions.Add(key, position);
				}
			}

			return position;
		}

		public object GetNativeId(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return _nativeIdSecurities.LastOrDefault(p => p.Value == security).Key;
		}

		public void AddSecurityByNativeId(object native, string stocksharp)
		{
			_nativeIdSecurities.Add(native, GetSecurityById(stocksharp));
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

			var totalCount = _trades.Count;

			if (TradesKeepCount == -1 || totalCount < (1.5 * TradesKeepCount))
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

			var totalCount = _orders.Count;

			if (OrdersKeepCount == -1 || totalCount < (1.5 * OrdersKeepCount))
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
	}
}
