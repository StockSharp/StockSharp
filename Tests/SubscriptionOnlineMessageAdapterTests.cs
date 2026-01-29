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
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

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
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		inner.EmitOut(new DisconnectMessage());

		output.Count.AssertEqual(2);
		output[0].AssertSame(extra);
		output[1].AssertSame(forward);

		manager.Verify(m => m.ProcessOutMessageAsync(It.IsAny<Message>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	#region End-to-end with real manager

	[TestMethod]
	public async Task RealManager_TickDataFlowsThrough_AfterOnline()
	{
		var inner = new RecordingMessageAdapter();
		using var adapter = new SubscriptionOnlineMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var secId = Helper.CreateSecurityId();

		// Subscribe
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		inner.InMessages.OfType<MarketDataMessage>().Any(m => m.TransactionId == 100 && m.IsSubscribe).AssertTrue("Subscribe should reach inner");

		// Inner sends back SubscriptionResponse OK
		inner.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		output.OfType<SubscriptionResponseMessage>().Any(m => m.OriginalTransactionId == 100 && m.IsOk()).AssertTrue("Response should flow out");

		// Inner sends back SubscriptionOnline
		inner.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });
		output.OfType<SubscriptionOnlineMessage>().Any(m => m.OriginalTransactionId == 100).AssertTrue("Online should flow out");

		output.Clear();

		// Inner emits tick data (as if from adapter)
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 50000m,
			TradeVolume = 1m,
			ServerTime = DateTime.UtcNow,
		};
		tick.SetSubscriptionIds(subscriptionId: 100);
		inner.EmitOut(tick);

		output.Count.AssertEqual(1, $"Tick should flow through, got {output.Count} messages");
		var receivedTick = (ExecutionMessage)output[0];
		receivedTick.TradePrice.AssertEqual(50000m);
		receivedTick.GetSubscriptionIds().Contains(100).AssertTrue("Subscription ID should be set");
	}

	[TestMethod]
	public async Task RealManager_TickDataWithoutSubscriptionId_FoundByKey()
	{
		var inner = new RecordingMessageAdapter();
		using var adapter = new SubscriptionOnlineMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var secId = Helper.CreateSecurityId();

		// Subscribe + Response + Online
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		inner.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		inner.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });
		output.Clear();

		// Inner emits tick data WITHOUT subscription IDs set (OriginalTransactionId = 0)
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 42000m,
			TradeVolume = 2m,
			ServerTime = DateTime.UtcNow,
		};
		// No SetSubscriptionIds call — manager must find by (DataType, SecurityId) key
		inner.EmitOut(tick);

		output.Count.AssertEqual(1, $"Tick should flow through by key lookup, got {output.Count}");
		var receivedTick = (ExecutionMessage)output[0];
		receivedTick.TradePrice.AssertEqual(42000m);
		receivedTick.GetSubscriptionIds().Contains(100).AssertTrue("Manager should assign subscription ID");
	}

	[TestMethod]
	public async Task RealManager_TickDataBeforeOnline_DroppedByDesign()
	{
		var inner = new RecordingMessageAdapter();
		using var adapter = new SubscriptionOnlineMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var secId = Helper.CreateSecurityId();

		// Subscribe + Response OK (state = Active, but NOT Online yet)
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		inner.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		output.Clear();

		// Emit tick data before Online — subscription is Active but not Online
		// SubscriptionOnlineManager only routes data to OnlineSubscribers, so
		// data for Active-but-not-yet-Online subscriptions is dropped by design
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 30000m,
			TradeVolume = 3m,
			ServerTime = DateTime.UtcNow,
		};
		// No subscription IDs — lookup by key finds the subscription but OnlineSubscribers is empty
		inner.EmitOut(tick);

		output.OfType<ExecutionMessage>().Any().AssertFalse("Data before Online should be dropped (OnlineSubscribers is empty)");
	}

	#endregion
}
