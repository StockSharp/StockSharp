namespace StockSharp.Tests;

using System.Runtime.CompilerServices;

[TestClass]
public class CacheableMarketDataStorageTests : BaseTestClass
{
	private sealed class TestMarketDataStorage(SecurityId securityId, DataType dataType, Message[] messages, int saveReturn) : IMarketDataStorage
	{
		private readonly Message[] _messages = messages ?? throw new ArgumentNullException(nameof(messages));
		private readonly int _saveReturn = saveReturn;

		public int LoadCalls { get; private set; }
		public int SaveCalls { get; private set; }

		ValueTask<IEnumerable<DateTime>> IMarketDataStorage.GetDatesAsync(CancellationToken cancellationToken)
			=> new([DateTime.UtcNow.Date]);

		DataType IMarketDataStorage.DataType => dataType;
		SecurityId IMarketDataStorage.SecurityId => securityId;
		IMarketDataStorageDrive IMarketDataStorage.Drive => Mock.Of<IMarketDataStorageDrive>();
		bool IMarketDataStorage.AppendOnlyNew { get; set; }

		IAsyncEnumerable<Message> IMarketDataStorage.LoadAsync(DateTime date)
		{
			LoadCalls++;
			return LoadImpl();
		}

		private async IAsyncEnumerable<Message> LoadImpl([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var message in _messages)
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return message;
				await Task.Yield();
			}
		}

		ValueTask<int> IMarketDataStorage.SaveAsync(IEnumerable<Message> data, CancellationToken cancellationToken)
		{
			SaveCalls++;
			return new(_saveReturn);
		}

		ValueTask IMarketDataStorage.DeleteAsync(IEnumerable<Message> data, CancellationToken cancellationToken) => default;
		ValueTask IMarketDataStorage.DeleteAsync(DateTime date, CancellationToken cancellationToken) => default;
		ValueTask<IMarketDataMetaInfo> IMarketDataStorage.GetMetaInfoAsync(DateTime date, CancellationToken cancellationToken) => new((IMarketDataMetaInfo)null);
		IMarketDataSerializer IMarketDataStorage.Serializer => Mock.Of<IMarketDataSerializer>();
	}

	[TestMethod]
	public async Task LoadAsync_UsesCache()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var date = new DateTime(2025, 1, 1);

		var messages = new Message[]
		{
			new ExecutionMessage
			{
				SecurityId = secId,
				DataTypeEx = DataType.Ticks,
				ServerTime = date.AddHours(1),
				TradePrice = 1,
				TradeVolume = 1,
			},
		};

		var underlying = new TestMarketDataStorage(secId, DataType.Ticks, messages, saveReturn: 42);
		var cache = new MarketDataStorageCache();

		IMarketDataStorage storage = new CacheableMarketDataStorage(underlying, cache);

		var first = await storage.LoadAsync(date).ToArrayAsync(token);
		var second = await storage.LoadAsync(date).ToArrayAsync(token);

		first.Length.AssertEqual(1);
		second.Length.AssertEqual(1);
		underlying.LoadCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task SaveAsync_PassesThrough()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var date = new DateTime(2025, 1, 1);

		var underlying = new TestMarketDataStorage(secId, DataType.Ticks, [new ExecutionMessage()], saveReturn: 7);
		var cache = new MarketDataStorageCache();

		IMarketDataStorage storage = new CacheableMarketDataStorage(underlying, cache);

		var saved = await storage.SaveAsync([new ExecutionMessage { ServerTime = date }], token);

		saved.AssertEqual(7);
		underlying.SaveCalls.AssertEqual(1);
	}

	[TestMethod]
	public async Task SaveAsync_InvalidatesCache()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var date = new DateTime(2025, 1, 1);

		var messages = new Message[]
		{
			new ExecutionMessage
			{
				SecurityId = secId,
				DataTypeEx = DataType.Ticks,
				ServerTime = date.AddHours(1),
				TradePrice = 1,
				TradeVolume = 1,
			},
		};

		var underlying = new TestMarketDataStorage(secId, DataType.Ticks, messages, saveReturn: 1);
		var cache = new MarketDataStorageCache();

		IMarketDataStorage storage = new CacheableMarketDataStorage(underlying, cache);

		// Load data - should cache it
		var first = await storage.LoadAsync(date).ToArrayAsync(token);
		first.Length.AssertEqual(1);
		underlying.LoadCalls.AssertEqual(1);

		// Save new data - should invalidate cache
		await storage.SaveAsync([new ExecutionMessage { ServerTime = date.AddHours(2) }], token);

		// Load again - bug: before fix, cache was not invalidated, so LoadCalls would still be 1
		var second = await storage.LoadAsync(date).ToArrayAsync(token);

		underlying.LoadCalls.AssertEqual(2);
	}

	[TestMethod]
	public async Task DeleteAsync_Messages_InvalidatesCache()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var date = new DateTime(2025, 1, 1);

		var messages = new Message[]
		{
			new ExecutionMessage
			{
				SecurityId = secId,
				DataTypeEx = DataType.Ticks,
				ServerTime = date.AddHours(1),
				TradePrice = 1,
				TradeVolume = 1,
			},
		};

		var underlying = new TestMarketDataStorage(secId, DataType.Ticks, messages, saveReturn: 0);
		var cache = new MarketDataStorageCache();

		IMarketDataStorage storage = new CacheableMarketDataStorage(underlying, cache);

		// Load data - should cache it
		var first = await storage.LoadAsync(date).ToArrayAsync(token);
		underlying.LoadCalls.AssertEqual(1);

		// Delete data - should invalidate cache
		await storage.DeleteAsync([new ExecutionMessage { ServerTime = date.AddHours(1) }], token);

		// Load again - cache should be invalidated
		var second = await storage.LoadAsync(date).ToArrayAsync(token);
		underlying.LoadCalls.AssertEqual(2);
	}

	[TestMethod]
	public async Task DeleteAsync_Date_InvalidatesCache()
	{
		var token = CancellationToken;

		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test };
		var date = new DateTime(2025, 1, 1);

		var messages = new Message[]
		{
			new ExecutionMessage
			{
				SecurityId = secId,
				DataTypeEx = DataType.Ticks,
				ServerTime = date.AddHours(1),
				TradePrice = 1,
				TradeVolume = 1,
			},
		};

		var underlying = new TestMarketDataStorage(secId, DataType.Ticks, messages, saveReturn: 0);
		var cache = new MarketDataStorageCache();

		IMarketDataStorage storage = new CacheableMarketDataStorage(underlying, cache);

		// Load data - should cache it
		var first = await storage.LoadAsync(date).ToArrayAsync(token);
		underlying.LoadCalls.AssertEqual(1);

		// Delete by date - should invalidate cache
		await storage.DeleteAsync(date, token);

		// Load again - cache should be invalidated
		var second = await storage.LoadAsync(date).ToArrayAsync(token);
		underlying.LoadCalls.AssertEqual(2);
	}
}
