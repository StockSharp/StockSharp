namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Commissions;
using StockSharp.Algo.Risk;

[TestClass]
public class MultithreadedStressTests : BaseTestClass
{
	private const int _durationSeconds = 5;
	private static readonly int _workerCount = Math.Max(4, Environment.ProcessorCount * 2);

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

		var tasks = Enumerable.Range(0, _workerCount).Select(_ => Task.Run(() =>
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
		}, token)).ToArray();

		try
		{
			await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(_durationSeconds + 5));
		}
		catch (TimeoutException)
		{
		}
		catch (TaskCanceledException)
		{
		}

		var list = provider.All.ToArray();
		if (list.Length != list.Distinct().Count())
			throw new InvalidOperationException("Provider.All contains duplicate entries after concurrent operations.");

		if (exceptions.Any())
			throw new AggregateException("Exceptions occurred during provider stress test.", exceptions);
	}
}
