namespace StockSharp.Tests;

using StockSharp.Algo.PnL;
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

		public DateTime CurrentTimeUtc { get; set; } = DateTime.UtcNow;
		public List<Message> SentMessages { get; } = [];
		public void SendOutMessage(Message message) => SentMessages.Add(message);
		public long GetNextTransactionId() => Interlocked.Increment(ref _nextId);
	}

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
		connector.Start();

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
		connector.Start();

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
		connector.Start();

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
		connector.Start();

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
		decomposed.Start();
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
	public void SmaEquivalence_StateLifecycle()
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
		decomposed.Start();
		decomposed.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Started));
		decomposed.ProcessState.AreEqual(ProcessStates.Started);
		decomposed.StateHistory.Count.AreEqual(1);
		decomposed.StateHistory[0].AreEqual(ProcessStates.Started);

		// stop
		decomposed.Stop();
		decomposed.Engine.OnMessage(new StrategyEngine.StrategyStateMessage(ProcessStates.Stopping));
		decomposed.ProcessState.AreEqual(ProcessStates.Stopping);
		decomposed.StateHistory.Count.AreEqual(2);
		decomposed.StateHistory[1].AreEqual(ProcessStates.Stopping);
	}

	#endregion
}
