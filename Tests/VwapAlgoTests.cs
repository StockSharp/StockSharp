namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;

[TestClass]
public class VwapAlgoTests : BaseTestClass
{
	private static readonly DateTime Start = new(2026, 5, 27, 9, 0, 0, DateTimeKind.Utc);
	private static readonly DateTime End = new(2026, 5, 27, 10, 0, 0, DateTimeKind.Utc);

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
		algo.OnOrderMatched(a1.Volume.Value);

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

	[TestMethod]
	public void Ctor_NonPositiveTotalVolume_ThrowsWithOffendingValue()
	{
		var zero = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 0m, Start, End));

		AreEqual("totalVolume", zero.ParamName);
		AreEqual(0m, (decimal)zero.ActualValue);

		var negative = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, -5m, Start, End));

		AreEqual("totalVolume", negative.ParamName);
		AreEqual(-5m, (decimal)negative.ActualValue);
	}

	[TestMethod]
	public void Ctor_EndAtNotAfterStartAt_ThrowsWithOffendingValue()
	{
		var equal = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, Start));

		AreEqual("endAt", equal.ParamName);
		AreEqual(Start, (DateTime)equal.ActualValue);

		var before = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, End, Start));

		AreEqual("endAt", before.ParamName);
		AreEqual(Start, (DateTime)before.ActualValue);
	}

	[TestMethod]
	public void Ctor_ParticipationRateOutsideUnitInterval_ThrowsWithOffendingValue()
	{
		var zero = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, End, participationRate: 0m));
		AreEqual("participationRate", zero.ParamName);
		AreEqual(0m, (decimal)zero.ActualValue);

		var negative = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, End, participationRate: -0.1m));
		AreEqual("participationRate", negative.ParamName);
		AreEqual(-0.1m, (decimal)negative.ActualValue);

		var pastOne = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, End, participationRate: 1.0000001m));
		AreEqual("participationRate", pastOne.ParamName);
		AreEqual(1.0000001m, (decimal)pastOne.ActualValue);
	}

	[TestMethod]
	public void Ctor_ParticipationRateOfOne_IsAccepted()
	{
		var algo = new VwapAlgo(Sides.Buy, 100m, Start, End, participationRate: 1m);

		AreEqual(1m, algo.ParticipationRate);
	}

	[TestMethod]
	public void Ctor_NonPositiveMinSliceVolume_ThrowsWithOffendingValue()
	{
		var zero = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, End, minSliceVolume: 0m));

		AreEqual("minSliceVolume", zero.ParamName);
		AreEqual(0m, (decimal)zero.ActualValue);

		var negative = Throws<ArgumentOutOfRangeException>(() => new VwapAlgo(Sides.Buy, 100m, Start, End, minSliceVolume: -1m));

		AreEqual("minSliceVolume", negative.ParamName);
		AreEqual(-1m, (decimal)negative.ActualValue);
	}

	[TestMethod]
	public void Ctor_Defaults_AreExposed()
	{
		var algo = new VwapAlgo(Sides.Sell, 250m, Start, End);

		AreEqual(Sides.Sell, algo.Side);
		AreEqual(250m, algo.TotalVolume);
		AreEqual(0.10m, algo.ParticipationRate);
		AreEqual(1m, algo.MinSliceVolume);
		AreEqual(Start, algo.StartAt);
		AreEqual(End, algo.EndAt);
		IsNull(algo.LimitPrice);
		AreEqual(250m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void Ctor_ExplicitPolicy_IsExposed()
	{
		var algo = new VwapAlgo(Sides.Buy, 500m, Start, End, participationRate: 0.25m, minSliceVolume: 5m, limitPrice: 101.5m);

		AreEqual(0.25m, algo.ParticipationRate);
		AreEqual(5m, algo.MinSliceVolume);
		AreEqual(101.5m, algo.LimitPrice);
	}

	[TestMethod]
	public void GetNextAction_NoMarketDataYet_ReturnsNone()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
		AreEqual(1000m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void GetNextAction_BeforeStart_ReturnsNone()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start.AddSeconds(-1), 100m, 10000m);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void UpdateMarketData_VolumeBeforeStart_NotCounted()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start.AddSeconds(-1), 100m, 1000m);
		algo.UpdateMarketData(Start, 100m, 50m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(5m, a.Volume);
	}

	[TestMethod]
	public void UpdateMarketData_NullZeroOrNegativeVolume_NotCounted()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, null);
		algo.UpdateMarketData(Start, 100m, 0m);
		algo.UpdateMarketData(Start, 100m, -400m);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateMarketData(Start.AddSeconds(1), 100m, 50m);
		AreEqual(5m, algo.GetNextAction().Volume);
	}

	[TestMethod]
	public void UpdateOrderBook_DoesNotFeedTheVolumeProfile()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, null);
		algo.UpdateOrderBook(new QuoteChangeMessage
		{
			ServerTime = Start,
			Bids = [new QuoteChange(99m, 5000m)],
			Asks = [new QuoteChange(101m, 5000m)],
		});

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_SliceExactlyMinSlice_Registers()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 10m);
		algo.UpdateMarketData(Start, 100m, 100m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(10m, a.Volume);
	}

	[TestMethod]
	public void GetNextAction_FragmentsAccumulateUntilMinSlice()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 10m);

		algo.UpdateMarketData(Start, 100m, 99.9m); // 9.99 - one hundredth short of the floor
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateMarketData(Start.AddSeconds(1), 100m, 0.1m);
		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(10m, a.Volume);
	}

	[TestMethod]
	public void GetNextAction_NoLimitPrice_RegistersMarketOrder()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);

		var a = algo.GetNextAction();
		AreEqual(Sides.Buy, a.Side);
		AreEqual(OrderTypes.Market, a.OrderType);
		IsNull(a.Price);
	}

	[TestMethod]
	public void GetNextAction_LimitPrice_RegistersLimitOrderAtThatPrice()
	{
		var algo = new VwapAlgo(Sides.Sell, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m, limitPrice: 101.5m);
		algo.UpdateMarketData(Start, 100m, 100m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(Sides.Sell, a.Side);
		AreEqual(OrderTypes.Limit, a.OrderType);
		AreEqual(101.5m, a.Price);
		AreEqual(10m, a.Volume);
	}

	[TestMethod]
	public void GetNextAction_OrderInFlight_ReturnsNoneUntilResolved()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		AreEqual(10m, algo.GetNextAction().Volume);

		algo.UpdateMarketData(Start.AddSeconds(10), 100m, 900m); // budget grows while the slice is out
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.OnOrderMatched(10m);
		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(90m, a.Volume);
	}

	[TestMethod]
	public void GetNextAction_NoNewMarketData_ReturnsNoneAfterFill()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		algo.GetNextAction();
		algo.OnOrderMatched(10m);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
		AreEqual(990m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void GetNextAction_BehindSchedule_NeverExceedsParticipationRate()
	{
		var algo = new VwapAlgo(Sides.Buy, 10000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		var time = Start;
		var market = 0m;
		var sent = 0m;

		for (var i = 0; i < 10; i++)
		{
			time = time.AddMinutes(1);
			market += 137m;
			algo.UpdateMarketData(time, 100m, 137m);

			var a = algo.GetNextAction();

			if (a.ActionType == PositionModifyAction.ActionTypes.Register)
			{
				sent += a.Volume.Value;
				algo.OnOrderMatched(a.Volume.Value);
			}

			IsTrue(sent <= 0.10m * market);
		}

		AreEqual(0.10m * market, sent);
	}

	[TestMethod]
	public void OnOrderFailed_ResendsOnlyTheFailedSlice()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);

		algo.UpdateMarketData(Start, 100m, 100m);
		AreEqual(10m, algo.GetNextAction().Volume);
		algo.OnOrderMatched(10m);

		algo.UpdateMarketData(Start.AddSeconds(10), 100m, 200m); // market=300, target=30
		AreEqual(20m, algo.GetNextAction().Volume);
		algo.OnOrderFailed();

		var retry = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, retry.ActionType);
		AreEqual(20m, retry.Volume);
	}

	[TestMethod]
	public void OnOrderFailed_KeepsRemainingVolumeAndDoesNotFinish()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		algo.GetNextAction();
		algo.OnOrderFailed();

		AreEqual(1000m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void OnOrderCanceled_PartialFill_ReducesRemainingVolume()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		algo.GetNextAction();
		algo.OnOrderCanceled(4m);

		AreEqual(996m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void OnOrderCanceled_UnfilledPartOfSlice_IsResent()
	{
		// A cancelled slice consumed market participation only for the volume that traded;
		// the unfilled remainder must go out again, as it does after OnOrderFailed.
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 100m, Start, End, participationRate: 1m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		AreEqual(100m, algo.GetNextAction().Volume);

		algo.OnOrderCanceled(40m);
		AreEqual(60m, algo.RemainingVolume);

		algo.UpdateMarketData(Start.AddSeconds(10), 100m, 1000m);
		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(60m, a.Volume);
	}

	[TestMethod]
	public void Cancel_MidFlight_FinishesAndStillAccountsTheMatchedVolume()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		AreEqual(PositionModifyAction.ActionTypes.Register, algo.GetNextAction().ActionType);

		algo.Cancel();
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);

		algo.OnOrderCanceled(4m);
		AreEqual(996m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Cancel_BeforeAnyMarketData_Finishes()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End);
		algo.Cancel();

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
		AreEqual(1000m, algo.RemainingVolume);
	}

	[TestMethod]
	public void IsFinished_TimeExactlyEndAt_Finishes()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(End, 100m, 1000m);

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
		AreEqual(1000m, algo.RemainingVolume);
	}

	[TestMethod]
	public void GetNextAction_LastTickBeforeEndAt_StillTrades()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(End.AddTicks(-1), 100m, 1000m);

		IsFalse(algo.IsFinished);
		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(100m, a.Volume);
	}

	[TestMethod]
	public void IsFinished_OutOfOrderTickAfterEndAt_StaysFinished()
	{
		// Once the window has closed the algo has reported itself finished; a late tick
		// must not revive it into registering another order.
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.10m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 500m);
		algo.UpdateMarketData(End, 100m, 100m);

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);

		algo.UpdateMarketData(Start.AddMinutes(30), 100m, 100m);

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_TotalCapClampsFinalSlice()
	{
		var algo = new VwapAlgo(Sides.Sell, totalVolume: 100m, Start, End, participationRate: 0.3m, minSliceVolume: 0.1m);
		var time = Start;

		algo.UpdateMarketData(time, 100m, 111m); // market=111, target=33.3
		AreEqual(33.3m, algo.GetNextAction().Volume);
		algo.OnOrderMatched(33.3m);

		time = time.AddSeconds(10);
		algo.UpdateMarketData(time, 100m, 111m); // market=222, target=66.6, slice=33.3
		AreEqual(33.3m, algo.GetNextAction().Volume);
		algo.OnOrderMatched(33.3m);

		time = time.AddSeconds(10);
		algo.UpdateMarketData(time, 100m, 178m); // market=400, target=120 capped at 100, slice=33.4
		AreEqual(33.4m, algo.GetNextAction().Volume);
		algo.OnOrderMatched(33.4m);

		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void GetNextAction_NonDividingRate_SlicesExactly()
	{
		var algo = new VwapAlgo(Sides.Buy, 1000m, Start, End, participationRate: 0.07m, minSliceVolume: 0.5m);

		algo.UpdateMarketData(Start, 100m, 333m); // 0.07 × 333 = 23.31
		AreEqual(23.31m, algo.GetNextAction().Volume);
		algo.OnOrderMatched(23.31m);

		algo.UpdateMarketData(Start.AddSeconds(10), 100m, 167m); // market=500, target=35, slice=11.69
		AreEqual(11.69m, algo.GetNextAction().Volume);
	}

	[TestMethod]
	public void GetNextAction_ResidualBelowMinSlice_IsNotSent()
	{
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 100m, Start, End, participationRate: 1m, minSliceVolume: 10m);
		algo.UpdateMarketData(Start, 100m, 95m);
		AreEqual(95m, algo.GetNextAction().Volume);
		algo.OnOrderMatched(95m);

		algo.UpdateMarketData(Start.AddSeconds(10), 100m, 1000m); // target caps at 100, residual 5 < minSlice
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
		AreEqual(5m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void GetNextAction_AfterTotalFilled_ReportsFinishedForever()
	{
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 50m, Start, End, participationRate: 1m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		AreEqual(50m, algo.GetNextAction().Volume);
		algo.OnOrderMatched(50m);

		AreEqual(0m, algo.RemainingVolume);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);

		algo.UpdateMarketData(Start.AddSeconds(10), 100m, 1000m);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void RemainingVolume_OverFill_ClampsAtZero()
	{
		var algo = new VwapAlgo(Sides.Buy, totalVolume: 100m, Start, End, participationRate: 1m, minSliceVolume: 1m);
		algo.UpdateMarketData(Start, 100m, 100m);
		algo.GetNextAction();
		algo.OnOrderMatched(120m);

		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void SentVolumeAfterFailedSlice_PartOfTheCounterInFlight_RollsBackOnlyThat()
	{
		// 200 filled by earlier slices, 100 of the counter in flight when the slice failed.
		AreEqual(200m, VwapAlgo.SentVolumeAfterFailedSlice(sentVolume: 300m, filledVolume: 200m));
	}

	[TestMethod]
	public void SentVolumeAfterFailedSlice_NothingInFlight_KeepsCounter()
	{
		AreEqual(200m, VwapAlgo.SentVolumeAfterFailedSlice(sentVolume: 200m, filledVolume: 200m));
		AreEqual(0m, VwapAlgo.SentVolumeAfterFailedSlice(sentVolume: 0m, filledVolume: 0m));
	}

	[TestMethod]
	public void SentVolumeAfterFailedSlice_WholeCounterInFlight_RollsBackToZero()
	{
		AreEqual(0m, VwapAlgo.SentVolumeAfterFailedSlice(sentVolume: 100m, filledVolume: 0m));
	}

	[TestMethod]
	public void SentVolumeAfterFailedSlice_FilledExceedsSent_KeepsCounter()
	{
		AreEqual(100m, VwapAlgo.SentVolumeAfterFailedSlice(sentVolume: 100m, filledVolume: 120m));
	}
}
