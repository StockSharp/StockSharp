namespace StockSharp.Configuration
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using System.Reflection;
	using Ecng.Serialization;

	/// <summary>
	/// Application updater locator.
	/// </summary>
	public class InstallerLocator : IPersistable
	{
		static readonly string ConfigPath = Path.Combine(Paths.InstallerDir, "installer_location.xml");
		const string CheckUpdatesFilename = "check_updates.exe";
		const string InstallerFilename = "Installer.exe";

		/// <summary>Singleton instance.</summary>
		public static InstallerLocator Instance { get; }

		/// <summary>Updater directory.</summary>
		public string Directory { get; private set; }
		/// <summary>Updater version.</summary>
		public Version Version { get; private set; }

		private InstallerLocator() { }

		/// <summary>
		/// Get filesystem path to check_updates.exe utility.
		/// </summary>
		public string GetCheckUpdatesPath()
		{
			if(!System.IO.Directory.Exists(Directory))
				return null;

			var path = Path.Combine(Directory, CheckUpdatesFilename);

			return File.Exists(path) ? path : null;
		}

		/// <summary>
		/// Get filesystem path to Installer.exe app.
		/// </summary>
		public string GetIntallerPath()
		{
			if(!System.IO.Directory.Exists(Directory))
				return null;

			var path = Path.Combine(Directory, InstallerFilename);

			return File.Exists(path) ? path : null;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			Directory = storage.GetValue<string>(nameof(Directory));
			Version = GetVersion(storage.GetValue<string>(nameof(Version)));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Directory), Directory);
			storage.SetValue(nameof(Version), Version.ToString());
		}

		/// <summary>Save locator data.</summary>
		public void Save()
		{
			var caller = Assembly.GetCallingAssembly();
			VerifyCallerIsValidUpdater(caller);

			Directory = System.IO.Path.GetDirectoryName(caller.Location);
			Version = GetVersion(FileVersionInfo.GetVersionInfo(caller.Location).FileVersion);

			new XmlSerializer<SettingsStorage>().Serialize(((IPersistable) this).Save(), ConfigPath);
		}

		/// <summary>Check if caller is a valid updater.</summary>
		public void VerifyCallerIsValidUpdater(Assembly asm = null)
		{
			var caller = asm ?? Assembly.GetCallingAssembly();
			var verInfo = FileVersionInfo.GetVersionInfo(caller.Location);
			var callerVersion = GetVersion(verInfo.FileVersion);

			if (Version > callerVersion)
				throw new InvalidOperationException($"config was saved with new version of installer ({Version} > {callerVersion}), other location='{Directory}'");
		}

		static Version GetVersion(string verString) => Version.TryParse(verString, out var ver) ? ver : new Version(1, 0, 0, 0);

		static InstallerLocator()
		{
			Instance = new InstallerLocator();
			var path = ConfigPath;

			if (File.Exists(path))
				((IPersistable) Instance).Load(new XmlSerializer<SettingsStorage>().Deserialize(path));
		}
	}
}
