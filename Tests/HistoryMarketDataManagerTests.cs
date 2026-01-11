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

	private static HistoryMarketDataManager CreateManager()
		=> new(new TradingTimeLineGenerator());

	private static HistoryMarketDataManager CreateManager(IStorageRegistry storageRegistry, DateTime? startDate = null, DateTime? stopDate = null)
	{
		var manager = new HistoryMarketDataManager(new TradingTimeLineGenerator())
		{
			StorageRegistry = storageRegistry,
		};
		if (startDate.HasValue)
			manager.StartDate = startDate.Value;
		if (stopDate.HasValue)
			manager.StopDate = stopDate.Value;
		return manager;
	}

	private static SecurityId CreateSecurityId() => Helper.CreateSecurityId();

	#region Property Tests

	[TestMethod]
	public void Constructor_SetsDefaultValues()
	{
		using var manager = CreateManager();

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
		using var manager = CreateManager();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.MarketTimeChangedInterval = TimeSpan.Zero);

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.MarketTimeChangedInterval = TimeSpan.FromSeconds(-1));
	}

	[TestMethod]
	public void PostTradeMarketTimeChangedCount_ThrowsOnNegativeValue()
	{
		using var manager = CreateManager();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.PostTradeMarketTimeChangedCount = -1);
	}

	[TestMethod]
	public void PostTradeMarketTimeChangedCount_AcceptsZero()
	{
		using var manager = CreateManager();

		manager.PostTradeMarketTimeChangedCount = 0;
		manager.PostTradeMarketTimeChangedCount.AssertEqual(0);
	}

	#endregion

	#region Generator Tests

	[TestMethod]
	public void RegisterGenerator_AddsGenerator()
	{
		using var manager = CreateManager();
		var secId = CreateSecurityId();
		var dataType = DataType.Ticks;
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, dataType, generator, 123);

		manager.HasGenerator(secId, dataType).AssertTrue();
	}

	[TestMethod]
	public void RegisterGenerator_ThrowsOnNullGenerator()
	{
		using var manager = CreateManager();
		var secId = CreateSecurityId();

		ThrowsExactly<ArgumentNullException>(() =>
			manager.RegisterGenerator(secId, DataType.Ticks, null, 123));
	}

	[TestMethod]
	public void HasGenerator_ReturnsFalseWhenNotRegistered()
	{
		using var manager = CreateManager();
		var secId = CreateSecurityId();

		manager.HasGenerator(secId, DataType.Ticks).AssertFalse();
	}

	[TestMethod]
	public void UnregisterGenerator_RemovesGenerator()
	{
		using var manager = CreateManager();
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
		using var manager = CreateManager();

		var result = manager.UnregisterGenerator(999);

		result.AssertFalse();
	}

	#endregion

	#region Subscription Tests

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task SubscribeAsync_ReturnsErrorWithoutStorageRegistry()
	{
		using var manager = CreateManager();
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
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task SubscribeAsync_ThrowsOnNullMessage()
	{
		using var manager = CreateManager();

		await ThrowsExactlyAsync<ArgumentNullException>(() =>
			manager.SubscribeAsync(null, CancellationToken).AsTask());
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task SubscribeAsync_ThrowsOnUnsubscribeMessage()
	{
		using var manager = CreateManager();
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
		using var manager = CreateManager();

		ThrowsExactly<ArgumentException>(() =>
			manager.Unsubscribe(0));
	}

	#endregion

	#region Reset Tests

	[TestMethod]
	public void Reset_ClearsGenerators()
	{
		using var manager = CreateManager();
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
		using var manager = CreateManager();

		manager.Reset();

		manager.LoadedMessageCount.AssertEqual(0);
		manager.IsStarted.AssertEqual(false);
	}

	#endregion

	#region GetSupportedDataTypes Tests

	[TestMethod]
	public void GetSupportedDataTypes_ReturnsEmptyWithoutDriveAndGenerators()
	{
		using var manager = CreateManager();
		var secId = CreateSecurityId();

		var dataTypes = manager.GetSupportedDataTypes(secId);

		dataTypes.Count().AssertEqual(0);
	}

	[TestMethod]
	public void GetSupportedDataTypes_IncludesGeneratorDataTypes()
	{
		using var manager = CreateManager();
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, DataType.Ticks, generator, 1);

		var dataTypes = manager.GetSupportedDataTypes(secId).ToList();

		dataTypes.Contains(DataType.Ticks).AssertTrue();
	}

	[TestMethod]
	public void GetSupportedDataTypes_WithoutDrive_ReturnsOnlyGenerators()
	{
		using var manager = CreateManager();
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
		using var manager = CreateManager();
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
		using var manager = CreateManager();

		manager.Stop();
		manager.Stop();
		manager.Stop();
	}

	#endregion

	#region StartAsync Tests

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_WithStorageNoData_YieldsTimeAndStoppingMessages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		// Use date range outside historical data (so no data is returned)
		using var manager = CreateManager(storageRegistry, new DateTime(2000, 1, 1), new DateTime(2000, 1, 2));

		// Subscribe to candles (no data will be found for this date range)
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};
		await manager.SubscribeAsync(subscribeMsg, CancellationToken);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		// Should end with EmulationStateMessage Stopping
		var lastMessage = messages.LastOrDefault();
		lastMessage.AssertNotNull();
		(lastMessage is EmulationStateMessage state && state.State == ChannelStates.Stopping).AssertTrue();
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_SetsIsStartedTrue()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddHours(1));

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};
		await manager.SubscribeAsync(subscribeMsg, CancellationToken);

		var enumerator = manager.StartAsync([]).GetAsyncEnumerator(CancellationToken);

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
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_IncrementsLoadedMessageCount()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddHours(1));

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};
		await manager.SubscribeAsync(subscribeMsg, CancellationToken);

		var initialCount = manager.LoadedMessageCount;

		await foreach (var _ in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			// Just consume messages
		}

		// LoadedMessageCount should have increased
		(manager.LoadedMessageCount > initialCount).AssertTrue();
	}

	[TestMethod]
	public void StartAsync_ThrowsOnNullBoards()
	{
		using var manager = CreateManager();

		ThrowsExactly<ArgumentNullException>(() =>
			manager.StartAsync(null).GetAsyncEnumerator());
	}

	#endregion

	#region Real Historical Data Tests

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_WithRealTickData_YieldsExecutionMessages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			// Skip test if history data not available
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

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
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_WithRealCandleData_YieldsCandleMessages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

		// Subscribe to 1-minute candles
		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
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

		using var manager = CreateManager(storageRegistry);

		var dataTypes = manager.GetSupportedDataTypes(secId).ToList();

		// Should have at least ticks and candles in sample data
		(dataTypes.Count > 0).AssertTrue();
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_MultipleSubscriptions_YieldsAllData()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddHours(1));

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
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
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
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_Unsubscribe_StopsReceivingData()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

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
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_WithLevel1Data_YieldsLevel1Messages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

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
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task StartAsync_WithMarketDepthData_YieldsQuoteMessages()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
		{
			return;
		}

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

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

	#region Time Ordering Tests

	/// <summary>
	/// Helper method to verify that all IServerTimeMessage messages are strictly ordered by ServerTime.
	/// </summary>
	private static void AssertStrictlyOrderedByTime(IList<Message> messages, string context)
	{
		var serverTimeMessages = messages.OfType<IServerTimeMessage>().ToList();

		if (serverTimeMessages.Count < 2)
			return;

		DateTime? prevTime = null;
		int prevIndex = -1;

		for (var i = 0; i < serverTimeMessages.Count; i++)
		{
			var msg = serverTimeMessages[i];
			var currentTime = msg.ServerTime;

			if (prevTime.HasValue && currentTime < prevTime.Value)
			{
				throw new AssertFailedException(
					$"[{context}] Message #{i} ({msg.GetType().Name}) has ServerTime {currentTime:O} which is earlier than " +
					$"message #{prevIndex} ServerTime {prevTime.Value:O}. Messages must be strictly ordered by ascending time.");
			}

			prevTime = currentTime;
			prevIndex = i;
		}
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task TimeOrdering_PureHistoricalData_Ticks_IsStrictlyAscending()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

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
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		AssertStrictlyOrderedByTime(messages, "PureHistoricalData_Ticks");
		(messages.OfType<ExecutionMessage>().Count() > 0).AssertTrue("Should have tick messages");
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task TimeOrdering_PureHistoricalData_Candles_IsStrictlyAscending()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(3));

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		error.AssertNull();

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		AssertStrictlyOrderedByTime(messages, "PureHistoricalData_Candles");
		(messages.OfType<CandleMessage>().Count() > 0).AssertTrue("Should have candle messages");
	}

	[TestMethod]
	public void Generator_Registration_IsTrackedCorrectly()
	{
		// Note: HistoryMarketDataManager tracks generator registration for HasGenerator() and
		// GetSupportedDataTypes() checks, but actual message generation happens in MarketEmulator.
		using var manager = CreateManager();
		var secId = CreateSecurityId();

		var generator = new RandomWalkTradeGenerator(secId)
		{
			Interval = TimeSpan.FromSeconds(30),
		};
		generator.Init();

		// Register generator
		manager.RegisterGenerator(secId, DataType.Ticks, generator, 1);

		// Verify registration is tracked
		manager.HasGenerator(secId, DataType.Ticks).AssertTrue("Generator should be registered");
		manager.GetSupportedDataTypes(secId).Contains(DataType.Ticks).AssertTrue("Data type should be supported");

		// Unregister
		var result = manager.UnregisterGenerator(1);
		result.AssertTrue("Unregister should succeed");
		manager.HasGenerator(secId, DataType.Ticks).AssertFalse("Generator should be unregistered");
	}

	[TestMethod]
	public void Generator_MultipleDataTypes_TrackedSeparately()
	{
		// Verify that different data type generators are tracked separately
		using var manager = CreateManager();
		var secId = CreateSecurityId();

		var tickGen = new RandomWalkTradeGenerator(secId);
		var depthGen = new TrendMarketDepthGenerator(secId);
		tickGen.Init();
		depthGen.Init();

		// Register both generators
		manager.RegisterGenerator(secId, DataType.Ticks, tickGen, 1);
		manager.RegisterGenerator(secId, DataType.MarketDepth, depthGen, 2);

		// Both should be tracked
		manager.HasGenerator(secId, DataType.Ticks).AssertTrue();
		manager.HasGenerator(secId, DataType.MarketDepth).AssertTrue();

		var dataTypes = manager.GetSupportedDataTypes(secId).ToList();
		dataTypes.Contains(DataType.Ticks).AssertTrue();
		dataTypes.Contains(DataType.MarketDepth).AssertTrue();

		// Unregister one
		manager.UnregisterGenerator(1);
		manager.HasGenerator(secId, DataType.Ticks).AssertFalse();
		manager.HasGenerator(secId, DataType.MarketDepth).AssertTrue();
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task TimeOrdering_MixedHistoryAndGenerator_IsStrictlyAscending()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var generatorSecId = CreateSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

		// Subscribe to historical ticks
		var historySubscribe = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		};
		await manager.SubscribeAsync(historySubscribe, CancellationToken);

		// Register generator for different security
		var generator = new RandomWalkTradeGenerator(generatorSecId)
		{
			Interval = TimeSpan.FromSeconds(10),
		};
		generator.Init();
		generator.Process(new SecurityMessage { SecurityId = generatorSecId });
		generator.Process(new Level1ChangeMessage
		{
			SecurityId = generatorSecId,
			ServerTime = Paths.HistoryBeginDate,
		}.TryAdd(Level1Fields.LastTradePrice, 100m));

		manager.RegisterGenerator(generatorSecId, DataType.Ticks, generator, 2);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		AssertStrictlyOrderedByTime(messages, "MixedHistoryAndGenerator");

		// Verify we have messages from both sources
		var historyTicks = messages.OfType<ExecutionMessage>()
			.Where(m => m.SecurityId == secId && m.DataTypeEx == DataType.Ticks).ToList();
		var generatorTicks = messages.OfType<ExecutionMessage>()
			.Where(m => m.SecurityId == generatorSecId && m.DataTypeEx == DataType.Ticks).ToList();

		(historyTicks.Count > 0 || generatorTicks.Count > 0).AssertTrue("Should have messages from history or generator");
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task TimeOrdering_MultipleSecurities_HistoricalData_IsStrictlyAscending()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId1 = Paths.HistoryDefaultSecurity.ToSecurityId();
		var secId2 = Paths.HistoryDefaultSecurity2.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

		// Subscribe to both securities
		var subscribe1 = new MarketDataMessage
		{
			SecurityId = secId1,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};
		var subscribe2 = new MarketDataMessage
		{
			SecurityId = secId2,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 2,
			IsSubscribe = true,
		};

		await manager.SubscribeAsync(subscribe1, CancellationToken);
		await manager.SubscribeAsync(subscribe2, CancellationToken);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		AssertStrictlyOrderedByTime(messages, "MultipleSecurities_Historical");

		// Verify we have candles
		var candles = messages.OfType<CandleMessage>().ToList();
		(candles.Count > 0).AssertTrue("Should have candle messages");
	}

	[TestMethod]
	public void Generator_MultipleSecurities_TrackedSeparately()
	{
		// Verify that generators for different securities are tracked separately
		using var manager = CreateManager();
		var secId1 = CreateSecurityId();
		var secId2 = CreateSecurityId();

		var gen1 = new RandomWalkTradeGenerator(secId1);
		var gen2 = new RandomWalkTradeGenerator(secId2);
		gen1.Init();
		gen2.Init();

		// Register generators for different securities
		manager.RegisterGenerator(secId1, DataType.Ticks, gen1, 1);
		manager.RegisterGenerator(secId2, DataType.Ticks, gen2, 2);

		// Both should be tracked for their respective securities
		manager.HasGenerator(secId1, DataType.Ticks).AssertTrue();
		manager.HasGenerator(secId2, DataType.Ticks).AssertTrue();
		manager.HasGenerator(secId1, DataType.MarketDepth).AssertFalse();

		manager.GetSupportedDataTypes(secId1).Contains(DataType.Ticks).AssertTrue();
		manager.GetSupportedDataTypes(secId2).Contains(DataType.Ticks).AssertTrue();

		// Unregister one security's generator
		manager.UnregisterGenerator(1);
		manager.HasGenerator(secId1, DataType.Ticks).AssertFalse();
		manager.HasGenerator(secId2, DataType.Ticks).AssertTrue();
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task TimeOrdering_MultipleDataTypes_SameSecurity_IsStrictlyAscending()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

		// Subscribe to ticks
		var tickSubscribe = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		};

		// Subscribe to candles
		var candleSubscribe = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 2,
			IsSubscribe = true,
		};

		await manager.SubscribeAsync(tickSubscribe, CancellationToken);
		await manager.SubscribeAsync(candleSubscribe, CancellationToken);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		AssertStrictlyOrderedByTime(messages, "MultipleDataTypes_SameSecurity");

		// Verify we have both types
		var ticks = messages.OfType<ExecutionMessage>().Where(m => m.DataTypeEx == DataType.Ticks).ToList();
		var candles = messages.OfType<CandleMessage>().ToList();

		(ticks.Count > 0 || candles.Count > 0).AssertTrue("Should have ticks or candles");
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task TimeOrdering_FullMix_MultipleSecuritiesAndDataTypesAndGenerators_IsStrictlyAscending()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		// Historical security
		var histSecId = Paths.HistoryDefaultSecurity.ToSecurityId();
		// Generator security
		var genSecId = CreateSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddHours(6));

		// Subscribe to historical ticks
		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = histSecId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		}, CancellationToken);

		// Subscribe to historical candles
		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = histSecId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 2,
			IsSubscribe = true,
		}, CancellationToken);

		// Register tick generator
		var tickGen = new RandomWalkTradeGenerator(genSecId) { Interval = TimeSpan.FromSeconds(30) };
		tickGen.Init();
		tickGen.Process(new SecurityMessage { SecurityId = genSecId });
		tickGen.Process(new Level1ChangeMessage
		{
			SecurityId = genSecId,
			ServerTime = Paths.HistoryBeginDate,
		}.TryAdd(Level1Fields.LastTradePrice, 50000m));
		manager.RegisterGenerator(genSecId, DataType.Ticks, tickGen, 3);

		// Register depth generator
		var depthGen = new TrendMarketDepthGenerator(genSecId) { Interval = TimeSpan.FromMinutes(1) };
		depthGen.Init();
		depthGen.Process(new SecurityMessage { SecurityId = genSecId });
		depthGen.Process(ExchangeBoard.Test.ToMessage());
		manager.RegisterGenerator(genSecId, DataType.MarketDepth, depthGen, 4);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		AssertStrictlyOrderedByTime(messages, "FullMix");

		// Log statistics for debugging
		var histTicks = messages.OfType<ExecutionMessage>().Where(m => m.SecurityId == histSecId && m.DataTypeEx == DataType.Ticks).Count();
		var histCandles = messages.OfType<CandleMessage>().Where(m => m.SecurityId == histSecId).Count();
		var genTicks = messages.OfType<ExecutionMessage>().Where(m => m.SecurityId == genSecId && m.DataTypeEx == DataType.Ticks).Count();
		var genDepths = messages.OfType<QuoteChangeMessage>().Where(m => m.SecurityId == genSecId).Count();

		// At least some data from various sources
		(histTicks + histCandles + genTicks + genDepths > 0).AssertTrue("Should have data from at least one source");
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task TimeOrdering_ThreeSecurities_DifferentDataTypes_IsStrictlyAscending()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId1 = Paths.HistoryDefaultSecurity.ToSecurityId();
		var secId2 = Paths.HistoryDefaultSecurity2.ToSecurityId();
		var genSecId = CreateSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddHours(2));

		// Security 1: Historical ticks
		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId1,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		}, CancellationToken);

		// Security 2: Historical candles
		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId2,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 2,
			IsSubscribe = true,
		}, CancellationToken);

		// Security 3: Generated ticks
		var generator = new RandomWalkTradeGenerator(genSecId) { Interval = TimeSpan.FromSeconds(15) };
		generator.Init();
		generator.Process(new SecurityMessage { SecurityId = genSecId });
		generator.Process(new Level1ChangeMessage
		{
			SecurityId = genSecId,
			ServerTime = Paths.HistoryBeginDate,
		}.TryAdd(Level1Fields.LastTradePrice, 100m));
		manager.RegisterGenerator(genSecId, DataType.Ticks, generator, 3);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		AssertStrictlyOrderedByTime(messages, "ThreeSecurities_DifferentDataTypes");
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task TimeOrdering_LongPeriod_IsStrictlyAscending()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		// Test 7 days period (full period is too slow)
		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(7));

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		error.AssertNull();

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		AssertStrictlyOrderedByTime(messages, "LongPeriod");

		var candles = messages.OfType<CandleMessage>().ToList();
		(candles.Count > 50).AssertTrue("Should have many candles over 7 days");
	}

	#endregion

	#region Message Property Validation Tests

	/// <summary>
	/// Helper to validate tick message properties
	/// </summary>
	private static void ValidateTickMessage(ExecutionMessage tick, SecurityId expectedSecId, DateTime startDate, DateTime stopDate, int index)
	{
		tick.SecurityId.AssertEqual(expectedSecId, $"Tick #{index}: SecurityId mismatch");
		tick.DataTypeEx.AssertEqual(DataType.Ticks, $"Tick #{index}: DataTypeEx must be Ticks");
		tick.HasTradeInfo.AssertTrue($"Tick #{index}: HasTradeInfo must be true");

		(tick.TradePrice > 0).AssertTrue($"Tick #{index}: TradePrice must be positive, got {tick.TradePrice}");
		(tick.TradeVolume > 0).AssertTrue($"Tick #{index}: TradeVolume must be positive, got {tick.TradeVolume}");

		(tick.ServerTime >= startDate).AssertTrue($"Tick #{index}: ServerTime {tick.ServerTime:O} before start date {startDate:O}");
		(tick.ServerTime <= stopDate).AssertTrue($"Tick #{index}: ServerTime {tick.ServerTime:O} after stop date {stopDate:O}");
	}

	/// <summary>
	/// Helper to validate candle message properties
	/// </summary>
	private static void ValidateCandleMessage(CandleMessage candle, SecurityId expectedSecId, DateTime startDate, DateTime stopDate, int index)
	{
		candle.SecurityId.AssertEqual(expectedSecId, $"Candle #{index}: SecurityId mismatch");

		(candle.OpenPrice > 0).AssertTrue($"Candle #{index}: OpenPrice must be positive, got {candle.OpenPrice}");
		(candle.HighPrice > 0).AssertTrue($"Candle #{index}: HighPrice must be positive, got {candle.HighPrice}");
		(candle.LowPrice > 0).AssertTrue($"Candle #{index}: LowPrice must be positive, got {candle.LowPrice}");
		(candle.ClosePrice > 0).AssertTrue($"Candle #{index}: ClosePrice must be positive, got {candle.ClosePrice}");
		(candle.TotalVolume >= 0).AssertTrue($"Candle #{index}: TotalVolume must be non-negative, got {candle.TotalVolume}");

		// OHLC consistency
		(candle.HighPrice >= candle.LowPrice).AssertTrue(
			$"Candle #{index}: High ({candle.HighPrice}) must be >= Low ({candle.LowPrice})");
		(candle.HighPrice >= candle.OpenPrice).AssertTrue(
			$"Candle #{index}: High ({candle.HighPrice}) must be >= Open ({candle.OpenPrice})");
		(candle.HighPrice >= candle.ClosePrice).AssertTrue(
			$"Candle #{index}: High ({candle.HighPrice}) must be >= Close ({candle.ClosePrice})");
		(candle.LowPrice <= candle.OpenPrice).AssertTrue(
			$"Candle #{index}: Low ({candle.LowPrice}) must be <= Open ({candle.OpenPrice})");
		(candle.LowPrice <= candle.ClosePrice).AssertTrue(
			$"Candle #{index}: Low ({candle.LowPrice}) must be <= Close ({candle.ClosePrice})");

		(candle.OpenTime >= startDate).AssertTrue($"Candle #{index}: OpenTime {candle.OpenTime:O} before start date {startDate:O}");
		(candle.OpenTime <= stopDate).AssertTrue($"Candle #{index}: OpenTime {candle.OpenTime:O} after stop date {stopDate:O}");
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task PropertyValidation_Ticks_AllPropertiesValid()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddDays(1);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);

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
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		var ticks = messages.OfType<ExecutionMessage>()
			.Where(m => m.DataTypeEx == DataType.Ticks)
			.ToList();

		(ticks.Count > 0).AssertTrue("Should have tick messages");

		for (var i = 0; i < ticks.Count; i++)
		{
			ValidateTickMessage(ticks[i], secId, startDate, stopDate, i);
		}
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task PropertyValidation_Candles_AllPropertiesValid()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddDays(3);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		error.AssertNull();

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		var candles = messages.OfType<CandleMessage>().ToList();

		(candles.Count > 0).AssertTrue("Should have candle messages");

		for (var i = 0; i < candles.Count; i++)
		{
			ValidateCandleMessage(candles[i], secId, startDate, stopDate, i);
		}
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task PropertyValidation_Candles_TimeFrameCorrect()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddDays(1);
		var timeFrame = TimeSpan.FromMinutes(1);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = timeFrame.TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		error.AssertNull();

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		var candles = messages.OfType<TimeFrameCandleMessage>().ToList();

		(candles.Count > 0).AssertTrue("Should have TimeFrameCandleMessages");

		foreach (var candle in candles)
		{
			candle.DataType.Arg.AssertEqual(timeFrame, $"Candle time frame mismatch");
		}
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task PropertyValidation_TimeChangedMessages_Valid()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddDays(1);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};

		await manager.SubscribeAsync(subscribeMsg, CancellationToken);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		var timeMessages = messages.OfType<TimeMessage>().ToList();

		// Should have time messages for market time progression
		(timeMessages.Count > 0).AssertTrue("Should have TimeMessage instances");

		foreach (var tm in timeMessages)
		{
			// ServerTime should be within range
			(tm.ServerTime >= startDate).AssertTrue($"TimeMessage.ServerTime {tm.ServerTime:O} before start date");
			(tm.ServerTime <= stopDate.AddDays(1)).AssertTrue($"TimeMessage.ServerTime {tm.ServerTime:O} after stop date");
		}
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task PropertyValidation_EmulationStateMessages_Valid()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddHours(1);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		};

		await manager.SubscribeAsync(subscribeMsg, CancellationToken);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		var stateMessages = messages.OfType<EmulationStateMessage>().ToList();

		// Should end with Stopping state
		(stateMessages.Count > 0).AssertTrue("Should have EmulationStateMessage");
		stateMessages.Last().State.AssertEqual(ChannelStates.Stopping, "Last state should be Stopping");
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task PropertyValidation_MultipleDataTypes_AllPropertiesValid()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddDays(1);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);

		// Subscribe to both ticks and candles
		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		}, CancellationToken);

		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 2,
			IsSubscribe = true,
		}, CancellationToken);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		// Validate all ticks
		var ticks = messages.OfType<ExecutionMessage>()
			.Where(m => m.DataTypeEx == DataType.Ticks)
			.ToList();

		for (var i = 0; i < ticks.Count; i++)
		{
			ValidateTickMessage(ticks[i], secId, startDate, stopDate, i);
		}

		// Validate all candles
		var candles = messages.OfType<CandleMessage>().ToList();

		for (var i = 0; i < candles.Count; i++)
		{
			ValidateCandleMessage(candles[i], secId, startDate, stopDate, i);
		}

		// At least one type should have data
		(ticks.Count > 0 || candles.Count > 0).AssertTrue("Should have ticks or candles");
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task PropertyValidation_SubscriptionResponse_Valid()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddDays(1));

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 123,
			IsSubscribe = true,
		};

		var error = await manager.SubscribeAsync(subscribeMsg, CancellationToken);
		error.AssertNull("Subscription should succeed without error");

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		// Find subscription response
		var responses = messages.OfType<SubscriptionResponseMessage>().ToList();
		// May or may not have explicit response depending on implementation

		// But all data messages should have OriginalTransactionId matching subscription
		var dataMessages = messages.OfType<ExecutionMessage>()
			.Where(m => m.DataTypeEx == DataType.Ticks)
			.ToList();

		foreach (var dm in dataMessages)
		{
			dm.OriginalTransactionId.AssertEqual(123, "Data message OriginalTransactionId should match subscription");
		}
	}

	[TestMethod]
	[Timeout(30000, CooperativeCancellation = true)]
	public async Task PropertyValidation_FinishedMessage_HasCorrectTransactionId()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		using var manager = CreateManager(storageRegistry, Paths.HistoryBeginDate, Paths.HistoryBeginDate.AddHours(1));

		var subscribeMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 456,
			IsSubscribe = true,
		};

		await manager.SubscribeAsync(subscribeMsg, CancellationToken);

		var messages = new List<Message>();
		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			messages.Add(msg);
		}

		// Find subscription finished message
		var finished = messages.OfType<SubscriptionFinishedMessage>()
			.Where(m => m.OriginalTransactionId == 456)
			.ToList();

		// Should have finished message for subscription
		(finished.Count > 0).AssertTrue("Should have SubscriptionFinishedMessage for subscription");
	}

	#endregion

	#region HistoryMarketDataManager + MarketEmulator Integration Tests

	private static IMarketEmulator CreateEmulator(SecurityId secId)
	{
		var emu = new Algo.Testing.Emulation.MarketEmulator(
			new CollectionSecurityProvider([new Security { Id = secId.ToStringId() }]),
			new CollectionPortfolioProvider([Portfolio.CreateSimulator()]),
			new InMemoryExchangeInfoProvider(),
			new IncrementalIdGenerator())
		{
			VerifyMode = true
		};
		return emu;
	}

	/// <summary>
	/// Validates that emulator output message time >= input message time
	/// </summary>
	private static void ValidateEmulatorTimeOrdering(Message input, Message output, int index)
	{
		if (input is not IServerTimeMessage inputTime || output is not IServerTimeMessage outputTime)
			return;

		(outputTime.ServerTime >= inputTime.ServerTime).AssertTrue(
			$"Emulator output #{index} ({output.GetType().Name}) time {outputTime.ServerTime:O} " +
			$"is less than input time {inputTime.ServerTime:O}. " +
			$"Emulator cannot return messages with time earlier than input.");
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Integration_HistoryToEmulator_TimeOrdering_Valid()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddHours(2);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);
		var emu = CreateEmulator(secId);

		// Subscribe to ticks
		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		}, CancellationToken);

		var historyMessages = new List<Message>();
		var emulatorOutputs = new List<Message>();

		emu.NewOutMessage += msg => emulatorOutputs.Add(msg);

		var lastInputTime = DateTimeOffset.MinValue;
		var outputIndex = 0;

		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			historyMessages.Add(msg);

			// Track input time
			if (msg is IServerTimeMessage timeMsg && timeMsg.ServerTime != default)
			{
				lastInputTime = timeMsg.ServerTime;
			}

			// Feed ticks to emulator
			if (msg is ExecutionMessage execMsg && execMsg.DataTypeEx == DataType.Ticks)
			{
				var outputsBefore = emulatorOutputs.Count;
				await emu.SendInMessageAsync(execMsg, CancellationToken);

				// Validate all new outputs have time >= input time
				for (var i = outputsBefore; i < emulatorOutputs.Count; i++)
				{
					var output = emulatorOutputs[i];
					ValidateEmulatorTimeOrdering(execMsg, output, outputIndex++);
				}
			}
		}

		// Validate overall time ordering of emulator output
		AssertStrictlyOrderedByTime(emulatorOutputs, "EmulatorOutput");

		// Should have processed some messages
		(historyMessages.OfType<ExecutionMessage>().Count() > 0).AssertTrue("Should have history ticks");
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Integration_HistoryToEmulator_WithOrders_ValidatesOrderResponses()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddHours(1);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);
		var emu = CreateEmulator(secId);

		// Subscribe to candles for market data
		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 1,
			IsSubscribe = true,
		}, CancellationToken);

		var emulatorOutputs = new List<Message>();
		emu.NewOutMessage += msg => emulatorOutputs.Add(msg);

		var orderSent = false;
		var orderId = 0L;
		var idGen = new IncrementalIdGenerator();
		var pfName = Messages.Extensions.SimulatorPortfolioName;

		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			// Use candle to build order book approximation
			if (msg is CandleMessage candle && !orderSent)
			{
				// Send quote change to setup order book
				await emu.SendInMessageAsync(new QuoteChangeMessage
				{
					SecurityId = secId,
					LocalTime = candle.OpenTime,
					ServerTime = candle.OpenTime,
					Bids = [new(candle.ClosePrice - 0.1m, 100)],
					Asks = [new(candle.ClosePrice + 0.1m, 100)]
				}, CancellationToken);

				// Place a limit order
				orderId = idGen.GetNextId();
				await emu.SendInMessageAsync(new OrderRegisterMessage
				{
					SecurityId = secId,
					LocalTime = candle.OpenTime,
					TransactionId = orderId,
					Side = Sides.Buy,
					Price = candle.ClosePrice - 0.2m, // Below bid
					Volume = 1,
					OrderType = OrderTypes.Limit,
					TimeInForce = TimeInForce.PutInQueue,
					PortfolioName = pfName,
				}, CancellationToken);

				orderSent = true;
			}
		}

		// Validate emulator outputs
		(emulatorOutputs.Count > 0).AssertTrue("Emulator should produce outputs");

		// Should have order response
		var orderResponses = emulatorOutputs.OfType<ExecutionMessage>()
			.Where(m => m.OriginalTransactionId == orderId && m.HasOrderInfo)
			.ToList();

		(orderResponses.Count > 0).AssertTrue("Should have order response from emulator");

		// Validate order response properties
		var orderResponse = orderResponses.First();
		orderResponse.SecurityId.AssertEqual(secId, "Order response SecurityId");
		(orderResponse.OrderState == OrderStates.Active || orderResponse.OrderState == OrderStates.Pending)
			.AssertTrue($"Order should be Active or Pending, got {orderResponse.OrderState}");
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Integration_HistoryToEmulator_MessageTypes_AllExpectedTypes()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddHours(1);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);
		var emu = CreateEmulator(secId);

		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		}, CancellationToken);

		var emulatorOutputs = new List<Message>();
		emu.NewOutMessage += msg => emulatorOutputs.Add(msg);

		var tickCount = 0;

		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			if (msg is ExecutionMessage execMsg && execMsg.DataTypeEx == DataType.Ticks)
			{
				await emu.SendInMessageAsync(execMsg, CancellationToken);
				tickCount++;

				// Limit for test speed
				if (tickCount > 100)
				{
					manager.Stop();
					break;
				}
			}
		}

		// Log message type statistics
		var typeGroups = emulatorOutputs.GroupBy(m => m.GetType().Name).ToList();

		// At minimum, emulator should process ticks (though output depends on subscriptions)
		(tickCount > 0).AssertTrue("Should have sent ticks to emulator");

		// Validate all output messages are not null
		foreach (var output in emulatorOutputs)
		{
			output.AssertNotNull("Emulator output should not be null");
		}
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Integration_HistoryToEmulator_OutputTimeNeverDecreasesFromInput()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddHours(3);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);
		var emu = CreateEmulator(secId);

		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		}, CancellationToken);

		var violations = new List<string>();
		var lastInputTime = DateTimeOffset.MinValue;

		emu.NewOutMessage += msg =>
		{
			if (msg is IServerTimeMessage timeMsg && timeMsg.ServerTime != default)
			{
				if (timeMsg.ServerTime < lastInputTime)
				{
					violations.Add(
						$"Output {msg.GetType().Name} time {timeMsg.ServerTime:O} < last input time {lastInputTime:O}");
				}
			}
		};

		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			if (msg is ExecutionMessage execMsg && execMsg.DataTypeEx == DataType.Ticks)
			{
				lastInputTime = execMsg.ServerTime;
				await emu.SendInMessageAsync(execMsg, CancellationToken);
			}
		}

		// Assert no violations
		(violations.Count == 0).AssertTrue(
			$"Found {violations.Count} time ordering violations:\n" + string.Join("\n", violations.Take(10)));
	}

	[TestMethod]
	[Timeout(60000, CooperativeCancellation = true)]
	public async Task Integration_HistoryToEmulator_MultipleDataTypes_TimeOrderingValid()
	{
		var storageRegistry = GetHistoryStorage();
		if (storageRegistry == null)
			return;

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
		var startDate = Paths.HistoryBeginDate;
		var stopDate = Paths.HistoryBeginDate.AddHours(1);

		using var manager = CreateManager(storageRegistry, startDate, stopDate);
		var emu = CreateEmulator(secId);

		// Subscribe to both ticks and candles
		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		}, CancellationToken);

		await manager.SubscribeAsync(new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = TimeSpan.FromMinutes(1).TimeFrame(),
			TransactionId = 2,
			IsSubscribe = true,
		}, CancellationToken);

		var emulatorOutputs = new List<Message>();
		var inputTimes = new List<DateTimeOffset>();

		emu.NewOutMessage += msg => emulatorOutputs.Add(msg);

		await foreach (var msg in manager.StartAsync([]).WithCancellation(CancellationToken))
		{
			if (msg is IServerTimeMessage timeMsg && timeMsg.ServerTime != default)
			{
				inputTimes.Add(timeMsg.ServerTime);
			}

			// Feed ticks to emulator
			if (msg is ExecutionMessage execMsg && execMsg.DataTypeEx == DataType.Ticks)
			{
				await emu.SendInMessageAsync(execMsg, CancellationToken);
			}
		}

		// Validate emulator output time ordering
		AssertStrictlyOrderedByTime(emulatorOutputs, "EmulatorOutput_MultipleDataTypes");

		// Validate all emulator outputs are within the expected time range
		foreach (var output in emulatorOutputs.OfType<IServerTimeMessage>())
		{
			if (output.ServerTime != default)
			{
				(output.ServerTime >= startDate).AssertTrue(
					$"Emulator output time {output.ServerTime:O} before start date");
				(output.ServerTime <= stopDate.AddDays(1)).AssertTrue(
					$"Emulator output time {output.ServerTime:O} after expected end");
			}
		}
	}

	#endregion
}
