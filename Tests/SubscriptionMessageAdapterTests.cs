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

	#region End-to-end with real manager

	[TestMethod]
	public async Task RealManager_TickDataFlowsThrough_AfterOnline()
	{
		var inner = new RecordingMessageAdapter();
		using var adapter = new SubscriptionMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

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
		inner.EmitOut(tick);

		output.OfType<ExecutionMessage>().Any(m => m.TradePrice == 50000m).AssertTrue(
			$"Tick should flow through SubscriptionMessageAdapter, got {output.Count} messages: {string.Join(", ", output.Select(m => m.Type))}");
	}

	[TestMethod]
	public async Task RealManager_TickDataDropped_WhenNoSubscription()
	{
		var inner = new RecordingMessageAdapter();
		using var adapter = new SubscriptionMessageAdapter(inner);

		var output = new List<Message>();
		adapter.NewOutMessage += output.Add;

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
		inner.EmitOut(tick);

		output.OfType<ExecutionMessage>().Any().AssertFalse("Tick with unknown subscription should be dropped");
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
		subscrAdapter.NewOutMessage += output.Add;

		var secId = Helper.CreateSecurityId();

		// Subscribe through chain
		await subscrAdapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		inner.InMessages.OfType<MarketDataMessage>().Any(m => m.TransactionId == 100 && m.IsSubscribe).AssertTrue("Subscribe should reach inner adapter");

		// Inner sends Response + Online
		inner.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		inner.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });

		output.OfType<SubscriptionResponseMessage>().Any(m => m.OriginalTransactionId == 100 && m.IsOk()).AssertTrue("Response should flow out");
		output.OfType<SubscriptionOnlineMessage>().Any(m => m.OriginalTransactionId == 100).AssertTrue("Online should flow out");

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
		inner.EmitOut(tick);

		output.OfType<ExecutionMessage>().Any(m => m.TradePrice == 50000m).AssertTrue(
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
		subscrAdapter.NewOutMessage += output.Add;

		var secId = Helper.CreateSecurityId();

		// Subscribe through chain
		await subscrAdapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, CancellationToken);

		inner.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = 100 });
		inner.EmitOut(new SubscriptionOnlineMessage { OriginalTransactionId = 100 });
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
		inner.EmitOut(tick);

		output.OfType<ExecutionMessage>().Any(m => m.TradePrice == 42000m).AssertTrue(
			$"Tick without ID should be resolved by key, got {output.Count} messages: {string.Join(", ", output.Select(m => m.Type))}");
	}

	#endregion
}
