namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> IMarketDataProvider.ValuesChanged
	{
		add { }
		remove { }
	}

	object IMarketDataProvider.GetSecurityValue(Security security, Level1Fields field)
		=> default;

	IEnumerable<Level1Fields> IMarketDataProvider.GetLevel1Fields(Security security)
		=> [];

	event Action<SecurityLookupMessage, IEnumerable<Security>, Exception> IMarketDataProvider.LookupSecuritiesResult
	{
		add { }
		remove { }
	}

	event Action<SecurityLookupMessage, IEnumerable<Security>, IEnumerable<Security>, Exception> IMarketDataProvider.LookupSecuritiesResult2
	{
		add { }
		remove { }
	}
}