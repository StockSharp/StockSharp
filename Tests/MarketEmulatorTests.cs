namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.MatchingEngine;

[TestClass]
public class MarketEmulatorTests : BaseTestClass
{
	private static IMarketEmulator CreateEmuWithEvents(SecurityId secId, out List<Message> result)
		=> CreateEmuWithEvents([secId], out result);

	private static IMarketEmulator CreateEmuWithEvents(IEnumerable<SecurityId> secIds, out List<Message> result)
	{
		var emu = new MarketEmulator(new CollectionSecurityProvider(secIds.Select(id => new Security { Id = id.ToStringId() })), new CollectionPortfolioProvider([Portfolio.CreateSimulator()]), new InMemoryExchangeInfoProvider(), new IncrementalIdGenerator()) { VerifyMode = true };
		emu.RandomProvider = new MockEmulationRandomizer();
		var result2 = new List<Message>();
		emu.NewOutMessageAsync += (m, ct) => { Helper.CheckPortfolioRowContract(m); result2.Add(m); return default; };
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
	public async Task CandleUpdates_MultipleTimeFrames_DoNotMoveTimeBackwards()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var dayStart = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc);
		var dailySubscriptionId = _idGenerator.GetNextId();
		var fiveMinuteSubscriptionId = _idGenerator.GetNextId();

