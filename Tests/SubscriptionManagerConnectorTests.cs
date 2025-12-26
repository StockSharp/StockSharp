namespace StockSharp.Tests;

[TestClass]
public class SubscriptionManagerConnectorTests : BaseTestClass
{
	[TestMethod]
	public void Subscribe_AssignsTransactionId_AndSendsRequest()
	{
		var manager = new ConnectorSubscriptionManager(new IncrementalIdGenerator(), true);

		var message = new MarketDataMessage
		{
			IsSubscribe = true,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		var subscription = new Subscription(message);

		var actions = manager.Subscribe(subscription);

		subscription.TransactionId.AssertNotEqual(0);

		actions.Items.Length.AssertEqual(1);
		actions.Items[0].Type.AssertEqual(ConnectorSubscriptionManager.Actions.Item.Types.SendInMessage);

		actions.Items[0].Message.AssertOfType<MarketDataMessage>();
		var sent = (MarketDataMessage)actions.Items[0].Message;
		sent.TransactionId.AssertEqual(subscription.TransactionId);
	}

	[TestMethod]
	public void ProcessResponse_ErrorAfterActive_ShouldMarkUnexpectedCancelled()
	{
		var manager = new ConnectorSubscriptionManager(new IncrementalIdGenerator(), true);

		var message = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		var subscription = new Subscription(message);

		_ = manager.Subscribe(subscription);
		subscription.State = SubscriptionStates.Active;

		var response = new SubscriptionResponseMessage
		{
			OriginalTransactionId = subscription.TransactionId,
			Error = new InvalidOperationException("boom"),
		};

		manager.ProcessResponse(response, out _, out var unexpectedCancelled, out _);

		unexpectedCancelled.AssertTrue();
	}
}
