namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	partial class Strategy
	{
		private IMarketDataProviderEx MarketDataProvider => (IMarketDataProviderEx)SafeGetConnector();

		/// <inheritdoc />
		public event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

		/// <inheritdoc />
		[Obsolete("Use MarketDepthReceived event.")]
		public MarketDepth GetMarketDepth(Security security)
			=> MarketDataProvider.GetMarketDepth(security);

		/// <inheritdoc />
		public object GetSecurityValue(Security security, Level1Fields field)
			=> MarketDataProvider.GetSecurityValue(security, field);

		/// <inheritdoc />
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
			=> MarketDataProvider.GetLevel1Fields(security);

		/// <inheritdoc />
		[Obsolete("Use TickTradeReceived event.")]
		public event Action<Trade> NewTrade
		{
			add => MarketDataProvider.NewTrade += value;
			remove => MarketDataProvider.NewTrade -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SecurityReceived event.")]
		public event Action<Security> NewSecurity
		{
			add => MarketDataProvider.NewSecurity += value;
			remove => MarketDataProvider.NewSecurity -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SecurityReceived event.")]
		public event Action<Security> SecurityChanged
		{
			add => MarketDataProvider.SecurityChanged += value;
			remove => MarketDataProvider.SecurityChanged -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use OrderBookReceived event.")]
		public event Action<MarketDepth> NewMarketDepth
		{
			add => MarketDataProvider.NewMarketDepth += value;
			remove => MarketDataProvider.NewMarketDepth -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use OrderBookReceived event.")]
		public event Action<MarketDepth> MarketDepthChanged
		{
			add => MarketDataProvider.MarketDepthChanged += value;
			remove => MarketDataProvider.MarketDepthChanged -= value;
		}

		/// <inheritdoc />
		public event Action<MarketDepth> FilteredMarketDepthChanged
		{
			add => MarketDataProvider.FilteredMarketDepthChanged += value;
			remove => MarketDataProvider.FilteredMarketDepthChanged -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use OrderLogItemReceived event.")]
		public event Action<OrderLogItem> NewOrderLogItem
		{
			add => MarketDataProvider.NewOrderLogItem += value;
			remove => MarketDataProvider.NewOrderLogItem -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use NewsReceived event.")]
		public event Action<News> NewNews
		{
			add => MarketDataProvider.NewNews += value;
			remove => MarketDataProvider.NewNews -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use NewsReceived event.")]
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
		public event Action<Security, MarketDataMessage, SubscriptionResponseMessage> MarketDataSubscriptionFailed2
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
		public event Action<Security, MarketDataMessage, SubscriptionResponseMessage> MarketDataUnSubscriptionFailed2
		{
			add => MarketDataProvider.MarketDataUnSubscriptionFailed2 += value;
			remove => MarketDataProvider.MarketDataUnSubscriptionFailed2 -= value;
		}

		/// <inheritdoc />
		public event Action<Security, SubscriptionFinishedMessage> MarketDataSubscriptionFinished
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
		public Subscription SubscribeMarketData(Security security, MarketDataMessage message)
		{
			return MarketDataProvider.SubscribeMarketData(security, message);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketData(Security security, MarketDataMessage message)
		{
			MarketDataProvider.UnSubscribeMarketData(security, message);
		}

		/// <inheritdoc />
		public Subscription SubscribeMarketData(MarketDataMessage message)
		{
			return MarketDataProvider.SubscribeMarketData(message);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketData(MarketDataMessage message)
		{
			MarketDataProvider.UnSubscribeMarketData(message);
		}

		/// <inheritdoc />
		public Subscription SubscribeFilteredMarketDepth(Security security)
		{
			return MarketDataProvider.SubscribeFilteredMarketDepth(security);
		}

		/// <inheritdoc />
		public Subscription SubscribeMarketDepth(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, int? maxDepth = null, TimeSpan? refreshSpeed = null, IOrderLogMarketDepthBuilder depthBuilder = null, bool passThroughOrderBookInrement = false, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeMarketDepth(security, from, to, count, buildMode, buildFrom, maxDepth, refreshSpeed, depthBuilder, passThroughOrderBookInrement, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeMarketDepth(Security security)
		{
			MarketDataProvider.UnSubscribeMarketDepth(security);
		}

		/// <inheritdoc />
		public Subscription SubscribeTrades(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeTrades(security, from, to, count, buildMode, buildFrom, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeTrades(Security security)
		{
			MarketDataProvider.UnSubscribeTrades(security);
		}

		/// <inheritdoc />
		public Subscription SubscribeLevel1(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, MarketDataBuildModes buildMode = MarketDataBuildModes.LoadAndBuild, DataType buildFrom = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeLevel1(security, from, to, count, buildMode, buildFrom, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeLevel1(Security security)
		{
			MarketDataProvider.UnSubscribeLevel1(security);
		}

		/// <inheritdoc />
		public Subscription SubscribeOrderLog(Security security, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeOrderLog(security, from, to, count, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeOrderLog(Security security)
		{
			MarketDataProvider.UnSubscribeOrderLog(security);
		}

		/// <inheritdoc />
		public Subscription SubscribeNews(Security security = null, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
		{
			return MarketDataProvider.SubscribeNews(security, from, to, count, adapter);
		}

		/// <inheritdoc />
		public void UnSubscribeNews(Security security = null)
		{
			MarketDataProvider.UnSubscribeNews(security);
		}

		/// <inheritdoc />
		public Subscription SubscribeBoard(ExchangeBoard board, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, IMessageAdapter adapter = null)
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