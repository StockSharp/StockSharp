namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Logging;

	/// <summary>
	/// Коллекция отладочных сообщений.
	/// </summary>
	public class LogMessageCollection : ThreadSafeObservableCollection<LogMessage>, INotifyPropertyChanged
	{
		/// <summary>
		/// Значение по умолчанию для максимального числа записей для отображения.
		/// </summary>
		public static readonly int DefaultMaxItemsCount = Environment.Is64BitProcess ? 10000 : 1000;

		internal LogMessageCollection()
			: base(new ObservableCollectionEx<LogMessage>())
		{
		}

		/// <summary>
		/// Количество сообщений типа <see cref="LogLevels.Info"/>.
		/// </summary>
		public int InfoCount { get; private set; }

		/// <summary>
		/// Количество сообщений типа <see cref="LogLevels.Warning"/>.
		/// </summary>
		public int WarningCount { get; private set; }

		/// <summary>
		/// Количество сообщений типа <see cref="LogLevels.Error"/>.
		/// </summary>
		public int ErrorCount { get; private set; }

		/// <summary>
		/// Количество сообщений типа <see cref="LogLevels.Debug"/>.
		/// </summary>
		public int DebugCount { get; private set; }

		private void AddMessage(LogMessage message, ref bool isDebug, ref bool isInfo, ref bool isWarning, ref bool isError)
		{
			switch (message.Level)
			{
				case LogLevels.Debug:
					DebugCount++;
					isDebug = true;
					break;

				case LogLevels.Info:
					InfoCount++;
					isInfo = true;
					break;

				case LogLevels.Warning:
					WarningCount++;
					isWarning = true;
					break;

				case LogLevels.Error:
					ErrorCount++;
					isError = true;
					break;
			}
		}

		/// <summary>
		/// Добавить элемент.
		/// </summary>
		/// <param name="item">Новый элемент.</param>
		public override void Add(LogMessage item)
		{
			AddRange(new[] { item });
		}

		/// <summary>
		/// Добавить элементы.
		/// </summary>
		/// <param name="items">Новые элементы.</param>
		public override void AddRange(IEnumerable<LogMessage> items)
		{
			base.AddRange(items);

			bool isDebug = false, isInfo = false, isWarning = false, isError = false;
			items.ForEach(i => AddMessage(i, ref isDebug, ref isInfo, ref isWarning, ref isError));

			if (isDebug)
				OnPropertyChanged("DebugCount");

			if (isInfo)
				OnPropertyChanged("InfoCount");

			if (isWarning)
				OnPropertyChanged("WarningCount");

			if (isError)
				OnPropertyChanged("ErrorCount");
		}

		private void OnPropertyChanged(string propName)
		{
			_propertyChanged.SafeInvoke(this, propName);
		}

		private PropertyChangedEventHandler _propertyChanged;

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { _propertyChanged += value; }
			remove { _propertyChanged -= value; }
		}
	}
}