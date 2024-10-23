namespace StockSharp.Algo;

using System.Security;

using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Risk;
using StockSharp.Algo.Slippage;

/// <summary>
/// The class to create connections to trading systems.
/// </summary>
public partial class Connector : BaseLogReceiver, IConnector, IMarketDataProvider, ISubscriptionProvider
{
	private readonly EntityCache _entityCache;
	private readonly SubscriptionManager _subscriptionManager;

	// backward compatibility for NewXXX events
	private readonly CachedSynchronizedSet<Security> _existingSecurities = [];
	private readonly CachedSynchronizedSet<Portfolio> _existingPortfolios = [];
	private readonly CachedSynchronizedSet<Position> _existingPositions = [];

	private bool _isDisposing;

	/// <summary>
	/// Initializes a new instance of the <see cref="Connector"/>.
	/// </summary>
	public Connector()
		: this(new InMemorySecurityStorage(), new InMemoryPositionStorage(), new InMemoryExchangeInfoProvider())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Connector"/>.
	/// </summary>
	/// <param name="securityStorage">Securities meta info storage.</param>
	/// <param name="positionStorage">Position storage.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	/// <param name="storageRegistry">The storage of market data.</param>
	/// <param name="snapshotRegistry">Snapshot storage registry.</param>
	/// <param name="buffer">Storage buffer.</param>
	/// <param name="initAdapter">Initialize basket adapter.</param>
	/// <param name="initChannels">Initialize channels.</param>
	public Connector(ISecurityStorage securityStorage, IPositionStorage positionStorage,
		IExchangeInfoProvider exchangeInfoProvider, IStorageRegistry storageRegistry = null,
		SnapshotRegistry snapshotRegistry = null, StorageBuffer buffer = null, bool initAdapter = true, bool initChannels = true)
	{
		Buffer = buffer;

		SecurityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
		PositionStorage = positionStorage ?? throw new ArgumentNullException(nameof(positionStorage));

		_entityCache = new EntityCache(this, TryGetSecurity, exchangeInfoProvider, PositionStorage);

		_subscriptionManager = new SubscriptionManager(this);

		//SupportLevel1DepthBuilder = true;
		SupportFilteredMarketDepth = true;

		if (initChannels)
		{
			InMessageChannel = new InMemoryMessageChannel(new MessageByOrderQueue(), $"Connector In ({Name})", RaiseError);
			OutMessageChannel = new InMemoryMessageChannel(new MessageByOrderQueue(), $"Connector Out ({Name})", RaiseError);
		}

		SnapshotRegistry = snapshotRegistry;

		if (initAdapter)
		{
			Adapter = new BasketMessageAdapter(new MillisecondIncrementalIdGenerator(), new CandleBuilderProvider(ExchangeInfoProvider), new InMemorySecurityMessageAdapterProvider(), new InMemoryPortfolioMessageAdapterProvider(), buffer)
			{
				StorageSettings = { StorageRegistry = storageRegistry }
			};
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="Connector"/>.
	/// </summary>
	/// <param name="entityRegistry">The storage of trade objects.</param>
	/// <param name="storageRegistry">The storage of market data.</param>
	/// <param name="snapshotRegistry">Snapshot storage registry.</param>
	/// <param name="buffer">Storage buffer.</param>
	[Obsolete]
	public Connector(IEntityRegistry entityRegistry, IStorageRegistry storageRegistry, SnapshotRegistry snapshotRegistry, StorageBuffer buffer = null)
		: this(entityRegistry.Securities, entityRegistry.PositionStorage, storageRegistry.CheckOnNull(nameof(storageRegistry)).ExchangeInfoProvider, storageRegistry, snapshotRegistry, buffer)
	{
	}

	/// <summary>
	/// Securities meta info storage.
	/// </summary>
	public ISecurityStorage SecurityStorage { get; }

	/// <summary>
	/// Position storage.
	/// </summary>
	public IPositionStorage PositionStorage { get; }

	/// <summary>
	/// Exchanges and trading boards provider.
	/// </summary>
	public IExchangeInfoProvider ExchangeInfoProvider => _entityCache.ExchangeInfoProvider;

	/// <summary>
	/// The storage of market data.
	/// </summary>
	public IStorageRegistry StorageRegistry => Adapter?.StorageSettings.StorageRegistry;

	/// <summary>
	/// Snapshot storage registry.
	/// </summary>
	public SnapshotRegistry SnapshotRegistry { get; private set; }

	private IBasketSecurityProcessorProvider _basketSecurityProcessorProvider = new BasketSecurityProcessorProvider();

	/// <summary>
	/// Basket security processors provider.
	/// </summary>
	public IBasketSecurityProcessorProvider BasketSecurityProcessorProvider
	{
		get => _basketSecurityProcessorProvider;
		set => _basketSecurityProcessorProvider = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Restore subscription on reconnect.
	/// </summary>
	/// <remarks>
	/// Normal case connect/disconnect.
	/// </remarks>
	public bool IsRestoreSubscriptionOnNormalReconnect { get; set; } = true;

	/// <summary>
	/// Send unsubscribe on disconnect command.
	/// </summary>
	/// <remarks>By default is <see langword="true"/>.</remarks>
	public bool IsAutoUnSubscribeOnDisconnect { get; set; } = true;

	/// <summary>
	/// Subscribe for new portfolios.
	/// </summary>
	/// <remarks>By default is <see langword="true"/>.</remarks>
	public bool IsAutoPortfoliosSubscribe { get; set; } = true;

	/// <summary>
	/// Settings of the connection control <see cref="IConnector"/> to the trading system.
	/// </summary>
	[Obsolete("Use exact IMessageAdapter to set reconnecting settings.")]
	public ReConnectionSettings ReConnectionSettings { get; } = new ReConnectionSettings();

	/// <summary>
	/// Number of tick trades for storage. The default is 100000. If the value is set to <see cref="int.MaxValue"/>, the trades will not be deleted. If the value is set to 0, then the trades will not be stored.
	/// </summary>
	[Obsolete("Do nothing.")]
	public int TradesKeepCount { get; set; }

	/// <summary>
	/// The number of orders for storage. The default is 1000. If the value is set to <see cref="int.MaxValue"/>, then the orders will not be deleted. If the value is set to 0, then the orders will not be stored.
	/// </summary>
	public int OrdersKeepCount
	{
		get => _entityCache.OrdersKeepCount;
		set => _entityCache.OrdersKeepCount = value;
	}

	/// <inheritdoc />
	public IdGenerator TransactionIdGenerator
	{
		get => Adapter.TransactionIdGenerator;
		set => Adapter.TransactionIdGenerator = value;
	}

	private SecurityIdGenerator _securityIdGenerator = new();

	/// <summary>
	/// The instrument identifiers generator <see cref="Security.Id"/>.
	/// </summary>
	public SecurityIdGenerator SecurityIdGenerator
	{
		get => _securityIdGenerator;
		set => _securityIdGenerator = value ?? throw new ArgumentNullException(nameof(value));
	}

	private bool _overrideSecurityData;

	/// <summary>
	/// Override previous security data by new values.
	/// </summary>
	public bool OverrideSecurityData
	{
		get => _overrideSecurityData;
		set
		{
			_overrideSecurityData = value;

			if (StorageAdapter != null)
				StorageAdapter.OverrideSecurityData = value;
		}
	}

	/// <inheritdoc />
	public IEnumerable<ExchangeBoard> ExchangeBoards => ExchangeInfoProvider.Boards;

	/// <inheritdoc />
	public IEnumerable<Security> Securities => _existingSecurities.Cache;

	int ISecurityProvider.Count => SecurityStorage.Count;

	private Action<IEnumerable<Security>> _added;

	event Action<IEnumerable<Security>> ISecurityProvider.Added
	{
		add => _added += value;
		remove => _added -= value;
	}

	private Action<IEnumerable<Security>> _removed;

	event Action<IEnumerable<Security>> ISecurityProvider.Removed
	{
		add => _removed += value;
		remove => _removed -= value;
	}

	private Action _cleared;

	event Action ISecurityProvider.Cleared
	{
		add => _cleared += value;
		remove => _cleared -= value;
	}

	/// <inheritdoc />
	public Security LookupById(SecurityId id) => SecurityStorage.LookupById(id);

	IEnumerable<Security> ISecurityProvider.Lookup(SecurityLookupMessage criteria) => SecurityStorage.Lookup(criteria);

	SecurityMessage ISecurityMessageProvider.LookupMessageById(SecurityId id)
		=> SecurityStorage.LookupMessageById(id);

	IEnumerable<SecurityMessage> ISecurityMessageProvider.LookupMessages(SecurityLookupMessage criteria)
		=> SecurityStorage.LookupMessages(criteria);

	/// <inheritdoc />
	public SessionStates? GetSessionState(ExchangeBoard board) => _entityCache.GetSessionState(board);

	/// <inheritdoc />
	[Obsolete("Use NewOrder event to collect data.")]
	public IEnumerable<Order> Orders => _entityCache.Orders;

	/// <inheritdoc />
	[Obsolete("Use NewStopOrder event to collect data.")]
	public IEnumerable<Order> StopOrders => Orders.Where(o => o.Type == OrderTypes.Conditional);

	/// <inheritdoc />
	[Obsolete("Use OrderRegisterFailed event to collect data.")]
	public IEnumerable<OrderFail> OrderRegisterFails => _entityCache.OrderRegisterFails;

	/// <inheritdoc />
	[Obsolete("Use OrderCancelFailed event to collect data.")]
	public IEnumerable<OrderFail> OrderCancelFails => _entityCache.OrderCancelFails;

	/// <inheritdoc />
	[Obsolete("Use NewTrade event to collect data.")]
	public IEnumerable<Trade> Trades => [];

	/// <inheritdoc />
	[Obsolete("Use NewMyTrade event to collect data.")]
	public IEnumerable<MyTrade> MyTrades => _entityCache.MyTrades;

	/// <inheritdoc />
	[Obsolete("Use NewNews event to collect data.")]
	public IEnumerable<News> News => _entityCache.News;

	/// <inheritdoc />
	public virtual IEnumerable<Portfolio> Portfolios => _existingPortfolios.Cache;

	/// <inheritdoc />
	public IEnumerable<Position> Positions => _existingPositions.Cache;

	/// <summary>
	/// Risk control manager.
	/// </summary>
	public virtual IRiskManager RiskManager { get; set; } = new RiskManager();

	/// <summary>
	/// Orders registration delay calculation manager.
	/// </summary>
	public ILatencyManager LatencyManager
	{
		get => Adapter.LatencyManager;
		set => Adapter.LatencyManager = value;
	}

	/// <summary>
	/// The profit-loss manager.
	/// </summary>
	public IPnLManager PnLManager
	{
		get => Adapter.PnLManager;
		set => Adapter.PnLManager = value;
	}

	/// <summary>
	/// The commission calculating manager.
	/// </summary>
	public ICommissionManager CommissionManager
	{
		get => Adapter.CommissionManager;
		set => Adapter.CommissionManager = value;
	}

	/// <summary>
	/// Slippage manager.
	/// </summary>
	public ISlippageManager SlippageManager
	{
		get => Adapter.SlippageManager;
		set => Adapter.SlippageManager = value;
	}

	private ConnectionStates _connectionState;

	/// <inheritdoc />
	public ConnectionStates ConnectionState
	{
		get => _connectionState;
		private set
		{
			_connectionState = value;
			_stateChanged?.Invoke();
		}
	}

	/// <summary>
	/// Use orders log to create market depths. Disabled by default.
	/// </summary>
	[Obsolete("Use MarketDataMessage.BuildFrom=OrderLog instead.")]
	public bool CreateDepthFromOrdersLog { get; set; }

	/// <summary>
	/// Use orders log to create ticks. Disabled by default.
	/// </summary>
	[Obsolete("Use MarketDataMessage.BuildFrom=OrderLog instead.")]
	public bool CreateTradesFromOrdersLog { get; set; }

	/// <summary>
	/// To update <see cref="Security.LastTick"/>, <see cref="Security.BestBid"/>, <see cref="Security.BestAsk"/> at each update of order book and/or trades. By default is enabled.
	/// </summary>
	public bool UpdateSecurityLastQuotes { get; set; } = true;

	/// <summary>
	/// To update <see cref="Security"/> fields when the <see cref="Level1ChangeMessage"/> message appears. By default is enabled.
	/// </summary>
	public bool UpdateSecurityByLevel1 { get; set; } = true;

	/// <summary>
	/// To update <see cref="Security"/> fields when the <see cref="SecurityMessage"/> message appears. By default is enabled.
	/// </summary>
	public bool UpdateSecurityByDefinition { get; set; } = true;

	/// <summary>
	/// To update the order book for the instrument when the <see cref="Level1ChangeMessage"/> message appears. By default is enabled.
	/// </summary>
	[Obsolete("Use SupportLevel1DepthBuilder property.")]
	public bool CreateDepthFromLevel1
	{
		get => SupportLevel1DepthBuilder;
		set => SupportLevel1DepthBuilder = value;
	}

	/// <summary>
	/// Create a combined security for securities from different boards.
	/// </summary>
	[Obsolete("Use SupportAssociatedSecurity property.")]
	public bool CreateAssociatedSecurity
	{
		get => SupportAssociatedSecurity;
		set => SupportAssociatedSecurity = value;
	}

	/// <summary>
	/// The number of errors passed through the <see cref="Error"/> event.
	/// </summary>
	public int ErrorCount { get; private set; }

	private TimeSpan _marketTimeChangedInterval = TimeSpan.FromMilliseconds(10);

	/// <summary>
	/// The <see cref="TimeMessage"/> message generating Interval. The default is 10 milliseconds.
	/// </summary>
	public virtual TimeSpan MarketTimeChangedInterval
	{
		get => _marketTimeChangedInterval;
		set
		{
			if (value <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_marketTimeChangedInterval = value;
		}
	}

	/// <summary>
	/// Increment periodically <see cref="MarketTimeChangedInterval"/> value of <see cref="ILogSource.CurrentTime"/>.
	/// </summary>
	public bool TimeChange { get; set; } = true;

	/// <summary>
	/// Process strategies positions and store it into <see cref="Positions"/>.
	/// </summary>
	public bool KeepStrategiesPositions { get; set; }

	private readonly CachedSynchronizedSet<MessageTypes> _lookupMessagesOnConnect = new(
	[
		MessageTypes.SecurityLookup,
		MessageTypes.PortfolioLookup,
		MessageTypes.OrderStatus,
		MessageTypes.TimeFrameLookup,
	]);

	/// <summary>
	/// Send lookup messages on connect.
	/// By default is <see cref="MessageTypes.SecurityLookup"/>, <see cref="MessageTypes.PortfolioLookup"/>, <see cref="MessageTypes.OrderStatus"/>, <see cref="MessageTypes.TimeFrameLookup"/>.
	/// </summary>
	public ISet<MessageTypes> LookupMessagesOnConnect => _lookupMessagesOnConnect;

	/// <inheritdoc />
	public void Connect()
	{
		this.AddInfoLog(nameof(Connect));

		try
		{
			if (ConnectionState is not ConnectionStates.Disconnected and not ConnectionStates.Failed)
			{
				this.AddWarningLog(LocalizedStrings.CannotConnectReasonState, ConnectionState);
				return;
			}

			ConnectionState = ConnectionStates.Connecting;

			OnConnect();
		}
		catch (Exception ex)
		{
			RaiseConnectionError(ex);
		}
	}

	/// <summary>
	/// Connect to trading system.
	/// </summary>
	protected virtual void OnConnect()
	{
		if (TimeChange)
			CreateTimer();

		if (!IsRestoreSubscriptionOnNormalReconnect)
			_subscriptionManager.ClearCache();

		SendInMessage(new ConnectMessage());
	}

	/// <inheritdoc />
	public void Disconnect()
	{
		this.AddInfoLog(nameof(Disconnect));

		if (ConnectionState != ConnectionStates.Connected)
		{
			this.AddWarningLog(LocalizedStrings.CannotDisconnectReasonState, ConnectionState);
			return;
		}

		ConnectionState = ConnectionStates.Disconnecting;

		try
		{
			OnDisconnect();
		}
		catch (Exception ex)
		{
			RaiseConnectionError(ex);
		}
	}

	/// <summary>
	/// Disconnect from trading system.
	/// </summary>
	protected virtual void OnDisconnect()
	{
		if (IsAutoUnSubscribeOnDisconnect)
			_subscriptionManager.UnSubscribeAll();

		SendInMessage(new DisconnectMessage());
	}

	/// <inheritdoc />
	public Position GetPosition(Portfolio portfolio, Security security, string strategyId = "", Sides? side = null, string clientCode = "", string depoName = "", TPlusLimits? limitType = null)
	{
		return GetPosition(portfolio, security, strategyId, side, clientCode, depoName, limitType, string.Empty);
	}

	private Position GetPosition(Portfolio portfolio, Security security, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limitType, string description)
	{
		if (portfolio == null)
			throw new ArgumentNullException(nameof(portfolio));

		if (security == null)
			throw new ArgumentNullException(nameof(security));

		var position = PositionStorage.GetOrCreatePosition(portfolio, security, strategyId, side, clientCode, depoName, limitType, (pf, sec, sid, sd, clCode, ddep, limit) =>
		{
			return new Position
			{
				Portfolio = portfolio,
				Security = security,
				DepoName = depoName,
				LimitType = limitType,
				Description = description,
				ClientCode = clientCode,
				StrategyId = strategyId,
				Side = side
			};
		}, out _);

		if (_existingPositions.TryAdd(position))
			RaiseNewPosition(position);

		return position;
	}

	/// <summary>
	/// Check <see cref="Order.Price"/> and <see cref="Order.Volume"/> are they multiply to step.
	/// </summary>
	public bool CheckSteps { get; set; }

	/// <inheritdoc />
	public void RegisterOrder(Order order)
	{
		try
		{
			this.AddOrderInfoLog(order, nameof(RegisterOrder));

			CheckOnNew(order);

			if (order.Type != OrderTypes.Conditional)
			{
				if (order.Volume <= 0)
					throw new ArgumentOutOfRangeException(nameof(order), order.Volume, LocalizedStrings.InvalidValue);
			}

			if (order.Type == null)
				order.Type = order.Price > 0 ? OrderTypes.Limit : OrderTypes.Market;

			InitNewOrder(order);

			OnRegisterOrder(order);
		}
		catch (Exception ex)
		{
			var transactionId = order.TransactionId;

			if (transactionId == 0 || order.State != OrderStates.None)
				transactionId = TransactionIdGenerator.GetNextId();

			SendOrderFailed(order, OrderOperations.Register, ex, transactionId);
		}
	}

	/// <inheritdoc />
	public bool? IsOrderEditable(Order order)
		=> _entityCache.TryGetAdapter(order)?.IsReplaceCommandEditCurrent;

	/// <inheritdoc />
	public bool? IsOrderReplaceable(Order order)
		=> _entityCache.TryGetAdapter(order)?.IsMessageSupported(MessageTypes.OrderReplace);

	/// <inheritdoc />
	public void EditOrder(Order order, Order changes)
	{
		if (order is null)
			throw new ArgumentNullException(nameof(order));

		if (changes is null)
			throw new ArgumentNullException(nameof(changes));

		try
		{
			this.AddOrderInfoLog(order, nameof(EditOrder));

			CheckOnOld(order);
			CheckOnNew(changes);

			if (IsOrderEditable(order) != true)
				this.AddWarningLog("Order {0} is not editable.", order.TransactionId);

			var transactionId = TransactionIdGenerator.GetNextId();
			_entityCache.AddOrderByEditionId(order, transactionId);

			changes.TransactionId = transactionId;
			OnEditOrder(order, changes);
		}
		catch (Exception ex)
		{
			SendOrderFailed(order, OrderOperations.Edit, ex, changes.TransactionId);
		}
	}

	/// <inheritdoc />
	public void ReRegisterOrder(Order oldOrder, Order newOrder)
	{
		if (oldOrder is null)
			throw new ArgumentNullException(nameof(oldOrder));

		if (newOrder is null)
			throw new ArgumentNullException(nameof(newOrder));

		try
		{
			this.AddOrderInfoLog(oldOrder, nameof(ReRegisterOrder));

			if (oldOrder.Security != newOrder.Security)
				throw new ArgumentException(LocalizedStrings.SecuritiesMismatch.Put(newOrder.Security.Id, oldOrder.Security.Id), nameof(newOrder));

			CheckOnOld(oldOrder);
			CheckOnNew(newOrder);

			if (IsOrderReplaceable(oldOrder) != true)
				this.AddWarningLog("Order {0} is not replaceable.", oldOrder.TransactionId);

			InitNewOrder(newOrder);
			_entityCache.AddOrderByCancelationId(oldOrder, newOrder.TransactionId);

			OnReRegisterOrder(oldOrder, newOrder);
		}
		catch (Exception ex)
		{
			var transactionId = newOrder.TransactionId;

			if (transactionId == 0 || newOrder.State != OrderStates.None)
				transactionId = TransactionIdGenerator.GetNextId();

			SendOrderFailed(oldOrder, OrderOperations.Cancel, ex, transactionId);
			SendOrderFailed(newOrder, OrderOperations.Register, ex, transactionId);
		}
	}

	/// <summary>
	/// Replace orders.
	/// </summary>
	/// <param name="oldOrder1">Cancelling order.</param>
	/// <param name="newOrder1">New order to register.</param>
	/// <param name="oldOrder2">Cancelling order.</param>
	/// <param name="newOrder2">New order to register.</param>
	[Obsolete("Use ReRegisterOrder method.")]
	public void ReRegisterOrderPair(Order oldOrder1, Order newOrder1, Order oldOrder2, Order newOrder2)
	{
		if (oldOrder1 == null)
			throw new ArgumentNullException(nameof(oldOrder1));

		if (newOrder1 == null)
			throw new ArgumentNullException(nameof(newOrder1));

		if (oldOrder2 == null)
			throw new ArgumentNullException(nameof(oldOrder2));

		if (newOrder2 == null)
			throw new ArgumentNullException(nameof(newOrder2));

		try
		{
			if (oldOrder1.Security != newOrder1.Security)
				throw new ArgumentException(LocalizedStrings.SecuritiesMismatch.Put(newOrder1.Security.Id, oldOrder1.Security.Id), nameof(newOrder1));

			if (oldOrder2.Security != newOrder2.Security)
				throw new ArgumentException(LocalizedStrings.SecuritiesMismatch.Put(newOrder2.Security.Id, oldOrder2.Security.Id), nameof(newOrder2));

			ReRegisterOrder(oldOrder1, newOrder1);
			ReRegisterOrder(oldOrder2, newOrder2);
		}
		catch (Exception ex)
		{
			var transactionId = newOrder1.TransactionId;

			if (transactionId == 0)
				transactionId = TransactionIdGenerator.GetNextId();

			SendOrderFailed(oldOrder1, OrderOperations.Cancel, ex, transactionId);
			SendOrderFailed(newOrder1, OrderOperations.Register, ex, transactionId);

			SendOrderFailed(oldOrder2, OrderOperations.Cancel, ex, transactionId);
			SendOrderFailed(newOrder2, OrderOperations.Register, ex, transactionId);
		}
	}

	/// <inheritdoc />
	public void CancelOrder(Order order)
	{
		long transactionId = 0;

		try
		{
			this.AddOrderInfoLog(order, nameof(CancelOrder));

			CheckOnOld(order);

			transactionId = TransactionIdGenerator.GetNextId();
			_entityCache.AddOrderByCancelationId(order, transactionId);

			OnCancelOrder(order, transactionId);
		}
		catch (Exception ex)
		{
			if (transactionId == 0)
				transactionId = TransactionIdGenerator.GetNextId();

			SendOrderFailed(order, OrderOperations.Cancel, ex, transactionId);
		}
	}

	private void SendOrderFailed(Order order, OrderOperations operation, Exception error, long originalTransactionId)
	{
		var fail = new OrderFail
		{
			Order = order,
			Error = error,
			ServerTime = CurrentTime
		};

		_entityCache.AddOrderFailById(fail, operation, originalTransactionId);

		SendOutMessage(fail.ToMessage(originalTransactionId));
	}

	private void CheckOnNew(Order order)
	{
		CheckOrderState(order);

		if (order.TransactionId != 0)
			throw new ArgumentException(LocalizedStrings.OrderAlreadyTransId.Put(order.TransactionId), nameof(order));

		if (order.State != OrderStates.None)
			throw new ArgumentException(LocalizedStrings.OrderAlreadyState.Put(order.State), nameof(order));

		if (order.Id != null || !order.StringId.IsEmpty())
			throw new ArgumentException(LocalizedStrings.OrderAlreadyId.Put(order.Id == null ? order.StringId : order.Id.To<string>()), nameof(order));

		if (CheckSteps)
		{
			if (order.Price > 0)
			{
				var priceStep = order.Security.PriceStep;

				if (priceStep != null && (order.Price % priceStep.Value) != 0)
					throw new ArgumentException(LocalizedStrings.OrderPriceNotMultipleOfPriceStep.Put(order.Price, order, priceStep.Value));
			}

			var volumeStep = order.Security.VolumeStep;

			if (volumeStep != null && (order.Volume % volumeStep.Value) != 0)
				throw new ArgumentException(LocalizedStrings.OrderVolumeNotMultipleOfVolumeStep.Put(order.Volume, order, volumeStep.Value));
		}
	}

	private static void CheckOnOld(Order order)
	{
		CheckOrderState(order);

		if (order.TransactionId == 0)
			throw new ArgumentException(LocalizedStrings.OrderNoTransId, nameof(order));
	}

	private static void CheckOrderState(Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		if (order.Type == OrderTypes.Conditional && order.Condition == null)
			throw new ArgumentException(LocalizedStrings.ConditionNotSpecified, nameof(order));

		if (order.Security == null)
			throw new ArgumentException(LocalizedStrings.SecurityNotSpecified, nameof(order));

		if (order.Portfolio == null)
			throw new ArgumentException(LocalizedStrings.PortfolioNotSpecified, nameof(order));

		if (order.Price < 0)
			throw new ArgumentOutOfRangeException(nameof(order), order.Price, LocalizedStrings.InvalidValue);

		if (order.Price == 0 && order.Type == OrderTypes.Limit)
			throw new ArgumentException(LocalizedStrings.LimitOrderMustPrice, nameof(order));
	}

	/// <summary>
	/// Initialize registering order (transaction id etc.).
	/// </summary>
	/// <param name="order">New order.</param>
	private void InitNewOrder(Order order)
	{
		order.Balance = order.Volume;

		if (order.TransactionId == 0)
			order.TransactionId = TransactionIdGenerator.GetNextId();

		//order.Connector = this;

		//if (order.Security is ContinuousSecurity)
		//	order.Security = ((ContinuousSecurity)order.Security).GetSecurity(CurrentTime);

		order.LocalTime = CurrentTime;
		order.ApplyNewState(OrderStates.Pending, this);

		_entityCache.AddOrderByRegistrationId(order);

		SendOutMessage(order.ToMessage());
	}

	/// <summary>
	/// Register new order.
	/// </summary>
	/// <param name="order">Registration details.</param>
	protected void OnRegisterOrder(Order order)
	{
		SendInMessage(order.CreateRegisterMessage(GetSecurityId(order.Security)));
	}

	/// <summary>
	/// Edit the order.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <param name="changes">Order changes.</param>
	protected void OnEditOrder(Order order, Order changes)
	{
		SendInMessage(order.CreateReplaceMessage(changes, GetSecurityId(order.Security)));
	}

	/// <summary>
	/// Reregister the order.
	/// </summary>
	/// <param name="oldOrder">Cancelling order.</param>
	/// <param name="newOrder">New order to register.</param>
	protected void OnReRegisterOrder(Order oldOrder, Order newOrder)
	{
		SendInMessage(oldOrder.CreateReplaceMessage(newOrder, GetSecurityId(newOrder.Security)));
	}

	/// <summary>
	/// Cancel the order.
	/// </summary>
	/// <param name="order">Order to cancel.</param>
	/// <param name="transactionId">Order cancellation transaction id.</param>
	protected void OnCancelOrder(Order order, long transactionId)
	{
		SendInMessage(order.CreateCancelMessage(GetSecurityId(order.Security), transactionId));
	}

	/// <inheritdoc />
	public void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null, SecurityTypes? securityType = null, long? transactionId = null)
	{
		transactionId ??= TransactionIdGenerator.GetNextId();

		_entityCache.TryAddMassCancelationId(transactionId.Value);
		OnCancelOrders(transactionId.Value, isStopOrder, portfolio, direction, board, security, securityType);
	}

	/// <summary>
	/// Cancel orders by filter.
	/// </summary>
	/// <param name="transactionId">Order cancellation transaction id.</param>
	/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
	/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
	/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
	/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
	/// <param name="security">Instrument. If the value is equal to <see langword="null" />, then the instrument does not match the orders cancel filter.</param>
	/// <param name="securityType">Security type. If the value is <see langword="null" />, the type does not use.</param>
	protected void OnCancelOrders(long transactionId, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null, SecurityTypes? securityType = null)
	{
		var cancelMsg = new OrderGroupCancelMessage
		{
			TransactionId = transactionId
		};

		if (security != null)
			cancelMsg.SecurityId = GetSecurityId(security);

		if (board != null)
		{
			var temp = cancelMsg.SecurityId;
			temp.BoardCode = board.Code;
			cancelMsg.SecurityId = temp;
		}

		if (portfolio != null)
			cancelMsg.PortfolioName = portfolio.Name;

		if (isStopOrder != null)
			cancelMsg.OrderType = isStopOrder == true ? OrderTypes.Conditional : OrderTypes.Limit;

		if (direction != null)
			cancelMsg.Side = direction.Value;

		//if (security != null)
		//	security.ToMessage(securityId).CopyTo(cancelMsg);

		if (securityType != null)
			cancelMsg.SecurityType = securityType;

		SendInMessage(cancelMsg);
	}

	/// <summary>
	/// Change password.
	/// </summary>
	/// <param name="newPassword">New password.</param>
	public void ChangePassword(SecureString newPassword)
	{
		var msg = new ChangePasswordMessage
		{
			NewPassword = newPassword,
			TransactionId = TransactionIdGenerator.GetNextId()
		};

		SendInMessage(msg);
	}

	private DateTimeOffset _prevTime;

	private void ProcessTimeInterval(Message message)
	{
		if (message == _marketTimeMessage)
		{
			lock (_marketTimerSync)
				_isMarketTimeHandled = true;
		}

		var currentTime = message.LocalTime;

		if (_prevTime == default)
		{
			_prevTime = currentTime;
			return;
		}

		var diff = currentTime - _prevTime;

		if (diff < MarketTimeChangedInterval)
			return;

		_prevTime = currentTime;
		RaiseMarketTimeChanged(diff);
	}

	/// <inheritdoc />
	public Security GetSecurity(SecurityId securityId)
		=> GetSecurity(securityId, s => false, out _);

	private Security TryGetSecurity(SecurityId? securityId)
		=> securityId == null || securityId.Value == default ? null : GetSecurity(securityId.Value);

	private Security EnsureGetSecurity<TMessage>(TMessage message)
		where TMessage : ISecurityIdMessage, ISubscriptionIdMessage
	{
		var secId = message.SecurityId;

		if (secId.IsMoney())
			return EntitiesExtensions.MoneySecurity;

		if (secId == default)
		{
			var subscrSecId = message
				.GetSubscriptionIds()
				.Select(id => TryGetSubscriptionById(id)?.SecurityId)
				.Where(id => id != null && id.Value != default)
				.FirstOrDefault();

			if (subscrSecId == null || subscrSecId.Value == default)
				throw new ArgumentOutOfRangeException(nameof(message), message, LocalizedStrings.EmptySecId);

			secId = subscrSecId.Value;
		}

		return TryGetSecurity(secId) ?? throw new ArgumentOutOfRangeException(nameof(message), message, LocalizedStrings.SecurityNoFound.Put(secId));
	}

	/// <summary>
	/// To get the instrument by the code. If the instrument is not found, then the <see cref="IEntityFactory.CreateSecurity"/> is called to create an instrument.
	/// </summary>
	/// <param name="id">Security ID.</param>
	/// <param name="changeSecurity">The handler changing the instrument. It returns <see langword="true" /> if the instrument has been changed and the <see cref="IConnector.SecuritiesChanged"/> should be called.</param>
	/// <param name="isNew">Is newly created.</param>
	/// <returns>Security.</returns>
	private Security GetSecurity(SecurityId id, Func<Security, bool> changeSecurity, out bool isNew)
	{
		if (id == default)
			throw new ArgumentNullException(nameof(id));

		if (changeSecurity == null)
			throw new ArgumentNullException(nameof(changeSecurity));

		var security = SecurityStorage.GetOrCreate(id, key =>
		{
			var idInfo = SecurityIdGenerator.Split(key);

			var code = idInfo.SecurityCode;
			var board = ExchangeInfoProvider.GetOrCreateBoard(idInfo.BoardCode);

			return new()
			{
				Id = key,
				Code = code,
				Board = board,
			};
		}, out isNew);

		if (isNew)
			ExchangeInfoProvider.Save(security.Board);

		var isChanged = changeSecurity(security);

		if (_existingSecurities.TryAdd(security))
			RaiseNewSecurity(security);
		else if (isChanged)
			RaiseSecurityChanged(security);

		return security;
	}

	/// <inheritdoc />
	public SecurityId GetSecurityId(Security security)
		=> security.ToSecurityId(SecurityIdGenerator, copyExtended: true);

	/// <summary>
	/// Generate <see cref="Security.Id"/> security.
	/// </summary>
	/// <param name="secCode"><see cref="SecurityId.SecurityCode"/>.</param>
	/// <param name="boardCode"><see cref="SecurityId.BoardCode"/>.</param>
	/// <returns><see cref="Security.Id"/> security.</returns>
	protected string CreateSecurityId(string secCode, string boardCode)
		=> SecurityIdGenerator.GenerateId(secCode, boardCode);

	/// <inheritdoc />
	public object GetSecurityValue(Security security, Level1Fields field)
		=> _entityCache.GetSecurityValue(security, field);

	/// <inheritdoc />
	public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		=> _entityCache.GetLevel1Fields(security);

	/// <summary>
	/// Clear cache.
	/// </summary>
	public virtual void ClearCache()
	{
		_entityCache.Clear();

		_existingSecurities.Clear();
		_existingPortfolios.Clear();
		_existingPositions.Clear();

		_prevTime = default;

		ConnectionState = ConnectionStates.Disconnected;

		_subscriptionManager.ClearCache();

		SendInMessage(new ResetMessage());

		CloseTimer();

		_cleared?.Invoke();
	}

	/// <summary>
	/// To release allocated resources. In particular, to disconnect from the trading system via <see cref="Disconnect"/>.
	/// </summary>
	protected override void DisposeManaged()
	{
		_isDisposing = true;

		if (ConnectionState == ConnectionStates.Connected)
		{
			try
			{
				Disconnect();
			}
			catch (Exception ex)
			{
				RaiseConnectionError(ex);
			}
		}

		base.DisposeManaged();

		//if (ConnectionState == ConnectionStates.Disconnected || ConnectionState == ConnectionStates.Failed)
		//	TransactionAdapter = null;

		//if (ExportState == ConnectionStates.Disconnected || ExportState == ConnectionStates.Failed)
		//	MarketDataAdapter = null;

		SendInMessage(_disposeMessage);

		CloseTimer();
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		OrdersKeepCount = storage.GetValue(nameof(OrdersKeepCount), OrdersKeepCount);
		UpdateSecurityLastQuotes = storage.GetValue(nameof(UpdateSecurityLastQuotes), UpdateSecurityLastQuotes);
		UpdateSecurityByLevel1 = storage.GetValue(nameof(UpdateSecurityByLevel1), UpdateSecurityByLevel1);
		UpdateSecurityByDefinition = storage.GetValue(nameof(UpdateSecurityByDefinition), UpdateSecurityByDefinition);
		//ReConnectionSettings.Load(storage.GetValue<SettingsStorage>(nameof(ReConnectionSettings)));
		OverrideSecurityData = storage.GetValue(nameof(OverrideSecurityData), OverrideSecurityData);
		CheckSteps = storage.GetValue(nameof(CheckSteps), CheckSteps);
		KeepStrategiesPositions = storage.GetValue(nameof(KeepStrategiesPositions), KeepStrategiesPositions);

		if (storage.ContainsKey(nameof(RiskManager)))
			RiskManager = storage.GetValue<SettingsStorage>(nameof(RiskManager)).LoadEntire<IRiskManager>();

		Adapter.Load(storage, nameof(Adapter));

		MarketTimeChangedInterval = storage.GetValue<TimeSpan>(nameof(MarketTimeChangedInterval));
		SupportAssociatedSecurity = storage.GetValue(nameof(SupportAssociatedSecurity), SupportAssociatedSecurity);

		var lookupMessagesOnConnect = storage.GetValue<object>(nameof(LookupMessagesOnConnect));
		if (lookupMessagesOnConnect is bool b)
		{
			if (!b)
				LookupMessagesOnConnect.Clear();
		}
		else if (lookupMessagesOnConnect is string str)
		{
			LookupMessagesOnConnect.Clear();
			LookupMessagesOnConnect.AddRange(str.SplitByComma(true).Select(s => s.To<MessageTypes>()));
		}

		IsRestoreSubscriptionOnNormalReconnect = storage.GetValue(nameof(IsRestoreSubscriptionOnNormalReconnect), IsRestoreSubscriptionOnNormalReconnect);
		IsAutoUnSubscribeOnDisconnect = storage.GetValue(nameof(IsAutoUnSubscribeOnDisconnect), IsAutoUnSubscribeOnDisconnect);
		IsAutoPortfoliosSubscribe = storage.GetValue(nameof(IsAutoPortfoliosSubscribe), IsAutoPortfoliosSubscribe);

		if (Buffer != null && storage.ContainsKey(nameof(Buffer)))
			Buffer.Load(storage, nameof(Buffer));

		base.Load(storage);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		if (storage is null)
			throw new ArgumentNullException(nameof(storage));

		storage.SetValue(nameof(OrdersKeepCount), OrdersKeepCount);
		storage.SetValue(nameof(UpdateSecurityLastQuotes), UpdateSecurityLastQuotes);
		storage.SetValue(nameof(UpdateSecurityByLevel1), UpdateSecurityByLevel1);
		storage.SetValue(nameof(UpdateSecurityByDefinition), UpdateSecurityByDefinition);
		//storage.SetValue(nameof(ReConnectionSettings), ReConnectionSettings.Save());
		storage.SetValue(nameof(OverrideSecurityData), OverrideSecurityData);
		storage.SetValue(nameof(CheckSteps), CheckSteps);
		storage.SetValue(nameof(KeepStrategiesPositions), KeepStrategiesPositions);

		if (RiskManager != null)
			storage.SetValue(nameof(RiskManager), RiskManager.SaveEntire(false));

		storage.SetValue(nameof(Adapter), Adapter.Save());

		storage.SetValue(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval);
		storage.SetValue(nameof(SupportAssociatedSecurity), SupportAssociatedSecurity);

		storage.SetValue(nameof(LookupMessagesOnConnect), _lookupMessagesOnConnect.Cache.Select(t => t.To<string>()).JoinComma());
		storage.SetValue(nameof(IsRestoreSubscriptionOnNormalReconnect), IsRestoreSubscriptionOnNormalReconnect);
		storage.SetValue(nameof(IsAutoUnSubscribeOnDisconnect), IsAutoUnSubscribeOnDisconnect);
		storage.SetValue(nameof(IsAutoPortfoliosSubscribe), IsAutoPortfoliosSubscribe);

		if (Buffer != null)
			storage.SetValue(nameof(Buffer), Buffer.Save());

		base.Save(storage);
	}

	#region IMessageChannel implementation

	private Action<Message> _newOutMessage;

	event Action<Message> IMessageChannel.NewOutMessage
	{
		add => _newOutMessage += value;
		remove => _newOutMessage -= value;
	}

	ChannelStates IMessageChannel.State => ConnectionState == ConnectionStates.Connected ? ChannelStates.Started : ChannelStates.Stopped;

	private Action _stateChanged;

	event Action IMessageChannel.StateChanged
	{
		add => _stateChanged += value;
		remove => _stateChanged -= value;
	}

	void IMessageChannel.Open()
	{
		Connect();
	}

	void IMessageChannel.Close()
	{
		Disconnect();
	}

	void IMessageChannel.Suspend()
	{
	}

	void IMessageChannel.Resume()
	{
	}

	void IMessageChannel.Clear()
	{
	}

	IMessageChannel ICloneable<IMessageChannel>.Clone()
	{
		return this.Clone();
	}

	object ICloneable.Clone()
	{
		return this.Clone();
	}

	#endregion
}