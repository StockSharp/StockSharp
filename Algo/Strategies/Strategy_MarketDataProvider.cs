namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	partial class Strategy
	{
		private IMarketDataProvider MarketDataProvider => SafeGetConnector();

		/// <inheritdoc />
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

		/// <inheritdoc />
		public MarketDepth GetMarketDepth(Security security)
		{
			return MarketDataProvider.GetMarketDepth(security);
		}

		/// <inheritdoc />
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return MarketDataProvider.GetSecurityValue(security, field);
		}

		/// <inheritdoc />
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return MarketDataProvider.GetLevel1Fields(security);
		}

		/// <inheritdoc />
		public event Action<Trade> NewTrade
		{
			add => MarketDataProvider.NewTrade += value;
			remove => MarketDataProvider.NewTrade -= value;
		}

		/// <inheritdoc />
		public event Action<Security> NewSecurity
		{
			add => MarketDataProvider.NewSecurity += value;
			remove => MarketDataProvider.NewSecurity -= value;
		}

		/// <inheritdoc />
		public event Action<Security> SecurityChanged
		{
			add => MarketDataProvider.SecurityChanged += value;
			remove => MarketDataProvider.SecurityChanged -= value;
		}

		/// <inheritdoc />
		public event Action<MarketDepth> NewMarketDepth
		{
			add => MarketDataProvider.NewMarketDepth += value;
			remove => MarketDataProvider.NewMarketDepth -= value;
		}

		/// <inheritdoc />
		public event Action<MarketDepth> MarketDepthChanged
		{
			add => MarketDataProvider.MarketDepthChanged += value;
			remove => MarketDataProvider.MarketDepthChanged -= value;
		}

		/// <inheritdoc />
		public event Action<OrderLogItem> NewOrderLogItem
		{
			add => MarketDataProvider.NewOrderLogItem += value;
			remove => MarketDataProvider.NewOrderLogItem -= value;
		}

		/// <inheritdoc />
		public event Action<News> NewNews
		{
			add => MarketDataProvider.NewNews += value;
			remove => MarketDataProvider.NewNews -= value;
		}

		/// <inheritdoc />
		public event Action<News> NewsChanged
		{
			add => MarketDataProvider.NewsChanged += value;
			remove => MarketDataProvider.NewsChanged -= value;
		}

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

		/// <inheritdoc />
		public event Action<Security, MarketDataMessage> MarketDataSubscriptionSucceeded
		{
			add => MarketDataProvider.MarketDataSubscriptionSucceeded += value;
			remove => MarketDataProvider.MarketDataSubscriptionSucceeded -= value;
		}

		/// <inheritdoc />
		public event Action<Security, MarketDataMessage, Exception> MarketDataSubscriptionFailed
		{
			add => MarketDataProvider.MarketDataSubscriptionFailed += value;
			remove => MarketDataProvider.MarketDataSubscriptionFailed -= value;
		}

		/// <inheritdoc />
		public event Action<Security, MarketDataMessage, MarketDataMessage> MarketDataSubscriptionFailed2
		{
			add => MarketDataProvider.MarketDataSubscriptionFailed2 += value;
			remove => MarketDataProvider.MarketDataSubscriptionFailed2 -= value;
		}

		/// <inheritdoc />
		public event Action<Security, MarketDataMessage> MarketDataUnSubscriptionSucceeded
		{
			add => MarketDataProvider.MarketDataUnSubscriptionSucceeded += value;
			remove => MarketDataProvider.MarketDataUnSubscriptionSucceeded -= value;
		}

		/// <inheritdoc />
		public event Action<Security, MarketDataMessage, Exception> MarketDataUnSubscriptionFailed
		{
			add => MarketDataProvider.MarketDataUnSubscriptionFailed += value;
			remove => MarketDataProvider.MarketDataUnSubscriptionFailed -= value;
		}

		/// <inheritdoc />
		public event Action<Security, MarketDataMessage, MarketDataMessage> MarketDataUnSubscriptionFailed2
		{
			add => MarketDataProvider.MarketDataUnSubscriptionFailed2 += value;
			remove => MarketDataProvider.MarketDataUnSubscriptionFailed2 -= value;
		}

		/// <inheritdoc />
		public event Action<Security, MarketDataFinishedMessage> MarketDataSubscriptionFinished
		{
			add => MarketDataProvider.MarketDataSubscriptionFinished += value;
			remove => MarketDataProvider.MarketDataSubscriptionFinished -= value;
		}

		/// <inheritdoc />
		public event Action<Security, MarketDataMessage, Exception> MarketDataUnexpectedCancelled
		{
			add => MarketDataProvider.MarketDataUnexpectedCancelled += value;
			remove => MarketDataProvider.MarketDataUnexpectedCancelled -= value;
		}

		/// <inheritdoc />
		public event Action<Security, MarketDataMessage> MarketDataSubscriptionOnline
		{
			add => MarketDataProvider.MarketDataSubscriptionOnline += value;
			remove => MarketDataProvider.MarketDataSubscriptionOnline -= value;
		}

		/// <inheritdoc />
		public void LookupSecurities(SecurityLookupMessage criteria)
		{
			MarketDataProvider.LookupSecurities(criteria);
		}

		/// <inheritdoc />
		public void LookupBoards(BoardLookupMessage criteria)
		{
			MarketDataProvider.LookupBoards(criteria);
		}

		/// <inheritdoc />
		public void LookupTimeFrames(TimeFrameLookupMessage criteria)
		{
			MarketDataProvider.LookupTimeFrames(criteria);
		}

		/// <inheritdoc />
		public MarketDepth GetFilteredMarketDepth(Security security)
		{
			return MarketDataProvider.GetFilteredMarketDepth(security);
		}

		/// <inheritdoc />
		public long SubscribeMarketData(Security security, MarketDataMessage message)
		{
			return MarketDataProvider.SubscribeMarketData(security, message);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketData(Security security, MarketDataMessage message)
		{
			MarketDataProvider.UnSubscribeMarketData(security, message);
		}

		/// <inheritdoc />
		public long SubscribeMarketData(MarketDataMessage message)
		{
			return MarketDataProvider.SubscribeMarketData(message);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketData(MarketDataMessage message)
		{
			MarketDataProvider.UnSubscribeMarketData(message);
		}

		/// <inheritdoc />
		public long RegisterFilteredMarketDepth(Security security)
		{
			return MarketDataProvider.RegisterFilteredMarketDepth(security);
		}

		/// <inheritdoc />
		public void UnRegisterFilteredMarketDepth(Security security)
		{
			MarketDataProvider.UnRegisterFilteredMarketDepth(security);
		}

		/// <inheritdoc />
		public long SubscribeMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, int? maxDepth = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeMarketDepth(security, from, to, count, buildMode, buildFrom, maxDepth, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketDepth(Security security)
		{
			MarketDataProvider.UnSubscribeMarketDepth(security);
		}

		/// <inheritdoc />
		public long SubscribeTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeTrades(security, from, to, count, buildMode, buildFrom, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeTrades(Security security)
		{
			MarketDataProvider.UnSubscribeTrades(security);
		}

		/// <inheritdoc />
		public long SubscribeLevel1(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, MarketDataTypes? buildFrom = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeLevel1(security, from, to, count, buildMode, buildFrom, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeLevel1(Security security)
		{
			MarketDataProvider.UnSubscribeLevel1(security);
		}

		/// <inheritdoc />
		public long SubscribeOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeOrderLog(security, from, to, count, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeOrderLog(Security security)
		{
			MarketDataProvider.UnSubscribeOrderLog(security);
		}

		/// <inheritdoc />
		public long SubscribeNews(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeNews(security, from, to, count, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeNews(Security security = null)
		{
			MarketDataProvider.UnSubscribeNews(security);
		}

		/// <inheritdoc />
		public long SubscribeBoard(ExchangeBoard board, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeBoard(board, from, to, count, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeBoard(ExchangeBoard board)
		{
			MarketDataProvider.UnSubscribeBoard(board);
		}

		/// <inheritdoc />
		public void UnSubscribe(long subscriptionId)
		{
			MarketDataProvider.UnSubscribe(subscriptionId);
		}
	}
}