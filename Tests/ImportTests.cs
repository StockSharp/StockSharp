namespace StockSharp.Tests;

using StockSharp.Algo.Export;
using StockSharp.Algo.Import;

[TestClass]
public class ImportTests : BaseTestClass
{
	private static readonly TimeSpan _1mcs = TimeSpan.
#if NET9_0_OR_GREATER
		FromMicroseconds(1)
#else
		FromTicks(TimeHelper.TicksPerMicrosecond)
#endif
	;

	private static string GetTemplate(DataType dataType)
	{
		var registry = new TemplateTxtRegistry();

		if (dataType == DataType.Ticks)
			return registry.TemplateTxtTick;
		else if (dataType == DataType.MarketDepth)
			return registry.TemplateTxtDepth;
		else if (dataType == DataType.OrderLog)
			return registry.TemplateTxtOrderLog;
		else if (dataType == DataType.PositionChanges)
			return registry.TemplateTxtPositionChange;
		else if (dataType == DataType.News)
			return registry.TemplateTxtNews;
		else if (dataType == DataType.Level1)
			return registry.TemplateTxtLevel1;
		else if (dataType == DataType.Board)
			return registry.TemplateTxtBoard;
		else if (dataType == DataType.BoardState)
			return registry.TemplateTxtBoardState;
		else if (dataType == DataType.Transactions)
			return registry.TemplateTxtTransaction;
		else if (dataType.IsCandles)
			return registry.TemplateTxtCandle;
		else if (dataType == DataType.Securities)
			return registry.TemplateTxtSecurity;
		else
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unsupported data type for import test.");
	}

	private async Task Import<TValue>(DataType dataType, bool addSecId, IEnumerable<TValue> values, FieldMapping[] fields, TimeSpan truncate, int? exportCnt = default, int? importCnt = default, DateTime? lastTime2 = default)
		where TValue : class
	{
		var arr = values.ToArray();
		var hasTime = typeof(TValue).Is<IServerTimeMessage>();

		exportCnt ??= arr.Length;
		importCnt ??= arr.Length;

		var template = GetTemplate(dataType);

		if (addSecId)
			template = "{SecurityId.SecurityCode};{SecurityId.BoardCode};" + template;

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var token = CancellationToken;

		var fs = Helper.MemorySystem;
		var filePath = fs.GetSubTemp($"{dataType.DataTypeToFileName()}_import.csv");

		// Export to memory file system
		using (var stream = fs.OpenWrite(filePath))
		{
			var (count, lastTime) = await new TextExporter(dataType, stream, template, null).Export(arr, token);

			count.AssertEqual(exportCnt.Value);

			if (hasTime && exportCnt > 0)
				lastTime.AssertEqual(lastTime2 ?? ((IServerTimeMessage)arr.Last()).ServerTime);
		}

		// Parser check
		using (var stream = fs.OpenRead(filePath))
		{
			var parser = new CsvParser(dataType, fields)
			{
				ColumnSeparator = ";"
			};

			var msgs = await parser.Parse(stream).ToArrayAsync(token);

			msgs.Length.AssertEqual(importCnt.Value);

			if (hasTime && importCnt.Value > 0)
				((IServerTimeMessage)msgs.Last()).ServerTime.AssertEqual((lastTime2 ?? ((IServerTimeMessage)arr.Last()).ServerTime).Truncate(truncate));
		}

		var storageRegistry = fs.GetStorage(fs.GetSubTemp());

		// Importer check
		using (var stream = fs.OpenRead(filePath))
		{
			var importer = new CsvImporter(dataType, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => storageRegistry.GetStorage(secId, dataType))
			{
				ColumnSeparator = ";"
			};

			var (count, lastTime) = await importer.Import(stream, _ => { }, token);

			count.AssertEqual(importCnt.Value);

			if (hasTime && importCnt.Value > 0)
				lastTime.AssertEqual((lastTime2 ?? ((IServerTimeMessage)arr.Last()).ServerTime).Truncate(truncate));
		}
	}

