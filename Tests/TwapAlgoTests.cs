namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;

[TestClass]
public class TwapAlgoTests : BaseTestClass
{
	private static readonly DateTime Start = new(2026, 5, 27, 9, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime End = new(2026, 5, 27, 10, 0, 0, DateTimeKind.Utc); // 60 minute window

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
		algo.UpdateMarketData(Start, 140m, 1m);

		var a = algo.GetNextAction();
		AreEqual(OrderTypes.Limit, a.OrderType);
		AreEqual(150m, a.Price);
	}

	[TestMethod]
	public void GetNextAction_OrderInFlight_ReturnsNone()
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
			algo.OnOrderMatched(a.Volume.Value);
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
	public void OnOrderFailed_NextDueSliceCarriesWhatTheFailedOneDidNotExecute()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);
		algo.GetNextAction(); // slice 1
		algo.OnOrderFailed();

		// The time of the failed slice has passed, so nothing goes out until the next one is due.
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateMarketData(Start.AddMinutes(15), 100m, 1m); // due slice 2
		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(100m / 3m, a.Volume); // what never executed is spread over the slices that are left
		AreEqual(100m, algo.RemainingVolume);
	}

	[TestMethod]
	public void Ctor_NonPositiveTotalVolume_ThrowsWithOffendingValue()
	{
		var zero = Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Buy, 0m, 5, Start, End));
		AreEqual("totalVolume", zero.ParamName);
		AreEqual(0m, (decimal)zero.ActualValue);

		var negative = Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Sell, -1m, 5, Start, End));
		AreEqual("totalVolume", negative.ParamName);
		AreEqual(-1m, (decimal)negative.ActualValue);
	}

	[TestMethod]
	public void Ctor_NonPositiveSliceCount_ThrowsWithOffendingValue()
	{
		var zero = Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Buy, 100m, 0, Start, End));
		AreEqual("sliceCount", zero.ParamName);
		AreEqual(0, (int)zero.ActualValue);

		var negative = Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Buy, 100m, -1, Start, End));
		AreEqual("sliceCount", negative.ParamName);
		AreEqual(-1, (int)negative.ActualValue);
	}

	[TestMethod]
	public void Ctor_EndAtNotAfterStartAt_ThrowsWithOffendingValue()
	{
		var equal = Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Buy, 100m, 5, Start, Start));
		AreEqual("endAt", equal.ParamName);
		AreEqual(Start, (DateTime)equal.ActualValue);

		var before = Throws<ArgumentOutOfRangeException>(() => new TwapAlgo(Sides.Buy, 100m, 5, End, Start));
		AreEqual("endAt", before.ParamName);
		AreEqual(Start, (DateTime)before.ActualValue);
	}

	[TestMethod]
	public void Ctor_MinimalValidArgs_Accepted()
	{
		var algo = new TwapAlgo(Sides.Buy, 0.00000001m, 1, Start, Start.AddTicks(1));

		AreEqual(1, algo.SliceCount);
		AreEqual(0.00000001m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void Ctor_ValidArgs_ExposesThemAndStartsUnfinished()
	{
		var market = new TwapAlgo(Sides.Sell, 100m, 4, Start, End);

		AreEqual(Sides.Sell, market.Side);
		AreEqual(4, market.SliceCount);
		AreEqual(Start, market.StartAt);
		AreEqual(End, market.EndAt);
		IsNull(market.LimitPrice);
		AreEqual(100m, market.RemainingVolume);
		IsFalse(market.IsFinished);

		var limit = new TwapAlgo(Sides.Buy, 100m, 4, Start, End, limitPrice: 12.5m);
		AreEqual(12.5m, limit.LimitPrice);
	}

	[TestMethod]
	public void GetNextAction_SingleSlice_RegistersWholeVolume()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 1, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(100m, a.Volume);

		algo.OnOrderMatched(100m);
		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_TickWithoutPrice_StillSchedulesSlice()
	{
		// TWAP is driven by the clock alone, so a tick that carries no price still starts the schedule.
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, null, null);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(25m, a.Volume);
	}

	[TestMethod]
	public void UpdateOrderBook_WithoutTick_KeepsAlgoIdle()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);

		algo.UpdateOrderBook(new QuoteChangeMessage
		{
			ServerTime = Start.AddMinutes(30),
			Bids = [new QuoteChange(99m, 10m)],
			Asks = [new QuoteChange(101m, 10m)],
		});
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateOrderBook(new QuoteChangeMessage
		{
			ServerTime = Start.AddMinutes(30),
			Bids = [],
			Asks = [],
		});
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateMarketData(Start, 100m, 1m);
		AreEqual(PositionModifyAction.ActionTypes.Register, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_JustBeforeLastSliceTime_ReturnsNoneThenRegistersAtIt()
	{
		// Slice k is due at StartAt + k * (EndAt - StartAt) / SliceCount, so with four slices
		// over an hour the last one is due at StartAt + 45 minutes and not a second earlier.
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);

		foreach (var minutes in new[] { 0, 15, 30 })
		{
			algo.UpdateMarketData(Start.AddMinutes(minutes), 100m, 1m);
			var slice = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, slice.ActionType);
			algo.OnOrderMatched(slice.Volume.Value);
		}

		algo.UpdateMarketData(Start.AddMinutes(44).AddSeconds(59), 100m, 1m);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateMarketData(Start.AddMinutes(45), 100m, 1m);
		var last = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, last.ActionType);
		AreEqual(25m, last.Volume);

		algo.OnOrderMatched(last.Volume.Value);
		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void GetNextAction_ManySlicesDueAtOnce_SendsThemOneByOne()
	{
		// First tick arrives at StartAt + 46 minutes: all four slice times have already passed.
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start.AddMinutes(46), 100m, 1m);

		var first = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, first.ActionType);
		AreEqual(25m, first.Volume);

		// Only one child order at a time, even with several slices overdue.
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
		algo.OnOrderMatched(first.Volume.Value);

		for (var i = 0; i < 3; i++)
		{
			var slice = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, slice.ActionType);
			AreEqual(25m, slice.Volume);
			algo.OnOrderMatched(slice.Volume.Value);
		}

		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_TotalNotDivisibleBySliceCount_SlicesSumToTotal()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 3, Start, End);
		var volumes = new List<decimal>();

		foreach (var minutes in new[] { 0, 20, 40 })
		{
			algo.UpdateMarketData(Start.AddMinutes(minutes), 100m, 1m);
			var slice = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, slice.ActionType);
			volumes.Add(slice.Volume.Value);
			algo.OnOrderMatched(slice.Volume.Value);
		}

		AreEqual(100m / 3m, volumes[0]);

		foreach (var volume in volumes)
			IsTrue(Math.Abs(volume - 100m / 3m) < 0.0001m);

		AreEqual(100m, volumes.Sum());
		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void GetNextAction_SellWithLimitPrice_RegistersAtSuppliedPriceWhereverTheMarketIs()
	{
		var algo = new TwapAlgo(Sides.Sell, 100m, 2, Start, End, limitPrice: 90m);

		algo.UpdateMarketData(Start, 88m, 1m);

		var below = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, below.ActionType);
		AreEqual(Sides.Sell, below.Side);
		AreEqual(OrderTypes.Limit, below.OrderType);
		AreEqual(90m, below.Price);
		AreEqual(50m, below.Volume);

		algo.OnOrderMatched(below.Volume.Value);
		algo.UpdateMarketData(Start.AddMinutes(30), 95m, 1m);

		var above = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, above.ActionType);
		AreEqual(90m, above.Price);
		AreEqual(50m, above.Volume);
	}

	[TestMethod]
	public void OnOrderCanceled_PartialFill_RedistributesRemainderOverRemainingSlices()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);

		var first = algo.GetNextAction();
		AreEqual(25m, first.Volume);

		algo.OnOrderCanceled(10m);
		AreEqual(90m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);

		algo.UpdateMarketData(Start.AddMinutes(15), 100m, 1m);
		var second = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, second.ActionType);
		AreEqual(30m, second.Volume); // 90 left over the three remaining slices
	}

	[TestMethod]
	public void OnOrderMatched_MoreThanRequested_ClampsRemainingAndFinishes()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);
		algo.GetNextAction();

		algo.OnOrderMatched(150m);

		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void IsFinished_LastSliceSentButNotFilled_False()
	{
		// The algo reports finished once the total volume is exhausted or EndAt has passed.
		// Dispatching the last slice is neither: half the volume is still working in the market.
		var algo = new TwapAlgo(Sides.Buy, 100m, 2, Start, End);

		algo.UpdateMarketData(Start, 100m, 1m);
		var first = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, first.ActionType);
		algo.OnOrderMatched(first.Volume.Value);

		algo.UpdateMarketData(Start.AddMinutes(30), 100m, 1m);
		var last = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, last.ActionType);
		AreEqual(50m, last.Volume);

		AreEqual(50m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void GetNextAction_LastSliceInFlight_ReturnsNone()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 2, Start, End);

		algo.UpdateMarketData(Start, 100m, 1m);
		algo.OnOrderMatched(algo.GetNextAction().Volume.Value);

		algo.UpdateMarketData(Start.AddMinutes(30), 100m, 1m);
		AreEqual(PositionModifyAction.ActionTypes.Register, algo.GetNextAction().ActionType);

		// The last child order is still working, so the algo waits for it instead of declaring itself done.
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.OnOrderMatched(50m);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void IsFinished_LastSliceFailed_FalseWhileVolumeRemains()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 2, Start, End);

		algo.UpdateMarketData(Start, 100m, 1m);
		algo.OnOrderMatched(algo.GetNextAction().Volume.Value);

		algo.UpdateMarketData(Start.AddMinutes(30), 100m, 1m);
		AreEqual(PositionModifyAction.ActionTypes.Register, algo.GetNextAction().ActionType);
		algo.OnOrderFailed();

		AreEqual(50m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);

		// There is no later slice to fold the failed one into, and the window is still open.
		var retry = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, retry.ActionType);
		AreEqual(50m, retry.Volume);
	}

	[TestMethod]
	public void OnOrderCanceled_LastSlicePartiallyFilled_RemainderGoesOutAgain()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 2, Start, End);

		algo.UpdateMarketData(Start, 100m, 1m);
		algo.OnOrderMatched(algo.GetNextAction().Volume.Value);

		algo.UpdateMarketData(Start.AddMinutes(30), 100m, 1m);
		AreEqual(50m, algo.GetNextAction().Volume);
		algo.OnOrderCanceled(20m);

		AreEqual(30m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);

		var rest = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, rest.ActionType);
		AreEqual(30m, rest.Volume);
	}

	[TestMethod]
	public void IsFinished_OutOfOrderTickInsideTheWindow_StaysFinished()
	{
		// Once the window has closed the algo has reported itself finished; a tick that arrives late
		// but is dated inside the window must not put it back to work.
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);

		algo.UpdateMarketData(Start, 100m, 1m);
		algo.OnOrderMatched(algo.GetNextAction().Volume.Value);

		algo.UpdateMarketData(End.AddMinutes(1), 100m, 1m);

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);

		algo.UpdateMarketData(Start.AddMinutes(30), 100m, 1m);

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
		AreEqual(75m, algo.RemainingVolume);
	}

	[TestMethod]
	public void IsFinished_TimePastEndAt_True()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);

		algo.UpdateMarketData(End.AddMinutes(1), 100m, 1m);

		IsTrue(algo.IsFinished);
		AreEqual(100m, algo.RemainingVolume); // the window closed with nothing executed
	}

	[TestMethod]
	public void GetNextAction_TimePastEndAt_ReturnsFinished()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);

		algo.UpdateMarketData(Start, 100m, 1m);
		algo.OnOrderMatched(algo.GetNextAction().Volume.Value);

		algo.UpdateMarketData(End.AddMinutes(1), 100m, 1m);

		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
		AreEqual(75m, algo.RemainingVolume);
	}

	[TestMethod]
	public void Cancel_MidFlight_FinishesAndKeepsLateFill()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);
		algo.UpdateMarketData(Start, 100m, 1m);
		AreEqual(PositionModifyAction.ActionTypes.Register, algo.GetNextAction().ActionType);

		algo.Cancel();
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);

		// The venue reports the working order as canceled with a partial fill.
		algo.OnOrderCanceled(10m);
		AreEqual(90m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);

		algo.UpdateMarketData(Start.AddMinutes(15), 100m, 1m);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_AfterAllVolumeFilled_StaysFinished()
	{
		var algo = new TwapAlgo(Sides.Sell, 100m, 2, Start, End);

		algo.UpdateMarketData(Start, 100m, 1m);
		algo.OnOrderMatched(algo.GetNextAction().Volume.Value);
		algo.UpdateMarketData(Start.AddMinutes(30), 100m, 1m);
		algo.OnOrderMatched(algo.GetNextAction().Volume.Value);

		IsTrue(algo.IsFinished);
		AreEqual(0m, algo.RemainingVolume);

		algo.UpdateMarketData(Start.AddMinutes(45), 100m, 1m);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);

		algo.OnOrderMatched(5m);
		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Dispose_CalledTwice_DoesNotThrow()
	{
		var algo = new TwapAlgo(Sides.Buy, 100m, 4, Start, End);

		algo.Dispose();
		algo.Dispose();
	}
}
