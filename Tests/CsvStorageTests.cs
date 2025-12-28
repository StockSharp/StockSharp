namespace StockSharp.Tests;

using System.Collections.Concurrent;
using System.Text;

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

	#region CsvSecurityMappingStorageProvider Tests

	[TestMethod]
	public async Task CsvSecurityMapping_SaveAndGet()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "US0378331005", BoardCode = "ISIN" };
		var mapping = new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId };

		var added = provider.GetStorage("TestStorage").Save(mapping);
		await FlushAsync(executor, token);

		IsTrue(added);
		AreEqual(stockSharpId, provider.GetStorage("TestStorage").TryGetStockSharpId(adapterId));
		AreEqual(adapterId, provider.GetStorage("TestStorage").TryGetAdapterId(stockSharpId));
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
		var provider1 = new CsvSecurityMappingStorageProvider(fs, path, executor);
		await provider1.InitAsync(token);
		provider1.GetStorage("TestStorage").Save(mapping);
		await FlushAsync(executor, token);

		// Load in new instance
		var provider2 = new CsvSecurityMappingStorageProvider(fs, path, executor);
		await provider2.InitAsync(token);
		AreEqual(stockSharpId, provider2.GetStorage("TestStorage").TryGetStockSharpId(adapterId));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Remove()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "US0378331005", BoardCode = "ISIN" };
		var mapping = new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId };

		provider.GetStorage("TestStorage").Save(mapping);
		await FlushAsync(executor, token);

		var removed = provider.GetStorage("TestStorage").Remove(stockSharpId);
		await FlushAsync(executor, token);

		IsTrue(removed);
		IsNull(provider.GetStorage("TestStorage").TryGetStockSharpId(adapterId));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_GetStorageNames()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "US0378331005", BoardCode = "ISIN" };

		provider.GetStorage("Storage1").Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });
		provider.GetStorage("Storage2").Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });
		await FlushAsync(executor, token);

		var names = provider.StorageNames.ToArray();
		AreEqual(2, names.Length);
		IsTrue(names.Contains("Storage1"));
		IsTrue(names.Contains("Storage2"));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Mappings_ReturnsAllMappings()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");

		var mapping1 = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		};
		var mapping2 = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "MSFT.US", BoardCode = "ISIN" }
		};
		var mapping3 = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "GOOG", BoardCode = "NASDAQ" },
			AdapterId = new SecurityId { SecurityCode = "GOOG.US", BoardCode = "ISIN" }
		};

		storage.Save(mapping1);
		storage.Save(mapping2);
		storage.Save(mapping3);
		await FlushAsync(executor, token);

		var mappings = storage.Mappings.ToArray();

		AreEqual(3, mappings.Length);
		IsTrue(mappings.Any(m => m.StockSharpId.SecurityCode == "AAPL"));
		IsTrue(mappings.Any(m => m.StockSharpId.SecurityCode == "MSFT"));
		IsTrue(mappings.Any(m => m.StockSharpId.SecurityCode == "GOOG"));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Mappings_EmptyWhenNoData()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("EmptyStorage");
		var mappings = storage.Mappings.ToArray();

		AreEqual(0, mappings.Length);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Mappings_UpdatesAfterRemove()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");

		var stockSharpId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var stockSharpId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" };

		storage.Save(new SecurityIdMapping
		{
			StockSharpId = stockSharpId1,
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		});
		storage.Save(new SecurityIdMapping
		{
			StockSharpId = stockSharpId2,
			AdapterId = new SecurityId { SecurityCode = "MSFT.US", BoardCode = "ISIN" }
		});
		await FlushAsync(executor, token);

		AreEqual(2, storage.Mappings.Count());

		storage.Remove(stockSharpId1);
		await FlushAsync(executor, token);

		var mappings = storage.Mappings.ToArray();
		AreEqual(1, mappings.Length);
		AreEqual("MSFT", mappings[0].StockSharpId.SecurityCode);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Mappings_PersistsAfterReload()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");

		var mapping1 = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		};
		var mapping2 = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "MSFT.US", BoardCode = "ISIN" }
		};

		// Save mappings
		var provider1 = new CsvSecurityMappingStorageProvider(fs, path, executor);
		await provider1.InitAsync(token);
		provider1.GetStorage("TestStorage").Save(mapping1);
		provider1.GetStorage("TestStorage").Save(mapping2);
		await FlushAsync(executor, token);

		// Reload and verify Mappings property
		var provider2 = new CsvSecurityMappingStorageProvider(fs, path, executor);
		await provider2.InitAsync(token);

		var mappings = provider2.GetStorage("TestStorage").Mappings.ToArray();
		AreEqual(2, mappings.Length);
		IsTrue(mappings.Any(m => m.StockSharpId.SecurityCode == "AAPL" && m.AdapterId.SecurityCode == "AAPL.US"));
		IsTrue(mappings.Any(m => m.StockSharpId.SecurityCode == "MSFT" && m.AdapterId.SecurityCode == "MSFT.US"));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_ChangedEvent_FiresOnSave()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");

		SecurityIdMapping changedMapping = default;
		var eventCount = 0;

		storage.Changed += mapping =>
		{
			changedMapping = mapping;
			eventCount++;
		};

		var mapping = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		};

		storage.Save(mapping);
		await FlushAsync(executor, token);

		AreEqual(1, eventCount);
		AreEqual("AAPL", changedMapping.StockSharpId.SecurityCode);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_ChangedEvent_FiresOnRemove()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		storage.Save(new SecurityIdMapping
		{
			StockSharpId = stockSharpId,
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		});
		await FlushAsync(executor, token);

		SecurityIdMapping changedMapping = default;
		var eventFired = false;
		storage.Changed += mapping =>
		{
			changedMapping = mapping;
			eventFired = true;
		};

		storage.Remove(stockSharpId);
		await FlushAsync(executor, token);

		IsTrue(eventFired);
		AreEqual("AAPL", changedMapping.StockSharpId.SecurityCode);
		// On remove, AdapterId should be default
		AreEqual(default, changedMapping.AdapterId);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_ChangedEvent_FiresOnUpdate()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };

		// First save
		storage.Save(new SecurityIdMapping
		{
			StockSharpId = stockSharpId,
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		});
		await FlushAsync(executor, token);

		var eventCount = 0;
		SecurityIdMapping lastMapping = default;
		storage.Changed += mapping =>
		{
			lastMapping = mapping;
			eventCount++;
		};

		// Update with new adapter id
		storage.Save(new SecurityIdMapping
		{
			StockSharpId = stockSharpId,
			AdapterId = new SecurityId { SecurityCode = "AAPL-NEW", BoardCode = "ISIN2" }
		});
		await FlushAsync(executor, token);

		AreEqual(1, eventCount);
		AreEqual("AAPL-NEW", lastMapping.AdapterId.SecurityCode);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_StorageNames_EmptyInitially()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var names = provider.StorageNames.ToArray();
		AreEqual(0, names.Length);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_StorageNames_UpdatesOnGetStorage()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		// Just getting storage should add it to names
		_ = provider.GetStorage("NewStorage");

		var names = provider.StorageNames.ToArray();
		AreEqual(1, names.Length);
		AreEqual("NewStorage", names[0]);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_StorageNames_PersistsAfterReload()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");

		// Create storages with data
		var provider1 = new CsvSecurityMappingStorageProvider(fs, path, executor);
		await provider1.InitAsync(token);

		provider1.GetStorage("Alpha").Save(new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "A", BoardCode = "B" },
			AdapterId = new SecurityId { SecurityCode = "C", BoardCode = "D" }
		});
		provider1.GetStorage("Beta").Save(new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "E", BoardCode = "F" },
			AdapterId = new SecurityId { SecurityCode = "G", BoardCode = "H" }
		});
		await FlushAsync(executor, token);

		// Reload
		var provider2 = new CsvSecurityMappingStorageProvider(fs, path, executor);
		await provider2.InitAsync(token);

		var names = provider2.StorageNames.ToArray();
		AreEqual(2, names.Length);
		IsTrue(names.Contains("Alpha"));
		IsTrue(names.Contains("Beta"));
	}

	[TestMethod]
	public async Task CsvSecurityMapping_InitAsync_ReturnsEmptyOnSuccess()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		var errors = await provider.InitAsync(token);

		AreEqual(0, errors.Count);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_TryGetStockSharpId_ReturnsNullWhenNotFound()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");
		var result = storage.TryGetStockSharpId(new SecurityId { SecurityCode = "UNKNOWN", BoardCode = "XXX" });

		IsNull(result);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_TryGetAdapterId_ReturnsNullWhenNotFound()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");
		var result = storage.TryGetAdapterId(new SecurityId { SecurityCode = "UNKNOWN", BoardCode = "XXX" });

		IsNull(result);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Save_ReturnsTrueOnAdd_FalseOnUpdate()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };

		// First save - should return true (added)
		var added = storage.Save(new SecurityIdMapping
		{
			StockSharpId = stockSharpId,
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		});
		IsTrue(added);

		// Second save with same StockSharpId - should return false (updated)
		var updated = storage.Save(new SecurityIdMapping
		{
			StockSharpId = stockSharpId,
			AdapterId = new SecurityId { SecurityCode = "AAPL-V2", BoardCode = "ISIN2" }
		});
		IsFalse(updated);
	}

	[TestMethod]
	public async Task CsvSecurityMapping_Remove_ReturnsFalseWhenNotExists()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var storage = provider.GetStorage("TestStorage");
		var removed = storage.Remove(new SecurityId { SecurityCode = "NOTEXIST", BoardCode = "XXX" });

		IsFalse(removed);
	}

	[TestMethod]
	public void InMemorySecurityMapping_Mappings_ReturnsAllMappings()
	{
		var storage = new InMemorySecurityMappingStorage();

		var mapping1 = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		};
		var mapping2 = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "MSFT.US", BoardCode = "ISIN" }
		};

		storage.Save(mapping1);
		storage.Save(mapping2);

		var mappings = storage.Mappings.ToArray();

		AreEqual(2, mappings.Length);
		IsTrue(mappings.Any(m => m.StockSharpId.SecurityCode == "AAPL"));
		IsTrue(mappings.Any(m => m.StockSharpId.SecurityCode == "MSFT"));
	}

	[TestMethod]
	public void InMemorySecurityMapping_ChangedEvent_FiresOnSave()
	{
		var storage = new InMemorySecurityMappingStorage();

		SecurityIdMapping changedMapping = default;
		var eventCount = 0;
		storage.Changed += mapping =>
		{
			changedMapping = mapping;
			eventCount++;
		};

		var mapping = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" }
		};

		storage.Save(mapping);

		AreEqual(1, eventCount);
		AreEqual("AAPL", changedMapping.StockSharpId.SecurityCode);
	}

	[TestMethod]
	public void InMemorySecurityMapping_SaveAndLookup()
	{
		var storage = new InMemorySecurityMappingStorage();

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" };

		storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		AreEqual(stockSharpId, storage.TryGetStockSharpId(adapterId));
		AreEqual(adapterId, storage.TryGetAdapterId(stockSharpId));
	}

	[TestMethod]
	public void InMemorySecurityMapping_Remove()
	{
		var storage = new InMemorySecurityMappingStorage();

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" };

		storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });
		AreEqual(1, storage.Mappings.Count());

		var removed = storage.Remove(stockSharpId);

		IsTrue(removed);
		AreEqual(0, storage.Mappings.Count());
		IsNull(storage.TryGetStockSharpId(adapterId));
		IsNull(storage.TryGetAdapterId(stockSharpId));
	}

	[TestMethod]
	public void InMemorySecurityMapping_Update_ByStockSharpId()
	{
		var storage = new InMemorySecurityMappingStorage();

		var stockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var adapterId1 = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" };
		var adapterId2 = new SecurityId { SecurityCode = "AAPL-NEW", BoardCode = "ISIN2" };

		// First save
		var added = storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId1 });
		IsTrue(added);

		// Update (same StockSharpId, different AdapterId)
		var updated = storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId2 });
		IsFalse(updated); // Returns false on update

		// Should have new mapping
		AreEqual(adapterId2, storage.TryGetAdapterId(stockSharpId));
		// Old adapterId lookup should fail
		IsNull(storage.TryGetStockSharpId(adapterId1));
		// New adapterId lookup should work
		AreEqual(stockSharpId, storage.TryGetStockSharpId(adapterId2));
	}

	[TestMethod]
	public void InMemorySecurityMapping_Update_ByAdapterId()
	{
		var storage = new InMemorySecurityMappingStorage();

		var stockSharpId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var stockSharpId2 = new SecurityId { SecurityCode = "APPLE", BoardCode = "NASDAQ" };
		var adapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ISIN" };

		// First save
		storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId1, AdapterId = adapterId });

		// Update (different StockSharpId, same AdapterId) - should replace
		storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId2, AdapterId = adapterId });

		// Should have new mapping
		AreEqual(stockSharpId2, storage.TryGetStockSharpId(adapterId));
		// Old stockSharpId lookup should fail
		IsNull(storage.TryGetAdapterId(stockSharpId1));
	}

	[TestMethod]
	public async Task InMemorySecurityMappingProvider_StorageNames()
	{
		using var provider = new InMemorySecurityMappingStorageProvider();
		await provider.InitAsync(CancellationToken);

		AreEqual(0, provider.StorageNames.Count());

		_ = provider.GetStorage("Storage1");
		_ = provider.GetStorage("Storage2");

		var names = provider.StorageNames.ToArray();
		AreEqual(2, names.Length);
		IsTrue(names.Contains("Storage1"));
		IsTrue(names.Contains("Storage2"));
	}

	[TestMethod]
	public async Task InMemorySecurityMappingProvider_InitAsync_ReturnsEmpty()
	{
		using var provider = new InMemorySecurityMappingStorageProvider();
		var errors = await provider.InitAsync(CancellationToken);

		AreEqual(0, errors.Count);
	}

	[TestMethod]
	public void InMemorySecurityMappingProvider_GetStorage_ReturnsSameInstance()
	{
		using var provider = new InMemorySecurityMappingStorageProvider();

		var storage1 = provider.GetStorage("TestStorage");
		var storage2 = provider.GetStorage("TestStorage");

		AreSame(storage1, storage2);
	}

	[TestMethod]
	public void InMemorySecurityMappingProvider_GetStorage_CaseInsensitive()
	{
		using var provider = new InMemorySecurityMappingStorageProvider();

		var storage1 = provider.GetStorage("TestStorage");
		var storage2 = provider.GetStorage("TESTSTORAGE");
		var storage3 = provider.GetStorage("teststorage");

		AreSame(storage1, storage2);
		AreSame(storage2, storage3);
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
		var provider = new CsvNativeIdStorageProvider(fs, path, executor);

		await provider.InitAsync(token);
		var storage = provider.GetStorage("TestStorage");

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var nativeId = "12345";

		var added = await storage.TryAddAsync(secId, nativeId, true, token);
		await FlushAsync(executor, token);

		IsTrue(added);
		AreEqual(secId, await storage.TryGetByNativeIdAsync(nativeId, token));
		AreEqual(nativeId, await storage.TryGetBySecurityIdAsync(secId, token));
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
		var provider1 = new CsvNativeIdStorageProvider(fs, path, executor);
		await provider1.InitAsync(token);
		await provider1.GetStorage("TestStorage").TryAddAsync(secId, nativeId, true, token);
		await FlushAsync(executor, token);

		// Load in new instance
		var provider2 = new CsvNativeIdStorageProvider(fs, path, executor);
		await provider2.InitAsync(token);

		AreEqual(secId, await provider2.GetStorage("TestStorage").TryGetByNativeIdAsync(nativeId, token));
	}

	[TestMethod]
	public async Task CsvNativeId_Clear()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var provider = new CsvNativeIdStorageProvider(fs, path, executor);

		await provider.InitAsync(token);
		var storage = provider.GetStorage("TestStorage");

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		await storage.TryAddAsync(secId, "12345", true, token);
		await FlushAsync(executor, token);

		await storage.ClearAsync(token);
		await FlushAsync(executor, token);

		var items = await storage.GetAsync(token);
		AreEqual(0, items.Length);
	}

	[TestMethod]
	public async Task CsvNativeId_RemoveBySecurityId()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var provider = new CsvNativeIdStorageProvider(fs, path, executor);

		await provider.InitAsync(token);
		var storage = provider.GetStorage("TestStorage");

		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" };

		await storage.TryAddAsync(secId1, "12345", true, token);
		await storage.TryAddAsync(secId2, "67890", true, token);
		await FlushAsync(executor, token);

		var removed = await storage.RemoveBySecurityIdAsync(secId1, true, token);
		await FlushAsync(executor, token);

		IsTrue(removed);
		IsNull(await storage.TryGetBySecurityIdAsync(secId1, token));
		IsNotNull(await storage.TryGetBySecurityIdAsync(secId2, token));
	}

	[TestMethod]
	public async Task CsvNativeId_RemoveByNativeId()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var provider = new CsvNativeIdStorageProvider(fs, path, executor);

		await provider.InitAsync(token);
		var storage = provider.GetStorage("TestStorage");

		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" };

		await storage.TryAddAsync(secId1, "12345", true, token);
		await storage.TryAddAsync(secId2, "67890", true, token);
		await FlushAsync(executor, token);

		var removed = await storage.RemoveByNativeIdAsync("12345", true, token);
		await FlushAsync(executor, token);

		IsTrue(removed);
		IsNull(await storage.TryGetByNativeIdAsync("12345", token));
		IsNotNull(await storage.TryGetByNativeIdAsync("67890", token));
	}

	[TestMethod]
	public async Task CsvNativeId_TupleNativeId()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var provider = new CsvNativeIdStorageProvider(fs, path, executor);

		await provider.InitAsync(token);
		var storage = provider.GetStorage("TestStorage");

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var nativeId = ("Exchange1", 12345L);

		var added = await storage.TryAddAsync(secId, nativeId, true, token);
		await FlushAsync(executor, token);

		IsTrue(added);
		AreEqual(secId, await storage.TryGetByNativeIdAsync(nativeId, token));
	}

	[TestMethod]
	public async Task CsvNativeId_AddedEvent()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var provider = new CsvNativeIdStorageProvider(fs, path, executor);

		await provider.InitAsync(token);
		var storage = provider.GetStorage("TestStorage");

		SecurityId eventSecId = default;
		object eventNativeId = null;
		var eventFired = false;

		storage.Added += (secId, nativeId, ct) =>
		{
			eventSecId = secId;
			eventNativeId = nativeId;
			eventFired = true;
			return default;
		};

		var secId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		await storage.TryAddAsync(secId, "12345", true, token);
		await FlushAsync(executor, token);

		IsTrue(eventFired);
		AreEqual(secId, eventSecId);
		AreEqual("12345", eventNativeId);
	}

	[TestMethod]
	public async Task CsvNativeId_MultipleStorages_BufferIsolation()
	{
		// This test verifies that items added to different storages
		// are written to their respective files, not mixed due to shared buffer

		var token = CancellationToken;
		var (fs, path) = CreateMemoryFs("nativeids");
		var executor = CreateExecutor(token);

		await using var provider = new CsvNativeIdStorageProvider(fs, path, executor);
		await provider.InitAsync(token);

		var secIdA = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var secIdB = new SecurityId { SecurityCode = "MSFT", BoardCode = "NYSE" };

		// Add to storage "AdapterA"
		await provider.GetStorage("AdapterA").TryAddAsync(secIdA, 12345L, true, token);

		// Add to storage "AdapterB"
		await provider.GetStorage("AdapterB").TryAddAsync(secIdB, 67890L, true, token);

		// Wait for executor to process
		await FlushAsync(executor, token);

		// Verify both files exist
		var fileA = Path.Combine(path, "AdapterA.csv");
		var fileB = Path.Combine(path, "AdapterB.csv");

		IsTrue(fs.FileExists(fileA), $"File {fileA} should exist");
		IsTrue(fs.FileExists(fileB), $"File {fileB} should exist");

		// Read and verify file contents
		var contentA = ReadFileContent(fs, fileA);
		var contentB = ReadFileContent(fs, fileB);

		// AdapterA.csv should contain AAPL, NOT MSFT
		IsTrue(contentA.Contains("AAPL"), "AdapterA.csv should contain AAPL");
		IsFalse(contentA.Contains("MSFT"), "AdapterA.csv should NOT contain MSFT");

		// AdapterB.csv should contain MSFT, NOT AAPL
		IsTrue(contentB.Contains("MSFT"), "AdapterB.csv should contain MSFT");
		IsFalse(contentB.Contains("AAPL"), "AdapterB.csv should NOT contain AAPL");
	}

	[TestMethod]
	public async Task CsvNativeId_MultipleStorages_SequentialAdds_CorrectFiles()
	{
		// More aggressive test: add multiple items to different storages rapidly

		var token = CancellationToken;
		var (fs, path) = CreateMemoryFs("nativeids");
		var executor = CreateExecutor(token);

		await using var provider = new CsvNativeIdStorageProvider(fs, path, executor);
		await provider.InitAsync(token);

		// Add items to different storages in interleaved order
		await provider.GetStorage("Storage1").TryAddAsync(new SecurityId { SecurityCode = "S1A", BoardCode = "B1" }, 1L, true, token);
		await provider.GetStorage("Storage2").TryAddAsync(new SecurityId { SecurityCode = "S2A", BoardCode = "B2" }, 2L, true, token);
		await provider.GetStorage("Storage1").TryAddAsync(new SecurityId { SecurityCode = "S1B", BoardCode = "B1" }, 3L, true, token);
		await provider.GetStorage("Storage2").TryAddAsync(new SecurityId { SecurityCode = "S2B", BoardCode = "B2" }, 4L, true, token);
		await provider.GetStorage("Storage3").TryAddAsync(new SecurityId { SecurityCode = "S3A", BoardCode = "B3" }, 5L, true, token);

		// Wait for executor
		await FlushAsync(executor, token);

		// Verify files
		var file1 = Path.Combine(path, "Storage1.csv");
		var file2 = Path.Combine(path, "Storage2.csv");
		var file3 = Path.Combine(path, "Storage3.csv");

		IsTrue(fs.FileExists(file1), "Storage1.csv should exist");
		IsTrue(fs.FileExists(file2), "Storage2.csv should exist");
		IsTrue(fs.FileExists(file3), "Storage3.csv should exist");

		var content1 = ReadFileContent(fs, file1);
		var content2 = ReadFileContent(fs, file2);
		var content3 = ReadFileContent(fs, file3);

		// Storage1 should have S1A and S1B only
		IsTrue(content1.Contains("S1A"), "Storage1.csv should contain S1A");
		IsTrue(content1.Contains("S1B"), "Storage1.csv should contain S1B");
		IsFalse(content1.Contains("S2A"), "Storage1.csv should NOT contain S2A");
		IsFalse(content1.Contains("S3A"), "Storage1.csv should NOT contain S3A");

		// Storage2 should have S2A and S2B only
		IsTrue(content2.Contains("S2A"), "Storage2.csv should contain S2A");
		IsTrue(content2.Contains("S2B"), "Storage2.csv should contain S2B");
		IsFalse(content2.Contains("S1A"), "Storage2.csv should NOT contain S1A");

		// Storage3 should have S3A only
		IsTrue(content3.Contains("S3A"), "Storage3.csv should contain S3A");
		IsFalse(content3.Contains("S1A"), "Storage3.csv should NOT contain S1A");
		IsFalse(content3.Contains("S2A"), "Storage3.csv should NOT contain S2A");
	}

	[TestMethod]
	public async Task CsvNativeId_ClearOneStorage_DoesNotAffectOther()
	{
		var token = CancellationToken;
		var (fs, path) = CreateMemoryFs("nativeids");
		var executor = CreateExecutor(token);

		await using var provider = new CsvNativeIdStorageProvider(fs, path, executor);
		await provider.InitAsync(token);

		var storageA = provider.GetStorage("StorageA");
		var storageB = provider.GetStorage("StorageB");

		// Add to both storages
		await storageA.TryAddAsync(new SecurityId { SecurityCode = "AAA", BoardCode = "BA" }, 100L, true, token);
		await storageB.TryAddAsync(new SecurityId { SecurityCode = "BBB", BoardCode = "BB" }, 200L, true, token);

		await FlushAsync(executor, token);

		// Clear only StorageA
		await storageA.ClearAsync(token);

		await FlushAsync(executor, token);

		// StorageB should still have its data
		var itemsB = await storageB.GetAsync(token);
		AreEqual(1, itemsB.Length, "StorageB should still have 1 item after clearing StorageA");
		AreEqual("BBB", itemsB[0].Item1.SecurityCode);

		// StorageA should be empty
		var itemsA = await storageA.GetAsync(token);
		AreEqual(0, itemsA.Length, "StorageA should be empty after clear");
	}

	private static string ReadFileContent(MemoryFileSystem fs, string path)
	{
		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream, Encoding.UTF8);
		return reader.ReadToEnd();
	}

	#endregion

	#region File State Verification Tests

	[TestMethod]
	public async Task TransactionFileStream_FileExistsAfterCommit()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");

		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);
		await provider.InitAsync(token);

		// File should not exist yet (no data)
		IsFalse(fs.FileExists(path));

		provider.SetAdapter("TestPortfolio", Guid.NewGuid());
		await FlushAsync(executor, token);

		// File should exist after commit
		IsTrue(fs.FileExists(path), "File should exist after SetAdapter + Flush");
	}

	[TestMethod]
	public async Task TransactionFileStream_FileHasContent()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");

		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);
		await provider.InitAsync(token);

		provider.SetAdapter("TestPortfolio", Guid.NewGuid());
		await FlushAsync(executor, token);

		// File should have content (size > 0)
		var fileLength = fs.GetFileLength(path);
		IsTrue(fileLength > 0, $"File should have content, but size is {fileLength}");
	}

	[TestMethod]
	public async Task TransactionFileStream_FileContentIsValid()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");

		var adapterId = Guid.NewGuid();
		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);
		await provider.InitAsync(token);

		provider.SetAdapter("TestPortfolio", adapterId);
		await FlushAsync(executor, token);

		// Read file content and verify
		using var stream = fs.OpenRead(path);
		using var reader = new StreamReader(stream);
		var content = reader.ReadToEnd();

		IsTrue(content.Contains("Portfolio"), "File should contain header 'Portfolio'");
		IsTrue(content.Contains("Adapter"), "File should contain header 'Adapter'");
		IsTrue(content.Contains("TestPortfolio"), "File should contain portfolio name");
		IsTrue(content.Contains(adapterId.ToString()), "File should contain adapter ID");
	}

	[TestMethod]
	public async Task TransactionFileStream_MultipleWritesAccumulate()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");

		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);
		await provider.InitAsync(token);

		// First write
		provider.SetAdapter("Portfolio1", Guid.NewGuid());
		await FlushAsync(executor, token);
		var size1 = fs.GetFileLength(path);

		// Second write
		provider.SetAdapter("Portfolio2", Guid.NewGuid());
		await FlushAsync(executor, token);
		var size2 = fs.GetFileLength(path);

		IsTrue(size2 > size1, $"File should grow after second write. Size1={size1}, Size2={size2}");
	}

	[TestMethod]
	public async Task TransactionFileStream_TempFileCleanedUp()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("portfolios.csv");
		var tempPath = path + ".tmp";

		var provider = new CsvPortfolioMessageAdapterProvider(fs, path, executor);
		await provider.InitAsync(token);

		provider.SetAdapter("TestPortfolio", Guid.NewGuid());
		await FlushAsync(executor, token);

		// Temp file should not exist after commit
		IsFalse(fs.FileExists(tempPath), "Temp file should be cleaned up after commit");
		IsTrue(fs.FileExists(path), "Final file should exist");
	}

	[TestMethod]
	public async Task CsvNativeIdStorage_FilePerStorageName()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("nativeids");
		var provider = new CsvNativeIdStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		// Use different SecurityId for each storage to avoid buffer race condition
		var secId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };
		var secId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };
		await provider.GetStorage("Exchange1").TryAddAsync(secId1, "111", true, token);
		await FlushAsync(executor, token);
		await provider.GetStorage("Exchange2").TryAddAsync(secId2, "222", true, token);
		await FlushAsync(executor, token);

		// Each storage name should create separate file
		var file1 = Path.Combine(path, "Exchange1.csv");
		var file2 = Path.Combine(path, "Exchange2.csv");

		IsTrue(fs.FileExists(file1), "Exchange1.csv should exist");
		IsTrue(fs.FileExists(file2), "Exchange2.csv should exist");
		IsTrue(fs.GetFileLength(file1) > 0, "Exchange1.csv should have content");
		IsTrue(fs.GetFileLength(file2) > 0, "Exchange2.csv should have content");
	}

	[TestMethod]
	public async Task CsvSecurityMapping_DirectoryCreated()
	{
		var token = CancellationToken;
		var executor = CreateExecutor(token);
		var (fs, path) = CreateMemoryFs("mappings");
		var provider = new CsvSecurityMappingStorageProvider(fs, path, executor);

		await provider.InitAsync(token);

		var mapping = new SecurityIdMapping
		{
			StockSharpId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" },
			AdapterId = new SecurityId { SecurityCode = "AAPL.US", BoardCode = "ADAPTER" }
		};

		provider.GetStorage("TestMapping").Save(mapping);
		await FlushAsync(executor, token);

		// Directory should be created
		IsTrue(fs.DirectoryExists(path), "Mapping directory should exist");

		// File should exist
		var filePath = Path.Combine(path, "TestMapping.csv");
		IsTrue(fs.FileExists(filePath), "Mapping file should exist");
	}

	#endregion
}
