namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class OfflineSampleCompositionTests
{
	private static readonly (string RelativeDirectory, string ProjectName, string PackageName, string SourceProject)[] _samples =
	[
		("04_Indicators/01_SimpleSMA", "01_Indicators.SimpleSMA.Avalonia", "StockSharp.Xaml.Charting.Avalonia", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj"),
		("04_Indicators/02_ComplexBollinger", "02_Indicators.ComplexBollinger.Avalonia", "StockSharp.Xaml.Charting.Avalonia", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj"),
		("04_Indicators/03_CreateOwn", "03_Indicators.CreateOwn.Avalonia", "StockSharp.Xaml.Charting.Avalonia", "Xaml.Charting.Avalonia\\Xaml.Charting.Avalonia.csproj"),
		("08_Misc/01_Logging", "01_Misc.Logging.Avalonia", "StockSharp.Xaml.Avalonia", "Xaml.Avalonia\\Xaml.Avalonia.csproj"),
	];

	[TestMethod]
	[Timeout(10_000)]
	public void OfflineHeads_HavePackageAndSourceTwins()
	{
		var root = FindRepositoryRoot();

		foreach (var (relativeDirectory, projectName, packageName, sourceProject) in _samples)
		{
			var directory = GetSampleDirectory(root, relativeDirectory);
			var packageProject = Path.Combine(directory, $"{projectName}.csproj");
			var sourceProjectFile = Path.Combine(directory, $"{projectName}_fromsrc.csproj");

			AssertProjectItem(packageProject, "PackageReference", packageName);
			AssertProjectItem(sourceProjectFile, "ProjectReference", sourceProject);
			Assert.IsTrue(File.Exists(Path.Combine(directory, "Program.cs")), $"Missing Program.cs in {directory}.");
			Assert.IsTrue(File.Exists(Path.Combine(directory, "MainWindow.axaml")), $"Missing MainWindow.axaml in {directory}.");
			Assert.IsTrue(File.Exists(Path.Combine(directory, "MainWindow.axaml.cs")), $"Missing MainWindow.axaml.cs in {directory}.");
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void IndicatorHeads_UseChartDispatcherAndCancelableLifetime()
	{
		var root = FindRepositoryRoot();

		foreach (var sample in _samples.Where(sample => sample.RelativeDirectory.StartsWith("04_Indicators/", StringComparison.Ordinal)))
		{
			var directory = GetSampleDirectory(root, sample.RelativeDirectory);
			var markup = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
			var code = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));

			StringAssert.Contains(markup, "ChartControl");
			StringAssert.Contains(code, "Dispatcher.UIThread.InvokeAsync");
			StringAssert.Contains(code, "CancellationTokenSource");
			StringAssert.Contains(code, "await _loadTask");
			Assert.IsFalse(code.Contains("System.Windows", StringComparison.Ordinal), $"WPF dependency found in {directory}.");
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void LoggingHead_UsesMonitorListenerAndDeterministicCleanup()
	{
		var directory = GetSampleDirectory(FindRepositoryRoot(), "08_Misc/01_Logging");
		var markup = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml"));
		var code = File.ReadAllText(Path.Combine(directory, "MainWindow.axaml.cs"));

		StringAssert.Contains(markup, "windows:Monitor");
		StringAssert.Contains(code, "new GuiLogListener(_monitor)");
		StringAssert.Contains(code, "_logManager.Dispose()");
		StringAssert.Contains(code, "((IDisposable)_monitor).Dispose()");
		Assert.IsFalse(code.Contains("System.Windows", StringComparison.Ordinal));
	}

	private static void AssertProjectItem(string project, string itemName, string expectedSuffix)
	{
		Assert.IsTrue(File.Exists(project), $"Missing project: {project}");
		var document = XDocument.Load(project);
		var item = document.Descendants().FirstOrDefault(element =>
			element.Name.LocalName == itemName &&
			(element.Attribute("Include")?.Value.Replace('/', '\\').EndsWith(expectedSuffix, StringComparison.OrdinalIgnoreCase) ?? false));

		Assert.IsNotNull(item, $"{Path.GetFileName(project)} must contain {itemName} ending with '{expectedSuffix}'.");
	}

	private static string GetSampleDirectory(string root, string relativeDirectory)
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
