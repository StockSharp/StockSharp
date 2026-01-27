namespace StockSharp.Tests;

using StockSharp.Algo.Latency;

[TestClass]
public class LatencyManagerMockTests : BaseTestClass
{
	[TestMethod]
	public void OrderRegister_CallsAddRegistration()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var mgr = new LatencyManager(mockState.Object);
		var t0 = DateTime.UtcNow;

		mgr.ProcessMessage(new OrderRegisterMessage { TransactionId = 1, LocalTime = t0 });

		mockState.Verify(s => s.AddRegistration(1, t0), Times.Once);
	}

	[TestMethod]
	public void OrderCancel_CallsAddCancellation()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var mgr = new LatencyManager(mockState.Object);
		var t0 = DateTime.UtcNow;

		mgr.ProcessMessage(new OrderCancelMessage { TransactionId = 10, LocalTime = t0 });

		mockState.Verify(s => s.AddCancellation(10, t0), Times.Once);
	}

	[TestMethod]
	public void OrderReplace_CallsBothAddMethods()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var mgr = new LatencyManager(mockState.Object);
		var t0 = DateTime.UtcNow;

		mgr.ProcessMessage(new OrderReplaceMessage { TransactionId = 5, LocalTime = t0 });

		mockState.Verify(s => s.AddCancellation(5, t0), Times.Once);
		mockState.Verify(s => s.AddRegistration(5, t0), Times.Once);
	}

	[TestMethod]
	public void Execution_RemovesRegistrationAndAddsLatency()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var t0 = DateTime.UtcNow;
		var t1 = t0 + TimeSpan.FromMilliseconds(50);

		mockState.Setup(s => s.TryGetAndRemoveRegistration(5, out t0)).Returns(true);

		var mgr = new LatencyManager(mockState.Object);

		var latency = mgr.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = 5,
			LocalTime = t1,
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		});

		latency.AssertNotNull();
		latency.Value.AssertEqual(TimeSpan.FromMilliseconds(50));
		mockState.Verify(s => s.AddLatencyRegistration(TimeSpan.FromMilliseconds(50)), Times.Once);
	}

	[TestMethod]
	public void Execution_RemovesCancellationAndAddsLatency()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var t0 = DateTime.UtcNow;
		var t1 = t0 + TimeSpan.FromMilliseconds(30);

		DateTime dummyTime;
		mockState.Setup(s => s.TryGetAndRemoveRegistration(7, out dummyTime)).Returns(false);
		mockState.Setup(s => s.TryGetAndRemoveCancellation(7, out t0)).Returns(true);

		var mgr = new LatencyManager(mockState.Object);

		var latency = mgr.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = 7,
			LocalTime = t1,
			OrderState = OrderStates.Done,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		});

		latency.AssertNotNull();
		latency.Value.AssertEqual(TimeSpan.FromMilliseconds(30));
		mockState.Verify(s => s.AddLatencyCancellation(TimeSpan.FromMilliseconds(30)), Times.Once);
	}

	[TestMethod]
	public void Execution_FailedOrderDoesNotAddLatency()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var t0 = DateTime.UtcNow;

		mockState.Setup(s => s.TryGetAndRemoveRegistration(5, out t0)).Returns(true);

		var mgr = new LatencyManager(mockState.Object);

		var latency = mgr.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = 5,
			LocalTime = t0 + TimeSpan.FromMilliseconds(50),
			OrderState = OrderStates.Failed,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true
		});

		latency.AssertNull();
		mockState.Verify(s => s.AddLatencyRegistration(It.IsAny<TimeSpan>()), Times.Never);
	}

	[TestMethod]
	public void Reset_CallsClear()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var mgr = new LatencyManager(mockState.Object);

		mgr.Reset();

		mockState.Verify(s => s.Clear(), Times.Once);
	}

	[TestMethod]
	public void ResetMessage_CallsClear()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var mgr = new LatencyManager(mockState.Object);

		mgr.ProcessMessage(new ResetMessage());

		mockState.Verify(s => s.Clear(), Times.Once);
	}

	[TestMethod]
	public void LatencyRegistration_ReadsFromState()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var expected = TimeSpan.FromSeconds(5);
		mockState.Setup(s => s.LatencyRegistration).Returns(expected);

		var mgr = new LatencyManager(mockState.Object);

		mgr.LatencyRegistration.AssertEqual(expected);
	}

	[TestMethod]
	public void LatencyCancellation_ReadsFromState()
	{
		var mockState = new Mock<ILatencyManagerState>();
		var expected = TimeSpan.FromSeconds(3);
		mockState.Setup(s => s.LatencyCancellation).Returns(expected);

		var mgr = new LatencyManager(mockState.Object);

		mgr.LatencyCancellation.AssertEqual(expected);
	}
}
