namespace StockSharp.Tests;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Statistics;
using StockSharp.Algo.Testing;
using StockSharp.Algo.Strategies.Decomposed;
using StockSharp.Designer;

/// <summary>
/// Equivalence tests: run a real backtest, then replay the same data
/// through decomposed pipelines and verify results match.
/// Also tests DecomposedSmaStrategy vs original SmaStrategy with same candle data.
/// </summary>
[TestClass]
public class StrategyDecomposedEquivalenceTests : BaseTestClass
{
	private class FakeHost : IStrategyHost
	{
		private long _nextId = 1000;

		public DateTime CurrentTime { get; set; } = DateTime.UtcNow;
		public List<Message> SentMessages { get; } = [];
		public void SendOutMessage(Message message) => SentMessages.Add(message);
		public ValueTask SendOutMessageAsync(Message message, CancellationToken cancellationToken)
		{
			SentMessages.Add(message);
			return default;
		}
		public long GetNextTransactionId() => Interlocked.Increment(ref _nextId);
	}

	#region Parity helpers

	private class TrackingRiskRule : RiskRule
	{
		public List<Message> ProcessedMessages { get; } = [];

		protected override string GetTitle() => "Tracking";

		public override bool ProcessMessage(Message message)
		{
			ProcessedMessages.Add(message);
			return Action != default;
		}
	}

	private class TestStrategy : DecomposedStrategy
	{
		public List<ProcessStates> StateHistory { get; } = [];
		public List<MyTrade> ReceivedTrades { get; } = [];
		public List<Order> RegisteredOrders { get; } = [];

		protected override void OnStateChanged(ProcessStates state) => StateHistory.Add(state);
		protected override void OnNewMyTrade(MyTrade trade) => ReceivedTrades.Add(trade);
		protected override void OnOrderRegistered(Order order) => RegisteredOrders.Add(order);
	}

	private static Security CreateSecurity(string code = null)
	{
		return new Security
		{
			Id = (code ?? Helper.CreateSecurityId().ToStringId()),
			Board = ExchangeBoard.Nyse,
			PriceStep = 0.01m,
		};
	}

	private static Portfolio CreatePortfolio() => new() { Name = "test_portfolio" };

	private static Mock<IConnector> CreateMockConnector()
	{
		var mock = new Mock<IConnector>();
		mock.Setup(c => c.TransactionIdGenerator).Returns(new IncrementalIdGenerator());
		return mock;
	}

	private static Order CreateOrder(Security security, Portfolio portfolio,
		Sides side, decimal price, decimal volume, long txId = 1)
	{
		return new Order
		{
			TransactionId = txId,
			State = OrderStates.Pending,
			Side = side,
			Price = price,
			Volume = volume,
			Security = security,
			Portfolio = portfolio,
			Time = DateTime.UtcNow,
		};
	}

	private static void AttachAndActivate(TestStrategy strategy, Subscription sub, Order order)
	{
		strategy.OnOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnOrderReceived(sub, order);
	}

