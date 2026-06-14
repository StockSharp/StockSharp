namespace StockSharp.Algo.Strategies.Decomposed;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Statistics;
using StockSharp.Algo.Strategies.Protective;

/// <summary>
/// The base class for all trade strategies.
/// </summary>
public class DecomposedStrategy : BaseLogReceiver, IStrategyHost, IPositionProvider, INotifyPropertyChangedEx, ITimeProvider
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
	private bool _isOnline;
	private decimal _position;
	private LogLevels _errorState;
	private string _lastCantTradeReason;
	private DateTime _startedTime;
	private TimeSpan _totalWorkingTime;
	private Action<TimeSpan> _currentTimeChanged;
	private bool _isProcessingConnectorMessage;
	private Security _security;
	private Portfolio _portfolio;
	private decimal _volume = 1;

	private Unit _takeProfit, _stopLoss;
	private bool _isStopTrailing;
	private TimeSpan _takeTimeout, _stopTimeout;
	private bool _protectiveUseMarketOrders;
	private bool _isLocalStop;
	private ProtectiveController _protectiveController;
	private IProtectivePositionController _posController;

	/// <summary>
	/// Initializes a new instance <see cref="DecomposedStrategy"/>.
	/// </summary>
	public DecomposedStrategy()
		: this(new PnLManager { UseOrderBook = true }, new StatisticManager())
	{
	}

	/// <summary>
	/// Initializes a new instance <see cref="DecomposedStrategy"/>.
	/// </summary>
	/// <param name="pnlManager">PnL manager.</param>
	/// <param name="stats">Statistic manager.</param>
	public DecomposedStrategy(IPnLManager pnlManager, IStatisticManager stats)
	{
		PnLManager = pnlManager ?? throw new ArgumentNullException(nameof(pnlManager));
		StatisticManager = stats ?? throw new ArgumentNullException(nameof(stats));

		_posManager = new(EnsureGetId);
		_posManager.PositionProcessed += ProcessStrategyPosition;

		Engine = new(this, PnLManager);
		Orders = new(StatisticManager);
		Trades = new(PnLManager, StatisticManager);
		Positions = new(StatisticManager);
		Subscriptions = new(this);
		RiskManager = new RiskManager();

		Subscriptions.SubscriptionRequested += s => _connector?.Subscribe(s);
		Subscriptions.UnsubscriptionRequested += s => _connector?.UnSubscribe(s);

		Engine.StateChanged += OnStateChanged;
		Engine.StateChanged += _ => this.Notify(nameof(ProcessState));
		Engine.StateChanged += state =>
		{
			if (state == ProcessStates.Stopping)
			{
				if (CancelOrdersWhenStopping)
					CancelActiveOrders();

				if (UnsubscribeOnStop)
					Subscriptions.UnSubscribeAll(globalAndLocal: false);
			}
		};
		Engine.StateChanged += _ => RefreshOnlineState();
		Engine.CurrentPriceUpdated += (secId, price, serverTime, localTime) =>
		{
			_posManager.UpdateCurrentPrice(secId, price, serverTime, localTime);
			OnCurrentPriceUpdated(secId, price, serverTime, localTime);

			if (!_isProcessingConnectorMessage)
				TryActivateProtection(secId, price, serverTime, requireStarted: false);
		};
		Orders.Registered += OnOrderRegistered;
		Orders.Registered += order =>
		{
			if (order.Commission is not null)
				RaiseCommissionChanged();

			if (order.LatencyRegistration is TimeSpan lat && lat != TimeSpan.Zero)
			{
				Latency = (Latency ?? TimeSpan.Zero) + lat;
				RaiseLatencyChanged();
			}
		};
		Orders.Registered += TrackOrderLifetime;
		Orders.Changed += HandleOrderChanged;
		Trades.TradeAdded += OnNewMyTrade;
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
	public StrategyEngine Engine { get; }

	/// <summary>
	/// Order tracking and processing.
	/// </summary>
	public OrderPipeline Orders { get; }

	/// <summary>
	/// Trade processing and PnL.
	/// </summary>
	public TradePipeline Trades { get; }

	/// <summary>
	/// Position event handling.
	/// </summary>
	public PositionPipeline Positions { get; }

	/// <summary>
	/// Subscription management.
	/// </summary>
	public SubscriptionRegistry Subscriptions { get; }

	/// <summary>
	/// PnL manager.
	/// </summary>
	public IPnLManager PnLManager { get; }

	/// <summary>
	/// Statistics manager.
	/// </summary>
	public IStatisticManager StatisticManager { get; }

	/// <summary>
	/// Risk manager.
	/// </summary>
	public IRiskManager RiskManager { get; set; }

	/// <summary>
	/// Connector (via interface for testability).
	/// </summary>
	public IConnector Connector
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

	private void RaiseParametersChanged(string name)
	{
		ParametersChanged?.Invoke();
		this.Notify(name);
	}

	/// <summary>
	/// Security.
	/// </summary>
	public Security Security
	{
		get => _security;
		set
		{
			if (_security == value)
				return;

			_security = value;
			RaiseParametersChanged(nameof(Security));
		}
	}

	/// <summary>
	/// Portfolio.
	/// </summary>
	public Portfolio Portfolio
	{
		get => _portfolio;
		set
		{
			if (_portfolio == value)
				return;

			_portfolio = value;
			RaiseParametersChanged(nameof(Portfolio));
		}
	}

	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal Volume
	{
		get => _volume;
		set
		{
			if (_volume == value)
				return;

			_volume = value;
			RaiseParametersChanged(nameof(Volume));
		}
	}

	/// <summary>
	/// Current position (primary security).
	/// </summary>
	public decimal Position
	{
		get => Security == null || Portfolio == null ? _position : GetPositionValue(Security, Portfolio);
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
	public decimal GetPositionValue(Security sec, Portfolio pf)
	{
		return _posManager.TryGetPosition(sec, pf)?.CurrentValue ?? 0;
	}

	/// <summary>
	/// All tracked positions.
	/// </summary>
	public IReadOnlyDictionary<(SecurityId secId, string pfName), decimal> PositionsList => _positions;

	/// <summary>
	/// Comment mode.
	/// </summary>
	public StrategyCommentModes CommentMode { get; set; }

	/// <summary>
	/// Trading mode.
	/// </summary>
	public StrategyTradingModes TradingMode { get; set; }

	/// <summary>
	/// Latency.
	/// </summary>
	public TimeSpan? Latency { get; set; }

	/// <summary>
	/// Error state.
	/// </summary>
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

	/// <summary>
	/// Strategy start time.
	/// </summary>
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
	public virtual bool IsFormed => true;

	/// <summary>
	/// Wait for all trades.
	/// </summary>
	public bool WaitAllTrades { get; set; }

	/// <summary>
	/// The time for storing orders in memory.
	/// </summary>
	public TimeSpan OrdersKeepTime { get; set; } = TimeSpan.FromDays(1);

	/// <summary>
	/// Is online.
	/// </summary>
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
	/// Cancel active orders when strategy is stopping.
	/// </summary>
	public bool CancelOrdersWhenStopping { get; set; } = true;

	/// <summary>
	/// Unsubscribe from market data when strategy is stopping.
	/// </summary>
	public bool UnsubscribeOnStop { get; set; } = true;

	/// <summary>
	/// Total accumulated commission (from orders + trades).
	/// </summary>
	public decimal? Commission =>
		Orders.Commission is null && Trades.Commission is null
			? null : (Orders.Commission ?? 0m) + (Trades.Commission ?? 0m);

	/// <summary>
	/// Total accumulated slippage.
	/// </summary>
	public decimal? Slippage => Trades.Slippage;

	/// <summary>
	/// Error event.
	/// </summary>
	public event Action<Exception> Error;

	/// <summary>
	/// Connector changed event.
	/// </summary>
	public event Action ConnectorChanged;

	/// <summary>
	/// Strategy parameters changed event.
	/// </summary>
	public event Action ParametersChanged;

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
	public event Action<OrderFail> OrderRegisterFailed;

	/// <summary>
	/// Order edited.
	/// </summary>
	public event Action<long, Order> OrderEdited;

	/// <summary>
	/// Order edit failed.
	/// </summary>
	public event Action<long, OrderFail> OrderEditFailed;

	/// <summary>
	/// Order cancellation failed.
	/// </summary>
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
	public event Action<DecomposedStrategy> IsOnlineChanged;

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
	public ProcessStates ProcessState => Engine.ProcessState;

	/// <summary>
	/// Start the strategy.
	/// </summary>
	public ValueTask StartAsync(CancellationToken cancellationToken = default)
	{
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
	public ValueTask StopAsync(CancellationToken cancellationToken = default) => Engine.RequestStopAsync(cancellationToken);

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
			ProcessRisk(order.CreateRegisterMessage());

			if (_isTradingBlocked)
				return;
		}

		if (!CanTrade(order.Security ?? Security, order.Portfolio ?? Portfolio, order.Side, order.Volume, out var reason))
		{
			ProcessOrderFail(order, new InvalidOperationException(reason));
			return;
		}

		Orders.TryAttach(order);
		OrderRegistering?.Invoke(order);
		SubmitNewOrder(order, () => _connector.RegisterOrder(order));
	}

	/// <summary>
	/// Cancel an order via the connector.
	/// </summary>
	public void CancelOrder(Order order)
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
			ProcessRisk(changes.CreateRegisterMessage());

			if (_isTradingBlocked)
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
			ProcessRisk(newOrder.CreateRegisterMessage());

			if (_isTradingBlocked)
				return;
		}

		Orders.TryAttach(newOrder);
		OrderReRegistering?.Invoke(oldOrder, newOrder);
		SubmitNewOrder(newOrder, () => _connector.ReRegisterOrder(oldOrder, newOrder));
	}

	/// <summary>
	/// Create an initialized order object.
	/// </summary>
	/// <param name="side">Order side.</param>
	/// <param name="price">Price. 0 for market order.</param>
	/// <param name="volume">Volume. If null, <see cref="Volume"/> is used.</param>
	/// <returns>Order.</returns>
	public Order CreateOrder(Sides side, decimal price, decimal? volume = null)
	{
		var order = new Order
		{
			Portfolio = Portfolio,
			Security = Security,
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
	public Order BuyMarket(decimal? volume = null)
	{
		var order = CreateOrder(Sides.Buy, default, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Sell at market price.
	/// </summary>
	public Order SellMarket(decimal? volume = null)
	{
		var order = CreateOrder(Sides.Sell, default, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Buy at limit price.
	/// </summary>
	public Order BuyLimit(decimal price, decimal? volume = null)
	{
		var order = CreateOrder(Sides.Buy, price, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Sell at limit price.
	/// </summary>
	public Order SellLimit(decimal price, decimal? volume = null)
	{
		var order = CreateOrder(Sides.Sell, price, volume);
		RegisterOrder(order);
		return order;
	}

	/// <summary>
	/// Close current position.
	/// </summary>
	public Order ClosePosition()
	{
		var position = Position;

		if (position == 0)
			return null;

		var volume = position.Abs();
		return position > 0 ? SellMarket(volume) : BuyMarket(volume);
	}

	/// <summary>
	/// Cancel all active orders.
	/// </summary>
	public void CancelActiveOrders()
	{
		foreach (var order in Orders.Orders.Where(o => o.State == OrderStates.Active))
			CancelOrder(order);
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
		OnOrderChanged(order);
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

		PnLReceived?.Invoke(subscription);

		if (Portfolio is not null)
			PnLReceived2?.Invoke(subscription, Portfolio, time, PnLManager.RealizedPnL, PnLManager.UnrealizedPnL, Commission);

		StatisticManager.AddPnL(time, PnLManager.GetPnL(), Commission);
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

	private void RecycleOrders()
	{
		if (OrdersKeepTime == TimeSpan.Zero)
			return;

		var diff = _lastOrderTime - _firstOrderTime;

		if (diff <= _maxOrdersKeepTime)
			return;

		_firstOrderTime = _lastOrderTime - OrdersKeepTime;
		Orders.RemoveDoneBefore(_firstOrderTime);
	}

	/// <summary>
	/// Reset all state (orders, trades, positions, PnL, subscriptions).
	/// </summary>
	public void Reset()
	{
		var positions = _posManager.Positions;

		StatisticManager.Reset();
		PnLManager.Reset();
		RiskManager.Reset();
		Orders.Reset();
		Trades.Reset();
		Subscriptions.Reset();
		_posManager.Reset();
		Engine.ForceStop();

		_ownTransactionIds.Clear();
		_pendingOwnOrders.Clear();
		_ordersAdjustedByTrade.Clear();
		_positions.Clear();
		_boardMsg = default;
		_portfolioLookup = default;
		_prevTradeDate = default;
		_isPrevDateTradable = default;
		_firstOrderTime = default;
		_lastOrderTime = default;
		_maxOrdersKeepTime = TimeSpan.FromTicks((long)(OrdersKeepTime.Ticks * 1.5));
		_position = 0;
		_isTradingBlocked = false;
		_lastCantTradeReason = default;

		Latency = null;
		ErrorState = default;

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

		Reseted?.Invoke();

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
				var startedTime = ((IStrategyHost)this).CurrentTime;
				var notifyStartedTime = StartedTime == startedTime;

				StartedTime = startedTime;

				if (notifyStartedTime)
					this.Notify(nameof(StartedTime));

				TotalWorkingTime = default;
				ErrorState = LogLevels.Info;
				this.Notify(nameof(IsFormed));
				break;
			}
			case ProcessStates.Stopping:
			{
				if (UnsubscribeOnStop)
					Subscriptions.UnSubscribeAll(globalAndLocal: false);

				Subscriptions.UnSubscribeAll(globalAndLocal: true);
				IsOnline = false;
				break;
			}
			case ProcessStates.Stopped:
			{
				var totalWorkingTime = TotalWorkingTime;
				var startedTime = StartedTime;

				if (StartedTime != default)
					TotalWorkingTime += ((IStrategyHost)this).CurrentTime - StartedTime;

				if (TotalWorkingTime == totalWorkingTime)
					this.Notify(nameof(TotalWorkingTime));

				StartedTime = default;

				if (startedTime == default)
					this.Notify(nameof(StartedTime));

				break;
			}
		}
	}

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
	/// Called when order state changed.
	/// </summary>
	/// <param name="order">Changed order.</param>
	protected virtual void OnOrderChanged(Order order) { }

	/// <summary>
	/// Called when new trade received.
	/// </summary>
	/// <param name="trade">New trade.</param>
	protected virtual void OnNewMyTrade(MyTrade trade) { }

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
		OrderRegisterFailed?.Invoke(fail);
		StatisticManager.AddRegisterFailedOrder(fail);
	}

	/// <summary>
	/// Processing of error, occurred as result of strategy operation.
	/// </summary>
	/// <param name="error">Error.</param>
	protected virtual void OnError(Exception error)
	{
		Error?.Invoke(error);
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

			var currentPosition = GetPositionValue(security, portfolio);

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

		return Orders.TryGetTracked(order);
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

		Orders.ProcessOrder(order, isChanging: true);

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

	private void ProcessRisk(Message msg)
	{
		foreach (var rule in RiskManager.ProcessRules(msg))
		{
			switch (rule.Action)
			{
				case RiskActions.StopTrading:
					_isTradingBlocked = true;
					break;
				case RiskActions.ClosePositions:
					ClosePosition();
					break;
				case RiskActions.CancelOrders:
					CancelActiveOrders();
					break;
			}
		}
	}

	private void SubscribeConnector()
	{
		_connector.OrderReceived += OnOrderReceived;
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
		_connector.OrderReceived -= OnOrderReceived;
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

		if (!Orders.IsTracked(order))
		{
			if (!CanAttach(order))
				return;

			Orders.TryAttach(order);
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

		if (!Orders.IsTracked(order) && !CanAttach(order))
			return;

		ErrorState = LogLevels.Error;
		OrderCancelFailed?.Invoke(fail);
		StatisticManager.AddFailedOrderCancel(fail);
	}

	private void OnConnectorOrderEdited(long transactionId, Order order)
	{
		if (Orders.IsTracked(order))
			OrderEdited?.Invoke(transactionId, order);
	}

	private void OnConnectorOrderEditFailed(long transactionId, OrderFail fail)
	{
		if (fail?.Order is Order order && Orders.IsTracked(order))
			OrderEditFailed?.Invoke(transactionId, fail);
	}

	/// <summary>
	/// Handle order received from connector.
	/// </summary>
	public void OnOrderReceived(Subscription sub, Order order)
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
			Orders.TryAttach(trackedOrder);
			Orders.ProcessOrder(trackedOrder, isChanging: true);
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
			PnLManager.UpdateSecurity(new Level1ChangeMessage
			{
				SecurityId = sec.ToSecurityId(),
				ServerTime = trade.Trade.ServerTime,
			}
			.TryAdd(Level1Fields.PriceStep, sec.PriceStep)
			.TryAdd(Level1Fields.Multiplier, sec.Multiplier));
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

	DateTime ITimeProvider.CurrentTime => ((IStrategyHost)this).CurrentTime;

	event Action<TimeSpan> ITimeProvider.CurrentTimeChanged
	{
		add => _currentTimeChanged += value;
		remove => _currentTimeChanged -= value;
	}

	void INotifyPropertyChangedEx.NotifyPropertyChanged(string info)
		=> PropertyChanged?.Invoke(this, info);

	#endregion
}
