namespace StockSharp.Tests;

[TestClass]
public class TransactionOrderingManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	private static SecurityId CreateSecurityId() => Helper.CreateSecurityId();

	[TestMethod]
	public void ProcessInMessage_Reset_ClearsState()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		// First add some state
		var regMsg = new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = CreateSecurityId(),
			Price = 10.500m,
			Volume = 5.000m,
		};

		manager.ProcessInMessage(regMsg);

		// Now reset
		var (toInner, toOut) = manager.ProcessInMessage(new ResetMessage());

		toInner.Length.AssertEqual(1);
		toInner[0].Type.AssertEqual(MessageTypes.Reset);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_OrderRegister_RemovesTrailingZeros()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var regMsg = new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = CreateSecurityId(),
			Price = 10.500m,
			Volume = 5.000m,
			VisibleVolume = 3.000m,
		};

		var (toInner, toOut) = manager.ProcessInMessage(regMsg);

		toInner.Length.AssertEqual(1);
		var processed = (OrderRegisterMessage)toInner[0];
		processed.Price.AssertEqual(10.5m);
		processed.Volume.AssertEqual(5m);
		processed.VisibleVolume.AssertEqual(3m);
	}

	[TestMethod]
	public void ProcessInMessage_OrderReplace_RemovesTrailingZeros()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var secId = CreateSecurityId();

		// First register an order
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = secId,
			Price = 10m,
			Volume = 5m,
		});

		// Then replace it
		var replaceMsg = new OrderReplaceMessage
		{
			TransactionId = 101,
			OriginalTransactionId = 100,
			Price = 11.500m,
			Volume = 6.000m,
		};

		var (toInner, toOut) = manager.ProcessInMessage(replaceMsg);

		toInner.Length.AssertEqual(1);
		var processed = (OrderReplaceMessage)toInner[0];
		processed.Price.AssertEqual(11.5m);
		processed.Volume.AssertEqual(6m);
	}

	[TestMethod]
	public void ProcessInMessage_OrderCancel_RemovesTrailingZerosFromVolume()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var cancelMsg = new OrderCancelMessage
		{
			TransactionId = 102,
			OriginalTransactionId = 100,
			Volume = 3.000m,
		};

		var (toInner, toOut) = manager.ProcessInMessage(cancelMsg);

		toInner.Length.AssertEqual(1);
		var processed = (OrderCancelMessage)toInner[0];
		processed.Volume.AssertEqual(3m);
	}

	[TestMethod]
	public void ProcessInMessage_OrderStatus_Subscribe_WithTransactionLog_AddsSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => true);

		var statusMsg = new OrderStatusMessage
		{
			TransactionId = 200,
			IsSubscribe = true,
		};

		var (toInner, toOut) = manager.ProcessInMessage(statusMsg);

		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessInMessage_OrderStatus_Subscribe_WithoutTransactionLog_AddsToOrderStatusIds()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var statusMsg = new OrderStatusMessage
		{
			TransactionId = 200,
			IsSubscribe = true,
		};

		var (toInner, toOut) = manager.ProcessInMessage(statusMsg);

		toInner.Length.AssertEqual(1);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionResponse_Error_RemovesSubscription()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => true);

		// First subscribe
		manager.ProcessInMessage(new OrderStatusMessage
		{
			TransactionId = 200,
			IsSubscribe = true,
		});

		// Then receive error response
		var errorResponse = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 200,
			Error = new InvalidOperationException("Test error"),
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(errorResponse);

		forward.AssertNotNull();
		forward.Type.AssertEqual(MessageTypes.SubscriptionResponse);
		extraOut.Length.AssertEqual(0);
		processSuspended.AssertFalse();
	}

	[TestMethod]
	public void ProcessOutMessage_Execution_MarketData_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = CreateSecurityId(),
			TradePrice = 100m,
			TradeVolume = 10m,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(execMsg);

		forward.AssertNotNull();
		processSuspended.AssertFalse();
	}

	[TestMethod]
	public void ProcessOutMessage_Execution_Cancellation_PassesThrough()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = CreateSecurityId(),
			IsCancellation = true,
			TransactionId = 100,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(execMsg);

		forward.AssertNotNull();
		processSuspended.AssertFalse();
	}

	[TestMethod]
	public void ProcessOutMessage_Execution_WithTransactionId_StoresSecurityId()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var secId = CreateSecurityId();

		// First register an order
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = secId,
			Price = 10m,
			Volume = 5m,
		});

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 100,
			OriginalTransactionId = 100,
			HasOrderInfo = true,
			OrderId = 12345,
			OrderState = OrderStates.Active,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(execMsg);

		forward.AssertNotNull();
		processSuspended.AssertTrue();
	}

	[TestMethod]
	public void ProcessOutMessage_Execution_OrderWithoutOriginTransId_LogsWarning()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = CreateSecurityId(),
			TransactionId = 0,
			OriginalTransactionId = 0,
			HasOrderInfo = true,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(execMsg);

		forward.AssertNull();
		logReceiver.Logs.Any(l => l.Message.Contains("Order doesn't have origin trans id")).AssertTrue();
	}

	[TestMethod]
	public void ProcessOutMessage_Trade_SuspendsWhenOrderNotKnown()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		// Trade arrives before order confirmation - should be suspended
		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = CreateSecurityId(),
			TransactionId = 0,
			OriginalTransactionId = 0,
			OrderId = 99999,
			TradeId = 55555,
			TradePrice = 100m,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(execMsg);

		forward.AssertNull();
		processSuspended.AssertFalse();
		logReceiver.Logs.Any(l => l.Message.Contains("suspended")).AssertTrue();
	}

	[TestMethod]
	public void GetSuspendedTrades_ReturnsAndClearsSuspendedTrades()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		// Trade arrives before order - gets suspended
		var suspendedTrade = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = CreateSecurityId(),
			TransactionId = 0,
			OriginalTransactionId = 0,
			OrderId = 12345,
			TradeId = 55555,
			TradePrice = 100m,
		};

		manager.ProcessOutMessage(suspendedTrade);

		// Now order confirmation arrives - should release the suspended trade
		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = CreateSecurityId(),
			HasOrderInfo = true,
			OrderId = 12345,
			TransactionId = 200,
		};

		var suspendedTrades = manager.GetSuspendedTrades(orderMsg);

		suspendedTrades.Length.AssertEqual(1);
		((ExecutionMessage)suspendedTrades[0]).TradeId.AssertEqual(55555);

		// Second call should return empty (trades already released)
		var secondCall = manager.GetSuspendedTrades(orderMsg);
		secondCall.Length.AssertEqual(0);
	}

	[TestMethod]
	public void GetSuspendedTrades_WithOrderStringId_ReturnsAndClearsSuspendedTrades()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		// Trade arrives before order - gets suspended
		var suspendedTrade = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = CreateSecurityId(),
			TransactionId = 0,
			OriginalTransactionId = 0,
			OrderStringId = "ORDER-123",
			TradeId = 55555,
			TradePrice = 100m,
		};

		manager.ProcessOutMessage(suspendedTrade);

		// Now order confirmation arrives - should release the suspended trade
		var orderMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = CreateSecurityId(),
			HasOrderInfo = true,
			OrderStringId = "ORDER-123",
			TransactionId = 200,
		};

		var suspendedTrades = manager.GetSuspendedTrades(orderMsg);

		suspendedTrades.Length.AssertEqual(1);
		((ExecutionMessage)suspendedTrades[0]).TradeId.AssertEqual(55555);
	}

	[TestMethod]
	public void ProcessOutMessage_Trade_ResolvesOriginTransIdFromOrderId()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var secId = CreateSecurityId();

		// First, register an order to establish the mapping
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = secId,
			Price = 10m,
			Volume = 5m,
		});

		// Simulate order execution with OrderId
		var orderExec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 100,
			OriginalTransactionId = 100,
			HasOrderInfo = true,
			OrderId = 12345,
			OrderState = OrderStates.Active,
		};

		manager.ProcessOutMessage(orderExec);

		// Now a trade comes in with OrderId but no OriginalTransactionId
		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 0,
			OriginalTransactionId = 0,
			OrderId = 12345,
			TradeId = 55555,
			TradePrice = 100m,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(tradeMsg);

		// Trade should pass through with resolved OriginalTransactionId
		forward.AssertNotNull();
		((ExecutionMessage)forward).OriginalTransactionId.AssertEqual(100);
	}

	[TestMethod]
	public void ProcessOutMessage_Trade_ResolvesOriginTransIdFromOrderStringId()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var secId = CreateSecurityId();

		// First, register an order
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = secId,
			Price = 10m,
			Volume = 5m,
		});

		// Simulate order execution with OrderStringId
		var orderExec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 100,
			OriginalTransactionId = 100,
			HasOrderInfo = true,
			OrderStringId = "ORDER-ABC",
			OrderState = OrderStates.Active,
		};

		manager.ProcessOutMessage(orderExec);

		// Now a trade comes in with OrderStringId but no OriginalTransactionId
		var tradeMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 0,
			OriginalTransactionId = 0,
			OrderStringId = "ORDER-ABC",
			TradeId = 55555,
			TradePrice = 100m,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(tradeMsg);

		// Trade should pass through with resolved OriginalTransactionId
		forward.AssertNotNull();
		((ExecutionMessage)forward).OriginalTransactionId.AssertEqual(100);
	}

	[TestMethod]
	public void ProcessOutMessage_Execution_FillsSecurityIdFromRegistration()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var secId = CreateSecurityId();

		// Register an order to establish secId mapping
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = secId,
			Price = 10m,
			Volume = 5m,
		});

		// Receive execution with empty SecurityId
		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = default,
			TransactionId = 0,
			OriginalTransactionId = 100,
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(execMsg);

		forward.AssertNotNull();
		((ExecutionMessage)forward).SecurityId.AssertEqual(secId);
	}

	[TestMethod]
	public void ProcessInMessage_OrderReplace_InheritsSecurityIdFromOriginal()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => false);

		var secId = CreateSecurityId();

		// First register an order
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = secId,
			Price = 10m,
			Volume = 5m,
		});

		// Replace the order
		var replaceMsg = new OrderReplaceMessage
		{
			TransactionId = 101,
			OriginalTransactionId = 100,
			Price = 11m,
			Volume = 6m,
		};

		manager.ProcessInMessage(replaceMsg);

		// Now receive an execution for the replace transaction with empty SecurityId
		var execMsg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = default,
			TransactionId = 0,
			OriginalTransactionId = 101,
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(execMsg);

		forward.AssertNotNull();
		((ExecutionMessage)forward).SecurityId.AssertEqual(secId);
	}
}
