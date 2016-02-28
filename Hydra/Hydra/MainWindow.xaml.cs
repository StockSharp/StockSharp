#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.HydraPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra
{
	using System;
	using System.Threading;
	using System.Configuration;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data.Common;
	using System.Diagnostics;
	using System.IO;
	using System.Linq;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Input;
	using System.Windows.Threading;
	using Timer = System.Timers.Timer;

	using ActiproSoftware.Windows.Controls.Docking;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Xaml;
	using Ecng.Net;
	using Ecng.Serialization;
	using Ecng.Interop;

	using StockSharp.Logging;
	using StockSharp.Algo;
	using StockSharp.Algo.History.Hydra;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Community;
	using StockSharp.Configuration;
	using StockSharp.Xaml;
	using StockSharp.Messages;

	using StockSharp.Hydra.Controls;
	using StockSharp.Hydra.Windows;
	using StockSharp.Hydra.Panes;
	using StockSharp.Hydra.Server;
	using StockSharp.Hydra.Core;
	using StockSharp.Localization;

	public partial class MainWindow : ILogListener
	{
		public static readonly RoutedCommand OpenLogCommand = new RoutedCommand();
		public static readonly RoutedCommand TargetPlatformCommand = new RoutedCommand();
		public static readonly RoutedCommand HelpCommand = new RoutedCommand();
		public static readonly RoutedCommand AboutCommand = new RoutedCommand();
		public static readonly RoutedCommand LogDirectoryCommand = new RoutedCommand();
		public static readonly RoutedCommand CopyToBufferCommand = new RoutedCommand();
		public static readonly RoutedCommand OpenPaneCommand = new RoutedCommand();
		public static readonly RoutedCommand ImportPaneCommand = new RoutedCommand();
		public static readonly RoutedCommand BoardsCommand = new RoutedCommand();
		public static readonly RoutedCommand AnalyticsCommand = new RoutedCommand();
		public static readonly RoutedCommand MemoryStatisticsCommand = new RoutedCommand();

		private class HydraEmailLogListener : EmailLogListener
		{
			private readonly MainWindow _parent;

			public HydraEmailLogListener(MainWindow parent)
			{
				if (parent == null)
					throw new ArgumentNullException(nameof(parent));

				_parent = parent;

				From = "noreply@stocksharp.com";
			}

			public bool ErrorEmailSent;
			public decimal ErrorCount;

			protected override string GetSubject(LogMessage message)
			{
				return message.Source.Name;
			}

			protected override void OnWriteMessage(LogMessage message)
			{
				if (ErrorEmailSent)
					return;

				if (message.Level != LogLevels.Error)
					return;

				ErrorCount++;

				var maxCount = _parent._entityRegistry.Settings.EmailErrorCount;

				if (maxCount <= 0 || ErrorCount <= maxCount)
					return;

				To = _parent._entityRegistry.Settings.EmailErrorAddress;

				if (To.IsEmpty())
					return;

				ErrorEmailSent = true;
				base.OnWriteMessage(new LogMessage(_parent._logManager.Application, TimeHelper.NowWithOffset, LogLevels.Error, LocalizedStrings.Str2940Params.Put(maxCount)));
			}
		}

		private readonly LogManager _logManager;
		private readonly HydraEmailLogListener _emailListener;

		private ServiceHost<HydraServer> _host;
		private IRemoteStorageAuthorization _customAutorization;

		private const string _pluginsDir = "Plugins";

		private IStorageRegistry _storageRegistry;
		private HydraEntityRegistry _entityRegistry;
		
		private Timer _timer;
		private Timer _killTimer;

		private DispatcherTimer _updateStatusTimer;

		private readonly SessionClient _sessionClient = new SessionClient();

		private TrayIcon _trayIcon;

		public long LoadedTrades { get; private set; }
		public long LoadedDepths { get; private set; }
		public long LoadedOrderLog { get; private set; }
		public long LoadedLevel1 { get; private set; }
		public long LoadedCandles { get; private set; }
		public long LoadedNews { get; private set; }
		public long LoadedTransactions { get; private set; }

		private Mutex _mutex;

		private bool _isReseting;
		private string _dbFile;

		private Security SelectedSecurity
		{
			get
			{
				var wnd = DockSite.ActiveWindow as PaneWindow;

				var taskPane = wnd?.Pane as TaskPane;

				if (taskPane == null)
					return null;

				var selectedSecurity = taskPane.SelectedSecurities.FirstOrDefault(s => !s.TaskSecurity.Security.IsAllSecurity());
				return selectedSecurity?.TaskSecurity.Security;
			}
		}

		public WindowState CurrentWindowState { get; set; }

		#region Dependency properties

		public static readonly DependencyProperty HydraEntityRegistryProperty = DependencyProperty.Register(nameof(HydraEntityRegistry), typeof(HydraEntityRegistry), typeof(MainWindow));

		public HydraEntityRegistry HydraEntityRegistry
		{
			get { return (HydraEntityRegistry)GetValue(HydraEntityRegistryProperty); }
			set { SetValue(HydraEntityRegistryProperty, value); }
		}

		public static readonly DependencyProperty IsStartedProperty = DependencyProperty.Register(nameof(IsStarted), typeof(bool), typeof(MainWindow));

		public bool IsStarted
		{
			get { return (bool)GetValue(IsStartedProperty); }
			set { SetValue(IsStartedProperty, value); }
		}

		#endregion

		public MainWindow()
		{
			CheckIsRunning();

			_logManager = UserConfig.Instance.CreateLogger();

			Mouse.OverrideCursor = Cursors.Wait;

			InitializeComponent();

			_logManager.Listeners.Add(new GuiLogListener(MonitorControl));
			_logManager.Listeners.Add(this);

			_emailListener = new HydraEmailLogListener(this);
			_logManager.Listeners.Add(_emailListener);

			MemoryStatMenuItem.IsChecked = MemoryStatistics.IsEnabled;

			Title = TypeHelper.ApplicationNameWithVersion;

			if (AutomaticUpdater.ClosingForInstall)
			{
				Application.Current.Shutdown();
				return;
			}

			AutomaticUpdater.MenuItem = MnuCheckForUpdates;
			AutomaticUpdater.Translate();

			//DockSite.DocumentWindows.CollectionChanged += DocumentWindows_OnCollectionChanged;

			_logManager.Sources.Add(UserConfig.Instance);

			Instance = this;

			UserConfig.Instance.Load();

			OnUpdateUi(null, null);
		}

		public static MainWindow Instance { get; private set; }

		private void MainWindowLoaded(object sender, RoutedEventArgs e)
		{
			_sessionClient.CreateSession(Products.Hydra);

			BusyIndicator.BusyContent = LocalizedStrings.Str2941;
			BusyIndicator.IsBusy = true;

			Task.Factory.StartNew(() =>
			{
				InitializeDataSource();

				var tasks = InitializeTasks();

				GuiDispatcher.GlobalDispatcher.AddSyncAction(() => BusyIndicator.BusyContent = LocalizedStrings.Str2942.Put(LocalizedStrings.Securities));
				ConfigManager.RegisterService<ISecurityProvider>(new FilterableSecurityProvider(_entityRegistry.Securities));

				return tasks;
			})
			.ContinueWith(res =>
			{
				BusyIndicator.IsBusy = false;

				if (res.IsFaulted && res.Exception != null)
				{
					var ex = res.Exception.InnerException;

					ex.LogError();

					Mouse.OverrideCursor = null;

					new MessageBoxBuilder()
						.Caption(ex is DbException ? LocalizedStrings.Str2943 : LocalizedStrings.Str2915)
						.Text(ex.ToString())
						.Error()
						.Owner(this)
						.Show();

					Close();

					return;
				}

				Tasks.AddRange(res.Result);

				var collectionView = (AutoRefreshCollectionViewSource)FindResource("SortedSources");
				if (collectionView != null)
				{
					var view = (ListCollectionView)collectionView.View;
					view.CustomSort = new LanguageSorter();
				}

				HydraEntityRegistry = _entityRegistry;

				var settings = _entityRegistry.Settings;

				try
				{
					_customAutorization = ConfigManager.TryGetService<IRemoteStorageAuthorization>();
				}
				catch (Exception ex)
				{
					ex.LogError();
				}

				if (_customAutorization == null)
					_customAutorization = new DummyRemoteStorageAuthorization();

				ApplySettings();

				InitializeGuiEnvironment();

				if (settings.AutoStart)
					Start(true);

				AutomaticUpdater.ForceCheckForUpdate(true);

				if (Tasks.Count == 0)
				{
					var newTasks = new List<Type>();

					var sourcesWnd = new SourcesWindow { AvailableTasks = _availableTasks.Where(t => !t.IsCategoryOf(TaskCategories.Tool)).ToArray() };

					if (sourcesWnd.ShowModal(this))
						newTasks.AddRange(sourcesWnd.SelectedTasks);

					var toolsWnd = new ToolsWindow { AvailableTasks = _availableTasks.Where(t => t.IsCategoryOf(TaskCategories.Tool)).ToArray() };

					if (toolsWnd.ShowModal(this))
						newTasks.AddRange(toolsWnd.SelectedTasks);

					if (newTasks.Any())
						AddTasks(newTasks);
				}
			}, TaskScheduler.FromCurrentSynchronizationContext());

			try
			{
				AdvertisePanel.Client = new NotificationClient();
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}

		private void InitializeDataSource()
		{
			_storageRegistry = new StorageRegistry();
			ConfigManager.RegisterService(_storageRegistry);

			_entityRegistry = (HydraEntityRegistry)ConfigManager.GetService<IEntityRegistry>();
			_entityRegistry.TasksSettings.Recycle = false;
			((SecurityList)_entityRegistry.Securities).BulkLoad = true;

			_dbFile = _entityRegistry.FirstTimeInit(Properties.Resources.StockSharp, db => _entityRegistry.Version = HydraEntityRegistry.LatestVersion);

			CheckDatabase();

			ConfigManager.RegisterService<IExchangeInfoProvider>(new ExchangeInfoProvider(_entityRegistry));

			var allSec = _entityRegistry.Securities.GetAllSecurity();

			if (allSec != null)
				return;

			_entityRegistry.Securities.Add(new Security
			{
				Id = Core.Extensions.AllSecurityId,
				Code = "ALL",
				//Class = task.GetDisplayName(),
				Name = LocalizedStrings.Str2835,
				Board = ExchangeBoard.Associated,
				ExtensionInfo = new Dictionary<object, object>(),
			});
			_entityRegistry.Securities.DelayAction.WaitFlush();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (IsStarted)
			{
				if (new MessageBoxBuilder()
						.Text(LocalizedStrings.Str2944)
						.Warning()
						.Owner(this)
						.YesNo()
						.Show() == MessageBoxResult.Yes)
				{
					StartStopClick(null, null);
				}
				else
				{
					e.Cancel = true;
					return;
				}
			}

			_sessionClient.CloseSession();

			if (!_isReseting)
			{
				// если что-то еще не "сбросилось" в БД
				_entityRegistry?.DelayAction.WaitFlush();

				// сервис не будет зарегистрирован, если приложение закрыто было при старте из TargetPlatformWindow
				if (ConfigManager.IsServiceRegistered<IStorageRegistry>())
				{
					UserConfig.Instance.Dispose();
					DriveCache.Instance.Dispose();
				}
			}

			_trayIcon?.Close();
			_host?.Close();
			_mutex?.ReleaseMutex();

			base.OnClosing(e);
		}

		private void CheckIsRunning()
		{
			var settings = ConfigurationManager.ConnectionStrings;
			var connectionStrings = (settings.Cast<ConnectionStringSettings>().Select(setting => setting.ConnectionString)).ToArray();

			var str = connectionStrings.Aggregate(string.Empty, (current, connectionString) => current + connectionString).GetHashCode().To<string>();

			if (ThreadingHelper.TryGetUniqueMutex(str, out _mutex))
				return;

			new MessageBoxBuilder()
				.Text(LocalizedStrings.Str2945)
				.Warning()
				.Show();

			Close();
		}

		private void InitializeGuiEnvironment()
		{
			if (CurrentSources.Items.Count > 0)
				CurrentSources.SelectedIndex = 0;

			if (CurrentTools.Items.Count > 0)
				CurrentTools.SelectedIndex = 0;

			UserConfig.Instance.LoadLayout();

			_updateStatusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
			_updateStatusTimer.Tick += OnUpdateUi;
			_updateStatusTimer.Start();

			_trayIcon = new TrayIcon();
			_trayIcon.StartStop += () => StartStopClick(null, null);
			_trayIcon.Logs += () => ExecutedOpenLogCommand(null, null);
			_trayIcon.Show(this);

			MnuTargetPlatform.Visibility = Environment.Is64BitOperatingSystem ? Visibility.Visible : Visibility.Hidden;

			Mouse.OverrideCursor = null;
		}

		private void ApplySettings()
		{
			if (_host != null)
			{
				_host.Close();
				_host.Instance.Dispose();
				_logManager.Sources.Remove(_host.Instance);
			}

			if (_entityRegistry.Settings.IsServer)
			{
				IRemoteStorageAuthorization authorization;

				switch (_entityRegistry.Settings.Authorization)
				{
					case AuthorizationModes.Anonymous:
						authorization = new AnonymousRemoteStorageAuthorization();
						break;
					case AuthorizationModes.Windows:
						authorization = new WindowsRemoteStorageAuthorization();
						break;
					case AuthorizationModes.Community:
						authorization = new CommunityRemoteStorageAuthorization();
						break;
					case AuthorizationModes.Custom:
						authorization = _customAutorization;
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				var server = new HydraServer(_storageRegistry, _entityRegistry, Sources)
				{
					Authorization = authorization,
					MaxSecurityCount = _entityRegistry.Settings.MaxSecurityCount
				};

				_logManager.Sources.Add(server);

				_host = new ServiceHost<HydraServer>(server);
				_host.Open();
			}

			if (_timer != null)
			{
				_timer.Stop();

				_killTimer?.Stop();
			}

			if (_entityRegistry.Settings.AutoStop)
			{
				_timer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds) { AutoReset = true };
				_timer.Elapsed += (s, args) =>
				{
					var time = DateTime.Now.TimeOfDay;
					if (time >= _entityRegistry.Settings.StopTime && time < _entityRegistry.Settings.StopTime + TimeSpan.FromMinutes(5))
					{
						GuiDispatcher.GlobalDispatcher.AddAction(AutoStopAndKill);
					}
				};
				_timer.Start();
			}

			//if (!CurrentSources.IsNull())
			//{
			//	DockSite.DocumentWindows
			//		.Where(d => d is SourceWindow)
			//		.ForEach(d => ((SourceWindow)d).TaskControl.SecuritiesCtrl.ChangeExtendedColumnVisible());
			//}
		}

		private void AutoStopAndKill()
		{
			if (IsStarted)
			{
				_killTimer = new Timer(TimeSpan.FromMinutes(10).TotalMilliseconds);
				_killTimer.Elapsed += (arg1, arg2) => Process.GetCurrentProcess().Kill();
				_killTimer.Start();
				StartStopClick(null, null);
			}
			else
				Process.GetCurrentProcess().Kill();
		}

		private void StartStopClick(object sender, RoutedEventArgs e)
		{
			if (IsStarted)
			{
				StartStop.IsEnabled = false;
				StopAllTasks();
			}
			else
				Start();
		}

		private void Start(bool auto = false)
		{
			if (Tasks.Count(l => l.Settings.IsEnabled) == 0)
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str2946Params.Put(auto ? LocalizedStrings.Str2947 + Environment.NewLine : string.Empty))
					.Warning()
					.Owner(this)
					.Show();

				StartStop.IsChecked = false;
				return;
			}

			var message = Sources
				.Where(s => s.Settings.IsEnabled)
				.GroupBy(s => s.Settings.Drive.Path, s => s.Name)
				.Where(path => path.Count() > 1)
				.Aggregate(string.Empty, (current, g) => current + LocalizedStrings.Str2948Params.Put(g.Join(", "), g.Key));

			if (!message.IsEmpty())
			{
				if (new MessageBoxBuilder()
						.Text(message + Environment.NewLine + LocalizedStrings.Str2949)
						.Warning()
						.Owner(this)
						.YesNo()
						.Show() != MessageBoxResult.Yes)
				{
					StartStop.IsChecked = false;
					return;
				}
			}

			if (StartAllTasks())
			{
				_emailListener.ErrorCount = 0;
				_emailListener.ErrorEmailSent = false;

				IsStarted = true;
				LockUnlock();

				if (auto)
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str2950)
						.Info()
						.Owner(this)
						.Show();
				}
			}
			else
			{
				new MessageBoxBuilder()
					.Text(LocalizedStrings.Str2951)
					.Warning()
					.Owner(this)
					.Show();

				StartStop.IsChecked = false;
			}
		}

		protected override void OnStateChanged(EventArgs e)
		{
			base.OnStateChanged(e);

			if (_entityRegistry == null || !_entityRegistry.Settings.MinimizeToTray)
				return;

			if (WindowState == WindowState.Minimized)
			{
				Hide();
			}
			else
			{
				CurrentWindowState = WindowState;
			}
		}

		private void OnUpdateUi(object sender, EventArgs e)
		{
			Status.Text = "T={LoadedTrades}     D={LoadedDepths}     OL={LoadedOrderLog}     L1={LoadedLevel1}     C={LoadedCandles}     N={LoadedNews}     TS={LoadedTransactions}".PutEx(this);
		}

		private void LockUnlock()
		{
			_trayIcon.UpdateState(IsStarted);

			if (IsStarted)
			{
				StartStop.Content = LocalizedStrings.Str242;
				StartStop.IsChecked = true;
			}
			else
			{
				StartStop.Content = LocalizedStrings.Str2421;
				StartStop.IsChecked = false;
			}
		}

		private void ResetLogsImages()
		{
			LogErrorImg.Visibility = Visibility.Collapsed;
			WarnErrorImg.Visibility = Visibility.Collapsed;

			LastWarnError.Visibility = Visibility.Collapsed;
			LastLogError.Visibility = Visibility.Collapsed;
			LastLogMessage = string.Empty;
		}

		private void ExecutedLogDirectoryCommand(object sender, ExecutedRoutedEventArgs e)
		{
			ResetLogsImages();

			if (Directory.Exists(UserConfig.Instance.LogsDir))
				Process.Start(UserConfig.Instance.LogsDir);
		}

		private void ExecutedOpenLogCommand(object sender, ExecutedRoutedEventArgs e)
		{
			ResetLogsImages();

			LogToolWindow.Activate();
			Logs.IsOpen = false;
		}

		private void ExecutedHelpCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Process.Start("http://stocksharp.com/doc/html/a720a275-440a-44ce-86e2-bcec2e0bc55f.htm");
		}

		private void ExecutedAboutCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var wnd = new AboutWindow(this);
			wnd.ShowModal(this);
		}

		private void ExecutedTargetPlatformCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var language = LocalizedStrings.ActiveLanguage;
			var platform = Environment.Is64BitProcess ? Platforms.x64 : Platforms.x86;

			var window = new TargetPlatformWindow();

			if (!window.ShowModal(this))
				return;

			if (window.SelectedLanguage == language && window.SelectedPlatform == platform)
				return;

			// temporarily set prev lang for display the followed message
			// and leave all text as is if user will not restart the app
			LocalizedStrings.ActiveLanguage = language;

			var result = new MessageBoxBuilder()
				.Text(LocalizedStrings.Str2952Params.Put(TypeHelper.ApplicationName))
				.Owner(this)
				.Info()
				.YesNo()
				.Show();

			if (result == MessageBoxResult.Yes)
				Application.Current.Restart();
		}

		private string LastLogMessage
		{
			get { return (string)LastLogMessageCtrl.Content; }
			set { LastLogMessageCtrl.Content = value; }
		}

		private void CanExecuteCopyToBufferCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !LastLogMessage.IsEmpty();
		}

		private void ExecutedCopyToBufferCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Clipboard.SetText(LastLogMessage); 
		}

		private void ExecutedExit(object sender, ExecutedRoutedEventArgs e)
		{
			Close();
		}

		private void ExecutedOpenPaneCommand(object sender, ExecutedRoutedEventArgs e)
		{
			IPane pane = null;

			switch (e.Parameter.ToString().ToLowerInvariant())
			{
				case "trade":
					pane = new TradesPane { SelectedSecurity = SelectedSecurity };
					break;

				case "depth":
					pane = new DepthPane { SelectedSecurity = SelectedSecurity };
					break;

				case "candle":
					pane = new CandlesPane { SelectedSecurity = SelectedSecurity };
					break;

				case "orderlog":
					pane = new OrderLogPane { SelectedSecurity = SelectedSecurity };
					break;

				case "level1":
					pane = new Level1Pane { SelectedSecurity = SelectedSecurity };
					break;

				case "news":
					pane = new NewsPane();
					break;

				case "task":
					var task = (IHydraTask)(NavigationBar.SelectedPane == SourcesPane ? CurrentSources.SelectedItem : CurrentTools.SelectedItem);

					if (task != null)
						pane = EnsureTaskPane(task);

					break;

				case "transaction":
					pane = new TransactionsPane { SelectedSecurity = SelectedSecurity };
					break;
					
				case "security":
					var wnd = DockSite.DocumentWindows.FirstOrDefault(w =>
					{
						var paneWnd = w as PaneWindow;
						return paneWnd?.Pane is AllSecuritiesPane;
					});

					if (wnd != null)
						wnd.Activate();
					else
						pane = new AllSecuritiesPane();

					break;
			}

			if (pane == null)
				return;

			ShowPane(pane);
		}

		private void ExecutedImportPaneCommand(object sender, ExecutedRoutedEventArgs e)
		{
			Type dataType;
			ExecutionTypes? execType = null;

			var param = e.Parameter.ToString().ToLowerInvariant();

			switch (param)
			{
				case "security":
					dataType = typeof(SecurityMessage);
					break;

				case "trade":
					dataType = typeof(ExecutionMessage);
					execType = ExecutionTypes.Tick;
					break;

				case "depth":
					dataType = typeof(QuoteChangeMessage);
					break;

				case "candle":
					dataType = typeof(CandleMessage);
					break;

				case "orderlog":
					dataType = typeof(ExecutionMessage);
					execType = ExecutionTypes.OrderLog;
					break;

				case "level1":
					dataType = typeof(Level1ChangeMessage);
					break;

				case "news":
					dataType = typeof(NewsMessage);
					break;

				case "transaction":
					dataType = typeof(ExecutionMessage);
					execType = ExecutionTypes.Transaction;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(e), param, LocalizedStrings.Str1655);
			}

			var importWnd = DockSite.DocumentWindows.FirstOrDefault(w =>
			{
				var pw = w as PaneWindow;

				var importPane = pw?.Pane as ImportPane;

				if (importPane == null)
					return false;

				return importPane.DataType == dataType && importPane.ExecutionType == execType;
			});

			if (importWnd != null)
				importWnd.Activate();
			else
				ShowPane(new ImportPane { ExecutionType = execType, DataType = dataType });
		}

		private void ResetSettings_Click(object sender, RoutedEventArgs e)
		{
			var res = new MessageBoxBuilder()
						.Text(LocalizedStrings.Str2954Params.Put(TypeHelper.ApplicationName))
						.Warning()
						.Owner(this)
						.YesNo()
						.Show();

			if (res != MessageBoxResult.Yes)
				return;

			_isReseting = true;

			if (_dbFile != null)
				File.Delete(_dbFile);

			UserConfig.Instance.DeleteFiles();

			Close();

			Process.Start(Application.ResourceAssembly.Location);
		}

		private void EraseData_Click(object sender, RoutedEventArgs e)
		{
			new EraseDataWindow
			{
				StorageRegistry = _storageRegistry,
				SelectedSecurity = SelectedSecurity,
				EntityRegistry = _entityRegistry
			}.ShowModal(this);
		}

		private void Settings_Click(object sender, RoutedEventArgs e)
		{
			var wnd = new SettingsWindow
			{
				Settings = _entityRegistry.Settings.Clone()
			};

			if (!wnd.ShowModal(this))
				return;

			_entityRegistry.Settings = wnd.Settings;
			ApplySettings();
		}

		private void Synchronize_Click(object sender, RoutedEventArgs e)
		{
			new SynchronizeWindow
			{
				StorageRegistry = _storageRegistry,
				EntityRegistry = _entityRegistry,
			}.ShowModal(this);
		}

		private void GluingData_Click(object sender, RoutedEventArgs e)
		{
			ShowPane(new GluingDataPane());
		}

		private void ExecutedBoardsCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var wnd = DockSite.DocumentWindows.FirstOrDefault(w =>
			{
				var paneWnd = w as PaneWindow;
				return paneWnd?.Pane is ExchangeBoardPane;
			});

			if (wnd != null)
				wnd.Activate();
			else
				ShowPane(new ExchangeBoardPane());
		}

		private void ExecutedAnalyticsCommand(object sender, ExecutedRoutedEventArgs e)
		{
			ShowPane(new AnalyticsPane());
		}

		public void ShowPane(IPane pane)
		{
			if (pane == null)
				throw new ArgumentNullException(nameof(pane));

			var wnd = new PaneWindow { Pane = pane };
			DockSite.DocumentWindows.Add(wnd);
			wnd.Activate();
		}

		private void ExecutedMemoryStatisticsCommand(object sender, ExecutedRoutedEventArgs e)
		{
			MemoryStatistics.AddOrRemove();
			MemoryStatMenuItem.IsChecked = MemoryStatistics.IsEnabled;
		}

		void ILogListener.WriteMessages(IEnumerable<LogMessage> messages)
		{
			foreach (var m in messages)
			{
				var message = m;

				switch (message.Level)
				{
					case LogLevels.Error:
					{
						GuiDispatcher.GlobalDispatcher.AddAction(() =>
						{
							LogErrorImg.Visibility = Visibility.Visible;
							LastWarnError.Visibility = Visibility.Collapsed;
							LastLogError.Visibility = Visibility.Visible;
						});

						break;
					}
					case LogLevels.Warning:
					{
						GuiDispatcher.GlobalDispatcher.AddAction(() =>
						{
							WarnErrorImg.Visibility = Visibility.Visible;
							LastWarnError.Visibility = Visibility.Visible;
							LastLogError.Visibility = Visibility.Collapsed;
						});

						break;
					}
				}

				if (message.Level == LogLevels.Warning || message.Level == LogLevels.Error)
				{
					GuiDispatcher.GlobalDispatcher.AddAction(() => LastLogMessage = "{0:HH:mm:ss}  {1}".Put(message.Time, message.Message));
				}
			}
		}

		void IPersistable.Load(SettingsStorage storage)
		{
		}

		void IPersistable.Save(SettingsStorage storage)
		{
		}

		private void DockSite_OnWindowClosing(object sender, DockingWindowEventArgs e)
		{
			var paneWnd = e.Window as PaneWindow;

			paneWnd?.Pane?.Dispose();

			//if (!paneWnd.Pane.InProcess)
			//	return;

			//new MessageBoxBuilder()
			//	.Text("Закладка '{0}' в процессе работы и ее невозможно закрыть.".Put(paneWnd.Pane.Title))
			//	.Warning()
			//	.Owner(this)
			//	.Show();

			//e.Cancel = true;
		}

		private void DockSite_OnWindowClosed(object sender, DockingWindowEventArgs e)
		{
			if (e.Window.Name == "LogToolWindow")
				return;

			DockSite.DocumentWindows.Remove((DocumentWindow)e.Window);
		}

		private void DockSite_OnWindowActivated(object sender, DockingWindowEventArgs e)
		{
			var wnd = e.Window as PaneWindow;

			var taskPane = wnd?.Pane as TaskPane;

			if (taskPane == null)
				return;

			var task = taskPane.Task;

			var lv = task.IsCategoryOf(TaskCategories.Tool) ? CurrentTools : CurrentSources;

			lv.ScrollIntoView(task);
			lv.SelectedItem = task;
		}

		private void ProxySettings_Click(object sender, RoutedEventArgs e)
		{
			BaseApplication.EditProxySettings(this);
		}

		void IDisposable.Dispose()
		{
		}
	}
}