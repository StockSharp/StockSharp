#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: StrategiesStatisticsPanel.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Windows;
	using System.Windows.Controls;
	using System.Windows.Input;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Xaml;
	using Ecng.Xaml.Grids;

	using MoreLinq;

	using StockSharp.Algo.Statistics;
	using StockSharp.Algo.Strategies;
	using StockSharp.Localization;

	/// <summary>
	/// The visual panel to display parameters <see cref="IStatisticParameter"/> of several strategies.
	/// </summary>
	public partial class StrategiesStatisticsPanel
	{
		private class StrategyItem : NotifiableObject
		{
			public Strategy Strategy { get; }

			public IStrategyParam[] Parameters => Strategy.Parameters.ToArray();

			public IStatisticParameter[] Statistics => Strategy.StatisticManager.Parameters.ToArray();

			private int _progress;

			public int Progress
			{
				get { return _progress; }
				set
				{
					if (_progress == value)
						return;

					_progress = value;
					NotifyChanged("Progress");
				}
			}

			public StrategyItem(Strategy strategy)
			{
				if (strategy == null)
					throw new ArgumentNullException(nameof(strategy));

				Strategy = strategy;
			}
		}

		/// <summary>
		/// Parameters of the strategy which is excluded from the display.
		/// </summary>
		public HashSet<string> ExcludeParameters { get; }

		/// <summary>
		/// To show the Test Progress column.
		/// </summary>
		public bool ShowProgress { get; set; }

		/// <summary>
		/// To show the Name Strategy column.
		/// </summary>
		public bool ShowStrategyName { get; set; }

		/// <summary>
		/// The selected strategy.
		/// </summary>
		public Strategy SelectedStrategy
		{
			get
			{
				var item = (StrategyItem)ResultsGrid.SelectedItem;
				return item == null ? null : item.Strategy;
			}
		}

		/// <summary>
		/// Selected strategies.
		/// </summary>
		public IEnumerable<Strategy> SelectedStrategies
		{
			get { return ResultsGrid.SelectedItems.OfType<StrategyItem>().Select(s => s.Strategy).ToArray(); }
		}

		/// <summary>
		/// Events of double-clicking the mouse on the selected strategy.
		/// </summary>
		public event Action<Strategy> StrategyDoubleClick;

		/// <summary>
		/// The selected strategies change event.
		/// </summary>
		public event Action SelectionChanged;

		private readonly SynchronizedDictionary<Strategy, StrategyItem> _map = new SynchronizedDictionary<Strategy, StrategyItem>(); 
		private readonly ThreadSafeObservableCollection<StrategyItem> _strategies;

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategiesStatisticsPanel"/>.
		/// </summary>
		public StrategiesStatisticsPanel()
		{
			InitializeComponent();

			ShowStrategyName = true;

			ExcludeParameters = new HashSet<string>
			{
				"MaxErrorCount",
				//"Volume"
			};

			var itemsSource = new ObservableCollectionEx<StrategyItem>();
			ResultsGrid.ItemsSource = itemsSource;

			_strategies = new ThreadSafeObservableCollection<StrategyItem>(itemsSource);
		}

		/// <summary>
		/// To add strategies to the table.
		/// </summary>
		/// <param name="strategies">Strategies.</param>
		public void AddStrategies(IEnumerable<Strategy> strategies)
		{
			GuiDispatcher.GlobalDispatcher.AddAction(() =>
			{
				foreach (var strategy in strategies)
				{
					var item = new StrategyItem(strategy);
					_map.Add(strategy, item);
					_strategies.Add(item);
					CreateColumns(strategy);
				}
			});
		}

		/// <summary>
		/// To clear the table.
		/// </summary>
		public void Clear()
		{
			_map.Clear();
			_strategies.Clear();
			ResultsGrid.Columns.Clear();
		}

		private void CreateColumns(Strategy strategy)
		{
			var id = 0;

			if (ShowProgress)
			{
				var progressColumn = (DataGridTextColumn)ResultsGrid.AddTextColumn("Progress", LocalizedStrings.Str1570);
				progressColumn.Binding.StringFormat = "{0}%";
			}

			if (ShowProgress)
				ResultsGrid.AddTextColumn("Strategy.Name", LocalizedStrings.NameKey);

			foreach (var strategyParam in strategy.Parameters)
			{
				var type = strategyParam.Value.GetType();

				if (type.IsNumeric() && !type.IsEnum() && !ExcludeParameters.Contains(strategyParam.Name))
					ResultsGrid.AddTextColumn("Parameters[{0}].Value".Put(id), strategyParam.Name);

				id++;
			}

			id = 0;

			foreach (var statisticParam in strategy.StatisticManager.Parameters)
			{
				var column = (DataGridTextColumn)ResultsGrid.AddTextColumn("Statistics[{0}].Value".Put(id++), statisticParam.DisplayName);
				var valueType = statisticParam.Value.GetType();

				if (valueType.IsNumeric())
					column.Binding.StringFormat = "{0:0.##}";
			}
		}

		/// <summary>
		/// To add a menu item for the table.
		/// </summary>
		/// <param name="menuItem">The menu item.</param>
		public void AddContextMenuItem(object menuItem)
		{
			ResultsGrid.ContextMenu.Items.Add(menuItem);
		}

		/// <summary>
		/// To set the visibility for a column of the table.
		/// </summary>
		/// <param name="name">The field name.</param>
		/// <param name="visibility">The visibility.</param>
		public void SetColumnVisibility(string name, Visibility visibility)
		{
			ResultsGrid
				.Columns
				.Where(c => c.SortMemberPath.CompareIgnoreCase(name))
				.ForEach(c => c.Visibility = visibility);
		}

		/// <summary>
		/// To update the progress for the strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="progress">Progress.</param>
		public void UpdateProgress(Strategy strategy, int progress)
		{
			var info = _map.TryGetValue(strategy);

			if (info == null)
				return;

			info.Progress = progress;
		}

		private void ResultsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var strategy = SelectedStrategy;

			if (strategy == null)
				return;

			StrategyDoubleClick.SafeInvoke(strategy);
		}

		private void ResultsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SelectionChanged.SafeInvoke();
		}
	}
}
