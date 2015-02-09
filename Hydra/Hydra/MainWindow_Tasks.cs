namespace StockSharp.Hydra
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.IO;
	using System.Linq;
	using System.Reflection;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Reflection;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Windows;
	using StockSharp.Hydra.Panes;
	using StockSharp.Logging;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class MainWindow
	{
		sealed class LanguageSorter : IComparer
		{
			private readonly Languages _language;

			public LanguageSorter()
			{
				_language = Thread.CurrentThread.CurrentCulture.Name == "en-US" ? Languages.English : Languages.Russian;
			}

			public int Compare(object x, object y)
			{
				var xTask = (IHydraTask)x;
				var yTask = (IHydraTask)y;

				if (xTask.Settings.IsEnabled != yTask.Settings.IsEnabled)
					return yTask.Settings.IsEnabled.CompareTo(xTask.Settings.IsEnabled);
				
				var xLang = GetLanguage(xTask);
				var yLang = GetLanguage(yTask);

				var xKey = xLang == _language ? -1 : (int)xLang;
				var yKey = yLang == _language ? -1 : (int)yLang;

				if (xKey == yKey)
					return string.Compare(xTask.ToString(), yTask.ToString(), StringComparison.Ordinal);

				return xKey.CompareTo(yKey);
			}

			private static Languages GetLanguage(IHydraTask task)
			{
				var targetPlatform = task.GetType().GetAttribute<TargetPlatformAttribute>();
				return targetPlatform != null ? targetPlatform.PreferLanguage : Languages.English;
			}
		}

		public static RoutedCommand NewTaskCommand = new RoutedCommand();

		public static RoutedCommand TaskEnabledChangedCommand = new RoutedCommand();

		public static RoutedCommand RemoveTaskCommand = new RoutedCommand();
		public static RoutedCommand EditTaskSettingsCommand = new RoutedCommand();
		//public static RoutedCommand EditConverterSettingsCommand = new RoutedCommand();

		private readonly IList<IHydraTask> _availableTasks = new List<IHydraTask>();

		public static readonly DependencyProperty TasksProperty = DependencyProperty.Register("Tasks", typeof(IList<IHydraTask>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<IHydraTask>()));

		public IList<IHydraTask> Tasks
		{
			get { return (IList<IHydraTask>)GetValue(TasksProperty); }
			set { SetValue(TasksProperty, value); }
		}

		public IEnumerable<IHydraTask> Sources
		{
			get { return Tasks.Where(t => t.Type == TaskTypes.Source); }
		}

		private IList<IHydraTask> InitializeTasks()
		{
			var tasks = new List<IHydraTask>();

			if (!Directory.Exists(_pluginsDir))
				return tasks;

			foreach (var plugin in Directory.GetFiles(_pluginsDir, "*.dll").Where(p => Path.GetFileNameWithoutExtension(p).ContainsIgnoreCase("StockSharp.Hydra.")))
			{
				if (!plugin.IsAssembly())
				{
					_logManager.Application.AddWarningLog(LocalizedStrings.Str2897Params, plugin);
					continue;
				}

				try
				{
					var asm = Assembly.Load(AssemblyName.GetAssemblyName(plugin));
					var allTasks = asm
						.GetTypes()
						.Where(t => typeof(IHydraTask).IsAssignableFrom(t) && !t.IsAbstract)
						.Select(t => t.CreateInstance<IHydraTask>())
						.ToArray();

					foreach (var t in allTasks)
					{
						var task = t;
						GuiDispatcher.GlobalDispatcher.AddSyncAction(() => BusyIndicator.BusyContent = LocalizedStrings.Str2898.Put(task.GetDisplayName()));
						_availableTasks.Add(task);
					}
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}

			var query = _entityRegistry.TasksSettings.ToArray().GroupBy(set => set.TaskType);

			foreach (var group in query)
			{
				var type = Type.GetType(group.Key, false, true);

				if (type == null)
				{
					_logManager.Application.AddErrorLog(LocalizedStrings.Str2899Params, group.Key);

					foreach (var settings in group)
					{
						settings.Securities.Clear();
						_entityRegistry.TasksSettings.Remove(settings);
					}

					continue;
				}

				var task = _availableTasks.FirstOrDefault(t => t.GetType() == type);

				if (task == null)
				{
					_logManager.Application.AddWarningLog(LocalizedStrings.Str2900Params, group.Key);
					continue;
				}

				foreach (var settings in group)
				{
					try
					{
						var title = settings.Title;

						if (title.IsEmpty())
							title = task.GetDisplayName();

						GuiDispatcher.GlobalDispatcher.AddSyncAction(() => BusyIndicator.BusyContent = LocalizedStrings.Str2904Params.Put(title));
						var newSource = task.GetType().CreateInstance<IHydraTask>();
						InitTask(newSource, settings);
						tasks.Add(newSource);
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
				}
			}

			if (tasks.Count != 0)
				return tasks;

			foreach (var task in _availableTasks)
			{
				try
				{
					tasks.Add(CreateTask(task.GetType()));
				}
				catch (Exception ex)
				{
					ex.LogError();
				}
			}

			return tasks;
		}

		private readonly SynchronizedDictionary<IHydraTask, HydraTaskSecurity> _taskAllSecurities = new SynchronizedDictionary<IHydraTask, HydraTaskSecurity>();
		private readonly SynchronizedDictionary<IHydraTask, SynchronizedDictionary<Security, HydraTaskSecurity>> _taskSecurityCache = new SynchronizedDictionary<IHydraTask, SynchronizedDictionary<Security, HydraTaskSecurity>>();

		private void InitTask(IHydraTask task, HydraTaskSettings settings)
		{
			Core.Extensions.Tasks.Add(task);

			task.Init(settings);

			_logManager.Sources.Add(task);

			task.DataLoaded += (security, dataType, arg, time, count) =>
			{
				if (dataType == typeof(News))
				{
					LoadedNews += count;
					return;
				}

				var allSecurity = _taskAllSecurities.SafeAdd(task, key => task.GetAllSecurity());
				var taskSecurity = _taskSecurityCache.SafeAdd(task).SafeAdd(security, key => task.Settings.Securities.FirstOrDefault(s => s.Security == key));

				HydraTaskSecurity.TypeInfo info;
				HydraTaskSecurity.TypeInfo allInfo;

				if (dataType == typeof(ExecutionMessage))
				{
					switch ((ExecutionTypes)arg)
					{
						case ExecutionTypes.Tick:
						{
							info = taskSecurity == null ? null : taskSecurity.TradeInfo;
							allInfo = allSecurity == null ? null : allSecurity.TradeInfo;

							LoadedTrades += count;
							break;
						}
						case ExecutionTypes.OrderLog:
						{
							info = taskSecurity == null ? null : taskSecurity.OrderLogInfo;
							allInfo = allSecurity == null ? null : allSecurity.OrderLogInfo;

							LoadedOrderLog += count;
							break;
						}
						case ExecutionTypes.Order:
						{
							info = taskSecurity == null ? null : taskSecurity.ExecutionInfo;
							allInfo = allSecurity == null ? null : allSecurity.ExecutionInfo;

							LoadedExecutions += count;
							break;
						}
						default:
							throw new ArgumentOutOfRangeException("arg");
					}
				}
				else if (dataType == typeof(QuoteChangeMessage))
				{
					info = taskSecurity == null ? null : taskSecurity.DepthInfo;
					allInfo = allSecurity == null ? null : allSecurity.DepthInfo;

					LoadedDepths += count;
				}
				else if (dataType == typeof(Level1ChangeMessage))
				{
					info = taskSecurity == null ? null : taskSecurity.Level1Info;
					allInfo = allSecurity == null ? null : allSecurity.Level1Info;

					LoadedLevel1 += count;
				}
				else if (dataType.IsSubclassOf(typeof(CandleMessage)))
				{
					info = taskSecurity == null ? null : taskSecurity.CandleInfo;
					allInfo = allSecurity == null ? null : allSecurity.CandleInfo;

					LoadedCandles += count;
				}
				else
					throw new ArgumentOutOfRangeException("dataType", dataType, LocalizedStrings.Str1018);

				if (allInfo != null)
				{
					allInfo.Count += count;
					allInfo.LastTime = time.LocalDateTime;

					task.Settings.Securities.Update(allSecurity);
				}

				if (info == null)
					return;

				info.Count += count;
				info.LastTime = time.LocalDateTime;

				task.Settings.Securities.Update(taskSecurity);
			};
		}

		private IHydraTask CreateTask(Type taskType)
		{
			var task = taskType.CreateInstance<IHydraTask>();

			var settings = new HydraTaskSettings
			{
				Id = Guid.NewGuid(),
				WorkingFrom = TimeSpan.Zero,
				WorkingTo = TimeHelper.LessOneDay,
				IsDefault = true,
				TaskType = taskType.GetTypeName(false),
			};

			_entityRegistry.TasksSettings.Add(settings);
			_entityRegistry.TasksSettings.DelayAction.WaitFlush();

			InitTask(task, settings);

			var allSec = _entityRegistry.Securities.ReadById(Core.Extensions.AllSecurityId);

			task.Settings.Securities.Add(task.ToTaskSecurity(allSec));
			task.Settings.Securities.DelayAction.WaitFlush();

			return task;
		}

		private void DeleteTask(IHydraTask task)
		{
			task.Settings.Securities.Clear();
			_entityRegistry.TasksSettings.Remove(task.Settings);

			Core.Extensions.Tasks.Cache.ForEach(t =>
			{
				if (t.Settings.DependFrom == task)
					t.Settings.DependFrom = null;
			});

			Core.Extensions.Tasks.Remove(task);
		}

		private ListView GetListView(IHydraTask task)
		{
			return task.Type == TaskTypes.Source ? CurrentSources : CurrentConverters;
		}

		private void ExecutedRemoveTaskCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var lv = (ListView)e.Source;
			var task = lv.SelectedItem as IHydraTask;

			if (task == null)
				return;

			BusyIndicator.BusyContent = LocalizedStrings.Str2902Params.Put(task.Name);
			BusyIndicator.IsBusy = true;

			Task.Factory
				.StartNew(() => DeleteTask(task))
				.ContinueWithExceptionHandling(this, res =>
				{
					if (res)
					{
						var wnd = DockSite.DocumentWindows
							.OfType<PaneWindow>()
							.Where(w => w.Pane is TaskPane)
							.FirstOrDefault(w => ((TaskPane)w.Pane).Task == task);

						if (wnd != null)
							wnd.Close();

						Tasks.Remove(task);

						//if (!Tasks.Any())
						//	btnSource.IsEnabled = false;
					}

					BusyIndicator.IsBusy = false;
				});
		}

		private void CanExecuteRemoveTaskCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			if (IsStarted)
			{
				var lv = (ListView)e.Source;
				e.CanExecute = lv.SelectedItem != null && !((IHydraTask)lv.SelectedItem).Settings.IsEnabled;
			}
			else
			{
				e.CanExecute = true;
			}
		}

		private void ExecutedEditTaskSettingsCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var lv = (ListView)e.Source;
			var task = lv.SelectedItem as IHydraTask;

			if (task == null)
				return;

			EditTask(task);
		}

		private void EditTask(IHydraTask task)
		{
			var wnd = new TaskSettingsWindow { Task = task };
			if (!wnd.ShowModal(this))
				return;

			task.Settings.IsDefault = false;
			task.SaveSettings();
		}

		private void CanExecuteEditTaskSettingsCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			if (IsStarted)
			{
				var lv = (ListView)e.Source;
				e.CanExecute = lv.SelectedItem != null && !((IHydraTask)lv.SelectedItem).Settings.IsEnabled;
			}
			else
			{
				e.CanExecute = true;
			}
		}

		private void ExecutedNewTaskCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var task = (IHydraTask)e.Parameter;

			if (task == null)
				return;

			//if (Tasks.Any(t => t.GetType() == task.GetType()))
			//{
			//	var msg = "Multi".ValidateLicense();

			//	if (msg != null)
			//	{
			//		_logManager.Application.AddErrorLog(msg);

			//		new MessageBoxBuilder()
			//			.Text(LocalizedStrings.Str2903Params.Put(task.GetDisplayName()))
			//			.Warning()
			//			.Owner(this)
			//			.Show();

			//		return;	
			//	}
			//}

			BusyIndicator.BusyContent = LocalizedStrings.Str2904Params.Put(task.GetDisplayName());
			BusyIndicator.IsBusy = true;

			IHydraTask newTask = null;

			Task.Factory
				.StartNew(() => newTask = CreateTask(task.GetType()))
				.ContinueWithExceptionHandling(this, res =>
				{
					BusyIndicator.IsBusy = false;

					if (!res)
						return;

					Tasks.Add(newTask);

					NavigationBar.SelectedIndex = task.Type == TaskTypes.Source ? 0 : 1;

					GetListView(newTask).SelectedItem = newTask;
					NewSourceButton.IsOpen = false;
					GetListView(newTask).ScrollIntoView(newTask);

					OpenPaneCommand.Execute("Task", null);
					EditTask(newTask);
				});
		}

		private void ExecutedTaskEnabledChangedCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var lv = (ListView)e.Source;
			var task = lv.SelectedItem as IHydraTask;

			if (task == null)
				return;

			if (task.Settings.IsEnabled)
				lv.ScrollIntoView(task);

			//если задача была включена впервые - открыть окно редактирования настроек
			if (task.Settings.IsDefault && task.Settings.IsEnabled)
			{
				EditTask(task);

				OpenPaneCommand.Execute("Task", null);

				//настройки не были сохранены - необходимо выключить задачу
				if (task.Settings.IsDefault)
				{
					var checkBox = e.OriginalSource as CheckBox;
					if (checkBox != null)
						checkBox.IsChecked = false;

					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str2905)
						.Warning()
						.Owner(this)
						.Show();
				}
			}
			else
				task.SaveSettings();
		}

		private void CanExecuteTaskEnabledChangedCommand(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = !IsStarted;
		}

		private readonly List<IHydraTask> _tasksToStart = new List<IHydraTask>();
		private readonly SynchronizedList<IHydraTask> _startedTasks = new SynchronizedList<IHydraTask>();
		
		private bool StartAllTasks()
		{
			_tasksToStart.Clear();

			var enabledTasks = Tasks.Where(s => s.Settings.IsEnabled).ToArray();

			if (enabledTasks.Length > 0)
			{
				foreach (var task in enabledTasks)
				{
					task.AddInfoLog(LocalizedStrings.Str2906);

					if (task.Settings.Securities.IsEmpty())
					{
						task.AddInfoLog(LocalizedStrings.Str2907);
						continue;
					}

					_tasksToStart.Add(task);
				}
			}

			if (_tasksToStart.IsEmpty())
				return false;

			foreach (var task in _tasksToStart)
			{
				task.Started += OnStartedTask;
				task.Stopped += OnStopedTask;

				task.Start();
			}

			// TODO
			//// группировка по "корням"
			//foreach (var taskGroup in _tasksToStart.GroupBy(t =>
			//{
			//	while (t.Settings.DependFrom != null)
			//		t = t.Settings.DependFrom;

			//	return t.Id;
			//}))
			//{
			//	var tasks = new CachedSynchronizedSet<IHydraTask>();
			//	tasks.AddRange(taskGroup);

			//	ThreadingHelper.Thread(() =>
			//	{
			//		try
			//		{
			//			foreach (var task in tasks.Cache)
			//			{
			//				task.Start();

			//				if (task.State != TaskStates.Started)
			//					tasks.Remove(task);
			//			}

			//			while (true)
			//			{
			//				if (tasks.Cache.Length == 0)
			//					break;

			//				foreach (var task in tasks.Cache)
			//				{
			//					task.Process();

			//					if (task.)
			//				}	
			//			}
			//		}
			//		catch (Exception ex)
			//		{
			//			_logManager.Application.AddErrorLog(ex);
			//		}
			//	})
			//	.Name("{0} Task thread".Put(Name))
			//	.Launch();
			//}

			return true;
		}

		private void StopAllTasks()
		{
			_tasksToStart.Where(t => t.State == TaskStates.Started).ForEach(d => d.Stop());
		}

		private void OnStartedTask(IHydraTask task)
		{
			_startedTasks.Add(task);
		}

		private void OnStopedTask(IHydraTask task)
		{
			task.Started -= OnStartedTask;
			task.Stopped -= OnStopedTask;

			_startedTasks.Remove(task);

			_taskAllSecurities.Remove(task);
			_taskSecurityCache.Remove(task);

			if (_startedTasks.IsEmpty())
				OnStoppedSources();
		}

		private void OnStoppedSources()
		{
			_updateStatusTimer.Stop();

			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				IsStarted = false;
				StartStop.IsEnabled = true;
				LockUnlock();
			});
		}

		private void SortedSources_OnFilter(object sender, FilterEventArgs e)
		{
			e.Accepted = IsAccept(e, TaskTypes.Source);
		}

		private void SortedConverters_OnFilter(object sender, FilterEventArgs e)
		{
			e.Accepted = IsAccept(e, TaskTypes.Converter);
		}

		private static bool IsAccept(FilterEventArgs e, TaskTypes type)
		{
			var task = (IHydraTask)e.Item;

			if (e.Item == null)
				return false;

			return task.Type == type;
		}

		private void CurrentTasks_OnSelectionChanged(object sender, EventArgs eventArgs)
		{
			var item = ((ListView)sender).SelectedItem;

			var wnd = DockSite.DocumentWindows.SingleOrDefault(w => w.DataContext == item);

			if (wnd != null)
				wnd.Activate();
		}

		private void CurrentTasks_OnSelectionItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			OpenPaneCommand.Execute("Task", null);
		}
	}
}
