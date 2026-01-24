namespace StockSharp.Tests;

using StockSharp.Algo.Testing.Emulation;

[TestClass]
public class EmulatedPortfolioTests : BaseTestClass
{
	private static SecurityId CreateSecId() => Helper.CreateSecurityId();

	#region Basic Properties

	[TestMethod]
	public void Constructor_SetsName()
	{
		var portfolio = new EmulatedPortfolio("TestPortfolio");

		AreEqual("TestPortfolio", portfolio.Name);
	}

	[TestMethod]
	public void Constructor_NullName_ThrowsArgumentNullException()
	{
		Throws<ArgumentNullException>(() => new EmulatedPortfolio(null));
	}

	[TestMethod]
	public void SetMoney_SetsBeginMoney()
	{
		var portfolio = new EmulatedPortfolio("Test");

		portfolio.SetMoney(10000m);

		AreEqual(10000m, portfolio.BeginMoney);
		AreEqual(10000m, portfolio.CurrentMoney);
		AreEqual(10000m, portfolio.AvailableMoney);
	}

	[TestMethod]
	public void InitialState_AllZero()
	{
		var portfolio = new EmulatedPortfolio("Test");

		AreEqual(0m, portfolio.BeginMoney);
		AreEqual(0m, portfolio.CurrentMoney);
		AreEqual(0m, portfolio.AvailableMoney);
		AreEqual(0m, portfolio.RealizedPnL);
		AreEqual(0m, portfolio.TotalPnL);
		AreEqual(0m, portfolio.BlockedMoney);
		AreEqual(0m, portfolio.Commission);
	}

	#endregion

	#region Position Management

	[TestMethod]
	public void SetPosition_CreatesNewPosition()
	{
		var portfolio = new EmulatedPortfolio("Test");
		var secId = CreateSecId();

		portfolio.SetPosition(secId, 100m, 50m);

		var pos = portfolio.GetPosition(secId);
		IsNotNull(pos);
		AreEqual(100m, pos.BeginValue);
		AreEqual(100m, pos.CurrentValue);
		AreEqual(50m, pos.AveragePrice);
		AreEqual(0m, pos.Diff);
	}

	[TestMethod]
	public void SetPosition_UpdatesExistingPosition()
	{
		var portfolio = new EmulatedPortfolio("Test");
		var secId = CreateSecId();

		portfolio.SetPosition(secId, 100m, 50m);
		portfolio.SetPosition(secId, 200m, 60m);

		var pos = portfolio.GetPosition(secId);
		AreEqual(200m, pos.BeginValue);
		AreEqual(60m, pos.AveragePrice);
	}

	[TestMethod]
	public void GetPosition_NonExisting_ReturnsNull()
	{
		var portfolio = new EmulatedPortfolio("Test");
		var secId = CreateSecId();

		var pos = portfolio.GetPosition(secId);

		IsNull(pos);
	}

	[TestMethod]
	public void GetPositions_ReturnsAllPositions()
	{
		var portfolio = new EmulatedPortfolio("Test");
		var secId1 = CreateSecId();
		var secId2 = CreateSecId();

		portfolio.SetPosition(secId1, 100m, 50m);
		portfolio.SetPosition(secId2, 200m, 60m);

		var positions = portfolio.GetPositions().ToList();

		AreEqual(2, positions.Count);
	}

	[TestMethod]
	public void GetAllPositions_ReturnsPositionInfos()
	{
		var portfolio = new EmulatedPortfolio("Test");
		var secId = CreateSecId();

		portfolio.SetPosition(secId, 100m, 50m);

		var positions = portfolio.GetAllPositions().ToList();

		AreEqual(1, positions.Count);
		AreEqual(secId, positions[0].SecurityId);
	}

	#endregion

	#region Trade Processing - Opening Positions

