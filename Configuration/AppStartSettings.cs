namespace StockSharp.Configuration;

/// <summary>
/// Application start configuration.
/// </summary>
public class AppStartSettings : IPersistable
{
	/// <summary>
	/// Selected application language.
	/// </summary>
	public string Language { get; set; } = LocalizedStrings.ActiveLanguage;

	/// <summary>
	/// Online mode.
	/// </summary>
	public bool Online { get; set; } = true;

	private TimeZoneInfo _timeZone = TimeZoneInfo.Local;

	/// <summary>
	/// Preferred application time zone.
	/// </summary>
	public TimeZoneInfo TimeZone
	{
		get => _timeZone;
		set => _timeZone = value ?? throw new ArgumentNullException(nameof(value));
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		Online = storage.GetValue(nameof(Online), Online);
		Language = storage.GetValue(nameof(Language), Language);

		var tzId = storage.GetValue<string>(nameof(TimeZone));
		if (!tzId.IsEmptyOrWhiteSpace())
		{
			try
			{
				TimeZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);
			}
			catch
			{
				// ignore invalid/unknown tz on current OS
			}
		}
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Language), Language)
			.Set(nameof(Online), Online)
			.Set(nameof(TimeZone), TimeZone.Id)
		;
	}

	/// <summary>
	/// Try load settings, if config file exists.
	/// </summary>
	public static AppStartSettings TryLoad()
	{
		var configFile = Paths.PlatformConfigurationFile;

		if (configFile.IsEmptyOrWhiteSpace() || !configFile.IsConfigExists())
			return null;

		return configFile.Deserialize<SettingsStorage>()?.Load<AppStartSettings>();
	}

	/// <summary>
	/// Save settings into <see cref="Paths.PlatformConfigurationFile"/> if it is defined.
	/// </summary>
	public void TrySave()
	{
		var configFile = Paths.PlatformConfigurationFile;
		if (configFile.IsEmptyOrWhiteSpace())
			return;

		configFile.CreateDirIfNotExists();
		this.Save().Serialize(configFile);
	}
}
