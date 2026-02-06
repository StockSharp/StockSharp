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
			_ => _security,
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
		order.State = OrderStates.Active;
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
			_ => security,
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
		var cache = new EntityCache(logReceiver.Object, _ => security, exchangeInfoProvider.Object, positionProvider.Object);

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
		var cache = new EntityCache(logReceiver.Object, _ => security, exchangeInfoProvider.Object, positionProvider.Object);

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
		var cache = new EntityCache(logReceiver.Object, _ => security, exchangeInfoProvider.Object, positionProvider.Object);

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
		var cache = new EntityCache(logReceiver.Object, _ => security, exchangeInfoProvider.Object, positionProvider.Object);

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
		var cache = new EntityCache(logReceiver.Object, _ => security, exchangeInfoProvider.Object, positionProvider.Object);

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
}