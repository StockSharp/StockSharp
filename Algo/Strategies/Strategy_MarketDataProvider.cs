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
		[Obsolete("Use SubscriptionStarted event.")]
		public event Action<Security, MarketDataMessage> MarketDataSubscriptionSucceeded
		{
			add => MarketDataProvider.MarketDataSubscriptionSucceeded += value;
			remove => MarketDataProvider.MarketDataSubscriptionSucceeded -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SubscriptionFailed event.")]
		public event Action<Security, MarketDataMessage, Exception> MarketDataSubscriptionFailed
		{
			add => MarketDataProvider.MarketDataSubscriptionFailed += value;
			remove => MarketDataProvider.MarketDataSubscriptionFailed -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SubscriptionFailed event.")]
		public event Action<Security, MarketDataMessage, SubscriptionResponseMessage> MarketDataSubscriptionFailed2
		{
			add => MarketDataProvider.MarketDataSubscriptionFailed2 += value;
			remove => MarketDataProvider.MarketDataSubscriptionFailed2 -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SubscriptionStopped event.")]
		public event Action<Security, MarketDataMessage> MarketDataUnSubscriptionSucceeded
		{
			add => MarketDataProvider.MarketDataUnSubscriptionSucceeded += value;
			remove => MarketDataProvider.MarketDataUnSubscriptionSucceeded -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SubscriptionFailed event.")]
		public event Action<Security, MarketDataMessage, Exception> MarketDataUnSubscriptionFailed
		{
			add => MarketDataProvider.MarketDataUnSubscriptionFailed += value;
			remove => MarketDataProvider.MarketDataUnSubscriptionFailed -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SubscriptionFailed event.")]
		public event Action<Security, MarketDataMessage, SubscriptionResponseMessage> MarketDataUnSubscriptionFailed2
		{
			add => MarketDataProvider.MarketDataUnSubscriptionFailed2 += value;
			remove => MarketDataProvider.MarketDataUnSubscriptionFailed2 -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SubscriptionStopped event.")]
		public event Action<Security, SubscriptionFinishedMessage> MarketDataSubscriptionFinished
		{
			add => MarketDataProvider.MarketDataSubscriptionFinished += value;
			remove => MarketDataProvider.MarketDataSubscriptionFinished -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SubscriptionFailed event.")]
		public event Action<Security, MarketDataMessage, Exception> MarketDataUnexpectedCancelled
		{
			add => MarketDataProvider.MarketDataUnexpectedCancelled += value;
			remove => MarketDataProvider.MarketDataUnexpectedCancelled -= value;
		}

		/// <inheritdoc />
		[Obsolete("Use SubscriptionOnline event.")]
		public event Action<Security, MarketDataMessage> MarketDataSubscriptionOnline
		{
			add => MarketDataProvider.MarketDataSubscriptionOnline += value;
			remove => MarketDataProvider.MarketDataSubscriptionOnline -= value;
		}
	}
}