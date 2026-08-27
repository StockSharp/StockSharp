namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class BasicSampleInventoryTests
{
	private static readonly (string RelativeDirectory, string ProjectName)[] _basicSamples =
	[
		("01_Basic/01_ConnectAndDownloadInstruments", "01_Basic.ConnectAndDownloadInstruments.Avalonia"),
		("01_Basic/02_MarketDepths", "02_Basic.MarketDepths.Avalonia"),
		("01_Basic/03_Orders", "03_Basic.Orders.Avalonia"),
	];

	[TestMethod]
	[Timeout(10_000)]
	public void BasicHeads_HavePackageAndSourceProjectTwins()
	{
		var root = FindRepositoryRoot();

		foreach (var (relativeDirectory, projectName) in _basicSamples)
		{
			var directory = Path.Combine(root, "SamplesAvalonia", relativeDirectory.Replace('/', Path.DirectorySeparatorChar));
			var packageProject = Path.Combine(directory, $"{projectName}.csproj");
			var sourceProject = Path.Combine(directory, $"{projectName}_fromsrc.csproj");

			Assert.IsTrue(File.Exists(packageProject), $"Missing package project: {packageProject}");
			Assert.IsTrue(File.Exists(sourceProject), $"Missing source project: {sourceProject}");
			AssertProjectContract(packageProject, "PackageReference", "StockSharp.Xaml.Avalonia");
			AssertProjectContract(sourceProject, "ProjectReference", "Xaml.Avalonia\\Xaml.Avalonia.csproj");
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void BasicHeads_UseSharedAvaloniaScaffold()
	{
		var root = FindRepositoryRoot();
		var commonProps = Path.Combine(root, "SamplesAvalonia", "common_samples_avalonia.props");

		Assert.IsTrue(File.Exists(commonProps), $"Missing shared Avalonia props: {commonProps}");

		foreach (var (relativeDirectory, projectName) in _basicSamples)
		{
			var project = Path.Combine(
				root,
				"SamplesAvalonia",
				relativeDirectory.Replace('/', Path.DirectorySeparatorChar),
				$"{projectName}.csproj");
			var document = XDocument.Load(project);
			var import = document.Root?.Elements().FirstOrDefault(element => element.Name.LocalName == "Import");

			Assert.IsNotNull(import, $"Missing common props import in {project}");
			var importedPath = Path.GetFullPath(Path.Combine(
				Path.GetDirectoryName(project)!,
				import.Attribute("Project")?.Value ?? string.Empty));
			Assert.AreEqual(commonProps, importedPath, $"Unexpected common props import in {project}");
		}
	}

	private static void AssertProjectContract(string project, string itemName, string includeSuffix)
	{
		var document = XDocument.Load(project);
		var item = document.Descendants()
			.FirstOrDefault(element =>
				element.Name.LocalName == itemName &&
				(element.Attribute("Include")?.Value.Replace('/', '\\').EndsWith(includeSuffix, StringComparison.OrdinalIgnoreCase) ?? false));

		Assert.IsNotNull(item, $"{Path.GetFileName(project)} must contain {itemName} ending with '{includeSuffix}'.");
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
