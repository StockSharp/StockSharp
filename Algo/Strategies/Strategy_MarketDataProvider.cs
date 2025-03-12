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

	SessionStates? IMarketDataProvider.GetSessionState(ExchangeBoard board)
		=> default;

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

	event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, Exception> IMarketDataProvider.LookupBoardsResult
	{
		add { }
		remove { }
	}

	event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, IEnumerable<ExchangeBoard>, Exception> IMarketDataProvider.LookupBoardsResult2
	{
		add { }
		remove { }
	}

	event Action<DataTypeLookupMessage, IEnumerable<TimeSpan>, Exception> IMarketDataProvider.LookupTimeFramesResult
	{
		add { }
		remove { }
	}

	event Action<DataTypeLookupMessage, IEnumerable<TimeSpan>, IEnumerable<TimeSpan>, Exception> IMarketDataProvider.LookupTimeFramesResult2
	{
		add { }
		remove { }
	}

	event Action<ExchangeBoard, SessionStates> IMarketDataProvider.SessionStateChanged
	{
		add { }
		remove { }
	}
}