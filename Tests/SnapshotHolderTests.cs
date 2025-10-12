namespace StockSharp.Tests;

[TestClass]
public class SnapshotHolderTests
{
	private static readonly SecurityId _secId1 = "AAPL@NASDAQ".ToSecurityId();
	private static readonly SecurityId _secId2 = "MSFT@NASDAQ".ToSecurityId();
	private static readonly DateTimeOffset _now = DateTimeOffset.UtcNow;

	#region Level1SnapshotHolder Tests

	[TestMethod]
	public void Level1_FirstProcess_CreatesSnapshot()
	{
		var holder = new Level1SnapshotHolder();
		var msg = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			LocalTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m)
		.TryAdd(Level1Fields.BestBidPrice, 99m);

		var result = holder.Process(msg, needResponse: true);

		result.AssertNotNull();
		result.SecurityId.AssertEqual(_secId1);
		result.Changes.Count.AssertEqual(2);
		result.Changes[Level1Fields.LastTradePrice].AssertEqual(100m);
	}

	[TestMethod]
	public void Level1_TryGetSnapshot_ReturnsClone()
	{
		var holder = new Level1SnapshotHolder();
		var msg = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m);

		holder.Process(msg, needResponse: false);

		holder.TryGetSnapshot(_secId1, out var snapshot).AssertTrue();
		snapshot.AssertNotNull();
		snapshot.Changes[Level1Fields.LastTradePrice].AssertEqual(100m);

		// Verify it's a clone
		snapshot.AssertNotSame(msg);
	}

	[TestMethod]
	public void Level1_TryGetSnapshot_NoSnapshot_ReturnsNull()
	{
		var holder = new Level1SnapshotHolder();
		holder.TryGetSnapshot(_secId1, out var snapshot).AssertFalse();
		snapshot.AssertNull();
	}

	[TestMethod]
	public void Level1_Process_UpdatesExisting_OnlyChanges()
	{
		var holder = new Level1SnapshotHolder();

		// First message
		var msg1 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m)
		.TryAdd(Level1Fields.BestBidPrice, 99m);

		holder.Process(msg1, needResponse: false);

		// Second message with update
		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(Level1Fields.LastTradePrice, 101m)
		.TryAdd(Level1Fields.BestBidPrice, 99m); // unchanged

		var diff = holder.Process(msg2, needResponse: true);

		diff.AssertNotNull();
		diff.Changes.Count.AssertEqual(1); // only changed field
		diff.Changes[Level1Fields.LastTradePrice].AssertEqual(101m);
		diff.Changes.ContainsKey(Level1Fields.BestBidPrice).AssertFalse();
	}

	[TestMethod]
	public void Level1_Process_NeedResponse_False_ReturnsNull_ForOptimization()
	{
        // When needResponse=false, it's acceptable to return null to skip diff calculations.
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m);

		holder.Process(msg1, needResponse: false);

		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(Level1Fields.LastTradePrice, 101m);

        var result = holder.Process(msg2, needResponse: false);
        result.AssertNull(); // Optimization: no diff computed when response isn't needed
	}

	[TestMethod]
	public void Level1_Process_UpdatesTimestamps()
	{
        // Snapshot should update timestamps on each Process call.
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			LocalTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m);

		holder.Process(msg1, needResponse: false);

		var laterTime = _now.AddMinutes(5);
		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = laterTime,
			LocalTime = laterTime,
		}
		.TryAdd(Level1Fields.BestBidPrice, 99m);

		holder.Process(msg2, needResponse: false);

		holder.TryGetSnapshot(_secId1, out var snapshot).AssertTrue();
		snapshot.ServerTime.AssertEqual(laterTime);
		snapshot.LocalTime.AssertEqual(laterTime);
	}

	[TestMethod]
	public void Level1_Process_NullMessage_ThrowsException()
	{
		var holder = new Level1SnapshotHolder();
		Assert.ThrowsExactly<ArgumentNullException>(() => holder.Process(null, needResponse: true));
	}

	[TestMethod]
	public void Level1_ResetSnapshot_SpecificSecurity_RemovesSnapshot()
	{
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage { SecurityId = _secId1, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 100m);
		var msg2 = new Level1ChangeMessage { SecurityId = _secId2, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 200m);

		holder.Process(msg1, needResponse: false);
		holder.Process(msg2, needResponse: false);

		holder.ResetSnapshot(_secId1);

		holder.TryGetSnapshot(_secId1, out var snap1).AssertFalse();
		snap1.AssertNull();
		holder.TryGetSnapshot(_secId2, out var snap2).AssertTrue();
		snap2.AssertNotNull();
	}

	[TestMethod]
	public void Level1_ResetSnapshot_Default_ClearsAll()
	{
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage { SecurityId = _secId1, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 100m);
		var msg2 = new Level1ChangeMessage { SecurityId = _secId2, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 200m);

		holder.Process(msg1, needResponse: false);
		holder.Process(msg2, needResponse: false);

		holder.ResetSnapshot(default);

		holder.TryGetSnapshot(_secId1, out var snap3).AssertFalse();
		snap3.AssertNull();
		holder.TryGetSnapshot(_secId2, out var snap4).AssertFalse();
		snap4.AssertNull();
	}

	[TestMethod]
	public void Level1_Process_NewField_AddsToSnapshot()
	{
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m);

		holder.Process(msg1, needResponse: false);

		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(Level1Fields.BestBidPrice, 99m);

		var diff = holder.Process(msg2, needResponse: true);

		diff.AssertNotNull();
		diff.Changes.Count.AssertEqual(1);
		diff.Changes[Level1Fields.BestBidPrice].AssertEqual(99m);

		holder.TryGetSnapshot(_secId1, out var snapshot2).AssertTrue();
		snapshot2.Changes.Count.AssertEqual(2);
	}

	#endregion

	#region OrderBookSnapshotHolder Tests

	[TestMethod]
	public void OrderBook_FirstSnapshot_CreatesSnapshot()
	{
		var holder = new OrderBookSnapshotHolder();
		var msg = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null, // full snapshot
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 20)],
		};

		var result = holder.Process(msg, needResponse: true);

		result.AssertNotNull();
		result.SecurityId.AssertEqual(_secId1);
		result.State.AssertEqual(QuoteChangeStates.SnapshotComplete);
		result.Bids.Length.AssertEqual(1);
		result.Asks.Length.AssertEqual(1);
	}

	[TestMethod]
	public void OrderBook_TryGetSnapshot_ReturnsClone()
	{
		var holder = new OrderBookSnapshotHolder();
		var msg = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};

		holder.Process(msg, needResponse: false);

		holder.TryGetSnapshot(_secId1, out var obSnap1).AssertTrue();
		obSnap1.AssertNotNull();
		obSnap1.State.AssertEqual(QuoteChangeStates.SnapshotComplete);
	}

	[TestMethod]
	public void OrderBook_TryGetSnapshot_NoSnapshot_ReturnsNull()
	{
		var holder = new OrderBookSnapshotHolder();
		holder.TryGetSnapshot(_secId1, out var obSnapNull).AssertFalse();
		obSnapNull.AssertNull();
	}

	[TestMethod]
	public void OrderBook_Process_NullMessage_ThrowsException()
	{
		var holder = new OrderBookSnapshotHolder();
		Assert.ThrowsExactly<ArgumentNullException>(() => holder.Process(null, needResponse: true));
	}

	[TestMethod]
	public void OrderBook_ResetSnapshot_SpecificSecurity_RemovesSnapshot()
	{
		var holder = new OrderBookSnapshotHolder();

		var msg1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};
		var msg2 = new QuoteChangeMessage
		{
			SecurityId = _secId2,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(200m, 10)],
			Asks = [],
		};

		holder.Process(msg1, needResponse: false);
		holder.Process(msg2, needResponse: false);

		holder.ResetSnapshot(_secId1);

		holder.TryGetSnapshot(_secId1, out var obSnap2).AssertFalse();
		obSnap2.AssertNull();
		holder.TryGetSnapshot(_secId2, out var obSnap3).AssertTrue();
		obSnap3.AssertNotNull();
	}

	[TestMethod]
	public void OrderBook_ResetSnapshot_Default_ClearsAll()
	{
		var holder = new OrderBookSnapshotHolder();

		var msg1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};
		var msg2 = new QuoteChangeMessage
		{
			SecurityId = _secId2,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(200m, 10)],
			Asks = [],
		};

		holder.Process(msg1, needResponse: false);
		holder.Process(msg2, needResponse: false);

		holder.ResetSnapshot(default);

		holder.TryGetSnapshot(_secId1, out var obSnap4).AssertFalse();
		obSnap4.AssertNull();
		holder.TryGetSnapshot(_secId2, out var obSnap5).AssertFalse();
		obSnap5.AssertNull();
	}

	[TestMethod]
	public void OrderBook_Process_Increment_AppliesChange()
	{
		var holder = new OrderBookSnapshotHolder();

		// First: full snapshot
		var snapshot = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10), new QuoteChange(99m, 5)],
			Asks = [new QuoteChange(101m, 20)],
		};

		holder.Process(snapshot, needResponse: false);

		// Second: incremental update
		var increment = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			Bids = [new QuoteChange(100m, 15)], // update existing level
			Asks = [],
		};

		var result = holder.Process(increment, needResponse: true);

		result.AssertNotNull();
		result.AssertEqual(increment); // returns original increment
	}

	[TestMethod]
	public void OrderBook_Process_InvalidIncrement_BuildsSnapshot()
	{
        // OrderBookIncrementBuilder is expected to accumulate increments
        // and build a snapshot once it has a complete set of data.
		var holder = new OrderBookSnapshotHolder();

        // Start with an increment (without a prior full snapshot)
		var increment1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = QuoteChangeStates.Increment,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};

        // The first increment without a snapshot may return null (waiting for a full snapshot).
		var result1 = holder.Process(increment1, needResponse: true);

        // Behavior depends on OrderBookIncrementBuilder. No exceptions should occur.
	}

	[TestMethod]
	public void OrderBook_Process_ErrorHandling_DisablesAfterMaxErrors()
	{
        // After reaching a maximum number of errors (e.g., 100), processing
        // should be disabled and return null to avoid log spam.
        // Hard to implement without mocking OrderBookIncrementBuilder.
        // TODO: Refactor for testability (inject OrderBookIncrementBuilder).
	}

	[TestMethod]
	public void OrderBook_Process_FullSnapshot_ResetsErrorCount()
	{
        // Receiving a new full snapshot should reset the error counter.
        // Important for recovering from prior increment errors.
        // TODO: Provide a way to simulate errors to fully test this behavior.
	}

	[TestMethod]
	public void Level1_Process_NoChanges_ReturnsEmptyDiff()
	{
        // If the new message contains the same values, the diff should be empty.
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m);

		holder.Process(msg1, needResponse: false);

        // Send the same value
		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m); // same value

		var diff = holder.Process(msg2, needResponse: true);

		diff.AssertNotNull();
        diff.Changes.Count.AssertEqual(0); // No changes

        // Timestamps should still update
		holder.TryGetSnapshot(_secId1, out var snapshot3).AssertTrue();
		snapshot3.ServerTime.AssertEqual(_now.AddSeconds(1));
	}

	[TestMethod]
	public void OrderBook_FullSnapshot_ShouldReinitializeBuilder()
	{
		var holder = new OrderBookSnapshotHolder();

		// Initial full snapshot with 1 level
		var full1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};
		holder.Process(full1, needResponse: false);

		// New full snapshot that clears the book (expected to reset builder state)
		var full2 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = null,
			Bids = [],
			Asks = [],
		};
		holder.Process(full2, needResponse: false);

		// Increment that assumes empty book and adds new best bid at position 0
		var inc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(2),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(101m, 5) { Action = QuoteChangeActions.New, StartPosition = 0 } ],
			Asks = [],
		};

		var res = holder.Process(inc, needResponse: true);
		res.AssertNotNull();
		res.AssertEqual(inc);

		holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();
		snap.AssertNotNull();

		// EXPECTED: builder was reinitialized by full2, so only one bid level should remain (101@5)
		snap.Bids.Length.AssertEqual(1);
		snap.Bids[0].Price.AssertEqual(101m);
		snap.Bids[0].Volume.AssertEqual(5m);
	}

	// NEW: expect healing after reaching max errors via full snapshot (will fail on current implementation)
	[TestMethod]
	public void OrderBook_ShouldHealAfterMaxErrors_WhenFullSnapshotArrives()
	{
		var holder = new OrderBookSnapshotHolder();

		// Start with an empty full snapshot to initialize state
		var start = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [],
			Asks = [],
		};
		holder.Process(start, needResponse: false);

		// Force errors by sending invalid increments (position out of range)
		var invalidInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 } ], // invalid for empty book
			Asks = [],
		};

		for (var i = 0; i < 100; i++)
		{
			try { holder.Process(invalidInc, needResponse: true); }
			catch { /* ignore expected exceptions to reach max error count */ }
		}

		// Send a valid full snapshot expected to recover processing
		var recovery = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(2),
			State = null,
			Bids = [ new QuoteChange(101m, 1) ],
			Asks = [],
		};

		var recRes = holder.Process(recovery, needResponse: true);
		// Recovery snapshot should be accepted and reset error state.

		// After recovery, a valid increment should be processed normally
		var validInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(3),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(101m, 2) { Action = QuoteChangeActions.Update, StartPosition = 0 } ],
			Asks = [],
		};

		var res2 = holder.Process(validInc, needResponse: true);
		res2.AssertNotNull();

		holder.TryGetSnapshot(_secId1, out var snap2).AssertTrue();
		snap2.AssertNotNull();
		snap2.Bids.Length.AssertEqual(1);
		snap2.Bids[0].Price.AssertEqual(101m);
		snap2.Bids[0].Volume.AssertEqual(2m);
	}

	#endregion
}
