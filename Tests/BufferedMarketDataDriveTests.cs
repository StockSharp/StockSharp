namespace StockSharp.Tests;

[TestClass]
public class BufferedMarketDataDriveTests : BaseTestClass
{
	private sealed class RecordingStorage(SecurityId securityId, DataType dataType, Func<bool> isFailing, Func<int> acceptBeforeFailure, Func<int> reportSaved) : IMarketDataStorage
	{
		private readonly Lock _sync = new();
		private readonly List<Message> _saved = [];
		private readonly List<Message[]> _batches = [];

		public Message[] Saved
		{
			get
			{
				using (_sync.EnterScope())
					return [.. _saved];
			}
		}

		// What every write was handed, in the order the writes were made.
		public Message[][] Batches
		{
			get
			{
				using (_sync.EnterScope())
					return [.. _batches];
			}
		}

		public int SaveAttempts;

		public int CancelledSaveAttempts;

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
		{
			var list = data as IList<Message> ?? [.. data];

			Interlocked.Increment(ref SaveAttempts);

			if (cancellationToken.IsCancellationRequested)
				Interlocked.Increment(ref CancelledSaveAttempts);

			// Copied: the caller hands over its own live batch and clears it once the write went through.
			using (_sync.EnterScope())
				_batches.Add([.. list]);

			var accepted = acceptBeforeFailure();

			// Part of the batch is on disk and the rest is not - what a storage that runs out of room
			// half way through a write leaves behind.
			if (accepted >= 0)
			{
				using (_sync.EnterScope())
					_saved.AddRange(list.Take(accepted));

				throw new InvalidOperationException("Storage ran out of room half way through.");
			}

			if (isFailing())
				throw new InvalidOperationException("Storage is down.");

			using (_sync.EnterScope())
				_saved.AddRange(list);

			// A storage may take the whole batch and still answer with a smaller number: a real one
			// skips the rows it already holds (AppendOnlyNew) and counts only what it wrote.
			var saved = reportSaved();

			return new(saved >= 0 ? saved : list.Count);
		}

		DataType IMarketDataStorage.DataType => dataType;
		SecurityId IMarketDataStorage.SecurityId => securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }
		IMarketDataSerializer IMarketDataStorage.Serializer => Mock.Of<IMarketDataSerializer>();

