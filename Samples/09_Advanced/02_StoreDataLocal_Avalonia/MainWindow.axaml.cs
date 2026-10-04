namespace StockSharp.Samples.Advanced.SaveDataLocal;

using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia.Controls;

using StockSharp.Samples.Advanced;

public partial class MainWindow : Window
{
	private readonly LocalStorageSampleRuntime _runtime;
	private AdvancedConnectorWorkspace _workspace;
	private Task _opening = Task.CompletedTask;
	private bool _isClosing;
	private bool _closeApproved;

	public MainWindow()
	{
		InitializeComponent();

		_runtime = LocalStorageSampleRuntime.Create();
		Opened += OnOpened;
		Closing += OnClosing;
	}

	private async void OnOpened(object sender, EventArgs e)
	{
		_opening = OpenAsync();
		await _opening;
	}

	private async Task OpenAsync()
	{
		await _runtime.InitializeAsync(CancellationToken.None);

		if (_isClosing)
			return;

		// the workspace shows the stored entities, so it is built once they are read
		_workspace = new(_runtime.Context);
		this.FindControl<ContentControl>(nameof(WorkspaceHost)).Content = _workspace;

		await _workspace.OpenAsync(CancellationToken.None);
	}

	private async void OnClosing(object sender, WindowClosingEventArgs e)
	{
		if (_closeApproved)
			return;

		// the window stays open until the runtime has written out what it queued
		e.Cancel = true;

		if (_isClosing)
			return;

		_isClosing = true;
		IsEnabled = false;

		// the opening has to be over before what it opens is released; its failure is reported by the opening itself
		await Task.WhenAny(_opening);

		Opened -= OnOpened;
		TryDispose(_workspace);
		await TryDisposeAsync(_runtime);
		_closeApproved = true;
		Close();
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

	private static async ValueTask TryDisposeAsync(IAsyncDisposable disposable)
	{
		try
		{
			await disposable.DisposeAsync();
		}
		catch
		{
		}
	}
}
