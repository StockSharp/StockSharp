namespace StockSharp.Tests;

[TestClass]
public class SecurityProviderTests : BaseTestClass
{
	private static readonly CancellationToken _token = CancellationToken.None;

	[TestMethod]
	public async Task CollectionSecurityProvider_LookupByIdAsync_ReturnsSecurityWhenExists()
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		var provider = new CollectionSecurityProvider([security]);

		var result = await provider.LookupByIdAsync(securityId, _token);

		result.AssertNotNull();
		result.Id.AssertEqual(security.Id);
	}

	[TestMethod]
	public async Task CollectionSecurityProvider_LookupByIdAsync_ReturnsNullWhenNotExists()
	{
		var provider = new CollectionSecurityProvider();

		var result = await provider.LookupByIdAsync(new SecurityId { SecurityCode = "UNKNOWN", BoardCode = BoardCodes.Test }, _token);

		result.AssertNull();
	}

	[TestMethod]
	public async Task CollectionSecurityProvider_LookupAsync_ReturnsAllSecurities()
	{
		var security1 = Helper.CreateSecurity();
		var security2 = Helper.CreateSecurity();
		var provider = new CollectionSecurityProvider([security1, security2]);

		var results = await provider.LookupAsync(Helper.LookupAll).ToArrayAsync(_token);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task CollectionSecurityProvider_LookupAsync_ReturnsEmptyWhenNoSecurities()
	{
		var provider = new CollectionSecurityProvider();

		var results = await provider.LookupAsync(Helper.LookupAll).ToArrayAsync(_token);

		results.Length.AssertEqual(0);
	}

	[TestMethod]
	public void CollectionSecurityProvider_Count_ReturnsCorrectCount()
	{
		var security1 = Helper.CreateSecurity();
		var security2 = Helper.CreateSecurity();
		var provider = new CollectionSecurityProvider([security1, security2]);

		provider.Count.AssertEqual(2);
	}

	[TestMethod]
	public void CollectionSecurityProvider_Add_IncrementsCount()
	{
		var provider = new CollectionSecurityProvider();
		var security = Helper.CreateSecurity();

		provider.Add(security);

		provider.Count.AssertEqual(1);
	}

	[TestMethod]
	public void CollectionSecurityProvider_Add_TriggersAddedEvent()
	{
		var provider = new CollectionSecurityProvider();
		var security = Helper.CreateSecurity();
		var addedCalled = false;

		((ISecurityProvider)provider).Added += _ => addedCalled = true;
		provider.Add(security);

		addedCalled.AssertTrue();
	}

	[TestMethod]
	public void CollectionSecurityProvider_Remove_DecrementsCount()
	{
		var security = Helper.CreateSecurity();
		var provider = new CollectionSecurityProvider([security]);

		provider.Remove(security);

		provider.Count.AssertEqual(0);
	}

	[TestMethod]
	public void CollectionSecurityProvider_Remove_TriggersRemovedEvent()
	{
		var security = Helper.CreateSecurity();
		var provider = new CollectionSecurityProvider([security]);
		var removedCalled = false;

		((ISecurityProvider)provider).Removed += _ => removedCalled = true;
		provider.Remove(security);

		removedCalled.AssertTrue();
	}

	[TestMethod]
	public void CollectionSecurityProvider_Clear_SetsCountToZero()
	{
		var security1 = Helper.CreateSecurity();
		var security2 = Helper.CreateSecurity();
		var provider = new CollectionSecurityProvider([security1, security2]);

		provider.Clear();

		provider.Count.AssertEqual(0);
	}

	[TestMethod]
	public void CollectionSecurityProvider_Clear_TriggersClearedEvent()
	{
		var security = Helper.CreateSecurity();
		var provider = new CollectionSecurityProvider([security]);
		var clearedCalled = false;

		((ISecurityProvider)provider).Cleared += () => clearedCalled = true;
		provider.Clear();

		clearedCalled.AssertTrue();
	}

	[TestMethod]
	public async Task ISecurityMessageProvider_LookupMessageByIdAsync_ReturnsMessageWhenExists()
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		ISecurityMessageProvider provider = new CollectionSecurityProvider([security]);

		var result = await provider.LookupMessageByIdAsync(securityId, _token);

		result.AssertNotNull();
		result.SecurityId.AssertEqual(securityId);
	}

	[TestMethod]
	public async Task ISecurityMessageProvider_LookupMessageByIdAsync_ReturnsNullWhenNotExists()
	{
		ISecurityMessageProvider provider = new CollectionSecurityProvider();

		var result = await provider.LookupMessageByIdAsync(new SecurityId { SecurityCode = "UNKNOWN", BoardCode = BoardCodes.Test }, _token);

		result.AssertNull();
	}

	[TestMethod]
	public async Task ISecurityMessageProvider_LookupMessagesAsync_ReturnsAllMessages()
	{
		var security1 = Helper.CreateSecurity();
		var security2 = Helper.CreateSecurity();
		ISecurityMessageProvider provider = new CollectionSecurityProvider([security1, security2]);

		var results = await provider.LookupMessagesAsync(Helper.LookupAll).ToArrayAsync(_token);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task ISecurityMessageProvider_LookupMessagesAsync_ConvertsSecurityToMessage()
	{
		var security = Helper.CreateSecurity();
		var securityId = security.ToSecurityId();
		ISecurityMessageProvider provider = new CollectionSecurityProvider([security]);

		var results = await provider.LookupMessagesAsync(Helper.LookupAll).ToArrayAsync(_token);

		results.Length.AssertEqual(1);
		results[0].SecurityId.AssertEqual(securityId);
	}

	[TestMethod]
	public async Task CollectionSecurityProvider_AddRange_AddsMultipleSecurities()
	{
		var provider = new CollectionSecurityProvider();
		var security1 = Helper.CreateSecurity();
		var security2 = Helper.CreateSecurity();

		provider.AddRange([security1, security2]);

		provider.Count.AssertEqual(2);

		var results = await provider.LookupAsync(Helper.LookupAll).ToArrayAsync(_token);
		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public void CollectionSecurityProvider_RemoveRange_RemovesMultipleSecurities()
	{
		var security1 = Helper.CreateSecurity();
		var security2 = Helper.CreateSecurity();
		var provider = new CollectionSecurityProvider([security1, security2]);

		provider.RemoveRange([security1, security2]);

		provider.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task LookupAllAsync_ReturnsAllSecurities()
	{
		var security1 = Helper.CreateSecurity();
		var security2 = Helper.CreateSecurity();
		ISecurityProvider provider = new CollectionSecurityProvider([security1, security2]);

		var results = await provider.LookupAllAsync().ToArrayAsync(_token);

		results.Length.AssertEqual(2);
	}

	#region IsMatch Tests

	[TestMethod]
	public void IsMatch_SecurityCode_ContainsFilter()
	{
		var security = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			SecurityType = SecurityTypes.Stock,
		};

		var criteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AA" }
		};

		security.IsMatch(criteria).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_SecurityCode_NoMatchReturnsFlase()
	{
		var security = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
		};

		var criteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { SecurityCode = "MSFT" }
		};

		security.IsMatch(criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_BoardCode_ExactMatch()
	{
		var security = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
		};

		var criteriaMatch = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = "NASDAQ" }
		};

		var criteriaNoMatch = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = "NYSE" }
		};

		security.IsMatch(criteriaMatch).AssertTrue();
		security.IsMatch(criteriaNoMatch).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SecurityType_SingleType()
	{
		var stock = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			SecurityType = SecurityTypes.Stock,
		};

		var option = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL230120C150", BoardCode = "NASDAQ" },
			SecurityType = SecurityTypes.Option,
		};

		var criteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };

		stock.IsMatch(criteria).AssertTrue();
		option.IsMatch(criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_SecurityType_MultipleTypes()
	{
		var stock = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			SecurityType = SecurityTypes.Stock,
		};

		var future = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "ESZ3", BoardCode = "CME" },
			SecurityType = SecurityTypes.Future,
		};

		var option = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL230120C150", BoardCode = "NASDAQ" },
			SecurityType = SecurityTypes.Option,
		};

		var criteria = new SecurityLookupMessage();
		criteria.SetSecurityTypes(null, new[] { SecurityTypes.Stock, SecurityTypes.Future });

		stock.IsMatch(criteria).AssertTrue();
		future.IsMatch(criteria).AssertTrue();
		option.IsMatch(criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_CombinedCriteria_CodeAndBoard()
	{
		var security = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
		};

		var criteriaMatch = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" }
		};

		var criteriaBoardMismatch = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NYSE" }
		};

		security.IsMatch(criteriaMatch).AssertTrue();
		security.IsMatch(criteriaBoardMismatch).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_EmptyCriteria_MatchesAll()
	{
		var security = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			SecurityType = SecurityTypes.Stock,
		};

		var criteria = new SecurityLookupMessage();

		security.IsMatch(criteria).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_SecurityIds_MatchesAny()
	{
		var security = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
		};

		var criteria = new SecurityLookupMessage
		{
			SecurityIds = [
				new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" },
				new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			]
		};

		security.IsMatch(criteria).AssertTrue();
	}

	[TestMethod]
	public void IsMatch_SecurityIds_NoMatch()
	{
		var security = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
		};

		var criteria = new SecurityLookupMessage
		{
			SecurityIds = [
				new SecurityId { SecurityCode = "MSFT", BoardCode = "NASDAQ" },
				new SecurityId { SecurityCode = "GOOGL", BoardCode = "NASDAQ" },
			]
		};

		security.IsMatch(criteria).AssertFalse();
	}

	[TestMethod]
	public void IsMatch_Name_ContainsFilter()
	{
		var security = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" },
			Name = "Apple Inc.",
		};

		var criteriaMatch = new SecurityLookupMessage { Name = "Apple" };
		var criteriaNoMatch = new SecurityLookupMessage { Name = "Microsoft" };

		security.IsMatch(criteriaMatch).AssertTrue();
		security.IsMatch(criteriaNoMatch).AssertFalse();
	}

	#endregion

	#region Filter Tests with Multiple Securities

	[TestMethod]
	public async Task LookupAsync_FilterBySecurityCode_ReturnsMatching()
	{
		var aapl = CreateSecurityWithCode("AAPL", "NASDAQ");
		var msft = CreateSecurityWithCode("MSFT", "NASDAQ");
		var googl = CreateSecurityWithCode("GOOGL", "NASDAQ");
		var provider = new CollectionSecurityProvider([aapl, msft, googl]);

		var criteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AA" }
		};

		var results = await provider.LookupAsync(criteria).ToArrayAsync(_token);

		results.Length.AssertEqual(1);
		results[0].Code.AssertEqual("AAPL");
	}

	[TestMethod]
	public async Task LookupAsync_FilterByBoard_ReturnsMatching()
	{
		var nasdaqSec = CreateSecurityWithCode("AAPL", "NASDAQ");
		var nyseSec = CreateSecurityWithCode("IBM", "NYSE");
		var provider = new CollectionSecurityProvider([nasdaqSec, nyseSec]);

		var criteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = "NASDAQ" }
		};

		var results = await provider.LookupAsync(criteria).ToArrayAsync(_token);

		results.Length.AssertEqual(1);
		results[0].Code.AssertEqual("AAPL");
	}

	[TestMethod]
	public async Task LookupAsync_FilterBySecurityType_ReturnsMatching()
	{
		var stock = CreateSecurityWithType("AAPL", SecurityTypes.Stock);
		var future = CreateSecurityWithType("ESZ3", SecurityTypes.Future);
		var option = CreateSecurityWithType("AAPL230120C150", SecurityTypes.Option);
		var provider = new CollectionSecurityProvider([stock, future, option]);

		var criteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };

		var results = await provider.LookupAsync(criteria).ToArrayAsync(_token);

		results.Length.AssertEqual(1);
		results[0].Code.AssertEqual("AAPL");
	}

	[TestMethod]
	public async Task LookupAsync_FilterByMultipleTypes_ReturnsMatching()
	{
		var stock = CreateSecurityWithType("AAPL", SecurityTypes.Stock);
		var future = CreateSecurityWithType("ESZ3", SecurityTypes.Future);
		var option = CreateSecurityWithType("AAPL230120C150", SecurityTypes.Option);
		var provider = new CollectionSecurityProvider([stock, future, option]);

		var criteria = new SecurityLookupMessage();
		criteria.SetSecurityTypes(null, new[] { SecurityTypes.Stock, SecurityTypes.Future });

		var results = await provider.LookupAsync(criteria).ToArrayAsync(_token);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public async Task LookupAsync_FilterByCombinedCriteria_ReturnsMatching()
	{
		var nasdaqStock = CreateSecurityFull("AAPL", "NASDAQ", SecurityTypes.Stock);
		var nasdaqOption = CreateSecurityFull("AAPL230120C150", "NASDAQ", SecurityTypes.Option);
		var nyseStock = CreateSecurityFull("IBM", "NYSE", SecurityTypes.Stock);
		var provider = new CollectionSecurityProvider([nasdaqStock, nasdaqOption, nyseStock]);

		var criteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = "NASDAQ" },
			SecurityType = SecurityTypes.Stock,
		};

		var results = await provider.LookupAsync(criteria).ToArrayAsync(_token);

		results.Length.AssertEqual(1);
		results[0].Code.AssertEqual("AAPL");
	}

	[TestMethod]
	public async Task LookupAsync_WithSkipAndCount_ReturnsPage()
	{
		var securities = Enumerable.Range(1, 10)
			.Select(i => CreateSecurityWithCode($"SEC{i:D2}", BoardCodes.Test))
			.ToArray();
		var provider = new CollectionSecurityProvider(securities);

		var criteria = new SecurityLookupMessage
		{
			Skip = 3,
			Count = 4,
		};

		var results = await provider.LookupAsync(criteria).ToArrayAsync(_token);

		results.Length.AssertEqual(4);
	}

	#endregion

	#region InMemorySecurityStorage Tests

	[TestMethod]
	public async Task InMemorySecurityStorage_SaveAsync_AddsSecurity()
	{
		var storage = new InMemorySecurityStorage();
		var security = Helper.CreateSecurity();

		await storage.SaveAsync(security, false, _token);

		var result = await storage.LookupByIdAsync(security.ToSecurityId(), _token);
		result.AssertNotNull();
		result.Id.AssertEqual(security.Id);
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_SaveAsync_TriggersAddedEvent()
	{
		var storage = new InMemorySecurityStorage();
		var security = Helper.CreateSecurity();
		var addedCalled = false;

		storage.Added += _ => addedCalled = true;
		await storage.SaveAsync(security, false, _token);

		addedCalled.AssertTrue();
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_SaveAsync_DoesNotDuplicate()
	{
		var storage = new InMemorySecurityStorage();
		var security = Helper.CreateSecurity();

		await storage.SaveAsync(security, false, _token);
		await storage.SaveAsync(security, false, _token);

		var results = await storage.LookupAsync(Helper.LookupAll).ToArrayAsync(_token);
		results.Length.AssertEqual(1);
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_DeleteAsync_RemovesSecurity()
	{
		var storage = new InMemorySecurityStorage();
		var security = Helper.CreateSecurity();
		await storage.SaveAsync(security, false, _token);

		await storage.DeleteAsync(security, _token);

		var result = await storage.LookupByIdAsync(security.ToSecurityId(), _token);
		result.AssertNull();
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_DeleteAsync_TriggersRemovedEvent()
	{
		var storage = new InMemorySecurityStorage();
		var security = Helper.CreateSecurity();
		await storage.SaveAsync(security, false, _token);
		var removedCalled = false;

		storage.Removed += _ => removedCalled = true;
		await storage.DeleteAsync(security, _token);

		removedCalled.AssertTrue();
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_DeleteRangeAsync_RemovesMultiple()
	{
		var storage = new InMemorySecurityStorage();
		var sec1 = Helper.CreateSecurity();
		var sec2 = Helper.CreateSecurity();
		var sec3 = Helper.CreateSecurity();
		await storage.SaveAsync(sec1, false, _token);
		await storage.SaveAsync(sec2, false, _token);
		await storage.SaveAsync(sec3, false, _token);

		await storage.DeleteRangeAsync([sec1, sec2], _token);

		var results = await storage.LookupAsync(Helper.LookupAll).ToArrayAsync(_token);
		results.Length.AssertEqual(1);
		results[0].Id.AssertEqual(sec3.Id);
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_DeleteRangeAsync_TriggersRemovedEventCorrectly()
	{
		var storage = new InMemorySecurityStorage();
		var sec1 = Helper.CreateSecurity();
		var sec2 = Helper.CreateSecurity();
		var sec3 = Helper.CreateSecurity(); // This one won't be saved - delete should not include it
		await storage.SaveAsync(sec1, false, _token);
		await storage.SaveAsync(sec2, false, _token);

		var removedSecurities = new List<Security>();
		storage.Removed += removedSecurities.AddRange;

		// Try to delete sec1, sec2 (existing) and sec3 (not existing)
		await storage.DeleteRangeAsync([sec1, sec2, sec3], _token);

		removedSecurities.Count.AssertEqual(2);
		removedSecurities.Any(s => s.Id == sec1.Id).AssertTrue();
		removedSecurities.Any(s => s.Id == sec2.Id).AssertTrue();
		removedSecurities.Any(s => s.Id == sec3.Id).AssertFalse();
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_Empty_DeleteRangeAsync_TriggersRemovedEventCorrectly()
	{
		var storage = new InMemorySecurityStorage();
		var sec1 = Helper.CreateSecurity();
		var sec2 = Helper.CreateSecurity();
		var sec3 = Helper.CreateSecurity();

		var removedSecurities = new List<Security>();
		storage.Removed += removedSecurities.AddRange;

		await storage.DeleteRangeAsync([sec1, sec2, sec3], _token);

		removedSecurities.Count.AssertEqual(0);
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_DeleteRangeAsync_NoEventWhenNothingDeleted()
	{
		var storage = new InMemorySecurityStorage();
		var sec1 = Helper.CreateSecurity();
		// Don't save sec1 - it doesn't exist in storage

		var removedCalled = false;
		storage.Removed += _ => removedCalled = true;

		// Try to delete non-existing security
		await storage.DeleteRangeAsync([sec1], _token);

		removedCalled.AssertFalse();
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_DeleteByAsync_LookupAll_ClearsAll()
	{
		var storage = new InMemorySecurityStorage();
		var sec1 = Helper.CreateSecurity();
		var sec2 = Helper.CreateSecurity();
		await storage.SaveAsync(sec1, false, _token);
		await storage.SaveAsync(sec2, false, _token);
		var clearedCalled = false;

		storage.Cleared += () => clearedCalled = true;
		await storage.DeleteByAsync(Helper.LookupAll, _token);

		clearedCalled.AssertTrue();
		var results = await storage.LookupAsync(Helper.LookupAll).ToArrayAsync(_token);
		results.Length.AssertEqual(0);
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_DeleteByAsync_ByCriteria()
	{
		var storage = new InMemorySecurityStorage();
		var stock = CreateSecurityWithType("AAPL", SecurityTypes.Stock);
		var option = CreateSecurityWithType("AAPL230120C150", SecurityTypes.Option);
		await storage.SaveAsync(stock, false, _token);
		await storage.SaveAsync(option, false, _token);

		var criteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };
		await storage.DeleteByAsync(criteria, _token);

		var results = await storage.LookupAsync(Helper.LookupAll).ToArrayAsync(_token);
		results.Length.AssertEqual(1);
		results[0].Type.AssertEqual(SecurityTypes.Option);
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_LookupAsync_FiltersCorrectly()
	{
		var storage = new InMemorySecurityStorage();
		var nasdaqStock = CreateSecurityFull("AAPL", "NASDAQ", SecurityTypes.Stock);
		var nyseStock = CreateSecurityFull("IBM", "NYSE", SecurityTypes.Stock);
		await storage.SaveAsync(nasdaqStock, false, _token);
		await storage.SaveAsync(nyseStock, false, _token);

		var criteria = new SecurityLookupMessage
		{
			SecurityId = new SecurityId { BoardCode = "NASDAQ" }
		};

		var results = await storage.LookupAsync(criteria).ToArrayAsync(_token);

		results.Length.AssertEqual(1);
		results[0].Code.AssertEqual("AAPL");
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_WithUnderlying_LookupFallsThrough()
	{
		var underlyingSec = Helper.CreateSecurity();
		var underlying = new CollectionSecurityProvider([underlyingSec]);
		var storage = new InMemorySecurityStorage(underlying);
		var storageSec = Helper.CreateSecurity();
		await storage.SaveAsync(storageSec, false, _token);

		var storageResult = await storage.LookupByIdAsync(storageSec.ToSecurityId(), _token);
		var underlyingResult = await storage.LookupByIdAsync(underlyingSec.ToSecurityId(), _token);

		storageResult.AssertNotNull();
		underlyingResult.AssertNotNull();
	}

	[TestMethod]
	public async Task InMemorySecurityStorage_LookupAsync_CombinesWithUnderlying()
	{
		var underlyingSec = Helper.CreateSecurity();
		var underlying = new CollectionSecurityProvider([underlyingSec]);
		var storage = new InMemorySecurityStorage(underlying);
		var storageSec = Helper.CreateSecurity();
		await storage.SaveAsync(storageSec, false, _token);

		var results = await storage.LookupAsync(Helper.LookupAll).ToArrayAsync(_token);

		results.Length.AssertEqual(2);
	}

	[TestMethod]
	public void InMemorySecurityStorage_SaveAsync_ThrowsOnNull()
	{
		var storage = new InMemorySecurityStorage();

		ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await storage.SaveAsync(null, false, _token));
	}

	[TestMethod]
	public void InMemorySecurityStorage_DeleteAsync_ThrowsOnNull()
	{
		var storage = new InMemorySecurityStorage();

		ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await storage.DeleteAsync(null, _token));
	}

	[TestMethod]
	public void InMemorySecurityStorage_DeleteByAsync_ThrowsOnNull()
	{
		var storage = new InMemorySecurityStorage();

		ThrowsExactlyAsync<ArgumentNullException>(async () =>
			await storage.DeleteByAsync(null, _token));
	}

	#endregion

	#region Helper Methods

	private static Security CreateSecurityWithCode(string code, string board)
	{
		return new Security
		{
			Id = $"{code}@{board}",
			Code = code,
			Board = new ExchangeBoard { Code = board, Exchange = Exchange.Test },
		};
	}

	private static Security CreateSecurityWithType(string code, SecurityTypes type)
	{
		return new Security
		{
			Id = $"{code}@{BoardCodes.Test}",
			Code = code,
			Board = ExchangeBoard.Test,
			Type = type,
		};
	}

	private static Security CreateSecurityFull(string code, string board, SecurityTypes type)
	{
		return new Security
		{
			Id = $"{code}@{board}",
			Code = code,
			Board = new ExchangeBoard { Code = board, Exchange = Exchange.Test },
			Type = type,
		};
	}

	#endregion
}
