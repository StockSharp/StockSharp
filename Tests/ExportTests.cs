namespace StockSharp.Tests;

using System.Xml.Linq;

using Ecng.Data;
using Ecng.Security;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using StockSharp.Algo.Export;
using StockSharp.Localization;

[TestClass]
public class ExportTests : BaseTestClass
{
	private static readonly TemplateTxtRegistry _txtReg = new();

	// A known day for the content tests below: rows can only be checked against a date when the
	// generated data lands on one instead of on whatever day the run happens to start.
	private static readonly DateTime _contentDay = new(2024, 3, 5, 10, 0, 0, DateTimeKind.Utc);

	// The xml leg writes times with millisecond precision, so its times are pinned at that precision.
	private static readonly TimeSpan _1ms = TimeSpan.FromMilliseconds(1);

	// The writers emit the UTF8 preamble, which would otherwise ride along in the first parsed value.
	private const char _bom = (char)0xFEFF;

	// The fields the default Level1 export template emits, in the order it emits them.
	private static readonly Level1Fields[] _level1Template =
	[
		Level1Fields.BestBidPrice,
		Level1Fields.BestBidVolume,
		Level1Fields.BestAskPrice,
		Level1Fields.BestAskVolume,
		Level1Fields.LastTradePrice,
		Level1Fields.LastTradeVolume,
	];

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
	// lazily without synchronization.
	private static readonly Lazy<string> _dbConnStrSecretValue = new(() => TryGetSecret(_dbConnStrSecret), true);

	// The database leg exports to a real server named by a secret. A machine that was never given one
	// can say nothing about whether the export is correct, so a run without it is reported as a run that
	// did not cover the database - inconclusive and named - rather than as a failure, which would read
	// like a fault in the exporter, or as a pass, which would claim coverage that never happened.
	private static string DbConnStr
	{
		get
		{
			var connStr = _dbConnStrSecretValue.Value;

			if (connStr.IsEmpty())
				Inconclusive($"Secret '{_dbConnStrSecret}' missing, so this run has no database server to export to. Set the environment variable or add it to {SecretsFile}.");

			return connStr;
		}
	}

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
			ConnectionString = DbConnStr,
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

	// Export runs under the invariant culture (BaseExporter.Export), so an expected cell has to be
	// rendered the same way rather than with the culture the test process happens to run in. An
	// absent value is an empty cell, never a substituted zero.
	private static string Cell(object value)
		=> value switch
		{
			null => string.Empty,
			IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString(),
		};

	private static string Cell(DateTime value, string format)
		=> value.ToString(format, CultureInfo.InvariantCulture);

	// An attribute the exporter skips (null value) and a value it writes as empty must compare alike.
	private static string Attr(XElement element, string name)
		=> element.Attribute(name)?.Value ?? string.Empty;

	private async Task<string[][]> ExportTextRowsAsync<TValue>(DataType dataType, TValue[] values, string template)
		where TValue : class
	{
		using var stream = new MemoryStream();

		var (count, _) = await new TextExporter(dataType, stream, template, null).Export(values.ToAsyncEnumerable(), CancellationToken);

		// The writer emits the UTF8 preamble, which would otherwise ride along in the first cell.
		var rows = stream.ToArray().UTF8().TrimStart('\uFEFF').SplitByLineSeps().Select(line => line.Split(';')).ToArray();

		// The reported count must be the number of rows actually written, not a tally kept beside them.
		rows.Length.AssertEqual(count);

		return rows;
	}

	private async Task<XElement> ExportXmlAsync<TValue>(DataType dataType, TValue[] values)
		where TValue : class
	{
		using var stream = new MemoryStream();

		await new XmlExporter(dataType, stream).Export(values.ToAsyncEnumerable(), CancellationToken);

		stream.Position = 0;
		return XElement.Load(stream);
	}

	private async Task<JArray> ExportJsonAsync<TValue>(DataType dataType, TValue[] values)
		where TValue : class
	{
		using var stream = new MemoryStream();

		var (count, _) = await new JsonExporter(dataType, stream).Export(values.ToAsyncEnumerable(), CancellationToken);

		// The writer emits the UTF8 preamble. FloatParseHandling.Decimal is not a detail: the default
		// reads every fractional number as a double, which would hide a rounded or rescaled decimal
		// behind the reader's own conversion instead of showing what the exporter wrote.
		using var reader = new JsonTextReader(new StringReader(stream.ToArray().UTF8().TrimStart(_bom)))
		{
			FloatParseHandling = FloatParseHandling.Decimal,
		};

		var arr = JArray.Load(reader);

		// The reported count must be the number of objects actually written, not a tally kept beside them.
		arr.Count.AssertEqual(count);

		return arr;
	}

	// A property the exporter skipped and one written as null are the same thing to a reader: no value.
	private static JToken Opt(JToken obj, string name)
		=> obj[name] is JToken token && token.Type != JTokenType.Null ? token : null;

