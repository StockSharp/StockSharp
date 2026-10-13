namespace StockSharp.Samples.Candles.CombineHistoryRealtime;

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Releases the connector and its externally owned storage runtime in dependency order.
/// </summary>
internal sealed class OwnedRuntimeLifetime(
	IAsyncDisposable connectorContext,
	IAsyncDisposable entityRegistry,
	IDisposable storageRegistry,
	IDisposable snapshotRegistry,
	IAsyncDisposable executor) : IAsyncDisposable
{
	private readonly IAsyncDisposable _connectorContext = connectorContext
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

	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		var errors = new List<Exception>();

		await TryDisposeAsync(_connectorContext, errors);
		await TryDisposeAsync(_entityRegistry, errors);
		TryDispose(_storageRegistry.Dispose, errors);
		TryDispose(_snapshotRegistry.Dispose, errors);
		await TryDisposeAsync(_executor, errors);

		if (errors.Count == 0)
			return;

		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();

		throw new AggregateException("One or more combined candle runtime resources failed to dispose.", errors);
	}

	private static void TryDispose(Action dispose, List<Exception> errors)
	{
		try
		{
			dispose();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}
	}

	private static async ValueTask TryDisposeAsync(IAsyncDisposable resource, List<Exception> errors)
	{
		try
		{
			await resource.DisposeAsync();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}
	}
}
