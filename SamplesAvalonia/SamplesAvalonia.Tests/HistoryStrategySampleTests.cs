namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class HistoryStrategySampleTests
{
	private static readonly (string Directory, string Project, string Package, string SourceProject)[] _heads =
	[
		("06_Strategies/01_HistorySMA", "01_Strategies.HistorySMA.Avalonia", "StockSharp.Xaml.Charting.Avalonia", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj"),
		("06_Strategies/02_HistoryBollingerBands", "02_Strategies.HistoryBollingerBands.Avalonia", "StockSharp.Xaml.Charting.Avalonia", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj"),
		("06_Strategies/03_HistoryTrend", "03_Strategies.HistoryTrend.Avalonia", "StockSharp.Xaml.Charting.Avalonia", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj"),
		("06_Strategies/04_HistoryMarketRule", "04_Strategies.HistoryMarketRule.Avalonia", "StockSharp.Xaml.Avalonia", "Xaml.Avalonia\\Xaml.Avalonia.csproj"),
	];

	[TestMethod]
	[Timeout(10_000)]
	public void HistoryHeads_HaveStandalonePackageAndSourceTwins()
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

			AssertImport(packageProject);
			AssertImport(sourceProject);
			AssertItem(packageProject, "PackageReference", head.Package);
			AssertItem(sourceProject, "ProjectReference", head.SourceProject);
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void HistoryHeads_UseRealBinaryEmulationAndOwnedLifecycle()
	{
		var root = FindRepositoryRoot();

		foreach (var head in _heads)
		{
			var directory = HeadDirectory(root, head.Directory);
			var runtime = File.ReadAllText(Path.Combine(directory, "HistoryStrategySession.cs"));
			var window = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));

			AssertContainsAll(runtime,
				"new HistoryEmulationConnector",
				"new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath)",
				"StorageFormats.Binary",
				"Paths.HistoryDefaultSecurity",
				"await Strategy.StartAsync(cancellationToken)",
				"Connector.Connect()",
				"await Connector.StartAsync(cancellationToken)",
				"await Strategy.StopAsync(stopCancellation.Token)",
				"TryDispose(Strategy, errors)",
				"TryDispose(Connector, errors)",
				"TryDispose(StorageRegistry, errors)",
				"Interlocked.Exchange(ref _disposeState, 1)");

			AssertContainsAll(window,
				"SemaphoreSlim _sessionGate",
				"SampleUiEventRouter",
				"EventSubscription _sessionEvents",
				"ProgressChanged +=",
				"ProgressChanged -=",
				"StateChanged2 +=",
				"StateChanged2 -=",
				"_lifetimeCancellation.Cancel()",
				"await StopSessionAsync()",
				"_sessionEvents?.Dispose()");

			Assert.IsFalse(runtime.Contains("Random", StringComparison.Ordinal), $"{head.Project} must not generate fake market data.");
			Assert.IsFalse(runtime.Contains("Task.Delay", StringComparison.Ordinal), $"{head.Project} must not simulate a history feed with delays.");
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void ChartedHistoryHeads_PreserveUiAndTradingContracts()
	{
		var root = FindRepositoryRoot();

		foreach (var head in _heads.Take(3))
		{
			var directory = HeadDirectory(root, head.Directory);
			var axaml = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
			var window = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));
			var runtime = File.ReadAllText(Path.Combine(directory, "HistoryStrategySession.cs"));

			AssertContainsAll(axaml,
				"ChartControl",
				"MarketDepthControl",
				"OrderGrid",
				"MyTradeGrid",
				"StatisticParameterGrid",
				"EquityCurveChart",
				"Monitor");
			AssertContainsAll(window,
				"OrderBookReceived +=",
				"OrderBookReceived -=",
				"OrderReceived +=",
				"OrderReceived -=",
				"OrderRegisterFailReceived +=",
				"OrderRegisterFailReceived -=",
				"OwnTradeReceived +=",
				"OwnTradeReceived -=",
				"strategy.SetChart(_chart)",
				"StatisticManager.Parameters",
				"DrawPnl(session.Strategy)",
				"session.StartAsync(true");
			AssertContainsAll(runtime,
				"CommissionTradeRule { Value = 0.01m }");
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void StrategyVariants_PreserveParametersAndSignals()
	{
		var root = FindRepositoryRoot();
		var sma = HeadDirectory(root, _heads[0].Directory);
		var classicSma = File.ReadAllText(Path.Combine(sma, "SmaStrategyClassicStrategy.cs"));
		var martingaleSma = File.ReadAllText(Path.Combine(sma, "SmaStrategyMartingaleStrategy.cs"));
		AssertContainsAll(classicSma,
			"Param(nameof(LongSmaLength), 20)", ".SetOptimize(10, 50, 5)",
			"Param(nameof(ShortSmaLength), 10)", ".SetOptimize(5, 25, 5)",
			"shortValue < longValue", "Volume + Position.Abs()", "SellMarket(volume)", "BuyMarket(volume)");
		AssertContainsAll(martingaleSma,
			"Param(nameof(LongSmaLength), 80)", ".SetOptimize(40, 120, 10)",
			"Param(nameof(ShortSmaLength), 30)", ".SetOptimize(10, 50, 5)",
			"CancelActiveOrders()", "Security.ShrinkPrice(shortValue)", "RegisterOrder(CreateOrder(direction, price, volume))");

		var bollinger = HeadDirectory(root, _heads[1].Directory);
		var classicBands = File.ReadAllText(Path.Combine(bollinger, "BollingerStrategyClassicStrategy.cs"));
		var lowBands = File.ReadAllText(Path.Combine(bollinger, "BollingerStrategyLowBandStrategy.cs"));
		var upBands = File.ReadAllText(Path.Combine(bollinger, "BollingerStrategyUpBandStrategy.cs"));
		AssertContainsAll(classicBands,
			"Param(nameof(BollingerLength), 20)", ".SetOptimize(10, 50, 5)",
			"Param(nameof(BollingerDeviation), 2m)", ".SetOptimize(1m, 3m, 0.5m)",
			"candle.ClosePrice >= bands.UpBand", "candle.ClosePrice <= bands.LowBand");
		AssertContainsAll(lowBands, "candle.ClosePrice <= bands.LowBand && Position == 0", "candle.ClosePrice >= bands.MovingAverage && Position < 0");
		AssertContainsAll(upBands, "candle.ClosePrice >= bands.UpBand && Position == 0", "candle.ClosePrice <= bands.MovingAverage && Position > 0");

		var trend = HeadDirectory(root, _heads[2].Directory);
		AssertContainsAll(File.ReadAllText(Path.Combine(trend, "OneCandleCountertrendStrategy.cs")),
			"candle.OpenPrice < candle.ClosePrice && Position >= 0", "SellMarket(Volume + Position.Abs())",
			"candle.OpenPrice > candle.ClosePrice && Position <= 0", "BuyMarket(Volume + Position.Abs())");
		AssertContainsAll(File.ReadAllText(Path.Combine(trend, "OneCandleTrendStrategy.cs")),
			"candle.OpenPrice < candle.ClosePrice && Position <= 0", "BuyMarket(Volume + Position.Abs())",
			"candle.OpenPrice > candle.ClosePrice && Position >= 0", "SellMarket(Volume + Position.Abs())");
		AssertContainsAll(File.ReadAllText(Path.Combine(trend, "StairsCountertrendStrategy.cs")),
			"Param(nameof(Length), 3)", ".SetOptimize(2, 10, 1)", "_bullLength >= Length && Position >= 0", "_bearLength >= Length && Position <= 0");
		AssertContainsAll(File.ReadAllText(Path.Combine(trend, "StairsTrendStrategy.cs")),
			"Param(nameof(Length), 3)", ".SetOptimize(2, 10, 1)", "_bullLength >= Length && Position <= 0", "_bearLength >= Length && Position >= 0");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void MarketRuleHead_PreservesNestedExclusiveOrAndUntilRules()
	{
		var root = FindRepositoryRoot();
		var directory = HeadDirectory(root, _heads[3].Directory);
		var window = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));
		AssertContainsAll(window,
			"new SimpleCandleRulesStrategy", "LogLevel = LogLevels.Debug", "session.StartAsync(false",
			"_logManager.Sources.Add(session.Strategy)");
		Assert.IsFalse(window.Contains("Sources.Add(session.Connector)", StringComparison.Ordinal), "The WPF lesson intentionally logs the market-rule strategy only.");

		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "SimpleCandleRulesStrategy.cs")),
			"WhenCandlesStarted(subscription)", "\"10%\".ToUnit()", "WhenTotalVolumeMore(candle, volumeDifference)", ".Once()");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "SimpleOrderRulesStrategy.cs")),
			"CreateOrder(Sides.Buy, default, volume)", "WhenRegistered(this)", "WhenRegisterFailed(this)", ".Exclusive(failed)", ".Exclusive(registered)", "10_000_000");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "SimpleRulesStrategy.cs")),
			"WhenOrderBookReceived(this)", "№1", "№2", "№3", "№4", ".Once()");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "SimpleTradeRulesStrategy.cs")),
			"WhenLastTradePriceMore(this, firstTrade.Price + 2)", ".Or(subscription.WhenLastTradePriceLess(this, firstTrade.Price - 2))", ".Once()");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "SimpleRulesUntilStrategy.cs")),
			"WhenOrderBookReceived(this)", ".Until(() => count >= 10)", "Subscribe(tickSubscription)", "Subscribe(depthSubscription)");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void ConnectorContext_DoesNotPublishOwnedAdapterCatalogGlobally()
	{
		var root = FindRepositoryRoot();
		var source = File.ReadAllText(Path.Combine(root, "SamplesAvalonia", "Common", "SampleConnectorContext.cs"));

		Assert.IsFalse(source.Contains("ConfigManager", StringComparison.Ordinal), "Sample-local adapter catalogs must not be process-global services.");
		Assert.IsFalse(source.Contains("TryRegisterService", StringComparison.Ordinal), "Sample-local adapter catalogs must not be process-global services.");
		AssertContainsAll(source,
			"_ownsAdapterProvider",
			"_ownedAdapterCatalog",
			"PossibleAdapters",
			"ReferenceEqualityComparer.Instance",
			"TryRelease(adapter.Dispose, errors)");
	}

	private static string HeadDirectory(string root, string relativeDirectory)
		=> Path.Combine(root, "SamplesAvalonia", relativeDirectory.Replace('/', Path.DirectorySeparatorChar));

	private static void AssertImport(string project)
	{
		var document = XDocument.Load(project);
		var import = document.Root?.Elements().FirstOrDefault(element => element.Name.LocalName == "Import");
		Assert.IsNotNull(import, $"Missing shared props import in {project}.");
		var importedPath = import.Attribute("Project")?.Value.Replace('/', '\\') ?? string.Empty;
		Assert.IsTrue(importedPath.EndsWith("common_samples_avalonia.props", StringComparison.OrdinalIgnoreCase),
			$"Unexpected shared props import '{importedPath}' in {project}.");
	}

	private static void AssertItem(string project, string itemName, string include)
	{
		var document = XDocument.Load(project);
		var match = document.Descendants().FirstOrDefault(element =>
			element.Name.LocalName == itemName &&
			(element.Attribute("Include")?.Value.Replace('/', '\\').EndsWith(include, StringComparison.OrdinalIgnoreCase) ?? false));
		Assert.IsNotNull(match, $"{Path.GetFileName(project)} must contain {itemName} '{include}'.");
	}

	private static void AssertContainsAll(string source, params string[] expected)
	{
		foreach (var value in expected)
			StringAssert.Contains(source, value);
	}

	private static string FindRepositoryRoot()
	{
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory is not null)
		{
			if (File.Exists(Path.Combine(directory.FullName, "StockSharp.slnx")))
				return directory.FullName;

			directory = directory.Parent;
		}

		throw new DirectoryNotFoundException("Unable to locate the StockSharp repository root.");
	}
}
