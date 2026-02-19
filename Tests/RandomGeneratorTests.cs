namespace StockSharp.Tests;

[TestClass]
public class RandomGeneratorTests : BaseTestClass
{
	private static Security CreateSecurity()
	{
		var security = Helper.CreateSecurity(100m);
		return security;
	}

	[TestMethod]
	public void RandomTicks_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 100;

		var ticks = security.RandomTicks(count, false);

		AreEqual(count, ticks.Length);

		foreach (var tick in ticks)
		{
			IsNotNull(tick.TradePrice);
			IsNotNull(tick.TradeVolume);
			AreEqual(DataType.Ticks, tick.DataType);
		}
	}

	[TestMethod]
	public void RandomTicks_WithOriginSide_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 100;

		var ticks = security.RandomTicks(count, true);

		AreEqual(count, ticks.Length);
	}

	[TestMethod]
	public void RandomOrderLog_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 100;

		var items = security.RandomOrderLog(count);

		AreEqual(count, items.Length);

		foreach (var item in items)
		{
			AreEqual(DataType.OrderLog, item.DataType);
		}
	}

	[TestMethod]
	public void RandomDepths_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 50;

		var depths = security.RandomDepths(count);

		AreEqual(count, depths.Length);

		foreach (var depth in depths)
		{
			AreEqual(DataType.MarketDepth, depth.DataType);
			IsTrue(depth.Bids.Length > 0 || depth.Asks.Length > 0);
		}
	}

	[TestMethod]
	public void RandomDepths_WithInterval_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 50;

		var depths = security.RandomDepths(count, interval: TimeSpan.FromMilliseconds(100));

		AreEqual(count, depths.Length);
	}

	[TestMethod]
	public void RandomDepths_WithOrdersCount_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 50;

		var depths = security.RandomDepths(count, ordersCount: true);

		AreEqual(count, depths.Length);
	}

	[TestMethod]
	public void RandomLevel1_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 100;

		var items = security.RandomLevel1(count: count);

		AreEqual(count, items.Length);

		foreach (var item in items)
		{
			AreEqual(DataType.Level1, item.DataType);
		}
	}

	[TestMethod]
	public void RandomTransactions_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 100;

		var items = security.RandomTransactions(count);

		AreEqual(count, items.Length);

		foreach (var item in items)
		{
			AreEqual(DataType.Transactions, item.DataType);
		}
	}

	[TestMethod]
	public void RandomPositionChanges_GeneratesExactCount()
	{
		var security = CreateSecurity();
		const int count = 100;

		var items = security.RandomPositionChanges(count);

		AreEqual(count, items.Length);

		foreach (var item in items)
		{
			AreEqual(DataType.PositionChanges, item.DataType);
		}
	}

	[TestMethod]
	public void RandomSecurities_GeneratesExactCount()
	{
		const int count = 100;

		var items = Helper.RandomSecurities(count);

		AreEqual(count, items.Length);
	}

	[TestMethod]
	public void RandomBoards_GeneratesExactCount()
	{
		const int count = 100;

		var items = Helper.RandomBoards(count);

		AreEqual(count, items.Length);
	}

	[TestMethod]
	public void RandomNews_GeneratesItems()
	{
		var items = Helper.RandomNews();

		IsTrue(items.Length > 0);

		foreach (var item in items)
			IsNotNull(item.Headline);
	}

	[TestMethod]
	public void RandomBoardStates_GeneratesItems()
	{
		var items = Helper.RandomBoardStates();

		IsTrue(items.Length > 0);
	}
}