	[TestMethod]
	public Task Ticks()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Ticks).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "TradeId"),
			allFields.First(f => f.Name == "TradePrice"),
			allFields.First(f => f.Name == "TradeVolume"),
			allFields.First(f => f.Name == "OriginSide"),
		};
		return Import(DataType.Ticks, true, security.RandomTicks(100, true), fields, _1mcs);
	}

	[TestMethod]
	public Task Depths()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.MarketDepth).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Price"),
			allFields.First(f => f.Name == "Volume"),
			allFields.First(f => f.Name == "Side"),
		};
		var depths = security.RandomDepths(100, ordersCount: true);
		return Import(DataType.MarketDepth, true, depths, fields, _1mcs, depths.Sum(q => q.ToTimeQuotes().Count()));
	}

	[TestMethod]
	public Task OrderLog()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.OrderLog).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "IsSystem"),
			allFields.First(f => f.Name == "OrderId"),
			allFields.First(f => f.Name == "OrderPrice"),
			allFields.First(f => f.Name == "OrderVolume"),
			allFields.First(f => f.Name == "Side"),
			allFields.First(f => f.Name == "OrderState"),
			allFields.First(f => f.Name == "TimeInForce"),
			allFields.First(f => f.Name == "TradeId"),
			allFields.First(f => f.Name == "TradePrice"),
		};
		return Import(DataType.OrderLog, true, security.RandomOrderLog(100), fields, _1mcs);
	}

	[TestMethod]
	public Task Positions()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.PositionChanges).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "PortfolioName"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Changes[CurrentValue]"),
			allFields.First(f => f.Name == "Changes[BlockedValue]"),
			allFields.First(f => f.Name == "Changes[RealizedPnL]"),
			allFields.First(f => f.Name == "Changes[UnrealizedPnL]"),
			allFields.First(f => f.Name == "Changes[AveragePrice]"),
			allFields.First(f => f.Name == "Changes[Commission]"),
		};
		return Import(DataType.PositionChanges, false, security.RandomPositionChanges(100), fields, TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	public Task News()
	{
		var allFields = FieldMappingRegistry.CreateFields(DataType.News).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Headline"),
			allFields.First(f => f.Name == "Source"),
			allFields.First(f => f.Name == "Url"),
		};
		return Import(DataType.News, false, Helper.RandomNews(), fields, TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	public Task Level1()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Level1).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Changes[BestBidPrice]"),
			allFields.First(f => f.Name == "Changes[BestBidVolume]"),
			allFields.First(f => f.Name == "Changes[BestAskPrice]"),
			allFields.First(f => f.Name == "Changes[BestAskVolume]"),
			allFields.First(f => f.Name == "Changes[LastTradePrice]"),
			allFields.First(f => f.Name == "Changes[LastTradeVolume]"),
		};
		return Import(DataType.Level1, true, security.RandomLevel1(count: 100), fields, TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	public async Task Candles()
	{
		var security = Helper.CreateStorageSecurity();
		var candles = CandleTests.GenerateCandles(security.RandomTicks(100, true), security, CandleTests.PriceRange.Pips(security), CandleTests.TotalTicks, CandleTests.TimeFrame, CandleTests.VolumeRange, CandleTests.BoxSize, CandleTests.PnF(security), true);

		foreach (var group in candles.GroupBy(c => (type: c.GetType(), arg: c.Arg)))
		{
			var dataType = DataType.Create(group.Key.type, group.Key.arg);
			var allFields = FieldMappingRegistry.CreateFields(dataType).ToArray();
			var fields = new[]
			{
				allFields.First(f => f.Name == "SecurityId.SecurityCode"),
				allFields.First(f => f.Name == "SecurityId.BoardCode"),
				allFields.First(f => f.Name == "OpenTime.Date"),
				allFields.First(f => f.Name == "OpenTime.TimeOfDay"),
				allFields.First(f => f.Name == "OpenPrice"),
				allFields.First(f => f.Name == "HighPrice"),
				allFields.First(f => f.Name == "LowPrice"),
				allFields.First(f => f.Name == "ClosePrice"),
				allFields.First(f => f.Name == "TotalVolume"),
			};
			await Import(dataType, true, group.ToArray(), fields, TimeSpan.FromSeconds(1));
		}
	}

	[TestMethod]
	public Task BoardState()
	{
		var allFields = FieldMappingRegistry.CreateFields(DataType.BoardState).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "BoardCode"),
			allFields.First(f => f.Name == "State"),
		};
		return Import(DataType.BoardState, false, Helper.RandomBoardStates(), fields, TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	public Task Transactions()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Transactions).ToArray();

		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "PortfolioName"),
			allFields.First(f => f.Name == "TransactionId"),
			allFields.First(f => f.Name == "OrderId"),
			allFields.First(f => f.Name == "OrderPrice"),
			allFields.First(f => f.Name == "OrderVolume"),
			allFields.First(f => f.Name == "Balance"),
			allFields.First(f => f.Name == "Side"),
			allFields.First(f => f.Name == "OrderType"),
			allFields.First(f => f.Name == "OrderState"),
			allFields.First(f => f.Name == "TradeId"),
			allFields.First(f => f.Name == "TradePrice"),
			allFields.First(f => f.Name == "TradeVolume"),
		};
		return Import(DataType.Transactions, true, security.RandomTransactions(10), fields, _1mcs);
	}

	[TestMethod]
	public Task Securities()
	{
		var allFields = FieldMappingRegistry.CreateFields(DataType.Securities).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "PriceStep"),
			allFields.First(f => f.Name == "SecurityType"),
			allFields.First(f => f.Name == "VolumeStep"),
			allFields.First(f => f.Name == "Multiplier"),
			allFields.First(f => f.Name == "Decimals"),
		};
		return Import(DataType.Securities, false, Helper.RandomSecurities(10), fields, TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	public Task Boards()
	{
		var allFields = FieldMappingRegistry.CreateFields(DataType.Board).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "ExchangeCode"),
			allFields.First(f => f.Name == "Code"),
			//allFields.First(f => f.Name == "ExpiryTime"),
			//allFields.First(f => f.Name == "TimeZone"),
		};
		return Import(DataType.Board, false, Helper.RandomBoards(10), fields, TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	public Task Depths_OnlyBids()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.MarketDepth).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Price"),
			allFields.First(f => f.Name == "Volume"),
			allFields.First(f => f.Name == "Side"),
		};

		// Create depths with only bids
		var depths = security.RandomDepths(20, ordersCount: false);
		var onlyBids = depths.Select((d, i) =>
		{
			var clone = d.TypedClone();
			clone.Asks = [];
			// Ensure unique timestamps to avoid grouping
			clone.ServerTime = d.ServerTime.AddMilliseconds(i);
			return clone;
		}).ToArray();

		return Import(DataType.MarketDepth, true, onlyBids, fields, _1mcs, onlyBids.Sum(q => q.ToTimeQuotes().Count()));
	}

	[TestMethod]
	public Task Depths_OnlyAsks()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.MarketDepth).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Price"),
			allFields.First(f => f.Name == "Volume"),
			allFields.First(f => f.Name == "Side"),
		};

		// Create depths with only asks
		var depths = security.RandomDepths(20, ordersCount: false);
		var onlyAsks = depths.Select((d, i) =>
		{
			var clone = d.TypedClone();
			clone.Bids = [];
			// Ensure unique timestamps to avoid grouping
			clone.ServerTime = d.ServerTime.AddMilliseconds(i);
			return clone;
		}).ToArray();

		return Import(DataType.MarketDepth, true, onlyAsks, fields, _1mcs, onlyAsks.Sum(q => q.ToTimeQuotes().Count()));
	}

	[TestMethod]
	public Task Depths_Empty()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.MarketDepth).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Price"),
			allFields.First(f => f.Name == "Volume"),
			allFields.First(f => f.Name == "Side"),
		};

		// Create empty depths (to clear the order book)
		var depths = security.RandomDepths(20, ordersCount: false);
		var empty = depths.Select((d, i) =>
		{
			var clone = d.TypedClone();
			clone.Bids = [];
			clone.Asks = [];
			// Ensure unique timestamps to avoid grouping
			clone.ServerTime = d.ServerTime.AddMilliseconds(i);
			return clone;
		}).ToArray();

		return Import(DataType.MarketDepth, true, empty, fields, _1mcs, 0, 0);
	}

	[TestMethod]
	public Task Depths_Mixed()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.MarketDepth).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Price"),
			allFields.First(f => f.Name == "Volume"),
			allFields.First(f => f.Name == "Side"),
		};

		// Create mixed depths: some full, some only bids, some only asks, some empty
		var depths = security.RandomDepths(40, ordersCount: false);
		var mixed = new List<QuoteChangeMessage>();

		for (int i = 0; i < depths.Length; i++)
		{
			var clone = depths[i].TypedClone();

			switch (i % 4)
			{
				case 0:
					// Full depth - keep as is
					break;
				case 1:
					// Only bids
					clone.Asks = [];
					break;
				case 2:
					// Only asks
					clone.Bids = [];
					break;
				case 3:
					// Empty
					clone.Bids = [];
					clone.Asks = [];
					break;
			}

			// Ensure unique timestamps to avoid grouping
			clone.ServerTime = depths[i].ServerTime.AddMilliseconds(i);
			mixed.Add(clone);
		}

		var withQuotes = mixed.Where(q => q.ToTimeQuotes().Any()).ToArray();
		return Import(DataType.MarketDepth, true, mixed.ToArray(), fields, _1mcs, mixed.Sum(q => q.ToTimeQuotes().Count()), withQuotes.Length, withQuotes.Last().ServerTime);
	}

	private const string _tickFullTemplate = "{SecurityId.SecurityCode};{SecurityId.BoardCode};{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss.ffffff};{TradeId};{TradePrice};{TradeVolume}";

	[TestMethod]
	public async Task CsvImporter_ProgressCalculation()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Ticks).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "TradeId"),
			allFields.First(f => f.Name == "TradePrice"),
			allFields.First(f => f.Name == "TradeVolume"),
		};

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var arr = security.RandomTicks(1000, true);

		var fs = Helper.MemorySystem;
		var filePath = fs.GetSubTemp("ticks_progress_import.csv");

		using (var stream = fs.OpenWrite(filePath))
			await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr, CancellationToken);

		var storage = fs.GetStorage(fs.GetSubTemp());

		var importer = new CsvImporter(DataType.Ticks, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => storage.GetTickMessageStorage(secId))
		{
			ColumnSeparator = ";"
		};

		var progresses = new List<int>();

		using (var stream = fs.OpenRead(filePath))
		{
			var (count, lastTime) = await importer.Import(stream, p =>
			{
				if (progresses.Count > 0 && progresses.Last() >= p)
					throw new DuplicateException($"Progress {p} already exist.");

				progresses.Add(p);
			}, CancellationToken);

			// Ensure we reported some progress values and they are non-decreasing
			(progresses.Count > 0).AssertTrue();
			for (var i = 1; i < progresses.Count; i++)
				(progresses[i] >= progresses[i - 1]).AssertTrue();

			(progresses.Max() <= 100).AssertTrue();
			(progresses.Min() >= 0).AssertTrue();

			progresses.First().AssertEqual(1);
			progresses.Last().AssertEqual(100);

			// Ensure importer processed all messages and returned last time equals last message server time
			count.AssertEqual(arr.Length);
			lastTime.AssertNotNull();
			lastTime.Value.AssertEqual(arr.Last().ServerTime.Truncate(_1mcs));
		}
	}

	[TestMethod]
	public async Task CsvImporter_StopsImport()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Ticks).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "TradeId"),
			allFields.First(f => f.Name == "TradePrice"),
			allFields.First(f => f.Name == "TradeVolume"),
		};

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var arr = security.RandomTicks(20000, true);

		var fs = Helper.MemorySystem;
		var filePath = fs.GetSubTemp("ticks_cancel_import.csv");

		using (var stream = fs.OpenWrite(filePath))
			await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr, CancellationToken);

		var storage = fs.GetStorage(fs.GetSubTemp());

		var importer = new CsvImporter(DataType.Ticks, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => storage.GetTickMessageStorage(secId))
		{
			ColumnSeparator = ";"
		};

		var progresses = new List<int>();
		using var cts = new CancellationTokenSource();

		using (var stream = fs.OpenRead(filePath))
		{
			await ThrowsExactlyAsync<OperationCanceledException>(() => importer.Import(stream, p =>
			{
				if (progresses.Count > 0 && progresses.Last() >= p)
					throw new DuplicateException($"Progress {p} already exist.");

				progresses.Add(p);

				if (p >= 40)
					cts.Cancel();
			}, cts.Token).AsTask());
		}

		(progresses.Count > 0).AssertTrue();
	}

	[TestMethod]
	public async Task CsvImporter_ErrorDuringImport()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Ticks).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "SecurityId.BoardCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "TradeId"),
			allFields.First(f => f.Name == "TradePrice"),
			allFields.First(f => f.Name == "TradeVolume"),
		};

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var arr = security.RandomTicks(1000, true);

		var fs = Helper.MemorySystem;
		var filePath = fs.GetSubTemp("ticks_error_import.csv");

		using (var stream = fs.OpenWrite(filePath))
			await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr, CancellationToken);

		// Make one of the field orders invalid (beyond column count) to provoke parsing error
		fields[0].Order = 9999;

		var storage = fs.GetStorage(fs.GetSubTemp());

		var importer = new CsvImporter(DataType.Ticks, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => storage.GetTickMessageStorage(secId))
		{
			ColumnSeparator = ";"
		};

		using (var stream = fs.OpenRead(filePath))
			await ThrowsExactlyAsync<InvalidOperationException>(() => importer.Import(stream, _ => { }, CancellationToken).AsTask());
	}
}