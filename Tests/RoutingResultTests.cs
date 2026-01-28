namespace StockSharp.Tests;

using StockSharp.Algo.Basket;

/// <summary>
/// Unit tests for <see cref="RoutingInResult"/> and <see cref="RoutingOutResult"/>.
/// </summary>
[TestClass]
public class RoutingResultTests : BaseTestClass
{
	#region RoutingInResult

	[TestMethod]
	public void RoutingInResult_Empty_HasCorrectDefaults()
	{
		var result = RoutingInResult.Empty;

		result.RoutingDecisions.Count.AssertEqual(0);
		result.OutMessages.Count.AssertEqual(0);
		result.LoopbackMessages.Count.AssertEqual(0);
		result.Handled.AssertFalse();
		result.IsPended.AssertFalse();
	}

	[TestMethod]
	public void RoutingInResult_CreateHandled_SetsHandledTrue()
	{
		var result = RoutingInResult.CreateHandled();

		result.RoutingDecisions.Count.AssertEqual(0);
		result.OutMessages.Count.AssertEqual(0);
		result.LoopbackMessages.Count.AssertEqual(0);
		result.Handled.AssertTrue();
		result.IsPended.AssertFalse();
	}

	[TestMethod]
	public void RoutingInResult_Pended_SetsIsPendedTrue()
	{
		var result = RoutingInResult.Pended();

		result.RoutingDecisions.Count.AssertEqual(0);
		result.OutMessages.Count.AssertEqual(0);
		result.LoopbackMessages.Count.AssertEqual(0);
		result.Handled.AssertTrue();
		result.IsPended.AssertTrue();
	}

	[TestMethod]
	public void RoutingInResult_WithRouting_StoresDecisions()
	{
		var adapter = new Mock<IMessageAdapter>().Object;
		var message = new SecurityMessage();
		var decisions = new List<(IMessageAdapter, Message)> { (adapter, message) };

		var result = RoutingInResult.WithRouting(decisions);

		result.RoutingDecisions.Count.AssertEqual(1);
		result.RoutingDecisions[0].Adapter.AssertEqual(adapter);
		result.RoutingDecisions[0].Message.AssertEqual(message);
		result.OutMessages.Count.AssertEqual(0);
		result.LoopbackMessages.Count.AssertEqual(0);
		result.Handled.AssertTrue();
		result.IsPended.AssertFalse();
	}

	[TestMethod]
	public void RoutingInResult_WithRouting_NullDecisions_CreatesEmpty()
	{
		var result = RoutingInResult.WithRouting(null);

		result.RoutingDecisions.Count.AssertEqual(0);
		result.Handled.AssertTrue();
	}

	[TestMethod]
	public void RoutingInResult_WithOutMessage_StoresMessage()
	{
		var message = new SubscriptionResponseMessage { OriginalTransactionId = 123 };

		var result = RoutingInResult.WithOutMessage(message);

		result.RoutingDecisions.Count.AssertEqual(0);
		result.OutMessages.Count.AssertEqual(1);
		result.OutMessages[0].AssertEqual(message);
		result.LoopbackMessages.Count.AssertEqual(0);
		result.Handled.AssertTrue();
		result.IsPended.AssertFalse();
	}

	[TestMethod]
	public void RoutingInResult_InitSyntax_AllFieldsSet()
	{
		var adapter = new Mock<IMessageAdapter>().Object;
		var routedMsg = new SecurityMessage();
		var outMsg = new ConnectMessage();
		var loopbackMsg = new ResetMessage();

		var result = new RoutingInResult
		{
			RoutingDecisions = [(adapter, routedMsg)],
			OutMessages = [outMsg],
			LoopbackMessages = [loopbackMsg],
			Handled = true,
			IsPended = false,
		};

		result.RoutingDecisions.Count.AssertEqual(1);
		result.OutMessages.Count.AssertEqual(1);
		result.LoopbackMessages.Count.AssertEqual(1);
		result.Handled.AssertTrue();
		result.IsPended.AssertFalse();
	}

	#endregion

