namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class HistoryAdvancedSampleTests
{
	private static readonly (string Directory, string Project, string[] RuntimeFiles)[] _heads =
	[
		("06_Strategies/05_HistoryIndex", "05_Strategies.HistoryIndex.Avalonia", ["HistoryIndexSession.cs"]),
		("06_Strategies/06_HistoryQuoting", "06_Strategies.HistoryQuoting.Avalonia", ["HistoryStrategySession.cs", "StairsCountertrendStrategy.cs"]),
		("07_Testing/01_History", "01_Testing.History.Avalonia", ["HistoryTestingSession.cs", "SmaStrategy.cs", "SmaServerStopStrategy.cs"]),
		("07_Testing/02_Optimization", "02_Testing.Optimization.Avalonia", ["OptimizationRun.cs"]),
	];

	[TestMethod]
	[Timeout(10_000)]
	public void AdvancedHistoryHeads_HaveStandalonePackageAndSourceTwins()
	{
		var root = FindRepositoryRoot();
		foreach (var head in _heads)
		{
			var directory = HeadDirectory(root, head.Directory);
			var packageProject = Path.Combine(directory, $"{head.Project}.csproj");
			var sourceProject = Path.Combine(directory, $"{head.Project}_fromsrc.csproj");

			Assert.IsTrue(File.Exists(packageProject), $"Missing package project: {packageProject}");
			Assert.IsTrue(File.Exists(sourceProject), $"Missing source project: {sourceProject}");
			Assert.IsTrue(File.Exists(Path.Combine(directory, "Program.cs")), $"Missing Program.cs in {directory}");
			Assert.IsTrue(File.Exists(Path.Combine(directory, "MainWindow.axaml")), $"Missing MainWindow.axaml in {directory}");
			Assert.IsTrue(File.Exists(Path.Combine(directory, "MainWindow.axaml.cs")), $"Missing MainWindow.axaml.cs in {directory}");
			foreach (var runtimeFile in head.RuntimeFiles)
				Assert.IsTrue(File.Exists(Path.Combine(directory, runtimeFile)), $"Missing {runtimeFile} in {directory}");

			AssertProjectItem(packageProject, "PackageReference", "StockSharp.Xaml.Charting.Avalonia");
			AssertProjectItem(sourceProject, "ProjectReference", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj");
			AssertSharedImport(packageProject);
			AssertSharedImport(sourceProject);
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void HistoryIndex_PreservesCompiledBasketAndBuiltCandleReplay()
	{
		var directory = GetHeadDirectory("06_Strategies/05_HistoryIndex");
		var runtime = File.ReadAllText(Path.Combine(directory, "HistoryIndexSession.cs"));
		var window = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));
		var markup = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));

		AssertContainsAll(runtime,
			"ExpressionIndexSecurity",
			"indexSecurity.Formula.Error",
			"new HistoryEmulationConnector",
			"new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath)",
			"BuildMode = MarketDataBuildModes.Build",
			"BuildFrom = DataType.Ticks",
			"Connector.Subscribe(new Subscription(DataType.Ticks, Security))",
			"Connector.Subscribe(IndexSubscription)",
			"TryDispose(IndexSecurity, errors)");
		AssertContainsAll(window,
			"new CSharpCompiler()",
			"SampleUiEventRouter",
			"CandleReceived +=",
			"CandleReceived -=",
			"ProgressChanged +=",
			"StateChanged2 +=",
			"_sessionCancellation?.Cancel()",
			"await session.DisposeAsync()");
		AssertContainsAll(markup, "CandleDataTypeEdit", "Expression", "ChartPanel", "Monitor");
		AssertNoFakeOrWpf(runtime, window);
	}

	[TestMethod]
	[Timeout(10_000)]
	public void HistoryQuoting_PreservesStairsAndRealMarketQuoting()
	{
		var directory = GetHeadDirectory("06_Strategies/06_HistoryQuoting");
		var runtime = File.ReadAllText(Path.Combine(directory, "HistoryStrategySession.cs"));
		var strategy = File.ReadAllText(Path.Combine(directory, "StairsCountertrendStrategy.cs"));
		var window = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));

		AssertContainsAll(runtime,
			"new HistoryEmulationConnector",
			"SupportFilteredMarketDepth = true",
			"CommissionTradeRule { Value = 0.01m }",
			"await Strategy.StartAsync(cancellationToken)",
			"await Connector.StartAsync(cancellationToken)",
			"await Strategy.StopAsync(timeout.Token)",
			"TryDispose(StorageRegistry, errors)");
		AssertContainsAll(strategy,
			"_bullLength >= Length && Position >= 0",
			"_bearLength >= Length && Position <= 0",
			"new MarketQuotingBehavior",
			"new QuotingProcessor",
			"processor.OrderRegistered +=",
			"processor.OrderFailed +=",
			"processor.OwnTrade +=",
			"processor.Finished +=",
			"_quotingProcessor?.Dispose()");
		AssertContainsAll(window,
			"MarketDepth.UpdateDepth",
			"OrderGrid.Orders.TryAdd",
			"MyTradeGrid.Trades.TryAdd",
			"StatisticManager.Parameters",
			"DrawPnl(session.Strategy)",
			"SampleUiEventRouter",
			"_sessionCancellation?.Cancel()");
		AssertNoFakeOrWpf(runtime, strategy, window);
	}

	[TestMethod]
	[Timeout(10_000)]
	public void HistoryTester_PreservesEightRealFeedsAndPerRunPresentation()
	{
		var directory = GetHeadDirectory("07_Testing/01_History");
		var runtime = File.ReadAllText(Path.Combine(directory, "HistoryTestingSession.cs"));
		var window = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));
		var markup = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));

		AssertContainsAll(markup,
			"CandlesCheckBox", "TicksCheckBox", "TicksAndDepthsCheckBox", "DepthsCheckBox",
			"CandlesAndDepthsCheckBox", "OrderLogCheckBox", "LastTradeCheckBox", "SpreadCheckBox",
			"CandlesChart", "TicksChart", "TicksAndDepthsChart", "DepthsChart",
			"CandlesAndDepthsChart", "OrderLogChart", "LastTradeChart", "SpreadChart",
			"using:StockSharp.Xaml.Charting.Avalonia.Controls",
			"ChartPanel", "EquityCurveChart", "StatisticParameterGrid",
			"ColumnDefinitions=\"Auto,*,Auto,160,Auto,160,Auto,300\"",
			"CalendarDatePicker x:Name=\"BeginDate\"",
			"CalendarDatePicker x:Name=\"EndDate\"",
			"Generate market depth", "Server-side stop orders");
		Assert.IsFalse(markup.Contains("<charting:ChartControl", StringComparison.Ordinal),
			"The history views must use the WPF-compatible ChartPanel shell, not a bare chart canvas.");
		Assert.IsFalse(markup.Contains("<DatePicker x:Name=", StringComparison.Ordinal),
			"Segmented date editors overflow this compact header; use CalendarDatePicker instead.");
		AssertContainsAll(runtime,
			"new HistoryEmulationConnector",
			"new LocalMarketDataDrive(Paths.FileSystem, options.HistoryPath)",
			"MatchOnTouch = false",
			"CommissionTradeRule { Value = 0.01m }",
			"OrderLogMarketDepthBuilder",
			"TrendMarketDepthGenerator",
			"DataType.Ticks", "DataType.MarketDepth", "DataType.OrderLog", "DataType.Level1",
			"await run.Connector.SuspendAsync()",
			"await run.Connector.StartAsync()",
			"await run.StopStrategyAsync(timeout.Token)",
			"TryDispose(_storageRegistry, errors)");
		AssertContainsAll(window,
			"SampleUiEventRouter",
			"EnableCompactTransactionMarkers(",
			"GetProperty(\"CompactTransactionMarkers\")",
			"Level1Fields.LastTradePrice",
			"Level1Fields.SpreadMiddle",
			"PnLReceived2 +=",
			"PnLReceived2 -=",
			"PositionReceived +=",
			"PositionReceived -=",
			"ProgressChanged +=",
			"ProgressChanged -=",
			"await session.SuspendAsync()",
			"await session.ResumeAsync()",
			"await session.DisposeAsync()");
		AssertNoFakeOrWpf(runtime, window);
	}

	[TestMethod]
	[Timeout(10_000)]
	public void Optimization_PreservesRealBatchGeneticResultsPauseAndCancel()
	{
		var directory = GetHeadDirectory("07_Testing/02_Optimization");
		var runtime = File.ReadAllText(Path.Combine(directory, "OptimizationRun.cs"));
		var window = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));
		var markup = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));

		AssertContainsAll(runtime,
			"new BruteForceOptimizer",
			"new GeneticOptimizer",
			"HistoryEmulationConnector",
			"new LocalMarketDataDrive(Paths.FileSystem, historyPath)",
			"CommissionTradeRule { Value = 0.01m }",
			"ToBruteForce(OptimizeParameters",
			"ToBruteForceRandom(OptimizeParameters",
			"ToGeneticParameters",
			"SingleProgressChanged +=",
			"StrategyInitialized +=",
			"StatisticParameterTypes.OrderCount",
			"StatisticParameterTypes.TradeCount",
			"StatisticParameterTypes.OrderErrorCount",
			"StatisticParameterTypes.OrderCancelErrorCount",
			"StatisticParameterTypes.OrderInsufficientFundErrorCount",
			"connector?.ErrorCount ?? 0",
			"connector?.IsFinished == true",
			"strategy.CurrentTime",
			"_optimizer.LogLevel = LogLevels.Error",
			"LoggingHelper.OnlyError",
			"OwnTradeReceived +=",
			"Error +=",
			"_optimizer.Pause()",
			"_optimizer.Resume()",
			"CancellationTokenSource.CreateLinkedTokenSource",
			"DisposeIterationObjects()");
		AssertContainsAll(window,
			"GeneticSettingsEditor.GetSelectedObject()",
			"ObservableCollection<OptimizationIterationSnapshot>",
			"Statistics.CreateColumns(strategy)",
			"Statistics.AddStrategy(strategy)",
			"Statistics.UpdateProgress(strategy, snapshot.Progress)",
			"Statistics.UpdatePnL(strategy, snapshot.CurrentTime, snapshot.PnL)",
			"CompletedCountChanged +=",
			"CompletedCountChanged -=",
			"_runCancellation?.Cancel()",
			"await run.DisposeAsync()");
		AssertContainsAll(markup,
			"PropertyGridEx", "Brute force", "Genetic", "Random combinations",
			"StrategiesStatisticsPanel", "x:Name=\"Statistics\"",
			"ShowProgress=\"True\"", "ShowPnLChart=\"True\"");
		Assert.IsFalse(markup.Contains("<ListBox", StringComparison.Ordinal),
			"Optimization results must use the native statistics/PnL surface rather than a text list.");
		AssertNoFakeOrWpf(runtime, window);
	}

	private static void AssertNoFakeOrWpf(params string[] sources)
	{
		foreach (var source in sources)
		{
			Assert.IsFalse(source.Contains("System.Windows", StringComparison.Ordinal));
			Assert.IsFalse(source.Contains("Task.Delay", StringComparison.Ordinal));
			Assert.IsFalse(source.Contains("mock", StringComparison.OrdinalIgnoreCase));
		}
	}

	private static void AssertContainsAll(string source, params string[] expected)
	{
		foreach (var value in expected)
			StringAssert.Contains(source, value);
	}

	private static void AssertSharedImport(string project)
	{
		var import = XDocument.Load(project).Descendants().FirstOrDefault(element => element.Name.LocalName == "Import");
		Assert.IsNotNull(import, $"Missing shared props import in {project}.");
		var importedPath = import.Attribute("Project")?.Value.Replace('/', '\\') ?? string.Empty;
		Assert.IsTrue(importedPath.EndsWith("common_samples_avalonia.props", StringComparison.OrdinalIgnoreCase),
			$"Unexpected shared props import '{importedPath}' in {project}.");
	}

	private static void AssertProjectItem(string project, string itemName, string expectedSuffix)
	{
		var item = XDocument.Load(project).Descendants().FirstOrDefault(element =>
			element.Name.LocalName == itemName &&
			(element.Attribute("Include")?.Value.Replace('/', '\\').EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase) ?? false));
		Assert.IsNotNull(item, $"{Path.GetFileName(project)} must contain {itemName} ending with '{expectedSuffix}'.");
	}

	private static string GetHeadDirectory(string relativeDirectory)
		=> HeadDirectory(FindRepositoryRoot(), relativeDirectory);

	private static string HeadDirectory(string root, string relativeDirectory)
		=> Path.Combine(root, "SamplesAvalonia", relativeDirectory.Replace('/', Path.DirectorySeparatorChar));

	private static string FindRepositoryRoot()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "StockSharp.slnx")))
				return directory.FullName;
		}

		throw new DirectoryNotFoundException("Unable to locate the StockSharp repository root.");
	}
}
