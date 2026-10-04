namespace StockSharp.Algo.Storages;

using System.Diagnostics;

using StockSharp.Algo.Storages.Binary.Snapshot;

/// <summary>
/// Snapshot storage registry.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SnapshotRegistry"/>.
/// </remarks>
/// <param name="fileSystem">File system.</param>
/// <param name="path">Path to storage.</param>
public class SnapshotRegistry(IFileSystem fileSystem, string path) : Disposable, ISnapshotRegistry
{
	private abstract class SnapshotStorage : ISnapshotStorage
	{
		public abstract ValueTask<List<Exception>> FlushChangesAsync(CancellationToken cancellationToken);
		public abstract IEnumerable<DateTime> Dates { get; }
		public abstract ValueTask ClearAllAsync(CancellationToken cancellationToken);
		public abstract ValueTask ClearAsync(object key, CancellationToken cancellationToken);
		public abstract ValueTask UpdateAsync(Message message, CancellationToken cancellationToken);
		public abstract ValueTask<Message> GetAsync(object key, CancellationToken cancellationToken);
		public abstract IAsyncEnumerable<Message> GetAllAsync(DateTime? from, DateTime? to);
	}

	private class SnapshotStorage<TKey, TMessage> : SnapshotStorage, ISnapshotStorage<TKey, TMessage>
		where TMessage : Message, ISecurityIdMessage, IServerTimeMessage
	{
		private class SnapshotStorageDate
		{
			private readonly HashSet<TKey> _dirtyKeys = [];
			private readonly SynchronizedDictionary<TKey, TMessage> _snapshots = [];
			private readonly Dictionary<TKey, byte[]> _buffers = [];
			private readonly ISnapshotSerializer<TKey, TMessage> _serializer;
			private readonly IFileSystem _fileSystem;
			private readonly string _fileName;
			private Version _version;
			//private long _currOffset;
			private bool _resetFile;

			// version has 2 bytes
			//private const int _versionLen = 2;

			// buffer length 4 bytes
			//private const int _bufSizeLen = 4;

			public SnapshotStorageDate(string fileName, ISnapshotSerializer<TKey, TMessage> serializer, IFileSystem fileSystem)
			{
				if (fileName.IsEmpty())
					throw new ArgumentNullException(nameof(fileName));

				_fileName = fileName;
				_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
				_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
			}

			public async Task LoadAsync(CancellationToken cancellationToken)
			{
				if (_fileSystem.FileExists(_fileName))
				{
					Debug.WriteLine($"Snapshot (Load): {_fileName}");
					var streamOpened = false;
					var hasInvalidSnapshot = false;

					try
					{
						using (var stream = _fileSystem.OpenRead(_fileName))
						{
							streamOpened = true;
							_version = new Version(stream.ReadByte(), stream.ReadByte());

							if (_version > _serializer.Version)
								new InvalidOperationException(LocalizedStrings.StorageVersionNewerKey.Put(_fileName, _version, _serializer.Version)).LogError();

							while (stream.Position < stream.Length)
							{
								var size = stream.Read<int>();

								var buffer = new byte[size];
								stream.ReadBytes(buffer, buffer.Length);

								//var offset = stream.Position;

								TMessage message;

								try
								{
									message = await _serializer.DeserializeAsync(_version, buffer, cancellationToken);
								}
								catch (Exception ex)
								{
									hasInvalidSnapshot = true;
									ex.LogError();
									continue;
								}

								var key = _serializer.GetKey(message);

								_snapshots.Add(key, message);
								_buffers.Add(key, buffer);
							}

							//_currOffset = stream.Length;
						}

						if (hasInvalidSnapshot && _snapshots.Count == 0)
						{
							MoveCorruptFileToBackup();
							_version = _serializer.Version;
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Snapshot (ERROR): {ex.Message}");
						ex.LogError();

						if (streamOpened && _snapshots.Count == 0)
							MoveCorruptFileToBackup();

						if (_snapshots.Count == 0)
							_version = _serializer.Version;
					}
				}
				else
				{
					_version = _serializer.Version;

					//_currOffset = _versionLen;
				}
			}

			private void MoveCorruptFileToBackup()
			{
				try
				{
					_fileName.MoveToBackup(_fileSystem);
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}

			public void ClearAll()
			{
				using (_snapshots.EnterScope())
				{
					_snapshots.Clear();
					_dirtyKeys.Clear();
					_resetFile = true;
				}
			}

			public void Clear(TKey key)
			{
				using (_snapshots.EnterScope())
				{
					_snapshots.Remove(key);
					_dirtyKeys.Remove(key);
					_resetFile = true;
				}
			}

			public void Update(TMessage curr)
			{
				if (curr is null)
					throw new ArgumentNullException(nameof(curr));

				var key = _serializer.GetKey(curr);

				using (_snapshots.EnterScope())
				{
					var prev = _snapshots.TryGetValue(key);

					if (prev is null)
					{
						if (curr is ExecutionMessage execMsg && execMsg.OrderState == OrderStates.Failed)
							return;

						if (curr.SecurityId == default)
							throw new ArgumentException(curr.ToString());

						_snapshots.Add(key, curr.TypedClone());
					}
					else
					{
						_serializer.Update(prev, curr);
					}

					_dirtyKeys.Add(key);
				}
			}

			public TMessage Get(TKey key)
			{
				using (_snapshots.EnterScope())
					return (TMessage)_snapshots.TryGetValue(key)?.Clone();
			}

			public IEnumerable<TMessage> GetAll(DateTime? from, DateTime? to)
			{
				using (_snapshots.EnterScope())
				{
					return [.. _snapshots.Values.Where(m =>
					{
						if (from == null && to == null)
							return true;

						var time = m.ServerTime;

						if (from != null && from > time)
							return false;

						if (to != null && to < time)
							return false;

						return true;
					}).Select(m => m.TypedClone())];
				}
			}

			public async ValueTask FlushChangesAsync(CancellationToken cancellationToken)
			{
				// TODO Optimize memory

				(TKey key, TMessage message)[] changed;
				bool resetFile;

				using (_snapshots.EnterScope())
				{
					resetFile = _resetFile;

					if (!resetFile && _dirtyKeys.Count == 0)
						return;

					changed = resetFile
						? [.. _snapshots.Select(p => (p.Key, p.Value.TypedClone()))]
						: [.. _dirtyKeys.Select(k => (k, _snapshots[k].TypedClone()))];

					_dirtyKeys.Clear();
					_resetFile = false;
				}

				try
				{
					var serialized = new List<(TKey key, byte[] buffer)>(changed.Length);

					foreach (var (key, message) in changed)
						serialized.Add((key, await _serializer.SerializeAsync(_version, message, cancellationToken)));

					IEnumerable<byte[]> buffers;

					using (_snapshots.EnterScope())
					{
						if (resetFile)
							_buffers.Clear();

						foreach (var (key, buffer) in serialized)
							_buffers[key] = buffer;

						buffers = [.. _buffers.Values];
					}

					_fileSystem.CreateDirectory(Path.GetDirectoryName(_fileName));

					Debug.WriteLine($"Snapshot (Save): {_fileName}");

					using var stream = _fileSystem.OpenWrite(_fileName);

					stream.WriteByte((byte)_version.Major);
					stream.WriteByte((byte)_version.Minor);

					foreach (var buffer in buffers)
					{
						stream.WriteEx(buffer);
					}
				}
				catch
				{
					// What was taken is not known to be in the file, so it stays due for the next flush.
					using (_snapshots.EnterScope())
					{
						if (resetFile)
							_resetFile = true;

						foreach (var (key, _) in changed)
						{
							if (_snapshots.ContainsKey(key))
								_dirtyKeys.Add(key);
						}
					}

					throw;
				}
			}
		}

		private readonly string _path;
		private readonly string _fileNameWithExtension;
		private readonly string _datesPath;

		private bool _flushDates;

		private readonly Lock _cacheSync = new();

		// Loaded dates, which are the ones a flush writes; a date is read once, the first time it is asked for.
		private readonly CachedSynchronizedDictionary<DateTime, SnapshotStorageDate> _dates = [];
		private readonly CachedSynchronizedDictionary<DateTime, Task<SnapshotStorageDate>> _loading = [];

		private readonly ISnapshotSerializer<TKey, TMessage> _serializer;
		private readonly IFileSystem _fileSystem;

		public SnapshotStorage(string path, ISnapshotSerializer<TKey, TMessage> serializer, IFileSystem fileSystem)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
			_fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

			_path = path.ToFullPath();
			_fileNameWithExtension = _serializer.Name.ToLowerInvariant() + ".bin";
			_datesPath = Path.Combine(_path, _serializer.Name + "Dates.txt");

			_datesDict = new(() =>
			{
				var retVal = new CachedSynchronizedOrderedDictionary<DateTime, DateTime>();

				if (_fileSystem.FileExists(_datesPath))
				{
					foreach (var date in LoadDates())
						retVal.Add(date, date);
				}
				else
				{
					var dates = _fileSystem
						.GetDirectories(_path)
						.Where(dir => _fileSystem.FileExists(Path.Combine(dir, _fileNameWithExtension)))
						.Select(dir => LocalMarketDataDrive.GetDate(Path.GetFileName(dir)));

					foreach (var date in dates)
						retVal.Add(date, date);

					SaveDates(retVal.CachedValues);
				}

				return retVal;
			});
		}

		public override IEnumerable<DateTime> Dates => DatesDict.CachedValues;

		private readonly ResettableLazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>> _datesDict;

		private CachedSynchronizedOrderedDictionary<DateTime, DateTime> DatesDict => _datesDict.Value;

		public void ClearDatesCache()
		{
			if (_fileSystem.DirectoryExists(_path))
			{
				using (_cacheSync.EnterScope())
					_fileSystem.DeleteFile(_datesPath);
			}

			ResetCache();
		}

		public override async ValueTask ClearAllAsync(CancellationToken cancellationToken)
		{
			foreach (var date in await LoadedAsync(cancellationToken))
				date.ClearAll();
		}

		async ValueTask ISnapshotStorage<TKey, TMessage>.ClearAsync(TKey key, CancellationToken cancellationToken)
		{
			foreach (var date in await LoadedAsync(cancellationToken))
				date.Clear(key);
		}

		public override ValueTask ClearAsync(object key, CancellationToken cancellationToken)
			=> ((ISnapshotStorage<TKey, TMessage>)this).ClearAsync((TKey)key, cancellationToken);

		public override async ValueTask UpdateAsync(Message message, CancellationToken cancellationToken)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var curr = (TMessage)message;

			var date = curr.ServerTime.Date;

			if (date == default)
				throw new ArgumentException(message.ToString());

			(await GetStorageDateAsync(date, cancellationToken)).Update(curr);

			using (DatesDict.EnterScope())
			{
				if (DatesDict.TryAdd2(date, date))
					_flushDates = true;
			}
		}

		async ValueTask<TMessage> ISnapshotStorage<TKey, TMessage>.GetAsync(TKey key, CancellationToken cancellationToken)
		{
			foreach (var date in Dates.OrderByDescending())
			{
				var snapshot = (await GetStorageDateAsync(date, cancellationToken)).Get(key);

				if (snapshot != null)
					return snapshot;
			}

			return null;
		}

		public override async ValueTask<Message> GetAsync(object key, CancellationToken cancellationToken)
			=> await ((ISnapshotStorage<TKey, TMessage>)this).GetAsync((TKey)key, cancellationToken);

		IAsyncEnumerable<TMessage> ISnapshotStorage<TKey, TMessage>.GetAllAsync(DateTime? from, DateTime? to)
			=> GetAllCoreAsync(from, to);

		private async IAsyncEnumerable<TMessage> GetAllCoreAsync(DateTime? from, DateTime? to, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var dates = Dates;

			var fromDate = from?.Date;
			var toDate = to?.Date;

			if (fromDate != null)
				dates = dates.Where(d => d >= fromDate.Value);

			if (toDate != null)
				dates = dates.Where(d => d <= toDate.Value);

			foreach (var d in dates.ToArray())
			{
				var f = d == fromDate ? from : null;
				var t = d == toDate ? to : null;

				foreach (var snapshot in (await GetStorageDateAsync(d, cancellationToken)).GetAll(f, t))
					yield return snapshot;
			}
		}

		public override async IAsyncEnumerable<Message> GetAllAsync(DateTime? from, DateTime? to)
		{
			await foreach (var snapshot in GetAllCoreAsync(from, to))
				yield return snapshot;
		}

		private Task<SnapshotStorageDate> GetStorageDateAsync(DateTime date, CancellationToken cancellationToken)
			=> _loading.SafeAdd(date, key => Task.Run(() => LoadStorageDateAsync(key))).WaitAsync(cancellationToken);

		// Read without the caller's token: the result is shared by every later caller of the same date.
		private async Task<SnapshotStorageDate> LoadStorageDateAsync(DateTime date)
		{
			var storage = new SnapshotStorageDate(Path.Combine(_path, LocalMarketDataDrive.GetDirName(date), _fileNameWithExtension), _serializer, _fileSystem);

			await storage.LoadAsync(CancellationToken.None);

			_dates[date] = storage;

			return storage;
		}

		private async ValueTask<SnapshotStorageDate[]> LoadedAsync(CancellationToken cancellationToken)
			=> await Task.WhenAll(_loading.CachedValues).WaitAsync(cancellationToken);

		private IEnumerable<DateTime> LoadDates()
		{
			try
			{
				return Do.Invariant(() =>
				{
					using var reader = new StreamReader(_fileSystem.OpenRead(_datesPath));

					var dates = new List<DateTime>();

					while (true)
					{
						var line = reader.ReadLine();

						if (line == null)
							break;

						dates.Add(LocalMarketDataDrive.GetDate(line));
					}

					return dates;
				});
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.ErrorReadFile.Put(_datesPath), ex);
			}
		}

