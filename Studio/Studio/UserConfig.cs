namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Studio.Core;
	using StockSharp.Xaml;
	using StockSharp.Xaml.Code;
	using StockSharp.Xaml.Diagram;
	using StockSharp.Localization;

	class UserConfig : BaseLogReceiver, IPersistableService
	{
		private readonly Timer _timer;
		private readonly string _configFile;
		private bool _needToSave;
		private bool _isLayoutChanged;

		static UserConfig()
		{
			Instance = new UserConfig();
		}

		private UserConfig()
		{
			MainFolder = BaseApplication.AppDataPath;
			LogsPath = Path.Combine(MainFolder, "Logs");
			CandleSeriesDumpPath = Path.Combine(MainFolder, "CandleSources", "Dump");
			//UIPath = Path.Combine(MainFolder, "UI");
			//LayoutFile = Path.Combine(UIPath, "Layout.xml");
			StrategiesAssemblyPath = Path.Combine(MainFolder, "Strategies", "Assemblies");
			StrategiesTempPath = Path.Combine(MainFolder, "Strategies", "Temp");
			ExecutionStoragePath = Path.Combine(MainFolder, "TradingData");

#if DEBUG
			CompositionsPath = Path.Combine(@"..\..\", "Compositions");
#else
			CompositionsPath = Path.Combine(MainFolder, "Compositions");
#endif
			TryCreateDirectory();

			_configFile = Path.Combine(MainFolder, "studio_config.xml");

			var logSettingsFile = Path.Combine(LogsPath, "logManager.xml");

			var logManager = ConfigManager.GetService<LogManager>();

			if (File.Exists(logSettingsFile))
				logManager.Load(new XmlSerializer<SettingsStorage>().Deserialize(logSettingsFile));

			if (logManager.Listeners.Count == 0)
			{
				logManager.Listeners.Add(new FileLogListener
				{
					LogDirectory = LogsPath,
					SeparateByDates = SeparateByDateModes.SubDirectories,
					Append = true,
					MaxLength = 10000000,
					MaxCount = 3
				});
			}

			new XmlSerializer<SettingsStorage>().Serialize(logManager.Save(), logSettingsFile);

			logManager.Sources.Add(this);

			//IPersistableService svc = this;

			//if (svc.GetReferences() == null)
			//	svc.SetReferences(GetDefaultReferences());

			ConfigManager.RegisterService<IPersistableService>(this);

			_timer = ThreadingHelper
				.Timer(Save)
				.Interval(TimeSpan.FromSeconds(60));

			AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
		}

		// после обфускации название типа нечитаемо
		public override string Name
		{
			get { return TypeHelper.ApplicationName; }
		}

		public static UserConfig Instance { get; internal set; }

		public bool IsChangesSuspended { get; private set; }

		private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
		{
			return AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
		}

		public Func<SettingsStorage> SaveLayout;

		//public void MarkDirty()
		//{
		//	if (!_isChangesSuspended)
		//		_needToSave = true;
		//}

		private SettingsStorage _storage;

		private SettingsStorage Storage
		{
			get
			{
				lock (this)
				{
					if (_storage != null)
						return _storage;

					try
					{
						if (File.Exists(_configFile))
							_storage = CultureInfo.InvariantCulture.DoInCulture(() => new XmlSerializer<SettingsStorage>().Deserialize(_configFile));
					}
					catch (Exception ex)
					{
						this.AddErrorLog(ex);
					}

					if (_storage == null)
						_storage = new SettingsStorage();

					if (!_storage.Contains("References"))
						_storage.SetValue("References", GetDefaultReferences());

					return _storage;
				}
			}
		}

		public void MarkLayoutChanged()
		{
			_isLayoutChanged = true;
			_needToSave = true;
		}

		public void SuspendChangesMonitor()
		{
			IsChangesSuspended = true;
		}

		public void ResumeChangesMonitor()
		{
			IsChangesSuspended = false;
		}

		public string MainFolder { get; private set; }
		public string LogsPath { get; set; }
		//public string UIPath { get; set; }
		public string CandleSeriesDumpPath { get; set; }
		public string StrategiesAssemblyPath { get; set; }
		public string StrategiesTempPath { get; set; }
		public string CompositionsPath { get; set; }
		public string ExecutionStoragePath { get; set; }

		private void TryCreateDirectory()
		{
			Directory.CreateDirectory(LogsPath);
			//Directory.CreateDirectory(UIPath);
			Directory.CreateDirectory(CandleSeriesDumpPath);
			Directory.CreateDirectory(StrategiesAssemblyPath);
			Directory.CreateDirectory(StrategiesTempPath);
			Directory.CreateDirectory(ExecutionStoragePath);
		}

		private static IEnumerable<CodeReference> GetDefaultReferences()
		{
			return CodeExtensions
				.DefaultReferences
				.Where(s => !s.CompareIgnoreCase("StockSharp.Xaml.Diagram"))
				.Concat(new[] { "StockSharp.Studio.Core", "StockSharp.Studio.Controls" })
				.ToReferences();
		}

		private void Save()
		{
			SettingsStorage clone;
			bool saveLayout;

			lock (Storage.SyncRoot)
			{
				if (!_needToSave)
					return;

				saveLayout = _isLayoutChanged;

				_needToSave = false;
				_isLayoutChanged = false;

				clone = new SettingsStorage();
				clone.AddRange(Storage);
			}

			if (saveLayout || !clone.ContainsKey("MainWindow"))
				clone.SetValue("MainWindow", SaveLayout());

			try
			{
				CultureInfo.InvariantCulture.DoInCulture(() => new XmlSerializer<SettingsStorage>().Serialize(clone, _configFile));
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);
			}
		}

		private string GetCompositionFileName(CompositionDiagramElement element)
		{
#if DEBUG
			return CheckCreatePath(Path.Combine(CompositionsPath, element.Name.Replace(" ", "") + "DiagramElement.xml"));
#else
			return CheckCreatePath(Path.Combine(CompositionsPath, element.TypeId + ".xml"));
#endif
		}

		public void SaveComposition(CompositionDiagramElement element, SettingsStorage data)
		{
			try
			{
				File.WriteAllText(GetCompositionFileName(element), data.SaveSettingsStorage());
			}
			catch (Exception ex)
			{
				this.AddErrorLog(LocalizedStrings.Str3635Params, ex);
			}
		}

		public void RemoveComposition(CompositionDiagramElement element)
		{
			var file = GetCompositionFileName(element);

			if (File.Exists(file))
				File.Delete(file);
		}

		public IEnumerable<string> LoadCompositions()
		{
			if (!Directory.Exists(CompositionsPath))
				Directory.CreateDirectory(CompositionsPath);

#if DEBUG
			return Directory.GetFiles(CompositionsPath, "*DiagramElement.xml").Select(File.ReadAllText).ToList();
#else
			return Directory.GetFiles(CompositionsPath, "*.xml").Select(File.ReadAllText).ToList();
#endif
		}

		private static string CheckCreatePath(string path)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			return path;
		}

		protected override void DisposeManaged()
		{
			// немедленно вызываем таймер для сброса изменений и выключаем таймер
			_timer.Change(TimeSpan.Zero, TimeSpan.FromMinutes(1));
			_timer.Dispose();

			base.DisposeManaged();
		}

		bool IPersistableService.ContainsKey(string key)
		{
			return Storage.ContainsKey(key);
		}

		TValue IPersistableService.GetValue<TValue>(string key, TValue defaultValue)
		{
			return Storage.GetValue(key, defaultValue);
		}

		void IPersistableService.SetValue(string key, object value)
		{
			if (IsChangesSuspended)
				return;

			lock (Storage.SyncRoot)
			{
				Storage.SetValue(key, value);
				_needToSave = true;
			}
		}
	}
}