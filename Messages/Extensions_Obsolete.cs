namespace StockSharp.Messages
{
	using System;

	using Ecng.Collections;

	using StockSharp.Localization;

	partial class Extensions
	{
		/// <summary>
		/// Convert <see cref="MarketDataMessage"/> to <see cref="DataType"/> value.
		/// </summary>
		/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
		/// <returns>Data type info.</returns>
		[Obsolete("Use MarketDataMessage.DataType2 property.")]
		public static DataType ToDataType(this MarketDataMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			return message.DataType.ToDataType(message.Arg);
		}

		/// <summary>
		/// Cast message type <see cref="CandleMessage"/> to the <see cref="MarketDataTypes"/>.
		/// </summary>
		/// <param name="messageType">The type of the message <see cref="CandleMessage"/>.</param>
		/// <returns><see cref="MarketDataTypes"/>.</returns>
		[Obsolete]
		public static MarketDataTypes ToCandleMarketDataType(this Type messageType)
		{
			return messageType.ToMessageType().ToMarketDataType(null);
		}

		[Obsolete]
		private static readonly SynchronizedPairSet<MarketDataTypes, Tuple<MessageTypes, object>> _messageTypeMapOld = new SynchronizedPairSet<MarketDataTypes, Tuple<MessageTypes, object>>
		{
			{ MarketDataTypes.Level1, Tuple.Create(MessageTypes.Level1Change, default(object)) },
			{ MarketDataTypes.MarketDepth, Tuple.Create(MessageTypes.QuoteChange, default(object)) },
			{ MarketDataTypes.Trades, Tuple.Create(MessageTypes.Execution, (object)ExecutionTypes.Tick) },
			{ MarketDataTypes.OrderLog, Tuple.Create(MessageTypes.Execution, (object)ExecutionTypes.OrderLog) },
			{ MarketDataTypes.News, Tuple.Create(MessageTypes.News, default(object)) },
			{ MarketDataTypes.Board, Tuple.Create(MessageTypes.Board, default(object)) },
		};

		/// <summary>
		/// Convert <see cref="MarketDataTypes"/> to <see cref="MessageTypes"/> value.
		/// </summary>
		/// <param name="type">Message type.</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <returns>Market data type.</returns>
		[Obsolete]
		public static MarketDataTypes ToMarketDataType(this MessageTypes type, object arg)
		{
			if (_messageTypeMapOld.TryGetKey(Tuple.Create(type, arg), out var dataType))
				return dataType;

			throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);
		}

		/// <summary>
		/// Convert <see cref="MarketDataTypes"/> to <see cref="MessageTypes"/> value.
		/// </summary>
		/// <param name="type">Market data type.</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <returns>Message type.</returns>
		[Obsolete]
		public static MessageTypes ToMessageType(this MarketDataTypes type, out object arg)
		{
			if (!_messageTypeMapOld.TryGetValue(type, out var tuple))
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);

			arg = tuple.Item2;
			return tuple.Item1;
		}

		/// <summary>
		/// Convert <see cref="MarketDataTypes"/> to <see cref="DataType"/> value.
		/// </summary>
		/// <param name="type">Market data type.</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <returns>Data type info.</returns>
		[Obsolete]
		public static DataType ToDataType(this MarketDataTypes type, object arg)
		{
			return type.ToMessageType(out var arg2).ToDataType(arg2 ?? arg);
		}

		/// <summary>
		/// Convert <see cref="DataType"/> to <see cref="MarketDataTypes"/> value.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns><see cref="MarketDataTypes"/> value or <see langword="null"/> if cannot be converted.</returns>
		[Obsolete]
		public static MarketDataTypes ToMarketDataType(this DataType dataType)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			if (dataType == DataType.Ticks)
				return MarketDataTypes.Trades;
			else if (dataType == DataType.Level1)
				return MarketDataTypes.Level1;
			else if (dataType == DataType.OrderLog)
				return MarketDataTypes.OrderLog;
			else if (dataType == DataType.MarketDepth)
				return MarketDataTypes.MarketDepth;
			else if (dataType == DataType.News)
				return MarketDataTypes.News;
			else if (dataType == DataType.Board)
				return MarketDataTypes.Board;
			else if (dataType.IsCandles)
				return dataType.MessageType.ToCandleMarketDataType();
			else if (dataType == DataType.FilteredMarketDepth)
				return MarketDataTypes.MarketDepth;
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1219);
		}

		/// <summary>
		/// Determine the <paramref name="type"/> is candle data type.
		/// </summary>
		/// <param name="type">The data type.</param>
		/// <returns><see langword="true" />, if data type is candle, otherwise, <see langword="false" />.</returns>
		[Obsolete]
		public static bool IsCandleDataType(this MarketDataTypes type) => _candleDataTypes.ContainsKey(type.ToMessageType(out _));

		/// <summary>
		/// Convert <see cref="MessageTypes"/> to <see cref="DataType"/> value.
		/// </summary>
		/// <param name="type">Message type.</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <returns>Data type info.</returns>
		[Obsolete]
		public static DataType ToDataType(this MessageTypes type, object arg)
		{
			switch (type)
			{
				case MessageTypes.Security:
					return DataType.Securities;

				case MessageTypes.Board:
					return DataType.Board;

				case MessageTypes.Portfolio:
				case MessageTypes.PositionChange:
					return DataType.PositionChanges;

				case MessageTypes.News:
					return DataType.News;

				case MessageTypes.BoardState:
					return DataType.BoardState;

				case MessageTypes.Level1Change:
					return DataType.Level1;

				case MessageTypes.QuoteChange:
					return DataType.MarketDepth;

				case MessageTypes.Execution:
					return ((ExecutionTypes)arg).ToDataType();

				case MessageTypes.TimeFrameInfo:
					return DataType.TimeFrames;

				case MessageTypes.UserInfo:
					return DataType.Users;

				default:
				{
					if (type.IsCandle())
						return DataType.Create(type.ToCandleMessage(), arg);

					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);
				}
			}
		}

		/// <summary>
		/// Determines the specified type is lookup.
		/// </summary>
		/// <param name="dataType">Data type info.</param>
		/// <returns>Check result.</returns>
		[Obsolete]
		public static bool IsLookup(this DataType dataType)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			return
				dataType == DataType.Transactions ||
				dataType == DataType.Securities ||
				dataType == DataType.PositionChanges ||
				dataType == DataType.TimeFrames ||
				dataType == DataType.Users ||
				dataType == DataType.Transactions;
		}
	}
}