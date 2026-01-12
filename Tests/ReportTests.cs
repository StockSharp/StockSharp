namespace StockSharp.Tests;

using Ecng.Excel;

using StockSharp.Reporting;

[TestClass]
public class ReportTests : BaseTestClass
{
	private static Strategy CreateTestStrategy()
	{
		var strategy = new Strategy
		{
			Name = "TestStrategy",
			Portfolio = Helper.CreatePortfolio(),
			Security = Helper.CreateSecurity(),
		};

		var orders = new List<Order>();

		for (var i = 0; i < 3; i++)
		{
			var order = new Order
			{
				Id = i + 1,
				TransactionId = 1000 + i,
				Side = i % 2 == 0 ? Sides.Buy : Sides.Sell,
				Time = DateTime.UtcNow.AddMinutes(-i),
				Price = 100 + i,
				State = OrderStates.Active,
				Balance = 10 - i,
				Volume = 10,
				Type = OrderTypes.Limit,
				Comment = $"Order {i + 1}",
				Security = strategy.Security,
				Portfolio = strategy.Portfolio,
			};
			orders.Add(order);
		}

		for (var i = 0; i < 2; i++)
		{
			var order = orders[i];
			var trade = new MyTrade
			{
				Order = order,
				Trade = new ExecutionMessage
				{
					DataTypeEx = DataType.Ticks,
					TradeId = 2000 + i,
					TradePrice = 100 + i,
					TradeVolume = 1,
					ServerTime = DateTime.UtcNow.AddMinutes(-i),
					SecurityId = strategy.Security.ToSecurityId(),
				},
				Slippage = 0.1m * i,
				PnL = 1.5m * i,
				Position = i
			};
			strategy.TryAddMyTrade(trade);
		}

		return strategy;
	}

	[TestMethod]
	[DataRow(FileExts.Csv)]
	[DataRow(FileExts.Json)]
	[DataRow(FileExts.Xml)]
	[DataRow(FileExts.Xlsx)]
	public async Task Reports(string format)
	{
		var token = CancellationToken;

		var strategy = CreateTestStrategy();

		IReportGenerator generator = format switch
		{
			FileExts.Csv => new CsvReportGenerator(),
			FileExts.Json => new JsonReportGenerator(),
			FileExts.Xml => new XmlReportGenerator(),
			FileExts.Xlsx => new ExcelReportGenerator(ServicesRegistry.ExcelProvider),
			_ => throw new ArgumentException($"Unknown format: {format}")
		};

		using var stream = new MemoryStream();

		await generator.Generate(strategy, stream, token);

		(stream.Length > 0).AssertTrue($"Report {format} should write data");
	}

	#region ReportSource tests

	[TestMethod]
	public async Task JsonReportGenerator_GeneratesValidJson()
	{
		var source = new ReportSource("TestStrategy")
		{
			PnL = 1000m,
			Position = 50m,
			Commission = 25m
		};

		var generator = new JsonReportGenerator();
		using var stream = new MemoryStream();

		await generator.Generate(source, stream, CancellationToken);

		stream.Position = 0;
		var json = new StreamReader(stream).ReadToEnd();

		json.AssertContains("\"name\"");
		json.AssertContains("TestStrategy");
		json.AssertContains("\"PnL\"");
		json.AssertContains("1000");
	}

	[TestMethod]
	public async Task CsvReportGenerator_GeneratesValidCsv()
	{
		var source = new ReportSource("CsvTestStrategy")
		{
			PnL = 2000m,
			Position = 75m,
		};

		var generator = new CsvReportGenerator();
		using var stream = new MemoryStream();

		await generator.Generate(source, stream, CancellationToken);

		stream.Position = 0;
		var csv = new StreamReader(stream).ReadToEnd();

		csv.AssertContains("CsvTestStrategy");
		csv.AssertContains("2000");
	}

	[TestMethod]
	public async Task XmlReportGenerator_GeneratesValidXml()
	{
		var source = new ReportSource("XmlTestStrategy")
		{
			PnL = 3000m,
		};

		var generator = new XmlReportGenerator();
		using var stream = new MemoryStream();

		await generator.Generate(source, stream, CancellationToken);

		stream.Position = 0;
		var xml = new StreamReader(stream).ReadToEnd();

		xml.AssertContains("<strategy");
		xml.AssertContains("name=\"XmlTestStrategy\"");
		xml.AssertContains("PnL=\"3000\"");
	}

