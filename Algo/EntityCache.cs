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

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	enum OrderOperations
	{
		Register,
		Cancel,
		Edit,
	}

	class EntityCache : ISnapshotHolder
	{
		private static readonly MemoryStatisticsValue<Trade> _tradeStat = new MemoryStatisticsValue<Trade>(LocalizedStrings.Ticks);

		public class OrderChangeInfo
		{
			private OrderChangeInfo() { }

			public OrderChangeInfo(Order order, bool isNew, bool isChanged, bool isEdit)
			{
				Order = order ?? throw new ArgumentNullException(nameof(order));

				IsNew = isNew;
				IsChanged = isChanged;
				IsEdit = isEdit;
			}

			public Order Order { get; }

			public bool IsNew { get; }
			public bool IsChanged { get; }
			public bool IsEdit { get; }

			public static readonly OrderChangeInfo NotExist = new OrderChangeInfo();
		}

		private sealed class OrderInfo
		{
			private readonly EntityCache _parent;
			private bool _raiseNewOrder;

			public OrderInfo(EntityCache parent, Order order, bool raiseNewOrder)
			{
				Order = order ?? throw new ArgumentNullException(nameof(order));
				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
				_raiseNewOrder = raiseNewOrder;
			}

			public Order Order { get; }

			public IEnumerable<OrderChangeInfo> ApplyChanges(ExecutionMessage message, OrderOperations operation, Action<Order> process)
			{
				if (process is null)
					throw new ArgumentNullException(nameof(process));

				var order = Order;

				OrderChangeInfo retVal;

				if (order.State == OrderStates.Done)
				{
					// данные о заявке могут приходить из маркет-дата и транзакционного адаптеров
					retVal = new OrderChangeInfo(order, _raiseNewOrder, false, false);
					_raiseNewOrder = false;
					process(order);
					yield return retVal;
					//throw new InvalidOperationException("Изменение заявки в состоянии Done невозможно.");
				}
				else if (order.State == OrderStates.Failed)
				{
					// some adapters can resend order's info

					//throw new InvalidOperationException();
					yield break;
				}

				var isPending = order.State == OrderStates.Pending;

				// is we have Pending order and received Done event
				// add intermediate Active event
				if (isPending && message.OrderState == OrderStates.Done)
				{
					var clone = message.TypedClone();
					clone.OrderState = OrderStates.Active;
					clone.Balance = null;

					foreach (var i in ApplyChanges(clone, operation, process))
						yield return i;
				}

				if (message.OrderId != null)
					order.Id = message.OrderId.Value;

				if (!message.OrderStringId.IsEmpty())
					order.StringId = message.OrderStringId;

				if (!message.OrderBoardId.IsEmpty())
					order.BoardId = message.OrderBoardId;

				if (message.Balance != null)
					order.Balance = ((decimal?)order.Balance).ApplyNewBalance(message.Balance.Value, order.TransactionId, _parent._logReceiver);

				if (message.OrderState != null)
					order.ApplyNewState(message.OrderState.Value, _parent._logReceiver);

				if (order.Time == DateTimeOffset.MinValue)
					order.Time = message.ServerTime;

				// для новых заявок используем серверное время, 
				// т.к. заявка получена первый раз и не менялась
				// ServerTime для заявки - это время регистрации
				order.LastChangeTime = _raiseNewOrder ? message.ServerTime : message.LocalTime;
				order.LocalTime = message.LocalTime;

				if (message.OrderPrice != 0)
					order.Price = message.OrderPrice;

				if (message.OrderVolume != null)
					order.Volume = message.OrderVolume.Value;

				if (message.Commission != default)
					order.Commission = message.Commission;

				if (!message.CommissionCurrency.IsEmpty())
					order.CommissionCurrency = message.CommissionCurrency;

				if (message.TimeInForce != default)
					order.TimeInForce = message.TimeInForce.Value;

				if (message.Latency != default)
				{
					switch (operation)
					{
						case OrderOperations.Register:
						{
							if (isPending && order.State != OrderStates.Pending)
								order.LatencyRegistration = message.Latency.Value;

							break;
						}
						case OrderOperations.Cancel:
						{
							order.LatencyCancellation = message.Latency.Value;
							break;
						}
						case OrderOperations.Edit:
						{
							order.LatencyEdition = message.Latency.Value;
							break;
						}
						default:
							throw new ArgumentOutOfRangeException(operation.ToString());
					}
				}

				if (message.AveragePrice != default)
					order.AveragePrice = message.AveragePrice;

				if (message.Yield != default)
					order.Yield = message.Yield;

				if (message.PostOnly != default)
					order.PostOnly = message.PostOnly;

				if (message.SeqNum != default)
					order.SeqNum = message.SeqNum;

				if (message.Leverage != default)
					order.Leverage = message.Leverage;

				message.CopyExtensionInfo(order);

				retVal = new OrderChangeInfo(order, _raiseNewOrder, true, operation == OrderOperations.Edit);
				_raiseNewOrder = false;
				process(order);
				yield return retVal;
			}
		}

		private class SecurityData
		{
			public readonly CachedSynchronizedDictionary<Tuple<long, long, string>, MyTrade> MyTrades = new CachedSynchronizedDictionary<Tuple<long, long, string>, MyTrade>();
			public readonly CachedSynchronizedDictionary<Tuple<long, bool, OrderOperations>, OrderInfo> Orders = new CachedSynchronizedDictionary<Tuple<long, bool, OrderOperations>, OrderInfo>();

			public OrderInfo TryGetOrder(OrderTypes? type, long transactionId, OrderOperations operation)
			{
				return Orders.TryGetValue(CreateOrderKey(type, transactionId, operation))
					?? (type == null ? Orders.TryGetValue(CreateOrderKey(OrderTypes.Conditional, transactionId, operation)) : null);
			}

			public readonly SynchronizedDictionary<long, Trade> TradesById = new SynchronizedDictionary<long, Trade>();
			public readonly SynchronizedDictionary<string, Trade> TradesByStringId = new SynchronizedDictionary<string, Trade>(StringComparer.InvariantCultureIgnoreCase);
			public readonly SynchronizedList<Trade> Trades = new SynchronizedList<Trade>();

			public readonly SynchronizedDictionary<long, Order> OrdersById = new SynchronizedDictionary<long, Order>();
			public readonly SynchronizedDictionary<string, Order> OrdersByStringId = new SynchronizedDictionary<string, Order>(StringComparer.InvariantCultureIgnoreCase);
		}

		private readonly SynchronizedDictionary<Security, SecurityData> _securityData = new SynchronizedDictionary<Security, SecurityData>();

		private SecurityData GetData(Security security)
			=> _securityData.SafeAdd(security);

		private readonly CachedSynchronizedList<Trade> _trades = new CachedSynchronizedList<Trade>();

		public IEnumerable<Trade> Trades
			=> _securityData.SyncGet(d => d.SelectMany(p => p.Value.Trades.SyncGet(t => t.ToArray()).Concat(p.Value.TradesById.SyncGet(t => t.Values.ToArray())).Concat(p.Value.TradesByStringId.SyncGet(t => t.Values.ToArray()))).ToArray());

		private readonly SynchronizedDictionary<Tuple<long, OrderOperations>, Order> _allOrdersByTransactionId = new SynchronizedDictionary<Tuple<long, OrderOperations>, Order>();
		private readonly SynchronizedDictionary<Tuple<long, OrderOperations>, OrderFail> _allOrdersByFailedId = new SynchronizedDictionary<Tuple<long, OrderOperations>, OrderFail>();
		private readonly SynchronizedDictionary<long, Order> _allOrdersById = new SynchronizedDictionary<long, Order>();
		private readonly SynchronizedDictionary<string, Order> _allOrdersByStringId = new SynchronizedDictionary<string, Order>(StringComparer.InvariantCultureIgnoreCase);

		private readonly SynchronizedDictionary<string, News> _newsById = new SynchronizedDictionary<string, News>(StringComparer.InvariantCultureIgnoreCase);
		private readonly SynchronizedList<News> _newsWithoutId = new SynchronizedList<News>();

		private class MarketDepthInfo
		{
			private QuoteChangeMessage _snapshot;
			private bool _hasChanges;

			public void TryFlushChanges(MarketDepth depth)
			{
				if (depth is null)
					throw new ArgumentNullException(nameof(depth));

				if (_hasChanges == false)
					return;

				_hasChanges = false;
				_snapshot.ToMarketDepth(depth);
			}

			public void UpdateSnapshot(QuoteChangeMessage snapshot)
			{
				if (snapshot is null)
					throw new ArgumentNullException(nameof(snapshot));

				_snapshot = snapshot;
				_hasChanges = true;
			}

			public QuoteChangeMessage GetCopy() => _snapshot?.TypedClone();
		}

		private readonly SynchronizedDictionary<Tuple<Security, bool>, MarketDepthInfo> _marketDepths = new SynchronizedDictionary<Tuple<Security, bool>, MarketDepthInfo>();

		public IEnumerable<News> News => _newsWithoutId.SyncGet(t => t.ToArray()).Concat(_newsById.SyncGet(t => t.Values.ToArray())).ToArray();

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
				_orders.Add(order, null);

			RecycleOrders();
		}

		private readonly HashSet<long> _orderStatusTransactions = new HashSet<long>();
		private readonly HashSet<long> _massCancelationTransactions = new HashSet<long>();

		public IEntityFactory EntityFactory { get; }
		
		public IExchangeInfoProvider ExchangeInfoProvider { get; }

		private readonly CachedSynchronizedDictionary<Order, IMessageAdapter> _orders = new CachedSynchronizedDictionary<Order, IMessageAdapter>();

		public IEnumerable<Order> Orders => _orders.CachedKeys;

		private readonly CachedSynchronizedList<MyTrade> _myTrades = new CachedSynchronizedList<MyTrade>();

		public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

		private readonly SynchronizedList<OrderFail> _orderRegisterFails = new SynchronizedList<OrderFail>();

		public IEnumerable<OrderFail> OrderRegisterFails => _orderRegisterFails.SyncGet(c => c.ToArray());

		private readonly SynchronizedList<OrderFail> _orderCancelFails = new SynchronizedList<OrderFail>();

		public IEnumerable<OrderFail> OrderCancelFails => _orderCancelFails.SyncGet(c => c.ToArray());

		private readonly SynchronizedList<OrderFail> _orderEditFails = new SynchronizedList<OrderFail>();

		public IEnumerable<OrderFail> OrderEditFails => _orderEditFails.SyncGet(c => c.ToArray());

		private readonly ILogReceiver _logReceiver;
		private readonly Func<SecurityId?, Security> _tryGetSecurity;
		private readonly IPositionProvider _positionProvider;

		public EntityCache(ILogReceiver logReceiver, Func<SecurityId?, Security> tryGetSecurity, IEntityFactory entityFactory, IExchangeInfoProvider exchangeInfoProvider, IPositionProvider positionProvider)
		{
			_logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
			_tryGetSecurity = tryGetSecurity ?? throw new ArgumentNullException(nameof(tryGetSecurity));
			EntityFactory = entityFactory ?? throw new ArgumentNullException(nameof(entityFactory));
			ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));
			_positionProvider = positionProvider ?? throw new ArgumentNullException(nameof(positionProvider));
		}

		public void Clear()
		{
			_securityData.Clear();

			_allOrdersById.Clear();
			_allOrdersByStringId.Clear();
			_allOrdersByTransactionId.Clear();
			_allOrdersByFailedId.Clear();
			_orders.Clear();

			_newsById.Clear();
			_newsWithoutId.Clear();

			_myTrades.Clear();

			_trades.Clear();
			_tradeStat.Clear(true);

			_orderStatusTransactions.Clear();
			_massCancelationTransactions.Clear();

			_orderCancelFails.Clear();
			_orderRegisterFails.Clear();
			_orderEditFails.Clear();

			_marketDepths.Clear();

			_securityValues.Clear();
			_boardStates.Clear();
		}

		public void AddOrderStatusTransactionId(long transactionId)
		{
			if (!_orderStatusTransactions.Add(transactionId))
				throw new InvalidOperationException();
		}

		public void RemoveOrderStatusTransactionId(long transactionId)
		{
			_orderStatusTransactions.Remove(transactionId);
		}

		public IEnumerable<Order> GetOrders(Security security, OrderStates state)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return GetData(security).Orders.CachedValues.Select(info => info.Order).Filter(state);
		}

		public void TryAddMassCancelationId(long transactionId)
		{
			_massCancelationTransactions.TryAdd(transactionId);
			//if (!_massCancelationTransactions.Add(transactionId))
			//	throw new InvalidOperationException();
		}

		public bool IsMassCancelation(long transactionId) => _massCancelationTransactions.Contains(transactionId);
		public bool IsOrderStatusRequest(long transactionId) => _orderStatusTransactions.Contains(transactionId);

		public void AddOrderByCancelationId(Order order, long transactionId)
		{
			AddOrderByTransactionId(order, transactionId, OrderOperations.Cancel);
		}

		public void AddOrderByRegistrationId(Order order)
		{
			AddOrder(order);
			AddOrderByTransactionId(order, order.TransactionId, OrderOperations.Register);
		}

		public void AddOrderByEditionId(Order order, long transactionId)
		{
			AddOrderByTransactionId(order, transactionId, OrderOperations.Edit);
		}

		public void AddOrderFailById(OrderFail fail, OrderOperations operation, long transactionId)
		{
			_allOrdersByFailedId.TryAdd(Tuple.Create(transactionId, operation), fail);
		}

		private void AddOrderByTransactionId(Order order, long transactionId, OrderOperations operation)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			GetData(order.Security).Orders.Add(CreateOrderKey(order.Type, transactionId, operation), new OrderInfo(this, order, operation == OrderOperations.Register));
			_allOrdersByTransactionId.Add(Tuple.Create(transactionId, operation), order);
		}

		public Order TryGetOrder(long? orderId, string orderStringId)
			=> orderId != null
				? _allOrdersById.TryGetValue(orderId.Value)
				: (orderStringId.IsEmpty() ? null : _allOrdersByStringId.TryGetValue(orderStringId));

		public Order TryGetOrder(long transactionId, OrderOperations operation)
			=> _allOrdersByTransactionId.TryGetValue(Tuple.Create(transactionId, operation));

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

		public IMessageAdapter TryGetAdapter(Order order)
		{
			if (order is null)
				throw new ArgumentNullException(nameof(order));

			return _orders.TryGetValue(order);
		}

		public void TrySetAdapter(Order order, IMessageAdapter adapter)
		{
			if (order is null)
				throw new ArgumentNullException(nameof(order));

			if (adapter is null || adapter is BasketMessageAdapter)
				return;

			_orders[order] = adapter;
		}

		public IEnumerable<OrderChangeInfo> ProcessOrderMessage(Order order, Security security, ExecutionMessage message, long transactionId, Func<string, Portfolio> getPortfolio)
		{
			if (security is null)
				throw new ArgumentNullException(nameof(security));

			if (message is null)
				throw new ArgumentNullException(nameof(message));

			if (getPortfolio is null)
				throw new ArgumentNullException(nameof(getPortfolio));

			if (message.Error != null)
				throw new ArgumentException(LocalizedStrings.Str714Params.PutEx(message));

			var securityData = GetData(security);

			if (transactionId == 0 && message.OrderId == null && message.OrderStringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str719);

			if (transactionId == 0)
			{
				var info = securityData.Orders.CachedValues.FirstOrDefault(i =>
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
					yield return OrderChangeInfo.NotExist;
					//throw new InvalidOperationException(LocalizedStrings.Str1156Params.Put(orderId.To<string>() ?? orderStringId));
				}
				else
				{
					foreach (var i in info.ApplyChanges(message, OrderOperations.Register, o => UpdateOrderIds(o, securityData)))
						yield return i;
				}
			}
			else
			{
				var cancelledInfo = securityData.TryGetOrder(message.OrderType, transactionId, OrderOperations.Cancel);
				var registeredInfo = securityData.TryGetOrder(message.OrderType, transactionId, OrderOperations.Register);
				var editedInfo = securityData.TryGetOrder(message.OrderType, transactionId, OrderOperations.Edit);

				// проверяем не отмененная ли заявка пришла
				if (cancelledInfo != null) // && (cancelledOrder.Id == orderId || (!cancelledOrder.StringId.IsEmpty() && cancelledOrder.StringId.CompareIgnoreCase(orderStringId))))
				{
					var cancellationOrder = cancelledInfo.Order;

					if (registeredInfo == null)
					{
						_logReceiver.AddDebugLog("Сancel '{0}': {1}", cancellationOrder.TransactionId, message);
						
						foreach (var i in cancelledInfo.ApplyChanges(message, OrderOperations.Cancel, o => UpdateOrderIds(o, securityData)))
							yield return i;

						yield break;
					}

					var newOrderState = message.OrderState;

					if ((newOrderState == OrderStates.Active || newOrderState == OrderStates.Done) && cancellationOrder.State != OrderStates.Done)
					{
						_logReceiver.AddDebugLog("Replace-cancel '{0}': {1}", cancellationOrder.TransactionId, message);
						
						cancellationOrder.ApplyNewState(OrderStates.Done, _logReceiver);
						
						if (message.Latency != null)
							cancellationOrder.LatencyCancellation = message.Latency.Value;

						yield return new OrderChangeInfo(cancellationOrder, false, true, false);

						//var isCancelOrderOnly = (message.OrderId != null && message.OrderId == cancellationOrder.Id)
						//	|| (message.OrderStringId != null && message.OrderStringId == cancellationOrder.StringId)
						//	|| (message.OrderBoardId != null && message.OrderBoardId == cancellationOrder.BoardId);

						//if (isCancelOrderOnly)
						//{
						//	_logReceiver.AddDebugLog("Replace-reg empty");
						//	yield break;
						//}
					}

					_logReceiver.AddDebugLog("Replace-reg '{0}': {1}", registeredInfo.Order.TransactionId, message);

					foreach (var i in registeredInfo.ApplyChanges(message, OrderOperations.Register, o => UpdateOrderIds(o, securityData)))
						yield return i;

					yield break;
				}

				if (editedInfo != null)
				{
					_logReceiver.AddDebugLog("Edit '{0}': {1}", editedInfo.Order.TransactionId, message);

					if (message.Latency != null)
						editedInfo.Order.LatencyEdition = message.Latency.Value;

					foreach (var i in editedInfo.ApplyChanges(message, OrderOperations.Edit, o => UpdateOrderIds(o, securityData)))
						yield return i;

					yield break;
				}

				if (registeredInfo == null)
				{
					var o = EntityFactory.CreateOrder(security, message.OrderType, transactionId);

					if (o == null)
						throw new InvalidOperationException(LocalizedStrings.Str720Params.Put(transactionId));

					o.Time = message.ServerTime;
					o.LastChangeTime = message.ServerTime;
					o.Price = message.OrderPrice;
					o.Volume = message.OrderVolume ?? 0;
					o.Direction = message.Side;
					o.Comment = message.Comment;
					o.ExpiryDate = message.ExpiryDate;
					o.Condition = message.Condition;
					o.UserOrderId = message.UserOrderId;
					o.StrategyId = message.StrategyId;
					o.ClientCode = message.ClientCode;
					o.BrokerCode = message.BrokerCode;
					o.IsMarketMaker = message.IsMarketMaker;
					o.IsMargin = message.IsMargin;
					o.Slippage = message.Slippage;
					o.IsManual = message.IsManual;
					o.MinVolume = message.MinVolume;
					o.PositionEffect = message.PositionEffect;
					o.PostOnly = message.PostOnly;
					o.SeqNum = message.SeqNum;
					o.Leverage = message.Leverage;

					if (message.Balance != null)
					{
						if (message.Balance.Value < 0)
							_logReceiver.AddErrorLog($"Order {transactionId}: balance {message.Balance.Value} < 0");

						o.Balance = message.Balance.Value;
					}
					
					o.Portfolio = getPortfolio(message.PortfolioName);

					//if (o.ExtensionInfo == null)
					//	o.ExtensionInfo = new Dictionary<string, object>();

					AddOrder(o);
					_allOrdersByTransactionId.Add(Tuple.Create(transactionId, OrderOperations.Register), o);

					registeredInfo = new OrderInfo(this, o, true);
					securityData.Orders.Add(CreateOrderKey(o.Type, transactionId, OrderOperations.Register), registeredInfo);
				}

				foreach (var i in registeredInfo.ApplyChanges(message, OrderOperations.Register, o => UpdateOrderIds(o, securityData)))
					yield return i;
			}
		}

		public IEnumerable<Tuple<OrderFail, OrderOperations>> ProcessOrderFailMessage(Order order, Security security, ExecutionMessage message)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var data = GetData(security);

			var orders = new List<Tuple<Order, OrderOperations>>();

			if (message.OriginalTransactionId == 0)
				throw new ArgumentOutOfRangeException(nameof(message), message.OriginalTransactionId, LocalizedStrings.Str715);

			var orderType = message.OrderType;

			if (order == null)
			{
				var cancelledOrder = data.TryGetOrder(orderType, message.OriginalTransactionId, OrderOperations.Cancel)?.Order;

				if (cancelledOrder != null && orderType == null)
					orderType = cancelledOrder.Type;

				if (cancelledOrder != null /*&& order.Id == message.OrderId*/)
					orders.Add(Tuple.Create(cancelledOrder, OrderOperations.Cancel));

				var registeredOrder = data.TryGetOrder(orderType, message.OriginalTransactionId, OrderOperations.Register)?.Order;

				if (registeredOrder != null)
					orders.Add(Tuple.Create(registeredOrder, OrderOperations.Register));

				var editedOrder = data.TryGetOrder(orderType, message.OriginalTransactionId, OrderOperations.Edit)?.Order;

				if (editedOrder != null)
					orders.Add(Tuple.Create(editedOrder, OrderOperations.Edit));

				if (cancelledOrder == null && registeredOrder == null && editedOrder == null)
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
				void TryAdd(OrderOperations operation)
				{
					var foundOrder = data.TryGetOrder(order.Type, message.OriginalTransactionId, operation)?.Order;
					if (foundOrder != null)
						orders.Add(Tuple.Create(foundOrder, operation));
				}

				TryAdd(OrderOperations.Cancel);
				TryAdd(OrderOperations.Register);
				TryAdd(OrderOperations.Edit);
			}

			if (orders.Count == 0)
			{
				var fails = new List<Tuple<OrderFail, OrderOperations>>();

				Order TryAddFail(OrderOperations operation)
				{
					lock (_allOrdersByFailedId.SyncRoot)
					{
						if (_allOrdersByFailedId.TryGetAndRemove(Tuple.Create(message.OriginalTransactionId, operation), out var fail))
						{
							fails.Add(Tuple.Create(fail, operation));
							return fail.Order;
						}
					}

					return null;
				}

				TryAddFail(OrderOperations.Edit);
				TryAddFail(OrderOperations.Cancel);

				var regOrder = TryAddFail(OrderOperations.Register);

				if (regOrder != null && regOrder.State == OrderStates.None)
				{
					regOrder.State = OrderStates.Failed;

					regOrder.LastChangeTime = message.ServerTime;
					regOrder.LocalTime = message.LocalTime;
				}

				return fails;
			}

			return orders.Select(t =>
			{
				var o = t.Item1;
				var operation = t.Item2;

				o.LastChangeTime = message.ServerTime;
				o.LocalTime = message.LocalTime;

				if (message.OrderStatus != null)
					o.Status = message.OrderStatus;

				//для ошибок снятия не надо менять состояние заявки
				if (operation == OrderOperations.Register)
					o.ApplyNewState(OrderStates.Failed, _logReceiver);

				if (message.Commission != null)
					o.Commission = message.Commission;

				if (!message.CommissionCurrency.IsEmpty())
					o.CommissionCurrency = message.CommissionCurrency;

				message.CopyExtensionInfo(o);

				var error = message.Error ?? new InvalidOperationException(operation == OrderOperations.Cancel ? LocalizedStrings.Str716 : LocalizedStrings.Str717);

				var fail = EntityFactory.CreateOrderFail(o, error);
				fail.ServerTime = message.ServerTime;
				fail.LocalTime = message.LocalTime;
				fail.SeqNum = message.SeqNum;
				return Tuple.Create(fail, operation);
			});
		}

		public Tuple<MyTrade, bool> ProcessOwnTradeMessage(Order order, Security security, ExecutionMessage message, long transactionId)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var securityData = GetData(security);

			if (transactionId == 0 && message.OrderId == null && message.OrderStringId.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(message), transactionId, LocalizedStrings.Str715);

			var tradeKey = Tuple.Create(transactionId, message.TradeId ?? 0, message.TradeStringId ?? string.Empty);

			if (securityData.MyTrades.TryGetValue(tradeKey, out var myTrade))
				return Tuple.Create(myTrade, false);

			if (order == null)
			{
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				var orderId = message.OrderId;
				var orderStringId = message.OrderStringId;

				if (transactionId == 0 && orderId == null && orderStringId.IsEmpty())
					throw new ArgumentException(LocalizedStrings.Str719);

				var data = GetData(security);

				if (transactionId != 0)
					order = data.TryGetOrder(OrderTypes.Limit, transactionId, OrderOperations.Register)?.Order;

				if (order == null)
				{
					if (orderId != null)
						order = data.OrdersById.TryGetValue(orderId.Value);

					if (order == null)
						order = orderStringId.IsEmpty() ? null : data.OrdersByStringId.TryGetValue(orderStringId);

					if (order == null)
						return null;
				}
			}

			var isNew = false;

			myTrade = securityData.MyTrades.SafeAdd(tradeKey, key =>
			{
				isNew = true;

				var trade = message.ToTrade(EntityFactory.CreateTrade(security, key.Item2, key.Item3));

				if (message.SeqNum != default)
					trade.SeqNum = message.SeqNum;

				var t = EntityFactory.CreateMyTrade(order, trade);

				//if (t.ExtensionInfo == null)
				//	t.ExtensionInfo = new Dictionary<string, object>();

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

				if (message.Initiator != null)
					t.Initiator = message.Initiator;

				if (message.Yield != null)
					t.Yield = message.Yield;

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

			var trade = GetTrade(security, message.TradeId, message.TradeStringId ?? string.Empty, (id, stringId) =>
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

			if (!message.Url.IsEmpty())
				news.Url = message.Url;

			if (message.Priority != null)
				news.Priority = message.Priority;

			if (!message.Language.IsEmpty())
				news.Language = message.Language;

			if (message.ExpiryDate != null)
				news.ExpiryDate = message.ExpiryDate;

			message.CopyExtensionInfo(news);

			return Tuple.Create(news, isNew);
		}

		private static Tuple<long, bool, OrderOperations> CreateOrderKey(OrderTypes? type, long transactionId, OrderOperations operation)
		{
			if (transactionId <= 0)
				throw new ArgumentOutOfRangeException(nameof(transactionId), transactionId, LocalizedStrings.Str718);

			return Tuple.Create(transactionId, type == OrderTypes.Conditional, operation);
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

		public void AddFail(OrderOperations operation, OrderFail fail)
		{
			switch (operation)
			{
				case OrderOperations.Register:
					_orderRegisterFails.Add(fail);
					break;
				case OrderOperations.Cancel:
					_orderCancelFails.Add(fail);
					break;
				case OrderOperations.Edit:
					_orderEditFails.Add(fail);
					break;
				default:
					throw new ArgumentOutOfRangeException(operation.ToString());
			}
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
					var tmp = d.Where(o => o.Key.State.IsFinal()).Take(countToRemove).Select(p => p.Key).ToHashSet();

					foreach (var order in tmp)
						d.Remove(order);

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

		public MarketDepth GetMarketDepth(Security security, QuoteChangeMessage message, out bool isNew)
		{
			if (security is null)
				throw new ArgumentNullException(nameof(security));

			if (message is null)
				throw new ArgumentNullException(nameof(message));

			isNew = false;

			var key = Tuple.Create(security, message.IsFiltered);

			lock (_marketDepths.SyncRoot)
			{
				if (!_marketDepths.TryGetValue(key, out var info))
				{
					isNew = true;

					info = new MarketDepthInfo();
					info.UpdateSnapshot(message.TypedClone());

					// стакан из лога заявок бесконечен
					//if (CreateDepthFromOrdersLog)
					//	info.First.MaxDepth = int.MaxValue;

					_marketDepths.Add(key, info);
				}

				var depth = EntityFactory.CreateMarketDepth(security);
				info.TryFlushChanges(depth);
				return depth;
			}
		}

		public bool HasMarketDepth(Security security, QuoteChangeMessage message)
			=> _marketDepths.ContainsKey(Tuple.Create(security, message.IsFiltered));

		public void UpdateMarketDepth(Security security, QuoteChangeMessage message)
		{
			lock (_marketDepths.SyncRoot)
			{
				var info = _marketDepths.SafeAdd(Tuple.Create(security, message.IsFiltered), key => new MarketDepthInfo());
				info.UpdateSnapshot(message);
			}
		}

		public class Level1Info
		{
			private readonly SyncObject _sync = new SyncObject();
			private readonly Level1ChangeMessage _snapshot;

			public Level1Info(SecurityId securityId, DateTimeOffset serverTime)
			{
				_snapshot = new Level1ChangeMessage
				{
					SecurityId = securityId,
					ServerTime = serverTime,
				};
			}

			public Level1ChangeMessage GetCopy()
			{
				lock (_sync)
					return _snapshot.TypedClone();
			}

			public bool CanBestQuotes { get; private set; } = true;
			public bool CanLastTrade { get; private set; } = true;

			public IEnumerable<Level1Fields> Level1Fields
			{
				get
				{
					lock (_sync)
						return _snapshot.Changes.Keys.ToArray();
				}
			}

			public void SetValue(DateTimeOffset serverTime, Level1Fields field, object value)
			{
				lock (_sync)
				{
					_snapshot.ServerTime = serverTime;
					_snapshot.Changes[field] = value;
				}
			}

			public object GetValue(Level1Fields field)
			{
				lock (_sync)
					return _snapshot.TryGet(field);
			}

			private void RemoveValues(CachedSynchronizedSet<Level1Fields> fields)
			{
				foreach (var field in fields.Cache)
					_snapshot.Changes.Remove(field);
			}

			public void ClearBestQuotes(DateTimeOffset serverTime)
			{
				lock (_sync)
				{
					if (!CanBestQuotes)
						return;

					RemoveValues(Extensions.BestBidFields);
					RemoveValues(Extensions.BestAskFields);

					_snapshot.ServerTime = serverTime;

					CanBestQuotes = false;
				}
			}

			public void ClearLastTrade(DateTimeOffset serverTime)
			{
				lock (_sync)
				{
					if (!CanLastTrade)
						return;

					RemoveValues(Extensions.LastTradeFields);

					_snapshot.ServerTime = serverTime;

					CanLastTrade = false;
				}
			}
		}

		private readonly SynchronizedDictionary<ExchangeBoard, SessionStates?> _boardStates = new SynchronizedDictionary<ExchangeBoard, SessionStates?>();

		public SessionStates? GetSessionState(ExchangeBoard board) => _boardStates.TryGetValue(board);
		public void SetSessionState(ExchangeBoard board, SessionStates? value) => _boardStates[board] = value;

		private readonly SynchronizedDictionary<Security, Level1Info> _securityValues = new SynchronizedDictionary<Security, Level1Info>();

		public object GetSecurityValue(Security security, Level1Fields field)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return _securityValues.TryGetValue(security)?.GetValue(field);
		}

		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (_securityValues.TryGetValue(security, out var info))
				return info.Level1Fields;

			return Enumerable.Empty<Level1Fields>();
		}

		public bool HasLevel1Info(Security security)
			=> _securityValues.ContainsKey(security);

		public Level1Info GetSecurityValues(Security security, DateTimeOffset serverTime)
			=> _securityValues.SafeAdd(security, key => new Level1Info(security.ToSecurityId(), serverTime));

		IEnumerable<Message> ISnapshotHolder.GetSnapshot(ISubscriptionMessage subscription)
		{
			if (subscription is null)
				throw new ArgumentNullException(nameof(subscription));

			var security = subscription is ISecurityIdMessage secIdMsg ? _tryGetSecurity(secIdMsg.SecurityId) : null;
			var dataType = subscription.DataType;

			if (dataType == DataType.Level1)
			{
				if (security == null)
					return Enumerable.Empty<Message>();

				if (_securityValues.TryGetValue(security, out var info))
					return new[] { info.GetCopy() };
			}
			else if (dataType == DataType.MarketDepth)
			{
				if (security == null)
					return Enumerable.Empty<Message>();

				lock (_marketDepths.SyncRoot)
				{
					if (_marketDepths.TryGetValue(Tuple.Create(security, false), out var info))
					{
						var copy = info.GetCopy();

						if (copy != null)
							return new[] { copy };
					}
				}
			}
			else if (dataType == DataType.Transactions)
			{
				lock (_orders.SyncRoot)
					return _orders.Keys.Select(o => o.ToMessage()).Where(m => m.IsMatch(subscription)).ToArray();
			}
			else if (dataType == DataType.PositionChanges)
			{
				var positions = _positionProvider.Positions;

				if (subscription is PortfolioLookupMessage lookupMsg)
					positions = positions.Filter(lookupMsg);

				return positions.Select(p => p.ToChangeMessage()).ToArray();
			}

			return Enumerable.Empty<Message>();
		}
	}
}
