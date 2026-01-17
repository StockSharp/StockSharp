namespace StockSharp.Tests;

using System.Collections.Concurrent;

[TestClass]
public class AsyncExtensionsTests : BaseTestClass
{
	private class MockAdapter : MessageAdapter
	{
		public ConcurrentQueue<Message> SentMessages { get; } = [];
		public Dictionary<long, MarketDataMessage> ActiveSubscriptions { get; } = [];
		public long LastSubscribedId { get; private set; }

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

				case MessageTypes.Reset:
				{
					ActiveSubscriptions.Clear();
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
			await foreach (var l1 in adapter.SubscribeAsync<Level1ChangeMessage>(subMsg, enumCts.Token))
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
			await foreach (var l1 in adapter.SubscribeAsync<Level1ChangeMessage>(subMsg, CancellationToken))
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
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub, enumCts.Token))
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
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub, CancellationToken))
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
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub, enumCts.Token))
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
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub, enumCts.Token))
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
			await foreach (var item in adapter.SubscribeAsync<Level1ChangeMessage>(subscription, cts.Token))
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

		items.Count.AssertGreater(0);
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
			await foreach (var item in adapter.SubscribeAsync<Level1ChangeMessage>(subscription, CancellationToken))
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
}