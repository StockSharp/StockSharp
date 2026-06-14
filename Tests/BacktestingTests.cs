namespace StockSharp.Tests;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Indicators;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.Designer;

/// <summary>
/// Tests for backtesting functionality using <see cref="HistoryEmulationConnector"/> with <see cref="SmaStrategy"/>.
/// </summary>
[TestClass]
public class BacktestingTests : BaseTestClass
{
	/// <summary>
	/// Simple test to verify backtest completes without hanging and orders work.
	/// Uses short time period to run fast.
	/// </summary>
	[TestMethod]
	[Timeout(60_000, CooperativeCancellation = true)]
	public async Task BacktestCompletesWithoutHanging()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		// Use only 2 days of data for fast test
		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(2);

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var ordersReceived = 0;
		var newOrders = 0;
		var execMessagesWithIds = 0;
		var execMessagesWithoutIds = 0;

		connector.OrderReceived += (sub, order) =>
		{
			Interlocked.Increment(ref ordersReceived);
		};

		connector.NewOrder += order =>
		{
			Interlocked.Increment(ref newOrders);
		};

		// Track ExecutionMessages at different levels
		var execAtEmulationTotal = 0;
		var execAtEmulationWithIds = 0;
		var execAtBasket = 0;

		// Track at EmulationMessageAdapter output
		connector.EmulationAdapter.NewOutMessageAsync += (msg, ct) =>
		{
			if (msg is ExecutionMessage exec && exec.HasOrderInfo)
			{
				Interlocked.Increment(ref execAtEmulationTotal);
				var ids = exec.GetSubscriptionIds();
				if (ids.Length > 0)
					Interlocked.Increment(ref execAtEmulationWithIds);
			}
			return default;
		};

		// Track at BasketMessageAdapter output
		connector.Adapter.NewOutMessageAsync += (msg, ct) =>
		{
			if (msg is ExecutionMessage exec && exec.HasOrderInfo)
			{
				var ids = exec.GetSubscriptionIds();
				if (ids.Length > 0)
					Interlocked.Increment(ref execMessagesWithIds);
				else
					Interlocked.Increment(ref execMessagesWithoutIds);
				Interlocked.Increment(ref execAtBasket);
			}
			return default;
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
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

		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();

		connector.Connect();
		await connector.StartAsync(CancellationToken);

		// Short timeout - should complete quickly with 2 days of data
		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(15), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in 15 seconds");
		}

		IsTrue(connector.IsFinished, "Backtest should finish");
		Console.WriteLine($"NewOrders: {newOrders}, OrdersReceived: {ordersReceived}");
		Console.WriteLine($"ExecMsgs at Emulation: {execAtEmulationTotal} total, {execAtEmulationWithIds} with IDs");
		Console.WriteLine($"ExecMsgs at Basket: {execAtBasket} (with IDs: {execMessagesWithIds}, without IDs: {execMessagesWithoutIds})");
		Console.WriteLine($"Strategy Orders: {strategy.Orders.Count()}");
		Console.WriteLine($"Strategy MyTrades: {strategy.MyTrades.Count()}");

		// Check order states
		var states = strategy.Orders.GroupBy(o => o.State).Select(g => $"{g.Key}:{g.Count()}");
		Console.WriteLine($"Order states: {states.JoinCommaSpace()}");

		// The instrumentation above exists to catch the regression where order-info execution
		// messages reach the basket output WITHOUT subscription IDs (they then cannot be routed to
		// strategy/connector subscribers). Assert that integrity instead of merely printing it:
		// every order-info execution that reaches the basket must carry subscription IDs.
		AreEqual(0, execMessagesWithoutIds,
			$"Order-info execution messages must carry subscription IDs at the basket output (without IDs={execMessagesWithoutIds}, with IDs={execMessagesWithIds})");