	[TestMethod]
	public void ProcessTrade_OpenLongPosition_UpdatesPositionAndAvgPrice()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		var result = portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m);

		AreEqual(10m, result.PositionChange);
		AreEqual(0m, result.RealizedPnL);
		AreEqual(10m, result.Position.CurrentValue);
		AreEqual(100m, result.Position.AveragePrice);
	}

	[TestMethod]
	public void ProcessTrade_OpenShortPosition_UpdatesPositionAndAvgPrice()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		var result = portfolio.ProcessTrade(secId, Sides.Sell, 100m, 10m);

		AreEqual(-10m, result.PositionChange);
		AreEqual(0m, result.RealizedPnL);
		AreEqual(-10m, result.Position.CurrentValue);
		AreEqual(100m, result.Position.AveragePrice);
	}

	[TestMethod]
	public void ProcessTrade_IncreaseLongPosition_RecalculatesAvgPrice()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Buy 10 at 100
		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m);
		// Buy 10 more at 120
		var result = portfolio.ProcessTrade(secId, Sides.Buy, 120m, 10m);

		AreEqual(20m, result.Position.CurrentValue);
		// Avg = (100*10 + 120*10) / 20 = 110
		AreEqual(110m, result.Position.AveragePrice);
		AreEqual(0m, result.RealizedPnL);
	}

	[TestMethod]
	public void ProcessTrade_IncreaseShortPosition_RecalculatesAvgPrice()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Sell 10 at 100
		portfolio.ProcessTrade(secId, Sides.Sell, 100m, 10m);
		// Sell 10 more at 80
		var result = portfolio.ProcessTrade(secId, Sides.Sell, 80m, 10m);

		AreEqual(-20m, result.Position.CurrentValue);
		// Avg = (100*10 + 80*10) / 20 = 90
		AreEqual(90m, result.Position.AveragePrice);
		AreEqual(0m, result.RealizedPnL);
	}

	#endregion

	#region Trade Processing - Closing Positions

	[TestMethod]
	public void ProcessTrade_CloseLongPosition_CalculatesRealizedPnL()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Buy 10 at 100
		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m);
		// Sell 10 at 120 (profit)
		var result = portfolio.ProcessTrade(secId, Sides.Sell, 120m, 10m);

		AreEqual(0m, result.Position.CurrentValue);
		// PnL = (120 - 100) * 10 = 200
		AreEqual(200m, result.RealizedPnL);
		AreEqual(200m, portfolio.RealizedPnL);
	}

	[TestMethod]
	public void ProcessTrade_CloseLongPosition_WithLoss()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Buy 10 at 100
		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m);
		// Sell 10 at 80 (loss)
		var result = portfolio.ProcessTrade(secId, Sides.Sell, 80m, 10m);

		AreEqual(0m, result.Position.CurrentValue);
		// PnL = (80 - 100) * 10 = -200
		AreEqual(-200m, result.RealizedPnL);
		AreEqual(-200m, portfolio.RealizedPnL);
	}

	[TestMethod]
	public void ProcessTrade_CloseShortPosition_CalculatesRealizedPnL()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Sell 10 at 100
		portfolio.ProcessTrade(secId, Sides.Sell, 100m, 10m);
		// Buy 10 at 80 (profit for short)
		var result = portfolio.ProcessTrade(secId, Sides.Buy, 80m, 10m);

		AreEqual(0m, result.Position.CurrentValue);
		// PnL = (80 - 100) * 10 * -1 = 200 (short gains when price drops)
		AreEqual(200m, result.RealizedPnL);
	}

	[TestMethod]
	public void ProcessTrade_CloseShortPosition_WithLoss()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Sell 10 at 100
		portfolio.ProcessTrade(secId, Sides.Sell, 100m, 10m);
		// Buy 10 at 120 (loss for short)
		var result = portfolio.ProcessTrade(secId, Sides.Buy, 120m, 10m);

		AreEqual(0m, result.Position.CurrentValue);
		// PnL = (120 - 100) * 10 * -1 = -200
		AreEqual(-200m, result.RealizedPnL);
	}

	[TestMethod]
	public void ProcessTrade_PartialCloseLongPosition_CalculatesPartialPnL()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Buy 10 at 100
		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m);
		// Sell 5 at 120
		var result = portfolio.ProcessTrade(secId, Sides.Sell, 120m, 5m);

		AreEqual(5m, result.Position.CurrentValue);
		// PnL = (120 - 100) * 5 = 100
		AreEqual(100m, result.RealizedPnL);
		// Avg price should remain 100 for remaining position
		AreEqual(100m, result.Position.AveragePrice);
	}

	#endregion

	#region Trade Processing - Position Flip

	[TestMethod]
	public void ProcessTrade_FlipLongToShort_ClosesAndOpensNew()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Buy 10 at 100
		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m);
		// Sell 15 at 120 (close long, open short of 5)
		var result = portfolio.ProcessTrade(secId, Sides.Sell, 120m, 15m);

		AreEqual(-5m, result.Position.CurrentValue);
		// PnL for closing long: (120 - 100) * 10 = 200
		AreEqual(200m, result.RealizedPnL);
		// New short position at price 120
		AreEqual(120m, result.Position.AveragePrice);
	}

	[TestMethod]
	public void ProcessTrade_FlipShortToLong_ClosesAndOpensNew()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Sell 10 at 100
		portfolio.ProcessTrade(secId, Sides.Sell, 100m, 10m);
		// Buy 15 at 80 (close short, open long of 5)
		var result = portfolio.ProcessTrade(secId, Sides.Buy, 80m, 15m);

		AreEqual(5m, result.Position.CurrentValue);
		// PnL for closing short: (80 - 100) * 10 * -1 = 200
		AreEqual(200m, result.RealizedPnL);
		// New long position at price 80
		AreEqual(80m, result.Position.AveragePrice);
	}

	#endregion

	#region Commission

	[TestMethod]
	public void ProcessTrade_WithCommission_UpdatesCommission()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m, commission: 5m);

		AreEqual(5m, portfolio.Commission);
		AreEqual(-5m, portfolio.TotalPnL); // TotalPnL = RealizedPnL - Commission
	}

	[TestMethod]
	public void ProcessTrade_MultipleCommissions_Accumulates()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m, commission: 5m);
		portfolio.ProcessTrade(secId, Sides.Sell, 110m, 10m, commission: 5m);

		AreEqual(10m, portfolio.Commission);
		// RealizedPnL = 100, TotalPnL = 100 - 10 = 90
		AreEqual(100m, portfolio.RealizedPnL);
		AreEqual(90m, portfolio.TotalPnL);
	}

	[TestMethod]
	public void CurrentMoney_IncludesPnL()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m, commission: 5m);
		portfolio.ProcessTrade(secId, Sides.Sell, 120m, 10m, commission: 5m);

		// RealizedPnL = 200, Commission = 10, TotalPnL = 190
		AreEqual(10190m, portfolio.CurrentMoney);
	}

	#endregion

	#region Order Registration and Cancellation

	[TestMethod]
	public void ProcessOrderRegistration_BlocksFunds()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10m, 100m);

		AreEqual(1000m, portfolio.BlockedMoney);
		AreEqual(9000m, portfolio.AvailableMoney);
	}

	[TestMethod]
	public void ProcessOrderRegistration_MultipleBuyOrders_BlocksSum()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10m, 100m);
		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 5m, 110m);

		// 10*100 + 5*110 = 1550
		AreEqual(1550m, portfolio.BlockedMoney);
		AreEqual(8450m, portfolio.AvailableMoney);
	}

	[TestMethod]
	public void ProcessOrderCancellation_UnblocksFunds()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10m, 100m);
		portfolio.ProcessOrderCancellation(secId, Sides.Buy, 10m, 100m);

		AreEqual(0m, portfolio.BlockedMoney);
		AreEqual(10000m, portfolio.AvailableMoney);
	}

	[TestMethod]
	public void ProcessOrderCancellation_PartialCancel_UnblocksPartially()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10m, 100m);
		portfolio.ProcessOrderCancellation(secId, Sides.Buy, 5m, 100m);

		AreEqual(500m, portfolio.BlockedMoney);
		AreEqual(9500m, portfolio.AvailableMoney);
	}

	[TestMethod]
	public void ProcessTrade_AfterRegistration_UpdatesBlockedCorrectly()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Register buy order
		portfolio.ProcessOrderRegistration(secId, Sides.Buy, 10m, 100m);
		AreEqual(1000m, portfolio.BlockedMoney);

		// Execute trade
		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m);

		// After full execution, blocked should be recalculated
		var pos = portfolio.GetPosition(secId);
		IsNotNull(pos);
		AreEqual(0m, pos.TotalBidsVolume);
	}

	#endregion

	#region Position Info

	[TestMethod]
	public void PositionInfo_CurrentValue_CalculatesCorrectly()
	{
		var pos = new PositionInfo(CreateSecId())
		{
			BeginValue = 100m,
			Diff = 50m
		};

		AreEqual(150m, pos.CurrentValue);
	}

	[TestMethod]
	public void PositionInfo_CurrentValue_WithNegativeDiff()
	{
		var pos = new PositionInfo(CreateSecId())
		{
			BeginValue = 100m,
			Diff = -30m
		};

		AreEqual(70m, pos.CurrentValue);
	}

	#endregion

	#region Edge Cases

	[TestMethod]
	public void ProcessTrade_ZeroVolume_NoChange()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		var result = portfolio.ProcessTrade(secId, Sides.Buy, 100m, 0m);

		AreEqual(0m, result.PositionChange);
		AreEqual(0m, result.Position.CurrentValue);
	}

	[TestMethod]
	public void ProcessTrade_MultipleTrades_AccumulatesPnL()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId = CreateSecId();

		// Trade 1: Buy 10 at 100, Sell at 110 -> PnL = 100
		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 10m);
		portfolio.ProcessTrade(secId, Sides.Sell, 110m, 10m);

		// Trade 2: Buy 20 at 100, Sell at 120 -> PnL = 400
		portfolio.ProcessTrade(secId, Sides.Buy, 100m, 20m);
		portfolio.ProcessTrade(secId, Sides.Sell, 120m, 20m);

		AreEqual(500m, portfolio.RealizedPnL);
	}

	[TestMethod]
	public void ProcessTrade_MultipleSecurities_TrackedSeparately()
	{
		var portfolio = new EmulatedPortfolio("Test");
		portfolio.SetMoney(10000m);
		var secId1 = CreateSecId();
		var secId2 = CreateSecId();

		portfolio.ProcessTrade(secId1, Sides.Buy, 100m, 10m);
		portfolio.ProcessTrade(secId2, Sides.Buy, 200m, 5m);

		var pos1 = portfolio.GetPosition(secId1);
		var pos2 = portfolio.GetPosition(secId2);

		AreEqual(10m, pos1.CurrentValue);
		AreEqual(100m, pos1.AveragePrice);
		AreEqual(5m, pos2.CurrentValue);
		AreEqual(200m, pos2.AveragePrice);
	}

	#endregion
}

