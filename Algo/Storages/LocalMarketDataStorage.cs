namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using IOPath = System.IO.Path;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	/// <summary>
	/// Файловое хранилище маркет-данных.
	/// </summary>
	public class LocalMarketDataStorage : BaseMarketDataStorage
	{
		private sealed class LocalSecurityMarketDataStorage : ISecurityMarketDataStorage
		{
			private sealed class LocalMessageStorage<TMessage> : IMessageStorage<TMessage>
				where TMessage : Message
			{
				private readonly SyncObject _cacheSync = new SyncObject();

				private readonly LocalSecurityMarketDataStorage _parent;
				private readonly string _fileName;
				private readonly IMessageSerializer<TMessage> _serializer;
				private readonly string _fileNameWithExtension;

				// ReSharper disable once StaticFieldInGenericType
				private static readonly Version _dateVersion = new Version(1, 0);
				// ReSharper enable once StaticFieldInGenericType
				private const string _dateFormat = "yyyy_MM_dd";

				public LocalMessageStorage(LocalSecurityMarketDataStorage parent, string fileName, IMessageSerializer<TMessage> serializer)
				{
					if (parent == null)
						throw new ArgumentNullException("parent");

					if (serializer == null)
						throw new ArgumentNullException("serializer");

					if (fileName.IsEmpty())
						throw new ArgumentNullException("fileName");

					_parent = parent;
					_fileName = fileName;
					_serializer = serializer;
					_fileNameWithExtension = _fileName + GetExtension(_serializer);

					_datesDict = new Lazy<CachedSynchronizedOrderedDictionary<DateTime, DateTime>>(() =>
					{
						var retVal = new CachedSynchronizedOrderedDictionary<DateTime, DateTime>();

						var datesPath = GetDatesCachePath();

						if (File.Exists(datesPath))
						{
							foreach (var date in LoadDates())
								retVal.Add(date, date);
						}
						else
						{
							var rootDir = _parent.Path;

							var dates = InteropHelper
								.GetDirectories(rootDir)
								.Where(dir => File.Exists(IOPath.Combine(dir, _fileNameWithExtension)))
								.Select(dir => IOPath.GetFileName(dir).ToDateTime(_dateFormat));

							foreach (var date in dates)
								retVal.Add(date, date);

							SaveDates(retVal.CachedValues);
						}

						return retVal;
					}).Track();
				}

				IMessageSerializer<TMessage> IMessageStorage<TMessage>.Serializer
				{
					get { return _serializer; }
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

				private string GetDatesCacheFileName()
				{
					return _fileName + (_serializer is CsvMarketDataSerializer<TMessage> ? "Csv" : string.Empty) + "Dates.bin";
				}

				private string GetDatesCachePath()
				{
					return IOPath.Combine(_parent.Path, GetDatesCacheFileName());
				}

				public void ClearDatesCache()
				{
					if (Directory.Exists(_parent.Path))
					{
						lock (_cacheSync)
							File.Delete(GetDatesCachePath());
					}

					// TODO
					//ResetCache();
				}

				void IMessageStorage<TMessage>.Delete(DateTime date)
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

				void IMessageStorage<TMessage>.Save(DateTime date, Stream stream)
				{
					date = date.ChangeKind(DateTimeKind.Utc);

					Directory.CreateDirectory(GetDataPath(date));

					using (var file = File.OpenWrite(GetPath(date, false)))
						stream.CopyTo(file);

					var dates = DatesDict;

					dates[date] = date;

					SaveDates(Dates.ToArray());
				}

				Stream IMessageStorage<TMessage>.Open(DateTime date)
				{
					var path = GetPath(date.ChangeKind(DateTimeKind.Utc), true);

					return File.Exists(path)
						? File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read)
						: Stream.Null;
				}

				private IEnumerable<DateTime> LoadDates()
				{
					var path = GetDatesCachePath();

					try
					{
						using (var file = new FileStream(path, FileMode.Open, FileAccess.Read))
						{
							var version = new Version(file.ReadByte(), file.ReadByte());

							if (version > _dateVersion)
								throw new InvalidOperationException(LocalizedStrings.Str1002Params.Put(GetDatesCacheFileName(), version, _dateVersion));

							var count = file.Read<int>();

							var dates = new DateTime[count];

							for (var i = 0; i < count; i++)
							{
								dates[i] = file.Read<DateTime>().ChangeKind(DateTimeKind.Utc);
							}

							return dates;
						}
					}
					catch (Exception ex)
					{
						throw new InvalidOperationException(LocalizedStrings.Str1003Params.Put(path), ex);
					}
				}

				private void SaveDates(DateTime[] dates)
				{
					try
					{
						if (!Directory.Exists(_parent.Path))
						{
							if (dates.IsEmpty())
								return;

							Directory.CreateDirectory(_parent.Path);
						}

						var stream = new MemoryStream();

						stream.WriteByte((byte)_dateVersion.Major);
						stream.WriteByte((byte)_dateVersion.Minor);
						stream.Write(dates.Length);

						foreach (var date in dates)
							stream.Write(date);

						lock (_cacheSync)
							stream.Save(GetDatesCachePath());
					}
					catch (UnauthorizedAccessException)
					{
						// если папка с данными с правами только на чтение
					}
				}

				private string GetDataPath(DateTime date)
				{
					return IOPath.Combine(_parent.Path, date.ToString(_dateFormat));
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
			}

			private readonly LocalMarketDataStorage _parent;
			private readonly SecurityId _securityId;

			public LocalSecurityMarketDataStorage(LocalMarketDataStorage parent, SecurityId securityId)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				if (securityId.IsDefault())
					throw new ArgumentNullException("securityId");

				_parent = parent;
				_securityId = securityId;
			}

			private string Path
			{
				get
				{
					return _parent.GetSecurityPath(_securityId);
				}
			}

			IMarketDataStorage ISecurityMarketDataStorage.Storage
			{
				get { return _parent; }
			}

			SecurityId ISecurityMarketDataStorage.SecurityId
			{
				get { return _securityId; }
			}

			IEnumerable<Tuple<Type, object[]>> ISecurityMarketDataStorage.GetCandleTypes(IMessageSerializer<CandleMessage> serializer)
			{
				var secPath = _parent.GetSecurityPath(_securityId);

				var ext = GetExtension(serializer);

				return InteropHelper
					.GetDirectories(secPath)
					.SelectMany(dir => Directory.GetFiles(dir, "candles_*" + ext).Select(IOPath.GetFileNameWithoutExtension))
					.Distinct()
					.Select(fileName =>
					{
						var parts = fileName.Split('_');
						var type = "{0}.{1}, {2}".Put(typeof(Candle).Namespace, parts[1], typeof(Candle).Assembly.FullName).To<Type>();
						var value = type.ToCandleMessageType().ToCandleArg(parts[2]);

						return Tuple.Create(type, value);
					})
					.GroupBy(t => t.Item1)
					.Select(g => Tuple.Create(g.Key, g.Select(t => t.Item2).ToArray()))
					.ToArray();
			}

			IMessageStorage<ExecutionMessage> ISecurityMarketDataStorage.GetTickMessageStorage(IMessageSerializer<ExecutionMessage> serializer)
			{
				return new LocalMessageStorage<ExecutionMessage>(this, CreateFileName(typeof(ExecutionMessage), ExecutionTypes.Tick), serializer);
			}

			IMessageStorage<QuoteChangeMessage> ISecurityMarketDataStorage.GetQuoteMessageStorage(IMessageSerializer<QuoteChangeMessage> serializer)
			{
				return new LocalMessageStorage<QuoteChangeMessage>(this, CreateFileName(typeof(QuoteChangeMessage), null), serializer);
			}

			IMessageStorage<ExecutionMessage> ISecurityMarketDataStorage.GetOrderLogMessageStorage(IMessageSerializer<ExecutionMessage> serializer)
			{
				return new LocalMessageStorage<ExecutionMessage>(this, CreateFileName(typeof(ExecutionMessage), ExecutionTypes.OrderLog), serializer);
			}

			IMessageStorage<Level1ChangeMessage> ISecurityMarketDataStorage.GetLevel1MessageStorage(IMessageSerializer<Level1ChangeMessage> serializer)
			{
				return new LocalMessageStorage<Level1ChangeMessage>(this, CreateFileName(typeof(Level1ChangeMessage), null), serializer);
			}

			IMessageStorage<TCandleMessage> ISecurityMarketDataStorage.GetCandleMessageStorage<TCandleMessage>(IMessageSerializer<TCandleMessage> serializer, object arg)
				where TCandleMessage : CandleMessage
			{
				return new LocalMessageStorage<TCandleMessage>(this, CreateFileName(typeof(TCandleMessage), arg), serializer);
			}

			IMessageStorage<ExecutionMessage> ISecurityMarketDataStorage.GetExecutionStorage(IMessageSerializer<ExecutionMessage> serializer, ExecutionTypes type)
			{
				return new LocalMessageStorage<ExecutionMessage>(this, CreateFileName(typeof(ExecutionMessage), type), serializer);
			}

			private static string GetExtension<T>(IMessageSerializer<T> serializer)
			{
				if (serializer is CsvMarketDataSerializer<T>)
					return ".csv";
				
				return ".bin";
			}

			//public void ResetCache()
			//{
			//	_datesDict.Reset();
			//}
		}

		//private readonly SynchronizedDictionary<SecurityId, LocalSecurityMarketDataStorage> _secStorages = new SynchronizedDictionary<SecurityId, LocalSecurityMarketDataStorage>();

		/// <summary>
		/// Создать <see cref="LocalMarketDataStorage"/>.
		/// </summary>
		public LocalMarketDataStorage()
			: this(Directory.GetCurrentDirectory())
		{
		}

		/// <summary>
		/// Создать <see cref="LocalMarketDataStorage"/>.
		/// </summary>
		/// <param name="path">Путь к директории с данными.</param>
		public LocalMarketDataStorage(string path)
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
				//ResetDrives();
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
				//ResetDrives();
			}
		}

		//private void ResetDrives()
		//{
		//	lock (_secStorages.SyncRoot)
		//		_secStorages.Values.ForEach(d => d.ResetCache());
		//}

		/// <summary>
		/// Получить хранилище для инструмента.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Хранилище для инструмента.</returns>
		public override ISecurityMarketDataStorage GetSecurityStorage(SecurityId securityId)
		{
			return new LocalSecurityMarketDataStorage(this, securityId);
			//return _secStorages.SafeAdd(securityId, key => new LocalSecurityMarketDataStorage(this, securityId));
		}

		/// <summary>
		/// Получить название файла по типу данных.
		/// </summary>
		/// <param name="dataType">Тип маркет-данных.</param>
		/// <param name="arg">Параметр, ассоциированный с типом <paramref name="dataType"/>. Например, <see cref="Candle.Arg"/>.</param>
		/// <returns>Название файла.</returns>
		public static string CreateFileName(Type dataType, object arg)
		{
			if (dataType == null)
				throw new ArgumentNullException("dataType");

			if (dataType == typeof(ExecutionMessage))
			{
				var execType = (ExecutionTypes)arg;

				switch (execType)
				{
					case ExecutionTypes.Tick:
						dataType = typeof(Trade);
						break;
					case ExecutionTypes.Order:
					case ExecutionTypes.Trade:
						dataType = typeof(Order);
						break;
					case ExecutionTypes.OrderLog:
						dataType = typeof(OrderLogItem);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else if (dataType.IsSubclassOf(typeof(CandleMessage)))
				dataType = dataType.ToCandleType();
			else if (dataType == typeof(QuoteChangeMessage))
				dataType = typeof(MarketDepth);

			if (dataType == typeof(Trade))
				return "trades";
			else if (dataType == typeof(MarketDepth))
				return "quotes";
			else if (dataType == typeof(OrderLogItem))
				return "orderLog";
			else if (dataType == typeof(Level1ChangeMessage))
				return "security";
			else if (dataType == typeof(Order))
				return "execution";
			else if (dataType.IsSubclassOf(typeof(Candle)))
				return "candles_{0}_{1}".Put(dataType.Name, TraderHelper.CandleArgToFolderName(arg));
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