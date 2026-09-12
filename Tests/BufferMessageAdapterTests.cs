namespace StockSharp.Tests;

[TestClass]
public class BufferMessageAdapterTests : BaseTestClass
{
	// The real storage keeps copies of its own: Update stores a clone of what it is given, and Get
	// and GetAll hand out clones. Whoever changes what Get returned has to Update it to keep it.
	private sealed class InMemorySnapshotStorage<TKey, TMessage>(Func<TMessage, TKey> getKey) : ISnapshotStorage<TKey, TMessage>
		where TMessage : Message
	{
		private readonly Func<TMessage, TKey> _getKey = getKey ?? throw new ArgumentNullException(nameof(getKey));
		private readonly SynchronizedDictionary<TKey, TMessage> _data = [];

		// Lets a test wait for the round that stored something rather than wait on the clock.
		public Action<TMessage> Updated { get; set; }

		IEnumerable<DateTime> ISnapshotStorage.Dates => [.. _data.Values.OfType<IServerTimeMessage>().Select(m => m.ServerTime.Date).Distinct()];

		void ISnapshotStorage.ClearAll() => _data.Clear();

		void ISnapshotStorage.Clear(object key) => Clear((TKey)key);

		public void Clear(TKey key) => _data.Remove(key);

		void ISnapshotStorage.Update(Message message) => Update((TMessage)message);

		public void Update(TMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			_data[_getKey(message)] = (TMessage)message.Clone();

			Updated?.Invoke(message);
		}

		Message ISnapshotStorage.Get(object key) => Get((TKey)key);

		public TMessage Get(TKey key) => (TMessage)_data.TryGetValue(key)?.Clone();

		IEnumerable<Message> ISnapshotStorage.GetAll(DateTime? from, DateTime? to)
			=> GetAll(from, to).Cast<Message>();

		public IEnumerable<TMessage> GetAll(DateTime? from = null, DateTime? to = null)
		{
			var all = _data.Values.ToArray();

			if (from is null && to is null)
				return [.. all.Select(m => (TMessage)m.Clone())];

			return [.. all.Where(m =>
			{
				if (m is not IServerTimeMessage stm)
					return true;

				var t = stm.ServerTime;

				if (from != null && t < from.Value)
					return false;

				if (to != null && t > to.Value)
					return false;

				return true;
			}).Select(m => (TMessage)m.Clone())];
		}
	}

	private sealed class InMemorySnapshotRegistry : ISnapshotRegistry
	{
		private readonly SynchronizedDictionary<DataType, ISnapshotStorage> _storages = [];

		public InMemorySnapshotRegistry Add(DataType dataType, ISnapshotStorage storage)
		{
			_storages[dataType.Immutable()] = storage ?? throw new ArgumentNullException(nameof(storage));
			return this;
		}

		ISnapshotStorage ISnapshotRegistry.GetSnapshotStorage(DataType dataType)
			=> _storages.TryGetValue(dataType.Immutable()) ?? throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Snapshot storage not registered.");

		ValueTask ISnapshotRegistry.InitAsync(CancellationToken cancellationToken) => default;
	}

	// A storage that cannot take what it is given - a full disk, a server that is down. It says so
	// the way one does: by throwing out of the write.
	private sealed class RefusingExecutionStorage(SecurityId securityId) : IMarketDataStorage<ExecutionMessage>
	{
		private readonly SecurityId _securityId = securityId;

		public TaskCompletionSource Tried { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		IAsyncEnumerable<DateTime> IMarketDataStorage.GetDatesAsync() => AsyncEnumerable.Empty<DateTime>();
		DataType IMarketDataStorage.DataType => DataType.Ticks;
		SecurityId IMarketDataStorage.SecurityId => _securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }
		IMarketDataSerializer IMarketDataStorage.Serializer => Mock.Of<IMarketDataSerializer>();

		public IAsyncEnumerable<ExecutionMessage> LoadAsync(DateTime date)
			=> AsyncEnumerable.Empty<ExecutionMessage>();

		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date) => LoadAsync(date);

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
			=> ((IMarketDataStorage<ExecutionMessage>)this).SaveAsync(data.Cast<ExecutionMessage>(), cancellationToken);

		ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => default;
		ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => new((IMarketDataMetaInfo)null);

