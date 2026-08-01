namespace StockSharp.Tests;

/// <summary>
/// Meta-information rebuilt from CSV market data files.
/// </summary>
/// <remarks>
/// The CSV meta info is recovered by scanning the whole day file, so it is the one place where
/// a cheaper scan could silently start reporting different counts, times or trailing values.
/// One test per meta info flavour: plain rows with an id, candles, order book increments.
/// </remarks>
[TestClass]
public class CsvMetaInfoTests : BaseTestClass
{
	private static readonly DateTime _start = new(2024, 6, 3, 10, 0, 0, DateTimeKind.Utc);

	// A registry created anew over the same folder is the only way to force the meta info to be
	// re-read from the file instead of being served from the storage's in-memory cache.
	private static IStorageRegistry OpenRegistry(string path)
		=> Helper.MemorySystem.GetStorage(path);

	[TestMethod]
	public async Task Ticks()
	{
		var token = CancellationToken;
		var path = Helper.MemorySystem.GetSubTemp();
		var secId = Helper.CreateSecurityId();

		var trades = Enumerable.Range(0, 10).Select(i => new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradeId = 1000 + i,
			TradePrice = 100 + i,
			TradeVolume = 1 + i,
			ServerTime = _start.AddSeconds(i),
		}).ToArray();

		IsTrue(trades.Length > 0);

		await OpenRegistry(path).GetTickMessageStorage(secId, null, StorageFormats.Csv).SaveAsync(trades, token);

		var storage = (IMarketDataStorage)OpenRegistry(path).GetTickMessageStorage(secId, null, StorageFormats.Csv);
		var meta = await storage.GetMetaInfoAsync(_start.Date, token);

		IsNotNull(meta);
		AreEqual(trades.Length, meta.Count);
		AreEqual(trades.First().ServerTime, meta.FirstTime);
		AreEqual(trades.Last().ServerTime, meta.LastTime);

		// the id belongs to the last row of the file, not to the first one
		AreEqual(trades.Last().TradeId.Value, (long)meta.LastId);
	}

	[TestMethod]
	public async Task Candles()
	{
		var token = CancellationToken;
		var path = Helper.MemorySystem.GetSubTemp();
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromMinutes(1);

		var candles = Enumerable.Range(0, 10).Select(i => new TimeFrameCandleMessage
		{
			SecurityId = secId,
			TypedArg = tf,
			OpenTime = _start.AddMinutes(i),
			OpenPrice = 100 + i,
			HighPrice = 110 + i,
			LowPrice = 90 + i,
			ClosePrice = 105 + i,
			TotalVolume = 1 + i,
			State = CandleStates.Finished,
		}).ToArray();

		IsTrue(candles.Length > 0);

		await OpenRegistry(path).GetTimeFrameCandleMessageStorage(secId, tf, null, StorageFormats.Csv).SaveAsync(candles, token);

		var storage = (IMarketDataStorage)OpenRegistry(path).GetTimeFrameCandleMessageStorage(secId, tf, null, StorageFormats.Csv);
		var meta = await storage.GetMetaInfoAsync(_start.Date, token);

		IsNotNull(meta);
		AreEqual(candles.Length, meta.Count);
		AreEqual(candles.First().OpenTime, meta.FirstTime);
		AreEqual(candles.Last().OpenTime, meta.LastTime);
	}

	[TestMethod]
	public async Task DepthsIncrementalOnly()
	{
		var token = CancellationToken;
		var path = Helper.MemorySystem.GetSubTemp();
		var secId = Helper.CreateSecurityId();

		var depths = Enumerable.Range(0, 3).Select(i => new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = _start.AddSeconds(i),
			Bids = [new(101 + i, 1)],
			Asks = [new(102 + i, 2)],
			State = QuoteChangeStates.SnapshotComplete,
		}).ToArray();

		IsTrue(depths.Length > 0);

		await OpenRegistry(path).GetQuoteMessageStorage(secId, null, StorageFormats.Csv).SaveAsync(depths, token);

		var storage = OpenRegistry(path).GetQuoteMessageStorage(secId, null, StorageFormats.Csv);
		var meta = await ((IMarketDataStorage)storage).GetMetaInfoAsync(_start.Date, token);

		IsNotNull(meta);

		// a book is stored as one row per quote, so the meta info counts rows - one bid plus one ask here
		AreEqual(depths.Length * 2, meta.Count);
		AreEqual(depths.First().ServerTime, meta.FirstTime);
		AreEqual(depths.Last().ServerTime, meta.LastTime);

		var nonIncremental = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = _start.AddSeconds(10),
			Bids = [new(101, 1)],
			Asks = [new(102, 2)],
			State = null,
		};

		// the incremental flag is recovered from the last row of the existing file - losing it
		// would let snapshot and increment books be mixed inside the same day
		await ThrowsExactlyAsync<ArgumentException>(async () => await storage.SaveAsync(new[] { nonIncremental }, token));
	}
}
