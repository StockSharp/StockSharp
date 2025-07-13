namespace StockSharp.Messages;

using System.Collections;

using Ecng.Reflection;

/// <summary>
/// Extension class.
/// </summary>
public static partial class Extensions
{
	private class StateChangeValidator<T>
	{
		private readonly bool[][] _map;

		private readonly Func<T, int> _converter;

		public StateChangeValidator(Func<T, int> converter)
		{
			_converter = converter ?? throw new ArgumentNullException(nameof(converter));

			_map = new bool[Enumerator.GetValues<T>().Count()][];

			for (var i = 0; i < _map.Length; i++)
				_map[i] = new bool[_map.Length];
		}

		public bool this[T from, T to]
		{
			get => _map[_converter(from)][_converter(to)];
			set => _map[_converter(from)][_converter(to)] = value;
		}
	}

	private static readonly StateChangeValidator<OrderStates> _orderStateValidator;
	private static readonly StateChangeValidator<ChannelStates> _channelStateValidator;

	static Extensions()
	{
		static string TimeSpanToString(TimeSpan arg) => arg.ToString().Replace(':', '-');
		static TimeSpan StringToTimeSpan(string str) => str.Replace('-', ':').To<TimeSpan>();

		static bool validateUnit(Unit v) => v is not null && v.Value > 0;

		RegisterCandleType(typeof(TimeFrameCandleMessage), MessageTypes.CandleTimeFrame, typeof(TimeFrameCandleMessage).Name.Remove(nameof(Message)), StringToTimeSpan, TimeSpanToString, a => a > TimeSpan.Zero, false);
		RegisterCandleType(typeof(TickCandleMessage), MessageTypes.CandleTick, typeof(TickCandleMessage).Name.Remove(nameof(Message)), str => str.To<int>(), arg => arg.ToString(), a => a > 0);
		RegisterCandleType(typeof(VolumeCandleMessage), MessageTypes.CandleVolume, typeof(VolumeCandleMessage).Name.Remove(nameof(Message)), str => str.To<decimal>(), arg => arg.ToString(), a => a > 0);
		RegisterCandleType(typeof(RangeCandleMessage), MessageTypes.CandleRange, typeof(RangeCandleMessage).Name.Remove(nameof(Message)), str => str.ToUnit(), arg => arg.ToString(), validateUnit);
		RegisterCandleType(typeof(RenkoCandleMessage), MessageTypes.CandleRenko, typeof(RenkoCandleMessage).Name.Remove(nameof(Message)), str => str.ToUnit(), arg => arg.ToString(), validateUnit);
		RegisterCandleType(typeof(PnFCandleMessage), MessageTypes.CandlePnF, typeof(PnFCandleMessage).Name.Remove(nameof(Message)), str =>
		{
			var parts = str.Split('_');

			return new PnFArg
			{
				BoxSize = parts[0].ToUnit(),
				ReversalAmount = parts[1].To<int>()
			};
		}, pnf => $"{pnf.BoxSize}_{pnf.ReversalAmount}", a => a is not null && validateUnit(a.BoxSize) && a.ReversalAmount > 0);
		RegisterCandleType(typeof(HeikinAshiCandleMessage), MessageTypes.CandleHeikinAshi, typeof(HeikinAshiCandleMessage).Name.Remove(nameof(Message)), StringToTimeSpan, TimeSpanToString, a => a > TimeSpan.Zero);

		_orderStateValidator = new(s => (int)s);

		_orderStateValidator[OrderStates.None, OrderStates.Pending] = true;
		_orderStateValidator[OrderStates.None, OrderStates.Active] = true;
		_orderStateValidator[OrderStates.None, OrderStates.Done] = true;
		_orderStateValidator[OrderStates.None, OrderStates.Failed] = true;
		_orderStateValidator[OrderStates.Pending, OrderStates.Active] = true;
		_orderStateValidator[OrderStates.Pending, OrderStates.Failed] = true;
		_orderStateValidator[OrderStates.Active, OrderStates.Done] = true;

		_channelStateValidator = new(s => (int)s);

		_channelStateValidator[ChannelStates.Stopped, ChannelStates.Starting] = true;
		_channelStateValidator[ChannelStates.Starting, ChannelStates.Stopped] = true;
		_channelStateValidator[ChannelStates.Starting, ChannelStates.Started] = true;
		_channelStateValidator[ChannelStates.Started, ChannelStates.Stopping] = true;
		_channelStateValidator[ChannelStates.Started, ChannelStates.Suspending] = true;
		_channelStateValidator[ChannelStates.Suspending, ChannelStates.Suspended] = true;
		_channelStateValidator[ChannelStates.Suspended, ChannelStates.Starting] = true;
		_channelStateValidator[ChannelStates.Suspended, ChannelStates.Stopping] = true;
		_channelStateValidator[ChannelStates.Stopping, ChannelStates.Stopped] = true;
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
	public static QuoteChange? GetBestBid(this IOrderBookMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return message.Bids.FirstOr();
	}

	/// <summary>
	/// Get best ask.
	/// </summary>
	/// <param name="message">Market depth.</param>
	/// <returns>Best ask, or <see langword="null" />, if no asks are empty.</returns>
	public static QuoteChange? GetBestAsk(this IOrderBookMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return message.Asks.FirstOr();
	}

	/// <summary>
	/// Get middle of spread.
	/// </summary>
	/// <param name="message">Market depth.</param>
	/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
	/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
	public static decimal? GetSpreadMiddle(this IOrderBookMessage message, decimal? priceStep)
	{
		var bestBid = message.GetBestBid();
		var bestAsk = message.GetBestAsk();

		return (bestBid?.Price).GetSpreadMiddle(bestAsk?.Price, priceStep);
	}

	/// <summary>
	/// Get price by side.
	/// </summary>
	/// <param name="message"><see cref="IOrderBookMessage"/></param>
	/// <param name="side"><see cref="Sides"/></param>
	/// <returns>Price.</returns>
	public static decimal? GetPrice(this IOrderBookMessage message, Sides? side)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return side switch
		{
			Sides.Buy	=> message.GetBestBid()?.Price,
			Sides.Sell	=> message.GetBestAsk()?.Price,
			_			=> message.GetSpreadMiddle(null),
		};
	}

	/// <summary>
	/// Get middle of spread.
	/// </summary>
	/// <param name="message">Market depth.</param>
	/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
	/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
	public static decimal? GetSpreadMiddle(this Level1ChangeMessage message, decimal? priceStep)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var spreadMiddle = message.TryGetDecimal(Level1Fields.SpreadMiddle);

		if (spreadMiddle == null)
		{
			var bestBid = message.TryGetDecimal(Level1Fields.BestBidPrice);
			var bestAsk = message.TryGetDecimal(Level1Fields.BestAskPrice);

			spreadMiddle = bestBid.GetSpreadMiddle(bestAsk, priceStep);
		}

