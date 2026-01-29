namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

/// <summary>
/// General BasketMessageAdapter tests: connection, reset, pending, disconnect.
/// White-box: validates all internal state objects at every step.
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
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, _) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting,
			twoAdapters: false);

		// --- Before connect: all state clean ---
		connectionState.ConnectedCount.AssertEqual(0, "ConnectedCount before connect");
		connectionState.HasPendingAdapters.AssertFalse("HasPendingAdapters before connect");
		connectionState.AllFailed.AssertFalse("AllFailed before connect");
		connectionState.AllDisconnectedOrFailed.AssertTrue("AllDisconnectedOrFailed before connect");
		pendingState.Count.AssertEqual(0, "PendingState.Count before connect");

		// --- Send ConnectMessage ---
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// --- After connect: validate all state ---
		connectionState.ConnectedCount.AssertEqual(1, "ConnectedCount after connect");
		connectionState.HasPendingAdapters.AssertFalse("HasPendingAdapters after connect");
		connectionState.AllFailed.AssertFalse("AllFailed after connect");
		connectionState.AllDisconnectedOrFailed.AssertFalse("AllDisconnectedOrFailed after connect");
		connectionState.TryGetAdapterState(adapter1, out var state1, out _).AssertTrue("Adapter1 should have state");
		state1.AssertEqual(ConnectionStates.Connected, "Adapter1 should be Connected");
		pendingState.Count.AssertEqual(0, "PendingState.Count after connect");

		// --- Output validation ---
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
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting);

		// --- Before connect ---
		connectionState.ConnectedCount.AssertEqual(0);
		connectionState.HasPendingAdapters.AssertFalse();
		pendingState.Count.AssertEqual(0);

		// --- Send ConnectMessage ---
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// --- After connect: both connected ---
		connectionState.ConnectedCount.AssertEqual(2, "Both adapters should be connected");
		connectionState.HasPendingAdapters.AssertFalse();
		connectionState.AllFailed.AssertFalse();
		connectionState.TryGetAdapterState(adapter1, out var s1, out _).AssertTrue();
		s1.AssertEqual(ConnectionStates.Connected);
		connectionState.TryGetAdapterState(adapter2, out var s2, out _).AssertTrue();
		s2.AssertEqual(ConnectionStates.Connected);
		pendingState.Count.AssertEqual(0);

		// --- Output ---
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
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// --- After connect ---
		connectionState.ConnectedCount.AssertEqual(2);
		connectionState.HasPendingAdapters.AssertFalse();
		connectionState.AllFailed.AssertFalse();
		connectionState.AllDisconnectedOrFailed.AssertFalse();
		connectionState.TryGetAdapterState(adapter1, out var s1, out _).AssertTrue();
		s1.AssertEqual(ConnectionStates.Connected);
		connectionState.TryGetAdapterState(adapter2, out var s2, out _).AssertTrue();
		s2.AssertEqual(ConnectionStates.Connected);
		pendingState.Count.AssertEqual(0);

		var connectOuts = GetOut<ConnectMessage>();
		connectOuts.Length.AssertGreater(0, "Basket should emit ConnectMessage after all connected");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Connect_AdapterFails_AllFailed_ReturnsError()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting);

		adapter1.ConnectError = new InvalidOperationException("fail1");
		adapter2.ConnectError = new InvalidOperationException("fail2");

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// --- After connect with errors ---
		connectionState.AllFailed.AssertTrue("AllFailed should be true");
		connectionState.ConnectedCount.AssertEqual(0, "No adapters connected");
		connectionState.HasPendingAdapters.AssertFalse("No pending adapters");
		connectionState.AllDisconnectedOrFailed.AssertTrue("AllDisconnectedOrFailed should be true");
		connectionState.TryGetAdapterState(adapter1, out var s1, out _).AssertTrue();
		s1.AssertEqual(ConnectionStates.Failed, "Adapter1 should be Failed");
		connectionState.TryGetAdapterState(adapter2, out var s2, out _).AssertTrue();
		s2.AssertEqual(ConnectionStates.Failed, "Adapter2 should be Failed");
		pendingState.Count.AssertEqual(0);

		// --- Output ---
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
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, _) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting,
			twoAdapters: false);

		// --- Connect ---
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(1);
		connectionState.TryGetAdapterState(adapter1, out var cs1, out _).AssertTrue();
		cs1.AssertEqual(ConnectionStates.Connected);

		// --- Subscribe to create routing state ---
		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		// --- Validate state after subscription ---
		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _).AssertTrue("Subscription should exist");
		connectionState.ConnectedCount.AssertEqual(1);
		pendingState.Count.AssertEqual(0);

		ClearOut();

		// --- Reset ---
		await SendToBasket(basket, new ResetMessage(), TestContext.CancellationToken);

		// --- After reset: ALL state should be cleared ---
		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertFalse("Subscription routing should be cleared after reset");
		connectionState.ConnectedCount.AssertEqual(0, "ConnectedCount should be 0 after reset");
		connectionState.HasPendingAdapters.AssertFalse("HasPendingAdapters should be false after reset");
		pendingState.Count.AssertEqual(0, "PendingState should be empty after reset");
	}

	#endregion

	#region Pending Messages

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MessageBeforeConnect_IsPended_ThenProcessedAfterConnect()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, _) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting,
			twoAdapters: false);

		adapter1.AutoRespond = false;

		// --- Send ConnectMessage (adapter will NOT auto-respond) ---
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// --- After connect sent but no response: adapter is pending ---
		connectionState.HasPendingAdapters.AssertTrue("Adapter should be pending (connecting)");
		connectionState.ConnectedCount.AssertEqual(0, "No adapters connected yet");
		connectionState.TryGetAdapterState(adapter1, out var state, out _).AssertTrue();
		state.AssertEqual(ConnectionStates.Connecting, "Adapter1 should be Connecting");
		pendingState.Count.AssertEqual(0, "No pending messages yet");

		// --- Send SecurityLookup while connecting ---
		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new SecurityLookupMessage
		{
			TransactionId = transId,
		}, TestContext.CancellationToken);

		// --- After sending message during connecting: message should be pended ---
		pendingState.Count.AssertGreater(0, "Pending state should have messages");
		connectionState.HasPendingAdapters.AssertTrue("Still pending");
		connectionState.ConnectedCount.AssertEqual(0, "Still not connected");

		adapter1.GetMessages<SecurityLookupMessage>().Any()
			.AssertFalse("Adapter should NOT receive SecurityLookupMessage while connecting");

		// subscriptionRouting should NOT have the subscription yet (it's pended)
		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertFalse("Pended message should not create subscription routing");

		// --- Now adapter connects ---
		adapter1.AutoRespond = true;
		await adapter1.SendOutMessageAsync(new ConnectMessage(), TestContext.CancellationToken);

		// --- After adapter connected: pending should be drained ---
		connectionState.ConnectedCount.AssertEqual(1, "Adapter now connected");
		connectionState.HasPendingAdapters.AssertFalse("No more pending adapters");
		connectionState.TryGetAdapterState(adapter1, out var state2, out _).AssertTrue();
		state2.AssertEqual(ConnectionStates.Connected, "Adapter1 should be Connected");
		pendingState.Count.AssertEqual(0, "Pending state should be empty after connect");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MessagePended_TwoAdapters_OneFailsOneConnects_ReleasedToLive()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting,
			twoAdapters: true);

		adapter1.AutoRespond = false;
		adapter2.AutoRespond = false;

		// --- Send ConnectMessage ---
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// --- Both adapters pending ---
		connectionState.HasPendingAdapters.AssertTrue("Both adapters should be pending");
		connectionState.ConnectedCount.AssertEqual(0);
		connectionState.TryGetAdapterState(adapter1, out var sa1, out _).AssertTrue();
		sa1.AssertEqual(ConnectionStates.Connecting);
		connectionState.TryGetAdapterState(adapter2, out var sa2, out _).AssertTrue();
		sa2.AssertEqual(ConnectionStates.Connecting);
		pendingState.Count.AssertEqual(0);

		// --- Send MarketData while both connecting ---
		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// --- Message should be pended ---
		pendingState.Count.AssertGreater(0, "Message should be pended while adapters connecting");
		connectionState.HasPendingAdapters.AssertTrue("Still pending");
		connectionState.ConnectedCount.AssertEqual(0);

		adapter1.GetMessages<MarketDataMessage>().Any()
			.AssertFalse("Adapter1 should not receive MarketData while connecting");
		adapter2.GetMessages<MarketDataMessage>().Any()
			.AssertFalse("Adapter2 should not receive MarketData while connecting");

		// --- Adapter1 connects ---
		adapter1.AutoRespond = true;
		await adapter1.SendOutMessageAsync(new ConnectMessage(), TestContext.CancellationToken);

		connectionState.ConnectedCount.AssertEqual(1, "Adapter1 connected");
		connectionState.HasPendingAdapters.AssertTrue("Adapter2 still connecting");
		connectionState.TryGetAdapterState(adapter1, out var sa1b, out _).AssertTrue();
		sa1b.AssertEqual(ConnectionStates.Connected);
		connectionState.TryGetAdapterState(adapter2, out var sa2b, out _).AssertTrue();
		sa2b.AssertEqual(ConnectionStates.Connecting);
		pendingState.Count.AssertGreater(0, "Message should still be pended");

		adapter1.GetMessages<MarketDataMessage>().Any()
			.AssertFalse("Adapter1 should not receive MarketData yet");

		// --- Adapter2 fails ---
		await adapter2.SendOutMessageAsync(new ConnectMessage { Error = new Exception("Connection refused") }, TestContext.CancellationToken);

		connectionState.HasPendingAdapters.AssertFalse("No more pending");
		connectionState.ConnectedCount.AssertEqual(1);
		connectionState.TryGetAdapterState(adapter2, out var sa2c, out _).AssertTrue();
		sa2c.AssertEqual(ConnectionStates.Failed, "Adapter2 should be Failed");
		pendingState.Count.AssertEqual(0, "Pending should be drained");

		// --- Process loopback messages ---
		var loopbacks = OutMessages.Where(m => m.IsBack()).ToArray();
		loopbacks.Length.AssertGreater(0, "Should have loopback messages from released pending");

		foreach (var lb in loopbacks)
		{
			lb.BackMode = MessageBackModes.None;
			await SendToBasket(basket, lb, TestContext.CancellationToken);
		}

		// --- After loopback processed: adapter1 should receive MarketData ---
		adapter1.GetMessages<MarketDataMessage>().Any()
			.AssertTrue("Adapter1 should receive MarketData after pending released");
		adapter2.GetMessages<MarketDataMessage>().Any()
			.AssertFalse("Failed adapter2 should not receive MarketData");

		// --- Validate subscription routing after processing ---
		// The subscription should now be routed through adapter1
		subscriptionRouting.TryGetSubscription(transId, out _, out var adapters, out _)
			.AssertTrue("Subscription routing should have entry for transId");
	}

	#endregion

	#region Disconnect

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Disconnect_AllAdaptersDisconnected_ReturnsDisconnectMessage()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting);

		// --- Connect ---
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(2);
		connectionState.TryGetAdapterState(adapter1, out var cs1, out _).AssertTrue();
		cs1.AssertEqual(ConnectionStates.Connected);
		connectionState.TryGetAdapterState(adapter2, out var cs2, out _).AssertTrue();
		cs2.AssertEqual(ConnectionStates.Connected);
		ClearOut();

		// --- Disconnect ---
		await SendToBasket(basket, new DisconnectMessage(), TestContext.CancellationToken);

		// --- After disconnect: validate all state ---
		connectionState.AllDisconnectedOrFailed.AssertTrue("All should be disconnected");
		connectionState.ConnectedCount.AssertEqual(0, "No connected adapters");
		connectionState.HasPendingAdapters.AssertFalse();
		connectionState.TryGetAdapterState(adapter1, out var ds1, out _).AssertTrue();
		ds1.AssertEqual(ConnectionStates.Disconnected, "Adapter1 should be Disconnected");
		connectionState.TryGetAdapterState(adapter2, out var ds2, out _).AssertTrue();
		ds2.AssertEqual(ConnectionStates.Disconnected, "Adapter2 should be Disconnected");
		pendingState.Count.AssertEqual(0);

		// --- Output ---
		adapter1.GetMessages<DisconnectMessage>().Any().AssertTrue("Adapter1 should receive DisconnectMessage");
		adapter2.GetMessages<DisconnectMessage>().Any().AssertTrue("Adapter2 should receive DisconnectMessage");
		GetOut<DisconnectMessage>().Length.AssertGreater(0, "Basket should emit DisconnectMessage");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Disconnect_CleansUpAdapters()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, _) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState,
			orderRouting: orderRouting,
			twoAdapters: false);

		// --- Connect ---
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(1);
		connectionState.TryGetAdapterState(adapter1, out var cs, out _).AssertTrue();
		cs.AssertEqual(ConnectionStates.Connected);

		// --- Subscribe ---
		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = SecId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertTrue("Subscription should exist before disconnect");

		ClearOut();

		// --- Disconnect ---
		await SendToBasket(basket, new DisconnectMessage(), TestContext.CancellationToken);

		// --- After disconnect ---
		connectionState.AllDisconnectedOrFailed.AssertTrue();
		connectionState.ConnectedCount.AssertEqual(0);
		connectionState.TryGetAdapterState(adapter1, out var ds, out _).AssertTrue();
		ds.AssertEqual(ConnectionStates.Disconnected);
		pendingState.Count.AssertEqual(0);
	}

	#endregion
}
