namespace StockSharp.Tests;

using StockSharp.Algo.Candles.Compression;

[TestClass]
public class StorageMetaInfoMessageAdapterTests : BaseTestClass
{
	#region Random Data Generators

	private static readonly Random _random = new(42);

	private static Security GenerateRandomSecurity(int index, ExchangeBoard board = null)
	{
		var types = new[] { SecurityTypes.Stock, SecurityTypes.Future, SecurityTypes.Option, SecurityTypes.Currency, SecurityTypes.Bond };
		var currencies = new[] { CurrencyTypes.USD, CurrencyTypes.EUR, CurrencyTypes.RUB, CurrencyTypes.GBP };

		var code = $"SEC{index:D4}";
		board ??= ExchangeBoard.Nasdaq;

		return new Security
		{
			Id = $"{code}@{board.Code}",
			Code = code,
			Board = board,
			Name = $"Security {index}",
			Type = types[_random.Next(types.Length)],
			Currency = currencies[_random.Next(currencies.Length)],
			PriceStep = (decimal)Math.Round(_random.NextDouble() * 0.1 + 0.01, 2),
			VolumeStep = _random.Next(1, 10),
			Multiplier = _random.Next(1, 100),
			Decimals = _random.Next(0, 5),
		};
	}

	private static Portfolio GenerateRandomPortfolio(int index)
	{
		var currencies = new[] { CurrencyTypes.USD, CurrencyTypes.EUR, CurrencyTypes.RUB };

		return new Portfolio
		{
			Name = $"Portfolio{index:D3}",
			Currency = currencies[_random.Next(currencies.Length)],
			BeginValue = (decimal)(_random.NextDouble() * 1000000),
			CurrentValue = (decimal)(_random.NextDouble() * 1000000),
		};
	}

	private static Position GenerateRandomPosition(Portfolio portfolio, Security security)
	{
		return new Position
		{
			Portfolio = portfolio,
			Security = security,
			CurrentValue = (decimal)((_random.NextDouble() - 0.5) * 10000),
			BeginValue = (decimal)((_random.NextDouble() - 0.5) * 10000),
			AveragePrice = (decimal)(_random.NextDouble() * 1000),
		};
	}

	private static ExchangeBoard GenerateRandomBoard(int index)
	{
		var exchange = new Exchange { Name = $"Exchange{index:D2}" };
		return new ExchangeBoard
		{
			Code = $"BOARD{index:D2}",
			Exchange = exchange,
			TimeZone = TimeZoneInfo.Utc,
		};
	}

	#endregion

	#region Mock Storage Processor

	private class TestStorageProcessor : IStorageProcessor
	{
		public bool ProcessMarketDataCalled { get; private set; }
		public MarketDataMessage LastMessage { get; private set; }

		public StorageCoreSettings Settings { get; } = new();
		public CandleBuilderProvider CandleBuilderProvider => null;

		public void Reset()
		{
			ProcessMarketDataCalled = false;
			LastMessage = null;
		}

		public async IAsyncEnumerable<Message> ProcessMarketData(MarketDataMessage message, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
		{
			ProcessMarketDataCalled = true;
			LastMessage = message;
			await Task.CompletedTask;
			yield break;
		}
	}

	#endregion

	#region Helper Methods

	private static (StorageMetaInfoMessageAdapter adapter,
		InMemorySecurityStorage secStorage,
		InMemoryPositionStorage posStorage,
		InMemoryExchangeInfoProvider exchProvider,
		TestStorageProcessor storageProcessor) CreateAdapter()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var secStorage = new InMemorySecurityStorage();
		var posStorage = new InMemoryPositionStorage();
		var exchProvider = new InMemoryExchangeInfoProvider();
		var storageProcessor = new TestStorageProcessor();

		var adapter = new StorageMetaInfoMessageAdapter(inner, secStorage, posStorage, exchProvider, storageProcessor);

		return (adapter, secStorage, posStorage, exchProvider, storageProcessor);
	}

	#endregion

	#region Constructor Tests

