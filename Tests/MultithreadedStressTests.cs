namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Risk;

[TestClass]
// The run is timed: workers stop on a deadline and the wait then requires every one of them to have
// returned. Sharing the machine with the rest of the suite would measure the scheduler, not the code.
[DoNotParallelize]
public class MultithreadedStressTests : BaseTestClass
{
	private const int _durationSeconds = 5;
	private static readonly int _workerCount = 4.Max(Environment.ProcessorCount * 2);

	[Timeout(60000, CooperativeCancellation = true)]
	[TestMethod]
	public async Task RiskRuleProvider()
	{
		await RunProviderStressTestAsync<IRiskRuleProvider, Type>(new InMemoryRiskRuleProvider());
	}

	[Timeout(60000, CooperativeCancellation = true)]
	[TestMethod]
	public async Task CommissionRuleProvider()
	{
		await RunProviderStressTestAsync<ICommissionRuleProvider, Type>(new InMemoryCommissionRuleProvider());
	}

	[Timeout(60000, CooperativeCancellation = true)]
	[TestMethod]
	public async Task IndicatorProvider()
	{
		var provider = new IndicatorProvider();
		provider.Init();
		await RunProviderStressTestAsync<IIndicatorProvider, IndicatorType>(provider);
	}

	private async Task RunProviderStressTestAsync<TProvider, TItem>(TProvider provider)
		where TProvider : ICustomProvider<TItem>
	{
		var typesPool = provider.All.ToArray();
		if (typesPool.Length == 0)
			throw new InvalidOperationException("The provider must contain at least one item to perform the stress test.");

		ConcurrentBag<Exception> exceptions = [];

		var (cts, token) = CancellationToken.CreateChildToken(TimeSpan.FromSeconds(_durationSeconds));

		// Dedicated threads rather than the pool: this test asserts that every worker finished, and a
		// pooled worker that never got scheduled beside the rest of the suite is not the same thing as
		// one that hung inside a provider call.
		var tasks = Enumerable.Range(0, _workerCount).Select(_ => RunOnDedicatedThread(() =>
		{
			while (!token.IsCancellationRequested)
			{
				try
				{
					var t = RandomGen.GetElement(typesPool);

					if (RandomGen.GetBool())
						provider.Add(t);
					else
						provider.Remove(t);
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);

					if (exceptions.Count >= 10)
						cts.Cancel();
				}
			}
		})).ToArray();

		await AwaitWorkersAsync(tasks, TimeSpan.FromSeconds(_durationSeconds + 5));

		var list = provider.All.ToArray();
		if (list.Length != list.Distinct().Count())
			throw new InvalidOperationException("Provider.All contains duplicate entries after concurrent operations.");

		if (exceptions.Any())
			throw new AggregateException("Exceptions occurred during provider stress test.", exceptions);
	}

	// A worker of its own thread, so the wait measures the worker rather than the thread pool.
	private static Task RunOnDedicatedThread(Action action)
	{
		var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var thread = new Thread(() =>
		{
			try
			{
				action();
				source.TrySetResult();
			}
			catch (Exception error)
			{
				source.TrySetException(error);
			}
		}) { IsBackground = true };

		thread.Start();
		return source.Task;
	}

	// Waits for the stress workers and fails naming every worker still running, so that a worker
	// stuck inside a provider call is reported instead of passing as an expired wait.
	private async Task AwaitWorkersAsync(Task[] workers, TimeSpan timeout)
	{
		try
		{
			await Task.WhenAll(workers).WaitAsync(timeout, CancellationToken);
		}
		catch (TimeoutException)
		{
		}
		catch (TaskCanceledException)
		{
		}

		var unfinished = workers
			.Select((w, i) => (worker: w, index: i))
			.Where(p => !p.worker.IsCompleted)
			.Select(p => $"#{p.index}")
			.ToArray();

		if (unfinished.Length > 0)
			Fail($"{unfinished.Length} of {workers.Length} stress worker(s) did not finish within {timeout}: {unfinished.JoinComma()}.");
	}

	// Pins that a worker which has not returned turns the wait into a failure naming that worker,
	// because a hung worker is the defect a multithreaded stress run exists to catch.
	[Timeout(30000, CooperativeCancellation = true)]
	[TestMethod]
	public async Task StressWaitFailsNamingUnfinishedWorkers()
	{
		var release = new TaskCompletionSource();

		Task[] workers = [Task.CompletedTask, release.Task];

		var error = await ThrowsAsync<AssertFailedException>(() => AwaitWorkersAsync(workers, TimeSpan.FromSeconds(1)));

		Contains("#1", error.Message);
		DoesNotContain("#0", error.Message);

		release.SetResult();

		await Task.WhenAll(workers);
	}

	// Pins that a worker ended by cancellation counts as finished, so the completion guard reports
	// only workers still running.
	[Timeout(30000, CooperativeCancellation = true)]
	[TestMethod]
	public async Task StressWaitAcceptsCancelledWorkers()
	{
		var (cts, token) = CancellationToken.CreateChildToken();
		cts.Cancel();

		Task[] workers = [Task.CompletedTask, Task.Run(() => { }, token)];

		await AwaitWorkersAsync(workers, TimeSpan.FromSeconds(5));
	}
}
