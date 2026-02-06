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
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		inner.InMessages.Count.AssertEqual(1);
		inner.InMessages[0].AssertSame(toInner);

		output.Count.AssertEqual(1);
		output[0].AssertSame(toOut);

		manager.Verify(m => m.ProcessInMessage(It.IsAny<Message>()), Times.Once);
	}

	[TestMethod]
	public async Task InnerMessage_DelegatesToManager_AndRoutesMessages()
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
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await inner.SendOutMessageAsync(new DisconnectMessage(), CancellationToken);

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
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, CancellationToken);
		await inner.SendOutMessageAsync(new ConnectionRestoredMessage { IsResetState = true }, CancellationToken);

		output.OfType<ProcessSuspendedMessage>().Count().AssertEqual(0);
	}

	#region End-to-end with real manager

	[TestMethod]
	public async Task RealManager_TickDataFlowsThrough_AfterOnline()
	{
		var inner = new RecordingMessageAdapter();
		using var adapter = new SubscriptionMessageAdapter(inner);

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

		// Inner emits tick data with subscription ID set
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

		output.OfType<ExecutionMessage>().Count(m => m.TradePrice == 50000m).AssertEqual(1,
			$"Tick should flow through SubscriptionMessageAdapter, got {output.Count} messages: {string.Join(", ", output.Select(m => m.Type))}");
	}

	[TestMethod]
	public async Task RealManager_TickDataDropped_WhenNoSubscription()
	{
		var inner = new RecordingMessageAdapter();
		using var adapter = new SubscriptionMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		// Emit tick data WITHOUT any subscription registered
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = Helper.CreateSecurityId(),
			TradePrice = 50000m,
			TradeVolume = 1m,
			ServerTime = DateTime.UtcNow,
		};
		tick.SetSubscriptionIds(subscriptionId: 999);
		await inner.SendOutMessageAsync(tick, CancellationToken);

		output.OfType<ExecutionMessage>().Count().AssertEqual(0, "Tick with unknown subscription should be dropped");
	}

	#endregion

	#region Combined adapter chain

	[TestMethod]
	public async Task CombinedChain_TickDataFlowsThrough_SubscriptionOnline_ThenSubscription()
	{
		// Tests the chain: inner → SubscriptionOnlineMessageAdapter → SubscriptionMessageAdapter
		var inner = new RecordingMessageAdapter();
		var onlineAdapter = new SubscriptionOnlineMessageAdapter(inner);
		using var subscrAdapter = new SubscriptionMessageAdapter(onlineAdapter);

		var output = new List<Message>();
		subscrAdapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var secId = Helper.CreateSecurityId();

		// Subscribe through chain
		await subscrAdapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		inner.InMessages.OfType<MarketDataMessage>().Count(m => m.TransactionId == 100 && m.IsSubscribe).AssertEqual(1, "Subscribe should reach inner adapter");

		// Inner sends Response + Online
		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, CancellationToken);
		await inner.SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, CancellationToken);

		output.OfType<SubscriptionResponseMessage>().Count(m => m.OriginalTransactionId == 100 && m.IsOk()).AssertEqual(1, "Response should flow out");
		output.OfType<SubscriptionOnlineMessage>().Count(m => m.OriginalTransactionId == 100).AssertEqual(1, "Online should flow out");

		output.Clear();

		// Emit tick (with subscription ID)
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

		output.OfType<ExecutionMessage>().Count(m => m.TradePrice == 50000m).AssertEqual(1,
			$"Tick should flow through combined chain, got {output.Count} messages: {string.Join(", ", output.Select(m => m.Type))}");
	}

	[TestMethod]
	public async Task CombinedChain_TickDataWithoutId_FoundByKey()
	{
		// Same chain but tick has no subscription ID — SubscriptionOnlineManager must find by (DataType, SecurityId)
		var inner = new RecordingMessageAdapter();
		var onlineAdapter = new SubscriptionOnlineMessageAdapter(inner);
		using var subscrAdapter = new SubscriptionMessageAdapter(onlineAdapter);

		var output = new List<Message>();
		subscrAdapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		var secId = Helper.CreateSecurityId();

		// Subscribe through chain
		await subscrAdapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		await inner.SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = 100 }, CancellationToken);
		await inner.SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = 100 }, CancellationToken);
		output.Clear();

		// Emit tick WITHOUT subscription IDs
		var tick = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 42000m,
			TradeVolume = 2m,
			ServerTime = DateTime.UtcNow,
		};
		await inner.SendOutMessageAsync(tick, CancellationToken);

		output.OfType<ExecutionMessage>().Count(m => m.TradePrice == 42000m).AssertEqual(1,
			$"Tick without ID should be resolved by key, got {output.Count} messages: {string.Join(", ", output.Select(m => m.Type))}");
	}

	#endregion
}
