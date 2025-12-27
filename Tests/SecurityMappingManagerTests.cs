namespace StockSharp.Tests;

using StockSharp.Algo.Storages;

[TestClass]
public class SecurityMappingManagerTests : BaseTestClass
{
	private const string StorageName = "TestAdapter";

	private static SecurityId CreateAdapterId(string code = "AAPL", string board = "NASDAQ")
		=> new() { SecurityCode = code, BoardCode = board };

	private static SecurityId CreateStockSharpId(string code = "AAPL", string board = "US")
		=> new() { SecurityCode = code, BoardCode = board };

	private static ISecurityMappingStorageProvider CreateProvider() => new InMemorySecurityMappingStorageProvider();

	#region ProcessInMessage Tests

	[TestMethod]
	public void ProcessInMessage_SecurityLookupMessage_PassesThrough()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new SecurityLookupMessage { TransactionId = 123 };

		var (result, forward) = manager.ProcessInMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
	}

	[TestMethod]
	public void ProcessInMessage_SecurityMessage_WithMapping_ReplacesSecurityId()
	{
		var provider = CreateProvider();
		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		provider.GetStorage(StorageName).Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new SecurityMessage { SecurityId = stockSharpId };

		var (result, forward) = manager.ProcessInMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((SecurityMessage)result).SecurityId.AssertEqual(adapterId);
	}

	[TestMethod]
	public void ProcessInMessage_SecurityMessage_WithoutMapping_KeepsOriginalId()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var securityId = CreateStockSharpId();
		var message = new SecurityMessage { SecurityId = securityId };

		var (result, forward) = manager.ProcessInMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((SecurityMessage)result).SecurityId.AssertEqual(securityId);
	}

	[TestMethod]
	public void ProcessInMessage_SecurityMessage_DefaultSecurityId_NoChange()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new SecurityMessage { SecurityId = default };

		var (result, forward) = manager.ProcessInMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((SecurityMessage)result).SecurityId.AssertEqual(default);
	}

	[TestMethod]
	public void ProcessInMessage_OtherMessage_PassesThrough()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new ResetMessage();

		var (result, forward) = manager.ProcessInMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
	}

	#endregion

	#region ProcessOutMessage Tests

	[TestMethod]
	public void ProcessOutMessage_SecurityMessage_WithMapping_ReplacesSecurityId()
	{
		var provider = CreateProvider();
		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		provider.GetStorage(StorageName).Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new SecurityMessage { SecurityId = adapterId };

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((SecurityMessage)result).SecurityId.AssertEqual(stockSharpId);
	}

	[TestMethod]
	public void ProcessOutMessage_SecurityMessage_WithoutMapping_KeepsAdapterId()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var adapterId = CreateAdapterId();
		var message = new SecurityMessage { SecurityId = adapterId };

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((SecurityMessage)result).SecurityId.AssertEqual(adapterId);
	}

	[TestMethod]
	public void ProcessOutMessage_SecurityMessage_DefaultSecurityId_ThrowsException()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new SecurityMessage { SecurityId = default };

		var thrown = false;
		try
		{
			manager.ProcessOutMessage(message);
		}
		catch (InvalidOperationException)
		{
			thrown = true;
		}
		thrown.AssertTrue();
	}

	[TestMethod]
	public void ProcessOutMessage_NewsMessage_WithSecurityId_MapsToStockSharpId()
	{
		var provider = CreateProvider();
		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		provider.GetStorage(StorageName).Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new NewsMessage
		{
			Headline = "Test News",
			SecurityId = adapterId
		};

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((NewsMessage)result).SecurityId.AssertEqual(stockSharpId);
	}

	[TestMethod]
	public void ProcessOutMessage_NewsMessage_WithoutSecurityId_PassesThrough()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new NewsMessage { Headline = "Test News" };

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((NewsMessage)result).SecurityId.AssertNull();
	}

	[TestMethod]
	public void ProcessOutMessage_SecurityMappingMessage_Save_AddsMapping()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		var message = new SecurityMappingMessage
		{
			StorageName = StorageName,
			Mapping = new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId },
			IsDelete = false
		};

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertNull();
		forward.AssertFalse();

		// Verify mapping was saved
		var savedAdapterId = provider.GetStorage(StorageName).TryGetAdapterId(stockSharpId);
		savedAdapterId.AssertNotNull();
		savedAdapterId.Value.AssertEqual(adapterId);
	}

	[TestMethod]
	public void ProcessOutMessage_SecurityMappingMessage_Delete_RemovesMapping()
	{
		var provider = CreateProvider();
		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		provider.GetStorage(StorageName).Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new SecurityMappingMessage
		{
			StorageName = StorageName,
			Mapping = new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId },
			IsDelete = true
		};

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertNull();
		forward.AssertFalse();

		// Verify mapping was removed
		var savedAdapterId = provider.GetStorage(StorageName).TryGetAdapterId(stockSharpId);
		savedAdapterId.AssertNull();
	}

	[TestMethod]
	public void ProcessOutMessage_ExecutionMessage_WithMapping_ReplacesSecurityId()
	{
		var provider = CreateProvider();
		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		provider.GetStorage(StorageName).Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new ExecutionMessage
		{
			SecurityId = adapterId,
			DataTypeEx = DataType.Ticks
		};

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((ExecutionMessage)result).SecurityId.AssertEqual(stockSharpId);
	}

	[TestMethod]
	public void ProcessOutMessage_Level1ChangeMessage_WithMapping_ReplacesSecurityId()
	{
		var provider = CreateProvider();
		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		provider.GetStorage(StorageName).Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new Level1ChangeMessage
		{
			SecurityId = adapterId,
			ServerTime = DateTime.UtcNow
		};

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((Level1ChangeMessage)result).SecurityId.AssertEqual(stockSharpId);
	}

	[TestMethod]
	public void ProcessOutMessage_QuoteChangeMessage_WithMapping_ReplacesSecurityId()
	{
		var provider = CreateProvider();
		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		provider.GetStorage(StorageName).Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new QuoteChangeMessage
		{
			SecurityId = adapterId,
			ServerTime = DateTime.UtcNow,
			Bids = [],
			Asks = []
		};

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		((QuoteChangeMessage)result).SecurityId.AssertEqual(stockSharpId);
	}

	[TestMethod]
	public void ProcessOutMessage_SpecialSecurityId_NotMapped()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		// Use the predefined special security ID (Money)
		var specialSecurityId = SecurityId.Money;

		var message = new Level1ChangeMessage
		{
			SecurityId = specialSecurityId,
			ServerTime = DateTime.UtcNow
		};

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
		// Special security ID should not be mapped
		((Level1ChangeMessage)result).SecurityId.SecurityCode.AssertEqual("MONEY");
	}

	[TestMethod]
	public void ProcessOutMessage_ConnectMessage_PassesThrough()
	{
		var provider = CreateProvider();
		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		var message = new ConnectMessage();

		var (result, forward) = manager.ProcessOutMessage(message);

		result.AssertSame(message);
		forward.AssertTrue();
	}

	#endregion

	#region Logging Tests

	[TestMethod]
	public void ProcessInMessage_SecurityMessage_WithMapping_LogsInfo()
	{
		var provider = CreateProvider();
		var stockSharpId = CreateStockSharpId();
		var adapterId = CreateAdapterId();

		provider.GetStorage(StorageName).Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId });

		var logCalled = false;
		object loggedArg0 = null;
		object loggedArg1 = null;

		var manager = new SecurityMappingManager(
			provider,
			() => StorageName,
			(format, arg0, arg1, arg2) =>
			{
				logCalled = true;
				loggedArg0 = arg0;
				loggedArg1 = arg1;
			});

		var message = new SecurityMessage { SecurityId = stockSharpId };

		manager.ProcessInMessage(message);

		logCalled.AssertTrue();
		loggedArg0.AssertEqual(stockSharpId);
		loggedArg1.AssertEqual(adapterId);
	}

	#endregion

	#region Multiple Mappings Tests

	[TestMethod]
	public void ProcessOutMessage_MultipleMappings_UsesCorrectOne()
	{
		var provider = CreateProvider();
		var storage = provider.GetStorage(StorageName);

		var stockSharpId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "US" };
		var adapterId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		var stockSharpId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "US" };
		var adapterId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

		storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId1, AdapterId = adapterId1 });
		storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId2, AdapterId = adapterId2 });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		// Test first mapping
		var message1 = new SecurityMessage { SecurityId = adapterId1 };
		var (result1, _) = manager.ProcessOutMessage(message1);
		((SecurityMessage)result1).SecurityId.AssertEqual(stockSharpId1);

		// Test second mapping
		var message2 = new SecurityMessage { SecurityId = adapterId2 };
		var (result2, _) = manager.ProcessOutMessage(message2);
		((SecurityMessage)result2).SecurityId.AssertEqual(stockSharpId2);
	}

	[TestMethod]
	public void ProcessInMessage_MultipleMappings_UsesCorrectOne()
	{
		var provider = CreateProvider();
		var storage = provider.GetStorage(StorageName);

		var stockSharpId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "US" };
		var adapterId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };

		var stockSharpId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "US" };
		var adapterId2 = new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" };

		storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId1, AdapterId = adapterId1 });
		storage.Save(new SecurityIdMapping { StockSharpId = stockSharpId2, AdapterId = adapterId2 });

		var manager = new SecurityMappingManager(provider, () => StorageName, null);

		// Test first mapping
		var message1 = new SecurityMessage { SecurityId = stockSharpId1 };
		var (result1, _) = manager.ProcessInMessage(message1);
		((SecurityMessage)result1).SecurityId.AssertEqual(adapterId1);

		// Test second mapping
		var message2 = new SecurityMessage { SecurityId = stockSharpId2 };
		var (result2, _) = manager.ProcessInMessage(message2);
		((SecurityMessage)result2).SecurityId.AssertEqual(adapterId2);
	}

	#endregion

	#region Storage Name Tests

	[TestMethod]
	public void ProcessInMessage_DifferentStorageNames_UsesCorrectStorage()
	{
		var provider = CreateProvider();

		var stockSharpId = CreateStockSharpId();
		var adapterId1 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" };
		var adapterId2 = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" };

		provider.GetStorage("Adapter1").Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId1 });
		provider.GetStorage("Adapter2").Save(new SecurityIdMapping { StockSharpId = stockSharpId, AdapterId = adapterId2 });

		var currentStorageName = "Adapter1";
		var manager = new SecurityMappingManager(provider, () => currentStorageName, null);

		var message = new SecurityMessage { SecurityId = stockSharpId };
		var (result1, _) = manager.ProcessInMessage(message);
		((SecurityMessage)result1).SecurityId.AssertEqual(adapterId1);

		// Change storage name and verify different mapping is used
		currentStorageName = "Adapter2";
		message = new SecurityMessage { SecurityId = stockSharpId };
		var (result2, _) = manager.ProcessInMessage(message);
		((SecurityMessage)result2).SecurityId.AssertEqual(adapterId2);
	}

	#endregion
}
