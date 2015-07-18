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
	using StockSharp.Algo.Candles;
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

		private static IEntityRegistry EntityRegistry
		{
			get { return ConfigManager.GetService<IEntityRegistry>();  }
		}

		public static readonly DependencyProperty TaskProperty = DependencyProperty.Register("Task", typeof(IHydraTask), typeof(TaskPane), new PropertyMetadata(null,
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
				pane.TimeFrames.ItemsSource = new ObservableCollection<SelectableObject>(task.SupportedCandleSeries.Select(s => new SelectableObject(s)));

				//pane.DeleteSecurities.IsEnabled = false;

				foreach (var pair in pane._dataTypes)
				{
					var checkBox = pair.Key;

					checkBox.IsEnabled = task.SupportedMarketDataTypes.Contains(pair.Value.Item1);

					if (checkBox.IsEnabled)
						continue;

					checkBox.IsThreeState = false;
					checkBox.IsChecked = false;
				}

				pane.Candles.IsEnabled = task.SupportedMarketDataTypes.Contains(typeof(Candle));
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

		private readonly List<HydraTaskSecurity> _allSecurities = new List<HydraTaskSecurity>();
		private readonly ObservableCollection<TaskVisualSecurity> _filteredSecurities = new ObservableCollection<TaskVisualSecurity>();
		private readonly Dictionary<CheckBox, Tuple<Type, Func<TaskVisualSecurity, bool>, Action<TaskVisualSecurity, bool>>> _dataTypes = new Dictionary<CheckBox, Tuple<Type, Func<TaskVisualSecurity, bool>, Action<TaskVisualSecurity, bool>>>();

		private readonly PairSet<HydraTaskSecurity, TaskVisualSecurity> _visualSecurities = new PairSet<HydraTaskSecurity, TaskVisualSecurity>(); 

		public class TaskVisualSecurity : NotifiableObject
		{
			public HydraTaskSecurity TaskSecurity { get; private set; }

			public TaskVisualSecurity(HydraTaskSecurity taskSecurity)
			{
				TaskSecurity = taskSecurity;
			}

			public bool GetIsEnabled(Type dataType)
			{
				if (dataType == null)
					throw new ArgumentNullException("dataType");

				return TaskSecurity.MarketDataTypesSet.Contains(dataType);
			}

			public void SetIsEnabled(Type dataType, bool value)
			{
				if (dataType == null)
					throw new ArgumentNullException("dataType");

				var set = TaskSecurity.MarketDataTypesSet;

				if (value)
					set.Add(dataType);
				else
					set.Remove(dataType);

				TaskSecurity.MarketDataTypes = set.ToArray();

				NotifyChanged("Is{0}Enabled".Put(dataType.Name
					.Replace("ChangeMessage", string.Empty)
					.Replace("Message", string.Empty)
					.Replace("Market", string.Empty)
					.Replace("Item", string.Empty)));

				NotifyChanged("IsInvalid");
			}

			public bool IsTradeEnabled
			{
				get { return GetIsEnabled(typeof(Trade)); }
			}

			public bool IsDepthEnabled
			{
				get { return GetIsEnabled(typeof(MarketDepth)); }
			}

			public bool IsLevel1Enabled
			{
				get { return GetIsEnabled(typeof(Level1ChangeMessage)); }
			}

			public bool IsOrderLogEnabled
			{
				get { return GetIsEnabled(typeof(OrderLogItem)); }
			}

			public bool IsCandleEnabled
			{
				get { return GetIsEnabled(typeof(Candle)); }
			}

			public bool IsExecutionEnabled
			{
				get { return GetIsEnabled(typeof(ExecutionMessage)); }
			}

			public bool IsInvalid
			{
				get
				{
					return
						!IsTradeEnabled &&
						!IsDepthEnabled &&
						!IsLevel1Enabled &&
						!IsOrderLogEnabled &&
						!IsCandleEnabled &&
						!IsExecutionEnabled;
				}
			}
		}

		private TaskVisualSecurity ToVisualSecurity(HydraTaskSecurity security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

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

			AddDataType<Trade>(Trades);
			AddDataType<MarketDepth>(Depths);
			AddDataType<Level1ChangeMessage>(Level1Changes);
			AddDataType<OrderLogItem>(OrderLog);
			AddDataType<ExecutionMessage>(Executions);
		}

		private void AddDataType<T>(CheckBox checkBox)
		{
			Func<TaskVisualSecurity, bool> get = s => s.GetIsEnabled(typeof(T));
			Action<TaskVisualSecurity, bool> set = (s, v) => s.SetIsEnabled(typeof(T), v);
			_dataTypes.Add(checkBox, Tuple.Create(typeof(T), get, set));
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
					AddAllSecurity.IsEnabled = _allSecurities.All(s => !s.Security.IsAllSecurity());
					Mouse.OverrideCursor = null;
				});
		}

		private TaskVisualSecurity SelectedSecurity
		{
			get { return SelectedSecurities.FirstOrDefault(); }
		}

		public TaskVisualSecurity[] SelectedSecurities
		{
			get { return SecuritiesCtrl.SelectedItems.Cast<TaskVisualSecurity>().ToArray(); }
		}

		private void NameLikeTextChanged(object sender, TextChangedEventArgs e)
		{
			FilterSecurities();
		}

		private void FilterSecurities()
		{
			var secLike = NameLike.Text;

			var selectedSecurities = _allSecurities
				.Where
				(s =>
					secLike.IsEmpty() ||
					(!s.Security.Id.IsEmpty() && s.Security.Id.ContainsIgnoreCase(secLike)) ||
					(!s.Security.Code.IsEmpty() && s.Security.Code.ContainsIgnoreCase(secLike)) ||
					(!s.Security.Name.IsEmpty() && s.Security.Name.ContainsIgnoreCase(secLike))
				)
				.ToArray();

			_filteredSecurities.Clear();
			_filteredSecurities.AddRange(selectedSecurities.Select(ToVisualSecurity));
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
			public SelectableObject(CandleSeries value)
			{
				if (value == null)
					throw new ArgumentNullException("value");

				Value = value;
			}

			public CandleSeries Value { get; private set; }

			private bool? _isSelected;

			public bool? IsSelected
			{
				get { return _isSelected; }
				set
				{
					_isSelected = value;
					NotifyChanged("IsSelected");
				}
			}

			private bool _isThreeState;

			public bool IsThreeState
			{
				get { return _isThreeState; }
				set
				{
					_isThreeState = value;
					NotifyChanged("IsThreeState");
				}
			}
		}

		private HydraTaskSecurity CreateSecurity(Security security)
		{
			return new HydraTaskSecurity
			{
				Security = security,
				Settings = Task.Settings,
				MarketDataTypes = Task.SupportedMarketDataTypes.ToArray(),
				CandleSeries = Task.SupportedCandleSeries.ToArray(),
			};
		}

		private void AddSecurities_OnClick(object sender, RoutedEventArgs e)
		{
			var secWnd = new SecuritiesWindowEx
			{
				Task = Task, 
				SecurityProvider = ConfigManager.GetService<FilterableSecurityProvider>()
			};
			secWnd.SelectSecurities(_allSecurities.Select(s => s.Security).Where(s => !s.IsAllSecurity()));

			if (!secWnd.ShowModal(this))
				return;

			var allSecurity = _allSecurities.FirstOrDefault(s => s.Security.IsAllSecurity());

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
				var toRemove = _allSecurities.Where(s => !selectedSecurities.Contains(s.Security)).ToArray();
				var toAdd = selectedSecurities.Except(_allSecurities.Select(s => s.Security)).Select(CreateSecurity).ToArray();

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
				Task.Settings.Securities.RemoveRange(_allSecurities);

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
			var allSecurity = EntityRegistry.Securities.ReadById(Core.Extensions.AllSecurityId);
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
						.Select(security => _visualSecurities[security].CandleSeries
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

				var series = taskSecurity.CandleSeries.ToList();

				if (checkBox.IsChecked == true && !series.Any(s => s.CandleType == selectedSeries.CandleType && s.Arg.Equals(selectedSeries.Arg)))
					series.Add(selectedSeries.Clone());

				if (checkBox.IsChecked != true)
					series.RemoveWhere(s => s.CandleType == selectedSeries.CandleType && s.Arg.Equals(selectedSeries.Arg));

				taskSecurity.CandleSeries = series.ToArray();
				selectedSecurity.SetIsEnabled(typeof(Candle), taskSecurity.CandleSeries.Any());
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

		bool IPane.IsValid
		{
			get { return Task != null; }
		}

		string IPane.Title
		{
			get { return Task.Settings.Title; }
		}

		Uri IPane.Icon
		{
			get { return Task.Icon; }
		}

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
		public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register("Series", typeof(CandleSeries), typeof(CandleSeriesCheckBox));

		public CandleSeries Series
		{
			get { return (CandleSeries)GetValue(SeriesProperty); }
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
			var tf = (TimeSpan)((CandleSeries)value).Arg;

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