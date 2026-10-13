namespace StockSharp.Configuration;

using System.Text;

using Newtonsoft.Json;

#if NET10_0_OR_GREATER
using NuGet.Configuration;
#endif

/// <summary>
/// System paths.
/// </summary>
public static class Paths
{
	static Paths()
	{
		var companyPath = PathsHolder.CompanyPath ?? ConfigManager.TryGet<string>("companyPath");

		// ToFullPathIfNeed expands the %Documents% variable; despite the name it does not make a
		// path absolute. It can also expand to nothing, because GetFolderPath answers with an empty
		// string when the platform cannot name the folder - a container with no HOME, which is what
		// a CI runner usually is. The same emptiness reaches the default below as a bare
		// "StockSharp". Every other path is built from this one, so it is settled against the
		// current directory here rather than letting a relative name travel: the data would
		// otherwise land wherever the process happened to start.
		var configured = companyPath.IsEmpty() ? null : companyPath.ToFullPathIfNeed();

		CompanyPath = Path.GetFullPath(configured.IsEmpty()
			? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockSharp")
			: configured);

		CredentialsFile = Path.Combine(CompanyPath, $"credentials{DefaultSettingsExt}");

		AppName = ConfigManager.TryGet("appName", TypeHelper.ApplicationName);
		AppDataPath = GetAppDataPath(PathsHolder.AppDataPath ?? ConfigManager.TryGet<string>("settingsPath"), AppName2);

		PlatformConfigurationFile = Path.Combine(AppDataPath, $"platform_config{DefaultSettingsExt}");
		ProxyConfigurationFile = Path.Combine(CompanyPath, $"proxy_config{DefaultSettingsExt}");
		SecurityNativeIdDir = Path.Combine(AppDataPath, "NativeId");
		SecurityMappingDir = Path.Combine(AppDataPath, "Symbol mapping");
		SecurityExtendedInfo = Path.Combine(AppDataPath, "Extended info");
		StorageDir = Path.Combine(AppDataPath, "Storage");
		SnapshotsDir = Path.Combine(AppDataPath, "Snapshots");
		CandlePatternsFile = Path.Combine(AppDataPath, $"candle_patterns{DefaultSettingsExt}");
		CompilerCacheDir = Path.Combine(AppDataPath, "compiler_cache");
		LogsDir = Path.Combine(AppDataPath, "Logs");
		ReportLogsPath = Path.Combine(AppDataPath, "BugReports");
		PythonUtilsPath = Path.Combine(CompanyPath, "Python");
		InstallerDir = Path.Combine(CompanyPath, "Installer");
		InstallerInstallationsConfigPath = Path.Combine(InstallerDir, $"installer_apps_installed{DefaultSettingsExt}");

		try
		{
			HistoryDataPath = GetHistoryDataPath(GetNuGetGlobalPackagesFolder());
		}
		catch (Exception ex)
		{
			System.Diagnostics.Trace.WriteLine(ex);
		}
	}

	/// <summary>
	/// Get NuGet global packages folder path.
	/// </summary>
	/// <returns>Path to NuGet global packages folder.</returns>
	public static string GetNuGetGlobalPackagesFolder()
	{
#if NET10_0_OR_GREATER
		var settings = Settings.LoadDefaultSettings(null);
		return SettingsUtility.GetGlobalPackagesFolder(settings);
#else
		var nugetPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
		if (!nugetPackages.IsEmpty())
			return nugetPackages;

		return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
#endif
	}

	/// <summary>
	/// Get <see cref="AppDataPath"/>.
	/// </summary>
	/// <param name="appDataPath">Relative <see cref="AppDataPath"/>.</param>
	/// <param name="appName"><see cref="AppName2"/></param>
	/// <returns><see cref="AppDataPath"/></returns>
	public static string GetAppDataPath(string appDataPath, string appName)
		=> appDataPath.IsEmpty()
			? Path.Combine(CompanyPath, appName)
			: appDataPath.ToFullPathIfNeed();

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
	[Obsolete("Blocking sync-over-async wrapper. Use GetAppNameWithVersionAsync instead.")]
	public static string AppNameWithVersion => $"{AppName} v{InstalledVersion}";

