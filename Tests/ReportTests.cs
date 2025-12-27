namespace StockSharp.Tests;

using StockSharp.Algo.Reporting;

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
	[DataRow("csv")]
	[DataRow("json")]
	[DataRow("xml")]
	[DataRow("xlsx")]
	public async Task Reports(string format)
	{
		var token = CancellationToken;

		var strategy = CreateTestStrategy();

		IReportGenerator generator = format switch
		{
			"csv" => new CsvReportGenerator(),
			"json" => new JsonReportGenerator(),
			"xml" => new XmlReportGenerator(),
			"xlsx" => new ExcelReportGenerator(ServicesRegistry.ExcelProvider),
			_ => throw new ArgumentException($"Unknown format: {format}")
		};

		using var stream = new MemoryStream();

		await generator.Generate(strategy, stream, token);

		(stream.Length > 0).AssertTrue($"Report {format} should write data");
	}

	[TestMethod]
	public async Task ExcelReportWithTemplate()
	{
		var token = CancellationToken;

		var strategy = CreateTestStrategy();

		// Create a template xlsx in memory
		using var templateStream = new MemoryStream();
		using (var worker = ServicesRegistry.ExcelProvider.CreateNew(templateStream))
		{
			worker
				.AddSheet()
				.RenameSheet("Summary")
				.SetCell(0, 0, "Template Header");
		}
		templateStream.Position = 0;

		var generator = new ExcelReportGenerator(ServicesRegistry.ExcelProvider, templateStream);

		using var outputStream = new MemoryStream();

		await generator.Generate(strategy, outputStream, token);

		(outputStream.Length > 0).AssertTrue("Excel report with template should write data");
	}
}
