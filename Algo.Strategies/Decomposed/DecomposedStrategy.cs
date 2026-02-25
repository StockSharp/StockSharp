namespace StockSharp.Algo.Strategies.Decomposed;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Statistics;

/// <summary>
/// The base class for all trade strategies.
/// </summary>
public class DecomposedStrategy : IStrategyHost
{
	private IConnector _connector;
	private readonly HashSet<long> _ownTransactionIds = [];
	private readonly Dictionary<(SecurityId secId, string pfName), decimal> _positions = [];
	private bool _isTradingBlocked;
	private decimal _position;

	/// <summary>
	/// Initializes a new instance <see cref="DecomposedStrategy"/>.
	/// </summary>
	public DecomposedStrategy()
		: this(new PnLManager(), new StatisticManager())
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

		Engine = new(this, PnLManager);
		Orders = new(StatisticManager);
		Trades = new(PnLManager, StatisticManager);
		Positions = new(StatisticManager);
		Subscriptions = new(this);
		RiskManager = new RiskManager();

		Subscriptions.SubscriptionRequested += s => _connector?.Subscribe(s);
		Subscriptions.UnsubscriptionRequested += s => _connector?.UnSubscribe(s);

		Engine.StateChanged += OnStateChanged;
		Engine.StateChanged += state =>
		{
			if (state == ProcessStates.Stopping)
			{
				if (CancelOrdersWhenStopping)
					CancelActiveOrders();

				if (UnsubscribeOnStop)
					foreach (var sub in Subscriptions.Subscriptions.ToArray())
						Subscriptions.UnSubscribe(sub);
			}
		};
		Engine.CurrentPriceUpdated += (secId, price, serverTime, localTime) => OnCurrentPriceUpdated(secId, price, serverTime, localTime);
		Orders.Registered += OnOrderRegistered;
		Orders.Registered += order =>
		{
			if (order.Commission is not null)
				CommissionChanged?.Invoke();

			if (order.LatencyRegistration is TimeSpan lat && lat != TimeSpan.Zero)
			{
				Latency = (Latency ?? TimeSpan.Zero) + lat;
				LatencyChanged?.Invoke();
			}
		};
		Orders.Changed += OnOrderChanged;
		Trades.TradeAdded += OnNewMyTrade;
		Trades.CommissionChanged += () => CommissionChanged?.Invoke();
		Trades.SlippageChanged += () => SlippageChanged?.Invoke();
		Trades.TradeAdded += trade =>
		{
			var vol = trade.Trade.Volume;
			var delta = trade.Order.Side == Sides.Buy ? vol : -vol;
			var key = (trade.Order.Security.ToSecurityId(), trade.Order.Portfolio?.Name ?? string.Empty);
			_positions.TryGetValue(key, out var current);
			var newPos = current + delta;
			_positions[key] = newPos;

			if (Security != null && key == (Security.ToSecurityId(), Portfolio?.Name ?? string.Empty))
				_position = newPos;

			trade.Position = newPos;
		};
		Positions.NewPosition += OnNewPosition;
		Positions.PositionChanged += OnPositionChanged;
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
			if (_connector != null)
				UnsubscribeConnector();

			_connector = value;

