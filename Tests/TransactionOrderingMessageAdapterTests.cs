namespace StockSharp.Tests;

[TestClass]
public class TransactionOrderingMessageAdapterTests : BaseTestClass
{
	[TestMethod]
	public async Task SendInMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ITransactionOrderingManager>();

		var toInner = new OrderRegisterMessage
		{
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			Price = 100m,
			Volume = 10m,
		};

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((toInner: (Message[])[toInner], toOut: (Message[])[]));

		using var adapter = new TransactionOrderingMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(toInner);

		output.Count.AssertEqual(0);

		manager.Verify(m => m.ProcessInMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task SendInMessage_WithOutputMessages_RaisesOutMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ITransactionOrderingManager>();

		var toOut = new SubscriptionResponseMessage { OriginalTransactionId = 1 };

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((toInner: (Message[])[], toOut: (Message[])[toOut]));

		using var adapter = new TransactionOrderingMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(0);
		output.Count.AssertEqual(1);
		output[0].AssertSame(toOut);
	}

	[TestMethod]
	public void InnerMessage_DelegatesToManager_AndForwardsMessage()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ITransactionOrderingManager>();

		var forward = new SubscriptionResponseMessage { OriginalTransactionId = 1 };

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)forward, extraOut: (Message[])[], processSuspended: false));

		using var adapter = new TransactionOrderingMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new ResetMessage());

		output.Count.AssertEqual(1);
		output[0].AssertSame(forward);

		manager.Verify(m => m.ProcessOutMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public void InnerMessage_WithExtraOut_ForwardsAllMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ITransactionOrderingManager>();

		var forward = new SubscriptionResponseMessage { OriginalTransactionId = 1 };
		var extra1 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			HasOrderInfo = false,
			TradeId = 123,
		};
		var extra2 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			HasOrderInfo = false,
			TradeId = 456,
		};

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)forward, extraOut: (Message[])[extra1, extra2], processSuspended: false));

		manager
			.Setup(m => m.GetSuspendedTrades(It.IsAny<ExecutionMessage>()))
			.Returns((Message[])[]);

		using var adapter = new TransactionOrderingMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });

		output.Count.AssertEqual(3);
		output[0].AssertSame(forward);
		output[1].AssertSame(extra1);
		output[2].AssertSame(extra2);
	}

	[TestMethod]
	public void InnerMessage_WhenProcessSuspended_CallsGetSuspendedTrades()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ITransactionOrderingManager>();

		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			HasOrderInfo = true,
			OrderId = 12345,
			TransactionId = 100,
		};

		var suspendedTrade = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			TradeId = 555,
		};

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)orderMsg, extraOut: (Message[])[], processSuspended: true));

		manager
			.Setup(m => m.GetSuspendedTrades(It.IsAny<ExecutionMessage>()))
			.Returns((Message[])[suspendedTrade]);

		using var adapter = new TransactionOrderingMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(orderMsg);

		manager.Verify(m => m.GetSuspendedTrades(It.IsAny<ExecutionMessage>()), Times.Once);
		output.Count.AssertEqual(2);
		output[0].AssertSame(orderMsg);
		output[1].AssertSame(suspendedTrade);
	}

	[TestMethod]
	public void InnerMessage_WhenForwardIsNull_DoesNotForward()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ITransactionOrderingManager>();

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)null, extraOut: (Message[])[], processSuspended: false));

		using var adapter = new TransactionOrderingMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			TradeId = 1,
		});

		output.Count.AssertEqual(0);
	}

	[TestMethod]
	public void InnerMessage_ExtraOut_WithOrderInfo_ProcessesSuspended()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ITransactionOrderingManager>();

		var forward = new SubscriptionOnlineMessage { OriginalTransactionId = 100 };

		var orderFromTransactionLog = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			HasOrderInfo = true,
			OrderId = 12345,
		};

		var suspendedTrade = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = Helper.CreateSecurityId(),
			TradeId = 999,
		};

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)forward, extraOut: (Message[])[orderFromTransactionLog], processSuspended: false));

		manager
			.Setup(m => m.GetSuspendedTrades(It.Is<ExecutionMessage>(e => e.OrderId == 12345)))
			.Returns((Message[])[suspendedTrade]);

		using var adapter = new TransactionOrderingMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });

		output.Count.AssertEqual(3);
		output[0].AssertSame(forward);
		output[1].AssertSame(orderFromTransactionLog);
		output[2].AssertSame(suspendedTrade);

		manager.Verify(m => m.GetSuspendedTrades(It.IsAny<ExecutionMessage>()), Times.Once);
	}

	[TestMethod]
	public async Task Clone_CreatesNewAdapter()
	{
		var inner = new RecordingMessageAdapter();

		using var adapter = new TransactionOrderingMessageAdapter(inner);

		var clone = adapter.Clone();

		clone.AssertNotNull();
		(clone is TransactionOrderingMessageAdapter).AssertTrue();
		clone.AssertNotSame(adapter);
	}

	[TestMethod]
	public async Task DefaultConstructor_CreatesManagerInternally()
	{
		var inner = new RecordingMessageAdapter();

		using var adapter = new TransactionOrderingMessageAdapter(inner);

		// Just verify it works without throwing
		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);
	}

	[TestMethod]
	public void Constructor_WithNullManager_ThrowsArgumentNullException()
	{
		var inner = new RecordingMessageAdapter();

		ThrowsExactly<ArgumentNullException>(() =>
		{
			new TransactionOrderingMessageAdapter(inner, null);
		});
	}
}
