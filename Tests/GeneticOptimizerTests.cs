namespace StockSharp.Tests;

using System.Collections;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;
using StockSharp.Designer;

[TestClass]
public class GeneticOptimizerTests : BaseTestClass
{
	// Records how the engine drives the provider: how many times Compile was
	// invoked and the last formula passed. Returns a fitness function so the GA
	// can actually evaluate chromosomes.
	private sealed class MockFitnessFormulaProvider : IFitnessFormulaProvider
	{
		private readonly Func<string, Func<Strategy, decimal>> _compileFunc;

		public MockFitnessFormulaProvider(Func<string, Func<Strategy, decimal>> compileFunc = null)
		{
			_compileFunc = compileFunc;
		}

		public int CompileCallCount { get; private set; }
		public string LastFormula { get; private set; }

		public Func<Strategy, decimal> Compile(string formula)
		{
			CompileCallCount++;
			LastFormula = formula;

			if (_compileFunc != null)
				return _compileFunc(formula);

			// Default: a trivial constant fitness so the GA can run without
			// depending on real statistics being populated.
			return _ => 0m;
		}
	}

	// Provider that always fails compilation - used to verify the engine surfaces
	// the compilation error to the RunAsync caller.
	private sealed class ThrowingFitnessFormulaProvider : IFitnessFormulaProvider
	{
		private readonly Exception _exception;

		public ThrowingFitnessFormulaProvider(Exception exception)
		{
			_exception = exception;
		}

		public int CompileCallCount { get; private set; }

		public Func<Strategy, decimal> Compile(string formula)
		{
			CompileCallCount++;
			throw _exception;
		}
	}

	private static Security CreateTestSecurity()
		=> new() { Id = Paths.HistoryDefaultSecurity };

	private static Portfolio CreateTestPortfolio()
		=> Portfolio.CreateSimulator();

	private static IStorageRegistry GetHistoryStorage()
		=> Helper.FileSystem.GetStorage(Paths.HistoryDataPath);

