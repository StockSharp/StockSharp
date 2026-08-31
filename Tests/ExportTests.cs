namespace StockSharp.Tests;

using Ecng.Data;
using Ecng.Security;

using StockSharp.Algo.Export;

[TestClass]
public class ExportTests : BaseTestClass
{
	private static readonly TemplateTxtRegistry _txtReg = new();

	// The DB leg covers table creation and CLR->SQL type mapping, not volume: every row
	// of a given message type travels through the same mapping code, so only a bounded
	// slice is sent to the (remote) server instead of the whole generated set.
	private const int _dbMaxRows = 150;

	private const string _dbConnStrSecret = "SQLSERVER_CONNECTION_STRING";

	// Several environments run this suite against one server at the same time, so a table named after
	// the test method alone is the same table in all of them - one run drops it while another is still
	// inserting into it. The marker keeps them apart. It is derived rather than random so that a rerun
	// of the same environment lands on the same tables and DropExisting recycles them, instead of
	// leaving a fresh set behind on every run: nothing here can enumerate tables to clean up after.
	private static readonly string _envMarker = (Environment.MachineName + AppContext.BaseDirectory)
		.UTF8().Sha256()[..8].ToLowerInvariant();

	// Resolved once: the test methods run in parallel and the secrets file cache behind the lookup is built
	// lazily without synchronization. A missing secret fails the test instead of passing it over, so a run
	// without a database is reported as a run that did not cover the database.
	private static readonly Lazy<string> _dbConnStr = new(() =>
		TryGetSecret(_dbConnStrSecret)
			?? throw new InvalidOperationException(
				$"Secret '{_dbConnStrSecret}' missing. Set the environment variable or add it to {SecretsFile}."),
		true);

	private async Task ExportAsync<TValue>(DataType dataType, IEnumerable<TValue> values, string txtTemplate)
		where TValue : class
	{
		var token = CancellationToken;
		var arr = values.ToArray();
		var hasTime = typeof(TValue).Is<IServerTimeMessage>();

		void validateResult(TValue[] source, int count, DateTime? lastTime, string name)
		{
			var expectedCount = typeof(TValue) == typeof(QuoteChangeMessage) &&
				name is not ("xml" or "json")
					? source.Cast<QuoteChangeMessage>().Sum(depth => depth.ToTimeQuotes().Count())
					: source.Length;

			count.AreEqual(expectedCount, $"ExportAsync returned unexpected count for {name}");

			if (hasTime && source.Length > 0)
				lastTime.AssertEqual(((IServerTimeMessage)source.Last()).ServerTime);
		}

		async Task Do(string extension, Func<Stream, BaseExporter> create)
		{
			using var stream = new MemoryStream();
			var export = create(stream);
			var (count, lastTime) = await export.Export(arr.ToAsyncEnumerable(), token);

			validateResult(arr, count, lastTime, extension);

			// Verify something was written
			(stream.Length > 0).AssertTrue($"ExportAsync {extension} should write data");
		}

		await Do("txt", f => new TextExporter(dataType, f, txtTemplate, null));
		await Do("xml", f => new XmlExporter(dataType, f));
		await Do("json", f => new JsonExporter(dataType, f));
		await Do("xlsx", f => new ExcelExporter(ServicesRegistry.ExcelProvider, dataType, f, () => { }));

#if NET10_0_OR_GREATER
		// A depth is written as one row per quote, every other message as a single row.
		static int toDbRows(TValue value)
			=> value is QuoteChangeMessage depth ? depth.ToTimeQuotes().Count() : 1;

		var dbValues = new List<TValue>();
		var dbRows = 0;

		foreach (var value in arr)
		{
			if (dbRows >= _dbMaxRows)
				break;

			dbValues.Add(value);
			dbRows += toDbRows(value);
		}

		// The slice must still carry rows, so an empty stream cannot pass the DB leg silently.
		(dbRows > 0).AssertTrue($"DB export slice is empty for {typeof(TValue).Name}");

		var dbArr = dbValues.ToArray();

		var dbExporter = new DatabaseExporter(DatabaseRegistry.Provider, dataType, new DatabaseConnectionPair
		{
			Provider = DatabaseProviderRegistry.AllProviders.First(),
			ConnectionString = _dbConnStr.Value,
		})
		{
			DropExisting = true,
			// Tests share target tables (Ticks and OrderLog both export ExecutionMessage)
			// and each of them drops and recreates its table, so the name has to be unique
			// per test for the class to run in parallel, and per environment for the runs
			// that share the server not to drop each other's tables.
			TableNamePrefix = $"SS_{TestContext.TestName}_{_envMarker}_",
			// Below the slice size on purpose: keeps the multi-batch path of the exporter
			// covered, while the production default would send the slice as one batch.
			BatchSize = 100,
		};
		var (dbCount, dbLastTime) = await dbExporter.Export(dbArr.ToAsyncEnumerable(), token);
		validateResult(dbArr, dbCount, dbLastTime, "DB");
#endif
	}

	[TestMethod]
	public async Task Cancellation()
	{
		var security = Helper.CreateStorageSecurity();
		var ticks = security.RandomTicks(1000, true).ToArray();

		using var stream = new MemoryStream();
		var exporter = new TextExporter(DataType.Ticks, stream, _txtReg.TemplateTxtTick, null);
		using var cts = new CancellationTokenSource();

		async IAsyncEnumerable<ExecutionMessage> Enumerate()
		{
			for (var i = 0; i < ticks.Length; i++)
			{
				if (i == 100)
					cts.Cancel();

				yield return ticks[i];
				await Task.Yield();
			}
		}

		await ThrowsAsync<OperationCanceledException>(() => exporter.Export(Enumerate(), cts.Token));

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
