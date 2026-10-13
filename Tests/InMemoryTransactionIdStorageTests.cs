namespace StockSharp.Tests;

[TestClass]
[TestCategory("Unit")]
public class InMemoryTransactionIdStorageTests : BaseTestClass
{
	private static ITransactionIdStorage CreateStorage()
		=> new InMemoryTransactionIdStorage(new IncrementalIdGenerator());

	// Basic functionality

	[TestMethod]
	public void Get_NonPersistable_AlwaysReturnsNewInstance()
	{
		var storage = CreateStorage();

		var s1 = storage.Get("session1", persistable: false);
		var s2 = storage.Get("session1", persistable: false);

		// non-persistable always creates new, so both can create same requestId
		var txId1 = s1.CreateTransactionId("req1");
		var txId2 = s2.CreateTransactionId("req1");

		AreNotEqual(txId1, txId2);
	}

	[TestMethod]
	public void Get_Persistable_SecondCall_ReturnsCleanInstance()
	{
		var storage = CreateStorage();

		var s1 = storage.Get("session1", persistable: true);
		s1.CreateTransactionId("req1");

		// second Get replaces the old session — clean state
		var s2 = storage.Get("session1", persistable: true);
		IsFalse(s2.TryGetTransactionId("req1", out _));
	}

	[TestMethod]
	public void CreateTransactionId_And_Lookup_Roundtrip()
	{
		var storage = CreateStorage();
		var session = storage.Get("session1", persistable: true);

		var txId = session.CreateTransactionId("order-123");

		IsTrue(session.TryGetTransactionId("order-123", out var found));
		AreEqual(txId, found);

		IsTrue(session.TryGetRequestId(txId, out var reqId));
		AreEqual("order-123", reqId);
	}

	[TestMethod]
	public void RemoveRequestId_ClearsMapping()
	{
		var storage = CreateStorage();
		var session = storage.Get("session1", persistable: true);

		session.CreateTransactionId("req1");
		IsTrue(session.RemoveRequestId("req1"));
		IsFalse(session.TryGetTransactionId("req1", out _));
	}

	// Reconnect behavior

	/// <summary>
	/// A client that reconnects asks for the same things again under the same names - the portfolio
	/// list, the order status - and is entitled to be answered. Were the session to remember the
	/// names it minted before the link dropped, the first request after every reconnect would be
	/// refused as a duplicate, and the client would come up with no portfolios and no orders.
	/// </summary>
	[TestMethod]
	public void AClientThatReconnectsMayAskForTheSameThingsAgain()
	{
		var storage = CreateStorage();

		var session1 = storage.Get("router", persistable: true);
		var beforeDrop = session1.CreateTransactionId("PortfolioLookup");
		session1.CreateTransactionId("OrderStatus");

		// A reconnect under the same sessionId must get fresh storage, with no stale mappings.
		var session2 = storage.Get("router", persistable: true);

		IsFalse(session2.TryGetTransactionId("PortfolioLookup", out _),
			"the session the reconnect got still remembers what the dropped one asked for");

		var afterReconnect = session2.CreateTransactionId("PortfolioLookup");

		IsTrue(afterReconnect > 0, "the request after a reconnect has to be given an identity of its own");
		AreNotEqual(beforeDrop, afterReconnect, "the new request must not be answered under the dropped one's transaction");

		IsTrue(session2.TryGetTransactionId("PortfolioLookup", out var found));
		AreEqual(afterReconnect, found, "the answer to the new request has to find its way back to it");
	}

	/// <summary>
	/// The same promise for the order status, which is the other request every client repeats the
	/// moment it is back: a trader who reconnects and is told nothing about their live orders has
	/// no way to know what is working.
	/// </summary>
	[TestMethod]
	public void AClientThatReconnectsMayAskForItsOrdersAgain()
	{
		var storage = CreateStorage();

		var session1 = storage.Get("router", persistable: true);
		var beforeDrop = session1.CreateTransactionId("OrderStatus");

		// Reconnect — must get clean state
		var session2 = storage.Get("router", persistable: true);

		var afterReconnect = session2.CreateTransactionId("OrderStatus");

		IsTrue(afterReconnect > 0, "the request after a reconnect has to be given an identity of its own");
		AreNotEqual(beforeDrop, afterReconnect, "the new request must not be answered under the dropped one's transaction");
		IsTrue(session2.TryGetRequestId(afterReconnect, out var reqId));
		AreEqual("OrderStatus", reqId);
	}

	/// <summary>
	/// A client that tidied up after itself before the link dropped is in no worse a position than
	/// one that did not: releasing the names it was using is allowed, and what follows the reconnect
	/// is the same clean session either way.
	/// </summary>
	[TestMethod]
	public void AClientThatReleasedItsRequestsBeforeReconnectingIsNoWorseOff()
	{
		var storage = CreateStorage();

		var session1 = storage.Get("router", persistable: true);
		session1.CreateTransactionId("PortfolioLookup");
		session1.CreateTransactionId("OrderStatus");

		IsTrue(session1.RemoveRequestId("PortfolioLookup"));
		IsTrue(session1.RemoveRequestId("OrderStatus"));

		var session2 = storage.Get("router", persistable: true);

		var txId = session2.CreateTransactionId("PortfolioLookup");

		IsTrue(txId > 0, "the request after a reconnect has to be given an identity of its own");
		IsTrue(session2.TryGetRequestId(txId, out var reqId));
		AreEqual("PortfolioLookup", reqId);
	}

	// Session isolation

	[TestMethod]
	public void Get_Persistable_DifferentSessions_Isolated()
	{
		var storage = CreateStorage();

		var s1 = storage.Get("client-A", persistable: true);
		var s2 = storage.Get("client-B", persistable: true);

		s1.CreateTransactionId("req1");

		IsFalse(s2.TryGetTransactionId("req1", out _));
	}
}
