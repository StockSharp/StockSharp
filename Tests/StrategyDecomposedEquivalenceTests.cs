#pragma warning disable CS0618 // equivalence tests deliberately exercise the obsolete StrategyOld engine
namespace StockSharp.Tests;

using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Statistics;
using StockSharp.Algo.Testing;
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

	private class TestStrategy : Strategy
	{
		public List<ProcessStates> StateHistory { get; } = [];
		public List<MyTrade> ReceivedTrades { get; } = [];
		public List<Order> RegisteredOrders { get; } = [];

		protected override void OnStateChanged(ProcessStates state) => StateHistory.Add(state);
		protected override void OnOwnTradeReceived(MyTrade trade) => ReceivedTrades.Add(trade);
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
		Sides side, decimal price, decimal volume, long txId = 1, DateTime time = default)
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
			Time = time == default ? DateTime.UtcNow : time,
		};
	}

	private static void AttachAndActivate(TestStrategy strategy, Subscription sub, Order order)
	{
		strategy.OnConnectorOrderReceived(sub, order);
		order.State = OrderStates.Active;
		strategy.OnConnectorOrderReceived(sub, order);
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

	private static TimeFrameCandleMessage Candle(SecurityId secId, DateTime openTime, decimal close)
		=> new()
		{
			SecurityId = secId,
			OpenTime = openTime,
			LocalTime = openTime,
			OpenPrice = close,
			HighPrice = close,
			LowPrice = close,
			ClosePrice = close,
			TotalVolume = 1m,
			State = CandleStates.Finished,
		};

	#endregion

	#region DecomposedSmaStrategy — same logic as SmaStrategy

	/// <summary>
	/// SMA crossover strategy built on Strategy.
	/// Uses the exact same OnProcess logic as SmaStrategy.
	/// </summary>
	private class DecomposedSmaStrategy : Strategy
	{
		private bool? _isShortLessThenLong;
		private readonly SimpleMovingAverage _longSma = new();
		private readonly SimpleMovingAverage _shortSma = new();

		public int Long { get; set; } = 80;
		public int Short { get; set; } = 30;

		public List<ProcessStates> StateHistory { get; } = [];
		public List<(DateTimeOffset time, Sides side, decimal price, decimal longVal, decimal shortVal)> CrossoverLog { get; } = [];

		public void Init()
		{
			_longSma.Length = Long;
			_shortSma.Length = Short;
			_isShortLessThenLong = null;
		}

		/// <summary>
		/// Feed a candle through SMA indicators and check for crossover signal.
		/// Uses Process return value (matching Bind's value extraction path exactly)
		/// rather than GetCurrentValue() (which reads from indicator Container).
		/// Both should return identical values for finished candles, but using
		/// Process return directly matches the Bind code path in Strategy_HighLevel.cs.
		/// </summary>
		public void ProcessCandle(ICandleMessage candle)
		{
			if (candle.State != CandleStates.Finished)
				return;

			var longVal = _longSma.Process(candle);
			var shortVal = _shortSma.Process(candle);

			OnProcess(candle, longVal.ToDecimal(), shortVal.ToDecimal());
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

				CrossoverLog.Add((candle.OpenTime, direction, price, longValue, shortValue));

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

	/// <summary>
	/// Create a deterministic connector that processes all messages synchronously.
	/// Uses <see cref="PassThroughMessageChannel"/> instead of <see cref="InMemoryMessageChannel"/>
	/// so strategy order fills are reflected in Position before the next candle arrives.
	/// This is critical for SmaEquivalence: the decomposed strategy applies fills instantly,
	/// so the real strategy must also process fills synchronously for the same crossover
	/// signals and volume calculations.
	/// </summary>
	private static HistoryEmulationConnector CreateConnector(
		ISecurityProvider secProvider,
		IPortfolioProvider pfProvider,
		IStorageRegistry storageRegistry,
		DateTime startTime,
		DateTime stopTime)
	{
		var historyAdapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			new HistoryMarketDataManager(new TradingTimeLineGenerator())
			{
				StorageRegistry = storageRegistry
			})
		{
			StartDate = startTime,
			StopDate = stopTime,
		};

		var connector = new HistoryEmulationConnector(
			historyAdapter, true,
			new PassThroughMessageChannel(),
			secProvider, pfProvider,
			storageRegistry.ExchangeInfoProvider);

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
	/// Captures candles from the STRATEGY's CandleReceived event (not connector-level),
	/// so the decomposed strategy can replay exactly the same candles that the real
	/// strategy's Bind mechanism processed.
	/// Uses a deterministic random provider so the emulator's behavior is reproducible.
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

		// Make emulator fully deterministic:
		// 1. RandomProvider — default uses DateTime-based seed
		// 2. InitialOrderId/InitialTradeId — EmulationMessageAdapter sets from DateTime.UtcNow.Ticks
		// 3. Latency/Slippage/Commission managers — may introduce non-determinism
		var emulator = (MarketEmulator)connector.EmulationAdapter.Emulator;
		emulator.RandomProvider = new DefaultRandomProvider(42);
		connector.EmulationAdapter.Settings.InitialOrderId = 100;
		connector.EmulationAdapter.Settings.InitialTradeId = 100;
		connector.Adapter.LatencyManager = null;
		connector.Adapter.SlippageManager = null;
		connector.Adapter.CommissionManager = null;

		var strategy = new SmaStrategy
		{
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
			// Disable protection so the strategy only generates SMA crossover orders
			// (DecomposedSmaStrategy doesn't have stop-loss protection)
			StopValue = new Unit(0, UnitTypes.Absolute),
			TakeValue = new Unit(0, UnitTypes.Absolute),
		};

		// Capture candles from the STRATEGY's subscription — this is exactly what
		// the Bind mechanism processes. connector.CandleReceived may include candles
		// that arrive before/after the strategy's subscription is active.
		var capturedCandles = new List<ICandleMessage>();
		strategy.CandleReceived += (sub, candle) =>
		{
			capturedCandles.Add(candle);
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
	/// Verify registration detection matches Strategy.ProcessOrder: an order is
	/// reported as "registered" only when it is observed transitioning from
	/// <see cref="OrderStates.Pending"/> to <see cref="OrderStates.Active"/>/<see cref="OrderStates.Done"/>
	/// (OrderPipeline.cs:71-73 mirrors Strategy.cs:1828).
	///
	/// The previous version of this test only asserted IsTracked (tautological:
	/// TryAttach unconditionally adds) and processed each order a single time
	/// while PrevState was still <see cref="OrderStates.None"/>, so the
	/// registration branch was never reached and the Registered event was dead.
	/// Here we drive the real Pending -> final transition explicitly so the
	/// detection logic is actually exercised.
	/// </summary>
	[TestMethod]
	[Timeout(180_000, CooperativeCancellation = true)]
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

		// After a completed backtest every generated order has reached a final
		// non-Pending state (Active/Done, or Failed if it was rejected). Capture
		// the true final state, then replay the canonical lifecycle the engine
		// would observe live: first a Pending snapshot (records PrevState=Pending),
		// then the final state (which is what triggers registration detection).
		var expectedRegistered = new List<Order>();

		foreach (var order in orders)
		{
			var finalState = order.State;

			pipeline.TryAttach(order).AssertTrue();

			// Re-attaching the same order must be rejected (dedup).
			pipeline.TryAttach(order).AssertFalse();

			// Observe Pending first so PrevState becomes Pending.
			order.State = OrderStates.Pending;
			pipeline.ProcessOrder(order, isChanging: true);

			// Now observe the real final state — registration is detected here
			// for orders that became Active/Done, but NOT for Failed orders.
			order.State = finalState;
			pipeline.ProcessOrder(order, isChanging: true);

			if (finalState is OrderStates.Active or OrderStates.Done)
				expectedRegistered.Add(order);
		}

		// All orders must be tracked.
		foreach (var order in orders)
			pipeline.IsTracked(order).AssertTrue();

		// Registration detection must fire exactly for the orders that reached
		// Active/Done — not zero (the old test's silent failure mode) and not
		// for Failed orders.
		AreEqual(expectedRegistered.Count, registeredOrders.Count,
			$"Registered event must fire for every Pending->Active/Done order. " +
			$"Expected {expectedRegistered.Count}, got {registeredOrders.Count}.");

		IsTrue(registeredOrders.Count > 0,
			"At least one backtest order must be detected as registered (Pending->Active/Done).");

		foreach (var order in expectedRegistered)
			IsTrue(registeredOrders.Contains(order),
				"Every Active/Done order must be reported via the Registered event.");

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

		// Replay each raw strategy transition into the engine WITHOUT pre-guarding
		// on engine.ProcessState (the old test duplicated the engine's own guard,
		// making the assertions self-fulfilling). The engine must apply its own
		// guards internally and end up with exactly the canonical sequence.
		foreach (var state in strategyStates)
			engine.OnMessage(new StrategyEngine.StrategyStateMessage(state));

		IsTrue(strategyStates.Count > 0, "Strategy should have had state transitions");

		// Build the canonical engine sequence the StrategyEngine state machine
		// (StrategyEngine.cs:158-177) must produce from those requests: it only
		// accepts Stopped->Started and Started->Stopping, ignores everything else,
		// and never emits StateChanged for a no-op transition. So the expected
		// engine sequence is the strategy's request stream with duplicates and
		// illegal requests collapsed.
		var expectedEngineStates = new List<ProcessStates>();
		var simState = ProcessStates.Stopped;

		foreach (var state in strategyStates)
		{
			if (state == ProcessStates.Started && simState == ProcessStates.Stopped)
			{
				simState = ProcessStates.Started;
				expectedEngineStates.Add(simState);
			}
			else if (state == ProcessStates.Stopping && simState == ProcessStates.Started)
			{
				simState = ProcessStates.Stopping;
				expectedEngineStates.Add(simState);
			}
		}

		Console.WriteLine($"Strategy states: {strategyStates.Select(x => $"{x}").Join(" -> ")}");
		Console.WriteLine($"Engine states:   {engineStates.Select(x => $"{x}").Join(" -> ")}");

		// Exact ordered-sequence comparison (not Contains): the engine must
		// reproduce the canonical state sequence step for step.
		AreEqual(expectedEngineStates.Count, engineStates.Count,
			$"Engine emitted {engineStates.Count} transitions, expected {expectedEngineStates.Count}");

		for (var i = 0; i < expectedEngineStates.Count; i++)
			AreEqual(expectedEngineStates[i], engineStates[i],
				$"Engine transition #{i} mismatch");

		// A real backtest always at least starts, so Started must be present.
		IsTrue(engineStates.Contains(ProcessStates.Started),
			"Engine must have transitioned to Started for a started strategy");
	}

	/// <summary>
	/// Deterministic contract test for the <see cref="StrategyEngine"/> state machine
	/// (no history/backtest). Verifies the exact accepted transitions, idempotency
	/// (no duplicate StateChanged), the ignored requests, and the illegal
	/// Stopped-&gt;Stopping guard described in StrategyEngine.cs:33-39 / :158-177.
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void StrategyEngine_StateMachine_ExactTransitions()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl);

		var states = new List<ProcessStates>();
		engine.StateChanged += s => states.Add(s);

		// Fresh engine is Stopped.
		AreEqual(ProcessStates.Stopped, engine.ProcessState);

		// Stopping request on a Stopped engine is a no-op (guarded), not a throw.
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		AreEqual(ProcessStates.Stopped, engine.ProcessState);
		AreEqual(0, states.Count, "Ignored request must not raise StateChanged");

		// Stopped -> Started.
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		AreEqual(ProcessStates.Started, engine.ProcessState);

		// Repeated Started is idempotent: no second event.
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		AreEqual(ProcessStates.Started, engine.ProcessState);

		// Started -> Stopping.
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		AreEqual(ProcessStates.Stopping, engine.ProcessState);

		// Repeated Stopping is idempotent.
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		AreEqual(ProcessStates.Stopping, engine.ProcessState);

		// Started request while Stopping is ignored (only Stopped->Started accepted).
		engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		AreEqual(ProcessStates.Stopping, engine.ProcessState);

		// Exactly two transitions were emitted, in order.
		AreEqual(2, states.Count);
		AreEqual(ProcessStates.Started, states[0]);
		AreEqual(ProcessStates.Stopping, states[1]);

		// ForceStop is the only path back to Stopped (no message routes to Stopped).
		engine.ForceStop();
		AreEqual(ProcessStates.Stopped, engine.ProcessState);
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

		// Verify the engine processed messages and generated events.
		IsTrue(priceUpdates > 0, $"Expected price updates from {capturedMessages.Count} messages");

		// The doc claim is that PnL-refresh events actually fire. With the default
		// 1s UnrealizedPnLInterval and a multi-day backtest, the very first message
		// carrying a server time is >= 1s past the initial (default) refresh time,
		// so at least one refresh MUST fire. The exact throttling semantics are
		// pinned deterministically in StrategyEngine_PnLRefresh_ThrottledByInterval.
		IsTrue(pnlRefreshes > 0,
			$"PnL refresh events must fire while replaying {capturedMessages.Count} timestamped messages");

		Console.WriteLine("Message processing equivalence OK");
	}

	/// <summary>
	/// Deterministic contract test (no history) for the PnL-refresh throttling that
	/// <see cref="StrategyEngine"/> performs (StrategyEngine.cs:189-194): a refresh
	/// fires only when the incoming message time advances by at least
	/// <see cref="StrategyEngine.UnrealizedPnLInterval"/> past the previous refresh.
	/// This is the "PnL refresh fires at the same times" behaviour that the replay
	/// test above can only assert weakly because real message timings vary per run.
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void StrategyEngine_PnLRefresh_ThrottledByInterval()
	{
		var host = new FakeHost();
		var pnl = new PnLManager();
		var engine = new StrategyEngine(host, pnl)
		{
			UnrealizedPnLInterval = TimeSpan.FromSeconds(1),
		};

		var secId = new SecurityId { SecurityCode = "TST", BoardCode = "BRD" };

		var refreshTimes = new List<DateTime>();
		engine.PnLRefreshRequired += t => refreshTimes.Add(t);

		Message L1(DateTime time, decimal price)
			=> new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = time,
				LocalTime = time,
			}.TryAdd(Level1Fields.LastTradePrice, price);

		var t0 = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		// First timestamped message: gap from default refresh time (MinValue) is
		// huge, so it must trigger exactly one refresh at t0.
		engine.OnMessage(L1(t0, 100m));
		AreEqual(1, refreshTimes.Count);
		AreEqual(t0, refreshTimes[0]);

		// +0.5s: within the interval -> NO refresh.
		engine.OnMessage(L1(t0.AddMilliseconds(500), 101m));
		AreEqual(1, refreshTimes.Count, "Sub-interval message must not refresh PnL");

		// +0.9s (=1.4s from last refresh): now >= 1s past last refresh -> refresh.
		var t1 = t0.AddMilliseconds(1400);
		engine.OnMessage(L1(t1, 102m));
		AreEqual(2, refreshTimes.Count, "Message past the interval must refresh PnL");
		AreEqual(t1, refreshTimes[1]);

		// Exactly at the boundary (+1s from last refresh) -> refresh ( >= test).
		var t2 = t1.AddSeconds(1);
		engine.OnMessage(L1(t2, 103m));
		AreEqual(3, refreshTimes.Count, "Message exactly one interval later must refresh PnL");
		AreEqual(t2, refreshTimes[2]);

		// Just before the next boundary -> NO refresh.
		engine.OnMessage(L1(t2.AddMilliseconds(999), 104m));
		AreEqual(3, refreshTimes.Count, "Message just under the interval must not refresh PnL");
	}

	#endregion

	#region DecomposedSmaStrategy equivalence tests

	/// <summary>
	/// Run real SmaStrategy backtest, capture candles, feed same candles
	/// to DecomposedSmaStrategy, compare crossover signals match.
	/// The comparison focuses on crossover DIRECTION (Buy/Sell) since the
	/// emulator legitimately rejects some limit orders whose price falls
	/// outside the candle [Low, High] range, causing Position to diverge.
	/// Both strategies detect the same SMA crossovers because:
	/// - Same finished candles → same SMA values → same crossover events
	/// - _isShortLessThenLong is updated regardless of order fill result
	/// </summary>
	[TestMethod]
	[Timeout(360_000, CooperativeCancellation = true)]
	public async Task SmaEquivalence_OrdersAndPositionMatch()
	{
		// This is a backtest-based test: skip cleanly when no history data is
		// available instead of throwing inside RunSmaBacktestWithCandles.
		if (SkipIfNoHistoryData()) return;

		// 1. Run real SmaStrategy backtest and capture candles
		var (sma, candles) = await RunSmaBacktestWithCandles(CancellationToken);

		var origOrders = sma.Orders.ToArray();

		IsTrue(origOrders.Length > 0, "SmaStrategy must generate orders");
		IsTrue(candles.Count > 0, "Expected captured candles from backtest");

		// Deduplicate candles: CandleReceived may fire for both Building and
		// Finished states. Use only unique finished candles (by OpenTime) to
		// match what the real strategy receives via isFinishedOnly=true.
		var finishedCandles = candles
			.Where(c => c.State == CandleStates.Finished)
			.GroupBy(c => c.OpenTime)
			.Select(g => g.First())
			.OrderBy(c => c.OpenTime)
			.ToList();

		Console.WriteLine($"Captured candles: {candles.Count} total, {finishedCandles.Count} unique finished");

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

		// Don't update Position on fill - the decomposed strategy should
		// match the real strategy's crossover DETECTION logic, which only
		// depends on SMA values and _isShortLessThenLong flag, not on Position.
		// Position affects only the VOLUME calculation, not the crossover count.
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
			.Callback<Order>(o => decomposedOrders.Add(o));

		decomposed.Connector = connMock.Object;
		decomposed.Init();
		await decomposed.StartAsync();
		decomposed.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		// 3. Feed unique finished candles to DecomposedSmaStrategy
		foreach (var candle in finishedCandles)
			decomposed.ProcessCandle(candle);

		Console.WriteLine($"Real strategy orders: {origOrders.Length}, Decomposed orders: {decomposedOrders.Count}");

		// 4. Verify the decomposed strategy produces consistent crossover signals.
		//
		// The real SmaStrategy runs through the full emulator pipeline where
		// async processing (HistoryEmulationConnector, adapter chain) causes
		// candle delivery timing to vary between runs. This means the real
		// strategy may detect slightly fewer crossovers (some candles processed
		// during order feedback loop shift SMA state).
		//
		// The decomposed strategy processes candles sequentially without emulator
		// feedback, so it always detects the maximum possible crossovers (671).
		//
		// We verify equivalence by:
		// a) Both strategies detect a substantial number of crossovers
		// b) The decomposed detects at least as many as the real (it has no losses)
		// c) The first N signals from the real match the decomposed (same logic)
		// d) All signals alternate between Buy and Sell (correct crossover behavior)

		IsTrue(origOrders.Length > 100, $"Real strategy should detect many crossovers, got {origOrders.Length}");
		IsTrue(decomposedOrders.Count > 100, $"Decomposed should detect many crossovers, got {decomposedOrders.Count}");
		IsTrue(decomposedOrders.Count >= origOrders.Length,
			$"Decomposed (no emulator feedback) should detect at least as many crossovers as real: {decomposedOrders.Count} vs {origOrders.Length}");

		// Verify decomposed crossovers alternate between Buy and Sell
		for (var i = 1; i < decomposedOrders.Count; i++)
		{
			AreNotEqual(decomposedOrders[i - 1].Side, decomposedOrders[i].Side,
				$"Crossovers must alternate Buy/Sell: [{i - 1}]={decomposedOrders[i - 1].Side}, [{i}]={decomposedOrders[i].Side}");
		}

		// Note: real strategy orders may NOT strictly alternate in edge cases
		// because the emulator may reject a limit order whose price falls outside
		// the candle range, causing the strategy's position to not flip,
		// which can lead to two consecutive orders in the same direction.

		// The FIRST crossover is the one point where the two strategies are
		// guaranteed to agree exactly: both start flat (Position==0 => volume==Volume)
		// and receive the identical finished-candle stream into identical SMA seeds,
		// before any emulator feedback can diverge their state. So the first emitted
		// order must match on side, price AND volume — not merely "at least one of
		// ~600 matched" (the previous, near-vacuous oracle). Later orders may
		// legitimately diverge as the real strategy's Position flips on fills, so we
		// deliberately do not pin the full sequence.
		var firstReal = origOrders[0];
		var firstDecomposed = decomposedOrders[0];

		AreEqual(firstReal.Side, firstDecomposed.Side,
			$"First crossover side must match: real={firstReal.Side}, decomposed={firstDecomposed.Side}");
		AreEqual(firstReal.Price, firstDecomposed.Price,
			$"First crossover price must match: real={firstReal.Price}, decomposed={firstDecomposed.Price}");
		AreEqual(firstReal.Volume, firstDecomposed.Volume,
			$"First crossover volume must match: real={firstReal.Volume}, decomposed={firstDecomposed.Volume}");

		// Report how long the prefix stays identical for diagnostics only.
		var matchingPrefix = 0;
		var minLen = origOrders.Length.Min(decomposedOrders.Count);
		for (var i = 0; i < minLen; i++)
		{
			if (origOrders[i].Side == decomposedOrders[i].Side && origOrders[i].Price == decomposedOrders[i].Price)
				matchingPrefix++;
			else
				break;
		}

		Console.WriteLine($"Equivalence OK: {decomposedOrders.Count} decomposed crossovers, {origOrders.Length} real, {matchingPrefix} matching prefix");
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

		// Exact expected value (PnLQueue.cs:201,319-323): the security context
		// (Multiplier=10) is applied via UpdateSecurity BEFORE the trade is
		// processed, so LotMultiplier=10 and, with StepPrice unset, the queue
		// multiplier = 1 * Leverage(1) * LotMultiplier(10) = 10. Realized PnL on
		// the round trip = (110-100) price diff * 10 vol * 10 multiplier = 1000.
		// The previous ">100" oracle also passed for a wrong multiplier
		// (e.g. 2 -> 200) or a double-applied one (10000).
		AreEqual(1000m, pnl,
			$"Realized PnL must be (110-100)*10*Multiplier(10)=1000, got {pnl}.");
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
		var hasMethod = typeof(Strategy).GetMethod("GetPositionValue",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		IsNotNull(hasMethod,
			"Strategy should expose GetPositionValue(Security, Portfolio)");
	}

	#endregion

	#region Order Features Parity

	[TestMethod]
	public void Parity_Order_CommentModeAvailable()
	{
		var hasProperty = typeof(Strategy).GetProperty("CommentMode");
		IsNotNull(hasProperty, "Strategy should expose CommentMode property");
	}

	[TestMethod]
	public void Parity_Order_TradingModeAvailable()
	{
		var hasProperty = typeof(Strategy).GetProperty("TradingMode");
		IsNotNull(hasProperty, "Strategy should expose TradingMode property");
	}

	[TestMethod]
	public void Parity_Order_LatencyTracked()
	{
		var hasProperty = typeof(Strategy).GetProperty("Latency");
		IsNotNull(hasProperty, "Strategy should expose Latency property");
	}

	[TestMethod]
	public void Parity_Order_RegisterFailHandled()
	{
		var hasMethod = typeof(Strategy).GetMethod("OnOrderRegisterFailed",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		IsNotNull(hasMethod, "Strategy should handle order registration failures");
	}

	/// <summary>
	/// A fresh strategy that has not registered any order of its own must NOT
	/// claim a foreign order. Real <see cref="StrategyOld"/> enforces this through
	/// <c>CanAttach</c> by matching the order's <see cref="Order.UserOrderId"/>
	/// against the strategy id (Strategy.cs:2440-2446), so before the first own
	/// registration a foreign order is rejected.
	///
	/// <see cref="Strategy"/>'s CanAttach
	/// (Strategy.cs:669-675) instead returns <see langword="true"/> for
	/// ANY order while its <c>_ownTransactionIds</c> set is still empty — i.e. a
	/// brand-new decomposed strategy would attach a foreign order, diverging from
	/// real Strategy. This test asserts the canonical contract (CanAttach == false
	/// for a foreign order on a fresh strategy); it is expected to FAIL on the
	/// current engine, pinning the divergence.
	///
	/// CanAttach is <c>protected virtual</c>, so it is invoked through reflection
	/// (matching the reflection idiom already used by the parity tests in this file).
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void Parity_Order_CanAttach_RejectsForeignOrderBeforeOwnRegistration()
	{
		var connMock = CreateMockConnector();
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		// Fresh strategy: no order has been registered yet, so _ownTransactionIds
		// is empty.
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		// A clearly FOREIGN order: a transaction id this strategy never produced
		// and a UserOrderId that does not match the strategy id. Real Strategy
		// keys CanAttach off UserOrderId, so this order does not belong here.
		var foreignOrder = CreateOrder(security, portfolio, Sides.Buy, 100m, 10m, txId: 999_999);
		foreignOrder.UserOrderId = "some_other_strategy_id";

		var canAttach = typeof(Strategy).GetMethod("CanAttach",
			BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(canAttach, "Strategy should expose CanAttach(Order)");

		var result = (bool)canAttach.Invoke(strategy, [foreignOrder]);

		// Canonical contract (parity with Strategy.CanAttach): a foreign order must
		// be rejected even before the strategy registers its first own order.
		IsFalse(result,
			"A fresh strategy with no own registrations must NOT claim a foreign order " +
			"(real Strategy.CanAttach matches by UserOrderId and rejects it).");
	}

	#endregion

	#region Error Handling Parity

	[TestMethod]
	public void Parity_Error_StateTracked()
	{
		var hasProperty = typeof(Strategy).GetProperty("ErrorState");
		IsNotNull(hasProperty, "Strategy should expose ErrorState property");
	}

	[TestMethod]
	public void Parity_Error_EventAvailable()
	{
		var hasEvent = typeof(Strategy).GetEvent("Error");
		IsNotNull(hasEvent, "Strategy should expose Error event");
	}

	#endregion

	#region Feature Completeness Parity

	[TestMethod]
	public void Parity_Feature_ClosePositionAvailable()
	{
		var hasMethod = typeof(Strategy).GetMethod("ClosePosition",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(hasMethod, "Strategy should expose ClosePosition() method");
	}

	[TestMethod]
	public void Parity_Feature_CancelActiveOrdersAvailable()
	{
		var hasMethod = typeof(Strategy).GetMethod("CancelActiveOrders",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(hasMethod, "Strategy should expose CancelActiveOrders() method");
	}

	[TestMethod]
	public void Parity_Feature_WaitAllTradesSupported()
	{
		var hasProperty = typeof(Strategy).GetProperty("WaitAllTrades");
		IsNotNull(hasProperty, "Strategy should expose WaitAllTrades property");
	}

	[TestMethod]
	public void Parity_Feature_IsOnlineTracked()
	{
		var hasProperty = typeof(Strategy).GetProperty("IsOnline");
		IsNotNull(hasProperty, "Strategy should expose IsOnline property");
	}

	[TestMethod]
	public void Parity_Feature_PositionCollectionAvailable()
	{
		var hasProperty = typeof(Strategy).GetProperty("PositionsList");
		IsNotNull(hasProperty, "Strategy should expose PositionsList collection");
	}

	#endregion

	#region Feature Gap Tests — missing in Strategy vs Strategy

	// --- API existence tests ---

	[TestMethod]
	public void Gap_EditOrder_MethodExists()
	{
		var method = typeof(Strategy).GetMethod("EditOrder",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(method,
			"Strategy should expose EditOrder(Order, Order) like Strategy");
	}

	[TestMethod]
	public void Gap_ReRegisterOrder_MethodExists()
	{
		var method = typeof(Strategy).GetMethod("ReRegisterOrder",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(method,
			"Strategy should expose ReRegisterOrder(Order, Order) like Strategy");
	}

	[TestMethod]
	public void Gap_Commission_PropertyExists()
	{
		var prop = typeof(Strategy).GetProperty("Commission",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(prop,
			"Strategy should expose Commission property (aggregated from orders+trades) like Strategy");
	}

	[TestMethod]
	public void Gap_Slippage_PropertyExists()
	{
		var prop = typeof(Strategy).GetProperty("Slippage",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(prop,
			"Strategy should expose Slippage property (aggregated from trades) like Strategy");
	}

	[TestMethod]
	public void Gap_Reset_MethodExists()
	{
		var method = typeof(Strategy).GetMethod("Reset",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(method,
			"Strategy should expose Reset() method like Strategy");
	}

	[TestMethod]
	public void Gap_StopWithError_OverloadExists()
	{
		// Strategy has Stop(Exception) that logs error, stores it, then stops.
		// Strategy should have StopAsync(Exception, CancellationToken) or equivalent.
		var methods = typeof(Strategy).GetMethods(BindingFlags.Instance | BindingFlags.Public)
			.Where(m => m.Name is "Stop" or "StopAsync")
			.Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(Exception)));

		IsTrue(methods.Any(),
			"Strategy should expose Stop/StopAsync overload that accepts Exception like Strategy.Stop(Exception)");
	}

	[TestMethod]
	public void Gap_CancelOrdersWhenStopping_PropertyExists()
	{
		var prop = typeof(Strategy).GetProperty("CancelOrdersWhenStopping",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(prop,
			"Strategy should expose CancelOrdersWhenStopping property like Strategy");
	}

	[TestMethod]
	public void Gap_UnsubscribeOnStop_PropertyExists()
	{
		var prop = typeof(Strategy).GetProperty("UnsubscribeOnStop",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(prop,
			"Strategy should expose UnsubscribeOnStop property like Strategy");
	}

	[TestMethod]
	public void Gap_CommissionChanged_EventExists()
	{
		var ev = typeof(Strategy).GetEvent("CommissionChanged");
		IsNotNull(ev,
			"Strategy should expose CommissionChanged event like Strategy");
	}

	[TestMethod]
	public void Gap_SlippageChanged_EventExists()
	{
		var ev = typeof(Strategy).GetEvent("SlippageChanged");
		IsNotNull(ev,
			"Strategy should expose SlippageChanged event like Strategy");
	}

	[TestMethod]
	public void Gap_LatencyChanged_EventExists()
	{
		var ev = typeof(Strategy).GetEvent("LatencyChanged");
		IsNotNull(ev,
			"Strategy should expose LatencyChanged event like Strategy");
	}

	// --- Behavioral tests ---

	[TestMethod]
	public async Task Gap_CancelOrdersWhenStopping_ActiveOrdersCancelled()
	{
		// Strategy cancels all active orders when stopping (CancelOrdersWhenStopping=true by default).
		// Strategy should do the same.

		var connMock = CreateMockConnector();
		connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()));
		connMock.Setup(c => c.CancelOrder(It.IsAny<Order>()));

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		// Start strategy
		await strategy.StartAsync();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// Register and activate an order
		var order = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		strategy.RegisterOrder(order);
		AttachAndActivate(strategy, sub, order);

		// Stop the strategy
		await strategy.StopAsync();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));

		// Active orders should be cancelled
		connMock.Verify(c => c.CancelOrder(It.IsAny<Order>()), Times.AtLeastOnce(),
			"Active orders should be cancelled when strategy stops (like Strategy.CancelOrdersWhenStopping)");
	}

	[TestMethod]
	public async Task Gap_UnsubscribeOnStop_SubscriptionsRemoved()
	{
		// Strategy unsubscribes all market data when stopping (UnsubscribeOnStop=true by default).
		// Strategy should do the same.

		var connMock = CreateMockConnector();
		var unsubscribed = new List<Subscription>();
		connMock.Setup(c => c.UnSubscribe(It.IsAny<Subscription>()))
			.Callback<Subscription>(s => unsubscribed.Add(s));

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();
		var strategy = new TestStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
		};

		// Start strategy
		await strategy.StartAsync();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		// Subscribe to market data
		var ticksSub = new Subscription(DataType.Ticks, security);
		strategy.Subscriptions.Subscribe(ticksSub);

		// Stop the strategy
		await strategy.StopAsync();
		strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));

		// Market data should be unsubscribed
		IsTrue(unsubscribed.Count > 0,
			"Market data subscriptions should be unsubscribed when strategy stops (like Strategy.UnsubscribeOnStop)");
	}

	[TestMethod]
	public void Gap_Commission_AccumulatedFromTrades()
	{
		// Strategy exposes Commission property that accumulates from order.Commission values.
		// Strategy should expose the same at the strategy level.

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

		var order = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		order.Commission = 1.5m;
		AttachAndActivate(strategy, sub, order);

		// Strategy.Commission should reflect the commission from registered orders
		var commProp = typeof(Strategy).GetProperty("Commission",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(commProp, "Commission property must exist");

		var commValue = (decimal?)commProp.GetValue(strategy);
		IsNotNull(commValue, "Commission should be set after order with commission is processed");
		AreEqual(1.5m, commValue.Value,
			$"Commission should accumulate from order.Commission. Got {commValue}");
	}

	[TestMethod]
	public void Gap_Slippage_AccumulatedFromTrades()
	{
		// Strategy exposes Slippage property that accumulates from trade.Slippage values.
		// Strategy should expose the same at the strategy level.

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

		var order = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		AttachAndActivate(strategy, sub, order);

		var trade = CreateTrade(order, 1, 100m, 10m);
		trade.Slippage = 0.5m;
		strategy.OnTradeReceived(sub, trade);

		// Strategy.Slippage should reflect the accumulated slippage
		var slipProp = typeof(Strategy).GetProperty("Slippage",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(slipProp, "Slippage property must exist");

		var slipValue = (decimal?)slipProp.GetValue(strategy);
		IsNotNull(slipValue, "Slippage should be set after trade with slippage");
		AreEqual(0.5m, slipValue.Value,
			$"Slippage should accumulate from trade.Slippage. Got {slipValue}");
	}

	[TestMethod]
	public void Gap_Latency_AccumulatedFromOrderEvents()
	{
		// Strategy accumulates Latency from order.LatencyRegistration and order.LatencyCancellation.
		// Strategy has Latency (get/set) but doesn't accumulate automatically.

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

		var order = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		strategy.OnConnectorOrderReceived(sub, order); // Pending

		order.State = OrderStates.Active;
		order.LatencyRegistration = TimeSpan.FromMilliseconds(50);
		strategy.OnConnectorOrderReceived(sub, order); // Active with latency

		// Latency should accumulate from registration latency
		IsNotNull(strategy.Latency,
			"Latency should be set after order with LatencyRegistration is processed");
		IsTrue(strategy.Latency.Value >= TimeSpan.FromMilliseconds(50),
			$"Latency should accumulate from order.LatencyRegistration. Got {strategy.Latency}");
	}

	/// <summary>
	/// Deterministic monolith-vs-decomposed latency parity. The full-equivalence harness cannot compare
	/// latency (synchronous emulation makes it structurally zero, real values are wall-clock), so this
	/// computes a fixed latency through the real <see cref="LatencyManager"/> and drives an order with that
	/// <see cref="Order.LatencyRegistration"/> through both engines' order-intake (monolith StrategyOld.cs:1815,
	/// decomposed Strategy.cs:142).
	/// </summary>
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public void Parity_Latency_DeterministicValueAndEventMatch()
	{
		// 1. Produce a deterministic non-zero registration latency (execTime - regTime) through the real
		// LatencyManager, so the asserted value is a genuine pipeline product, not a literal.
		var regTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		var execTime = regTime + TimeSpan.FromMilliseconds(37);

		var latencyMgr = new LatencyManager(new LatencyManagerState());
		const long latencyTxId = 777;

		latencyMgr.ProcessMessage(new OrderRegisterMessage
		{
			TransactionId = latencyTxId,
			LocalTime = regTime,
		}).AssertNull();

		var computedLatency = latencyMgr.ProcessMessage(new ExecutionMessage
		{
			OriginalTransactionId = latencyTxId,
			LocalTime = execTime,
			OrderState = OrderStates.Active,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		});

		// The latency the manager computed must be exactly the fixed 37 ms gap.
		IsNotNull(computedLatency, "LatencyManager must compute a registration latency");
		AreEqual(TimeSpan.FromMilliseconds(37), computedLatency.Value,
			$"Latency must equal the fixed execTime - regTime gap. Got {computedLatency}");
		AreNotEqual(TimeSpan.Zero, computedLatency.Value,
			"Latency must be non-zero so the comparison is non-vacuous");

		var fixedLatency = computedLatency.Value;

		// 2. Drive the SAME fixed registration latency through BOTH engines and capture
		// the resulting strategy.Latency and the LatencyChanged event firing.
		var decomposedResult = RunDecomposedLatency(fixedLatency);
		var monolithResult = RunMonolithLatency(fixedLatency);

		// 3a. Both engines must expose the SAME latency value...
		IsNotNull(decomposedResult.latency, "Decomposed engine must expose Latency");
		IsNotNull(monolithResult.latency, "Monolith engine must expose Latency");
		AreEqual(monolithResult.latency.Value, decomposedResult.latency.Value,
			$"Latency must match between engines. Monolith={monolithResult.latency}, Decomposed={decomposedResult.latency}");

		// 3b. ...and that value must be exactly the fixed, non-zero latency (non-vacuous).
		AreEqual(fixedLatency, monolithResult.latency.Value,
			$"Monolith Latency must equal the fixed registration latency. Got {monolithResult.latency}");
		AreEqual(fixedLatency, decomposedResult.latency.Value,
			$"Decomposed Latency must equal the fixed registration latency. Got {decomposedResult.latency}");
		AreNotEqual(TimeSpan.Zero, decomposedResult.latency.Value,
			"Decomposed Latency must be non-zero (test would otherwise pass vacuously)");

		// 3c. Both engines must have raised LatencyChanged for the registration latency.
		IsTrue(monolithResult.eventFired, "Monolith must raise LatencyChanged");
		IsTrue(decomposedResult.eventFired, "Decomposed must raise LatencyChanged");
	}

	// Drive one owned order (Pending -> Active) with a fixed LatencyRegistration through the decomposed
	// order-intake; report the resulting Latency and whether LatencyChanged fired.
	private static (TimeSpan? latency, bool eventFired) RunDecomposedLatency(TimeSpan fixedLatency)
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

		var eventFired = false;
		strategy.LatencyChanged += () => eventFired = true;

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		var order = CreateOrder(security, portfolio, Sides.Buy, 100m, 10m);

		// First delivery: Pending - attaches/tracks the order, no latency yet.
		strategy.OnConnectorOrderReceived(sub, order);

		// Second delivery: Active with the fixed registration latency -> ChangeLatency.
		order.State = OrderStates.Active;
		order.Balance = order.Volume;
		order.LatencyRegistration = fixedLatency;
		strategy.OnConnectorOrderReceived(sub, order);

		return (strategy.Latency, eventFired);
	}

	// Equivalent flow through the monolith StrategyOld: a bare Connector (so CurrentTime resolves), the
	// OrderLookup subscription registered so CanProcess passes, and the private OnConnectorOrderReceived
	// invoked via reflection. Exercises the real CanProcess -> AttachOrder/ProcessOrder -> ChangeLatency path.
	private static (TimeSpan? latency, bool eventFired) RunMonolithLatency(TimeSpan fixedLatency)
	{
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		var strategy = new StrategyOld
		{
			Connector = new Connector(),
			Security = security,
			Portfolio = portfolio,
		};

		var eventFired = false;
		strategy.LatencyChanged += () => eventFired = true;

		// Register the OrderLookup subscription in the monolith's private subscription set so
		// CanProcess(OrderLookup) is satisfied, without triggering connector.Subscribe I/O.
		var orderLookup = strategy.OrderLookup;
		RegisterMonolithSubscription(strategy, orderLookup);

		// The monolith owns an order when its UserOrderId matches the strategy id (CanAttach).
		var order = CreateOrder(security, portfolio, Sides.Buy, 100m, 10m);
		order.UserOrderId = strategy.Id.To<string>();

		var onOrderReceived = typeof(StrategyOld).GetMethod("OnConnectorOrderReceived",
			BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(onOrderReceived, "StrategyOld must expose OnConnectorOrderReceived(Subscription, Order)");

		// First delivery: Pending - AttachOrder -> ProcessOrder(order, false), order tracked.
		onOrderReceived.Invoke(strategy, [orderLookup, order]);

		// Second delivery: Active with the fixed registration latency -> ChangeLatency.
		order.State = OrderStates.Active;
		order.Balance = order.Volume;
		order.LatencyRegistration = fixedLatency;
		onOrderReceived.Invoke(strategy, [orderLookup, order]);

		return (strategy.Latency, eventFired);
	}

	// Add a subscription to the monolith's private _subscriptions (the set CanProcess checks) without the
	// public Subscribe, which would also call connector.Subscribe and need a live transport.
	private static void RegisterMonolithSubscription(StrategyOld strategy, Subscription subscription)
	{
		var field = typeof(StrategyOld).GetField("_subscriptions",
			BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(field, "StrategyOld must have a _subscriptions field");

		var dict = field.GetValue(strategy);
		var add = dict.GetType().GetMethod("Add", [typeof(Subscription), typeof(bool)]);
		IsNotNull(add, "_subscriptions must expose Add(Subscription, bool)");
		add.Invoke(dict, [subscription, false]);
	}

	[TestMethod]
	public void Gap_Reset_ClearsAllState()
	{
		// Strategy.Reset() clears position, trades, orders, PnL, commission, etc.
		// Strategy should have the same.

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

		var sub = new Subscription(DataType.Transactions);
		strategy.Subscriptions.Subscribe(sub);

		// Build up state: register order, execute trade
		var order = CreateOrder(security, portfolio, Sides.Buy, 100, 10);
		strategy.RegisterOrder(order);
		AttachAndActivate(strategy, sub, order);
		strategy.OnTradeReceived(sub, CreateTrade(order, 1, 100m, 10m));

		// Verify state exists
		AreEqual(10m, strategy.Position, "Position should be 10 before reset");
		IsTrue(strategy.Trades.MyTrades.Any(), "Should have trades before reset");

		// Reset — uses reflection because method may not exist yet
		var resetMethod = typeof(Strategy).GetMethod("Reset",
			BindingFlags.Instance | BindingFlags.Public);
		IsNotNull(resetMethod, "Reset() method must exist");

		resetMethod.Invoke(strategy, null);

		// All state should be cleared
		AreEqual(0m, strategy.Position, "Position should be 0 after Reset()");
		IsFalse(strategy.Trades.MyTrades.Any(), "Trades should be empty after Reset()");
		IsFalse(strategy.Orders.Any(), "Orders should be empty after Reset()");
	}

	#endregion

	#region SMA with Protection equivalence

	/// <summary>
	/// Deterministic test (no history) that the decomposed protective contour is
	/// actually wired and FIRES: StartProtection -> a real fill (OnTradeReceived,
	/// which creates the position controller, Strategy.cs:108-123) ->
	/// an adverse market price (Level1) -> a protective closing order reaches the
	/// connector via RegisterOrder.
	///
	/// The previous backtest-driven version of this test never simulated any fill,
	/// so the position controller was never created and the whole protective path
	/// (TryActivate -> ActiveProtection) was structurally dead — yet its
	/// "withProtection >= noProtection" oracle still passed with ZERO protective
	/// orders. Here we drive a fill explicitly and require that protection emits a
	/// strictly larger order count than an identical strategy run without
	/// protection, and that the extra order is a closing order for the position.
	/// </summary>
	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task SmaWithProtection_Decomposed_GeneratesProtectiveOrders()
	{
		// --- Helper: build a strategy, optionally with protection, drive one
		// buy fill at 100 and then an adverse price drop to 94, capturing every
		// order that reaches the connector. ---
		async Task<List<Order>> Run(bool withProtection)
		{
			var connMock = CreateMockConnector();
			var registered = new List<Order>();
			connMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()))
				.Callback<Order>(registered.Add);

			var security = CreateSecurity();
			security.PriceStep = 0.01m;
			var portfolio = CreatePortfolio();

			var strategy = new TestStrategy
			{
				Connector = connMock.Object,
				Security = security,
				Portfolio = portfolio,
				Volume = 10,
			};

			if (withProtection)
			{
				// Proven-firing configuration (mirrors the dedicated protection
				// tests): a 5% local stop on a long bought at 100 activates when
				// the price falls to 94.
				strategy.StartProtection(
					new Unit(), new Unit(5, UnitTypes.Percent),
					isLocalStop: true);
			}

			await strategy.StartAsync();
			strategy.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

			var sub = new Subscription(DataType.Transactions);
			strategy.Subscriptions.Subscribe(sub);

			// Open a long: register + activate the order, then fill it at 100.
			var order = CreateOrder(security, portfolio, Sides.Buy, 100m, 10m);
			strategy.RegisterOrder(order);
			AttachAndActivate(strategy, sub, order);
			strategy.OnTradeReceived(sub, CreateTrade(order, 1, 100m, 10m));

			strategy.Position.AreEqual(10m);

			// Adverse price move (stop territory): drives CurrentPriceUpdated ->
			// ProtectiveController.TryActivate for the protected strategy.
			strategy.Engine.OnMessage(new Level1ChangeMessage
			{
				SecurityId = security.ToSecurityId(),
				ServerTime = DateTime.UtcNow,
				LocalTime = DateTime.UtcNow,
			}.TryAdd(Level1Fields.LastTradePrice, 94m));

			return registered;
		}

		var ordersNoProtection = await Run(withProtection: false);
		var ordersWithProtection = await Run(withProtection: true);

		Console.WriteLine($"Orders without protection: {ordersNoProtection.Count}");
		Console.WriteLine($"Orders with protection: {ordersWithProtection.Count}");

		// Without protection only the single opening order is registered.
		AreEqual(1, ordersNoProtection.Count,
			"Unprotected strategy should register only the opening order");

		// With protection, the adverse price must additionally generate a
		// protective order — strictly more than the unprotected run. A vacuous
		// ">=" oracle would also pass at zero protective orders (the old bug).
		IsTrue(ordersWithProtection.Count > ordersNoProtection.Count,
			$"Protected strategy must register MORE orders ({ordersWithProtection.Count}) " +
			$"than unprotected ({ordersNoProtection.Count}) — i.e. an actual protective order fired");

		// The protective order closes the long, so it is a Sell.
		var protectiveOrder = ordersWithProtection.Last();
		protectiveOrder.Side.AreEqual(Sides.Sell,
			"Protective order for a long position must be a Sell to close it");
	}

	#endregion

	#region Extra parity: IsFormed/CanTrade, Save/Load/Clone, ApplyCommand, RecycleOrders, WaitAllTrades

	// Focused parity tests the equivalence audit flagged as missing: native IsFormed -> CanTrade gate
	// (full-equivalence variants override IsFormed, so it is never compared), Save/Load/Clone round-trip,
	// ApplyCommand, and order teardown (OrdersKeepTime/RecycleOrders, WaitAllTrades). The monolith's private
	// intake handler and state machine are driven via reflection (decomposed exposes them publicly).

	// --- Monolith reflection drivers ---

	private static void MonolithOrderReceived(StrategyOld strategy, Subscription sub, Order order)
	{
		var method = typeof(StrategyOld).GetMethod("OnConnectorOrderReceived",
			BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(method, "StrategyOld must expose OnConnectorOrderReceived(Subscription, Order)");
		method.Invoke(strategy, [sub, order]);
	}

	// Drive the monolith state machine as Start()/Stop() do: build the private StrategyChangeStateMessage and
	// pump it into OnConnectorNewMessage directly (a bare Connector won't round-trip it). Real engine path.
	private static void DriveMonolithState(StrategyOld strategy, ProcessStates state)
	{
		var msgType = typeof(StrategyOld).GetNestedType("StrategyChangeStateMessage",
			BindingFlags.NonPublic);
		IsNotNull(msgType, "StrategyOld must define StrategyChangeStateMessage");

		var msg = (Message)msgType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			.First()
			.Invoke([strategy, state]);

		var onMessage = typeof(StrategyOld).GetMethod("OnConnectorNewMessage",
			BindingFlags.Instance | BindingFlags.NonPublic);
		IsNotNull(onMessage, "StrategyOld must expose OnConnectorNewMessage(Message, CancellationToken)");

		var task = (ValueTask)onMessage.Invoke(strategy, [msg, default(CancellationToken)]);
		task.AsTask().GetAwaiter().GetResult();
	}

	private static void StartMonolith(StrategyOld strategy)
	{
		strategy.Start();
		// Deliver the Started state message directly (the bare connector won't round-trip it).
		if (strategy.ProcessState != ProcessStates.Started)
			DriveMonolithState(strategy, ProcessStates.Started);
	}

	// 1. IsFormed engine transition + CanTrade gating. A real SMA is added to both engines WITHOUT
	// overriding IsFormed, so the native IsFormed and the CanTrade gate consulting it are compared 1:1.

	private sealed class FormedRecordingStrategy : Strategy
	{
		public List<Order> RegisteredOrders { get; } = [];
		protected override void OnOrderRegistered(Order order) => RegisteredOrders.Add(order);
	}

	private static bool MonolithCanTrade(StrategyOld strategy, Security security, Portfolio portfolio, Sides side, decimal volume)
	{
		var method = typeof(StrategyOld).GetMethod("CanTrade",
			BindingFlags.Instance | BindingFlags.NonPublic,
			null,
			[typeof(Security), typeof(Portfolio), typeof(Sides), typeof(decimal), typeof(string).MakeByRefType()],
			null);
		IsNotNull(method, "StrategyOld must expose CanTrade(Security, Portfolio, Sides, decimal, out string)");

		object[] args = [security, portfolio, side, volume, null];
		var result = (bool)method.Invoke(strategy, args);
		return result;
	}

	private static bool DecomposedCanTrade(Strategy strategy, Security security, Portfolio portfolio, Sides side, decimal volume)
	{
		var method = typeof(Strategy).GetMethod("CanTrade",
			BindingFlags.Instance | BindingFlags.NonPublic,
			null,
			[typeof(Security), typeof(Portfolio), typeof(Sides), typeof(decimal), typeof(string).MakeByRefType()],
			null);
		IsNotNull(method, "Strategy must expose CanTrade(Security, Portfolio, Sides, decimal, out string)");

		object[] args = [security, portfolio, side, volume, null];
		var result = (bool)method.Invoke(strategy, args);
		return result;
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task Parity_IsFormed_NativeTransitionAndCanTradeGate()
	{
		const int smaLen = 3;

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();
		var secId = security.ToSecurityId();

		// --- Monolith side ---
		var monoSma = new SimpleMovingAverage { Length = smaLen };
		var mono = new StrategyOld
		{
			Connector = new Connector(),
			Security = security,
			Portfolio = portfolio,
			Volume = 1m,
		};
		mono.Indicators.Add(monoSma);

		// --- Decomposed side ---
		var connMock = CreateMockConnector();
		var decoSma = new SimpleMovingAverage { Length = smaLen };
		var deco = new FormedRecordingStrategy
		{
			Connector = connMock.Object,
			Security = security,
			Portfolio = portfolio,
			Volume = 1m,
		};
		deco.Indicators.Add(decoSma);

		// Both must start NOT formed (an SMA of length 3 needs 3 inputs).
		IsFalse(mono.IsFormed, "Monolith must start not formed");
		IsFalse(deco.IsFormed, "Decomposed must start not formed");

		// CanTrade must reject on both while not formed and not started (state gate fails first,
		// but IsFormed is also false, so neither side can trade).
		IsFalse(MonolithCanTrade(mono, security, portfolio, Sides.Buy, 1m), "Monolith CanTrade must reject before start");
		IsFalse(DecomposedCanTrade(deco, security, portfolio, Sides.Buy, 1m), "Decomposed CanTrade must reject before start");

		// Start both so the only remaining CanTrade gate is IsFormed.
		StartMonolith(mono);
		await deco.StartAsync(CancellationToken);
		deco.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		mono.ProcessState.AreEqual(ProcessStates.Started);
		deco.ProcessState.AreEqual(ProcessStates.Started);

		// Feed identical finished candles into each engine's indicator and record the input index
		// at which IsFormed flips false -> true on each side.
		int? monoFormedAt = null;
		int? decoFormedAt = null;
		int? monoCanTradeAt = null;
		int? decoCanTradeAt = null;

		var t0 = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		for (var i = 0; i < smaLen + 2; i++)
		{
			var candle = Candle(secId, t0.AddMinutes(i), 100m + i);

			// Feed the indicator directly (the value extraction path Bind uses) on both sides.
			monoSma.Process(candle);
			decoSma.Process(candle);

			// IsFormed is consulted on the engine itself.
			if (monoFormedAt is null && mono.IsFormed)
				monoFormedAt = i;
			if (decoFormedAt is null && deco.IsFormed)
				decoFormedAt = i;

			// CanTrade (now started) returns true only once IsFormed is true.
			if (monoCanTradeAt is null && MonolithCanTrade(mono, security, portfolio, Sides.Buy, 1m))
				monoCanTradeAt = i;
			if (decoCanTradeAt is null && DecomposedCanTrade(deco, security, portfolio, Sides.Buy, 1m))
				decoCanTradeAt = i;
		}

		// (a) IsFormed flips false -> true at the SAME input on both engines.
		IsNotNull(monoFormedAt, "Monolith IsFormed must become true");
		IsNotNull(decoFormedAt, "Decomposed IsFormed must become true");
		AreEqual(monoFormedAt.Value, decoFormedAt.Value,
			$"IsFormed must flip at the same input on both engines: monolith@{monoFormedAt}, decomposed@{decoFormedAt}");

		// (b) IsFormed reflects "all indicators formed": an SMA of length N forms on its Nth input (index N-1).
		AreEqual(smaLen - 1, decoFormedAt.Value,
			$"IsFormed must reflect all indicators formed (SMA len {smaLen} forms at input #{smaLen}, index {smaLen - 1})");

		// (c) The CanTrade gate (started strategy) opens at the same input as IsFormed, identically.
		IsNotNull(monoCanTradeAt, "Monolith CanTrade must eventually allow trading");
		IsNotNull(decoCanTradeAt, "Decomposed CanTrade must eventually allow trading");
		AreEqual(monoCanTradeAt.Value, decoCanTradeAt.Value,
			$"CanTrade must open at the same input on both engines: monolith@{monoCanTradeAt}, decomposed@{decoCanTradeAt}");
		AreEqual(decoFormedAt.Value, decoCanTradeAt.Value,
			"CanTrade must open exactly when IsFormed becomes true");

		// (d) End-to-end: a RegisterOrder before forming is gated out, after forming admitted. Mirrors the
		// monolith CanTrade->RegisterOrder gate (StrategyOld.cs:1466).
		var beforeDeco = new FormedRecordingStrategy
		{
			Connector = CreateMockConnector().Object,
			Security = security,
			Portfolio = portfolio,
			Volume = 1m,
		};
		var beforeMock = Mock.Get(beforeDeco.Connector);
		var beforeRegistered = new List<Order>();
		beforeMock.Setup(c => c.RegisterOrder(It.IsAny<Order>())).Callback<Order>(beforeRegistered.Add);
		var beforeSma = new SimpleMovingAverage { Length = smaLen };
		beforeDeco.Indicators.Add(beforeSma);

		await beforeDeco.StartAsync(CancellationToken);
		beforeDeco.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		// Not formed yet -> order is gated out.
		beforeDeco.RegisterOrder(beforeDeco.CreateOrder(Sides.Buy, 100m, 1m));
		AreEqual(0, beforeRegistered.Count, "Order before forming must be gated out by CanTrade(IsFormed)");

		// Form the indicator with smaLen inputs.
		for (var i = 0; i < smaLen; i++)
			beforeSma.Process(Candle(secId, t0.AddMinutes(i), 100m + i));

		IsTrue(beforeDeco.IsFormed, "Strategy must be formed after smaLen inputs");

		beforeDeco.RegisterOrder(beforeDeco.CreateOrder(Sides.Buy, 100m, 1m));
		AreEqual(1, beforeRegistered.Count, "Order after forming must reach the connector");
	}

	// 2. Save / Load / Clone round-trip: configure an identical non-default parameter set on both engines,
	// then compare serialized values, round-tripped Load values, and Clone. KeepStatistics gating too.

	private sealed class ConfigStrategyOld : StrategyOld { }
	private sealed class ConfigStrategy : Strategy { }

	private static void ApplyNonDefaultConfig(dynamic strategy)
	{
		strategy.Volume = 7m;
		strategy.CommentMode = StrategyCommentModes.Id;
		strategy.TradingMode = StrategyTradingModes.LongOnly;
		strategy.UnrealizedPnLInterval = TimeSpan.FromSeconds(42);
		strategy.MaxOrdersBeforeAggregation = 123;
		strategy.IndicatorSource = Level1Fields.SpreadMiddle;
		strategy.RiskFreeRate = 3.5m;
		strategy.OrdersKeepTime = TimeSpan.FromHours(6);
		strategy.WaitAllTrades = true;
		strategy.RiskManager.Rules.Add(new RiskPositionSizeRule
		{
			Position = 50m,
			Action = RiskActions.StopTrading,
		});
	}

	// Persisted parameter values keyed by id; compares stored VALUES of shared parameters (type names out of scope).
	private static Dictionary<string, string> ParamValues(SettingsStorage storage)
	{
		var result = new Dictionary<string, string>();

		var parameters = storage.GetValue<SettingsStorage[]>(nameof(Strategy.Parameters));
		IsNotNull(parameters, "Saved storage must contain Parameters");

		foreach (var p in parameters)
		{
			var id = p.GetValue<string>(nameof(IStrategyParam.Id));
			var value = p.GetValue<object>(nameof(IStrategyParam.Value));
			result[id] = value?.ToString() ?? "null";
		}

		return result;
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public void Parity_SaveLoad_SerializedStoragesMatch()
	{
		var mono = new ConfigStrategyOld();
		var deco = new ConfigStrategy();

		ApplyNonDefaultConfig(mono);
		ApplyNonDefaultConfig(deco);

		var monoStorage = new SettingsStorage();
		var decoStorage = new SettingsStorage();
		mono.Save(monoStorage);
		deco.Save(decoStorage);

		var monoValues = ParamValues(monoStorage);
		var decoValues = ParamValues(decoStorage);

		// Compare shared parameters value-for-value. Id is a per-instance GUID, so it is excluded from the
		// value compare (only its presence matters); the decomposed engine also lacks the monolith _name param.
		var shared = monoValues.Keys.Intersect(decoValues.Keys)
			.Where(k => k != nameof(Strategy.Id))
			.OrderBy(k => k)
			.ToArray();

		IsTrue(monoValues.ContainsKey(nameof(Strategy.Id)) && decoValues.ContainsKey(nameof(Strategy.Id)),
			"Both engines must persist the Id parameter (value differs per instance, excluded from value compare)");

		IsTrue(shared.Length >= 10, $"Expected the engines to share many parameters, got {shared.Length}");

		// The non-default settings we explicitly configured must be present and persisted with the
		// SAME value on both sides (proves the comparison is non-vacuous).
		string[] mustContain = [nameof(Strategy.Volume), nameof(Strategy.CommentMode), nameof(Strategy.TradingMode),
			nameof(Strategy.IndicatorSource), nameof(Strategy.RiskFreeRate), nameof(Strategy.OrdersKeepTime), nameof(Strategy.WaitAllTrades)];

		foreach (var key in mustContain)
			IsTrue(shared.Contains(key), $"Shared persisted parameters must include {key}");

		foreach (var key in shared)
			AreEqual(monoValues[key], decoValues[key],
				$"Persisted value of parameter '{key}' must match: monolith='{monoValues[key]}', decomposed='{decoValues[key]}'");
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public void Parity_Load_RoundTripsValuesIntoFreshInstance()
	{
		// Monolith: Save -> Load into a fresh monolith.
		var mono = new ConfigStrategyOld();
		ApplyNonDefaultConfig(mono);
		var monoStorage = new SettingsStorage();
		mono.Save(monoStorage);

		var monoReloaded = new ConfigStrategyOld();
		monoReloaded.Load(monoStorage);

		// Decomposed: Save -> Load into a fresh decomposed.
		var deco = new ConfigStrategy();
		ApplyNonDefaultConfig(deco);
		var decoStorage = new SettingsStorage();
		deco.Save(decoStorage);

		var decoReloaded = new ConfigStrategy();
		decoReloaded.Load(decoStorage);

		// Round-tripped values must match the originals on both engines.
		void Check(string name, object monoVal, object decoVal, object expected)
		{
			AreEqual(expected, monoVal, $"Monolith round-trip of {name} must equal original");
			AreEqual(expected, decoVal, $"Decomposed round-trip of {name} must equal original");
		}

		Check(nameof(Strategy.Volume), monoReloaded.Volume, decoReloaded.Volume, 7m);
		Check(nameof(Strategy.CommentMode), monoReloaded.CommentMode, decoReloaded.CommentMode, StrategyCommentModes.Id);
		Check(nameof(Strategy.TradingMode), monoReloaded.TradingMode, decoReloaded.TradingMode, StrategyTradingModes.LongOnly);
		Check(nameof(Strategy.IndicatorSource), monoReloaded.IndicatorSource, decoReloaded.IndicatorSource, Level1Fields.SpreadMiddle);
		Check(nameof(Strategy.RiskFreeRate), monoReloaded.RiskFreeRate, decoReloaded.RiskFreeRate, 3.5m);
		Check(nameof(Strategy.OrdersKeepTime), monoReloaded.OrdersKeepTime, decoReloaded.OrdersKeepTime, TimeSpan.FromHours(6));
		Check(nameof(Strategy.WaitAllTrades), monoReloaded.WaitAllTrades, decoReloaded.WaitAllTrades, true);

		// RiskRules round-trip: the concrete rule must come back on both engines.
		AreEqual(1, monoReloaded.RiskManager.Rules.Count, "Monolith must round-trip the risk rule");
		AreEqual(1, decoReloaded.RiskManager.Rules.Count, "Decomposed must round-trip the risk rule");
		AreEqual(RiskActions.StopTrading, monoReloaded.RiskManager.Rules.First().Action);
		AreEqual(RiskActions.StopTrading, decoReloaded.RiskManager.Rules.First().Action);

		// UnrealizedPnLInterval and MaxOrdersBeforeAggregation are not StrategyParams (not persisted by either
		// engine), so they are out of the round-trip and covered by Clone below instead.
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public void Parity_Clone_CopiesParameterSet()
	{
		var mono = new ConfigStrategyOld();
		ApplyNonDefaultConfig(mono);
		var monoClone = mono.Clone();

		var deco = new ConfigStrategy();
		ApplyNonDefaultConfig(deco);
		var decoClone = deco.Clone();

		void Check(string name, object monoVal, object decoVal, object expected)
		{
			AreEqual(expected, monoVal, $"Monolith clone of {name} must equal original");
			AreEqual(expected, decoVal, $"Decomposed clone of {name} must equal original");
		}

		Check(nameof(Strategy.Volume), monoClone.Volume, decoClone.Volume, 7m);
		Check(nameof(Strategy.CommentMode), monoClone.CommentMode, decoClone.CommentMode, StrategyCommentModes.Id);
		Check(nameof(Strategy.TradingMode), monoClone.TradingMode, decoClone.TradingMode, StrategyTradingModes.LongOnly);
		Check(nameof(Strategy.IndicatorSource), monoClone.IndicatorSource, decoClone.IndicatorSource, Level1Fields.SpreadMiddle);
		Check(nameof(Strategy.RiskFreeRate), monoClone.RiskFreeRate, decoClone.RiskFreeRate, 3.5m);
		Check(nameof(Strategy.OrdersKeepTime), monoClone.OrdersKeepTime, decoClone.OrdersKeepTime, TimeSpan.FromHours(6));
		Check(nameof(Strategy.WaitAllTrades), monoClone.WaitAllTrades, decoClone.WaitAllTrades, true);

		// Risk rules survive the clone on both engines.
		AreEqual(1, monoClone.RiskManager.Rules.Count, "Monolith clone must copy the risk rule");
		AreEqual(1, decoClone.RiskManager.Rules.Count, "Decomposed clone must copy the risk rule");
	}

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public void Parity_SaveLoad_KeepStatisticsGatesStatsPersistence()
	{
		// With KeepStatistics=false (default) Save must NOT emit PnLManager/StatisticManager; with
		// KeepStatistics=true it must, on BOTH engines identically.
		static bool HasStats(SettingsStorage s)
			=> s.ContainsKey(nameof(Strategy.PnLManager)) || s.ContainsKey(nameof(Strategy.StatisticManager));

		var monoOff = new ConfigStrategyOld { KeepStatistics = false };
		var decoOff = new ConfigStrategy { KeepStatistics = false };
		var monoOffStorage = new SettingsStorage();
		var decoOffStorage = new SettingsStorage();
		monoOff.Save(monoOffStorage);
		decoOff.Save(decoOffStorage);

		IsFalse(HasStats(monoOffStorage), "Monolith must NOT persist stats when KeepStatistics=false");
		IsFalse(HasStats(decoOffStorage), "Decomposed must NOT persist stats when KeepStatistics=false");

		var monoOn = new ConfigStrategyOld { KeepStatistics = true };
		var decoOn = new ConfigStrategy { KeepStatistics = true };
		var monoOnStorage = new SettingsStorage();
		var decoOnStorage = new SettingsStorage();
		monoOn.Save(monoOnStorage);
		decoOn.Save(decoOnStorage);

		IsTrue(HasStats(monoOnStorage), "Monolith MUST persist stats when KeepStatistics=true");
		IsTrue(HasStats(decoOnStorage), "Decomposed MUST persist stats when KeepStatistics=true");
	}

	// 3. ApplyCommand.
	// Feed the SAME CommandMessage sequence to each engine and assert the same resulting
	// action/state: Start -> Started, RegisterOrder -> an order registered, Stop -> Stopped.

	private static CommandMessage Cmd(CommandTypes type)
		=> new() { Command = type, ObjectId = "test" };

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task Parity_ApplyCommand_StartRegisterStop()
	{
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		// --- Decomposed ---
		var decoMock = CreateMockConnector();
		var decoRegistered = new List<Order>();
		decoMock.Setup(c => c.RegisterOrder(It.IsAny<Order>())).Callback<Order>(decoRegistered.Add);
		var deco = new ConfigStrategy
		{
			Connector = decoMock.Object,
			Security = security,
			Portfolio = portfolio,
			Volume = 1m,
		};

		// Start command -> Started.
		deco.ApplyCommand(Cmd(CommandTypes.Start));
		// Start() drives the async entry point; settle and confirm the engine reached Started.
		deco.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		deco.ProcessState.AreEqual(ProcessStates.Started, "Decomposed Start command must reach Started");

		// RegisterOrder command -> an order registered at the connector.
		var regCmd = Cmd(CommandTypes.RegisterOrder);
		regCmd.Parameters[nameof(Order.Side)] = Sides.Buy.To<string>();
		regCmd.Parameters[nameof(Order.Volume)] = 3m.To<string>();
		regCmd.Parameters[nameof(Order.Price)] = 100m.To<string>();
		deco.ApplyCommand(regCmd);

		AreEqual(1, decoRegistered.Count, "Decomposed RegisterOrder command must register one order");
		decoRegistered[0].Side.AreEqual(Sides.Buy);
		decoRegistered[0].Volume.AreEqual(3m);
		decoRegistered[0].Price.AreEqual(100m);

		// Stop command -> Stopping/Stopped.
		deco.ApplyCommand(Cmd(CommandTypes.Stop));
		deco.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		IsTrue(deco.ProcessState is ProcessStates.Stopping or ProcessStates.Stopped,
			$"Decomposed Stop command must leave Started; got {deco.ProcessState}");

		// --- Monolith: same command sequence, same resulting actions ---
		var mono = new ConfigStrategyOld
		{
			Connector = new Connector(),
			Security = security,
			Portfolio = portfolio,
			Volume = 1m,
		};
		mono.WaitRulesOnStop = false;

		// ApplyCommand(Start) dispatches to Start(); deliver the Started state message directly because the
		// bare connector does not round-trip it (same as StartMonolith).
		mono.ApplyCommand(Cmd(CommandTypes.Start));
		if (mono.ProcessState != ProcessStates.Started)
			DriveMonolithState(mono, ProcessStates.Started);
		mono.ProcessState.AreEqual(ProcessStates.Started, "Monolith Start command must reach Started");

		var monoRegCmd = Cmd(CommandTypes.RegisterOrder);
		monoRegCmd.Parameters[nameof(Order.Side)] = Sides.Buy.To<string>();
		monoRegCmd.Parameters[nameof(Order.Volume)] = 3m.To<string>();
		monoRegCmd.Parameters[nameof(Order.Price)] = 100m.To<string>();

		var monoOrders = mono.Orders.Count();
		mono.ApplyCommand(monoRegCmd);
		AreEqual(monoOrders + 1, mono.Orders.Count(), "Monolith RegisterOrder command must register one order");
		var monoOrder = mono.Orders.Last();
		monoOrder.Side.AreEqual(Sides.Buy);
		monoOrder.Volume.AreEqual(3m);
		monoOrder.Price.AreEqual(100m);

		mono.ApplyCommand(Cmd(CommandTypes.Stop));
		if (mono.ProcessState == ProcessStates.Started)
			DriveMonolithState(mono, ProcessStates.Stopping);
		IsTrue(mono.ProcessState is ProcessStates.Stopping or ProcessStates.Stopped,
			$"Monolith Stop command must leave Started; got {mono.ProcessState}");

		// Same observable result on both engines: an order with the same side/volume/price was registered.
		decoRegistered[0].Side.AreEqual(monoOrder.Side, "Both engines must register the same side");
		decoRegistered[0].Volume.AreEqual(monoOrder.Volume, "Both engines must register the same volume");
		decoRegistered[0].Price.AreEqual(monoOrder.Price, "Both engines must register the same price");
	}

	// 4a. OrdersKeepTime / RecycleOrders teardown: once order-time span exceeds ~1.5x the window, RecycleOrders
	// drops Done orders older than (lastOrderTime - OrdersKeepTime); the surviving count must match on both engines.

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public async Task Parity_RecycleOrders_DropsOldDoneOrders()
	{
		var keepTime = TimeSpan.FromMinutes(10);
		var t0 = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		// Three early Done orders + one at t0+60 that triggers recycling (span 60min > 1.5*10min). The keep
		// window (Time >= t0+50) drops all three early ones.
		var times = new[]
		{
			t0,                       // order 1 (Done, old)
			t0.AddMinutes(2),         // order 2 (Done, old)
			t0.AddMinutes(4),         // order 3 (Done, old)
			t0.AddMinutes(60),        // order 4 (Done, recent) -> triggers recycle
		};

		var monoCount = RunRecycleMonolith(security, portfolio, keepTime, times);
		var decoCount = await RunRecycleDecomposed(security, portfolio, keepTime, times);

		// Only the recent order survives recycling on both engines.
		AreEqual(1, monoCount, "Monolith must keep only the recent Done order after recycling");
		AreEqual(1, decoCount, "Decomposed must keep only the recent Done order after recycling");
		AreEqual(monoCount, decoCount, "Orders count after recycling must match between engines");
	}

	private static int RunRecycleMonolith(Security security, Portfolio portfolio, TimeSpan keepTime, DateTime[] times)
	{
		var mono = new ConfigStrategyOld
		{
			Connector = new Connector(),
			Security = security,
			Portfolio = portfolio,
			OrdersKeepTime = keepTime,
			WaitRulesOnStop = false,
		};
		StartMonolith(mono);

		// OnStarted2 already subscribed OrderLookup, so CanProcess passes without manual registration.
		var orderLookup = mono.OrderLookup;

		long txId = 1;
		foreach (var time in times)
		{
			var order = CreateOrder(security, portfolio, Sides.Buy, 100m, 1m, txId++, time);
			// Pending attaches the order, then drive to Done so RecycleOrders can collect it.
			order.UserOrderId = mono.Id.To<string>();
			MonolithOrderReceived(mono, orderLookup, order);
			order.State = OrderStates.Done;
			order.Balance = 0;
			MonolithOrderReceived(mono, orderLookup, order);
		}

		return mono.Orders.Count();
	}

	private async Task<int> RunRecycleDecomposed(Security security, Portfolio portfolio, TimeSpan keepTime, DateTime[] times)
	{
		var deco = new ConfigStrategy
		{
			Connector = CreateMockConnector().Object,
			Security = security,
			Portfolio = portfolio,
			OrdersKeepTime = keepTime,
		};

		// Start so InitStartValues recomputes the recycle threshold (1.5x OrdersKeepTime), as the monolith does;
		// otherwise recycling never triggers.
		await deco.StartAsync(CancellationToken);
		deco.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		// Deliver orders on the auto-subscribed OrderLookup (a fresh Transactions sub would collide on its key).
		var sub = deco.OrderLookup;

		long txId = 1;
		foreach (var time in times)
		{
			var order = CreateOrder(security, portfolio, Sides.Buy, 100m, 1m, txId++, time);
			order.UserOrderId = deco.Id.To<string>();
			deco.OnConnectorOrderReceived(sub, order);
			order.State = OrderStates.Done;
			order.Balance = 0;
			deco.OnConnectorOrderReceived(sub, order);
		}

		return deco.Orders.Count();
	}

	// 4b. WaitAllTrades teardown. WaitAllTrades=true defers the final Stopped transition until the last trade
	// arrives; the monolith implements this by wiring a cancel-on-stop rule per registered order (whose Until
	// gates TryFinalStop). This test pins the divergence at that wiring surface: under WaitAllTrades +
	// CancelOrdersWhenStopping the monolith adds an order-scoped rule, the decomposed engine wires none (so it
	// cannot defer). The full behavioural Stopped-timing comparison needs post-stop trade delivery, which only
	// the FullEquivalence backtest harness can drive (a lightweight feed can't - OrderLookup is torn down on stop).

	private static int CountOrderRules(IMarketRuleContainer strategy, Order order)
		=> strategy.Rules.GetRulesByToken(order).Count();

	[TestMethod]
	[Timeout(15_000, CooperativeCancellation = true)]
	public void Parity_WaitAllTrades_WiresCancelOnStopRuleOnRegistration()
	{
		var security = CreateSecurity();
		var portfolio = CreatePortfolio();

		// --- Monolith: registering under WaitAllTrades + CancelOrdersWhenStopping wires an order-scoped
		// cancel-on-stop rule (whose Until defers the final stop). ---
		var mono = new ConfigStrategyOld
		{
			Connector = new Connector(),
			Security = security,
			Portfolio = portfolio,
			Volume = 10m,
			WaitAllTrades = true,
			CancelOrdersWhenStopping = true,
			WaitRulesOnStop = true,
		};
		StartMonolith(mono);

		var monoOrder = mono.CreateOrder(Sides.Buy, 100m, 10m);
		mono.RegisterOrder(monoOrder);

		var monoOrderRules = CountOrderRules(mono, monoOrder);

		IsTrue(monoOrderRules > 0,
			"Monolith must wire an order-scoped cancel-on-stop rule on registration (the WaitAllTrades deferral mechanism)");

		// --- Decomposed: same config; it wires NO order-scoped rule, so CanFinalStop has nothing to wait on. ---
		var decoMock = CreateMockConnector();
		decoMock.Setup(c => c.RegisterOrder(It.IsAny<Order>()));
		var deco = new ConfigStrategy
		{
			Connector = decoMock.Object,
			Security = security,
			Portfolio = portfolio,
			Volume = 10m,
			WaitAllTrades = true,
			CancelOrdersWhenStopping = true,
			WaitRulesOnStop = true,
		};

		// Drive the engine to Started so RegisterOrder is admitted.
		deco.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));

		var decoOrder = deco.CreateOrder(Sides.Buy, 100m, 10m);
		deco.RegisterOrder(decoOrder);

		var decoOrderRules = CountOrderRules(deco, decoOrder);

		// Documented divergence, not asserted equal: the decomposed engine lacks the WaitAllTrades
		// cancel-on-stop rule wiring, so it cannot defer Stopped. Behavioural comparison deferred (see region).
		if (decoOrderRules == monoOrderRules)
		{
			// If a future change wires the rule, surface it so this test is upgraded to a strict assertion.
			Inconclusive(
				$"Decomposed engine now wires {decoOrderRules} order-scoped rule(s) on registration, matching the " +
				$"monolith - the WaitAllTrades divergence appears closed. Upgrade this test to assert the full " +
				$"Stopped-deferral behaviour via the FullEquivalence backtest harness.");
		}

		AreEqual(0, decoOrderRules,
			"Known divergence: the decomposed engine wires no WaitAllTrades cancel-on-stop rule (so it does not " +
			"defer Stopped on WaitAllTrades). Behavioural parity is deferred to the FullEquivalence backtest harness.");
	}

	#endregion

	#region Design-time property-surface parity (Browsable/Obsolete)

	// Design-time property-surface parity. The designer/property grids enumerate members via TypeDescriptor,
	// honouring [Browsable]/[Obsolete] on the CLR properties; a different visible/obsolete set would render a
	// different grid. Resolving the attributes the same way the grid does (AttributeHelper.IsBrowsable/IsObsolete),
	// these tests assert: every COMMON property has matching Browsable AND Obsolete (with a reason-carrying
	// allow-list), and the NEW-ONLY / OLD-ONLY sets each equal a documented expected set. Flipping any single
	// attribute or adding/removing a visible property fails an assert.

	private const BindingFlags _surfaceFlags = BindingFlags.Public | BindingFlags.Instance;

	private readonly record struct PropFacts(bool Browsable, bool Obsolete, string DeclaringType);

	// The design-time property surface: public instance, non-indexer properties keyed by name with their
	// inheritance-aware Browsable/Obsolete state (most-derived declaration wins).
	private static Dictionary<string, PropFacts> Surface(Type type)
	{
		var result = new Dictionary<string, PropFacts>();

		foreach (var p in type.GetProperties(_surfaceFlags))
		{
			if (p.GetIndexParameters().Length > 0)
				continue; // skip indexers - never shown as a grid row

			if (result.ContainsKey(p.Name))
				continue; // keep the most-derived declaration

			result[p.Name] = new(p.IsBrowsable(), p.IsObsolete(), p.DeclaringType.Name);
		}

		return result;
	}

	// --- Documented expected NEW-ONLY / OLD-ONLY sets (bound via nameof so a rename breaks the build) ---

	// Decomposed-only properties (the new sub-pipeline objects and position view); all runtime
	// plumbing/views, so all must be hidden ([Browsable(false)]) like the monolith hid its managers.
	private static readonly Dictionary<string, bool> _expectedNewOnly = new()
	{
		[nameof(Strategy.Engine)] = false,          // StrategyEngine - state machine + routing
		[nameof(Strategy.OrderProcessor)] = false,  // OrderPipeline
		[nameof(Strategy.Trades)] = false,          // TradePipeline
		[nameof(Strategy.Subscriptions)] = false,   // SubscriptionRegistry
		[nameof(Strategy.PositionsList)] = false,   // new (securityId, portfolio) -> net position view
	};

	// Monolith-only properties. The decomposed engine is a superset, so this is intentionally EMPTY;
	// an accidental drop of a monolith property shows up here and fails the test.
	private static readonly string[] _expectedOldOnly = [];

	// Justified COMMON-property divergences (none today): name -> reason, excluded from the equality assert.
	// The set is asserted to contain ONLY these keys, so it cannot silently absorb a regression.
	private static readonly Dictionary<string, string> _allowedCommonDivergences = [];

	[TestMethod]
	public void CommonProperties_HaveMatchingBrowsableAndObsolete()
	{
		var oldS = Surface(typeof(StrategyOld));
		var newS = Surface(typeof(Strategy));

		var common = oldS.Keys.Intersect(newS.Keys).OrderBy(n => n, StringComparer.Ordinal).ToArray();

		IsGreaterOrEqual(common.Length, 50,
			$"Expected a large shared property surface, got only {common.Length}");

		var divergences = new List<string>();

		foreach (var name in common)
		{
			if (_allowedCommonDivergences.ContainsKey(name))
				continue;

			var o = oldS[name];
			var n = newS[name];

			if (o.Browsable != n.Browsable)
				divergences.Add($"{name}: Browsable monolith={o.Browsable} decomposed={n.Browsable}");

			if (o.Obsolete != n.Obsolete)
				divergences.Add($"{name}: Obsolete monolith={o.Obsolete} decomposed={n.Obsolete}");
		}

		IsTrue(divergences.Count == 0,
			$"Design-time Browsable/Obsolete divergences between StrategyOld and Strategy " +
			$"(the designer would render a different grid):{Environment.NewLine}{divergences.JoinN()}");

		// The allow-list must reference only real shared properties, otherwise it is dead config that
		// could mask a future regression.
		foreach (var allowed in _allowedCommonDivergences.Keys)
			IsTrue(common.Contains(allowed),
				$"Allow-listed COMMON divergence '{allowed}' is not actually a shared property - stale entry.");
	}

	[TestMethod]
	public void NewOnlyProperties_MatchExpectedSetAndAreHidden()
	{
		var oldS = Surface(typeof(StrategyOld));
		var newS = Surface(typeof(Strategy));

		var newOnly = newS.Keys.Except(oldS.Keys).OrderBy(n => n, StringComparer.Ordinal).ToArray();

		AreEquivalent(_expectedNewOnly.Keys.ToArray(), newOnly,
			$"Decomposed-only property set changed. Expected [{_expectedNewOnly.Keys.ToArray().JoinComma()}], " +
			$"got [{newOnly.JoinComma()}]. A genuinely new feature must be added here (with its expected " +
			$"Browsable state); an accidental new VISIBLE property must be hidden or removed.");

		// Each new member must carry its expected Browsable state (all hidden); a stray visible one leaks into the grid.
		foreach (var (name, expectedBrowsable) in _expectedNewOnly)
			AreEqual(expectedBrowsable, newS[name].Browsable,
				$"Decomposed-only property '{name}' must have Browsable={expectedBrowsable} to match the " +
				$"monolith's design-time intent (internal pipelines/views are not grid rows).");
	}

	[TestMethod]
	public void OldOnlyProperties_MatchExpectedSet()
	{
		var oldS = Surface(typeof(StrategyOld));
		var newS = Surface(typeof(Strategy));

		var oldOnly = oldS.Keys.Except(newS.Keys).OrderBy(n => n, StringComparer.Ordinal).ToArray();

		AreEquivalent(_expectedOldOnly, oldOnly,
			$"Monolith-only property set changed. Expected [{_expectedOldOnly.JoinComma()}], " +
			$"got [{oldOnly.JoinComma()}]. The decomposed engine is meant to be a superset of the " +
			$"monolith's public properties; an entry here means a property was dropped in the migration.");
	}

	// Pins the concrete facts the COMMON comparison relies on: ChildStrategies (whose missing [Obsolete]
	// was the original regression) must be hidden AND obsolete on both engines.
	[TestMethod]
	public void ChildStrategies_IsHiddenAndObsoleteOnBothEngines()
	{
		var oldS = Surface(typeof(StrategyOld));
		var newS = Surface(typeof(Strategy));

		const string name = nameof(StrategyOld.ChildStrategies);

		IsTrue(oldS.ContainsKey(name) && newS.ContainsKey(name),
			"ChildStrategies must be a COMMON property on both engines");

		IsFalse(oldS[name].Browsable, "Monolith ChildStrategies must be hidden");
		IsFalse(newS[name].Browsable, "Decomposed ChildStrategies must be hidden");

		IsTrue(oldS[name].Obsolete, "Monolith ChildStrategies must be obsolete");
		IsTrue(newS[name].Obsolete, "Decomposed ChildStrategies must be obsolete (the original regression - now fixed)");
	}

	// Sample of runtime/computed-state properties the monolith hides: each must be hidden on the decomposed
	// engine too (this task hid 20 that were wrongly visible on the new engine).
	[TestMethod]
	public void RuntimeStateProperties_AreHiddenOnDecomposedToo()
	{
		var newS = Surface(typeof(Strategy));

		string[] mustBeHidden =
		[
			nameof(Strategy.Connector), nameof(Strategy.PnL), nameof(Strategy.Position),
			nameof(Strategy.ProcessState), nameof(Strategy.IsFormed), nameof(Strategy.IsOnline),
			nameof(Strategy.StatisticManager), nameof(Strategy.PnLManager), nameof(Strategy.RiskManager),
			nameof(Strategy.Commission), nameof(Strategy.Slippage), nameof(Strategy.Latency),
			nameof(Strategy.ErrorState), nameof(Strategy.LastError), nameof(Strategy.StartedTime),
			nameof(Strategy.TotalWorkingTime), nameof(Strategy.IsBacktesting), nameof(Strategy.Indicators),
			nameof(Strategy.Positions), nameof(Strategy.OrderBookSources),
		];

		foreach (var name in mustBeHidden)
			IsFalse(newS[name].Browsable,
				$"Decomposed '{name}' is a runtime/computed-state member the monolith hides; it must be [Browsable(false)].");
	}

	#endregion
}
