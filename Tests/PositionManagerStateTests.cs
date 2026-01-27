namespace StockSharp.Tests;

using StockSharp.Algo.Positions;

[TestClass]
public class PositionManagerStateTests : BaseTestClass
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();
	private const string Portfolio = "test_portfolio";

	[TestMethod]
	public void AddOrGetOrder_NewOrder_ReturnsBalance()
	{
		var state = new PositionManagerState();

		var result = state.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 100m, 100m);

		result.AssertEqual(100m);
	}

	[TestMethod]
	public void AddOrGetOrder_ExistingOrder_ReturnsStoredBalance()
	{
		var state = new PositionManagerState();

		state.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 100m, 100m);

		// Update balance to 50 first
		state.UpdateOrderBalance(1, 50m);

		// Adding same transactionId should return the stored (updated) balance
		var result = state.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 100m, 100m);

		result.AssertEqual(50m);
	}

	[TestMethod]
	public void TryGetOrder_Existing_ReturnsTrue()
	{
		var state = new PositionManagerState();

		state.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 100m, 80m);

		var found = state.TryGetOrder(1, out var secId, out var portfolio, out var side, out var balance);

		found.AssertTrue();
		secId.AssertEqual(_secId);
		portfolio.AssertEqual(Portfolio);
		side.AssertEqual(Sides.Buy);
		balance.AssertEqual(80m);
	}

	[TestMethod]
	public void TryGetOrder_NonExistent_ReturnsFalse()
	{
		var state = new PositionManagerState();

		var found = state.TryGetOrder(999, out _, out _, out _, out _);

		found.AssertFalse();
	}

	[TestMethod]
	public void UpdateOrderBalance_ChangesBalance()
	{
		var state = new PositionManagerState();

		state.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 100m, 100m);
		state.UpdateOrderBalance(1, 30m);

		state.TryGetOrder(1, out _, out _, out _, out var balance);

		balance.AssertEqual(30m);
	}

	[TestMethod]
	public void RemoveOrder_RemovesOrder()
	{
		var state = new PositionManagerState();

		state.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 100m, 100m);
		state.RemoveOrder(1);

		state.TryGetOrder(1, out _, out _, out _, out _).AssertFalse();
	}

	[TestMethod]
	public void UpdatePosition_InitialDiff_ReturnsNewPosition()
	{
		var state = new PositionManagerState();

		var result = state.UpdatePosition(_secId, Portfolio, 50m);

		result.AssertEqual(50m);
	}

	[TestMethod]
	public void UpdatePosition_MultipleDiffs_Accumulates()
	{
		var state = new PositionManagerState();

		state.UpdatePosition(_secId, Portfolio, 50m);
		state.UpdatePosition(_secId, Portfolio, 30m);
		var result = state.UpdatePosition(_secId, Portfolio, -20m);

		result.AssertEqual(60m);
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = new PositionManagerState();

		state.AddOrGetOrder(1, _secId, Portfolio, Sides.Buy, 100m, 100m);
		state.UpdatePosition(_secId, Portfolio, 50m);

		state.Clear();

		state.TryGetOrder(1, out _, out _, out _, out _).AssertFalse();
		// After clear, position should be reset to 0 (adding 0 diff returns 0)
		state.UpdatePosition(_secId, Portfolio, 0m).AssertEqual(0m);
	}
}
