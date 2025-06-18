namespace StockSharp.Tests;

using StockSharp.Algo.Export;
using StockSharp.Algo.Import;

[TestClass]
public class ImportTests
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

	private static void Import<TValue>(DataType dataType, IEnumerable<TValue> values, FieldMapping[] fields)
		where TValue : class
	{
		var arr = values.ToArray();

		var template = GetTemplate(dataType);

		for (var i = 0; i < fields.Length; i++)
			fields[i].Order = i;

		var fileName = Helper.GetSubTemp($"{dataType.DataTypeToFileName()}_import.csv");

		new TextExporter(dataType, _ => false, fileName, template, null).Export(arr);

		var parser = new CsvParser(dataType, fields)
		{
			ColumnSeparator = ";"
		};
		var msgs = parser.Parse(fileName, () => false).ToArray();

		msgs.Length.AssertEqual(arr.Length);
	}

	[TestMethod]
	public void Ticks()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Ticks).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "TradeId"),
			allFields.First(f => f.Name == "TradePrice"),
			allFields.First(f => f.Name == "TradeVolume"),
			allFields.First(f => f.Name == "OriginSide"),
		};
		Import(DataType.Ticks, security.RandomTicks(100, true), fields);
	}

	//[TestMethod]
	//public void Depths()
	//{
	//	var security = Helper.CreateStorageSecurity();
	//	var allFields = FieldMappingRegistry.CreateFields(DataType.MarketDepth).ToArray();
	//	var fields = new[]
	//	{
	//		allFields.First(f => f.Name == "ServerTime.Date"),
	//		allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
	//		allFields.First(f => f.Name == "Quote.Price"),
	//		allFields.First(f => f.Name == "Quote.Volume"),
	//		allFields.First(f => f.Name == "Side"),
	//	};
	//	Import(DataType.MarketDepth, security.RandomDepths(100, ordersCount: true), fields);
	//}

	[TestMethod]
	public void OrderLog()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.OrderLog).ToArray();
		var fields = new[]
		{
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
		Import(DataType.OrderLog, security.RandomOrderLog(100), fields);
	}

	[TestMethod]
	public void Positions()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.PositionChanges).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "SecurityId.SecurityCode"),
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Changes[CurrentValue]"),
			allFields.First(f => f.Name == "Changes[BlockedValue]"),
			allFields.First(f => f.Name == "Changes[RealizedPnL]"),
			allFields.First(f => f.Name == "Changes[UnrealizedPnL]"),
			allFields.First(f => f.Name == "Changes[AveragePrice]"),
			allFields.First(f => f.Name == "Changes[Commission]"),
		};
		Import(DataType.PositionChanges, security.RandomPositionChanges(100), fields);
	}

	[TestMethod]
	public void News()
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
		Import(DataType.News, Helper.RandomNews(), fields);
	}

	[TestMethod]
	public void Level1()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Level1).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "Changes[BestBidPrice]"),
			allFields.First(f => f.Name == "Changes[BestBidVolume]"),
			allFields.First(f => f.Name == "Changes[BestAskPrice]"),
			allFields.First(f => f.Name == "Changes[BestAskVolume]"),
			allFields.First(f => f.Name == "Changes[LastTradePrice]"),
			allFields.First(f => f.Name == "Changes[LastTradeVolume]"),
		};
		Import(DataType.Level1, security.RandomLevel1(count: 100), fields);
	}

	[TestMethod]
	public void Candles()
	{
		var security = Helper.CreateStorageSecurity();
		var candles = CandleTests.GenerateCandles(security.RandomTicks(100, true), security, CandleTests.PriceRange.Pips(security), CandleTests.TotalTicks, CandleTests.TimeFrame, CandleTests.VolumeRange, CandleTests.BoxSize, CandleTests.PnF(security), true);

		foreach (var group in candles.GroupBy(c => (type: c.GetType(), arg: c.Arg)))
		{
			var dataType = DataType.Create(group.Key.type, group.Key.arg);
			var allFields = FieldMappingRegistry.CreateFields(dataType).ToArray();
			var fields = new[]
			{
				allFields.First(f => f.Name == "OpenTime.Date"),
				allFields.First(f => f.Name == "OpenTime.TimeOfDay"),
				allFields.First(f => f.Name == "OpenPrice"),
				allFields.First(f => f.Name == "HighPrice"),
				allFields.First(f => f.Name == "LowPrice"),
				allFields.First(f => f.Name == "ClosePrice"),
				allFields.First(f => f.Name == "TotalVolume"),
			};
			Import(dataType, group.ToArray(), fields);
		}
	}

	[TestMethod]
	public void BoardState()
	{
		var allFields = FieldMappingRegistry.CreateFields(DataType.BoardState).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "ServerTime.Date"),
			allFields.First(f => f.Name == "ServerTime.TimeOfDay"),
			allFields.First(f => f.Name == "BoardCode"),
			allFields.First(f => f.Name == "State"),
		};
		Import(DataType.BoardState, Helper.RandomBoardStates(), fields);
	}

	[TestMethod]
	public void Transactions()
	{
		var security = Helper.CreateStorageSecurity();
		var allFields = FieldMappingRegistry.CreateFields(DataType.Transactions).ToArray();

		var fields = new[]
		{
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
		Import(DataType.Transactions, security.RandomTransactions(10), fields);
	}

	[TestMethod]
	public void Securities()
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
		Import(DataType.Securities, Helper.RandomSecurities(10), fields);
	}

	[TestMethod]
	public void Boards()
	{
		var allFields = FieldMappingRegistry.CreateFields(DataType.Board).ToArray();
		var fields = new[]
		{
			allFields.First(f => f.Name == "ExchangeCode"),
			allFields.First(f => f.Name == "Code"),
			//allFields.First(f => f.Name == "ExpiryTime"),
			//allFields.First(f => f.Name == "TimeZone"),
		};
		Import(DataType.Board, Helper.RandomBoards(10), fields);
	}
}