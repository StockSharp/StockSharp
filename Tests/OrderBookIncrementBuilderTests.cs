namespace StockSharp.Tests;

[TestClass]
public class OrderBookIncrementBuilderTests : BaseTestClass
{
	private static readonly DateTime _time = new(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

	private static SecurityId CreateSec() => new() { SecurityCode = "TEST", BoardCode = "TEST" };

	// A price-keyed book message: the quotes carry no positions, which is how a feed sends them when
	// it names its levels by price instead of numbering them.
	private static QuoteChangeMessage CreateBook(QuoteChangeStates state, QuoteChange[] bids, QuoteChange[] asks) => new()
	{
		SecurityId = CreateSec(),
		ServerTime = _time,
		State = state,
		Bids = bids,
		Asks = asks,
	};

	private static QuoteChange Quote(decimal price, decimal volume) => new(price, volume);

	private static string Levels(QuoteChange[] quotes)
		=> quotes.Select(q => FormattableString.Invariant($"{q.Price}:{q.Volume}")).JoinComma();

	// Everything the builder warned about, in the order it said it.
	private static List<string> RecordWarnings(OrderBookIncrementBuilder builder)
	{
		var warnings = new List<string>();

		builder.Log += m =>
		{
			if (m.Level == LogLevels.Warning)
				warnings.Add(m.Message);
		};

		return warnings;
	}

	// A cache that only keeps the book up to date has no use for the whole book after every change: stating it
	// costs as much as the book is deep, and the change itself a level or two.
	[TestMethod]
	public void Fold_ThenSnapshot_StatesWhatApplyStates()
	{
		var applied = new OrderBookIncrementBuilder(CreateSec());
		var folded = new OrderBookIncrementBuilder(CreateSec());

		var snapshot = CreateBook(QuoteChangeStates.SnapshotComplete, [Quote(99, 2), Quote(100, 1)], [Quote(102, 4), Quote(101, 3)]);
		var increment = CreateBook(QuoteChangeStates.Increment, [Quote(100, 0), Quote(98, 5)], [Quote(101, 7)]);

		applied.TryApply(snapshot);
		var stated = applied.TryApply(increment);

		IsTrue(folded.TryFold(snapshot));
		IsTrue(folded.TryFold(increment));

		var asked = folded.GetSnapshot(null);

		AreEqual("99:2,98:5", Levels(asked.Bids));
		AreEqual("101:7,102:4", Levels(asked.Asks));
		AreEqual(Levels(stated.Bids), Levels(asked.Bids));
		AreEqual(Levels(stated.Asks), Levels(asked.Asks));
		AreEqual(CreateSec(), asked.SecurityId);
	}

	[TestMethod]
	public void Snapshot_CutToADepth_StatesTheBestLevelsOfEachSide()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		IsTrue(builder.TryFold(CreateBook(QuoteChangeStates.SnapshotComplete,
			[Quote(98, 1), Quote(100, 2), Quote(99, 3)],
			[Quote(103, 4), Quote(101, 5), Quote(102, 6)])));

		var best = builder.GetSnapshot(2);

		AreEqual("100:2,99:3", Levels(best.Bids));
		AreEqual("101:5,102:6", Levels(best.Asks));

		var all = builder.GetSnapshot(10);

		AreEqual("100:2,99:3,98:1", Levels(all.Bids));
		AreEqual("101:5,102:6,103:4", Levels(all.Asks));
	}

