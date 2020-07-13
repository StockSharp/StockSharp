#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BusinessEntities.BusinessEntities
File: IMarketDataProvider.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	/// <summary>
	/// The market data by the instrument provider interface.
	/// </summary>
	public interface IMarketDataProvider
	{
		/// <summary>
		/// Security changed.
		/// </summary>
		event Action<Security, IEnumerable<KeyValuePair<Level1Fields, object>>, DateTimeOffset, DateTimeOffset> ValuesChanged;

		/// <summary>
		/// To get the quotes order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Order book.</returns>
		[Obsolete("Use MarketDepthReceived event.")]
		MarketDepth GetMarketDepth(Security security);

		/// <summary>
		/// To get the value of market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="field">Market-data field.</param>
		/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
		object GetSecurityValue(Security security, Level1Fields field);

		/// <summary>
		/// To get a set of available fields <see cref="Level1Fields"/>, for which there is a market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Possible fields.</returns>
		IEnumerable<Level1Fields> GetLevel1Fields(Security security);

		/// <summary>
		/// Tick trade received.
		/// </summary>
		[Obsolete("Use TickTradeReceived event.")]
		event Action<Trade> NewTrade;

		/// <summary>
		/// Security received.
		/// </summary>
		[Obsolete("Use SecurityReceived event.")]
		event Action<Security> NewSecurity;

		/// <summary>
		/// Security changed.
		/// </summary>
		[Obsolete("Use SecurityReceived event.")]
		event Action<Security> SecurityChanged;

		/// <summary>
		/// Order book received.
		/// </summary>
		[Obsolete("Use OrderBookReceived event.")]
		event Action<MarketDepth> NewMarketDepth;

		/// <summary>
		/// Order book changed.
		/// </summary>
		[Obsolete("Use OrderBookReceived event.")]
		event Action<MarketDepth> MarketDepthChanged;

		/// <summary>
		/// Order book changed.
		/// </summary>
		[Obsolete("Use MarketDepthReceived event.")]
		event Action<MarketDepth> FilteredMarketDepthChanged;

		/// <summary>
		/// Order log received.
		/// </summary>
		[Obsolete("Use OrderLogItemReceived event.")]
		event Action<OrderLogItem> NewOrderLogItem;

		/// <summary>
		/// News received.
		/// </summary>
		[Obsolete("Use NewsReceived event.")]
		event Action<News> NewNews;

		/// <summary>
		/// News updated (news body received <see cref="News.Story"/>).
		/// </summary>
		[Obsolete("Use NewsReceived event.")]
		event Action<News> NewsChanged;

		/// <summary>
		/// Lookup result <see cref="SecurityLookupMessage"/> received.
		/// </summary>
		event Action<SecurityLookupMessage, IEnumerable<Security>, Exception> LookupSecuritiesResult;

		/// <summary>
		/// Lookup result <see cref="SecurityLookupMessage"/> received.
		/// </summary>
		event Action<SecurityLookupMessage, IEnumerable<Security>, IEnumerable<Security>, Exception> LookupSecuritiesResult2;

		/// <summary>
		/// Lookup result <see cref="BoardLookupMessage"/> received.
		/// </summary>
		event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult;

		/// <summary>
		/// Lookup result <see cref="BoardLookupMessage"/> received.
		/// </summary>
		event Action<BoardLookupMessage, IEnumerable<ExchangeBoard>, IEnumerable<ExchangeBoard>, Exception> LookupBoardsResult2;

		/// <summary>
		/// Lookup result <see cref="TimeFrameLookupMessage"/> received.
		/// </summary>
		event Action<TimeFrameLookupMessage, IEnumerable<TimeSpan>, Exception> LookupTimeFramesResult;

		/// <summary>
		/// Lookup result <see cref="TimeFrameLookupMessage"/> received.
		/// </summary>
		event Action<TimeFrameLookupMessage, IEnumerable<TimeSpan>, IEnumerable<TimeSpan>, Exception> LookupTimeFramesResult2;

		/// <summary>
		/// Successful subscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage> MarketDataSubscriptionSucceeded;

		/// <summary>
		/// Error subscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, Exception> MarketDataSubscriptionFailed;

		/// <summary>
		/// Error subscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, SubscriptionResponseMessage> MarketDataSubscriptionFailed2;

		/// <summary>
		/// Successful unsubscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage> MarketDataUnSubscriptionSucceeded;

		/// <summary>
		/// Error unsubscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, Exception> MarketDataUnSubscriptionFailed;

		/// <summary>
		/// Error unsubscription market-data.
		/// </summary>
		event Action<Security, MarketDataMessage, SubscriptionResponseMessage> MarketDataUnSubscriptionFailed2;

		/// <summary>
		/// Subscription market-data finished.
		/// </summary>
		event Action<Security, SubscriptionFinishedMessage> MarketDataSubscriptionFinished;

		/// <summary>
		/// Market-data subscription unexpected cancelled.
		/// </summary>
		event Action<Security, MarketDataMessage, Exception> MarketDataUnexpectedCancelled;

		/// <summary>
		/// Subscription is online.
		/// </summary>
		event Action<Security, MarketDataMessage> MarketDataSubscriptionOnline;

		/// <summary>
		/// Get filtered order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Filtered order book.</returns>
		[Obsolete("Use MarketDepthReceived event.")]
		MarketDepth GetFilteredMarketDepth(Security security);
	}
}