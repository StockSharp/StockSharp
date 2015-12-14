#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Windows.HydraPublic
File: ToolsWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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
					throw new ArgumentNullException(nameof(value));

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