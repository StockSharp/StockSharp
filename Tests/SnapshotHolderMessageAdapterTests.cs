namespace StockSharp.Tests;

[TestClass]
public class SnapshotHolderMessageAdapterTests : BaseTestClass
{
	private sealed class TestSnapshotHolder : ISnapshotHolder
	{
		public IEnumerable<Message> GetSnapshot(ISubscriptionMessage subscription)
		{
			return [];
		}
	}

	[TestMethod]
	public async Task SendInMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var holder = new TestSnapshotHolder();
		var manager = new Mock<ISnapshotHolderManager>();

		var toInner = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Level1,
		};

		var toOut = new SubscriptionResponseMessage { OriginalTransactionId = 1 };

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((toInner: (Message[])[toInner], toOut: (Message[])[toOut]));

		using var adapter = new SnapshotHolderMessageAdapter(inner, holder, manager.Object);

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
	public void InnerMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var holder = new TestSnapshotHolder();
		var manager = new Mock<ISnapshotHolderManager>();

		var forward = new ConnectMessage();
		var extra = new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId() };

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)forward, extraOut: (Message[])[extra]));

		using var adapter = new SnapshotHolderMessageAdapter(inner, holder, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new DisconnectMessage());

		output.Count.AssertEqual(2);
		output[0].AssertSame(forward);
		output[1].AssertSame(extra);

		manager.Verify(m => m.ProcessOutMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task SendInMessage_WithMultipleToInner_SendsAll()
	{
		var inner = new RecordingMessageAdapter();
		var holder = new TestSnapshotHolder();
		var manager = new Mock<ISnapshotHolderManager>();

		var msg1 = new MarketDataMessage { TransactionId = 1, SecurityId = Helper.CreateSecurityId(), DataType2 = DataType.Level1 };
		var msg2 = new MarketDataMessage { TransactionId = 2, SecurityId = Helper.CreateSecurityId(), DataType2 = DataType.Level1 };

		manager
			.Setup(m => m.ProcessInMessage(It.IsAny<Message>()))
			.Returns((toInner: (Message[])[msg1, msg2], toOut: (Message[])[]));

		using var adapter = new SnapshotHolderMessageAdapter(inner, holder, manager.Object);

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(2);
		inner.InMessages[0].AssertSame(msg1);
		inner.InMessages[1].AssertSame(msg2);
	}

	[TestMethod]
	public void OutMessage_ForwardNull_DoesNotForward()
	{
		var inner = new RecordingMessageAdapter();
		var holder = new TestSnapshotHolder();
		var manager = new Mock<ISnapshotHolderManager>();

		var extra = new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId() };

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)null, extraOut: (Message[])[extra]));

		using var adapter = new SnapshotHolderMessageAdapter(inner, holder, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new DisconnectMessage());

		output.Count.AssertEqual(1);
		output[0].AssertSame(extra);
	}

	[TestMethod]
	public void Clone_CreatesNewAdapter()
	{
		var inner = new RecordingMessageAdapter();
		var holder = new TestSnapshotHolder();

		using var adapter = new SnapshotHolderMessageAdapter(inner, holder);

		var clone = adapter.Clone();

		clone.AssertNotNull();
		clone.AssertNotSame(adapter);
		(clone is SnapshotHolderMessageAdapter).AssertTrue();
	}
}
