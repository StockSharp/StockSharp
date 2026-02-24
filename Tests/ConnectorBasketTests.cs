namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Connector tests with injected basket state objects for white-box validation.
/// Each test is an analog of AsyncExtensionsTests Connector tests,
/// but validates internal BasketMessageAdapter state at every step.
/// </summary>
[TestClass]
public class ConnectorBasketTests : BaseTestClass
{
	#region Infrastructure

	/// <summary>
	/// Subclass to allow setting custom Adapter with injected routing manager.
	/// </summary>
	private sealed class TestConnector : Connector
	{
		public TestConnector(BasketMessageAdapter adapter)
			: base(new InMemorySecurityStorage(), new InMemoryPositionStorage(), new InMemoryExchangeInfoProvider(), initAdapter: false, initChannels: false)
		{
			InMessageChannel = new PassThroughMessageChannel();
			OutMessageChannel = new PassThroughMessageChannel();
			Adapter = adapter;
		}
	}

	/// <summary>
	/// Mock adapter identical to AsyncExtensionsTests.MockAdapter.
	/// </summary>
	private sealed class MockAdapter : MessageAdapter
	{
		public ConcurrentQueue<Message> SentMessages { get; } = [];
		public Dictionary<long, MarketDataMessage> ActiveSubscriptions { get; } = [];
		public Dictionary<long, OrderRegisterMessage> ActiveOrders { get; } = [];
		public long LastSubscribedId { get; private set; }
		public long LastOrderTransactionId { get; private set; }
		public OrderCancelMessage LastCancelMessage { get; private set; }
		public long OrderStatusSubscriptionId { get; private set; }

		public override bool UseInChannel => false;
		public override bool UseOutChannel => false;

		public MockAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMarketDataType(DataType.Level1);
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || dataType == DataType.Transactions;

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			SentMessages.Enqueue(message);

