namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class LiveStrategySampleTests
{
	[TestMethod]
	[Timeout(10_000)]
	public void LiveSpread_UsesSharedStrategiesAndOperationalAvaloniaSurface()
	{
		var directory = HeadDirectory("06_Strategies/07_LiveSpread");
		AssertTwinProjects(directory, "07_Strategies.LiveSpread", "MqSpreadStrategy.cs", "MqStrategy.cs", "StairsCountertrendStrategy.cs");

		var view = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
		AssertContainsAll(view,
			"StockSharp.Samples.Strategies.LiveSpread.Avalonia.MainWindow",
			"SecurityEditor",
			"PortfolioEditor",
			"CandleDataTypeEdit",
			"OrderGrid",
			"MyTradeGrid",
			"PortfolioGrid",
			"LogMonitor");

		var code = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));
		AssertContainsAll(code,
			"SampleConnectorContext",
			"StairsCountertrendStrategy",
			"CandleDataType = _candleDataTypeEdit.DataType",
			"strategy.StartAsync",
			"strategy.StopAsync",
			"strategy.Dispose()",
			"OrderRegisterFailReceived",
			"OwnTradeReceived",
			"_strategyGeneration");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void LiveArbitrage_MapsBothLegsAndPreservesOriginalParameters()
	{
		var directory = HeadDirectory("06_Strategies/08_LiveArbitrage");
		AssertTwinProjects(directory, "08_Strategies.LiveArbitrage", "ArbitrageStrategy.cs");

		var view = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
		AssertContainsAll(view,
			"StockSharp.Samples.Strategies.LiveArbitrage.Avalonia.MainWindow",
			"FutureSecurityEditor",
			"StockSecurityEditor",
			"FuturePortfolioEditor",
			"StockPortfolioEditor",
			"OrderGrid",
			"MyTradeGrid");

		var code = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));
		AssertContainsAll(code,
			"FutureSecurity = futureSecurity",
			"StockSecurity = stockSecurity",
			"FuturePortfolio = futurePortfolio",
			"StockPortfolio = stockPortfolio",
			"ProfitToExit = -0.05m",
			"SpreadToGenerateSignal = 0.03m",
			"StockMultiplicator = 1.26m",
			"OrderRegisterFailReceived",
			"OwnTradeReceived");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void LiveHeads_FenceUiEventsAndDetachEveryRuntimeSubscription()
	{
		foreach (var relative in new[]
		{
			"06_Strategies/07_LiveSpread",
			"06_Strategies/08_LiveArbitrage",
		})
		{
			var code = File.ReadAllText(Path.Combine(HeadDirectory(relative), "MainWindow.axaml.cs"));
			AssertContainsAll(code,
				"SampleUiEventRouter",
				"EventSubscription",
				"_connectorEvents.Attach()",
				"_connectorEvents.Dispose()",
				"_strategyEvents.Attach()",
				"_strategyEvents?.Dispose()",
				"ReferenceEquals(_strategy, strategy)",
				"_lifetimeCancellation.Cancel()",
				"TryDispose(_context)",
				"TryDispose(_logManager)");

			Assert.AreEqual(
				Count(code, "_context.Connector.SecurityReceived += OnSecurityReceived;"),
				Count(code, "_context.Connector.SecurityReceived -= OnSecurityReceived;"),
				$"Security handlers must be symmetric in {relative}.");
			Assert.AreEqual(
				Count(code, "strategy.OrderReceived += orderReceived;"),
				Count(code, "strategy.OrderReceived -= orderReceived;"),
				$"Strategy handlers must be symmetric in {relative}.");
		}
	}

	private static void AssertTwinProjects(string directory, string projectName, params string[] linkedSources)
	{
		var packageProject = Path.Combine(directory, $"{projectName}.Avalonia.csproj");
		var sourceProject = Path.Combine(directory, $"{projectName}.Avalonia_fromsrc.csproj");
		Assert.IsTrue(File.Exists(packageProject));
		Assert.IsTrue(File.Exists(sourceProject));

		var package = XDocument.Load(packageProject);
		var source = XDocument.Load(sourceProject);
		Assert.IsTrue(package.Descendants().Any(node =>
			node.Name.LocalName == "PackageReference" &&
			node.Attribute("Include")?.Value == "StockSharp.Xaml.Avalonia"));
		Assert.IsTrue(source.Descendants().Any(node =>
			node.Name.LocalName == "ProjectReference" &&
			(node.Attribute("Include")?.Value.EndsWith("Xaml.Avalonia\\Xaml.Avalonia.csproj", StringComparison.OrdinalIgnoreCase) ?? false)));

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
