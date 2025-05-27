namespace StockSharp.BusinessEntities;

/// <summary>
/// Transactional operations provider interface.
/// </summary>
public interface ITransactionProvider
{
	/// <summary>
	/// Transaction id generator.
	/// </summary>
	IdGenerator TransactionIdGenerator { get; }

	/// <summary>
	/// Own trade received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.OwnTradeReceived event.")]
	event Action<MyTrade> NewMyTrade;

	/// <summary>
	/// Order received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.OrderReceived event.")]
	event Action<Order> NewOrder;

	/// <summary>
	/// Order changed (cancelled, matched).
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.OrderReceived event.")]
	event Action<Order> OrderChanged;

	/// <summary>
	/// <see cref="EditOrder"/> success result event.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.OrderReceived event.")]
	event Action<long, Order> OrderEdited;

	/// <summary>
	/// Order registration error event.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.OrderRegisterFailReceived event.")]
	event Action<OrderFail> OrderRegisterFailed;

	/// <summary>
	/// Order cancellation error event.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.OrderCancelFailReceived event.")]
	event Action<OrderFail> OrderCancelFailed;

	/// <summary>
	/// <see cref="EditOrder"/> error result event.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.OrderEditFailReceived event.")]
	event Action<long, OrderFail> OrderEditFailed;

	/// <summary>
	/// Mass order cancellation event.
	/// </summary>
	event Action<long> MassOrderCanceled;

	/// <summary>
	/// Mass order cancellation event.
	/// </summary>
	event Action<long, DateTimeOffset> MassOrderCanceled2;

	/// <summary>
	/// Mass order cancellation errors event.
	/// </summary>
	event Action<long, Exception> MassOrderCancelFailed;

	/// <summary>
	/// Mass order cancellation errors event.
	/// </summary>
	event Action<long, Exception, DateTimeOffset> MassOrderCancelFailed2;

	/// <summary>
	/// Failed order status request event.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.SubscriptionFailed event.")]
	event Action<long, Exception, DateTimeOffset> OrderStatusFailed2;

	/// <summary>
	/// Lookup result <see cref="PortfolioLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.PortfolioReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> LookupPortfoliosResult;

	/// <summary>
	/// Lookup result <see cref="PortfolioLookupMessage"/> received.
	/// </summary>
	[Obsolete("Use ISubscriptionProvider.PortfolioReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, IEnumerable<Portfolio>, Exception> LookupPortfoliosResult2;

	/// <summary>
	/// Register new order.
	/// </summary>
	/// <param name="order">Registration details.</param>
	void RegisterOrder(Order order);

	/// <summary>
	/// Edit the order.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <param name="changes">Order changes.</param>
	void EditOrder(Order order, Order changes);

	/// <summary>
	/// Reregister the order.
	/// </summary>
	/// <param name="oldOrder">Cancelling order.</param>
	/// <param name="newOrder">New order to register.</param>
	void ReRegisterOrder(Order oldOrder, Order newOrder);

	/// <summary>
	/// Cancel the order.
	/// </summary>
	/// <param name="order">The order which should be canceled.</param>
	void CancelOrder(Order order);

	/// <summary>
	/// Cancel orders by filter.
	/// </summary>
	/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
	/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
	/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
	/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
	/// <param name="security">Instrument. If the value is equal to <see langword="null" />, then the instrument does not match the orders cancel filter.</param>
	/// <param name="securityType">Security type. If the value is <see langword="null" />, the type does not use.</param>
	/// <param name="transactionId">Order cancellation transaction id.</param>
	void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null, SecurityTypes? securityType = null, long? transactionId = null);

	/// <summary>
	/// Determines the specified order can be edited by <see cref="EditOrder"/>.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <returns><see langword="true"/> if the order is editable, <see langword="false"/> order cannot be changed, <see langword="null"/> means no information.</returns>
	bool? IsOrderEditable(Order order);

	/// <summary>
	/// Determines the specified order can be replaced by <see cref="ReRegisterOrder"/>.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <returns><see langword="true"/> if the order is replaceable, <see langword="false"/> order cannot be replaced, <see langword="null"/> means no information.</returns>
	bool? IsOrderReplaceable(Order order);
}