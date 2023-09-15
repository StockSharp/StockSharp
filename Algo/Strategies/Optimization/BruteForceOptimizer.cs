namespace StockSharp.Algo.Strategies.Optimization;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Localization;

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
		if (strategies is null)
			throw new ArgumentNullException(nameof(strategies));

		if (iterationCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(iterationCount), iterationCount, LocalizedStrings.Str1219);

		var maxIters = EmulationSettings.MaxIterations;
		if (maxIters > 0 && iterationCount > maxIters)
		{
			iterationCount = maxIters;
			strategies = strategies.Take(iterationCount);
		}

		_itersCount = iterationCount;
		_itersDone = 0;

		OnStart();

		var batchSize = EmulationSettings.BatchSize;

		var enumerator = strategies.GetEnumerator();

		for (var i = 0; i < batchSize; i++)
		{
			var adapterCache = AllocateAdapterCache();
			var storageCache = AllocateStorageCache();

			void _()
			{
				TryNextRun(startTime, stopTime,
					() =>
					{
						if (enumerator.MoveNext())
							return enumerator.Current;

						enumerator.Dispose();
						return default;
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