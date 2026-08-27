namespace StockSharp.Samples.Advanced.MultiConnect.Avalonia;

using System;

using global::Avalonia.Controls;

using StockSharp.Samples.Advanced.Avalonia;
using StockSharp.Samples.Avalonia;

public partial class MainWindow : Window
{
	private readonly SampleConnectorContext _context;
	private readonly AdvancedConnectorWorkspace _workspace;
	private bool _disposed;

	public MainWindow()
	{
		SampleConnectorContext context = null;
		try
		{
			context = new();
			InitializeComponent();
			_context = context;
			_workspace = new(context);
			this.FindControl<ContentControl>(nameof(WorkspaceHost)).Content = _workspace;
			Opened += OnOpened;
			Closed += OnClosed;
		}
		catch
		{
			context?.Dispose();
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
		TryDispose(_context);
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
