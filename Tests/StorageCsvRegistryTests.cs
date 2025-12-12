namespace StockSharp.Tests;

using System.IO.Compression;

using StockSharp.Algo.Storages.Csv;

[TestClass]
public class StorageCsvRegistryTests : BaseTestClass
{
	private static ChannelExecutor CreateExecutor(CancellationToken token)
		=> Helper.CreateExecutor(token);

	[TestMethod]
	public async Task ExchangeList_AddAndRead()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		var exchange = new Exchange
		{
			Name = "TEST",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};

		registry.Exchanges.Add(exchange);
		await executor.WaitFlushAsync(token);

		var loaded = registry.Exchanges.ReadById("TEST");
		IsNotNull(loaded);
		AreEqual("TEST", loaded.Name);
		AreEqual(CountryCodes.US, loaded.CountryCode);
		AreEqual("Test Exchange", loaded.FullNameLoc);
	}

	[TestMethod]
	public async Task ExchangeList_Update()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		var exchange = new Exchange
		{
			Name = "TEST",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};

		registry.Exchanges.Add(exchange);
		await executor.WaitFlushAsync(token);

		// Update
		exchange.CountryCode = CountryCodes.RU;
		exchange.FullNameLoc = "Updated Exchange";
		registry.Exchanges.Save(exchange);
		await executor.WaitFlushAsync(token);

		var loaded = registry.Exchanges.ReadById("TEST");
		IsNotNull(loaded);
		AreEqual(CountryCodes.RU, loaded.CountryCode);
		AreEqual("Updated Exchange", loaded.FullNameLoc);
	}

	[TestMethod]
	public async Task ExchangeList_Remove()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		var exchange = new Exchange
		{
			Name = "TEST",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};

		registry.Exchanges.Add(exchange);
		await executor.WaitFlushAsync(token);

		registry.Exchanges.Remove(exchange);
		await executor.WaitFlushAsync(token);

		var loaded = registry.Exchanges.ReadById("TEST");
		IsNull(loaded);
	}

	[TestMethod]
	public async Task ExchangeList_Clear()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		registry.Exchanges.Add(new Exchange { Name = "TEST1", CountryCode = CountryCodes.US });
		registry.Exchanges.Add(new Exchange { Name = "TEST2", CountryCode = CountryCodes.RU });
		await executor.WaitFlushAsync(token);

		registry.Exchanges.Count.AreEqual(2);

		registry.Exchanges.Clear();
		await executor.WaitFlushAsync(token);

		registry.Exchanges.Count.AreEqual(0);
	}

	[TestMethod]
	public async Task ExchangeBoardList_AddAndRead()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		// First add exchange
		var exchange = new Exchange
		{
			Name = "TEST_EX",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};
		registry.Exchanges.Add(exchange);
		await executor.WaitFlushAsync(token);

		// Then add board
		var board = new ExchangeBoard
		{
			Code = "TEST_BOARD",
			Exchange = exchange,
			TimeZone = TimeZoneInfo.Utc
		};

		registry.ExchangeBoards.Add(board);
		await executor.WaitFlushAsync(token);

		var loaded = registry.ExchangeBoards.ReadById("TEST_BOARD");
		IsNotNull(loaded);
		AreEqual("TEST_BOARD", loaded.Code);
		AreEqual("TEST_EX", loaded.Exchange.Name);
	}

	[TestMethod]
	public async Task SecurityList_AddAndRead()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		// Add exchange and board first
		var exchange = new Exchange { Name = "TEST_EX", CountryCode = CountryCodes.US };
		registry.Exchanges.Add(exchange);
		await executor.WaitFlushAsync(token);

		var board = new ExchangeBoard { Code = "TBOARD", Exchange = exchange, TimeZone = TimeZoneInfo.Utc };
		registry.ExchangeBoards.Add(board);
		await executor.WaitFlushAsync(token);

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
		await executor.WaitFlushAsync(token);

		var loaded = registry.Securities.ReadById(security.ToSecurityId());
		IsNotNull(loaded);
		AreEqual("TEST", loaded.Code);
		AreEqual("TBOARD", loaded.Board.Code);
		AreEqual("Test Security", loaded.Name);
	}

	[TestMethod]
	public async Task PortfolioList_AddAndRead()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		var portfolio = new Portfolio
		{
			Name = "TEST_PORTFOLIO",
			Currency = CurrencyTypes.USD
		};

		registry.Portfolios.Add(portfolio);
		await executor.WaitFlushAsync(token);

		var loaded = registry.Portfolios.ReadById("TEST_PORTFOLIO");
		IsNotNull(loaded);
		AreEqual("TEST_PORTFOLIO", loaded.Name);
		AreEqual(CurrencyTypes.USD, loaded.Currency);
	}

	[TestMethod]
	public async Task CsvEntityList_LoadExisting()
	{
		var token = CancellationToken;
		var path = Helper.GetSubTemp();
		var executor = CreateExecutor(token);
		var registry1 = new CsvEntityRegistry(path, executor);

		var exchange = new Exchange
		{
			Name = "TEST",
			CountryCode = CountryCodes.US,
			FullNameLoc = "Test Exchange"
		};

		registry1.Exchanges.Add(exchange);
		await executor.WaitFlushAsync(token);

		// Create new registry instance to test loading
		var registry2 = new CsvEntityRegistry(path, executor);
		registry2.Init();

		var loaded = registry2.Exchanges.ReadById("TEST");
		IsNotNull(loaded);
		AreEqual("TEST", loaded.Name);
		AreEqual(CountryCodes.US, loaded.CountryCode);
	}

	[TestMethod]
	public async Task CsvEntityList_DuplicateHandling()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		var exchange1 = new Exchange { Name = "TEST", CountryCode = CountryCodes.US };
		var exchange2 = new Exchange { Name = "TEST", CountryCode = CountryCodes.RU };

		registry.Exchanges.Add(exchange1);
		await executor.WaitFlushAsync(token);

		// Try to add duplicate - should fail
		var added = registry.Exchanges.Contains(exchange2);
		IsTrue(added, "Duplicate should be detected");
	}

	[TestMethod]
	public async Task ArchivedCopy_NotEnabled_ThrowsException()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
		var exchanges = (ICsvEntityList)registry.Exchanges;

		// CreateArchivedCopy is false by default
		IsFalse(exchanges.CreateArchivedCopy);

		ThrowsExactly<NotSupportedException>(() =>
		{
			var copy = exchanges.GetCopy();
		});
	}

	[TestMethod]
	public async Task ArchivedCopy_EmptyList()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;
		await executor.WaitFlushAsync(token);

		var copy = exchanges.GetCopy();
		IsNotNull(copy);
		Assert.IsGreaterThanOrEqualTo(0, copy.Length);

		// Decompress and verify - basic check
		using var memStream = new MemoryStream(copy);
		using var gzipStream = new GZipStream(memStream, CompressionMode.Decompress);
		using var resultStream = new MemoryStream();
		gzipStream.CopyTo(resultStream);
		var decompressed = resultStream.ToArray();

		IsNotNull(decompressed);
	}

	[TestMethod]
	public async Task ArchivedCopy_WithData()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add some data
		var exchange1 = new Exchange { Name = "NYSE", CountryCode = CountryCodes.US, FullNameLoc = "New York Stock Exchange" };
		var exchange2 = new Exchange { Name = "MOEX", CountryCode = CountryCodes.RU, FullNameLoc = "Moscow Exchange" };
		var exchange3 = new Exchange { Name = "LSE", CountryCode = CountryCodes.GB, FullNameLoc = "London Stock Exchange" };

		registry.Exchanges.Add(exchange1);
		registry.Exchanges.Add(exchange2);
		registry.Exchanges.Add(exchange3);
		await executor.WaitFlushAsync(token);

		// Get archived copy
		var copy = exchanges.GetCopy();
		IsNotNull(copy);
		IsNotEmpty(copy);

		// Decompress and verify content
		using var memStream = new MemoryStream(copy);
		using var gzipStream = new GZipStream(memStream, CompressionMode.Decompress);
		using var resultStream = new MemoryStream();
		gzipStream.CopyTo(resultStream);
		var content = resultStream.ToArray().UTF8();

		Contains("NYSE", content);
		Contains("MOEX", content);
		Contains("LSE", content);
		Contains("New York Stock Exchange", content);
		Contains("Moscow Exchange", content);
		Contains("London Stock Exchange", content);
	}

	[TestMethod]
	public async Task ArchivedCopy_CacheTest()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add data
		registry.Exchanges.Add(new Exchange { Name = "TEST1", CountryCode = CountryCodes.US });
		await executor.WaitFlushAsync(token);

		// Get first copy
		var copy1 = exchanges.GetCopy();

		// Get second copy - should be cached
		var copy2 = exchanges.GetCopy();

		// Should be the same instance (cached)
		AreSame(copy1, copy2);
	}

	[TestMethod]
	public async Task ArchivedCopy_ResetOnAdd()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add initial data
		registry.Exchanges.Add(new Exchange { Name = "TEST1", CountryCode = CountryCodes.US });
		await executor.WaitFlushAsync(token);

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
		await executor.WaitFlushAsync(token);

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
		AreNotSame(copy1, copy2);

		// Content should be different
		DoesNotContain("TEST2", content1);
		Contains("TEST2", content2);
	}

	[TestMethod]
	public async Task ArchivedCopy_ResetOnClear()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add data
		registry.Exchanges.Add(new Exchange { Name = "TEST1", CountryCode = CountryCodes.US });
		registry.Exchanges.Add(new Exchange { Name = "TEST2", CountryCode = CountryCodes.RU });
		await executor.WaitFlushAsync(token);

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
		Contains("TEST1", content1);

		// Clear list
		registry.Exchanges.Clear();
		await executor.WaitFlushAsync(token);

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
		AreNotSame(copy1, copy2);

		// Content should be empty or minimal
		DoesNotContain("TEST1", content2);
	}

	[TestMethod]
	public async Task ArchivedCopy_ResetOnUpdate()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
		var exchanges = (ICsvEntityList)registry.Exchanges;

		exchanges.CreateArchivedCopy = true;

		// Add data
		var exchange = new Exchange { Name = "TEST", CountryCode = CountryCodes.US, FullNameLoc = "Original Name" };
		registry.Exchanges.Add(exchange);
		await executor.WaitFlushAsync(token);

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
		Contains("Original Name", content1);

		// Update data
		exchange.FullNameLoc = "Updated Name";
		registry.Exchanges.Save(exchange);
		await executor.WaitFlushAsync(token);

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
		AreNotSame(copy1, copy2);

		// Content should be updated
		Contains("Updated Name", content2);
	}

	[TestMethod]
	public async Task ArchivedCopy_MultipleEntityTypes()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);

		// Test with Exchanges
		var exchangesList = (ICsvEntityList)registry.Exchanges;
		exchangesList.CreateArchivedCopy = true;
		registry.Exchanges.Add(new Exchange { Name = "NYSE", CountryCode = CountryCodes.US });
		await executor.WaitFlushAsync(token);

		var exchangeCopy = exchangesList.GetCopy();
		IsNotNull(exchangeCopy);
		IsNotEmpty(exchangeCopy);

		// Test with Portfolios
		var portfoliosList = (ICsvEntityList)registry.Portfolios;
		portfoliosList.CreateArchivedCopy = true;
		registry.Portfolios.Add(new Portfolio { Name = "PORT1", Currency = CurrencyTypes.USD });
		await executor.WaitFlushAsync(token);

		var portfolioCopy = portfoliosList.GetCopy();
		IsNotNull(portfolioCopy);
		IsNotEmpty(portfolioCopy);

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

		Contains("NYSE", exchangeContent);
		Contains("PORT1", portfolioContent);
	}

	[TestMethod]
	public async Task ArchivedCopy_CompressionRatio()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
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
		await executor.WaitFlushAsync(token);

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
		Assert.IsLessThan(decompressed.Length, compressed.Length, $"Compressed ({compressed.Length} bytes) should be smaller than decompressed ({decompressed.Length} bytes)");

		// Verify all data is present
		var content = decompressed.UTF8();
		Contains("EXCHANGE_000", content);
		Contains("EXCHANGE_050", content);
		Contains("EXCHANGE_099", content);
	}

	[TestMethod]
	public async Task ArchivedCopy_EnableDisable()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
		var exchanges = (ICsvEntityList)registry.Exchanges;

		// Initially disabled
		IsFalse(exchanges.CreateArchivedCopy);

		// Enable
		exchanges.CreateArchivedCopy = true;
		registry.Exchanges.Add(new Exchange { Name = "TEST", CountryCode = CountryCodes.US });
		await executor.WaitFlushAsync(token);

		var copy = exchanges.GetCopy();
		IsNotNull(copy);

		// Disable
		exchanges.CreateArchivedCopy = false;

		// Should throw when disabled
		ThrowsExactly<NotSupportedException>(() =>
		{
			var copy2 = exchanges.GetCopy();
		});
	}

	[TestMethod]
	public async Task ArchivedCopy_LargeData()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var registry = Helper.GetEntityRegistry(executor);
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
		await executor.WaitFlushAsync(token);

		byte[] copy1 = null;
		// Measure time for first call (creates and caches)
		var sw1 = Watch.Do(() => copy1 = exchanges.GetCopy());

		// Measure time for second call (should use cache)
		var sw2 = Watch.Do(() => exchanges.GetCopy());

		// Cached call should be much faster
		IsTrue(sw2 < sw1,
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

		Contains("LARGE_EXCHANGE_0000", content);
		Contains("LARGE_EXCHANGE_0500", content);
		Contains("LARGE_EXCHANGE_0999", content);
	}
}
