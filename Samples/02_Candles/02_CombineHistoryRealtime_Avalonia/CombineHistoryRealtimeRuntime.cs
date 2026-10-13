namespace StockSharp.Samples.Candles.CombineHistoryRealtime;

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.Configuration;
using Ecng.IO;
using Ecng.Logging;
using Ecng.Serialization;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Samples;

/// <summary>
/// Owns every storage dependency injected into the combined history/realtime connector.
/// </summary>
internal sealed class CombineHistoryRealtimeRuntime : IAsyncDisposable
{
	private readonly OwnedRuntimeLifetime _lifetime;

	private CombineHistoryRealtimeRuntime(
		SampleConnectorContext context,
		OwnedRuntimeLifetime lifetime)
	{
		Context = context;
		_lifetime = lifetime;
	}

	public SampleConnectorContext Context { get; }

	public static CombineHistoryRealtimeRuntime Create()
	{
		StorageRegistry storageRegistry = null;
		SnapshotRegistry snapshotRegistry = null;
		SampleConnectorContext context = null;

		try
		{
			var executor = new ChannelExecutor(ex => ex.LogError(), TimeSpan.FromSeconds(1));
			var entityRegistry = new CsvEntityRegistry(Paths.FileSystem, Paths.HistoryDataPath, executor);
			storageRegistry = new();
			storageRegistry.DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath);
			snapshotRegistry = new(Paths.FileSystem, "SnapshotRegistry");
			var connector = new Connector(
				entityRegistry.Securities,
				entityRegistry.PositionStorage,
				new InMemoryExchangeInfoProvider(),
				storageRegistry,
				snapshotRegistry);

			// SampleConnectorContext owns the connector from constructor entry,
			// including the failure path.
			context = new(connector);
			var lifetime = new OwnedRuntimeLifetime(
				context,
				entityRegistry,
				storageRegistry,
				snapshotRegistry,
				executor);

			// The executor is started last: a construction that fails before this point leaves nothing
			// running and nothing queued, so the registry and the executor have nothing to release.
			_ = executor.RunAsync();
			return new(context, lifetime);
		}
		catch (Exception initializationError)
		{
			var errors = new List<Exception> { initializationError };

			if (context is not null)
				TryRelease(context.Dispose, errors);
			if (storageRegistry is not null)
				TryRelease(storageRegistry.Dispose, errors);
			if (snapshotRegistry is not null)
				TryRelease(snapshotRegistry.Dispose, errors);

			if (errors.Count > 1)
				throw new AggregateException(
					"The combined candle runtime failed to initialize and release its resources.",
					errors);

			ExceptionDispatchInfo.Capture(initializationError).Throw();
			throw;
		}
	}

	private static void TryRelease(Action release, ICollection<Exception> errors)
	{
		try
		{
			release();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}
	}

	public ValueTask DisposeAsync()
		=> _lifetime.DisposeAsync();
}
