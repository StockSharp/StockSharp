namespace StockSharp.Configuration;

/// <summary>
/// Application start configuration.
/// </summary>
public class AppStartSettings : IAsyncPersistable
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

	Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
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

		return Task.CompletedTask;
	}

	Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage
			.Set(nameof(Language), Language)
			.Set(nameof(Online), Online)
			.Set(nameof(TimeZone), TimeZone.Id)
		;

		return Task.CompletedTask;
	}

	/// <summary>
	/// Try load settings, if config file exists.
	/// </summary>
	[Obsolete("Blocking sync-over-async wrapper. Use TryLoadAsync instead.")]
	public static AppStartSettings TryLoad(IFileSystem fileSystem)
		=> AsyncHelper.Run(() => TryLoadAsync(fileSystem, default));

	/// <summary>
	/// Try load settings, if config file exists.
	/// </summary>
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The settings, or <see langword="null"/> when there are none.</returns>
	public static async ValueTask<AppStartSettings> TryLoadAsync(IFileSystem fileSystem, CancellationToken cancellationToken)
	{
		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		var configFile = Paths.PlatformConfigurationFile;

		if (configFile.IsEmptyOrWhiteSpace() || !configFile.IsConfigExists(fileSystem))
			return null;

		var storage = await configFile.DeserializeAsync<SettingsStorage>(fileSystem, cancellationToken);

		return storage is null ? null : await storage.LoadAsync<AppStartSettings>(cancellationToken);
	}

	/// <summary>
	/// Save settings into <see cref="Paths.PlatformConfigurationFile"/> if it is defined.
	/// </summary>
	[Obsolete("Blocking sync-over-async wrapper. Use TrySaveAsync instead.")]
	public void TrySave(IFileSystem fileSystem)
		=> AsyncHelper.Run(() => TrySaveAsync(fileSystem, default));

	/// <summary>
	/// Save settings into <see cref="Paths.PlatformConfigurationFile"/>.
	/// </summary>
	/// <param name="fileSystem"><see cref="IFileSystem"/></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public async ValueTask TrySaveAsync(IFileSystem fileSystem, CancellationToken cancellationToken)
	{
		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		var configFile = Paths.PlatformConfigurationFile;
		if (configFile.IsEmptyOrWhiteSpace())
			return;

		fileSystem.CreateDirIfNotExists(configFile);
		await (await this.SaveAsync(cancellationToken)).SerializeAsync(fileSystem, configFile, true, cancellationToken);
	}
}