	[TestMethod]
	public void Constructor_NullInnerAdapter_Throws()
	{
		var secStorage = new InMemorySecurityStorage();
		var posStorage = new InMemoryPositionStorage();
		var exchProvider = new InMemoryExchangeInfoProvider();
		var storageProcessor = new TestStorageProcessor();

		ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(null, secStorage, posStorage, exchProvider, storageProcessor));
	}

	[TestMethod]
	public void Constructor_NullSecurityStorage_Throws()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var posStorage = new InMemoryPositionStorage();
		var exchProvider = new InMemoryExchangeInfoProvider();
		var storageProcessor = new TestStorageProcessor();

		ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(inner, null, posStorage, exchProvider, storageProcessor));
	}

	[TestMethod]
	public void Constructor_NullPositionStorage_Throws()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var secStorage = new InMemorySecurityStorage();
		var exchProvider = new InMemoryExchangeInfoProvider();
		var storageProcessor = new TestStorageProcessor();

		ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(inner, secStorage, null, exchProvider, storageProcessor));
	}

	[TestMethod]
	public void Constructor_NullExchangeInfoProvider_Throws()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var secStorage = new InMemorySecurityStorage();
		var posStorage = new InMemoryPositionStorage();
		var storageProcessor = new TestStorageProcessor();

		ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(inner, secStorage, posStorage, null, storageProcessor));
	}

	[TestMethod]
	public void Constructor_NullStorageProcessor_Throws()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var secStorage = new InMemorySecurityStorage();
		var posStorage = new InMemoryPositionStorage();
		var exchProvider = new InMemoryExchangeInfoProvider();

		ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(inner, secStorage, posStorage, exchProvider, null));
	}

	[TestMethod]
	public void Constructor_ValidParams_CreatesAdapter()
	{
		var (adapter, _, _, _, _) = CreateAdapter();
		adapter.AssertNotNull();
	}

	#endregion

	#region OverrideSecurityData Property Tests

	[TestMethod]
	public void OverrideSecurityData_DefaultIsFalse()
	{
		var (adapter, _, _, _, _) = CreateAdapter();
		IsFalse(adapter.OverrideSecurityData);
	}

	[TestMethod]
	public void OverrideSecurityData_CanBeSet()
	{
		var (adapter, _, _, _, _) = CreateAdapter();
		adapter.OverrideSecurityData = true;
		IsTrue(adapter.OverrideSecurityData);
	}

	#endregion

	#region Security Storage Tests

	[TestMethod]
	public async Task SecurityStorage_100Securities_SavesCorrectly()
	{
		var (_, secStorage, _, _, _) = CreateAdapter();

		// Pre-populate storage with securities
		var securities = new List<Security>();
		for (int i = 0; i < 100; i++)
		{
			var sec = GenerateRandomSecurity(i);
			securities.Add(sec);
			secStorage.Save(sec, false);
		}

		AreEqual(100, ((ISecurityProvider)secStorage).Count);

		// Verify all securities are in storage
		foreach (var sec in securities)
		{
			var found = await secStorage.LookupByIdAsync(sec.ToSecurityId(), CancellationToken);
			found.AssertNotNull();
			AreEqual(sec.Code, found.Code);
			AreEqual(sec.Type, found.Type);
		}
	}

	[TestMethod]
	public async Task SecurityLookup_ByType_FiltersCorrectly()
	{
		var (adapter, secStorage, _, _, _) = CreateAdapter();

		// Add securities of different types
		for (int i = 0; i < 50; i++)
		{
			var sec = new Security
			{
				Id = $"STOCK{i:D2}@NASDAQ",
				Code = $"STOCK{i:D2}",
				Board = ExchangeBoard.Nasdaq,
				Type = SecurityTypes.Stock,
			};
			secStorage.Save(sec, false);
		}

		for (int i = 0; i < 30; i++)
		{
			var sec = new Security
			{
				Id = $"FUT{i:D2}@NASDAQ",
				Code = $"FUT{i:D2}",
				Board = ExchangeBoard.Nasdaq,
				Type = SecurityTypes.Future,
			};
			secStorage.Save(sec, false);
		}

		AreEqual(80, ((ISecurityProvider)secStorage).Count);

		// Lookup only stocks
		var stockCriteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };
		var stocks = await secStorage.LookupAsync(stockCriteria).ToListAsync(CancellationToken);

		AreEqual(50, stocks.Count);
		IsTrue(stocks.All(s => s.Type == SecurityTypes.Stock));

		// Lookup only futures
		var futureCriteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Future };
		var futures = await secStorage.LookupAsync(futureCriteria).ToListAsync(CancellationToken);

		AreEqual(30, futures.Count);
		IsTrue(futures.All(s => s.Type == SecurityTypes.Future));
	}

	[TestMethod]
	public async Task SecurityLookup_ByBoard_FiltersCorrectly()
	{
		var (adapter, secStorage, _, exchProvider, _) = CreateAdapter();

		// Add securities on different boards
		for (int i = 0; i < 40; i++)
		{
			var sec = new Security
			{
				Id = $"SEC{i:D2}@NASDAQ",
				Code = $"SEC{i:D2}",
				Board = ExchangeBoard.Nasdaq,
			};
			secStorage.Save(sec, false);
		}

		for (int i = 0; i < 60; i++)
		{
			var sec = new Security
			{
				Id = $"SEC{i:D2}@NYSE",
				Code = $"SEC{i:D2}",
				Board = ExchangeBoard.Nyse,
			};
			secStorage.Save(sec, false);
		}

		AreEqual(100, ((ISecurityProvider)secStorage).Count);

		// Lookup by board
		var nasdaqCriteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = "NASDAQ" }
		};
		var nasdaqSecurities = await secStorage.LookupAsync(nasdaqCriteria).ToListAsync(CancellationToken);

		AreEqual(40, nasdaqSecurities.Count);
		IsTrue(nasdaqSecurities.All(s => s.Board?.Code == "NASDAQ"));
	}

	[TestMethod]
	public async Task SecurityLookup_ByCurrency_FiltersCorrectly()
	{
		var (_, secStorage, _, _, _) = CreateAdapter();

		// Add securities with different currencies
		for (int i = 0; i < 30; i++)
		{
			var sec = new Security
			{
				Id = $"USD{i:D2}@NASDAQ",
				Code = $"USD{i:D2}",
				Board = ExchangeBoard.Nasdaq,
				Currency = CurrencyTypes.USD,
			};
			secStorage.Save(sec, false);
		}

		for (int i = 0; i < 20; i++)
		{
			var sec = new Security
			{
				Id = $"EUR{i:D2}@NASDAQ",
				Code = $"EUR{i:D2}",
				Board = ExchangeBoard.Nasdaq,
				Currency = CurrencyTypes.EUR,
			};
			secStorage.Save(sec, false);
		}

		AreEqual(50, ((ISecurityProvider)secStorage).Count);

		// Lookup by currency
		var usdCriteria = new SecurityLookupMessage { Currency = CurrencyTypes.USD };
		var usdSecurities = await secStorage.LookupAsync(usdCriteria).ToListAsync(CancellationToken);

		AreEqual(30, usdSecurities.Count);
		IsTrue(usdSecurities.All(s => s.Currency == CurrencyTypes.USD));
	}

	[TestMethod]
	public async Task SecurityStorage_DuplicateSave_DoesNotDuplicate()
	{
		var (_, secStorage, _, _, _) = CreateAdapter();

		var sec = new Security
		{
			Id = "AAPL@NASDAQ",
			Code = "AAPL",
			Board = ExchangeBoard.Nasdaq,
			Name = "Apple Inc",
		};

		secStorage.Save(sec, false);
		AreEqual(1, ((ISecurityProvider)secStorage).Count);

		// Save again with same ID
		secStorage.Save(sec, false);
		AreEqual(1, ((ISecurityProvider)secStorage).Count);
	}

	#endregion

	#region Portfolio Storage Tests

	[TestMethod]
	public async Task PortfolioStorage_100Portfolios_SavesCorrectly()
	{
		var (_, _, posStorage, _, _) = CreateAdapter();

		var portfolios = new List<Portfolio>();
		for (int i = 0; i < 100; i++)
		{
			var portfolio = GenerateRandomPortfolio(i);
			portfolios.Add(portfolio);
			posStorage.Save(portfolio);
		}

		AreEqual(100, posStorage.Portfolios.Count());

		// Verify all portfolios are retrievable
		foreach (var pf in portfolios)
		{
			var found = posStorage.LookupByPortfolioName(pf.Name);
			found.AssertNotNull();
			AreEqual(pf.Name, found.Name);
		}
	}

	[TestMethod]
	public async Task PortfolioStorage_LookupByName_FindsCorrectPortfolio()
	{
		var (_, _, posStorage, _, _) = CreateAdapter();

		var portfolio = new Portfolio
		{
			Name = "TestPortfolio",
			Currency = CurrencyTypes.USD,
			BeginValue = 100000,
			CurrentValue = 150000,
		};

		posStorage.Save(portfolio);

		var found = posStorage.LookupByPortfolioName("TestPortfolio");
		found.AssertNotNull();
		AreEqual("TestPortfolio", found.Name);
		AreEqual(CurrencyTypes.USD, found.Currency);
		AreEqual(100000, found.BeginValue);
	}

	[TestMethod]
	public async Task PortfolioStorage_LookupNonExistent_ReturnsNull()
	{
		var (_, _, posStorage, _, _) = CreateAdapter();

		var found = posStorage.LookupByPortfolioName("NonExistent");
		IsNull(found);
	}

	#endregion

	#region Position Storage Tests

	[TestMethod]
	public async Task PositionStorage_200Positions_SavesCorrectly()
	{
		var (_, secStorage, posStorage, _, _) = CreateAdapter();

		// Create portfolios and securities first
		var portfolios = new List<Portfolio>();
		for (int i = 0; i < 10; i++)
		{
			var pf = GenerateRandomPortfolio(i);
			portfolios.Add(pf);
			posStorage.Save(pf);
		}

		var securities = new List<Security>();
		for (int i = 0; i < 20; i++)
		{
			var sec = GenerateRandomSecurity(i);
			securities.Add(sec);
			secStorage.Save(sec, false);
		}

		// Create positions
		var positions = new List<Position>();
		for (int i = 0; i < 200; i++)
		{
			var pf = portfolios[i % portfolios.Count];
			var sec = securities[i % securities.Count];
			var pos = GenerateRandomPosition(pf, sec);
			positions.Add(pos);
			posStorage.Save(pos);
		}

		// Note: Positions with same portfolio+security will overwrite each other
		// So we won't have exactly 200 unique positions
		IsTrue(posStorage.Positions.Count() > 0);
	}

	[TestMethod]
	public async Task PositionStorage_GetPosition_ReturnsCorrectPosition()
	{
		var (_, secStorage, posStorage, _, _) = CreateAdapter();

		var portfolio = new Portfolio { Name = "PF1" };
		var security = new Security
		{
			Id = "AAPL@NASDAQ",
			Code = "AAPL",
			Board = ExchangeBoard.Nasdaq,
		};

		posStorage.Save(portfolio);
		secStorage.Save(security, false);

		var position = new Position
		{
			Portfolio = portfolio,
			Security = security,
			CurrentValue = 1000,
			BeginValue = 500,
		};

		posStorage.Save(position);

		var found = posStorage.GetPosition(portfolio, security, null, null);
		found.AssertNotNull();
		AreEqual(1000, found.CurrentValue);
		AreEqual(500, found.BeginValue);
	}

	#endregion

	#region Exchange Board Storage Tests

	[TestMethod]
	public async Task BoardStorage_SaveAndRetrieve_WorksCorrectly()
	{
		var (_, _, _, exchProvider, _) = CreateAdapter();

		var exchange = new Exchange { Name = "TestExchange" };
		var board = new ExchangeBoard
		{
			Code = "TESTBOARD",
			Exchange = exchange,
			TimeZone = TimeZoneInfo.Utc,
		};

		exchProvider.Save(exchange);
		exchProvider.Save(board);

		var foundBoard = exchProvider.TryGetExchangeBoard("TESTBOARD");
		foundBoard.AssertNotNull();
		AreEqual("TESTBOARD", foundBoard.Code);
		AreEqual("TestExchange", foundBoard.Exchange.Name);

		var foundExchange = exchProvider.TryGetExchange("TestExchange");
		foundExchange.AssertNotNull();
		AreEqual("TestExchange", foundExchange.Name);
	}

	[TestMethod]
	public async Task BoardStorage_50Boards_SavesAllCorrectly()
	{
		var (_, _, _, exchProvider, _) = CreateAdapter();

		var initialBoardCount = exchProvider.Boards.Count();

		for (int i = 0; i < 50; i++)
		{
			var exchange = new Exchange { Name = $"Exchange{i:D2}" };
			var board = new ExchangeBoard
			{
				Code = $"BOARD{i:D2}",
				Exchange = exchange,
			};

			exchProvider.Save(exchange);
			exchProvider.Save(board);
		}

		// Verify boards were added
		var addedBoards = exchProvider.Boards.Count() - initialBoardCount;
		AreEqual(50, addedBoards);

		// Verify each board is retrievable
		for (int i = 0; i < 50; i++)
		{
			var found = exchProvider.TryGetExchangeBoard($"BOARD{i:D2}");
			found.AssertNotNull();
			AreEqual($"BOARD{i:D2}", found.Code);
		}
	}

	#endregion

	#region MarketData Processing Tests

	[TestMethod]
	public async Task MarketDataMessage_ProcessedByStorageProcessor()
	{
		var (adapter, _, _, _, storageProcessor) = CreateAdapter();

		var mdMessage = new MarketDataMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 1,
		};

		// Send message through adapter
		await adapter.SendInMessageAsync(mdMessage, CancellationToken);

		IsTrue(storageProcessor.ProcessMarketDataCalled);
		storageProcessor.LastMessage.AssertNotNull();
		AreEqual(mdMessage.SecurityId, storageProcessor.LastMessage.SecurityId);
	}

	#endregion

	#region Clone Tests

	[TestMethod]
	public void Clone_CopiesProperties()
	{
		var (adapter, _, _, _, _) = CreateAdapter();
		adapter.OverrideSecurityData = true;

		var clone = (StorageMetaInfoMessageAdapter)adapter.Clone();

		clone.AssertNotNull();
		AreEqual(true, clone.OverrideSecurityData);
	}

	#endregion

	#region Save/Load Settings Tests

	[TestMethod]
	public void SaveLoad_PreservesSettings()
	{
		var (adapter, _, _, _, _) = CreateAdapter();
		adapter.OverrideSecurityData = true;

		var storage = new SettingsStorage();
		adapter.Save(storage);

		var (adapter2, _, _, _, _) = CreateAdapter();
		adapter2.Load(storage);

		AreEqual(true, adapter2.OverrideSecurityData);
	}

	#endregion

	#region Integration-like Tests

	[TestMethod]
	public async Task FullWorkflow_SecuritiesPortfoliosPositions_SavesAndRetrieves()
	{
		var (_, secStorage, posStorage, exchProvider, _) = CreateAdapter();

		// 1. Save boards
		var board1 = new ExchangeBoard { Code = "TESTBOARD1", Exchange = new Exchange { Name = "TESTEXCH1" } };
		var board2 = new ExchangeBoard { Code = "TESTBOARD2", Exchange = new Exchange { Name = "TESTEXCH2" } };

		exchProvider.Save(board1.Exchange);
		exchProvider.Save(board1);
		exchProvider.Save(board2.Exchange);
		exchProvider.Save(board2);

		// 2. Save securities
		var securities = new List<Security>();
		for (int i = 0; i < 50; i++)
		{
			var sec = new Security
			{
				Id = $"SEC{i:D2}@{(i % 2 == 0 ? board1.Code : board2.Code)}",
				Code = $"SEC{i:D2}",
				Board = i % 2 == 0 ? board1 : board2,
				Type = SecurityTypes.Stock,
				PriceStep = 0.01m,
			};
			securities.Add(sec);
			secStorage.Save(sec, false);
		}

		AreEqual(50, ((ISecurityProvider)secStorage).Count);

		// 3. Save portfolios
		var portfolios = new List<Portfolio>();
		for (int i = 0; i < 5; i++)
		{
			var pf = new Portfolio
			{
				Name = $"Portfolio{i}",
				BeginValue = 100000 * (i + 1),
				CurrentValue = 100000 * (i + 1) + _random.Next(-10000, 10000),
			};
			portfolios.Add(pf);
			posStorage.Save(pf);
		}

		AreEqual(5, posStorage.Portfolios.Count());

		// 4. Save positions
		foreach (var pf in portfolios)
		{
			foreach (var sec in securities.Take(10)) // 10 securities per portfolio
			{
				var pos = new Position
				{
					Portfolio = pf,
					Security = sec,
					CurrentValue = _random.Next(-1000, 1000),
					BeginValue = _random.Next(-1000, 1000),
				};
				posStorage.Save(pos);
			}
		}

		// 5. Verify retrieval
		// - Securities by board
		var board1Securities = await secStorage.LookupAsync(new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = board1.Code }
		}).ToListAsync(CancellationToken);

		AreEqual(25, board1Securities.Count);
		IsTrue(board1Securities.All(s => s.Board?.Code == board1.Code));

		// - Portfolio lookup
		var foundPf = posStorage.LookupByPortfolioName("Portfolio0");
		foundPf.AssertNotNull();

		// - Position lookup
		var foundPos = posStorage.GetPosition(portfolios[0], securities[0], null, null);
		foundPos.AssertNotNull();
	}

	#endregion
}