	#region RoutingOutResult

	[TestMethod]
	public void RoutingOutResult_Empty_HasCorrectDefaults()
	{
		var result = RoutingOutResult.Empty;

		result.TransformedMessage.AssertNull();
		result.ExtraMessages.Count.AssertEqual(0);
		result.LoopbackMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RoutingOutResult_PassThrough_StoresMessage()
	{
		var message = new ExecutionMessage { TransactionId = 100 };

		var result = RoutingOutResult.PassThrough(message);

		result.TransformedMessage.AssertEqual(message);
		result.ExtraMessages.Count.AssertEqual(0);
		result.LoopbackMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RoutingOutResult_WithMessage_StoresMessage()
	{
		var message = new SubscriptionResponseMessage { OriginalTransactionId = 42 };

		var result = RoutingOutResult.WithMessage(message);

		result.TransformedMessage.AssertEqual(message);
		result.ExtraMessages.Count.AssertEqual(0);
		result.LoopbackMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RoutingOutResult_WithExtraMessages_StoresExtras()
	{
		var extra1 = new ConnectMessage();
		var extra2 = new DisconnectMessage();
		var extras = new List<Message> { extra1, extra2 };

		var result = RoutingOutResult.WithExtraMessages(extras);

		result.TransformedMessage.AssertNull();
		result.ExtraMessages.Count.AssertEqual(2);
		result.ExtraMessages[0].AssertEqual(extra1);
		result.ExtraMessages[1].AssertEqual(extra2);
		result.LoopbackMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RoutingOutResult_WithExtraMessages_NullExtras_CreatesEmpty()
	{
		var result = RoutingOutResult.WithExtraMessages(null);

		result.TransformedMessage.AssertNull();
		result.ExtraMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RoutingOutResult_WithLoopback_StoresLoopbackMessage()
	{
		var loopback = new MarketDataMessage { TransactionId = 123 };

		var result = RoutingOutResult.WithLoopback(loopback);

		result.TransformedMessage.AssertNull();
		result.ExtraMessages.Count.AssertEqual(0);
		result.LoopbackMessages.Count.AssertEqual(1);
		result.LoopbackMessages[0].AssertEqual(loopback);
	}

	[TestMethod]
	public void RoutingOutResult_WithMessageAndExtras_StoresBoth()
	{
		var message = new SubscriptionOnlineMessage { OriginalTransactionId = 1 };
		var extra = new SubscriptionResponseMessage { OriginalTransactionId = 2 };
		var extras = new List<Message> { extra };

		var result = RoutingOutResult.WithMessageAndExtras(message, extras);

		result.TransformedMessage.AssertEqual(message);
		result.ExtraMessages.Count.AssertEqual(1);
		result.ExtraMessages[0].AssertEqual(extra);
		result.LoopbackMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RoutingOutResult_WithMessageAndExtras_NullExtras_CreatesEmptyExtras()
	{
		var message = new ConnectMessage();

		var result = RoutingOutResult.WithMessageAndExtras(message, null);

		result.TransformedMessage.AssertEqual(message);
		result.ExtraMessages.Count.AssertEqual(0);
	}

	[TestMethod]
	public void RoutingOutResult_InitSyntax_AllFieldsSet()
	{
		var transformedMsg = new SubscriptionResponseMessage { OriginalTransactionId = 1 };
		var extraMsg = new SubscriptionOnlineMessage { OriginalTransactionId = 1 };
		var loopbackMsg = new MarketDataMessage { TransactionId = 2 };

		var result = new RoutingOutResult
		{
			TransformedMessage = transformedMsg,
			ExtraMessages = [extraMsg],
			LoopbackMessages = [loopbackMsg],
		};

		result.TransformedMessage.AssertEqual(transformedMsg);
		result.ExtraMessages.Count.AssertEqual(1);
		result.ExtraMessages[0].AssertEqual(extraMsg);
		result.LoopbackMessages.Count.AssertEqual(1);
		result.LoopbackMessages[0].AssertEqual(loopbackMsg);
	}

	#endregion
}
