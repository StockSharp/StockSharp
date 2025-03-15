namespace StockSharp.Algo;

enum OrderOperations
{
	Register,
	Cancel,
	Edit,
}

class EntityCache(ILogReceiver logReceiver, Func<SecurityId?, Security> tryGetSecurity, IExchangeInfoProvider exchangeInfoProvider, IPositionProvider positionProvider) : ISnapshotHolder
{
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

		public static readonly OrderChangeInfo NotExist = new();
	}

	private class OrderInfo(EntityCache parent, Order order, bool raiseNewOrder)
	{
		private readonly EntityCache _parent = parent ?? throw new ArgumentNullException(nameof(parent));

		public Order Order { get; } = order ?? throw new ArgumentNullException(nameof(order));

		public IEnumerable<OrderChangeInfo> ApplyChanges(ExecutionMessage message, OrderOperations operation, Action<Order> process)
		{
			if (process is null)
				throw new ArgumentNullException(nameof(process));

			var order = Order;

			OrderChangeInfo retVal;

			if (order.State == OrderStates.Done)
			{
				// данные о заявке могут приходить из маркет-дата и транзакционного адаптеров
				retVal = new OrderChangeInfo(order, raiseNewOrder, false, false);
				raiseNewOrder = false;
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
			order.ServerTime = raiseNewOrder ? message.ServerTime : message.LocalTime;
			order.LocalTime = message.LocalTime;

			if (message.OrderState == OrderStates.Done)
			{
				if (message.IsCanceled())
					order.CancelledTime ??= message.ServerTime;
				else
					order.MatchedTime ??= message.ServerTime;
			}

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

			retVal = new OrderChangeInfo(order, raiseNewOrder, true, operation == OrderOperations.Edit);
			raiseNewOrder = false;
			process(order);
			yield return retVal;
		}
	}

	private class SecurityData
	{
		public readonly CachedSynchronizedDictionary<(long transId, long tradeId, string tradeStrId), MyTrade> MyTrades = [];
		public readonly CachedSynchronizedDictionary<(long transId, bool conditional, OrderOperations operation), OrderInfo> Orders = [];

		public OrderInfo TryGetOrder(OrderTypes? type, long transactionId, OrderOperations operation)
		{
			return Orders.TryGetValue(CreateOrderKey(type, transactionId, operation))
				?? (type == null ? Orders.TryGetValue(CreateOrderKey(OrderTypes.Conditional, transactionId, operation)) : null);
		}

		public readonly SynchronizedDictionary<long, Order> OrdersById = [];
		public readonly SynchronizedDictionary<string, Order> OrdersByStringId = new(StringComparer.InvariantCultureIgnoreCase);
	}

	private readonly SynchronizedDictionary<Security, SecurityData> _securityData = [];

	private SecurityData GetData(Security security)
		=> _securityData.SafeAdd(security);

	private readonly SynchronizedDictionary<(long transId, OrderOperations operation), Order> _allOrdersByTransactionId = [];
	private readonly SynchronizedDictionary<(long transId, OrderOperations operation), OrderFail> _allOrdersByFailedId = [];
	private readonly SynchronizedDictionary<long, Order> _allOrdersById = [];
	private readonly SynchronizedDictionary<string, Order> _allOrdersByStringId = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly SynchronizedDictionary<Order, (decimal totalVolume, decimal weightedPriceSum)> _ordersAvgPrices = [];

	private readonly SynchronizedDictionary<string, News> _newsById = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly SynchronizedList<News> _newsWithoutId = [];

	public IEnumerable<News> News => [.. _newsWithoutId.SyncGet(t => t.ToArray()), .. _newsById.SyncGet(t => t.Values.ToArray())];

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

	private void AddOrder(Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		if (OrdersKeepCount > 0)
			_orders.Add(order, null);

		RecycleOrders();
	}

	private readonly HashSet<long> _orderStatusTransactions = [];
	private readonly HashSet<long> _massCancelationTransactions = [];

	public IExchangeInfoProvider ExchangeInfoProvider { get; } = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

	private readonly CachedSynchronizedDictionary<Order, IMessageAdapter> _orders = [];
	public IEnumerable<Order> Orders => _orders.CachedKeys;

	private readonly CachedSynchronizedList<MyTrade> _myTrades = [];
	public IEnumerable<MyTrade> MyTrades => _myTrades.Cache;

	private readonly SynchronizedList<OrderFail> _orderRegisterFails = [];
	public IEnumerable<OrderFail> OrderRegisterFails => _orderRegisterFails.SyncGet(c => c.ToArray());

	private readonly SynchronizedList<OrderFail> _orderCancelFails = [];
	public IEnumerable<OrderFail> OrderCancelFails => _orderCancelFails.SyncGet(c => c.ToArray());

	private readonly SynchronizedList<OrderFail> _orderEditFails = [];
	public IEnumerable<OrderFail> OrderEditFails => _orderEditFails.SyncGet(c => c.ToArray());

	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly Func<SecurityId?, Security> _tryGetSecurity = tryGetSecurity ?? throw new ArgumentNullException(nameof(tryGetSecurity));
	private readonly IPositionProvider _positionProvider = positionProvider ?? throw new ArgumentNullException(nameof(positionProvider));

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

		_orderStatusTransactions.Clear();
		_massCancelationTransactions.Clear();

		_orderCancelFails.Clear();
		_orderRegisterFails.Clear();
		_orderEditFails.Clear();

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
		_allOrdersByFailedId.TryAdd2((transactionId, operation), fail);
	}

	private void AddOrderByTransactionId(Order order, long transactionId, OrderOperations operation)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		GetData(order.Security).Orders.Add(CreateOrderKey(order.Type, transactionId, operation), new OrderInfo(this, order, operation == OrderOperations.Register));
		_allOrdersByTransactionId.Add((transactionId, operation), order);
	}

	public Order TryGetOrder(long? orderId, string orderStringId)
		=> orderId != null
			? _allOrdersById.TryGetValue(orderId.Value)
			: (orderStringId.IsEmpty() ? null : _allOrdersByStringId.TryGetValue(orderStringId));

	public Order TryGetOrder(long transactionId, OrderOperations operation)
		=> _allOrdersByTransactionId.TryGetValue((transactionId, operation));

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

		if (adapter is null or BasketMessageAdapter)
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

		if (!message.IsOk())
			throw new ArgumentException(LocalizedStrings.MessageHasStateAndError.PutEx(message));

		var securityData = GetData(security);

		if (transactionId == 0 && message.OrderId == null && message.OrderStringId.IsEmpty())
			throw new ArgumentException(LocalizedStrings.NoOrderIds);

		if (transactionId == 0)
		{
			var info = securityData.Orders.CachedValues.FirstOrDefault(i =>
			{
				if (order != null)
					return i.Order == order;

				if (message.OrderId != null)
					return i.Order.Id == message.OrderId;
				else
					return i.Order.StringId.EqualsIgnoreCase(message.OrderStringId);
			});

			if (info == null)
			{
				yield return OrderChangeInfo.NotExist;
				//throw new InvalidOperationException(LocalizedStrings.OrderNotFound.Put(orderId.To<string>() ?? orderStringId));
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
			if (cancelledInfo != null) // && (cancelledOrder.Id == orderId || (!cancelledOrder.StringId.IsEmpty() && cancelledOrder.StringId.EqualsIgnoreCase(orderStringId))))
			{
				var cancellationOrder = cancelledInfo.Order;

				if (registeredInfo == null)
				{
					_logReceiver.LogDebug("Сancel '{0}': {1}", cancellationOrder.TransactionId, message);

					foreach (var i in cancelledInfo.ApplyChanges(message, OrderOperations.Cancel, o => UpdateOrderIds(o, securityData)))
						yield return i;

					yield break;
				}

				var newOrderState = message.OrderState;

				if ((newOrderState == OrderStates.Active || newOrderState == OrderStates.Done) && cancellationOrder.State != OrderStates.Done)
				{
					_logReceiver.LogDebug("Replace-cancel '{0}': {1}", cancellationOrder.TransactionId, message);

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

				_logReceiver.LogDebug("Replace-reg '{0}': {1}", registeredInfo.Order.TransactionId, message);

				foreach (var i in registeredInfo.ApplyChanges(message, OrderOperations.Register, o => UpdateOrderIds(o, securityData)))
					yield return i;

				yield break;
			}

			if (editedInfo != null)
			{
				_logReceiver.LogDebug("Edit '{0}': {1}", editedInfo.Order.TransactionId, message);

				if (message.Latency != null)
					editedInfo.Order.LatencyEdition = message.Latency.Value;

				foreach (var i in editedInfo.ApplyChanges(message, OrderOperations.Edit, o => UpdateOrderIds(o, securityData)))
					yield return i;

				yield break;
			}

			if (registeredInfo == null)
			{
				var o = new Order
				{
					Security = security,
					Type = message.OrderType,
					TransactionId = transactionId,
					Time = message.ServerTime,
					ServerTime = message.ServerTime,
					Price = message.OrderPrice,
					Volume = message.OrderVolume ?? 0,
					Side = message.Side,
					Comment = message.Comment,
					ExpiryDate = message.ExpiryDate,
					Condition = message.Condition,
					UserOrderId = message.UserOrderId,
					StrategyId = message.StrategyId,
					ClientCode = message.ClientCode,
					BrokerCode = message.BrokerCode,
					IsMarketMaker = message.IsMarketMaker,
					MarginMode = message.MarginMode,
					Slippage = message.Slippage,
					IsManual = message.IsManual,
					MinVolume = message.MinVolume,
					PositionEffect = message.PositionEffect,
					PostOnly = message.PostOnly,
					SeqNum = message.SeqNum,
					Leverage = message.Leverage
				};

				if (message.Balance != null)
				{
					if (message.Balance.Value < 0)
						_logReceiver.LogError($"Order {transactionId}: balance {message.Balance.Value} < 0");

					o.Balance = message.Balance.Value;
				}

				o.Portfolio = getPortfolio(message.PortfolioName);

				AddOrder(o);
				_allOrdersByTransactionId.TryAdd2((transactionId, OrderOperations.Register), o);

				registeredInfo = new OrderInfo(this, o, true);
				securityData.Orders.Add(CreateOrderKey(o.Type, transactionId, OrderOperations.Register), registeredInfo);
			}

			foreach (var i in registeredInfo.ApplyChanges(message, OrderOperations.Register, o => UpdateOrderIds(o, securityData)))
				yield return i;
		}
	}

	public IEnumerable<(OrderFail, OrderOperations)> ProcessOrderFailMessage(Order order, Security security, ExecutionMessage message)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var data = GetData(security);

		var orders = new List<(Order order, OrderOperations operation)>();

		if (message.OriginalTransactionId == 0)
			throw new ArgumentOutOfRangeException(nameof(message), message.OriginalTransactionId, LocalizedStrings.TransactionInvalid);

		var orderType = message.OrderType;

		if (order == null)
		{
			var cancelledOrder = data.TryGetOrder(orderType, message.OriginalTransactionId, OrderOperations.Cancel)?.Order;

			if (cancelledOrder != null && orderType == null)
				orderType = cancelledOrder.Type;

			if (cancelledOrder != null /*&& order.Id == message.OrderId*/)
				orders.Add((cancelledOrder, OrderOperations.Cancel));

			var registeredOrder = data.TryGetOrder(orderType, message.OriginalTransactionId, OrderOperations.Register)?.Order;

			if (registeredOrder != null)
				orders.Add((registeredOrder, OrderOperations.Register));

			var editedOrder = data.TryGetOrder(orderType, message.OriginalTransactionId, OrderOperations.Edit)?.Order;

			if (editedOrder != null)
				orders.Add((editedOrder, OrderOperations.Edit));

			if (cancelledOrder == null && registeredOrder == null && editedOrder == null)
			{
				if (!message.OrderStringId.IsEmpty())
				{
					order = data.OrdersByStringId.TryGetValue(message.OrderStringId);

					if (order != null)
					{
						var pair = data.Orders.LastOrDefault(p => p.Value.Order == order);

						if (pair.Value != null)
							orders.Add((pair.Value.Order, pair.Key.operation));
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
					orders.Add((foundOrder, operation));
			}

			TryAdd(OrderOperations.Cancel);
			TryAdd(OrderOperations.Register);
			TryAdd(OrderOperations.Edit);
		}

		if (orders.Count == 0)
		{
			var fails = new List<(OrderFail, OrderOperations)>();

			Order TryAddFail(OrderOperations operation)
			{
				lock (_allOrdersByFailedId.SyncRoot)
				{
					if (_allOrdersByFailedId.TryGetAndRemove((message.OriginalTransactionId, operation), out var fail))
					{
						fails.Add((fail, operation));
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

				regOrder.ServerTime = message.ServerTime;
				regOrder.LocalTime = message.LocalTime;
			}

			return fails;
		}

		return orders.Select(t =>
		{
			var o = t.order;
			var operation = t.operation;

			o.ServerTime = message.ServerTime;
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

			var error = message.Error ?? new InvalidOperationException(operation == OrderOperations.Cancel ? LocalizedStrings.ErrorCancelling : LocalizedStrings.ErrorRegistering);

			var fail = new OrderFail
			{
				Order = o,
				Error = error,
				ServerTime = message.ServerTime,
				LocalTime = message.LocalTime,
				SeqNum = message.SeqNum,
				TransactionId = message.OriginalTransactionId,
			};
			return (fail, operation);
		});
	}

	public (MyTrade trade, bool isNew) ProcessOwnTradeMessage(Order order, Security security, ExecutionMessage message, long transactionId)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var securityData = GetData(security);

		if (transactionId == 0 && message.OrderId == null && message.OrderStringId.IsEmpty())
			throw new ArgumentOutOfRangeException(nameof(message), transactionId, LocalizedStrings.TransactionInvalid);

		var tradeKey = (transactionId, message.TradeId ?? 0, message.TradeStringId ?? string.Empty);

		if (securityData.MyTrades.TryGetValue(tradeKey, out var myTrade))
			return (myTrade, false);

		if (order == null)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var orderId = message.OrderId;
			var orderStringId = message.OrderStringId;

			if (transactionId == 0 && orderId == null && orderStringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.NoOrderIds);

			var data = GetData(security);

			if (transactionId != 0)
				order = data.TryGetOrder(OrderTypes.Limit, transactionId, OrderOperations.Register)?.Order;

			if (order == null)
			{
				if (orderId != null)
					order = data.OrdersById.TryGetValue(orderId.Value);

				if (order == null && !orderStringId.IsEmpty())
					order = data.OrdersByStringId.TryGetValue(orderStringId);

				if (order == null)
					return default;
			}
		}

		var isNew = false;

		myTrade = securityData.MyTrades.SafeAdd(tradeKey, key =>
		{
			isNew = true;

#pragma warning disable CS0618 // Type or member is obsolete
			var trade = message.ToTrade(security);
#pragma warning restore CS0618 // Type or member is obsolete

			if (message.SeqNum != default)
				trade.SeqNum = message.SeqNum;

			var t = new MyTrade { Order = order, Trade = trade };

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

			_myTrades.Add(t);

			if (order.AveragePrice is null)
			{
				var weightedPrice = trade.Price * trade.Volume;

				if (_ordersAvgPrices.TryGetValue(order, out var t1))
				{
					t1.totalVolume += trade.Volume;
					t1.weightedPriceSum += weightedPrice;
					
					_ordersAvgPrices[order] = t1;
				}
				else
				{
					_ordersAvgPrices.Add(order, t1 = new(trade.Volume, weightedPrice));
				}

				order.AveragePrice = t1.weightedPriceSum / t1.totalVolume;
			}

			return t;
		});

		return (myTrade, isNew);
	}

	public (News news, bool isNew) ProcessNewsMessage(Security security, NewsMessage message)
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
				return new News
				{
					Id = key
				};
			});
		}
		else
		{
			isNew = true;

			news = new();
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

		return (news, isNew);
	}

	private static (long transId, bool conditional, OrderOperations operation) CreateOrderKey(OrderTypes? type, long transactionId, OrderOperations operation)
	{
		if (transactionId <= 0)
			throw new ArgumentOutOfRangeException(nameof(transactionId), transactionId, LocalizedStrings.TransactionInvalid);

		return (transactionId, type == OrderTypes.Conditional, operation);
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
				var tmp = d.Where(o => o.Key.State.IsFinal()).Take(countToRemove).Select(p => p.Key).ToSet();

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

	public class Level1Info
	{
		private readonly SyncObject _sync = new();
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
					return [.. _snapshot.Changes.Keys];
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

	private readonly SynchronizedDictionary<ExchangeBoard, SessionStates?> _boardStates = [];

	public SessionStates? GetSessionState(ExchangeBoard board) => _boardStates.TryGetValue(board);
	public void SetSessionState(ExchangeBoard board, SessionStates? value) => _boardStates[board] = value;

	private readonly SynchronizedDictionary<Security, Level1Info> _securityValues = [];

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

		return [];
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
				return [];

			if (_securityValues.TryGetValue(security, out var info))
				return [info.GetCopy()];
		}
		else if (dataType == DataType.Transactions)
		{
			lock (_orders.SyncRoot)
				return [.. _orders.Keys.Select(o => o.ToMessage()).Where(m => m.IsMatch(m.Type, subscription))];
		}
		else if (dataType == DataType.PositionChanges)
		{
			var positions = _positionProvider.Positions;

			if (subscription is PortfolioLookupMessage lookupMsg)
				positions = positions.Filter(lookupMsg);

			return [.. positions.Select(p => p.ToChangeMessage())];
		}

		return [];
	}
}
