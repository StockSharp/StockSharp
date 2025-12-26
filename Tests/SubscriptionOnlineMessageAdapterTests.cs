namespace StockSharp.Tests;

[TestClass]
public class SubscriptionOnlineMessageAdapterTests : BaseTestClass
{
	[TestMethod]
	public async Task SendInMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISubscriptionOnlineManager>();

		var toInner = new ConnectMessage();
		var toOut = new SubscriptionResponseMessage { OriginalTransactionId = 10 };

		manager
			.Setup(m => m.ProcessInMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
			.Returns(new ValueTask<(Message[] toInner, Message[] toOut)>((toInner: (Message[])[toInner], toOut: (Message[])[toOut])));

		using var adapter = new SubscriptionOnlineMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(toInner);

		output.Count.AssertEqual(1);
		output[0].AssertSame(toOut);

		manager.Verify(m => m.ProcessInMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public void InnerMessage_DelegatesToManager_AndRoutesMessages()
	{
		var inner = new RecordingMessageAdapter();
		var manager = new Mock<ISubscriptionOnlineManager>();

		var forward = new ConnectMessage();
		var extra = new SubscriptionResponseMessage { OriginalTransactionId = 12 };

		manager
			.Setup(m => m.ProcessOutMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()))
			.Returns(new ValueTask<(Message forward, Message[] extraOut)>((forward: (Message)forward, extraOut: (Message[])[extra])));

		using var adapter = new SubscriptionOnlineMessageAdapter(inner, manager.Object);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

		inner.EmitOut(new DisconnectMessage());

		output.Count.AssertEqual(2);
		output[0].AssertSame(extra);
		output[1].AssertSame(forward);

		manager.Verify(m => m.ProcessOutMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
	}
}
