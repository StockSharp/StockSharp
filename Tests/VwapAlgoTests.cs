namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;

[TestClass]
public class VwapAlgoTests : BaseTestClass
{
	private static readonly DateTime Start = new(2026, 5, 27, 9, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime End = new(2026, 5, 27, 10, 0, 0, DateTimeKind.Utc);

	[TestMethod]
	public void Ctor_RejectsBadArgs()
	{
		Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 0m, Start, End));
		Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, End, Start));
		Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, End, participationRate: 0m));
		Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, End, participationRate: 1.5m));
		Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, End, minSliceVolume: 0m));
	}

	[TestMethod]
	public void GetNextAction_ParticipationCappedAtRate()
	{
		// 10% participation, market trades 100 → algo wants 10. minSlice=1.
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		var a = algo.GetNextAction();

		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(10m, a.Volume);
	}

	[TestMethod]
	public void GetNextAction_BelowMinSlice_ReturnsNone()
	{
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 10m);
		algo.UpdateMarketData(Start, 100m, 50m); // 10% × 50 = 5 < minSlice 10

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_CumulativeMarketVolume_AdjustsSliceTarget()
	{
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);

		algo.UpdateMarketData(Start, 100m, 100m); // market=100, target=10, slice=10
		var a1 = algo.GetNextAction();
		algo.OnOrderMatched(a1.Volume!.Value);

		algo.UpdateMarketData(Start.AddSeconds(10), 100m, 200m); // market=300, target=30, slice = 30 - 10 = 20
		var a2 = algo.GetNextAction();
		AreEqual(20m, a2.Volume);
	}

	[TestMethod]
	public void GetNextAction_TotalVolumeReached_Finishes()
	{
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 50m, Start, End, participationRate: 1.0m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m); // target=100 capped at 50
		var a = algo.GetNextAction();
		AreEqual(50m, a.Volume);
		algo.OnOrderMatched(50m);

		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void EndAtPassed_FinishesEvenWithRemainder()
	{
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 1000m, Start, End, participationRate: 0.01m, minSliceVolume: 1m);
		algo.UpdateMarketData(End.AddMinutes(5), 100m, 100m); // past EndAt

		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void Cancel_Finishes()
	{
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 1000m, Start, End);
		algo.UpdateMarketData(Start, 100m, 100m);
		algo.Cancel();
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	// V041 - a failed slice must roll back only the in-flight (sent-but-unfilled) slice, not the
	// whole remaining order; otherwise the algorithm forgets earlier sends and re-sends them,
	// breaching the participation cap.
	[TestMethod]
	public void FailedSlice_RollsBackOnlyTheInFlightSlice()
	{
		// total=1000, filled=200 (earlier fills), sent=300 (200 filled + 100 in-flight).
		AreEqual(200m, VwapAlgo.SentVolumeAfterFailedSlice(sentVolume: 300m, filledVolume: 200m, totalVolume: 1000m));
	}
}
