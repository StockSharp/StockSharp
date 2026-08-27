namespace StockSharp.Samples.Strategies.LiveTerminal.Avalonia;

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
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;
using StockSharp.Samples.Avalonia;

internal sealed class TerminalConnectorRuntime : IDisposable
{
	private readonly ChannelExecutor _executor;
	private readonly CsvEntityRegistry _entityRegistry;
	private readonly StorageRegistry _storageRegistry;
	private readonly SnapshotRegistry _snapshotRegistry;
	private readonly SemaphoreSlim _initializeGate = new(1, 1);
	private int _initialized;
	private int _disposed;

	private TerminalConnectorRuntime(
		string dataPath,
		ChannelExecutor executor,
		CsvEntityRegistry entityRegistry,
		StorageRegistry storageRegistry,
		SnapshotRegistry snapshotRegistry,
		SampleConnectorContext context)
	{
		DataPath = dataPath;
		_executor = executor;
		_entityRegistry = entityRegistry;
		_storageRegistry = storageRegistry;
		_snapshotRegistry = snapshotRegistry;
		Context = context;
	}

	public string DataPath { get; }

	public IFileSystem FileSystem => Paths.FileSystem;

	public SampleConnectorContext Context { get; }

	public Security ResolveSecurity(string id)
		=> id.IsEmpty() ? null : _entityRegistry.Securities.ReadById(id.ToSecurityId());

	public Portfolio ResolvePortfolio(string name)
		=> name.IsEmpty() ? null : _entityRegistry.Portfolios.ReadById(name);

	public static TerminalConnectorRuntime Create()
	{
		ChannelExecutor executor = null;
		CsvEntityRegistry entityRegistry = null;
		StorageRegistry storageRegistry = null;
		SnapshotRegistry snapshotRegistry = null;
		SampleConnectorContext context = null;

		try
		{
			const string dataPath = "Data";
			var fileSystem = Paths.FileSystem;
			executor = new ChannelExecutor(error => error.LogError(), TimeSpan.FromSeconds(1));
			_ = executor.RunAsync();
			entityRegistry = new CsvEntityRegistry(fileSystem, dataPath, executor);
			var exchangeInfoProvider = new StorageExchangeInfoProvider(entityRegistry);
			storageRegistry = new StorageRegistry(exchangeInfoProvider)
			{
				DefaultDrive = new LocalMarketDataDrive(fileSystem, Path.Combine(dataPath, "Storage")),
			};
			snapshotRegistry = new SnapshotRegistry(fileSystem, Path.Combine(dataPath, "Snapshots"));
			var connector = new Connector(
				entityRegistry.Securities,
				entityRegistry.PositionStorage,
				storageRegistry.ExchangeInfoProvider,
				storageRegistry,
				snapshotRegistry,
				new StorageBuffer())
			{
				CheckSteps = true,
			};
			connector.Adapter.StorageSettings.Mode = StorageModes.Snapshot;
			context = new SampleConnectorContext(connector);
			return new(dataPath, executor, entityRegistry, storageRegistry, snapshotRegistry, context);
		}
		catch (Exception initializationError)
		{
			var errors = new List<Exception> { initializationError };
			if (context is not null)
				TryRelease(context.Dispose, errors);
			TryRelease(entityRegistry is null ? null : () => AsyncHelper.Run(entityRegistry.DisposeAsync), errors);
			if (storageRegistry is not null)
				TryRelease(storageRegistry.Dispose, errors);
			if (snapshotRegistry is not null)
				TryRelease(snapshotRegistry.Dispose, errors);
			TryRelease(executor is null ? null : () => AsyncHelper.Run(executor.DisposeAsync), errors);

			if (errors.Count > 1)
				throw new AggregateException("The live terminal runtime failed to initialize and release its resources.", errors);

			ExceptionDispatchInfo.Capture(initializationError).Throw();
			throw;
		}
	}

	public async Task InitializeAsync(CancellationToken cancellationToken)
	{
		ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
		if (Volatile.Read(ref _initialized) != 0)
			return;

		await _initializeGate.WaitAsync(cancellationToken);
		try
		{
			if (_initialized != 0)
				return;

			var entityErrors = await _entityRegistry.InitAsync(cancellationToken);
			if (entityErrors.Count > 0)
				throw new AggregateException("One or more terminal entities failed to load.", entityErrors.Values);

			await ((ISnapshotRegistry)_snapshotRegistry).InitAsync(cancellationToken);
			Context.Connector.LookupAll();
			Volatile.Write(ref _initialized, 1);
		}
		finally
		{
			_initializeGate.Release();
		}
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		var errors = new List<Exception>();
		TryRelease(Context.Dispose, errors);
		TryRelease(() => AsyncHelper.Run(_entityRegistry.DisposeAsync), errors);
		TryRelease(_storageRegistry.Dispose, errors);
		TryRelease(_snapshotRegistry.Dispose, errors);
		TryRelease(() => AsyncHelper.Run(_executor.DisposeAsync), errors);
		TryRelease(_initializeGate.Dispose, errors);

		if (errors.Count == 1)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (errors.Count > 1)
			throw new AggregateException("One or more live terminal resources failed to dispose.", errors);
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
