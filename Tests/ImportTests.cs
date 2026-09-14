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

	// A known day for the storage roundtrips below: "the right date" cannot be asserted at all when the
	// generated data lands on whatever day the run happens to start.
	private static readonly DateTime _importDay = new(2024, 3, 5, 10, 0, 0, DateTimeKind.Utc);

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
			var (count, lastTime) = await new TextExporter(dataType, stream, template, null).Export(arr.ToAsyncEnumerable(), token);

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

		// Compute the expected exported row count independently of the engine's own
		// ToTimeQuotes() (which TextExporter also uses) so the oracle does not become a
		// tautology: one CSV row is written per quote, i.e. Bids.Length + Asks.Length.
		var expectedRows = depths.Sum(q => q.Bids.Length + q.Asks.Length);
		return Import(DataType.MarketDepth, true, depths, fields, _1mcs, expectedRows);
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
	[Timeout(5_000, CooperativeCancellation = true)]
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

		// The default PositionChange export template writes sub-second precision
		// ({ServerTime:default:HH:mm:ss.ffffff}), so a roundtrip through the default
		// import mapping must preserve time down to a microsecond, exactly like Ticks/
		// OrderLog/Transactions/MarketDepth do. Asserting only 1-second precision would
		// merely pin the silent sub-second loss caused by the import TimeOfDay mapping
		// lacking { Format = "hh:mm:ss.ffffff" } (FieldMappingRegistry.cs:250).
		return Import(DataType.PositionChanges, false, security.RandomPositionChanges(100), fields, _1mcs);
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
	[Timeout(5_000, CooperativeCancellation = true)]
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

		// The default Level1 export template writes sub-second precision
		// ({ServerTime:default:HH:mm:ss.ffffff}), so a roundtrip through the default
		// import mapping must preserve time down to a microsecond, exactly like Ticks/
		// OrderLog/Transactions/MarketDepth do. Level1 updates many times per second, so
		// truncating to 1 second would collapse distinct updates onto the same timestamp.
		// Asserting only 1-second precision would merely pin the silent sub-second loss
		// caused by the import TimeOfDay mapping lacking { Format = "hh:mm:ss.ffffff" }
		// (FieldMappingRegistry.cs:227).
		return Import(DataType.Level1, true, security.RandomLevel1(count: 100), fields, _1mcs);
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
			// ExpiryTime/TimeZone are intentionally absent here because the import field
			// registry does not create mappings for them (see Boards_ImportMappingsCoverExportTemplate).
		};
		return Import(DataType.Board, false, Helper.RandomBoards(10), fields, TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public void Boards_ImportMappingsCoverExportTemplate()
	{
		// The default Board export template writes four fields:
		//   "{ExchangeCode};{Code};{ExpiryTime};{TimeZone}" (TemplateTxtRegistry.cs:130),
		// but the import field registry only creates mappings for Code and ExchangeCode
		// (FieldMappingRegistry.cs:284-285). As a result ExpiryTime and TimeZone are lost on
		// any roundtrip of the engine's own Board export. A correct registry must expose an
		// import mapping for every field the matching export template emits, otherwise the
		// roundtrip silently drops data. This asserts that contract; it is expected to fail
		// until ExpiryTime/TimeZone import mappings are added.
		var fields = FieldMappingRegistry.CreateFields(DataType.Board).ToArray();
		var names = fields.Select(f => f.Name).ToArray();

		names.Contains("ExpiryTime").AssertTrue("Board import is missing an ExpiryTime mapping while the export template writes it.");
		names.Contains("TimeZone").AssertTrue("Board import is missing a TimeZone mapping while the export template writes it.");
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

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_ContentRoundtrip()
	{
		// The other tests in this class only verify counts and the last timestamp. This test
		// closes the main coverage gap noted in the audit: it compares the actual content of
		// every imported tick against its source element-by-element. If the importer swapped
		// price/volume columns, mangled a value, or reordered rows, the count-only oracles
		// would not notice - this one would.
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

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var token = CancellationToken;

		var arr = security.RandomTicks(100, true);

		var template = "{SecurityId.SecurityCode};{SecurityId.BoardCode};" + GetTemplate(DataType.Ticks);

		var fs = Helper.MemorySystem;
		var filePath = fs.GetSubTemp("ticks_content_import.csv");

		using (var stream = fs.OpenWrite(filePath))
		{
			var (count, _) = await new TextExporter(DataType.Ticks, stream, template, null).Export(arr.ToAsyncEnumerable(), token);
			count.AssertEqual(arr.Length);
		}

		ExecutionMessage[] imported;

		using (var stream = fs.OpenRead(filePath))
		{
			var parser = new CsvParser(DataType.Ticks, fields)
			{
				ColumnSeparator = ";"
			};

			var msgs = await parser.Parse(stream).ToArrayAsync(token);
			imported = [.. msgs.Cast<ExecutionMessage>()];
		}

		imported.Length.AssertEqual(arr.Length);

		for (var i = 0; i < arr.Length; i++)
		{
			var expected = arr[i];
			var actual = imported[i];

			// The default tick export writes microsecond precision, so the roundtrip must
			// preserve ServerTime down to a microsecond.
			actual.ServerTime.AssertEqual(expected.ServerTime.Truncate(_1mcs));

			// Compare exactly the columns the export template emits. A column swap or a
			// mangled value (the classic importer bug) would surface here.
			actual.SecurityId.SecurityCode.AssertEqual(expected.SecurityId.SecurityCode);
			actual.SecurityId.BoardCode.AssertEqual(expected.SecurityId.BoardCode);
			actual.TradePrice.AssertEqual(expected.TradePrice);
			actual.TradeVolume.AssertEqual(expected.TradeVolume);
			actual.TradeId.AssertEqual(expected.TradeId);
			actual.OriginSide.AssertEqual(expected.OriginSide);
		}
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
			await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr.ToAsyncEnumerable(), CancellationToken);

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
			await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr.ToAsyncEnumerable(), CancellationToken);

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
			await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr.ToAsyncEnumerable(), CancellationToken);

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

	// The count the importer returns is what it read out of the file, not what the storage kept, and a
	// row filed under the wrong security or the wrong day is counted just the same. So these run the
	// real import path and hand back the registry the importer wrote into, to be read from.
	private async Task<IStorageRegistry> ImportToStorageAsync<TValue>(DataType dataType, TValue[] values, FieldMapping[] fields, int expectedRows)
		where TValue : class
	{
		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var token = CancellationToken;
		var template = "{SecurityId.SecurityCode};{SecurityId.BoardCode};" + GetTemplate(dataType);

		var fs = Helper.MemorySystem;
		var filePath = fs.GetSubTemp($"{dataType.DataTypeToFileName()}_storage_import.csv");

		using (var stream = fs.OpenWrite(filePath))
		{
			var (count, _) = await new TextExporter(dataType, stream, template, null).Export(values.ToAsyncEnumerable(), token);

			count.AssertEqual(expectedRows);
		}

		var registry = fs.GetStorage(fs.GetSubTemp());

		using (var stream = fs.OpenRead(filePath))
		{
			var importer = new CsvImporter(dataType, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => registry.GetStorage(secId, dataType))
			{
				ColumnSeparator = ";"
			};

			await importer.Import(stream, _ => { }, token);
		}

		return registry;
	}

	private async Task<(DateTime[] dates, TMessage[] loaded)> ReadBackAsync<TMessage>(IStorageRegistry registry, SecurityId secId, DataType dataType)
		where TMessage : Message, IServerTimeMessage
	{
		var token = CancellationToken;
		var storage = (IMarketDataStorage<TMessage>)registry.GetStorage(secId, dataType);

		return (await storage.GetDatesAsync().ToArrayAsync(token), await storage.LoadAsync(default, default).ToArrayAsync(token));
	}

	private static void AssertSide(QuoteChange[] expected, QuoteChange[] actual, string side)
	{
		actual.Length.AssertEqual(expected.Length, $"{side} quote count");

		for (var i = 0; i < expected.Length; i++)
		{
			actual[i].Price.AssertEqual(expected[i].Price, $"{side} price at {i}");
			actual[i].Volume.AssertEqual(expected[i].Volume, $"{side} volume at {i}");
		}
	}

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task Depths_ContentRoundtripThroughStorage()
	{
		// Depths were only counted, so a book that came back with its sides swapped, its volumes
		// attached to the wrong prices, or its ask side in reverse order still passed. What a caller is
		// entitled to get back is the book it put in: the same quotes, each on its own side with its own
		// price and volume, in a well-formed book - bids descending, asks ascending, which is what
		// Extensions.Verify defines a book to be, and what every consumer relies on when it reads
		// Asks[0] as the best ask.
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

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

		var depths = security.RandomDepths(20, ordersCount: false);

		// Distinct times: the parser tells one book from the next by the timestamp of its rows.
		for (var i = 0; i < depths.Length; i++)
			depths[i].ServerTime = _importDay.AddSeconds(i);

		var registry = await ImportToStorageAsync(DataType.MarketDepth, depths, fields, depths.Sum(d => d.Bids.Length + d.Asks.Length));

		var (dates, loaded) = await ReadBackAsync<QuoteChangeMessage>(registry, secId, DataType.MarketDepth);

		dates.Length.AssertEqual(1);
		dates[0].AssertEqual(_importDay.Date);

		loaded.Length.AssertEqual(depths.Length);

		for (var i = 0; i < depths.Length; i++)
		{
			var expected = depths[i];
			var actual = loaded[i];

			actual.SecurityId.AssertEqual(secId);
			actual.ServerTime.AssertEqual(expected.ServerTime.Truncate(_1mcs));

			actual.Verify().AssertTrue($"book {i} is not a well-formed order book after the roundtrip");

			AssertSide(expected.Bids, actual.Bids, $"book {i} bids");
			AssertSide(expected.Asks, actual.Asks, $"book {i} asks");
		}
	}

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task OrderLog_ContentRoundtripThroughStorage()
	{
		// Order log was only counted, so a swapped price/volume column or a flipped side survived. Every
		// column the default template carries is compared here, on the rows read back out of the storage
		// the importer wrote to.
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

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

		var ol = security.RandomOrderLog(100, _importDay);

		var registry = await ImportToStorageAsync(DataType.OrderLog, ol, fields, ol.Length);

		var (dates, loaded) = await ReadBackAsync<ExecutionMessage>(registry, secId, DataType.OrderLog);

		dates.Length.AssertEqual(1);
		dates[0].AssertEqual(_importDay.Date);

		loaded.Length.AssertEqual(ol.Length);

		for (var i = 0; i < ol.Length; i++)
		{
			var expected = ol[i];
			var actual = loaded[i];

			actual.SecurityId.AssertEqual(secId);
			actual.ServerTime.AssertEqual(expected.ServerTime.Truncate(_1mcs));

			actual.IsSystem.AssertEqual(expected.IsSystem);
			actual.OrderId.AssertEqual(expected.OrderId);
			actual.OrderPrice.AssertEqual(expected.OrderPrice);
			actual.OrderVolume.AssertEqual(expected.OrderVolume);
			actual.Side.AssertEqual(expected.Side);
			actual.OrderState.AssertEqual(expected.OrderState);
			actual.TimeInForce.AssertEqual(expected.TimeInForce);
			actual.TradeId.AssertEqual(expected.TradeId);
			actual.TradePrice.AssertEqual(expected.TradePrice);
		}
	}

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task Level1_ContentRoundtripThroughStorage()
	{
		// Level1 was only counted. A message carries an arbitrary subset of fields, so the roundtrip has
		// two things to get right: a value that was there comes back under its own field, and a value
		// that was not there does not appear out of nowhere. A message left with no changes at all is
		// not stored - Level1Storage keeps only messages that have changes - so the expected set is
		// derived from the source rather than from the file.
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

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

		Level1Fields[] mapped =
		[
			Level1Fields.BestBidPrice,
			Level1Fields.BestBidVolume,
			Level1Fields.BestAskPrice,
			Level1Fields.BestAskVolume,
			Level1Fields.LastTradePrice,
			Level1Fields.LastTradeVolume,
		];

		var level1 = security.RandomLevel1(count: 100);

		for (var i = 0; i < level1.Length; i++)
			level1[i].ServerTime = _importDay.AddSeconds(i);

		var registry = await ImportToStorageAsync(DataType.Level1, level1, fields, level1.Length);

		var (dates, loaded) = await ReadBackAsync<Level1ChangeMessage>(registry, secId, DataType.Level1);

		dates.Length.AssertEqual(1);
		dates[0].AssertEqual(_importDay.Date);

		var expectedMsgs = level1.Where(m => mapped.Any(m.Changes.ContainsKey)).ToArray();

		loaded.Length.AssertEqual(expectedMsgs.Length);

		for (var i = 0; i < expectedMsgs.Length; i++)
		{
			var expected = expectedMsgs[i];
			var actual = loaded[i];

			actual.SecurityId.AssertEqual(secId);
			actual.ServerTime.AssertEqual(expected.ServerTime.Truncate(_1mcs));

			foreach (var field in mapped)
			{
				var hasValue = expected.Changes.TryGetValue(field, out var value);

				actual.Changes.ContainsKey(field).AssertEqual(hasValue, $"{field} presence changed by the roundtrip");

				if (hasValue)
					actual.Changes[field].AssertEqual(value);
			}

			// Nothing but the mapped fields may survive: the file carries no other value to restore.
			actual.Changes.Keys.Except(mapped).Count().AssertEqual(0);
		}
	}

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task Candles_ContentRoundtripThroughStorage()
	{
		// Candles were only counted, so open/close and high/low could be transposed unnoticed. Time
		// frame candles are used because their open time is aligned to the frame, which the default
		// candle template writes without loss; for the candle kinds whose open time carries sub-second
		// detail the template's precision is a separate question and is not decided here.
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var dataType = CandleTests.TimeFrame.TimeFrame();

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

		var ticks = security.RandomTicks(500, true, TimeSpan.FromSeconds(30), _importDay);
		var candles = CandleTests
			.GenerateCandles(ticks, security, CandleTests.PriceRange.Pips(security), CandleTests.TotalTicks, CandleTests.TimeFrame, CandleTests.VolumeRange, CandleTests.BoxSize, CandleTests.PnF(security), true)
			.OfType<TimeFrameCandleMessage>()
			.ToArray();

		(candles.Length > 1).AssertTrue("the roundtrip needs more than one candle to be worth running");

		var registry = await ImportToStorageAsync(dataType, candles, fields, candles.Length);

		var (dates, loaded) = await ReadBackAsync<CandleMessage>(registry, secId, dataType);

		dates.Length.AssertEqual(1);
		dates[0].AssertEqual(_importDay.Date);

		loaded.Length.AssertEqual(candles.Length);

		for (var i = 0; i < candles.Length; i++)
		{
			var expected = candles[i];
			var actual = loaded[i];

			actual.SecurityId.AssertEqual(secId);
			actual.OpenTime.AssertEqual(expected.OpenTime);

			actual.OpenPrice.AssertEqual(expected.OpenPrice);
			actual.HighPrice.AssertEqual(expected.HighPrice);
			actual.LowPrice.AssertEqual(expected.LowPrice);
			actual.ClosePrice.AssertEqual(expected.ClosePrice);
			actual.TotalVolume.AssertEqual(expected.TotalVolume);
		}
	}

	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task Ticks_TwoSecuritiesAcrossBatchBoundaries()
	{
		// The importer buffers rows and flushes every 1000 of them, grouping each flush by security
		// (CsvImporter.Import/FlushBuffer). A count cannot show a row dropped or written twice where one
		// flush ends and the next begins, nor a row filed under the other security. So this imports two
		// securities interleaved in one file across several flush boundaries and reads each security's
		// storage back: each must hold exactly its own rows, on the right day, in order, unchanged.
		var securityA = Helper.CreateStorageSecurity();
		var securityB = Helper.CreateStorageSecurity();

		var secIdA = securityA.ToSecurityId();
		var secIdB = securityB.ToSecurityId();

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

		// Over the 1000-row flush threshold twice, so the boundary is crossed rather than approached.
		var ticksA = securityA.RandomTicks(1200, true, TimeSpan.FromSeconds(1), _importDay);
		var ticksB = securityB.RandomTicks(1200, true, TimeSpan.FromSeconds(1), _importDay);

		var interleaved = ticksA.Concat(ticksB).OrderBy(t => t.ServerTime).ToArray();

		var registry = await ImportToStorageAsync(DataType.Ticks, interleaved, fields, interleaved.Length);

		async Task Check(SecurityId secId, ExecutionMessage[] expectedTicks)
		{
			var (dates, loaded) = await ReadBackAsync<ExecutionMessage>(registry, secId, DataType.Ticks);

			dates.Length.AssertEqual(1);
			dates[0].AssertEqual(_importDay.Date);

			loaded.Length.AssertEqual(expectedTicks.Length);

			for (var i = 0; i < expectedTicks.Length; i++)
			{
				var expected = expectedTicks[i];
				var actual = loaded[i];

				actual.SecurityId.AssertEqual(secId);
				actual.ServerTime.AssertEqual(expected.ServerTime.Truncate(_1mcs));
				actual.TradeId.AssertEqual(expected.TradeId);
				actual.TradePrice.AssertEqual(expected.TradePrice);
				actual.TradeVolume.AssertEqual(expected.TradeVolume);
				actual.OriginSide.AssertEqual(expected.OriginSide);
			}
		}

		await Check(secIdA, ticksA);
		await Check(secIdB, ticksB);
	}

	// A hand-authored file: an oracle TextExporter had no hand in, so a mistake the exporter and the
	// parser make in the same direction cannot cancel itself out and pass.
	private static string WriteFixedCsv(string name, params string[] rows)
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp(name);

		fs.WriteAllText(path, string.Join(StringHelper.RN, rows) + StringHelper.RN);

		return path;
	}

	// The tick columns of the default export template, in template order, so a file written with
	// _tickFullTemplate and these fields line up cell for cell.
	private static FieldMapping[] TickFields(bool withOriginSide)
	{
		var all = FieldMappingRegistry.CreateFields(DataType.Ticks).ToArray();

		var names = new List<string>
		{
			"SecurityId.SecurityCode",
			"SecurityId.BoardCode",
			"ServerTime.Date",
			"ServerTime.TimeOfDay",
			"TradeId",
			"TradePrice",
			"TradeVolume",
		};

		if (withOriginSide)
			names.Add("OriginSide");

		var fields = names.Select(n => all.First(f => f.Name == n)).ToArray();

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		return fields;
	}

	// Hands back the registry the importer writes into, so a test that expects the import to stop halfway
	// can still read what did reach the storage.
	private static (IStorageRegistry registry, CsvImporter importer) CreateImporter(DataType dataType, FieldMapping[] fields)
	{
		var fs = Helper.MemorySystem;
		var registry = fs.GetStorage(fs.GetSubTemp());

		var importer = new CsvImporter(dataType, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => registry.GetStorage(secId, dataType))
		{
			ColumnSeparator = ";"
		};

		return (registry, importer);
	}

	private async Task<string> ExportTicksAsync(string name, ExecutionMessage[] ticks)
	{
		var fs = Helper.MemorySystem;
		var path = fs.GetSubTemp(name);

		using (var stream = fs.OpenWrite(path))
		{
			var (count, _) = await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(ticks.ToAsyncEnumerable(), CancellationToken);

			count.AssertEqual(ticks.Length);
		}

		return path;
	}

	// Cuts one row down to its first cell, so the parser is asked for a column that row does not have.
	private static string RewriteWithBrokenRow(string sourcePath, string name, int rowIndex)
	{
		var fs = Helper.MemorySystem;
		// The source was written with WriteLine, so its terminator is whatever the platform uses:
		// splitting on a fixed pair finds one line on a system that ends them with a single character.
		var lines = fs.ReadAllText(sourcePath).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

		lines[rowIndex] = lines[rowIndex].Split(';')[0];

		var path = fs.GetSubTemp(name);
		fs.WriteAllText(path, string.Join(StringHelper.RN, lines) + StringHelper.RN);

		return path;
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_FixedCsvIsParsedIntoExactValues()
	{
		// Every roundtrip in this class writes the file with TextExporter and reads it back with CsvParser, so
		// a mistake the two share - a swapped pair of columns, a dropped microsecond - cancels out and passes.
		// This file is written by hand and every expected value is worked out from the format alone, so the
		// parser is measured against the format instead of against the exporter.
		var fields = TickFields(true);

		var path = WriteFixedCsv("ticks_fixed_import.csv",
			"AAPL;NASDAQ;20240305;10:00:00.000001;101;123.4;7;Buy",
			"AAPL;NASDAQ;20240305;10:00:00.500000;102;123.5;11;Sell",
			"AAPL;NASDAQ;20240305;10:00:01.000000;103;123.6;2;Buy");

		ExecutionMessage[] msgs;

		using (var stream = Helper.MemorySystem.OpenRead(path))
		{
			var parser = new CsvParser(DataType.Ticks, fields)
			{
				ColumnSeparator = ";"
			};

			msgs = [.. (await parser.Parse(stream).ToArrayAsync(CancellationToken)).Cast<ExecutionMessage>()];
		}

		msgs.Length.AssertEqual(3);

		// Date and time of day are two columns and have to be added, not one overwriting the other:
		// 20240305 + 10:00:00.000001 is 2024-03-05 10:00:00 UTC plus one microsecond, .500000 is half a
		// second past it, .000000 on the next second is exactly that second.
		DateTime[] expectedTimes =
		[
			_importDay + _1mcs,
			_importDay + TimeSpan.FromMilliseconds(500),
			_importDay + TimeSpan.FromSeconds(1),
		];

		long?[] expectedIds = [101, 102, 103];
		decimal?[] expectedPrices = [123.4m, 123.5m, 123.6m];
		decimal?[] expectedVolumes = [7m, 11m, 2m];
		Sides?[] expectedSides = [Sides.Buy, Sides.Sell, Sides.Buy];

		for (var i = 0; i < msgs.Length; i++)
		{
			var msg = msgs[i];

			msg.SecurityId.SecurityCode.AssertEqual("AAPL");
			msg.SecurityId.BoardCode.AssertEqual("NASDAQ");

			msg.ServerTime.AssertEqual(expectedTimes[i]);

			// Everything downstream treats message times as UTC, so a parsed one must not arrive unspecified.
			msg.ServerTime.Kind.AssertEqual(DateTimeKind.Utc);

			msg.TradeId.AssertEqual(expectedIds[i]);
			msg.TradePrice.AssertEqual(expectedPrices[i]);
			msg.TradeVolume.AssertEqual(expectedVolumes[i]);
			msg.OriginSide.AssertEqual(expectedSides[i]);

			msg.DataTypeEx.AssertEqual(DataType.Ticks);
		}
	}

	[TestMethod]
	[Timeout(30_000, CooperativeCancellation = true)]
	public async Task Transactions_FixedCsvIsImportedIntoStorage()
	{
		// Transactions were checked by row count only, and the count is what came out of the file, not what
		// the storage kept. So a hand-written file goes the whole way in - parser, importer, storage - and
		// every column is read back out and compared with the value that was written into the file.
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

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

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var prefix = $"{secId.SecurityCode};{secId.BoardCode};20240305;";

		var path = WriteFixedCsv("transactions_fixed_import.csv",
			prefix + "10:00:00.000000;PF1;5001;9001;100.5;10;4;Buy;Limit;Active;7001;100.5;6",
			prefix + "10:00:01.000000;PF1;5002;9002;101.5;20;0;Sell;Limit;Done;7002;101.5;20",
			prefix + "10:00:02.000000;PF2;5003;9003;99.5;5;5;Buy;Limit;Active;7003;99.5;5");

		ExecutionMessage[] expected =
		[
			new() { ServerTime = _importDay, PortfolioName = "PF1", TransactionId = 5001, OrderId = 9001, OrderPrice = 100.5m, OrderVolume = 10, Balance = 4, Side = Sides.Buy, OrderType = OrderTypes.Limit, OrderState = OrderStates.Active, TradeId = 7001, TradePrice = 100.5m, TradeVolume = 6 },
			new() { ServerTime = _importDay.AddSeconds(1), PortfolioName = "PF1", TransactionId = 5002, OrderId = 9002, OrderPrice = 101.5m, OrderVolume = 20, Balance = 0, Side = Sides.Sell, OrderType = OrderTypes.Limit, OrderState = OrderStates.Done, TradeId = 7002, TradePrice = 101.5m, TradeVolume = 20 },
			new() { ServerTime = _importDay.AddSeconds(2), PortfolioName = "PF2", TransactionId = 5003, OrderId = 9003, OrderPrice = 99.5m, OrderVolume = 5, Balance = 5, Side = Sides.Buy, OrderType = OrderTypes.Limit, OrderState = OrderStates.Active, TradeId = 7003, TradePrice = 99.5m, TradeVolume = 5 },
		];

		var (registry, importer) = CreateImporter(DataType.Transactions, fields);

		using (var stream = Helper.MemorySystem.OpenRead(path))
		{
			var (count, lastTime) = await importer.Import(stream, _ => { }, CancellationToken);

			count.AssertEqual(expected.Length);
			lastTime.AssertNotNull();
			lastTime.Value.AssertEqual(_importDay.AddSeconds(2));
		}

		var (dates, loaded) = await ReadBackAsync<ExecutionMessage>(registry, secId, DataType.Transactions);

		dates.Length.AssertEqual(1);
		dates[0].AssertEqual(_importDay.Date);

		loaded.Length.AssertEqual(expected.Length);

		for (var i = 0; i < expected.Length; i++)
		{
			var e = expected[i];
			var a = loaded[i];

			a.SecurityId.AssertEqual(secId);
			a.ServerTime.AssertEqual(e.ServerTime);

			a.PortfolioName.AssertEqual(e.PortfolioName);
			a.TransactionId.AssertEqual(e.TransactionId);
			a.OrderId.AssertEqual(e.OrderId);
			a.OrderPrice.AssertEqual(e.OrderPrice);
			a.OrderVolume.AssertEqual(e.OrderVolume);
			a.Balance.AssertEqual(e.Balance);
			a.Side.AssertEqual(e.Side);
			a.OrderType.AssertEqual(e.OrderType);
			a.OrderState.AssertEqual(e.OrderState);
			a.TradeId.AssertEqual(e.TradeId);
			a.TradePrice.AssertEqual(e.TradePrice);
			a.TradeVolume.AssertEqual(e.TradeVolume);

			// The order columns were filled in, so the row must not come back as a trade-only record.
			a.HasOrderInfo.AssertTrue();
		}
	}

	[TestMethod]
	[Timeout(60_000, CooperativeCancellation = true)]
	[DataRow(1000)]
	[DataRow(1001)]
	[DataRow(1002)]
	public async Task CsvImporter_ImportsEveryRowAcrossTheFlushBoundary(int count)
	{
		// The importer holds rows in a buffer and flushes it once it holds more than 1000, so these three
		// sizes are the three shapes of that boundary: nothing flushed until the file ends, one flush that
		// empties the buffer exactly, and a flush that leaves a remainder for the final one. Each size must
		// reach the storage whole - the same rows, in the same order, none dropped and none written twice.
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var ticks = security.RandomTicks(count, false, TimeSpan.FromSeconds(1), _importDay);
		var path = await ExportTicksAsync($"ticks_flush_{count}_import.csv", ticks);

		var (registry, importer) = CreateImporter(DataType.Ticks, TickFields(false));

		using (var stream = Helper.MemorySystem.OpenRead(path))
		{
			var (imported, _) = await importer.Import(stream, _ => { }, CancellationToken);

			imported.AssertEqual(count);
		}

		var (dates, loaded) = await ReadBackAsync<ExecutionMessage>(registry, secId, DataType.Ticks);

		dates.Length.AssertEqual(1);
		dates[0].AssertEqual(_importDay.Date);

		loaded.Length.AssertEqual(count);

		for (var i = 0; i < count; i++)
		{
			loaded[i].SecurityId.AssertEqual(secId);
			loaded[i].ServerTime.AssertEqual(ticks[i].ServerTime.Truncate(_1mcs));
			loaded[i].TradeId.AssertEqual(ticks[i].TradeId);
			loaded[i].TradePrice.AssertEqual(ticks[i].TradePrice);
			loaded[i].TradeVolume.AssertEqual(ticks[i].TradeVolume);
		}
	}

	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task CsvImporter_ErrorMidFileLeavesAnExactPrefixInStorage()
	{
		// A row the parser cannot read stops the import, and what is left behind was never checked. Whatever
		// the policy turns out to be, a caller is entitled to a boundary: what reached the storage has to be a
		// prefix of the file - the first N rows, in file order, each exactly as written, nothing from at or
		// after the failing row and nothing written twice. How far that boundary should reach - to the last
		// flushed batch as it does today, or to nothing at all - is still to be decided, so it is not asserted.
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var ticks = security.RandomTicks(2000, false, TimeSpan.FromSeconds(1), _importDay);

		var sourcePath = await ExportTicksAsync("ticks_error_prefix_source.csv", ticks);

		const int badRow = 1500;

		var path = RewriteWithBrokenRow(sourcePath, "ticks_error_prefix_import.csv", badRow);

		var (registry, importer) = CreateImporter(DataType.Ticks, TickFields(false));

		using (var stream = Helper.MemorySystem.OpenRead(path))
			await ThrowsExactlyAsync<InvalidOperationException>(() => importer.Import(stream, _ => { }, CancellationToken).AsTask());

		var (_, loaded) = await ReadBackAsync<ExecutionMessage>(registry, secId, DataType.Ticks);

		loaded.Length.AssertLess(badRow, "rows at or after the row that failed to parse reached the storage");

		for (var i = 0; i < loaded.Length; i++)
		{
			loaded[i].SecurityId.AssertEqual(secId);
			loaded[i].ServerTime.AssertEqual(ticks[i].ServerTime.Truncate(_1mcs));
			loaded[i].TradeId.AssertEqual(ticks[i].TradeId);
			loaded[i].TradePrice.AssertEqual(ticks[i].TradePrice);
			loaded[i].TradeVolume.AssertEqual(ticks[i].TradeVolume);
		}
	}

	[TestMethod]
	[Timeout(120_000, CooperativeCancellation = true)]
	public async Task CsvImporter_CancelMidFileLeavesAnExactPrefixInStorage()
	{
		// Cancelling raises the same question as a failing row, and CsvImporter_StopsImport only checks that
		// the exception comes out. The same boundary is owed here: what was already saved stays saved as an
		// exact prefix of the file, and cancelling really does stop the file short rather than quietly
		// finishing it. Where exactly the import gives up is timing-dependent and deliberately not pinned.
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();

		var ticks = security.RandomTicks(5000, false, TimeSpan.FromSeconds(1), _importDay);
		var path = await ExportTicksAsync("ticks_cancel_prefix_import.csv", ticks);

		var (registry, importer) = CreateImporter(DataType.Ticks, TickFields(false));

		using var cts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken);

		using (var stream = Helper.MemorySystem.OpenRead(path))
		{
			await ThrowsExactlyAsync<OperationCanceledException>(() => importer.Import(stream, p =>
			{
				if (p >= 50)
					cts.Cancel();
			}, cts.Token).AsTask());
		}

		var (_, loaded) = await ReadBackAsync<ExecutionMessage>(registry, secId, DataType.Ticks);

		loaded.Length.AssertLess(ticks.Length, "the cancelled import stored the whole file anyway");

		for (var i = 0; i < loaded.Length; i++)
		{
			loaded[i].SecurityId.AssertEqual(secId);
			loaded[i].ServerTime.AssertEqual(ticks[i].ServerTime.Truncate(_1mcs));
			loaded[i].TradeId.AssertEqual(ticks[i].TradeId);
			loaded[i].TradePrice.AssertEqual(ticks[i].TradePrice);
			loaded[i].TradeVolume.AssertEqual(ticks[i].TradeVolume);
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Depths_SameTimeSnapshotsKeepEveryQuote()
	{
		// The depth format carries one row per quote and tells one book from the next by a change of timestamp
		// or security, so two consecutive snapshots of one security stamped with the same time have no boundary
		// between them in the file at all. What is beyond doubt is that no quote may be lost, invented, moved
		// to the other side or handed another row's volume, and that is what is asserted, counting the quotes
		// across everything the parser produced. Whether such rows should merge - as they do today, into a book
		// whose bids are then no longer descending - or be rejected, or be separated by an explicit boundary
		// column, is a format decision still to be taken and is deliberately left unasserted.
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

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var path = WriteFixedCsv("depths_same_time_import.csv",
			"SECA;ALL;20240305;10:00:00.000000;100;1;Buy",
			"SECA;ALL;20240305;10:00:00.000000;105;2;Sell",
			"SECA;ALL;20240305;10:00:00.000000;102;3;Buy",
			"SECA;ALL;20240305;10:00:00.000000;107;4;Sell");

		QuoteChangeMessage[] msgs;

		using (var stream = Helper.MemorySystem.OpenRead(path))
		{
			var parser = new CsvParser(DataType.MarketDepth, fields)
			{
				ColumnSeparator = ";"
			};

			msgs = [.. (await parser.Parse(stream).ToArrayAsync(CancellationToken)).Cast<QuoteChangeMessage>()];
		}

		foreach (var msg in msgs)
		{
			msg.SecurityId.SecurityCode.AssertEqual("SECA");
			msg.SecurityId.BoardCode.AssertEqual("ALL");
			msg.ServerTime.AssertEqual(_importDay);
		}

		var quotes = msgs
			.SelectMany(m => m.Bids.Select(q => $"Buy:{q.Price}:{q.Volume}").Concat(m.Asks.Select(q => $"Sell:{q.Price}:{q.Volume}")))
			.OrderBy(s => s, StringComparer.Ordinal)
			.JoinComma();

		quotes.AssertEqual("Buy:100:1,Buy:102:3,Sell:105:2,Sell:107:4");
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Depths_AlternatingSecuritiesAreNotMixed()
	{
		// Rows join one book while the timestamp and the security both stay the same, so a file that
		// alternates two securities at a single timestamp must come out as three books, each holding only its
		// own security's quote. A book that swallowed the other security's price would be a corrupt order
		// book, and a check on the number of books alone would not see it.
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

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var path = WriteFixedCsv("depths_alternating_import.csv",
			"SECA;ALL;20240305;10:00:00.000000;100;1;Buy",
			"SECB;ALL;20240305;10:00:00.000000;200;2;Buy",
			"SECA;ALL;20240305;10:00:00.000000;99;3;Buy");

		QuoteChangeMessage[] msgs;

		using (var stream = Helper.MemorySystem.OpenRead(path))
		{
			var parser = new CsvParser(DataType.MarketDepth, fields)
			{
				ColumnSeparator = ";"
			};

			msgs = [.. (await parser.Parse(stream).ToArrayAsync(CancellationToken)).Cast<QuoteChangeMessage>()];
		}

		msgs.Length.AssertEqual(3);

		string[] expectedCodes = ["SECA", "SECB", "SECA"];
		decimal[] expectedPrices = [100m, 200m, 99m];
		decimal[] expectedVolumes = [1m, 2m, 3m];

		for (var i = 0; i < msgs.Length; i++)
		{
			msgs[i].SecurityId.SecurityCode.AssertEqual(expectedCodes[i]);
			msgs[i].ServerTime.AssertEqual(_importDay);

			msgs[i].Asks.Length.AssertEqual(0);
			msgs[i].Bids.Length.AssertEqual(1);

			msgs[i].Bids[0].Price.AssertEqual(expectedPrices[i]);
			msgs[i].Bids[0].Volume.AssertEqual(expectedVolumes[i]);
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task CsvParser_IsReusableAfterAParseError()
	{
		// A parser builds a date and a time parser out of the field formats and drops them at the start of
		// every Parse. A caller is entitled to reuse the instance: a file that failed halfway through a time
		// cell must leave nothing behind, and a format changed between the two runs must be the one the second
		// file is read with. Reading the second file with the first run's cached time parser would either
		// throw or land on a different second, so the times are what is asserted, not merely that rows came out.
		var fields = TickFields(false);
		var timeField = fields.First(f => f.Name == "ServerTime.TimeOfDay");

		var parser = new CsvParser(DataType.Ticks, fields)
		{
			ColumnSeparator = ";"
		};

		var broken = WriteFixedCsv("ticks_reuse_broken_import.csv",
			"AAPL;NASDAQ;20240305;10:00:00.000000;101;1;1",
			// The time cell stops short of the fractional part the "hh:mm:ss.ffffff" format asks for.
			"AAPL;NASDAQ;20240305;10:00;102;2;2");

		using (var stream = Helper.MemorySystem.OpenRead(broken))
			await ThrowsExactlyAsync<InvalidOperationException>(() => parser.Parse(stream).ToArrayAsync(CancellationToken).AsTask());

		timeField.Format = "hh:mm:ss";

		var second = WriteFixedCsv("ticks_reuse_second_import.csv",
			"AAPL;NASDAQ;20240305;10:00:01;201;3;3",
			"AAPL;NASDAQ;20240305;10:00:02;202;4;4");

		ExecutionMessage[] msgs;

		using (var stream = Helper.MemorySystem.OpenRead(second))
			msgs = [.. (await parser.Parse(stream).ToArrayAsync(CancellationToken)).Cast<ExecutionMessage>()];

		msgs.Length.AssertEqual(2);

		msgs[0].ServerTime.AssertEqual(_importDay.AddSeconds(1));
		msgs[1].ServerTime.AssertEqual(_importDay.AddSeconds(2));

		msgs[0].TradeId.AssertEqual((long?)201);
		msgs[1].TradeId.AssertEqual((long?)202);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task News_QuotedCellsKeepSeparatorsAndNewlines()
	{
		// Free text in a cell may contain the column separator, a quote or a line break, and the format's
		// answer to all three is quoting. A caller who quotes a cell is entitled to the exact text back: a
		// headline must not be cut short at its semicolon, split into two rows at its line break, or keep the
		// doubled quotes that only escape the real ones.
		var allFields = FieldMappingRegistry.CreateFields(DataType.News).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Headline"),
			allFields.First(f => f.Name == "Source"),
			allFields.First(f => f.Name == "Url"),
		};

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var path = WriteFixedCsv("news_quoted_import.csv",
			"20240305;10:00:00;\"Rates up; bonds down\";Reuters;http://a/b",
			"20240305;10:00:01;\"line one" + StringHelper.RN + "line two\";Bloomberg;http://c/d",
			"20240305;10:00:02;\"He said \"\"hi\"\"\";TASS;http://e/f");

		NewsMessage[] msgs;

		using (var stream = Helper.MemorySystem.OpenRead(path))
		{
			var parser = new CsvParser(DataType.News, fields)
			{
				ColumnSeparator = ";"
			};

			msgs = [.. (await parser.Parse(stream).ToArrayAsync(CancellationToken)).Cast<NewsMessage>()];
		}

		msgs.Length.AssertEqual(3);

		msgs[0].Headline.AssertEqual("Rates up; bonds down");
		msgs[1].Headline.AssertEqual("line one" + StringHelper.RN + "line two");
		msgs[2].Headline.AssertEqual("He said \"hi\"");

		msgs[0].Source.AssertEqual("Reuters");
		msgs[1].Source.AssertEqual("Bloomberg");
		msgs[2].Source.AssertEqual("TASS");

		msgs[0].Url.AssertEqual("http://a/b");
		msgs[1].Url.AssertEqual("http://c/d");
		msgs[2].Url.AssertEqual("http://e/f");

		msgs[0].ServerTime.AssertEqual(_importDay);
		msgs[1].ServerTime.AssertEqual(_importDay.AddSeconds(1));
		msgs[2].ServerTime.AssertEqual(_importDay.AddSeconds(2));
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Ticks_ValueMappingsAndDefaultAreApplied()
	{
		// A column may carry the file's own vocabulary rather than ours, and a cell may be empty. The mapping
		// answers both: a listed file value is translated, an empty cell falls back to the default, and a cell
		// that is filled in wins over that default. Only the resulting side shows whether the translation ran
		// at all - a message with the wrong side is still a perfectly well-formed message.
		var fields = TickFields(true);
		var sideField = fields.First(f => f.Name == "OriginSide");

		sideField.Values =
		[
			new() { ValueFile = "B", ValueStockSharp = "Buy" },
			new() { ValueFile = "S", ValueStockSharp = "Sell" },
		];

		sideField.DefaultValue = "Sell";

		var path = WriteFixedCsv("ticks_mapping_import.csv",
			"AAPL;NASDAQ;20240305;10:00:00.000000;101;1;1;B",
			"AAPL;NASDAQ;20240305;10:00:01.000000;102;2;2;S",
			"AAPL;NASDAQ;20240305;10:00:02.000000;103;3;3;");

		ExecutionMessage[] msgs;

		using (var stream = Helper.MemorySystem.OpenRead(path))
		{
			var parser = new CsvParser(DataType.Ticks, fields)
			{
				ColumnSeparator = ";"
			};

			msgs = [.. (await parser.Parse(stream).ToArrayAsync(CancellationToken)).Cast<ExecutionMessage>()];
		}

		msgs.Length.AssertEqual(3);

		Sides?[] expectedSides = [Sides.Buy, Sides.Sell, Sides.Sell];
		long?[] expectedIds = [101, 102, 103];

		for (var i = 0; i < msgs.Length; i++)
		{
			msgs[i].OriginSide.AssertEqual(expectedSides[i]);
			msgs[i].TradeId.AssertEqual(expectedIds[i]);
			msgs[i].ServerTime.AssertEqual(_importDay.AddSeconds(i));
		}
	}
}
