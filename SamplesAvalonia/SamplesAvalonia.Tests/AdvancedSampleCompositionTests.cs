namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AdvancedSampleCompositionTests
{
	[TestMethod]
	[Timeout(10_000)]
	public void AdvancedHeads_HavePackageAndSourceTwinsWithDistinctNamespaces()
	{
		AssertHead(
			"09_Advanced/01_MultiConnect",
			"01_Advanced.MultiConnect.Avalonia",
			"StockSharp.Samples.Advanced.MultiConnect.Avalonia.MainWindow");
		AssertHead(
			"09_Advanced/02_StoreDataLocal",
			"02_Advanced.SaveDataLocal.Avalonia",
			"StockSharp.Samples.Advanced.SaveDataLocal.Avalonia.MainWindow");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void SharedWorkspace_UsesOperationalAvaloniaSurfacesAndTradingRoutes()
	{
		var directory = Path.Combine(FindRepositoryRoot(), "SamplesAvalonia", "09_Advanced", "Common");
		var markup = File.ReadAllText(Path.Combine(directory, "AdvancedConnectorWorkspace.axaml"));
		var code = File.ReadAllText(Path.Combine(directory, "AdvancedConnectorWorkspace.axaml.cs"));

		AssertContainsAll(markup,
			"SecurityPicker",
			"MarketDepthControl",
			"ChartControl",
			"PortfolioGrid",
			"OrderGrid",
			"MyTradeGrid",
			"TradeGrid",
			"OrderLogGrid",
			"Level1Grid",
			"NewsGrid",
			"Monitor");

		AssertContainsAll(code,
			"SampleConnectorContext",
			"SampleUiEventRouter",
			"EventSubscription",
			"RegisterOrder",
			"ReRegisterOrderEx",
			"CancelOrder",
			"DataType.MarketDepth",
			"DataType.Ticks",
			"DataType.OrderLog",
			"TimeSpan.FromMinutes(5).TimeFrame()",
			"DataType.News");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void SharedWorkspace_DetachesRuntimeEventsAndFencesQueuedUiWork()
	{
		var code = File.ReadAllText(Path.Combine(
			FindRepositoryRoot(), "SamplesAvalonia", "09_Advanced", "Common", "AdvancedConnectorWorkspace.axaml.cs"));

		AssertContainsAll(code,
			"_events.Attach()",
			"_events.Dispose()",
			"RemoveSubscriptions(_ => true)",
			"_uiEvents.Dispose()",
			"if (!_disposed)",
			"_logManager.Sources.Remove(_context.Connector)");

		Assert.AreEqual(
			Count(code, "connector.SubscriptionFailed += OnSubscriptionFailed;"),
			Count(code, "connector.SubscriptionFailed -= OnSubscriptionFailed;"));
		Assert.AreEqual(
			Count(code, "_orderGrid.OrderReRegistering += OnOrderReRegistering;"),
			Count(code, "_orderGrid.OrderReRegistering -= OnOrderReRegistering;"));
	}

	[TestMethod]
	[Timeout(10_000)]
	public void StoreDataLocal_OwnsRealStorageGraphAndDeterministicCleanup()
	{
		var code = File.ReadAllText(Path.Combine(
			HeadDirectory("09_Advanced/02_StoreDataLocal"), "LocalStorageSampleRuntime.cs"));

		AssertContainsAll(code,
			"CsvEntityRegistry",
			"StorageRegistry",
			"LocalMarketDataDrive",
			"SnapshotRegistry",
			"CsvNativeIdStorageProvider",
			"ChannelExecutor",
			"StorageBuffer",
			"((ISnapshotRegistry)snapshotRegistry).InitAsync",
			"Context.Dispose",
			"_entityRegistry.DisposeAsync",
			"_storageRegistry.Dispose",
			"_snapshotRegistry.Dispose",
			"_executor.DisposeAsync");

		Assert.IsFalse(code.Contains("ConfigManager.", StringComparison.Ordinal));
	}

	private static void AssertHead(string relativeDirectory, string projectName, string windowClass)
	{
		var directory = HeadDirectory(relativeDirectory);
		var packageProject = Path.Combine(directory, $"{projectName}.csproj");
		var sourceProject = Path.Combine(directory, $"{projectName}_fromsrc.csproj");

		Assert.IsTrue(File.Exists(packageProject), $"Missing package project {packageProject}.");
		Assert.IsTrue(File.Exists(sourceProject), $"Missing source project {sourceProject}.");

		var package = XDocument.Load(packageProject);
		var source = XDocument.Load(sourceProject);
		Assert.IsTrue(package.Descendants().Any(node =>
			node.Name.LocalName == "PackageReference" &&
			node.Attribute("Include")?.Value == "StockSharp.Xaml.Charting.Avalonia"));
		Assert.IsTrue(source.Descendants().Any(node =>
			node.Name.LocalName == "ProjectReference" &&
			(node.Attribute("Include")?.Value.EndsWith("Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj", StringComparison.OrdinalIgnoreCase) ?? false)));

		var markup = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
		StringAssert.Contains(markup, windowClass);
		Assert.IsTrue(package.Descendants().Any(node =>
			node.Name.LocalName == "Compile" &&
			(node.Attribute("Include")?.Value.EndsWith("AdvancedConnectorWorkspace.axaml.cs", StringComparison.OrdinalIgnoreCase) ?? false)));
		Assert.IsTrue(source.Descendants().Any(node =>
			node.Name.LocalName == "AvaloniaResource" &&
			(node.Attribute("Include")?.Value.EndsWith("AdvancedConnectorWorkspace.axaml", StringComparison.OrdinalIgnoreCase) ?? false)));
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
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "StockSharp.slnx")))
				return directory.FullName;
		}

		throw new DirectoryNotFoundException("Unable to locate the StockSharp repository root.");
	}
}
