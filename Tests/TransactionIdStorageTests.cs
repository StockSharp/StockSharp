namespace StockSharp.Tests;

[TestClass]
public class TransactionIdStorageTests : BaseTestClass
{
	private static readonly string _session1 = "session1";
	private static readonly string _session2 = "session2";

	// Deterministic generator to simulate duplicate transaction IDs.
	private sealed class DeterministicIdGenerator : IdGenerator
	{
		private readonly long[] _sequence;
		private int _index;

		public DeterministicIdGenerator(params long[] sequence)
		{
			_sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
			if (_sequence.Length == 0)
				throw new ArgumentException("Sequence must contain at least one value.", nameof(sequence));
		}

		public override long GetNextId()
		{
			if (_index < _sequence.Length)
				return _sequence[_index++];

			// If exhausted, keep returning the last value to maintain determinism in tests
			return _sequence[^1];
		}
	}

	#region InMemoryTransactionIdStorage Tests

	[TestMethod]
	public void InMemory_Constructor_NullIdGeneratorThrows()
	{
		ThrowsExactly<ArgumentNullException>(() => new InMemoryTransactionIdStorage(null));
	}

	[TestMethod]
	public void InMemory_Get_NonPersistable_CreatesNewInstance()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);

		var session1 = storage.Get(_session1, persistable: false);
		var session2 = storage.Get(_session1, persistable: false);

		// Non-persistable should return different instances
		session1.AssertNotSame(session2);
	}

	[TestMethod]
	public void InMemory_Get_Persistable_ReturnsSameInstance()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);

		var session1 = storage.Get(_session1, persistable: true);
		var session2 = storage.Get(_session1, persistable: true);

		// Persistable should return same instance for same session ID
		session1.AssertSame(session2);
	}

	[TestMethod]
	public void InMemory_Get_DifferentSessions_ReturnsDifferentInstances()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);

		var session1 = storage.Get(_session1, persistable: true);
		var session2 = storage.Get(_session2, persistable: true);

		session1.AssertNotSame(session2);
	}

	#endregion

	#region InMemorySessionTransactionIdStorage Tests

	[TestMethod]
	public void InMemorySession_CreateRequestId_GeneratesUniqueId()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId1 = session.CreateRequestId();
		var requestId2 = session.CreateRequestId();

		requestId1.AssertNotNull();
		requestId2.AssertNotNull();
		requestId1.AssertNotEqual(requestId2);
	}

	[TestMethod]
	public void InMemorySession_CreateRequestId_AutoAssociatesTransactionId()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId = session.CreateRequestId();
		var found = session.TryGetTransactionId(requestId, out var transactionId);

		found.AssertTrue();
		transactionId.AssertEqual(1);
	}

	[TestMethod]
	public void InMemorySession_CreateTransactionId_NullRequestIdThrows()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		ThrowsExactly<ArgumentNullException>(() => session.CreateTransactionId(null));
	}

	[TestMethod]
	public void InMemorySession_CreateTransactionId_EmptyRequestIdThrows()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		ThrowsExactly<ArgumentNullException>(() => session.CreateTransactionId(""));
	}

	[TestMethod]
	public void InMemorySession_CreateTransactionId_CreatesAssociation()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId = "custom-request-123";
		var transactionId = session.CreateTransactionId(requestId);

		transactionId.AssertEqual(1);

		// Verify bidirectional association
		var ok = session.TryGetTransactionId(requestId, out var t1);
		ok.AssertTrue();
		t1.AssertEqual(transactionId);
		session.TryGetRequestId(transactionId, out var r1).AssertTrue();
		r1.AssertEqual(requestId);
	}

	[TestMethod]
	public void InMemorySession_CreateTransactionId_DuplicateTransactionIdThrows()
	{
		// Force generator to return the same transaction ID twice
		var idGen = new DeterministicIdGenerator(10, 10);
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		// First association succeeds
		session.CreateTransactionId("request-1");

		// Second association tries to use the same transactionId but a different requestId
		// Should trigger duplicate transactionId branch in Add(...)
		ThrowsExactly<ArgumentException>(() => session.CreateTransactionId("request-2"));
	}

	[TestMethod]
	public void InMemorySession_CreateRequestId_DuplicateTransactionIdThrows()
	{
		// Duplicate transaction IDs through CreateRequestId path
		var idGen = new DeterministicIdGenerator(42, 42);
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var _ = session.CreateRequestId();
		ThrowsExactly<ArgumentException>(() => session.CreateRequestId());
	}

	[TestMethod]
	public void InMemorySession_CreateTransactionId_DuplicateRequestIdThrows()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId = "duplicate-request";
		session.CreateTransactionId(requestId);

		// Try to create another transaction ID for same request ID
		ThrowsExactly<ArgumentException>(() => session.CreateTransactionId(requestId));
	}

	[TestMethod]
	public void InMemorySession_TryGetRequestId_NotFound_ReturnsFalse()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var ok = session.TryGetRequestId(999999, out var requestId);

		ok.AssertFalse();
		requestId.AssertNull();
	}

	[TestMethod]
	public void InMemorySession_TryGetTransactionId_NotFound_ReturnsFalse()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var found = session.TryGetTransactionId("nonexistent-request", out var _);
		found.AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_TryGetTransactionId_EmptyRequestId_ReturnsFalse()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var ok = session.TryGetTransactionId(string.Empty, out var _);
		ok.AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_RemoveRequestId_RemovesAssociation()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId = "remove-test";
		var transactionId = session.CreateTransactionId(requestId);

		var removed = session.RemoveRequestId(requestId);

		removed.AssertTrue();
		session.TryGetRequestId(transactionId, out var _).AssertFalse();
		session.TryGetTransactionId(requestId, out _).AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_RemoveRequestId_NotFound_ReturnsFalse()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var removed = session.RemoveRequestId("nonexistent");

		removed.AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_RemoveRequestId_Twice_SecondReturnsFalse()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId = "remove-twice";
		session.CreateTransactionId(requestId);

		session.RemoveRequestId(requestId).AssertTrue();
		session.RemoveRequestId(requestId).AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_RemoveTransactionId_RemovesAssociation()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId = "remove-test-2";
		var transactionId = session.CreateTransactionId(requestId);

		var removed = session.RemoveTransactionId(transactionId);

		removed.AssertTrue();
		session.TryGetRequestId(transactionId, out var _).AssertFalse();
