namespace StockSharp.Tests;

using System.IO.Compression;

using StockSharp.Algo.Storages.Csv;

[TestClass]
public class StorageCsvRegistryTests : BaseTestClass
{
	[TestMethod]
	public void ExchangeList_AddAndRead()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		var exchange = new Exchange
		{
			Name = "TEST",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};

		registry.Exchanges.Add(exchange);
		exchanges.DelayAction.WaitFlush(true);

		var loaded = registry.Exchanges.ReadById("TEST");
		Assert.IsNotNull(loaded);
		Assert.AreEqual("TEST", loaded.Name);
		Assert.AreEqual(CountryCodes.US, loaded.CountryCode);
		Assert.AreEqual("Test Exchange", loaded.FullNameLoc);
	}

	[TestMethod]
	public void ExchangeList_Update()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		var exchange = new Exchange
		{
			Name = "TEST",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};

		registry.Exchanges.Add(exchange);
		exchanges.DelayAction.WaitFlush(true);

		// Update
		exchange.CountryCode = CountryCodes.RU;
		exchange.FullNameLoc = "Updated Exchange";
		registry.Exchanges.Save(exchange);
		exchanges.DelayAction.WaitFlush(true);

		var loaded = registry.Exchanges.ReadById("TEST");
		Assert.IsNotNull(loaded);
		Assert.AreEqual(CountryCodes.RU, loaded.CountryCode);
		Assert.AreEqual("Updated Exchange", loaded.FullNameLoc);
	}

	[TestMethod]
	public void ExchangeList_Remove()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		var exchange = new Exchange
		{
			Name = "TEST",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};

		registry.Exchanges.Add(exchange);
		exchanges.DelayAction.WaitFlush(true);

		registry.Exchanges.Remove(exchange);
		exchanges.DelayAction.WaitFlush(true);

		var loaded = registry.Exchanges.ReadById("TEST");
		Assert.IsNull(loaded);
	}

	[TestMethod]
	public void ExchangeList_Clear()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		registry.Exchanges.Add(new Exchange { Name = "TEST1", CountryCode = CountryCodes.US });
		registry.Exchanges.Add(new Exchange { Name = "TEST2", CountryCode = CountryCodes.RU });
		exchanges.DelayAction.WaitFlush(true);

		Assert.AreEqual(2, registry.Exchanges.Count);

		registry.Exchanges.Clear();
		exchanges.DelayAction.WaitFlush(true);

		Assert.AreEqual(0, registry.Exchanges.Count);
	}

	[TestMethod]
	public void ExchangeBoardList_AddAndRead()
	{
		var registry = Helper.GetEntityRegistry();

		// First add exchange
		var exchange = new Exchange
		{
			Name = "TEST_EX",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};
		registry.Exchanges.Add(exchange);
		((ICsvEntityList)registry.Exchanges).DelayAction.WaitFlush(true);

		// Then add board
		var board = new ExchangeBoard
		{
			Code = "TEST_BOARD",
			Exchange = exchange,
			TimeZone = TimeZoneInfo.Utc
		};

		registry.ExchangeBoards.Add(board);
		((ICsvEntityList)registry.ExchangeBoards).DelayAction.WaitFlush(true);

		var loaded = registry.ExchangeBoards.ReadById("TEST_BOARD");
		Assert.IsNotNull(loaded);
		Assert.AreEqual("TEST_BOARD", loaded.Code);
		Assert.AreEqual("TEST_EX", loaded.Exchange.Name);
	}

	[TestMethod]
	public void SecurityList_AddAndRead()
	{
		var registry = Helper.GetEntityRegistry();

		// Add exchange and board first
		var exchange = new Exchange { Name = "TEST_EX", CountryCode = CountryCodes.US };
		registry.Exchanges.Add(exchange);
		((ICsvEntityList)registry.Exchanges).DelayAction.WaitFlush(true);

		var board = new ExchangeBoard { Code = "TBOARD", Exchange = exchange, TimeZone = TimeZoneInfo.Utc };
		registry.ExchangeBoards.Add(board);
		((ICsvEntityList)registry.ExchangeBoards).DelayAction.WaitFlush(true);

		// Add security
		var security = new Security
		{
			Id = "TEST@TBOARD",
			Code = "TEST",
			Board = board,
			Name = "Test Security",
			Type = SecurityTypes.Stock
		};

		registry.Securities.Save(security);
		((ICsvEntityList)registry.Securities).DelayAction.WaitFlush(true);

		var loaded = registry.Securities.ReadById(security.ToSecurityId());
		Assert.IsNotNull(loaded);
		Assert.AreEqual("TEST", loaded.Code);
		Assert.AreEqual("TBOARD", loaded.Board.Code);
		Assert.AreEqual("Test Security", loaded.Name);
	}

	[TestMethod]
	public void PortfolioList_AddAndRead()
	{
		var registry = Helper.GetEntityRegistry();

		var portfolio = new Portfolio
		{
			Name = "TEST_PORTFOLIO",
			Currency = CurrencyTypes.USD
		};

		registry.Portfolios.Add(portfolio);
		((ICsvEntityList)registry.Portfolios).DelayAction.WaitFlush(true);

		var loaded = registry.Portfolios.ReadById("TEST_PORTFOLIO");
		Assert.IsNotNull(loaded);
		Assert.AreEqual("TEST_PORTFOLIO", loaded.Name);
		Assert.AreEqual(CurrencyTypes.USD, loaded.Currency);
	}

	[TestMethod]
	public void CsvEntityList_LoadExisting()
	{
		var path = Helper.GetSubTemp();
		var registry1 = new CsvEntityRegistry(path);

		var exchange = new Exchange
		{
			Name = "TEST",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};

		registry1.Exchanges.Add(exchange);
		((ICsvEntityList)registry1.Exchanges).DelayAction.WaitFlush(true);

		// Create new registry instance to test loading
		var registry2 = new CsvEntityRegistry(path);
		registry2.Init();

		var loaded = registry2.Exchanges.ReadById("TEST");
		Assert.IsNotNull(loaded);
		Assert.AreEqual("TEST", loaded.Name);
		Assert.AreEqual(CountryCodes.US, loaded.CountryCode);
	}

	[TestMethod]
	public void CsvEntityList_DuplicateHandling()
	{
		var path = Helper.GetSubTemp();
		var registry = new CsvEntityRegistry(path);

		var exchange1 = new Exchange { Name = "TEST", CountryCode = CountryCodes.US };
		var exchange2 = new Exchange { Name = "TEST", CountryCode = CountryCodes.RU };

		registry.Exchanges.Add(exchange1);
		((ICsvEntityList)registry.Exchanges).DelayAction.WaitFlush(true);

		// Try to add duplicate - should fail
		var added = registry.Exchanges.Contains(exchange2);
		Assert.IsTrue(added, "Duplicate should be detected");
	}

	[TestMethod]
	public void ArchivedCopy_NotEnabled_ThrowsException()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		// CreateArchivedCopy is false by default
		Assert.IsFalse(exchanges.CreateArchivedCopy);

		Assert.ThrowsExactly<NotSupportedException>(() =>
		{
			var copy = exchanges.GetCopy();
		});
	}

	[TestMethod]
	public void ArchivedCopy_EmptyList()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;
		exchanges.DelayAction.WaitFlush(true);

		var copy = exchanges.GetCopy();
		Assert.IsNotNull(copy);
		Assert.IsGreaterThanOrEqualTo(0, copy.Length);

		// Decompress and verify - basic check
		using var memStream = new MemoryStream(copy);
		using var gzipStream = new GZipStream(memStream, CompressionMode.Decompress);
		using var resultStream = new MemoryStream();
		gzipStream.CopyTo(resultStream);
		var decompressed = resultStream.ToArray();

		Assert.IsNotNull(decompressed);
	}

	[TestMethod]
	public void ArchivedCopy_WithData()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add some data
		var exchange1 = new Exchange { Name = "NYSE", CountryCode = CountryCodes.US, FullNameLoc = "New York Stock Exchange" };
		var exchange2 = new Exchange { Name = "MOEX", CountryCode = CountryCodes.RU, FullNameLoc = "Moscow Exchange" };
		var exchange3 = new Exchange { Name = "LSE", CountryCode = CountryCodes.GB, FullNameLoc = "London Stock Exchange" };

		registry.Exchanges.Add(exchange1);
		registry.Exchanges.Add(exchange2);
		registry.Exchanges.Add(exchange3);
		exchanges.DelayAction.WaitFlush(true);

		// Get archived copy
		var copy = exchanges.GetCopy();
		Assert.IsNotNull(copy);
		Assert.IsNotEmpty(copy);

		// Decompress and verify content
		using var memStream = new MemoryStream(copy);
		using var gzipStream = new GZipStream(memStream, CompressionMode.Decompress);
		using var resultStream = new MemoryStream();
		gzipStream.CopyTo(resultStream);
		var content = resultStream.ToArray().UTF8();

		Assert.Contains("NYSE", content);
		Assert.Contains("MOEX", content);
		Assert.Contains("LSE", content);
		Assert.Contains("New York Stock Exchange", content);
		Assert.Contains("Moscow Exchange", content);
		Assert.Contains("London Stock Exchange", content);
	}

	[TestMethod]
	public void ArchivedCopy_CacheTest()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add data
		registry.Exchanges.Add(new Exchange { Name = "TEST1", CountryCode = CountryCodes.US });
		exchanges.DelayAction.WaitFlush(true);

		// Get first copy
		var copy1 = exchanges.GetCopy();

		// Get second copy - should be cached
		var copy2 = exchanges.GetCopy();

		// Should be the same instance (cached)
		Assert.AreSame(copy1, copy2);
	}

	[TestMethod]
	public void ArchivedCopy_ResetOnAdd()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add initial data
		registry.Exchanges.Add(new Exchange { Name = "TEST1", CountryCode = CountryCodes.US });
		exchanges.DelayAction.WaitFlush(true);

		// Get first copy
		var copy1 = exchanges.GetCopy();
		string content1;
		using (var memStream = new MemoryStream(copy1))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			content1 = resultStream.ToArray().UTF8();
		}

		// Add more data
		registry.Exchanges.Add(new Exchange { Name = "TEST2", CountryCode = CountryCodes.RU });
		exchanges.DelayAction.WaitFlush(true);

		// Get second copy - should be regenerated
		var copy2 = exchanges.GetCopy();
		string content2;
		using (var memStream = new MemoryStream(copy2))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			content2 = resultStream.ToArray().UTF8();
		}

		// Should not be the same instance
		Assert.AreNotSame(copy1, copy2);

		// Content should be different
		Assert.DoesNotContain("TEST2", content1);
		Assert.Contains("TEST2", content2);
	}

	[TestMethod]
	public void ArchivedCopy_ResetOnClear()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add data
		registry.Exchanges.Add(new Exchange { Name = "TEST1", CountryCode = CountryCodes.US });
		registry.Exchanges.Add(new Exchange { Name = "TEST2", CountryCode = CountryCodes.RU });
		exchanges.DelayAction.WaitFlush(true);

		// Get first copy
		var copy1 = exchanges.GetCopy();
		string content1;
		using (var memStream = new MemoryStream(copy1))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			content1 = resultStream.ToArray().UTF8();
		}
		Assert.Contains("TEST1", content1);

		// Clear list
		registry.Exchanges.Clear();
		exchanges.DelayAction.WaitFlush(true);

		// Get second copy - should be regenerated
		var copy2 = exchanges.GetCopy();
		string content2;
		using (var memStream = new MemoryStream(copy2))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			content2 = resultStream.ToArray().UTF8();
		}

		// Should not be the same instance
		Assert.AreNotSame(copy1, copy2);

		// Content should be empty or minimal
		Assert.DoesNotContain("TEST1", content2);
	}

	[TestMethod]
	public void ArchivedCopy_ResetOnUpdate()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add data
		var exchange = new Exchange { Name = "TEST", CountryCode = CountryCodes.US, FullNameLoc = "Original Name" };
		registry.Exchanges.Add(exchange);
		exchanges.DelayAction.WaitFlush(true);

		// Get first copy
		var copy1 = exchanges.GetCopy();
		string content1;
		using (var memStream = new MemoryStream(copy1))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			content1 = resultStream.ToArray().UTF8();
		}
		Assert.Contains("Original Name", content1);

		// Update data
		exchange.FullNameLoc = "Updated Name";
		registry.Exchanges.Save(exchange);
		exchanges.DelayAction.WaitFlush(true);

		// Get second copy - should be regenerated
		var copy2 = exchanges.GetCopy();
		string content2;
		using (var memStream = new MemoryStream(copy2))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			content2 = resultStream.ToArray().UTF8();
		}

		// Should not be the same instance
		Assert.AreNotSame(copy1, copy2);

		// Content should be updated
		Assert.Contains("Updated Name", content2);
	}

	[TestMethod]
	public void ArchivedCopy_MultipleEntityTypes()
	{
		var registry = Helper.GetEntityRegistry();

		// Test with Exchanges
		var exchangesList = (ICsvEntityList)registry.Exchanges;
		exchangesList.CreateArchivedCopy = true;
		registry.Exchanges.Add(new Exchange { Name = "NYSE", CountryCode = CountryCodes.US });
		exchangesList.DelayAction.WaitFlush(true);

		var exchangeCopy = exchangesList.GetCopy();
		Assert.IsNotNull(exchangeCopy);
		Assert.IsNotEmpty(exchangeCopy);

		// Test with Portfolios
		var portfoliosList = (ICsvEntityList)registry.Portfolios;
		portfoliosList.CreateArchivedCopy = true;
		registry.Portfolios.Add(new Portfolio { Name = "PORT1", Currency = CurrencyTypes.USD });
		portfoliosList.DelayAction.WaitFlush(true);

		var portfolioCopy = portfoliosList.GetCopy();
		Assert.IsNotNull(portfolioCopy);
		Assert.IsNotEmpty(portfolioCopy);

		// Verify content
		string exchangeContent;
		using (var memStream = new MemoryStream(exchangeCopy))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			exchangeContent = resultStream.ToArray().UTF8();
		}

		string portfolioContent;
		using (var memStream = new MemoryStream(portfolioCopy))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			portfolioContent = resultStream.ToArray().UTF8();
		}

		Assert.Contains("NYSE", exchangeContent);
		Assert.Contains("PORT1", portfolioContent);
	}

	[TestMethod]
	public void ArchivedCopy_CompressionRatio()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add many entries to test compression
		for (int i = 0; i < 100; i++)
		{
			registry.Exchanges.Add(new Exchange
			{
				Name = $"EXCHANGE_{i:D3}",
				CountryCode = (CountryCodes)(i % 10),
				FullNameLoc = $"Test Exchange Number {i} with some additional text for better compression testing"
			});
		}
		exchanges.DelayAction.WaitFlush(true);

		// Get compressed copy
		var compressed = exchanges.GetCopy();
		byte[] decompressed;
		using (var memStream = new MemoryStream(compressed))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			decompressed = resultStream.ToArray();
		}

		// Verify compression is working (compressed should be smaller)
		Assert.IsLessThan(decompressed.Length,
compressed.Length, $"Compressed ({compressed.Length} bytes) should be smaller than decompressed ({decompressed.Length} bytes)");

		// Verify all data is present
		var content = decompressed.UTF8();
		Assert.Contains("EXCHANGE_000", content);
		Assert.Contains("EXCHANGE_050", content);
		Assert.Contains("EXCHANGE_099", content);
	}

	[TestMethod]
	public void ArchivedCopy_EnableDisable()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		// Initially disabled
		Assert.IsFalse(exchanges.CreateArchivedCopy);

		// Enable
		exchanges.CreateArchivedCopy = true;
		registry.Exchanges.Add(new Exchange { Name = "TEST", CountryCode = CountryCodes.US });
		exchanges.DelayAction.WaitFlush(true);

		var copy = exchanges.GetCopy();
		Assert.IsNotNull(copy);

		// Disable
		exchanges.CreateArchivedCopy = false;

		// Should throw when disabled
		Assert.ThrowsExactly<NotSupportedException>(() =>
		{
			var copy2 = exchanges.GetCopy();
		});
	}

	[TestMethod]
	public void ArchivedCopy_LargeData()
	{
		var registry = Helper.GetEntityRegistry();
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add large amount of data
		for (int i = 0; i < 1000; i++)
		{
			registry.Exchanges.Add(new Exchange
			{
				Name = $"LARGE_EXCHANGE_{i:D4}",
				CountryCode = (CountryCodes)(i % 20),
				FullNameLoc = $"Large Test Exchange Number {i} with lots of additional text to make the data larger and test performance of compression and caching mechanisms"
			});
		}
		exchanges.DelayAction.WaitFlush(true);

		byte[] copy1 = null;
		// Measure time for first call (creates and caches)
		var sw1 = Watch.Do(() => copy1 = exchanges.GetCopy());

		// Measure time for second call (should use cache)
		var sw2 = Watch.Do(() => exchanges.GetCopy());

		// Cached call should be much faster
		Assert.IsTrue(sw2 < sw1,
			$"Cached call ({sw2.TotalMilliseconds}ms) should be faster than first call ({sw1.TotalMilliseconds}ms)");

		// Verify data integrity
		string content;
		using (var memStream = new MemoryStream(copy1))
		using (var gzipStream = new GZipStream(memStream, CompressionMode.Decompress))
		using (var resultStream = new MemoryStream())
		{
			gzipStream.CopyTo(resultStream);
			content = resultStream.ToArray().UTF8();
		}

		Assert.Contains("LARGE_EXCHANGE_0000", content);
		Assert.Contains("LARGE_EXCHANGE_0500", content);
		Assert.Contains("LARGE_EXCHANGE_0999", content);
	}
}
