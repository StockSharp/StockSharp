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

	private BasketRoutingManager CreateManager(
		IAdapterConnectionState connectionState = null,
		IAdapterConnectionManager connectionManager = null,
		IPendingMessageState pendingState = null,
		IPendingMessageManager pendingManager = null,
		ISubscriptionRoutingState subscriptionRouting = null,
		IParentChildMap parentChildMap = null,
		IOrderRoutingState orderRouting = null,
		IdGenerator idGen = null)
	{
		connectionState ??= new AdapterConnectionState();
		connectionManager ??= new AdapterConnectionManager(connectionState);
		pendingState ??= new PendingMessageState();
		pendingManager ??= new PendingMessageManager(pendingState);
		subscriptionRouting ??= new SubscriptionRoutingState();
		parentChildMap ??= new ParentChildMap();
		orderRouting ??= new OrderRoutingState();
		idGen ??= new IncrementalIdGenerator();

		return new BasketRoutingManager(
			connectionState,
			connectionManager,
			pendingState,
			pendingManager,
			subscriptionRouting,
			parentChildMap,
			orderRouting,
			GetUnderlyingAdapter,
			new CandleBuilderProvider(new InMemoryExchangeInfoProvider()),
			() => false,
			idGen);
	}

	private static IMessageAdapter GetUnderlyingAdapter(IMessageAdapter adapter) => adapter;

	private static TestRoutingAdapter CreateAdapter(IdGenerator idGen = null)
		=> new(idGen ?? new IncrementalIdGenerator());

	#endregion

	#region ProcessInMessage — MarketData Subscribe

	[TestMethod]
	public async Task ProcessInMessage_MarketData_Subscribe_CreatesChildMappings()
	{
		var parentChildMap = new ParentChildMap();
		var subscriptionRouting = new SubscriptionRoutingState();
		var idGen = new IncrementalIdGenerator();

		var manager = CreateManager(
			parentChildMap: parentChildMap,
			subscriptionRouting: subscriptionRouting,
			idGen: idGen);

		var adapter = CreateAdapter(idGen);
		manager.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);

		// Simulate connected state
		manager.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		var transId = idGen.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};

		var result = await manager.ProcessInMessageAsync(mdMsg, a => a, CancellationToken);

		// Verify routing decisions exist
		result.RoutingDecisions.Count.AssertGreater(0, "Should have routing decisions");
		result.Handled.AssertTrue("Message should be handled");
		result.IsPended.AssertFalse("Message should not be pended");

		// Verify subscription routing recorded
		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertTrue("Subscription should be recorded");

		// Verify parent-child mapping created
		var childTransId = result.RoutingDecisions[0].Message is ISubscriptionMessage subMsg
			? subMsg.TransactionId
			: 0;

		parentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("ParentChildMap should have mapping");
		parentId.AssertEqual(transId, "Parent ID should match");
	}

	[TestMethod]
	public async Task ProcessInMessage_MarketData_Subscribe_NoAdapters_ReturnsNotSupported()
	{
		var manager = CreateManager();

		// Simulate connected but no adapters for MarketData
		manager.ConnectionState.SetAdapterState(CreateAdapter(), ConnectionStates.Connected, null);

		var transId = 100L;
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};

		var result = await manager.ProcessInMessageAsync(mdMsg, a => a, CancellationToken);

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
		var parentChildMap = new ParentChildMap();
		var subscriptionRouting = new SubscriptionRoutingState();
		var idGen = new IncrementalIdGenerator();

		var manager = CreateManager(
			parentChildMap: parentChildMap,
			subscriptionRouting: subscriptionRouting,
			idGen: idGen);

		var adapter = CreateAdapter(idGen);
		manager.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);
		manager.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		// First subscribe
		var subscribeTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = subscribeTransId,
		};

		var subscribeResult = await manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);
		subscribeResult.RoutingDecisions.Count.AssertGreater(0);

		// Verify subscription exists
		subscriptionRouting.TryGetSubscription(subscribeTransId, out _, out _, out _).AssertTrue();

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

		var unsubscribeResult = await manager.ProcessInMessageAsync(unsubscribeMsg, a => a, CancellationToken);

		// Should have routing decisions for unsubscribe
		// (actual removal happens in ProcessOutMessage when response comes)
		unsubscribeResult.Handled.AssertTrue("Unsubscribe should be handled");
	}

	[TestMethod]
	public async Task ProcessInMessage_MarketData_Unsubscribe_NotFound_ReturnsHandled()
	{
		var manager = CreateManager();
		var adapter = CreateAdapter();
		manager.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		var unsubscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = false,
			TransactionId = 200,
			OriginalTransactionId = 100, // non-existent
		};

		var result = await manager.ProcessInMessageAsync(unsubscribeMsg, a => a, CancellationToken);

		// Should be handled (logged and ignored)
		result.Handled.AssertTrue("Unknown unsubscribe should be handled");
		result.RoutingDecisions.Count.AssertEqual(0, "No routing for unknown unsubscribe");
	}

	#endregion

	#region ProcessInMessage — Pending Messages

	[TestMethod]
	public async Task ProcessInMessage_HasPendingAdapters_Pends()
	{
		var pendingState = new PendingMessageState();
		var connectionState = new AdapterConnectionState();

		var manager = CreateManager(
			connectionState: connectionState,
			pendingState: pendingState);

		// Simulate adapter in Connecting state
		var adapter = CreateAdapter();
		connectionState.SetAdapterState(adapter, ConnectionStates.Connecting, null);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var result = await manager.ProcessInMessageAsync(mdMsg, a => a, CancellationToken);

		result.IsPended.AssertTrue("Message should be pended");
		result.Handled.AssertTrue("Pended message is handled");
		pendingState.Count.AssertGreater(0, "Message should be in pending state");
	}

	#endregion

	#region ProcessOutMessage — SubscriptionResponse

	[TestMethod]
	public async Task ProcessOutMessage_SubscriptionResponse_RemapsToParent()
	{
		var parentChildMap = new ParentChildMap();
		var subscriptionRouting = new SubscriptionRoutingState();
		var idGen = new IncrementalIdGenerator();

		var manager = CreateManager(
			parentChildMap: parentChildMap,
			subscriptionRouting: subscriptionRouting,
			idGen: idGen);

		var adapter = CreateAdapter(idGen);
		manager.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter);
		manager.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		// Subscribe to create child mapping
		var parentTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		var subscribeResult = await manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);
		var childMsg = (ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message;
		var childTransId = childMsg.TransactionId;

		// Simulate adapter response with child ID
		var responseMsg = new SubscriptionResponseMessage
		{
			OriginalTransactionId = childTransId,
		};

		var outResult = await manager.ProcessOutMessageAsync(adapter, responseMsg, a => a, CancellationToken);

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
		var parentChildMap = new ParentChildMap();
		var subscriptionRouting = new SubscriptionRoutingState();
		var idGen = new IncrementalIdGenerator();

		var manager = CreateManager(
			parentChildMap: parentChildMap,
			subscriptionRouting: subscriptionRouting,
			idGen: idGen);

		var adapter1 = CreateAdapter(idGen);
		var adapter2 = CreateAdapter(idGen);
		manager.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		manager.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		manager.ConnectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		manager.ConnectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		// Subscribe (goes to both adapters)
		var parentTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		var subscribeResult = await manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);

		// Should have 2 child subscriptions
		subscribeResult.RoutingDecisions.Count.AssertEqual(2, "Should route to both adapters");

		var childTransId1 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;
		var childTransId2 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[1].Message).TransactionId;

		// Simulate responses
		await manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId1 }, a => a, CancellationToken);
		await manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId2 }, a => a, CancellationToken);

		// First adapter finishes
		var finish1Result = await manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionFinishedMessage { OriginalTransactionId = childTransId1 }, a => a, CancellationToken);

		// Should NOT emit parent finish yet
		finish1Result.TransformedMessage.AssertNull("Should not emit finish until all children done");

		// Second adapter finishes
		var finish2Result = await manager.ProcessOutMessageAsync(adapter2,
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
		var parentChildMap = new ParentChildMap();
		var subscriptionRouting = new SubscriptionRoutingState();
		var idGen = new IncrementalIdGenerator();

		var manager = CreateManager(
			parentChildMap: parentChildMap,
			subscriptionRouting: subscriptionRouting,
			idGen: idGen);

		var adapter1 = CreateAdapter(idGen);
		var adapter2 = CreateAdapter(idGen);
		manager.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter1);
		manager.Router.AddMessageTypeAdapter(MessageTypes.MarketData, adapter2);
		manager.ConnectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		manager.ConnectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		var parentTransId = idGen.GetNextId();
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = parentTransId,
		};

		var subscribeResult = await manager.ProcessInMessageAsync(subscribeMsg, a => a, CancellationToken);
		var childTransId1 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[0].Message).TransactionId;
		var childTransId2 = ((ISubscriptionMessage)subscribeResult.RoutingDecisions[1].Message).TransactionId;

		// Send responses
		await manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId1 }, a => a, CancellationToken);
		await manager.ProcessOutMessageAsync(adapter2,
			new SubscriptionResponseMessage { OriginalTransactionId = childTransId2 }, a => a, CancellationToken);

		// First online
		var online1Result = await manager.ProcessOutMessageAsync(adapter1,
			new SubscriptionOnlineMessage { OriginalTransactionId = childTransId1 }, a => a, CancellationToken);

		// Should not emit until all are online
		online1Result.TransformedMessage.AssertNull("Should not emit online until all children");

		// Second online
		var online2Result = await manager.ProcessOutMessageAsync(adapter2,
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
		var orderRouting = new OrderRoutingState();
		var manager = CreateManager(orderRouting: orderRouting);
		var adapter = CreateAdapter();

		manager.AddOrderAdapter(100, adapter);

		manager.TryGetOrderAdapter(100, out var found).AssertTrue("Should find adapter");
		found.AssertEqual(adapter, "Should be same adapter");
	}

	[TestMethod]
	public void TryGetOrderAdapter_NotFound_ReturnsFalse()
	{
		var manager = CreateManager();

		manager.TryGetOrderAdapter(999, out _).AssertFalse("Should not find adapter");
	}

	#endregion

	#region Register/Unregister Message Types

	[TestMethod]
	public void RegisterAdapterMessageTypes_AddsToRouter()
	{
		var manager = CreateManager();
		var adapter = CreateAdapter();

		manager.RegisterAdapterMessageTypes(adapter, [MessageTypes.MarketData, MessageTypes.OrderRegister]);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = manager.Router.GetAdapters(mdMsg, a => a);

		adapters.AssertNotNull("Should find adapter");
		adapters.Length.AssertEqual(1);
	}

	[TestMethod]
	public void UnregisterAdapterMessageTypes_RemovesFromRouter()
	{
		var manager = CreateManager();
		var adapter = CreateAdapter();

		manager.RegisterAdapterMessageTypes(adapter, [MessageTypes.MarketData]);
		manager.UnregisterAdapterMessageTypes(adapter, [MessageTypes.MarketData]);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 100,
		};

		var (adapters, _) = manager.Router.GetAdapters(mdMsg, a => a);

		adapters.AssertNull("Should not find adapter after unregister");
	}

	#endregion

	#region Reset

	[TestMethod]
	public void Reset_ClearsAllState()
	{
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();

		var manager = CreateManager(
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap,
			pendingState: pendingState);

		var adapter = CreateAdapter();

		// Add some state
		manager.RegisterAdapterMessageTypes(adapter, [MessageTypes.MarketData]);
		subscriptionRouting.AddSubscription(100, new MarketDataMessage { TransactionId = 100 }, [adapter], DataType.Ticks);
		pendingState.Add(new SecurityLookupMessage { TransactionId = 200 });

		// Reset with clearPending=true
		manager.Reset(true);

		// Verify cleared
		subscriptionRouting.TryGetSubscription(100, out _, out _, out _)
			.AssertFalse("Subscription routing should be cleared");
		pendingState.Count.AssertEqual(0, "Pending state should be cleared");
	}

	[TestMethod]
	public void Reset_PreservesPending_WhenClearPendingFalse()
	{
		var pendingState = new PendingMessageState();
		var manager = CreateManager(pendingState: pendingState);

		pendingState.Add(new SecurityLookupMessage { TransactionId = 200 });

		manager.Reset(false);

		pendingState.Count.AssertGreater(0, "Pending should be preserved when clearPending=false");
	}

	#endregion

	#region GetSubscribers

	[TestMethod]
	public void GetSubscribers_ReturnsCorrectIds()
	{
		var subscriptionRouting = new SubscriptionRoutingState();
		var manager = CreateManager(subscriptionRouting: subscriptionRouting);
		var adapter = CreateAdapter();

		subscriptionRouting.AddSubscription(100, new MarketDataMessage { TransactionId = 100 }, [adapter], DataType.Ticks);
		subscriptionRouting.AddSubscription(101, new MarketDataMessage { TransactionId = 101 }, [adapter], DataType.Ticks);
		subscriptionRouting.AddSubscription(102, new MarketDataMessage { TransactionId = 102 }, [adapter], DataType.Level1);

		var tickSubscribers = manager.GetSubscribers(DataType.Ticks);

		tickSubscribers.Length.AssertEqual(2, "Should have 2 tick subscribers");
		tickSubscribers.AssertContains(100L);
		tickSubscribers.AssertContains(101L);
	}

	#endregion

	#region State Properties

	[TestMethod]
	public void HasPendingAdapters_ReflectsConnectionState()
	{
		var connectionState = new AdapterConnectionState();
		var manager = CreateManager(connectionState: connectionState);

		manager.HasPendingAdapters.AssertFalse("Initially no pending");

		var adapter = CreateAdapter();
		connectionState.SetAdapterState(adapter, ConnectionStates.Connecting, null);

		manager.HasPendingAdapters.AssertTrue("Should have pending after Connecting");
	}

	[TestMethod]
	public void ConnectedCount_ReflectsConnectionState()
	{
		var connectionState = new AdapterConnectionState();
		var manager = CreateManager(connectionState: connectionState);

		manager.ConnectedCount.AssertEqual(0, "Initially 0");

		var adapter1 = CreateAdapter();
		var adapter2 = CreateAdapter();
		connectionState.SetAdapterState(adapter1, ConnectionStates.Connected, null);
		connectionState.SetAdapterState(adapter2, ConnectionStates.Connected, null);

		manager.ConnectedCount.AssertEqual(2, "Should be 2 after connections");
	}

	#endregion

	#region Subscription Message Processing

	[TestMethod]
	public async Task ProcessInMessage_SecurityLookup_CreatesChildMappings()
	{
		var parentChildMap = new ParentChildMap();
		var subscriptionRouting = new SubscriptionRoutingState();
		var idGen = new IncrementalIdGenerator();

		var manager = CreateManager(
			parentChildMap: parentChildMap,
			subscriptionRouting: subscriptionRouting,
			idGen: idGen);

		var adapter = CreateAdapter(idGen);
		adapter.SetAllDownloadingSupported(DataType.Securities);
		manager.Router.AddMessageTypeAdapter(MessageTypes.SecurityLookup, adapter);
		manager.ConnectionState.SetAdapterState(adapter, ConnectionStates.Connected, null);

		var transId = idGen.GetNextId();
		var lookupMsg = new SecurityLookupMessage
		{
			TransactionId = transId,
		};

		var result = await manager.ProcessInMessageAsync(lookupMsg, a => a, CancellationToken);

		result.RoutingDecisions.Count.AssertGreater(0, "Should have routing decisions");
		result.Handled.AssertTrue("Should be handled");

		// Verify child mapping
		var childTransId = ((ISubscriptionMessage)result.RoutingDecisions[0].Message).TransactionId;
		parentChildMap.TryGetParent(childTransId, out var parentId).AssertTrue("Should have mapping");
		parentId.AssertEqual(transId, "Parent should match");
	}

	#endregion
}
