namespace StockSharp.Algo.Storages;

using IOPath = System.IO.Path;

using Ecng.Reflection;

using StockSharp.Algo.Storages.Binary;

/// <summary>
/// The file storage for market data.
/// </summary>
public class LocalMarketDataDrive : BaseMarketDataDrive
{
	private class LocalMarketDataStorageDrive : IMarketDataStorageDrive
	{
		private static readonly Version _ver10 = new(1, 0);

		private readonly string _path;
		private readonly string _fileNameWithExtension;
		private readonly string _datesPath;
		private readonly string _datesPathObsoleteBin;
		private readonly string _datesPathObsoleteTxt;
		private readonly DataType _dataType;
		private readonly SecurityId _secId;
		private readonly StorageFormats _format;
		private readonly SyncObject _cacheSync = new();

		public LocalMarketDataStorageDrive(DataType dataType, SecurityId secId, StorageFormats format, LocalMarketDataDrive drive)
		{
			_dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
			_secId = secId;
			_format = format;
			_drive = drive ?? throw new ArgumentNullException(nameof(drive));

			var fileName = GetFileName(_dataType);

			_path = drive.GetSecurityPath(_secId);
			_fileNameWithExtension = fileName + GetExtension(_format);

			var datesPath = IOPath.Combine(_path, $"{fileName}{_format}Dates");
			_datesPath = $"{datesPath}2.bin";
			_datesPathObsoleteBin = $"{datesPath}.bin";
			_datesPathObsoleteTxt = $"{datesPath}.txt";

			_datesDict = new Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>>(() =>
			{
				var retVal = new CachedSynchronizedOrderedDictionary<DateTime, DateTime>();

				var save = false;
				IEnumerable<DateTime> dates;

				if (_drive.TryGetIndex(out var index))
				{
					dates = index.GetDates(_secId, _dataType, format);
				}
				else if (File.Exists(_datesPath))
				{
					dates = LoadDates();
				}
				else if (File.Exists(_datesPathObsoleteBin))
				{
					dates = LoadDatesObsoleteBin();
				}
				else if (File.Exists(_datesPathObsoleteTxt))
				{
					dates = LoadDatesObsoleteTxt();
				}
				else
				{
					dates = IOHelper
						.GetDirectories(_path)
						.Where(dir => File.Exists(IOPath.Combine(dir, _fileNameWithExtension)))
						.Select(dir => GetDate(IOPath.GetFileName(dir)));

					save = true;
				}

				foreach (var date in dates)
					retVal.Add(date, date);

				if (save)
					SaveDates(retVal.CachedValues);

				return retVal;
			}).Track();
		}

		private readonly LocalMarketDataDrive _drive;
		IMarketDataDrive IMarketDataStorageDrive.Drive => _drive;

		IEnumerable<DateTime> IMarketDataStorageDrive.Dates => DatesDict.CachedValues;

		private readonly Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>> _datesDict;
		private CachedSynchronizedOrderedDictionary<DateTime, DateTime> DatesDict => _datesDict.Value;

		public void ClearDatesCache()
		{
			if (Directory.Exists(_path))
			{
				lock (_cacheSync)
				{
					File.Delete(_datesPath);
					File.Delete(_datesPathObsoleteBin);
					File.Delete(_datesPathObsoleteTxt);
				}
			}

			ResetCache();
		}

		private void ChangeIndex(DateTime date, bool remove)
		{
			if (_drive.TryGetIndex(out var index))
				index.ChangeDate(_secId, _format, _dataType, date, remove);
		}

		void IMarketDataStorageDrive.Delete(DateTime date)
		{
			date = date.UtcKind();

			var path = GetPath(date, true);

			if (File.Exists(path))
			{
				File.Delete(path);

				var dir = GetDirectoryName(path);

				if (Directory.EnumerateFiles(dir).IsEmpty())
				{
					lock (_cacheSync)
						IOHelper.BlockDeleteDir(dir);
				}
			}

			DatesDict.Remove(date);
			SaveDates(DatesDict.CachedValues);
			ChangeIndex(date, true);

			_availableDataTypes.Remove(_drive.Path);
		}

