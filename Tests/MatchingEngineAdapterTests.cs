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

	private static QuoteChangeMessage IncrementalBook(SecurityId securityId, DateTime time, QuoteChangeStates state, QuoteChange[] bids, QuoteChange[] asks)
		=> new()
		{
			SecurityId = securityId,
			LocalTime = time,
			ServerTime = time,
			State = state,
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
	private static async Task<decimal> FillAtMarketAsync(Sides side, decimal volume, bool extendBook, CancellationToken cancellationToken)
	{
		const long tx = 4001;

		var engine = new MatchingEngineAdapter();
		engine.Settings.IncreaseDepthVolume = extendBook;

		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 1m)], [new QuoteChange(101m, 1m)]), cancellationToken);
		await run.SendAsync(NewOrder(tx, "Client", side, OrderTypes.Market, 0m, volume, _start.AddSeconds(1)), cancellationToken);

		return run.Executions
			.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == tx)
			.Sum(m => m.TradeVolume ?? 0m);
	}

	#endregion

	/// <summary>
	/// The engine is told what an instrument is before anything is matched on it, so it can also
	/// answer what the venue lists - and whoever needs that answer does not have to keep a second
	/// copy of every definition it forwarded.
	/// </summary>
	[TestMethod]
	public async Task TheEngineListsTheSecuritiesItWasTold()
	{
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(new SecurityMessage { SecurityId = _securityId, PriceStep = 0.01m, VolumeStep = 1m }, CancellationToken);
		await run.SendAsync(new SecurityMessage { SecurityId = _otherSecurityId, PriceStep = 0.1m, VolumeStep = 2m }, CancellationToken);

		// A security the engine only ever saw a book for states no definition, and is not listed.
		await run.SendAsync(VenueBook(new() { SecurityCode = "QUOTED", BoardCode = "IMEX" }, _start,
			[new QuoteChange(1m, 1m)], [new QuoteChange(2m, 1m)]), CancellationToken);

		var listed = engine.Securities.ToArray();

		AreEqual(2, listed.Length, $"two definitions were stated, got [{listed.Select(s => s.SecurityId.ToString()).JoinComma()}]");
		IsTrue(listed.Any(s => s.SecurityId == _securityId && s.PriceStep == 0.01m), "the first must be listed as stated");
		IsTrue(listed.Any(s => s.SecurityId == _otherSecurityId && s.VolumeStep == 2m), "and the second too");
	}

	/// <summary>
	/// Every order row the engine states names the account it is about. A consumer routes and books
	/// by account, and a row that names none is a row it cannot place: the venue has to put the
	/// account back from its own memory of the registration, or the wire drops the row outright.
	/// </summary>
	[TestMethod]
	public async Task EveryOrderRowNamesTheAccountItIsAbout()
	{
		const string account = "NAMED-PF";

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, 1_000_000m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// A registration, a replace of it, a cancel of the replacement - and the same three against
		// transactions the engine holds nothing for, which is the path that answers with a failure.
		await run.SendAsync(NewOrder(5001, account, Sides.Buy, OrderTypes.Limit, 50m, 1m, _start), CancellationToken);

		await run.SendAsync(new OrderReplaceMessage
		{
			TransactionId = 5002,
			OriginalTransactionId = 5001,
			SecurityId = _securityId,
			PortfolioName = account,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			Price = 51m,
			Volume = 1m,
			LocalTime = _start,
		}, CancellationToken);

		await run.SendAsync(new OrderCancelMessage
		{
			TransactionId = 5003,
			OriginalTransactionId = 5002,
			SecurityId = _securityId,
			PortfolioName = account,
			LocalTime = _start,
		}, CancellationToken);

		await run.SendAsync(new OrderCancelMessage
		{
			TransactionId = 5004,
			OriginalTransactionId = 9999,
			SecurityId = _securityId,
			PortfolioName = account,
			LocalTime = _start,
		}, CancellationToken);

		await run.SendAsync(new OrderReplaceMessage
		{
			TransactionId = 5005,
			OriginalTransactionId = 9998,
			SecurityId = _securityId,
			PortfolioName = account,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			Price = 52m,
			Volume = 1m,
			LocalTime = _start,
		}, CancellationToken);

		var unnamed = run.Executions
			.Where(e => e.HasOrderInfo && e.PortfolioName.IsEmpty())
			.Select(e => $"{e.OrderState} on {e.OriginalTransactionId}")
			.ToArray();

		AreEqual(0, unnamed.Length,
			$"every order row must name its account; [{unnamed.JoinComma()}] named none");
	}

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
	/// A venue that publishes its book as increments must still reach the engine: the opening
	/// snapshot stands, and every increment after it moves the book it stands on.
	/// </summary>
	[TestMethod]
	public async Task AnIncrementalBookFeedReachesTheEngine()
	{
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(IncrementalBook(_securityId, _start, QuoteChangeStates.SnapshotComplete,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		var book = engine.GetSecurityState(_securityId).OrderBook;

		AreEqual(100m, book.BestBid?.price, "the opening snapshot must stand as the book");
		AreEqual(101m, book.BestAsk?.price, "on both sides");

		// The venue moves its bid up, stating only what changed.
		await run.SendAsync(IncrementalBook(_securityId, _start.AddSeconds(1), QuoteChangeStates.Increment,
			[new QuoteChange(100.5m, 5m)], []), CancellationToken);

		AreEqual(100.5m, engine.GetSecurityState(_securityId).OrderBook.BestBid?.price,
			"an increment that improves the bid must move the book with it");
	}

	/// <summary>
	/// A level an increment states at zero volume is gone from the book, which is how a venue
	/// withdraws a price: the level behind it becomes the best.
	/// </summary>
	[TestMethod]
	public async Task AnIncrementAtZeroVolumeTakesTheLevelOut()
	{
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(IncrementalBook(_securityId, _start, QuoteChangeStates.SnapshotComplete,
			[new QuoteChange(100m, 10m), new QuoteChange(99m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(IncrementalBook(_securityId, _start.AddSeconds(1), QuoteChangeStates.Increment,
			[new QuoteChange(100m, 0m)], []), CancellationToken);

		AreEqual(99m, engine.GetSecurityState(_securityId).OrderBook.BestBid?.price,
			"the withdrawn level is gone, so the one behind it is the best bid");
	}

	/// <summary>
	/// A feed that went quiet has to state a whole book again before its increments mean anything:
	/// folding them onto the base it left behind would price orders off a market that is gone.
	/// </summary>
	[TestMethod]
	public async Task AnIncrementAfterAForgottenBookIsNotFoldedOntoIt()
	{
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(IncrementalBook(_securityId, _start, QuoteChangeStates.SnapshotComplete,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		engine.ForgetBook(_securityId);

		// The venue comes back with an increment and no snapshot behind it, naming a bid that would
		// become the best if it were folded into the book left behind.
		await run.SendAsync(IncrementalBook(_securityId, _start.AddSeconds(1), QuoteChangeStates.Increment,
			[new QuoteChange(105m, 5m)], []), CancellationToken);

		AreEqual(100m, engine.GetSecurityState(_securityId).OrderBook.BestBid?.price,
			"the increment had no book to fold into, so it must not have moved the one the engine holds");

		// A whole book from the venue stands again.
		await run.SendAsync(IncrementalBook(_securityId, _start.AddSeconds(2), QuoteChangeStates.SnapshotComplete,
			[new QuoteChange(90m, 5m)], [new QuoteChange(91m, 5m)]), CancellationToken);

		AreEqual(90m, engine.GetSecurityState(_securityId).OrderBook.BestBid?.price,
			"and the feed picks up again from what it states whole");
	}

	/// <summary>
	/// A quote states where the market is, not what is resting behind it, so it builds no book. An
	/// engine that turned each one into a level quoted the extremes of the session against each other.
	/// </summary>
	[TestMethod]
	public async Task AQuoteBuildsNoBook()
	{
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		// The market walks down: 100/101, then 99/100, then 98/99.
		for (var i = 0; i < 3; i++)
		{
			var touch = new Level1ChangeMessage
			{
				SecurityId = _securityId,
				ServerTime = _start.AddSeconds(i),
				LocalTime = _start.AddSeconds(i),
			};
			touch.Add(Level1Fields.BestBidPrice, 100m - i);
			touch.Add(Level1Fields.BestAskPrice, 101m - i);

			await run.SendAsync(touch, CancellationToken);
		}

		var book = engine.GetSecurityState(_securityId).OrderBook;

		IsNull(book.BestBid, "a quote is not a book: nothing is resting at that bid");
		IsNull(book.BestAsk, "nor at that ask");
	}

	/// <summary>
	/// An order larger than the market cannot be filled by more market than there is: what is not
	/// there does not appear because someone asked for it.
	/// </summary>
	[TestMethod]
	public async Task AnOrderLargerThanTheBookDoesNotConjureTheRest()
	{
		const long tx = 5001;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		// Five lots offered, and nothing behind them.
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 5m)], [new QuoteChange(101m, 5m)]), CancellationToken);

		await run.SendAsync(NewOrder(tx, "Client", Sides.Buy, OrderTypes.Market, 0m, 50m, _start.AddSeconds(1)), CancellationToken);

		var filled = run.Executions
			.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == tx)
			.Sum(m => m.TradeVolume ?? 0m);

		AreEqual(5m, filled, "five lots were offered, so five lots is what an order of fifty gets");

		var fills = run.Executions.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == tx).ToArray();

		IsTrue(fills.All(m => m.TradePrice == 101m),
			$"and all of it at the one price the market offered; prices were {fills.Select(m => m.TradePrice.ToString()).JoinComma()}");
	}

	/// <summary>
	/// A step the venue states is the step, and the venue's own definition of the instrument is not
	/// the engine's to write in.
	/// </summary>
	[TestMethod]
	public async Task AStatedPriceStepIsNotGuessedOver()
	{
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		var definition = new SecurityMessage
		{
			SecurityId = _securityId,
			PriceStep = 1m,
			VolumeStep = 1m,
		};

		await run.SendAsync(definition, CancellationToken);

		// A price with two decimals: guessing off it would say the step is 0.01.
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(1000.25m, 3m)], [new QuoteChange(1001.25m, 3m)]), CancellationToken);

		AreEqual(1m, engine.GetSecurityState(_securityId).PriceStep, "the venue stated the step, so the step is what it stated");
		AreEqual(1m, definition.PriceStep, "and the venue's own definition is left as the venue wrote it");
	}

	/// <summary>
	/// A print is news about a trade, not about the book: the depth a venue stated stands after one,
	/// whole, and an order still walks the levels the venue actually published.
	/// </summary>
	[TestMethod]
	public async Task ATickDoesNotCutTheBookTheVenueStated()
	{
		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		QuoteChange[] bids = [.. Enumerable.Range(0, 20).Select(i => new QuoteChange(100m - i, 10m))];
		QuoteChange[] asks = [.. Enumerable.Range(0, 20).Select(i => new QuoteChange(101m + i, 10m))];

		await run.SendAsync(VenueBook(_securityId, _start, bids, asks), CancellationToken);

		AreEqual(20, engine.GetSecurityState(_securityId).OrderBook.BidLevels, "the venue stated twenty levels");

		await run.SendAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = _securityId,
			ServerTime = _start.AddSeconds(1),
			LocalTime = _start.AddSeconds(1),
			TradePrice = 100.5m,
			TradeVolume = 1m,
			TradeId = 1,
		}, CancellationToken);

		var book = engine.GetSecurityState(_securityId).OrderBook;

		AreEqual(20, book.BidLevels, "a print says nothing about the levels behind the touch");
		AreEqual(20, book.AskLevels, "on either side");
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
	/// An account the venue has never funded can pay for nothing: with the money check on, an order
	/// naming a name the engine has never heard of has to be refused like any other unpayable one.
	/// </summary>
	[TestMethod]
	public async Task AnAccountTheVenueHasNeverFundedCannotBuy()
	{
		const string account = "NeverFunded";
		const long tx = 1201;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// No money row for this account: the engine has never heard the name.
		await run.SendAsync(NewOrder(tx, account, Sides.Buy, OrderTypes.Limit, 101m, 5m, _start.AddSeconds(1)), CancellationToken);

		var replies = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == tx).ToArray();

		IsTrue(replies.Length > 0, "the engine must answer the registration");
		IsTrue(replies.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"an account with nothing behind it cannot pay 505 for this order; states were {replies.Select(m => m.OrderState.ToString()).JoinComma()}, errors were {replies.Select(m => m.Error?.Message ?? "<none>").JoinComma()}");
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

		// One lot is offered on each side, so one lot is what either side of the order gets.
		var bought = await FillAtMarketAsync(Sides.Buy, volume, extendBook: false, CancellationToken);
		var sold = await FillAtMarketAsync(Sides.Sell, volume, extendBook: false, CancellationToken);

		AreEqual(sold, bought, $"the same order on either side of one book must fill the same: bought {bought}, sold {sold}");
		AreEqual(1m, bought, "and neither side reaches past what the market holds");
	}

	/// <summary>
	/// The same, of a book extended to meet the order: replaying history means filling what the
	/// record shows, and both sides have to be extended alike.
	/// </summary>
	[TestMethod]
	public async Task AnExtendedBookIsExtendedAlikeOnBothSides()
	{
		const decimal volume = 100m;

		var bought = await FillAtMarketAsync(Sides.Buy, volume, extendBook: true, CancellationToken);
		var sold = await FillAtMarketAsync(Sides.Sell, volume, extendBook: true, CancellationToken);

		AreEqual(sold, bought, $"the same order on either side of one book must fill the same: bought {bought}, sold {sold}");
		AreEqual(volume, bought, "a book extended for the order fills it in full");
	}

	/// <summary>
	/// A market order the book cannot fill in full ends Done with a balance that rests nowhere: it
	/// never enters the book, so no cancel and no expiry can reach it afterwards. The money blocked
	/// for that balance at registration is released with the order, or the account loses that much
	/// buying power for good on every partial market fill.
	/// </summary>
	[TestMethod]
	public async Task AMarketOrderReleasesWhatItsUnfilledBalanceBlocked()
	{
		const long tx = 4101;
		const string account = "Client";

		var engine = new MatchingEngineAdapter();
		engine.Settings.IncreaseDepthVolume = false;

		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 1m)], [new QuoteChange(101m, 1m)]), CancellationToken);
		await run.SendAsync(NewOrder(tx, account, Sides.Buy, OrderTypes.Market, 0m, 100m, _start.AddSeconds(1)), CancellationToken);

		var state = run.Executions.Last(m => m.OriginalTransactionId == tx && m.HasOrderInfo && !m.HasTradeInfo());

		AreEqual(OrderStates.Done, state.OrderState);
		AreEqual(99m, state.Balance, "one lot was offered, so ninety-nine of the hundred found no liquidity");

		// Registration blocked all hundred lots at the best bid of 100. What stays blocked afterwards
		// is the one lot actually bought, at the price it was bought at.
		AreEqual(101m, engine.PortfolioManager.GetPortfolio(account).BlockedMoney);
	}

	/// <summary>
	/// An order in this book is filled by a counterparty in this book and by nothing else. A print
	/// reports a trade between two other parties: it takes no volume from any level here, so a
	/// resting limit the print traded through is still resting, for its whole balance, afterwards.
	/// </summary>
	[TestMethod]
	public async Task APrintThroughARestingLimitLeavesItResting()
	{
		const string account = "Trader";
		const long tx = 5001;
		const decimal volume = 10m;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// A buy at 90 stands well below the bid, so nothing in the book crosses it and it rests.
		await run.SendAsync(NewOrder(tx, account, Sides.Buy, OrderTypes.Limit, 90m, volume, _start.AddSeconds(1)), CancellationToken);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(tx, out _), "the order rests to begin with");

		// A trade prints at 89 - below where the order stands, which is where it would have been
		// filled had the print been a counterparty.
		await run.SendAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = _securityId,
			ServerTime = _start.AddSeconds(2),
			LocalTime = _start.AddSeconds(2),
			TradePrice = 89m,
			TradeVolume = 1m,
			TradeId = 1,
		}, CancellationToken);

		IsNull(run.Executions.FirstOrDefault(m => m.HasTradeInfo() && m.OriginalTransactionId == tx),
			"a print is not a counterparty, so it cannot have filled the order");

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(tx, out var order),
			"and the order is still there to be filled by one");

		AreEqual(volume, order.Balance, "for everything it was placed for");
		AreEqual(0m, PositionOf(engine, account), "the account took on no position from someone else's trade");
	}
}
