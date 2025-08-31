namespace StockSharp.Algo.Import;

/// <summary>
/// Settings of import.
/// </summary>
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ImportSettingsKey)]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class ImportSettings : NotifiableObject, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ImportSettings"/>.
	/// </summary>
	public ImportSettings()
	{
		DataType = TimeSpan.FromMinutes(1).TimeFrame();
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
		GroupName = LocalizedStrings.CommonKey,
		Order = 0)]
	[BasicSetting]
	public DataType DataType
	{
		get => _dataType;
		set
		{
			if (_dataType == value)
				return;

			_dataType = value ?? throw new ArgumentNullException(nameof(value));
			
			AllFields = FieldMappingRegistry.CreateFields(value);
			SelectedFields = [.. AllFields.Where(f => f.IsRequired).Select((f, i) =>
			{
				f.Order = i;
				return f.GetOrClone();
			})];

			NotifyChanged();
		}
	}

	private string _fileName;

	/// <summary>
	/// Full path to CSV file.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FileNameKey,
		Description = LocalizedStrings.FilePathCsvKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 1)]
	[Editor(typeof(IFileBrowserEditor), typeof(IFileBrowserEditor))]
	[BasicSetting]
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
		Name = LocalizedStrings.DataDirectoryKey,
		Description = LocalizedStrings.DataDirectoryKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.CommonKey,
		Order = 2)]
	[Editor(typeof(IFolderBrowserEditor), typeof(IFolderBrowserEditor))]
	[BasicSetting]
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
		GroupName = LocalizedStrings.CommonKey,
		Order = 3)]
	public string FileMask
	{
		get => _fileMask;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

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
		GroupName = LocalizedStrings.CommonKey,
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
		Name = LocalizedStrings.ColumnSeparatorKey,
		Description = LocalizedStrings.ColumnSeparatorDescKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 5)]
	[BasicSetting]
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

	private string _lineSeparator = StringHelper.RN;

	/// <summary>
	/// Line separator.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LineKey,
		Description = LocalizedStrings.LineSeparatorKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 6)]
	[BasicSetting]
	public string LineSeparator
	{
		get => _lineSeparator;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_lineSeparator = value;
			NotifyChanged();
		}
	}

	private int _skipFromHeader;

	/// <summary>
	/// Number of lines to be skipped from the beginning of the file (if they contain meta information).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SkipLinesKey,
		Description = LocalizedStrings.SkipLinesDescKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 6)]
	[BasicSetting]
	public int SkipFromHeader
	{
		get => _skipFromHeader;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

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
		GroupName = LocalizedStrings.CommonKey,
		Order = 7)]
	[BasicSetting]
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
		Name = LocalizedStrings.IntervalKey,
		Description = LocalizedStrings.IntervalDataUpdatesKey,
		GroupName = LocalizedStrings.CommonKey,
		Order = 8)]
	[TimeSpanEditor(Mask = TimeSpanEditorMask.Days | TimeSpanEditorMask.Hours | TimeSpanEditorMask.Minutes)]
	public TimeSpan Interval
	{
		get => _interval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

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

			SelectedFields = [.. SelectedFields.Except(ExtendedFields)];
			AllFields = [.. AllFields.Except(ExtendedFields)];

			_extendedStorage = value;

			//if (_extendedStorage != null)
			//	AllFields = AllFields.Concat(FieldMappingRegistry.CreateExtendedFields(_extendedStorage)).ToArray();

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

	private IEnumerable<FieldMapping> _selectedFields = [];

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
		if (!FileName.IsEmpty())
			return [FileName];

		if (!Directory.IsEmpty())
			return System.IO.Directory.GetFiles(Directory, FileMask, IncludeSubDirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

		throw new InvalidOperationException("No any directory or file was set for import.");
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
			ExtendedStorage = ServicesRegistry.TryExtendedInfoStorage?.Get(extendedStorage);

		SelectedFields = LoadSelectedFields(storage.GetValue<SettingsStorage[]>("Fields") ?? storage.GetValue<SettingsStorage[]>(nameof(SelectedFields)));

		FileName = storage.GetValue<string>(nameof(FileName));
		Directory = storage.GetValue(nameof(Directory), Directory);
		FileMask = storage.GetValue(nameof(FileMask), FileMask);
		IncludeSubDirectories = storage.GetValue(nameof(IncludeSubDirectories), IncludeSubDirectories);
		ColumnSeparator = storage.GetValue(nameof(ColumnSeparator), ColumnSeparator);
		LineSeparator = storage.GetValue(nameof(LineSeparator), LineSeparator);
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
		storage.SetValue(nameof(LineSeparator), LineSeparator);
		storage.SetValue(nameof(SkipFromHeader), SkipFromHeader);
		storage.SetValue(nameof(TimeZone), TimeZone);
		storage.SetValue(nameof(UpdateDuplicateSecurities), UpdateDuplicateSecurities);
		storage.SetValue(nameof(IgnoreNonIdSecurities), IgnoreNonIdSecurities);
		storage.SetValue(nameof(Interval), Interval);
	}

	private IEnumerable<FieldMapping> LoadSelectedFields(IEnumerable<SettingsStorage> storages)
	{
		ArgumentNullException.ThrowIfNull(storages);

		var selectedFields = new List<FieldMapping>();

		foreach (var fieldSettings in storages)
		{
			var fieldName = fieldSettings.GetValue<string>(nameof(FieldMapping.Name));
			var field = AllFields.FirstOrDefault(f => f.Name.EqualsIgnoreCase(fieldName));

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
			return LocalizedStrings.Positions;
		else if (DataType.IsCandles)
			return LocalizedStrings.Candles;
		else if (DataType == DataType.OrderLog)
			return LocalizedStrings.OrderLog;
		else if (DataType == DataType.Ticks)
			return LocalizedStrings.Ticks;
		else if (DataType == DataType.Transactions)
			return LocalizedStrings.Transactions;
		else
			throw new ArgumentOutOfRangeException(nameof(DataType.MessageType), msgType, LocalizedStrings.InvalidValue);
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
		parser.LineSeparator = LineSeparator;
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