		private void SaveDates(DateTime[] dates)
		{
			try
			{
				if (!_fileSystem.DirectoryExists(_path))
				{
					if (dates.IsEmpty())
						return;

					_fileSystem.CreateDirectory(_path);
				}

				var stream = new MemoryStream();

				Do.Invariant(() =>
				{
					var writer = new StreamWriter(stream) { AutoFlush = true };

					foreach (var date in dates)
					{
						writer.WriteLine(LocalMarketDataDrive.GetDirName(date));
					}
				});

				using (_cacheSync.EnterScope())
				{
					stream.Position = 0;
					_fileSystem.WriteAllBytes(_datesPath, stream.To<byte[]>());
				}
			}
			catch (UnauthorizedAccessException)
			{
				// если папка с данными с правами только на чтение
			}
		}

		public void ResetCache()
		{
			_datesDict.Reset();
		}

		public override async ValueTask<List<Exception>> FlushChangesAsync(CancellationToken cancellationToken)
		{
			var errors = new List<Exception>();

			foreach (var d in _dates.CachedValues)
			{
				try
				{
					await d.FlushChangesAsync(cancellationToken);
				}
				catch (Exception ex)
				{
					errors.Add(ex);
				}
			}

			var saveDates = false;

			try
			{
				using (DatesDict.EnterScope())
				{
					if (_flushDates)
						saveDates = true;
				}

				if (saveDates)
					SaveDates(DatesDict.CachedValues);
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}

			return errors;
		}
	}

	private readonly IFileSystem _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
	private readonly CachedSynchronizedDictionary<DataType, SnapshotStorage> _snapshotStorages = [];
	private ControllablePeriodicTimer _timer;

	/// <summary>
	/// Initializes a new instance of the <see cref="SnapshotRegistry"/> class.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	[Obsolete("Use overload with IFileSystem.")]
	public SnapshotRegistry(string path)
		: this(Paths.FileSystem, path)
	{
	}

	ValueTask ISnapshotRegistry.InitAsync(CancellationToken cancellationToken)
	{
		var isFlushing = false;
		var flushLock = new Lock();

		_timer = AsyncHelper.CreatePeriodicTimer(async () =>
		{
			using (flushLock.EnterScope())
			{
				if (isFlushing)
					return;

				isFlushing = true;
			}

			try
			{
				var errors = new List<Exception>();

				foreach (var storage in _snapshotStorages.CachedValues)
					errors.AddRange(await storage.FlushChangesAsync(cancellationToken));

				if (errors.Count > 0)
					throw new AggregateException(errors);
			}
			catch (Exception ex)
			{
				ex.LogError();
			}

			using (flushLock.EnterScope())
			{
				isFlushing = false;
			}
		}).Start(TimeSpan.FromSeconds(10), cancellationToken: cancellationToken);

		return default;
	}

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		if (_timer != null)
		{
			_timer.Dispose();
			_timer = null;
		}

		base.DisposeManaged();
	}

	ISnapshotStorage ISnapshotRegistry.GetSnapshotStorage(DataType dataType)
	{
		return _snapshotStorages.SafeAdd(dataType, key =>
		{
			if (key == DataType.Level1)
				return new SnapshotStorage<SecurityId, Level1ChangeMessage>(path, new Level1BinarySnapshotSerializer(), _fileSystem);
			else if (key == DataType.MarketDepth)
				return new SnapshotStorage<SecurityId, QuoteChangeMessage>(path, new QuotesBinarySnapshotSerializer(), _fileSystem);
			else if (key == DataType.PositionChanges)
				return new SnapshotStorage<(SecurityId, string, string), PositionChangeMessage>(path, new PositionBinarySnapshotSerializer(), _fileSystem);
			else if (key == DataType.Transactions)
				return new SnapshotStorage<string, ExecutionMessage>(path, new TransactionBinarySnapshotSerializer(), _fileSystem);
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
		});
	}
}
