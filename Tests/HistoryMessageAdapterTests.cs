namespace StockSharp.Tests;

using System.Collections.Concurrent;
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

		event Action<IEnumerable<Security>> ISecurityProvider.Added { add { } remove { } }
		event Action<IEnumerable<Security>> ISecurityProvider.Removed { add { } remove { } }
		event Action ISecurityProvider.Cleared { add { } remove { } }

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

	private sealed class QueuedSynchronizationContext : SynchronizationContext
	{
		private readonly ConcurrentQueue<(SendOrPostCallback callback, object state)> _callbacks = [];

		public override void Post(SendOrPostCallback callback, object state)
			=> _callbacks.Enqueue((callback, state));

		public void Drain()
		{
			while (_callbacks.TryDequeue(out var item))
				item.callback(item.state);
		}
	}

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
		public int ResetCount => Volatile.Read(ref _resetCount);
		public int StopCount => Volatile.Read(ref _stopCount);
		public int StartCount => Volatile.Read(ref _startCount);
		public int MaxConcurrentStartCount => Volatile.Read(ref _maxConcurrentStartCount);
		public int MaxConcurrentStopCount => Volatile.Read(ref _maxConcurrentStopCount);
		public TaskCompletionSource<bool> StartEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public SynchronizationContext StartContext { get; private set; }
		public Action StopAction { get; set; }
		public Action ResetAction { get; set; }
		public IReadOnlyList<MarketDataMessage> Subscriptions => _subscriptions;
		public IReadOnlyList<long> Unsubscriptions => _unsubscriptions;

		private int _resetCount;
		private int _stopCount;
		private int _startCount;
		private int _activeStartCount;
		private int _maxConcurrentStartCount;
		private int _activeStopCount;
		private int _maxConcurrentStopCount;

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

		public IAsyncEnumerable<DataType> GetSupportedDataTypesAsync(SecurityId securityId)
			=> _generators.Where(g => g.Key.Item1 == securityId).Select(g => g.Key.Item2).ToAsyncEnumerable();

		public IAsyncEnumerable<Message> StartAsync(IEnumerable<BoardMessage> boards)
		{
			IsStarted = true;
			StartContext = SynchronizationContext.Current;
			Interlocked.Increment(ref _startCount);
			StartEntered.TrySetResult(true);

			if (ExceptionToThrow != null)
			{
				IsStarted = false;
				throw ExceptionToThrow;
			}

			return Impl();

			async IAsyncEnumerable<Message> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
			{
				var activeCount = Interlocked.Increment(ref _activeStartCount);
				UpdateMaximum(ref _maxConcurrentStartCount, activeCount);

				try
				{
					foreach (var msg in MessagesToYield)
					{
						cancellationToken.ThrowIfCancellationRequested();
						yield return msg;
					}

					if (ShouldWaitForCancellation)
						await Task.Delay(Timeout.Infinite, cancellationToken);

					yield return new EmulationStateMessage
					{
						LocalTime = StopDate,
						State = ChannelStates.Stopping,
					};
				}
				finally
				{
					IsStarted = false;
					Interlocked.Decrement(ref _activeStartCount);
				}
			}
		}

		private static void UpdateMaximum(ref int maximum, int value)
		{
			while (true)
			{
				var current = Volatile.Read(ref maximum);

				if (value <= current || Interlocked.CompareExchange(ref maximum, value, current) == current)
					return;
			}
		}

		public void Stop()
		{
			StopCalled = true;
			Interlocked.Increment(ref _stopCount);
			var activeCount = Interlocked.Increment(ref _activeStopCount);
			UpdateMaximum(ref _maxConcurrentStopCount, activeCount);

			try
			{
				StopAction?.Invoke();
				IsStarted = false;
			}
			finally
			{
				Interlocked.Decrement(ref _activeStopCount);
			}
		}

		public void Reset()
		{
			ResetCalled = true;
			Interlocked.Increment(ref _resetCount);
			ResetAction?.Invoke();
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

		adapter.CurrentTime.AssertEqual(expectedTime);
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

	[TestMethod]
	public async Task ResetMessage_WhenOutputCallbackFails_PublishesErrorResponse()
	{
		var manager = new TestHistoryMarketDataManager();
		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);
		var errorResponse = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

		adapter.NewOutMessageAsync += (message, _) =>
		{
			if (message is ResetMessage)
				return ValueTask.FromException(new InvalidOperationException("Reset callback failed."));

			if (message is ErrorMessage errorMessage)
				errorResponse.TrySetResult(errorMessage.Error);

			return default;
		};

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		(await errorResponse.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken))
			.AssertOfType<InvalidOperationException>();
		manager.ResetCount.AssertEqual(1);
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

		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		await adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		outMessages.OfType<ConnectMessage>().Count().AssertEqual(1);
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
	public async Task GetSupportedMarketDataTypes_DelegatesToManager()
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

		var result = await adapter.GetSupportedMarketDataTypesAsync(secId, null, null).ToListAsync(CancellationToken);

		result.Count.AssertEqual(2);
		result.Count(dt => dt == DataType.Ticks).AssertEqual(1);
		result.Count(dt => dt == DataType.Level1).AssertEqual(1);
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

	[TestMethod]
	public async Task DisconnectMessage_WhenOutputCallbackFails_PublishesErrorResponse()
	{
		var manager = new TestHistoryMarketDataManager();
		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);
		var errorResponse = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);

		adapter.NewOutMessageAsync += (message, _) =>
		{
			if (message is DisconnectMessage { Error: null })
				return ValueTask.FromException(new InvalidOperationException("Disconnect callback failed."));

			if (message is DisconnectMessage { Error: { } error })
				errorResponse.TrySetResult(error);

			return default;
		};

		await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken);

		(await errorResponse.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken))
			.AssertOfType<InvalidOperationException>();
	}

	[TestMethod]
	public async Task EmulationStateMessage_StoppingFromReplayCallback_WaitsForReplayCompletion()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};

		manager.MessagesToYield.Add(new TimeMessage { ServerTime = DateTime.UtcNow });

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var terminalState = AsyncHelper.CreateTaskCompletionSource<bool>();
		var disconnectState = AsyncHelper.CreateTaskCompletionSource<bool>();
		var stopReturned = false;
		var terminalCount = 0;

		adapter.NewOutMessageAsync += async (message, _) =>
		{
			if (message is TimeMessage)
			{
				await adapter.SendInMessageAsync(
					new EmulationStateMessage { State = ChannelStates.Stopping },
					CancellationToken.None);
				stopReturned = true;
			}
			else if (message is EmulationStateMessage { State: ChannelStates.Stopping })
			{
				Interlocked.Increment(ref terminalCount);
				terminalState.TrySetResult(stopReturned);
			}
			else if (message is DisconnectMessage)
			{
				disconnectState.TrySetResult(true);
			}
		};

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);

		var callbackReturned = await terminalState.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
		callbackReturned.AssertTrue("Terminal stop was published before its in-flight replay callback returned.");

		await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken);
		await disconnectState.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
		terminalCount.AssertEqual(1, "A replay generation must publish exactly one terminal stop.");
	}

	[TestMethod]
	public async Task NaturalStopping_ReentrantDisconnect_WaitsForTerminalCallback()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var disconnectState = AsyncHelper.CreateTaskCompletionSource<bool>();
		var terminalCallbackReturned = false;
		var terminalCount = 0;

		adapter.NewOutMessageAsync += async (message, _) =>
		{
			if (message is EmulationStateMessage { State: ChannelStates.Stopping })
			{
				Interlocked.Increment(ref terminalCount);

				await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken.None);
				terminalCallbackReturned = true;
			}
			else if (message is DisconnectMessage)
			{
				disconnectState.TrySetResult(terminalCallbackReturned);
			}
		};

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);

		var callbackReturned = await disconnectState.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
		callbackReturned.AssertTrue("Disconnect was published from inside the in-flight terminal callback.");
		terminalCount.AssertEqual(1, "Natural replay completion must publish exactly one terminal stop.");
	}

	[TestMethod]
	public async Task StopAndRestart_GenerationsDoNotOverlapOrDuplicateTerminal()
	{
		var secProvider = CreateSecurityProvider();
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};

		manager.MessagesToYield.Add(new TimeMessage { ServerTime = DateTime.UtcNow });

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var firstTerminal = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondTerminal = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstDisconnect = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondDisconnect = AsyncHelper.CreateTaskCompletionSource<bool>();
		var restartAttemptReturned = false;
		var timeCount = 0;
		var startingCount = 0;
		var terminalCount = 0;
		var disconnectCount = 0;

		adapter.NewOutMessageAsync += async (message, _) =>
		{
			switch (message)
			{
				case TimeMessage:
				{
					var current = Interlocked.Increment(ref timeCount);

					if (current == 1)
					{
						var stopping = new EmulationStateMessage { State = ChannelStates.Stopping };

						await adapter.SendInMessageAsync(stopping, CancellationToken.None);
						await adapter.SendInMessageAsync(
							new EmulationStateMessage { State = ChannelStates.Stopping },
							CancellationToken.None);

						// A start racing the stopping generation must not start or announce a new replay.
						await adapter.SendInMessageAsync(
							new EmulationStateMessage { State = ChannelStates.Starting },
							CancellationToken.None);
						restartAttemptReturned = true;
					}
					else if (current == 2)
					{
						secondStarted.TrySetResult(true);
					}

					break;
				}

				case EmulationStateMessage { State: ChannelStates.Starting }:
					Interlocked.Increment(ref startingCount);
					break;

				case EmulationStateMessage { State: ChannelStates.Stopping }:
				{
					var current = Interlocked.Increment(ref terminalCount);

					if (current == 1)
						firstTerminal.TrySetResult(restartAttemptReturned);
					else if (current == 2)
						secondTerminal.TrySetResult(true);

					break;
				}

				case DisconnectMessage:
				{
					var current = Interlocked.Increment(ref disconnectCount);

					if (current == 1)
						firstDisconnect.TrySetResult(true);
					else if (current == 2)
						secondDisconnect.TrySetResult(true);

					break;
				}
			}
		};

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);

		(await firstTerminal.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken))
			.AssertTrue("The first terminal was published before the replay callback returned.");

		await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken);
		await firstDisconnect.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		manager.StartCount.AssertEqual(1, "A start racing an active stop must not create another generation.");

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);
		await secondStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Stopping },
			CancellationToken);
		await secondTerminal.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken);
		await secondDisconnect.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		manager.StartCount.AssertEqual(2);
		manager.MaxConcurrentStartCount.AssertEqual(1, "Replay generations overlapped.");
		startingCount.AssertEqual(2, "The rejected racing start must not be announced.");
		terminalCount.AssertEqual(2, "Each replay generation must publish exactly one terminal stop.");
	}

	[TestMethod]
	public void ReplayWorker_DoesNotCaptureCallerSynchronizationContext()
	{
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};
		var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);
		var previousContext = SynchronizationContext.Current;
		var callerContext = new QueuedSynchronizationContext();

		try
		{
			SynchronizationContext.SetSynchronizationContext(callerContext);

			adapter.SendInMessageAsync(
				new EmulationStateMessage { State = ChannelStates.Starting },
				CancellationToken.None).AsTask().GetAwaiter().GetResult();

			callerContext.Drain();
			manager.StartEntered.Task.Wait(TimeSpan.FromSeconds(5)).AssertTrue();
			manager.StartContext.AssertNotSame(callerContext);
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previousContext);
			adapter.Dispose();
		}
	}

	[TestMethod]
	public void StartingPublication_CompletesBeforeReplayStarts()
	{
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};
		var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);
		var previousContext = SynchronizationContext.Current;
		var callerContext = new QueuedSynchronizationContext();
		var releaseStarting = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		adapter.NewOutMessageAsync += (message, _) =>
			message is EmulationStateMessage { State: ChannelStates.Starting }
				? new(releaseStarting.Task)
				: default;

		try
		{
			SynchronizationContext.SetSynchronizationContext(callerContext);

			var start = adapter.SendInMessageAsync(
				new EmulationStateMessage { State = ChannelStates.Starting },
				CancellationToken.None).AsTask();

			callerContext.Drain();
			manager.StartCount.AssertEqual(0, "Replay began before the Starting callback completed.");

			releaseStarting.TrySetResult(true);
			callerContext.Drain();
			start.GetAwaiter().GetResult();
			manager.StartEntered.Task.Wait(TimeSpan.FromSeconds(5)).AssertTrue();
		}
		finally
		{
			releaseStarting.TrySetResult(true);
			callerContext.Drain();
			SynchronizationContext.SetSynchronizationContext(previousContext);
			adapter.Dispose();
		}
	}

	[TestMethod]
	public async Task StopAndDisconnect_ConcurrentRequestsPublishBothMessages()
	{
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};
		manager.MessagesToYield.Add(new TimeMessage { ServerTime = DateTime.UtcNow });

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);

		var replayStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var terminal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var disconnected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		adapter.NewOutMessageAsync += (message, _) =>
		{
			switch (message)
			{
				case TimeMessage:
					replayStarted.TrySetResult(true);
					break;
				case EmulationStateMessage { State: ChannelStates.Stopping }:
					terminal.TrySetResult(true);
					break;
				case DisconnectMessage:
					disconnected.TrySetResult(true);
					break;
			}

			return default;
		};

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);
		await replayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		using var stopEntered = new ManualResetEventSlim();
		using var releaseStop = new ManualResetEventSlim();
		manager.StopAction = () =>
		{
			stopEntered.Set();
			releaseStop.Wait();
		};

		try
		{
			var stop = Task.Run(async () => await adapter.SendInMessageAsync(
				new EmulationStateMessage { State = ChannelStates.Stopping },
				CancellationToken.None));

			stopEntered.Wait(TimeSpan.FromSeconds(5)).AssertTrue();
			await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken);

			releaseStop.Set();
			await stop;
			await terminal.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
			await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
		}
		finally
		{
			manager.StopAction = null;
			releaseStop.Set();
		}
	}

	[TestMethod]
	public async Task DisconnectAndStop_ConcurrentRequestsPublishTerminalBeforeDisconnect()
	{
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};
		manager.MessagesToYield.Add(new TimeMessage { ServerTime = DateTime.UtcNow });

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);

		var replayStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var terminal = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var disconnected = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
		var outputOrder = 0;

		adapter.NewOutMessageAsync += (message, _) =>
		{
			switch (message)
			{
				case TimeMessage:
					replayStarted.TrySetResult(true);
					break;
				case EmulationStateMessage { State: ChannelStates.Stopping }:
					terminal.TrySetResult(Interlocked.Increment(ref outputOrder));
					break;
				case DisconnectMessage:
					disconnected.TrySetResult(Interlocked.Increment(ref outputOrder));
					break;
			}

			return default;
		};

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);
		await replayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		using var stopEntered = new ManualResetEventSlim();
		using var releaseStop = new ManualResetEventSlim();
		manager.StopAction = () =>
		{
			stopEntered.Set();
			releaseStop.Wait();
		};

		try
		{
			var disconnect = Task.Run(async () => await adapter.SendInMessageAsync(
				new DisconnectMessage(),
				CancellationToken.None));

			stopEntered.Wait(TimeSpan.FromSeconds(5)).AssertTrue();
			await adapter.SendInMessageAsync(
				new EmulationStateMessage { State = ChannelStates.Stopping },
				CancellationToken);

			releaseStop.Set();
			await disconnect;
			var terminalOrder = await terminal.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
			var disconnectOrder = await disconnected.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

			terminalOrder.AssertLess(disconnectOrder);
		}
		finally
		{
			manager.StopAction = null;
			releaseStop.Set();
		}
	}

	[TestMethod]
	public async Task ActiveReset_CompletesSuspendedReplayBeforeReentrantRestart()
	{
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};
		manager.MessagesToYield.Add(new TimeMessage { ServerTime = DateTime.UtcNow });

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);

		var firstReplay = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondReplay = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var timeCount = 0;

		adapter.NewOutMessageAsync += async (message, _) =>
		{
			if (message is TimeMessage)
			{
				if (Interlocked.Increment(ref timeCount) == 1)
				{
					await adapter.SendInMessageAsync(
						new EmulationStateMessage { State = ChannelStates.Suspending },
						CancellationToken.None);
					firstReplay.TrySetResult(true);
				}
				else
					secondReplay.TrySetResult(true);
			}
			else if (message is ResetMessage)
			{
				await adapter.SendInMessageAsync(
					new EmulationStateMessage { State = ChannelStates.Starting },
					CancellationToken.None);
			}
		};

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);
		await firstReplay.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);
		await secondReplay.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		manager.ResetCount.AssertEqual(1);
		manager.StartCount.AssertEqual(2);
		manager.MaxConcurrentStartCount.AssertEqual(1);
	}

	[TestMethod]
	public async Task ConcurrentReset_OlderCallbackCannotReleaseNewerReset()
	{
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};
		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);
		var firstCallbackEntered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseFirstCallback = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var resetOutputCount = 0;

		adapter.NewOutMessageAsync += async (message, _) =>
		{
			if (message is ResetMessage && Interlocked.Increment(ref resetOutputCount) == 1)
			{
				firstCallbackEntered.TrySetResult(true);
				await releaseFirstCallback.Task;
			}
		};

		using var secondResetEntered = new ManualResetEventSlim();
		using var releaseSecondReset = new ManualResetEventSlim();

		try
		{
			var firstReset = Task.Run(async () =>
				await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken.None));
			await firstCallbackEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

			manager.ResetAction = () =>
			{
				if (manager.ResetCount != 2)
					return;

				secondResetEntered.Set();
				releaseSecondReset.Wait();
			};

			var secondReset = Task.Run(async () =>
				await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken.None));
			secondResetEntered.Wait(TimeSpan.FromSeconds(5)).AssertTrue();

			releaseFirstCallback.TrySetResult(true);
			await firstReset;

			await adapter.SendInMessageAsync(
				new EmulationStateMessage { State = ChannelStates.Starting },
				CancellationToken);
			manager.StartCount.AssertEqual(0, "An older reset callback released the newer reset operation.");

			releaseSecondReset.Set();
			await secondReset;

			await adapter.SendInMessageAsync(
				new EmulationStateMessage { State = ChannelStates.Starting },
				CancellationToken);
			await manager.StartEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
			manager.StartCount.AssertEqual(1);
		}
		finally
		{
			manager.ResetAction = null;
			releaseFirstCallback.TrySetResult(true);
			releaseSecondReset.Set();
		}
	}

	[TestMethod]
	public async Task DisposeWhileStopIsBlocked_DoesNotCallManagerStopConcurrently()
	{
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};
		manager.MessagesToYield.Add(new TimeMessage { ServerTime = DateTime.UtcNow });
		var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);
		var replayStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		adapter.NewOutMessageAsync += (message, _) =>
		{
			if (message is TimeMessage)
				replayStarted.TrySetResult(true);

			return default;
		};

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);
		await replayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		using var stopEntered = new ManualResetEventSlim();
		using var releaseStop = new ManualResetEventSlim();
		manager.StopAction = () =>
		{
			stopEntered.Set();
			releaseStop.Wait();
		};

		var stop = Task.Run(async () => await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Stopping },
			CancellationToken.None));

		try
		{
			stopEntered.Wait(TimeSpan.FromSeconds(5)).AssertTrue();
			await Task.Run(adapter.Dispose).WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
			manager.MaxConcurrentStopCount.AssertEqual(1);
		}
		finally
		{
			manager.StopAction = null;
			releaseStop.Set();
			await stop;
			adapter.Dispose();
		}
	}

	[TestMethod]
	public async Task NaturalStopping_DoesNotDrainProducerAfterTerminal()
	{
		var manager = new TestHistoryMarketDataManager
		{
			ShouldWaitForCancellation = true,
		};
		manager.MessagesToYield.Add(new EmulationStateMessage
		{
			State = ChannelStates.Stopping,
			LocalTime = DateTime.UtcNow,
		});

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			CreateSecurityProvider(),
			manager);
		var terminal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		adapter.NewOutMessageAsync += (message, _) =>
		{
			if (message is EmulationStateMessage { State: ChannelStates.Stopping })
				terminal.TrySetResult(true);

			return default;
		};

		await adapter.SendInMessageAsync(
			new EmulationStateMessage { State = ChannelStates.Starting },
			CancellationToken);
		await terminal.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		manager.IsStarted.AssertFalse();
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

		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

		var stateMsg = new EmulationStateMessage
		{
			State = ChannelStates.Starting,
		};

		await adapter.SendInMessageAsync(stateMsg, CancellationToken);

		// Give time for background task to process
		await Task.Delay(100, CancellationToken);

		// Should have EmulationStateMessage with Stopping state
		var stoppingState = outMessages.OfType<EmulationStateMessage>()
			.SingleOrDefault(m => m.State == ChannelStates.Stopping && m.Error != null);

		stoppingState.AssertNotNull();
		// The stopping state must carry the actual failure cause, not a generic cancellation.
		(stoppingState.Error is InvalidOperationException).AssertTrue();
		stoppingState.LocalTime.AssertEqual(manager.StopDate);

		// The thrown manager exception should surface as an ErrorMessage (the test name promises an error).
		var errorMsg = outMessages.OfType<ErrorMessage>().FirstOrDefault();
		errorMsg.AssertNotNull("Should have sent an ErrorMessage for the manager failure");
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

		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

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

		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

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
	public async Task StartAsync_ManagerYieldedTick_ForwardsData()
	{
		var secProvider = CreateSecurityProvider();
		var secId = CreateSecurityId();

		var manager = new TestHistoryMarketDataManager();

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

		adapter.NewOutMessageAsync += (m, ct) => { outMessages.Add(m); return default; };

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
	public async Task GetSupportedMarketDataTypes_WithGeneratorOnly_ReturnsGeneratorTypes()
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

		var dataTypes = await adapter.GetSupportedMarketDataTypesAsync(secId, null, null).ToListAsync(CancellationToken);

		dataTypes.Count.AssertEqual(1);
		dataTypes.Count(dt => dt == DataType.Ticks).AssertEqual(1);
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

		var outMessages = new ConcurrentQueue<Message>();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider,
			manager);

		var processingCompleted = AsyncHelper.CreateTaskCompletionSource<EmulationStateMessage>();
		adapter.NewOutMessageAsync += (m, ct) =>
		{
			outMessages.Enqueue(m);

			if (m is EmulationStateMessage { State: ChannelStates.Stopping } stopped)
				processingCompleted.TrySetResult(stopped);

			return default;
		};

		await adapter.SendInMessageAsync(new EmulationStateMessage { State = ChannelStates.Starting }, CancellationToken);
		var stopped = await processingCompleted.Task.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
		stopped.Error.AssertNull();

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

		// Start the adapter first, so the generator is registered AFTER start (as the test name states).
		adapter.NewOutMessageAsync += (m, ct) => default;
		await adapter.SendInMessageAsync(new EmulationStateMessage { State = ChannelStates.Starting }, CancellationToken);
		await Task.Delay(50, CancellationToken);

		// Query supported types BEFORE the generator is registered (populates the adapter's per-security cache).
		var before = await adapter.GetSupportedMarketDataTypesAsync(secId, null, null).ToListAsync(CancellationToken);
		before.Count(dt => dt == DataType.Ticks).AssertEqual(0, "No tick generator registered yet");

		// Register generator through adapter, after start
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

		// After registering the generator, the adapter must report its data type (querying again).
		var after = await adapter.GetSupportedMarketDataTypesAsync(secId, null, null).ToListAsync(CancellationToken);
		after.Count(dt => dt == DataType.Ticks).AssertEqual(1, "Supported types should include the newly registered generator");
	}

	#endregion
}
