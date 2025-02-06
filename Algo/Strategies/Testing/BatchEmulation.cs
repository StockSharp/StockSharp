namespace StockSharp.Algo.Strategies.Testing;

/// <summary>
/// </summary>
[Obsolete("Use BruteForceOptimizer.")]
public class BatchEmulation : Optimization.BruteForceOptimizer
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BatchEmulation"/>.
	/// </summary>
	/// <param name="securities">Instruments, the operation will be performed with.</param>
	/// <param name="portfolios">Portfolios, the operation will be performed with.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	public BatchEmulation(IEnumerable<Security> securities, IEnumerable<Portfolio> portfolios, IStorageRegistry storageRegistry)
		: base(securities, portfolios, storageRegistry)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BatchEmulation"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	public BatchEmulation(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IStorageRegistry storageRegistry)
		: base(securityProvider, portfolioProvider, storageRegistry)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BatchEmulation"/>.
	/// </summary>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="storageRegistry">Market data storage.</param>
	/// <param name="storageFormat">The format of market data. <see cref="StorageFormats.Binary"/> is used by default.</param>
	/// <param name="drive">The storage which is used by default. By default, <see cref="IStorageRegistry.DefaultDrive"/> is used.</param>
	public BatchEmulation(ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry, StorageFormats storageFormat = StorageFormats.Binary, IMarketDataDrive drive = null)
		: base(securityProvider, portfolioProvider, exchangeInfoProvider, storageRegistry, storageFormat, drive)
	{
	}

	/// <summary>
	/// Start emulation.
	/// </summary>
	/// <param name="strategies">The strategies.</param>
	/// <param name="iterationCount">Iteration count.</param>
	public void Start(IEnumerable<Strategy> strategies, int iterationCount)
	{
		Start(EmulationSettings.StartTime, EmulationSettings.StopTime, strategies.Select(s => (s, s.GetParameters().ToArray())), iterationCount);
	}
}