		IMarketDataSerializer<ExecutionMessage> IMarketDataStorage<ExecutionMessage>.Serializer => Mock.Of<IMarketDataSerializer<ExecutionMessage>>();

		public ValueTask DeleteAsync(IEnumerable<ExecutionMessage> data, CancellationToken cancellationToken) => default;

		public ValueTask<int> SaveAsync(IEnumerable<ExecutionMessage> data, CancellationToken cancellationToken)
		{
			Tried.TrySetResult();
			throw new InvalidOperationException("storage is down");
		}
	}

	private sealed class TestExecutionStorage(SecurityId securityId) : IMarketDataStorage<ExecutionMessage>
	{
		private readonly SecurityId _securityId = securityId;

		public TaskCompletionSource<IReadOnlyList<ExecutionMessage>> Saved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		IAsyncEnumerable<DateTime> IMarketDataStorage.GetDatesAsync() => AsyncEnumerable.Empty<DateTime>();
		DataType IMarketDataStorage.DataType => DataType.Ticks;
		SecurityId IMarketDataStorage.SecurityId => _securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }
		IMarketDataSerializer IMarketDataStorage.Serializer => Mock.Of<IMarketDataSerializer>();

		public IAsyncEnumerable<ExecutionMessage> LoadAsync(DateTime date)
			=> AsyncEnumerable.Empty<ExecutionMessage>();

		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date)
			=> LoadAsync(date);

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
			=> ((IMarketDataStorage<ExecutionMessage>)this).SaveAsync(data.Cast<ExecutionMessage>(), cancellationToken);

		ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => default;
		ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => new((IMarketDataMetaInfo)null);

		IMarketDataSerializer<ExecutionMessage> IMarketDataStorage<ExecutionMessage>.Serializer => Mock.Of<IMarketDataSerializer<ExecutionMessage>>();

		public ValueTask<int> SaveAsync(IEnumerable<ExecutionMessage> data, CancellationToken cancellationToken)
		{
			var saved = data.ToArray();
			Saved.TrySetResult(saved);
			return new(saved.Length);
		}

		ValueTask IMarketDataStorage<ExecutionMessage>.DeleteAsync(IEnumerable<ExecutionMessage> data, CancellationToken cancellationToken) => default;
	}

	// Keeps everything that was ever written to it, and lets a test wait for the round that wrote the
	// first batch instead of waiting on the clock.
	private sealed class RecordingExecutionStorage(SecurityId securityId) : IMarketDataStorage<ExecutionMessage>
	{
		private readonly SecurityId _securityId = securityId;
		private readonly SynchronizedList<ExecutionMessage> _saved = [];

		public TaskCompletionSource FirstSaved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ExecutionMessage[] Saved => [.. _saved];

		IAsyncEnumerable<DateTime> IMarketDataStorage.GetDatesAsync() => AsyncEnumerable.Empty<DateTime>();
		DataType IMarketDataStorage.DataType => DataType.Ticks;
		SecurityId IMarketDataStorage.SecurityId => _securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }
		IMarketDataSerializer IMarketDataStorage.Serializer => Mock.Of<IMarketDataSerializer>();

		public IAsyncEnumerable<ExecutionMessage> LoadAsync(DateTime date)
			=> AsyncEnumerable.Empty<ExecutionMessage>();

		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date) => LoadAsync(date);

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
			=> ((IMarketDataStorage<ExecutionMessage>)this).SaveAsync(data.Cast<ExecutionMessage>(), cancellationToken);

		ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => default;
		ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => new((IMarketDataMetaInfo)null);

		IMarketDataSerializer<ExecutionMessage> IMarketDataStorage<ExecutionMessage>.Serializer => Mock.Of<IMarketDataSerializer<ExecutionMessage>>();

		ValueTask IMarketDataStorage<ExecutionMessage>.DeleteAsync(IEnumerable<ExecutionMessage> data, CancellationToken cancellationToken) => default;

		public ValueTask<int> SaveAsync(IEnumerable<ExecutionMessage> data, CancellationToken cancellationToken)
		{
			var saved = data.ToArray();

			_saved.AddRange(saved);
			FirstSaved.TrySetResult();

			return new(saved.Length);
		}
	}

	private static ExecutionMessage CreateTick(SecurityId securityId, DateTime serverTime) => new()
	{
		SecurityId = securityId,
		DataTypeEx = DataType.Ticks,
		ServerTime = serverTime,
		TradePrice = 1,
		TradeVolume = 1,
	};

	[TestMethod]
	public async Task MarketData_Subscribe_Snapshot_Level1_DefaultSecurity_SendsAllSnapshots()
	{
		var token = CancellationToken;

		var sec1 = new SecurityId { SecurityCode = "AAA", BoardCode = BoardCodes.Test };
		var sec2 = new SecurityId { SecurityCode = "BBB", BoardCode = BoardCodes.Test };

		var l1Storage = new InMemorySnapshotStorage<SecurityId, Level1ChangeMessage>(m => m.SecurityId);
		var l11 = new Level1ChangeMessage { SecurityId = sec1, ServerTime = DateTime.UtcNow };
		l11.Add(Level1Fields.LastTradePrice, 1m);
		l1Storage.Update(l11);

		var l12 = new Level1ChangeMessage { SecurityId = sec2, ServerTime = DateTime.UtcNow };
		l12.Add(Level1Fields.LastTradePrice, 2m);
		l1Storage.Update(l12);

		var quotesStorage = new InMemorySnapshotStorage<SecurityId, QuoteChangeMessage>(m => m.SecurityId);

		var snapshotRegistry = new InMemorySnapshotRegistry()
			.Add(DataType.Level1, l1Storage)
			.Add(DataType.MarketDepth, quotesStorage);

		var settings = new StorageCoreSettings
		{
			Mode = StorageModes.Snapshot,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 10,
			DataType2 = DataType.Level1,
			SecurityId = default,
		}, token);

		var l1Out = output.OfType<Level1ChangeMessage>().ToArray();
		l1Out.Length.AssertEqual(2);
		l1Out.All(m => m.SubscriptionId == 10).AssertTrue();
		l1Out.Single(m => m.SecurityId == sec1).TryGetDecimal(Level1Fields.LastTradePrice).AssertEqual(1m);
		l1Out.Single(m => m.SecurityId == sec2).TryGetDecimal(Level1Fields.LastTradePrice).AssertEqual(2m);
		output.OfType<MarketDataMessage>().Count().AssertEqual(1);
	}

	[TestMethod]
	public async Task MarketData_Subscribe_Snapshot_MarketDepth_SpecificSecurity_SendsSingleSnapshot()
	{
		var token = CancellationToken;

		var sec1 = new SecurityId { SecurityCode = "AAA", BoardCode = BoardCodes.Test };
		var sec2 = new SecurityId { SecurityCode = "BBB", BoardCode = BoardCodes.Test };

		var l1Storage = new InMemorySnapshotStorage<SecurityId, Level1ChangeMessage>(m => m.SecurityId);
		var quotesStorage = new InMemorySnapshotStorage<SecurityId, QuoteChangeMessage>(m => m.SecurityId);

		quotesStorage.Update(new QuoteChangeMessage
		{
			SecurityId = sec1,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100, 1)],
			Asks = [new QuoteChange(101, 1)],
		});

		quotesStorage.Update(new QuoteChangeMessage
		{
			SecurityId = sec2,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(200, 1)],
			Asks = [new QuoteChange(201, 1)],
		});

		var snapshotRegistry = new InMemorySnapshotRegistry()
			.Add(DataType.Level1, l1Storage)
			.Add(DataType.MarketDepth, quotesStorage);

		var settings = new StorageCoreSettings
		{
			Mode = StorageModes.Snapshot,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 11,
			DataType2 = DataType.MarketDepth,
			SecurityId = sec2,
		}, token);

		var quotesOut = output.OfType<QuoteChangeMessage>().ToArray();
		quotesOut.Length.AssertEqual(1);
		quotesOut[0].SecurityId.AssertEqual(sec2);
		quotesOut[0].SubscriptionId.AssertEqual(11);
		output.OfType<MarketDataMessage>().Count().AssertEqual(1);
	}

	[TestMethod]
	public async Task Connect_StartsTimer_AndSavesBufferedTicks()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var execStorage = new TestExecutionStorage(secId);

		var registry = new Mock<IStorageRegistry>();
		registry
			.Setup(r => r.GetStorage(It.IsAny<SecurityId>(), It.IsAny<DataType>(), It.IsAny<IMarketDataDrive>(), It.IsAny<StorageFormats>()))
			.Returns<SecurityId, DataType, IMarketDataDrive, StorageFormats>((_, dt, _, _) =>
			{
				if (dt == DataType.Ticks)
					return execStorage;

				throw new NotSupportedException(dt.ToString());
			});

		var settings = new StorageCoreSettings
		{
			StorageRegistry = registry.Object,
			Mode = StorageModes.Incremental,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		var tick = CreateTick(secId, DateTime.UtcNow);
		buffer.ProcessOutMessage(tick);

		var snapshotRegistry = new InMemorySnapshotRegistry()
			.Add(DataType.Level1, new InMemorySnapshotStorage<SecurityId, Level1ChangeMessage>(m => m.SecurityId))
			.Add(DataType.MarketDepth, new InMemorySnapshotStorage<SecurityId, QuoteChangeMessage>(m => m.SecurityId));

		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var completed = await Task.WhenAny(execStorage.Saved.Task, Task.Delay(TimeSpan.FromSeconds(5), token));
		(completed == execStorage.Saved.Task).AssertTrue();

		var saved = await execStorage.Saved.Task;
		saved.Count.AssertEqual(1);
		saved[0].SecurityId.AssertEqual(secId);
		saved[0].DataTypeEx.AssertEqual(DataType.Ticks);
		saved[0].ServerTime.AssertEqual(tick.ServerTime);
		saved[0].TradePrice.AssertEqual(tick.TradePrice);
		saved[0].TradeVolume.AssertEqual(tick.TradeVolume);
	}

	[TestMethod]
	public async Task Disconnect_FlushesWhatTheBufferStillHolds()
	{
		// The buffer is written out on a ten second round. Whatever arrived since the last round is
		// still in it when the connection goes down, and for that data there is no next round: if the
		// disconnect does not write it, nothing ever will and the last seconds of the session are lost.
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var execStorage = new RecordingExecutionStorage(secId);

		var registry = new Mock<IStorageRegistry>();
		registry
			.Setup(r => r.GetStorage(It.IsAny<SecurityId>(), It.IsAny<DataType>(), It.IsAny<IMarketDataDrive>(), It.IsAny<StorageFormats>()))
			.Returns<SecurityId, DataType, IMarketDataDrive, StorageFormats>((_, dt, _, _) =>
			{
				if (dt == DataType.Ticks)
					return execStorage;

				throw new NotSupportedException(dt.ToString());
			});

		var settings = new StorageCoreSettings
		{
			StorageRegistry = registry.Object,
			Mode = StorageModes.Incremental,
			Format = StorageFormats.Binary,
		};

		var time = new DateTime(2026, 09, 11, 10, 00, 00, DateTimeKind.Utc);

		var buffer = new StorageBuffer();
		// something for the first round to write, so the test can tell when that round is over and the
		// next one is ten seconds away
		var beforeConnect = CreateTick(secId, time);
		buffer.ProcessOutMessage(beforeConnect);

		var snapshotRegistry = new InMemorySnapshotRegistry();

		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var completed = await Task.WhenAny(execStorage.FirstSaved.Task, Task.Delay(TimeSpan.FromSeconds(5), token));
		(completed == execStorage.FirstSaved.Task).AssertTrue("the first round has to have run");

		// arrives while the round that follows is still ten seconds away
		var afterTheRound = CreateTick(secId, time.AddSeconds(1));
		buffer.ProcessOutMessage(afterTheRound);

		await adapter.SendInMessageAsync(new DisconnectMessage(), token);

		execStorage.Saved.Any(m => m.ServerTime == afterTheRound.ServerTime)
			.AssertTrue("what the buffer still held when the connection went down was written out");
	}

	[TestMethod]
	public async Task ASaveThatFailsKeepsWhatItCouldNotWrite()
	{
		// Draining hands the data over and forgets it, so a write that throws used to take it with
		// it - and a storage that is down for a while cost every message that arrived while it was.
		// What cannot be written goes back, to be written when the storage is there again.
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var execStorage = new RefusingExecutionStorage(secId);

		var registry = new Mock<IStorageRegistry>();
		registry
			.Setup(r => r.GetStorage(It.IsAny<SecurityId>(), It.IsAny<DataType>(), It.IsAny<IMarketDataDrive>(), It.IsAny<StorageFormats>()))
			.Returns<SecurityId, DataType, IMarketDataDrive, StorageFormats>((_, dt, _, _) =>
			{
				if (dt == DataType.Ticks)
					return execStorage;

				throw new NotSupportedException(dt.ToString());
			});

		var settings = new StorageCoreSettings
		{
			StorageRegistry = registry.Object,
			Mode = StorageModes.Incremental,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		var tick = CreateTick(secId, DateTime.UtcNow);
		buffer.ProcessOutMessage(tick);

		var snapshotRegistry = new InMemorySnapshotRegistry();

		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var tried = await Task.WhenAny(execStorage.Tried.Task, Task.Delay(TimeSpan.FromSeconds(5), token));
		(tried == execStorage.Tried.Task).AssertTrue("the round has to have tried to write it");

		IDictionary<SecurityId, IEnumerable<ExecutionMessage>> back = null;

		// Put back inside the catch, a moment after the write threw.
		for (var i = 0; i < 100 && back is not { Count: > 0 }; i++)
		{
			back = buffer.GetTicks();

			if (back.Count == 0)
				await Task.Delay(50, token);
		}

		back.Count.AssertEqual(1, "what could not be written is still there to be written again");
		back[secId].Single().TradePrice.AssertEqual(tick.TradePrice);
	}

	private static ExecutionMessage CreateOrder(SecurityId securityId, long transId, DateTime serverTime, OrderStates state) => new()
	{
		SecurityId = securityId,
		DataTypeEx = DataType.Transactions,
		HasOrderInfo = true,
		TransactionId = transId,
		ServerTime = serverTime,
		OrderState = state,
		OrderPrice = 100,
		OrderVolume = 10,
		Side = Sides.Buy,
		PortfolioName = "PF",
	};

	[TestMethod]
	public async Task Replace_Snapshot_LeavesReplacedOrderDone()
	{
		// A replace ends the order it replaces, so the snapshot has to show that order as Done.
		// The snapshot hands out clones, so the state has to be written back, not set on the copy.
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var time = new DateTime(2026, 09, 10, 10, 00, 00, DateTimeKind.Utc);

		const long replacedId = 1;
		const long replaceId = 2;

		var transStorage = new InMemorySnapshotStorage<string, ExecutionMessage>(m => m.TransactionId.To<string>());
		transStorage.Update(CreateOrder(secId, replacedId, time, OrderStates.Active));

		var snapshotRegistry = new InMemorySnapshotRegistry()
			.Add(DataType.Transactions, transStorage);

		var settings = new StorageCoreSettings
		{
			Mode = StorageModes.Snapshot,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		var inner = new PassThroughMessageAdapter(new IncrementalIdGenerator());

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		var replacementStored = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		transStorage.Updated = m =>
		{
			if (m.TransactionId == replaceId)
				replacementStored.TrySetResult();
		};

		await adapter.SendInMessageAsync(new OrderReplaceMessage
		{
			SecurityId = secId,
			TransactionId = replaceId,
			OriginalTransactionId = replacedId,
			Side = Sides.Buy,
			Volume = 10,
			Price = 101,
			PortfolioName = "PF",
		}, token);

		// the replace is what the buffer records, and the storage round works from that
		await adapter.SendInMessageAsync(new ConnectMessage(), token);

		var completed = await Task.WhenAny(replacementStored.Task, Task.Delay(TimeSpan.FromSeconds(10), token));
		(completed == replacementStored.Task).AssertTrue("the round has to have stored the replacement");

		transStorage.Get(replaceId.To<string>()).AssertNotNull("the replacement is in the snapshot");

		var replaced = transStorage.Get(replacedId.To<string>());
		replaced.AssertNotNull("the replaced order is still in the snapshot");
		replaced.OrderState.AssertEqual(OrderStates.Done);
	}

	[TestMethod]
	public async Task OrderStatus_Snapshot_CoveringWholeRange_StillAnswersTheSubscription()
	{
		// The snapshot reaching the end of the requested range is not a reason to leave the caller
		// with nothing: the request either goes on to the inner adapter or the subscription is
		// closed here. Serving it and then dropping it silently leaves the caller waiting for good.
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var from = new DateTime(2026, 09, 10, 10, 00, 00, DateTimeKind.Utc);
		var to = from.AddHours(1);

		const long transId = 100;

		var transStorage = new InMemorySnapshotStorage<string, ExecutionMessage>(m => m.TransactionId.To<string>());
		// the last order in the snapshot sits exactly on the end of the range
		transStorage.Update(CreateOrder(secId, 1, to, OrderStates.Active));

		var snapshotRegistry = new InMemorySnapshotRegistry()
			.Add(DataType.Transactions, transStorage);

		var settings = new StorageCoreSettings
		{
			Mode = StorageModes.Snapshot,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = transId,
			From = from,
			To = to,
		}, token);

		var served = output.OfType<ExecutionMessage>().ToArray();
		served.Length.AssertEqual(1, "what the snapshot holds is sent out");
		served[0].OriginalTransactionId.AssertEqual(transId);

		var reachedInner = inner.InMessages.OfType<OrderStatusMessage>().Any(m => m.TransactionId == transId);
		var answered = output.OfType<SubscriptionResponseMessage>().Any(m => m.OriginalTransactionId == transId)
			|| output.OfType<SubscriptionFinishedMessage>().Any(m => m.OriginalTransactionId == transId);

		(reachedInner || answered).AssertTrue("the subscription has to be either passed on or closed here");
	}

	[TestMethod]
	public async Task OrderStatus_Snapshot_ShortOfTheRange_PassesTheRequestOn()
	{
		// The other side of the same rule: with the range not yet covered the request goes on to
		// the inner adapter, unchanged - From stays as asked so nothing asks for the range twice.
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var from = new DateTime(2026, 09, 10, 10, 00, 00, DateTimeKind.Utc);
		var to = from.AddHours(2);

		const long transId = 101;

		var transStorage = new InMemorySnapshotStorage<string, ExecutionMessage>(m => m.TransactionId.To<string>());
		transStorage.Update(CreateOrder(secId, 1, from.AddHours(1), OrderStates.Active));

		var snapshotRegistry = new InMemorySnapshotRegistry()
			.Add(DataType.Transactions, transStorage);

		var settings = new StorageCoreSettings
		{
			Mode = StorageModes.Snapshot,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		await adapter.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = transId,
			From = from,
			To = to,
		}, token);

		var passed = inner.InMessages.OfType<OrderStatusMessage>().SingleOrDefault(m => m.TransactionId == transId);
		passed.AssertNotNull("the request goes on to the inner adapter");
		passed.From.AssertEqual((DateTime?)from);
		passed.To.AssertEqual((DateTime?)to);
	}

	/// <summary>
	/// Snapshot mode is a setting, and a snapshot store is optional - a connector is built with none.
	/// Turning the setting on without one must not cost the user their subscription: there is simply
	/// no snapshot to replay, and the request has to reach the adapter as it always does.
	/// </summary>
	[TestMethod]
	public async Task SubscribingInSnapshotModeWithoutASnapshotStoreStillReachesTheAdapter()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };

		var settings = new StorageCoreSettings
		{
			Mode = StorageModes.Snapshot,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, null);

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 30,
			DataType2 = DataType.Level1,
			SecurityId = secId,
		}, token);

		inner.InMessages.OfType<MarketDataMessage>().Count(m => m.TransactionId == 30).AssertEqual(1);
	}

	/// <summary>
	/// A live adapter may publish current data while it handles a subscription. The persisted snapshot
	/// must precede that call, otherwise its older state can arrive after the current state and roll the
	/// subscriber back.
	/// </summary>
	[TestMethod]
	public async Task TheSnapshotArrivesBeforeTheLiveSubscriptionCanProduceData()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };

		var l1Storage = new InMemorySnapshotStorage<SecurityId, Level1ChangeMessage>(m => m.SecurityId);
		var l1 = new Level1ChangeMessage { SecurityId = secId, ServerTime = DateTime.UtcNow };
		l1.Add(Level1Fields.LastTradePrice, 1m);
		l1Storage.Update(l1);

		var snapshotRegistry = new InMemorySnapshotRegistry()
			.Add(DataType.Level1, l1Storage);

		var settings = new StorageCoreSettings
		{
			Mode = StorageModes.Snapshot,
			Format = StorageFormats.Binary,
		};

		var buffer = new StorageBuffer();
		// Echoes what it is sent, marking the earliest point at which live output could be produced.
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, snapshotRegistry);

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 31,
			DataType2 = DataType.Level1,
			SecurityId = secId,
		}, token);

		var forwarded = output.FindIndex(m => m is MarketDataMessage);
		var snapshot = output.FindIndex(m => m is Level1ChangeMessage);

		IsGreaterOrEqual(forwarded, 0, "the subscription has to reach the inner adapter");
		IsGreaterOrEqual(snapshot, 0, "the snapshot has to be sent out");
		IsLess(snapshot, forwarded, "persisted state must be applied before the live stream can publish newer data");
	}

	/// <summary>
	/// Incremental storage is the other way the buffer keeps transactions, and a user who turned it on
	/// has their orders on disk. Asking for order status has to give them back what was stored for that
	/// instrument - answering with nothing means the orders of the previous session are simply gone.
	/// </summary>
	[TestMethod]
	public async Task OrderStatus_Incremental_ServesWhatWasStoredForTheSecurity()
	{
		var token = CancellationToken;

		var fs = Helper.MemorySystem;
		var registry = fs.GetStorage(fs.GetSubTemp());

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var from = new DateTime(2026, 09, 10, 10, 00, 00, DateTimeKind.Utc);
		var to = from.AddHours(2);

		const long transId = 102;
		const long orderTransId = 1;

		var settings = new StorageCoreSettings
		{
			StorageRegistry = registry,
			Drive = registry.DefaultDrive,
			Mode = StorageModes.Incremental,
			Format = StorageFormats.Binary,
		};

		var stored = CreateOrder(secId, orderTransId, from.AddHours(1), OrderStates.Active);
		stored.LocalTime = stored.ServerTime;

		await settings.GetStorage<ExecutionMessage>(secId, DataType.Transactions).SaveAsync([stored], token);

		var buffer = new StorageBuffer();
		var inner = new RecordingPassThroughMessageAdapter();

		using var adapter = new BufferMessageAdapter(inner, settings, buffer, new InMemorySnapshotRegistry());

		var output = new List<Message>();
		adapter.NewOutMessageAsync += (m, ct) => { output.Add(m); return default; };

		await adapter.SendInMessageAsync(new OrderStatusMessage
		{
			IsSubscribe = true,
			TransactionId = transId,
			SecurityId = secId,
			From = from,
			To = to,
		}, token);

		var served = output.OfType<ExecutionMessage>().ToArray();
		served.Length.AssertEqual(1, "what incremental storage holds for that instrument is sent out");
		served[0].TransactionId.AssertEqual(orderTransId);
		served[0].OriginalTransactionId.AssertEqual(transId);
	}
}
