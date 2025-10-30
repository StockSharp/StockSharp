namespace StockSharp.Tests;

using StockSharp.Algo.Export;
using StockSharp.Algo.Import;

using Ecng.Linq;

[TestClass]
public class ImportTests : BaseTestClass
{
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

	private async Task Import<TValue>(DataType dataType, bool addSecId, IEnumerable<TValue> values, FieldMapping[] fields)
		where TValue : class
	{
		var arr = values.ToArray();

		var template = GetTemplate(dataType);

		if (addSecId)
			template = "{SecurityId.SecurityCode};{SecurityId.BoardCode};" + template;

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var token = CancellationToken;

		var filePath = Helper.GetSubTemp($"{dataType.DataTypeToFileName()}_import.csv");

		using (var stream = File.Create(filePath))
		{
			await new TextExporter(dataType, stream, template, null).Export(arr, token);
		}

		// Parser check
		using (var stream = File.OpenRead(filePath))
		{
			var parser = new CsvParser(dataType, fields)
			{
				ColumnSeparator = ";"
			};

			var msgs = await parser.Parse(stream, token).ToArrayAsync2(token);

			msgs.Length.AssertEqual(arr.Length);
		}

		var storageRegistry = Helper.GetStorage(Helper.GetSubTemp());

		using (var stream = File.OpenRead(filePath))
		{
			var importer = new CsvImporter(dataType, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => storageRegistry.GetStorage(secId, dataType))
			{
				ColumnSeparator = ";"
			};

			var (count, lastTime) = await importer.Import(stream, _ => { }, token);

			count.AssertEqual(arr.Length);
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
		return Import(DataType.Ticks, true, security.RandomTicks(100, true), fields);
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
		return Import(DataType.MarketDepth, true, security.RandomDepths(100, ordersCount: true), fields);
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
		return Import(DataType.OrderLog, true, security.RandomOrderLog(100), fields);
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
		return Import(DataType.PositionChanges, false, security.RandomPositionChanges(100), fields);
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
		return Import(DataType.News, false, Helper.RandomNews(), fields);
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
		return Import(DataType.Level1, true, security.RandomLevel1(count: 100), fields);
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
			await Import(dataType, true, group.ToArray(), fields);
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
		return Import(DataType.BoardState, false, Helper.RandomBoardStates(), fields);
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
		return Import(DataType.Transactions, true, security.RandomTransactions(10), fields);
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
		return Import(DataType.Securities, false, Helper.RandomSecurities(10), fields);
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
		return Import(DataType.Board, false, Helper.RandomBoards(10), fields);
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

		return Import(DataType.MarketDepth, true, onlyBids, fields);
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

		return Import(DataType.MarketDepth, true, onlyAsks, fields);
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

		return Import(DataType.MarketDepth, true, empty, fields);
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

		return Import(DataType.MarketDepth, true, mixed.ToArray(), fields);
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

		using var stream = File.Create(Helper.GetSubTemp($"ticks_progress_import.csv"));
		await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr, CancellationToken);
		stream.Flush();
		stream.Position = 0;

		var storage = Helper.GetStorage(Helper.GetSubTemp());

		var importer = new CsvImporter(DataType.Ticks, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => storage.GetTickMessageStorage(secId))
		{
			ColumnSeparator = ";"
		};

		var progresses = new List<int>();

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
		lastTime.Value.AssertEqual(arr.Last().ServerTime.Truncate(TimeSpan.FromSeconds(1)));
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

		using var stream = File.Create(Helper.GetSubTemp($"ticks_cancel_import.csv"));
		await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr, CancellationToken);
		stream.Flush();
		stream.Position = 0;

		var storage = Helper.GetStorage(Helper.GetSubTemp());

		var importer = new CsvImporter(DataType.Ticks, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => storage.GetTickMessageStorage(secId))
		{
			ColumnSeparator = ";"
		};

		var progresses = new List<int>();
		using var cts = new CancellationTokenSource();

		await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => importer.Import(stream, p =>
		{
			if (progresses.Count > 0 && progresses.Last() >= p)
				throw new DuplicateException($"Progress {p} already exist.");

			progresses.Add(p);

			if (p >= 40)
				cts.Cancel();
		}, cts.Token).AsTask());

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

		using var stream = File.Create(Helper.GetSubTemp($"ticks_error_import.csv"));
		await new TextExporter(DataType.Ticks, stream, _tickFullTemplate, null).Export(arr, CancellationToken);
		stream.Flush();
		stream.Position = 0;

		// Make one of the field orders invalid (beyond column count) to provoke parsing error
		fields[0].Order = 9999;

		var storage = Helper.GetStorage(Helper.GetSubTemp());

		var importer = new CsvImporter(DataType.Ticks, fields, ServicesRegistry.SecurityStorage, ServicesRegistry.ExchangeInfoProvider, secId => storage.GetTickMessageStorage(secId))
		{
			ColumnSeparator = ";"
		};

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => importer.Import(stream, _ => { }, CancellationToken).AsTask());
	}
}