	/// <summary>
	/// App title with version.
	/// </summary>
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>App title with version.</returns>
	public static async ValueTask<string> GetAppNameWithVersionAsync(IFileSystem fileSystem, CancellationToken cancellationToken)
		=> $"{AppName} v{await GetInstalledVersionAsync(fileSystem, cancellationToken)}";

	/// <summary>
	/// The path to directory with all applications.
	/// </summary>
	public static readonly string CompanyPath;

	/// <summary>
	/// The path to the file with credentials.
	/// </summary>
	public static readonly string CredentialsFile;

	/// <summary>
	/// The path to the settings directory.
	/// </summary>
	public static readonly string AppDataPath;

	/// <summary>
	/// The path to the Python utils directory.
	/// </summary>
	public static readonly string PythonUtilsPath;

	/// <summary>
	/// The path to the settings directory.
	/// </summary>
	public static readonly string ReportLogsPath;

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
	/// The path to the compiler cache directory.
	/// </summary>
	public static readonly string CompilerCacheDir;

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
	/// Installer UI exe name.
	/// </summary>
	public const string InstallerUIName = "StockSharp.Installer.UI";

	/// <summary>
	/// Installer console exe name.
	/// </summary>
	public const string InstallerConsoleName = "StockSharp.Installer.Console";

	/// <summary>
	/// Setup name.
	/// </summary>
	public const string SetupName = "stocksharp_setup";

	/// <summary>
	/// Get website url.
	/// </summary>
	/// <returns>Localized url.</returns>
	public static string GetWebSiteUrl() => "https://stocksharp.com";

	/// <summary>
	/// Get logo url.
	/// </summary>
	/// <returns>Logo url.</returns>
	public static string GetLogoUrl() => $"{GetWebSiteUrl()}/images/logo.png";

	/// <summary>
	/// Chat on the web site.
	/// </summary>
	public static string Chat => GetSitePageUrl("chat");

	/// <summary>
	/// Languages the web site is published in.
	/// </summary>
	/// <remarks>
	/// The site itself reads them from its domain rows, which nothing outside it can reach; kept in step with
	/// that list by hand. A language missing from here is shown in English rather than 404.
	/// </remarks>
	private static readonly string[] _siteLanguages = ["en", "ru", "zh", "es", "de", "pt", "ja"];

	/// <summary>
	/// Language segment the site expects in its addresses, for the language the application is running in.
	/// </summary>
	public static string SiteLanguage
	{
		get
		{
			var lang = LocalizedStrings.ActiveLanguage;

			return _siteLanguages.Any(l => l.EqualsIgnoreCase(lang)) ? lang.ToLowerInvariant() : LocalizedStrings.EnCode;
		}
	}

