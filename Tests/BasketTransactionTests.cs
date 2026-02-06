namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

/// <summary>
/// Order routing tests for BasketMessageAdapter.
/// White-box: validates all internal state objects at every step.
/// </summary>
[TestClass]
public class BasketTransactionTests : BasketTestBase
{
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderRegister_RoutesToCorrectAdapter_ByPortfolioProvider()
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
		pendingState.Count.AssertEqual(0);

		ClearOut();

		// --- Set portfolio→adapter mapping ---
		basket.PortfolioAdapterProvider.SetAdapter(Portfolio1, adapter1);

		// --- Register order ---
		var transId = basket.TransactionIdGenerator.GetNextId();
		var regMsg = new OrderRegisterMessage
		{
			SecurityId = SecId1,
			PortfolioName = Portfolio1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			TransactionId = transId,
		};
		await SendToBasket(basket, regMsg, TestContext.CancellationToken);

		// --- After order register: validate state ---
		connectionState.ConnectedCount.AssertEqual(2, "Connection state unchanged");
		pendingState.Count.AssertEqual(0, "No pending messages");

		// orderRouting should have the mapping
		orderRouting.TryGetOrderAdapter(transId, out var routedAdapter)
			.AssertTrue("OrderRouting should have transId→adapter mapping");
		routedAdapter.AssertEqual(adapter1, "Order should be routed to adapter1");

		// subscriptionRouting should NOT have order transId (orders are not subscriptions)
		// parentChildMap should be empty for orders

		// --- Output ---
		adapter1.GetMessages<OrderRegisterMessage>().Any().AssertTrue("Adapter1 should receive OrderRegisterMessage");
		adapter2.GetMessages<OrderRegisterMessage>().Any().AssertFalse("Adapter2 should NOT receive OrderRegisterMessage");

		var execMsgs = GetOut<ExecutionMessage>();
		execMsgs.Any(e => e.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit ExecutionMessage for the order");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderCancel_RoutesToSameAdapter_AsOriginalOrder()
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

		// --- Register order ---
		basket.PortfolioAdapterProvider.SetAdapter(Portfolio1, adapter1);

		var regTransId = basket.TransactionIdGenerator.GetNextId();
		var regMsg = new OrderRegisterMessage
		{
			SecurityId = SecId1,
			PortfolioName = Portfolio1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			TransactionId = regTransId,
		};
		await SendToBasket(basket, regMsg, TestContext.CancellationToken);

		// --- Validate after register ---
		orderRouting.TryGetOrderAdapter(regTransId, out var routedAdapter).AssertTrue();
		routedAdapter.AssertEqual(adapter1);
		pendingState.Count.AssertEqual(0);

		ClearOut();

		// --- Cancel order ---
		var cancelTransId = basket.TransactionIdGenerator.GetNextId();
		var cancelMsg = new OrderCancelMessage
		{
			SecurityId = SecId1,
			PortfolioName = Portfolio1,
			TransactionId = cancelTransId,
			OriginalTransactionId = regTransId,
		};
		await SendToBasket(basket, cancelMsg, TestContext.CancellationToken);

		// --- After cancel: validate state ---
		connectionState.ConnectedCount.AssertEqual(2, "Connection state unchanged after cancel");
		pendingState.Count.AssertEqual(0);

		// orderRouting should still have the original order mapping
		orderRouting.TryGetOrderAdapter(regTransId, out var stillRouted).AssertTrue("Original order mapping preserved");
		stillRouted.AssertEqual(adapter1);

		// --- Output ---
		adapter1.GetMessages<OrderCancelMessage>().Any().AssertTrue("Adapter1 should receive OrderCancelMessage");
		adapter2.GetMessages<OrderCancelMessage>().Any().AssertFalse("Adapter2 should NOT receive OrderCancelMessage");

		GetOut<ExecutionMessage>().Any(e => e.OriginalTransactionId == cancelTransId && e.OrderState == OrderStates.Done)
			.AssertTrue("Basket should emit ExecutionMessage with Done state");
	}

	/// <summary>
	/// BUG: OrderCancel portfolio fallback sets adapter to wrapper, then _adapterWrappers.TryGetValue(wrapper)
	/// fails because _adapterWrappers maps underlying→wrapper, not wrapper→wrapper.
	/// Result: UnknownTransactionId error even though the portfolio is correctly mapped.
	/// NOTE: IgnoreExtraAdapters must be false so wrapper != underlying to trigger the bug.
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderCancel_PortfolioFallback_ShouldRouteCorrectly()
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