[TestClass]
public class EmulatedPortfolioManagerTests : BaseTestClass
{
	[TestMethod]
	public void GetPortfolio_NewPortfolio_CreatesIt()
	{
		var manager = new EmulatedPortfolioManager();

		var portfolio = manager.GetPortfolio("Test");

		IsNotNull(portfolio);
		AreEqual("Test", portfolio.Name);
	}

	[TestMethod]
	public void GetPortfolio_ExistingPortfolio_ReturnsSame()
	{
		var manager = new EmulatedPortfolioManager();

		var portfolio1 = manager.GetPortfolio("Test");
		var portfolio2 = manager.GetPortfolio("Test");

		AreSame(portfolio1, portfolio2);
	}

	[TestMethod]
	public void GetPortfolio_DifferentNames_ReturnsDifferent()
	{
		var manager = new EmulatedPortfolioManager();

		var portfolio1 = manager.GetPortfolio("Test1");
		var portfolio2 = manager.GetPortfolio("Test2");

		AreNotSame(portfolio1, portfolio2);
	}

	[TestMethod]
	public void HasPortfolio_Existing_ReturnsTrue()
	{
		var manager = new EmulatedPortfolioManager();
		manager.GetPortfolio("Test");

		IsTrue(manager.HasPortfolio("Test"));
	}

