#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Xaml.Xaml
File: LogControl.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Xaml
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Data;
	using System.Windows.Controls;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Logging;

	/// <summary>
	/// The graphical component for logs displaying.
	/// </summary>
	public partial class LogControl : ILogListener
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LogControl"/>.
		/// </summary>
		public LogControl()
		{
			InitializeComponent();

			Messages = new LogMessageCollection { MaxCount = LogMessageCollection.DefaultMaxItemsCount };

			MessageGrid.SelectionMode = DataGridSelectionMode.Extended;
		}

		#region Dependency properties

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.AutoScroll"/>.
		/// </summary>
		public static readonly DependencyProperty AutoScrollProperty =
			DependencyProperty.Register("AutoScroll", typeof(bool), typeof(LogControl), new PropertyMetadata(false, AutoScrollChanged));

		private static void AutoScrollChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d.FindLogicalChild<LogControl>();
			var autoScroll = (bool)e.NewValue;

			ctrl._autoScroll = autoScroll;
		}

		private bool _autoScroll;

		/// <summary>
		/// Automatically to scroll control on the last row added. The default is off.
		/// </summary>
		public bool AutoScroll
		{
			get { return _autoScroll; }
			set { SetValue(AutoScrollProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.AutoResize"/>.
		/// </summary>
		public static readonly DependencyProperty AutoResizeProperty =
			DependencyProperty.Register("AutoResize", typeof(bool), typeof(LogControl), new PropertyMetadata(false, AutoResizeChanged));

		private static void AutoResizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d.FindLogicalChild<LogControl>();
			var autoResize = (bool)e.NewValue;

			ctrl.SetColumnsWidth(autoResize);
		}

		/// <summary>
		/// Automatically to align the width of the columns by content. The default is off.
		/// </summary>
		public bool AutoResize
		{
			get { return (bool)GetValue(AutoResizeProperty); }
			set { SetValue(AutoResizeProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.MaxItemsCount"/>.
		/// </summary>
		public static readonly DependencyProperty MaxItemsCountProperty =
			DependencyProperty.Register("MaxItemsCount", typeof(int), typeof(LogControl),
				new PropertyMetadata(LogMessageCollection.DefaultMaxItemsCount, MaxItemsCountChanged));

		private static void MaxItemsCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.FindLogicalChild<LogControl>()._messages.MaxCount = (int)e.NewValue;
		}

		/// <summary>
		/// The maximum number of entries to display. The -1 value means an unlimited amount of records. By default, the last 10000 records for 64-bit process and 1000 records for 32-bit process are displayed.
		/// </summary>
		public int MaxItemsCount
		{
			get { return _messages.MaxCount; }
			set { SetValue(MaxItemsCountProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.ShowSourceNameColumn"/>.
		/// </summary>
		public static readonly DependencyProperty ShowSourceNameColumnProperty =
			DependencyProperty.Register("ShowSourceNameColumn", typeof(bool), typeof(LogControl), new PropertyMetadata(true, ShowSourceNameColumnChanged));

		private static void ShowSourceNameColumnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			d.FindLogicalChild<LogControl>().MessageGrid.Columns[0].Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
		}

		/// <summary>
		/// To show the column with the source name. Enabled by default.
		/// </summary>
		public bool ShowSourceNameColumn
		{
			get { return (bool)GetValue(ShowSourceNameColumnProperty); }
			set { SetValue(ShowSourceNameColumnProperty, value); }
		}

		private const string _defaultTimeFormat = "yy/MM/dd HH:mm:ss.fff";

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.TimeFormat"/>.
		/// </summary>
		public static readonly DependencyProperty TimeFormatProperty =
			DependencyProperty.Register("TimeFormat", typeof(string), typeof(LogControl), new PropertyMetadata(_defaultTimeFormat, TimeFormatChanged));

		private static void TimeFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = (LogControl)d;
			var timeFormat = (string)e.NewValue;

			if (timeFormat.IsEmpty())
				throw new ArgumentNullException();

			ctrl._timeFormat = timeFormat;
		}

		private string _timeFormat = _defaultTimeFormat;

		/// <summary>
		/// Format for conversion time into a string. The default format is yy/MM/dd HH:mm:ss.fff.
		/// </summary>
		public string TimeFormat
		{
			get { return _timeFormat; }
			set { SetValue(TimeFormatProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.ShowError"/>.
		/// </summary>
		public static readonly DependencyProperty ShowErrorProperty =
			DependencyProperty.Register("ShowError", typeof(bool), typeof(LogControl), new PropertyMetadata(true, ShowChanged));

		private bool _showError = true;

		/// <summary>
		/// To show messages of type <see cref="LogLevels.Error"/>. Enabled by default.
		/// </summary>
		public bool ShowError
		{
			get { return _showError; }
			set { SetValue(ShowErrorProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.ShowWarning"/>.
		/// </summary>
		public static readonly DependencyProperty ShowWarningProperty =
			DependencyProperty.Register("ShowWarning", typeof(bool), typeof(LogControl), new PropertyMetadata(true, ShowChanged));

		private bool _showWarning = true;

		/// <summary>
		/// To show messages of type <see cref="LogLevels.Warning"/>. Enabled by default.
		/// </summary>
		public bool ShowWarning
		{
			get { return _showWarning; }
			set { SetValue(ShowWarningProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.ShowInfo"/>.
		/// </summary>
		public static readonly DependencyProperty ShowInfoProperty =
			DependencyProperty.Register("ShowInfo", typeof(bool), typeof(LogControl), new PropertyMetadata(true, ShowChanged));

		private bool _showInfo = true;

		/// <summary>
		/// To show messages of type <see cref="LogLevels.Info"/>. Enabled by default.
		/// </summary>
		public bool ShowInfo
		{
			get { return _showInfo; }
			set { SetValue(ShowInfoProperty, value); }
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.ShowDebug"/>.
		/// </summary>
		public static readonly DependencyProperty ShowDebugProperty =
			DependencyProperty.Register("ShowDebug", typeof(bool), typeof(LogControl), new PropertyMetadata(true, ShowChanged));

		private bool _showDebug = true;

		/// <summary>
		/// To show messages of type <see cref="LogLevels.Debug"/>. Enabled by default.
		/// </summary>
		public bool ShowDebug
		{
			get { return _showDebug; }
			set { SetValue(ShowDebugProperty, value); }
		}

		private static void ShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var ctrl = d.FindLogicalChild<LogControl>();
			var newValue = (bool)e.NewValue;

			if (e.Property == ShowDebugProperty)
				ctrl._showDebug = newValue;
			else if (e.Property == ShowErrorProperty)
				ctrl._showError = newValue;
			else if (e.Property == ShowInfoProperty)
				ctrl._showInfo = newValue;
			else if (e.Property == ShowWarningProperty)
				ctrl._showWarning = newValue;

			if (ctrl._view != null)
				ctrl._view.Refresh();
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.Messages"/>.
		/// </summary>
		public static readonly DependencyProperty MessagesProperty =
			DependencyProperty.Register("Messages", typeof(LogMessageCollection), typeof(LogControl), new PropertyMetadata(null, MessagesChanged));

		/// <summary>
		/// The log entries collection.
		/// </summary>
		public LogMessageCollection Messages
		{
			get { return _messages; }
			set { SetValue(MessagesProperty, value); }
		}

		private static void MessagesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var ctrl = (LogControl)sender;
			var value = (LogMessageCollection)args.NewValue;

			ctrl.SetMessagesCollection(value);
		}

		#endregion

		#region Attached properties

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.AutoScroll"/>.
		/// </summary>
		public static readonly DependencyProperty LogAutoScrollProperty = 
			DependencyProperty.RegisterAttached("LogAutoScroll", typeof(bool),  typeof(LogControl), new PropertyMetadata(false, AutoScrollChanged));

		/// <summary>
		/// To set the value for <see cref="LogControl.AutoScroll"/>.
		/// </summary>
		/// <param name="element">Object <see cref="LogControl"/>.</param>
		/// <param name="value">New value for <see cref="LogControl.AutoScroll"/>.</param>
		public static void SetLogAutoScroll(UIElement element, bool value)
		{
			element.SetValue(LogAutoScrollProperty, value);
		}

		/// <summary>
		/// To get the value for <see cref="LogControl.AutoScroll"/>.
		/// </summary>
		/// <param name="element">Object <see cref="LogControl"/>.</param>
		/// <returns>The value of <see cref="LogControl.AutoScroll"/>.</returns>
		public static bool GetLogAutoScroll(UIElement element)
		{
			return (bool)element.GetValue(LogAutoScrollProperty);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.AutoResize"/>.
		/// </summary>
		public static readonly DependencyProperty LogAutoResizeProperty =
			DependencyProperty.Register("LogAutoResize", typeof(bool), typeof(LogControl), new PropertyMetadata(false, AutoResizeChanged));

		/// <summary>
		/// To set the value for <see cref="LogControl.AutoResize"/>.
		/// </summary>
		/// <param name="element">Object <see cref="LogControl"/>.</param>
		/// <param name="value">New value for <see cref="LogControl.AutoResize"/>.</param>
		public static void SetLogAutoResize(UIElement element, bool value)
		{
			element.SetValue(LogAutoResizeProperty, value);
		}

		/// <summary>
		/// To get the value for <see cref="LogControl.AutoResize"/>.
		/// </summary>
		/// <param name="element">Object <see cref="LogControl"/>.</param>
		/// <returns>The value of <see cref="LogControl.AutoResize"/>.</returns>
		public static bool GetLogAutoResize(UIElement element)
		{
			return (bool)element.GetValue(LogAutoResizeProperty);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.MaxItemsCount"/>.
		/// </summary>
		public static readonly DependencyProperty LogMaxItemsCountProperty =
			DependencyProperty.RegisterAttached("LogMaxItemsCount", typeof(int), typeof(LogControl), new PropertyMetadata(LogMessageCollection.DefaultMaxItemsCount, MaxItemsCountChanged));

		/// <summary>
		/// To set the value for <see cref="LogControl.MaxItemsCount"/>.
		/// </summary>
		/// <param name="element">Object <see cref="LogControl"/>.</param>
		/// <param name="value">New value for <see cref="LogControl.MaxItemsCount"/>.</param>
		public static void SetLogMaxItemsCount(UIElement element, int value)
		{
			element.SetValue(LogMaxItemsCountProperty, value);
		}

		/// <summary>
		/// To get the value for <see cref="LogControl.MaxItemsCount"/>.
		/// </summary>
		/// <param name="element">Object <see cref="LogControl"/>.</param>
		/// <returns>The value of <see cref="LogControl.MaxItemsCount"/>.</returns>
		public static int GetLogMaxItemsCount(UIElement element)
		{
			return (int)element.GetValue(LogMaxItemsCountProperty);
		}

		/// <summary>
		/// <see cref="DependencyProperty"/> for <see cref="LogControl.ShowSourceNameColumn"/>.
		/// </summary>
		public static readonly DependencyProperty LogShowSourceNameColumnProperty =
			DependencyProperty.RegisterAttached("LogShowSourceNameColumn", typeof(bool), typeof(LogControl), new PropertyMetadata(ShowSourceNameColumnChanged));

		/// <summary>
		/// To set the value for <see cref="LogControl.ShowSourceNameColumn"/>.
		/// </summary>
		/// <param name="element">Object <see cref="LogControl"/>.</param>
		/// <param name="value">New value for <see cref="LogControl.ShowSourceNameColumn"/>.</param>
		public static void SetLogShowSourceNameColumn(UIElement element, bool value)
		{
			element.SetValue(LogShowSourceNameColumnProperty, value);
		}

		/// <summary>
		/// To get the value for <see cref="LogControl.ShowSourceNameColumn"/>.
		/// </summary>
		/// <param name="element">Object <see cref="LogControl"/>.</param>
		/// <returns>The value of <see cref="LogControl.ShowSourceNameColumn"/>.</returns>
		public static bool GetLogShowSourceNameColumn(UIElement element)
		{
			return (bool)element.GetValue(LogShowSourceNameColumnProperty);
		}

		#endregion

		private LogMessageCollection _messages;
		private ICollectionView _view;

		private void SetMessagesCollection(LogMessageCollection value)
		{
			if (value == null)
				throw new ArgumentNullException();

			if (_messages != null)
				((INotifyCollectionChanged)_messages.Items).CollectionChanged -= MessagesCollectionChanged;

			if (_view != null)
				_view.Filter = null;

			_messages = value;
			((INotifyCollectionChanged)_messages.Items).CollectionChanged += MessagesCollectionChanged;

			_view = CollectionViewSource.GetDefaultView(_messages.Items);
			_view.Filter = MessageFilter;

			MessageGrid.ItemsSource = _messages.Items;

			TryScroll();
		}

		private bool MessageFilter(object obj)
		{
			var message = obj as LogMessage;
			if (message != null)
			{
				switch (message.Level)
				{
					case LogLevels.Debug:
						return ShowDebug;

					case LogLevels.Info:
						return ShowInfo;

					case LogLevels.Warning:
						return ShowWarning;

					case LogLevels.Error:
						return ShowError;
				}
			}

			return false;
		}

		private void SetColumnsWidth(bool auto)
		{
			foreach (var column in MessageGrid.Columns)
			{
				column.Width = auto ? DataGridLength.Auto : column.ActualWidth;
			}
		}

		private void MessagesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			TryScroll();
		}

		private void TryScroll()
		{
			if (!AutoScroll || _messages == null || _messages.Count <= 0 || Visibility != Visibility.Visible)
				return;

			var scroll = MessageGrid.FindVisualChild<ScrollViewer>();
			if (scroll != null)
				scroll.ScrollToEnd();
		}

		void ILogListener.WriteMessages(IEnumerable<LogMessage> messages)
		{
			if (messages == null)
				throw new ArgumentNullException(nameof(messages));

			_messages.AddRange(messages);
		}

		#region Implementation of IPersistable

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			AutoScroll = storage.GetValue("AutoScroll", false);
			AutoResize = storage.GetValue("AutoResize", false);
			ShowSourceNameColumn = storage.GetValue("ShowSourceNameColumn", true);
			MaxItemsCount = storage.GetValue("MaxItemsCount", LogMessageCollection.DefaultMaxItemsCount);
			TimeFormat = storage.GetValue("TimeFormat", _defaultTimeFormat);
			ShowInfo = storage.GetValue("ShowInfo", true);
			ShowError = storage.GetValue("ShowError", true);
			ShowWarning = storage.GetValue("ShowWarning", true);
			ShowDebug = storage.GetValue("ShowDebug", true);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("AutoScroll", AutoScroll);
			storage.SetValue("AutoResize", AutoResize);
			storage.SetValue("ShowSourceNameColumn", ShowSourceNameColumn);
			storage.SetValue("MaxItemsCount", MaxItemsCount);
			storage.SetValue("TimeFormat", TimeFormat);
			storage.SetValue("ShowInfo", ShowInfo);
			storage.SetValue("ShowError", ShowError);
			storage.SetValue("ShowWarning", ShowWarning);
			storage.SetValue("ShowDebug", ShowDebug);
		}

		#endregion

		void IDisposable.Dispose()
		{
		}
	}
}