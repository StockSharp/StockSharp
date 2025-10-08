namespace StockSharp.Tests;

[TestClass]
public class StrategyPositionManagerTests
{
	private static long _tx;

	private static void AssertCalcFieldsNonNull(
		Position p,
		bool averagePriceCanBeNull = false,
		bool currentPriceCanBeNull = true,
		bool commissionCanBeNull = false,
		bool blockedValueCanBeNull = true,
		bool buyCountCanBeNull = true,
		bool sellCountCanBeNull = true)
	{
		p.AssertNotNull();
		p.RealizedPnL.AssertNotNull();
		p.LocalTime.AssertNotNull();
		p.LastChangeTime.AssertNotNull();
		if (!averagePriceCanBeNull) p.AveragePrice.AssertNotNull();
		if (!currentPriceCanBeNull) p.CurrentPrice.AssertNotNull();
		if (!commissionCanBeNull) p.Commission.AssertNotNull();
		if (!blockedValueCanBeNull) p.BlockedValue.AssertNotNull();
		if (!buyCountCanBeNull) p.BuyOrdersCount.AssertNotNull();
		if (!sellCountCanBeNull) p.SellOrdersCount.AssertNotNull();
	}

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
			State = OrderStates.Pending, // explicit initial state
			TransactionId = ++_tx,
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

		// initial call (no matches) - ignored (Pending)
		buy.State = OrderStates.Pending;
		mgr.ProcessOrder(buy);
		lastPos.AssertNull(); // No position should be created before any fill.

		// First partial fill: 3 @100, commission 1 (still Active)
		buy.Balance = 7m; // matched 3
		buy.AveragePrice = 100m; // cumulative avg
		buy.Commission = 1m; // cumulative commission
		buy.State = OrderStates.Active;
		mgr.ProcessOrder(buy);

		lastPos.AssertNotNull();
		(lastIsNew == true).AssertTrue(); // First fill must create position.
		lastPos.CurrentValue.AssertEqual(3m);
		lastPos.AveragePrice.AssertEqual(100m);
		(lastPos.RealizedPnL ?? 0m).AssertEqual(0m);
		(lastPos.Commission ?? 0m).AssertEqual(1m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: false, blockedValueCanBeNull: false, buyCountCanBeNull: false);

		// Second partial fill: +5 @110 (new cumulative 8 @106.25), commission -> 1.8
		buy.Balance = 2m; // matched now 8
		buy.AveragePrice = 106.25m; // cumulative avg ((3*100 + 5*110)/8)
		buy.Commission = 1.8m; // cumulative
		buy.State = OrderStates.Active;
		mgr.ProcessOrder(buy);

		(lastIsNew == false).AssertTrue(); // Second fill updates existing position.
		lastPos.CurrentValue.AssertEqual(8m);
		lastPos.AveragePrice.AssertEqual(106.25m);
		(lastPos.RealizedPnL ?? 0m).AssertEqual(0m);
		(lastPos.Commission ?? 0m).AssertEqual(1.8m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: false, blockedValueCanBeNull: false, buyCountCanBeNull: false);

		// Duplicate (no change) - should not alter
		mgr.ProcessOrder(buy);
		lastPos.CurrentValue.AssertEqual(8m);
		lastPos.AveragePrice.AssertEqual(106.25m);

		// 2) Partial close: SELL 5 @120 commission delta 0.5 => cumulative commissions 1.8 + 0.5 = 2.3 (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};

		mgr.ProcessOrder(sell);
		// Realized PnL for closed 5: (120 - 106.25)*5 = 68.75
		lastPos.CurrentValue.AssertEqual(3m); // 8 - 5
		lastPos.AveragePrice.AssertEqual(106.25m); // unchanged after partial close
		Math.Round(lastPos.RealizedPnL ?? 0, 5).AssertEqual(68.75m);
		Math.Round(lastPos.Commission ?? 0, 5).AssertEqual(2.3m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: false);

		// 3) Reversal: SELL 10 @105 commission 1.2 -> delta commission 1.2 (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};

