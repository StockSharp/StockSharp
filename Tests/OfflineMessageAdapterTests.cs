namespace StockSharp.Tests;

[TestClass]
public class OfflineMessageAdapterTests : BaseTestClass
{
	[TestMethod]
	public async Task SendInMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOfflineManager>();

		manager.SetupProperty(m => m.MaxMessageCount, 10000);

		var toInner = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		var toOut = new SubscriptionResponseMessage { OriginalTransactionId = 1 };

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((toInner: (Message[])[toInner], toOut: (Message[])[toOut], shouldForward: false));

		using var adapter = new OfflineMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(toInner);

		output.Count.AssertEqual(1);
		output[0].AssertSame(toOut);

		manager.Verify(m => m.ProcessInMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task SendInMessage_ShouldForward_ForwardsOriginalMessage()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOfflineManager>();

		manager.SetupProperty(m => m.MaxMessageCount, 10000);

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((toInner: (Message[])[], toOut: (Message[])[], shouldForward: true));

		using var adapter = new OfflineMessageAdapter(inner, manager.Object);

		var connectMsg = new ConnectMessage();
		await adapter.SendInMessageAsync(connectMsg, CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(connectMsg);
	}

	[TestMethod]
	public async Task SendInMessage_MultipleToInner_SendsAll()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOfflineManager>();

		manager.SetupProperty(m => m.MaxMessageCount, 10000);

		var msg1 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		var msg2 = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.OrderLog,
		};

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((toInner: (Message[])[msg1, msg2], toOut: (Message[])[], shouldForward: false));

		using var adapter = new OfflineMessageAdapter(inner, manager.Object);

		await adapter.SendInMessageAsync(new ProcessSuspendedMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(2);
	}

	[TestMethod]
	public void InnerMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOfflineManager>();

		manager.SetupProperty(m => m.MaxMessageCount, 10000);

		var extra = new ProcessSuspendedMessage();

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((suppressOriginal: false, extraOut: (Message[])[extra]));

		using var adapter = new OfflineMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		var connectMsg = new ConnectMessage();
		inner.EmitOut(connectMsg);

		// Original message + extra message
		output.Count.AssertEqual(2);
		output[0].AssertSame(connectMsg);
		output[1].AssertSame(extra);

		manager.Verify(m => m.ProcessOutMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public void InnerMessage_SuppressOriginal_DoesNotForwardOriginal()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOfflineManager>();

		manager.SetupProperty(m => m.MaxMessageCount, 10000);

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((suppressOriginal: true, extraOut: (Message[])[]));

		using var adapter = new OfflineMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new ConnectionLostMessage { IsResetState = true });

		output.Count.AssertEqual(0);
	}

	[TestMethod]
	public void MaxMessageCount_DelegatesToManager()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<IOfflineManager>();

		manager.SetupProperty(m => m.MaxMessageCount, 10000);

		using var adapter = new OfflineMessageAdapter(inner, manager.Object);

		adapter.MaxMessageCount.AssertEqual(10000);

		adapter.MaxMessageCount = 5000;

		manager.VerifySet(m => m.MaxMessageCount = 5000, Times.Once);
	}

	[TestMethod]
	public async Task FullWorkflow_StoreAndReplay()
	{
		var inner = new RecordingMessageAdapter();

		using var adapter = new OfflineMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		var secId = Helper.CreateSecurityId();

		// Send subscription while offline
		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		await adapter.SendInMessageAsync(subscription, CancellationToken);

		// Message should not be forwarded (stored in offline)
		inner.InMessages.Count.AssertEqual(0);

		// Simulate connection success
		inner.EmitOut(new ConnectMessage());

		// Should receive connect message and ProcessSuspended
		output.Any(m => m.Type == MessageTypes.Connect).AssertTrue();
		output.Any(m => m.Type == MessageTypes.ProcessSuspended).AssertTrue();

		// Clear for next check
		output.Clear();
		inner.InMessages.Clear();

		// Now process the suspended message that was sent back
		var processSuspended = new ProcessSuspendedMessage(adapter);
		await adapter.SendInMessageAsync(processSuspended, CancellationToken);

		// Now the stored subscription should be forwarded
		inner.InMessages.Count.AssertEqual(1);
		var forwarded = (MarketDataMessage)inner.InMessages[0];
		forwarded.SecurityId.AssertEqual(secId);
	}

	[TestMethod]
	public async Task OrderCancel_WhileOffline_PendingOrder_ReturnsExecutionMessage()
	{
		var inner = new RecordingMessageAdapter();

		using var adapter = new OfflineMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		// Register order while offline
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
			OrderType = OrderTypes.Limit,
		};

		await adapter.SendInMessageAsync(orderMsg, CancellationToken);

		// Cancel order while still offline
		var cancelMsg = new OrderCancelMessage
		{
			TransactionId = 101,
			OriginalTransactionId = 100,
		};

		await adapter.SendInMessageAsync(cancelMsg, CancellationToken);

		// Should receive an ExecutionMessage indicating cancellation
		output.Count.AssertEqual(1);
		var execMsg = (ExecutionMessage)output[0];
		execMsg.OriginalTransactionId.AssertEqual(101);
		execMsg.OrderState.AssertEqual(OrderStates.Done);
	}

	[TestMethod]
	public void Clone_CreatesNewAdapter()
	{
		var inner = new RecordingMessageAdapter();

		using var adapter = new OfflineMessageAdapter(inner);

		var cloned = adapter.Clone();

		cloned.AssertNotNull();
		cloned.AssertNotSame(adapter);
		(cloned is OfflineMessageAdapter).AssertTrue();
	}

	[TestMethod]
	public void Constructor_NullManager_Throws()
	{
		var inner = new RecordingMessageAdapter();

		var thrown = false;
		try
		{
			new OfflineMessageAdapter(inner, null);
		}
		catch (ArgumentNullException)
		{
			thrown = true;
		}
		thrown.AssertTrue();
	}
}
