namespace StockSharp.Algo.Storages;

using System.Threading.Channels;

/// <summary>
/// <see cref="IMarketDataDrive"/> that writes what it is given in the background.
/// </summary>
/// <remarks>
/// A source that writes every message the moment it arrives waits for the storage. Here the two are
/// separated by a queue: <see cref="Enqueue"/> only puts the message into it, and a loop takes what
/// has accumulated and writes it to <see cref="Underlying"/> in batches - as soon as a batch reaches
/// <see cref="MaxBatchSize"/>, and in any case every <see cref="FlushInterval"/>.
/// Both the queue and the batches are bounded, so a storage that stopped taking data cannot grow
/// this drive until the process runs out of memory: past the bounds new data is dropped and counted
/// in <see cref="DroppedMessages"/>. A batch the storage refused is kept and written on the next
/// round, so a storage that comes back gets what was waiting for it.
/// </remarks>
public class BufferedMarketDataDrive : BaseLogReceiver, IMarketDataDrive
{
	/// <summary>
	/// How many messages may wait in the queue before new ones are dropped.
	/// </summary>
	public const int DefaultQueueCapacity = 1_000_000;

	/// <summary>
	/// How many messages may wait in the batches, which is where they wait while writes keep failing.
	/// </summary>
	public const long DefaultMaxBufferedMessages = 5_000_000;

	private readonly IStorageRegistry _storageRegistry;
	private readonly int _queueCapacity;
	private readonly long _maxBufferedMessages;

	// Completing a channel is final, so a drive that was stopped needs a new one to be started again.
	// Until it is started, "stopped" is what tells a refused write apart from a lost message.
	private Channel<Message> _queue;
	private volatile bool _stopped;
	private Task _loop;
	private CancellationTokenSource _cts;
	private long _dropped;
	private long _failedFlushes;

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferedMarketDataDrive"/>.
	/// </summary>
	/// <param name="underlying">The drive the data is actually written to.</param>
	/// <param name="storageRegistry">Registry the per-instrument storages are taken from.</param>
	public BufferedMarketDataDrive(IMarketDataDrive underlying, IStorageRegistry storageRegistry)
		: this(underlying, storageRegistry, DefaultQueueCapacity, DefaultMaxBufferedMessages)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BufferedMarketDataDrive"/>.
	/// </summary>
	/// <param name="underlying">The drive the data is actually written to.</param>
	/// <param name="storageRegistry">Registry the per-instrument storages are taken from.</param>
	/// <param name="queueCapacity">How many messages may wait to be written before new ones are dropped.</param>
	/// <param name="maxBufferedMessages">How many may wait in the batches, which is where they wait when a write keeps failing.</param>
	public BufferedMarketDataDrive(IMarketDataDrive underlying, IStorageRegistry storageRegistry, int queueCapacity, long maxBufferedMessages)
	{
		if (queueCapacity <= 0)
			throw new ArgumentOutOfRangeException(nameof(queueCapacity));

		if (maxBufferedMessages <= 0)
			throw new ArgumentOutOfRangeException(nameof(maxBufferedMessages));

		Underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
		_storageRegistry = storageRegistry ?? throw new ArgumentNullException(nameof(storageRegistry));
		_queueCapacity = queueCapacity;
		_maxBufferedMessages = maxBufferedMessages;
		_queue = CreateQueue();
	}

	/// <summary>
	/// The drive the data is actually written to.
	/// </summary>
	public IMarketDataDrive Underlying { get; }

	/// <summary>
	/// Format the buffered data is written in.
	/// </summary>
	public StorageFormats Format { get; set; } = StorageFormats.Binary;

	/// <summary>
	/// How long what has been given to the drive may wait before it is written.
	/// </summary>
	public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(10);

	/// <summary>
	/// How many messages are taken from the queue before a write is forced.
	/// </summary>
	public int MaxBatchSize { get; set; } = 100000;

	/// <summary>
	/// How many messages had to be thrown away because the drive could not take them: the queue was
	/// full, or what is buffered for the next write already is. Data counted here is gone.
	/// </summary>
	public long DroppedMessages => Interlocked.Read(ref _dropped);

