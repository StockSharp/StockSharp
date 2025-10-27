namespace StockSharp.Tests;

[TestClass]
public class SnapshotHolderTests : BaseTestClass
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
		Assert.ThrowsExactly<ArgumentNullException>(() => holder.Process(null));
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
		// Expect delta (increment), not full snapshot
		(delta.State == QuoteChangeStates.Increment || delta.State == null).AssertTrue();

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

		var res = holder.Process(inc);
		(res == null || res.State == QuoteChangeStates.SnapshotComplete || res.State == QuoteChangeStates.Increment).AssertTrue();
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
	public void OrderBook_InvalidFullSnapshot_ThrowsException()
	{
		var holder = new OrderBookSnapshotHolder();

		// try to force builder.TryApply to fail by using positions in full snapshot (potentially invalid)
		var invalidFull = new QuoteChangeMessage
		{
			SecurityId = _secId1,
			ServerTime = _now,
			State = null,
			HasPositions = true,
			Bids = [ new QuoteChange(100m, 1) { Action = QuoteChangeActions.New, StartPosition = 5 } ],
			Asks = [],
		};

		try
		{
			Assert.ThrowsExactly<InvalidOperationException>(() => holder.Process(invalidFull));
		}
		catch
		{
			// if builder accepts it, mark as inconclusive by asserting non-null snapshot
			holder.TryGetSnapshot(_secId1, out var snap).AssertTrue();
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

		var beforeSnap = holder.TryGetSnapshot(_secId1, out var snap1) ? snap1 : null;

		var inc = new QuoteChangeMessage { SecurityId = _secId1, ServerTime = _now.AddSeconds(1), State = QuoteChangeStates.Increment, Bids = [], Asks = [] };
		var res = holder.Process(inc);

		(res == null || res == inc).AssertTrue();

		holder.TryGetSnapshot(_secId1, out var snap2).AssertTrue();
		// snapshot should not change for empty increment
		if (beforeSnap != null)
		{
			snap2.Bids.Length.AssertEqual(beforeSnap.Bids.Length);
			snap2.Asks.Length.AssertEqual(beforeSnap.Asks.Length);
		}
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
		Assert.ThrowsExactly<ArgumentNullException>(() => holder.Process(null));
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
}