	// Every page on the site lives under its language; only files do not, and they are addressed from the root.
	private static string GetSitePageUrl(string path)
		=> $"{GetWebSiteUrl()}/{SiteLanguage}/{path}/";

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
		// A file is served from the root whatever one is reading in: sending it through the language would only
		// bounce back here, so it is the one page id that keeps the bare address.
		var url = id == Pages.File
			? GetWebSiteUrl() + "/"
			: $"{GetWebSiteUrl()}/{SiteLanguage}/";

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
			_ => throw new ArgumentOutOfRangeException(nameof(id), id, LocalizedStrings.InvalidValue),
		};

		url += "/";

		if (urlPart is not null)
			url += $"{urlPart}/";

		return url;
	}

	/// <summary>
	/// Try to build documentation URL.
	/// </summary>
	/// <param name="urlPart">URL part.</param>
	/// <returns>Absolute URL.</returns>
	public static string TryBuildDocUrl(this string urlPart)
	{
		if (!urlPart.StartsWithIgnoreCase("http"))
			urlPart = GetDocUrl(urlPart);

		return urlPart;
	}

	/// <summary>
	/// To create localized url.
	/// </summary>
	/// <param name="docUrl">Help topic.</param>
	/// <returns>Localized url.</returns>
	public static string GetDocUrl(string docUrl)
	{
		var path = docUrl?.TrimStart('/') ?? string.Empty;

		// Some paths arrive with the language already on them - the server stores its topic links that way - and
		// prefixing those again asks for "/ru/ru/topics/...", which is nowhere.
		var prefix = HasLanguagePrefix(path) ? string.Empty : $"{SiteLanguage}/";

		return $"https://doc.stocksharp.com/{prefix}{path}";
	}

	private static bool HasLanguagePrefix(string path)
	{
		var slash = path.IndexOf('/');

		if (slash < 0)
			return false;

		var head = path[..slash];

		return _siteLanguages.Any(l => l.EqualsIgnoreCase(head));
	}

	/// <summary>
	/// Entry assembly.
	/// </summary>
	public static Assembly EntryAssembly => Assembly.GetEntryAssembly();

	private static string _installedVersion;

	private static string AssemblyVersion => EntryAssembly?.GetName().Version.To<string>();

	/// <summary>
	/// Installed version of the product.
	/// </summary>
	[Obsolete("Blocking sync-over-async wrapper. Use GetInstalledVersionAsync instead.")]
	public static string InstalledVersion
		=> _installedVersion ?? AsyncHelper.Run(() => GetInstalledVersionAsync(FileSystem, default));

	/// <summary>
	/// Installed version of the product.
	/// </summary>
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The installed version.</returns>
	public static async ValueTask<string> GetInstalledVersionAsync(IFileSystem fileSystem, CancellationToken cancellationToken)
	{
		if (_installedVersion != null)
			return _installedVersion;

		try
		{
			_installedVersion = (await TryGetInstalledVersionAsync(Directory.GetCurrentDirectory(), fileSystem, cancellationToken))
				.IsEmpty(ConfigManager.TryGet("actualVersion", "5.0.0").IsEmpty(AssemblyVersion));
		}
		catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
		{
			_installedVersion = AssemblyVersion;
		}

		return _installedVersion;
	}

	/// <summary>
	/// Reset installed version cache so it would be re-generated next time when it's requested.
	/// </summary>
	public static void ResetInstalledVersionCache() => _installedVersion = null;

	/// <summary>
	/// Build info version.
	/// </summary>
	public static string BuildVersion
		=> EntryAssembly?.GetAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

	private static async ValueTask<SettingsStorage[]> GetInstallationsAsync(IFileSystem fileSystem, CancellationToken cancellationToken)
	{
		if (!InstallerInstallationsConfigPath.IsConfigExists(fileSystem))
			return null;

		var storage = await InstallerInstallationsConfigPath.DeserializeInvariantAsync(fileSystem, cancellationToken);

		if (storage is null)
			return null;

		var installations = storage?.GetValue<SettingsStorage[]>("Installations");
		if (!(installations?.Length > 0))
			return null;

		return installations;
	}

	/// <summary>
	/// Try get installed path by product id.
	/// </summary>
	/// <param name="productId">Identifier.</param>
	/// <param name="fileSystem">File system.</param>
	/// <returns>Installed path.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use TryGetInstalledPathAsync instead.")]
	public static string TryGetInstalledPath(long productId, IFileSystem fileSystem)
		=> AsyncHelper.Run(() => TryGetInstalledPathAsync(productId, fileSystem, default));

	/// <summary>
	/// Try get installed path by product id.
	/// </summary>
	/// <param name="productId">Identifier.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Installed path.</returns>
	public static async ValueTask<string> TryGetInstalledPathAsync(long productId, IFileSystem fileSystem, CancellationToken cancellationToken)
	{
		var installations = await GetInstallationsAsync(fileSystem, cancellationToken);
		if (installations == null)
			return null;

		var installation = installations.FirstOrDefault(ss => productId == ss.TryGet<long>("ProductKey"));
		if (installation == null)
			return null;

		return installation.TryGet<string>("InstallDirectory");
	}

	/// <summary>
	/// Get currently installed version of the product.
	/// </summary>
	/// <param name="productInstallPath">File system path to product installation.</param>
	/// <param name="fileSystem">File system.</param>
	/// <returns>Installed version of the product.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use TryGetInstalledVersionAsync instead.")]
	public static string TryGetInstalledVersion(string productInstallPath, IFileSystem fileSystem)
		=> AsyncHelper.Run(() => TryGetInstalledVersionAsync(productInstallPath, fileSystem, default));

	/// <summary>
	/// Get currently installed version of the product.
	/// </summary>
	/// <param name="productInstallPath">File system path to product installation.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Installed version of the product.</returns>
	public static async ValueTask<string> TryGetInstalledVersionAsync(string productInstallPath, IFileSystem fileSystem, CancellationToken cancellationToken)
	{
		if (productInstallPath.IsEmpty())
			throw new ArgumentException(nameof(productInstallPath));

		var installations = await GetInstallationsAsync(fileSystem, cancellationToken);
		if (installations == null)
			return null;

		var installation = installations.FirstOrDefault(ss => productInstallPath.ComparePaths(ss.TryGet<string>("InstallDirectory")));
		if (installation == null)
			return null;

		var version = installation.GetValue<object>("Version");

		if (version is string str)
			return str;

		var identityStr = ((SettingsStorage)version)?
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
	/// Sample history data security.
	/// </summary>
	public const string HistoryDefaultSecurity = "BTCUSDT@BNBFT";

	/// <summary>
	/// Sample history data security.
	/// </summary>
	public const string HistoryDefaultSecurity2 = "TONUSDT@BNBFT";

	/// <summary>
	/// Begin date of <see cref="HistoryDataPath"/>.
	/// </summary>
	public static readonly DateTime HistoryBeginDate = new DateTime(2024, 3, 1).UtcKind();

	/// <summary>
	/// End date of <see cref="HistoryDataPath"/>.
	/// </summary>
	public static readonly DateTime HistoryEndDate = new DateTime(2024, 3, 31).UtcKind();

	/// <summary>
	/// Birthday.
	/// </summary>
	public static readonly DateTime Birthday = new DateTime(1977, 5, 24).UtcKind();

	/// <summary>
	/// Default extension for settings file.
	/// </summary>
	public const string DefaultSettingsExt = FileExts.Json;

	/// <summary>
	/// Returns an files with <see cref="DefaultSettingsExt"/> extension.
	/// </summary>
	/// <param name="path">The relative or absolute path to the directory to search.</param>
	/// <param name="filter">The search string to match against the names of files in path.</param>
	/// <returns>Files.</returns>
	[Obsolete("Use IFileSystem overload.")]
	public static IEnumerable<string> EnumerateConfigs(this string path, string filter = "*")
		=> EnumerateConfigs(path, FileSystem, filter);

	/// <summary>
	/// Returns an files with <see cref="DefaultSettingsExt"/> extension.
	/// </summary>
	/// <param name="path">The relative or absolute path to the directory to search.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="filter">The search string to match against the names of files in path.</param>
	/// <returns>Files.</returns>
	public static IEnumerable<string> EnumerateConfigs(this string path, IFileSystem fileSystem, string filter = "*")
		=> fileSystem.CheckOnNull(nameof(fileSystem)).EnumerateFiles(path, $"{filter}{DefaultSettingsExt}");

	/// <summary>
	/// Make the specified <paramref name="filePath"/> with <see cref="FileExts.Backup"/> extension.
	/// </summary>
	/// <param name="filePath">File path.</param>
	/// <returns>File path.</returns>
	public static string MakeBackup(this string filePath)
		=> $"{filePath}{FileExts.Backup}";

	/// <summary>
	/// Rename the specified file with <see cref="FileExts.Backup"/> extension.
	/// </summary>
	/// <param name="filePath">File path.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="backupFilePath">Backup file path.</param>
	public static void MoveToBackup(this string filePath, IFileSystem fileSystem, string backupFilePath = null)
	{
		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		var target = backupFilePath ?? filePath;
		var bak = target.MakeBackup();
		var idx = 0;
		do
		{
			if(!fileSystem.FileExists(bak))
				break;

			bak = (target + $".{++idx}").MakeBackup();
		} while(true);

		fileSystem.MoveFile(filePath, bak);
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
	/// Default file system.
	/// </summary>
	public static readonly IFileSystem FileSystem = Messages.Extensions.DefaultFileSystem;

	/// <summary>
	/// Serialize <paramref name="value"/> state into <see cref="string"/> value.
	/// </summary>
	/// <typeparam name="T">Type of <paramref name="value"/>.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="bom">Serializer adds UTF8 BOM preamble.</param>
	/// <returns><see cref="string"/> value.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use SerializeToStringAsync instead.")]
	public static string SerializeToString<T>(this T value, bool bom = true)
		=> AsyncHelper.Run(() => value.SerializeToStringAsync(bom, default));

	/// <summary>
	/// Serialize <paramref name="value"/> state into <see cref="string"/> value.
	/// </summary>
	/// <typeparam name="T">Type of <paramref name="value"/>.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="bom">Serializer adds UTF8 BOM preamble.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="string"/> value.</returns>
	public static async ValueTask<string> SerializeToStringAsync<T>(this T value, bool bom, CancellationToken cancellationToken)
		=> (await value.SerializeAsync(bom, cancellationToken)).UTF8();

	/// <summary>
	/// Deserialize <paramref name="value"/> state from <paramref name="str"/>.
	/// </summary>
	/// <typeparam name="T">Type of <paramref name="value"/>.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="str"><see cref="string"/> value.</param>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeFromStringAsync instead.")]
	public static void DeserializeFromString<T>(this T value, string str)
		where T : IAsyncPersistable
		=> AsyncHelper.Run(() => value.DeserializeFromStringAsync(str, default));

	/// <summary>
	/// Deserialize <paramref name="value"/> state from <paramref name="str"/>.
	/// </summary>
	/// <typeparam name="T">Type of <paramref name="value"/>.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="str"><see cref="string"/> value.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public static async ValueTask DeserializeFromStringAsync<T>(this T value, string str, CancellationToken cancellationToken)
		where T : IAsyncPersistable
		=> await value.LoadAsync(await str.UTF8().DeserializeAsync<SettingsStorage>(cancellationToken), cancellationToken);

	/// <summary>
	/// Serialize value into the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="filePath">File path.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	[Obsolete("Use IFileSystem overload.")]
	public static void Serialize<T>(this T value, string filePath, bool bom = true)
		=> Serialize(value, FileSystem, filePath, bom);

	/// <summary>
	/// Serialize value into byte array.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <returns>Serialized data.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use SerializeAsync instead.")]
	public static byte[] Serialize<T>(this T value, bool bom = true)
		=> AsyncHelper.Run(() => value.SerializeAsync(bom, default));

	/// <summary>
	/// Serialize value into byte array.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Serialized data.</returns>
	public static async ValueTask<byte[]> SerializeAsync<T>(this T value, bool bom, CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream();
		await CreateSerializer<T>(bom).SerializeAsync(value, stream, cancellationToken);
		return stream.ToArray();
	}

	/// <summary>
	/// Deserialize value from the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="filePath">File path.</param>
	/// <returns>Value.</returns>
	[Obsolete("Use IFileSystem overload.")]
	public static T Deserialize<T>(this string filePath)
		=> Deserialize<T>(filePath, FileSystem);

	/// <summary>
	/// Deserialize value from the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="filePath">File path.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Value.</returns>
	[Obsolete("Use IFileSystem overload.")]
	public static ValueTask<T> DeserializeAsync<T>(this string filePath, CancellationToken cancellationToken)
		=> DeserializeAsync<T>(filePath, FileSystem, cancellationToken);

	/// <summary>
	/// Deserialize value from the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="filePath">File path.</param>
	/// <returns>Value.</returns>
	[Obsolete("Use IFileSystem overload.")]
	public static T DeserializeOrThrow<T>(this string filePath)
		=> DeserializeOrThrow<T>(filePath, FileSystem);

	/// <summary>
	/// Deserialize value from the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="filePath">File path.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Value.</returns>
	[Obsolete("Use IFileSystem overload.")]
	public static ValueTask<T> DeserializeOrThrowAsync<T>(this string filePath, CancellationToken cancellationToken)
		=> DeserializeOrThrowAsync<T>(filePath, FileSystem, cancellationToken);

	/// <summary>
	/// Deserialize value from the serialized data.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="data">Serialized data.</param>
	/// <returns>Value.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeAsync instead.")]
	public static T Deserialize<T>(this byte[] data)
		=> AsyncHelper.Run(() => data.DeserializeAsync<T>(default));

	/// <summary>
	/// Deserialize value from the serialized data.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="data">Serialized data.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Value.</returns>
	public static async ValueTask<T> DeserializeAsync<T>(this byte[] data, CancellationToken cancellationToken)
	{
		using var stream = new MemoryStream(data);
		return await stream.DeserializeAsync<T>(cancellationToken);
	}

	/// <summary>
	/// Deserialize value from the serialized data.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="data">Serialized data.</param>
	/// <returns>Value.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeAsync instead.")]
	public static T Deserialize<T>(this Stream data)
		=> AsyncHelper.Run(() => data.DeserializeAsync<T>(default));

	/// <summary>
	/// Deserialize value from the serialized data.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="data">Serialized data.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Value, or the default when the data cannot be read; the failure is logged.</returns>
	public static async ValueTask<T> DeserializeAsync<T>(this Stream data, CancellationToken cancellationToken)
	{
		var serializer = CreateSerializer<T>();

		try
		{
			return await serializer.DeserializeAsync(data, cancellationToken);
		}
		catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
		{
			e.LogError();
		}

		return default;
	}

	/// <summary>
	/// Serialize value into the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="filePath">File path.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	[Obsolete("Blocking sync-over-async wrapper. Use SerializeAsync instead.")]
	public static void Serialize<T>(this T value, IFileSystem fileSystem, string filePath, bool bom = true)
		=> AsyncHelper.Run(() => value.SerializeAsync(fileSystem, filePath, bom, default));

	/// <summary>
	/// Serialize value into the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="value">Value.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="filePath">File path.</param>
	/// <param name="bom">Add UTF8 BOM preamble.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public static async ValueTask SerializeAsync<T>(this T value, IFileSystem fileSystem, string filePath, bool bom, CancellationToken cancellationToken)
	{
		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		if (filePath.IsEmpty())
			throw new ArgumentNullException(nameof(filePath));

		var bytes = await value.SerializeAsync(bom, cancellationToken);

		// OpenWrite (like File.Open) does not create missing parent folders, so create the directory.
		var dir = Path.GetDirectoryName(filePath);
		if (!dir.IsEmpty() && !fileSystem.DirectoryExists(dir))
			fileSystem.CreateDirectory(dir);

		cancellationToken.ThrowIfCancellationRequested();

		// Opening the file empties it, so the write that follows is not cancelled: it would leave the file empty.
		await using var stream = fileSystem.OpenWrite(filePath);
		await stream.WriteAsync(bytes, CancellationToken.None);
	}

	/// <summary>
	/// Deserialize value from the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="filePath">File path.</param>
	/// <param name="fileSystem">File system.</param>
	/// <returns>Value.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeAsync instead.")]
	public static T Deserialize<T>(this string filePath, IFileSystem fileSystem)
		=> AsyncHelper.Run(() => filePath.DeserializeAsync<T>(fileSystem, default));

	/// <summary>
	/// Deserialize value from the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="filePath">File path.</param>
	/// <param name="fileSystem">File system.</param>
	/// <returns>Value.</returns>
	[Obsolete("Blocking sync-over-async wrapper. Use DeserializeOrThrowAsync instead.")]
	public static T DeserializeOrThrow<T>(this string filePath, IFileSystem fileSystem)
		=> AsyncHelper.Run(() => filePath.DeserializeOrThrowAsync<T>(fileSystem, default));

	/// <summary>
	/// Deserialize value from the specified file.
	/// </summary>
	/// <typeparam name="T">Value type.</typeparam>
	/// <param name="filePath">File path.</param>
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Value.</returns>
	public static async ValueTask<T> DeserializeAsync<T>(this string filePath, IFileSystem fileSystem, CancellationToken cancellationToken)
	{
		if (filePath.IsEmpty())
			throw new ArgumentNullException(nameof(filePath));

		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		try
		{
			return await filePath.DeserializeOrThrowAsync<T>(fileSystem, cancellationToken);
		}
		catch (Exception e) when (e is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
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
	/// <param name="fileSystem">File system.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Value.</returns>
	public static async ValueTask<T> DeserializeOrThrowAsync<T>(this string filePath, IFileSystem fileSystem, CancellationToken cancellationToken)
	{
		if (filePath.IsEmpty())
			throw new ArgumentNullException(nameof(filePath));

		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		var defFile = Path.ChangeExtension(filePath, DefaultSettingsExt);

		if (!fileSystem.FileExists(defFile))
			throw new FileNotFoundException($"file not found: '{defFile}'");

		using var stream = fileSystem.OpenRead(defFile);
		return await CreateSerializer<T>().DeserializeAsync(stream, cancellationToken);
	}

	/// <summary>
	/// Determines the specified config file exists.
	/// </summary>
	/// <param name="configFile">Config file.</param>
	/// <param name="fileSystem">File system.</param>
	/// <returns>Check result.</returns>
	public static bool IsConfigExists(this string configFile, IFileSystem fileSystem)
	{
		if (fileSystem is null)
			throw new ArgumentNullException(nameof(fileSystem));

		return fileSystem.FileExists(configFile);
	}

	/// <summary>
	/// Determines the specified config file exists.
	/// </summary>
	/// <param name="configFile">Config file.</param>
	/// <returns>Check result.</returns>
	[Obsolete("Use IFileSystem overload.")]
	public static bool IsConfigExists(this string configFile)
		=> IsConfigExists(configFile, FileSystem);

	/// <summary>
	/// Get file name without extension for the specified id.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <param name="by">Replacing character.</param>
	/// <returns>File name without extension.</returns>
	public static string GetFileNameWithoutExtension(this Guid id, char? by = '_')
	{
		var s = id.ToString();

		return by is null ? s.Remove("-") : s.Replace('-', by.Value);
	}

	/// <summary>
	/// Get file name for the specified id.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <returns>File name.</returns>
	public static string GetFileName(this Guid id)
		=> $"{id.GetFileNameWithoutExtension()}{DefaultSettingsExt}";

	/// <summary>
	/// <see cref="ServerCredentials.Email"/> in case <see cref="ServerCredentials.Token"/>.
	/// </summary>
	public const string TokenBasedEmail = "x";
}
