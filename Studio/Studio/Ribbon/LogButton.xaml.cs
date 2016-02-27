#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Studio.Ribbon.StudioPublic
File: LogButton.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Studio.Ribbon
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Windows;
	using System.Windows.Input;

	using Ecng.Configuration;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.Logging;
	using StockSharp.Studio.Controls;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;

	public partial class LogButton : ILogListener, IStudioControl
	{
		public static readonly RoutedCommand OpenMonitorCommand = new RoutedCommand();
		public static readonly RoutedCommand OpenLogDirectoryCommand = new RoutedCommand();
		public static readonly RoutedCommand MemoryStatisticsCommand = new RoutedCommand();

		public static readonly DependencyProperty HasErrorsProperty = DependencyProperty.Register("HasErrors", typeof(bool), typeof(LogButton), new PropertyMetadata(false));

		private bool HasErrors
		{
			get { return (bool)GetValue(HasErrorsProperty); }
			set { SetValue(HasErrorsProperty, value); }
		}

		public static readonly DependencyProperty IsMemoryStatEnabledProperty = DependencyProperty.Register("IsMemoryStatEnabled", typeof(bool), typeof(LogButton), new PropertyMetadata(false));

		private bool IsMemoryStatEnabled
		{
			get { return (bool)GetValue(IsMemoryStatEnabledProperty); }
			set { SetValue(IsMemoryStatEnabledProperty, value); }
		}

		private bool _logControlOpened;

		public LogButton()
		{
			InitializeComponent();

			if (this.IsDesignMode())
				return;

			ConfigManager
				.GetService<IStudioCommandService>()
				.Register<ControlOpenedCommand>(this, false, cmd =>
				{
					_logControlOpened = cmd.Control.GetType() == typeof(LogManagerPanel);

					if (_logControlOpened)
						GuiDispatcher.GlobalDispatcher.AddAction(() => HasErrors = false);
				});

			Loaded += LogButton_Loaded;
		}

		private void LogButton_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= LogButton_Loaded;
			new AddLogListenerCommand(this).Process(this);
			IsMemoryStatEnabled = MemoryStatistics.IsEnabled;
		}

		private void ExecutedOpenMonitor(object sender, ExecutedRoutedEventArgs e)
		{
			new OpenWindowCommand(typeof(LogManagerPanel).GUID.ToString(), typeof(LogManagerPanel), true).SyncProcess(this);
		}

		private void ExecutedOpenLogDirectory(object sender, ExecutedRoutedEventArgs e)
		{
			var fileListener = ConfigManager.GetService<LogManager>().Listeners.OfType<FileLogListener>().FirstOrDefault();

			if (fileListener != null)
				Process.Start(fileListener.LogDirectory);
		}

		private void ExecutedMemoryStatistics(object sender, ExecutedRoutedEventArgs e)
		{
			MemoryStatistics.AddOrRemove();
			IsMemoryStatEnabled = MemoryStatistics.IsEnabled;
		}

		void ILogListener.WriteMessages(IEnumerable<LogMessage> messages)
		{
			if (_logControlOpened)
				return;

			foreach (var message in messages)
			{
				if (message.Level != LogLevels.Error)
					continue;

				_logControlOpened = true;
				GuiDispatcher.GlobalDispatcher.AddAction(() => HasErrors = true);
			}
		}

		void IPersistable.Load(SettingsStorage storage)
		{
		}

		void IPersistable.Save(SettingsStorage storage)
		{
		}

		void IDisposable.Dispose()
		{
		}

		string IStudioControl.Title => null;

		Uri IStudioControl.Icon => null;
	}
}
