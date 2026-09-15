namespace StockSharp.Tests;

using System.Collections;
using System.Diagnostics;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;
using StockSharp.Algo.Testing;
using StockSharp.Designer;

[TestClass]
// The optimizer spreads its own iterations across every core, so running these beside other tests
// oversubscribes the machine and the ones bounded by wall clock stop making their deadlines.
[DoNotParallelize]
public class OptimizerTests : BaseTestClass
{
	private sealed class BlockingStopSmaStrategy : SmaStrategy
	{
		public ManualResetEventSlim StopEntered { get; } = new(false);

		public ManualResetEventSlim ReleaseStop { get; } = new(false);

		protected override void OnStopping()
		{
			StopEntered.Set();
			if (!ReleaseStop.Wait(TimeSpan.FromSeconds(10)))
				throw new TimeoutException("The test did not release the blocked strategy stop.");

			base.OnStopping();
		}
	}

	// A source whose draws are decided in advance: every range is asked for the same fraction of itself,
	// so a generated parameter set is a fact the test can state rather than a sample it has to survey.
	private sealed class FixedFractionRandomProvider(double fraction) : IRandomProvider
	{
		int IRandomProvider.Next(int min, int max) => min + (int)((max - min) * fraction);
		long IRandomProvider.NextLong(long min, long max) => min + (long)((max - min) * fraction);
		double IRandomProvider.NextDouble() => fraction;
		void IRandomProvider.NextBytes(byte[] buffer) => throw new NotSupportedException();
	}

	// The SMA strategy plus one parameter of each remaining type the genetic search generates values
	// for. The strategy itself never reads them - only the values it is handed back are under test.
	private sealed class TypedParamsStrategy : SmaStrategy
	{
		private readonly StrategyParam<decimal> _decimalParam;
		private readonly StrategyParam<double> _doubleParam;
		private readonly StrategyParam<TimeSpan> _timeSpanParam;

		public TypedParamsStrategy()
		{
			_decimalParam = Param(nameof(DecimalParam), 10m);
			_doubleParam = Param(nameof(DoubleParam), 10d);
			_timeSpanParam = Param(nameof(TimeSpanParam), TimeSpan.FromMinutes(10));
		}

		public decimal DecimalParam
		{
			get => _decimalParam.Value;
			set => _decimalParam.Value = value;
		}

		public double DoubleParam
		{
			get => _doubleParam.Value;
			set => _doubleParam.Value = value;
		}

		public TimeSpan TimeSpanParam
		{
			get => _timeSpanParam.Value;
			set => _timeSpanParam.Value = value;
		}
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
	/// Tests that BruteForceOptimizer.RunAsync completes all iterations.
	/// </summary>
	[TestMethod]
	public async Task BruteForceRunAsyncCompletesAllIterations()
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
		var expectedCount = strategies.Count;

		var results = new List<Strategy>();

		await foreach (var (strategy, _) in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
			results.Add(strategy);
		}

		AreEqual(expectedCount, results.Count, $"Expected {expectedCount} results but got {results.Count}");
	}

	/// <summary>
	/// Tests that BruteForceOptimizer.RunAsync raises SingleProgressChanged events.
	/// </summary>
	[TestMethod]
	public async Task BruteForceRunAsyncRaisesProgressEvents()
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

