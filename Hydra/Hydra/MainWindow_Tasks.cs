#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.HydraPublic
File: MainWindow_Tasks.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	using Ecng.ComponentModel;
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
		private sealed class LanguageSorter : IComparer
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
				return targetPlatform?.PreferLanguage ?? Languages.English;
			}
		}

		public static readonly RoutedCommand TaskEnabledChangedCommand = new RoutedCommand();
		public static readonly RoutedCommand RemoveTaskCommand = new RoutedCommand();
		public static readonly RoutedCommand EditTaskSettingsCommand = new RoutedCommand();
		public static readonly RoutedCommand AddSourcesCommand = new RoutedCommand();
		public static readonly RoutedCommand AddToolsCommand = new RoutedCommand();

		private readonly List<Type> _availableTasks = new List<Type>();

		public static readonly DependencyProperty TasksProperty = DependencyProperty.Register(nameof(Tasks), typeof(IList<IHydraTask>), typeof(MainWindow), new PropertyMetadata(new ObservableCollection<IHydraTask>()));

		public IList<IHydraTask> Tasks
		{
			get { return (IList<IHydraTask>)GetValue(TasksProperty); }
			set { SetValue(TasksProperty, value); }
		}

		public IEnumerable<IHydraTask> Sources
		{
			get { return Tasks.Where(t => !t.IsCategoryOf(TaskCategories.Tool)); }
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

					_availableTasks.AddRange(asm
						.GetTypes()
						.Where(t => typeof(IHydraTask).IsAssignableFrom(t) && !t.IsAbstract)
						.ToArray());
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

				var task = _availableTasks.FirstOrDefault(t => t == type);

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
						var newTask = task.CreateInstance<IHydraTask>();
						InitTask(newTask, settings);
						tasks.Add(newTask);
					}
					catch (Exception ex)
					{
						ex.LogError();
					}
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
				if (dataType == typeof(NewsMessage))
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
							info = taskSecurity?.TradeInfo;
							allInfo = allSecurity?.TradeInfo;

							LoadedTrades += count;
							break;
						}
						case ExecutionTypes.OrderLog:
						{
							info = taskSecurity?.OrderLogInfo;
							allInfo = allSecurity?.OrderLogInfo;

							LoadedOrderLog += count;
							break;
						}
						case ExecutionTypes.Transaction:
						{
							info = taskSecurity?.TransactionInfo;
							allInfo = allSecurity?.TransactionInfo;

							LoadedTransactions += count;
							break;
						}
						default:
							throw new ArgumentOutOfRangeException(nameof(arg));
					}
				}
				else if (dataType == typeof(QuoteChangeMessage))
				{
					info = taskSecurity?.DepthInfo;
					allInfo = allSecurity?.DepthInfo;

					LoadedDepths += count;
				}
				else if (dataType == typeof(Level1ChangeMessage))
				{
					info = taskSecurity?.Level1Info;
					allInfo = allSecurity?.Level1Info;

					LoadedLevel1 += count;
				}
				else if (dataType.IsCandleMessage())
				{
					info = taskSecurity?.CandleInfo;
					allInfo = allSecurity?.CandleInfo;

					LoadedCandles += count;
				}
				else
					throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1018);

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

						wnd?.Close();

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

		private void ExecutedAddSourcesCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var wnd = new SourcesWindow { AvailableTasks = _availableTasks.Where(t => !t.IsCategoryOf(TaskCategories.Tool)).ToArray() };

			if (!wnd.ShowModal(this))
				return;

			AddTasks(wnd.SelectedTasks);
		}

		private void ExecutedAddToolsCommand(object sender, ExecutedRoutedEventArgs e)
		{
			var wnd = new ToolsWindow { AvailableTasks = _availableTasks.Where(t => t.IsCategoryOf(TaskCategories.Tool)).ToArray() };

			if (!wnd.ShowModal(this))
				return;

			AddTasks(wnd.SelectedTasks);
		}

		private void AddTasks(IEnumerable<Type> taskTypes)
		{
			if (taskTypes == null)
				throw new ArgumentNullException(nameof(taskTypes));

			BusyIndicator.IsBusy = true;
			BusyIndicator.BusyContent = LocalizedStrings.Str2904Params.Put(taskTypes.First().GetDisplayName());

			var tasks = new List<IHydraTask>();

			Task.Factory
				.StartNew(() =>
				{
					foreach (var type in taskTypes)
					{
						this.GuiSync(() =>
						{
							BusyIndicator.BusyContent = LocalizedStrings.Str2904Params.Put(type.GetDisplayName());
						});

						var task = type.CreateInstance<IHydraTask>();

						var settings = new HydraTaskSettings
						{
							Id = Guid.NewGuid(),
							WorkingFrom = TimeSpan.Zero,
							WorkingTo = TimeHelper.LessOneDay,
							IsDefault = true,
							TaskType = type.GetTypeName(false),
						};

						_entityRegistry.TasksSettings.Add(settings);
						_entityRegistry.TasksSettings.DelayAction.WaitFlush();

						InitTask(task, settings);

						var allSec = _entityRegistry.Securities.GetAllSecurity();

						task.Settings.Securities.Add(task.ToTaskSecurity(allSec));
						task.Settings.Securities.DelayAction.WaitFlush();

						tasks.Add(task);
					}
				})
				.ContinueWithExceptionHandling(this, res =>
				{
					BusyIndicator.IsBusy = false;

					if (!res)
						return;

					Tasks.AddRange(tasks);

					var first = tasks.FirstOrDefault();

					if (first != null)
					{
						var isTool = first.IsCategoryOf(TaskCategories.Tool);

						NavigationBar.SelectedIndex = isTool ? 1 : 0;

						var listView = isTool ? CurrentTools : CurrentSources;

						listView.SelectedItem = first;
						listView.ScrollIntoView(first);

						foreach (var task in tasks)
						{
							var pane = EnsureTaskPane(task);

							if (pane != null)
								ShowPane(pane);

							//EditTask(newTask);	
						}
					}
				});
		}

		private TaskPane EnsureTaskPane(IHydraTask task)
		{
			var taskWnd = DockSite.DocumentWindows.FirstOrDefault(w =>
			{
				var pw = w as PaneWindow;

				if (pw == null)
					return false;

				var taskPane = pw.Pane as TaskPane;

				if (taskPane == null)
					return false;

				return taskPane.Task == task;
			});

			if (taskWnd != null)
			{
				taskWnd.Activate();
				return null;
			}
			else
				return new TaskPane { Task = task };
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
			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				IsStarted = false;
				StartStop.IsEnabled = true;
				LockUnlock();
			});
		}

		private void SortedSources_OnFilter(object sender, FilterEventArgs e)
		{
			e.Accepted = !IsAccept(e, TaskCategories.Tool);
		}

		private void SortedTools_OnFilter(object sender, FilterEventArgs e)
		{
			e.Accepted = IsAccept(e, TaskCategories.Tool);
		}

		private static bool IsAccept(FilterEventArgs e, TaskCategories category)
		{
			var task = (IHydraTask)e.Item;

			if (e.Item == null)
				return false;

			return task.IsCategoryOf(category);
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