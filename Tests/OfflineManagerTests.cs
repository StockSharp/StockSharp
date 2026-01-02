namespace StockSharp.Tests;

[TestClass]
public class OfflineManagerTests : BaseTestClass
{
	private sealed class TestReceiver : TestLogReceiver
	{
	}

	[TestMethod]
	public void Reset_ClearsState()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Store a subscription while offline
		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		manager.ProcessInMessage(subscription);

		// Reset clears state
		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new ResetMessage());

		shouldForward.AssertTrue();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Connect_AlwaysForwardsMessage()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new ConnectMessage());

		shouldForward.AssertTrue();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Disconnect_AlwaysForwardsMessage()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new DisconnectMessage());

		shouldForward.AssertTrue();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Time_NotConnected_DoesNotForward()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new TimeMessage());

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Time_NotConnected_IgnoreMode_Forwards()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var timeMsg = new TimeMessage { OfflineMode = MessageOfflineModes.Ignore };
		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(timeMsg);

		shouldForward.AssertTrue();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Time_Connected_Forwards()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Connect
		manager.ProcessOutMessage(new ConnectMessage());

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new TimeMessage());

		shouldForward.AssertTrue();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void OrderRegister_NotConnected_StoresMessage()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(orderMsg);

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void OrderRegister_Connected_ForwardsMessage()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Connect
		manager.ProcessOutMessage(new ConnectMessage());

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(orderMsg);

		shouldForward.AssertTrue();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void OrderCancel_NotConnected_PendingOrder_EmitsExecutionMessage()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Register order first
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
			OrderType = OrderTypes.Limit,
		};

		manager.ProcessInMessage(orderMsg);

		// Cancel the pending order
		var cancelMsg = new OrderCancelMessage
		{
			TransactionId = 101,
			OriginalTransactionId = 100,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(cancelMsg);

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);

		var execMsg = (ExecutionMessage)toOut[0];
		execMsg.DataTypeEx.AssertEqual(DataType.Transactions);
		execMsg.HasOrderInfo.AssertTrue();
		execMsg.OriginalTransactionId.AssertEqual(101);
		execMsg.OrderState.AssertEqual(OrderStates.Done);
		execMsg.OrderType.AssertEqual(OrderTypes.Limit);
	}

	[TestMethod]
	public void OrderCancel_NotConnected_NoPendingOrder_StoresCancel()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Cancel without prior registration
		var cancelMsg = new OrderCancelMessage
		{
			TransactionId = 101,
			OriginalTransactionId = 100,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(cancelMsg);

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void OrderReplace_NotConnected_PendingOrder_EmitsExecutionAndStoresNew()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Register order first
		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
			OrderType = OrderTypes.Limit,
		};

		manager.ProcessInMessage(orderMsg);

		// Replace the pending order
		var replaceMsg = new OrderReplaceMessage
		{
			TransactionId = 101,
			OriginalTransactionId = 100,
			Price = 105m,
			Volume = 15,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(replaceMsg);

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);

		var execMsg = (ExecutionMessage)toOut[0];
		execMsg.DataTypeEx.AssertEqual(DataType.Transactions);
		execMsg.HasOrderInfo.AssertTrue();
		execMsg.OriginalTransactionId.AssertEqual(replaceMsg.TransactionId);
		execMsg.OrderState.AssertEqual(OrderStates.Done);
		execMsg.OrderType.AssertEqual(OrderTypes.Limit);
	}

	[TestMethod]
	public void Subscription_NotConnected_StoresMessage()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(subscription);

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Unsubscription_NotConnected_PendingSubscription_EmitsResponse()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Subscribe first
		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		manager.ProcessInMessage(subscription);

		// Unsubscribe
		var unsubscription = new MarketDataMessage
		{
			IsSubscribe = false,
			TransactionId = 101,
			OriginalTransactionId = 100,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(unsubscription);

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);

		var response = (SubscriptionResponseMessage)toOut[0];
		response.OriginalTransactionId.AssertEqual(101);
	}

	[TestMethod]
	public void ProcessSuspended_SendsSuspendedMessages()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Store messages while offline
		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		};

		manager.ProcessInMessage(subscription);

		var orderMsg = new OrderRegisterMessage
		{
			TransactionId = 200,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
		};

		manager.ProcessInMessage(orderMsg);

		// Process suspended messages
		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new ProcessSuspendedMessage());

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(2);
		toOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Connect_Success_EmitsProcessSuspended()
	{
		var logReceiver = new TestReceiver();
		var processSuspendedCreated = false;
		var manager = new OfflineManager(logReceiver, () =>
		{
			processSuspendedCreated = true;
			return new ProcessSuspendedMessage();
		});

		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(new ConnectMessage());

		suppressOriginal.AssertFalse();
		extraOut.Length.AssertEqual(1);
		extraOut[0].Type.AssertEqual(MessageTypes.ProcessSuspended);
		processSuspendedCreated.AssertTrue();
	}

	[TestMethod]
	public void Connect_Error_DoesNotEmitProcessSuspended()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var connectMsg = new ConnectMessage { Error = new InvalidOperationException("Connection failed") };
		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(connectMsg);

		suppressOriginal.AssertFalse();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Disconnect_SetsDisconnected()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Connect
		manager.ProcessOutMessage(new ConnectMessage());

		// Disconnect
		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(new DisconnectMessage());

		suppressOriginal.AssertFalse();
		extraOut.Length.AssertEqual(0);

		// Verify disconnected state by sending Time message
		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(new TimeMessage());
		shouldForward.AssertFalse(); // Not forwarded because disconnected
	}

	[TestMethod]
	public void ConnectionLost_SetsDisconnected()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Connect
		manager.ProcessOutMessage(new ConnectMessage());

		// Connection lost
		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(new ConnectionLostMessage());

		suppressOriginal.AssertFalse();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ConnectionLost_IsResetState_SuppressesOriginal()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Connect
		manager.ProcessOutMessage(new ConnectMessage());

		// Connection lost with reset state
		var lostMsg = new ConnectionLostMessage { IsResetState = true };
		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(lostMsg);

		suppressOriginal.AssertTrue();
		extraOut.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ConnectionRestored_EmitsProcessSuspended()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		// Connect, then lose connection
		manager.ProcessOutMessage(new ConnectMessage());
		manager.ProcessOutMessage(new ConnectionLostMessage());

		// Connection restored
		var (suppressOriginal, extraOut) = manager.ProcessOutMessage(new ConnectionRestoredMessage());

		suppressOriginal.AssertFalse();
		extraOut.Length.AssertEqual(1);
		extraOut[0].Type.AssertEqual(MessageTypes.ProcessSuspended);
	}

	[TestMethod]
	public void MaxMessageCount_ThrowsWhenExceeded()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage())
		{
			MaxMessageCount = 2
		};

		// Store first message
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 1,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		});

		// Store second message
		manager.ProcessInMessage(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 2,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
		});

		// Third message should throw
		var thrown = false;
		try
		{
			manager.ProcessInMessage(new MarketDataMessage
			{
				IsSubscribe = true,
				TransactionId = 3,
				SecurityId = Helper.CreateSecurityId(),
				DataType2 = DataType.Ticks,
			});
		}
		catch (InvalidOperationException)
		{
			thrown = true;
		}
		thrown.AssertTrue();
	}

	[TestMethod]
	public void MaxMessageCount_InvalidValue_Throws()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var thrown = false;
		try
		{
			manager.MaxMessageCount = -2;
		}
		catch (ArgumentOutOfRangeException)
		{
			thrown = true;
		}
		thrown.AssertTrue();
	}

	[TestMethod]
	public void MaxMessageCount_MinusOne_AllowsUnlimited()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage())
		{
			MaxMessageCount = -1
		};

		// Should not throw
		for (int i = 0; i < 100; i++)
		{
			manager.ProcessInMessage(new MarketDataMessage
			{
				IsSubscribe = true,
				TransactionId = i,
				SecurityId = Helper.CreateSecurityId(),
				DataType2 = DataType.Ticks,
			});
		}
	}

	[TestMethod]
	public void OfflineMode_Cancel_EmitsSubscriptionResult()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
			OfflineMode = MessageOfflineModes.Cancel,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(subscription);

		shouldForward.AssertFalse();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(1);
	}

	[TestMethod]
	public void OfflineMode_Ignore_Forwards()
	{
		var logReceiver = new TestReceiver();
		var manager = new OfflineManager(logReceiver, () => new ProcessSuspendedMessage());

		var subscription = new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			SecurityId = Helper.CreateSecurityId(),
			DataType2 = DataType.Ticks,
			OfflineMode = MessageOfflineModes.Ignore,
		};

		var (toInner, toOut, shouldForward) = manager.ProcessInMessage(subscription);

		shouldForward.AssertTrue();
		toInner.Length.AssertEqual(0);
		toOut.Length.AssertEqual(0);
	}
}
