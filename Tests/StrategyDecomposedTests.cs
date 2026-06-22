namespace StockSharp.Tests;

using StockSharp.Algo.PnL;
using StockSharp.Algo.PositionManagement;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Statistics;

[TestClass]
public class StrategyDecomposedTests : BaseTestClass
{
	#region FakeHost

	private class FakeHost : IStrategyHost
	{
		private long _nextId = 1000;

		public DateTime CurrentTime { get; set; } = DateTime.UtcNow;

		public string StrategyId { get; set; } = "test";

		public bool HasPositions { get; set; }

		public bool CanRefreshPnL(DateTime time) => true;

		public List<Message> SentMessages { get; } = [];

		public void SendOutMessage(Message message) => SentMessages.Add(message);

		public ValueTask SendOutMessageAsync(Message message, CancellationToken cancellationToken)
		{
			SentMessages.Add(message);
			return default;
		}

		public long GetNextTransactionId() => Interlocked.Increment(ref _nextId);
	}

	#endregion

	#region StrategyEngine tests

	[TestMethod]
	public async Task StrategyEngine_RequestStart_SendsStartedMessage()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		await engine.RequestStartAsync(default);

		host.SentMessages.Count.AreEqual(1);
		var msg = host.SentMessages[0] as StrategyEngine.StrategyStateMessage;
		IsNotNull(msg);
		msg.RequestedState.AreEqual(ProcessStates.Started);
	}

	[TestMethod]
	public async Task StrategyEngine_RequestStop_SendsStoppingMessage()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		await engine.RequestStartAsync(default);
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		engine.ProcessState.AreEqual(ProcessStates.Started);

		host.SentMessages.Clear();
		await engine.RequestStopAsync(default);

		host.SentMessages.Count.AreEqual(2);
		var msg = host.SentMessages[0] as StrategyEngine.StrategyStateMessage;
		IsNotNull(msg);
		msg.RequestedState.AreEqual(ProcessStates.Stopping);

		msg = host.SentMessages[1] as StrategyEngine.StrategyStateMessage;
		IsNotNull(msg);
		msg.RequestedState.AreEqual(ProcessStates.Stopped);
	}

	[TestMethod]
	public void StrategyEngine_OnMessage_StateTransition_StoppedToStarted()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		ProcessStates? receivedState = null;
		engine.StateChanged += s => receivedState = s;

		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		engine.ProcessState.AreEqual(ProcessStates.Started);
		receivedState.AreEqual(ProcessStates.Started);
	}

	[TestMethod]
	public void StrategyEngine_OnMessage_StateTransition_StartedToStopping()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		engine.ProcessState.AreEqual(ProcessStates.Started);

		ProcessStates? receivedState = null;
		engine.StateChanged += s => receivedState = s;

		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));

		engine.ProcessState.AreEqual(ProcessStates.Stopping);
		receivedState.AreEqual(ProcessStates.Stopping);
	}

	[TestMethod]
	public void StrategyEngine_OnMessage_Level1_UpdatesCurrentPrice()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		SecurityId? updatedSecId = null;
		decimal? updatedPrice = null;
		engine.CurrentPriceUpdated += (secId, price, _, _) =>
		{
			updatedSecId = secId;
			updatedPrice = price;
		};

		var secId = Helper.CreateSecurityId();
		var msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};
		msg.Add(Level1Fields.LastTradePrice, 100m);

		engine.OnMessage(msg);

		IsTrue(updatedSecId.HasValue);
		updatedSecId.Value.AreEqual(secId);
		updatedPrice.AreEqual(100m);
	}

	[TestMethod]
	public void StrategyEngine_OnMessage_CandleUpdates_CurrentPrice()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		SecurityId? updatedSecId = null;
		decimal? updatedPrice = null;
		engine.CurrentPriceUpdated += (secId, price, _, _) =>
		{
			updatedSecId = secId;
			updatedPrice = price;
		};

		var secId = Helper.CreateSecurityId();
		var msg = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			OpenTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
			ClosePrice = 55.5m,
			OpenPrice = 50m,
			HighPrice = 60m,
			LowPrice = 45m,
		};

		engine.OnMessage(msg);

		IsTrue(updatedSecId.HasValue);
		updatedSecId.Value.AreEqual(secId);
		updatedPrice.AreEqual(55.5m);
	}

	[TestMethod]
	public async Task StrategyEngine_RequestStop_WhenAlreadyStopped_NoMessage()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		await engine.RequestStopAsync(default);

		host.SentMessages.Count.AreEqual(0);
	}

	[TestMethod]
	public void StrategyEngine_ForceStop_ResetsState()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		engine.ProcessState.AreEqual(ProcessStates.Started);

		engine.ForceStop();
		engine.ProcessState.AreEqual(ProcessStates.Stopped);
	}

	#endregion

	#region OrderPipeline tests

	[TestMethod]
	public void OrderPipeline_TryAttach_NewOrder_ReturnsTrue()
	{
		using var stats = new StatisticManager();
		var pipeline = new OrderPipeline(stats);

		var order = new Order { TransactionId = 1 };

		Order newOrderEvent = null;
		pipeline.NewOrder += o => newOrderEvent = o;

		pipeline.TryAttach(order).AssertTrue();
		pipeline.IsTracked(order).AssertTrue();
		IsNotNull(newOrderEvent);
	}

	[TestMethod]
	public void OrderPipeline_TryAttach_DuplicateOrder_ReturnsFalse()
	{
		using var stats = new StatisticManager();
		var pipeline = new OrderPipeline(stats);

		var order = new Order { TransactionId = 1 };

		pipeline.TryAttach(order).AssertTrue();
		pipeline.TryAttach(order).AssertFalse();
	}

	[TestMethod]
	public void OrderPipeline_ProcessOrder_PendingToActive_FiresRegistered()
	{
		using var stats = new StatisticManager();
		var pipeline = new OrderPipeline(stats);

		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = CreateSecurity(),
			Portfolio = CreatePortfolio(),
			Time = DateTime.UtcNow,
		};

		pipeline.TryAttach(order);
		pipeline.ProcessOrder(order, false);

		Order registeredOrder = null;
		pipeline.Registered += o => registeredOrder = o;

		order.State = OrderStates.Active;
		pipeline.ProcessOrder(order, false);

		IsNotNull(registeredOrder);
		AreEqual(order, registeredOrder);
	}

	[TestMethod]
	public void OrderPipeline_ProcessOrder_ActiveChanged_FiresChanged()
	{
		using var stats = new StatisticManager();
		var pipeline = new OrderPipeline(stats);

		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = CreateSecurity(),
			Portfolio = CreatePortfolio(),
			Time = DateTime.UtcNow,
		};

		pipeline.TryAttach(order);
		pipeline.ProcessOrder(order, false);

		order.State = OrderStates.Active;
		pipeline.ProcessOrder(order, false);

		Order changedOrder = null;
		pipeline.Changed += o => changedOrder = o;

		pipeline.ProcessOrder(order, true);

		IsNotNull(changedOrder);
		AreEqual(order, changedOrder);
	}

	[TestMethod]
	public void OrderPipeline_Commission_AccumulatesFromOrders()
	{
		using var stats = new StatisticManager();
		var pipeline = new OrderPipeline(stats);

		var order1 = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Commission = 5m,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = CreateSecurity(),
			Portfolio = CreatePortfolio(),
			Time = DateTime.UtcNow,
		};

		pipeline.TryAttach(order1);
		pipeline.ProcessOrder(order1, false);

		order1.State = OrderStates.Active;
		pipeline.ProcessOrder(order1, false);

		pipeline.Commission.AreEqual(5m);
	}

	[TestMethod]
	public void OrderPipeline_Reset_ClearsAll()
	{
		using var stats = new StatisticManager();
		var pipeline = new OrderPipeline(stats);

		var order = new Order { TransactionId = 1 };
		pipeline.TryAttach(order);

		pipeline.Reset();

		pipeline.IsTracked(order).AssertFalse();
		pipeline.Orders.Any().AssertFalse();
	}

	#endregion

	#region TradePipeline tests

	[TestMethod]
	public void TradePipeline_TryAdd_NewTrade_ReturnsTrue_FiresEvent()
	{
		var pnl = new PnLManager();
		using var stats = new StatisticManager();
		var pipeline = new TradePipeline(pnl, stats);

		var trade = CreateMyTrade(1, 100m, 10m);

		MyTrade addedTrade = null;
		pipeline.TradeAdded += t => addedTrade = t;

		pipeline.TryAdd(trade).AssertTrue();

		IsNotNull(addedTrade);
		AreEqual(trade, addedTrade);
	}

	[TestMethod]
	public void TradePipeline_TryAdd_DuplicateTrade_ReturnsFalse()
	{
		var pnl = new PnLManager();
		using var stats = new StatisticManager();
		var pipeline = new TradePipeline(pnl, stats);

		var trade = CreateMyTrade(1, 100m, 10m);

		pipeline.TryAdd(trade).AssertTrue();
		pipeline.TryAdd(trade).AssertFalse();
	}

	[TestMethod]
	public void TradePipeline_Commission_AccumulatesAcrossTrades()
	{
		var pnl = new PnLManager();
		using var stats = new StatisticManager();
		var pipeline = new TradePipeline(pnl, stats);

		var trade1 = CreateMyTrade(1, 100m, 10m, commission: 2m);
		var trade2 = CreateMyTrade(2, 101m, 5m, commission: 3m);

		pipeline.TryAdd(trade1);
		pipeline.TryAdd(trade2);

		pipeline.Commission.AreEqual(5m);
	}

	[TestMethod]
	public void TradePipeline_Slippage_AccumulatesAcrossTrades()
	{
		var pnl = new PnLManager();
		using var stats = new StatisticManager();
		var pipeline = new TradePipeline(pnl, stats);

		var trade1 = CreateMyTrade(1, 100m, 10m, slippage: 0.5m);
		var trade2 = CreateMyTrade(2, 101m, 5m, slippage: 0.3m);

		pipeline.TryAdd(trade1);
		pipeline.TryAdd(trade2);

		pipeline.Slippage.AreEqual(0.8m);
	}

	[TestMethod]
	public void TradePipeline_Reset_ClearsAll()
	{
		var pnl = new PnLManager();
		using var stats = new StatisticManager();
		var pipeline = new TradePipeline(pnl, stats);

		var trade = CreateMyTrade(1, 100m, 10m, commission: 2m);
		pipeline.TryAdd(trade);

		pipeline.Reset();

		IsNull(pipeline.Commission);
		IsNull(pipeline.Slippage);
		pipeline.MyTrades.Any().AssertFalse();
	}

	#endregion

	#region PositionPipeline tests

	[TestMethod]
	public void PositionPipeline_Process_NewPosition_FiresNewEvent()
	{
		using var stats = new StatisticManager();
		var pipeline = new PositionPipeline(stats);

		Position received = null;
		pipeline.NewPosition += p => received = p;

		var position = new Position
		{
			Security = CreateSecurity(),
			Portfolio = CreatePortfolio(),
			CurrentValue = 100,
			LocalTime = DateTime.UtcNow,
		};

		pipeline.Process(position, isNew: true);

		IsNotNull(received);
		AreEqual(position, received);
	}

	[TestMethod]
	public void PositionPipeline_Process_ExistingPosition_FiresChangedEvent()
	{
		using var stats = new StatisticManager();
		var pipeline = new PositionPipeline(stats);

		Position newReceived = null;
		Position changedReceived = null;
		pipeline.NewPosition += p => newReceived = p;
		pipeline.PositionChanged += p => changedReceived = p;

		var position = new Position
		{
			Security = CreateSecurity(),
			Portfolio = CreatePortfolio(),
			CurrentValue = 100,
			LocalTime = DateTime.UtcNow,
		};

		pipeline.Process(position, isNew: false);

		IsNull(newReceived);
		IsNotNull(changedReceived);
		AreEqual(position, changedReceived);
	}

	#endregion

	#region SubscriptionRegistry tests

	[TestMethod]
	public void SubscriptionRegistry_Subscribe_AddsToTracking()
	{
		var host = new FakeHost();
		var registry = new SubscriptionRegistry(host);

		var sub = new Subscription(DataType.Level1);

		Subscription requested = null;
		registry.SubscriptionRequested += s => requested = s;

		registry.Subscribe(sub);

		registry.CanProcess(sub).AssertTrue();
		IsNotNull(requested);
		AreEqual(sub, requested);
		AreNotEqual(0L, sub.TransactionId);
	}

	[TestMethod]
	public void SubscriptionRegistry_Subscribe_WhenSuspended_QueuesSubscription()
	{
		var host = new FakeHost();
		var registry = new SubscriptionRegistry(host);

		Subscription requested = null;
		registry.SubscriptionRequested += s => requested = s;

		registry.SuspendRules();
		registry.IsRulesSuspended.AssertTrue();

		var sub = new Subscription(DataType.Level1);
		registry.Subscribe(sub);

		registry.CanProcess(sub).AssertTrue();
		IsNull(requested);
	}

	[TestMethod]
	public void SubscriptionRegistry_ResumeRules_SendsQueuedSubscriptions()
	{
		var host = new FakeHost();
		var registry = new SubscriptionRegistry(host);

		var requestedSubs = new List<Subscription>();
		registry.SubscriptionRequested += s => requestedSubs.Add(s);

		registry.SuspendRules();

		var sub1 = new Subscription(DataType.Level1);
		var sub2 = new Subscription(DataType.Ticks);
		registry.Subscribe(sub1);
		registry.Subscribe(sub2);

		requestedSubs.Count.AreEqual(0);

		registry.ResumeRules();

		requestedSubs.Count.AreEqual(2);
		requestedSubs.Contains(sub1).AssertTrue();
		requestedSubs.Contains(sub2).AssertTrue();
		registry.IsRulesSuspended.AssertFalse();
	}

	[TestMethod]
	public void SubscriptionRegistry_CanProcess_UntrackedSubscription_ReturnsFalse()
	{
		var host = new FakeHost();
		var registry = new SubscriptionRegistry(host);

		var sub = new Subscription(DataType.Level1);

		registry.CanProcess(sub).AssertFalse();
	}

	[TestMethod]
	public void SubscriptionRegistry_UnSubscribe_WhenSuspended_RemovesFromQueue()
	{
		var host = new FakeHost();
		var registry = new SubscriptionRegistry(host);

		registry.SuspendRules();

		var sub = new Subscription(DataType.Level1);
		registry.Subscribe(sub);

		registry.CanProcess(sub).AssertTrue();

		registry.UnSubscribe(sub);

		registry.CanProcess(sub).AssertFalse();
	}

	[TestMethod]
	public void SubscriptionRegistry_TryGetById_ReturnsSubscription()
	{
		var host = new FakeHost();
		var registry = new SubscriptionRegistry(host);

		var sub = new Subscription(DataType.Level1);
		registry.Subscribe(sub);

		var found = registry.TryGetById(sub.TransactionId);
		IsNotNull(found);
		AreEqual(sub, found);
	}

	[TestMethod]
	public void SubscriptionRegistry_Reset_ClearsAll()
	{
		var host = new FakeHost();
		var registry = new SubscriptionRegistry(host);

		var sub = new Subscription(DataType.Level1);
		registry.Subscribe(sub);

		registry.SuspendRules();
		registry.IsRulesSuspended.AssertTrue();

		registry.Reset();

		registry.CanProcess(sub).AssertFalse();
		registry.IsRulesSuspended.AssertFalse();
		IsNull(registry.TryGetById(sub.TransactionId));
	}

	[TestMethod]
	public void SubscriptionRegistry_MultipleSuspend_RequiresMultipleResume()
	{
		var host = new FakeHost();
		var registry = new SubscriptionRegistry(host);

		var requestedSubs = new List<Subscription>();
		registry.SubscriptionRequested += s => requestedSubs.Add(s);

		registry.SuspendRules();
		registry.SuspendRules();

		var sub = new Subscription(DataType.Level1);
		registry.Subscribe(sub);

		registry.ResumeRules();
		registry.IsRulesSuspended.AssertTrue();
		requestedSubs.Count.AreEqual(0);

		registry.ResumeRules();
		registry.IsRulesSuspended.AssertFalse();
		requestedSubs.Count.AreEqual(1);
	}

	#endregion

	#region Helpers

	private static Security CreateSecurity(string code = null)
	{
		return new Security
		{
			Id = code ?? Helper.CreateSecurityId().ToStringId(),
			Board = ExchangeBoard.Nyse,
			PriceStep = 0.01m,
		};
	}

	private static Portfolio CreatePortfolio(string name = "test_portfolio")
	{
		return new Portfolio { Name = name };
	}

	private static MyTrade CreateMyTrade(long tradeId, decimal price, decimal volume, decimal? commission = null, decimal? slippage = null)
	{
		var security = CreateSecurity();
		return new MyTrade
		{
			Order = new Order
			{
				TransactionId = tradeId * 100,
				Security = security,
				Portfolio = CreatePortfolio(),
				Side = Sides.Buy,
				Price = price,
				Volume = volume,
				Time = DateTime.UtcNow,
			},
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = tradeId,
				TradePrice = price,
				TradeVolume = volume,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
			Commission = commission,
			Slippage = slippage,
		};
	}

	#endregion

	#region Strategy composite tests

	// Test strategy that inherits Strategy and records all hook calls.
	private class BuyOnSignalStrategy : Strategy
	{
		public List<ProcessStates> StateChanges { get; } = [];
		public List<(SecurityId SecId, decimal Price)> PriceUpdates { get; } = [];
		public List<Order> RegisteredOrders { get; } = [];
		public List<Order> ChangedOrders { get; } = [];
		public List<MyTrade> ReceivedTrades { get; } = [];
		public List<Position> NewPositions { get; } = [];
		public List<Position> ChangedPositions { get; } = [];

		public decimal? BuySignalPrice { get; set; }
		public Security TradeSecurity { get; set; }
		public Portfolio TradePortfolio { get; set; }

		protected override void OnStateChanged(ProcessStates state)
		{
			StateChanges.Add(state);
		}

		protected override void OnCurrentPriceUpdated(SecurityId secId, decimal price, DateTime serverTime, DateTime localTime)
		{
			PriceUpdates.Add((secId, price));

			if (BuySignalPrice != null && price >= BuySignalPrice.Value && ProcessState == ProcessStates.Started)
			{
				var order = new Order
				{
					TransactionId = 42,
					Side = Sides.Buy,
					Price = price,
					Volume = 5,
					Security = TradeSecurity,
					Portfolio = TradePortfolio,
					Time = serverTime,
				};
				RegisterOrder(order);
			}
		}

		protected override void OnOrderRegistered(Order order)
		{
			RegisteredOrders.Add(order);
		}

		protected override void OnOrderChanged(Order order)
		{
			ChangedOrders.Add(order);
		}

		protected override void OnOwnTradeReceived(MyTrade trade)
		{
			ReceivedTrades.Add(trade);
		}

		protected override void OnNewPosition(Position position)
		{
			NewPositions.Add(position);
		}

		protected override void OnPositionChanged(Position position)
		{
			ChangedPositions.Add(position);
		}
	}

	private static Mock<IConnector> CreateMockConnector()
	{
		var mock = new Mock<IConnector>();
		mock.Setup(c => c.TransactionIdGenerator).Returns(new IncrementalIdGenerator());
		return mock;
	}

	private static void MarkStarted(Strategy strategy)
		=> strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

	[TestMethod]
	public async Task Composite_StateTransitions_AllHooksCalled()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		// initial state
		strategy.ProcessState.AreEqual(ProcessStates.Stopped);
		strategy.StateChanges.Count.AreEqual(0);

		// start
		await strategy.StartAsync();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		strategy.ProcessState.AreEqual(ProcessStates.Started);
		strategy.StateChanges.Count.AreEqual(1);
		strategy.StateChanges[0].AreEqual(ProcessStates.Started);

		// stop
		await strategy.StopAsync();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));

		strategy.ProcessState.AreEqual(ProcessStates.Stopping);
		strategy.StateChanges.Count.AreEqual(2);
		strategy.StateChanges[1].AreEqual(ProcessStates.Stopping);
	}

	[TestMethod]
	public void Composite_RegisterOrder_DelegatesToConnector()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };
		MarkStarted(strategy);

		var order = new Order
		{
			TransactionId = 1,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = CreateSecurity(),
			Portfolio = CreatePortfolio(),
		};

		strategy.RegisterOrder(order);
		connMock.Verify(c => c.RegisterOrder(order), Times.Once);
	}

	[TestMethod]
	public void Composite_CancelOrder_DelegatesToConnector()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };
		MarkStarted(strategy);

		// The order must be a started, owned order for the public CancelOrder guards to let it through.
		var order = RegisterOwnActiveOrder(strategy, connMock);
		connMock.Invocations.Clear();

		strategy.CancelOrder(order);
		connMock.Verify(c => c.CancelOrder(order), Times.Once);
	}

	[TestMethod]
	public void Composite_PriceUpdate_HookCalledWithExactValues()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		var secId = Helper.CreateSecurityId();
		var serverTime = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
		var localTime = new DateTime(2025, 6, 1, 12, 0, 1, DateTimeKind.Utc);

		var msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = serverTime,
			LocalTime = localTime,
		};
		msg.Add(Level1Fields.LastTradePrice, 42.5m);

		strategy.OnNewMessage(msg, CancellationToken).GetAwaiter().GetResult();

		strategy.PriceUpdates.Count.AreEqual(1);
		strategy.PriceUpdates[0].SecId.AreEqual(secId);
		strategy.PriceUpdates[0].Price.AreEqual(42.5m);
	}

	[TestMethod]
	public void Composite_SubscriptionFiltering_UntrackedIgnored()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		var trackedSub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(trackedSub);

		var untrackedSub = new Subscription(DataType.Ticks);

		var security = CreateSecurity();
		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Active,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = security,
			Portfolio = CreatePortfolio(),
			UserOrderId = strategy.Id.To<string>(),
			Time = DateTime.UtcNow,
		};

		// untracked subscription — order NOT attached
		strategy.OnConnectorOrderReceived(untrackedSub, order);
		strategy.OrderProcessor.IsTracked(order).AssertFalse();
		strategy.OrderProcessor.Orders.Any().AssertFalse();

		// tracked subscription — order attached
		strategy.OnConnectorOrderReceived(trackedSub, order);
		strategy.OrderProcessor.IsTracked(order).AssertTrue();
		strategy.OrderProcessor.Orders.Count().AreEqual(1);
		strategy.OrderProcessor.Orders.First().AreEqual(order);
	}

	[TestMethod]
	public void Composite_TradeIgnoredForUntrackedOrder()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var trade = CreateMyTrade(1, 100m, 10m, commission: 2m);
		// do NOT attach the order

		strategy.OnTradeReceived(sub, trade);

		strategy.Trades.MyTrades.Any().AssertFalse();
		strategy.ReceivedTrades.Count.AreEqual(0);
		IsNull(strategy.Trades.Commission);
	}

	[TestMethod]
	public async Task Composite_FullOrderLifecycle_AllValuesVerified()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Commission = 3m,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		// 1. order received as Pending
		strategy.OnConnectorOrderReceived(sub, order);
		strategy.OrderProcessor.IsTracked(order).AssertTrue();
		strategy.OrderProcessor.Orders.Count().AreEqual(1);
		strategy.RegisteredOrders.Count.AreEqual(0); // not registered yet

		// 2. order becomes Active → fires OnOrderRegistered
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		strategy.RegisteredOrders.Count.AreEqual(1);
		strategy.RegisteredOrders[0].AreEqual(order);
		strategy.RegisteredOrders[0].TransactionId.AreEqual(1L);
		strategy.RegisteredOrders[0].Price.AreEqual(100m);
		strategy.RegisteredOrders[0].Volume.AreEqual(10m);
		strategy.RegisteredOrders[0].Side.AreEqual(Sides.Buy);
		strategy.OrderProcessor.Commission.AreEqual(3m);

		// 3. first trade arrives
		var trade1 = new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 100,
				TradePrice = 100m,
				TradeVolume = 6m,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
			Commission = 1.5m,
			Slippage = 0.2m,
		};

		strategy.OnTradeReceived(sub, trade1);

		strategy.ReceivedTrades.Count.AreEqual(1);
		strategy.ReceivedTrades[0].AreEqual(trade1);
		strategy.Trades.MyTrades.Count().AreEqual(1);
		strategy.Trades.Commission.AreEqual(1.5m);
		strategy.Trades.Slippage.AreEqual(0.2m);

		// 4. second trade arrives — fills remaining volume
		var trade2 = new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 101,
				TradePrice = 100.5m,
				TradeVolume = 4m,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
			Commission = 1m,
			Slippage = 0.1m,
		};

		strategy.OnTradeReceived(sub, trade2);

		strategy.ReceivedTrades.Count.AreEqual(2);
		strategy.ReceivedTrades[0].AreEqual(trade1);
		strategy.ReceivedTrades[1].AreEqual(trade2);
		strategy.Trades.MyTrades.Count().AreEqual(2);
		strategy.Trades.Commission.AreEqual(2.5m); // 1.5 + 1.0
		strategy.Trades.Slippage.AreEqual(0.3m); // 0.2 + 0.1

		// 5. duplicate trade is ignored
		strategy.OnTradeReceived(sub, trade1);
		strategy.ReceivedTrades.Count.AreEqual(2); // unchanged
		strategy.Trades.MyTrades.Count().AreEqual(2);
		strategy.Trades.Commission.AreEqual(2.5m);
	}

	[TestMethod]
	public void Composite_MultipleOrders_TrackedSeparately()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var order1 = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Commission = 2m,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		var order2 = new Order
		{
			TransactionId = 2,
			State = OrderStates.Pending,
			Side = Sides.Sell,
			Price = 110,
			Volume = 5,
			Commission = 1m,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		// receive both as pending
		strategy.OnConnectorOrderReceived(sub, order1);
		strategy.OnConnectorOrderReceived(sub, order2);
		strategy.OrderProcessor.Orders.Count().AreEqual(2);

		// activate both
		order1.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order1);
		order2.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order2);

		strategy.RegisteredOrders.Count.AreEqual(2);
		strategy.RegisteredOrders[0].TransactionId.AreEqual(1L);
		strategy.RegisteredOrders[0].Side.AreEqual(Sides.Buy);
		strategy.RegisteredOrders[0].Price.AreEqual(100m);
		strategy.RegisteredOrders[1].TransactionId.AreEqual(2L);
		strategy.RegisteredOrders[1].Side.AreEqual(Sides.Sell);
		strategy.RegisteredOrders[1].Price.AreEqual(110m);
		strategy.OrderProcessor.Commission.AreEqual(3m); // 2 + 1

		// trade for order1 only
		var trade = new MyTrade
		{
			Order = order1,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 200,
				TradePrice = 100m,
				TradeVolume = 10m,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
			Commission = 0.5m,
		};

		strategy.OnTradeReceived(sub, trade);

		strategy.ReceivedTrades.Count.AreEqual(1);
		strategy.ReceivedTrades[0].Order.TransactionId.AreEqual(1L);
		strategy.Trades.Commission.AreEqual(0.5m);
	}

	[TestMethod]
	public void Composite_Position_New()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		var sub = new Subscription(DataType.PositionChanges);
		strategy.Subscriptions.Subscribe(sub);

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var pos = new Position
		{
			Security = security,
			Portfolio = portfolio,
			CurrentValue = 50,
			LocalTime = DateTime.UtcNow,
		};

		// new position
		strategy.OnPositionReceived(sub, pos);

		strategy.NewPositions.Count.AreEqual(1);
		strategy.NewPositions[0].AreEqual(pos);
		strategy.NewPositions[0].CurrentValue.AreEqual(50m);
		strategy.NewPositions[0].Security.AreEqual(security);
		strategy.NewPositions[0].Portfolio.AreEqual(portfolio);
	}

	[TestMethod]
	public async Task Composite_BuyOnSignal_EndToEnd()
	{
		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			BuySignalPrice = 150m,
			TradeSecurity = security,
			TradePortfolio = portfolio,
		};

		// start strategy
		await strategy.StartAsync();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		strategy.ProcessState.AreEqual(ProcessStates.Started);
		strategy.StateChanges.Count.AreEqual(1);

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var secId = security.ToSecurityId();

		// price below threshold — no order
		var msg1 = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};
		msg1.Add(Level1Fields.LastTradePrice, 120m);
		strategy.OnNewMessage(msg1, CancellationToken).GetAwaiter().GetResult();

		strategy.PriceUpdates.Count.AreEqual(1);
		strategy.PriceUpdates[0].Price.AreEqual(120m);
		connMock.Verify(c => c.RegisterOrder(It.IsAny<Order>()), Times.Never);

		// price at threshold — order placed
		var msg2 = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		};
		msg2.Add(Level1Fields.LastTradePrice, 150m);
		strategy.OnNewMessage(msg2, CancellationToken).GetAwaiter().GetResult();

		strategy.PriceUpdates.Count.AreEqual(2);
		strategy.PriceUpdates[1].Price.AreEqual(150m);
		connMock.Verify(c => c.RegisterOrder(It.IsAny<Order>()), Times.Once);

		// simulate order received from connector as pending
		var order = new Order
		{
			TransactionId = 42,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 150m,
			Volume = 5,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		strategy.OnConnectorOrderReceived(sub, order);
		strategy.OrderProcessor.IsTracked(order).AssertTrue();
		strategy.OrderProcessor.Orders.Count().AreEqual(1);

		// order activated
		order.State = OrderStates.Active;
		order.Commission = 2m;
		strategy.OnConnectorOrderReceived(sub, order);

		strategy.RegisteredOrders.Count.AreEqual(1);
		strategy.RegisteredOrders[0].Price.AreEqual(150m);
		strategy.RegisteredOrders[0].Volume.AreEqual(5m);
		strategy.RegisteredOrders[0].Side.AreEqual(Sides.Buy);
		strategy.OrderProcessor.Commission.AreEqual(2m);

		// trade fills the order
		var trade = new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 500,
				TradePrice = 150m,
				TradeVolume = 5m,
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
			},
			Commission = 0.75m,
			Slippage = 0.1m,
		};

		strategy.OnTradeReceived(sub, trade);

		// verify trade values
		strategy.ReceivedTrades.Count.AreEqual(1);
		strategy.ReceivedTrades[0].AreEqual(trade);
		strategy.Trades.MyTrades.Count().AreEqual(1);
		strategy.Trades.Commission.AreEqual(0.75m);
		strategy.Trades.Slippage.AreEqual(0.1m);

		var firstTrade = strategy.Trades.MyTrades.First();
		firstTrade.Order.TransactionId.AreEqual(42L);
		firstTrade.Order.Price.AreEqual(150m);
		firstTrade.Order.Volume.AreEqual(5m);
		firstTrade.Order.Side.AreEqual(Sides.Buy);

		// stop strategy
		await strategy.StopAsync();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		strategy.ProcessState.AreEqual(ProcessStates.Stopping);
		strategy.StateChanges.Count.AreEqual(2);
		strategy.StateChanges[0].AreEqual(ProcessStates.Started);
		strategy.StateChanges[1].AreEqual(ProcessStates.Stopping);
	}

	[TestMethod]
	public void Composite_ConnectorSwitch_UnsubscribesOld()
	{
		var conn1 = CreateMockConnector();
		var conn2 = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = conn1.Object };

		strategy.Connector = conn2.Object;

		strategy.Connector.AreEqual(conn2.Object);
		conn1.VerifyRemove(
			c => c.OrderReceived -= It.IsAny<Action<Subscription, Order>>(),
			Times.Once);

		// Cancel of an order owned on conn2 must route to conn2, never the detached conn1.
		MarkStarted(strategy);
		var order = RegisterOwnActiveOrder(strategy, conn2);
		conn2.Invocations.Clear();

		strategy.CancelOrder(order);
		conn1.Verify(c => c.CancelOrder(It.IsAny<Order>()), Times.Never);
		conn2.Verify(c => c.CancelOrder(order), Times.Once);
	}

	[TestMethod]
	public void Composite_TradeFill_UpdatesPosition()
	{
		// When a trade fills an order, the strategy's Position should update automatically.
		// Currently PositionPipeline is a pass-through and doesn't compute positions from trades.

		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
			Volume = 10,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// register and activate an order
		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		strategy.OnConnectorOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		// fill the order with a trade
		var trade = new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 100,
				TradePrice = 100m,
				TradeVolume = 10m,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
		};

		strategy.OnTradeReceived(sub, trade);

		// position should be updated automatically from trade fill
		strategy.Position.AreEqual(10m);
	}

	[TestMethod]
	public void Composite_RoundTrip_TrackedFromTrades()
	{
		// When position goes from 0 → open → 0, a round-trip should be recorded.
		// Currently Strategy has no PositionLifecycleTracker.

		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
			Volume = 5,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// buy order filled
		var buyOrder = new Order
		{
			TransactionId = 1, State = OrderStates.Pending,
			Side = Sides.Buy, Price = 100, Volume = 5,
			Security = security, Portfolio = portfolio, Time = DateTime.UtcNow,
		};
		strategy.OnConnectorOrderReceived(sub, buyOrder);
		buyOrder.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, buyOrder);

		strategy.OnTradeReceived(sub, new MyTrade
		{
			Order = buyOrder,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks, TradeId = 1,
				TradePrice = 100m, TradeVolume = 5m,
				SecurityId = security.ToSecurityId(), ServerTime = DateTime.UtcNow,
			},
		});

		// sell order filled — closes the position
		var sellOrder = new Order
		{
			TransactionId = 2, State = OrderStates.Pending,
			Side = Sides.Sell, Price = 110, Volume = 5,
			Security = security, Portfolio = portfolio, Time = DateTime.UtcNow,
		};
		strategy.OnConnectorOrderReceived(sub, sellOrder);
		sellOrder.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, sellOrder);

		strategy.OnTradeReceived(sub, new MyTrade
		{
			Order = sellOrder,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks, TradeId = 2,
				TradePrice = 110m, TradeVolume = 5m,
				SecurityId = security.ToSecurityId(), ServerTime = DateTime.UtcNow,
			},
		});

		// position should be back to 0
		strategy.Position.AreEqual(0m);

		// TODO: verify round-trip history when PositionLifecycleTracker is added
	}

	[TestMethod]
	public void Composite_PositionReceived_DetectsNewVsChanged()
	{
		// PositionPipeline should detect new vs changed positions.
		// Currently OnPositionReceived always passes isNew: true.

		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var sub = new Subscription(DataType.PositionChanges);
		strategy.Subscriptions.Subscribe(sub);

		var pos = new Position
		{
			Security = security,
			Portfolio = portfolio,
			CurrentValue = 50,
			LocalTime = DateTime.UtcNow,
		};

		// first time — should be new
		strategy.OnPositionReceived(sub, pos);
		strategy.NewPositions.Count.AreEqual(1);
		strategy.ChangedPositions.Count.AreEqual(0);

		// second time same security+portfolio — should be changed, not new
		pos.CurrentValue = 60;
		strategy.OnPositionReceived(sub, pos);
		strategy.NewPositions.Count.AreEqual(1);
		strategy.ChangedPositions.Count.AreEqual(1);
	}

	[TestMethod]
	public void Composite_OrderFiltering_OnlyOwnOrdersTracked()
	{
		// Strategy should only track its own orders (via CanAttach/UserOrderId).
		// Currently Strategy attaches any order from any subscription.

		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// register our own order via strategy
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()));
		var ownOrder = strategy.CreateOrder(Sides.Buy, 100m, 5m);
		strategy.RegisterOrder(ownOrder);

		// simulate foreign order from another strategy/system
		var foreignOrder = new Order
		{
			TransactionId = 999,
			State = OrderStates.Active,
			Side = Sides.Sell,
			Price = 200,
			Volume = 100,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		strategy.OnConnectorOrderReceived(sub, foreignOrder);

		// foreign order should NOT be tracked
		strategy.OrderProcessor.IsTracked(foreignOrder).AssertFalse();
	}

	[TestMethod]
	public void Composite_PnL_UpdatesOnPositionChange()
	{
		// When position changes from trades, PnL should be recalculated.
		// Currently PnLManager only processes market data, not position changes.

		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// buy at 100
		var buyOrder = new Order
		{
			TransactionId = 1, State = OrderStates.Pending,
			Side = Sides.Buy, Price = 100, Volume = 10,
			Security = security, Portfolio = portfolio, Time = DateTime.UtcNow,
		};
		strategy.OnConnectorOrderReceived(sub, buyOrder);
		buyOrder.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, buyOrder);

		strategy.OnTradeReceived(sub, new MyTrade
		{
			Order = buyOrder,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks, TradeId = 1,
				TradePrice = 100m, TradeVolume = 10m,
				SecurityId = security.ToSecurityId(), ServerTime = DateTime.UtcNow,
			},
		});

		// sell at 110 — should produce realized PnL
		var sellOrder = new Order
		{
			TransactionId = 2, State = OrderStates.Pending,
			Side = Sides.Sell, Price = 110, Volume = 10,
			Security = security, Portfolio = portfolio, Time = DateTime.UtcNow,
		};
		strategy.OnConnectorOrderReceived(sub, sellOrder);
		sellOrder.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, sellOrder);

		strategy.OnTradeReceived(sub, new MyTrade
		{
			Order = sellOrder,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks, TradeId = 2,
				TradePrice = 110m, TradeVolume = 10m,
				SecurityId = security.ToSecurityId(), ServerTime = DateTime.UtcNow,
			},
		});

		// PnL should reflect realized profit from the round trip
		var pnl = strategy.PnLManager.RealizedPnL;
		pnl.AssertEqual(100m);
	}

	[TestMethod]
	public void Composite_RiskRule_StopsOnPositionLimit()
	{
		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		strategy.RiskManager.Rules.Add(new RiskPositionSizeRule
		{
			Position = 10m,
			Action = RiskActions.StopTrading,
		});

		var positionSub = new Subscription(DataType.PositionChanges);
		strategy.Subscriptions.Subscribe(positionSub);
		strategy.OnPositionReceived(positionSub, new Position
		{
			Security = security,
			Portfolio = portfolio,
			CurrentValue = 10m,
			LocalTime = DateTime.UtcNow,
		});

		var order = strategy.CreateOrder(Sides.Buy, 100m, 1m);
		strategy.RegisterOrder(order);

		connMock.Verify(c => c.RegisterOrder(It.IsAny<Order>()), Times.Never);
		strategy.OrderProcessor.IsTracked(order).AssertFalse();
	}

	#endregion

	#region StartProtection tests

	[TestMethod]
	public void StartProtection_LocalStop_ActivatesOnPriceChange()
	{
		var connMock = CreateMockConnector();
		var registeredOrders = new List<Order>();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o => registeredOrders.Add(o));

		var security = CreateSecurity();
		security.PriceStep = 0.01m;
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};
		MarkStarted(strategy);

		// Configure local stop protection: 5% stop loss
		strategy.StartProtection(
			new Unit(), new Unit(5, UnitTypes.Percent),
			isLocalStop: true);

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// Create and fill a buy order at price 100
		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		strategy.OnConnectorOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		var trade = new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 100m,
				TradeVolume = 10m,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
		};

		strategy.OnTradeReceived(sub, trade);

		// Position should be 10
		strategy.Position.AreEqual(10m);

		// Now simulate price drop to 94 (6% drop, below 5% stop)
		var secId = security.ToSecurityId();
		var l1 = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 94m);

		strategy.Engine.OnMessage(l1);

		// The stop should have activated — a sell protective order should be registered
		IsTrue(registeredOrders.Count > 0,
			"Stop loss should generate a protective order when price drops below threshold");

		// The last registered order should be a sell to close position
		var protectiveOrder = registeredOrders.Last();
		protectiveOrder.Side.AreEqual(Sides.Sell);
	}

	[TestMethod]
	public void StartProtection_NoEffect_WhenNotConfigured()
	{
		var connMock = CreateMockConnector();
		var registeredOrders = new List<Order>();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o => registeredOrders.Add(o));

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		// Do NOT call StartProtection

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		strategy.OnConnectorOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		strategy.OnTradeReceived(sub, new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 100m,
				TradeVolume = 10m,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
		});

		// Simulate price drop — should NOT generate any protective orders
		var secId = security.ToSecurityId();
		var l1 = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 50m);

		strategy.Engine.OnMessage(l1);

		// No protective orders should have been registered
		AreEqual(0, registeredOrders.Count,
			"Without StartProtection, no protective orders should be generated");
	}

	[TestMethod]
	public void StartProtection_Reset_ClearsProtection()
	{
		var connMock = CreateMockConnector();
		var registeredOrders = new List<Order>();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o => registeredOrders.Add(o));

		var security = CreateSecurity();
		security.PriceStep = 0.01m;
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		// Configure protection
		strategy.StartProtection(
			new Unit(), new Unit(5, UnitTypes.Percent),
			isLocalStop: true);

		// Reset should clear protection state
		strategy.Reset();

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// After reset, new trades should NOT trigger protection
		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		strategy.OnConnectorOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		strategy.OnTradeReceived(sub, new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 100m,
				TradeVolume = 10m,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
		});

		var secId = security.ToSecurityId();
		var l1 = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 80m);

		strategy.Engine.OnMessage(l1);

		AreEqual(0, registeredOrders.Count,
			"After Reset(), protection should be cleared and no protective orders generated");
	}

	[TestMethod]
	public void StartProtection_TrailingStop_AdjustsPrice()
	{
		var connMock = CreateMockConnector();
		var registeredOrders = new List<Order>();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o => registeredOrders.Add(o));

		var security = CreateSecurity();
		security.PriceStep = 0.01m;
		var portfolio = CreatePortfolio();

		var strategy = new BuyOnSignalStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};
		MarkStarted(strategy);

		// Configure trailing stop: 5% trailing stop loss
		strategy.StartProtection(
			new Unit(), new Unit(5, UnitTypes.Percent),
			isStopTrailing: true,
			isLocalStop: true);

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// Buy at 100
		var order = new Order
		{
			TransactionId = 1,
			State = OrderStates.Pending,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};

		strategy.OnConnectorOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		strategy.OnTradeReceived(sub, new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = 1,
				TradePrice = 100m,
				TradeVolume = 10m,
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
		});

		var secId = security.ToSecurityId();

		// Price goes up to 110 — trailing stop should trail up
		strategy.Engine.OnMessage(new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 110m));

		// No stop triggered yet
		AreEqual(0, registeredOrders.Count,
			"Price moving up should not trigger stop");

		// Price drops to 104 (5.5% from high of 110) — below 5% trailing
		strategy.Engine.OnMessage(new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			LocalTime = DateTime.UtcNow,
		}.TryAdd(Level1Fields.LastTradePrice, 104m));

		IsTrue(registeredOrders.Count > 0,
			"Trailing stop should activate when price drops 5% from high");

		var protectiveOrder = registeredOrders.Last();
		protectiveOrder.Side.AreEqual(Sides.Sell);
	}

	#endregion

	#region CancelOrder guard tests

	// Register a tracked, own, active order on a started strategy so it can be the subject of a CancelOrder.
	private static Order RegisterOwnActiveOrder(Strategy strategy, Mock<IConnector> connMock, long transactionId = 1)
	{
		var order = new Order
		{
			TransactionId = transactionId,
			Side = Sides.Buy,
			Price = 100,
			Volume = 10,
			State = OrderStates.Active,
			Security = CreateSecurity(),
			Portfolio = CreatePortfolio(),
		};

		strategy.RegisterOrder(order);
		strategy.OrderProcessor.IsTracked(order).AssertTrue();
		return order;
	}

	[TestMethod]
	public void CancelOrder_NotStarted_DoesNotReachConnector()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		// ProcessState is Stopped (not Started): the state guard must turn the cancel into a no-op.
		var order = new Order { TransactionId = 1, State = OrderStates.Active };

		strategy.CancelOrder(order);

		connMock.Verify(c => c.CancelOrder(It.IsAny<Order>()), Times.Never);
	}

	[TestMethod]
	public void CancelOrder_Null_Throws()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };
		MarkStarted(strategy);

		ThrowsExactly<ArgumentNullException>(() => strategy.CancelOrder(null));
	}

	[TestMethod]
	public void CancelOrder_TradingDisabled_DoesNotReachConnector()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };
		MarkStarted(strategy);

		var order = RegisterOwnActiveOrder(strategy, connMock);
		connMock.Invocations.Clear();

		// Disabled trading mode must block cancellation just like it blocks registration.
		strategy.TradingMode = StrategyTradingModes.Disabled;
		strategy.CancelOrder(order);

		connMock.Verify(c => c.CancelOrder(It.IsAny<Order>()), Times.Never);
	}

	[TestMethod]
	public void CancelOrder_UnregisteredOrder_Throws()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };
		MarkStarted(strategy);

		// An order the strategy never registered is not owned: cancelling it must throw.
		var foreign = new Order { TransactionId = 999, State = OrderStates.Active };

		ThrowsExactly<ArgumentException>(() => strategy.CancelOrder(foreign));
		connMock.Verify(c => c.CancelOrder(It.IsAny<Order>()), Times.Never);
	}

	[TestMethod]
	public void CancelOrder_CalledTwice_ReachesConnectorOnce()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };
		MarkStarted(strategy);

		var order = RegisterOwnActiveOrder(strategy, connMock);
		connMock.Invocations.Clear();

		// The second cancel of the same order is a duplicate and must be deduplicated (IsCanceled flag).
		strategy.CancelOrder(order);
		strategy.CancelOrder(order);

		connMock.Verify(c => c.CancelOrder(order), Times.Once);
	}

	#endregion

	#region IndicatorSource parity (monolith vs decomposed)

	// Indicator added with null Source inherits IndicatorSource (??=); one with its own Source keeps it.
	// Pinned 1:1 against the monolith StrategyOld.IndicatorList.OnAdded.
