namespace StockSharp.Algo.Import
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Localization;

	using DataType = StockSharp.Messages.DataType;

	/// <summary>
	/// Settings of import.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str2842Key)]
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public class ImportSettings : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ImportSettings"/>.
		/// </summary>
		public ImportSettings()
		{
			DataType = DataType.TimeFrame(TimeSpan.FromMinutes(1));
			Directory = string.Empty;
			IncludeSubDirectories = false;
			FileMask = "*.csv";
			SkipFromHeader = 0;
			ColumnSeparator = ",";
			TimeZone = TimeZoneInfo.Utc;
			UpdateDuplicateSecurities = true;
			IgnoreNonIdSecurities = true;
		}

		private DataType _dataType;

		/// <summary>
		/// Data type info.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DataTypeKey,
			Description = LocalizedStrings.DataTypeKey,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 0)]
		public DataType DataType
		{
			get => _dataType;
			set
			{
				if (_dataType == value)
					return;

				_dataType = value ?? throw new ArgumentNullException(nameof(value));
				
				AllFields = FieldMappingRegistry.CreateFields(value);
				SelectedFields = AllFields.Where(f => f.IsRequired).Select((f, i) =>
				{
					f.Order = i;
					return f.GetOrClone();
				}).ToArray();

				NotifyChanged();
			}
		}

		private string _fileName;

		/// <summary>
		/// Full path to CSV file.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str2002Key,
			Description = LocalizedStrings.Str2843Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 1)]
		[Editor(typeof(IFileBrowserEditor), typeof(IFileBrowserEditor))]
		public string FileName
		{
			get => _fileName;
			set
			{
				_fileName = value?.Trim();
				NotifyChanged();
			}
		}

		private string _directory;

		/// <summary>
		/// Data directory.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str2237Key,
			Description = LocalizedStrings.Str2237Key + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 2)]
		[Editor(typeof(IFolderBrowserEditor), typeof(IFolderBrowserEditor))]
		public string Directory
		{
			get => _directory;
			set
			{
				_directory = value?.Trim();
				NotifyChanged();
			}
		}

		private string _fileMask = "*";

		/// <summary>
		/// File mask that uses for scanning in directory. For example, candles_*.csv.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.FileMaskKey,
			Description = LocalizedStrings.FileMaskDescriptionKey,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 3)]
		public string FileMask
		{
			get => _fileMask;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException();

				_fileMask = value;
				NotifyChanged();
			}
		}

		private bool _includeSubDirectories;

		/// <summary>
		/// Include subdirectories.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SubDirectoriesKey,
			Description = LocalizedStrings.SubDirectoriesIncludeKey,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 4)]
		public bool IncludeSubDirectories
		{
			get => _includeSubDirectories;
			set
			{
				_includeSubDirectories = value;
				NotifyChanged();
			}
		}

		private string _columnSeparator = ",";

		/// <summary>
		/// Column separator. Tabulation is denoted by TAB.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str2844Key,
			Description = LocalizedStrings.Str2845Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 5)]
		public string ColumnSeparator
		{
			get => _columnSeparator;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_columnSeparator = value;
				NotifyChanged();
			}
		}

		private int _skipFromHeader;

		/// <summary>
		/// Number of lines to be skipped from the beginning of the file (if they contain meta information).
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str2846Key,
			Description = LocalizedStrings.Str2847Key,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 6)]
		public int SkipFromHeader
		{
			get => _skipFromHeader;
			set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_skipFromHeader = value;
				NotifyChanged();
			}
		}

		private TimeZoneInfo _timeZone;

		/// <summary>
		/// Time zone.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TimeZoneKey,
			Description = LocalizedStrings.TimeZoneKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 7)]
		public TimeZoneInfo TimeZone
		{
			get => _timeZone;
			set
			{
				_timeZone = value ?? throw new ArgumentNullException(nameof(value));
				NotifyChanged();
			}
		}

		private TimeSpan _interval;

		/// <summary>
		/// Interval.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.Str175Key,
			Description = LocalizedStrings.IntervalDataUpdatesKey,
			GroupName = LocalizedStrings.Str1559Key,
			Order = 8)]
		[TimeSpanEditor(Mask = TimeSpanEditorMask.Days | TimeSpanEditorMask.Hours | TimeSpanEditorMask.Minutes)]
		public TimeSpan Interval
		{
			get => _interval;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str940);

				_interval = value;
				NotifyChanged();
			}
		}

		private IExtendedInfoStorageItem _extendedStorage;

		/// <summary>
		/// Extended information.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.ExtendedInfoKey,
			Description = LocalizedStrings.ExtendedInfoImportKey,
			GroupName = LocalizedStrings.SecuritiesKey,
			Order = 40)]
		public IExtendedInfoStorageItem ExtendedStorage
		{
			get => _extendedStorage;
			set
			{
				if (_extendedStorage == value)
					return;

				SelectedFields = SelectedFields.Except(ExtendedFields).ToArray();
				AllFields = AllFields.Except(ExtendedFields).ToArray();

				_extendedStorage = value;

				if (_extendedStorage != null)
					AllFields = AllFields.Concat(FieldMappingRegistry.CreateExtendedFields(_extendedStorage)).ToArray();

				NotifyChanged();
			}
		}

		private bool _updateDuplicateSecurities;

		/// <summary>
		/// Update duplicate securities if they already exists.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DuplicatesKey,
			Description = LocalizedStrings.UpdateDuplicateSecuritiesKey,
			GroupName = LocalizedStrings.SecuritiesKey,
			Order = 40)]
		public bool UpdateDuplicateSecurities
		{
			get => _updateDuplicateSecurities;
			set
			{
				_updateDuplicateSecurities = value;
				NotifyChanged();
			}
		}

		private bool _ignoreNonIdSecurities;

		/// <summary>
		/// Ignore securities without identifiers.
		/// </summary>
		[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.IgnoreNonIdSecuritiesKey,
			Description = LocalizedStrings.IgnoreNonIdSecuritiesDescKey,
			GroupName = LocalizedStrings.SecuritiesKey,
			Order = 41)]
		public bool IgnoreNonIdSecurities
		{
			get => _ignoreNonIdSecurities;
			set
			{
				_ignoreNonIdSecurities = value;
				NotifyChanged();
			}
		}

		//private void InitExtendedFields()
		//{
		//	UnSelectedFields.RemoveRange(UnSelectedFields.Where(f => f.IsExtended).ToArray());
		//	SelectedFields.RemoveRange(SelectedFields.Where(f => f.IsExtended).ToArray());

		//	UnSelectedFields.AddRange(Settings.ExtendedFields);
		//}

		/// <summary>
		/// All fields.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<FieldMapping> AllFields { get; private set; }

		/// <summary>
		/// Extended fields.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<FieldMapping> ExtendedFields => AllFields.Where(f => f.IsExtended);

		private IEnumerable<FieldMapping> _selectedFields = Enumerable.Empty<FieldMapping>();

		/// <summary>
		/// Selected fields.
		/// </summary>
		[Browsable(false)]
		public IEnumerable<FieldMapping> SelectedFields
		{
			get => _selectedFields;
			set
			{
				_selectedFields = value ?? throw new ArgumentNullException(nameof(value));
				NotifyChanged();
			}
		}

		/// <summary>
		/// Find files for importing.
		/// </summary>
		/// <returns>File list.</returns>
		public IEnumerable<string> GetFiles()
		{
			return !FileName.IsEmpty()
				? new[] { FileName }
				: System.IO.Directory.GetFiles(Directory, FileMask, IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			DataType = storage.GetValue<SettingsStorage>(nameof(DataType)).Load<DataType>();

			var extendedStorage = storage.GetValue<string>(nameof(ExtendedStorage));
			if (!extendedStorage.IsEmpty())
				ExtendedStorage = ServicesRegistry.ExtendedInfoStorage.Get(extendedStorage);

			SelectedFields = LoadSelectedFields(storage.GetValue<SettingsStorage[]>("Fields") ?? storage.GetValue<SettingsStorage[]>(nameof(SelectedFields)));

			FileName = storage.GetValue<string>(nameof(FileName));
			Directory = storage.GetValue(nameof(Directory), Directory);
			FileMask = storage.GetValue(nameof(FileMask), FileMask);
			IncludeSubDirectories = storage.GetValue(nameof(IncludeSubDirectories), IncludeSubDirectories);
			ColumnSeparator = storage.GetValue(nameof(ColumnSeparator), ColumnSeparator);
			SkipFromHeader = storage.GetValue(nameof(SkipFromHeader), SkipFromHeader);
			TimeZone = storage.GetValue(nameof(TimeZone), TimeZone);
			UpdateDuplicateSecurities = storage.GetValue(nameof(UpdateDuplicateSecurities), UpdateDuplicateSecurities);
			IgnoreNonIdSecurities = storage.GetValue(nameof(IgnoreNonIdSecurities), IgnoreNonIdSecurities);
			Interval = storage.GetValue(nameof(Interval), Interval);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(DataType), DataType.Save());
			storage.SetValue(nameof(ExtendedStorage), ExtendedStorage?.StorageName);
			storage.SetValue(nameof(SelectedFields), SelectedFields.Select(f => f.Save()).ToArray());

			storage.SetValue(nameof(FileName), FileName);
			storage.SetValue(nameof(Directory), Directory);
			storage.SetValue(nameof(FileMask), FileMask);
			storage.SetValue(nameof(IncludeSubDirectories), IncludeSubDirectories);
			storage.SetValue(nameof(ColumnSeparator), ColumnSeparator);
			storage.SetValue(nameof(SkipFromHeader), SkipFromHeader);
			storage.SetValue(nameof(TimeZone), TimeZone);
			storage.SetValue(nameof(UpdateDuplicateSecurities), UpdateDuplicateSecurities);
			storage.SetValue(nameof(IgnoreNonIdSecurities), IgnoreNonIdSecurities);
			storage.SetValue(nameof(Interval), Interval);
		}

		private IEnumerable<FieldMapping> LoadSelectedFields(IEnumerable<SettingsStorage> storages)
		{
			var selectedFields = new List<FieldMapping>();

			foreach (var fieldSettings in storages)
			{
				var fieldName = fieldSettings.GetValue<string>(nameof(FieldMapping.Name));
				var field = AllFields.FirstOrDefault(f => f.Name.CompareIgnoreCase(fieldName));

				if (field == null)
					continue;

				field = field.GetOrClone();

				field.Load(fieldSettings);
				selectedFields.Add(field);
			}

			return selectedFields;
		}

		/// <inheritdoc />
		public override string ToString()
		{
			var msgType = DataType.MessageType;

			if (DataType == DataType.Securities)
				return LocalizedStrings.Securities;
			else if (DataType == DataType.Level1)
				return LocalizedStrings.Level1;
			else if (DataType == DataType.MarketDepth)
				return LocalizedStrings.MarketDepths;
			else if (DataType == DataType.PositionChanges)
				return LocalizedStrings.Str972;
			else if (DataType.IsCandles)
				return LocalizedStrings.Candles;
			else if (DataType == DataType.OrderLog)
				return LocalizedStrings.OrderLog;
			else if (DataType == DataType.Ticks)
				return LocalizedStrings.Ticks;
			else if (DataType == DataType.Transactions)
				return LocalizedStrings.Transactions;
			else
				throw new ArgumentOutOfRangeException(nameof(DataType.MessageType), msgType, LocalizedStrings.Str1219);
		}

		/// <summary>
		/// Fill <see cref="CsvImporter"/>.
		/// </summary>
		/// <param name="parser">Messages parser from text file in CSV format.</param>
		public void FillParser(CsvParser parser)
		{
			if (parser == null)
				throw new ArgumentNullException(nameof(parser));

			parser.ColumnSeparator = ColumnSeparator;
			parser.SkipFromHeader = SkipFromHeader;
			parser.TimeZone = TimeZone;
			parser.ExtendedInfoStorageItem = ExtendedStorage;
			parser.IgnoreNonIdSecurities = IgnoreNonIdSecurities;
		}

		/// <summary>
		/// Fill <see cref="CsvImporter"/>.
		/// </summary>
		/// <param name="importer">Messages importer from text file in CSV format into storage.</param>
		public void FillImporter(CsvImporter importer)
		{
			FillParser(importer);
			importer.UpdateDuplicateSecurities = UpdateDuplicateSecurities;
		}
	}
}