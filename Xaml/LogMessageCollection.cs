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
	/// The debug messages collection.
	/// </summary>
	public class LogMessageCollection : ThreadSafeObservableCollection<LogMessage>, INotifyPropertyChanged
	{
		/// <summary>
		/// The default value for the maximum number of entries to display.
		/// </summary>
		public static readonly int DefaultMaxItemsCount = Environment.Is64BitProcess ? 10000 : 1000;

		internal LogMessageCollection()
			: base(new ObservableCollectionEx<LogMessage>())
		{
		}

		/// <summary>
		/// Number of messages of type <see cref="LogLevels.Info"/>.
		/// </summary>
		public int InfoCount { get; private set; }

		/// <summary>
		/// Number of messages of type <see cref="LogLevels.Warning"/>.
		/// </summary>
		public int WarningCount { get; private set; }

		/// <summary>
		/// Number of messages of type <see cref="LogLevels.Error"/>.
		/// </summary>
		public int ErrorCount { get; private set; }

		/// <summary>
		/// Number of messages of type <see cref="LogLevels.Debug"/>.
		/// </summary>
		public int DebugCount { get; private set; }

		/// <summary>
		/// To add item.
		/// </summary>
		/// <param name="item">New item.</param>
		public override void Add(LogMessage item)
		{
			AddRange(new[] { item });
		}

		/// <summary>
		/// To add items.
		/// </summary>
		/// <param name="items">New items.</param>
		public override void AddRange(IEnumerable<LogMessage> items)
		{
			base.AddRange(items);

			bool isDebug = false, isInfo = false, isWarning = false, isError = false;

			items.ForEach(message =>
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
			});

			if (isDebug)
				OnPropertyChanged("DebugCount");

			if (isInfo)
				OnPropertyChanged("InfoCount");

			if (isWarning)
				OnPropertyChanged("WarningCount");

			if (isError)
				OnPropertyChanged("ErrorCount");
		}

		/// <summary>
		/// To delete all items.
		/// </summary>
		public override void Clear()
		{
			DebugCount = InfoCount = WarningCount = ErrorCount = 0;

			OnPropertyChanged("DebugCount");
			OnPropertyChanged("InfoCount");
			OnPropertyChanged("WarningCount");
			OnPropertyChanged("ErrorCount");

			base.Clear();
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