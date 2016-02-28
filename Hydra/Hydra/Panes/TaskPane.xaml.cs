#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Panes.HydraPublic
File: TaskPane.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Panes
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Data;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Hydra.Windows;
	using StockSharp.Logging;
	using StockSharp.Hydra.Core;
	using StockSharp.Messages;
	using StockSharp.Localization;

	public partial class TaskPane : IPane, INotifyPropertyChanged
	{
		public static RoutedUICommand OpenDirectoryCommand = new RoutedUICommand();

		private static IEntityRegistry EntityRegistry => ConfigManager.GetService<IEntityRegistry>();

		public static readonly DependencyProperty TaskProperty = DependencyProperty.Register(nameof(Task), typeof(IHydraTask), typeof(TaskPane), new PropertyMetadata(null,
			(o, args) =>
			{
				var pane = (TaskPane)o;
				var task = (IHydraTask)args.NewValue;
				var prevTask = pane._task;

				if (prevTask != null)
				{
					prevTask.Settings.PropertyChanged -= pane.SettingsOnPropertyChanged;

					prevTask.Started -= pane.OnStartedTask;
					prevTask.Stopped -= pane.OnStopedTask;
				}

				pane._task = task;
				pane.DataContext = task;

				if (task == null)
				{
					pane._dataTypes.Keys.ForEach(cb => cb.IsEnabled = false);
					pane.Candles.IsEnabled = false;
					return;
				}

				task.Settings.PropertyChanged += pane.SettingsOnPropertyChanged;
				task.Started += pane.OnStartedTask;
				task.Stopped += pane.OnStopedTask;

				pane.AddAllSecurity.Content = Core.Extensions.AllSecurityId;
				pane.TimeFrames.ItemsSource = new ObservableCollection<SelectableObject>(task.SupportedDataTypes.Where(t => t.MessageType.IsCandleMessage()).Select(s => new SelectableObject(s)));

				//pane.DeleteSecurities.IsEnabled = false;

				foreach (var pair in pane._dataTypes)
				{
					var checkBox = pair.Key;

					checkBox.IsEnabled = task.SupportedDataTypes.Contains(pair.Value.Item1);

					if (checkBox.IsEnabled)
						continue;

					checkBox.IsThreeState = false;
					checkBox.IsChecked = false;
				}

				pane.Candles.IsEnabled = task.SupportedDataTypes.Any(t => t.MessageType.IsCandleMessage());
			}));

		private IHydraTask _task;

		public IHydraTask Task
		{
			get { return _task; }
			set { SetValue(TaskProperty, value); }
		}

		private void OnStartedTask(IHydraTask task)
		{
			this.GuiAsync(() => ControlPanel.IsEnabled = false);
		}

		private void OnStopedTask(IHydraTask task)
		{
			this.GuiAsync(() => ControlPanel.IsEnabled = true);
		}

		private void SettingsOnPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Title")
				_propertyChanged.SafeInvoke(this, "Title");
		}

		private class HydraSecurityTrie : SecurityTrie
		{
			private readonly Dictionary<Security, HydraTaskSecurity> _securities = new Dictionary<Security, HydraTaskSecurity>();

			public void AddRange(IEnumerable<HydraTaskSecurity> securities)
			{
				foreach (var security in securities)
				{
					Add(security);
				}
			}

			public void Add(HydraTaskSecurity security)
			{
				Add(security.Security);
				_securities.Add(security.Security, security);
			}

			public HydraTaskSecurity GetAllSecurity()
			{
				return RetrieveHydra(Core.Extensions.AllSecurityId).FirstOrDefault();
			}

			public IEnumerable<HydraTaskSecurity> RetrieveHydra(string filter)
			{
				var securities = Retrieve(filter);
				return securities.Select(s => _securities[s]);
			}

			public void RemoveRange(IEnumerable<HydraTaskSecurity> securities)
			{
				var keys = securities.Select(s => s.Security).ToArray();

                RemoveRange(keys);

				foreach (var security in keys)
					_securities.Remove(security);
			}
		}

		private readonly HydraSecurityTrie _allSecurities = new HydraSecurityTrie();
		private readonly ObservableCollection<TaskVisualSecurity> _filteredSecurities = new ObservableCollection<TaskVisualSecurity>();
		private readonly Dictionary<CheckBox, Tuple<DataType, Func<TaskVisualSecurity, bool>, Action<TaskVisualSecurity, bool>>> _dataTypes = new Dictionary<CheckBox, Tuple<DataType, Func<TaskVisualSecurity, bool>, Action<TaskVisualSecurity, bool>>>();

		private readonly PairSet<HydraTaskSecurity, TaskVisualSecurity> _visualSecurities = new PairSet<HydraTaskSecurity, TaskVisualSecurity>(); 

		public class TaskVisualSecurity : NotifiableObject
		{
			public HydraTaskSecurity TaskSecurity { get; }

			public TaskVisualSecurity(HydraTaskSecurity taskSecurity)
			{
				TaskSecurity = taskSecurity;
			}

			public bool GetIsEnabled(Type dataType, object arg)
			{
				if (dataType == null)
					throw new ArgumentNullException(nameof(dataType));

				return TaskSecurity.DataTypesSet.Contains(DataType.Create(dataType, arg));
			}

			public void SetIsEnabled(string name, Type dataType, object arg, bool value)
			{
				if (dataType == null)
					throw new ArgumentNullException(nameof(dataType));

				var set = TaskSecurity.DataTypesSet;

				if (value)
					set.Add(DataType.Create(dataType, arg));
				else
					set.Remove(DataType.Create(dataType, arg));

				TaskSecurity.DataTypes = set.ToArray();

				SetIsEnabled(name);
			}

			public void SetIsEnabled(string name)
			{
				NotifyChanged("Is{0}Enabled".Put(name));
				NotifyChanged(nameof(IsInvalid));
			}

			public bool IsTicksEnabled => GetIsEnabled(typeof(ExecutionMessage), ExecutionTypes.Tick);
			public bool IsDepthsEnabled => GetIsEnabled(typeof(QuoteChangeMessage), null);
			public bool IsLevel1Enabled => GetIsEnabled(typeof(Level1ChangeMessage), null);
			public bool IsOrderLogEnabled => GetIsEnabled(typeof(ExecutionMessage), ExecutionTypes.OrderLog);
			public bool IsCandlesEnabled => TaskSecurity.GetCandleSeries().Any();
			public bool IsTransactionsEnabled => GetIsEnabled(typeof(ExecutionMessage), ExecutionTypes.Transaction);
			public bool IsNewsEnabled => GetIsEnabled(typeof(NewsMessage), null);

			public bool IsInvalid => !IsTicksEnabled &&
			                         !IsDepthsEnabled &&
			                         !IsLevel1Enabled &&
			                         !IsOrderLogEnabled &&
			                         !IsCandlesEnabled &&
			                         !IsTransactionsEnabled &&
			                         !IsNewsEnabled;
		}

		private TaskVisualSecurity ToVisualSecurity(HydraTaskSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return _visualSecurities.SafeAdd(security, key => new TaskVisualSecurity(key));
		}

		public TaskPane()
		{
			InitializeComponent();

			SecuritiesCtrl.ItemsSource = _filteredSecurities;
			SecuritiesCtrl.ContextMenu.Items.Add(new MenuItem
			{
				Header = LocalizedStrings.Str2916,
				Command = OpenDirectoryCommand,
			});

			AddDataType<ExecutionMessage>(Ticks, ExecutionTypes.Tick);
			AddDataType<QuoteChangeMessage>(Depths, null);
			AddDataType<Level1ChangeMessage>(Level1, null);
			AddDataType<ExecutionMessage>(OrderLog, ExecutionTypes.OrderLog);
			AddDataType<ExecutionMessage>(Transactions, ExecutionTypes.Transaction);
			AddDataType<NewsMessage>(News, null);
		}

		private void AddDataType<T>(CheckBox checkBox, object arg)
			where T : Message
		{
			Func<TaskVisualSecurity, bool> get = s => s.GetIsEnabled(typeof(T), arg);
			Action<TaskVisualSecurity, bool> set = (s, v) => s.SetIsEnabled(checkBox.Name, typeof(T), arg, v);
			_dataTypes.Add(checkBox, Tuple.Create(DataType.Create(typeof(T), arg), get, set));
		}

		private bool _isInitialized;

		private void TaskPane_OnLoaded(object sender, RoutedEventArgs e)
		{
			if (_isInitialized)
				return;

			_isInitialized = true;

			_allSecurities.Clear();
			_filteredSecurities.Clear();

			Mouse.OverrideCursor = Cursors.Wait;

			var task = Task;

			System.Threading.Tasks.Task.Factory
				.StartNew(() => _allSecurities.AddRange(task.Settings.Securities))
				.ContinueWithExceptionHandling(this.GetWindow(), rest =>
				{
					FilterSecurities();
					AddAllSecurity.IsEnabled = _allSecurities.GetAllSecurity() == null;
					Mouse.OverrideCursor = null;
				});
		}

		private TaskVisualSecurity SelectedSecurity => SelectedSecurities.FirstOrDefault();

		public TaskVisualSecurity[] SelectedSecurities => SecuritiesCtrl.SelectedItems.Cast<TaskVisualSecurity>().ToArray();

		private void NameLikeTextChanged(object sender, TextChangedEventArgs e)
		{
			FilterSecurities();
		}

		private void FilterSecurities()
		{
			_filteredSecurities.Clear();
			_filteredSecurities.AddRange(_allSecurities.RetrieveHydra(NameLike.Text).Select(ToVisualSecurity));
		}

		private void ExecutedOpenDirectory(object sender, ExecutedRoutedEventArgs e)
		{
			var security = SelectedSecurity;

			var drive = Task.Settings.Drive;

			if (!(drive is LocalMarketDataDrive))
				return;

			var path = drive.Path;

			if (!security.TaskSecurity.Security.IsAllSecurity())
				path = ((LocalMarketDataDrive)drive).GetSecurityPath(security.TaskSecurity.Security.ToSecurityId());

			try
			{
				Process.Start(path);
			}
			catch (Exception ex)
			{
				ex.LogError();

				new MessageBoxBuilder()
						.Text(LocalizedStrings.Str2917Params.Put(ex.Message, path))
						.Warning()
						.Owner(this)
						.Show();
			}
		}

		private void CanExecuteOpenDirectory(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = SelectedSecurity != null;
		}

		sealed class SelectableObject : NotifiableObject
		{
			public SelectableObject(DataType value)
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				Value = value;
			}

			public DataType Value { get; }

			private bool? _isSelected;

			public bool? IsSelected
			{
				get { return _isSelected; }
				set
				{
					_isSelected = value;
					NotifyChanged(nameof(IsSelected));
				}
			}

			private bool _isThreeState;

			public bool IsThreeState
			{
				get { return _isThreeState; }
				set
				{
					_isThreeState = value;
					NotifyChanged(nameof(IsThreeState));
				}
			}
		}

		private HydraTaskSecurity CreateSecurity(Security security)
		{
			return new HydraTaskSecurity
			{
				Security = security,
				Settings = Task.Settings,
				DataTypes = Task.SupportedDataTypes.ToArray(),
			};
		}

		private void AddSecurities_OnClick(object sender, RoutedEventArgs e)
		{
			var secWnd = new SecuritiesWindowEx
			{
				Task = Task, 
				SecurityProvider = ConfigManager.GetService<ISecurityProvider>()
			};
			secWnd.SecuritiesAll.ExcludeAllSecurity();
			secWnd.SelectSecurities(_allSecurities.Retrieve(string.Empty).Where(s => !s.IsAllSecurity()).ToArray());

			if (!secWnd.ShowModal(this))
				return;

			var allSecurity = _allSecurities.GetAllSecurity();

			var selectedSecurities = secWnd.SelectedSecurities;

			if (allSecurity != null)
			{
				var taskSecurities = selectedSecurities.Select(CreateSecurity).ToArray();

				if (taskSecurities.Length == 0)
					return;

				Task.Settings.Securities.Remove(allSecurity);
				Task.Settings.Securities.AddRange(taskSecurities);

				_allSecurities.Clear();
				_filteredSecurities.Clear();

				_allSecurities.AddRange(taskSecurities);
				_filteredSecurities.AddRange(taskSecurities.Select(ToVisualSecurity));

				AddAllSecurity.IsEnabled = true;
			}
			else
			{
				var toRemove = _allSecurities.RetrieveHydra(string.Empty).Where(s => !selectedSecurities.Contains(s.Security)).ToArray();
				var toAdd = selectedSecurities.Except(_allSecurities.Retrieve(string.Empty)).Select(CreateSecurity).ToArray();

				Task.Settings.Securities.RemoveRange(toRemove);
				Task.Settings.Securities.AddRange(toAdd);

				_allSecurities.RemoveRange(toRemove);
				_allSecurities.AddRange(toAdd);

				_filteredSecurities.RemoveRange(toRemove.Select(ToVisualSecurity));
				_filteredSecurities.AddRange(toAdd.Select(ToVisualSecurity));

				if (_allSecurities.Count == 0)
					AllSecurityAdd();
				else
					AddAllSecurity.IsEnabled = true;
			}
		}

		private void AddAllSecurity_OnClick(object sender, RoutedEventArgs e)
		{
			if (_allSecurities.Count > 0)
			{
				Task.Settings.Securities.RemoveRange(_allSecurities.RetrieveHydra(string.Empty));

				_allSecurities.Clear();
				_filteredSecurities.Clear();
			}

			AllSecurityAdd();
		}

		private void DeleteSecurities_OnClick(object sender, RoutedEventArgs e)
		{
			var selectedSecurities = SelectedSecurities.ToArray();
			var toRemove = selectedSecurities.Select(s => _visualSecurities[s]).ToArray();
			Task.Settings.Securities.RemoveRange(toRemove);

			_allSecurities.RemoveRange(toRemove);
			_filteredSecurities.RemoveRange(selectedSecurities);

			if (_allSecurities.Count == 0)
				AllSecurityAdd();
		}

		private void EditSecurities_OnClick(object sender, RoutedEventArgs e)
		{
			EditSecurity();
		}

		private void AllSecurityAdd()
		{
			var allSecurity = EntityRegistry.Securities.GetAllSecurity();
			var taskSecurity = Task.ToTaskSecurity(allSecurity);

			Task.Settings.Securities.Save(taskSecurity);

			_allSecurities.Add(taskSecurity);
			_filteredSecurities.Add(ToVisualSecurity(taskSecurity));

			AddAllSecurity.IsEnabled = false;
		}

		private void SecuritiesCtrl_OnSelectionChanged(object sender, EventArgs e)
		{
			var selectedSecurities = SelectedSecurities;

			MarketDataPanel.IsEnabled = selectedSecurities.Any();
			DeleteSecurities.IsEnabled = EditSecurities.IsEnabled =
				MarketDataPanel.IsEnabled && selectedSecurities.All(s => !s.TaskSecurity.Security.IsAllSecurity());

			_suspendTimeFrames = true;

			try
			{
				foreach (var i in TimeFrames.ItemsSource.Cast<SelectableObject>())
				{
					var item = i;

					var states = selectedSecurities
						.Select(security => _visualSecurities[security].GetCandleSeries()
							.Select(a => a.Arg)
							.Contains(item.Value.Arg))
						.ToArray();

					var hasSelected = states.Contains(true);
					var hasUnselected = states.Contains(false);

					item.IsThreeState = hasSelected == hasUnselected && hasSelected;
					item.IsSelected = item.IsThreeState ? (bool?)null : hasSelected;
				}
			}
			finally
			{
				_suspendTimeFrames = false;
			}

			foreach (var pair in _dataTypes)
			{
				var checkBox = pair.Key;
				var isEnabled = pair.Value.Item2;

				checkBox.Click -= CheckBoxClick;

				try
				{
					var states = selectedSecurities.Select(isEnabled).ToArray();

					var hasSelected = states.Contains(true);
					var hasUnselected = states.Contains(false);

					checkBox.IsThreeState = hasSelected == hasUnselected && hasSelected;
					checkBox.IsChecked = checkBox.IsThreeState ? (bool?)null : hasSelected;
				}
				finally
				{
					checkBox.Click += CheckBoxClick;
				}
			}
		}

		private void HandleDoubleClick(object sender, MouseButtonEventArgs e)
		{
			EditSecurity();
		}

		private void EditSecurity()
		{
			var securities = SelectedSecurities
				.Where(s => !s.TaskSecurity.Security.IsAllSecurity())
				.Select(s => s.TaskSecurity.Security)
				.ToArray();

			if (securities.IsEmpty())
				return;

			new SecurityEditWindow { Securities = securities }.ShowModal(this);
		}

		private void CheckBoxClick(object sender, RoutedEventArgs e)
		{
			var checkBox = (CheckBox)sender;

			var isEnabled = checkBox.IsChecked == true;
			var set = _dataTypes[checkBox].Item3;
			SelectedSecurities.ForEach(s => set(s, isEnabled));

			checkBox.IsThreeState = false;

			SaveSecurities();
		}

		private bool _suspendTimeFrames;

		private void OnSelectTimeFrame(object sender, RoutedEventArgs e)
		{
			if (_suspendTimeFrames)
				return;

			var checkBox = (CandleSeriesCheckBox)sender;
			checkBox.IsThreeState = false;

			var selectedSeries = checkBox.Series;

			foreach (var selectedSecurity in SelectedSecurities)
			{
				var taskSecurity = _visualSecurities[selectedSecurity];

				if (checkBox.IsChecked == true && !taskSecurity.DataTypesSet.Contains(selectedSeries))
					taskSecurity.DataTypesSet.Add(selectedSeries);

				if (checkBox.IsChecked != true)
					taskSecurity.DataTypesSet.Remove(selectedSeries);

				taskSecurity.DataTypes = taskSecurity.DataTypesSet.ToArray();

				selectedSecurity.SetIsEnabled("Candles");
			}

			SaveSecurities();
		}

		private void SaveSecurities()
		{
			SelectedSecurities.Select(s => _visualSecurities[s]).ForEach(Task.Settings.Securities.Update);
		}

		private void Candles_OnClick(object sender, RoutedEventArgs e)
		{
			ShowCandlesPopup();
		}

		private void Candles_OnMouseEnter(object sender, MouseEventArgs e)
		{
			ShowCandlesPopup();
		}

		private void ShowCandlesPopup()
		{
			TimeFramesPopup.IsOpen = false;
			TimeFramesPopup.IsOpen = true;
		}

		void IPersistable.Load(SettingsStorage storage)
		{
			if (storage.ContainsKey("TaskId"))
			{
				var taskId = storage.GetValue<Guid>("TaskId");
				Task = MainWindow.Instance.Tasks.SingleOrDefault(s => s.Settings.Id == taskId);
			}

			SecuritiesCtrl.Load(storage.GetValue<SettingsStorage>("Securities"));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			if (Task != null)
				storage.SetValue("TaskId", Task.Settings.Id);

			storage.SetValue("Securities", SecuritiesCtrl.Save());
		}

		bool IPane.IsValid => Task != null;

		string IPane.Title => Task.Settings.Title;

		Uri IPane.Icon => Task.Icon;

		void IDisposable.Dispose()
		{
			Task = null;
		}

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}
	}

	sealed class CandleSeriesCheckBox : CheckBox
	{
		public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(nameof(Series), typeof(DataType), typeof(CandleSeriesCheckBox));

		public DataType Series
		{
			get { return (DataType)GetValue(SeriesProperty); }
			set { SetValue(SeriesProperty, value); }
		}
	}

	class EmptyFieldToImageConvertor : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var dec = (decimal?)value;
			return dec == null || dec == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class DecimalsToImageConvertor : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var sec = (int?)value;
			return sec == null ? Visibility.Visible : Visibility.Collapsed;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	class SeriesToStringConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var tf = (TimeSpan)((DataType)value).Arg;

			var str = string.Empty;

			if (tf.Days > 0)
				str += LocalizedStrings.Str2918Params.Put(tf.Days);

			if (tf.Hours > 0)
				str = (str + " " + LocalizedStrings.Str2919Params.Put(tf.Hours)).Trim();

			if (tf.Minutes > 0)
				str = (str + " " + LocalizedStrings.Str2920Params.Put(tf.Minutes)).Trim();

			if (tf.Seconds > 0)
				str = (str + " " + LocalizedStrings.Seconds.Put(tf.Seconds)).Trim();

			return str;
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}