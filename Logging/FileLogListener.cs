namespace StockSharp.Logging
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Localization;

	/// <summary>
	/// Режими разделения лог файлов по датам.
	/// </summary>
	public enum SeparateByDateModes
	{
		/// <summary>
		/// Не разделять. Разделение выключено.
		/// </summary>
		None,

		/// <summary>
		/// Разделять через добавление к названию файла.
		/// </summary>
		FileName,

		/// <summary>
		/// Разделять через под директории.
		/// </summary>
		SubDirectories,
	}

	/// <summary>
	/// Логгер, записывающий данные в текстовый файл.
	/// </summary>
	public class FileLogListener : LogListener
	{
		private static readonly char[] _digitChars;

		static FileLogListener()
		{
			_digitChars = new char[10];

			for (var i = 0; i < 10; i++)
				_digitChars[i] = (char)(i + '0');
		}

		private readonly PairSet<Tuple<string, DateTime>, StreamWriter> _writers = new PairSet<Tuple<string, DateTime>, StreamWriter>();
		private readonly Dictionary<StreamWriter, string> _fileNames = new Dictionary<StreamWriter, string>();

		/// <summary>
		/// Создать <see cref="FileLogListener"/>. Для каждого <see cref="ILogSource"/> будет создан отдельный файл с названием, равный <see cref="ILogSource.Name"/>.
		/// </summary>
		public FileLogListener()
		{
		}

		/// <summary>
		/// Создать <see cref="FileLogListener"/>. Все сообщения из <see cref="ILogSource.Log"/> будут записывать в файл <paramref name="fileName"/>.
		/// </summary>
		/// <param name="fileName">Название текстового файла, в который будут писаться сообщения из события <see cref="ILogSource.Log"/>.</param>
		public FileLogListener(string fileName)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException("fileName");

			var info = new FileInfo(fileName);

			if (info.Name.IsEmpty())
				throw new ArgumentException(LocalizedStrings.NameFileNotContainFileName.Put(fileName), "fileName");

			FileName = Path.GetFileNameWithoutExtension(info.Name);

			if (!info.Extension.IsEmpty())
				Extension = info.Extension;

			if (!info.DirectoryName.IsEmpty())
				LogDirectory = info.DirectoryName;
		}

		private string _fileName;

		/// <summary>
		/// Название текстового файла (без расширения), в который будут писаться сообщения из события <see cref="ILogSource.Log"/>.
		/// </summary>
		public string FileName
		{
			get { return _fileName; }
			set
			{
				_fileName = value.IsEmpty() ? null : value;
			}
		}

		private Encoding _encoding = Encoding.UTF8;

		/// <summary>
		/// Кодировка файла. По умолчанию используется кодировка UTF-8.
		/// </summary>
		public Encoding Encoding
		{
			get { return _encoding; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_encoding = value;
			}
		}

		private long _maxLength;

		/// <summary>
		/// Максимальная длина файла лога. По-умолчанию установлено 0, что значит файл будет иметь неограниченный размер.
		/// </summary>
		public long MaxLength
		{
			get { return _maxLength; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_maxLength = value;
			}
		}

		private int _maxCount;

		/// <summary>
		/// Максимальное количество роллируемых файлов. По-умолчанию установлено 0, что значит файлы будут роллироваться без ограничения.
		/// </summary>
		public int MaxCount
		{
			get { return _maxCount; }
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_maxCount = value;
			}
		}

		/// <summary>
		/// Добавлять ли в файл данные, если он уже существует. По-умолчанию выключено.
		/// </summary>
		public bool Append { get; set; }

		private string _logDirectory = Directory.GetCurrentDirectory();

		/// <summary>
		/// Директория, где будет создан файл лога. По умолчанию - директория с исполняемым файлом.
		/// </summary>
		/// <remarks>
		/// Если директория не существует, она будет создана.
		/// </remarks>
		public string LogDirectory
		{
			get { return _logDirectory; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				Directory.CreateDirectory(value);

				_logDirectory = value;
			}
		}

		private bool _writeChildDataToRootFile = true;

		/// <summary>
		/// Записывать данные дочерних источников в файл родителя. По-умолчанию режим включен.
		/// </summary>
		public bool WriteChildDataToRootFile
		{
			get { return _writeChildDataToRootFile; }
			set { _writeChildDataToRootFile = value; }
		}

		private string _extension = ".txt";

		/// <summary>
		/// Расширение лог файлов. По-умолчанию значение равно txt.
		/// </summary>
		public string Extension
		{
			get { return _extension; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_extension = value;
			}
		}

		/// <summary>
		/// Выводить в файл идентификатор источника <see cref="ILogSource.Id"/>. По-умолчанию выключено.
		/// </summary>
		public bool WriteSourceId { get; set; }

		private string _directoryDateFormat = "yyyy_MM_dd";

		/// <summary>
		/// Формат названия директории, представляющая дату. По-умолчанию используется yyyy_MM_dd.
		/// </summary>
		public string DirectoryDateFormat
		{
			get { return _directoryDateFormat; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_directoryDateFormat = value;
			}
		}

		/// <summary>
		/// Режим разделения лог файлов по датам. По умолчанию режим равен <see cref="SeparateByDateModes.None"/>.
		/// </summary>
		public SeparateByDateModes SeparateByDates { get; set; }

		private string GetFileName(string sourceName, DateTime date)
		{
			var invalidChars = sourceName.Intersect(Path.GetInvalidFileNameChars()).ToArray();

			if (invalidChars.Any())
			{
				var sb = new StringBuilder(sourceName);

				foreach (var invalidChar in invalidChars)
					sb.Replace(invalidChar, '_');

				sourceName = sb.ToString();
			}

			var fileName = sourceName + Extension;
			var dirName = LogDirectory;

			switch (SeparateByDates)
			{
				case SeparateByDateModes.None:
					break;
				case SeparateByDateModes.FileName:
					fileName = date.ToString(DirectoryDateFormat) + "_" + fileName;
					break;
				case SeparateByDateModes.SubDirectories:
					dirName = Path.Combine(dirName, date.ToString(DirectoryDateFormat));
					Directory.CreateDirectory(dirName);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			fileName = Path.Combine(dirName, fileName);
			return fileName;
		}

		/// <summary>
		/// Создать текстового писателя.
		/// </summary>
		/// <param name="fileName">Название текстового файла, в которое будут писаться сообщения из события <see cref="ILogSource.Log"/>.</param>
		/// <returns>Текстовый писатель.</returns>
		protected virtual StreamWriter OnCreateWriter(string fileName)
		{
			var writer = new StreamWriter(fileName, Append, Encoding);
			_fileNames.Add(writer, fileName);
			return writer;
		}

		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		protected override void OnWriteMessages(IEnumerable<LogMessage> messages)
		{
			// pyh: эмуляция года данных происходит за 5 секунд. На выходе 365 файлов лога? Бред.
			//var date = SeparateByDates != SeparateByDateModes.None ? message.Time.Date : default(DateTime);
			var date = SeparateByDates != SeparateByDateModes.None ? DateTime.Today : default(DateTime);

			string prevFileName = null;
			StreamWriter prevWriter = null;

			var isDisposing = false;

			foreach (var group in messages.GroupBy(m =>
			{
				if (isDisposing || m.IsDispose)
				{
					isDisposing = true;
					return null;
				}

				var fileName = FileName ?? GetSourceName(m.Source);

				if (prevFileName == fileName)
					return prevWriter;

				var key = Tuple.Create(fileName, date);

				var writer = _writers.TryGetValue(key);

				if (writer == null)
				{
					if (isDisposing)
						return null;

					writer = OnCreateWriter(GetFileName(fileName, date));
					_writers.Add(key, writer);
				}

				prevFileName = fileName;
				prevWriter = writer;
				return writer;
			}).AsParallel())
			{
				if (isDisposing)
				{
					_writers.Values.ForEach(w => w.Dispose());
					_writers.Clear();
					return;
				}

				var writer = group.Key;

				foreach (var message in group)
				{
					WriteMessage(writer, message);

					if (MaxLength <= 0 || writer.BaseStream.Position < MaxLength)
						continue;

					var fileName = _fileNames[writer];
					_fileNames.Remove(writer);

					var key = _writers[writer];
					writer.Dispose();
						
					var maxIndex = 0;

					while (File.Exists(GetRollingFileName(fileName, maxIndex + 1)))
					{
						maxIndex++;
					}

					for (var i = maxIndex; i > 0; i--)
					{
						File.Move(GetRollingFileName(fileName, i), GetRollingFileName(fileName, i + 1));
					}

					File.Move(fileName, GetRollingFileName(fileName, 1));

					if (MaxCount > 0)
					{
						maxIndex++;

						for (var i = MaxCount; i <= maxIndex; i++)
						{
							File.Delete(GetRollingFileName(fileName, i));
						}
					}

					writer = OnCreateWriter(fileName);
					_writers[key] = writer;
				}

				writer.Flush();
			}
		}

		private static string GetRollingFileName(string fileName, int index)
		{
			if (index <= 0)
				throw new ArgumentOutOfRangeException("index", index, LocalizedStrings.RollerFileIndexMustGreatZero);

			return Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "." + index + Path.GetExtension(fileName));
		}

		private string GetSourceName(ILogSource source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			var name = source.Name;

			if (WriteChildDataToRootFile && source.Parent != null)
				name = GetSourceName(source.Parent);

			return name;
		}

		//private string DateTimeFormat
		//{
		//    get { return SeparateByDates != SeparateByDateModes.None ? TimeFormat : DateFormat + " " + TimeFormat; }
		//}

		private void WriteMessage(TextWriter writer, LogMessage message)
		{
			writer.Write(ToFastDateCharArray(message.Time));
			writer.Write("|");
			writer.Write("{0, -7}".Put(message.Level == LogLevels.Info ? string.Empty : message.Level.ToString()));
			writer.Write("|");
			writer.Write("{0, -10}".Put(message.Source.Name));
			writer.Write("|");

			if (WriteSourceId)
			{
				writer.Write("{0, -20}".Put(message.Source.Id));
				writer.Write("|");
			}

			writer.WriteLine(message.Message);
		}

		// http://ramblings.markstarmer.co.uk/2011/07/efficiency-datetime-tostringstring/
		private char[] ToFastDateCharArray(DateTimeOffset time)
		{
			var hasDate = SeparateByDates == SeparateByDateModes.None;
			
			var timeChars = new char[12 + (hasDate ? 11: 0)];

			var offset = 0;

			if (hasDate)
			{
				var year = time.Year;
				var month = time.Month;
				var day = time.Day;

				timeChars[0] = _digitChars[year / 1000];
				timeChars[1] = _digitChars[year % 1000 / 100];
				timeChars[2] = _digitChars[year % 100 / 10];
				timeChars[3] = _digitChars[year % 10];
				timeChars[4] = '/';
				timeChars[5] = _digitChars[month / 10];
				timeChars[6] = _digitChars[month % 10];
				timeChars[7] = '/';
				timeChars[8] = _digitChars[day / 10];
				timeChars[9] = _digitChars[day % 10];
				timeChars[10] = ' ';

				offset = 11;
			}

			var hour = time.Hour;
			var minute = time.Minute;
			var second = time.Second;
			var millisecond = time.Millisecond;

			timeChars[offset + 0] = _digitChars[hour / 10];
			timeChars[offset + 1] = _digitChars[hour % 10];
			timeChars[offset + 2] = ':';
			timeChars[offset + 3] = _digitChars[minute / 10];
			timeChars[offset + 4] = _digitChars[minute % 10];
			timeChars[offset + 5] = ':';
			timeChars[offset + 6] = _digitChars[second / 10];
			timeChars[offset + 7] = _digitChars[second % 10];
			timeChars[offset + 8] = '.';
			timeChars[offset + 9] = _digitChars[millisecond % 1000 / 100];
			timeChars[offset + 10] = _digitChars[millisecond % 100 / 10];
			timeChars[offset + 11] = _digitChars[millisecond % 10];

			return timeChars;
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			FileName = storage.GetValue<string>("FileName");
			MaxLength = storage.GetValue<long>("MaxLength");
			MaxCount = storage.GetValue<int>("MaxCount");
			Append = storage.GetValue<bool>("Append");
			LogDirectory = storage.GetValue<string>("LogDirectory");
			WriteChildDataToRootFile = storage.GetValue<bool>("WriteChildDataToRootFile");
			Extension = storage.GetValue<string>("Extension");
			WriteSourceId = storage.GetValue<bool>("WriteSourceId");
			DirectoryDateFormat = storage.GetValue<string>("DirectoryDateFormat");
			SeparateByDates = storage.GetValue<SeparateByDateModes>("SeparateByDates");
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("FileName", FileName);
			storage.SetValue("MaxLength", MaxLength);
			storage.SetValue("MaxCount", MaxCount);
			storage.SetValue("Append", Append);
			storage.SetValue("LogDirectory", LogDirectory);
			storage.SetValue("WriteChildDataToRootFile", WriteChildDataToRootFile);
			storage.SetValue("Extension", Extension);
			storage.SetValue("WriteSourceId", WriteSourceId);
			storage.SetValue("DirectoryDateFormat", DirectoryDateFormat);
			storage.SetValue("SeparateByDates", SeparateByDates.To<string>());
		}
	}
}