		void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
		{
			date = date.UtcKind();

			Directory.CreateDirectory(GetDataPath(date));

			using (var file = new FileStream(GetPath(date, false), FileMode.Create, FileAccess.Write))
				stream.CopyTo(file);

			DatesDict[date] = date;
			SaveDates(DatesDict.CachedValues);
			ChangeIndex(date, false);

			lock (_availableDataTypes.SyncRoot)
			{
				var tuple = _availableDataTypes.TryGetValue(_drive.Path);

				if (tuple == null || !tuple.Second)
					return;

				tuple.First.Add(_dataType);
			}
		}

		Stream IMarketDataStorageDrive.LoadStream(DateTime date, bool readOnly)
		{
			var path = GetPath(date.UtcKind(), true);

			return File.Exists(path)
				? File.Open(path, FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite, FileShare.Read)
				: Stream.Null;
		}

		private IEnumerable<DateTime> LoadDates()
		{
			try
			{
				var reader = new BitArrayReader(File.ReadAllBytes(_datesPath));
				
				// version
				reader.ReadInt();
				reader.ReadInt();

				return ReadDates(reader);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.ErrorReadFile.Put(_datesPath), ex);
			}
		}

		private IEnumerable<DateTime> LoadDatesObsoleteBin()
		{
			try
			{
				return ReadDatesObsolete(File.ReadAllBytes(_datesPathObsoleteBin).To<Stream>());
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.ErrorReadFile.Put(_datesPathObsoleteBin), ex);
			}
		}

		private IEnumerable<DateTime> LoadDatesObsoleteTxt()
		{
			try
			{
				return Do.Invariant(() =>
				{
					using var reader = new StreamReader(new FileStream(_datesPathObsoleteTxt, FileMode.Open, FileAccess.Read));

					var dates = new List<DateTime>();

					while (true)
					{
						var line = reader.ReadLine();

						if (line == null)
							break;

						dates.Add(GetDate(line));
					}

					return dates;
				});
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.ErrorReadFile.Put(_datesPathObsoleteTxt), ex);
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

				using (var writer = new BitArrayWriter(stream))
				{
					writer.WriteInt(_ver10.Major);
					writer.WriteInt(_ver10.Minor);

					WriteDates(writer, dates);
				}

				stream.Position = 0;

				lock (_cacheSync)
				{
					stream.Save(_datesPath);

					File.Delete(_datesPathObsoleteBin);
					File.Delete(_datesPathObsoleteTxt);
				}
			}
			catch (UnauthorizedAccessException)
			{
				// если папка с данными с правами только на чтение
			}
		}

		private string GetDataPath(DateTime date)
		{
			return IOPath.Combine(_path, GetDirName(date));
		}

		private string GetPath(DateTime date, bool isLoad)
		{
			var result = IOPath.Combine(GetDataPath(date), _fileNameWithExtension);

			System.Diagnostics.Debug.WriteLine($"FileAccess ({(isLoad ? "Load" : "Save")}): {result}");
			return result;
		}

		private static string GetDirectoryName(string path)
			=> IOPath.GetDirectoryName(path) ?? throw new ArgumentException(path);

		public void ResetCache()
		{
			_datesDict.Reset();
		}
	}

	private static IEnumerable<DateTime> ReadDatesObsolete(Stream stream)
	{
		var dates = new List<DateTime>();

		var length = stream.Read<int>();

		DateTime lastDate = default;

		for (var i = 0; i < length; i++)
		{
			if (i == 0)
				lastDate = stream.Read<DateTime>();
			else
			{
				var shift = stream.ReadByte();

				if (shift == byte.MaxValue)
					lastDate = stream.Read<DateTime>();
				else
				{
					if (shift < 0)
						throw new InvalidOperationException("Dates non ordered: {0}/{1}.".Put(lastDate, shift));

					lastDate = lastDate.AddDays(shift);
				}
			}

			dates.Add(lastDate.UtcKind());
		}

		return dates;
	}

	private static void WriteDates(BitArrayWriter writer, DateTime[] dates)
	{
		writer.WriteInt(dates.Length);

		DateTime lastDate = default;

		for (var i = 0; i < dates.Length; i++)
		{
			var date = dates[i];

			if (i == 0)
				writer.WriteLong(date.Ticks);
			else
			{
				var shift = (int)(date - lastDate).TotalDays;

				if (shift < 0)
					throw new InvalidOperationException("Dates non ordered: {0}/{1}.".Put(lastDate, date));

				writer.WriteInt(shift);
			}

			lastDate = date;
		}
	}

	private static IEnumerable<DateTime> ReadDates(BitArrayReader reader)
	{
		var dates = new List<DateTime>();

		var length = reader.ReadInt();

		DateTime lastDate = default;

		for (var i = 0; i < length; i++)
		{
			if (i == 0)
				lastDate = reader.ReadLong().To<DateTime>();
			else
			{
				var shift = reader.ReadInt();

				if (shift < 0)
					throw new InvalidOperationException("Dates non ordered: {0}/{1}.".Put(lastDate, shift));

				lastDate = lastDate.AddDays(shift);
			}

			dates.Add(lastDate.UtcKind());
		}

		return dates;
	}

	private class Index : CachedSynchronizedDictionary<SecurityId, Dictionary<StorageFormats, Dictionary<DataType, HashSet<DateTime>>>>
	{
		private static readonly Version _ver10 = new(1, 0);
		private static readonly Version _ver11 = new(1, 1);

		private const byte _customCode = 0;
		private const byte _candlesCode = 7;

		private static readonly PairSet<DataType, byte> _map = new()
		{
			{ DataType.Ticks, 1 },
			{ DataType.MarketDepth, 2 },
			{ DataType.Level1, 3 },
			{ DataType.PositionChanges, 4 },
			{ DataType.OrderLog, 5 },
			{ DataType.Transactions, 6 },

			{ DataType.CandleTimeFrame, _candlesCode + 0 },
			{ DataType.CandleVolume, _candlesCode + 1 },
			{ DataType.CandleTick, _candlesCode + 2 },
			{ DataType.CandleRenko, _candlesCode + 3 },
			{ DataType.CandlePnF, _candlesCode + 4 },
			{ DataType.CandleRange, _candlesCode + 5 },
		};

		public void Load(byte[] data)
		{
			lock (SyncRoot)
			{
				Clear();

				var reader = new BitArrayReader(data);

				var ver = new Version(reader.ReadInt(), reader.ReadInt());

				if (ver > _ver11)
					throw new InvalidOperationException(LocalizedStrings.StorageVersionNewerKey.Put(nameof(Index), ver, _ver11));
				else if (ver == _ver10)
					throw new NotSupportedException("osolete format");

				var boardsLen = reader.ReadInt();

				var boardsDict = new Dictionary<int, string>();

				for (var i = 0; i < boardsLen; i++)
					boardsDict.Add(i, reader.ReadStringEx());

				var typesLen = reader.ReadInt();

				var typesDict = new Dictionary<int, DataType>();

				for (var i = 0; i < typesLen; i++)
				{
					var dtCode = (byte)reader.ReadInt();

					if (_map.TryGetKey(dtCode, out var dt))
					{
						if (dtCode >= _candlesCode)
						{
							var arg = reader.ReadStringEx();
							dt = DataType.Create(dt.MessageType, dt.MessageType.ToDataTypeArg(arg));
						}
					}
					else
					{
						var type = reader.ReadStringEx();
						var arg = reader.ReadStringEx();

						dt = type.ToDataType(arg);
					}

					typesDict.Add(i, dt);
				}

				var secsLen = reader.ReadInt();

				for (var i = 0; i < secsLen; i++)
				{
					var secId = new SecurityId
					{
						SecurityCode = reader.ReadStringEx(),
						BoardCode = boardsDict[reader.ReadInt()],
					};

					var formatsDict = this.SafeAdd(secId);

					var formatsLen = reader.ReadInt();

					for (var k = 0; k < formatsLen; k++)
					{
						var format = (StorageFormats)reader.ReadInt();

						var formatDict = formatsDict.SafeAdd(format);

						typesLen = reader.ReadInt();

						for (var j = 0; j < typesLen; j++)
						{
							formatDict.Add(typesDict[reader.ReadInt()], [.. ReadDates(reader)]);
						}
					}
				}
			}
		}

		public void Save(Stream stream)
		{
			lock (SyncRoot)
			{
				_lastTimeChanged = null;

				using var writer = new BitArrayWriter(stream);

				writer.WriteInt(_ver11.Major);
				writer.WriteInt(_ver11.Minor);

				var boards = AvailableSecurities.Select(id => id.BoardCode).Distinct(StringComparer.InvariantCultureIgnoreCase).ToArray();
				var boardsDict = new Dictionary<string, int>();

				writer.WriteInt(boards.Length);

				foreach (var board in boards)
				{
					writer.WriteStringEx(board);
					boardsDict.Add(board, boardsDict.Count);
				}

				var dataTypes = this.SelectMany(p => p.Value.SelectMany(p => p.Value.Keys)).Distinct().ToArray();
				var dtDict = new Dictionary<DataType, int>();

				writer.WriteInt(dataTypes.Length);

				foreach (var dt in dataTypes)
				{
					if (_map.TryGetValue(dt, out var dtCode))
						writer.WriteInt(dtCode);
					else
					{
						if (dt.IsCandles && _map.TryGetValue(DataType.Create(dt.MessageType, default), out var candleCode))
						{
							writer.WriteInt(candleCode);
							writer.WriteStringEx(dt.DataTypeArgToString());
						}
						else
						{
							writer.WriteInt(_customCode);

							var (typeStr, argStr) = dt.FormatToString();

							writer.WriteStringEx(typeStr);
							writer.WriteStringEx(argStr);
						}
					}

					dtDict.Add(dt, dtDict.Count);
				}

				writer.WriteInt(Count);

				foreach (var (secId, formatsDict) in this)
				{
					writer.WriteStringEx(secId.SecurityCode);
					writer.WriteInt(boardsDict[secId.BoardCode]);

					writer.WriteInt(formatsDict.Count);

					foreach (var (format, typesDict) in formatsDict)
					{
						writer.WriteInt((byte)format);

						writer.WriteInt(typesDict.Count);

						foreach (var (dt, dates) in typesDict)
						{
							writer.WriteInt(dtDict[dt]);
							WriteDates(writer, [.. dates.OrderBy()]);
						}
					}
				}
			}
		}

		private DateTime? _lastTimeChanged;

		public void ChangeDate(SecurityId secId, StorageFormats format, DataType dataType, DateTime date, bool remove)
		{
			lock (SyncRoot)
			{
				if (remove)
				{
					if (TryGetValue(secId, out var formatsDict) &&
						formatsDict.TryGetValue(format, out var typesDict) &&
						typesDict.TryGetValue(dataType, out var dates) &&
						dates.Remove(date))
					{
						if (dates.Count == 0)
							typesDict.Remove(dataType);

						_lastTimeChanged ??= DateTime.UtcNow;
					}
				}
				else
				{
					var dates = this
						.SafeAdd(secId, out var isNew1)
						.SafeAdd(format, out var isNew2)
						.SafeAdd(dataType, out var isNew3);

					var added = dates.Add(date);

					if (isNew1 || isNew2 || isNew3 || added)
						_lastTimeChanged ??= DateTime.UtcNow;
				}
			}
		}

		public bool NeedSave(TimeSpan diff)
		{
			lock (SyncRoot)
				return _lastTimeChanged is not null && (DateTime.UtcNow - _lastTimeChanged.Value) >= diff;
		}

		public IEnumerable<SecurityId> AvailableSecurities => CachedKeys;

		public IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		{
			lock (SyncRoot)
			{
				if (securityId == default)
					return [.. this.SelectMany(p => p.Value.TryGetValue(format, out var formatsDict) ? formatsDict.Keys : Enumerable.Empty<DataType>()).Distinct()];

				if (TryGetValue(securityId, out var formatsDict) && formatsDict.TryGetValue(format, out var typesDict))
					return [.. typesDict.Keys];
			}

			return [];
		}

		public IEnumerable<DateTime> GetDates(SecurityId securityId, DataType dataType, StorageFormats format)
		{
			lock (SyncRoot)
			{
				if (TryGetValue(securityId, out var dict) &&
					dict.TryGetValue(format, out var dict2) &&
					dict2.TryGetValue(dataType, out var dates))
					return [.. dates.OrderBy()];
			}

			return [];
		}
	}

	private readonly SynchronizedDictionary<(SecurityId, DataType, StorageFormats), LocalMarketDataStorageDrive> _drives = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="LocalMarketDataDrive"/>.
	/// </summary>
	public LocalMarketDataDrive()
		: this(Directory.GetCurrentDirectory())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LocalMarketDataDrive"/>.
	/// </summary>
	/// <param name="path">The path to the directory with data.</param>
	public LocalMarketDataDrive(string path)
	{
		_path = path.ToFullPathIfNeed();
	}

	private string _path;

	/// <inheritdoc />
	public override string Path
	{
		get => _path;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			if (Path == value)
				return;

			_path = value;
			ResetDrives();
		}
	}

	private void ResetDrives()
	{
		lock (_drives.SyncRoot)
			_drives.Values.ForEach(d => d.ResetCache());

		lock (_indexLock)
			_index = null;
	}

	private readonly SyncObject _indexLock = new();
	private Index _index;

	private string IndexFullPath => IOPath.Combine(Path, "index.bin");

	private bool TryGetIndex(out Index index)
	{
		index = _index;

		if (index is not null)
			return true;

		lock (_indexLock)
		{
			index = _index;

			if (index is not null)
				return true;

			if (!File.Exists(IndexFullPath))
				return false;

			try
			{
				index = new();
				index.Load(File.ReadAllBytes(IndexFullPath));

				_index = index;
			}
			catch
			{
				index = null;
				File.Delete(IndexFullPath);
			}

			return index is not null;
		}
	}

	private IEnumerable<SecurityId> ScanAvailableSecurities()
	{
		var idGenerator = new SecurityIdGenerator();

		var path = Path;

		if (!Directory.Exists(path))
			return [];

		return Directory
			.EnumerateDirectories(path)
			.SelectMany(Directory.EnumerateDirectories)
			.Select(IOPath.GetFileName)
			.Select(StorageHelper.FolderNameToSecurityId)
			.Select(n => idGenerator.Split(n, true))
			.Where(t => t != default);
	}

	/// <inheritdoc />
	public override IEnumerable<SecurityId> AvailableSecurities
	{
		get
		{
			if (TryGetIndex(out var index))
				return index.AvailableSecurities;

			return ScanAvailableSecurities();
		}
	}

	/// <summary>
	/// Get all available instruments.
	/// </summary>
	/// <param name="path">The path to the directory with data.</param>
	/// <returns>All available instruments.</returns>
	[Obsolete("Use AvailableSecurities property.")]
	public static IEnumerable<SecurityId> GetAvailableSecurities(string path)
	{
		using var drive = new LocalMarketDataDrive(path);
		return [.. drive.AvailableSecurities];
	}

	private static readonly SynchronizedDictionary<string, RefPair<HashSet<DataType>, bool>> _availableDataTypes = new(StringComparer.InvariantCultureIgnoreCase);

	/// <inheritdoc />
	public override IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
	{
		if (TryGetIndex(out var index))
			return index.GetAvailableDataTypes(securityId, format);

		var ext = GetExtension(format);

		IEnumerable<DataType> GetDataTypes(string secPath)
		{
			return IOHelper
					.GetDirectories(secPath)
					.SelectMany(dateDir => Directory.GetFiles(dateDir, "*" + ext))
					.Select(IOPath.GetFileNameWithoutExtension)
					.Distinct()
					.Select(GetDataType)
					.WhereNotNull()
					.OrderBy(d =>
					{
						if (!d.IsCandles)
							return 0;

						return d.IsTFCandles
							? d.GetTimeFrame().Ticks : long.MaxValue;
					});
		}

		if (securityId == default)
		{
			lock (_availableDataTypes.SyncRoot)
			{
				var tuple = _availableDataTypes.SafeAdd(Path, key => RefTuple.Create(new HashSet<DataType>(), false));

				if (!tuple.Second)
				{
					if (Directory.Exists(Path))
					{
						tuple.First.AddRange(Directory
							.EnumerateDirectories(Path)
							.SelectMany(Directory.EnumerateDirectories)
							.SelectMany(GetDataTypes));
					}

					tuple.Second = true;
				}

				return [.. tuple.First];
			}
		}

		var s = GetSecurityPath(securityId);

		return Directory.Exists(s) ? GetDataTypes(s) : [];
	}

	/// <inheritdoc />
	public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		if (dataType.IsSecurityRequired && securityId == default)
			throw new ArgumentNullException(nameof(securityId));

		return _drives.SafeAdd((securityId, dataType, format),
			key => new LocalMarketDataStorageDrive(dataType, securityId, format, this));
	}

	/// <inheritdoc />
	public override void Verify()
	{
		if (!Directory.Exists(Path))
			throw new InvalidOperationException(LocalizedStrings.DirectoryNotExist.Put(Path));
	}

	/// <inheritdoc />
	public override void LookupSecurities(SecurityLookupMessage criteria, ISecurityProvider securityProvider, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		if (securityProvider == null)
			throw new ArgumentNullException(nameof(securityProvider));

		if (newSecurity == null)
			throw new ArgumentNullException(nameof(newSecurity));

		if (isCancelled == null)
			throw new ArgumentNullException(nameof(isCancelled));

		if (updateProgress == null)
			throw new ArgumentNullException(nameof(updateProgress));

		var securityPaths = new List<string>();
		var progress = 0;

		foreach (var letterDir in IOHelper.GetDirectories(Path))
		{
			if (isCancelled())
				break;

			var name = IOPath.GetFileName(letterDir);

			if (name == null || name.Length != 1)
				continue;

			securityPaths.AddRange(IOHelper.GetDirectories(letterDir));
		}

		if (isCancelled())
			return;

		var iterCount = securityPaths.Count;

		updateProgress(0, iterCount);

		var existingIds = securityProvider.LookupAll().Select(s => s.Id).ToIgnoreCaseSet();

		foreach (var securityPath in securityPaths)
		{
			if (isCancelled())
				break;

			var securityId = IOPath.GetFileName(securityPath).FolderNameToSecurityId();

			if (!existingIds.Contains(securityId))
			{
				var firstDataFile =
					Directory.EnumerateDirectories(securityPath)
						.SelectMany(d => Directory.EnumerateFiles(d, "*.bin")
							.Concat(Directory.EnumerateFiles(d, "*.csv"))
							.OrderBy(f => IOPath.GetExtension(f).EqualsIgnoreCase(".bin") ? 0 : 1))
						.FirstOrDefault();

				if (firstDataFile != null)
				{
					var id = securityId.ToSecurityId();

					decimal priceStep;

					if (IOPath.GetExtension(firstDataFile).EqualsIgnoreCase(".bin"))
					{
						try
						{
							priceStep = File.ReadAllBytes(firstDataFile).AsSpan().Slice(6, 16).ToArray().To<decimal>();
						}
						catch (Exception ex)
						{
							throw new InvalidOperationException(LocalizedStrings.FileWrongFormat.Put(firstDataFile), ex);
						}
					}
					else
						priceStep = 0.01m;

					var security = new SecurityMessage
					{
						SecurityId = securityId.ToSecurityId(),
						PriceStep = priceStep,
						Name = id.SecurityCode,
					};

					if (security.IsMatch(criteria))
						newSecurity(security);

					existingIds.Add(securityId);
				}
			}

			updateProgress(progress++, iterCount);
		}
	}

	/// <summary>
	/// To get the file extension for the format.
	/// </summary>
	/// <param name="format">Format.</param>
	/// <returns>The extension.</returns>
	public static string GetExtension(StorageFormats format)
		=> format switch
		{
			StorageFormats.Binary => ".bin",
			StorageFormats.Csv => ".csv",
			_ => throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.InvalidValue),
		};

	/// <summary>
	/// Get data type and parameter for the specified file name.
	/// </summary>
	/// <param name="fileName">The file name.</param>
	/// <returns>Data type and parameter associated with the type. For example, candle arg.</returns>
	public static DataType GetDataType(string fileName)
	{
		try
		{
			return fileName.FileNameToDataType();
		}
		catch (Exception ex)
		{
			ex.LogError();
			return null;
		}
	}

	/// <summary>
	/// To get the file name by the type of data.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format. If set an extension will be added to the file name.</param>
	/// <param name="throwIfUnknown">Throw exception if the specified type is unknown.</param>
	/// <returns>The file name.</returns>
	public static string GetFileName(DataType dataType, StorageFormats? format = null, bool throwIfUnknown = true)
	{
		var fileName = dataType.DataTypeToFileName();

		if (fileName == null)
		{
			if (throwIfUnknown)
				throw new NotSupportedException(LocalizedStrings.UnsupportedType.Put(dataType.ToString()));

			return null;
		}

		if (format != null)
			fileName += GetExtension(format.Value);

		return fileName;
	}

	private const string _dateFormat = "yyyy_MM_dd";

	/// <summary>
	/// Convert directory name to the date.
	/// </summary>
	/// <param name="dirName">Directory name.</param>
	/// <returns>The date.</returns>
	public static DateTime GetDate(string dirName)
		=> dirName.ToDateTime(_dateFormat).UtcKind();

	/// <summary>
	/// Convert the date to directory name.
	/// </summary>
	/// <param name="date">The date.</param>
	/// <returns>Directory name.</returns>
	public static string GetDirName(DateTime date)
		=> date.ToString(_dateFormat);

	/// <summary>
	/// To get the path to the folder with market data for the instrument.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns>The path to the folder with market data.</returns>
	public string GetSecurityPath(SecurityId securityId)
	{
		var id = securityId == default ? EntitiesExtensions.AllSecurity.Id : securityId.ToStringId();

		var folderName = id.SecurityIdToFolderName();

		return IOPath.Combine(Path, folderName[..1], folderName);
	}

	/// <summary>
	/// Build an index for fast performance of accessing available data types from the storage.
	/// </summary>
	/// <param name="logs">Logs. Can be <see langword="null"/>.</param>
	/// <param name="updateProgress">Progress handler.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public Task BuildIndexAsync(ILogReceiver logs, Action<int, int> updateProgress, CancellationToken cancellationToken)
	{
		if (updateProgress is null)
			throw new ArgumentNullException(nameof(updateProgress));

		var index = new Index();
		var idGenerator = new SecurityIdGenerator();
		var formats = Enumerator.GetValues<StorageFormats>().ToArray();
		var path = Path;

		if (Directory.Exists(path))
		{
			var secPaths = Directory
				.EnumerateDirectories(path)
				.SelectMany(Directory.EnumerateDirectories)
				.ToArray();

			for (var i = 0; i < secPaths.Length; i++)
			{
				cancellationToken.ThrowIfCancellationRequested();

				var secPath = secPaths[i];

				var secId = idGenerator.Split(IOPath
					.GetFileName(secPath)
					.FolderNameToSecurityId(), true);

				if (secId == default)
					continue;

				var formatDict = index.SafeAdd(secId, _ => []);

				foreach (var format in formats)
				{
					cancellationToken.ThrowIfCancellationRequested();

					var ext = GetExtension(format);
					
					var dates = IOHelper
						.GetDirectories(secPath)
						.ToDictionary(
							dateDir => GetDate(IOPath.GetFileName(dateDir)),
							dateDir =>
							{
								cancellationToken.ThrowIfCancellationRequested();

								return Directory
									.GetFiles(dateDir, "*" + ext)
									.Select(IOPath.GetFileNameWithoutExtension)
									.Select(GetDataType)
									.WhereNotNull()
									.ToHashSet()
								;
							}
						);

					var typesDict = formatDict.SafeAdd(format, _ => []);

					foreach (var dt in dates.Values.SelectMany().Distinct())
					{
						cancellationToken.ThrowIfCancellationRequested();

						typesDict.Add(dt, [.. dates
							.Where(p => p.Value.Contains(dt))
							.Select(p => p.Key)
							.OrderBy()]
						);
					}
				}

				updateProgress(i, secPaths.Length);
				logs?.LogInfo("Sec {0} processed.", secId);
			}
		}

		lock (_indexLock)
			_index = index;

		SaveIndex(index);

		logs?.LogInfo("Index saved.");

		return Task.CompletedTask;
	}

	/// <summary>
	/// Try save existing index.
	/// </summary>
	/// <param name="diff">Time diff from prev index change.</param>
	/// <returns>Time taken by build process. <see langword="null"/> means no build happened.</returns>
	public TimeSpan? TrySaveIndex(TimeSpan diff)
	{
		Index index;

		lock (_indexLock)
			index = _index;

		if (index?.NeedSave(diff) != true)
			return null;

		return Watch.Do(() => SaveIndex(index));
	}

	private void SaveIndex(Index index)
	{
		if (index is null)
			throw new ArgumentNullException(nameof(index));

		var stream = new MemoryStream();
		index.Save(stream);

		stream.Position = 0;

		lock (_indexLock)
			stream.Save(IndexFullPath);
	}
}