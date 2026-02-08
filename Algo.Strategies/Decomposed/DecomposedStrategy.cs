namespace StockSharp.Algo.Strategies.Decomposed;

using StockSharp.Algo.PnL;
using StockSharp.Algo.Statistics;

/// <summary>
/// The base class for all trade strategies.
/// </summary>
public class DecomposedStrategy : IStrategyHost
{
	private IConnector _connector;

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

		Subscriptions.SubscriptionRequested += s => _connector?.Subscribe(s);
		Subscriptions.UnsubscriptionRequested += s => _connector?.UnSubscribe(s);

		Engine.StateChanged += OnStateChanged;
		Engine.CurrentPriceUpdated += (secId, price, serverTime, localTime) => OnCurrentPriceUpdated(secId, price, serverTime, localTime);
		Orders.Registered += OnOrderRegistered;
		Orders.Changed += OnOrderChanged;
		Trades.TradeAdded += OnNewMyTrade;
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
	/// Current position.
	/// </summary>
	public decimal Position { get; set; }

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
	/// Stop the strategy.
	/// </summary>
	[Obsolete("Use StopAsync instead.")]
	public void Stop() => AsyncHelper.Run(() => StopAsync());

	/// <summary>
	/// Register an order via the connector.
	/// </summary>
	public void RegisterOrder(Order order) => _connector.RegisterOrder(order);

	/// <summary>
	/// Cancel an order via the connector.
	/// </summary>
	public void CancelOrder(Order order) => _connector.CancelOrder(order);

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

	private void SubscribeConnector()
	{
		_connector.OrderReceived += OnOrderReceived;
		_connector.OwnTradeReceived += OnTradeReceived;
		_connector.PositionReceived += OnPositionReceived;
		_connector.NewOutMessageAsync += OnNewMessage;
		_connector.CurrentTimeChanged += OnTimeChanged;
	}

	private void UnsubscribeConnector()
	{
		_connector.OrderReceived -= OnOrderReceived;
		_connector.OwnTradeReceived -= OnTradeReceived;
		_connector.PositionReceived -= OnPositionReceived;
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

		Trades.TryAdd(trade);
	}

	/// <summary>
	/// Handle position received from connector.
	/// </summary>
	public void OnPositionReceived(Subscription sub, Position pos)
	{
		if (!Subscriptions.CanProcess(sub))
			return;

		Positions.Process(pos, isNew: true);
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
