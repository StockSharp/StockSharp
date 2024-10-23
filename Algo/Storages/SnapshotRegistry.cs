namespace StockSharp.Algo.Storages;

using System.Diagnostics;

using Ecng.Reflection;
using Ecng.Interop;

using StockSharp.Algo.Storages.Binary.Snapshot;

/// <summary>
/// Snapshot storage registry.
/// </summary>
public class SnapshotRegistry : Disposable
{
	private abstract class SnapshotStorage : ISnapshotStorage
	{
		public abstract List<Exception> FlushChanges();
		public abstract IEnumerable<DateTime> Dates { get; }
		public abstract void ClearAll();
		public abstract void Clear(object key);
		public abstract void Update(Message message);
		public abstract Message Get(object key);
		public abstract IEnumerable<Message> GetAll(DateTimeOffset? from, DateTimeOffset? to);
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
			private readonly Version _version;
			private readonly string _fileName;
			//private long _currOffset;
			private bool _resetFile;

			// version has 2 bytes
			//private const int _versionLen = 2;

			// buffer length 4 bytes
			//private const int _bufSizeLen = 4;

			public SnapshotStorageDate(string fileName, ISnapshotSerializer<TKey, TMessage> serializer)
			{
				if (fileName.IsEmpty())
					throw new ArgumentNullException(nameof(fileName));

				_fileName = fileName;
				_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

				if (File.Exists(_fileName))
				{
					Debug.WriteLine($"Snapshot (Load): {_fileName}");

					try
					{
						var allError = true;

						using (var stream = File.OpenRead(_fileName))
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
							File.Delete(_fileName);
						}
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"Snapshot (ERROR): {ex.Message}");
						ex.LogError();
						File.Delete(_fileName);
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
				lock (_snapshots.SyncRoot)
				{
					_snapshots.Clear();
					_dirtyKeys.Clear();
					_resetFile = true;
				}
			}

			public void Clear(TKey key)
			{
				lock (_snapshots.SyncRoot)
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

				lock (_snapshots.SyncRoot)
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
				lock (_snapshots.SyncRoot)
					return (TMessage)_snapshots.TryGetValue(key)?.Clone();
			}

			public IEnumerable<TMessage> GetAll(DateTimeOffset? from, DateTimeOffset? to)
			{
				lock (_snapshots.SyncRoot)
				{
					return _snapshots.Values.Where(m =>
					{
						if (from == null && to == null)
							return true;

						var time = m.ServerTime;

						if (from != null && from > time)
							return false;

						if (to != null && to < time)
							return false;

						return true;
					}).Select(m => m.TypedClone()).ToArray();
				}
			}

			public void FlushChanges()
			{
				// TODO Optimize memory

				IEnumerable<byte[]> buffers;

				lock (_snapshots.SyncRoot)
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

				Directory.CreateDirectory(Path.GetDirectoryName(_fileName));

				Debug.WriteLine($"Snapshot (Save): {_fileName}");

				using (var stream = new TransactionFileStream(_fileName, FileMode.Create))
				{
					stream.WriteByte((byte)_version.Major);
					stream.WriteByte((byte)_version.Minor);

					foreach (var buffer in buffers)
					{
						stream.WriteEx(buffer);
					}
				}
			}
		}

		private readonly string _path;
		private readonly string _fileNameWithExtension;
		private readonly string _datesPath;

		private bool _flushDates;

		private readonly SyncObject _cacheSync = new();

		private readonly CachedSynchronizedDictionary<DateTime, SnapshotStorageDate> _dates = [];

		private readonly ISnapshotSerializer<TKey, TMessage> _serializer;

		public SnapshotStorage(string path, ISnapshotSerializer<TKey, TMessage> serializer)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

			_path = path.ToFullPath();
			_fileNameWithExtension = _serializer.Name.ToLowerInvariant() + ".bin";
			_datesPath = Path.Combine(_path, _serializer.Name + "Dates.txt");

