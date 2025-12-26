namespace StockSharp.Tests;

[TestClass]
public class SnapshotRegistryTests : BaseTestClass
{
	private static Level1ChangeMessage CreateLevel1(SecurityId secId, DateTime serverTime, long seqNum, params (Level1Fields field, object value)[] changes)
	{
		var msg = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = serverTime,
			LocalTime = serverTime,
			SeqNum = seqNum,
		};

		foreach (var (field, value) in changes)
			msg.Changes[field] = value;

		return msg;
	}

	private static QuoteChangeMessage CreateOrderBook(SecurityId secId, DateTime serverTime, long seqNum, QuoteChange[] bids, QuoteChange[] asks) => new()
	{
		SecurityId = secId,
		ServerTime = serverTime,
		LocalTime = serverTime,
		SeqNum = seqNum,
		Bids = bids,
		Asks = asks,
	};

	[TestMethod]
	public void Level1_Update_Get_ReturnsLatestAcrossDates()
	{
		var path = Helper.GetSubTemp();
		using var registry = new SnapshotRegistry(Helper.MemorySystem, path);

		var snapshotRegistry = (ISnapshotRegistry)registry;
		var storage = (ISnapshotStorage<SecurityId, Level1ChangeMessage>)snapshotRegistry.GetSnapshotStorage(DataType.Level1);

		var secId = Helper.CreateSecurityId();

		var t1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var t2 = t1.AddMinutes(1);
		var t3 = t1.AddDays(1);

		storage.Update(CreateLevel1(secId, t1, seqNum: 1, (Level1Fields.LastTradePrice, 100m), (Level1Fields.BestBidPrice, 99m)));
		storage.Update(CreateLevel1(secId, t2, seqNum: 2, (Level1Fields.LastTradePrice, 101m), (Level1Fields.LastTradeVolume, 5m), (Level1Fields.BestBidPrice, 100m)));

		var snapDay1 = storage.Get(secId);
		snapDay1.AssertNotNull();
		snapDay1.ServerTime.AssertEqual(t2);
		snapDay1.SeqNum.AssertEqual(2L);
		((decimal)snapDay1.Changes[Level1Fields.LastTradePrice]).AssertEqual(101m);
		((decimal)snapDay1.Changes[Level1Fields.LastTradeVolume]).AssertEqual(5m);
		((decimal)snapDay1.Changes[Level1Fields.BestBidPrice]).AssertEqual(100m);

		storage.Dates.Count().AssertEqual(1);
		storage.Dates.Contains(t1.Date).AssertTrue();

		storage.Update(CreateLevel1(secId, t3, seqNum: 3, (Level1Fields.LastTradePrice, 200m)));

		storage.Dates.Count().AssertEqual(2);
		storage.Dates.Contains(t1.Date).AssertTrue();
		storage.Dates.Contains(t3.Date).AssertTrue();

		var latest = storage.Get(secId);
		latest.AssertNotNull();
		latest.ServerTime.AssertEqual(t3);
		latest.SeqNum.AssertEqual(3L);
		((decimal)latest.Changes[Level1Fields.LastTradePrice]).AssertEqual(200m);

		var all = storage.GetAll().ToArray();
		all.Length.AssertEqual(2);
		all.Any(m => m.ServerTime == t2).AssertTrue();
		all.Any(m => m.ServerTime == t3).AssertTrue();
	}

	[TestMethod]
	public void Level1_Clear_And_ClearAll()
	{
		var path = Helper.GetSubTemp();
		using var registry = new SnapshotRegistry(Helper.MemorySystem, path);

		var snapshotRegistry = (ISnapshotRegistry)registry;
		var storage = (ISnapshotStorage<SecurityId, Level1ChangeMessage>)snapshotRegistry.GetSnapshotStorage(DataType.Level1);

		var secId1 = Helper.CreateSecurityId();
		var secId2 = Helper.CreateSecurityId();

		var t1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		storage.Update(CreateLevel1(secId1, t1, seqNum: 1, (Level1Fields.LastTradePrice, 100m)));
		storage.Update(CreateLevel1(secId2, t1, seqNum: 2, (Level1Fields.LastTradePrice, 200m)));

		storage.Get(secId1).AssertNotNull();
		storage.Get(secId2).AssertNotNull();

		storage.Clear(secId1);
		storage.Get(secId1).AssertNull();
		storage.Get(secId2).AssertNotNull();

		storage.ClearAll();
		storage.Get(secId1).AssertNull();
		storage.Get(secId2).AssertNull();
		storage.GetAll().Any().AssertFalse();

		// Dates are tracked separately and are not removed by Clear/ClearAll.
		storage.Dates.Contains(t1.Date).AssertTrue();
	}

	[TestMethod]
	public void MarketDepth_Update_Get_And_ClonesSnapshots()
	{
		var path = Helper.GetSubTemp();
		using var registry = new SnapshotRegistry(Helper.MemorySystem, path);

		var snapshotRegistry = (ISnapshotRegistry)registry;
		var storage = (ISnapshotStorage<SecurityId, QuoteChangeMessage>)snapshotRegistry.GetSnapshotStorage(DataType.MarketDepth);

		var secId = Helper.CreateSecurityId();

		var t1 = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var t2 = t1.AddMinutes(1);

		storage.Update(CreateOrderBook(secId, t1, seqNum: 1,
			bids: [new QuoteChange(99m, 10m), new QuoteChange(98m, 5m)],
			asks: [new QuoteChange(101m, 7m), new QuoteChange(102m, 3m)]));

		storage.Update(CreateOrderBook(secId, t2, seqNum: 2,
			bids: [new QuoteChange(100m, 11m)],
			asks: [new QuoteChange(101m, 1m)]));

		var snap1 = storage.Get(secId);
		snap1.AssertNotNull();
		snap1.ServerTime.AssertEqual(t2);
		snap1.SeqNum.AssertEqual(2L);
		snap1.Bids.Length.AssertEqual(1);
		snap1.Asks.Length.AssertEqual(1);
		snap1.Bids[0].Price.AssertEqual(100m);
		snap1.Bids[0].Volume.AssertEqual(11m);
		snap1.Asks[0].Price.AssertEqual(101m);
		snap1.Asks[0].Volume.AssertEqual(1m);

		// Verify Get returns clones (mutating returned snapshot must not affect stored state).
		snap1.Bids[0] = new QuoteChange(1m, 1m);

		var snap2 = storage.Get(secId);
		snap2.AssertNotNull();
		snap2.Bids[0].Price.AssertEqual(100m);
		snap2.Bids[0].Volume.AssertEqual(11m);
	}

	[TestMethod]
	public void UnsupportedDataType_Throws()
	{
		var path = Helper.GetSubTemp();
		using var registry = new SnapshotRegistry(Helper.MemorySystem, path);

		var snapshotRegistry = (ISnapshotRegistry)registry;
		ThrowsExactly<ArgumentOutOfRangeException>(() => snapshotRegistry.GetSnapshotStorage(DataType.Ticks));
	}
}

