namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using IOPath = System.IO.Path;

	using MoreLinq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Файловое хранилище маркет-данных.
	/// </summary>
	public class LocalMarketDataDrive : BaseMarketDataDrive
	{
		private sealed class LocalMarketDataStorageDrive : IMarketDataStorageDrive
		{
			private readonly string _path;
			private readonly string _fileNameWithExtension;
			private readonly string _datesPath;

			private readonly SyncObject _cacheSync = new SyncObject();

			//private static readonly Version _dateVersion = new Version(1, 0);
			private const string _dateFormat = "yyyy_MM_dd";

			public LocalMarketDataStorageDrive(string fileName, string path, StorageFormats format, IMarketDataDrive drive)
			{
				if (drive == null)
					throw new ArgumentNullException("drive");

				if (fileName.IsEmpty())
					throw new ArgumentNullException("fileName");

				if (path.IsEmpty())
					throw new ArgumentNullException("path");

				_path = path;
				_drive = drive;
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
						var dates = InteropHelper
							.GetDirectories(_path)
							.Where(dir => File.Exists(IOPath.Combine(dir, _fileNameWithExtension)))
							.Select(dir => IOPath.GetFileName(dir).ToDateTime(_dateFormat).ChangeKind(DateTimeKind.Utc));

						foreach (var date in dates)
							retVal.Add(date, date);

						SaveDates(retVal.CachedValues);
					}

					return retVal;
				}).Track();
			}

			private readonly IMarketDataDrive _drive;

			IMarketDataDrive IMarketDataStorageDrive.Drive
			{
				get { return _drive; }
			}

			public IEnumerable<DateTime> Dates
			{
				get { return DatesDict.CachedValues; }
			}

			private readonly Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>> _datesDict;

			private CachedSynchronizedOrderedDictionary<DateTime, DateTime> DatesDict
			{
				get { return _datesDict.Value; }
			}

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
				date = date.ChangeKind(DateTimeKind.Utc);

				var path = GetPath(date, true);

				if (File.Exists(path))
				{
					File.Delete(path);

					var dir = GetDirectoryName(path);

					if (Directory.EnumerateFiles(dir).IsEmpty())
					{
						lock (_cacheSync)
							InteropHelper.BlockDeleteDir(dir);
					}
				}

				var dates = DatesDict;

				dates.Remove(date);

				SaveDates(Dates.ToArray());
			}

			void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
			{
				date = date.ChangeKind(DateTimeKind.Utc);

				Directory.CreateDirectory(GetDataPath(date));

				using (var file = File.OpenWrite(GetPath(date, false)))
					stream.CopyTo(file);

				var dates = DatesDict;

				dates[date] = date;

				SaveDates(Dates.ToArray());
			}

			Stream IMarketDataStorageDrive.LoadStream(DateTime date)
			{
				var path = GetPath(date.ChangeKind(DateTimeKind.Utc), true);

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

								dates.Add(line.ToDateTime(_dateFormat));
							}

							//for (var i = 0; i < count; i++)
							//{
							//	dates[i] = file.Read<DateTime>().ChangeKind(DateTimeKind.Utc);
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
							writer.WriteLine(date.ToString(_dateFormat));
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
				return IOPath.Combine(_path, date.ToString(_dateFormat));
			}

			private string GetPath(DateTime date, bool isLoad)
			{
				var result = IOPath.Combine(GetDataPath(date), _fileNameWithExtension);

				System.Diagnostics.Trace.WriteLine("FileAccess ({0}): {1}".Put(isLoad ? "Load" : "Save", result));
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

		private readonly SynchronizedDictionary<Tuple<SecurityId, Type, object, StorageFormats>, LocalMarketDataStorageDrive> _drives = new SynchronizedDictionary<Tuple<SecurityId, Type, object, StorageFormats>, LocalMarketDataStorageDrive>();

		/// <summary>
		/// Создать <see cref="LocalMarketDataDrive"/>.
		/// </summary>
		public LocalMarketDataDrive()
			: this(Directory.GetCurrentDirectory())
		{
		}

		/// <summary>
		/// Создать <see cref="LocalMarketDataDrive"/>.
		/// </summary>
		/// <param name="path">Путь к директории с данными.</param>
		public LocalMarketDataDrive(string path)
		{
			_path = path;
		}

		private string _path;

		/// <summary>
		/// Путь к директории с данными.
		/// </summary>
		public override string Path
		{
			get { return _path; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				if (Path == value)
					return;

				_path = value;
				ResetDrives();
			}
		}

		private bool _useAlphabeticPath = true;

		/// <summary>
		/// Использовать ли алфавитный путь к данным. По-умолчанию включено.
		/// </summary>
		[Obsolete]
		public bool UseAlphabeticPath
		{
			get { return _useAlphabeticPath; }
			set
			{
				if (value == UseAlphabeticPath)
					return;

				_useAlphabeticPath = value;
				ResetDrives();
			}
		}

		private void ResetDrives()
		{
			lock (_drives.SyncRoot)
				_drives.Values.ForEach(d => d.ResetCache());
		}

		private static string GetExtension(StorageFormats format)
		{
			switch (format)
			{
				case StorageFormats.Binary:
					return ".bin";
				case StorageFormats.Csv:
					return ".csv";
				default:
					throw new ArgumentOutOfRangeException("format");
			}
		}

		/// <summary>
		/// Получить для инструмента доступные типы свечек с параметрами.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="format">Тип формата.</param>
		/// <returns>Доступные типы свечек с параметрами.</returns>
		public override IEnumerable<Tuple<Type, object[]>> GetCandleTypes(SecurityId securityId, StorageFormats format)
		{
			var secPath = GetSecurityPath(securityId);

			var ext = GetExtension(format);

			return InteropHelper
				.GetDirectories(secPath)
				.SelectMany(dir => Directory.GetFiles(dir, "candles_*" + ext).Select(IOPath.GetFileNameWithoutExtension))
				.Distinct()
				.Select(fileName =>
				{
					var parts = fileName.Split('_');
					var type = "{0}.{1}Message, {2}".Put(typeof(CandleMessage).Namespace, parts[1], typeof(CandleMessage).Assembly.FullName).To<Type>();
					var value = type.ToCandleArg(parts[2]);

					return Tuple.Create(type, value);
				})
				.GroupBy(t => t.Item1)
				.Select(g => Tuple.Create(g.Key, g.Select(t => t.Item2).ToArray()))
				.ToArray();
		}

		/// <summary>
		/// Создать хранилище для <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">Тип формата.</param>
		/// <returns>Хранилище для <see cref="IMarketDataStorage"/>.</returns>
		public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format)
		{
			if (securityId.IsDefault())
				throw new ArgumentNullException("securityId");

			return _drives.SafeAdd(Tuple.Create(securityId, dataType, arg, format),
				key => new LocalMarketDataStorageDrive(CreateFileName(dataType, arg), GetSecurityPath(securityId), format, this));
		}

		/// <summary>
		/// Получить название файла по типу данных.
		/// </summary>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="CandleMessage.Arg"/>.</param>
		/// <returns>Название файла.</returns>
		public static string CreateFileName(Type dataType, object arg)
		{
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			if (dataType == typeof(ExecutionMessage))
			{
				switch ((ExecutionTypes)arg)
				{
					case ExecutionTypes.Tick:
						return "trades";
					case ExecutionTypes.Order:
					case ExecutionTypes.Trade:
						return "execution";
					case ExecutionTypes.OrderLog:
						return "orderLog";
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else if (dataType == typeof(QuoteChangeMessage))
				return "quotes";
			else if (dataType == typeof(Level1ChangeMessage))
				return "security";
			else if (dataType == typeof(NewsMessage))
				return "news";
			else if (dataType.IsSubclassOf(typeof(CandleMessage)))
				return "candles_{0}_{1}".Put(dataType.Name.Replace("Message", string.Empty), TraderHelper.CandleArgToFolderName(arg));
			else
				throw new NotSupportedException(LocalizedStrings.Str2872Params.Put(dataType.FullName));
		}

#pragma warning disable 612
		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			UseAlphabeticPath = storage.GetValue<bool>("UseAlphabeticPath");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("UseAlphabeticPath", UseAlphabeticPath);
		}

		/// <summary>
		/// Получить путь к папке с маркет-данными для инструмента.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Путь к папке с маркет-данными.</returns>
		public string GetSecurityPath(SecurityId securityId)
		{
			if (securityId.IsDefault())
				throw new ArgumentNullException("securityId");

			var id = securityId.SecurityCode + "@" + securityId.BoardCode;

			var folderName = id.SecurityIdToFolderName();

			return UseAlphabeticPath
				? IOPath.Combine(Path, id.Substring(0, 1), folderName)
				: IOPath.Combine(Path, folderName);
		}
#pragma warning restore 612
	}
}