		mgr.ProcessOrder(sellRev);
		// Additional realized: closing remaining 3 long at 105 => (105 - 106.25)*3 = -3.75
		// Total realized = 68.75 - 3.75 = 65.0
		lastPos.CurrentValue.AssertEqual(-7m); // reversed to short 7
		lastPos.AveragePrice.AssertEqual(105m); // new basis
		Math.Round(lastPos.RealizedPnL ?? 0, 5).AssertEqual(65m);
		Math.Round(lastPos.Commission ?? 0, 5).AssertEqual(3.5m); // 2.3 + 1.2
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: false);

		// 4) Full close of short: BUY 7 @100 (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};

		mgr.ProcessOrder(buyClose);
		// Closing 7 short: average short 105, cover at 100 => PnL += (105 - 100)*7 = 35
		lastPos.CurrentValue.AssertEqual(0m);
		lastPos.AveragePrice.AssertNull(); // Average should reset after flat.
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: true, currentPriceCanBeNull: true, commissionCanBeNull: false);

		// Set last market price to compute position market value (qty=0 -> value=0)
		var secId = sec.ToSecurityId();
		mgr.UpdateCurrentPrice(secId, 100m, buyClose.ServerTime, buyClose.LocalTime);
		Math.Round(lastPos.CurrentPrice ?? 0m, 5).AssertEqual(0m);
		Math.Round(lastPos.RealizedPnL ?? 0m, 5).AssertEqual(100m); // 65 + 35
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: true, currentPriceCanBeNull: false, commissionCanBeNull: false);
	}

	[TestMethod]
	public void ShortPositionFlow()
	{
		var mgr = new StrategyPositionManager(() => "SHORT_STRAT");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (sell, sec, pf) = CreateOrder(Sides.Sell, 10m);

		// Open short: SELL 10 @100 (Done)
		sell.Balance = 0m;
		sell.AveragePrice = 100m;
		sell.Commission = 0.5m;
		sell.State = OrderStates.Done;
		mgr.ProcessOrder(sell);

		lastPos.CurrentValue.AssertEqual(-10m);
		lastPos.AveragePrice.AssertEqual(100m);
		(lastPos.RealizedPnL ?? 0m).AssertEqual(0m);
		(lastPos.Commission ?? 0m).AssertEqual(0.5m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: false);

		// Partial cover: BUY 4 @95 (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};
		mgr.ProcessOrder(buy);

		// Realized PnL: (100 - 95) * 4 = 20
		lastPos.CurrentValue.AssertEqual(-6m);
		lastPos.AveragePrice.AssertEqual(100m);
		Math.Round(lastPos.RealizedPnL ?? 0, 5).AssertEqual(20m);
		Math.Round(lastPos.Commission ?? 0, 5).AssertEqual(0.7m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: false);
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
		lastPos.SellOrdersCount.AssertEqual(0);

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
			TransactionId = ++_tx,
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
			TransactionId = ++_tx,
		};
		mgr.ProcessOrder(sell);

		lastPos.BlockedValue.AssertEqual(22m); // 15 buy + 7 sell (no netting)
		lastPos.BuyOrdersCount.AssertEqual(2);
		lastPos.SellOrdersCount.AssertEqual(1);

		// Partial fill on buy1: matched 3, balance 7 (still Active)
		buy1.Balance = 7m;
		buy1.AveragePrice = 100m;
		buy1.State = OrderStates.Active;
		mgr.ProcessOrder(buy1);

		lastPos.BlockedValue.AssertEqual(19m); // 7 + 5 + 7 (no netting)
		lastPos.BuyOrdersCount.AssertEqual(2);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: true, blockedValueCanBeNull: false, buyCountCanBeNull: false);

		// Cancel buy2 (Done + Balance>0 indicates cancellation)
		buy2.State = OrderStates.Done;
		// keep remaining balance 5 to indicate cancelled remainder
		buy2.Balance = 5m;
		mgr.ProcessOrder(buy2);

		lastPos.BlockedValue.AssertEqual(14m); // (7 buy1) + (7 sell)
		lastPos.BuyOrdersCount.AssertEqual(1);
		lastPos.SellOrdersCount.AssertEqual(1);

		// Cancel sell (Done + Balance>0 indicates cancellation)
		sell.State = OrderStates.Done;
		// keep remaining balance 7 to indicate cancelled remainder
		sell.Balance = 7m;
		mgr.ProcessOrder(sell);

		lastPos.BlockedValue.AssertEqual(7m); // Only buy1 remains
		lastPos.BuyOrdersCount.AssertEqual(1);
		lastPos.SellOrdersCount.AssertEqual(0);
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
		buy.State = OrderStates.Done;
		mgr.ProcessOrder(buy);

		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: true);
		lastPos.CurrentPrice.AssertNull(); // No current price yet

		var secId = sec.ToSecurityId();
		var now = DateTimeOffset.Now;
		mgr.UpdateCurrentPrice(secId, 105m, now, now);

		lastPos.CurrentPrice.AssertEqual(1050m); // 10 * 105
		lastPos.LastChangeTime.AssertEqual(now);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: false, commissionCanBeNull: true);

		// Update to different price
		var later = now.AddMinutes(1);
		mgr.UpdateCurrentPrice(secId, 110m, later, later);

		lastPos.CurrentPrice.AssertEqual(1100m); // 10 * 110
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

		// Create position for SEC1/PF1 (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};
		mgr.ProcessOrder(order1);

		// Create position for SEC2/PF1 (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};
		mgr.ProcessOrder(order2);

		// Create position for SEC1/PF2 (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};
		mgr.ProcessOrder(order3);

		mgr.Positions.Length.AssertEqual(3);

		var pos1 = mgr.TryGetPosition(sec1, pf1);
		pos1.AssertNotNull();
		pos1.CurrentValue.AssertEqual(10m);
		pos1.AveragePrice.AssertEqual(100m);
		AssertCalcFieldsNonNull(pos1, averagePriceCanBeNull: false, commissionCanBeNull: true);

		var pos2 = mgr.TryGetPosition(sec2, pf1);
		pos2.AssertNotNull();
		pos2.CurrentValue.AssertEqual(-5m);
		pos2.AveragePrice.AssertEqual(200m);
		AssertCalcFieldsNonNull(pos2, averagePriceCanBeNull: false, commissionCanBeNull: true);

		var pos3 = mgr.TryGetPosition(sec1, pf2);
		pos3.AssertNotNull();
		pos3.CurrentValue.AssertEqual(7m);
		pos3.AveragePrice.AssertEqual(105m);
		AssertCalcFieldsNonNull(pos3, averagePriceCanBeNull: false, commissionCanBeNull: true);
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
	public void ResetClearsAllData()
	{
		var mgr = new StrategyPositionManager(() => "RESET");

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);
		order.Balance = 0m;
		order.AveragePrice = 100m;
		order.State = OrderStates.Done;
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
		order.State = OrderStates.Done;
		mgr.ProcessOrder(order);

		(lastPos.Commission ?? 0m).AssertEqual(0m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: true);

		// Add order with commission (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};
		mgr.ProcessOrder(sell);

		(lastPos.Commission ?? 0m).AssertEqual(1.5m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: false);
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
		order.State = OrderStates.Done;
		mgr.ProcessOrder(order);

		lastPos.CurrentValue.AssertEqual(10m);
		(lastPos.AveragePrice ?? 0m).AssertEqual(0m); // Should default to 0
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: true);
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
		order.State = OrderStates.Done;
		mgr.ProcessOrder(order);

		lastPos.StrategyId.AssertEqual(strategyId);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, commissionCanBeNull: true);
	}

	[TestMethod]
	public void OrderStateTransitions()
	{
		var mgr = new StrategyPositionManager(() => "STATE_TEST");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);

		// Pending order - ignored (no exchange ack yet)
		order.State = OrderStates.Pending;
		mgr.ProcessOrder(order);
		lastPos.AssertNull();

		// Active order
		order.State = OrderStates.Active;
		mgr.ProcessOrder(order);
		lastPos.AssertNotNull();
		lastPos.BlockedValue.AssertEqual(10m);

		// Partial fill (still Active)
		order.Balance = 6m;
		order.AveragePrice = 100m;
		order.State = OrderStates.Active;
		mgr.ProcessOrder(order);
		lastPos.BlockedValue.AssertEqual(6m);
		lastPos.CurrentValue.AssertEqual(4m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, currentPriceCanBeNull: true, commissionCanBeNull: true, blockedValueCanBeNull: false, buyCountCanBeNull: false);

		// Order done (Done + Balance=0)
		order.State = OrderStates.Done;
		order.Balance = 0m;
		order.AveragePrice = 101m;
		mgr.ProcessOrder(order);
		lastPos.BlockedValue.AssertEqual(0m);
		lastPos.CurrentValue.AssertEqual(10m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, commissionCanBeNull: true);
	}

	[TestMethod]
	public void NegativeBalance()
	{
		var mgr = new StrategyPositionManager(() => "NEG_BAL");
		Position lastPos = null;
		mgr.PositionProcessed += (p, _) => lastPos = p;

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);
		order.Balance = 5m;
		order.AveragePrice = 100m;
		order.State = OrderStates.Active;
		mgr.ProcessOrder(order);

		lastPos.CurrentValue.AssertEqual(5m);
		AssertCalcFieldsNonNull(lastPos, averagePriceCanBeNull: false, commissionCanBeNull: true, blockedValueCanBeNull: false, buyCountCanBeNull: false);

		// Zero volume (invalid data)
		order.Volume = 0;
		Assert.ThrowsExactly<ArgumentException>(() => mgr.ProcessOrder(order));

		// Zero balance with active state (invalid data)
		order.Balance = 0;
		Assert.ThrowsExactly<ArgumentException>(() => mgr.ProcessOrder(order));

		// Negative balance (invalid data)
		order.Balance = -1m;
		Assert.ThrowsExactly<ArgumentException>(() => mgr.ProcessOrder(order));

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

		// Create two positions with same security, different portfolios (Done)
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
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
			State = OrderStates.Done,
			TransactionId = ++_tx,
		};
		mgr.ProcessOrder(order2);

		positions.Clear();

		// Update current price for security
		var now = DateTimeOffset.Now;
		mgr.UpdateCurrentPrice(sec.ToSecurityId(), 105m, now, now);

		// Both positions should be updated
		positions.Count.AssertEqual(2);
		positions.Any(p => p.CurrentValue == 10m && p.CurrentPrice == 1050m).AssertTrue();
		positions.Any(p => p.CurrentValue == -5m && p.CurrentPrice == 525m).AssertTrue();
		positions.All(p => p.LastChangeTime == now && p.LocalTime == now).AssertTrue();
	}

	[TestMethod]
	public void Constructor_Null_Throws()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => new StrategyPositionManager(null));
	}

	[TestMethod]
	public void NoneAndFailedStatesIgnored()
	{
		var mgr = new StrategyPositionManager(() => "IGNORED");
		Position last = null;
		mgr.PositionProcessed += (p, _) => last = p;
		var (order, _, _) = CreateOrder(Sides.Buy, 10m);

		order.State = OrderStates.None;
		mgr.ProcessOrder(order);
		last.AssertNull();

		order.State = OrderStates.Failed;
		mgr.ProcessOrder(order);
		last.AssertNull();
	}

	[TestMethod]
	public void ActiveWithZeroBalanceThrows()
	{
		var mgr = new StrategyPositionManager(() => "ACTIVE_ZERO");
		var (order, _, _) = CreateOrder(Sides.Buy, 10m);
		order.State = OrderStates.Active;
		order.Balance = 0m;
		Assert.ThrowsExactly<ArgumentException>(() => mgr.ProcessOrder(order));
	}

	[TestMethod]
	public void TimestampsSetOnProcessing()
	{
		var mgr = new StrategyPositionManager(() => "TS");
		Position last = null;
		mgr.PositionProcessed += (p, _) => last = p;
		var (order, _, _) = CreateOrder(Sides.Buy, 10m);
		var lt = DateTimeOffset.Now.AddMinutes(-1);
		var st = DateTimeOffset.Now;
		order.State = OrderStates.Active;
		order.Balance = 6m; // matched 4
		order.AveragePrice = 100m;
		order.LocalTime = lt;
		order.ServerTime = st;
		mgr.ProcessOrder(order);

		last.LocalTime.AssertEqual(lt);
		last.LastChangeTime.AssertEqual(st);
		AssertCalcFieldsNonNull(last, averagePriceCanBeNull: false, commissionCanBeNull: true, blockedValueCanBeNull: false, buyCountCanBeNull: false);
	}

	[TestMethod]
	public void UsesCachedPriceDuringProcessOrder()
	{
		var mgr = new StrategyPositionManager(() => "CACHED_PRICE");
		Position last = null;
		mgr.PositionProcessed += (p, _) => last = p;
		var (order, sec, _) = CreateOrder(Sides.Buy, 10m);

		// set last price before fills
		var now = DateTimeOffset.Now;
		mgr.UpdateCurrentPrice(sec.ToSecurityId(), 50m, now, now);

		order.State = OrderStates.Done;
		order.Balance = 0m;
		order.AveragePrice = 100m;
		mgr.ProcessOrder(order);

		last.CurrentPrice.AssertEqual(500m); // 10 * 50
		AssertCalcFieldsNonNull(last, averagePriceCanBeNull: false, currentPriceCanBeNull: false, commissionCanBeNull: true);
	}

	[TestMethod]
	public void ReactivationUpdatesAggregates()
	{
		var mgr = new StrategyPositionManager(() => "REACT");
		Position last = null;
		mgr.PositionProcessed += (p, _) => last = p;
		var (order, _, _) = CreateOrder(Sides.Buy, 10m);

		order.State = OrderStates.Active;
		mgr.ProcessOrder(order);
		last.BlockedValue.AssertEqual(10m);

		// cancel (Done + Balance>0)
		order.State = OrderStates.Done;
		order.Balance = 10m;
		mgr.ProcessOrder(order);
		last.BlockedValue.AssertEqual(0m);
		last.BuyOrdersCount.AssertEqual(0);

		// re-activate
		order.State = OrderStates.Active;
		mgr.ProcessOrder(order);
		last.BlockedValue.AssertEqual(10m);
		last.BuyOrdersCount.AssertEqual(1);
	}

	[TestMethod]
	public void NonMonotonicCommissionHandled()
	{
		var mgr = new StrategyPositionManager(() => "COMM");
		Position last = null;
		mgr.PositionProcessed += (p, _) => last = p;
		var (order, _, _) = CreateOrder(Sides.Buy, 10m);

		// first partial: matched 4, comm 1.0
		order.State = OrderStates.Active;
		order.Balance = 6m;
		order.AveragePrice = 100m;
		order.Commission = 1.0m;
		mgr.ProcessOrder(order);
		(last.Commission ?? 0m).AssertEqual(1.0m);
		AssertCalcFieldsNonNull(last, averagePriceCanBeNull: false, commissionCanBeNull: false, blockedValueCanBeNull: false, buyCountCanBeNull: false);

		// provider corrected cumulative commission downwards to 0.8
		order.Balance = 2m; // matched 8 total
		order.AveragePrice = 105m;
		order.Commission = 0.8m; // cumulative decreased => delta -0.2
		mgr.ProcessOrder(order);
		Math.Round(last.Commission ?? 0m, 5).AssertEqual(0.8m);
		AssertCalcFieldsNonNull(last, averagePriceCanBeNull: false, commissionCanBeNull: false, blockedValueCanBeNull: false, buyCountCanBeNull: false);
	}

	[TestMethod]
	public void UpdateCurrentPrice_NoPositions_NoEvents()
	{
		var mgr = new StrategyPositionManager(() => "NOPOS");
		var count = 0;
		mgr.PositionProcessed += (_, __) => count++;
		var sec = new Security { Id = "S" };
		var now = DateTimeOffset.Now;
		mgr.UpdateCurrentPrice(sec.ToSecurityId(), 10m, now, now);
		count.AssertEqual(0);
	}

	[TestMethod]
	public void BlockedValueRemainsNullWithoutActiveOrders()
	{
		var mgr = new StrategyPositionManager(() => "BLK_NULL");
		var sec = new Security { Id = "S" };
		var pf = new Portfolio { Name = "P" };
		mgr.SetPosition(sec, pf, 1m);
		var pos = mgr.TryGetPosition(sec, pf);
		pos.BlockedValue.AssertNull();
	}

	[TestMethod]
	public void NegativePricesValidation()
	{
		var mgr = new StrategyPositionManager(() => "NEG_PRICE");
		var (_, sec, _) = CreateOrder(Sides.Buy, 1m);

		// negative market price should throw
		var now = DateTimeOffset.Now;
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => mgr.UpdateCurrentPrice(sec.ToSecurityId(), -10m, now, now));
	}

	[TestMethod]
	public void ProcessOrder_Throws_When_NoTransactionId()
	{
		var mgr = new StrategyPositionManager(() => "TX");
		var (order, _, _) = CreateOrder(Sides.Buy, 10m);
		order.TransactionId = 0;
		order.State = OrderStates.Active;
		Assert.ThrowsExactly<ArgumentException>(() => mgr.ProcessOrder(order));
	}

	[TestMethod]
	public void Aggregates_Use_TransactionId_As_Key()
	{
		var mgr = new StrategyPositionManager(() => "TX_AGG");
		Position last = null;
		mgr.PositionProcessed += (p, _) => last = p;

		var sec = new Security { Id = "S" };
		var pf = new Portfolio { Name = "P" };

		var order1 = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Buy,
			Volume = 10m,
			Balance = 10m,
			State = OrderStates.Active,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
			TransactionId = ++_tx,
		};
		mgr.ProcessOrder(order1);
		last.BlockedValue.AssertEqual(10m);
		last.BuyOrdersCount.AssertEqual(1);

		// simulate provider creating new Order instance for same TransactionId
		var order1b = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Buy,
			Volume = 10m,
			Balance = 6m, // partial fill happened (matched 4)
			AveragePrice = 100m,
			State = OrderStates.Active,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
			TransactionId = order1.TransactionId, // same id
		};
		mgr.ProcessOrder(order1b);

		// aggregates updated by diff (10 -> 6)
		last.BlockedValue.AssertEqual(6m);
		last.BuyOrdersCount.AssertEqual(1);
		last.CurrentValue.AssertEqual(4m); // matched 4
	}

	[TestMethod]
	public void ExecInfos_Use_TransactionId_As_Key()
	{
		var mgr = new StrategyPositionManager(() => "TX_EXEC");
		Position last = null;
		mgr.PositionProcessed += (p, _) => last = p;

		var (order, sec, pf) = CreateOrder(Sides.Buy, 10m);
		order.State = OrderStates.Active;
		order.TransactionId = ++_tx;

		// first snapshot: matched 3
		order.Balance = 7m;
		order.AveragePrice = 100m;
		mgr.ProcessOrder(order);
		last.CurrentValue.AssertEqual(3m);

		// new instance but same tx id: matched 8 total
		var order2 = new Order
		{
			Security = sec,
			Portfolio = pf,
			Side = Sides.Buy,
			Volume = 10m,
			Balance = 2m,
			AveragePrice = 106.25m,
			State = OrderStates.Active,
			LocalTime = DateTimeOffset.Now,
			ServerTime = DateTimeOffset.Now,
			TransactionId = order.TransactionId,
		};
		mgr.ProcessOrder(order2);
		last.CurrentValue.AssertEqual(8m);
	}

	[TestMethod]
	public void CommissionCorrectionWithoutNewExecution()
	{
		var mgr = new StrategyPositionManager(() => "COMM_CORR");
		Position last = null;
		mgr.PositionProcessed += (p, _) => last = p;

		var (order, _, _) = CreateOrder(Sides.Buy, 10m);
		order.State = OrderStates.Active;
		order.Balance = 6m; // matched 4
		order.AveragePrice = 100m;
		order.Commission = 1.0m;
		mgr.ProcessOrder(order);

		(last.Commission ?? 0m).AssertEqual(1.0m);

		// Provider sends commission correction only (no new executions)
		order.State = OrderStates.Active;
		order.Balance = 6m; // matched stays 4
		order.AveragePrice = 100m;
		order.Commission = 0.8m; // corrected cumulative commission
		mgr.ProcessOrder(order);

		// Expected: commission reflects correction even without new executions
		(last.Commission ?? 0m).AssertEqual(0.8m);
	}
}