	[TestMethod]
	public async Task JsonReportGenerator_IncludesOrders_WhenEnabled()
	{
		var source = new ReportSource("OrderTest");
		source.AddOrder(new ReportOrder(
			Id: 12345,
			TransactionId: 100,
			Side: Sides.Buy,
			Time: DateTime.UtcNow,
			Price: 150m,
			State: OrderStates.Done,
			Balance: 0m,
			Volume: 10m,
			Type: OrderTypes.Limit
		));

		var generator = new JsonReportGenerator { IncludeOrders = true };
		using var stream = new MemoryStream();

		await generator.Generate(source, stream, CancellationToken);

		stream.Position = 0;
		var json = new StreamReader(stream).ReadToEnd();

		json.AssertContains("\"orders\"");
		json.AssertContains("12345");
	}

	[TestMethod]
	public async Task JsonReportGenerator_ExcludesOrders_WhenDisabled()
	{
		var source = new ReportSource("NoOrderTest");
		source.AddOrder(new ReportOrder(
			Id: 99999,
			TransactionId: 1,
			Side: Sides.Buy,
			Time: DateTime.UtcNow,
			Price: 100m,
			State: null,
			Balance: null,
			Volume: null,
			Type: null
		));

		var generator = new JsonReportGenerator { IncludeOrders = false };
		using var stream = new MemoryStream();

		await generator.Generate(source, stream, CancellationToken);

		stream.Position = 0;
		var json = new StreamReader(stream).ReadToEnd();

		IsFalse(json.Contains("\"orders\""), "Orders section should not be present when IncludeOrders=false");
	}

	[TestMethod]
	public void ReportSource_AddParameter_Works()
	{
		var source = new ReportSource("Test")
			.AddParameter("Param1", 100)
			.AddParameter("Param2", "Value");

		var parameters = source.Parameters.ToList();
		parameters.Count.AssertEqual(2);
		parameters[0].Name.AssertEqual("Param1");
		parameters[0].Value.AssertEqual(100);
		parameters[1].Name.AssertEqual("Param2");
		parameters[1].Value.AssertEqual("Value");
	}

	[TestMethod]
	public void ReportSource_AddStatisticParameter_Works()
	{
		var source = new ReportSource("Test")
			.AddStatisticParameter("WinRate", 0.65m)
			.AddStatisticParameter("Sharpe", 1.5m);

		var stats = source.StatisticParameters.ToList();
		stats.Count.AssertEqual(2);
		stats[0].Name.AssertEqual("WinRate");
		stats[0].Value.AssertEqual(0.65m);
	}

	[TestMethod]
	public void ReportSource_AddOrder_Works()
	{
		var source = new ReportSource("Test");
		var time = DateTime.UtcNow;

		source.AddOrder(1, 100, Sides.Buy, time, 50000m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);

		source.OrdersCount.AssertEqual(1);
		var order = source.Orders.First();
		order.Id.AssertEqual(1);
		order.TransactionId.AssertEqual(100);
		order.Side.AssertEqual(Sides.Buy);
		order.Price.AssertEqual(50000m);
	}

