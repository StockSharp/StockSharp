namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

[TestClass]
[DoNotParallelize]
public class StorageNotParallelizeTests : BaseTestClass
{
	private static readonly DateTime _regressionFrom = DateTime.ParseExact("01/12/2021 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture).ApplyMoscow().UtcDateTime;
	private static readonly DateTime _regressionTo = DateTime.ParseExact("01/01/2022 +03:00", "dd/MM/yyyy zzz", CultureInfo.InvariantCulture).ApplyMoscow().UtcDateTime;
	private static readonly int[] _sourceArray = [01, 02, 03, 06, 07, 08, 09, 10, 13, 14, 15, 16, 17, 20, 21, 22, 23, 24, 27, 28, 29, 30];

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public async Task RegressionBuildFromSmallerTimeframes(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/hydra-10
		// build daily candles from smaller timeframes

		var token = CancellationToken;
		var secId = "SBER@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var expectedDates = _sourceArray.Select(d => new DateTime(2021, 12, d)).ToHashSet();
		var dates = (await buildableStorage.GetDatesAsync(CancellationToken)).ToHashSet();

		expectedDates.SetEquals(dates).AssertTrue();

		var candles = await buildableStorage.LoadAsync(_regressionFrom, _regressionTo).ToArrayAsync(token);
		candles.Length.AssertEqual(expectedDates.Count);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public async Task RegressionBuildFromSmallerTimeframesCandleOrder(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/hydra-10
		// build daily candles from smaller timeframes using original issue data, ensure candle updates are ordered in time and not doubled

		var token = CancellationToken;
		var secId = "SBER1@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var candles = await buildableStorage.LoadAsync(_regressionFrom, _regressionTo).ToArrayAsync(token);

		CandleMessage prevCandle = null;

		foreach (var c in candles)
		{
			if (prevCandle == null)
			{
				prevCandle = c.TypedClone();
				continue;
			}

			(c.OpenTime > prevCandle.OpenTime ||
				c.OpenTime == prevCandle.OpenTime && prevCandle.State == CandleStates.Active).AssertTrue();

			prevCandle = c.TypedClone();
		}
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	//[DataRow(StorageFormats.Csv)]
	public async Task RegressionBuildableRange(StorageFormats format)
	{
		// https://stocksharp.myjetbrains.com/youtrack/issue/SS-192

		var secId = "SBER@MICEX".ToSecurityId();

		var reg = Helper.GetResourceStorage();
		var token = CancellationToken;

		var tf = TimeSpan.FromDays(1);
		var eiProv = ServicesRegistry.EnsureGetExchangeInfoProvider();
		var cbProv = new CandleBuilderProvider(eiProv);

		var buildableStorage = cbProv.GetCandleMessageBuildableStorage(reg, secId, tf, null, format);

		var range = await buildableStorage.GetRangeAsync(_regressionFrom, _regressionTo, token);

		range.Min.AssertEqual(range.Min.Date);
		range.Max.AssertEqual(range.Max.Date);
	}
}
