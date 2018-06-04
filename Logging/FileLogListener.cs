#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Logging.Logging
File: FileLogListener.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	/// Modes of log files splitting by date.
	/// </summary>
	public enum SeparateByDateModes
	{
		/// <summary>
		/// Do not split. The splitting is off.
		/// </summary>
		None,

		/// <summary>
		/// To split by adding to the file name.
		/// </summary>
		FileName,

		/// <summary>
		/// To split via subdirectories.
		/// </summary>
		SubDirectories,
	}

	/// <summary>
	/// The logger recording the data to a text file.
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
		/// To create <see cref="FileLogListener"/>. For each <see cref="ILogSource"/> a separate file with a name equal to <see cref="ILogSource.Name"/> will be created.
		/// </summary>
		public FileLogListener()
		{
		}

		/// <summary>
		/// To create <see cref="FileLogListener"/>. All messages from the <see cref="ILogSource.Log"/> will be recorded to the file <paramref name="fileName" />.
		/// </summary>
		/// <param name="fileName">The name of a text file to which messages from the event <see cref="ILogSource.Log"/> will be recorded.</param>
		public FileLogListener(string fileName)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			var info = new FileInfo(fileName);

			if (info.Name.IsEmpty())
				throw new ArgumentException(LocalizedStrings.NameFileNotContainFileName.Put(fileName), nameof(fileName));

			FileName = Path.GetFileNameWithoutExtension(info.Name);

			if (!info.Extension.IsEmpty())
				Extension = info.Extension;

			if (!info.DirectoryName.IsEmpty())
				LogDirectory = info.DirectoryName;
		}

		private string _fileName;

		/// <summary>
		/// The name of a text file (without filename extension) to which messages from the event <see cref="ILogSource.Log"/> will be recorded.
		/// </summary>
		public string FileName
		{
			get => _fileName;
			set => _fileName = value.IsEmpty() ? null : value;
		}

		private Encoding _encoding = Encoding.UTF8;

		/// <summary>
		/// File encoding. The default is UTF-8 encoding.
		/// </summary>
		public Encoding Encoding
		{
			get => _encoding;
			set => _encoding = value ?? throw new ArgumentNullException(nameof(value));
		}

		private long _maxLength;

		/// <summary>
		/// The maximum length of the log file. The default is 0, which means that the file will have unlimited size.
		/// </summary>
		public long MaxLength
		{
			get => _maxLength;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_maxLength = value;
			}
		}

		private int _maxCount;

		/// <summary>
		/// The maximum number of rolling files. The default is 0, which means that the files will be rolled without limitation.
		/// </summary>
		public int MaxCount
		{
			get => _maxCount;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException();

				_maxCount = value;
			}
		}

		/// <summary>
		/// Whether to add the data to a file, if it already exists. The default is off.
		/// </summary>
		public bool Append { get; set; }

		private string _logDirectory = Directory.GetCurrentDirectory();

		/// <summary>
		/// The directory where the log file will be created. By default, it is the directory where the executable file is located.
		/// </summary>
		/// <remarks>
		/// If the directory does not exist, it will be created.
		/// </remarks>
		public string LogDirectory
		{
			get => _logDirectory;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				Directory.CreateDirectory(value);

				_logDirectory = value;
			}
		}

		/// <summary>
		/// To record the subsidiary sources data to the parent file. The default mode is enabled.
		/// </summary>
		public bool WriteChildDataToRootFile { get; set; } = true;

		private string _extension = ".txt";

		/// <summary>
		/// Extension of log files. The default value is 'txt'.
		/// </summary>
		public string Extension
		{
			get => _extension;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_extension = value;
			}
		}

		/// <summary>
		/// To output the source identifier <see cref="ILogSource.Id"/> to a file. The default is off.
		/// </summary>
		public bool WriteSourceId { get; set; }

		private string _directoryDateFormat = "yyyy_MM_dd";

		/// <summary>
		/// The directory name format that represents a date. By default is 'yyyy_MM_dd'.
		/// </summary>
		public string DirectoryDateFormat
		{
			get => _directoryDateFormat;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_directoryDateFormat = value;
			}
		}

		/// <summary>
		/// The mode of log files splitting by date. The default mode is <see cref="SeparateByDateModes.None"/>.
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
		/// To create a text writer.
		/// </summary>
		/// <param name="fileName">The name of the text file to which messages from the event <see cref="ILogSource.Log"/> will be recorded.</param>
		/// <returns>A text writer.</returns>
		protected virtual StreamWriter OnCreateWriter(string fileName)
		{
			var writer = new StreamWriter(fileName, Append, Encoding);
			_fileNames.Add(writer, fileName);
			return writer;
		}

		/// <summary>
		/// To record messages.
		/// </summary>
		/// <param name="messages">Debug messages.</param>
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
					Dispose();
					return;
				}

				var writer = group.Key;

				try
				{
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
				}
				finally
				{
					writer.Flush();
				}
			}
		}

		private static string GetRollingFileName(string fileName, int index)
		{
			if (index <= 0)
				throw new ArgumentOutOfRangeException(nameof(index), index, LocalizedStrings.RollerFileIndexMustGreatZero);

			return Path.Combine(Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "." + index + Path.GetExtension(fileName));
		}

		private string GetSourceName(ILogSource source)
		{
			if (source == null)
				throw new ArgumentNullException(nameof(source));

			var name = source.Name;

			if (!source.IsRoot && WriteChildDataToRootFile && source.Parent != null)
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
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			FileName = storage.GetValue<string>(nameof(FileName));
			MaxLength = storage.GetValue<long>(nameof(MaxLength));
			MaxCount = storage.GetValue<int>(nameof(MaxCount));
			Append = storage.GetValue<bool>(nameof(Append));
			LogDirectory = storage.GetValue<string>(nameof(LogDirectory));
			WriteChildDataToRootFile = storage.GetValue<bool>(nameof(WriteChildDataToRootFile));
			Extension = storage.GetValue<string>(nameof(Extension));
			WriteSourceId = storage.GetValue<bool>(nameof(WriteSourceId));
			DirectoryDateFormat = storage.GetValue<string>(nameof(DirectoryDateFormat));
			SeparateByDates = storage.GetValue<SeparateByDateModes>(nameof(SeparateByDates));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(FileName), FileName);
			storage.SetValue(nameof(MaxLength), MaxLength);
			storage.SetValue(nameof(MaxCount), MaxCount);
			storage.SetValue(nameof(Append), Append);
			storage.SetValue(nameof(LogDirectory), LogDirectory);
			storage.SetValue(nameof(WriteChildDataToRootFile), WriteChildDataToRootFile);
			storage.SetValue(nameof(Extension), Extension);
			storage.SetValue(nameof(WriteSourceId), WriteSourceId);
			storage.SetValue(nameof(DirectoryDateFormat), DirectoryDateFormat);
			storage.SetValue(nameof(SeparateByDates), SeparateByDates.To<string>());
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_writers.Values.ForEach(w => w.Dispose());

			_fileNames.Clear();
			_writers.Clear();

			base.DisposeManaged();
		}
	}
}