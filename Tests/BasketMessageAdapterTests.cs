namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

/// <summary>
/// General BasketMessageAdapter tests: connection, reset, pending, disconnect.
/// </summary>
[TestClass]
public class BasketMessageAdapterTests : BasketTestBase
{
	#region Connection

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Connect_SingleAdapter_ReturnsConnectMessage()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);

		var (basket, adapter1, _) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			twoAdapters: false);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		connectionState.ConnectedCount.AssertEqual(1);
		connectionState.HasPendingAdapters.AssertFalse();

		adapter1.GetMessages<ConnectMessage>().Any().AssertTrue("Adapter should receive ConnectMessage");

		var connectOuts = GetOut<ConnectMessage>();
		connectOuts.Length.AssertGreater(0, "Basket should emit ConnectMessage");
		connectOuts.First().Error.AssertNull();
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Connect_TwoAdapters_OnFirst_ReturnsAfterFirstConnects()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		connectionManager.ConnectDisconnectEventOnFirstAdapter = true;

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		connectionState.ConnectedCount.AssertEqual(2);

		var connectOuts = GetOut<ConnectMessage>();
		connectOuts.Length.AssertGreater(0, "Basket should emit ConnectMessage");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Connect_TwoAdapters_WaitAll_ReturnsAfterBothConnect()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		connectionManager.ConnectDisconnectEventOnFirstAdapter = false;

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		connectionState.ConnectedCount.AssertEqual(2);
		connectionState.HasPendingAdapters.AssertFalse();

		var connectOuts = GetOut<ConnectMessage>();
		connectOuts.Length.AssertGreater(0, "Basket should emit ConnectMessage after all connected");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Connect_AdapterFails_AllFailed_ReturnsError()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager);

		adapter1.ConnectError = new InvalidOperationException("fail1");
		adapter2.ConnectError = new InvalidOperationException("fail2");

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		connectionState.AllFailed.AssertTrue();
		connectionState.ConnectedCount.AssertEqual(0);

		var connectOuts = GetOut<ConnectMessage>();
		connectOuts.Length.AssertGreater(0);
		connectOuts.Any(c => c.Error != null).AssertTrue("ConnectMessage should contain error");
	}

	#endregion

	#region Reset

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Reset_ClearsAllState()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();

		var (basket, adapter1, _) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			twoAdapters: false);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(1);

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _).AssertTrue();
		ClearOut();

		await SendToBasket(basket, new ResetMessage(), TestContext.CancellationToken);

		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertFalse("Subscription routing should be cleared after reset");

		connectionState.ConnectedCount.AssertEqual(0, "ConnectedCount should be 0 after reset");
	}

	#endregion

	#region Pending Messages

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MessageBeforeConnect_IsPended_ThenProcessedAfterConnect()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var pendingState = new PendingMessageState();

		var (basket, adapter1, _) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			pendingState: pendingState,
			twoAdapters: false);

		adapter1.AutoRespond = false;

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		connectionState.HasPendingAdapters.AssertTrue();

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new SecurityLookupMessage
		{
			TransactionId = transId,
		}, TestContext.CancellationToken);

		pendingState.Count.AssertGreater(0, "Pending state should have messages");

		adapter1.GetMessages<SecurityLookupMessage>().Any()
			.AssertFalse("Adapter should NOT receive SecurityLookupMessage while connecting");

		adapter1.AutoRespond = true;
		adapter1.EmitOut(new ConnectMessage());

		connectionState.ConnectedCount.AssertEqual(1);
		pendingState.Count.AssertEqual(0, "Pending state should be empty after connect");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MessagePended_TwoAdapters_OneFailsOneConnects_ReleasedToLive()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var pendingState = new PendingMessageState();

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			pendingState: pendingState,
			twoAdapters: true);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		connectionState.HasPendingAdapters.AssertTrue("Both adapters should be pending");

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		pendingState.Count.AssertGreater(0, "Message should be pended while adapters connecting");

		adapter1.GetMessages<MarketDataMessage>().Any()
			.AssertFalse("Adapter1 should not receive MarketData while connecting");
		adapter2.GetMessages<MarketDataMessage>().Any()
			.AssertFalse("Adapter2 should not receive MarketData while connecting");

		adapter1.AutoRespond = true;
		adapter1.EmitOut(new ConnectMessage());

		connectionState.HasPendingAdapters.AssertTrue("Adapter2 still connecting");
		pendingState.Count.AssertGreater(0, "Message should still be pended");
		adapter1.GetMessages<MarketDataMessage>().Any()
			.AssertFalse("Adapter1 should not receive MarketData yet");

		adapter2.EmitOut(new ConnectMessage { Error = new Exception("Connection refused") });

		connectionState.HasPendingAdapters.AssertFalse("No more pending");
		pendingState.Count.AssertEqual(0, "Pending should be drained");

		var loopbacks = OutMessages.Where(m => m.IsBack()).ToArray();
		loopbacks.Length.AssertGreater(0, "Should have loopback messages from released pending");

		foreach (var lb in loopbacks)
		{
			lb.BackMode = MessageBackModes.None;
			await SendToBasket(basket, lb, TestContext.CancellationToken);
		}

		adapter1.GetMessages<MarketDataMessage>().Any()
			.AssertTrue("Adapter1 should receive MarketData after pending released");

		adapter2.GetMessages<MarketDataMessage>().Any()
			.AssertFalse("Failed adapter2 should not receive MarketData");
	}

	#endregion

	#region Disconnect

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Disconnect_AllAdaptersDisconnected_ReturnsDisconnectMessage()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(2);
		ClearOut();

		await SendToBasket(basket, new DisconnectMessage(), TestContext.CancellationToken);

		adapter1.GetMessages<DisconnectMessage>().Any().AssertTrue("Adapter1 should receive DisconnectMessage");
		adapter2.GetMessages<DisconnectMessage>().Any().AssertTrue("Adapter2 should receive DisconnectMessage");

		connectionState.AllDisconnectedOrFailed.AssertTrue();

		GetOut<DisconnectMessage>().Length.AssertGreater(0, "Basket should emit DisconnectMessage");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Disconnect_CleansUpAdapters()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();

		var (basket, adapter1, _) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			twoAdapters: false);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(1);

		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);
		ClearOut();

		await SendToBasket(basket, new DisconnectMessage(), TestContext.CancellationToken);

		connectionState.AllDisconnectedOrFailed.AssertTrue();
	}

	#endregion
}
