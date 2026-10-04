namespace StockSharp.Samples.Advanced.MultiConnect;

using System;
using System.Threading;

using Avalonia.Controls;

using StockSharp.Samples.Advanced;
using StockSharp.Samples;

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

	private async void OnOpened(object sender, EventArgs e)
		=> await _workspace.OpenAsync(CancellationToken.None);

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
