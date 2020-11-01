namespace StockSharp.Configuration
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Globalization;
	using System.Threading;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// System paths.
	/// </summary>
	public static class Paths
	{
		static Paths()
		{
			var companyPath = ConfigManager.TryGet<string>("companyPath");
			CompanyPath = companyPath.IsEmpty() ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockSharp") : companyPath.ToFullPathIfNeed();

			AppName = ConfigManager.TryGet("appName", TypeHelper.ApplicationName);

			var settingsPath = ConfigManager.TryGet<string>("settingsPath");
			AppDataPath = settingsPath.IsEmpty() ? Path.Combine(CompanyPath, AppName2) : settingsPath.ToFullPathIfNeed();

			PlatformConfigurationFile = Path.Combine(AppDataPath, "platform_config.xml");
			ProxyConfigurationFile = Path.Combine(CompanyPath, "proxy_config.xml");
			SecurityNativeIdDir = Path.Combine(AppDataPath, "NativeId");
			SecurityMappingDir = Path.Combine(AppDataPath, "Symbol mapping");
			SecurityExtendedInfo = Path.Combine(AppDataPath, "Extended info");
			StorageDir = Path.Combine(AppDataPath, "Storage");
			SnapshotsDir = Path.Combine(AppDataPath, "Snapshots");
			InstallerDir = Path.Combine(CompanyPath, "Installer");
			InstallerInstallationsConfigPath = Path.Combine(InstallerDir, "installer_apps_installed.xml");

			HistoryDataPath = GetHistoryDataPath(Assembly.GetExecutingAssembly().Location);
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
		/// The path to the installer directory.
		/// </summary>
		public static readonly string InstallerDir;

		/// <summary>
		/// The path to the installer directory.
		/// </summary>
		public static readonly string InstallerInstallationsConfigPath;

		/// <summary>
		/// Get website url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetWebSiteUrl() => $"https://stocksharp.{LocalizedStrings.Domain}";

		/// <summary>
		/// Get user url.
		/// </summary>
		/// <param name="userId">User id.</param>
		/// <returns>Localized url.</returns>
		public static string GetUserUrl(long userId) => $"{GetWebSiteUrl()}/users/{userId}/";

		/// <summary>
		/// Get strategy url.
		/// </summary>
		/// <param name="robotId">The strategy identifier.</param>
		/// <returns>Localized url.</returns>
		public static string GetRobotLink(long robotId) => $"{GetWebSiteUrl()}/robot/{robotId}/";

		/// <summary>
		/// Get file url.
		/// </summary>
		/// <param name="fileId">File ID.</param>
		/// <returns>Localized url.</returns>
		public static string GetFileLink(object fileId) => $"{GetWebSiteUrl()}/file/{fileId}/";

		/// <summary>
		/// To create localized url.
		/// </summary>
		/// <param name="docUrl">Help topic.</param>
		/// <returns>Localized url.</returns>
		public static string GetDocUrl(string docUrl) => $"https://doc.stocksharp.{LocalizedStrings.Domain}/html/{docUrl}";

		/// <summary>
		/// Get open account url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetOpenAccountUrl() => $"{GetWebSiteUrl()}/broker/openaccount/";

		/// <summary>
		/// Get sign up url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetSignUpUrl() => $"{GetWebSiteUrl()}/register/";

		/// <summary>
		/// Get forgot password url.
		/// </summary>
		/// <returns>Localized url.</returns>
		public static string GetForgotUrl() => $"{GetWebSiteUrl()}/forgot/";

		/// <summary>
		/// Installed version of the product.
		/// </summary>
		public static string InstalledVersion
		{
			get
			{
				string version;

				try
				{
					version = GetInstalledVersion(Directory.GetCurrentDirectory());
				}
				catch
				{

					version = Assembly.GetEntryAssembly().GetName().Version.To<string>();
				}

				return version;
			}
		}

		/// <summary>
		/// Get currently installed version of the product.
		/// </summary>
		/// <param name="productInstallPath">File system path to product installation.</param>
		/// <returns>Installed version of the product.</returns>
		public static string GetInstalledVersion(string productInstallPath)
		{
			if(productInstallPath.IsEmpty())
				throw new ArgumentException(nameof(productInstallPath));

			if (!File.Exists(InstallerInstallationsConfigPath))
				return null;

			SettingsStorage storage = null;
			CultureInfo.InvariantCulture.DoInCulture(() => storage = new XmlSerializer<SettingsStorage>().Deserialize(InstallerInstallationsConfigPath));

			var installations = storage?.GetValue<SettingsStorage[]>("Installations");
			if (!(installations?.Length > 0))
				return null;

			var installation = installations.FirstOrDefault(ss => productInstallPath.ComparePaths(ss.TryGet<string>("InstallDirectory")));
			if(installation == null)
				return null;

			var identityStr = installation
				.TryGet<SettingsStorage>("Version")
			   ?.TryGet<SettingsStorage>("Metadata")
			   ?.TryGet<string>("Identity");

			if(identityStr.IsEmpty())
				return null;

			// ReSharper disable once PossibleNullReferenceException
			var parts = identityStr.Split('|');

			return parts.Length != 2 ? null : parts[1];
		}

		/// <summary>
		/// Sample history data.
		/// </summary>
		public static readonly string HistoryDataPath;

		private static Mutex _mutex;

		/// <summary>
		/// Check if an instance of the application already started.
		/// </summary>
		/// <returns>Check result.</returns>
		public static bool StartIsRunning() => ThreadingHelper.TryGetUniqueMutex(AppDataPath.GetHashCode().To<string>(), out _mutex);

		/// <summary>
		/// Release all resources allocated by <see cref="StartIsRunning"/>.
		/// </summary>
		public static void StopIsRunning() => _mutex?.ReleaseMutex();
	}
}