namespace StockSharp.Tests;

using System.Threading.Tasks.Sources;

[TestClass]
public class AsyncMessageChannelTests : BaseTestClass
{
	private sealed class SingleConsumerValueTaskSource : IValueTaskSource
	{
		private ManualResetValueTaskSourceCore<bool> _core;
		private readonly TaskCompletionSource<bool> _registered = AsyncHelper.CreateTaskCompletionSource<bool>();
		private readonly TaskCompletionSource<bool> _consumed = AsyncHelper.CreateTaskCompletionSource<bool>();
		private int _onCompletedCount;
		private int _getResultCount;

		public SingleConsumerValueTaskSource()
		{
			_core.RunContinuationsAsynchronously = true;
		}

		public int OnCompletedCount => Volatile.Read(ref _onCompletedCount);
		public int GetResultCount => Volatile.Read(ref _getResultCount);
		public Task Registered => _registered.Task;
		public Task Consumed => _consumed.Task;

		public ValueTask CreateValueTask() => new(this, _core.Version);
		public void Complete() => _core.SetResult(true);

		public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			// Record a duplicate without throwing so a regression fails this test instead of crashing testhost.
			if (Interlocked.Increment(ref _onCompletedCount) != 1)
				return;

			_core.OnCompleted(continuation, state, token, flags);
			_registered.TrySetResult(true);
		}

