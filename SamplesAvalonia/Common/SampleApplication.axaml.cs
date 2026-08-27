namespace StockSharp.Samples.Avalonia;

using System;

using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;
using global::Avalonia.Markup.Xaml;

/// <summary>
/// Shared application bootstrap used by every standalone Avalonia sample head.
/// </summary>
public partial class SampleApplication : Application
{
	private static Func<Window> _mainWindowFactory;

	/// <summary>
	/// Runs a sample with the specified main window.
	/// </summary>
	public static int Run<TWindow>(string[] args)
		where TWindow : Window, new()
	{
		_mainWindowFactory = static () => new TWindow();

		return AppBuilder.Configure<SampleApplication>()
			.UsePlatformDetect()
			.LogToTrace()
			.StartWithClassicDesktopLifetime(args);
	}

	/// <inheritdoc />
	public override void Initialize()
		=> AvaloniaXamlLoader.Load(this);

	/// <inheritdoc />
	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			var factory = _mainWindowFactory
				?? throw new InvalidOperationException("The sample main-window factory was not configured.");
			desktop.MainWindow = factory();
		}

		base.OnFrameworkInitializationCompleted();
	}
}
