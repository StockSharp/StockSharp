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

	#region Mock-based tests

	/// <summary>
	/// Mock implementation of IReportSource for testing.
	/// </summary>
	private class MockReportSource : IReportSource
	{
		public string Name { get; set; } = "TestStrategy";
		public TimeSpan TotalWorkingTime { get; set; } = TimeSpan.FromHours(2);
		public decimal? Commission { get; set; } = 10.5m;
		public decimal Position { get; set; } = 100m;
		public decimal PnL { get; set; } = 500m;
		public decimal? Slippage { get; set; } = 2.5m;
		public TimeSpan? Latency { get; set; } = TimeSpan.FromMilliseconds(50);

		public List<ReportOrder> OrdersList { get; } = [];
		public List<ReportTrade> TradesList { get; } = [];
		public List<(string Name, object Value)> StatisticsList { get; } = [];
		public List<(string Name, object Value)> ParametersList { get; } = [];

		public IEnumerable<ReportOrder> Orders => OrdersList;
		public IEnumerable<ReportTrade> OwnTrades => TradesList;
		public IEnumerable<(string Name, object Value)> StatisticParameters => StatisticsList;
		public IEnumerable<(string Name, object Value)> Parameters => ParametersList;
	}

	[TestMethod]
	public async Task JsonReportGenerator_GeneratesValidJson()
	{
		var source = new MockReportSource
		{
			Name = "TestStrategy",
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
		var source = new MockReportSource
		{
			Name = "CsvTestStrategy",
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
		var source = new MockReportSource
		{
			Name = "XmlTestStrategy",
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
		var source = new MockReportSource { Name = "OrderTest" };
		source.OrdersList.Add(new ReportOrder(
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
		var source = new MockReportSource { Name = "NoOrderTest" };
		source.OrdersList.Add(new ReportOrder(
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

	private static MockReportSource CreateMockSourceWithData()
	{
		var source = new MockReportSource
		{
			Name = "ExcelTestStrategy",
			PnL = 5000m,
			Position = 100m,
			Commission = 50m,
			TotalWorkingTime = TimeSpan.FromHours(8),
		};

		source.ParametersList.Add(("Symbol", "BTCUSD"));
		source.ParametersList.Add(("TimeFrame", "1m"));
		source.ParametersList.Add(("InitialCapital", 10000m));

		source.StatisticsList.Add(("Win Rate", 0.65m));
		source.StatisticsList.Add(("Sharpe Ratio", 1.5m));
		source.StatisticsList.Add(("Max Drawdown", -0.12m));

		var baseTime = new DateTime(2024, 3, 1, 10, 0, 0, DateTimeKind.Utc);
		source.TradesList.Add(new ReportTrade(
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
		source.TradesList.Add(new ReportTrade(
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

		source.OrdersList.Add(new ReportOrder(
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
		source.OrdersList.Add(new ReportOrder(
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
