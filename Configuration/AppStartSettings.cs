namespace StockSharp.Configuration
{
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Application start configuration.
	/// </summary>
	public class AppStartSettings : IPersistable
	{
		/// <summary>
		/// Skip settings window.
		/// </summary>
		public bool AutoStart { get; set; }

		/// <summary>
		/// Selected application language.
		/// </summary>
		public string Language { get; set; }

		/// <summary>
		/// Online mode.
		/// </summary>
		public bool Online { get; set; } = true;

		/// <summary>
		/// Create instance of <see cref="AppStartSettings"/>.
		/// </summary>
		public AppStartSettings()
		{
			Language = LocalizedStrings.ActiveLanguage;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			AutoStart = storage.GetValue<bool>(nameof(AutoStart));
			Online = storage.GetValue(nameof(Online), Online);
			Language = storage.GetValue<string>(nameof(Language));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage
				.Set(nameof(AutoStart), AutoStart)
				.Set(nameof(Language), Language)
				.Set(nameof(Online), Online);
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
}