	private static ExecutionMessage Tick(SecurityId secId, DateTime time, decimal price, decimal volume, long tradeId)
		=> new()
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			ServerTime = time,
			TradeId = tradeId,
			TradePrice = price,
			TradeVolume = volume,
		};

	private static string[] DescribeQuotes(QuoteChangeMessage depth)
		=> [.. depth.Bids
			.Select(q => $"{Cell(q.Price)};{Cell(q.Volume)};{Sides.Buy}")
			.Concat(depth.Asks.Select(q => $"{Cell(q.Price)};{Cell(q.Volume)};{Sides.Sell}"))
			.OrderBy(s => s, StringComparer.Ordinal)];

	private static string Level1Cell(Level1ChangeMessage message, Level1Fields field)
		=> message.Changes.TryGetValue(field, out var value) ? Cell(value) : string.Empty;

	[TestMethod]
	public async Task Depths_ExportContent()
	{
		// ExportAsync counts rows and checks the stream is not empty, so a book written with price and
		// volume swapped, or with a bid labelled an ask, passes it. This pins the payload: every quote
		// of every book is written once, under that book's own time, on its own side, with its own
		// price and volume, and the quotes of one book stay consecutive - a run of rows sharing a time
		// is the only thing that lets a reader tell one book from the next.
		var security = Helper.CreateStorageSecurity();
		var depths = security.RandomDepths(20, ordersCount: false);

		for (var i = 0; i < depths.Length; i++)
			depths[i].ServerTime = _contentDay.AddSeconds(i);

		var rows = await ExportTextRowsAsync(DataType.MarketDepth, depths, _txtReg.TemplateTxtDepth);

		rows.Length.AssertEqual(depths.Sum(d => d.Bids.Length + d.Asks.Length));

		var offset = 0;

		foreach (var depth in depths)
		{
			var expected = DescribeQuotes(depth);
			var actual = new List<string>();

			for (var i = 0; i < expected.Length; i++)
			{
				var row = rows[offset + i];

				row.Length.AssertEqual(5);
				row[0].AssertEqual(Cell(depth.ServerTime, "yyyyMMdd"));
				row[1].AssertEqual(Cell(depth.ServerTime, "HH:mm:ss.ffffff"));

				actual.Add($"{row[2]};{row[3]};{row[4]}");
			}

			actual.OrderBy(s => s, StringComparer.Ordinal).JoinN().AssertEqual(expected.JoinN());

			offset += expected.Length;
		}

		var books = (await ExportXmlAsync(DataType.MarketDepth, depths)).Elements("depth").ToArray();

		books.Length.AssertEqual(depths.Length);

		for (var i = 0; i < depths.Length; i++)
		{
			var depth = depths[i];

			DateTime.Parse(Attr(books[i], "serverTime"), CultureInfo.InvariantCulture).AssertEqual(depth.ServerTime.Truncate(_1ms));

			books[i]
				.Elements("quote")
				.Select(q => $"{Attr(q, "price")};{Attr(q, "volume")};{Attr(q, "side")}")
				.OrderBy(s => s, StringComparer.Ordinal)
				.JoinN()
				.AssertEqual(DescribeQuotes(depth).JoinN());
		}
	}

	[TestMethod]
	public async Task XmlDepth_IdenticalBidAndAskKeepTheirSides()
	{
		var quote = new QuoteChange(100m, 5m);
		var depth = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = _contentDay,
			Bids = [quote],
			Asks = [quote],
		};

		var quotes = (await ExportXmlAsync(DataType.MarketDepth, new[] { depth }))
			.Elements("depth")
			.Single()
			.Elements("quote")
			.ToArray();

		quotes.Length.AssertEqual(2);
		quotes.Count(q => Attr(q, "side") == Sides.Buy.ToString()).AssertEqual(1);
		quotes.Count(q => Attr(q, "side") == Sides.Sell.ToString()).AssertEqual(1);
	}

	[TestMethod]
	public async Task OrderLog_ExportContent()
	{
		// Only the row count was checked, so a shifted or swapped column survived. Every column the
		// default order log template emits is pinned here, in both txt and xml.
		var security = Helper.CreateStorageSecurity();
		var ol = security.RandomOrderLog(100, _contentDay);

		var rows = await ExportTextRowsAsync(DataType.OrderLog, ol, _txtReg.TemplateTxtOrderLog);

		rows.Length.AssertEqual(ol.Length);

		for (var i = 0; i < ol.Length; i++)
		{
			var item = ol[i];

			rows[i].JoinComma().AssertEqual(new[]
			{
				Cell(item.ServerTime, "yyyyMMdd"),
				Cell(item.ServerTime, "HH:mm:ss.ffffff"),
				Cell(item.IsSystem),
				Cell(item.OrderId),
				Cell(item.OrderPrice),
				Cell(item.OrderVolume),
				Cell(item.Side),
				Cell(item.OrderState),
				Cell(item.TimeInForce),
				Cell(item.TradeId),
				Cell(item.TradePrice),
			}.JoinComma());
		}

		var items = (await ExportXmlAsync(DataType.OrderLog, ol)).Elements("item").ToArray();

		items.Length.AssertEqual(ol.Length);

		for (var i = 0; i < ol.Length; i++)
		{
			var item = ol[i];
			var el = items[i];

			DateTime.Parse(Attr(el, "serverTime"), CultureInfo.InvariantCulture).AssertEqual(item.ServerTime.Truncate(_1ms));

			Attr(el, "id").AssertEqual(item.OrderId is long orderId ? Cell(orderId) : Cell(item.OrderStringId));
			Attr(el, "price").AssertEqual(Cell(item.OrderPrice));
			Attr(el, "volume").AssertEqual(Cell(item.OrderVolume));
			Attr(el, "side").AssertEqual(Cell(item.Side));
			Attr(el, "state").AssertEqual(Cell(item.OrderState));
			Attr(el, "timeInForce").AssertEqual(Cell(item.TimeInForce));
			Attr(el, "isSystem").AssertEqual(Cell(item.IsSystem));
			Attr(el, "tradePrice").AssertEqual(Cell(item.TradePrice));
		}
	}

	[TestMethod]
	public async Task Level1_ExportContent()
	{
		// A Level1 message carries an arbitrary subset of fields, so the export has two things to get
		// right and neither was checked: each present value lands in its own column, and each absent
		// one leaves that column empty rather than filling it with a zero the source never had.
		var security = Helper.CreateStorageSecurity();
		var level1 = security.RandomLevel1(count: 100);

		for (var i = 0; i < level1.Length; i++)
			level1[i].ServerTime = _contentDay.AddSeconds(i);

		var rows = await ExportTextRowsAsync(DataType.Level1, level1, _txtReg.TemplateTxtLevel1);

		rows.Length.AssertEqual(level1.Length);

		for (var i = 0; i < level1.Length; i++)
		{
			var msg = level1[i];
			var row = rows[i];

			row.Length.AssertEqual(2 + _level1Template.Length);
			row[0].AssertEqual(Cell(msg.ServerTime, "yyyyMMdd"));
			row[1].AssertEqual(Cell(msg.ServerTime, "HH:mm:ss.ffffff"));

			for (var j = 0; j < _level1Template.Length; j++)
				row[2 + j].AssertEqual(Level1Cell(msg, _level1Template[j]));
		}

		var changes = (await ExportXmlAsync(DataType.Level1, level1)).Elements("change").ToArray();

		changes.Length.AssertEqual(level1.Length);

		for (var i = 0; i < level1.Length; i++)
		{
			var msg = level1[i];

			DateTime.Parse(Attr(changes[i], "serverTime"), CultureInfo.InvariantCulture).AssertEqual(msg.ServerTime.Truncate(_1ms));

			foreach (var field in _level1Template)
				Attr(changes[i], field.ToString()).AssertEqual(Level1Cell(msg, field));
		}
	}

	[TestMethod]
	public async Task Candles_ExportContent()
	{
		// Counting candles cannot tell open from close or high from low. This pins every column the
		// default candle template emits, for each candle kind the suite generates.
		var security = Helper.CreateStorageSecurity();

		var candles = CandleTests.GenerateCandles(security.RandomTicks(1000, true, start: _contentDay), security, CandleTests.PriceRange.Pips(security), CandleTests.TotalTicks, CandleTests.TimeFrame, CandleTests.VolumeRange, CandleTests.BoxSize, CandleTests.PnF(security), true);

		foreach (var group in candles.GroupBy(c => (type: c.GetType(), arg: c.Arg)))
		{
			var dataType = DataType.Create(group.Key.type, group.Key.arg);
			var arr = group.ToArray();

			var rows = await ExportTextRowsAsync(dataType, arr, _txtReg.TemplateTxtCandle);

			rows.Length.AssertEqual(arr.Length);

			for (var i = 0; i < arr.Length; i++)
			{
				var candle = arr[i];

				rows[i].JoinComma().AssertEqual(new[]
				{
					Cell(candle.OpenTime, "yyyyMMdd"),
					Cell(candle.OpenTime, "HH:mm:ss"),
					Cell(candle.OpenPrice),
					Cell(candle.HighPrice),
					Cell(candle.LowPrice),
					Cell(candle.ClosePrice),
					Cell(candle.TotalVolume),
				}.JoinComma());
			}

			var els = (await ExportXmlAsync(dataType, arr)).Elements("candle").ToArray();

			els.Length.AssertEqual(arr.Length);

			for (var i = 0; i < arr.Length; i++)
			{
				var candle = arr[i];
				var el = els[i];

				DateTime.Parse(Attr(el, "openTime"), CultureInfo.InvariantCulture).AssertEqual(candle.OpenTime.Truncate(_1ms));

				Attr(el, "O").AssertEqual(Cell(candle.OpenPrice));
				Attr(el, "H").AssertEqual(Cell(candle.HighPrice));
				Attr(el, "L").AssertEqual(Cell(candle.LowPrice));
				Attr(el, "C").AssertEqual(Cell(candle.ClosePrice));
				Attr(el, "V").AssertEqual(Cell(candle.TotalVolume));
			}
		}
	}

	/// <summary>
	/// A user is entitled to read an exported news file and see the feed's own text: the headline under the
	/// headline column, the source under the source column, the link under the link column, and an empty cell
	/// wherever the feed published nothing. The common export test counts rows, so a headline written into the
	/// source column or a link silently dropped would pass it; nothing but the cells themselves can say the
	/// story a reader ends up with is the story that was published.
	/// </summary>
	[TestMethod]
	public async Task News_ExportContent()
	{
		var full = new NewsMessage
		{
			Id = "N-1",
			ServerTime = _contentDay,
			LocalTime = _contentDay.AddMilliseconds(7),
			SecurityId = $"RIZ4@{BoardCodes.Forts}".ToSecurityId(),
			BoardCode = BoardCodes.Forts,
			Headline = "Rates left unchanged",
			Source = "Reuters",
			Url = "http://example.com/a",
			Priority = NewsPriorities.High,
			Language = "EN",
			ExpiryDate = _contentDay.AddDays(1),
		};

		// The same feed, an item carrying nothing but the time and the headline.
		var bare = new NewsMessage
		{
			ServerTime = _contentDay.AddSeconds(1),
			LocalTime = _contentDay.AddSeconds(1),
			Headline = "No source and no link",
		};

		var news = new[] { full, bare };

		var rows = await ExportTextRowsAsync(DataType.News, news, _txtReg.TemplateTxtNews);

		rows.Length.AssertEqual(news.Length);

		for (var i = 0; i < news.Length; i++)
		{
			var item = news[i];

			rows[i].JoinComma().AssertEqual(new[]
			{
				Cell(item.ServerTime, "yyyyMMdd"),
				Cell(item.ServerTime, "HH:mm:ss"),
				Cell(item.Headline),
				Cell(item.Source),
				Cell(item.Url),
			}.JoinComma());
		}

		var items = (await ExportXmlAsync(DataType.News, news)).Elements("item").ToArray();

		items.Length.AssertEqual(news.Length);

		Attr(items[0], "id").AssertEqual(full.Id);
		DateTime.Parse(Attr(items[0], "serverTime"), CultureInfo.InvariantCulture).AssertEqual(full.ServerTime.Truncate(_1ms));
		DateTime.Parse(Attr(items[0], "localTime"), CultureInfo.InvariantCulture).AssertEqual(full.LocalTime.Truncate(_1ms));
		Attr(items[0], "securityCode").AssertEqual(full.SecurityId.Value.SecurityCode);
		Attr(items[0], "boardCode").AssertEqual(full.BoardCode);
		Attr(items[0], "headline").AssertEqual(full.Headline);
		Attr(items[0], "source").AssertEqual(full.Source);
		Attr(items[0], "url").AssertEqual(full.Url);
		Attr(items[0], "priority").AssertEqual(Cell(full.Priority.Value));
		Attr(items[0], "language").AssertEqual(full.Language);
		DateTime.Parse(Attr(items[0], "expiry"), CultureInfo.InvariantCulture).AssertEqual(full.ExpiryDate.Value.Truncate(_1ms));

		// The bare item published none of those, so the file must not claim it did.
		Attr(items[1], "id").AssertEqual(string.Empty);
		Attr(items[1], "securityCode").AssertEqual(string.Empty);
		Attr(items[1], "boardCode").AssertEqual(string.Empty);
		Attr(items[1], "source").AssertEqual(string.Empty);
		Attr(items[1], "url").AssertEqual(string.Empty);
		Attr(items[1], "priority").AssertEqual(string.Empty);
		Attr(items[1], "language").AssertEqual(string.Empty);
		Attr(items[1], "expiry").AssertEqual(string.Empty);

		Attr(items[1], "headline").AssertEqual(bare.Headline);
		DateTime.Parse(Attr(items[1], "serverTime"), CultureInfo.InvariantCulture).AssertEqual(bare.ServerTime.Truncate(_1ms));
	}

	/// <summary>
	/// A user is entitled to export a news item that carries both its full text and the sequence number that
	/// places it in the stream, and get both back. They answer different questions - the story is what a reader
	/// reads, the sequence number is what a resuming caller counts from - so carrying one must not cost the
	/// other, nor cost the export of every remaining item in the file.
	/// </summary>
	[TestMethod]
	public async Task News_StoryAndSeqNumAreBothExported()
	{
		var news = new[]
		{
			new NewsMessage
			{
				ServerTime = _contentDay,
				LocalTime = _contentDay,
				Headline = "Rates left unchanged",
				Story = "The board left the rate where it was and said so in one sentence.",
				SeqNum = 7,
			},
		};

		var items = (await ExportXmlAsync(DataType.News, news)).Elements("item").ToArray();

		items.Length.AssertEqual(news.Length);

		items[0].Value.AssertEqual(news[0].Story);
		Attr(items[0], "seqNum").AssertEqual(Cell(news[0].SeqNum));
		Attr(items[0], "headline").AssertEqual(news[0].Headline);
	}

	[TestMethod]
	public async Task News_StoryWithCDataTerminatorRoundTrips()
	{
		const string story = "First section ]]> second section ]]> final section.";
		var news = new[]
		{
			new NewsMessage
			{
				ServerTime = _contentDay,
				LocalTime = _contentDay,
				Headline = "CDATA terminator",
				Story = story,
			},
			new NewsMessage
			{
				ServerTime = _contentDay.AddSeconds(1),
				LocalTime = _contentDay.AddSeconds(1),
				Headline = "Following item",
				Story = "Still exported",
			},
		};

		var items = (await ExportXmlAsync(DataType.News, news)).Elements("item").ToArray();

		items.Length.AssertEqual(2);
		items[0].Value.AssertEqual(story);
		items[1].Value.AssertEqual(news[1].Story);
		Attr(items[1], "headline").AssertEqual(news[1].Headline);
	}

	/// <summary>
	/// A user is entitled to open an exported workbook and find each candle value under its own heading. Open,
	/// high, low and close are four numbers of the same shape, so a row count cannot tell a swap from a correct
	/// sheet; and an open interest the source never carried must leave its cell empty rather than read as a
	/// zero position nobody held.
	/// </summary>
	[TestMethod]
	public async Task Excel_CandleContent()
	{
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromMinutes(1);

		var plain = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			TypedArg = tf,
			OpenTime = _contentDay,
			CloseTime = _contentDay + tf,
			OpenPrice = 100.12345m,
			HighPrice = 101.9m,
			LowPrice = 99.7m,
			ClosePrice = 100.4m,
			TotalVolume = 12.5m,
			State = CandleStates.Finished,
		};

		var extended = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			TypedArg = tf,
			OpenTime = _contentDay + tf,
			CloseTime = _contentDay + tf + tf,
			OpenPrice = 100.4m,
			HighPrice = 100.9m,
			LowPrice = 100.1m,
			ClosePrice = 100.8m,
			TotalVolume = 7m,
			OpenInterest = 0m,
			State = CandleStates.Finished,
		};

		var candles = new[] { plain, extended };
		var provider = ServicesRegistry.ExcelProvider;

		using var stream = new MemoryStream();

		var (count, lastTime) = await new ExcelExporter(provider, DataType.TimeFrame(tf), stream, () => { })
			.Export(candles.ToAsyncEnumerable(), CancellationToken);

		count.AssertEqual(candles.Length);
		lastTime.AssertEqual(extended.OpenTime);

		stream.Position = 0;

		using var worker = provider.OpenExist(stream);

		worker.SwitchSheet(LocalizedStrings.Export);

		// The headings say which column is which, so they are part of the promise, not decoration.
		worker.GetCell<string>(0, 0).AssertEqual(LocalizedStrings.Time);
		worker.GetCell<string>(1, 0).AssertEqual("O");
		worker.GetCell<string>(2, 0).AssertEqual("H");
		worker.GetCell<string>(3, 0).AssertEqual("L");
		worker.GetCell<string>(4, 0).AssertEqual("C");
		worker.GetCell<string>(5, 0).AssertEqual("V");
		worker.GetCell<string>(6, 0).AssertEqual(LocalizedStrings.OI);

		for (var i = 0; i < candles.Length; i++)
		{
			var candle = candles[i];
			var row = i + 1;

			worker.GetCell<DateTime>(0, row).AssertEqual(candle.OpenTime);
			worker.GetCell<decimal>(1, row).AssertEqual(candle.OpenPrice);
			worker.GetCell<decimal>(2, row).AssertEqual(candle.HighPrice);
			worker.GetCell<decimal>(3, row).AssertEqual(candle.LowPrice);
			worker.GetCell<decimal>(4, row).AssertEqual(candle.ClosePrice);
			worker.GetCell<decimal>(5, row).AssertEqual(candle.TotalVolume);
		}

		// Never set by the source, so the cell stays empty.
		IsNull(worker.GetCell<decimal?>(6, 1), "an open interest the candle never carried must not be written");

		// Set to zero by the source, so the cell carries zero - that difference is the whole point.
		worker.GetCell<decimal?>(6, 2).AssertEqual((decimal?)0m);

		// Nothing beyond the two candles may be written under them.
		IsNull(worker.GetCell<decimal?>(1, candles.Length + 1), "a row past the last candle must stay empty");
	}

	[TestMethod]
	public async Task Json_DepthContent()
	{
		// ExportAsync counts the json objects and checks the stream is not empty, so a book whose sides
		// were swapped, whose volume landed on the neighbouring price, or whose per-quote extras leaked
		// into the wrong quote passes it. This pins the payload of a plain book and of one carrying every
		// optional field, and pins that a field the source never set is left out rather than written as a
		// zero - a reader cannot tell a substituted zero from a published one.
		var secId = Helper.CreateSecurityId();

		var plain = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = _contentDay.AddTicks(1_234_567),
			LocalTime = _contentDay.AddSeconds(1),
			Bids = [new(99.5m, 10m), new(99.25m, 20m, 3)],
			Asks = [new(100.5m, 1.5m)],
		};

		var extended = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = _contentDay.AddSeconds(2),
			LocalTime = _contentDay.AddSeconds(3),
			State = QuoteChangeStates.SnapshotComplete,
			HasPositions = true,
			SeqNum = 42,
			Bids = [new(98m, 5m) { StartPosition = 2, EndPosition = 4, Action = QuoteChangeActions.Update, Condition = QuoteConditions.Indicative }],
			Asks = [],
		};

		var books = await ExportJsonAsync(DataType.MarketDepth, new[] { plain, extended });

		books.Count.AssertEqual(2);

		var first = books[0];

		((DateTime)first["s"]).AssertEqual(plain.ServerTime);
		((DateTime)first["l"]).AssertEqual(plain.LocalTime);

		// Nothing of the kind was set on the plain book, so nothing of the kind may appear.
		Opt(first, "st").AssertNull();
		Opt(first, "pos").AssertNull();
		Opt(first, "sn").AssertNull();

		var bids = (JArray)first["bids"];
		var asks = (JArray)first["asks"];

		bids.Count.AssertEqual(2);
		asks.Count.AssertEqual(1);

		((decimal)bids[0]["p"]).AssertEqual(99.5m);
		((decimal)bids[0]["v"]).AssertEqual(10m);
		Opt(bids[0], "cnt").AssertNull();
		Opt(bids[0], "s").AssertNull();
		Opt(bids[0], "e").AssertNull();
		Opt(bids[0], "a").AssertNull();
		Opt(bids[0], "cond").AssertNull();

		((decimal)bids[1]["p"]).AssertEqual(99.25m);
		((decimal)bids[1]["v"]).AssertEqual(20m);
		((int)bids[1]["cnt"]).AssertEqual(3);

		((decimal)asks[0]["p"]).AssertEqual(100.5m);
		((decimal)asks[0]["v"]).AssertEqual(1.5m);
		Opt(asks[0], "cnt").AssertNull();

		var second = books[1];

		((DateTime)second["s"]).AssertEqual(extended.ServerTime);
		second["st"].ToObject<QuoteChangeStates>().AssertEqual(QuoteChangeStates.SnapshotComplete);
		((bool)second["pos"]).AssertTrue();
		((long)second["sn"]).AssertEqual(42L);

		var increment = (JArray)second["bids"];

		increment.Count.AssertEqual(1);
		((JArray)second["asks"]).Count.AssertEqual(0);

		((decimal)increment[0]["p"]).AssertEqual(98m);
		((decimal)increment[0]["v"]).AssertEqual(5m);
		((int)increment[0]["s"]).AssertEqual(2);
		((int)increment[0]["e"]).AssertEqual(4);
		increment[0]["a"].ToObject<QuoteChangeActions>().AssertEqual(QuoteChangeActions.Update);
		increment[0]["cond"].ToObject<QuoteConditions>().AssertEqual(QuoteConditions.Indicative);
	}

	[TestMethod]
	public async Task Json_CandleContent()
	{
		// Open, high, low and close are four numbers of the same shape: a count cannot tell them apart,
		// so a swap survives every check the common set makes. Each is pinned to its own key here, along
		// with an open interest of zero the source really carried and a volume profile.
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromMinutes(1);

		var plain = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			TypedArg = tf,
			OpenTime = _contentDay,
			CloseTime = _contentDay + tf,
			OpenPrice = 100.12345m,
			HighPrice = 101.9m,
			LowPrice = 99.7m,
			ClosePrice = 100.4m,
			TotalVolume = 12.5m,
			State = CandleStates.Finished,
		};

		var extended = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			TypedArg = tf,
			OpenTime = _contentDay + tf,
			CloseTime = _contentDay + tf + tf,
			OpenPrice = 100.4m,
			HighPrice = 100.9m,
			LowPrice = 100.1m,
			ClosePrice = 100.8m,
			TotalVolume = 7m,
			OpenInterest = 0m,
			SeqNum = 11,
			State = CandleStates.Finished,
			PriceLevels =
			[
				new() { Price = 100.5m, BuyCount = 2, SellCount = 3, BuyVolume = 4.5m, SellVolume = 5.5m, TotalVolume = 10m },
			],
		};

		var candles = await ExportJsonAsync(DataType.TimeFrame(tf), new[] { plain, extended });

		candles.Count.AssertEqual(2);

		((DateTime)candles[0]["open"]).AssertEqual(plain.OpenTime);
		((DateTime)candles[0]["close"]).AssertEqual(plain.CloseTime);
		((decimal)candles[0]["O"]).AssertEqual(100.12345m);
		((decimal)candles[0]["H"]).AssertEqual(101.9m);
		((decimal)candles[0]["L"]).AssertEqual(99.7m);
		((decimal)candles[0]["C"]).AssertEqual(100.4m);
		((decimal)candles[0]["V"]).AssertEqual(12.5m);

		Opt(candles[0], "oi").AssertNull();
		Opt(candles[0], "sn").AssertNull();
		Opt(candles[0], "levels").AssertNull();

		((decimal)candles[1]["O"]).AssertEqual(100.4m);
		((decimal)candles[1]["H"]).AssertEqual(100.9m);
		((decimal)candles[1]["L"]).AssertEqual(100.1m);
		((decimal)candles[1]["C"]).AssertEqual(100.8m);

		// An open interest of zero is a value the source carried, not a missing one.
		((decimal)candles[1]["oi"]).AssertEqual(0m);
		((long)candles[1]["sn"]).AssertEqual(11L);

		var levels = (JArray)candles[1]["levels"];

		levels.Count.AssertEqual(1);
		((decimal)levels[0]["price"]).AssertEqual(100.5m);
		((int)levels[0]["buyCount"]).AssertEqual(2);
		((int)levels[0]["sellCount"]).AssertEqual(3);
		((decimal)levels[0]["buyVolume"]).AssertEqual(4.5m);
		((decimal)levels[0]["sellVolume"]).AssertEqual(5.5m);
		((decimal)levels[0]["volume"]).AssertEqual(10m);
	}

	[TestMethod]
	public async Task Json_TickOptionalValues()
	{
		// The optional legs of the tick writer are exactly what a count cannot see. Zero and false are
		// values the source carried and must be written; a field it never set must be absent, because a
		// reader has no way to tell an invented zero from a real one.
		var secId = Helper.CreateSecurityId();

		var full = Tick(secId, _contentDay, 100.5m, 2m, 1);

		full.LocalTime = _contentDay.AddMilliseconds(7);
		full.OriginSide = Sides.Sell;
		full.OpenInterest = 0m;
		full.IsUpTick = false;
		full.Currency = CurrencyTypes.USD;
		full.SeqNum = 5;
		full.Yield = 0m;
		full.OrderBuyId = 77;
		full.OrderSellId = 88;

		var bare = Tick(secId, _contentDay.AddSeconds(1), 100.75m, 3m, 2);

		bare.LocalTime = bare.ServerTime;

		var ticks = await ExportJsonAsync(DataType.Ticks, new[] { full, bare });

		ticks.Count.AssertEqual(2);

		((string)ticks[0]["id"]).AssertEqual("1");
		((DateTime)ticks[0]["s"]).AssertEqual(full.ServerTime);
		((DateTime)ticks[0]["l"]).AssertEqual(full.LocalTime);
		((decimal)ticks[0]["p"]).AssertEqual(100.5m);
		((decimal)ticks[0]["v"]).AssertEqual(2m);
		ticks[0]["side"].ToObject<Sides>().AssertEqual(Sides.Sell);
		((decimal)ticks[0]["oi"]).AssertEqual(0m);
		((bool)ticks[0]["up"]).AssertFalse();
		ticks[0]["cur"].ToObject<CurrencyTypes>().AssertEqual(CurrencyTypes.USD);
		((long)ticks[0]["sn"]).AssertEqual(5L);
		((decimal)ticks[0]["yield"]).AssertEqual(0m);
		((long)ticks[0]["buy"]).AssertEqual(77L);
		((long)ticks[0]["sell"]).AssertEqual(88L);

		((string)ticks[1]["id"]).AssertEqual("2");
		((decimal)ticks[1]["p"]).AssertEqual(100.75m);
		((decimal)ticks[1]["v"]).AssertEqual(3m);

		Opt(ticks[1], "side").AssertNull();
		Opt(ticks[1], "oi").AssertNull();
		Opt(ticks[1], "up").AssertNull();
		Opt(ticks[1], "cur").AssertNull();
		Opt(ticks[1], "sn").AssertNull();
		Opt(ticks[1], "yield").AssertNull();
		Opt(ticks[1], "buy").AssertNull();
		Opt(ticks[1], "sell").AssertNull();
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

	private static string DescribeTicks(IEnumerable<ExecutionMessage> ticks)
		=> ticks.Select(t => $"{Cell(t.ServerTime, "yyyyMMdd HH:mm:ss.ffffff")};{Cell(t.TradeId)};{Cell(t.TradePrice)};{Cell(t.TradeVolume)}").JoinN();

	private async Task<ExecutionMessage[]> LoadTicksAsync(IStorageRegistry registry, IMarketDataDrive drive, SecurityId secId)
		=> await ((IMarketDataStorage<ExecutionMessage>)registry.GetStorage(secId, DataType.Ticks, drive, StorageFormats.Csv))
			.LoadAsync(default, default).ToArrayAsync(CancellationToken);

	[TestMethod]
	public async Task StockSharp_InterleavedSecurities_LastTimeIsLastMessage()
	{
		// StockSharpExporter is in no format test at all. It regroups a package by security before saving,
		// so the group it happens to finish on is not the message the caller sent last. lastTime is what a
		// caller writes down to resume the next export from, so it has to be the time of the last exported
		// message - here A at t3, which is also the largest - whatever order the groups were saved in.
		var fs = Helper.MemorySystem;
		var registry = fs.GetStorage(fs.GetSubTemp());
		var drive = registry.DefaultDrive;

		var secA = Helper.CreateSecurityId();
		var secB = Helper.CreateSecurityId();

		var t1 = _contentDay;
		var t2 = t1.AddSeconds(1);
		var t3 = t1.AddSeconds(2);

		var ticks = new[]
		{
			Tick(secA, t1, 100m, 1m, 1),
			Tick(secB, t2, 200m, 2m, 2),
			Tick(secA, t3, 300m, 3m, 3),
		};

		var (count, lastTime) = await new StockSharpExporter(DataType.Ticks, registry, drive, StorageFormats.Csv)
			.Export(ticks.ToAsyncEnumerable(), CancellationToken);

		count.AssertEqual(ticks.Length);

		// Each security gets its own messages, and only its own.
		DescribeTicks(await LoadTicksAsync(registry, drive, secA)).AssertEqual(DescribeTicks([ticks[0], ticks[2]]));
		DescribeTicks(await LoadTicksAsync(registry, drive, secB)).AssertEqual(DescribeTicks([ticks[1]]));

		lastTime.AssertEqual(t3);
	}

	[TestMethod]
	public async Task StockSharp_BatchSize_SavesEveryMessageOnce()
	{
		// BatchSize only says how much is held in memory at a time, so it must change neither what is
		// saved nor what is reported. The sizes straddle the input - well below it, one short of it, and
		// equal to it - because it is the regrouping inside a single package that a boundary exposes.
		var secA = Helper.CreateSecurityId();
		var secB = Helper.CreateSecurityId();

		const int total = 51;

		var ticks = new ExecutionMessage[total];

		for (var i = 0; i < total; i++)
			ticks[i] = Tick(i % 2 == 0 ? secA : secB, _contentDay.AddSeconds(i), 100m + i, 1m + i, i + 1);

		var expectedA = DescribeTicks(ticks.Where((_, i) => i % 2 == 0));
		var expectedB = DescribeTicks(ticks.Where((_, i) => i % 2 != 0));

		foreach (var batchSize in new[] { 1, 50, total })
		{
			var fs = Helper.MemorySystem;
			var registry = fs.GetStorage(fs.GetSubTemp());
			var drive = registry.DefaultDrive;

			var exporter = new StockSharpExporter(DataType.Ticks, registry, drive, StorageFormats.Csv)
			{
				BatchSize = batchSize,
			};

			var (count, lastTime) = await exporter.Export(ticks.ToAsyncEnumerable(), CancellationToken);

			count.AssertEqual(total, $"count for batch {batchSize}");

			DescribeTicks(await LoadTicksAsync(registry, drive, secA)).AssertEqual(expectedA);
			DescribeTicks(await LoadTicksAsync(registry, drive, secB)).AssertEqual(expectedB);

			lastTime.AssertEqual(ticks[total - 1].ServerTime, $"lastTime for batch {batchSize}");
		}
	}

#if NET10_0_OR_GREATER
	/// <summary>
	/// Every format test carries a database leg that writes to a real server named by a secret. A
	/// machine that was never given one cannot answer whether the exporter writes correct rows, and
	/// neither colour is honest about that: red blames the exporter for an absent server, green claims
	/// the rows were checked. The distinction is drawn once here, so that an unconfigured machine is
	/// told so by name; and when a server is named, it has to be one the run can actually open, so that
	/// every database failure below belongs to the exporter rather than to the connection it was given.
	/// </summary>
	[TestMethod]
	public async Task MissingDatabaseServerIsReportedAsInconclusiveNotFailure()
	{
		var connStr = DbConnStr;

		var providers = DatabaseProviderRegistry.AllProviders;

		IsNotEmpty(providers, "No database provider is registered, so the database leg of every export below picks its driver out of an empty list.");

		using var connection = DatabaseRegistry.Provider.CreateConnection(new()
		{
			Provider = providers.First(),
			ConnectionString = connStr,
		});

		IsNotNull(connection, "The registered provider handed back no connection for the server this run was pointed at.");

		// Opening it here names the connection as the thing that failed. Left to the export legs, the
		// same failure arrives as a stack trace inside a writer and reads like a bug in the export.
		await connection.VerifyAsync(CancellationToken);
	}
#endif

	private static DatabaseExporter CreateDbExporter(RecordingDatabaseProvider provider, DataType dataType)
		=> new(provider, dataType, new DatabaseConnectionPair { Provider = "recording", ConnectionString = "recording" });

	// A value the exporter did not put into the row must read as absent, never as a substituted zero.
	private static object Value(IDictionary<string, object> row, string name)
		=> row.TryGetValue(name, out var value) ? value : null;

	[TestMethod]
	public async Task Database_Candles_RowsCarryEveryValue()
	{
		// The DB leg of the common set checks the count the exporter returns, not the rows it handed the
		// driver, so a column mapped to the wrong property is invisible to it. This reads the rows: every
		// value as the message carried it, undivided and unrounded, an unset optional left null, and no
		// column in a row that the table was not created with.
		var secId = Helper.CreateSecurityId();
		var tf = TimeSpan.FromMinutes(1);

		var plain = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			TypedArg = tf,
			OpenTime = _contentDay,
			CloseTime = _contentDay + tf,
			OpenPrice = 100.12345m,
			HighPrice = 101.9m,
			LowPrice = 99.7m,
			ClosePrice = 100.4m,
			TotalVolume = 12.5m,
			State = CandleStates.Finished,
		};

		var extended = new TimeFrameCandleMessage
		{
			SecurityId = secId,
			TypedArg = tf,
			OpenTime = _contentDay + tf,
			CloseTime = _contentDay + tf + tf,
			OpenPrice = 100.4m,
			HighPrice = 100.9m,
			LowPrice = 100.1m,
			ClosePrice = 100.8m,
			TotalVolume = 7m,
			OpenInterest = 0m,
			TotalTicks = 9,
			SeqNum = 11,
			State = CandleStates.Finished,
		};

		var provider = new RecordingDatabaseProvider();

		var (count, lastTime) = await CreateDbExporter(provider, DataType.TimeFrame(tf))
			.Export(new[] { plain, extended }.ToAsyncEnumerable(), CancellationToken);

		count.AssertEqual(2);
		lastTime.AssertEqual(extended.OpenTime);

		var table = provider.SingleTable;

		table.Name.AssertEqual("Candle");

		// DropExisting is off by default, and dropping a table nobody asked to drop destroys data.
		table.DropCount.AssertEqual(0);
		table.Creates.Count.AssertEqual(1);
		table.Rows.Count.AssertEqual(2);

		var columns = table.Creates[0];

		foreach (var row in table.Rows)
		{
			foreach (var key in row.Keys)
				columns.ContainsKey(key).AssertTrue(key);
		}

		var first = table.Rows[0];

		Value(first, "SecurityCode").AssertEqual((object)secId.SecurityCode);
		Value(first, "BoardCode").AssertEqual((object)secId.BoardCode);
		Value(first, "Type").AssertEqual((object)nameof(MessageTypes.CandleTimeFrame));
		Value(first, "Arg").AssertEqual((object)tf.Ticks);
		Value(first, "OpenTime").AssertEqual((object)plain.OpenTime);
		Value(first, "OpenPrice").AssertEqual((object)100.12345m);
		Value(first, "HighPrice").AssertEqual((object)101.9m);
		Value(first, "LowPrice").AssertEqual((object)99.7m);
		Value(first, "ClosePrice").AssertEqual((object)100.4m);
		Value(first, "TotalVolume").AssertEqual((object)12.5m);

		// Decimal equality ignores scale, so the rendered form is compared too: the row must carry the
		// value the message carried, to the digit, neither rounded to a step nor rescaled.
		Cell(Value(first, "OpenPrice")).AssertEqual("100.12345");

		// Never set by the source.
		Value(first, "OpenInterest").AssertNull();
		Value(first, "TotalTicks").AssertNull();

		var second = table.Rows[1];

		Value(second, "OpenTime").AssertEqual((object)extended.OpenTime);
		Value(second, "OpenPrice").AssertEqual((object)100.4m);
		Value(second, "HighPrice").AssertEqual((object)100.9m);
		Value(second, "LowPrice").AssertEqual((object)100.1m);
		Value(second, "ClosePrice").AssertEqual((object)100.8m);

		// Set to zero by the source, so the row carries zero - the difference from the row above is the
		// whole point of a nullable column.
		Value(second, "OpenInterest").AssertEqual((object)0m);
		Value(second, "TotalTicks").AssertEqual((object)9);
		Value(second, "SeqNum").AssertEqual((object)11L);

		// The connection is opened for the export and must not outlive it.
		provider.Connections.Single().IsDisposed.AssertTrue();
	}

	[TestMethod]
	public async Task Database_BatchSize_PackagesEveryRowOnce()
	{
		// BatchSize is documented as the size of the transmitted package. Whatever it is set to, the
		// driver must receive every row exactly once and in export order, in packages no larger than
		// that: a row dropped or repeated at a package boundary is a silent data error.
		var secId = Helper.CreateSecurityId();

		var ticks = new ExecutionMessage[5];

		for (var i = 0; i < ticks.Length; i++)
			ticks[i] = Tick(secId, _contentDay.AddSeconds(i), 100m + i, 1m + i, i + 1);

		var expectedIds = ticks.Select(t => t.TradeId.To<string>()).JoinComma();

		foreach (var batchSize in new[] { 1, 2, 5, 10 })
		{
			var provider = new RecordingDatabaseProvider();

			var exporter = CreateDbExporter(provider, DataType.Ticks);
			exporter.BatchSize = batchSize;
			// Stated rather than inherited: what CheckUnique defaults to is disputed (see the property's doc),
			// and this test is about packaging, not about that.
			exporter.CheckUnique = false;

			var (count, lastTime) = await exporter.Export(ticks.ToAsyncEnumerable(), CancellationToken);

			var expectedSizes = new List<int>();

			for (var left = ticks.Length; left > 0; left -= batchSize)
				expectedSizes.Add(batchSize.Min(left));

			var table = provider.SingleTable;

			table.Name.AssertEqual("Execution");
			table.BulkSizes.Select(s => s.To<string>()).JoinComma().AssertEqual(expectedSizes.Select(s => s.To<string>()).JoinComma());
			table.SingleInserts.AssertEqual(0, $"single inserts for batch {batchSize}");
			table.Rows.Select(r => (string)Value(r, "TradeId")).JoinComma().AssertEqual(expectedIds);

			count.AssertEqual(ticks.Length, $"count for batch {batchSize}");
			lastTime.AssertEqual(ticks[^1].ServerTime, $"lastTime for batch {batchSize}");
		}
	}

	[TestMethod]
	public async Task Database_Level1_AbsentFieldIsNotWrittenAsZero()
	{
		// A Level1 message carries an arbitrary subset of fields while the table has a column for every
		// field, so the row is the only place that can say which of them the message actually carried:
		// an explicit zero is a quote, an unset field is not, and writing the unset one as zero publishes
		// a price nobody quoted.
		var secId = Helper.CreateSecurityId();

		var message = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = _contentDay,
		};

		message.Add(Level1Fields.BestBidPrice, 99.5m);
		message.Add(Level1Fields.BestBidVolume, 0m);
		message.Add(Level1Fields.State, SecurityStates.Trading);

		var provider = new RecordingDatabaseProvider();

		var (count, _) = await CreateDbExporter(provider, DataType.Level1)
			.Export(new[] { message }.ToAsyncEnumerable(), CancellationToken);

		count.AssertEqual(1);

		var table = provider.SingleTable;

		table.Name.AssertEqual("Level1");

		var columns = table.Creates[0];
		var row = table.Rows.Single();

		foreach (var key in row.Keys)
			columns.ContainsKey(key).AssertTrue(key);

		Value(row, "ServerTime").AssertEqual((object)message.ServerTime);
		Value(row, "SecurityCode").AssertEqual((object)secId.SecurityCode);
		Value(row, "BoardCode").AssertEqual((object)secId.BoardCode);
		Value(row, "BestBidPrice").AssertEqual((object)99.5m);

		// Published as zero, so stored as zero.
		Value(row, "BestBidVolume").AssertEqual((object)0m);

		// Never published, so nothing at all.
		Value(row, "BestAskPrice").AssertNull();
		Value(row, "BestAskVolume").AssertNull();

		// An enum field is declared as its numeric base type, so whatever the row carries for it has to
		// be storable in that column.
		columns["State"].AssertEqual(typeof(int?));
		Value(row, "State").To<int?>().AssertEqual((int?)(int)SecurityStates.Trading);
	}

	[TestMethod]
	public async Task Database_CheckUnique_DoesNotUseBulkPath()
	{
		// CheckUnique promises the export checks uniqueness. Whichever way that is decided - the exporter
		// suppressing a repeat, or a key in the table rejecting it - it cannot be honoured by a bulk
		// insert, which hands the driver a block of rows with nothing to check them against. So the rows
		// must leave by the single-row path, and each distinct row must still arrive.
		var secId = Helper.CreateSecurityId();

		var first = Tick(secId, _contentDay, 100m, 1m, 1);
		var repeat = Tick(secId, _contentDay, 100m, 1m, 1);
		var other = Tick(secId, _contentDay.AddSeconds(1), 101m, 2m, 2);

		var provider = new RecordingDatabaseProvider();

		var exporter = CreateDbExporter(provider, DataType.Ticks);
		exporter.CheckUnique = true;

		await exporter.Export(new[] { first, repeat, other }.ToAsyncEnumerable(), CancellationToken);

		var table = provider.SingleTable;

		table.BulkSizes.Count.AssertEqual(0);

		// Everything that reached the table came in by that path, and nothing arrived by another.
		table.SingleInserts.AssertEqual(table.Rows.Count);

		table.Rows.Any(r => (string)Value(r, "TradeId") == "1").AssertTrue();
		table.Rows.Any(r => (string)Value(r, "TradeId") == "2").AssertTrue();
	}

	[TestMethod]
	public async Task Database_FailedPackage_PropagatesAndIsNotResent()
	{
		// A package the driver rejects has to reach the caller as a failure, not as a count that reads
		// like success. And the exporter must not answer a failure by sending anything again: the rows
		// of the packages before it are already in the table, and a resend doubles them.
		var secId = Helper.CreateSecurityId();

		var ticks = new ExecutionMessage[6];

		for (var i = 0; i < ticks.Length; i++)
			ticks[i] = Tick(secId, _contentDay.AddSeconds(i), 100m + i, 1m + i, i + 1);

		var provider = new RecordingDatabaseProvider();

		provider.Table("Execution").FailBulkAt = 2;

		var exporter = CreateDbExporter(provider, DataType.Ticks);
		exporter.BatchSize = 2;
		exporter.CheckUnique = false;

		await ThrowsAsync<InvalidOperationException>(() => exporter.Export(ticks.ToAsyncEnumerable(), CancellationToken));

		var table = provider.SingleTable;

		table.BulkSizes.Count.AssertEqual(1);
		table.Rows.Select(r => (string)Value(r, "TradeId")).JoinComma().AssertEqual("1,2");
		table.SingleInserts.AssertEqual(0);
		provider.Connections.Single().IsDisposed.AssertTrue();
	}

	// The DB legs above run against these instead of a server: the point is what the exporter hands the
	// driver, which a real connection swallows into rows nobody reads back.
	private class RecordingDatabaseConnection : IDatabaseConnection
	{
		public bool IsDisposed { get; private set; }

		void IDisposable.Dispose() => IsDisposed = true;

		Task IDatabaseConnection.VerifyAsync(CancellationToken cancellationToken) => Task.CompletedTask;
	}

	private class RecordingDatabaseTable(string name) : IDatabaseTable
	{
		public string Name { get; } = name;

		public int DropCount { get; private set; }
		public int SingleInserts { get; private set; }

		public List<IDictionary<string, Type>> Creates { get; } = [];
		public List<IDictionary<string, object>> Rows { get; } = [];
		public List<int> BulkSizes { get; } = [];

		/// <summary>
		/// 1-based number of the bulk insert that fails, before it records anything. Zero means none.
		/// </summary>
		public int FailBulkAt { get; set; }

		Task IDatabaseTable.CreateAsync(IDictionary<string, Type> columns, CancellationToken cancellationToken)
		{
			Creates.Add(columns);
			return Task.CompletedTask;
		}

		Task IDatabaseTable.DropAsync(CancellationToken cancellationToken)
		{
			DropCount++;
			return Task.CompletedTask;
		}

		Task IDatabaseTable.InsertAsync(IDictionary<string, object> values, CancellationToken cancellationToken)
		{
			SingleInserts++;
			Rows.Add(values);
			return Task.CompletedTask;
		}

		Task IDatabaseTable.BulkInsertAsync(IEnumerable<IDictionary<string, object>> rows, CancellationToken cancellationToken)
		{
			if (FailBulkAt > 0 && (BulkSizes.Count + 1) == FailBulkAt)
				throw new InvalidOperationException($"{Name}: package {FailBulkAt} rejected.");

			var arr = rows.ToArray();

			BulkSizes.Add(arr.Length);
			Rows.AddRange(arr);

			return Task.CompletedTask;
		}

		Task IDatabaseTable.ModifyAsync(IDictionary<string, Type> columns, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		Task<IEnumerable<IDictionary<string, object>>> IDatabaseTable.SelectAsync(IEnumerable<FilterCondition> filters, IEnumerable<OrderByCondition> orderBy, long? skip, long? take, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		Task IDatabaseTable.UpdateAsync(IDictionary<string, object> values, IEnumerable<FilterCondition> filters, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		Task<int> IDatabaseTable.DeleteAsync(IEnumerable<FilterCondition> filters, CancellationToken cancellationToken)
			=> throw new NotSupportedException();

		Task IDatabaseTable.UpsertAsync(IDictionary<string, object> values, IEnumerable<string> keyColumns, CancellationToken cancellationToken)
			=> throw new NotSupportedException();
	}

	private class RecordingDatabaseProvider : IDatabaseProvider
	{
		private readonly Dictionary<string, RecordingDatabaseTable> _tables = [];

		public List<RecordingDatabaseConnection> Connections { get; } = [];

		public RecordingDatabaseTable SingleTable => _tables.Values.Single();

		public RecordingDatabaseTable Table(string tableName)
			=> _tables.SafeAdd(tableName, n => new RecordingDatabaseTable(n));

		IDatabaseConnection IDatabaseProvider.CreateConnection(DatabaseConnectionPair pair)
		{
			var connection = new RecordingDatabaseConnection();
			Connections.Add(connection);
			return connection;
		}

		IDatabaseTable IDatabaseProvider.GetTable(IDatabaseConnection connection, string tableName)
			=> Table(tableName);
	}
}
