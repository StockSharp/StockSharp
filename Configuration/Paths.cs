namespace StockSharp.Configuration
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Globalization;
	using System.Threading;

	using Ecng.Common;
	using Ecng.Security;
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
		/// <param name="userId">Identifier.</param>
		/// <returns>Localized url.</returns>
		public static string GetUserUrl(long userId) => $"{GetWebSiteUrl()}/users/{userId}/";

		/// <summary>
		/// Get strategy url.
		/// </summary>
		/// <param name="robotId">Identifier.</param>
		/// <returns>Localized url.</returns>
		[Obsolete]
		public static string GetRobotLink(long robotId) => $"{GetWebSiteUrl()}/robot/{robotId}/";

		/// <summary>
		/// Get produdct url.
		/// </summary>
		/// <param name="productId">Identifier.</param>
		/// <returns>Localized url.</returns>
		public static string GetProductLink(object productId) => $"{GetWebSiteUrl()}/store/{productId}/";

		/// <summary>
		/// Get topic url.
		/// </summary>
		/// <param name="topicId">Identifier.</param>
		/// <returns>Localized url.</returns>
		public static string GetTopicLink(long topicId) => $"{GetWebSiteUrl()}/topic/{topicId}/";

		/// <summary>
		/// Get message url.
		/// </summary>
		/// <param name="messageId">Identifier.</param>
		/// <returns>Localized url.</returns>
		public static string GetMessageLink(long messageId) => $"{GetWebSiteUrl()}/posts/m/{messageId}/";

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

				static string GetAssemblyVersion() => (Assembly.GetEntryAssembly() ?? typeof(Paths).Assembly).GetName().Version.To<string>();

				try
				{
					version = GetInstalledVersion(Directory.GetCurrentDirectory()) ?? GetAssemblyVersion();
				}
				catch
				{
					version = GetAssemblyVersion();
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
			if (productInstallPath.IsEmpty())
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

		private static ProcessSingleton _isRunningMutex;

		/// <summary>
		/// Check if an instance of the application already started.
		/// </summary>
		/// <returns>Check result.</returns>
		public static bool StartIsRunning() => StartIsRunning(AppDataPath);

		/// <summary>
		/// Check if an instance of the application already started.
		/// </summary>
		/// <returns>Check result.</returns>
		public static bool StartIsRunning(string appKey)
		{
			if (_isRunningMutex != null)
				throw new InvalidOperationException("mutex was already initialized");

			try
			{
				_isRunningMutex = new ProcessSingleton(appKey);
			}
			catch (Exception)
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Release all resources allocated by <see cref="StartIsRunning()"/>.
		/// </summary>
		public static void StopIsRunning()
		{
			_isRunningMutex?.Dispose();
			_isRunningMutex = null;
		}

		private class ProcessSingleton : Disposable
		{
			private readonly ManualResetEvent _stop = new ManualResetEvent(false);
			private readonly ManualResetEvent _stopped = new ManualResetEvent(false);

			public ProcessSingleton(string key)
			{
				Exception error = null;
				var started = new ManualResetEvent(false);

				// mutex должен освобождаться из того же потока, в котором захвачен. некоторые приложения вызывают StopIsRunning из другого потока нежели StartIsRunning
				// выделяя отдельный поток, обеспечивается гарантия корректной работы в любом случае
				ThreadingHelper.Thread(() =>
				{
					Mutex mutex;

					try
					{
						var mutexName = "stocksharp_app_" + key.UTF8().Md5();
						if (!ThreadingHelper.TryGetUniqueMutex(mutexName, out mutex))
							throw new InvalidOperationException($"can't acquire the mutex {mutexName}, (key={key})");
					}
					catch (Exception e)
					{
						error = e;
						_stopped.Set();
						return;
					}
					finally
					{
						started.Set();
					}

					try
					{
						_stop.WaitOne();
						mutex.ReleaseMutex();
					}
					finally
					{
						_stopped.Set();
					}
				})
				.Name("process_singleton")
				.Launch();

				started.WaitOne();
				if (error != null)
					throw error;
			}

			protected override void DisposeManaged()
			{
				_stop.Set();
				_stopped.WaitOne();
				base.DisposeManaged();
			}
		}
	}
}