	[TestMethod]
	public void ReportSource_AddTrade_Works()
	{
		var source = new ReportSource("Test");
		var time = DateTime.UtcNow;

		source.AddTrade(1001, 100, time, 50000m, 50000m, 1m, Sides.Buy, 1, 0m, 100m, 1m);

		source.TradesCount.AssertEqual(1);
		var trade = source.OwnTrades.First();
		trade.TradeId.AssertEqual(1001);
		trade.TradePrice.AssertEqual(50000m);
		trade.PnL.AssertEqual(100m);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_ByHour_Works()
	{
		var source = new ReportSource("Test");
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		// Add 5 buy orders in the same hour
		for (var i = 0; i < 5; i++)
		{
			source.AddOrder(i, i, Sides.Buy, baseTime.AddMinutes(i * 10), 100m + i, OrderStates.Done, 0m, 10m, OrderTypes.Limit);
		}

		// Add 3 sell orders in the same hour
		for (var i = 0; i < 3; i++)
		{
			source.AddOrder(100 + i, 100 + i, Sides.Sell, baseTime.AddMinutes(i * 15), 105m + i, OrderStates.Done, 0m, 5m, OrderTypes.Limit);
		}

		source.OrdersCount.AssertEqual(8);

		source.AggregateOrders(AggregationModes.ByHour);

		// Should be aggregated to 2 orders (1 buy, 1 sell)
		source.OrdersCount.AssertEqual(2);

		var orders = source.Orders.ToList();
		var buyOrder = orders.First(o => o.Side == Sides.Buy);
		var sellOrder = orders.First(o => o.Side == Sides.Sell);

		buyOrder.Volume.AssertEqual(50m); // 5 * 10
		sellOrder.Volume.AssertEqual(15m); // 3 * 5
	}

	[TestMethod]
	public void ReportSource_AggregateTrades_ByHour_Works()
	{
		var source = new ReportSource("Test");
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		// Add 5 buy trades in the same hour
		for (var i = 0; i < 5; i++)
		{
			source.AddTrade(i, i, baseTime.AddMinutes(i * 10), 100m, 100m, 1m, Sides.Buy, i, 0.1m, 10m, i);
		}

		source.TradesCount.AssertEqual(5);

		source.AggregateTrades(AggregationModes.ByHour);

		// Should be aggregated to 1 trade
		source.TradesCount.AssertEqual(1);

		var trade = source.OwnTrades.First();
		trade.Volume.AssertEqual(5m); // 5 * 1
		trade.PnL.AssertEqual(50m); // 5 * 10
		trade.Slippage.AssertEqual(0.5m); // 5 * 0.1
	}

	[TestMethod]
	public void ReportSource_AggregateTrades_ByDay_Works()
	{
		var source = new ReportSource("Test");
		var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		// Add trades across different hours but same day
		source.AddTrade(1, 1, baseTime.AddHours(1), 100m, 100m, 1m, Sides.Buy, 1, null, 10m, 1m);
		source.AddTrade(2, 2, baseTime.AddHours(5), 101m, 101m, 2m, Sides.Buy, 2, null, 20m, 3m);
		source.AddTrade(3, 3, baseTime.AddHours(10), 102m, 102m, 3m, Sides.Buy, 3, null, 30m, 6m);

		source.TradesCount.AssertEqual(3);

		source.AggregateTrades(AggregationModes.ByDay);

		source.TradesCount.AssertEqual(1);

		var trade = source.OwnTrades.First();
		trade.Volume.AssertEqual(6m); // 1 + 2 + 3
		trade.PnL.AssertEqual(60m); // 10 + 20 + 30
		trade.Position.AssertEqual(6m); // last position
	}

	[TestMethod]
	public void ReportSource_AutoAggregation_TriggersWhenThresholdExceeded()
	{
		var source = new ReportSource("Test")
		{
			MaxOrdersBeforeAggregation = 10,
			AutoAggregationMode = AggregationModes.ByHour
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		// Add 15 orders in the same hour - should trigger auto-aggregation
		for (var i = 0; i < 15; i++)
		{
			source.AddOrder(i, i, Sides.Buy, baseTime.AddMinutes(i), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		// After auto-aggregation, should have 1 aggregated order
		source.OrdersCount.AssertEqual(1);
		source.Orders.First().Volume.AssertEqual(15m);
	}

	[TestMethod]
	public void ReportSource_NoAutoAggregation_WhenDisabled()
	{
		var source = new ReportSource("Test")
		{
			MaxOrdersBeforeAggregation = 0 // Disabled
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		for (var i = 0; i < 100; i++)
		{
			source.AddOrder(i, i, Sides.Buy, baseTime.AddMinutes(i), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		source.OrdersCount.AssertEqual(100);
	}

	[TestMethod]
	public void ReportSource_Clear_RemovesAllData()
	{
		var source = new ReportSource("Test")
			.AddParameter("P1", 1)
			.AddStatisticParameter("S1", 2);

		source.AddOrder(1, 1, Sides.Buy, DateTime.UtcNow, 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddTrade(1, 1, DateTime.UtcNow, 100m, 100m, 1m, Sides.Buy, 1, null, null, null);

		source.Clear();

		source.OrdersCount.AssertEqual(0);
		source.TradesCount.AssertEqual(0);
		source.Parameters.Count().AssertEqual(0);
		source.StatisticParameters.Count().AssertEqual(0);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_PreservesWeightedAveragePrice()
	{
		var source = new ReportSource("Test");
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);

		// Order 1: 100 @ 10 volume = 1000 total cost
		// Order 2: 200 @ 20 volume = 4000 total cost
		// Weighted avg = 5000 / 30 = 166.67
		source.AddOrder(1, 1, Sides.Buy, time, 100m, OrderStates.Done, 0m, 10m, OrderTypes.Limit);
		source.AddOrder(2, 2, Sides.Buy, time.AddMinutes(5), 200m, OrderStates.Done, 0m, 20m, OrderTypes.Limit);

		source.AggregateOrders(AggregationModes.ByHour);

		var order = source.Orders.First();
		order.Volume.AssertEqual(30m);
		// Weighted average: (100*10 + 200*20) / 30 = 5000/30 = 166.666...
		IsTrue(order.Price > 166m && order.Price < 167m, $"Expected weighted average ~166.67, got {order.Price}");
	}

	#endregion

	#region Excel Report Generator Tests

	[TestMethod]
	public void ExcelReportGenerator_GetTemplateStream_ReturnsStream()
	{
		var template = ExcelReportGenerator.GetTemplate();

		template.AssertNotNull("Template stream should not be null");
		IsTrue(template.Length > 0, "Template stream should have content");
	}

	[TestMethod]
	public async Task ExcelReportGenerator_WithTemplate_GeneratesValidXlsx()
	{
		var source = CreateMockSourceWithData();
		var template = ExcelReportGenerator.GetTemplate();
		template.AssertNotNull("Template must be available for this test");

		var provider = new OpenXmlExcelWorkerProvider();
		var generator = new ExcelReportGenerator(provider, template)
		{
			IncludeTrades = true,
			IncludeOrders = true
		};

		using var outputStream = new MemoryStream();

		await generator.Generate(source, outputStream, CancellationToken);

		IsTrue(outputStream.Length > 0, "Generated file should have content");

		outputStream.Position = 0;
		using var worker = provider.OpenExist(outputStream);

		var sheetNames = worker.GetSheetNames().ToList();
		sheetNames.AssertContains("Params", "Should have Params sheet");
		sheetNames.AssertContains("Trades", "Should have Trades sheet");
		sheetNames.AssertContains("Orders", "Should have Orders sheet");
	}

	[TestMethod]
	public async Task ExcelReportGenerator_WithoutTemplate_GeneratesValidXlsx()
	{
		var source = CreateMockSourceWithData();

		var provider = new OpenXmlExcelWorkerProvider();
		var generator = new ExcelReportGenerator(provider)
		{
			IncludeTrades = true,
			IncludeOrders = true
		};

		using var outputStream = new MemoryStream();

		await generator.Generate(source, outputStream, CancellationToken);

		IsTrue(outputStream.Length > 0, "Generated file should have content");

		outputStream.Position = 0;
		using var worker = provider.OpenExist(outputStream);

		var sheetNames = worker.GetSheetNames().ToList();
		sheetNames.AssertContains("Params", "Should have Params sheet");
		sheetNames.AssertContains("Trades", "Should have Trades sheet");
		sheetNames.AssertContains("Orders", "Should have Orders sheet");
		sheetNames.AssertContains("Equity", "Should have Equity sheet");
	}

	[TestMethod]
	public async Task ExcelReportGenerator_WithoutTemplate_ContainsCorrectData()
	{
		var source = CreateMockSourceWithData();

		var provider = new OpenXmlExcelWorkerProvider();
		var generator = new ExcelReportGenerator(provider)
		{
			IncludeTrades = true,
			IncludeOrders = true
		};

		using var outputStream = new MemoryStream();

		await generator.Generate(source, outputStream, CancellationToken);

		outputStream.Position = 0;
		using var worker = provider.OpenExist(outputStream);

		worker.SwitchSheet("Params");
		var strategyName = worker.GetCell<string>(1, 1);
		strategyName.AssertEqual("ExcelTestStrategy");

		worker.SwitchSheet("Trades");
		var rowCount = worker.GetRowsCount();
		IsTrue(rowCount > 1, "Trades sheet should have header + data rows");
	}

	[TestMethod]
	public async Task ExcelReportGenerator_WithoutTemplate_ExcludesTrades_WhenDisabled()
	{
		var source = CreateMockSourceWithData();

		var provider = new OpenXmlExcelWorkerProvider();
		var generator = new ExcelReportGenerator(provider)
		{
			IncludeTrades = false,
			IncludeOrders = false
		};

		using var outputStream = new MemoryStream();

		await generator.Generate(source, outputStream, CancellationToken);

		outputStream.Position = 0;
		using var worker = provider.OpenExist(outputStream);

		var sheetNames = worker.GetSheetNames().ToList();
		IsFalse(sheetNames.Contains("Trades"), "Trades sheet should not exist when IncludeTrades=false");
		IsFalse(sheetNames.Contains("Orders"), "Orders sheet should not exist when IncludeOrders=false");
	}

	[TestMethod]
	public async Task ExcelReportGenerator_WithTemplate_ContainsTradeData()
	{
		var source = CreateMockSourceWithData();
		var template = ExcelReportGenerator.GetTemplate();
		template.AssertNotNull("Template must be available for this test");

		var provider = new OpenXmlExcelWorkerProvider();
		var generator = new ExcelReportGenerator(provider, template)
		{
			IncludeTrades = true
		};

		using var outputStream = new MemoryStream();

		await generator.Generate(source, outputStream, CancellationToken);

		outputStream.Position = 0;
		using var worker = provider.OpenExist(outputStream);

		worker.SwitchSheet("Trades");
		var rowCount = worker.GetRowsCount();
		IsTrue(rowCount > 0, "Trades sheet should have data");
	}

	private static ReportSource CreateMockSourceWithData()
	{
		var source = new ReportSource("ExcelTestStrategy")
		{
			PnL = 5000m,
			Position = 100m,
			Commission = 50m,
			TotalWorkingTime = TimeSpan.FromHours(8),
		};

		source.AddParameter("Symbol", "BTCUSD");
		source.AddParameter("TimeFrame", "1m");
		source.AddParameter("InitialCapital", 10000m);

		source.AddStatisticParameter("Win Rate", 0.65m);
		source.AddStatisticParameter("Sharpe Ratio", 1.5m);
		source.AddStatisticParameter("Max Drawdown", -0.12m);

		var baseTime = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
		source.AddTrade(new ReportTrade(
			TradeId: 1001,
			OrderTransactionId: 100,
			Time: baseTime,
			TradePrice: 50000m,
			OrderPrice: 50000m,
			Volume: 0.1m,
			Side: Sides.Buy,
			OrderId: 1,
			Slippage: 0m,
			PnL: 100m,
			Position: 0.1m
		));
		source.AddTrade(new ReportTrade(
			TradeId: 1002,
			OrderTransactionId: 101,
			Time: baseTime.AddHours(1),
			TradePrice: 50500m,
			OrderPrice: 50500m,
			Volume: 0.1m,
			Side: Sides.Sell,
			OrderId: 2,
			Slippage: 0m,
			PnL: 50m,
			Position: 0m
		));

		source.AddOrder(new ReportOrder(
			Id: 1,
			TransactionId: 100,
			Side: Sides.Buy,
			Time: baseTime,
			Price: 50000m,
			State: OrderStates.Done,
			Balance: 0m,
			Volume: 0.1m,
			Type: OrderTypes.Limit
		));
		source.AddOrder(new ReportOrder(
			Id: 2,
			TransactionId: 101,
			Side: Sides.Sell,
			Time: baseTime.AddHours(1),
			Price: 50500m,
			State: OrderStates.Done,
			Balance: 0m,
			Volume: 0.1m,
			Type: OrderTypes.Limit
		));

		return source;
	}

	#endregion
}
