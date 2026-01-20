namespace StockSharp.Tests;

using System.Collections.Concurrent;

[TestClass]
public class AsyncExtensionsTests : BaseTestClass
{
	private class MockAdapter : MessageAdapter
	{
		public ConcurrentQueue<Message> SentMessages { get; } = [];
		public Dictionary<long, MarketDataMessage> ActiveSubscriptions { get; } = [];
		public Dictionary<long, OrderRegisterMessage> ActiveOrders { get; } = [];
		public long LastSubscribedId { get; private set; }
		public long LastOrderTransactionId { get; private set; }
		public OrderCancelMessage LastCancelMessage { get; private set; }

		public MockAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();

			this.AddSupportedMarketDataType(DataType.Level1);
		}

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			SentMessages.Enqueue(message);

			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					SendOutMessage(new ConnectMessage());
					break;
				}

				case MessageTypes.Disconnect:
				{
					SendOutMessage(new DisconnectMessage());
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						// ack subscribe
						SendOutMessage(mdMsg.CreateResponse());

						ActiveSubscriptions[mdMsg.TransactionId] = mdMsg;
						LastSubscribedId = mdMsg.TransactionId;

						// For historical subscriptions, send result AFTER data is sent
						// For live subscriptions, send result now to start receiving
						if (mdMsg.To == null)
							SendSubscriptionResult(mdMsg);
					}
					else
					{
						ActiveSubscriptions.Remove(mdMsg.OriginalTransactionId);
						// ack unsubscribe
						SendOutMessage(mdMsg.CreateResponse());
					}

					break;
				}

				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;
					ActiveOrders[regMsg.TransactionId] = regMsg;
					LastOrderTransactionId = regMsg.TransactionId;
					// Don't auto-respond - let tests control responses via SimulateOrderExecution
					break;
				}

				case MessageTypes.OrderCancel:
				{
					LastCancelMessage = (OrderCancelMessage)message;
					break;
				}

				case MessageTypes.Reset:
				{
					ActiveSubscriptions.Clear();
					ActiveOrders.Clear();
					break;
				}
			}

			return default;
		}

		public void SimulateData(long subscriptionId, Message data)
		{
			if (data is ISubscriptionIdMessage sid)
				sid.SetSubscriptionIds([subscriptionId]);

			SendOutMessage(data);
		}

		public void FinishHistoricalSubscription(long subscriptionId)
		{
			if (ActiveSubscriptions.TryGetValue(subscriptionId, out var mdMsg))
				SendSubscriptionResult(mdMsg);
		}

		public void SimulateOrderExecution(long origTransId, OrderStates? state = null, long? orderId = null,
			decimal? tradePrice = null, decimal? tradeVolume = null, long? tradeId = null, Exception error = null)
		{
			var exec = new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = origTransId,
				OrderId = orderId,
				OrderState = state,
				TradePrice = tradePrice,
				TradeVolume = tradeVolume,
				TradeId = tradeId,
				Error = error,
				HasOrderInfo = state != null || orderId != null || error != null,
				ServerTime = DateTime.UtcNow,
			};

			if (ActiveOrders.TryGetValue(origTransId, out var regMsg))
				exec.SecurityId = regMsg.SecurityId;

			SendOutMessage(exec);
		}
	}

	private class MockAsyncAdapter : HistoricalMessageAdapter
	{
		public ConcurrentQueue<Message> SentMessages { get; } = [];
		public Dictionary<long, MarketDataMessage> ActiveSubscriptions { get; } = [];
		public long LastSubscribedId { get; private set; }

		public MockAsyncAdapter(IdGenerator transactionIdGenerator) : base(transactionIdGenerator)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();

			this.AddSupportedMarketDataType(DataType.Level1);
		}

		protected override ValueTask ConnectAsync(ConnectMessage connectMsg, CancellationToken cancellationToken)
		{
			SendOutMessage(new ConnectMessage());
			return default;
		}

		protected override ValueTask DisconnectAsync(DisconnectMessage disconnectMsg, CancellationToken cancellationToken)
		{
			SendOutMessage(new DisconnectMessage());
			return default;
		}

		protected override ValueTask OnLevel1SubscriptionAsync(MarketDataMessage mdMsg, CancellationToken cancellationToken)
		{
			SentMessages.Enqueue(mdMsg);

			if (mdMsg.IsSubscribe)
			{
				SendSubscriptionReply(mdMsg.TransactionId);

				ActiveSubscriptions[mdMsg.TransactionId] = mdMsg;
				LastSubscribedId = mdMsg.TransactionId;

				// For historical subscriptions, send result AFTER data is sent
				// For live subscriptions, send result now to start receiving
				if (mdMsg.To == null)
					SendSubscriptionResult(mdMsg);
			}
			else
			{
				ActiveSubscriptions.Remove(mdMsg.OriginalTransactionId);
				SendSubscriptionReply(mdMsg.OriginalTransactionId);
			}

			return default;
		}

		public void SimulateData(long subscriptionId, Message data)
		{
			if (data is ISubscriptionIdMessage sid)
				sid.SetSubscriptionIds([subscriptionId]);
			SendOutMessage(data);
		}

		public void FinishHistoricalSubscription(long subscriptionId)
		{
			if (ActiveSubscriptions.TryGetValue(subscriptionId, out var mdMsg))
				SendSubscriptionResult(mdMsg);
		}

		public override IMessageAdapter Clone() => new MockAsyncAdapter(TransactionIdGenerator);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Connector_ConnectAsync()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);
		AreEqual(ConnectionStates.Connected, connector.ConnectionState);

		await connector.DisconnectAsync(CancellationToken);
		AreEqual(ConnectionStates.Disconnected, connector.ConnectionState);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Adapter_ConnectAsync()
	{
		var adapter = new MockAdapter(new IncrementalIdGenerator());

		await adapter.ConnectAsync(CancellationToken);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Connector_RegisterOrder_Basic()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		connector.SendOutMessage(security.ToMessage());

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		// Track events
		var orderReceived = new List<Order>();
		connector.OrderReceived += (_, o) => orderReceived.Add(o);

		// Register order
		connector.RegisterOrder(order);

		// Wait for Pending state
		await Task.Run(async () =>
		{
			while (order.State == OrderStates.None)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		AreEqual(OrderStates.Pending, order.State);
		AreNotEqual(0L, order.TransactionId);

		// Simulate acceptance
		adapter.SimulateOrderExecution(order.TransactionId, OrderStates.Active, orderId: 123);

		// Wait for Active state
		await Task.Run(async () =>
		{
			while (order.State != OrderStates.Active)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		AreEqual(OrderStates.Active, order.State);
		AreEqual(123L, order.Id);

		// Check events were received
		orderReceived.Count.AssertGreater(0);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Adapter_Subscription_Live_SyncAdapter()
	{
		var adapter = new MockAdapter(new IncrementalIdGenerator());

		await adapter.ConnectAsync(CancellationToken);

		var subMsg = new MarketDataMessage
		{
			TransactionId = adapter.TransactionIdGenerator.GetNextId(),
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			SecurityId = new SecurityId { SecurityCode = "SBER", BoardCode = "TQBR" },
		};

		var got = new List<Level1ChangeMessage>();
		using var enumCts = new CancellationTokenSource();

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in adapter.SubscribeAsync<Level1ChangeMessage>(subMsg).WithCancellation(enumCts.Token))
			{
				got.Add(l1);
				if (got.Count >= 3)
				{
					enumCts.Cancel();
					break;
				}
			}
		}, CancellationToken);

		// Give time for enumeration to start
		await Task.Delay(200, CancellationToken);

		var id = subMsg.TransactionId;

		for (var i = 0; i < 3; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow };
			adapter.SimulateData(id, l1);
		}

		await enumerating.WithCancellation(CancellationToken);

		HasCount(3, got);

		while (!adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id))
			await Task.Delay(10, CancellationToken);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Adapter_Subscription_History_SyncAdapter()
	{
		var adapter = new MockAdapter(new IncrementalIdGenerator());
		await adapter.ConnectAsync(CancellationToken);

		var subMsg = new MarketDataMessage
		{
			TransactionId = adapter.TransactionIdGenerator.GetNextId(),
			DataType2 = DataType.Level1,
			IsSubscribe = true,
			SecurityId = new SecurityId { SecurityCode = "SBER", BoardCode = "TQBR" },
			From = DateTime.UtcNow.AddDays(-2),
			To = DateTime.UtcNow.AddDays(-1),
		};

		var got = new List<Level1ChangeMessage>();

		var run = Task.Run(async () =>
		{
			await foreach (var l1 in adapter.SubscribeAsync<Level1ChangeMessage>(subMsg).WithCancellation(CancellationToken))
			{
				got.Add(l1);
				if (got.Count >= 2)
					break;
			}
		}, CancellationToken);

		// wait activation
		await Task.Delay(100, CancellationToken);

		var id = subMsg.TransactionId;

		for (var i = 0; i < 2; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow.AddDays(-1).AddMinutes(i) };
			adapter.SimulateData(id, l1);
		}

		adapter.FinishHistoricalSubscription(id);
		await run.WithCancellation(CancellationToken);

		HasCount(2, got);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Subscription_Live_SyncAdapter()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(DataType.Level1);

		var got = new List<Level1ChangeMessage>();
		using var enumCts = new CancellationTokenSource();

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub).WithCancellation(enumCts.Token))
			{
				got.Add(l1);
				if (got.Count >= 3)
				{
					enumCts.Cancel();
					break;
				}
			}
		}, CancellationToken);

		// Wait until subscription is started (online message received)
		await started.Task.WithCancellation(CancellationToken);

		// Give time for enumeration to start
		await Task.Delay(200, CancellationToken);

		var id = adapter.LastSubscribedId;

		for (var i = 0; i < 3; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow };
			adapter.SimulateData(id, l1);
		}

		await enumerating.WithCancellation(CancellationToken);

		HasCount(3, got);

		while (!adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id))
			await Task.Delay(10, CancellationToken);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Subscription_History_SyncAdapter()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(DataType.Level1)
		{
			From = DateTime.UtcNow.AddDays(-2),
			To = DateTime.UtcNow.AddDays(-1),
		};

		var got = new List<Level1ChangeMessage>();

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub).WithCancellation(CancellationToken))
			{
				got.Add(l1);
			}
		}, CancellationToken);

		// Wait for subscription to be processed
		await Task.Run(async () =>
		{
			while (adapter.ActiveSubscriptions.Count == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		// Give time for subscription to fully activate
		await Task.Delay(100, CancellationToken);

		var id = adapter.LastSubscribedId;

		for (var i = 0; i < 2; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow.AddDays(-1).AddMinutes(i) };
			adapter.SimulateData(id, l1);
		}

		// Finish historical subscription after sending all data
		adapter.FinishHistoricalSubscription(id);

		await enumerating.WithCancellation(CancellationToken);

		HasCount(2, got);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Subscription_Live_AsyncAdapter()
	{
		var connector = new Connector();
		var adapter = new MockAsyncAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(DataType.Level1);

		var got = new List<Level1ChangeMessage>();
		using var enumCts = new CancellationTokenSource();

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub).WithCancellation(enumCts.Token))
			{
				got.Add(l1);
				if (got.Count >= 3)
				{
					enumCts.Cancel();
					break;
				}
			}
		}, CancellationToken);

		// Wait until subscription is started (online message received)
		await started.Task.WithCancellation(CancellationToken);

		// Give time for enumeration to start
		await Task.Delay(200, CancellationToken);

		var id = adapter.LastSubscribedId;

		for (var i = 0; i < 3; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow };
			adapter.SimulateData(id, l1);
		}

		await enumerating.WithCancellation(CancellationToken);

		HasCount(3, got);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Subscription_History_AsyncAdapter()
	{
		var connector = new Connector();
		var adapter = new MockAsyncAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(DataType.Level1)
		{
			From = DateTime.UtcNow.AddDays(-2),
			To = DateTime.UtcNow.AddDays(-1),
		};

		var got = new List<Level1ChangeMessage>();
		using var enumCts = new CancellationTokenSource();

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub).WithCancellation(enumCts.Token))
			{
				got.Add(l1);
				if (got.Count >= 2)
				{
					enumCts.Cancel();
					break;
				}
			}
		}, CancellationToken);

		// Wait for subscription to be processed
		await Task.Run(async () =>
		{
			while (adapter.ActiveSubscriptions.Count == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		// Give time for subscription to fully activate
		await Task.Delay(100, CancellationToken);

		var id = adapter.LastSubscribedId;

		for (var i = 0; i < 2; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow.AddDays(-1).AddMinutes(i) };
			adapter.SimulateData(id, l1);
		}

		// Finish historical subscription after sending all data
		adapter.FinishHistoricalSubscription(id);

		await enumerating.WithCancellation(CancellationToken);

		HasCount(2, got);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Subscription_Lifecycle()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var sub = new Subscription(DataType.Level1);

		var started = AsyncHelper.CreateTaskCompletionSource<bool>();
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token).AsTask();

		await started.Task.WithCancellation(CancellationToken);

		var id = adapter.LastSubscribedId;
		AreNotEqual(0L, id);

		// cancel -> triggers UnSubscribe and completes after stop
		runCts.Cancel();
		await run.WithCancellation(CancellationToken);

		IsTrue(adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id));
	}

	#region SubscribeAsync Bug Tests

	/// <summary>
	/// Test that SubscribeAsync (non-generic) does not hang when cancellation token is cancelled
	/// and adapter does not send SubscriptionFinishedMessage.
	///
	/// BUG: Currently the method hangs because it awaits finishedTcs.Task after cancellation,
	/// but not all adapters send SubscriptionFinishedMessage on unsubscribe.
	/// </summary>
	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task SubscribeAsync_LiveSubscription_Cancel_ShouldNotHang()
	{
		// Arrange: adapter that does NOT send SubscriptionFinishedMessage on unsubscribe
		var adapter = new NoFinishedMessageAdapter();

		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			IsSubscribe = true,
			To = null, // live subscription
		};

		using var subCts = new CancellationTokenSource();

		// Act: start subscription
		var subscribeTask = adapter.SubscribeAsync(subscription, subCts.Token).AsTask();

		// Wait for subscription to be established
		await adapter.WaitForSubscriptionStarted(CancellationToken);

		// Cancel the subscription
		subCts.Cancel();

		// Assert: method should complete within 2 seconds, not hang
		var (cts, token) = CancellationToken.CreateChildToken(TimeSpan.FromSeconds(2));
		using (cts)
		{
			var completedTask = await Task.WhenAny(subscribeTask, Task.Delay(Timeout.Infinite, token));

			// If this fails, the method is hanging (bug exists)
			(completedTask == subscribeTask).AssertTrue(
				"SubscribeAsync should not hang when cancelled, even if adapter doesn't send SubscriptionFinishedMessage");
		}
	}

	/// <summary>
	/// Test that SubscribeAsync (non-generic) completes normally for historical subscription
	/// when SubscriptionFinishedMessage is received.
	/// </summary>
	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task SubscribeAsync_HistoricalSubscription_CompletesOnFinished()
	{
		var adapter = new ControlledTestAdapter();

		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			IsSubscribe = true,
			From = DateTime.UtcNow.AddDays(-1),
			To = DateTime.UtcNow, // historical subscription
		};

		// Act: start subscription
		var subscribeTask = adapter.SubscribeAsync(subscription, CancellationToken).AsTask();

		// Wait for subscription to be established
		await adapter.WaitForSubscriptionStarted(CancellationToken);

		// Send finished message
		adapter.SendSubscriptionFinished(subscription.TransactionId);

		// Assert: should complete within 2 seconds
		var (cts, token) = CancellationToken.CreateChildToken(TimeSpan.FromSeconds(2));
		using (cts)
		{
			var completedTask = await Task.WhenAny(subscribeTask, Task.Delay(Timeout.Infinite, token));
			(completedTask == subscribeTask).AssertTrue("SubscribeAsync should complete when SubscriptionFinishedMessage is received");
		}
	}

	/// <summary>
	/// Test that SubscribeAsync (non-generic) throws on subscription error.
	/// </summary>
	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task SubscribeAsync_SubscriptionError_Throws()
	{
		var adapter = new ControlledTestAdapter { AutoSendSubscriptionResponse = false };

		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			IsSubscribe = true,
			To = null,
		};

		// Act: start subscription
		var subscribeTask = adapter.SubscribeAsync(subscription, CancellationToken).AsTask();

		// Wait for subscription message to be received by adapter
		await adapter.WaitForSubscriptionStarted(CancellationToken);

		// Send error response (instead of success)
		adapter.SendSubscriptionError(subscription.TransactionId, new InvalidOperationException("Test error"));

		// Assert: should throw
		await ThrowsExactlyAsync<InvalidOperationException>(async () => await subscribeTask);
	}

	/// <summary>
	/// Test that generic SubscribeAsync completes on cancellation without hanging.
	/// </summary>
	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task SubscribeAsyncGeneric_Cancel_CompletesWithoutHang()
	{
		var adapter = new NoFinishedMessageAdapter();

		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			IsSubscribe = true,
			To = null,
		};

		using var cts = new CancellationTokenSource();

		// Act: start enumeration
		var items = new List<Level1ChangeMessage>();
		var enumerateTask = Task.Run(async () =>
		{
			await foreach (var item in adapter.SubscribeAsync<Level1ChangeMessage>(subscription).WithCancellation(cts.Token))
			{
				items.Add(item);
			}
		}, CancellationToken);

		await adapter.WaitForSubscriptionStarted(CancellationToken);

		// Send some data
		adapter.SendLevel1Data(subscription.TransactionId);

		await Task.Delay(100, CancellationToken);

		// Cancel
		cts.Cancel();

		// Assert: enumeration should complete within 2 seconds
		var (childCts, childToken) = CancellationToken.CreateChildToken(TimeSpan.FromSeconds(2));
		using (childCts)
		{
			var completedTask = await Task.WhenAny(enumerateTask, Task.Delay(Timeout.Infinite, childToken));
			(completedTask == enumerateTask).AssertTrue("SubscribeAsync<T> should complete on cancellation");
		}

		// Sent exactly 1 data message, should receive exactly 1
		HasCount(1, items);
	}

	/// <summary>
	/// Test that generic SubscribeAsync yields messages correctly.
	/// </summary>
	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task SubscribeAsyncGeneric_YieldsMessages()
	{
		var adapter = new ControlledTestAdapter();

		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			IsSubscribe = true,
			From = DateTime.UtcNow.AddDays(-1),
			To = DateTime.UtcNow,
		};

		var items = new List<Level1ChangeMessage>();

		// Act
		var enumerateTask = Task.Run(async () =>
		{
			await foreach (var item in adapter.SubscribeAsync<Level1ChangeMessage>(subscription).WithCancellation(CancellationToken))
			{
				items.Add(item);
			}
		}, CancellationToken);

		await adapter.WaitForSubscriptionStarted(CancellationToken);

		// Send data
		for (int i = 0; i < 5; i++)
		{
			adapter.SendLevel1Data(subscription.TransactionId, 100 + i);
		}

		// Finish
		adapter.SendSubscriptionFinished(subscription.TransactionId);

		await enumerateTask.WithCancellation(CancellationToken);

		// Assert
		items.Count.AssertEqual(5);
	}

	#endregion

	#region Bug Test Helper Adapters

	/// <summary>
	/// Adapter that does NOT send SubscriptionFinishedMessage on unsubscribe.
	/// This simulates real-world adapters that don't always send finish messages.
	/// </summary>
	private class NoFinishedMessageAdapter : MessageAdapter
	{
		private readonly TaskCompletionSource<bool> _subscriptionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();

		public NoFinishedMessageAdapter() : base(new IncrementalIdGenerator()) { }

		public Task WaitForSubscriptionStarted(CancellationToken cancellationToken)
			=> _subscriptionStarted.Task.WithCancellation(cancellationToken);

		public void SendLevel1Data(long subscriptionId, decimal price = 100m)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
				ServerTime = DateTime.UtcNow,
				SubscriptionId = subscriptionId,
			}.TryAdd(Level1Fields.LastTradePrice, price);

			SendOutMessage(msg);
		}

		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			switch (message)
			{
				case MarketDataMessage mdm when mdm.IsSubscribe:
					// Send response (subscription started)
					SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = mdm.TransactionId });
					_subscriptionStarted.TrySetResult(true);
					break;

				case MarketDataMessage mdm when !mdm.IsSubscribe:
					// Unsubscribe - intentionally do NOT send SubscriptionFinishedMessage
					// This is the bug scenario - adapter doesn't confirm unsubscribe
					break;
			}

			return default;
		}
	}

	/// <summary>
	/// Adapter that allows controlled sending of responses.
	/// </summary>
	private class ControlledTestAdapter : MessageAdapter
	{
		private readonly TaskCompletionSource<bool> _subscriptionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();

		public ControlledTestAdapter() : base(new IncrementalIdGenerator()) { }

		/// <summary>
		/// If true, automatically sends success response when subscribe message is received.
		/// Set to false to manually control responses (e.g., for error testing).
		/// </summary>
		public bool AutoSendSubscriptionResponse { get; set; } = true;

		public Task WaitForSubscriptionStarted(CancellationToken cancellationToken)
			=> _subscriptionStarted.Task.WithCancellation(cancellationToken);

		public void SendSubscriptionResponse(long subscriptionId)
		{
			SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = subscriptionId });
		}

		public void SendSubscriptionFinished(long subscriptionId)
		{
			SendOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = subscriptionId });
		}

		public void SendSubscriptionError(long subscriptionId, Exception error)
		{
			SendOutMessage(new SubscriptionResponseMessage
			{
				OriginalTransactionId = subscriptionId,
				Error = error,
			});
		}

		public void SendLevel1Data(long subscriptionId, decimal price = 100m)
		{
			var msg = new Level1ChangeMessage
			{
				SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
				ServerTime = DateTime.UtcNow,
				SubscriptionId = subscriptionId,
			}.TryAdd(Level1Fields.LastTradePrice, price);

			SendOutMessage(msg);
		}

		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			switch (message)
			{
				case MarketDataMessage mdm when mdm.IsSubscribe:
					_subscriptionStarted.TrySetResult(true);
					if (AutoSendSubscriptionResponse)
						SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = mdm.TransactionId });
					break;

				case MarketDataMessage mdm when !mdm.IsSubscribe:
					// Unsubscribe - send response (not finished - finished is for subscription completion)
					SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = mdm.TransactionId });
					break;
			}

			return default;
		}
	}

	#endregion

	#region RegisterOrderAsync Tests

	/// <summary>
	/// Adapter for order lifecycle tests.
	/// </summary>
	private class OrderTestAdapter : MessageAdapter
	{
		private readonly TaskCompletionSource<bool> _orderReceived = AsyncHelper.CreateTaskCompletionSource<bool>();
		private OrderRegisterMessage _lastOrder;
		private OrderCancelMessage _lastCancel;

		public OrderTestAdapter() : base(new IncrementalIdGenerator()) { }

		public OrderRegisterMessage LastOrder => _lastOrder;
		public OrderCancelMessage LastCancel => _lastCancel;

		public Task WaitForOrderReceived(CancellationToken cancellationToken)
			=> _orderReceived.Task.WithCancellation(cancellationToken);

		public void SendOrderExecution(long origTransId, OrderStates? state = null, long? orderId = null,
			decimal? tradePrice = null, decimal? tradeVolume = null, Exception error = null)
		{
			SendOutMessage(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				OriginalTransactionId = origTransId,
				OrderId = orderId,
				OrderState = state,
				TradePrice = tradePrice,
				TradeVolume = tradeVolume,
				Error = error,
				HasOrderInfo = state != null || orderId != null,
				ServerTime = DateTime.UtcNow,
			});
		}

		public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
		{
			switch (message)
			{
				case OrderRegisterMessage reg:
					_lastOrder = reg;
					_orderReceived.TrySetResult(true);
					break;

				case OrderCancelMessage cancel:
					_lastCancel = cancel;
					break;
			}

			return default;
		}
	}

	[TestMethod]
	public void RegisterOrderAsync_NullAdapter_Throws()
	{
		IMessageAdapter adapter = null;
		var order = new OrderRegisterMessage();

		ThrowsExactly<ArgumentNullException>(() => adapter.RegisterOrderAsync(order));
	}

	[TestMethod]
	public void RegisterOrderAsync_NullOrder_Throws()
	{
		var adapter = new OrderTestAdapter();

		ThrowsExactly<ArgumentNullException>(() => adapter.RegisterOrderAsync(null));
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_OrderAccepted_ReturnsExecutions()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var executions = new List<ExecutionMessage>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var exec in adapter.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				executions.Add(exec);
		}, CancellationToken);

		await adapter.WaitForOrderReceived(CancellationToken);

		var transId = order.TransactionId;
		adapter.SendOrderExecution(transId, OrderStates.Active, orderId: 123);
		adapter.SendOrderExecution(transId, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		HasCount(2, executions);
		AreEqual(OrderStates.Active, executions[0].OrderState);
		AreEqual(123L, executions[0].OrderId);
		AreEqual(OrderStates.Done, executions[1].OrderState);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_WithTrades_ReturnsAllExecutions()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var executions = new List<ExecutionMessage>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var exec in adapter.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				executions.Add(exec);
		}, CancellationToken);

		await adapter.WaitForOrderReceived(CancellationToken);

		var transId = order.TransactionId;
		adapter.SendOrderExecution(transId, OrderStates.Active, orderId: 123);
		adapter.SendOrderExecution(transId, orderId: 123, tradePrice: 100.5m, tradeVolume: 5);
		adapter.SendOrderExecution(transId, orderId: 123, tradePrice: 100.6m, tradeVolume: 5);
		adapter.SendOrderExecution(transId, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		HasCount(4, executions);
		AreEqual(100.5m, executions[1].TradePrice);
		AreEqual(5m, executions[1].TradeVolume);
		AreEqual(100.6m, executions[2].TradePrice);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_CancellationSendsCancelMessage()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			PortfolioName = "TestPortfolio",
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		using var cts = new CancellationTokenSource();

		var executions = new List<ExecutionMessage>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var exec in adapter.RegisterOrderAsync(order).WithCancellation(cts.Token))
			{
				executions.Add(exec);
				if (exec.OrderState == OrderStates.Active)
					cts.Cancel();
			}
		}, CancellationToken);

		await adapter.WaitForOrderReceived(CancellationToken);

		var transId = order.TransactionId;
		adapter.SendOrderExecution(transId, OrderStates.Active, orderId: 456);

		// Wait for cancel to be sent
		await Task.Run(async () =>
		{
			while (adapter.LastCancel == null)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		// Send done after cancel
		adapter.SendOrderExecution(transId, OrderStates.Done, orderId: 456);

		await enumTask.WithCancellation(CancellationToken);

		adapter.LastCancel.AssertNotNull();
		AreEqual(456L, adapter.LastCancel.OrderId);
		AreEqual("AAPL", adapter.LastCancel.SecurityId.SecurityCode);
		AreEqual("TestPortfolio", adapter.LastCancel.PortfolioName);
		AreEqual(Sides.Buy, adapter.LastCancel.Side);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_AssignsTransactionIdIfZero()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			TransactionId = 0,
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var enumTask = Task.Run(async () =>
		{
			await foreach (var _ in adapter.RegisterOrderAsync(order).WithCancellation(CancellationToken))
			{ }
		}, CancellationToken);

		await adapter.WaitForOrderReceived(CancellationToken);

		AreNotEqual(0L, order.TransactionId);

		adapter.SendOrderExecution(order.TransactionId, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_CancelledToken_YieldsNothing()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var count = 0;
		await foreach (var _ in adapter.RegisterOrderAsync(order).WithCancellation(cts.Token))
			count++;

		AreEqual(0, count);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_OrderFailed_CompletesWithError()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var executions = new List<ExecutionMessage>();
		Exception caughtError = null;

		var enumTask = Task.Run(async () =>
		{
			try
			{
				await foreach (var exec in adapter.RegisterOrderAsync(order).WithCancellation(CancellationToken))
					executions.Add(exec);
			}
			catch (InvalidOperationException ex)
			{
				caughtError = ex;
			}
		}, CancellationToken);

		await adapter.WaitForOrderReceived(CancellationToken);

		adapter.SendOrderExecution(order.TransactionId, OrderStates.Failed, error: new InvalidOperationException("Order rejected"));

		await enumTask.WithCancellation(CancellationToken);

		HasCount(1, executions);
		AreEqual(OrderStates.Failed, executions[0].OrderState);
		caughtError.AssertNotNull();
		AreEqual("Order rejected", caughtError.Message);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_FiltersOtherTransactions()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var executions = new List<ExecutionMessage>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var exec in adapter.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				executions.Add(exec);
		}, CancellationToken);

		await adapter.WaitForOrderReceived(CancellationToken);

		var transId = order.TransactionId;
		var otherTransId1 = transId + 100;
		var otherTransId2 = transId + 200;

		// Send updates for OTHER transactions - should be filtered out
		adapter.SendOrderExecution(otherTransId1, OrderStates.Active, orderId: 999);
		adapter.SendOrderExecution(otherTransId2, OrderStates.Active, orderId: 888);

		// Send updates for OUR transaction
		adapter.SendOrderExecution(transId, OrderStates.Active, orderId: 123);

		// More updates for other transactions
		adapter.SendOrderExecution(otherTransId1, orderId: 999, tradePrice: 50m, tradeVolume: 5);
		adapter.SendOrderExecution(otherTransId2, OrderStates.Done, orderId: 888);

		// Trade for our order
		adapter.SendOrderExecution(transId, orderId: 123, tradePrice: 100.5m, tradeVolume: 5);

		// More noise
		adapter.SendOrderExecution(otherTransId1, OrderStates.Done, orderId: 999);

		// Complete our order
		adapter.SendOrderExecution(transId, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		// Should only have 3 executions for our order: Active, Trade, Done
		HasCount(3, executions);
		AreEqual(OrderStates.Active, executions[0].OrderState);
		AreEqual(123L, executions[0].OrderId);
		AreEqual(100.5m, executions[1].TradePrice);
		AreEqual(5m, executions[1].TradeVolume);
		AreEqual(OrderStates.Done, executions[2].OrderState);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_MatchesByOrderIdAfterAssignment()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var executions = new List<ExecutionMessage>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var exec in adapter.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				executions.Add(exec);
		}, CancellationToken);

		await adapter.WaitForOrderReceived(CancellationToken);

		var transId = order.TransactionId;

		// First message assigns OrderId
		adapter.SendOrderExecution(transId, OrderStates.Active, orderId: 555);

		// Subsequent messages come with OrderId but different OriginalTransactionId (some exchanges do this)
		// Should still match because we track OrderId after it's assigned
		var exec2 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			OriginalTransactionId = 0, // No transaction ID
			OrderId = 555,             // But has our OrderId
			TradePrice = 100.5m,
			TradeVolume = 3,
			HasOrderInfo = true,
			ServerTime = DateTime.UtcNow,
		};
		adapter.SendOutMessage(exec2);

		// Complete
		adapter.SendOrderExecution(transId, OrderStates.Done, orderId: 555);

		await enumTask.WithCancellation(CancellationToken);

		// Should have all 3: Active, Trade (matched by OrderId), Done
		HasCount(3, executions);
		AreEqual(OrderStates.Active, executions[0].OrderState);
		AreEqual(100.5m, executions[1].TradePrice);
		AreEqual(OrderStates.Done, executions[2].OrderState);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task RegisterOrderAsync_FullLifecycle_AllStatesAndTrades()
	{
		var adapter = new OrderTestAdapter();

		var order = new OrderRegisterMessage
		{
			SecurityId = new SecurityId { SecurityCode = "SBER", BoardCode = "TQBR" },
			PortfolioName = "TestPortfolio",
			Price = 250,
			Volume = 100,
			Side = Sides.Buy,
		};

		var executions = new List<ExecutionMessage>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var exec in adapter.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				executions.Add(exec);
		}, CancellationToken);

		await adapter.WaitForOrderReceived(CancellationToken);

		var transId = order.TransactionId;

		// 1. Order pending (some exchanges send this)
		adapter.SendOrderExecution(transId, OrderStates.Pending);

		// 2. Order accepted and active
		adapter.SendOrderExecution(transId, OrderStates.Active, orderId: 12345);

		// 3. First partial fill - 30 units
		adapter.SendOrderExecution(transId, orderId: 12345, tradePrice: 249.5m, tradeVolume: 30);

		// 4. Second partial fill - 50 units
		adapter.SendOrderExecution(transId, orderId: 12345, tradePrice: 249.8m, tradeVolume: 50);

		// 5. Final fill - 20 units, order complete
		adapter.SendOrderExecution(transId, orderId: 12345, tradePrice: 250.0m, tradeVolume: 20);
		adapter.SendOrderExecution(transId, OrderStates.Done, orderId: 12345);

		await enumTask.WithCancellation(CancellationToken);

		// Verify all states and trades received
		HasCount(6, executions);

		// State progression
		AreEqual(OrderStates.Pending, executions[0].OrderState);
		AreEqual(OrderStates.Active, executions[1].OrderState);
		AreEqual(12345L, executions[1].OrderId);

		// Trades
		AreEqual(249.5m, executions[2].TradePrice);
		AreEqual(30m, executions[2].TradeVolume);

		AreEqual(249.8m, executions[3].TradePrice);
		AreEqual(50m, executions[3].TradeVolume);

		AreEqual(250.0m, executions[4].TradePrice);
		AreEqual(20m, executions[4].TradeVolume);

		// Final state
		AreEqual(OrderStates.Done, executions[5].OrderState);
	}

	#endregion

	#region IConnector.RegisterOrderAsync Tests

	[TestMethod]
	public void Connector_RegisterOrderAsync_NullConnector_Throws()
	{
		IConnector connector = null;
		var order = new Order();

		ThrowsExactly<ArgumentNullException>(() => connector.RegisterOrderAsync(order));
	}

	[TestMethod]
	public void Connector_RegisterOrderAsync_NullOrder_Throws()
	{
		var connector = new Connector();

		ThrowsExactly<ArgumentNullException>(() => connector.RegisterOrderAsync(null));
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Connector_RegisterOrderAsync_CancelledToken_YieldsNothing()
	{
		var connector = new Connector();
		var order = new Order
		{
			Security = new Security { Id = "AAPL@TEST" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		using var cts = new CancellationTokenSource();
		cts.Cancel();

		var count = 0;
		await foreach (var _ in connector.RegisterOrderAsync(order).WithCancellation(cts.Token))
			count++;

		AreEqual(0, count);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Connector_RegisterOrderAsync_OrderAccepted_ReturnsEvents()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		connector.SendOutMessage(security.ToMessage());

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var events = new List<(Order order, MyTrade trade)>();

		// track all OrderReceived events
		var allOrderReceived = new List<Order>();
		connector.OrderReceived += (_, o) => allOrderReceived.Add(o);

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		// Wait for order to reach Pending state (connector processed registration)
		await Task.Run(async () =>
		{
			while (order.State == OrderStates.None)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = order.TransactionId;

		// Simulate order accepted
		adapter.SimulateOrderExecution(transId, OrderStates.Active, orderId: 123);

		// Wait for state change via order object
		await Task.Run(async () =>
		{
			while (order.State != OrderStates.Active)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		// Give connector time to process and fire events
		await Task.Delay(100, CancellationToken);

		// Debug check: did OrderReceived fire?
		allOrderReceived.Count.AssertGreater(0, $"OrderReceived should have fired. Order state: {order.State}, Id: {order.Id}");

		// Simulate order filled
		adapter.SimulateOrderExecution(transId, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		HasCount(2, events);
		AreEqual(OrderStates.Active, events[0].order.State);
		AreEqual(123L, events[0].order.Id);
		AreEqual(OrderStates.Done, events[1].order.State);
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Connector_RegisterOrderAsync_WithTrades_ReturnsTradeEvents()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		connector.SendOutMessage(security.ToMessage());

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var events = new List<(Order order, MyTrade trade)>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// Simulate order accepted
		adapter.SimulateOrderExecution(transId, OrderStates.Active, orderId: 123);
		// Simulate partial fill (trade)
		adapter.SimulateOrderExecution(transId, orderId: 123, tradePrice: 100.5m, tradeVolume: 5, tradeId: 1001);
		// Simulate second fill (trade)
		adapter.SimulateOrderExecution(transId, orderId: 123, tradePrice: 100.6m, tradeVolume: 5, tradeId: 1002);
		// Simulate order done
		adapter.SimulateOrderExecution(transId, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		// Exact verification: 4 events (Active, Trade1, Trade2, Done)
		HasCount(4, events);

		// Event 0: Order Active
		AreEqual(OrderStates.Active, events[0].order.State);
		AreEqual(123L, events[0].order.Id);
		events[0].trade.AssertNull();

		// Event 1: First trade
		events[1].trade.AssertNotNull();
		AreEqual(100.5m, events[1].trade.Trade.Price);
		AreEqual(5m, events[1].trade.Trade.Volume);

		// Event 2: Second trade
		events[2].trade.AssertNotNull();
		AreEqual(100.6m, events[2].trade.Trade.Price);
		AreEqual(5m, events[2].trade.Trade.Volume);

		// Event 3: Order Done
		AreEqual(OrderStates.Done, events[3].order.State);
		events[3].trade.AssertNull();
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Connector_RegisterOrderAsync_CancellationSendsCancelOrder()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		connector.SendOutMessage(security.ToMessage());

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		using var cts = new CancellationTokenSource();

		var events = new List<(Order order, MyTrade trade)>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAsync(order).WithCancellation(cts.Token))
			{
				events.Add(evt);
				if (evt.order.State == OrderStates.Active)
					cts.Cancel(); // Cancel after order becomes active
			}
		}, CancellationToken);

		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// Simulate order accepted
		adapter.SimulateOrderExecution(transId, OrderStates.Active, orderId: 456);

		// Wait for cancel message to be sent
		await Task.Run(async () =>
		{
			while (adapter.LastCancelMessage == null)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		// Simulate order cancelled
		adapter.SimulateOrderExecution(transId, OrderStates.Done, orderId: 456);

		await enumTask.WithCancellation(CancellationToken);

		adapter.LastCancelMessage.AssertNotNull();
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Connector_RegisterOrderAsync_FiltersOtherOrders()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "AAPL@TEST" };
		connector.SendOutMessage(security.ToMessage());

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "Test" },
			Price = 100,
			Volume = 10,
			Side = Sides.Buy,
		};

		var events = new List<(Order order, MyTrade trade)>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;
		var otherTransId1 = transId + 100;
		var otherTransId2 = transId + 200;

		// Send updates for OTHER orders - should be filtered out
		adapter.SimulateOrderExecution(otherTransId1, OrderStates.Active, orderId: 999);
		adapter.SimulateOrderExecution(otherTransId2, OrderStates.Active, orderId: 888);

		// Send update for OUR order
		adapter.SimulateOrderExecution(transId, OrderStates.Active, orderId: 123);

		// More updates for other orders
		adapter.SimulateOrderExecution(otherTransId1, orderId: 999, tradePrice: 50m, tradeVolume: 5, tradeId: 5001);
		adapter.SimulateOrderExecution(otherTransId2, OrderStates.Done, orderId: 888);

		// Trade for our order
		adapter.SimulateOrderExecution(transId, orderId: 123, tradePrice: 100.5m, tradeVolume: 5, tradeId: 2001);

		// More noise
		adapter.SimulateOrderExecution(otherTransId1, OrderStates.Done, orderId: 999);

		// Complete our order
		adapter.SimulateOrderExecution(transId, OrderStates.Done, orderId: 123);

		await enumTask.WithCancellation(CancellationToken);

		// Exact verification: 3 events for our order (Active, Trade, Done)
		HasCount(3, events);

		// Event 0: Active
		AreEqual(OrderStates.Active, events[0].order.State);
		AreEqual(123L, events[0].order.Id);
		events[0].trade.AssertNull();

		// Event 1: Trade
		events[1].trade.AssertNotNull();
		AreEqual(100.5m, events[1].trade.Trade.Price);
		AreEqual(5m, events[1].trade.Trade.Volume);

		// Event 2: Done
		AreEqual(OrderStates.Done, events[2].order.State);
		events[2].trade.AssertNull();
	}

	[TestMethod]
	[Timeout(6000, CooperativeCancellation = true)]
	public async Task Connector_RegisterOrderAsync_FullLifecycle_AllStatesAndTrades()
	{
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(CancellationToken);

		var security = new Security { Id = "SBER@TQBR" };
		connector.SendOutMessage(security.ToMessage());

		var order = new Order
		{
			Security = security,
			Portfolio = new Portfolio { Name = "TestPortfolio" },
			Price = 250,
			Volume = 100,
			Side = Sides.Buy,
		};

		var events = new List<(Order order, MyTrade trade)>();

		var enumTask = Task.Run(async () =>
		{
			await foreach (var evt in connector.RegisterOrderAsync(order).WithCancellation(CancellationToken))
				events.Add(evt);
		}, CancellationToken);

		await Task.Run(async () =>
		{
			while (adapter.LastOrderTransactionId == 0)
				await Task.Delay(10, CancellationToken);
		}, CancellationToken);

		var transId = adapter.LastOrderTransactionId;

		// 1. Order pending
		adapter.SimulateOrderExecution(transId, OrderStates.Pending);

		// 2. Order accepted and active
		adapter.SimulateOrderExecution(transId, OrderStates.Active, orderId: 12345);

		// 3. First partial fill - 30 units
		adapter.SimulateOrderExecution(transId, orderId: 12345, tradePrice: 249.5m, tradeVolume: 30, tradeId: 3001);

		// 4. Second partial fill - 50 units
		adapter.SimulateOrderExecution(transId, orderId: 12345, tradePrice: 249.8m, tradeVolume: 50, tradeId: 3002);

		// 5. Final fill - 20 units
		adapter.SimulateOrderExecution(transId, orderId: 12345, tradePrice: 250.0m, tradeVolume: 20, tradeId: 3003);

		// 6. Order complete
		adapter.SimulateOrderExecution(transId, OrderStates.Done, orderId: 12345);

		await enumTask.WithCancellation(CancellationToken);

		// Exact verification: 6 events (Pending, Active, Trade1, Trade2, Trade3, Done)
		HasCount(6, events);

		// Event 0: Pending
		AreEqual(OrderStates.Pending, events[0].order.State);
		events[0].trade.AssertNull();

		// Event 1: Active
		AreEqual(OrderStates.Active, events[1].order.State);
		AreEqual(12345L, events[1].order.Id);
		events[1].trade.AssertNull();

		// Event 2: Trade 1 - 30 units @ 249.5
		events[2].trade.AssertNotNull();
		AreEqual(249.5m, events[2].trade.Trade.Price);
		AreEqual(30m, events[2].trade.Trade.Volume);

		// Event 3: Trade 2 - 50 units @ 249.8
		events[3].trade.AssertNotNull();
		AreEqual(249.8m, events[3].trade.Trade.Price);
		AreEqual(50m, events[3].trade.Trade.Volume);

		// Event 4: Trade 3 - 20 units @ 250.0
		events[4].trade.AssertNotNull();
		AreEqual(250.0m, events[4].trade.Trade.Price);
		AreEqual(20m, events[4].trade.Trade.Volume);

		// Event 5: Done
		AreEqual(OrderStates.Done, events[5].order.State);
		events[5].trade.AssertNull();
	}

	#endregion

	#region ConnectAndDownloadAsync Tests

	[TestMethod]
	public void ConnectAndDownloadAsync_NullAdapter_Throws()
	{
		IMessageAdapter adapter = null;
		var subscription = new MarketDataMessage { DataType2 = DataType.Ticks };

		Throws<ArgumentNullException>(() => adapter.ConnectAndDownloadAsync<ExecutionMessage>(subscription));
	}

	[TestMethod]
	public void ConnectAndDownloadAsync_NullSubscription_Throws()
	{
		var adapter = new RecordingMessageAdapter();

		Throws<ArgumentNullException>(() => adapter.ConnectAndDownloadAsync<ExecutionMessage>(null));
	}

	[TestMethod]
	public async Task ConnectAndDownloadAsync_ConnectsSubscribesAndDisconnects()
	{
		var adapter = new RecordingMessageAdapter();
		var secId = Helper.CreateSecurityId();
		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			SecurityId = secId,
			IsSubscribe = true,
		};

		var messages = new List<ExecutionMessage>();

		// Start enumeration in background
		var enumerationTask = Task.Run(async () =>
		{
			await foreach (var msg in adapter.ConnectAndDownloadAsync<ExecutionMessage>(subscription).WithCancellation(CancellationToken))
				messages.Add(msg);
		}, CancellationToken);

		// Wait for connect message
		await WaitForConditionAsync(() => adapter.InMessages.OfType<ConnectMessage>().Any(), TimeSpan.FromSeconds(5));

		// Emit connect response
		adapter.EmitOut(new ConnectMessage());

		// Wait for subscription message
		await WaitForConditionAsync(() => adapter.InMessages.OfType<MarketDataMessage>().Any(), TimeSpan.FromSeconds(5));

		var subMsg = adapter.InMessages.OfType<MarketDataMessage>().First();
		var subId = subMsg.TransactionId;

		// Emit subscription response
		adapter.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = subId });

		// Emit data messages
		var tick1 = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 100,
			SubscriptionId = subId,
		};
		var tick2 = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 101,
			SubscriptionId = subId,
		};

		adapter.EmitOut(tick1);
		adapter.EmitOut(tick2);

		// Emit subscription finished
		adapter.EmitOut(new SubscriptionFinishedMessage { OriginalTransactionId = subId });

		// Wait for enumeration to complete
		await enumerationTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);

		// Verify messages received
		messages.Count.AssertEqual(2);
		messages[0].TradePrice.AssertEqual(100);
		messages[1].TradePrice.AssertEqual(101);

		// Verify disconnect was sent
		adapter.InMessages.OfType<DisconnectMessage>().Any().AssertTrue();
	}

	[TestMethod]
	public async Task ConnectAndDownloadAsync_OnCancellation_Disconnects()
	{
		var adapter = new RecordingMessageAdapter();
		var secId = Helper.CreateSecurityId();
		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			SecurityId = secId,
			IsSubscribe = true,
		};

		using var cts = new CancellationTokenSource();
		var messages = new List<ExecutionMessage>();

		// Start enumeration in background
		var enumerationTask = Task.Run(async () =>
		{
			await foreach (var msg in adapter.ConnectAndDownloadAsync<ExecutionMessage>(subscription).WithCancellation(cts.Token))
				messages.Add(msg);
		}, CancellationToken);

		// Wait for connect message
		await WaitForConditionAsync(() => adapter.InMessages.OfType<ConnectMessage>().Any(), TimeSpan.FromSeconds(5));

		// Emit connect response
		adapter.EmitOut(new ConnectMessage());

		// Wait for subscription message
		await WaitForConditionAsync(() => adapter.InMessages.OfType<MarketDataMessage>().Any(), TimeSpan.FromSeconds(5));

		var subMsg = adapter.InMessages.OfType<MarketDataMessage>().First();
		var subId = subMsg.TransactionId;

		// Emit subscription response
		adapter.EmitOut(new SubscriptionResponseMessage { OriginalTransactionId = subId });

		// Emit one data message
		adapter.EmitOut(new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = secId,
			TradePrice = 100,
			SubscriptionId = subId,
		});

		// Wait for message to be received
		await WaitForConditionAsync(() => messages.Count > 0, TimeSpan.FromSeconds(5));

		// Cancel
		await cts.CancelAsync();

		// Wait for enumeration to complete
		try
		{
			await enumerationTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken);
		}
		catch (OperationCanceledException)
		{
			// Expected
		}

		// Verify disconnect was sent
		await WaitForConditionAsync(() => adapter.InMessages.OfType<DisconnectMessage>().Any(), TimeSpan.FromSeconds(5));
		adapter.InMessages.OfType<DisconnectMessage>().Any().AssertTrue();
	}

	[TestMethod]
	public async Task ConnectAndDownloadAsync_OnConnectError_Throws()
	{
		var adapter = new RecordingMessageAdapter();
		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			SecurityId = Helper.CreateSecurityId(),
		};

		// Start enumeration in background
		var enumerationTask = Task.Run(async () =>
		{
			await foreach (var _ in adapter.ConnectAndDownloadAsync<ExecutionMessage>(subscription).WithCancellation(CancellationToken))
			{
				// Should not get here
			}
		}, CancellationToken);

		// Wait for connect message
		await WaitForConditionAsync(() => adapter.InMessages.OfType<ConnectMessage>().Any(), TimeSpan.FromSeconds(5));

		// Emit connect error
		adapter.EmitOut(new ConnectMessage { Error = new InvalidOperationException("Connection failed") });

		// Verify exception is thrown
		await ThrowsAsync<InvalidOperationException>(async () => await enumerationTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken));
	}

	[TestMethod]
	public async Task ConnectAndDownloadAsync_OnSubscriptionError_ThrowsAndDisconnects()
	{
		var adapter = new RecordingMessageAdapter();
		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			SecurityId = Helper.CreateSecurityId(),
		};

		// Start enumeration in background
		var enumerationTask = Task.Run(async () =>
		{
			await foreach (var _ in adapter.ConnectAndDownloadAsync<ExecutionMessage>(subscription).WithCancellation(CancellationToken))
			{
				// Should not get here
			}
		}, CancellationToken);

		// Wait for connect message
		await WaitForConditionAsync(() => adapter.InMessages.OfType<ConnectMessage>().Any(), TimeSpan.FromSeconds(5));

		// Emit connect response
		adapter.EmitOut(new ConnectMessage());

		// Wait for subscription message
		await WaitForConditionAsync(() => adapter.InMessages.OfType<MarketDataMessage>().Any(), TimeSpan.FromSeconds(5));

		var subMsg = adapter.InMessages.OfType<MarketDataMessage>().First();
		var subId = subMsg.TransactionId;

		// Emit subscription error
		adapter.EmitOut(new SubscriptionResponseMessage
		{
			OriginalTransactionId = subId,
			Error = new InvalidOperationException("Subscription failed")
		});

		// Verify exception is thrown
		await ThrowsAsync<InvalidOperationException>(async () => await enumerationTask.WaitAsync(TimeSpan.FromSeconds(5), CancellationToken));

		// Verify disconnect was sent (finally block should execute)
		await WaitForConditionAsync(() => adapter.InMessages.OfType<DisconnectMessage>().Any(), TimeSpan.FromSeconds(5));
		adapter.InMessages.OfType<DisconnectMessage>().Any().AssertTrue();
	}

	private async Task WaitForConditionAsync(Func<bool> condition, TimeSpan timeout)
	{
		var deadline = DateTime.UtcNow + timeout;
		while (!condition() && DateTime.UtcNow < deadline)
			await Task.Delay(10, CancellationToken);
	}

	#endregion
}