namespace StockSharp.Samples.Misc.Logging.Avalonia;

using System;
using System.Diagnostics;

using global::Avalonia.Controls;
using global::Avalonia.Interactivity;

using Ecng.Common;
using Ecng.Logging;

using StockSharp.Xaml;
using StockSharp.Xaml.Windows.Avalonia;

public partial class MainWindow : Window
{
	private sealed class TestSource : BaseLogReceiver
	{
	}

	private readonly LogManager _logManager;
	private readonly TestSource _testSource;
	private readonly Monitor _monitor;
	private bool _isDisposed;

	public MainWindow()
	{
		InitializeComponent();

		_monitor = this.FindControl<Monitor>(nameof(LogMonitor));
		_logManager = new LogManager
		{
			// Flush immediately so each button press is visible in the lesson.
			FlushInterval = TimeSpan.FromMilliseconds(1),
		};

		_logManager.Sources.Add(_testSource = new TestSource());
		_logManager.Sources.Add(new Ecng.Logging.TraceSource());
		_logManager.Listeners.Add(new GuiLogListener(_monitor));
		_logManager.Listeners.Add(new FileLogListener
		{
			FileName = "logs",
		});

		Closed += OnClosed;
	}

	private void OnTestSourceClick(object sender, RoutedEventArgs e)
	{
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

	private void OnTestTraceClick(object sender, RoutedEventArgs e)
	{
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

	private void OnClosed(object sender, EventArgs e)
	{
		if (_isDisposed)
			return;

		_isDisposed = true;
		Closed -= OnClosed;
		try
		{
			_logManager.Dispose();
		}
		finally
		{
			((IDisposable)_monitor).Dispose();
		}
	}
}
