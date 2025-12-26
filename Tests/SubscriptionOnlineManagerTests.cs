namespace StockSharp.Tests;

[TestClass]
public class SubscriptionOnlineManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	[TestMethod]
	public async Task Subscribe_SecondSubscription_JoinsAndReturnsResponseAndOnline()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		var first = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut) = await manager.ProcessInMessageAsync(first, token);
		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);

		var second = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		};

		var secondResult = await manager.ProcessInMessageAsync(second, token);
		secondResult.toInner.Length.AssertEqual(0);
		secondResult.toOut.Length.AssertEqual(2);

		secondResult.toOut.OfType<SubscriptionResponseMessage>().Single().OriginalTransactionId.AssertEqual(2);
		secondResult.toOut.OfType<SubscriptionOnlineMessage>().Single().OriginalTransactionId.AssertEqual(2);
	}

	[TestMethod]
	public async Task SubscriptionError_ShouldNotifyJoinedSubscribers()
	{
		var logReceiver = new TestReceiver();
		var manager = new SubscriptionOnlineManager(logReceiver, _ => true);
		var token = CancellationToken;

		var secId = Helper.CreateSecurityId();

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		await manager.ProcessInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = secId,
			DataType2 = DataType.Ticks,
		}, token);

		var error = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("boom"),
		};

		var (forward, extraOut) = await manager.ProcessOutMessageAsync(error, token);

		extraOut.OfType<SubscriptionResponseMessage>().Any(msg => msg.OriginalTransactionId == 2 && msg.Error != null).AssertTrue();
	}
}
