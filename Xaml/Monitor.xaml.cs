namespace StockSharp.Xaml
{
	using System;
	using System.Globalization;
	using System.Linq;
	using System.Windows;
	using System.Windows.Data;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using Hardcodet.Wpf.GenericTreeView;

	using MoreLinq;

	using StockSharp.Logging;
	using StockSharp.Algo;
	using StockSharp.Algo.Strategies;
	using StockSharp.Localization;

	/// <summary>
	/// Компонент для мониторинга работы торговых стратегий.
	/// </summary>
	public partial class Monitor : ILogListener
	{
		private static readonly MemoryStatisticsValue<LogMessage> _msgStat = new MemoryStatisticsValue<LogMessage>(LocalizedStrings.Str1565);

		static Monitor()
		{
			MemoryStatistics.Instance.Values.Add(_msgStat);
		}

		private class NodeInfo : Tuple<LogMessageCollection, LogSourceNode>
		{
			public NodeInfo(LogSourceNode node)
				: base(new LogMessageCollection(), node)
			{
			}
		}

		private readonly Dictionary<Guid, NodeInfo> _logInfo = new Dictionary<Guid, NodeInfo>();
		private int _totalMessageCount;

		/// <summary>
		/// Создать <see cref="Monitor"/>.
		/// </summary>
		public Monitor()
		{
			InitializeComponent();

			_logInfo.Add(SourcesTree.CoreRootNode.Key, new NodeInfo(SourcesTree.CoreRootNode));
			_logInfo.Add(SourcesTree.StrategyRootNode.Key, new NodeInfo(SourcesTree.StrategyRootNode));

			SourcesTree.SelectedItem = SourcesTree.StrategyRootNode;
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="ShowStrategies"/>.
		/// </summary>
		public static readonly DependencyProperty ShowStrategiesProperty =
			DependencyProperty.Register("ShowStrategies", typeof(bool), typeof(Monitor), new PropertyMetadata(true, ShowStrategiesPropertyChanged));

		private static void ShowStrategiesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var monitor = d.FindLogicalChild<Monitor>();

			monitor._showStrategies = (bool)e.NewValue;

			var tree = monitor.SourcesTree;

			if (monitor._showStrategies)
				((IList<LogSourceNode>)tree.Items).Add(tree.StrategyRootNode);
			else
			{
				((IList<LogSourceNode>)tree.Items).Remove(tree.StrategyRootNode);
				tree.SelectedItem = tree.CoreRootNode;
			}
		}

		private bool _showStrategies = true;

		/// <summary>
		/// Показывать узел 'Стратегии'. По умолчанию включено.
		/// </summary>
		public bool ShowStrategies
		{
			get { return _showStrategies; }
			set { SetValue(ShowStrategiesProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> для <see cref="MaxItemsCount"/>.
		/// </summary>
		public static readonly DependencyProperty MaxItemsCountProperty = DependencyProperty.Register("MaxItemsCount", typeof(int), typeof(Monitor),
				new PropertyMetadata(LogMessageCollection.DefaultMaxItemsCount, MaxItemsCountChanged));

		private static void MaxItemsCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.FindLogicalChild<Monitor>()._maxItemsCount = (int)e.NewValue;
		}

		private int _maxItemsCount = LogMessageCollection.DefaultMaxItemsCount;

		/// <summary>
		/// Максимальное число записей для отображения. Значение -1 обозначает бесконечный объем записей.
		/// По умолчанию отображается последние 10000 записей для 64 битного процесса
		/// и 1000 записей для 32 битного процесса.
		/// </summary>
		public int MaxItemsCount
		{
			get { return _maxItemsCount; }
			set { SetValue(MaxItemsCountProperty, value); }
		}

		/// <summary>
		/// Графический компонент для отображения логов.
		/// </summary>
		public LogControl LogControl { get { return LogCtrl; } }

		/// <summary>
		/// Записать сообщение.
		/// </summary>
		/// <param name="message">Отладочное сообщение.</param>
		public void WriteMessage(LogMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			WriteMessages(message.Source, new[] { message });
		}

		/// <summary>
		/// Записать сообщения.
		/// </summary>
		/// <param name="messages">Отладочные сообщения.</param>
		public void WriteMessages(IEnumerable<LogMessage> messages)
		{
			if (messages == null)
				throw new ArgumentNullException("messages");
			
			messages.GroupBy(m => m.Source).ForEach(g => WriteMessages(g.Key, g));

			CheckCount();
		}

		private NodeInfo EnsureBuildNodes(ILogSource source)
		{
			var info = _logInfo.TryGetValue(source.Id);

			if (info != null)
				return info;

			// если новый источник данных
			var newSources = new Stack<ILogSource>();
			newSources.Push(source);

			var root = source.Parent;

			// ищем корневую ноду, которая уже была ранее добавлена
			while (root != null && !_logInfo.ContainsKey(root.Id))
			{
				newSources.Push(root);
				root = root.Parent;
			}

			LogSourceNode parentNode;

			// если корневой ноды нет, то создаем ее, основываясь на корневом источнике
			if (root == null)
			{
				parentNode = (newSources.Peek() is Strategy)
					? SourcesTree.StrategyRootNode
					: SourcesTree.CoreRootNode;
			}
			else
				parentNode = _logInfo[root.Id].Item2;

			// добавляем поочередно ноды для новых источников
			foreach (var newSource in newSources)
			{
				var sourceNode = new LogSourceNode(newSource.Id, newSource.Name, parentNode);
				parentNode.ChildNodes.Add(sourceNode);
				parentNode = sourceNode;
				_logInfo.Add(newSource.Id, new NodeInfo(sourceNode));
			}

			return _logInfo[source.Id];
		}

		private void WriteMessages(ILogSource source, IEnumerable<LogMessage> messages)
		{
			var currentNode = EnsureBuildNodes(source).Item2;

			// записываем одно и то же сообщение в родительские ноды,
			// чтобы просматривать через родительский источник как его сообщение,
			// так и всех дочерних источников

			var messagesCache = messages.ToArray();

			while (currentNode != null)
			{
				var list = _logInfo[currentNode.Key].Item1;

				list.AddRange(messagesCache);

				_msgStat.Add(messagesCache);
				_totalMessageCount += messagesCache.Length;

				currentNode = currentNode.ParentNode;
			}
		}

		private void CheckCount()
		{
			if (MaxItemsCount == -1 || _totalMessageCount < (1.5 * MaxItemsCount))
				return;

			var removedCount = 0;

			var countToRemove = _totalMessageCount - MaxItemsCount;

			foreach (var nodeInfo in _logInfo.Values)
			{
				var list = nodeInfo.Item1;
				var count = (int)(list.Count * (countToRemove / (double)_totalMessageCount));

				if (count <= 0)
					continue;

				var removed = list.RemoveRange(0, count);
				_msgStat.Remove(removed);

				removedCount += removed;
			}

			_totalMessageCount -= removedCount;
		}

		private void OnSelectedItemChanged(object sender, RoutedTreeItemEventArgs<LogSourceNode> e)
		{
			LogCtrl.Messages = e.NewItem != null
				? _logInfo[e.NewItem.Key].Item1
				: new LogMessageCollection();
		}

		#region Implementation of IPersistable

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			var settings = storage.GetValue<SettingsStorage>("LogControl");

			if (settings != null)
				LogCtrl.Load(settings);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("LogControl", LogCtrl.Save());
		}

		#endregion
	}

	class SourceStateConverter : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var strategy = value as Strategy;

			if (strategy == null)
				return -1;

			switch (strategy.ProcessState)
			{
				case ProcessStates.Started:
					return 0;
				case ProcessStates.Stopping:
					return 1;
				case ProcessStates.Stopped:
					return 2;
				default:
					return -1;
			}
		}

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var num = (int)value;

			switch (num)
			{
				case 0:
					return ProcessStates.Started;
				case 1:
					return ProcessStates.Stopping;
				case 2:
					return ProcessStates.Stopped;
				default:
					return null;
			}
		}
	}
}