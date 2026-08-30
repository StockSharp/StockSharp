namespace StockSharp.Tests;

using StockSharp.Algo.PositionManagement;

[TestClass]
public class NbboAlgoTests : BaseTestClass
{
	private static QuoteChangeMessage Book(decimal bid, decimal ask, decimal bidSize = 10, decimal askSize = 10)
		=> new()
		{
			Bids = [new QuoteChange(bid, bidSize)],
			Asks = [new QuoteChange(ask, askSize)],
		};

	[TestMethod]
	public void Ctor_RejectsBadArgs()
	{
		Throws<ArgumentOutOfRangeException>(() => new NbboAlgo(Sides.Buy, 0m, 5m));
		Throws<ArgumentOutOfRangeException>(() => new NbboAlgo(Sides.Buy, 100m, 0m));
		Throws<ArgumentOutOfRangeException>(() => new NbboAlgo(Sides.Buy, 100m, 200m));
	}

	[TestMethod]
	public void NoBookYet_ReturnsNone()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Buy_PegsToBestBid()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(bid: 99m, ask: 101m));

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(99m, a.Price);
		AreEqual(10m, a.Volume);
		AreEqual(OrderTypes.Limit, a.OrderType);
	}

	[TestMethod]
	public void Sell_PegsToBestAsk()
	{
		var algo = new NbboAlgo(Sides.Sell, 100m, 10m);
		algo.UpdateOrderBook(Book(bid: 99m, ask: 101m));

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(101m, a.Price);
	}

	[TestMethod]
	public void TopOfBookMoves_CancelsThenReregisters()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(bid: 99m, ask: 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(bid: 99.5m, ask: 101m)); // top moves up

		// First call after the move: Cancel.
		var a1 = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Cancel, a1.ActionType);

		// Caller acknowledges the cancel through OnOrderCanceled.
		algo.OnOrderCanceled(matchedVolume: 0m);

		// Next call: Register at new best.
		var a2 = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a2.ActionType);
		AreEqual(99.5m, a2.Price);
	}

	[TestMethod]
	public void Filled_ResumesWithRemainingVolume()
	{
		var algo = new NbboAlgo(Sides.Buy, totalVolume: 100m, sliceVolume: 25m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();
		algo.OnOrderMatched(25m);

		AreEqual(75m, algo.RemainingVolume);
		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(25m, a.Volume);
	}

	[TestMethod]
	public void LastSlice_ClampedToRemainingVolume()
	{
		var algo = new NbboAlgo(Sides.Buy, totalVolume: 30m, sliceVolume: 25m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();
		algo.OnOrderMatched(25m);

		var a = algo.GetNextAction();
		AreEqual(5m, a.Volume);
	}

	[TestMethod]
	public void AllFilled_Finishes()
	{
		var algo = new NbboAlgo(Sides.Buy, totalVolume: 25m, sliceVolume: 25m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();
		algo.OnOrderMatched(25m);

		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void Cancel_Finishes()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.Cancel();
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}
}
