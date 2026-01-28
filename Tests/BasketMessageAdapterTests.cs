namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

[TestClass]
public class BasketMessageAdapterTests : BaseTestClass
{
	private sealed class TestBasketInnerAdapter : MessageAdapter
	{
		private readonly ConcurrentQueue<Message> _inMessages = new();

		public TestBasketInnerAdapter(IdGenerator idGen)
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
		}

		public IReadOnlyList<Message> ReceivedMessages => [.. _inMessages];
		public IEnumerable<T> GetMessages<T>() where T : Message => _inMessages.OfType<T>();
		public bool AutoRespond { get; set; } = true;
		public Exception ConnectError { get; set; }

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken ct)
		{
			_inMessages.Enqueue(message.TypedClone());

			if (!AutoRespond)
				return default;

			switch (message.Type)
			{
				case MessageTypes.Reset:
					SendOutMessage(new ResetMessage());
					break;
				case MessageTypes.Connect:
					SendOutMessage(ConnectError != null
						? new ConnectMessage { Error = ConnectError }
						: new ConnectMessage());
					break;
				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					break;
				case MessageTypes.MarketData:
				{
					var md = (MarketDataMessage)message;
					SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = md.TransactionId });
					if (md.IsSubscribe)
						SendOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = md.TransactionId });
					break;
				}
				case MessageTypes.SecurityLookup:
				{
					var sl = (SecurityLookupMessage)message;
					SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = sl.TransactionId });
					SendOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = sl.TransactionId });
					break;
				}
				case MessageTypes.PortfolioLookup:
				{
					var pl = (PortfolioLookupMessage)message;
					SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = pl.TransactionId });
					SendOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = pl.TransactionId });
					break;
				}
				case MessageTypes.OrderStatus:
				{
					var os = (OrderStatusMessage)message;
					SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = os.TransactionId });
					SendOutMessage(new SubscriptionOnlineMessage { OriginalTransactionId = os.TransactionId });
					break;
				}
				case MessageTypes.OrderRegister:
				{
					var reg = (OrderRegisterMessage)message;
					SendOutMessage(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = reg.SecurityId,
						OriginalTransactionId = reg.TransactionId,
						OrderState = OrderStates.Active,
						HasOrderInfo = true,
						ServerTime = DateTime.UtcNow,
						LocalTime = DateTime.UtcNow,
					});
					break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancel = (OrderCancelMessage)message;
					SendOutMessage(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = cancel.SecurityId,
						OriginalTransactionId = cancel.TransactionId,
						OrderState = OrderStates.Done,
						HasOrderInfo = true,
						ServerTime = DateTime.UtcNow,
						LocalTime = DateTime.UtcNow,
					});
					break;
				}
			}

			return default;
		}

		public void EmitOut(Message msg) => SendOutMessage(msg);

		public override IMessageAdapter Clone() => new TestBasketInnerAdapter(TransactionIdGenerator);
	}

	#region Helpers

	private static readonly SecurityId _secId1 = "AAPL@NASDAQ".ToSecurityId();
	private static readonly SecurityId _secId2 = "SBER@MOEX".ToSecurityId();
	private const string _portfolio1 = "Portfolio1";
	private const string _portfolio2 = "Portfolio2";

	private ConcurrentQueue<Message> _outMessages;

	private (BasketMessageAdapter basket, TestBasketInnerAdapter adapter1, TestBasketInnerAdapter adapter2)
		CreateBasket(
			IAdapterConnectionState connectionState = null,
			IAdapterConnectionManager connectionManager = null,
			IPendingMessageState pendingState = null,
			ISubscriptionRoutingState subscriptionRouting = null,
			IParentChildMap parentChildMap = null,
			IOrderRoutingState orderRouting = null,
			bool twoAdapters = true)
	{
		var idGen = new IncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		// Create routing manager with optional injected state
		var cs = connectionState ?? new AdapterConnectionState();
		var cm = connectionManager ?? new AdapterConnectionManager(cs);
		var ps = pendingState ?? new PendingMessageState();
		var sr = subscriptionRouting ?? new SubscriptionRoutingState();
		var pcm = parentChildMap ?? new ParentChildMap();
		var or = orderRouting ?? new OrderRoutingState();

		var routingManager = new BasketRoutingManager(
			cs, cm, ps, sr, pcm, or,
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null,
			null,
			routingManager);

		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var adapter1 = new TestBasketInnerAdapter(idGen);
		basket.InnerAdapters.Add(adapter1);
		basket.ApplyHeartbeat(adapter1, false);

		TestBasketInnerAdapter adapter2 = null;

		if (twoAdapters)
		{
			adapter2 = new TestBasketInnerAdapter(idGen);
			basket.InnerAdapters.Add(adapter2);
			basket.ApplyHeartbeat(adapter2, false);
		}

		_outMessages = new ConcurrentQueue<Message>();
		basket.NewOutMessageAsync += (msg, ct) =>
		{
			_outMessages.Enqueue(msg);
			return default;
		};

		return (basket, adapter1, adapter2);
	}

	private static async Task SendToBasket(BasketMessageAdapter basket, Message message, CancellationToken ct = default)
	{
		await ((IMessageTransport)basket).SendInMessageAsync(message, ct);
	}

	private T[] GetOut<T>() where T : Message
		=> [.. _outMessages.OfType<T>()];

	private void ClearOut() => _outMessages = new();

	#endregion

	#region 1. Connection

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

		// Act: send Connect
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// Verify connection state after connect
		connectionState.ConnectedCount.AssertEqual(1, "ConnectedCount should be 1");
		connectionState.HasPendingAdapters.AssertFalse("No adapters should be pending");

		// Verify inner adapter received Connect (Reset only sent to existing wrappers, none on first connect)
		adapter1.GetMessages<ConnectMessage>().Any().AssertTrue("Adapter should receive ConnectMessage");

		// Verify basket emitted ConnectMessage
		var connectOuts = GetOut<ConnectMessage>();
		connectOuts.Length.AssertGreater(0, "Basket should emit ConnectMessage");
		connectOuts.First().Error.AssertNull("ConnectMessage should have no error");
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

		// Act
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// Both adapters auto-respond synchronously, so both should be connected
		connectionState.ConnectedCount.AssertEqual(2, "Both adapters should be connected");

		// With OnFirstAdapter=true, ConnectMessage should have been emitted after first adapter connected
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

		// Act
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// Both connected
		connectionState.ConnectedCount.AssertEqual(2, "Both adapters should be connected");
		connectionState.HasPendingAdapters.AssertFalse("No adapters should be pending");

		// ConnectMessage emitted after all connected
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

		// Act
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// Verify states
		connectionState.AllFailed.AssertTrue("All adapters should be failed");
		connectionState.ConnectedCount.AssertEqual(0, "No adapters should be connected");

		// Basket should emit ConnectMessage with error
		var connectOuts = GetOut<ConnectMessage>();
		connectOuts.Length.AssertGreater(0, "Basket should emit ConnectMessage");
		connectOuts.Any(c => c.Error != null).AssertTrue("ConnectMessage should contain error");
	}

	#endregion

	#region 2. MarketData Routing

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_RoutesToCorrectAdapter_BySecurityProvider()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager,
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap);

		// Connect
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(2);
		ClearOut();

		// Map _secId1 → adapter1
		basket.SecurityAdapterProvider.SetAdapter(_secId1, null, adapter1.Id);

		// Act: subscribe to _secId1
		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// Verify: subscription routing recorded
		subscriptionRouting.TryGetSubscription(transId, out _, out var adapters, out _)
			.AssertTrue("Subscription should be recorded");

		// Verify: adapter1 received MarketDataMessage, adapter2 did not
		adapter1.GetMessages<MarketDataMessage>().Any().AssertTrue("Adapter1 should receive MarketDataMessage");

		// adapter2 should NOT have received a MarketDataMessage (only Reset+Connect)
		adapter2.GetMessages<MarketDataMessage>().Any().AssertFalse("Adapter2 should NOT receive MarketDataMessage");

		// Verify: SubscriptionResponse came out with parent transId
		GetOut<SubscriptionResponseMessage>().Any(m => m.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit SubscriptionResponseMessage with parent transId");

		// Verify: SubscriptionOnline came out
		GetOut<SubscriptionOnlineMessage>().Any(m => m.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit SubscriptionOnlineMessage with parent transId");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_NoMapping_GoesToAllAdapters()
	{
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, adapter2) = CreateBasket(
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		// No security mapping — subscribe to Ticks for secId1, no SecurityAdapterProvider mapping
		// Both adapters support Ticks, so basket should route to first matching adapter
		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// At least one adapter should receive MarketDataMessage
		var a1Got = adapter1.GetMessages<MarketDataMessage>().Any();
		var a2Got = adapter2.GetMessages<MarketDataMessage>().Any();
		(a1Got || a2Got).AssertTrue("At least one adapter should receive MarketDataMessage");

		// Subscription routing should be recorded
		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertTrue("Subscription should be recorded in routing state");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_Subscribe_AdapterReceivesMessage()
	{
		var subscriptionRouting = new SubscriptionRoutingState();

		var (basket, adapter1, _) = CreateBasket(
			subscriptionRouting: subscriptionRouting,
			twoAdapters: false);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// Verify adapter received MarketDataMessage
		var received = adapter1.GetMessages<MarketDataMessage>().ToArray();
		received.Length.AssertGreater(0, "Adapter must receive MarketDataMessage");

		// For single adapter, basket uses SendRequest directly (no ToChild),
		// so subscriptionRouting should have the subscription recorded
		subscriptionRouting.TryGetSubscription(transId, out _, out var adapters, out _)
			.AssertTrue("SubscriptionRouting should record the subscription");

		// subscriptionRouting should also have a request tracked
		var childTransId = received.First().TransactionId;
		subscriptionRouting.TryGetRequest(childTransId, out _, out var routedAdapter)
			.AssertTrue("SubscriptionRouting should track the request");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_ResponseBubblesUp()
	{
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, _) = CreateBasket(
			parentChildMap: parentChildMap,
			twoAdapters: false);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		var mdMsg = new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		};
		await SendToBasket(basket, mdMsg, TestContext.CancellationToken);

		// SubscriptionResponseMessage with parent transId
		var responses = GetOut<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == transId && r.Error == null)
			.AssertTrue("Basket should bubble up SubscriptionResponseMessage with parent transId");

		// SubscriptionOnlineMessage with parent transId
		var onlines = GetOut<SubscriptionOnlineMessage>();
		onlines.Any(r => r.OriginalTransactionId == transId)
			.AssertTrue("Basket should bubble up SubscriptionOnlineMessage with parent transId");
	}

	#endregion

	#region 3. Order Routing

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderRegister_RoutesToCorrectAdapter_ByPortfolioProvider()
	{
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, adapter2) = CreateBasket(
			orderRouting: orderRouting);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		// Map portfolio → adapter1
		basket.PortfolioAdapterProvider.SetAdapter(_portfolio1, adapter1);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId1,
			PortfolioName = _portfolio1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			TransactionId = transId,
		};
		await SendToBasket(basket, regMsg, TestContext.CancellationToken);

		// Verify: orderRouting recorded the mapping
		orderRouting.TryGetOrderAdapter(transId, out var routedAdapter)
			.AssertTrue("OrderRouting should have transId→adapter mapping");
		routedAdapter.AssertEqual(adapter1, "Order should be routed to adapter1");

		// Verify: adapter1 received OrderRegisterMessage
		adapter1.GetMessages<OrderRegisterMessage>().Any().AssertTrue("Adapter1 should receive OrderRegisterMessage");

		// Verify: adapter2 did NOT receive OrderRegisterMessage
		adapter2.GetMessages<OrderRegisterMessage>().Any().AssertFalse("Adapter2 should NOT receive OrderRegisterMessage");

		// Verify: basket emitted ExecutionMessage (adapter auto-responds with order accepted)
		var execMsgs = GetOut<ExecutionMessage>();
		execMsgs.Any(e => e.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit ExecutionMessage for the order");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderCancel_RoutesToSameAdapter_AsOriginalOrder()
	{
		var orderRouting = new OrderRoutingState();

		var (basket, adapter1, adapter2) = CreateBasket(
			orderRouting: orderRouting);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		// Map portfolio → adapter1
		basket.PortfolioAdapterProvider.SetAdapter(_portfolio1, adapter1);

		// Step 1: Register order
		var regTransId = basket.TransactionIdGenerator.GetNextId();
		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId1,
			PortfolioName = _portfolio1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			TransactionId = regTransId,
		};
		await SendToBasket(basket, regMsg, TestContext.CancellationToken);

		// Verify: order routed to adapter1
		orderRouting.TryGetOrderAdapter(regTransId, out var routedAdapter).AssertTrue();
		routedAdapter.AssertEqual(adapter1);
		ClearOut();

		// Step 2: Cancel order
		var cancelTransId = basket.TransactionIdGenerator.GetNextId();
		var cancelMsg = new OrderCancelMessage
		{
			SecurityId = _secId1,
			PortfolioName = _portfolio1,
			TransactionId = cancelTransId,
			OriginalTransactionId = regTransId,
		};
		await SendToBasket(basket, cancelMsg, TestContext.CancellationToken);

		// Verify: adapter1 received OrderCancelMessage (same adapter as original order)
		adapter1.GetMessages<OrderCancelMessage>().Any().AssertTrue("Adapter1 should receive OrderCancelMessage");

		// Verify: adapter2 did NOT receive OrderCancelMessage
		adapter2.GetMessages<OrderCancelMessage>().Any().AssertFalse("Adapter2 should NOT receive OrderCancelMessage");

		// Verify: basket emitted ExecutionMessage with Done
		GetOut<ExecutionMessage>().Any(e => e.OriginalTransactionId == cancelTransId && e.OrderState == OrderStates.Done)
			.AssertTrue("Basket should emit ExecutionMessage with Done state");
	}

	#endregion

	#region 4. SecurityLookup

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SecurityLookup_GoesToAllAdapters()
	{
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();

		var (basket, adapter1, adapter2) = CreateBasket(
			subscriptionRouting: subscriptionRouting,
			parentChildMap: parentChildMap);

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage
		{
			TransactionId = transId,
			SecurityId = _secId1,  // non-empty to avoid IsLookupAll filter
		};
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		// Both adapters should receive SecurityLookupMessage
		adapter1.GetMessages<SecurityLookupMessage>().Any().AssertTrue("Adapter1 should receive SecurityLookupMessage");
		adapter2.GetMessages<SecurityLookupMessage>().Any().AssertTrue("Adapter2 should receive SecurityLookupMessage");

		// Subscription routing was recorded and then cleaned up by SubscriptionFinishedMessage auto-response,
		// so we verify via output: basket should emit SubscriptionFinishedMessage with parent transId
		GetOut<SubscriptionFinishedMessage>().Any(m => m.OriginalTransactionId == transId)
			.AssertTrue("Basket should emit SubscriptionFinishedMessage with parent transId");
	}

	#endregion

	#region 5. Reset

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

		// Connect first
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(1);

		// Subscribe to something to populate state
		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		// Verify state is populated
		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _).AssertTrue();
		ClearOut();

		// Act: Reset
		await SendToBasket(basket, new ResetMessage(), TestContext.CancellationToken);

		// Verify: subscription routing cleared
		subscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertFalse("Subscription routing should be cleared after reset");

		// Verify: connection manager reset
		connectionState.ConnectedCount.AssertEqual(0, "ConnectedCount should be 0 after reset");
	}

	#endregion

	#region 6. Pending Messages

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

		// Adapter does NOT auto-respond to Connect (simulates slow connection)
		adapter1.AutoRespond = false;

		// Send connect but adapter won't reply yet
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// State: connecting, adapter pending
		connectionState.HasPendingAdapters.AssertTrue("Adapter should be pending");

		// Send a SecurityLookup while still connecting
		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage
		{
			TransactionId = transId,
		};
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		// Verify: message was pended
		pendingState.Count.AssertGreater(0, "Pending state should have messages");

		// Adapter should NOT have received SecurityLookupMessage
		adapter1.GetMessages<SecurityLookupMessage>().Any()
			.AssertFalse("Adapter should NOT receive SecurityLookupMessage while connecting");

		// Now simulate adapter connecting: emit ConnectMessage
		adapter1.AutoRespond = true;
		adapter1.EmitOut(new ConnectMessage());

		// After connect, pending messages should be drained
		connectionState.ConnectedCount.AssertEqual(1);
		pendingState.Count.AssertEqual(0, "Pending state should be empty after connect");
	}

	#endregion

	#region 7. Disconnect

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Disconnect_AllAdaptersDisconnected_ReturnsDisconnectMessage()
	{
		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);

		var (basket, adapter1, adapter2) = CreateBasket(
			connectionState: connectionState,
			connectionManager: connectionManager);

		// Connect first
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(2);
		ClearOut();

		// Act: Disconnect
		await SendToBasket(basket, new DisconnectMessage(), TestContext.CancellationToken);

		// Verify adapters received DisconnectMessage
		adapter1.GetMessages<DisconnectMessage>().Any().AssertTrue("Adapter1 should receive DisconnectMessage");
		adapter2.GetMessages<DisconnectMessage>().Any().AssertTrue("Adapter2 should receive DisconnectMessage");

		// Verify state
		connectionState.AllDisconnectedOrFailed.AssertTrue("All adapters should be disconnected or failed");

		// Verify basket emitted DisconnectMessage
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

		// Connect
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		connectionState.ConnectedCount.AssertEqual(1);

		// Subscribe
		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);
		ClearOut();

		// Act: Disconnect
		await SendToBasket(basket, new DisconnectMessage(), TestContext.CancellationToken);

		// After disconnect: message type routing tables should be cleaned
		// Attempting to send another message should not route to any adapter
		connectionState.AllDisconnectedOrFailed.AssertTrue("All adapters should be disconnected");
	}

	#endregion
}