		// Enable heartbeat to ensure at least one wrapper is created (HeartbeatMessageAdapter).
		// HeartbeatMessageAdapter is created BEFORE the IgnoreExtraAdapters check in the pipeline builder,
		// so wrapper != underlying even with IgnoreExtraAdapters=true.
		// Without this, wrapper == underlying and the bug is hidden.
		basket.ApplyHeartbeat(adapter1, true);
		basket.ApplyHeartbeat(adapter2, true);

		// --- Connect ---
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(2);
		ClearOut();

		// --- Set portfolio mapping (this populates _portfolioAdapters in AdapterRouter) ---
		basket.PortfolioAdapterProvider.SetAdapter(Portfolio1, adapter1);

		// --- Send OrderCancel with unknown OriginalTransactionId but valid PortfolioName ---
		// This triggers the portfolio fallback path in ProcessOrderMessage.
		// The routing manager does NOT have transId 999999 mapped (no order was registered),
		// so it falls back to portfolio lookup.
		var cancelTransId = basket.TransactionIdGenerator.GetNextId();
		var cancelMsg = new OrderCancelMessage
		{
			SecurityId = SecId1,
			PortfolioName = Portfolio1,
			TransactionId = cancelTransId,
			OriginalTransactionId = 999999, // Unknown — forces portfolio fallback
		};
		await SendToBasket(basket, cancelMsg, TestContext.CancellationToken);

		// --- Expected: OrderCancel routed to adapter1 via portfolio fallback ---
		// BUG: adapter1 does NOT receive the cancel because _adapterWrappers.TryGetValue(wrapper)
		// fails (wrapper is not a key in _adapterWrappers). Instead, UnknownTransactionId error is emitted.
		adapter1.GetMessages<OrderCancelMessage>().Any()
			.AssertTrue("Adapter1 should receive OrderCancelMessage via portfolio fallback");

		// Should NOT have error response
		GetOut<ExecutionMessage>().Any(e => e.OriginalTransactionId == cancelTransId && e.Error != null)
			.AssertFalse("Should not produce UnknownTransactionId error when portfolio fallback finds the adapter");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderStatus_WithPortfolio_BroadcastToBothAdapters()
	{
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, adapter2) = CreateBasket(
			parentChildMap: parentChildMap);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		basket.PortfolioAdapterProvider.SetAdapter(Portfolio1, adapter1);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			PortfolioName = Portfolio1,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		// With PortfolioName set, FilterEnabled=true → broadcast to all adapters
		adapter1.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter1 should receive OrderStatusMessage");
		adapter2.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter2 should receive OrderStatusMessage");

		// Parent-child mapping should exist for both
		var children = parentChildMap.GetChild(transId);
		children.Count.AssertEqual(2, "Should have 2 child subscriptions");

