namespace StockSharp.Tests;

using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Optimization;
using StockSharp.Designer;

[TestClass]
// Each window runs a whole brute-force search across every core.
[DoNotParallelize]
public class WalkForwardOptimizerTests : BaseTestClass
{
	private static readonly DateTime _start = new(2026, 3, 2, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// The windows step forward by the out-of-sample length, each testing on the stretch right after the
	/// one it was fitted on, and stop where a whole out-of-sample stretch no longer fits.
	/// </summary>
	[TestMethod]
	public void WindowsTileTheRangeForward()
	{
		var windows = WalkForwardWindow.Split(_start, _start.AddDays(5), TimeSpan.FromDays(2), TimeSpan.FromDays(1));

		windows.Count.AssertEqual(3);

		for (var i = 0; i < windows.Count; i++)
		{
			var window = windows[i];

			window.InSampleFrom.AssertEqual(_start.AddDays(i));
			window.InSampleTo.AssertEqual(_start.AddDays(i + 2));
			window.OutOfSampleFrom.AssertEqual(window.InSampleTo);
			window.OutOfSampleTo.AssertEqual(_start.AddDays(i + 3));
		}
	}

	/// <summary>A range shorter than one window has no window in it.</summary>
	[TestMethod]
	public void ARangeTooShortHasNoWindows()
	{
		WalkForwardWindow.Split(_start, _start.AddDays(2), TimeSpan.FromDays(2), TimeSpan.FromDays(1)).Count.AssertEqual(0);
	}

	/// <summary>Lengths that cannot form a window are refused.</summary>
	[TestMethod]
	public void EmptyLengthsAreRefused()
	{
		ThrowsExactly<ArgumentOutOfRangeException>(() => WalkForwardWindow.Split(_start, _start.AddDays(5), TimeSpan.Zero, TimeSpan.FromDays(1)));
		ThrowsExactly<ArgumentOutOfRangeException>(() => WalkForwardWindow.Split(_start, _start.AddDays(5), TimeSpan.FromDays(1), TimeSpan.Zero));
	}

	/// <summary>
	/// Every window is fitted on its own in-sample stretch, and the setting the fitness preferred there is
	/// the one run on the stretch after it.
	/// </summary>
	/// <remarks>
	/// The fitness here prefers the largest short average outright, so which setting each window must pick
	/// is known before anything runs: the largest short length the grid offers.
	/// </remarks>
	[TestMethod]
	public async Task EachWindowTestsTheSettingItsInSampleChose()
	{
		var security = new Security { Id = Paths.HistoryDefaultSecurity };
		var portfolio = Portfolio.CreateSimulator();

		var strategy = new SmaStrategy
		{
			Security = security,
			Portfolio = portfolio,
			Volume = 1,
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),
			Short = 10,
			Long = 80,
		};

		var shortParam = (StrategyParam<int>)strategy.Parameters[nameof(SmaStrategy.Short)];
		shortParam.SetCanOptimize(true).SetOptimize(10, 30, 10);

		using var optimizer = new BruteForceOptimizer(
			new CollectionSecurityProvider([security]),
			new CollectionPortfolioProvider([portfolio]),
			Helper.FileSystem.GetStorage(Paths.HistoryDataPath));

		var walkForward = new WalkForwardOptimizer(optimizer);

		var windows = WalkForwardWindow.Split(Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(4), TimeSpan.FromDays(2), TimeSpan.FromDays(1));

		var steps = new List<WalkForwardStep>();

		await foreach (var step in walkForward.RunAsync(strategy, [shortParam], windows, s => ((SmaStrategy)s).Short, CancellationToken))
			steps.Add(step);

		steps.Count.AssertEqual(windows.Count);

		for (var i = 0; i < steps.Count; i++)
		{
			var step = steps[i];

			step.Window.AssertEqual(windows[i]);
			step.Parameters[nameof(SmaStrategy.Short)].AssertEqual(30);
			step.InSampleFitness.AssertEqual(30m);

			((SmaStrategy)step.OutOfSample).Short.AssertEqual(30);
			((SmaStrategy)step.OutOfSample).Long.AssertEqual(80);
		}
	}
}
