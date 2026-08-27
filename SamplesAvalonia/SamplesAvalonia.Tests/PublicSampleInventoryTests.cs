namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.Collections.Generic;
using System.IO;

using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class PublicSampleInventoryTests
{
	public static IEnumerable<object[]> UiSampleCases
	{
		get
		{
			yield return Case("01_Basic/01_ConnectAndDownloadInstruments", "01_Basic.ConnectAndDownloadInstruments.Avalonia");
			yield return Case("01_Basic/02_MarketDepths", "02_Basic.MarketDepths.Avalonia");
			yield return Case("01_Basic/03_Orders", "03_Basic.Orders.Avalonia");
			yield return Case("02_Candles/01_Realtime", "01_Candles.Realtime.Avalonia");
			yield return Case("02_Candles/02_CombineHistoryRealtime", "02_Candles.CombineHistoryRealtime.Avalonia");
			yield return Case("03_Storage/04_HydraServerConnect", "04_Storage.HydraServerConnect.Avalonia");
			yield return Case("04_Indicators/01_SimpleSMA", "01_Indicators.SimpleSMA.Avalonia");
			yield return Case("04_Indicators/02_ComplexBollinger", "02_Indicators.ComplexBollinger.Avalonia");
			yield return Case("04_Indicators/03_CreateOwn", "03_Indicators.CreateOwn.Avalonia");
			yield return Case("05_Chart/01_Chart", "01_Chart.Avalonia");
			yield return Case("05_Chart/02_ActiveOrders", "02_Chart.ActiveOrders.Avalonia");
			yield return Case("05_Chart/03_Performance", "03_Chart.Performance.Avalonia");
			yield return Case("06_Strategies/01_HistorySMA", "01_Strategies.HistorySMA.Avalonia");
			yield return Case("06_Strategies/02_HistoryBollingerBands", "02_Strategies.HistoryBollingerBands.Avalonia");
			yield return Case("06_Strategies/03_HistoryTrend", "03_Strategies.HistoryTrend.Avalonia");
			yield return Case("06_Strategies/04_HistoryMarketRule", "04_Strategies.HistoryMarketRule.Avalonia");
			yield return Case("06_Strategies/05_HistoryIndex", "05_Strategies.HistoryIndex.Avalonia");
			yield return Case("06_Strategies/06_HistoryQuoting", "06_Strategies.HistoryQuoting.Avalonia");
			yield return Case("06_Strategies/07_LiveSpread", "07_Strategies.LiveSpread.Avalonia");
			yield return Case("06_Strategies/08_LiveArbitrage", "08_Strategies.LiveArbitrage.Avalonia");
			yield return Case("06_Strategies/09_LiveOptionsQuoting", "09_Strategies.LiveOptionsQuoting.Avalonia");
			yield return Case("06_Strategies/10_LiveTerminal", "10_Strategies.LiveTerminal.Avalonia");
			yield return Case("07_Testing/01_History", "01_Testing.History.Avalonia");
			yield return Case("07_Testing/02_Optimization", "02_Testing.Optimization.Avalonia");
			yield return Case("07_Testing/03_RealTime", "03_Testing.RealTime.Avalonia");
			yield return Case("08_Misc/01_Logging", "01_Misc.Logging.Avalonia");
			yield return Case("09_Advanced/01_MultiConnect", "01_Advanced.MultiConnect.Avalonia");
			yield return Case("09_Advanced/02_StoreDataLocal", "02_Advanced.SaveDataLocal.Avalonia");
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	[DynamicData(nameof(UiSampleCases))]
	public void EveryWpfUiSampleHasPackageAndSourceAvaloniaHeads(string relativeDirectory, string projectName)
	{
		var directory = Path.Combine(
			FindRepositoryRoot(),
			"SamplesAvalonia",
			relativeDirectory.Replace('/', Path.DirectorySeparatorChar));

		Assert.IsTrue(
			File.Exists(Path.Combine(directory, $"{projectName}.csproj")),
			$"Missing Avalonia package head for {relativeDirectory}.");
		Assert.IsTrue(
			File.Exists(Path.Combine(directory, $"{projectName}_fromsrc.csproj")),
			$"Missing Avalonia source head for {relativeDirectory}.");
	}

	private static object[] Case(string relativeDirectory, string projectName)
		=> [relativeDirectory, projectName];

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
