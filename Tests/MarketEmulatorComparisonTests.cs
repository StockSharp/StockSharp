namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Shared integration tests for both MarketEmulator and MarketEmulator2.
/// Uses DataRow to run same tests against both implementations.
/// </summary>
[TestClass]
public class MarketEmulatorComparisonTests : BaseTestClass
{
	private const string EmulatorV1 = "V1";
	private const string EmulatorV2 = "V2";

	private static readonly IdGenerator _idGenerator = new IncrementalIdGenerator();
	private const string _pfName = Messages.Extensions.SimulatorPortfolioName;

	private IMarketEmulator CreateEmulator(string version, SecurityId secId, out List<Message> result)
		=> CreateEmulator(version, [secId], out result);

	private IMarketEmulator CreateEmulator(string version, IEnumerable<SecurityId> secIds, out List<Message> result)
	{
		var securities = secIds.Select(id => new Security { Id = id.ToStringId() });
		var secProvider = new CollectionSecurityProvider(securities);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchProvider = new InMemoryExchangeInfoProvider();
		var idGen = new IncrementalIdGenerator();

		IMarketEmulator emu = version switch
		{
			EmulatorV1 => new MarketEmulator(secProvider, pfProvider, exchProvider, idGen) { VerifyMode = true },
			EmulatorV2 => new MarketEmulator2(secProvider, pfProvider, exchProvider, idGen) { VerifyMode = true },
			_ => throw new ArgumentException($"Unknown emulator version: {version}")
		};

		var resultList = new List<Message>();
		emu.NewOutMessage += resultList.Add;
		result = resultList;
		return emu;
	}

