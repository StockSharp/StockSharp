namespace StockSharp.Messages;

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
	[Obsolete("Use DataType class.")]
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
	[Obsolete("Use DataType class.")]
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

			case MessageTypes.DataTypeInfo:
				return DataType.DataTypeInfo;

			case MessageTypes.UserInfo:
				return DataType.Users;

			default:
			{
				if (msgType.IsCandle())
					return DataType.Create(msgType.ToCandleMessage(), arg);

				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
			}
		}
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to <see cref="MarketDataTypes"/> value.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <returns><see cref="MarketDataTypes"/> value or <see langword="null"/> if cannot be converted.</returns>
	[Obsolete("Use DataType class.")]
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

			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
		}
		else if (dataType == DataType.FilteredMarketDepth)
			return MarketDataTypes.MarketDepth;
		else
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
	}

	/// <summary>
	/// To group the order book by the price range.
	/// </summary>
	/// <param name="depth">The order book to be grouped.</param>
	/// <param name="priceRange">The price range, for which grouping shall be performed.</param>
	/// <returns>The grouped order book.</returns>
	[Obsolete("Use method with decimal priceRange parameter")]
	public static QuoteChangeMessage Group(this IOrderBookMessage depth, Unit priceRange)
		=> depth.Group(GetActualPriceRange(priceRange));

	/// <summary>
	/// </summary>
	[Obsolete("Use method with decimal priceRange parameter")]
	public static QuoteChangeMessage Sparse(this IOrderBookMessage depth, Unit priceRange, decimal? priceStep)
		=> depth.Sparse(GetActualPriceRange(priceRange), priceStep);

	/// <summary>
	/// </summary>
	[Obsolete("Use method with decimal priceRange parameter")]
	public static (QuoteChange[] bids, QuoteChange[] asks) Sparse(this QuoteChange bid, QuoteChange ask, Unit priceRange, decimal? priceStep)
		=> bid.Sparse(ask, GetActualPriceRange(priceRange), priceStep);

	/// <summary>
	/// To create the sparse collection of quotes from regular quotes.
	/// </summary>
	/// <remarks>
	/// In sparsed collection shown quotes with no active orders. The volume of these quotes is 0.
	/// </remarks>
	/// <param name="quotes">Regular quotes. The collection shall contain quotes of the same direction (only bids or only offers).</param>
	/// <param name="side">Side.</param>
	/// <param name="priceRange">Minimum price step.</param>
	/// <param name="priceStep">Security price step.</param>
	/// <returns>The sparse collection of quotes.</returns>
	[Obsolete("Use method with decimal priceRange parameter")]
	public static QuoteChange[] Sparse(this QuoteChange[] quotes, Sides side, Unit priceRange, decimal? priceStep)
	{
		if (quotes is null)
			throw new ArgumentNullException(nameof(quotes));

		return quotes.Sparse(side, GetActualPriceRange(priceRange), priceStep);
	}

	/// <summary>
	/// </summary>
	[Obsolete("Use method with decimal priceRange")]
	public static QuoteChange[] Group(this QuoteChange[] quotes, Sides side, Unit priceRange)
	{
		if (quotes is null)
			throw new ArgumentNullException(nameof(quotes));

		return quotes.Group(side, GetActualPriceRange(priceRange));
	}
}