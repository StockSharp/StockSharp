namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class ChartSampleCompositionTests
{
	private static readonly (string Directory, string Project)[] _samples =
	[
		("01_Chart", "01_Chart.Avalonia"),
		("02_ActiveOrders", "02_Chart.ActiveOrders.Avalonia"),
		("03_Performance", "03_Chart.Performance.Avalonia"),
	];

	[TestMethod]
	[Timeout(10_000)]
	public void ChartHeads_HavePackageAndSourceProjects()
	{
		var root = FindRepositoryRoot();

		foreach (var (directoryName, projectName) in _samples)
		{
			var directory = Path.Combine(root, "SamplesAvalonia", "05_Chart", directoryName);
			var packageProject = Path.Combine(directory, $"{projectName}.csproj");
			var sourceProject = Path.Combine(directory, $"{projectName}_fromsrc.csproj");

			AssertProjectItem(packageProject, "PackageReference", "StockSharp.Xaml.Charting.Avalonia");
			AssertProjectItem(sourceProject, "ProjectReference", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj");
			Assert.IsTrue(File.Exists(Path.Combine(directory, "Program.cs")));
			Assert.IsTrue(File.Exists(Path.Combine(directory, "MainWindow.axaml")));
			Assert.IsTrue(File.Exists(Path.Combine(directory, "MainWindow.axaml.cs")));
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void GeneralChart_PreservesStorageRealtimeColorAndAnnotationLessons()
	{
		var code = ReadCode("01_Chart");

		AssertTokens(code,
			"GetAvailableSecuritiesAsync",
			"GetTickMessageStorage",
			"GetTimeFrameCandleMessageStorage",
			"RandomWalkTradeGenerator",
			"SubscribeIndicatorElement",
			"Colorer",
			"ChartAnnotationData",
			"Dispatcher.UIThread.InvokeAsync",
			"await _loadTask");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void ActiveOrdersChart_PreservesTransactionAndPersistenceLessons()
	{
		var code = ReadCode("02_ActiveOrders");

		AssertTokens(code,
			"RegisterOrder +=",
			"MoveOrder +=",
			"CancelOrder +=",
			"CreateActiveOrdersElement",
			"NeedToDelay",
			"NeedToFail",
			"NeedToConfirm",
			"UseSingleOrderObject",
			"Save(settings)",
			"Load(settings)",
			"await _loadTask");
	}

	[TestMethod]
	[Timeout(10_000)]
	public void PerformanceChart_PreservesPacketRealtimeAndPerfStatsLessons()
	{
		var directory = GetSampleDirectory("03_Performance");
		var markup = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
		var code = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));

		StringAssert.Contains(markup, "ShowPerfStats=\"True\"");
		AssertTokens(code,
			"_candlesPacketSize = 10",
			"Stopwatch.StartNew",
			"GetTickMessageStorage",
			"MyMovingAverage",
			"OnRealtimeTick",
			"Dispatcher.UIThread.InvokeAsync",
			"await _loadTask");
	}

	private static void AssertTokens(string code, params string[] tokens)
	{
		Assert.IsFalse(code.Contains("System.Windows", StringComparison.Ordinal));
		foreach (var token in tokens)
			StringAssert.Contains(code, token);
	}

	private static string ReadCode(string directoryName)
		=> File.ReadAllText(Path.Combine(GetSampleDirectory(directoryName), "MainWindow.axaml.cs"));

	private static string GetSampleDirectory(string directoryName)
		=> Path.Combine(FindRepositoryRoot(), "SamplesAvalonia", "05_Chart", directoryName);

	private static void AssertProjectItem(string project, string itemName, string expectedSuffix)
	{
		Assert.IsTrue(File.Exists(project), $"Missing project: {project}");
		var item = XDocument.Load(project).Descendants().FirstOrDefault(element =>
			element.Name.LocalName == itemName &&
			(element.Attribute("Include")?.Value.Replace('/', '\\').EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase) ?? false));

		Assert.IsNotNull(item, $"{Path.GetFileName(project)} must contain {itemName} ending with '{expectedSuffix}'.");
	}

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
