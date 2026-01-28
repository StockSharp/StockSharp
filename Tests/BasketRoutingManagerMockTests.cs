namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Mock tests for BasketMessageAdapter with injected routing manager.
/// Tests that BasketMessageAdapter correctly uses the routing manager.
/// </summary>
[TestClass]
public class BasketRoutingManagerMockTests : BaseTestClass
{
	#region Test Adapter

	private sealed class TestMockInnerAdapter : MessageAdapter
	{
		private readonly ConcurrentQueue<Message> _inMessages = new();

		public TestMockInnerAdapter(IdGenerator idGen)
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
			}

			return default;
		}

		public void EmitOut(Message msg) => SendOutMessage(msg);

		public override IMessageAdapter Clone() => new TestMockInnerAdapter(TransactionIdGenerator);
	}

	#endregion

	#region Helpers

	private static readonly SecurityId _secId1 = "AAPL@NASDAQ".ToSecurityId();
	private ConcurrentQueue<Message> _outMessages;

	private (BasketMessageAdapter basket, TestMockInnerAdapter adapter1, IBasketRoutingManager routingManager)
		CreateBasketWithMock()
	{
		var idGen = new IncrementalIdGenerator();

		var connectionState = new AdapterConnectionState();
		var connectionManager = new AdapterConnectionManager(connectionState);
		var subscriptionRouting = new SubscriptionRoutingState();
		var parentChildMap = new ParentChildMap();
		var pendingState = new PendingMessageState();
		var pendingManager = new PendingMessageManager(pendingState);
		var orderRouting = new OrderRoutingState();

		// Use real BasketRoutingManager for integration tests
		var routingManager = new BasketRoutingManager(
			connectionState,
			connectionManager,
			pendingState,
			pendingManager,
			subscriptionRouting,
			parentChildMap,
			orderRouting,
			a => a, // GetUnderlyingAdapter - simplified for tests
			new CandleBuilderProvider(new InMemoryExchangeInfoProvider()),
			() => false,
			idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			new CandleBuilderProvider(new InMemoryExchangeInfoProvider()),
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null,
			connectionState,
			connectionManager,
			pendingState,
			pendingManager,
			subscriptionRouting,
			parentChildMap,
			orderRouting,
			null,
			routingManager);

		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var adapter1 = new TestMockInnerAdapter(idGen);
		basket.InnerAdapters.Add(adapter1);
		basket.ApplyHeartbeat(adapter1, false);

		_outMessages = new ConcurrentQueue<Message>();
		basket.NewOutMessageAsync += (msg, ct) =>
		{
			_outMessages.Enqueue(msg);
			return default;
		};

		return (basket, adapter1, routingManager);
	}

	private static async Task SendToBasket(BasketMessageAdapter basket, Message message, CancellationToken ct = default)
	{
		await ((IMessageTransport)basket).SendInMessageAsync(message, ct);
	}

	private T[] GetOut<T>() where T : Message
		=> [.. _outMessages.OfType<T>()];

	private void ClearOut() => _outMessages = new();

	#endregion

	#region RoutingManager Property Access

	[TestMethod]
	public void BasketMessageAdapter_HasRoutingManagerProperty()
	{
		var (basket, _, routingManager) = CreateBasketWithMock();

		basket.RoutingManager.AssertEqual(routingManager, "RoutingManager should be injected");
	}

	[TestMethod]
	public void BasketMessageAdapter_NoRoutingManager_ReturnsNull()
	{
		var idGen = new IncrementalIdGenerator();
		var basket = new BasketMessageAdapter(
			idGen,
			new CandleBuilderProvider(new InMemoryExchangeInfoProvider()),
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null);

		basket.RoutingManager.AssertNull("RoutingManager should be null when not injected");
	}

	#endregion

	#region Connect/Disconnect State Updates

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Connect_UpdatesConnectionState()
	{
		var (basket, adapter1, routingManager) = CreateBasketWithMock();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// Verify connection state updated
		routingManager.ConnectionState.ConnectedCount.AssertEqual(1, "Should have 1 connected adapter");
		routingManager.HasPendingAdapters.AssertFalse("No adapters should be pending");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Disconnect_UpdatesConnectionState()
	{
		var (basket, adapter1, routingManager) = CreateBasketWithMock();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		routingManager.ConnectionState.ConnectedCount.AssertEqual(1);
		ClearOut();

		await SendToBasket(basket, new DisconnectMessage(), TestContext.CancellationToken);

		routingManager.ConnectionState.AllDisconnectedOrFailed.AssertTrue("All should be disconnected");
	}

	#endregion

	#region Subscription Routing State Integration

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task MarketData_Subscribe_UsesSubscriptionRouting()
	{
		var (basket, adapter1, mockManager) = CreateBasketWithMock();

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

		// Verify subscription was recorded (using manager's state)
		mockManager.SubscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertTrue("Subscription should be recorded");

		// Verify child mapping created (since we use ToChild path)
		var adapterMsgs = adapter1.GetMessages<MarketDataMessage>().ToArray();
		adapterMsgs.Length.AssertGreater(0, "Adapter should receive message");

		var childTransId = adapterMsgs[0].TransactionId;
		mockManager.ParentChildMap.TryGetParent(childTransId, out var parentId)
			.AssertTrue("Child mapping should exist");
		parentId.AssertEqual(transId, "Parent should match");
	}

	#endregion

	#region Order Routing State Integration

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task OrderRegister_RecordsInOrderRouting()
	{
		var (basket, adapter1, routingManager) = CreateBasketWithMock();

		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		ClearOut();

		// Set up portfolio mapping
		basket.PortfolioAdapterProvider.SetAdapter("Portfolio1", adapter1);

		var transId = basket.TransactionIdGenerator.GetNextId();
		var regMsg = new OrderRegisterMessage
		{
			SecurityId = _secId1,
			PortfolioName = "Portfolio1",
			Side = Sides.Buy,
			Price = 100,
			Volume = 1,
			TransactionId = transId,
		};
		await SendToBasket(basket, regMsg, TestContext.CancellationToken);

		// Verify order adapter recorded
		routingManager.TryGetOrderAdapter(transId, out var routedAdapter)
			.AssertTrue("Order should be recorded in routing");
	}

	#endregion

	#region Pending Messages Integration

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Message_BeforeConnect_PendedInState()
	{
		var (basket, adapter1, mockManager) = CreateBasketWithMock();

		// Don't auto respond to keep adapter in Connecting state
		adapter1.AutoRespond = false;

		// Start connection but don't complete
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);

		// Adapter is connecting
		mockManager.HasPendingAdapters.AssertTrue("Adapter should be pending");

		// Send a message that should be pended
		var transId = basket.TransactionIdGenerator.GetNextId();
		var lookupMsg = new SecurityLookupMessage { TransactionId = transId };
		await SendToBasket(basket, lookupMsg, TestContext.CancellationToken);

		// Verify message was pended (using manager's state)
		mockManager.PendingState.Count.AssertGreater(0, "Message should be in pending state");

		// Adapter should NOT have received the lookup
		adapter1.GetMessages<SecurityLookupMessage>().Any()
			.AssertFalse("Adapter should not receive message while connecting");
	}

	#endregion

	#region ParentChildMap Integration

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task SubscriptionResponse_RemapsChildToParent()
	{
		var (basket, adapter1, mockManager) = CreateBasketWithMock();

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

		// Find the child transaction ID sent to adapter
		var adapterMsgs = adapter1.GetMessages<MarketDataMessage>().ToArray();
		adapterMsgs.Length.AssertGreater(0, "Adapter should receive message");
		var childTransId = adapterMsgs[0].TransactionId;

		// Verify response was remapped to parent
		var responses = GetOut<SubscriptionResponseMessage>();
		responses.Any(r => r.OriginalTransactionId == transId)
			.AssertTrue("Response should have parent transId");

		var onlines = GetOut<SubscriptionOnlineMessage>();
		onlines.Any(o => o.OriginalTransactionId == transId)
			.AssertTrue("Online should have parent transId");
	}

	#endregion

	#region Reset Integration

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Reset_ClearsAllRoutingState()
	{
		var (basket, adapter1, mockManager) = CreateBasketWithMock();

		// Connect
		await SendToBasket(basket, new ConnectMessage(), TestContext.CancellationToken);
		mockManager.ConnectionState.ConnectedCount.AssertEqual(1);

		// Subscribe to add some state
		var transId = basket.TransactionIdGenerator.GetNextId();
		await SendToBasket(basket, new MarketDataMessage
		{
			SecurityId = _secId1,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = transId,
		}, TestContext.CancellationToken);

		mockManager.SubscriptionRouting.TryGetSubscription(transId, out _, out _, out _).AssertTrue();
		ClearOut();

		// Reset
		await SendToBasket(basket, new ResetMessage(), TestContext.CancellationToken);

		// Verify state cleared
		mockManager.SubscriptionRouting.TryGetSubscription(transId, out _, out _, out _)
			.AssertFalse("Subscription routing should be cleared");
		mockManager.ConnectionState.ConnectedCount.AssertEqual(0, "Connection count should be 0");
	}

	#endregion
}
