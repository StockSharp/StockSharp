namespace StockSharp.BusinessEntities;

using System.Collections;

using StockSharp.Algo.Candles;

static partial class EntitiesExtensions
{
	static EntitiesExtensions()
	{
#pragma warning disable CS0618 // Type or member is obsolete
		RegisterCandle(() => new TimeFrameCandle(), () => new TimeFrameCandleMessage());
		RegisterCandle(() => new TickCandle(), () => new TickCandleMessage());
		RegisterCandle(() => new VolumeCandle(), () => new VolumeCandleMessage());
		RegisterCandle(() => new RangeCandle(), () => new RangeCandleMessage());
		RegisterCandle(() => new PnFCandle(), () => new PnFCandleMessage());
		RegisterCandle(() => new RenkoCandle(), () => new RenkoCandleMessage());
		RegisterCandle(() => new HeikinAshiCandle(), () => new HeikinAshiCandleMessage());
#pragma warning restore CS0618 // Type or member is obsolete
	}

	/// <summary>
	/// Get portfolio identifier.
	/// </summary>
	/// <param name="portfolio">Portfolio.</param>
	/// <returns>Portfolio identifier.</returns>
	[Obsolete("Use Portfolio.Name property.")]
	public static string GetUniqueId(this Portfolio portfolio)
	{
		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		return /*portfolio.InternalId?.To<string>() ?? */portfolio.Name;
	}

	/// <summary>
	/// To convert the message into tick trade.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="security">Security.</param>
	/// <returns>Tick trade.</returns>
	[Obsolete("Use ITickTradeMessage.")]
	public static Trade ToTrade(this ExecutionMessage message, Security security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		return message.ToTrade(new Trade { Security = security });
	}

	/// <summary>
	/// To convert the message into tick trade.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="trade">Tick trade.</param>
	/// <returns>Tick trade.</returns>
	[Obsolete("Use ITickTradeMessage.")]
	public static Trade ToTrade(this ExecutionMessage message, Trade trade)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		trade.Id = message.TradeId;
		trade.StringId = message.TradeStringId;
		trade.Price = message.TradePrice ?? 0;
		trade.Volume = message.TradeVolume ?? 0;
		trade.Status = message.TradeStatus;
		trade.IsSystem = message.IsSystem;
		trade.ServerTime = message.ServerTime;
		trade.LocalTime = message.LocalTime;
		trade.OpenInterest = message.OpenInterest;
		trade.OriginSide = message.OriginSide;
		trade.IsUpTick = message.IsUpTick;
		trade.Currency = message.Currency;
		trade.SeqNum = message.SeqNum;
		trade.BuildFrom = message.BuildFrom;
		trade.Yield = message.Yield;
		trade.OrderBuyId = message.OrderBuyId;
		trade.OrderSellId = message.OrderSellId;

