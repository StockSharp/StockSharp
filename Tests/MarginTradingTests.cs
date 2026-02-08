namespace StockSharp.Tests;

using StockSharp.Algo.Testing.Emulation;

[TestClass]
public class MarginTradingTests : BaseTestClass
{
	private static SecurityId CreateSecId(string code = "AAPL", string board = "NYSE")
		=> new() { SecurityCode = code, BoardCode = board };

	#region CalculateUnrealizedPnL

	[TestMethod]
	public void CalculateUnrealizedPnL_LongPosition_PriceUp()
	{
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(10000);
		var secId = CreateSecId();

		portfolio.ProcessTrade(secId, Sides.Buy, price: 100, volume: 10);

		var unrealized = portfolio.CalculateUnrealizedPnL(id => id == secId ? 110m : null);

		// (110 - 100) * 10 = 100
		AreEqual(100m, unrealized);
	}

	[TestMethod]
	public void CalculateUnrealizedPnL_LongPosition_PriceDown()
	{
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(10000);
		var secId = CreateSecId();

		portfolio.ProcessTrade(secId, Sides.Buy, price: 100, volume: 10);

		var unrealized = portfolio.CalculateUnrealizedPnL(id => id == secId ? 90m : null);

		// (90 - 100) * 10 = -100
		AreEqual(-100m, unrealized);
	}

	[TestMethod]
	public void CalculateUnrealizedPnL_ShortPosition_PriceDown()
	{
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(10000);
		var secId = CreateSecId();

		portfolio.ProcessTrade(secId, Sides.Sell, price: 100, volume: 10);

		var unrealized = portfolio.CalculateUnrealizedPnL(id => id == secId ? 90m : null);

		// (90 - 100) * (-10) = 100
		AreEqual(100m, unrealized);
	}

	[TestMethod]
	public void CalculateUnrealizedPnL_ShortPosition_PriceUp()
	{
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(10000);
		var secId = CreateSecId();

		portfolio.ProcessTrade(secId, Sides.Sell, price: 100, volume: 10);

		var unrealized = portfolio.CalculateUnrealizedPnL(id => id == secId ? 110m : null);

		// (110 - 100) * (-10) = -100
		AreEqual(-100m, unrealized);
	}

	[TestMethod]
	public void CalculateUnrealizedPnL_NoPosition_ReturnsZero()
	{
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(10000);

		var unrealized = portfolio.CalculateUnrealizedPnL(_ => 110m);

		AreEqual(0m, unrealized);
	}

	[TestMethod]
	public void CalculateUnrealizedPnL_NoPriceAvailable_ReturnsZero()
	{
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(10000);
		var secId = CreateSecId();

		portfolio.ProcessTrade(secId, Sides.Buy, price: 100, volume: 10);

		var unrealized = portfolio.CalculateUnrealizedPnL(_ => null);

		AreEqual(0m, unrealized);
	}

	[TestMethod]
	public void CalculateUnrealizedPnL_MultiplePositions_SumsCorrectly()
	{
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(100000);

		var secId1 = CreateSecId("AAPL", "NYSE");
		var secId2 = CreateSecId("MSFT", "NASDAQ");

		portfolio.ProcessTrade(secId1, Sides.Buy, price: 100, volume: 10);
		portfolio.ProcessTrade(secId2, Sides.Sell, price: 200, volume: 10);

		var unrealized = portfolio.CalculateUnrealizedPnL(id =>
		{
			if (id == secId1) return 110m;
			if (id == secId2) return 190m;
			return null;
		});

		// (110 - 100) * 10 + (190 - 200) * (-10) = 100 + 100 = 200
		AreEqual(200m, unrealized);
	}

	#endregion

	#region PositionInfo.Leverage

	[TestMethod]
	public void PositionInfo_Leverage_DefaultNull()
	{
		var pos = new PositionInfo(CreateSecId());

		IsNull(pos.Leverage);
	}

	[TestMethod]
	public void PositionInfo_Leverage_CanBeSet()
	{
		var pos = new PositionInfo(CreateSecId());
		pos.Leverage = 10m;

		AreEqual(10m, pos.Leverage);
	}

	#endregion

	#region IPortfolio margin settings

	[TestMethod]
	public void EmulatedPortfolio_MarginCallLevel_Default()
	{
		var portfolio = new EmulatedPortfolio("test");

		AreEqual(0.5m, portfolio.MarginCallLevel);
	}

	[TestMethod]
	public void EmulatedPortfolio_StopOutLevel_Default()
	{
		var portfolio = new EmulatedPortfolio("test");

		AreEqual(0.2m, portfolio.StopOutLevel);
	}

	[TestMethod]
	public void EmulatedPortfolio_EnableStopOut_DefaultFalse()
	{
		var portfolio = new EmulatedPortfolio("test");

		IsFalse(portfolio.EnableStopOut);
	}

	#endregion

	#region MarginController - GetRequiredMargin

	[TestMethod]
	public void GetRequiredMargin_NoLeverage()
	{
		var mc = new MarginController();

		// position = null => leverage 1 => 100 * 10 / 1 = 1000
		AreEqual(1000m, mc.GetRequiredMargin(100, 10, null));
	}

	[TestMethod]
	public void GetRequiredMargin_WithLeverage10x()
	{
		var mc = new MarginController();
		var pos = new PositionInfo(CreateSecId()) { Leverage = 10m };

		// 100 * 10 / 10 = 100
		AreEqual(100m, mc.GetRequiredMargin(100, 10, pos));
	}

