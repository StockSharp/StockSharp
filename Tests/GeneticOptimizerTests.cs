namespace StockSharp.Tests;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;

[TestClass]
public class GeneticOptimizerTests : BaseTestClass
{
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

			// Default: return a simple function that returns PnL
			return s => s.PnL;
		}
	}

	private sealed class ThrowingFitnessFormulaProvider : IFitnessFormulaProvider
	{
		private readonly Exception _exception;

		public ThrowingFitnessFormulaProvider(Exception exception)
		{
			_exception = exception;
		}

		public Func<Strategy, decimal> Compile(string formula)
		{
			throw _exception;
		}
	}

	[TestMethod]
	public void Constructor_WithProvider_StoresProvider()
	{
		var provider = new MockFitnessFormulaProvider();

		using var optimizer = CreateOptimizer(provider);

		IsNotNull(optimizer);
	}

	[TestMethod]
	public void Start_WithInvalidFormula_ThrowsFromProvider()
	{
		var expectedException = new InvalidOperationException("Compilation failed");
		var provider = new ThrowingFitnessFormulaProvider(expectedException);

		using var optimizer = CreateOptimizer(provider);

		var strategy = CreateTestStrategy();
		var parameters = CreateTestParameters(strategy);

		var ex = Throws<InvalidOperationException>(() =>
			optimizer.Start(
				DateTime.Today.AddDays(-10),
				DateTime.Today,
				strategy,
				parameters));

		ex.Message.AssertEqual("Compilation failed");
	}

	[TestMethod]
	public void Start_WithCustomCalcFitness_DoesNotCallProvider()
	{
		var provider = new MockFitnessFormulaProvider();

		using var optimizer = CreateOptimizer(provider);

		var strategy = CreateTestStrategy();
		var parameters = CreateTestParameters(strategy);

		Func<Strategy, decimal> customFitness = s => s.PnL * 2;

		// Start should not throw and should not call provider
		// Note: We can't easily test that the optimization runs correctly without
		// a full integration test, but we can verify the provider wasn't called
		try
		{
			optimizer.Start(
				DateTime.Today.AddDays(-10),
				DateTime.Today,
				strategy,
				parameters,
				calcFitness: customFitness);

			// Give it a moment to start
			Thread.Sleep(100);

			// Stop immediately
			optimizer.Stop();
		}
		catch
		{
			// Ignore errors from the actual optimization process
		}

		provider.CompileCallCount.AssertEqual(0);
	}

	[TestMethod]
	public void Start_WithNullCalcFitness_UsesProviderWithSettingsFormula()
	{
		var provider = new MockFitnessFormulaProvider();

		using var optimizer = CreateOptimizer(provider);
		optimizer.Settings.Fitness = "PnL * Recovery";

		var strategy = CreateTestStrategy();
		var parameters = CreateTestParameters(strategy);

		try
		{
			optimizer.Start(
				DateTime.Today.AddDays(-10),
				DateTime.Today,
				strategy,
				parameters);

			Thread.Sleep(100);
			optimizer.Stop();
		}
		catch
		{
			// Ignore errors from the actual optimization process
		}

		provider.CompileCallCount.AssertEqual(1);
		provider.LastFormula.AssertEqual("PnL * Recovery");
	}

	private static GeneticOptimizer CreateOptimizer(IFitnessFormulaProvider provider)
	{
		var securityProvider = new CollectionSecurityProvider();
		var portfolioProvider = new CollectionPortfolioProvider();
		var exchangeInfoProvider = new InMemoryExchangeInfoProvider();
		var storageRegistry = Helper.FileSystem.GetStorage(Helper.FileSystem.GetSubTemp());

		return new GeneticOptimizer(
			securityProvider,
			portfolioProvider,
			exchangeInfoProvider,
			storageRegistry,
			StorageFormats.Binary,
			storageRegistry.DefaultDrive,
			provider);
	}

	private static Strategy CreateTestStrategy()
	{
		var strategy = new Strategy
		{
			Security = Helper.CreateStorageSecurity(),
			Portfolio = new Portfolio { Name = "Test" }
		};

		// Add a test parameter
		strategy.Parameters.Add(new StrategyParam<int>(strategy, "TestParam", 10));

		return strategy;
	}

	private static IEnumerable<(IStrategyParam param, object from, object to, object step, IEnumerable values)> CreateTestParameters(Strategy strategy)
	{
		var param = strategy.Parameters["TestParam"];

		yield return (param, 1, 100, 10, null);
	}
}
