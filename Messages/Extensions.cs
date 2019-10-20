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

			return (message.IsSorted ? (IEnumerable<QuoteChange>)message.Bids : message.Bids.OrderByDescending(q => q.Price)).FirstOrDefault();
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

			return (message.IsSorted ? (IEnumerable<QuoteChange>)message.Asks : message.Asks.OrderBy(q => q.Price)).FirstOrDefault();
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
			return (message as IServerTimeMessage)?.ServerTime;
		}

		/// <summary>
		/// Convert <see cref="MessageTypes"/> to <see cref="MessageTypeInfo"/> value.
		/// </summary>
		/// <param name="type"><see cref="MessageTypes"/> value.</param>
		/// <param name="isMarketData">Market data.</param>
		/// <returns><see cref="MessageTypeInfo"/> value.</returns>
		public static MessageTypeInfo ToInfo(this MessageTypes type, bool? isMarketData = null)
		{
			if (isMarketData == null)
				isMarketData = IsMarketData(type);

			return new MessageTypeInfo(type, isMarketData);
		}

		private static readonly CachedSynchronizedSet<MessageTypes> _transactionalTypes = new CachedSynchronizedSet<MessageTypes>(new[]
		{
			MessageTypes.OrderRegister,
			MessageTypes.OrderCancel,
			MessageTypes.OrderStatus,
			MessageTypes.OrderGroupCancel,
			MessageTypes.OrderReplace,
			MessageTypes.OrderPairReplace,
			MessageTypes.Portfolio,
			MessageTypes.PortfolioLookup
		});

		/// <summary>
		/// Transactional message types.
		/// </summary>
		public static IEnumerable<MessageTypes> TransactionalMessageTypes => _transactionalTypes.Cache;

		/// <summary>
		/// Fill the <see cref="IMessageAdapter.SupportedMessages"/> message types related to transactional.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void AddTransactionalSupport(this IMessageAdapter adapter)
		{
			foreach (var type in TransactionalMessageTypes)
				adapter.AddSupportedMessage(type, false);
		}

		/// <summary>
		/// Remove from <see cref="IMessageAdapter.SupportedMessages"/> message types related to transactional.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveTransactionalSupport(this IMessageAdapter adapter)
		{
			foreach (var type in TransactionalMessageTypes)
				adapter.RemoveSupportedMessage(type);
		}

		private static readonly CachedSynchronizedSet<MessageTypes> _marketDataTypes = new CachedSynchronizedSet<MessageTypes>(new[]
		{
			MessageTypes.MarketData,
			MessageTypes.SecurityLookup,
		});

		/// <summary>
		/// Market-data message types.
		/// </summary>
		public static IEnumerable<MessageTypes> MarketDataMessageTypes => _marketDataTypes.Cache;

		/// <summary>
		/// Fill the <see cref="IMessageAdapter.SupportedMessages"/> message types related to market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void AddMarketDataSupport(this IMessageAdapter adapter)
		{
			foreach (var type in MarketDataMessageTypes)
				adapter.AddSupportedMessage(type, true);
		}

		/// <summary>
		/// Remove from <see cref="IMessageAdapter.SupportedMessages"/> message types related to market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveMarketDataSupport(this IMessageAdapter adapter)
		{
			foreach (var type in MarketDataMessageTypes)
				adapter.RemoveSupportedMessage(type);

			adapter.RemoveSupportedAllMarketDataTypes();
		}

		private static bool? IsMarketData(MessageTypes type)
		{
			if (MarketDataMessageTypes.Contains(type))
				return true;
			else if (TransactionalMessageTypes.Contains(type))
				return false;

			return null;
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		[Obsolete]
		public static void AddSupportedMessage(this IMessageAdapter adapter, MessageTypes type)
		{
			AddSupportedMessage(adapter, type, IsMarketData(type));
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <param name="isMarketData"><paramref name="type"/> is market-data type.</param>
		public static void AddSupportedMessage(this IMessageAdapter adapter, MessageTypes type, bool? isMarketData)
		{
			adapter.AddSupportedMessage(new MessageTypeInfo(type, isMarketData));
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.SupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="info">Extended info for <see cref="MessageTypes"/>.</param>
		public static void AddSupportedMessage(this IMessageAdapter adapter, MessageTypeInfo info)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			if (info == null)
				throw new ArgumentNullException(nameof(info));

			var dict = adapter.PossibleSupportedMessages.ToDictionary(i => i.Type);
			dict.TryAdd(info.Type, info);

			adapter.PossibleSupportedMessages = dict.Values.ToArray();
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

			adapter.PossibleSupportedMessages = adapter.PossibleSupportedMessages.Where(i => i.Type != type).ToArray();
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

		private static readonly PairSet<MessageTypes, MarketDataTypes> _candleDataTypes = new PairSet<MessageTypes, MarketDataTypes>
		{
			{ MessageTypes.CandleTimeFrame, MarketDataTypes.CandleTimeFrame },
			{ MessageTypes.CandleTick, MarketDataTypes.CandleTick },
			{ MessageTypes.CandleVolume, MarketDataTypes.CandleVolume },
			{ MessageTypes.CandleRange, MarketDataTypes.CandleRange },
			{ MessageTypes.CandlePnF, MarketDataTypes.CandlePnF },
			{ MessageTypes.CandleRenko, MarketDataTypes.CandleRenko },
		};

		/// <summary>
		/// Determine the <paramref name="type"/> is candle data type.
		/// </summary>
		/// <param name="type">The data type.</param>
		/// <returns><see langword="true" />, if data type is candle, otherwise, <see langword="false" />.</returns>
		public static bool IsCandleDataType(this MarketDataTypes type)
		{
			return _candleDataTypes.ContainsValue(type);
		}

		/// <summary>
		/// To convert the type of candles <see cref="MarketDataTypes"/> into type of message <see cref="MessageTypes"/>.
		/// </summary>
		/// <param name="type">Candles type.</param>
		/// <returns>Message type.</returns>
		public static MessageTypes ToCandleMessageType(this MarketDataTypes type)
		{
			return _candleDataTypes[type];
		}

		/// <summary>
		/// To convert the type of message <see cref="MessageTypes"/> into type of candles <see cref="MarketDataTypes"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		/// <returns>Candles type.</returns>
		public static MarketDataTypes ToCandleMarketDataType(this MessageTypes type)
		{
			return _candleDataTypes[type];
		}

		private static readonly PairSet<Type, MarketDataTypes> _candleMarketDataTypes = new PairSet<Type, MarketDataTypes>
		{
			{ typeof(TimeFrameCandleMessage), MarketDataTypes.CandleTimeFrame },
			{ typeof(TickCandleMessage), MarketDataTypes.CandleTick },
			{ typeof(VolumeCandleMessage), MarketDataTypes.CandleVolume },
			{ typeof(RangeCandleMessage), MarketDataTypes.CandleRange },
			{ typeof(PnFCandleMessage), MarketDataTypes.CandlePnF },
			{ typeof(RenkoCandleMessage), MarketDataTypes.CandleRenko },
		};

		/// <summary>
		/// Cast candle type <see cref="MarketDataTypes"/> to the message <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="type">Candle type.</param>
		/// <returns>Message type <see cref="CandleMessage"/>.</returns>
		public static Type ToCandleMessage(this MarketDataTypes type)
		{
			var messageType = _candleMarketDataTypes.TryGetKey(type);

			if (messageType == null)
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.WrongCandleType);

			return messageType;
		}

		/// <summary>
		/// Cast message type <see cref="CandleMessage"/> to the <see cref="MarketDataTypes"/>.
		/// </summary>
		/// <param name="messageType">The type of the message <see cref="CandleMessage"/>.</param>
		/// <returns><see cref="MarketDataTypes"/>.</returns>
		public static MarketDataTypes ToCandleMarketDataType(this Type messageType)
		{
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			var dataType = _candleMarketDataTypes.TryGetValue2(messageType);

			if (dataType == null)
				throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);

			return dataType.Value;
		}

		/// <summary>
		/// Determines whether the specified subscription request is supported by the adapter.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="subscription">Subscription.</param>
		/// <returns><see langword="true"/> if the specified subscription request is supported, otherwise, <see langword="false"/>.</returns>
		public static bool IsCandlesSupported(this IMessageAdapter adapter, MarketDataMessage subscription)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			if (subscription == null)
				throw new ArgumentNullException(nameof(subscription));

			if (!adapter.SupportedMarketDataTypes.Contains(subscription.DataType))
				return false;

			var args = adapter.GetCandleArgs(subscription.DataType.ToCandleMessage(), subscription.SecurityId, subscription.From, subscription.To).ToArray();

			if (args.IsEmpty())
				return true;

			return args.Contains(subscription.Arg);
		}

		/// <summary>
		/// Get possible time-frames for the specified instrument.
		/// </summary>
		/// <param name="adapter">Trading system adapter.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <returns>Possible time-frames.</returns>
		public static IEnumerable<TimeSpan> GetTimeFrames(this IMessageAdapter adapter, SecurityId securityId = default, DateTimeOffset? from = null, DateTimeOffset? to = null)
			=> adapter.GetCandleArgs<TimeSpan>(typeof(TimeFrameCandleMessage), securityId, from, to);

		/// <summary>
		/// Get possible args for the specified candle type and instrument.
		/// </summary>
		/// <typeparam name="TArg">Type of <see cref="CandleMessage.Arg"/>.</typeparam>
		/// <param name="adapter">Adapter.</param>
		/// <param name="candleType">The type of the message <see cref="CandleMessage"/>.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		/// <returns>Possible args.</returns>
		public static IEnumerable<TArg> GetCandleArgs<TArg>(this IMessageAdapter adapter, Type candleType, SecurityId securityId = default, DateTimeOffset? from = null, DateTimeOffset? to = null)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.GetCandleArgs(candleType, securityId, from, to).Cast<TArg>();
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
		/// Fields related to last trade.
		/// </summary>
		public static CachedSynchronizedSet<Level1Fields> LastTradeFields { get; } = new CachedSynchronizedSet<Level1Fields>(new[]
		{
			Level1Fields.LastTradeId,
			Level1Fields.LastTradeTime,
			Level1Fields.LastTradeOrigin,
			Level1Fields.LastTradePrice,
			Level1Fields.LastTradeUpDown,
			Level1Fields.LastTradeVolume,
			Level1Fields.IsSystem
		});

		/// <summary>
		/// Is the specified <see cref="Level1Fields"/> is related to last trade.
		/// </summary>
		/// <param name="field">Field.</param>
		/// <returns>Check result.</returns>
		public static bool IsLastTradeField(this Level1Fields field) => LastTradeFields.Contains(field);

		/// <summary>
		/// Fields related to best bid.
		/// </summary>
		public static CachedSynchronizedSet<Level1Fields> BestBidFields { get; } = new CachedSynchronizedSet<Level1Fields>(new[]
		{
			Level1Fields.BestBidPrice,
			Level1Fields.BestBidTime,
			Level1Fields.BestBidVolume
		});

		/// <summary>
		/// Is the specified <see cref="Level1Fields"/> is related to best bid.
		/// </summary>
		/// <param name="field">Field.</param>
		/// <returns>Check result.</returns>
		public static bool IsBestBidField(this Level1Fields field) => BestBidFields.Contains(field);
		
		/// <summary>
		/// Fields related to best ask.
		/// </summary>
		public static CachedSynchronizedSet<Level1Fields> BestAskFields { get; } = new CachedSynchronizedSet<Level1Fields>(new[]
		{
			Level1Fields.BestAskPrice,
			Level1Fields.BestAskTime,
			Level1Fields.BestAskVolume
		});

		/// <summary>
		/// Is the specified <see cref="Level1Fields"/> is related to best ask.
		/// </summary>
		/// <param name="field">Field.</param>
		/// <returns>Check result.</returns>
		public static bool IsBestAskField(this Level1Fields field) => BestAskFields.Contains(field);

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

			try
			{
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
							var idx = s.IndexOf(':');
							return new KeyValuePair<DayOfWeek, Range<TimeSpan>[]>(s.Substring(0, idx).To<DayOfWeek>(), s.Substring(idx + 1).Split("--").Select(s2 =>
							{
								var parts3 = s2.Split('-');
								return new Range<TimeSpan>(parts3[0].ToTimeSpan(_timeFormat), parts3[1].ToTimeSpan(_timeFormat));
							}).ToArray());
						}).ToDictionary()
					});
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.Str2141Params.Put(input), ex);
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

			try
			{
				foreach (var str in input.Split(","))
				{
					var parts = str.Split('=');
					specialDays[parts[0].ToDateTime(_dateFormat)] = parts[1].Split("--").Select(s =>
					{
						var parts2 = s.Split('-');
						return new Range<TimeSpan>(parts2[0].ToTimeSpan(_timeFormat), parts2[1].ToTimeSpan(_timeFormat));
					}).ToArray();
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(LocalizedStrings.Str2141Params.Put(input), ex);
			}

			return specialDays;
		}

		/// <summary>
		/// Is the specified adapter support market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Check result.</returns>
		public static bool IsMarketData(this IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.IsMessageSupported(MessageTypes.MarketData) || adapter.IsMessageSupported(MessageTypes.SecurityLookup);
		}

		/// <summary>
		/// Is the specified adapter support transactions.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Check result.</returns>
		public static bool IsTransactional(this IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.IsMessageSupported(MessageTypes.OrderRegister);
		}

		private static bool IsOrderConditionOf(this IMessageAdapter adapter, Type interfaceType)
		{
			if (interfaceType == null)
				throw new ArgumentNullException(nameof(interfaceType));

			var type = adapter.OrderConditionType;

			return type != null && interfaceType.IsAssignableFrom(type);
		}

		/// <summary>
		/// Determines whether the adapter support stop-loss orders.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Check result.</returns>
		public static bool IsSupportStopLoss(this IMessageAdapter adapter)
			=> adapter.IsOrderConditionOf(typeof(IStopLossOrderCondition));

		/// <summary>
		/// Determines whether the adapter support take-profit orders.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Check result.</returns>
		public static bool IsSupportTakeProfit(this IMessageAdapter adapter)
			=> adapter.IsOrderConditionOf(typeof(ITakeProfitOrderCondition));

		/// <summary>
		/// Determines whether the adapter support withdraw orders.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Check result.</returns>
		public static bool IsSupportWithdraw(this IMessageAdapter adapter)
			=> adapter.IsOrderConditionOf(typeof(IWithdrawOrderCondition));

		/// <summary>
		/// Initialize <see cref="SecurityId.SecurityCode"/>.
		/// </summary>
		/// <param name="message">A message containing info about the security.</param>
		/// <param name="secCode">Security code.</param>
		public static void SetSecurityCode(this SecurityMessage message, string secCode)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var secId = message.SecurityId;
			secId.SecurityCode = secCode;
			message.SecurityId = secId;
		}

		/// <summary>
		/// Initialize <see cref="SecurityId.Native"/>.
		/// </summary>
		/// <param name="secId">Security ID.</param>
		/// <param name="nativeId">Native (internal) trading system security id.</param>
		/// <returns>Security ID.</returns>
		public static SecurityId SetNativeId(this SecurityId secId, object nativeId)
		{
			secId.Native = nativeId;
			return secId;
		}

		/// <summary>
		/// Initialize <see cref="SecurityId.Native"/>.
		/// </summary>
		/// <param name="message">A message containing info about the security.</param>
		/// <param name="nativeId">Native (internal) trading system security id.</param>
		public static void SetNativeId(this SecurityMessage message, object nativeId)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.SecurityId = message.SecurityId.SetNativeId(nativeId);
		}

		/// <summary>
		/// Initialize <see cref="SecurityLookupMessage.SecurityTypes"/>.
		/// </summary>
		/// <param name="message">Message security lookup for specified criteria.</param>
		/// <param name="type">Security type.</param>
		/// <param name="types">Securities types.</param>
		public static void SetSecurityTypes(this SecurityLookupMessage message, SecurityTypes? type, IEnumerable<SecurityTypes> types = null)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (type != null)
				message.SecurityType = type.Value;
			else if (types != null)
			{
				var set = types.ToHashSet();

				if (set.Count <= 0)
					return;

				if (set.Count == 1)
					message.SecurityType = set.First();
				else
					message.SecurityTypes = set.ToArray();
			}
		}

		/// <summary>
		/// Get <see cref="SecurityLookupMessage.SecurityTypes"/>.
		/// </summary>
		/// <param name="message">Message security lookup for specified criteria.</param>
		/// <returns>Securities types.</returns>
		public static HashSet<SecurityTypes> GetSecurityTypes(this SecurityLookupMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var types = new HashSet<SecurityTypes>();

			if (message.SecurityType != null)
				types.Add(message.SecurityType.Value);
			else if (message.SecurityTypes != null)
				types.AddRange(message.SecurityTypes);

			return types;
		}

		/// <summary>
		/// Get adapter by the specified key.
		/// </summary>
		/// <param name="adapters">All available adapters.</param>
		/// <param name="id">Adapter identifier.</param>
		/// <returns>Found adapter or <see langword="null"/>.</returns>
		public static IMessageAdapter FindById(this IEnumerable<IMessageAdapter> adapters, Guid id)
		{
			return adapters.FirstOrDefault(a => a.Id == id);
		}

		/// <summary>
		/// Create <see cref="IMessageAdapter"/> instance.
		/// </summary>
		/// <param name="adapterType">Adapter type.</param>
		/// <returns><see cref="IMessageAdapter"/> instance.</returns>
		public static IMessageAdapter CreateAdapter(this Type adapterType)
		{
			return adapterType.CreateAdapter(new IncrementalIdGenerator());
		}

		/// <summary>
		/// Create <see cref="IMessageAdapter"/>.
		/// </summary>
		/// <param name="adapterType">Adapter type.</param>
		/// <param name="idGenerator">Transaction id generator.</param>
		/// <returns><see cref="IMessageAdapter"/> instance.</returns>
		public static IMessageAdapter CreateAdapter(this Type adapterType, IdGenerator idGenerator)
		{
			return adapterType.CreateInstance<IMessageAdapter>(idGenerator);
		}

		/// <summary>
		/// Find adapter by the specified type.
		/// </summary>
		/// <typeparam name="TAdapter">The adapter type.</typeparam>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Found adapter or <see langword="null"/>.</returns>
		public static TAdapter FindAdapter<TAdapter>(this IMessageAdapter adapter)
			where TAdapter : IMessageAdapter
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			if (adapter is TAdapter t)
				return t;
			else if (adapter is IMessageAdapterWrapper wrapper)
				return wrapper.FindAdapter<TAdapter>();

			return default;
		}

		/// <summary>
		/// Find adapter by the specified type.
		/// </summary>
		/// <typeparam name="TAdapter">The adapter type.</typeparam>
		/// <param name="wrapper">Wrapping based adapter.</param>
		/// <returns>Found adapter or <see langword="null"/>.</returns>
		public static TAdapter FindAdapter<TAdapter>(this IMessageAdapterWrapper wrapper)
			where TAdapter : IMessageAdapter
		{
			if (wrapper == null)
				throw new ArgumentNullException(nameof(wrapper));

			if (wrapper is TAdapter adapter)
				return adapter;

			if (wrapper.InnerAdapter is IMessageAdapterWrapper w)
				return w.FindAdapter<TAdapter>();

			return default;
		}

		/// <summary>
		/// Determines whether the reply contains an error <see cref="MarketDataMessage.Error"/> or has <see cref="MarketDataMessage.IsNotSupported"/>.
		/// </summary>
		/// <param name="message">Reply.</param>
		/// <returns>Check result.</returns>
		public static bool IsOk(this MarketDataMessage message)
		{
			return message.Error == null && !message.IsNotSupported;
		}

		/// <summary>
		/// Special set mean any depth for <see cref="IMessageAdapter.SupportedOrderBookDepths"/> option.
		/// </summary>
		public static IEnumerable<int> AnyDepths = Array.AsReadOnly(new[] { -1 });

		/// <summary>
		/// Get the nearest supported depth for the specified.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="depth">Depth.</param>
		/// <returns>Supported depth.</returns>
		public static int? NearestSupportedDepth(this IMessageAdapter adapter, int depth)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			if (depth <= 0)
				throw new ArgumentOutOfRangeException(nameof(depth));

			var supported = adapter.SupportedOrderBookDepths;

			if (ReferenceEquals(supported, AnyDepths))
				return depth;

			var arr = supported.ToArray();

			if (arr.IsEmpty())
				return null;

			return arr.Where(d => d >= depth).OrderBy().FirstOr() ?? arr.Max();
		}

		/// <summary>
		/// Create <see cref="IOrderLogMarketDepthBuilder"/> instance.
		/// </summary>
		/// <param name="builderType">Builder type.</param>
		/// <param name="securityId">Security ID.</param>
		/// <returns><see cref="IOrderLogMarketDepthBuilder"/> instance.</returns>
		public static IOrderLogMarketDepthBuilder CreateOrderLogMarketDepthBuilder(this Type builderType, SecurityId securityId)
		{
			return builderType.CreateInstance<IOrderLogMarketDepthBuilder>(securityId);
		}

		/// <summary>
		/// Get time-frame from the specified market-data message.
		/// </summary>
		/// <param name="mdMsg">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <returns>Time-frame.</returns>
		public static TimeSpan GetTimeFrame(this MarketDataMessage mdMsg)
		{
			if (mdMsg == null)
				throw new ArgumentNullException(nameof(mdMsg));

			if (!(mdMsg.Arg is TimeSpan timeFrame))
				throw new InvalidOperationException(LocalizedStrings.WrongCandleArg.Put(mdMsg.Arg));

			return timeFrame;
		}

		internal static bool HandleErrorResponse(this Message message, Exception ex, DateTimeOffset currentTime, Action<Message> sendOut)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (ex == null)
				throw new ArgumentNullException(nameof(ex));

			if (sendOut == null)
				throw new ArgumentNullException(nameof(sendOut));

			void SendOutErrorExecution(ExecutionMessage execMsg)
			{
				execMsg.ServerTime = currentTime;
				execMsg.Error = ex;
				execMsg.OrderState = OrderStates.Failed;
				sendOut(execMsg);
			}

			switch (message.Type)
			{
				case MessageTypes.Connect:
					sendOut(new ConnectMessage { Error = ex });
					return true;

				case MessageTypes.Disconnect:
					sendOut(new DisconnectMessage { Error = ex });
					return true;

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				{
					var replyMsg = ((OrderMessage)message).CreateReply();
					SendOutErrorExecution(replyMsg);
					return true;
				}
				case MessageTypes.OrderPairReplace:
				{
					var replyMsg = ((OrderPairReplaceMessage)message).Message1.CreateReply();
					SendOutErrorExecution(replyMsg);
					return true;
				}

				case MessageTypes.MarketData:
				{
					var reply = (MarketDataMessage)message.Clone();
					reply.OriginalTransactionId = reply.TransactionId;
					reply.Error = ex;
					sendOut(reply);
					return true;
				}

				case MessageTypes.SecurityLookup:
				{
					var lookupMsg = (SecurityLookupMessage)message;
					sendOut(new SecurityLookupResultMessage
					{
						OriginalTransactionId = lookupMsg.TransactionId,
						Error = ex
					});
					return true;
				}

				case MessageTypes.BoardLookup:
				{
					var lookupMsg = (BoardLookupMessage)message;
					sendOut(new BoardLookupResultMessage
					{
						OriginalTransactionId = lookupMsg.TransactionId,
						Error = ex
					});
					return true;
				}

				case MessageTypes.PortfolioLookup:
				{
					var lookupMsg = (PortfolioLookupMessage)message;
					sendOut(new PortfolioLookupResultMessage
					{
						OriginalTransactionId = lookupMsg.TransactionId,
						Error = ex
					});
					return true;
				}

				case MessageTypes.UserLookup:
				{
					var lookupMsg = (UserLookupMessage)message;
					sendOut(new UserLookupResultMessage
					{
						OriginalTransactionId = lookupMsg.TransactionId,
						Error = ex
					});
					return true;
				}

				case MessageTypes.UserRequest:
				{
					var requestMsg = (UserRequestMessage)message;
					sendOut(new UserRequestMessage
					{
						OriginalTransactionId = requestMsg.TransactionId,
						Error = ex
					});
					return true;
				}

				case MessageTypes.ChangePassword:
				{
					var pwdMsg = (ChangePasswordMessage)message;
					sendOut(new ChangePasswordMessage
					{
						OriginalTransactionId = pwdMsg.TransactionId,
						Error = ex
					});
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Get subscription identifiers from the specified message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Identifiers.</returns>
		public static IEnumerable<long> GetSubscriptionIds(this ISubscriptionIdMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.SubscriptionIds != null)
				return message.SubscriptionIds;
			else if (message.SubscriptionId > 0)
				return new[] { message.SubscriptionId };
			else
				return Enumerable.Empty<long>();
		}

		/// <summary>
		/// Is the data type required security info.
		/// </summary>
		/// <param name="type">Market data type.</param>
		/// <returns>Check result.</returns>
		public static bool IsSecurityRequired(this MarketDataTypes type)
			=> type != MarketDataTypes.News && type != MarketDataTypes.Board;
	}
}