		public void GetResult(short token)
		{
			try
			{
				if (Interlocked.Increment(ref _getResultCount) == 1)
					_core.GetResult(token);
			}
			finally
			{
				_consumed.TrySetResult(true);
			}
		}
	}

	private sealed class DisconnectTimeoutMessageAdapter(TimeSpan timeout)
		: PassThroughMessageAdapter(new IncrementalIdGenerator())
	{
		public override TimeSpan DisconnectTimeout => timeout;
	}

	private static MarketDataMessage CreateUnsubscribe(long originalTransactionId = 1, long transactionId = 2)
	{
		return new MarketDataMessage
		{
			IsSubscribe = false,
			OriginalTransactionId = originalTransactionId,
			TransactionId = transactionId,
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
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task HandlerValueTask_IsConsumedOnce()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 1
		};

		using var channel = new AsyncMessageChannel(adapter);
		var source = new SingleConsumerValueTaskSource();
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var markerProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();
		var asyncMessage = new ExecutionMessage();
		var markerMessage = new ExecutionMessage();

		channel.NewOutMessageAsync += (message, token) =>
		{
			if (message is ConnectMessage)
				connected.TrySetResult(true);
			else if (ReferenceEquals(message, asyncMessage))
				return source.CreateValueTask();
			else if (ReferenceEquals(message, markerMessage))
				markerProcessed.TrySetResult(true);

			return default;
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(asyncMessage, CancellationToken);
		await source.Registered.WithCancellation(CancellationToken);
		await channel.SendInMessageAsync(markerMessage, CancellationToken);

		source.Complete();
		await source.Consumed.WithCancellation(CancellationToken);

		source.OnCompletedCount.AssertEqual(1);
		source.GetResultCount.AssertEqual(1);
		await markerProcessed.Task.WithCancellation(CancellationToken);
	}

	[TestMethod]
	public async Task SendInMessageAsync_RequiresOpenChannel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 1
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
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Close_StopsProcessing()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 1
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var messageStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var messageCompleted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var messageRelease = AsyncHelper.CreateTaskCompletionSource<bool>();
		var processedCount = 0;

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case ExecutionMessage:
					Interlocked.Increment(ref processedCount);
					messageStarted.TrySetResult(true);
					await messageRelease.Task;
					messageCompleted.TrySetResult(true);
					return;
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await messageStarted.Task.WithCancellation(CancellationToken);

		// Keep one message queued behind the in-flight handler.
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);

		try
		{
			channel.Close();
		}
		finally
		{
			messageRelease.TrySetResult(true);
		}

		await messageCompleted.Task.WithCancellation(CancellationToken);

		channel.State.AssertEqual(ChannelStates.Stopped);
		processedCount.AssertEqual(1, "Queued message must not start after Close");

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await Task.Delay(100, CancellationToken);
		processedCount.AssertEqual(1, "Messages sent after Close must be dropped");
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
					using (token.Register(() => throw new InvalidOperationException("Cancellation callback failure.")))
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

		adapter.NewOutMessageAsync += (message, ct) =>
		{
			if (message is SubscriptionResponseMessage resp && resp.OriginalTransactionId == unsubscribeId)
				unsubscribeResponse.TrySetResult(true);
			return default;
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

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Disconnect_WaitsForAllPriorUnsubscribes()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2,
		};

		using var channel = new AsyncMessageChannel(adapter);
		var connectStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var releaseConnect = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var releaseFirst = AsyncHelper.CreateTaskCompletionSource<bool>();
		var firstCancelled = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var releaseSecond = AsyncHelper.CreateTaskCompletionSource<bool>();
		var secondCancelled = AsyncHelper.CreateTaskCompletionSource<bool>();
		var disconnectStarted = AsyncHelper.CreateTaskCompletionSource<bool>();

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connectStarted.TrySetResult(true);
					await releaseConnect.Task.WithCancellation(token);
					return;

				case MarketDataMessage { IsSubscribe: false, TransactionId: 2 }:
					firstStarted.TrySetResult(true);

					try
					{
						await releaseFirst.Task.WithCancellation(token);
					}
					catch (OperationCanceledException)
					{
						firstCancelled.TrySetResult(true);
						throw;
					}

					return;

				case MarketDataMessage { IsSubscribe: false, TransactionId: 3 }:
					secondStarted.TrySetResult(true);

					try
					{
						await releaseSecond.Task.WithCancellation(token);
					}
					catch (OperationCanceledException)
					{
						secondCancelled.TrySetResult(true);
						throw;
					}

					return;

				case DisconnectMessage:
					disconnectStarted.TrySetResult(true);
					return;
			}
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connectStarted.Task.WithCancellation(CancellationToken);

		try
		{
			await channel.SendInMessageAsync(CreateUnsubscribe(1, 2), CancellationToken);
			await channel.SendInMessageAsync(CreateUnsubscribe(2, 3), CancellationToken);
			await channel.SendInMessageAsync(new DisconnectMessage(), CancellationToken);
			releaseConnect.TrySetResult(true);

			var first = await Task.WhenAny(firstStarted.Task, secondStarted.Task, disconnectStarted.Task)
				.WithCancellation(CancellationToken);
			(first == disconnectStarted.Task).AssertFalse("Disconnect overtook prior unsubscribe requests.");

			await firstStarted.Task.WithCancellation(CancellationToken);
			await secondStarted.Task.WithCancellation(CancellationToken);
			await AssertNotCompleted(disconnectStarted.Task, TimeSpan.FromMilliseconds(100), CancellationToken);

			releaseFirst.TrySetResult(true);
			await AssertNotCompleted(disconnectStarted.Task, TimeSpan.FromMilliseconds(100), CancellationToken);

			releaseSecond.TrySetResult(true);
			await disconnectStarted.Task.WithCancellation(CancellationToken);

			firstCancelled.Task.IsCompleted.AssertFalse();
			secondCancelled.Task.IsCompleted.AssertFalse();
		}
		finally
		{
			releaseConnect.TrySetResult(true);
			releaseFirst.TrySetResult(true);
			releaseSecond.TrySetResult(true);
		}
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Disconnect_WaitsForActiveSubscriptionCleanup()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2,
		};

		using var channel = new AsyncMessageChannel(adapter);
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var subscriptionStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var subscriptionCancelled = AsyncHelper.CreateTaskCompletionSource<bool>();
		var releaseSubscriptionCleanup = AsyncHelper.CreateTaskCompletionSource<bool>();
		var unsubscribeResponseStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var releaseUnsubscribeResponse = AsyncHelper.CreateTaskCompletionSource<bool>();
		var unexpectedUnsubscribeForwarded = AsyncHelper.CreateTaskCompletionSource<bool>();
		var disconnectStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var responseSeenByDisconnect = false;
		var unsubscribeResponseCompleted = 0;

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
						await releaseSubscriptionCleanup.Task;
						throw;
					}

					return;

				case MarketDataMessage { IsSubscribe: false }:
					unexpectedUnsubscribeForwarded.TrySetResult(true);
					return;

				case DisconnectMessage:
					responseSeenByDisconnect = Volatile.Read(ref unsubscribeResponseCompleted) != 0;
					disconnectStarted.TrySetResult(true);
					return;
			}
		};

		adapter.NewOutMessageAsync += async (message, token) =>
		{
			if (message is SubscriptionResponseMessage { OriginalTransactionId: 101 })
			{
				unsubscribeResponseStarted.TrySetResult(true);
				await releaseUnsubscribeResponse.Task;
				Volatile.Write(ref unsubscribeResponseCompleted, 1);
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

		channel.Suspend();

		try
		{
			await channel.SendInMessageAsync(CreateUnsubscribe(100, 101), CancellationToken);
			await channel.SendInMessageAsync(new DisconnectMessage(), CancellationToken);
			channel.Resume();

			await subscriptionCancelled.Task.WithCancellation(CancellationToken);
			await AssertNotCompleted(disconnectStarted.Task, TimeSpan.FromMilliseconds(100), CancellationToken);

			releaseSubscriptionCleanup.TrySetResult(true);
			await unsubscribeResponseStarted.Task.WithCancellation(CancellationToken);
			await AssertNotCompleted(disconnectStarted.Task, TimeSpan.FromMilliseconds(100), CancellationToken);

			releaseUnsubscribeResponse.TrySetResult(true);
			await disconnectStarted.Task.WithCancellation(CancellationToken);

			responseSeenByDisconnect.AssertTrue("Disconnect started before subscription cleanup produced the unsubscribe response.");
			unexpectedUnsubscribeForwarded.Task.IsCompleted.AssertFalse();
		}
		finally
		{
			releaseSubscriptionCleanup.TrySetResult(true);
			releaseUnsubscribeResponse.TrySetResult(true);
		}
	}

	[TestMethod]
	[Timeout(5_000, CooperativeCancellation = true)]
	public async Task Disconnect_PriorUnsubscribeTimeoutIsBounded()
	{
		var adapter = new DisconnectTimeoutMessageAdapter(TimeSpan.FromMilliseconds(200))
		{
			MaxParallelMessages = 2,
		};

		using var channel = new AsyncMessageChannel(adapter);
		var connected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var unsubscribeStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var releaseUnsubscribe = AsyncHelper.CreateTaskCompletionSource<bool>();
		var unsubscribeCompleted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var disconnectStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var disconnectFailed = AsyncHelper.CreateTaskCompletionSource<Exception>();

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					connected.TrySetResult(true);
					return;
				case ISubscriptionMessage { IsSubscribe: false }:
					unsubscribeStarted.TrySetResult(true);
					await releaseUnsubscribe.Task;
					unsubscribeCompleted.TrySetResult(true);
					return;
				case DisconnectMessage:
					disconnectStarted.TrySetResult(true);
					return;
			}
		};

		adapter.NewOutMessageAsync += (message, token) =>
		{
			token.ThrowIfCancellationRequested();

			if (message is DisconnectMessage { Error: not null } disconnect)
				disconnectFailed.TrySetResult(disconnect.Error);

			return default;
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await connected.Task.WithCancellation(CancellationToken);
		channel.Suspend();

		try
		{
			await channel.SendInMessageAsync(CreateUnsubscribe(), CancellationToken);
			await channel.SendInMessageAsync(new DisconnectMessage(), CancellationToken);
			channel.Resume();

			var terminal = await Task.WhenAny(disconnectFailed.Task, disconnectStarted.Task)
				.WithCancellation(CancellationToken);
			terminal.AssertSame(disconnectFailed.Task);
			(await disconnectFailed.Task).AssertOfType<TimeoutException>();
			await unsubscribeStarted.Task.WithCancellation(CancellationToken);
			disconnectStarted.Task.IsCompleted.AssertFalse();
		}
		finally
		{
			releaseUnsubscribe.TrySetResult(true);
			await unsubscribeCompleted.Task.WithCancellation(CancellationToken);
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Disconnect_AbortedByCloseDoesNotAffectReopenedChannel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2,
		};

		using var channel = new AsyncMessageChannel(adapter);
		var firstConnected = AsyncHelper.CreateTaskCompletionSource<bool>();
		var reopened = AsyncHelper.CreateTaskCompletionSource<bool>();
		var operationStarted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var operationCancelled = AsyncHelper.CreateTaskCompletionSource<bool>();
		var releaseOperation = AsyncHelper.CreateTaskCompletionSource<bool>();
		var operationCompleted = AsyncHelper.CreateTaskCompletionSource<bool>();
		var staleDisconnect = AsyncHelper.CreateTaskCompletionSource<bool>();
		var staleDisconnectResponse = AsyncHelper.CreateTaskCompletionSource<bool>();
		var markerProcessed = AsyncHelper.CreateTaskCompletionSource<bool>();
		var connectCount = 0;

		channel.NewOutMessageAsync += async (message, token) =>
		{
			switch (message)
			{
				case ConnectMessage:
					if (Interlocked.Increment(ref connectCount) == 1)
						firstConnected.TrySetResult(true);
					else
						reopened.TrySetResult(true);
					return;

				case ExecutionMessage { TransactionId: 1 }:
					operationStarted.TrySetResult(true);
					using (token.Register(() => operationCancelled.TrySetResult(true)))
						await releaseOperation.Task;
					operationCompleted.TrySetResult(true);
					return;

				case ExecutionMessage { TransactionId: 2 }:
					markerProcessed.TrySetResult(true);
					return;

				case DisconnectMessage:
					staleDisconnect.TrySetResult(true);
					return;
			}
		};

		adapter.NewOutMessageAsync += (message, token) =>
		{
			if (message is DisconnectMessage)
				staleDisconnectResponse.TrySetResult(true);

			return default;
		};

		channel.Open();
		await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
		await firstConnected.Task.WithCancellation(CancellationToken);

		try
		{
			await channel.SendInMessageAsync(new ExecutionMessage { TransactionId = 1 }, CancellationToken);
			await operationStarted.Task.WithCancellation(CancellationToken);

			await channel.SendInMessageAsync(new DisconnectMessage(), CancellationToken);
			await operationCancelled.Task.WithCancellation(CancellationToken);

			channel.Close();
			channel.Open();

			await channel.SendInMessageAsync(new ConnectMessage(), CancellationToken);
			await reopened.Task.WithCancellation(CancellationToken);

			releaseOperation.TrySetResult(true);
			await operationCompleted.Task.WithCancellation(CancellationToken);
			await AssertNotCompleted(staleDisconnect.Task, TimeSpan.FromMilliseconds(100), CancellationToken);
			await AssertNotCompleted(staleDisconnectResponse.Task, TimeSpan.FromMilliseconds(100), CancellationToken);

			await channel.SendInMessageAsync(new ExecutionMessage { TransactionId = 2 }, CancellationToken);
			await markerProcessed.Task.WithCancellation(CancellationToken);
			await AssertNotCompleted(staleDisconnect.Task, TimeSpan.FromMilliseconds(100), CancellationToken);
			await AssertNotCompleted(staleDisconnectResponse.Task, TimeSpan.FromMilliseconds(100), CancellationToken);
		}
		finally
		{
			releaseOperation.TrySetResult(true);
		}
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

		adapter.NewOutMessageAsync += (message, ct) =>
		{
			if (message is SubscriptionResponseMessage resp && resp.OriginalTransactionId == transactionId)
				errorResponse.TrySetResult(message);
			return default;
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

		adapter.NewOutMessageAsync += (message, ct) =>
		{
			if (message is ExecutionMessage exec && exec.OriginalTransactionId == transactionId && exec.Error != null)
				errorResponse.TrySetResult(message);
			return default;
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
