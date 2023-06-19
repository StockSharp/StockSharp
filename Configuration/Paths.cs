namespace StockSharp.Configuration
{
	using System;
	using System.Text;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Localization;

	using Newtonsoft.Json;

	using NuGet.Configuration;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// System paths.
	/// </summary>
	public static class Paths
	{
		static Paths()
		{
			var companyPath = PathsHolder.CompanyPath ?? ConfigManager.TryGet<string>("companyPath");
			CompanyPath = companyPath.IsEmpty() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockSharp") : companyPath.ToFullPathIfNeed();

			AppName = ConfigManager.TryGet("appName", TypeHelper.ApplicationName);

			var settingsPath = PathsHolder.AppDataPath ?? ConfigManager.TryGet<string>("settingsPath");
			AppDataPath = settingsPath.IsEmpty() ? Path.Combine(CompanyPath, AppName2) : settingsPath.ToFullPathIfNeed();

			PlatformConfigurationFile = Path.Combine(AppDataPath, $"platform_config{DefaultSettingsExt}");
			ProxyConfigurationFile = Path.Combine(CompanyPath, $"proxy_config{DefaultSettingsExt}");
			SecurityNativeIdDir = Path.Combine(AppDataPath, "NativeId");
			SecurityMappingDir = Path.Combine(AppDataPath, "Symbol mapping");
			SecurityExtendedInfo = Path.Combine(AppDataPath, "Extended info");
			StorageDir = Path.Combine(AppDataPath, "Storage");
			SnapshotsDir = Path.Combine(AppDataPath, "Snapshots");
			CandlePatternsFile = Path.Combine(AppDataPath, $"candle_patterns{DefaultSettingsExt}");
			LogsDir = Path.Combine(AppDataPath, "Logs");
			InstallerDir = Path.Combine(CompanyPath, "Installer");
			InstallerInstallationsConfigPath = Path.Combine(InstallerDir, $"installer_apps_installed{DefaultSettingsExt}");

			var settings = Settings.LoadDefaultSettings(null);
			HistoryDataPath = GetHistoryDataPath(SettingsUtility.GetGlobalPackagesFolder(settings));
		}

		/// <summary>
		/// Get history data path.
		/// </summary>
		/// <param name="startDir">Directory.</param>
		/// <returns>History data path.</returns>
		public static string GetHistoryDataPath(string startDir)
		{
			static DirectoryInfo FindHistoryDataSubfolder(DirectoryInfo packageRoot)
			{
				if (!packageRoot.Exists)
					return null;

				foreach (var di in packageRoot.GetDirectories().OrderByDescending(di => di.Name))
				{
					var d = new DirectoryInfo(Path.Combine(di.FullName, "HistoryData"));

					if (d.Exists)
						return d;
				}

				return null;
			}

			var dir = new DirectoryInfo(Path.GetDirectoryName(startDir));

			while (dir != null)
			{
				var hdRoot = FindHistoryDataSubfolder(new DirectoryInfo(Path.Combine(dir.FullName, "packages", "stocksharp.samples.historydata")));
				if (hdRoot != null)
					return hdRoot.FullName;

				dir = dir.Parent;
			}

			return null;
		}

		/// <summary>
		/// App title.
		/// </summary>
		public static readonly string AppName;

		/// <summary>
		///
		/// </summary>
		public static string AppName2 => AppName.Remove("S#.", true);

		/// <summary>
		/// App title with version.
		/// </summary>
		public static string AppNameWithVersion => $"{AppName} v{InstalledVersion}";

		/// <summary>
		/// The path to directory with all applications.
		/// </summary>
		public static readonly string CompanyPath;

		/// <summary>
		/// The path to the settings directory.
		/// </summary>
		public static readonly string AppDataPath;

		/// <summary>
		/// The path to the configuration file of platform definition.
		/// </summary>
		public static readonly string PlatformConfigurationFile;

		/// <summary>
		/// The path to the configuration file of proxy settings.
		/// </summary>
		public static readonly string ProxyConfigurationFile;

		/// <summary>
		/// The path to the directory with native security identifiers.
		/// </summary>
		public static readonly string SecurityNativeIdDir;

		/// <summary>
		/// The path to the directory with securities id mapping.
		/// </summary>
		public static readonly string SecurityMappingDir;

		/// <summary>
		/// The path to the directory with securities extended info.
		/// </summary>
		public static readonly string SecurityExtendedInfo;

		/// <summary>
		/// The path to the directory with market data.
		/// </summary>
		public static readonly string StorageDir;

		/// <summary>
		/// The path to the directory with snapshots of market data.
		/// </summary>
		public static readonly string SnapshotsDir;

		/// <summary>
		/// The path to the file with candle patterns.
		/// </summary>
		public static readonly string CandlePatternsFile;

		/// <summary>
		/// The path to the logs directory.
		/// </summary>
		public static readonly string LogsDir;

		/// <summary>
		/// The path to the installer directory.
		/// </summary>
		public static readonly string InstallerDir;

		/// <summary>
		/// The path to the installer directory.
		/// </summary>
		public static readonly string InstallerInstallationsConfigPath;

		/// <summary>
		/// Web site domain.
		/// </summary>
		public static string Domain => LocalizedStrings.ActiveLanguage == LangCodes.Ru ? "ru" : "com";

		/// <summary>
		/// Get website url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetWebSiteUrl() => $"https://stocksharp.{Domain}";

		/// <summary>
		/// Get logo url.
		/// </summary>
		/// <returns>Logo url.</returns>
		public static string GetLogoUrl() => $"{GetWebSiteUrl()}/images/logo.png";

		/// <summary>
		/// Chat in Telegram.
		/// </summary>
		public static string Chat
		{
			get
			{
				var channelId = LocalizedStrings.ActiveLanguage == LangCodes.Ru ? 1 : 361;
				return $"https://t.me/stocksharpchat/{channelId}";
			}
		}

		/// <summary>
		/// Bot in Telegram.
		/// </summary>
		public static string Bot => $"https://t.me/stocksharpbot";

		/// <summary>
		/// </summary>
		public static class Pages
		{
			/// <summary>
			/// </summary>
			public const long Eula = 274;
			/// <summary>
			/// </summary>
			public const long Pricing = 157;
			/// <summary>
			/// </summary>
			public const long NugetManual = 241;
			/// <summary>
			/// </summary>
			public const long Message = 278;
			/// <summary>
			/// </summary>
			public const long Topic = 275;
			/// <summary>
			/// </summary>
			public const long File = 276;
			/// <summary>
			/// </summary>
			public const long Users = 246;
			/// <summary>
			/// </summary>
			public const long Register = 252;
			/// <summary>
			/// </summary>
			public const long Forgot = 253;
			/// <summary>
			/// </summary>
			public const long Faq = 239;
			/// <summary>
			/// </summary>
			public const long Store = 164;
			/// <summary>
			/// </summary>
			public const long Login = 251;
			/// <summary>
			/// </summary>
			public const long Profile = 243;
		}

		/// <summary>
		/// Get page url.
		/// </summary>
		/// <param name="id">Page id.</param>
		/// <param name="urlPart">Url part (topic id, file name etc.).</param>
		/// <returns>Localized url.</returns>
		public static string GetPageUrl(long id, object urlPart = default)
		{
			var url = GetWebSiteUrl() + "/";

			url += id switch
			{
				Pages.Eula => "products/eula",
				Pages.Pricing => "pricing",
				Pages.NugetManual => "products/nuget_manual",
				Pages.Message => "posts/m",
				Pages.Topic => "topic",
				Pages.File => "file",
				Pages.Users => "users",
				Pages.Register => "register",
				Pages.Forgot => "forgot",
				Pages.Faq => "store/faq",
				Pages.Store => "store",
				Pages.Login => "login",
				Pages.Profile => "profile",
				_ => throw new ArgumentOutOfRangeException(nameof(id), id, LocalizedStrings.Str1219),
			};

			url += "/";

			if (urlPart is not null)
				url += $"{urlPart}/";

			return url;
		}

		/// <summary>
		/// To create localized url.
		/// </summary>
		/// <param name="docUrl">Help topic.</param>
		/// <returns>Localized url.</returns>
		public static string GetDocUrl(string docUrl) => $"https://doc.stocksharp.{Domain}/{docUrl}";

		private static string _installedVersion;

		/// <summary>
		/// Installed version of the product.
		/// </summary>
		public static string InstalledVersion
		{
			get
			{
				if (_installedVersion != null)
					return _installedVersion;

				static string GetAssemblyVersion() => (Assembly.GetEntryAssembly() ?? typeof(Paths).Assembly).GetName().Version.To<string>();

				try
				{
					_installedVersion = GetInstalledVersion(Directory.GetCurrentDirectory()) ?? GetAssemblyVersion();
				}
				catch
				{
					_installedVersion = GetAssemblyVersion();
				}

				_installedVersion ??= "<error>";

				return _installedVersion;
			}
		}

		/// <summary>
		/// Reset installed version cache so it would be re-generated next time when it's requested.
		/// </summary>
		public static void ResetInstalledVersionCache() => _installedVersion = null;

		/// <summary>
		/// Get currently installed version of the product.
		/// </summary>
		/// <param name="productInstallPath">File system path to product installation.</param>
		/// <returns>Installed version of the product.</returns>
		public static string GetInstalledVersion(string productInstallPath)
		{
			if (productInstallPath.IsEmpty())
				throw new ArgumentException(nameof(productInstallPath));

			if (!InstallerInstallationsConfigPath.IsConfigExists())
				return null;

			var storage = Do.Invariant(() =>
			{
				lock(InstallerInstallationsConfigPath)
					return InstallerInstallationsConfigPath.Deserialize<SettingsStorage>();
			});

			if (storage is null)
				return null;

			var installations = storage?.GetValue<SettingsStorage[]>("Installations");
			if (!(installations?.Length > 0))
				return null;

			var installation = installations.FirstOrDefault(ss => productInstallPath.ComparePaths(ss.TryGet<string>("InstallDirectory")));
			if(installation == null)
				return null;

			var identityStr = installation
				.TryGet<SettingsStorage>("Version")?
				.TryGet<SettingsStorage>("Metadata")?
				.TryGet<string>("Identity");

			if (identityStr.IsEmpty())
				return null;

			// ReSharper disable once PossibleNullReferenceException
			var parts = identityStr.Split('|');

			return parts.Length != 2 ? null : parts[1];
		}

		/// <summary>
		/// Sample history data.
		/// </summary>
		public static readonly string HistoryDataPath;

		/// <summary>
		/// Begin date of <see cref="HistoryDataPath"/>.
		/// </summary>
		public static readonly DateTime HistoryBeginDate = new(2020, 4, 1);

		/// <summary>
		/// End date of <see cref="HistoryDataPath"/>.
		/// </summary>
		public static readonly DateTime HistoryEndDate = new(2020, 4, 30);

		/// <summary>
		/// Default extension for settings file.
		/// </summary>
		public const string DefaultSettingsExt = ".json";

		/// <summary>
		/// Backup extension for settings file.
		/// </summary>
		public const string BackupExt = ".bak";

		/// <summary>
		/// Returns an files with <see cref="DefaultSettingsExt"/> extension.
		/// </summary>
		/// <param name="path">The relative or absolute path to the directory to search.</param>
		/// <param name="filter">The search string to match against the names of files in path.</param>
		/// <returns>Files.</returns>
		public static IEnumerable<string> EnumerateConfigs(this string path, string filter = "*")
			=> Directory.EnumerateFiles(path, $"{filter}{DefaultSettingsExt}");

		/// <summary>
		/// Make the specified <paramref name="filePath"/> with <see cref="BackupExt"/> extension.
		/// </summary>
		/// <param name="filePath">File path.</param>
		/// <returns>File path.</returns>
		public static string MakeBackup(this string filePath)
			=> $"{filePath}{BackupExt}";

		/// <summary>
		/// Rename the specified file with <see cref="BackupExt"/> extension.
		/// </summary>
		/// <param name="filePath">File path.</param>
		/// <param name="backupFilePath">Backup file path.</param>
		public static void MoveToBackup(this string filePath, string backupFilePath = null)
		{
			var target = backupFilePath ?? filePath;
			var bak = target.MakeBackup();
			var idx = 0;
			do
			{
				if(!File.Exists(bak))
					break;

				bak = (target + $".{++idx}").MakeBackup();
			} while(true);

			File.Move(filePath, bak);
		}

		/// <summary>
		/// Create serializer.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="bom">Serializer adds UTF8 BOM preamble.</param>
		/// <returns>Serializer.</returns>
		public static ISerializer<T> CreateSerializer<T>(bool bom = true)
			=> new JsonSerializer<T>
			{
				Indent = true,
				Encoding = bom ? Encoding.UTF8 : JsonHelper.UTF8NoBom,
				EnumAsString = true,
				NullValueHandling = NullValueHandling.Ignore,
			};

		/// <summary>
		/// Create serializer.
		/// </summary>
		/// <param name="type">Value type.</param>
		/// <returns>Serializer.</returns>
		public static ISerializer CreateSerializer(Type type)
			=> CreateSerializer<int>().GetSerializer(type);

		/// <summary>
		/// Serialize value into the specified file.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="value">Value.</param>
		/// <param name="filePath">File path.</param>
		/// <param name="bom">Add UTF8 BOM preamble.</param>
		public static void Serialize<T>(this T value, string filePath, bool bom = true)
			=> CreateSerializer<T>(bom).Serialize(value, filePath);

		/// <summary>
		/// Serialize value into byte array.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="value">Value.</param>
		/// <param name="bom">Add UTF8 BOM preamble.</param>
		/// <returns>Serialized data.</returns>
		public static byte[] Serialize<T>(this T value, bool bom = true)
			=> CreateSerializer<T>(bom).Serialize(value);

		/// <summary>
		///
		/// </summary>
		[Obsolete("Use Deserialize instead.")]
		public static T DeserializeWithMigration<T>(this string filePath)
			=> filePath.Deserialize<T>();

		/// <summary>
		/// Deserialize value from the specified file.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="filePath">File path.</param>
		/// <returns>Value.</returns>
		public static T Deserialize<T>(this string filePath)
		{
			try
			{
				return filePath.DeserializeOrThrow<T>();
			}
			catch (Exception e)
			{
				new Exception($"Error deserializing '{filePath}'", e).LogError();
				return default;
			}
		}

		/// <summary>
		/// Deserialize value from the specified file.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="filePath">File path.</param>
		/// <returns>Value.</returns>
		public static T DeserializeOrThrow<T>(this string filePath)
		{
			var defFile = Path.ChangeExtension(filePath, DefaultSettingsExt);
			var defSer = CreateSerializer<T>();

			if(!File.Exists(defFile))
				throw new FileNotFoundException($"file not found: '{defFile}'");

			return defSer.Deserialize(defFile);
		}

		/// <summary>
		/// Deserialize value from the serialized data.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="data">Serialized data.</param>
		/// <returns>Value.</returns>
		public static T Deserialize<T>(this byte[] data)
		{
			var serializer = CreateSerializer<T>();

			try
			{
				return serializer.Deserialize(data);
			}
			catch(Exception e)
			{
				e.LogError();
			}

			return default;
		}

		/// <summary>
		/// Get file name for the specified id.
		/// </summary>
		/// <param name="id">Identifier.</param>
		/// <returns>File name.</returns>
		public static string GetFileName(this Guid id)
			=> $"{id.ToString().Replace('-', '_')}{DefaultSettingsExt}";

		/// <summary>
		/// Determines the specified config file exists.
		/// </summary>
		/// <param name="configFile">Config file.</param>
		/// <returns>Check result.</returns>
		public static bool IsConfigExists(this string configFile)
			=> File.Exists(configFile);
	}
}
