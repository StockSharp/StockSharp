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

	private static QuoteChangeMessage BidsOnly(decimal bid, decimal bidSize = 10)
		=> new() { Bids = [new QuoteChange(bid, bidSize)] };

	private static QuoteChangeMessage AsksOnly(decimal ask, decimal askSize = 10)
		=> new() { Asks = [new QuoteChange(ask, askSize)] };

	private static QuoteChangeMessage EmptyBook() => new();

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

	[TestMethod]
	public void Ctor_TotalVolumeNotPositive_ReportsOffendingValue()
	{
		var zero = Throws<ArgumentOutOfRangeException>(() => new NbboAlgo(Sides.Buy, 0m, 5m));
		AreEqual("totalVolume", zero.ParamName);
		AreEqual(0m, zero.ActualValue);

		var negative = Throws<ArgumentOutOfRangeException>(() => new NbboAlgo(Sides.Buy, -1m, 5m));
		AreEqual("totalVolume", negative.ParamName);
		AreEqual(-1m, negative.ActualValue);
	}

	[TestMethod]
	public void Ctor_SliceVolumeOutOfRange_ReportsOffendingValue()
	{
		var zero = Throws<ArgumentOutOfRangeException>(() => new NbboAlgo(Sides.Buy, 100m, 0m));
		AreEqual("sliceVolume", zero.ParamName);
		AreEqual(0m, zero.ActualValue);

		var negative = Throws<ArgumentOutOfRangeException>(() => new NbboAlgo(Sides.Buy, 100m, -5m));
		AreEqual("sliceVolume", negative.ParamName);
		AreEqual(-5m, negative.ActualValue);

		var tooBig = Throws<ArgumentOutOfRangeException>(() => new NbboAlgo(Sides.Buy, 100m, 100.0001m));
		AreEqual("sliceVolume", tooBig.ParamName);
		AreEqual(100.0001m, tooBig.ActualValue);
	}

	[TestMethod]
	public void Ctor_SliceVolumeEqualsTotal_RegistersWholeSizeAtOnce()
	{
		var algo = new NbboAlgo(Sides.Buy, totalVolume: 100m, sliceVolume: 100m);
		AreEqual(100m, algo.SliceVolume);

		algo.UpdateOrderBook(Book(99m, 101m));

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(100m, a.Volume);

		algo.OnOrderMatched(100m);
		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void InitialState_BeforeAnyBook_ExposesConstructorArgs()
	{
		var algo = new NbboAlgo(Sides.Sell, totalVolume: 100m, sliceVolume: 10m);

		AreEqual(Sides.Sell, algo.Side);
		AreEqual(100m, algo.TotalVolume);
		AreEqual(10m, algo.SliceVolume);
		AreEqual(100m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);
		IsNull(algo.CurrentBestPrice);
	}

	[TestMethod]
	public void CurrentBestPrice_TracksPeggedSideOnly()
	{
		var buy = new NbboAlgo(Sides.Buy, 100m, 10m);
		var sell = new NbboAlgo(Sides.Sell, 100m, 10m);

		var book = Book(bid: 99m, ask: 101m);
		buy.UpdateOrderBook(book);
		sell.UpdateOrderBook(book);

		AreEqual(99m, buy.CurrentBestPrice);
		AreEqual(101m, sell.CurrentBestPrice);
	}

	[TestMethod]
	public void UpdateOrderBook_Null_LeavesStateUntouched()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);

		algo.UpdateOrderBook(null);
		IsNull(algo.CurrentBestPrice);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(null);
		AreEqual(99m, algo.CurrentBestPrice);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void EmptyBook_ReturnsNone()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(EmptyBook());

		IsNull(algo.CurrentBestPrice);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void OneSidedBook_OppositeSideOnly_ReturnsNone()
	{
		var buy = new NbboAlgo(Sides.Buy, 100m, 10m);
		buy.UpdateOrderBook(AsksOnly(101m));
		IsNull(buy.CurrentBestPrice);
		AreEqual(PositionModifyAction.ActionTypes.None, buy.GetNextAction().ActionType);

		var sell = new NbboAlgo(Sides.Sell, 100m, 10m);
		sell.UpdateOrderBook(BidsOnly(99m));
		IsNull(sell.CurrentBestPrice);
		AreEqual(PositionModifyAction.ActionTypes.None, sell.GetNextAction().ActionType);
	}

	[TestMethod]
	public void OneSidedBook_PeggedSideOnly_Registers()
	{
		var buy = new NbboAlgo(Sides.Buy, 100m, 10m);
		buy.UpdateOrderBook(BidsOnly(99m));

		var buyAction = buy.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, buyAction.ActionType);
		AreEqual(99m, buyAction.Price);

		var sell = new NbboAlgo(Sides.Sell, 100m, 10m);
		sell.UpdateOrderBook(AsksOnly(101m));

		var sellAction = sell.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, sellAction.ActionType);
		AreEqual(101m, sellAction.Price);
	}

	[TestMethod]
	public void PeggedSideVanishes_KeepsRestingOrder()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(AsksOnly(101m));

		AreEqual(99m, algo.CurrentBestPrice);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void PeggedSideVanishes_RestingOrderEnds_WaitsForABestInsteadOfQuotingTheOldOne()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(AsksOnly(101m)); // the whole bid side goes away
		algo.OnOrderCanceled(0m);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateOrderBook(Book(98.5m, 101m));

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(98.5m, a.Price);
	}

	[TestMethod]
	public void CancelEmitted_OrderFillsInstead_ResumesAtTheNewBest()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.5m, 101m));
		AreEqual(PositionModifyAction.ActionTypes.Cancel, algo.GetNextAction().ActionType);

		// The venue reports a fill rather than the cancel acknowledgement.
		algo.OnOrderMatched(10m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(99.5m, a.Price);
		AreEqual(90m, algo.RemainingVolume);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void CancelEmitted_OrderFailsInstead_ResumesAtTheNewBest()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.5m, 101m));
		AreEqual(PositionModifyAction.ActionTypes.Cancel, algo.GetNextAction().ActionType);

		algo.OnOrderFailed();

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(99.5m, a.Price);
		AreEqual(100m, algo.RemainingVolume);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void CrossedBook_PegsToOwnSide()
	{
		var buy = new NbboAlgo(Sides.Buy, 100m, 10m);
		var sell = new NbboAlgo(Sides.Sell, 100m, 10m);

		var crossed = Book(bid: 101m, ask: 99m);
		buy.UpdateOrderBook(crossed);
		sell.UpdateOrderBook(crossed);

		AreEqual(101m, buy.GetNextAction().Price);
		AreEqual(99m, sell.GetNextAction().Price);
	}

	[TestMethod]
	public void LockedBook_BothSidesPegToSamePrice()
	{
		var buy = new NbboAlgo(Sides.Buy, 100m, 10m);
		var sell = new NbboAlgo(Sides.Sell, 100m, 10m);

		var locked = Book(bid: 100m, ask: 100m);
		buy.UpdateOrderBook(locked);
		sell.UpdateOrderBook(locked);

		AreEqual(100m, buy.GetNextAction().Price);
		AreEqual(100m, sell.GetNextAction().Price);
	}

	[TestMethod]
	public void MultiLevelBook_PegsToTopLevel()
	{
		var deep = new QuoteChangeMessage
		{
			Bids = [new QuoteChange(99m, 1m), new QuoteChange(98.5m, 50m), new QuoteChange(98m, 100m)],
			Asks = [new QuoteChange(101m, 1m), new QuoteChange(101.5m, 50m), new QuoteChange(102m, 100m)],
		};

		var buy = new NbboAlgo(Sides.Buy, 100m, 10m);
		buy.UpdateOrderBook(deep);
		AreEqual(99m, buy.GetNextAction().Price);

		var sell = new NbboAlgo(Sides.Sell, 100m, 10m);
		sell.UpdateOrderBook(deep);
		AreEqual(101m, sell.GetNextAction().Price);
	}

	[TestMethod]
	public void BookMovesBeforeFirstRegister_PegsToLatestBest()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.UpdateOrderBook(Book(99.5m, 101m));

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(99.5m, a.Price);
	}

	[TestMethod]
	public void BestUnchanged_KeepsRestingOrder()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(bid: 99m, ask: 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(bid: 99m, ask: 100.5m, bidSize: 999m));

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void SmallestMove_TriggersReprice()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.0001m, 101m));

		AreEqual(PositionModifyAction.ActionTypes.Cancel, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void BestMovesAway_RepegsDown()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(98.5m, 101m));
		AreEqual(PositionModifyAction.ActionTypes.Cancel, algo.GetNextAction().ActionType);

		algo.OnOrderCanceled(0m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(98.5m, a.Price);
		AreEqual(10m, a.Volume);
		AreEqual(100m, algo.RemainingVolume);
	}

	[TestMethod]
	public void Sell_TopOfBookMoves_RepegsToNewAsk()
	{
		var algo = new NbboAlgo(Sides.Sell, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 101

		algo.UpdateOrderBook(Book(99m, 100.5m));
		AreEqual(PositionModifyAction.ActionTypes.Cancel, algo.GetNextAction().ActionType);

		algo.OnOrderCanceled(0m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(Sides.Sell, a.Side);
		AreEqual(100.5m, a.Price);
	}

	[TestMethod]
	public void CancelEmitted_NotRepeatedUntilAcked()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.5m, 101m));

		AreEqual(PositionModifyAction.ActionTypes.Cancel, algo.GetNextAction().ActionType);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void OrderInFlight_SecondCall_ReturnsNone()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void CanceledWithPartialFill_ResumesWithRemainder()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.5m, 101m));
		algo.GetNextAction(); // Cancel
		algo.OnOrderCanceled(4m);

		AreEqual(96m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(10m, a.Volume);
		AreEqual(99.5m, a.Price);
	}

	[TestMethod]
	public void CanceledWithPartialFill_LastSliceClampedToRemainder()
	{
		var algo = new NbboAlgo(Sides.Buy, totalVolume: 30m, sliceVolume: 25m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();
		algo.OnOrderCanceled(7m);

		AreEqual(23m, algo.RemainingVolume);
		AreEqual(23m, algo.GetNextAction().Volume);
	}

	[TestMethod]
	public void OrderFailed_KeepsVolume_AndRetriesAtSameBest()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();
		algo.OnOrderFailed();

		AreEqual(100m, algo.RemainingVolume);
		IsFalse(algo.IsFinished);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(10m, a.Volume);
		AreEqual(99m, a.Price);
	}

	[TestMethod]
	public void SliceVolume_DoesNotDivideTotal_LastSliceIsRemainder()
	{
		var algo = new NbboAlgo(Sides.Buy, totalVolume: 100m, sliceVolume: 30m);
		algo.UpdateOrderBook(Book(99m, 101m));

		decimal[] slices = [30m, 30m, 30m, 10m];
		decimal[] remainingAfterFill = [70m, 40m, 10m, 0m];

		for (var i = 0; i < slices.Length; i++)
		{
			var a = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
			AreEqual(slices[i], a.Volume);
			IsFalse(algo.IsFinished);

			algo.OnOrderMatched(a.Volume.Value);
			AreEqual(remainingAfterFill[i], algo.RemainingVolume);
		}

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void FractionalSlices_NoRoundingDrift()
	{
		var algo = new NbboAlgo(Sides.Buy, totalVolume: 1m, sliceVolume: 0.3m);
		algo.UpdateOrderBook(Book(99.995m, 100.005m));

		decimal[] slices = [0.3m, 0.3m, 0.3m, 0.1m];

		foreach (var slice in slices)
		{
			var a = algo.GetNextAction();
			AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
			AreEqual(slice, a.Volume);
			AreEqual(99.995m, a.Price);

			algo.OnOrderMatched(a.Volume.Value);
		}

		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
	}

	[TestMethod]
	public void OverFill_ClampsRemainingToZeroAndFinishes()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();

		algo.OnOrderMatched(150m);

		AreEqual(0m, algo.RemainingVolume);
		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void AfterFullFill_FurtherBooksDoNotResume()
	{
		var algo = new NbboAlgo(Sides.Buy, totalVolume: 10m, sliceVolume: 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();
		algo.OnOrderMatched(10m);

		algo.UpdateOrderBook(Book(98m, 102m));

		IsTrue(algo.IsFinished);
		AreEqual(0m, algo.RemainingVolume);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Cancel_MidFlight_FinishesAndKeepsUnexecutedVolume()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction();
		algo.OnOrderMatched(10m);
		algo.GetNextAction(); // second slice is working

		algo.Cancel();

		IsTrue(algo.IsFinished);
		AreEqual(90m, algo.RemainingVolume);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);

		algo.UpdateOrderBook(Book(99.5m, 101m));
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void Cancel_BeforeAnyBook_Finishes()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.Cancel();

		IsTrue(algo.IsFinished);
		AreEqual(PositionModifyAction.ActionTypes.Finished, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void UpdateMarketData_Ignored_BookStillRequired()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateMarketData(new DateTime(2026, 5, 27, 9, 0, 0, DateTimeKind.Utc), 105m, 3m);

		IsNull(algo.CurrentBestPrice);
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);

		algo.UpdateOrderBook(Book(99m, 101m));
		algo.UpdateMarketData(new DateTime(2026, 5, 27, 9, 0, 1, DateTimeKind.Utc), 105m, 3m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(99m, a.Price);
	}

	[TestMethod]
	public void BestReturnsToRestingPrice_KeepsRestingOrder()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.5m, 101m));
		algo.UpdateOrderBook(Book(99m, 101m));

		// The resting order sits at the current best bid again, so its queue position must be kept.
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void RepriceThenFill_FreshPegNotCancelled()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.5m, 101m)); // reprice becomes due
		algo.OnOrderMatched(10m); // the order filled before the cancel was emitted

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(99.5m, a.Price);

		// The reprice was answered by the new registration, so nothing is left to cancel.
		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void RepriceThenFail_FreshPegNotCancelled()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.5m, 101m));
		algo.OnOrderFailed();

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(99.5m, a.Price);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}

	[TestMethod]
	public void BookMovesAgainBeforeCancelAck_FreshPegNotCancelled()
	{
		var algo = new NbboAlgo(Sides.Buy, 100m, 10m);
		algo.UpdateOrderBook(Book(99m, 101m));
		algo.GetNextAction(); // Register at 99

		algo.UpdateOrderBook(Book(99.5m, 101m));
		AreEqual(PositionModifyAction.ActionTypes.Cancel, algo.GetNextAction().ActionType);

		algo.UpdateOrderBook(Book(100m, 101m)); // best moves again while the cancel is in flight
		algo.OnOrderCanceled(0m);

		var a = algo.GetNextAction();
		AreEqual(PositionModifyAction.ActionTypes.Register, a.ActionType);
		AreEqual(100m, a.Price);

		AreEqual(PositionModifyAction.ActionTypes.None, algo.GetNextAction().ActionType);
	}
}