	[TestMethod]
	public void Fold_OfAChangeTheBookCannotTake_LeavesNoBook()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		IsFalse(builder.TryFold(CreateBook(QuoteChangeStates.Increment, [Quote(100, 1)], [])));
	}

	[TestMethod]
	public void Fold_OfASnapshotStillBeingSent_LeavesNoBookUntilItIsComplete()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		IsFalse(builder.TryFold(CreateBook(QuoteChangeStates.SnapshotStarted, [Quote(100, 1)], [])));
		IsFalse(builder.TryFold(CreateBook(QuoteChangeStates.SnapshotBuilding, [Quote(99, 2)], [Quote(101, 3)])));
		IsTrue(builder.TryFold(CreateBook(QuoteChangeStates.SnapshotComplete, [], [Quote(102, 4)])));

		var book = builder.GetSnapshot(null);

		AreEqual("100:1,99:2", Levels(book.Bids));
		AreEqual("101:3,102:4", Levels(book.Asks));
	}

	[TestMethod]
	public void Fold_OfABookKeptByPosition_StatesItByPosition()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		IsTrue(builder.TryFold(new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids =
			[
				new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 },
				new QuoteChange { Price = 99m, Volume = 20m, Action = QuoteChangeActions.New, StartPosition = 1 },
			],
			Asks = [],
		}));

		IsTrue(builder.TryFold(new QuoteChangeMessage
		{
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange { Price = 99m, Volume = 25m, Action = QuoteChangeActions.Update, StartPosition = 1 }],
			Asks = [],
		}));

		AreEqual("100:10,99:25", Levels(builder.GetSnapshot(null).Bids));
		AreEqual("100:10", Levels(builder.GetSnapshot(1).Bids));
	}

	// The point of folding: what a change costs must not grow with the book it lands in.
	[TestMethod]
	public void Fold_OfOneLevel_DoesNotCostTheWholeBook()
	{
		const int depth = 20000;

		var builder = new OrderBookIncrementBuilder(CreateSec());

		IsTrue(builder.TryFold(CreateBook(QuoteChangeStates.SnapshotComplete,
			[.. Enumerable.Range(0, depth).Select(i => Quote(100000 - i, 1))],
			[.. Enumerable.Range(1, depth).Select(i => Quote(100000 + i, 1))])));

		var increment = CreateBook(QuoteChangeStates.Increment, [Quote(100000, 2), Quote(100000.5m, 1)], [Quote(100001, 0)]);

		// Once before it is measured, so nothing that is done once is counted.
		IsTrue(builder.TryFold(increment));

		var before = GC.GetAllocatedBytesForCurrentThread();

		for (var i = 0; i < 100; i++)
			IsTrue(builder.TryFold(increment));

		var perFold = (GC.GetAllocatedBytesForCurrentThread() - before) / 100;

		// Stating a book of this depth takes megabytes; a level or two takes a few hundred bytes.
		IsTrue(perFold < 16 * 1024, $"Folding one change into a book of {depth} levels a side allocated {perFold} bytes.");
	}

	[TestMethod]
	public void New_InsertAtEnd_Works()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		var change = new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids = [new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 }],
			Asks = []
        };

		var full = builder.TryApply(change);

		IsNotNull(full);
		AreEqual(1, full.Bids.Length);
		AreEqual(100m, full.Bids[0].Price);
		AreEqual(10m, full.Bids[0].Volume);
	}

	[TestMethod]
	public void Update_StartPosEqualsCount_ReturnsNullWhenInvalid()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		// initial snapshot with one quote
		var snapshot = new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids = [new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 }],
			Asks = []
        };

		builder.TryApply(snapshot);

		// Update with startPos == count (1) -> should return null
		var update = new QuoteChangeMessage
		{
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange { Price = 100m, Volume = 15m, Action = QuoteChangeActions.Update, StartPosition = 1 }],
			Asks = []
        };

		IsNull(builder.TryApply(update));
	}

	[TestMethod]
	public void Delete_EndPositionLessThanStart_ReturnsNullWhenInvalid()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		// add two quotes
		var add = new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids = [
				new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 },
				new QuoteChange { Price = 99m, Volume = 20m, Action = QuoteChangeActions.New, StartPosition = 1 }
			],
			Asks = []
        };

		builder.TryApply(add);

		var del = new QuoteChangeMessage
		{
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange { Action = QuoteChangeActions.Delete, StartPosition = 1, EndPosition = 0 }],
			Asks = []
        };

		IsNull(builder.TryApply(del));
	}

	[TestMethod]
	public void Delete_RemoveSingleAtPosition_Works()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		// add two quotes
		var add = new QuoteChangeMessage
		{
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids = [
				new QuoteChange { Price = 100m, Volume = 10m, Action = QuoteChangeActions.New, StartPosition = 0 },
				new QuoteChange { Price = 99m, Volume = 20m, Action = QuoteChangeActions.New, StartPosition = 1 }
			],
			Asks = []
        };

		var full = builder.TryApply(add);
		IsNotNull(full);
		AreEqual(2, full.Bids.Length);

		// delete the first (startPos=0) with no EndPosition -> remove single
		var del = new QuoteChangeMessage
		{
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange { Action = QuoteChangeActions.Delete, StartPosition = 0 }],
			Asks = []
        };

		var after = builder.TryApply(del);
		IsNotNull(after);
		AreEqual(1, after.Bids.Length);
		AreEqual(99m, after.Bids[0].Price);
	}

	[TestMethod]
	public void NullChange_Throws()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		ThrowsExactly<ArgumentNullException>(() => builder.TryApply(null));
	}

	[TestMethod]
	public void NullState_Throws()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		var msg = new QuoteChangeMessage { State = null };
		ThrowsExactly<ArgumentException>(() => builder.TryApply(msg));
	}

	[TestMethod]
	public void Update_OutOfBoundsPosition_ReturnsNull()
	{
		var builder = new OrderBookIncrementBuilder(
			new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" });

		// Create a message that triggers Update action with invalid position
		var change = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" },
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids =
			[
				new QuoteChange
				{
					Price = 100,
					Volume = 10,
					Action = QuoteChangeActions.Update,
					StartPosition = 0 // Position that doesn't exist in empty book
				}
			],
			Asks = []
		};

		IsNull(builder.TryApply(change));
	}

	[TestMethod]
	public void Delete_InvalidRange_ReturnsNull()
	{
		var builder = new OrderBookIncrementBuilder(
			new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" });

		// First add some quotes
		var addChange = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" },
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			Bids =
			[
				new QuoteChange { Price = 100, Volume = 10, Action = QuoteChangeActions.New, StartPosition = 0 },
				new QuoteChange { Price = 99, Volume = 20, Action = QuoteChangeActions.New, StartPosition = 1 }
			],
			Asks = []
		};
		builder.TryApply(addChange);

		// Now try to delete with EndPosition < StartPosition (invalid)
		var deleteChange = new QuoteChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" },
			ServerTime = DateTime.UtcNow,
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids =
			[
				new QuoteChange
				{
					Action = QuoteChangeActions.Delete,
					StartPosition = 1,
					EndPosition = 0 // Bug: EndPosition < StartPosition
				}
			],
			Asks = []
		};

		IsNull(builder.TryApply(deleteChange));
	}

	/// <summary>
	/// An increment says how a book changed, so with no snapshot behind it there is nothing to change
	/// and applying it would invent depth that was never quoted. The builder drops it and reports the
	/// subscription once rather than on every message, so a feed that joined in the middle is visible
	/// in the log without burying everything else in it.
	/// </summary>
	[TestMethod]
	public void IncrementBeforeSnapshot_WarnsOnceAndIsDropped()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		var warnings = RecordWarnings(builder);

		const long subscriptionId = 42;

		for (var i = 0; i < 3; i++)
			IsNull(builder.TryApply(CreateBook(QuoteChangeStates.Increment, [Quote(100m - i, 10m)], []), subscriptionId), "an increment with no snapshot behind it cannot make a book");

		HasCount(1, warnings, "a feed that keeps sending increments before its snapshot is reported once, not once per message");
		Contains(nameof(QuoteChangeStates.Increment), warnings[0], "the warning says which message was refused");
		Contains($"{subscriptionId}", warnings[0], "and which subscription sent it");
	}

	/// <summary>
	/// The single warning is a mute on one subscription, not on the builder: two subscribers sharing
	/// a security each hear about their own broken feed, so one noisy feed cannot hide another.
	/// </summary>
	[TestMethod]
	public void IncrementBeforeSnapshot_WarnsOncePerSubscription()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		var warnings = RecordWarnings(builder);

		foreach (var subscriptionId in new long[] { 1, 2, 1, 2 })
			IsNull(builder.TryApply(CreateBook(QuoteChangeStates.Increment, [Quote(100m, 10m)], []), subscriptionId));

		HasCount(2, warnings, "each of the two subscriptions is reported, and each of them only once");
		IsTrue(warnings.Any(w => w.Contains("sub=1")), "the first subscription is named");
		IsTrue(warnings.Any(w => w.Contains("sub=2")), "the second subscription is named");
	}

	/// <summary>
	/// A snapshot that arrives in parts is not a book until its last part does. The builder hands
	/// nothing out while it is still building, so a subscriber never sees - or trades against - half
	/// the depth of an instrument.
	/// </summary>
	[TestMethod]
	public void SnapshotBuilding_PublishesNothingUntilTheSnapshotIsComplete()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		var warnings = RecordWarnings(builder);

		IsNull(builder.TryApply(CreateBook(QuoteChangeStates.SnapshotBuilding, [Quote(100m, 10m)], [Quote(101m, 5m)]), 1), "one part of a snapshot is not a book");
		IsNull(builder.TryApply(CreateBook(QuoteChangeStates.SnapshotBuilding, [Quote(99m, 20m)], []), 1), "and neither is the next one");

		var full = builder.TryApply(CreateBook(QuoteChangeStates.SnapshotComplete, [Quote(100m, 10m), Quote(99m, 20m)], [Quote(101m, 5m)]), 1);

		IsNotNull(full, "the completed snapshot is handed out as a book");
		AreEqual(2, full.Bids.Length);
		AreEqual(1, full.Asks.Length);
		IsEmpty(warnings, "delivering a snapshot part by part is an ordinary feed, not a broken one");
	}

	/// <summary>
	/// The parts of a snapshot are the snapshot: what the feed quoted while it was building has to be
	/// in the book that its completion hands out. Lose them and the subscriber gets a book holding
	/// only its last part, with the rest of the depth silently gone.
	/// </summary>
	[TestMethod]
	public void MultiPartSnapshot_KeepsThePartsDeliveredWhileBuilding()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());

		IsNull(builder.TryApply(CreateBook(QuoteChangeStates.SnapshotBuilding, [Quote(100m, 10m)], []), 1));

		var full = builder.TryApply(CreateBook(QuoteChangeStates.SnapshotComplete, [Quote(99m, 20m)], []), 1);

		IsNotNull(full);
		AreEqual(2, full.Bids.Length, "the part quoted while building and the part that completed the snapshot are both in the book");
		AreEqual(100m, full.Bids[0].Price, "the best bid was quoted in the first part");
		AreEqual(99m, full.Bids[1].Price);
	}

	/// <summary>
	/// Announcing the start of a snapshot is how a feed opens, so the first message of a fresh
	/// subscription saying exactly that is ordinary traffic. Treating it as a broken transition
	/// reports every healthy feed as faulty on its very first message.
	/// </summary>
	[TestMethod]
	public void SnapshotStarted_OpeningAFeed_IsNotReportedAsABrokenTransition()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		var warnings = RecordWarnings(builder);

		IsNull(builder.TryApply(CreateBook(QuoteChangeStates.SnapshotStarted, [], []), 1), "the announcement of a snapshot is not a book yet");

		IsEmpty(warnings, "a feed that opens by announcing its snapshot is doing nothing wrong");
	}

	/// <summary>
	/// Part of a snapshot cannot be applied to a book that is already complete - the builder has no
	/// way to tell what that part is replacing. It refuses the message and keeps the book it has, so
	/// a stray message costs the subscriber neither its depth nor the increments that follow.
	/// </summary>
	[TestMethod]
	public void SnapshotComplete_FollowedByABuildingPart_IsRefusedAndTheBookStands()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		var warnings = RecordWarnings(builder);

		IsNotNull(builder.TryApply(CreateBook(QuoteChangeStates.SnapshotComplete, [Quote(100m, 10m)], [Quote(101m, 5m)]), 1));

		// A zero volume is how a quote is taken off the book, so this message would empty the bid side.
		IsNull(builder.TryApply(CreateBook(QuoteChangeStates.SnapshotBuilding, [Quote(100m, 0m)], []), 1), "a snapshot part cannot be applied on top of a finished book");
		HasCount(1, warnings, "the refused transition is reported");

		var full = builder.TryApply(CreateBook(QuoteChangeStates.Increment, [Quote(99m, 20m)], []), 1);

		IsNotNull(full, "the subscription goes on from the book it already had");
		AreEqual(2, full.Bids.Length, "the refused message left the book alone");
		AreEqual(100m, full.Bids[0].Price);
		AreEqual(10m, full.Bids[0].Volume, "the quote the refused message would have removed is still quoted");
		AreEqual(99m, full.Bids[1].Price);
	}

	/// <summary>
	/// The once-per-subscription warning mutes one broken run, not the subscription for good: a feed
	/// that recovers with a whole snapshot and then breaks again is reported again, so the second
	/// fault is not hidden behind the first.
	/// </summary>
	[TestMethod]
	public void AFreshSnapshot_ClearsTheMuteSoALaterBreakIsReportedAgain()
	{
		var builder = new OrderBookIncrementBuilder(CreateSec());
		var warnings = RecordWarnings(builder);

		const long subscriptionId = 7;

		IsNull(builder.TryApply(CreateBook(QuoteChangeStates.Increment, [Quote(100m, 10m)], []), subscriptionId));
		HasCount(1, warnings, "the increment that arrived before any snapshot is reported");

		IsNotNull(builder.TryApply(CreateBook(QuoteChangeStates.SnapshotComplete, [Quote(100m, 10m)], []), subscriptionId), "the feed recovers by sending a whole snapshot");

		IsNull(builder.TryApply(CreateBook(QuoteChangeStates.SnapshotBuilding, [Quote(99m, 20m)], []), subscriptionId), "and then breaks again");
		HasCount(2, warnings, "the second break is reported rather than swallowed by the mute the first one set");
	}
}
