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
	/// Raised by the mock adapters after every message they handle, so a test can wait for the state
	/// it expects to be reached instead of sleeping for a stretch of clock in which it has probably
	/// been reached. A loaded machine then makes a test slower, never green for the wrong reason.
	/// </summary>
	private sealed class ProgressSignal
	{
		private TaskCompletionSource<bool> _next = AsyncHelper.CreateTaskCompletionSource<bool>();

		/// <summary>
		/// Completes on the next signal. Taken before a condition is tested, so a signal raised
		/// between the two still ends the wait instead of being missed and waited out.
		/// </summary>
		public Task Next => Volatile.Read(ref _next).Task;

		/// <summary>
		/// Tell everyone waiting that something happened and their condition is worth re-checking.
		/// </summary>
		public void Raise()
			=> Interlocked.Exchange(ref _next, AsyncHelper.CreateTaskCompletionSource<bool>()).TrySetResult(true);
	}

	/// <summary>
	/// Wait until the condition holds, re-checking it on every signal the adapter raises. The test's
	/// own token ends the wait - its timeout cancels it - and the failure then says what was waited
	/// for and what had arrived instead.
	/// </summary>
	/// <param name="signal">Signal of the adapter whose state is being waited on.</param>
	/// <param name="condition">What the test is waiting for.</param>
	/// <param name="describeFailure">Message to fail with, built when the wait is given up on.</param>
	/// <param name="cancellationToken">Token of the running test.</param>
	private static async Task WaitFor(ProgressSignal signal, Func<bool> condition, Func<string> describeFailure, CancellationToken cancellationToken)
	{
		while (true)
		{
			var next = signal.Next;

			if (condition())
				return;

			try
			{
				await next.WithCancellation(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				Fail(describeFailure());
				return;
			}
		}
	}

	/// <summary>
	/// Subclass to allow setting custom Adapter with injected routing manager.
	/// </summary>
	private sealed class TestConnector : Connector
	{
		public TestConnector(BasketMessageAdapter adapter)
			: this()
		{
			Adapter = adapter;
		}

		/// <summary>
		/// A connector whose adapter chain is assigned as a whole, the way an application composes its own.
		/// </summary>
		public TestConnector(IMessageAdapterWrapper chain)
			: this()
		{
			InnerAdapter = chain;
		}

		private TestConnector()
			: base(new InMemorySecurityStorage(), new InMemoryPositionStorage(), new InMemoryExchangeInfoProvider(), initAdapter: false, initChannels: false)
		{
			InMessageChannel = new PassThroughMessageChannel();
			OutMessageChannel = new PassThroughMessageChannel();
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

		/// <summary>
		/// Signalled once the adapter has finished with a message, so a test waits for what it sent
		/// to have been handled rather than for a while.
		/// </summary>
		public ProgressSignal Progress { get; } = new();

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
			try
			{
				await HandleAsync(message, cancellationToken);
			}
			finally
			{
				// Everything a test waits for here is recorded while the message is handled, so one
				// signal afterwards wakes every waiter, however the handling ended.
				Progress.Raise();
			}
		}

		private async ValueTask HandleAsync(Message message, CancellationToken cancellationToken)
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

						if (mdMsg.To == null)
							await SendSubscriptionResultAsync(mdMsg, cancellationToken);

						// Taken down only once the subscription has been answered in full, so a test
						// that waits for it cannot go on to send data the subscription is not up for.
						ActiveSubscriptions[mdMsg.TransactionId] = mdMsg;
						LastSubscribedId = mdMsg.TransactionId;
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

		// None->Pending (registration) + Pending->Active = 2 OrderReceived.
		orderReceived.Count.AssertEqual(2);

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
		var baseTime = new DateTime(2025, 1, 1, 10, 0, 0).UtcKind();

		// The last of these is only there to be waited for: the channel keeps its order, so any
		// duplicate of an earlier one has to show up in the list before it does.
		var expectedTimes = Enumerable.Range(0, 4).Select(i => baseTime.AddMinutes(i)).ToArray();

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		var arrived = new ProgressSignal();

		var enumerating = Task.Run(async () =>
		{
			try
			{
				await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub).WithCancellation(enumCts.Token))
				{
					got.Add(l1);
					arrived.Raise();
				}
			}
			catch (OperationCanceledException) when (enumCts.IsCancellationRequested) { }
		}, CancellationToken);

		await started.Task.WithCancellation(CancellationToken);

		await WaitFor(adapter.Progress, () => adapter.LastSubscribedId != 0,
			() => "The subscription never reached the adapter.", CancellationToken);

		var id = adapter.LastSubscribedId;
		AreNotEqual(0L, id);

		// --- Validate state after subscription started ---
		state.ConnectionState.ConnectedCount.AssertEqual(1, "Still connected");
		state.PendingState.Count.AssertEqual(0, "No pending messages");
		// subscriptionRouting should have an entry for the subscription
		// (the connector generates its own transId, basket creates child)

		// --- Send data ---
		foreach (var serverTime in expectedTimes)
		{
			var l1 = new Level1ChangeMessage { ServerTime = serverTime };
			await adapter.SimulateData(id, l1, CancellationToken);
		}

		await WaitFor(arrived, () => got.Count >= expectedTimes.Length,
			() => $"Only {got.Count} of the {expectedTimes.Length} sent Level1 messages came out of the subscription.",
			CancellationToken);

		enumCts.Cancel();
		await enumerating.WithCancellation(CancellationToken);
		HasCount(expectedTimes.Length, got);
		got.Select(m => m.ServerTime).SequenceEqual(expectedTimes)
			.AssertTrue("Live basket subscription should preserve order without duplicates");

		// Wait for unsubscribe
		await WaitFor(adapter.Progress,
			() => adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id),
			() => "Cancelling the enumeration never sent an unsubscribe to the adapter.", CancellationToken);

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

		// Wait for the subscription to be processed: the adapter records it and takes down its id
		// while handling the very same message, so one is as good as the other to wait for.
		await WaitFor(adapter.Progress, () => adapter.ActiveSubscriptions.Count > 0,
			() => "The historical subscription never reached the adapter.", CancellationToken);

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

	#region Chain Assigned As A Whole

	[TestMethod]
	public void ConnectorBasket_ChainAssignedAsAWhole_NumbersSubscriptionsByItsBasket()
	{
		var basketIds = new IncrementalIdGenerator { Current = 1000 };

		var basket = new BasketMessageAdapter(
			basketIds,
			new CandleBuilderProvider(new InMemoryExchangeInfoProvider()),
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null);

		using var connector = new TestConnector(new ChannelMessageAdapter(basket, new PassThroughMessageChannel(), new PassThroughMessageChannel())
		{
			OwnInnerAdapter = true,
		});

		var subscription = new Subscription(new SecurityLookupMessage());

		connector.Subscribe(subscription);

		// A subscription sent to a named adapter keeps its number on the way down, where the basket numbers the
		// others: two sequences would meet there.
		AreEqual(1001, subscription.TransactionId);
		AreSame(basketIds, connector.TransactionIdGenerator);
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

		// The registration itself and the acceptance are one update of the order each, so the test
		// waits for the second of them instead of for a while after the first.
		var reported = new ProgressSignal();

		connector.OrderReceived += (_, o) =>
		{
			allOrderReceived.Add(o);
			reported.Raise();
		};

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAndWaitAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		// Wait for adapter to actually receive the order
		await WaitFor(adapter.Progress, () => adapter.LastOrderTransactionId != 0,
			() => "The order never reached the adapter.", CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// --- Validate state after order sent ---
		state.ConnectionState.ConnectedCount.AssertEqual(1);
		state.PendingState.Count.AssertEqual(0);
		state.OrderRouting.TryGetOrderAdapter(transId, out _)
			.AssertTrue("OrderRouting should have mapping after registration");

		// Simulate acceptance
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Active, orderId: 123);

		await WaitFor(reported, () => allOrderReceived.Count >= 2,
			() => $"The order was reported {allOrderReceived.Count} time(s): the registration and the acceptance are two. Order state: {order.State}, Id: {order.Id}",
			CancellationToken);

		AreEqual(OrderStates.Active, order.State);

		allOrderReceived.Count.AssertEqual(2,
			$"OrderReceived should have fired for the registration and for the acceptance, and for nothing else. Order state: {order.State}, Id: {order.Id}");

		// --- State after active ---
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue("Mapping preserved after active");

		// Simulate done
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		events.Count.AssertEqual(3, "Should receive Pending, Active and Done events");
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

		await WaitFor(adapter.Progress, () => adapter.LastOrderTransactionId != 0,
			() => "The order never reached the adapter.", CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// --- Validate order routing ---
		state.OrderRouting.TryGetOrderAdapter(transId, out _)
			.AssertTrue("OrderRouting should have mapping");

		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Active, orderId: 123);
		await adapter.SimulateOrderExecution(transId, CancellationToken, orderId: 123, tradePrice: 100.5m, tradeVolume: 5, tradeId: 1001);
		await adapter.SimulateOrderExecution(transId, CancellationToken, orderId: 123, tradePrice: 100.6m, tradeVolume: 5, tradeId: 1002);
		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		events.Count.AssertEqual(7, "Expected Pending, Active, two order+trade pairs, and Done");
		AreEqual(OrderStates.Done, order.State);
		AreEqual(123L, order.Id);

		var tradeEvents = events.Where(e => e.trade != null).ToList();
		tradeEvents.Count.AssertEqual(2, "Should have received exactly two trade events");

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

		await WaitFor(adapter.Progress, () => adapter.LastOrderTransactionId != 0,
			() => "The order never reached the adapter.", CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// Validate order routing
		state.OrderRouting.TryGetOrderAdapter(transId, out _).AssertTrue("OrderRouting mapping exists");

		await adapter.SimulateOrderExecution(transId, CancellationToken, OrderStates.Active, orderId: 456);

		await WaitFor(adapter.Progress, () => adapter.LastCancelMessage is not null,
			() => "Cancelling the enumeration never sent a cancel order to the adapter.", CancellationToken);

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

		await WaitFor(adapter.Progress, () => adapter.LastOrderTransactionId != 0,
			() => "The order never reached the adapter.", CancellationToken);

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

		events.Count.AssertEqual(5, "Should receive Pending, Active, Trade, order update and Done events for our order");
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

		await WaitFor(adapter.Progress, () => adapter.LastOrderTransactionId != 0,
			() => "The order never reached the adapter.", CancellationToken);

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
		public ConcurrentQueue<MarketDataMessage> RecordedUnsubscriptions { get; } = [];

		/// <summary>
		/// Signalled once the adapter has finished with a message, so a test waits for what it sent
		/// to have been handled rather than for a while.
		/// </summary>
		public ProgressSignal Progress { get; } = new();

		public CandleMockAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMarketDataType(TimeSpan.FromMinutes(5).TimeFrame());
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.Level1);
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Securities || dataType == DataType.Transactions;

		public override bool UseInChannel => false;
		public override bool UseOutChannel => false;

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			try
			{
				await HandleAsync(message, cancellationToken);
			}
			finally
			{
				// Everything a test waits for here is recorded while the message is handled, so one
				// signal afterwards wakes every waiter, however the handling ended.
				Progress.Raise();
			}
		}

		private async ValueTask HandleAsync(Message message, CancellationToken cancellationToken)
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
						var recorded = mdMsg.TypedClone();

						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);
						// Online if live (To==null), Finished if history (To!=null)
						await SendSubscriptionResultAsync(mdMsg, cancellationToken);

						// Recorded only once the subscription has been answered in full, so a test
						// that waits for it cannot go on to act on a subscription that is not up yet.
						RecordedSubscriptions.Enqueue(recorded);
					}
					else
					{
						var recorded = mdMsg.TypedClone();

						await SendOutMessageAsync(mdMsg.CreateResponse(), cancellationToken);

						RecordedUnsubscriptions.Enqueue(recorded);
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

	/// <summary>
	/// What the adapter has been asked to subscribe to so far, so a wait that is given up on can say
	/// what arrived instead of what was expected.
	/// </summary>
	private static string DescribeSubscriptions(CandleMockAdapter adapter)
		=> $"Subscriptions: [{adapter.RecordedSubscriptions.Select(m => $"{m.DataType2}(histOnly={m.IsHistoryOnly()}, To={m.To?.ToString("HH:mm:ss") ?? "null"})").Join("; ")}]";

	/// <summary>
	/// What the adapter has been asked to unsubscribe from so far, for the same reason.
	/// </summary>
	private static string DescribeUnsubscriptions(CandleMockAdapter adapter)
		=> $"Unsubscriptions: [{adapter.RecordedUnsubscriptions.Select(m => m.DataType2.ToString()).Join("; ")}]";

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task CandleHistLive_LiveSubscriptionShouldReachAdapter()
	{
		// A hist+live candle subscription is split into two phases:
		//   Phase 1: candle subscription with To set (history-only)
		//   Phase 2: candle subscription with To=null (live)
		// The test waits for the full transition before checking both live sources.

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
		await WaitFor(adapter.Progress,
			() => adapter.RecordedSubscriptions.Any(m => m.DataType2 == DataType.Ticks && !m.IsHistoryOnly()),
			() => $"The live ticks subscription that builds the candles never reached the adapter. {DescribeSubscriptions(adapter)}",
			CancellationToken);

		// The second phase is the live candle subscription: wait for it to be sent rather than for a
		// stretch of clock in which it may or may not have been.
		await WaitFor(adapter.Progress,
			() => adapter.RecordedSubscriptions.Any(m => m.DataType2.IsTFCandles && !m.IsHistoryOnly()),
			() => $"Expected a live candle subscription (To=null) at the adapter, but none arrived. {DescribeSubscriptions(adapter)}",
			CancellationToken);

		var liveCandleSub = adapter.RecordedSubscriptions.FirstOrDefault(m => m.DataType2.IsTFCandles && !m.IsHistoryOnly());

		IsNotNull(liveCandleSub,
			$"Expected a live candle subscription (To=null) at the adapter, but none arrived. {DescribeSubscriptions(adapter)}");

		runCts.Cancel();
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task CandleHistLive_UnsubscribeShouldReachAdapter()
	{
		// After hist+live candle subscription is fully established (history→ticks+live candle),
		// calling UnSubscribe should send unsubscribe messages to the adapter
		// for ALL active subscriptions (ticks build + live candle).

		var (connector, adapter, _) = CreateConnectorForCandleTest();
		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var sub = new Subscription(TimeSpan.FromMinutes(5).TimeFrame(), security)
		{
			From = DateTime.UtcNow.AddDays(-1),
		};

		connector.Subscribe(sub);

		// Wait for full pipeline cycle (ticks subscription = proof history finished and compress started)
		await WaitFor(adapter.Progress,
			() => adapter.RecordedSubscriptions.Any(m => m.DataType2 == DataType.Ticks && !m.IsHistoryOnly()),
			() => $"The live ticks subscription that builds the candles never reached the adapter. {DescribeSubscriptions(adapter)}",
			CancellationToken);

		// Both live subscriptions have to be in place before unsubscribing, or there is nothing for
		// the unsubscribe to undo and the test would be measuring the wrong thing.
		await WaitFor(adapter.Progress,
			() => adapter.RecordedSubscriptions.Any(m => m.DataType2.IsTFCandles && !m.IsHistoryOnly()),
			() => $"The live candle subscription never reached the adapter, so there was nothing to unsubscribe. {DescribeSubscriptions(adapter)}",
			CancellationToken);

		// Now unsubscribe via sync API
		connector.UnSubscribe(sub);

		// Wait for unsubscribe messages to arrive
		await WaitFor(adapter.Progress,
			() => adapter.RecordedUnsubscriptions.Any(m => m.DataType2 == DataType.Ticks)
				&& adapter.RecordedUnsubscriptions.Any(m => m.DataType2.IsTFCandles),
			() => $"Unsubscribing left one of the two live subscriptions in place. {DescribeUnsubscriptions(adapter)}",
			CancellationToken);

		var unsubs = adapter.RecordedUnsubscriptions.ToList();

		// Should have unsubscribed the ticks build subscription
		var tickUnsub = unsubs.FirstOrDefault(m => m.DataType2 == DataType.Ticks);
		IsNotNull(tickUnsub,
			$"Expected ticks unsubscribe at adapter. Unsubs: [{unsubs.Select(m => m.DataType2.ToString()).Join("; ")}]");

		// Should have unsubscribed the live candle subscription
		var candleUnsub = unsubs.FirstOrDefault(m => m.DataType2.IsTFCandles);
		IsNotNull(candleUnsub,
			$"Expected candle unsubscribe at adapter. Unsubs: [{unsubs.Select(m => m.DataType2.ToString()).Join("; ")}]");
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task TicksLive_UnsubscribeShouldReachAdapter()
	{
		// Subscribe to live ticks via sync API → UnSubscribe → adapter should receive unsubscribe.

		var (connector, adapter, _) = CreateConnectorForCandleTest();
		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var sub = new Subscription(DataType.Ticks, security);

		connector.Subscribe(sub);

		// Wait for subscription to reach adapter
		await WaitFor(adapter.Progress, () => adapter.RecordedSubscriptions.Any(m => m.DataType2 == DataType.Ticks),
			() => $"The ticks subscription never reached the adapter. {DescribeSubscriptions(adapter)}", CancellationToken);

		// Now unsubscribe via sync API
		connector.UnSubscribe(sub);

		// Wait for unsubscribe
		await WaitFor(adapter.Progress, () => adapter.RecordedUnsubscriptions.Any(m => m.DataType2 == DataType.Ticks),
			() => $"Unsubscribing never reached the adapter. {DescribeUnsubscriptions(adapter)}", CancellationToken);

		var unsub = adapter.RecordedUnsubscriptions.First(m => m.DataType2 == DataType.Ticks);
		unsub.IsSubscribe.AssertFalse();
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task Level1Live_UnsubscribeShouldReachAdapter()
	{
		// Subscribe to live Level1 via sync API → UnSubscribe → adapter should receive unsubscribe.

		var (connector, adapter, _) = CreateConnectorForCandleTest();
		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var sub = new Subscription(DataType.Level1, security);

		connector.Subscribe(sub);

		// Wait for subscription to reach adapter
		await WaitFor(adapter.Progress, () => adapter.RecordedSubscriptions.Any(m => m.DataType2 == DataType.Level1),
			() => $"The Level1 subscription never reached the adapter. {DescribeSubscriptions(adapter)}", CancellationToken);

		// Now unsubscribe via sync API
		connector.UnSubscribe(sub);

		// Wait for unsubscribe
		await WaitFor(adapter.Progress, () => adapter.RecordedUnsubscriptions.Any(m => m.DataType2 == DataType.Level1),
			() => $"Unsubscribing never reached the adapter. {DescribeUnsubscriptions(adapter)}", CancellationToken);

		var unsub = adapter.RecordedUnsubscriptions.First(m => m.DataType2 == DataType.Level1);
		unsub.IsSubscribe.AssertFalse();
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task CandleHistLive_UnsubscribeAsync_ShouldReachAdapter()
	{
		// Same scenario as sync version but using SubscribeAsync + CancellationToken.

		var (connector, adapter, _) = CreateConnectorForCandleTest();
		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var sub = new Subscription(TimeSpan.FromMinutes(5).TimeFrame(), security)
		{
			From = DateTime.UtcNow.AddDays(-1),
		};

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token);

		await WaitFor(adapter.Progress,
			() => adapter.RecordedSubscriptions.Any(m => m.DataType2 == DataType.Ticks && !m.IsHistoryOnly()),
			() => $"The live ticks subscription that builds the candles never reached the adapter. {DescribeSubscriptions(adapter)}",
			CancellationToken);

		// Both live subscriptions have to be in place before cancelling, or there is nothing for the
		// cancellation to undo and the test would be measuring the wrong thing.
		await WaitFor(adapter.Progress,
			() => adapter.RecordedSubscriptions.Any(m => m.DataType2.IsTFCandles && !m.IsHistoryOnly()),
			() => $"The live candle subscription never reached the adapter, so there was nothing to unsubscribe. {DescribeSubscriptions(adapter)}",
			CancellationToken);

		runCts.Cancel();
		try { await run; } catch (OperationCanceledException) { }

		await WaitFor(adapter.Progress,
			() => adapter.RecordedUnsubscriptions.Any(m => m.DataType2 == DataType.Ticks)
				&& adapter.RecordedUnsubscriptions.Any(m => m.DataType2.IsTFCandles),
			() => $"Cancelling left one of the two live subscriptions in place. {DescribeUnsubscriptions(adapter)}",
			CancellationToken);

		var unsubs = adapter.RecordedUnsubscriptions.ToList();

		var tickUnsub = unsubs.FirstOrDefault(m => m.DataType2 == DataType.Ticks);
		IsNotNull(tickUnsub,
			$"Expected ticks unsubscribe. Unsubs: [{unsubs.Select(m => m.DataType2.ToString()).Join("; ")}]");

		var candleUnsub = unsubs.FirstOrDefault(m => m.DataType2.IsTFCandles);
		IsNotNull(candleUnsub,
			$"Expected candle unsubscribe. Unsubs: [{unsubs.Select(m => m.DataType2.ToString()).Join("; ")}]");
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task TicksLive_UnsubscribeAsync_ShouldReachAdapter()
	{
		// SubscribeAsync + CancellationToken cancel for ticks.

		var (connector, adapter, _) = CreateConnectorForCandleTest();
		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var sub = new Subscription(DataType.Ticks, security);

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token);

		await WaitFor(adapter.Progress, () => adapter.RecordedSubscriptions.Any(m => m.DataType2 == DataType.Ticks),
			() => $"The ticks subscription never reached the adapter. {DescribeSubscriptions(adapter)}", CancellationToken);

		runCts.Cancel();
		try { await run; } catch (OperationCanceledException) { }

		await WaitFor(adapter.Progress, () => adapter.RecordedUnsubscriptions.Any(m => m.DataType2 == DataType.Ticks),
			() => $"Cancelling the subscription never reached the adapter as an unsubscribe. {DescribeUnsubscriptions(adapter)}", CancellationToken);

		var unsub = adapter.RecordedUnsubscriptions.First(m => m.DataType2 == DataType.Ticks);
		unsub.IsSubscribe.AssertFalse();
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task Level1Live_UnsubscribeAsync_ShouldReachAdapter()
	{
		// SubscribeAsync + CancellationToken cancel for Level1.

		var (connector, adapter, _) = CreateConnectorForCandleTest();
		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		await connector.SendOutMessageAsync(security.ToMessage(), CancellationToken);

		var sub = new Subscription(DataType.Level1, security);

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token);

		await WaitFor(adapter.Progress, () => adapter.RecordedSubscriptions.Any(m => m.DataType2 == DataType.Level1),
			() => $"The Level1 subscription never reached the adapter. {DescribeSubscriptions(adapter)}", CancellationToken);

		runCts.Cancel();
		try { await run; } catch (OperationCanceledException) { }

		await WaitFor(adapter.Progress, () => adapter.RecordedUnsubscriptions.Any(m => m.DataType2 == DataType.Level1),
			() => $"Cancelling the subscription never reached the adapter as an unsubscribe. {DescribeUnsubscriptions(adapter)}", CancellationToken);

		var unsub = adapter.RecordedUnsubscriptions.First(m => m.DataType2 == DataType.Level1);
		unsub.IsSubscribe.AssertFalse();
	}

	#endregion
}