		return spreadMiddle;
	}

	/// <summary>
	/// Get middle of spread.
	/// </summary>
	/// <param name="bestBidPrice">Best bid price.</param>
	/// <param name="bestAskPrice">Best ask price.</param>
	/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
	/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
	public static decimal? GetSpreadMiddle(this decimal? bestBidPrice, decimal? bestAskPrice, decimal? priceStep)
	{
		if (bestBidPrice == null && bestAskPrice == null)
			return null;

		if (bestBidPrice != null && bestAskPrice != null)
			return bestBidPrice.Value.GetSpreadMiddle(bestAskPrice.Value, priceStep);

		return bestAskPrice ?? bestBidPrice.Value;
	}

	/// <summary>
	/// Get middle of spread.
	/// </summary>
	/// <param name="bestBidPrice">Best bid price.</param>
	/// <param name="bestAskPrice">Best ask price.</param>
	/// <param name="priceStep"><see cref="SecurityMessage.PriceStep"/></param>
	/// <returns>The middle of spread. Is <see langword="null" />, if quotes are empty.</returns>
	public static decimal GetSpreadMiddle(this decimal bestBidPrice, decimal bestAskPrice, decimal? priceStep)
	{
		var price = (bestAskPrice + bestBidPrice) / 2;

		if (priceStep is not null)
			price = ShrinkPrice(price, priceStep, priceStep.Value.GetCachedDecimals());

		return price;
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

		return message.TryGetDecimal(Level1Fields.LastTradePrice);
	}

	/// <summary>
	/// Cast <see cref="OrderMessage"/> to the <see cref="ExecutionMessage"/>.
	/// </summary>
	/// <param name="message"><see cref="OrderMessage"/>.</param>
	/// <param name="error">Error info.</param>
	/// <returns><see cref="ExecutionMessage"/>.</returns>
	public static ExecutionMessage CreateReply(this OrderMessage message, Exception error = null)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		var reply = message.TransactionId.CreateOrderReply(message.LocalTime);

		reply.Error = error;

		if (error != null)
			reply.OrderState = OrderStates.Failed;

		return reply;
	}

	/// <summary>
	/// Create order's transaction reply.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="serverTime">Server time.</param>
	/// <returns>The message contains information about the execution.</returns>
	public static ExecutionMessage CreateOrderReply(this long transactionId, DateTimeOffset serverTime)
	{
		return new ExecutionMessage
		{
			OriginalTransactionId = transactionId,
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			ServerTime = serverTime,
		};
	}

	/// <summary>
	/// Get message server time.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>Server time.</returns>
	public static DateTimeOffset GetServerTime(this Message message)
	{
		if (message.TryGetServerTime(out var serverTime))
			return serverTime;

		throw new InvalidOperationException();
	}

	/// <summary>
	/// Try get message server time.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="serverTime">Server time. If the value is <see langword="default" />, the message does not contain the server time.</param>
	/// <returns>Operation result.</returns>
	public static bool TryGetServerTime(this Message message, out DateTimeOffset serverTime)
	{
		if (message is IServerTimeMessage timeMsg)
		{
			serverTime = timeMsg.ServerTime;
			return true;
		}

		serverTime = default;
		return false;
	}

	/// <summary>
	/// Convert <see cref="MessageTypes"/> to <see cref="MessageTypeInfo"/> value.
	/// </summary>
	/// <param name="type"><see cref="MessageTypes"/> value.</param>
	/// <param name="isMarketData">Market data.</param>
	/// <returns><see cref="MessageTypeInfo"/> value.</returns>
	public static MessageTypeInfo ToInfo(this MessageTypes type, bool? isMarketData = null)
	{
		isMarketData ??= IsMarketData(type);

		return new MessageTypeInfo(type, isMarketData);
	}

	private static readonly CachedSynchronizedSet<MessageTypes> _transactionalTypes = new(
	[
		MessageTypes.OrderRegister,
		MessageTypes.OrderCancel,
		MessageTypes.OrderStatus,
		MessageTypes.OrderGroupCancel,
		MessageTypes.OrderReplace,
		MessageTypes.PortfolioLookup
	]);

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

	private static readonly CachedSynchronizedSet<MessageTypes> _marketDataTypes = new(
	[
		MessageTypes.MarketData,
		MessageTypes.SecurityLookup,
	]);

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
		dict.TryAdd2(info.Type, info);

		adapter.PossibleSupportedMessages = [.. dict.Values];
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

		adapter.PossibleSupportedMessages = [.. adapter.PossibleSupportedMessages.Where(i => i.Type != type)];
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
	/// Add time-frames into <see cref="IMessageAdapter.GetSupportedMarketDataTypes"/>.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="timeFrames">Time-frames.</param>
	public static void AddSupportedCandleTimeFrames(this MessageAdapter adapter, IEnumerable<TimeSpan> timeFrames)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		if (timeFrames is null)
			throw new ArgumentNullException(nameof(timeFrames));

		foreach (var tf in timeFrames)
			adapter.AddSupportedMarketDataType(tf.TimeFrame());
	}

	/// <summary>
	/// Add market data type into <see cref="IMessageAdapter.GetSupportedMarketDataTypes"/>.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="dataType">Data type info.</param>
	public static void AddSupportedMarketDataType(this MessageAdapter adapter, DataType dataType)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		adapter.SupportedMarketDataTypes = [.. adapter.SupportedMarketDataTypes.Append(dataType).Distinct()];
	}

	/// <summary>
	/// Remove market data type from <see cref="IMessageAdapter.GetSupportedMarketDataTypes"/>.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="type">Market data type.</param>
	public static void RemoveSupportedMarketDataType(this MessageAdapter adapter, DataType type)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		adapter.SupportedMarketDataTypes = [.. adapter.SupportedMarketDataTypes.Except([type])];
	}

	/// <summary>
	/// Add the message type info <see cref="IMessageAdapter.NotSupportedResultMessages"/>.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="type">Message type.</param>
	public static void AddNotSupportedResultMessage(this MessageAdapter adapter, MessageTypes type)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		adapter.NotSupportedResultMessages = adapter.NotSupportedResultMessages.Append(type).Distinct();
	}

	/// <summary>
	/// Remove the message type from <see cref="IMessageAdapter.NotSupportedResultMessages"/>.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="type">Message type.</param>
	public static void RemoveNotSupportedResultMessage(this MessageAdapter adapter, MessageTypes type)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		adapter.NotSupportedResultMessages = adapter.NotSupportedResultMessages.Where(t => t != type);
	}

	/// <summary>
	/// Determines whether the specified message type is contained in <see cref="IMessageAdapter.NotSupportedResultMessages"/>..
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="type">Message type.</param>
	/// <returns><see langword="true"/> if the specified message type is supported, otherwise, <see langword="false"/>.</returns>
	public static bool IsResultMessageNotSupported(this IMessageAdapter adapter, MessageTypes type)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		return adapter.NotSupportedResultMessages.Contains(type);
	}

	private static readonly SynchronizedDictionary<Type, Type> _candleArgTypes = [];

	/// <summary>
	/// Get candle arg type.
	/// </summary>
	/// <param name="candleMessageType">Candle message type.</param>
	/// <returns>Candle arg type.</returns>
	public static Type GetCandleArgType(this Type candleMessageType)
		=> _candleArgTypes[candleMessageType];

	private static readonly SynchronizedDictionary<Type, Func<object, bool>> _candleArgValidators = [];

	/// <summary>
	/// Validate candle arg.
	/// </summary>
	/// <param name="candleMessageType">Candle message type.</param>
	/// <param name="value">Candle arg.</param>
	/// <returns>Check result.</returns>
	public static bool ValidateCandleArg(this Type candleMessageType, object value)
		=> _candleArgValidators[candleMessageType](value);

	private static readonly CachedSynchronizedPairSet<MessageTypes, Type> _candleDataTypes = [];

	/// <summary>
	/// Determine the <paramref name="type"/> is candle data type.
	/// </summary>
	/// <param name="type">Message type.</param>
	/// <returns><see langword="true" />, if data type is candle, otherwise, <see langword="false" />.</returns>
	public static bool IsCandle(this MessageTypes type)	=> _candleDataTypes.ContainsKey(type);

	private static readonly SynchronizedPairSet<MessageTypes, Type> _messageTypeMap = [];

	/// <summary>
	/// Convert <see cref="Type"/> to <see cref="MessageTypes"/> value.
	/// </summary>
	/// <param name="type"><see cref="Type"/> value.</param>
	/// <returns><see cref="MessageTypes"/> value.</returns>
	public static MessageTypes ToMessageType(this Type type)
	{
		lock (_messageTypeMap.SyncRoot)
		{
			if (_messageTypeMap.TryGetKey(type, out var enumVal))
				return enumVal;

			if (!_candleDataTypes.TryGetKey(type, out enumVal))
				enumVal = type.CreateInstance<Message>().Type;

			_messageTypeMap.Add(enumVal, type);
			return enumVal;
		}
	}

	/// <summary>
	/// Convert <see cref="MessageTypes"/> to <see cref="Type"/> value.
	/// </summary>
	/// <param name="type"><see cref="MessageTypes"/> value.</param>
	/// <returns><see cref="Type"/> value.</returns>
	public static Type ToMessageType(this MessageTypes type)
	{
		lock (_messageTypeMap.SyncRoot)
		{
			if (_messageTypeMap.TryGetValue(type, out var typeVal))
				return typeVal;

			if (_candleDataTypes.TryGetValue(type, out typeVal))
				_messageTypeMap.Add(type, typeVal);
			else
			{
				var types = typeof(Message)
					.Assembly
					.FindImplementations<Message>(true, true);

				foreach (var type1 in types)
					type1.ToMessageType();

				if (!_messageTypeMap.TryGetValue(type, out typeVal))
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
			}

			return typeVal;
		}
	}

	/// <summary>
	/// To convert the type of message <see cref="MessageTypes"/> into type of candles <see cref="CandleMessage"/>.
	/// </summary>
	/// <param name="type">Message type.</param>
	/// <returns>Candles type.</returns>
	public static Type ToCandleMessageType(this MessageTypes type) => _candleDataTypes[type];

	private static readonly SynchronizedDictionary<Type, (Func<string, object> parse, Func<object, string> toString)> _dataTypeArgConverters = new()
	{
		{ typeof(ExecutionMessage), (str => str.To<ExecutionTypes>(), arg => arg.To<string>()) }
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
			return converter.parse(str);

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
			return converter.toString(arg);

		return arg.To<string>();
		//throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);
	}

	private static readonly SynchronizedPairSet<DataType, string> _fileNames = new(EqualityComparer<DataType>.Default, StringComparer.InvariantCultureIgnoreCase)
	{
		{ DataType.Ticks, "trades" },
		{ DataType.OrderLog, "orderLog" },
		{ DataType.Transactions, "transactions" },
		{ DataType.MarketDepth, "quotes" },
		{ DataType.Level1, "security" },
		{ DataType.PositionChanges, "position" },
		{ DataType.News, "news" },
		{ DataType.Board, "board" },
		{ DataType.BoardState, "board_state" },
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
	/// Determines whether the specified data type is supported by the storage.
	/// </summary>
	/// <param name="dataType"><see cref="DataType"/></param>
	/// <returns>Check result.</returns>
	public static bool IsStorageSupported(this DataType dataType)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		if (dataType.MessageType.IsCandleMessage())
			return _fileNames.ContainsKey(DataType.Create(dataType.MessageType, null));
		else
			return _fileNames.ContainsKey(dataType);
	}

	/// <summary>
	/// All registered candle types.
	/// </summary>
	public static IEnumerable<Type> AllCandleTypes => _candleDataTypes.CachedValues;

	private static readonly SynchronizedSet<Type> _buildOnlyCandles = [];

	/// <summary>
	/// Determines whether the specified candle type can build only from underlying data.
	/// </summary>
	/// <param name="candleType">The type of candle message.</param>
	/// <returns>Check result.</returns>
	public static bool IsBuildOnly(this Type candleType)
		=> _buildOnlyCandles.Contains(candleType);

	/// <summary>
	/// Register new candle type.
	/// </summary>
	/// <param name="messageType">The type of candle message.</param>
	/// <param name="type">Message type.</param>
	/// <param name="fileName">File name.</param>
	/// <param name="argParse"><see cref="string"/> to <typeparamref name="TArg"/> converter.</param>
	/// <param name="argToString"><typeparamref name="TArg"/> to <see cref="string"/> converter.</param>
	/// <param name="argValidator">Arg validator.</param>
	/// <param name="isBuildOnly">The candle type can build only from underlying data.</param>
	public static void RegisterCandleType<TArg>(
		Type messageType, MessageTypes type, string fileName,
		Func<string, TArg> argParse, Func<TArg, string> argToString,
		Func<TArg, bool> argValidator, bool isBuildOnly = true)
	{
		if (messageType is null)
			throw new ArgumentNullException(nameof(messageType));

		if (argParse is null)
			throw new ArgumentNullException(nameof(argParse));

		if (argToString is null)
			throw new ArgumentNullException(nameof(argToString));

		if (argValidator is null)
			throw new ArgumentNullException(nameof(argValidator));

		static T Do<T>(Func<T> func) => Ecng.Common.Do.Invariant(func);

		object p1(string str) => Do(() => argParse(str));
		string p2(object arg) => arg is string s ? s : Do(() => argToString((TArg)arg));

		_candleDataTypes.Add(type, messageType);
		_dataTypeArgConverters.Add(messageType, (p1, p2));
		_fileNames.Add(DataType.Create(messageType, null), fileName);
		_candleArgTypes.Add(messageType, messageType.CreateInstance<ICandleMessage>().ArgType);
		_candleArgValidators.Add(messageType, a => argValidator((TArg)a));

		if (isBuildOnly)
			_buildOnlyCandles.Add(messageType);
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

		return adapter.GetSupportedMarketDataTypes(subscription.SecurityId, subscription.From, subscription.To).Contains(subscription.DataType2);
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
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		return adapter.GetSupportedMarketDataTypes(securityId, from, to).Where(dt => dt.IsTFCandles).Select(dt => dt.Arg).OfType<TimeSpan>();
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

		var messageType = type.To<Type>();
		return DataType.Create(messageType, messageType.ToDataTypeArg(arg));
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
	/// <param name="type"><see cref="ExecutionTypes"/>.</param>
	/// <returns><see cref="DataType"/>.</returns>
	public static DataType ToDataType(this ExecutionTypes type)
	{
		return type switch
		{
			ExecutionTypes.Tick => DataType.Ticks,
			ExecutionTypes.Transaction => DataType.Transactions,
			ExecutionTypes.OrderLog => DataType.OrderLog,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to <see cref="ExecutionTypes"/> value.
	/// </summary>
	/// <param name="type"><see cref="DataType"/>.</param>
	/// <returns><see cref="ExecutionTypes"/>.</returns>
	public static ExecutionTypes ToExecutionType(this DataType type)
	{
		if (type == DataType.Ticks)
			return ExecutionTypes.Tick;
		else if (type == DataType.Transactions)
			return ExecutionTypes.Transaction;
		else if (type == DataType.OrderLog)
			return ExecutionTypes.OrderLog;
		else
			throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
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

		return adapter.GetSupportedMarketDataTypes().Contains(type);
	}

	/// <summary>
	/// Remove all market data types from <see cref="IMessageAdapter.GetSupportedMarketDataTypes"/>.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	public static void RemoveSupportedAllMarketDataTypes(this MessageAdapter adapter)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		adapter.SupportedMarketDataTypes = [];
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

		return message.DataType == DataType.Transactions && message.HasOrderInfo;
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

		return message.DataType == DataType.Transactions && message.HasTradeInfo;
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
	public static bool IsObsolete(this Level1Fields field) => field.GetAttributeOfType<ObsoleteAttribute>() != null;

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

		if (message.LocalTime == default)
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
				throw new InvalidOperationException(LocalizedStrings.StartCannotBeMoreEnd.Put(message.From, message.To));
		}

		return message;
	}

	/// <summary>
	/// Fields related to last trade.
	/// </summary>
	public static CachedSynchronizedSet<Level1Fields> LastTradeFields { get; } = new CachedSynchronizedSet<Level1Fields>(
	[
		Level1Fields.LastTradeId,
		Level1Fields.LastTradeStringId,
		Level1Fields.LastTradeTime,
		Level1Fields.LastTradeOrigin,
		Level1Fields.LastTradePrice,
		Level1Fields.LastTradeUpDown,
		Level1Fields.LastTradeVolume,
		Level1Fields.IsSystem,
	]);

	/// <summary>
	/// Is the specified <see cref="Level1Fields"/> is related to last trade.
	/// </summary>
	/// <param name="field">Field.</param>
	/// <returns>Check result.</returns>
	public static bool IsLastTradeField(this Level1Fields field) => LastTradeFields.Contains(field);

	/// <summary>
	/// Fields related to best bid.
	/// </summary>
	public static CachedSynchronizedSet<Level1Fields> BestBidFields { get; } = new CachedSynchronizedSet<Level1Fields>(
	[
		Level1Fields.BestBidPrice,
		Level1Fields.BestBidTime,
		Level1Fields.BestBidVolume
	]);

	/// <summary>
	/// Is the specified <see cref="Level1Fields"/> is related to best bid.
	/// </summary>
	/// <param name="field">Field.</param>
	/// <returns>Check result.</returns>
	public static bool IsBestBidField(this Level1Fields field) => BestBidFields.Contains(field);

	/// <summary>
	/// Fields related to best ask.
	/// </summary>
	public static CachedSynchronizedSet<Level1Fields> BestAskFields { get; } = new CachedSynchronizedSet<Level1Fields>(
	[
		Level1Fields.BestAskPrice,
		Level1Fields.BestAskTime,
		Level1Fields.BestAskVolume
	]);

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
	public static string GetPreferredLanguage(this MessageAdapterCategories? categories)
	{
		return categories?.HasFlag(MessageAdapterCategories.Russia) == true ? LocalizedStrings.RuCode : LocalizedStrings.EnCode;
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
					Times = [.. parts[1].SplitBySep("--").Select(s =>
					{
						var parts2 = s.Split('-');
						return new Range<TimeSpan>(parts2[0].ToTimeSpan(_timeFormat), parts2[1].ToTimeSpan(_timeFormat));
					})],
					SpecialDays = parts[2].SplitBySep("//").Select(s =>
					{
						var idx = s.IndexOf(':');
						return new KeyValuePair<DayOfWeek, Range<TimeSpan>[]>(s.Substring(0, idx).To<DayOfWeek>(), [.. s.Substring(idx + 1).SplitBySep("--").Select(s2 =>
						{
							var parts3 = s2.Split('-');
							return new Range<TimeSpan>(parts3[0].ToTimeSpan(_timeFormat), parts3[1].ToTimeSpan(_timeFormat));
						})]);
					}).ToDictionary()
				});
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(LocalizedStrings.ErrorParsing.Put(input), ex);
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
				specialDays[parts[0].ToDateTime(_dateFormat)] = [.. parts[1].SplitBySep("--").Select(s =>
				{
					var parts2 = s.Split('-');
					return new Range<TimeSpan>(parts2[0].ToTimeSpan(_timeFormat), parts2[1].ToTimeSpan(_timeFormat));
				})];
			}
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException(LocalizedStrings.ErrorParsing.Put(input), ex);
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

		return type != null && type.Is(interfaceType);
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
	/// Initialize <see cref="ISecurityTypesMessage.SecurityTypes"/>.
	/// </summary>
	/// <param name="message">Message security lookup for specified criteria.</param>
	/// <param name="type">Security type.</param>
	/// <param name="types">Securities types.</param>
	public static void SetSecurityTypes(this ISecurityTypesMessage message, SecurityTypes? type, IEnumerable<SecurityTypes> types = null)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (type != null)
			message.SecurityType = type.Value;
		else if (types != null)
		{
			var set = types.ToSet();

			if (set.Count <= 0)
				return;

			if (set.Count == 1)
				message.SecurityType = set.First();
			else
				message.SecurityTypes = [.. set];
		}
	}

	/// <summary>
	/// Get <see cref="ISecurityTypesMessage.SecurityTypes"/>.
	/// </summary>
	/// <param name="message">Message security lookup for specified criteria.</param>
	/// <returns>Securities types.</returns>
	public static HashSet<SecurityTypes> GetSecurityTypes(this ISecurityTypesMessage message)
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
	/// Remove adapter by the specified type.
	/// </summary>
	/// <typeparam name="TAdapter">The adapter type.</typeparam>
	/// <param name="adapter">Adapter.</param>
	/// <returns>Removed adapter or <see langword="null"/>.</returns>
	public static TAdapter TryRemoveWrapper<TAdapter>(this IMessageAdapter adapter)
		where TAdapter : IMessageAdapterWrapper
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		if (adapter is IMessageAdapterWrapper wrapper)
		{
			if (wrapper.InnerAdapter is TAdapter found)
			{
				wrapper.InnerAdapter = found.InnerAdapter;
				found.InnerAdapter = null;
				return found;
			}
			else
				return wrapper.InnerAdapter.TryRemoveWrapper<TAdapter>();
		}
		else
			return default;
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
	/// Determines whether the reply contains an error <see cref="IErrorMessage.Error"/>.
	/// </summary>
	/// <param name="message"><see cref="IErrorMessage"/></param>
	/// <returns>Check result.</returns>
	public static bool IsOk(this IErrorMessage message)
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

		var reply = message.IsHistoryOnly() ? (IOriginalTransactionIdMessage)new SubscriptionFinishedMessage() : new SubscriptionOnlineMessage();
		reply.OriginalTransactionId = message.TransactionId;
#if MSG_TRACE
		((Message)reply).StackTrace = ((Message)message).StackTrace;
