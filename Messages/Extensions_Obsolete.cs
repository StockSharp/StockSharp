namespace StockSharp.Messages
{
	using System;

	using Ecng.Collections;

	using StockSharp.Localization;

	partial class Extensions
	{
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
		private static readonly SynchronizedPairSet<MarketDataTypes, (MessageTypes, object)> _messageTypeMapOld = new()
		{
			{ MarketDataTypes.Level1, (MessageTypes.Level1Change, default) },
			{ MarketDataTypes.MarketDepth, (MessageTypes.QuoteChange, default) },
			{ MarketDataTypes.Trades, (MessageTypes.Execution, ExecutionTypes.Tick) },
			{ MarketDataTypes.OrderLog, (MessageTypes.Execution, ExecutionTypes.OrderLog) },
			{ MarketDataTypes.News, (MessageTypes.News, default) },
			{ MarketDataTypes.Board, (MessageTypes.Board, default) },
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
			if (_messageTypeMapOld.TryGetKey((type, arg), out var dataType))
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
	}
}