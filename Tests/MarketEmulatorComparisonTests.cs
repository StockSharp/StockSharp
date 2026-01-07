namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Shared integration tests for both MarketEmulatorOld and MarketEmulator.
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
			EmulatorV1 => new MarketEmulatorOld(secProvider, pfProvider, exchProvider, idGen) { VerifyMode = true },
			EmulatorV2 => new MarketEmulator(secProvider, pfProvider, exchProvider, idGen) { VerifyMode = true },
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

	#region V1 vs V2 Comparison Tests

	private (IMarketEmulator emu, List<Message> results) CreateEmulatorPair(bool useV2, SecurityId secId, bool verifyMode = true)
	{
		var securities = new[] { new Security { Id = secId.ToStringId() } };
		var secProvider = new CollectionSecurityProvider(securities);
		var pfProvider = new CollectionPortfolioProvider([Portfolio.CreateSimulator()]);
		var exchProvider = new InMemoryExchangeInfoProvider();
		var idGen = new IncrementalIdGenerator();

		IMarketEmulator emu;

		if (useV2)
		{
			var emu2 = new MarketEmulator(secProvider, pfProvider, exchProvider, idGen) { VerifyMode = verifyMode };
			emu2.OrderIdGenerator = new IncrementalIdGenerator();
			emu2.TradeIdGenerator = new IncrementalIdGenerator();
			emu = emu2;
		}
		else
		{
			var emu1 = new MarketEmulatorOld(secProvider, pfProvider, exchProvider, idGen) { VerifyMode = verifyMode };
			emu1.Settings.Failing = 0;
			emu1.Settings.Latency = TimeSpan.Zero;
			emu = emu1;
		}

		var results = new List<Message>();
		emu.NewOutMessage += results.Add;
		return (emu, results);
	}

	[TestMethod]
	public async Task Comparison_FullMessageComparison_BuyAndSell()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulatorPair(false, secId);
		var (emuV2, resV2) = CreateEmulatorPair(true, secId);

		var now = DateTime.UtcNow;

		var initMoney = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m);

		await emuV1.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);

		var book = new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(100m, 100m)],
			Asks = [new(101m, 100m)]
		};

		await emuV1.SendInMessageAsync(book.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(book.TypedClone(), CancellationToken);

		var buyOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 100,
			Side = Sides.Buy,
			Price = 101m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(buyOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(buyOrder.TypedClone(), CancellationToken);

		now = now.AddSeconds(1);

		book = new QuoteChangeMessage
		{
			SecurityId = secId,
			LocalTime = now,
			ServerTime = now,
			Bids = [new(102m, 100m)],
			Asks = [new(103m, 100m)]
		};

		await emuV1.SendInMessageAsync(book.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(book.TypedClone(), CancellationToken);

		var sellOrder = new OrderRegisterMessage
		{
			SecurityId = secId,
			LocalTime = now,
			TransactionId = 101,
			Side = Sides.Sell,
			Price = 102m,
			Volume = 10m,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		await emuV1.SendInMessageAsync(sellOrder.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(sellOrder.TypedClone(), CancellationToken);

		var tradesV1 = resV1.OfType<ExecutionMessage>().Where(e => e.TradeId != null).ToList();
		var tradesV2 = resV2.OfType<ExecutionMessage>().Where(e => e.TradeId != null).ToList();

		AreEqual(tradesV1.Count, tradesV2.Count, $"Trade count mismatch: V1={tradesV1.Count}, V2={tradesV2.Count}");
		for (var i = 0; i < tradesV1.Count; i++)
		{
			AreEqual(tradesV1[i].TradePrice, tradesV2[i].TradePrice, $"Trade[{i}].Price");
			AreEqual(tradesV1[i].TradeVolume, tradesV2[i].TradeVolume, $"Trade[{i}].Volume");
			AreEqual(tradesV1[i].Side, tradesV2[i].Side, $"Trade[{i}].Side");
		}

		var lastMoneyV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.IsMoney())
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));
		var lastMoneyV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.IsMoney())
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));

		IsNotNull(lastMoneyV1, "V1 should produce RealizedPnL");
		IsNotNull(lastMoneyV2, "V2 should produce RealizedPnL");

		var pnlV1 = (decimal)lastMoneyV1.Changes[PositionChangeTypes.RealizedPnL];
		var pnlV2 = (decimal)lastMoneyV2.Changes[PositionChangeTypes.RealizedPnL];
		AreEqual(pnlV1, pnlV2, $"RealizedPnL mismatch: V1={pnlV1}, V2={pnlV2}");
	}

	[TestMethod]
	public async Task Comparison_RealizedPnL_AfterClosingPosition()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulatorPair(false, secId);
		var (emuV2, resV2) = CreateEmulatorPair(true, secId);

		var now = DateTime.UtcNow;

		var initMoney = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m);

		await emuV1.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);

		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 101m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 101m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		now = now.AddSeconds(1);

		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(110m, 100m)], Asks = [new(111m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(110m, 100m)], Asks = [new(111m, 100m)]
		}, CancellationToken);

		resV1.Clear();
		resV2.Clear();

		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 2,
			Side = Sides.Sell, Price = 110m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 2,
			Side = Sides.Sell, Price = 110m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		var pnlMsgV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));

		var pnlMsgV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));

		IsNotNull(pnlMsgV1, "V1 should produce RealizedPnL message");
		IsNotNull(pnlMsgV2, "V2 should produce RealizedPnL message");

		var realizedV1 = (decimal)pnlMsgV1.Changes[PositionChangeTypes.RealizedPnL];
		var realizedV2 = (decimal)pnlMsgV2.Changes[PositionChangeTypes.RealizedPnL];

		AreEqual(realizedV1, realizedV2, $"RealizedPnL mismatch: V1={realizedV1}, V2={realizedV2}");
		IsTrue(realizedV1 > 0, $"RealizedPnL should be positive, got {realizedV1}");
	}

	[TestMethod]
	public async Task Comparison_PositionTracking_CurrentValueAndAveragePrice()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulatorPair(false, secId);
		var (emuV2, resV2) = CreateEmulatorPair(true, secId);

		var now = DateTime.UtcNow;

		var initMoney = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m);

		await emuV1.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);
		await emuV2.SendInMessageAsync(initMoney.TypedClone(), CancellationToken);

		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		resV1.Clear();
		resV2.Clear();

		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 101m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 101m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		var posV1 = resV1.OfType<PositionChangeMessage>()
			.LastOrDefault(m => m.SecurityId != SecurityId.Money && m.Changes.ContainsKey(PositionChangeTypes.CurrentValue));

		var posV2 = resV2.OfType<PositionChangeMessage>()
			.LastOrDefault(m => m.SecurityId != SecurityId.Money && m.Changes.ContainsKey(PositionChangeTypes.CurrentValue));

		if (posV1 != null)
		{
			IsNotNull(posV2, "V2 should produce position CurrentValue message if V1 does");

			var curValV1 = (decimal)posV1.Changes[PositionChangeTypes.CurrentValue];
			var curValV2 = (decimal)posV2.Changes[PositionChangeTypes.CurrentValue];

			AreEqual(curValV1, curValV2, $"Position CurrentValue mismatch: V1={curValV1}, V2={curValV2}");

			if (posV1.Changes.TryGetValue(PositionChangeTypes.AveragePrice, out var avgV1Obj))
			{
				IsTrue(posV2.Changes.ContainsKey(PositionChangeTypes.AveragePrice),
					"V2 should produce AveragePrice if V1 does");

				var avgV1 = (decimal)avgV1Obj;
				var avgV2 = (decimal)posV2.Changes[PositionChangeTypes.AveragePrice];

				AreEqual(avgV1, avgV2, $"AveragePrice mismatch: V1={avgV1}, V2={avgV2}");
			}
		}
	}

	[TestMethod]
	public async Task Comparison_Commission_MatchesBetweenVersions()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulatorPair(false, secId);
		var (emuV2, resV2) = CreateEmulatorPair(true, secId);

		var now = DateTime.UtcNow;

		await emuV1.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money, PortfolioName = _pfName,
			LocalTime = now, ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV2.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money, PortfolioName = _pfName,
			LocalTime = now, ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		resV1.Clear();
		resV2.Clear();

		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 101m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 101m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		var commV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.Commission));

		var commV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.Commission));

		if (commV1 != null)
		{
			IsNotNull(commV2, "V2 should produce Commission if V1 does");

			var commValV1 = (decimal)commV1.Changes[PositionChangeTypes.Commission];
			var commValV2 = (decimal)commV2.Changes[PositionChangeTypes.Commission];

			AreEqual(commValV1, commValV2, $"Commission mismatch: V1={commValV1}, V2={commValV2}");
		}
	}

	[TestMethod]
	public async Task Comparison_UnrealizedPnL_WithOpenPosition()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulatorPair(false, secId);
		var (emuV2, resV2) = CreateEmulatorPair(true, secId);

		var now = DateTime.UtcNow;

		await emuV1.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money, PortfolioName = _pfName,
			LocalTime = now, ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV2.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money, PortfolioName = _pfName,
			LocalTime = now, ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 101m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 101m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		now = now.AddSeconds(1);
		resV1.Clear();
		resV2.Clear();

		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(110m, 100m)], Asks = [new(111m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(110m, 100m)], Asks = [new(111m, 100m)]
		}, CancellationToken);

		var unrealV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.UnrealizedPnL));

		var unrealV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.UnrealizedPnL));

		if (unrealV1 != null)
		{
			IsNotNull(unrealV2, "V2 should produce UnrealizedPnL if V1 does");

			var unrealValV1 = (decimal)unrealV1.Changes[PositionChangeTypes.UnrealizedPnL];
			var unrealValV2 = (decimal)unrealV2.Changes[PositionChangeTypes.UnrealizedPnL];

			AreEqual(unrealValV1, unrealValV2, $"UnrealizedPnL mismatch: V1={unrealValV1}, V2={unrealValV2}");
		}
	}

	[TestMethod]
	public async Task Comparison_BlockedValue_WithActiveOrder()
	{
		var secId = Helper.CreateSecurityId();
		var (emuV1, resV1) = CreateEmulatorPair(false, secId);
		var (emuV2, resV2) = CreateEmulatorPair(true, secId);

		var now = DateTime.UtcNow;

		await emuV1.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money, PortfolioName = _pfName,
			LocalTime = now, ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV2.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money, PortfolioName = _pfName,
			LocalTime = now, ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), CancellationToken);

		await emuV1.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new QuoteChangeMessage
		{
			SecurityId = secId, LocalTime = now, ServerTime = now,
			Bids = [new(100m, 100m)], Asks = [new(101m, 100m)]
		}, CancellationToken);

		resV1.Clear();
		resV2.Clear();

		await emuV1.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 100m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		await emuV2.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = secId, LocalTime = now, TransactionId = 1,
			Side = Sides.Buy, Price = 100m, Volume = 10m,
			OrderType = OrderTypes.Limit, PortfolioName = _pfName,
		}, CancellationToken);

		var blockedV1 = resV1.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.BlockedValue));

		var blockedV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.SecurityId == SecurityId.Money)
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.BlockedValue));

		if (blockedV1 != null)
		{
			IsNotNull(blockedV2, "V2 should produce BlockedValue if V1 does");

			var blockedValV1 = (decimal)blockedV1.Changes[PositionChangeTypes.BlockedValue];
			var blockedValV2 = (decimal)blockedV2.Changes[PositionChangeTypes.BlockedValue];

			AreEqual(blockedValV1, blockedValV2, $"BlockedValue mismatch: V1={blockedValV1}, V2={blockedValV2}");
		}
	}

	[TestMethod]
	public async Task Comparison_FullScenario_WithCandles()
	{
		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var (emuV1, resV1) = CreateEmulatorPair(false, secId, verifyMode: false);
		var (emuV2, resV2) = CreateEmulatorPair(true, secId, verifyMode: false);

		var storageRegistry = Helper.FileSystem.GetStorage(Paths.HistoryDataPath);
		var candleStorage = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1));

		var candles = await candleStorage.LoadAsync(Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1))
			.Take(100)
			.ToArrayAsync(CancellationToken);

		if (candles.Length == 0)
			return;

		var initTime = (DateTime)candles[0].OpenTime;
		await emuV1.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money, PortfolioName = _pfName,
			LocalTime = initTime, ServerTime = initTime,
		}.Add(PositionChangeTypes.BeginValue, 10000000m), CancellationToken);

		await emuV2.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money, PortfolioName = _pfName,
			LocalTime = initTime, ServerTime = initTime,
		}.Add(PositionChangeTypes.BeginValue, 10000000m), CancellationToken);

		long trId = 1;
		var position = 0m;
		decimal? prevClose = null;

		foreach (var candle in candles)
		{
			var time = (DateTime)candle.OpenTime;

			await emuV1.SendInMessageAsync(candle.TypedClone(), CancellationToken);
			await emuV2.SendInMessageAsync(candle.TypedClone(), CancellationToken);

			if (prevClose != null)
			{
				if (candle.ClosePrice > prevClose && position <= 0)
				{
					var order = new OrderRegisterMessage
					{
						SecurityId = secId, LocalTime = time, TransactionId = trId++,
						Side = Sides.Buy, Price = candle.ClosePrice, Volume = 1,
						OrderType = OrderTypes.Limit, PortfolioName = _pfName,
					};
					await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
					await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);
					position++;
				}
				else if (candle.ClosePrice < prevClose && position >= 0)
				{
					var order = new OrderRegisterMessage
					{
						SecurityId = secId, LocalTime = time, TransactionId = trId++,
						Side = Sides.Sell, Price = candle.ClosePrice, Volume = 1,
						OrderType = OrderTypes.Limit, PortfolioName = _pfName,
					};
					await emuV1.SendInMessageAsync(order.TypedClone(), CancellationToken);
					await emuV2.SendInMessageAsync(order.TypedClone(), CancellationToken);
					position--;
				}
			}
			prevClose = candle.ClosePrice;
		}

		var tradesV2 = resV2.OfType<ExecutionMessage>().Where(e => e.TradeId != null).ToList();
		IsTrue(tradesV2.Count > 0, $"V2 should generate trades, got {tradesV2.Count}");

		var lastMoneyV2 = resV2.OfType<PositionChangeMessage>()
			.Where(m => m.IsMoney())
			.LastOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));

		IsNotNull(lastMoneyV2, "V2 should produce RealizedPnL");

		var pnlV2 = (decimal)lastMoneyV2.Changes[PositionChangeTypes.RealizedPnL];
		IsTrue(pnlV2 != 0 || position == 0, $"V2 RealizedPnL should be calculated, got {pnlV2}");
	}

	#endregion
}
