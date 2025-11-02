namespace StockSharp.Samples.Strategies.LiveOptionsQuoting;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

class DummyProvider(IEnumerable<Security> securities, IEnumerable<Position> positions) : CollectionSecurityProvider(securities), IMarketDataProvider, IPositionProvider
{
	event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTime, DateTime> IMarketDataProvider.ValuesChanged
	{
		add { }
		remove { }
	}

	object IMarketDataProvider.GetSecurityValue(Security security, Level1Fields field)
		=> field switch
		{
			Level1Fields.OpenInterest => security.OpenInterest,
			Level1Fields.ImpliedVolatility => security.ImpliedVolatility,
			Level1Fields.HistoricalVolatility => security.HistoricalVolatility,
			Level1Fields.Volume => security.Volume,
			Level1Fields.LastTradePrice => security.LastTick?.Price,
			Level1Fields.LastTradeVolume => security.LastTick?.Volume,
			Level1Fields.BestBidPrice => security.BestBid?.Price,
			Level1Fields.BestBidVolume => security.BestBid?.Volume,
			Level1Fields.BestAskPrice => security.BestAsk?.Price,
			Level1Fields.BestAskVolume => security.BestAsk?.Volume,
			_ => null,
		};

	IEnumerable<Level1Fields> IMarketDataProvider.GetLevel1Fields(Security security)
		=>
		[
			Level1Fields.OpenInterest,
			Level1Fields.ImpliedVolatility,
			Level1Fields.HistoricalVolatility,
			Level1Fields.Volume,
			Level1Fields.LastTradePrice,
			Level1Fields.LastTradeVolume,
			Level1Fields.BestBidPrice,
			Level1Fields.BestAskPrice,
			Level1Fields.BestBidVolume,
			Level1Fields.BestAskVolume
		];

	event Action<SecurityLookupMessage, IEnumerable<Security>, Exception> IMarketDataProvider.LookupSecuritiesResult
	{
		add => throw new NotSupportedException();
		remove => throw new NotSupportedException();
	}

	event Action<SecurityLookupMessage, IEnumerable<Security>, IEnumerable<Security>, Exception> IMarketDataProvider.LookupSecuritiesResult2
	{
		add => throw new NotSupportedException();
		remove => throw new NotSupportedException();
	}

	private readonly IEnumerable<Position> _positions = positions ?? throw new ArgumentNullException(nameof(positions));
	
	IEnumerable<Position> IPositionProvider.Positions => _positions;

	event Action<Position> IPositionProvider.NewPosition
	{
		add { }
		remove { }
	}

	event Action<Position> IPositionProvider.PositionChanged
	{
		add { }
		remove { }
	}

	Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limit)
	{
		return _positions.FirstOrDefault(p => p.Security == security && p.Portfolio == portfolio);
	}
}