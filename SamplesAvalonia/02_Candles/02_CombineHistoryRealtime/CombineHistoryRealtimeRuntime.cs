namespace StockSharp.Samples.Candles.CombineHistoryRealtime.Avalonia;

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

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
using StockSharp.Samples.Avalonia;

/// <summary>
/// Owns every storage dependency injected into the combined history/realtime connector.
/// </summary>
internal sealed class CombineHistoryRealtimeRuntime : IDisposable
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
		ChannelExecutor executor = null;
		CsvEntityRegistry entityRegistry = null;
		StorageRegistry storageRegistry = null;
		SnapshotRegistry snapshotRegistry = null;
		SampleConnectorContext context = null;

		try
		{
			executor = new ChannelExecutor(ex => ex.LogError(), TimeSpan.FromSeconds(1));
			_ = executor.RunAsync();
			entityRegistry = new(Paths.FileSystem, Paths.HistoryDataPath, executor);
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
			return new(context, lifetime);
		}
		catch (Exception initializationError)
		{
			var errors = new List<Exception> { initializationError };

			if (context is not null)
				TryRelease(context.Dispose, errors);
			if (entityRegistry is not null)
				TryRelease(() => AsyncHelper.Run(entityRegistry.DisposeAsync), errors);
			if (storageRegistry is not null)
				TryRelease(storageRegistry.Dispose, errors);
			if (snapshotRegistry is not null)
				TryRelease(snapshotRegistry.Dispose, errors);
			if (executor is not null)
				TryRelease(() => AsyncHelper.Run(executor.DisposeAsync), errors);

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

	public void Dispose()
		=> _lifetime.Dispose();
}
