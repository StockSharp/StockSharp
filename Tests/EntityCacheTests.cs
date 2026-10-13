namespace StockSharp.Tests;

using Moq;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

/// <summary>
/// Tests for <see cref="EntityCache"/>.
/// </summary>
[TestClass]
public class EntityCacheTests : BaseTestClass
{
	private Mock<ILogReceiver> _logReceiver;
	private Mock<IExchangeInfoProvider> _exchangeInfoProvider;
	private Mock<IPositionProvider> _positionProvider;
	private Security _security;
	private EntityCache _cache;

	[TestInitialize]
	public void Setup()
	{
		_logReceiver = new Mock<ILogReceiver>();
		_exchangeInfoProvider = new Mock<IExchangeInfoProvider>();
		_positionProvider = new Mock<IPositionProvider>();

		_security = new Security
		{
			Id = "AAPL@NASDAQ",
			Code = "AAPL",
			Board = ExchangeBoard.Nasdaq
		};

		_cache = new EntityCache(
			_logReceiver.Object,
			_exchangeInfoProvider.Object,
			_positionProvider.Object);
	}

	[TestMethod]
	public void Constructor_WithValidArgs_CreatesInstance()
	{
		IsNotNull(_cache);
		_cache.ExchangeInfoProvider.AssertEqual(_exchangeInfoProvider.Object);
	}

	[TestMethod]
	public void OrdersKeepCount_Default_Is1000()
	{
		_cache.OrdersKeepCount.AssertEqual(1000);
	}

