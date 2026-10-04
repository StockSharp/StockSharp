namespace StockSharp.Samples.Advanced.SaveDataLocal;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.ComponentModel;
using Ecng.IO;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Storages.Csv;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Samples;

internal sealed class LocalStorageSampleRuntime : IAsyncDisposable
{
	private readonly CsvEntityRegistry _entityRegistry;
	private readonly StorageExchangeInfoProvider _exchangeInfoProvider;
	private readonly StorageRegistry _storageRegistry;
	private readonly SnapshotRegistry _snapshotRegistry;
	private readonly CsvNativeIdStorageProvider _nativeIdStorage;
	private readonly ChannelExecutor _executor;
	private int _disposed;

	private LocalStorageSampleRuntime(
		SampleConnectorContext context,
		CsvEntityRegistry entityRegistry,
		StorageExchangeInfoProvider exchangeInfoProvider,
		StorageRegistry storageRegistry,
		SnapshotRegistry snapshotRegistry,
		CsvNativeIdStorageProvider nativeIdStorage,
		ChannelExecutor executor)
	{
		Context = context;
		_entityRegistry = entityRegistry;
		_exchangeInfoProvider = exchangeInfoProvider;
		_storageRegistry = storageRegistry;
		_snapshotRegistry = snapshotRegistry;
		_nativeIdStorage = nativeIdStorage;
		_executor = executor;
	}

	public SampleConnectorContext Context { get; }

	public static LocalStorageSampleRuntime Create()
	{
		StorageRegistry storageRegistry = null;
		SnapshotRegistry snapshotRegistry = null;
		SampleConnectorContext context = null;

		try
		{
			var dataPath = "Data".ToFullPath();
			var fileSystem = Paths.FileSystem;
			var executor = new ChannelExecutor(error => error.LogError(), TimeSpan.FromSeconds(1));

			var entityRegistry = new CsvEntityRegistry(fileSystem, dataPath, executor);
			var exchangeInfoProvider = new StorageExchangeInfoProvider(entityRegistry);
			storageRegistry = new StorageRegistry(exchangeInfoProvider)
			{
				DefaultDrive = new LocalMarketDataDrive(fileSystem, dataPath),
			};
			snapshotRegistry = new SnapshotRegistry(fileSystem, Path.Combine(dataPath, "Snapshots"));
			var nativeIdStorage = new CsvNativeIdStorageProvider(fileSystem, Path.Combine(dataPath, "NativeId"), executor);

			var connector = new Connector(
				entityRegistry.Securities,
				entityRegistry.PositionStorage,
				exchangeInfoProvider,
				storageRegistry,
				snapshotRegistry,
				new StorageBuffer());
			context = new SampleConnectorContext(connector);

			// The executor is started last: a construction that fails before this point leaves nothing
			// running and nothing queued, so the storages and the executor have nothing to release.
			_ = executor.RunAsync();

			return new LocalStorageSampleRuntime(
				context,
				entityRegistry,
				exchangeInfoProvider,
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
			if (storageRegistry is not null)
				TryRelease(storageRegistry.Dispose, errors);
			if (snapshotRegistry is not null)
				TryRelease(snapshotRegistry.Dispose, errors);

			if (errors.Count > 1)
				throw new AggregateException("The local-storage sample failed to initialize and release its runtime.", errors);

			ExceptionDispatchInfo.Capture(initializationError).Throw();
			throw;
		}
	}

	/// <summary>
	/// Reads the stored entities and gives the connector the storages that were read.
	/// </summary>
	public async Task InitializeAsync(CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

		await _entityRegistry.InitAsync(cancellationToken);
		await _exchangeInfoProvider.InitAsync(cancellationToken);
		await _nativeIdStorage.InitAsync(cancellationToken);
		await ((ISnapshotRegistry)_snapshotRegistry).InitAsync(cancellationToken);

		var adapter = Context.Connector.Adapter;
		adapter.NativeIdStorage = _nativeIdStorage;
		adapter.StorageSettings.Mode = StorageModes.Snapshot;
	}

	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		var errors = new List<Exception>();
		await TryReleaseAsync(Context, errors);
		await TryReleaseAsync(_nativeIdStorage, errors);
		await TryReleaseAsync(_entityRegistry, errors);
		TryRelease(_storageRegistry.Dispose, errors);
		TryRelease(_snapshotRegistry.Dispose, errors);
		await TryReleaseAsync(_executor, errors);

		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException("One or more local-storage sample resources failed to dispose.", errors);
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

	private static async ValueTask TryReleaseAsync(IAsyncDisposable resource, ICollection<Exception> errors)
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
