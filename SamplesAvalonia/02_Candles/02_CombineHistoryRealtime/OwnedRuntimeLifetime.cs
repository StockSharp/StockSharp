namespace StockSharp.Samples.Candles.CombineHistoryRealtime.Avalonia;

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;

using Ecng.Common;

/// <summary>
/// Releases the connector and its externally owned storage runtime in dependency order.
/// </summary>
internal sealed class OwnedRuntimeLifetime(
	IDisposable connectorContext,
	IAsyncDisposable entityRegistry,
	IDisposable storageRegistry,
	IDisposable snapshotRegistry,
	IAsyncDisposable executor) : IDisposable
{
	private readonly IDisposable _connectorContext = connectorContext
		?? throw new ArgumentNullException(nameof(connectorContext));
	private readonly IAsyncDisposable _entityRegistry = entityRegistry
		?? throw new ArgumentNullException(nameof(entityRegistry));
	private readonly IDisposable _storageRegistry = storageRegistry
		?? throw new ArgumentNullException(nameof(storageRegistry));
	private readonly IDisposable _snapshotRegistry = snapshotRegistry
		?? throw new ArgumentNullException(nameof(snapshotRegistry));
	private readonly IAsyncDisposable _executor = executor
		?? throw new ArgumentNullException(nameof(executor));
	private int _disposed;

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		List<Exception> errors = null;

		TryDispose(_connectorContext.Dispose, ref errors);
		TryDispose(() => AsyncHelper.Run(_entityRegistry.DisposeAsync), ref errors);
		TryDispose(_storageRegistry.Dispose, ref errors);
		TryDispose(_snapshotRegistry.Dispose, ref errors);
		TryDispose(() => AsyncHelper.Run(_executor.DisposeAsync), ref errors);

		if (errors is null)
			return;

		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();

		throw new AggregateException("One or more combined candle runtime resources failed to dispose.", errors);
	}

	private static void TryDispose(Action dispose, ref List<Exception> errors)
	{
		try
		{
			dispose();
		}
		catch (Exception error)
		{
			(errors ??= []).Add(error);
		}
	}
}
