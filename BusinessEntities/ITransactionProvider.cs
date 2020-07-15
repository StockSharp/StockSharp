namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Transactional operations provider interface.
	/// </summary>
	public interface ITransactionProvider : IPortfolioProvider, IPositionProvider
	{
		/// <summary>
		/// Transaction id generator.
		/// </summary>
		IdGenerator TransactionIdGenerator { get; }

		/// <summary>
		/// Own trade received.
		/// </summary>
		event Action<MyTrade> NewMyTrade;

		/// <summary>
		/// Order received.
		/// </summary>
		event Action<Order> NewOrder;

		/// <summary>
		/// Order changed (cancelled, matched).
		/// </summary>
		event Action<Order> OrderChanged;

		/// <summary>
		/// <see cref="EditOrder"/> success result event.
		/// </summary>
		event Action<long, Order> OrderEdited;

		/// <summary>
		/// Order registration error event.
		/// </summary>
		event Action<OrderFail> OrderRegisterFailed;

		/// <summary>
		/// Order cancellation error event.
		/// </summary>
		event Action<OrderFail> OrderCancelFailed;

		/// <summary>
		/// <see cref="EditOrder"/> error result event.
		/// </summary>
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
		event Action<long, Exception> OrderStatusFailed;

		/// <summary>
		/// Failed order status request event.
		/// </summary>
		event Action<long, Exception, DateTimeOffset> OrderStatusFailed2;

		/// <summary>
		/// Stop-order registration error event.
		/// </summary>
		[Obsolete("Use OrderRegisterFailed event.")]
		event Action<OrderFail> StopOrderRegisterFailed;

		/// <summary>
		/// Stop-order cancellation error event.
		/// </summary>
		[Obsolete("Use OrderCancelFailed event.")]
		event Action<OrderFail> StopOrderCancelFailed;

		/// <summary>
		/// Stop-order received.
		/// </summary>
		[Obsolete("Use NewOrder event.")]
		event Action<Order> NewStopOrder;

		/// <summary>
		/// Stop order state change event.
		/// </summary>
		[Obsolete("Use OrderChanged event.")]
		event Action<Order> StopOrderChanged;

		/// <summary>
		/// Lookup result <see cref="PortfolioLookupMessage"/> received.
		/// </summary>
		event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> LookupPortfoliosResult;

		/// <summary>
		/// Lookup result <see cref="PortfolioLookupMessage"/> received.
		/// </summary>
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
		/// Subscribe on the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for subscription.</param>
		void RegisterPortfolio(Portfolio portfolio);

		/// <summary>
		/// Unsubscribe from the portfolio changes.
		/// </summary>
		/// <param name="portfolio">Portfolio for unsubscription.</param>
		void UnRegisterPortfolio(Portfolio portfolio);

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
}