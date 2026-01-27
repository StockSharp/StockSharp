namespace StockSharp.Tests;

[TestClass]
public class OfflineManagerStateTests : BaseTestClass
{
	private static readonly SecurityId _secId = new() { SecurityCode = "AAPL", BoardCode = "NYSE" };

	private static MarketDataMessage CreateSubscription(long transactionId = 1) => new()
	{
		TransactionId = transactionId,
		IsSubscribe = true,
		DataType2 = DataType.Ticks,
		SecurityId = _secId,
	};

	private static OrderRegisterMessage CreateOrder(long transactionId = 1) => new()
	{
		TransactionId = transactionId,
		SecurityId = _secId,
		PortfolioName = "pf",
		Side = Sides.Buy,
		Volume = 10,
	};

	[TestMethod]
	public void IsConnected_Initially_False()
	{
		var state = new OfflineManagerState();

		state.IsConnected.AssertFalse();
	}

	[TestMethod]
	public void SetConnected_True_IsConnectedTrue()
	{
		var state = new OfflineManagerState();

		state.SetConnected(true);

		state.IsConnected.AssertTrue();
	}

	[TestMethod]
	public void AddSuspended_IncreasesCount()
	{
		var state = new OfflineManagerState();

		state.SuspendedCount.AssertEqual(0);

		state.AddSuspended(new ResetMessage());

		state.SuspendedCount.AssertEqual(1);

		state.AddSuspended(new ResetMessage());

		state.SuspendedCount.AssertEqual(2);
	}

	[TestMethod]
	public void GetAndClearSuspended_ReturnsAndClears()
	{
		var state = new OfflineManagerState();
		var msg1 = new ResetMessage();
		var msg2 = new ResetMessage();

		state.AddSuspended(msg1);
		state.AddSuspended(msg2);

		var result = state.GetAndClearSuspended();

		result.Length.AssertEqual(2);
		result[0].AssertEqual(msg1);
		result[1].AssertEqual(msg2);

		state.SuspendedCount.AssertEqual(0);
	}

	[TestMethod]
	public void RemoveSuspended_ReturnsTrueIfExists()
	{
		var state = new OfflineManagerState();
		var msg = new ResetMessage();

		state.AddSuspended(msg);

		state.RemoveSuspended(msg).AssertTrue();
		state.SuspendedCount.AssertEqual(0);

		// removing again should return false
		state.RemoveSuspended(msg).AssertFalse();
	}

	[TestMethod]
	public void AddPendingSubscription_TryGetAndRemove_ReturnsTrue()
	{
		var state = new OfflineManagerState();
		var sub = CreateSubscription(1);

		state.AddPendingSubscription(1, sub);

		var found = state.TryGetAndRemovePendingSubscription(1, out var result);

		found.AssertTrue();
		result.AssertEqual(sub);

		// second call should return false (already removed)
		state.TryGetAndRemovePendingSubscription(1, out _).AssertFalse();
	}

	[TestMethod]
	public void TryGetAndRemovePendingSubscription_NonExistent_ReturnsFalse()
	{
		var state = new OfflineManagerState();

		state.TryGetAndRemovePendingSubscription(999, out var result).AssertFalse();
		result.AssertNull();
	}

	[TestMethod]
	public void AddPendingRegistration_TryGetAndRemove_ReturnsTrue()
	{
		var state = new OfflineManagerState();
		var order = CreateOrder(1);

		state.AddPendingRegistration(1, order);

		var found = state.TryGetAndRemovePendingRegistration(1, out var result);

		found.AssertTrue();
		result.AssertEqual(order);

		// second call should return false (already removed)
		state.TryGetAndRemovePendingRegistration(1, out _).AssertFalse();
	}

	[TestMethod]
	public void Clear_RemovesAll()
	{
		var state = new OfflineManagerState();

		state.SetConnected(true);
		state.AddSuspended(new ResetMessage());
		state.AddPendingSubscription(1, CreateSubscription(1));
		state.AddPendingRegistration(2, CreateOrder(2));

		state.Clear();

		state.IsConnected.AssertFalse();
		state.SuspendedCount.AssertEqual(0);
		state.TryGetAndRemovePendingSubscription(1, out _).AssertFalse();
		state.TryGetAndRemovePendingRegistration(2, out _).AssertFalse();
	}
}
