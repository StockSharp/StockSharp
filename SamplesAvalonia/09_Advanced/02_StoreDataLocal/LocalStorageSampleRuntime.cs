namespace StockSharp.Samples.Advanced.SaveDataLocal.Avalonia;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.IO;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;

internal sealed class LocalStorageSampleRuntime : IDisposable
{
	private readonly CsvEntityRegistry _entityRegistry;
	private readonly StorageRegistry _storageRegistry;
	private readonly SnapshotRegistry _snapshotRegistry;
	private readonly CsvNativeIdStorageProvider _nativeIdStorage;
	private readonly ChannelExecutor _executor;
	private int _disposed;

	private LocalStorageSampleRuntime(
		SampleConnectorContext context,
		CsvEntityRegistry entityRegistry,
		StorageRegistry storageRegistry,
		SnapshotRegistry snapshotRegistry,
		CsvNativeIdStorageProvider nativeIdStorage,
		ChannelExecutor executor)
	{
		Context = context;
		_entityRegistry = entityRegistry;
		_storageRegistry = storageRegistry;
		_snapshotRegistry = snapshotRegistry;
		_nativeIdStorage = nativeIdStorage;
		_executor = executor;
	}

	public SampleConnectorContext Context { get; }

	public static LocalStorageSampleRuntime Create()
	{
		ChannelExecutor executor = null;
		CsvEntityRegistry entityRegistry = null;
		StorageRegistry storageRegistry = null;
		SnapshotRegistry snapshotRegistry = null;
		CsvNativeIdStorageProvider nativeIdStorage = null;
		SampleConnectorContext context = null;

		try
		{
			var dataPath = "Data".ToFullPath();
			var fileSystem = Paths.FileSystem;
			executor = new ChannelExecutor(error => error.LogError(), TimeSpan.FromSeconds(1));
			_ = executor.RunAsync();

			entityRegistry = new CsvEntityRegistry(fileSystem, dataPath, executor);
			var exchangeInfoProvider = new StorageExchangeInfoProvider(entityRegistry);
			storageRegistry = new StorageRegistry(exchangeInfoProvider)
			{
				DefaultDrive = new LocalMarketDataDrive(fileSystem, dataPath),
			};
			snapshotRegistry = new SnapshotRegistry(fileSystem, Path.Combine(dataPath, "Snapshots"));
			nativeIdStorage = new CsvNativeIdStorageProvider(fileSystem, Path.Combine(dataPath, "NativeId"), executor);

			var connector = new Connector(
				entityRegistry.Securities,
				entityRegistry.PositionStorage,
				exchangeInfoProvider,
				storageRegistry,
				snapshotRegistry,
				new StorageBuffer());
			context = new SampleConnectorContext(connector);

			AsyncHelper.Run(() => entityRegistry.InitAsync(default));
			AsyncHelper.Run(() => exchangeInfoProvider.InitAsync(default));
			AsyncHelper.Run(() => nativeIdStorage.InitAsync(default));
			AsyncHelper.Run(() => ((ISnapshotRegistry)snapshotRegistry).InitAsync(default));
			connector.Adapter.NativeIdStorage = nativeIdStorage;
			connector.Adapter.StorageSettings.Mode = StorageModes.Snapshot;

			return new LocalStorageSampleRuntime(
				context,
				entityRegistry,
				storageRegistry,
				snapshotRegistry,
				nativeIdStorage,
				executor);
		}
		catch (Exception initializationError)
		{
			var errors = new List<Exception> { initializationError };
			if (context is not null)
				TryRelease(context.Dispose, errors);
			if (nativeIdStorage is not null)
				TryRelease(() => AsyncHelper.Run(nativeIdStorage.DisposeAsync), errors);
			if (entityRegistry is not null)
				TryRelease(() => AsyncHelper.Run(entityRegistry.DisposeAsync), errors);
			if (storageRegistry is not null)
				TryRelease(storageRegistry.Dispose, errors);
			if (snapshotRegistry is not null)
				TryRelease(snapshotRegistry.Dispose, errors);
			if (executor is not null)
				TryRelease(() => AsyncHelper.Run(executor.DisposeAsync), errors);

			if (errors.Count > 1)
				throw new AggregateException("The local-storage sample failed to initialize and release its runtime.", errors);

			ExceptionDispatchInfo.Capture(initializationError).Throw();
			throw;
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		List<Exception> errors = null;
		TryRelease(Context.Dispose, errors ??= []);
		TryRelease(() => AsyncHelper.Run(_nativeIdStorage.DisposeAsync), errors);
		TryRelease(() => AsyncHelper.Run(_entityRegistry.DisposeAsync), errors);
		TryRelease(_storageRegistry.Dispose, errors);
		TryRelease(_snapshotRegistry.Dispose, errors);
		TryRelease(() => AsyncHelper.Run(_executor.DisposeAsync), errors);

		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException("One or more local-storage sample resources failed to dispose.", errors);
	}

	private static void TryRelease(Action release, ICollection<Exception> errors)
	{
		if (release is null)
			return;

		try
		{
			release();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}
	}
}
