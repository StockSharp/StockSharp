namespace StockSharp.Algo.Strategies;

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
	/// Orders with errors, registered within the strategy.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Subscribe on OrderRegisterFailed event.")]
	public IEnumerable<OrderFail> OrderFails => [];

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

	/// <inheritdoc />
	[Obsolete("Use SubscriptionStarted event.")]
	public event Action<Security, MarketDataMessage> MarketDataSubscriptionSucceeded
	{
		add => MarketDataProvider.MarketDataSubscriptionSucceeded += value;
		remove => MarketDataProvider.MarketDataSubscriptionSucceeded -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SubscriptionFailed event.")]
	public event Action<Security, MarketDataMessage, Exception> MarketDataSubscriptionFailed
	{
		add => MarketDataProvider.MarketDataSubscriptionFailed += value;
		remove => MarketDataProvider.MarketDataSubscriptionFailed -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SubscriptionFailed event.")]
	public event Action<Security, MarketDataMessage, SubscriptionResponseMessage> MarketDataSubscriptionFailed2
	{
		add => MarketDataProvider.MarketDataSubscriptionFailed2 += value;
		remove => MarketDataProvider.MarketDataSubscriptionFailed2 -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SubscriptionStopped event.")]
	public event Action<Security, MarketDataMessage> MarketDataUnSubscriptionSucceeded
	{
		add => MarketDataProvider.MarketDataUnSubscriptionSucceeded += value;
		remove => MarketDataProvider.MarketDataUnSubscriptionSucceeded -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SubscriptionFailed event.")]
	public event Action<Security, MarketDataMessage, Exception> MarketDataUnSubscriptionFailed
	{
		add => MarketDataProvider.MarketDataUnSubscriptionFailed += value;
		remove => MarketDataProvider.MarketDataUnSubscriptionFailed -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SubscriptionFailed event.")]
	public event Action<Security, MarketDataMessage, SubscriptionResponseMessage> MarketDataUnSubscriptionFailed2
	{
		add => MarketDataProvider.MarketDataUnSubscriptionFailed2 += value;
		remove => MarketDataProvider.MarketDataUnSubscriptionFailed2 -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SubscriptionStopped event.")]
	public event Action<Security, SubscriptionFinishedMessage> MarketDataSubscriptionFinished
	{
		add => MarketDataProvider.MarketDataSubscriptionFinished += value;
		remove => MarketDataProvider.MarketDataSubscriptionFinished -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SubscriptionFailed event.")]
	public event Action<Security, MarketDataMessage, Exception> MarketDataUnexpectedCancelled
	{
		add => MarketDataProvider.MarketDataUnexpectedCancelled += value;
		remove => MarketDataProvider.MarketDataUnexpectedCancelled -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SubscriptionOnline event.")]
	public event Action<Security, MarketDataMessage> MarketDataSubscriptionOnline
	{
		add => MarketDataProvider.MarketDataSubscriptionOnline += value;
		remove => MarketDataProvider.MarketDataSubscriptionOnline -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use TickTradeReceived event.")]
	public event Action<Trade> NewTrade
	{
		add => MarketDataProvider.NewTrade += value;
		remove => MarketDataProvider.NewTrade -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SecurityReceived event.")]
	public event Action<Security> NewSecurity
	{
		add => MarketDataProvider.NewSecurity += value;
		remove => MarketDataProvider.NewSecurity -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use SecurityReceived event.")]
	public event Action<Security> SecurityChanged
	{
		add => MarketDataProvider.SecurityChanged += value;
		remove => MarketDataProvider.SecurityChanged -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use OrderBookReceived event.")]
	public event Action<MarketDepth> NewMarketDepth
	{
		add => MarketDataProvider.NewMarketDepth += value;
		remove => MarketDataProvider.NewMarketDepth -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use OrderBookReceived event.")]
	public event Action<MarketDepth> MarketDepthChanged
	{
		add => MarketDataProvider.MarketDepthChanged += value;
		remove => MarketDataProvider.MarketDepthChanged -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use OrderLogItemReceived event.")]
	public event Action<OrderLogItem> NewOrderLogItem
	{
		add => MarketDataProvider.NewOrderLogItem += value;
		remove => MarketDataProvider.NewOrderLogItem -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use NewsReceived event.")]
	public event Action<News> NewNews
	{
		add => MarketDataProvider.NewNews += value;
		remove => MarketDataProvider.NewNews -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use NewsReceived event.")]
	public event Action<News> NewsChanged
	{
		add => MarketDataProvider.NewsChanged += value;
		remove => MarketDataProvider.NewsChanged -= value;
	}

	/// <inheritdoc />
	[Obsolete("Use OrderBookReceived event.")]
	public event Action<Subscription, MarketDepth> MarketDepthReceived;

	/// <inheritdoc />
	[Obsolete("Use OrderLogReceived event.")]
	public event Action<Subscription, OrderLogItem> OrderLogItemReceived;

	[Obsolete("Use Subscribe method.")]
	void ITransactionProvider.RegisterPortfolio(Portfolio portfolio)
	{
		SafeGetConnector().RegisterPortfolio(portfolio);
	}

	[Obsolete("Use UnSubscribe method.")]
	void ITransactionProvider.UnRegisterPortfolio(Portfolio portfolio)
	{
		SafeGetConnector().UnRegisterPortfolio(portfolio);
	}
}