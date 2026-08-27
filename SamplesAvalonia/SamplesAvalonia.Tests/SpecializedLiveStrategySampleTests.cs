namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class SpecializedLiveStrategySampleTests
{
	[TestMethod]
	[Timeout(10_000)]
	public void LiveOptionsQuoting_PreservesOptionAnalyticsAndHedgedQuotingWorkflow()
	{
		var directory = HeadDirectory("06_Strategies/09_LiveOptionsQuoting");
		AssertTwinProjects(
			directory,
			"09_Strategies.LiveOptionsQuoting",
			"StockSharp.Xaml.Charting.Avalonia",
			"Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj",
			"DummyProvider.cs",
			"DeltaHedgeStrategy.cs",
			"HedgeStrategy.cs");

		var view = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
		AssertContainsAll(view,
			"OptionPositionChart",
			"OptionDesk",
			"OptionVolatilitySmileChart",
			"OrderGrid",
			"MyTradeGrid",
			"PortfolioGrid",
			"LogMonitor");

		var code = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));
		AssertContainsAll(code,
			"DrawTestData()",
			"DataType.Level1, DataType.MarketDepth, DataType.Ticks",
			"asset.GetDerivatives(Connector)",
			"new BasketBlackScholes",
			"VolatilityQuotingStrategy",
			"DeltaHedgeStrategy",
			"hedge.ChildStrategies.Add(quoting)",
			"depth.ImpliedVolatility",
			"strategy.OrderReceived += orderReceived",
			"strategy.OrderReceived -= orderReceived",
			"session.Generation != generation",
			"await session.Strategy.StopAsync()",
			"session.Strategy.Dispose()");

		var quotes = File.ReadAllText(Path.Combine(directory, "QuotesWindow.axaml"));
		StringAssert.Contains(quotes, "MarketDepthControl");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void LiveTerminal_PreservesStorageConnectorAndMultiWindowTradingSurface()
	{
		var directory = HeadDirectory("06_Strategies/10_LiveTerminal");
		AssertTwinProjects(
			directory,
			"10_Strategies.LiveTerminal",
			"StockSharp.Xaml.Avalonia",
			"Xaml.Avalonia\\Xaml.Avalonia.csproj",
			"MarketQuotingProcessorStrategy.cs");

		foreach (var file in new[]
		{
			"SecuritiesWindow.axaml",
			"QuotesWindow.axaml",
			"OrdersWindow.axaml",
			"PortfoliosWindow.axaml",
			"MyTradesWindow.axaml",
			"StrategiesWindow.axaml",
			"StrategyEditWindow.axaml",
		})
			Assert.IsTrue(File.Exists(Path.Combine(directory, file)), $"Missing terminal surface {file}.");

		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "SecuritiesWindow.axaml")), "SecurityPicker", "Bid/Ask", "Depth", "New order");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "OrdersWindow.axaml")), "OrderGrid");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "PortfoliosWindow.axaml")), "PortfolioGrid");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "MyTradesWindow.axaml")), "MyTradeGrid");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "StrategiesWindow.axaml")), "StrategiesDashboard", "Add quoting");
		AssertContainsAll(File.ReadAllText(Path.Combine(directory, "StrategyEditWindow.axaml")), "PropertyGridEx");

		var runtime = File.ReadAllText(Path.Combine(directory, "TerminalConnectorRuntime.cs"));
		AssertContainsAll(runtime,
			"CsvEntityRegistry",
			"StorageExchangeInfoProvider",
			"StorageRegistry",
			"SnapshotRegistry",
			"LocalMarketDataDrive",
			"StorageModes.Snapshot",
			"var entityErrors = await _entityRegistry.InitAsync",
			"entityErrors.Values",
			"await ((ISnapshotRegistry)_snapshotRegistry).InitAsync",
			"Context.Connector.LookupAll()",
			"Context.Dispose",
			"_entityRegistry.DisposeAsync",
			"_executor.DisposeAsync");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void SpecializedLiveHeads_FenceCallbacksAndDetachOwnedEventsOnAsyncClose()
	{
		var options = File.ReadAllText(Path.Combine(HeadDirectory("06_Strategies/09_LiveOptionsQuoting"), "MainWindow.axaml.cs"));
		AssertContainsAll(options,
			"SampleUiEventRouter",
			"EventSubscription",
			"_connectorEvents.Dispose()",
			"_lifetimeCancellation.Cancel()",
			"await _strategyGate.WaitAsync()",
			"StopQuoteSessionCoreAsync");
		AssertSymmetric(options, "Connector.OrderBookReceived += OnOrderBookReceived;", "Connector.OrderBookReceived -= OnOrderBookReceived;");
		AssertSymmetric(options, "strategy.OwnTradeReceived += ownTradeReceived;", "strategy.OwnTradeReceived -= ownTradeReceived;");

		var terminalDirectory = HeadDirectory("06_Strategies/10_LiveTerminal");
		var terminal = File.ReadAllText(Path.Combine(terminalDirectory, "MainWindow.axaml.cs"));
		AssertContainsAll(terminal,
			"SampleUiEventRouter",
			"EventSubscription",
			"_callbackGeneration",
			"Interlocked.Increment(ref _callbackGeneration)",
			"await _initializationTask",
			"await _strategiesWindow.StopAllAsync()",
			"_connectorEvents.Dispose()",
			"_toolWindowEvents.Dispose()",
			"TryDispose(_runtime)");
		AssertSymmetric(terminal, "Connector.SecurityReceived += OnSecurityReceived;", "Connector.SecurityReceived -= OnSecurityReceived;");
		AssertSymmetric(terminal, "Connector.OrderReceived += OnOrderReceived;", "Connector.OrderReceived -= OnOrderReceived;");

		var securities = File.ReadAllText(Path.Combine(terminalDirectory, "SecuritiesWindow.axaml.cs"));
		AssertContainsAll(securities,
			"_connector.Subscribe(new Subscription(DataType.Level1, security))",
			"new Subscription(DataType.MarketDepth, security)",
			"_connector.UnSubscribe(session.Subscription)",
			"_connector.OrderBookReceived += OnOrderBookReceived",
			"_connector.OrderBookReceived -= OnOrderBookReceived",
			"current.Generation == generation");

		var strategies = File.ReadAllText(Path.Combine(terminalDirectory, "StrategiesWindow.axaml.cs"));
		AssertContainsAll(strategies,
			"TerminalStrategyPersistence.Load(storage, _resolveSecurity, _resolvePortfolio)",
			"MarketQuotingProcessorStrategy",
			"StrategiesDashboardItem",
			"SaveEntire(false)",
			"await strategy.StopAsync()",
			"strategy.Dispose()");

		var persistence = File.ReadAllText(Path.Combine(terminalDirectory, "TerminalStrategyPersistence.cs"));
		AssertContainsAll(persistence,
			"envelope.LoadEntire<Strategy>()",
			"resolveSecurity(persistedValue)",
			"resolvePortfolio(persistedValue)",
			"strategy.Dispose()");

		var terminalSources = Directory
			.EnumerateFiles(terminalDirectory, "*.cs", SearchOption.TopDirectoryOnly)
			.Select(File.ReadAllText)
			.ToArray();
		Assert.IsFalse(terminalSources.Any(source => source.Contains("ConfigManager", StringComparison.Ordinal)));
		Assert.IsFalse(terminalSources.Any(source => source.Contains("ServicesRegistry", StringComparison.Ordinal)));
	}

	private static void AssertTwinProjects(
		string directory,
		string projectName,
		string packageName,
		string sourceReference,
		params string[] linkedSources)
	{
		var packageProject = Path.Combine(directory, $"{projectName}.Avalonia.csproj");
		var sourceProject = Path.Combine(directory, $"{projectName}.Avalonia_fromsrc.csproj");
		var package = XDocument.Load(packageProject);
		var source = XDocument.Load(sourceProject);

		Assert.IsTrue(package.Descendants().Any(node =>
			node.Name.LocalName == "PackageReference" &&
			node.Attribute("Include")?.Value == packageName));
		Assert.IsTrue(source.Descendants().Any(node =>
			node.Name.LocalName == "ProjectReference" &&
			(node.Attribute("Include")?.Value.EndsWith(sourceReference, StringComparison.OrdinalIgnoreCase) ?? false)));

		foreach (var linkedSource in linkedSources)
		{
			Assert.IsTrue(package.Descendants().Any(node =>
				node.Name.LocalName == "Compile" &&
				(node.Attribute("Include")?.Value.EndsWith(linkedSource, StringComparison.OrdinalIgnoreCase) ?? false)));
			Assert.IsTrue(source.Descendants().Any(node =>
				node.Name.LocalName == "Compile" &&
				(node.Attribute("Include")?.Value.EndsWith(linkedSource, StringComparison.OrdinalIgnoreCase) ?? false)));
		}
	}

	private static void AssertSymmetric(string source, string attach, string detach)
		=> Assert.AreEqual(Count(source, attach), Count(source, detach), $"Unbalanced event pair: {attach}");

	private static int Count(string source, string value)
	{
		var count = 0;
		var offset = 0;
		while ((offset = source.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
		{
			count++;
			offset += value.Length;
		}
		return count;
	}

	private static void AssertContainsAll(string source, params string[] expected)
	{
		foreach (var value in expected)
			StringAssert.Contains(source, value);
	}

	private static string HeadDirectory(string relative)
		=> Path.Combine(FindRepositoryRoot(), "SamplesAvalonia", relative.Replace('/', Path.DirectorySeparatorChar));

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
