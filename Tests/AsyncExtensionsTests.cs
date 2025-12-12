namespace StockSharp.Tests;

[TestClass]
public class AsyncExtensionsTests : BaseTestClass
{
	private class MockAdapter : MessageAdapter
	{
		public List<Message> SentMessages { get; } = [];
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
			SentMessages.Add(message);

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
		public List<Message> SentMessages { get; } = [];
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
			SentMessages.Add(mdMsg);

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
	public async Task Connector_ConnectAsync()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(cts.Token);
		AreEqual(ConnectionStates.Connected, connector.ConnectionState);

		await connector.DisconnectAsync(cts.Token);
		AreEqual(ConnectionStates.Disconnected, connector.ConnectionState);
	}

	[TestMethod]
	public async Task Adapter_ConnectAsync()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		var adapter = new MockAdapter(new IncrementalIdGenerator());

		await adapter.ConnectAsync(cts.Token);
	}

	[TestMethod]
	public async Task Adapter_Subscription_Live_SyncAdapter()
	{
		var token = CancellationToken;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		var adapter = new MockAdapter(new IncrementalIdGenerator());

		await adapter.ConnectAsync(cts.Token);

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
					break;
			}
		}, token);

		// Give time for enumeration to start
		await Task.Delay(200, cts.Token);

		var id = subMsg.TransactionId;

		for (var i = 0; i < 3; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow };
			adapter.SimulateData(id, l1);
		}

		enumCts.Cancel();
		await enumerating.WithTimeout(TimeSpan.FromSeconds(5));

		HasCount(3, got);
		IsTrue(adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id));
	}

	[TestMethod]
	public async Task Adapter_Subscription_History_SyncAdapter()
	{
		var token = CancellationToken;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		var adapter = new MockAdapter(new IncrementalIdGenerator());
		await adapter.ConnectAsync(cts.Token);

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
		}, token);

		// wait activation
		await Task.Delay(100, cts.Token);

		var id = subMsg.TransactionId;

		for (var i = 0; i < 2; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow.AddDays(-1).AddMinutes(i) };
			adapter.SimulateData(id, l1);
		}

		adapter.FinishHistoricalSubscription(id);
		await run.WithTimeout(TimeSpan.FromSeconds(5));

		HasCount(2, got);
	}

	[TestMethod]
	public async Task Subscription_Live_SyncAdapter()
	{
		var token = CancellationToken;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(cts.Token);

		var sub = new Subscription(DataType.Level1);

		var got = new List<Level1ChangeMessage>();
		using var enumCts = new CancellationTokenSource();

		var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub, enumCts.Token))
			{
				got.Add(l1);
				if (got.Count >= 3)
					break;
			}
		}, token);

		// Wait until subscription is started (online message received)
		await started.Task.WithTimeout(TimeSpan.FromSeconds(3));

		// Give time for enumeration to start
		await Task.Delay(200, cts.Token);

		var id = adapter.LastSubscribedId;

		for (var i = 0; i < 3; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow };
			adapter.SimulateData(id, l1);
		}

		enumCts.Cancel();
		await enumerating.WithTimeout(TimeSpan.FromSeconds(5));

		HasCount(3, got);
		IsTrue(adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id));
	}

	[TestMethod]
	public async Task Subscription_History_SyncAdapter()
	{
		var token = CancellationToken;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(cts.Token);

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
					break;
			}
		}, token);

		// Wait for subscription to be processed
		await Task.Run(async () =>
		{
			while (adapter.ActiveSubscriptions.Count == 0)
				await Task.Delay(10, cts.Token);
		}, cts.Token);

		// Give time for subscription to fully activate
		await Task.Delay(100, cts.Token);

		var id = adapter.LastSubscribedId;

		for (var i = 0; i < 2; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow.AddDays(-1).AddMinutes(i) };
			adapter.SimulateData(id, l1);
		}

		// Finish historical subscription after sending all data
		adapter.FinishHistoricalSubscription(id);

		enumCts.Cancel();
		await enumerating.WithTimeout(TimeSpan.FromSeconds(5));

		HasCount(2, got);
		IsTrue(adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id));
	}

	[TestMethod]
	public async Task Subscription_Live_AsyncAdapter()
	{
		var token = CancellationToken;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var connector = new Connector();
		var adapter = new MockAsyncAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(cts.Token);

		var sub = new Subscription(DataType.Level1);

		var got = new List<Level1ChangeMessage>();
		using var enumCts = new CancellationTokenSource();

		var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		var enumerating = Task.Run(async () =>
		{
			await foreach (var l1 in connector.SubscribeAsync<Level1ChangeMessage>(sub, enumCts.Token))
			{
				got.Add(l1);
				if (got.Count >= 3)
					break;
			}
		}, token);

		// Wait until subscription is started (online message received)
		await started.Task.WithTimeout(TimeSpan.FromSeconds(3));

		// Give time for enumeration to start
		await Task.Delay(200, cts.Token);

		var id = adapter.LastSubscribedId;

		for (var i = 0; i < 3; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow };
			adapter.SimulateData(id, l1);
		}

		enumCts.Cancel();
		await enumerating.WithTimeout(TimeSpan.FromSeconds(5));

		HasCount(3, got);
	}

	[TestMethod]
	public async Task Subscription_History_AsyncAdapter()
	{
		var token = CancellationToken;
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var connector = new Connector();
		var adapter = new MockAsyncAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(cts.Token);

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
					break;
			}
		}, token);

		// Wait for subscription to be processed
		await Task.Run(async () =>
		{
			while (adapter.ActiveSubscriptions.Count == 0)
				await Task.Delay(10, cts.Token);
		}, cts.Token);

		// Give time for subscription to fully activate
		await Task.Delay(100, cts.Token);

		var id = adapter.LastSubscribedId;

		for (var i = 0; i < 2; i++)
		{
			var l1 = new Level1ChangeMessage { ServerTime = DateTime.UtcNow.AddDays(-1).AddMinutes(i) };
			adapter.SimulateData(id, l1);
		}

		// Finish historical subscription after sending all data
		adapter.FinishHistoricalSubscription(id);

		enumCts.Cancel();
		await enumerating.WithTimeout(TimeSpan.FromSeconds(5));

		HasCount(2, got);
	}

	[TestMethod]
	public async Task Subscription_Lifecycle()
	{
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var connector = new Connector();
		var adapter = new MockAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(adapter);

		await connector.ConnectAsync(cts.Token);

		var sub = new Subscription(DataType.Level1);

		var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		connector.SubscriptionStarted += s => { if (ReferenceEquals(s, sub)) started.TrySetResult(true); };

		using var runCts = new CancellationTokenSource();
		var run = connector.SubscribeAsync(sub, runCts.Token).AsTask();

		await started.Task.WithTimeout(TimeSpan.FromSeconds(3));

		var id = adapter.LastSubscribedId;
		Assert.IsGreaterThan(0, id);

		// cancel -> triggers UnSubscribe and completes after stop
		runCts.Cancel();
		await run.WithTimeout(TimeSpan.FromSeconds(3));

		IsTrue(adapter.SentMessages.OfType<MarketDataMessage>().Any(m => !m.IsSubscribe && m.OriginalTransactionId == id));
	}
}

static class TestTaskExtensions
{
	public static async Task WithTimeout(this Task task, TimeSpan timeout)
	{
		using var cts = new CancellationTokenSource(timeout);
		var delay = Task.Delay(Timeout.Infinite, cts.Token);
		
		var completed = await Task.WhenAny(task, delay).NoWait();
		
		if (completed == delay)
			throw new TimeoutException("Task did not complete in time.");

		await task.NoWait();
	}

	public static async Task<T> WithTimeout<T>(this Task<T> task, TimeSpan timeout)
	{
		using var cts = new CancellationTokenSource(timeout);
		var delay = Task.Delay(Timeout.Infinite, cts.Token);

		var completed = await Task.WhenAny(task, delay).NoWait();

		if (completed == delay)
			throw new TimeoutException("Task did not complete in time.");

		return await task.NoWait();
	}
}