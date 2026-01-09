namespace StockSharp.Tests;

using System.Runtime.CompilerServices;

[TestClass]
public class MarketDataStorageCacheTests : BaseTestClass
{
	private static SecurityId CreateSecurityId(string code = "TEST") => new()
	{
		SecurityCode = code,
		BoardCode = BoardCodes.Test,
	};

	private static ExecutionMessage CreateTick(SecurityId securityId, DateTime serverTime) => new()
	{
		SecurityId = securityId,
		DataTypeEx = DataType.Ticks,
		ServerTime = serverTime,
		TradePrice = 1,
		TradeVolume = 1,
	};

	[TestMethod]
	public async Task GetMessagesAsync_CachesPerKey()
	{
		var token = CancellationToken;

		var cache = new MarketDataStorageCache();
		var secId = CreateSecurityId();
		var date = new DateTime(2025, 1, 1);

		var messages = new Message[]
		{
			CreateTick(secId, date.AddHours(1)),
			CreateTick(secId, date.AddHours(2)),
		};

		var loadCalls = 0;

		async IAsyncEnumerable<Message> Loader(DateTime _)
		{
			loadCalls++;

			foreach (var message in messages)
			{
				yield return message;
				await Task.Yield();
			}
		}

		var first = await cache.GetMessagesAsync(secId, DataType.Ticks, date, Loader).ToArrayAsync(token);
		var second = await cache.GetMessagesAsync(secId, DataType.Ticks, date, Loader).ToArrayAsync(token);

		loadCalls.AssertEqual(1);
		first.Length.AssertEqual(messages.Length);
		second.Length.AssertEqual(messages.Length);
	}

	[TestMethod]
	public Task GetMessagesAsync_Cancellation_ThrowsOperationCanceledException()
	{
		var cache = new MarketDataStorageCache();
		var secId = CreateSecurityId();
		var date = new DateTime(2025, 1, 1);

		async IAsyncEnumerable<Message> Loader(DateTime _)
		{
			yield return CreateTick(secId, date.AddHours(1));
			await Task.Yield();
		}

		var cts = new CancellationTokenSource();
		cts.Cancel();

		return ThrowsExactlyAsync<OperationCanceledException>(async () =>
		{
			await cache.GetMessagesAsync(secId, DataType.Ticks, date, Loader).ToArrayAsync(cts.Token);
		});
	}

	[TestMethod]
	public async Task GetMessagesAsync_OverLimit_EvictsOldest()
	{
		var token = CancellationToken;

		var cache = new MarketDataStorageCache { Limit = 1 };
		var secId = CreateSecurityId();

		var loadCalls = 0;

		async IAsyncEnumerable<Message> Loader(DateTime date)
		{
			loadCalls++;

			yield return CreateTick(secId, date.AddHours(1));
			await Task.Yield();
		}

		await cache.GetMessagesAsync(secId, DataType.Ticks, new DateTime(2025, 1, 1), Loader).ToArrayAsync(token);
		await cache.GetMessagesAsync(secId, DataType.Ticks, new DateTime(2025, 1, 2), Loader).ToArrayAsync(token);
		await cache.GetMessagesAsync(secId, DataType.Ticks, new DateTime(2025, 1, 3), Loader).ToArrayAsync(token);

		loadCalls.AssertEqual(3);

		await cache.GetMessagesAsync(secId, DataType.Ticks, new DateTime(2025, 1, 1), Loader).ToArrayAsync(token);
		loadCalls.AssertEqual(4);
	}

	[TestMethod]
	public void Limit_Invalid_ThrowsArgumentOutOfRangeException()
	{
		var cache = new MarketDataStorageCache();
		ThrowsExactly<ArgumentOutOfRangeException>(() => cache.Limit = 0);
	}

}