			switch (message.Type)
			{
				case MessageTypes.Connect:
					await SendOutMessageAsync(new ConnectMessage(), cancellationToken);
					break;
				case MessageTypes.Disconnect:
					await SendOutMessageAsync(new DisconnectMessage(), cancellationToken);
					break;
				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					if (mdMsg.IsSubscribe)
					{
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
						ActiveSubscriptions[mdMsg.TransactionId] = mdMsg;
						LastSubscribedId = mdMsg.TransactionId;
						if (mdMsg.To == null)
							await SendSubscriptionResultAsync(mdMsg, cancellationToken);
					}
					else
					{
						ActiveSubscriptions.Remove(mdMsg.OriginalTransactionId);
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
					}
					break;
				}
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					ActiveOrders[regMsg.TransactionId] = regMsg;
					LastOrderTransactionId = regMsg.TransactionId;
					break;
				}
				case MessageTypes.OrderCancel:
				{
					LastCancelMessage = (OrderCancelMessage)message;
					break;
				}
				case MessageTypes.OrderStatus:
				{
					var osm = (OrderStatusMessage)message;
					if (osm.IsSubscribe)
					{
						OrderStatusSubscriptionId = osm.TransactionId;
						await SendOutMessageAsync(osm.CreateResponse(), cancellationToken);
						await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = osm.TransactionId }, cancellationToken);
					}
					break;
				}
				case MessageTypes.Reset:
				{
					ActiveSubscriptions.Clear();
					ActiveOrders.Clear();
					break;
				}
			}
		}

		public async ValueTask SimulateData(long subscriptionId, Message data, CancellationToken cancellationToken)
		{
			if (data is IOriginalTransactionIdMessage origMsg)
				origMsg.OriginalTransactionId = subscriptionId;
			await SendOutMessageAsync(data, cancellationToken);
		}

		public async ValueTask FinishHistoricalSubscription(long subscriptionId, CancellationToken cancellationToken)
		{
			if (ActiveSubscriptions.TryGetValue(subscriptionId, out var mdMsg))
				await SendOutMessageAsync(mdMsg.CreateResult(), cancellationToken);
		}

		public async ValueTask SimulateOrderExecution(long origTransId, CancellationToken cancellationToken, OrderStates? state = null, long? orderId = null,
			decimal? tradePrice = null, decimal? tradeVolume = null, long? tradeId = null, Exception error = null)
		{
			var exec = new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = origTransId,
				OrderId = orderId,
				OrderState = state,
				TradePrice = tradePrice,
				TradeVolume = tradeVolume,
				TradeId = tradeId,
				Error = error,
				HasOrderInfo = state != null || orderId != null || error != null,
				ServerTime = DateTime.UtcNow,
			};

			if (ActiveOrders.TryGetValue(origTransId, out var regMsg))
				exec.SecurityId = regMsg.SecurityId;

			await SendOutMessageAsync(exec, cancellationToken);
		}
	}

	/// <summary>
	/// Holds all injectable state objects for white-box validation.
	/// </summary>
	private sealed class BasketState
	{
		public AdapterConnectionState ConnectionState { get; } = new();
		public AdapterConnectionManager ConnectionManager { get; }
		public SubscriptionRoutingState SubscriptionRouting { get; } = new();
		public ParentChildMap ParentChildMap { get; } = new();
		public PendingMessageState PendingState { get; } = new();
		public OrderRoutingState OrderRouting { get; } = new();

		public BasketState()
		{
			ConnectionManager = new AdapterConnectionManager(ConnectionState);
		}
	}

	private static (TestConnector connector, MockAdapter adapter, BasketState state) CreateConnectorWithBasketState()
	{
		var state = new BasketState();

		var idGen = new MillisecondIncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var routingManager = new BasketRoutingManager(
			state.ConnectionState,
			state.ConnectionManager,
			state.PendingState,
			state.SubscriptionRouting,
			state.ParentChildMap,
			state.OrderRouting,
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null, null, routingManager);

		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var connector = new TestConnector(basket);

		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		return (connector, adapter, state);
	}

	#endregion

	#region Connection

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_ConnectAsync()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		// --- Before connect ---
		state.ConnectionState.ConnectedCount.AssertEqual(0, "ConnectedCount before connect");
		state.ConnectionState.HasPendingAdapters.AssertFalse("HasPendingAdapters before connect");
		state.PendingState.Count.AssertEqual(0, "PendingState before connect");

		// --- Connect ---
		await connector.ConnectAsync(CancellationToken);

		// --- After connect: validate state ---
		AreEqual(ConnectionStates.Connected, connector.ConnectionState);
		state.ConnectionState.ConnectedCount.AssertEqual(1, "One adapter connected");
		state.ConnectionState.HasPendingAdapters.AssertFalse("No pending adapters after connect");
		state.ConnectionState.AllFailed.AssertFalse("No failures");
		state.PendingState.Count.AssertEqual(0, "No pending messages");

		// --- Disconnect ---
		await connector.DisconnectAsync(CancellationToken);

		// --- After disconnect: validate state ---
		AreEqual(ConnectionStates.Disconnected, connector.ConnectionState);
		state.ConnectionState.AllDisconnectedOrFailed.AssertTrue("All disconnected after disconnect");
		state.ConnectionState.ConnectedCount.AssertEqual(0, "No connected adapters after disconnect");
		state.PendingState.Count.AssertEqual(0);
	}

	#endregion

	#region Order Registration

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_RegisterOrder_Basic()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);

		// --- After connect: validate state ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.ConnectionState.HasPendingAdapters.AssertFalse();
		state.PendingState.Count.AssertEqual(0);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var orderReceived = new List<Order>();
		connector.OrderReceived += (_, o) => orderReceived.Add(o);

		// --- Register order ---
		connector.RegisterOrder(order);

		// With pass-through channels, processing is synchronous
		AreEqual(OrderStates.Pending, order.State);
		AreNotEqual(0L, order.TransactionId);
		adapter.LastOrderTransactionId.AssertNotEqual(0L, "Adapter should have received the order");

		// --- Validate state after registration ---
		state.ConnectionState.ConnectedCount.AssertEqual(1, "Still connected");
		state.PendingState.Count.AssertEqual(0, "No pending messages");

		// orderRouting should have the mapping
		state.OrderRouting.TryGetOrderAdapter(order.TransactionId, out var routedAdapter)
			.AssertTrue("OrderRouting should have transId→adapter mapping");

		// --- Simulate acceptance ---
		await adapter.SimulateOrderExecution(order.TransactionId, CancellationToken, OrderStates.Active, orderId: 123);

		AreEqual(OrderStates.Active, order.State);
		AreEqual(123L, order.Id);

		orderReceived.Count.AssertEqual(1);

		// --- State still consistent ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.OrderRouting.TryGetOrderAdapter(order.TransactionId, out _).AssertTrue("Order mapping preserved");
	}

	#endregion

	#region Subscription Live

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_Subscription_Live()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);

		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.ConnectionState.HasPendingAdapters.AssertFalse();

		var sub = new Subscription(DataType.Level1);

		var got = new List<Level1ChangeMessage>();
		using var enumCts = new CancellationTokenSource();

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub).WithCancellation(enumCts.Token))
			{
				got.Add(l1);
				if (got.Count >= 3)
				{
					enumCts.Cancel();
					break;
				}
			}
		}, CancellationToken);

		await started.Task.WithCancellation(CancellationToken);
		await Task.Delay(200, CancellationToken);

		var id = adapter.LastSubscribedId;
		AreNotEqual(0L, id);

		// --- Validate state after subscription started ---
		state.ConnectionState.ConnectedCount.AssertEqual(1, "Still connected");
		state.PendingState.Count.AssertEqual(0, "No pending messages");
		// subscriptionRouting should have an entry for the subscription
		// (the connector generates its own transId, basket creates child)

		// --- Send data ---
		for (var i = 0; i < 3; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow };
			await adapter.SimulateData(id, l1, CancellationToken);
		}

		await enumerating.WithCancellation(CancellationToken);
		HasCount(3, got);

		// Wait for unsubscribe
		while (!adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id))
			await Task.Delay(10, CancellationToken);

		// --- State after unsubscribe ---
		state.ConnectionState.ConnectedCount.AssertEqual(1, "Still connected after unsub");
		state.PendingState.Count.AssertEqual(0);
	}

	#endregion

	#region Subscription History

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_Subscription_History()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);

		state.ConnectionState.ConnectedCount.AssertEqual(1);

		var sub = new Subscription(DataType.Level1)
		{
			From = DateTime.UtcNow.AddDays(-2),
			To = DateTime.UtcNow.AddDays(-1),
		};

		var got = new List<Level1ChangeMessage>();

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub).WithCancellation(CancellationToken))
				got.Add(l1);
		}, CancellationToken);

		// Wait for subscription to be processed
		await Task.Run(async () =>
		{
			while (adapter.ActiveSubscriptions.Count == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		await Task.Delay(100, CancellationToken);

		var id = adapter.LastSubscribedId;
		AreNotEqual(0L, id);

		// --- Validate state after subscribe ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);

		// --- Send historical data ---
		for (var i = 0; i < 2; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow.AddDays(-1).AddMinutes(i) };
			await adapter.SimulateData(id, l1, CancellationToken);
		}

		await adapter.FinishHistoricalSubscription(id, CancellationToken);

		await enumerating.WithCancellation(CancellationToken);
		HasCount(2, got);

		// --- State after history completed ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
	}

	#endregion

	#region Subscription Lifecycle

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_Subscription_Lifecycle()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);

		state.ConnectionState.ConnectedCount.AssertEqual(1);

		var sub = new Subscription(DataType.Level1);

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token).AsTask();

		await started.Task.WithCancellation(CancellationToken);

		var id = adapter.LastSubscribedId;
		AreNotEqual(0L, id);

		// --- State after subscribe ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);

		// --- Cancel → triggers UnSubscribe ---
		runCts.Cancel();
		await run.WithCancellation(CancellationToken);

		adapter.SentMessages.OfType<MarketDataMessage>().Count(m => !m.IsSubscribe && m.OriginalTransactionId == id).AssertEqual(1);

		// --- State after unsubscribe ---
		state.ConnectionState.ConnectedCount.AssertEqual(1, "Still connected");
		state.PendingState.Count.AssertEqual(0);
	}

	#endregion

	#region RegisterOrderAsync

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_RegisterOrderAsync_OrderAccepted_ReturnsEvents()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);
		state.ConnectionState.ConnectedCount.AssertEqual(1);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var events = new List<(Order order, MyTrade trade)>();
		var allOrderReceived = new List<Order>();
		connector.OrderReceived += (_, o) => allOrderReceived.Add(o);

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAndWaitAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		// Wait for adapter to actually receive the order
		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// --- Validate state after order sent ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
		state.OrderRouting.TryGetOrderAdapter(transId, out _)
			.AssertTrue("OrderRouting should have mapping after registration");

		// Simulate acceptance
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Active, orderId: 123);

		await Task.Run(async () =>
		{
			while (order.State != OrderStates.Active)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		await Task.Delay(100, CancellationToken);

		allOrderReceived.Count.AssertEqual(1,
			$"OrderReceived should have fired. Order state: {order.State}, Id: {order.Id}");

		// --- State after active ---
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue("Mapping preserved after active");

		// Simulate done
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		events.Count.AssertEqual(2, "Should receive Active and Done events");
		AreEqual(OrderStates.Done, order.State);
		AreEqual(123L, order.Id);

		// --- Final state ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_RegisterOrderAsync_WithTrades_ReturnsTradeEvents()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);
		state.ConnectionState.ConnectedCount.AssertEqual(1);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var events = new List<(Order order, MyTrade trade)>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAndWaitAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// --- Validate order routing ---
		state.OrderRouting.TryGetOrderAdapter(transId, out _)
			.AssertTrue("OrderRouting should have mapping");

		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Active, orderId: 123);
		await adapter.SimulateOrderExecution(transId, CancellationToken, orderId: 123, tradePrice: 100.5m, tradeVolume: 5, tradeId: 1001);
		await adapter.SimulateOrderExecution(transId, CancellationToken, orderId: 123, tradePrice: 100.6m, tradeVolume: 5, tradeId: 1002);
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		events.Count.AssertGreater(3, "Should receive events for order states and trades");
		AreEqual(OrderStates.Done, order.State);
		AreEqual(123L, order.Id);

		var tradeEvents = events.Where(e => e.trade != null).ToList();
		tradeEvents.Count.AssertGreater(1, "Should have received trade events");

		var tradePrices = tradeEvents.Select(e => e.trade.Trade.Price).ToList();
		tradePrices.AssertContains(100.5m);
		tradePrices.AssertContains(100.6m);

		// --- Final state ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue("Mapping preserved");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_RegisterOrderAsync_CancellationSendsCancelOrder()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);
		state.ConnectionState.ConnectedCount.AssertEqual(1);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		using var cts = new CancellationTokenSource();
		var events = new List<(Order order, MyTrade trade)>();

		var enumTask = Task.Run(async () =>
		{
			try
			{
				await foreach (var evt in connector.RegisterOrderAndWaitAsync(order).WithCancellation(cts.Token))
				{
					events.Add(evt);
					if (evt.order.State == OrderStates.Active)
						cts.Cancel();
				}
			}
			catch (OperationCanceledException)
			{
				// expected when cts is cancelled
			}
		}, CancellationToken);

		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// Validate order routing
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue("OrderRouting mapping exists");

		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Active, orderId: 456);

		await Task.Run(async () =>
		{
			while (adapter.LastCancelMessage == null)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Done, orderId: 456);

		await enumTask.WithCancellation(CancellationToken);

		adapter.LastCancelMessage.AssertNotNull();
		AreEqual(456L, adapter.LastCancelMessage.OrderId);

		// State after cancel
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_RegisterOrderAsync_FiltersOtherOrders()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);
		state.ConnectionState.ConnectedCount.AssertEqual(1);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var events = new List<(Order order, MyTrade trade)>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAndWaitAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// Validate routing
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue();

		var otherTransId1 = transId + 100;
		var otherTransId2 = transId + 200;

		await adapter.SimulateOrderExecution(otherTransId1, CancellationToken, OrderStates.Active, orderId: 999);
		await adapter.SimulateOrderExecution(otherTransId2, CancellationToken, OrderStates.Active, orderId: 888);
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Active, orderId: 123);
		await adapter.SimulateOrderExecution(otherTransId1, CancellationToken, orderId: 999, tradePrice: 50m, tradeVolume: 5, tradeId: 5001);
		await adapter.SimulateOrderExecution(otherTransId2, CancellationToken, OrderStates.Done, orderId: 888);
		await adapter.SimulateOrderExecution(transId, CancellationToken, orderId: 123, tradePrice: 100.5m, tradeVolume: 5, tradeId: 2001);
		await adapter.SimulateOrderExecution(otherTransId1, CancellationToken, OrderStates.Done, orderId: 999);
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		events.Count.AssertEqual(4, "Should receive Active, Trade, order update and Done events for our order");
		AreEqual(OrderStates.Done, order.State);
		AreEqual(123L, order.Id);

		var tradeEvents = events.Where(e => e.trade != null).ToList();
		tradeEvents.Count.AssertEqual(1, "Should have received our trade");
		tradeEvents.Count(e => e.trade.Trade.Price == 100.5m).AssertEqual(1, "Should have our trade at 100.5");
		tradeEvents.Count(e => e.trade.Trade.Price == 50m).AssertEqual(0, "Should NOT have trade from other order");

		// State
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue("Our order mapping preserved");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task ConnectorBasket_RegisterOrderAsync_FullLifecycle()
	{
		var (connector, adapter, state) = CreateConnectorWithBasketState();

		await connector.ConnectAsync(CancellationToken);
		state.ConnectionState.ConnectedCount.AssertEqual(1);

		var security = new Security { Id = "SBER@TQBR" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "TestPortfolio" },
			Price = 250,
			Volume = 100,
			Side = Sides.Buy,
		};

		var events = new List<(Order order, MyTrade trade)>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAndWaitAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// --- State after registration ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue("OrderRouting has mapping");

		// Full lifecycle
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Pending);
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Active, orderId: 12345);
		await adapter.SimulateOrderExecution(transId, CancellationToken, orderId: 12345, tradePrice: 249.5m, tradeVolume: 30, tradeId: 3001);
		await adapter.SimulateOrderExecution(transId, CancellationToken, orderId: 12345, tradePrice: 249.8m, tradeVolume: 50, tradeId: 3002);
		await adapter.SimulateOrderExecution(transId, CancellationToken, orderId: 12345, tradePrice: 250.0m, tradeVolume: 20, tradeId: 3003);
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Done, orderId: 12345);

		await enumTask.WithCancellation(CancellationToken);

		events.Count.AssertGreater(5, "Should receive events for states and trades");
		AreEqual(OrderStates.Done, order.State);
		AreEqual(12345L, order.Id);

		var tradeEvents = events.Where(e => e.trade != null).ToList();
		tradeEvents.Count.AssertGreater(2, "Should have received all 3 trades");

		var tradePrices = tradeEvents.Select(e => e.trade.Trade.Price).ToList();
		tradePrices.AssertContains(249.5m);
		tradePrices.AssertContains(249.8m);
		tradePrices.AssertContains(250.0m);

		var totalVolume = tradeEvents.Sum(e => e.trade.Trade.Volume);
		AreEqual(100m, totalVolume);

		// --- Final state ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue("Mapping preserved after full lifecycle");
	}

	#endregion

	#region Candle Pipeline — Live Subscription Bug

	/// <summary>
	/// Mock adapter supporting TF candles + ticks. Records all MarketData subscriptions.
	/// Auto-responds: Response OK → Online (if live) or Finished (if history-only).
	/// </summary>
	private sealed class CandleMockAdapter : MessageAdapter
	{
		public ConcurrentQueue<MarketDataMessage> RecordedSubscriptions { get; } = [];

		public CandleMockAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMarketDataType(TimeSpan.FromMinutes(5).TimeFrame());
			this.AddSupportedMarketDataType(DataType.Ticks);
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || dataType == DataType.Transactions;

		public override bool UseInChannel => false;
		public override bool UseOutChannel => false;

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
					await SendOutMessageAsync(new ConnectMessage(), cancellationToken);
					break;
				case MessageTypes.Disconnect:
					await SendOutMessageAsync(new DisconnectMessage(), cancellationToken);
					break;
				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					if (mdMsg.IsSubscribe)
					{
						RecordedSubscriptions.Enqueue(mdMsg.TypedClone());
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
						// Online if live (To==null), Finished if history (To!=null)
						await SendSubscriptionResultAsync(mdMsg, cancellationToken);
					}
					else
					{
						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
					}
					break;
				}
				case MessageTypes.OrderStatus:
				{
					var osm = (OrderStatusMessage)message;
					if (osm.IsSubscribe)
					{
						await SendOutMessageAsync(osm.CreateResponse(), cancellationToken);
						await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = osm.TransactionId }, cancellationToken);
					}
					break;
				}
				case MessageTypes.Reset:
					break;
			}
		}
	}

	private static (TestConnector connector, CandleMockAdapter adapter, BasketState state) CreateConnectorForCandleTest()
	{
		var state = new BasketState();

		var idGen = new MillisecondIncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var routingManager = new BasketRoutingManager(
			state.ConnectionState,
			state.ConnectionManager,
			state.PendingState,
			state.SubscriptionRouting,
			state.ParentChildMap,
			state.OrderRouting,
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null, null, routingManager);

		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var connector = new TestConnector(basket);

		var adapter = new CandleMockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		return (connector, adapter, state);
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task CandleHistLive_LiveSubscriptionShouldReachAdapter()
	{
		// Bug repro: user subscribes to candles with From + To=null (hist+live).
		// CandleBuilderManager always caps To=CurrentTime when IsSupportCandlesUpdates=false,
		// so the actual adapter ONLY ever sees history-only candle subscriptions.
		// After history finishes, it transitions to build from ticks —
		// a live TICKS subscription arrives, but a live CANDLE subscription never does.
		//
		// The pipeline is allowed to split hist+live into two phases:
		//   Phase 1: candle subscription with To set (history-only)
		//   Phase 2: candle subscription with To=null (live)
		// We wait for the FULL cycle to complete before checking.

		var (connector, adapter, _) = CreateConnectorForCandleTest();

		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var sub = new Subscription(TimeSpan.FromMinutes(5).TimeFrame(), security)
		{
			From = DateTime.UtcNow.AddDays(-1),
			// To = null → hist+live
		};

		using var runCts = new CancellationTokenSource();

		_ = Task.Run(async () =>
		{
			try
			{
				await foreach (var _ in connector.SubscribeAsync<CandleMessage>(sub).WithCancellation(runCts.Token))
				{ }
			}
			catch (OperationCanceledException) { }
		}, CancellationToken);

		// Wait for the full pipeline cycle to complete:
		// 1) candle subscription (history-only, To capped) arrives at adapter
		// 2) adapter responds Finished → CandleBuilderManager transitions to Compress
		// 3) ticks subscription (live) arrives at adapter
		// Ticks subscription = proof the full cycle completed.
		// After that we also check if a live candle subscription arrived (as 2nd phase).
		await Task.Run(async () =>
		{
			while (!adapter.RecordedSubscriptions.Any(m => m.DataType2 == DataType.Ticks && !m.IsHistoryOnly()))
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		// Small extra delay: if pipeline sends a 2nd candle sub (live) it would arrive around the same time
		await Task.Delay(500, CancellationToken);

		var allSubs = adapter.RecordedSubscriptions.ToList();

		// The pipeline COULD legitimately split into:
		//   1st: candle TF 5min (To=CurrentTime, history-only)
		//   2nd: candle TF 5min (To=null, live)              ← this is what we expect but never arrives
		// Instead what actually happens:
		//   1st: candle TF 5min (To=CurrentTime, history-only)
		//   2nd: Ticks (To=null, live)                        ← redirect to ticks, not candles

		var liveCandleSub = allSubs.FirstOrDefault(m => m.DataType2.IsTFCandles && !m.IsHistoryOnly());

		// Fails — proving the bug: adapter never receives a live candle subscription
		IsNotNull(liveCandleSub,
			$"Expected a live candle subscription (To=null) at the adapter, but none arrived. " +
			$"Subscriptions: [{string.Join("; ", allSubs.Select(m =>
				$"{m.DataType2}(histOnly={m.IsHistoryOnly()}, To={m.To?.ToString("HH:mm:ss") ?? "null"})"))}]");

		runCts.Cancel();
	}

	#endregion
}
