namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Statistics;
using StockSharp.Algo.Strategies.Protective;

/// <summary>
/// The base class for all trade strategies.
/// </summary>
public partial class Strategy : BaseLogReceiver, IStrategyHost, IPositionProvider, INotifyPropertyChangedEx, ITimeProvider, IStrategy, ICloneable<Strategy>, ICustomTypeDescriptor, IScheduledTask
{
	private IConnector _connector;
	private readonly StrategyPositionManager _posManager;
	private readonly HashSet<long> _ownTransactionIds = [];
	private readonly HashSet<Order> _pendingOwnOrders = [];
	private readonly HashSet<long> _ordersAdjustedByTrade = [];
	private readonly Dictionary<(SecurityId secId, string pfName), decimal> _positions = [];
	private string _idStr;
	private BoardMessage _boardMsg;
	private Subscription _portfolioLookup;
	private DateTime _prevTradeDate;
	private bool _isPrevDateTradable;
	private DateTime _firstOrderTime;
	private DateTime _lastOrderTime;
	private TimeSpan _maxOrdersKeepTime = TimeSpan.FromTicks((long)(TimeSpan.TicksPerDay * 1.5));
	private bool _isTradingBlocked;
	// Set the moment a stop is requested (before the Stopping state round-trips through the engine) so
	// CanTrade rejects new orders immediately, matching the monolith _stopping flag. Cleared on start/reset.
	private bool _stopping;
	private bool _isOnline;
	private decimal _position;
	private LogLevels _errorState;
	private string _lastCantTradeReason;
	private DateTime _startedTime;
	private TimeSpan _totalWorkingTime;
	private Action<TimeSpan> _currentTimeChanged;
	private bool _isProcessingConnectorMessage;

	private Unit _takeProfit, _stopLoss;
	private bool _isStopTrailing;
	private TimeSpan _takeTimeout, _stopTimeout;
	private bool _protectiveUseMarketOrders;
	private bool _isLocalStop;
	private ProtectiveController _protectiveController;
	private IProtectivePositionController _posController;

	/// <summary>
	/// Initializes a new instance <see cref="Strategy"/>.
	/// </summary>
	public Strategy()
		: this(new PnLManager { UseOrderBook = true }, new StatisticManager())
	{
	}

	/// <summary>
	/// Initializes a new instance <see cref="Strategy"/>.
	/// </summary>
	/// <param name="pnlManager">PnL manager.</param>
	/// <param name="stats">Statistic manager.</param>
	public Strategy(IPnLManager pnlManager, IStatisticManager stats)
	{
		PnLManager = pnlManager ?? throw new ArgumentNullException(nameof(pnlManager));
		StatisticManager = stats ?? throw new ArgumentNullException(nameof(stats));

		_posManager = new(EnsureGetId);
		_posManager.PositionProcessed += ProcessStrategyPosition;

		Engine = new(this, PnLManager);
		OrderProcessor = new(StatisticManager);
		Trades = new(PnLManager, StatisticManager);
		Positions = new(StatisticManager);
		Subscriptions = new(this);
		RiskManager = new RiskManager();

		Parameters = new(this);

		InitParameters();

		// Wire the indicator collection's default-source inheritance (mirrors the monolith IndicatorList.OnAdded).
		InitIndicators();

		NameGenerator = new(this);
		// Apply generated names through the base setter so auto-generation is not disabled by our own update.
		NameGenerator.Changed += name => base.Name = name;
		// Seed the default short name (e.g. "SS" for SmaStrategy); the generator refines it as Security/Portfolio
		// are assigned. The generator's initial Changed fires before the handler above is attached, so set it here.
		base.Name = NameGenerator.ShortName;

		Subscriptions.SubscriptionRequested += s => _connector?.Subscribe(s);
		Subscriptions.UnsubscriptionRequested += s => _connector?.UnSubscribe(s);

		// Gate the final Stopping -> Stopped transition exactly as the monolith TryFinalStop does:
		// defer it only while WaitRulesOnStop is set and rules are still outstanding.
		Engine.CanFinalStop = CanFinalStop;

		// Stop-time subscription teardown. This is the SINGLE unsubscription path: it is wired on the engine
		// event (not inside the overridable OnStateChanged) so it always runs, even for subclasses that
		// override OnStateChanged without calling base - the teardown must not depend on a base call.
		// OnStateChanged keeps only the overridable OnStopping() user hook, mirroring the monolith where the
		// unsubscription lives in OnStopping and the cancellation is fixed.
		Engine.StateChanged += state =>
		{
			if (state != ProcessStates.Stopping)
				return;

			if (UnsubscribeOnStop)
				Subscriptions.UnSubscribeAll(globalAndLocal: false);

			Subscriptions.UnSubscribeAll(globalAndLocal: true);
			IsOnline = false;
		};
		Engine.StateChanged += OnStateChanged;
		Engine.StateChanged += _ => this.Notify(nameof(ProcessState));
		// Stop-time order cancellation, sequenced after the unsubscription above and the OnStopping() hook,
		// matching the monolith which cancels active orders after OnStopping() has unsubscribed. Wired on the
		// engine event (always runs) for the same robustness as the unsubscription path.
		Engine.StateChanged += state =>
		{
			if (state == ProcessStates.Stopping && CancelOrdersWhenStopping)
				CancelAllActiveOrders();
		};
		Engine.StateChanged += _ => RefreshOnlineState();
		Engine.StateChanged += _ => ProcessStateChanged?.Invoke(this);
		Engine.CurrentPriceUpdated += (secId, price, serverTime, localTime) =>
		{
			_posManager.UpdateCurrentPrice(secId, price, serverTime, localTime);
			OnCurrentPriceUpdated(secId, price, serverTime, localTime);

			if (!_isProcessingConnectorMessage)
				TryActivateProtection(secId, price, serverTime, requireStarted: false);
		};
		OrderProcessor.Registered += OnOrderRegistered;
#pragma warning disable CS0618 // the OrderRegistered event is obsolete but still part of the strategy surface.
		OrderProcessor.Registered += o => OrderRegistered?.Invoke(o);
#pragma warning restore CS0618
		OrderProcessor.Registered += order =>
		{
			// Conditional (stop/take) orders never fold their commission into the strategy total
			// (see OrderPipeline.ProcessOrder), so do not raise the commission-changed notification for
			// them either - matching the monolith ProcessOrder. Latency is still applied for both kinds.
			if (order.Type != OrderTypes.Conditional && order.Commission is not null)
				RaiseCommissionChanged();

			ChangeLatency(order.LatencyRegistration);
		};
		OrderProcessor.Registered += TrackOrderLifetime;
		OrderProcessor.Registered += order =>
		{
			// An order can be confirmed after the strategy has already entered the Stopping state (the
			// initial CancelAllActiveOrders sweep ran before it was acknowledged). Re-cancel such a freshly
			// confirmed, still-cancelable order, exactly once - mirroring the monolith ProcessOrder stop-time
			// re-cancel guarded by info.IsCanceled.
			if (ProcessState == ProcessStates.Stopping && CancelOrdersWhenStopping
				&& !order.State.IsFinal() && OrderProcessor.TryMarkCanceled(order))
			{
				// Go straight to the connector: the public CancelOrder would be blocked by its Stopping-state guard.
				CancelOrderHandler(order);
			}
		};
		OrderProcessor.Changed += HandleOrderChanged;
		Trades.TradeAdded += OnOwnTradeReceived;
#pragma warning disable CS0618 // the NewMyTrade event is obsolete but still part of the strategy surface.
		Trades.TradeAdded += t => NewMyTrade?.Invoke(t);
#pragma warning restore CS0618
		Trades.PnLChanged += RaisePnLChanged;
		Trades.CommissionChanged += RaiseCommissionChanged;
		Trades.SlippageChanged += RaiseSlippageChanged;
		Trades.TradeAdded += trade =>
		{
			trade.Position = GetPositionValue(trade.Order.Security, trade.Order.Portfolio);

			if (_protectiveController is not null)
			{
				var security = trade.Order.Security;
				var portfolio = trade.Order.Portfolio;

				_posController ??= _protectiveController.GetController(
					security.ToSecurityId(),
					portfolio.Name,
					GetProtectiveBehaviourFactory(security, portfolio),
					_takeProfit ?? new(), _stopLoss ?? new(), _isStopTrailing, _takeTimeout, _stopTimeout, _protectiveUseMarketOrders);

				var info = _posController?.Update(trade.Trade.Price, trade.GetPosition(), trade.Trade.ServerTime);

				if (info is not null)
					ActiveProtection(info.Value);
			}
		};
		Positions.NewPosition += OnNewPosition;
		Positions.PositionChanged += OnPositionChanged;
		Engine.PnLRefreshRequired += time =>
		{
			if (((IStrategyHost)this).HasPositions)
				RaisePnLChanged(time);
		};
	}

	/// <summary>
	/// State machine + message routing.
	/// </summary>
	[Browsable(false)]
	public StrategyEngine Engine { get; }

	/// <summary>
	/// Order tracking and processing pipeline.
	/// </summary>
	[Browsable(false)]
	public OrderPipeline OrderProcessor { get; }

	/// <summary>
	/// Strategy orders.
	/// </summary>
	[Browsable(false)]
	public IEnumerable<Order> Orders => OrderProcessor.Orders;

	/// <summary>
	/// Trade processing and PnL.
	/// </summary>
	[Browsable(false)]
	public TradePipeline Trades { get; }

	/// <summary>
	/// Position event handling.
	/// </summary>
	[Browsable(false)]
	public PositionPipeline Positions { get; }

	/// <summary>
	/// Subscription management.
	/// </summary>
	[Browsable(false)]
	public SubscriptionRegistry Subscriptions { get; }

	private IPnLManager _pnLManager;
	private IStatisticManager _statisticManager;

