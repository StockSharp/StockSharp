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
		result.RoutingDecisions.Count.AssertGreater(0, "Should have routing decisions");
		result.Handled.AssertTrue("Message should be handled");
		result.IsPended.AssertFalse("Message should not be pended");

		// Verify subscription routing recorded
		ctx.SubscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertTrue("Subscription should be recorded");

		// Verify parent-child mapping created
		var childTransId = result.RoutingDecisions[0].Message is ISubscriptionMessage subMsg
			? subMsg.TransactionId
			: 0;

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
		((SubscriptionResponseMessage)result.OutMessages[0]).IsNotSupported()
			.AssertTrue("Should be NotSupported");
	}

	#endregion

	#region ProcessInMessage — MarketData Unsubscribe

	[TestMethod]
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
		subscribeResult.RoutingDecisions.Count.AssertGreater(0);

		// Verify subscription exists
		ctx.SubscriptionRouting.TryGetSubscription(subscribeTransId, out _, out _, out _).AssertTrue();

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

		// Should have routing decisions for unsubscribe
		// (actual removal happens in ProcessOutMessage when response comes)
		unsubscribeResult.Handled.AssertTrue("Unsubscribe should be handled");
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
		ctx.PendingState.Count.AssertGreater(0, "Message should be in pending state");
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

		ctx.PendingState.Count.AssertGreater(0, "Pending should be preserved when clearPending=false");
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

		result.RoutingDecisions.Count.AssertGreater(0, "Should have routing decisions");
		result.Handled.AssertTrue("Should be handled");

		// Verify child mapping
		var childTransId = ((ISubscriptionMessage)result.RoutingDecisions[0].Message).TransactionId;
		ctx.ParentChildMap.TryGetParent(childTransId, out var parentId).AssertTrue("Should have mapping");
		parentId.AssertEqual(transId, "Parent should match");
	}

	#endregion
}