#pragma warning disable CS0618 // parity test deliberately exercises the obsolete StrategyOld monolith engine

	[TestMethod]
	public void IndicatorSource_NullSource_Decomposed_InheritsStrategyDefault()
	{
		var strategy = new Strategy { IndicatorSource = Level1Fields.BestBidPrice };

		var indicator = new SimpleMovingAverage();
		IsNull(indicator.Source);

		strategy.Indicators.TryAdd(indicator);

		indicator.Source.AreEqual(Level1Fields.BestBidPrice);
	}

	[TestMethod]
	public void IndicatorSource_NullSource_Monolith_InheritsStrategyDefault()
	{
		var strategy = new StrategyOld { IndicatorSource = Level1Fields.BestBidPrice };

		var indicator = new SimpleMovingAverage();
		IsNull(indicator.Source);

		strategy.Indicators.TryAdd(indicator);

		indicator.Source.AreEqual(Level1Fields.BestBidPrice);
	}

	[TestMethod]
	public void IndicatorSource_NullSource_DecomposedMatchesMonolith()
	{
		const Level1Fields source = Level1Fields.BestBidPrice;

		var monolith = new StrategyOld { IndicatorSource = source };
		var decomposed = new Strategy { IndicatorSource = source };

		var monolithIndicator = new SimpleMovingAverage();
		var decomposedIndicator = new SimpleMovingAverage();

		monolith.Indicators.TryAdd(monolithIndicator);
		decomposed.Indicators.TryAdd(decomposedIndicator);

		// Both engines must leave the indicator with Source == IndicatorSource (1:1).
		monolithIndicator.Source.AreEqual(source);
		decomposedIndicator.Source.AreEqual(monolithIndicator.Source);
	}

	[TestMethod]
	public void IndicatorSource_NonNullSource_DecomposedMatchesMonolith()
	{
		const Level1Fields strategySource = Level1Fields.BestBidPrice;
		const Level1Fields ownSource = Level1Fields.BestAskPrice;

		var monolith = new StrategyOld { IndicatorSource = strategySource };
		var decomposed = new Strategy { IndicatorSource = strategySource };

		var monolithIndicator = new SimpleMovingAverage { Source = ownSource };
		var decomposedIndicator = new SimpleMovingAverage { Source = ownSource };

		monolith.Indicators.TryAdd(monolithIndicator);
		decomposed.Indicators.TryAdd(decomposedIndicator);

		// The ??= semantics keep an indicator's own Source untouched on both engines.
		monolithIndicator.Source.AreEqual(ownSource);
		decomposedIndicator.Source.AreEqual(monolithIndicator.Source);
	}

