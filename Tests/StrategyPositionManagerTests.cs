namespace StockSharp.Tests;

[TestClass]
public class StrategyPositionManagerTests
{
	private static (Order order, Security sec, Portfolio pf) CreateOrder(Sides side, decimal volume)
	{
		var sec = new Security { Id = "TEST" };
		var pf = new Portfolio { Name = "TEST_PF" };

		var order = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = side,
			Volume = volume,
			Balance = volume, // nothing matched yet
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};

		return (order, sec, pf);
	}

	[TestMethod]
	public void IncrementalAveragePnLCommissionFlow()
	{
		var mgr = new StrategyPositionManager(() => "STRAT");
		Position lastPos = null;
		bool? lastIsNew = null;
		mgr.PositionProcessed += (p, n) => { lastPos = p; lastIsNew = n; };

		// 1) BUY order partial fills
		var (buy, sec, pf) = CreateOrder(Sides.Buy, 10m);

		// initial call (no matches) - ignored
		mgr.ProcessOrder(buy);
		lastPos.AssertNull(); // No position should be created before any fill.

		// First partial fill: 3 @100, commission 1
		buy.Balance = 7m; // matched 3
		buy.AveragePrice = 100m; // cumulative avg
		buy.Commission = 1m; // cumulative commission
		mgr.ProcessOrder(buy);

		lastPos.AssertNotNull();
		(lastIsNew == true).AssertTrue(); // First fill must create position.
		lastPos.CurrentValue.AssertEqual(3m);
		lastPos.AveragePrice.AssertEqual(100m);
		(lastPos.RealizedPnL ?? 0m).AssertEqual(0m);
		(lastPos.Commission ?? 0m).AssertEqual(1m);

		// Second partial fill: +5 @110 (new cumulative 8 @106.25), commission -> 1.8
		buy.Balance = 2m; // matched now 8
		buy.AveragePrice = 106.25m; // cumulative avg ((3*100 + 5*110)/8)
		buy.Commission = 1.8m; // cumulative
		mgr.ProcessOrder(buy);

		(lastIsNew == false).AssertTrue(); // Second fill updates existing position.
		lastPos.CurrentValue.AssertEqual(8m);
		lastPos.AveragePrice.AssertEqual(106.25m);
		(lastPos.RealizedPnL ?? 0m).AssertEqual(0m);
		(lastPos.Commission ?? 0m).AssertEqual(1.8m);

		// Duplicate (no change) - should not alter
		mgr.ProcessOrder(buy);
		lastPos.CurrentValue.AssertEqual(8m);
		lastPos.AveragePrice.AssertEqual(106.25m);

		// 2) Partial close: SELL 5 @120 commission delta 0.5 => cumulative commissions 1.8 + 0.5 = 2.3
		var sell = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Sell,
			Volume = 5m,
			Balance = 0m, // fully matched
			AveragePrice = 120m,
			Commission = 0.5m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};

		mgr.ProcessOrder(sell);
		// Realized PnL for closed 5: (120 - 106.25)*5 = 68.75
		lastPos.CurrentValue.AssertEqual(3m); // 8 - 5
		lastPos.AveragePrice.AssertEqual(106.25m); // unchanged after partial close
		Math.Round(lastPos.RealizedPnL ?? 0, 5).AssertEqual(68.75m);
		Math.Round(lastPos.Commission ?? 0, 5).AssertEqual(2.3m);

		// 3) Reversal: SELL 10 @105 commission 1.2 -> delta commission 1.2
		var sellRev = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Sell,
			Volume = 10m,
			Balance = 0m,
			AveragePrice = 105m,
			Commission = 1.2m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};

		mgr.ProcessOrder(sellRev);
		// Additional realized: closing remaining 3 long at 105 => (105 - 106.25)*3 = -3.75
		// Total realized = 68.75 - 3.75 = 65.0
		lastPos.CurrentValue.AssertEqual(-7m); // reversed to short 7
		lastPos.AveragePrice.AssertEqual(105m); // new basis
		Math.Round(lastPos.RealizedPnL ?? 0, 5).AssertEqual(65m);
		Math.Round(lastPos.Commission ?? 0, 5).AssertEqual(3.5m); // 2.3 + 1.2

		// 4) Full close of short: BUY 7 @100
		var buyClose = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Buy,
			Volume = 7m,
			Balance = 0m,
			AveragePrice = 100m,
			Commission = 0.2m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};

		mgr.ProcessOrder(buyClose);
		// Closing 7 short: average short 105, cover at 100 => PnL += (105 - 100)*7 = 35
		lastPos.CurrentValue.AssertEqual(0m);
		lastPos.AveragePrice.AssertNull(); // Average should reset after flat.
		Math.Round(lastPos.CurrentPrice ?? 0m, 5).AssertEqual(100m);
		Math.Round(lastPos.RealizedPnL ?? 0m, 5).AssertEqual(100m); // 65 + 35
	}

	[TestMethod]
	public void ShortPositionFlow()
	{
		var mgr = new StrategyPositionManager(() => "SHORT_STRAT");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (sell, sec, pf) = CreateOrder(Sides.Sell, 10m);

		// Open short: SELL 10 @100
		sell.Balance = 0m;
		sell.AveragePrice = 100m;
		sell.Commission = 0.5m;
		mgr.ProcessOrder(sell);

		lastPos.CurrentValue.AssertEqual(-10m);
		lastPos.AveragePrice.AssertEqual(100m);
		(lastPos.RealizedPnL ?? 0m).AssertEqual(0m);
		(lastPos.Commission ?? 0m).AssertEqual(0.5m);

		// Partial cover: BUY 4 @95
		var buy = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Buy,
			Volume = 4m,
			Balance = 0m,
			AveragePrice = 95m,
			Commission = 0.2m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(buy);

		// Realized PnL: (100 - 95) * 4 = 20
		lastPos.CurrentValue.AssertEqual(-6m);
		lastPos.AveragePrice.AssertEqual(100m);
		Math.Round(lastPos.RealizedPnL ?? 0, 5).AssertEqual(20m);
		Math.Round(lastPos.Commission ?? 0, 5).AssertEqual(0.7m);
	}

	[TestMethod]
	public void BlockedVolumeAndOrderCounts()
	{
		var mgr = new StrategyPositionManager(() => "BLOCK_STRAT");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (buy1, sec, pf) = CreateOrder(Sides.Buy, 10m);
		buy1.State = OrderStates.Active;

		// Register active buy order
		mgr.ProcessOrder(buy1);

		lastPos.AssertNotNull();
		lastPos.BlockedValue.AssertEqual(10m); // Blocked buy = 10
		lastPos.BuyOrdersCount.AssertEqual(1);
		lastPos.SellOrdersCount.AssertNull();

		// Add another buy order
		var buy2 = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Buy,
			Volume = 5m,
			Balance = 5m,
			State = OrderStates.Active,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(buy2);

		lastPos.BlockedValue.AssertEqual(15m); // 10 + 5
		lastPos.BuyOrdersCount.AssertEqual(2);

		// Add sell order
		var sell = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Sell,
			Volume = 7m,
			Balance = 7m,
			State = OrderStates.Active,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(sell);

		lastPos.BlockedValue.AssertEqual(8m); // 15 buy - 7 sell
		lastPos.BuyOrdersCount.AssertEqual(2);
		lastPos.SellOrdersCount.AssertEqual(1);

		// Partial fill on buy1: matched 3, balance 7
		buy1.Balance = 7m;
		buy1.AveragePrice = 100m;
		mgr.ProcessOrder(buy1);

		lastPos.BlockedValue.AssertEqual(5m); // 7 + 5 - 7
		lastPos.BuyOrdersCount.AssertEqual(2);

		// Cancel buy2
		buy2.State = OrderStates.Done;
		buy2.Balance = 0m;
		mgr.ProcessOrder(buy2);

		lastPos.BlockedValue.AssertEqual(0m); // 7 - 7
		lastPos.BuyOrdersCount.AssertEqual(1);
		lastPos.SellOrdersCount.AssertEqual(1);

		// Cancel sell
		sell.State = OrderStates.Done;
		sell.Balance = 0m;
		mgr.ProcessOrder(sell);

		lastPos.BlockedValue.AssertEqual(7m); // Only buy1 remains
		lastPos.BuyOrdersCount.AssertEqual(1);
		lastPos.SellOrdersCount.AssertNull();
	}

	[TestMethod]
	public void CurrentPriceUpdate()
	{
		var mgr = new StrategyPositionManager(() => "PRICE_STRAT");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (buy, sec, pf) = CreateOrder(Sides.Buy, 10m);
		buy.Balance = 0m;
		buy.AveragePrice = 100m;
		mgr.ProcessOrder(buy);

		lastPos.CurrentPrice.AssertNull(); // No current price yet

		var secId = sec.ToSecurityId();
		var now = DateTimeOffset.Now;
		mgr.UpdateCurrentPrice(secId, 105m, now, now);

		lastPos.CurrentPrice.AssertEqual(105m);
		lastPos.LastChangeTime.AssertEqual(now);

		// Update to different price
		var later = now.AddMinutes(1);
		mgr.UpdateCurrentPrice(secId, 110m, later, later);

		lastPos.CurrentPrice.AssertEqual(110m);
		lastPos.LastChangeTime.AssertEqual(later);
	}

	[TestMethod]
	public void MultiplePositions()
	{
		var mgr = new StrategyPositionManager(() => "MULTI");

		var sec1 = new Security { Id = "SEC1" };
		var sec2 = new Security { Id = "SEC2" };
		var pf1 = new Portfolio { Name = "PF1" };
		var pf2 = new Portfolio { Name = "PF2" };

		// Create position for SEC1/PF1
		var order1 = new Order
		{
			Security = sec1,
			Portfolio = pf1,
			Side = Sides.Buy,
			Volume = 10m,
			Balance = 0m,
			AveragePrice = 100m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(order1);

		// Create position for SEC2/PF1
		var order2 = new Order
		{
			Security = sec2,
			Portfolio = pf1,
			Side = Sides.Sell,
			Volume = 5m,
			Balance = 0m,
			AveragePrice = 200m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(order2);

		// Create position for SEC1/PF2
		var order3 = new Order
		{
			Security = sec1,
			Portfolio = pf2,
			Side = Sides.Buy,
			Volume = 7m,
			Balance = 0m,
			AveragePrice = 105m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(order3);

		mgr.Positions.Length.AssertEqual(3);

		var pos1 = mgr.TryGetPosition(sec1, pf1);
		pos1.AssertNotNull();
		pos1.CurrentValue.AssertEqual(10m);
		pos1.AveragePrice.AssertEqual(100m);

		var pos2 = mgr.TryGetPosition(sec2, pf1);
		pos2.AssertNotNull();
		pos2.CurrentValue.AssertEqual(-5m);
		pos2.AveragePrice.AssertEqual(200m);

		var pos3 = mgr.TryGetPosition(sec1, pf2);
		pos3.AssertNotNull();
		pos3.CurrentValue.AssertEqual(7m);
		pos3.AveragePrice.AssertEqual(105m);
	}

	[TestMethod]
	public void NullOrderThrowsException()
	{
		var mgr = new StrategyPositionManager(() => "NULL_TEST");
		Assert.ThrowsExactly<ArgumentNullException>(() => mgr.ProcessOrder(null));
	}

	[TestMethod]
	public void NullSecurityOrPortfolioThrows()
	{
		var mgr = new StrategyPositionManager(() => "NULL_TEST");

		Assert.ThrowsExactly<ArgumentNullException>(() => mgr.TryGetPosition(null, new Portfolio()));
		Assert.ThrowsExactly<ArgumentNullException>(() => mgr.TryGetPosition(new Security(), null));
		Assert.ThrowsExactly<ArgumentNullException>(() => mgr.SetPosition(null, new Portfolio(), 10m));
		Assert.ThrowsExactly<ArgumentNullException>(() => mgr.SetPosition(new Security(), null, 10m));
	}

	[TestMethod]
	public void OutOfOrderSnapshotsIgnored()
	{
		var mgr = new StrategyPositionManager(() => "OOO");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);

		// First fill: 5 matched
		order.Balance = 5m;
		order.AveragePrice = 100m;
		mgr.ProcessOrder(order);

		lastPos.CurrentValue.AssertEqual(5m);

		// Out-of-order snapshot: 3 matched (less than current)
		order.Balance = 7m;
		order.AveragePrice = 100m;
		mgr.ProcessOrder(order);

		// Position should remain unchanged
		lastPos.CurrentValue.AssertEqual(5m);
	}

	[TestMethod]
	public void ResetClearsAllData()
	{
		var mgr = new StrategyPositionManager(() => "RESET");

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);
		order.Balance = 0m;
		order.AveragePrice = 100m;
		mgr.ProcessOrder(order);

		mgr.Positions.Length.AssertEqual(1);
		mgr.TryGetPosition(sec, pf).AssertNotNull();

		mgr.Reset();

		mgr.Positions.Length.AssertEqual(0);
		mgr.TryGetPosition(sec, pf).AssertNull();
	}

	[TestMethod]
	public void SetPositionManually()
	{
		var mgr = new StrategyPositionManager(() => "MANUAL");

		var sec = new Security { Id = "TEST" };
		var pf = new Portfolio { Name = "PF" };

		mgr.SetPosition(sec, pf, 15m);

		var pos = mgr.TryGetPosition(sec, pf);
		pos.AssertNotNull();
		pos.CurrentValue.AssertEqual(15m);
		pos.AveragePrice.AssertNull(); // Not set by manual position

		// Update manual position
		mgr.SetPosition(sec, pf, -5m);
		pos.CurrentValue.AssertEqual(-5m);
	}

	[TestMethod]
	public void ZeroCommissionHandling()
	{
		var mgr = new StrategyPositionManager(() => "ZERO_COMM");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);
		order.Balance = 0m;
		order.AveragePrice = 100m;
		order.Commission = null; // No commission
		mgr.ProcessOrder(order);

		(lastPos.Commission ?? 0m).AssertEqual(0m);

		// Add order with commission
		var sell = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Sell,
			Volume = 10m,
			Balance = 0m,
			AveragePrice = 110m,
			Commission = 1.5m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(sell);

		(lastPos.Commission ?? 0m).AssertEqual(1.5m);
	}

	[TestMethod]
	public void NullAveragePriceHandling()
	{
		var mgr = new StrategyPositionManager(() => "NULL_PRICE");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);
		order.Balance = 0m;
		order.AveragePrice = null; // Null price
		mgr.ProcessOrder(order);

		lastPos.CurrentValue.AssertEqual(10m);
		(lastPos.AveragePrice ?? 0m).AssertEqual(0m); // Should default to 0
	}

	[TestMethod]
	public void StrategyIdAssignment()
	{
		var strategyId = "MY_STRATEGY_123";
		var mgr = new StrategyPositionManager(() => strategyId);
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (order, _, _) = CreateOrder(Sides.Buy, 10m);
		order.Balance = 0m;
		order.AveragePrice = 100m;
		mgr.ProcessOrder(order);

		lastPos.StrategyId.AssertEqual(strategyId);
	}

	[TestMethod]
	public void OrderStateTransitions()
	{
		var mgr = new StrategyPositionManager(() => "STATE_TEST");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);

		// Pending order
		order.State = OrderStates.Pending;
		mgr.ProcessOrder(order);
		lastPos.AssertNotNull();
		lastPos.BlockedValue.AssertEqual(10m);

		// Active order
		order.State = OrderStates.Active;
		mgr.ProcessOrder(order);
		lastPos.BlockedValue.AssertEqual(10m);

		// Partial fill
		order.Balance = 6m;
		order.AveragePrice = 100m;
		mgr.ProcessOrder(order);
		lastPos.BlockedValue.AssertEqual(6m);
		lastPos.CurrentValue.AssertEqual(4m);

		// Order done
		order.State = OrderStates.Done;
		order.Balance = 0m;
		order.AveragePrice = 101m;
		mgr.ProcessOrder(order);
		lastPos.BlockedValue.AssertNull();
		lastPos.CurrentValue.AssertEqual(10m);
	}

	[TestMethod]
	public void NegativeBalanceIgnored()
	{
		var mgr = new StrategyPositionManager(() => "NEG_BAL");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);
		order.Balance = 5m;
		order.AveragePrice = 100m;
		mgr.ProcessOrder(order);

		lastPos.CurrentValue.AssertEqual(5m);

		// Negative balance (invalid data) - should be ignored
		order.Balance = -1m;
		mgr.ProcessOrder(order);

		// Position unchanged
		lastPos.CurrentValue.AssertEqual(5m);
	}

	[TestMethod]
	public void CurrentPriceUpdateMultiplePositions()
	{
		var mgr = new StrategyPositionManager(() => "MULTI_PRICE");
		var positions = new List<Position>();
		mgr.PositionProcessed += (p, _) => positions.Add(p);

		var sec = new Security { Id = "AAPL" };
		var pf1 = new Portfolio { Name = "PF1" };
		var pf2 = new Portfolio { Name = "PF2" };

		// Create two positions with same security, different portfolios
		var order1 = new Order
		{
			Security = sec,
			Portfolio = pf1,
			Side = Sides.Buy,
			Volume = 10m,
			Balance = 0m,
			AveragePrice = 100m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(order1);

		var order2 = new Order
		{
			Security = sec,
			Portfolio = pf2,
			Side = Sides.Sell,
			Volume = 5m,
			Balance = 0m,
			AveragePrice = 100m,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
		};
		mgr.ProcessOrder(order2);

		positions.Clear();

		// Update current price for security
		var now = DateTimeOffset.Now;
		mgr.UpdateCurrentPrice(sec.ToSecurityId(), 105m, now, now);

		// Both positions should be updated
		positions.Count.AssertEqual(2);
		positions.All(p => p.CurrentPrice == 105m).AssertTrue();
	}
}
