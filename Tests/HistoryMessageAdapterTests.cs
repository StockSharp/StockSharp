namespace StockSharp.Tests;

using System.Runtime.CompilerServices;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Generation;

[TestClass]
public class HistoryMessageAdapterTests : BaseTestClass
{
	private static SecurityId CreateSecurityId() => Helper.CreateSecurityId();

	private class TestSecurityProvider : ISecurityProvider
	{
		private readonly List<Security> _securities = [];

		public int Count => _securities.Count;

		public event Action<IEnumerable<Security>> Added;
		public event Action<IEnumerable<Security>> Removed;
		public event Action Cleared;

		public ValueTask<Security> LookupByIdAsync(SecurityId id, CancellationToken cancellationToken)
			=> new(_securities.FirstOrDefault(s => s.ToSecurityId() == id));

		public async IAsyncEnumerable<Security> LookupAsync(SecurityLookupMessage criteria)
		{
			foreach (var s in _securities)
				yield return s;
		}

		public ValueTask<SecurityMessage> LookupMessageByIdAsync(SecurityId id, CancellationToken cancellationToken)
			=> new(_securities.FirstOrDefault(s => s.ToSecurityId() == id)?.ToMessage());

		public async IAsyncEnumerable<SecurityMessage> LookupMessagesAsync(SecurityLookupMessage criteria)
		{
			foreach (var s in _securities)
				yield return s.ToMessage();
		}

		public void Add(Security security) => _securities.Add(security);
	}

	private static TestSecurityProvider CreateSecurityProvider() => new();

	#region Test Implementation

	private class TestHistoryMarketDataManager : IHistoryMarketDataManager
	{
		private readonly Dictionary<(SecurityId, DataType), (MarketDataGenerator, long)> _generators = [];
		private readonly List<MarketDataMessage> _subscriptions = [];
		private readonly List<long> _unsubscriptions = [];

		public DateTime StartDate { get; set; } = DateTime.MinValue;
		public DateTime StopDate { get; set; } = DateTime.MaxValue;
		public TimeSpan MarketTimeChangedInterval { get; set; } = TimeSpan.FromSeconds(1);
		public int PostTradeMarketTimeChangedCount { get; set; } = 2;
		public bool CheckTradableDates { get; set; }
		public IStorageRegistry StorageRegistry { get; set; }
		public IMarketDataDrive Drive { get; set; }
		public StorageFormats StorageFormat { get; set; }
		public MarketDataStorageCache StorageCache { get; set; }
		public MarketDataStorageCache AdapterCache { get; set; }
		public int LoadedMessageCount { get; set; }
		public DateTime CurrentTime { get; set; }
		public bool IsStarted { get; set; }

		public bool ResetCalled { get; private set; }
		public bool StopCalled { get; private set; }
		public IReadOnlyList<MarketDataMessage> Subscriptions => _subscriptions;
		public IReadOnlyList<long> Unsubscriptions => _unsubscriptions;

		// For controlling StartAsync behavior
		public List<Message> MessagesToYield { get; } = [];
		public Exception ExceptionToThrow { get; set; }
		public bool ShouldWaitForCancellation { get; set; }

		public ValueTask<Exception> SubscribeAsync(MarketDataMessage message, CancellationToken cancellationToken)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			_subscriptions.Add(message);
			return new ValueTask<Exception>((Exception)null);
		}

		public void Unsubscribe(long originalTransactionId)
		{
			_unsubscriptions.Add(originalTransactionId);
		}

		public void RegisterGenerator(SecurityId securityId, DataType dataType, MarketDataGenerator generator, long transactionId)
		{
			_generators[(securityId, dataType)] = (generator, transactionId);
		}

		public bool UnregisterGenerator(long originalTransactionId)
		{
			var key = _generators.FirstOrDefault(p => p.Value.Item2 == originalTransactionId).Key;
			if (key == default)
				return false;

			_generators.Remove(key);
			return true;
		}

		public bool HasGenerator(SecurityId securityId, DataType dataType)
			=> _generators.ContainsKey((securityId, dataType));

		public IEnumerable<DataType> GetSupportedDataTypes(SecurityId securityId)
			=> _generators.Where(g => g.Key.Item1 == securityId).Select(g => g.Key.Item2);

