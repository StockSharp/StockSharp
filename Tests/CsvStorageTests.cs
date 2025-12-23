namespace StockSharp.Tests;

using System.Collections.Concurrent;

using Ecng.Common;

using StockSharp.Algo.Storages;

[TestClass]
public class CsvStorageTests : BaseTestClass
{
	private static readonly ConcurrentQueue<Exception> _executorErrors = new();

	private static ChannelExecutor CreateExecutor(CancellationToken token)
	{
		while (_executorErrors.TryDequeue(out _))
		{
		}

		var executor = new ChannelExecutor(ex => _executorErrors.Enqueue(ex));
		_ = executor.RunAsync(token);
		return executor;
	}

	private static async Task FlushAsync(ChannelExecutor executor, CancellationToken token)
	{
		var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		executor.Add(() => tcs.TrySetResult());
		await tcs.Task.WaitAsync(token);

		if (!_executorErrors.IsEmpty)
			throw new AggregateException([.. _executorErrors]);
	}

	private static (MemoryFileSystem fs, string path) CreateMemoryFs(string name)
	{
		var fs = new MemoryFileSystem();
		var path = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid().ToString("N"), name);
		return (fs, path);
	}

	#region CsvPortfolioMessageAdapterProvider Tests

	[TestMethod]
	public async Task CsvPortfolioProvider_SetAndGetAdapter()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");
		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);

		await provider.InitAsync(token);

		var adapterId = Guid.NewGuid();
		var result = provider.SetAdapter("TestPortfolio", adapterId);
		await FlushAsync(executor, token);

		IsTrue(result);
		AreEqual(adapterId, provider.TryGetAdapter("TestPortfolio"));
	}

	[TestMethod]
	public async Task CsvPortfolioProvider_UpdateAdapter()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");
		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);

		await provider.InitAsync(token);

		var adapterId1 = Guid.NewGuid();
		var adapterId2 = Guid.NewGuid();

		provider.SetAdapter("TestPortfolio", adapterId1);
		await FlushAsync(executor, token);

		provider.SetAdapter("TestPortfolio", adapterId2);
		await FlushAsync(executor, token);

		AreEqual(adapterId2, provider.TryGetAdapter("TestPortfolio"));
	}

	[TestMethod]
	public async Task CsvPortfolioProvider_RemoveAssociation()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");
		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);

		await provider.InitAsync(token);

		var adapterId = Guid.NewGuid();
		provider.SetAdapter("TestPortfolio", adapterId);
		await FlushAsync(executor, token);

		var removed = provider.RemoveAssociation("TestPortfolio");
		await FlushAsync(executor, token);

		IsTrue(removed);
		IsNull(provider.TryGetAdapter("TestPortfolio"));
	}

	[TestMethod]
	public async Task CsvPortfolioProvider_Persistence()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");

		var adapterId = Guid.NewGuid();

		// Create and save
		var provider1 = new CsvPortfolioMessageAdapterProvider(fs, path, executor);
		await provider1.InitAsync(token);
		provider1.SetAdapter("TestPortfolio", adapterId);
		await FlushAsync(executor, token);

		// Load in new instance
		var provider2 = new CsvPortfolioMessageAdapterProvider(fs, path, executor);
		await provider2.InitAsync(token);

		AreEqual(adapterId, provider2.TryGetAdapter("TestPortfolio"));
	}

	[TestMethod]
	public async Task CsvPortfolioProvider_ChangedEvent()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");
		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);

		await provider.InitAsync(token);

		string changedKey = null;
		Guid changedAdapterId = default;
		var eventFired = false;

		provider.Changed += (key, adapterId, isSet) =>
		{
			changedKey = key;
			changedAdapterId = adapterId;
			eventFired = true;
		};

		var adapterId = Guid.NewGuid();
		provider.SetAdapter("TestPortfolio", adapterId);
		await FlushAsync(executor, token);

		IsTrue(eventFired);
		AreEqual("TestPortfolio", changedKey);
		AreEqual(adapterId, changedAdapterId);
	}

	#endregion

	#region CsvSecurityMessageAdapterProvider Tests

	[TestMethod]
	public async Task CsvSecurityProvider_SetAndGetAdapter()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("securities.csv");
		var provider = new CsvSecurityMessageAdapterProvider(fs, path, executor);

		await provider.InitAsync(token);

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var dataType = DataType.Ticks;
		var adapterId = Guid.NewGuid();

		var result = provider.SetAdapter(secId, dataType, adapterId);
		await FlushAsync(executor, token);

		IsTrue(result);
		AreEqual(adapterId, provider.TryGetAdapter(secId, dataType));
	}

	[TestMethod]
	public async Task CsvSecurityProvider_Persistence()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("securities.csv");

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var dataType = DataType.Ticks;
		var adapterId = Guid.NewGuid();

		// Create and save
		var provider1 = new CsvSecurityMessageAdapterProvider(fs, path, executor);
		await provider1.InitAsync(token);
		provider1.SetAdapter(secId, dataType, adapterId);
		await FlushAsync(executor, token);

		// Load in new instance
		var provider2 = new CsvSecurityMessageAdapterProvider(fs, path, executor);
		await provider2.InitAsync(token);

		AreEqual(adapterId, provider2.TryGetAdapter(secId, dataType));
	}

	[TestMethod]
	public async Task CsvSecurityProvider_RemoveAssociation()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("securities.csv");
		var provider = new CsvSecurityMessageAdapterProvider(fs, path, executor);

		await provider.InitAsync(token);

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var dataType = DataType.Ticks;
		var adapterId = Guid.NewGuid();

		provider.SetAdapter(secId, dataType, adapterId);
		await FlushAsync(executor, token);

		var removed = provider.RemoveAssociation((secId, dataType));
		await FlushAsync(executor, token);

		IsTrue(removed);
		IsNull(provider.TryGetAdapter(secId, dataType));
	}

	#endregion

	#region CsvSecurityMappingStorage Tests

	[TestMethod]
	public async Task CsvSecurityMapping_SaveAndGet()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var storage = new CsvSecurityMappingStorage(fs, path, executor);

		await storage.InitAsync(token);

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "US0378331005", BoardCode = "ISIN" };
		var mapping = new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId };

		var added = storage.Save("TestStorage", mapping);
		await FlushAsync(executor, token);

		IsTrue(added);
		AreEqual(stockSharpId, storage.TryGetStockSharpId("TestStorage", adapterId));
		AreEqual(adapterId, storage.TryGetAdapterId("TestStorage", stockSharpId));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Persistence()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "US0378331005", BoardCode = "ISIN" };
		var mapping = new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId };

		// Create and save
		var storage1 = new CsvSecurityMappingStorage(fs, path, executor);
		await storage1.InitAsync(token);
		storage1.Save("TestStorage", mapping);
		await FlushAsync(executor, token);

		// Load in new instance
		var storage2 = new CsvSecurityMappingStorage(fs, path, executor);
		await storage2.InitAsync(token);

		AreEqual(stockSharpId, storage2.TryGetStockSharpId("TestStorage", adapterId));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Remove()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var storage = new CsvSecurityMappingStorage(fs, path, executor);

		await storage.InitAsync(token);

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "US0378331005", BoardCode = "ISIN" };
		var mapping = new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId };

		storage.Save("TestStorage", mapping);
		await FlushAsync(executor, token);

		var removed = storage.Remove("TestStorage", stockSharpId);
		await FlushAsync(executor, token);

		IsTrue(removed);
		IsNull(storage.TryGetStockSharpId("TestStorage", adapterId));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_GetStorageNames()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var storage = new CsvSecurityMappingStorage(fs, path, executor);

		await storage.InitAsync(token);

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "US0378331005", BoardCode = "ISIN" };

		storage.Save("Storage1", new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });
		storage.Save("Storage2", new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });
		await FlushAsync(executor, token);

		var names = storage.GetStorageNames().ToArray();
		AreEqual(2, names.Length);
		IsTrue(names.Contains("Storage1"));
		IsTrue(names.Contains("Storage2"));
	}

	#endregion

	#region CsvExtendedInfoStorage Tests

	[TestMethod]
	public async Task CsvExtendedInfo_CreateAndAdd()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("extinfo");
		var storage = new CsvExtendedInfoStorage(fs, path, executor);

		await storage.InitAsync(token);

		var fields = new[] { ("Field1", typeof(string)), ("Field2", typeof(int)) };
		var item = await ((IExtendedInfoStorage)storage).CreateAsync("TestStorage", fields, token);
		await FlushAsync(executor, token);

		IsNotNull(item);
		AreEqual("TestStorage", item.StorageName);

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var extInfo = new Dictionary<string, object> { ["Field1"] = "Value1", ["Field2"] = 42 };
		item.Add(secId, extInfo);
		await FlushAsync(executor, token);

		var loaded = item.Load(secId);
		IsNotNull(loaded);
		AreEqual("Value1", loaded["Field1"]);
		AreEqual(42, loaded["Field2"]);
	}

	[TestMethod]
	public async Task CsvExtendedInfo_Persistence()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("extinfo");

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };

		// Create and save
		var storage1 = new CsvExtendedInfoStorage(fs, path, executor);
		await storage1.InitAsync(token);

		var fields = new[] { ("Field1", typeof(string)), ("Field2", typeof(int)) };
		var item1 = await ((IExtendedInfoStorage)storage1).CreateAsync("TestStorage", fields, token);

		var extInfo = new Dictionary<string, object> { ["Field1"] = "Value1", ["Field2"] = 42 };
		item1.Add(secId, extInfo);
		await FlushAsync(executor, token);

		// Load in new instance
		var storage2 = new CsvExtendedInfoStorage(fs, path, executor);
		await storage2.InitAsync(token);

		var item2 = await ((IExtendedInfoStorage)storage2).GetAsync("TestStorage", token);
		IsNotNull(item2);

		var loaded = item2.Load(secId);
		IsNotNull(loaded);
		AreEqual("Value1", loaded["Field1"]);
		AreEqual(42, loaded["Field2"]);
	}

	[TestMethod]
	public async Task CsvExtendedInfo_Delete()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("extinfo");
		var storage = new CsvExtendedInfoStorage(fs, path, executor);

		await storage.InitAsync(token);

		var fields = new[] { ("Field1", typeof(string)) };
		var item = await ((IExtendedInfoStorage)storage).CreateAsync("TestStorage", fields, token);

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		item.Add(secId, new Dictionary<string, object> { ["Field1"] = "Value1" });
		await FlushAsync(executor, token);

		item.Delete(secId);
		await FlushAsync(executor, token);

		var loaded = item.Load(secId);
		IsNull(loaded);
	}

	#endregion

	#region CsvNativeIdStorage Tests

	[TestMethod]
	public async Task CsvNativeId_TryAddAndGet()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var storage = new CsvNativeIdStorage(fs, path, executor);

		await storage.InitAsync(token);

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var nativeId = "12345";

		var added = await storage.TryAddAsync("TestStorage", secId, nativeId, true, token);
		await FlushAsync(executor, token);

		IsTrue(added);
		AreEqual(secId, await storage.TryGetByNativeIdAsync("TestStorage", nativeId, token));
		AreEqual(nativeId, await storage.TryGetBySecurityIdAsync("TestStorage", secId, token));
	}

	[TestMethod]
	public async Task CsvNativeId_Persistence()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var nativeId = "12345";

		// Create and save
		var storage1 = new CsvNativeIdStorage(fs, path, executor);
		await storage1.InitAsync(token);
		await storage1.TryAddAsync("TestStorage", secId, nativeId, true, token);
		await FlushAsync(executor, token);

		// Load in new instance
		var storage2 = new CsvNativeIdStorage(fs, path, executor);
		await storage2.InitAsync(token);

		AreEqual(secId, await storage2.TryGetByNativeIdAsync("TestStorage", nativeId, token));
	}

	[TestMethod]
	public async Task CsvNativeId_Clear()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var storage = new CsvNativeIdStorage(fs, path, executor);

		await storage.InitAsync(token);

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		await storage.TryAddAsync("TestStorage", secId, "12345", true, token);
		await FlushAsync(executor, token);

		await storage.ClearAsync("TestStorage", token);
		await FlushAsync(executor, token);

		var items = await storage.GetAsync("TestStorage", token);
		AreEqual(0, items.Length);
	}

	[TestMethod]
	public async Task CsvNativeId_RemoveBySecurityId()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var storage = new CsvNativeIdStorage(fs, path, executor);

		await storage.InitAsync(token);

		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" };

		await storage.TryAddAsync("TestStorage", secId1, "12345", true, token);
		await storage.TryAddAsync("TestStorage", secId2, "67890", true, token);
		await FlushAsync(executor, token);

		var removed = await storage.RemoveBySecurityIdAsync("TestStorage", secId1, true, token);
		await FlushAsync(executor, token);

		IsTrue(removed);
		IsNull(await storage.TryGetBySecurityIdAsync("TestStorage", secId1, token));
		IsNotNull(await storage.TryGetBySecurityIdAsync("TestStorage", secId2, token));
	}

	[TestMethod]
	public async Task CsvNativeId_RemoveByNativeId()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var storage = new CsvNativeIdStorage(fs, path, executor);

		await storage.InitAsync(token);

		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" };

		await storage.TryAddAsync("TestStorage", secId1, "12345", true, token);
		await storage.TryAddAsync("TestStorage", secId2, "67890", true, token);
		await FlushAsync(executor, token);

		var removed = await storage.RemoveByNativeIdAsync("TestStorage", "12345", true, token);
		await FlushAsync(executor, token);

		IsTrue(removed);
		IsNull(await storage.TryGetByNativeIdAsync("TestStorage", "12345", token));
		IsNotNull(await storage.TryGetByNativeIdAsync("TestStorage", "67890", token));
	}

	[TestMethod]
	public async Task CsvNativeId_TupleNativeId()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var storage = new CsvNativeIdStorage(fs, path, executor);

		await storage.InitAsync(token);

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var nativeId = ("Exchange1", 12345L);

		var added = await storage.TryAddAsync("TestStorage", secId, nativeId, true, token);
		await FlushAsync(executor, token);

		IsTrue(added);
		AreEqual(secId, await storage.TryGetByNativeIdAsync("TestStorage", nativeId, token));
	}

	[TestMethod]
	public async Task CsvNativeId_AddedEvent()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var storage = new CsvNativeIdStorage(fs, path, executor);

		await storage.InitAsync(token);

		string eventStorageName = null;
		SecurityId eventSecId = default;
		object eventNativeId = null;
		var eventFired = false;

		storage.Added += (storageName, secId, nativeId, ct) =>
		{
			eventStorageName = storageName;
			eventSecId = secId;
			eventNativeId = nativeId;
			eventFired = true;
			return default;
		};

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		await storage.TryAddAsync("TestStorage", secId, "12345", true, token);
		await FlushAsync(executor, token);

		IsTrue(eventFired);
		AreEqual("TestStorage", eventStorageName);
		AreEqual(secId, eventSecId);
		AreEqual("12345", eventNativeId);
	}

	#endregion
}
