namespace StockSharp.BusinessEntities;

/// <summary>
/// Subscription provider interface.
/// </summary>
public interface ISubscriptionProvider
{
	/// <summary>
	/// Subscriptions.
	/// </summary>
	IEnumerable<Subscription> Subscriptions { get; }

	/// <summary>
	/// Get global subscription on <see cref="Security"/> lookup. Can be <see langword="null"/>.
	/// </summary>
	Subscription SecurityLookup { get; }

	/// <summary>
	/// Get global subscription on <see cref="Portfolio"/> lookup. Can be <see langword="null"/>.
	/// </summary>
	Subscription PortfolioLookup { get; }

	/// <summary>
	/// Get global subscription on <see cref="ExchangeBoard"/> lookup. Can be <see langword="null"/>.
	/// </summary>
	Subscription BoardLookup { get; }

	/// <summary>
	/// Get global subscription on <see cref="Order"/> lookup. Can be <see langword="null"/>.
	/// </summary>
	Subscription OrderLookup { get; }

	/// <summary>
	/// Get global subscription on <see cref="DataTypeInfoMessage"/> lookup. Can be <see langword="null"/>.
	/// </summary>
	Subscription DataTypeLookup { get; }

	/// <summary>
	/// Value received.
	/// </summary>
	event Action<Subscription, object> SubscriptionReceived;

	/// <summary>
	/// <see cref="Level1ChangeMessage"/> received.
	/// </summary>
	event Action<Subscription, Level1ChangeMessage> Level1Received;

	/// <summary>
	/// <see cref="IOrderBookMessage"/> received.
	/// </summary>
	event Action<Subscription, IOrderBookMessage> OrderBookReceived;

	/// <summary>
	/// <see cref="ITickTradeMessage"/> received.
	/// </summary>
	event Action<Subscription, ITickTradeMessage> TickTradeReceived;

	/// <summary>
	/// <see cref="IOrderLogMessage"/> received.
	/// </summary>
	event Action<Subscription, IOrderLogMessage> OrderLogReceived;

	/// <summary>
	/// <see cref="Security"/> received.
	/// </summary>
	event Action<Subscription, Security> SecurityReceived;

	/// <summary>
	/// <see cref="ExchangeBoard"/> received.
	/// </summary>
	event Action<Subscription, ExchangeBoard> BoardReceived;

	/// <summary>
	/// <see cref="News"/> received.
	/// </summary>
	event Action<Subscription, News> NewsReceived;

	/// <summary>
	/// <see cref="ICandleMessage"/> received.
	/// </summary>
	event Action<Subscription, ICandleMessage> CandleReceived;

	/// <summary>
	/// <see cref="MyTrade"/> received.
	/// </summary>
	event Action<Subscription, MyTrade> OwnTradeReceived;

	/// <summary>
	/// <see cref="Order"/> received.
	/// </summary>
	event Action<Subscription, Order> OrderReceived;

	/// <summary>
	/// <see cref="OrderFail"/> registration event.
	/// </summary>
	event Action<Subscription, OrderFail> OrderRegisterFailReceived;

	/// <summary>
	/// <see cref="OrderFail"/> cancellation event.
	/// </summary>
	event Action<Subscription, OrderFail> OrderCancelFailReceived;

	/// <summary>
	/// <see cref="OrderFail"/> edition event.
	/// </summary>
	event Action<Subscription, OrderFail> OrderEditFailReceived;

	/// <summary>
	/// <see cref="Portfolio"/> received.
	/// </summary>
	event Action<Subscription, Portfolio> PortfolioReceived;

	/// <summary>
	/// <see cref="Position"/> received.
	/// </summary>
	event Action<Subscription, Position> PositionReceived;

	/// <summary>
	/// <see cref="DataType"/> received.
	/// </summary>
	event Action<Subscription, DataType> DataTypeReceived;

	/// <summary>
	/// Subscription is online.
	/// </summary>
	event Action<Subscription> SubscriptionOnline;

	/// <summary>
	/// Subscription is started.
	/// </summary>
	event Action<Subscription> SubscriptionStarted;

	/// <summary>
	/// Subscription is stopped.
	/// </summary>
	event Action<Subscription, Exception> SubscriptionStopped;

	/// <summary>
	/// Subscription is failed.
	/// </summary>
	event Action<Subscription, Exception, bool> SubscriptionFailed;

	/// <summary>
	/// Subscribe.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	void Subscribe(Subscription subscription);

	/// <summary>
	/// Unsubscribe.
	/// </summary>
	/// <param name="subscription">Subscription.</param>
	void UnSubscribe(Subscription subscription);
}