			_datesDict = new Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>>(() =>
			{
				var retVal = new CachedSynchronizedOrderedDictionary<DateTime, DateTime>();

				if (File.Exists(_datesPath))
				{
					foreach (var date in LoadDates())
						retVal.Add(date, date);
				}
				else
				{
					var dates = IOHelper
					            .GetDirectories(_path)
					            .Where(dir => File.Exists(Path.Combine(dir, _fileNameWithExtension)))
					            .Select(dir => LocalMarketDataDrive.GetDate(Path.GetFileName(dir)));

					foreach (var date in dates)
						retVal.Add(date, date);

					SaveDates(retVal.CachedValues);
				}

				return retVal;
			}).Track();
		}

		public override IEnumerable<DateTime> Dates => DatesDict.CachedValues;

		private readonly Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>> _datesDict;

		private CachedSynchronizedOrderedDictionary<DateTime, DateTime> DatesDict => _datesDict.Value;

		public void ClearDatesCache()
		{
			if (Directory.Exists(_path))
			{
				lock (_cacheSync)
					File.Delete(_datesPath);
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

			var date = curr.ServerTime.UtcDateTime.Date;

			if (date == default)
				throw new ArgumentException(message.ToString());

			GetStorageDate(date).Update(curr);

			lock (DatesDict.SyncRoot)
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

		IEnumerable<TMessage> ISnapshotStorage<TKey, TMessage>.GetAll(DateTimeOffset? from, DateTimeOffset? to)
		{
			var dates = Dates;

			var fromDate = from?.UtcDateTime.Date;
			var toDate = to?.UtcDateTime.Date;

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

		public override IEnumerable<Message> GetAll(DateTimeOffset? from, DateTimeOffset? to)
		{
			return ((ISnapshotStorage<TKey, TMessage>)this).GetAll(from, to);
		}

		private SnapshotStorageDate GetStorageDate(DateTime date)
		{
			return _dates.SafeAdd(date, key => new SnapshotStorageDate(Path.Combine(_path, LocalMarketDataDrive.GetDirName(key), _fileNameWithExtension), _serializer));
		}

		private IEnumerable<DateTime> LoadDates()
		{
			try
			{
				return Do.Invariant(() =>
				{
					using (var reader = new StreamReader(new FileStream(_datesPath, FileMode.Open, FileAccess.Read)))
					{
						var dates = new List<DateTime>();

						while (true)
						{
							var line = reader.ReadLine();

							if (line == null)
								break;

							dates.Add(LocalMarketDataDrive.GetDate(line));
						}

						return dates;
					}
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
				if (!Directory.Exists(_path))
				{
					if (dates.IsEmpty())
						return;

					Directory.CreateDirectory(_path);
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

				lock (_cacheSync)
				{
					stream.Position = 0;
					stream.Save(_datesPath);
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
				lock (DatesDict.SyncRoot)
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

	private readonly string _path;
	private readonly CachedSynchronizedDictionary<DataType, SnapshotStorage> _snapshotStorages = [];
	private Timer _timer;

	/// <summary>
	/// Initializes a new instance of the <see cref="SnapshotRegistry"/>.
	/// </summary>
	/// <param name="path">Path to storage.</param>
	public SnapshotRegistry(string path)
	{
		_path = path;
	}

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	public void Init()
	{
		var isFlushing = false;
		var flushLock = new SyncObject();

		_timer = ThreadingHelper.Timer(() =>
		{
			lock (flushLock)
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

			lock (flushLock)
			{
				isFlushing = false;
			}
		}).Interval(TimeSpan.FromSeconds(10));
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

	/// <summary>
	/// To get the snapshot storage.
	/// </summary>
	/// <param name="dataType">Market data type.</param>
	/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, candle arg.</param>
	/// <returns>The snapshot storage.</returns>
	public ISnapshotStorage GetSnapshotStorage(Type dataType, object arg)
	{
		return _snapshotStorages.SafeAdd(DataType.Create(dataType, arg), key =>
		{
			SnapshotStorage storage;

			if (dataType == typeof(Level1ChangeMessage))
				storage = new SnapshotStorage<SecurityId, Level1ChangeMessage>(_path, new Level1BinarySnapshotSerializer());
			else if (dataType == typeof(QuoteChangeMessage))
				storage = new SnapshotStorage<SecurityId, QuoteChangeMessage>(_path, new QuotesBinarySnapshotSerializer());
			else if (dataType == typeof(PositionChangeMessage))
				storage = new SnapshotStorage<Tuple<SecurityId, string, string>, PositionChangeMessage>(_path, new PositionBinarySnapshotSerializer());
			else if (dataType == typeof(ExecutionMessage))
			{
				switch ((ExecutionTypes)arg)
				{
					case ExecutionTypes.Transaction:
						storage = new SnapshotStorage<string, ExecutionMessage>(_path, new TransactionBinarySnapshotSerializer());
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(arg), arg, LocalizedStrings.InvalidValue);
				}
			}
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);

			return storage;
		});
	}
}