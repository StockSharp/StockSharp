namespace StockSharp.Tests;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Slippage;

/// <summary>
/// Tests for manager Clone() functionality.
/// These tests verify that managers can be cloned.
/// </summary>
[TestClass]
public class ManagerCloneTests
{
	[TestMethod]
	public void LatencyManager_Clone_CreatesNewInstance()
	{
		// Arrange
		var state = new LatencyManagerState();
		var manager = new LatencyManager(state);

		// Act - uses the instance Clone() method
		var clone = manager.Clone();

		// Assert
		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void LatencyManager_Clone_PreservesSettings()
	{
		// Arrange
		var state = new LatencyManagerState();
		var manager = new LatencyManager(state);

		// Act
		var clone = manager.Clone();

		// Assert - both should have same initial state
		clone.LatencyRegistration.AssertEqual(manager.LatencyRegistration);
		clone.LatencyCancellation.AssertEqual(manager.LatencyCancellation);
	}

	[TestMethod]
	public void SlippageManager_Clone_CreatesNewInstance()
	{
		// Arrange
		var state = new SlippageManagerState();
		var manager = new SlippageManager(state);

		// Act
		var clone = manager.Clone();

		// Assert
		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void SlippageManager_Clone_PreservesSettings()
	{
		// Arrange
		var state = new SlippageManagerState();
		var manager = new SlippageManager(state);
		manager.CalculateNegative = false; // Change from default

		// Act
		var clone = (SlippageManager)manager.Clone();

		// Assert - settings should be preserved
		clone.CalculateNegative.AssertEqual(false);
	}

	[TestMethod]
	public void SlippageManager_Clone_HasIndependentState()
	{
		// Arrange
		var state = new SlippageManagerState();
		var manager = new SlippageManager(state);

		// Act
		var clone = manager.Clone();

		// Process message on original to add slippage
		var l1Msg = new Level1ChangeMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			ServerTime = DateTime.UtcNow,
		};
		l1Msg.TryAdd(Level1Fields.BestBidPrice, 100m);
		l1Msg.TryAdd(Level1Fields.BestAskPrice, 101m);

		manager.ProcessMessage(l1Msg);

		// Assert - clone should have independent state (slippage = 0 still)
		clone.Slippage.AssertEqual(0);
	}

	[TestMethod]
	public void CommissionManager_Clone_CreatesNewInstance()
	{
		// Arrange
		var manager = new CommissionManager();

		// Act - uses IPersistable.Clone() extension method
		var clone = manager.Clone();

		// Assert
		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void PnLManager_Clone_CreatesNewInstance()
	{
		// Arrange
		var manager = new PnLManager();

		// Act - uses IPersistable.Clone() extension method
		var clone = manager.Clone();

		// Assert
		clone.AssertNotNull();
		clone.AssertNotSame(manager);
	}

	[TestMethod]
	public void AllBasketManagersCloneable()
	{
		// Test that all managers used in BasketMessageAdapter can be cloned
		// This is critical because CreateWrappers calls Clone() on all managers

		// LatencyManager
		var latencyManager = new LatencyManager(new LatencyManagerState());
		latencyManager.Clone().AssertNotNull();

		// SlippageManager
		var slippageManager = new SlippageManager(new SlippageManagerState());
		slippageManager.Clone().AssertNotNull();

		// CommissionManager
		var commissionManager = new CommissionManager();
		commissionManager.Clone().AssertNotNull();

		// PnLManager
		var pnlManager = new PnLManager();
		pnlManager.Clone().AssertNotNull();
	}
}
