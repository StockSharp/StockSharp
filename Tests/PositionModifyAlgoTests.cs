namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;
using StockSharp.Algo.Strategies.Quoting;

[TestClass]
public class PositionModifyAlgoTests : BaseTestClass
{
	#region MarketOrderAlgo

	[TestMethod]
	public void MarketOrderAlgo_GetNextAction_ReturnsRegister()
	{
		var algo = new MarketOrderAlgo(Sides.Buy, 100);

		var action = algo.GetNextAction();

		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.Register);
		action.Side.AreEqual(Sides.Buy);
		action.Volume.AreEqual(100m);
		action.OrderType.AreEqual(OrderTypes.Market);
	}

	[TestMethod]
	public void MarketOrderAlgo_AfterRegister_ReturnsNone()
	{
		var algo = new MarketOrderAlgo(Sides.Sell, 50);

		algo.GetNextAction(); // first call registers
		var action = algo.GetNextAction(); // second call should be None

		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.None);
	}

	[TestMethod]
	public void MarketOrderAlgo_OnOrderMatched_IsFinished()
	{
		var algo = new MarketOrderAlgo(Sides.Buy, 100);
		algo.GetNextAction();

		algo.OnOrderMatched(100);

		IsTrue(algo.IsFinished);
		algo.RemainingVolume.AreEqual(0m);
	}

	[TestMethod]
	public void MarketOrderAlgo_OnOrderFailed_IsFinished()
	{
		var algo = new MarketOrderAlgo(Sides.Buy, 100);
		algo.GetNextAction();

		algo.OnOrderFailed();

		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void MarketOrderAlgo_Cancel_IsFinished()
	{
		var algo = new MarketOrderAlgo(Sides.Buy, 100);

		algo.Cancel();

		algo.GetNextAction().ActionType.AreEqual(PositionModifyAction.ActionTypes.Finished);
	}

	[TestMethod]
	public void MarketOrderAlgo_InvalidVolume_Throws()
	{
		Throws<ArgumentOutOfRangeException>(() => new MarketOrderAlgo(Sides.Buy, 0));
		Throws<ArgumentOutOfRangeException>(() => new MarketOrderAlgo(Sides.Buy, -1));
	}

	#endregion

	#region QuotingBehaviorAlgo with VWAPQuotingBehavior

	[TestMethod]
	public void QuotingBehaviorAlgo_VWAP_NoData_ReturnsNone()
	{
		var algo = new QuotingBehaviorAlgo(
			new VWAPQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(10));

		var action = algo.GetNextAction();

		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.None);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_VWAP_WithData_CalculatesVWAP()
	{
		var algo = new QuotingBehaviorAlgo(
			new VWAPQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(50));

		// Price 100, Volume 10
		algo.UpdateMarketData(DateTime.UtcNow, 100m, 10m);
		// Price 110, Volume 20
		algo.UpdateMarketData(DateTime.UtcNow, 110m, 20m);

		// VWAP = (100*10 + 110*20) / (10+20) = (1000 + 2200) / 30 = 3200/30 ≈ 106.6667
		var action = algo.GetNextAction();

		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.Register);
		action.Side.AreEqual(Sides.Buy);
		action.Volume.AreEqual(50m); // volumePart = 50, remaining = 100, min(50, 100) = 50
		action.OrderType.AreEqual(OrderTypes.Limit);

		var expectedVwap = 3200m / 30m;
		action.Price.AreEqual(expectedVwap);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_VWAP_SliceExecution_ReducesRemaining()
	{
		var algo = new QuotingBehaviorAlgo(
			new VWAPQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(30));
		algo.UpdateMarketData(DateTime.UtcNow, 100m, 10m);

		var action = algo.GetNextAction();
		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.Register);
		action.Volume.AreEqual(30m);

		algo.OnOrderMatched(30m);

		algo.RemainingVolume.AreEqual(70m);
		IsFalse(algo.IsFinished);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_VWAP_NullPrice_IgnoresUpdate()
	{
		var algo = new QuotingBehaviorAlgo(
			new VWAPQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(50));
		algo.UpdateMarketData(DateTime.UtcNow, null, 10m);

		algo.GetNextAction().ActionType.AreEqual(PositionModifyAction.ActionTypes.None);
	}

	#endregion

	#region QuotingBehaviorAlgo with TWAPQuotingBehavior

	[TestMethod]
	public void QuotingBehaviorAlgo_TWAP_FirstUpdate_RegistersImmediately()
	{
		var algo = new QuotingBehaviorAlgo(
			new TWAPQuotingBehavior(TimeSpan.FromSeconds(10)),
			Sides.Sell, 100, new Unit(25));

		algo.UpdateMarketData(new DateTime(2024, 1, 1, 10, 0, 0), 50m, 1m);
		var action = algo.GetNextAction();

		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.Register);
		action.Side.AreEqual(Sides.Sell);
		action.Price.AreEqual(50m);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_TWAP_BeforeInterval_ReturnsNone()
	{
		var algo = new QuotingBehaviorAlgo(
			new TWAPQuotingBehavior(TimeSpan.FromSeconds(10)),
			Sides.Buy, 100, new Unit(25));

		algo.UpdateMarketData(new DateTime(2024, 1, 1, 10, 0, 0), 50m, 1m);
		algo.GetNextAction(); // registers first order
		algo.OnOrderMatched(25m);

		// 5 seconds later - not yet interval
		algo.UpdateMarketData(new DateTime(2024, 1, 1, 10, 0, 5), 55m, 1m);
		var action = algo.GetNextAction();

		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.None);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_TWAP_AfterInterval_RegistersAgain()
	{
		var algo = new QuotingBehaviorAlgo(
			new TWAPQuotingBehavior(TimeSpan.FromSeconds(10)),
			Sides.Buy, 100, new Unit(25));

		algo.UpdateMarketData(new DateTime(2024, 1, 1, 10, 0, 0), 50m, 1m);
		algo.GetNextAction(); // registers first order
		algo.OnOrderMatched(25m);

		// 10 seconds later - past interval
		algo.UpdateMarketData(new DateTime(2024, 1, 1, 10, 0, 10), 55m, 1m);
		var action = algo.GetNextAction();

		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.Register);
		// TWAP price = avg(50, 55) = 52.5
		action.Price.AreEqual(52.5m);
	}

	#endregion

	#region QuotingBehaviorAlgo with LastTradeQuotingBehavior (Iceberg)

	[TestMethod]
	public void QuotingBehaviorAlgo_Iceberg_WithPrice_RegistersSlice()
	{
		var algo = new QuotingBehaviorAlgo(
			new LastTradeQuotingBehavior(new Unit(0)),
			Sides.Buy, 1000, new Unit(100));

		algo.UpdateMarketData(DateTime.UtcNow, 50m, null);
		var action = algo.GetNextAction();

		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.Register);
		action.Volume.AreEqual(100m);
		action.Price.AreEqual(50m);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_Iceberg_MultipleSlices_ReducesRemaining()
	{
		var algo = new QuotingBehaviorAlgo(
			new LastTradeQuotingBehavior(new Unit(0)),
			Sides.Sell, 300, new Unit(100));

		algo.UpdateMarketData(DateTime.UtcNow, 50m, null);

		// First slice
		algo.GetNextAction();
		algo.OnOrderMatched(100m);
		algo.RemainingVolume.AreEqual(200m);

		// Second slice
		algo.GetNextAction();
		algo.OnOrderMatched(100m);
		algo.RemainingVolume.AreEqual(100m);

		// Third slice
		algo.GetNextAction();
		algo.OnOrderMatched(100m);
		algo.RemainingVolume.AreEqual(0m);
		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_Iceberg_NoPrice_ReturnsNone()
	{
		var algo = new QuotingBehaviorAlgo(
			new LastTradeQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(50));

		algo.GetNextAction().ActionType.AreEqual(PositionModifyAction.ActionTypes.None);
	}

	#endregion

	#region QuotingBehaviorAlgo common

	[TestMethod]
	public void QuotingBehaviorAlgo_OnOrderCanceled_ReducesByMatchedVolume()
	{
		var algo = new QuotingBehaviorAlgo(
			new LastTradeQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(50));
		algo.UpdateMarketData(DateTime.UtcNow, 50m, null);

		algo.GetNextAction(); // register
		algo.OnOrderCanceled(20m); // 20 of 50 matched before cancel

		algo.RemainingVolume.AreEqual(80m);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_OnOrderFailed_CanRetry()
	{
		var algo = new QuotingBehaviorAlgo(
			new LastTradeQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(50));
		algo.UpdateMarketData(DateTime.UtcNow, 50m, null);

		algo.GetNextAction(); // register
		algo.OnOrderFailed();

		// Should be able to try again
		IsFalse(algo.IsFinished);
		var action = algo.GetNextAction();
		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.Register);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_Cancel_BecomesFinished()
	{
		var algo = new QuotingBehaviorAlgo(
			new VWAPQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(50));
		algo.UpdateMarketData(DateTime.UtcNow, 50m, 10m);

		algo.Cancel();
		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_PercentVolumePart()
	{
		// 10% volume part of 100 remaining
		var algo = new QuotingBehaviorAlgo(
			new LastTradeQuotingBehavior(new Unit(0)),
			Sides.Buy, 100, new Unit(10, UnitTypes.Percent));
		algo.UpdateMarketData(DateTime.UtcNow, 50m, null);

		var action = algo.GetNextAction();
		action.ActionType.AreEqual(PositionModifyAction.ActionTypes.Register);
		// 10% of 100 = 10
		action.Volume.AreEqual(10m);
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_InvalidVolume_Throws()
	{
		Throws<ArgumentOutOfRangeException>(() => new QuotingBehaviorAlgo(
			new VWAPQuotingBehavior(new Unit(0)), Sides.Buy, 0, new Unit(10)));
		Throws<ArgumentOutOfRangeException>(() => new QuotingBehaviorAlgo(
			new VWAPQuotingBehavior(new Unit(0)), Sides.Buy, -1, new Unit(10)));
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_NullBehavior_Throws()
	{
		Throws<ArgumentNullException>(() => new QuotingBehaviorAlgo(
			null, Sides.Buy, 100, new Unit(10)));
	}

	[TestMethod]
	public void QuotingBehaviorAlgo_NullVolumePart_Throws()
	{
		Throws<ArgumentNullException>(() => new QuotingBehaviorAlgo(
			new VWAPQuotingBehavior(new Unit(0)), Sides.Buy, 100, null));
	}

	#endregion
}
