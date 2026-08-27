namespace StockSharp.Samples.Avalonia;

using System;
using System.Threading;

/// <summary>
/// Idempotent event attachment with deterministic teardown.
/// </summary>
internal sealed class EventSubscription(Action attach, Action detach) : IDisposable
{
	private readonly Action _attach = attach ?? throw new ArgumentNullException(nameof(attach));
	private readonly Action _detach = detach ?? throw new ArgumentNullException(nameof(detach));
	private int _state;

	public void Attach()
	{
		if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
			_attach();
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _state, 2) == 1)
			_detach();
	}
}