		IAsyncEnumerable<DateTime> IMarketDataStorage.GetDatesAsync() => AsyncEnumerable.Empty<DateTime>();
		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date) => AsyncEnumerable.Empty<Message>();
		ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => default;
		ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => new((IMarketDataMetaInfo)null);
	}

	private sealed class Harness
	{
		private readonly CachedSynchronizedDictionary<(SecurityId secId, DataType dataType), RecordingStorage> _storages = [];
		private readonly SynchronizedSet<SecurityId> _failingSecurities = [];
		private readonly Lock _sync = new();

		private readonly Queue<int> _acceptBeforeFailure = new();
		private readonly Queue<int> _reportedSaved = new();

		private IMarketDataDrive _lastAskedFor;
		private StorageFormats _lastAskedFormat;

		public bool IsFailing { get; set; }

		public void FailFor(SecurityId securityId) => _failingSecurities.Add(securityId);

		public void StopFailingFor(SecurityId securityId) => _failingSecurities.Remove(securityId);

		// The next write keeps this many messages of the batch it is handed and then fails; every write
		// after it behaves as usual, so what a retry is handed can be told apart from what was kept.
		// Said more than once, the counts are used up by consecutive writes in the order they were given.
		public void FailNextWriteAfter(int accepted) => Push(_acceptBeforeFailure, accepted);

		// The next write stores everything it is handed and answers with this count instead of the
		// batch size, which is what a storage that filtered part of the batch out reports.
		public void ReportNextWriteSaved(int saved) => Push(_reportedSaved, saved);

		private void Push(Queue<int> queue, int value)
		{
			using (_sync.EnterScope())
				queue.Enqueue(value);
		}

		private int Take(Queue<int> queue)
		{
			using (_sync.EnterScope())
				return queue.Count > 0 ? queue.Dequeue() : -1;
		}

		public IMarketDataDrive Underlying { get; }

		public BufferedMarketDataDrive Drive { get; }

		public Harness(int queueCapacity, long maxBufferedMessages)
		{
			var registry = new Mock<IStorageRegistry>();

			registry
				.Setup(r => r.GetStorage(It.IsAny<SecurityId>(), It.IsAny<DataType>(), It.IsAny<IMarketDataDrive>(), It.IsAny<StorageFormats>()))
				.Returns<SecurityId, DataType, IMarketDataDrive, StorageFormats>((secId, dataType, drive, format) =>
				{
					using (_sync.EnterScope())
					{
						_lastAskedFor = drive;
						_lastAskedFormat = format;
					}

					return _storages.SafeAdd((secId, dataType), key => new(key.secId, key.dataType,
						() => IsFailing || _failingSecurities.Contains(key.secId),
						() => Take(_acceptBeforeFailure),
						() => Take(_reportedSaved)));
				});

			Underlying = Mock.Of<IMarketDataDrive>();
			Drive = new(Underlying, registry.Object, queueCapacity, maxBufferedMessages);
		}

		public long Written => _storages.CachedValues.Sum(s => (long)s.Saved.Length);

		public int SaveAttempts => _storages.CachedValues.Sum(s => Volatile.Read(ref s.SaveAttempts));

		public int CancelledSaveAttempts => _storages.CachedValues.Sum(s => Volatile.Read(ref s.CancelledSaveAttempts));

		public int StorageCount => _storages.CachedKeys.Length;

		public IMarketDataDrive LastAskedFor
		{
			get
			{
				using (_sync.EnterScope())
					return _lastAskedFor;
			}
		}

		public StorageFormats LastAskedFormat
		{
			get
			{
				using (_sync.EnterScope())
					return _lastAskedFormat;
			}
		}

		public Message[] SavedFor(SecurityId secId, DataType dataType)
			=> _storages.TryGetValue((secId, dataType), out var storage) ? storage.Saved : [];

		public Message[][] BatchesFor(SecurityId secId, DataType dataType)
			=> _storages.TryGetValue((secId, dataType), out var storage) ? storage.Batches : [];
	}

	private static readonly SecurityId _secId = new() { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
	private static readonly SecurityId _otherSecId = new() { SecurityCode = "OTHER", BoardCode = BoardCodes.Test };

	private static readonly DateTime _start = new(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);

	private static ExecutionMessage CreateTick(SecurityId securityId, int index) => new()
	{
		SecurityId = securityId,
		DataTypeEx = DataType.Ticks,
		ServerTime = _start.AddSeconds(index),
		TradeId = index + 1,
		TradePrice = 100 + index,
		TradeVolume = 1,
	};

	private static ExecutionMessage CreateTick(int index) => CreateTick(_secId, index);

	// The rows themselves, named by the id each tick carries, so a row that went down twice or not
	// at all is visible in the failure and not just in a count.
	private static string TradeIds(IEnumerable<Message> messages)
		=> messages.Cast<ExecutionMessage>().Select(m => $"{m.TradeId}").JoinComma();

	// One batch of five that the storage half writes before it fails, and one retry at shutdown.
	private async Task<Harness> HalfWrittenBatchAsync()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 5;
		drive.FlushInterval = TimeSpan.FromHours(1);

		// The storage keeps the first three of the batch and only then fails, so the retry meets a
		// storage that already holds part of what it is about to be handed.
		harness.FailNextWriteAfter(3);

		for (var i = 0; i < 5; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => drive.FailedFlushes > 0, TimeSpan.FromSeconds(10), "the write that only half went through was counted as refused");

		await drive.StopAsync(CancellationToken);

		return harness;
	}

	[TestMethod]
	public async Task FlushInterval_Elapsed_WritesWhatWaited()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		// A batch that will never fill up, so only the interval can get this written.
		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(200);

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(CreateTick(0));

		await Helper.WaitUntilAsync(() => harness.Written == 1, TimeSpan.FromSeconds(10), "what is waiting is written once the interval passes");

		await drive.StopAsync(CancellationToken);
	}

	[TestMethod]
	public async Task StopAsync_DataStillWaiting_WritesItDown()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(CreateTick(0));

		await drive.StopAsync(CancellationToken);

		AreEqual(1L, harness.Written);
		AreEqual(0L, drive.DroppedMessages);
		AreEqual(1, harness.SaveAttempts);
		AreEqual(0, harness.CancelledSaveAttempts, "the write of what waited at shutdown is not given a token that is already cancelled");
	}

	[TestMethod]
	public void Enqueue_QueueFull_CountsWhatDoesNotFit()
	{
		var harness = new Harness(2, 1000);
		var drive = harness.Drive;

		// Nothing is draining the queue, so everything past its capacity has nowhere to go.
		for (var i = 0; i < 5; i++)
			drive.Enqueue(CreateTick(i));

		AreEqual(3L, drive.DroppedMessages);
	}

	[TestMethod]
	public async Task Enqueue_StorageStalled_KeepsWhatFitsInTheCapAndCountsTheRest()
	{
		const int sent = 100;
		const long maxBuffered = 10;

		var harness = new Harness(10_000, maxBuffered) { IsFailing = true };
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		await drive.StartAsync(CancellationToken);

		for (var i = 0; i < sent; i++)
			drive.Enqueue(CreateTick(i));

		// What it kept has to fit in the bound it was given, and every message past it is counted.
		await Helper.WaitUntilAsync(() => drive.DroppedMessages == sent - maxBuffered, TimeSpan.FromSeconds(10), "a storage that cannot take data makes the drive say what it dropped");

		AreEqual(maxBuffered, drive.BufferedMessages);
		AreEqual(0L, harness.Written);

		harness.IsFailing = false;

		await Helper.WaitUntilAsync(() => harness.Written == maxBuffered, TimeSpan.FromSeconds(10), "what did fit is written once the storage takes data again");

		AreEqual(sent - maxBuffered, drive.DroppedMessages);
		AreEqual(0L, drive.BufferedMessages);

		await drive.StopAsync(CancellationToken);
	}

	[TestMethod]
	public async Task FailedFlushes_StorageRefused_KeepsTheBatchForTheNextTry()
	{
		var harness = new Harness(1000, 1000) { IsFailing = true };
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(CreateTick(0));

		await Helper.WaitUntilAsync(() => drive.FailedFlushes > 0, TimeSpan.FromSeconds(10), "the drive tried to write and the storage refused");

		AreEqual(0L, harness.Written);
		AreEqual(0L, drive.DroppedMessages);

		harness.IsFailing = false;

		await Helper.WaitUntilAsync(() => harness.Written == 1, TimeSpan.FromSeconds(10), "the refused batch is written on the next try");

		await drive.StopAsync(CancellationToken);
	}

	[TestMethod]
	public async Task StartAsync_AfterStop_TakesDataAgain()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		await drive.StartAsync(CancellationToken);
		drive.Enqueue(CreateTick(0));
		await drive.StopAsync(CancellationToken);

		AreEqual(1L, harness.Written);

		await drive.StartAsync(CancellationToken);
		drive.Enqueue(CreateTick(1));

		await Helper.WaitUntilAsync(() => harness.Written == 2, TimeSpan.FromSeconds(10), "a drive started again takes data again");

		await drive.StopAsync(CancellationToken);

		AreEqual(0L, drive.DroppedMessages);
	}

	[TestMethod]
	public async Task Enqueue_AfterStop_IsNotCountedAsLoss()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		await drive.StartAsync(CancellationToken);
		await drive.StopAsync(CancellationToken);

		// Whoever produces the data is still delivering what was in flight when the drive stopped.
		drive.Enqueue(CreateTick(0));

		AreEqual(0L, drive.DroppedMessages, "a closed drive is not a drive that could not keep up");
	}

	[TestMethod]
	public async Task Dispose_WithoutStopping_LeavesNothingRunning()
	{
		// A batch the storage keeps refusing is retried on every round, so a loop that is still alive
		// goes on asking - which is what makes a loop nobody stopped visible from outside.
		var harness = new Harness(1000, 1000) { IsFailing = true };
		var drive = harness.Drive;

		drive.FlushInterval = TimeSpan.FromMilliseconds(20);

		// A host token that stays alive, so nothing but Dispose can stop the loop.
		await drive.StartAsync(CancellationToken.None);

		drive.Enqueue(CreateTick(0));

		await Helper.WaitUntilAsync(() => harness.SaveAttempts >= 2, TimeSpan.FromSeconds(10), "the loop is running and retrying the batch");

		drive.Dispose();
		await Task.Delay(200, CancellationToken);

		var afterDispose = harness.SaveAttempts;

		await Task.Delay(500, CancellationToken);

		AreEqual(afterDispose, harness.SaveAttempts, "a drive that was disposed rather than stopped has nothing left running");
	}

	[TestMethod]
	public void Ctor_Fresh_HasDefaultWritingSettings()
	{
		var drive = new Harness(1000, 1000).Drive;

		AreEqual(TimeSpan.FromSeconds(10), drive.FlushInterval);
		AreEqual(100000, drive.MaxBatchSize);
		AreEqual(StorageFormats.Binary, drive.Format);
	}

	[TestMethod]
	public void Ctor_BadArgument_NamesTheOffendingOne()
	{
		AreEqual("underlying", Throws<ArgumentNullException>(() => new BufferedMarketDataDrive(null, Mock.Of<IStorageRegistry>())).ParamName);
		AreEqual("storageRegistry", Throws<ArgumentNullException>(() => new BufferedMarketDataDrive(Mock.Of<IMarketDataDrive>(), null)).ParamName);
		AreEqual("queueCapacity", Throws<ArgumentOutOfRangeException>(() => new BufferedMarketDataDrive(Mock.Of<IMarketDataDrive>(), Mock.Of<IStorageRegistry>(), 0, 10)).ParamName);
		AreEqual("queueCapacity", Throws<ArgumentOutOfRangeException>(() => new BufferedMarketDataDrive(Mock.Of<IMarketDataDrive>(), Mock.Of<IStorageRegistry>(), -1, 10)).ParamName);
		AreEqual("maxBufferedMessages", Throws<ArgumentOutOfRangeException>(() => new BufferedMarketDataDrive(Mock.Of<IMarketDataDrive>(), Mock.Of<IStorageRegistry>(), 10, 0)).ParamName);
		AreEqual("maxBufferedMessages", Throws<ArgumentOutOfRangeException>(() => new BufferedMarketDataDrive(Mock.Of<IMarketDataDrive>(), Mock.Of<IStorageRegistry>(), 10, -1)).ParamName);
	}

	[TestMethod]
	public void Ctor_Fresh_HasNothingDroppedAndNothingRefused()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		AreSame(harness.Underlying, drive.Underlying);
		AreEqual(0L, drive.DroppedMessages);
		AreEqual(0L, drive.FailedFlushes);
		AreEqual(0L, drive.BufferedMessages);
	}

	[TestMethod]
	public void FlushInterval_NotPositive_Throws()
	{
		var drive = new Harness(1000, 1000).Drive;

		AreEqual("value", Throws<ArgumentOutOfRangeException>(() => drive.FlushInterval = TimeSpan.Zero).ParamName);
		AreEqual("value", Throws<ArgumentOutOfRangeException>(() => drive.FlushInterval = TimeSpan.FromMilliseconds(-1)).ParamName);
		AreEqual(TimeSpan.FromSeconds(10), drive.FlushInterval);
	}

	[TestMethod]
	public void MaxBatchSize_NotPositive_Throws()
	{
		var drive = new Harness(1000, 1000).Drive;

		AreEqual("value", Throws<ArgumentOutOfRangeException>(() => drive.MaxBatchSize = 0).ParamName);
		AreEqual("value", Throws<ArgumentOutOfRangeException>(() => drive.MaxBatchSize = -1).ParamName);
		AreEqual(100000, drive.MaxBatchSize);
	}

	[TestMethod]
	public async Task Load_WritingSettingsOutOfRange_Throws()
	{
		var storage = new SettingsStorage();
		storage.SetValue(nameof(BufferedMarketDataDrive.FlushInterval), TimeSpan.Zero);

		await ThrowsAsync<ArgumentOutOfRangeException>(() => new Harness(1000, 1000).Drive.LoadAsync(storage, CancellationToken));
	}

	[TestMethod]
	public async Task Ctor_SmallestBoundsAllowed_StillTakesAndWritesOneMessage()
	{
		var harness = new Harness(1, 1);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(CreateTick(0));

		await drive.StopAsync(CancellationToken);

		AreEqual(1L, harness.Written);
		AreEqual(0L, drive.DroppedMessages);
	}

	[TestMethod]
	public void Enqueue_Null_Throws()
	{
		var drive = new Harness(1000, 1000).Drive;

		AreEqual("message", Throws<ArgumentNullException>(() => drive.Enqueue(null)).ParamName);
	}

	[TestMethod]
	public async Task Enqueue_NotMarketData_IsIgnoredWithoutAskingForAStorage()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		await drive.StartAsync(CancellationToken);

		// Names no instrument.
		drive.Enqueue(new TimeMessage());
		// Names an instrument but no data type.
		drive.Enqueue(new SecurityMessage { SecurityId = _secId });
		drive.Enqueue(new ExecutionMessage { SecurityId = _secId, ServerTime = _start });
		// A candle without its argument has no data type either.
		drive.Enqueue(new TimeFrameCandleMessage { SecurityId = _secId, OpenTime = _start });

		await drive.StopAsync(CancellationToken);

		AreEqual(0L, harness.Written);
		AreEqual(0, harness.StorageCount, "nothing that is not market data reaches a storage");
		AreEqual(0L, drive.DroppedMessages, "what is not market data is ignored, not lost");
	}

	[TestMethod]
	public async Task Enqueue_EveryKindOfMarketData_GoesToTheStorageOfItsOwnDataType()
	{
		var timeFrame = TimeSpan.FromMinutes(1).TimeFrame();

		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		var tick = CreateTick(0);
		var level1 = new Level1ChangeMessage { SecurityId = _secId, ServerTime = _start };
		var depth = new QuoteChangeMessage
		{
			SecurityId = _secId,
			ServerTime = _start,
			Bids = [new QuoteChange(100, 10)],
			Asks = [new QuoteChange(101, 5)],
		};
		var candle = new TimeFrameCandleMessage
		{
			SecurityId = _secId,
			DataType = timeFrame,
			OpenTime = _start,
			CloseTime = _start.AddMinutes(1),
			State = CandleStates.Finished,
		};

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(tick);
		drive.Enqueue(level1);
		drive.Enqueue(depth);
		drive.Enqueue(candle);

		await drive.StopAsync(CancellationToken);

		AreEqual(4, harness.StorageCount);
		AreSame(tick, harness.SavedFor(_secId, DataType.Ticks).Single());
		AreSame(level1, harness.SavedFor(_secId, DataType.Level1).Single());
		AreSame(depth, harness.SavedFor(_secId, DataType.MarketDepth).Single());
		AreSame(candle, harness.SavedFor(_secId, timeFrame).Single());
	}

	[TestMethod]
	public async Task Enqueue_TwoInstruments_KeepsTheirDataApart()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(CreateTick(_secId, 0));
		drive.Enqueue(CreateTick(_otherSecId, 1));
		drive.Enqueue(CreateTick(_otherSecId, 2));

		await drive.StopAsync(CancellationToken);

		AreEqual(2, harness.StorageCount);
		AreEqual(1, harness.SavedFor(_secId, DataType.Ticks).Length);
		AreEqual(2, harness.SavedFor(_otherSecId, DataType.Ticks).Length);
	}

	[TestMethod]
	public void Enqueue_QueueExactlyFull_TakesTheLastOneAndDropsTheNext()
	{
		var harness = new Harness(1, 1000);
		var drive = harness.Drive;

		// Nothing was started, so nothing drains what is handed over.
		drive.Enqueue(CreateTick(0));

		AreEqual(0L, drive.DroppedMessages, "the message that exactly fills the queue is taken");

		drive.Enqueue(CreateTick(1));

		AreEqual(1L, drive.DroppedMessages, "the first one past the capacity is lost");
	}

	[TestMethod]
	public async Task Enqueue_BuffersAtTheirCap_DropsOnlyWhatDoesNotFit()
	{
		const long maxBuffered = 3;

		var harness = new Harness(1000, maxBuffered) { IsFailing = true };
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		// Handed over before the loop runs, so all five are taken from the queue in one go.
		for (var i = 0; i < 5; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => drive.FailedFlushes > 0, TimeSpan.FromSeconds(10), "the storage refused what was buffered");

		AreEqual(2L, drive.DroppedMessages, "only the two that did not fit in the cap of three are lost");
		AreEqual(0L, harness.Written);

		harness.IsFailing = false;

		await Helper.WaitUntilAsync(() => harness.Written == maxBuffered, TimeSpan.FromSeconds(10), "what did fit is written once the storage takes data again");

		AreEqual(2L, drive.DroppedMessages);

		await drive.StopAsync(CancellationToken);
	}

	[TestMethod]
	public async Task FailedFlushes_StorageKeepsRefusing_CountsEveryRefusedWrite()
	{
		var harness = new Harness(1000, 1000) { IsFailing = true };
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		drive.Enqueue(CreateTick(_secId, 0));
		drive.Enqueue(CreateTick(_otherSecId, 1));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => drive.FailedFlushes >= 2, TimeSpan.FromSeconds(10), "the write of each instrument was refused");

		await drive.StopAsync(CancellationToken);

		// Nothing is running any more, so the two counts can be compared without racing.
		AreEqual((long)harness.SaveAttempts, drive.FailedFlushes, "every write the storage refused is counted");
		AreEqual(0L, harness.Written);
		AreEqual(0L, drive.DroppedMessages, "a refused write keeps its batch instead of losing it");
	}

	[TestMethod]
	public async Task FailedFlushes_OneInstrumentRefused_TheOthersAreStillWritten()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		harness.FailFor(_secId);

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		drive.Enqueue(CreateTick(_secId, 0));
		drive.Enqueue(CreateTick(_otherSecId, 1));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => drive.FailedFlushes > 0 && harness.Written == 1, TimeSpan.FromSeconds(10), "one storage refused its batch while the other took its own");

		AreEqual(0, harness.SavedFor(_secId, DataType.Ticks).Length);
		AreEqual(1, harness.SavedFor(_otherSecId, DataType.Ticks).Length);
		AreEqual(0L, drive.DroppedMessages);

		harness.StopFailingFor(_secId);

		await Helper.WaitUntilAsync(() => harness.Written == 2, TimeSpan.FromSeconds(10), "the batch that was kept goes out once its own storage takes data again");

		AreEqual(1, harness.SavedFor(_secId, DataType.Ticks).Length);

		await drive.StopAsync(CancellationToken);
	}

	[TestMethod]
	public async Task FailedFlushes_StorageWroteHalfTheBatch_LosesNothing()
	{
		// A failure half way through a write must cost no data: whichever side of it a row was on,
		// it is in the storage once the retry went through.
		var harness = await HalfWrittenBatchAsync();

		var saved = harness.SavedFor(_secId, DataType.Ticks).Cast<ExecutionMessage>();

		AreEqual("1,2,3,4,5", TradeIds(saved.DistinctBy(m => m.TradeId).OrderBy(m => m.TradeId)), "every row of the half-written batch is in the storage");
		AreEqual(0L, harness.Drive.DroppedMessages, "a write that failed half way keeps its data rather than losing it");
		AreEqual(0L, harness.Drive.BufferedMessages, "nothing is left waiting once the retry went through");
	}

	/// <summary>
	/// A write that fails half way hands back no receipt: a storage answers with a count of what it
	/// wrote only when the write returns, and the exception carries none. The drive therefore
	/// cannot resume in the middle of a batch - it replays the same rows, in the order they arrived,
	/// so that a storage which skips what it already holds can recognise them and de-duplicate. That
	/// faithful replay is the drive's whole contribution to writing a row down once.
	/// </summary>
	[TestMethod]
	public async Task FailedFlushes_PartialWrite_RetryReplaysTheSameRowsInTheSameOrder()
	{
		var harness = await HalfWrittenBatchAsync();

		var batches = harness.BatchesFor(_secId, DataType.Ticks);

		AreEqual(2, batches.Length, "one write that failed half way, then one retry");
		AreEqual(TradeIds(batches[0]), TradeIds(batches[1]), "the retry replays the rows of the write that failed, not some other set");

		for (var i = 0; i < batches[0].Length; i++)
			AreSame(batches[0][i], batches[1][i], $"row {i} of the retry is the very row the first write was handed, in the same place, so a storage that skips what it already holds can tell it apart");
	}

	/// <summary>
	/// What ends up stored after a write that failed half way is decided by the storage, not by the
	/// drive: the drive knows only that the write threw, so it hands the whole batch over again, and
	/// a storage that does not skip what it already holds keeps both copies. Worth pinning because a
	/// reader who expects the drive to de-duplicate would be writing an order book twice.
	/// </summary>
	[TestMethod]
	public async Task FailedFlushes_PartialWrite_RetryIsDecidedByWhatTheStorageHolds()
	{
		var harness = await HalfWrittenBatchAsync();

		var batches = harness.BatchesFor(_secId, DataType.Ticks);

		AreEqual(2, batches.Length, "one write that failed half way, then one retry");
		AreEqual("1,2,3,4,5", TradeIds(batches[0]), "the first write is handed the whole batch");
		AreEqual("1,2,3,4,5", TradeIds(batches[1]), "a failure reports no count, so the retry hands the whole batch over rather than guessing where the storage stopped");
		AreEqual("1,2,3,1,2,3,4,5", TradeIds(harness.SavedFor(_secId, DataType.Ticks)), "the three rows the storage took before it failed are handed to it again: skipping them is the storage's own job");
	}

	[TestMethod]
	public async Task FailedFlushes_TwoWritesInARowFailHalfWay_StillGetsEveryRowDown()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 5;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		// A storage that stays sick: the first write keeps three rows of the batch and fails, the
		// second keeps one and fails, and only the third goes through. Five rows went in, so all five
		// have to be in the storage no matter which side of either failure they were on.
		harness.FailNextWriteAfter(3);
		harness.FailNextWriteAfter(1);

		for (var i = 0; i < 5; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => harness.SavedFor(_secId, DataType.Ticks).Cast<ExecutionMessage>().Select(m => m.TradeId).Distinct().Count() == 5, TimeSpan.FromSeconds(10), "the rows go down once a write finally goes through");

		await drive.StopAsync(CancellationToken);

		var saved = harness.SavedFor(_secId, DataType.Ticks).Cast<ExecutionMessage>();

		AreEqual("1,2,3,4,5", TradeIds(saved.DistinctBy(m => m.TradeId).OrderBy(m => m.TradeId)), "no row is lost to a storage that fails half way through two writes running");
		AreEqual(2L, drive.FailedFlushes, "the two writes that threw are the two that are counted");
		AreEqual(0L, drive.DroppedMessages, "a write that failed half way keeps its data rather than losing it");
		AreEqual(0L, drive.BufferedMessages, "nothing is left waiting once a write went through");
	}

	[TestMethod]
	public async Task FailedFlushes_StorageWroteHalfTheBatch_LaterDataIsNotHeldUp()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 5;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		harness.FailNextWriteAfter(3);

		for (var i = 0; i < 5; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => drive.FailedFlushes > 0, TimeSpan.FromSeconds(10), "the write that only half went through was counted as refused");

		// The source goes on producing while the batch of the failed write is still waiting: seven ticks
		// were handed over in all, so seven rows have to be in the storage when the drive stops.
		drive.Enqueue(CreateTick(5));
		drive.Enqueue(CreateTick(6));

		await drive.StopAsync(CancellationToken);

		var saved = harness.SavedFor(_secId, DataType.Ticks).Cast<ExecutionMessage>();

		AreEqual("1,2,3,4,5,6,7", TradeIds(saved.DistinctBy(m => m.TradeId).OrderBy(m => m.TradeId)), "what arrived after the failure goes down together with what was waiting");
		AreEqual(0L, drive.DroppedMessages);
		AreEqual(0L, drive.BufferedMessages);

		// Ticks are handed to the storage in the order they arrived, whichever of them a write starts at.
		foreach (var batch in harness.BatchesFor(_secId, DataType.Ticks))
			AreEqual(TradeIds(batch.Cast<ExecutionMessage>().OrderBy(m => m.TradeId)), TradeIds(batch), "a batch is handed over in the order the rows arrived");
	}

	[TestMethod]
	public async Task FailedFlushes_StorageReportedFewerSavedThanHanded_IsNotARefusedWrite()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		// SaveAsync answers with the count of rows it wrote, and a storage that skips what it already
		// holds writes fewer than it was handed without anything having gone wrong. A write that
		// returned rather than threw is done: three ticks in, one reported written, nothing to retry.
		harness.ReportNextWriteSaved(1);

		for (var i = 0; i < 3; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => harness.SaveAttempts == 1, TimeSpan.FromSeconds(10), "the batch was handed to the storage");

		// Several rounds of the loop come and go, so a batch that was kept would be handed over again.
		await Task.Delay(300, CancellationToken);

		AreEqual(0L, drive.FailedFlushes, "a write that did not fail is not counted as refused");
		AreEqual(1, harness.SaveAttempts, "a write that did not fail is not made a second time");
		AreEqual(0L, drive.BufferedMessages, "a write that did not fail leaves nothing waiting");

		drive.Enqueue(CreateTick(3));

		await Helper.WaitUntilAsync(() => harness.Written == 4, TimeSpan.FromSeconds(10), "the drive goes on writing what arrives afterwards");

		await drive.StopAsync(CancellationToken);

		AreEqual(0L, drive.DroppedMessages);
	}

	[TestMethod]
	public async Task MaxBatchSize_QueueHoldsMoreThanOneBatch_WritesWholeBatchesAndKeepsTheRemainder()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 5;
		drive.FlushInterval = TimeSpan.FromHours(1);

		// Twelve does not divide by five: two whole batches go out and the last two have to wait.
		for (var i = 0; i < 12; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => harness.Written >= 10, TimeSpan.FromSeconds(10), "two full batches are written without waiting for the interval");

		AreEqual(10L, harness.Written);
		AreEqual(2, harness.SaveAttempts);

		await drive.StopAsync(CancellationToken);

		AreEqual(12L, harness.Written, "the last, partial batch goes out when the drive stops");
		AreEqual(0L, drive.DroppedMessages);
	}

	[TestMethod]
	public async Task MaxBatchSize_OneShortOfIt_NothingIsWrittenUntilTheDriveStops()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 5;
		drive.FlushInterval = TimeSpan.FromHours(1);

		for (var i = 0; i < 4; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => drive.BufferedMessages == 4, TimeSpan.FromSeconds(10), "the four messages were taken from the queue");

		AreEqual(0, harness.SaveAttempts, "four messages do not fill a batch of five");

		await drive.StopAsync(CancellationToken);

		AreEqual(4L, harness.Written);
	}

	[TestMethod]
	public async Task MaxBatchSize_BatchFillsUpAcrossSeveralReads_StillForcesTheWrite()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 5;
		drive.FlushInterval = TimeSpan.FromHours(1);

		for (var i = 0; i < 4; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		// The fifth has to arrive after the first four were taken, or the batch never spans two reads.
		await Helper.WaitUntilAsync(() => drive.BufferedMessages == 4, TimeSpan.FromSeconds(10), "the first four were taken from the queue");

		drive.Enqueue(CreateTick(4));

		await Helper.WaitUntilAsync(() => harness.Written == 5, TimeSpan.FromSeconds(10), "a batch that reached MaxBatchSize is written without waiting for the interval");

		await drive.StopAsync(CancellationToken);
	}

	[TestMethod]
	public async Task StartAsync_CalledTwice_LeavesOneLoopThatOneStopDrains()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		await drive.StartAsync(CancellationToken);
		await drive.StartAsync(CancellationToken);

		for (var i = 0; i < 3; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StopAsync(CancellationToken);

		AreEqual(3L, harness.Written, "one stop drains everything, so a second start left no second loop holding data");
		AreEqual(1, harness.SaveAttempts);
		AreEqual(0L, drive.DroppedMessages);
	}

	[TestMethod]
	public async Task StopAsync_NeverStarted_LeavesTheDriveStoppedAndRestartable()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		await drive.StopAsync(CancellationToken);

		drive.Enqueue(CreateTick(0));

		AreEqual(0L, drive.DroppedMessages, "a drive that never ran is not a drive that could not keep up");

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(CreateTick(1));

		await drive.StopAsync(CancellationToken);

		AreEqual(1L, harness.Written, "only what arrived after the start is written");
		AreEqual(0L, drive.DroppedMessages);
	}

	[TestMethod]
	public async Task StopAsync_CalledTwice_WritesWhatWaitedOnlyOnce()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(CreateTick(0));

		await drive.StopAsync(CancellationToken);
		await drive.StopAsync(CancellationToken);

		AreEqual(1L, harness.Written);
		AreEqual(1, harness.SaveAttempts);
	}

	[TestMethod]
	public void Enqueue_AfterDispose_IsNotCountedAsLoss()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.Dispose();

		drive.Enqueue(CreateTick(0));

		AreEqual(0L, drive.DroppedMessages, "a disposed drive is not a drive that could not keep up");
		AreEqual(0L, harness.Written);
	}

	[TestMethod]
	public async Task StartAsync_AfterDispose_Throws()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);

		await drive.StartAsync(CancellationToken);
		drive.Dispose();

		// A drive that answers a start and then takes nothing down would be silent data loss.
		await ThrowsAsync<ObjectDisposedException>(() => drive.StartAsync(CancellationToken).AsTask());

		drive.Enqueue(CreateTick(0));

		AreEqual(0L, harness.Written);
		AreEqual(0L, drive.DroppedMessages);
	}

	[TestMethod]
	public async Task Format_Changed_IsWhatTheStorageIsAskedFor()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 1000;
		drive.FlushInterval = TimeSpan.FromHours(1);
		drive.Format = StorageFormats.Csv;

		await drive.StartAsync(CancellationToken);

		drive.Enqueue(CreateTick(0));

		await drive.StopAsync(CancellationToken);

		AreEqual(StorageFormats.Csv, harness.LastAskedFormat);
		AreSame(harness.Underlying, harness.LastAskedFor, "the storage is asked for on the drive underneath, not on the buffering one");
	}

	[TestMethod]
	public async Task SaveLoad_TheWritingSettings_RoundTrip()
	{
		var drive = new Harness(1000, 1000).Drive;

		drive.Format = StorageFormats.Csv;
		drive.FlushInterval = TimeSpan.FromMilliseconds(1234);
		drive.MaxBatchSize = 7;

		var storage = new SettingsStorage();
		await drive.SaveAsync(storage, CancellationToken);

		var loaded = new Harness(1000, 1000).Drive;
		await loaded.LoadAsync(storage, CancellationToken);

		AreEqual(StorageFormats.Csv, loaded.Format);
		AreEqual(TimeSpan.FromMilliseconds(1234), loaded.FlushInterval);
		AreEqual(7, loaded.MaxBatchSize);
	}

	[TestMethod]
	public async Task IMarketDataDriveMembers_AreAnsweredByTheDriveUnderneath()
	{
		var token = CancellationToken;

		var storageDrive = Mock.Of<IMarketDataStorageDrive>();
		var criteria = new SecurityLookupMessage();
		var provider = Mock.Of<ISecurityProvider>();
		var found = new SecurityMessage { SecurityId = _secId };

		var underlying = new Mock<IMarketDataDrive>();

		underlying.Setup(d => d.Path).Returns("some path");
		underlying.Setup(d => d.GetAvailableSecuritiesAsync()).Returns(new[] { _secId }.ToAsyncEnumerable());
		underlying.Setup(d => d.GetAvailableDataTypesAsync(_secId, StorageFormats.Csv)).Returns(new[] { DataType.Ticks }.ToAsyncEnumerable());
		underlying.Setup(d => d.GetStorageDrive(_secId, DataType.Ticks, StorageFormats.Csv)).Returns(storageDrive);
		underlying.Setup(d => d.VerifyAsync(It.IsAny<CancellationToken>())).Returns(ValueTask.CompletedTask);
		underlying.Setup(d => d.LookupSecuritiesAsync(criteria, provider)).Returns(new[] { found }.ToAsyncEnumerable());

		IMarketDataDrive drive = new BufferedMarketDataDrive(underlying.Object, Mock.Of<IStorageRegistry>());

		AreEqual("some path", drive.Path);
		AreEqual(_secId, (await drive.GetAvailableSecuritiesAsync().ToArrayAsync(token)).Single());
		AreEqual(DataType.Ticks, (await drive.GetAvailableDataTypesAsync(_secId, StorageFormats.Csv).ToArrayAsync(token)).Single());
		AreSame(storageDrive, drive.GetStorageDrive(_secId, DataType.Ticks, StorageFormats.Csv));
		AreSame(found, (await drive.LookupSecuritiesAsync(criteria, provider).ToArrayAsync(token)).Single());

		await drive.VerifyAsync(token);

		underlying.Verify(d => d.VerifyAsync(token), Times.Once);
	}

	[TestMethod]
	public async Task Enqueue_FromManyThreadsAtOnce_LosesNothing()
	{
		const int writers = 4;
		const int perWriter = 250;

		var harness = new Harness(10_000, 10_000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 10_000;
		drive.FlushInterval = TimeSpan.FromMilliseconds(50);

		await drive.StartAsync(CancellationToken);

		await Task.WhenAll(Enumerable.Range(0, writers).Select(writer => Task.Run(() =>
		{
			for (var i = 0; i < perWriter; i++)
				drive.Enqueue(CreateTick(writer * perWriter + i));
		}, CancellationToken)));

		await drive.StopAsync(CancellationToken);

		AreEqual((long)(writers * perWriter), harness.Written);
		AreEqual(0L, drive.DroppedMessages);
	}
}