	/// <summary>
	/// PnL manager.
	/// </summary>
	[Browsable(false)]
	public IPnLManager PnLManager
	{
		get => _pnLManager;
		set
		{
			// Match the monolith public setter: reject null. The decomposed engine and the trade pipeline
			// capture the manager by reference, so re-point them when it is swapped (the dependents are
			// null only during the very first assignment from the constructor, where they get the manager
			// through their own constructors instead).
			_pnLManager = value ?? throw new ArgumentNullException(nameof(value));

			Engine?.SetPnLManager(_pnLManager);
			Trades?.SetPnLManager(_pnLManager);
		}
	}

	/// <summary>
	/// Statistics manager.
	/// </summary>
	[Browsable(false)]
	public IStatisticManager StatisticManager
	{
		get => _statisticManager;
		protected set
		{
			// Match the monolith protected setter: reject null and re-point the pipelines that capture the
			// statistic manager by reference (null only during the constructor's first assignment).
			_statisticManager = value ?? throw new ArgumentNullException(nameof(value));

			OrderProcessor?.SetStatisticManager(_statisticManager);
			Trades?.SetStatisticManager(_statisticManager);
			Positions?.SetStatisticManager(_statisticManager);
		}
	}

	private IRiskManager _riskManager;

	/// <summary>
	/// Risk manager.
	/// </summary>
	[Browsable(false)]
	public IRiskManager RiskManager
	{
		get => _riskManager;
		set
		{
			// Match the monolith: reject a null manager and adopt the strategy as the rule parent
			// (only when it is not already owned) so error-count / position risk rules resolve correctly.
			_riskManager = value ?? throw new ArgumentNullException(nameof(value));
			_riskManager.Parent ??= this;
		}
	}

	/// <summary>
	/// Connector (via interface for testability).
	/// </summary>
	[Browsable(false)]
	public virtual IConnector Connector
	{
		get => _connector;
		set
		{
			if (_connector == value)
				return;

			if (_connector != null)
				UnsubscribeConnector();

			_connector = value;

			if (_connector != null)
				SubscribeConnector();

			ConnectorChanged?.Invoke();
		}
	}

	/// <inheritdoc />
	public void RaiseParametersChanged([CallerMemberName]string name = default)
	{
		ParametersChanged?.Invoke();
		this.Notify(name);
	}

	/// <summary>
	/// Current position (primary security).
	/// </summary>
	[Browsable(false)]
	public decimal Position
	{
		get => Security == null || Portfolio == null ? _position : GetPositionValue(Security, Portfolio) ?? _position;
		set
		{
			_position = value;

			if (Security != null)
			{
				var key = (Security.ToSecurityId(), Portfolio?.Name ?? string.Empty);
				_positions[key] = value;

				if (Portfolio != null)
					_posManager.SetPosition(Security, Portfolio, value, ((IStrategyHost)this).CurrentTime);
			}
		}
	}

	/// <summary>
	/// Get position value for specific security and portfolio.
	/// </summary>
	public decimal? GetPositionValue(Security sec, Portfolio pf)
	{
		return _posManager.TryGetPosition(sec, pf)?.CurrentValue;
	}

	/// <summary>
	/// All tracked positions.
	/// </summary>
	[Browsable(false)]
	public IReadOnlyDictionary<(SecurityId secId, string pfName), decimal> PositionsList => _positions;

	/// <summary>
	/// Latency.
	/// </summary>
	[Browsable(false)]
	public TimeSpan? Latency { get; set; }

	/// <summary>
	/// Error state.
	/// </summary>
	[Browsable(false)]
	public LogLevels ErrorState
	{
		get => _errorState;
		set
		{
			if (_errorState == value)
				return;

			_errorState = value;
			this.Notify();
		}
	}

	/// <inheritdoc />
	protected override void RaiseLog(LogMessage message)
	{
		if (message is null)
			throw new ArgumentNullException(nameof(message));

		// Promote ErrorState from logging exactly as the monolith Strategy does, so the ErrorState change
		// stream matches it: a warning raises Info -> Warning, an error escalates straight to Error.
		switch (message.Level)
		{
			case LogLevels.Warning:
				if (ErrorState == LogLevels.Info)
					ErrorState = LogLevels.Warning;
				break;
			case LogLevels.Error:
				ErrorState = LogLevels.Error;
				break;
		}

		base.RaiseLog(message);
	}

	/// <summary>
	/// Strategy start time.
	/// </summary>
	[Browsable(false)]
	public DateTime StartedTime
	{
		get => _startedTime;
		private set
		{
			if (_startedTime == value)
				return;

			_startedTime = value;
			this.Notify();
		}
	}

	/// <summary>
	/// Total working time.
	/// </summary>
	[Browsable(false)]
	public TimeSpan TotalWorkingTime
	{
		get => _totalWorkingTime;
		private set
		{
			if (_totalWorkingTime == value)
				return;

			_totalWorkingTime = value;
			this.Notify();
		}
	}

	/// <summary>
	/// Whether the strategy is formed.
	/// </summary>
	/// <remarks>
	/// Default implementation uses the <see cref="Indicators"/> collection to check that every tracked
	/// indicator is formed. An empty collection is considered formed, matching the monolith strategy's
	/// <c>AllFormed</c> semantics.
	/// </remarks>
	[Browsable(false)]
	public virtual bool IsFormed => _indicators.All(i => i.IsFormed);

	/// <summary>
	/// Is online.
	/// </summary>
	[Browsable(false)]
	public bool IsOnline
	{
		get => _isOnline;
		private set
		{
			if (_isOnline == value)
				return;

			_isOnline = value;
			this.Notify();
			IsOnlineChanged?.Invoke(this);
		}
	}

	/// <summary>
	/// Total accumulated commission (from orders + trades).
	/// </summary>
	[Browsable(false)]
	public decimal? Commission =>
		OrderProcessor.Commission is null && Trades.Commission is null
			? null : (OrderProcessor.Commission ?? 0m) + (Trades.Commission ?? 0m);

	/// <summary>
	/// Total accumulated slippage.
	/// </summary>
	[Browsable(false)]
	public decimal? Slippage => Trades.Slippage;

	/// <summary>
	/// Error event.
	/// </summary>
	public event Action<IStrategy, Exception> Error;

	/// <summary>
	/// Connector changed event.
	/// </summary>
	public event Action ConnectorChanged;

	/// <summary>
	/// Strategy parameters changed event.
	/// </summary>
	public event Action ParametersChanged;

	/// <summary>
	/// <see cref="ProcessState"/> change event.
	/// </summary>
	public event Action<IStrategy> ProcessStateChanged;

	/// <summary>
	/// The event of order successful registration.
	/// </summary>
	[Obsolete("Use OrderReceived event.")]
	public event Action<Order> OrderRegistered;

	/// <summary>
	/// The event of order change.
	/// </summary>
	[Obsolete("Use OrderReceived event.")]
	public event Action<Order> OrderChanged;

	/// <summary>
	/// The event of new own trade occurring.
	/// </summary>
	[Obsolete("Use OwnTradeReceived event.")]
	public event Action<MyTrade> NewMyTrade;

	/// <summary>
	/// Strategy parameters.
	/// </summary>
	[Browsable(false)]
	public StrategyParameterDictionary Parameters { get; }

	private StrategyParam<T> Param<T>(StrategyParam<T> p)
	{
		Parameters.Add(p ?? throw new ArgumentNullException(nameof(p)));
		return p;
	}

	/// <summary>
	/// Create and register a strategy parameter.
	/// </summary>
	/// <typeparam name="T">The type of the parameter value.</typeparam>
	/// <param name="id">Parameter identifier.</param>
	/// <param name="initialValue">The initial value.</param>
	/// <returns>The strategy parameter.</returns>
	public StrategyParam<T> Param<T>(string id, T initialValue = default)
		=> Param(new StrategyParam<T>(id, initialValue)).SetBasic(true);

	/// <summary>
	/// Current profit-loss.
	/// </summary>
	[Browsable(false)]
	public decimal PnL => PnLManager.GetPnL();

	/// <summary>
	/// The last error that occurred in the strategy.
	/// </summary>
	[Browsable(false)]
	public Exception LastError { get; private set; }

	/// <summary>
	/// Strategy reset event.
	/// </summary>
	public event Action Reseted;

	/// <inheritdoc />
	public event PropertyChangedEventHandler PropertyChanged;

	/// <summary>
	/// Commission changed event.
	/// </summary>
	public event Action CommissionChanged;

	/// <summary>
	/// Slippage changed event.
	/// </summary>
	public event Action SlippageChanged;

	/// <summary>
	/// Latency changed event.
	/// </summary>
	public event Action LatencyChanged;

	/// <summary>
	/// PnL changed event.
	/// </summary>
	public event Action PnLChanged;

	/// <summary>
	/// PnL received event.
	/// </summary>
	[Obsolete("Use PnLReceived2 event.")]
	public event Action<Subscription> PnLReceived;

	/// <summary>
	/// PnL received event.
	/// </summary>
	public event Action<Subscription, Portfolio, DateTime, decimal, decimal?, decimal?> PnLReceived2;

	/// <summary>
	/// Order is about to be registered.
	/// </summary>
	public event Action<Order> OrderRegistering;

	/// <summary>
	/// Order is about to be re-registered.
	/// </summary>
	public event Action<Order, Order> OrderReRegistering;

	/// <summary>
	/// Order is about to be canceled.
	/// </summary>
	public event Action<Order> OrderCanceling;

	/// <summary>
	/// Order registration failed.
	/// </summary>
	[Obsolete("Use OrderRegisterFailReceived event.")]
	public event Action<OrderFail> OrderRegisterFailed;

