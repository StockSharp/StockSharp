namespace StockSharp.Samples.Advanced.SaveDataLocal.Avalonia;

using System;

using global::Avalonia.Controls;

using StockSharp.Samples.Advanced.Avalonia;

public partial class MainWindow : Window
{
	private readonly LocalStorageSampleRuntime _runtime;
	private readonly AdvancedConnectorWorkspace _workspace;
	private bool _disposed;

	public MainWindow()
	{
		LocalStorageSampleRuntime runtime = null;
		try
		{
			runtime = LocalStorageSampleRuntime.Create();
			InitializeComponent();
			_runtime = runtime;
			_workspace = new(runtime.Context);
			this.FindControl<ContentControl>(nameof(WorkspaceHost)).Content = _workspace;
			Opened += OnOpened;
			Closed += OnClosed;
		}
		catch
		{
			runtime?.Dispose();
			throw;
		}
	}

	private void OnOpened(object sender, EventArgs e)
		=> _workspace.Open();

	private void OnClosed(object sender, EventArgs e)
	{
		if (_disposed)
			return;

		_disposed = true;
		Opened -= OnOpened;
		Closed -= OnClosed;
		TryDispose(_workspace);
		TryDispose(_runtime);
	}

	private static void TryDispose(IDisposable disposable)
	{
		try
		{
			disposable?.Dispose();
		}
		catch
		{
		}
	}
}
