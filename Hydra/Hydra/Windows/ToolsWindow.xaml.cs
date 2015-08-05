namespace StockSharp.Hydra.Windows
{
	using System;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;

	using MoreLinq;

	public partial class ToolsWindow
	{
		private readonly ObservableCollection<TaskInfo> _tasks = new ObservableCollection<TaskInfo>();

		public ToolsWindow()
		{
			InitializeComponent();

			TasksCtrl.ItemsSource = _tasks;
		}

		public Type[] AvailableTasks
		{
			get { return _tasks.Select(i => i.Task).ToArray(); }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_tasks.Clear();

				foreach (var task in value)
				{
					var info = new TaskInfo(task)
					{
						IsVisible = true,
						//IsSelected = true,
					};
					info.Selected += InfoOnSelected;
					_tasks.Add(info);
				}
			}
		}

		public Type[] SelectedTasks
		{
			get { return _tasks.Where(i => i.IsSelected).Select(i => i.Task).ToArray(); }
		}

		private void SelectAll_OnClick(object sender, RoutedEventArgs e)
		{
			_tasks.ForEach(i => i.IsSelected = true);
		}

		private void UnSelectAll_OnClick(object sender, RoutedEventArgs e)
		{
			_tasks.ForEach(i => i.IsSelected = false);
		}

		private void InfoOnSelected()
		{
			TryEnableOk();
		}

		private void TryEnableOk()
		{
			OkBtn.IsEnabled = SelectedTasks.Any();
		}
	}
}