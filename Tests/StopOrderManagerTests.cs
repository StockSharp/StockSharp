namespace StockSharp.Tests;

[TestClass]
public class StopOrderManagerTests : BaseTestClass
{
	private static SecurityId CreateSecId() => Helper.CreateSecurityId();

	private static StopOrderInfo CreateStopInfo(
		SecurityId secId, Sides side, decimal stopPrice,
		decimal volume = 10, decimal? limitPrice = null,
		bool isTrailing = false, decimal? trailingOffset = null,
		long transactionId = 1, string portfolioName = "test")
	{
		return new()
		{
			TransactionId = transactionId,
			SecurityId = secId,
			Side = side,
			Volume = volume,
			PortfolioName = portfolioName,
			StopPrice = stopPrice,
			LimitPrice = limitPrice,
			IsTrailing = isTrailing,
			TrailingOffset = trailingOffset,
		};
	}

	[TestMethod]
	public void StopMarketBuy_TriggeredWhenPriceRises()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		var info = CreateStopInfo(secId, Sides.Buy, stopPrice: 105);
		mgr.Register(info);

		// Price below stop - no trigger
		var triggers = mgr.CheckPrice(secId, 100, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);

		// Price at stop - trigger
		triggers = mgr.CheckPrice(secId, 105, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(OrderTypes.Market, triggers[0].ResultingOrder.OrderType);
		Assert.AreEqual(Sides.Buy, triggers[0].ResultingOrder.Side);
		Assert.AreEqual(10m, triggers[0].ResultingOrder.Volume);
	}

	[TestMethod]
	public void StopMarketSell_TriggeredWhenPriceDrops()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		var info = CreateStopInfo(secId, Sides.Sell, stopPrice: 95);
		mgr.Register(info);

		// Price above stop - no trigger
		var triggers = mgr.CheckPrice(secId, 100, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);

		// Price at stop - trigger
		triggers = mgr.CheckPrice(secId, 95, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(OrderTypes.Market, triggers[0].ResultingOrder.OrderType);
		Assert.AreEqual(Sides.Sell, triggers[0].ResultingOrder.Side);
	}

	[TestMethod]
	public void StopLimitBuy_CreatesLimitOrder()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		var info = CreateStopInfo(secId, Sides.Buy, stopPrice: 105, limitPrice: 106);
		mgr.Register(info);

		var triggers = mgr.CheckPrice(secId, 106, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(OrderTypes.Limit, triggers[0].ResultingOrder.OrderType);
		Assert.AreEqual(106m, triggers[0].ResultingOrder.Price);
	}

	[TestMethod]
	public void StopLimitSell_CreatesLimitOrder()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		var info = CreateStopInfo(secId, Sides.Sell, stopPrice: 95, limitPrice: 94);
		mgr.Register(info);

		var triggers = mgr.CheckPrice(secId, 94, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(OrderTypes.Limit, triggers[0].ResultingOrder.OrderType);
		Assert.AreEqual(94m, triggers[0].ResultingOrder.Price);
	}

	[TestMethod]
	public void TrailingStopSell_TracksMaxPrice()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		var info = CreateStopInfo(secId, Sides.Sell, stopPrice: 95, isTrailing: true, trailingOffset: 5);
		mgr.Register(info);

		// Price rises to 110 - best seen = 110, stop = 105
		var triggers = mgr.CheckPrice(secId, 110, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);

		// Price rises to 115 - best seen = 115, stop = 110
		triggers = mgr.CheckPrice(secId, 115, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);

		// Price drops to 112 - still above 110
		triggers = mgr.CheckPrice(secId, 112, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);

		// Price drops to 110 - triggered (115 - 5 = 110)
		triggers = mgr.CheckPrice(secId, 110, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(OrderTypes.Market, triggers[0].ResultingOrder.OrderType);
		Assert.AreEqual(Sides.Sell, triggers[0].ResultingOrder.Side);
	}

	[TestMethod]
	public void TrailingStopBuy_TracksMinPrice()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		var info = CreateStopInfo(secId, Sides.Buy, stopPrice: 105, isTrailing: true, trailingOffset: 5);
		mgr.Register(info);

		// Price drops to 90 - best seen = 90, stop = 95
		var triggers = mgr.CheckPrice(secId, 90, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);

		// Price drops to 85 - best seen = 85, stop = 90
		triggers = mgr.CheckPrice(secId, 85, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);

		// Price rises to 88 - still below 90
		triggers = mgr.CheckPrice(secId, 88, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);

		// Price rises to 90 - triggered (85 + 5 = 90)
		triggers = mgr.CheckPrice(secId, 90, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(OrderTypes.Market, triggers[0].ResultingOrder.OrderType);
		Assert.AreEqual(Sides.Buy, triggers[0].ResultingOrder.Side);
	}

