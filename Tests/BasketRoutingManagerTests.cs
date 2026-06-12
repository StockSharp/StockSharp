namespace StockSharp.Tests;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Unit tests for <see cref="BasketRoutingManager"/>.
/// </summary>
[TestClass]
public class BasketRoutingManagerTests : BaseTestClass
{
	#region Test Adapter

	private sealed class TestRoutingAdapter : MessageAdapter
	{
		private HashSet<DataType> _allDownloadingTypes = [];

		public TestRoutingAdapter(IdGenerator idGen)
			: base(idGen)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMessage(MessageTypes.SecurityLookup, null);
			this.AddSupportedMessage(MessageTypes.PortfolioLookup, null);
			this.AddSupportedMessage(MessageTypes.OrderStatus, null);
			this.AddSupportedMessage(MessageTypes.MarketData, null);
			this.AddSupportedMessage(MessageTypes.OrderRegister, null);
			this.AddSupportedMessage(MessageTypes.OrderCancel, null);
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.MarketDepth);
			this.AddSupportedMarketDataType(DataType.Level1);
			this.AddSupportedMarketDataType(DataType.News);
		}

		public void SetAllDownloadingSupported(params DataType[] types)
			=> _allDownloadingTypes = [.. types];

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> _allDownloadingTypes.Contains(dataType);

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken ct)
			=> default;

		public override IMessageAdapter Clone() => new TestRoutingAdapter(TransactionIdGenerator);
	}

	#endregion

	#region Helpers

	private static readonly SecurityId _secId1 = "AAPL@NASDAQ".ToSecurityId();

	private sealed class RoutingTestContext
	{
		public BasketRoutingManager Manager { get; init; }
		public IAdapterConnectionState ConnectionState { get; init; }
		public IAdapterConnectionManager ConnectionManager { get; init; }
		public IPendingMessageState PendingState { get; init; }
		public ISubscriptionRoutingState SubscriptionRouting { get; init; }
		public IParentChildMap ParentChildMap { get; init; }
		public IOrderRoutingState OrderRouting { get; init; }
		public IAdapterRouter Router { get; init; }
		public IdGenerator IdGen { get; init; }
	}

	private RoutingTestContext CreateTestContext(IdGenerator idGen = null)
	{
		idGen ??= new IncrementalIdGenerator();
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var pendingState = new PendingMessageState();
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var orderRouting = new OrderRoutingState();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var router = new AdapterRouter(orderRouting, GetUnderlyingAdapter, candleBuilderProvider, () => false);

		var manager = new BasketRoutingManager(
			connectionState,
			connectionManager,
			pendingState,
			subscriptionRouting,
			parentChildMap,
			orderRouting,
			GetUnderlyingAdapter,
			candleBuilderProvider,
			() => false,
			idGen,
			null,
			router);

		return new RoutingTestContext
		{
			Manager = manager,
			ConnectionState = connectionState,
			ConnectionManager = connectionManager,
			PendingState = pendingState,
			SubscriptionRouting = subscriptionRouting,
			ParentChildMap = parentChildMap,
			OrderRouting = orderRouting,
			Router = router,
			IdGen = idGen,
		};
	}

	private static IMessageAdapter GetUnderlyingAdapter(IMessageAdapter adapter) => adapter;

	private static TestRoutingAdapter CreateAdapter(IdGenerator idGen = null)
		=> new(idGen ?? new IncrementalIdGenerator());

	#endregion

	#region ProcessInMessage — MarketData Subscribe

	[TestMethod]
	public async Task ProcessInMessage_MarketData_Subscribe_CreatesChildMappings()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);

		// Simulate connected state
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		var transId = idGen.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};

		var result = await ctx.Manager.ProcessInMessageAsync(mdMsg, a => a, CancellationToken);

		// Verify routing decisions exist
		result.RoutingDecisions.Count.AssertEqual(1, "Should have routing decisions");
		result.Handled.AssertTrue("Message should be handled");
		result.IsPended.AssertFalse("Message should not be pended");
		result.OutMessages.Count.AssertEqual(0, "Should not have out messages for successful routing");
		result.LoopbackMessages.Count.AssertEqual(0, "Should not have loopback messages");

		// Verify routing decision content
		var (routedAdapter, routedMsg) = result.RoutingDecisions[0];
		routedAdapter.AssertEqual(adapter, "Should route to the adapter");
		var childMdMsg = routedMsg as MarketDataMessage;
		childMdMsg.AssertNotNull("Routed message should be MarketDataMessage");
		childMdMsg.SecurityId.AssertEqual(_secId1, "SecurityId should match");
		childMdMsg.DataType2.AssertEqual(DataType.Ticks, "DataType should match");
		childMdMsg.IsSubscribe.AssertTrue("Should be subscribe");

		// Verify subscription routing recorded
		ctx.SubscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertTrue("Subscription should be recorded");

		// Verify parent-child mapping created
		var childTransId = childMdMsg.TransactionId;
		childTransId.AssertNotEqual(transId, "Child should have different transactionId");

		ctx.ParentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have mapping");
		parentId.AssertEqual(transId, "Parent ID should match");
	}

	[TestMethod]
	public async Task ProcessInMessage_MarketData_Subscribe_NoAdapters_ReturnsNotSupported()
	{
		var ctx = CreateTestContext();

		// Simulate connected but no adapters for MarketData
		ctx.ConnectionState.SetAdapterState(CreateAdapter(), ConnectionStates.Connected, null);

		var transId = 100L;
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};

		var result = await ctx.Manager.ProcessInMessageAsync(mdMsg, a => a, CancellationToken);

		// Should return not supported message
		result.OutMessages.Count.AssertEqual(1, "Should have one out message");
		result.OutMessages[0].Type.AssertEqual(MessageTypes.SubscriptionResponse, "Should be response message");
		var response = (SubscriptionResponseMessage)result.OutMessages[0];
		response.IsNotSupported().AssertTrue("Should be NotSupported");

		// The response must be addressed back to the original request (CreateNotSupported sets
		// OriginalTransactionId = transId), otherwise the outer subscriber cannot correlate it.
		response.OriginalTransactionId.AssertEqual(transId, "Response should reference the original transaction id");
	}

	#endregion

	#region ProcessInMessage — MarketData Unsubscribe

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task ProcessInMessage_MarketData_Unsubscribe_RemovesMapping()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		// First subscribe
		var subscribeTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = subscribeTransId,
		};

		var subscribeResult = await ctx.Manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);
		subscribeResult.RoutingDecisions.Count.AssertEqual(1);

		// Verify subscription exists
		ctx.SubscriptionRouting.TryGetSubscription(subscribeTransId, out _, out _, out _).AssertTrue();

		// The child id created for the subscribe must be mapped to the parent.
		var childTransId = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;
		ctx.ParentChildMap.TryGetParent(childTransId, out _)
			.AssertTrue("Original child mapping should exist before unsubscribe");

		// The adapter confirms the child subscription. This moves the child to the Active state
		// in ParentChildMap, which is required for ToChild's GetChild (it only enumerates active
		// children) to route the subsequent unsubscribe down to that child.
		await ctx.Manager.ProcessOutMessageAsync(adapter,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId }, a => a, CancellationToken);

		// Now unsubscribe
		var unsubscribeTransId = idGen.GetNextId();
		var unsubscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = unsubscribeTransId,
			OriginalTransactionId = subscribeTransId,
		};

		var unsubscribeResult = await ctx.Manager.ProcessInMessageAsync(unsubscribeMsg, a => a, CancellationToken);

		// Unsubscribe is routed to the child adapter (one decision per active child).
		unsubscribeResult.Handled.AssertTrue("Unsubscribe should be handled");
		unsubscribeResult.RoutingDecisions.Count.AssertEqual(1, "Unsubscribe should be routed to the single child");

		// The fix cb2caf56c8 removes the original child mapping SYNCHRONOUSLY in ToChild
		// (ParentChildMap.RemoveMapping) so that subsequent data with the original child id
		// is no longer forwarded — this must hold immediately after ProcessInMessageAsync,
		// not only after an out-message response arrives.
		ctx.ParentChildMap.TryGetParent(childTransId, out _)
			.AssertFalse("Original child mapping must be removed synchronously on unsubscribe");
	}

	[TestMethod]
	public async Task ProcessInMessage_MarketData_Unsubscribe_NotFound_ReturnsHandled()
	{
		var ctx = CreateTestContext();
		var adapter = CreateAdapter();
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		var unsubscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = 200,
			OriginalTransactionId = 100, // non-existent
		};

		var result = await ctx.Manager.ProcessInMessageAsync(unsubscribeMsg, a => a, CancellationToken);

		// Should be handled (logged and ignored)
		result.Handled.AssertTrue("Unknown unsubscribe should be handled");
		result.RoutingDecisions.Count.AssertEqual(0, "No routing for unknown unsubscribe");
	}

	#endregion

	#region ProcessInMessage — Pending Messages

	[TestMethod]
	public async Task ProcessInMessage_HasPendingAdapters_Pends()
	{
		var ctx = CreateTestContext();

		// Simulate adapter in Connecting state
		var adapter = CreateAdapter();
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connecting, null);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var result = await ctx.Manager.ProcessInMessageAsync(mdMsg, a => a, CancellationToken);

		result.IsPended.AssertTrue("Message should be pended");
		result.Handled.AssertTrue("Pended message is handled");
		result.RoutingDecisions.Count.AssertEqual(0, "Should not have routing decisions when pended");
		result.OutMessages.Count.AssertEqual(0, "Should not have out messages when pended");
		result.LoopbackMessages.Count.AssertEqual(0, "Should not have loopback messages when pended");
		ctx.PendingState.Count.AssertEqual(1, "Message should be in pending state");
	}

	#endregion

	#region ProcessOutMessage — SubscriptionResponse

	[TestMethod]
	public async Task ProcessOutMessage_SubscriptionResponse_RemapsToParent()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		// Subscribe to create child mapping
		var parentTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		var subscribeResult = await ctx.Manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);
		var childMsg = (ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message;
		var childTransId = childMsg.TransactionId;

		// Simulate adapter response with child ID
		var responseMsg = new SubscriptionResponseMessage
		{
			OriginalTransactionId = childTransId,
		};

		var outResult = await ctx.Manager.ProcessOutMessageAsync(adapter, responseMsg, a => a, CancellationToken);

		// Should remap to parent
		outResult.TransformedMessage.AssertNotNull("Should have transformed message");
		var transformed = (SubscriptionResponseMessage)outResult.TransformedMessage;
		transformed.OriginalTransactionId.AssertEqual(parentTransId, "Should remap to parent ID");
	}

	#endregion

	#region ProcessOutMessage — SubscriptionFinished

	[TestMethod]
	public async Task ProcessOutMessage_SubscriptionFinished_WaitsForAll()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter1 = CreateAdapter(idGen);
		var adapter2 = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		ctx.ConnectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		ctx.ConnectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		// Subscribe (goes to both adapters)
		var parentTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		var subscribeResult = await ctx.Manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);

		// Should have 2 child subscriptions
		subscribeResult.RoutingDecisions.Count.AssertEqual(2, "Should route to both adapters");

		var childTransId1 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;
		var childTransId2 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[1].Message).TransactionId;

		// Simulate responses
		await ctx.Manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId1 }, a => a, CancellationToken);
		await ctx.Manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId2 }, a => a, CancellationToken);

		// First adapter finishes
		var finish1Result = await ctx.Manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionFinishedMessage { OriginalTransactionId = childTransId1 }, a => a, CancellationToken);

		// Should NOT emit parent finish yet
		finish1Result.TransformedMessage.AssertNull("Should not emit finish until all children done");

		// Second adapter finishes
		var finish2Result = await ctx.Manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionFinishedMessage { OriginalTransactionId = childTransId2 }, a => a, CancellationToken);

		// NOW should emit parent finish
		finish2Result.TransformedMessage.AssertNotNull("Should emit finish after all children");
		var finishMsg = (SubscriptionFinishedMessage)finish2Result.TransformedMessage;
		finishMsg.OriginalTransactionId.AssertEqual(parentTransId, "Should have parent ID");
	}

	#endregion

	#region ProcessOutMessage — SubscriptionOnline

	[TestMethod]
	public async Task ProcessOutMessage_SubscriptionOnline_Aggregates()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter1 = CreateAdapter(idGen);
		var adapter2 = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		ctx.ConnectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		ctx.ConnectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		var parentTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		var subscribeResult = await ctx.Manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);
		var childTransId1 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;
		var childTransId2 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[1].Message).TransactionId;

		// Send responses
		await ctx.Manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId1 }, a => a, CancellationToken);
		await ctx.Manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId2 }, a => a, CancellationToken);

		// First online
		var online1Result = await ctx.Manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionOnlineMessage { OriginalTransactionId = childTransId1 }, a => a, CancellationToken);

		// Should not emit until all are online
		online1Result.TransformedMessage.AssertNull("Should not emit online until all children");

		// Second online
		var online2Result = await ctx.Manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionOnlineMessage { OriginalTransactionId = childTransId2 }, a => a, CancellationToken);

		online2Result.TransformedMessage.AssertNotNull("Should emit online after all children");
		var onlineMsg = (SubscriptionOnlineMessage)online2Result.TransformedMessage;
		onlineMsg.OriginalTransactionId.AssertEqual(parentTransId, "Should have parent ID");
	}

	[TestMethod]
	public async Task ProcessOutMessage_DataMessage_RemapsSubscriptionIds()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		// Subscribe
		var parentTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		var subscribeResult = await ctx.Manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);
		var childTransId = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;

		// Simulate response
		await ctx.Manager.ProcessOutMessageAsync(adapter,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId }, a => a, CancellationToken);

		// Data message from adapter with child subscription ID
		var dataMsg = new ExecutionMessage
		{
			SecurityId = _secId1,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMsg.SetSubscriptionIds([childTransId]);

		var dataResult = await ctx.Manager.ProcessOutMessageAsync(adapter, dataMsg, a => a, CancellationToken);

		dataResult.TransformedMessage.AssertNotNull("Data should be forwarded");
		var ids = ((ISubscriptionIdMessage)dataResult.TransformedMessage).GetSubscriptionIds();
		ids.Contains(parentTransId).AssertTrue("Should contain parent ID");
		ids.Count(id => id == childTransId).AssertEqual(0, "Should NOT contain child ID");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task ProcessOutMessage_DataMessage_MultipleChildIds_AllRemapped()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter1 = CreateAdapter(idGen);
		var adapter2 = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		ctx.ConnectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		ctx.ConnectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		// Subscribe (broadcast to both adapters)
		var parentTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		var subscribeResult = await ctx.Manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);
		subscribeResult.RoutingDecisions.Count.AssertEqual(2, "Should route to both adapters");

		var childId1 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;
		var childId2 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[1].Message).TransactionId;
		childId1.AssertNotEqual(childId2, "Child ids must be distinct");

		// Responses
		await ctx.Manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionResponseMessage { OriginalTransactionId = childId1 }, a => a, CancellationToken);
		await ctx.Manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionResponseMessage { OriginalTransactionId = childId2 }, a => a, CancellationToken);

		// A single out message carrying BOTH child ids (both children belong to the same
		// parent subscription). This is what the test name promises: multiple child ids on
		// one data message, all of which must be remapped to the parent.
		var dataMsg = new ExecutionMessage
		{
			SecurityId = _secId1,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMsg.SetSubscriptionIds([childId1, childId2]);

		var dataResult = await ctx.Manager.ProcessOutMessageAsync(adapter1, dataMsg, a => a, CancellationToken);

		dataResult.TransformedMessage.AssertNotNull("Data should be forwarded");
		var ids = ((ISubscriptionIdMessage)dataResult.TransformedMessage).GetSubscriptionIds();

		// No raw child id may leak to the outer subscriber.
		ids.Count(id => id == childId1).AssertEqual(0, "Child1 ID should NOT leak");
		ids.Count(id => id == childId2).AssertEqual(0, "Child2 ID should NOT leak");

		// Canonical contract: both children map to the SAME parent, so the parent must be
		// referenced exactly once. ApplyParentLookupId currently does not deduplicate, so
		// [child1, child2] -> [parent, parent], which would deliver the same tick twice to
		// the parent subscriber. Asserting the correct (deduplicated) behavior; if the engine
		// emits a duplicate parent id this test goes red and pins that bug.
		ids.Contains(parentTransId).AssertTrue("Should remap to parent ID");
		ids.Count(id => id == parentTransId).AssertEqual(1, "Parent ID must not be duplicated");
	}

	[TestMethod]
	public async Task ProcessOutMessage_SubscriptionFinished_OneOfTwo_DataStillRemapsOther()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter1 = CreateAdapter(idGen);
		var adapter2 = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		ctx.ConnectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		ctx.ConnectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		// Subscribe (broadcast)
		var parentTransId = idGen.GetNextId();
		var subscribeResult = await ctx.Manager.ProcessInMessageAsync(new MarketDataMessage
		{
			SecurityId = _secId1, DataType2 = DataType.Ticks,
			IsSubscribe = true, TransactionId = parentTransId,
		}, a => a, CancellationToken);

		var childId1 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;
		var childId2 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[1].Message).TransactionId;

		// Responses for both
		await ctx.Manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionResponseMessage { OriginalTransactionId = childId1 }, a => a, CancellationToken);
		await ctx.Manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionResponseMessage { OriginalTransactionId = childId2 }, a => a, CancellationToken);

		// Finished for child1
		await ctx.Manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionFinishedMessage { OriginalTransactionId = childId1 }, a => a, CancellationToken);

		// Data from adapter2 with child2 ID — should still remap to parent
		var dataMsg = new ExecutionMessage
		{
			SecurityId = _secId1, DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow, TradePrice = 100m, TradeVolume = 10m,
		};
		dataMsg.SetSubscriptionIds([childId2]);

		var dataResult = await ctx.Manager.ProcessOutMessageAsync(adapter2, dataMsg, a => a, CancellationToken);

		dataResult.TransformedMessage.AssertNotNull("Data should still be forwarded after one child finished");
		var ids = ((ISubscriptionIdMessage)dataResult.TransformedMessage).GetSubscriptionIds();
		ids.Contains(parentTransId).AssertTrue("Should remap to parent ID");
		ids.Count(id => id == childId2).AssertEqual(0, "Child ID should not leak");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task ProcessOutMessage_SubscriptionError_OneOfTwo_OtherStillWorks()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter1 = CreateAdapter(idGen);
		var adapter2 = CreateAdapter(idGen);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		ctx.ConnectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		ctx.ConnectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		// Subscribe (broadcast)
		var parentTransId = idGen.GetNextId();
		var subscribeResult = await ctx.Manager.ProcessInMessageAsync(new MarketDataMessage
		{
			SecurityId = _secId1, DataType2 = DataType.Ticks,
			IsSubscribe = true, TransactionId = parentTransId,
		}, a => a, CancellationToken);

		var childId1 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;
		var childId2 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[1].Message).TransactionId;

		// Error for child1. The other child has not responded yet (Stopped), so the parent
		// response must be withheld: ProcessChildResponse returns needParentResponse == false.
		var errorResult = await ctx.Manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionResponseMessage
			{
				OriginalTransactionId = childId1,
				Error = new InvalidOperationException("fail"),
			}, a => a, CancellationToken);

		errorResult.TransformedMessage.AssertNull("Parent response must wait until all children responded");

		// Success for child2. Now every child has responded and at least one succeeded
		// (allError == false), so the aggregated parent SubscriptionResponse is emitted with
		// NO error — the partial failure of child1 must not surface as a parent-level error.
		var successResult = await ctx.Manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionResponseMessage { OriginalTransactionId = childId2 }, a => a, CancellationToken);

		successResult.TransformedMessage.AssertNotNull("Parent response should be emitted after the last child");
		var parentResponse = (SubscriptionResponseMessage)successResult.TransformedMessage;
		parentResponse.OriginalTransactionId.AssertEqual(parentTransId, "Parent response should carry parent id");
		parentResponse.Error.AssertNull("Parent response must be successful when at least one child succeeded");
		parentResponse.IsOk().AssertTrue("Parent response must be Ok when at least one child succeeded");

		// Online for child2
		await ctx.Manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionOnlineMessage { OriginalTransactionId = childId2 }, a => a, CancellationToken);

		// Data from adapter2
		var dataMsg = new ExecutionMessage
		{
			SecurityId = _secId1, DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow, TradePrice = 100m, TradeVolume = 10m,
		};
		dataMsg.SetSubscriptionIds([childId2]);

		var dataResult = await ctx.Manager.ProcessOutMessageAsync(adapter2, dataMsg, a => a, CancellationToken);

		dataResult.TransformedMessage.AssertNotNull("Data should still arrive from working adapter");
		var ids = ((ISubscriptionIdMessage)dataResult.TransformedMessage).GetSubscriptionIds();
		ids.Contains(parentTransId).AssertTrue("Should remap to parent ID");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task ProcessOutMessage_DataMessage_UnmappedSubscriptionId_IsDropped()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter = CreateAdapter(idGen);
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		// Data message with a subscription ID that has no parent-child mapping
		// (e.g. the subscription was already unsubscribed). The current contract
		// (BasketRoutingManager.ApplyParentLookupId / cb2caf56c8 "Fix data forwarding
		// after unsubscribe") is to DROP such data: ApplyParentLookupId returns false
		// when no id maps to a valid parent, so ProcessOutMessageAsync yields
		// RoutingOutResult.Empty and TransformedMessage stays null.
		var dataMsg = new ExecutionMessage
		{
			SecurityId = _secId1,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 100m,
			TradeVolume = 10m,
		};
		dataMsg.SetSubscriptionIds([9999]);

		var dataResult = await ctx.Manager.ProcessOutMessageAsync(adapter, dataMsg, a => a, CancellationToken);

		// Data for an unmapped id must be dropped (not forwarded) — unconditional assert,
		// no hidden `if` that could make it vacuous.
		dataResult.TransformedMessage.AssertNull("Data for an unmapped subscription id must be dropped");
		dataResult.LoopbackMessages.Count.AssertEqual(0, "No loopback for dropped data");
		dataResult.ExtraMessages.Count.AssertEqual(0, "No extra messages for dropped data");
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task ProcessOutMessage_DataMessage_PinnedAdapterSubscription_StillForwarded()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter = CreateAdapter(idGen);
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		// A subscription routed DIRECTLY to a pinned adapter (Message.Adapter set) takes the
		// short-circuit in ProcessSubscriptionMessageAsync: it is registered in the routing
		// state with its OWN transaction id and is NOT split into child subscriptions, so no
		// parent-child mapping is ever created for it.
		var subTransId = idGen.GetNextId();
		var lookupMsg = new SecurityLookupMessage
		{
			TransactionId = subTransId,
			Adapter = adapter,
		};

		var inResult = await ctx.Manager.ProcessInMessageAsync(lookupMsg, a => a, CancellationToken);

		inResult.RoutingDecisions.Count.AssertEqual(1, "Pinned subscription should route to the pinned adapter");
		inResult.RoutingDecisions[0].Adapter.AssertEqual(adapter, "Should route to the pinned adapter");
		ctx.SubscriptionRouting.TryGetSubscription(subTransId, out _, out _, out _)
			.AssertTrue("Pinned subscription must be recorded in the routing state");

		// Data emitted upstream carries this subscription's own transaction id (as set by
		// SubscriptionOnlineMessageAdapter). It belongs to a KNOWN, active subscription and must
		// reach the outer subscriber unchanged. ApplyParentLookupId only consults the parent-child
		// map (empty for pinned subscriptions), so dropping this data would be silent data loss.
		var dataMsg = new SecurityMessage { SecurityId = _secId1 };
		dataMsg.SetSubscriptionIds([subTransId]);

		var dataResult = await ctx.Manager.ProcessOutMessageAsync(adapter, dataMsg, a => a, CancellationToken);

		dataResult.TransformedMessage.AssertNotNull("Data for a known pinned subscription must be forwarded, not dropped");
		var ids = ((ISubscriptionIdMessage)dataResult.TransformedMessage).GetSubscriptionIds();
		ids.Contains(subTransId).AssertTrue("The pinned subscription id must be preserved");
	}

	#endregion

	#region Order Adapter Mapping

	[TestMethod]
	public void AddOrderAdapter_StoresMapping()
	{
		var ctx = CreateTestContext();
		var adapter = CreateAdapter();

		ctx.Manager.AddOrderAdapter(100, adapter);

		ctx.Manager.TryGetOrderAdapter(100, out var found).AssertTrue("Should find adapter");
		found.AssertEqual(adapter, "Should be same adapter");
	}

	[TestMethod]
	public void TryGetOrderAdapter_NotFound_ReturnsFalse()
	{
		var ctx = CreateTestContext();

		ctx.Manager.TryGetOrderAdapter(999, out _).AssertFalse("Should not find adapter");
	}

	#endregion

	#region ProcessConnect

	[TestMethod]
	public void ProcessConnect_AddsMessageTypeAdapters()
	{
		var ctx = CreateTestContext();
		var adapter = CreateAdapter();

		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connecting, null);
		ctx.Manager.BeginConnect();

		// Process successful connection
		var (outMsgs, pending, notSupported) = ctx.Manager.ProcessConnect(
			adapter, adapter, adapter.SupportedInMessages, null);

		// The first adapter going Connected must produce exactly one successful ConnectMessage
		// (ConnectDisconnectEventOnFirstAdapter is true by default) and no pending/not-supported.
		var outList = outMsgs.ToArray();
		outList.Length.AssertEqual(1, "Should emit a single ConnectMessage");
		var connectMsg = outList[0] as ConnectMessage;
		connectMsg.AssertNotNull("Out message should be a ConnectMessage");
		connectMsg.Error.AssertNull("Successful connect must have no error");
		pending.Length.AssertEqual(0, "No pending loopback expected");
		notSupported.Length.AssertEqual(0, "No not-supported messages expected");

		// After connect, adapter should be registered for its supported message types
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = ctx.Router.GetAdapters(mdMsg, a => a);
		adapters.AssertNotNull("Should find adapter after ProcessConnect");
		adapters.Contains(adapter).AssertTrue("Returned adapters must include the connected adapter");
	}

	[TestMethod]
	public void ProcessConnect_WithError_DoesNotAddAdapters()
	{
		var ctx = CreateTestContext();
		var adapter = CreateAdapter();

		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connecting, null);
		ctx.Manager.BeginConnect();

		// Process failed connection
		var (outMsgs, pending, notSupported) = ctx.Manager.ProcessConnect(
			adapter, adapter, adapter.SupportedInMessages, new Exception("Connection failed"));

		// Should not register adapters on error
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = ctx.Router.GetAdapters(mdMsg, a => a);
		adapters.AssertNull("Should not find adapter after failed connection");
	}

	#endregion

	#region ProcessDisconnect

	[TestMethod]
	public void ProcessDisconnect_RemovesMessageTypeAdapters()
	{
		var ctx = CreateTestContext();
		var adapter = CreateAdapter();

		// First connect
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connecting, null);
		ctx.Manager.BeginConnect();
		ctx.Manager.ProcessConnect(adapter, adapter, adapter.SupportedInMessages, null);

		// Verify adapter is registered
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};
		var (adapters, _) = ctx.Router.GetAdapters(mdMsg, a => a);
		adapters.AssertNotNull("Should find adapter before disconnect");

		// Now disconnect
		ctx.Manager.ProcessDisconnect(adapter, adapter, adapter.SupportedInMessages, null);

		// Verify adapter is removed
		(adapters, _) = ctx.Router.GetAdapters(mdMsg, a => a);
		adapters.AssertNull("Should not find adapter after disconnect");
	}

	#endregion

	#region Reset

	[TestMethod]
	public void Reset_ClearsAllState()
	{
		var ctx = CreateTestContext();
		var adapter = CreateAdapter();

		// Add some state
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connecting, null);
		ctx.Manager.BeginConnect();
		ctx.Manager.ProcessConnect(adapter, adapter, adapter.SupportedInMessages, null);

		ctx.SubscriptionRouting.AddSubscription(100, new MarketDataMessage { TransactionId = 100 }, [adapter], DataType.Ticks);
		ctx.PendingState.Add(new SecurityLookupMessage { TransactionId = 200 });

		// Reset with clearPending=true
		ctx.Manager.Reset(true);

		// Verify cleared
		ctx.SubscriptionRouting.TryGetSubscription(100, out _, out _, out _)
			.AssertFalse("Subscription routing should be cleared");
		ctx.PendingState.Count.AssertEqual(0, "Pending state should be cleared");
	}

	[TestMethod]
	public void Reset_PreservesPending_WhenClearPendingFalse()
	{
		var ctx = CreateTestContext();

		ctx.PendingState.Add(new SecurityLookupMessage { TransactionId = 200 });

		ctx.Manager.Reset(false);

		ctx.PendingState.Count.AssertEqual(1, "Pending should be preserved when clearPending=false");
	}

	#endregion

	#region GetSubscribers

	[TestMethod]
	public void GetSubscribers_ReturnsCorrectIds()
	{
		var ctx = CreateTestContext();
		var adapter = CreateAdapter();

		ctx.SubscriptionRouting.AddSubscription(100, new MarketDataMessage { TransactionId = 100 }, [adapter], DataType.Ticks);
		ctx.SubscriptionRouting.AddSubscription(101, new MarketDataMessage { TransactionId = 101 }, [adapter], DataType.Ticks);
		ctx.SubscriptionRouting.AddSubscription(102, new MarketDataMessage { TransactionId = 102 }, [adapter], DataType.Level1);

		var tickSubscribers = ctx.Manager.GetSubscribers(DataType.Ticks);

		tickSubscribers.Length.AssertEqual(2, "Should have 2 tick subscribers");
		tickSubscribers.AssertContains(100L);
		tickSubscribers.AssertContains(101L);
	}

	#endregion

	#region State Properties

	[TestMethod]
	public void HasPendingAdapters_ReflectsConnectionState()
	{
		var ctx = CreateTestContext();

		ctx.Manager.HasPendingAdapters.AssertFalse("Initially no pending");

		var adapter = CreateAdapter();
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connecting, null);

		ctx.Manager.HasPendingAdapters.AssertTrue("Should have pending after Connecting");
	}

	[TestMethod]
	public void ConnectedCount_ReflectsConnectionState()
	{
		var ctx = CreateTestContext();

		ctx.Manager.ConnectedCount.AssertEqual(0, "Initially 0");

		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();
		ctx.ConnectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		ctx.ConnectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		ctx.Manager.ConnectedCount.AssertEqual(2, "Should be 2 after connections");
	}

	#endregion

	#region Subscription Message Processing

	[TestMethod]
	public async Task ProcessInMessage_SecurityLookup_CreatesChildMappings()
	{
		var idGen = new IncrementalIdGenerator();
		var ctx = CreateTestContext(idGen);

		var adapter = CreateAdapter(idGen);
		adapter.SetAllDownloadingSupported(DataType.Securities);
		ctx.Router.AddMessageTypeAdapter(MessageTypes.SecurityLookup, adapter);
		ctx.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		var transId = idGen.GetNextId();
		var lookupMsg = new SecurityLookupMessage
		{
			TransactionId = transId,
		};

		var result = await ctx.Manager.ProcessInMessageAsync(lookupMsg, a => a, CancellationToken);

		result.RoutingDecisions.Count.AssertEqual(1, "Should have routing decisions");
		result.Handled.AssertTrue("Should be handled");

		// Verify child mapping
		var childTransId = ((ISubscriptionMessage)result.RoutingDecisions[0].Message).TransactionId;
		ctx.ParentChildMap.TryGetParent(childTransId, out var parentId).AssertTrue("Should have mapping");
		parentId.AssertEqual(transId, "Parent should match");
	}

	#endregion
}