		await emu.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = dailySubscriptionId,
			DataType2 = TimeSpan.FromDays(1).TimeFrame(),
			SecurityId = id,
			IsSubscribe = true,
			IsFinishedOnly = false,
		}, CancellationToken);

		await emu.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = fiveMinuteSubscriptionId,
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			SecurityId = id,
			IsSubscribe = true,
			IsFinishedOnly = false,
		}, CancellationToken);

		await emu.SendInMessageAsync(new TimeFrameCandleMessage
		{
			SecurityId = id,
			OriginalTransactionId = dailySubscriptionId,
			TypedArg = TimeSpan.FromDays(1),
			LocalTime = dayStart,
			OpenTime = dayStart,
			HighTime = dayStart.AddHours(10),
			LowTime = dayStart.AddHours(20),
			CloseTime = dayStart.AddDays(1),
			OpenPrice = 100,
			HighPrice = 110,
			LowPrice = 90,
			ClosePrice = 105,
			TotalVolume = 100,
			State = CandleStates.Finished,
		}, CancellationToken);

		await emu.SendInMessageAsync(new TimeFrameCandleMessage
		{
			SecurityId = id,
			OriginalTransactionId = fiveMinuteSubscriptionId,
			TypedArg = TimeSpan.FromMinutes(5),
			LocalTime = dayStart,
			OpenTime = dayStart,
			HighTime = dayStart.AddMinutes(2),
			LowTime = dayStart.AddMinutes(4),
			CloseTime = dayStart.AddMinutes(5),
			OpenPrice = 100,
			HighPrice = 102,
			LowPrice = 99,
			ClosePrice = 101,
			TotalVolume = 10,
			State = CandleStates.Finished,
		}, CancellationToken);

		res.Clear();
		await emu.SendInMessageAsync(new TimeMessage { LocalTime = dayStart.AddMinutes(5) }, CancellationToken);

		var intradayUpdates = res
			.OfType<TimeFrameCandleMessage>()
			.Where(c => c.State == CandleStates.Active)
			.ToArray();

		AreEqual(4, intradayUpdates.Length);
		IsTrue(intradayUpdates.All(c => c.LocalTime <= dayStart.AddMinutes(5)));
		IsFalse(res
			.OfType<TimeFrameCandleMessage>()
			.Any(c => c.TypedArg == TimeSpan.FromDays(1) && c.State == CandleStates.Finished),
			"The daily candle must remain pending while only five minutes of its interval have elapsed.");

		res.Clear();
		await emu.SendInMessageAsync(new TimeMessage { LocalTime = dayStart.AddDays(1) }, CancellationToken);

		var updates = intradayUpdates
			.Concat(res
				.OfType<TimeFrameCandleMessage>()
				.Where(c => c.State == CandleStates.Active))
			.ToArray();

		AreEqual(6, updates.Length);
		IsTrue(res
			.OfType<TimeFrameCandleMessage>()
			.Any(c => c.TypedArg == TimeSpan.FromDays(1) && c.State == CandleStates.Finished),
			"The daily candle must finish when its own interval closes.");

		for (var i = 1; i < updates.Length; i++)
		{
			var previous = updates[i - 1];
			var current = updates[i];

			IsTrue(previous.LocalTime <= current.LocalTime,
				$"Candle updates moved backwards from {previous.TypedArg} at {previous.LocalTime:O} " +
				$"to {current.TypedArg} at {current.LocalTime:O}.");
		}
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

	/// <summary>
	/// A book built from a long stream of prices follows the market: it stays bounded, and its touch
	/// is the last thing quoted rather than the first. A book that only grows one way ends up quoting
	/// the extremes of the session against each other.
	/// </summary>
	[TestMethod]
	public async Task ABookBuiltFromTicksFollowsTheMarket()
	{
		var secId = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(secId, out _);
		var book = ((MarketEmulator)emu).GetSecurityState(secId).OrderBook;
		var now = DateTime.UtcNow;

		for (var i = 0; i < 2000; i++)
		{
			now = now.AddSeconds(1);

			await emu.SendInMessageAsync(new ExecutionMessage
			{
				SecurityId = secId,
				LocalTime = now,
				ServerTime = now,
				DataTypeEx = DataType.Ticks,
				TradePrice = 1000 + i,
				TradeVolume = 10,
			}, CancellationToken);
		}

		IsTrue(book.BidLevels <= emu.Settings.MaxDepth, $"bid depth {book.BidLevels} is unbounded");
		IsTrue(book.AskLevels <= emu.Settings.MaxDepth, $"ask depth {book.AskLevels} is unbounded");

		IsTrue(book.BestBid?.price >= 2900m,
			$"the market walked up to 2999, so the bid must be near it, not at {book.BestBid?.price}");
		IsTrue(book.BestBid?.price < book.BestAsk?.price,
			$"a book cannot cross itself: bid {book.BestBid?.price}, ask {book.BestAsk?.price}");
	}

	/// <summary>
	/// The same of a book built from a stream of quotes: the touch a venue states is where the market
	/// is, not one more level to keep beside the ones it stated before.
	/// </summary>
	[TestMethod]
	public async Task ABookBuiltFromLevel1FollowsTheMarket()
	{
		var secId = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(secId, out _);
		var book = ((MarketEmulator)emu).GetSecurityState(secId).OrderBook;
		var now = DateTime.UtcNow;

		for (var i = 0; i < 2000; i++)
		{
			now = now.AddSeconds(1);

			await emu.SendInMessageAsync(new Level1ChangeMessage
			{
				SecurityId = secId,
				LocalTime = now,
				ServerTime = now,
			}
			.Add(Level1Fields.BestBidPrice, 1000m + i)
			.Add(Level1Fields.BestAskPrice, 1001m + i)
			.Add(Level1Fields.BestBidVolume, 10m)
			.Add(Level1Fields.BestAskVolume, 10m), CancellationToken);
		}

		IsTrue(book.BidLevels <= emu.Settings.MaxDepth, $"bid depth {book.BidLevels} is unbounded");
		IsTrue(book.AskLevels <= emu.Settings.MaxDepth, $"ask depth {book.AskLevels} is unbounded");

		AreEqual(2999m, book.BestBid?.price, "the last quote stated a bid of 2999");
		AreEqual(3000m, book.BestAsk?.price, "and an ask of 3000");
	}

	/// <summary>
	/// A fill emits two portfolio rows: the instrument position (volume + average price) and the
	/// account's cash. The position row must name the instrument that was traded. Emitting it under
	/// <see cref="SecurityId.Money"/> makes a lot quantity indistinguishable from a cash balance, so
	/// anyone persisting or displaying these rows reads a position as money — and the portfolio
	/// lookup, which does name the instrument, then disagrees with the live stream about the same
	/// account.
	/// </summary>
	[TestMethod]
	public async Task FillEmitsPositionRowUnderTheTradedSecurity()
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

		await AddBookAsync(emu, id, now);

		res.Clear();

		now = now.AddSeconds(1);

		await emu.SendInMessageAsync(new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		}, CancellationToken);

		// The two rows are told apart by what they carry: only the cash row reports realized PnL.
		var rows = res.OfType<PositionChangeMessage>().ToArray();

		var positionRow = rows.FirstOrDefault(m => !m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));
		positionRow.AssertNotNull("the fill must emit a position row");
		positionRow.SecurityId.AssertEqual(id, "the position row must name the traded instrument");
		positionRow.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(1m);

		var cashRow = rows.FirstOrDefault(m => m.Changes.ContainsKey(PositionChangeTypes.RealizedPnL));
		cashRow.AssertNotNull("the fill must emit a cash row");
		cashRow.SecurityId.AssertEqual(SecurityId.Money, "the cash row stays money");
	}

	/// <summary>
	/// A position fed in as an opening state must keep the average price it was given. Dropping it
	/// leaves the position at average price 0, and the first close then realizes a profit equal to
	/// the entire notional instead of the actual difference.
	/// </summary>
	[TestMethod]
	public async Task SeededPositionKeepsItsAveragePrice()
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
		.Add(PositionChangeTypes.BeginValue, 100000000m), CancellationToken);

		await emu.SendInMessageAsync(new PositionChangeMessage
		{
			SecurityId = id,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}
		.Add(PositionChangeTypes.BeginValue, 2m)
		.Add(PositionChangeTypes.AveragePrice, 100m), CancellationToken);

		res.Clear();

		await emu.SendInMessageAsync(new PortfolioLookupMessage
		{
			TransactionId = _idGenerator.GetNextId(),
			PortfolioName = _pfName,
			LocalTime = now,
			IsSubscribe = true,
		}, CancellationToken);

		var reported = res.OfType<PositionChangeMessage>().FirstOrDefault(m => m.SecurityId == id);
		reported.AssertNotNull("the seeded position must be reported back");
		reported.Changes[PositionChangeTypes.CurrentValue].To<decimal>().AssertEqual(2m);
		reported.Changes[PositionChangeTypes.AveragePrice].To<decimal>().AssertEqual(100m, "the seeded average price must survive");
	}

	/// <summary>
	/// An order that is already live somewhere else must be able to enter the engine as state, not
	/// as a request. The matcher is an internal venue: it is told what is outstanding when it comes
	/// up, and the messages that carry that are the ordinary execution reports the rest of the
	/// system already speaks. Feeding such a report through registration instead re-charges the
	/// account for an order it already owns, gives it a new identity, and can cross it against the
	/// book on the way in - so the order the engine ends up holding is not the order that exists.
	/// </summary>
	[TestMethod]
	public void ActiveOrderArrivesAsStateNotAsARequest()
	{
		var id = Helper.CreateSecurityId();
		var engine = new MatchingEngineAdapter();
		var now = DateTime.UtcNow;
		var results = new List<Message>();

		engine.ProcessMessage(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), results);

		results.Clear();

		// What the router hands over: a buy for 10 that has already been half filled elsewhere,
		// carrying the identity it is known by and the balance it actually has left.
		engine.ProcessMessage(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = id,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
			TransactionId = 42,
			OrderId = 555,
			OrderState = OrderStates.Active,
			OrderType = OrderTypes.Limit,
			Side = Sides.Buy,
			OrderPrice = 100,
			OrderVolume = 10,
			Balance = 5,
		}, results);

		// Taking it on is not an event anyone asked for: the order was not registered here, so no
		// registration report may go out for it.
		results.OfType<ExecutionMessage>().Any(m => m.HasOrderInfo && m.OriginalTransactionId == 42)
			.AssertFalse("adopting an order is not the same as accepting one");

		var book = engine.GetSecurityState(id).OrderBook;
		var resting = book.GetLevels(Sides.Buy).SelectMany(l => l.Orders).FirstOrDefault(o => o.TransactionId == 42);

		resting.AssertNotNull("the order must be standing in the book");
		resting.OrderId.AssertEqual(555L, "under the identity it is already known by");
		resting.Balance.AssertEqual(5m, "with what is left of it, not what it started as");
		resting.Volume.AssertEqual(10m);

		// And it must behave as a real resting order: an aggressor crosses it.
		results.Clear();
		now = now.AddSeconds(1);

		engine.ProcessMessage(new OrderRegisterMessage
		{
			SecurityId = id,
			PortfolioName = _pfName,
			LocalTime = now,
			TransactionId = 77,
			Side = Sides.Sell,
			Price = 100,
			Volume = 5,
			OrderType = OrderTypes.Limit,
		}, results);

		var trade = results.OfType<ExecutionMessage>().FirstOrDefault(m => m.HasTradeInfo);

		trade.AssertNotNull("an adopted order takes part in matching like any other");
		trade.TradePrice.AssertEqual(100m);
	}

	/// <summary>
	/// The same order can be described either way round. The report that registered it carries the
	/// registration as its own transaction; a later state report carries it as the transaction it
	/// answers. Reading only one of the two would let the other through as a fresh request - and a
	/// fresh request charges the account again and can cross the book on the way in.
	/// </summary>
	[TestMethod]
	public void AnAdoptedOrderIsRecognisedWhicheverFieldCarriesItsId()
	{
		var id = Helper.CreateSecurityId();
		var engine = new MatchingEngineAdapter();
		var now = DateTime.UtcNow;
		var results = new List<Message>();

		engine.ProcessMessage(new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
		}.Add(PositionChangeTypes.BeginValue, 1000000m), results);

		ExecutionMessage state(long transactionId, long originalTransactionId, long orderId) => new()
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = id,
			PortfolioName = _pfName,
			LocalTime = now,
			ServerTime = now,
			TransactionId = transactionId,
			OriginalTransactionId = originalTransactionId,
			OrderId = orderId,
			OrderState = OrderStates.Active,
			OrderType = OrderTypes.Limit,
			Side = Sides.Buy,
			OrderPrice = 100,
			OrderVolume = 10,
			Balance = 10,
		};

		results.Clear();

		engine.ProcessMessage(state(transactionId: 42, originalTransactionId: 0, orderId: 555), results);
		engine.ProcessMessage(state(transactionId: 0, originalTransactionId: 43, orderId: 556), results);

		results.OfType<ExecutionMessage>().Any(m => m.HasOrderInfo)
			.AssertFalse("neither shape is a request, so neither is answered");

		var book = engine.GetSecurityState(id).OrderBook;
		var resting = book.GetLevels(Sides.Buy).SelectMany(l => l.Orders).Select(o => o.TransactionId).ToArray();

		resting.Contains(42L).AssertTrue("the registration shape must be taken on");
		resting.Contains(43L).AssertTrue("and so must the state-report shape");
	}

	/// <summary>
	/// A control message is not data and must not be held to the data clock.
	/// </summary>
	/// <remarks>
	/// The emulator refuses data that moves time backwards, which is right: replaying an earlier
	/// bar after a later one would matter. But the stop signal is deliberately pushed through the
	/// same queue as a marker, so that it comes back only once everything ahead of it has been
	/// processed - and it carries the time the run was told to stop at, which by then is behind
	/// where the emulator has got to. Judging it as data turns an ordinary shutdown into an
	/// exception, and the run ends by throwing instead of by finishing.
	/// </remarks>
	[TestMethod]
	public async Task AStopSignalIsNotHeldToTheDataClock()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out _);

		var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var later = start.AddDays(1).AddMinutes(30);

		await AddBookAsync(emu, id, later);

		// The stop was scheduled for the start of the run; the emulator is already a day past it.
		await emu.SendInMessageAsync(new EmulationStateMessage
		{
			LocalTime = start,
			State = ChannelStates.Stopping,
		}, CancellationToken);
	}

	/// <summary>
	/// A market order names no price, so nothing of it can rest in the order book. Candle matching
	/// caps a fill at the candle's traded volume; queuing the balance left over puts a level at the
	/// order's own zero price into the book, and once the candle subscription ends and the book
	/// takes matching back, a later opposite order trades against that level at a price of zero.
	/// </summary>
	[TestMethod]
	public async Task CandleMarketOrderRemainderIsNotQueued()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		// Off, so the candle's traded volume is what the order can fill and there is a remainder
		// at all. With it on - the default - the order fills in full and leaves nothing to queue.
		emu.Settings.IncreaseDepthVolume = false;

		var candleSubscriptionId = _idGenerator.GetNextId();

		await emu.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = candleSubscriptionId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityId = id,
			IsSubscribe = true,
		}, CancellationToken);

		await emu.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = _idGenerator.GetNextId(),
			DataType2 = DataType.MarketDepth,
			SecurityId = id,
			IsSubscribe = true,
		}, CancellationToken);

		// The candle traded 0.32, far less than the order below asks for.
		await emu.SendInMessageAsync(new TimeFrameCandleMessage
		{
			SecurityId = id,
			OpenTime = now.AddMinutes(-5),
			CloseTime = now,
			OpenPrice = 100,
			HighPrice = 105,
			LowPrice = 95,
			ClosePrice = 104,
			TotalVolume = 0.32m,
		}, CancellationToken);

		var buy = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};

		await emu.SendInMessageAsync(buy, CancellationToken);

		var buyTrades = res
			.OfType<ExecutionMessage>()
			.Where(m => m.OriginalTransactionId == buy.TransactionId && m.HasTradeInfo())
			.ToArray();

		AreEqual(1, buyTrades.Length, "the candle fills the order once, for what it traded");
		AreEqual(0.32m, buyTrades[0].TradeVolume);

		var buyState = res
			.OfType<ExecutionMessage>()
			.Last(m => m.OriginalTransactionId == buy.TransactionId && m.HasOrderInfo && !m.HasTradeInfo());

		AreEqual(OrderStates.Done, buyState.OrderState, "a market order that cannot be filled further is done, not active");
		AreEqual(0.68m, buyState.Balance);

		foreach (var book in res.OfType<QuoteChangeMessage>())
		{
			IsFalse(book.Bids.Any(q => q.Price <= 0), "a market order must leave no bid at its own zero price");
			IsFalse(book.Asks.Any(q => q.Price <= 0), "a market order must leave no ask at its own zero price");
		}

		var position = ((MarketEmulator)emu).PortfolioManager.GetPortfolio(_pfName).GetPosition(id);

		AreEqual(0m, position.TotalBidsVolume, "the cancelled balance must hold nothing blocked against the account");

		// The candle subscription ends, so the book matches again and whatever the buy left in it is
		// reachable by the opposite order.
		await emu.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = _idGenerator.GetNextId(),
			OriginalTransactionId = candleSubscriptionId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityId = id,
			IsSubscribe = false,
		}, CancellationToken);

		var sell = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now.AddSeconds(1),
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Sell,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};

		await emu.SendInMessageAsync(sell, CancellationToken);

		var trades = res.OfType<ExecutionMessage>().Where(m => m.HasTradeInfo()).ToArray();

		IsFalse(trades.Any(m => m.TradePrice is not decimal price || price <= 0), "every trade carries a positive price");
		AreEqual(1, trades.Count(m => m.OriginalTransactionId == buy.TransactionId), "the first order trades only against the candle");

		var buyTradeIds = trades.Where(m => m.OriginalTransactionId == buy.TransactionId).Select(m => m.TradeId);
		var sellTradeIds = trades.Where(m => m.OriginalTransactionId == sell.TransactionId).Select(m => m.TradeId);

		IsFalse(buyTradeIds.Intersect(sellTradeIds).Any(), "the two orders must not be the two sides of one trade");
	}
	/// <summary>
	/// A candle says how much the market traded over that minute; it does not say how much an order
	/// is allowed to fill. That is what <see cref="MarketEmulatorSettings.IncreaseDepthVolume"/>
	/// decides, and it is on by default — the order book path tops the book up past its worst level
	/// so an order asking for more than the market holds still fills in full. Candle matching has to
	/// answer the same setting the same way. While it did not, one strategy filled completely on
	/// ticks and partially on candles, and was left holding the fraction the candle could not cover.
	/// </summary>
	[TestMethod]
	public async Task CandleMarketOrderFillsInFullWhenDepthVolumeIncreases()
	{
		var id = Helper.CreateSecurityId();
		var emu = CreateEmuWithEvents(id, out var res);
		var now = DateTime.UtcNow;

		IsTrue(emu.Settings.IncreaseDepthVolume, "the setting under test is the default one");

		await emu.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = _idGenerator.GetNextId(),
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityId = id,
			IsSubscribe = true,
		}, CancellationToken);

		// The candle traded 0.32, less than a third of what the order below asks for.
		await emu.SendInMessageAsync(new TimeFrameCandleMessage
		{
			SecurityId = id,
			OpenTime = now.AddMinutes(-5),
			CloseTime = now,
			OpenPrice = 100,
			HighPrice = 105,
			LowPrice = 95,
			ClosePrice = 104,
			TotalVolume = 0.32m,
		}, CancellationToken);

		var buy = new OrderRegisterMessage
		{
			SecurityId = id,
			LocalTime = now,
			TransactionId = _idGenerator.GetNextId(),
			Side = Sides.Buy,
			Volume = 1,
			OrderType = OrderTypes.Market,
			PortfolioName = _pfName,
		};

		await emu.SendInMessageAsync(buy, CancellationToken);

		var trades = res
			.OfType<ExecutionMessage>()
			.Where(m => m.OriginalTransactionId == buy.TransactionId && m.HasTradeInfo())
			.ToArray();

		AreEqual(1, trades.Length, "the order fills in one trade");
		AreEqual(1m, trades[0].TradeVolume, "the whole order fills, not the candle's own volume");

		var state = res
			.OfType<ExecutionMessage>()
			.Last(m => m.OriginalTransactionId == buy.TransactionId && m.HasOrderInfo && !m.HasTradeInfo());

		AreEqual(OrderStates.Done, state.OrderState);
		AreEqual(0m, state.Balance, "nothing is left to cancel");

		var position = ((MarketEmulator)emu).PortfolioManager.GetPortfolio(_pfName).GetPosition(id);

		AreEqual(1m, position.BeginValue + position.CurrentValue, "the strategy holds exactly what it ordered");
	}
	/// <summary>
	/// The reason the fill matters. A strategy that opens only from a flat position sends the same
	/// volume out and back; if each leg fills for whatever its own candle traded, the two fractions
	/// do not cancel and the position never returns to zero, so the strategy never opens again. It
	/// took two trades in a month of history to find that, which is why it is asserted here.
	/// </summary>
	[TestMethod]
	public async Task CandleRoundTripReturnsToFlat()
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

		// Two candles, each thinner than the order that trades against it, and thin by a different
		// amount - the fractions left over would not even cancel each other out.
		async Task TradeAsync(Sides side, decimal candleVolume, DateTime time)
		{
			await emu.SendInMessageAsync(new TimeFrameCandleMessage
			{
				SecurityId = id,
				OpenTime = time.AddMinutes(-1),
				CloseTime = time,
				OpenPrice = 100,
				HighPrice = 105,
				LowPrice = 95,
				ClosePrice = 104,
				TotalVolume = candleVolume,
			}, CancellationToken);

			await emu.SendInMessageAsync(new OrderRegisterMessage
			{
				SecurityId = id,
				LocalTime = time,
				TransactionId = _idGenerator.GetNextId(),
				Side = side,
				Volume = 1,
				OrderType = OrderTypes.Market,
				PortfolioName = _pfName,
			}, CancellationToken);
		}

		await TradeAsync(Sides.Buy, 0.414m, now);
		await TradeAsync(Sides.Sell, 0.446m, now.AddMinutes(1));

		var traded = res
			.OfType<ExecutionMessage>()
			.Where(m => m.HasTradeInfo())
			.Sum(m => (m.OriginSide ?? m.Side) == Sides.Sell ? -m.TradeVolume : m.TradeVolume);

		AreEqual(0m, traded, "buying one and selling one leaves nothing behind");

		var position = ((MarketEmulator)emu).PortfolioManager.GetPortfolio(_pfName).GetPosition(id);

		AreEqual(0m, position.BeginValue + position.CurrentValue, "a strategy that opens from flat can open again");
	}
}
