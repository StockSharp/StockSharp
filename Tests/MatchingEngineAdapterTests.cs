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

	/// <summary>
	/// A trade the venue printed - the most direct statement there is of what the instrument is
	/// trading at, and what the engine measures its stops against.
	/// </summary>
	private static ExecutionMessage Print(SecurityId securityId, decimal price, DateTime time)
		=> new()
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = securityId,
			LocalTime = time,
			ServerTime = time,
			TradePrice = price,
			TradeVolume = 1m,
			TradeId = 1,
		};

	private static decimal PositionOf(MatchingEngineAdapter engine, string account)
		=> PositionOf(engine, account, _securityId);

	private static decimal PositionOf(MatchingEngineAdapter engine, string account, SecurityId securityId)
		=> engine.PortfolioManager.GetPortfolio(account).GetPosition(securityId)?.CurrentValue ?? 0m;

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

	/// <summary>
	/// A condition that is a take-profit and nothing else - the shape a venue offering only
	/// take-profits sends, and the shape the engine has to recognise as a stop rather than as a
	/// priceless ordinary order.
	/// </summary>
	private sealed class TakeProfitOnlyCondition : OrderCondition, ITakeProfitOrderCondition
	{
		public decimal? ActivationPrice
		{
			get => (decimal?)Parameters.TryGetValue(nameof(ActivationPrice));
			set => Parameters[nameof(ActivationPrice)] = value;
		}

		public decimal? ClosePositionPrice
		{
			get => (decimal?)Parameters.TryGetValue(nameof(ClosePositionPrice));
			set => Parameters[nameof(ClosePositionPrice)] = value;
		}
	}

	#endregion

	/// <summary>
	/// Sweeping several price levels produces several trades but only one terminal transition for the
	/// order that caused them. A Done row between fills makes the later fills arrive for an order the
	/// caller has already discarded.
	/// </summary>
	[TestMethod]
	public async Task AFillAcrossSeveralLevelsFinishesTheOrderOnce()
	{
		const long transactionId = 101;

		var engine = new MatchingEngineAdapter();
		engine.Settings.IncreaseDepthVolume = false;
		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)],
			[new QuoteChange(101m, 2m), new QuoteChange(102m, 3m)]), CancellationToken);

		await run.SendAsync(NewOrder(transactionId, "Client", Sides.Buy, OrderTypes.Limit,
			102m, 5m, _start.AddSeconds(1)), CancellationToken);

		var rows = run.Executions.Where(m => m.OriginalTransactionId == transactionId).ToArray();
		var trades = rows.Where(m => m.HasTradeInfo()).ToArray();
		var terminal = rows.Where(m => m.HasOrderInfo() && m.OrderState?.IsFinal() == true).ToArray();

		AreEqual(2, trades.Length, "the order sweeps the two ask levels");
		AreEqual(5m, trades.Sum(m => m.TradeVolume ?? 0m));
		AreEqual(1, terminal.Length, "one order reaches its terminal state once");
		AreEqual(0m, terminal[0].Balance);
		IsTrue(rows.IndexOf(terminal[0]) > rows.IndexOf(trades[^1]),
			"the terminal transition follows every fill that produced it");
	}

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
	/// A close nobody can price is a close that did not happen, and the caller has to be told: answered
	/// by silence it reads the request as carried out and its risk as flat, while the position stands.
	/// </summary>
	[TestMethod]
	public async Task AGroupCancelThatCannotCloseAPositionSaysSoInsteadOfPassingSilently()
	{
		const string account = "Client";
		const long groupTx = 6301;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(PositionRow(_securityId, account, 5m, 90m, _start), CancellationToken);

		// Offers only: nothing is bid for the instrument, so a long of 5 has nothing to be sold into.
		await run.SendAsync(VenueBook(_securityId, _start, [], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(new OrderGroupCancelMessage
		{
			TransactionId = groupTx,
			PortfolioName = account,
			Mode = OrderGroupCancelModes.ClosePositions,
			LocalTime = _start.AddSeconds(1),
		}, CancellationToken);

		AreEqual(0, run.Executions.Count(m => m.HasTradeInfo()), "an empty bid side can fill nothing");
		AreEqual(5m, PositionOf(engine, account), "so the account still holds the 5 it was long");

		IsTrue(run.Executions.Any(m => m.OriginalTransactionId == groupTx && m.OrderState == OrderStates.Failed && m.Error is not null),
			"the request was not carried out, and this engine already answers a ClosePositions it cannot carry out with a failure on the request's own id");
	}

	/// <summary>
	/// A close takes what the market holds and no more: against a bid of 3 a long of 10 closes 3 and
	/// keeps 7. What could not be closed has to stay a working order for exactly those 7, or the caller
	/// is left with an open position and nothing standing to close it.
	/// </summary>
	[TestMethod]
	public async Task AGroupCancelClosesOnlyWhatTheBookCanTakeAndKeepsTheRestWorking()
	{
		const string account = "Client";
		const long groupTx = 6401;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(PositionRow(_securityId, account, 10m, 90m, _start), CancellationToken);

		// 3 lots are bid at 100, so 3 of the 10 can be sold there and 10 - 3 = 7 cannot.
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 3m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(new OrderGroupCancelMessage
		{
			TransactionId = groupTx,
			PortfolioName = account,
			Mode = OrderGroupCancelModes.ClosePositions,
			LocalTime = _start.AddSeconds(1),
		}, CancellationToken);

		var fills = run.Executions.Where(m => m.HasTradeInfo()).ToArray();

		AreEqual(1, fills.Length, "one level was bid, so one fill comes of taking it");
		AreEqual(3m, fills[0].TradeVolume, "the bid held 3 lots, and 3 is all a close can take from it");
		AreEqual(7m, PositionOf(engine, account), "10 held less the 3 closed leaves 7 open");

		var closeTx = fills[0].OriginalTransactionId;
		var lastRow = run.Executions.LastOrDefault(m => m.OriginalTransactionId == closeTx && m.HasOrderInfo && !m.HasTradeInfo());

		IsNotNull(lastRow, "the caller has to be told where the closing order stands");
		AreEqual(OrderStates.Active, lastRow.OrderState, "the part the market could not take is still working, not finished");
		AreEqual(7m, lastRow.Balance, "and it works for exactly the 7 that stayed open");

		IsTrue(engine.GetSecurityState(_securityId).OrderBook.HasLevel(Sides.Sell, 100m),
			"an order reported as working has to stand in the book it works in");
	}

	/// <summary>
	/// A close reaches only the account the request names. Another account's position is not the
	/// caller's to flatten, and closing it trades on its behalf without its asking.
	/// </summary>
	[TestMethod]
	public async Task AGroupCancelClosesOnlyTheAccountItNames()
	{
		const string named = "Client";
		const string other = "Other";
		const long groupTx = 6501;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(PositionRow(_securityId, named, 5m, 90m, _start), CancellationToken);
		await run.SendAsync(PositionRow(_securityId, other, 4m, 95m, _start), CancellationToken);

		// 20 lots are bid, more than both positions together, so liquidity decides nothing here.
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 20m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(new OrderGroupCancelMessage
		{
			TransactionId = groupTx,
			PortfolioName = named,
			Mode = OrderGroupCancelModes.ClosePositions,
			LocalTime = _start.AddSeconds(1),
		}, CancellationToken);

		var fills = run.Executions.Where(m => m.HasTradeInfo()).ToArray();

		AreEqual(1, fills.Length, "one account was named, so one position is closed");
		AreEqual(named, fills[0].PortfolioName, "and the fill belongs to the account that was named");
		AreEqual(5m, fills[0].TradeVolume, "which was long 5");

		AreEqual(0m, PositionOf(engine, named), "the named account ends the request flat");
		AreEqual(4m, PositionOf(engine, other), "the account nobody asked about keeps the 4 it held");
	}

	/// <summary>
	/// Side narrows what a close touches. With it set on an account holding one long and one short,
	/// exactly one is closed and the other is left as it was - ignoring the filter flattens both,
	/// misreading it flattens neither.
	/// </summary>
	[TestMethod]
	public async Task SideNarrowsWhichPositionsAGroupCancelCloses()
	{
		const string account = "Client";
		const long groupTx = 6601;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(PositionRow(_securityId, account, 5m, 90m, _start), CancellationToken);
		await run.SendAsync(PositionRow(_otherSecurityId, account, -4m, 40m, _start), CancellationToken);

		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);
		await run.SendAsync(VenueBook(_otherSecurityId, _start, [new QuoteChange(50m, 10m)], [new QuoteChange(51m, 10m)]), CancellationToken);

		await run.SendAsync(new OrderGroupCancelMessage
		{
			TransactionId = groupTx,
			PortfolioName = account,
			Side = Sides.Buy,
			Mode = OrderGroupCancelModes.ClosePositions,
			LocalTime = _start.AddSeconds(1),
		}, CancellationToken);

		var fills = run.Executions.Where(m => m.HasTradeInfo()).ToArray();
		var longAfter = PositionOf(engine, account, _securityId);
		var shortAfter = PositionOf(engine, account, _otherSecurityId);

		AreEqual(1, fills.Length, $"a side picks one of the two positions, not both and not neither; long {longAfter}, short {shortAfter}");

		var longClosed = longAfter == 0m;
		var shortClosed = shortAfter == 0m;

		IsTrue(longClosed != shortClosed, $"exactly one of the two must be closed; long {longAfter}, short {shortAfter}");

		if (longClosed)
			AreEqual(-4m, shortAfter, "the position the side passed over must be left exactly as it was");
		else
			AreEqual(5m, longAfter, "the position the side passed over must be left exactly as it was");
	}

	/// <summary>
	/// One request that both cancels orders and closes positions has to cancel first. The account's own
	/// bid of 100 stands above the venue's 99, so a close priced before the cancel meets that bid: the
	/// sell and the buy are both the account's, they net to nothing, and it ends the request still long.
	/// </summary>
	[TestMethod]
	public async Task AGroupCancelCancelsTheAccountsOrdersBeforeClosingItsPositions()
	{
		const string account = "Client";
		const long restingTx = 6701;
		const long groupTx = 6702;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(PositionRow(_securityId, account, 5m, 90m, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start, [new QuoteChange(99m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// A buy of 5 at 100 rests: 100 is under the ask of 101, and it becomes the best bid.
		await run.SendAsync(NewOrder(restingTx, account, Sides.Buy, OrderTypes.Limit, 100m, 5m, _start.AddSeconds(1)), CancellationToken);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(restingTx, out _),
			"the order has to be working before the request can reach it");

		await run.SendAsync(new OrderGroupCancelMessage
		{
			TransactionId = groupTx,
			PortfolioName = account,
			Mode = OrderGroupCancelModes.CancelOrders | OrderGroupCancelModes.ClosePositions,
			LocalTime = _start.AddSeconds(2),
		}, CancellationToken);

		var cancelRow = run.Executions.LastOrDefault(m => m.OriginalTransactionId == restingTx && m.HasOrderInfo && !m.HasTradeInfo());

		IsNotNull(cancelRow, "a request that says CancelOrders has to answer for the order it ended");
		AreEqual(OrderStates.Done, cancelRow.OrderState, "the working order is finished by the cancel");
		IsFalse(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(restingTx, out _),
			"and it no longer stands to be filled");

		var fills = run.Executions.Where(m => m.HasTradeInfo()).ToArray();

		AreEqual(1, fills.Length, "one fill closes the long, and a second one would mean it traded with itself");
		AreEqual(5m, fills[0].TradeVolume, "the whole position is closed");
		AreEqual(99m, fills[0].TradePrice, "the account's own bid of 100 was cancelled first, so the close meets the venue's 99");

		IsFalse(fills.Any(m => m.OriginalTransactionId == restingTx),
			"the close must not be filled by the order the same request cancelled");

		AreEqual(0m, PositionOf(engine, account), "the account ends the request flat");
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

		// Registration held all hundred lots at the ask of 101 the order was checked against. What stays
		// blocked afterwards is the one lot actually bought, at the price it was bought at.
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

	/// <summary>
	/// A replace is a cancel and a registration, and the registration can be refused. Whichever of the
	/// two orders the engine keeps, the caller has to be told the new one failed and has to be told
	/// what became of the old one - and cancelling both has to leave the account blocking nothing,
	/// because money held against an order no cancel can reach is money it never gets back.
	/// </summary>
	[TestMethod]
	public async Task AReplaceWhoseNewOrderIsRefusedLeavesTheAccountBlockingNothing()
	{
		const string account = "Trader";
		const decimal begin = 1000m;
		const long oldTx = 6001;
		const long newTx = 6002;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, begin, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// A buy of 2 at 50 costs 100 of the 1000 the account holds, and rests: 50 is under the ask.
		await run.SendAsync(NewOrder(oldTx, account, Sides.Buy, OrderTypes.Limit, 50m, 2m, _start.AddSeconds(1)), CancellationToken);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(oldTx, out _),
			"the order to be replaced has to be resting first");
		IsTrue(engine.PortfolioManager.GetPortfolio(account).BlockedMoney > 0m,
			"and holding money against it, or there is nothing for the replace to release");

		// Replacing it with 100 at 60 asks the account for 6000 where it was funded with 1000.
		await run.SendAsync(new OrderReplaceMessage
		{
			TransactionId = newTx,
			OriginalTransactionId = oldTx,
			SecurityId = _securityId,
			PortfolioName = account,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			Price = 60m,
			Volume = 100m,
			LocalTime = _start.AddSeconds(2),
		}, CancellationToken);

		var newRows = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == newTx).ToArray();

		IsTrue(newRows.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"the replacement is unpayable, so the caller must be told it failed and why; states were {newRows.Select(m => m.OrderState.ToString()).JoinComma()}");

		IsTrue(run.Executions.Any(m => m.HasOrderInfo() && m.OriginalTransactionId == oldTx),
			"and told what became of the order it asked to replace, or it cannot know which of the two is live");

		// Cancelling both transactions the caller named: one of them is not live and answers so, which
		// is not an error of the account's. Nothing traded and no position was taken, so what the
		// account blocks afterwards is 0 and what it can trade with is the 1000 it was funded with.
		await run.SendAsync(new OrderCancelMessage
		{
			TransactionId = 6003,
			OriginalTransactionId = oldTx,
			SecurityId = _securityId,
			PortfolioName = account,
			LocalTime = _start.AddSeconds(3),
		}, CancellationToken);

		await run.SendAsync(new OrderCancelMessage
		{
			TransactionId = 6004,
			OriginalTransactionId = newTx,
			SecurityId = _securityId,
			PortfolioName = account,
			LocalTime = _start.AddSeconds(4),
		}, CancellationToken);

		var portfolio = engine.PortfolioManager.GetPortfolio(account);

		AreEqual(0m, PositionOf(engine, account), "a refused replace trades nothing");
		AreEqual(0m, portfolio.BlockedMoney,
			"no order of this account is live any more, so none of its money may stay blocked");
		AreEqual(begin, portfolio.AvailableMoney,
			"and everything it was funded with is available to trade with again");
	}

	/// <summary>
	/// An order that reaches its expiry is taken off the book and reported Done, and the money it was
	/// holding is the account's again: nothing is live to hold it. Anything less and every expiry
	/// quietly costs the account buying power it can never get back, since no cancel can reach an
	/// order that is already gone.
	/// </summary>
	[TestMethod]
	public async Task AnExpiredOrderGivesTheAccountItsMoneyBack()
	{
		const string account = "Trader";
		const decimal begin = 1000m;
		const long restingTx = 7001;
		const long refusedTx = 7002;
		const long afterExpiryTx = 7003;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, begin, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// A buy of 5 at 90 rests: 90 is under the ask, and nothing in the book crosses it. It can never
		// cost more than the 90 it names, which is the price it was checked against, so 5 * 90 = 450
		// of the 1000 is held against it.
		var resting = NewOrder(restingTx, account, Sides.Buy, OrderTypes.Limit, 90m, 5m, _start.AddSeconds(1));
		resting.TillDate = _start.AddSeconds(10);
		await run.SendAsync(resting, CancellationToken);

		var portfolio = engine.PortfolioManager.GetPortfolio(account);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(restingTx, out _),
			"the order has to be resting before it can expire");
		AreEqual(450m, portfolio.BlockedMoney, "5 lots held at the 90 they were checked against");
		AreEqual(550m, portfolio.AvailableMoney, "leaving the rest of the 1000 to trade with");

		// A second buy of 9 at 90 costs 810, more than the 550 still free, so it cannot be paid for
		// while the first order is alive.
		await run.SendAsync(NewOrder(refusedTx, account, Sides.Buy, OrderTypes.Limit, 90m, 9m, _start.AddSeconds(2)), CancellationToken);

		IsTrue(run.Executions.Any(m => m.OriginalTransactionId == refusedTx && m.OrderState == OrderStates.Failed && m.Error is not null),
			"810 is beyond the 550 the account has free while the first order holds the rest");

		await run.SendAsync(new TimeMessage
		{
			LocalTime = _start.AddSeconds(30),
			ServerTime = _start.AddSeconds(30),
		}, CancellationToken);

		var expiryRow = run.Executions.LastOrDefault(m => m.OriginalTransactionId == restingTx && m.HasOrderInfo && !m.HasTradeInfo());

		IsNotNull(expiryRow, "the caller has to be told the order ended");
		AreEqual(OrderStates.Done, expiryRow.OrderState, "an expired order is finished, not still working");
		AreEqual(5m, expiryRow.Balance, "and nothing of it was ever filled");

		IsFalse(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(restingTx, out _),
			"an expired order is no longer live");
		IsFalse(engine.GetSecurityState(_securityId).OrderBook.HasLevel(Sides.Buy, 90m),
			"and no longer stands in the book to be filled");

		AreEqual(0m, portfolio.BlockedMoney,
			"no order of this account is live any more, so none of its money may stay blocked");
		AreEqual(begin, portfolio.AvailableMoney,
			"and everything it was funded with is available to trade with again");

		// The same 810 the account could not pay for while the order was alive.
		await run.SendAsync(NewOrder(afterExpiryTx, account, Sides.Buy, OrderTypes.Limit, 90m, 9m, _start.AddSeconds(31)), CancellationToken);

		var afterExpiryRows = run.Executions.Where(m => m.OriginalTransactionId == afterExpiryTx && m.HasOrderInfo && !m.HasTradeInfo()).ToArray();

		IsFalse(afterExpiryRows.Any(m => m.OrderState == OrderStates.Failed),
			$"the money the expiry released has to be spendable; states were {afterExpiryRows.Select(m => m.OrderState.ToString()).JoinComma()}");
		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(afterExpiryTx, out _),
			"and the order it paid for has to be working");
	}

	/// <summary>
	/// When an order is partly filled before it expires, the account owes back the margin of the
	/// balance that never traded and nothing else: the filled lots were paid for and are a position
	/// now, so what the expiry releases is the remainder alone.
	/// </summary>
	[TestMethod]
	public async Task AnExpiredPartialFillGivesBackOnlyWhatItsRemainderHeld()
	{
		const string account = "Trader";
		const decimal begin = 10000m;
		const long tx = 8001;
		const long refusedTx = 8002;
		const long afterExpiryTx = 8003;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, begin, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// A buy of 15 at 101 takes the 10 lots offered and rests for the other 5. Registration held all
		// 15 at the 101 they were checked against (1515); the 10 that traded release their share of
		// that at the same average, so 5 * 101 = 505 stays held against the balance.
		var order = NewOrder(tx, account, Sides.Buy, OrderTypes.Limit, 101m, 15m, _start.AddSeconds(1));
		order.TillDate = _start.AddSeconds(10);
		await run.SendAsync(order, CancellationToken);

		var portfolio = engine.PortfolioManager.GetPortfolio(account);
		var position = portfolio.GetPosition(_securityId);

		AreEqual(10m, PositionOf(engine, account), "the book offered 10 of the 15");
		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(tx, out var resting),
			"and the balance rests, waiting for a counterparty");
		AreEqual(5m, resting.Balance, "which is the other 5");
		AreEqual(505m, position.TotalBidsValue, "5 lots still held at the 101 they were checked against");

		var blockedBefore = portfolio.BlockedMoney;

		// 86 lots at 100 cost 8600, beyond what the account has free while the balance is alive.
		await run.SendAsync(NewOrder(refusedTx, account, Sides.Buy, OrderTypes.Limit, 100m, 86m, _start.AddSeconds(2)), CancellationToken);

		IsTrue(run.Executions.Any(m => m.OriginalTransactionId == refusedTx && m.OrderState == OrderStates.Failed && m.Error is not null),
			$"8600 is beyond the {portfolio.AvailableMoney} the account has free while the balance holds 500");

		await run.SendAsync(new TimeMessage
		{
			LocalTime = _start.AddSeconds(30),
			ServerTime = _start.AddSeconds(30),
		}, CancellationToken);

		var expiryRow = run.Executions.LastOrDefault(m => m.OriginalTransactionId == tx && m.HasOrderInfo && !m.HasTradeInfo());

		IsNotNull(expiryRow, "the caller has to be told the order ended");
		AreEqual(OrderStates.Done, expiryRow.OrderState, "an expired order is finished, not still working");
		AreEqual(5m, expiryRow.Balance, "with the 5 that never found a counterparty");

		IsFalse(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(tx, out _),
			"an expired order is no longer live");
		IsFalse(engine.GetSecurityState(_securityId).OrderBook.HasLevel(Sides.Buy, 101m),
			"and no longer stands in the book to be filled");

		AreEqual(10m, PositionOf(engine, account), "expiry ends the balance, it does not undo the fill");
		AreEqual(0m, position.TotalBidsVolume, "no buy order of this account is live any more");
		AreEqual(0m, position.TotalBidsValue, "so nothing may stay held against one");
		AreEqual(blockedBefore - 505m, portfolio.BlockedMoney,
			"the expiry owes back the 5 unfilled lots at the 101 they were held at, and no more: the 10 that traded were paid for");

		// The same 8600 the account could not pay for while the balance was alive.
		await run.SendAsync(NewOrder(afterExpiryTx, account, Sides.Buy, OrderTypes.Limit, 100m, 86m, _start.AddSeconds(31)), CancellationToken);

		var afterExpiryRows = run.Executions.Where(m => m.OriginalTransactionId == afterExpiryTx && m.HasOrderInfo && !m.HasTradeInfo()).ToArray();

		IsFalse(afterExpiryRows.Any(m => m.OrderState == OrderStates.Failed),
			$"the money the expiry released has to be spendable; states were {afterExpiryRows.Select(m => m.OrderState.ToString()).JoinComma()}");
	}

	/// <summary>
	/// A take-profit rests until its price is reached, like any other stop. Not recognising the
	/// condition sends it to the book as an ordinary order carrying no price, and a sell at no price
	/// takes every bid there is - the account is flattened at the market the moment it asks to be
	/// taken out higher.
	/// </summary>
	[TestMethod]
	public async Task ATakeProfitDoesNotSellIntoTheBookBeforeItsPriceIsReached()
	{
		const string account = "Client";
		const long takeTx = 9101;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// Take me out at 110, said an account whose market is at 100.
		var take = NewOrder(takeTx, account, Sides.Sell, OrderTypes.Conditional, 0m, 10m, _start.AddSeconds(1));
		take.Condition = new TakeProfitOnlyCondition { ActivationPrice = 110m };

		await run.SendAsync(take, CancellationToken);

		var fills = run.Executions.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == takeTx).ToArray();

		AreEqual(0, fills.Length,
			$"the market is at 100 and the take asks for 110, so nothing may trade yet; {fills.Sum(m => m.TradeVolume ?? 0m)} did");
		AreEqual(0m, PositionOf(engine, account), "and the account may not be put short by an order that was never triggered");

		var restsAsStop = engine.StopOrderManager.GetStopIds(_securityId).Contains(takeTx);
		var refused = run.Executions.Any(m => m.HasOrderInfo() && m.OriginalTransactionId == takeTx && m.OrderState == OrderStates.Failed);

		IsTrue(restsAsStop || refused,
			"a take-profit is either taken as a stop and waits for its price, or refused outright - it may not be quietly accepted as something else");
	}

	/// <summary>
	/// The engine numbers every order it accepts, and a status request naming that number must find
	/// that order. The number the venue issued and the transaction the caller issued come from two
	/// different generators, so reading one as the other finds nothing.
	/// </summary>
	[TestMethod]
	public async Task AStatusRequestFindsTheOrderByTheNumberTheVenueGaveIt()
	{
		const string account = "Client";
		const long orderTx = 9201;
		const long statusTx = 9202;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// A buy at 50 is under the ask, so it rests and can be asked about.
		await run.SendAsync(NewOrder(orderTx, account, Sides.Buy, OrderTypes.Limit, 50m, 2m, _start.AddSeconds(1)), CancellationToken);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(orderTx, out var resting),
			"the order has to be resting before its status can be asked for");

		var venueOrderId = resting.OrderId;

		IsNotNull(venueOrderId, "the venue numbers what it accepts, or there is no number to ask by");
		AreNotEqual(orderTx, venueOrderId.Value, "and that number is its own, not the transaction the caller issued");

		await run.SendAsync(new OrderStatusMessage
		{
			TransactionId = statusTx,
			OrderId = venueOrderId,
			IsSubscribe = true,
			LocalTime = _start.AddSeconds(2),
		}, CancellationToken);

		var rows = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == statusTx).ToArray();

		AreEqual(1, rows.Length, "the order asked for by the venue's own number must be the one answered");
		AreEqual<long?>(venueOrderId, rows[0].OrderId, "and answered under that number");
		AreEqual(orderTx, rows[0].TransactionId, "naming the transaction it was placed under, so the caller can match it to its own order");
	}

	/// <summary>
	/// A replace naming an order the engine holds nothing for is refused, and refused once. Answering
	/// it with a finished row for the unknown original tells the caller an order it still believes is
	/// live has filled in full - it books a position that never traded and stops waiting for the order.
	/// </summary>
	[TestMethod]
	public async Task AReplaceOfAnOrderTheEngineNeverHeldDoesNotReportItFilled()
	{
		const string account = "Client";
		const long unknownTx = 9301;
		const long newTx = 9302;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		await run.SendAsync(new OrderReplaceMessage
		{
			TransactionId = newTx,
			OriginalTransactionId = unknownTx,
			SecurityId = _securityId,
			PortfolioName = account,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			Price = 50m,
			Volume = 1m,
			LocalTime = _start.AddSeconds(1),
		}, CancellationToken);

		IsTrue(run.Executions.Any(m => m.HasOrderInfo() && m.OriginalTransactionId == newTx && m.OrderState == OrderStates.Failed && m.Error is not null),
			"there was nothing to replace, so the replacement must be refused and the reason stated");

		var originalRows = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == unknownTx).ToArray();

		IsFalse(originalRows.Any(m => m.OrderState == OrderStates.Done && m.Balance == 0m),
			$"an order the engine never held may not be reported finished with nothing left of it; rows were {originalRows.Select(m => $"{m.OrderState}/{m.Balance}").JoinComma()}");
	}

	/// <summary>
	/// An order is held against the account at the price it was checked against, and a side of the book
	/// standing empty changes nothing about that: a buy at 100 for 10 lots commits 1000 whether or not
	/// anyone is bidding. Holding nothing lets the very same money be checked again and spent twice, so
	/// the second order here has to be refused.
	/// </summary>
	[TestMethod]
	public async Task AnOrderOnAnEmptyBookSideStillCostsTheAccountWhatItCommits()
	{
		const string account = "Trader";
		const decimal begin = 1000m;
		const long firstTx = 9101;
		const long secondTx = 9102;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, begin, _start), CancellationToken);

		// Offers only: nobody is bidding, so the buy side of the book is empty.
		await run.SendAsync(VenueBook(_securityId, _start, [], [new QuoteChange(101m, 10m)]), CancellationToken);

		// 10 lots at 100 cost 1000 - the whole account - and rest, since 100 is under the ask of 101.
		await run.SendAsync(NewOrder(firstTx, account, Sides.Buy, OrderTypes.Limit, 100m, 10m, _start.AddSeconds(1)), CancellationToken);

		var portfolio = engine.PortfolioManager.GetPortfolio(account);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(firstTx, out _),
			"the account can afford the first order, so it has to be resting");

		var blockedAfterFirst = portfolio.BlockedMoney;

		// The same order again, for the same money the account no longer has.
		await run.SendAsync(NewOrder(secondTx, account, Sides.Buy, OrderTypes.Limit, 100m, 10m, _start.AddSeconds(2)), CancellationToken);

		var secondRows = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == secondTx).ToArray();

		IsTrue(secondRows.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"the first order already spoke for all 1000, so the second cannot be paid for; states were {secondRows.Select(m => m.OrderState.ToString()).JoinComma()}");

		AreEqual(1000m, blockedAfterFirst,
			"a resting buy of 10 at 100 commits 1000 of the account, whether or not the bid side of the book is empty");
	}

	/// <summary>
	/// What an order holds is what it was checked against: a limit can never trade above its own price,
	/// so that is the price the account is asked for and the price it stays held at. Holding it at the
	/// best of its own side instead is a number the account was never checked against - too little when
	/// the order is priced above the touch, and the difference is spendable twice.
	/// </summary>
	[TestMethod]
	public async Task AnOrderIsHeldAtThePriceItWasCheckedAgainstNotAtTheBestOfItsOwnSide()
	{
		const string account = "Trader";
		const decimal begin = 1000m;
		const long restingTx = 9201;
		const long refusedTx = 9202;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, begin, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(200m, 10m)]), CancellationToken);

		// A buy of 5 at 110 stands above the bid of 100 and under the ask of 200, so it rests. It was
		// checked against 5 * 110 = 550 of the 1000, and that is what it holds.
		await run.SendAsync(NewOrder(restingTx, account, Sides.Buy, OrderTypes.Limit, 110m, 5m, _start.AddSeconds(1)), CancellationToken);

		var portfolio = engine.PortfolioManager.GetPortfolio(account);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(restingTx, out _),
			"the order has to be resting for its money to be held");

		var blockedAfterResting = portfolio.BlockedMoney;

		// 4 lots at 120 cost 480, beyond the 450 the account still has free.
		await run.SendAsync(NewOrder(refusedTx, account, Sides.Buy, OrderTypes.Limit, 120m, 4m, _start.AddSeconds(2)), CancellationToken);

		var refusedRows = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == refusedTx).ToArray();

		IsTrue(refusedRows.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"480 is beyond the 450 left free by an order holding 550; states were {refusedRows.Select(m => m.OrderState.ToString()).JoinComma()}");

		AreEqual(550m, blockedAfterResting,
			"5 lots at the 110 the order named and was checked against, not at the 100 someone else is bidding");
	}

	/// <summary>
	/// Both sides of a fill are trades, and both book a position, so both are priced through the same
	/// seam. What the tariff charges either side is the caller's business - the seam is handed the row
	/// and can read its side from it - but the engine has to ask about the maker's fill too, or half
	/// the trades of an internalised book are free whatever the tariff says.
	/// </summary>
	[TestMethod]
	public async Task AMakerFillIsPricedTheSameWayTheTakerFillIs()
	{
		const string maker = "Maker";
		const string taker = "Taker";
		const long makerTx = 9301;
		const long takerTx = 9302;
		const decimal fee = 3m;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(maker, 100_000m, _start), CancellationToken);
		await run.SendAsync(MoneyRow(taker, 100_000m, _start), CancellationToken);

		// Offers far above and nobody else bidding: the maker's buy at 100 rests alone on its side.
		await run.SendAsync(VenueBook(_securityId, _start, [], [new QuoteChange(200m, 10m)]), CancellationToken);
		await run.SendAsync(NewOrder(makerTx, maker, Sides.Buy, OrderTypes.Limit, 100m, 10m, _start.AddSeconds(1)), CancellationToken);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(makerTx, out _),
			"the maker has to be resting for the taker to hit it");

		var state = engine.GetSecurityState(_securityId);
		var regMsg = NewOrder(takerTx, taker, Sides.Sell, OrderTypes.Limit, 100m, 4m, _start.AddSeconds(2));

		var order = new EmulatorOrder
		{
			TransactionId = regMsg.TransactionId,
			Side = regMsg.Side,
			Price = regMsg.Price,
			Balance = regMsg.Volume,
			Volume = regMsg.Volume,
			PortfolioName = regMsg.PortfolioName,
			OrderType = regMsg.OrderType,
			ServerTime = regMsg.LocalTime,
			LocalTime = regMsg.LocalTime,
			MarginPrice = regMsg.Price,
			OrderId = 7701,
		};

		var matchResult = new OrderMatcher().Match(order, state.OrderBook, new MatchingSettings
		{
			PriceStep = state.PriceStep,
			VolumeStep = state.VolumeStep,
		});

		var replyMsg = new ExecutionMessage
		{
			HasOrderInfo = true,
			DataTypeEx = DataType.Transactions,
			ServerTime = regMsg.LocalTime,
			LocalTime = regMsg.LocalTime,
			OriginalTransactionId = takerTx,
			PortfolioName = taker,
			Side = regMsg.Side,
			OrderState = OrderStates.Active,
		};

		var results = new List<Message> { replyMsg };

		// The tariff prices every row it is handed the same way; nothing about it singles out a side.
		engine.EmitRegistrationResult(regMsg, order, matchResult, replyMsg, _ => fee, results);

		var trades = results.OfType<ExecutionMessage>().Where(m => m.HasTradeInfo()).ToArray();

		AreEqual(2, trades.Length,
			$"one fill has two sides, the taker's and the maker's; got {trades.Select(m => m.OriginalTransactionId.ToString()).JoinComma()}");

		var takerRow = trades.First(m => m.OriginalTransactionId == takerTx);
		var makerRow = trades.First(m => m.OriginalTransactionId == makerTx);

		AreEqual((decimal?)fee, takerRow.Commission, "the taker's fill is priced by the tariff");
		AreEqual((decimal?)fee, makerRow.Commission, "and the maker's fill is a fill too, so the tariff is asked about it as well");
		AreEqual(fee, engine.PortfolioManager.GetPortfolio(maker).Commission,
			"and the maker's account is charged what its own row says it owes");
	}

	/// <summary>
	/// A stop ends one of two ways - its owner takes it back, or the market reaches it and it fills -
	/// and the row that reports the ending has to say which. Both endings are reported as the same
	/// final state on the same transaction, so what tells them apart is the instrument the row names
	/// and how much of the order is left: a cancelled stop never traded and is outstanding in full, a
	/// triggered one has been bought and has nothing left. A cancel row that states neither cannot be
	/// booked at all - the reader either records a fill that never happened or misses one that did.
	/// </summary>
	[TestMethod]
	public async Task ACancelledStopIsReportedWithEnoughToTellItFromATriggeredOne()
	{
		const string account = "Client";
		const long cancelledTx = 9401;
		const long triggeredTx = 9402;
		const decimal volume = 3m;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// Two stops of the same size, both waiting for the market to reach 105.
		var cancelled = NewOrder(cancelledTx, account, Sides.Buy, OrderTypes.Conditional, 0m, volume, _start.AddSeconds(1));
		cancelled.Condition = new StopOrderCondition { ActivationPrice = 105m };
		await run.SendAsync(cancelled, CancellationToken);

		var triggered = NewOrder(triggeredTx, account, Sides.Buy, OrderTypes.Conditional, 0m, volume, _start.AddSeconds(1));
		triggered.Condition = new StopOrderCondition { ActivationPrice = 105m };
		await run.SendAsync(triggered, CancellationToken);

		// One is taken back by its owner.
		await run.SendAsync(new OrderCancelMessage
		{
			TransactionId = 9403,
			OriginalTransactionId = cancelledTx,
			SecurityId = _securityId,
			PortfolioName = account,
			LocalTime = _start.AddSeconds(2),
		}, CancellationToken);

		// The market then prints through 105, firing the other.
		await run.SendAsync(new Level1ChangeMessage
		{
			SecurityId = _securityId,
			LocalTime = _start.AddSeconds(3),
			ServerTime = _start.AddSeconds(3),
		}
		.Add(Level1Fields.LastTradePrice, 106m), CancellationToken);

		var cancelRow = run.Executions.LastOrDefault(m => m.HasOrderInfo() && !m.HasTradeInfo() && m.OriginalTransactionId == cancelledTx);
		var triggeredRow = run.Executions.LastOrDefault(m => m.HasOrderInfo() && !m.HasTradeInfo() && m.OriginalTransactionId == triggeredTx);

		IsNotNull(cancelRow, "the owner has to be told the stop it took back has ended");
		IsNotNull(triggeredRow, "and told what became of the one the market reached");

		AreEqual(OrderStates.Done, cancelRow.OrderState, "a cancelled stop is finished");
		AreEqual(OrderStates.Done, triggeredRow.OrderState, "and so is one that fired and filled");

		AreEqual(_securityId, cancelRow.SecurityId,
			"a row naming no instrument cannot be booked against one, and the stop was placed on this one");
		AreEqual((decimal?)volume, cancelRow.OrderVolume, "the cancelled stop was placed for three lots");
		AreEqual((decimal?)volume, cancelRow.Balance, "and none of them traded, so all three are still outstanding");

		AreEqual((decimal?)0m, triggeredRow.Balance, "the stop the market reached was filled in full, so nothing of it is left");
		AreNotEqual(cancelRow.Balance, triggeredRow.Balance, "which is what tells the two endings apart");
	}

	/// <summary>
	/// A replace asks for one order to become another, and the account is entitled to keep the first
	/// when the second is refused. Taking the working order off the book before the replacement has
	/// been accepted leaves the account with nothing where it asked for an order at a different price:
	/// it is flat on an instrument it believes it has a bid in, and no message it sent can bring the
	/// order back, because the cancel was never what it asked for.
	/// </summary>
	[TestMethod]
	public async Task ARefusedReplaceLeavesTheOriginalOrderWorking()
	{
		const string account = "Trader";
		const decimal begin = 1000m;
		const long oldTx = 9501;
		const long newTx = 9502;

		var engine = new MatchingEngineAdapter();
		engine.Settings.CheckMoney = true;

		var run = new EngineRun(engine);

		await run.SendAsync(MoneyRow(account, begin, _start), CancellationToken);
		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// A buy of 2 at 50 rests: 50 is under the ask, so nothing in the book crosses it.
		await run.SendAsync(NewOrder(oldTx, account, Sides.Buy, OrderTypes.Limit, 50m, 2m, _start.AddSeconds(1)), CancellationToken);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(oldTx, out _),
			"the order to be replaced has to be resting first");

		var portfolio = engine.PortfolioManager.GetPortfolio(account);
		var blockedBefore = portfolio.BlockedMoney;

		IsTrue(blockedBefore > 0m, "and holding money against it, or there is nothing to watch here");

		// Replacing it with 100 lots at 60 asks the account for 6000 where it was funded with 1000.
		await run.SendAsync(new OrderReplaceMessage
		{
			TransactionId = newTx,
			OriginalTransactionId = oldTx,
			SecurityId = _securityId,
			PortfolioName = account,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			Price = 60m,
			Volume = 100m,
			LocalTime = _start.AddSeconds(2),
		}, CancellationToken);

		var newRows = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == newTx).ToArray();

		IsTrue(newRows.Any(m => m.OrderState == OrderStates.Failed && m.Error is not null),
			$"the replacement is unpayable, so it must be refused and the reason stated; states were {newRows.Select(m => m.OrderState.ToString()).JoinComma()}");

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(oldTx, out var original),
			"the replacement was refused, so the order it was to replace has to be standing untouched");
		AreEqual(2m, original.Balance, "for everything it was placed for");
		IsTrue(engine.GetSecurityState(_securityId).OrderBook.HasLevel(Sides.Buy, 50m),
			"an order reported as working has to stand in the book it works in");

		var oldRows = run.Executions.Where(m => m.HasOrderInfo() && m.OriginalTransactionId == oldTx).ToArray();

		IsFalse(oldRows.Any(m => m.OrderState == OrderStates.Done || m.OrderState == OrderStates.Failed),
			$"an order that is still working may not be reported finished; states were {oldRows.Select(m => m.OrderState.ToString()).JoinComma()}");

		AreEqual(blockedBefore, portfolio.BlockedMoney,
			"and the money held against a live order stays held, or the same money can be spent twice");
	}

	/// <summary>
	/// A session asks the venue what an account holds and waits for the answer. An account the venue
	/// was never told a cash balance for is an account all the same - it can hold a position taken
	/// here - so the lookup has to answer for it: accepted, the account named, what it holds stated,
	/// and the subscription closed out. Answered with silence, the session waits for an answer that
	/// never comes and never learns about the position it is carrying.
	/// </summary>
	[TestMethod]
	public async Task APortfolioLookupAnswersEvenWithNoMoneyRow()
	{
		const string account = "NeverFunded";
		const long orderTx = 9601;
		const long lookupTx = 9602;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		// No money row for this account ever reaches the engine; it takes a position all the same.
		await run.SendAsync(NewOrder(orderTx, account, Sides.Buy, OrderTypes.Limit, 101m, 4m, _start.AddSeconds(1)), CancellationToken);

		AreEqual(4m, PositionOf(engine, account), "the buy has to fill, or there is nothing for the lookup to report");

		var before = run.Out.Count;

		await run.SendAsync(new PortfolioLookupMessage
		{
			TransactionId = lookupTx,
			PortfolioName = account,
			IsSubscribe = true,
			LocalTime = _start.AddSeconds(2),
		}, CancellationToken);

		var answer = run.Out.Skip(before).ToArray();

		var accepted = answer.OfType<SubscriptionResponseMessage>().FirstOrDefault(m => m.OriginalTransactionId == lookupTx);

		IsNotNull(accepted, "the session has to be told its lookup was accepted");
		IsNull(accepted.Error, "and accepted without an error: asking about an unfunded account is not a mistake");

		var named = answer.OfType<PortfolioMessage>().FirstOrDefault(m => m.OriginalTransactionId == lookupTx);

		IsNotNull(named, "an account the engine holds a position for has to be named by the lookup, funded or not");
		AreEqual(account, named.PortfolioName, "under the name it was asked about");

		var held = answer.OfType<PositionChangeMessage>().FirstOrDefault(m => m.PortfolioName == account && m.SecurityId == _securityId);

		IsNotNull(held, "and what the account holds has to be stated");
		AreEqual((decimal?)4m, (decimal?)held.Changes.TryGetValue(PositionChangeTypes.CurrentValue),
			"as the four lots it actually holds");

		IsTrue(answer.Any(m => m is SubscriptionOnlineMessage o && o.OriginalTransactionId == lookupTx),
			"and the lookup has to be closed out, or the session keeps waiting for an answer it has already been given");
	}

	/// <summary>
	/// Some of what the venue sends carries no clock at all - an instrument definition is a fact
	/// about the instrument, not about the moment. A resting order lives until the time it was given,
	/// and that time is reached by the clock moving forward, never by a message that has none: an
	/// order retired on the strength of an untimed message is an order the trader believed was
	/// working, taken off the book at a moment that never happened.
	/// </summary>
	[TestMethod]
	public async Task AMessageCarryingNoClockDoesNotRetireARestingOrder()
	{
		const string account = "Trader";
		const long restingTx = 9701;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);

		var resting = NewOrder(restingTx, account, Sides.Buy, OrderTypes.Limit, 90m, 5m, _start.AddSeconds(1));
		resting.TillDate = _start.AddSeconds(10);

		await run.SendAsync(resting, CancellationToken);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(restingTx, out _),
			"the order has to be resting before anything can take it off the book");

		// An instrument definition carries no LocalTime at all.
		await run.SendAsync(new SecurityMessage { SecurityId = _securityId }, CancellationToken);

		IsTrue(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(restingTx, out _),
			"a message with no clock is not the passing of time, so the order is still working");

		IsFalse(run.Executions.Any(m => m.OriginalTransactionId == restingTx && m.HasOrderInfo && m.OrderState == OrderStates.Done),
			"and nothing may be reported ended while it is still live");

		// Once the clock really does move past the order's own expiry, it ends as it was told to.
		await run.SendAsync(new TimeMessage
		{
			LocalTime = _start.AddSeconds(30),
			ServerTime = _start.AddSeconds(30),
		}, CancellationToken);

		IsFalse(engine.GetSecurityState(_securityId).OrderManager.TryGetOrder(restingTx, out _),
			"the expiry the order was given still holds once time actually reaches it");
	}

	/// <summary>
	/// A book deepened so a large order can fill is a convenience of replay, and it is still a book:
	/// every level it gains has to be a price someone could really have traded at. Walking the levels
	/// down past nothing hands the seller lots given away for zero - a fill the trader can read as a
	/// total loss on that part of the order, produced by the engine rather than by the market.
	/// </summary>
	[TestMethod]
	public async Task ABookDeepenedForALargeSellNeverBuysAtNothing()
	{
		const long tx = 9702;

		var engine = new MatchingEngineAdapter();
		engine.Settings.IncreaseDepthVolume = true;

		var run = new EngineRun(engine);

		// A cheap instrument whose step is large next to its price: walking the bids down reaches zero fast.
		await run.SendAsync(new SecurityMessage
		{
			SecurityId = _securityId,
			PriceStep = 1m,
			VolumeStep = 1m,
		}, CancellationToken);

		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(3m, 1m)], [new QuoteChange(4m, 1m)]), CancellationToken);

		await run.SendAsync(NewOrder(tx, "Client", Sides.Sell, OrderTypes.Market, 0m, 1000m, _start.AddSeconds(1)), CancellationToken);

		var fills = run.Executions.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == tx).ToArray();

		IsNotEmpty(fills, "a market sell into a book being deepened for it has to fill somewhere");
		IsTrue(fills.All(m => m.TradePrice > 0m),
			$"a price of zero or less is not a price anyone could have traded at; prices were {fills.Select(m => m.TradePrice.ToString()).JoinComma()}");
	}

	/// <summary>
	/// A stop is armed against the price the instrument trades at, and a print is the most direct
	/// statement of that price there is. Whether the news arrives as a print or as a quoted last
	/// price, the stop has to move the same way: a stop that answers only one of the two sits idle
	/// through the move it was placed for, and the protection the trader paid for never fires.
	/// </summary>
	[TestMethod]
	public async Task APrintPastAStopsPriceTriggersItJustAsAQuotedLastPriceDoes()
	{
		const string account = "Client";
		const long stopTx = 9703;

		static OrderRegisterMessage ArmedStop()
		{
			var stop = NewOrder(stopTx, account, Sides.Buy, OrderTypes.Conditional, 0m, 3m, _start.AddSeconds(1));
			stop.Condition = new StopOrderCondition { ActivationPrice = 105m };
			return stop;
		}

		static decimal FilledBy(EngineRun run)
			=> run.Executions
				.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == stopTx)
				.Sum(m => m.TradeVolume ?? 0m);

		var printRun = new EngineRun(new MatchingEngineAdapter());

		await printRun.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);
		await printRun.SendAsync(ArmedStop(), CancellationToken);
		await printRun.SendAsync(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = _securityId,
			LocalTime = _start.AddSeconds(2),
			ServerTime = _start.AddSeconds(2),
			TradePrice = 106m,
			TradeVolume = 1m,
			TradeId = 1,
		}, CancellationToken);

		var quoteRun = new EngineRun(new MatchingEngineAdapter());

		await quoteRun.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(101m, 10m)]), CancellationToken);
		await quoteRun.SendAsync(ArmedStop(), CancellationToken);
		await quoteRun.SendAsync(new Level1ChangeMessage
		{
			SecurityId = _securityId,
			LocalTime = _start.AddSeconds(2),
			ServerTime = _start.AddSeconds(2),
		}
		.Add(Level1Fields.LastTradePrice, 106m), CancellationToken);

		var byPrint = FilledBy(printRun);
		var byQuote = FilledBy(quoteRun);

		AreEqual(3m, byQuote, "a last price past the activation price arms and fills the stop in full");
		AreEqual(byQuote, byPrint, $"and a print at the same price has to do the same: printed {byPrint}, quoted {byQuote}");
	}

	/// <summary>
	/// An activation price marked as a percent states a distance from the market, not a price of its
	/// own. The percent is taken against the price the instrument is at when the stop is registered -
	/// the touch the stop's own order would trade against, which here is also the price it last
	/// printed - and a buy stop rests that far above it. Read as an absolute instead, "five percent"
	/// becomes a level of five, which the market is already far past, and the stop fires on the first
	/// price it sees rather than on the move it was placed for.
	/// </summary>
	[TestMethod]
	public async Task APercentActivationPriceIsADistanceFromTheMarketNotAPriceOfItsOwn()
	{
		const string account = "Client";
		const long stopTx = 9801;

		var run = new EngineRun(new MatchingEngineAdapter());

		// The market is at 100: the ask a buy would take, and the price the instrument last printed.
		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(99.5m, 10m)], [new QuoteChange(100m, 10m)]), CancellationToken);
		await run.SendAsync(Print(_securityId, 100m, _start.AddSeconds(1)), CancellationToken);

		var stop = NewOrder(stopTx, account, Sides.Buy, OrderTypes.Conditional, 0m, 3m, _start.AddSeconds(2));
		stop.Condition = new StopOrderCondition { ActivationPrice = 5m, IsActivationPricePercent = true };

		await run.SendAsync(stop, CancellationToken);

		decimal Filled() => run.Executions
			.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == stopTx)
			.Sum(m => m.TradeVolume ?? 0m);

		await run.SendAsync(Print(_securityId, 101m, _start.AddSeconds(3)), CancellationToken);

		AreEqual(0m, Filled(), $"the market moved one percent and the stop was armed five percent away, yet {Filled()} traded");

		await run.SendAsync(Print(_securityId, 104.99m, _start.AddSeconds(4)), CancellationToken);

		AreEqual(0m, Filled(), $"a hair short of five percent above 100 is still short of it, yet {Filled()} traded");

		await run.SendAsync(Print(_securityId, 105m, _start.AddSeconds(5)), CancellationToken);

		AreEqual(3m, Filled(), "five percent above a market of 100 is 105, and the stop has to fire there in full");
		AreEqual(3m, PositionOf(run.Engine, account), "and the account has to end up holding what the stop bought");
	}

	/// <summary>
	/// The percent runs the way the stop protects: a sell stop rests that far below the market, as a
	/// buy stop rests that far above it. Taking the number as an absolute puts a sell stop's trigger
	/// at five, a level the market will never fall to, so the protection the trader paid for never
	/// fires at all - the mirror image of the buy case, and the half that fails silently.
	/// </summary>
	[TestMethod]
	public async Task APercentActivationPriceRestsBelowTheMarketForASellStop()
	{
		const string account = "Client";
		const long stopTx = 9802;

		var run = new EngineRun(new MatchingEngineAdapter());

		// The market is at 100: the bid a sell would hit, and the price the instrument last printed.
		await run.SendAsync(VenueBook(_securityId, _start,
			[new QuoteChange(100m, 10m)], [new QuoteChange(100.5m, 10m)]), CancellationToken);
		await run.SendAsync(Print(_securityId, 100m, _start.AddSeconds(1)), CancellationToken);

		var stop = NewOrder(stopTx, account, Sides.Sell, OrderTypes.Conditional, 0m, 3m, _start.AddSeconds(2));
		stop.Condition = new StopOrderCondition { ActivationPrice = 5m, IsActivationPricePercent = true };

		await run.SendAsync(stop, CancellationToken);

		decimal Filled() => run.Executions
			.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == stopTx)
			.Sum(m => m.TradeVolume ?? 0m);

		await run.SendAsync(Print(_securityId, 95.01m, _start.AddSeconds(3)), CancellationToken);

		AreEqual(0m, Filled(), $"a hair above five percent below 100 has not reached the stop, yet {Filled()} traded");

		await run.SendAsync(Print(_securityId, 95m, _start.AddSeconds(4)), CancellationToken);

		AreEqual(3m, Filled(), "five percent below a market of 100 is 95, and the stop has to fire there in full");
	}

	/// <summary>
	/// A percent needs something to be a percent of. Stated for an instrument the engine has seen no
	/// price for, there is nothing to snapshot at registration time, so the stop cannot be armed:
	/// either the registration is refused, or it waits until it can be. What it may not do is keep
	/// the number as an absolute level - two, for two percent - because the first price the market
	/// ever states is already past it, and the stop fires on the instant instead of on a move.
	/// </summary>
	[TestMethod]
	public async Task APercentActivationPriceWithNoMarketToTakeItFromIsNotArmedAtTheRawNumber()
	{
		const string account = "Client";
		const long stopTx = 9803;

		var engine = new MatchingEngineAdapter();
		var run = new EngineRun(engine);

		// Nothing has been quoted or printed for this instrument yet.
		var stop = NewOrder(stopTx, account, Sides.Buy, OrderTypes.Conditional, 0m, 3m, _start);
		stop.Condition = new StopOrderCondition { ActivationPrice = 2m, IsActivationPricePercent = true };

		await run.SendAsync(stop, CancellationToken);

		await run.SendAsync(VenueBook(_securityId, _start.AddSeconds(1),
			[new QuoteChange(99.5m, 10m)], [new QuoteChange(100m, 10m)]), CancellationToken);
		await run.SendAsync(Print(_securityId, 100m, _start.AddSeconds(2)), CancellationToken);

		var filled = run.Executions
			.Where(m => m.HasTradeInfo() && m.OriginalTransactionId == stopTx)
			.Sum(m => m.TradeVolume ?? 0m);

		AreEqual(0m, filled, $"the market has not moved two percent off anything, so nothing may trade; {filled} did");

		var restsAsStop = engine.StopOrderManager.GetStopIds(_securityId).Contains(stopTx);
		var refused = run.Executions.Any(m => m.HasOrderInfo() && m.OriginalTransactionId == stopTx && m.OrderState == OrderStates.Failed);

		IsTrue(restsAsStop || refused,
			"a percent stated against a market the engine has never seen is either refused or still waiting - it may not be quietly armed at the raw number and spent on the first price");
	}

	/// <summary>
	/// <see cref="IPercentStopOrderCondition"/> is the whole of what a venue may say about reading a
	/// stop's numbers as percents, so every flag on it has to reach the stop the engine rests:
	/// raising one and changing nothing is the engine agreeing to a condition it then ignores. The
	/// flags are walked off the interface rather than named one by one, so a fourth one added to the
	/// contract fails here until it is carried through as well.
	/// </summary>
	[TestMethod]
	public async Task EveryPercentFlagAStopConditionStatesReachesTheStopTheEngineRests()
	{
		// Every number the flags speak about is stated, so raising any one of them has something to
		// act on and the runs differ only in the flag itself.
		static StopOrderCondition Condition(PropertyInfo raised)
		{
			var condition = new StopOrderCondition
			{
				ActivationPrice = 5m,
				ClosePositionPrice = 1m,
				IsTrailing = true,
				TrailingOffset = 2m,
			};

			raised?.SetValue(condition, true);

			return condition;
		}

		async Task<StopOrderInfo> RestedAsync(PropertyInfo raised)
		{
			const long tx = 9804;

			var engine = new MatchingEngineAdapter();
			var run = new EngineRun(engine);

			await run.SendAsync(VenueBook(_securityId, _start,
				[new QuoteChange(99.5m, 10m)], [new QuoteChange(100m, 10m)]), CancellationToken);
			await run.SendAsync(Print(_securityId, 100m, _start.AddSeconds(1)), CancellationToken);

			var stop = NewOrder(tx, "Client", Sides.Buy, OrderTypes.Conditional, 0m, 3m, _start.AddSeconds(2));
			stop.Condition = Condition(raised);

			await run.SendAsync(stop, CancellationToken);

			IsTrue(engine.StopOrderManager.Cancel(tx, out var info),
				$"the stop had to rest before {raised?.Name ?? "anything"} could be read off it");

			return info;
		}

		// Read off the resting stop by reflection too: a flag answered by a field this test does not
		// name is still a flag answered.
		static string Describe(StopOrderInfo info)
			=> typeof(StopOrderInfo)
				.GetProperties()
				.OrderBy(p => p.Name)
				.Select(p => $"{p.Name}={p.GetValue(info)}")
				.JoinComma();

		var flags = typeof(IPercentStopOrderCondition).GetProperties();

		IsTrue(flags.Length > 0, "the contract has to state at least one percent flag to be worth honouring");

		var silent = Describe(await RestedAsync(null));

		foreach (var flag in flags)
		{
			var raised = Describe(await RestedAsync(flag));

			AreNotEqual(silent, raised,
				$"{flag.Name} is stated by the condition and the stop the engine rests is the same either way: {raised}");
		}
	}
}
