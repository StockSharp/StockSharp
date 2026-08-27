namespace StockSharp.Samples.Avalonia;

using System;

using global::Avalonia.Threading;

internal sealed class AvaloniaSampleUiDispatcher : ISampleUiDispatcher
{
	public static AvaloniaSampleUiDispatcher Instance { get; } = new();

	private AvaloniaSampleUiDispatcher()
	{
	}

	public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

	public void Post(Action action) => Dispatcher.UIThread.Post(action);
}
