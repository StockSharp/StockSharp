namespace StockSharp.Tests;

using StockSharp.Algo.Testing.Emulation;

[TestClass]
public class OrderLifecycleManagerTests : BaseTestClass
{
	private static EmulatorOrder CreateOrder(long transactionId, string portfolio = "Test", Sides side = Sides.Buy, DateTime? expiry = null)
	{
		return new EmulatorOrder
		{
			TransactionId = transactionId,
			Side = side,
			Price = 100,
			Balance = 10,
			Volume = 10,
			PortfolioName = portfolio,
			ExpiryDate = expiry,
		};
	}

	[TestMethod]
	public void RegisterOrder_NewOrder_ReturnsTrue()
	{
		var manager = new OrderLifecycleManager();
		var order = CreateOrder(1);
		var now = DateTime.UtcNow;

		var result = manager.RegisterOrder(order, now);

		IsTrue(result);
		AreEqual(1, manager.Count);
	}

	[TestMethod]
	public void RegisterOrder_DuplicateOrder_ReturnsFalse()
	{
		var manager = new OrderLifecycleManager();
		var order1 = CreateOrder(1);
		var order2 = CreateOrder(1); // Same transaction ID
		var now = DateTime.UtcNow;

		manager.RegisterOrder(order1, now);
		var result = manager.RegisterOrder(order2, now);

		IsFalse(result);
		AreEqual(1, manager.Count);
	}

	[TestMethod]
	public void GetOrder_ExistingOrder_ReturnsOrder()
	{
		var manager = new OrderLifecycleManager();
		var order = CreateOrder(1);
		var now = DateTime.UtcNow;

		manager.RegisterOrder(order, now);
		var retrieved = manager.GetOrder(1);

		IsNotNull(retrieved);
		AreEqual(1, retrieved.TransactionId);
	}

	[TestMethod]
	public void GetOrder_NonExistingOrder_ReturnsNull()
	{
		var manager = new OrderLifecycleManager();

		var retrieved = manager.GetOrder(999);

		IsNull(retrieved);
	}

	[TestMethod]
	public void TryGetOrder_ExistingOrder_ReturnsTrueWithOrder()
	{
		var manager = new OrderLifecycleManager();
		var order = CreateOrder(1);
		var now = DateTime.UtcNow;

		manager.RegisterOrder(order, now);
		var found = manager.TryGetOrder(1, out var retrieved);

		IsTrue(found);
		IsNotNull(retrieved);
		AreEqual(1, retrieved.TransactionId);
	}

	[TestMethod]
	public void TryGetOrder_NonExistingOrder_ReturnsFalse()
	{
		var manager = new OrderLifecycleManager();

		var found = manager.TryGetOrder(999, out var retrieved);

		IsFalse(found);
		IsNull(retrieved);
	}

	[TestMethod]
	public void RemoveOrder_ExistingOrder_ReturnsTrue()
	{
		var manager = new OrderLifecycleManager();
		var order = CreateOrder(1);
		var now = DateTime.UtcNow;

		manager.RegisterOrder(order, now);
		var result = manager.RemoveOrder(1);

		IsTrue(result);
		AreEqual(0, manager.Count);
	}

	[TestMethod]
	public void RemoveOrder_NonExistingOrder_ReturnsFalse()
	{
		var manager = new OrderLifecycleManager();

		var result = manager.RemoveOrder(999);

		IsFalse(result);
	}

	[TestMethod]
	public void TryRemoveOrder_ExistingOrder_ReturnsTrueWithOrder()
	{
		var manager = new OrderLifecycleManager();
		var order = CreateOrder(1);
		var now = DateTime.UtcNow;

		manager.RegisterOrder(order, now);
		var found = manager.TryRemoveOrder(1, out var retrieved);

		IsTrue(found);
		IsNotNull(retrieved);
		AreEqual(1, retrieved.TransactionId);
		AreEqual(0, manager.Count);
	}

	[TestMethod]
	public void TryRemoveOrder_NonExistingOrder_ReturnsFalse()
	{
		var manager = new OrderLifecycleManager();

		var found = manager.TryRemoveOrder(999, out var retrieved);

		IsFalse(found);
		IsNull(retrieved);
	}

	[TestMethod]
	public void GetActiveOrders_ReturnsAllOrders()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;

		manager.RegisterOrder(CreateOrder(1), now);
		manager.RegisterOrder(CreateOrder(2), now);
		manager.RegisterOrder(CreateOrder(3), now);

		var orders = manager.GetActiveOrders().ToList();

