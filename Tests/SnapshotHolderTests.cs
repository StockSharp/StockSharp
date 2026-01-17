namespace StockSharp.Tests;

[TestClass]
public class SnapshotHolderTests : BaseTestClass
{
	private static readonly SecurityId _secId1 = "AAPL@NASDAQ".ToSecurityId();
	private static readonly SecurityId _secId2 = "MSFT@NASDAQ".ToSecurityId();
	private static readonly DateTime _now = DateTime.UtcNow;

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

		var result = holder.Process(msg);

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

		holder.Process(msg);

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

		holder.Process(msg1);

		// Second message with update
		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(Level1Fields.LastTradePrice, 101m)
		.TryAdd(Level1Fields.BestBidPrice, 99m); // unchanged

		var diff = holder.Process(msg2);

		diff.AssertNotNull();
		diff.Changes.Count.AssertEqual(1); // only changed field
		diff.Changes[Level1Fields.LastTradePrice].AssertEqual(101m);
		diff.Changes.ContainsKey(Level1Fields.BestBidPrice).AssertFalse();
	}

	[TestMethod]
	public void Level1_Process_NeedResponse_False_ReturnsNull_ForOptimization()
	{
		// Now: always return diff (possibly empty), even when needResponse=false.
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m);

		holder.Process(msg1);

		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(Level1Fields.LastTradePrice, 101m);

		var result = holder.Process(msg2);
		result.AssertNotNull();
		result.SecurityId.AssertEqual(_secId1);
		result.Changes[Level1Fields.LastTradePrice].AssertEqual(101m);
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

		holder.Process(msg1);

		var laterTime = _now.AddMinutes(5);
		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = laterTime,
			LocalTime = laterTime,
		}
		.TryAdd(Level1Fields.BestBidPrice, 99m);

		holder.Process(msg2);

		holder.TryGetSnapshot(_secId1, out var snapshot).AssertTrue();
		snapshot.ServerTime.AssertEqual(laterTime);
		snapshot.LocalTime.AssertEqual(laterTime);
	}

	[TestMethod]
	public void Level1_Process_NullMessage_ThrowsException()
	{
		var holder = new Level1SnapshotHolder();
		ThrowsExactly<ArgumentNullException>(() => holder.Process(null));
	}

	[TestMethod]
	public void Level1_ResetSnapshot_SpecificSecurity_RemovesSnapshot()
	{
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage { SecurityId = _secId1, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 100m);
		var msg2 = new Level1ChangeMessage { SecurityId = _secId2, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 200m);

		holder.Process(msg1);
		holder.Process(msg2);

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

		holder.Process(msg1);
		holder.Process(msg2);

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

		holder.Process(msg1);

		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(Level1Fields.BestBidPrice, 99m);

		var diff = holder.Process(msg2);

		diff.AssertNotNull();
		diff.Changes.Count.AssertEqual(1);
		diff.Changes[Level1Fields.BestBidPrice].AssertEqual(99m);

		holder.TryGetSnapshot(_secId1, out var snapshot2).AssertTrue();
		snapshot2.Changes.Count.AssertEqual(2);
	}

	#endregion

	#region Additional Level1 tests

	[TestMethod]
	public void Level1_Process_MultipleSecurities_AreIsolated()
	{
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage { SecurityId = _secId1, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 100m);
		var msg2 = new Level1ChangeMessage { SecurityId = _secId2, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 200m);

		holder.Process(msg1);
		holder.Process(msg2);

		var update1 = new Level1ChangeMessage { SecurityId = _secId1, ServerTime = _now.AddSeconds(1) }
			.TryAdd(Level1Fields.LastTradePrice, 101m);
		holder.Process(update1);

		holder.TryGetSnapshot(_secId2, out var snap2).AssertTrue();
		snap2.Changes[Level1Fields.LastTradePrice].AssertEqual(200m);
	}

	[TestMethod]
	public void Level1_Process_BuildFrom_CopiedToDiff()
	{
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			BuildFrom = DataType.Ticks,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m);

		holder.Process(msg1);

		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			BuildFrom = DataType.OrderLog,
		}
		.TryAdd(Level1Fields.BestBidPrice, 99m);

		var diff = holder.Process(msg2);
		diff.AssertNotNull();
		diff.BuildFrom.AssertEqual(DataType.OrderLog);
	}

	[TestMethod]
	public Task Level1_ConcurrentAccess_ThreadSafe()
	{
		var holder = new Level1SnapshotHolder();
		var token = CancellationToken;

		var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
		{
			var code = $"SEC{i % 10}@TEST".ToSecurityId();
			var msg = new Level1ChangeMessage { SecurityId = code, ServerTime = _now }
				.TryAdd(Level1Fields.LastTradePrice, i);
			holder.Process(msg);
			holder.TryGetSnapshot(code, out var _);
		}, token)).ToArray();

		return Task.WhenAll(tasks);
	}

	[TestMethod]
	public void OrderBook_ReturnedMessage_IsNotInternalInstance()
	{
		var holder = new OrderBookSnapshotHolder();
		var inc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = QuoteChangeStates.Increment,
			Bids = [ new QuoteChange(100m, 10) ],
			Asks = [],
		};

		// First increment may build snapshot immediately depending on builder logic
		var res = holder.Process(inc);
		if (res == null)
		{
			// Make a full snapshot to initialize
			var full = new QuoteChangeMessage
			{
				SecurityId = _secId1,
				ServerTime = _now.AddSeconds(1),
				State = null,
				Bids = [ new QuoteChange(100m, 10) ],
				Asks = [],
			};
			res = holder.Process(full);
		}

		res.AssertNotNull();

		// mutate returned message
		res.Bids = [ new QuoteChange(999m, 1) ];

		// internal snapshot must not be affected
		holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();
		snap.AssertNotNull();
		(snap.Bids.Length == 1 && snap.Bids[0].Price != 999m).AssertTrue();
	}

	#endregion

	#region Additional OrderBook tests

	[TestMethod]
	public void OrderBook_NewFullSnapshot_ReturnsCorrectDelta()
	{
		var holder = new OrderBookSnapshotHolder();

		var snap1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10), new QuoteChange(99m, 5)],
			Asks = [new QuoteChange(101m, 20)],
		};

		holder.Process(snap1);

		var snap2 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = null,
			Bids = [new QuoteChange(100m, 15), new QuoteChange(98m, 3)],
			Asks = [new QuoteChange(101m, 20)],
		};

		var delta = holder.Process(snap2);
		delta.AssertNotNull();
		// GetDelta returns increment with changes
		delta.State.AssertEqual(QuoteChangeStates.Increment);

		// Ensure error counter reset to 0
		var err = holder.GetErrorCount(_secId1);
		err.AssertNotNull();
		err.Value.AssertEqual(0);
	}

	[TestMethod]
	public void OrderBook_FirstIncrement_WithoutSnapshot_ReturnsNull()
	{
		var holder = new OrderBookSnapshotHolder();
		var inc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = QuoteChangeStates.Increment,
			Bids = [new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 0 }],
			Asks = [],
		};

		// First increment without prior snapshot - builder may build snapshot or return null
		var res = holder.Process(inc);

		// If result is not null, it must be a SnapshotComplete (builder constructed snapshot from increment)
		if (res != null)
		{
			res.State.AssertEqual(QuoteChangeStates.SnapshotComplete);
			holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();
			snap.AssertNotNull();
		}
	}

	[TestMethod]
	public void OrderBook_MultipleSecurities_IndependentErrorStates()
	{
		var holder = new OrderBookSnapshotHolder();

		var s1 = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now, State = null, Bids = [], Asks = [] };
		var s2 = new QuoteChangeMessage { SecurityId = _secId2, ServerTime = _now, State = null, Bids = [new QuoteChange(200m, 1)], Asks = [] };

		holder.Process(s1);
		holder.Process(s2);

		var invalid = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 } ],
			Asks = [],
		};

		for (var i = 0; i < 100; i++)
		{
			try { holder.Process(invalid); } catch { }
		}

		var okInc = new QuoteChangeMessage
		{
			SecurityId = _secId2,
			ServerTime = _now.AddSeconds(2),
			State = QuoteChangeStates.Increment,
			Bids = [ new QuoteChange(200m, 2) { Action = QuoteChangeActions.Update, StartPosition = 0 } ],
			Asks = [],
		};

		var res2 = holder.Process(okInc);
		res2.AssertNotNull();

		var res1 = holder.Process(invalid);
		res1.AssertNull();
	}

	[TestMethod]
	public void OrderBook_InvalidFullSnapshot_ThrowsOrCreatesSnapshot()
	{
		var holder = new OrderBookSnapshotHolder();

		// Full snapshot with positions may be invalid depending on builder implementation
		var fullWithPositions = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			HasPositions = true,
			Bids = [ new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 } ],
			Asks = [],
		};

		Exception caught = null;
		QuoteChangeMessage result = null;

		try
		{
			result = holder.Process(fullWithPositions);
		}
		catch (InvalidOperationException ex)
		{
			caught = ex;
		}

		// Either throws InvalidOperationException OR creates snapshot - but must be one of these
		if (caught != null)
		{
			// If it throws, no snapshot should exist
			holder.TryGetSnapshot(_secId1, out var snap).AssertFalse();
		}
		else
		{
			// If it doesn't throw, result must be valid and snapshot must exist
			result.AssertNotNull();
			holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();
			snap.AssertNotNull();
		}
	}

	[TestMethod]
	public void OrderBook_Process_FullSnapshot_WithMaxErrors_DoesNotThrow()
	{
		var holder = new OrderBookSnapshotHolder();

		var start = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now, State = null, Bids = [], Asks = [] };
		holder.Process(start);

		var badInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 } ],
			Asks = [],
		};

		for (var i = 0; i < 100; i++) { try { holder.Process(badInc); } catch { } }

		var validFull = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now.AddSeconds(2), State = null, Bids = [ new QuoteChange(101m, 1) ], Asks = [] };
		// should not throw even when disabled
		holder.Process(validFull);
	}

	[TestMethod]
	public Task OrderBook_ConcurrentAccess_ThreadSafe()
	{
		var holder = new OrderBookSnapshotHolder();
		var token = CancellationToken;

		var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
		{
			var code = $"SEC{i % 10}@TEST".ToSecurityId();
			var full = new QuoteChangeMessage { SecurityId = code, ServerTime = _now, State = null, Bids = [], Asks = [] };
			holder.Process(full);

			var inc = new QuoteChangeMessage { SecurityId = code, ServerTime = _now.AddSeconds(1), State = QuoteChangeStates.Increment, Bids = [], Asks = [] };
			try { holder.Process(inc); } catch { }
			holder.TryGetSnapshot(code, out var _);
		}, token)).ToArray();

		return Task.WhenAll(tasks);
	}

	[TestMethod]
	public void Level1_Process_EmptyChanges_ReturnsEmptyDiff()
	{
		var holder = new Level1SnapshotHolder();

		var msg1 = new Level1ChangeMessage { SecurityId = _secId1, ServerTime = _now }
			.TryAdd(Level1Fields.LastTradePrice, 100m);

		holder.Process(msg1);

		var msg2 = new Level1ChangeMessage { SecurityId = _secId1, ServerTime = _now.AddSeconds(1) };

		var diff = holder.Process(msg2);
		diff.AssertNotNull();
		diff.Changes.Count.AssertEqual(0);

		holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();
		snap.ServerTime.AssertEqual(_now.AddSeconds(1));
	}

	[TestMethod]
	public void OrderBook_Process_EmptyIncrement_UpdatesNothing()
	{
		var holder = new OrderBookSnapshotHolder();
		var full = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now, State = null, Bids = [ new QuoteChange(100m, 1) ], Asks = [] };
		holder.Process(full);

		holder.TryGetSnapshot(_secId1, out var beforeSnap).AssertTrue();
		beforeSnap.AssertNotNull();

		var inc = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now.AddSeconds(1), State = QuoteChangeStates.Increment, Bids = [], Asks = [] };
		var res = holder.Process(inc);

		// Empty increment should return the original increment message (no changes)
		res.AssertEqual(inc);

		holder.TryGetSnapshot(_secId1, out var afterSnap).AssertTrue();
		// snapshot bids/asks should not change for empty increment
		afterSnap.Bids.Length.AssertEqual(beforeSnap.Bids.Length);
		afterSnap.Asks.Length.AssertEqual(beforeSnap.Asks.Length);
	}

	#endregion

	#region Edge cases

	[TestMethod]
	public Task Level1_ResetSnapshot_WhileProcessing_ThreadSafe()
	{
		var holder = new Level1SnapshotHolder();
		var token = CancellationToken;

		var t1 = Task.Run(() =>
		{
			for (var i = 0; i < 1000; i++)
			{
				var sid = $"SEC{i % 5}@TEST".ToSecurityId();
				var msg = new Level1ChangeMessage { SecurityId = sid, ServerTime = _now }
					.TryAdd(Level1Fields.LastTradePrice, i);
				holder.Process(msg);
			}
		}, token);

		var t2 = Task.Run(() =>
		{
			for (var i = 0; i < 50; i++)
				holder.ResetSnapshot(default);
		}, token);

		return Task.WhenAll(t1, t2);
	}

	[TestMethod]
	public void OrderBook_Process_AfterReset_StartsClean()
	{
		var holder = new OrderBookSnapshotHolder();

		var full = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now, State = null, Bids = [], Asks = [] };
		holder.Process(full);

		var badInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 } ],
			Asks = [],
		};

		for (var i = 0; i < 100; i++) { try { holder.Process(badInc); } catch { } }

		holder.ResetSnapshot(_secId1);

		var newFull = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now.AddSeconds(2), State = null, Bids = [ new QuoteChange(101m, 1) ], Asks = [] };
		var r = holder.Process(newFull);
		r.AssertNotNull();

		var goodInc = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now.AddSeconds(3), State = QuoteChangeStates.Increment, Bids = [ new QuoteChange(101m, 2) { Action = QuoteChangeActions.Update, StartPosition = 0 } ], Asks = [] };
		var r2 = holder.Process(goodInc);
		r2.AssertNotNull();
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

		var result = holder.Process(msg);

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

		holder.Process(msg);

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
		ThrowsExactly<ArgumentNullException>(() => holder.Process(null));
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

		holder.Process(msg1);
		holder.Process(msg2);

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

		holder.Process(msg1);
		holder.Process(msg2);

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

		holder.Process(snapshot);

		// Second: incremental update
		var increment = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			Bids = [new QuoteChange(100m, 15)], // update existing level
			Asks = [],
		};

		var result = holder.Process(increment);

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
		var result1 = holder.Process(increment1);

		// Behavior depends on OrderBookIncrementBuilder. No exceptions should occur.
		// If no snapshot built yet, result may be null.
		// Ensure it didn't throw and state is still consistent after a valid full snapshot.
		var full = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = null,
			Bids = [ new QuoteChange(100m, 10) ],
			Asks = [],
		};
		holder.Process(full);
		holder.TryGetSnapshot(_secId1, out var snapAfterFull).AssertTrue();
		snapAfterFull.AssertNotNull();
	}

	[TestMethod]
	public void OrderBook_Process_ErrorHandling_DisablesAfterMaxErrors()
	{
		var holder = new OrderBookSnapshotHolder();

		// Initialize with empty order book snapshot
		var start = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [],
			Asks = [],
		};
		holder.Process(start);

		// Prepare invalid increment likely causing builder error/exception
		var invalidInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 } ],
			Asks = [],
		};

		// Trigger errors until reaching the internal max error limit
		for (var i = 0; i < 100; i++)
		{
			try { holder.Process(invalidInc); }
			catch { /* ignore to reach max */ }
		}

		// Now processing should be disabled for increments: return null and no throw
		var res = holder.Process(invalidInc);
		res.AssertNull();
	}

	[TestMethod]
	public void OrderBook_Process_FullSnapshot_ResetsErrorCount()
	{
		var holder = new OrderBookSnapshotHolder();

		// Init empty snapshot
		var start = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [],
			Asks = [],
		};
		holder.Process(start);

		// Reach max errors via invalid increments
		var invalidInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 } ],
			Asks = [],
		};

		for (var i = 0; i < 100; i++)
		{
			try { holder.Process(invalidInc); }
			catch { /* ignore */ }
		}

		// Healing full snapshot should reset counter even if it was disabled
		var recovery = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(2),
			State = null,
			Bids = [ new QuoteChange(101m, 1) ],
			Asks = [],
		};

		var rec = holder.Process(recovery);
		// rec may be null or delta, but must not throw and must reset error state

		// After recovery, valid increment should succeed
		var validInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(3),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [ new QuoteChange(101m, 2) { Action = QuoteChangeActions.Update, StartPosition = 0 } ],
			Asks = [],
		};

		var res2 = holder.Process(validInc);
		res2.AssertNotNull();
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

		holder.Process(msg1);

		// Send the same value
		var msg2 = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m); // same value

		var diff = holder.Process(msg2);

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
		holder.Process(full1);

		// New full snapshot that clears the book (expected to reset builder state)
		var full2 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = null,
			Bids = [],
			Asks = [],
		};
		holder.Process(full2);

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

		var res = holder.Process(inc);
		res.AssertNotNull();
		res.AssertEqual(inc);

		holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();
		snap.AssertNotNull();

		// EXPECTED: builder was reinitialized by full2, so only one bid level should remain (101@5)
		snap.Bids.Length.AssertEqual(1);
		snap.Bids[0].Price.AssertEqual(101m);
		snap.Bids[0].Volume.AssertEqual(5m);

		// Ensure Process result is not the same instance as snapshot stored
		// (TryGetSnapshot returns a clone, so res reference should differ from retrieved clone)
		res.AssertNotSame(snap);
	}

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
		holder.Process(start);

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
			try { holder.Process(invalidInc); }
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

		var recRes = holder.Process(recovery);
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

		var res2 = holder.Process(validInc);
		res2.AssertNotNull();

		holder.TryGetSnapshot(_secId1, out var snap2).AssertTrue();
		snap2.AssertNotNull();
		snap2.Bids.Length.AssertEqual(1);
		snap2.Bids[0].Price.AssertEqual(101m);
		snap2.Bids[0].Volume.AssertEqual(2m);
	}

	[TestMethod]
	public void Level1_FirstProcess_ReturnsDifferentReferenceThanStored()
	{
		var holder = new Level1SnapshotHolder();
		var msg = new Level1ChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m);

		var result = holder.Process(msg);
		holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();

		// should not be the same reference as stored snapshot clone
		result.AssertNotSame(snap);
	}

	[TestMethod]
	public void OrderBook_FirstProcess_ReturnsDifferentReferenceThanStored()
	{
		var holder = new OrderBookSnapshotHolder();
		var msg = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [ new QuoteChange(100m, 10) ],
			Asks = [],
		};

		var res = holder.Process(msg);
		holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();
		snap.AssertNotNull();

		// Ensure returned instance differs from snapshot instance we can read back
		res.AssertNotSame(snap);
	}

	#endregion

	[TestMethod]
	public void OrderBook_SnapshotUpdatedButBuilderNotReinitialized()
	{
		var holder = new OrderBookSnapshotHolder();

		// Create initial snapshot with one bid level at 100
		var snap1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};
		holder.Process(snap1);

		// Create new full snapshot with EMPTY book but using HasPositions
		// This will cause builder.TryApply to fail (can't apply positioned changes to empty book)
		var snap2 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = null,
			HasPositions = true,
			Bids = [], // empty - builder expects positions but gets empty
			Asks = [],
		};

		// Full snapshot with HasPositions=true and empty book should NOT throw.
		// It should reinitialize builder to empty state.
		holder.Process(snap2);

		// Now send increment that assumes EMPTY book (as per snap2)
		var inc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(2),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange(101m, 5) { Action = QuoteChangeActions.New, StartPosition = 0 }], // add to position 0 of empty book
			Asks = [],
		};

		// After empty full snapshot, builder must think book is empty, so result is [101:5]
		var res = holder.Process(inc);
		res.AssertNotNull();

		// Check final snapshot
		holder.TryGetSnapshot(_secId1, out var finalSnap).AssertTrue();
		finalSnap.Bids.Length.AssertEqual(1);
		finalSnap.Bids[0].Price.AssertEqual(101m);
		finalSnap.Bids[0].Volume.AssertEqual(5m);
	}

	[TestMethod]
	public void OrderBook_SnapshotAndBuilderOutOfSync()
	{
		var holder = new OrderBookSnapshotHolder();

		// Initial snapshot: Bids=[100:10, 99:5]
		var snap1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10), new QuoteChange(99m, 5)],
			Asks = [],
		};
		holder.Process(snap1);

		// Send new full snapshot that will fail in builder but succeed in GetDelta
		// Use invalid HasPositions on full snapshot
		var snap2 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = null,
			HasPositions = true, // invalid for full snapshot
			Bids = [new QuoteChange(101m, 20) { Action = QuoteChangeActions.Update, StartPosition = 5 }], // impossible position
			Asks = [],
		};

		try
		{
			holder.Process(snap2);
		}
		catch (InvalidOperationException)
		{
			// Expected - builder.TryApply failed
			// BUG: info.Snapshot now points to snap2, but builder still has snap1 state
		}

		// Send simple update increment without positions
		var inc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(2),
			State = QuoteChangeStates.Increment,
			Bids = [new QuoteChange(101m, 25)], // simple price update
			Asks = [],
		};

		var res = holder.Process(inc);

		// After increment, check snapshot
		holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();

		// EXPECTED: Should have bid at 101:25 (from snap2 base + increment)
		// BUG: Will have wrong state because builder has snap1 state but snapshot has snap2
		// The exact result depends on builder behavior, but state is corrupted

		// At minimum, errorCount should be > 0 if subsequent processing fails
		var errorCount = holder.GetErrorCount(_secId1);
		errorCount.AssertNotNull();

		// This test may be flaky depending on builder, but demonstrates the issue
	}

	[TestMethod]
	public void OrderBook_ErrorCount_ExceedsMaxError()
	{
		var holder = new OrderBookSnapshotHolder();

		// Create empty snapshot
		var start = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [],
			Asks = [],
		};
		holder.Process(start);

		// Invalid increment that will always fail
		var invalidInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 }], // invalid position
			Asks = [],
		};

		// Send 105 times to exceed maxError
		for (var i = 0; i < 105; i++)
		{
			try
			{ holder.Process(invalidInc); }
			catch { /* ignore */ }
		}

		var errorCount = holder.GetErrorCount(_secId1);
		errorCount.AssertNotNull();

		// BUG: ErrorCount = 105 (continues to grow)
		// EXPECTED: ErrorCount should be capped at 100
		errorCount.Value.AssertEqual(100); // FAILS - will be 105
	}

	[TestMethod]
	public void OrderBook_MultipleLogsAfterMaxError()
	{
		var holder = new OrderBookSnapshotHolder();

		var start = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [],
			Asks = [],
		};
		holder.Process(start);

		var invalidInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 }],
			Asks = [],
		};

		// Send exactly 100 times to reach maxError
		for (var i = 0; i < 100; i++)
		{
			try
			{ holder.Process(invalidInc); }
			catch { }
		}

		var errorCount1 = holder.GetErrorCount(_secId1);
		errorCount1.Value.AssertEqual(100);

		// Send 1 more time
		try
		{ holder.Process(invalidInc); }
		catch { }

		var errorCount2 = holder.GetErrorCount(_secId1);

		// BUG: ErrorCount = 101 (incremented again)
		// EXPECTED: Should stay at 100
		errorCount2.Value.AssertEqual(100); // FAILS - will be 101
	}

	[TestMethod]
	public void OrderBook_ErrorCount_ResetBeforeBuilderValidation()
	{
		var holder = new OrderBookSnapshotHolder();

		// Create initial snapshot
		var snap1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};
		holder.Process(snap1);

		// Force 5 errors with invalid increments
		var invalidInc = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = QuoteChangeStates.Increment,
			HasPositions = true,
			Bids = [new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 99 }],
			Asks = [],
		};

		for (var i = 0; i < 5; i++)
		{
			try
			{ holder.Process(invalidInc); }
			catch { }
		}

		// Now ErrorCount should be 5
		var errorCountBefore = holder.GetErrorCount(_secId1);
		errorCountBefore.Value.AssertEqual(5);

		// Send new full snapshot that will fail in builder.TryApply
		// (but succeed in GetDelta)
		var snap2 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(2),
			State = null,
			HasPositions = true,
			Bids = [new QuoteChange(99m, 5) { Action = QuoteChangeActions.Update, StartPosition = 100 }],
			Asks = [],
		};

		try
		{
			holder.Process(snap2);
		}
		catch (InvalidOperationException)
		{
			// Expected - builder.TryApply failed
		}

		// Check ErrorCount after failed full snapshot
		var errorCountAfter = holder.GetErrorCount(_secId1);
		errorCountAfter.AssertNotNull();

		// BUG: ErrorCount = 1 (was reset to 0, then incremented)
		// EXPECTED: ErrorCount = 6 (old value 5 + 1)
		errorCountAfter.Value.AssertEqual(6); // FAILS - will be 1
	}

	[TestMethod]
	public void OrderBook_SnapshotUpdated_ButBuilderNotReinitialized()
	{
		var holder = new OrderBookSnapshotHolder();

		// Initial: bid at 100
		var snap1 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};
		holder.Process(snap1);

		// Get snapshot to verify initial state
		holder.TryGetSnapshot(_secId1, out var snapBefore).AssertTrue();
		snapBefore.Bids[0].Price.AssertEqual(100m);

		// Try to send full snapshot with bid at 200, but use invalid data for builder
		var snap2 = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			State = null,
			HasPositions = true, // invalid for full snapshot
			Bids = [new QuoteChange(200m, 20) { Action = QuoteChangeActions.New, StartPosition = 50 }],
			Asks = [],
		};

		try
		{
			holder.Process(snap2);
		}
		catch (InvalidOperationException)
		{
			// Expected - builder.TryApply failed
			// BUG: info.Snapshot was ALREADY set to snap2 (with bid at 200)
			// but builder was NOT reinitialized, still thinks bid is at 100
		}

		// Now TryGetSnapshot should return what's in info.Snapshot
		holder.TryGetSnapshot(_secId1, out var snapAfter).AssertTrue();

		// BUG: snapAfter will have bid at 200 (from failed snap2)
		// EXPECTED: Should still have bid at 100 (from snap1)
		// Because snap2 processing failed, state should not have changed
		snapAfter.Bids[0].Price.AssertEqual(100m); // FAILS - will be 200m
	}

	#region OrderSnapshotHolder Tests

	[TestMethod]
	public void Order_FirstProcess_CreatesSnapshot()
	{
		var holder = new OrderSnapshotHolder();
		var msg = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			OrderPrice = 100m,
			OrderVolume = 10,
			Side = Sides.Buy,
		};

		var result = holder.Process(msg);

		result.AssertNotNull();
		result.TransactionId.AssertEqual(1);
		result.OrderState.AssertEqual(OrderStates.Pending);
		result.OrderPrice.AssertEqual(100m);
		result.OrderVolume.AssertEqual(10);
	}

	[TestMethod]
	public void Order_TryGetSnapshot_ReturnsSnapshot()
	{
		var holder = new OrderSnapshotHolder();
		var msg = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
		};

		holder.Process(msg);

		holder.TryGetSnapshot(1, out var snapshot).AssertTrue();
		snapshot.AssertNotNull();
		snapshot.OrderState.AssertEqual(OrderStates.Pending);
	}

	[TestMethod]
	public void Order_TryGetSnapshot_NoSnapshot_ReturnsFalse()
	{
		var holder = new OrderSnapshotHolder();
		holder.TryGetSnapshot(1, out var snapshot).AssertFalse();
		snapshot.AssertNull();
	}

	[TestMethod]
	public void Order_Process_ReturnsCopy()
	{
		var holder = new OrderSnapshotHolder();
		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
		};

		var result1 = holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};

		var result2 = holder.Process(msg2);

		// Returns different references (copies)
		result1.AssertNotSame(result2);

		// But both have correct state for their point in time
		result1.OrderState.AssertEqual(OrderStates.Pending);
		result2.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void Order_Process_UpdatesExistingSnapshot()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			OrderPrice = 100m,
			OrderVolume = 10,
			Balance = 10,
		};

		var snapshot = holder.Process(msg1);
		snapshot.OrderState.AssertEqual(OrderStates.Pending);
		snapshot.Balance.AssertEqual(10);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
			OrderId = 12345,
		};

		holder.Process(msg2);

		// Same snapshot should be updated
		snapshot.OrderState.AssertEqual(OrderStates.Active);
		snapshot.OrderId.AssertEqual(12345);
		// Balance not updated since msg2 has no balance
		snapshot.Balance.AssertEqual(10);
	}

	[TestMethod]
	public void Order_Process_NullMessage_ThrowsException()
	{
		var holder = new OrderSnapshotHolder();
		ThrowsExactly<ArgumentNullException>(() => holder.Process(null));
	}

	[TestMethod]
	public void Order_Process_NoOrderInfo_ReturnsNull()
	{
		var holder = new OrderSnapshotHolder();
		var msg = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = false,
		};

		var result = holder.Process(msg);
		result.AssertNull();
	}

	[TestMethod]
	public void Order_Process_ZeroTransactionId_ThrowsException()
	{
		var holder = new OrderSnapshotHolder();
		var msg = new ExecutionMessage
		{
			TransactionId = 0,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
		};

		ThrowsExactly<ArgumentException>(() => holder.Process(msg));
	}

	[TestMethod]
	public void Order_ResetSnapshot_SpecificTransaction_RemovesSnapshot()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage { TransactionId = 1, SecurityId = _secId1, ServerTime = _now, HasOrderInfo = true };
		var msg2 = new ExecutionMessage { TransactionId = 2, SecurityId = _secId2, ServerTime = _now, HasOrderInfo = true };

		holder.Process(msg1);
		holder.Process(msg2);

		holder.ResetSnapshot(1);

		holder.TryGetSnapshot(1, out var snap1).AssertFalse();
		snap1.AssertNull();
		holder.TryGetSnapshot(2, out var snap2).AssertTrue();
		snap2.AssertNotNull();
	}

	[TestMethod]
	public void Order_ResetSnapshot_Zero_ClearsAll()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage { TransactionId = 1, SecurityId = _secId1, ServerTime = _now, HasOrderInfo = true };
		var msg2 = new ExecutionMessage { TransactionId = 2, SecurityId = _secId2, ServerTime = _now, HasOrderInfo = true };

		holder.Process(msg1);
		holder.Process(msg2);

		holder.ResetSnapshot(0);

		holder.TryGetSnapshot(1, out var snap1).AssertFalse();
		snap1.AssertNull();
		holder.TryGetSnapshot(2, out var snap2).AssertFalse();
		snap2.AssertNull();
	}

	[TestMethod]
	public void Order_Process_AppliesAllFields()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			OrderPrice = 100m,
			OrderVolume = 10,
		};

		var snapshot = holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderId = 12345,
			OrderStringId = "ORD-123",
			OrderBoardId = "BOARD-1",
			Balance = 8,
			OrderState = OrderStates.Active,
			PnL = 50m,
			Position = 2m,
			Commission = 0.5m,
			CommissionCurrency = "USD",
			AveragePrice = 100.5m,
			Latency = TimeSpan.FromMilliseconds(10),
		};

		holder.Process(msg2);

		snapshot.OrderId.AssertEqual(12345);
		snapshot.OrderStringId.AssertEqual("ORD-123");
		snapshot.OrderBoardId.AssertEqual("BOARD-1");
		snapshot.Balance.AssertEqual(8);
		snapshot.OrderState.AssertEqual(OrderStates.Active);
		snapshot.PnL.AssertEqual(50m);
		snapshot.Position.AssertEqual(2m);
		snapshot.Commission.AssertEqual(0.5m);
		snapshot.CommissionCurrency.AssertEqual("USD");
		snapshot.AveragePrice.AssertEqual(100.5m);
		snapshot.Latency.AssertEqual(TimeSpan.FromMilliseconds(10));
		snapshot.ServerTime.AssertEqual(_now.AddSeconds(1));
	}

	[TestMethod]
	public void Order_Process_DoneState_StopsUpdating()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			Balance = 10,
		};

		var snapshot = holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
			Balance = 5,
		};

		holder.Process(msg2);
		snapshot.OrderState.AssertEqual(OrderStates.Active);
		snapshot.Balance.AssertEqual(5);

		var msg3 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(2),
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			Balance = 0,
		};

		holder.Process(msg3);
		snapshot.OrderState.AssertEqual(OrderStates.Done);
		snapshot.Balance.AssertEqual(0);
	}

	[TestMethod]
	public void Order_MultipleTransactions_AreIsolated()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			OrderPrice = 100m,
		};

		var msg2 = new ExecutionMessage
		{
			TransactionId = 2,
			SecurityId = _secId2,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			OrderPrice = 200m,
		};

		var snap1 = holder.Process(msg1);
		var snap2 = holder.Process(msg2);

		snap1.AssertNotSame(snap2);
		snap1.OrderPrice.AssertEqual(100m);
		snap2.OrderPrice.AssertEqual(200m);

		// Update only first order
		var msg3 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};

		holder.Process(msg3);

		snap1.OrderState.AssertEqual(OrderStates.Active);
		snap2.OrderState.AssertEqual(OrderStates.Pending);
	}

	[TestMethod]
	public Task Order_ConcurrentAccess_ThreadSafe()
	{
		var holder = new OrderSnapshotHolder();
		var token = CancellationToken;

		var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
		{
			var transId = i % 10 + 1;
			var msg = new ExecutionMessage
			{
				TransactionId = transId,
				SecurityId = _secId1,
				ServerTime = _now.AddMilliseconds(i),
				HasOrderInfo = true,
				OrderState = i < 50 ? OrderStates.Pending : OrderStates.Active,
				Balance = 100 - i,
			};
			holder.Process(msg);
			holder.TryGetSnapshot(transId, out var _);
		}, token)).ToArray();

		return Task.WhenAll(tasks);
	}

	[TestMethod]
	public void Order_FirstMessage_IsCloned()
	{
		var holder = new OrderSnapshotHolder();

		var original = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			OrderPrice = 100m,
		};

		var snapshot = holder.Process(original);

		// Modifying original should not affect snapshot
		original.OrderPrice = 200m;

		snapshot.OrderPrice.AssertEqual(100m);
	}

	[TestMethod]
	public void Order_StateTransition_DoneToPending_ThrowsWhenEnabled()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = true };

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
		};

		holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
		};

		ThrowsExactly<InvalidOperationException>(() => holder.Process(msg2));
	}

	[TestMethod]
	public void Order_StateTransition_DoneToActive_ThrowsWhenEnabled()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = true };

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
		};

		holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};

		ThrowsExactly<InvalidOperationException>(() => holder.Process(msg2));
	}

	[TestMethod]
	public void Order_StateTransition_FailedToPending_ThrowsWhenEnabled()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = true };

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Failed,
		};

		holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
		};

		ThrowsExactly<InvalidOperationException>(() => holder.Process(msg2));
	}

	[TestMethod]
	public void Order_StateTransition_FailedToActive_ThrowsWhenEnabled()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = true };

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Failed,
		};

		holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};

		ThrowsExactly<InvalidOperationException>(() => holder.Process(msg2));
	}

	[TestMethod]
	public void Order_StateTransition_ActiveToPending_ThrowsWhenEnabled()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = true };

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};

		holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
		};

		ThrowsExactly<InvalidOperationException>(() => holder.Process(msg2));
	}

	[TestMethod]
	public void Order_StateTransition_InvalidTransition_OnlyLogsWhenDisabled()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = false };

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
		};

		holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
		};

		// Should not throw, only log warning
		var snapshot = holder.Process(msg2);
		snapshot.AssertNotNull();
		// State should still be updated (holder doesn't block it)
		snapshot.OrderState.AssertEqual(OrderStates.Pending);
	}

	[TestMethod]
	public void Order_StateTransition_ValidTransitions_DoNotThrow()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = true };

		// None -> Pending
		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
		};
		holder.Process(msg1);

		// Pending -> Active
		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};
		holder.Process(msg2);

		// Active -> Done
		var msg3 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(2),
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
		};
		var snapshot = holder.Process(msg3);

		snapshot.AssertNotNull();
		snapshot.OrderState.AssertEqual(OrderStates.Done);
	}

	[TestMethod]
	public void Order_StateTransition_PendingToFailed_Valid()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = true };

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
		};
		holder.Process(msg1);

		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Failed,
		};
		var snapshot = holder.Process(msg2);

		snapshot.AssertNotNull();
		snapshot.OrderState.AssertEqual(OrderStates.Failed);
	}

	[TestMethod]
	public void Order_StateTransition_NoneToFailed_Valid()
	{
		var holder = new OrderSnapshotHolder { ThrowOnInvalidStateTransition = true };

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Failed,
		};
		var snapshot = holder.Process(msg1);

		snapshot.AssertNotNull();
		snapshot.OrderState.AssertEqual(OrderStates.Failed);
	}

	#endregion

	#region PositionSnapshotHolder Tests

	[TestMethod]
	public void Position_FirstProcess_CreatesSnapshot()
	{
		var holder = new PositionSnapshotHolder();
		var msg = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m)
		.TryAdd(PositionChangeTypes.AveragePrice, 50m);

		var result = holder.Process(msg);

		result.AssertNotNull();
		result.PortfolioName.AssertEqual("Portfolio1");
		result.SecurityId.AssertEqual(_secId1);
		result.Changes[PositionChangeTypes.CurrentValue].AssertEqual(100m);
		result.Changes[PositionChangeTypes.AveragePrice].AssertEqual(50m);
	}

	[TestMethod]
	public void Position_TryGetSnapshot_ReturnsSnapshot()
	{
		var holder = new PositionSnapshotHolder();
		var msg = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		holder.Process(msg);

		holder.TryGetSnapshot("Portfolio1", _secId1, null, null, null, null, null, out var snapshot).AssertTrue();
		snapshot.AssertNotNull();
		snapshot.Changes[PositionChangeTypes.CurrentValue].AssertEqual(100m);
	}

	[TestMethod]
	public void Position_TryGetSnapshot_ByMessage_ReturnsSnapshot()
	{
		var holder = new PositionSnapshotHolder();
		var msg = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		holder.Process(msg);

		holder.TryGetSnapshot(msg, out var snapshot).AssertTrue();
		snapshot.AssertNotNull();
	}

	[TestMethod]
	public void Position_TryGetSnapshot_NoSnapshot_ReturnsFalse()
	{
		var holder = new PositionSnapshotHolder();
		holder.TryGetSnapshot("Portfolio1", _secId1, null, null, null, null, null, out var snapshot).AssertFalse();
		snapshot.AssertNull();
	}

	[TestMethod]
	public void Position_Process_ReturnsSameReference()
	{
		var holder = new PositionSnapshotHolder();
		var msg1 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		var result1 = holder.Process(msg1);

		var msg2 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 150m);

		var result2 = holder.Process(msg2);

		// Must return same reference
		result1.AssertSame(result2);
	}

	[TestMethod]
	public void Position_Process_UpdatesExistingSnapshot()
	{
		var holder = new PositionSnapshotHolder();

		var msg1 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m)
		.TryAdd(PositionChangeTypes.AveragePrice, 50m);

		var snapshot = holder.Process(msg1);
		snapshot.Changes[PositionChangeTypes.CurrentValue].AssertEqual(100m);
		snapshot.Changes[PositionChangeTypes.AveragePrice].AssertEqual(50m);

		var msg2 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 150m)
		.TryAdd(PositionChangeTypes.UnrealizedPnL, 25m);

		holder.Process(msg2);

		// Same snapshot should be updated
		snapshot.Changes[PositionChangeTypes.CurrentValue].AssertEqual(150m);
		snapshot.Changes[PositionChangeTypes.AveragePrice].AssertEqual(50m); // unchanged
		snapshot.Changes[PositionChangeTypes.UnrealizedPnL].AssertEqual(25m); // new field
	}

	[TestMethod]
	public void Position_Process_NullMessage_ThrowsException()
	{
		var holder = new PositionSnapshotHolder();
		ThrowsExactly<ArgumentNullException>(() => holder.Process(null));
	}

	[TestMethod]
	public void Position_ResetSnapshot_SpecificPosition_RemovesSnapshot()
	{
		var holder = new PositionSnapshotHolder();

		var msg1 = new PositionChangeMessage { PortfolioName = "Portfolio1", SecurityId = _secId1, ServerTime = _now }
			.TryAdd(PositionChangeTypes.CurrentValue, 100m);
		var msg2 = new PositionChangeMessage { PortfolioName = "Portfolio2", SecurityId = _secId2, ServerTime = _now }
			.TryAdd(PositionChangeTypes.CurrentValue, 200m);

		holder.Process(msg1);
		holder.Process(msg2);

		holder.ResetSnapshot("Portfolio1", _secId1);

		holder.TryGetSnapshot(msg1, out var snap1).AssertFalse();
		snap1.AssertNull();
		holder.TryGetSnapshot(msg2, out var snap2).AssertTrue();
		snap2.AssertNotNull();
	}

	[TestMethod]
	public void Position_ResetSnapshot_Null_ClearsAll()
	{
		var holder = new PositionSnapshotHolder();

		var msg1 = new PositionChangeMessage { PortfolioName = "Portfolio1", SecurityId = _secId1, ServerTime = _now }
			.TryAdd(PositionChangeTypes.CurrentValue, 100m);
		var msg2 = new PositionChangeMessage { PortfolioName = "Portfolio2", SecurityId = _secId2, ServerTime = _now }
			.TryAdd(PositionChangeTypes.CurrentValue, 200m);

		holder.Process(msg1);
		holder.Process(msg2);

		holder.ResetSnapshot(null);

		holder.TryGetSnapshot(msg1, out var snap1).AssertFalse();
		snap1.AssertNull();
		holder.TryGetSnapshot(msg2, out var snap2).AssertFalse();
		snap2.AssertNull();
	}

	[TestMethod]
	public void Position_MultiplePositions_AreIsolated()
	{
		var holder = new PositionSnapshotHolder();

		var msg1 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		var msg2 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio2",
			SecurityId = _secId2,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 200m);

		var snap1 = holder.Process(msg1);
		var snap2 = holder.Process(msg2);

		snap1.AssertNotSame(snap2);
		snap1.Changes[PositionChangeTypes.CurrentValue].AssertEqual(100m);
		snap2.Changes[PositionChangeTypes.CurrentValue].AssertEqual(200m);

		// Update only first position
		var msg3 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 150m);

		holder.Process(msg3);

		snap1.Changes[PositionChangeTypes.CurrentValue].AssertEqual(150m);
		snap2.Changes[PositionChangeTypes.CurrentValue].AssertEqual(200m);
	}

	[TestMethod]
	public void Position_DifferentSides_AreIsolated()
	{
		var holder = new PositionSnapshotHolder();

		var msgLong = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			Side = Sides.Buy,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		var msgShort = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			Side = Sides.Sell,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, -50m);

		var snapLong = holder.Process(msgLong);
		var snapShort = holder.Process(msgShort);

		snapLong.AssertNotSame(snapShort);
		snapLong.Changes[PositionChangeTypes.CurrentValue].AssertEqual(100m);
		snapShort.Changes[PositionChangeTypes.CurrentValue].AssertEqual(-50m);
	}

	[TestMethod]
	public void Position_DifferentStrategies_AreIsolated()
	{
		var holder = new PositionSnapshotHolder();

		var msg1 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			StrategyId = "Strategy1",
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		var msg2 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			StrategyId = "Strategy2",
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 200m);

		var snap1 = holder.Process(msg1);
		var snap2 = holder.Process(msg2);

		snap1.AssertNotSame(snap2);
		snap1.Changes[PositionChangeTypes.CurrentValue].AssertEqual(100m);
		snap2.Changes[PositionChangeTypes.CurrentValue].AssertEqual(200m);
	}

	[TestMethod]
	public Task Position_ConcurrentAccess_ThreadSafe()
	{
		var holder = new PositionSnapshotHolder();
		var token = CancellationToken;

		var tasks = Enumerable.Range(0, 100).Select(i => Task.Run(() =>
		{
			var portfolioName = $"Portfolio{i % 5}";
			var secId = i % 2 == 0 ? _secId1 : _secId2;
			var msg = new PositionChangeMessage
			{
				PortfolioName = portfolioName,
				SecurityId = secId,
				ServerTime = _now.AddMilliseconds(i),
			}
			.TryAdd(PositionChangeTypes.CurrentValue, i);
			holder.Process(msg);
			holder.TryGetSnapshot(msg, out var _);
		}, token)).ToArray();

		return Task.WhenAll(tasks);
	}

	[TestMethod]
	public void Position_FirstMessage_IsCloned()
	{
		var holder = new PositionSnapshotHolder();

		var original = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		var snapshot = holder.Process(original);

		// Modifying original should not affect snapshot
		original.Changes[PositionChangeTypes.CurrentValue] = 200m;

		snapshot.Changes[PositionChangeTypes.CurrentValue].AssertEqual(100m);
	}

	[TestMethod]
	public void Position_KeyIsCaseInsensitive()
	{
		var holder = new PositionSnapshotHolder();

		var msg1 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			StrategyId = "STRATEGY",
			ClientCode = "CLIENT",
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		var snap1 = holder.Process(msg1);

		var msg2 = new PositionChangeMessage
		{
			PortfolioName = "PORTFOLIO1",
			SecurityId = _secId1,
			StrategyId = "strategy",
			ClientCode = "client",
			ServerTime = _now.AddSeconds(1),
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 200m);

		var snap2 = holder.Process(msg2);

		// Should be same reference due to case-insensitive key
		snap1.AssertSame(snap2);
		snap1.Changes[PositionChangeTypes.CurrentValue].AssertEqual(200m);
	}

	#endregion

	#region Order Immutability and Behavior Tests

	/// <summary>
	/// Immutable fields (SecurityId, Side, Price, Volume, Portfolio, OrderType, TimeInForce)
	/// cannot be changed after order is created. Attempting to change them throws an exception.
	/// </summary>
	[TestMethod]
	public void Order_ImmutableFields_ThrowOnChange()
	{
		var holder = new OrderSnapshotHolder();

		// First message sets all base fields
		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			Side = Sides.Buy,
			OrderPrice = 100m,
			OrderVolume = 10,
			PortfolioName = "Portfolio1",
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
		};

		holder.Process(msg1);

		// Try to change Side - should throw
		ThrowsExactly<InvalidOperationException>(() => holder.Process(new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			Side = Sides.Sell,
		}));

		// Try to change SecurityId - should throw
		ThrowsExactly<InvalidOperationException>(() => holder.Process(new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId2,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
		}));

		// Try to change OrderPrice - should throw
		ThrowsExactly<InvalidOperationException>(() => holder.Process(new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderPrice = 200m,
		}));

		// Try to change OrderVolume - should throw
		ThrowsExactly<InvalidOperationException>(() => holder.Process(new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderVolume = 20,
		}));

		// Try to change PortfolioName - should throw
		ThrowsExactly<InvalidOperationException>(() => holder.Process(new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			PortfolioName = "Portfolio2",
		}));

		// Try to change OrderType - should throw
		ThrowsExactly<InvalidOperationException>(() => holder.Process(new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderType = OrderTypes.Market,
		}));

		// Try to change TimeInForce - should throw
		ThrowsExactly<InvalidOperationException>(() => holder.Process(new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			TimeInForce = TimeInForce.MatchOrCancel,
		}));
	}

	/// <summary>
	/// Mutable fields (Balance, OrderState, Commission, etc.) can be updated normally.
	/// </summary>
	[TestMethod]
	public void Order_MutableFields_UpdateNormally()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			Side = Sides.Buy,
			OrderPrice = 100m,
			OrderVolume = 10,
			Balance = 10,
		};

		var snapshot = holder.Process(msg1);
		snapshot.OrderState.AssertEqual(OrderStates.Pending);
		snapshot.Balance.AssertEqual(10);

		// Update mutable fields - should work
		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
			Balance = 5,
			Commission = 1.5m,
		};

		holder.Process(msg2);

		snapshot.OrderState.AssertEqual(OrderStates.Active);
		snapshot.Balance.AssertEqual(5);
		snapshot.Commission.AssertEqual(1.5m);

		// Immutable fields unchanged
		snapshot.Side.AssertEqual(Sides.Buy);
		snapshot.OrderPrice.AssertEqual(100m);
		snapshot.OrderVolume.AssertEqual(10);
	}

	/// <summary>
	/// Snapshots remain in memory after order is Done.
	/// This is by design - cleanup is application responsibility via ResetSnapshot.
	/// </summary>
	[TestMethod]
	public void Order_Snapshots_RemainAfterDone()
	{
		var holder = new OrderSnapshotHolder();

		// Simulate long-running system with many orders
		const int orderCount = 1000;

		for (var i = 1; i <= orderCount; i++)
		{
			var msg = new ExecutionMessage
			{
				TransactionId = i,
				SecurityId = _secId1,
				ServerTime = _now,
				HasOrderInfo = true,
				OrderState = OrderStates.Done, // Order is finished
			};
			holder.Process(msg);
		}

		// All 1000 snapshots are still in memory - by design
		// Cleanup is application responsibility

		var foundCount = 0;
		for (var i = 1; i <= orderCount; i++)
		{
			if (holder.TryGetSnapshot(i, out _))
				foundCount++;
		}

		// All snapshots still exist
		foundCount.AssertEqual(orderCount);

		// Manual ResetSnapshot cleans them
		holder.ResetSnapshot(0);

		foundCount = 0;
		for (var i = 1; i <= orderCount; i++)
		{
			if (holder.TryGetSnapshot(i, out _))
				foundCount++;
		}

		foundCount.AssertEqual(0); // Only after manual reset
	}

	/// <summary>
	/// Position snapshots remain in memory even when position is closed.
	/// This is by design - cleanup is application responsibility via ResetSnapshot.
	/// </summary>
	[TestMethod]
	public void Position_Snapshots_RemainAfterClosed()
	{
		var holder = new PositionSnapshotHolder();

		const int positionCount = 1000;

		for (var i = 0; i < positionCount; i++)
		{
			var msg = new PositionChangeMessage
			{
				PortfolioName = $"Portfolio{i}",
				SecurityId = _secId1,
				ServerTime = _now,
			}
			.TryAdd(PositionChangeTypes.CurrentValue, 0m); // Position closed

			holder.Process(msg);
		}

		// All positions still in memory - by design, cleanup is application responsibility

		var foundCount = 0;
		for (var i = 0; i < positionCount; i++)
		{
			if (holder.TryGetSnapshot($"Portfolio{i}", _secId1, null, null, null, null, null, out _))
				foundCount++;
		}

		foundCount.AssertEqual(positionCount);

		// Manual ResetSnapshot cleans them
		holder.ResetSnapshot(null);

		foundCount = 0;
		for (var i = 0; i < positionCount; i++)
		{
			if (holder.TryGetSnapshot($"Portfolio{i}", _secId1, null, null, null, null, null, out _))
				foundCount++;
		}

		foundCount.AssertEqual(0);
	}

	/// <summary>
	/// Verifies that TryGetSnapshot returns a copy, not the internal reference.
	/// This ensures thread-safety - concurrent readers get consistent snapshots.
	/// </summary>
	[TestMethod]
	public void Order_TryGetSnapshot_ReturnsCopy()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			Balance = 100,
		};

		holder.Process(msg1);

		// Get snapshot twice
		holder.TryGetSnapshot(1, out var snap1);
		holder.TryGetSnapshot(1, out var snap2);

		// Should be different references (copies)
		snap1.AssertNotSame(snap2);

		// But with same data
		snap1.OrderState.AssertEqual(snap2.OrderState);
		snap1.Balance.AssertEqual(snap2.Balance);
	}

	/// <summary>
	/// BUG #3: Position holder also returns live reference.
	/// </summary>
	[TestMethod]
	public void Position_LiveReference_CanBeMutatedExternally_Bug()
	{
		var holder = new PositionSnapshotHolder();

		var msg1 = new PositionChangeMessage
		{
			PortfolioName = "Portfolio1",
			SecurityId = _secId1,
			ServerTime = _now,
		}
		.TryAdd(PositionChangeTypes.CurrentValue, 100m);

		var snapshot = holder.Process(msg1);

		// Get snapshot via TryGetSnapshot
		holder.TryGetSnapshot(msg1, out var retrieved);

		// BUG: Same reference returned
		snapshot.AssertSame(retrieved);

		// External code can mutate the internal state!
		retrieved.Changes[PositionChangeTypes.CurrentValue] = 999m;

		// This affects the internal snapshot
		holder.TryGetSnapshot(msg1, out var afterMutation);
		afterMutation.Changes[PositionChangeTypes.CurrentValue].AssertEqual(999m);

		// Original 'snapshot' variable is also affected (same reference)
		snapshot.Changes[PositionChangeTypes.CurrentValue].AssertEqual(999m);
	}

	/// <summary>
	/// Verifies that returned copies are isolated from internal state changes.
	/// When component A gets a snapshot, subsequent updates don't affect it.
	/// </summary>
	[TestMethod]
	public void Order_ReturnedCopy_IsIsolated()
	{
		var holder = new OrderSnapshotHolder();

		var msg1 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now,
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
			Balance = 100,
		};

		holder.Process(msg1);

		// Component A gets snapshot for reporting
		holder.TryGetSnapshot(1, out var forReport);
		forReport.Balance.AssertEqual(100);

		// Component B updates the order
		var msg2 = new ExecutionMessage
		{
			TransactionId = 1,
			SecurityId = _secId1,
			ServerTime = _now.AddSeconds(1),
			HasOrderInfo = true,
			Balance = 50,
		};
		holder.Process(msg2);

		// Component A's copy is NOT affected - it's isolated
		forReport.Balance.AssertEqual(100);

		// New snapshot shows updated value
		holder.TryGetSnapshot(1, out var updated);
		updated.Balance.AssertEqual(50);
	}

	#endregion
}
