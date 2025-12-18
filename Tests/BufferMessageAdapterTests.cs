namespace StockSharp.Tests;

[TestClass]
public class BufferMessageAdapterTests : BaseTestClass
{
	private sealed class InMemorySnapshotStorage<TKey, TMessage>(Func<TMessage, TKey> getKey) : ISnapshotStorage<TKey, TMessage>
		where TMessage : Message
	{
		private readonly Func<TMessage, TKey> _getKey = getKey ?? throw new ArgumentNullException(nameof(getKey));
		private readonly SynchronizedDictionary<TKey, TMessage> _data = [];

		IEnumerable<DateTime> ISnapshotStorage.Dates => [.. _data.Values.OfType<IServerTimeMessage>().Select(m => m.ServerTime.Date).Distinct()];

		void ISnapshotStorage.ClearAll() => _data.Clear();

		void ISnapshotStorage.Clear(object key) => Clear((TKey)key);

		public void Clear(TKey key) => _data.Remove(key);

		void ISnapshotStorage.Update(Message message) => Update((TMessage)message);

		public void Update(TMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			_data[_getKey(message)] = message;
		}

		Message ISnapshotStorage.Get(object key) => Get((TKey)key);

		public TMessage Get(TKey key) => _data.TryGetValue(key);

		IEnumerable<Message> ISnapshotStorage.GetAll(DateTime? from, DateTime? to)
			=> GetAll(from, to).Cast<Message>();

		public IEnumerable<TMessage> GetAll(DateTime? from = null, DateTime? to = null)
		{
			var all = _data.Values.ToArray();

			if (from is null && to is null)
				return all;

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
			})];
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

	private sealed class TestExecutionStorage(SecurityId securityId) : IMarketDataStorage<ExecutionMessage>
	{
		private readonly SecurityId _securityId = securityId;

		public TaskCompletionSource<IReadOnlyList<ExecutionMessage>> Saved { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		ValueTask<IEnumerable<DateTime>> IMarketDataStorage.GetDatesAsync(CancellationToken cancellationToken) => new([]);
		DataType IMarketDataStorage.DataType => DataType.Ticks;
		SecurityId IMarketDataStorage.SecurityId => _securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }
		IMarketDataSerializer IMarketDataStorage.Serializer => Mock.Of<IMarketDataSerializer>();

		public IAsyncEnumerable<ExecutionMessage> LoadAsync(DateTime date, CancellationToken cancellationToken)
			=> AsyncEnumerable.Empty<ExecutionMessage>();

		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date, CancellationToken cancellationToken)
			=> LoadAsync(date, cancellationToken);

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
		adapter.NewOutMessage += output.Add;

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
		adapter.NewOutMessage += output.Add;

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
		buffer.ProcessOutMessage(CreateTick(secId, DateTime.UtcNow));

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
	}
}
