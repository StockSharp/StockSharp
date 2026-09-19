namespace StockSharp.Tests;

/// <summary>
/// Securities, transactions and positions storage.
/// </summary>
partial class StorageTests
{
	[TestMethod]
	public async Task Securities()
	{
		var exchangeProvider = ServicesRegistry.ExchangeInfoProvider;
		var securities = Helper.RandomSecurities().Select(s => s.ToSecurity(exchangeProvider)).ToArray();
		var token = CancellationToken;
		var fs = Helper.MemorySystem;
		var executor = TimeSpan.FromSeconds(5).CreateExecutorAndRun(err => { }, token);
		var registry = fs.GetEntityRegistry(executor);

		var storage = registry.Securities;

		foreach (var security in securities)
		{
			await storage.SaveAsync(security, true, token);
		}

		storage = registry.Securities;
		var loaded = await storage.LookupAllAsync().ToArrayAsync(token);

		loaded.Length.AssertEqual(securities.Length);

		for (var i = 0; i < loaded.Length; i++)
		{
			Helper.CheckEqual(securities[i], loaded[i]);
		}

		await storage.DeleteAllAsync(token);
		(await storage.LookupAllAsync().ToArrayAsync(token)).Count().AssertEqual(0);

		await registry.DisposeAsync();
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Transaction(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var transactions = security.RandomTransactions(1000);

		var storage = GetStorageRegistry().GetTransactionStorage(secId, null, format);

		await storage.SaveAsync(transactions, token);
		var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		loaded.CompareMessages(transactions, skipLocalTime: false);

		await storage.DeleteAsync(default, default, token);
		(await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).Length.AssertEqual(0);
	}

	/// <summary>
	/// A rejection of a cancel names no side, and Sides.Buy is the default of the enum - so a side
	/// that was never stated has to come back unstated rather than as a buy.
	/// </summary>
	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task TransactionWithoutSide(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;
		var now = DateTime.UtcNow.Truncate(TimeSpan.FromSeconds(1));

		var transactions = new[]
		{
			new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				SecurityId = secId,
				ServerTime = now,
				OriginalTransactionId = 1,
				OrderState = OrderStates.Failed,
				Error = new InvalidOperationException("test"),
			},
			new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				SecurityId = secId,
				ServerTime = now.AddSeconds(1),
				OriginalTransactionId = 2,
				OrderState = OrderStates.Active,
				Side = Sides.Sell,
				OrderPrice = 10,
				OrderVolume = 1,
			},
		};

		var storage = GetStorageRegistry().GetTransactionStorage(secId, null, format);

		await storage.SaveAsync(transactions, token);
		var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		loaded.Length.AssertEqual(2);
		loaded[0].Side.AssertNull();
		loaded[1].Side.AssertEqual(Sides.Sell);

		await storage.DeleteAsync(default, default, token);
	}

	/// <summary>
	/// The snapshot record is positional, so the absence has to travel inside the byte the side
	/// already occupies rather than shifting every field written after it.
	/// </summary>

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task Position(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var testValues = security.RandomPositionChanges();
		var token = CancellationToken;

		var secId = security.ToSecurityId();

		var storage = GetStorageRegistry().GetPositionMessageStorage(secId, null, format);

		await storage.SaveAsync(testValues, token);
		var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		testValues = [.. testValues.Where(t => t.HasChanges())];

		loaded.CompareMessages(testValues, skipLocalTime: format == StorageFormats.Csv);

		await storage.DeleteAsync(default, default, token);
		(await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).Length.AssertEqual(0);
	}

	[TestMethod]
	[DataRow(StorageFormats.Binary)]
	[DataRow(StorageFormats.Csv)]
	public async Task PositionEmpty(StorageFormats format)
	{
		var security = Helper.CreateSecurity();
		var secId = security.ToSecurityId();
		var token = CancellationToken;

		var testValues = new[]
		{
			new PositionChangeMessage
			{
				SecurityId = secId,
				ServerTime = DateTime.UtcNow,
			},
		};

		var storage = GetStorageRegistry().GetPositionMessageStorage(secId, null, format);

		await storage.SaveAsync(testValues, token);
		var loaded = await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token);

		loaded.Length.AssertEqual(0);

		await storage.DeleteAsync(default, default, token);
		(await storage.LoadAsync(DateTime.MinValue, default).ToArrayAsync(token)).Length.AssertEqual(0);
	}

}
