namespace StockSharp.Samples.Avalonia;

using System;

internal interface ISampleUiDispatcher
{
	bool CheckAccess();

	void Post(Action action);
}

/// <summary>
/// Routes connector callbacks to the UI and drops queued work after window teardown.
/// </summary>
internal sealed class SampleUiEventRouter(ISampleUiDispatcher dispatcher) : IDisposable
{
	private readonly ISampleUiDispatcher _dispatcher = dispatcher
		?? throw new ArgumentNullException(nameof(dispatcher));
	private readonly object _sync = new();
	private bool _disposed;

	public void Dispatch(Action action)
	{
		ArgumentNullException.ThrowIfNull(action);

		lock (_sync)
		{
			if (_disposed)
				return;
		}

		if (_dispatcher.CheckAccess())
			InvokeIfAlive(action);
		else
			_dispatcher.Post(() => InvokeIfAlive(action));
	}

	private void InvokeIfAlive(Action action)
	{
		lock (_sync)
		{
			if (!_disposed)
				action();
		}
	}

	public void Dispose()
	{
		lock (_sync)
			_disposed = true;
	}
}
