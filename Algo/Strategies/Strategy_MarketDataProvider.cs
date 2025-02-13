namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	private IMarketDataProvider MarketDataProvider => SafeGetConnector();

	/// <inheritdoc />
	public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged
	{
		add => MarketDataProvider.ValuesChanged += value;
		remove => MarketDataProvider.ValuesChanged -= value;
	}

	/// <inheritdoc />
	public object GetSecurityValue(Security security, Level1Fields field)
		=> MarketDataProvider.GetSecurityValue(security, field);

	/// <inheritdoc />
	public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		=> MarketDataProvider.GetLevel1Fields(security);

	/// <inheritdoc />
	public event Action<SecurityLookupMessage, IEnumerable<Security>, Exception> LookupSecuritiesResult
	{
		add => MarketDataProvider.LookupSecuritiesResult += value;
		remove => MarketDataProvider.LookupSecuritiesResult -= value;
	}

	/// <inheritdoc />
	public event Action<SecurityLookupMessage, IEnumerable<Security>, IEnumerable<Security>, Exception> LookupSecuritiesResult2
	{
		add => MarketDataProvider.LookupSecuritiesResult2 += value;
		remove => MarketDataProvider.LookupSecuritiesResult2 -= value;
	}

	/// <inheritdoc />
	public event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult
	{
		add => MarketDataProvider.LookupBoardsResult += value;
		remove => MarketDataProvider.LookupBoardsResult -= value;
	}

	/// <inheritdoc />
	public event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult2
	{
		add => MarketDataProvider.LookupBoardsResult2 += value;
		remove => MarketDataProvider.LookupBoardsResult2 -= value;
	}

	/// <inheritdoc />
	public event Action<TimeFrameLookupMessage, IEnumerable<TimeSpan>, Exception> LookupTimeFramesResult
	{
		add => MarketDataProvider.LookupTimeFramesResult += value;
		remove => MarketDataProvider.LookupTimeFramesResult -= value;
	}

	/// <inheritdoc />
	public event Action<TimeFrameLookupMessage, IEnumerable<TimeSpan>, IEnumerable<TimeSpan>, Exception> LookupTimeFramesResult2
	{
		add => MarketDataProvider.LookupTimeFramesResult2 += value;
		remove => MarketDataProvider.LookupTimeFramesResult2 -= value;
	}
}