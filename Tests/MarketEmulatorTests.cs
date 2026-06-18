namespace StockSharp.Tests;

using StockSharp.Algo.Testing;

[TestClass]
public class MarketEmulatorTests : BaseTestClass
{
	private static IMarketEmulator CreateEmuWithEvents(SecurityId secId, out List<Message> result)
		=> CreateEmuWithEvents([secId], out result);

	private static IMarketEmulator CreateEmuWithEvents(IEnumerable<SecurityId> secIds, out List<Message> result)
	{
		var emu = new MarketEmulator(new CollectionSecurityProvider(secIds.Select(id => new Security { Id = id.ToStringId() })), new CollectionPortfolioProvider([Portfolio.CreateSimulator()]), new InMemoryExchangeInfoProvider(), new IncrementalIdGenerator()) { VerifyMode = true };
		emu.RandomProvider = new MockRandomProvider();
		var result2 = new List<Message>();
		emu.NewOutMessageAsync += (m, ct) => { result2.Add(m); return default; };
		result = result2;
		return emu;
	}

	private const string _pfName = Messages.Extensions.SimulatorPortfolioName;
	private static readonly IdGenerator _idGenerator = new IncrementalIdGenerator();

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

	[TestMethod]
	public async Task OrderMatcher()
	{
		static ExecutionMessage CreateQuote(Sides side, decimal price, decimal volume, SecurityId secId)
		{
			return new ExecutionMessage
			{
				LocalTime = DateTime.UtcNow,
				SecurityId = secId,
				Side = side,
				OrderPrice = price,
				OrderVolume = volume,
				DataTypeEx = DataType.OrderLog,
			};
		}

		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);

		await emu.SendInMessageAsync(CreateQuote(Sides.Buy, 90, 1, id), CancellationToken);
		await emu.SendInMessageAsync(CreateQuote(Sides.Buy, 91, 1, id), CancellationToken);
		await emu.SendInMessageAsync(CreateQuote(Sides.Buy, 92, 1, id), CancellationToken);
		await emu.SendInMessageAsync(CreateQuote(Sides.Buy, 93, 1, id), CancellationToken);
		await emu.SendInMessageAsync(CreateQuote(Sides.Buy, 94, 1, id), CancellationToken);

		await emu.SendInMessageAsync(CreateQuote(Sides.Sell, 96, 1, id), CancellationToken);
		await emu.SendInMessageAsync(CreateQuote(Sides.Sell, 97, 1, id), CancellationToken);
		await emu.SendInMessageAsync(CreateQuote(Sides.Sell, 98, 1, id), CancellationToken);
		await emu.SendInMessageAsync(CreateQuote(Sides.Sell, 99, 1, id), CancellationToken);
		await emu.SendInMessageAsync(CreateQuote(Sides.Sell, 100, 1, id), CancellationToken);

		await emu.SendInMessageAsync(new ExecutionMessage
		{
			LocalTime = DateTime.UtcNow,
			SecurityId = id,
			Side = Sides.Buy,
			TransactionId = _idGenerator.GetNextId(),
			OrderPrice = 96,
			OrderVolume = 2,
			PortfolioName = "test",
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		}, CancellationToken);

		res.Count.AssertEqual(6);
		var firstTrade = res.OfType<ExecutionMessage>().Single(m => m.HasTradeInfo());
		firstTrade.TradePrice.AssertEqual(96m);
		firstTrade.TradeVolume.AssertEqual(1m);
		var firstOrder = res.OfType<ExecutionMessage>().Last(m => m.HasOrderInfo && !m.HasTradeInfo());
		firstOrder.Balance.AssertEqual(1m);
		firstOrder.OrderState.AssertEqual(OrderStates.Active);

