namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Generation;

[TestClass]
public class HistoryMarketDataManagerTests : BaseTestClass
{
	private static SecurityId CreateSecurityId() => Helper.CreateSecurityId();

	#region Property Tests

	[TestMethod]
	public void Constructor_SetsDefaultValues()
	{
		using var manager = new HistoryMarketDataManager();

		manager.StartDate.AssertEqual(DateTime.MinValue);
		manager.StopDate.AssertEqual(DateTime.MaxValue);
		manager.MarketTimeChangedInterval.AssertEqual(TimeSpan.FromSeconds(1));
		manager.PostTradeMarketTimeChangedCount.AssertEqual(2);
		manager.CheckTradableDates.AssertEqual(false);
		manager.IsStarted.AssertEqual(false);
		manager.LoadedMessageCount.AssertEqual(0);
	}

	[TestMethod]
	public void MarketTimeChangedInterval_ThrowsOnInvalidValue()
	{
		using var manager = new HistoryMarketDataManager();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.MarketTimeChangedInterval = TimeSpan.Zero);

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.MarketTimeChangedInterval = TimeSpan.FromSeconds(-1));
	}

	[TestMethod]
	public void PostTradeMarketTimeChangedCount_ThrowsOnNegativeValue()
	{
		using var manager = new HistoryMarketDataManager();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			manager.PostTradeMarketTimeChangedCount = -1);
	}

	[TestMethod]
	public void PostTradeMarketTimeChangedCount_AcceptsZero()
	{
		using var manager = new HistoryMarketDataManager();

		manager.PostTradeMarketTimeChangedCount = 0;
		manager.PostTradeMarketTimeChangedCount.AssertEqual(0);
	}

	#endregion

	#region Generator Tests

	[TestMethod]
	public void RegisterGenerator_AddsGenerator()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();
		var dataType = DataType.Ticks;
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, dataType, generator, 123);

		manager.HasGenerator(secId, dataType).AssertTrue();
	}

	[TestMethod]
	public void RegisterGenerator_ThrowsOnNullGenerator()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		ThrowsExactly<ArgumentNullException>(() =>
			manager.RegisterGenerator(secId, DataType.Ticks, null, 123));
	}

	[TestMethod]
	public void HasGenerator_ReturnsFalseWhenNotRegistered()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		manager.HasGenerator(secId, DataType.Ticks).AssertFalse();
	}

	[TestMethod]
	public void UnregisterGenerator_RemovesGenerator()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();
		var dataType = DataType.Ticks;
		var generator = new RandomWalkTradeGenerator(secId);
		long transId = 123;

		manager.RegisterGenerator(secId, dataType, generator, transId);
		manager.HasGenerator(secId, dataType).AssertTrue();

		var result = manager.UnregisterGenerator(transId);

		result.AssertTrue();
		manager.HasGenerator(secId, dataType).AssertFalse();
	}

	[TestMethod]
	public void UnregisterGenerator_ReturnsFalseWhenNotFound()
	{
		using var manager = new HistoryMarketDataManager();

		var result = manager.UnregisterGenerator(999);

		result.AssertFalse();
	}

	#endregion

	#region Subscription Tests

	[TestMethod]
	public async Task SubscribeAsync_ReturnsErrorWithoutStorageRegistry()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		var message = new MarketDataMessage
		{
			IsSubscribe = true,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};

		var error = await manager.SubscribeAsync(message, CancellationToken);

		error.AssertNotNull();
		(error is InvalidOperationException).AssertTrue();
	}

	[TestMethod]
	public async Task SubscribeAsync_ThrowsOnNullMessage()
	{
		using var manager = new HistoryMarketDataManager();

		await ThrowsExactlyAsync<ArgumentNullException>(() =>
			manager.SubscribeAsync(null, CancellationToken).AsTask());
	}

	[TestMethod]
	public async Task SubscribeAsync_ThrowsOnUnsubscribeMessage()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		var message = new MarketDataMessage
		{
			IsSubscribe = false,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
		};

		await ThrowsExactlyAsync<ArgumentException>(() =>
			manager.SubscribeAsync(message, CancellationToken).AsTask());
	}

	[TestMethod]
	public void Unsubscribe_ThrowsOnZeroTransactionId()
	{
		using var manager = new HistoryMarketDataManager();

		ThrowsExactly<ArgumentException>(() =>
			manager.Unsubscribe(0));
	}

	#endregion

	#region Reset Tests

	[TestMethod]
	public void Reset_ClearsGenerators()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, DataType.Ticks, generator, 1);
		manager.HasGenerator(secId, DataType.Ticks).AssertTrue();

		manager.Reset();

		manager.HasGenerator(secId, DataType.Ticks).AssertFalse();
	}

	[TestMethod]
	public void Reset_ResetsLoadedMessageCount()
	{
		using var manager = new HistoryMarketDataManager();

		manager.Reset();

		manager.LoadedMessageCount.AssertEqual(0);
		manager.IsStarted.AssertEqual(false);
	}

	#endregion

	#region GetSupportedDataTypes Tests

	[TestMethod]
	public void GetSupportedDataTypes_ReturnsEmptyWithoutDrive()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();

		var dataTypes = manager.GetSupportedDataTypes(secId);

		dataTypes.Count().AssertEqual(0);
	}

	[TestMethod]
	public void GetSupportedDataTypes_IncludesGeneratorDataTypes()
	{
		using var manager = new HistoryMarketDataManager();
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		manager.RegisterGenerator(secId, DataType.Ticks, generator, 1);

		var dataTypes = manager.GetSupportedDataTypes(secId).ToList();

		dataTypes.Contains(DataType.Ticks).AssertTrue();
	}

	#endregion

	#region Stop Tests

	[TestMethod]
	public void Stop_CanBeCalledMultipleTimes()
	{
		using var manager = new HistoryMarketDataManager();

		manager.Stop();
		manager.Stop();
		manager.Stop();
	}

	#endregion
}
