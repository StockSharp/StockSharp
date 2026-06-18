namespace StockSharp.Tests;

[TestClass]
public class LookupTrackingManagerStateTests : BaseTestClass
{
	private static LookupTrackingManagerState CreateState() => new();

	private static SecurityLookupMessage CreateLookup(long transactionId)
	{
		return new SecurityLookupMessage { TransactionId = transactionId };
	}

	[TestMethod]
	public void AddLookup_TryGetAndRemove_ReturnsTrue()
	{
		var state = CreateState();
		var msg = CreateLookup(1);

		state.AddLookup(1, msg, TimeSpan.FromSeconds(10));

		IsTrue(state.TryGetAndRemoveLookup(1, out var sub));
		IsNotNull(sub);
		AreEqual(1L, sub.TransactionId);
	}

	[TestMethod]
	public void TryGetAndRemove_NonExistent_ReturnsFalse()
	{
		var state = CreateState();

		IsFalse(state.TryGetAndRemoveLookup(999, out var sub));
		IsNull(sub);
	}

	[TestMethod]
	public void TryEnqueue_FirstItem_ReturnsFalse()
	{
		var state = CreateState();
		var msg = CreateLookup(1);

		// First item should proceed immediately, not be queued
		var result = state.TryEnqueue(MessageTypes.SecurityLookup, 1, msg);

		IsFalse(result);
	}

	[TestMethod]
	public void TryEnqueue_SecondItem_ReturnsTrue()
	{
		var state = CreateState();
		var msg1 = CreateLookup(1);
		var msg2 = CreateLookup(2);

		state.TryEnqueue(MessageTypes.SecurityLookup, 1, msg1);
		var result = state.TryEnqueue(MessageTypes.SecurityLookup, 2, msg2);

		IsTrue(result);
	}

	[TestMethod]
	public void TryDequeueNext_AfterEnqueue_ReturnsNextItem()
	{
		var state = CreateState();
		var msg1 = CreateLookup(1);
		var msg2 = CreateLookup(2);

		state.TryEnqueue(MessageTypes.SecurityLookup, 1, msg1);
		state.TryEnqueue(MessageTypes.SecurityLookup, 2, msg2);

		// Remove first item, should get second
		var next = state.TryDequeueNext(MessageTypes.SecurityLookup, 1);

		next.AssertSame(msg2);
	}

	[TestMethod]
	public void TryDequeueNext_NoQueue_ReturnsNull()
	{
		var state = CreateState();

		var next = state.TryDequeueNext(MessageTypes.SecurityLookup, 999);

		IsNull(next);
	}

	[TestMethod]
	public void TryDequeueFromAnyType_FindsInAnyQueue()
	{
		var state = CreateState();
		var securityLookup = CreateLookup(1);
		var portfolioLookup1 = new PortfolioLookupMessage { TransactionId = 2 };
		var portfolioLookup2 = new PortfolioLookupMessage { TransactionId = 3 };

		state.TryEnqueue(MessageTypes.SecurityLookup, 1, securityLookup);
		state.TryEnqueue(MessageTypes.PortfolioLookup, 2, portfolioLookup1);
		state.TryEnqueue(MessageTypes.PortfolioLookup, 3, portfolioLookup2);

		// Remove id=2 from the portfolio queue while another queue also exists.
		var next = state.TryDequeueFromAnyType(2);

		next.AssertSame(portfolioLookup2);
	}

	[TestMethod]
	public void ProcessTimeouts_TimedOut_ReturnsSubscription()
	{
		var state = CreateState();
		var msg = CreateLookup(1);

		state.AddLookup(1, msg, TimeSpan.FromSeconds(1));

		// Process 2 seconds - should time out
		var timedOut = state.ProcessTimeouts(TimeSpan.FromSeconds(2), null).ToArray();

		AreEqual(1, timedOut.Length);
		AreEqual(1L, timedOut[0].subscription.TransactionId);
	}

	[TestMethod]
	public void ProcessTimeouts_NotTimedOut_ReturnsEmpty()
	{
		var state = CreateState();
		var msg = CreateLookup(1);

		state.AddLookup(1, msg, TimeSpan.FromSeconds(10));

		// Process 1 second - should NOT time out
		var timedOut = state.ProcessTimeouts(TimeSpan.FromSeconds(1), null).ToArray();

		AreEqual(0, timedOut.Length);
	}

	[TestMethod]
	public void ProcessTimeouts_IgnoredIds_Skipped()
	{
		var state = CreateState();
		var msg = CreateLookup(1);

		state.AddLookup(1, msg, TimeSpan.FromSeconds(1));

		// Process 2 seconds but ignore id=1
		var timedOut = state.ProcessTimeouts(TimeSpan.FromSeconds(2), [1]).ToArray();

		AreEqual(0, timedOut.Length);
	}

	[TestMethod]
	public void PreviousTime_SetGet()
	{
		var state = CreateState();
		var time = new DateTime(2025, 6, 15, 12, 0, 0);

		state.PreviousTime = time;

		AreEqual(time, state.PreviousTime);
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = CreateState();
		var msg = CreateLookup(1);

		state.AddLookup(1, msg, TimeSpan.FromSeconds(10));
		state.PreviousTime = DateTime.Now;

		state.Clear();

		IsFalse(state.TryGetAndRemoveLookup(1, out _));
		AreEqual(default, state.PreviousTime);
	}
}
