namespace StockSharp.Tests;

/// <summary>
/// Tests of the comparison harness itself: tolerance, value comparison and row diffs.
/// </summary>
partial class IndicatorTests
{
	// A flag carries no rounding error: an inverted trend direction must fail the GPU/CPU comparison in
	// every tolerance mode, not only the exact one.
	[TestMethod]
	public void CompareValueChecksBoolResultsUnderGpuTolerance()
	{
		var ind = new SuperTrend();
		var t = DateTime.UtcNow;
		const string name = nameof(SuperTrend);

		static SuperTrendIndicatorValue trend(IIndicator indicator, decimal price, bool isUpTrend, DateTime time)
			=> new(indicator, price, isUpTrend, time) { IsFinal = true, IsFormed = true };

		CompareValue(trend(ind, 100m, true, t), trend(ind, 100m, true, t), name, true, GpuTolerance(7000m, 20));

		Throws<AssertFailedException>(() => CompareValue(trend(ind, 100m, false, t), trend(ind, 100m, true, t), name, true, GpuTolerance(7000m, 20)));
		Throws<AssertFailedException>(() => CompareValue(trend(ind, 100m, false, t), trend(ind, 100m, true, t), name, true));
	}

	// The GPU allowance is formed from the magnitudes the kernel works on - the scale of its inputs and the
	// number of bars it accumulates - so the same difference passes or fails by where it came from, never by
	// how big the compared value happens to be.
	[TestMethod]
	public void CompareValueGpuToleranceComesFromInputScaleNotResultSize()
	{
		var ind = new PassThroughIndicator();
		var t = DateTime.UtcNow;
		const string name = nameof(PassThroughIndicator);
		const int window = 20;

		static DecimalIndicatorValue dec(IIndicator indicator, decimal v, DateTime time)
			=> new(indicator, v, time) { IsFinal = true, IsFormed = true };

		// float32 drift accumulated over 20 bars of a 7000 price scale.
		CompareValue(dec(ind, 1000.02m, t), dec(ind, 1000m, t), name, true, GpuTolerance(7000m, window));

		// The same pair read off a series that never leaves the hundreds is beyond what the arithmetic
		// accounts for, though the compared value is as large as before.
		Throws<AssertFailedException>(() => CompareValue(dec(ind, 1000.02m, t), dec(ind, 1000m, t), name, true, GpuTolerance(100m, window)));

		// Half a unit is a defect at every scale this suite runs on.
		Throws<AssertFailedException>(() => CompareValue(dec(ind, 1000.5m, t), dec(ind, 1000m, t), name, true, GpuTolerance(7000m, window)));

		Throws<AssertFailedException>(() => CompareValue(dec(ind, 0.5m, t), dec(ind, 1.4m, t), name, true, GpuTolerance(7000m, window)));
	}

	// The allowance grows with the window because a kernel rounds once per accumulated bar, and with the
	// scale because float32 rounds relative to the magnitude it works on. Even over a long window it stays
	// far below the whole units a band proportional to the result forgave.
	[TestMethod]
	public void GpuToleranceGrowsWithWindowAndScale()
	{
		GpuTolerance(7000m, 40).AssertEqual(GpuTolerance(7000m, 20) * 2);
		GpuTolerance(14000m, 20).AssertEqual(GpuTolerance(7000m, 20) * 2);

		// A window of zero bars still rounds once.
		GpuTolerance(7000m, 0).AssertEqual(GpuTolerance(7000m, 1));

		GpuTolerance(7000m, 200).AssertLess(7000m * 0.001m);
	}

	// A reference value that a fresh run no longer produces is a regression, whether the row went empty
	// altogether or only in a column past the ones still produced.
	[TestMethod]
	public void ValidatedDiffsReportsLostValues()
	{
		static string[] diffs(string[] rows, string[] committed)
			=> new IndicatorDataRunner.RenderedSeries { Rows = rows, FormedFrom = 0, Epsilon = 0.1m }.ValidatedDiffs(committed);

		diffs(["1,2"], ["1,2"]).Length.AssertEqual(0);
		diffs(["1,2"], ["1.05,2"]).Length.AssertEqual(0);
		diffs(["1,"], ["1,"]).Length.AssertEqual(0);

		diffs([""], ["5"]).Length.AssertEqual(1);
		diffs(["1"], ["1,2"]).Length.AssertEqual(1);
		diffs([",2"], ["1,2"]).Length.AssertEqual(1);
		diffs(["1,2"], ["1,"]).Length.AssertEqual(1);
	}

	// The row rule Check() runs on covers the union of both column sets, so neither a dropped nor an
	// unexpected column can go unread.
	[TestMethod]
	public void RowDiffsAccountsForEveryExpectedColumn()
	{
		static int count(decimal?[] produced, decimal?[] reference)
			=> IndicatorDataRunner.RowDiffs(produced, reference, 0.1m, 1).Count();

		count([1m, 2m], [1m, 2m]).AssertEqual(0);
		count([null, 2m], [null, 2m]).AssertEqual(0);
		count([1m], [1.05m]).AssertEqual(0);

		count([null, 2m], [1m, 2m]).AssertEqual(1);
		count([], [1m]).AssertEqual(1);
		count([1m], [1m, 2m]).AssertEqual(1);
		count([1m, 2m], [1m]).AssertEqual(1);
		count([1m], [5m]).AssertEqual(1);
	}
}
