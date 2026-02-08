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
		toInner[0].AssertSame(statusMsg);
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
		toInner[0].AssertSame(statusMsg);
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

		forward.AssertSame(errorResponse);
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

		forward.AssertSame(execMsg);
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

		forward.AssertSame(execMsg);
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

		forward.AssertSame(execMsg);
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
		logReceiver.Logs.Count(l => l.Message.Contains("Order doesn't have origin trans id")).AssertEqual(1);
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
		logReceiver.Logs.Count(l => l.Message.Contains("suspended")).AssertEqual(1);
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

	[TestMethod]
	public void ProcessOutMessage_SubscriptionOnline_EmitsDataBeforeOnline()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => true);

		var secId = CreateSecurityId();

		// 1. Subscribe with transaction log
		manager.ProcessInMessage(new OrderStatusMessage
		{
			TransactionId = 200,
			IsSubscribe = true,
		});

		// 2. Register order (so secId is tracked)
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = secId,
			Price = 10m,
			Volume = 5m,
		});

		// 3. Execution with order info — gets accumulated
		var orderExec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 100,
			OriginalTransactionId = 200,
			HasOrderInfo = true,
			OrderId = 12345,
			OrderState = OrderStates.Active,
			OrderPrice = 10m,
			OrderVolume = 5m,
			Balance = 5m,
		};

		var (fwd1, _, _) = manager.ProcessOutMessage(orderExec);
		fwd1.AssertNull(); // accumulated, not forwarded

		// 4. Trade execution — also accumulated
		var tradeExec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 100,
			OriginalTransactionId = 200,
			HasOrderInfo = true,
			OrderId = 12345,
			TradeId = 999,
			TradePrice = 10m,
			TradeVolume = 2m,
		};

		var (fwd2, _, _) = manager.ProcessOutMessage(tradeExec);
		fwd2.AssertNull(); // accumulated, not forwarded

		// 5. SubscriptionOnline arrives — should flush accumulated data
		var onlineMsg = new SubscriptionOnlineMessage
		{
			OriginalTransactionId = 200,
		};

		var (forward, extraOut, processSuspended) = manager.ProcessOutMessage(onlineMsg);

		// forward IS the SubscriptionOnline message
		forward.AssertSame(onlineMsg);

		// extraOut should contain: order snapshot + trade
		(extraOut.Length >= 2).AssertTrue();

		// first element is the order snapshot
		var orderSnapshot = (ExecutionMessage)extraOut[0];
		orderSnapshot.HasOrderInfo.AssertTrue();
		orderSnapshot.OrderId.AssertEqual(12345L);

		// last element is the trade
		var trade = (ExecutionMessage)extraOut[^1];
		trade.HasTradeInfo.AssertTrue();
		trade.TradeId.AssertEqual(999L);

		processSuspended.AssertFalse();
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionOnline_DoesNotIncludeSuspendedTrades()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => true);

		var secId = CreateSecurityId();

		// 1. Subscribe with transaction log
		manager.ProcessInMessage(new OrderStatusMessage
		{
			TransactionId = 200,
			IsSubscribe = true,
		});

		// 2. Suspend a trade (order not yet known)
		var suspendedTrade = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 0,
			OriginalTransactionId = 0,
			OrderId = 77777,
			TradeId = 88888,
			TradePrice = 50m,
		};

		manager.ProcessOutMessage(suspendedTrade);

		// 3. Register order and accumulate execution
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = secId,
			Price = 10m,
			Volume = 5m,
		});

		var orderExec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 100,
			OriginalTransactionId = 200,
			HasOrderInfo = true,
			OrderId = 77777,
			OrderState = OrderStates.Active,
			OrderPrice = 10m,
			OrderVolume = 5m,
			Balance = 5m,
		};

		manager.ProcessOutMessage(orderExec);

		// 4. SubscriptionOnline flushes
		var onlineMsg = new SubscriptionOnlineMessage
		{
			OriginalTransactionId = 200,
		};

		var (forward, extraOut, _) = manager.ProcessOutMessage(onlineMsg);

		forward.AssertSame(onlineMsg);

		// extraOut should NOT contain the suspended trade — adapter handles that
		foreach (var msg in extraOut)
		{
			if (msg is ExecutionMessage exec && exec.TradeId == 88888)
				Fail("Suspended trade should not be in extraOut — adapter handles it via GetSuspendedTrades");
		}

		// suspended trade should still be retrievable via GetSuspendedTrades
		var orderForSuspended = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = 77777,
		};

		var suspended = manager.GetSuspendedTrades(orderForSuspended);
		suspended.Length.AssertEqual(1);
		((ExecutionMessage)suspended[0]).TradeId.AssertEqual(88888L);
	}

	[TestMethod]
	public void ProcessOutMessage_SubscriptionFinished_EmitsDataToo()
	{
		var logReceiver = new TestReceiver();
		var manager = new TransactionOrderingManager(logReceiver, () => true);

		var secId = CreateSecurityId();

		// 1. Subscribe with transaction log
		manager.ProcessInMessage(new OrderStatusMessage
		{
			TransactionId = 300,
			IsSubscribe = true,
		});

		// 2. Register + accumulate
		manager.ProcessInMessage(new OrderRegisterMessage
		{
			TransactionId = 150,
			SecurityId = secId,
			Price = 20m,
			Volume = 3m,
		});

		var orderExec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			SecurityId = secId,
			TransactionId = 150,
			OriginalTransactionId = 300,
			HasOrderInfo = true,
			OrderId = 55555,
			OrderState = OrderStates.Done,
			OrderPrice = 20m,
			OrderVolume = 3m,
			Balance = 0m,
		};

		manager.ProcessOutMessage(orderExec);

		// 3. SubscriptionFinished
		var finishedMsg = new SubscriptionFinishedMessage
		{
			OriginalTransactionId = 300,
		};

		var (forward, extraOut, _) = manager.ProcessOutMessage(finishedMsg);

		forward.AssertSame(finishedMsg);

		extraOut.Length.AssertEqual(1);
		var snapshot = (ExecutionMessage)extraOut[0];
		snapshot.HasOrderInfo.AssertTrue();
		snapshot.OrderId.AssertEqual(55555L);
	}
}
