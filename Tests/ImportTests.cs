namespace StockSharp.Tests;

using StockSharp.Algo.Export;
using StockSharp.Algo.Import;

[TestClass]
public class ImportTests
{
    private static string BuildTemplate(FieldMapping f)
        => "{" + f.Name + (f.Format.IsEmpty() ? string.Empty : $":default:{f.Format}") + "}";

    private static void Import<TValue>(DataType dataType, IEnumerable<TValue> values)
        where TValue : class
    {
        var arr = values.ToArray();

        var fields = FieldMappingRegistry.CreateFields(dataType).ToArray();
        for (var i = 0; i < fields.Length; i++)
            fields[i].Order = i;

        var template = fields.Select(BuildTemplate).JoinDotComma();
        var fileName = Helper.GetSubTemp($"{dataType.MessageType.Name}_import.csv");

        new TextExporter(dataType, _ => false, fileName, template, null).Export(arr);

        var storage = Helper.GetStorage(Helper.GetSubTemp($"{dataType.MessageType.Name}_storage"));

        var importer = new CsvImporter(dataType, fields, new InMemorySecurityStorage(), ServicesRegistry.EnsureGetExchangeInfoProvider(), storage.DefaultDrive, StorageFormats.Csv);
        var (count, _) = importer.Import(fileName, null, () => false);

        count.AssertEqual(arr.Length);
    }

    [TestMethod]
    public void Ticks()
    {
        var security = Helper.CreateStorageSecurity();
        Import(DataType.Ticks, security.RandomTicks(100, true));
    }

    [TestMethod]
    public void Depths()
    {
        var security = Helper.CreateStorageSecurity();
        Import(DataType.MarketDepth, security.RandomDepths(100, ordersCount: true));
    }

    [TestMethod]
    public void OrderLog()
    {
        var security = Helper.CreateStorageSecurity();
        Import(DataType.OrderLog, security.RandomOrderLog(100));
    }

    [TestMethod]
    public void Positions()
    {
        var security = Helper.CreateStorageSecurity();
        Import(DataType.PositionChanges, security.RandomPositionChanges(100));
    }

    [TestMethod]
    public void News()
    {
        Import(DataType.News, Helper.RandomNews());
    }

    [TestMethod]
    public void Level1()
    {
        var security = Helper.CreateStorageSecurity();
        Import(DataType.Level1, security.RandomLevel1(count: 100));
    }

    [TestMethod]
    public void Candles()
    {
        var security = Helper.CreateStorageSecurity();
        var candles = CandleTests.GenerateCandles(security.RandomTicks(100, true), security, CandleTests.PriceRange.Pips(security), CandleTests.TotalTicks, CandleTests.TimeFrame, CandleTests.VolumeRange, CandleTests.BoxSize, CandleTests.PnF(security), true);

        foreach (var group in candles.GroupBy(c => (type: c.GetType(), arg: c.Arg)))
        {
            Import(DataType.Create(group.Key.type, group.Key.arg), group.ToArray());
        }
    }
}