namespace StockSharp.Tests;

[TestClass]
public class SubscriptionMessageAdapterTests : BaseTestClass
{
	[TestMethod]
	public async Task SendInMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISubscriptionManager>();

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
			.Returns((toInner: (Message[])[toInner], toOut: (Message[])[toOut]));

		using var adapter = new SubscriptionMessageAdapter(inner, manager.Object);

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
		var manager = new Mock<ISubscriptionManager>();

		var forward = new ConnectMessage();
		var extra = new SubscriptionResponseMessage { OriginalTransactionId = 2 };

		manager
			.Setup(m => m.ProcessOutMessage(It.IsAny<Message>()))
			.Returns((forward: (Message)forward, extraOut: (Message[])[extra]));

		using var adapter = new SubscriptionMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new DisconnectMessage());

		output.Count.AssertEqual(2);
		output[0].AssertSame(forward);
		output[1].AssertSame(extra);

		manager.Verify(m => m.ProcessOutMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task ConnectionRestored_WhenRestoreDisabled_ShouldNotRemap()
	{
		var inner = new RecordingMessageAdapter();

		using var adapter = new SubscriptionMessageAdapter(inner)
		{
			IsRestoreSubscriptionOnErrorReconnect = false,
		};

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		inner.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		inner.EmitOut(new ConnectionRestoredMessage { IsResetState = true });

		output.OfType<ProcessSuspendedMessage>().Any().AssertFalse();
	}
}