		// When the strategy actually placed orders, those orders must produce order-info execution
		// messages all the way through the emulator and basket layers (no exec loss).
		if (newOrders > 0)
		{
			IsTrue(execAtEmulationTotal > 0, $"Orders were placed ({newOrders}) but no order execs reached the emulation adapter");
			IsTrue(execAtBasket > 0, $"Orders were placed ({newOrders}) but no order execs reached the basket adapter");
			IsTrue(execMessagesWithIds > 0, "Order-info execution messages at the basket must carry subscription IDs when orders were placed");
		}
	}

	/// <summary>
	/// Verify that candles are built from ticks during backtest (BuildFrom = Ticks).
	/// Reproduces the WPF sample scenario where ticks are the data source.
	/// </summary>
	[TestMethod]
	public async Task BacktestBuildCandlesFromTicks()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(2);

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var candleCount = 0;

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connect();

		// subscribe to candles built from ticks
		var subscription = new Subscription(
			TimeSpan.FromMinutes(1).TimeFrame(),
			security)
		{
			MarketData =
			{
				BuildFrom = DataType.Ticks,
				BuildMode = MarketDataBuildModes.Build,
				IsFinishedOnly = true,
			}
		};

		connector.CandleReceived += (sub, candle) =>
		{
			Interlocked.Increment(ref candleCount);
		};

		connector.Subscribe(subscription);

		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in 30 seconds");
		}

		IsTrue(candleCount > 0, $"Should build candles from ticks, got {candleCount}");
	}

	/// <summary>
	/// Verify that candles are built from Level1 during backtest (BuildFrom = Level1).
	/// </summary>
	[TestMethod]
	public async Task BacktestBuildCandlesFromLevel1()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(2);

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var candleCount = 0;

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connect();

		var subscription = new Subscription(
			TimeSpan.FromMinutes(1).TimeFrame(),
			security)
		{
			MarketData =
			{
				BuildFrom = DataType.Level1,
				BuildMode = MarketDataBuildModes.Build,
				BuildField = Level1Fields.BestBidPrice,
				IsFinishedOnly = true,
			}
		};

		connector.CandleReceived += (sub, candle) =>
		{
			Interlocked.Increment(ref candleCount);
		};

		connector.Subscribe(subscription);

		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(30), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in 30 seconds");
		}

		IsTrue(candleCount > 0, $"Should build candles from Level1, got {candleCount}");
	}

	/// <summary>
	/// Full month backtest with BuildFrom=Ticks — reproduces WPF sample scenario.
	/// Tests that backtest completes without hanging for the full date range.
	/// </summary>
	[TestMethod]
	public async Task BacktestFullMonth_BuildFromTicks()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var candleCount = 0;
		var lastProgress = 0;

		connector.ProgressChanged += step =>
		{
			if (step % 10 == 0 && step > lastProgress)
				Console.WriteLine($"Progress: {step}%, candles: {candleCount}");
			lastProgress = step;
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connect();

		// Subscribe to candles built from ticks (same as WPF Ticks mode)
		var subscription = new Subscription(
			TimeSpan.FromMinutes(1).TimeFrame(),
			security)
		{
			MarketData =
			{
				BuildFrom = DataType.Ticks,
				BuildMode = MarketDataBuildModes.Build,
				IsFinishedOnly = true,
			}
		};

		connector.CandleReceived += (sub, candle) =>
		{
			Interlocked.Increment(ref candleCount);
		};

		connector.Subscribe(subscription);

		await connector.StartAsync(CancellationToken);

		// 5 minutes timeout for full month of tick data
		using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
		cts.Token.Register(() => tcs.TrySetResult(false));
		var ok = await tcs.Task;

		Console.WriteLine($"Final: progress={lastProgress}%, candles={candleCount}");

		IsTrue(ok, $"Backtest should complete without timeout (progress reached {lastProgress}%)");
		IsTrue(lastProgress >= 100, $"Progress should reach 100%, got {lastProgress}%");
		IsTrue(candleCount > 0, $"Should have candles, got {candleCount}");
	}

	/// <summary>
	/// Full month backtest with a strategy that really builds its candles from ticks
	/// (<see cref="WpfSmaStrategy"/> with <see cref="DataType.Ticks"/> as the build source,
	/// <see cref="MarketDataBuildModes.Build"/>). Verifies the tick-driven candle path is
	/// actually exercised end-to-end: the strategy places orders, those orders fill into
	/// trades, and no OrderRegisterFailed occurs due to the emulator time-race condition.
	/// </summary>
	[TestMethod]
	[Timeout(360_000, CooperativeCancellation = true)]
	public async Task BacktestFullMonth_BuildFromTicks_WithStrategy()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		security.PriceStep = 0.01m;
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var lastProgress = 0;
		var orderErrors = 0;

		// Verify the candles the strategy trades on are genuinely built from ticks
		// (BuildMode=Build), not loaded from the candle storage.
		var tickBuiltCandles = 0;
		connector.CandleReceived += (sub, candle) =>
		{
			// Candle subscriptions always carry a MarketDataMessage, so MarketData is safe here.
			if (candle.State == CandleStates.Finished && sub.MarketData.BuildFrom == DataType.Ticks)
				Interlocked.Increment(ref tickBuiltCandles);
		};

		connector.ProgressChanged += step => lastProgress = step;
		connector.OrderRegisterFailed += fail =>
		{
			Interlocked.Increment(ref orderErrors);
			Console.WriteLine($"OrderRegisterFailed: {fail.Error.Message}");
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		// Real BuildFrom=Ticks strategy: its candle subscription uses
		// BuildFrom=Ticks + BuildMode=Build (see WpfSmaStrategy.OnStarted2).
		var strategy = new WpfSmaStrategy
		{
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			LongSma = 80,
			ShortSma = 10,
			BuildFrom = DataType.Ticks,
		};

		connector.Connect();
		strategy.Start();
		await connector.StartAsync(CancellationToken);

		using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
		cts.Token.Register(() => tcs.TrySetResult(false));
		var ok = await tcs.Task;

		strategy.Stop();

		var orders = strategy.Orders.Count();
		var trades = strategy.MyTrades.Count();

		Console.WriteLine($"Progress: {lastProgress}%");
		Console.WriteLine($"Tick-built candles: {tickBuiltCandles}");
		Console.WriteLine($"Strategy trades: {trades}");
		Console.WriteLine($"Strategy orders: {orders}");
		Console.WriteLine($"Order errors (time race): {orderErrors}");
		Console.WriteLine($"Strategy PnL: {strategy.PnL}");

		IsTrue(ok, $"Backtest should complete without timeout (progress reached {lastProgress}%)");
		IsTrue(tickBuiltCandles > 0, $"Strategy must trade on candles built from ticks, got {tickBuiltCandles}");
		IsTrue(orders > 0, $"Tick-built SMA strategy should place orders over a full month, got {orders}");
		IsTrue(trades > 0, $"Tick-built SMA strategy orders should produce fills over a full month, got {trades}");
		AreEqual(0, orderErrors, $"No OrderRegisterFailed (time race) errors should occur, got {orderErrors}");
	}

	/// <summary>
	/// Backtest with BuildFrom=Ticks subscription and order registration.
	/// Reproduces time race: CandleBuilderMessageAdapter absorbs ticks,
	/// Connector._currentTime lags behind emulator._lastInputTime.
	/// </summary>
	[TestMethod]
	public async Task BacktestBuildFromTicks_OrderTimeMismatch()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		security.PriceStep = 0.01m;
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = startTime.AddDays(3);

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var orderErrors = 0;
		var orderCount = 0;
		var candleCount = 0;

		connector.OrderRegisterFailed += fail =>
		{
			Interlocked.Increment(ref orderErrors);
			Console.WriteLine($"OrderRegisterFailed: {fail.Error.Message}");
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Connect();

		// Subscribe to candles with BuildFrom=Ticks (like the console/WPF sample)
		var subscription = new Subscription(
			TimeSpan.FromMinutes(1).TimeFrame(),
			security)
		{
			MarketData =
			{
				BuildFrom = DataType.Ticks,
				BuildMode = MarketDataBuildModes.Build,
				IsFinishedOnly = true,
			}
		};

		var side = Sides.Buy;
		connector.CandleReceived += (sub, candle) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			Interlocked.Increment(ref candleCount);

			// register order on every 50th candle
			if (candleCount % 50 != 0)
				return;

			side = side == Sides.Buy ? Sides.Sell : Sides.Buy;
			var order = new Order
			{
				Security = security,
				Portfolio = portfolio,
				Side = side,
				Price = candle.ClosePrice,
				Volume = 1,
				Type = OrderTypes.Limit,
			};
			connector.RegisterOrder(order);
			Interlocked.Increment(ref orderCount);
		};

		connector.Subscribe(subscription);

		await connector.StartAsync(CancellationToken);

		using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
		cts.Token.Register(() => tcs.TrySetResult(false));
		var ok = await tcs.Task;

		Console.WriteLine($"Candles: {candleCount}, Orders: {orderCount}, Errors: {orderErrors}");

		IsTrue(ok, "Backtest should complete without timeout");
		IsTrue(orderCount > 0, $"Should have placed orders, got {orderCount}");
		AreEqual(0, orderErrors, $"No OrderRegisterFailed errors should occur (got {orderErrors} out of {orderCount} orders)");
	}

	private static Security CreateTestSecurity()
	{
		return new() { Id = Paths.HistoryDefaultSecurity };
	}

	private static Portfolio CreateTestPortfolio()
	{
		return Portfolio.CreateSimulator();
	}

	private static IStorageRegistry GetHistoryStorage()
	{
		var fs = Helper.FileSystem;
		return fs.GetStorage(Paths.HistoryDataPath);
	}

	private static bool SkipIfNoHistoryData()
	{
		if (Paths.HistoryDataPath == null)
		{
			Console.WriteLine("Skipping test: HistoryDataPath is null (stocksharp.samples.historydata package not installed)");
			return true;
		}
		return false;
	}

	private static HistoryEmulationConnector CreateConnector(
		ISecurityProvider secProvider,
		IPortfolioProvider pfProvider,
		IStorageRegistry storageRegistry,
		DateTime startTime,
		DateTime stopTime,
		bool verifyMode = true)
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
		((MarketEmulator)connector.EmulationAdapter.Emulator).VerifyMode = verifyMode;

		return connector;
	}

	/// <summary>
	/// Create a deterministic connector that processes all messages synchronously.
	/// Uses <see cref="PassThroughMessageChannel"/> instead of <see cref="InMemoryMessageChannel"/>
	/// to eliminate async background processing that causes non-determinism.
	/// </summary>
	private static HistoryEmulationConnector CreateDeterministicConnector(
		ISecurityProvider secProvider,
		IPortfolioProvider pfProvider,
		IStorageRegistry storageRegistry,
		DateTime startTime,
		DateTime stopTime,
		bool verifyMode = true)
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

		// Use PassThroughMessageChannel for synchronous, deterministic processing.
		// InMemoryMessageChannel uses an async background task with a priority queue,
		// which causes non-deterministic ordering when strategy feedback (orders)
		// competes with market data for processing time.
		var connector = new HistoryEmulationConnector(
			historyAdapter, true,
			new PassThroughMessageChannel(),
			secProvider, pfProvider,
			storageRegistry.ExchangeInfoProvider);

		connector.EmulationAdapter.Settings.MatchOnTouch = true;
		((MarketEmulator)connector.EmulationAdapter.Emulator).VerifyMode = verifyMode;

		return connector;
	}

	/// <summary>
	/// Tests that orders are generated during backtesting when SMA crossover occurs.
	/// </summary>
	[TestMethod]
	public async Task BacktestGeneratesOrders()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		// Debug: track candle reception
		var candlesReceived = 0;
		connector.CandleReceived += (sub, candle) =>
		{
			Interlocked.Increment(ref candlesReceived);
		};

		// Debug: track subscription responses
		var subscriptionResponses = new List<string>();
		connector.SubscriptionReceived += (sub, resp) =>
		{
			subscriptionResponses.Add($"OK {sub.DataType}");
		};
		connector.SubscriptionFailed += (sub, error, isSubscribe) =>
		{
			subscriptionResponses.Add($"FAILED {sub.DataType}: {error.Message}");
		};

		// Debug: track connection
		var connectionStates = new List<string>();
		connector.Connected += () => connectionStates.Add("Connected");
		connector.Disconnected += () => connectionStates.Add("Disconnected");
		connector.ConnectionError += ex => connectionStates.Add($"Error: {ex.Message}");

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

		var ordersReceived = new List<Order>();
		strategy.OrderReceived += (sub, order) =>
		{
			ordersReceived.Add(order);
		};

		// Debug: track strategy candles
		var strategyCandlesCount = 0;
		var finishedCandlesCount = 0;
		strategy.CandleReceived += (sub, candle) =>
		{
			Interlocked.Increment(ref strategyCandlesCount);
			if (candle.State == CandleStates.Finished)
				Interlocked.Increment(ref finishedCandlesCount);
		};

		// Track strategy errors
		var strategyErrors = new List<string>();
		strategy.Error += (strat, ex) =>
		{
			strategyErrors.Add(ex.Message);
		};

		// Track order registrations
		strategy.OrderRegistering += order =>
		{
			Console.WriteLine($"Registering order: {order.Side} {order.Volume} @ {order.Price}");
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			connectionStates.Add($"State: {state}");
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();

		Console.WriteLine($"After strategy.Start, subscriptions: {connector.Subscriptions.Count()}");
		foreach (var sub in connector.Subscriptions)
			Console.WriteLine($"  - {sub.SubscriptionMessage.Type}: {sub.DataType}");

		connector.Connect();

		// Wait for connection
		await Task.Delay(500);
		Console.WriteLine($"After connector.Connect, connectionState: {connector.ConnectionState}");

		Console.WriteLine($"After Connect, subscriptions: {connector.Subscriptions.Count()}");
		foreach (var sub in connector.Subscriptions)
			Console.WriteLine($"  - {sub.SubscriptionMessage.Type}: {sub.DataType}, State: {sub.State}");

		await connector.StartAsync(CancellationToken);

		Console.WriteLine($"After connector.Start");

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Debug output
		Console.WriteLine($"Connection states: {connectionStates.Join(" -> ")}");
		Console.WriteLine($"Candles received (connector): {candlesReceived}");
		Console.WriteLine($"Candles received (strategy): {strategyCandlesCount}");
		Console.WriteLine($"Finished candles (strategy): {finishedCandlesCount}");
		Console.WriteLine($"Subscription responses: {subscriptionResponses.Count}");
		Console.WriteLine($"Orders received: {ordersReceived.Count}");
		Console.WriteLine($"Final subscriptions: {connector.Subscriptions.Count()}");
		Console.WriteLine($"Final strategy state: {strategy.ProcessState}");
		Console.WriteLine($"Strategy errors: {strategyErrors.Join("; ")}");
		Console.WriteLine($"Strategy position: {strategy.Position}");

		// Verify that orders were generated
		IsTrue(ordersReceived.Count > 0, $"Expected at least one order. Candles: {candlesReceived}, Subscriptions: {subscriptionResponses.Join("; ")}, Connection: {connectionStates.Join(" -> ")}");
	}

	/// <summary>
	/// Tests that trades are executed during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestGeneratesTrades()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

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

		var tradesReceived = new List<MyTrade>();
		strategy.OwnTradeReceived += (sub, trade) =>
		{
			tradesReceived.Add(trade);
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify that trades were executed
		IsTrue(tradesReceived.Count > 0, "Expected at least one trade to be executed");
	}

	/// <summary>
	/// Tests that PnL changes occur during backtesting.
	/// </summary>
	[TestMethod]
	[Timeout(180_000, CooperativeCancellation = true)]
	public async Task BacktestGeneratesPnLChanges()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

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

		// PnLReceived2 reports (realized, unrealized) snapshots; strategy.PnL == realized + unrealized
		// (see TraderHelper.GetPnL). The realized component changes only on fills, and every fill with
		// non-zero PnL raises PnLReceived2 (Strategy.OnConnectorOwnTradeReceived), so the realized value
		// of the LAST event is the final realized PnL and is event-synced (deterministic). The
		// unrealized component, however, is marked-to-market on every incoming market message
		// (PnLManager.ProcessMessage on each tick/candle/quote) while PnLReceived2 is throttled by
		// UnrealizedPnLInterval (1 min) and only fires when Positions.Any(); so the unrealized value of
		// the last event is frozen at that event's price and keeps drifting afterwards. Track both
		// components separately rather than only the combined total.
		var pnlChanges = new List<decimal>();
		var lastReportedRealized = 0m;
		var lastReportedTotal = 0m;
		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			lastReportedRealized = realized;
			lastReportedTotal = realized + (unrealized ?? 0);
			pnlChanges.Add(lastReportedTotal);
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		var tradeCount = strategy.MyTrades.Count();
		Console.WriteLine($"Trades: {tradeCount}, PnL events: {pnlChanges.Count}, Final PnL: {strategy.PnL}");

		// The full-month SMA strategy is expected to trade; guard so the oracle is not vacuous.
		IsTrue(tradeCount > 0, "Expected trades so PnL movement can be verified");

		// Once trades occur there MUST be at least one PnL event - the previous code skipped this
		// check entirely when no trades happened, masking a regression that drops PnL events.
		IsTrue(pnlChanges.Count > 0, "Expected PnL changes when trades occurred");

		var manager = strategy.PnLManager;

		// strategy.PnL must be exactly its definition: realized + unrealized straight from the PnL
		// manager (TraderHelper.GetPnL). This pins the property to the manager and catches a sign
		// flip or magnitude error in either component, without depending on event timing.
		AreEqual(manager.RealizedPnL + manager.UnrealizedPnL, strategy.PnL,
			"Strategy PnL must equal realized + unrealized from the PnL manager");

		// The realized PnL reported by the LAST PnLReceived2 event must equal the final realized PnL:
		// realized only moves on fills, and every realized-moving fill raises the event, so no realized
		// change can occur after the last event. This is a deterministic, event-synced oracle for the
		// traded result. NOTE: comparing the last event's *total* (realized + unrealized) to the final
		// strategy.PnL would be wrong - the strategy ends with an open position, and its unrealized PnL
		// is re-marked on every later market message while the event is throttled by UnrealizedPnLInterval,
		// so the two legitimately diverge (that is engine-correct behavior, not a bug).
		AreEqual(manager.RealizedPnL, lastReportedRealized,
			"Last reported realized PnL must equal the final realized PnL (realized only changes on fills, each of which raises PnLReceived2)");

		// The combined total of the last event likewise reflects realized + unrealized at that event's
		// time; assert that reconstruction is internally consistent (guards the event payload wiring).
		AreEqual(pnlChanges[^1], lastReportedTotal,
			"Last collected total must match the last event's (realized + unrealized)");
	}

	/// <summary>
	/// Tests that position changes occur during backtesting.
	/// </summary>
	[TestMethod]
	[Timeout(180_000, CooperativeCancellation = true)]
	public async Task BacktestGeneratesPositionChanges()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

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

		var positionChanges = new List<decimal>();
		strategy.PositionReceived += (s, pos) =>
		{
			positionChanges.Add(pos.CurrentValue ?? 0);
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		var tradeCount = strategy.MyTrades.Count();
		Console.WriteLine($"Trades: {tradeCount}, Position events: {positionChanges.Count}, Final position: {strategy.Position}");

		// The full-month SMA strategy is expected to trade; guard so the oracle is not vacuous.
		IsTrue(tradeCount > 0, "Expected trades so position movement can be verified");

		// Once trades occur there MUST be position events - the previous code skipped this check
		// entirely when no trades happened, masking a regression that drops position events.
		IsTrue(positionChanges.Count > 0, "Expected position changes when trades occurred");

		// The position must have actually moved away from flat at some point; a stream of only
		// zero values would mean fills never updated the position.
		IsTrue(positionChanges.Any(v => v != 0), "Expected a non-zero position after fills occurred");
	}

	/// <summary>
	/// Tests that statistics are tracked during backtesting.
	/// </summary>
	[TestMethod]
	[Timeout(180_000, CooperativeCancellation = true)]
	public async Task BacktestTracksStatistics()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

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

		// Independently count order *registrations* exactly as the OrderCount statistic does:
		// OrderCountParameter.New(order) is invoked once per order when it transitions
		// Pending -> Active/Done (Strategy.ProcessOrder => StatisticManager.AddNewOrder), and the
		// statistic is monotonic (never decremented). Replicate that precise condition here: track,
		// per order, that it was first seen Pending and then count it once when it later reaches
		// Active or Done. This is the true contract of the OrderCount statistic and is deterministic
		// for the fixed sample history.
		var pendingSeen = new HashSet<long>();
		var counted = new HashSet<long>();
		var registeredOrderCount = 0;
		strategy.OrderReceived += (sub, order) =>
		{
			var id = order.TransactionId;

			if (order.State == OrderStates.Pending)
				pendingSeen.Add(id);
			else if ((order.State == OrderStates.Active || order.State == OrderStates.Done) &&
				pendingSeen.Contains(id) && counted.Add(id))
			{
				registeredOrderCount++;
			}
		};

		var tcs = new TaskCompletionSource<bool>();
		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify statistics manager is populated
		var statisticManager = strategy.StatisticManager;
		IsNotNull(statisticManager, "StatisticManager should not be null");

		// Note: Parameters is populated by the StatisticManager constructor
		// (StatisticParameterRegistry.CreateAll), so "Parameters.Count > 0" is always true and
		// proves nothing about the run. Instead assert the parameters were actually *updated*
		// by the backtest by cross-checking their values against the strategy's own state.

		var orders = strategy.Orders.ToList();
		var trades = strategy.MyTrades.ToList();

		// The full-month SMA strategy is expected to trade; if it did not, the assertions below
		// would be vacuous, so guard the data first.
		IsTrue(orders.Count > 0, $"Expected orders during backtest, got {orders.Count}");

		object StatValue(StatisticParameterTypes type)
			=> statisticManager.Parameters.First(p => p.Type == type).Value;

		// OrderCount statistic must equal the number of order *registrations* observed over the whole
		// run, NOT strategy.Orders.Count. The statistic is a monotonic cumulative counter incremented
		// once per registration (OrderCountParameter.New), while strategy.Orders is a recycled live
		// window: with the default OrdersKeepTime (1 day) Strategy.RecycleOrders evicts Done orders
		// older than ~1.5 days from the live collection, so over a full month strategy.Orders.Count is
		// far smaller than the all-time registration count. Comparing them is comparing two different
		// quantities. registeredOrderCount counts the same registrations the statistic does.
		var orderCountStat = (int)StatValue(StatisticParameterTypes.OrderCount);
		AreEqual(registeredOrderCount, orderCountStat,
			"OrderCount statistic must equal the number of order registrations observed");

		// Sanity: every order still present in the recycled live collection must have been registered,
		// so the live count can never exceed the cumulative registration count.
		IsTrue(orders.Count <= orderCountStat,
			$"Live order count {orders.Count} cannot exceed cumulative registrations {orderCountStat}");

		// TradeCount statistic counts closing trades (ClosedVolume > 0); it can never exceed the
		// total own trades and must be positive once trades have occurred.
		var tradeCountStat = (int)StatValue(StatisticParameterTypes.TradeCount);
		IsTrue(tradeCountStat >= 0 && tradeCountStat <= trades.Count,
			$"TradeCount statistic {tradeCountStat} must be within [0; own trades {trades.Count}]");

		// Commission statistic must equal the strategy's accumulated commission (0 here - no rules).
		var commissionStat = (decimal)StatValue(StatisticParameterTypes.Commission);
		AreEqual(strategy.Commission ?? 0m, commissionStat, "Commission statistic must match strategy commission");
	}

	/// <summary>
	/// Tests that commission rules are applied during backtesting.
	/// </summary>
	[TestMethod]
	[Timeout(180_000, CooperativeCancellation = true)]
	public async Task BacktestAppliesCommission()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		// Add commission rule: a flat ABSOLUTE 0.001 currency units per trade.
		// CommissionTradeRule.Value is a Unit; with the default Absolute type it is NOT a percent -
		// ICommissionRule.GetValue returns the value as-is (see ICommissionRule.GetValue), so each
		// own trade is charged exactly 0.001, independent of price/volume.
		const decimal perTradeCommission = 0.001m;
		connector.EmulationAdapter.Settings.CommissionRules = [new CommissionTradeRule { Value = perTradeCommission }];

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

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		var tradeCount = strategy.MyTrades.Count();
		Console.WriteLine($"Trades: {tradeCount}, Commission: {strategy.Commission}");

		// The full-month SMA strategy is expected to trade; without trades the commission oracle
		// would be vacuous, so require fills first.
		IsTrue(tradeCount > 0, "Expected trades to occur so commission can be verified");

		// Each own trade is charged a flat absolute commission, so the strategy's accumulated
		// commission must be exactly perTradeCommission * tradeCount. This catches a wrong sign or
		// magnitude in the emulator's commission handling (MarketEmulator -> CommissionManager).
		IsNotNull(strategy.Commission, "Commission should be tracked when trades occurred");
		AreEqual(perTradeCommission * tradeCount, strategy.Commission.Value,
			$"Commission must equal {perTradeCommission} * {tradeCount} trades");
	}

	/// <summary>
	/// Tests that emulation properly finishes and sets IsFinished flag.
	/// </summary>
	[TestMethod]
	public async Task BacktestSetsIsFinished()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6); // Short period for faster test

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

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(1), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify that IsFinished is set properly
		IsTrue(connector.IsFinished, "IsFinished should be true after successful completion");
	}

	/// <summary>
	/// Tests that progress events are raised during backtesting.
	/// </summary>
	[TestMethod]
	[Timeout(180_000, CooperativeCancellation = true)]
	public async Task BacktestRaisesProgressEvents()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var progressValues = new List<int>();
		connector.ProgressChanged += progress =>
		{
			progressValues.Add(progress);
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

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify that progress events were raised
		IsTrue(progressValues.Count > 0, "Expected progress events to be raised");

		// The run reached ChannelStates.Stopped (full completion), so progress MUST reach 100%.
		// A softer threshold (e.g. 50%) would silently pass a regression where ProgressChanged
		// stops short of 100% (e.g. a broken final calculation in HistoryMessageAdapter).
		var maxProgress = progressValues.Max();
		IsTrue(maxProgress >= 100, $"Completed backtest must report 100% progress, got {maxProgress}%");
	}

	/// <summary>
	/// Tests that strategy can be suspended and resumed during backtesting.
	/// </summary>
	[TestMethod]
	public async Task BacktestCanBeSuspendedAndResumed()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

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

		var startedTcs = new TaskCompletionSource<bool>();
		var suspendedTcs = new TaskCompletionSource<bool>();
		var stoppedTcs = new TaskCompletionSource<bool>();

		connector.StateChanged2 += state =>
		{
			switch (state)
			{
				case ChannelStates.Started:
					startedTcs.TrySetResult(true);
					break;
				case ChannelStates.Suspended:
					suspendedTcs.TrySetResult(true);
					break;
				case ChannelStates.Stopped:
					stoppedTcs.TrySetResult(true);
					break;
			}
		};

		// Start strategy before emulation (like BaseOptimizer.StartIteration)
		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		// Wait for started
		await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!startedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not start in time");
		}

		// Suspend
		await connector.SuspendAsync(CancellationToken);

		// Wait for suspended
		await Task.WhenAny(suspendedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!suspendedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not suspend in time");
		}

		AreEqual(ChannelStates.Suspended, connector.State, "State should be Suspended");

		// Resume
		await connector.StartAsync(CancellationToken);

		// Wait for completion
		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(5), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not complete after resume in time");
		}

		connector.IsFinished.AssertTrue("IsFinished should be true after completion");
	}

	/// <summary>
	/// Suspending a backtest must actually halt the historical replay: while suspended no
	/// new data may be delivered. Reproduces the bug where Pause only flips the connector
	/// state to Suspended but the replay keeps feeding candles/ticks.
	/// </summary>
	[TestMethod]
	public async Task BacktestSuspendHaltsReplay()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, Paths.HistoryBeginDate, Paths.HistoryEndDate);

		var dataCount = 0;

		// every candle built from the replayed ticks bumps the counter
		connector.CandleReceived += (sub, candle) => Interlocked.Increment(ref dataCount);

		connector.Subscribe(new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), security)
		{
			MarketData =
			{
				BuildFrom = DataType.Ticks,
				BuildMode = MarketDataBuildModes.Build,
			}
		});

		var startedTcs = new TaskCompletionSource<bool>();
		var suspendedTcs = new TaskCompletionSource<bool>();
		var stoppedTcs = new TaskCompletionSource<bool>();

		connector.StateChanged2 += state =>
		{
			switch (state)
			{
				case ChannelStates.Started:
					startedTcs.TrySetResult(true);
					break;
				case ChannelStates.Suspended:
					suspendedTcs.TrySetResult(true);
					break;
				case ChannelStates.Stopped:
					stoppedTcs.TrySetResult(true);
					break;
			}
		};

		connector.Connect();
		await connector.StartAsync(CancellationToken);

		await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));
		IsTrue(startedTcs.Task.IsCompleted, "Backtest did not start in time");

		// run until some data is produced so we suspend in the middle of the replay
		for (var i = 0; i < 200 && Volatile.Read(ref dataCount) == 0; i++)
			await Task.Delay(50, CancellationToken);

		IsTrue(Volatile.Read(ref dataCount) > 0, "No data received before suspend");

		await connector.SuspendAsync(CancellationToken);

		await Task.WhenAny(suspendedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));
		IsTrue(suspendedTcs.Task.IsCompleted, "Backtest did not suspend in time");
		AreEqual(ChannelStates.Suspended, connector.State, "State should be Suspended");

		// let any in-flight dispatch settle, then snapshot
		await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken);
		var countAtSuspend = Volatile.Read(ref dataCount);

		// stay suspended for a real-time window: nothing must change
		await Task.Delay(TimeSpan.FromSeconds(2), CancellationToken);
		var countAfter = Volatile.Read(ref dataCount);

		AreEqual(countAtSuspend, countAfter, $"No data must be delivered while suspended (before={countAtSuspend}, after={countAfter})");

		// resume and run to completion
		await connector.StartAsync(CancellationToken);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(5), CancellationToken));
		IsTrue(stoppedTcs.Task.IsCompleted, "Backtest did not complete after resume");
		connector.IsFinished.AssertTrue("IsFinished should be true after completion");
	}

	// Same SMA crossover logic as the WPF history-testing sample: subscribe candles built from the
	// chosen source (storage candles / ticks / level1), and on a crossover place a limit order at
	// the candle close.
	private class WpfSmaStrategy : Strategy
	{
		private bool? _isShortLessThenLong;

		public int LongSma { get; set; } = 80;
		public int ShortSma { get; set; } = 10;
		public DataType CandleType { get; set; } = TimeSpan.FromMinutes(1).TimeFrame();
		public DataType BuildFrom { get; set; }
		public Level1Fields? BuildField { get; set; }

		protected override void OnReseted()
		{
			base.OnReseted();
			_isShortLessThenLong = null;
		}

		protected override void OnStarted2(DateTime time)
		{
			base.OnStarted2(time);

			var subscription = new Subscription(CandleType, Security)
			{
				MarketData =
				{
					IsFinishedOnly = true,
					BuildFrom = BuildFrom,
					BuildMode = BuildFrom is null ? MarketDataBuildModes.LoadAndBuild : MarketDataBuildModes.Build,
					BuildField = BuildField,
				}
			};

			var longSma = new SMA { Length = LongSma };
			var shortSma = new SMA { Length = ShortSma };

			SubscribeCandles(subscription)
				.Bind(longSma, shortSma, OnProcess)
				.Start();
		}

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
				var price = candle.ClosePrice;

				if (direction == Sides.Buy)
					BuyLimit(price, volume);
				else
					SellLimit(price, volume);

				_isShortLessThenLong = isShortLessThenLong;
			}
		}
	}

	/// <summary>
	/// Runs the WPF-sample SMA strategy over the first 7 days of real history in three modes
	/// (candles, ticks, level1/spread) and verifies no own trade fills outside the candle it
	/// belongs to - i.e. no trade gets drawn off the candles, which is what the stale synthesized
	/// order book used to cause.
	/// </summary>
	[TestMethod]
	public async Task BacktestTradesStayWithinCandlesAllModes()
	{
		if (SkipIfNoHistoryData()) return;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(7);
		var tf = TimeSpan.FromMinutes(1);

		var modes = new (string name, DataType buildFrom, Level1Fields? buildField)[]
		{
			("Candles", null, null),
			("Ticks", DataType.Ticks, null),
			("Spread", DataType.Level1, Level1Fields.SpreadMiddle),
		};

		var outside = new Dictionary<string, int>();

		foreach (var (name, buildFrom, buildField) in modes)
		{
			var security = CreateTestSecurity();
			security.PriceStep = 0.01m;
			var portfolio = CreateTestPortfolio();

			var secProvider = new CollectionSecurityProvider([security]);
			var pfProvider = new CollectionPortfolioProvider([portfolio]);
			var storageRegistry = GetHistoryStorage();

			using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

			var candles = new List<(DateTimeOffset open, decimal low, decimal high)>();
			var trades = new List<(DateTimeOffset time, decimal price, decimal limit, Sides side)>();

			connector.CandleReceived += (s, c) =>
			{
				if (c.State == CandleStates.Finished)
					candles.Add((c.OpenTime, c.LowPrice, c.HighPrice));
			};
			connector.OwnTradeReceived += (s, t) => trades.Add((t.Trade.ServerTime, t.Trade.Price, t.Order.Price, t.Order.Side));

			var strategy = new WpfSmaStrategy
			{
				Connector = connector,
				Security = security,
				Portfolio = portfolio,
				Volume = 1,
				CandleType = tf.TimeFrame(),
				LongSma = 80,
				ShortSma = 10,
				BuildFrom = buildFrom,
				BuildField = buildField,
			};

			var stoppedTcs = new TaskCompletionSource<bool>();
			connector.StateChanged2 += state =>
			{
				if (state == ChannelStates.Stopped)
					stoppedTcs.TrySetResult(true);
			};

			strategy.Start();
			connector.Connect();
			await connector.StartAsync(CancellationToken);

			await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(5), CancellationToken));
			IsTrue(stoppedTcs.Task.IsCompleted, $"[{name}] backtest did not complete in time");

			strategy.Stop();

			var outOfCandle = 0;
			var maxDev = 0m;
			var examples = new List<string>();

			var window = TimeSpan.FromMinutes(2);

			foreach (var (time, price, limit, side) in trades)
			{
				// Use the price range over a small window around the trade time. A limit order acts
				// on the finished candle M but the own trade is stamped at the M+1 boundary, so a
				// strict single-candle match would flag a normal fill across that boundary; the
				// window only catches fills that are genuinely off the market.
				var lo = decimal.MaxValue;
				var hi = decimal.MinValue;

				foreach (var c in candles)
				{
					if (c.open + tf <= time - window || c.open >= time + window)
						continue;

					if (c.low < lo) lo = c.low;
					if (c.high > hi) hi = c.high;
				}

				if (hi < lo)
					continue;

				var dev = price < lo ? lo - price : price > hi ? price - hi : 0m;

				// a fill more than 0.5% of price outside the windowed range is clearly off the market
				if (dev > price * 0.005m)
				{
					outOfCandle++;
					if (dev > maxDev) maxDev = dev;
					if (examples.Count < 5)
						examples.Add($"{side} fill={price} limit={limit} range=[{lo};{hi}] dev={dev}");
				}
			}

			outside[name] = outOfCandle;

			Console.WriteLine($"[{name}] candles={candles.Count} trades={trades.Count} outOfCandle={outOfCandle} maxDev={maxDev}");
			foreach (var e in examples)
				Console.WriteLine($"    [{name}] {e}");
		}

		foreach (var kv in outside)
			AreEqual(0, kv.Value, $"[{kv.Key}] {kv.Value} trade(s) filled outside their candle range");
	}

	/// <summary>
	/// Tests that multiple securities can be backtested.
	/// </summary>
	[TestMethod]
	[Timeout(180_000, CooperativeCancellation = true)]
	public async Task BacktestWithMultipleSecurities()
	{
		if (SkipIfNoHistoryData()) return;

		var security1 = CreateTestSecurity();
		var security2 = new Security { Id = Paths.HistoryDefaultSecurity2 };

		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security1, security2]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(14);

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		// Track finished candles per security so we can prove BOTH instruments were actually
		// replayed - not just that the strategies were stopped (which the handler does itself).
		var secId1 = security1.Id.ToSecurityId();
		var secId2 = security2.Id.ToSecurityId();
		var candles1 = 0;
		var candles2 = 0;

		connector.CandleReceived += (sub, candle) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			if (candle.SecurityId == secId1)
				Interlocked.Increment(ref candles1);
			else if (candle.SecurityId == secId2)
				Interlocked.Increment(ref candles2);
		};

		var strategy1 = new SmaStrategy
		{
			Connector = connector,
			Security = security1,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var strategy2 = new SmaStrategy
		{
			Connector = connector,
			Security = security2,
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
			{
				strategy1.Stop();
				strategy2.Stop();
				tcs.TrySetResult(true);
			}
		};

		// Start strategies before emulation (like BaseOptimizer.StartIteration)
		strategy1.WaitRulesOnStop = false;
		strategy1.Reset();
		strategy2.WaitRulesOnStop = false;
		strategy2.Reset();
		strategy1.Start();
		strategy2.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		Console.WriteLine($"Candles: {security1.Id}={candles1}, {security2.Id}={candles2}");

		// Both strategies should have completed
		strategy1.ProcessState.AssertEqual(ProcessStates.Stopped, "Strategy 1 should be stopped");
		strategy2.ProcessState.AssertEqual(ProcessStates.Stopped, "Strategy 2 should be stopped");

		// The real point of a multi-security backtest: data for BOTH securities must be replayed.
		// The previous test would pass even if security2 stayed completely silent (no subscription,
		// no data) because the stop handler stops the strategies itself.
		IsTrue(candles1 > 0, $"Expected finished candles for {security1.Id}, got {candles1}");
		IsTrue(candles2 > 0, $"Expected finished candles for {security2.Id}, got {candles2}");
	}

	/// <summary>
	/// Tests that event times are monotonically increasing within each event type during backtesting.
	/// Checks:
	/// - Trade times (ServerTime from execution message) are monotonic
	/// - PnL times are monotonic
	/// - Order ServerTime is monotonic
	/// Note: Position events are NOT checked because they include both fill-based changes
	/// and market-price-driven value updates from multiple emulator sources (stored candle
	/// processing, order matching, ProcessTime). These sources output position changes at
	/// different emulated times that aren't guaranteed to arrive in chronological order
	/// (e.g., a pending order fill at T2 can generate a position update after a candle at T3).
	/// </summary>
	[TestMethod]
	public async Task BacktestEventTimesAreMonotonicallyIncreasing()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

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

		// Track times per event type
		DateTime? lastOrderTime = null;
		DateTime? lastTradeTime = null;
		DateTime? lastPnLTime = null;
		DateTimeOffset? lastCandleTime = null;

		var tradeTimeErrors = new List<string>();
		var pnlTimeErrors = new List<string>();
		var orderTimeErrors = new List<string>();
		var candleTimeErrors = new List<string>();

		var orderCount = 0;
		var tradeCount = 0;
		var pnlCount = 0;
		var candleCount = 0;

		strategy.OrderReceived += (sub, order) =>
		{
			// Use ServerTime (set by emulator on each state change) instead of
			// Time (set once at registration). ServerTime reflects when the emulator
			// actually processed the state transition.
			var time = order.ServerTime != default ? order.ServerTime : order.Time;
			orderCount++;

			if (lastOrderTime.HasValue && time < lastOrderTime.Value)
			{
				orderTimeErrors.Add($"Order[{orderCount}] ServerTime decreased: {lastOrderTime.Value:O} -> {time:O} (TxId={order.TransactionId}, State={order.State})");
			}
			lastOrderTime = time;
		};

		strategy.OwnTradeReceived += (sub, trade) =>
		{
			var time = trade.Trade.ServerTime;
			tradeCount++;

			if (lastTradeTime.HasValue && time < lastTradeTime.Value)
			{
				tradeTimeErrors.Add($"Trade[{tradeCount}] time decreased: {lastTradeTime.Value:O} -> {time:O}");
			}
			lastTradeTime = time;
		};

		connector.CandleReceived += (sub, candle) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			var time = candle.OpenTime;
			candleCount++;

			if (lastCandleTime.HasValue && time < lastCandleTime.Value)
			{
				candleTimeErrors.Add($"Candle[{candleCount}] OpenTime decreased: {lastCandleTime.Value:O} -> {time:O}");
			}
			lastCandleTime = time;
		};

		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			pnlCount++;

			if (lastPnLTime.HasValue && time < lastPnLTime.Value)
			{
				pnlTimeErrors.Add($"PnL[{pnlCount}] time decreased: {lastPnLTime.Value:O} -> {time:O}");
			}
			lastPnLTime = time;
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

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		var totalEvents = orderCount + tradeCount + pnlCount + candleCount;
		IsTrue(totalEvents > 0, "Expected to receive some events");

		Console.WriteLine($"Events: Orders={orderCount}, Trades={tradeCount}, PnL={pnlCount}, Candles={candleCount}");

		// Check within-type monotonicity for orders, trades, PnL, and candles
		var allErrors = new List<string>();
		allErrors.AddRange(orderTimeErrors);
		allErrors.AddRange(tradeTimeErrors);
		allErrors.AddRange(pnlTimeErrors);
		allErrors.AddRange(candleTimeErrors);

		if (allErrors.Count > 0)
		{
			Fail($"Time ordering violations detected:\n{allErrors.Take(20).JoinN()}");
		}
	}

	/// <summary>
	/// Tests that backtesting with VerifyMode enabled completes successfully (no violations detected in normal flow).
	/// </summary>
	[TestMethod]
	public async Task BacktestWithVerifyModeCompletes()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(7); // Short period

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime, verifyMode: true);

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
		Exception caughtException = null;

		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
				tcs.TrySetResult(true);
		};

		connector.Error += ex =>
		{
			caughtException = ex;
			tcs.TrySetResult(false);
		};

		strategy.WaitRulesOnStop = false;
		strategy.Reset();
		strategy.Start();
		connector.Connect();
		await connector.StartAsync(CancellationToken);

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(1), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Should complete without VerifyMode throwing
		IsNull(caughtException, $"No exception expected with VerifyMode, but got: {caughtException?.Message}");
		IsTrue(connector.IsFinished, "Backtest should be marked finished");
	}

	/// <summary>
	/// Tests that two emulation runs with same deterministic settings produce identical
	/// output messages from the <see cref="MarketEmulator"/>.
	/// Verifies emulator-level determinism: given identical input (same history data,
	/// same random seed, same initial IDs), the emulator produces bit-identical output.
	/// Does NOT include strategy feedback (order placement) because the full Connector
	/// adapter chain contains DateTime.UtcNow calls in subscription management that
	/// introduce unavoidable non-determinism in transaction IDs.
	/// </summary>
	[TestMethod]
	public async Task BacktestWithSameRandomSeedProducesIdenticalMessages()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(3); // Short period for faster test

		const int randomSeed = 42;

		// Run emulation and collect emulator output messages.
		// Uses deterministic connector (PassThroughMessageChannel) for synchronous processing.
		async Task<List<Message>> RunEmulation(bool verifyMode)
		{
			var messages = new List<Message>();

			using var connector = CreateDeterministicConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime, verifyMode);

			var emulator = (MarketEmulator)connector.EmulationAdapter.Emulator;
			emulator.RandomProvider = new DefaultRandomProvider(randomSeed);

			// Set deterministic initial IDs — EmulationMessageAdapter constructor
			// seeds these from DateTime.UtcNow.Ticks which changes between runs.
			// MarketEmulator.Reset() applies these to OrderIdGenerator/TradeIdGenerator.
			connector.EmulationAdapter.Settings.InitialOrderId = 100;
			connector.EmulationAdapter.Settings.InitialTradeId = 100;

			// Capture emulator output messages
			emulator.NewOutMessageAsync += (msg, ct) =>
			{
				// Skip non-deterministic messages
				if (msg is TimeMessage or ResetMessage)
					return default;

				messages.Add(msg.TypedClone());
				return default;
			};

			var tcs = new TaskCompletionSource<bool>();
			connector.StateChanged2 += state =>
			{
				if (state == ChannelStates.Stopped)
					tcs.TrySetResult(true);
			};

			// Subscribe to candles to trigger history data replay through emulator
			connector.Connected += () =>
			{
				connector.Subscribe(new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), security));
			};

			connector.Connect();
			await connector.StartAsync(CancellationToken);

			var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(1), CancellationToken));

			if (completed != tcs.Task)
				Fail("Emulation did not complete in time");

			return messages;
		}

		// Run twice with same seed
		var messages1 = await RunEmulation(verifyMode: true);
		var messages2 = await RunEmulation(verifyMode: true);

		// Compare using existing CheckEqual method
		messages1.Count.AssertEqual(messages2.Count);

		for (var i = 0; i < messages1.Count; i++)
			Helper.CheckEqual(messages1[i], messages2[i]);
	}

	/// <summary>
	/// Tests that VerifyMode does not change emulation results (same random seed).
	/// Verifies emulator-level determinism: given identical input, VerifyMode=true
	/// and VerifyMode=false produce bit-identical output.
	/// Does NOT include strategy feedback (order placement) because the full Connector
	/// adapter chain contains DateTime.UtcNow calls in subscription management that
	/// introduce unavoidable non-determinism in transaction IDs.
	/// </summary>
	[TestMethod]
	public async Task BacktestVerifyModeDoesNotAffectResults()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(3); // Short period

		const int randomSeed = 42;

		// Run emulation and collect emulator output messages.
		// Uses deterministic connector (PassThroughMessageChannel) for synchronous processing.
		async Task<List<Message>> RunEmulation(bool verifyMode)
		{
			var messages = new List<Message>();

			using var connector = CreateDeterministicConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime, verifyMode);

			var emulator = (MarketEmulator)connector.EmulationAdapter.Emulator;
			emulator.RandomProvider = new DefaultRandomProvider(randomSeed);

			// Set deterministic initial IDs — EmulationMessageAdapter constructor
			// seeds these from DateTime.UtcNow.Ticks which changes between runs.
			// MarketEmulator.Reset() applies these to OrderIdGenerator/TradeIdGenerator.
			connector.EmulationAdapter.Settings.InitialOrderId = 100;
			connector.EmulationAdapter.Settings.InitialTradeId = 100;

			// Capture emulator output messages
			emulator.NewOutMessageAsync += (msg, ct) =>
			{
				// Skip non-deterministic messages
				if (msg is TimeMessage or ResetMessage)
					return default;

				messages.Add(msg.TypedClone());
				return default;
			};

			var tcs = new TaskCompletionSource<bool>();
			connector.StateChanged2 += state =>
			{
				if (state == ChannelStates.Stopped)
					tcs.TrySetResult(true);
			};

			// Subscribe to candles to trigger history data replay through emulator
			connector.Connected += () =>
			{
				connector.Subscribe(new Subscription(TimeSpan.FromMinutes(1).TimeFrame(), security));
			};

			connector.Connect();
			await connector.StartAsync(CancellationToken);

			var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(1), CancellationToken));

			if (completed != tcs.Task)
				Fail("Emulation did not complete in time");

			return messages;
		}

		// Run with VerifyMode on and off
		var messagesWithVerify = await RunEmulation(verifyMode: true);
		var messagesWithoutVerify = await RunEmulation(verifyMode: false);

		// Compare using existing CheckEqual method
		messagesWithVerify.Count.AssertEqual(messagesWithoutVerify.Count);

		for (var i = 0; i < messagesWithVerify.Count; i++)
			Helper.CheckEqual(messagesWithVerify[i], messagesWithoutVerify[i]);
	}

	/// <summary>
	/// Tests MarketEmulator in isolation - without history, without channels.
	/// Verifies that order registration produces ExecutionMessage.
	/// </summary>
	[TestMethod]
	public async Task MarketEmulator_OrderRegistration_ProducesExecutionMessage()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator())
		{
			Settings = { MatchOnTouch = true }
		};

		var messages = new List<Message>();
		emulator.NewOutMessageAsync += (msg, ct) =>
		{
			messages.Add(msg);
			return default;
		};

		// Cast to interface for SendInMessageAsync
		var adapter = (IMarketEmulator)emulator;

		// Connect
		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		// Set security info with initial price
		var now = DateTime.UtcNow;
		await adapter.SendInMessageAsync(new Level1ChangeMessage
		{
			SecurityId = security.ToSecurityId(),
			ServerTime = now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m)
		.TryAdd(Level1Fields.BestBidPrice, 99m)
		.TryAdd(Level1Fields.BestAskPrice, 101m), CancellationToken);

		// Register order
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = emulator.TransactionIdGenerator.GetNextId(),
			SecurityId = security.ToSecurityId(),
			PortfolioName = portfolio.Name,
			Side = Sides.Buy,
			Price = 100m,
			Volume = 1,
			OrderType = OrderTypes.Limit,
		};

		await adapter.SendInMessageAsync(orderMsg, CancellationToken);

		// Check that we got an ExecutionMessage for the order
		var execMsgs = messages.OfType<ExecutionMessage>()
			.Where(e => e.TransactionId == orderMsg.TransactionId || e.OriginalTransactionId == orderMsg.TransactionId)
			.ToList();

		Console.WriteLine($"Total messages: {messages.Count}");
		Console.WriteLine($"ExecutionMessages for order: {execMsgs.Count}");
		foreach (var e in execMsgs)
			Console.WriteLine($"  ExecMsg: HasOrderInfo={e.HasOrderInfo}, HasTradeInfo={e.HasTradeInfo}, State={e.OrderState}, OrigTransId={e.OriginalTransactionId}");

		execMsgs.Count.AssertGreater(0, "Expected at least one ExecutionMessage for the order");
		execMsgs.Count(e => e.HasOrderInfo).AssertEqual(1, "Expected ExecutionMessage with order info");
	}

	/// <summary>
	/// Tests SubscriptionOnlineMessageAdapter + MarketEmulator chain.
	/// Verifies that ExecutionMessage gets subscription IDs after OrderStatus subscription.
	/// </summary>
	[TestMethod]
	public async Task SubscriptionOnlineAdapter_SetsSubscriptionIdsOnExecutionMessage()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator())
		{
			Settings = { MatchOnTouch = true }
		};

		// Wrap emulator with adapter then SubscriptionOnlineMessageAdapter
		var emulatorAdapter = new MarketEmulatorAdapter(emulator, new IncrementalIdGenerator());
		var subscriptionAdapter = new SubscriptionOnlineMessageAdapter(emulatorAdapter);

		var messages = new List<Message>();
		subscriptionAdapter.NewOutMessageAsync += (msg, ct) =>
		{
			messages.Add(msg);
			return default;
		};

		var adapter = (IMessageAdapter)subscriptionAdapter;

		// Connect
		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		// Subscribe to OrderStatus (transactions)
		var orderStatusTransId = emulator.TransactionIdGenerator.GetNextId();
		await adapter.SendInMessageAsync(new OrderStatusMessage
		{
			TransactionId = orderStatusTransId,
			IsSubscribe = true,
		}, CancellationToken);

		// Set security info with initial price
		var now = DateTime.UtcNow;
		await adapter.SendInMessageAsync(new Level1ChangeMessage
		{
			SecurityId = security.ToSecurityId(),
			ServerTime = now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m)
		.TryAdd(Level1Fields.BestBidPrice, 99m)
		.TryAdd(Level1Fields.BestAskPrice, 101m), CancellationToken);

		// Register order
		var orderTransId = emulator.TransactionIdGenerator.GetNextId();
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = orderTransId,
			SecurityId = security.ToSecurityId(),
			PortfolioName = portfolio.Name,
			Side = Sides.Buy,
			Price = 100m,
			Volume = 1,
			OrderType = OrderTypes.Limit,
		};

		await adapter.SendInMessageAsync(orderMsg, CancellationToken);

		// Check that ExecutionMessage has subscription IDs
		var execMsgs = messages.OfType<ExecutionMessage>()
			.Where(e => e.OriginalTransactionId == orderTransId)
			.ToList();

		Console.WriteLine($"Total messages: {messages.Count}");
		Console.WriteLine($"ExecutionMessages for order: {execMsgs.Count}");
		foreach (var e in execMsgs)
		{
			var ids = e.GetSubscriptionIds();
			Console.WriteLine($"  ExecMsg: HasOrderInfo={e.HasOrderInfo}, State={e.OrderState}, SubscriptionIds=[{ids.Select(x => $"{x}").JoinComma()}]");
		}

		IsTrue(execMsgs.Count > 0, "Expected at least one ExecutionMessage for the order");

		// The key assertion: ExecutionMessage should have subscription IDs set
		var execWithIds = execMsgs.Where(e => e.GetSubscriptionIds().Length > 0).ToList();
		Console.WriteLine($"ExecutionMessages with subscription IDs: {execWithIds.Count}");
		IsTrue(execWithIds.Count > 0, "Expected ExecutionMessage to have subscription IDs from OrderStatus subscription");
	}

	/// <summary>
	/// Tests EmulationMessageAdapter with isEmulationOnly=true.
	/// Verifies that ExecutionMessage gets subscription IDs through the full internal chain.
	/// </summary>
	[TestMethod]
	public async Task EmulationMessageAdapter_EmulationOnly_SetsSubscriptionIds()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		// Create a PassThrough adapter as inner adapter
		var innerAdapter = new PassThroughMessageAdapter(new IncrementalIdGenerator());

		// Create EmulationMessageAdapter with isEmulationOnly=true
		// Use PassThroughMessageChannel for simpler synchronous testing
		var channel = new PassThroughMessageChannel();
		var emulationAdapter = new EmulationMessageAdapter(
			innerAdapter,
			channel,
			isEmulationOnly: true,
			secProvider,
			pfProvider,
			exchangeProvider)
		{
			OwnInnerAdapter = false // Test without outer adapter chain
		};

		emulationAdapter.Settings.MatchOnTouch = true;

		var messages = new List<Message>();
		emulationAdapter.NewOutMessageAsync += (msg, ct) =>
		{
			messages.Add(msg);
			return default;
		};

		var adapter = (IMessageAdapter)emulationAdapter;

		// Use emulator's transaction ID generator to avoid conflicts
		var idGen = emulationAdapter.TransactionIdGenerator;

		// Connect
		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		// Subscribe to OrderStatus (transactions) - use ID starting from 1000 to avoid conflicts
		var orderStatusTransId = 1000L;
		await adapter.SendInMessageAsync(new OrderStatusMessage
		{
			TransactionId = orderStatusTransId,
			IsSubscribe = true,
		}, CancellationToken);

		// Set security info with initial price (goes to emulator)
		var now = DateTime.UtcNow;
		await adapter.SendInMessageAsync(new Level1ChangeMessage
		{
			SecurityId = security.ToSecurityId(),
			ServerTime = now,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m)
		.TryAdd(Level1Fields.BestBidPrice, 99m)
		.TryAdd(Level1Fields.BestAskPrice, 101m), CancellationToken);

		messages.Clear(); // Clear connect/subscription messages

		// Register order - use explicit high ID to avoid conflicts
		var orderTransId = 2000L;
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = orderTransId,
			SecurityId = security.ToSecurityId(),
			PortfolioName = portfolio.Name,
			Side = Sides.Buy,
			Price = 100m,
			Volume = 1,
			OrderType = OrderTypes.Limit,
		};

		await adapter.SendInMessageAsync(orderMsg, CancellationToken);

		// Check that ExecutionMessage has subscription IDs
		var execMsgs = messages.OfType<ExecutionMessage>()
			.Where(e => e.OriginalTransactionId == orderTransId)
			.ToList();

		Console.WriteLine($"Total messages: {messages.Count}");
		Console.WriteLine($"ExecutionMessages for order: {execMsgs.Count}");
		foreach (var e in execMsgs)
		{
			var ids = e.GetSubscriptionIds();
			Console.WriteLine($"  ExecMsg: HasOrderInfo={e.HasOrderInfo}, State={e.OrderState}, SubscriptionIds=[{ids.Select(x => $"{x}").JoinComma()}]");
		}

		IsTrue(execMsgs.Count > 0, "Expected at least one ExecutionMessage for the order");

		// The key assertion: ExecutionMessage should have subscription IDs set
		var execWithIds = execMsgs.Where(e => e.GetSubscriptionIds().Length > 0).ToList();
		Console.WriteLine($"ExecutionMessages with subscription IDs: {execWithIds.Count}");
		IsTrue(execWithIds.Count > 0, "Expected ExecutionMessage to have subscription IDs from OrderStatus subscription");
	}

	/// <summary>
	/// Mock adapter that behaves like HistoryMessageAdapter - responds to subscriptions but doesn't echo.
	/// </summary>
	private class MockHistoryAdapter : MessageAdapter
	{
		public List<string> ReceivedMessages { get; } = [];

		public MockHistoryAdapter() : base(new IncrementalIdGenerator())
		{
			this.AddTransactionalSupport();
			this.AddMarketDataSupport();
			this.AddSupportedMessage(MessageTypes.SecurityLookup, null);
			this.AddSupportedMessage(MessageTypes.PortfolioLookup, null);
			this.AddSupportedMessage(MessageTypes.OrderStatus, null);
			this.AddSupportedMessage(MessageTypes.MarketData, null);
			this.AddSupportedMessage(MessageTypes.OrderRegister, null);
			this.AddSupportedMessage(MessageTypes.OrderCancel, null);
			this.AddSupportedMarketDataType(DataType.Level1);
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.MarketDepth);
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Transactions || dataType == DataType.PositionChanges || base.IsAllDownloadingSupported(dataType);

		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			ReceivedMessages.Add($"{message.Type}");
			Console.WriteLine($"[MockHistoryAdapter IN] {message.Type}");

			// Respond to subscription messages like HistoryMessageAdapter would
			if (message is ISubscriptionMessage subscrMsg && subscrMsg.IsSubscribe)
			{
				Console.WriteLine($"  -> Responding to subscription TransId={subscrMsg.TransactionId}");
				// Send response
				_ = SendOutMessageAsync(subscrMsg.TransactionId.CreateSubscriptionResponse(), cancellationToken);
				_ = SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = subscrMsg.TransactionId }, cancellationToken);
			}
			else if (message.Type == MessageTypes.Connect)
			{
				_ = SendOutMessageAsync(new ConnectMessage(), cancellationToken);
			}
			// Other messages - just ignore (history adapter would read from storage)
			return default;
		}
	}

	/// <summary>
	/// Test EmulationMessageAdapter + BasketMessageAdapter chain.
	/// This is closer to real backtest setup.
	/// </summary>
	[TestMethod]
	public async Task EmulationMessageAdapter_WithBasket_TraceIDs()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var innerAdapter = new MockHistoryAdapter();

		// Use PassThroughMessageChannel for sync behavior
		var channel = new PassThroughMessageChannel();

		var emulationAdapter = new EmulationMessageAdapter(
			innerAdapter,
			channel,
			isEmulationOnly: true,
			secProvider,
			pfProvider,
			exchangeProvider)
		{
			OwnInnerAdapter = true
		};

		emulationAdapter.Settings.MatchOnTouch = true;

		// Create BasketMessageAdapter and add EmulationMessageAdapter to it
		var idGen = new IncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(exchangeProvider);

		var cs = new AdapterConnectionState();
		var cm = new AdapterConnectionManager(cs);
		var ps = new PendingMessageState();
		var sr = new SubscriptionRoutingState();
		var pcm = new ParentChildMap();
		var or = new OrderRoutingState();

		var routingManager = new BasketRoutingManager(
			cs, cm, ps, sr, pcm, or,
			a => a, candleBuilderProvider, () => false, idGen);

		var basketAdapter = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null,
			null,
			routingManager);

		basketAdapter.IgnoreExtraAdapters = true;
		basketAdapter.LatencyManager = null;
		basketAdapter.SlippageManager = null;
		basketAdapter.CommissionManager = null;

		basketAdapter.InnerAdapters.Add(emulationAdapter);
		basketAdapter.ApplyHeartbeat(emulationAdapter, false);

		// Track ALL messages at different levels to see the flow
		var atEmulation = new List<(string type, long[] ids)>();
		var atBasket = new List<(string type, long[] ids)>();
		var intoEmulation = new List<string>();

		emulationAdapter.NewOutMessageAsync += (msg, ct) =>
		{
			var ids = (msg as ISubscriptionIdMessage)?.GetSubscriptionIds() ?? [];
			Console.WriteLine($"[EmulationAdapter OUT] {msg.Type} SubIds=[{ids.Select(x => $"{x}").JoinComma()}]");

			if (msg is ExecutionMessage exec && exec.HasOrderInfo)
			{
				atEmulation.Add(($"Exec OrigTransId={exec.OriginalTransactionId}", ids));
			}
			else if (msg is SubscriptionResponseMessage resp)
			{
				Console.WriteLine($"  -> SubscriptionResponse: OrigTransId={resp.OriginalTransactionId}");
			}
			else if (msg is SubscriptionOnlineMessage online)
			{
				Console.WriteLine($"  -> SubscriptionOnline: OrigTransId={online.OriginalTransactionId}");
			}
			return default;
		};

		basketAdapter.NewOutMessageAsync += async (msg, ct) =>
		{
			// Re-process loopback messages (simulates Connector behavior)
			if (msg.IsBack())
			{
				Console.WriteLine($"[BasketAdapter BACK] {msg.Type}");
				await ((IMessageTransport)basketAdapter).SendInMessageAsync(msg, ct);
				return;
			}

			if (msg is ExecutionMessage exec && exec.HasOrderInfo)
			{
				var ids = exec.GetSubscriptionIds();
				atBasket.Add(($"Exec OrigTransId={exec.OriginalTransactionId}", ids));
				Console.WriteLine($"[BasketAdapter] Execution: OrigTransId={exec.OriginalTransactionId} SubIds=[{ids.Select(x => $"{x}").JoinComma()}]");
			}
		};

		var adapter = (IMessageAdapter)basketAdapter;

		Console.WriteLine("=== Connect ===");
		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		Console.WriteLine("\n=== OrderStatus subscription ===");
		await adapter.SendInMessageAsync(new OrderStatusMessage { TransactionId = 1000L, IsSubscribe = true }, CancellationToken);

		Console.WriteLine("\n=== Level1 ===");
		await adapter.SendInMessageAsync(new Level1ChangeMessage
		{
			SecurityId = security.ToSecurityId(),
			ServerTime = DateTime.UtcNow,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m)
		.TryAdd(Level1Fields.BestBidPrice, 99m)
		.TryAdd(Level1Fields.BestAskPrice, 101m), CancellationToken);

		Console.WriteLine("\n=== Register order ===");
		await adapter.SendInMessageAsync(new OrderRegisterMessage
		{
			TransactionId = 2000L,
			SecurityId = security.ToSecurityId(),
			PortfolioName = portfolio.Name,
			Side = Sides.Buy,
			Price = 100m,
			Volume = 1,
			OrderType = OrderTypes.Limit,
		}, CancellationToken);

		Console.WriteLine("\n=== Summary ===");
		Console.WriteLine($"At EmulationAdapter: {atEmulation.Count} executions");
		foreach (var (type, ids) in atEmulation)
			Console.WriteLine($"  {type} SubIds=[{ids.Select(x => $"{x}").JoinComma()}]");

		Console.WriteLine($"At BasketAdapter: {atBasket.Count} executions");
		foreach (var (type, ids) in atBasket)
			Console.WriteLine($"  {type} SubIds=[{ids.Select(x => $"{x}").JoinComma()}]");

		var emulationWithIds = atEmulation.Count(x => x.ids.Length > 0);
		var basketWithIds = atBasket.Count(x => x.ids.Length > 0);

		Console.WriteLine($"\nEmulation: {emulationWithIds}/{atEmulation.Count} with IDs");
		Console.WriteLine($"Basket: {basketWithIds}/{atBasket.Count} with IDs");

		IsTrue(emulationWithIds > 0, "Expected ExecutionMessage with IDs at EmulationAdapter output");
		IsTrue(basketWithIds > 0, "Expected ExecutionMessage with IDs at BasketAdapter output");
	}

	/// <summary>
	/// Regression test: verifies all backtest message types are routed correctly through the full adapter chain.
	/// If any message type doesn't reach the emulator or response doesn't come back - test fails immediately.
	/// </summary>
	[TestMethod]
	public async Task BacktestMessageRouting_AllMessageTypesReachEmulator()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		// Track what messages the emulator receives and sends
		var receivedByEmulator = new List<MessageTypes>();
		var sentByEmulator = new List<MessageTypes>();

		var innerAdapter = new RoutingTestAdapter(receivedByEmulator, sentByEmulator);

		var channel = new PassThroughMessageChannel();
		var emulationAdapter = new EmulationMessageAdapter(
			innerAdapter, channel, isEmulationOnly: true,
			secProvider, pfProvider, exchangeProvider)
		{
			OwnInnerAdapter = true
		};

		// Setup BasketMessageAdapter (same as real backtest)
		var idGen = new IncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(exchangeProvider);
		var cs = new AdapterConnectionState();
		var cm = new AdapterConnectionManager(cs);
		var ps = new PendingMessageState();
		var sr = new SubscriptionRoutingState();
		var pcm = new ParentChildMap();
		var or = new OrderRoutingState();
		var routingManager = new BasketRoutingManager(cs, cm, ps, sr, pcm, or, a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(idGen, candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null, null, routingManager);

		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.InnerAdapters.Add(emulationAdapter);
		basket.ApplyHeartbeat(emulationAdapter, false);

		// Track what we receive back
		var receivedFromBasket = new List<MessageTypes>();
		basket.NewOutMessageAsync += async (msg, ct) =>
		{
			if (msg.IsBack())
			{
				await ((IMessageTransport)basket).SendInMessageAsync(msg, ct);
				return;
			}
			receivedFromBasket.Add(msg.Type);
		};

		var adapter = (IMessageAdapter)basket;

		// === Test all message types used in backtest ===

		// 1. Connect
		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		AssertReceived(receivedByEmulator, MessageTypes.Connect, "Connect must reach emulator");

		// 2. OrderStatus subscription (THE BUG WE FIXED!)
		await adapter.SendInMessageAsync(new OrderStatusMessage { TransactionId = 100, IsSubscribe = true }, CancellationToken);
		AssertReceived(receivedByEmulator, MessageTypes.OrderStatus, "OrderStatus must reach emulator for order tracking");

		// 3. PortfolioLookup
		await adapter.SendInMessageAsync(new PortfolioLookupMessage { TransactionId = 101, IsSubscribe = true }, CancellationToken);
		AssertReceived(receivedByEmulator, MessageTypes.PortfolioLookup, "PortfolioLookup must reach emulator");

		// 4. SecurityLookup
		await adapter.SendInMessageAsync(new SecurityLookupMessage { TransactionId = 102 }, CancellationToken);
		AssertReceived(receivedByEmulator, MessageTypes.SecurityLookup, "SecurityLookup must reach emulator");

		// 5. MarketData subscription (candles)
		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			TransactionId = 103,
			IsSubscribe = true,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			SecurityId = security.ToSecurityId(),
		}, CancellationToken);
		AssertReceived(receivedByEmulator, MessageTypes.MarketData, "MarketData must reach emulator");

		// Note: OrderRegister goes directly to MarketEmulator inside EmulationMessageAdapter,
		// not through the inner adapter (RoutingTestAdapter). This is by design.

		// === Test responses come back ===
		AssertReceived(receivedFromBasket, MessageTypes.Connect, "Connect response must come back");
		AssertReceived(receivedFromBasket, MessageTypes.SubscriptionResponse, "SubscriptionResponse must come back");

		Console.WriteLine($"Emulator received: {receivedByEmulator.Distinct().Select(x => $"{x}").JoinCommaSpace()}");
		Console.WriteLine($"Basket returned: {receivedFromBasket.Distinct().Select(x => $"{x}").JoinCommaSpace()}");
	}

	private static void AssertReceived(List<MessageTypes> list, MessageTypes expected, string message)
	{
		if (!list.Contains(expected))
			throw new AssertFailedException($"{message}. Received: [{list.Select(x => $"{x}").JoinCommaSpace()}]");
	}

	/// <summary>
	/// Test adapter that tracks all received messages and responds appropriately.
	/// </summary>
	private sealed class RoutingTestAdapter : MessageAdapter
	{
		private readonly List<MessageTypes> _received;
		private readonly List<MessageTypes> _sent;

		public RoutingTestAdapter(List<MessageTypes> received, List<MessageTypes> sent)
			: base(new IncrementalIdGenerator())
		{
			_received = received;
			_sent = sent;

			this.AddTransactionalSupport();
			this.AddMarketDataSupport();
			this.AddSupportedMessage(MessageTypes.SecurityLookup, null);
			this.AddSupportedMessage(MessageTypes.PortfolioLookup, null);
			this.AddSupportedMessage(MessageTypes.OrderStatus, null);
			this.AddSupportedMessage(MessageTypes.MarketData, null);
			this.AddSupportedMessage(MessageTypes.OrderRegister, null);
			this.AddSupportedMessage(MessageTypes.OrderCancel, null);
			this.AddSupportedMarketDataType(DataType.Level1);
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.CandleTimeFrame);
		}

		public override bool IsAllDownloadingSupported(DataType dataType)
			=> dataType == DataType.Transactions || dataType == DataType.PositionChanges || dataType == DataType.Securities || base.IsAllDownloadingSupported(dataType);

		public override async ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			_received.Add(message.Type);

			// Respond to messages
			switch (message.Type)
			{
				case MessageTypes.Connect:
					await SendOut(new ConnectMessage(), cancellationToken);
					break;

				case MessageTypes.OrderStatus:
				case MessageTypes.PortfolioLookup:
				case MessageTypes.SecurityLookup:
				case MessageTypes.MarketData:
					var subscrMsg = (ISubscriptionMessage)message;
					await SendOut(subscrMsg.TransactionId.CreateSubscriptionResponse(), cancellationToken);
					await SendOut(new SubscriptionOnlineMessage { OriginalTransactionId = subscrMsg.TransactionId }, cancellationToken);
					break;

				case MessageTypes.OrderRegister:
					var regMsg = (OrderRegisterMessage)message;
					await SendOut(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = regMsg.SecurityId,
						OriginalTransactionId = regMsg.TransactionId,
						OrderState = OrderStates.Active,
						HasOrderInfo = true,
						ServerTime = DateTime.UtcNow,
					}, cancellationToken);
					break;
			}
		}

		private async ValueTask SendOut(Message msg, CancellationToken ct)
		{
			_sent.Add(msg.Type);
			await SendOutMessageAsync(msg, ct);
		}
	}

	/// <summary>
	/// Minimal test to check if internal SubscriptionOnlineMessageAdapter processes OrderStatus correctly.
	/// Uses a simpler chain without PositionMessageAdapter.
	/// </summary>
	[TestMethod]
	public async Task InternalSubscriptionOnlineAdapter_ProcessesOrderStatus()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var exchangeProvider = new InMemoryExchangeInfoProvider();

		var emulator = new MarketEmulator(secProvider, pfProvider, exchangeProvider, new IncrementalIdGenerator())
		{
			Settings = { MatchOnTouch = true }
		};

		// Create simplified chain like EmulationMessageAdapter (without PositionMessageAdapter):
		// ChannelMessageAdapter -> SubscriptionOnlineMessageAdapter -> MarketEmulatorAdapter -> MarketEmulator

		var emulatorAdapter = new MarketEmulatorAdapter(emulator, new IncrementalIdGenerator());
		IMessageAdapterWrapper inAdapter = new SubscriptionOnlineMessageAdapter(emulatorAdapter);

		// Use PassThroughMessageChannel for sync behavior
		var channel = new PassThroughMessageChannel();
		var outChannel = new PassThroughMessageChannel();
		inAdapter = new ChannelMessageAdapter(inAdapter, channel, outChannel);

		var outputMessages = new List<Message>();
		inAdapter.NewOutMessageAsync += (msg, ct) =>
		{
			outputMessages.Add(msg);
			var ids = (msg as ISubscriptionIdMessage)?.GetSubscriptionIds() ?? [];
			Console.WriteLine($"[OUT] {msg.Type} SubIds=[{ids.Select(x => $"{x}").JoinComma()}]");
			return default;
		};

		var adapter = (IMessageAdapter)inAdapter;

		Console.WriteLine("=== Connect ===");
		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		Console.WriteLine("\n=== OrderStatus ===");
		await adapter.SendInMessageAsync(new OrderStatusMessage { TransactionId = 1000L, IsSubscribe = true }, CancellationToken);

		Console.WriteLine("\n=== Level1 ===");
		await adapter.SendInMessageAsync(new Level1ChangeMessage
		{
			SecurityId = security.ToSecurityId(),
			ServerTime = DateTime.UtcNow,
		}
		.TryAdd(Level1Fields.LastTradePrice, 100m)
		.TryAdd(Level1Fields.BestBidPrice, 99m)
		.TryAdd(Level1Fields.BestAskPrice, 101m), CancellationToken);

		Console.WriteLine("\n=== OrderRegister ===");
		await adapter.SendInMessageAsync(new OrderRegisterMessage
		{
			TransactionId = 2000L,
			SecurityId = security.ToSecurityId(),
			PortfolioName = portfolio.Name,
			Side = Sides.Buy,
			Price = 100m,
			Volume = 1,
			OrderType = OrderTypes.Limit,
		}, CancellationToken);

		Console.WriteLine("\n=== Results ===");
		var execMsgs = outputMessages.OfType<ExecutionMessage>().Where(e => e.HasOrderInfo).ToList();
		Console.WriteLine($"ExecutionMessages: {execMsgs.Count}");
		foreach (var e in execMsgs)
		{
			var ids = e.GetSubscriptionIds();
			Console.WriteLine($"  OrigTransId={e.OriginalTransactionId} State={e.OrderState} SubIds=[{ids.Select(x => $"{x}").JoinComma()}]");
		}

		var execWithIds = execMsgs.Count(e => e.GetSubscriptionIds().Length > 0);
		IsTrue(execWithIds > 0, "Expected ExecutionMessage to have subscription IDs");
	}

	/// <summary>
	/// Tests that <see cref="SmaServerStopStrategy"/> completes backtesting
	/// and generates both regular and conditional (stop) orders.
	/// </summary>
	[TestMethod]
	public async Task BacktestServerStopStrategy_GeneratesStopOrders()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var strategy = new SmaServerStopStrategy
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

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		IsTrue(connector.IsFinished, "Backtest should finish");

		var allOrders = strategy.Orders.ToList();
		var limitOrders = allOrders.Where(o => o.Type == OrderTypes.Limit).ToList();
		var conditionalOrders = allOrders.Where(o => o.Type == OrderTypes.Conditional).ToList();

		Console.WriteLine($"Total orders: {allOrders.Count}, Limit: {limitOrders.Count}, Conditional (stop): {conditionalOrders.Count}");
		Console.WriteLine($"MyTrades: {strategy.MyTrades.Count()}");

		IsTrue(limitOrders.Count > 0, "Expected limit orders from SMA crossover");
		IsTrue(conditionalOrders.Count > 0, "Expected conditional (stop) orders");
	}

	/// <summary>
	/// Verifies that all candles from storage reach the connector during backtesting.
	/// If this test fails, it means some candle messages are being lost in the adapter pipeline.
	/// </summary>
	[TestMethod]
	[Timeout(180_000, CooperativeCancellation = true)]
	public async Task BacktestAllCandlesDelivered()
	{
		if (SkipIfNoHistoryData()) return;

		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		// Count candles in storage
		var candleType = TimeSpan.FromMinutes(1).TimeFrame();
		var storageCandles = await storageRegistry
			.GetTimeFrameCandleMessageStorage(security.Id.ToSecurityId(), TimeSpan.FromMinutes(1))
			.LoadAsync(startTime, stopTime)
			.CountAsync(CancellationToken);

		IsTrue(storageCandles > 0, "No candles in storage");

		using var connector = CreateConnector(secProvider, pfProvider, storageRegistry, startTime, stopTime);

		var finishedCandlesReceived = 0;

		connector.CandleReceived += (sub, candle) =>
		{
			if (candle.State == CandleStates.Finished)
				Interlocked.Increment(ref finishedCandlesReceived);
		};

		var strategy = new SmaStrategy
		{
			Connector = connector,
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = candleType,
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

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		Console.WriteLine($"Storage candles: {storageCandles}, Received finished candles: {finishedCandlesReceived}");

		// Every stored candle must be delivered exactly once: no candle lost in the adapter
		// pipeline and no duplicates. The previous tolerance ("&gt;= storageCandles - 1", with no
		// upper bound) accepted both a dropped last candle and arbitrary duplicates; the engine
		// actually delivers an exact 1:1 mapping (subscribers also receive a direct forward of the
		// final candle), so assert exact equality.
		AreEqual(storageCandles, finishedCandlesReceived,
			$"All stored candles must be delivered exactly once. Storage: {storageCandles}, Received: {finishedCandlesReceived}");
	}
}
