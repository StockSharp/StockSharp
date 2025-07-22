namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.Strategies.Quoting;

partial class Strategy
{
	/// <summary>
	/// Subsidiary trade strategies.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Child strategies no longer supported.")]
	public INotifyList<Strategy> ChildStrategies { get; } = new SynchronizedList<Strategy>();

	/// <summary>
	/// The event of order successful registration.
	/// </summary>
	[Obsolete("Use OrderReceived event.")]
	public event Action<Order> OrderRegistered;

	/// <inheritdoc />
	[Obsolete("Use OrderRegisterFailReceived event.")]
	public event Action<OrderFail> OrderRegisterFailed;

	/// <inheritdoc />
	[Obsolete("Use OrderCancelFailReceived event.")]
	public event Action<OrderFail> OrderCancelFailed;

	/// <inheritdoc />
	[Obsolete("Use OrderReceived event.")]
	public event Action<Order> OrderChanged;

	/// <inheritdoc />
	[Obsolete("Use OrderReceived event.")]
	public event Action<long, Order> OrderEdited;

	/// <inheritdoc />
	[Obsolete("Use OrderEditFailReceived event.")]
	public event Action<long, OrderFail> OrderEditFailed;

	/// <inheritdoc />
	[Obsolete("Use OwnTradeReceived event.")]
	public event Action<MyTrade> NewMyTrade;

	/// <summary>
	/// <see cref="PnL"/> change event.
	/// </summary>
	[Obsolete("Use PnLReceived2 event.")]
	public event Action<Subscription> PnLReceived;

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

	/// <summary>
	/// The maximal number of errors, which strategy shall receive prior to stop operation.
	/// </summary>
	/// <remarks>
	/// The default value is 1.
	/// </remarks>
	[Browsable(false)]
	[Obsolete("Use RiskErrorRule rule.")]
	public int MaxErrorCount { get; set; }

	/// <summary>
	/// The current number of errors.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use RiskErrorRule rule.")]
	public int ErrorCount { get; private set; }

	/// <summary>
	/// The maximum number of order registration errors above which the algorithm will be stopped.
	/// </summary>
	/// <remarks>
	/// The default value is 10.
	/// </remarks>
	[Browsable(false)]
	[Obsolete("Use RiskOrderErrorRule rule.")]
	public int MaxOrderRegisterErrorCount { get; set; }

	/// <summary>
	/// Current number of order registration errors.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use RiskOrderErrorRule rule.")]
	public int OrderRegisterErrorCount { get; private set; }

	/// <summary>
	/// Current number of order changes.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use RiskOrderFreqRule rule.")]
	public int CurrentRegisterCount { get; private set; }

	/// <summary>
	/// The maximum number of orders above which the algorithm will be stopped.
	/// </summary>
	/// <remarks>
	/// The default value is <see cref="int.MaxValue"/>.
	/// </remarks>
	[Browsable(false)]
	[Obsolete("Use RiskOrderFreqRule rule.")]
	public int MaxRegisterCount { get; set; }

	/// <summary>
	/// The order registration interval above which the new order would not be registered.
	/// </summary>
	/// <remarks>
	/// By default, the interval is disabled and it is equal to <see cref="TimeSpan.Zero"/>.
	/// </remarks>
	[Browsable(false)]
	[Obsolete("Use RiskOrderFreqRule rule.")]
	public TimeSpan RegisterInterval { get; set; }

	/// <summary>
	/// The method is called when the <see cref="Start()"/> method has been called and the <see cref="ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
	/// </summary>
	[Obsolete("Use overload with time param.")]
	protected virtual void OnStarted()
	{
		OnStarted(CurrentTime);
	}

	/// <summary>
	/// To process orders, received for the connection <see cref="Connector"/>, and find among them those, belonging to the strategy.
	/// </summary>
	/// <param name="newOrders">New orders.</param>
	/// <returns>Orders, belonging to the strategy.</returns>
	[Obsolete("CanAttach method must be overrided.")]
	protected virtual IEnumerable<Order> ProcessNewOrders(IEnumerable<Order> newOrders)
	{
		return _ordersInfo.SyncGet(d => newOrders.Where(_ordersInfo.ContainsKey).ToArray());
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

	/// <summary>
	/// To open the position via quoting.
	/// </summary>
	/// <param name="finishPosition">The position value that should be reached. A negative value means the short position.</param>
	[Obsolete("Child strategies no longer supported.")]
	public void OpenPositionByQuoting(decimal finishPosition)
	{
		var position = Position;

		if (finishPosition == position)
			return;

		var delta = (finishPosition - position).Abs();

		ChildStrategies.Add(new MarketQuotingStrategy()
		{
			QuotingSide = finishPosition < position ? Sides.Sell : Sides.Buy,
			QuotingVolume = delta,
		});
	}

	/// <summary>
	/// To close the open position via quoting.
	/// </summary>
	[Obsolete("Child strategies no longer supported.")]
	public void ClosePositionByQuoting()
	{
		var position = Position;

		if (position == 0)
			return;

		ChildStrategies.Add(new MarketQuotingStrategy
		{
			QuotingSide = position > 0 ? Sides.Sell : Sides.Buy,
			QuotingVolume = position.Abs(),
		});
	}
}