			if (_connector != null)
				SubscribeConnector();
		}
	}

	/// <summary>
	/// Security.
	/// </summary>
	public Security Security { get; set; }

	/// <summary>
	/// Portfolio.
	/// </summary>
	public Portfolio Portfolio { get; set; }

	/// <summary>
	/// Default order volume.
	/// </summary>
	public decimal Volume { get; set; } = 1;

	/// <summary>
	/// Current position (primary security).
	/// </summary>
	public decimal Position
	{
		get => _position;
		set
		{
			_position = value;

			if (Security != null)
			{
				var key = (Security.ToSecurityId(), Portfolio?.Name ?? string.Empty);
				_positions[key] = value;
			}
		}
	}

	/// <summary>
	/// Get position value for specific security and portfolio.
	/// </summary>
	public decimal GetPositionValue(Security sec, Portfolio pf)
	{
		var key = (sec.ToSecurityId(), pf?.Name ?? string.Empty);
		return _positions.TryGetValue(key, out var val) ? val : 0;
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
	public LogLevels ErrorState { get; set; }

	/// <summary>
	/// Wait for all trades.
	/// </summary>
	public bool WaitAllTrades { get; set; }

	/// <summary>
	/// Is online.
	/// </summary>
	public bool IsOnline { get; set; }

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
	/// Current process state.
	/// </summary>
	public ProcessStates ProcessState => Engine.ProcessState;

	/// <summary>
	/// Start the strategy.
	/// </summary>
	public ValueTask StartAsync(CancellationToken cancellationToken = default) => Engine.RequestStartAsync(cancellationToken);

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
		if (_isTradingBlocked)
			return;

		if (order.TransactionId == 0 && _connector?.TransactionIdGenerator != null)
			order.TransactionId = _connector.TransactionIdGenerator.GetNextId();

		_ownTransactionIds.Add(order.TransactionId);

		if (RiskManager.Rules.Count > 0)
		{
			ProcessRisk(order.CreateRegisterMessage());

			if (_isTradingBlocked)
				return;
		}

		_connector.RegisterOrder(order);
	}

	/// <summary>
	/// Cancel an order via the connector.
	/// </summary>
	public void CancelOrder(Order order) => _connector.CancelOrder(order);

	/// <summary>
	/// Edit an order via the connector.
	/// </summary>
	/// <param name="order">Original order.</param>
	/// <param name="changes">Order changes.</param>
	public void EditOrder(Order order, Order changes)
	{
		if (_isTradingBlocked)
			return;

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
		if (_isTradingBlocked)
			return;

		if (newOrder.TransactionId == 0 && _connector?.TransactionIdGenerator != null)
			newOrder.TransactionId = _connector.TransactionIdGenerator.GetNextId();

		_ownTransactionIds.Add(newOrder.TransactionId);

		if (RiskManager.Rules.Count > 0)
		{
			ProcessRisk(newOrder.CreateRegisterMessage());

			if (_isTradingBlocked)
				return;
		}

		_connector.ReRegisterOrder(oldOrder, newOrder);
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

		var volume = Math.Abs(position);
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

	/// <summary>
	/// Reset all state (orders, trades, positions, PnL, subscriptions).
	/// </summary>
	public void Reset()
	{
		Orders.Reset();
		Trades.Reset();
		Subscriptions.Reset();
		Engine.ForceStop();

		_ownTransactionIds.Clear();
		_positions.Clear();
		_position = 0;
		_isTradingBlocked = false;

		Latency = null;
		ErrorState = default;
	}

	#region Virtual hooks

	/// <summary>
	/// Called when process state changes.
	/// </summary>
	/// <param name="state">New state.</param>
	protected virtual void OnStateChanged(ProcessStates state) { }

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
		if (fail.Error != null)
			OnError(fail.Error);
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
		if (_ownTransactionIds.Count == 0)
			return true;

		return _ownTransactionIds.Contains(order.TransactionId);
	}

	#endregion

	#region IStrategyHost

	DateTime IStrategyHost.CurrentTime
		=> _connector is ITimeProvider tp ? tp.CurrentTime : DateTime.UtcNow;

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
		_connector.OrderRegisterFailReceived += OnOrderRegisterFailReceived;
		_connector.NewOutMessageAsync += OnNewMessage;
		_connector.CurrentTimeChanged += OnTimeChanged;
	}

	private void UnsubscribeConnector()
	{
		_connector.OrderReceived -= OnOrderReceived;
		_connector.OwnTradeReceived -= OnTradeReceived;
		_connector.PositionReceived -= OnPositionReceived;
		_connector.OrderRegisterFailReceived -= OnOrderRegisterFailReceived;
		_connector.NewOutMessageAsync -= OnNewMessage;
		_connector.CurrentTimeChanged -= OnTimeChanged;
	}

	/// <summary>
	/// Handle order received from connector.
	/// </summary>
	public void OnOrderReceived(Subscription sub, Order order)
	{
		if (!Subscriptions.CanProcess(sub))
			return;

		if (!Orders.IsTracked(order) && !CanAttach(order))
			return;

		Orders.TryAttach(order);
		Orders.ProcessOrder(order, isChanging: true);
	}

	/// <summary>
	/// Handle trade received from connector.
	/// </summary>
	public void OnTradeReceived(Subscription sub, MyTrade trade)
	{
		if (!Subscriptions.CanProcess(sub))
			return;

		if (!Orders.IsTracked(trade.Order))
			return;

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

		Trades.TryAdd(trade);

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

	/// <summary>
	/// Handle order registration failure from connector.
	/// </summary>
	public void OnOrderRegisterFailReceived(Subscription sub, OrderFail fail)
	{
		if (!Subscriptions.CanProcess(sub))
			return;

		OnOrderRegisterFailed(fail);
	}

	/// <summary>
	/// Handle new outgoing message from connector.
	/// </summary>
	public ValueTask OnNewMessage(Message msg, CancellationToken ct)
	{
		Engine.OnMessage(msg);
		return default;
	}

	private void OnTimeChanged(TimeSpan diff)
	{
		// time changed events can be used for periodic checks
	}

	#endregion
}
