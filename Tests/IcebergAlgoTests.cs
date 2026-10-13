namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;

[TestClass]
public class IcebergAlgoTests : BaseTestClass
{
	private static readonly DateTime Time = new(2026, 5, 27, 9, 0, 0, DateTimeKind.Utc);

	private static QuoteChangeMessage Book(decimal bid, decimal ask)
		=> new()
		{
			Bids = [new QuoteChange(bid, 10m)],
			Asks = [new QuoteChange(ask, 10m)],
		};

	[TestMethod]
	public void Ctor_ZeroTotalVolume_ReportsTotalVolume()
	{
		var ex = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 0m, 10m, 150m));

		AreEqual("totalVolume", ex.ParamName);
		AreEqual(0m, (decimal)ex.ActualValue);
	}

	[TestMethod]
	public void Ctor_NegativeTotalVolume_ReportsTotalVolume()
	{
		var ex = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Sell, -1m, 10m, 150m));

		AreEqual("totalVolume", ex.ParamName);
		AreEqual(-1m, (decimal)ex.ActualValue);
	}

	[TestMethod]
	public void Ctor_ZeroDisplayVolume_ReportsDisplayVolume()
	{
		var ex = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, 0m, 150m));

		AreEqual("displayVolume", ex.ParamName);
		AreEqual(0m, (decimal)ex.ActualValue);
	}

	[TestMethod]
	public void Ctor_NegativeDisplayVolume_ReportsDisplayVolume()
	{
		var ex = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, -5m, 150m));

		AreEqual("displayVolume", ex.ParamName);
		AreEqual(-5m, (decimal)ex.ActualValue);
	}

	[TestMethod]
	public void Ctor_DisplayVolumeJustAboveTotal_ReportsDisplayVolume()
	{
		var ex = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, 100.01m, 150m));

		AreEqual("displayVolume", ex.ParamName);
		AreEqual(100.01m, (decimal)ex.ActualValue);
	}

	[TestMethod]
	public void Ctor_ZeroLimitPrice_ReportsLimitPrice()
	{
		var ex = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, 10m, 0m));

		AreEqual("limitPrice", ex.ParamName);
		AreEqual(0m, (decimal)ex.ActualValue);
	}

	[TestMethod]
	public void Ctor_NegativeLimitPrice_ReportsLimitPrice()
	{
		var ex = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, 10m, -150m));

		AreEqual("limitPrice", ex.ParamName);
		AreEqual(-150m, (decimal)ex.ActualValue);
	}

	[TestMethod]
	public void Ctor_SeveralInvalidArgs_ReportsFirstFailedCheck()
	{
		var total = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, -1m, -1m, -1m));
		AreEqual("totalVolume", total.ParamName);

		var display = Throws<ArgumentOutOfRangeException>(() => new IcebergAlgo(Sides.Buy, 100m, -1m, -1m));
		AreEqual("displayVolume", display.ParamName);
	}

	[TestMethod]
	public void Ctor_ValidArgs_ExposesArgumentsAndFullRemaining()
	{
		var algo = new IcebergAlgo(Sides.Sell, totalVolume: 250m, displayVolume: 40m, limitPrice: 12.5m);

		AreEqual(Sides.Sell, algo.Side);
		AreEqual(250m, algo.TotalVolume);
		AreEqual(40m, algo.DisplayVolume);
		AreEqual(12.5m, algo.LimitPrice);
		AreEqual(250m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void Ctor_DisplayVolumeEqualsTotal_RunsAsSingleSlice()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 100m, limitPrice: 150m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(100m, a.Volume);

		algo.OnOrderMatched(100m);

		IsTrue(algo.IsFinished);
		AreEqual(0m, algo.RemainingVolume);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
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
	public void GetNextAction_BeforeAnyMarketData_RegistersAtLimitPrice()
	{
		// The slice price is the constructor limit, so the algo quotes without any tick or book.
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(Sides.Buy, a.Side);
		AreEqual(150m, a.Price);
		AreEqual(OrderTypes.Limit, a.OrderType);
	}

	[TestMethod]
	public void GetNextAction_SellSide_RegistersSellSlicesAtLimitPrice()
	{
		var algo = new IcebergAlgo(Sides.Sell, totalVolume: 100m, displayVolume: 40m, limitPrice: 12.5m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(Sides.Sell, a.Side);
		AreEqual(40m, a.Volume);
		AreEqual(12.5m, a.Price);
		AreEqual(OrderTypes.Limit, a.OrderType);
	}

	[TestMethod]
	public void MarketDataAndBook_AreIgnored_SlicePriceStaysAtTheLimit()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);

		algo.UpdateMarketData(Time, 999m, 7m);
		algo.UpdateMarketData(Time.AddMinutes(1), null, null);
		algo.UpdateOrderBook(Book(bid: 99m, ask: 101m));

		var first = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, first.ActionType);
		AreEqual(150m, first.Price);

		algo.OnOrderMatched(25m);

		algo.UpdateMarketData(Time.AddMinutes(2), 1m, 7m);
		algo.UpdateOrderBook(Book(bid: 200m, ask: 201m));
		algo.UpdateOrderBook(new QuoteChangeMessage { Bids = [], Asks = [] });
		algo.UpdateOrderBook(null);

		var second = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, second.ActionType);
		AreEqual(150m, second.Price);
		AreEqual(25m, second.Volume);
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
	public void Slices_TotalNotDivisibleByDisplay_LastSliceIsRemainder()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 30m, limitPrice: 12.5m);
		var slices = new List<decimal>();

		for (var i = 0; i < 10 && !algo.IsFinished; i++)
		{
			var a = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
			AreEqual(12.5m, a.Price);

			slices.Add(a.Volume.Value);
			algo.OnOrderMatched(a.Volume.Value);
		}

		AreEqual(4, slices.Count);
		AreEqual(30m, slices[0]);
		AreEqual(30m, slices[1]);
		AreEqual(30m, slices[2]);
		AreEqual(10m, slices[3]);
		AreEqual(100m, slices.Sum());
		IsTrue(algo.IsFinished);
		AreEqual(0m, algo.RemainingVolume);
	}

	[TestMethod]
	public void Slices_FractionalVolumes_SumExactlyToTotal()
	{
		var algo = new IcebergAlgo(Sides.Sell, totalVolume: 0.7m, displayVolume: 0.3m, limitPrice: 1.05m);
		var slices = new List<decimal>();

		for (var i = 0; i < 10 && !algo.IsFinished; i++)
		{
			var a = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);

			slices.Add(a.Volume.Value);
			algo.OnOrderMatched(a.Volume.Value);
		}

		AreEqual(3, slices.Count);
		AreEqual(0.3m, slices[0]);
		AreEqual(0.3m, slices[1]);
		AreEqual(0.1m, slices[2]);
		AreEqual(0.7m, slices.Sum());
		AreEqual(0m, algo.RemainingVolume);
	}

	[TestMethod]
	public void Slices_DisplayOfOne_EmitsOneUnitPerSlice()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 3m, displayVolume: 1m, limitPrice: 150m);

		for (var i = 0; i < 3; i++)
		{
			AreEqual(3m - i, algo.RemainingVolume);

			var a = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
			AreEqual(1m, a.Volume);

			algo.OnOrderMatched(1m);
		}

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
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
	public void OnOrderMatched_LessThanSlice_AccumulatesFill()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderMatched(10m);

		AreEqual(90m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(25m, a.Volume);
		AreEqual(150m, a.Price);
	}

	[TestMethod]
	public void OnOrderMatched_MoreThanRemaining_RemainingClampedToZero()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 25m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderMatched(30m);

		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void OnOrderFailed_ReRegistersSameSlice()
	{
		var algo = new IcebergAlgo(Sides.Sell, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderFailed();

		IsFalse(algo.IsFinished);
		AreEqual(100m, algo.RemainingVolume);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(Sides.Sell, a.Side);
		AreEqual(25m, a.Volume);
		AreEqual(150m, a.Price);
		AreEqual(OrderTypes.Limit, a.OrderType);
	}

	[TestMethod]
	public void OnOrderFailed_Repeatedly_ConsumesNoVolume()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);

		for (var i = 0; i < 3; i++)
		{
			var a = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
			AreEqual(25m, a.Volume);

			algo.OnOrderFailed();
		}

		AreEqual(100m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
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

	[TestMethod]
	public void OnOrderCanceled_NothingMatched_ReRegistersSameSlice()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderCanceled(0m);

		AreEqual(100m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(25m, a.Volume);
		AreEqual(150m, a.Price);
	}

	[TestMethod]
	public void OnOrderCanceled_PartialFillNearEnd_NextSliceClampedToRemaining()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 30m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderCanceled(20m);

		AreEqual(10m, algo.RemainingVolume);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(10m, a.Volume);
	}

	[TestMethod]
	public void OnOrderCanceled_CompletesTotal_Finishes()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 30m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.OnOrderCanceled(25m);
		algo.GetNextAction();
		algo.OnOrderCanceled(5m);

		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Cancel_BeforeFirstAction_NeverRegisters()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		algo.Cancel();

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Cancel_WhileSliceInFlight_ReportsFinished()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		AreEqual(PositionModifyAction.ActionTypes.Register, algo.GetNextAction().ActionType);

		algo.Cancel();

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Cancel_LateCancelConfirmation_StillFinishedAndCreditsFill()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);
		algo.GetNextAction();
		algo.Cancel();

		// The venue confirms the cancel after the algo was already stopped.
		algo.OnOrderCanceled(10m);

		AreEqual(90m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Finished_AfterFullFill_StaysFinishedAcrossCallbacks()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 50m, displayVolume: 25m, limitPrice: 150m);

		for (var i = 0; i < 2; i++)
		{
			algo.GetNextAction();
			algo.OnOrderMatched(25m);
		}

		algo.OnOrderFailed();
		algo.OnOrderCanceled(0m);
		algo.UpdateMarketData(Time, 150m, 1m);
		algo.UpdateOrderBook(Book(bid: 149m, ask: 151m));

		IsTrue(algo.IsFinished);
		AreEqual(0m, algo.RemainingVolume);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Dispose_CalledTwice_DoesNotThrow()
	{
		var algo = new IcebergAlgo(Sides.Buy, totalVolume: 100m, displayVolume: 25m, limitPrice: 150m);

		algo.Dispose();
		algo.Dispose();
	}
}
