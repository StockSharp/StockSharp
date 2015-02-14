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
	/// Визуальная панель для отображения параметров <see cref="IStatisticParameter"/> нескольких стратегий.
	/// </summary>
	public partial class StrategiesStatisticsPanel
	{
		private class StrategyItem : NotifiableObject
		{
			public Strategy Strategy { get; private set; }

			public IStrategyParam[] Parameters { get { return Strategy.Parameters.ToArray(); } }

			public IStatisticParameter[] Statistics { get { return Strategy.StatisticManager.Parameters.ToArray(); } }

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
					throw new ArgumentNullException("strategy");

				Strategy = strategy;
			}
		}

		/// <summary>
		/// Параметры стратегии исключенных из показа.
		/// </summary>
		public HashSet<string> ExcludeParameters { get; private set; }

		/// <summary>
		/// Показывать столбец Прогресс тестирования.
		/// </summary>
		public bool ShowProgress { get; set; }

		/// <summary>
		/// Показывать столбец Название стратегии.
		/// </summary>
		public bool ShowStrategyName { get; set; }

		/// <summary>
		/// Выбранная стратегия.
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
		/// Выбранные стратегии.
		/// </summary>
		public IEnumerable<Strategy> SelectedStrategies
		{
			get { return ResultsGrid.SelectedItems.OfType<StrategyItem>().Select(s => s.Strategy).ToArray(); }
		}

		/// <summary>
		/// События двойного нажатия мышкой на выбранную стратегию.
		/// </summary>
		public event Action<Strategy> StrategyDoubleClick;

		/// <summary>
		/// Событие изменения выбранных стратегий.
		/// </summary>
		public event Action SelectionChanged;

		private readonly SynchronizedDictionary<Strategy, StrategyItem> _map = new SynchronizedDictionary<Strategy, StrategyItem>(); 
		private readonly ThreadSafeObservableCollection<StrategyItem> _strategies;

		/// <summary>
		/// Создать <see cref="StrategiesStatisticsPanel"/>.
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
		/// Добавить стратегии в таблицу.
		/// </summary>
		/// <param name="strategies">Стратегии.</param>
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
		/// Очистить таблицу.
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
		/// Добавить пункт меню для таблицы.
		/// </summary>
		/// <param name="menuItem">Пункт меню.</param>
		public void AddContextMenuItem(object menuItem)
		{
			ResultsGrid.ContextMenu.Items.Add(menuItem);
		}

		/// <summary>
		/// Установить видимость для столбца таблицы.
		/// </summary>
		/// <param name="name">Имя поля.</param>
		/// <param name="visibility">Видимость.</param>
		public void SetColumnVisibility(string name, Visibility visibility)
		{
			ResultsGrid
				.Columns
				.Where(c => c.SortMemberPath.CompareIgnoreCase(name))
				.ForEach(c => c.Visibility = visibility);
		}

		/// <summary>
		/// Обновить прогресс для стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="progress">Прогресс.</param>
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
