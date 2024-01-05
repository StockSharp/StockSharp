#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: LocalMarketDataDrive.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using IOPath = System.IO.Path;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Reflection;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// The file storage for market data.
	/// </summary>
	public class LocalMarketDataDrive : BaseMarketDataDrive
	{
		private class LocalMarketDataStorageDrive : IMarketDataStorageDrive
		{
			private readonly string _path;
			private readonly string _fileNameWithExtension;
			private readonly string _datesPath;
			private readonly string _datesPathObsolete;
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
				_datesPath = $"{datesPath}.bin";
				_datesPathObsolete = $"{datesPath}.txt";

				_datesDict = new Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>>(() =>
				{
					var retVal = new CachedSynchronizedOrderedDictionary<DateTime, DateTime>();

					if (File.Exists(_datesPath))
					{
						foreach (var date in LoadDates())
							retVal.Add(date, date);
					}
					else if (File.Exists(_datesPathObsolete))
					{
						foreach (var date in LoadDatesObsolete())
							retVal.Add(date, date);
					}
					else
					{
						var dates = IOHelper
							.GetDirectories(_path)
							.Where(dir => File.Exists(IOPath.Combine(dir, _fileNameWithExtension)))
							.Select(dir => GetDate(IOPath.GetFileName(dir)));

						foreach (var date in dates)
							retVal.Add(date, date);

						SaveDates(retVal.CachedValues);
					}

					return retVal;
				}).Track();
			}

			private readonly LocalMarketDataDrive _drive;
			IMarketDataDrive IMarketDataStorageDrive.Drive => _drive;

			public IEnumerable<DateTime> Dates => DatesDict.CachedValues;

			private readonly Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>> _datesDict;

			private CachedSynchronizedOrderedDictionary<DateTime, DateTime> DatesDict => _datesDict.Value;

			public void ClearDatesCache()
			{
				if (Directory.Exists(_path))
				{
					lock (_cacheSync)
					{
						File.Delete(_datesPath);
						File.Delete(_datesPathObsolete);
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

				using (var file = File.OpenWrite(GetPath(date, false)))
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
					return ReadDates(File.ReadAllBytes(_datesPath).To<Stream>());
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(LocalizedStrings.ErrorReadFile.Put(_datesPath), ex);
				}
			}

			private IEnumerable<DateTime> LoadDatesObsolete()
			{
				try
				{
					return Do.Invariant(() =>
					{
						using var reader = new StreamReader(new FileStream(_datesPathObsolete, FileMode.Open, FileAccess.Read));

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
					throw new InvalidOperationException(LocalizedStrings.ErrorReadFile.Put(_datesPathObsolete), ex);
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

					WriteDates(stream, dates);
					stream.Position = 0;

					lock (_cacheSync)
					{
						stream.Save(_datesPath);

						File.Delete(_datesPathObsolete);
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

				Debug.WriteLine("FileAccess ({0}): {1}".Put(isLoad ? "Load" : "Save", result));
				return result;
			}

			private static string GetDirectoryName(string path)
				=> IOPath.GetDirectoryName(path) ?? throw new ArgumentException(path);

			public void ResetCache()
			{
				_datesDict.Reset();
			}
		}

		private static void WriteDates(Stream stream, DateTime[] dates)
		{
			stream.WriteEx(dates.Length);

			foreach (var date in dates)
				stream.WriteEx(date.Ticks);
		}

		private static IEnumerable<DateTime> ReadDates(Stream stream)
		{
			var dates = new List<DateTime>();

			var length = stream.Read<int>();

			for (var i = 0; i < length; i++)
				dates.Add(stream.Read<DateTime>().UtcKind());

			return dates;
		}

		private class Index : CachedSynchronizedDictionary<SecurityId, Dictionary<StorageFormats, Dictionary<DataType, HashSet<DateTime>>>>//, IPersistable
		{
			private static class SKeys
			{
				public const string SecId = nameof(SecId);
				public const string Formats = nameof(Formats);
				public const string Format = nameof(Format);
				public const string Types = nameof(Types);
				public const string Type = nameof(Type);
				public const string Arg = nameof(Arg);
				public const string Dates = nameof(Dates);
			}

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

			public void Load(Stream stream)
			{
				lock (SyncRoot)
				{
					Clear();

					var secsLen = stream.Read<int>();

					for (var i = 0; i < secsLen; i++)
					{
						var secId = new SecurityId
						{
							SecurityCode = stream.Read<string>(),
							BoardCode = stream.Read<string>(),
						};

						var formatsDict = this.SafeAdd(secId);

						var formatsLen = stream.Read<int>();

						for (var k = 0; k < formatsLen; k++)
						{
							var format = (StorageFormats)stream.ReadByte();

							var formatDict = formatsDict.SafeAdd(format);

							var typesLen = stream.Read<int>();

							for (var j = 0; j < typesLen; j++)
							{
								var dtCode = (byte)stream.ReadByte();

								if (_map.TryGetKey(dtCode, out var dt))
								{
									if (dtCode >= _candlesCode)
									{
										var arg = stream.Read<string>();
										dt = DataType.Create(dt.MessageType, dt.MessageType.ToDataTypeArg(arg));
									}
								}
								else
								{
									var type = stream.Read<string>();
									var arg = stream.Read<string>();

									dt = type.ToDataType(arg);
								}

								formatDict.Add(dt, ReadDates(stream).ToHashSet());
							}
						}
					}
				}

				//foreach (var secStorage in storage.GetValue<IEnumerable<SettingsStorage>>(nameof(Index)))
				//{
				//	var formatsDict = this.SafeAdd(secStorage.GetValue<string>(SKeys.SecId).ToSecurityId(), key => new());

				//	foreach (var formatStorage in secStorage.GetValue<IEnumerable<SettingsStorage>>(SKeys.Formats))
				//	{
				//		var typesDict = formatsDict.SafeAdd(formatStorage.GetValue<StorageFormats>(SKeys.Format), key => new());

				//		foreach (var typeStorage in formatStorage.GetValue<IEnumerable<SettingsStorage>>(SKeys.Types))
				//		{
				//			var type = typeStorage.GetValue<string>(SKeys.Type);
				//			var arg = typeStorage.GetValue<string>(SKeys.Arg);
				//			typesDict.Add(type.ToDataType(arg), typeStorage.GetValue<DateTime[]>(SKeys.Dates));
				//		}
				//	}
				//}
			}

			public void Save(Stream stream)
			{
				lock (SyncRoot)
				{
					_lastTimeChanged = null;

					stream.WriteEx(Count);

					foreach (var (secId, formatsDict) in this)
					{
						stream.WriteEx(secId.SecurityCode);
						stream.WriteEx(secId.BoardCode);

						stream.WriteEx(formatsDict.Count);

						foreach (var (format, typesDict) in formatsDict)
						{
							stream.WriteByte((byte)format);

							stream.WriteEx(typesDict.Count);

							foreach (var (dt, dates) in typesDict)
							{
								if (_map.TryGetValue(dt, out var dtCode))
									stream.WriteByte(dtCode);
								else
								{
									if (dt.IsCandles && _map.TryGetValue(DataType.Create(dt.MessageType, default), out var candleCode))
									{
										stream.WriteByte(candleCode);
										stream.WriteEx(dt.DataTypeArgToString());
									}
									else
									{
										stream.WriteByte(_customCode);

										var (typeStr, argStr) = dt.FormatToString();

										stream.WriteEx(typeStr);
										stream.WriteEx(argStr);
									}
								}

								WriteDates(stream, dates.OrderBy().ToArray());
							}
						}
					}
				}

				//storage.Set(nameof(Index), this.Select(t => new SettingsStorage()
				//	.Set(SKeys.SecId, t.Key.ToStringId())
				//	.Set(SKeys.Formats, t.Value.Select(t => new SettingsStorage()
				//		.Set(SKeys.Format, t.Key)
				//		.Set(SKeys.Types, t.Value.Select(t =>
				//		{
				//			var (dt, arg) = t.Key.FormatToString();

				//			return new SettingsStorage()
				//				.Set(SKeys.Type, dt)
				//				.Set(SKeys.Arg, arg)
				//				.Set(SKeys.Dates, t.Value)
				//			;
				//		})))
				//)));
			}

			private DateTime? _lastTimeChanged;

			public void ChangeDate(SecurityId secId, StorageFormats format, DataType dataType, DateTime date, bool remove)
			{
				lock (SyncRoot)
				{
					if (TryGetValue(secId, out var formatsDict) &&
						formatsDict.TryGetValue(format, out var typesDict) &&
						typesDict.TryGetValue(dataType, out var prevDates) &&
						remove == prevDates.Contains(date))
					{
						if (remove)
							prevDates.Remove(date);
						else
							prevDates.Add(date);

						_lastTimeChanged = DateTime.UtcNow;
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
						return this.SelectMany(p => p.Value.SelectMany(p => p.Value.Keys)).Distinct().ToArray();

					if (TryGetValue(securityId, out var formatsDict) && formatsDict.TryGetValue(format, out var typesDict))
						return typesDict.Keys;
				}

				return Enumerable.Empty<DataType>();
			}
		}

		private readonly SynchronizedDictionary<(SecurityId, DataType, StorageFormats), LocalMarketDataStorageDrive> _drives = new();

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

		private string IndexFullPath => IOPath.Combine(_path, "index.bin");

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
					index.Load(File.ReadAllBytes(IndexFullPath).To<Stream>());

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

		/// <inheritdoc />
		public override IEnumerable<SecurityId> AvailableSecurities
		{
			get
			{
				if (TryGetIndex(out var index))
					return index.AvailableSecurities;

				var idGenerator = new SecurityIdGenerator();

				if (!Directory.Exists(_path))
					return Enumerable.Empty<SecurityId>();

				return Directory
					.EnumerateDirectories(_path)
					.SelectMany(Directory.EnumerateDirectories)
					.Select(IOPath.GetFileName)
					.Select(StorageHelper.FolderNameToSecurityId)
					.Select(n => idGenerator.Split(n, true))
					.Where(t => t != default);
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
			return drive.AvailableSecurities.ToArray();
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
						.Where(t => t != null)
						.OrderBy(d =>
						{
							if (!d.IsCandles)
								return 0;

							return d.IsTFCandles
								? ((TimeSpan)d.Arg).Ticks : long.MaxValue;
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

					return tuple.First.ToArray();
				}
			}

			var s = GetSecurityPath(securityId);

			return Directory.Exists(s) ? GetDataTypes(s) : Enumerable.Empty<DataType>();
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

			//if (securityProvider == null)
			//	throw new ArgumentNullException(nameof(securityProvider));

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

			var existingIds = securityProvider?.LookupAll().Select(s => s.Id).ToIgnoreCaseSet() ?? new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

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
								priceStep = File.ReadAllBytes(firstDataFile).Range(6, 16).To<decimal>();
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
			var id = securityId == default ? TraderHelper.AllSecurity.Id : securityId.ToStringId();

			var folderName = id.SecurityIdToFolderName();

			return IOPath.Combine(Path, folderName.Substring(0, 1), folderName);
		}

		/// <summary>
		/// Build an index for fast performance of accessing available data types from the storage.
		/// </summary>
		public void BuildIndex()
		{
			var securities = AvailableSecurities;
			var formats = Enumerator.GetValues<StorageFormats>().ToArray();

			var index = new Index();

			foreach (var secId in securities)
			{
				var formatDict = index.SafeAdd(secId, key => new());

				foreach (var format in formats)
				{
					var typesDict = formatDict.SafeAdd(format, key => new());

					foreach (var dt in GetAvailableDataTypes(secId, format))
						typesDict.Add(dt, GetStorageDrive(secId, dt, format).Dates.ToHashSet());
				}
			}

			lock (_indexLock)
				_index = index;

			SaveIndex(index);
		}

		/// <summary>
		/// Try save existing index.
		/// </summary>
		/// <param name="diff">Time diff from prev index change.</param>
		public void TrySaveIndex(TimeSpan diff)
		{
			Index index;

			lock (_indexLock)
				index = _index;

			if (index?.NeedSave(diff) != true)
				return;

			SaveIndex(index);
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
}