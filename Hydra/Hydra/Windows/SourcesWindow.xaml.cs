#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Hydra.Windows.HydraPublic
File: SourcesWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Hydra.Windows
{
	using System;
	using System.Collections.Generic;
	using System.Collections.ObjectModel;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Hydra.Core;

	public partial class SourcesWindow
	{
		private readonly ObservableCollection<TaskInfo> _tasks = new ObservableCollection<TaskInfo>();
		private readonly Dictionary<CheckBox, TaskCategories> _categories;

		private TaskCategories[] _lastCategories = ArrayHelper.Empty<TaskCategories>();

		public SourcesWindow()
		{
			InitializeComponent();

			TasksCtrl.ItemsSource = _tasks;

			_categories = new Dictionary<CheckBox, TaskCategories>
			{
				{ Russia, TaskCategories.Russia },
				{ America, TaskCategories.America },
				{ History, TaskCategories.History },
				{ RealTime, TaskCategories.RealTime },
				{ Stock, TaskCategories.Stock },
				{ Forex, TaskCategories.Forex },
				{ Bitcoins, TaskCategories.Crypto },
				{ Ticks, TaskCategories.Ticks },
				{ Candles, TaskCategories.Candles },
				{ Depths, TaskCategories.MarketDepth },
				{ Level1, TaskCategories.Level1 },
				{ OrderLog, TaskCategories.OrderLog },
				{ News, TaskCategories.News },
				{ Transactions, TaskCategories.Transactions },
				{ Free, TaskCategories.Free },
				{ Paid, TaskCategories.Paid },
			};
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
						IsVisible = IsTaskVisible(task),
						//IsSelected = true,
					};
					info.Selected += InfoOnSelected;
					_tasks.Add(info);
				}
			}
		}

		private IEnumerable<TaskInfo> VisibleTasks
		{
			get { return _tasks.Where(i => i.IsVisible); }
		}

		public Type[] SelectedTasks
		{
			get { return VisibleTasks.Where(i => i.IsSelected).Select(i => i.Task).ToArray(); }
		}

		private void OnFilterChanged(object sender, RoutedEventArgs e)
		{
			if (_categories == null)
				return;

			_lastCategories = _categories
				.Where(p => p.Key.IsChecked == true)
				.Select(p => p.Value)
				.ToArray();

			RefreshTasks();

			TryEnableOk();
		}

		private void RefreshTasks()
		{
			_tasks.ForEach(t => t.IsVisible = IsTaskVisible(t.Task));
		}

		private bool IsTaskVisible(Type type)
		{
			return (_lastCategories.IsEmpty() || _lastCategories.All(type.IsCategoryOf)) &&
				(NameLike.Text.IsEmpty() || type.GetDisplayName().ContainsIgnoreCase(NameLike.Text));
		}

		private void SelectAll_OnClick(object sender, RoutedEventArgs e)
		{
			VisibleTasks.ForEach(i => i.IsSelected = true);
		}

		private void UnSelectAll_OnClick(object sender, RoutedEventArgs e)
		{
			VisibleTasks.ForEach(i => i.IsSelected = false);
		}

		private void InfoOnSelected()
		{
			TryEnableOk();
		}

		private void TryEnableOk()
		{
			OkBtn.IsEnabled = SelectedTasks.Any();
		}

		private void NameLike_OnTextChanged(object sender, RoutedEventArgs e)
		{
			RefreshTasks();
		}
	}
}