	[TestMethod]
	public void HasPortfolio_NonExisting_ReturnsFalse()
	{
		var manager = new EmulatedPortfolioManager();

		IsFalse(manager.HasPortfolio("Test"));
	}

	[TestMethod]
	public void GetAllPortfolios_ReturnsAll()
	{
		var manager = new EmulatedPortfolioManager();
		manager.GetPortfolio("Test1");
		manager.GetPortfolio("Test2");
		manager.GetPortfolio("Test3");

		var portfolios = manager.GetAllPortfolios().ToList();

		AreEqual(3, portfolios.Count);
	}

	[TestMethod]
	public void GetAllPortfolios_Empty_ReturnsEmpty()
	{
		var manager = new EmulatedPortfolioManager();

		var portfolios = manager.GetAllPortfolios().ToList();

		AreEqual(0, portfolios.Count);
	}

	[TestMethod]
	public void Clear_RemovesAllPortfolios()
	{
		var manager = new EmulatedPortfolioManager();
		manager.GetPortfolio("Test1");
		manager.GetPortfolio("Test2");

		manager.Clear();

		IsFalse(manager.HasPortfolio("Test1"));
		IsFalse(manager.HasPortfolio("Test2"));
		AreEqual(0, manager.GetAllPortfolios().Count());
	}

	[TestMethod]
	public void GetPortfolio_AfterClear_CreatesNew()
	{
		var manager = new EmulatedPortfolioManager();
		var portfolio1 = manager.GetPortfolio("Test");
		portfolio1.SetMoney(10000m);

		manager.Clear();

		var portfolio2 = manager.GetPortfolio("Test");

		AreNotSame(portfolio1, portfolio2);
		AreEqual(0m, portfolio2.BeginMoney);
	}
}
