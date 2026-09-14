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
			PriceStep = (decimal)(_random.NextDouble() * 0.1 + 0.01).Round(2),
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
			await secStorage.SaveAsync(sec, false, CancellationToken);
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
			await secStorage.SaveAsync(sec, false, CancellationToken);
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
			await secStorage.SaveAsync(sec, false, CancellationToken);
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
			await secStorage.SaveAsync(sec, false, CancellationToken);
		}

		for (int i = 0; i < 60; i++)
		{
			var sec = new Security
			{
				Id = $"SEC{i:D2}@NYSE",
				Code = $"SEC{i:D2}",
				Board = ExchangeBoard.Nyse,
			};
			await secStorage.SaveAsync(sec, false, CancellationToken);
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
			await secStorage.SaveAsync(sec, false, CancellationToken);
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
			await secStorage.SaveAsync(sec, false, CancellationToken);
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

		await secStorage.SaveAsync(sec, false, CancellationToken);
		AreEqual(1, ((ISecurityProvider)secStorage).Count);

		// Save again with same ID
		await secStorage.SaveAsync(sec, false, CancellationToken);
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
			await secStorage.SaveAsync(sec, false, CancellationToken);
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

		posStorage.Positions.Count().AssertEqual(20);
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
		await secStorage.SaveAsync(security, false, CancellationToken);

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
		var inner = new RecordingMessageAdapter();
		var storageProcessor = new TestStorageProcessor();
		var adapter = new StorageMetaInfoMessageAdapter(
			inner,
			new InMemorySecurityStorage(),
			new InMemoryPositionStorage(),
			new InMemoryExchangeInfoProvider(),
			storageProcessor);

		var mdMessage = new MarketDataMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
			TransactionId = 1,
		};

		await adapter.SendInMessageAsync(mdMessage, CancellationToken);

		IsTrue(storageProcessor.ProcessMarketDataCalled);
		storageProcessor.LastMessage.AssertSame(mdMessage);
		inner.InMessages.Count.AssertEqual(0);
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
			await secStorage.SaveAsync(sec, false, CancellationToken);
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

	#region Meta-Info Write-Through Tests

	private static readonly SecurityId _msft = new() { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

	private static (StorageMetaInfoMessageAdapter adapter,
		RecordingMessageAdapter inner,
		InMemorySecurityStorage secStorage,
		InMemoryPositionStorage posStorage,
		List<Message> heard) CreateConnectedAdapter()
	{
		var inner = new RecordingMessageAdapter();
		var secStorage = new InMemorySecurityStorage();
		var posStorage = new InMemoryPositionStorage();

		var adapter = new StorageMetaInfoMessageAdapter(inner, secStorage, posStorage, new InMemoryExchangeInfoProvider(), new TestStorageProcessor());

		var heard = new List<Message>();
		((IMessageAdapter)adapter).NewOutMessageAsync += (m, ct) => { heard.Add(m); return default; };

		return (adapter, inner, secStorage, posStorage, heard);
	}

	/// <summary>
	/// A portfolio subscription answered out of the meta-info storage replays what was recorded
	/// earlier, so every row has to carry the time it was last changed. Stamped with the moment of
	/// the lookup instead, a position nobody has touched for a week claims to have moved just now,
	/// and the subscriber has no other field left to learn the truth from.
	/// </summary>
	[TestMethod]
	public async Task PortfolioLookup_FromStorage_KeepsTheStoredServerTime()
	{
		var (adapter, _, secStorage, posStorage, heard) = CreateConnectedAdapter();

		var lastChange = new DateTime(2025, 3, 4, 9, 30, 0, DateTimeKind.Utc);

		var portfolio = new Portfolio
		{
			Name = "PF1",
			BeginValue = 100000,
			CurrentValue = 110000,
			ServerTime = lastChange,
		};

		posStorage.Save(portfolio);

		var security = new Security { Id = "MSFT@NASDAQ", Code = "MSFT", Board = ExchangeBoard.Nasdaq };
		await secStorage.SaveAsync(security, false, CancellationToken);

		posStorage.Save(new Position
		{
			Portfolio = portfolio,
			Security = security,
			CurrentValue = 10,
			ServerTime = lastChange,
		});

		await adapter.SendInMessageAsync(new PortfolioLookupMessage { TransactionId = 7, IsSubscribe = true }, CancellationToken);

		var posMsg = heard.OfType<PositionChangeMessage>().FirstOrDefault(m => m.SecurityId == _msft);
		IsNotNull(posMsg, "the position held in storage is replayed to the subscription");
		AreEqual(lastChange, posMsg.ServerTime, "the replayed position carries the time it was last changed, not the time of the lookup");

		var moneyMsg = heard.OfType<PositionChangeMessage>().FirstOrDefault(m => m.SecurityId == SecurityId.Money);
		IsNotNull(moneyMsg, "so is the money the portfolio holds");
		AreEqual(lastChange, moneyMsg.ServerTime, "and it keeps its own recorded time too");
	}

	/// <summary>
	/// What the meta-info storage already knows answers a portfolio subscription straight away,
	/// addressed to the subscription that asked, and the lookup still goes on to the connection - so
	/// the subscriber sees the last known state at once and the live state as soon as it arrives,
	/// rather than one instead of the other.
	/// </summary>
	[TestMethod]
	public async Task PortfolioLookup_FromStorage_AnswersWithWhatItHoldsAndStillAsksTheConnection()
	{
		var (adapter, inner, secStorage, posStorage, heard) = CreateConnectedAdapter();

		var portfolio = new Portfolio { Name = "PF1", BeginValue = 100000, CurrentValue = 110000 };
		posStorage.Save(portfolio);

		var security = new Security { Id = "MSFT@NASDAQ", Code = "MSFT", Board = ExchangeBoard.Nasdaq };
		await secStorage.SaveAsync(security, false, CancellationToken);

		posStorage.Save(new Position
		{
			Portfolio = portfolio,
			Security = security,
			CurrentValue = 10,
		});

		var lookupStarted = adapter.CurrentTime;
		await adapter.SendInMessageAsync(new PortfolioLookupMessage { TransactionId = 7, IsSubscribe = true }, CancellationToken);
		var lookupFinished = adapter.CurrentTime;

		var pfMsg = heard.OfType<PortfolioMessage>().FirstOrDefault(m => m.PortfolioName == "PF1");
		IsNotNull(pfMsg, "the portfolio held in storage answers the lookup on its own");
		AreEqual(7L, pfMsg.SubscriptionId, "and is addressed to the subscription that asked for it");

		var posMsg = heard.OfType<PositionChangeMessage>().FirstOrDefault(m => m.SecurityId == _msft);
		IsNotNull(posMsg, "so does the position it holds");
		AreEqual(7L, posMsg.SubscriptionId);
		AreEqual<decimal?>(10m, posMsg.TryGetDecimal(PositionChangeTypes.CurrentValue), "with the value that was recorded for it");
		IsTrue(posMsg.ServerTime >= lookupStarted && posMsg.ServerTime <= lookupFinished,
			"a stored position without its own time is stamped with the time of this lookup");

		HasCount(1, inner.InMessages.OfType<PortfolioLookupMessage>().ToArray(), "the lookup still reaches the connection: storage answers first, it does not answer instead");
	}

	/// <summary>
	/// A security the connection reports is written down as it passes, so the next session knows the
	/// instrument without having to look it up again - and the message still reaches whoever was
	/// listening, because recording it is not the same as consuming it.
	/// </summary>
	[TestMethod]
	public async Task Security_FromTheConnection_IsWrittenDownAndStillPassedOn()
	{
		var (_, inner, secStorage, _, heard) = CreateConnectedAdapter();

		await inner.SendOutMessageAsync(new SecurityMessage
		{
			SecurityId = _msft,
			Name = "Microsoft",
			PriceStep = 0.01m,
		}, CancellationToken);

		var stored = await secStorage.LookupByIdAsync(_msft, CancellationToken);

		IsNotNull(stored, "a security the connection reported is written down without anyone asking for it");
		AreEqual("Microsoft", stored.Name);
		AreEqual<decimal?>(0.01m, stored.PriceStep);

		IsNotNull(heard.OfType<SecurityMessage>().FirstOrDefault(m => m.SecurityId == _msft), "and still reaches whoever was listening for it");
	}

	/// <summary>
	/// A position can be reported for an instrument and an account nobody has looked up yet. Both are
	/// created and written down so that the position has something to hang on, otherwise the very
	/// first report of a holding would be dropped for want of a security row.
	/// </summary>
	[TestMethod]
	public async Task PositionChange_ForAnUnknownSecurity_CreatesWhatItNeedsAndStoresThePosition()
	{
		var (_, inner, secStorage, posStorage, _) = CreateConnectedAdapter();

		await inner.SendOutMessageAsync(new PositionChangeMessage
		{
			SecurityId = _msft,
			PortfolioName = "PF1",
			ServerTime = new DateTime(2025, 3, 4, 9, 30, 0, DateTimeKind.Utc),
		}.Add(PositionChangeTypes.CurrentValue, 5m), CancellationToken);

		var security = await secStorage.LookupByIdAsync(_msft, CancellationToken);
		IsNotNull(security, "the instrument the position speaks of is created rather than the position being dropped");

		var portfolio = posStorage.LookupByPortfolioName("PF1");
		IsNotNull(portfolio, "and so is the account it belongs to");

		var position = posStorage.GetPosition(portfolio, security, null, null);
		IsNotNull(position, "the position itself is written down");
		AreEqual<decimal?>(5m, position.CurrentValue, "with the value that was reported");
	}

	#endregion
}
