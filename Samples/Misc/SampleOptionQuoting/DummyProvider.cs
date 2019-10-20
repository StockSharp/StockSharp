namespace SampleOptionQuoting
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

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

		event Action<Security, MarketDataMessage, MarketDataMessage> IMarketDataProvider.MarketDataSubscriptionFailed2
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

		event Action<Security, MarketDataMessage, MarketDataMessage> IMarketDataProvider.MarketDataUnSubscriptionFailed2
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataFinishedMessage> IMarketDataProvider.MarketDataSubscriptionFinished
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		event Action<Security, MarketDataMessage, Exception> IMarketDataProvider.MarketDataUnexpectedCancelled
		{
			add => throw new NotSupportedException();
			remove => throw new NotSupportedException();
		}

		void IMarketDataProvider.LookupSecurities(SecurityLookupMessage criteria)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.LookupBoards(BoardLookupMessage criteria)
		{
			throw new NotSupportedException();
		}

		MarketDepth IMarketDataProvider.GetFilteredMarketDepth(Security security)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.SubscribeMarketData(Security security, MarketDataMessage message)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnSubscribeMarketData(Security security, MarketDataMessage message)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.SubscribeMarketData(MarketDataMessage message)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnSubscribeMarketData(MarketDataMessage message)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.SubscribeMarketDepth(Security security, DateTimeOffset? @from, DateTimeOffset? to, long? count, MarketDataBuildModes buildMode, MarketDataTypes? buildFrom, int? maxDepth, IMessageAdapter adapter)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnSubscribeMarketDepth(Security security)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.RegisterFilteredMarketDepth(Security security)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnRegisterFilteredMarketDepth(Security security)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.SubscribeTrades(Security security, DateTimeOffset? from, DateTimeOffset? to, long? count, MarketDataBuildModes buildMode, MarketDataTypes? buildFrom, IMessageAdapter adapter)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnSubscribeTrades(Security security)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.SubscribeLevel1(Security security, DateTimeOffset? from, DateTimeOffset? to, long? count, MarketDataBuildModes buildMode, MarketDataTypes? buildFrom, IMessageAdapter adapter)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnSubscribeLevel1(Security security)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.SubscribeOrderLog(Security security, DateTimeOffset? from, DateTimeOffset? to, long? count, IMessageAdapter adapter)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnSubscribeOrderLog(Security security)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.SubscribeNews(Security security, DateTimeOffset? from, DateTimeOffset? to, long? count, IMessageAdapter adapter)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnSubscribeNews(Security security)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.SubscribeBoard(ExchangeBoard board, DateTimeOffset? from, DateTimeOffset? to, long? count, IMessageAdapter adapter)
		{
			throw new NotSupportedException();
		}

		void IMarketDataProvider.UnSubscribeBoard(ExchangeBoard board)
		{
			throw new NotSupportedException();
		}

		private readonly IEnumerable<Position> _positions;

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

		Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string clientCode, string depoName)
		{
			return _positions.FirstOrDefault(p => p.Security == security && p.Portfolio == portfolio);
		}

		void IPositionProvider.SubscribePositions(Security security, DateTimeOffset? @from, DateTimeOffset? to, long? count, IMessageAdapter adapter)
		{
		}

		void IPositionProvider.UnSubscribePositions()
		{
		}
	}
}