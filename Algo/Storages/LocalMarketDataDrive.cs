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
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using IOPath = System.IO.Path;

	using MoreLinq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// The file storage for market data.
	/// </summary>
	public class LocalMarketDataDrive : BaseMarketDataDrive
	{
		private sealed class LocalMarketDataStorageDrive : IMarketDataStorageDrive
		{
			private readonly string _path;
			private readonly string _fileNameWithExtension;
			private readonly string _datesPath;
			private readonly DataType _dataType;

			private readonly SyncObject _cacheSync = new SyncObject();

			//private static readonly Version _dateVersion = new Version(1, 0);

			public LocalMarketDataStorageDrive(DataType dataType, string path, StorageFormats format, LocalMarketDataDrive drive)
			{
				_dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));

				var fileName = GetFileName(_dataType);

				if (path.IsEmpty())
					throw new ArgumentNullException(nameof(path));

				_path = path;
				_drive = drive ?? throw new ArgumentNullException(nameof(drive));
				_fileNameWithExtension = fileName + GetExtension(format);
				_datesPath = IOPath.Combine(_path, fileName + format + "Dates.txt");

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
						File.Delete(_datesPath);
				}

				ResetCache();
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

				var dates = DatesDict;

				dates.Remove(date);

				SaveDates(Dates.ToArray());

				_availableDataTypes.Remove(_drive.Path);
			}

			void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
			{
				date = date.UtcKind();

				Directory.CreateDirectory(GetDataPath(date));

				using (var file = File.OpenWrite(GetPath(date, false)))
					stream.CopyTo(file);

				var dates = DatesDict;

				dates[date] = date;

				SaveDates(Dates.ToArray());

				lock (_availableDataTypes.SyncRoot)
				{
					var tuple = _availableDataTypes.TryGetValue(_drive.Path);

					if (tuple == null || !tuple.Second)
						return;

					tuple.First.Add(_dataType);
				}
			}

			Stream IMarketDataStorageDrive.LoadStream(DateTime date)
			{
				var path = GetPath(date.UtcKind(), true);

				return File.Exists(path)
					? File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
					: Stream.Null;
			}

			private IEnumerable<DateTime> LoadDates()
			{
				try
				{
					return CultureInfo.InvariantCulture.DoInCulture(() =>
					{
						using (var reader = new StreamReader(new FileStream(_datesPath, FileMode.Open, FileAccess.Read)))
						{
							//var version = new Version(file.ReadByte(), file.ReadByte());

							//if (version > _dateVersion)
							//	throw new InvalidOperationException(LocalizedStrings.Str1002Params.Put(_datesPath, version, _dateVersion));

							//var count = file.Read<int>();

							var dates = new List<DateTime>();

							while (true)
							{
								var line = reader.ReadLine();

								if (line == null)
									break;

								dates.Add(GetDate(line));
							}

							//for (var i = 0; i < count; i++)
							//{
							//	dates[i] = file.Read<DateTime>().UtcKind();
							//}

							return dates;
						}
					});
				}
				catch (Exception ex)
				{
					throw new InvalidOperationException(LocalizedStrings.Str1003Params.Put(_datesPath), ex);
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

					//stream.WriteByte((byte)_dateVersion.Major);
					//stream.WriteByte((byte)_dateVersion.Minor);
					//stream.Write(dates.Length);

					CultureInfo.InvariantCulture.DoInCulture(() =>
					{
						var writer = new StreamWriter(stream) { AutoFlush = true };

						foreach (var date in dates)
						{
							writer.WriteLine(GetDirName(date));
							//stream.Write(date);
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
			{
				var name = IOPath.GetDirectoryName(path);

				if (name == null)
					throw new ArgumentException(LocalizedStrings.Str1004Params.Put(path));

				return name;
			}

			public void ResetCache()
			{
				_datesDict.Reset();
			}
		}

		private readonly SynchronizedDictionary<Tuple<SecurityId, DataType, StorageFormats>, LocalMarketDataStorageDrive> _drives = new SynchronizedDictionary<Tuple<SecurityId, DataType, StorageFormats>, LocalMarketDataStorageDrive>();

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
		}

		/// <inheritdoc />
		public override IEnumerable<SecurityId> AvailableSecurities => GetAvailableSecurities(Path);

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		/// <param name="path">The path to the directory with data.</param>
		/// <returns>All available instruments.</returns>
		public static IEnumerable<SecurityId> GetAvailableSecurities(string path)
		{
			var idGenerator = new SecurityIdGenerator();

			if (!Directory.Exists(path))
				return Enumerable.Empty<SecurityId>();

			return Directory
				.EnumerateDirectories(path)
				.SelectMany(Directory.EnumerateDirectories)
				.Select(IOPath.GetFileName)
				.Select(StorageHelper.FolderNameToSecurityId)
				.Select(n => idGenerator.Split(n, true))
				.Where(t => !t.IsDefault());
		}

		private static readonly SynchronizedDictionary<string, RefPair<HashSet<DataType>, bool>> _availableDataTypes = new SynchronizedDictionary<string, RefPair<HashSet<DataType>, bool>>(StringComparer.InvariantCultureIgnoreCase);

		/// <inheritdoc />
		public override IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		{
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

					       return d.MessageType == typeof(TimeFrameCandleMessage)
						       ? ((TimeSpan)d.Arg).Ticks : long.MaxValue;
				       });
			}

			if (securityId.IsDefault())
			{
				lock (_availableDataTypes.SyncRoot)
				{
					var tuple = _availableDataTypes.SafeAdd(Path, key => RefTuple.Create(new HashSet<DataType>(), false));
				
					if (!tuple.Second)
					{
						tuple.First.AddRange(Directory
		                     .EnumerateDirectories(Path)
		                     .SelectMany(Directory.EnumerateDirectories)
		                     .SelectMany(GetDataTypes));

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

			return _drives.SafeAdd(Tuple.Create(securityId, dataType, format),
				key => new LocalMarketDataStorageDrive(dataType, GetSecurityPath(securityId), format, this));
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

			var existingIds = securityProvider.LookupAll().Select(s => s.Id).ToHashSet(StringComparer.InvariantCultureIgnoreCase);

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
								.OrderBy(f => IOPath.GetExtension(f).CompareIgnoreCase(".bin") ? 0 : 1))
							.FirstOrDefault();

					if (firstDataFile != null)
					{
						var id = securityId.ToSecurityId();

						decimal priceStep;

						if (IOPath.GetExtension(firstDataFile).CompareIgnoreCase(".bin"))
						{
							try
							{
								priceStep = File.ReadAllBytes(firstDataFile).Range(6, 16).To<decimal>();
							}
							catch (Exception ex)
							{
								throw new InvalidOperationException(LocalizedStrings.Str2929Params.Put(firstDataFile), ex);
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
		{
			switch (format)
			{
				case StorageFormats.Binary:
					return ".bin";
				case StorageFormats.Csv:
					return ".csv";
				default:
					throw new ArgumentOutOfRangeException(nameof(format), format, LocalizedStrings.Str1219);
			}
		}

		/// <summary>
		/// Get data type and parameter for the specified file name.
		/// </summary>
		/// <param name="fileName">The file name.</param>
		/// <returns>Data type and parameter associated with the type. For example, <see cref="CandleMessage.Arg"/>.</returns>
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
					throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(dataType.ToString()));

				return null;
			}

			if (format != null)
				fileName += GetExtension(format.Value);

			return fileName;
		}

		/// <summary>
		/// To get the file name by the type of data.
		/// </summary>
		/// <param name="dataType">Data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">Storage format. If set an extension will be added to the file name.</param>
		/// <returns>The file name.</returns>
		[Obsolete]
		public static string GetFileName(Type dataType, object arg, StorageFormats? format = null)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			return GetFileName(DataType.Create(dataType, arg), format);
		}

		private const string _dateFormat = "yyyy_MM_dd";

		/// <summary>
		/// Convert directory name to the date.
		/// </summary>
		/// <param name="dirName">Directory name.</param>
		/// <returns>The date.</returns>
		public static DateTime GetDate(string dirName)
		{
			return dirName.ToDateTime(_dateFormat).UtcKind();
		}

		/// <summary>
		/// Convert the date to directory name.
		/// </summary>
		/// <param name="date">The date.</param>
		/// <returns>Directory name.</returns>
		public static string GetDirName(DateTime date)
		{
			return date.ToString(_dateFormat);
		}

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
	}
}