	/// <summary>
	/// How many writes the underlying storage refused. The data of a refused write is kept.
	/// </summary>
	public long FailedFlushes => Interlocked.Read(ref _failedFlushes);

	private Channel<Message> CreateQueue()
		=> Channel.CreateBounded<Message>(new BoundedChannelOptions(_queueCapacity)
		{
			FullMode = BoundedChannelFullMode.Wait,
			SingleReader = true,
			SingleWriter = false,
		});

	/// <summary>
	/// Hands a message over to be written. The call does not wait for the write.
	/// </summary>
	/// <param name="message">What to write. Anything that names neither an instrument nor a data type is not market data and is ignored.</param>
	public void Enqueue(Message message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (TryGetDataType(message) is null)
			return;

		// A full queue means the writing is not keeping up (the storage stalled); drop the message
		// rather than block whoever is producing it. A drive that has been stopped is not a drive
		// that could not keep up: what arrives after it closed is not loss.
		if (!_queue.Writer.TryWrite(message) && !_stopped)
			Dropped();
	}

	/// <summary>
	/// Starts writing in the background.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public ValueTask StartAsync(CancellationToken cancellationToken)
	{
		if (_loop != null)
			return default;

		if (_stopped)
		{
			_queue = CreateQueue();
			_stopped = false;
		}

		// Held locally, not read from the field when the task gets to run: StopAsync clears the
		// field, and the token goes to the loop rather than to Task.Run, which would cancel the
		// delegate before it ever ran - and with it the drain and the last write.
		var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

		_cts = cts;
		_loop = Task.Run(() => LoopAsync(cts.Token), CancellationToken.None);

		LogInfo("Buffered writing started.");

		return default;
	}

	/// <summary>
	/// Stops writing in the background and writes down what is still waiting.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public async ValueTask StopAsync(CancellationToken cancellationToken)
	{
		_stopped = true;
		_queue.Writer.TryComplete();

		var cts = _cts;
		_cts = null;

		if (cts != null)
			await cts.CancelAsync();

		if (_loop != null)
		{
			try
			{
				await _loop.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
			}
			catch (OperationCanceledException) { }
			catch (TimeoutException ex) { LogWarning("Buffered writing did not drain within 30s: {0}", ex.Message); }
			catch (Exception ex) { LogError("Buffered writing faulted on shutdown: {0}", ex); }

			_loop = null;
		}

		// Only once the loop is done with it: the token is what the loop waits on, and a source
		// disposed under it turns an ordinary shutdown into a fault.
		cts?.Dispose();

		LogInfo("Buffered writing stopped.");
	}

	private static DataType TryGetDataType(Message message)
	{
		if (message is not ISecurityIdMessage)
			return null;

		return message switch
		{
			CandleMessage cm => cm.DataType,
			QuoteChangeMessage => DataType.MarketDepth,
			Level1ChangeMessage => DataType.Level1,
			ExecutionMessage em => em.DataTypeEx,
			_ => null,
		};
	}

	// Market data that will not be written down anywhere. The first one says so plainly, because a
	// counter that only ever grows is the kind of loss nobody notices.
	private void Dropped()
	{
		if (Interlocked.Increment(ref _dropped) == 1)
			LogError("Storage cannot take market data and it is being dropped; what is counted here is lost.");
	}

