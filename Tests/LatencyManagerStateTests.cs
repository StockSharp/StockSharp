namespace StockSharp.Tests;

using StockSharp.Algo.Latency;

[TestClass]
public class LatencyManagerStateTests : BaseTestClass
{
	[TestMethod]
	public void AddRegistration_ThrowsOnZeroTransactionId()
	{
		var state = new LatencyManagerState();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			state.AddRegistration(0, DateTime.UtcNow));
	}

	[TestMethod]
	public void AddRegistration_ThrowsOnDefaultLocalTime()
	{
		var state = new LatencyManagerState();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			state.AddRegistration(1, default));
	}

	[TestMethod]
	public void AddRegistration_ThrowsOnDuplicate()
	{
		var state = new LatencyManagerState();
		var t0 = DateTime.UtcNow;

		state.AddRegistration(1, t0);

		ThrowsExactly<ArgumentException>(() =>
			state.AddRegistration(1, t0));
	}

	[TestMethod]
	public void AddCancellation_ThrowsOnZeroTransactionId()
	{
		var state = new LatencyManagerState();

		ThrowsExactly<ArgumentOutOfRangeException>(() =>
			state.AddCancellation(0, DateTime.UtcNow));
	}

	[TestMethod]
	public void TryGetAndRemoveRegistration_ReturnsCorrectValue()
	{
		var state = new LatencyManagerState();
		var t0 = DateTime.UtcNow;

		state.AddRegistration(1, t0);

		var found = state.TryGetAndRemoveRegistration(1, out var time);

		found.AssertTrue();
		time.AssertEqual(t0);

		// Second call should return false
		state.TryGetAndRemoveRegistration(1, out _).AssertFalse();
	}

	[TestMethod]
	public void Clear_ResetsEverything()
	{
		var state = new LatencyManagerState();
		var t0 = DateTime.UtcNow;

		state.AddRegistration(1, t0);
		state.AddCancellation(2, t0);
		state.AddLatencyRegistration(TimeSpan.FromSeconds(1));
		state.AddLatencyCancellation(TimeSpan.FromSeconds(2));

		state.Clear();

		state.TryGetAndRemoveRegistration(1, out _).AssertFalse();
		state.TryGetAndRemoveCancellation(2, out _).AssertFalse();
		state.LatencyRegistration.AssertEqual(TimeSpan.Zero);
		state.LatencyCancellation.AssertEqual(TimeSpan.Zero);
	}
}
