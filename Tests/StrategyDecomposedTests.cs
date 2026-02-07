namespace StockSharp.Tests;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Statistics;
using StockSharp.Algo.Strategies.Decomposed;

[TestClass]
public class StrategyDecomposedTests : BaseTestClass
{
	#region FakeHost

	private class FakeHost : IStrategyHost
	{
		private long _nextId = 1000;

		public DateTime CurrentTimeUtc { get; set; } = DateTime.UtcNow;

		public List<Message> SentMessages { get; } = [];

		public void SendOutMessage(Message message) => SentMessages.Add(message);

		public long GetNextTransactionId() => Interlocked.Increment(ref _nextId);
	}

	#endregion

	#region StrategyEngine tests

	[TestMethod]
	public void StrategyEngine_RequestStart_SendsStartedMessage()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		engine.RequestStart();

		host.SentMessages.Count.AreEqual(1);
		var msg = host.SentMessages[0] as StrategyEngine.StrategyStateMessage;
		IsNotNull(msg);
		msg.RequestedState.AreEqual(ProcessStates.Started);
	}

	[TestMethod]
	public void StrategyEngine_RequestStop_SendsStoppingMessage()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		engine.RequestStart();
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		engine.ProcessState.AreEqual(ProcessStates.Started);

		host.SentMessages.Clear();
		engine.RequestStop();

		host.SentMessages.Count.AreEqual(1);
		var msg = host.SentMessages[0] as StrategyEngine.StrategyStateMessage;
		IsNotNull(msg);
		msg.RequestedState.AreEqual(ProcessStates.Stopping);
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
	public void StrategyEngine_RequestStop_WhenAlreadyStopped_NoMessage()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		engine.RequestStop();

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
	public void OrderPipeline_CancelAll_MarksAllCanceled()
	{
		using var stats = new StatisticManager();
		var pipeline = new OrderPipeline(stats);

		var order1 = new Order { TransactionId = 1, State = OrderStates.Active };
		var order2 = new Order { TransactionId = 2, State = OrderStates.Active };

		pipeline.TryAttach(order1);
		pipeline.TryAttach(order2);

		pipeline.CancelAll();

		pipeline.IsTracked(order1).AssertTrue();
		pipeline.IsTracked(order2).AssertTrue();
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

	private static Security CreateSecurity()
	{
		return new Security
		{
			Id = Helper.CreateSecurityId().ToStringId(),
			Board = ExchangeBoard.Nyse,
			PriceStep = 0.01m,
		};
	}

	private static Portfolio CreatePortfolio()
	{
		return new Portfolio { Name = "test_portfolio" };
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

	#region DecomposedStrategy composite tests

	// Test strategy that inherits DecomposedStrategy and records all hook calls.
	private class BuyOnSignalStrategy : DecomposedStrategy
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

		protected override void OnNewMyTrade(MyTrade trade)
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

	[TestMethod]
	public void Composite_StateTransitions_AllHooksCalled()
	{
		var connMock = CreateMockConnector();
		var strategy = new BuyOnSignalStrategy { Connector = connMock.Object };

		// initial state
		strategy.ProcessState.AreEqual(ProcessStates.Stopped);
		strategy.StateChanges.Count.AreEqual(0);

		// start
		strategy.Start();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		strategy.ProcessState.AreEqual(ProcessStates.Started);
		strategy.StateChanges.Count.AreEqual(1);
		strategy.StateChanges[0].AreEqual(ProcessStates.Started);

		// stop
		strategy.Stop();
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

		var order = new Order { TransactionId = 1 };

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
			Time = DateTime.UtcNow,
		};

		// untracked subscription — order NOT attached
		strategy.OnOrderReceived(untrackedSub, order);
		strategy.Orders.IsTracked(order).AssertFalse();
		strategy.Orders.Orders.Any().AssertFalse();

		// tracked subscription — order attached
		strategy.OnOrderReceived(trackedSub, order);
		strategy.Orders.IsTracked(order).AssertTrue();
		strategy.Orders.Orders.Count().AreEqual(1);
		strategy.Orders.Orders.First().AreEqual(order);
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
	public void Composite_FullOrderLifecycle_AllValuesVerified()
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
		strategy.OnOrderReceived(sub, order);
		strategy.Orders.IsTracked(order).AssertTrue();
		strategy.Orders.Orders.Count().AreEqual(1);
		strategy.RegisteredOrders.Count.AreEqual(0); // not registered yet

		// 2. order becomes Active → fires OnOrderRegistered
		order.State = OrderStates.Active;
		strategy.OnOrderReceived(sub, order);

		strategy.RegisteredOrders.Count.AreEqual(1);
		strategy.RegisteredOrders[0].AreEqual(order);
		strategy.RegisteredOrders[0].TransactionId.AreEqual(1L);
		strategy.RegisteredOrders[0].Price.AreEqual(100m);
		strategy.RegisteredOrders[0].Volume.AreEqual(10m);
		strategy.RegisteredOrders[0].Side.AreEqual(Sides.Buy);
		strategy.Orders.Commission.AreEqual(3m);

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
		strategy.OnOrderReceived(sub, order1);
		strategy.OnOrderReceived(sub, order2);
		strategy.Orders.Orders.Count().AreEqual(2);

		// activate both
		order1.State = OrderStates.Active;
		strategy.OnOrderReceived(sub, order1);
		order2.State = OrderStates.Active;
		strategy.OnOrderReceived(sub, order2);

		strategy.RegisteredOrders.Count.AreEqual(2);
		strategy.RegisteredOrders[0].TransactionId.AreEqual(1L);
		strategy.RegisteredOrders[0].Side.AreEqual(Sides.Buy);
		strategy.RegisteredOrders[0].Price.AreEqual(100m);
		strategy.RegisteredOrders[1].TransactionId.AreEqual(2L);
		strategy.RegisteredOrders[1].Side.AreEqual(Sides.Sell);
		strategy.RegisteredOrders[1].Price.AreEqual(110m);
		strategy.Orders.Commission.AreEqual(3m); // 2 + 1

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
	public void Composite_Position_NewAndChanged()
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
	public void Composite_BuyOnSignal_EndToEnd()
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
		strategy.Start();
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

		strategy.OnOrderReceived(sub, order);
		strategy.Orders.IsTracked(order).AssertTrue();
		strategy.Orders.Orders.Count().AreEqual(1);

		// order activated
		order.State = OrderStates.Active;
		order.Commission = 2m;
		strategy.OnOrderReceived(sub, order);

		strategy.RegisteredOrders.Count.AreEqual(1);
		strategy.RegisteredOrders[0].Price.AreEqual(150m);
		strategy.RegisteredOrders[0].Volume.AreEqual(5m);
		strategy.RegisteredOrders[0].Side.AreEqual(Sides.Buy);
		strategy.Orders.Commission.AreEqual(2m);

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
		strategy.Stop();
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

		// verify that events on old connector fire nothing
		// and new connector is used for operations
		var order = new Order { TransactionId = 1 };
		strategy.CancelOrder(order);
		conn1.Verify(c => c.CancelOrder(It.IsAny<Order>()), Times.Never);
		conn2.Verify(c => c.CancelOrder(order), Times.Once);
	}

	#endregion
}
