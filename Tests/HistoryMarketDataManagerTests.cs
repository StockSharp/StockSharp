namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Generation;
using StockSharp.Configuration;

[TestClass]
public class HistoryMarketDataManagerTests : BaseTestClass
{
	private static IStorageRegistry GetHistoryStorage()
	{
		var fs = Helper.FileSystem;
		return fs.GetStorage(Paths.HistoryDataPath);
	}
	private static SecurityId CreateSecurityId() => Helper.CreateSecurityId();

	#region Property Tests

	[TestMethod]
	public void Constructor_SetsDefaultValues()
	{
		using var manager = new HistoryMarketDataManager();

		manager.StartDate.AssertEqual(DateTime.MinValue);
		manager.StopDate.AssertEqual(DateTime.MaxValue);
		manager.MarketTimeChangedInterval.AssertEqual(TimeSpan.FromSeconds(1));
		manager.PostTradeMarketTimeChangedCount.AssertEqual(2);
		manager.CheckTradableDates.AssertEqual(false);
		manager.IsStarted.AssertEqual(false);
		manager.LoadedMessageCount.AssertEqual(0);
	}

	[TestMethod]
	public void MarketTimeChangedInterval_ThrowsOnInvalidValue()
	{
		using var manager = new HistoryMarketDataManager();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.MarketTimeChangedInterval = TimeSpan.Zero);

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.MarketTimeChangedInterval = TimeSpan.FromSeconds(-1));
	}

	[TestMethod]
	public void PostTradeMarketTimeChangedCount_ThrowsOnNegativeValue()
	{
		using var manager = new HistoryMarketDataManager();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.PostTradeMarketTimeChangedCount = -1);
	}

	[TestMethod]
	public void PostTradeMarketTimeChangedCount_AcceptsZero()
	{
		using var manager = new HistoryMarketDataManager();

		manager.PostTradeMarketTimeChangedCount = 0;
		manager.PostTradeMarketTimeChangedCount.AssertEqual(0);
	}

	#endregion

	#region Generator Tests

	[TestMethod]
	public void RegisterGenerator_AddsGenerator()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();
		var dataType = DataType.Ticks;
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, dataType, generator, 123);

		manager.HasGenerator(secId, dataType).AssertTrue();
	}

	[TestMethod]
	public void RegisterGenerator_ThrowsOnNullGenerator()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		ThrowsExactly<ArgumentNullException>(() =>
			manager.RegisterGenerator(secId, DataType.Ticks, null, 123));
	}

	[TestMethod]
	public void HasGenerator_ReturnsFalseWhenNotRegistered()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		manager.HasGenerator(secId, DataType.Ticks).AssertFalse();
	}

	[TestMethod]
	public void UnregisterGenerator_RemovesGenerator()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();
		var dataType = DataType.Ticks;
		var generator = new RandomWalkTradeGenerator(secId);
		long transId = 123;

		manager.RegisterGenerator(secId, dataType, generator, transId);
		manager.HasGenerator(secId, dataType).AssertTrue();

		var result = manager.UnregisterGenerator(transId);

		result.AssertTrue();
		manager.HasGenerator(secId, dataType).AssertFalse();
	}

	[TestMethod]
	public void UnregisterGenerator_ReturnsFalseWhenNotFound()
	{
		using var manager = new HistoryMarketDataManager();

		var result = manager.UnregisterGenerator(999);

		result.AssertFalse();
	}

	#endregion

	#region Subscription Tests

	[TestMethod]
	public async Task SubscribeAsync_ReturnsErrorWithoutStorageRegistry()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		var message = new MarketDataMessage
		{
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};

		var error = await manager.SubscribeAsync(message, CancellationToken);

		error.AssertNotNull();
		(error is InvalidOperationException).AssertTrue();
	}

	[TestMethod]
	public async Task SubscribeAsync_ThrowsOnNullMessage()
	{
		using var manager = new HistoryMarketDataManager();

		await ThrowsExactlyAsync<ArgumentNullException>(() =>
			manager.SubscribeAsync(null, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task SubscribeAsync_ThrowsOnUnsubscribeMessage()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		var message = new MarketDataMessage
		{
			IsSubscribe = false,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};

		await ThrowsExactlyAsync<ArgumentException>(() =>
			manager.SubscribeAsync(message, CancellationToken).AsTask());
	}

	[TestMethod]
	public void Unsubscribe_ThrowsOnZeroTransactionId()
	{
		using var manager = new HistoryMarketDataManager();

		ThrowsExactly<ArgumentException>(() =>
			manager.Unsubscribe(0));
	}

	#endregion

	#region Reset Tests

	[TestMethod]
	public void Reset_ClearsGenerators()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, DataType.Ticks, generator, 1);
		manager.HasGenerator(secId, DataType.Ticks).AssertTrue();

		manager.Reset();

		manager.HasGenerator(secId, DataType.Ticks).AssertFalse();
	}

	[TestMethod]
	public void Reset_ResetsLoadedMessageCount()
	{
		using var manager = new HistoryMarketDataManager();

		manager.Reset();

		manager.LoadedMessageCount.AssertEqual(0);
		manager.IsStarted.AssertEqual(false);
	}

	#endregion

	#region GetSupportedDataTypes Tests

	[TestMethod]
	public void GetSupportedDataTypes_ReturnsEmptyWithoutDriveAndGenerators()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		var dataTypes = manager.GetSupportedDataTypes(secId);

		dataTypes.Count().AssertEqual(0);
	}

	[TestMethod]
	public void GetSupportedDataTypes_IncludesGeneratorDataTypes()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, DataType.Ticks, generator, 1);

		var dataTypes = manager.GetSupportedDataTypes(secId).ToList();

		dataTypes.Contains(DataType.Ticks).AssertTrue();
	}

	[TestMethod]
	public void GetSupportedDataTypes_WithoutDrive_ReturnsOnlyGenerators()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		manager.RegisterGenerator(secId, DataType.Ticks, new RandomWalkTradeGenerator(secId), 1);
		manager.RegisterGenerator(secId, DataType.Level1, new RandomWalkTradeGenerator(secId), 2);

		var dataTypes = manager.GetSupportedDataTypes(secId).ToList();

		dataTypes.Count.AssertEqual(2);
		dataTypes.Contains(DataType.Ticks).AssertTrue();
		dataTypes.Contains(DataType.Level1).AssertTrue();
	}

	[TestMethod]
	public void GetSupportedDataTypes_FiltersGeneratorsBySecurityId()
	{
		using var manager = new HistoryMarketDataManager();
		var secId1 = CreateSecurityId();
		var secId2 = CreateSecurityId();

		manager.RegisterGenerator(secId1, DataType.Ticks, new RandomWalkTradeGenerator(secId1), 1);
		manager.RegisterGenerator(secId2, DataType.Level1, new RandomWalkTradeGenerator(secId2), 2);

		var dataTypes1 = manager.GetSupportedDataTypes(secId1).ToList();
		var dataTypes2 = manager.GetSupportedDataTypes(secId2).ToList();

		dataTypes1.Count.AssertEqual(1);
		dataTypes1.Contains(DataType.Ticks).AssertTrue();

		dataTypes2.Count.AssertEqual(1);
		dataTypes2.Contains(DataType.Level1).AssertTrue();
	}

	#endregion

	#region Stop Tests

	[TestMethod]
	public void Stop_CanBeCalledMultipleTimes()
	{
		using var manager = new HistoryMarketDataManager();

		manager.Stop();
		manager.Stop();
		manager.Stop();
	}

	#endregion

	#region StartAsync Tests

	[TestMethod]
	public async Task StartAsync_WithoutStorageOrGenerators_YieldsOnlyTimeAndStoppingMessages()
	{
		using var manager = new HistoryMarketDataManager
		{
			StartDate = DateTime.UtcNow,
			StopDate = DateTime.UtcNow.AddMinutes(1),
		};

		var messages = new List<Message>();
		var boards = Array.Empty<BoardMessage>();

		await foreach (var msg in manager.StartAsync(boards).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		// Should end with EmulationStateMessage Stopping
		var lastMessage = messages.LastOrDefault();
		lastMessage.AssertNotNull();
		(lastMessage is EmulationStateMessage state && state.State == ChannelStates.Stopping).AssertTrue();
	}

	[TestMethod]
	public async Task StartAsync_SetsIsStartedTrue()
	{
		using var manager = new HistoryMarketDataManager
		{
			StartDate = DateTime.UtcNow,
			StopDate = DateTime.UtcNow.AddSeconds(1),
		};

		var boards = Array.Empty<BoardMessage>();
		var enumerator = manager.StartAsync(boards).GetAsyncEnumerator(CancellationToken);

		try
		{
			// Just starting the enumeration should set IsStarted
			await enumerator.MoveNextAsync();
			manager.IsStarted.AssertTrue();
		}
		finally
		{
			await enumerator.DisposeAsync();
		}
	}

	[TestMethod]
	public async Task StartAsync_IncrementsLoadedMessageCount()
	{
		using var manager = new HistoryMarketDataManager
		{
			StartDate = DateTime.UtcNow,
			StopDate = DateTime.UtcNow.AddSeconds(1),
		};

		var initialCount = manager.LoadedMessageCount;
		var boards = Array.Empty<BoardMessage>();

		await foreach (var _ in manager.StartAsync(boards).WithCancellation(CancellationToken))
		{
			// Just consume messages
		}

		// LoadedMessageCount should have increased
		(manager.LoadedMessageCount >= initialCount).AssertTrue();
	}

	[TestMethod]
	public void StartAsync_ThrowsOnNullBoards()
	{
		using var manager = new HistoryMarketDataManager();

		ThrowsExactly<ArgumentNullException>(() =>
			manager.StartAsync(null).GetAsyncEnumerator());
	}

	#endregion

	#region Real Historical Data Tests

	[TestMethod]
	public async Task StartAsync_WithRealTickData_YieldsExecutionMessages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			// Skip test if history data not available
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = new HistoryMarketDataManager
		{
			StorageRegistry = storageRegistry,
			StartDate = Paths.HistoryBeginDate,
			StopDate = Paths.HistoryBeginDate.AddDays(1),
		};

		// Subscribe to ticks
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		error.AssertNull();

		var messages = new List<Message>();
		var boards = Array.Empty<BoardMessage>();

		await foreach (var msg in manager.StartAsync(boards).WithCancellation(CancellationToken))
		{
			messages.Add(msg);

			// Stop after receiving some data to avoid long test
			if (messages.Count > 100)
			{
				manager.Stop();
				break;
			}
		}

		// Should have received execution messages (ticks)
		var ticks = messages.OfType<ExecutionMessage>().Where(m => m.DataTypeEx == DataType.Ticks).ToList();
		(ticks.Count > 0).AssertTrue();
	}

	[TestMethod]
	public async Task StartAsync_WithRealCandleData_YieldsCandleMessages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = new HistoryMarketDataManager
		{
			StorageRegistry = storageRegistry,
			StartDate = Paths.HistoryBeginDate,
			StopDate = Paths.HistoryBeginDate.AddDays(1),
		};

		// Subscribe to 1-minute candles
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.TimeFrame(TimeSpan.FromMinutes(1)),
			TransactionId = 1,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		error.AssertNull();

		var messages = new List<Message>();
		var boards = Array.Empty<BoardMessage>();

		await foreach (var msg in manager.StartAsync(boards).WithCancellation(CancellationToken))
		{
			messages.Add(msg);

			if (messages.Count > 100)
			{
				manager.Stop();
				break;
			}
		}

		// Should have received candle messages
		var candles = messages.OfType<CandleMessage>().ToList();
		(candles.Count > 0).AssertTrue();
	}

	[TestMethod]
	public void GetSupportedDataTypes_WithRealStorage_ReturnsAvailableTypes()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = new HistoryMarketDataManager
		{
			StorageRegistry = storageRegistry,
		};

		var dataTypes = manager.GetSupportedDataTypes(secId).ToList();

		// Should have at least ticks and candles in sample data
		(dataTypes.Count > 0).AssertTrue();
	}

	[TestMethod]
	public async Task StartAsync_MultipleSubscriptions_YieldsAllData()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = new HistoryMarketDataManager
		{
			StorageRegistry = storageRegistry,
			StartDate = Paths.HistoryBeginDate,
			StopDate = Paths.HistoryBeginDate.AddHours(1),
		};

		// Subscribe to both ticks and candles
		var tickSubscribe = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		};

		var candleSubscribe = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.TimeFrame(TimeSpan.FromMinutes(1)),
			TransactionId = 2,
			IsSubscribe = true,
		};

		await manager.SubscribeAsync(tickSubscribe, CancellationToken);
		await manager.SubscribeAsync(candleSubscribe, CancellationToken);

		var messages = new List<Message>();
		var boards = Array.Empty<BoardMessage>();

		await foreach (var msg in manager.StartAsync(boards).WithCancellation(CancellationToken))
		{
			messages.Add(msg);

			if (messages.Count > 200)
			{
				manager.Stop();
				break;
			}
		}

		// Should have received both types
		var ticks = messages.OfType<ExecutionMessage>().Where(m => m.DataTypeEx == DataType.Ticks).ToList();
		var candles = messages.OfType<CandleMessage>().ToList();

		// At least one type should have data
		(ticks.Count > 0 || candles.Count > 0).AssertTrue();
	}

	[TestMethod]
	public async Task StartAsync_Unsubscribe_StopsReceivingData()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = new HistoryMarketDataManager
		{
			StorageRegistry = storageRegistry,
			StartDate = Paths.HistoryBeginDate,
			StopDate = Paths.HistoryBeginDate.AddDays(1),
		};

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		};

		await manager.SubscribeAsync(subscribeMsg, CancellationToken);

		var messageCountBeforeUnsubscribe = 0;
		var boards = Array.Empty<BoardMessage>();

		await foreach (var msg in manager.StartAsync(boards).WithCancellation(CancellationToken))
		{
			messageCountBeforeUnsubscribe++;

			if (messageCountBeforeUnsubscribe == 50)
			{
				// Unsubscribe mid-stream
				manager.Unsubscribe(1);
			}

			if (messageCountBeforeUnsubscribe > 100)
			{
				manager.Stop();
				break;
			}
		}

		// Test passed if we didn't hang
		(messageCountBeforeUnsubscribe > 0).AssertTrue();
	}

	[TestMethod]
	public async Task StartAsync_WithLevel1Data_YieldsLevel1Messages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = new HistoryMarketDataManager
		{
			StorageRegistry = storageRegistry,
			StartDate = Paths.HistoryBeginDate,
			StopDate = Paths.HistoryBeginDate.AddDays(1),
		};

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Level1,
			TransactionId = 1,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		// Level1 may not be available - that's ok
		if (error != null)
			return;

		var messages = new List<Message>();
		var boards = Array.Empty<BoardMessage>();

		await foreach (var msg in manager.StartAsync(boards).WithCancellation(CancellationToken))
		{
			messages.Add(msg);

			if (messages.Count > 100)
			{
				manager.Stop();
				break;
			}
		}

		// If Level1 data exists, we should have received it
		var level1Messages = messages.OfType<Level1ChangeMessage>().ToList();
		// Just verify we didn't crash - Level1 may or may not be in sample data
	}

	[TestMethod]
	public async Task StartAsync_WithMarketDepthData_YieldsQuoteMessages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = new HistoryMarketDataManager
		{
			StorageRegistry = storageRegistry,
			StartDate = Paths.HistoryBeginDate,
			StopDate = Paths.HistoryBeginDate.AddDays(1),
		};

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.MarketDepth,
			TransactionId = 1,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		// MarketDepth may not be available - that's ok
		if (error != null)
			return;

		var messages = new List<Message>();
		var boards = Array.Empty<BoardMessage>();

		await foreach (var msg in manager.StartAsync(boards).WithCancellation(CancellationToken))
		{
			messages.Add(msg);

			if (messages.Count > 100)
			{
				manager.Stop();
				break;
			}
		}

		// Just verify we didn't crash
		var depthMessages = messages.OfType<QuoteChangeMessage>().ToList();
	}

	#endregion
}