#endif
		return (Message)reply;
	}

	/// <summary>
	/// Special set mean any depth for <see cref="IMessageAdapter.SupportedOrderBookDepths"/> option.
	/// </summary>
	public readonly static IEnumerable<int> AnyDepths = Array.AsReadOnly(new[] { -1 });

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
	/// Get time-frame.
	/// </summary>
	/// <param name="dataType"><see cref="DataType"/>.</param>
	/// <returns>Time-frame.</returns>
	public static TimeSpan GetTimeFrame(this DataType dataType)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		return (TimeSpan)dataType.Arg;
	}

	private static DataType CreateAndFreeze<TMessage>(object arg)
		=> DataType.Create<TMessage>(arg).Immutable();

	/// <summary>
	/// Create data type info for <see cref="TimeFrameCandleMessage"/>.
	/// </summary>
	/// <param name="tf">Candle arg.</param>
	/// <returns>Data type info.</returns>
	public static DataType TimeFrame(this TimeSpan tf)
		=> CreateAndFreeze<TimeFrameCandleMessage>(tf);

	/// <summary>
	/// Create data type info for <see cref="RangeCandleMessage"/>.
	/// </summary>
	/// <param name="arg">Candle arg.</param>
	/// <returns>Data type info.</returns>
	public static DataType Range(this Unit arg)
		=> CreateAndFreeze<RangeCandleMessage>(arg);

	/// <summary>
	/// Create data type info for <see cref="VolumeCandleMessage"/>.
	/// </summary>
	/// <param name="arg">Candle arg.</param>
	/// <returns>Data type info.</returns>
	public static DataType Volume(this decimal arg)
		=> CreateAndFreeze<VolumeCandleMessage>(arg);

	/// <summary>
	/// Create data type info for <see cref="TickCandleMessage"/>.
	/// </summary>
	/// <param name="arg">Candle arg.</param>
	/// <returns>Data type info.</returns>
	public static DataType Tick(this int arg)
		=> CreateAndFreeze<TickCandleMessage>(arg);

	/// <summary>
	/// Create data type info for <see cref="PnFCandleMessage"/>.
	/// </summary>
	/// <param name="arg">Candle arg.</param>
	/// <returns>Data type info.</returns>
	public static DataType PnF(this PnFArg arg)
		=> CreateAndFreeze<PnFCandleMessage>(arg);

	/// <summary>
	/// Create data type info for <see cref="RenkoCandleMessage"/>.
	/// </summary>
	/// <param name="arg">Candle arg.</param>
	/// <returns>Data type info.</returns>
	public static DataType Renko(this Unit arg)
		=> CreateAndFreeze<RenkoCandleMessage>(arg);

	/// <summary>
	/// Create data type info for <see cref="PortfolioMessage"/>.
	/// </summary>
	/// <param name="portfolioName">Portfolio name.</param>
	/// <returns>Data type info.</returns>
	public static DataType Portfolio(this string portfolioName)
	{
		if (portfolioName.IsEmpty())
			throw new ArgumentNullException(nameof(portfolioName));

		return CreateAndFreeze<PortfolioMessage>(portfolioName);
	}

	/// <summary>
	/// Get typed argument.
	/// </summary>
	/// <typeparam name="TArg">Arg type.</typeparam>
	/// <param name="mdMsg">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <returns>The additional argument, associated with data. For example, candle argument.</returns>
	public static TArg GetArg<TArg>(this MarketDataMessage mdMsg)
	{
		if (mdMsg.GetArg() is not TArg arg)
			throw new InvalidOperationException(LocalizedStrings.WrongCandleArg.Put(mdMsg.DataType2.Arg));

		return arg;
	}

	/// <summary>
	/// Get time-frame from the specified market-data message.
	/// </summary>
	/// <param name="mdMsg">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <returns>Time-frame.</returns>
	public static TimeSpan GetTimeFrame(this MarketDataMessage mdMsg)
		=> mdMsg.GetArg<TimeSpan>();

	/// <summary>
	/// Determines the specified time-frame is intraday.
	/// </summary>
	/// <param name="tf">Time-frame.</param>
	/// <returns>Check result.</returns>
	public static bool IsIntraday(this TimeSpan tf)
	{
		if (tf <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(tf));

		return tf < TimeSpan.FromDays(1);
	}

	/// <summary>
	/// Split to pair.
	/// </summary>
	/// <param name="symbol">Symbol.</param>
	/// <returns>Pair.</returns>
	public static (string pairFrom, string pairTo) SplitToPair(this string symbol)
	{
		if (symbol.IsEmpty())
			throw new ArgumentNullException(nameof(symbol));

		var parts = symbol.Split('/');

		if (parts.Length != 2)
			parts = symbol.Split('-');

		if (parts.Length != 2)
			throw new ArgumentException($"Symbol {symbol} cannot split.", nameof(symbol));

		return (parts[0], parts[1]);
	}

	/// <summary>
	/// Create error response.
	/// </summary>
	/// <param name="message">Original message.</param>
	/// <param name="ex">Error.</param>
	/// <param name="logs">Logs.</param>
	/// <param name="getSubscribers">Subscriber identifiers provider.</param>
	/// <returns>Error response.</returns>
	public static Message CreateErrorResponse(this Message message, Exception ex, ILogReceiver logs, Func<DataType, long[]> getSubscribers = null)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (ex == null)
			throw new ArgumentNullException(nameof(ex));

		logs.AddErrorLog(ex);

		ExecutionMessage makeErrorExecution(ExecutionMessage execMsg)
		{
			var subscribers = getSubscribers?.Invoke(DataType.Transactions);

			if (subscribers != null)
				execMsg.SetSubscriptionIds(subscribers);

			return execMsg;
		}

		switch (message.Type)
		{
			case MessageTypes.Connect:
				return new ConnectMessage { Error = ex };

			case MessageTypes.Disconnect:
				return new DisconnectMessage { Error = ex };

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			case MessageTypes.OrderGroupCancel:
				return makeErrorExecution(((OrderMessage)message).CreateReply(ex));

			case MessageTypes.ChangePassword:
			{
				var pwdMsg = (ChangePasswordMessage)message;
				return new ChangePasswordMessage
				{
					OriginalTransactionId = pwdMsg.TransactionId,
					Error = ex
				};
			}

			default:
			{
				if (message is ISubscriptionMessage subscrMsg)
					return subscrMsg.CreateResponse(ex);
				else
					return ex.ToErrorMessage((message as ITransactionIdMessage)?.TransactionId ?? 0);
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
			return [message.SubscriptionId];
		else
			return [];
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

		return execMsg.DataType == DataType.Ticks || execMsg.DataType == DataType.OrderLog;
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

		return message.TradePrice ?? throw new ArgumentOutOfRangeException(nameof(message), null, LocalizedStrings.PriceIsNotSpecified.Put(message.TradeId));
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

		return message.Balance ?? throw new ArgumentOutOfRangeException(nameof(message));
	}

	/// <summary>
	/// Try get security ID from the specified message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>Security ID or <see langword="null"/> if message do not provide it.</returns>
	public static SecurityId? TryGetSecurityId(this Message message)
	{
		return message switch
		{
			ISecurityIdMessage secIdMsg => secIdMsg.SecurityId,
			INullableSecurityIdMessage nullSecIdMsg => nullSecIdMsg.SecurityId,
			_ => null,
		};
	}

	/// <summary>
	/// Replace security id by the specified.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="securityId">Security ID.</param>
	/// <returns>Message.</returns>
	public static Message ReplaceSecurityId(this Message message, SecurityId securityId)
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
				throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.InvalidValue);
		}

		return message;
	}

	/// <summary>
	/// Check if the specified id is money id.
	/// </summary>
	/// <param name="secId">The message contains information about the position changes.</param>
	/// <returns>Check result.</returns>
	public static bool IsMoney(this SecurityId secId) => secId == SecurityId.Money;

	/// <summary>
	/// Determines the specified message contains <see cref="SecurityId.Money"/> position.
	/// </summary>
	/// <param name="posMsg">The message contains information about the position changes.</param>
	/// <returns>Check result.</returns>
	public static bool IsMoney(this PositionChangeMessage posMsg)
	{
		if (posMsg == null)
			throw new ArgumentNullException(nameof(posMsg));

		return posMsg.SecurityId.IsMoney();
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
	/// <param name="securityId"><see cref="SecurityId"/></param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="iterationInterval">Interval between iterations.</param>
	/// <returns>Step.</returns>
	public static TimeSpan GetHistoryStepSize(this IMessageAdapter adapter, SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		if (dataType == null)
			throw new ArgumentNullException(nameof(dataType));

		iterationInterval = adapter.IterationInterval;

		if (dataType.IsTFCandles)
		{
			if (!adapter.CheckTimeFrameByRequest && !adapter.GetSupportedMarketDataTypes(securityId).Contains(dataType))
				return TimeSpan.Zero;

			var tf = dataType.GetTimeFrame();

			if (tf.TotalDays <= 1)
			{
				if (tf.TotalMinutes < 0.1)
					return TimeSpan.FromHours(0.5);

				return TimeSpan.FromDays(30);
			}

			return TimeSpan.MaxValue;
		}

		// by default adapter do not provide historical data except candles
		return TimeSpan.Zero;
	}

	/// <summary>
	/// Get maximum possible items count per single subscription request.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <returns>Max items count.</returns>
	public static int? GetDefaultMaxCount(this DataType dataType)
	{
		if (dataType == DataType.Ticks ||
			dataType == DataType.Level1 ||
			dataType == DataType.OrderLog ||
			dataType == DataType.MarketDepth)
			return 1000;

		return null;
	}

	/// <summary>
	/// StockSharp news source.
	/// </summary>
	public const string NewsStockSharpSource = nameof(StockSharp);

	/// <summary>
	/// Simulator.
	/// </summary>
	public const string SimulatorPortfolioName = "Simulator_SS";

	/// <summary>
	/// Anonymous account.
	/// </summary>
	public const string AnonymousPortfolioName = "Anonymous_SS";

	private class TickEnumerable : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
	{
		private class TickEnumerator(IEnumerator<Level1ChangeMessage> level1Enumerator) : IEnumerator<ExecutionMessage>
		{
			private readonly IEnumerator<Level1ChangeMessage> _level1Enumerator = level1Enumerator ?? throw new ArgumentNullException(nameof(level1Enumerator));

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

				GC.SuppressFinalize(this);
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
			DataTypeEx = DataType.Ticks,
			SecurityId = level1.SecurityId,
			TradeId = (long?)level1.TryGet(Level1Fields.LastTradeId),
			TradePrice = level1.TryGetDecimal(Level1Fields.LastTradePrice),
			TradeVolume = level1.TryGetDecimal(Level1Fields.LastTradeVolume),
			OriginSide = (Sides?)level1.TryGet(Level1Fields.LastTradeOrigin),
			ServerTime = (DateTimeOffset?)level1.TryGet(Level1Fields.LastTradeTime) ?? level1.ServerTime,
			IsUpTick = (bool?)level1.TryGet(Level1Fields.LastTradeUpDown),
			LocalTime = level1.LocalTime,
			BuildFrom = level1.BuildFrom ?? DataType.Level1,
		};
	}

	/// <summary>
	/// To check, are there <see cref="DataType.IsCandles"/> in the level1 data.
	/// </summary>
	/// <param name="level1">Level1 data.</param>
	/// <returns>The test result.</returns>
	public static bool IsContainsCandle(this Level1ChangeMessage level1)
	{
		if (level1 is null)
			throw new ArgumentNullException(nameof(level1));

		var changes = level1.Changes;

		return
			changes.ContainsKey(Level1Fields.OpenPrice) ||
			changes.ContainsKey(Level1Fields.HighPrice) ||
			changes.ContainsKey(Level1Fields.LowPrice) ||
			changes.ContainsKey(Level1Fields.ClosePrice);
	}

	private class OrderBookEnumerable : SimpleEnumerable<QuoteChangeMessage>//, IEnumerableEx<QuoteChangeMessage>
	{
		private class OrderBookEnumerator(IEnumerator<Level1ChangeMessage> level1Enumerator) : IEnumerator<QuoteChangeMessage>
		{
			private readonly IEnumerator<Level1ChangeMessage> _level1Enumerator = level1Enumerator ?? throw new ArgumentNullException(nameof(level1Enumerator));

			private decimal? _prevBidPrice;
			private decimal? _prevBidVolume;
			private decimal? _prevAskPrice;
			private decimal? _prevAskVolume;

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

					_prevBidPrice = level1.TryGetDecimal(Level1Fields.BestBidPrice) ?? _prevBidPrice;
					_prevBidVolume = level1.TryGetDecimal(Level1Fields.BestBidVolume) ?? _prevBidVolume;
					_prevAskPrice = level1.TryGetDecimal(Level1Fields.BestAskPrice) ?? _prevAskPrice;
					_prevAskVolume = level1.TryGetDecimal(Level1Fields.BestAskVolume) ?? _prevAskVolume;

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
						Bids = _prevBidPrice == null ? [] : [new QuoteChange(_prevBidPrice.Value, _prevBidVolume ?? 0)],
						Asks = _prevAskPrice == null ? [] : [new QuoteChange(_prevAskPrice.Value, _prevAskVolume ?? 0)],
						BuildFrom = level1.BuildFrom ?? DataType.Level1,
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

				GC.SuppressFinalize(this);
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
	/// Check if the specified <see cref="ISecurityIdMessage"/> is <see cref="IsAllSecurity(SecurityId)"/>.
	/// </summary>
	/// <param name="message"><see cref="ISecurityIdMessage"/></param>
	/// <returns><see langword="true"/>, if the specified <see cref="ISecurityIdMessage"/> is <see cref="IsAllSecurity(SecurityId)"/>, otherwise, <see langword="false"/>.</returns>
	public static bool IsAllSecurity(this ISecurityIdMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return message.SecurityId.IsAllSecurity();
	}

	/// <summary>
	/// Check if the specified <see cref="SecurityId"/> is <see cref="IsAllSecurity(SecurityId)"/>.
	/// </summary>
	/// <param name="securityId"><see cref="SecurityId"/></param>
	/// <returns><see langword="true"/>, if the specified <see cref="SecurityId"/> is <see cref="IsAllSecurity(SecurityId)"/>, otherwise, <see langword="false"/>.</returns>
	public static bool IsAllSecurity(this SecurityId securityId)
	{
		//if (security == null)
		//	throw new ArgumentNullException(nameof(security));

		return securityId == default || (securityId.SecurityCode.EqualsIgnoreCase(SecurityId.AssociatedBoardCode) && securityId.BoardCode.EqualsIgnoreCase(SecurityId.AssociatedBoardCode));
	}

	/// <summary>
	/// To convert the currency type into the name in the MICEX format.
	/// </summary>
	/// <param name="type">Currency type.</param>
	/// <returns>The currency name in the MICEX format.</returns>
	public static string ToMicexCurrencyName(this CurrencyTypes type)
	{
		return type switch
		{
			CurrencyTypes.RUB => "SUR",
			_ => type.GetName(),
		};
	}

	private static readonly SynchronizedSet<string> _ignoreCurrs = [];

	/// <summary>
	/// To convert the currency name in the MICEX format into <see cref="CurrencyTypes"/>.
	/// </summary>
	/// <param name="name">The currency name in the MICEX format.</param>
	/// <param name="errorHandler">Error handler.</param>
	/// <returns>Currency type. If the value is empty, <see langword="null" /> will be returned.</returns>
	public static CurrencyTypes? FromMicexCurrencyName(this string name, Action<Exception> errorHandler = null)
	{
		if (name.IsEmpty() || _ignoreCurrs.Contains(name))
			return null;

		try
		{
			return name.To<CurrencyTypes>();
		}
		catch (Exception ex)
		{
			_ignoreCurrs.Add(name);

			if (errorHandler == null)
				ex.LogError();
			else
				errorHandler.Invoke(ex);

			return null;
		}
	}

	/// <summary>
	/// Convert <see cref="Level1Fields"/> to <see cref="Type"/> value.
	/// </summary>
	/// <param name="field"><see cref="Level1Fields"/> value.</param>
	/// <returns><see cref="Type"/> value.</returns>
	public static Type ToType(this Level1Fields field)
	{
		return field switch
		{
			Level1Fields.AsksCount or Level1Fields.BidsCount or
			Level1Fields.TradesCount or Level1Fields.Decimals => typeof(int),

			Level1Fields.LastTradeId => typeof(long),

			Level1Fields.BestAskTime or Level1Fields.BestBidTime or
			Level1Fields.LastTradeTime or Level1Fields.BuyBackDate or
			Level1Fields.CouponDate => typeof(DateTimeOffset),

			Level1Fields.LastTradeUpDown or Level1Fields.IsSystem => typeof(bool),

			Level1Fields.State => typeof(SecurityStates),
			Level1Fields.LastTradeOrigin => typeof(Sides),
			Level1Fields.LastTradeStringId => typeof(string),

			_ => field.IsObsolete() ? null : typeof(decimal),
		};
	}

	/// <summary>
	/// Convert <see cref="PositionChangeTypes"/> to <see cref="Type"/> value.
	/// </summary>
	/// <param name="type"><see cref="PositionChangeTypes"/> value.</param>
	/// <returns><see cref="Type"/> value.</returns>
	public static Type ToType(this PositionChangeTypes type)
	{
		return type switch
		{
			PositionChangeTypes.ExpirationDate => typeof(DateTimeOffset),
			PositionChangeTypes.State => typeof(PortfolioStates),
			PositionChangeTypes.Currency => typeof(CurrencyTypes),
			PositionChangeTypes.BuyOrdersCount or PositionChangeTypes.SellOrdersCount or PositionChangeTypes.OrdersCount or PositionChangeTypes.TradesCount => typeof(int),
			_ => type.IsObsolete() ? null : typeof(decimal),
		};
	}

	/// <summary>
	/// Convert <see cref="IOrderBookMessage"/> to <see cref="Level1ChangeMessage"/> value.
	/// </summary>
	/// <param name="message"><see cref="IOrderBookMessage"/> instance.</param>
	/// <returns><see cref="Level1ChangeMessage"/> instance.</returns>
	public static Level1ChangeMessage ToLevel1(this IOrderBookMessage message)
	{
		var b = message.GetBestBid();
		var a = message.GetBestAsk();

		var level1 = new Level1ChangeMessage
		{
			SecurityId = message.SecurityId,
			ServerTime = message.ServerTime,
		};

		if (b != null)
		{
			var bestBid = b.Value;

			level1.Add(Level1Fields.BestBidPrice, bestBid.Price);
			level1.Add(Level1Fields.BestBidVolume, bestBid.Volume);
		}

		if (a != null)
		{
			var bestAsk = a.Value;

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
			ServerTime = message.CloseTime == default ? message.OpenTime : message.CloseTime,
		}
		.Add(Level1Fields.OpenPrice, message.OpenPrice)
		.Add(Level1Fields.HighPrice, message.HighPrice)
		.Add(Level1Fields.LowPrice, message.LowPrice)
		.Add(Level1Fields.ClosePrice, message.ClosePrice)
		.TryAdd(Level1Fields.Volume, message.TotalVolume)
		.TryAdd(Level1Fields.TradesCount, message.TotalTicks)
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
		.TryAdd(Level1Fields.LastTradeUpDown, message.IsUpTick)
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
				BuildFrom = quote.BuildFrom ?? DataType.MarketDepth,
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
		=> dataTypes.Where(t => t.IsTFCandles && t.Arg != null).Select(t => t.GetTimeFrame());

	/// <summary>
	/// To determine whether the order book is in the right state.
	/// </summary>
	/// <param name="book">Order book.</param>
	/// <returns><see langword="true" />, if the order book contains correct data, otherwise <see langword="false" />.</returns>
	/// <remarks>
	/// It is used in cases when the trading system by mistake sends the wrong quotes.
	/// </remarks>
	public static bool Verify(this IOrderBookMessage book)
	{
		if (book is null)
			throw new ArgumentNullException(nameof(book));

		var bids = book.Bids;
		var asks = book.Asks;

		var bestBid = bids.FirstOr();
		var bestAsk = asks.FirstOr();

		if (bestBid != null && bestAsk != null)
		{
			return bids.All(b => b.Price < bestAsk.Value.Price) && asks.All(a => a.Price > bestBid.Value.Price) && Verify(bids, true) && Verify(asks, false);
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
		return quote.Price > 0 && quote.Volume > 0;
	}

	/// <summary>
	/// Determines the specified message is matched lookup criteria.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="type"><see cref="IMessage.Type"/></param>
	/// <param name="criteria">The message which fields will be used as a filter.</param>
	/// <returns>Check result.</returns>
	public static bool IsMatch(this ISubscriptionIdMessage message, MessageTypes type, ISubscriptionMessage criteria)
	{
		switch (type)
		{
			case MessageTypes.Security:
			{
				if (criteria is SecurityLookupMessage lookupMsg)
					return message.To<SecurityMessage>().IsMatch(lookupMsg);

				return true;
			}
			case MessageTypes.Board:
			{
				if (criteria is BoardLookupMessage lookupMsg)
					return message.To<BoardMessage>().IsMatch(lookupMsg);

				return true;
			}
			case MessageTypes.Portfolio:
			{
				if (criteria is PortfolioLookupMessage lookupMsg)
					return message.To<PortfolioMessage>().IsMatch(lookupMsg, true);

				return true;
			}
			case MessageTypes.PositionChange:
			{
				if (criteria is PortfolioLookupMessage lookupMsg)
					return message.To<PositionChangeMessage>().IsMatch(lookupMsg, true);

				return true;
			}
			case MessageTypes.Execution:
			{
				var execMsg = message.To<ExecutionMessage>();

				if (execMsg.IsMarketData())
				{
					//if (criteria is MarketDataMessage mdMsg)
					//	return execMsg.IsMatch(mdMsg);

					return true;
				}
				else
				{
					if (criteria is OrderStatusMessage statusMsg)
						return execMsg.IsMatch(statusMsg);

					return true;
				}
			}
			//case MessageTypes.QuoteChange:
			//{
			//	if (criteria is MarketDataMessage mdMsg)
			//		return ((QuoteChangeMessage)message).IsMatch(mdMsg);

			//	return true;
			//}

			default:
			{
				//if (message.Type.IsCandle())
				//{
				//	if (criteria is MarketDataMessage mdMsg)
				//		return message.IsMatch(mdMsg);

				//	return true;
				//}

				return true;
			}
		}
	}

	/// <summary>
	/// Determines the specified message is matched lookup criteria.
	/// </summary>
	/// <param name="board">Board.</param>
	/// <param name="criteria">The message which fields will be used as a filter.</param>
	/// <returns>Check result.</returns>
	public static bool IsMatch(this BoardMessage board, BoardLookupMessage criteria)
	{
		if (board is null)
			throw new ArgumentNullException(nameof(board));

		if (criteria is null)
			throw new ArgumentNullException(nameof(criteria));

		if (!criteria.Like.IsEmpty() && !board.Code.ContainsIgnoreCase(criteria.Like))
			return false;

		return true;
	}

	/// <summary>
	/// Determines the specified message is matched lookup criteria.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <param name="criteria">The message which fields will be used as a filter.</param>
	/// <param name="compareName">Fully compare <see cref="PortfolioMessage.PortfolioName"/>.</param>
	/// <returns>Check result.</returns>
	public static bool IsMatch(this PortfolioMessage portfolio, PortfolioLookupMessage criteria, bool compareName)
	{
		if (portfolio is null)
			throw new ArgumentNullException(nameof(portfolio));

		if (criteria is null)
			throw new ArgumentNullException(nameof(criteria));

		if (!criteria.PortfolioName.IsEmpty())
		{
			if (compareName && !!portfolio.PortfolioName.EqualsIgnoreCase(criteria.PortfolioName))
				return false;
			else if (!compareName && !portfolio.PortfolioName.ContainsIgnoreCase(criteria.PortfolioName))
				return false;
		}

		if (!criteria.BoardCode.IsEmpty() && !portfolio.BoardCode.EqualsIgnoreCase(criteria.BoardCode))
			return false;

		if (!criteria.ClientCode.IsEmpty() && !portfolio.ClientCode.EqualsIgnoreCase(criteria.ClientCode))
			return false;

		if (criteria.Currency != null && portfolio.Currency != criteria.Currency)
			return false;

		return true;
	}

	/// <summary>
	/// Determines the specified message is matched lookup criteria.
	/// </summary>
	/// <param name="position">Position.</param>
	/// <param name="criteria">The message which fields will be used as a filter.</param>
	/// <param name="compareName">Fully compare <see cref="PositionChangeMessage.PortfolioName"/>.</param>
	/// <returns>Check result.</returns>
	public static bool IsMatch(this PositionChangeMessage position, PortfolioLookupMessage criteria, bool compareName)
	{
		if (position is null)
			throw new ArgumentNullException(nameof(position));

		if (criteria is null)
			throw new ArgumentNullException(nameof(criteria));

		if (!criteria.PortfolioName.IsEmpty())
		{
			if (compareName && !!position.PortfolioName.EqualsIgnoreCase(criteria.PortfolioName))
				return false;
			else if (!compareName && !position.PortfolioName.ContainsIgnoreCase(criteria.PortfolioName))
				return false;
		}

		if (criteria.SecurityId != null && position.SecurityId != criteria.SecurityId.Value)
			return false;

		if (!criteria.BoardCode.IsEmpty() && !position.BoardCode.EqualsIgnoreCase(criteria.BoardCode))
			return false;

		if (!criteria.ClientCode.IsEmpty() && !position.ClientCode.EqualsIgnoreCase(criteria.ClientCode))
			return false;

		if (!criteria.StrategyId.IsEmpty() && !position.StrategyId.EqualsIgnoreCase(criteria.StrategyId))
			return false;

		if (criteria.Side != null && position.Side != criteria.Side)
			return false;

		return true;
	}

	/// <summary>
	/// Determines the specified message is matched lookup criteria.
	/// </summary>
	/// <param name="transaction">Transaction.</param>
	/// <param name="criteria">The message which fields will be used as a filter.</param>
	/// <returns>Check result.</returns>
	public static bool IsMatch(this ExecutionMessage transaction, OrderStatusMessage criteria)
	{
		if (transaction.IsMarketData())
			throw new ArgumentException(nameof(transaction));

		if (criteria is null)
			throw new ArgumentNullException(nameof(criteria));

		return transaction.IsMatch(criteria, criteria.States.ToSet());
	}

	/// <summary>
	/// Determines the specified transaction is matched lookup criteria.
	/// </summary>
	/// <param name="transaction">Transaction.</param>
	/// <param name="criteria">The order which fields will be used as a filter.</param>
	/// <param name="states">Filter order by the specified states.</param>
	/// <returns>Check result.</returns>
	public static bool IsMatch(this ExecutionMessage transaction, OrderStatusMessage criteria, ISet<OrderStates> states)
	{
		if (transaction.IsMarketData())
			throw new ArgumentException(nameof(transaction));

		if (criteria is null)
			throw new ArgumentNullException(nameof(criteria));

		if (states is null)
			throw new ArgumentNullException(nameof(states));

		if (criteria.SecurityId != default && criteria.SecurityId != transaction.SecurityId)
			return false;

		if (states.Count > 0 && transaction.OrderState != null && !states.Contains(transaction.OrderState.Value))
			return false;

		if (criteria.Side != default && criteria.Side != transaction.Side)
			return false;

		if (criteria.Volume != default && criteria.Volume != transaction.OrderVolume)
			return false;

		if (!criteria.StrategyId.IsEmpty() && !criteria.StrategyId.EqualsIgnoreCase(transaction.StrategyId))
			return false;

		if (!criteria.PortfolioName.IsEmpty() && !criteria.PortfolioName.EqualsIgnoreCase(transaction.PortfolioName))
			return false;

		return true;
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

		if (!secId.BoardCode.IsEmpty() && !security.SecurityId.BoardCode.EqualsIgnoreCase(secId.BoardCode))
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
			foreach (var i in criteria.SecurityIds)
			{
				if (!i.SecurityCode.IsEmpty() && !security.SecurityId.SecurityCode.ContainsIgnoreCase(i.SecurityCode))
					continue;

				if (!i.BoardCode.IsEmpty() && !security.SecurityId.BoardCode.EqualsIgnoreCase(i.BoardCode))
					continue;

				// sec + board codes means exact id
				if (!i.SecurityCode.IsEmpty() && !i.BoardCode.IsEmpty())
				{
					if (security.SecurityId == i)
						return true;
				}
			}

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

		if (criteria.ExpiryDate != null && security.ExpiryDate?.Date != criteria.ExpiryDate?.Date)
			return false;

		if (criteria.SettlementDate != null && security.SettlementDate?.Date != criteria.SettlementDate?.Date)
			return false;

		if (!criteria.GetUnderlyingCode().IsEmpty() && !security.GetUnderlyingCode().ContainsIgnoreCase(criteria.GetUnderlyingCode()))
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

		if (criteria.IssueDate != null && security.IssueDate?.Date != criteria.IssueDate?.Date)
			return false;

		if (criteria.UnderlyingSecurityType != null && security.UnderlyingSecurityType != criteria.UnderlyingSecurityType)
			return false;

		if (criteria.Shortable != null && security.Shortable != criteria.Shortable)
			return false;

		if (!criteria.BasketCode.IsEmptyOrWhiteSpace() && !security.BasketCode.ContainsIgnoreCase(criteria.BasketCode))
			return false;

		if (!criteria.BasketExpression.IsEmptyOrWhiteSpace() && !security.BasketExpression.EqualsIgnoreCase(criteria.BasketExpression))
			return false;

		if (criteria.FaceValue != default && security.FaceValue != criteria.FaceValue)
			return false;

		if (criteria.SettlementType != default && security.SettlementType != criteria.SettlementType)
			return false;

		if (criteria.OptionStyle != default && security.OptionStyle != criteria.OptionStyle)
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
			return [.. securities.TryLimitByCount(criteria)];

		var secTypes = criteria.GetSecurityTypes();

		var result = securities.Where(s => s.IsMatch(criteria, secTypes));

		if (criteria.Skip != null)
			result = result.Skip((int)criteria.Skip.Value);

		if (criteria.Count != null)
			result = result.Take((int)criteria.Count.Value);

		return [.. result];
	}

	/// <summary>
	/// "All securities" instance.
	/// </summary>
	public static SecurityMessage AllSecurity { get; } = new SecurityMessage();

	/// <summary>
	/// Lookup all securities predefined criteria.
	/// </summary>
	public static readonly SecurityLookupMessage LookupAllCriteriaMessage = new();

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
			criteria.SecurityId == default &&
			criteria.Name.IsEmpty() &&
			criteria.ShortName.IsEmpty() &&
			criteria.VolumeStep == default &&
			criteria.MinVolume == default &&
			criteria.MaxVolume == default &&
			criteria.Multiplier == default &&
			criteria.Decimals == default &&
			criteria.PriceStep == default &&
			criteria.SecurityType == default &&
			criteria.CfiCode.IsEmpty() &&
			criteria.ExpiryDate == default &&
			criteria.SettlementDate == default &&
			criteria.UnderlyingSecurityId == default &&
			criteria.UnderlyingSecurityMinVolume == default &&
			criteria.Strike == default &&
			criteria.OptionType == default &&
			criteria.BinaryOptionType.IsEmpty() &&
			criteria.Currency == default &&
			criteria.Class.IsEmpty() &&
			criteria.IssueSize == default &&
			criteria.IssueDate == default &&
			criteria.UnderlyingSecurityType == default &&
			criteria.Shortable == default &&
			criteria.BasketCode.IsEmpty() &&
			criteria.BasketExpression.IsEmpty() &&
			criteria.FaceValue == default &&
			criteria.SettlementType == default &&
			criteria.OptionStyle == default &&
			criteria.PrimaryId == default &&
			(criteria.SecurityTypes == default || criteria.SecurityTypes.Length == 0) &&
			criteria.IncludeExpired == default &&
			// count is NOT filter by fields
			//criteria.Count == default &&
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

		adapter.SupportedInMessages = [.. supported.Distinct()];
	}

	private static readonly SynchronizedDictionary<DataType, MessageTypes> _messageTypes = [];

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
			return MessageTypes.Board;
		else if (type == DataType.BoardState)
			return MessageTypes.BoardState;
		else if (type == DataType.Securities)
			return MessageTypes.Security;
		else if (type == DataType.SecurityLegs)
			return MessageTypes.SecurityLegsInfo;
		else if (type == DataType.DataTypeInfo)
			return MessageTypes.DataTypeInfo;
		else if (type.IsCandles)
			return type.MessageType.ToMessageType();
		else
		{
			return _messageTypes.SafeAdd(type, key => key.MessageType.ToMessageType());
			//throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
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

	/// <summary>
	/// Constant value for <see cref="OrderRegisterMessage.TillDate"/> means Today(=Session).
	/// </summary>
	public static readonly DateTimeOffset Today = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

	/// <summary>
	/// To check the specified date is today.
	/// </summary>
	/// <param name="date">The specified date.</param>
	/// <returns><see langword="true"/> if the specified date is today, otherwise, <see langword="false"/>.</returns>
	public static bool IsToday(this DateTimeOffset? date)
	{
		return date?.IsToday() == true;
	}

	/// <summary>
	/// To check the specified date is today.
	/// </summary>
	/// <param name="date">The specified date.</param>
	/// <returns><see langword="true"/> if the specified date is today, otherwise, <see langword="false"/>.</returns>
	public static bool IsToday(this DateTimeOffset date)
	{
		return date == Today;
	}

	/// <summary>
	/// Determines the specified date equals is <see cref="Today"/> and returns <see cref="DateTime.Today"/>.
	/// </summary>
	/// <param name="date">The specified date.</param>
	/// <returns>Result value.</returns>
	public static DateTimeOffset? EnsureToday(this DateTimeOffset? date)
		=> date.EnsureToday(DateTime.Today.ApplyUtc());

	/// <summary>
	/// Determines the specified date equals is <see cref="Today"/> and returns <paramref name="todayValue"/>.
	/// </summary>
	/// <param name="date">The specified date.</param>
	/// <param name="todayValue">Today value.</param>
	/// <returns>Result value.</returns>
	public static DateTimeOffset? EnsureToday(this DateTimeOffset? date, DateTimeOffset? todayValue)
		=> date == null ? null : (date.IsToday() ? todayValue : date);

	/// <summary>
	/// Truncate the specified order book by max depth value.
	/// </summary>
	/// <param name="depth">Order book.</param>
	/// <param name="maxDepth">The maximum depth of order book.</param>
	/// <returns>Truncated order book.</returns>
	public static QuoteChangeMessage Truncate(this IOrderBookMessage depth, int maxDepth)
	{
		if (depth == null)
			throw new ArgumentNullException(nameof(depth));

		depth.CheckIsSnapshot();

		return new()
		{
			ServerTime = depth.ServerTime,
			SecurityId = depth.SecurityId,
			Bids = [.. depth.Bids.Take(maxDepth)],
			Asks = [.. depth.Asks.Take(maxDepth)],
		};
	}

	/// <summary>
	/// Determine whether the order book is final.
	/// </summary>
	/// <param name="depth">Order book message.</param>
	/// <returns><see langword="true"/>, if the order book is final, otherwise <see langword="false"/>.</returns>
	public static bool IsFinal(this IOrderBookMessage depth)
	{
		if (depth is null)
			throw new ArgumentNullException(nameof(depth));

		return depth.State is null or QuoteChangeStates.SnapshotComplete;
	}

	private static void CheckIsSnapshot(this IOrderBookMessage depth)
	{
		if (!depth.IsFinal())
			throw new ArgumentException($"State={depth.State}", nameof(depth));
	}

	/// <summary>
	/// To create from regular order book a sparse one.
	/// </summary>
	/// <remarks>
	/// In sparsed book shown quotes with no active orders. The volume of these quotes is 0.
	/// </remarks>
	/// <param name="depth">The regular order book.</param>
	/// <param name="priceRange">Minimum price step.</param>
	/// <param name="priceStep">Security price step.</param>
	/// <param name="maxDepth">Max depth.</param>
	/// <returns>The sparse order book.</returns>
	public static QuoteChangeMessage Sparse(this IOrderBookMessage depth, decimal priceRange, decimal? priceStep, int maxDepth = 20)
	{
		depth.CheckIsSnapshot();

		var bids = depth.Bids.Sparse(Sides.Buy, priceRange, priceStep, maxDepth);
		var asks = depth.Asks.Sparse(Sides.Sell, priceRange, priceStep, maxDepth);

		var bestBid = depth.GetBestBid();
		var bestAsk = depth.GetBestAsk();

		long spreadMaxDepthLong = (long)(maxDepth - bids.Length) + (maxDepth - asks.Length);
		var spreadMaxDepth = spreadMaxDepthLong > int.MaxValue ? int.MaxValue : (int)spreadMaxDepthLong;

		var spreadQuotes = bestBid is null || bestAsk is null || spreadMaxDepth <= 0
			? (bids: [], asks: [])
			: bestBid.Value.Sparse(bestAsk.Value, priceRange, priceStep, spreadMaxDepth);

		return new()
		{
			SecurityId = depth.SecurityId,
			ServerTime = depth.ServerTime,
			BuildFrom = DataType.MarketDepth,
			Bids = spreadQuotes.bids.Concat(bids),
			Asks = spreadQuotes.asks.Concat(asks),
		};
	}

	private static void ValidatePriceRange(decimal priceRange)
	{
		if (priceRange <= 0)
			throw new ArgumentOutOfRangeException(nameof(priceRange), priceRange, LocalizedStrings.InvalidValue);
	}

	private static decimal GetActualPriceRange(Unit priceRange)
	{
		if (priceRange is null)
			throw new ArgumentNullException(nameof(priceRange));
		
		var val = (decimal)priceRange;
		ValidatePriceRange(val);

		// Limit can be casted to decimal, so check it extra
		if (priceRange.Type is UnitTypes.Limit)
			throw new ArgumentException(LocalizedStrings.UnsupportedType.Put(priceRange.Type), nameof(priceRange));

		return val;
	}

	/// <summary>
	/// To create form pair of quotes a sparse collection of quotes, which will be included into the range between the pair.
	/// </summary>
	/// <remarks>
	/// In sparsed collection shown quotes with no active orders. The volume of these quotes is 0.
	/// </remarks>
	/// <param name="bid">Bid.</param>
	/// <param name="ask">Ask.</param>
	/// <param name="priceRange">Minimum price step.</param>
	/// <param name="priceStep">Security price step.</param>
	/// <param name="maxDepth">Max depth.</param>
	/// <returns>The sparse collection of quotes.</returns>
	public static (QuoteChange[] bids, QuoteChange[] asks) Sparse(this QuoteChange bid, QuoteChange ask, decimal priceRange, decimal? priceStep, int maxDepth = 10)
	{
		ValidatePriceRange(priceRange);

		var bidPrice = bid.Price;
		var askPrice = ask.Price;

		if (bidPrice == default || askPrice == default || bidPrice == askPrice)
			return ([], []);

		var bids = new List<QuoteChange>();
		var asks = new List<QuoteChange>();

		var currentBidPrice = bidPrice.ShrinkPrice(priceStep, null, ShrinkRules.More);
		var currentAskPrice = askPrice.ShrinkPrice(priceStep, null, ShrinkRules.Less);

		while (currentBidPrice < currentAskPrice && (bids.Count + asks.Count) < maxDepth)
		{
			var wasBid = currentBidPrice;
			var wasAsk = currentAskPrice;

			currentBidPrice = (currentBidPrice + priceRange).ShrinkPrice(priceStep, null, ShrinkRules.Less);

			if (wasBid > currentBidPrice)
				break;

			if (currentBidPrice > bidPrice && currentBidPrice < askPrice)
				bids.Add(new() { Price = currentBidPrice });

			currentAskPrice = (currentAskPrice - priceRange).ShrinkPrice(priceStep, null, ShrinkRules.More);

			if (wasAsk < currentAskPrice)
				break;

			if (currentAskPrice > bidPrice && currentAskPrice < askPrice)
				asks.Insert(0, new() { Price = currentAskPrice });

			if (wasBid == currentBidPrice && wasAsk == currentAskPrice)
				break;
		}

		return (bids.ToArray(), asks.ToArray());
	}

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
	/// <param name="maxDepth">Max depth.</param>
	/// <returns>The sparse collection of quotes.</returns>
	public static QuoteChange[] Sparse(this QuoteChange[] quotes, Sides side, decimal priceRange, decimal? priceStep, int maxDepth)
	{
		if (quotes is null)
			throw new ArgumentNullException(nameof(quotes));

		ValidatePriceRange(priceRange);

		if (quotes.Length < 2)
			return [.. quotes];

		var retVal = new List<QuoteChange>();

		for (var i = 0; i < (quotes.Length - 1); i++)
		{
			var from = quotes[i];
			var toPrice = quotes[i + 1].Price;

			// Always add the original quote
			retVal.Add(from);

			if (side == Sides.Buy)
			{
				for (var price = from.Price - priceRange; price > toPrice; price -= priceRange)
				{
					var p = price.ShrinkPrice(priceStep, null, ShrinkRules.Less);

					if (p <= toPrice)
						break;

					retVal.Add(new QuoteChange { Price = p });

					if (retVal.Count > maxDepth)
						break;
				}
			}
			else
			{
				for (var price = from.Price + priceRange; price < toPrice; price += priceRange)
				{
					var p = price.ShrinkPrice(priceStep, null, ShrinkRules.More);

					if (p >= toPrice)
						break;

					retVal.Add(new QuoteChange { Price = p });

					if (retVal.Count > maxDepth)
						break;
				}
			}

			if (retVal.Count > maxDepth)
				break;
		}

		retVal.Add(quotes[quotes.Length - 1]);  // Add the last quote
		return [.. retVal];
	}

	/// <summary>
	/// To group the order book by the price range.
	/// </summary>
	/// <param name="depth">The order book to be grouped.</param>
	/// <param name="priceRange">The price range, for which grouping shall be performed.</param>
	/// <returns>The grouped order book.</returns>
	public static QuoteChangeMessage Group(this IOrderBookMessage depth, decimal priceRange)
	{
		depth.CheckIsSnapshot();

		return new()
		{
			SecurityId = depth.SecurityId,
			ServerTime = depth.ServerTime,
			Bids = depth.Bids.Group(Sides.Buy, priceRange),
			Asks = depth.Asks.Group(Sides.Sell, priceRange),
			BuildFrom = DataType.MarketDepth,
		};
	}

	/// <summary>
	/// To de-group the order book, grouped using the method <see cref="Group(IOrderBookMessage, decimal)"/>.
	/// </summary>
	/// <param name="depth">The grouped order book.</param>
	/// <returns>The de-grouped order book.</returns>
	public static QuoteChangeMessage UnGroup(this IOrderBookMessage depth)
	{
		static QuoteChange[] GetInner(QuoteChange quote)
			=> quote.InnerQuotes ?? throw new ArgumentException(quote.ToString(), nameof(quote));

		depth.CheckIsSnapshot();

		return new()
		{
			SecurityId = depth.SecurityId,
			ServerTime = depth.ServerTime,
			Bids = [.. depth.Bids.SelectMany(GetInner).OrderByDescending(q => q.Price)],
			Asks = [.. depth.Asks.SelectMany(GetInner).OrderBy(q => q.Price)],
			BuildFrom = DataType.MarketDepth,
		};
	}

	/// <summary>
	/// To group quotes by the price range.
	/// </summary>
	/// <param name="quotes">Quotes to be grouped.</param>
	/// <param name="side">Side.</param>
	/// <param name="priceRange">The price range, for which grouping shall be performed.</param>
	/// <returns>Grouped quotes.</returns>
	public static QuoteChange[] Group(this QuoteChange[] quotes, Sides side, decimal priceRange)
	{
		if (quotes is null)
			throw new ArgumentNullException(nameof(quotes));

		ValidatePriceRange(priceRange);

		if (quotes.Length == 0)
			return [];
		else if (quotes.Length == 1)
		{
			var q = quotes[0];
			return [new(q.Price, q.Volume) { InnerQuotes = [q] }];
		}

		if (side == Sides.Buy)
			priceRange = -priceRange;

		const int maxLimit = 10000;

		var retVal = new List<QuoteChange>();

		var groupedQuote = new QuoteChange { Price = quotes[0].Price };
		var innerQuotes = new List<QuoteChange> { quotes[0] };

		var nextPrice = groupedQuote.Price + priceRange;

		for (int i = 1; i < quotes.Length; i++)
		{
			var currQuote = quotes[i];

			if (side == Sides.Buy)
			{
				if (currQuote.Price > nextPrice)
				{
					innerQuotes.Add(currQuote);

					if (innerQuotes.Count > maxLimit)
						break;

					continue;
				}
			}
			else
			{
				if (currQuote.Price < nextPrice)
				{
					innerQuotes.Add(currQuote);

					if (innerQuotes.Count > maxLimit)
						break;

					continue;
				}
			}

			groupedQuote.InnerQuotes = innerQuotes.CopyAndClear();
			retVal.Add(groupedQuote);

			if (innerQuotes.Count > maxLimit)
				break;

			groupedQuote = new QuoteChange { Price = currQuote.Price };
			innerQuotes.Add(currQuote);

			nextPrice = groupedQuote.Price + priceRange;
		}

		if (innerQuotes.Count > 0)
		{
			groupedQuote.InnerQuotes = innerQuotes.CopyAndClear();
			retVal.Add(groupedQuote);
		}

		return [.. retVal];
	}

	/// <summary>
	/// To calculate the change between order books.
	/// </summary>
	/// <param name="from">First order book.</param>
	/// <param name="to">Second order book.</param>
	/// <returns>The order book, storing only increments.</returns>
	public static QuoteChangeMessage GetDelta(this IOrderBookMessage from, IOrderBookMessage to)
	{
		if (from == null)
			throw new ArgumentNullException(nameof(from));

		if (to == null)
			throw new ArgumentNullException(nameof(to));

		return new()
		{
			LocalTime = to.LocalTime,
			SecurityId = to.SecurityId,
			Bids = GetDelta(from.Bids, to.Bids, new BackwardComparer<decimal>()),
			Asks = GetDelta(from.Asks, to.Asks, null),
			ServerTime = to.ServerTime,
			State = QuoteChangeStates.Increment,
		};
	}

	/// <summary>
	/// To calculate the change between quotes.
	/// </summary>
	/// <param name="from">First quotes.</param>
	/// <param name="to">Second quotes.</param>
	/// <param name="comparer">The direction, showing the type of quotes.</param>
	/// <returns>Changes.</returns>
	private static QuoteChange[] GetDelta(this IEnumerable<QuoteChange> from, IEnumerable<QuoteChange> to, IComparer<decimal> comparer)
	{
		if (from == null)
			throw new ArgumentNullException(nameof(from));

		if (to == null)
			throw new ArgumentNullException(nameof(to));

		var mapFrom = new SortedList<decimal, QuoteChange>(comparer);
		var mapTo = new SortedList<decimal, QuoteChange>(comparer);

		foreach (var change in from)
		{
			if (!mapFrom.TryAdd2(change.Price, change))
				throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(change.Price), nameof(from));
		}

		foreach (var change in to)
		{
			if (!mapTo.TryAdd2(change.Price, change))
				throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(change.Price), nameof(to));
		}

		foreach (var pair in mapFrom)
		{
			var price = pair.Key;
			var quoteFrom = pair.Value;

			if (mapTo.TryGetValue(price, out var quoteTo))
			{
				if (quoteTo.Volume == quoteFrom.Volume &&
					quoteTo.OrdersCount == quoteFrom.OrdersCount &&
					quoteTo.Action == quoteFrom.Action &&
					quoteTo.Condition == quoteFrom.Condition &&
					quoteTo.StartPosition == quoteFrom.StartPosition &&
					quoteTo.EndPosition == quoteFrom.EndPosition)
				{
					// nothing was changes, remove this
					mapTo.Remove(price);
				}
			}
			else
			{
				// zero volume means remove price level
				mapTo[price] = new QuoteChange { Price = price };
			}
		}

		return [.. mapTo.Values];
	}

	/// <summary>
	/// To add change to the first order book.
	/// </summary>
	/// <param name="from">First order book.</param>
	/// <param name="delta">Change.</param>
	/// <returns>The changed order book.</returns>
	public static QuoteChangeMessage AddDelta(this IOrderBookMessage from, IOrderBookMessage delta)
	{
		if (from == null)
			throw new ArgumentNullException(nameof(from));

		if (delta == null)
			throw new ArgumentNullException(nameof(delta));

		return new()
		{
			LocalTime = delta.LocalTime,
			SecurityId = from.SecurityId,
			Bids = AddDelta(from.Bids, delta.Bids, true),
			Asks = AddDelta(from.Asks, delta.Asks, false),
			ServerTime = delta.ServerTime,
		};
	}

	/// <summary>
	/// To add change to quote.
	/// </summary>
	/// <param name="fromQuotes">Quotes.</param>
	/// <param name="deltaQuotes">Changes.</param>
	/// <param name="isBids">The indication of quotes direction.</param>
	/// <returns>Changed quotes.</returns>
	public static QuoteChange[] AddDelta(this IEnumerable<QuoteChange> fromQuotes, IEnumerable<QuoteChange> deltaQuotes, bool isBids)
	{
		var result = new List<QuoteChange>();

		using (var fromEnu = fromQuotes.GetEnumerator())
		{
			var hasFrom = fromEnu.MoveNext();

			foreach (var quoteChange in deltaQuotes)
			{
				var canAdd = true;

				while (hasFrom)
				{
					var current = fromEnu.Current;

					if (isBids)
					{
						if (current.Price > quoteChange.Price)
							result.Add(current);
						else if (current.Price == quoteChange.Price)
						{
							if (quoteChange.Volume != 0)
								result.Add(quoteChange);

							hasFrom = fromEnu.MoveNext();
							canAdd = false;

							break;
						}
						else
							break;
					}
					else
					{
						if (current.Price < quoteChange.Price)
							result.Add(current);
						else if (current.Price == quoteChange.Price)
						{
							if (quoteChange.Volume != 0)
								result.Add(quoteChange);

							hasFrom = fromEnu.MoveNext();
							canAdd = false;

							break;
						}
						else
							break;
					}

					hasFrom = fromEnu.MoveNext();
				}

				if (canAdd && quoteChange.Volume != 0)
					result.Add(quoteChange);
			}

			while (hasFrom)
			{
				result.Add(fromEnu.Current);
				hasFrom = fromEnu.MoveNext();
			}
		}

		return [.. result];
	}

	/// <summary>
	/// To merge the initial order book and its sparse representation.
	/// </summary>
	/// <param name="original">The initial order book.</param>
	/// <param name="rare">The sparse order book.</param>
	/// <returns>The merged order book.</returns>
	public static QuoteChangeMessage Join(this IOrderBookMessage original, IOrderBookMessage rare)
	{
		if (original is null)
			throw new ArgumentNullException(nameof(original));

		if (rare is null)
			throw new ArgumentNullException(nameof(rare));

		return new()
		{
			ServerTime = original.ServerTime,
			SecurityId = original.SecurityId,
			BuildFrom = DataType.MarketDepth,

			Bids = [.. original.Bids.Concat(rare.Bids).OrderByDescending(q => q.Price)],
			Asks = [.. original.Asks.Concat(rare.Asks).OrderBy(q => q.Price)],
		};
	}

	/// <summary>
	/// Get best pair.
	/// </summary>
	/// <param name="book"><see cref="IOrderBookMessage"/></param>
	/// <returns>Best pair.</returns>
	public static (QuoteChange? bid, QuoteChange? ask) GetBestPair(this IOrderBookMessage book)
		=> book.GetPair(0);

	private static QuoteChange? GetQuote(this IOrderBookMessage book, Sides side, int idx)
	{
		var quotes = side == Sides.Buy ? book.Bids : book.Asks;
		return quotes.Length > idx ? quotes[idx] : null;
	}

	/// <summary>
	/// To get a pair of quotes (bid + offer) by the depth index.
	/// </summary>
	/// <param name="book"><see cref="IOrderBookMessage"/></param>
	/// <param name="depthIndex">Depth index. Zero index means the best pair of quotes.</param>
	/// <returns>The pair of quotes. If the index is larger than book order depth, then the <see langword="null" /> is returned.</returns>
	public static (QuoteChange? bid, QuoteChange? ask) GetPair(this IOrderBookMessage book, int depthIndex)
	{
		if (book is null)
			throw new ArgumentNullException(nameof(book));

		if (depthIndex < 0)
			throw new ArgumentOutOfRangeException(nameof(depthIndex), depthIndex, LocalizedStrings.InvalidValue);

		var bid = book.GetQuote(Sides.Buy, depthIndex);
		var ask = book.GetQuote(Sides.Sell, depthIndex);

		if (bid is null && ask is null)
			return default;

		return new(bid, ask);
	}

	/// <summary>
	/// To get a pair of quotes for a given book depth.
	/// </summary>
	/// <param name="book"><see cref="IOrderBookMessage"/></param>
	/// <param name="depth">Book depth. The counting is from the best quotes.</param>
	/// <returns>Spread.</returns>
	public static IEnumerable<(QuoteChange? bid, QuoteChange? ask)> GetTopPairs(this IOrderBookMessage book, int depth)
	{
		if (book is null)
			throw new ArgumentNullException(nameof(book));

		if (depth < 0)
			throw new ArgumentOutOfRangeException(nameof(depth), depth, LocalizedStrings.InvalidValue);

		for (var i = 0; i < depth; i++)
		{
			var (bid, ask) = book.GetPair(i);

			if (bid is null && ask is null)
				break;

			yield return new(bid, ask);
		}
	}

	/// <summary>
	/// To get quotes for a given book depth.
	/// </summary>
	/// <param name="book"><see cref="IOrderBookMessage"/></param>
	/// <param name="depth">Book depth. Quotes are in order of price increasing from bids to offers.</param>
	/// <returns>Spread.</returns>
	public static IEnumerable<QuoteChange> GetTopQuotes(this IOrderBookMessage book, int depth)
	{
		if (book is null)
			throw new ArgumentNullException(nameof(book));

		if (depth < 0)
			throw new ArgumentOutOfRangeException(nameof(depth), depth, LocalizedStrings.InvalidValue);

		var retVal = new List<QuoteChange>();

		for (var i = depth - 1; i >= 0; i--)
		{
			var single = book.GetQuote(Sides.Buy, i);

			if (single is not null)
				retVal.Add(single.Value);
		}

		for (var i = 0; i < depth; i++)
		{
			var single = book.GetQuote(Sides.Sell, i);

			if (single is not null)
				retVal.Add(single.Value);
			else
				break;
		}

		return retVal;
	}

	/// <summary>
	/// To cut the price, to make it multiple of minimal step, also to limit number of signs after the comma.
	/// </summary>
	/// <param name="price">The price to be made multiple.</param>
	/// <param name="secMsg"><see cref="SecurityMessage"/></param>
	/// <param name="rule"></param>
	/// <returns>The multiple price.</returns>
	public static decimal ShrinkPrice(this decimal price, SecurityMessage secMsg, ShrinkRules rule = ShrinkRules.Auto)
	{
		if (secMsg is null)
			throw new ArgumentNullException(nameof(secMsg));

		return ShrinkPrice(price, secMsg.PriceStep, secMsg.Decimals, rule);
	}

	/// <summary>
	/// To cut the price, to make it multiple of minimal step, also to limit number of signs after the comma.
	/// </summary>
	/// <param name="price">The price to be made multiple.</param>
	/// <param name="priceStep">Price step.</param>
	/// <param name="decimals">Number of digits in price after coma.</param>
	/// <param name="rule"></param>
	/// <returns>The multiple price.</returns>
	public static decimal ShrinkPrice(this decimal price, decimal? priceStep, int? decimals, ShrinkRules rule = ShrinkRules.Auto)
	{
		var rounding = rule == ShrinkRules.Auto
			? MidpointRounding.ToEven
			: rule == ShrinkRules.Less ? MidpointRounding.AwayFromZero : MidpointRounding.ToEven;

		var result = price.Round(priceStep ?? 0.01m, decimals, rounding);

		return result.RemoveTrailingZeros();
	}

	/// <summary>
	/// Convert <see cref="ExecutionMessage"/> to <see cref="OrderRegisterMessage"/>.
	/// </summary>
	/// <param name="execMsg">The message contains information about the execution.</param>
	/// <returns>The message containing the information for the order registration.</returns>
	public static OrderRegisterMessage ToReg(this ExecutionMessage execMsg)
	{
		if (execMsg is null)
			throw new ArgumentNullException(nameof(execMsg));

		return new OrderRegisterMessage
		{
			SecurityId = execMsg.SecurityId,
			TransactionId = execMsg.TransactionId,
			Price = execMsg.OrderPrice,
			Volume = execMsg.Balance ?? execMsg.OrderVolume ?? 0L,
			Currency = execMsg.Currency,
			PortfolioName = execMsg.PortfolioName,
			ClientCode = execMsg.ClientCode,
			BrokerCode = execMsg.BrokerCode,
			Comment = execMsg.Comment,
			Side = execMsg.Side,
			TimeInForce = execMsg.TimeInForce,
			TillDate = execMsg.ExpiryDate,
			VisibleVolume = execMsg.VisibleVolume,
			LocalTime = execMsg.LocalTime,
			IsMarketMaker = execMsg.IsMarketMaker,
			MarginMode = execMsg.MarginMode,
			Slippage = execMsg.Slippage,
			IsManual = execMsg.IsManual,
			OrderType = execMsg.OrderType,
			UserOrderId = execMsg.UserOrderId,
			StrategyId = execMsg.StrategyId,
			Condition = execMsg.Condition?.Clone(),
			MinOrderVolume = execMsg.MinVolume,
			PositionEffect = execMsg.PositionEffect,
			PostOnly = execMsg.PostOnly,
			Leverage = execMsg.Leverage,
		};
	}

	/// <summary>
	/// Convert <see cref="OrderRegisterMessage"/> to <see cref="ExecutionMessage"/>.
	/// </summary>
	/// <param name="regMsg">The message containing the information for the order registration.</param>
	/// <returns>The message contains information about the execution.</returns>
	public static ExecutionMessage ToExec(this OrderRegisterMessage regMsg)
	{
		if (regMsg is null)
			throw new ArgumentNullException(nameof(regMsg));

		return new ExecutionMessage
		{
			ServerTime = DateTimeOffset.UtcNow,
			DataTypeEx = DataType.Transactions,
			SecurityId = regMsg.SecurityId,
			TransactionId = regMsg.TransactionId,
			OriginalTransactionId = regMsg.TransactionId,
			HasOrderInfo = true,
			OrderPrice = regMsg.Price,
			OrderVolume = regMsg.Volume,
			Currency = regMsg.Currency,
			PortfolioName = regMsg.PortfolioName,
			ClientCode = regMsg.ClientCode,
			BrokerCode = regMsg.BrokerCode,
			Comment = regMsg.Comment,
			Side = regMsg.Side,
			TimeInForce = regMsg.TimeInForce,
			ExpiryDate = regMsg.TillDate,
			Balance = regMsg.Volume,
			VisibleVolume = regMsg.VisibleVolume,
			LocalTime = regMsg.LocalTime,
			IsMarketMaker = regMsg.IsMarketMaker,
			MarginMode = regMsg.MarginMode,
			Slippage = regMsg.Slippage,
			IsManual = regMsg.IsManual,
			OrderType = regMsg.OrderType,
			UserOrderId = regMsg.UserOrderId,
			StrategyId = regMsg.StrategyId,
			OrderState = OrderStates.Pending,
			Condition = regMsg.Condition?.Clone(),
			MinVolume = regMsg.MinOrderVolume,
			PositionEffect = regMsg.PositionEffect,
			PostOnly = regMsg.PostOnly,
			Leverage = regMsg.Leverage,
		};
	}

	/// <summary>
	/// Determines the specified message contains historical request only.
	/// </summary>
	/// <param name="message">Subscription.</param>
	/// <returns>Check result.</returns>
	public static bool IsHistoryOnly(this ISubscriptionMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		if (!message.IsSubscribe)
			throw new ArgumentException(message.ToString());

		return message.To != null || message.Count != null;
	}

	/// <summary>
	/// Filter boards by code criteria.
	/// </summary>
	/// <param name="boards">All boards.</param>
	/// <param name="criteria">Criteria.</param>
	/// <returns>Found boards.</returns>
	public static IEnumerable<BoardMessage> Filter(this IEnumerable<BoardMessage> boards, BoardLookupMessage criteria)
		=> boards.Where(b => b.IsMatch(criteria));

	/// <summary>
	///
	/// </summary>
	/// <param name="parameters"></param>
	/// <param name="name"></param>
	/// <param name="defaultValue"></param>
	/// <returns></returns>
	public static string TryGet(this IDictionary<string, string> parameters, string name, string defaultValue = default)
	{
		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (parameters.TryGetValue(name, out var value))
			return value;

		return defaultValue;
	}

	/// <summary>
	/// To change the direction to opposite.
	/// </summary>
	/// <param name="side">The initial direction.</param>
	/// <returns>The opposite direction.</returns>
	public static Sides Invert(this Sides side)
		=> side == Sides.Buy ? Sides.Sell : Sides.Buy;

	/// <summary>
	/// To check, whether the order was cancelled.
	/// </summary>
	/// <param name="order">The order to be checked.</param>
	/// <returns><see langword="true" />, if the order is cancelled, otherwise, <see langword="false" />.</returns>
	public static bool IsCanceled(this IOrderMessage order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return order.State == OrderStates.Done && order.Balance > 0;
	}

	/// <summary>
	/// To check, is the order matched completely.
	/// </summary>
	/// <param name="order">The order to be checked.</param>
	/// <returns><see langword="true" />, if the order is matched completely, otherwise, <see langword="false" />.</returns>
	public static bool IsMatched(this IOrderMessage order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return order.State == OrderStates.Done && order.Balance == 0;
	}

	/// <summary>
	/// To check, is a part of volume is implemented in the order.
	/// </summary>
	/// <param name="order">The order to be checked.</param>
	/// <returns><see langword="true" />, if part of volume is implemented, otherwise, <see langword="false" />.</returns>
	public static bool IsMatchedPartially(this IOrderMessage order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return order.Balance > 0 && order.Volume > 0 && order.Balance != order.Volume;
	}

	/// <summary>
	/// To check, if no contract in order is implemented.
	/// </summary>
	/// <param name="order">The order to be checked.</param>
	/// <returns><see langword="true" />, if no contract is implemented, otherwise, <see langword="false" />.</returns>
	public static bool IsMatchedEmpty(this IOrderMessage order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return order.Balance > 0 && order.Volume > 0 && order.Balance == order.Volume;
	}

	/// <summary>
	/// To calculate the implemented part of volume for order.
	/// </summary>
	/// <param name="order">The order, for which the implemented part of volume shall be calculated.</param>
	/// <returns>The implemented part of volume.</returns>
	public static decimal? GetMatchedVolume(this IOrderMessage order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return order.Volume - order.Balance;
	}

	/// <summary>
	/// Is specified security is basket.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <returns>Check result.</returns>
	public static bool IsBasket(this SecurityMessage security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		return !security.BasketCode.IsEmpty();
	}

	/// <summary>
	/// Is specified security is index.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <returns>Check result.</returns>
	public static bool IsIndex(this SecurityMessage security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		return security.BasketCode is "WI" or "EI";
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to readable string.
	/// </summary>
	/// <param name="dt"><see cref="DataType"/> instance.</param>
	/// <returns>Readable string.</returns>
	public static string ToReadableString(this DataType dt)
	{
		if (dt == null)
			throw new ArgumentNullException(nameof(dt));

		var tf = dt.GetTimeFrame();

		var str = string.Empty;

		if (tf.Days > 0)
			str += LocalizedStrings.DaysParams.Put(tf.Days);

		if (tf.Hours > 0)
			str = (str + " " + LocalizedStrings.HoursParams.Put(tf.Hours)).Trim();

		if (tf.Minutes > 0)
			str = (str + " " + LocalizedStrings.MinsParams.Put(tf.Minutes)).Trim();

		if (tf.Seconds > 0)
			str = (str + " " + LocalizedStrings.Seconds.Put(tf.Seconds)).Trim();

		if (str.IsEmpty())
			str = LocalizedStrings.Ticks;

		return str;
	}

	/// <summary>
	/// To get the type for the instrument in the ISO 10962 standard.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <returns>Type in ISO 10962 standard.</returns>
	public static string Iso10962(this SecurityMessage security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		// https://en.wikipedia.org/wiki/ISO_10962

		switch (security.SecurityType)
		{
			case SecurityTypes.Stock:
				return "ESXXXX";
			case SecurityTypes.Future:
				return "FFXXXX";
			case SecurityTypes.Option:
			{
				return security.OptionType switch
				{
					OptionTypes.Call => "OCXXXX",
					OptionTypes.Put => "OPXXXX",
					null => "OXXXXX",
					_ => throw new ArgumentOutOfRangeException(nameof(security), security.OptionType, LocalizedStrings.InvalidValue),
				};
			}
			case SecurityTypes.Index:
				return "MRIXXX";
			case SecurityTypes.Currency:
				return "MRCXXX";
			case SecurityTypes.Bond:
				return "DBXXXX";
			case SecurityTypes.Warrant:
				return "RWXXXX";
			case SecurityTypes.Forward:
				return "FFMXXX";
			case SecurityTypes.Swap:
				return "FFWXXX";
			case SecurityTypes.Commodity:
				return "MRTXXX";
			case SecurityTypes.Cfd:
				return "MMCXXX";
			case SecurityTypes.Adr:
				return "MMAXXX";
			case SecurityTypes.News:
				return "MMNXXX";
			case SecurityTypes.Weather:
				return "MMWXXX";
			case SecurityTypes.Fund:
				return "EUXXXX";
			case SecurityTypes.CryptoCurrency:
				return "MMBXXX";
			case null:
				return "XXXXXX";
			default:
				throw new ArgumentOutOfRangeException(nameof(security), security.SecurityType, LocalizedStrings.InvalidValue);
		}
	}

	/// <summary>
	/// To convert the type in the ISO 10962 standard into <see cref="SecurityTypes"/>.
	/// </summary>
	/// <param name="cfi">Type in ISO 10962 standard.</param>
	/// <returns>Security type.</returns>
	public static SecurityTypes? Iso10962ToSecurityType(this string cfi)
	{
		if (cfi.IsEmpty())
		{
			return null;
			//throw new ArgumentNullException(nameof(cfi));
		}

		if (cfi.Length != 6)
		{
			return null;
			//throw new ArgumentOutOfRangeException(nameof(cfi), cfi, LocalizedStrings.InvalidValue);
		}

		switch (cfi[0])
		{
			case 'E':
				return SecurityTypes.Stock;

			case 'D':
				return SecurityTypes.Bond;

			case 'R':
				return SecurityTypes.Warrant;

			case 'O':
				return SecurityTypes.Option;

			case 'F':
			{
				return cfi[2] switch
				{
					'W' => SecurityTypes.Swap,
					'M' => SecurityTypes.Forward,
					_ => SecurityTypes.Future,
				};
			}

			case 'M':
			{
				switch (cfi[1])
				{
					case 'R':
					{
						switch (cfi[2])
						{
							case 'I':
								return SecurityTypes.Index;

							case 'C':
								return SecurityTypes.Currency;

							case 'R':
								return SecurityTypes.Currency;

							case 'T':
								return SecurityTypes.Commodity;
						}

						break;
					}

					case 'M':
					{
						switch (cfi[2])
						{
							case 'B':
								return SecurityTypes.CryptoCurrency;

							case 'W':
								return SecurityTypes.Weather;

							case 'A':
								return SecurityTypes.Adr;

							case 'C':
								return SecurityTypes.Cfd;

							case 'N':
								return SecurityTypes.News;
						}

						break;
					}
				}

				break;
			}
		}

		return null;
	}

	/// <summary>
	/// To convert the type in the ISO 10962 standard into <see cref="OptionTypes"/>.
	/// </summary>
	/// <param name="cfi">Type in ISO 10962 standard.</param>
	/// <returns>Option type.</returns>
	public static OptionTypes? Iso10962ToOptionType(this string cfi)
	{
		if (cfi.IsEmpty())
			throw new ArgumentNullException(nameof(cfi));

		if (cfi[0] != 'O')
			return null;

		if (cfi.Length < 2)
			throw new ArgumentOutOfRangeException(nameof(cfi), cfi, LocalizedStrings.InvalidValue);

		return cfi[1] switch
		{
			'C' => OptionTypes.Call,
			'P' => OptionTypes.Put,
			'X' or ' ' => null,
			_ => throw new ArgumentOutOfRangeException(nameof(cfi), LocalizedStrings.UnknownType.Put(cfi)),
		};
	}

	private static readonly SecurityIdGenerator _defaultGenerator = new();

	/// <summary>
	/// Returns the specified generator or the default in case of <see langword="null"/>.
	/// </summary>
	/// <param name="generator"><see cref="SecurityIdGenerator"/></param>
	/// <returns><see cref="SecurityIdGenerator"/></returns>
	public static SecurityIdGenerator EnsureGetGenerator(this SecurityIdGenerator generator) => generator ?? _defaultGenerator;

	/// <summary>
	/// Convert <see cref="SecurityId"/> to <see cref="SecurityId"/> value.
	/// </summary>
	/// <param name="securityId"><see cref="SecurityId"/> value.</param>
	/// <param name="generator">The instrument identifiers generator <see cref="SecurityId"/>. Can be <see langword="null"/>.</param>
	/// <param name="nullIfEmpty">Return <see langword="null"/> if <see cref="SecurityId"/> is empty.</param>
	/// <returns><see cref="SecurityId"/> value.</returns>
	public static string ToStringId(this SecurityId securityId, SecurityIdGenerator generator = null, bool nullIfEmpty = false)
	{
		var secCode = securityId.SecurityCode;
		var boardCode = securityId.BoardCode;

		if (nullIfEmpty)
		{
			if (secCode.IsEmpty() || boardCode.IsEmpty())
				return null;
		}

		return generator.EnsureGetGenerator().GenerateId(secCode, boardCode);
	}

	/// <summary>
	/// "All securities" id.
	/// </summary>
	public static readonly string AllSecurityId = $"{SecurityId.AssociatedBoardCode}@{SecurityId.AssociatedBoardCode}";

	/// <summary>
	/// Convert <see cref="string"/> to <see cref="SecurityId"/> value.
	/// </summary>
	/// <param name="id"><see cref="string"/> value.</param>
	/// <param name="generator">The instrument identifiers generator <see cref="SecurityId"/>. Can be <see langword="null"/>.</param>
	/// <returns><see cref="SecurityId"/> value.</returns>
	public static SecurityId ToNullableSecurityId(this string id, SecurityIdGenerator generator = null)
	{
		return id.IsEmpty() ? default : id.ToSecurityId(generator);
	}

	/// <summary>
	/// Convert <see cref="string"/> to <see cref="SecurityId"/> value.
	/// </summary>
	/// <param name="id"><see cref="string"/> value.</param>
	/// <param name="generator">The instrument identifiers generator <see cref="SecurityId"/>. Can be <see langword="null"/>.</param>
	/// <returns><see cref="SecurityId"/> value.</returns>
	public static SecurityId ToSecurityId(this string id, SecurityIdGenerator generator = null)
	{
		if (id.EqualsIgnoreCase(AllSecurityId))
			return default;

		return generator.EnsureGetGenerator().Split(id);
	}

	/// <summary>
	/// Is specified security id associated with the board.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="boardCode">Board code.</param>
	/// <returns><see langword="true" />, if associated, otherwise, <see langword="false"/>.</returns>
	public static bool IsAssociated(this SecurityId securityId, string boardCode)
	{
		if (boardCode.IsEmpty())
			throw new ArgumentNullException(nameof(boardCode));

		return securityId.BoardCode.EqualsIgnoreCase(boardCode);
	}

	/// <summary>
	/// Fill <see cref="SecurityMessage.UnderlyingSecurityId"/> property.
	/// </summary>
	/// <param name="secMsg"><see cref="SecurityMessage"/></param>
	/// <param name="underlyingCode">Underlying asset id.</param>
	/// <returns><see cref="SecurityMessage"/></returns>
	public static SecurityMessage TryFillUnderlyingId(this SecurityMessage secMsg, string underlyingCode)
	{
		if (!underlyingCode.IsEmpty())
			secMsg.UnderlyingSecurityId = new() { SecurityCode = underlyingCode, BoardCode = secMsg.SecurityId.BoardCode };

		return secMsg;
	}

	/// <summary>
	/// Get underlying asset.
	/// </summary>
	/// <param name="secMsg"><see cref="SecurityMessage"/></param>
	/// <returns>Underlying asset.</returns>
	public static string GetUnderlyingCode(this SecurityMessage secMsg)
		=> secMsg.UnderlyingSecurityId.SecurityCode;

	/// <summary>
	/// To get the number of operations, or discard the exception, if no information available.
	/// </summary>
	/// <param name="message">Operations.</param>
	/// <returns>Quantity.</returns>
	public static decimal SafeGetVolume(this ExecutionMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var volume = message.OrderVolume ?? message.TradeVolume;

		if (volume != null)
			return volume.Value;

		var errorMsg = message.DataType == DataType.Ticks || message.HasTradeInfo()
			? LocalizedStrings.WrongTradeVolume.Put((object)message.TradeId ?? message.TradeStringId)
			: LocalizedStrings.WrongOrderVolume.Put((object)message.OrderId ?? message.OrderStringId);

		throw new ArgumentOutOfRangeException(nameof(message), null, errorMsg);
	}

	/// <summary>
	/// To get order identifier, or discard exception, if no information available.
	/// </summary>
	/// <param name="message">Operations.</param>
	/// <returns>Order ID.</returns>
	public static long SafeGetOrderId(this ExecutionMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var orderId = message.OrderId;

		if (orderId != null)
			return orderId.Value;

		throw new ArgumentOutOfRangeException(nameof(message), null, LocalizedStrings.InvalidValue);
	}

	/// <summary>
	/// Is the specified state is final (<see cref="OrderStates.Done"/> or <see cref="OrderStates.Failed"/>).
	/// </summary>
	/// <param name="state">Order state.</param>
	/// <returns>Check result.</returns>
	public static bool IsFinal(this OrderStates state)
		=> state is OrderStates.Done or OrderStates.Failed;

	/// <summary>
	/// Extract <see cref="TimeInForce"/> from bits flag.
	/// </summary>
	/// <param name="status">Bits flag.</param>
	/// <returns><see cref="TimeInForce"/>.</returns>
	public static TimeInForce? GetPlazaTimeInForce(this long status)
	{
		if (status.HasBits(0x1))
			return TimeInForce.PutInQueue;
		else if (status.HasBits(0x2))
			return TimeInForce.CancelBalance;
		else if (status.HasBits(0x80000))
			return TimeInForce.MatchOrCancel;

		return null;
	}

	/// <summary>
	/// Extract system attribute from the bits flag.
	/// </summary>
	/// <param name="status">Bits flag.</param>
	/// <returns><see langword="true"/> if an order is system, otherwise, <see langword="false"/>.</returns>
	public static bool IsPlazaSystem(this long status)
	{
		return !status.HasBits(0x4);
	}

	/// <summary>
	/// To get the reason for cancelling order in orders log.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns>The reason for order cancelling in order log.</returns>
	public static OrderLogCancelReasons GetOrderLogCancelReason(this ExecutionMessage item)
	{
		if (!item.IsOrderLogCanceled())
			throw new ArgumentException(LocalizedStrings.OrderLogIsNotCancellation, nameof(item));

		if (item.OrderStatus == null)
			throw new ArgumentException(LocalizedStrings.OrderLogNotStatus, nameof(item));

		var status = item.OrderStatus.Value;

		if (status.HasBits(0x100000))
			return OrderLogCancelReasons.ReRegistered;
		else if (status.HasBits(0x200000))
			return OrderLogCancelReasons.Canceled;
		else if (status.HasBits(0x400000))
			return OrderLogCancelReasons.GroupCanceled;
		else if (status.HasBits(0x800000))
			return OrderLogCancelReasons.CrossTrade;
		else
			throw new ArgumentOutOfRangeException(nameof(item), status, LocalizedStrings.InvalidValue);
	}

	private static readonly WorkingTime _time = new() { IsEnabled = true };

	/// <summary>
	/// To get candle time frames relatively to the exchange working pattern.
	/// </summary>
	/// <param name="timeFrame">The time frame for which you need to get time range.</param>
	/// <param name="currentTime">The current time within the range of time frames.</param>
	/// <returns>The candle time frames.</returns>
	public static Range<DateTimeOffset> GetCandleBounds(this TimeSpan timeFrame, DateTimeOffset currentTime)
		=> GetCandleBounds(timeFrame, currentTime, TimeZoneInfo.Utc, _time);

	private static readonly long _weekTf = TimeSpan.FromDays(7).Ticks;

	/// <summary>
	/// To get candle time frames relatively to the exchange working pattern.
	/// </summary>
	/// <param name="timeFrame">The time frame for which you need to get time range.</param>
	/// <param name="currentTime">The current time within the range of time frames.</param>
	/// <param name="timeZone">Information about the time zone where the exchange is located.</param>
	/// <param name="time">The information about the exchange working pattern.</param>
	/// <returns>The candle time frames.</returns>
	public static Range<DateTimeOffset> GetCandleBounds(this TimeSpan timeFrame, DateTimeOffset currentTime, TimeZoneInfo timeZone, WorkingTime time)
	{
		if (timeZone == null)
			throw new ArgumentNullException(nameof(timeZone));

		if (time == null)
			throw new ArgumentNullException(nameof(time));

		var exchangeTime = currentTime.ToLocalTime(timeZone);
		Range<DateTime> bounds;

		if (timeFrame.Ticks == _weekTf)
		{
			var monday = exchangeTime.StartOfWeek(DayOfWeek.Monday);

			var endDay = exchangeTime.Date;

			while (endDay.DayOfWeek != DayOfWeek.Sunday)
			{
				var nextDay = endDay.AddDays(1);

				if (nextDay.Month != endDay.Month)
					break;

				endDay = nextDay;
			}

			bounds = new Range<DateTime>(monday, endDay.EndOfDay());
		}
		else if (timeFrame.Ticks == TimeHelper.TicksPerMonth)
		{
			var month = new DateTime(exchangeTime.Year, exchangeTime.Month, 1);
			bounds = new Range<DateTime>(month, (month + TimeSpan.FromDays(month.DaysInMonth())).EndOfDay());
		}
		else
		{
			var period = time.GetPeriod(exchangeTime);

			// http://stocksharp.com/forum/yaf_postsm13887_RealtimeEmulationTrader---niepravil-nyie-sviechi.aspx#post13887
			// отсчет свечек идет от начала сессии и игнорируются клиринги
			var startTime = period != null && period.Times.Count > 0 ? period.Times[0].Min : TimeSpan.Zero;

			var length = (exchangeTime.TimeOfDay - startTime).To<long>();
			var beginTime = exchangeTime.Date + (startTime + length.Floor(timeFrame.Ticks).To<TimeSpan>());

			//последняя свеча должна заканчиваться в конец торговой сессии
			var tempEndTime = beginTime.TimeOfDay + timeFrame;
			TimeSpan stopTime;

			if (period != null && period.Times.Count > 0)
			{
				var last = period.Times.LastOrDefault(t => tempEndTime > t.Min);
				stopTime = last == null ? TimeSpan.MaxValue : last.Max;
			}
			else
				stopTime = TimeSpan.MaxValue;

			var endTime = beginTime + timeFrame.Min(stopTime - beginTime.TimeOfDay);

			// если currentTime попало на клиринг
			if (endTime < beginTime)
				endTime = beginTime.Date + tempEndTime;

			var days = timeFrame.Days > 1 ? timeFrame.Days - 1 : 0;

			var min = beginTime.Truncate(TimeSpan.TicksPerMillisecond);
			var max = endTime.Truncate(TimeSpan.TicksPerMillisecond).AddDays(days);

			bounds = new Range<DateTime>(min, max);
		}

		var offset = currentTime.Offset;
		var diff = currentTime.DateTime - exchangeTime;

		return new Range<DateTimeOffset>(
			(bounds.Min + diff).ApplyTimeZone(offset),
			(bounds.Max + diff).ApplyTimeZone(offset));
	}

	private static readonly WorkingTime _allRange = new();

	/// <summary>
	/// To get the number of time frames within the specified time range.
	/// </summary>
	/// <param name="range">The specified time range for which you need to get the number of time frames.</param>
	/// <param name="timeFrame">The time frame size.</param>
	/// <param name="workingTime"><see cref="WorkingTime"/>.</param>
	/// <param name="timeZone">Information about the time zone where the exchange is located.</param>
	/// <returns>The received number of time frames.</returns>
	public static long GetTimeFrameCount(this Range<DateTimeOffset> range, TimeSpan timeFrame, WorkingTime workingTime = default, TimeZoneInfo timeZone = default)
	{
		if (range is null)
			throw new ArgumentNullException(nameof(range));

		workingTime ??= _allRange;
		timeZone ??= TimeZoneInfo.Utc;

		var to = range.Max.ToLocalTime(timeZone);
		var from = range.Min.ToLocalTime(timeZone);

		var days = (int)(to.Date - from.Date).TotalDays;

		var period = workingTime.GetPeriod(from);

		if (period == null || period.Times.IsEmpty())
		{
			return (to - from).Ticks / timeFrame.Ticks;
		}

		if (days == 0)
		{
			return workingTime.GetTimeFrameCount(from, new Range<TimeSpan>(from.TimeOfDay, to.TimeOfDay), timeFrame);
		}

		var totalCount = workingTime.GetTimeFrameCount(from, new Range<TimeSpan>(from.TimeOfDay, TimeHelper.LessOneDay), timeFrame);
		totalCount += workingTime.GetTimeFrameCount(to, new Range<TimeSpan>(TimeSpan.Zero, to.TimeOfDay), timeFrame);

		if (days <= 1)
			return totalCount;

		var fullDayLength = period.Times.Sum(r => r.Length.Ticks);
		totalCount += TimeSpan.FromTicks((days - 1) * fullDayLength).Ticks / timeFrame.Ticks;

		return totalCount;
	}

	private static long GetTimeFrameCount(this WorkingTime workingTime, DateTime date, Range<TimeSpan> fromToRange, TimeSpan timeFrame)
	{
		if (workingTime is null)
			throw new ArgumentNullException(nameof(workingTime));

		if (fromToRange is null)
			throw new ArgumentNullException(nameof(fromToRange));

		var period = workingTime.GetPeriod(date);

		if (period == null)
			return 0;

		return period.Times
					.Select(fromToRange.Intersect)
					.WhereNotNull()
					.Sum(intersection => intersection.Length.Ticks / timeFrame.Ticks);
	}

	/// <summary>
	/// To get the instrument by the instrument code.
	/// </summary>
	/// <param name="provider">The provider of information about instruments.</param>
	/// <param name="code">Security code.</param>
	/// <param name="type">Security type.</param>
	/// <returns>The got instrument. If there is no instrument by given criteria, <see langword="null" /> is returned.</returns>
	public static IEnumerable<SecurityMessage> LookupByCode(this ISecurityMessageProvider provider, string code, SecurityTypes? type = null)
	{
		if (provider is null)
			throw new ArgumentNullException(nameof(provider));

		return code.IsEmpty() && type == null
			? provider.LookupMessages(LookupAllCriteriaMessage)
			: provider.LookupMessages(new() { SecurityId = new() { SecurityCode = code }, SecurityType = type });
	}

	/// <summary>
	/// get icon uri
	/// </summary>
	public static Uri MakeVectorIconUri(this string key) => new($"pack://application:,,,/StockSharp.Xaml;component/IconsSvg/{key}.svg");

	/// <summary>
	/// Try get <see cref="VectorIconAttribute.Icon"/> path.
	/// </summary>
	/// <param name="type">Component type with applied <see cref="VectorIconAttribute"/>.</param>
	/// <returns>Icon url.</returns>
	public static Uri TryGetVectorIcon(this Type type)
	{
		var attr = type.GetAttribute<VectorIconAttribute>();

		if (attr is null)
			return null;

		return MakeVectorIconUri(attr.Icon);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="type"></param>
	/// <returns>Icon url.</returns>
	public static Uri TryGetIconUrl(this Type type)
		=> type.GetIconUrl() ?? type.TryGetVectorIcon();

	/// <summary>
	/// Get typed <see cref="CommandMessage.ObjectId"/>.
	/// </summary>
	/// <typeparam name="T">Type of <see cref="CommandMessage.ObjectId"/>.</typeparam>
	/// <param name="message"><see cref="CommandMessage"/>.</param>
	/// <returns>Typed <see cref="CommandMessage.ObjectId"/>.</returns>
	public static T GetId<T>(this CommandMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		return message.ObjectId.To<T>();
	}

	/// <summary>
	/// Set typed <see cref="CommandMessage.ObjectId"/>.
	/// </summary>
	/// <typeparam name="T">Type of <see cref="CommandMessage.ObjectId"/>.</typeparam>
	/// <param name="message"><see cref="CommandMessage"/>.</param>
	/// <param name="id">Typed <see cref="CommandMessage.ObjectId"/>.</param>
	public static void SetId<T>(this CommandMessage message, T id)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		message.ObjectId = id.To<string>();
	}

	/// <summary>
	/// To check, whether the time is traded (has the session started, ended, is there a clearing).
	/// </summary>
	/// <param name="board">Board info.</param>
	/// <param name="time">The passed time to be checked.</param>
	/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
	public static bool IsTradeTime(this BoardMessage board, DateTimeOffset time)
	{
		return board.IsTradeTime(time, out _, out _);
	}

	/// <summary>
	/// To check, whether the time is traded (has the session started, ended, is there a clearing).
	/// </summary>
	/// <param name="board">Board info.</param>
	/// <param name="time">The passed time to be checked.</param>
	/// <param name="isWorkingDay"><see langword="true" />, if the date is traded, otherwise, is not traded.</param>
	/// <param name="period">Current working time period.</param>
	/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
	public static bool IsTradeTime(this BoardMessage board, DateTimeOffset time, out bool? isWorkingDay, out WorkingTimePeriod period)
	{
		if (board is null)
			throw new ArgumentNullException(nameof(board));

		var exchangeTime = time.ToLocalTime(board.TimeZone);
		var workingTime = board.WorkingTime;

		return workingTime.IsTradeTime(exchangeTime, out isWorkingDay, out period);
	}

	/// <summary>
	/// To check, whether the time is traded (has the session started, ended, is there a clearing).
	/// </summary>
	/// <param name="workingTime">Board working hours.</param>
	/// <param name="time">The passed time to be checked.</param>
	/// <param name="isWorkingDay"><see langword="true" />, if the date is traded, otherwise, is not traded.</param>
	/// <param name="period">Current working time period.</param>
	/// <returns><see langword="true" />, if time is traded, otherwise, not traded.</returns>
	public static bool IsTradeTime(this WorkingTime workingTime, DateTime time, out bool? isWorkingDay, out WorkingTimePeriod period)
	{
		if (workingTime is null)
			throw new ArgumentNullException(nameof(workingTime));

		period = null;
		isWorkingDay = null;

		if (!workingTime.IsEnabled)
			return true;

		isWorkingDay = workingTime.IsTradeDate(time);

		if (isWorkingDay == false)
			return false;

		period = workingTime.GetPeriod(time);

		var tod = time.TimeOfDay;
		return period == null || period.Times.IsEmpty() || period.Times.Any(r => r.Contains(tod));
	}

	/// <summary>
	/// To check, whether date is traded.
	/// </summary>
	/// <param name="board">Board info.</param>
	/// <param name="date">The passed date to be checked.</param>
	/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
	/// <returns><see langword="true" />, if the date is traded, otherwise, is not traded.</returns>
	public static bool IsTradeDate(this BoardMessage board, DateTimeOffset date, bool checkHolidays = false)
	{
		if (board is null)
			throw new ArgumentNullException(nameof(board));

		var exchangeTime = date.ToLocalTime(board.TimeZone);
		var workingTime = board.WorkingTime;

		return workingTime.IsTradeDate(exchangeTime, checkHolidays);
	}

	/// <summary>
	/// To check, whether date is traded.
	/// </summary>
	/// <param name="workingTime">Board working hours.</param>
	/// <param name="date">The passed date to be checked.</param>
	/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
	/// <returns><see langword="true" />, if the date is traded, otherwise, is not traded.</returns>
	public static bool IsTradeDate(this WorkingTime workingTime, DateTime date, bool checkHolidays = false)
	{
		var period = workingTime.GetPeriod(date);

		if ((period == null || period.Times.Count == 0) && workingTime.SpecialWorkingDays.Length == 0 && workingTime.SpecialHolidays.Length == 0)
			return true;

		bool isWorkingDay;

		if (checkHolidays && (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday))
			isWorkingDay = workingTime.SpecialWorkingDays.Contains(date.Date);
		else
			isWorkingDay = !workingTime.SpecialHolidays.Contains(date.Date);

		return isWorkingDay;
	}

	/// <summary>
	/// Get last trade date.
	/// </summary>
	/// <param name="board">Board info.</param>
	/// <param name="date">The date from which to start checking.</param>
	/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
	/// <returns>Last trade date.</returns>
	public static DateTimeOffset LastTradeDay(this BoardMessage board, DateTimeOffset date, bool checkHolidays = true)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		while (!board.IsTradeDate(date, checkHolidays))
			date = date.AddDays(-1);

		return date;
	}

	/// <summary>
	/// To get date of day T +/- of N trading days.
	/// </summary>
	/// <param name="board">Board info.</param>
	/// <param name="date">The start T date, to which are added or subtracted N trading days.</param>
	/// <param name="n">The N size. The number of trading days for the addition or subtraction.</param>
	/// <param name="checkHolidays">Whether to check the passed date for a weekday (Saturday and Sunday are days off, returned value for them is <see langword="false" />).</param>
	/// <returns>The end T +/- N date.</returns>
	public static DateTimeOffset AddOrSubtractTradingDays(this BoardMessage board, DateTimeOffset date, int n, bool checkHolidays = true)
	{
		if (board == null)
			throw new ArgumentNullException(nameof(board));

		while (n != 0)
		{
			//if need to Add
			if (n > 0)
			{
				date = date.AddDays(1);
				if (board.IsTradeDate(date, checkHolidays)) n--;
			}
			//if need to Subtract
			if (n < 0)
			{
				date = date.AddDays(-1);
				if (board.IsTradeDate(date, checkHolidays)) n++;
			}
		}

		return date;
	}

	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="set"></param>
	/// <param name="msg"></param>
	/// <returns></returns>
	public static IEnumerable<T> TryLimitByCount<T>(this IEnumerable<T> set, SecurityLookupMessage msg)
	{
		if (set is null)
			throw new ArgumentNullException(nameof(set));

		if (msg is null)
			throw new ArgumentNullException(nameof(msg));

		if (msg.Count is not null)
			set = set.Take((int)msg.Count.Value);

		return set;
	}

	/// <summary>
	/// To filter messages for the given time period.
	/// </summary>
	/// <param name="messages">All messages, in which the required shall be searched for.</param>
	/// <param name="from">The start date for searching.</param>
	/// <param name="to">The end date for searching.</param>
	/// <returns>Filtered messages.</returns>
	public static IEnumerable<TMessage> Filter<TMessage>(this IEnumerable<TMessage> messages, DateTimeOffset from, DateTimeOffset to)
		where TMessage : IServerTimeMessage
	{
		if (messages == null)
			throw new ArgumentNullException(nameof(messages));

		return messages.Where(trade => trade.ServerTime >= from && trade.ServerTime < to);
	}

	private sealed class OrderLogTickEnumerable : SimpleEnumerable<ExecutionMessage>//, IEnumerableEx<ExecutionMessage>
	{
		private sealed class OrderLogTickEnumerator : IEnumerator<ExecutionMessage>
		{
			private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;

			private readonly HashSet<long> _tradesByNum = [];
			private readonly HashSet<string> _tradesByString = new(StringComparer.InvariantCultureIgnoreCase);

			public OrderLogTickEnumerator(IEnumerable<ExecutionMessage> items)
			{
				if (items == null)
					throw new ArgumentNullException(nameof(items));

				_itemsEnumerator = items.GetEnumerator();
			}

			public ExecutionMessage Current { get; private set; }

			bool IEnumerator.MoveNext()
			{
				while (_itemsEnumerator.MoveNext())
				{
					var currItem = _itemsEnumerator.Current;

					if (currItem.TradeId != null)
					{
						if (TryProcess(currItem.TradeId.Value, _tradesByNum, currItem))
							return true;
					}
					else if (!currItem.TradeStringId.IsEmpty())
					{
						if (TryProcess(currItem.TradeStringId, _tradesByString, currItem))
							return true;
					}
				}

				Current = null;
				return false;
			}

			private bool TryProcess<T>(T tradeId, HashSet<T> trades, ExecutionMessage currItem)
			{
				if (!trades.Add(tradeId))
					return false;

				trades.Remove(tradeId);
				Current = currItem.ToTick();
				return true;
			}

			void IEnumerator.Reset()
			{
				_itemsEnumerator.Reset();

				_tradesByNum.Clear();
				_tradesByString.Clear();

				Current = null;
			}

			object IEnumerator.Current => Current;

			void IDisposable.Dispose()
			{
				Current = null;
				_itemsEnumerator.Dispose();

				GC.SuppressFinalize(this);
			}
		}

		//private readonly IEnumerable<ExecutionMessage> _items;

		public OrderLogTickEnumerable(IEnumerable<ExecutionMessage> items)
			: base(() => new OrderLogTickEnumerator(items))
		{
			if (items == null)
				throw new ArgumentNullException(nameof(items));

			//_items = items;
		}

		//int IEnumerableEx.Count => _items.Count;
	}

	/// <summary>
	/// To tick trade from the order log.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns>Tick trade.</returns>
	public static ExecutionMessage ToTick(this ExecutionMessage item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));

		if (item.DataType != DataType.OrderLog)
			throw new ArgumentException(nameof(item));

		return new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			SecurityId = item.SecurityId,
			TradeId = item.TradeId,
			TradeStringId = item.TradeStringId,
			TradePrice = item.TradePrice,
			TradeStatus = item.TradeStatus,
			TradeVolume = item.OrderVolume,
			ServerTime = item.ServerTime,
			LocalTime = item.LocalTime,
			IsSystem = item.IsSystem,
			OpenInterest = item.OpenInterest,
			OriginSide = item.OriginSide,
			//OriginSide = prevItem.Item2 == Sides.Buy
			//	? (prevItem.Item1 > item.OrderId ? Sides.Buy : Sides.Sell)
			//	: (prevItem.Item1 > item.OrderId ? Sides.Sell : Sides.Buy),
			BuildFrom = DataType.OrderLog,
		};
	}

	/// <summary>
	/// To build tick trades from the orders log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <returns>Tick trades.</returns>
	public static IEnumerable<ExecutionMessage> ToTicks(this IEnumerable<ExecutionMessage> items)
	{
		return new OrderLogTickEnumerable(items);
	}

	private sealed class TickLevel1Enumerable : SimpleEnumerable<Level1ChangeMessage>
	{
		private sealed class TickLevel1Enumerator : IEnumerator<Level1ChangeMessage>
		{
			private readonly IEnumerator<ExecutionMessage> _itemsEnumerator;

			public TickLevel1Enumerator(IEnumerable<ExecutionMessage> items)
			{
				if (items is null)
					throw new ArgumentNullException(nameof(items));

				_itemsEnumerator = items.GetEnumerator();
			}

			public Level1ChangeMessage Current { get; private set; }

			bool IEnumerator.MoveNext()
			{
				while (_itemsEnumerator.MoveNext())
				{
					var tick = _itemsEnumerator.Current;

					var l1Msg = new Level1ChangeMessage
					{
						SecurityId = tick.SecurityId,
						ServerTime = tick.ServerTime,
						LocalTime = tick.LocalTime,
					}
					.TryAdd(Level1Fields.LastTradeId, tick.TradeId)
					.TryAdd(Level1Fields.LastTradeStringId, tick.TradeStringId)
					.TryAdd(Level1Fields.LastTradePrice, tick.TradePrice)
					.TryAdd(Level1Fields.LastTradeVolume, tick.TradeVolume)
					.TryAdd(Level1Fields.LastTradeUpDown, tick.IsUpTick)
					.TryAdd(Level1Fields.LastTradeOrigin, tick.OriginSide)
					;

					if (!l1Msg.HasChanges())
						continue;

					Current = l1Msg;
					return true;
				}

				Current = null;
				return false;
			}

			void IEnumerator.Reset()
			{
				_itemsEnumerator.Reset();
				Current = null;
			}

			object IEnumerator.Current => Current;

			void IDisposable.Dispose()
			{
				Current = null;
				_itemsEnumerator.Dispose();

				GC.SuppressFinalize(this);
			}
		}

		public TickLevel1Enumerable(IEnumerable<ExecutionMessage> items)
			: base(() => new TickLevel1Enumerator(items))
		{
			if (items is null)
				throw new ArgumentNullException(nameof(items));
		}
	}

	/// <summary>
	/// To build level1 from the orders log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <param name="builder">Order log to market depth builder.</param>
	/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
	/// <returns>Tick trades.</returns>
	public static IEnumerable<Level1ChangeMessage> ToLevel1(this IEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default)
	{
		if (builder == null)
			return new TickLevel1Enumerable(items);
		else
			return items.ToOrderBooks(builder, interval, 1).BuildIfNeed().ToLevel1();
	}

	/// <summary>
	/// Try build books by <see cref="OrderBookIncrementBuilder"/> in case of <paramref name="books"/> is incremental changes.
	/// </summary>
	/// <param name="books">Order books.</param>
	/// <param name="logs">Logs.</param>
	/// <returns>Order books.</returns>
	public static IEnumerable<QuoteChangeMessage> BuildIfNeed(this IEnumerable<QuoteChangeMessage> books, ILogReceiver logs = null)
	{
		if (books is null)
			throw new ArgumentNullException(nameof(books));

		var builders = new Dictionary<SecurityId, OrderBookIncrementBuilder>();

		foreach (var book in books)
		{
			if (book.State != null)
			{
				var builder = builders.SafeAdd(book.SecurityId, key => new OrderBookIncrementBuilder(key) { Parent = logs ?? LogManager.Instance?.Application });
				var change = builder.TryApply(book);

				if (change != null)
					yield return change;
			}
			else
				yield return book;
		}
	}

	/// <summary>
	/// Build market depths from order log.
	/// </summary>
	/// <param name="items">Orders log lines.</param>
	/// <param name="builder">Order log to market depth builder.</param>
	/// <param name="interval">The interval of the order book generation. The default is <see cref="TimeSpan.Zero"/>, which means order books generation at each new item of orders log.</param>
	/// <param name="maxDepth">The maximal depth of order book. The default is <see cref="Int32.MaxValue"/>, which means endless depth.</param>
	/// <returns>Market depths.</returns>
	public static IEnumerable<QuoteChangeMessage> ToOrderBooks(this IEnumerable<ExecutionMessage> items, IOrderLogMarketDepthBuilder builder, TimeSpan interval = default, int maxDepth = int.MaxValue)
	{
		var snapshotSent = false;
		var prevTime = default(DateTimeOffset?);

		foreach (var item in items)
		{
			if (!snapshotSent)
			{
				yield return builder.GetSnapshot(item.ServerTime);
				snapshotSent = true;
			}

			var depth = builder.Update(item);
			if (depth is null)
				continue;

			if (prevTime != null && (depth.ServerTime - prevTime.Value) < interval)
				continue;

			if (maxDepth < int.MaxValue)
			{
				depth = builder.GetSnapshot(item.ServerTime); // cannot trim incremental book

				depth.Bids = [.. depth.Bids.Take(maxDepth)];
				depth.Asks = [.. depth.Asks.Take(maxDepth)];
			}
			else if (interval != default)
			{
				depth = builder.GetSnapshot(item.ServerTime); // cannot return incrementals if interval is set
			}

			yield return depth;

			prevTime = depth.ServerTime;
		}
	}

	/// <summary>
	/// To determine, is the order book empty.
	/// </summary>
	/// <param name="depth">Market depth.</param>
	/// <returns><see langword="true" />, if order book is empty, otherwise, <see langword="false" />.</returns>
	public static bool IsFullEmpty(this IOrderBookMessage depth)
	{
		if (depth == null)
			throw new ArgumentNullException(nameof(depth));

		return depth.Bids.Length == 0 && depth.Asks.Length == 0;
	}

	/// <summary>
	/// To determine, is the order book half-empty.
	/// </summary>
	/// <param name="depth">Market depth.</param>
	/// <returns><see langword="true" />, if the order book is half-empty, otherwise, <see langword="false" />.</returns>
	public static bool IsHalfEmpty(this IOrderBookMessage depth)
	{
		if (depth == null)
			throw new ArgumentNullException(nameof(depth));

		var (bid, ask) = depth.GetBestPair();

		if (bid is null)
			return ask is not null;
		else
			return ask is null;
	}

	/// <summary>
	/// Convert order changes to final snapshot.
	/// </summary>
	/// <param name="diffs">Changes.</param>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="logs">Logs.</param>
	/// <returns>Snapshot.</returns>
	public static ExecutionMessage ToOrderSnapshot(this IEnumerable<ExecutionMessage> diffs, long transactionId, ILogReceiver logs)
	{
		if (diffs is null)
			throw new ArgumentNullException(nameof(diffs));

		diffs = diffs.OrderBy(m =>
		{
			return m.OrderState switch
			{
				null or OrderStates.None => 0,
				OrderStates.Pending => 1,
				OrderStates.Active => 2,
				OrderStates.Done or OrderStates.Failed => 3,
				_ => throw new ArgumentOutOfRangeException(m.OrderState.ToString()),
			};
		});

		ExecutionMessage snapshot = null;

		foreach (var execMsg in diffs)
		{
			if (!execMsg.HasOrderInfo)
				throw new InvalidOperationException(LocalizedStrings.NoInfoAboutOrder.Put(transactionId));

			if (snapshot is null)
				snapshot = execMsg;
			else
			{
				if (execMsg.Balance != null)
					snapshot.Balance = snapshot.Balance.ApplyNewBalance(execMsg.Balance.Value, transactionId, logs);

				if (execMsg.OrderState != null)
				{
					snapshot.OrderState.VerifyOrderState(execMsg.OrderState.Value, transactionId, logs);
					snapshot.OrderState = execMsg.OrderState.Value;
				}

				if (execMsg.OrderStatus != null)
					snapshot.OrderStatus = execMsg.OrderStatus;

				if (execMsg.OrderId != null)
					snapshot.OrderId = execMsg.OrderId;

				if (!execMsg.OrderStringId.IsEmpty())
					snapshot.OrderStringId = execMsg.OrderStringId;

				if (execMsg.OrderBoardId != null)
					snapshot.OrderBoardId = execMsg.OrderBoardId;

				if (execMsg.PnL != null)
					snapshot.PnL = execMsg.PnL;

				if (execMsg.Position != null)
					snapshot.Position = execMsg.Position;

				if (execMsg.Commission != null)
					snapshot.Commission = execMsg.Commission;

				if (execMsg.CommissionCurrency != null)
					snapshot.CommissionCurrency = execMsg.CommissionCurrency;

				if (execMsg.AveragePrice != null)
					snapshot.AveragePrice = execMsg.AveragePrice;

				if (execMsg.Latency != null)
					snapshot.Latency = execMsg.Latency;
			}
		}

		if (snapshot is null)
			throw new InvalidOperationException(LocalizedStrings.ElementNotFoundParams.Put(transactionId));

		return snapshot;
	}

	/// <summary>
	/// Check the possibility <see cref="ExecutionMessage.Balance"/> change.
	/// </summary>
	/// <param name="currBal">Current balance.</param>
	/// <param name="newBal">New balance.</param>
	/// <param name="transactionId">Transaction id.</param>
	/// <param name="logs">Logs.</param>
	/// <returns>New balance.</returns>
	public static decimal ApplyNewBalance(this decimal? currBal, decimal newBal, long transactionId, ILogReceiver logs)
	{
		if (logs is null)
			throw new ArgumentNullException(nameof(logs));

		if (newBal < 0)
			logs.AddErrorLog($"Order {transactionId}: balance {newBal} < 0");

		if (currBal < newBal)
			logs.AddErrorLog($"Order {transactionId}: bal_old {currBal} -> bal_new {newBal}");

		return newBal;
	}

	/// <summary>
	/// Validate state change.
	/// </summary>
	/// <param name="currState">Current state.</param>
	/// <param name="newState">New state.</param>
	public static bool ValidateChannelState(this ChannelStates currState, ChannelStates newState)
		=> _channelStateValidator[currState, newState];

	/// <summary>
	/// Check the possibility <see cref="OrderStates"/> change.
	/// </summary>
	/// <param name="currState">Current order's state.</param>
	/// <param name="newState">New state.</param>
	/// <param name="transactionId">Transaction id.</param>
	/// <param name="logs">Logs.</param>
	/// <returns>Check result.</returns>
	public static bool VerifyOrderState(this OrderStates? currState, OrderStates newState, long transactionId, ILogReceiver logs)
	{
		var isInvalid = currState is not null && currState != newState && !_orderStateValidator[currState.Value, newState];

		if (isInvalid)
			logs?.AddWarningLog($"Order {transactionId} invalid state change: {currState} -> {newState}");

		return !isInvalid;
	}

	/// <summary>
	/// Determines the value is set.
	/// </summary>
	/// <param name="value"><see cref="Unit"/></param>
	/// <returns>Check result.</returns>
	public static bool IsSet(this Unit value)
		=> value is not null && value.Value != 0;

	/// <summary>
	/// Determines the specified message is lookup.
	/// </summary>
	/// <param name="message"><see cref="Message"/></param>
	/// <returns>Check result.</returns>
	public static bool IsLookup(this IMessage message)
		=> message.CheckOnNull(nameof(message)).Type.IsLookup();

	/// <summary>
	/// Determines the specified message type is lookup.
	/// </summary>
	/// <param name="type"><see cref="MessageTypes"/></param>
	/// <returns>Check result.</returns>
	public static bool IsLookup(this MessageTypes type)
		=> type
			is MessageTypes.PortfolioLookup
			or MessageTypes.OrderStatus
			or MessageTypes.SecurityLookup
			or MessageTypes.BoardLookup
			or MessageTypes.DataTypeLookup;

	/// <summary>
	/// Convert <see cref="ConnectionStates"/> to <see cref="Message"/>.
	/// </summary>
	/// <param name="state"><see cref="ConnectionStates"/> value.</param>
	/// <returns><see cref="Message"/> value.</returns>
	public static Message ToMessage(this ConnectionStates state)
		=> state switch
		{
			ConnectionStates.Disconnected => new DisconnectMessage(),
			ConnectionStates.Disconnecting => null,
			ConnectionStates.Connecting => null,
			ConnectionStates.Connected => new ConnectMessage(),
			ConnectionStates.Reconnecting => new ConnectionLostMessage(),
			ConnectionStates.Restored => new ConnectionRestoredMessage(),
			ConnectionStates.Failed => new ConnectMessage { Error = new InvalidOperationException(LocalizedStrings.UnexpectedDisconnection) },
			_ => throw new ArgumentOutOfRangeException(nameof(state), state, LocalizedStrings.InvalidValue),
		};

	/// <summary>
	/// Create instance of <see cref="CandleMessage"/>.
	/// </summary>
	/// <param name="messageType">The type of candle message.</param>
	/// <returns>Instance of <see cref="CandleMessage"/>.</returns>
	public static CandleMessage CreateCandleMessage(this Type messageType)
		=> messageType.CreateInstance<CandleMessage>();

	/// <summary>
	/// Convert <see cref="string"/> into <see cref="MessageTypes"/>.
	/// </summary>
	/// <param name="str"><see cref="string"/></param>
	/// <returns><see cref="MessageTypes"/></returns>
	public static MessageTypes ToMessageType(this string str)
	{
		// TODO 2025-03-09 Remove 1 year later
		if (str == "TimeFrameInfo")
			return MessageTypes.DataTypeInfo;
		else if (str == "TimeFrameLookup")
			return MessageTypes.DataTypeLookup;
		else
			return str.To<MessageTypes>();
	}

	/// <summary>
	/// To check, whether the message adapter uses channels.
	/// </summary>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <returns>Check result.</returns>
	public static bool UseChannels(this IMessageAdapter adapter)
	{
		if (adapter is null)
			throw new ArgumentNullException(nameof(adapter));

		return adapter.UseInChannel || adapter.UseOutChannel;
	}
}
