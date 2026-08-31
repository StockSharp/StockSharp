namespace StockSharp.Tests;

[TestClass]
public class BufferedMarketDataDriveTests : BaseTestClass
{
	private sealed class RecordingStorage(SecurityId securityId, DataType dataType, Func<bool> isFailing) : IMarketDataStorage
	{
		private readonly Lock _sync = new();
		private readonly List<Message> _saved = [];

		public Message[] Saved
		{
			get
			{
				using (_sync.EnterScope())
					return [.. _saved];
			}
		}

		public int SaveAttempts;

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
		{
			var list = data as IList<Message> ?? [.. data];

			Interlocked.Increment(ref SaveAttempts);

			if (isFailing())
				throw new InvalidOperationException("Storage is down.");

			using (_sync.EnterScope())
				_saved.AddRange(list);

			return new(list.Count);
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

		public bool IsFailing { get; set; }

		public BufferedMarketDataDrive Drive { get; }

		public Harness(int queueCapacity, long maxBufferedMessages)
		{
			var registry = new Mock<IStorageRegistry>();

			registry
				.Setup(r => r.GetStorage(It.IsAny<SecurityId>(), It.IsAny<DataType>(), It.IsAny<IMarketDataDrive>(), It.IsAny<StorageFormats>()))
				.Returns<SecurityId, DataType, IMarketDataDrive, StorageFormats>((secId, dataType, drive, format)
					=> _storages.SafeAdd((secId, dataType), key => new(key.secId, key.dataType, () => IsFailing)));

			Drive = new(Mock.Of<IMarketDataDrive>(), registry.Object, queueCapacity, maxBufferedMessages);
		}

		public long Written => _storages.CachedValues.Sum(s => (long)s.Saved.Length);

		public int SaveAttempts => _storages.CachedValues.Sum(s => Volatile.Read(ref s.SaveAttempts));
	}

	private static readonly SecurityId _secId = new() { SecurityCode = "TEST", BoardCode = BoardCodes.Test };

	private static ExecutionMessage CreateTick(int index) => new()
	{
		SecurityId = _secId,
		DataTypeEx = DataType.Ticks,
		ServerTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc).AddSeconds(index),
		TradeId = index + 1,
		TradePrice = 100 + index,
		TradeVolume = 1,
	};

	[TestMethod]
	public async Task AFullBatchIsWrittenWithoutWaitingForTheInterval()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.MaxBatchSize = 5;
		// Long enough that anything written within the test was written because the batch filled up.
		drive.FlushInterval = TimeSpan.FromHours(1);

		for (var i = 0; i < 5; i++)
			drive.Enqueue(CreateTick(i));

		await drive.StartAsync(CancellationToken);

		await Helper.WaitUntilAsync(() => harness.Written == 5, TimeSpan.FromSeconds(10), "a full batch is written without waiting for the interval");

		await drive.StopAsync(CancellationToken);
	}

	[TestMethod]
	public async Task WhatIsWaitingIsWrittenOnceTheIntervalPasses()
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
	public async Task StoppingWritesWhatIsStillWaiting()
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
	}

	[TestMethod]
	public void WhatDoesNotFitInTheQueueIsCountedAsLost()
	{
		var harness = new Harness(2, 1000);
		var drive = harness.Drive;

		// Nothing is draining the queue, so everything past its capacity has nowhere to go.
		for (var i = 0; i < 5; i++)
			drive.Enqueue(CreateTick(i));

		AreEqual(3L, drive.DroppedMessages);
	}

	[TestMethod]
	public async Task AStalledStorageNeitherGrowsWithoutBoundNorLosesDataSilently()
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

		await Helper.WaitUntilAsync(() => drive.DroppedMessages > 0, TimeSpan.FromSeconds(10), "a storage that cannot take data makes the drive say what it dropped");

		// What it kept has to fit in the bound it was given, and what did not fit is counted.
		IsLessOrEqual(drive.DroppedMessages, sent - maxBuffered);

		harness.IsFailing = false;

		await Helper.WaitUntilAsync(() => harness.Written + drive.DroppedMessages == sent, TimeSpan.FromSeconds(10), "everything sent is either written down or counted as dropped");

		IsLessOrEqual(harness.Written, maxBuffered);

		await drive.StopAsync(CancellationToken);
	}

	[TestMethod]
	public async Task ABatchTheStorageRefusedIsKeptForTheNextTry()
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
	public void TheBoundsHaveToLeaveRoomForSomething()
	{
		Throws<ArgumentOutOfRangeException>(() => new BufferedMarketDataDrive(Mock.Of<IMarketDataDrive>(), Mock.Of<IStorageRegistry>(), 0, 10));
		Throws<ArgumentOutOfRangeException>(() => new BufferedMarketDataDrive(Mock.Of<IMarketDataDrive>(), Mock.Of<IStorageRegistry>(), 10, 0));
		Throws<ArgumentNullException>(() => new BufferedMarketDataDrive(null, Mock.Of<IStorageRegistry>()));
		Throws<ArgumentNullException>(() => new BufferedMarketDataDrive(Mock.Of<IMarketDataDrive>(), null));
	}

	[TestMethod]
	public async Task StoppingAndStartingAgainTakesDataAgain()
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
	public async Task WhatArrivesAfterStoppingIsNotCountedAsLoss()
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
	public async Task DisposingWithoutStoppingLeavesNothingRunning()
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
	public void HowLongDataMayWaitAndHowMuchGoesAtOnceHaveDefaults()
	{
		var drive = new Harness(1000, 1000).Drive;

		AreEqual(TimeSpan.FromSeconds(10), drive.FlushInterval);
		AreEqual(100000, drive.MaxBatchSize);
		AreEqual(StorageFormats.Binary, drive.Format);
	}

	[TestMethod]
	public void MessagesThatNameNoInstrumentOrDataTypeAreNotTaken()
	{
		var harness = new Harness(1000, 1000);
		var drive = harness.Drive;

		drive.Enqueue(new TimeMessage());

		AreEqual(0L, drive.DroppedMessages);
	}

	[TestMethod]
	public void EverythingOtherThanWritingGoesToTheDriveUnderneath()
	{
		var underlying = new Mock<IMarketDataDrive>();
		underlying.Setup(d => d.Path).Returns("some path");

		IMarketDataDrive drive = new BufferedMarketDataDrive(underlying.Object, Mock.Of<IStorageRegistry>());

		AreEqual("some path", drive.Path);

		drive.GetStorageDrive(_secId, DataType.Ticks, StorageFormats.Binary);

		underlying.Verify(d => d.GetStorageDrive(_secId, DataType.Ticks, StorageFormats.Binary), Times.Once);
	}
}