		// Responses should be remapped to parent transId
		var onlines = GetOut<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("SubscriptionOnline should have parent transId");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderStatus_WithPortfolio_OrderData_RemappedToParent()
	{
		var (basket, adapter1, adapter2) = CreateBasket();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		basket.PortfolioAdapterProvider.SetAdapter(Portfolio1, adapter1);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			PortfolioName = Portfolio1,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		// Adapter auto-responds with ExecutionMessage (order data)
		// Verify that order data reaches output with parent subscription ID
		var orders = GetOut<ExecutionMessage>()
			.Where(e => e.DataTypeEx == DataType.Transactions && e.HasOrderInfo)
			.ToArray();
		orders.Length.AssertGreater(0, "Should receive order data from adapters");

		foreach (var order in orders)
		{
			var subIds = order.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "Order should have subscription IDs");
			subIds.Contains(transId).AssertTrue(
				$"Order subscription IDs should contain parent {transId}, got [{string.Join(",", subIds)}]");
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderStatus_WithSecurityFilter_BroadcastToBothAdapters()
	{
		var (basket, adapter1, adapter2) = CreateBasket();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			SecurityId = SecId1,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		// SecurityId set → FilterEnabled=true → broadcast to all
		adapter1.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter1 should receive OrderStatusMessage with SecurityId filter");
		adapter2.GetMessages<OrderStatusMessage>().Any()
			.AssertTrue("Adapter2 should receive OrderStatusMessage with SecurityId filter");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task PortfolioLookup_WithPortfolio_BroadcastToBothAdapters()
	{
		var (basket, adapter1, adapter2) = CreateBasket();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		basket.PortfolioAdapterProvider.SetAdapter(Portfolio1, adapter1);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			PortfolioName = Portfolio1,
		};
		await SendToBasket(basket, plMsg, TestContext.CancellationToken);

		// With PortfolioName set, FilterEnabled=true → broadcast to all adapters
		adapter1.GetMessages<PortfolioLookupMessage>().Any()
			.AssertTrue("Adapter1 should receive PortfolioLookupMessage");
		adapter2.GetMessages<PortfolioLookupMessage>().Any()
			.AssertTrue("Adapter2 should receive PortfolioLookupMessage");

		// Finished should be remapped to parent
		var finished = GetOut<SubscriptionFinishedMessage>();
		finished.Any(f => f.OriginalTransactionId == transId)
			.AssertTrue("SubscriptionFinished should have parent transId");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task PortfolioLookup_WithPortfolio_PortfolioData_RemappedToParent()
	{
		var (basket, adapter1, adapter2) = CreateBasket();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		basket.PortfolioAdapterProvider.SetAdapter(Portfolio1, adapter1);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
			PortfolioName = Portfolio1,
		};
		await SendToBasket(basket, plMsg, TestContext.CancellationToken);

		// Verify that portfolio data has correct subscription IDs
		var portfolios = GetOut<PortfolioMessage>();
		portfolios.Length.AssertGreater(0, "Should receive portfolio data");

		foreach (var pf in portfolios)
		{
			var subIds = pf.GetSubscriptionIds();
			subIds.Length.AssertGreater(0, "PortfolioMessage should have subscription IDs");
			subIds.Contains(transId).AssertTrue(
				$"Portfolio subscription IDs should contain parent {transId}, got [{string.Join(",", subIds)}]");
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderStatus_NoFilter_OnlyAllDownloadingAdapters()
	{
		var (basket, adapter1, adapter2) = CreateBasket();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		// No PortfolioName, no SecurityId → FilterEnabled=false
		// → only adapters with IsAllDownloadingSupported(DataType.Transactions) receive it
		// TestBasketInnerAdapter returns false by default → no adapters should receive it
		var transId = basket.TransactionIdGenerator.GetNextId();
		var osMsg = new OrderStatusMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, osMsg, TestContext.CancellationToken);

		adapter1.GetMessages<OrderStatusMessage>().Any()
			.AssertFalse("Adapter1 should NOT receive unfiltered OrderStatus (IsAllDownloadingSupported=false)");
		adapter2.GetMessages<OrderStatusMessage>().Any()
			.AssertFalse("Adapter2 should NOT receive unfiltered OrderStatus (IsAllDownloadingSupported=false)");

		// Basket should emit SubscriptionOnline as empty result
		var onlines = GetOut<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("Should emit SubscriptionOnline for empty adapter list");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task PortfolioLookup_NoFilter_OnlyAllDownloadingAdapters()
	{
		var (basket, adapter1, adapter2) = CreateBasket();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		// No PortfolioName → FilterEnabled=false
		// → only adapters with IsAllDownloadingSupported(DataType.PositionChanges) receive it
		var transId = basket.TransactionIdGenerator.GetNextId();
		var plMsg = new PortfolioLookupMessage
		{
			TransactionId = transId,
			IsSubscribe = true,
		};
		await SendToBasket(basket, plMsg, TestContext.CancellationToken);

		adapter1.GetMessages<PortfolioLookupMessage>().Any()
			.AssertFalse("Adapter1 should NOT receive unfiltered PortfolioLookup (IsAllDownloadingSupported=false)");
		adapter2.GetMessages<PortfolioLookupMessage>().Any()
			.AssertFalse("Adapter2 should NOT receive unfiltered PortfolioLookup (IsAllDownloadingSupported=false)");

		// Basket should emit SubscriptionOnline (not history-only) as empty result
		var onlines = GetOut<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("Should emit SubscriptionOnline for empty adapter list");
	}
}
