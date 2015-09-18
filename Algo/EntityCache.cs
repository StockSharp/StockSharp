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
			public Order Order { get; private set; }

			public bool RaiseNewOrder { get; set; }

			public OrderInfo(Order order, bool raiseNewOrder = true)
			{
				Order = order;
				RaiseNewOrder = raiseNewOrder;
			}

			public static explicit operator Order(OrderInfo info)
			{
				return info == null ? null : info.Order;
			}
		}

		private sealed class Cache
		{
			public sealed class SecurityData
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
			private readonly CachedSynchronizedList<Trade> _trades = new CachedSynchronizedList<Trade>();

			public readonly SynchronizedDictionary<Tuple<long, bool>, Order> AllOrdersByTransactionId = new SynchronizedDictionary<Tuple<long, bool>, Order>();
			public readonly SynchronizedDictionary<long, Order> AllOrdersById = new SynchronizedDictionary<long, Order>();
			public readonly SynchronizedDictionary<string, Order> AllOrdersByStringId = new SynchronizedDictionary<string, Order>(StringComparer.InvariantCultureIgnoreCase);

			public IEnumerable<Trade> Trades
			{
				get
				{
					return _securityData.SyncGet(d => d.SelectMany(p => p.Value.Trades.SyncGet(t => t.ToArray()).Concat(p.Value.TradesById.SyncGet(t => t.Values.ToArray())).Concat(p.Value.TradesByStringId.SyncGet(t => t.Values.ToArray()))).ToArray());
				}
			}

			public readonly SynchronizedDictionary<string, News> NewsById = new SynchronizedDictionary<string, News>(StringComparer.InvariantCultureIgnoreCase);
			public readonly SynchronizedList<News> NewsWithoutId = new SynchronizedList<News>();

			public IEnumerable<News> News
			{
				get
				{
					return NewsWithoutId.SyncGet(t => t.ToArray()).Concat(NewsById.SyncGet(t => t.Values.ToArray())).ToArray();
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
						throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.NegativeTickCountStorage);

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
						throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.NegativeOrderCountStorage);

					_ordersKeepCount = value;
					RecycleOrders();
				}
			}

			public readonly CachedSynchronizedList<MyTrade> MyTrades = new CachedSynchronizedList<MyTrade>();
			public readonly CachedSynchronizedList<Order> Orders = new CachedSynchronizedList<Order>();
			
			public void AddTrade(Trade trade)
			{
				if (TradesKeepCount == 0)
					return;

				_tradeStat.Add(trade);
				_trades.Add(trade);
				RecycleTrades();
			}

			public void AddOrder(Order order)
			{
				if (OrdersKeepCount == 0)
					return;

				Orders.Add(order);
				RecycleOrders();
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
					Orders.Clear();

					AllOrdersByTransactionId.Clear();
					AllOrdersById.Clear();
					AllOrdersByStringId.Clear();

					_securityData.SyncDo(d => d.Values.ForEach(v =>
					{
						v.Orders.Clear();
						v.OrdersById.Clear();
						v.OrdersByStringId.Clear();
					}));

					return;
				}

				var totalCount = Orders.Count;

				if (OrdersKeepCount == -1 || totalCount < (1.5 * OrdersKeepCount))
					return;

				var countToRemove = totalCount - OrdersKeepCount;

				lock (_securityData.SyncRoot)
				{
					var toRemove = Orders.SyncGet(d =>
					{
						var tmp = d.Where(o => o.State == OrderStates.Done || o.State == OrderStates.Failed).Take(countToRemove).ToHashSet();
						d.RemoveRange(tmp);
						return tmp;
					});

					MyTrades.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Order)));
					AllOrdersByTransactionId.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));
					AllOrdersById.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));
					AllOrdersByStringId.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));

					foreach (var pair in _securityData)
					{
						pair.Value.Orders.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value.Order)));
						pair.Value.MyTrades.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value.Order)));
						pair.Value.OrdersById.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));
						pair.Value.OrdersByStringId.SyncDo(d => d.RemoveWhere(t => toRemove.Contains(t.Value)));
					}
				}
			}

			public SecurityData GetData(Security security)
			{
				return _securityData.SafeAdd(security);
			}

			public void Clear()
			{
				_securityData.Clear();

				AllOrdersById.Clear();
				AllOrdersByStringId.Clear();
				AllOrdersByTransactionId.Clear();
				Orders.Clear();

				NewsById.Clear();
				NewsWithoutId.Clear();

				MyTrades.Clear();

				_trades.Clear();
				_tradeStat.Clear();
			}
		}

		private readonly CachedSynchronizedDictionary<string, Portfolio> _portfolios = new CachedSynchronizedDictionary<string, Portfolio>();
		private readonly Cache _cache = new Cache();
		private readonly HashSet<long> _orderStatusTransactions = new HashSet<long>();

		private IEntityFactory _entityFactory = Algo.EntityFactory.Instance;

		public IEntityFactory EntityFactory
		{
			get { return _entityFactory; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_entityFactory = value;
			}
		}

		public int TradesKeepCount
		{
			get { return _cache.TradesKeepCount; }
			set { _cache.TradesKeepCount = value; }
		}

		public int OrdersKeepCount
		{
			get { return _cache.OrdersKeepCount; }
			set { _cache.OrdersKeepCount = value; }
		}

		public IEnumerable<Order> Orders
		{
			get { return _cache.Orders.Cache; }
		}

		public IEnumerable<Trade> Trades
		{
			get { return _cache.Trades; }
		}

		public IEnumerable<MyTrade> MyTrades
		{
			get { return _cache.MyTrades.Cache; }
		}

		public IEnumerable<News> News
		{
			get { return _cache.News; }
		}

		public virtual IEnumerable<Portfolio> Portfolios
		{
			get { return _portfolios.CachedValues; }
		}

		public void Clear()
		{
			_cache.Clear();
			_orderStatusTransactions.Clear();
		}

		public void AddOrderStatusTransactionId(long transactionId)
		{
			_orderStatusTransactions.Add(transactionId);
		}

		public IEnumerable<Order> GetOrders(Security security, OrderStates state)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return _cache.GetData(security).Orders.CachedValues.Select(info => info.Order).Filter(state);
		}

		public Order GetOrderById(long id)
		{
			return _cache.AllOrdersById.TryGetValue(id);
		}

		public Order GetOrderByTransactionId(long transactionId, bool isCancel)
		{
			return _cache.AllOrdersByTransactionId.TryGetValue(Tuple.Create(transactionId, isCancel));
		}

		public void AddOrderByCancelTransaction(long transactionId, Order order)
		{
			if (order == null)
				return;

			var key = CreateOrderKey(order.Type == OrderTypes.Conditional ? OrderTypes.Conditional : OrderTypes.Limit, transactionId, true);
			_cache.GetData(order.Security).Orders.Add(key, new OrderInfo(order, false));
			_cache.AllOrdersByTransactionId.Add(Tuple.Create(transactionId, true), order);
		}

		public bool TryAddOrder(Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			bool isNew;
			GetOrderInfo(_cache.GetData(order.Security), order.Type, order.TransactionId, null, null, id => order, out isNew);
			return isNew;
		}

		public Tuple<Order, bool, bool> ProcessOrderMessage(Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (message == null)
				throw new ArgumentNullException("message");

			if (message.Error != null)
				throw new ArgumentException(LocalizedStrings.Str714Params.PutEx(message));

			bool isNew;

			var transactionId = message.TransactionId;

			if (transactionId == 0)
			{
				// ExecMsg.OriginalTransactionId == OrderStatMsg.TransactionId when orders info requested by OrderStatMsg
				transactionId = _orderStatusTransactions.Contains(message.OriginalTransactionId) ? 0 : message.OriginalTransactionId;
			}

			var securityData = _cache.GetData(security);

			var orderInfo = GetOrderInfo(securityData, message.OrderType, transactionId, message.OrderId, message.OrderStringId, trId =>
			{
				var o = EntityFactory.CreateOrder(security, message.OrderType, trId);

				o.Time = message.ServerTime;
				o.Price = message.Price;
				o.Volume = message.Volume ?? 0;
				o.Direction = message.Side;
				o.Comment = message.Comment;
				o.ExpiryDate = message.ExpiryDate;
				o.Condition = message.Condition;
				o.UserOrderId = message.UserOrderId;
				o.Portfolio = message.PortfolioName.IsEmpty()
					? _portfolios.FirstOrDefault().Value
					: ProcessPortfolio(message.PortfolioName).Item1;
				o.ClientCode = message.ClientCode;

				return o;
			}, out isNew, true);

			var order = orderInfo.Item1;
			var isCancelled = orderInfo.Item2;
			//var isReReregisterCancelled = orderInfo.Item3;
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
				//if (message.Balance > 0 || !isCancelled || isReReregisterCancelled)
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
				if (order.Volume == 0 && message.Volume != null)
					order.Volume = message.Volume.Value;

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
				_cache.AllOrdersById[order.Id.Value] = order;
			}
				
			if (!order.StringId.IsEmpty())
			{
				securityData.OrdersByStringId[order.StringId] = order;
				_cache.AllOrdersByStringId[order.StringId] = order;
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

		public Tuple<OrderFail, bool> ProcessOrderFailMessage(Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (message == null)
				throw new ArgumentNullException("message");

			var data = _cache.GetData(security);

			Order order = null;

			if (!message.OrderStringId.IsEmpty())
				order = data.OrdersByStringId.TryGetValue(message.OrderStringId);

			bool isCancelled;

			if (order == null)
			{
				if (message.OriginalTransactionId == 0)
					throw new ArgumentOutOfRangeException("message", message.OriginalTransactionId, LocalizedStrings.Str715);

				var orders = data.Orders;

				order = (Order)orders.TryGetValue(CreateOrderKey(message.OrderType, message.OriginalTransactionId, true));

				if (order != null && order.Id == message.OrderId)
					isCancelled = true;
				else
				{
					order = (Order)orders.TryGetValue(CreateOrderKey(message.OrderType, message.OriginalTransactionId, false));
					isCancelled = false;
				}

				if (order == null)
					return null;
			}
			else
			{
				var pair = data.Orders.LastOrDefault(p => p.Value.Order == order);
				isCancelled = pair.Key.Item3;
			}

			// ServerTime для заявки - это время регистрации
			order.LastChangeTime = message.LocalTime;
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
		}

		public Tuple<MyTrade, bool> ProcessMyTradeMessage(Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (message == null)
				throw new ArgumentNullException("message");

			var originalTransactionId = _orderStatusTransactions.Contains(message.OriginalTransactionId)
				? 0 : message.OriginalTransactionId;

			if (originalTransactionId == 0 && message.OrderId == null && message.OrderStringId.IsEmpty())
				throw new ArgumentOutOfRangeException("message", originalTransactionId, LocalizedStrings.Str715);

			var securityData = _cache.GetData(security);

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

				message.CopyExtensionInfo(t);

				//trades.Add(t);
				_cache.MyTrades.Add(t);

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
				throw new ArgumentNullException("security");

			if (message == null)
				throw new ArgumentNullException("message");

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
				throw new ArgumentNullException("message");

			var isNew = false;

			News news;

			if (!message.Id.IsEmpty())
			{
				news = _cache.NewsById.SafeAdd(message.Id, key =>
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
				_cache.NewsWithoutId.Add(news);
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

		private static Tuple<long, bool, bool> CreateOrderKey(OrderTypes type, long transactionId, bool isCancel)
		{
			if (transactionId <= 0)
				throw new ArgumentOutOfRangeException("transactionId", transactionId, LocalizedStrings.Str718);

			return Tuple.Create(transactionId, type == OrderTypes.Conditional, isCancel);
		}

		public Order GetOrder(Security security, long transactionId, long? orderId, string orderStringId, OrderTypes orderType = OrderTypes.Limit, bool isCancel = false)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (transactionId == 0 && orderId == null && orderStringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str719);

			var data = _cache.GetData(security);

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

		private Tuple<Order, bool, bool> GetOrderInfo(Cache.SecurityData securityData, OrderTypes type, long transactionId, long? orderId, string orderStringId, Func<long, Order> createOrder, out bool isNew, bool newOrderRaised = false)
		{
			if (createOrder == null)
				throw new ArgumentNullException("createOrder");

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

					_cache.AddOrder(o);

					// с таким же идентификатором транзакции может быть заявка по другому инструменту
					_cache.AllOrdersByTransactionId.TryAdd(Tuple.Create(transactionId, type == OrderTypes.Conditional), o);

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
				throw new ArgumentNullException("security");

			if (createTrade == null)
				throw new ArgumentNullException("createTrade");

			var isNew = false;

			Trade trade;

			var securityData = _cache.GetData(security);

			if (id != null)
			{
				trade = securityData.TradesById.SafeAdd(id.Value, k =>
				{
					isNew = true;

					var t = createTrade(id.Value, strId);
					_cache.AddTrade(t);
					return t;
				});
			}
			else if (!strId.IsEmpty())
			{
				trade = securityData.TradesByStringId.SafeAdd(strId, k =>
				{
					isNew = true;

					var t = createTrade(null, strId);
					_cache.AddTrade(t);
					return t;
				});
			}
			else
			{
				isNew = true;

				trade = createTrade(null, null);
				_cache.AddTrade(trade);
				securityData.Trades.Add(trade);
			}

			return Tuple.Create(trade, isNew);
		}

		public Tuple<Portfolio, bool, bool> ProcessPortfolio(string name, Func<Portfolio, bool> changePortfolio = null)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

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
	}
}
