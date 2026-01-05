namespace StockSharp.Tests;

using Ecng.Data;

using StockSharp.Algo.Export;

[TestClass]
public class ExportTests : BaseTestClass
{
	private static readonly TemplateTxtRegistry _txtReg = new();

#if NET10_0_OR_GREATER
	[ClassInitialize]
	public static void ClassInit(TestContext context)
	{
		// Drop test tables to ensure fresh schema (handles column additions like MarginMode)
		var connStr = GetSecret("DB_CONNECTION_STRING");
		if (connStr.IsEmpty())
			return;

		var tables = new[]
		{
			"Execution", "TimeQuoteChange", "Level1", "Candle", "News",
			"Security", "Position", "IndicatorValue", "BoardState", "Board"
		};

		using var conn = new Microsoft.Data.SqlClient.SqlConnection(connStr);
		conn.Open();

		foreach (var table in tables)
		{
			using var cmd = conn.CreateCommand();
			cmd.CommandText = $"IF OBJECT_ID('{table}', 'U') IS NOT NULL DROP TABLE [{table}]";
			cmd.ExecuteNonQuery();
		}
	}
#endif

	private async Task ExportAsync<TValue>(DataType dataType, IEnumerable<TValue> values, string txtTemplate)
		where TValue : class
	{
		var token = CancellationToken;
		var arr = values.ToArray();
		var ignoreCount = typeof(TValue) == typeof(QuoteChangeMessage);
		var hasTime = typeof(TValue).Is<IServerTimeMessage>();

		void validateResult(int count, DateTime? lastTime, string name)
		{
			// Verify returned values: count equals number of elements; lastTime should be non-null for non-empty arrays
			if (!ignoreCount)
				count.AreEqual(arr.Length, $"ExportAsync returned unexpected count for {name}");

			if (hasTime && arr.Length > 0)
				lastTime.AssertEqual(((IServerTimeMessage)arr.Last()).ServerTime);
		}

		async Task Do(string extension, Func<Stream, BaseExporter> create)
		{
			using var stream = new MemoryStream();
			var export = create(stream);
			var (count, lastTime) = await export.Export(arr.ToAsyncEnumerable(), token);

			validateResult(count, lastTime, extension);

			// Verify something was written
			(stream.Length > 0).AssertTrue($"ExportAsync {extension} should write data");
		}

		await Do("txt", f => new TextExporter(dataType, f, txtTemplate, null));
		await Do("xml", f => new XmlExporter(dataType, f));
		await Do("json", f => new JsonExporter(dataType, f));
		await Do("xlsx", f => new ExcelExporter(ServicesRegistry.ExcelProvider, dataType, f, () => { }));

#if NET10_0_OR_GREATER
		var dbExporter = new DatabaseExporter(DatabaseRegistry.Provider, dataType, new DatabaseConnectionPair
		{
			Provider = DatabaseProviderRegistry.AllProviders.First(),
			ConnectionString = GetSecret("DB_CONNECTION_STRING"),
		});
		var (dbCount, dbLastTime) = await dbExporter.Export(arr.ToAsyncEnumerable(), token);
		validateResult(dbCount, dbLastTime, "DB");
#endif
	}

	[TestMethod]
	public async Task Cancellation()
	{
		var security = Helper.CreateStorageSecurity();
		var ticks = security.RandomTicks(20000, true).ToArray();

		using var stream = new MemoryStream();
		var exporter = new TextExporter(DataType.Ticks, stream, _txtReg.TemplateTxtTick, null);

		var (_, token) = CancellationToken.CreateChildToken(TimeSpan.FromSeconds(1));

		await ThrowsAsync<OperationCanceledException>(() => exporter.Export(ticks.ToAsyncEnumerable(), token));

		// partial data should be written
		(stream.Length > 0).AssertTrue();
	}

	[TestMethod]
	public Task Ticks()
	{
		var security = Helper.CreateStorageSecurity();
		var ticks = security.RandomTicks(1000, true);

		return ExportAsync(DataType.Ticks, ticks, _txtReg.TemplateTxtTick);
	}

	[TestMethod]
	public Task Depths()
	{
		var security = Helper.CreateStorageSecurity();
		var depths = security.RandomDepths(100, ordersCount: true);

		return ExportAsync(DataType.MarketDepth, depths, _txtReg.TemplateTxtDepth);
	}

	[TestMethod]
	public Task OrderLog()
	{
		var security = Helper.CreateStorageSecurity();
		var ol = security.RandomOrderLog(1000);

		return ExportAsync(DataType.OrderLog, ol, _txtReg.TemplateTxtOrderLog);
	}

	[TestMethod]
	public Task Positions()
	{
		var security = Helper.CreateStorageSecurity();
		var pos = security.RandomPositionChanges(1000);

		return ExportAsync(DataType.PositionChanges, pos, _txtReg.TemplateTxtPositionChange);
	}

	[TestMethod]
	public Task News()
	{
		var news = Helper.RandomNews();

		return ExportAsync(DataType.News, news, _txtReg.TemplateTxtNews);
	}

	[TestMethod]
	public Task Level1()
	{
		var security = Helper.CreateStorageSecurity();
		var level1 = security.RandomLevel1(count: 1000);

		return ExportAsync(DataType.Level1, level1, _txtReg.TemplateTxtLevel1);
	}

	[TestMethod]
	public async Task Candles()
	{
		var security = Helper.CreateStorageSecurity();

		var candles = CandleTests.GenerateCandles(security.RandomTicks(1000, true), security, CandleTests.PriceRange.Pips(security), CandleTests.TotalTicks, CandleTests.TimeFrame, CandleTests.VolumeRange, CandleTests.BoxSize, CandleTests.PnF(security), true);

		foreach (var group in candles.GroupBy(c => (type: c.GetType(), arg: c.Arg)))
		{
			var type = group.Key.type;
			var arg = group.Key.arg;
			await ExportAsync(DataType.Create(type, arg), group.ToArray(), _txtReg.TemplateTxtCandle);
		}
	}

	[TestMethod]
	public Task Indicator()
	{
		var security = Helper.CreateStorageSecurity();
		var secId = security.ToSecurityId();
		var sma = new SimpleMovingAverage();

		var values = new List<IndicatorValue>();

		var ticks = security.RandomTicks(1000, true);

		foreach (var tick in ticks)
		{
			values.Add(new IndicatorValue
			{
				SecurityId = secId,
				Time = tick.ServerTime,
				Value = sma.Process(new TickIndicatorValue(sma, tick) { IsFinal = true }),
			});
		}

		return ExportAsync(TraderHelper.IndicatorValue, values, _txtReg.TemplateTxtIndicator);
	}

	[TestMethod]
	public Task Board()
	{
		var boards = Helper.RandomBoards(100);
		return ExportAsync(DataType.Board, boards, _txtReg.TemplateTxtBoard);
	}

	[TestMethod]
	public Task BoardState()
	{
		var boardStates = Helper.RandomBoardStates();
		return ExportAsync(DataType.BoardState, boardStates, _txtReg.TemplateTxtBoardState);
	}

	[TestMethod]
	public Task Security()
	{
		var securities = Helper.RandomSecurities(100);
		return ExportAsync(DataType.Securities, securities, _txtReg.TemplateTxtSecurity);
	}
}