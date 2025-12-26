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
		var processed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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

		await processed.Task.WithTimeout(TimeSpan.FromSeconds(2));
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
		var connectStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var connectRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var processed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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
		await connectStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new OrderRegisterMessage(), CancellationToken);
		await channel.SendInMessageAsync(new SecurityLookupMessage(), CancellationToken);
		await channel.SendInMessageAsync(CreateUnsubscribe(), CancellationToken);
		await channel.SendInMessageAsync(new TimeMessage(), CancellationToken);

		connectRelease.TrySetResult(true);
		await processed.Task.WithTimeout(TimeSpan.FromSeconds(2));

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

		var connectStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var connectRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var executionStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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

		await connectStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await AssertNotCompleted(executionStarted.Task, TimeSpan.FromMilliseconds(200), CancellationToken);

		connectRelease.TrySetResult(true);
		await executionStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
	}

	[TestMethod]
	public async Task PingMessages_AreNotParallel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstPingStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondPingStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var otherStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var pingRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
		await connected.Task.WithTimeout(TimeSpan.FromSeconds(2));

		await channel.SendInMessageAsync(new TimeMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new TimeMessage(), CancellationToken);

		await firstPingStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await otherStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await AssertNotCompleted(secondPingStarted.Task, TimeSpan.FromMilliseconds(200), CancellationToken);

		pingRelease.TrySetResult(true);
		await secondPingStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
	}

	[TestMethod]
	public async Task LookupMessages_AreNotParallel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstLookupStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondLookupStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var otherStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var lookupRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
		await connected.Task.WithTimeout(TimeSpan.FromSeconds(2));

		await channel.SendInMessageAsync(new SecurityLookupMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new BoardLookupMessage(), CancellationToken);

		await firstLookupStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await otherStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await AssertNotCompleted(secondLookupStarted.Task, TimeSpan.FromMilliseconds(200), CancellationToken);

		lookupRelease.TrySetResult(true);
		await secondLookupStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
	}

	[TestMethod]
	public async Task TransactionMessages_AreNotParallel()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstTransactionStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondTransactionStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var otherStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var transactionRelease = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
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
		await connected.Task.WithTimeout(TimeSpan.FromSeconds(2));

		await channel.SendInMessageAsync(new OrderRegisterMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new OrderRegisterMessage(), CancellationToken);

		await firstTransactionStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await otherStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await AssertNotCompleted(secondTransactionStarted.Task, TimeSpan.FromMilliseconds(200), CancellationToken);

		transactionRelease.TrySetResult(true);
		await secondTransactionStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
	}

	[TestMethod]
	public async Task ParallelProcessing_AllowsOverlappingMessages()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 2
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var allCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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
		await connected.Task.WithTimeout(TimeSpan.FromSeconds(2));

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);

		await secondStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));

		gate.TrySetResult(true);
		await allCompleted.Task.WithTimeout(TimeSpan.FromSeconds(2));
	}

	[TestMethod]
	public async Task MaxParallelMessages_LimitsConcurrency()
	{
		var adapter = new PassThroughMessageAdapter(new IncrementalIdGenerator())
		{
			MaxParallelMessages = 1
		};

		using var channel = new AsyncMessageChannel(adapter);

		var connected = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var allCompleted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var gate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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
		await connected.Task.WithTimeout(TimeSpan.FromSeconds(2));

		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);
		await channel.SendInMessageAsync(new ExecutionMessage(), CancellationToken);

		await firstStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await Task.Delay(100, CancellationToken);
		secondStarted.Task.IsCompleted.AssertFalse();

		gate.TrySetResult(true);

		await secondStarted.Task.WithTimeout(TimeSpan.FromSeconds(2));
		await allCompleted.Task.WithTimeout(TimeSpan.FromSeconds(2));
	}
}
