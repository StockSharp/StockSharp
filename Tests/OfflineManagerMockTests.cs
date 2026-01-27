namespace StockSharp.Tests;

[TestClass]
public class OfflineManagerMockTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver { }

	private static Mock<IOfflineManagerState> CreateStateMock(bool isConnected = false)
	{
		var mock = new Mock<IOfflineManagerState>(MockBehavior.Loose);
		mock.Setup(s => s.IsConnected).Returns(isConnected);
		return mock;
	}

	private static OfflineManager CreateManager(Mock<IOfflineManagerState> stateMock, Func<Message> createProcessSuspended = null)
	{
		return new OfflineManager(
			new TestReceiver(),
			createProcessSuspended ?? (() => new ProcessSuspendedMessage()),
			stateMock.Object);
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var stateMock = CreateStateMock();
		var manager = CreateManager(stateMock);

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new ResetMessage());

		stateMock.Verify(s => s.Clear(), Times.Once());
		shouldForward.AssertTrue();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Connect_WhenDisconnected_StoresOrder()
	{
		var stateMock = CreateStateMock(isConnected: false);
		var manager = CreateManager(stateMock);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
			Price = 100m,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(orderMsg);

		stateMock.Verify(s => s.AddPendingRegistration(1, It.IsAny<OrderRegisterMessage>()), Times.Once());
		stateMock.Verify(s => s.AddSuspended(It.IsAny<Message>()), Times.Once());
		shouldForward.AssertFalse();
	}

	[TestMethod]
	public void Connect_WhenConnected_ForwardsOrder()
	{
		var stateMock = CreateStateMock(isConnected: true);
		var manager = CreateManager(stateMock);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			PortfolioName = "pf",
			Side = Sides.Buy,
			Volume = 10,
			Price = 100m,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(orderMsg);

		stateMock.Verify(s => s.AddPendingRegistration(It.IsAny<long>(), It.IsAny<OrderRegisterMessage>()), Times.Never());
		stateMock.Verify(s => s.AddSuspended(It.IsAny<Message>()), Times.Never());
		shouldForward.AssertTrue();
	}

	[TestMethod]
	public void ProcessOutMessage_Connect_SetsConnected()
	{
		var stateMock = CreateStateMock();
		var manager = CreateManager(stateMock);

		var connectMsg = new ConnectMessage();
		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(connectMsg);

		stateMock.Verify(s => s.SetConnected(true), Times.Once());
		suppressOriginal.AssertFalse();
		extraOut.Length.AssertEqual(1);
		extraOut[0].Type.AssertEqual(MessageTypes.ProcessSuspended);
	}

	[TestMethod]
	public void ProcessOutMessage_ConnectWithError_DoesNotSetConnected()
	{
		var stateMock = CreateStateMock();
		var manager = CreateManager(stateMock);

		var connectMsg = new ConnectMessage { Error = new InvalidOperationException("fail") };
		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(connectMsg);

		stateMock.Verify(s => s.SetConnected(It.IsAny<bool>()), Times.Never());
		suppressOriginal.AssertFalse();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_Disconnect_SetsDisconnected()
	{
		var stateMock = CreateStateMock();
		var manager = CreateManager(stateMock);

		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(new DisconnectMessage());

		stateMock.Verify(s => s.SetConnected(false), Times.Once());
		suppressOriginal.AssertFalse();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessSuspended_GetsAndClearsSuspended()
	{
		var stateMock = CreateStateMock();
		var suspendedMessages = new Message[]
		{
			new OrderRegisterMessage { TransactionId = 1 },
			new OrderRegisterMessage { TransactionId = 2 },
		};
		stateMock.Setup(s => s.GetAndClearSuspended()).Returns(suspendedMessages);
		var manager = CreateManager(stateMock);

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new ProcessSuspendedMessage());

		stateMock.Verify(s => s.GetAndClearSuspended(), Times.Once());
		stateMock.Verify(s => s.RemovePendingRegistrationByValue(It.IsAny<OrderRegisterMessage>()), Times.Exactly(2));
		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(2);
		toOut.Length.AssertEqual(0);
	}
}
