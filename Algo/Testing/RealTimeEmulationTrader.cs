namespace StockSharp.Algo.Testing;

/// <summary>
/// The simulation connection, intended for strategy testing with real connection to trading system through <see cref="RealTimeEmulationTrader{T}.UnderlyngMarketDataAdapter"/>, but without real registering orders on stock. Execution of orders and their trades are emulated by connection, using information by order books, coming from real connection.
/// </summary>
/// <typeparam name="TUnderlyingMarketDataAdapter">The type <see cref="IMessageAdapter"/>, through which market data will be received.</typeparam>
public class RealTimeEmulationTrader<TUnderlyingMarketDataAdapter> : BaseEmulationConnector
	where TUnderlyingMarketDataAdapter : class, IMessageAdapter
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
	/// </summary>
	/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, ISecurityProvider securityProvider)
		: this(underlyngMarketDataAdapter, securityProvider, Portfolio.CreateSimulator())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
	/// </summary>
	/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolio">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="ownAdapter">Track the connection <paramref name="underlyngMarketDataAdapter" /> lifetime.</param>
	public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, ISecurityProvider securityProvider, Portfolio portfolio, bool ownAdapter = true)
		: this(underlyngMarketDataAdapter, securityProvider, new CollectionPortfolioProvider([portfolio]), new InMemoryExchangeInfoProvider(), ownAdapter)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="RealTimeEmulationTrader{T}"/>.
	/// </summary>
	/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, through which market data will be got.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="ownAdapter">Track the connection <paramref name="underlyngMarketDataAdapter" /> lifetime.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, bool ownAdapter = true)
		: base(new EmulationMessageAdapter(underlyngMarketDataAdapter, new InMemoryMessageChannel(new MessageByOrderQueue(), "Emulator in", err => err.LogError()), false, securityProvider, portfolioProvider, exchangeInfoProvider) { OwnInnerAdapter = ownAdapter }, ownAdapter, true)
	{
		UpdateSecurityByLevel1 = false;
		UpdateSecurityLastQuotes = false;
		UpdatePortfolioByChange = false;

		Adapter.IgnoreExtraAdapters = true;
	}

	/// <summary>
	/// <see cref="IMessageAdapter"/>, through which market data will be got.
	/// </summary>
	public TUnderlyingMarketDataAdapter UnderlyngMarketDataAdapter => (TUnderlyingMarketDataAdapter)EmulationAdapter.InnerAdapter;
}