#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: SampleLogging.SampleLoggingPublic
File: MainWindow.xaml.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace SampleLogging
{
	using System;
	using System.Diagnostics;
	using System.Windows;

	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private class TestSource : BaseLogReceiver
		{
		}

		private readonly LogManager _logManager = new LogManager();
		private readonly TestSource _testSource;

		public MainWindow()
		{
			InitializeComponent();

			// immediate flush
			_logManager.FlushInterval = TimeSpan.FromMilliseconds(1);

			// set test log source
			_logManager.Sources.Add(_testSource = new TestSource());

			// set .NET Trace system based source
			_logManager.Sources.Add(new StockSharp.Logging.TraceSource());

			// write logs into MainWindow
			_logManager.Listeners.Add(new GuiLogListener(Monitor));

			// and file logs.txt
			_logManager.Listeners.Add(new FileLogListener
			{
				FileName = "logs",
			});
		}

		private void TestSource_OnClick(object sender, RoutedEventArgs e)
		{
			// push randomly log's event from test source
			//

			var level = RandomGen.GetEnum<LogLevels>();

			switch (level)
			{
				case LogLevels.Inherit:
				case LogLevels.Debug:
				case LogLevels.Info:
				case LogLevels.Off:
				case LogLevels.Verbose:
					_testSource.AddInfoLog("{0} (source)!!!".Put(level));
					break;
				case LogLevels.Warning:
					_testSource.AddWarningLog("Warning (source)!!!");
					break;
				case LogLevels.Error:
					_testSource.AddErrorLog("Error (source)!!!");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void TestTrace_OnClick(object sender, RoutedEventArgs e)
		{
			// push randomly log's event from .NET Trace system
			//

			var level = RandomGen.GetEnum<LogLevels>();

			switch (level)
			{
				case LogLevels.Inherit:
				case LogLevels.Debug:
				case LogLevels.Info:
				case LogLevels.Off:
				case LogLevels.Verbose:
					Trace.TraceInformation("{0} (trace)!!!".Put(level));
					break;
				case LogLevels.Warning:
					Trace.TraceWarning("Warning (trace)!!!");
					break;
				case LogLevels.Error:
					Trace.TraceError("Error (trace)!!!");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}