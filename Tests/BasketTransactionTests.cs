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
		connectionState.TryGetAdapterState(adapter1, out var cs1).AssertTrue();
		cs1.AssertEqual(ConnectionStates.Connected);
		connectionState.TryGetAdapterState(adapter2, out var cs2).AssertTrue();
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
		connectionState.TryGetAdapterState(adapter1, out var cs1).AssertTrue();
		cs1.AssertEqual(ConnectionStates.Connected);
		connectionState.TryGetAdapterState(adapter2, out var cs2).AssertTrue();
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
}
