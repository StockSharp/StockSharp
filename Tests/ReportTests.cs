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

	private static ReportSource CreateDeterministicSource()
	{
		var source = new ReportSource
		{
			Name = "TestStrategy",
			PnL = 5000m,
			Position = 100m,
			Commission = 25m,
			Slippage = 1.5m,
			Latency = TimeSpan.FromMilliseconds(50),
			TotalWorkingTime = TimeSpan.FromHours(8),
		};

		// Diverse parameter types
		source.AddParameter("Symbol", "BTCUSD");
		source.AddParameter("TimeFrame", TimeSpan.FromMinutes(5));
		source.AddParameter("StartDate", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
		source.AddParameter("Volume", 100m);
		source.AddParameter("IsLong", true);
		source.AddParameter("MaxPositions", 5);

		// Diverse statistic types
		source.AddStatisticParameter("WinRate", 0.65m);
		source.AddStatisticParameter("MaxDrawdown", -500m);
		source.AddStatisticParameter("TradesCount", 42);
		source.AddStatisticParameter("AverageTradeTime", TimeSpan.FromMinutes(15));
		source.AddStatisticParameter("BestTradeDate", new DateTime(2024, 1, 15, 14, 30, 0, DateTimeKind.Utc));
		source.AddStatisticParameter("SharpeRatio", 1.85m);

		var baseTime = new DateTime(2024, 1, 15, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "BTCUSD", BoardCode = "CRYPTO" };

		source.AddOrder(new ReportOrder(
			Id: 1, TransactionId: 100, SecurityId: securityId, Side: Sides.Buy, Time: baseTime,
			Price: 50000m, State: OrderStates.Done, Balance: 0m, Volume: 1m, Type: OrderTypes.Limit));
		source.AddOrder(new ReportOrder(
			Id: 2, TransactionId: 101, SecurityId: securityId, Side: Sides.Sell, Time: baseTime.AddHours(1),
			Price: 50500m, State: OrderStates.Done, Balance: 0m, Volume: 1m, Type: OrderTypes.Limit));

		source.AddTrade(new ReportTrade(
			TradeId: 1001, OrderTransactionId: 100, SecurityId: securityId, Time: baseTime,
			TradePrice: 50000m, OrderPrice: 50000m, Volume: 1m, Side: Sides.Buy,
			OrderId: 1, Slippage: 0m, PnL: 0m, Position: 1m));
		source.AddTrade(new ReportTrade(
			TradeId: 1002, OrderTransactionId: 101, SecurityId: securityId, Time: baseTime.AddHours(1),
			TradePrice: 50500m, OrderPrice: 50500m, Volume: 1m, Side: Sides.Sell,
			OrderId: 2, Slippage: 0m, PnL: 500m, Position: 0m));

		source.AddPosition(new ReportPosition(
			SecurityId: securityId, PortfolioName: "TestPortfolio",
			OpenTime: baseTime, OpenPrice: 50000m,
			CloseTime: baseTime.AddHours(1), ClosePrice: 50500m, MaxPosition: 1m));

		return source;
	}

	private static string GetExpectedFilePath(string filename)
	{
		var dir = Path.GetDirectoryName(typeof(ReportTests).Assembly.Location);
		return Path.Combine(dir!, "Resources", "Reports", filename);
	}

	[TestMethod]
	[DataRow(FileExts.Json)]
	[DataRow(FileExts.Xml)]
	[DataRow(FileExts.Csv)]
	public async Task ReportGenerator_MatchesExpectedOutput(string format)
	{
		var source = CreateDeterministicSource();

		IReportGenerator generator = format switch
		{
			FileExts.Json => new JsonReportGenerator { IncludeOrders = true, IncludeTrades = true },
			FileExts.Xml => new XmlReportGenerator { IncludeOrders = true, IncludeTrades = true },
			FileExts.Csv => new CsvReportGenerator { IncludeOrders = true, IncludeTrades = true },
			_ => throw new ArgumentException($"Unknown format: {format}")
		};

		using var stream = new MemoryStream();
		await generator.Generate(source, stream, CancellationToken);

		stream.Position = 0;
		var actual = new StreamReader(stream).ReadToEnd();

		var expectedPath = GetExpectedFilePath($"expected_report{format}");
		var expected = File.ReadAllText(expectedPath);

		// Normalize line endings for comparison
		actual = actual.Replace("\r\n", "\n").Trim();
		expected = expected.Replace("\r\n", "\n").Trim();

		actual.AssertEqual(expected, $"{format} output should match expected file");
	}

	[TestMethod]
	public async Task CsvReportGenerator_ContainsAllRequiredSections()
	{
		var source = CreateDeterministicSource();
		var generator = new CsvReportGenerator { IncludeOrders = true, IncludeTrades = true };

		using var stream = new MemoryStream();
		await generator.Generate(source, stream, CancellationToken);

		stream.Position = 0;
		var csv = new StreamReader(stream).ReadToEnd();

		// Verify strategy info
		csv.AssertContains("TestStrategy");
		csv.AssertContains("5000"); // PnL
		csv.AssertContains("100"); // Position
		csv.AssertContains("25"); // Commission

		// Verify parameters
		csv.AssertContains("Symbol");
		csv.AssertContains("BTCUSD");

		// Verify statistics
		csv.AssertContains("WinRate");
		csv.AssertContains("0.65");
		csv.AssertContains("MaxDrawdown");
		csv.AssertContains("-500");

		// Verify orders
		csv.AssertContains("50000"); // Order price
		csv.AssertContains("50500"); // Order price

		// Verify trades
		csv.AssertContains("1001"); // Trade ID
		csv.AssertContains("1002"); // Trade ID
		csv.AssertContains("500"); // PnL

		// Verify diverse parameter types
		csv.AssertContains("TimeFrame");
		csv.AssertContains("00:05:00"); // TimeSpan
		csv.AssertContains("StartDate");
		csv.AssertContains("IsLong");
		csv.AssertContains("True");
		csv.AssertContains("MaxPositions");
		csv.AssertContains("5");

		// Verify diverse statistic types
		csv.AssertContains("TradesCount");
		csv.AssertContains("42");
		csv.AssertContains("AverageTradeTime");
		csv.AssertContains("00:15:00");
		csv.AssertContains("SharpeRatio");
		csv.AssertContains("1.85");
	}

	[TestMethod]
	public async Task JsonReportGenerator_GeneratesValidJson()
	{
		var source = new ReportSource
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
		var source = new ReportSource
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
		var source = new ReportSource
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
		var source = new ReportSource { Name = "OrderTest" };
		source.AddOrder(new ReportOrder(
			Id: 12345,
			TransactionId: 100,
			SecurityId: new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" },
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
		var source = new ReportSource { Name = "NoOrderTest" };
		source.AddOrder(new ReportOrder(
			Id: 99999,
			TransactionId: 1,
			SecurityId: new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" },
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

		json.Contains("\"orders\"").AssertFalse("Orders section should not be present when IncludeOrders=false");
	}

	[TestMethod]
	public void ReportSource_AddParameter_Works()
	{
		var source = new ReportSource { Name = "Test" }
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
		var source = new ReportSource { Name = "Test" }
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
		var source = new ReportSource { Name = "Test" };
		var time = DateTime.UtcNow;
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		source.AddOrder(1, 100, securityId, Sides.Buy, time, 50000m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);

		source.OrdersCount.AssertEqual(1);
		var order = source.Orders.First();
		order.Id.AssertEqual(1);
		order.TransactionId.AssertEqual(100);
		order.SecurityId.AssertEqual(securityId);
		order.Side.AssertEqual(Sides.Buy);
		order.Price.AssertEqual(50000m);
	}

	[TestMethod]
	public void ReportSource_AddTrade_Works()
	{
		var source = new ReportSource { Name = "Test" };
		var time = DateTime.UtcNow;
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		source.AddTrade(1001, 100, securityId, time, 50000m, 50000m, 1m, Sides.Buy, 1, 0m, 100m, 1m);

		source.TradesCount.AssertEqual(1);
		var trade = source.OwnTrades.First();
		trade.TradeId.AssertEqual(1001);
		trade.SecurityId.AssertEqual(securityId);
		trade.TradePrice.AssertEqual(50000m);
		trade.PnL.AssertEqual(100m);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_ByHour_Works()
	{
		var source = new ReportSource { Name = "Test" };
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add 5 buy orders in the same hour
		for (var i = 0; i < 5; i++)
		{
			source.AddOrder(i, i, securityId, Sides.Buy, baseTime.AddMinutes(i * 10), 100m + i, OrderStates.Done, 0m, 10m, OrderTypes.Limit);
		}

		// Add 3 sell orders in the same hour
		for (var i = 0; i < 3; i++)
		{
			source.AddOrder(100 + i, 100 + i, securityId, Sides.Sell, baseTime.AddMinutes(i * 15), 105m + i, OrderStates.Done, 0m, 5m, OrderTypes.Limit);
		}

		source.OrdersCount.AssertEqual(8);

		source.AggregateOrders(TimeSpan.FromHours(1));

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
		var source = new ReportSource { Name = "Test" };
		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add 5 buy trades in the same hour
		for (var i = 0; i < 5; i++)
		{
			source.AddTrade(i, i, securityId, baseTime.AddMinutes(i * 10), 100m, 100m, 1m, Sides.Buy, i, 0.1m, 10m, i);
		}

		source.TradesCount.AssertEqual(5);

		source.AggregateTrades(TimeSpan.FromHours(1));

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
		var source = new ReportSource { Name = "Test" };
		var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add trades across different hours but same day
		source.AddTrade(1, 1, securityId, baseTime.AddHours(1), 100m, 100m, 1m, Sides.Buy, 1, null, 10m, 1m);
		source.AddTrade(2, 2, securityId, baseTime.AddHours(5), 101m, 101m, 2m, Sides.Buy, 2, null, 20m, 3m);
		source.AddTrade(3, 3, securityId, baseTime.AddHours(10), 102m, 102m, 3m, Sides.Buy, 3, null, 30m, 6m);

		source.TradesCount.AssertEqual(3);

		source.AggregateTrades(TimeSpan.FromDays(1));

		source.TradesCount.AssertEqual(1);

		var trade = source.OwnTrades.First();
		trade.Volume.AssertEqual(6m); // 1 + 2 + 3
		trade.PnL.AssertEqual(60m); // 10 + 20 + 30
		trade.Position.AssertEqual(6m); // last position
	}

	[TestMethod]
	public void ReportSource_AutoAggregation_TriggersWhenThresholdExceeded()
	{
		var source = new ReportSource
		{
			Name = "Test",
			MaxOrdersBeforeAggregation = 10,
			AggregationInterval = TimeSpan.FromHours(1)
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add 15 orders in the same hour - should trigger auto-aggregation at 11th
		// and continue aggregating new orders into the same time bucket
		for (var i = 0; i < 15; i++)
		{
			source.AddOrder(i, i, securityId, Sides.Buy, baseTime.AddMinutes(i), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		// All orders are in the same hour, so after aggregation = 1 order
		source.OrdersCount.AssertEqual(1);
		source.Orders.First().Volume.AssertEqual(15m);
	}

	[TestMethod]
	public void ReportSource_AutoAggregation_GroupsByTimeBucket()
	{
		var source = new ReportSource
		{
			Name = "Test",
			MaxOrdersBeforeAggregation = 10,
			AggregationInterval = TimeSpan.FromHours(1)
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add 8 orders in hour 10 (not enough to trigger)
		for (var i = 0; i < 8; i++)
		{
			source.AddOrder(i, i, securityId, Sides.Buy, baseTime.AddMinutes(i), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		// Add 5 orders in hour 11 - this brings total to 13 which exceeds threshold
		for (var i = 0; i < 5; i++)
		{
			source.AddOrder(100 + i, 100 + i, securityId, Sides.Buy, baseTime.AddHours(1).AddMinutes(i), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		// Should be 2 orders: 1 for hour 10 (8 orders) + 1 for hour 11 (5 orders)
		source.OrdersCount.AssertEqual(2);

		var orders = source.Orders.OrderBy(o => o.Time).ToList();
		orders[0].Volume.AssertEqual(8m);
		orders[1].Volume.AssertEqual(5m);
	}

	[TestMethod]
	public void ReportSource_NoAutoAggregation_WhenDisabled()
	{
		var source = new ReportSource
		{
			Name = "Test",
			MaxOrdersBeforeAggregation = 0 // Disabled
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		for (var i = 0; i < 100; i++)
		{
			source.AddOrder(i, i, securityId, Sides.Buy, baseTime.AddMinutes(i), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		source.OrdersCount.AssertEqual(100);
	}

	[TestMethod]
	public void ReportSource_Clear_RemovesAllData()
	{
		var source = new ReportSource { Name = "Test" }
			.AddParameter("P1", 1)
			.AddStatisticParameter("S1", 2);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		source.AddOrder(1, 1, securityId, Sides.Buy, DateTime.UtcNow, 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddTrade(1, 1, securityId, DateTime.UtcNow, 100m, 100m, 1m, Sides.Buy, 1, null, null, null);

		source.Clear();

		source.OrdersCount.AssertEqual(0);
		source.TradesCount.AssertEqual(0);
		source.Parameters.Count().AssertEqual(0);
		source.StatisticParameters.Count().AssertEqual(0);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_PreservesWeightedAveragePrice()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Order 1: 100 @ 10 volume = 1000 total cost
		// Order 2: 200 @ 20 volume = 4000 total cost
		// Weighted avg = 5000 / 30 = 166.666...
		source.AddOrder(1, 1, securityId, Sides.Buy, time, 100m, OrderStates.Done, 0m, 10m, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, time.AddMinutes(5), 200m, OrderStates.Done, 0m, 20m, OrderTypes.Limit);

		source.AggregateOrders(TimeSpan.FromHours(1));

		var order = source.Orders.First();
		order.Volume.AssertEqual(30m);
		// Weighted average: (100*10 + 200*20) / 30 = 5000/30 = 166.666...
		var expectedPrice = (100m * 10m + 200m * 20m) / 30m;
		IsTrue(Math.Abs(order.Price - expectedPrice) < 0.0001m, $"Expected weighted average {expectedPrice}, got {order.Price}");
	}

	[TestMethod]
	public void ReportSource_AggregateTrades_PreservesWeightedAveragePrices()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Trade 1: TradePrice=100, OrderPrice=99, Volume=2
		// Trade 2: TradePrice=110, OrderPrice=108, Volume=3
		// Weighted TradePrice = (100*2 + 110*3) / 5 = 530/5 = 106
		// Weighted OrderPrice = (99*2 + 108*3) / 5 = 522/5 = 104.4
		source.AddTrade(1, 1, securityId, time, 100m, 99m, 2m, Sides.Buy, 1, 0.1m, 10m, 2m);
		source.AddTrade(2, 2, securityId, time.AddMinutes(5), 110m, 108m, 3m, Sides.Buy, 2, 0.2m, 20m, 5m);

		source.AggregateTrades(TimeSpan.FromHours(1));

		source.TradesCount.AssertEqual(1);
		var trade = source.OwnTrades.First();

		trade.Volume.AssertEqual(5m);

		var expectedTradePrice = (100m * 2m + 110m * 3m) / 5m;
		IsTrue(Math.Abs(trade.TradePrice - expectedTradePrice) < 0.0001m,
			$"Expected weighted TradePrice {expectedTradePrice}, got {trade.TradePrice}");

		var expectedOrderPrice = (99m * 2m + 108m * 3m) / 5m;
		IsTrue(Math.Abs(trade.OrderPrice - expectedOrderPrice) < 0.0001m,
			$"Expected weighted OrderPrice {expectedOrderPrice}, got {trade.OrderPrice}");

		trade.PnL.AssertEqual(30m); // 10 + 20
		trade.Slippage.AssertEqual(0.3m); // 0.1 + 0.2
		trade.Position.AssertEqual(5m); // last position
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_TimeIsBucketStart_WhenIntervalPositive()
	{
		var source = new ReportSource { Name = "Test" };
		var bucketStart = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Orders at different times within the hour
		source.AddOrder(1, 1, securityId, Sides.Buy, bucketStart.AddMinutes(15), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, bucketStart.AddMinutes(30), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(3, 3, securityId, Sides.Buy, bucketStart.AddMinutes(45), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);

		source.AggregateOrders(TimeSpan.FromHours(1));

		source.OrdersCount.AssertEqual(1);
		// Time should be bucket start (truncated to hour), not min of original times
		source.Orders.First().Time.AssertEqual(bucketStart);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_TimeIsMinTime_WhenIntervalZero()
	{
		var source = new ReportSource { Name = "Test" };
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		var time1 = new DateTime(2024, 1, 1, 10, 30, 0, DateTimeKind.Utc);
		var time2 = new DateTime(2024, 1, 2, 15, 45, 0, DateTimeKind.Utc);
		var time3 = new DateTime(2024, 1, 3, 8, 15, 0, DateTimeKind.Utc);

		source.AddOrder(1, 1, securityId, Sides.Buy, time2, 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, time1, 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit); // earliest
		source.AddOrder(3, 3, securityId, Sides.Buy, time3, 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);

		source.AggregateOrders(TimeSpan.Zero);

		source.OrdersCount.AssertEqual(1);
		// Time should be minimum of original times
		source.Orders.First().Time.AssertEqual(time1);
	}

	[TestMethod]
	public void ReportSource_AggregateTrades_NullPnL_ResultsInNullAggregate()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// All trades have null PnL
		source.AddTrade(1, 1, securityId, time, 100m, 100m, 1m, Sides.Buy, 1, null, null, null);
		source.AddTrade(2, 2, securityId, time.AddMinutes(5), 100m, 100m, 1m, Sides.Buy, 2, null, null, null);

		source.AggregateTrades(TimeSpan.FromHours(1));

		source.TradesCount.AssertEqual(1);
		var trade = source.OwnTrades.First();

		IsNull(trade.PnL, "Aggregated PnL should be null when all source trades have null PnL");
		IsNull(trade.Slippage, "Aggregated Slippage should be null when all source trades have null Slippage");
	}

	[TestMethod]
	public void ReportSource_AggregateTrades_MixedNullPnL_SumsNonNullValues()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Mix of null and non-null PnL
		source.AddTrade(1, 1, securityId, time, 100m, 100m, 1m, Sides.Buy, 1, null, 10m, null);
		source.AddTrade(2, 2, securityId, time.AddMinutes(5), 100m, 100m, 1m, Sides.Buy, 2, 0.5m, null, null); // null PnL treated as 0
		source.AddTrade(3, 3, securityId, time.AddMinutes(10), 100m, 100m, 1m, Sides.Buy, 3, null, 20m, null);

		source.AggregateTrades(TimeSpan.FromHours(1));

		source.TradesCount.AssertEqual(1);
		var trade = source.OwnTrades.First();

		// PnL: 10 + 0 + 20 = 30 (null treated as 0 when at least one has value)
		trade.PnL.AssertEqual(30m);
		// Slippage: 0 + 0.5 + 0 = 0.5
		trade.Slippage.AssertEqual(0.5m);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_NullVolume_UsesAveragePrice()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Orders with null volume - weighted average falls back to simple average
		source.AddOrder(1, 1, securityId, Sides.Buy, time, 100m, OrderStates.Done, null, null, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, time.AddMinutes(5), 200m, OrderStates.Done, null, null, OrderTypes.Limit);
		source.AddOrder(3, 3, securityId, Sides.Buy, time.AddMinutes(10), 300m, OrderStates.Done, null, null, OrderTypes.Limit);

		source.AggregateOrders(TimeSpan.FromHours(1));

		source.OrdersCount.AssertEqual(1);
		var order = source.Orders.First();

		// Simple average price: (100 + 200 + 300) / 3 = 200
		order.Price.AssertEqual(200m);
		// Volume should be null when total is 0
		IsNull(order.Volume, "Volume should be null when all source orders have null/zero volume");
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_BalanceIsNull()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		source.AddOrder(1, 1, securityId, Sides.Buy, time, 100m, OrderStates.Done, 5m, 10m, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, time.AddMinutes(5), 100m, OrderStates.Done, 3m, 10m, OrderTypes.Limit);

		source.AggregateOrders(TimeSpan.FromHours(1));

		source.OrdersCount.AssertEqual(1);
		// Balance in aggregated order should be null (not sum or last)
		IsNull(source.Orders.First().Balance, "Balance should be null in aggregated order");
	}

	[TestMethod]
	public void ReportSource_AutoAggregation_ExactlyAtThreshold_DoesNotAggregate()
	{
		var source = new ReportSource
		{
			Name = "Test",
			MaxOrdersBeforeAggregation = 10,
			AggregationInterval = TimeSpan.FromHours(1)
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add exactly 10 orders - should NOT trigger aggregation (trigger is > threshold)
		for (var i = 0; i < 10; i++)
		{
			source.AddOrder(i, i, securityId, Sides.Buy, baseTime.AddMinutes(i), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		// Should still have 10 orders (not aggregated)
		source.OrdersCount.AssertEqual(10);
	}

	[TestMethod]
	public void ReportSource_AutoAggregation_OneAboveThreshold_TriggersAggregation()
	{
		var source = new ReportSource
		{
			Name = "Test",
			MaxOrdersBeforeAggregation = 10,
			AggregationInterval = TimeSpan.FromHours(1)
		};

		var baseTime = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add 11 orders - should trigger aggregation (count > threshold)
		for (var i = 0; i < 11; i++)
		{
			source.AddOrder(i, i, securityId, Sides.Buy, baseTime.AddMinutes(i), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		// Should be aggregated to 1 (all in same hour, same side, same type)
		source.OrdersCount.AssertEqual(1);
		source.Orders.First().Volume.AssertEqual(11m);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_StateCalculatedByPriority()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add orders with different states in the same hour
		source.AddOrder(1, 1, securityId, Sides.Buy, time, 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, time.AddMinutes(1), 100m, OrderStates.Failed, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(3, 3, securityId, Sides.Buy, time.AddMinutes(2), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);

		source.AggregateOrders(TimeSpan.FromHours(1));

		source.OrdersCount.AssertEqual(1);
		// Failed has higher priority than Done
		source.Orders.First().State.AssertEqual(OrderStates.Failed);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_ActiveStateHasHighestPriority()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		source.AddOrder(1, 1, securityId, Sides.Buy, time, 100m, OrderStates.Failed, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, time.AddMinutes(1), 100m, OrderStates.Active, 5m, 10m, OrderTypes.Limit);
		source.AddOrder(3, 3, securityId, Sides.Buy, time.AddMinutes(2), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);

		source.AggregateOrders(TimeSpan.FromHours(1));

		source.OrdersCount.AssertEqual(1);
		// Active has highest priority
		source.Orders.First().State.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_DifferentTypesCreateSeparateAggregates()
	{
		var source = new ReportSource { Name = "Test" };
		var time = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc);
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add Limit orders
		source.AddOrder(1, 1, securityId, Sides.Buy, time, 100m, OrderStates.Done, 0m, 5m, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, time.AddMinutes(1), 101m, OrderStates.Done, 0m, 5m, OrderTypes.Limit);

		// Add Market orders in the same hour
		source.AddOrder(3, 3, securityId, Sides.Buy, time.AddMinutes(2), 102m, OrderStates.Done, 0m, 3m, OrderTypes.Market);
		source.AddOrder(4, 4, securityId, Sides.Buy, time.AddMinutes(3), 103m, OrderStates.Done, 0m, 3m, OrderTypes.Market);

		source.AggregateOrders(TimeSpan.FromHours(1));

		// Should have 2 aggregates: 1 for Limit, 1 for Market
		source.OrdersCount.AssertEqual(2);

		var orders = source.Orders.ToList();
		var limitOrder = orders.First(o => o.Type == OrderTypes.Limit);
		var marketOrder = orders.First(o => o.Type == OrderTypes.Market);

		limitOrder.Volume.AssertEqual(10m); // 5 + 5
		marketOrder.Volume.AssertEqual(6m); // 3 + 3
	}

	[TestMethod]
	public void ReportSource_AggregateOrders_ZeroInterval_AllInOneBucket()
	{
		var source = new ReportSource { Name = "Test" };
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add orders across different days
		source.AddOrder(1, 1, securityId, Sides.Buy, new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(2, 2, securityId, Sides.Buy, new DateTime(2024, 1, 2, 15, 0, 0, DateTimeKind.Utc), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(3, 3, securityId, Sides.Buy, new DateTime(2024, 1, 3, 20, 0, 0, DateTimeKind.Utc), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		source.AddOrder(4, 4, securityId, Sides.Buy, new DateTime(2024, 2, 1, 10, 0, 0, DateTimeKind.Utc), 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);

		// Zero interval = no time grouping, all collapse into one bucket
		source.AggregateOrders(TimeSpan.Zero);

		source.OrdersCount.AssertEqual(1);
		source.Orders.First().Volume.AssertEqual(4m);
	}

	[TestMethod]
	public void ReportSource_AggregateTrades_ZeroInterval_AllInOneBucket()
	{
		var source = new ReportSource { Name = "Test" };
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add trades across different days
		source.AddTrade(1, 1, securityId, new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc), 100m, 100m, 1m, Sides.Buy, 1, 0.1m, 10m, 1m);
		source.AddTrade(2, 2, securityId, new DateTime(2024, 1, 2, 15, 0, 0, DateTimeKind.Utc), 100m, 100m, 2m, Sides.Buy, 2, 0.2m, 20m, 3m);
		source.AddTrade(3, 3, securityId, new DateTime(2024, 1, 3, 20, 0, 0, DateTimeKind.Utc), 100m, 100m, 3m, Sides.Buy, 3, 0.3m, 30m, 6m);

		source.AggregateTrades(TimeSpan.Zero);

		source.TradesCount.AssertEqual(1);

		var trade = source.OwnTrades.First();
		trade.Volume.AssertEqual(6m); // 1 + 2 + 3
		trade.PnL.AssertEqual(60m); // 10 + 20 + 30
		trade.Slippage.AssertEqual(0.6m); // 0.1 + 0.2 + 0.3
		trade.Position.AssertEqual(6m); // last position
	}

	[TestMethod]
	public void ReportSource_AutoAggregation_ZeroInterval_AllCollapse()
	{
		var source = new ReportSource
		{
			Name = "Test",
			MaxOrdersBeforeAggregation = 5,
			AggregationInterval = TimeSpan.Zero // No time grouping
		};
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add 10 orders across different days - all should collapse into one
		for (var i = 0; i < 10; i++)
		{
			var time = new DateTime(2024, 1, 1 + i, 10, 0, 0, DateTimeKind.Utc);
			source.AddOrder(i, i, securityId, Sides.Buy, time, 100m, OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		// All orders in same bucket (no time grouping), same side, same type = 1 aggregate
		source.OrdersCount.AssertEqual(1);
		source.Orders.First().Volume.AssertEqual(10m);
	}

	[TestMethod]
	public void ReportSource_LoadTest_AggregationReducesCount()
	{
		var source = new ReportSource
		{
			Name = "LoadTest",
			MaxOrdersBeforeAggregation = 1000,
			MaxTradesBeforeAggregation = 1000,
			AggregationInterval = TimeSpan.FromHours(1)
		};

		var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		const int totalOrders = 50_000;
		const int totalTrades = 50_000;
		var securityId = new SecurityId { SecurityCode = "TEST", BoardCode = "TEST" };

		// Add 50k orders spread across 24 hours (~ 2000 per hour)
		for (var i = 0; i < totalOrders; i++)
		{
			var hour = i % 24;
			var minute = (i / 24) % 60;
			var time = baseTime.AddHours(hour).AddMinutes(minute).AddSeconds(i % 60);
			var side = i % 2 == 0 ? Sides.Buy : Sides.Sell;

			source.AddOrder(i, i, securityId, side, time, 100m + (i % 100), OrderStates.Done, 0m, 1m, OrderTypes.Limit);
		}

		// Add 50k trades spread across 24 hours
		for (var i = 0; i < totalTrades; i++)
		{
			var hour = i % 24;
			var minute = (i / 24) % 60;
			var time = baseTime.AddHours(hour).AddMinutes(minute).AddSeconds(i % 60);
			var side = i % 2 == 0 ? Sides.Buy : Sides.Sell;

			source.AddTrade(i, i, securityId, time, 100m + (i % 100), 100m, 1m, side, i, 0.01m, 1m, i % 100);
		}

		// After auto-aggregation, should have much fewer items
		// 24 hours * 2 sides * 1 type = 48 max orders (grouped by hour, side, type)
		// 24 hours * 2 sides = 48 max trades (grouped by hour, side)
		var ordersCount = source.OrdersCount;
		var tradesCount = source.TradesCount;

		IsTrue(ordersCount < 100, $"Expected orders to be aggregated to <100, got {ordersCount}");
		IsTrue(tradesCount < 100, $"Expected trades to be aggregated to <100, got {tradesCount}");

		// Verify total volume is preserved
		var totalOrderVolume = source.Orders.Sum(o => o.Volume ?? 0);
		var totalTradeVolume = source.OwnTrades.Sum(t => t.Volume);

		totalOrderVolume.AssertEqual(totalOrders, "Total order volume should be preserved after aggregation");
		totalTradeVolume.AssertEqual(totalTrades, "Total trade volume should be preserved after aggregation");

		// Verify PnL is preserved for trades
		var totalPnL = source.OwnTrades.Sum(t => t.PnL ?? 0);
		totalPnL.AssertEqual(totalTrades, $"Total PnL should be {totalTrades} (1m per trade)");
	}

	[TestMethod]
	public void ReportSource_MaxOrdersBeforeAggregation_ThrowsOnNegative()
	{
		var source = new ReportSource();
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.MaxOrdersBeforeAggregation = -1);
	}

	[TestMethod]
	public void ReportSource_MaxTradesBeforeAggregation_ThrowsOnNegative()
	{
		var source = new ReportSource();
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.MaxTradesBeforeAggregation = -1);
	}

	[TestMethod]
	public void ReportSource_AggregationInterval_ThrowsOnNegative()
	{
		var source = new ReportSource();
		ThrowsExactly<ArgumentOutOfRangeException>(() => source.AggregationInterval = TimeSpan.FromSeconds(-1));
	}

	[TestMethod]
	public void ReportSource_MaxOrdersBeforeAggregation_AcceptsZero()
	{
		var source = new ReportSource { MaxOrdersBeforeAggregation = 0 };
		source.MaxOrdersBeforeAggregation.AssertEqual(0);
	}

	[TestMethod]
	public void ReportSource_AggregationInterval_AcceptsZero()
	{
		var source = new ReportSource { AggregationInterval = TimeSpan.Zero };
		source.AggregationInterval.AssertEqual(TimeSpan.Zero);
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
		sheetNames.Count(s => s == "Trades").AssertEqual(0, "Trades sheet should not exist when IncludeTrades=false");
		sheetNames.Count(s => s == "Orders").AssertEqual(0, "Orders sheet should not exist when IncludeOrders=false");
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
		var source = new ReportSource
		{
			Name = "ExcelTestStrategy",
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
		var securityId = new SecurityId { SecurityCode = "BTCUSD", BoardCode = "CRYPTO" };
		source.AddTrade(new ReportTrade(
			TradeId: 1001,
			OrderTransactionId: 100,
			SecurityId: securityId,
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
			SecurityId: securityId,
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
			SecurityId: securityId,
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
			SecurityId: securityId,
			Side: Sides.Sell,
			Time: baseTime.AddHours(1),
			Price: 50500m,
			State: OrderStates.Done,
			Balance: 0m,
			Volume: 0.1m,
			Type: OrderTypes.Limit
		));

		source.AddPosition(new ReportPosition(
			SecurityId: securityId, PortfolioName: "TestPortfolio",
			OpenTime: baseTime, OpenPrice: 50000m,
			CloseTime: baseTime.AddHours(1), ClosePrice: 50500m, MaxPosition: 0.1m));

		return source;
	}

	#endregion
}