	private async Task LoopAsync(CancellationToken cancellationToken)
	{
		var lastFlush = DateTime.UtcNow;
		var buffers = new Dictionary<(SecurityId secId, DataType dataType), List<Message>>();
		var bufferedCount = 0L;

		// The token is passed in rather than captured: the last write of all runs after the loop was
		// cancelled, and writing with a cancelled token would throw away what it is there to save.
		async ValueTask FlushAsync(CancellationToken flushToken)
		{
			try
			{
				var flushed = 0L;

				foreach (var (key, list) in buffers.ToArray())
				{
					if (list.Count == 0)
						continue;

					var batchSize = list.Count;

					try
					{
						var storage = _storageRegistry.GetStorage(key.secId, key.dataType, Underlying, Format);
						await storage.SaveAsync(list, flushToken);
						flushed += batchSize;

						// Cleared only after a confirmed write - a failed one has to keep the batch
						// buffered for the next round instead of dropping market data.
						list.Clear();
						bufferedCount -= batchSize;
					}
					catch (Exception ex)
					{
						Interlocked.Increment(ref _failedFlushes);
						LogError("Failed to write {0} messages for {1}/{2}; keeping the batch for the next try: {3}",
							batchSize, key.secId, key.dataType, ex.Message);
					}
				}

				if (flushed > 0)
					LogInfo("Wrote {0} messages.", flushed);
			}
			catch (Exception ex)
			{
				LogError(ex);
			}

			lastFlush = DateTime.UtcNow;
		}

		void AddToBuffer(Message message)
		{
			if (message is not ISecurityIdMessage secMsg || TryGetDataType(message) is not DataType dataType)
				return;

			// If writes keep failing (the storage stalled) the batches grow without bound. Once the
			// cap is reached, drop new data so memory stays bounded while the storage recovers.
			if (bufferedCount >= _maxBufferedMessages)
			{
				Dropped();
				return;
			}

			buffers.SafeAdd((secMsg.SecurityId, dataType.Immutable())).Add(message);
			bufferedCount++;
		}

		while (!cancellationToken.IsCancellationRequested)
		{
			try
			{
				var waitTask = _queue.Reader.WaitToReadAsync(cancellationToken).AsTask();
				await Task.WhenAny(waitTask, FlushInterval.Delay(cancellationToken));

				var batchCount = 0;

				while (_queue.Reader.TryRead(out var message))
				{
					AddToBuffer(message);

					if (++batchCount >= MaxBatchSize)
					{
						await FlushAsync(cancellationToken);
						batchCount = 0;
					}
				}

				if ((DateTime.UtcNow - lastFlush) >= FlushInterval)
					await FlushAsync(cancellationToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				LogError(ex);
			}
		}

		// What is still in the queue was accepted from the producer and has to be written down; the
		// loop may have been cancelled before it got to read it.
		while (_queue.Reader.TryRead(out var pending))
			AddToBuffer(pending);

		await FlushAsync(CancellationToken.None);
	}

	string IMarketDataDrive.Path => Underlying.Path;

	IAsyncEnumerable<SecurityId> IMarketDataDrive.GetAvailableSecuritiesAsync()
		=> Underlying.GetAvailableSecuritiesAsync();

	IAsyncEnumerable<DataType> IMarketDataDrive.GetAvailableDataTypesAsync(SecurityId securityId, StorageFormats format)
		=> Underlying.GetAvailableDataTypesAsync(securityId, format);

	IMarketDataStorageDrive IMarketDataDrive.GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format)
		=> Underlying.GetStorageDrive(securityId, dataType, format);

	ValueTask IMarketDataDrive.VerifyAsync(CancellationToken cancellationToken)
		=> Underlying.VerifyAsync(cancellationToken);

	IAsyncEnumerable<SecurityMessage> IMarketDataDrive.LookupSecuritiesAsync(SecurityLookupMessage criteria, ISecurityProvider securityProvider)
		=> Underlying.LookupSecuritiesAsync(criteria, securityProvider);

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Format = storage.GetValue(nameof(Format), Format);
		FlushInterval = storage.GetValue(nameof(FlushInterval), FlushInterval);
		MaxBatchSize = storage.GetValue(nameof(MaxBatchSize), MaxBatchSize);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Format), Format);
		storage.SetValue(nameof(FlushInterval), FlushInterval);
		storage.SetValue(nameof(MaxBatchSize), MaxBatchSize);
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_stopped = true;
		_queue.Writer.TryComplete();

		// Cancelled, not just let go of: the loop is waiting on this token, and a source that is
		// merely disposed leaves it turning over a queue nobody will write to again. Not disposed
		// here - nothing can await the loop from Dispose, and the token has to outlive it.
		_cts?.Cancel();
		_cts = null;

		base.DisposeManaged();
	}
}
