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
		public abstract List<Exception> FlushChanges();
		public abstract IEnumerable<DateTime> Dates { get; }
		public abstract void ClearAll();
		public abstract void Clear(object key);
		public abstract void Update(Message message);
		public abstract Message Get(object key);
		public abstract IEnumerable<Message> GetAll(DateTime? from, DateTime? to);
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
			private readonly Version _version;
			private readonly string _fileName;
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

				if (_fileSystem.FileExists(_fileName))
				{
					Debug.WriteLine($"Snapshot (Load): {_fileName}");

					try
					{
						var allError = true;

						using (var stream = _fileSystem.OpenRead(_fileName))
						{
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
									message = _serializer.Deserialize(_version, buffer);
									allError = false;
								}
								catch (Exception ex)
								{
									ex.LogError();
									continue;
								}

								var key = _serializer.GetKey(message);

								_snapshots.Add(key, message);
								_buffers.Add(key, buffer);
							}

							//_currOffset = stream.Length;
						}

						if (allError)
						{
							_fileSystem.DeleteFile(_fileName);
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Snapshot (ERROR): {ex.Message}");
						ex.LogError();
						_fileSystem.DeleteFile(_fileName);
					}
				}
				else
				{
					_version = _serializer.Version;

					//_currOffset = _versionLen;
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

			public void FlushChanges()
			{
				// TODO Optimize memory

				IEnumerable<byte[]> buffers;

				using (_snapshots.EnterScope())
				{
					if (!_resetFile)
					{
						if (_dirtyKeys.Count == 0)
							return;

						foreach (var key in _dirtyKeys)
							_buffers[key] = _serializer.Serialize(_version, _snapshots[key]);
					}
					else
					{
						_buffers.Clear();

						foreach (var pair in _snapshots)
							_buffers.Add(pair.Key, _serializer.Serialize(_version, pair.Value));
					}

					_dirtyKeys.Clear();

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
		}

		private readonly string _path;
		private readonly string _fileNameWithExtension;
		private readonly string _datesPath;

		private bool _flushDates;

		private readonly Lock _cacheSync = new();

		private readonly CachedSynchronizedDictionary<DateTime, SnapshotStorageDate> _dates = [];

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

		public override void ClearAll()
		{
			_dates.CachedValues.ForEach(d => d.ClearAll());
		}

		void ISnapshotStorage<TKey, TMessage>.Clear(TKey key)
		{
			_dates.CachedValues.ForEach(d => d.Clear(key));
		}

		public override void Clear(object key)
		{
			((ISnapshotStorage<TKey, TMessage>)this).Clear((TKey)key);
		}

		public override void Update(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var curr = (TMessage)message;

			var date = curr.ServerTime.Date;

			if (date == default)
				throw new ArgumentException(message.ToString());

			GetStorageDate(date).Update(curr);

			using (DatesDict.EnterScope())
			{
				if (DatesDict.TryAdd2(date, date))
					_flushDates = true;
			}
		}

		TMessage ISnapshotStorage<TKey, TMessage>.Get(TKey key)
		{
			foreach (var date in Dates.OrderByDescending())
			{
				var snapshot = GetStorageDate(date).Get(key);

				if (snapshot != null)
					return snapshot;
			}

			return null;
		}

		public override Message Get(object key)
		{
			return ((ISnapshotStorage<TKey, TMessage>)this).Get((TKey)key);
		}

		IEnumerable<TMessage> ISnapshotStorage<TKey, TMessage>.GetAll(DateTime? from, DateTime? to)
		{
			var dates = Dates;

			var fromDate = from?.Date;
			var toDate = to?.Date;

			if (fromDate != null)
				dates = dates.Where(d => d >= fromDate.Value);

			if (toDate != null)
				dates = dates.Where(d => d <= toDate.Value);

			return dates.SelectMany(d =>
			{
				var f = d == fromDate ? from : null;
				var t = d == toDate ? to : null;
				return GetStorageDate(d).GetAll(f, t);
			});
		}

		public override IEnumerable<Message> GetAll(DateTime? from, DateTime? to)
		{
			return ((ISnapshotStorage<TKey, TMessage>)this).GetAll(from, to);
		}

		private SnapshotStorageDate GetStorageDate(DateTime date)
		{
			return _dates.SafeAdd(date, key => new SnapshotStorageDate(Path.Combine(_path, LocalMarketDataDrive.GetDirName(key), _fileNameWithExtension), _serializer, _fileSystem));
		}

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

		public override List<Exception> FlushChanges()
		{
			var errors = new List<Exception>();

			_dates.CachedValues.ForEach(d =>
			{
				try
				{
					d.FlushChanges();
				}
				catch (Exception ex)
				{
					errors.Add(ex);
				}
			});

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

		_timer = AsyncHelper.CreatePeriodicTimer(() =>
		{
			using (flushLock.EnterScope())
			{
				if (isFlushing)
					return;

				isFlushing = true;
			}

			try
			{
				var errors = _snapshotStorages.CachedValues.SelectMany(s => s.FlushChanges()).ToArray();

				if (errors.Length > 0)
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