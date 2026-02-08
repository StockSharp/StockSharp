namespace StockSharp.Algo.Strategies.Optimization;

/// <summary>
/// The brute force optimizer of strategies.
/// </summary>
public class BruteForceOptimizer : BaseOptimizer
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BruteForceOptimizer"/>.
	/// </summary>
	/// <param name="securities">Instruments, the operation will be performed with.</param>
	/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	public BruteForceOptimizer(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
		: this(new CollectionSecurityProvider(securities), new CollectionPortfolioProvider(portfolios), storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry, StorageFormats.Binary, storageRegistry.DefaultDrive)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BruteForceOptimizer"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	public BruteForceOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IStorageRegistry storageRegistry)
		: this(securityProvider, portfolioProvider, storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry, StorageFormats.Binary, storageRegistry.DefaultDrive)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BruteForceOptimizer"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
	/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
	public BruteForceOptimizer(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat = StorageFormats.Binary, IMarketDataDrive drive = null)
		: base(securityProvider, portfolioProvider, exchangeInfoProvider, storageRegistry, storageFormat, drive)
	{
	}

	/// <summary>
	/// Run optimization and yield completed iterations as they finish.
	/// </summary>
	/// <param name="startTime">Date in history for starting the paper trading.</param>
	/// <param name="stopTime">Date in history to stop the paper trading (date is included).</param>
	/// <param name="strategies">The strategies and parameters used for optimization.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of completed (strategy, parameters) pairs.</returns>
	public IAsyncEnumerable<(Strategy Strategy, IStrategyParam[] Parameters)> RunAsync(
		DateTime startTime, DateTime stopTime,
		IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> strategies,
		CancellationToken cancellationToken = default)
	{
		var enumerator = strategies.CheckOnNull(nameof(strategies)).GetEnumerator();

		return RunAsync(startTime, stopTime, pfProvider =>
		{
			if (!enumerator.MoveNext())
				return null;

			var strategy = enumerator.Current.strategy;
			strategy.Portfolio = pfProvider.LookupByPortfolioName((strategy.Portfolio?.Name).IsEmpty(Messages.Extensions.SimulatorPortfolioName));
			return enumerator.Current;
		}, cancellationToken);
	}

	/// <summary>
	/// Run optimization and yield completed iterations as they finish.
	/// </summary>
	/// <param name="startTime">Date in history for starting the paper trading.</param>
	/// <param name="stopTime">Date in history to stop the paper trading (date is included).</param>
	/// <param name="tryGetNext">Handler to try to get next strategy object.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Async enumerable of completed (strategy, parameters) pairs.</returns>
	public async IAsyncEnumerable<(Strategy Strategy, IStrategyParam[] Parameters)> RunAsync(
		DateTime startTime, DateTime stopTime,
		Func<IPortfolioProvider, (Strategy strategy, IStrategyParam[] parameters)?> tryGetNext,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		if (tryGetNext is null)
			throw new ArgumentNullException(nameof(tryGetNext));

		var maxIters = EmulationSettings.MaxIterations;
		var totalIterations = maxIters > 0 ? maxIters : int.MaxValue;

		InitializeRunAsync(totalIterations, cancellationToken);

		var batchSize = EmulationSettings.BatchSize;

		var workers = new Task[batchSize];

		for (var i = 0; i < batchSize; i++)
		{
			var adapterCache = AllocateAdapterCache();
			var storageCache = AllocateStorageCache();

			workers[i] = Task.Run(async () =>
			{
				try
				{
					while (await TryNextRunAsync(startTime, stopTime, tryGetNext, adapterCache, storageCache, cancellationToken))
					{
					}
				}
				finally
				{
					FreeAdapterCache(adapterCache);
					FreeStorageCache(storageCache);
				}
			}, cancellationToken);
		}

		// When all workers complete, complete the channel
		_ = Task.Run(async () =>
		{
			try
			{
				await Task.WhenAll(workers);
			}
			catch
			{
				// workers may throw on cancellation
			}
			finally
			{
				CompleteChannel();
			}
		}, CancellationToken.None);

		await foreach (var result in ReadResultsAsync(cancellationToken))
		{
			yield return result;
		}
	}
}
