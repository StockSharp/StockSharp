namespace StockSharp.Tests;

using StockSharp.Algo.Latency;

[TestClass]
public class LatencyTests
{
	[TestMethod]
	public void RegisterLatencyCalculated()
	{
		var mgr = new LatencyManager();
		var t0 = DateTimeOffset.UtcNow;
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
		var mgr = new LatencyManager();
		var t0 = DateTimeOffset.UtcNow;
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
	public void ReplaceUsesOriginalIdForCancel()
	{
		var mgr = new LatencyManager();
		var t0 = DateTimeOffset.UtcNow;
		var replace = new OrderReplaceMessage
		{
			TransactionId = 3,
			OriginalTransactionId = 30,
			LocalTime = t0
		};
		mgr.ProcessMessage(replace).AssertNull();

		var cancelExec = new ExecutionMessage
		{
			OriginalTransactionId = replace.OriginalTransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(7),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		var cancelLat = mgr.ProcessMessage(cancelExec);
		cancelLat.AssertEqual(TimeSpan.FromMilliseconds(7));

		var regExec = new ExecutionMessage
		{
			OriginalTransactionId = replace.TransactionId,
			LocalTime = t0 + TimeSpan.FromMilliseconds(15),
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		};
		var regLat = mgr.ProcessMessage(regExec);
		regLat.AssertEqual(TimeSpan.FromMilliseconds(15));
	}

	[TestMethod]
	public void ResetClearsState()
	{
		var mgr = new LatencyManager();
		var t0 = DateTimeOffset.UtcNow;
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
}