	private async Task AddBookAsync(IMarketEmulator emu, SecurityId secId, DateTime now, decimal bid = 100, decimal ask = 101)
	{
		await emu.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(bid, 10)],
			Asks = [new(ask, 10)]
		}, CancellationToken);
	}

	#region Limit Order Tests

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: LimitBuyPutInQueue")]
	[DataRow(EmulatorV2, DisplayName = "V2: LimitBuyPutInQueue")]
	public async Task LimitBuyPutInQueueOrderBook(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100, // At bid, below ask
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.Find(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Active, m.OrderState, $"Expected Active state for {version}");
	}

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: LimitSellPutInQueue")]
	[DataRow(EmulatorV2, DisplayName = "V2: LimitSellPutInQueue")]
	public async Task LimitSellPutInQueueOrderBook(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 101, // At ask, above bid
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.Find(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Active, m.OrderState, $"Expected Active state for {version}");
	}

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: LimitBuyExecute")]
	[DataRow(EmulatorV2, DisplayName = "V2: LimitBuyExecute")]
	public async Task LimitBuyExecuteAtAsk(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101, // At ask, will execute
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var orderMsg = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		var tradeMsg = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());

		IsNotNull(orderMsg, $"No order message found for {version}");
		AreEqual(OrderStates.Done, orderMsg.OrderState, $"Expected Done state for {version}");
		AreEqual(0m, orderMsg.Balance, $"Expected zero balance for {version}");

		IsNotNull(tradeMsg, $"No trade message found for {version}");
		AreEqual(5m, tradeMsg.TradeVolume, $"Expected trade volume 5 for {version}");
	}

	#endregion

	#region IOC Tests

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: LimitBuyIOCFull")]
	[DataRow(EmulatorV2, DisplayName = "V2: LimitBuyIOCFull")]
	public async Task LimitBuyIOCFull(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 5, // Less than available (10)
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance, // IOC
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected Done state for {version}");
		AreEqual(0m, m.Balance, $"Expected zero balance for {version}");

		var trade = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		IsNotNull(trade, $"No trade message found for {version}");
		AreEqual(reg.Volume, trade.TradeVolume, $"Expected full trade volume for {version}");
	}

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: LimitBuyIOCPartial")]
	[DataRow(EmulatorV2, DisplayName = "V2: LimitBuyIOCPartial")]
	public async Task LimitBuyIOCPartial(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 15, // More than available (10)
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance, // IOC
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected Done state for {version}");
		AreEqual(5m, m.Balance, $"Expected balance of 5 (15-10) for {version}");

		var trade = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		IsNotNull(trade, $"No trade message found for {version}");
		AreEqual(10m, trade.TradeVolume, $"Expected trade volume 10 for {version}");
	}

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: LimitBuyIOCNone")]
	[DataRow(EmulatorV2, DisplayName = "V2: LimitBuyIOCNone")]
	public async Task LimitBuyIOCNone(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100, // At bid, no match
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance, // IOC
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected Done state for {version}");
		AreEqual(15m, m.Balance, $"Expected full balance cancelled for {version}");

		var trade = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		IsNull(trade, $"Expected no trade for {version}");
	}

	#endregion

	#region Market Order Tests

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: MarketBuy")]
	[DataRow(EmulatorV2, DisplayName = "V2: MarketBuy")]
	public async Task MarketBuyOrderBook(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 5,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected Done state for {version}");
		AreEqual(0m, m.Balance, $"Expected zero balance for {version}");

		var trade = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		IsNotNull(trade, $"No trade message found for {version}");
		AreEqual(reg.Volume, trade.TradeVolume, $"Expected full trade volume for {version}");
	}

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: MarketSell")]
	[DataRow(EmulatorV2, DisplayName = "V2: MarketSell")]
	public async Task MarketSellOrderBook(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Volume = 5,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected Done state for {version}");
		AreEqual(0m, m.Balance, $"Expected zero balance for {version}");

		var trade = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		IsNotNull(trade, $"No trade message found for {version}");
		AreEqual(reg.Volume, trade.TradeVolume, $"Expected full trade volume for {version}");
	}

	#endregion

	#region Post-Only Tests

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: PostOnlyNoMatch")]
	[DataRow(EmulatorV2, DisplayName = "V2: PostOnlyNoMatch")]
	public async Task LimitBuyPostOnlyNoMatch(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 99, // Below bid, won't cross
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Active, m.OrderState, $"Expected Active state for {version}");
	}

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: PostOnlyWouldMatch")]
	[DataRow(EmulatorV2, DisplayName = "V2: PostOnlyWouldMatch")]
	public async Task LimitBuyPostOnlyWouldMatch(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101, // At ask, would cross
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected Done (rejected) state for {version}");
		IsNotNull(m.Balance, $"Expected balance for {version}");
		AreEqual(m.OrderVolume, m.Balance, $"Expected full balance (rejected) for {version}");
	}

	#endregion

	#region Cancel Order Tests

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: CancelOrder")]
	[DataRow(EmulatorV2, DisplayName = "V2: CancelOrder")]
	public async Task CancelOrder(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Active, m.OrderState, $"Expected Active state for {version}");

		res.Clear();

		await emu.SendInMessageAsync(new OrderCancelMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = _idGenerator.GetNextId(),
			OrderId = 1,
			OriginalTransactionId = reg.TransactionId,
			PortfolioName = _pfName,
		}, CancellationToken);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		IsNotNull(m, $"No cancel confirmation found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected Done state after cancel for {version}");
		IsNotNull(m.Balance, $"Expected balance for {version}");
		AreEqual(m.OrderVolume, m.Balance, $"Expected full balance after cancel for {version}");
	}

	#endregion

	#region Replace Order Tests

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: ReplaceOrder")]
	[DataRow(EmulatorV2, DisplayName = "V2: ReplaceOrder")]
	public async Task ReplaceOrder(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		AreEqual(OrderStates.Active, m.OrderState, $"Expected Active state for {version}");

		var replace = new OrderReplaceMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = _idGenerator.GetNextId(),
			OriginalTransactionId = reg.TransactionId,
			OldOrderId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		res.Clear();

		await emu.SendInMessageAsync(replace, CancellationToken);

		// Old order should be done
		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		IsNotNull(m, $"No old order update found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected old order Done for {version}");

		// New order should be active
		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == replace.TransactionId);
		IsNotNull(m, $"No new order found for {version}");
		AreEqual(OrderStates.Active, m.OrderState, $"Expected new order Active for {version}");
	}

	#endregion

	#region Order Status Tests

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: OrderStatus")]
	[DataRow(EmulatorV2, DisplayName = "V2: OrderStatus")]
	public async Task OrderStatusReturnsActiveOrders(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		// Create two orders
		var reg1 = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 99,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg1, CancellationToken);

		var reg2 = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 102,
			Volume = 3,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg2, CancellationToken);

		res.Clear();

		await emu.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		var activeOrders = res.OfType<ExecutionMessage>()
			.Where(x => x.OrderState == OrderStates.Active)
			.ToList();

		AreEqual(2, activeOrders.Count, $"Expected 2 active orders for {version}");
	}

	#endregion

	#region FOK Tests

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: FOK Full")]
	[DataRow(EmulatorV2, DisplayName = "V2: FOK Full")]
	public async Task LimitBuyFOKFull(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100, // At bid, no match expected
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel, // FOK
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		IsNotNull(m, $"No execution message found for {version}");
		AreEqual(OrderStates.Done, m.OrderState, $"Expected Done state for {version}");
		IsNotNull(m.Balance, $"Expected balance for {version}");
		AreEqual(m.OrderVolume, m.Balance, $"Expected full balance (no fill) for {version}");
	}

	[TestMethod]
	[DataRow(EmulatorV1, DisplayName = "V1: FOK Execute")]
	[DataRow(EmulatorV2, DisplayName = "V2: FOK Execute")]
	public async Task LimitBuyFOKExecute(string version)
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmulator(version, id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 102, // Above ask, will execute
			Volume = 5, // Less than available (10)
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel, // FOK
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var trade = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		IsNotNull(trade, $"No trade message found for {version}");
		AreEqual(reg.Volume, trade.TradeVolume, $"Expected full trade volume for {version}");
	}

	#endregion
}
