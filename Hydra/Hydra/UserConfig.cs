#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.HydraPublic
File: UserConfig.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Windows;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Hydra.Panes;
	using StockSharp.Logging;
	using StockSharp.Hydra.Core;
	using StockSharp.Xaml;

	using ActiproSoftware.Windows.Controls.Docking.Serialization;

	class UserConfig : BaseLogReceiver
	{
		private readonly SyncObject _timerSync = new SyncObject();
		private readonly object _timerToken;
		private bool _needToSave;
		
		private readonly string _configFile;
		private readonly string _layoutFile;
		private SettingsStorage _settings;
		private readonly string _logSettingsFile;

		public string LogsDir { get; private set; }
		public string AnalyticsDir { get; private set; }

		public static UserConfig Instance { get; private set; }

		private static MainWindow MainWindow => MainWindow.Instance;

		private static DockSiteLayoutSerializer LayoutSerializer => new DockSiteLayoutSerializer
		{
			SerializationBehavior = DockSiteSerializationBehavior.All,
			DocumentWindowDeserializationBehavior = DockingWindowDeserializationBehavior.AutoCreate,
			ToolWindowDeserializationBehavior = DockingWindowDeserializationBehavior.LazyLoad
		};

		static UserConfig()
		{
			Instance = new UserConfig();
		}

		private UserConfig()
		{
			Directory.CreateDirectory(BaseApplication.AppDataPath);

			var uiPath = Path.Combine(BaseApplication.AppDataPath, "UI");
			Directory.CreateDirectory(uiPath);

			_layoutFile = Path.Combine(uiPath, "Layout.xml");
			_configFile = Path.Combine(BaseApplication.AppDataPath, "hydra_config.xml");
			_logSettingsFile = Path.Combine(BaseApplication.AppDataPath, "logManager.xml");

			AnalyticsDir = Path.Combine(BaseApplication.AppDataPath, "Analytics");
			Directory.CreateDirectory(AnalyticsDir);

			LogsDir = Path.Combine(BaseApplication.AppDataPath, "Logs");

			_timerToken = GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(() =>
			{
				lock (_timerSync)
				{
					if (!_needToSave)
						return;

					_needToSave = false;
				}

				Save();
			});
		}

		// после обфускации название типа нечитаемо
		public override string Name => TypeHelper.ApplicationName;

		private void Save()
		{
			try
			{
				CultureInfo.InvariantCulture.DoInCulture(() =>
				{
					var root = new SettingsStorage();

					root.SetValue("DriveCache", DriveCache.Instance.Save());
					root.SetValue("DatabaseConnectionCache", DatabaseConnectionCache.Instance.Save());

					var mainWindow = new SettingsStorage();

					mainWindow.SetValue("Width", MainWindow.Width);
					mainWindow.SetValue("Height", MainWindow.Height);
					mainWindow.SetValue("Location", MainWindow.GetLocation());
					mainWindow.SetValue("WindowState", MainWindow.WindowState);

					root.SetValue("mainWindow", mainWindow);

					var navBar = new SettingsStorage();

					navBar.SetValue("IsMinimized", MainWindow.NavigationBar.IsMinimized);
					navBar.SetValue("Width", MainWindow.NavigationBar.ContentWidth);
					navBar.SetValue("SelectedPane", MainWindow.NavigationBar.SelectedIndex);
					navBar.SetValue("SelectedSource", MainWindow.CurrentSources.SelectedIndex);
					navBar.SetValue("SelectedTool", MainWindow.CurrentTools.SelectedIndex);

					root.SetValue("navBar", navBar);

					var panes = new List<SettingsStorage>();

					foreach (var paneWnd in MainWindow.DockSite.DocumentWindows.OfType<PaneWindow>())
					{
						var pane = paneWnd.Pane;

						if (!pane.IsValid)
							continue;

						var settings = pane.SaveEntire(false);
						settings.SetValue("isActive", MainWindow.DockSite.ActiveWindow == paneWnd);
						panes.Add(settings);
					}

					root.SetValue("panes", panes);

					new XmlSerializer<SettingsStorage>().Serialize(root, _configFile);

					if (MainWindow.DockSite != null)
					{
						var stream = new MemoryStream();
						LayoutSerializer.SaveToStream(stream, MainWindow.DockSite);
						stream.Position = 0;
						stream.Save(_layoutFile);
					}
				});
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);
			}
		}

		public void Load()
		{
			try
			{
				if (!File.Exists(_configFile))
					return;

				_settings = CultureInfo.InvariantCulture.DoInCulture(() => new XmlSerializer<SettingsStorage>().Deserialize(_configFile));

				var driveCacheSettings = _settings.GetValue<SettingsStorage>("DriveCache");
				if (driveCacheSettings != null)
					DriveCache.Instance.Load(driveCacheSettings);

				var dbSettings = _settings.GetValue<SettingsStorage>("DatabaseConnectionCache");
				if (dbSettings != null)
					DatabaseConnectionCache.Instance.Load(dbSettings);

				var wnd = _settings.GetValue<SettingsStorage>("mainWindow");
				MainWindow.Width = wnd.GetValue<double>("Width");
				MainWindow.Height = wnd.GetValue<double>("Height");
				MainWindow.SetLocation(wnd.GetValue<Point<int>>("Location"));
				MainWindow.WindowState = wnd.GetValue<WindowState>("WindowState");

				var navBar = _settings.GetValue<SettingsStorage>("navBar");
				MainWindow.NavigationBar.IsMinimized = navBar.GetValue<bool>("IsMinimized");
				MainWindow.NavigationBar.ContentWidth = navBar.GetValue<double>("Width");
				MainWindow.NavigationBar.SelectedIndex = navBar.GetValue<int>("SelectedPane");
			}
			catch (Exception ex)
			{
				this.AddErrorLog(ex);
			}
		}

		public void LoadLayout()
		{
			try
			{
				if (File.Exists(_layoutFile))
					CultureInfo.InvariantCulture.DoInCulture(() => LayoutSerializer.LoadFromFile(_layoutFile, MainWindow.DockSite));

				if (_settings == null)
					return;

				var navBar = _settings.GetValue<SettingsStorage>("navBar");
				MainWindow.CurrentSources.SelectedIndex = navBar.GetValue<int>("SelectedSource");
				MainWindow.CurrentTools.SelectedIndex = navBar.GetValue<int?>("SelectedTool") ?? navBar.GetValue<int>("SelectedConverter");

				var panes = _settings.GetValue<IEnumerable<SettingsStorage>>("panes").ToArray();
				var wnds = MainWindow.DockSite.DocumentWindows.OfType<PaneWindow>().ToArray();

				// почему-то после загрузки разметки устанавливается в MainWindow
				wnds.ForEach(w => w.DataContext = null);

				var len = wnds.Length.Min(panes.Length);

				PaneWindow activeWnd = null;

				for (var i = 0; i < len; i++)
				{
					try
					{
						var wnd = wnds[i];

						wnd.Pane = panes[i].LoadEntire<IPane>();

						if (wnd.Pane.IsValid)
						{
							if (panes[i].GetValue<bool>("isActive"))
								activeWnd = wnd;
						}
						else
							wnd.Pane = null;
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
				}

				wnds.Where(w => w.Pane == null).ForEach(w => w.Close());

				if (activeWnd != null)
					activeWnd.Activate();
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
			finally
			{
				DriveCache.Instance.NewDriveCreated += s => { lock (_timerSync) _needToSave = true; };
				DatabaseConnectionCache.Instance.NewConnectionCreated += c => { lock (_timerSync) _needToSave = true; };
			}
		}

		public void DeleteFiles()
		{
			File.Delete(_configFile);
			File.Delete(_layoutFile);
			File.Delete(_logSettingsFile);
		}

		public LogManager CreateLogger()
		{
			var logManager = new LogManager();

			var serializer = new XmlSerializer<SettingsStorage>();

			if (File.Exists(_logSettingsFile))
			{
				logManager.Load(serializer.Deserialize(_logSettingsFile));

				var listener = logManager
					.Listeners
					.OfType<FileLogListener>()
					.FirstOrDefault(fl => !fl.LogDirectory.IsEmpty());

				if (listener != null)
					LogsDir = listener.LogDirectory;
			}
			else
			{
				logManager.Listeners.Add(new FileLogListener/*(LoggerErrorFileName)*/
				{
					Append = true,
					LogDirectory = LogsDir,
					MaxLength = 1024 * 1024 * 100 /* 100mb */,
					MaxCount = 10,
					SeparateByDates = SeparateByDateModes.SubDirectories,
				});

				//logManager.Listeners.Add(new FileLogListener(LoggerFileName)
				//{
				//	Append = true,
				//	LogDirectory = LogsDir,
				//	SeparateByDates = SeparateByDateModes.SubDirectories,
				//});

				serializer.Serialize(logManager.Save(), _logSettingsFile);
			}

			//logManager.Listeners
			//	.OfType<FileLogListener>()
			//	.Where(fl => fl.FileName == Path.GetFileNameWithoutExtension(LoggerErrorFileName))
			//	.ForEach(fl =>
			//	{
			//		fl.Filters.Add(LogListener.AllWarningFilter);
			//		fl.Filters.Add(LogListener.AllErrorFilter);
			//	});

			return logManager;
		}

		protected override void DisposeManaged()
		{
			GuiDispatcher.GlobalDispatcher.RemovePeriodicalAction(_timerToken);
			Save();

			base.DisposeManaged();
		}
	}
}
