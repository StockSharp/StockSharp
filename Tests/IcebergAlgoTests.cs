namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;

[TestClass]
public class IcebergAlgoTests : BaseTestClass
{
	[TestMethod]
	public void Ctor_RejectsBadArgs()
	{
		Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 0m, 10m, 150m));
		Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, 0m, 150m));
		Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, 200m, 150m));
		Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, 10m, 0m));
	}

	[TestMethod]
	public void FirstAction_RegistersFirstSlice_AtDisplayVolume()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		var a = algo.GetNextAction();

		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(25m, a.Volume);
		AreEqual(150m, a.Price);
		AreEqual(OrderTypes.Limit, a.OrderType);
	}

	[TestMethod]
	public void SliceFilled_NextSliceEmitted()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderMatched(25m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(25m, a.Volume);
	}

	[TestMethod]
	public void LastSlice_ClampedToRemaining()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 70m, displayVolume: 25m, limitPrice: 150m);

		for (var i = 0; i < 2; i++)
		{
			algo.GetNextAction();
			algo.OnOrderMatched(25m);
		}

		// Remaining = 20; next slice = min(25, 20) = 20
		var a = algo.GetNextAction();
		AreEqual(20m, a.Volume);
	}

	[TestMethod]
	public void AllVolumeFilled_Finishes()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 50m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderMatched(25m);
		algo.GetNextAction();
		algo.OnOrderMatched(25m);

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void OrderInFlight_ReturnsNoneOnSecondCall()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void OrderCanceled_PartiallyFilled_ContinuesWithRemaining()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderCanceled(10m); // partial

		AreEqual(90m, algo.RemainingVolume);
		var a = algo.GetNextAction();
		AreEqual(25m, a.Volume); // next display slice
	}
}
