namespace StockSharp.Tests;

[TestClass]
public class AsyncMessageChannelTests : BaseTestClass
{
	private static MarketDataMessage CreateUnsubscribe()
	{
		return new MarketDataMessage
		{
			IsSubscribe = false,
			OriginalTransactionId = 1,
			TransactionId = 2,
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
		};
	}

	private static async Task AssertNotCompleted(Task task, TimeSpan timeout, CancellationToken cancellationToken)
	{
		var completed = await Task.WhenAny(task, Task.Delay(timeout, cancellationToken));
		(completed == task).AssertFalse();
	}

	[TestMethod]
	public async Task SendInMessageAsync_RequiresOpenChannel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);
		var processed = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (message, token) =>
		{
			if (message is ConnectMessage)
				processed.TrySetResult(true);

			return default;
		};

		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await Task.Delay(100, CancellationToken);
		processed.Task.IsCompleted.AssertFalse();

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);

		await processed.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	public async Task PriorityOrder_ProcessesByCategory()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 1
		};

		using var channel = new AsyncMessageChannel(adapter);

		var order = new List<MessageTypes>();
		var connectStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var connectRelease = AsyncHelper.CreateTaskCompletionSource<bool>();
		var processed = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += async (message, token) =>
		{
			order.Add(message.Type);

			if (message is ConnectMessage)
			{
				connectStarted.TrySetResult(true);
				await connectRelease.Task;
				return;
			}

			if (order.Count == 6)
				processed.TrySetResult(true);
		};

		channel.Open();

		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connectStarted.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new OrderRegisterMessage(), CancellationToken);
		await channel.SendInMessageAsync(new SecurityLookupMessage(), CancellationToken);
		await channel.SendInMessageAsync(CreateUnsubscribe(), CancellationToken);
		await channel.SendInMessageAsync(new TimeMessage(), CancellationToken);

		connectRelease.TrySetResult(true);
		await processed.Task.WithCancellation(CancellationToken);

		order.SequenceEqual(
		[
			MessageTypes.Connect,
			MessageTypes.Time,
			MessageTypes.MarketData,
			MessageTypes.SecurityLookup,
			MessageTypes.OrderRegister,
			MessageTypes.Execution
		]).AssertTrue();
	}

	[TestMethod]
	public async Task ControlMessage_BlocksOtherProcessing()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connectStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var connectRelease = AsyncHelper.CreateTaskCompletionSource<bool>();
		var executionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connectStarted.TrySetResult(true);
					await connectRelease.Task;
					return;
				case ExecutionMessage:
					executionStarted.TrySetResult(true);
					return;
			}
		};

		channel.Open();

		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);

		await connectStarted.Task.WithCancellation(CancellationToken);
		await AssertNotCompleted(executionStarted.Task, TimeSpan.FromMilliseconds(200), CancellationToken);

		connectRelease.TrySetResult(true);
		await executionStarted.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	public async Task PingMessages_AreNotParallel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstPingStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondPingStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var otherStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var pingRelease = AsyncHelper.CreateTaskCompletionSource<bool>();
		var pingCount = 0;

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case TimeMessage:
				{
					if (Interlocked.Increment(ref pingCount) == 1)
					{
						firstPingStarted.TrySetResult(true);
						await pingRelease.Task;
					}
					else
					{
						secondPingStarted.TrySetResult(true);
					}

					return;
				}
				case ExecutionMessage:
					otherStarted.TrySetResult(true);
					return;
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new TimeMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new TimeMessage(), CancellationToken);

		await firstPingStarted.Task.WithCancellation(CancellationToken);
		await otherStarted.Task.WithCancellation(CancellationToken);
		await AssertNotCompleted(secondPingStarted.Task, TimeSpan.FromMilliseconds(200), CancellationToken);

		pingRelease.TrySetResult(true);
		await secondPingStarted.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	public async Task LookupMessages_AreNotParallel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstLookupStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondLookupStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var otherStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var lookupRelease = AsyncHelper.CreateTaskCompletionSource<bool>();
		var lookupCount = 0;

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case SecurityLookupMessage:
				case BoardLookupMessage:
				{
					if (Interlocked.Increment(ref lookupCount) == 1)
					{
						firstLookupStarted.TrySetResult(true);
						await lookupRelease.Task;
					}
					else
					{
						secondLookupStarted.TrySetResult(true);
					}

					return;
				}
				case ExecutionMessage:
					otherStarted.TrySetResult(true);
					return;
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new SecurityLookupMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new BoardLookupMessage(), CancellationToken);

		await firstLookupStarted.Task.WithCancellation(CancellationToken);
		await otherStarted.Task.WithCancellation(CancellationToken);
		await AssertNotCompleted(secondLookupStarted.Task, TimeSpan.FromMilliseconds(200), CancellationToken);

		lookupRelease.TrySetResult(true);
		await secondLookupStarted.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	public async Task TransactionMessages_AreNotParallel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstTransactionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondTransactionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var otherStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var transactionRelease = AsyncHelper.CreateTaskCompletionSource<bool>();
		var transactionCount = 0;

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case OrderRegisterMessage:
				{
					if (Interlocked.Increment(ref transactionCount) == 1)
					{
						firstTransactionStarted.TrySetResult(true);
						await transactionRelease.Task;
					}
					else
					{
						secondTransactionStarted.TrySetResult(true);
					}

					return;
				}
				case ExecutionMessage:
					otherStarted.TrySetResult(true);
					return;
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new OrderRegisterMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new OrderRegisterMessage(), CancellationToken);

		await firstTransactionStarted.Task.WithCancellation(CancellationToken);
		await otherStarted.Task.WithCancellation(CancellationToken);
		await AssertNotCompleted(secondTransactionStarted.Task, TimeSpan.FromMilliseconds(200), CancellationToken);

		transactionRelease.TrySetResult(true);
		await secondTransactionStarted.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	public async Task ParallelProcessing_AllowsOverlappingMessages()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var allCompleted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var gate = AsyncHelper.CreateTaskCompletionSource<bool>();

		var startedCount = 0;
		var completedCount = 0;

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case ExecutionMessage:
				{
					if (Interlocked.Increment(ref startedCount) == 2)
						secondStarted.TrySetResult(true);

					await gate.Task;

					if (Interlocked.Increment(ref completedCount) == 2)
						allCompleted.TrySetResult(true);

					return;
				}
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);

		await secondStarted.Task.WithCancellation(CancellationToken);

		gate.TrySetResult(true);
		await allCompleted.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	public async Task MaxParallelMessages_LimitsConcurrency()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 1
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var allCompleted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var gate = AsyncHelper.CreateTaskCompletionSource<bool>();

		var startedCount = 0;
		var completedCount = 0;

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case ExecutionMessage:
				{
					var started = Interlocked.Increment(ref startedCount);
					if (started == 1)
						firstStarted.TrySetResult(true);
					else if (started == 2)
						secondStarted.TrySetResult(true);

					await gate.Task;

					if (Interlocked.Increment(ref completedCount) == 2)
						allCompleted.TrySetResult(true);

					return;
				}
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);

		await firstStarted.Task.WithCancellation(CancellationToken);
		await Task.Delay(100, CancellationToken);
		secondStarted.Task.IsCompleted.AssertFalse();

		gate.TrySetResult(true);

		await secondStarted.Task.WithCancellation(CancellationToken);
		await allCompleted.Task.WithCancellation(CancellationToken);
	}

	#region Close/Dispose/Reopen Tests

	[TestMethod]
	public async Task Close_StopsProcessing()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var messageStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var messageRelease = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case ExecutionMessage:
					messageStarted.TrySetResult(true);
					await messageRelease.Task;
					return;
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await messageStarted.Task.WithCancellation(CancellationToken);

		messageRelease.TrySetResult(true);
		channel.Close();

		channel.State.AssertEqual(ChannelStates.Stopped);
	}

	[TestMethod]
	public async Task Close_CancelsSubscriptions()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var subscriptionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var subscriptionCancelled = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case MarketDataMessage { IsSubscribe: true }:
					subscriptionStarted.TrySetResult(true);
					try
					{
						await Task.Delay(Timeout.Infinite, token);
					}
					catch (OperationCanceledException)
					{
						subscriptionCancelled.TrySetResult(true);
						throw;
					}
					return;
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
		}, CancellationToken);

		await subscriptionStarted.Task.WithCancellation(CancellationToken);

		channel.Close();

		await subscriptionCancelled.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	public async Task Dispose_ClosesChannel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (message, token) =>
		{
			if (message is ConnectMessage)
				connected.TrySetResult(true);
			return default;
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		channel.Dispose();

		channel.State.AssertEqual(ChannelStates.Stopped);
		await ThrowsExactlyAsync<ObjectDisposedException>(async () =>
			await channel.SendInMessageAsync(new TimeMessage(), CancellationToken));
	}

	[TestMethod]
	public async Task Reopen_WorksAfterClose()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connectCount = 0;
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (message, token) =>
		{
			if (message is ConnectMessage)
			{
				Interlocked.Increment(ref connectCount);
				connected.TrySetResult(true);
			}
			return default;
		};

		// First open
		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);
		connectCount.AssertEqual(1);

		// Close
		channel.Close();
		channel.State.AssertEqual(ChannelStates.Stopped);

		// Reopen
		connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);
		connectCount.AssertEqual(2);
	}

	#endregion

	#region Unsubscribe Tests

	[TestMethod]
	public async Task Unsubscribe_CancelsRunningSubscription()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var subscriptionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var subscriptionCancelled = AsyncHelper.CreateTaskCompletionSource<bool>();
		var unsubscribeResponse = AsyncHelper.CreateTaskCompletionSource<bool>();
		const long subscriptionId = 100;
		const long unsubscribeId = 101;

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case MarketDataMessage { IsSubscribe: true }:
					subscriptionStarted.TrySetResult(true);
					try
					{
						await Task.Delay(Timeout.Infinite, token);
					}
					catch (OperationCanceledException)
					{
						subscriptionCancelled.TrySetResult(true);
						throw;
					}
					return;
			}
		};

		adapter.NewOutMessage += message =>
		{
			if (message is SubscriptionResponseMessage resp && resp.OriginalTransactionId == unsubscribeId)
				unsubscribeResponse.TrySetResult(true);
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = subscriptionId,
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
		}, CancellationToken);

		await subscriptionStarted.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = false,
			OriginalTransactionId = subscriptionId,
			TransactionId = unsubscribeId,
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
		}, CancellationToken);

		await subscriptionCancelled.Task.WithCancellation(CancellationToken);
		await unsubscribeResponse.Task.WithCancellation(CancellationToken);
	}

	#endregion

	#region Disconnect Tests

	[TestMethod]
	public async Task Disconnect_CancelsRunningOperations()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2,
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var subscriptionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var subscriptionCancelled = AsyncHelper.CreateTaskCompletionSource<bool>();
		var disconnected = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case MarketDataMessage { IsSubscribe: true }:
					subscriptionStarted.TrySetResult(true);
					try
					{
						await Task.Delay(Timeout.Infinite, token);
					}
					catch (OperationCanceledException)
					{
						subscriptionCancelled.TrySetResult(true);
						throw;
					}
					return;
				case DisconnectMessage:
					disconnected.TrySetResult(true);
					return;
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = 100,
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
		}, CancellationToken);

		await subscriptionStarted.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new DisconnectMessage(), CancellationToken);

		await subscriptionCancelled.Task.WithCancellation(CancellationToken);
		await disconnected.Task.WithCancellation(CancellationToken);
	}

	#endregion

	#region Error Handling Tests

	[TestMethod]
	public async Task ErrorInHandler_SendsErrorResponse()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2,
			FaultDelay = TimeSpan.Zero,
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var errorResponse = AsyncHelper.CreateTaskCompletionSource<Message>();
		const long transactionId = 100;

		channel.NewOutMessageAsync += (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return default;
				case MarketDataMessage { IsSubscribe: true }:
					throw new InvalidOperationException("Test error");
			}
			return default;
		};

		adapter.NewOutMessage += message =>
		{
			if (message is SubscriptionResponseMessage resp && resp.OriginalTransactionId == transactionId)
				errorResponse.TrySetResult(message);
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new MarketDataMessage
		{
			IsSubscribe = true,
			TransactionId = transactionId,
			DataType2 = DataType.Level1,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
		}, CancellationToken);

		var response = await errorResponse.Task.WithCancellation(CancellationToken);
		response.AssertNotNull();
		((SubscriptionResponseMessage)response).Error.AssertNotNull();
	}

	[TestMethod]
	public async Task TransactionError_SendsErrorResponse()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var errorResponse = AsyncHelper.CreateTaskCompletionSource<Message>();
		const long transactionId = 100;

		channel.NewOutMessageAsync += (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return default;
				case OrderRegisterMessage:
					throw new InvalidOperationException("Order failed");
			}
			return default;
		};

		adapter.NewOutMessage += message =>
		{
			if (message is ExecutionMessage exec && exec.OriginalTransactionId == transactionId && exec.Error != null)
				errorResponse.TrySetResult(message);
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new OrderRegisterMessage
		{
			TransactionId = transactionId,
			SecurityId = new SecurityId { SecurityCode = "TEST", BoardCode = BoardCodes.Test },
			Price = 100,
			Volume = 1,
		}, CancellationToken);

		var response = await errorResponse.Task.WithCancellation(CancellationToken);
		response.AssertNotNull();
		((ExecutionMessage)response).Error.AssertNotNull();
	}

	#endregion

	#region Suspend/Resume Tests

	[TestMethod]
	public async Task Suspend_PausesProcessing()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var messageProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return default;
				case ExecutionMessage:
					messageProcessed.TrySetResult(true);
					return default;
			}
			return default;
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		channel.Suspend();
		channel.State.AssertEqual(ChannelStates.Suspended);

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await AssertNotCompleted(messageProcessed.Task, TimeSpan.FromMilliseconds(300), CancellationToken);

		channel.Resume();
		await messageProcessed.Task.WithCancellation(CancellationToken);
	}

	#endregion
}
