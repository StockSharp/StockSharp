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
	public async Task InnerMessage_DelegatesToManager_AndRoutesMessages()
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

		await inner.SendOutMessageAsync(new DisconnectMessage(), CancellationToken);

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

		inner.InMessages.OfType<MarketDataMessage>().Count(m => m.TransactionId == 100 && m.IsSubscribe).AssertEqual(1, "Subscribe should reach inner");

		// Inner sends back SubscriptionResponse OK
		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, CancellationToken);
		output.OfType<SubscriptionResponseMessage>().Count(m => m.OriginalTransactionId == 100 && m.IsOk()).AssertEqual(1, "Response should flow out");

		// Inner sends back SubscriptionOnline
		await inner.SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, CancellationToken);
		output.OfType<SubscriptionOnlineMessage>().Count(m => m.OriginalTransactionId == 100).AssertEqual(1, "Online should flow out");

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
		await inner.SendOutMessageAsync(tick, CancellationToken);

		output.Count.AssertEqual(1, $"Tick should flow through, got {output.Count} messages");
		var receivedTick = (ExecutionMessage)output[0];
		receivedTick.TradePrice.AssertEqual(50000m);
		receivedTick.GetSubscriptionIds().Count(id => id == 100).AssertEqual(1, "Subscription ID should be set");
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

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, CancellationToken);
		await inner.SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, CancellationToken);
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
		await inner.SendOutMessageAsync(tick, CancellationToken);

		output.Count.AssertEqual(1, $"Tick should flow through by key lookup, got {output.Count}");
		var receivedTick = (ExecutionMessage)output[0];
		receivedTick.TradePrice.AssertEqual(42000m);
		receivedTick.GetSubscriptionIds().Count(id => id == 100).AssertEqual(1, "Manager should assign subscription ID");
	}

	[TestMethod]
	public async Task RealManager_TickDataBeforeOnline_ForwardedAsHistorical()
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

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, CancellationToken);
		output.Clear();

		// Emit tick data before Online — subscription is Active but not Online
		// Active state = historical data phase, data is routed to all Subscribers (not OnlineSubscribers)
		// OnlineSubscribers filter only applies after Online message is received
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 30000m,
			TradeVolume = 3m,
			ServerTime = DateTime.UtcNow,
		};
		await inner.SendOutMessageAsync(tick, CancellationToken);

		// Data in Active state IS forwarded (it's historical data before the live stream starts)
		output.OfType<ExecutionMessage>().Count().AssertEqual(1, "Historical data in Active state should be forwarded");
		var receivedTick = output.OfType<ExecutionMessage>().First();
		receivedTick.TradePrice.AssertEqual(30000m);
		receivedTick.GetSubscriptionIds().Count(id => id == 100).AssertEqual(1, "Should have subscription ID set");
	}

	#endregion
}
