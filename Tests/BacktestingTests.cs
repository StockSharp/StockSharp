namespace StockSharp.Tests;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Emulation;
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
		connector.Start();

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
		Console.WriteLine($"Order states: {string.Join(", ", states)}");

		// Check done orders details
		var doneOrders = strategy.Orders.Where(o => o.State == OrderStates.Done).Take(3);
		foreach (var o in doneOrders)
			Console.WriteLine($"Done order: {o.TransactionId} Status={o.Status} Balance={o.Balance} Volume={o.Volume}");
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

		connector.Start();

		Console.WriteLine($"After connector.Start");

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Debug output
		Console.WriteLine($"Connection states: {string.Join(" -> ", connectionStates)}");
		Console.WriteLine($"Candles received (connector): {candlesReceived}");
		Console.WriteLine($"Candles received (strategy): {strategyCandlesCount}");
		Console.WriteLine($"Finished candles (strategy): {finishedCandlesCount}");
		Console.WriteLine($"Subscription responses: {subscriptionResponses.Count}");
		Console.WriteLine($"Orders received: {ordersReceived.Count}");
		Console.WriteLine($"Final subscriptions: {connector.Subscriptions.Count()}");
		Console.WriteLine($"Final strategy state: {strategy.ProcessState}");
		Console.WriteLine($"Strategy errors: {string.Join("; ", strategyErrors)}");
		Console.WriteLine($"Strategy position: {strategy.Position}");

		// Verify that orders were generated
		IsTrue(ordersReceived.Count > 0, $"Expected at least one order. Candles: {candlesReceived}, Subscriptions: {string.Join("; ", subscriptionResponses)}, Connection: {string.Join(" -> ", connectionStates)}");
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
		connector.Start();

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

		var pnlChanges = new List<decimal>();
		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			pnlChanges.Add(realized + (unrealized ?? 0));
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
		connector.Start();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// After trades, PnL should have been tracked
		// Note: PnL might be 0 if no trades, but if trades occurred, we expect PnL events
		if (strategy.MyTrades.Any())
		{
			IsTrue(pnlChanges.Count > 0, "Expected PnL changes when trades occurred");
		}
	}

	/// <summary>
	/// Tests that position changes occur during backtesting.
	/// </summary>
	[TestMethod]
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
		connector.Start();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// If trades occurred, position should have changed
		if (strategy.MyTrades.Any())
		{
			IsTrue(positionChanges.Count > 0, "Expected position changes when trades occurred");
		}
	}

	/// <summary>
	/// Tests that statistics are tracked during backtesting.
	/// </summary>
	[TestMethod]
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
		connector.Start();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify statistics manager is populated
		var statisticManager = strategy.StatisticManager;
		IsNotNull(statisticManager, "StatisticManager should not be null");

		// Check that some statistics are available
		var parameters = statisticManager.Parameters.ToList();
		IsTrue(parameters.Count > 0, "Expected statistics parameters to be tracked");
	}

	/// <summary>
	/// Tests that commission rules are applied during backtesting.
	/// </summary>
	[TestMethod]
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

		// Add commission rule - 0.1% per trade
		connector.EmulationAdapter.Settings.CommissionRules = [new CommissionTradeRule { Value = 0.001m }];

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

		var totalCommission = 0m;
		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			if (commission.HasValue)
				totalCommission = commission.Value;
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
		connector.Start();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// If trades occurred, commission should be tracked
		if (strategy.MyTrades.Any())
		{
			IsNotNull(strategy.Commission, "Commission should be tracked when trades occurred");
		}
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
		connector.Start();

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
		connector.Start();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify that progress events were raised
		IsTrue(progressValues.Count > 0, "Expected progress events to be raised");

		// Progress should eventually reach or approach 100
		var maxProgress = progressValues.Max();
		IsTrue(maxProgress >= 50, "Expected progress to reach at least 50%");
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
		connector.Start();

		// Wait for started
		await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!startedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not start in time");
		}

		// Suspend
		connector.Suspend();

		// Wait for suspended
		await Task.WhenAny(suspendedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!suspendedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not suspend in time");
		}

		AreEqual(ChannelStates.Suspended, connector.State, "State should be Suspended");

		// Resume
		connector.Start();

		// Wait for completion
		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			connector.Disconnect();
			Fail("Backtest did not complete after resume in time");
		}

		connector.IsFinished.AssertTrue("IsFinished should be true after completion");
	}

	/// <summary>
	/// Tests that multiple securities can be backtested.
	/// </summary>
	[TestMethod]
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
		connector.Start();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Both strategies should have completed
		strategy1.ProcessState.AssertEqual(ProcessStates.Stopped, "Strategy 1 should be stopped");
		strategy2.ProcessState.AssertEqual(ProcessStates.Stopped, "Strategy 2 should be stopped");
	}

	/// <summary>
	/// Tests that all event times are monotonically increasing during backtesting.
	/// Time should always increase - both within same event type and across different event types.
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

		// Track all event times globally
		var allEventTimes = new List<(DateTime time, string eventType)>();
		var syncLock = new object();

		// Track times per event type
		DateTime? lastOrderTime = null;
		DateTime? lastTradeTime = null;
		DateTime? lastPnLTime = null;
		DateTime? lastPositionTime = null;

		var orderTimeErrors = new List<string>();
		var tradeTimeErrors = new List<string>();
		var pnlTimeErrors = new List<string>();
		var positionTimeErrors = new List<string>();
		var globalTimeErrors = new List<string>();

		strategy.OrderReceived += (sub, order) =>
		{
			var time = order.Time;
			lock (syncLock)
			{
				// Check within order events
				if (lastOrderTime.HasValue && time < lastOrderTime.Value)
				{
					orderTimeErrors.Add($"Order time decreased: {lastOrderTime.Value} -> {time}");
				}
				lastOrderTime = time;

				// Check global time ordering
				if (allEventTimes.Count > 0)
				{
					var lastGlobal = allEventTimes[^1];
					if (time < lastGlobal.time)
					{
						globalTimeErrors.Add($"Order time {time} is less than previous {lastGlobal.eventType} time {lastGlobal.time}");
					}
				}
				allEventTimes.Add((time, "Order"));
			}
		};

		strategy.OwnTradeReceived += (sub, trade) =>
		{
			var time = trade.Trade.ServerTime;
			lock (syncLock)
			{
				// Check within trade events
				if (lastTradeTime.HasValue && time < lastTradeTime.Value)
				{
					tradeTimeErrors.Add($"Trade time decreased: {lastTradeTime.Value} -> {time}");
				}
				lastTradeTime = time;

				// Check global time ordering
				if (allEventTimes.Count > 0)
				{
					var lastGlobal = allEventTimes[^1];
					if (time < lastGlobal.time)
					{
						globalTimeErrors.Add($"Trade time {time} is less than previous {lastGlobal.eventType} time {lastGlobal.time}");
					}
				}
				allEventTimes.Add((time, "Trade"));
			}
		};

		strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
		{
			lock (syncLock)
			{
				// Check within PnL events
				if (lastPnLTime.HasValue && time < lastPnLTime.Value)
				{
					pnlTimeErrors.Add($"PnL time decreased: {lastPnLTime.Value} -> {time}");
				}
				lastPnLTime = time;

				// Check global time ordering
				if (allEventTimes.Count > 0)
				{
					var lastGlobal = allEventTimes[^1];
					if (time < lastGlobal.time)
					{
						globalTimeErrors.Add($"PnL time {time} is less than previous {lastGlobal.eventType} time {lastGlobal.time}");
					}
				}
				allEventTimes.Add((time, "PnL"));
			}
		};

		strategy.PositionReceived += (s, pos) =>
		{
			var time = pos.ServerTime != default ? pos.ServerTime : DateTime.MinValue;
			if (time == DateTime.MinValue)
				return;

			lock (syncLock)
			{
				// Check within position events
				if (lastPositionTime.HasValue && time < lastPositionTime.Value)
				{
					positionTimeErrors.Add($"Position time decreased: {lastPositionTime.Value} -> {time}");
				}
				lastPositionTime = time;

				// Check global time ordering
				if (allEventTimes.Count > 0)
				{
					var lastGlobal = allEventTimes[^1];
					if (time < lastGlobal.time)
					{
						globalTimeErrors.Add($"Position time {time} is less than previous {lastGlobal.eventType} time {lastGlobal.time}");
					}
				}
				allEventTimes.Add((time, "Position"));
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
		connector.Start();

		var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != tcs.Task)
		{
			connector.Disconnect();
			Fail("Backtest did not complete in time");
		}

		// Verify we received some events
		IsTrue(allEventTimes.Count > 0, "Expected to receive some events");

		// Report all errors
		var allErrors = new List<string>();
		allErrors.AddRange(orderTimeErrors);
		allErrors.AddRange(tradeTimeErrors);
		allErrors.AddRange(pnlTimeErrors);
		allErrors.AddRange(positionTimeErrors);
		allErrors.AddRange(globalTimeErrors);

		if (allErrors.Count > 0)
		{
			Fail($"Time ordering violations detected:\n{string.Join("\n", allErrors.Take(20))}");
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
		connector.Start();

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
	/// Tests that two emulation runs with same deterministic random seed produce identical output messages.
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

		// Helper to run emulation and collect messages
		async Task<List<Message>> RunEmulation(bool verifyMode)
		{
			var messages = new List<Message>();

			using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
			{
				HistoryMessageAdapter =
				{
					StartDate = startTime,
					StopDate = stopTime,
				},
			};

			connector.EmulationAdapter.Settings.MatchOnTouch = true;
			var emulator = (MarketEmulator)connector.EmulationAdapter.Emulator;
			emulator.VerifyMode = verifyMode;
			emulator.RandomProvider = new DefaultRandomProvider(randomSeed);

			// Capture emulator output messages
			emulator.NewOutMessageAsync += (msg, ct) =>
			{
				// Skip non-deterministic messages
				if (msg is TimeMessage or ResetMessage)
					return default;

				messages.Add(msg.TypedClone());
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

			await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(1), CancellationToken));

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

		// Helper to run emulation and collect messages
		async Task<List<Message>> RunEmulation(bool verifyMode)
		{
			var messages = new List<Message>();

			using var connector = new HistoryEmulationConnector(secProvider, pfProvider, storageRegistry)
			{
				HistoryMessageAdapter =
				{
					StartDate = startTime,
					StopDate = stopTime,
				},
			};

			connector.EmulationAdapter.Settings.MatchOnTouch = true;
			var emulator = (MarketEmulator)connector.EmulationAdapter.Emulator;
			emulator.VerifyMode = verifyMode;
			emulator.RandomProvider = new DefaultRandomProvider(randomSeed);

			emulator.NewOutMessageAsync += (msg, ct) =>
			{
				// Skip non-deterministic messages
				if (msg is TimeMessage or ResetMessage)
					return default;

				messages.Add(msg.TypedClone());
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

			await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromMinutes(1), CancellationToken));

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
			Console.WriteLine($"  ExecMsg: HasOrderInfo={e.HasOrderInfo}, State={e.OrderState}, SubscriptionIds=[{string.Join(",", ids)}]");
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
			Console.WriteLine($"  ExecMsg: HasOrderInfo={e.HasOrderInfo}, State={e.OrderState}, SubscriptionIds=[{string.Join(",", ids)}]");
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

		// Intercept messages going INTO EmulationAdapter
		var origSendIn = emulationAdapter.GetType().GetMethod("SendInMessageAsync", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

		emulationAdapter.NewOutMessageAsync += (msg, ct) =>
		{
			var ids = (msg as ISubscriptionIdMessage)?.GetSubscriptionIds() ?? [];
			Console.WriteLine($"[EmulationAdapter OUT] {msg.Type} SubIds=[{string.Join(",", ids)}]");

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
				Console.WriteLine($"[BasketAdapter] Execution: OrigTransId={exec.OriginalTransactionId} SubIds=[{string.Join(",", ids)}]");
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
			Console.WriteLine($"  {type} SubIds=[{string.Join(",", ids)}]");

		Console.WriteLine($"At BasketAdapter: {atBasket.Count} executions");
		foreach (var (type, ids) in atBasket)
			Console.WriteLine($"  {type} SubIds=[{string.Join(",", ids)}]");

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

		Console.WriteLine($"Emulator received: {string.Join(", ", receivedByEmulator.Distinct())}");
		Console.WriteLine($"Basket returned: {string.Join(", ", receivedFromBasket.Distinct())}");
	}

	private static void AssertReceived(List<MessageTypes> list, MessageTypes expected, string message)
	{
		if (!list.Contains(expected))
			throw new AssertFailedException($"{message}. Received: [{string.Join(", ", list)}]");
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
			Console.WriteLine($"[OUT] {msg.Type} SubIds=[{string.Join(",", ids)}]");
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
			Console.WriteLine($"  OrigTransId={e.OriginalTransactionId} State={e.OrderState} SubIds=[{string.Join(",", ids)}]");
		}

		var execWithIds = execMsgs.Count(e => e.GetSubscriptionIds().Length > 0);
		IsTrue(execWithIds > 0, "Expected ExecutionMessage to have subscription IDs");
	}
}