	private static SmaStrategy CreateSmaStrategy(Security security, Portfolio portfolio)
		=> new()
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Long = 80,
			Short = 30,
		};

	private static (IStrategyParam param, object from, object to, object step, IEnumerable values)[] CreateGeneticParams(SmaStrategy strategy)
	{
		var shortParam = strategy.Parameters[nameof(SmaStrategy.Short)];
		var longParam = strategy.Parameters[nameof(SmaStrategy.Long)];

		return
		[
			(shortParam, 20, 40, 5, null),
			(longParam, 60, 100, 10, null),
		];
	}

	private static GeneticOptimizer CreateOptimizer(IFitnessFormulaProvider provider, IStorageRegistry storageRegistry = null)
	{
		storageRegistry ??= Helper.FileSystem.GetStorage(Helper.FileSystem.GetSubTemp());

		var securityProvider = new CollectionSecurityProvider();
		var portfolioProvider = new CollectionPortfolioProvider();
		var exchangeInfoProvider = new InMemoryExchangeInfoProvider();

		return new GeneticOptimizer(
			securityProvider,
			portfolioProvider,
			exchangeInfoProvider,
			storageRegistry,
			StorageFormats.Binary,
			storageRegistry.DefaultDrive,
			provider);
	}

	private static GeneticOptimizer CreateHistoryOptimizer(IFitnessFormulaProvider provider, CollectionSecurityProvider secProvider, CollectionPortfolioProvider pfProvider)
	{
		var storageRegistry = GetHistoryStorage();

		return new GeneticOptimizer(
			secProvider,
			pfProvider,
			storageRegistry.ExchangeInfoProvider,
			storageRegistry,
			StorageFormats.Binary,
			storageRegistry.DefaultDrive,
			provider);
	}

	[TestMethod]
	public void Constructor_NullProvider_ThrowsArgumentNullException()
	{
		// The engine contract (GeneticOptimizer.cs:181) rejects a null provider.
		var securityProvider = new CollectionSecurityProvider();
		var portfolioProvider = new CollectionPortfolioProvider();
		var exchangeInfoProvider = new InMemoryExchangeInfoProvider();
		var storageRegistry = Helper.FileSystem.GetStorage(Helper.FileSystem.GetSubTemp());

		Throws<ArgumentNullException>(() => new GeneticOptimizer(
			securityProvider,
			portfolioProvider,
			exchangeInfoProvider,
			storageRegistry,
			StorageFormats.Binary,
			storageRegistry.DefaultDrive,
			(IFitnessFormulaProvider)null));
	}

	[TestMethod]
	public void Constructor_WithProvider_DoesNotCompileEagerly()
	{
		// Storing the provider must not trigger compilation: the engine only
		// compiles the fitness formula lazily inside SetupGA/RunAsync. This
		// guards against accidental eager use of the injected provider.
		var provider = new MockFitnessFormulaProvider();

		using var optimizer = CreateOptimizer(provider);

		IsNotNull(optimizer);
		provider.CompileCallCount.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task RunAsync_NullCalcFitness_CompilesSettingsFitnessViaProvider()
	{
		// The only engine consumer of the injected provider is
		// GeneticOptimizer.cs:358 'calcFitness ??= _formulaProvider.Compile(Settings.Fitness)'.
		// Driving RunAsync with calcFitness=null must reach that line exactly once
		// and pass the configured Settings.Fitness formula.
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);

		var provider = new MockFitnessFormulaProvider();

		using var optimizer = CreateHistoryOptimizer(provider, secProvider, pfProvider);
		optimizer.EmulationSettings.MaxIterations = 3;

		// Sanity: default fitness formula is PnL.
		optimizer.Settings.Fitness.AssertEqual("PnL");

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategy = CreateSmaStrategy(security, portfolio);
		var geneticParams = CreateGeneticParams(strategy);

		var count = 0;

		// calcFitness is intentionally left null so the engine must use the provider.
		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, calcFitness: null, cancellationToken: CancellationToken))
		{
			count++;
		}

		IsTrue(count > 0, "Expected at least one optimization result");

		// The engine compiled the fitness exactly once, using the configured formula.
		provider.CompileCallCount.AssertEqual(1);
		provider.LastFormula.AssertEqual(optimizer.Settings.Fitness);
	}

	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task RunAsync_CustomFitnessFormula_PassedToProvider()
	{
		// When Settings.Fitness is customized, the engine must pass that exact
		// formula to the provider (not the default), proving Settings.Fitness is
		// the source of the compiled formula.
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);

		var provider = new MockFitnessFormulaProvider();

		using var optimizer = CreateHistoryOptimizer(provider, secProvider, pfProvider);
		optimizer.EmulationSettings.MaxIterations = 3;
		optimizer.Settings.Fitness = "PnL * 2";

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategy = CreateSmaStrategy(security, portfolio);
		var geneticParams = CreateGeneticParams(strategy);

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, calcFitness: null, cancellationToken: CancellationToken))
		{
		}

		provider.CompileCallCount.AssertEqual(1);
		provider.LastFormula.AssertEqual("PnL * 2");
	}

	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task RunAsync_ExplicitCalcFitness_ProviderNotUsed()
	{
		// Contract from GeneticOptimizer.cs:358: when an explicit calcFitness is
		// supplied, the engine must NOT touch the formula provider at all.
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);

		// Provider that would throw if the engine ever called it - proving it is
		// untouched when calcFitness is explicit.
		var provider = new ThrowingFitnessFormulaProvider(new InvalidOperationException("must not be called"));

		using var optimizer = CreateHistoryOptimizer(provider, secProvider, pfProvider);
		optimizer.EmulationSettings.MaxIterations = 3;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategy = CreateSmaStrategy(security, portfolio);
		var geneticParams = CreateGeneticParams(strategy);

		var count = 0;

		await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, s => s.PnL, cancellationToken: CancellationToken))
		{
			count++;
		}

		IsTrue(count > 0, "Expected at least one optimization result");
		provider.CompileCallCount.AssertEqual(0);
	}

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task RunAsync_NullCalcFitness_ProviderCompileThrows_PropagatesFromEngine()
	{
		// When the provider fails to compile the fitness formula, the failure must
		// surface to the RunAsync caller. SetupGA compiles the formula before the
		// background GA task starts, so the exception propagates on the first
		// MoveNextAsync of the iterator (no history data is required to reach it).
		var security = CreateTestSecurity();
		var portfolio = CreateTestPortfolio();

		var secProvider = new CollectionSecurityProvider([security]);
		var pfProvider = new CollectionPortfolioProvider([portfolio]);

		var expected = new InvalidOperationException("Compilation failed");
		var provider = new ThrowingFitnessFormulaProvider(expected);

		using var optimizer = CreateHistoryOptimizer(provider, secProvider, pfProvider);
		optimizer.EmulationSettings.MaxIterations = 3;

		var startTime = Paths.HistoryBeginDate;
		var stopTime = Paths.HistoryBeginDate.AddDays(6);

		var strategy = CreateSmaStrategy(security, portfolio);
		var geneticParams = CreateGeneticParams(strategy);

		var ex = await ThrowsAsync<InvalidOperationException>(async () =>
		{
			await foreach (var _ in optimizer.RunAsync(startTime, stopTime, strategy, geneticParams, calcFitness: null, cancellationToken: CancellationToken))
			{
			}
		});

		ex.Message.AssertEqual("Compilation failed");

		// The engine attempted compilation exactly once before failing.
		provider.CompileCallCount.AssertEqual(1);
	}
}
