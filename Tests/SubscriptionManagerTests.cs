namespace StockSharp.Tests;

[TestClass]
public class SubscriptionManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	[TestMethod]
	public void Subscribe_FromFuture_ClampsToNow()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var message = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
			From = logReceiver.CurrentTimeUtc.AddHours(1),
		};

		var (toInner, toOut) = manager.ProcessInMessage(message);

		toOut.Length.AssertEqual(0);
		toInner.Length.AssertEqual(1);

		var sent = (MarketDataMessage)toInner[0];
		sent.From.AssertEqual(logReceiver.CurrentTimeUtc);
		message.From.AssertEqual(logReceiver.CurrentTimeUtc.AddHours(1));
	}

	[TestMethod]
	public void ConnectionRestored_RemapsSubscriptions()
	{
		var logReceiver = new TestReceiver();
		var transactionIdGenerator = new IncrementalIdGenerator();
		var manager = new SubscriptionManager(logReceiver, transactionIdGenerator, () => new ProcessSuspendedMessage());

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		manager.ProcessInMessage(subscription);
		manager.ProcessOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = 100 });

		var restored = new ConnectionRestoredMessage { IsResetState = true };
		var (forward, extraOut) = manager.ProcessOutMessage(restored);

		extraOut.Length.AssertEqual(1);
		extraOut[0].Type.AssertEqual(MessageTypes.ProcessSuspended);

		var (toInner, toOut) = manager.ProcessInMessage(new ProcessSuspendedMessage());

		toInner.Length.AssertEqual(1);
		((MarketDataMessage)toInner[0]).TransactionId.AssertNotEqual(100);
	}
}
