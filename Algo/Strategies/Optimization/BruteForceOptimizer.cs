namespace StockSharp.Algo.Strategies.Optimization;

/// <summary>
/// The brute force optimizer of strategies.
/// </summary>
public class BruteForceOptimizer : BaseOptimizer
{
	private int _itersCount;
	private int _itersDone;

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
	/// Start optimization.
	/// </summary>
	/// <param name="startTime">Date in history for starting the paper trading.</param>
	/// <param name="stopTime">Date in history to stop the paper trading (date is included).</param>
	/// <param name="strategies">The strategies and parameters used for optimization.</param>
	/// <param name="iterationCount">Iteration count.</param>
	public void Start(DateTime startTime, DateTime stopTime, IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> strategies, int iterationCount)
	{
		var enumerator = strategies.GetEnumerator();

		Start(startTime, stopTime, pfProvider =>
		{
			if (!enumerator.MoveNext())
				return null;

			var strategy = enumerator.Current.strategy;
			strategy.Portfolio = pfProvider.LookupByPortfolioName((strategy.Portfolio?.Name).IsEmpty(Messages.Extensions.SimulatorPortfolioName));
			return enumerator.Current;
		}, iterationCount);
	}

	/// <summary>
	/// Start optimization.
	/// </summary>
	/// <param name="startTime">Date in history for starting the paper trading.</param>
	/// <param name="stopTime">Date in history to stop the paper trading (date is included).</param>
	/// <param name="tryGetNext">Handler to try to get next strategy object.</param>
	/// <param name="iterationCount">Iteration count.</param>
	public void Start(DateTime startTime, DateTime stopTime, Func<IPortfolioProvider, (Strategy strategy, IStrategyParam[] parameters)?> tryGetNext, int iterationCount)
	{
		if (tryGetNext is null)
			throw new ArgumentNullException(nameof(tryGetNext));

		if (iterationCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(iterationCount), iterationCount, LocalizedStrings.InvalidValue);

		var maxIters = EmulationSettings.MaxIterations;
		if (maxIters > 0 && iterationCount > maxIters)
		{
			iterationCount = maxIters;
		}

		_itersCount = iterationCount;
		_itersDone = 0;

		var leftSync = new SyncObject();
		var left = iterationCount;

		OnStart();

		var batchSize = EmulationSettings.BatchSize;

		for (var i = 0; i < batchSize; i++)
		{
			var adapterCache = AllocateAdapterCache();
			var storageCache = AllocateStorageCache();

			void _()
			{
				TryNextRun(startTime, stopTime,
					pfProvider =>
					{
						lock (leftSync)
						{
							if (left <= 0)
								return null;

							left--;
						}

						return tryGetNext(pfProvider);
					},
					adapterCache, storageCache,
					() => _());
			}

			_();
		}
	}

	/// <inheritdoc />
	protected override int GetProgress()
		=> _itersCount == int.MaxValue ? -1 : (int)(++_itersDone * 100.0 / _itersCount);
}