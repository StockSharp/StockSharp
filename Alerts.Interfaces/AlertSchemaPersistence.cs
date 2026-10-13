namespace StockSharp.Alerts;

using System.Threading.Channels;

/// <summary>
/// Keeps the alert schemas a user set up across runs: brings back what was stored when the host starts,
/// and writes them again whenever one is added or removed.
/// </summary>
/// <remarks>
/// Writes happen on a loop of their own, so a change made while one is being written is written after
/// it, and several changes made together are written once.
/// </remarks>
public sealed class AlertSchemaPersistence : IDisposable, IAsyncDisposable
{
	private readonly IAlertProcessingService _service;
	private readonly Action<SettingsStorage> _save;
	private readonly Action<Exception> _reportFailure;
	private readonly Channel<bool> _changes = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
	{
		FullMode = BoundedChannelFullMode.DropWrite,
		SingleReader = true,
	});
	private readonly Task _writing;
	private int _isDisposed;

	private AlertSchemaPersistence(IAlertProcessingService service, Action<SettingsStorage> save, Action<Exception> reportFailure)
	{
		_service = service;
		_save = save;
		_reportFailure = reportFailure;

		_service.Registered += OnChanged;
		_service.UnRegistered += OnChanged;

		_writing = WriteAsync();
	}

	/// <summary>
	/// Restores what was stored and starts keeping it up to date.
	/// </summary>
	/// <param name="service">The engine whose schemas are kept.</param>
	/// <param name="load">Reads what was stored, or returns <see langword="null"/> when nothing was.</param>
	/// <param name="save">Writes the schemas.</param>
	/// <param name="reportFailure">Told when stored schemas could not be read or written.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>The running persistence, to be disposed when the host stops.</returns>
	public static async Task<AlertSchemaPersistence> StartAsync(
		IAlertProcessingService service,
		Func<SettingsStorage> load,
		Action<SettingsStorage> save,
		Action<Exception> reportFailure,
		CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(service);
		ArgumentNullException.ThrowIfNull(load);
		ArgumentNullException.ThrowIfNull(save);
		ArgumentNullException.ThrowIfNull(reportFailure);

		if (load() is { } settings)
		{
			try
			{
				await Do.InvariantAsync(() => service.LoadAsync(settings, cancellationToken));
			}
			catch (Exception error) when (error is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
			{
				// Stored settings are as often as not from an earlier version of the application. A schema
				// that can no longer be read costs the user that schema; it must not cost them the host.
				reportFailure(error);
			}
		}

		return new(service, save, reportFailure);
	}

	/// <summary>
	/// Stops following changes. A write already under way finishes by itself.
	/// </summary>
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _isDisposed, 1) != 0)
			return;

		_service.Registered -= OnChanged;
		_service.UnRegistered -= OnChanged;

		_changes.Writer.TryComplete();
	}

	/// <summary>
	/// Stops following changes and waits for the last of them to be written.
	/// </summary>
	/// <returns><see cref="ValueTask"/></returns>
	public async ValueTask DisposeAsync()
	{
		Dispose();

		await _writing;
	}

	private void OnChanged(AlertSchema schema)
		=> _changes.Writer.TryWrite(true);

	private async Task WriteAsync()
	{
		await foreach (var _ in _changes.Reader.ReadAllAsync())
		{
			try
			{
				var storage = new SettingsStorage();

				await _service.SaveAsync(storage, CancellationToken.None);

				_save(storage);
			}
			catch (Exception error)
			{
				// One write that failed is no reason to stop writing the next change.
				_reportFailure(error);
			}
		}
	}
}