#pragma warning restore CS0618

	#endregion

	#region Decomposed-only new features (PositionsList, target-position API, sub-object event ordering, PnLReceived2)

	// Features only the decomposed Strategy exposes (so not comparable in the equivalence suites), pinned
	// directly: PositionsList (per-(securityId, portfolio) net view); the SetTargetPosition API; the
	// sub-objects' internal events firing before/consistently with the public surface; and PnLReceived2.

	private static readonly DateTime _newFeatureTradeTime = new(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

	private static Order CreateNewFeatureOrder(Security security, Portfolio portfolio, Sides side, decimal price, decimal volume, long txId)
		=> new()
		{
			TransactionId = txId,
			State = OrderStates.Pending,
			Side = side,
			Price = price,
			Volume = volume,
			Security = security,
			Portfolio = portfolio,
			Time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc),
		};

	private static MyTrade CreateNewFeatureTrade(Order order, long tradeId, decimal price, decimal volume)
		=> new()
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = tradeId,
				TradePrice = price,
				TradeVolume = volume,
				SecurityId = order.Security.ToSecurityId(),
				// LocalTime drives PnLReceived2's timestamp; set it (UTC) explicitly to pin that.
				ServerTime = _newFeatureTradeTime,
				LocalTime = _newFeatureTradeTime,
			},
		};

	// Drive a Pending->Active->fill cycle for one order, stamped with the strategy id (UserOrderId) so
	// CanAttach owns it on any security/portfolio (auto-attach only covers the primary Security+Portfolio).
	private static void RegisterFillThroughStrategy(Strategy strategy, Subscription sub,
		Security security, Portfolio portfolio, Sides side, decimal price, decimal volume, long txId, long tradeId)
	{
		var order = CreateNewFeatureOrder(security, portfolio, side, price, volume, txId);
		order.UserOrderId = strategy.Id.To<string>();

		strategy.OnConnectorOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		strategy.OnTradeReceived(sub, CreateNewFeatureTrade(order, tradeId, price, volume));
	}

	[TestMethod]
	public void PositionsList_KeyedBySecurityAndPortfolio_NetValuesPerKey()
	{
		var connMock = CreateMockConnector();

		var sec1 = CreateSecurity("AAA@NYSE");
		var sec2 = CreateSecurity("BBB@NYSE");
		var pf1 = CreatePortfolio("PF1");
		var pf2 = CreatePortfolio("PF2");

		// Primary (Security/Portfolio) is sec1/pf1 so the Position aggregate tracks that key.
		var strategy = new Strategy
		{
			Connector = connMock.Object,
			Security = sec1,
			Portfolio = pf1,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// sec1/pf1: buy 10 then sell 4 -> net +6.
		RegisterFillThroughStrategy(strategy, sub, sec1, pf1, Sides.Buy, 100m, 10m, txId: 1, tradeId: 1);
		RegisterFillThroughStrategy(strategy, sub, sec1, pf1, Sides.Sell, 101m, 4m, txId: 2, tradeId: 2);

		// sec2/pf1: sell 5 -> net -5 (different security, same portfolio).
		RegisterFillThroughStrategy(strategy, sub, sec2, pf1, Sides.Sell, 50m, 5m, txId: 3, tradeId: 3);

		// sec1/pf2: buy 3 -> net +3 (same security, different portfolio -> distinct key, not folded into sec1/pf1).
		RegisterFillThroughStrategy(strategy, sub, sec1, pf2, Sides.Buy, 100m, 3m, txId: 4, tradeId: 4);

		var list = strategy.PositionsList;

		// Three distinct (securityId, portfolioName) keys.
		list.Count.AreEqual(3);

		var k1 = (sec1.ToSecurityId(), pf1.Name);
		var k2 = (sec2.ToSecurityId(), pf1.Name);
		var k3 = (sec1.ToSecurityId(), pf2.Name);

		IsTrue(list.ContainsKey(k1));
		IsTrue(list.ContainsKey(k2));
		IsTrue(list.ContainsKey(k3));

		list[k1].AreEqual(6m);   // +10 - 4
		list[k2].AreEqual(-5m);  // -5
		list[k3].AreEqual(3m);   // +3

		// The dictionary is keyed by securityId AND portfolio: sec1 under two portfolios stays split.
		AreNotEqual(k1, k3);

		// The Position aggregate (primary security/portfolio = sec1/pf1) stays consistent with the
		// matching dictionary entry and is NOT polluted by the other keys.
		strategy.Position.AreEqual(6m);
		strategy.Position.AreEqual(list[k1]);

		// Cross-check against the per-(security,portfolio) accessor that backs the aggregate.
		strategy.GetPositionValue(sec1, pf1).AreEqual(6m);
		strategy.GetPositionValue(sec2, pf1).AreEqual(-5m);
		strategy.GetPositionValue(sec1, pf2).AreEqual(3m);
	}

	// Drive the strategy to IsOnline=true: mark every tracked subscription Online, start, then raise
	// SubscriptionOnline so RefreshOnlineState (which needs ALL non-history-only subs Online) passes.
	private static void DriveOnline(Strategy strategy, Mock<IConnector> connMock)
	{
		MarkStarted(strategy);

		Subscription last = null;
		foreach (var s in strategy.Subscriptions.Subscriptions)
		{
			s.State = SubscriptionStates.Online;
			last = s;
		}

		connMock.Raise(c => c.SubscriptionOnline += null, last);
		IsTrue(strategy.IsOnline, "Strategy must be online so the target-position canTrade gate passes");
	}

	[TestMethod]
	public void SetTargetPosition_EmitsOrderTowardTarget_AndCancelClears()
	{
		var connMock = CreateMockConnector();
		var registered = new List<Order>();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>())).Callback<Order>(registered.Add);

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new Strategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		// A non-history-only subscription that, once Online, makes the strategy Online.
		var sub = new Subscription(DataType.MarketDepth, security);
		strategy.Subscriptions.Subscribe(sub);
		DriveOnline(strategy, connMock);

		// No target set yet.
		IsNull(strategy.GetTargetPosition());

		// Current position is 0; target +7 must emit a Buy market order for the full delta toward target.
		strategy.SetTargetPosition(7m);

		strategy.GetTargetPosition().AreEqual(7m);

		AreEqual(1, registered.Count, "SetTargetPosition must emit one order toward the target");
		var order = registered[0];
		order.Side.AreEqual(Sides.Buy);
		order.Volume.AreEqual(7m);            // delta = target(7) - current(0)
		order.Type.AreEqual(OrderTypes.Market);
		order.Security.AreEqual(security);
		order.Portfolio.AreEqual(portfolio);

		// The target manager is the engaged object behind the high-level API.
		IsTrue(strategy.TargetPositionManager.GetTarget(security, portfolio) == 7m);

		// Cancelling the target clears it (GetTargetPosition -> null) and does not emit a new order.
		strategy.CancelTargetPosition();
		IsNull(strategy.GetTargetPosition());
		AreEqual(1, registered.Count, "CancelTargetPosition must not emit a new order");
	}

	[TestMethod]
	public void SetTargetPosition_WhenNotOnline_DoesNotEmitButRemembersTarget()
	{
		// Not online: the target is recorded but no order is emitted (target stored independently of execution).
		var connMock = CreateMockConnector();
		var registered = new List<Order>();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>())).Callback<Order>(registered.Add);

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new Strategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		MarkStarted(strategy);
		IsFalse(strategy.IsOnline);

		strategy.SetTargetPosition(5m);

		strategy.GetTargetPosition().AreEqual(5m);
		AreEqual(0, registered.Count, "No order may be emitted while canTrade (online) is false");

		// Cancelling still clears the remembered target.
		strategy.CancelTargetPosition();
		IsNull(strategy.GetTargetPosition());
	}

	[TestMethod]
	public void Engine_StateChanged_DrivesAndIsConsistentWith_PublicProcessStateChanged()
	{
		var connMock = CreateMockConnector();
		var strategy = new Strategy { Connector = connMock.Object };

		var engineStates = new List<ProcessStates>();
		var publicStates = new List<ProcessStates>();

		// Engine.StateChanged is the source that drives the public ProcessStateChanged (wired in the ctor):
		// both must observe the same state sequence.
		strategy.Engine.StateChanged += s => engineStates.Add(s);
		strategy.ProcessStateChanged += s => publicStates.Add(s.ProcessState);

		MarkStarted(strategy);
		strategy.ProcessState.AreEqual(ProcessStates.Started);

		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		strategy.ProcessState.AreEqual(ProcessStates.Stopping);

		// Each transition produced exactly one engine event and one public event, in the same order with
		// the same state values (the public surface mirrors the engine sub-object 1:1).
		engineStates.SequenceEqual([ProcessStates.Started, ProcessStates.Stopping]).AssertTrue();
		publicStates.SequenceEqual([ProcessStates.Started, ProcessStates.Stopping]).AssertTrue();
		engineStates.SequenceEqual(publicStates).AssertTrue();

		// The engine's own ProcessState is the authoritative value the public ProcessState reflects.
		strategy.Engine.ProcessState.AreEqual(strategy.ProcessState);
	}

	[TestMethod]
	public void OrderProcessor_Registered_FiresBeforeAndConsistentWith_PublicOrderReceived()
	{
		var connMock = CreateMockConnector();
		var strategy = new Strategy { Connector = connMock.Object };

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var sequence = new List<string>();
		Order registeredOrder = null;
		Order receivedOrder = null;

		strategy.OrderProcessor.Registered += o => { registeredOrder = o; sequence.Add("registered"); };
		strategy.OrderReceived += (_, o) => { receivedOrder = o; sequence.Add("received"); };

		// Pending snapshot: only OrderReceived fires (not yet Registered).
		var order = CreateNewFeatureOrder(security, portfolio, Sides.Buy, 100m, 10m, txId: 1);
		strategy.OnConnectorOrderReceived(sub, order);

		sequence.SequenceEqual(["received"]).AssertTrue();
		IsNull(registeredOrder);

		// Active transition: OrderProcessor.Registered fires, THEN the public OrderReceived for the same update.
		sequence.Clear();
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		sequence.SequenceEqual(["registered", "received"]).AssertTrue();
		IsNotNull(registeredOrder);
		IsNotNull(receivedOrder);
		// Both events observed the SAME order instance.
		AreEqual(order, registeredOrder);
		AreEqual(order, receivedOrder);
		registeredOrder.TransactionId.AreEqual(1L);
		registeredOrder.State.AreEqual(OrderStates.Active);
	}

	[TestMethod]
	public void Trades_TradeAdded_FiresBeforeAndConsistentWith_PublicOwnTradeReceived()
	{
		var connMock = CreateMockConnector();
		var strategy = new Strategy { Connector = connMock.Object };

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var sequence = new List<string>();
		MyTrade addedTrade = null;
		MyTrade publicTrade = null;

		strategy.Trades.TradeAdded += t => { addedTrade = t; sequence.Add("added"); };
		strategy.OwnTradeReceived += (_, t) => { publicTrade = t; sequence.Add("public"); };

		var order = CreateNewFeatureOrder(security, portfolio, Sides.Buy, 100m, 10m, txId: 1);
		strategy.OnConnectorOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);

		var trade = CreateNewFeatureTrade(order, tradeId: 7, price: 100m, volume: 10m);
		strategy.OnTradeReceived(sub, trade);

		// Sub-object TradeAdded precedes the public OwnTradeReceived, both once, same instance.
		sequence.SequenceEqual(["added", "public"]).AssertTrue();
		IsNotNull(addedTrade);
		IsNotNull(publicTrade);
		AreEqual(trade, addedTrade);
		AreEqual(trade, publicTrade);

		// The trade is now part of the public trade surface, consistent with the sub-object.
		strategy.Trades.MyTrades.Contains(trade).AssertTrue();
		strategy.MyTrades.Contains(trade).AssertTrue();
		strategy.Trades.MyTrades.Count().AreEqual(strategy.MyTrades.Count());
	}

	[TestMethod]
	public void Engine_UnrealizedPnLInterval_BackedBy_StrategyProperty()
	{
		// Strategy.UnrealizedPnLInterval delegates to the engine sub-object; pin that they stay in lock-step.
		var strategy = new Strategy();

		// Default 1 minute on both.
		strategy.UnrealizedPnLInterval.AreEqual(TimeSpan.FromMinutes(1));
		strategy.Engine.UnrealizedPnLInterval.AreEqual(TimeSpan.FromMinutes(1));

		strategy.UnrealizedPnLInterval = TimeSpan.FromSeconds(30);
		strategy.Engine.UnrealizedPnLInterval.AreEqual(TimeSpan.FromSeconds(30));
		strategy.UnrealizedPnLInterval.AreEqual(TimeSpan.FromSeconds(30));
	}

	[TestMethod]
	public void PnLReceived2_FiresWithPnLReceived_CarryingConsistentPayload()
	{
		var connMock = CreateMockConnector();

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new Strategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var sequence = new List<string>();
		var pnl2Count = 0;

		Portfolio capturedPf = null;
		DateTime capturedTime = default;
		decimal capturedRealized = 0;
		decimal? capturedUnrealized = null;
		decimal? capturedCommission = null;

#pragma warning disable CS0618 // PnLReceived is obsolete but still raised alongside PnLReceived2.
		strategy.PnLReceived += _ => sequence.Add("obsolete");
#pragma warning restore CS0618
		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			sequence.Add("pnl2");
			pnl2Count++;
			capturedPf = pf;
			capturedTime = time;
			capturedRealized = realized;
			capturedUnrealized = unrealized;
			capturedCommission = commission;
		};

		// Opening buy at 100 (PnL == 0 -> no PnL change -> PnLReceived* must NOT fire yet).
		RegisterFillThroughStrategy(strategy, sub, security, portfolio, Sides.Buy, 100m, 10m, txId: 1, tradeId: 1);
		AreEqual(0, pnl2Count, "Opening trade with zero realized PnL must not raise PnLReceived2");

		// Closing sell at 110 realizes +100 of PnL -> PnL changes -> both events fire together.
		RegisterFillThroughStrategy(strategy, sub, security, portfolio, Sides.Sell, 110m, 10m, txId: 2, tradeId: 2);

		IsTrue(pnl2Count >= 1, "PnLReceived2 must fire when PnL recomputes on a realizing trade");

		// Both events fire together (same count) and PnLReceived2 carries the consistent detailed payload.
		var obsoleteCount = sequence.Count(x => x == "obsolete");
		var pnl2Seq = sequence.Count(x => x == "pnl2");
		obsoleteCount.AreEqual(pnl2Seq);
		AreEqual(strategy.PnLManager.RealizedPnL, capturedRealized,
			"PnLReceived2 realized must equal the live PnLManager.RealizedPnL");
		capturedRealized.AreEqual(100m);
		AreEqual(strategy.PnLManager.UnrealizedPnL, capturedUnrealized);
		AreEqual(strategy.Commission, capturedCommission);
		AreEqual(portfolio, capturedPf);
		// Time stamp is the realizing trade's LocalTime (UTC), not default.
		capturedTime.AreEqual(_newFeatureTradeTime);
		capturedTime.Kind.AreEqual(DateTimeKind.Utc);
	}

	#endregion
}
