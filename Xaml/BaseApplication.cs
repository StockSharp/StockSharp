namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Windows;
	using System.Windows.Threading;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;
	using Ecng.Reflection;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Базовый класс для WPF приложений.
	/// </summary>
	public abstract class BaseApplication : Application
	{
		private bool _isInitialized;

		/// <summary>
		/// Инициализировать <see cref="BaseApplication"/>.
		/// </summary>
		protected BaseApplication()
		{
			ShutdownMode = ShutdownMode.OnMainWindowClose;
			DispatcherUnhandledException += OnDispatcherUnhandledException;
			System.Windows.Forms.Application.ThreadException += OnThreadException;
			AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			ShowExceptions = true;

			// TODO esper. перестает работать график!!!
			//чтобы в GUI была корректная культура
			//FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
		}

		/// <summary>
		/// Показывать на экран ошибки, или только передавать их в <see cref="LogManager"/>. По-умолчанию ошибки показываются.
		/// </summary>
		public bool ShowExceptions { get; set; }

		/// <summary>
		/// Расширенная функциональность, которая будет отображена в окне <see cref="TargetPlatformWindow"/>.
		/// </summary>
		protected internal virtual IEnumerable<TargetPlatformFeature> ExtendedFeatures
		{
			get { return Enumerable.Empty<TargetPlatformFeature>(); }
		}

		private void HandleException(Exception exception, bool isWpf)
		{
			try
			{
				exception.LogError();

				if (!isWpf)
					return;

				if (!_isInitialized)
				{
					if (MainWindow != null)
						_isInitialized = !MainWindow.GetValue<Window, VoidType, bool>("IsSourceWindowNull", null);
				}

				if (ShowExceptions)
				{
					var builder = new MessageBoxBuilder()
						.Text(exception.ToString())
						.Error();

					if (_isInitialized)
					{
						try
						{
							builder.Owner(Current.GetActiveOrMainWindow());
						}
						catch
						{
							builder.Owner(MainWindow);
						}
					}

					builder.Show();
				}

				if (!_isInitialized)
					Close();
			}
			catch (Exception e)
			{
				try
				{
					e.LogError();
				}
				catch (Exception e1)
				{
					// как последняя возможность вывести ошибки
					Trace.WriteLine(e);
					Trace.WriteLine(e1);
				}

				Close(); // в процессе показа диалога с ошибкой у нас ошибка - значит все совсем плохо.
			}
		}

		private void OnThreadException(object sender, ThreadExceptionEventArgs e)
		{
			HandleException(e.Exception, false);
		}

		private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			var comException = e.Exception as COMException;

			// http://stackoverflow.com/questions/12769264/openclipboard-failed-when-copy-pasting-data-from-wpf-datagrid
			if (comException == null || comException.ErrorCode != -2147221040)
				HandleException(e.Exception, true);

			e.Handled = true;
		}

		private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			HandleException((Exception)e.ExceptionObject, false);
		}

		private static string Title
		{
			get { return TypeHelper.ApplicationName.Replace("S#.", string.Empty); }
		}

		/// <summary>
		/// Путь к директории с настройками.
		/// </summary>
		public static readonly string AppDataPath;

		/// <summary>
		/// Путь к конфигурационному файлу определения платформы.
		/// </summary>
		public static readonly string PlatformConfigurationFile;

		/// <summary>
		/// Путь к конфигурационному файлу настроек прокси-сервера.
		/// </summary>
		public static readonly string ProxyConfigurationFile;

		/// <summary>
		/// Настройки прокси-сервера.
		/// </summary>
		public static ProxySettings ProxySettings { get; private set; }

		static BaseApplication()
		{
			var s = ConfigurationManager.AppSettings.Get("settingsPath");

			if (s != null)
			{
				s = s.Replace("%Documents%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
				AppDataPath = s;
			}
			else
			{
				AppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StockSharp", Title);
			}

			PlatformConfigurationFile = Path.Combine(AppDataPath, "platform_config.xml");
			ProxyConfigurationFile = Path.Combine(AppDataPath, "proxy_config.xml");
		}

		/// <summary>
		/// Иконка приложения.
		/// </summary>
		protected internal string AppIcon { get; set; }

		/// <summary>
		/// Проверять ли платформу при запуске.
		/// </summary>
		protected bool CheckTargetPlatform { get; set; }

		private const string _langPrefix = "lang=";

		/// <summary>
		/// Обработка запуска приложения.
		/// </summary>
		/// <param name="e">Аргумент.</param>
		protected override void OnStartup(StartupEventArgs e)
		{
			Extensions.TranslateActiproDocking();
			Extensions.TranslateActiproNavigation();

			GuiDispatcher.InitGlobalDispatcher();
			DispatcherPropertyChangedEventManager.Init();

			//GuiDispatcher.GlobalDispatcher.AddPeriodicalAction(CommandManager.InvalidateRequerySuggested);

			if (!ProxyConfigurationFile.IsEmpty() && File.Exists(ProxyConfigurationFile))
			{
				var settings = new XmlSerializer<SettingsStorage>().Deserialize(ProxyConfigurationFile);

				ProxySettings = new ProxySettings();
				ProxySettings.Load(settings);
				ProxySettings.ApplyProxySettings();
			}
			else
				ProxySettings = ProxySettings.GetProxySettings();

			if (CheckTargetPlatform)
			{
				//если одним из аргументов является путь к самому приложению,
				//то значит оно запущено из под специального загрузчика
				var executablePath = Assembly.GetEntryAssembly().Location;
				if (e.Args.Any(a => a == executablePath) || !Environment.Is64BitOperatingSystem)
				{
					var language = e.Args.FirstOrDefault(s => s.ContainsIgnoreCase(_langPrefix));

					LocalizedStrings.ActiveLanguage = language != null
						? language.Replace(_langPrefix, string.Empty).To<Languages>()
						: LocalizedStrings.ActiveLanguage;

					StartApp();

					return;
				}

				if (!Directory.Exists(AppDataPath))
					Directory.CreateDirectory(AppDataPath);

				var window = new TargetPlatformWindow();

				if (window.AutoStart || window.ShowDialog() == true)
				{
					switch (window.SelectedPlatform)
					{
						case Platforms.x86:
							StartX86();
							Process.GetCurrentProcess().Kill();
							break;

						case Platforms.x64:
							// обнуляем ссылку главного окна, так как она равна TargetPlatformWindow
							MainWindow = null;

							StartApp();
							break;
					}
				}
				else
				{
					MainWindow = null;
					Close();
				}
			}

			base.OnStartup(e);
		}

		private static void Close()
		{
			Environment.Exit(-1);

			// WPF shutdow do not terminate app immediatelly
			//Shutdown();
		}

		/// <summary>
		/// Редактировать настройки прокси-сервера.
		/// </summary>
		public static void EditProxySettigs()
		{
			var wnd = new ProxyEditorWindow
			{
				ProxySettings = ProxySettings.Clone()
			};

			if (!wnd.ShowModal())
				return;

			ProxySettings.Load(wnd.ProxySettings.Save());
			ProxySettings.ApplyProxySettings();

			if (!ProxyConfigurationFile.IsEmpty())
				new XmlSerializer<SettingsStorage>().Serialize(ProxySettings.Save(), ProxyConfigurationFile);
		}

		private void StartApp()
		{
			ShutdownMode = ShutdownMode.OnMainWindowClose;

			//if (culture.IsEmpty())
			//	return;

			var cultureInfo = CultureInfo.GetCultureInfo(LocalizedStrings.ActiveLanguage == Languages.English ? "en-US" : "ru-RU");

			Thread.CurrentThread.CurrentCulture = cultureInfo;
			Thread.CurrentThread.CurrentUICulture = cultureInfo;
		}

		private static void StartX86()
		{
			var launcher = Path.Combine(AppDataPath, "{0}.x86.exe".Put(Title));

			Xaml.Properties.Resources.Launcher_x86.Save(launcher);

			Process.Start(launcher, "\"{0}\" {1}{2}".Put(Assembly.GetEntryAssembly().Location, _langPrefix, LocalizedStrings.ActiveLanguage));
		}
	}
}