		return trade;
	}

	private static readonly CachedSynchronizedPairSet<Type, Type> _candleTypes = [];

	/// <summary>
	/// Cast candle type <see cref="Candle"/> to the message <see cref="CandleMessage"/>.
	/// </summary>
	/// <param name="candleType">The type of the candle <see cref="Candle"/>.</param>
	/// <returns>The type of the message <see cref="CandleMessage"/>.</returns>
	public static Type ToCandleMessageType(this Type candleType)
	{
		if (candleType is null)
			throw new ArgumentNullException(nameof(candleType));

		if (!_candleTypes.TryGetValue(candleType, out var messageType))
			throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

		return messageType;
	}

	/// <summary>
	/// Cast message type <see cref="CandleMessage"/> to the candle type <see cref="Candle"/>.
	/// </summary>
	/// <param name="messageType">The type of the message <see cref="CandleMessage"/>.</param>
	/// <returns>The type of the candle <see cref="Candle"/>.</returns>
	public static Type ToCandleType(this Type messageType)
	{
		if (messageType is null)
			throw new ArgumentNullException(nameof(messageType));

		if (!_candleTypes.TryGetKey(messageType, out var candleType))
			throw new ArgumentOutOfRangeException(nameof(messageType), messageType, LocalizedStrings.WrongCandleType);

		return candleType;
	}

	/// <summary>
	/// To convert the candle into message.
	/// </summary>
	/// <param name="candle">Candle.</param>
	/// <returns>Message.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static CandleMessage ToMessage(this Candle candle)
	{
		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		if (!_candleTypes.TryGetValue(candle.GetType(), out var messageType))
			throw new ArgumentException(LocalizedStrings.UnknownCandleType.Put(candle.GetType()), nameof(candle));

		var message = messageType.CreateCandleMessage();

		message.LocalTime = candle.OpenTime;
		message.SecurityId = candle.Security.ToSecurityId();
		message.OpenTime = candle.OpenTime;
		message.HighTime = candle.HighTime;
		message.LowTime = candle.LowTime;
		message.CloseTime = candle.CloseTime;
		message.OpenPrice = candle.OpenPrice;
		message.HighPrice = candle.HighPrice;
		message.LowPrice = candle.LowPrice;
		message.ClosePrice = candle.ClosePrice;
		message.TotalVolume = candle.TotalVolume;
		message.BuyVolume = candle.BuyVolume;
		message.SellVolume = candle.SellVolume;
		message.OpenInterest = candle.OpenInterest;
		message.OpenVolume = candle.OpenVolume;
		message.HighVolume = candle.HighVolume;
		message.LowVolume = candle.LowVolume;
		message.CloseVolume = candle.CloseVolume;
		message.RelativeVolume = candle.RelativeVolume;
		message.DataType = DataType.Create(messageType, candle.Arg);
		message.PriceLevels = candle.PriceLevels?/*.Select(l => l.Clone())*/.ToArray();
		message.State = candle.State;
		message.SeqNum = candle.SeqNum;
		message.BuildFrom = candle.BuildFrom;

		return message;
	}

	/// <summary>
	/// Cast <see cref="MarketDepth"/> to the <see cref="QuoteChangeMessage"/>.
	/// </summary>
	/// <param name="depth"><see cref="MarketDepth"/>.</param>
	/// <returns><see cref="QuoteChangeMessage"/>.</returns>
	[Obsolete("Use IOrderBookMessage.")]
	public static QuoteChangeMessage ToMessage(this MarketDepth depth)
	{
		if (depth == null)
			throw new ArgumentNullException(nameof(depth));

		var securityId = depth.Security.ToSecurityId();

		return new QuoteChangeMessage
		{
			LocalTime = depth.LocalTime,
			SecurityId = securityId,
			Bids = [.. depth.Bids],
			Asks = [.. depth.Asks],
			ServerTime = depth.ServerTime,
			Currency = depth.Currency,
			SeqNum = depth.SeqNum,
			BuildFrom = depth.BuildFrom,
		};
	}


	[Obsolete]
	private class ToMessagesEnumerable<TEntity, TMessage>(IEnumerable<TEntity> entities) : IEnumerable<TMessage>
	{
		private readonly IEnumerable<TEntity> _entities = entities ?? throw new ArgumentNullException(nameof(entities));

		public IEnumerator<TMessage> GetEnumerator()
		{
			return _entities.Select(Convert).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		//int IEnumerableEx.Count => _entities.Count;

		private static TMessage Convert(TEntity value)
		{
			if (value is OrderLogItem)
				return value.To<OrderLogItem>().ToMessage().To<TMessage>();
			else if (value is MarketDepth)
				return value.To<MarketDepth>().ToMessage().To<TMessage>();
			else if (value is Trade)
				return value.To<Trade>().ToMessage().To<TMessage>();
			else if (value is MyTrade)
				return value.To<MyTrade>().ToMessage().To<TMessage>();
			else if (value is Candle)
				return value.To<Candle>().ToMessage().To<TMessage>();
			else if (value is Order)
				return value.To<Order>().ToMessage().To<TMessage>();

			else
				throw new InvalidOperationException();
		}
	}

	/// <summary>
	/// To convert trading objects into messages.
	/// </summary>
	/// <typeparam name="TEntity">The type of trading object.</typeparam>
	/// <typeparam name="TMessage">Message type.</typeparam>
	/// <param name="entities">Trading objects.</param>
	/// <returns>Messages.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static IEnumerable<TMessage> ToMessages<TEntity, TMessage>(this IEnumerable<TEntity> entities)
		=> new ToMessagesEnumerable<TEntity, TMessage>(entities);

	[Obsolete]
	private class ToEntitiesEnumerable<TMessage, TEntity> : IEnumerable<TEntity>
		where TMessage : Message
	{
		private readonly IEnumerable<TMessage> _messages;
		private readonly Security _security;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;
		//private readonly object _candleArg;

		public ToEntitiesEnumerable(IEnumerable<TMessage> messages, Security security, IExchangeInfoProvider exchangeInfoProvider)
		{
			if (typeof(TMessage) != typeof(NewsMessage))
			{
				if (security == null)
					throw new ArgumentNullException(nameof(security));
			}

			_messages = messages ?? throw new ArgumentNullException(nameof(messages));
			_security = security;
			_exchangeInfoProvider = exchangeInfoProvider;
		}

		public IEnumerator<TEntity> GetEnumerator()
		{
			return _messages.Select(Convert).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		//int IEnumerableEx.Count => _messages.Count;

		private TEntity Convert(TMessage message)
		{
			switch (message.Type)
			{
				case MessageTypes.Execution:
				{
					var execMsg = message.To<ExecutionMessage>();

					if (execMsg.DataType == DataType.Ticks)
						return execMsg.ToTrade(_security).To<TEntity>();
					else if (execMsg.DataType == DataType.OrderLog)
						return execMsg.ToOrderLog(_security).To<TEntity>();
					else if (execMsg.DataType == DataType.Transactions)
						return execMsg.ToOrder(_security).To<TEntity>();
					else
						throw new ArgumentOutOfRangeException(nameof(message), LocalizedStrings.UnsupportedType.Put(execMsg.DataType));
				}

				case MessageTypes.QuoteChange:
					return message.To<QuoteChangeMessage>().ToMarketDepth(_security).To<TEntity>();

				case MessageTypes.News:
					return message.To<NewsMessage>().ToNews(_exchangeInfoProvider).To<TEntity>();

				case MessageTypes.BoardState:
					return message.To<TEntity>();

				default:
				{
					if (message is CandleMessage candleMsg)
						return candleMsg.ToCandle(_security).To<TEntity>();

					throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.InvalidValue);
				}
			}
		}
	}

	/// <summary>
	/// To convert the tick trade into message.
	/// </summary>
	/// <param name="trade">Tick trade.</param>
	/// <returns>Message.</returns>
	[Obsolete("Use ITickTradeMessage.")]
	public static ExecutionMessage ToMessage(this Trade trade)
	{
		if (trade == null)
			throw new ArgumentNullException(nameof(trade));

		return new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			LocalTime = trade.LocalTime,
			ServerTime = trade.ServerTime,
			SecurityId = trade.Security.ToSecurityId(),
			TradeId = trade.Id,
			TradeStringId = trade.StringId,
			TradePrice = trade.Price,
			TradeVolume = trade.Volume,
			IsSystem = trade.IsSystem,
			TradeStatus = trade.Status,
			OpenInterest = trade.OpenInterest,
			OriginSide = trade.OriginSide,
			IsUpTick = trade.IsUpTick,
			Currency = trade.Currency,
			SeqNum = trade.SeqNum,
			BuildFrom = trade.BuildFrom,
			Yield = trade.Yield,
			OrderBuyId = trade.OrderBuyId,
			OrderSellId = trade.OrderSellId,
		};
	}

	/// <summary>
	/// To convert the string of orders log onto message.
	/// </summary>
	/// <param name="item">Order log item.</param>
	/// <returns>Message.</returns>
	[Obsolete("Use OrderLogMessage.")]
	public static ExecutionMessage ToMessage(this OrderLogItem item)
	{
		if (item == null)
			throw new ArgumentNullException(nameof(item));

		var order = item.Order;
		var trade = item.Trade;

		return new ExecutionMessage
		{
			LocalTime = order.LocalTime,
			SecurityId = order.Security.ToSecurityId(),
			OrderId = order.Id,
			OrderStringId = order.StringId,
			TransactionId = order.TransactionId,
			OriginalTransactionId = trade == null ? 0 : order.TransactionId,
			ServerTime = order.Time,
			OrderPrice = order.Price,
			OrderVolume = order.Volume,
			Balance = order.Balance,
			Side = order.Side,
			IsSystem = order.IsSystem,
			OrderState = order.State,
			OrderStatus = order.Status,
			TimeInForce = order.TimeInForce,
			ExpiryDate = order.ExpiryDate,
			PortfolioName = order.Portfolio?.Name,
			DataTypeEx = DataType.OrderLog,
			TradeId = trade?.Id,
			TradeStringId = trade?.StringId,
			TradePrice = trade?.Price,
			Currency = order.Currency,
			SeqNum = order.SeqNum,
			OrderBuyId = trade?.OrderBuyId,
			OrderSellId = trade?.OrderSellId,
			TradeStatus = trade?.Status,
			IsUpTick = trade?.IsUpTick,
			Yield = trade?.Yield,
			OpenInterest = trade?.OpenInterest,
			OriginSide = trade?.OriginSide,
		};
	}

	/// <summary>
	/// To convert messages into trading objects.
	/// </summary>
	/// <typeparam name="TMessage">Message type.</typeparam>
	/// <typeparam name="TEntity">The type of trading object.</typeparam>
	/// <param name="messages">Messages.</param>
	/// <param name="security">Security.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <returns>Trading objects.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static IEnumerable<TEntity> ToEntities<TMessage, TEntity>(this IEnumerable<TMessage> messages, Security security, IExchangeInfoProvider exchangeInfoProvider = null)
		where TMessage : Message
	{
		if (messages is IEnumerable<QuoteChangeMessage> books)
			messages = books.BuildIfNeed().To<IEnumerable<TMessage>>();

		return new ToEntitiesEnumerable<TMessage, TEntity>(messages, security, exchangeInfoProvider);
	}

	/// <summary>
	/// To convert messages into trading objects.
	/// </summary>
	/// <typeparam name="TCandle">The candle type.</typeparam>
	/// <param name="messages">Messages.</param>
	/// <param name="security">Security.</param>
	/// <returns>Trading objects.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static IEnumerable<TCandle> ToCandles<TCandle>(this IEnumerable<CandleMessage> messages, Security security)
	{
		return new ToEntitiesEnumerable<CandleMessage, TCandle>(messages, security, null);
	}

	/// <summary>
	/// To convert <see cref="CandleMessage"/> into candle.
	/// </summary>
	/// <typeparam name="TCandle">The candle type.</typeparam>
	/// <param name="message">Message.</param>
	/// <param name="series">Series.</param>
	/// <returns>Candle.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static TCandle ToCandle<TCandle>(this CandleMessage message, CandleSeries series)
		where TCandle : Candle, new()
	{
		return (TCandle)message.ToCandle(series);
	}

	/// <summary>
	/// To convert <see cref="CandleMessage"/> into candle.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="series">Series.</param>
	/// <returns>Candle.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static Candle ToCandle(this CandleMessage message, CandleSeries series)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (series == null)
			throw new ArgumentNullException(nameof(series));

		var candle = message.ToCandle(series.Security);
		//candle.Series = series;

		if (candle.Arg.IsNull(true))
			candle.Arg = series.Arg;

		return candle;
	}

	[Obsolete]
	private static readonly SynchronizedDictionary<Type, Func<Candle>> _candleCreators = [];

	/// <summary>
	/// Register new candle type.
	/// </summary>
	/// <typeparam name="TCandle">Candle type.</typeparam>
	/// <typeparam name="TMessage">The type of candle message.</typeparam>
	/// <param name="candleCreator"><see cref="Candle"/> instance creator.</param>
	/// <param name="candleMessageCreator"><see cref="CandleMessage"/> instance creator.</param>
	[Obsolete("Conversion reduce performance.")]
	public static void RegisterCandle<TCandle, TMessage>(Func<TCandle> candleCreator, Func<TMessage> candleMessageCreator)
		where TCandle : Candle
		where TMessage : CandleMessage
	{
		RegisterCandle(typeof(TCandle), typeof(TMessage), candleCreator, candleMessageCreator);
	}

	/// <summary>
	/// Register new candle type.
	/// </summary>
	/// <param name="candleType">Candle type.</param>
	/// <param name="messageType">The type of candle message.</param>
	/// <param name="candleCreator"><see cref="Candle"/> instance creator.</param>
	/// <param name="candleMessageCreator"><see cref="CandleMessage"/> instance creator.</param>
	[Obsolete("Conversion reduce performance.")]
	public static void RegisterCandle(Type candleType, Type messageType, Func<Candle> candleCreator, Func<CandleMessage> candleMessageCreator)
	{
		if (messageType == null)
			throw new ArgumentNullException(nameof(messageType));

		if (candleCreator == null)
			throw new ArgumentNullException(nameof(candleCreator));

		if (candleMessageCreator == null)
			throw new ArgumentNullException(nameof(candleMessageCreator));

		_candleTypes.Add(candleType, messageType);
		_candleCreators.Add(candleType, candleCreator);
	}

	/// <summary>
	/// To convert <see cref="CandleMessage"/> into candle.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="security">Security.</param>
	/// <returns>Candle.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static Candle ToCandle(this CandleMessage message, Security security)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (security == null)
			throw new ArgumentNullException(nameof(security));

		if (!_candleTypes.TryGetKey(message.GetType(), out var candleType) || !_candleCreators.TryGetValue(candleType, out var creator))
			throw new ArgumentOutOfRangeException(nameof(message), message.Type, LocalizedStrings.WrongCandleType);

		var candle = creator();

		candle.Security = security;
		candle.Arg = message.DataType.Arg;

		return candle.Update(message);
	}

	/// <summary>
	/// Update candle from <see cref="CandleMessage"/>.
	/// </summary>
	/// <param name="candle">Candle.</param>
	/// <param name="message">Message.</param>
	/// <returns>Candle.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static Candle Update(this Candle candle, CandleMessage message)
	{
		if (candle == null)
			throw new ArgumentNullException(nameof(candle));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		candle.OpenPrice = message.OpenPrice;
		candle.OpenVolume = message.OpenVolume;
		candle.OpenTime = message.OpenTime;

		candle.HighPrice = message.HighPrice;
		candle.HighVolume = message.HighVolume;
		candle.HighTime = message.HighTime;

		candle.LowPrice = message.LowPrice;
		candle.LowVolume = message.LowVolume;
		candle.LowTime = message.LowTime;

		candle.ClosePrice = message.ClosePrice;
		candle.CloseVolume = message.CloseVolume;
		candle.CloseTime = message.CloseTime;

		candle.TotalVolume = message.TotalVolume;
		candle.RelativeVolume = message.RelativeVolume;

		candle.BuyVolume = message.BuyVolume;
		candle.SellVolume = message.SellVolume;

		candle.OpenInterest = message.OpenInterest;

		candle.TotalTicks = message.TotalTicks;
		candle.UpTicks = message.UpTicks;
		candle.DownTicks = message.DownTicks;

		candle.PriceLevels = message.PriceLevels?/*.Select(l => l.Clone())*/.ToArray();

		candle.State = message.State;
		candle.SeqNum = message.SeqNum;
		candle.BuildFrom = message.BuildFrom;

		return candle;
	}

	/// <summary>
	/// To convert the message into order book.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="security">Security.</param>
	/// <returns>Market depth.</returns>
	[Obsolete("Use IOrderBookMessage.")]
	public static MarketDepth ToMarketDepth(this QuoteChangeMessage message, Security security)
	{
		return message.ToMarketDepth(new MarketDepth(security));
	}

	/// <summary>
	/// To convert the message into order book.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="marketDepth">Market depth.</param>
	/// <returns>Market depth.</returns>
	[Obsolete("Use IOrderBookMessage.")]
	public static MarketDepth ToMarketDepth(this QuoteChangeMessage message, MarketDepth marketDepth)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (marketDepth == null)
			throw new ArgumentNullException(nameof(marketDepth));

		marketDepth.Update(
			message.Bids,
			message.Asks,
			message.ServerTime);

		marketDepth.LocalTime = message.LocalTime;
		marketDepth.Currency = message.Currency;
		marketDepth.SeqNum = message.SeqNum;
		marketDepth.BuildFrom = message.BuildFrom;

		return marketDepth;
	}

	/// <summary>
	/// To convert the message into orders log string.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="security">Security.</param>
	/// <returns>Order log item.</returns>
	[Obsolete("Use IOrderLogMessage.")]
	public static OrderLogItem ToOrderLog(this ExecutionMessage message, Security security)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		return message.ToOrderLog(new OrderLogItem
		{
			Order = new() { Security = security },
			Trade = message.HasTradeInfo ? new() { Security = security } : null
		});
	}

	/// <summary>
	/// To convert the message into orders log string.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="item">Order log item.</param>
	/// <returns>Order log item.</returns>
	[Obsolete("Use IOrderLogMessage.")]
	public static OrderLogItem ToOrderLog(this ExecutionMessage message, OrderLogItem item)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (item == null)
			throw new ArgumentNullException(nameof(item));

		var order = item.Order;

		order.Portfolio = Portfolio.AnonymousPortfolio;

		order.Id = message.OrderId;
		order.StringId = message.OrderStringId;
		order.TransactionId = message.TransactionId;
		order.Price = message.OrderPrice;
		order.Volume = message.OrderVolume ?? 0;
		order.Balance = message.Balance ?? 0;
		order.Side = message.Side;
		order.Time = message.ServerTime;
		order.ServerTime = message.ServerTime;
		order.LocalTime = message.LocalTime;

		order.Status = message.OrderStatus;
		order.TimeInForce = message.TimeInForce;
		order.IsSystem = message.IsSystem;
		order.Currency = message.Currency;
		order.SeqNum = message.SeqNum;

		order.ApplyNewState(message.OrderState ?? (message.HasTradeInfo ? OrderStates.Done : OrderStates.Active));

		if (message.HasTradeInfo)
		{
			var trade = item.Trade;

			trade.Id = message.TradeId;
			trade.StringId = message.TradeStringId;
			trade.Price = message.TradePrice ?? default;
			trade.ServerTime = message.ServerTime;
			trade.Volume = message.OrderVolume ?? default;
			trade.IsSystem = message.IsSystem;
			trade.Status = message.TradeStatus;
			trade.OrderBuyId = message.OrderBuyId;
			trade.OrderSellId = message.OrderSellId;
			trade.OriginSide = message.OriginSide;
			trade.OpenInterest = message.OpenInterest;
			trade.IsUpTick = message.IsUpTick;
			trade.Yield = message.Yield;
		}

		return item;
	}

	/// <summary>
	/// Cast <see cref="Level1ChangeMessage"/> to the <see cref="MarketDepth"/>.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="security">Security.</param>
	/// <returns>Market depth.</returns>
	[Obsolete("Use IOrderBookMessage.")]
	public static MarketDepth ToMarketDepth(this Level1ChangeMessage message, Security security)
	{
		QuoteChange createQuote(Level1Fields priceField, Level1Fields volumeField)
		{
			var changes = message.Changes;
			return new QuoteChange((decimal)changes[priceField], (decimal?)changes.TryGetValue(volumeField) ?? 0m);
		}

		return new MarketDepth(security) { LocalTime = message.LocalTime }.Update(
			[createQuote(Level1Fields.BestBidPrice, Level1Fields.BestBidVolume)],
			[createQuote(Level1Fields.BestAskPrice, Level1Fields.BestAskVolume)],
			message.ServerTime);
	}

	/// <summary>
	/// To convert the type of business object into type of message.
	/// </summary>
	/// <param name="dataType">The type of business object.</param>
	/// <param name="arg">The data parameter.</param>
	/// <returns>Message type.</returns>
	[Obsolete("Conversion reduce performance.")]
	public static Type ToMessageType(this Type dataType, ref object arg)
	{
		if (dataType == typeof(Trade))
		{
			arg = ExecutionTypes.Tick;
			return typeof(ExecutionMessage);
		}
		else if (dataType == typeof(MarketDepth))
			return typeof(QuoteChangeMessage);
		else if (dataType == typeof(Order) || dataType == typeof(MyTrade))
		{
			arg = ExecutionTypes.Transaction;
			return typeof(ExecutionMessage);
		}
		else if (dataType == typeof(OrderLogItem))
		{
			arg = ExecutionTypes.OrderLog;
			return typeof(ExecutionMessage);
		}
		else if (dataType.IsCandle())
		{
			if (arg == null)
				throw new ArgumentNullException(nameof(arg));

			return dataType.ToCandleMessageType();
		}
		else if (dataType == typeof(News))
			return typeof(NewsMessage);
		else if (dataType == typeof(Security))
			return typeof(SecurityMessage);
		else
			throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.InvalidValue);
	}

	/// <summary>
	/// Cast <see cref="CandleSeries"/> to <see cref="MarketDataMessage"/>.
	/// </summary>
	/// <param name="series">Candles series.</param>
	/// <param name="isSubscribe">The message is subscription.</param>
	/// <param name="from">The initial date from which you need to get data.</param>
	/// <param name="to">The final date by which you need to get data.</param>
	/// <param name="count">Candles count.</param>
	/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
	/// <returns>Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</returns>
	[Obsolete("Use Subscription class.")]
	public static MarketDataMessage ToMarketDataMessage(this CandleSeries series, bool isSubscribe, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = null, bool throwIfInvalidType = true)
	{
		if (series == null)
			throw new ArgumentNullException(nameof(series));

		var mdMsg = new MarketDataMessage
		{
			IsSubscribe = isSubscribe,
			From = from ?? series.From,
			To = to ?? series.To,
			Count = count ?? series.Count,
			BuildMode = series.BuildCandlesMode,
			BuildFrom = series.BuildCandlesFrom2,
			BuildField = series.BuildCandlesField,
			IsCalcVolumeProfile = series.IsCalcVolumeProfile,
			AllowBuildFromSmallerTimeFrame = series.AllowBuildFromSmallerTimeFrame,
			IsRegularTradingHours = series.IsRegularTradingHours,
			IsFinishedOnly = series.IsFinishedOnly,
		};

		if (series.CandleType == null)
		{
			if (throwIfInvalidType)
				throw new ArgumentException(LocalizedStrings.WrongCandleType);
		}
		else
		{
			var msgType = series
				.CandleType
				.ToCandleMessageType();

			mdMsg.DataType2 = DataType.Create(msgType, series.Arg);
		}

		mdMsg.ValidateBounds();
		series.Security?.ToMessage(copyExtendedId: true).CopyTo(mdMsg, false);

		return mdMsg;
	}

	/// <summary>
	/// Cast <see cref="MarketDataMessage"/> to <see cref="CandleSeries"/>.
	/// </summary>
	/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <param name="security">Security.</param>
	/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries ToCandleSeries(this MarketDataMessage message, Security security, bool throwIfInvalidType)
	{
		if (security == null)
			throw new ArgumentNullException(nameof(security));

		return message.ToCandleSeries(new CandleSeries { Security = security }, throwIfInvalidType);
	}

	/// <summary>
	/// Cast <see cref="MarketDataMessage"/> to <see cref="CandleSeries"/>.
	/// </summary>
	/// <param name="message">Market-data message (uses as a subscribe/unsubscribe in outgoing case, confirmation event in incoming case).</param>
	/// <param name="series">Candles series.</param>
	/// <param name="throwIfInvalidType">Throw an error if <see cref="MarketDataMessage.DataType2"/> isn't candle type.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries ToCandleSeries(this MarketDataMessage message, CandleSeries series, bool throwIfInvalidType)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (series == null)
			throw new ArgumentNullException(nameof(series));

		if (message.DataType2.IsCandles)
		{
			series.CandleType = message.DataType2.MessageType.ToCandleType();
			series.Arg = message.GetArg();
		}
		else
		{
			if (throwIfInvalidType)
				throw new ArgumentException(LocalizedStrings.UnknownCandleType.Put(message.DataType2), nameof(message));
		}

		series.From = message.From;
		series.To = message.To;
		series.Count = message.Count;
		series.BuildCandlesMode = message.BuildMode;
		series.BuildCandlesFrom2 = message.BuildFrom;
		series.BuildCandlesField = message.BuildField;
		series.IsCalcVolumeProfile = message.IsCalcVolumeProfile;
		series.AllowBuildFromSmallerTimeFrame = message.AllowBuildFromSmallerTimeFrame;
		series.IsRegularTradingHours = message.IsRegularTradingHours;
		series.IsFinishedOnly = message.IsFinishedOnly;

		return series;
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to <see cref="CandleSeries"/> value.
	/// </summary>
	/// <param name="dataType">Data type info.</param>
	/// <param name="security">The instrument to be used for candles formation.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries ToCandleSeries(this DataType dataType, Security security)
	{
		if (dataType is null)
			throw new ArgumentNullException(nameof(dataType));

		return new()
		{
			CandleType = dataType.MessageType.ToCandleType(),
			Arg = dataType.Arg,
			Security = security,
		};
	}

	/// <summary>
	/// Convert <see cref="DataType"/> to <see cref="CandleSeries"/> value.
	/// </summary>
	/// <param name="series">Candles series.</param>
	/// <returns>Data type info.</returns>
	[Obsolete("Use Subscription class.")]
	public static DataType ToDataType(this CandleSeries series)
	{
		if (series == null)
			throw new ArgumentNullException(nameof(series));

		return DataType.Create(series.CandleType.ToCandleMessageType(), series.Arg);
	}

	/// <summary>
	/// Determines whether the specified type is derived from <see cref="Candle"/>.
	/// </summary>
	/// <param name="candleType">The candle type.</param>
	/// <returns><see langword="true"/> if the specified type is derived from <see cref="Candle"/>, otherwise, <see langword="false"/>.</returns>
	[Obsolete("Use ICandleMessage.")]
	public static bool IsCandle(this Type candleType)
	{
		if (candleType == null)
			throw new ArgumentNullException(nameof(candleType));

		return candleType.IsSubclassOf(typeof(Candle));
	}

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="TimeFrameCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="TimeFrameCandle.TimeFrame"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries TimeFrame(this Security security, TimeSpan arg)
		=> arg.TimeFrame().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="RangeCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="RangeCandle.PriceRange"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries Range(this Security security, Unit arg)
		=> arg.Range().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="VolumeCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="VolumeCandle.Volume"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries Volume(this Security security, decimal arg)
		=> arg.Volume().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="TickCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="TickCandle.MaxTradeCount"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries Tick(this Security security, int arg)
		=> arg.Tick().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="PnFCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="PnFCandle.PnFArg"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries PnF(this Security security, PnFArg arg)
		=> arg.PnF().ToCandleSeries(security);

	/// <summary>
	/// To create <see cref="CandleSeries"/> for <see cref="RenkoCandle"/> candles.
	/// </summary>
	/// <param name="security">Security.</param>
	/// <param name="arg">The value of <see cref="RenkoCandle.BoxSize"/>.</param>
	/// <returns>Candles series.</returns>
	[Obsolete("Use Subscription class.")]
	public static CandleSeries Renko(this Security security, Unit arg)
		=> arg.Renko().ToCandleSeries(security);

	/// <summary>
	/// Determines the specified candle series if time frame based.
	/// </summary>
	/// <param name="series"><see cref="CandleSeries"/></param>
	/// <returns>Check result.</returns>
	[Obsolete("Use Subscription class.")]
	public static bool IsTimeFrame(this CandleSeries series)
		=> series.CheckOnNull(nameof(series)).CandleType == typeof(TimeFrameCandle);

	/// <summary>
	/// Convert string to <see cref="Unit"/>.
	/// </summary>
	/// <param name="str">String value of <see cref="Unit"/>.</param>
	/// <param name="throwIfNull">Throw <see cref="ArgumentNullException"/> if the specified string is empty.</param>
	/// <param name="security">Information about the instrument. Required when using <see cref="UnitTypes.Point"/> и <see cref="UnitTypes.Step"/>.</param>
	/// <returns>Object <see cref="Unit"/>.</returns>
	[Obsolete]
	public static Unit ToUnit2(this string str, bool throwIfNull = true, Security security = null)
	{
		return str.ToUnit(throwIfNull);
	}

	/// <summary>
	/// Cast the value to another type.
	/// </summary>
	/// <param name="unit">Source unit.</param>
	/// <param name="destinationType">Destination value type.</param>
	/// <param name="security">Information about the instrument. Required when using <see cref="UnitTypes.Point"/> и <see cref="UnitTypes.Step"/>.</param>
	/// <returns>Converted value.</returns>
	[Obsolete]
	public static Unit Convert(this Unit unit, UnitTypes destinationType, Security security)
	{
		return unit.Convert(destinationType);
	}

	/// <summary>
	/// To set the <see cref="Unit.GetTypeValue"/> property for the value.
	/// </summary>
	/// <param name="unit">Unit.</param>
	/// <param name="security">Security.</param>
	/// <returns>Unit.</returns>
	[Obsolete("Unit.GetTypeValue obsolete.")]
	public static Unit SetSecurity(this Unit unit, Security security)
	{
		if (unit is null)
			throw new ArgumentNullException(nameof(unit));

		unit.GetTypeValue = type => GetTypeValue(security, type);

		return unit;
	}

	[Obsolete("Unit.GetTypeValue obsolete.")]
	private static decimal? GetTypeValue(Security security, UnitTypes type)
	{
		switch (type)
		{
			case UnitTypes.Point:
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				return security.StepPrice;
			case UnitTypes.Step:
				if (security == null)
					throw new ArgumentNullException(nameof(security));

				return security.PriceStep;
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
		}
	}
}