;
		session.TryGetTransactionId(requestId, out _).AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_RemoveTransactionId_NotFound_ReturnsFalse()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var removed = session.RemoveTransactionId(999999);

		removed.AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_RemoveTransactionId_Twice_SecondReturnsFalse()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId = "remove-trans-twice";
		var transactionId = session.CreateTransactionId(requestId);

		session.RemoveTransactionId(transactionId).AssertTrue();
		session.RemoveTransactionId(transactionId).AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_MultipleAssociations_WorkCorrectly()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var req1 = "request-1";
		var req2 = "request-2";
		var req3 = "request-3";

		var trans1 = session.CreateTransactionId(req1);
		var trans2 = session.CreateTransactionId(req2);
		var trans3 = session.CreateTransactionId(req3);

		// Verify all associations exist
		session.TryGetRequestId(trans1, out var r1).AssertTrue();
		r1.AssertEqual(req1);

		session.TryGetRequestId(trans2, out var r2).AssertTrue();
		r2.AssertEqual(req2);

		session.TryGetRequestId(trans3, out var r3).AssertTrue();
		r3.AssertEqual(req3);

		session.TryGetTransactionId(req1, out var t1).AssertTrue();
		t1.AssertEqual(trans1);

		session.TryGetTransactionId(req2, out var t2).AssertTrue();
		t2.AssertEqual(trans2);

		session.TryGetTransactionId(req3, out var t3).AssertTrue();
		t3.AssertEqual(trans3);

		// Remove one and verify others remain
		session.RemoveRequestId(req2);

		session.TryGetRequestId(trans1, out r1).AssertTrue();
		r1.AssertEqual(req1);

		session.TryGetRequestId(trans2, out _).AssertFalse();

		session.TryGetRequestId(trans3, out r3).AssertTrue();
		r3.AssertEqual(req3);
	}

	#endregion

	#region PlainTransactionIdStorage Tests

	[TestMethod]
	public void Plain_Get_AlwaysReturnsNewInstance()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();

		var session1a = storage.Get(_session1, persistable: true);
		var session1b = storage.Get(_session1, persistable: true);
		var session2 = storage.Get(_session2, persistable: false);

		// Plain storage ignores persistable flag and always returns new instances
		session1a.AssertNotSame(session1b);
		session1a.AssertNotSame(session2);
	}

	[TestMethod]
	public void PlainSession_TryGetRequestId_ConvertsTransactionIdToString()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		session.TryGetRequestId(123456, out var requestId).AssertTrue();
		requestId.AssertNotNull();
		requestId.AssertEqual("123456");
	}

	[TestMethod]
	public void PlainSession_TryGetTransactionId_ConvertsStringToLong()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		var ok = session.TryGetTransactionId("789012", out var transactionId);

		ok.AssertTrue();
		transactionId.AssertEqual(789012);
	}

	[TestMethod]
	public async Task PlainSession_CreateRequestId_GeneratesTimestampBasedId()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		var requestId1 = session.CreateRequestId();
		await Task.Delay(1, CancellationToken); // Ensure different timestamp
		var requestId2 = session.CreateRequestId();

		requestId1.AssertNotNull();
		requestId2.AssertNotNull();

		// Request IDs should be different (different timestamps)
		requestId1.AssertNotEqual(requestId2);

		// Should be parseable as long (ticks)
		long.Parse(requestId1);
		long.Parse(requestId2);
	}

	[TestMethod]
	public void PlainSession_CreateTransactionId_ConvertsRequestIdToLong()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		var transactionId = session.CreateTransactionId("999888");

		transactionId.AssertEqual(999888);
	}

	[TestMethod]
	public void PlainSession_RemoveRequestId_AlwaysReturnsTrue()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		var removed = session.RemoveRequestId("any-id");

		// Plain implementation doesn't track associations, always returns true
		removed.AssertTrue();
	}

	[TestMethod]
	public void PlainSession_RemoveTransactionId_AlwaysReturnsTrue()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		var removed = session.RemoveTransactionId(12345);

		// Plain implementation doesn't track associations, always returns true
		removed.AssertTrue();
	}

	[TestMethod]
	public void PlainSession_NoActualMapping_JustConversion()
	{
		// Plain storage doesn't maintain actual mappings,
		// it just converts between long and string representations
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		var transId1 = session.CreateTransactionId("12345");
		var transId2 = session.CreateTransactionId("67890");

		transId1.AssertEqual(12345);
		transId2.AssertEqual(67890);

		// "Removing" doesn't affect anything
		session.RemoveTransactionId(transId1);

		// Can still "get" the same values (because it's just conversion)
		session.TryGetRequestId(transId1, out var requestId).AssertTrue();
		requestId.AssertEqual("12345");

		session.TryGetTransactionId("12345", out var t1).AssertTrue();
		t1.AssertEqual(12345);
	}

	[TestMethod]
	public void PlainSession_TryGetTransactionId_EmptyRequestId_ReturnsFalse()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		var ok = session.TryGetTransactionId(string.Empty, out var _);
		ok.AssertFalse();
	}

	[TestMethod]
	public void PlainSession_RemoveRequestId_Null_ReturnsTrue()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		// Method always returns true regardless of input
		session.RemoveRequestId(null).AssertTrue();
	}

	#endregion

	#region Session Isolation Tests

	[TestMethod]
	public void InMemory_Sessions_AreIsolated()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);

		var session1 = storage.Get(_session1, persistable: true);
		var session2 = storage.Get(_session2, persistable: true);

		var req1 = "shared-request-id";
		var trans1 = session1.CreateTransactionId(req1);
		var trans2 = session2.CreateTransactionId(req1);

		// Different sessions can have same request ID with different transaction IDs
		trans1.AssertNotEqual(trans2);

		// Each session only knows about its own associations
		session1.TryGetTransactionId(req1, out var t1).AssertTrue();
		t1.AssertEqual(trans1);

		session2.TryGetTransactionId(req1, out var t2).AssertTrue();
		t2.AssertEqual(trans2);
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public void InMemorySession_LargeTransactionId_HandledCorrectly()
	{
		var idGen = new IncrementalIdGenerator
		{
			Current = long.MaxValue - 10
		};
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var requestId = session.CreateRequestId();
		var ok = session.TryGetTransactionId(requestId, out var transactionId);

		ok.AssertTrue();
		transactionId.AssertGreater(long.MaxValue - 11);
	}

	[TestMethod]
	public void PlainSession_InvalidNumericString_ReturnsFalse()
	{
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		// Try* methods should not throw exceptions. Should return false on invalid input
		var ok = session.TryGetTransactionId("not-a-number", out var _);
		ok.AssertFalse();
	}

	[TestMethod]
	public void InMemorySession_TryGetTransactionId_NullRequestId_ReturnsFalse()
	{
		var idGen = new IncrementalIdGenerator();
		ITransactionIdStorage storage = new InMemoryTransactionIdStorage(idGen);
		var session = storage.Get(_session1, persistable: true);

		var ok = session.TryGetTransactionId(null, out var _);
		ok.AssertFalse();
	}

	[TestMethod]
	public void PlainSession_CreateTransactionId_NullRequestId_Throws()
	{
		// CreateTransactionId should validate inputs similar to InMemory version
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		ThrowsExactly<ArgumentNullException>(() => session.CreateTransactionId(null));
	}

	[TestMethod]
	public void PlainSession_CreateTransactionId_EmptyRequestId_Throws()
	{
		// CreateTransactionId should validate inputs similar to InMemory version
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		ThrowsExactly<ArgumentNullException>(() => session.CreateTransactionId(""));
	}

	[TestMethod]
	public void PlainSession_TryGetTransactionId_NullRequestId_ShouldNotThrowAndReturnFalse()
	{
		// Try* methods should not throw exceptions
		ITransactionIdStorage storage = new PlainTransactionIdStorage();
		var session = storage.Get(_session1, persistable: false);

		var ok = session.TryGetTransactionId(null, out var _);
		ok.AssertFalse();
	}

	#endregion
}