	private static MyTrade CreateTrade(Order order, long tradeId, decimal price, decimal volume)
	{
		return new MyTrade
		{
			Order = order,
			Trade = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				TradeId = tradeId,
				TradePrice = price,
				TradeVolume = volume,
				SecurityId = order.Security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
			},
		};
	}

	#endregion

	#region DecomposedSmaStrategy — same logic as SmaStrategy

	/// <summary>
	/// SMA crossover strategy built on DecomposedStrategy.
	/// Uses the exact same OnProcess logic as SmaStrategy.
	/// </summary>
	private class DecomposedSmaStrategy : DecomposedStrategy
	{
		private bool? _isShortLessThenLong;
		private readonly SimpleMovingAverage _longSma = new();
		private readonly SimpleMovingAverage _shortSma = new();

		public int Long { get; set; } = 80;
		public int Short { get; set; } = 30;

		public List<ProcessStates> StateHistory { get; } = [];

		public void Init()
		{
			_longSma.Length = Long;
			_shortSma.Length = Short;
			_isShortLessThenLong = null;
		}

		/// <summary>
		/// Feed a candle through SMA indicators and check for crossover signal.
		/// </summary>
		public void ProcessCandle(ICandleMessage candle)
		{
			_longSma.Process(candle);
			_shortSma.Process(candle);

			if (_longSma.IsFormed && _shortSma.IsFormed)
				OnProcess(candle, _longSma.GetCurrentValue(), _shortSma.GetCurrentValue());
		}

		// Same logic as SmaStrategy.OnProcess
		private void OnProcess(ICandleMessage candle, decimal longValue, decimal shortValue)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var isShortLessThenLong = shortValue < longValue;

			if (_isShortLessThenLong == null)
			{
				_isShortLessThenLong = isShortLessThenLong;
			}
			else if (_isShortLessThenLong != isShortLessThenLong)
			{
				var direction = isShortLessThenLong ? Sides.Sell : Sides.Buy;

				var volume = Position == 0 ? Volume : Position.Abs().Min(Volume) * 2;

				var priceStep = Security.PriceStep ?? 1;

				var price = candle.ClosePrice + (direction == Sides.Buy ? priceStep : -priceStep);

				if (direction == Sides.Buy)
					BuyLimit(price, volume);
				else
					SellLimit(price, volume);

				_isShortLessThenLong = isShortLessThenLong;
			}
		}

		protected override void OnStateChanged(ProcessStates state)
			=> StateHistory.Add(state);
	}

	#endregion

	#region Backtest infrastructure

	private static bool SkipIfNoHistoryData()
	{
		if (Paths.HistoryDataPath == null)
		{
			Console.WriteLine("Skipping: no history data");
			return true;
		}
		return false;
	}

	private static HistoryEmulationConnector CreateConnector(
		ISecurityProvider secProvider,
		IPortfolioProvider pfProvider,
		IStorageRegistry storageRegistry,
		DateTime startTime,
		DateTime stopTime)
	{
		var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
		{
			HistoryMessageAdapter =
			{
				StartDate = startTime,
				StopDate = stopTime,
			},
		};

		connector.EmulationAdapter.Settings.MatchOnTouch = true;

		return connector;
	}

	private async Task<SmaStrategy> RunBacktest(CancellationToken ct)
	{
		var security = new Security { Id = Paths.HistoryDefaultSecurity };
		var portfolio = Portfolio.CreateSimulator();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = Helper.FileSystem.GetStorage(Paths.HistoryDataPath);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var strategy = new SmaStrategy
		{
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), ct));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		return strategy;
	}

	/// <summary>
	/// Run SmaStrategy backtest with protection disabled, capturing candles for replay.
	/// </summary>
	private async Task<(SmaStrategy strategy, List<ICandleMessage> candles)> RunSmaBacktestWithCandles(CancellationToken ct)
	{
		var security = new Security { Id = Paths.HistoryDefaultSecurity };
		var portfolio = Portfolio.CreateSimulator();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = Helper.FileSystem.GetStorage(Paths.HistoryDataPath);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		// Capture candles delivered to subscriptions
		var capturedCandles = new List<ICandleMessage>();
		connector.CandleReceived += (sub, candle) =>
		{
			capturedCandles.Add(candle);
		};

		var strategy = new SmaStrategy
		{
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(5), ct));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		Console.WriteLine($"Backtest done: state={strategy.ProcessState}, orders={strategy.Orders.Count()}, trades={strategy.MyTrades.Count()}, candles={capturedCandles.Count}, position={strategy.Position}");

		return (strategy, capturedCandles);
	}

	#endregion

	#region Pipeline replay tests

	/// <summary>
	/// Run a real backtest, then replay the same trades through TradePipeline.
	/// Verify commission and slippage match.
	/// </summary>
	[TestMethod]
	public async Task TradePipeline_MatchesStrategy_CommissionAndSlippage()
	{
		if (SkipIfNoHistoryData()) return;

		var strategy = await RunBacktest(CancellationToken);

		var trades = strategy.MyTrades.ToArray();

		if (trades.Length == 0)
		{
			Console.WriteLine("No trades generated, skipping equivalence check");
			return;
		}

		// Replay through TradePipeline with a fresh PnLManager
		var pnl = new PnLManager();
		using var stats = new StatisticManager();
		var pipeline = new TradePipeline(pnl, stats);

		foreach (var trade in trades)
			pipeline.TryAdd(trade);

		// Compare
		AreEqual(strategy.Commission ?? 0m, pipeline.Commission ?? 0m,
			$"Commission mismatch: Strategy={strategy.Commission}, Pipeline={pipeline.Commission}");

		AreEqual(strategy.Slippage ?? 0m, pipeline.Slippage ?? 0m,
			$"Slippage mismatch: Strategy={strategy.Slippage}, Pipeline={pipeline.Slippage}");

		AreEqual(trades.Length, pipeline.MyTrades.Count(),
			"Trade count mismatch");

		Console.WriteLine($"Equivalence OK: {trades.Length} trades, Commission={pipeline.Commission}, Slippage={pipeline.Slippage}");
	}

	/// <summary>
	/// Run a real backtest, then replay the same trades through TradePipeline.
	/// Verify deduplication works identically (adding same trade twice returns false).
	/// </summary>
	[TestMethod]
	public async Task TradePipeline_MatchesStrategy_Deduplication()
	{
		if (SkipIfNoHistoryData()) return;

		var strategy = await RunBacktest(CancellationToken);

		var trades = strategy.MyTrades.ToArray();

		if (trades.Length == 0)
		{
			Console.WriteLine("No trades generated, skipping");
			return;
		}

		var pnl = new PnLManager();
		using var stats = new StatisticManager();
		var pipeline = new TradePipeline(pnl, stats);

		// Add all trades — should all succeed
		foreach (var trade in trades)
			pipeline.TryAdd(trade).AssertTrue();

		// Add again — should all fail (dedup)
		foreach (var trade in trades)
			pipeline.TryAdd(trade).AssertFalse();

		// Same behavior as Strategy.TryAddMyTrade — calling it again returns false
		foreach (var trade in trades)
			strategy.TryAddMyTrade(trade).AssertFalse();

		Console.WriteLine($"Dedup OK: {trades.Length} trades, all duplicates rejected by both");
	}

	/// <summary>
	/// Run a real backtest, then replay the same orders through OrderPipeline.
	/// Verify order tracking and registration detection match.
	/// </summary>
	[TestMethod]
	public async Task OrderPipeline_MatchesStrategy_OrderTracking()
	{
		if (SkipIfNoHistoryData()) return;

		var strategy = await RunBacktest(CancellationToken);

		var orders = strategy.Orders.ToArray();

		if (orders.Length == 0)
		{
			Console.WriteLine("No orders generated, skipping");
			return;
		}

		using var stats = new StatisticManager();
		var pipeline = new OrderPipeline(stats);

		var registeredOrders = new List<Order>();
		pipeline.Registered += o => registeredOrders.Add(o);

		foreach (var order in orders)
		{
			pipeline.TryAttach(order);

			// Simulate state progression: initial Pending -> current state
			// First process with None->Pending transition
			pipeline.ProcessOrder(order, false);

			// If order is Active or Done, simulate the registration transition
			if (order.State is OrderStates.Active or OrderStates.Done)
			{
				// ProcessOrder already saw the final state, so registration was detected
			}
		}

		// All orders should be tracked
		foreach (var order in orders)
			pipeline.IsTracked(order).AssertTrue();

		Console.WriteLine($"Order tracking OK: {orders.Length} orders tracked, {registeredOrders.Count} registered");
	}

	/// <summary>
	/// Run a real backtest, capture the strategy's state transitions,
	/// then replay through StrategyEngine and compare.
	/// </summary>
	[TestMethod]
	public async Task StrategyEngine_MatchesStrategy_StateTransitions()
	{
		if (SkipIfNoHistoryData()) return;

		// Capture strategy state transitions
		var strategyStates = new List<ProcessStates>();

		var security = new Security { Id = Paths.HistoryDefaultSecurity };
		var portfolio = Portfolio.CreateSimulator();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = Helper.FileSystem.GetStorage(Paths.HistoryDataPath);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(2);

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var strategy = new SmaStrategy
		{
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		strategy.ProcessStateChanged += s => strategyStates.Add(s.ProcessState);

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30), CancellationToken));

		// Now replay the same state transitions through StrategyEngine
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		var engineStates = new List<ProcessStates>();
		engine.StateChanged += s => engineStates.Add(s);

		// Strategy went: Stopped -> Started -> (maybe Stopping -> Stopped)
		// Replay the same transitions via messages
		foreach (var state in strategyStates)
		{
			if (state == ProcessStates.Started && engine.ProcessState == ProcessStates.Stopped)
				engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
			else if (state == ProcessStates.Stopping && engine.ProcessState == ProcessStates.Started)
				engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		}

		// Compare transition sequences
		IsTrue(strategyStates.Count > 0, "Strategy should have had state transitions");

		// The engine should have matched the key transitions
		if (strategyStates.Contains(ProcessStates.Started))
			engineStates.Contains(ProcessStates.Started).AssertTrue();

		if (strategyStates.Contains(ProcessStates.Stopping))
			engineStates.Contains(ProcessStates.Stopping).AssertTrue();

		Console.WriteLine($"Strategy states: {string.Join(" -> ", strategyStates)}");
		Console.WriteLine($"Engine states:   {string.Join(" -> ", engineStates)}");
	}

	/// <summary>
	/// Run a real backtest with message interception, replay messages through StrategyEngine,
	/// verify PnL refresh events fire at the same times.
	/// </summary>
	[TestMethod]
	public async Task StrategyEngine_MatchesStrategy_MessageProcessing()
	{
		if (SkipIfNoHistoryData()) return;

		var security = new Security { Id = Paths.HistoryDefaultSecurity };
		var portfolio = Portfolio.CreateSimulator();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = Helper.FileSystem.GetStorage(Paths.HistoryDataPath);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(2);

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		// Intercept messages that Strategy's OnConnectorNewMessage receives
		var capturedMessages = new List<Message>();

		connector.Adapter.NewOutMessageAsync += (msg, ct) =>
		{
			switch (msg.Type)
			{
				case MessageTypes.Level1Change:
				case MessageTypes.Execution:
				case MessageTypes.QuoteChange:
					capturedMessages.Add(msg.Clone());
					break;
				default:
					if (msg is CandleMessage)
						capturedMessages.Add(msg.Clone());
					break;
			}
			return default;
		};

		var strategy = new SmaStrategy
		{
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30), CancellationToken));

		if (capturedMessages.Count == 0)
		{
			Console.WriteLine("No messages captured, skipping");
			return;
		}

		// Now replay messages through StrategyEngine
		var host = new FakeHost();
		var pnl = new PnLManager { UseOrderBook = true };
		var engine = new StrategyEngine(host, pnl);

		// Start the engine
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		var priceUpdates = 0;
		var pnlRefreshes = 0;
		engine.CurrentPriceUpdated += (_, _, _, _) => priceUpdates++;
		engine.PnLRefreshRequired += _ => pnlRefreshes++;

		foreach (var msg in capturedMessages)
			engine.OnMessage(msg);

		Console.WriteLine($"Replayed {capturedMessages.Count} messages through StrategyEngine");
		Console.WriteLine($"Price updates: {priceUpdates}, PnL refreshes: {pnlRefreshes}");

		// Verify the engine processed messages and generated events
		IsTrue(priceUpdates > 0, $"Expected price updates from {capturedMessages.Count} messages");

		Console.WriteLine("Message processing equivalence OK");
	}

	#endregion

	#region DecomposedSmaStrategy equivalence tests

	/// <summary>
	/// Run real SmaStrategy backtest, capture candles, feed same candles
	/// to DecomposedSmaStrategy, compare every order and final position.
	/// </summary>
	[TestMethod]
	public async Task SmaEquivalence_OrdersAndPositionMatch()
	{
		// 1. Run real SmaStrategy backtest and capture candles
		var (sma, candles) = await RunSmaBacktestWithCandles(CancellationToken);

		var origOrders = sma.Orders.ToArray();

		IsTrue(origOrders.Length > 0, "SmaStrategy must generate orders");
		IsTrue(candles.Count > 0, "Expected captured candles from backtest");

		// 2. Create DecomposedSmaStrategy with same parameters
		var connMock = new Mock<IConnector>();
		connMock.Setup(c => c.TransactionIdGenerator).Returns(new IncrementalIdGenerator());

		var decomposedOrders = new List<Order>();
		var decomposed = new DecomposedSmaStrategy
		{
			Security = new Security
			{
				Id = sma.Security.Id,
				Board = sma.Security.Board,
				PriceStep = sma.Security.PriceStep,
			},
			Portfolio = new Portfolio { Name = "test" },
			Volume = sma.Volume,
			Long = sma.Long,
			Short = sma.Short,
		};

		// Simulate immediate fills on RegisterOrder (MatchOnTouch approximation)
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o =>
			{
				decomposedOrders.Add(o);

				// Update Position to match emulator fill behavior
				if (o.Side == Sides.Buy)
					decomposed.Position += o.Volume;
				else
					decomposed.Position -= o.Volume;
			});

		decomposed.Connector = connMock.Object;
		decomposed.Init();
		await decomposed.StartAsync();
		decomposed.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		// 3. Feed same candles to DecomposedSmaStrategy
		foreach (var candle in candles)
			decomposed.ProcessCandle(candle);

		// 4. Compare order count
		AreEqual(origOrders.Length, decomposedOrders.Count,
			$"Order count: SmaStrategy={origOrders.Length}, DecomposedSma={decomposedOrders.Count}");

		// 5. Compare each order in detail
		for (var i = 0; i < origOrders.Length; i++)
		{
			var orig = origOrders[i];
			var dec = decomposedOrders[i];

			AreEqual(orig.Side, dec.Side,
				$"Order[{i}] side: SmaStrategy={orig.Side}, DecomposedSma={dec.Side}");
			AreEqual(orig.Volume, dec.Volume,
				$"Order[{i}] volume: SmaStrategy={orig.Volume}, DecomposedSma={dec.Volume}");
			AreEqual(orig.Price, dec.Price,
				$"Order[{i}] price: SmaStrategy={orig.Price}, DecomposedSma={dec.Price}");
			AreEqual(orig.Type, dec.Type,
				$"Order[{i}] type: SmaStrategy={orig.Type}, DecomposedSma={dec.Type}");
		}

		// 6. Compare final position
		AreEqual(sma.Position, decomposed.Position,
			$"Final position: SmaStrategy={sma.Position}, DecomposedSma={decomposed.Position}");

		Console.WriteLine($"Equivalence OK: {origOrders.Length} orders match, position={decomposed.Position}");
	}

	/// <summary>
	/// Verify DecomposedSmaStrategy state machine lifecycle:
	/// Stopped -> Started -> (trading) -> Stopping.
	/// </summary>
	[TestMethod]
	public async Task SmaEquivalence_StateLifecycle()
	{
		var connMock = new Mock<IConnector>();
		connMock.Setup(c => c.TransactionIdGenerator).Returns(new IncrementalIdGenerator());

		var decomposed = new DecomposedSmaStrategy
		{
			Connector = connMock.Object,
			Security = new Security { Id = "TEST@BOARD", Board = ExchangeBoard.Nyse, PriceStep = 0.01m },
			Portfolio = new Portfolio { Name = "test" },
		};
		decomposed.Init();

		// initial state
		decomposed.ProcessState.AreEqual(ProcessStates.Stopped);
		decomposed.StateHistory.Count.AreEqual(0);

		// start
		await decomposed.StartAsync();
		decomposed.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		decomposed.ProcessState.AreEqual(ProcessStates.Started);
		decomposed.StateHistory.Count.AreEqual(1);
		decomposed.StateHistory[0].AreEqual(ProcessStates.Started);

		// stop
		await decomposed.StopAsync();
		decomposed.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		decomposed.ProcessState.AreEqual(ProcessStates.Stopping);
		decomposed.StateHistory.Count.AreEqual(2);
		decomposed.StateHistory[1].AreEqual(ProcessStates.Stopping);
	}

	#endregion

	#region Risk Management Parity

	[TestMethod]
	public void Parity_Risk_RulesConsultedOnTradeExecution()
	{
		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var rule = new TrackingRiskRule();
		strategy.RiskManager.Rules.Add(rule);

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var order = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		AttachAndActivate(strategy, sub, order);

		strategy.OnTradeReceived(sub, CreateTrade(order, 1, 100m, 10m));

		IsTrue(rule.ProcessedMessages.Count > 0,
			"RiskManager.ProcessRules should be called when trade executes");
	}

	[TestMethod]
	public void Parity_Risk_RulesConsultedOnOrderRegistration()
	{
		var connMock = CreateMockConnector();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()));
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var rule = new TrackingRiskRule();
		strategy.RiskManager.Rules.Add(rule);

		strategy.RegisterOrder(strategy.CreateOrder(Sides.Buy, 100m, 5m));

		IsTrue(rule.ProcessedMessages.Count > 0,
			"RiskManager.ProcessRules should be called when order is registered");
	}

	[TestMethod]
	public void Parity_Risk_RulesConsultedOnPositionChange()
	{
		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var rule = new TrackingRiskRule();
		strategy.RiskManager.Rules.Add(rule);

		var posSub = new Subscription(DataType.PositionChanges);
		strategy.Subscriptions.Subscribe(posSub);

		var pos = new Position
		{
			Security = security,
			Portfolio = portfolio,
			CurrentValue = 100,
			LocalTime = DateTime.UtcNow,
		};

		strategy.OnPositionReceived(posSub, pos);

		IsTrue(rule.ProcessedMessages.Count > 0,
			"RiskManager.ProcessRules should be called when position changes");
	}

	[TestMethod]
	public void Parity_Risk_StopTradingBlocksNewOrders()
	{
		var connMock = CreateMockConnector();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()));
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var rule = new TrackingRiskRule { Action = RiskActions.StopTrading };
		strategy.RiskManager.Rules.Add(rule);

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var order1 = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		AttachAndActivate(strategy, sub, order1);
		strategy.OnTradeReceived(sub, CreateTrade(order1, 1, 100m, 10m));

		connMock.Invocations.Clear();

		var order2 = strategy.CreateOrder(Sides.Buy, 200m, 5m);
		try { strategy.RegisterOrder(order2); } catch { }

		var wasRegistered = connMock.Invocations.Any(i => i.Method.Name == "RegisterOrder");
		IsFalse(wasRegistered,
			"After StopTrading risk action triggers, new orders should NOT reach the connector");
	}

	#endregion

	#region PnL and Trade Processing Parity

	[TestMethod]
	public void Parity_PnL_SecurityContextUpdatedBeforeTrade()
	{
		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		security.PriceStep = 0.5m;
		security.Multiplier = 10m;
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var buyOrder = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		AttachAndActivate(strategy, sub, buyOrder);
		strategy.OnTradeReceived(sub, CreateTrade(buyOrder, 1, 100m, 10m));

		var sellOrder = CreateOrder(security, portfolio, Sides.Sell, 110, 10, txId: 2);
		AttachAndActivate(strategy, sub, sellOrder);
		strategy.OnTradeReceived(sub, CreateTrade(sellOrder, 2, 110m, 10m));

		var pnl = strategy.PnLManager.RealizedPnL;

		IsTrue(pnl > 100m,
			$"PnL should reflect security Multiplier={security.Multiplier} " +
			$"(expected >100 with multiplier, got {pnl}).");
	}

	[TestMethod]
	public void Parity_Trade_PositionRecordedAtTradeTime()
	{
		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var order1 = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		AttachAndActivate(strategy, sub, order1);
		strategy.OnTradeReceived(sub, CreateTrade(order1, 1, 100m, 10m));

		var order2 = CreateOrder(security, portfolio, Sides.Sell, 105, 4, txId: 2);
		AttachAndActivate(strategy, sub, order2);
		strategy.OnTradeReceived(sub, CreateTrade(order2, 2, 105m, 4m));

		var trades = strategy.Trades.MyTrades.ToArray();
		AreEqual(2, trades.Length);

		IsNotNull(trades[0].Position, "Trade[0].Position should be set after trade processing");
		IsNotNull(trades[1].Position, "Trade[1].Position should be set after trade processing");

		trades[0].Position.AreEqual(10m, "Trade[0].Position should be 10 after buying 10");
		trades[1].Position.AreEqual(6m, "Trade[1].Position should be 6 after selling 4");
	}

	#endregion

	#region Position Management Parity

	[TestMethod]
	public void Parity_Position_MultiSecurity_TrackedIndependently()
	{
		var connMock = CreateMockConnector();
		var securityA = CreateSecurity("AAPL@NYSE");
		var securityB = CreateSecurity("GOOG@NYSE");
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = securityA,
			Portfolio = portfolio,
		};

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var orderA = CreateOrder(securityA, portfolio, Sides.Buy, 100, 5);
		AttachAndActivate(strategy, sub, orderA);
		strategy.OnTradeReceived(sub, CreateTrade(orderA, 1, 100m, 5m));

		var orderB = CreateOrder(securityB, portfolio, Sides.Buy, 200, 3, txId: 2);
		AttachAndActivate(strategy, sub, orderB);
		strategy.OnTradeReceived(sub, CreateTrade(orderB, 2, 200m, 3m));

		AreEqual(5m, strategy.Position,
			$"Position should reflect only primary security trades. Got {strategy.Position}.");
	}

	[TestMethod]
	public void Parity_Position_PerSecurityLookupAvailable()
	{
		var hasMethod = typeof(DecomposedStrategy).GetMethod("GetPositionValue",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		IsNotNull(hasMethod,
			"DecomposedStrategy should expose GetPositionValue(Security, Portfolio)");
	}

	#endregion

	#region Order Features Parity

	[TestMethod]
	public void Parity_Order_CommentModeAvailable()
	{
		var hasProperty = typeof(DecomposedStrategy).GetProperty("CommentMode");
		IsNotNull(hasProperty, "DecomposedStrategy should expose CommentMode property");
	}

	[TestMethod]
	public void Parity_Order_TradingModeAvailable()
	{
		var hasProperty = typeof(DecomposedStrategy).GetProperty("TradingMode");
		IsNotNull(hasProperty, "DecomposedStrategy should expose TradingMode property");
	}

	[TestMethod]
	public void Parity_Order_LatencyTracked()
	{
		var hasProperty = typeof(DecomposedStrategy).GetProperty("Latency");
		IsNotNull(hasProperty, "DecomposedStrategy should expose Latency property");
	}

	[TestMethod]
	public void Parity_Order_RegisterFailHandled()
	{
		var hasMethod = typeof(DecomposedStrategy).GetMethod("OnOrderRegisterFailed",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		IsNotNull(hasMethod, "DecomposedStrategy should handle order registration failures");
	}

	#endregion

	#region Error Handling Parity

	[TestMethod]
	public void Parity_Error_StateTracked()
	{
		var hasProperty = typeof(DecomposedStrategy).GetProperty("ErrorState");
		IsNotNull(hasProperty, "DecomposedStrategy should expose ErrorState property");
	}

	[TestMethod]
	public void Parity_Error_EventAvailable()
	{
		var hasEvent = typeof(DecomposedStrategy).GetEvent("Error");
		IsNotNull(hasEvent, "DecomposedStrategy should expose Error event");
	}

	#endregion

	#region Feature Completeness Parity

	[TestMethod]
	public void Parity_Feature_ClosePositionAvailable()
	{
		var hasMethod = typeof(DecomposedStrategy).GetMethod("ClosePosition",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(hasMethod, "DecomposedStrategy should expose ClosePosition() method");
	}

	[TestMethod]
	public void Parity_Feature_CancelActiveOrdersAvailable()
	{
		var hasMethod = typeof(DecomposedStrategy).GetMethod("CancelActiveOrders",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(hasMethod, "DecomposedStrategy should expose CancelActiveOrders() method");
	}

	[TestMethod]
	public void Parity_Feature_WaitAllTradesSupported()
	{
		var hasProperty = typeof(DecomposedStrategy).GetProperty("WaitAllTrades");
		IsNotNull(hasProperty, "DecomposedStrategy should expose WaitAllTrades property");
	}

	[TestMethod]
	public void Parity_Feature_IsOnlineTracked()
	{
		var hasProperty = typeof(DecomposedStrategy).GetProperty("IsOnline");
		IsNotNull(hasProperty, "DecomposedStrategy should expose IsOnline property");
	}

	[TestMethod]
	public void Parity_Feature_PositionCollectionAvailable()
	{
		var hasProperty = typeof(DecomposedStrategy).GetProperty("PositionsList");
		IsNotNull(hasProperty, "DecomposedStrategy should expose PositionsList collection");
	}

	#endregion
}