	[TestMethod]
	public void Cancel_RemovesStopOrder()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		var info = CreateStopInfo(secId, Sides.Buy, stopPrice: 105, transactionId: 42);
		mgr.Register(info);

		var cancelled = mgr.Cancel(42, out var cancelledInfo);
		Assert.IsTrue(cancelled);
		Assert.AreEqual(42L, cancelledInfo.TransactionId);

		// Price reaches stop - should not trigger
		var triggers = mgr.CheckPrice(secId, 110, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);
	}

	[TestMethod]
	public void Cancel_NonExistent_ReturnsFalse()
	{
		var mgr = new StopOrderManager();

		var cancelled = mgr.Cancel(999, out _);
		Assert.IsFalse(cancelled);
	}

	[TestMethod]
	public void MultipleStops_SameSecurity()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();

		mgr.Register(CreateStopInfo(secId, Sides.Buy, stopPrice: 105, transactionId: 1));
		mgr.Register(CreateStopInfo(secId, Sides.Sell, stopPrice: 95, transactionId: 2));
		mgr.Register(CreateStopInfo(secId, Sides.Buy, stopPrice: 110, transactionId: 3));

		// Price = 106: triggers stop buy at 105, not the one at 110
		var triggers = mgr.CheckPrice(secId, 106, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(1L, triggers[0].Info.TransactionId);

		// Price = 110: triggers stop buy at 110
		triggers = mgr.CheckPrice(secId, 110, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(3L, triggers[0].Info.TransactionId);

		// Price = 94: triggers stop sell at 95
		triggers = mgr.CheckPrice(secId, 94, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(2L, triggers[0].Info.TransactionId);
	}

	[TestMethod]
	public void TriggeredStopIsRemoved()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		mgr.Register(CreateStopInfo(secId, Sides.Buy, stopPrice: 105));

		// Trigger
		var triggers = mgr.CheckPrice(secId, 105, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);

		// Check again - should not trigger again
		triggers = mgr.CheckPrice(secId, 110, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		mgr.Register(CreateStopInfo(secId, Sides.Buy, stopPrice: 105, transactionId: 1));
		mgr.Register(CreateStopInfo(secId, Sides.Sell, stopPrice: 95, transactionId: 2));

		mgr.Clear();

		var triggers = mgr.CheckPrice(secId, 110, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);
	}

	[TestMethod]
	public void DifferentSecurities_Independent()
	{
		var mgr = new StopOrderManager();
		var secId1 = CreateSecId();
		var secId2 = CreateSecId();

		mgr.Register(CreateStopInfo(secId1, Sides.Buy, stopPrice: 105, transactionId: 1));
		mgr.Register(CreateStopInfo(secId2, Sides.Buy, stopPrice: 200, transactionId: 2));

		// Only secId1 should trigger
		var triggers = mgr.CheckPrice(secId1, 110, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);
		Assert.AreEqual(1L, triggers[0].Info.TransactionId);

		// secId2 still pending
		triggers = mgr.CheckPrice(secId2, 150, DateTime.UtcNow);
		Assert.AreEqual(0, triggers.Count);
	}

	[TestMethod]
	public void ResultingOrder_HasCorrectFields()
	{
		var mgr = new StopOrderManager();
		var secId = CreateSecId();
		var info = CreateStopInfo(secId, Sides.Buy, stopPrice: 105, volume: 50, portfolioName: "myPortfolio");
		mgr.Register(info);

		var triggers = mgr.CheckPrice(secId, 105, DateTime.UtcNow);
		Assert.AreEqual(1, triggers.Count);

		var order = triggers[0].ResultingOrder;
		Assert.AreEqual(secId, order.SecurityId);
		Assert.AreEqual(Sides.Buy, order.Side);
		Assert.AreEqual(50m, order.Volume);
		Assert.AreEqual("myPortfolio", order.PortfolioName);
		Assert.AreEqual(OrderTypes.Market, order.OrderType);
	}

	[TestMethod]
	public void StopOrderCondition_PropertiesRoundTrip()
	{
		var cond = new StopOrderCondition
		{
			ActivationPrice = 105.5m,
			ClosePositionPrice = 106m,
			IsTrailing = true,
			TrailingOffset = 3.5m,
		};

		Assert.AreEqual(105.5m, cond.ActivationPrice);
		Assert.AreEqual(106m, cond.ClosePositionPrice);
		Assert.IsTrue(cond.IsTrailing);
		Assert.AreEqual(3.5m, cond.TrailingOffset);
	}
}
