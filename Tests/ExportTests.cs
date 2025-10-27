namespace StockSharp.Tests;

using StockSharp.Algo.Export;

[TestClass]
public class ExportTests : BaseTestClass
{
	private static readonly TemplateTxtRegistry _txtReg = new();

	[TestMethod]
	public Task Ticks()
	{
		var security = Helper.CreateStorageSecurity();
		var ticks = security.RandomTicks(1000, true);

		return Export(DataType.Ticks, ticks, "tick_export", _txtReg.TemplateTxtTick);
	}

	[TestMethod]
	public Task Depths()
	{
		var security = Helper.CreateStorageSecurity();
		var depths = security.RandomDepths(100, ordersCount: true);

		return Export(DataType.MarketDepth, depths, "depth_export", _txtReg.TemplateTxtDepth);
	}

	[TestMethod]
	public Task OrderLog()
	{
		var security = Helper.CreateStorageSecurity();
		var ol = security.RandomOrderLog(1000);

		return Export(DataType.OrderLog, ol, "ol_export", _txtReg.TemplateTxtOrderLog);
	}

	[TestMethod]
	public Task Positions()
	{
		var security = Helper.CreateStorageSecurity();
		var pos = security.RandomPositionChanges(1000);

		return Export(DataType.PositionChanges, pos, "pos_export", _txtReg.TemplateTxtPositionChange);
	}

	[TestMethod]
	public Task News()
	{
		var news = Helper.RandomNews();

		return Export(DataType.News, news, "news_export", _txtReg.TemplateTxtNews);
	}

	[TestMethod]
	public Task Level1()
	{
		var security = Helper.CreateStorageSecurity();
		var level1 = security.RandomLevel1(count: 1000);

		return Export(DataType.Level1, level1, "level1_export", _txtReg.TemplateTxtLevel1);
	}

	[TestMethod]
	public async Task Candles()
	{
		var security = Helper.CreateStorageSecurity();

		var candles = CandleTests.GenerateCandles(security.RandomTicks(1000, true), security, CandleTests.PriceRange.Pips(security), CandleTests.TotalTicks, CandleTests.TimeFrame, CandleTests.VolumeRange, CandleTests.BoxSize, CandleTests.PnF(security), true);

		foreach (var group in candles.GroupBy(c => Tuple.Create(c.GetType(), c.Arg)))
		{
			var name = $"candles_{group.Key.Item1.Name}_{group.Key.Item2}_export".Replace(":", "_");
			await Export(DataType.Create(group.Key.Item1, group.Key.Item2), group.ToArray(), name, _txtReg.TemplateTxtCandle);
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

		return Export(TraderHelper.IndicatorValue, values, "indicator_export", _txtReg.TemplateTxtIndicator);
	}

	[TestMethod]
	public Task Board()
	{
		var boards = Helper.RandomBoards(100);
		return Export(DataType.Board, boards, "board_export", _txtReg.TemplateTxtBoard);
	}

	[TestMethod]
	public Task BoardState()
	{
		var boardStates = Helper.RandomBoardStates();
		return Export(DataType.BoardState, boardStates, "boardstate_export", _txtReg.TemplateTxtBoardState);
	}

	[TestMethod]
	public Task Security()
	{
		var securities = Helper.RandomSecurities(100);
		return Export(DataType.Securities, securities, "security_export", _txtReg.TemplateTxtSecurity);
	}

	private async Task Export<TValue>(
		DataType dataType, IEnumerable<TValue> values,
		string fileNameNoExt, string txtTemplate)
		where TValue : class
	{
		var token = CancellationToken;
		var arr = values.ToArray();

		Task Do(string extension, Func<Stream, BaseExporter> create)
		{
			using var stream = File.OpenWrite(Helper.GetSubTemp($"{fileNameNoExt}.{extension}"));
			var export = create(stream);
			return export.Export(arr, token);
		}

		await Do("txt", f => new TextExporter(dataType, f, txtTemplate, null));
		await Do("xml", f => new XmlExporter(dataType, f));
		await Do("json", f => new JsonExporter(dataType, f));
		await Do("xlsx", f => new ExcelExporter(ServicesRegistry.ExcelProvider, dataType, f, () => { }));
	}
}