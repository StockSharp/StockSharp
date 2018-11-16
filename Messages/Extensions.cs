#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: Extensions.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.Linq;
	using System.ServiceModel;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Localization;
	using Ecng.Net;

	using MoreLinq;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioChangeMessage"/>.
		/// </summary>
		/// <param name="adapter">Trading system adapter.</param>
		/// <param name="pfName">Portfolio name.</param>
		/// <returns>Portfolio change message.</returns>
		public static PortfolioChangeMessage CreatePortfolioChangeMessage(this IMessageAdapter adapter, string pfName)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			var time = adapter.CurrentTime;

			return new PortfolioChangeMessage
			{
				PortfolioName = pfName,
				LocalTime = time,
				ServerTime = time,
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <param name="adapter">Trading system adapter.</param>
		/// <param name="pfName">Portfolio name.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="depoName">The depositary where the physical security.</param>
		/// <returns>Position change message.</returns>
		public static PositionChangeMessage CreatePositionChangeMessage(this IMessageAdapter adapter, string pfName, SecurityId securityId, string depoName = null)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			var time = adapter.CurrentTime;

			return new PositionChangeMessage
			{
				PortfolioName = pfName,
				SecurityId = securityId,
				LocalTime = time,
				ServerTime = time,
				DepoName = depoName
			};
		}

		/// <summary>
		/// Get best bid.
		/// </summary>
		/// <param name="message">Market depth.</param>
		/// <returns>Best bid, or <see langword="null" />, if no bids are empty.</returns>
		public static QuoteChange GetBestBid(this QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return (message.IsSorted ? message.Bids : message.Bids.OrderByDescending(q => q.Price)).FirstOrDefault();
		}

		/// <summary>
		/// Get best ask.
		/// </summary>
		/// <param name="message">Market depth.</param>
		/// <returns>Best ask, or <see langword="null" />, if no asks are empty.</returns>
		public static QuoteChange GetBestAsk(this QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return (message.IsSorted ? message.Asks : message.Asks.OrderBy(q => q.Price)).FirstOrDefault();
		}

		/// <summary>
		/// Get middle of spread.
		/// </summary>
		/// <param name="message">Market depth.</param>
		/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
		public static decimal? GetSpreadMiddle(this QuoteChangeMessage message)
		{
			var bestBid = message.GetBestBid();
			var bestAsk = message.GetBestAsk();

			return (bestBid?.Price).GetSpreadMiddle(bestAsk?.Price);
		}

		/// <summary>
		/// Get middle of spread.
		/// </summary>
		/// <param name="message">Market depth.</param>
		/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
		public static decimal? GetSpreadMiddle(this Level1ChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var bestBid = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidPrice);
			var bestAsk = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskPrice);

			return bestBid.GetSpreadMiddle(bestAsk);
		}

		/// <summary>
		/// Get middle of spread.
		/// </summary>
		/// <param name="bestBidPrice">Best bid price.</param>
		/// <param name="bestAskPrice">Best ask price.</param>
		/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
		public static decimal? GetSpreadMiddle(this decimal? bestBidPrice, decimal? bestAskPrice)
		{
			if (bestBidPrice == null && bestAskPrice == null)
				return null;

			if (bestBidPrice != null && bestAskPrice != null)
				return (bestAskPrice + bestBidPrice).Value / 2;

			return bestAskPrice ?? bestBidPrice.Value;
		}

		/// <summary>
		/// Get last tick trade price.
		/// </summary>
		/// <param name="message">Market depth.</param>
		/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
		public static decimal? GetLastTradePrice(this Level1ChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return (decimal?)message.Changes.TryGetValue(Level1Fields.LastTradePrice);
		}

		/// <summary>
		/// Cast <see cref="OrderMessage"/> to the <see cref="ExecutionMessage"/>.
		/// </summary>
		/// <param name="message"><see cref="OrderMessage"/>.</param>
		/// <returns><see cref="ExecutionMessage"/>.</returns>
		public static ExecutionMessage CreateReply(this OrderMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return new ExecutionMessage
			{
				OriginalTransactionId = message.TransactionId,
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
			};
		}

		///// <summary>
		///// Cast <see cref="OrderGroupCancelMessage"/> to the <see cref="ExecutionMessage"/>.
		///// </summary>
		///// <param name="message"><see cref="OrderGroupCancelMessage"/>.</param>
		///// <returns><see cref="ExecutionMessage"/>.</returns>
		//public static ExecutionMessage ToExecutionMessage(this OrderGroupCancelMessage message)
		//{
		//	return new ExecutionMessage
		//	{
		//		OriginalTransactionId = message.TransactionId,
		//		ExecutionType = ExecutionTypes.Transaction,
		//	};
		//}

		///// <summary>
		///// Cast <see cref="OrderPairReplaceMessage"/> to the <see cref="ExecutionMessage"/>.
		///// </summary>
		///// <param name="message"><see cref="OrderPairReplaceMessage"/>.</param>
		///// <returns><see cref="ExecutionMessage"/>.</returns>
		//public static ExecutionMessage ToExecutionMessage(this OrderPairReplaceMessage message)
		//{
		//	throw new NotImplementedException();
		//	//return new ExecutionMessage
		//	//{
		//	//	LocalTime = message.LocalTime,
		//	//	OriginalTransactionId = message.TransactionId,
		//	//	Action = ExecutionActions.Canceled,
		//	//};
		//}

		///// <summary>
		///// Cast <see cref="OrderCancelMessage"/> to the <see cref="ExecutionMessage"/>.
		///// </summary>
		///// <param name="message"><see cref="OrderCancelMessage"/>.</param>
		///// <returns><see cref="ExecutionMessage"/>.</returns>
		//public static ExecutionMessage ToExecutionMessage(this OrderCancelMessage message)
		//{
		//	return new ExecutionMessage
		//	{
		//		SecurityId = message.SecurityId,
		//		OriginalTransactionId = message.TransactionId,
		//		//OriginalTransactionId = message.OriginalTransactionId,
		//		OrderId = message.OrderId,
		//		OrderType = message.OrderType,
		//		PortfolioName = message.PortfolioName,
		//		ExecutionType = ExecutionTypes.Transaction,
		//		UserOrderId = message.UserOrderId,
		//		HasOrderInfo = true,
		//	};
		//}

		///// <summary>
		///// Cast <see cref="OrderReplaceMessage"/> to the <see cref="ExecutionMessage"/>.
		///// </summary>
		///// <param name="message"><see cref="OrderReplaceMessage"/>.</param>
		///// <returns><see cref="ExecutionMessage"/>.</returns>
		//public static ExecutionMessage ToExecutionMessage(this OrderReplaceMessage message)
		//{
		//	return new ExecutionMessage
		//	{
		//		SecurityId = message.SecurityId,
		//		OriginalTransactionId = message.TransactionId,
		//		OrderType = message.OrderType,
		//		OrderPrice = message.Price,
		//		OrderVolume = message.Volume,
		//		Side = message.Side,
		//		PortfolioName = message.PortfolioName,
		//		ExecutionType = ExecutionTypes.Transaction,
		//		Condition = message.Condition,
		//		UserOrderId = message.UserOrderId,
		//		HasOrderInfo = true,
		//	};
		//}

		///// <summary>
		///// Cast <see cref="OrderRegisterMessage"/> to the <see cref="ExecutionMessage"/>.
		///// </summary>
		///// <param name="message"><see cref="OrderRegisterMessage"/>.</param>
		///// <returns><see cref="ExecutionMessage"/>.</returns>
		//public static ExecutionMessage ToExecutionMessage(this OrderRegisterMessage message)
		//{
		//	return new ExecutionMessage
		//	{
		//		SecurityId = message.SecurityId,
		//		OriginalTransactionId = message.TransactionId,
		//		OrderType = message.OrderType,
		//		OrderPrice = message.Price,
		//		OrderVolume = message.Volume,
		//		Balance = message.Volume,
		//		Side = message.Side,
		//		PortfolioName = message.PortfolioName,
		//		ExecutionType = ExecutionTypes.Transaction,
		//		Condition = message.Condition,
		//		UserOrderId = message.UserOrderId,
		//		HasOrderInfo = true,
		//	};
		//}

		/// <summary>
		/// Copy extended info.
		/// </summary>
		/// <param name="from">The object of which is copied to extended information.</param>
		/// <param name="to">The object, which is copied to extended information.</param>
		public static void CopyExtensionInfo(this IExtendableEntity from, IExtendableEntity to)
		{
			if (from == null)
				throw new ArgumentNullException(nameof(from));

			if (to == null)
				throw new ArgumentNullException(nameof(to));

			if (from.ExtensionInfo == null)
				return;

			if (to.ExtensionInfo == null)
				to.ExtensionInfo = new Dictionary<string, object>();

			foreach (var pair in from.ExtensionInfo)
			{
				to.ExtensionInfo[pair.Key] = pair.Value;
			}
		}

		/// <summary>
		/// Get message server time.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Server time.</returns>
		public static DateTimeOffset GetServerTime(this Message message)
		{
			return message.TryGetServerTime().Value;
		}

		/// <summary>
		/// Get message server time.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Server time. If the value is <see langword="null" />, the message does not contain the server time.</returns>
		public static DateTimeOffset? TryGetServerTime(this Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Execution:
					return ((ExecutionMessage)message).ServerTime;
				case MessageTypes.QuoteChange:
					return ((QuoteChangeMessage)message).ServerTime;
				case MessageTypes.Level1Change:
					return ((Level1ChangeMessage)message).ServerTime;
				case MessageTypes.PositionChange:
					return ((PositionChangeMessage)message).ServerTime;
				case MessageTypes.PortfolioChange:
					return ((PortfolioChangeMessage)message).ServerTime;
				case MessageTypes.Time:
					return ((TimeMessage)message).ServerTime;
				case MessageTypes.Connect:
					return ((ConnectMessage)message).LocalTime;
				default:
				{
					var candleMsg = message as CandleMessage;
					return candleMsg?.OpenTime;
				}
			}
		}

		/// <summary>
		/// Fill the <see cref="IMessageAdapter.SupportedMessages"/> message types related to transactional.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void AddTransactionalSupport(this IMessageAdapter adapter)
		{
			adapter.AddSupportedMessage(MessageTypes.OrderCancel);
			adapter.AddSupportedMessage(MessageTypes.OrderGroupCancel);
			adapter.AddSupportedMessage(MessageTypes.OrderPairReplace);
			adapter.AddSupportedMessage(MessageTypes.OrderRegister);
			adapter.AddSupportedMessage(MessageTypes.OrderReplace);
			adapter.AddSupportedMessage(MessageTypes.OrderStatus);
			adapter.AddSupportedMessage(MessageTypes.Portfolio);
			adapter.AddSupportedMessage(MessageTypes.PortfolioLookup);
			//adapter.AddSupportedMessage(MessageTypes.Position);
		}

		/// <summary>
		/// Remove from <see cref="IMessageAdapter.SupportedMessages"/> message types related to transactional.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveTransactionalSupport(this IMessageAdapter adapter)
		{
			adapter.RemoveSupportedMessage(MessageTypes.OrderCancel);
			adapter.RemoveSupportedMessage(MessageTypes.OrderGroupCancel);
			adapter.RemoveSupportedMessage(MessageTypes.OrderPairReplace);
			adapter.RemoveSupportedMessage(MessageTypes.OrderRegister);
			adapter.RemoveSupportedMessage(MessageTypes.OrderReplace);
			adapter.RemoveSupportedMessage(MessageTypes.OrderStatus);
			adapter.RemoveSupportedMessage(MessageTypes.Portfolio);
			adapter.RemoveSupportedMessage(MessageTypes.PortfolioLookup);
			//adapter.RemoveSupportedMessage(MessageTypes.Position);
		}

		/// <summary>
		/// Fill the <see cref="IMessageAdapter.SupportedMessages"/> message types related to market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void AddMarketDataSupport(this IMessageAdapter adapter)
		{
			adapter.AddSupportedMessage(MessageTypes.MarketData);
			adapter.AddSupportedMessage(MessageTypes.SecurityLookup);

			//adapter.AddSupportedAllMarketDataTypes();
		}

		/// <summary>
		/// Remove from <see cref="IMessageAdapter.SupportedMessages"/> message types related to market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveMarketDataSupport(this IMessageAdapter adapter)
		{
			adapter.RemoveSupportedMessage(MessageTypes.MarketData);
			adapter.RemoveSupportedMessage(MessageTypes.SecurityLookup);
			adapter.RemoveSupportedMessage(MessageTypes.TimeFrameLookup);

			adapter.RemoveSupportedAllMarketDataTypes();
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void AddSupportedMessage(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedMessages = adapter.SupportedMessages.Concat(type).Distinct().ToArray();
		}

		/// <summary>
		/// Remove the message type from <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void RemoveSupportedMessage(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedMessages = adapter.SupportedMessages.Except(new[] { type }).ToArray();
		}

		/// <summary>
		/// Determines whether the specified message type is supported by the adapter.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <returns><see langword="true"/> if the specified message type is supported, otherwise, <see langword="false"/>.</returns>
		public static bool IsMessageSupported(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.SupportedMessages.Contains(type);
		}

		/// <summary>
		/// Add market data type into <see cref="IMessageAdapter.SupportedMarketDataTypes"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Market data type.</param>
		public static void AddSupportedMarketDataType(this IMessageAdapter adapter, MarketDataTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedMarketDataTypes = adapter.SupportedMarketDataTypes.Concat(type).ToArray();
		}

		/// <summary>
		/// Remove market data type from <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Market data type.</param>
		public static void RemoveSupportedMarketDataType(this IMessageAdapter adapter, MarketDataTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedMarketDataTypes = adapter.SupportedMarketDataTypes.Except(new[] { type }).ToArray();
		}

		/// <summary>
		/// Determines whether the specified market-data type is supported by the adapter.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <returns><see langword="true"/> if the specified message type is supported, otherwise, <see langword="false"/>.</returns>
		public static bool IsMarketDataTypeSupported(this IMessageAdapter adapter, MarketDataTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.SupportedMarketDataTypes.Contains(type);
		}

		/// <summary>
		/// Add all market data types into <see cref="IMessageAdapter.SupportedMarketDataTypes"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void AddSupportedAllMarketDataTypes(this IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			//adapter.AddSupportedMarketDataType(MarketDataTypes.OrderLog);
			adapter.AddSupportedMarketDataType(MarketDataTypes.Trades);
			adapter.AddSupportedMarketDataType(MarketDataTypes.MarketDepth);
			adapter.AddSupportedMarketDataType(MarketDataTypes.Level1);
			adapter.AddSupportedMarketDataType(MarketDataTypes.CandleTimeFrame);
			//adapter.AddSupportedMarketDataType(MarketDataTypes.CandleTick);
			//adapter.AddSupportedMarketDataType(MarketDataTypes.CandleVolume);
			//adapter.AddSupportedMarketDataType(MarketDataTypes.CandleRange);
			//adapter.AddSupportedMarketDataType(MarketDataTypes.CandlePnF);
			//adapter.AddSupportedMarketDataType(MarketDataTypes.CandleRenko);
		}

		/// <summary>
		/// Remove all market data types from <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveSupportedAllMarketDataTypes(this IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedMarketDataTypes = ArrayHelper.Empty<MarketDataTypes>();
		}

		/// <summary>
		/// Determines whether the specified message type is derived from <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="messageType">The message type.</param>
		/// <returns><see langword="true"/> if the specified message type is derived from <see cref="CandleMessage"/>, otherwise, <see langword="false"/>.</returns>
		public static bool IsCandleMessage(this Type messageType)
		{
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			return messageType.IsSubclassOf(typeof(CandleMessage));
		}

		/// <summary>
		/// Determines whether the specified message contains order information.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <returns><see langword="true"/> if the specified message contains order information, otherwise, <see langword="false"/>.</returns>
		public static bool HasOrderInfo(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.ExecutionType == ExecutionTypes.Transaction && message.HasOrderInfo;
		}

		/// <summary>
		/// Determines whether the specified message contains trade information.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <returns><see langword="true"/> if the specified message contains trade information, otherwise, <see langword="false"/>.</returns>
		public static bool HasTradeInfo(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.ExecutionType == ExecutionTypes.Transaction && message.HasTradeInfo;
		}

		/// <summary>
		/// Convert error text message to <see cref="ErrorMessage"/> instance.
		/// </summary>
		/// <param name="description">Error text message.</param>
		/// <returns><see cref="ErrorMessage"/> instance.</returns>
		public static ErrorMessage ToErrorMessage(this string description)
		{
			if (description.IsEmpty())
				throw new ArgumentNullException(nameof(description));

			return new InvalidOperationException(description).ToErrorMessage();
		}

		/// <summary>
		/// Convert error info into <see cref="ErrorMessage"/>.
		/// </summary>
		/// <param name="error">Error info.</param>
		/// <returns>Error message.</returns>
		public static ErrorMessage ToErrorMessage(this Exception error)
		{
			if (error == null)
				throw new ArgumentNullException(nameof(error));

			return new ErrorMessage { Error = error };
		}

		///// <summary>
		///// Get inner <see cref="IMessageAdapter"/>.
		///// </summary>
		///// <param name="adapter"><see cref="IMessageAdapter"/>.</param>
		///// <returns>Inner <see cref="IMessageAdapter"/>.</returns>
		//public static IMessageAdapter GetInnerAdapter(this IMessageAdapter adapter)
		//{
		//	if (adapter == null)
		//		return null;

		//	IMessageAdapterWrapper wrapper;

		//	while ((wrapper = adapter as IMessageAdapterWrapper) != null)
		//		adapter = wrapper.InnerAdapter;

		//	return adapter;
		//}

		private static readonly ChannelFactory<IDailyInfoSoap> _dailyInfoFactory = new ChannelFactory<IDailyInfoSoap>(new BasicHttpBinding(), new EndpointAddress("http://www.cbr.ru/dailyinfowebserv/dailyinfo.asmx"));
		private static readonly Dictionary<DateTime, Dictionary<CurrencyTypes, decimal>> _rateInfo = new Dictionary<DateTime, Dictionary<CurrencyTypes, decimal>>();

		/// <summary>
		/// To convert one currency to another.
		/// </summary>
		/// <param name="currencyFrom">The currency to be converted.</param>
		/// <param name="currencyTypeTo">The code of the target currency.</param>
		/// <returns>Converted currency.</returns>
		public static Currency Convert(this Currency currencyFrom, CurrencyTypes currencyTypeTo)
		{
			if (currencyFrom == null)
				throw new ArgumentNullException(nameof(currencyFrom));

			return new Currency { Type = currencyTypeTo, Value = currencyFrom.Value * currencyFrom.Type.Convert(currencyTypeTo) };
		}

		/// <summary>
		/// To get the conversion rate for converting one currency to another.
		/// </summary>
		/// <param name="from">The code of currency to be converted.</param>
		/// <param name="to">The code of the target currency.</param>
		/// <returns>The rate.</returns>
		public static decimal Convert(this CurrencyTypes from, CurrencyTypes to)
		{
			return from.Convert(to, DateTime.Today);
		}

		/// <summary>
		/// To get the conversion rate for the specified date.
		/// </summary>
		/// <param name="from">The code of currency to be converted.</param>
		/// <param name="to">The code of the target currency.</param>
		/// <param name="date">The rate date.</param>
		/// <returns>The rate.</returns>
		public static decimal Convert(this CurrencyTypes from, CurrencyTypes to, DateTime date)
		{
			if (from == to)
				return 1;

			var info = _rateInfo.SafeAdd(date, key =>
			{
				var i = _dailyInfoFactory.Invoke(c => c.GetCursOnDate(key));
				return i.Tables[0].Rows.Cast<DataRow>().ToDictionary(r => r[4].To<CurrencyTypes>(), r => r[2].To<decimal>());
			});

			if (from != CurrencyTypes.RUB && !info.ContainsKey(from))
				throw new ArgumentException(LocalizedStrings.Str1212Params.Put(from), nameof(from));

			if (to != CurrencyTypes.RUB && !info.ContainsKey(to))
				throw new ArgumentException(LocalizedStrings.Str1212Params.Put(to), nameof(to));

			if (from == CurrencyTypes.RUB)
				return 1 / info[to];
			else if (to == CurrencyTypes.RUB)
				return info[from];
			else
				return info[from] / info[to];
		}

		/// <summary>
		/// Is the specified <see cref="PositionChangeTypes"/> was marked by <see cref="ObsoleteAttribute"/>.
		/// </summary>
		/// <param name="type"><see cref="PositionChangeTypes"/> value.</param>
		/// <returns><see langword="true" />, if obsolete, otherwise, not obsolete.</returns>
		public static bool IsObsolete(this PositionChangeTypes type) => type.GetAttributeOfType<ObsoleteAttribute>() != null;

		/// <summary>
		/// Is the specified <see cref="Level1Fields"/> was obsolete.
		/// </summary>
		/// <param name="field"><see cref="Level1Fields"/> value.</param>
		/// <returns><see langword="true" />, if obsolete, otherwise, not obsolete.</returns>
		public static bool IsObsolete(this Level1Fields field)
		{
			switch (field)
			{
				case Level1Fields.LastTrade:
				case Level1Fields.BestBid:
				case Level1Fields.BestAsk:
				case Level1Fields.ExtensionInfo:
					return true;
			}

			return false;
		}

		/// <summary>
		/// Try to initialize <see cref="Message.LocalTime"/> by <see cref="ILogSource.CurrentTime"/>.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="source">Source.</param>
		public static void TryInitLocalTime(this Message message, ILogSource source)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (message.LocalTime.IsDefault())
				message.LocalTime = source.CurrentTime;
		}

		/// <summary>
		/// Is specified message id real-time subscription.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns><see langword="true" />, if real-time, otherwise, <see langword="false"/>.</returns>
		public static bool IsRealTimeSubscription(this MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return /*message.From == null && */message.To == null;
		}

		/// <summary>
		/// Validate <see cref="MarketDataMessage.From"/> and <see cref="MarketDataMessage.To"/> values.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Message.</returns>
		public static MarketDataMessage ValidateBounds(this MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.From != null && message.To != null)
			{
				if (message.From.Value > message.To.Value)
					throw new ArgumentOutOfRangeException(nameof(message), message.To, LocalizedStrings.Str1014.Put(message.From));
			}

			return message;
		}

		/// <summary>
		/// Is the specified <see cref="Level1Fields"/> is related to last trade.
		/// </summary>
		/// <param name="field">Field.</param>
		/// <returns>Check result.</returns>
		public static bool IsLastTradeField(this Level1Fields field) =>
			field == Level1Fields.LastTradeId || field == Level1Fields.LastTradeTime ||
			field == Level1Fields.LastTradeOrigin || field == Level1Fields.LastTradePrice ||
			field == Level1Fields.LastTradeUpDown || field == Level1Fields.LastTradeVolume;

		/// <summary>
		/// Is the specified <see cref="Level1Fields"/> is related to best bid.
		/// </summary>
		/// <param name="field">Field.</param>
		/// <returns>Check result.</returns>
		public static bool IsBestBidField(this Level1Fields field) =>
			field == Level1Fields.BestBidPrice || field == Level1Fields.BestBidTime || field == Level1Fields.BestBidVolume;
		
		/// <summary>
		/// Is the specified <see cref="Level1Fields"/> is related to best ask.
		/// </summary>
		/// <param name="field">Field.</param>
		/// <returns>Check result.</returns>
		public static bool IsBestAskField(this Level1Fields field) =>
			field == Level1Fields.BestAskPrice || field == Level1Fields.BestAskTime || field == Level1Fields.BestAskVolume;

		/// <summary>
		/// Fill default <see cref="SecurityTypes.CryptoCurrency"/> price and volume step by 0.00000001 value.
		/// </summary>
		/// <param name="secId">Security ID.</param>
		/// <returns>A message containing info about the security.</returns>
		public static SecurityMessage FillDefaultCryptoFields(this SecurityId secId)
		{
			var message = new SecurityMessage
			{
				SecurityId = secId,
			}.FillDefaultCryptoFields();

			return message;
		}

		/// <summary>
		/// Fill default <see cref="SecurityTypes.CryptoCurrency"/> price and volume step by 0.00000001 value.
		/// </summary>
		/// <param name="message">A message containing info about the security.</param>
		/// <returns>A message containing info about the security.</returns>
		public static SecurityMessage FillDefaultCryptoFields(this SecurityMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.PriceStep = message.VolumeStep = 0.00000001m;
			message.SecurityType = SecurityTypes.CryptoCurrency;

			return message;
		}

		/// <summary>
		/// Get preferred language.
		/// </summary>
		/// <param name="categories">Message adapter categories.</param>
		/// <returns>Language</returns>
		public static Languages GetPreferredLanguage(this MessageAdapterCategories? categories)
		{
			return categories?.Contains(MessageAdapterCategories.Russia) == true ? Languages.Russian : Languages.English;
		}

		/// <summary>
		/// To check, does the string contain the order registration.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contains the order registration, otherwise, <see langword="false" />.</returns>
		public static bool IsOrderLogRegistered(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			return item.OrderState == OrderStates.Active && item.TradePrice == null;
		}

		/// <summary>
		/// To check, does the string contain the cancelled order.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contain the cancelled order, otherwise, <see langword="false" />.</returns>
		public static bool IsOrderLogCanceled(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			return item.OrderState == OrderStates.Done && item.TradeVolume == null;
		}

		/// <summary>
		/// To check, does the string contain the order matching.
		/// </summary>
		/// <param name="item">Order log item.</param>
		/// <returns><see langword="true" />, if the string contains order matching, otherwise, <see langword="false" />.</returns>
		public static bool IsOrderLogMatched(this ExecutionMessage item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			return item.TradeVolume != null;
		}

		/// <summary>
		/// Get period for schedule.
		/// </summary>
		/// <param name="time">Trading schedule.</param>
		/// <param name="date">The date in time for search of appropriate period.</param>
		/// <returns>The schedule period. If no period is appropriate, <see langword="null" /> is returned.</returns>
		public static WorkingTimePeriod GetPeriod(this WorkingTime time, DateTime date)
		{
			if (time == null)
				throw new ArgumentNullException(nameof(time));

			return time.Periods.FirstOrDefault(p => p.Till >= date);
		}

		private const string _dateFormat = "yyyyMMdd";
		private const string _timeFormat = "hh\\:mm";

		/// <summary>
		/// Encode <see cref="WorkingTime.Periods"/> to string.
		/// </summary>
		/// <param name="periods">Schedule validity periods.</param>
		/// <returns>Encoded string.</returns>
		public static string EncodeToString(this IEnumerable<WorkingTimePeriod> periods)
		{
			return periods.Select(p => $"{p.Till:yyyyMMdd}=" + p.Times.Select(r => $"{r.Min:hh\\:mm}-{r.Max:hh\\:mm}").Join("--") + "=" + p.SpecialDays.Select(p2 => $"{p2.Key}:" + p2.Value.Select(r => $"{r.Min:hh\\:mm}-{r.Max:hh\\:mm}").Join("--")).Join("//")).Join(",");
		}

		/// <summary>
		/// Decode from string to <see cref="WorkingTime.Periods"/>.
		/// </summary>
		/// <param name="input">Encoded string.</param>
		/// <returns>Schedule validity periods.</returns>
		public static IEnumerable<WorkingTimePeriod> DecodeToPeriods(this string input)
		{
			var periods = new List<WorkingTimePeriod>();

			if (input.IsEmpty())
				return periods;

			foreach (var str in input.Split(","))
			{
				var parts = str.Split('=');
				periods.Add(new WorkingTimePeriod
				{
					Till = parts[0].ToDateTime(_dateFormat),
					Times = parts[1].Split("--").Select(s =>
					{
						var parts2 = s.Split('-');
						return new Range<TimeSpan>(parts2[0].ToTimeSpan(_timeFormat), parts2[1].ToTimeSpan(_timeFormat));
					}).ToList(),
					SpecialDays = parts[2].Split("//").Select(s =>
					{
						var parts2 = s.Split(':');
						return new KeyValuePair<DayOfWeek, Range<TimeSpan>[]>(parts2[0].To<DayOfWeek>(), parts2[1].Split("--").Select(s2 =>
						{
							var parts3 = s2.Split('-');
							return new Range<TimeSpan>(parts3[0].ToTimeSpan(_timeFormat), parts3[1].ToTimeSpan(_timeFormat));
						}).ToArray());
					}).ToDictionary()
				});
			}

			return periods;
		}

		/// <summary>
		/// Encode <see cref="WorkingTime.SpecialDays"/> to string.
		/// </summary>
		/// <param name="specialDays">Special working days and holidays.</param>
		/// <returns>Encoded string.</returns>
		public static string EncodeToString(this IDictionary<DateTime, Range<TimeSpan>[]> specialDays)
		{
			return specialDays.Select(p => $"{p.Key:yyyyMMdd}=" + p.Value.Select(r => $"{r.Min:hh\\:mm}-{r.Max:hh\\:mm}").Join("--")).Join(",");
		}

		/// <summary>
		/// Decode from string to <see cref="WorkingTime.SpecialDays"/>.
		/// </summary>
		/// <param name="input">Encoded string.</param>
		/// <returns>Special working days and holidays.</returns>
		public static IDictionary<DateTime, Range<TimeSpan>[]> DecodeToSpecialDays(this string input)
		{
			var specialDays = new Dictionary<DateTime, Range<TimeSpan>[]>();

			if (input.IsEmpty())
				return specialDays;

			foreach (var str in input.Split(","))
			{
				var parts = str.Split('=');
				specialDays[parts[0].ToDateTime(_dateFormat)] = parts[1].Split("--").Select(s =>
				{
					var parts2 = s.Split('-');
					return new Range<TimeSpan>(parts2[0].ToTimeSpan(_timeFormat), parts2[1].ToTimeSpan(_timeFormat));
				}).ToArray();
			}

			return specialDays;
		}
	}
}