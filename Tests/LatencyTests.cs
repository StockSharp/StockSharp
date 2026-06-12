namespace StockSharp.Tests;

using StockSharp.Algo.Latency;

[TestClass]
public class LatencyTests
{
	[TestMethod]
	public void RegisterLatencyCalculated()
	{
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;
		var reg = new OrderRegisterMessage { TransactionId = 1, LocalTime = t0 };
		mgr.ProcessMessage(reg).AssertNull();

		var exec = new ExecutionMessage
		{
			OriginalTransactionId = reg.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(10),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		var latency = mgr.ProcessMessage(exec);
		latency.AssertEqual(TimeSpan.FromMilliseconds(10));
		mgr.LatencyRegistration.AssertEqual(latency.Value);
	}

	[TestMethod]
	public void CancelLatencyCalculated()
	{
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;
		var cancel = new OrderCancelMessage { TransactionId = 2, LocalTime = t0 };
		mgr.ProcessMessage(cancel).AssertNull();

		var exec = new ExecutionMessage
		{
			OriginalTransactionId = cancel.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(5),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		var latency = mgr.ProcessMessage(exec);
		latency.AssertEqual(TimeSpan.FromMilliseconds(5));
		mgr.LatencyCancellation.AssertEqual(latency.Value);
	}

	[TestMethod]
	public void ReplaceTracksBothCancelAndRegister()
	{
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;
		var replace = new OrderReplaceMessage
		{
			TransactionId = 3,
			OriginalTransactionId = 30,
			LocalTime = t0
		};
		mgr.ProcessMessage(replace).AssertNull();

		// First execution for cancel part (tracked by TransactionId)
		var cancelExec = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(7),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		var cancelLat = mgr.ProcessMessage(cancelExec);
		// Gets register latency first (both tracked by same TransactionId).
		cancelLat.AssertEqual(TimeSpan.FromMilliseconds(7));
		// The first response is attributed to the registration bucket.
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(7));
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero);

		// Second execution for new order (also tracked by TransactionId)
		var regExec = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(15),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		var regLat = mgr.ProcessMessage(regExec);
		// Gets cancel latency (register was already consumed).
		regLat.AssertEqual(TimeSpan.FromMilliseconds(15));
		// The second response falls into the cancellation bucket; registration stays at 7ms.
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(7));
		mgr.LatencyCancellation.AssertEqual(TimeSpan.FromMilliseconds(15));
	}

	[TestMethod]
	public void ResetClearsState()
	{
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;
		var transId = 5L;
		mgr.ProcessMessage(new OrderRegisterMessage { TransactionId = transId, LocalTime = t0 });
		mgr.Reset();

		var exec = new ExecutionMessage
		{
			OriginalTransactionId = transId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(1),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		mgr.ProcessMessage(exec).AssertNull();
		mgr.LatencyRegistration.AreEqual(TimeSpan.Zero);
		mgr.LatencyCancellation.AreEqual(TimeSpan.Zero);
	}

	[TestMethod]
	public void ReplaceUsesTransactionIdForCancelTracking()
	{
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;

		// Replace order: TransactionId=100 (new order), OriginalTransactionId=50 (order to replace)
		var replace = new OrderReplaceMessage
		{
			TransactionId = 100,
			OriginalTransactionId = 50,
			LocalTime = t0
		};
		mgr.ProcessMessage(replace).AssertNull();

		// On OrderReplace the engine tracks BOTH a registration and a cancellation under the
		// SAME key (replaceMsg.TransactionId == 100). The Execution handler probes registration
		// FIRST, so the first response consumes the registration entry (returns register-latency),
		// not the cancellation. To actually prove the cancellation is keyed by TransactionId (the
		// fix), we must feed a SECOND response under the same key and verify it consumes the
		// cancellation. If the bug regressed (AddCancellation keyed by OriginalTransactionId=50),
		// nothing would be tracked under 100 and the second response would return null.
		var firstExec = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId, // 100, not 50
			LocalTime = t0 + TimeSpan.FromMilliseconds(5),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		// First response consumes the registration entry under key 100 (register-latency = 5ms).
		var firstLat = mgr.ProcessMessage(firstExec);
		firstLat.AssertEqual(TimeSpan.FromMilliseconds(5));
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(5));
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero);

		// Second response under the SAME key 100 must consume the cancellation entry.
		// This is the real regression guard: with the bug it would be null (cancellation
		// would have been stored under key 50), here it must be the cancel-latency = 8ms.
		var secondExec = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId, // 100, not 50
			LocalTime = t0 + TimeSpan.FromMilliseconds(8),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		var secondLat = mgr.ProcessMessage(secondExec);
		secondLat.AssertEqual(TimeSpan.FromMilliseconds(8));
		mgr.LatencyCancellation.AssertEqual(TimeSpan.FromMilliseconds(8));
	}

	[TestMethod]
	[Timeout(5_000)]
	public void PendingExecutionIgnored()
	{
		// Pending executions must be ignored (LatencyManager: OrderState == Pending -> break),
		// WITHOUT consuming the pending registration entry.
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;

		mgr.ProcessMessage(new OrderRegisterMessage { TransactionId = 1, LocalTime = t0 }).AssertNull();

		var pending = new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(3),
			OrderState = OrderStates.Pending,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		// Pending is ignored: no latency returned and no aggregate accumulated.
		mgr.ProcessMessage(pending).AssertNull();
		mgr.LatencyRegistration.AssertEqual(TimeSpan.Zero);
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero);

		// The registration entry must still be present: a subsequent terminal state consumes it.
		var done = new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(10),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		mgr.ProcessMessage(done).AssertEqual(TimeSpan.FromMilliseconds(10));
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(10));
	}

	[TestMethod]
	[Timeout(5_000)]
	public void ExecutionWithoutOrderInfoIgnored()
	{
		// HasOrderInfo == false -> HasOrderInfo() is false -> the execution is ignored
		// before any dictionary lookup, leaving the registration entry intact.
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;

		mgr.ProcessMessage(new OrderRegisterMessage { TransactionId = 1, LocalTime = t0 }).AssertNull();

		var noInfo = new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(3),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = false
		};

		mgr.ProcessMessage(noInfo).AssertNull();
		mgr.LatencyRegistration.AssertEqual(TimeSpan.Zero);

		// Registration is untouched: a proper order-info execution still consumes it.
		var done = new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(10),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		mgr.ProcessMessage(done).AssertEqual(TimeSpan.FromMilliseconds(10));
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(10));
	}

	[TestMethod]
	[Timeout(5_000)]
	public void NonTransactionExecutionIgnored()
	{
		// HasOrderInfo() also requires DataType == Transactions. A non-transaction execution
		// (even with HasOrderInfo == true) must be ignored and leave the registration intact.
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;

		mgr.ProcessMessage(new OrderRegisterMessage { TransactionId = 1, LocalTime = t0 }).AssertNull();

		var tick = new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(3),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Ticks,
			HasOrderInfo = true
		};

		mgr.ProcessMessage(tick).AssertNull();
		mgr.LatencyRegistration.AssertEqual(TimeSpan.Zero);

		var done = new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(10),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		mgr.ProcessMessage(done).AssertEqual(TimeSpan.FromMilliseconds(10));
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(10));
	}

	[TestMethod]
	[Timeout(5_000)]
	public void FailedRegistrationDoesNotAddLatency()
	{
		// Failed registration confirmation: the registration entry is removed but no latency
		// is accumulated (LatencyManager: OrderState == Failed -> break in the registration branch).
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;

		mgr.ProcessMessage(new OrderRegisterMessage { TransactionId = 1, LocalTime = t0 }).AssertNull();

		var failed = new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(4),
			OrderState = OrderStates.Failed,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		mgr.ProcessMessage(failed).AssertNull();
		mgr.LatencyRegistration.AssertEqual(TimeSpan.Zero);

		// The registration entry was consumed (removed) by the failed confirmation: a later
		// terminal execution under the same key finds nothing and returns null.
		var done = new ExecutionMessage
		{
			OriginalTransactionId = 1,
			LocalTime = t0 + TimeSpan.FromMilliseconds(10),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		mgr.ProcessMessage(done).AssertNull();
		mgr.LatencyRegistration.AssertEqual(TimeSpan.Zero);
	}

	[TestMethod]
	[Timeout(5_000)]
	public void FailedCancellationDoesNotAddLatency()
	{
		// Failed cancellation confirmation: the cancellation entry is removed but no latency
		// is accumulated (LatencyManager: OrderState == Failed -> break in the cancellation branch).
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;

		mgr.ProcessMessage(new OrderCancelMessage { TransactionId = 20, LocalTime = t0 }).AssertNull();

		var failed = new ExecutionMessage
		{
			OriginalTransactionId = 20,
			LocalTime = t0 + TimeSpan.FromMilliseconds(6),
			OrderState = OrderStates.Failed,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		mgr.ProcessMessage(failed).AssertNull();
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero);

		// The cancellation entry was consumed (removed): a later execution under the same key
		// finds nothing and returns null.
		var done = new ExecutionMessage
		{
			OriginalTransactionId = 20,
			LocalTime = t0 + TimeSpan.FromMilliseconds(12),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		mgr.ProcessMessage(done).AssertNull();
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero);
	}
}
