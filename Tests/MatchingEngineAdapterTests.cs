namespace StockSharp.Tests;

using StockSharp.MatchingEngine;

/// <summary>
/// Tests for <see cref="MatchingEngineAdapter"/> - the book the internal (B-Book) venue is built on.
/// Every test drives the real engine through its own transport and reads back what it emitted.
/// </summary>
[TestClass]
[TestCategory("Unit")]
public class MatchingEngineAdapterTests : BaseTestClass
{
	private static readonly SecurityId _securityId = new() { SecurityCode = "BTCUSDT", BoardCode = "IMEX" };
	private static readonly SecurityId _otherSecurityId = new() { SecurityCode = "ETHUSDT", BoardCode = "IMEX" };
	private static readonly DateTime _start = new(2026, 8, 20, 10, 0, 0, DateTimeKind.Utc);

	#region Test helpers

	/// <summary>
	/// Feeds an engine through <see cref="IMessageTransport"/> and keeps everything it emits, in order.
	/// </summary>
	private sealed class EngineRun
	{
		public EngineRun(MatchingEngineAdapter engine)
		{
			Engine = engine ?? throw new ArgumentNullException(nameof(engine));

			Engine.NewOutMessageAsync += (message, ct) =>
			{
				Out.Add(message);
				return default;
			};
		}

		/// <summary>The engine under test.</summary>
		public MatchingEngineAdapter Engine { get; }

		/// <summary>Everything the engine has emitted so far.</summary>
		public List<Message> Out { get; } = [];

		/// <summary>The transactional rows among them.</summary>
		public IEnumerable<ExecutionMessage> Executions => Out.OfType<ExecutionMessage>();

		/// <summary>Sends one message in the way the venue module sends it.</summary>
		public ValueTask SendAsync(Message message, CancellationToken cancellationToken)
			=> ((IMessageTransport)Engine).SendInMessageAsync(message, cancellationToken);
	}

	private static QuoteChangeMessage VenueBook(SecurityId securityId, DateTime time, QuoteChange[] bids, QuoteChange[] asks)
		=> new()
		{
			SecurityId = securityId,
			LocalTime = time,
			ServerTime = time,
			Bids = bids,
			Asks = asks,
		};

