namespace StockSharp.Tests;

using StockSharp.Algo.Export;

[TestClass]
public class ExportTests
{
	[TestMethod]
	public void Ticks()
	{
		var security = Helper.CreateStorageSecurity();
		var ticks = security.RandomTicks(1000, true);

		Export(Messages.DataType.Ticks, ticks, "tick_export", new TemplateTxtRegistry().TemplateTxtTick);
	}

	[TestMethod]
	public void Depths()
	{
		var security = Helper.CreateStorageSecurity();
		var depths = security.RandomDepths(100, ordersCount: true);

		Export(Messages.DataType.MarketDepth, depths, "depth_export", new TemplateTxtRegistry().TemplateTxtDepth);
	}

	[TestMethod]
	public void OrderLog()
	{
		var security = Helper.CreateStorageSecurity();
		var ol = security.RandomOrderLog(1000);

		Export(Messages.DataType.OrderLog, ol, "ol_export", new TemplateTxtRegistry().TemplateTxtOrderLog);
	}

	[TestMethod]
	public void Positions()
	{
		var security = Helper.CreateStorageSecurity();
		var pos = security.RandomPositionChanges(false, 1000);

		Export(Messages.DataType.PositionChanges, pos, "pos_export", new TemplateTxtRegistry().TemplateTxtPositionChange);
	}

	[TestMethod]
	public void News()
	{
		var news = Helper.RandomNews(false);

		Export(Messages.DataType.News, news, "news_export", new TemplateTxtRegistry().TemplateTxtNews);
	}

	[TestMethod]
	public void Level1()
	{
		var security = Helper.CreateStorageSecurity();
		var level1 = security.RandomLevel1(count: 1000);

		Export(Messages.DataType.Level1, level1, "level1_export", new TemplateTxtRegistry().TemplateTxtLevel1);
	}

	[TestMethod]
	public void Candles()
	{
		var security = Helper.CreateStorageSecurity();

		var candles = CandleTests.GenerateCandles(security.RandomTicks(1000, true), security, CandleTests.PriceRange.Pips(security), CandleTests.TotalTicks, CandleTests.TimeFrame, CandleTests.VolumeRange, CandleTests.BoxSize, CandleTests.PnF(security), true);

		foreach (var group in candles.GroupBy(c => Tuple.Create(c.GetType(), c.Arg)))
		{
			var name = $"candles_{group.Key.Item1.Name}_{group.Key.Item2}_export".Replace(":", "_");
			Export(Messages.DataType.Create(group.Key.Item1, group.Key.Item2), group.ToArray(), name, new TemplateTxtRegistry().TemplateTxtCandle);
		}
	}

	[TestMethod]
	public void Indicator()
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

		Export(TraderHelper.IndicatorValue, values, "indicator_export", new TemplateTxtRegistry().TemplateTxtIndicator);
	}

	private static void Export<TValue>(
		Messages.DataType dataType, IEnumerable<TValue> values,
		string fileNameNoExt, string txtTemplate)
		where TValue : class
	{
		var arr = values.ToArray();

		void Do(string extension, Func<string, BaseExporter> create)
		{
			var fileName = Helper.GetSubTemp($"{fileNameNoExt}.{extension}");
			var export = create(fileName);
			export.Export(arr);
		}

		Do("txt", f => new TextExporter(dataType, i => false, f, txtTemplate, null));
		Do("xml", f => new XmlExporter(dataType, i => false, f));
		Do("json", f => new JsonExporter(dataType, i => false, f));
		Do("xlsx", f => new ExcelExporter(new DevExpExcelWorkerProvider(), dataType, i => false, f, () => { }));
	}
}