		public IAsyncEnumerable<Message> StartAsync(IEnumerable<BoardMessage> boards)
		{
			IsStarted = true;

			if (ExceptionToThrow != null)
				throw ExceptionToThrow;

			return Impl();

			async IAsyncEnumerable<Message> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				foreach (var msg in MessagesToYield)
				{
					cancellationToken.ThrowIfCancellationRequested();
					yield return msg;
				}

				if (ShouldWaitForCancellation)
				{
					await Task.Delay(Timeout.Infinite, cancellationToken);
				}

				yield return new EmulationStateMessage
				{
					LocalTime = StopDate,
					State = ChannelStates.Stopping,
				};

				IsStarted = false;
			}
		}

		public void Stop()
		{
			StopCalled = true;
			IsStarted = false;
		}

		public void Reset()
		{
			ResetCalled = true;
			_generators.Clear();
			_subscriptions.Clear();
			_unsubscriptions.Clear();
			IsStarted = false;
		}

		public void Dispose()
		{
		}
	}

	#endregion

	#region Constructor Tests

	[TestMethod]
	public void Constructor_WithManager_UsesProvidedManager()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.SecurityProvider.AssertEqual(secProvider);
	}

	[TestMethod]
	public void Constructor_ThrowsOnNullSecurityProvider()
	{
		var manager = new TestHistoryMarketDataManager();

		ThrowsExactly<ArgumentNullException>(() =>
			new HistoryMessageAdapter(new IncrementalIdGenerator(), null, manager));
	}

	[TestMethod]
	public void Constructor_ThrowsOnNullManager()
	{
		var secProvider = CreateSecurityProvider();

		ThrowsExactly<ArgumentNullException>(() =>
			new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider, null));
	}

	#endregion

	#region Property Delegation Tests

	[TestMethod]
	public void Properties_DelegateToManager()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var startDate = DateTime.UtcNow;
		var stopDate = DateTime.UtcNow.AddDays(1);
		var interval = TimeSpan.FromMinutes(5);

		adapter.StartDate = startDate;
		adapter.StopDate = stopDate;
		adapter.MarketTimeChangedInterval = interval;
		adapter.PostTradeMarketTimeChangedCount = 5;
		adapter.CheckTradableDates = true;

		manager.StartDate.AssertEqual(startDate);
		manager.StopDate.AssertEqual(stopDate);
		manager.MarketTimeChangedInterval.AssertEqual(interval);
		manager.PostTradeMarketTimeChangedCount.AssertEqual(5);
		manager.CheckTradableDates.AssertEqual(true);
	}

	[TestMethod]
	public void LoadedMessageCount_DelegatesToManager()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager { LoadedMessageCount = 42 };

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.LoadedMessageCount.AssertEqual(42);
	}

	[TestMethod]
	public void CurrentTimeUtc_DelegatesToManager()
	{
		var secProvider = CreateSecurityProvider();
		var expectedTime = DateTime.UtcNow;
		var manager = new TestHistoryMarketDataManager { CurrentTime = expectedTime };

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.CurrentTimeUtc.AssertEqual(expectedTime);
	}

	#endregion

	#region Adapter Properties Tests

	[TestMethod]
	public void UseOutChannel_ReturnsFalse()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider, manager);

		adapter.UseOutChannel.AssertFalse();
	}

	[TestMethod]
	public void IsFullCandlesOnly_ReturnsFalse()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider, manager);

		adapter.IsFullCandlesOnly.AssertFalse();
	}

	[TestMethod]
	public void IsSupportCandlesUpdates_ReturnsTrue()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider, manager);

		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.CandleTimeFrame,
			IsSubscribe = true,
		};

		adapter.IsSupportCandlesUpdates(subscription).AssertTrue();
	}

	[TestMethod]
	public void IsAllDownloadingSupported_ReturnsTrueForSecurities()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider, manager);

		adapter.IsAllDownloadingSupported(DataType.Securities).AssertTrue();
	}

	#endregion

	#region Generator Tests

	[TestMethod]
	public async Task GeneratorMessage_Subscribe_RegistersGenerator()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var generatorMsg = new GeneratorMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			Generator = generator,
			TransactionId = 1,
			IsSubscribe = true,
		};

		await adapter.SendInMessageAsync(generatorMsg, CancellationToken);

		manager.HasGenerator(secId, DataType.Ticks).AssertTrue();
	}

	[TestMethod]
	public async Task GeneratorMessage_Unsubscribe_UnregistersGenerator()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, DataType.Ticks, generator, 1);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var generatorMsg = new GeneratorMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			OriginalTransactionId = 1,
			IsSubscribe = false,
		};

		await adapter.SendInMessageAsync(generatorMsg, CancellationToken);

		manager.HasGenerator(secId, DataType.Ticks).AssertFalse();
	}

	#endregion

	#region Reset Tests

	[TestMethod]
	public async Task ResetMessage_CallsManagerReset()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		manager.ResetCalled.AssertTrue();
	}

	#endregion

	#region Connect Tests

	[TestMethod]
	public async Task ConnectMessage_WhenNotStarted_SendsConnectMessage()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager { IsStarted = false };
		var outMessages = new List<Message>();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.NewOutMessage += outMessages.Add;

		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		outMessages.OfType<ConnectMessage>().Any().AssertTrue();
	}

	#endregion

	#region Disconnect Tests

	[TestMethod]
	public async Task DisconnectMessage_StopsManager()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken);

		manager.StopCalled.AssertTrue();
	}

	#endregion

	#region MarketData Tests

	[TestMethod]
	public async Task MarketDataMessage_Subscribe_CallsManagerSubscribe()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();
		var secId = CreateSecurityId();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		};

		await adapter.SendInMessageAsync(mdMsg, CancellationToken);

		manager.Subscriptions.Count.AssertEqual(1);
		manager.Subscriptions[0].SecurityId.AssertEqual(secId);
	}

	[TestMethod]
	public async Task MarketDataMessage_Unsubscribe_CallsManagerUnsubscribe()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();
		var secId = CreateSecurityId();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 2,
			OriginalTransactionId = 1,
			IsSubscribe = false,
		};

		await adapter.SendInMessageAsync(mdMsg, CancellationToken);

		manager.Unsubscriptions.Count.AssertEqual(1);
		manager.Unsubscriptions[0].AssertEqual(1);
	}

	#endregion

	#region GetSupportedMarketDataTypes Tests

	[TestMethod]
	public void GetSupportedMarketDataTypes_DelegatesToManager()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();
		var secId = CreateSecurityId();

		manager.RegisterGenerator(secId, DataType.Ticks, new RandomWalkTradeGenerator(secId), 1);
		manager.RegisterGenerator(secId, DataType.Level1, new RandomWalkTradeGenerator(secId), 2);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var result = adapter.GetSupportedMarketDataTypes(secId, null, null).ToList();

		result.Count.AssertEqual(2);
		result.Contains(DataType.Ticks).AssertTrue();
		result.Contains(DataType.Level1).AssertTrue();
	}

	#endregion

	#region ToString Tests

	[TestMethod]
	public void ToString_ReturnsFormattedString()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager
		{
			StartDate = new DateTime(2024, 1, 1),
			StopDate = new DateTime(2024, 12, 31)
		};

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var result = adapter.ToString();

		result.Contains("Hist:").AssertTrue();
	}

	#endregion

	#region EmulationState Tests

	[TestMethod]
	public async Task EmulationStateMessage_Stopping_StopsManager()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var stateMsg = new EmulationStateMessage
		{
			State = ChannelStates.Stopping,
		};

		await adapter.SendInMessageAsync(stateMsg, CancellationToken);

		manager.StopCalled.AssertTrue();
	}

	#endregion

	#region Error Handling Tests

	[TestMethod]
	public async Task StartAsync_WhenManagerThrows_SendsErrorAndStoppingState()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager
		{
			ExceptionToThrow = new InvalidOperationException("Test error")
		};

		var outMessages = new List<Message>();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.NewOutMessage += outMessages.Add;

		var stateMsg = new EmulationStateMessage
		{
			State = ChannelStates.Starting,
		};

		await adapter.SendInMessageAsync(stateMsg, CancellationToken);

		// Give time for background task to process
		await Task.Delay(100, CancellationToken);

		// Should have EmulationStateMessage with Stopping state
		var stoppingState = outMessages.OfType<EmulationStateMessage>()
			.FirstOrDefault(m => m.State == ChannelStates.Stopping);

		stoppingState.AssertNotNull();
	}

	[TestMethod]
	public async Task StartAsync_WhenCancelled_SendsStoppingState()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true
		};

		var outMessages = new List<Message>();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.NewOutMessage += outMessages.Add;

		var stateMsg = new EmulationStateMessage
		{
			State = ChannelStates.Starting,
		};

		await adapter.SendInMessageAsync(stateMsg, CancellationToken);

		// Give time for background task to start
		await Task.Delay(50, CancellationToken);

		// Stop the adapter
		await adapter.SendInMessageAsync(new EmulationStateMessage { State = ChannelStates.Stopping }, CancellationToken);

		// Give time for cancellation to propagate
		await Task.Delay(100, CancellationToken);

		// Should have EmulationStateMessage with Stopping state
		var stoppingState = outMessages.OfType<EmulationStateMessage>()
			.FirstOrDefault(m => m.State == ChannelStates.Stopping);

		stoppingState.AssertNotNull();
	}

	[TestMethod]
	public async Task StartAsync_YieldsMessages_SendsThemViaNewOutMessage()
	{
		var secProvider = CreateSecurityProvider();
		var secId = CreateSecurityId();
		var manager = new TestHistoryMarketDataManager();

		var tickMessage = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 100m,
			TradeVolume = 10m,
		};

		manager.MessagesToYield.Add(tickMessage);

		var outMessages = new List<Message>();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.NewOutMessage += outMessages.Add;

		var stateMsg = new EmulationStateMessage
		{
			State = ChannelStates.Starting,
		};

		await adapter.SendInMessageAsync(stateMsg, CancellationToken);

		// Give time for background task to process
		await Task.Delay(100, CancellationToken);

		// Should have received the tick message
		var receivedTick = outMessages.OfType<ExecutionMessage>()
			.FirstOrDefault(m => m.DataTypeEx == DataType.Ticks);

		receivedTick.AssertNotNull();
		receivedTick.TradePrice.AssertEqual(100m);
	}

	#endregion

	#region Generator Data Tests (without history)

	[TestMethod]
	public async Task StartAsync_WithGenerator_NoHistory_YieldsGeneratorData()
	{
		var secProvider = CreateSecurityProvider();
		var secId = CreateSecurityId();

		// Create manager that simulates generator-only data (no storage)
		var manager = new TestHistoryMarketDataManager();
		var generator = new RandomWalkTradeGenerator(secId);
		manager.RegisterGenerator(secId, DataType.Ticks, generator, 1);

		// Add simulated generator output
		var generatedTick = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 150m,
			TradeVolume = 25m,
		};
		manager.MessagesToYield.Add(generatedTick);

		var outMessages = new List<Message>();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.NewOutMessage += outMessages.Add;

		var stateMsg = new EmulationStateMessage
		{
			State = ChannelStates.Starting,
		};

		await adapter.SendInMessageAsync(stateMsg, CancellationToken);

		// Give time for background task to process
		await Task.Delay(100, CancellationToken);

		// Should have received generator-produced tick
		var receivedTick = outMessages.OfType<ExecutionMessage>()
			.FirstOrDefault(m => m.DataTypeEx == DataType.Ticks);

		receivedTick.AssertNotNull();
		receivedTick.TradePrice.AssertEqual(150m);
		receivedTick.TradeVolume.AssertEqual(25m);
	}

	[TestMethod]
	public void GetSupportedMarketDataTypes_WithGeneratorOnly_ReturnsGeneratorTypes()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();
		var secId = CreateSecurityId();

		// No storage, only generator
		manager.RegisterGenerator(secId, DataType.Ticks, new RandomWalkTradeGenerator(secId), 1);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var dataTypes = adapter.GetSupportedMarketDataTypes(secId, null, null).ToList();

		dataTypes.Count.AssertEqual(1);
		dataTypes.Contains(DataType.Ticks).AssertTrue();
	}

	[TestMethod]
	public async Task StartAsync_WithMultipleGenerators_YieldsAllData()
	{
		var secProvider = CreateSecurityProvider();
		var secId = CreateSecurityId();

		var manager = new TestHistoryMarketDataManager();

		// Register multiple generators
		manager.RegisterGenerator(secId, DataType.Ticks, new RandomWalkTradeGenerator(secId), 1);
		manager.RegisterGenerator(secId, DataType.Level1, new RandomWalkTradeGenerator(secId), 2);

		// Simulate output from both generators
		var tickMessage = new ExecutionMessage
		{
			SecurityId = secId,
			DataTypeEx = DataType.Ticks,
			ServerTime = DateTime.UtcNow,
			TradePrice = 100m,
		};

		var level1Message = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
		};
		level1Message.Changes.Add(Level1Fields.BestBidPrice, 99m);
		level1Message.Changes.Add(Level1Fields.BestAskPrice, 101m);

		manager.MessagesToYield.Add(tickMessage);
		manager.MessagesToYield.Add(level1Message);

		var outMessages = new List<Message>();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		adapter.NewOutMessage += outMessages.Add;

		await adapter.SendInMessageAsync(new EmulationStateMessage { State = ChannelStates.Starting }, CancellationToken);

		await Task.Delay(100, CancellationToken);

		// Should have received both types of messages
		var ticks = outMessages.OfType<ExecutionMessage>().Where(m => m.DataTypeEx == DataType.Ticks).ToList();
		var level1s = outMessages.OfType<Level1ChangeMessage>().ToList();

		ticks.Count.AssertEqual(1);
		level1s.Count.AssertEqual(1);
	}

	[TestMethod]
	public async Task StartAsync_GeneratorRegisteredAfterStart_ManagerTracksIt()
	{
		var secProvider = CreateSecurityProvider();
		var secId = CreateSecurityId();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		// Register generator through adapter
		var generatorMsg = new GeneratorMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			Generator = new RandomWalkTradeGenerator(secId),
			TransactionId = 1,
			IsSubscribe = true,
		};

		await adapter.SendInMessageAsync(generatorMsg, CancellationToken);

		// Verify generator is tracked
		manager.HasGenerator(secId, DataType.Ticks).AssertTrue();

		// Verify GetSupportedMarketDataTypes includes the generator
		var dataTypes = adapter.GetSupportedMarketDataTypes(secId, null, null).ToList();
		dataTypes.Contains(DataType.Ticks).AssertTrue();
	}

	#endregion
}