	/// <summary>
	/// Order edited.
	/// </summary>
	[Obsolete("Use OrderReceived event.")]
	public event Action<long, Order> OrderEdited;

	/// <summary>
	/// Order edit failed.
	/// </summary>
	[Obsolete("Use OrderEditFailReceived event.")]
	public event Action<long, OrderFail> OrderEditFailed;

	/// <summary>
	/// Order cancellation failed.
	/// </summary>
	[Obsolete("Use OrderCancelFailReceived event.")]
	public event Action<OrderFail> OrderCancelFailed;

	/// <summary>
	/// Order registration failure.
	/// </summary>
	public event Action<Subscription, OrderFail> OrderRegisterFailReceived;

	/// <summary>
	/// Order cancel failure.
	/// </summary>
	public event Action<Subscription, OrderFail> OrderCancelFailReceived;

	/// <summary>
	/// Order edit failure.
	/// </summary>
	public event Action<Subscription, OrderFail> OrderEditFailReceived;

	/// <summary>
	/// Subscription is started.
	/// </summary>
	public event Action<Subscription> SubscriptionStarted;

	/// <summary>
	/// Subscription is online.
	/// </summary>
	public event Action<Subscription> SubscriptionOnline;

	/// <summary>
	/// Subscription is stopped.
	/// </summary>
	public event Action<Subscription, Exception> SubscriptionStopped;

	/// <summary>
	/// Subscription failed.
	/// </summary>
	public event Action<Subscription, Exception, bool> SubscriptionFailed;

	/// <summary>
	/// Subscription value received.
	/// </summary>
	public event Action<Subscription, object> SubscriptionReceived;

	/// <summary>
	/// Level1 value received.
	/// </summary>
	public event Action<Subscription, Level1ChangeMessage> Level1Received;

	/// <summary>
	/// Order book value received.
	/// </summary>
	public event Action<Subscription, IOrderBookMessage> OrderBookReceived;

	/// <summary>
	/// Tick trade value received.
	/// </summary>
	public event Action<Subscription, ITickTradeMessage> TickTradeReceived;

	/// <summary>
	/// Order log value received.
	/// </summary>
	public event Action<Subscription, IOrderLogMessage> OrderLogReceived;

	/// <summary>
	/// Security value received.
	/// </summary>
	public event Action<Subscription, Security> SecurityReceived;

	/// <summary>
	/// Board value received.
	/// </summary>
	public event Action<Subscription, ExchangeBoard> BoardReceived;

	/// <summary>
	/// News value received.
	/// </summary>
	public event Action<Subscription, News> NewsReceived;

	/// <summary>
	/// Candle value received.
	/// </summary>
	public event Action<Subscription, ICandleMessage> CandleReceived;

	/// <summary>
	/// Own trade value received.
	/// </summary>
	public event Action<Subscription, MyTrade> OwnTradeReceived;

	/// <summary>
	/// Order value received.
	/// </summary>
	public event Action<Subscription, Order> OrderReceived;

	/// <summary>
	/// Portfolio value received.
	/// </summary>
	public event Action<Subscription, Portfolio> PortfolioReceived;

	/// <summary>
	/// Position value received.
	/// </summary>
	public event Action<Subscription, Position> PositionReceived;

	/// <summary>
	/// Data type value received.
	/// </summary>
	public event Action<Subscription, DataType> DataTypeReceived;

	/// <summary>
	/// Online state changed.
	/// </summary>
	public event Action<Strategy> IsOnlineChanged;

	/// <summary>
	/// Position changed.
	/// </summary>
	[Obsolete("Use IPositionProvider.PositionChanged instead.")]
	public event Action PositionChanged;

	private Action<Position> _newPosition;
	private Action<Position> _positionChanged;

	/// <summary>
	/// Current process state.
	/// </summary>
	[Browsable(false)]
	public ProcessStates ProcessState => Engine.ProcessState;

	/// <summary>
	/// Start the strategy.
	/// </summary>
	public ValueTask StartAsync(CancellationToken cancellationToken = default)
	{
		// Clear the stop request so CanTrade allows orders again on (re)start, matching the monolith Start.
		_stopping = false;
		_maxOrdersKeepTime = TimeSpan.FromTicks((long)(OrdersKeepTime.Ticks * 1.5));
		return Engine.RequestStartAsync(cancellationToken);
	}

	/// <summary>
	/// Start the strategy.
	/// </summary>
	[Obsolete("Use StartAsync instead.")]
	public void Start() => AsyncHelper.Run(() => StartAsync());

	/// <summary>
	/// Stop the strategy.
	/// </summary>
	public ValueTask StopAsync(CancellationToken cancellationToken = default)
	{
		// Flag the stop request before the Stopping state round-trips through the engine, so CanTrade
		// rejects new orders immediately - matching the monolith Stop. Skip the no-op already-stopped case.
		if (ProcessState != ProcessStates.Stopped)
			_stopping = true;

		return Engine.RequestStopAsync(cancellationToken);
	}

	/// <summary>
	/// Stop the strategy with error.
	/// </summary>
	/// <param name="error">The error that caused the stop.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	public ValueTask StopAsync(Exception error, CancellationToken cancellationToken = default)
	{
		OnError(error);
		return StopAsync(cancellationToken);
	}

	/// <summary>
	/// Stop the strategy.
	/// </summary>
	[Obsolete("Use StopAsync instead.")]
	public void Stop() => AsyncHelper.Run(() => StopAsync());

	/// <summary>
	/// Register an order via the connector.
	/// </summary>
	public void RegisterOrder(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		PrepareNewOrder(order);

		if (RiskManager.Rules.Count > 0)
		{
			var action = ProcessRisk(order.CreateRegisterMessage());

			if (action is not null)
			{
				// A risk rule fired: veto the triggering order, it must not reach the connector.
				ProcessOrderFail(order, new InvalidOperationException(action.Value.GetFieldDisplayName()));
				return;
			}
		}

		if (!CanTrade(order.Security ?? Security, order.Portfolio ?? Portfolio, order.Side, order.Volume, out var reason))
		{
			ProcessOrderFail(order, new InvalidOperationException(reason));
			return;
		}

		OrderProcessor.TryAttach(order);
		OnOrderRegistering(order);
		SubmitNewOrder(order, () => _connector.RegisterOrder(order));
	}

	/// <summary>
	/// Cancel an order via the connector.
	/// </summary>
	public void CancelOrder(Order order)
	{
		if (ProcessState != ProcessStates.Started)
		{
			LogWarning(LocalizedStrings.StrategyInStateCannotCancelOrder, ProcessState);
			return;
		}

		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (TradingMode == StrategyTradingModes.Disabled)
		{
			LogWarning(LocalizedStrings.TradingDisabled);
			return;
		}

		if (!OrderProcessor.IsTracked(order))
			throw new ArgumentException(LocalizedStrings.OrderNotFromStrategy.Put(order.TransactionId, Name));

		if (!OrderProcessor.TryMarkCanceled(order))
		{
			LogWarning(LocalizedStrings.OrderAlreadySentCancel, order.TransactionId);
			return;
		}

		CancelOrderHandler(order);
	}

	/// <summary>
	/// Dispatch a cancel without the public guards (stop-time teardown); the caller handles the IsCanceled dedup.
	/// </summary>
	private void CancelOrderHandler(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		OrderCanceling?.Invoke(order);
		_connector.CancelOrder(order);
	}

	/// <summary>
	/// Edit an order via the connector.
	/// </summary>
	/// <param name="order">Original order.</param>
	/// <param name="changes">Order changes.</param>
	public void EditOrder(Order order, Order changes)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (changes is null)
			throw new ArgumentNullException(nameof(changes));

		if (!CanTrade(order.Security ?? Security, order.Portfolio ?? Portfolio, order.Side, changes.Volume, out var reason))
		{
			ProcessOrderFail(order, new InvalidOperationException(reason));
			return;
		}

		if (RiskManager.Rules.Count > 0)
		{
			// A risk rule fired: skip the edit, mirroring the monolith which only edits when no action.
			if (ProcessRisk(changes.CreateRegisterMessage()) is not null)
				return;
		}

