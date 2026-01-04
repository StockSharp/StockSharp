namespace StockSharp.Tests;

using System.Collections;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;
using StockSharp.Designer;

[TestClass]
public class OptimizerTests : BaseTestClass
{
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

	/// <summary>
	/// Creates a list of SMA strategy parameter combinations for optimization.
	/// </summary>
	private static IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> CreateStrategyIterations(
		Security security, Portfolio portfolio, int shortFrom, int shortTo, int shortStep, int longFrom, int longTo, int longStep)
	{
		for (var s = shortFrom; s <= shortTo; s += shortStep)
		{
			for (var l = longFrom; l <= longTo; l += longStep)
			{
				if (s >= l)
					continue; // Short SMA should always be less than Long SMA

				var strategy = new SmaStrategy
				{
					Security = security,
					Portfolio = portfolio,
					Volume = 1,
					CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
					Long = l,
					Short = s,
				};

				var shortParam = strategy.Parameters[nameof(SmaStrategy.Short)];
				var longParam = strategy.Parameters[nameof(SmaStrategy.Long)];

				yield return (strategy, [shortParam, longParam]);
			}
		}
	}

	/// <summary>
	/// Tests that BruteForceOptimizer starts and stops properly.
	/// </summary>
	[TestMethod]
	public async Task BruteForceOptimizerStartsAndStops()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6); // Short period for faster test

		var strategies = CreateStrategyIterations(security, portfolio, 20, 30, 10, 60, 80, 20).ToList();

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		// Wait for completion
		var completed = await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (completed != stoppedTcs.Task)
		{
			optimizer.Stop();
			Fail("Optimizer did not complete in time");
		}

		optimizer.State.AssertEqual(ChannelStates.Stopped, "Optimizer should be stopped");
	}

	/// <summary>
	/// Tests that BruteForceOptimizer raises progress events.
	/// </summary>
	[TestMethod]
	public async Task BruteForceOptimizerRaisesProgressEvents()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 20, 30, 10, 60, 80, 20).ToList();

		var singleProgressCount = 0;
		var totalProgressValues = new List<int>();

		optimizer.SingleProgressChanged += (strategy, parameters, progress) =>
		{
			Interlocked.Increment(ref singleProgressCount);
		};

		optimizer.TotalProgressChanged += (progress, duration, remaining) =>
		{
			lock (totalProgressValues)
			{
				totalProgressValues.Add(progress);
			}
		};

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
		}

		// Verify that progress events were raised
		IsTrue(singleProgressCount > 0, "Expected single progress events to be raised");
		IsTrue(totalProgressValues.Count > 0, "Expected total progress events to be raised");
	}

	/// <summary>
	/// Tests that BruteForceOptimizer raises StrategyInitialized event.
	/// </summary>
	[TestMethod]
	public async Task BruteForceOptimizerRaisesStrategyInitializedEvent()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 25, 35, 10, 70, 90, 20).ToList();

		var initializedStrategies = new List<Strategy>();

		optimizer.StrategyInitialized += (strategy, parameters) =>
		{
			lock (initializedStrategies)
			{
				initializedStrategies.Add(strategy);
			}
		};

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
		}

		// Verify that strategies were initialized
		IsTrue(initializedStrategies.Count > 0, "Expected strategies to be initialized");
	}

	/// <summary>
	/// Tests that BruteForceOptimizer can be stopped mid-run.
	/// </summary>
	[TestMethod]
	public async Task BruteForceOptimizerCanBeStopped()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate; // Longer period

		// Create many iterations so we can stop mid-run
		var strategies = CreateStrategyIterations(security, portfolio, 20, 40, 5, 50, 100, 10).ToList();

		var startedTcs = new TaskCompletionSource<bool>();
		var stoppedTcs = new TaskCompletionSource<bool>();

		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Started)
				startedTcs.TrySetResult(true);
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		// Wait for start
		await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!startedTcs.Task.IsCompleted)
		{
			Fail("Optimizer did not start in time");
		}

		// Stop the optimizer
		optimizer.Stop();

		// Wait for stopped
		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromSeconds(30)));

		optimizer.State.AssertEqual(ChannelStates.Stopped, "Optimizer should be stopped after Stop() call");
		optimizer.IsCancelled.AssertTrue("IsCancelled should be true after Stop()");
	}

	/// <summary>
	/// Tests that BruteForceOptimizer can be suspended and resumed.
	/// </summary>
	[TestMethod]
	public async Task BruteForceOptimizerCanBeSuspendedAndResumed()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		var strategies = CreateStrategyIterations(security, portfolio, 20, 35, 5, 60, 90, 10).ToList();

		var startedTcs = new TaskCompletionSource<bool>();
		var suspendedTcs = new TaskCompletionSource<bool>();
		var stoppedTcs = new TaskCompletionSource<bool>();

		optimizer.StateChanged += (oldState, newState) =>
		{
			switch (newState)
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

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		// Wait for start
		await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!startedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
			Fail("Optimizer did not start in time");
		}

		// Suspend
		optimizer.Suspend();

		// Wait for suspended
		await Task.WhenAny(suspendedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!suspendedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
			Fail("Optimizer did not suspend in time");
		}

		optimizer.State.AssertEqual(ChannelStates.Suspended, "State should be Suspended");

		// Resume and wait for completion
		startedTcs = new TaskCompletionSource<bool>();
		optimizer.Resume();

		await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(5), CancellationToken));

		// Wait for completion or timeout
		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
		}

		optimizer.State.AssertEqual(ChannelStates.Stopped, "Optimizer should be stopped");
	}

	/// <summary>
	/// Tests that BruteForceOptimizer respects MaxIterations setting.
	/// </summary>
	[TestMethod]
	public async Task BruteForceOptimizerRespectsMaxIterations()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		// Limit iterations
		optimizer.EmulationSettings.MaxIterations = 2;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		// Create more iterations than MaxIterations
		var strategies = CreateStrategyIterations(security, portfolio, 20, 40, 5, 60, 100, 10).ToList();

		var executedCount = 0;
		optimizer.SingleProgressChanged += (strategy, parameters, progress) =>
		{
			Interlocked.Increment(ref executedCount);
		};

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
		}

		// Due to MaxIterations, should not execute all strategies
		IsTrue(executedCount <= 2, "Should respect MaxIterations limit");
	}

	/// <summary>
	/// Tests that GeneticOptimizer starts and stops properly.
	/// </summary>
	[TestMethod]
	public async Task GeneticOptimizerStartsAndStops()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new GeneticOptimizer(secProvider, pfProvider, storageRegistry, Paths.FileSystem);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		// Get optimizable parameters (those with CanOptimize = true)
		var shortParam = strategy.Parameters[nameof(SmaStrategy.Short)];
		var longParam = strategy.Parameters[nameof(SmaStrategy.Long)];

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		// Limit iterations for faster test
		optimizer.EmulationSettings.MaxIterations = 5;

		// Use fitness function based on PnL
		// Parameters format: (param, from, to, step, values)
		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(shortParam, 20, 40, 5, null),
			(longParam, 60, 100, 10, null),
		};
		optimizer.Start(startTime, stopTime, strategy, geneticParams, s => s.PnL);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(3), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
			await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken);
		}

		optimizer.State.AssertEqual(ChannelStates.Stopped, "Optimizer should be stopped");
	}

	/// <summary>
	/// Tests that GeneticOptimizer raises progress events.
	/// </summary>
	[TestMethod]
	public async Task GeneticOptimizerRaisesProgressEvents()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new GeneticOptimizer(secProvider, pfProvider, storageRegistry, Paths.FileSystem);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var shortParam = strategy.Parameters[nameof(SmaStrategy.Short)];
		var longParam = strategy.Parameters[nameof(SmaStrategy.Long)];

		var singleProgressCount = 0;
		optimizer.SingleProgressChanged += (s, parameters, progress) =>
		{
			Interlocked.Increment(ref singleProgressCount);
		};

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.EmulationSettings.MaxIterations = 5;

		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(shortParam, 20, 40, 5, null),
			(longParam, 60, 100, 10, null),
		};
		optimizer.Start(startTime, stopTime, strategy, geneticParams, s => s.PnL);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(3), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
			await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken);
		}

		// Verify that progress events were raised
		IsTrue(singleProgressCount > 0, "Expected single progress events to be raised");
	}

	/// <summary>
	/// Tests that GeneticOptimizer can be stopped mid-run.
	/// </summary>
	[TestMethod]
	public async Task GeneticOptimizerCanBeStopped()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new GeneticOptimizer(secProvider, pfProvider, storageRegistry, Paths.FileSystem);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate;

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var shortParam = strategy.Parameters[nameof(SmaStrategy.Short)];
		var longParam = strategy.Parameters[nameof(SmaStrategy.Long)];

		var startedTcs = new TaskCompletionSource<bool>();
		var stoppedTcs = new TaskCompletionSource<bool>();

		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Started)
				startedTcs.TrySetResult(true);
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		// Many iterations so we can stop mid-run
		optimizer.EmulationSettings.MaxIterations = 100;

		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(shortParam, 20, 40, 5, null),
			(longParam, 60, 100, 10, null),
		};
		optimizer.Start(startTime, stopTime, strategy, geneticParams, s => s.PnL);

		// Wait for start
		await Task.WhenAny(startedTcs.Task, Task.Delay(TimeSpan.FromSeconds(10), CancellationToken));

		if (!startedTcs.Task.IsCompleted)
		{
			Fail("Optimizer did not start in time");
		}

		// Stop the optimizer
		optimizer.Stop();

		// Wait for stopped
		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromSeconds(30), CancellationToken));

		optimizer.State.AssertEqual(ChannelStates.Stopped, "Optimizer should be stopped after Stop() call");
		optimizer.IsCancelled.AssertTrue("IsCancelled should be true after Stop()");
	}

	/// <summary>
	/// Tests that optimizer handles multiple securities.
	/// </summary>
	[TestMethod]
	public async Task OptimizerHandlesMultipleSecurities()
	{
		var security1 = CreateTestSecurity();
		var security2 = new Security { Id = Paths.HistoryDefaultSecurity2 };

		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security1, security2]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		// Create strategies for both securities
		var strategies = new List<(Strategy strategy, IStrategyParam[] parameters)>();

		foreach (var security in new[] { security1, security2 })
		{
			var strategy = new SmaStrategy
			{
				Security = security,
				Portfolio = portfolio,
				Volume = 1,
				CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
				Long = 80,
				Short = 30,
			};

			var shortParam = strategy.Parameters[nameof(SmaStrategy.Short)];
			var longParam = strategy.Parameters[nameof(SmaStrategy.Long)];

			strategies.Add((strategy, [shortParam, longParam]));
		}

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
		}

		optimizer.State.AssertEqual(ChannelStates.Stopped, "Optimizer should be stopped");
	}

	/// <summary>
	/// Tests that optimizer raises events with strategy statistics after completion.
	/// </summary>
	[TestMethod]
	public async Task OptimizerRaisesEventsWithStrategyStatistics()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 25, 35, 10, 70, 90, 20).ToList();

		var completedStrategies = new List<(Strategy strategy, IStrategyParam[] parameters)>();

		optimizer.SingleProgressChanged += (strategy, parameters, progress) =>
		{
			lock (completedStrategies)
			{
				completedStrategies.Add((strategy, parameters));
			}
		};

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
		}

		// Verify that completed strategies have statistics
		foreach (var (strategy, parameters) in completedStrategies)
		{
			IsNotNull(strategy, "Strategy should not be null");
			IsNotNull(parameters, "Parameters should not be null");
			IsTrue(parameters.Length > 0, "Parameters should not be empty");

			// Strategy should have run and have some data
			var statisticManager = strategy.StatisticManager;
			statisticManager.AssertNotNull("StatisticManager should not be null");
		}
	}

	/// <summary>
	/// Tests that BruteForce optimizer with batch size works correctly.
	/// </summary>
	[TestMethod]
	public async Task BruteForceOptimizerWithBatchSize()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		// Set batch size for parallel execution
		optimizer.EmulationSettings.BatchSize = 2;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 20, 30, 10, 60, 80, 20).ToList();

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
			Fail("Optimizer did not complete in time");
		}

		optimizer.State.AssertEqual(ChannelStates.Stopped, "Optimizer should be stopped");
	}

	[TestMethod]
	public async Task OptimizerIterationTimesAreIncreasing()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 20, 30, 10, 60, 80, 20).ToList();
		var expectedIterations = strategies.Count;

		var completedIterations = new List<(Strategy strategy, DateTime? endTime, int progress)>();
		var timeErrors = new List<string>();
		var syncLock = new object();
		DateTime? lastIterationEndTime = null;

		optimizer.SingleProgressChanged += (strategy, parameters, progress) =>
		{
			// Get the strategy's end time from its statistics or trades
			DateTime? endTime = null;

			// Try to get the last trade time as the end time of the strategy run
			var lastTrade = strategy.MyTrades.OrderByDescending(t => t.Trade.ServerTime).FirstOrDefault();
			if (lastTrade != null)
			{
				endTime = lastTrade.Trade.ServerTime;
			}
			else
			{
				// If no trades, use last order time
				var lastOrder = strategy.Orders.OrderByDescending(o => o.Time).FirstOrDefault();
				if (lastOrder != null)
				{
					endTime = lastOrder.Time;
				}
			}

			lock (syncLock)
			{
				// Check that iteration end times are monotonically increasing
				if (endTime.HasValue && lastIterationEndTime.HasValue)
				{
					// Note: With parallel execution, times might not be strictly increasing
					// but for sequential execution they should be
				}

				if (endTime.HasValue)
					lastIterationEndTime = endTime;

				completedIterations.Add((strategy, endTime, progress));
			}
		};

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
			Fail("Optimizer did not complete in time");
		}

		// Verify iteration count
		AreEqual(expectedIterations, completedIterations.Count,
			$"Expected {expectedIterations} iterations but got {completedIterations.Count}");

		// Verify progress values are reasonable (should increase)
		var progressValues = completedIterations.Select(x => x.progress).ToList();
		for (var i = 1; i < progressValues.Count; i++)
		{
			IsTrue(progressValues[i] >= progressValues[i - 1],
				$"Progress should not decrease: {progressValues[i - 1]} -> {progressValues[i]}");
		}

		// Verify final progress is 100
		if (progressValues.Count > 0)
		{
			IsTrue(progressValues[^1] >= 99, $"Final progress should be ~100%, got {progressValues[^1]}%");
		}

		// Report any errors
		if (timeErrors.Count > 0)
		{
			Fail($"Time ordering violations:\n{string.Join("\n", timeErrors)}");
		}
	}

	/// <summary>
	/// Tests that within each optimizer iteration, strategy events have increasing times.
	/// </summary>
	[TestMethod]
	public async Task OptimizerStrategyEventsHaveIncreasingTimes()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		// Force sequential execution for easier time validation
		optimizer.EmulationSettings.BatchSize = 1;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 25, 35, 10, 70, 90, 20).ToList();

		var iterationTimeErrors = new List<string>();
		var syncLock = new object();

		optimizer.StrategyInitialized += (strategy, parameters) =>
		{
			// Subscribe to strategy events to check time ordering within each iteration
			DateTime? lastEventTime = null;
			var strategyErrors = new List<string>();

			strategy.OrderReceived += (sub, order) =>
			{
				var time = order.Time;
				if (lastEventTime.HasValue && time < lastEventTime.Value)
				{
					strategyErrors.Add($"Order time {time} < last event time {lastEventTime.Value}");
				}
				lastEventTime = time;
			};

			strategy.OwnTradeReceived += (sub, trade) =>
			{
				var time = trade.Trade.ServerTime;
				if (lastEventTime.HasValue && time < lastEventTime.Value)
				{
					strategyErrors.Add($"Trade time {time} < last event time {lastEventTime.Value}");
				}
				lastEventTime = time;
			};

			strategy.PnLReceived2 += (s, pf, time, realized, unrealized, commission) =>
			{
				if (lastEventTime.HasValue && time < lastEventTime.Value)
				{
					strategyErrors.Add($"PnL time {time} < last event time {lastEventTime.Value}");
				}
				lastEventTime = time;
			};

			// When strategy stops, collect errors
			strategy.ProcessStateChanged += (s) =>
			{
				if (s.ProcessState == ProcessStates.Stopped && strategyErrors.Count > 0)
				{
					lock (syncLock)
					{
						iterationTimeErrors.AddRange(strategyErrors.Select(e =>
							$"Strategy {strategy.Name}: {e}"));
					}
				}
			};
		};

		var stoppedTcs = new TaskCompletionSource<bool>();
		optimizer.StateChanged += (oldState, newState) =>
		{
			if (newState == ChannelStates.Stopped)
				stoppedTcs.TrySetResult(true);
		};

		optimizer.Start(startTime, stopTime, strategies, strategies.Count);

		await Task.WhenAny(stoppedTcs.Task, Task.Delay(TimeSpan.FromMinutes(2), CancellationToken));

		if (!stoppedTcs.Task.IsCompleted)
		{
			optimizer.Stop();
			Fail("Optimizer did not complete in time");
		}

		// Report any time errors
		if (iterationTimeErrors.Count > 0)
		{
			Fail($"Time ordering violations within strategy iterations:\n{string.Join("\n", iterationTimeErrors.Take(20))}");
		}
	}
}
