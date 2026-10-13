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
	public void ReplaceTracksOneRegistrationRoundTrip()
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

		var response = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(7),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		var latency = mgr.ProcessMessage(response);
		latency.AssertEqual(TimeSpan.FromMilliseconds(7));
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(7));
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero);

		var laterOrderUpdate = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(15),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		mgr.ProcessMessage(laterOrderUpdate).AssertNull();
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(7));
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero);
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
	public void ReplaceUsesTransactionIdForRoundTripTracking()
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

		var oldOrderUpdate = new ExecutionMessage
		{
			OriginalTransactionId = replace.OriginalTransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(5),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		mgr.ProcessMessage(oldOrderUpdate).AssertNull();

		var response = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(8),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};

		mgr.ProcessMessage(response).AssertEqual(TimeSpan.FromMilliseconds(8));
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(8));
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero);
	}

	/// <summary>
	/// A replace is one round trip, and the venue answers it once. Whatever else the manager files
	/// away when the replace goes out, only that one answer is a measurement; if something is left
	/// pending under the order's key, the order's ordinary later life - a fill, a finish - is read as
	/// the answer to a cancellation that never happened, and the cancellation latency the trader
	/// reads becomes the age of an order rather than the time a venue took to cancel one.
	/// </summary>
	[TestMethod]
	[Timeout(5_000)]
	public void ReplaceLeavesNoPendingCancellationForAnOrderNobodyCancelled()
	{
		var mgr = new LatencyManager(new LatencyManagerState());
		var t0 = DateTime.UtcNow;

		var replace = new OrderReplaceMessage
		{
			TransactionId = 300,
			OriginalTransactionId = 200,
			LocalTime = t0,
		};

		mgr.ProcessMessage(replace).AssertNull();

		// The venue confirms the replaced order is working: this is the answer to the round trip.
		var active = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(5),
			OrderState = OrderStates.Active,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		};

		mgr.ProcessMessage(active).AssertEqual(TimeSpan.FromMilliseconds(5));
		mgr.LatencyRegistration.AssertEqual(TimeSpan.FromMilliseconds(5));

		// Much later the order simply finishes - nobody cancelled it.
		var done = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(500),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		};

		mgr.ProcessMessage(done).AssertNull("an order finishing is not a cancellation being answered");
		mgr.LatencyCancellation.AssertEqual(TimeSpan.Zero, "no cancellation was ever asked for, so there is no cancellation latency to report");
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
