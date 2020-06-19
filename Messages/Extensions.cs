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
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;
	using Ecng.Localization;

	using MoreLinq;

	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static partial class Extensions
	{
		static Extensions()
		{
			string TimeSpanToString(TimeSpan arg) => arg.ToString().Replace(':', '-');
			TimeSpan StringToTimeSpan(string str) => str.Replace('-', ':').To<TimeSpan>();

			RegisterCandleType(typeof(TimeFrameCandleMessage), MessageTypes.CandleTimeFrame, MarketDataTypes.CandleTimeFrame, typeof(TimeFrameCandleMessage).Name.Remove(nameof(Message)), StringToTimeSpan, TimeSpanToString);
			RegisterCandleType(typeof(TickCandleMessage), MessageTypes.CandleTick, MarketDataTypes.CandleTick, typeof(TickCandleMessage).Name.Remove(nameof(Message)), str => str.To<int>(), arg => arg.ToString());
			RegisterCandleType(typeof(VolumeCandleMessage), MessageTypes.CandleVolume, MarketDataTypes.CandleVolume, typeof(VolumeCandleMessage).Name.Remove(nameof(Message)), str => str.To<decimal>(), arg => arg.ToString());
			RegisterCandleType(typeof(RangeCandleMessage), MessageTypes.CandleRange, MarketDataTypes.CandleRange, typeof(RangeCandleMessage).Name.Remove(nameof(Message)), str => str.ToUnit(), arg => arg.ToString());
			RegisterCandleType(typeof(RenkoCandleMessage), MessageTypes.CandleRenko, MarketDataTypes.CandleRenko, typeof(RenkoCandleMessage).Name.Remove(nameof(Message)), str => str.ToUnit(), arg => arg.ToString());
			RegisterCandleType(typeof(PnFCandleMessage), MessageTypes.CandlePnF, MarketDataTypes.CandlePnF, typeof(PnFCandleMessage).Name.Remove(nameof(Message)), str =>
			{
				var parts = str.Split('_');

				return new PnFArg
				{
					BoxSize = parts[0].ToUnit(),
					ReversalAmount = parts[1].To<int>()
				};
			}, pnf => $"{pnf.BoxSize}_{pnf.ReversalAmount}");
			RegisterCandleType(typeof(HeikinAshiCandleMessage), MessageTypes.CandleHeikinAshi, MarketDataTypes.CandleHeikinAshi, typeof(HeikinAshiCandleMessage).Name.Remove(nameof(Message)), StringToTimeSpan, TimeSpanToString);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <param name="adapter">Trading system adapter.</param>
		/// <param name="pfName">Portfolio name.</param>
		/// <returns>Portfolio change message.</returns>
		public static PositionChangeMessage CreatePortfolioChangeMessage(this IMessageAdapter adapter, string pfName)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			if (pfName.IsEmpty())
				throw new ArgumentNullException(nameof(pfName));

			var time = adapter.CurrentTime;

			return new PositionChangeMessage
			{
				SecurityId = SecurityId.Money,
				PortfolioName = pfName,
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

			if (pfName.IsEmpty())
				throw new ArgumentNullException(nameof(pfName));

			var time = adapter.CurrentTime;

			return new PositionChangeMessage
			{
				PortfolioName = pfName,
				SecurityId = securityId,
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

			var spreadMiddle = (decimal?)message.Changes.TryGetValue(Level1Fields.SpreadMiddle);

			if (spreadMiddle == null)
			{
				var bestBid = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidPrice);
				var bestAsk = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskPrice);

				spreadMiddle = bestBid.GetSpreadMiddle(bestAsk);
			}

			return spreadMiddle;
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
				return bestBidPrice.Value.GetSpreadMiddle(bestAskPrice.Value);

			return bestAskPrice ?? bestBidPrice.Value;
		}

		/// <summary>
		/// Get middle of spread.
		/// </summary>
		/// <param name="bestBidPrice">Best bid price.</param>
		/// <param name="bestAskPrice">Best ask price.</param>
		/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
		public static decimal GetSpreadMiddle(this decimal bestBidPrice, decimal bestAskPrice)
		{
			return (bestAskPrice + bestBidPrice) / 2;
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

			return message.TransactionId.CreateOrderReply();
		}

		/// <summary>
		/// Create order's transaction reply.
		/// </summary>
		/// <param name="transactionId">Transaction ID.</param>
		/// <returns>The message contains information about the execution.</returns>
		public static ExecutionMessage CreateOrderReply(this long transactionId)
		{
			return new ExecutionMessage
			{
				OriginalTransactionId = transactionId,
				ExecutionType = ExecutionTypes.Transaction,
				HasOrderInfo = true,
			};
		}

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
		/// Fill the <see cref="IMessageAdapter.SupportedInMessages"/> message types related to transactional.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void AddTransactionalSupport(this MessageAdapter adapter)
		{
			foreach (var type in TransactionalMessageTypes)
				adapter.AddSupportedMessage(type, false);
		}

		/// <summary>
		/// Remove from <see cref="IMessageAdapter.SupportedInMessages"/> message types related to transactional.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveTransactionalSupport(this MessageAdapter adapter)
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
		/// Fill the <see cref="IMessageAdapter.SupportedInMessages"/> message types related to market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void AddMarketDataSupport(this MessageAdapter adapter)
		{
			foreach (var type in MarketDataMessageTypes)
				adapter.AddSupportedMessage(type, true);
		}

		/// <summary>
		/// Remove from <see cref="IMessageAdapter.SupportedInMessages"/> message types related to market-data.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveMarketDataSupport(this MessageAdapter adapter)
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
		/// Add the message type info <see cref="IMessageAdapter.SupportedInMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		[Obsolete]
		public static void AddSupportedMessage(this MessageAdapter adapter, MessageTypes type)
		{
			AddSupportedMessage(adapter, type, IsMarketData(type));
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.SupportedInMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <param name="isMarketData"><paramref name="type"/> is market-data type.</param>
		public static void AddSupportedMessage(this MessageAdapter adapter, MessageTypes type, bool? isMarketData)
		{
			adapter.AddSupportedMessage(new MessageTypeInfo(type, isMarketData));
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.PossibleSupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="info">Extended info for <see cref="MessageTypes"/>.</param>
		public static void AddSupportedMessage(this MessageAdapter adapter, MessageTypeInfo info)
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
		/// Remove the message type from <see cref="IMessageAdapter.PossibleSupportedMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void RemoveSupportedMessage(this MessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.PossibleSupportedMessages = adapter.PossibleSupportedMessages.Where(i => i.Type != type).ToArray();
		}

		/// <summary>
		/// Determines whether the specified message type is contained in <see cref="IMessageAdapter.SupportedInMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <returns><see langword="true"/> if the specified message type is supported, otherwise, <see langword="false"/>.</returns>
		public static bool IsMessageSupported(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.SupportedInMessages.Contains(type);
		}

		/// <summary>
		/// Add market data type into <see cref="IMessageAdapter.SupportedMarketDataTypes"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="dataType">Data type info.</param>
		public static void AddSupportedMarketDataType(this MessageAdapter adapter, DataType dataType)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedMarketDataTypes = adapter.SupportedMarketDataTypes.Concat(dataType).ToArray();
		}

		/// <summary>
		/// Remove market data type from <see cref="IMessageAdapter.SupportedInMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Market data type.</param>
		public static void RemoveSupportedMarketDataType(this MessageAdapter adapter, DataType type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedMarketDataTypes = adapter.SupportedMarketDataTypes.Except(new[] { type }).ToArray();
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.SupportedResultMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void AddSupportedResultMessage(this MessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedResultMessages = adapter.SupportedResultMessages.Concat(type).Distinct();
		}

		/// <summary>
		/// Remove the message type from <see cref="IMessageAdapter.SupportedResultMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void RemoveSupportedResultMessage(this MessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedResultMessages = adapter.SupportedResultMessages.Where(t => t != type);
		}

		/// <summary>
		/// Determines whether the specified message type is contained in <see cref="IMessageAdapter.SupportedResultMessages"/>..
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <returns><see langword="true"/> if the specified message type is supported, otherwise, <see langword="false"/>.</returns>
		public static bool IsResultMessageSupported(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.SupportedResultMessages.Contains(type);
		}

		/// <summary>
		/// Add the message type info <see cref="IMessageAdapter.SupportedOutMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void AddSupportedOutMessage(this MessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedOutMessages = adapter.SupportedOutMessages.Concat(type).Distinct();
		}

		/// <summary>
		/// Remove the message type from <see cref="IMessageAdapter.SupportedOutMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		public static void RemoveSupportedOutMessage(this MessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedOutMessages = adapter.SupportedOutMessages.Where(t => t != type);
		}

		/// <summary>
		/// Determines whether the specified message type is contained in <see cref="IMessageAdapter.SupportedOutMessages"/>..
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <returns><see langword="true"/> if the specified message type is supported, otherwise, <see langword="false"/>.</returns>
		public static bool IsOutMessageSupported(this IMessageAdapter adapter, MessageTypes type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.SupportedOutMessages.Contains(type);
		}

		private static readonly CachedSynchronizedPairSet<MessageTypes, Type> _candleDataTypes = new CachedSynchronizedPairSet<MessageTypes, Type>();

		/// <summary>
		/// Determine the <paramref name="type"/> is candle data type.
		/// </summary>
		/// <param name="type">Message type.</param>
		/// <returns><see langword="true" />, if data type is candle, otherwise, <see langword="false" />.</returns>
		public static bool IsCandle(this MessageTypes type)	=> _candleDataTypes.ContainsKey(type);

		/// <summary>
		/// To convert the type of candles <see cref="CandleMessage"/> into type of message <see cref="MessageTypes"/>.
		/// </summary>
		/// <param name="type">Candles type.</param>
		/// <returns>Message type.</returns>
		public static MessageTypes ToMessageType(this Type type) => _candleDataTypes[type];

		/// <summary>
		/// To convert the type of message <see cref="MessageTypes"/> into type of candles <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		/// <returns>Candles type.</returns>
		public static Type ToCandleMessageType(this MessageTypes type) => _candleDataTypes[type];

		private static readonly SynchronizedDictionary<Type, Tuple<Func<string, object>, Func<object, string>>> _dataTypeArgConverters = new SynchronizedDictionary<Type, Tuple<Func<string, object>, Func<object, string>>>
		{
			{ typeof(ExecutionMessage), Tuple.Create((Func<string, object>)(str => str.To<ExecutionTypes>()), (Func<object, string>)(arg => arg.To<string>())) }
		};

		/// <summary>
		/// To convert string representation of the candle argument into typified.
		/// </summary>
		/// <param name="messageType">The type of candle message.</param>
		/// <param name="str">The string representation of the argument.</param>
		/// <returns>Argument.</returns>
		public static object ToDataTypeArg(this Type messageType, string str)
		{
			if (messageType is null)
				throw new ArgumentNullException(nameof(messageType));

			if (str.IsEmpty())
			{
				return null;
				//throw new ArgumentNullException(nameof(str));
			}

			if (_dataTypeArgConverters.TryGetValue(messageType, out var converter))
				return converter.Item1(str);

			return str;
			//throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);
		}

		/// <summary>
		/// Convert candle parameter into folder name replacing the reserved symbols.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns>Directory name.</returns>
		public static string DataTypeArgToString(this DataType dataType)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			return dataType.MessageType.DataTypeArgToString(dataType.Arg);
		}

		/// <summary>
		/// Convert candle parameter into folder name replacing the reserved symbols.
		/// </summary>
		/// <param name="messageType">The type of candle message.</param>
		/// <param name="arg">Candle arg.</param>
		/// <returns>Directory name.</returns>
		public static string DataTypeArgToString(this Type messageType, object arg)
		{
			if (messageType == null)
				throw new ArgumentNullException(nameof(messageType));

			if (_dataTypeArgConverters.TryGetValue(messageType, out var converter))
				return converter.Item2(arg);

			return arg.To<string>();
			//throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);
		}

		private static readonly SynchronizedPairSet<DataType, string> _fileNames = new SynchronizedPairSet<DataType, string>(EqualityComparer<DataType>.Default, StringComparer.InvariantCultureIgnoreCase)
		{
			{ DataType.Ticks, "trades" },
			{ DataType.OrderLog, "orderLog" },
			{ DataType.Transactions, "transactions" },
			{ DataType.MarketDepth, "quotes" },
			{ DataType.Level1, "security" },
			{ DataType.PositionChanges, "position" },
			{ DataType.News, "news" },
			{ DataType.Board, "board" },
		};

		/// <summary>
		/// Convert <see cref="DataType"/> to file name.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <returns>Data type info.</returns>
		public static DataType FileNameToDataType(this string fileName)
		{
			var info = _fileNames.TryGetKey(fileName);

			if (info != null)
				return info;

			if (!fileName.StartsWithIgnoreCase("candles_"))
				return null;

			var parts = fileName.Split('_');

			if (parts.Length != 3)
				return null;

			if (!_fileNames.TryGetKey(parts[1], out var type))
				return null;

			return DataType.Create(type.MessageType, type.MessageType.ToDataTypeArg(parts[2]));
		}

		/// <summary>
		/// Convert file name to <see cref="DataType"/>.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns>File name.</returns>
		public static string DataTypeToFileName(this DataType dataType)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			if (dataType.MessageType.IsCandleMessage())
			{
				if (_fileNames.TryGetValue(DataType.Create(dataType.MessageType, null), out var fileName))
					return $"candles_{fileName}_{dataType.DataTypeArgToString()}";

				return null;
			}
			else
			{
				return _fileNames.TryGetValue(dataType);
			}
		}

		/// <summary>
		/// All registered candle types.
		/// </summary>
		public static IEnumerable<Type> AllCandleTypes => _candleDataTypes.CachedValues;

		/// <summary>
		/// Register new candle type.
		/// </summary>
		/// <param name="messageType">The type of candle message.</param>
		/// <param name="type">Message type.</param>
		/// <param name="dataType">Candles type.</param>
		/// <param name="fileName">File name.</param>
		/// <param name="argParserTo"><see cref="string"/> to <typeparamref name="TArg"/> converter.</param>
		/// <param name="argParserFrom"><typeparamref name="TArg"/> to <see cref="string"/> converter.</param>
		public static void RegisterCandleType<TArg>(Type messageType, MessageTypes type, MarketDataTypes dataType, string fileName, Func<string, TArg> argParserTo, Func<TArg, string> argParserFrom)
		{
			if (messageType is null)
				throw new ArgumentNullException(nameof(messageType));

			if (argParserTo is null)
				throw new ArgumentNullException(nameof(argParserTo));

			if (argParserFrom is null)
				throw new ArgumentNullException(nameof(argParserFrom));

			T Do<T>(Func<T> func) => CultureInfo.InvariantCulture.DoInCulture(func);

			Func<string, object> p1 = str => Do(() => argParserTo(str));
			Func<object, string> p2 = arg => arg is string s ? s : Do(() => argParserFrom((TArg)arg));

#pragma warning disable CS0612 // Type or member is obsolete
			_messageTypeMap.Add(dataType, Tuple.Create(type, default(object)));
#pragma warning restore CS0612 // Type or member is obsolete

			_candleDataTypes.Add(type, messageType);
			_dataTypeArgConverters.Add(messageType, Tuple.Create(p1, p2));
			_fileNames.Add(DataType.Create(messageType, null), fileName);
		}

		/// <summary>
		/// Cast candle type <see cref="MessageTypes"/> to the message <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="type">Candle type.</param>
		/// <returns>Message type <see cref="CandleMessage"/>.</returns>
		public static Type ToCandleMessage(this MessageTypes type)
		{
			if (!_candleDataTypes.TryGetValue(type, out var messageType))
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.WrongCandleType);

			return messageType;
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

			if (!adapter.SupportedMarketDataTypes.Contains(subscription.DataType2))
				return false;

			var args = adapter.GetCandleArgs(subscription.DataType2.MessageType, subscription.SecurityId, subscription.From, subscription.To).ToArray();

			if (args.IsEmpty())
				return true;

			return args.Contains(subscription.GetArg());
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
		/// Convert <see cref="string"/> to <see cref="DataType"/> value.
		/// </summary>
		/// <param name="type">type.</param>
		/// <param name="arg">Arg.</param>
		/// <returns>Data type info.</returns>
		public static DataType ToDataType(this string type, string arg)
		{
			if (type.IsEmpty())
				throw new ArgumentNullException(nameof(type));

			if (type.Contains(','))
			{
				var messageType = type.To<Type>();
				return DataType.Create(messageType, messageType.ToDataTypeArg(arg));
			}
			else
			{
#pragma warning disable CS0612 // Type or member is obsolete
				var dataType = type.To<MarketDataTypes>();
				return dataType.ToDataType(dataType.ToMessageType(out _).ToCandleMessage().ToDataTypeArg(arg));
#pragma warning restore CS0612 // Type or member is obsolete
			}
		}

		/// <summary>
		/// Convert <see cref="DataType"/> to <see cref="string"/> value.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns><see cref="string"/> value.</returns>
		public static (string type, string arg) FormatToString(this DataType dataType)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			var type = dataType.MessageType.GetTypeName(false);
			var arg = dataType.DataTypeArgToString();
			return (type, arg);
		}

		/// <summary>
		/// Convert <see cref="ExecutionTypes"/> to <see cref="DataType"/> value.
		/// </summary>
		/// <param name="type">Data type, information about which is contained in the <see cref="ExecutionMessage"/>.</param>
		/// <returns>Data type info.</returns>
		public static DataType ToDataType(this ExecutionTypes type)
		{
			switch (type)
			{
				case ExecutionTypes.Tick:
					return DataType.Ticks;
				case ExecutionTypes.Transaction:
					return DataType.Transactions;
				case ExecutionTypes.OrderLog:
					return DataType.OrderLog;
				default:
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);
			}
		}

		/// <summary>
		/// Determines whether the specified market-data type is supported by the adapter.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="type">Message type.</param>
		/// <returns><see langword="true"/> if the specified message type is supported, otherwise, <see langword="false"/>.</returns>
		public static bool IsMarketDataTypeSupported(this IMessageAdapter adapter, DataType type)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.SupportedMarketDataTypes.Contains(type);
		}

		/// <summary>
		/// Remove all market data types from <see cref="IMessageAdapter.SupportedInMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		public static void RemoveSupportedAllMarketDataTypes(this MessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			adapter.SupportedMarketDataTypes = ArrayHelper.Empty<DataType>();
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
		/// <param name="originalTransactionId">ID of the original message <see cref="ITransactionIdMessage.TransactionId"/> for which this message is a response.</param>
		/// <returns>Error message.</returns>
		public static ErrorMessage ToErrorMessage(this Exception error, long originalTransactionId = 0)
		{
			if (error == null)
				throw new ArgumentNullException(nameof(error));

			return new ErrorMessage { Error = error, OriginalTransactionId = originalTransactionId };
		}

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

			using (var client = new WebClient())
				return decimal.Parse(client.DownloadString($"https://stocksharp.com/services/currencyconverter.ashx?from={from}&to={to}&date={(long)date.ToUnix()}"), CultureInfo.InvariantCulture);
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
				foreach (var str in input.SplitByComma())
				{
					var parts = str.Split('=');
					periods.Add(new WorkingTimePeriod
					{
						Till = parts[0].ToDateTime(_dateFormat),
						Times = parts[1].SplitBySep("--").Select(s =>
						{
							var parts2 = s.Split('-');
							return new Range<TimeSpan>(parts2[0].ToTimeSpan(_timeFormat), parts2[1].ToTimeSpan(_timeFormat));
						}).ToList(),
						SpecialDays = parts[2].SplitBySep("//").Select(s =>
						{
							var idx = s.IndexOf(':');
							return new KeyValuePair<DayOfWeek, Range<TimeSpan>[]>(s.Substring(0, idx).To<DayOfWeek>(), s.Substring(idx + 1).SplitBySep("--").Select(s2 =>
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
			return specialDays.Select(p => $"{p.Key:yyyyMMdd}=" + p.Value.Select(r => $"{r.Min:hh\\:mm}-{r.Max:hh\\:mm}").Join("--")).JoinComma();
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
				foreach (var str in input.SplitByComma())
				{
					var parts = str.Split('=');
					specialDays[parts[0].ToDateTime(_dateFormat)] = parts[1].SplitBySep("--").Select(s =>
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

			if (wrapper.InnerAdapter is TAdapter adapter2)
				return adapter2;

			if (wrapper.InnerAdapter is IMessageAdapterWrapper w)
				return w.FindAdapter<TAdapter>();

			return default;
		}

		/// <summary>
		/// Determines whether the reply contains an error <see cref="SubscriptionResponseMessage.Error"/>.
		/// </summary>
		/// <param name="message">Reply.</param>
		/// <returns>Check result.</returns>
		public static bool IsOk(this SubscriptionResponseMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.Error == null;
		}

		/// <summary>
		/// Determines whether the reply contains the error <see cref="SubscriptionResponseMessage.NotSupported"/>.
		/// </summary>
		/// <param name="message">Reply.</param>
		/// <returns>Check result.</returns>
		public static bool IsNotSupported(this SubscriptionResponseMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.Error == SubscriptionResponseMessage.NotSupported;
		}

		/// <summary>
		/// Create non supported subscription response.
		/// </summary>
		/// <param name="id">ID of the original message for which this message is a response.</param>
		/// <returns>Subscription response message.</returns>
		public static SubscriptionResponseMessage CreateNotSupported(this long id)
		{
			return id.CreateSubscriptionResponse(SubscriptionResponseMessage.NotSupported);
		}

		/// <summary>
		/// Create subscription response.
		/// </summary>
		/// <param name="id">ID of the original message for which this message is a response.</param>
		/// <param name="error">Error info.</param>
		/// <returns>Subscription response message.</returns>
		public static SubscriptionResponseMessage CreateSubscriptionResponse(this long id, Exception error = null)
		{
			return new SubscriptionResponseMessage
			{
				OriginalTransactionId = id,
				Error = error
			};
		}

		/// <summary>
		/// Create subscription response.
		/// </summary>
		/// <param name="message">Subscription.</param>
		/// <param name="error">Error info.</param>
		/// <returns>Subscription response message.</returns>
		public static SubscriptionResponseMessage CreateResponse(this ISubscriptionMessage message, Exception error = null)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.TransactionId.CreateSubscriptionResponse(error);
		}

		/// <summary>
		/// Create <see cref="SubscriptionOnlineMessage"/> or <see cref="SubscriptionFinishedMessage"/> depends of <see cref="ISubscriptionMessage.To"/>.
		/// </summary>
		/// <param name="message">Subscription.</param>
		/// <returns>Message.</returns>
		public static Message CreateResult(this ISubscriptionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (!message.IsSubscribe)
				return new SubscriptionResponseMessage { OriginalTransactionId = message.TransactionId };

			var reply = message.To == null ? (IOriginalTransactionIdMessage)new SubscriptionOnlineMessage() : new SubscriptionFinishedMessage();
			reply.OriginalTransactionId = message.TransactionId;
#if MSG_TRACE
			((Message)reply).StackTrace = ((Message)message).StackTrace;
#endif
			return (Message)reply;
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
		/// Get typed argument.
		/// </summary>
		/// <param name="mdMsg">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <returns>The additional argument, associated with data. For example, candle argument.</returns>
		public static object GetArg(this MarketDataMessage mdMsg)
		{
			if (mdMsg is null)
				throw new ArgumentNullException(nameof(mdMsg));

			return mdMsg.DataType2.Arg;
		}

		/// <summary>
		/// Get typed argument.
		/// </summary>
		/// <typeparam name="TArg">Arg type.</typeparam>
		/// <param name="mdMsg">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <returns>The additional argument, associated with data. For example, candle argument.</returns>
		public static TArg GetArg<TArg>(this MarketDataMessage mdMsg)
		{
			if (!(mdMsg.GetArg() is TArg arg))
				throw new InvalidOperationException(LocalizedStrings.WrongCandleArg.Put(mdMsg.DataType2.Arg));

			return arg;
		}

		/// <summary>
		/// Set typed argument.
		/// </summary>
		/// <typeparam name="TArg">Arg type.</typeparam>
		/// <param name="mdMsg">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <returns>Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</returns>
		public static MarketDataMessage SetArg<TArg>(this MarketDataMessage mdMsg, TArg arg)
		{
			if (mdMsg is null)
				throw new ArgumentNullException(nameof(mdMsg));

			mdMsg.DataType2.Arg = arg;
			return mdMsg;
		}

		/// <summary>
		/// Get time-frame from the specified market-data message.
		/// </summary>
		/// <param name="mdMsg">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <returns>Time-frame.</returns>
		public static TimeSpan GetTimeFrame(this MarketDataMessage mdMsg)
			=> mdMsg.GetArg<TimeSpan>();

		/// <summary>
		/// 
		/// </summary>
		/// <param name="message"></param>
		/// <param name="ex"></param>
		/// <param name="logs"></param>
		/// <param name="sendOut"></param>
		/// <param name="getSubscribers"></param>
		public static void HandleErrorResponse(this Message message, Exception ex, ILogReceiver logs, Action<Message> sendOut, Func<DataType, long[]> getSubscribers = null)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (ex == null)
				throw new ArgumentNullException(nameof(ex));

			if (sendOut == null)
				throw new ArgumentNullException(nameof(sendOut));

			logs.AddErrorLog(ex);

			void SendOutErrorExecution(ExecutionMessage execMsg)
			{
				execMsg.ServerTime = logs.CurrentTime;
				execMsg.Error = ex;
				execMsg.OrderState = OrderStates.Failed;

				var subscribers = getSubscribers?.Invoke(DataType.Transactions);

				if (subscribers != null)
					execMsg.SetSubscriptionIds(subscribers);

				sendOut(execMsg);
			}

			switch (message.Type)
			{
				case MessageTypes.Connect:
					sendOut(new ConnectMessage { Error = ex });
					break;

				case MessageTypes.Disconnect:
					sendOut(new DisconnectMessage { Error = ex });
					break;

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				{
					var replyMsg = ((OrderMessage)message).CreateReply();
					SendOutErrorExecution(replyMsg);
					break;
				}
				case MessageTypes.OrderPairReplace:
				{
					var replyMsg = ((OrderPairReplaceMessage)message).Message1.CreateReply();
					SendOutErrorExecution(replyMsg);
					break;
				}

				case MessageTypes.ChangePassword:
				{
					var pwdMsg = (ChangePasswordMessage)message;
					sendOut(new ChangePasswordMessage
					{
						OriginalTransactionId = pwdMsg.TransactionId,
						Error = ex
					});
					break;
				}

				default:
				{
					if (message is ISubscriptionMessage subscrMsg)
						sendOut(subscrMsg.CreateResponse(ex));
					else
						sendOut(ex.ToErrorMessage((message as ITransactionIdMessage)?.TransactionId ?? 0));

					break;
				}
			}
		}

		/// <summary>
		/// Set subscription identifiers into the specified message.
		/// </summary>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <param name="message">Message.</param>
		/// <param name="subscriptionIds">Identifiers.</param>
		/// <param name="subscriptionId">Identifier.</param>
		/// <returns>Message.</returns>
		public static TMessage SetSubscriptionIds<TMessage>(this TMessage message, long[] subscriptionIds = null, long subscriptionId = 0)
				where TMessage : ISubscriptionIdMessage
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (subscriptionId != 0)
			{
				message.SubscriptionId = subscriptionId;
				message.SubscriptionIds = null;
			}
			else if (subscriptionIds != null && subscriptionIds.Length > 0)
			{
				message.SubscriptionId = 0;
				message.SubscriptionIds = subscriptionIds;
			}

			return message;
		}

		/// <summary>
		/// Get subscription identifiers from the specified message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Identifiers.</returns>
		public static long[] GetSubscriptionIds(this ISubscriptionIdMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (message.SubscriptionIds != null)
				return message.SubscriptionIds;
			else if (message.SubscriptionId > 0)
				return new[] { message.SubscriptionId };
			else
				return ArrayHelper.Empty<long>();
		}

		/// <summary>
		/// Determines whether the <paramref name="execMsg"/> contains market-data info.
		/// </summary>
		/// <param name="execMsg">The message contains information about the execution.</param>
		/// <returns>Check result.</returns>
		public static bool IsMarketData(this ExecutionMessage execMsg)
		{
			if (execMsg == null)
				throw new ArgumentNullException(nameof(execMsg));

			return execMsg.ExecutionType == ExecutionTypes.Tick || execMsg.ExecutionType == ExecutionTypes.OrderLog;
		}
		
		/// <summary>
		/// Get <see cref="ExecutionMessage.TradePrice"/>.
		/// </summary>
		/// <param name="message">The message contains information about the execution.</param>
		/// <returns>Trade price.</returns>
		public static decimal GetTradePrice(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var price = message.TradePrice;

			if (price == null)
				throw new ArgumentOutOfRangeException(nameof(message), null, LocalizedStrings.Str1021Params.Put(message.TradeId));

			return price.Value;
		}

		/// <summary>
		/// Get <see cref="ExecutionMessage.Balance"/>.
		/// </summary>
		/// <param name="message">The message contains information about the execution.</param>
		/// <returns>Order contracts balance.</returns>
		public static decimal GetBalance(this ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var balance = message.Balance;

			if (balance != null)
				return balance.Value;

			throw new ArgumentOutOfRangeException(nameof(message));
		}

		/// <summary>
		/// Try get security ID from the specified message.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Security ID or <see langword="null"/> if message do not provide it.</returns>
		public static SecurityId? TryGetSecurityId(this Message message)
		{
			switch (message)
			{
				case ISecurityIdMessage secIdMsg:
					return secIdMsg.SecurityId;
				case INullableSecurityIdMessage nullSecIdMsg:
					return nullSecIdMsg.SecurityId;
				default:
					return null;
			}
		}

		/// <summary>
		/// Replace security id by the specified.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <param name="securityId">Security ID.</param>
		public static void ReplaceSecurityId(this Message message, SecurityId securityId)
		{
			switch (message)
			{
				case ISecurityIdMessage secIdMsg:
					secIdMsg.SecurityId = securityId;
					break;
				case INullableSecurityIdMessage nullSecIdMsg:
					nullSecIdMsg.SecurityId = securityId;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.Str2770);
			}
		}

		/// <summary>
		/// Support portfolio subscriptions.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Check result.</returns>
		public static bool IsSupportSubscriptionByPortfolio(this IMessageAdapter adapter)
			=> adapter.IsMessageSupported(MessageTypes.Portfolio);

		/// <summary>
		/// Determines the specified message contains <see cref="SecurityId.Money"/> position.
		/// </summary>
		/// <param name="posMsg">The message contains information about the position changes.</param>
		/// <returns>Check result.</returns>
		public static bool IsMoney(this PositionChangeMessage posMsg)
		{
			if (posMsg == null)
				throw new ArgumentNullException(nameof(posMsg));

			return posMsg.SecurityId == SecurityId.Money;
		}

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <remarks>
		/// If the connection does not support the order type <see cref="OrderTypes.Conditional"/>, it will be returned <see langword="null" />.
		/// </remarks>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Order condition.</returns>
		public static OrderCondition CreateOrderCondition(this IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.OrderConditionType?.CreateOrderCondition();
		}

		/// <summary>
		/// Create condition for order type <see cref="OrderTypes.Conditional"/>, that supports the adapter.
		/// </summary>
		/// <param name="orderConditionType">Type of <see cref="OrderCondition"/>.</param>
		/// <returns>Order condition.</returns>
		public static OrderCondition CreateOrderCondition(this Type orderConditionType)
		{
			return orderConditionType?.CreateInstance<OrderCondition>();
		}

		/// <summary>
		/// Support lookup all securities.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <returns>Check result.</returns>
		public static bool IsSupportSecuritiesLookupAll(this IMessageAdapter adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			return adapter.IsAllDownloadingSupported(DataType.Securities);
		}

		/// <summary>
		/// Determines the specified message contains single order request.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Check result.</returns>
		public static bool HasOrderId(this OrderStatusMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.OrderId != null || !message.OrderStringId.IsEmpty();
		}

		/// <summary>
		/// Made the specified message as <see cref="Message.BackMode"/>.
		/// </summary>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <param name="message">Message.</param>
		/// <param name="adapter">Adapter.</param>
		/// <param name="mode">Back mode.</param>
		/// <returns>Message.</returns>
		public static TMessage LoopBack<TMessage>(this TMessage message, IMessageAdapter adapter, MessageBackModes mode = MessageBackModes.Direct)
			where TMessage : IMessage
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.BackMode = mode;
			message.Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

			return message;
		}

		/// <summary>
		/// Undo operation made via <see cref="LoopBack{TMessage}"/>.
		/// </summary>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <param name="message">Message.</param>
		/// <returns>Message.</returns>
		public static TMessage UndoBack<TMessage>(this TMessage message)
			where TMessage : IMessage
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			message.BackMode = MessageBackModes.None;
			message.Adapter = null;

			return message;
		}

		/// <summary>
		/// Determines the specified message is loopback.
		/// </summary>
		/// <param name="message">Message.</param>
		/// <returns>Is loopback message.</returns>
		public static bool IsBack(this IMessage message)
		{
			if (message is null)
				throw new ArgumentNullException(nameof(message));

			return message.BackMode != MessageBackModes.None;
		}

		/// <summary>
		/// Get maximum size step allowed for historical download.
		/// </summary>
		/// <param name="adapter">Trading system adapter.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="iterationInterval">Interval between iterations.</param>
		/// <returns>Step.</returns>
		public static TimeSpan GetHistoryStepSize(this IMessageAdapter adapter, DataType dataType, out TimeSpan iterationInterval)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			iterationInterval = TimeSpan.FromSeconds(2);

			if (dataType.IsCandles)
			{
				var supportedCandles = adapter.SupportedMarketDataTypes.FirstOrDefault(d => d.MessageType == dataType.MessageType);

				if (supportedCandles == null)
					return TimeSpan.Zero;

				if (dataType.MessageType == typeof(TimeFrameCandleMessage))
				{
					var tf = (TimeSpan)dataType.Arg;

					if (!adapter.CheckTimeFrameByRequest && !adapter.GetTimeFrames().Contains(tf))
						return TimeSpan.Zero;

					if (tf.TotalDays <= 1)
					{
						if (tf.TotalMinutes < 0.1)
							return TimeSpan.FromHours(0.5);

						return TimeSpan.FromDays(30);
					}

					return TimeSpan.MaxValue;
				}
			}

			// by default adapter do not provide historical data except candles
			return TimeSpan.Zero;
		}

		/// <summary>
		/// Determines the specified type is crypto currency.
		/// </summary>
		/// <param name="type">Currency type.</param>
		/// <returns>Check result.</returns>
		public static bool IsCrypto(this CurrencyTypes type) => type.GetAttributeOfType<CryptoAttribute>() != null;

		/// <summary>
		/// StockSharp news source.
		/// </summary>
		public const string NewsStockSharpSource = nameof(StockSharp);

		/// <summary>
		/// Determines whether the specified news related with StockSharp.
		/// </summary>
		/// <param name="news">News.</param>
		/// <returns>Check result.</returns>
		public static bool IsStockSharp(this NewsMessage news)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			return news.Source.CompareIgnoreCase(NewsStockSharpSource);
		}

		/// <summary>
		/// Make news related with StockSharp.
		/// </summary>
		/// <param name="news">News.</param>
		/// <returns>News.</returns>
		public static NewsMessage MakeAsStockSharp(this NewsMessage news)
		{
			if (news == null)
				throw new ArgumentNullException(nameof(news));

			if (!news.Story.IsEmpty())
				throw new ArgumentException(nameof(news));

			news.Story = NewsStockSharpSource;
			return news;
		}

		/// <summary>
		/// Simulator.
		/// </summary>
		public static string SimulatorPortfolioName = "Simulator (S#)";

		/// <summary>
		/// Anonymous account.
		/// </summary>
		public static string AnonymousPortfolioName = "Anonymous (S#)";

		/// <summary>
		/// Convert key to numeric identifier.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>Identifier.</returns>
		public static int? ToId(this SecureString key)
		{
			return key?.UnSecure().GetDeterministicHashCode();
		}

		private class TickEnumerable : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
		{
			private class TickEnumerator : IEnumerator<ExecutionMessage>
			{
				private readonly IEnumerator<Level1ChangeMessage> _level1Enumerator;

				public TickEnumerator(IEnumerator<Level1ChangeMessage> level1Enumerator)
				{
					_level1Enumerator = level1Enumerator ?? throw new ArgumentNullException(nameof(level1Enumerator));
				}

				public ExecutionMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_level1Enumerator.MoveNext())
					{
						var level1 = _level1Enumerator.Current;

						if (!level1.IsContainsTick())
							continue;

						Current = level1.ToTick();
						return true;
					}

					Current = null;
					return false;
				}

				public void Reset()
				{
					_level1Enumerator.Reset();
					Current = null;
				}

				object IEnumerator.Current => Current;

				void IDisposable.Dispose()
				{
					Current = null;
					_level1Enumerator.Dispose();
				}
			}

			//private readonly IEnumerable<Level1ChangeMessage> _level1;

			public TickEnumerable(IEnumerable<Level1ChangeMessage> level1)
				: base(() => new TickEnumerator(level1.GetEnumerator()))
			{
				if (level1 == null)
					throw new ArgumentNullException(nameof(level1));

				//_level1 = level1;
			}

			//int IEnumerableEx.Count => _level1.Count;
		}

		/// <summary>
		/// To convert level1 data into tick data.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>Tick data.</returns>
		public static IEnumerable<ExecutionMessage> ToTicks(this IEnumerable<Level1ChangeMessage> level1)
		{
			return new TickEnumerable(level1);
		}

		/// <summary>
		/// To check, are there tick data in the level1 data.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>The test result.</returns>
		public static bool IsContainsTick(this Level1ChangeMessage level1)
		{
			if (level1 == null)
				throw new ArgumentNullException(nameof(level1));

			return level1.Changes.ContainsKey(Level1Fields.LastTradePrice);
		}

		/// <summary>
		/// To convert level1 data into tick data.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>Tick data.</returns>
		public static ExecutionMessage ToTick(this Level1ChangeMessage level1)
		{
			if (level1 == null)
				throw new ArgumentNullException(nameof(level1));

			return new ExecutionMessage
			{
				ExecutionType = ExecutionTypes.Tick,
				SecurityId = level1.SecurityId,
				TradeId = (long?)level1.Changes.TryGetValue(Level1Fields.LastTradeId),
				TradePrice = (decimal?)level1.Changes.TryGetValue(Level1Fields.LastTradePrice),
				TradeVolume = (decimal?)level1.Changes.TryGetValue(Level1Fields.LastTradeVolume),
				OriginSide = (Sides?)level1.Changes.TryGetValue(Level1Fields.LastTradeOrigin),
				ServerTime = (DateTimeOffset?)level1.Changes.TryGetValue(Level1Fields.LastTradeTime) ?? level1.ServerTime,
				IsUpTick = (bool?)level1.Changes.TryGetValue(Level1Fields.LastTradeUpDown),
				LocalTime = level1.LocalTime,
			};
		}

		private class OrderBookEnumerable : SimpleEnumerable<QuoteChangeMessage>//, IEnumerableEx<QuoteChangeMessage>
		{
			private class OrderBookEnumerator : IEnumerator<QuoteChangeMessage>
			{
				private readonly IEnumerator<Level1ChangeMessage> _level1Enumerator;

				private decimal? _prevBidPrice;
				private decimal? _prevBidVolume;
				private decimal? _prevAskPrice;
				private decimal? _prevAskVolume;

				public OrderBookEnumerator(IEnumerator<Level1ChangeMessage> level1Enumerator)
				{
					_level1Enumerator = level1Enumerator ?? throw new ArgumentNullException(nameof(level1Enumerator));
				}

				public QuoteChangeMessage Current { get; private set; }

				bool IEnumerator.MoveNext()
				{
					while (_level1Enumerator.MoveNext())
					{
						var level1 = _level1Enumerator.Current;

						if (!level1.IsContainsQuotes())
							continue;

						var prevBidPrice = _prevBidPrice;
						var prevBidVolume = _prevBidVolume;
						var prevAskPrice = _prevAskPrice;
						var prevAskVolume = _prevAskVolume;

						_prevBidPrice = (decimal?)level1.Changes.TryGetValue(Level1Fields.BestBidPrice) ?? _prevBidPrice;
						_prevBidVolume = (decimal?)level1.Changes.TryGetValue(Level1Fields.BestBidVolume) ?? _prevBidVolume;
						_prevAskPrice = (decimal?)level1.Changes.TryGetValue(Level1Fields.BestAskPrice) ?? _prevAskPrice;
						_prevAskVolume = (decimal?)level1.Changes.TryGetValue(Level1Fields.BestAskVolume) ?? _prevAskVolume;

						if (_prevBidPrice == 0)
							_prevBidPrice = null;

						if (_prevAskPrice == 0)
							_prevAskPrice = null;

						if (prevBidPrice == _prevBidPrice && prevBidVolume == _prevBidVolume && prevAskPrice == _prevAskPrice && prevAskVolume == _prevAskVolume)
							continue;

						Current = new QuoteChangeMessage
						{
							SecurityId = level1.SecurityId,
							LocalTime = level1.LocalTime,
							ServerTime = level1.ServerTime,
							Bids = _prevBidPrice == null ? ArrayHelper.Empty<QuoteChange>() : new[] { new QuoteChange(_prevBidPrice.Value, _prevBidVolume ?? 0) },
							Asks = _prevAskPrice == null ? ArrayHelper.Empty<QuoteChange>() : new[] { new QuoteChange(_prevAskPrice.Value, _prevAskVolume ?? 0) },
						};

						return true;
					}

					Current = null;
					return false;
				}

				public void Reset()
				{
					_level1Enumerator.Reset();
					Current = null;
				}

				object IEnumerator.Current => Current;

				void IDisposable.Dispose()
				{
					Current = null;
					_level1Enumerator.Dispose();
				}
			}

			//private readonly IEnumerable<Level1ChangeMessage> _level1;

			public OrderBookEnumerable(IEnumerable<Level1ChangeMessage> level1)
				: base(() => new OrderBookEnumerator(level1.GetEnumerator()))
			{
				if (level1 == null)
					throw new ArgumentNullException(nameof(level1));

				//_level1 = level1;
			}

			//int IEnumerableEx.Count => _level1.Count;
		}

		/// <summary>
		/// To convert level1 data into order books.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>Market depths.</returns>
		public static IEnumerable<QuoteChangeMessage> ToOrderBooks(this IEnumerable<Level1ChangeMessage> level1)
		{
			return new OrderBookEnumerable(level1);
		}

		/// <summary>
		/// To check, are there quotes in the level1.
		/// </summary>
		/// <param name="level1">Level1 data.</param>
		/// <returns>Quotes.</returns>
		public static bool IsContainsQuotes(this Level1ChangeMessage level1)
		{
			if (level1 == null)
				throw new ArgumentNullException(nameof(level1));

			return level1.Changes.ContainsKey(Level1Fields.BestBidPrice) || level1.Changes.ContainsKey(Level1Fields.BestAskPrice);
		}

		/// <summary>
		/// To get the price increment on the basis of accuracy.
		/// </summary>
		/// <param name="decimals">Decimals.</param>
		/// <returns>Price step.</returns>
		public static decimal GetPriceStep(this int decimals)
		{
			return 1m / 10m.Pow(decimals);
		}

		/// <summary>
		/// Check if the specified identifier is <see cref="SecurityId.All"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns><see langword="true"/>, if the specified identifier is <see cref="SecurityId.All"/>, otherwise, <see langword="false"/>.</returns>
		public static bool IsAllSecurity(this SecurityId securityId)
		{
			//if (security == null)
			//	throw new ArgumentNullException(nameof(security));

			return securityId.SecurityCode.CompareIgnoreCase(SecurityId.AssociatedBoardCode) && securityId.BoardCode.CompareIgnoreCase(SecurityId.AssociatedBoardCode);
		}

		/// <summary>
		/// To convert the currency type into the name in the MICEX format.
		/// </summary>
		/// <param name="type">Currency type.</param>
		/// <returns>The currency name in the MICEX format.</returns>
		public static string ToMicexCurrencyName(this CurrencyTypes type)
		{
			switch (type)
			{
				case CurrencyTypes.RUB:
					return "SUR";
				default:
					return type.GetName();
			}
		}

		/// <summary>
		/// To convert the currency name in the MICEX format into <see cref="CurrencyTypes"/>.
		/// </summary>
		/// <param name="name">The currency name in the MICEX format.</param>
		/// <param name="errorHandler">Error handler.</param>
		/// <returns>Currency type. If the value is empty, <see langword="null" /> will be returned.</returns>
		public static CurrencyTypes? FromMicexCurrencyName(this string name, Action<Exception> errorHandler = null)
		{
			if (name.IsEmpty())
				return null;

			switch (name)
			{
				case "SUR":
				case "RUR":
					return CurrencyTypes.RUB;
				case "PLD":
				case "PLT":
				case "GLD":
				case "SLV":
					return null;
				default:
				{
					try
					{
						return name.To<CurrencyTypes>();
					}
					catch (Exception ex)
					{
						if (errorHandler == null)
							ex.LogError();
						else
							errorHandler.Invoke(ex);

						return null;
					}
				}
			}
		}

		/// <summary>
		/// To get the instrument description by the class.
		/// </summary>
		/// <param name="securityClassInfo">Description of the class of securities, depending on which will be marked in the <see cref="SecurityMessage.SecurityType"/> and <see cref="SecurityId.BoardCode"/>.</param>
		/// <param name="secClass">Security class.</param>
		/// <returns>The instrument description. If the class is not found, then empty value is returned as instrument type.</returns>
		public static Tuple<SecurityTypes?, string> GetSecurityClassInfo(this IDictionary<string, RefPair<SecurityTypes, string>> securityClassInfo, string secClass)
		{
			var pair = securityClassInfo.TryGetValue(secClass);
			return Tuple.Create(pair?.First, pair == null ? secClass : pair.Second);
		}

		/// <summary>
		/// To get the board code for the instrument class.
		/// </summary>
		/// <param name="adapter">Adapter to the trading system.</param>
		/// <param name="secClass">Security class.</param>
		/// <returns>Board code.</returns>
		public static string GetBoardCode(this IMessageAdapter adapter, string secClass)
		{
			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			if (secClass.IsEmpty())
				throw new ArgumentNullException(nameof(secClass));

			return adapter.SecurityClassInfo.GetSecurityClassInfo(secClass).Item2;
		}

		/// <summary>
		/// Convert <see cref="Level1Fields"/> to <see cref="Type"/> value.
		/// </summary>
		/// <param name="field"><see cref="Level1Fields"/> value.</param>
		/// <returns><see cref="Type"/> value.</returns>
		public static Type ToType(this Level1Fields field)
		{
			switch (field)
			{
				case Level1Fields.AsksCount:
				case Level1Fields.BidsCount:
				case Level1Fields.TradesCount:
				case Level1Fields.Decimals:
					return typeof(int);

				case Level1Fields.LastTradeId:
					return typeof(long);

				case Level1Fields.BestAskTime:
				case Level1Fields.BestBidTime:
				case Level1Fields.LastTradeTime:
				case Level1Fields.BuyBackDate:
				case Level1Fields.CouponDate:
					return typeof(DateTimeOffset);

				case Level1Fields.LastTradeUpDown:
				case Level1Fields.IsSystem:
					return typeof(bool);

				case Level1Fields.State:
					return typeof(SecurityStates);

				case Level1Fields.LastTradeOrigin:
					return typeof(Sides);

				default:
					return field.IsObsolete() ? null : typeof(decimal);
			}
		}

		/// <summary>
		/// Convert <see cref="PositionChangeTypes"/> to <see cref="Type"/> value.
		/// </summary>
		/// <param name="type"><see cref="PositionChangeTypes"/> value.</param>
		/// <returns><see cref="Type"/> value.</returns>
		public static Type ToType(this PositionChangeTypes type)
		{
			switch (type)
			{
				case PositionChangeTypes.ExpirationDate:
					return typeof(DateTimeOffset);

				case PositionChangeTypes.State:
					return typeof(PortfolioStates);

				case PositionChangeTypes.Currency:
					return typeof(CurrencyTypes);

				case PositionChangeTypes.BuyOrdersCount:
				case PositionChangeTypes.SellOrdersCount:
				case PositionChangeTypes.OrdersCount:
				case PositionChangeTypes.TradesCount:
					return typeof(int);

				default:
					return type.IsObsolete() ? null : typeof(decimal);
			}
		}

		/// <summary>
		/// Convert <see cref="QuoteChangeMessage"/> to <see cref="Level1ChangeMessage"/> value.
		/// </summary>
		/// <param name="message"><see cref="QuoteChangeMessage"/> instance.</param>
		/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
		public static Level1ChangeMessage ToLevel1(this QuoteChangeMessage message)
		{
			var bestBid = message.GetBestBid();
			var bestAsk = message.GetBestAsk();

			var level1 = new Level1ChangeMessage
			{
				SecurityId = message.SecurityId,
				ServerTime = message.ServerTime,
			};

			if (bestBid != null)
			{
				level1.Add(Level1Fields.BestBidPrice, bestBid.Price);
				level1.Add(Level1Fields.BestBidVolume, bestBid.Volume);
			}

			if (bestAsk != null)
			{
				level1.Add(Level1Fields.BestAskPrice, bestAsk.Price);
				level1.Add(Level1Fields.BestAskVolume, bestAsk.Volume);
			}

			return level1;
		}

		/// <summary>
		/// Convert <see cref="CandleMessage"/> to <see cref="Level1ChangeMessage"/> value.
		/// </summary>
		/// <param name="message"><see cref="CandleMessage"/> instance.</param>
		/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
		public static Level1ChangeMessage ToLevel1(this CandleMessage message)
		{
			var level1 = new Level1ChangeMessage
			{
				SecurityId = message.SecurityId,
				ServerTime = message.OpenTime,
			}
			.Add(Level1Fields.OpenPrice, message.OpenPrice)
			.Add(Level1Fields.HighPrice, message.HighPrice)
			.Add(Level1Fields.LowPrice, message.LowPrice)
			.Add(Level1Fields.ClosePrice, message.ClosePrice)
			.Add(Level1Fields.Volume, message.TotalVolume)
			.TryAdd(Level1Fields.OpenInterest, message.OpenInterest, true);

			return level1;
		}

		/// <summary>
		/// Convert <see cref="ExecutionMessage"/> to <see cref="Level1ChangeMessage"/> value.
		/// </summary>
		/// <param name="message"><see cref="ExecutionMessage"/> instance.</param>
		/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
		public static Level1ChangeMessage ToLevel1(this ExecutionMessage message)
		{
			var level1 = new Level1ChangeMessage
			{
				SecurityId = message.SecurityId,
				ServerTime = message.ServerTime,
			}
			.TryAdd(Level1Fields.LastTradeId, message.TradeId)
			.TryAdd(Level1Fields.LastTradePrice, message.TradePrice)
			.TryAdd(Level1Fields.LastTradeVolume, message.TradeVolume)
			.TryAdd(Level1Fields.OpenInterest, message.OpenInterest, true)
			.TryAdd(Level1Fields.LastTradeOrigin, message.OriginSide);

			return level1;
		}

		/// <summary>
		/// To build level1 from the order books.
		/// </summary>
		/// <param name="quotes">Order books.</param>
		/// <returns>Level1.</returns>
		public static IEnumerable<Level1ChangeMessage> ToLevel1(this IEnumerable<QuoteChangeMessage> quotes)
		{
			if (quotes is null)
				throw new ArgumentNullException(nameof(quotes));

			foreach (var quote in quotes)
			{
				var l1Msg = new Level1ChangeMessage
				{
					SecurityId = quote.SecurityId,
					ServerTime = quote.ServerTime,
				};

				if (quote.Bids.Length > 0)
				{
					l1Msg
						.TryAdd(Level1Fields.BestBidPrice, quote.Bids[0].Price)
						.TryAdd(Level1Fields.BestBidVolume, quote.Bids[0].Volume);
				}

				if (quote.Asks.Length > 0)
				{
					l1Msg
						.TryAdd(Level1Fields.BestAskPrice, quote.Asks[0].Price)
						.TryAdd(Level1Fields.BestAskVolume, quote.Asks[0].Volume);
				}

				yield return l1Msg;
			}
		}

		/// <summary>
		/// Extract time frames from the specified data types set.
		/// </summary>
		/// <param name="dataTypes">Data types.</param>
		/// <returns>Possible time-frames.</returns>
		public static IEnumerable<TimeSpan> FilterTimeFrames(this IEnumerable<DataType> dataTypes)
			=> dataTypes.Where(t => t.MessageType == typeof(TimeFrameCandleMessage) && t.Arg != null).Select(t => (TimeSpan)t.Arg);

		/// <summary>
		/// To determine whether the order book is in the right state.
		/// </summary>
		/// <param name="book">Order book.</param>
		/// <returns><see langword="true" />, if the order book contains correct data, otherwise <see langword="false" />.</returns>
		/// <remarks>
		/// It is used in cases when the trading system by mistake sends the wrong quotes.
		/// </remarks>
		public static bool Verify(this QuoteChangeMessage book)
		{
			if (book is null)
				throw new ArgumentNullException(nameof(book));

			var bids = book.IsSorted ? book.Bids : book.Bids.OrderByDescending(b => b.Price).ToArray();
			var asks = book.IsSorted ? book.Asks : book.Asks.OrderBy(b => b.Price).ToArray();

			var bestBid = bids.FirstOrDefault();
			var bestAsk = asks.FirstOrDefault();

			if (bestBid != null && bestAsk != null)
			{
				return bids.All(b => b.Price < bestAsk.Price) && asks.All(a => a.Price > bestBid.Price) && Verify(bids, true) && Verify(asks, false);
			}
			else
			{
				return Verify(bids, true) && Verify(asks, false);
			}
		}

		private static bool Verify(QuoteChange[] quotes, bool isBids)
		{
			if (quotes.IsEmpty())
				return true;

			if (quotes.Any(q => !Verify(q)))
				return false;

			if (quotes.GroupBy(q => q.Price).Any(g => g.Count() > 1))
				return false;

			var prev = quotes.First();

			foreach (var current in quotes.Skip(1))
			{
				if (isBids)
				{
					if (current.Price > prev.Price)
						return false;
				}
				else
				{
					if (current.Price < prev.Price)
						return false;
				}

				prev = current;
			}

			return true;
		}

		private static bool Verify(QuoteChange quote)
		{
			if (quote is null)
				throw new ArgumentNullException(nameof(quote));

			return quote.Price > 0 && quote.Volume > 0;
		}

		/// <summary>
		/// Determines the specified security is matched lookup criteria.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="criteria">Message security lookup for specified criteria.</param>
		/// <returns>Check result.</returns>
		public static bool IsMatch(this SecurityMessage security, SecurityLookupMessage criteria)
		{
			return security.IsMatch(criteria, criteria.GetSecurityTypes());
		}

		/// <summary>
		/// Determines the specified security is matched lookup criteria.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="criteria">Message security lookup for specified criteria.</param>
		/// <param name="secTypes">Securities types.</param>
		/// <returns>Check result.</returns>
		public static bool IsMatch(this SecurityMessage security, SecurityLookupMessage criteria, HashSet<SecurityTypes> secTypes)
		{
			var secId = criteria.SecurityId;

			if (!secId.SecurityCode.IsEmpty() && !security.SecurityId.SecurityCode.ContainsIgnoreCase(secId.SecurityCode))
				return false;

			if (!secId.BoardCode.IsEmpty() && !security.SecurityId.BoardCode.CompareIgnoreCase(secId.BoardCode))
				return false;

			// sec + board codes means exact id
			if (!secId.SecurityCode.IsEmpty() && !secId.BoardCode.IsEmpty())
			{
				if (security.SecurityId != secId)
					return false;
			}

			if (secTypes.Count > 0)
			{
				if (security.SecurityType == null || !secTypes.Contains(security.SecurityType.Value))
					return false;
			}

			if (criteria.SecurityIds.Length > 0)
			{
				if (!criteria.SecurityIds.Contains(security.SecurityId))
					return false;
			}

			if (!secId.Bloomberg.IsEmptyOrWhiteSpace() && !security.SecurityId.Bloomberg.ContainsIgnoreCase(secId.Bloomberg))
				return false;

			if (!secId.Cusip.IsEmptyOrWhiteSpace() && !security.SecurityId.Cusip.ContainsIgnoreCase(secId.Cusip))
				return false;

			if (!secId.IQFeed.IsEmptyOrWhiteSpace() && !security.SecurityId.IQFeed.ContainsIgnoreCase(secId.IQFeed))
				return false;

			if (!secId.Isin.IsEmptyOrWhiteSpace() && !security.SecurityId.Isin.ContainsIgnoreCase(secId.Isin))
				return false;

			if (!secId.Ric.IsEmptyOrWhiteSpace() && !security.SecurityId.Ric.ContainsIgnoreCase(secId.Ric))
				return false;

			if (!secId.Sedol.IsEmptyOrWhiteSpace() && !security.SecurityId.Sedol.ContainsIgnoreCase(secId.Sedol))
				return false;

			if (!criteria.Name.IsEmptyOrWhiteSpace() && !security.Name.ContainsIgnoreCase(criteria.Name))
				return false;

			if (!criteria.ShortName.IsEmptyOrWhiteSpace() && !security.ShortName.ContainsIgnoreCase(criteria.ShortName))
				return false;

			if (criteria.VolumeStep != null && security.VolumeStep != criteria.VolumeStep)
				return false;

			if (criteria.MinVolume != null && security.MinVolume != criteria.MinVolume)
				return false;

			if (criteria.MaxVolume != null && security.MaxVolume != criteria.MaxVolume)
				return false;

			if (criteria.Multiplier != null && security.Multiplier != criteria.Multiplier)
				return false;

			if (criteria.Decimals != null && security.Decimals != criteria.Decimals)
				return false;

			if (criteria.PriceStep != null && security.PriceStep != criteria.PriceStep)
				return false;

			if (!criteria.CfiCode.IsEmptyOrWhiteSpace() && !security.CfiCode.ContainsIgnoreCase(criteria.CfiCode))
				return false;

			if (criteria.ExpiryDate != null && security.ExpiryDate != criteria.ExpiryDate)
				return false;

			if (criteria.SettlementDate != null && security.SettlementDate != criteria.SettlementDate)
				return false;

			if (!criteria.UnderlyingSecurityCode.IsEmpty() && !security.UnderlyingSecurityCode.ContainsIgnoreCase(criteria.UnderlyingSecurityCode))
				return false;

			if (criteria.UnderlyingSecurityMinVolume != null && security.UnderlyingSecurityMinVolume != criteria.UnderlyingSecurityMinVolume)
				return false;

			if (criteria.Strike != null && security.Strike != criteria.Strike)
				return false;

			if (criteria.OptionType != null && security.OptionType != criteria.OptionType)
				return false;

			if (!criteria.BinaryOptionType.IsEmptyOrWhiteSpace() && !security.BinaryOptionType.ContainsIgnoreCase(criteria.BinaryOptionType))
				return false;

			if (criteria.Currency != null && security.Currency != criteria.Currency)
				return false;

			if (!criteria.Class.IsEmptyOrWhiteSpace() && !security.Class.ContainsIgnoreCase(criteria.Class))
				return false;

			if (criteria.IssueSize != null && security.IssueSize != criteria.IssueSize)
				return false;

			if (criteria.IssueDate != null && security.IssueDate != criteria.IssueDate)
				return false;

			if (criteria.UnderlyingSecurityType != null && security.UnderlyingSecurityType != criteria.UnderlyingSecurityType)
				return false;

			if (criteria.Shortable != null && security.Shortable != criteria.Shortable)
				return false;

			if (!criteria.BasketCode.IsEmptyOrWhiteSpace() && !security.BasketCode.ContainsIgnoreCase(criteria.BasketCode))
				return false;

			if (!criteria.BasketExpression.IsEmptyOrWhiteSpace() && !security.BasketExpression.CompareIgnoreCase(criteria.BasketExpression))
				return false;

			if (criteria.FaceValue != default && security.FaceValue != criteria.FaceValue)
				return false;

			if (criteria.PrimaryId != default && security.PrimaryId != criteria.PrimaryId)
				return false;

			return true;
		}

		/// <summary>
		/// To filter instruments by the given criteria.
		/// </summary>
		/// <param name="securities">Securities.</param>
		/// <param name="criteria">Message security lookup for specified criteria.</param>
		/// <returns>Instruments filtered.</returns>
		public static IEnumerable<SecurityMessage> Filter(this IEnumerable<SecurityMessage> securities, SecurityLookupMessage criteria)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			if (criteria.IsLookupAll())
				return securities.ToArray();

			var secTypes = criteria.GetSecurityTypes();

			var result = securities.Where(s => s.IsMatch(criteria, secTypes));

			if (criteria.Count != null)
				result = result.Take(criteria.Count.Value);

			return result.ToArray();
		}

		/// <summary>
		/// "All securities" instance.
		/// </summary>
		public static SecurityMessage AllSecurity { get; } = new SecurityMessage();

		/// <summary>
		/// Lookup all securities predefined criteria.
		/// </summary>
		public static readonly SecurityLookupMessage LookupAllCriteriaMessage = new SecurityLookupMessage();

		/// <summary>
		/// Determine the <paramref name="criteria"/> contains lookup all filter.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Check result.</returns>
		public static bool IsLookupAll(this SecurityLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (criteria == LookupAllCriteriaMessage)
				return true;

			return
				criteria.SecurityId.IsDefault() &&
				criteria.Name.IsEmpty() &&
				criteria.ShortName.IsEmpty() &&
				criteria.VolumeStep == null &&
				criteria.MinVolume == null &&
				criteria.MaxVolume == null &&
				criteria.Multiplier == null &&
				criteria.Decimals == null &&
				criteria.PriceStep == null &&
				criteria.SecurityType == null &&
				criteria.CfiCode.IsEmpty() &&
				criteria.ExpiryDate == null &&
				criteria.SettlementDate == null &&
				criteria.UnderlyingSecurityCode.IsEmpty() &&
				criteria.UnderlyingSecurityMinVolume == null &&
				criteria.Strike == null &&
				criteria.OptionType == null &&
				criteria.BinaryOptionType.IsEmpty() &&
				criteria.Currency == null &&
				criteria.Class.IsEmpty() &&
				criteria.IssueSize == null &&
				criteria.IssueDate == null &&
				criteria.UnderlyingSecurityType == null &&
				criteria.Shortable == null &&
				criteria.BasketCode.IsEmpty() &&
				criteria.BasketExpression.IsEmpty() &&
				criteria.FaceValue == null &&
				criteria.PrimaryId == default &&
				(criteria.SecurityTypes == null || criteria.SecurityTypes.Length == 0) &&
				criteria.Count == null &&
				criteria.SecurityIds.Length == 0;
		}

		/// <summary>
		/// Change <see cref="IMessageAdapter.SupportedInMessages"/>.
		/// </summary>
		/// <param name="adapter">Adapter.</param>
		/// <param name="add">Command.</param>
		/// <param name="isMarketData">Message types.</param>
		public static void ChangeSupported(this IMessageAdapter adapter, bool add, bool isMarketData)
		{
			if (adapter is null)
				throw new ArgumentNullException(nameof(adapter));

			var types = adapter.PossibleSupportedMessages.Where(i => i.IsMarketData == isMarketData).Select(i => i.Type);

			var supported = add
				? adapter.SupportedInMessages.Concat(types)
				: adapter.SupportedInMessages.Except(types);

			adapter.SupportedInMessages = supported.Distinct().ToArray();
		}

		private static readonly SynchronizedDictionary<DataType, MessageTypes> _messageTypes = new SynchronizedDictionary<DataType, MessageTypes>();

		/// <summary>
		/// Convert <see cref="DataType"/> to <see cref="MessageTypes"/> value.
		/// </summary>
		/// <param name="type"><see cref="DataType"/> value.</param>
		/// <returns>Message type.</returns>
		public static MessageTypes ToMessageType2(this DataType type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (type == DataType.Level1)
				return MessageTypes.Level1Change;
			else if (type == DataType.MarketDepth)
				return MessageTypes.QuoteChange;
			else if (type == DataType.Ticks || type == DataType.OrderLog || type == DataType.Transactions)
				return MessageTypes.Execution;
			else if (type == DataType.News)
				return MessageTypes.News;
			else if (type == DataType.Board)
				return MessageTypes.BoardState;
			else if (type == DataType.Securities)
				return MessageTypes.Security;
			else if (type == DataType.SecurityLegs)
				return MessageTypes.SecurityLegsInfo;
			else if (type == DataType.SecurityRoute)
				return MessageTypes.SecurityRoute;
			else if (type == DataType.TimeFrames)
				return MessageTypes.TimeFrameInfo;
			else if (type.IsCandles)
				return type.MessageType.ToMessageType();
			else 
			{
				return _messageTypes.SafeAdd(type, key => key.MessageType.CreateInstance<Message>().Type);
				//throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);
			}
		}

		/// <summary>
		/// Is channel opened.
		/// </summary>
		/// <param name="channel">Message channel.</param>
		/// <returns>Check result.</returns>
		public static bool IsOpened(this IMessageChannel channel)
		{
			if (channel is null)
				throw new ArgumentNullException(nameof(channel));

			return channel.State != ChannelStates.Stopped;
		}
	}
}