		await emu.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = "test",
			LocalTime = DateTime.UtcNow,
		}
		.Add(PositionChangeTypes.BeginValue, 100000000m)
		.Add(PositionChangeTypes.CurrentValue, 100000000m), CancellationToken);

		await emu.SendInMessageAsync(new ExecutionMessage
		{
			LocalTime = DateTime.UtcNow,
			SecurityId = id,
			Side = Sides.Buy,
			TransactionId = _idGenerator.GetNextId(),
			OrderPrice = 96,
			OrderVolume = 2,
			PortfolioName = "test",
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		}, CancellationToken);

		res.Count.AssertEqual(9);
	}

	/// <summary>
	/// When the emulator only gets tick (trade) data it synthesizes the order book from ticks.
	/// A market order must fill near the current price, not at a stale early-tick level. Reproduces
	/// the bug where the synthesized book accumulates levels and never clears them, so the best
	/// bid/ask drift to historical extremes and fills happen far from the market.
	/// </summary>
	[TestMethod]
	public async Task TickMarketOrderFillsAtCurrentPriceNotStaleExtreme()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		// fund the simulated portfolio
		await emu.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}
		.Add(PositionChangeTypes.BeginValue, 100000000m)
		.Add(PositionChangeTypes.CurrentValue, 100000000m), CancellationToken);

		// feed a rising tick sequence; with no order book data the emulator builds the book from ticks
		decimal[] prices = [1000, 1100, 1200, 1300, 1400, 1500];

		foreach (var p in prices)
		{
			now = now.AddSeconds(1);

			await emu.SendInMessageAsync(new ExecutionMessage
			{
				SecurityId = id,
				LocalTime = now,
				ServerTime = now,
				DataTypeEx = DataType.Ticks,
				TradePrice = p,
				TradeVolume = 10,
			}, CancellationToken);
		}

		now = now.AddSeconds(1);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var fill = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		fill.AssertNotNull("market order did not fill");

		Console.WriteLine($"last tick={prices[^1]} fillPrice={fill.TradePrice}");

		// fill must be near the current price (~1500), not the stale first-tick level (~1000)
		IsTrue(fill.TradePrice >= 1400m, $"market buy filled at stale price {fill.TradePrice}, expected near {prices[^1]}");
	}

	/// <summary>
	/// Same problem for the Level1-synthesized order book: a rising best bid/ask must move the
	/// book, not accumulate stale levels, so a market order fills near the current price instead
	/// of an early-quote extreme.
	/// </summary>
	[TestMethod]
	public async Task Level1MarketOrderFillsAtCurrentPriceNotStaleExtreme()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		await emu.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}
		.Add(PositionChangeTypes.BeginValue, 100000000m)
		.Add(PositionChangeTypes.CurrentValue, 100000000m), CancellationToken);

		// feed a rising best bid/ask via Level1
		decimal[] mids = [1000, 1100, 1200, 1300, 1400, 1500];

		foreach (var m in mids)
		{
			now = now.AddSeconds(1);

			await emu.SendInMessageAsync(new Level1ChangeMessage
			{
				SecurityId = id,
				LocalTime = now,
				ServerTime = now,
			}
			.Add(Level1Fields.BestBidPrice, m - 1)
			.Add(Level1Fields.BestAskPrice, m + 1)
			.Add(Level1Fields.BestBidVolume, 10m)
			.Add(Level1Fields.BestAskVolume, 10m), CancellationToken);
		}

		now = now.AddSeconds(1);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var fill = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		fill.AssertNotNull("market order did not fill");

		Console.WriteLine($"last mid={mids[^1]} fillPrice={fill.TradePrice}");

		// fill must be near the current ask (~1501), not the stale first-quote ask (~1001)
		IsTrue(fill.TradePrice >= 1400m, $"market buy filled at stale price {fill.TradePrice}, expected near {mids[^1]}");
	}

	/// <summary>
	/// A resting limit order must be swept by ticks that trade through its price. Reproduces the
	/// residual bug where a buy limit left below the market is not executed as the price falls
	/// through it, so it lingers in the book and later fills at a price the market has left behind.
	/// </summary>
	[TestMethod]
	public async Task FallingTickSweepsRestingBuyLimit()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		await emu.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}
		.Add(PositionChangeTypes.BeginValue, 100000000m)
		.Add(PositionChangeTypes.CurrentValue, 100000000m), CancellationToken);

		async Task Tick(decimal price)
		{
			now = now.AddSeconds(1);
			await emu.SendInMessageAsync(new ExecutionMessage
			{
				SecurityId = id,
				LocalTime = now,
				ServerTime = now,
				DataTypeEx = DataType.Ticks,
				TradePrice = price,
				TradeVolume = 10,
			}, CancellationToken);
		}

		// establish the market around 1100
		await Tick(1100);

		// rest a buy limit well below the market
		now = now.AddSeconds(1);
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 1050,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		// price falls and trades through 1050
		foreach (var p in new[] { 1090m, 1070m, 1050m, 1030m, 1000m })
			await Tick(p);

		var fill = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());

		fill.AssertNotNull("resting buy limit was not swept by ticks trading through its price");

		Console.WriteLine($"buy limit fill={fill.TradePrice}");

		// it should fill at or below its limit, in the range that actually traded
		IsTrue(fill.TradePrice <= 1050m && fill.TradePrice >= 1000m, $"buy limit filled at off-market price {fill.TradePrice}");
	}

	[TestMethod]
	public async Task LimitBuyPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.Find(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public async Task LimitSellPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.Find(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public async Task LimitBuyFOKNone()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task LimitBuyFOKFull()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 102,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
		m.TradePrice.AssertEqual(101m);
	}

	[TestMethod]
	public async Task LimitSellFOKNone()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task LimitSellFOKFull()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 98,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.MatchOrCancel,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
		m.TradePrice.AssertEqual(100m);
	}

	[TestMethod]
	public async Task LimitBuyIOCFull()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task LimitBuyIOCPartial()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(5);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(10);
	}

	[TestMethod]
	public async Task LimitBuyIOCNone()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(15);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNull();
	}

	[TestMethod]
	public async Task LimitSellIOCFull()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task LimitSellIOCPartial()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 100,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(5);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(10);
	}

	[TestMethod]
	public async Task LimitSellIOCNone()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 101,
			Volume = 15,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.CancelBalance,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(15);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNull();
	}

	[TestMethod]
	public async Task LimitBuyPostOnlyOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 99,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		res.Clear();

		reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public async Task LimitSellPostOnlyOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 102,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		res.Clear();

		reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			PostOnly = true,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public async Task MarketBuyPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task MarketSellPutInQueueOrderBook()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Volume = 1,
			OrderType = OrderTypes.Market,
			TimeInForce = TimeInForce.PutInQueue,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task ExpiryDateLimitOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		var expiry = DateTime.UtcNow.AddDays(1);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			TillDate = expiry,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		res.Clear();
		await emu.SendInMessageAsync(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = expiry.AddSeconds(1),
			ServerTime = expiry.AddSeconds(1),
			DataTypeEx = DataType.Ticks,
			TradePrice = 105,
			TradeVolume = 2
		}, CancellationToken);
		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public async Task ExpiryDateInvalidLimitOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow.AddDays(1);
		var expiry = DateTime.UtcNow;

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			OrderType = OrderTypes.Limit,
			TillDate = expiry,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public async Task ReplaceOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
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
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

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

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == replace.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public async Task ReplaceOrderAndMatch()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
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
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

		var replace = new OrderReplaceMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = _idGenerator.GetNextId(),
			OriginalTransactionId = reg.TransactionId,
			OldOrderId = 1,
			Side = Sides.Buy,
			Price = 101,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};

		res.Clear();

		await emu.SendInMessageAsync(replace, CancellationToken);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId);
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == replace.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == replace.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(replace.Volume);
	}

	[TestMethod]
	public async Task CancelOrder()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
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
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Active);

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
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertNotNull();
		m.Balance.AssertEqual(m.OrderVolume);
	}

	[TestMethod]
	public async Task CandleExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await emu.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = _idGenerator.GetNextId(),
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityId = id,
			IsSubscribe = true,
		}, CancellationToken);
		await emu.SendInMessageAsync(new TimeFrameCandleMessage
		{
			SecurityId = id,
			OpenTime = now.AddMinutes(-5),
			CloseTime = now,
			OpenPrice = 100,
			HighPrice = 105,
			LowPrice = 95,
			ClosePrice = 104,
			TotalVolume = 100
		}, CancellationToken);
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 104,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task TickExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		await emu.SendInMessageAsync(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now,
			DataTypeEx = DataType.Ticks,
			TradePrice = 105,
			TradeVolume = 2
		}, CancellationToken);
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 105,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task Level1Execution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		await emu.SendInMessageAsync(new Level1ChangeMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now
		}
		.Add(Level1Fields.BestBidPrice, 104m)
		.Add(Level1Fields.BestAskPrice, 105m)
		.Add(Level1Fields.BestBidVolume, 1m)
		.Add(Level1Fields.BestAskVolume, 2m), CancellationToken);

		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 105,
			Volume = 2,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task OrderLogExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		await emu.SendInMessageAsync(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now,
			DataTypeEx = DataType.OrderLog,
			OrderPrice = 106,
			OrderVolume = 4,
			Side = Sides.Buy
		}, CancellationToken);
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 105,
			Volume = 4,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task MarketOrderOnTickExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		await emu.SendInMessageAsync(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now,
			DataTypeEx = DataType.Ticks,
			TradePrice = 107,
			TradeVolume = 1
		}, CancellationToken);
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task MarketOrderOnOrderLogExecution()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		await emu.SendInMessageAsync(new ExecutionMessage
		{
			SecurityId = id,
			LocalTime = now,
			ServerTime = now,
			DataTypeEx = DataType.OrderLog,
			OrderPrice = 108,
			OrderVolume = 2,
			Side = Sides.Sell
		}, CancellationToken);
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Volume = 2,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		var m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(2);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNull();

		res.Clear();

		reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 2,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && !em.HasTradeInfo());
		m.AssertNotNull();
		m.OrderState.AssertEqual(OrderStates.Done);
		m.Balance.AssertEqual(0);

		m = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		m.AssertNotNull();
		m.TradeVolume.AssertEqual(reg.Volume);
	}

	[TestMethod]
	public async Task OrderGroupCancelOrders()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		// Create two limit orders
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

		// Check that orders are active
		var m1 = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg1.TransactionId);
		m1.AssertNotNull();
		m1.OrderState.AssertEqual(OrderStates.Active);

		var m2 = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg2.TransactionId);
		m2.AssertNotNull();
		m2.OrderState.AssertEqual(OrderStates.Active);

		res.Clear();

		now = now.AddSeconds(1);

		// Check active orders via OrderStatusMessage
		await emu.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Active)
			.AssertEqual(2);

		res.Clear();

		now = now.AddSeconds(1);

		// Cancel all orders
		await emu.SendInMessageAsync(new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			PortfolioName = _pfName,
			Mode = OrderGroupCancelModes.CancelOrders,
		}, CancellationToken);

		// Check that both orders are cancelled
		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Done)
			.AssertEqual(2);

		res.Clear();

		now = now.AddSeconds(1);

		// Verify no active orders remain
		await emu.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Active)
			.AssertEqual(0);
	}

	[TestMethod]
	public async Task OrderGroupClosePositions()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		// Create a long position by executing a buy order
		var reg = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg, CancellationToken);

		// Verify the order executed
		var trade = (ExecutionMessage)res.FindLast(x => x is ExecutionMessage em && em.OriginalTransactionId == reg.TransactionId && em.HasTradeInfo());
		trade.AssertNotNull();
		trade.TradeVolume.AssertEqual(10);

		res.Clear();

		now = now.AddSeconds(1);

		// Check open positions via PortfolioLookupMessage
		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<PositionChangeMessage>()
			.Single(x => x.SecurityId == id)
			.TryGetDecimal(PositionChangeTypes.CurrentValue)
			.AssertEqual(10);

		res.Clear();

		now = now.AddSeconds(1);

		// Close all positions
		await emu.SendInMessageAsync(new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			PortfolioName = _pfName,
			Mode = OrderGroupCancelModes.ClosePositions,
		}, CancellationToken);

		// Check that a closing order was created (sell order to close long position)
		res.OfType<ExecutionMessage>().Count(x => x.OrderState == OrderStates.Done).AssertEqual(1);

		res.Clear();

		now = now.AddSeconds(1);

		// Verify no open positions remain after close
		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<PositionChangeMessage>()
			.Count(x => x.SecurityId == id)
			.AssertEqual(0);
	}

	[TestMethod]
	public async Task OrderGroupClosePositionsWithSecurityFilter()
	{
		var id1 = Helper.CreateSecurityId();
		var id2 = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents([id1, id2], out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id1, now);
		await AddBookAsync(emu, id2, now);

		// Create a long position for id1
		var reg1 = new OrderRegisterMessage
		{
			SecurityId = id1,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg1, CancellationToken);

		// Create a long position for id2
		var reg2 = new OrderRegisterMessage
		{
			SecurityId = id2,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg2, CancellationToken);

		res.Clear();

		now = now.AddSeconds(1);

		// Check open positions via PortfolioLookupMessage
		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<PositionChangeMessage>()
			.Count(x => x.SecurityId == id1 || x.SecurityId == id2)
			.AssertEqual(2);

		res.Clear();

		now = now.AddSeconds(1);

		// Close only positions for id1
		await emu.SendInMessageAsync(new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			PortfolioName = _pfName,
			SecurityId = id1,
			Mode = OrderGroupCancelModes.ClosePositions,
		}, CancellationToken);

		// Check that a closing order was created for id1
		res.OfType<ExecutionMessage>().Count(x => x.OrderState == OrderStates.Done).AssertEqual(1);

		res.Clear();

		now = now.AddSeconds(1);

		// Verify that position for id1 is closed but id2 remains open
		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<PositionChangeMessage>()
			.Count(x => x.SecurityId == id1)
			.AssertEqual(0);

		res.OfType<PositionChangeMessage>()
			.Single(x => x.SecurityId == id2)
			.TryGetDecimal(PositionChangeTypes.CurrentValue)
			.AssertEqual(5);
	}

	[TestMethod]
	public async Task OrderGroupClosePositionsWithSideFilter()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		// Create a long position (buy)
		var reg1 = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg1, CancellationToken);

		res.Clear();

		now = now.AddSeconds(1);

		// Create a short position (sell)
		var reg2 = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg2, CancellationToken);

		res.Clear();

		now = now.AddSeconds(1);

		// Check open positions via PortfolioLookupMessage
		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		// Net position should be 10 - 5 = 5 (long)
		res.OfType<PositionChangeMessage>()
			.Single(x => x.SecurityId == id)
			.TryGetDecimal(PositionChangeTypes.CurrentValue)
			.AssertEqual(5);

		res.Clear();

		now = now.AddSeconds(1);

		// Close only long positions (Side = Sides.Buy means close long positions)
		await emu.SendInMessageAsync(new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			PortfolioName = _pfName,
			Side = Sides.Buy,
			Mode = OrderGroupCancelModes.ClosePositions,
		}, CancellationToken);

		// Check that a closing sell order was created to close long position
		res.OfType<ExecutionMessage>().Count(x => x.OrderState == OrderStates.Done).AssertEqual(1);

		res.Clear();

		now = now.AddSeconds(1);

		// Verify that position is now closed
		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<PositionChangeMessage>()
			.Count(x => x.SecurityId == id)
			.AssertEqual(0);
	}

	[TestMethod]
	public async Task OrderGroupCancelAndClose()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		// Create a limit order that stays in the book
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

		now = now.AddSeconds(1);

		// Create a position by executing a buy order
		var reg2 = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 101,
			Volume = 10,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg2, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Done)
			.AssertEqual(1);

		res.Clear();

		now = now.AddSeconds(1);

		res.Clear();

		await emu.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Active)
			.AssertEqual(1);

		res.Clear();

		now = now.AddSeconds(1);

		// Check open positions via PortfolioLookupMessage
		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<PositionChangeMessage>()
			.Single(x => x.SecurityId == id)
			.TryGetDecimal(PositionChangeTypes.CurrentValue)
			.AssertEqual(10);

		res.Clear();

		now = now.AddSeconds(1);

		// Cancel all orders AND close all positions
		await emu.SendInMessageAsync(new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			PortfolioName = _pfName,
			Mode = OrderGroupCancelModes.CancelOrders | OrderGroupCancelModes.ClosePositions,
		}, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Done)
			.AssertEqual(2);

		res.Clear();

		now = now.AddSeconds(1);

		// Verify no open positions remain after close
		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<PositionChangeMessage>()
			.Count(x => x.SecurityId == id)
			.AssertEqual(0);
	}

	[TestMethod]
	public async Task OrderGroupCancelOrdersWithSecurityFilter()
	{
		var id1 = Helper.CreateSecurityId();
		var id2 = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents([id1, id2], out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id1, now);
		await AddBookAsync(emu, id2, now);

		// Create orders for two different securities
		var reg1 = new OrderRegisterMessage
		{
			SecurityId = id1,
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
			SecurityId = id2,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Price = 99,
			Volume = 3,
			OrderType = OrderTypes.Limit,
			PortfolioName = _pfName,
		};
		await emu.SendInMessageAsync(reg2, CancellationToken);

		res.Clear();

		now = now.AddSeconds(1);

		// Check active orders via OrderStatusMessage
		await emu.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Active)
			.AssertEqual(2);

		res.Clear();

		now = now.AddSeconds(1);

		// Cancel only orders for id1
		await emu.SendInMessageAsync(new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			PortfolioName = _pfName,
			SecurityId = id1,
			Mode = OrderGroupCancelModes.CancelOrders,
		}, CancellationToken);

		// Check that only order for id1 is cancelled
		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Done)
			.AssertEqual(1);

		res.Clear();

		now = now.AddSeconds(1);

		// Verify that order for id2 is still active
		await emu.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Active && x.SecurityId == id2)
			.AssertEqual(1);
	}

	[TestMethod]
	public async Task OrderGroupCancelOrdersWithSideFilter()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;
		await AddBookAsync(emu, id, now);

		// Create buy and sell orders
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

		now = now.AddSeconds(1);

		// Check active orders via OrderStatusMessage
		await emu.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Active)
			.AssertEqual(2);

		res.Clear();

		now = now.AddSeconds(1);

		// Cancel only buy orders
		await emu.SendInMessageAsync(new OrderGroupCancelMessage
		{
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			PortfolioName = _pfName,
			Side = Sides.Buy,
			Mode = OrderGroupCancelModes.CancelOrders,
		}, CancellationToken);

		// Check that only buy order is cancelled
		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Done && x.Side == Sides.Buy)
			.AssertEqual(1);

		res.Clear();

		now = now.AddSeconds(1);

		// Verify that sell order is still active
		await emu.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = _idGenerator.GetNextId(),
		}, CancellationToken);

		res.OfType<ExecutionMessage>()
			.Count(x => x.OrderState == OrderStates.Active && x.Side == Sides.Sell)
			.AssertEqual(1);
	}
}