	private static PositionChangeMessage MoneyRow(string account, decimal money, DateTime time)
		=> new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = account,
			LocalTime = time,
			ServerTime = time,
		}
		.Add(PositionChangeTypes.BeginValue, money);

	private static PositionChangeMessage PositionRow(SecurityId securityId, string account, decimal volume, decimal averagePrice, DateTime time)
		=> new PositionChangeMessage
		{
			SecurityId = securityId,
			PortfolioName = account,
			LocalTime = time,
			ServerTime = time,
		}
		.Add(PositionChangeTypes.BeginValue, volume)
		.Add(PositionChangeTypes.AveragePrice, averagePrice);

	private static OrderRegisterMessage NewOrder(long transactionId, string account, Sides side, OrderTypes type, decimal price, decimal volume, DateTime time)
		=> new()
		{
			TransactionId = transactionId,
			SecurityId = _securityId,
			PortfolioName = account,
			Side = side,
			OrderType = type,
			Price = price,
			Volume = volume,
			LocalTime = time,
		};

	private static decimal PositionOf(MatchingEngineAdapter engine, string account)
		=> engine.PortfolioManager.GetPortfolio(account).GetPosition(_securityId)?.CurrentValue ?? 0m;

	/// <summary>
	/// Sends one market order of <paramref name="side"/> into a book holding a single level per side
	/// and answers how much of it was filled.
	/// </summary>
	private static async Task<decimal> FillAtMarketAsync(Sides side, decimal volume, CancellationToken cancellationToken)
	{
		const long tx = 4001;

		var run = new EngineRun(new MatchingEngineAdapter());

		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 1m)], [new QuoteChange(101m, 1m)]), cancellationToken);
		await run.SendAsync(NewOrder(tx, "Client", side, OrderTypes.Market, 0m, volume, _start.AddSeconds(1)), cancellationToken);

		return run.Executions
			.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == tx)
			.Sum(m => m.TradeVolume ?? 0m);
	}

	#endregion

	/// <summary>
	/// An account is one account however its name is spelled: an order naming it differently has to
	/// find the money it was funded with, not a second, empty book beside it.
	/// </summary>
	[TestMethod]
	public async Task AnAccountIsTheSameAccountHoweverItsNameIsSpelled()
	{
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow("Demo1", 1000m, _start), CancellationToken);

		AreEqual(1000m, engine.PortfolioManager.GetPortfolio("DEMO1").BeginMoney,
			"the account was funded under one spelling, so the other spelling must reach the same money");
	}

	/// <summary>
	/// An instrument the venue has halted takes no orders at all, whatever the account can pay for.
	/// </summary>
	[TestMethod]
	public async Task AHaltedInstrumentTakesNoOrders()
	{
		const string account = "Trader";

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckTradingState = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, 10000m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		var halt = new Level1ChangeMessage
		{
			SecurityId = _securityId,
			ServerTime = _start.AddSeconds(1),
			LocalTime = _start.AddSeconds(1),
		};
		halt.Add(Level1Fields.State, SecurityStates.Stoped);

		await run.SendAsync(halt, CancellationToken);

		await run.SendAsync(NewOrder(1, account, Sides.Buy, OrderTypes.Limit, 100m, 1m, _start.AddSeconds(2)), CancellationToken);

		var replies = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == 1).ToArray();

		IsTrue(replies.Length > 0, "the engine must answer the registration");
		IsTrue(replies.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"the venue has halted the instrument, so the order cannot stand whatever the account holds; states were {replies.Select(m => m.OrderState.ToString()).JoinComma()}");
	}

	/// <summary>
	/// An account holding no cash must have its market buy rejected: a market order names no price,
	/// but it still costs what the book charges for it.
	/// </summary>
	[TestMethod]
	public async Task AZeroCashAccountCannotBuyAtMarket()
	{
		const string account = "ZeroCash";
		const long tx = 1001;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, 0m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(NewOrder(tx, account, Sides.Buy, OrderTypes.Market, 0m, 5m, _start.AddSeconds(1)), CancellationToken);

		var replies = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == tx).ToArray();

		IsTrue(replies.Length > 0, "the engine must answer the registration");
		IsTrue(replies.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"a market buy of 5 at an ask of 101 costs 505 and the account holds nothing, so it must be rejected; states were {replies.Select(m => m.OrderState.ToString()).JoinComma()}");

		var fills = run.Executions.Where(m => m.HasTradeInfo()).ToArray();

		AreEqual(0, fills.Length, "a rejected order must not trade");
		AreEqual(0m, PositionOf(engine, account), "a rejected order must not move the account's position");
	}

	/// <summary>
	/// A position that moved against the account has spent money the account no longer has: what is
	/// left to trade with must go down with it, or the account keeps buying on a loss it never took.
	/// </summary>
	[TestMethod]
	public async Task APositionThatMovedAgainstTheAccountLeavesLessToTradeWith()
	{
		const string account = "Trader";
		const decimal begin = 2000m;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, begin, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// Buys the whole ask: 10 at 101, so the account is long 10 at an average of 101.
		await run.SendAsync(NewOrder(1, account, Sides.Buy, OrderTypes.Limit, 101m, 10m, _start.AddSeconds(1)), CancellationToken);

		AreEqual(10m, PositionOf(engine, account), "the opening buy has to fill, or there is no position to revalue");

		// The market halves: the position is worth 500 where it cost 1010, a loss of 510.
		await run.SendAsync(VenueBook(_securityId, _start.AddSeconds(2), [new QuoteChange(50m, 10m)], [new QuoteChange(51m, 10m)]), CancellationToken);

		await run.SendAsync(NewOrder(2, account, Sides.Buy, OrderTypes.Limit, 50m, 10m, _start.AddSeconds(3)), CancellationToken);

		var replies = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == 2).ToArray();

		IsTrue(replies.Length > 0, "the engine must answer the second registration");
		IsTrue(replies.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"the account opened with {begin}, is down 510 on an open position and already has 1010 committed to it, so a further 500 is money it does not have; states were {replies.Select(m => m.OrderState.ToString()).JoinComma()}");
	}

	/// <summary>
	/// A short that the market ran away from has lost the same money a long would have, and what is
	/// left to trade with must go down with it.
	/// </summary>
	[TestMethod]
	public async Task AShortThatTheMarketRanAwayFromLeavesLessToTradeWith()
	{
		const string account = "Trader";

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, 2000m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// Sells the whole bid: short 10 at 100.
		await run.SendAsync(NewOrder(1, account, Sides.Sell, OrderTypes.Limit, 100m, 10m, _start.AddSeconds(1)), CancellationToken);

		AreEqual(-10m, PositionOf(engine, account), "the opening sell has to fill, or there is no position to revalue");

		// The market doubles: buying the short back now costs 2010 where it sold for 1000.
		await run.SendAsync(VenueBook(_securityId, _start.AddSeconds(2), [new QuoteChange(200m, 10m)], [new QuoteChange(201m, 10m)]), CancellationToken);

		await run.SendAsync(NewOrder(2, account, Sides.Buy, OrderTypes.Limit, 200m, 1m, _start.AddSeconds(3)), CancellationToken);

		var replies = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == 2).ToArray();

		IsTrue(replies.Length > 0, "the engine must answer the second registration");
		IsTrue(replies.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"the account opened with 2000 and is down 1010 on the short, so it has nothing left for another 200; states were {replies.Select(m => m.OrderState.ToString()).JoinComma()}");
	}

	/// <summary>
	/// A position in an instrument nobody has quoted is worth what it cost: the engine must not invent
	/// a loss out of a price it does not have, and must not fall over asking for one.
	/// </summary>
	[TestMethod]
	public void APositionTheMarketHasNotPricedIsWorthWhatItCost()
	{
		const string account = "Trader";

		var engine = new MatchingEngineAdapter();
		var portfolio = engine.PortfolioManager.GetPortfolio(account);

		portfolio.SetMoney(1000m);
		portfolio.SetPosition(_otherSecurityId, 5m, 20m);

		AreEqual(0m, portfolio.UnrealizedPnL, "an unpriced position cannot have gained or lost anything");
		AreEqual(1000m, portfolio.CurrentMoney, "and the account still holds what it started with");
	}

	/// <summary>
	/// A position is worth what the market would pay for it whether or not the venue checks money:
	/// what an account is told it holds cannot depend on a setting about order acceptance.
	/// </summary>
	[TestMethod]
	public async Task APositionIsRevaluedWhetherOrNotTheVenueChecksMoney()
	{
		const string account = "Trader";

		// Money checks off, which is what an engine is built with.
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, 2000m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// Ten bought at 101, and the market then bids 150 for them.
		await run.SendAsync(NewOrder(1, account, Sides.Buy, OrderTypes.Limit, 101m, 10m, _start.AddSeconds(1)), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start.AddSeconds(2), [new QuoteChange(150m, 10m)], [new QuoteChange(151m, 10m)]), CancellationToken);

		var portfolio = engine.PortfolioManager.GetPortfolio(account);

		AreEqual(490m, portfolio.UnrealizedPnL,
			"the position cost 1010 and the bid would pay 1500 for it, so it stands 490 ahead");
		AreEqual(2490m, portfolio.CurrentMoney,
			"and what the account holds moves with it");
	}

	/// <summary>
	/// A position that moved the account's way is not punished for it: what it can trade with does not
	/// shrink because the market went in its favour.
	/// </summary>
	[TestMethod]
	public async Task APositionThatMovedTheAccountsWayIsNotPunished()
	{
		const string account = "Trader";

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, 2000m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(NewOrder(1, account, Sides.Buy, OrderTypes.Limit, 101m, 10m, _start.AddSeconds(1)), CancellationToken);

		var afterOpening = engine.PortfolioManager.GetPortfolio(account).AvailableMoney;

		// The market rises: the position is worth more than it cost.
		await run.SendAsync(VenueBook(_securityId, _start.AddSeconds(2), [new QuoteChange(150m, 10m)], [new QuoteChange(151m, 10m)]), CancellationToken);

		IsTrue(engine.PortfolioManager.GetPortfolio(account).AvailableMoney >= afterOpening,
			$"a position in profit must not cost the account anything: {afterOpening} before the rise, {engine.PortfolioManager.GetPortfolio(account).AvailableMoney} after");
	}

	/// <summary>
	/// When one client's order is filled by another client's resting order, the resting side must be
	/// filled for the whole volume that was taken from it, not for nothing.
	/// </summary>
	[TestMethod]
	public async Task AnInternalCrossFillsTheMakerForWhatTheTakerTook()
	{
		const string maker = "Maker";
		const string taker = "Taker";
		const long makerTx = 2001;
		const long takerTx = 2002;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(NewOrder(makerTx, maker, Sides.Sell, OrderTypes.Limit, 100m, 10m, _start), CancellationToken);
		await run.SendAsync(NewOrder(takerTx, taker, Sides.Buy, OrderTypes.Limit, 100m, 10m, _start.AddSeconds(1)), CancellationToken);

		var makerFill = run.Executions.FirstOrDefault(m => m.HasTradeInfo() && m.OriginalTransactionId == makerTx);

		IsNotNull(makerFill, "the resting side of an internal cross must be told about its own fill");
		AreEqual(10m, makerFill.TradeVolume, "the maker was consumed for 10, so its fill is for 10");
		AreEqual(100m, makerFill.TradePrice, "both sides of one cross trade at the same price");

		AreEqual(-10m, PositionOf(engine, maker), "the maker sold 10 and must be short 10");
		AreEqual(10m, PositionOf(engine, taker), "the taker bought 10 and must be long 10");
	}

	/// <summary>
	/// A resting order eaten down to nothing by an internal cross must be reported finished, so the
	/// session that placed it stops waiting for it.
	/// </summary>
	[TestMethod]
	public async Task AMakerConsumedToTheLastLotIsToldItsOrderIsFinished()
	{
		const string maker = "Maker";
		const string taker = "Taker";
		const long makerTx = 3001;
		const long takerTx = 3002;

		var run = new EngineRun(new MatchingEngineAdapter());

		await run.SendAsync(NewOrder(makerTx, maker, Sides.Sell, OrderTypes.Limit, 100m, 10m, _start), CancellationToken);
		await run.SendAsync(NewOrder(takerTx, taker, Sides.Buy, OrderTypes.Limit, 100m, 10m, _start.AddSeconds(1)), CancellationToken);

		var makerFinal = run.Executions
			.FirstOrDefault(m => m.HasOrderInfo() && m.OriginalTransactionId == makerTx && m.OrderState == OrderStates.Done);

		IsNotNull(makerFinal, "an order consumed in full must reach a final state, whoever consumed it");
		AreEqual(0m, makerFinal.Balance, "nothing is left of an order that was consumed in full");
	}

	/// <summary>
	/// The order a triggered stop turns into must carry a transaction of its own, so its fills can be
	/// told apart and delivered to the session that placed the stop.
	/// </summary>
	[TestMethod]
	public async Task ATriggeredStopFillsUnderARealTransactionId()
	{
		const string account = "Client";
		const long stopTx = 5001;

		var run = new EngineRun(new MatchingEngineAdapter());

		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		var stop = NewOrder(stopTx, account, Sides.Buy, OrderTypes.Conditional, 0m, 3m, _start.AddSeconds(1));
		stop.Condition = new StopOrderCondition { ActivationPrice = 105m };

		await run.SendAsync(stop, CancellationToken);

		await run.SendAsync(new Level1ChangeMessage
		{
			SecurityId = _securityId,
			LocalTime = _start.AddSeconds(2),
			ServerTime = _start.AddSeconds(2),
		}
		.Add(Level1Fields.LastTradePrice, 106m), CancellationToken);

		var fill = run.Executions.FirstOrDefault(m => m.HasTradeInfo());

		IsNotNull(fill, "the stop was passed through its activation price, so its order must reach the book");
		AreNotEqual(0L, fill.OriginalTransactionId, "a fill nobody can attribute is a fill that reaches every session or none");

		var unattributed = run.Executions.Where(m => m.OriginalTransactionId == 0 && m.TransactionId == 0).ToArray();

		AreEqual(0, unattributed.Length, "every transactional row the engine raises must name the order it belongs to");
	}

	/// <summary>
	/// Positions closed by a group cancel are closed on someone's behalf, and the fills must say
	/// whose - the closing order never passed through the caller, so nothing else can name it.
	/// </summary>
	[TestMethod]
	public async Task ClosingAPositionOnGroupCancelNamesTheAccountItBelongsTo()
	{
		const string account = "Client";
		const long groupTx = 6001;

		var run = new EngineRun(new MatchingEngineAdapter());

		await run.SendAsync(PositionRow(_securityId, account, 5m, 90m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(new OrderGroupCancelMessage
		{
			TransactionId = groupTx,
			PortfolioName = account,
			Mode = OrderGroupCancelModes.ClosePositions,
			LocalTime = _start.AddSeconds(1),
		}, CancellationToken);

		var fill = run.Executions.FirstOrDefault(m => m.HasTradeInfo());

		IsNotNull(fill, "a long of 5 against a bid of 100 must be closed by a fill");
		AreEqual(5m, fill.TradeVolume, "the whole position is closed");
		AreEqual(account, fill.PortfolioName, "a fill the engine raised by itself must still name the account it closed");
	}

	/// <summary>
	/// The closing order a group cancel raises is built inside the engine and reaches no caller on its
	/// way in, so nothing outside can attach an account to it afterwards. Every row it raises - its
	/// acceptance as much as its fills - must name the account, or it arrives unattributed and is
	/// booked against nobody.
	/// </summary>
	[TestMethod]
	public async Task EveryRowRaisedForAGroupCancelCloseNamesTheAccount()
	{
		const string account = "Client";
		const long groupTx = 6101;

		var run = new EngineRun(new MatchingEngineAdapter());

		await run.SendAsync(PositionRow(_securityId, account, 5m, 90m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(new OrderGroupCancelMessage
		{
			TransactionId = groupTx,
			PortfolioName = account,
			Mode = OrderGroupCancelModes.ClosePositions,
			LocalTime = _start.AddSeconds(1),
		}, CancellationToken);

		var rows = run.Executions.ToArray();

		IsTrue(rows.Any(m => m.HasTradeInfo()), "a long of 5 against a bid of 100 must be closed by a fill");
		IsTrue(rows.Any(m => m.HasOrderInfo()), "the order that closes the position must report its own lifecycle");

		var unnamed = rows.Where(m => m.PortfolioName.IsEmpty()).ToArray();
		var unnamedKinds = unnamed.Select(m => m.HasTradeInfo() ? "trade" : $"order {m.OrderState}").JoinComma();

		AreEqual(0, unnamed.Length,
			$"the engine placed this order itself, so only it can say whose it is: {unnamedKinds}");
	}

	/// <summary>
	/// One group cancel closes every position the account holds, and each closing order stands on its
	/// own. Two orders alive at once cannot answer to one transaction id - whoever keys fills by it
	/// books both against a single order and loses one of the two.
	/// </summary>
	[TestMethod]
	public async Task TwoPositionsClosedByOneGroupCancelAnswerToTwoOrders()
	{
		const string account = "Client";
		const long groupTx = 6201;

		var run = new EngineRun(new MatchingEngineAdapter());

		await run.SendAsync(PositionRow(_securityId, account, 5m, 90m, _start), CancellationToken);
		await run.SendAsync(PositionRow(_otherSecurityId, account, 4m, 40m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);
		await run.SendAsync(VenueBook(_otherSecurityId, _start, [new QuoteChange(50m, 10m)], [new QuoteChange(51m, 10m)]), CancellationToken);

		await run.SendAsync(new OrderGroupCancelMessage
		{
			TransactionId = groupTx,
			PortfolioName = account,
			Mode = OrderGroupCancelModes.ClosePositions,
			LocalTime = _start.AddSeconds(1),
		}, CancellationToken);

		var fills = run.Executions.Where(m => m.HasTradeInfo()).ToArray();

		AreEqual(2, fills.Length, "both positions are closed, so each raises a fill of its own");

		var first = fills.FirstOrDefault(m => m.SecurityId == _securityId);
		var second = fills.FirstOrDefault(m => m.SecurityId == _otherSecurityId);

		IsNotNull(first, "the position in the first instrument must be closed by a fill naming that instrument");
		IsNotNull(second, "the position in the second instrument must be closed by a fill naming that instrument");

		AreEqual(5m, first.TradeVolume, "the whole position in the first instrument is closed");
		AreEqual(4m, second.TradeVolume, "the whole position in the second instrument is closed");

		AreEqual(account, first.PortfolioName, "a fill the engine raised by itself must still name the account it closed");
		AreEqual(account, second.PortfolioName, "a fill the engine raised by itself must still name the account it closed");

		AreNotEqual(first.OriginalTransactionId, second.OriginalTransactionId,
			$"two closing orders standing at once must be told apart, and both answer to {first.OriginalTransactionId}");
	}

	/// <summary>
	/// A market buy must reach as far into the book as the mirrored market sell does; the same order
	/// on the other side cannot fill a hundredth of it.
	/// </summary>
	[TestMethod]
	public async Task AMarketBuyReachesAsDeepIntoTheBookAsAMarketSell()
	{
		const decimal volume = 100m;

		var bought = await FillAtMarketAsync(Sides.Buy, volume, CancellationToken);
		var sold = await FillAtMarketAsync(Sides.Sell, volume, CancellationToken);

		AreEqual(sold, bought, $"the same order on either side of one book must fill the same: bought {bought}, sold {sold}");
		AreEqual(volume, bought, "a market order against a book that is deepened for it fills in full");
	}
}