		AreEqual(3, orders.Count);
	}

	[TestMethod]
	public void GetActiveOrders_ByPortfolio_FiltersCorrectly()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;

		manager.RegisterOrder(CreateOrder(1, "Portfolio1"), now);
		manager.RegisterOrder(CreateOrder(2, "Portfolio1"), now);
		manager.RegisterOrder(CreateOrder(3, "Portfolio2"), now);

		var orders = manager.GetActiveOrders("Portfolio1").ToList();

		AreEqual(2, orders.Count);
		IsTrue(orders.All(o => o.PortfolioName == "Portfolio1"));
	}

	[TestMethod]
	public void GetActiveOrders_ByPortfolioAndSide_FiltersCorrectly()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;

		manager.RegisterOrder(CreateOrder(1, "Portfolio1", Sides.Buy), now);
		manager.RegisterOrder(CreateOrder(2, "Portfolio1", Sides.Sell), now);
		manager.RegisterOrder(CreateOrder(3, "Portfolio1", Sides.Buy), now);

		var orders = manager.GetActiveOrders("Portfolio1", null, Sides.Buy).ToList();

		AreEqual(2, orders.Count);
		IsTrue(orders.All(o => o.Side == Sides.Buy));
	}

	[TestMethod]
	public void GetExpiredOrders_OrderExpired_ReturnsOrder()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;
		var expiry = now.AddHours(1);

		manager.RegisterOrder(CreateOrder(1, expiry: expiry), now);

		// Before expiry
		var expired = manager.GetExpiredOrders(now.AddMinutes(30)).ToList();
		AreEqual(0, expired.Count);

		// After expiry
		expired = [.. manager.GetExpiredOrders(now.AddHours(2))];
		AreEqual(1, expired.Count);
		AreEqual(1, expired[0].TransactionId);
	}

	[TestMethod]
	public void GetExpiredOrders_OrderWithoutExpiry_NotReturned()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;

		manager.RegisterOrder(CreateOrder(1, expiry: null), now);

		var expired = manager.GetExpiredOrders(now.AddYears(100)).ToList();

		AreEqual(0, expired.Count);
	}

	[TestMethod]
	public void ProcessTime_RemovesExpiredOrders()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;
		var expiry = now.AddHours(1);

		manager.RegisterOrder(CreateOrder(1, expiry: expiry), now);
		manager.RegisterOrder(CreateOrder(2, expiry: null), now); // No expiry
		manager.RegisterOrder(CreateOrder(3, expiry: now.AddHours(3)), now); // Not expired yet

		AreEqual(3, manager.Count);

		var expired = manager.ProcessTime(now.AddHours(2)).ToList();

		AreEqual(1, expired.Count);
		AreEqual(1, expired[0].TransactionId);
		AreEqual(2, manager.Count);
	}

	[TestMethod]
	public void Clear_RemovesAllOrders()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;

		manager.RegisterOrder(CreateOrder(1), now);
		manager.RegisterOrder(CreateOrder(2), now);
		manager.RegisterOrder(CreateOrder(3), now);

		AreEqual(3, manager.Count);

		manager.Clear();

		AreEqual(0, manager.Count);
		IsNull(manager.GetOrder(1));
	}

	[TestMethod]
	public void RegisterOrder_ExpiredAtRegistration_NotTrackedForExpiry()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;
		var pastExpiry = now.AddHours(-1); // Already expired

		manager.RegisterOrder(CreateOrder(1, expiry: pastExpiry), now);

		// Should still be in active orders (caller responsible for checking)
		AreEqual(1, manager.Count);

		// Not returned in expired orders since expiry is in the past relative to registration
		// (orders already expired at registration are not tracked for expiry processing)
		var expired = manager.GetExpiredOrders(now).ToList();
		AreEqual(0, expired.Count);
	}

	[TestMethod]
	public void Count_ReturnsCorrectCount()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;

		AreEqual(0, manager.Count);

		manager.RegisterOrder(CreateOrder(1), now);
		AreEqual(1, manager.Count);

		manager.RegisterOrder(CreateOrder(2), now);
		AreEqual(2, manager.Count);

		manager.RemoveOrder(1);
		AreEqual(1, manager.Count);
	}

	[TestMethod]
	public void GetActiveOrders_EmptyManager_ReturnsEmptyEnumerable()
	{
		var manager = new OrderLifecycleManager();

		var orders = manager.GetActiveOrders().ToList();

		AreEqual(0, orders.Count);
	}

	[TestMethod]
	public void RemoveOrder_AlsoRemovesExpiry()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;
		var expiry = now.AddHours(1);

		manager.RegisterOrder(CreateOrder(1, expiry: expiry), now);
		manager.RemoveOrder(1);

		// Re-add same order without expiry
		manager.RegisterOrder(CreateOrder(1, expiry: null), now);

		// Should not be in expired orders
		var expired = manager.GetExpiredOrders(now.AddHours(2)).ToList();
		AreEqual(0, expired.Count);
	}

	[TestMethod]
	public void GetActiveOrders_CaseInsensitivePortfolioMatch()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;

		manager.RegisterOrder(CreateOrder(1, "TestPortfolio"), now);

		var orders1 = manager.GetActiveOrders("testportfolio").ToList();
		var orders2 = manager.GetActiveOrders("TESTPORTFOLIO").ToList();
		var orders3 = manager.GetActiveOrders("TestPortfolio").ToList();

		AreEqual(1, orders1.Count);
		AreEqual(1, orders2.Count);
		AreEqual(1, orders3.Count);
	}

	[TestMethod]
	public void MultipleOrdersWithSameExpiry_AllProcessed()
	{
		var manager = new OrderLifecycleManager();
		var now = DateTime.UtcNow;
		var expiry = now.AddHours(1);

		manager.RegisterOrder(CreateOrder(1, expiry: expiry), now);
		manager.RegisterOrder(CreateOrder(2, expiry: expiry), now);
		manager.RegisterOrder(CreateOrder(3, expiry: expiry), now);

		var expired = manager.ProcessTime(now.AddHours(2)).ToList();

		AreEqual(3, expired.Count);
		AreEqual(0, manager.Count);
	}
}
