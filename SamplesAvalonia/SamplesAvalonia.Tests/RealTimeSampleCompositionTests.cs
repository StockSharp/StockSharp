namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class RealTimeSampleCompositionTests
{
	[TestMethod]
	[Timeout(10_000)]
	public void RealTimeHead_HasPackageAndSourceTwins()
	{
		var directory = GetSampleDirectory();
		var packageProject = Path.Combine(directory, "03_Testing.RealTime.Avalonia.csproj");
		var sourceProject = Path.Combine(directory, "03_Testing.RealTime.Avalonia_fromsrc.csproj");

		AssertProjectItem(packageProject, "PackageReference", "StockSharp.Xaml.Charting.Avalonia");
		AssertProjectItem(sourceProject, "ProjectReference", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj");
		AssertImport(packageProject, "common_connectors.props");
		AssertImport(sourceProject, "common_connectors.props");
		Assert.IsTrue(File.Exists(Path.Combine(directory, "Program.cs")));
		Assert.IsTrue(File.Exists(Path.Combine(directory, "MainWindow.axaml")));
		Assert.IsTrue(File.Exists(Path.Combine(directory, "MainWindow.axaml.cs")));
	}

	[TestMethod]
	[Timeout(10_000)]
	public void RealTimeHead_PreservesRealtimeEmulationAndTradingLessons()
	{
		var code = File.ReadAllText(Path.Combine(GetSampleDirectory(), "MainWindow.axaml.cs"));

		AssertTokens(code,
			"new RealTimeEmulationTrader<IMessageAdapter>",
			"_realContext.Connector.Adapter",
			"ownAdapter: false",
			"settings.TimeZone = TimeHelper.Est",
			"settings.ConvertTime = true",
			"new Subscription(DataType.MarketDepth, security)",
			"new Subscription(DataType.Ticks, security)",
			"new Subscription(DataType.Level1, security)",
			"_realContext.Connector.Subscribe(realDepth)",
			"CreateActiveOrdersElement",
			"connector.RegisterOrder(order)",
			"_realContext.Connector.RegisterOrder(order)",
			"ReRegisterOrder",
			"CancelOrder");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void RealTimeHead_UsesAvaloniaControlsAndFencedOwnedLifetime()
	{
		var directory = GetSampleDirectory();
		var markup = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
		var code = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));

		AssertTokens(markup,
			"ChartPanel",
			"CandleDataTypeEdit",
			"SecurityPicker",
			"MarketDepthControl",
			"PortfolioGrid",
			"OrderGrid",
			"MyTradeGrid",
			"LogControl");
		AssertTokens(code,
			"SampleUiEventRouter",
			"_emulationGeneration",
			"IsCurrentEmulation",
			"EventSubscription _emulationEvents",
			"_emulationEvents?.Dispose()",
			"ClearMarketSubscriptions(connector)",
			"_lifetimeCancellation.Cancel()",
			"await Task.WhenAll(pendingTasks)",
			"TryDisconnect(_realContext.Connector)",
			"TryDispose(_realContext)");
		Assert.IsFalse(code.Contains("System.Windows", StringComparison.Ordinal));
	}

	private static void AssertTokens(string text, params string[] tokens)
	{
		foreach (var token in tokens)
			StringAssert.Contains(text, token);
	}

	private static void AssertProjectItem(string project, string itemName, string expectedSuffix)
	{
		Assert.IsTrue(File.Exists(project), $"Missing project: {project}");
		var item = XDocument.Load(project).Descendants().FirstOrDefault(element =>
			element.Name.LocalName == itemName &&
			(element.Attribute("Include")?.Value.Replace('/', '\\').EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase) ?? false));
		Assert.IsNotNull(item, $"{Path.GetFileName(project)} must contain {itemName} ending with '{expectedSuffix}'.");
	}

	private static void AssertImport(string project, string expectedSuffix)
	{
		var item = XDocument.Load(project).Descendants().FirstOrDefault(element =>
			element.Name.LocalName == "Import" &&
			(element.Attribute("Project")?.Value.Replace('/', '\\').EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase) ?? false));
		Assert.IsNotNull(item, $"{Path.GetFileName(project)} must import '{expectedSuffix}'.");
	}

	private static string GetSampleDirectory()
		=> Path.Combine(FindRepositoryRoot(), "SamplesAvalonia", "07_Testing", "03_RealTime");

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
