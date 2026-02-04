namespace StockSharp.Tests;

using StockSharp.Algo.Strategies.Optimization;

[TestClass]
public class OptimizationProgressTrackerTests : BaseTestClass
{
	[TestMethod]
	public void Reset_SetsInitialState()
	{
		var tracker = new OptimizationProgressTracker();

		tracker.Reset(100);

		AreEqual(100, tracker.TotalIterations);
		AreEqual(0, tracker.CompletedIterations);
		AreEqual(0, tracker.TotalProgress);
	}

	[TestMethod]
	public void IterationCompleted_IncrementsCount()
	{
		var tracker = new OptimizationProgressTracker();
		tracker.Reset(10);

		tracker.IterationCompleted();
		tracker.IterationCompleted();

		AreEqual(2, tracker.CompletedIterations);
	}

	[TestMethod]
	public void TotalProgress_CalculatesCorrectly()
	{
		var tracker = new OptimizationProgressTracker();
		tracker.Reset(10);

		for (var i = 0; i < 5; i++)
			tracker.IterationCompleted();

		AreEqual(50, tracker.TotalProgress);
	}

	[TestMethod]
	public void TotalProgress_CapsAt100()
	{
		var tracker = new OptimizationProgressTracker();
		tracker.Reset(10);

		for (var i = 0; i < 15; i++) // more than total
			tracker.IterationCompleted();

		AreEqual(100, tracker.TotalProgress);
	}

	[TestMethod]
	public void TotalProgress_ZeroIterations_ReturnsZero()
	{
		var tracker = new OptimizationProgressTracker();
		tracker.Reset(0);

		AreEqual(0, tracker.TotalProgress);
	}

	[TestMethod]
	public void Remaining_InitiallyMaxValue()
	{
		var tracker = new OptimizationProgressTracker();
		tracker.Reset(100);

		AreEqual(TimeSpan.MaxValue, tracker.Remaining);
	}

	[TestMethod]
	public void Reset_ThrowsOnNegative()
	{
		var tracker = new OptimizationProgressTracker();

		ThrowsExactly<ArgumentOutOfRangeException>(() => tracker.Reset(-1));
	}

	[TestMethod]
	public async Task Elapsed_IncreasesOverTime()
	{
		var tracker = new OptimizationProgressTracker();
		tracker.Reset(100);

		var elapsed1 = tracker.Elapsed;
		await Task.Delay(50, CancellationToken);
		var elapsed2 = tracker.Elapsed;

		IsTrue(elapsed2 > elapsed1);
	}
}