namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;

[TestClass]
public class TwapAlgoTests : BaseTestClass
{
	private static readonly DateTime Start = new(2026, 5, 27, 9, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime End = new(2026, 5, 27, 10, 0, 0, DateTimeKind.Utc); // 60 minute window

	[TestMethod]
	public void Ctor_RejectsBadArgs()
	{
		Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Buy, 0m, 5, Start, End));
		Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Buy, 100m, 0, Start, End));
		Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Buy, 100m, 5, End, Start));
	}

	[TestMethod]
	public void GetNextAction_NoTickYet_ReturnsNone()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_BeforeStart_ReturnsNone()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start.AddMinutes(-5), 100m, 1m);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_FirstSliceAtStart_RegistersMarketOrder()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(Sides.Buy, a.Side);
		AreEqual(25m, a.Volume); // 100 / 4
		AreEqual(OrderTypes.Market, a.OrderType);
		IsNull(a.Price);
	}

	[TestMethod]
	public void GetNextAction_LimitPrice_RegistersLimitOrder()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 2, Start, End, limitPrice: 150m);
		algo.UpdateMarketData(Start, 150m, 1m);

		var a = algo.GetNextAction();
		AreEqual(OrderTypes.Limit, a.OrderType);
		AreEqual(150m, a.Price);
	}

	[TestMethod]
	public void GetNextAction_OneOrderInFlight_ReturnsNoneUntilFilled()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);
		algo.GetNextAction(); // slice 1
		algo.UpdateMarketData(Start.AddMinutes(15), 100m, 1m); // slice 2 time

		// Slice 1 not filled yet — second call returns None.
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_AfterFill_ContinuesWithNextSlice()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);
		algo.GetNextAction(); // slice 1
		algo.OnOrderMatched(25m);

		algo.UpdateMarketData(Start.AddMinutes(15), 100m, 1m);
		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(25m, a.Volume);
	}

	[TestMethod]
	public void AllSlicesFilled_SumsToTotalVolume()
	{
		var algo = new TwapAlgo(Sides.Sell, 100m, 4, Start, End);
		var time = Start;

		for (var i = 0; i < 4; i++)
		{
			algo.UpdateMarketData(time, 100m, 1m);
			var a = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
			algo.OnOrderMatched(a.Volume!.Value);
			time = time.AddMinutes(15);
		}

		IsTrue(algo.IsFinished);
		AreEqual(0m, algo.RemainingVolume);
	}

	[TestMethod]
	public void Cancel_StopsFurtherSlices()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);
		algo.Cancel();
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void OnOrderFailed_ClearsInFlightFlag()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);
		algo.GetNextAction(); // slice 1
		algo.OnOrderFailed();

		// Same slice tick — but slice already counted, so next call returns None until time advances.
		algo.UpdateMarketData(Start.AddMinutes(15), 100m, 1m); // due slice 2
		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
	}
}