		optimizer.SingleProgressChanged += (strategy, parameters, progress) =>
		{
			Interlocked.Increment(ref singleProgressCount);
		};

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
		}

		IsTrue(singleProgressCount > 0, "Expected single progress events to be raised");
	}

	/// <summary>
	/// Tests that BruteForceOptimizer.RunAsync raises StrategyInitialized event.
	/// </summary>
	[TestMethod]
	public async Task BruteForceRunAsyncRaisesStrategyInitializedEvent()
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

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
		}

		IsTrue(initializedStrategies.Count > 0, "Expected strategies to be initialized");
	}

	/// <summary>
	/// Tests that BruteForceOptimizer.RunAsync can be cancelled mid-run.
	/// </summary>
	[TestMethod]
	[Timeout(60_000)]
	public async Task BruteForceRunAsyncCancellation()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);
		optimizer.EmulationSettings.BatchSize = 1;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 10, 40, 5, 50, 100, 5).ToList();
		var totalCount = strategies.Count;

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var count = 0;

		try
		{
			await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, cts.Token))
			{
				count++;
				if (count >= 2)
					cts.Cancel();
			}
		}
		catch (OperationCanceledException)
		{
			// expected
		}

		IsTrue(count >= 2, $"Should have received at least 2 results, got {count}");
		IsTrue(count < totalCount, $"Should have been cancelled before all {totalCount} iterations, got {count}");
	}

	[TestMethod]
	[Timeout(30_000)]
	[DoNotParallelize]
	public async Task BruteForceCancellationDrainsInFlightIterationBeforeReturning()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(
			new CollectionSecurityProvider([security]),
			new CollectionPortfolioProvider([portfolio]),
			storageRegistry);
		optimizer.EmulationSettings.BatchSize = 1;

		using var strategy = new BlockingStopSmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 10,
		};
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var cancellationRequested = 0;
		optimizer.SingleProgressChanged += (_, _, progress) =>
		{
			if (progress is > 0 and < 100 && Interlocked.Exchange(ref cancellationRequested, 1) == 0)
				cts.Cancel();
		};

		var enumeration = Task.Run(async () =>
		{
			try
			{
				await foreach (var _ in optimizer.RunAsync(
					Paths.HistoryBeginDate,
					Paths.HistoryBeginDate.AddDays(6),
					[(strategy, [strategy.Parameters[nameof(SmaStrategy.Short)], strategy.Parameters[nameof(SmaStrategy.Long)]])],
					cts.Token))
				{
				}
			}
			catch (OperationCanceledException) when (cts.IsCancellationRequested)
			{
			}
		});

		try
		{
			IsTrue(strategy.StopEntered.Wait(TimeSpan.FromSeconds(10)), "Cancellation never reached the in-flight strategy stop.");
			await Task.Delay(100, CancellationToken);
			IsFalse(enumeration.IsCompleted, "RunAsync returned before the in-flight optimizer worker finished stopping.");
		}
		finally
		{
			strategy.ReleaseStop.Set();
		}

		await enumeration.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);
		var connector = strategy.Connector as HistoryEmulationConnector;
		IsNotNull(connector);
		AreEqual(ChannelStates.Stopped, connector.State);
		strategy.Dispose();
		connector.Dispose();
	}

	/// <summary>
	/// Tests that BruteForceOptimizer.RunAsync respects MaxIterations.
	/// </summary>
	[TestMethod]
	public async Task BruteForceRunAsyncRespectsMaxIterations()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);
		optimizer.EmulationSettings.MaxIterations = 2;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 20, 40, 5, 60, 100, 10).ToList();

		var count = 0;

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
			count++;
		}

		AreEqual(2, count, $"MaxIterations=2 should yield exactly 2 results, but got {count}");
	}

	/// <summary>
	/// Tests that BruteForce optimizer with batch size=2 works correctly.
	/// </summary>
	[TestMethod]
	public async Task BruteForceRunAsyncWithBatchSize()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);
		optimizer.EmulationSettings.BatchSize = 2;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 20, 30, 10, 60, 80, 20).ToList();
		var expectedCount = strategies.Count;

		var count = 0;

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
			count++;
		}

		AreEqual(expectedCount, count, $"Expected {expectedCount} results but got {count}");
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

		var count = 0;

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
			count++;
		}

		AreEqual(strategies.Count, count, $"Expected {strategies.Count} results but got {count}");
	}

	/// <summary>
	/// Tests that optimizer yields strategy statistics after completion.
	/// </summary>
	[TestMethod]
	public async Task OptimizerYieldsStrategyStatistics()
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

		var results = new List<(Strategy strategy, IStrategyParam[] parameters)>();

		await foreach (var result in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
			results.Add(result);
		}

		foreach (var (strategy, parameters) in results)
		{
			IsNotNull(strategy, "Strategy should not be null");
			IsNotNull(parameters, "Parameters should not be null");
			IsTrue(parameters.Length > 0, "Parameters should not be empty");

			var statisticManager = strategy.StatisticManager;
			statisticManager.AssertNotNull("StatisticManager should not be null");

			// The SMA strategy trades over the history, so statistics must reflect real activity, not just exist.
			IsTrue(strategy.Orders.Any(), "Strategy should have placed orders during the backtest");
			IsTrue(strategy.MyTrades.Any() || strategy.PnL != 0, "Strategy should have trades or non-zero PnL");
		}
	}

	/// <summary>
	/// Tests that iteration count matches expected.
	/// </summary>
	[TestMethod]
	public async Task OptimizerIterationCountMatchesExpected()
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

		var count = 0;

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
			count++;
		}

		AreEqual(expectedIterations, count,
			$"Expected {expectedIterations} iterations but got {count}");
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

			// Order.Time is when the order was REGISTERED, not when this notification was raised, and
			// it does not move when the order later fills or is cancelled. An order that rests
			// across a bar is therefore reported after trades that are stamped later than it, which
			// is correct and says nothing about the clock. What it may not do is claim a
			// registration time the run has not reached yet, so that is what is checked.
			strategy.OrderReceived += (sub, order) =>
			{
				var time = order.Time;
				if (time > strategy.CurrentTime)
				{
					strategyErrors.Add($"Order registered at {time}, ahead of the run's {strategy.CurrentTime}");
				}
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

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, CancellationToken))
		{
		}

		// Report any time errors
		if (iterationTimeErrors.Count > 0)
		{
			Fail($"Time ordering violations within strategy iterations:\n{iterationTimeErrors.Take(20).JoinN()}");
		}
	}

	/// <summary>
	/// Tests that GeneticOptimizer.RunAsync yields results.
	/// </summary>
	[TestMethod]
	public async Task GeneticRunAsyncYieldsResults()
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

		optimizer.EmulationSettings.MaxIterations = 5;

		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(shortParam, 20, 40, 5, null),
			(longParam, 60, 100, 10, null),
		};

		var results = new List<Strategy>();

		await foreach (var (s, _) in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, s => s.PnL, cancellationToken: CancellationToken))
		{
			results.Add(s);
		}

		IsTrue(results.Count > 0, "Expected at least one result from genetic optimizer");
	}

	/// <summary>
	/// A value the search hands back must be one the caller could have asked for: inside the range,
	/// on the from + n*step grid, and, where a set of values was given, one of that set.
	/// </summary>
	[TestMethod]
	public async Task GeneticRunAsyncChoosesParametersTheCallerAskedFor()
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
			Long = 21,
			Short = 5,
		};

		var shortParam = strategy.Parameters[nameof(SmaStrategy.Short)];
		var longParam = strategy.Parameters[nameof(SmaStrategy.Long)];
		var stopParam = strategy.Parameters[nameof(SmaStrategy.StopValue)];

		// Neither range starts on a multiple of its step, so a grid anchored anywhere but at from
		// lands between the steps the caller asked for, or outside the range altogether.
		const int shortFrom = 5;
		const int shortTo = 11;
		const int shortStep = 4;

		const int longFrom = 21;
		const int longTo = 45;
		const int longStep = 8;

		var stopValues = new[] { new Unit(1, UnitTypes.Percent), new Unit(3, UnitTypes.Percent) };

		optimizer.EmulationSettings.MaxIterations = 5;

		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(shortParam, shortFrom, shortTo, shortStep, null),
			(longParam, longFrom, longTo, longStep, null),
			(stopParam, null, null, null, stopValues),
		};

		static void CheckGrid(IStrategyParam param, int from, int to, int step, List<string> violations)
		{
			var value = (int)param.Value;

			if (value < from || value > to)
				violations.Add($"{param.Id}={value} is outside the requested range [{from}, {to}]");
			else if ((value - from) % step != 0)
				violations.Add($"{param.Id}={value} is not on the requested grid {from} + n*{step}");
		}

		var violations = new List<string>();
		var iterations = 0;

		await foreach (var (_, parameters) in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, s => s.PnL, cancellationToken: CancellationToken))
		{
			iterations++;

			foreach (var param in parameters)
			{
				switch (param.Id)
				{
					case nameof(SmaStrategy.Short):
						CheckGrid(param, shortFrom, shortTo, shortStep, violations);
						break;

					case nameof(SmaStrategy.Long):
						CheckGrid(param, longFrom, longTo, longStep, violations);
						break;

					case nameof(SmaStrategy.StopValue):
						if (!stopValues.Contains((Unit)param.Value))
							violations.Add($"{param.Id}={param.Value} is none of the given values");

						break;

					default:
						violations.Add($"Unexpected optimized parameter {param.Id}");
						break;
				}
			}
		}

		IsTrue(iterations > 0, "Expected at least one evaluated iteration to check parameters of");

		if (violations.Count > 0)
			Fail($"Parameter values the caller never asked for:\n{violations.Distinct().JoinN()}");
	}

	/// <summary>
	/// A search whose draws are all pinned to the same fraction of every range: one chromosome exists,
	/// so one evaluated iteration says everything there is to say about what the search generates.
	/// </summary>
	private static GeneticOptimizer CreatePinnedDrawOptimizer(Strategy strategy, double fraction)
	{
		strategy.RandomProvider = new FixedFractionRandomProvider(fraction);

		var storageRegistry = GetHistoryStorage();

		var optimizer = new GeneticOptimizer(
			new CollectionSecurityProvider([strategy.Security]),
			new CollectionPortfolioProvider([strategy.Portfolio]),
			storageRegistry,
			Paths.FileSystem);

		optimizer.EmulationSettings.BatchSize = 1;
		optimizer.EmulationSettings.MaxIterations = 1;

		return optimizer;
	}

	/// <summary>
	/// Runs the search to the end and returns the parameter values it handed back, by parameter id.
	/// </summary>
	private async Task<Dictionary<string, object>> RunAndCollectParameters(
		GeneticOptimizer optimizer,
		Strategy strategy,
		(IStrategyParam param, object from, object to, object step, IEnumerable values)[] geneticParams)
	{
		var values = new Dictionary<string, object>();

		await foreach (var (_, parameters) in optimizer.RunAsync(
			Paths.HistoryBeginDate,
			Paths.HistoryBeginDate.AddDays(6),
			strategy,
			geneticParams,
			s => s.PnL,
			cancellationToken: CancellationToken))
		{
			foreach (var param in parameters)
				values[param.Id] = param.Value;
		}

		IsTrue(values.Count > 0, "The search evaluated nothing, so there are no generated values to check.");

		return values;
	}

	/// <summary>
	/// With the draw pinned to the bottom of every range, the only value the caller can be handed is the
	/// bottom of the range - whichever of the types the search generates values for it was asked about.
	/// </summary>
	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task GeneticParametersOfEveryTypeStayOnTheCallersGrid()
	{
		var strategy = new TypedParamsStrategy
		{
			Security = CreateTestSecurity(),
			Portfolio = CreateTestPortfolio(),
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		// Every range runs from 10 to 14 in steps of 4, so the caller asked for two values, 10 and 14.
		// Neither is a multiple of four: a grid measured from zero rather than from 'from' misses both.
		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(strategy.Parameters[nameof(SmaStrategy.Short)], 10, 14, 4, null),
			(strategy.Parameters[nameof(TypedParamsStrategy.DecimalParam)], 10m, 14m, 4m, null),
			(strategy.Parameters[nameof(TypedParamsStrategy.TimeSpanParam)], TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(14), TimeSpan.FromMinutes(4), null),
			(strategy.Parameters[nameof(SmaStrategy.StopValue)], new Unit(10, UnitTypes.Percent), new Unit(14, UnitTypes.Percent), new Unit(4, UnitTypes.Percent), null),
		};

		using var optimizer = CreatePinnedDrawOptimizer(strategy, 0d);

		var values = await RunAndCollectParameters(optimizer, strategy, geneticParams);

		AreEqual(10, (int)values[nameof(SmaStrategy.Short)], "the int parameter was drawn at the bottom of 10..14");
		AreEqual(10m, (decimal)values[nameof(TypedParamsStrategy.DecimalParam)], "the decimal parameter was drawn at the bottom of 10..14");
		AreEqual(TimeSpan.FromMinutes(10), (TimeSpan)values[nameof(TypedParamsStrategy.TimeSpanParam)], "the TimeSpan parameter was drawn at the bottom of 10..14 minutes");
		AreEqual(new Unit(10, UnitTypes.Percent), (Unit)values[nameof(SmaStrategy.StopValue)], "the Unit parameter was drawn at the bottom of 10%..14%");
	}

	/// <summary>
	/// A step written backwards names the same values as the same step written forwards, and a range
	/// whose ends meet names exactly one value and leaves the search nothing to choose.
	/// </summary>
	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task GeneticParameterRangeReadsTheSameBackwardsAndAtASinglePoint()
	{
		var strategy = new SmaStrategy
		{
			Security = CreateTestSecurity(),
			Portfolio = CreateTestPortfolio(),
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		// 14 down to 10 by -4 is the same request as 10 up to 14 by 4: the values are 10 and 14. The
		// second range offers 65 alone, so 65 is the only answer there is.
		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(strategy.Parameters[nameof(SmaStrategy.Short)], 14, 10, -4, null),
			(strategy.Parameters[nameof(SmaStrategy.Long)], 65, 65, 10, null),
		};

		using var optimizer = CreatePinnedDrawOptimizer(strategy, 0d);

		var values = await RunAndCollectParameters(optimizer, strategy, geneticParams);

		AreEqual(10, (int)values[nameof(SmaStrategy.Short)], "a backwards step still bottoms out at the low end of the range");
		AreEqual(65, (int)values[nameof(SmaStrategy.Long)], "a range of one value can only produce that value");
	}

	/// <summary>
	/// The bounds of a double parameter arrive as doubles - that is what StrategyParam&lt;double&gt;
	/// holds and what every other reader of those bounds takes - so the search has to accept them.
	/// </summary>
	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task GeneticDoubleParameterTakesDoubleBounds()
	{
		var strategy = new TypedParamsStrategy
		{
			Security = CreateTestSecurity(),
			Portfolio = CreateTestPortfolio(),
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		// The int parameter is here only to give the crossover a second gene to work with.
		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(strategy.Parameters[nameof(TypedParamsStrategy.DoubleParam)], 10d, 14d, 4d, null),
			(strategy.Parameters[nameof(SmaStrategy.Short)], 10, 14, 2, null),
		};

		using var optimizer = CreatePinnedDrawOptimizer(strategy, 0d);

		var values = await RunAndCollectParameters(optimizer, strategy, geneticParams);

		AreEqual(10d, (double)values[nameof(TypedParamsStrategy.DoubleParam)], "the double parameter was drawn at the bottom of 10..14");
	}

	/// <summary>
	/// Ten percent and fourteen absolute bound nothing, so the pair is a mistake to report rather than
	/// a range to draw from: StrategyParamHelper.GetRandom, the other generator over the same bounds,
	/// refuses it instead of quietly keeping one side's type and the other side's magnitude.
	/// </summary>
	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task GeneticUnitRangeWithMismatchedTypesIsRejected()
	{
		var strategy = new SmaStrategy
		{
			Security = CreateTestSecurity(),
			Portfolio = CreateTestPortfolio(),
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(strategy.Parameters[nameof(SmaStrategy.StopValue)], new Unit(10, UnitTypes.Percent), new Unit(14, UnitTypes.Absolute), new Unit(4, UnitTypes.Percent), null),
			(strategy.Parameters[nameof(SmaStrategy.Short)], 10, 14, 2, null),
		};

		using var optimizer = CreatePinnedDrawOptimizer(strategy, 0d);

		await ThrowsAsync<ArgumentException>(async () =>
		{
			await foreach (var _ in optimizer.RunAsync(
				Paths.HistoryBeginDate,
				Paths.HistoryBeginDate.AddDays(6),
				strategy,
				geneticParams,
				s => s.PnL,
				cancellationToken: CancellationToken))
			{
			}
		});
	}

	/// <summary>
	/// Tests that GeneticOptimizer.RunAsync raises SingleProgressChanged events.
	/// </summary>
	[TestMethod]
	public async Task GeneticRunAsyncRaisesProgressEvents()
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

		optimizer.EmulationSettings.MaxIterations = 5;

		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(shortParam, 20, 40, 5, null),
			(longParam, 60, 100, 10, null),
		};

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, s => s.PnL, cancellationToken: CancellationToken))
		{
		}

		IsTrue(singleProgressCount > 0, "Expected single progress events to be raised");
	}

	/// <summary>
	/// A genetic search never runs a known number of iterations - it stops when it stops
	/// improving, and a cached fitness costs no iteration at all - so the only thing a
	/// caller can measure it against is how many of its generations have been through.
	/// </summary>
	[TestMethod]
	public async Task GeneticRunAsyncReportsGenerations()
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

		var generations = new SynchronizedList<int>();

		optimizer.GenerationChanged += generations.Add;

		optimizer.Settings.Population = 4;
		optimizer.Settings.GenerationsMax = 3;
		optimizer.Settings.GenerationsStagnation = 0;

		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(strategy.Parameters[nameof(SmaStrategy.Short)], 20, 40, 5, null),
			(strategy.Parameters[nameof(SmaStrategy.Long)], 60, 100, 10, null),
		};

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, s => s.PnL, cancellationToken: CancellationToken))
		{
		}

		var ran = generations.SyncGet(c => c.ToArray());

		IsTrue(ran.Length > 0, "A search that ran generations has to say so.");

		// The number is what it is measured against, so it counts up and never repeats.
		for (var i = 1; i < ran.Length; i++)
			IsTrue(ran[i] > ran[i - 1], $"Generation {ran[i]} came after {ran[i - 1]}.");

		IsTrue(ran[^1] <= optimizer.Settings.GenerationsMax, $"Ran {ran[^1]} of at most {optimizer.Settings.GenerationsMax} generations.");
	}

	/// <summary>
	/// Tests that GeneticOptimizer.RunAsync can be cancelled.
	/// </summary>
	[TestMethod]
	[Timeout(60_000)]
	public async Task GeneticRunAsyncCancellation()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new GeneticOptimizer(secProvider, pfProvider, storageRegistry, Paths.FileSystem);

		var startTime = Paths.HistoryBeginDate;
		// Same slice as the other genetic tests: MaxIterations=100 already guarantees the run
		// cannot finish by itself, so replaying the whole history adds time but no coverage.
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

		// Many iterations so we can stop mid-run
		optimizer.EmulationSettings.MaxIterations = 100;

		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(shortParam, 20, 40, 5, null),
			(longParam, 60, 100, 10, null),
		};

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var count = 0;

		try
		{
			await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, s => s.PnL, cancellationToken: cts.Token))
			{
				count++;
				if (count >= 2)
					cts.Cancel();
			}
		}
		catch (OperationCanceledException)
		{
			// expected
		}

		IsTrue(count >= 2, $"Should have received at least 2 results before cancellation, got {count}");
		IsTrue(count < optimizer.EmulationSettings.MaxIterations,
			"Cancellation must stop the genetic optimizer before all iterations complete");
	}

	[TestMethod]
	[Timeout(10_000)]
	[DoNotParallelize]
	public async Task GeneticPreCancelledRunDoesNotStartProducer()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();
		using var optimizer = new GeneticOptimizer(
			new CollectionSecurityProvider([security]),
			new CollectionPortfolioProvider([portfolio]),
			GetHistoryStorage(),
			Paths.FileSystem);
		using var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
		};
		var parameters = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(strategy.Parameters[nameof(SmaStrategy.Short)], 20, 40, 5, null),
			(strategy.Parameters[nameof(SmaStrategy.Long)], 60, 100, 10, null),
		};
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cts.Cancel();
		var initialized = 0;
		optimizer.StrategyInitialized += (_, _) => Interlocked.Increment(ref initialized);

		try
		{
			await foreach (var _ in optimizer.RunAsync(
				Paths.HistoryBeginDate,
				Paths.HistoryBeginDate.AddDays(1),
				strategy,
				parameters,
				cancellationToken: cts.Token))
			{
			}
		}
		catch (OperationCanceledException) when (cts.IsCancellationRequested)
		{
		}

		AreEqual(0, initialized);
	}

	[TestMethod]
	[Timeout(30_000)]
	[DoNotParallelize]
	public async Task GeneticConsumerBreakCancelsAndDrainsFitnessProducer()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new GeneticOptimizer(
			new CollectionSecurityProvider([security]),
			new CollectionPortfolioProvider([portfolio]),
			storageRegistry,
			Paths.FileSystem);
		optimizer.EmulationSettings.BatchSize = 1;
		optimizer.EmulationSettings.MaxIterations = 5;

		using var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};
		var geneticParams = new (IStrategyParam param, object from, object to, object step, IEnumerable values)[]
		{
			(strategy.Parameters[nameof(SmaStrategy.Short)], 20, 40, 5, null),
			(strategy.Parameters[nameof(SmaStrategy.Long)], 60, 100, 10, null),
		};
		using var fitnessEntered = new ManualResetEventSlim(false);
		using var releaseFitness = new ManualResetEventSlim(false);
		using var consumerBreaking = new ManualResetEventSlim(false);
		Strategy yieldedStrategy = null;

		var enumeration = Task.Run(async () =>
		{
			await foreach (var (result, _) in optimizer.RunAsync(
				Paths.HistoryBeginDate,
				Paths.HistoryBeginDate.AddDays(6),
				strategy,
				geneticParams,
				resultStrategy =>
				{
					fitnessEntered.Set();
					if (!releaseFitness.Wait(TimeSpan.FromSeconds(10)))
						throw new TimeoutException("The test did not release the blocked fitness calculation.");
					return resultStrategy.PnL;
				},
				cancellationToken: CancellationToken))
			{
				yieldedStrategy = result;
				if (!fitnessEntered.Wait(TimeSpan.FromSeconds(10)))
					throw new TimeoutException("The genetic producer never entered its post-result fitness calculation.");
				consumerBreaking.Set();
				break;
			}
		});

		try
		{
			IsTrue(consumerBreaking.Wait(TimeSpan.FromSeconds(15)), "The genetic consumer never received its first result.");
			await Task.Delay(100, CancellationToken);
			IsFalse(enumeration.IsCompleted, "The async iterator returned while its genetic producer was still calculating fitness.");
		}
		finally
		{
			releaseFitness.Set();
		}

		await enumeration.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);
		IsNotNull(yieldedStrategy);
		var connector = yieldedStrategy.Connector as HistoryEmulationConnector;
		IsNotNull(connector);
		AreEqual(ChannelStates.Stopped, connector.State);
		yieldedStrategy.Dispose();
		connector.Dispose();
	}

	[TestMethod]
	[Timeout(10_000)]
	[DoNotParallelize]
	public async Task BruteForcePreCancelledRunDoesNotStartProducer()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();
		using var optimizer = new BruteForceOptimizer(
			new CollectionSecurityProvider([security]),
			new CollectionPortfolioProvider([portfolio]),
			GetHistoryStorage());
		using var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
		};
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cts.Cancel();
		var initialized = 0;
		optimizer.StrategyInitialized += (_, _) => Interlocked.Increment(ref initialized);

		try
		{
			await foreach (var _ in optimizer.RunAsync(
				Paths.HistoryBeginDate,
				Paths.HistoryBeginDate.AddDays(1),
				[(strategy, [strategy.Parameters[nameof(SmaStrategy.Short)], strategy.Parameters[nameof(SmaStrategy.Long)]])],
				cts.Token))
			{
			}
		}
		catch (OperationCanceledException) when (cts.IsCancellationRequested)
		{
		}

		AreEqual(0, initialized);
		IsNull(strategy.Connector);
	}

	[TestMethod]
	[Timeout(30_000)]
	[DoNotParallelize]
	public async Task BruteForceConsumerBreakCancelsAndDrainsInFlightWorker()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();
		using var optimizer = new BruteForceOptimizer(
			new CollectionSecurityProvider([security]),
			new CollectionPortfolioProvider([portfolio]),
			GetHistoryStorage());
		optimizer.EmulationSettings.BatchSize = 2;

		var fast = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 10,
		};
		var inFlight = new BlockingStopSmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 10,
		};
		using var inFlightStarted = new ManualResetEventSlim(false);
		using var consumerBreaking = new ManualResetEventSlim(false);
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		inFlight.CandleReceived += (_, candle) =>
		{
			if (candle.State != CandleStates.Finished)
				return;

			inFlightStarted.Set();
			Thread.Sleep(2);
		};

		var iterations = new (Strategy strategy, IStrategyParam[] parameters)[]
		{
			(fast, [fast.Parameters[nameof(SmaStrategy.Short)], fast.Parameters[nameof(SmaStrategy.Long)]]),
			(inFlight, [inFlight.Parameters[nameof(SmaStrategy.Short)], inFlight.Parameters[nameof(SmaStrategy.Long)]]),
		};
		var enumeration = Task.Run(async () =>
		{
			await foreach (var _ in optimizer.RunAsync(
				Paths.HistoryBeginDate,
				Paths.HistoryBeginDate.AddDays(2),
				iterations,
				cts.Token))
			{
				if (!inFlightStarted.Wait(TimeSpan.FromSeconds(10)))
					throw new TimeoutException("The parallel optimizer worker never started replaying history.");

				consumerBreaking.Set();
				break;
			}
		});

		try
		{
			IsTrue(consumerBreaking.Wait(TimeSpan.FromSeconds(15)), "The brute-force consumer never received a result.");
			IsTrue(inFlight.StopEntered.Wait(TimeSpan.FromSeconds(5)), "Consumer break did not stop the in-flight worker.");
			await Task.Delay(100, CancellationToken);
			IsFalse(enumeration.IsCompleted, "RunAsync returned before the in-flight worker finished stopping.");
		}
		finally
		{
			inFlight.ReleaseStop.Set();
			cts.Cancel();
		}

		await enumeration.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken);
		var connectors = new[] { fast.Connector, inFlight.Connector }
			.OfType<HistoryEmulationConnector>()
			.Distinct()
			.ToArray();
		IsTrue(connectors.Length > 0);
		foreach (var connector in connectors)
			AreEqual(ChannelStates.Stopped, connector.State);

		fast.Dispose();
		inFlight.Dispose();
		foreach (var connector in connectors)
			connector.Dispose();
	}

	/// <summary>
	/// Tests cancellation by iteration count inside the loop (consumer-side limit).
	/// </summary>
	[TestMethod]
	[Timeout(60_000)]
	public async Task BruteForceRunAsyncCancelByIterationCount()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategies = CreateStrategyIterations(security, portfolio, 10, 40, 5, 50, 100, 5).ToList();

		const int maxResults = 3;
		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		var results = new List<(Strategy strategy, IStrategyParam[] parameters)>();

		try
		{
			await foreach (var result in optimizer.RunAsync(startTime, stopTime, strategies, cts.Token))
			{
				results.Add(result);

				if (results.Count >= maxResults)
					cts.Cancel();
			}
		}
		catch (OperationCanceledException)
		{
			// expected
		}

		IsTrue(results.Count >= maxResults, $"Should have at least {maxResults} results, got {results.Count}");
		IsTrue(results.Count < strategies.Count, $"Should have been cancelled before all {strategies.Count} iterations");
	}

	/// <summary>
	/// Tests cancellation by timeout (CancelAfter).
	/// </summary>
	[TestMethod]
	[Timeout(60_000)]
	public async Task BruteForceRunAsyncCancelByTimeout()
	{
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);
		var storageRegistry = GetHistoryStorage();

		using var optimizer = new BruteForceOptimizer(secProvider, pfProvider, storageRegistry);

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryEndDate; // long period so it won't finish naturally

		var strategies = CreateStrategyIterations(security, portfolio, 10, 40, 5, 50, 100, 5).ToList();

		// The full history over all combinations takes minutes, so any short timeout interrupts the run.
		// The value only has to be shorter than the whole run, not long enough to complete iterations.
		var timeout = TimeSpan.FromSeconds(3);

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);
		cts.CancelAfter(timeout);

		var count = 0;
		var watch = Stopwatch.StartNew();

		try
		{
			await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategies, cts.Token))
			{
				count++;
			}
		}
		catch (OperationCanceledException)
		{
			// expected
		}

		watch.Stop();

		IsTrue(count < strategies.Count, $"Should have been cancelled by timeout before all {strategies.Count} iterations, got {count}");

		// The timeout must actually tear the run down, not just be observed at some later point:
		// the enumeration has to end shortly after it fires. The margin covers connector shutdown
		// of the in-flight backtests and scheduling noise on a loaded machine.
		var maxDuration = timeout + TimeSpan.FromSeconds(20);
		IsTrue(watch.Elapsed < maxDuration, $"Enumeration should have ended within {maxDuration} after a {timeout} timeout, but took {watch.Elapsed}");
	}
}
