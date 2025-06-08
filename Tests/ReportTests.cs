namespace StockSharp.Tests;

using StockSharp.Algo.Strategies.Reporting;

[TestClass]
public class ReportTests
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
				Time = DateTimeOffset.UtcNow.AddMinutes(-i),
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
				Trade = new() { Id = 2000 + i, Price = 100 + i, Volume = 1, ServerTime = DateTimeOffset.UtcNow.AddMinutes(-i) },
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
		var strategy = CreateTestStrategy();
		var fileName = Path.Combine(Helper.TempFolder, $"test_report.{format}");
		Directory.CreateDirectory(Helper.TempFolder);

		IReportGenerator generator = format switch
		{
			"csv" => new CsvReportGenerator(),
			"json" => new JsonReportGenerator(),
			"xml" => new XmlReportGenerator(),
			"xlsx" => new ExcelReportGenerator(ServicesRegistry.ExcelProvider),
			_ => throw new ArgumentException($"Unknown format: {format}")
		};

		await generator.Generate(strategy, fileName, CancellationToken.None);

		File.Exists(fileName).AssertTrue();
		var content = File.ReadAllText(fileName);
		content.IsEmptyOrWhiteSpace().AssertFalse();
	}
}