		_connector.EditOrder(order, changes);
	}

	/// <summary>
	/// Re-register (cancel + register new) an order via the connector.
	/// </summary>
	/// <param name="oldOrder">Order to cancel.</param>
	/// <param name="newOrder">New order to register.</param>
	public void ReRegisterOrder(Order oldOrder, Order newOrder)
	{
		if (oldOrder is null)
			throw new ArgumentNullException(nameof(oldOrder));

		if (newOrder is null)
			throw new ArgumentNullException(nameof(newOrder));

		if (!CanTrade(newOrder.Security ?? Security, newOrder.Portfolio ?? Portfolio, newOrder.Side, newOrder.Volume, out var reason))
		{
			ProcessOrderFail(newOrder, new InvalidOperationException(reason));
			return;
		}

		PrepareNewOrder(newOrder);

		if (RiskManager.Rules.Count > 0)
		{
			var action = ProcessRisk(newOrder.CreateRegisterMessage());

			if (action is not null)
			{
				// A risk rule fired: veto the new order, it must not reach the connector.
				ProcessOrderFail(newOrder, new InvalidOperationException(action.Value.GetFieldDisplayName()));
				return;
			}
		}

		OrderProcessor.TryAttach(newOrder);
		OnOrderReRegistering(oldOrder, newOrder);
		SubmitNewOrder(newOrder, () => _connector.ReRegisterOrder(oldOrder, newOrder));
	}

	/// <summary>
	/// Create an initialized order object.
	/// </summary>
	/// <param name="side">Order side.</param>
	/// <param name="price">Price. 0 for market order.</param>
	/// <param name="volume">Volume. If null, <see cref="Volume"/> is used.</param>
	/// <param name="security">Security. If null, <see cref="Security"/> is used.</param>
	/// <returns>Order.</returns>
	public Order CreateOrder(Sides side, decimal price, decimal? volume = null, Security security = null)
	{
		var order = new Order
		{
			Portfolio = Portfolio,
			Security = security ?? Security,
			Side = side,
			Volume = volume ?? Volume,
		};

		if (price == 0)
			order.Type = OrderTypes.Market;
		else
			order.Price = price;

		return order;
	}

	/// <summary>
	/// Buy at market price.
	/// </summary>
	/// <param name="volume">Volume. If null, <see cref="Volume"/> is used.</param>
	/// <param name="security">Security. If null, <see cref="Security"/> is used.</param>
	public Order BuyMarket(decimal? volume = null, Security security = null)
	{
		var order = CreateOrder(Sides.Buy, default, volume, security);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Sell at market price.
	/// </summary>
	/// <param name="volume">Volume. If null, <see cref="Volume"/> is used.</param>
	/// <param name="security">Security. If null, <see cref="Security"/> is used.</param>
	public Order SellMarket(decimal? volume = null, Security security = null)
	{
		var order = CreateOrder(Sides.Sell, default, volume, security);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Buy at limit price.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">Volume. If null, <see cref="Volume"/> is used.</param>
	/// <param name="security">Security. If null, <see cref="Security"/> is used.</param>
	public Order BuyLimit(decimal price, decimal? volume = null, Security security = null)
	{
		var order = CreateOrder(Sides.Buy, price, volume, security);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Sell at limit price.
	/// </summary>
	/// <param name="price">Price.</param>
	/// <param name="volume">Volume. If null, <see cref="Volume"/> is used.</param>
	/// <param name="security">Security. If null, <see cref="Security"/> is used.</param>
	public Order SellLimit(decimal price, decimal? volume = null, Security security = null)
	{
		var order = CreateOrder(Sides.Sell, price, volume, security);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Close open position by market (register a <see cref="OrderTypes.Market"/> order).
	/// </summary>
	/// <param name="security"><see cref="Security"/>. If not passed, the value from <see cref="Security"/> is used.</param>
	/// <param name="portfolio"><see cref="Portfolio"/>. If not passed, the value from <see cref="Portfolio"/> is used.</param>
	/// <returns>The initialized order object, or <see langword="null"/> when there is no position to close.</returns>
	public Order ClosePosition(Security security = default, Portfolio portfolio = default)
	{
		var position = security is null ? Position : GetPositionValue(security, portfolio) ?? default;

		if (position == 0)
			return null;

		var volume = position.Abs();
		return position > 0 ? SellMarket(volume) : BuyMarket(volume);
	}

	/// <summary>
	/// Cancel all active orders unconditionally (used internally on stop and risk actions, where the public
	/// <see cref="CancelActiveOrders(bool?, Portfolio, Sides?, ExchangeBoard, Security, SecurityTypes?, long?)"/>
	/// would be skipped because the strategy is no longer in the <see cref="ProcessStates.Started"/> state).
	/// </summary>
	private void CancelAllActiveOrders()
	{
		// Bypass the public CancelOrder Started-state guard (runs at stop time / on risk actions); IsCanceled
		// dedup still respected via TryMarkCanceled. Mirrors the monolith ProcessCancelActiveOrders.
		foreach (var order in OrderProcessor.Orders.Where(o => o.State == OrderStates.Active))
		{
			if (OrderProcessor.TryMarkCanceled(order))
				CancelOrderHandler(order);
		}
	}

	private void TrackOrderLifetime(Order order)
	{
		if (order.Time == default)
			return;

		if (_firstOrderTime == default)
			_firstOrderTime = order.Time;

		_lastOrderTime = order.Time;
		RecycleOrders();
	}

	private void HandleOrderChanged(Order order)
	{
#pragma warning disable CS0618 // the public OrderChanged event is obsolete but still part of the strategy surface.
		OrderChanged?.Invoke(order);
#pragma warning restore CS0618
		OnOrderChanged(order);

		// Apply cancellation latency on a non-registration order change, matching the monolith ProcessOrder
		// isChanging branch which calls ChangeLatency(order.LatencyCancellation).
		ChangeLatency(order.LatencyCancellation);
	}

	private void ProcessStrategyPosition(Position position, bool isNew)
	{
		ArgumentNullException.ThrowIfNull(position);

		var security = position.Security;
		var portfolio = position.Portfolio;

		if (security != null)
		{
			var key = (security.ToSecurityId(), portfolio?.Name ?? string.Empty);
			_positions[key] = position.CurrentValue ?? 0;

			if (Security != null && key == (Security.ToSecurityId(), Portfolio?.Name ?? string.Empty))
				_position = position.CurrentValue ?? 0;
		}

		ProcessRisk(position.ToChangeMessage());

		if (isNew)
			_newPosition?.Invoke(position);
		else
			_positionChanged?.Invoke(position);

		RaisePositionChanged(position.LocalTime);

		foreach (var subscription in Subscriptions.Subscriptions.Where(s => s.SubscriptionMessage is PortfolioLookupMessage))
			PositionReceived?.Invoke(subscription, position);
	}

	private void RaisePositionChanged(DateTime time)
	{
		this.Notify(nameof(Position));
		PositionChanged?.Invoke();
		StatisticManager.AddPosition(time, Position);
		StatisticManager.AddPnL(time, PnLManager.GetPnL(), Commission);
	}

	private void RaisePnLChanged(DateTime time)
	{
		this.Notify(nameof(PnL));
		PnLChanged?.Invoke();

		var subscription = Subscriptions.Subscriptions.FirstOrDefault(s => s.SubscriptionMessage is PortfolioLookupMessage) ?? PortfolioLookup;

#pragma warning disable CS0618 // the PnLReceived event is obsolete but still part of the strategy surface.
		PnLReceived?.Invoke(subscription);
#pragma warning restore CS0618

		if (Portfolio is not null)
			PnLReceived2?.Invoke(subscription, Portfolio, time, PnLManager.RealizedPnL, PnLManager.UnrealizedPnL, Commission);

		// Attribute stats to the engine's PnL-refresh time (not the notification time), as the monolith does,
		// so time-typed stats (MaxProfitDate/MaxDrawdownDate) stay identical.
		StatisticManager.AddPnL(Engine.LastPnLRefreshTime, PnLManager.GetPnL(), Commission);
	}

	private void RaiseCommissionChanged()
	{
		this.Notify(nameof(Commission));
		CommissionChanged?.Invoke();
	}

	private void RaiseSlippageChanged()
	{
		this.Notify(nameof(Slippage));
		SlippageChanged?.Invoke();
	}

	private void RaiseLatencyChanged()
	{
		this.Notify(nameof(Latency));
		LatencyChanged?.Invoke();
	}

	// Accumulate the per-order latency (registration / cancellation / edition) into the running total and
	// notify, matching the monolith ChangeLatency: a null or zero delta is ignored.
	private void ChangeLatency(TimeSpan? diff)
	{
		if (diff is not TimeSpan delta || delta == TimeSpan.Zero)
			return;

		Latency = (Latency ?? TimeSpan.Zero) + delta;
		RaiseLatencyChanged();
	}

	private void RecycleOrders()
	{
		if (OrdersKeepTime == TimeSpan.Zero)
			return;

		var diff = _lastOrderTime - _firstOrderTime;

		if (diff <= _maxOrdersKeepTime)
			return;

		_firstOrderTime = _lastOrderTime - OrdersKeepTime;
		OrderProcessor.RemoveDoneBefore(_firstOrderTime);
	}

	/// <summary>
	/// Reset all state (orders, trades, positions, PnL, subscriptions).
	/// </summary>
	public void Reset()
	{
		Position[] positions = null;

		// When KeepStatistics is set, preserve the accumulated trading statistics (orders, trades, PnL,
		// positions, risk) across a restart - exactly as the monolith Strategy.Reset does.
		if (!KeepStatistics)
		{
			positions = _posManager.Positions;

			StatisticManager.Reset();
			PnLManager.Reset();
			RiskManager.Reset();
			OrderProcessor.Reset();
			Trades.Reset();
			_posManager.Reset();

			_positions.Clear();
			_position = 0;

			Latency = null;
		}

		Subscriptions.Reset();
		Engine.ForceStop();

		_ownTransactionIds.Clear();
		_pendingOwnOrders.Clear();
		_ordersAdjustedByTrade.Clear();
		_boardMsg = default;
		_portfolioLookup = default;
		_prevTradeDate = default;
		_isPrevDateTradable = default;
		_firstOrderTime = default;
		_lastOrderTime = default;
		_maxOrdersKeepTime = TimeSpan.FromTicks((long)(OrdersKeepTime.Ticks * 1.5));
		_isTradingBlocked = false;
		_stopping = false;
		_lastCantTradeReason = default;

		// Reset to Info (not the default Inherit) to match the monolith Strategy.Reset, so the ErrorState
		// change stream is identical at reset instead of emitting an extra Info -> Inherit transition.
		ErrorState = LogLevels.Info;

		var totalWorkingTime = TotalWorkingTime;
		TotalWorkingTime = default;

		if (totalWorkingTime == default)
			this.Notify(nameof(TotalWorkingTime));

		StartedTime = default;

		_protectiveController = default;
		_posController = default;
		_takeProfit = default;
		_stopLoss = default;
		_isStopTrailing = default;
		_takeTimeout = default;
		_stopTimeout = default;
		_protectiveUseMarketOrders = default;
		_isLocalStop = default;
		IsOnline = false;

		OnReseted();
		Reseted?.Invoke();

		if (!KeepStatistics)
		{
			var time = ((IStrategyHost)this).CurrentTime;

			RaisePnLChanged(time);
			RaiseCommissionChanged();
			RaiseLatencyChanged();
			RaiseSlippageChanged();

			foreach (var position in positions)
			{
				position.CurrentValue = 0;
				_positionChanged?.Invoke(position);
			}

			RaisePositionChanged(time);
		}
	}

	#region Protection

	/// <summary>
	/// Start position protection.
	/// </summary>
	/// <param name="takeProfit">Take offset.</param>
	/// <param name="stopLoss">Stop offset.</param>
	/// <param name="isStopTrailing">Whether to use a trailing technique.</param>
	/// <param name="takeTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="stopTimeout">Time limit. If protection has not worked by this time, the position will be closed on the market.</param>
	/// <param name="useMarketOrders">Whether to use market orders.</param>
	/// <param name="isLocalStop">Force local stop processing regardless of adapter capabilities.</param>
	public void StartProtection(
		Unit takeProfit, Unit stopLoss,
		bool isStopTrailing = default,
		TimeSpan? takeTimeout = default,
		TimeSpan? stopTimeout = default,
		bool useMarketOrders = default,
		bool isLocalStop = default)
	{
		if (!takeProfit.IsSet() && !stopLoss.IsSet())
			return;

		if (takeTimeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(takeTimeout), takeTimeout, LocalizedStrings.InvalidValue);

		if (stopTimeout < TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(stopTimeout), stopTimeout, LocalizedStrings.InvalidValue);

		_protectiveController = new() { Parent = this };
		_takeProfit = takeProfit;
		_stopLoss = stopLoss;
		_isStopTrailing = isStopTrailing;
		_takeTimeout = takeTimeout ?? default;
		_stopTimeout = stopTimeout ?? default;
		_protectiveUseMarketOrders = useMarketOrders;
		_isLocalStop = isLocalStop;
	}

	private void ActiveProtection((bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition) info)
	{
		var order = CreateOrder(info.side, info.price, info.volume);

		if (info.condition != null)
		{
			order.Type = OrderTypes.Conditional;
			order.Condition = info.condition;
		}

		RegisterOrder(order);
	}

	private void TryActivateProtection(SecurityId securityId, decimal price, DateTime time, bool requireStarted)
	{
		if (_protectiveController is null)
			return;

		if (requireStarted && ProcessState != ProcessStates.Started)
			return;

		foreach (var info in _protectiveController.TryActivate(securityId, price, time))
			ActiveProtection(info);
	}

	private IProtectiveBehaviourFactory GetProtectiveBehaviourFactory(Security security, Portfolio portfolio)
	{
		if (!_isLocalStop && (Connector as Connector)?.Adapter is { } basket && basket.TryGetAdapter(portfolio.Name, out var adapter)
			&& (adapter.IsSupportStopLoss() || adapter.IsSupportTakeProfit()))
		{
			return new ServerProtectiveBehaviourFactory(adapter);
		}

		return new LocalProtectiveBehaviourFactory(security.PriceStep, security.Decimals);
	}

	#endregion

	#region Virtual hooks

	/// <summary>
	/// Called when process state changes.
	/// </summary>
	/// <param name="state">New state.</param>
	protected virtual void OnStateChanged(ProcessStates state)
	{
		switch (state)
		{
			case ProcessStates.Started:
			{
				// StartedTime / TotalWorkingTime measure wall-clock operating time, so use base.CurrentTime as
				// the monolith does (host CurrentTime is market time, still default before replay begins).
				var startedTime = base.CurrentTime;
				var notifyStartedTime = StartedTime == startedTime;

				StartedTime = startedTime;

				if (notifyStartedTime)
					this.Notify(nameof(StartedTime));

				TotalWorkingTime = default;
				ErrorState = LogLevels.Info;
				this.Notify(nameof(IsFormed));

				// Seed start-of-run statistic values and the orders-keep window, matching the monolith
				// which calls InitStartValues at the head of OnStarted2 before subscribing the lookups.
				InitStartValues();

				// Subscribe to own transactions (orders/trades) and portfolio/position updates so the connector
				// routes them back through these registered subscriptions. Without this the order/position
				// notifications arrive on a subscription the strategy does not own and CanProcess filters them out.
				Subscriptions.Subscribe(PortfolioLookup, isGlobal: true);
				Subscriptions.Subscribe(OrderLookup, isGlobal: true);

				// Guard the user transition callback exactly as the monolith does: route any exception to
				// OnError and auto-stop on a failed Started transition, so a throwing override does not
				// escape the message loop and leave the strategy stuck in the Started state.
				try
				{
					OnStarted2(StartedTime);
				}
				catch (Exception error)
				{
					OnError(error);
					Stop(error);
				}

				break;
			}
			case ProcessStates.Stopping:
			{
				// The subscription unsubscription and IsOnline reset are performed by the always-run engine
				// teardown lambda wired in the constructor (so they survive a subclass overriding this method
				// without calling base); here only the overridable OnStopping() user hook is invoked, matching
				// the monolith which calls OnStopping() at this point in the Stopping transition.
				try
				{
					OnStopping();
				}
				catch (Exception error)
				{
					OnError(error);
				}

				break;
			}
			case ProcessStates.Stopped:
			{
				var totalWorkingTime = TotalWorkingTime;
				var startedTime = StartedTime;

				if (StartedTime != default)
					TotalWorkingTime += base.CurrentTime - StartedTime;

				if (TotalWorkingTime == totalWorkingTime)
					this.Notify(nameof(TotalWorkingTime));

				StartedTime = default;

				if (startedTime == default)
					this.Notify(nameof(StartedTime));

				try
				{
					OnStopped();
				}
				catch (Exception error)
				{
					OnError(error);
				}

				// Match the monolith TryFinalStop: dispose the strategy once it has reached Stopped
				// when DisposeOnStop is set (used by transient strategies such as quoting).
				if (DisposeOnStop)
					Dispose();

				break;
			}
		}
	}

	/// <summary>
	/// The method is called when the strategy has entered the started state.
	/// </summary>
	/// <param name="time">The strategy start time.</param>
	protected virtual void OnStarted2(DateTime time) { }

	/// <summary>
	/// The method is called when the <see cref="ProcessState"/> has taken the
	/// <see cref="ProcessStates.Stopping"/> value. The base unsubscription is performed by the engine
	/// before this call; override to release strategy-specific resources on stop.
	/// </summary>
	protected virtual void OnStopping() { }

	/// <summary>
	/// Seed the start-of-run values that the statistic parameters and the orders-keep window depend on.
	/// </summary>
	protected void InitStartValues()
	{
		var manager = StatisticManager;

		if (Portfolio?.CurrentValue is decimal beginValue)
		{
			foreach (var p in manager.Parameters.OfType<IBeginValueStatisticParameter>())
				p.BeginValue = beginValue;
		}

		foreach (var p in manager.Parameters.OfType<IRiskFreeRateStatisticParameter>())
			p.RiskFreeRate = RiskFreeRate;

		_maxOrdersKeepTime = TimeSpan.FromTicks((long)(OrdersKeepTime.Ticks * 1.5));
	}

	/// <summary>
	/// The method is called at the strategy reset.
	/// </summary>
	protected virtual void OnReseted() { }

	/// <summary>
	/// Get all securities required for the strategy.
	/// </summary>
	/// <returns>Securities.</returns>
	public virtual IEnumerable<(Security sec, DataType dt)> GetWorkingSecurities() => [];

	/// <summary>
	/// Called when current price updated from market data.
	/// </summary>
	protected virtual void OnCurrentPriceUpdated(SecurityId secId, decimal price, DateTime serverTime, DateTime localTime) { }

	/// <summary>
	/// Called when order registered (transitioned from Pending to Active/Done).
	/// </summary>
	/// <param name="order">Registered order.</param>
	protected virtual void OnOrderRegistered(Order order) { }

	/// <summary>
	/// Called just before an order is registered via the connector. Raises <see cref="OrderRegistering"/>.
	/// </summary>
	/// <param name="order">Order about to be registered.</param>
	protected virtual void OnOrderRegistering(Order order)
	{
		OrderRegistering?.Invoke(order);
	}

	/// <summary>
	/// Called just before an order is re-registered (cancel + register new) via the connector.
	/// Raises <see cref="OrderReRegistering"/>.
	/// </summary>
	/// <param name="oldOrder">Order being cancelled.</param>
	/// <param name="newOrder">New order to register.</param>
	protected virtual void OnOrderReRegistering(Order oldOrder, Order newOrder)
	{
		OrderReRegistering?.Invoke(oldOrder, newOrder);
	}

	/// <summary>
	/// Called when order state changed. Driven by the order-tracking pipeline on every non-registration
	/// change of a tracked order (the obsolete <see cref="OrderChanged"/> counterpart). Distinct from
	/// <see cref="OnOrderReceived(Order)"/>, which fires for any update of one of the strategy's own
	/// orders arriving from the connector's order-lookup subscription.
	/// </summary>
	/// <param name="order">Changed order.</param>
	protected virtual void OnOrderChanged(Order order) { }

	/// <summary>
	/// Called when one of the strategy's own orders is updated via the connector's order-lookup
	/// subscription, matching the monolith user hook. See <see cref="OnOrderChanged(Order)"/> for the
	/// difference between the two.
	/// </summary>
	/// <param name="order">Received order.</param>
	protected virtual void OnOrderReceived(Order order) { }

	/// <summary>
	/// Called when one of the strategy's own trades is received. This is the user-overridable hook, named to
	/// match the monolith <see cref="StrategyOld.OnOwnTradeReceived(MyTrade)"/> so subclass overrides bind. It
	/// is the single real hook: the obsolete <see cref="NewMyTrade"/> event is raised alongside it for the same
	/// trade (both wired from the trade pipeline's add), so user code is never invoked twice for one trade.
	/// </summary>
	/// <param name="trade">Received trade.</param>
	protected virtual void OnOwnTradeReceived(MyTrade trade) { }

	/// <summary>
	/// Called when new position appears.
	/// </summary>
	/// <param name="position">New position.</param>
	protected virtual void OnNewPosition(Position position) { }

	/// <summary>
	/// Called when position changes.
	/// </summary>
	/// <param name="position">Changed position.</param>
	protected virtual void OnPositionChanged(Position position) { }

	/// <summary>
	/// Called when order registration fails.
	/// </summary>
	/// <param name="fail">Order failure info.</param>
	protected virtual void OnOrderRegisterFailed(OrderFail fail)
	{
#pragma warning disable CS0618 // the OrderRegisterFailed event is obsolete but still part of the strategy surface.
		OrderRegisterFailed?.Invoke(fail);
#pragma warning restore CS0618
		StatisticManager.AddRegisterFailedOrder(fail);
	}

	/// <summary>
	/// Processing of error, occurred as result of strategy operation.
	/// </summary>
	/// <param name="error">Error.</param>
	protected virtual void OnError(Exception error)
	{
		LastError = error;

		// Feed the error into the risk manager (so error-count rules see exceptions) and log it,
		// matching the monolith OnError which calls ProcessRisk(error.ToErrorMessage()) and LogError.
		ProcessRisk(error.ToErrorMessage());

		Error?.Invoke(this, error);

		LogError(error.ToString());
	}

	/// <summary>
	/// Whether the order can be attached (tracked) by this strategy.
	/// </summary>
	/// <param name="order">Order to check.</param>
	/// <returns><see langword="true"/> if the order belongs to this strategy.</returns>
	protected virtual bool CanAttach(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (_pendingOwnOrders.Contains(order))
			return true;

		if (order.UserOrderId.EqualsIgnoreCase(EnsureGetId()))
			return true;

		return _ownTransactionIds.Contains(order.TransactionId);
	}

	private bool CanAttachUnclaimedPendingOrder(Order order)
	{
		if (order.State != OrderStates.Pending || !order.UserOrderId.IsEmpty())
			return false;

		if (Security is not null && order.Security is not null && order.Security.ToSecurityId() != Security.ToSecurityId())
			return false;

		if (Portfolio is not null && order.Portfolio is not null && !order.Portfolio.Name.EqualsIgnoreCase(Portfolio.Name))
			return false;

		return true;
	}

	private string EnsureGetId() => _idStr ??= Id.To<string>();

	/// <summary>
	/// Check if can trade with order information.
	/// </summary>
	/// <param name="security">Security to trade.</param>
	/// <param name="portfolio">Portfolio to trade.</param>
	/// <param name="side">Order side.</param>
	/// <param name="volume">Order volume.</param>
	/// <param name="noTradeReason">Reason why trading is not allowed.</param>
	/// <returns>True if trading is allowed.</returns>
	protected virtual bool CanTrade(Security security, Portfolio portfolio, Sides side, decimal volume, out string noTradeReason)
	{
		bool canTrade(out string noTradeReason)
		{
			// Reject new orders the moment a stop is requested, before the Stopping state has round-tripped
			// through the engine - matching the monolith which checks _stopping ahead of ProcessState.
			if (_stopping)
			{
				noTradeReason = "Strategy is stopping.";
				return false;
			}

			if (ProcessState != ProcessStates.Started)
			{
				noTradeReason = LocalizedStrings.StrategyInStateCannotRegisterOrder.Put(ProcessState);
				return false;
			}

			if (!IsFormed)
			{
				noTradeReason = LocalizedStrings.NonFormed;
				return false;
			}

			var mode = TradingMode;

			if (mode == StrategyTradingModes.Disabled)
			{
				noTradeReason = LocalizedStrings.TradingDisabled;
				return false;
			}

			var currentPosition = GetPositionValue(security, portfolio) ?? 0;

			if (mode == StrategyTradingModes.ReducePositionOnly)
			{
				if (volume > 0)
				{
					var noReducePosition = currentPosition == 0 ||
						currentPosition.GetDirection() == side ||
						currentPosition.Abs() < volume;

					if (noReducePosition)
					{
						noTradeReason = LocalizedStrings.PosConditionReduceOnly;
						return false;
					}
				}
			}
			else if (mode == StrategyTradingModes.LongOnly)
			{
				if (side == Sides.Sell && volume > 0)
				{
					if (currentPosition <= 0)
					{
						noTradeReason = LocalizedStrings.LongOnly;
						return false;
					}

					if (volume > currentPosition)
					{
						noTradeReason = $"{LocalizedStrings.LongOnly}: sell volume ({volume}) exceeds position ({currentPosition})";
						return false;
					}
				}
			}

			if (_isTradingBlocked)
			{
				noTradeReason = LocalizedStrings.TradingDisabled;
				return false;
			}

			noTradeReason = null;
			return true;
		}

		if (!canTrade(out noTradeReason))
		{
			var logLevel = noTradeReason == _lastCantTradeReason ? LogLevels.Verbose : LogLevels.Warning;

			_lastCantTradeReason = noTradeReason;
			this.AddLog(logLevel, () => $"can't send orders: {_lastCantTradeReason}");

			if (logLevel == LogLevels.Warning && ErrorState == LogLevels.Info)
				ErrorState = LogLevels.Warning;

			return false;
		}

		_lastCantTradeReason = null;
		noTradeReason = null;
		return true;
	}

	private void ProcessOrderFail(Order order, Exception error)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (error is null)
			throw new ArgumentNullException(nameof(error));

		order.ApplyNewState(OrderStates.Failed, this);

		var fail = new OrderFail
		{
			Order = order,
			Error = error,
			ServerTime = ((IStrategyHost)this).CurrentTime,
			TransactionId = order.TransactionId,
		};

		OnOrderRegisterFailed(fail);

		foreach (var subscription in Subscriptions.Subscriptions.Where(s => s.DataType == DataType.Transactions))
			OrderRegisterFailReceived?.Invoke(subscription, fail);
	}

	private void PrepareNewOrder(Order order)
	{
		order.Security ??= Security;
		order.Portfolio ??= Portfolio;

		if (order.Comment.IsEmpty())
		{
			switch (CommentMode)
			{
				case StrategyCommentModes.Disabled:
					break;
				case StrategyCommentModes.Id:
					order.Comment = EnsureGetId();
					break;
				case StrategyCommentModes.Name:
					order.Comment = Name;
					break;
				default:
					throw new ArgumentOutOfRangeException(CommentMode.To<string>());
			}
		}

		if (order.UserOrderId.IsEmpty())
			order.UserOrderId = EnsureGetId();

		order.StrategyId = EnsureGetId();
	}

	private static void EnsureActiveOrderBalance(Order order)
	{
		if (order.State == OrderStates.Active && order.Balance == 0 && order.Volume > 0 && (order.GetMatchedVolume() ?? 0) == order.Volume)
			order.Balance = order.Volume;
	}

	private Order TryGetTrackedOrder(Order order)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		return OrderProcessor.TryGetTracked(order);
	}

	private static void CopyOrderState(Order source, Order destination)
	{
		if (ReferenceEquals(source, destination))
			return;

		destination.Id = source.Id;
		destination.StringId = source.StringId;
		destination.BoardId = source.BoardId;
		destination.Time = source.Time;
		destination.ServerTime = source.ServerTime;
		destination.LocalTime = source.LocalTime;
		destination.CancelledTime = source.CancelledTime;
		destination.MatchedTime = source.MatchedTime;
		destination.State = source.State;
		destination.Security = source.Security ?? destination.Security;
		destination.Portfolio = source.Portfolio ?? destination.Portfolio;
		destination.Price = source.Price;
		destination.Volume = source.Volume;
		destination.VisibleVolume = source.VisibleVolume;
		destination.Side = source.Side;
		destination.Balance = source.Balance;
		destination.Status = source.Status;
		destination.IsSystem = source.IsSystem;
		destination.Comment = source.Comment;
		destination.Type = source.Type;
		destination.ExpiryDate = source.ExpiryDate;
		destination.Condition = source.Condition;
		destination.TimeInForce = source.TimeInForce;
		destination.Commission = source.Commission;
		destination.CommissionCurrency = source.CommissionCurrency;
		destination.UserOrderId = source.UserOrderId;
		destination.StrategyId = source.StrategyId;
		destination.BrokerCode = source.BrokerCode;
		destination.ClientCode = source.ClientCode;
		destination.Currency = source.Currency;
		destination.IsMarketMaker = source.IsMarketMaker;
		destination.MarginMode = source.MarginMode;
		destination.Slippage = source.Slippage;
		destination.IsManual = source.IsManual;
		destination.AveragePrice = source.AveragePrice;
		destination.MarketPrice = source.MarketPrice;
		destination.Yield = source.Yield;
		destination.MinVolume = source.MinVolume;
		destination.PositionEffect = source.PositionEffect;
		destination.PostOnly = source.PostOnly;
		destination.SeqNum = source.SeqNum;
		destination.Leverage = source.Leverage;
		destination.LatencyRegistration = source.LatencyRegistration;
		destination.LatencyCancellation = source.LatencyCancellation;
		destination.LatencyEdition = source.LatencyEdition;
	}

	private void ApplyTradeToOrder(MyTrade trade)
	{
		var order = trade.Order;
		var volume = trade.Trade.Volume;

		if (volume <= 0 || order.Volume <= 0)
			return;

		EnsureActiveOrderBalance(order);

		order.Balance = (order.Balance - volume).Max(0);

		if (order.Balance == 0)
			order.State = OrderStates.Done;
		else if (order.State is OrderStates.None or OrderStates.Pending)
			order.State = OrderStates.Active;

		OrderProcessor.ProcessOrder(order, isChanging: true);

		var res = _posManager.ProcessOrder(order);

		if (res != StrategyPositionManager.OrderResults.OK && ErrorState == LogLevels.Info)
			ErrorState = LogLevels.Warning;
	}

	private bool ShouldApplyTradeToOrder(Order order)
	{
		var txId = order.TransactionId;

		if (txId <= 0)
			return false;

		if (_ordersAdjustedByTrade.Contains(txId))
			return true;

		if ((order.GetMatchedVolume() ?? 0) > 0)
			return false;

		_ordersAdjustedByTrade.Add(txId);
		return true;
	}

	private void SubmitNewOrder(Order order, Action submit)
	{
		if (submit is null)
			throw new ArgumentNullException(nameof(submit));

		_pendingOwnOrders.Add(order);

		try
		{
			submit();
		}
		finally
		{
			_pendingOwnOrders.Remove(order);

			if (order.TransactionId != 0)
				_ownTransactionIds.Add(order.TransactionId);
		}
	}

	#endregion

	#region IPositionProvider

	/// <summary>
	/// Portfolio lookup subscription.
	/// </summary>
	public Subscription PortfolioLookup => _portfolioLookup ??= new(new PortfolioLookupMessage
	{
		StrategyId = EnsureGetId(),
	});

	IEnumerable<Position> IPositionProvider.Positions => _posManager.Positions;

	event Action<Position> IPositionProvider.NewPosition
	{
		add => _newPosition += value;
		remove => _newPosition -= value;
	}

	event Action<Position> IPositionProvider.PositionChanged
	{
		add => _positionChanged += value;
		remove => _positionChanged -= value;
	}

	Position IPositionProvider.GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limitType)
		=> _posManager.TryGetPosition(security, portfolio);

	#endregion

	#region IStrategyHost

	DateTime IStrategyHost.CurrentTime
		=> _connector is ITimeProvider tp ? tp.CurrentTime : DateTime.UtcNow;

	string IStrategyHost.StrategyId => EnsureGetId();

	bool IStrategyHost.HasPositions => _posManager.Positions.Length > 0;

	bool IStrategyHost.CanRefreshPnL(DateTime time)
	{
		_boardMsg ??= Security?.Board?.ToMessage() ?? Portfolio?.Board?.ToMessage();

		if (_boardMsg is null)
			return true;

		var date = time.Date;

		if (date != _prevTradeDate)
		{
			_prevTradeDate = date;
			_isPrevDateTradable = _boardMsg.IsWorkingDate(_prevTradeDate);
		}

		if (!_isPrevDateTradable)
			return false;

		var period = _boardMsg.WorkingTime.GetPeriod(date);

		return period == null || period.Times.IsEmpty() || period.Times.Any(r => r.Contains(time.TimeOfDay));
	}

	ValueTask IStrategyHost.SendOutMessageAsync(Message message, CancellationToken cancellationToken)
		=> _connector?.SendOutMessageAsync(message, cancellationToken) ?? default;

	long IStrategyHost.GetNextTransactionId()
		=> _connector?.TransactionIdGenerator?.GetNextId() ?? 0;

	#endregion

	#region Connector wiring

	private RiskActions? ProcessRisk(Message msg)
	{
		var triggeredCnt = 0;

		foreach (var rule in RiskManager.ProcessRules(msg))
		{
			triggeredCnt++;

			LogWarning(LocalizedStrings.ActivatingRiskRule,
				rule.GetType().Name, rule.Title, rule.Action);

			// Stop at the first triggered rule: perform its action and return it so the
			// caller can veto the in-flight order. Further rules are not evaluated.
			switch (rule.Action)
			{
				case RiskActions.ClosePositions:
					ClosePosition();
					return rule.Action;
				case RiskActions.StopTrading:
					_isTradingBlocked = true;
					LogInfo(LocalizedStrings.TradingDisabled);
					return rule.Action;
				case RiskActions.CancelOrders:
					CancelAllActiveOrders();
					return rule.Action;
				default:
					throw new InvalidOperationException(rule.Action.ToString());
			}
		}

		// No rule triggered on this pass: if trading was previously blocked, unblock it.
		if (_isTradingBlocked && triggeredCnt == 0)
		{
			_isTradingBlocked = false;
			LogInfo("Trading unblocked - risk limits no longer exceeded.");
		}

		return null;
	}

	private void SubscribeConnector()
	{
		_connector.OrderReceived += OnConnectorOrderReceived;
		_connector.OwnTradeReceived += OnTradeReceived;
		_connector.PositionReceived += OnPositionReceived;
#pragma warning disable CS0618 // obsolete transaction events are still part of the strategy surface.
		_connector.OrderRegisterFailed += OnConnectorOrderRegisterFailed;
		_connector.OrderCancelFailed += OnConnectorOrderCancelFailed;
		_connector.OrderEdited += OnConnectorOrderEdited;
		_connector.OrderEditFailed += OnConnectorOrderEditFailed;
#pragma warning restore CS0618
		_connector.OrderRegisterFailReceived += OnOrderRegisterFailReceived;
		_connector.OrderCancelFailReceived += OnOrderCancelFailReceived;
		_connector.OrderEditFailReceived += OnOrderEditFailReceived;
		_connector.NewOutMessageAsync += OnNewMessage;
		_connector.CurrentTimeChanged += OnTimeChanged;
		_connector.SubscriptionStarted += OnSubscriptionStarted;
		_connector.SubscriptionOnline += OnSubscriptionOnline;
		_connector.SubscriptionStopped += OnSubscriptionStopped;
		_connector.SubscriptionFailed += OnSubscriptionFailed;
		_connector.SubscriptionReceived += OnSubscriptionReceived;
		_connector.Level1Received += OnLevel1Received;
		_connector.OrderBookReceived += OnOrderBookReceived;
		_connector.TickTradeReceived += OnTickTradeReceived;
		_connector.OrderLogReceived += OnOrderLogReceived;
		_connector.SecurityReceived += OnSecurityReceived;
		_connector.BoardReceived += OnBoardReceived;
		_connector.NewsReceived += OnNewsReceived;
		_connector.CandleReceived += OnCandleReceived;
		_connector.PortfolioReceived += OnPortfolioReceived;
		_connector.DataTypeReceived += OnDataTypeReceived;
	}

	private void UnsubscribeConnector()
	{
		_connector.OrderReceived -= OnConnectorOrderReceived;
		_connector.OwnTradeReceived -= OnTradeReceived;
		_connector.PositionReceived -= OnPositionReceived;
#pragma warning disable CS0618 // obsolete transaction events are still part of the strategy surface.
		_connector.OrderRegisterFailed -= OnConnectorOrderRegisterFailed;
		_connector.OrderCancelFailed -= OnConnectorOrderCancelFailed;
		_connector.OrderEdited -= OnConnectorOrderEdited;
		_connector.OrderEditFailed -= OnConnectorOrderEditFailed;
#pragma warning restore CS0618
		_connector.OrderRegisterFailReceived -= OnOrderRegisterFailReceived;
		_connector.OrderCancelFailReceived -= OnOrderCancelFailReceived;
		_connector.OrderEditFailReceived -= OnOrderEditFailReceived;
		_connector.NewOutMessageAsync -= OnNewMessage;
		_connector.CurrentTimeChanged -= OnTimeChanged;
		_connector.SubscriptionStarted -= OnSubscriptionStarted;
		_connector.SubscriptionOnline -= OnSubscriptionOnline;
		_connector.SubscriptionStopped -= OnSubscriptionStopped;
		_connector.SubscriptionFailed -= OnSubscriptionFailed;
		_connector.SubscriptionReceived -= OnSubscriptionReceived;
		_connector.Level1Received -= OnLevel1Received;
		_connector.OrderBookReceived -= OnOrderBookReceived;
		_connector.TickTradeReceived -= OnTickTradeReceived;
		_connector.OrderLogReceived -= OnOrderLogReceived;
		_connector.SecurityReceived -= OnSecurityReceived;
		_connector.BoardReceived -= OnBoardReceived;
		_connector.NewsReceived -= OnNewsReceived;
		_connector.CandleReceived -= OnCandleReceived;
		_connector.PortfolioReceived -= OnPortfolioReceived;
		_connector.DataTypeReceived -= OnDataTypeReceived;
	}

	private void OnConnectorOrderRegisterFailed(OrderFail fail)
	{
		if (fail?.Order is not Order order)
			return;

		if (order.TransactionId == 0)
			order.TransactionId = fail.TransactionId;

		if (!OrderProcessor.IsTracked(order))
		{
			if (!CanAttach(order))
				return;

			OrderProcessor.TryAttach(order);
		}

		if (order.Time == default)
			order.Time = fail.ServerTime;

		if (order.ServerTime == default)
			order.ServerTime = fail.ServerTime;

		order.ApplyNewState(OrderStates.Failed, this);
		OnOrderRegisterFailed(fail);
	}

	private void OnConnectorOrderCancelFailed(OrderFail fail)
	{
		if (fail?.Order is not Order order)
			return;

		if (order.TransactionId == 0)
			order.TransactionId = fail.TransactionId;

		if (!OrderProcessor.IsTracked(order) && !CanAttach(order))
			return;

		ErrorState = LogLevels.Error;
#pragma warning disable CS0618 // the OrderCancelFailed event is obsolete but still part of the strategy surface.
		OrderCancelFailed?.Invoke(fail);
#pragma warning restore CS0618
		StatisticManager.AddFailedOrderCancel(fail);
	}

	private void OnConnectorOrderEdited(long transactionId, Order order)
	{
		if (OrderProcessor.IsTracked(order))
		{
#pragma warning disable CS0618 // the OrderEdited event is obsolete but still part of the strategy surface.
			OrderEdited?.Invoke(transactionId, order);
#pragma warning restore CS0618

			// Apply edition latency, matching the monolith OnConnectorOrderEdited which calls
			// ChangeLatency(order.LatencyEdition) for a tracked order.
			ChangeLatency(order.LatencyEdition);
		}
	}

	private void OnConnectorOrderEditFailed(long transactionId, OrderFail fail)
	{
		if (fail?.Order is Order order && OrderProcessor.IsTracked(order))
		{
#pragma warning disable CS0618 // the OrderEditFailed event is obsolete but still part of the strategy surface.
			OrderEditFailed?.Invoke(transactionId, fail);
#pragma warning restore CS0618
		}
	}

	/// <summary>
	/// Handle order received from the connector. This is the pipeline callback wired to
	/// <see cref="ISubscriptionProvider.OrderReceived"/>; the user-overridable hook is <see cref="OnOrderReceived(Order)"/>.
	/// </summary>
	public void OnConnectorOrderReceived(Subscription sub, Order order)
	{
		if (!Subscriptions.CanProcess(sub))
			return;

		var trackedOrder = TryGetTrackedOrder(order);
		var isOwnOrder = trackedOrder != null || CanAttach(order) || CanAttachUnclaimedPendingOrder(order);

		if (isOwnOrder)
		{
			trackedOrder ??= order;
			CopyOrderState(order, trackedOrder);
			EnsureActiveOrderBalance(trackedOrder);
			OrderProcessor.TryAttach(trackedOrder);
			OrderProcessor.ProcessOrder(trackedOrder, isChanging: true);
		}

		OrderReceived?.Invoke(sub, order);

		if (order.Volume > 0)
		{
			var positionOrder = trackedOrder ?? order;

			EnsureActiveOrderBalance(positionOrder);
			var res = _posManager.ProcessOrder(positionOrder);

			if (res != StrategyPositionManager.OrderResults.OK && ErrorState == LogLevels.Info)
				ErrorState = LogLevels.Warning;
		}

		// Invoke the user hook when one of the strategy's own orders updates, matching the monolith
		// which calls OnOrderReceived(order) only for the OrderLookup subscription.
		if (sub == OrderLookup)
			OnOrderReceived(order);
	}

	/// <summary>
	/// Handle trade received from connector.
	/// </summary>
	public void OnTradeReceived(Subscription sub, MyTrade trade)
	{
		if (!Subscriptions.CanProcess(sub))
			return;

		var trackedOrder = TryGetTrackedOrder(trade.Order);

		if (trackedOrder is null)
			return;

		trade.Order = trackedOrder;

		var sec = trade.Order.Security;

		if (sec != null)
		{
			// Mirror the monolith's per-trade security feed: stamp with strategy CurrentTime and resolve
			// StepPrice/Multiplier provider-first (falling back to the security's own Multiplier).
			PnLManager.UpdateSecurity(new Level1ChangeMessage
			{
				SecurityId = sec.ToSecurityId(),
				ServerTime = CurrentTime,
			}
			.TryAdd(Level1Fields.PriceStep, sec.PriceStep)
			.TryAdd(Level1Fields.StepPrice, this.GetSecurityValue<decimal?>(sec, Level1Fields.StepPrice))
			.TryAdd(Level1Fields.Multiplier, this.GetSecurityValue<decimal?>(sec, Level1Fields.Multiplier) ?? sec.Multiplier));
		}

		if (Trades.Contains(trade))
			return;

		if (ShouldApplyTradeToOrder(trade.Order))
			ApplyTradeToOrder(trade);

		if (!Trades.TryAdd(trade))
			return;

		OwnTradeReceived?.Invoke(sub, trade);

		ProcessRisk(trade.ToMessage());
	}

	/// <summary>
	/// Handle position received from connector.
	/// </summary>
	public void OnPositionReceived(Subscription sub, Position pos)
	{
		if (!Subscriptions.CanProcess(sub))
			return;

		Positions.Process(pos);

		ProcessRisk(pos.ToChangeMessage());
	}

	private void OnSubscriptionStarted(Subscription subscription)
	{
		if (!Subscriptions.CanProcess(subscription))
			return;

		SubscriptionStarted?.Invoke(subscription);
		RefreshOnlineState();
	}

	private void OnSubscriptionOnline(Subscription subscription)
	{
		if (!Subscriptions.CanProcess(subscription))
			return;

		SubscriptionOnline?.Invoke(subscription);
		RefreshOnlineState();
	}

	private void OnSubscriptionStopped(Subscription subscription, Exception error)
	{
		if (!Subscriptions.CanProcess(subscription))
			return;

		SubscriptionStopped?.Invoke(subscription, error);
		RefreshOnlineState();
	}

	private void OnSubscriptionFailed(Subscription subscription, Exception error, bool isSubscribe)
	{
		if (!Subscriptions.CanProcess(subscription))
			return;

		SubscriptionFailed?.Invoke(subscription, error, isSubscribe);
		RefreshOnlineState();
	}

	private void OnSubscriptionReceived(Subscription subscription, object value)
	{
		if (value is Order { State: OrderStates.Done, Balance: 0, Volume: <= 0 } && subscription.DataType == DataType.Transactions)
			return;

		if (Subscriptions.CanProcess(subscription))
			SubscriptionReceived?.Invoke(subscription, value);
	}

	private void OnLevel1Received(Subscription subscription, Level1ChangeMessage value)
	{
		if (Subscriptions.CanProcess(subscription))
			Level1Received?.Invoke(subscription, value);
	}

	private void OnOrderBookReceived(Subscription subscription, IOrderBookMessage value)
	{
		if (Subscriptions.CanProcess(subscription))
			OrderBookReceived?.Invoke(subscription, value);
	}

	private void OnTickTradeReceived(Subscription subscription, ITickTradeMessage value)
	{
		if (Subscriptions.CanProcess(subscription))
			TickTradeReceived?.Invoke(subscription, value);
	}

	private void OnOrderLogReceived(Subscription subscription, IOrderLogMessage value)
	{
		if (Subscriptions.CanProcess(subscription))
			OrderLogReceived?.Invoke(subscription, value);
	}

	private void OnSecurityReceived(Subscription subscription, Security value)
	{
		if (Subscriptions.CanProcess(subscription))
			SecurityReceived?.Invoke(subscription, value);
	}

	private void OnBoardReceived(Subscription subscription, ExchangeBoard value)
	{
		if (Subscriptions.CanProcess(subscription))
			BoardReceived?.Invoke(subscription, value);
	}

	private void OnNewsReceived(Subscription subscription, News value)
	{
		if (Subscriptions.CanProcess(subscription))
			NewsReceived?.Invoke(subscription, value);
	}

	private void OnCandleReceived(Subscription subscription, ICandleMessage value)
	{
		if (Subscriptions.CanProcess(subscription))
		{
			TryActivateProtection(value.SecurityId, value.ClosePrice, ((IStrategyHost)this).CurrentTime, requireStarted: true);
			CandleReceived?.Invoke(subscription, value);
		}
	}

	private void OnPortfolioReceived(Subscription subscription, Portfolio value)
	{
		if (Subscriptions.CanProcess(subscription))
			PortfolioReceived?.Invoke(subscription, value);
	}

	private void OnDataTypeReceived(Subscription subscription, DataType value)
	{
		if (Subscriptions.CanProcess(subscription))
			DataTypeReceived?.Invoke(subscription, value);
	}

	private void RefreshOnlineState()
	{
		var isOnline = ProcessState == ProcessStates.Started &&
			Subscriptions.Subscriptions
				.Where(subscription => !subscription.SubscriptionMessage.IsHistoryOnly())
				.All(subscription => subscription.State == SubscriptionStates.Online);

		IsOnline = isOnline;
	}

	/// <summary>
	/// Handle order registration failure from connector.
	/// </summary>
	public void OnOrderRegisterFailReceived(Subscription sub, OrderFail fail)
	{
		if (Subscriptions.CanProcess(sub))
			OrderRegisterFailReceived?.Invoke(sub, fail);
	}

	/// <summary>
	/// Handle order cancellation failure from connector.
	/// </summary>
	public void OnOrderCancelFailReceived(Subscription sub, OrderFail fail)
	{
		if (Subscriptions.CanProcess(sub))
			OrderCancelFailReceived?.Invoke(sub, fail);
	}

	/// <summary>
	/// Handle order edit failure from connector.
	/// </summary>
	public void OnOrderEditFailReceived(Subscription sub, OrderFail fail)
	{
		if (Subscriptions.CanProcess(sub))
			OrderEditFailReceived?.Invoke(sub, fail);
	}

	/// <summary>
	/// Handle new outgoing message from connector.
	/// </summary>
	public ValueTask OnNewMessage(Message msg, CancellationToken ct)
	{
		_isProcessingConnectorMessage = true;

		try
		{
			Engine.OnMessage(msg);
		}
		finally
		{
			_isProcessingConnectorMessage = false;
		}

		return default;
	}

	private void OnTimeChanged(TimeSpan diff)
	{
		_currentTimeChanged?.Invoke(diff);
	}

	/// <inheritdoc />
	public override DateTime CurrentTime => ((IStrategyHost)this).CurrentTime;

	DateTime ITimeProvider.CurrentTime => ((IStrategyHost)this).CurrentTime;

	event Action<TimeSpan> ITimeProvider.CurrentTimeChanged
	{
		add => _currentTimeChanged += value;
		remove => _currentTimeChanged -= value;
	}

	void INotifyPropertyChangedEx.NotifyPropertyChanged(string info)
		=> PropertyChanged?.Invoke(this, info);

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		// Mirror the monolith DisposeManaged: detach the connector (which unsubscribes its event
		// handlers), dispose the parameters, unhook the position manager and dispose the statistics.
		Connector = null;

		Parameters.Dispose();

		_posManager.PositionProcessed -= ProcessStrategyPosition;

		StatisticManager.Dispose();

		base.DisposeManaged();
	}

	#endregion
}
