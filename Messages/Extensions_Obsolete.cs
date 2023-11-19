namespace StockSharp.Messages
{
	using System;

	using Ecng.Collections;

	using StockSharp.Localization;

	partial class Extensions
	{
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
		/// <param name="type">Market data type.</param>
		/// <param name="arg">The additional argument, associated with data. For example, candle argument.</param>
		/// <returns>Message type.</returns>
		[Obsolete]
		public static MessageTypes ToMessageType(this MarketDataTypes type, out object arg)
		{
			if (!_messageTypeMapOld.TryGetValue(type, out var tuple))
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);

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
			var msgType = type.ToMessageType(out var arg2);
			arg = arg2 ?? arg;

			switch (msgType)
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
					if (msgType.IsCandle())
						return DataType.Create(msgType.ToCandleMessage(), arg);

					throw new ArgumentOutOfRangeException(nameof(msgType), msgType, LocalizedStrings.InvalidValue);
				}
			}
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
			{
				var msgType = dataType.MessageType.ToMessageType();

				if (_messageTypeMapOld.TryGetKey((msgType, default), out var dataType2))
					return dataType2;

				throw new ArgumentOutOfRangeException(nameof(msgType), msgType, LocalizedStrings.InvalidValue);
			}
			else if (dataType == DataType.FilteredMarketDepth)
				return MarketDataTypes.MarketDepth;
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
		}
	}
}