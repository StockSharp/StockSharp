namespace SampleOptionQuoting
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	class DummyProvider : CollectionSecurityProvider, IMarketDataProvider, IPositionProvider
	{
		public DummyProvider(IEnumerable<Security> securities, IEnumerable<Position> positions)
			: base(securities)
		{
			_positions = positions ?? throw new ArgumentNullException(nameof(positions));
		}

		event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> IMarketDataProvider.ValuesChanged
		{
			add { }
			remove { }
		}

		MarketDepth IMarketDataProvider.GetMarketDepth(Security security)
		{
			return null;
		}

		object IMarketDataProvider.GetSecurityValue(Security security, Level1Fields field)
		{
			switch (field)
			{
				case Level1Fields.OpenInterest:
					return security.OpenInterest;

				case Level1Fields.ImpliedVolatility:
					return security.ImpliedVolatility;

				case Level1Fields.HistoricalVolatility:
					return security.HistoricalVolatility;

				case Level1Fields.Volume:
					return security.Volume;

				case Level1Fields.LastTradePrice:
					return security.LastTrade?.Price;

				case Level1Fields.LastTradeVolume:
					return security.LastTrade?.Volume;

				case Level1Fields.BestBidPrice:
					return security.BestBid?.Price;

				case Level1Fields.BestBidVolume:
					return security.BestBid?.Volume;

				case Level1Fields.BestAskPrice:
					return security.BestAsk?.Price;

				case Level1Fields.BestAskVolume:
					return security.BestAsk?.Volume;
			}

			return null;
		}

		IEnumerable<Level1Fields> IMarketDataProvider.GetLevel1Fields(Security security)
		{
			return new[]
			{
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
			};
		}

		event Action<Trade> IMarketDataProvider.NewTrade
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security> IMarketDataProvider.NewSecurity
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security> IMarketDataProvider.SecurityChanged
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<MarketDepth> IMarketDataProvider.NewMarketDepth
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<MarketDepth> IMarketDataProvider.MarketDepthChanged
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<MarketDepth> IMarketDataProvider.FilteredMarketDepthChanged
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<OrderLogItem> IMarketDataProvider.NewOrderLogItem
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<News> IMarketDataProvider.NewNews
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<News> IMarketDataProvider.NewsChanged
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

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

		event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, Exception> IMarketDataProvider.LookupBoardsResult
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, IEnumerable<ExchangeBoard>, Exception> IMarketDataProvider.LookupBoardsResult2
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<TimeFrameLookupMessage, IEnumerable<TimeSpan>, Exception> IMarketDataProvider.LookupTimeFramesResult
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<TimeFrameLookupMessage, IEnumerable<TimeSpan>, IEnumerable<TimeSpan>, Exception> IMarketDataProvider.LookupTimeFramesResult2
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage> IMarketDataProvider.MarketDataSubscriptionSucceeded
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage, Exception> IMarketDataProvider.MarketDataSubscriptionFailed
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage, SubscriptionResponseMessage> IMarketDataProvider.MarketDataSubscriptionFailed2
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage> IMarketDataProvider.MarketDataUnSubscriptionSucceeded
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage, Exception> IMarketDataProvider.MarketDataUnSubscriptionFailed
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage, SubscriptionResponseMessage> IMarketDataProvider.MarketDataUnSubscriptionFailed2
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, SubscriptionFinishedMessage> IMarketDataProvider.MarketDataSubscriptionFinished
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage, Exception> IMarketDataProvider.MarketDataUnexpectedCancelled
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage> IMarketDataProvider.MarketDataSubscriptionOnline
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		MarketDepth IMarketDataProvider.GetFilteredMarketDepth(Security security)
		{
			throw new NotSupportedException();
		}

		private readonly IEnumerable<Position> _positions;

		IEnumerable<Position> IPositionProvider.Positions => _positions;

		IEnumerable<Portfolio> IPortfolioProvider.Portfolios => _positions.OfType<Portfolio>();

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

		event Action<Portfolio> IPortfolioProvider.NewPortfolio
		{
			add { }
			remove { }
		}

		event Action<Portfolio> IPortfolioProvider.PortfolioChanged
		{
			add { }
			remove { }
		}

		Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limit)
		{
			return _positions.FirstOrDefault(p => p.Security == security && p.Portfolio == portfolio);
		}

		Portfolio IPortfolioProvider.LookupByPortfolioName(string name)
		{
			return _positions.OfType<Portfolio>().FirstOrDefault(p => p.Name.CompareIgnoreCase(name));
		}
	}
}