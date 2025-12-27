namespace StockSharp.Tests;

using System.Text;

using StockSharp.Algo.Statistics;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Reporting;

/// <summary>
/// Tests for report generators using IReportSource mock.
/// Demonstrates that reports are decoupled from Strategy class.
/// </summary>
[TestClass]
public class ReportGeneratorTests : BaseTestClass
{
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
		public IEnumerable<ReportTrade> MyTrades => TradesList;
		public IEnumerable<(string Name, object Value)> StatisticParameters => StatisticsList;
		public IEnumerable<(string Name, object Value)> Parameters => ParametersList;
	}

	[TestMethod]
	public async Task JsonReportGenerator_GeneratesValidJson()
	{
		// Arrange
		var source = new MockReportSource
		{
			Name = "TestStrategy",
			PnL = 1000m,
			Position = 50m,
			Commission = 25m
		};

		var generator = new JsonReportGenerator();
		using var stream = new MemoryStream();

		// Act
		await generator.Generate(source, stream, CancellationToken.None);

		// Assert
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
		// Arrange
		var source = new MockReportSource
		{
			Name = "CsvTestStrategy",
			PnL = 2000m,
			Position = 75m,
		};

		var generator = new CsvReportGenerator();
		using var stream = new MemoryStream();

		// Act
		await generator.Generate(source, stream, CancellationToken.None);

		// Assert
		stream.Position = 0;
		var csv = new StreamReader(stream).ReadToEnd();

		csv.AssertContains("CsvTestStrategy");
		csv.AssertContains("2000");
	}

	[TestMethod]
	public async Task XmlReportGenerator_GeneratesValidXml()
	{
		// Arrange
		var source = new MockReportSource
		{
			Name = "XmlTestStrategy",
			PnL = 3000m,
		};

		var generator = new XmlReportGenerator();
		using var stream = new MemoryStream();

		// Act
		await generator.Generate(source, stream, CancellationToken.None);

		// Assert
		stream.Position = 0;
		var xml = new StreamReader(stream).ReadToEnd();

		xml.AssertContains("<strategy");
		xml.AssertContains("name=\"XmlTestStrategy\"");
		xml.AssertContains("PnL=\"3000\"");
	}

	[TestMethod]
	public async Task JsonReportGenerator_IncludesOrders_WhenEnabled()
	{
		// Arrange
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

		// Act
		await generator.Generate(source, stream, CancellationToken.None);

		// Assert
		stream.Position = 0;
		var json = new StreamReader(stream).ReadToEnd();

		json.AssertContains("\"orders\"");
		json.AssertContains("12345");
	}

	[TestMethod]
	public async Task JsonReportGenerator_ExcludesOrders_WhenDisabled()
	{
		// Arrange
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

		// Act
		await generator.Generate(source, stream, CancellationToken.None);

		// Assert
		stream.Position = 0;
		var json = new StreamReader(stream).ReadToEnd();

		// Should not contain orders section
		Assert.IsFalse(json.Contains("\"orders\""), "Orders section should not be present when IncludeOrders=false");
	}
}
