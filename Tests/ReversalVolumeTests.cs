namespace StockSharp.Tests;

/// <summary>
/// The volume that turns a position around. What it has to guarantee is a ceiling: the naive
/// Volume + |Position| has none, and a position read larger than the strategy holds then feeds back into an
/// order larger still.
/// </summary>
[TestClass]
public class ReversalVolumeTests : BaseTestClass
{
	private static Strategy CreateStrategy(decimal volume, decimal position)
		=> new() { Volume = volume, Position = position };

	[TestMethod]
	public void WithNothingHeldItOpensTheDraftedVolume()
	{
		AreEqual(1m, CreateStrategy(1m, 0m).ReversalVolume());
		AreEqual(5m, CreateStrategy(5m, 0m).ReversalVolume());
	}

	[TestMethod]
	public void ItClosesWhatIsHeldAndOpensTheSameAgain()
	{
		// Holding one, the reversal sells two: one to flatten and one to stand on the other side.
		AreEqual(2m, CreateStrategy(1m, 1m).ReversalVolume());
		AreEqual(2m, CreateStrategy(1m, -1m).ReversalVolume());
		AreEqual(6m, CreateStrategy(3m, 3m).ReversalVolume());
	}

	[TestMethod]
	public void APartialHoldingIsTurnedAroundInFull()
	{
		AreEqual(1m, CreateStrategy(1m, 0.5m).ReversalVolume());
		AreEqual(1m, CreateStrategy(1m, -0.5m).ReversalVolume());
	}

	[TestMethod]
	public void MoreHeldThanDraftedStillTradesAtMostTwiceTheDraft()
	{
		// The ceiling. An emulated position is also moved by candle processing, order matching and ProcessTime,
		// so it reads larger than the strategy's own fills; without this cap each reversal doubles the last.
		AreEqual(2m, CreateStrategy(1m, 5m).ReversalVolume());
		AreEqual(2m, CreateStrategy(1m, -5m).ReversalVolume());
		AreEqual(2m, CreateStrategy(1m, 1_000_000m).ReversalVolume());
	}

	[TestMethod]
	public void TheSideHeldDoesNotChangeHowMuchIsTraded()
	{
		AreEqual(
			CreateStrategy(2m, 7m).ReversalVolume(),
			CreateStrategy(2m, -7m).ReversalVolume());
	}

	[TestMethod]
	public void ThereIsNoStrategyToReverse()
	{
		Throws<ArgumentNullException>(() => ((Strategy)null).ReversalVolume());
	}
}
