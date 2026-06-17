namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Messages;

[TestClass]
public class VolumeProfileBuilderTests : BaseTestClass
{
	[TestMethod]
	public void PriceStepBucketsLevels()
	{
		// Regression: a profile built from a near-continuous source (Level1 SpreadMiddle on a
		// wide-range instrument) used to spawn one level per distinct fractional price, so an
		// hour candle ballooned to thousands of levels and could no longer be served as deep
		// history. With a price step every level must snap to the grid, keeping the count bounded.
		const decimal step = 1m;
		var builder = new VolumeProfileBuilder(step);

		for (var i = 0; i < 1000; i++)
		{
			// 100.000, 100.001 ... 100.999 — all collapse onto the integer grid.
			var price = 100m + i / 1000m;
			builder.Update(price, 1m, Sides.Buy);
		}

		var levels = builder.PriceLevels.ToArray();

		(levels.Length <= 2).AssertTrue($"Expected <=2 grid-bucketed levels, got {levels.Length}");
		levels.All(l => l.Price == decimal.Round(l.Price / step) * step).AssertTrue("Every level price must be snapped to the step grid");
		AreEqual(1000m, levels.Sum(l => l.TotalVolume), "All volume must be preserved across buckets");
	}

	[TestMethod]
	public void NoPriceStepKeepsRawLevels()
	{
		// Back-compat: without a step each distinct price stays its own level.
		var builder = new VolumeProfileBuilder();

		for (var i = 0; i < 100; i++)
			builder.Update(100m + i / 1000m, 1m, Sides.Buy);

		AreEqual(100, builder.PriceLevels.Count(), "Without bucketing each distinct price is its own level");
	}
}
