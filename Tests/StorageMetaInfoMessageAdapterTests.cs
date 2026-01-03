namespace StockSharp.Tests;

using StockSharp.Algo;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

[TestClass]
public class StorageMetaInfoMessageAdapterTests
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

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(null, secStorage, posStorage, exchProvider, storageProcessor));
	}

	[TestMethod]
	public void Constructor_NullSecurityStorage_Throws()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var posStorage = new InMemoryPositionStorage();
		var exchProvider = new InMemoryExchangeInfoProvider();
		var storageProcessor = new TestStorageProcessor();

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(inner, null, posStorage, exchProvider, storageProcessor));
	}

	[TestMethod]
	public void Constructor_NullPositionStorage_Throws()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var secStorage = new InMemorySecurityStorage();
		var exchProvider = new InMemoryExchangeInfoProvider();
		var storageProcessor = new TestStorageProcessor();

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(inner, secStorage, null, exchProvider, storageProcessor));
	}

	[TestMethod]
	public void Constructor_NullExchangeInfoProvider_Throws()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var secStorage = new InMemorySecurityStorage();
		var posStorage = new InMemoryPositionStorage();
		var storageProcessor = new TestStorageProcessor();

		Assert.ThrowsExactly<ArgumentNullException>(() =>
			new StorageMetaInfoMessageAdapter(inner, secStorage, posStorage, null, storageProcessor));
	}

	[TestMethod]
	public void Constructor_NullStorageProcessor_Throws()
	{
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());
		var secStorage = new InMemorySecurityStorage();
		var posStorage = new InMemoryPositionStorage();
		var exchProvider = new InMemoryExchangeInfoProvider();

		Assert.ThrowsExactly<ArgumentNullException>(() =>
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
		Assert.IsFalse(adapter.OverrideSecurityData);
	}

	[TestMethod]
	public void OverrideSecurityData_CanBeSet()
	{
		var (adapter, _, _, _, _) = CreateAdapter();
		adapter.OverrideSecurityData = true;
		Assert.IsTrue(adapter.OverrideSecurityData);
	}

	#endregion

	#region Security Storage Tests

	[TestMethod]
	public async Task SecurityStorage_100Securities_SavesCorrectly()
	{
		var (adapter, secStorage, _, _, _) = CreateAdapter();

		// Pre-populate storage with securities
		var securities = new List<Security>();
		for (int i = 0; i < 100; i++)
		{
			var sec = GenerateRandomSecurity(i);
			securities.Add(sec);
			secStorage.Save(sec, false);
		}

		Assert.AreEqual(100, ((ISecurityProvider)secStorage).Count);

		// Verify all securities are in storage
		foreach (var sec in securities)
		{
			var found = secStorage.LookupById(sec.ToSecurityId());
			found.AssertNotNull();
			Assert.AreEqual(sec.Code, found.Code);
			Assert.AreEqual(sec.Type, found.Type);
		}

		await Task.CompletedTask;
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

		Assert.AreEqual(80, ((ISecurityProvider)secStorage).Count);

		// Lookup only stocks
		var stockCriteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };
		var stocks = secStorage.Lookup(stockCriteria).ToList();

		Assert.AreEqual(50, stocks.Count);
		Assert.IsTrue(stocks.All(s => s.Type == SecurityTypes.Stock));

		// Lookup only futures
		var futureCriteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Future };
		var futures = secStorage.Lookup(futureCriteria).ToList();

		Assert.AreEqual(30, futures.Count);
		Assert.IsTrue(futures.All(s => s.Type == SecurityTypes.Future));

		await Task.CompletedTask;
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

		Assert.AreEqual(100, ((ISecurityProvider)secStorage).Count);

		// Lookup by board
		var nasdaqCriteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = "NASDAQ" }
		};
		var nasdaqSecurities = secStorage.Lookup(nasdaqCriteria).ToList();

		Assert.AreEqual(40, nasdaqSecurities.Count);
		Assert.IsTrue(nasdaqSecurities.All(s => s.Board?.Code == "NASDAQ"));

		await Task.CompletedTask;
	}

	[TestMethod]
	public async Task SecurityLookup_ByCurrency_FiltersCorrectly()
	{
		var (adapter, secStorage, _, _, _) = CreateAdapter();

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

		Assert.AreEqual(50, ((ISecurityProvider)secStorage).Count);

		// Lookup by currency
		var usdCriteria = new SecurityLookupMessage { Currency = CurrencyTypes.USD };
		var usdSecurities = secStorage.Lookup(usdCriteria).ToList();

		Assert.AreEqual(30, usdSecurities.Count);
		Assert.IsTrue(usdSecurities.All(s => s.Currency == CurrencyTypes.USD));

		await Task.CompletedTask;
	}

	[TestMethod]
	public async Task SecurityStorage_DuplicateSave_DoesNotDuplicate()
	{
		var (adapter, secStorage, _, _, _) = CreateAdapter();

		var sec = new Security
		{
			Id = "AAPL@NASDAQ",
			Code = "AAPL",
			Board = ExchangeBoard.Nasdaq,
			Name = "Apple Inc",
		};

		secStorage.Save(sec, false);
		Assert.AreEqual(1, ((ISecurityProvider)secStorage).Count);

		// Save again with same ID
		secStorage.Save(sec, false);
		Assert.AreEqual(1, ((ISecurityProvider)secStorage).Count);

		await Task.CompletedTask;
	}

	#endregion

	#region Portfolio Storage Tests

	[TestMethod]
	public async Task PortfolioStorage_100Portfolios_SavesCorrectly()
	{
		var (adapter, _, posStorage, _, _) = CreateAdapter();

		var portfolios = new List<Portfolio>();
		for (int i = 0; i < 100; i++)
		{
			var portfolio = GenerateRandomPortfolio(i);
			portfolios.Add(portfolio);
			posStorage.Save(portfolio);
		}

		Assert.AreEqual(100, posStorage.Portfolios.Count());

		// Verify all portfolios are retrievable
		foreach (var pf in portfolios)
		{
			var found = posStorage.LookupByPortfolioName(pf.Name);
			found.AssertNotNull();
			Assert.AreEqual(pf.Name, found.Name);
		}

		await Task.CompletedTask;
	}

	[TestMethod]
	public async Task PortfolioStorage_LookupByName_FindsCorrectPortfolio()
	{
		var (adapter, _, posStorage, _, _) = CreateAdapter();

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
		Assert.AreEqual("TestPortfolio", found.Name);
		Assert.AreEqual(CurrencyTypes.USD, found.Currency);
		Assert.AreEqual(100000, found.BeginValue);

		await Task.CompletedTask;
	}

	[TestMethod]
	public async Task PortfolioStorage_LookupNonExistent_ReturnsNull()
	{
		var (adapter, _, posStorage, _, _) = CreateAdapter();

		var found = posStorage.LookupByPortfolioName("NonExistent");
		Assert.IsNull(found);

		await Task.CompletedTask;
	}

	#endregion

	#region Position Storage Tests

	[TestMethod]
	public async Task PositionStorage_200Positions_SavesCorrectly()
	{
		var (adapter, secStorage, posStorage, _, _) = CreateAdapter();

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
		Assert.IsTrue(posStorage.Positions.Count() > 0);

		await Task.CompletedTask;
	}

	[TestMethod]
	public async Task PositionStorage_GetPosition_ReturnsCorrectPosition()
	{
		var (adapter, secStorage, posStorage, _, _) = CreateAdapter();

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
		Assert.AreEqual(1000, found.CurrentValue);
		Assert.AreEqual(500, found.BeginValue);

		await Task.CompletedTask;
	}

	#endregion

	#region Exchange Board Storage Tests

	[TestMethod]
	public async Task BoardStorage_SaveAndRetrieve_WorksCorrectly()
	{
		var (adapter, _, _, exchProvider, _) = CreateAdapter();

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
		Assert.AreEqual("TESTBOARD", foundBoard.Code);
		Assert.AreEqual("TestExchange", foundBoard.Exchange.Name);

		var foundExchange = exchProvider.TryGetExchange("TestExchange");
		foundExchange.AssertNotNull();
		Assert.AreEqual("TestExchange", foundExchange.Name);

		await Task.CompletedTask;
	}

	[TestMethod]
	public async Task BoardStorage_50Boards_SavesAllCorrectly()
	{
		var (adapter, _, _, exchProvider, _) = CreateAdapter();

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
		Assert.AreEqual(50, addedBoards);

		// Verify each board is retrievable
		for (int i = 0; i < 50; i++)
		{
			var found = exchProvider.TryGetExchangeBoard($"BOARD{i:D2}");
			found.AssertNotNull();
			Assert.AreEqual($"BOARD{i:D2}", found.Code);
		}

		await Task.CompletedTask;
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
		await adapter.SendInMessageAsync(mdMessage, CancellationToken.None);

		Assert.IsTrue(storageProcessor.ProcessMarketDataCalled);
		storageProcessor.LastMessage.AssertNotNull();
		Assert.AreEqual(mdMessage.SecurityId, storageProcessor.LastMessage.SecurityId);

		await Task.CompletedTask;
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
		Assert.AreEqual(true, clone.OverrideSecurityData);
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

		Assert.AreEqual(true, adapter2.OverrideSecurityData);
	}

	#endregion

	#region Integration-like Tests

	[TestMethod]
	public async Task FullWorkflow_SecuritiesPortfoliosPositions_SavesAndRetrieves()
	{
		var (adapter, secStorage, posStorage, exchProvider, _) = CreateAdapter();

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

		Assert.AreEqual(50, ((ISecurityProvider)secStorage).Count);

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

		Assert.AreEqual(5, posStorage.Portfolios.Count());

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
		var board1Securities = secStorage.Lookup(new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = board1.Code }
		}).ToList();

		Assert.AreEqual(25, board1Securities.Count);
		Assert.IsTrue(board1Securities.All(s => s.Board?.Code == board1.Code));

		// - Portfolio lookup
		var foundPf = posStorage.LookupByPortfolioName("Portfolio0");
		foundPf.AssertNotNull();

		// - Position lookup
		var foundPos = posStorage.GetPosition(portfolios[0], securities[0], null, null);
		foundPos.AssertNotNull();

		await Task.CompletedTask;
	}

	#endregion
}
