namespace StockSharp.Tests;

using StockSharp.Algo.Slippage;

[TestClass]
public class SlippageManagerStateTests : BaseTestClass
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	[TestMethod]
	public void Slippage_InitiallyZero()
	{
		var state = new SlippageManagerState();

		state.Slippage.AssertEqual(0m);
	}

	[TestMethod]
	public void AddSlippage_Accumulates()
	{
		var state = new SlippageManagerState();

		state.AddSlippage(1.5m);
		state.AddSlippage(2.5m);

		state.Slippage.AssertEqual(4m);
	}

	[TestMethod]
	public void UpdateBestPrices_StoresPrices()
	{
		var state = new SlippageManagerState();
		var now = DateTime.UtcNow;

		state.UpdateBestPrices(_secId, 99m, 101m, now);

		state.TryGetBestPrice(_secId, Sides.Buy, out var askPrice).AssertTrue();
		askPrice.AssertEqual(101m);

		state.TryGetBestPrice(_secId, Sides.Sell, out var bidPrice).AssertTrue();
		bidPrice.AssertEqual(99m);
	}

	[TestMethod]
	public void TryGetBestPrice_Buy_ReturnsAsk()
	{
		var state = new SlippageManagerState();
		var now = DateTime.UtcNow;

		state.UpdateBestPrices(_secId, 99m, 101m, now);

		state.TryGetBestPrice(_secId, Sides.Buy, out var price).AssertTrue();
		price.AssertEqual(101m);
	}

	[TestMethod]
	public void TryGetBestPrice_Sell_ReturnsBid()
	{
		var state = new SlippageManagerState();
		var now = DateTime.UtcNow;

		state.UpdateBestPrices(_secId, 99m, 101m, now);

		state.TryGetBestPrice(_secId, Sides.Sell, out var price).AssertTrue();
		price.AssertEqual(99m);
	}

	[TestMethod]
	public void TryGetBestPrice_NonExistent_ReturnsFalse()
	{
		var state = new SlippageManagerState();

		var secId = new SecurityId { SecurityCode = "UNKNOWN", BoardCode = "TEST" };

		state.TryGetBestPrice(secId, Sides.Buy, out _).AssertFalse();
	}

	[TestMethod]
	public void AddPlannedPrice_TryGet_ReturnsTrue()
	{
		var state = new SlippageManagerState();

		state.AddPlannedPrice(1, Sides.Buy, 100m);

		var found = state.TryGetPlannedPrice(1, out var side, out var price);

		found.AssertTrue();
		side.AssertEqual(Sides.Buy);
		price.AssertEqual(100m);
	}

	[TestMethod]
	public void RemovePlannedPrice_TryGet_ReturnsFalse()
	{
		var state = new SlippageManagerState();

		state.AddPlannedPrice(1, Sides.Buy, 100m);
		state.RemovePlannedPrice(1);

		state.TryGetPlannedPrice(1, out _, out _).AssertFalse();
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = new SlippageManagerState();
		var now = DateTime.UtcNow;

		state.AddSlippage(5m);
		state.UpdateBestPrices(_secId, 99m, 101m, now);
		state.AddPlannedPrice(1, Sides.Buy, 100m);

		state.Clear();

		state.Slippage.AssertEqual(0m);
		state.TryGetBestPrice(_secId, Sides.Buy, out _).AssertFalse();
		state.TryGetPlannedPrice(1, out _, out _).AssertFalse();
	}
}