	[TestMethod]
	public void OrdersKeepCount_SetNegative_ThrowsArgumentOutOfRangeException()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => _cache.OrdersKeepCount = -1);
	}

	[TestMethod]
	public void Clear_EmptiesAllCollections()
	{
		var order = CreateOrder();
		_cache.AddOrderByRegistrationId(order);

		var tradeMessage = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			TradeId = 1,
			TradePrice = order.Price,
			TradeVolume = 1,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};
		_cache.ProcessOwnTradeMessage(order, _security, tradeMessage, order.TransactionId);

		_cache.ProcessNewsMessage(null, new NewsMessage
		{
			Id = "news-clear",
			Headline = "Clear test",
			ServerTime = DateTime.UtcNow,
		});

		_cache.Orders.Any().AssertTrue();
		_cache.MyTrades.Any().AssertTrue();
		_cache.News.Any().AssertTrue();

		_cache.Clear();

		_cache.Orders.Count().AssertEqual(0);
		_cache.MyTrades.Count().AssertEqual(0);
		_cache.News.Count().AssertEqual(0);
	}

	[TestMethod]
	public void AddOrderByRegistrationId_AddsOrder()
	{
		var order = CreateOrder();

		_cache.AddOrderByRegistrationId(order);

		_cache.Orders.Count(o => o == order).AssertEqual(1);
	}

	[TestMethod]
	public void TryGetOrder_ByTransactionId_ReturnsOrder()
	{
		var order = CreateOrder();
		_cache.AddOrderByRegistrationId(order);

		var found = _cache.TryGetOrder(order.TransactionId, OrderOperations.Register);

		found.AssertEqual(order);
	}

	[TestMethod]
	public void TryGetOrder_ByOrderId_ReturnsNull_WhenNotProcessed()
	{
		var order = CreateOrder();
		_cache.AddOrderByRegistrationId(order);

		// Order is added but not yet processed with an exchange ID
		var found = _cache.TryGetOrder(123L, null);

		found.IsNull();
	}

	[TestMethod]
	public void AddOrderByCancelationId_AddsOrderForCancel()
	{
		var order = CreateOrder();
		_cache.AddOrderByRegistrationId(order);

		var cancelTransId = 999L;
		_cache.AddOrderByCancelationId(order, cancelTransId);

		var found = _cache.TryGetOrder(cancelTransId, OrderOperations.Cancel);
		found.AssertEqual(order);
	}

	[TestMethod]
	public void IsMassCancelation_ReturnsFalse_WhenNotAdded()
	{
		_cache.IsMassCancelation(123).AssertFalse();
	}

	[TestMethod]
	public void TryAddMassCancelationId_ThenIsMassCancelation_ReturnsTrue()
	{
		_cache.TryAddMassCancelationId(123);

		_cache.IsMassCancelation(123).AssertTrue();
	}

	[TestMethod]
	public void IsOrderStatusRequest_ReturnsFalse_WhenNotAdded()
	{
		_cache.IsOrderStatusRequest(123).AssertFalse();
	}

	[TestMethod]
	public void AddOrderStatusTransactionId_ThenIsOrderStatusRequest_ReturnsTrue()
	{
		_cache.AddOrderStatusTransactionId(123);

		_cache.IsOrderStatusRequest(123).AssertTrue();
	}

	[TestMethod]
	public void RemoveOrderStatusTransactionId_RemovesId()
	{
		_cache.AddOrderStatusTransactionId(123);
		_cache.RemoveOrderStatusTransactionId(123);

		_cache.IsOrderStatusRequest(123).AssertFalse();
	}

	[TestMethod]
	public void GetOrders_BySecurityAndState_ReturnsMatchingOrders()
	{
		var order = CreateOrder();
		_cache.AddOrderByRegistrationId(order);

		// Process order to set state
		var message = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderState = OrderStates.Active,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		foreach (var _ in _cache.ProcessOrderMessage(order, _security, message, order.TransactionId, _ => order.Portfolio))
		{
			// Process
		}

		var orders = _cache.GetOrders(_security, OrderStates.Active).ToArray();
		orders.Length.AssertEqual(1);
		orders[0].AssertSame(order);
	}

	[TestMethod]
	public void ProcessOrderMessage_DoneThenActive_IgnoresStateRegression()
	{
		var order = CreateOrder();
		_cache.AddOrderByRegistrationId(order);

		var doneMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderState = OrderStates.Done,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		foreach (var _ in _cache.ProcessOrderMessage(order, _security, doneMsg, order.TransactionId, _ => order.Portfolio))
		{
			// Process
		}

		order.State.AssertEqual(OrderStates.Done);

		var activeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderState = OrderStates.Active,
			ServerTime = DateTime.UtcNow.AddSeconds(1),
			LocalTime = DateTime.UtcNow.AddSeconds(1),
		};
		  
		foreach (var _ in _cache.ProcessOrderMessage(order, _security, activeMsg, order.TransactionId, _ => order.Portfolio))
		{
			// Process
		}

		order.State.AssertEqual(OrderStates.Done);
	}

	[TestMethod]
	public void ProcessOrderMessage_LateMessageAfterDone_KeepsFinalBalance()
	{
		// A Done order is terminal: a late duplicate (the same order replayed by the market-data
		// adapter, or a stale order-status snapshot) must not push the filled balance back up.
		var order = CreateOrder();
		_cache.AddOrderByRegistrationId(order);

		var doneMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderState = OrderStates.Done,
			Balance = 0m,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		foreach (var _ in _cache.ProcessOrderMessage(order, _security, doneMsg, order.TransactionId, _ => order.Portfolio))
		{
			// Process
		}

		order.State.AssertEqual(OrderStates.Done);
		order.Balance.AssertEqual(0m);

		// No state claim at all - only stale fields, so the existing Done state guard cannot cover it.
		var staleMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			Balance = order.Volume,
			ServerTime = DateTime.UtcNow.AddSeconds(1),
			LocalTime = DateTime.UtcNow.AddSeconds(1),
		};

		foreach (var _ in _cache.ProcessOrderMessage(order, _security, staleMsg, order.TransactionId, _ => order.Portfolio))
		{
			// Process
		}

		order.Balance.AssertEqual(0m, "balance of a Done order must stay final");
	}

	[TestMethod]
	public void ProcessOrderMessage_LateMessageAfterDone_KeepsOrderTerms()
	{
		// Price and volume are the registration terms of the order; once it is Done they are
		// history and a late message must not rewrite them.
		var order = CreateOrder();
		var price = order.Price;
		var volume = order.Volume;

		_cache.AddOrderByRegistrationId(order);

		var doneMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderState = OrderStates.Done,
			OrderPrice = price,
			OrderVolume = volume,
			Balance = 0m,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		foreach (var _ in _cache.ProcessOrderMessage(order, _security, doneMsg, order.TransactionId, _ => order.Portfolio))
		{
			// Process
		}

		order.State.AssertEqual(OrderStates.Done);

		var staleMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderPrice = price + 100m,
			OrderVolume = volume + 90m,
			ServerTime = DateTime.UtcNow.AddSeconds(1),
			LocalTime = DateTime.UtcNow.AddSeconds(1),
		};

		foreach (var _ in _cache.ProcessOrderMessage(order, _security, staleMsg, order.TransactionId, _ => order.Portfolio))
		{
			// Process
		}

		order.Price.AssertEqual(price, "price of a Done order must not be rewritten");
		order.Volume.AssertEqual(volume, "volume of a Done order must not be rewritten");
	}

	[TestMethod]
	public void ProcessOrderFailMessage_FailedThenActive_IgnoresStateResurrection()
	{
		var order = CreateOrder();
		_cache.AddOrderByRegistrationId(order);

		var failMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderType = order.Type,
			OriginalTransactionId = order.TransactionId,
			Error = new InvalidOperationException("Test fail"),
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		_cache.ProcessOrderFailMessage(order, _security, failMsg).ToArray();

		order.State.AssertEqual(OrderStates.Failed);

		var activeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderState = OrderStates.Active,
			ServerTime = DateTime.UtcNow.AddSeconds(1),
			LocalTime = DateTime.UtcNow.AddSeconds(1),
		};

		foreach (var _ in _cache.ProcessOrderMessage(order, _security, activeMsg, order.TransactionId, _ => order.Portfolio))
		{
			// Process
		}

		order.State.AssertEqual(OrderStates.Failed);
	}

	[TestMethod]
	public void ProcessNewsMessage_NewNews_ReturnsIsNewTrue()
	{
		var newsMessage = new NewsMessage
		{
			Id = "news-123",
			Headline = "Test News",
			ServerTime = DateTime.UtcNow,
		};

		var (news, isNew) = _cache.ProcessNewsMessage(null, newsMessage);

		isNew.AssertTrue();
		news.Id.AssertEqual("news-123");
		news.Headline.AssertEqual("Test News");
	}

	[TestMethod]
	public void ProcessNewsMessage_SameNewsId_ReturnsIsNewFalse()
	{
		var newsMessage = new NewsMessage
		{
			Id = "news-123",
			Headline = "Test News",
			ServerTime = DateTime.UtcNow,
		};

		_cache.ProcessNewsMessage(null, newsMessage);
		var (_, isNew) = _cache.ProcessNewsMessage(null, newsMessage);

		isNew.AssertFalse();
	}

	[TestMethod]
	public void Level1Info_SetAndGetValue_Works()
	{
		var info = _cache.GetSecurityValues(_security, DateTime.UtcNow);

		info.SetValue(DateTime.UtcNow, Level1Fields.LastTradePrice, 150.5m);

		var value = info.GetValue(Level1Fields.LastTradePrice);
		value.AssertEqual(150.5m);
	}

	[TestMethod]
	public void HasLevel1Info_ReturnsFalse_WhenNoData()
	{
		_cache.HasLevel1Info(_security).AssertFalse();
	}

	[TestMethod]
	public void HasLevel1Info_ReturnsTrue_AfterGetSecurityValues()
	{
		_cache.GetSecurityValues(_security, DateTime.UtcNow);

		_cache.HasLevel1Info(_security).AssertTrue();
	}

	[TestMethod]
	public void GetSecurityValue_ReturnsNull_WhenNoData()
	{
		var value = _cache.GetSecurityValue(_security, Level1Fields.LastTradePrice);

		value.IsNull();
	}

	[TestMethod]
	public void AddFail_Register_AddsToOrderRegisterFails()
	{
		var order = CreateOrder();
		var fail = new OrderFail { Order = order, Error = new Exception("Test") };

		_cache.AddFail(OrderOperations.Register, fail);

		_cache.OrderRegisterFails.Count(f => f == fail).AssertEqual(1);
	}

	[TestMethod]
	public void AddFail_Cancel_AddsToOrderCancelFails()
	{
		var order = CreateOrder();
		var fail = new OrderFail { Order = order, Error = new Exception("Test") };

		_cache.AddFail(OrderOperations.Cancel, fail);

		_cache.OrderCancelFails.Count(f => f == fail).AssertEqual(1);
	}

	[TestMethod]
	public void AddFail_Edit_AddsToOrderEditFails()
	{
		var order = CreateOrder();
		var fail = new OrderFail { Order = order, Error = new Exception("Test") };

		_cache.AddFail(OrderOperations.Edit, fail);

		_cache.OrderEditFails.Count(f => f == fail).AssertEqual(1);
	}

	/// <summary>
	/// A venue is free to answer a registration with the final state directly: the order was
	/// matched or rejected before any Active state existed. The subscriber is entitled to be
	/// told what actually happened - one change, carrying Done - and never to be handed an
	/// Active state the venue never reported, because acting on it means trading against an
	/// order that is already gone.
	/// </summary>
	[TestMethod]
	public void ProcessOrderMessage_PendingThenDone_DoesNotInventAnActiveState()
	{
		var order = CreateOrder();
		order.State = OrderStates.Pending;

		_cache.AddOrderByRegistrationId(order);

		var doneMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderState = OrderStates.Done,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		var observed = new List<OrderStates>();

		foreach (var change in _cache.ProcessOrderMessage(order, _security, doneMsg, order.TransactionId, _ => order.Portfolio))
			observed.Add(change.Order.State);

		observed.Count.AssertEqual(1, "one reported state change must raise one change");
		observed[0].AssertEqual(OrderStates.Done, "the only state the subscriber may see is the one the venue reported");
		order.State.AssertEqual(OrderStates.Done);
	}

	/// <summary>
	/// Recycling bounds how many finished orders the cache keeps. It must not take the own
	/// trades of the orders it keeps along with them: those trades are the account's fill
	/// history, and a history that shrinks by itself makes every P&amp;L computed from it wrong
	/// with nothing to point at.
	/// </summary>
	[TestMethod]
	public void RecycleOrders_KeepsTradesOfOrdersItStillHolds()
	{
		_cache.OrdersKeepCount = 2;

		// The only finished order, so it is the one recycling is allowed to drop.
		var finished = CreateOrder();
		finished.TransactionId = 5001;
		finished.State = OrderStates.Done;
		_cache.AddOrderByRegistrationId(finished);
		AddOwnTrade(finished, 9001);

		var live1 = CreateOrder();
		live1.TransactionId = 5002;
		live1.State = OrderStates.Active;
		_cache.AddOrderByRegistrationId(live1);
		AddOwnTrade(live1, 9002);

		// The third order takes the cache past 1.5 x OrdersKeepCount, which is what starts recycling.
		var live2 = CreateOrder();
		live2.TransactionId = 5003;
		live2.State = OrderStates.Active;
		_cache.AddOrderByRegistrationId(live2);
		AddOwnTrade(live2, 9003);

		var orders = _cache.Orders.ToArray();

		orders.Length.AssertEqual(2, "recycling keeps OrdersKeepCount orders");
		Contains(orders, live1, "an unfinished order is never recycled");
		Contains(orders, live2, "an unfinished order is never recycled");

		var trades = _cache.MyTrades.ToArray();

		trades.Any(t => t.Order == live1).AssertTrue("the trade of an order the cache still holds must survive recycling");
		trades.Any(t => t.Order == live2).AssertTrue("the trade of an order the cache still holds must survive recycling");
		trades.All(t => orders.Contains(t.Order)).AssertTrue("no trade may point at an order the cache no longer holds");
	}

	/// <summary>
	/// An order-status row that carries no transaction id describes an order this terminal never
	/// sent. The cache must say it does not know it, so the caller matches it by the exchange's
	/// own ids instead of materializing an order under an id nobody ever sent.
	/// </summary>
	[TestMethod]
	public void ProcessOrderMessage_StatusRowWithoutTransactionId_DoesNotInventAnOrder()
	{
		var portfolio = new Portfolio { Name = "TestPortfolio" };

		var statusRow = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderId = 777,
			OrderState = OrderStates.Active,
			Side = Sides.Buy,
			OrderPrice = 150m,
			OrderVolume = 10m,
			Balance = 10m,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		var changes = _cache.ProcessOrderMessage(null, _security, statusRow, 0, _ => portfolio).ToArray();

		changes.Length.AssertEqual(1);
		AreSame(EntityCache.OrderChangeInfo.NotExist, changes[0], "an unknown order must be reported as unknown");
		_cache.Orders.Count().AssertEqual(0, "no order may be created for a row the cache cannot match");
	}

	/// <summary>
	/// An order restored from an order-status snapshot is reachable by the exchange id it arrived
	/// with. That is what lets the next snapshot of the same order find it instead of producing a
	/// second entity for one order on the venue.
	/// </summary>
	[TestMethod]
	public void ProcessOrderMessage_OrderRestoredFromStatusRow_IsFoundByItsExchangeId()
	{
		var portfolio = new Portfolio { Name = "TestPortfolio" };

		const long restoredId = 424242;

		var statusRow = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OrderId = 777,
			OrderState = OrderStates.Active,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			OrderPrice = 150m,
			OrderVolume = 10m,
			Balance = 10m,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};

		var changes = _cache.ProcessOrderMessage(null, _security, statusRow, restoredId, _ => portfolio).ToArray();

		changes.Length.AssertEqual(1);

		var restored = changes[0].Order;

		restored.Id.Value.AssertEqual(777L);
		AreSame(restored, _cache.TryGetOrder(777L, null), "the restored order must be reachable by its exchange id");
		_cache.Orders.Count().AssertEqual(1, "one venue order must become one entity");
	}

	/// <summary>
	/// A Level1 subscription is answered from what the cache holds for its instrument, found by the
	/// instrument's id rather than by asking anybody for the instrument itself.
	/// </summary>
	[TestMethod]
	public void GetSnapshot_Level1IsFoundByTheSubscriptionsSecurityId()
	{
		var info = _cache.GetSecurityValues(_security, DateTime.UtcNow);

		info.SetValue(DateTime.UtcNow, Level1Fields.LastTradePrice, 101m);

		var snapshot = ((ISnapshotHolder)_cache).GetSnapshot(new MarketDataMessage
		{
			DataType2 = DataType.Level1,
			SecurityId = _security.ToSecurityId(),
			IsSubscribe = true,
		}).Cast<Level1ChangeMessage>().ToArray();

		snapshot.Length.AssertEqual(1);
		snapshot[0].TryGetDecimal(Level1Fields.LastTradePrice).AssertEqual(101m);
	}

	private ExecutionMessage[] GetTransactionsSnapshot(OrderStatusMessage subscription)
		=> [.. ((ISnapshotHolder)_cache).GetSnapshot(subscription).Cast<ExecutionMessage>()];

	private Order AddOrder(long transactionId, OrderStates state)
	{
		var order = CreateOrder();

		order.TransactionId = transactionId;
		order.State = state;

		_cache.AddOrderByRegistrationId(order);

		return order;
	}

	/// <summary>
	/// A terminal that has just come up asks what its orders are and rebuilds the blotter from the
	/// answer. Everything the cache still holds has to be in it - the finished orders too, because
	/// an order that filled or was cancelled while the terminal was away is exactly what the user
	/// needs to see - and every row has to say which order it is and what became of it.
	/// </summary>
	[TestMethod]
	public void GetSnapshot_ANewOrderStatusSubscriptionIsHandedEveryOrderTheCacheHolds()
	{
		var active = AddOrder(7001, OrderStates.Active);
		active.Balance = 4m;

		var done = AddOrder(7002, OrderStates.Done);
		done.Balance = 0m;

		var snapshot = GetTransactionsSnapshot(new OrderStatusMessage { TransactionId = 1, IsSubscribe = true });

		snapshot.Length.AssertEqual(2, "a subscription that asks for everything must be handed every order the cache holds");

		var activeRow = snapshot.First(m => m.TransactionId == active.TransactionId);
		var doneRow = snapshot.First(m => m.TransactionId == done.TransactionId);

		activeRow.OrderState.AssertEqual(OrderStates.Active);
		activeRow.Balance.AssertEqual(4m, "an unfilled remainder is what tells the user the order is still working");

		doneRow.OrderState.AssertEqual(OrderStates.Done, "an order that finished while the terminal was away must arrive as finished");
		doneRow.Balance.AssertEqual(0m);

		foreach (var row in snapshot)
		{
			row.HasOrderInfo.AssertTrue("a row that does not say it carries order info is not read as an order");
			row.DataTypeEx.AssertEqual(DataType.Transactions);
			row.SecurityId.AssertEqual(_security.ToSecurityId());
			row.PortfolioName.AssertEqual("TestPortfolio", "a row without the account it belongs to cannot be put in any blotter");
		}
	}

	/// <summary>
	/// Asking only for working orders is how a caller keeps a blotter of what can still be
	/// cancelled. Handing it yesterday's filled and cancelled orders puts rows in that blotter the
	/// user can act on and the venue will refuse.
	/// </summary>
	[TestMethod]
	public void GetSnapshot_AnOrderStatusSubscriptionForActiveOrdersIsNotHandedFinishedOnes()
	{
		var active = AddOrder(7101, OrderStates.Active);

		AddOrder(7102, OrderStates.Done);
		AddOrder(7103, OrderStates.Failed);

		var snapshot = GetTransactionsSnapshot(new OrderStatusMessage
		{
			TransactionId = 2,
			IsSubscribe = true,
			States = [OrderStates.Active],
		});

		snapshot.Length.AssertEqual(1, "only the orders in the requested states may be served");
		snapshot[0].TransactionId.AssertEqual(active.TransactionId);
		snapshot[0].OrderState.AssertEqual(OrderStates.Active);
	}

	/// <summary>
	/// One connection can carry several accounts and every instrument they trade. A subscription
	/// that names the account and the instrument it is about is drawing one blotter; anything else
	/// the cache holds belongs to another window, and showing another account's orders in it is
	/// both wrong on screen and a disclosure the user never asked for.
	/// </summary>
	[TestMethod]
	public void GetSnapshot_AnOrderStatusSubscriptionScopedToOneAccountIsNotHandedTheOthers()
	{
		var msft = new Security
		{
			Id = "MSFT@NASDAQ",
			Code = "MSFT",
			Board = ExchangeBoard.Nasdaq
		};

		var accountA = new Portfolio { Name = "Account-A" };

		var wanted = CreateOrder();
		wanted.TransactionId = 7201;
		wanted.State = OrderStates.Active;
		wanted.Portfolio = accountA;
		_cache.AddOrderByRegistrationId(wanted);

		// Same account, another instrument.
		var otherSecurity = CreateOrder();
		otherSecurity.TransactionId = 7202;
		otherSecurity.State = OrderStates.Active;
		otherSecurity.Portfolio = accountA;
		otherSecurity.Security = msft;
		_cache.AddOrderByRegistrationId(otherSecurity);

		// Same instrument, another account.
		var otherAccount = CreateOrder();
		otherAccount.TransactionId = 7203;
		otherAccount.State = OrderStates.Active;
		otherAccount.Portfolio = new Portfolio { Name = "Account-B" };
		_cache.AddOrderByRegistrationId(otherAccount);

		var snapshot = GetTransactionsSnapshot(new OrderStatusMessage
		{
			TransactionId = 3,
			IsSubscribe = true,
			PortfolioName = "Account-A",
			SecurityId = _security.ToSecurityId(),
		});

		snapshot.Length.AssertEqual(1, "the snapshot must be confined to the account and the instrument asked for");
		snapshot[0].TransactionId.AssertEqual(wanted.TransactionId);
		snapshot[0].PortfolioName.AssertEqual("Account-A");
		snapshot[0].SecurityId.AssertEqual(_security.ToSecurityId());
	}

	/// <summary>
	/// A subscription can name several instruments instead of one - a watchlist, the legs of a
	/// spread. That list is a filter like any other: the orders handed back must be the orders on
	/// those instruments, or the caller has to filter the venue's answer itself and every caller
	/// that forgets shows orders it never asked for.
	/// </summary>
	[TestMethod]
	public void GetSnapshot_AnOrderStatusSubscriptionNamingItsInstrumentsIsNotHandedTheRest()
	{
		var msft = new Security
		{
			Id = "MSFT@NASDAQ",
			Code = "MSFT",
			Board = ExchangeBoard.Nasdaq
		};

		var wanted = AddOrder(7301, OrderStates.Active);

		var unwanted = CreateOrder();
		unwanted.TransactionId = 7302;
		unwanted.State = OrderStates.Active;
		unwanted.Security = msft;
		_cache.AddOrderByRegistrationId(unwanted);

		var snapshot = GetTransactionsSnapshot(new OrderStatusMessage
		{
			TransactionId = 4,
			IsSubscribe = true,
			SecurityIds = [_security.ToSecurityId()],
		});

		snapshot.Length.AssertEqual(1, "an order on an instrument the subscription did not name must not be served");
		snapshot[0].TransactionId.AssertEqual(wanted.TransactionId);
		snapshot[0].SecurityId.AssertEqual(_security.ToSecurityId());
	}

	private void AddOwnTrade(Order order, long tradeId)
	{
		_cache.ProcessOwnTradeMessage(order, _security, new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = _security.ToSecurityId(),
			OriginalTransactionId = order.TransactionId,
			TradeId = tradeId,
			TradePrice = order.Price,
			TradeVolume = 1m,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}, order.TransactionId);
	}

	private Order CreateOrder()
	{
		return new Order
		{
			Security = _security,
			Portfolio = new Portfolio { Name = "TestPortfolio" },
			TransactionId = 100 + Random.Shared.Next(1000),
			Type = OrderTypes.Limit,
			Price = 150m,
			Volume = 10m,
			Side = Sides.Buy,
		};
	}

	[TestMethod]
	public void EntityCache_ProcessOwnTradeMessage_ZeroVolume()
	{
		// Create dependencies
		var logReceiver = new Mock<ILogReceiver>();
		var exchangeInfoProvider = new Mock<IExchangeInfoProvider>();
		var positionProvider = new Mock<IPositionProvider>();

		var security = new Security
		{
			Id = "AAPL@NASDAQ",
			Code = "AAPL",
			Board = ExchangeBoard.Nasdaq
		};

		// Create EntityCache
		var cache = new EntityCache(
			logReceiver.Object,
			exchangeInfoProvider.Object,
			positionProvider.Object);

		// Create order
		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			TransactionId = 123,
			Type = OrderTypes.Limit,
			Price = 100m,
			Volume = 10m,
			State = OrderStates.Active,
			// AveragePrice is null - this triggers the average price calculation
		};

		// Create ExecutionMessage with ZERO volume - this should cause division by zero
		var message = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = security.ToSecurityId(),
			OrderId = 456,
			TradeId = 789,
			TradePrice = 100m,
			TradeVolume = 0m, // BUG: Zero volume will cause division by zero
			ServerTime = DateTime.UtcNow,
		};

		Throws<ArgumentException>(() => cache.ProcessOwnTradeMessage(order, security, message, order.TransactionId));
	}

	[TestMethod]
	public void EntityCache_ProcessOwnTradeMessage_ZeroPrice()
	{
		var logReceiver = new Mock<ILogReceiver>();
		var exchangeInfoProvider = new Mock<IExchangeInfoProvider>();
		var positionProvider = new Mock<IPositionProvider>();

		var security = new Security { Id = "AAPL@NASDAQ", Code = "AAPL", Board = ExchangeBoard.Nasdaq };
		var cache = new EntityCache(logReceiver.Object, exchangeInfoProvider.Object, positionProvider.Object);

		var order = new Order { Security = security, Portfolio = new Portfolio { Name = "Test" }, TransactionId = 123, Type = OrderTypes.Limit, Price = 100m, Volume = 10m, State = OrderStates.Active };

		var message = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = security.ToSecurityId(),
			OrderId = 456,
			TradeId = 789,
			TradePrice = 0m,
			TradeVolume = 1m,
			ServerTime = DateTime.UtcNow,
		};

		Throws<ArgumentException>(() => cache.ProcessOwnTradeMessage(order, security, message, order.TransactionId));
	}

	[TestMethod]
	public void EntityCache_ProcessOwnTradeMessage_NegativePrice()
	{
		var logReceiver = new Mock<ILogReceiver>();
		var exchangeInfoProvider = new Mock<IExchangeInfoProvider>();
		var positionProvider = new Mock<IPositionProvider>();

		var security = new Security { Id = "AAPL@NASDAQ", Code = "AAPL", Board = ExchangeBoard.Nasdaq };
		var cache = new EntityCache(logReceiver.Object, exchangeInfoProvider.Object, positionProvider.Object);

		var order = new Order { Security = security, Portfolio = new Portfolio { Name = "Test" }, TransactionId = 124, Type = OrderTypes.Limit, Price = 100m, Volume = 10m, State = OrderStates.Active };

		var message = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = security.ToSecurityId(),
			OrderId = 456,
			TradeId = 790,
			TradePrice = -1m,
			TradeVolume = 1m,
			ServerTime = DateTime.UtcNow,
		};

		Throws<ArgumentException>(() => cache.ProcessOwnTradeMessage(order, security, message, order.TransactionId));
	}

	[TestMethod]
	public void EntityCache_ProcessOwnTradeMessage_NullPrice()
	{
		var logReceiver = new Mock<ILogReceiver>();
		var exchangeInfoProvider = new Mock<IExchangeInfoProvider>();
		var positionProvider = new Mock<IPositionProvider>();

		var security = new Security { Id = "AAPL@NASDAQ", Code = "AAPL", Board = ExchangeBoard.Nasdaq };
		var cache = new EntityCache(logReceiver.Object, exchangeInfoProvider.Object, positionProvider.Object);

		var order = new Order { Security = security, Portfolio = new Portfolio { Name = "Test" }, TransactionId = 125, Type = OrderTypes.Limit, Price = 100m, Volume = 10m, State = OrderStates.Active };

		var message = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = security.ToSecurityId(),
			OrderId = 456,
			TradeId = 791,
			TradePrice = null,
			TradeVolume = 1m,
			ServerTime = DateTime.UtcNow,
		};

		Throws<ArgumentException>(() => cache.ProcessOwnTradeMessage(order, security, message, order.TransactionId));
	}

	[TestMethod]
	public void EntityCache_ProcessOwnTradeMessage_NullVolume()
	{
		var logReceiver = new Mock<ILogReceiver>();
		var exchangeInfoProvider = new Mock<IExchangeInfoProvider>();
		var positionProvider = new Mock<IPositionProvider>();

		var security = new Security { Id = "AAPL@NASDAQ", Code = "AAPL", Board = ExchangeBoard.Nasdaq };
		var cache = new EntityCache(logReceiver.Object, exchangeInfoProvider.Object, positionProvider.Object);

		var order = new Order { Security = security, Portfolio = new Portfolio { Name = "Test" }, TransactionId = 126, Type = OrderTypes.Limit, Price = 100m, Volume = 10m, State = OrderStates.Active };

		var message = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = security.ToSecurityId(),
			OrderId = 456,
			TradeId = 792,
			TradePrice = 100m,
			TradeVolume = null,
			ServerTime = DateTime.UtcNow,
		};

		Throws<ArgumentException>(() => cache.ProcessOwnTradeMessage(order, security, message, order.TransactionId));
	}

	[TestMethod]
	public void EntityCache_ProcessOwnTradeMessage_NegativeVolume()
	{
		var logReceiver = new Mock<ILogReceiver>();
		var exchangeInfoProvider = new Mock<IExchangeInfoProvider>();
		var positionProvider = new Mock<IPositionProvider>();

		var security = new Security { Id = "AAPL@NASDAQ", Code = "AAPL", Board = ExchangeBoard.Nasdaq };
		var cache = new EntityCache(logReceiver.Object, exchangeInfoProvider.Object, positionProvider.Object);

		var order = new Order { Security = security, Portfolio = new Portfolio { Name = "Test" }, TransactionId = 127, Type = OrderTypes.Limit, Price = 100m, Volume = 10m, State = OrderStates.Active };

		var message = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = security.ToSecurityId(),
			OrderId = 456,
			TradeId = 793,
			TradePrice = 100m,
			TradeVolume = -1m,
			ServerTime = DateTime.UtcNow,
		};

		Throws<ArgumentException>(() => cache.ProcessOwnTradeMessage(order, security, message, order.TransactionId));
	}

	[TestMethod]
	public void ProcessOwnTradeMessage_FillsWithoutTradeId_AreNotMerged()
	{
		// A venue that reports no trade id still fills one order with several trades. Each fill is
		// its own trade: the second must not come back as a duplicate of the first.
		var order = CreateOrder();
		var secId = _security.ToSecurityId();

		var fill1 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			OriginalTransactionId = order.TransactionId,
			TradePrice = 150m,
			TradeVolume = 4m,
			ServerTime = DateTime.UtcNow,
		};

		var fill2 = fill1.TypedClone();
		fill2.TradePrice = 160m;
		fill2.TradeVolume = 6m;
		fill2.ServerTime = fill1.ServerTime.AddSeconds(1);

		var (trade1, isNew1) = _cache.ProcessOwnTradeMessage(order, _security, fill1, order.TransactionId);
		var (trade2, isNew2) = _cache.ProcessOwnTradeMessage(order, _security, fill2, order.TransactionId);

		isNew1.AssertTrue();
		isNew2.AssertTrue();

		AreNotSame(trade1, trade2);

		trade1.Trade.Volume.AssertEqual(4m);
		trade2.Trade.Volume.AssertEqual(6m);
		trade2.Trade.Price.AssertEqual(160m);

		_cache.MyTrades.Count().AssertEqual(2);
	}
}
