namespace StockSharp.Tests;

/// <summary>
/// Tests for <see cref="AssociatedSecurityAdapter"/>.
/// </summary>
[TestClass]
public class AssociatedSecurityAdapterTests : BaseTestClass
{
	private const string _code = "SBER";
	private const string _boardA = "TQBR";
	private const string _boardB = "SMAL";

	private static SecurityId CreateId(string boardCode)
		=> new() { SecurityCode = _code, BoardCode = boardCode };

	private static QuoteChangeMessage CreateDepth(SecurityId securityId, (decimal price, decimal volume)[] bids, (decimal price, decimal volume)[] asks)
	{
		return new QuoteChangeMessage
		{
			SecurityId = securityId,
			ServerTime = new(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
			LocalTime = new(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
			State = null,
			Bids = [.. bids.Select(q => new QuoteChange(q.price, q.volume))],
			Asks = [.. asks.Select(q => new QuoteChange(q.price, q.volume))],
		};
	}

	private static QuoteChangeMessage TakeAssociated(List<Message> output)
	{
		var books = output
			.OfType<QuoteChangeMessage>()
			.Where(q => q.SecurityId.BoardCode.EqualsIgnoreCase(SecurityId.AssociatedBoardCode))
			.ToArray();

		IsTrue(books.Length > 0, "No associated book was produced.");
		return books[^1];
	}

	private static Dictionary<decimal, decimal> ToMap(QuoteChange[] quotes)
	{
		var map = new Dictionary<decimal, decimal>();

		foreach (var quote in quotes)
			map[quote.Price] = map.TryGetValue(quote.Price, out var volume) ? volume + quote.Volume : quote.Volume;

		return map;
	}

	[TestMethod]
	public async Task AssociatedBookMergesBoardsIntoOneSortedBook()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter([DataType.MarketDepth]);
		using var adapter = new AssociatedSecurityAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardA),
			[(100m, 5m), (99m, 7m)],
			[(101m, 4m), (102m, 6m)]), token);

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardB),
			[(100.5m, 3m), (98.5m, 2m)],
			[(100.9m, 1m), (103m, 8m)]), token);

		var associated = TakeAssociated(output);

		// The aggregate of two venues is still one order book, so its levels must be best-first:
		// bids 100.5, 100, 99, 98.5 and asks 100.9, 101, 102, 103 - merged from the two inputs by price.
		// Extensions.Verify is this codebase's own definition of a well-formed book (bids descending,
		// asks ascending, no duplicate price, best bid below best ask).
		IsTrue(associated.Verify(), "Merged associated book must be a well-formed order book.");

		associated.Bids.Select(q => q.Price).ToArray().AssertEqual(new[] { 100.5m, 100m, 99m, 98.5m });
		associated.Bids.Select(q => q.Volume).ToArray().AssertEqual(new[] { 3m, 5m, 7m, 2m });

		associated.Asks.Select(q => q.Price).ToArray().AssertEqual(new[] { 100.9m, 101m, 102m, 103m });
		associated.Asks.Select(q => q.Volume).ToArray().AssertEqual(new[] { 1m, 4m, 6m, 8m });
	}

	[TestMethod]
	public async Task AssociatedBookSumsVolumeOfTheSamePriceOnBothBoards()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter([DataType.MarketDepth]);
		using var adapter = new AssociatedSecurityAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardA), [(100m, 5m)], [(101m, 2m)]), token);
		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardB), [(100m, 3m)], [(101m, 4m)]), token);

		var associated = TakeAssociated(output);

		// One price traded on two venues is one price level with the liquidity of both: 5 + 3 = 8 to buy
		// and 2 + 4 = 6 to sell. Two entries at the same price would let a consumer read the touch twice.
		associated.Bids.Length.AssertEqual(1);
		associated.Bids[0].Price.AssertEqual(100m);
		associated.Bids[0].Volume.AssertEqual(8m);

		associated.Asks.Length.AssertEqual(1);
		associated.Asks[0].Price.AssertEqual(101m);
		associated.Asks[0].Volume.AssertEqual(6m);
	}

	[TestMethod]
	public async Task AssociatedBookKeepsTheQuotesThatMadeACombinedLevel()
	{
		var inner = new RecordingPassThroughMessageAdapter([DataType.MarketDepth]);
		using var adapter = new AssociatedSecurityAdapter(inner);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var boardA = CreateDepth(CreateId(_boardA), [], []);
		boardA.Bids =
		[
			new QuoteChange(100m, 5m, 2, QuoteConditions.Indicative)
			{
				StartPosition = 3,
				EndPosition = 4,
				Action = QuoteChangeActions.Update,
			},
		];

		await inner.SendOutMessageAsync(boardA, CancellationToken);

		// The builder owns its snapshot; a producer is free to reuse the message after publishing it.
		boardA.Bids[0].Volume = 500m;
		boardA.Bids[0].OrdersCount = 20;

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardB), [(100m, 7m)], []), CancellationToken);

		var level = TakeAssociated(output).Bids.Single();
		level.Volume.AssertEqual(12m);
		level.OrdersCount.AssertEqual(2);
		level.InnerQuotes.AssertNotNull();
		level.InnerQuotes.Length.AssertEqual(2);

		var source = level.InnerQuotes.Single(q => q.BoardCode.EqualsIgnoreCase(_boardA));
		source.Volume.AssertEqual(5m);
		source.OrdersCount.AssertEqual(2);
		source.Condition.AssertEqual(QuoteConditions.Indicative);
		source.StartPosition.AssertEqual(3);
		source.EndPosition.AssertEqual(4);
		source.Action.AssertEqual(QuoteChangeActions.Update);
	}

	[TestMethod]
	public async Task AnIncomingAssociatedBookIsNotCountedAsAnotherVenue()
	{
		var inner = new RecordingPassThroughMessageAdapter([DataType.MarketDepth]);
		using var adapter = new AssociatedSecurityAdapter(inner);
		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardA), [(100m, 5m)], []), CancellationToken);
		await inner.SendOutMessageAsync(CreateDepth(CreateId(SecurityId.AssociatedBoardCode), [(100m, 1000m)], []), CancellationToken);
		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardB), [(100m, 3m)], []), CancellationToken);

		TakeAssociated(output).Bids.Single().Volume.AssertEqual(8m,
			"ALL is an aggregate of venues, not an extra venue whose liquidity can be added again");
	}

	[TestMethod]
	public async Task AssociatedBookForgetsBoardsAfterReset()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter([DataType.MarketDepth]);
		using var adapter = new AssociatedSecurityAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardA), [(100m, 5m)], [(101m, 4m)]), token);
		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardB), [(99m, 7m)], [(102m, 6m)]), token);

		await adapter.SendInMessageAsync(new ResetMessage(), token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardA), [(100m, 5m)], [(101m, 4m)]), token);

		var associated = TakeAssociated(output);

		// Reset discards accumulated state, so the only board heard from since is the only one in the
		// aggregate: bid 100 x 5 and ask 101 x 4. The board B levels quoted before the reset are stale.
		var bids = ToMap(associated.Bids);
		var asks = ToMap(associated.Asks);

		bids.Count.AssertEqual(1);
		bids[100m].AssertEqual(5m);

		asks.Count.AssertEqual(1);
		asks[101m].AssertEqual(4m);
	}

	[TestMethod]
	public async Task AssociatedBookKeepsOnlyTheLatestBookOfEachBoard()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter([DataType.MarketDepth]);
		using var adapter = new AssociatedSecurityAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardA), [(100m, 5m)], [(101m, 4m)]), token);
		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardB), [(99m, 7m)], [(102m, 6m)]), token);

		output.Clear();

		await inner.SendOutMessageAsync(CreateDepth(CreateId(_boardA), [(100m, 9m)], [(101m, 1m)]), token);

		var associated = TakeAssociated(output);

		// A board sends a full book, not an increment, so the second board A book replaces the first:
		// 100 x 9 from board A plus 99 x 7 still standing on board B - never 5 + 9 accumulated.
		var bids = ToMap(associated.Bids);
		var asks = ToMap(associated.Asks);

		bids.Count.AssertEqual(2);
		bids[100m].AssertEqual(9m);
		bids[99m].AssertEqual(7m);

		asks.Count.AssertEqual(2);
		asks[101m].AssertEqual(1m);
		asks[102m].AssertEqual(6m);
	}

	[TestMethod]
	public async Task OriginalBoardBookIsForwardedUntouched()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter([DataType.MarketDepth]);
		using var adapter = new AssociatedSecurityAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var depth = CreateDepth(CreateId(_boardA), [(100m, 5m)], [(101m, 4m)]);

		await inner.SendOutMessageAsync(depth, token);

		// Aggregating into the ALL board adds a book, it does not consume the board's own one: the
		// subscriber of TQBR must still get its message, the same instance and still addressed to TQBR.
		var books = output.OfType<QuoteChangeMessage>().ToArray();
		books.Length.AssertEqual(2);

		var original = books.First(b => !b.SecurityId.BoardCode.EqualsIgnoreCase(SecurityId.AssociatedBoardCode));
		AreSame(depth, original);
		original.SecurityId.AssertEqual(CreateId(_boardA));
		original.Bids[0].Price.AssertEqual(100m);
		original.Asks[0].Price.AssertEqual(101m);
	}

	[TestMethod]
	public async Task Level1AndTicksAreRepublishedOnAssociatedBoard()
	{
		var token = CancellationToken;

		var inner = new RecordingPassThroughMessageAdapter([DataType.Level1, DataType.Ticks]);
		using var adapter = new AssociatedSecurityAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var time = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		var level1 = new Level1ChangeMessage
		{
			SecurityId = CreateId(_boardA),
			ServerTime = time,
		};

		level1.Add(Level1Fields.LastTradePrice, 100m);

		await inner.SendOutMessageAsync(level1, token);

		await inner.SendOutMessageAsync(new ExecutionMessage
		{
			SecurityId = CreateId(_boardA),
			DataTypeEx = DataType.Ticks,
			ServerTime = time,
			TradePrice = 100m,
			TradeVolume = 3m,
		}, token);

		// A board's own data is also offered under the ALL code, so a subscriber of the associated
		// security sees the same values without knowing which venue produced them.
		var l1All = output.OfType<Level1ChangeMessage>().Single(m => m.SecurityId.BoardCode.EqualsIgnoreCase(SecurityId.AssociatedBoardCode));
		l1All.SecurityId.SecurityCode.AssertEqual(_code);
		((decimal)l1All.Changes[Level1Fields.LastTradePrice]).AssertEqual(100m);

		var tickAll = output.OfType<ExecutionMessage>().Single(m => m.SecurityId.BoardCode.EqualsIgnoreCase(SecurityId.AssociatedBoardCode));
		tickAll.SecurityId.SecurityCode.AssertEqual(_code);
		tickAll.TradePrice.AssertEqual(100m);
		tickAll.TradeVolume.AssertEqual(3m);
	}

	/// <summary>
	/// A copy of an adapter is a second adapter, and a second adapter needs a link of its own. Given
	/// the very object the original wraps, the two become two heads on one connection: whatever
	/// either of them asks of the venue is answered to both, and disposing or reconnecting one
	/// disturbs the other.
	/// </summary>
	[TestMethod]
	public void ACopyOfTheAdapterWrapsACopyOfTheLink()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		using var adapter = new AssociatedSecurityAdapter(inner);

		using var clone = (AssociatedSecurityAdapter)adapter.Clone();

		AreNotSame(adapter.InnerAdapter, clone.InnerAdapter, "the copy has to have a link of its own, not the original's");
	}

	/// <summary>
	/// The other half of the same promise, and the one a user notices: work handed to the copy stays
	/// with the copy. A consumer that subscribed to the original is entitled to receive what the
	/// original was asked for and nothing else - messages belonging to a subscription somebody else
	/// made arrive under transaction ids it never issued, and it has no way to tell them from its own.
	/// </summary>
	[TestMethod]
	public async Task WorkGivenToTheCopyStaysWithTheCopy()
	{
		var token = CancellationToken;

		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		using var adapter = new AssociatedSecurityAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		using var clone = (AssociatedSecurityAdapter)adapter.Clone();

		await clone.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2001,
			SecurityId = CreateId(_boardA),
			DataType2 = DataType.MarketDepth,
		}, token);

		IsEmpty(output, "the original was asked for nothing, so its consumer must be handed nothing");
	}
}
