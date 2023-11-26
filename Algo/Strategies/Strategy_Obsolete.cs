namespace StockSharp.Algo.Strategies;

using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System;

using StockSharp.BusinessEntities;
using StockSharp.Messages;

partial class Strategy
{
	/// <summary>
	/// <see cref="PnL"/> change event.
	/// </summary>
	[Obsolete("Use PnLReceived2 event.")]
	public event Action<Subscription> PnLReceived;

	/// <summary>
	/// Stop-orders, registered within the strategy framework.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use Orders property.")]
	public IEnumerable<Order> StopOrders => Orders.Where(o => o.Type == OrderTypes.Conditional);

	/// <summary>
	/// To add to <see cref="Order.Comment"/> the name of the strategy <see cref="Name"/>, registering the order.
	/// </summary>
	/// <remarks>
	/// It is disabled by default.
	/// </remarks>
	[Browsable(false)]
	[Obsolete("Use CommentMode property.")]
	public bool CommentOrders
	{
		get => CommentMode == StrategyCommentModes.Name;
		set => CommentMode = value ? StrategyCommentModes.Name : StrategyCommentModes.Disabled;
	}

#pragma warning disable 67
	/// <inheritdoc />
	[Obsolete("Use OrderRegisterFailed event.")]
	public event Action<OrderFail> StopOrderRegisterFailed;

	/// <inheritdoc />
	[Obsolete("Use OrderChanged event.")]
	public event Action<Order> StopOrderChanged;

	/// <summary>
	/// The event of sending stop-order for registration.
	/// </summary>
	[Obsolete("Use OrderRegistering event.")]
	public event Action<Order> StopOrderRegistering;

	/// <summary>
	/// The event of stop-order successful registration.
	/// </summary>
	[Obsolete("Use OrderRegistered event.")]
	public event Action<Order> StopOrderRegistered;

	/// <summary>
	/// The event of sending stop-order for cancelling.
	/// </summary>
	[Obsolete("Use OrderCanceling event.")]
	public event Action<Order> StopOrderCanceling;

	/// <summary>
	/// The event of sending stop-order for re-registration.
	/// </summary>
	[Obsolete("Use OrderReRegistering event.")]
	public event Action<Order, Order> StopOrderReRegistering;

	/// <inheritdoc />
	[Obsolete("Use OrderCancelFailed event.")]
	public event Action<OrderFail> StopOrderCancelFailed;
#pragma warning restore 67

	/// <summary>
	/// The method is called when the <see cref="Start()"/> method has been called and the <see cref="ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
	/// </summary>
	[Obsolete("Use overload with time param.")]
	protected virtual void OnStarted()
	{
		OnStarted(CurrentTime);
	}

	/// <summary>
	/// To add the active order to the strategy and process trades by the order.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <param name="myTrades">Trades for order.</param>
	/// <remarks>
	/// It is used to restore a state of the strategy, when it is necessary to subscribe for getting data on orders, registered earlier.
	/// </remarks>
	[Obsolete("CanAttach method must be overrided.")]
	public virtual void AttachOrder(Order order, IEnumerable<MyTrade> myTrades)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		if (myTrades == null)
			throw new ArgumentNullException(nameof(myTrades));

		AttachOrder(order, true);
	}
}