	[TestMethod]
	public void GetRequiredMargin_DifferentPositionsDifferentLeverage()
	{
		var mc = new MarginController();
		var pos1 = new PositionInfo(CreateSecId("AAPL", "NYSE")) { Leverage = 5m };
		var pos2 = new PositionInfo(CreateSecId("MSFT", "NASDAQ")) { Leverage = 20m };

		AreEqual(200m, mc.GetRequiredMargin(100, 10, pos1));  // 1000 / 5
		AreEqual(50m, mc.GetRequiredMargin(100, 10, pos2));   // 1000 / 20
	}

	#endregion

	#region MarginController - ValidateOrder

	[TestMethod]
	public void ValidateOrder_SufficientFunds_ReturnsNull()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(10000);

		var error = mc.ValidateOrder(portfolio, price: 100, volume: 10, position: null);

		IsNull(error);
	}

	[TestMethod]
	public void ValidateOrder_InsufficientFunds_ReturnsError()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(500);

		var error = mc.ValidateOrder(portfolio, price: 100, volume: 10, position: null);

		IsNotNull(error);
	}

	[TestMethod]
	public void ValidateOrder_WithLeverage_SufficientAfterLeverage()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(500);
		var pos = new PositionInfo(CreateSecId()) { Leverage = 10m };

		// Without leverage: need 1000, would fail
		// With 10x leverage: need 100 < 500, should pass
		var error = mc.ValidateOrder(portfolio, price: 100, volume: 10, position: pos);

		IsNull(error);
	}

	#endregion

	#region MarginController - IsMarginCall

	[TestMethod]
	public void IsMarginCall_BelowThreshold_ReturnsTrue()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(1000);
		portfolio.MarginCallLevel = 0.5m;
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10, 100);

		// equity = 1000 + (-600) = 400, marginLevel = 400 / 1000 = 0.4 <= 0.5
		IsTrue(mc.IsMarginCall(portfolio, -600));
	}

	[TestMethod]
	public void IsMarginCall_AboveThreshold_ReturnsFalse()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(1000);
		portfolio.MarginCallLevel = 0.5m;
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10, 100);

		// marginLevel = 1000 / 1000 = 1.0 > 0.5
		IsFalse(mc.IsMarginCall(portfolio, 0));
	}

	#endregion

	#region MarginController - IsStopOut

	[TestMethod]
	public void IsStopOut_Disabled_ReturnsFalse()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(1000);
		portfolio.EnableStopOut = false;
		portfolio.StopOutLevel = 0.2m;
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10, 100);

		// Even below stop-out level, should return false when disabled
		IsFalse(mc.IsStopOut(portfolio, -900));
	}

	[TestMethod]
	public void IsStopOut_Enabled_BelowThreshold_ReturnsTrue()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(1000);
		portfolio.EnableStopOut = true;
		portfolio.StopOutLevel = 0.2m;
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10, 100);

		// equity = 1000 + (-900) = 100, marginLevel = 100 / 1000 = 0.1 <= 0.2
		IsTrue(mc.IsStopOut(portfolio, -900));
	}

	#endregion

	#region MarginController - CheckMarginLevel

	[TestMethod]
	public void CheckMarginLevel_NoBlockedMoney_ReturnsMaxValue()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(1000);

		AreEqual(decimal.MaxValue, mc.CheckMarginLevel(portfolio, 0));
	}

	[TestMethod]
	public void CheckMarginLevel_WithBlockedMoney_CalculatesCorrectly()
	{
		var mc = new MarginController();
		var portfolio = new EmulatedPortfolio("test");
		portfolio.SetMoney(1000);
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10, 100);

		// equity = 1000, blocked = 1000, marginLevel = 1.0
		AreEqual(1.0m, mc.CheckMarginLevel(portfolio, 0));
	}

	#endregion

	#region EmulatedPortfolioManager with MarginController

	[TestMethod]
	public void ValidateFunds_WithMarginController_UsesPositionLeverage()
	{
		var manager = new EmulatedPortfolioManager();
		manager.MarginController = new MarginController();

		var portfolio = (EmulatedPortfolio)manager.GetPortfolio("test");
		portfolio.SetMoney(500);

		var secId = CreateSecId();

		// Set position with leverage 10x
		portfolio.SetPosition(secId, 0);
		portfolio.GetPosition(secId).Leverage = 10m;

		// Need 100 * 10 / 10 = 100 < 500
		var error = manager.ValidateFunds("test", secId, 100, 10);
		IsNull(error);
	}

	[TestMethod]
	public void ValidateFunds_WithMarginController_NoPosition_UsesDefaultLeverage()
	{
		var manager = new EmulatedPortfolioManager();
		manager.MarginController = new MarginController();

		var portfolio = manager.GetPortfolio("test");
		portfolio.SetMoney(500);

		var secId = CreateSecId();

		// No position => leverage 1 => need 1000 > 500
		var error = manager.ValidateFunds("test", secId, 100, 10);
		IsNotNull(error);
	}

	[TestMethod]
	public void ValidateFunds_WithoutMarginController_UsesOriginalLogic()
	{
		var manager = new EmulatedPortfolioManager();

		var portfolio = manager.GetPortfolio("test");
		portfolio.SetMoney(500);

		var secId = CreateSecId();

		// Original logic: need 100 * 10 = 1000 > 500
		var error = manager.ValidateFunds("test", secId, 100, 10);
		IsNotNull(error);
	}

	#endregion
}
