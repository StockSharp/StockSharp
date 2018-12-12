#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: Connector.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Candles.Compression;
	using StockSharp.Algo.Commissions;
	using StockSharp.Algo.Latency;
	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Risk;
	using StockSharp.Algo.Slippage;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The class to create connections to trading systems.
	/// </summary>
	public partial class Connector : BaseLogReceiver, IConnector, ICandleManager
	{
		private static readonly MemoryStatisticsValue<Trade> _tradeStat = new MemoryStatisticsValue<Trade>(LocalizedStrings.Ticks);
		private static readonly MemoryStatisticsValue<Connector> _connectorStat = new MemoryStatisticsValue<Connector>(LocalizedStrings.Str1093);
		private static readonly MemoryStatisticsValue<Message> _messageStat = new MemoryStatisticsValue<Message>(LocalizedStrings.Str1094);

		static Connector()
		{
			MemoryStatistics.Instance.Values.Add(_tradeStat);
			MemoryStatistics.Instance.Values.Add(_connectorStat);
			MemoryStatistics.Instance.Values.Add(_messageStat);
		}

		private class MarketDepthInfo : RefTriple<MarketDepth, IEnumerable<QuoteChange>, IEnumerable<QuoteChange>>
		{
			public MarketDepthInfo(MarketDepth depth)
				: base(depth, null, null)
			{
			}

			public bool HasChanges => Second != null;
		}

		private readonly EntityCache _entityCache = new EntityCache();

		private readonly SynchronizedDictionary<Tuple<Security, bool>, MarketDepthInfo> _marketDepths = new SynchronizedDictionary<Tuple<Security, bool>, MarketDepthInfo>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedByIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedByTransactionIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, List<ExecutionMessage>> _nonAssociatedByStringIdMyTrades = new Dictionary<string, List<ExecutionMessage>>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedOrderIds = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, List<ExecutionMessage>> _nonAssociatedStringOrderIds = new Dictionary<string, List<ExecutionMessage>>();
		//private readonly MultiDictionary<Tuple<long?, string>, RefPair<Order, Action<Order, Order>>> _orderStopOrderAssociations = new MultiDictionary<Tuple<long?, string>, RefPair<Order, Action<Order, Order>>>(false);

		private readonly List<Security> _lookupResult = new List<Security>();
		private readonly SynchronizedQueue<SecurityLookupMessage> _lookupQueue = new SynchronizedQueue<SecurityLookupMessage>();
		private readonly SynchronizedDictionary<long, SecurityLookupMessage> _securityLookups = new SynchronizedDictionary<long, SecurityLookupMessage>();
		private readonly SynchronizedDictionary<long, PortfolioLookupMessage> _portfolioLookups = new SynchronizedDictionary<long, PortfolioLookupMessage>();
		private readonly SynchronizedDictionary<long, BoardLookupMessage> _boardLookups = new SynchronizedDictionary<long, BoardLookupMessage>();

		private readonly SubscriptionManager _subscriptionManager;

		private readonly SynchronizedDictionary<ExchangeBoard, SessionStates> _boardStates = new SynchronizedDictionary<ExchangeBoard, SessionStates>();
		private readonly SynchronizedDictionary<Security, object[]> _securityValues = new SynchronizedDictionary<Security, object[]>();

		private bool _isDisposing;

		/// <summary>
		/// Initializes a new instance of the <see cref="Connector"/>.
		/// </summary>
		public Connector()
			: this(true)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Connector"/>.
		/// </summary>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		/// <param name="snapshotRegistry">Snapshot storage registry.</param>
		/// <param name="initManagers">Initialize managers.</param>
		/// <param name="supportOffline">Use <see cref="OfflineMessageAdapter"/>.</param>
		/// <param name="supportSubscriptionTracking">Use <see cref="SubscriptionMessageAdapter"/>.</param>
		/// <param name="isRestoreSubscriptionOnReconnect">Restore subscription on reconnect.</param>
		public Connector(IEntityRegistry entityRegistry, IStorageRegistry storageRegistry, SnapshotRegistry snapshotRegistry, bool initManagers = true,
			bool supportOffline = false, bool supportSubscriptionTracking = false, bool isRestoreSubscriptionOnReconnect = true)
			: this(false, true, initManagers, supportOffline, supportSubscriptionTracking, isRestoreSubscriptionOnReconnect)
		{
			InitializeStorage(entityRegistry, storageRegistry, snapshotRegistry);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Connector"/>.
		/// </summary>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="positionStorage">Position storage.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		/// <param name="snapshotRegistry">Snapshot storage registry.</param>
		/// <param name="initManagers">Initialize managers.</param>
		/// <param name="supportOffline">Use <see cref="OfflineMessageAdapter"/>.</param>
		/// <param name="supportSubscriptionTracking">Use <see cref="SubscriptionMessageAdapter"/>.</param>
		/// <param name="isRestoreSubscriptionOnReconnect">Restore subscription on reconnect.</param>
		public Connector(ISecurityStorage securityStorage, IPositionStorage positionStorage, IStorageRegistry storageRegistry, SnapshotRegistry snapshotRegistry, bool initManagers = true,
			bool supportOffline = false, bool supportSubscriptionTracking = false, bool isRestoreSubscriptionOnReconnect = true)
			: this(false, true, initManagers, supportOffline, supportSubscriptionTracking, isRestoreSubscriptionOnReconnect)
		{
			InitializeStorage(securityStorage, positionStorage, storageRegistry, snapshotRegistry);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Connector"/>.
		/// </summary>
		/// <param name="initAdapter">Initialize basket adapter.</param>
		/// <param name="initChannels">Initialize channels.</param>
		/// <param name="initManagers">Initialize managers.</param>
		/// <param name="supportOffline">Use <see cref="OfflineMessageAdapter"/>.</param>
		/// <param name="supportSubscriptionTracking">Use <see cref="SubscriptionMessageAdapter"/>.</param>
		/// <param name="isRestoreSubscriptionOnReconnect">Restore subscription on reconnect.</param>
		protected Connector(bool initAdapter, bool initChannels = true, bool initManagers = true,
			bool supportOffline = false, bool supportSubscriptionTracking = false, bool isRestoreSubscriptionOnReconnect = true)
		{
			_entityCache.ExchangeInfoProvider = new InMemoryExchangeInfoProvider();

			_supportOffline = supportOffline;
			_supportSubscriptionTracking = supportSubscriptionTracking;
			ReConnectionSettings = new ReConnectionSettings();

			_subscriptionManager = new SubscriptionManager(this);

			UpdateSecurityLastQuotes = UpdateSecurityByLevel1 = UpdateSecurityByDefinition = true;

			CreateDepthFromLevel1 = true;
			SupportFilteredMarketDepth = true;

			if (initManagers)
			{
				//PnLManager = new PnLManager();
				RiskManager = new RiskManager();
			}

			_connectorStat.Add(this);

			if (initChannels)
			{
				InMessageChannel = new InMemoryMessageChannel("Connector In", RaiseError);
				OutMessageChannel = new InMemoryMessageChannel("Connector Out", RaiseError);
			}

			IsRestoreSubscriptionOnReconnect = isRestoreSubscriptionOnReconnect;

			if (initAdapter)
				InitAdapter();
		}

		/// <summary>
		/// Initialize <see cref="StorageAdapter"/>.
		/// </summary>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		/// <param name="snapshotRegistry">Snapshot storage registry.</param>
		public void InitializeStorage(IEntityRegistry entityRegistry, IStorageRegistry storageRegistry, SnapshotRegistry snapshotRegistry)
		{
			if (entityRegistry == null)
				throw new ArgumentNullException(nameof(entityRegistry));

			EntityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));
			InitializeStorage(entityRegistry.Securities, entityRegistry.PositionStorage, storageRegistry, snapshotRegistry);
		}

		/// <summary>
		/// Initialize <see cref="StorageAdapter"/>.
		/// </summary>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="positionStorage">Position storage.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		/// <param name="snapshotRegistry">Snapshot storage registry.</param>
		public void InitializeStorage(ISecurityStorage securityStorage, IPositionStorage positionStorage, IStorageRegistry storageRegistry, SnapshotRegistry snapshotRegistry)
		{
			SecurityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
			PositionStorage = positionStorage ?? throw new ArgumentNullException(nameof(positionStorage));
			StorageRegistry = storageRegistry ?? throw new ArgumentNullException(nameof(storageRegistry));
			SnapshotRegistry = snapshotRegistry ?? throw new ArgumentNullException(nameof(snapshotRegistry));

			_entityCache.ExchangeInfoProvider = storageRegistry.ExchangeInfoProvider;

			InitAdapter();
		}

		/// <summary>
		/// The storage of trade objects.
		/// </summary>
		public IEntityRegistry EntityRegistry { get; private set; }

		/// <summary>
		/// Securities meta info storage.
		/// </summary>
		public ISecurityStorage SecurityStorage { get; private set; }

		/// <summary>
		/// Position storage.
		/// </summary>
		public IPositionStorage PositionStorage { get; private set; }

		/// <summary>
		/// The storage of market data.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; private set; }

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

		private void InitAdapter()
		{
			Adapter = new BasketMessageAdapter(new MillisecondIncrementalIdGenerator(), new InMemoryMessageAdapterProvider(), new CandleBuilderProvider(_entityCache.ExchangeInfoProvider));
		}

		/// <summary>
		/// Settings of the connection control <see cref="IConnector"/> to the trading system.
		/// </summary>
		public ReConnectionSettings ReConnectionSettings { get; }

		/// <summary>
		/// Entity factory (<see cref="Security"/>, <see cref="Order"/> etc.).
		/// </summary>
		public IEntityFactory EntityFactory
		{
			get => _entityCache.EntityFactory;
			set => _entityCache.EntityFactory = value;
		}

		/// <summary>
		/// Number of tick trades for storage. The default is 100000. If the value is set to <see cref="int.MaxValue"/>, the trades will not be deleted. If the value is set to 0, then the trades will not be stored.
		/// </summary>
		public int TradesKeepCount
		{
			get => _entityCache.TradesKeepCount;
			set => _entityCache.TradesKeepCount = value;
		}

		/// <summary>
		/// The number of orders for storage. The default is 1000. If the value is set to <see cref="int.MaxValue"/>, then the orders will not be deleted. If the value is set to 0, then the orders will not be stored.
		/// </summary>
		public int OrdersKeepCount
		{
			get => _entityCache.OrdersKeepCount;
			set => _entityCache.OrdersKeepCount = value;
		}

		/// <summary>
		/// Transaction id generator.
		/// </summary>
		public IdGenerator TransactionIdGenerator
		{
			get => Adapter.TransactionIdGenerator;
			set => Adapter.TransactionIdGenerator = value;
		}

		private SecurityIdGenerator _securityIdGenerator = new SecurityIdGenerator();

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

		/// <summary>
		/// List of all exchange boards, for which instruments are loaded <see cref="IConnector.Securities"/>.
		/// </summary>
		public IEnumerable<ExchangeBoard> ExchangeBoards => _entityCache.ExchangeBoards;

		/// <summary>
		/// List of all loaded instruments. It should be called after event <see cref="IConnector.NewSecurities"/> arisen. Otherwise the empty set will be returned.
		/// </summary>
		public virtual IEnumerable<Security> Securities => _entityCache.Securities;

		int ISecurityProvider.Count => _entityCache.SecurityCount;

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

		/// <summary>
		/// Lookup securities by criteria <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		/// <returns>Found instruments.</returns>
		public virtual IEnumerable<Security> Lookup(Security criteria)
		{
			return Securities.Filter(criteria);
		}

		private DateTimeOffset _currentTime;

		/// <summary>
		/// Current time, which will be passed to the <see cref="LogMessage.Time"/>.
		/// </summary>
		public override DateTimeOffset CurrentTime => _currentTime;

		/// <summary>
		/// Get session state for required board.
		/// </summary>
		/// <param name="board">Electronic board.</param>
		/// <returns>Session state. If the information about session state does not exist, then <see langword="null" /> will be returned.</returns>
		public SessionStates? GetSessionState(ExchangeBoard board)
		{
			return _boardStates.TryGetValue2(board);
		}

		/// <summary>
		/// Get all orders.
		/// </summary>
		public IEnumerable<Order> Orders => _entityCache.Orders;

		/// <summary>
		/// Get all stop-orders.
		/// </summary>
		public IEnumerable<Order> StopOrders
		{
			get { return Orders.Where(o => o.Type == OrderTypes.Conditional); }
		}

		/// <summary>
		/// Get all registration errors.
		/// </summary>
		public IEnumerable<OrderFail> OrderRegisterFails => _entityCache.OrderRegisterFails;

		/// <summary>
		/// Get all cancellation errors.
		/// </summary>
		public IEnumerable<OrderFail> OrderCancelFails => _entityCache.OrderCancelFails;

		/// <summary>
		/// Get all tick trades.
		/// </summary>
		public IEnumerable<Trade> Trades => _entityCache.Trades;

		/// <summary>
		/// Get all own trades.
		/// </summary>
		public IEnumerable<MyTrade> MyTrades => _entityCache.MyTrades;

		/// <summary>
		/// Get all portfolios.
		/// </summary>
		public virtual IEnumerable<Portfolio> Portfolios => _entityCache.Portfolios;

		/// <summary>
		/// Get all positions.
		/// </summary>
		public IEnumerable<Position> Positions => _entityCache.Positions;

		/// <summary>
		/// All news.
		/// </summary>
		public IEnumerable<News> News => _entityCache.News;

		/// <summary>
		/// Risk control manager.
		/// </summary>
		public IRiskManager RiskManager { get; set; }

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

		/// <summary>
		/// Connection state.
		/// </summary>
		public ConnectionStates ConnectionState { get; private set; }

		///// <summary>
		///// Gets a value indicating whether the re-registration orders via the method <see cref="ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/> as a single transaction. The default is enabled.
		///// </summary>
		//public virtual bool IsSupportAtomicReRegister { get; protected set; } = true;

		/// <summary>
		/// Use orders log to create market depths. Disabled by default.
		/// </summary>
		[Obsolete("Use MarketDataMessage.BuildFrom=OrderLog instead.")]
		public virtual bool CreateDepthFromOrdersLog { get; set; }

		/// <summary>
		/// Use orders log to create ticks. Disabled by default.
		/// </summary>
		[Obsolete("Use MarketDataMessage.BuildFrom=OrderLog instead.")]
		public virtual bool CreateTradesFromOrdersLog { get; set; }

		/// <summary>
		/// To update <see cref="Security.LastTrade"/>, <see cref="Security.BestBid"/>, <see cref="Security.BestAsk"/> at each update of order book and/or trades. By default is enabled.
		/// </summary>
		public bool UpdateSecurityLastQuotes { get; set; }

		/// <summary>
		/// To update <see cref="Security"/> fields when the <see cref="Level1ChangeMessage"/> message appears. By default is enabled.
		/// </summary>
		public bool UpdateSecurityByLevel1 { get; set; }

		/// <summary>
		/// To update <see cref="Security"/> fields when the <see cref="SecurityMessage"/> message appears. By default is enabled.
		/// </summary>
		public bool UpdateSecurityByDefinition { get; set; }

		/// <summary>
		/// To update the order book for the instrument when the <see cref="Level1ChangeMessage"/> message appears. By default is enabled.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str200Key)]
		[DescriptionLoc(LocalizedStrings.Str201Key)]
		public bool CreateDepthFromLevel1
		{
			get => SupportLevel1DepthBuilder;
			set => SupportLevel1DepthBuilder = value;
		}

		/// <summary>
		/// Create a combined security for securities from different boards.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str197Key)]
		[DescriptionLoc(LocalizedStrings.Str198Key)]
		public bool CreateAssociatedSecurity
		{
			get => SupportAssociatedSecurity;
			set => SupportAssociatedSecurity = value;
		}

		/// <summary>
		/// The number of errors passed through the <see cref="Connector.Error"/> event.
		/// </summary>
		public int ErrorCount { get; private set; }

		///// <summary>
		///// Временной сдвиг от текущего времени. Используется в случае, если сервер брокера самостоятельно
		///// указывает сдвиг во времени.
		///// </summary>
		//public TimeSpan? TimeShift { get; private set; }

		private TimeSpan _marketTimeChangedInterval = TimeSpan.FromMilliseconds(10);

		/// <summary>
		/// The <see cref="TimeMessage"/> message generating Interval. The default is 10 milliseconds.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
		[DescriptionLoc(LocalizedStrings.Str195Key)]
		public virtual TimeSpan MarketTimeChangedInterval
		{
			get => _marketTimeChangedInterval;
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str196);

				_marketTimeChangedInterval = value;
			}
		}

		/// <summary>
		/// Increment periodically <see cref="MarketTimeChangedInterval"/> value of <see cref="CurrentTime"/>.
		/// </summary>
		public bool TimeChange { get; set; } = true;

		private bool _isRestoreSubscriptionOnReconnect;

		/// <summary>
		/// Restore subscription on reconnect.
		/// </summary>
		public bool IsRestoreSubscriptionOnReconnect
		{
			get => _isRestoreSubscriptionOnReconnect;
			set
			{
				_isRestoreSubscriptionOnReconnect = value;

				if (Adapter != null)
					Adapter.IsRestoreSubscriptionOnReconnect = value;
			}
		}

		/// <summary>
		/// Connect to trading system.
		/// </summary>
		public void Connect()
		{
			this.AddInfoLog("Connect");

			try
			{
				if (ConnectionState != ConnectionStates.Disconnected && ConnectionState != ConnectionStates.Failed)
				{
					this.AddWarningLog(LocalizedStrings.Str1095Params, ConnectionState);
					return;
				}

				ConnectionState = ConnectionStates.Connecting;

				foreach (var adapter in Adapter.InnerAdapters.SortedAdapters)
				{
					_adapterStates[adapter] = ConnectionStates.Connecting;
				}

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

			SendInMessage(new ConnectMessage());
		}

		/// <summary>
		/// Disconnect from trading system.
		/// </summary>
		public void Disconnect()
		{
			this.AddInfoLog("Disconnect");

			if (ConnectionState != ConnectionStates.Connected)
			{
				this.AddWarningLog(LocalizedStrings.Str1096Params, ConnectionState);
				return;
			}

			ConnectionState = ConnectionStates.Disconnecting;

			foreach (var adapter in Adapter.InnerAdapters.SortedAdapters)
			{
				var prevState = _adapterStates.TryGetValue2(adapter);

				if (prevState != ConnectionStates.Failed)
					_adapterStates[adapter] = ConnectionStates.Disconnecting;
			}

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
			SendInMessage(new DisconnectMessage());
		}

		/// <inheritdoc />
		public void LookupSecurities(Security criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var boardCode = criteria.Board?.Code;
			var securityCode = criteria.Code;

			if (!criteria.Id.IsEmpty())
			{
				var id = SecurityIdGenerator.Split(criteria.Id);

				if (boardCode.IsEmpty())
					boardCode = GetBoardCode(id.BoardCode);

				if (securityCode.IsEmpty())
					securityCode = id.SecurityCode;
			}

			var message = criteria.ToLookupMessage(criteria.ExternalId.ToSecurityId(securityCode, boardCode, criteria.Type));
			message.TransactionId = TransactionIdGenerator.GetNextId();
			message.Adapter = adapter;
			message.OfflineMode = offlineMode;

			LookupSecurities(message);
		}

		/// <inheritdoc />
		public virtual void LookupSecurities(SecurityLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			//если для критерия указаны код биржи и код инструмента, то сначала смотрим нет ли такого инструмента
			if (!NeedLookupSecurities(criteria.SecurityId))
			{
				_securityLookups.Add(criteria.TransactionId, (SecurityLookupMessage)criteria.Clone());
				SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = criteria.TransactionId });
				return;
			}

			lock (_lookupQueue.SyncRoot)
			{
				_lookupQueue.Enqueue(criteria);

				if (_lookupQueue.Count == 1)
					SendInMessage(criteria);
			}
		}

		private bool NeedLookupSecurities(SecurityId securityId)
		{
			if (securityId.SecurityCode.IsEmpty() || securityId.BoardCode.IsEmpty())
				return true;

			var id = SecurityIdGenerator.GenerateId(securityId.SecurityCode, securityId.BoardCode);

			var security = Securities.FirstOrDefault(s => s.Id.CompareIgnoreCase(id));

			return security == null;
		}

		/// <inheritdoc />
		public void LookupOrders(Order criteria, IMessageAdapter adapter = null)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			LookupOrders(new OrderStatusMessage
			{
				TransactionId = transactionId,
				Adapter = adapter,
			});
		}

		/// <inheritdoc />
		public virtual void LookupOrders(OrderStatusMessage criteria)
		{
			_entityCache.AddOrderStatusTransactionId(criteria.TransactionId);
			SendInMessage(criteria);
		}

		/// <inheritdoc />
		public void LookupPortfolios(Portfolio criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var msg = new PortfolioLookupMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				BoardCode = criteria.Board?.Code,
				Currency = criteria.Currency,
				PortfolioName = criteria.Name,
				Adapter = adapter,
				OfflineMode = offlineMode,
			};

			LookupPortfolios(msg);
		}

		/// <inheritdoc />
		public virtual void LookupPortfolios(PortfolioLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			_portfolioLookups.Add(criteria.TransactionId, criteria);

			SendInMessage(criteria);
		}

		/// <inheritdoc />
		public void LookupBoards(ExchangeBoard criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var msg = new BoardLookupMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				Like = criteria.Code,
				Adapter = adapter,
				OfflineMode = offlineMode,
			};

			LookupBoards(msg);
		}

		/// <inheritdoc />
		public void LookupBoards(BoardLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			_boardLookups.Add(criteria.TransactionId, criteria);

			SendInMessage(criteria);
		}

		/// <summary>
		/// To get the position by portfolio and instrument.
		/// </summary>
		/// <param name="portfolio">The portfolio on which the position should be found.</param>
		/// <param name="security">The instrument on which the position should be found.</param>
		/// <param name="clientCode">The client code.</param>
		/// <param name="depoName">The depository name where the stock is located physically. By default, an empty string is passed, which means the total position by all depositories.</param>
		/// <returns>Position.</returns>
		public Position GetPosition(Portfolio portfolio, Security security, string clientCode = "", string depoName = "")
		{
			return GetPosition(portfolio, security, clientCode, depoName, null, string.Empty);
		}

		private Position GetPosition(Portfolio portfolio, Security security, string clientCode, string depoName, TPlusLimits? limitType, string description)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var position = _entityCache.TryAddPosition(portfolio, security, clientCode, depoName, limitType, description, out var isNew);

			if (isNew)
				RaiseNewPosition(position);

			return position;
		}

		private MarketDepth GetMarketDepth(Security security, bool isFiltered)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			MarketDepthInfo info;

			var isNew = false;

			lock (_marketDepths.SyncRoot)
			{
				var key = Tuple.Create(security, isFiltered);

				if (!_marketDepths.TryGetValue(key, out info))
				{
					isNew = true;

					info = new MarketDepthInfo(EntityFactory.CreateMarketDepth(security));

					// стакан из лога заявок бесконечен
					//if (CreateDepthFromOrdersLog)
					//	info.First.MaxDepth = int.MaxValue;

					_marketDepths.Add(key, info);
				}
				else
				{
					if (info.HasChanges)
					{
						new QuoteChangeMessage
						{
							LocalTime = info.First.LocalTime,
							ServerTime = info.First.LastChangeTime,
							Bids = info.Second,
							Asks = info.Third
						}.ToMarketDepth(info.First, GetSecurity);

						info.Second = null;
						info.Third = null;
					}
				}
			}

			if (isNew && !isFiltered)
				RaiseNewMarketDepth(info.First);

			return info.First;
		}

		/// <summary>
		/// To get the quotes order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Order book.</returns>
		public MarketDepth GetMarketDepth(Security security)
		{
			return GetMarketDepth(security, false);
		}

		/// <summary>
		/// Get filtered order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Filtered order book.</returns>
		public MarketDepth GetFilteredMarketDepth(Security security)
		{
			return GetMarketDepth(security, true);
		}

		/// <summary>
		/// Register new order.
		/// </summary>
		/// <param name="order">Registration details.</param>
		public void RegisterOrder(Order order)
		{
			RegisterOrder(order, true);
		}

		private void RegisterOrder(Order order, bool initOrder)
		{
			try
			{
				this.AddOrderInfoLog(order, "RegisterOrder");

				if (initOrder)
				{
					CheckOnNew(order, order.Type != OrderTypes.Conditional);

					if (order.Type == null)
						order.Type = order.Price > 0 ? OrderTypes.Limit : OrderTypes.Market;

					InitNewOrder(order);
				}

				OnRegisterOrder(order);
			}
			catch (Exception ex)
			{
				SendOrderFailed(order, ex, order.TransactionId);
			}
		}

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="oldOrder">Changing order.</param>
		/// <param name="price">Price of the new order.</param>
		/// <param name="volume">Volume of the new order.</param>
		/// <returns>New order.</returns>
		/// <remarks>
		/// If the volume is not set, only the price changes.
		/// </remarks>
		public Order ReRegisterOrder(Order oldOrder, decimal price, decimal volume = 0)
		{
			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			var newOrder = oldOrder.ReRegisterClone(price, volume);
			ReRegisterOrder(oldOrder, newOrder);
			return newOrder;
		}

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="oldOrder">Cancelling order.</param>
		/// <param name="newOrder">New order to register.</param>
		public void ReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			if (newOrder == null)
				throw new ArgumentNullException(nameof(newOrder));

			try
			{
				if (oldOrder.Security != newOrder.Security)
					throw new ArgumentException(LocalizedStrings.Str1098Params.Put(newOrder.Security.Id, oldOrder.Security.Id), nameof(newOrder));

				if (oldOrder.Type == OrderTypes.Conditional)
				{
					CancelOrder(oldOrder);
					RegisterOrder(newOrder);
				}
				else
				{
					CheckOnOld(oldOrder);
					CheckOnNew(newOrder, false);

					if (oldOrder.Comment.IsEmpty())
						oldOrder.Comment = newOrder.Comment;

					InitNewOrder(newOrder);
					_entityCache.AddOrderByCancelationId(oldOrder, newOrder.TransactionId);

					OnReRegisterOrder(oldOrder, newOrder);
				}
			}
			catch (Exception ex)
			{
				SendOrderFailed(oldOrder, ex, newOrder.TransactionId);
				SendOrderFailed(newOrder, ex, newOrder.TransactionId);
			}
		}

		/// <summary>
		/// Reregister of pair orders.
		/// </summary>
		/// <param name="oldOrder1">First order to cancel.</param>
		/// <param name="newOrder1">First new order to register.</param>
		/// <param name="oldOrder2">Second order to cancel.</param>
		/// <param name="newOrder2">Second new order to register.</param>
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
					throw new ArgumentException(LocalizedStrings.Str1099Params.Put(newOrder1.Security.Id, oldOrder1.Security.Id), nameof(newOrder1));

				if (oldOrder2.Security != newOrder2.Security)
					throw new ArgumentException(LocalizedStrings.Str1100Params.Put(newOrder2.Security.Id, oldOrder2.Security.Id), nameof(newOrder2));

				if (oldOrder1.Type == OrderTypes.Conditional || oldOrder2.Type == OrderTypes.Conditional)
				{
					CancelOrder(oldOrder1);
					RegisterOrder(newOrder1);

					CancelOrder(oldOrder2);
					RegisterOrder(newOrder2);
				}
				else
				{
					CheckOnOld(oldOrder1);
					CheckOnNew(newOrder1, false);

					CheckOnOld(oldOrder2);
					CheckOnNew(newOrder2, false);

					if (oldOrder1.Comment.IsEmpty())
						oldOrder1.Comment = newOrder1.Comment;

					if (oldOrder2.Comment.IsEmpty())
						oldOrder2.Comment = newOrder2.Comment;

					InitNewOrder(newOrder1);
					InitNewOrder(newOrder2);

					_entityCache.AddOrderByCancelationId(oldOrder1, newOrder1.TransactionId);
					_entityCache.AddOrderByCancelationId(oldOrder2, newOrder2.TransactionId);

					OnReRegisterOrderPair(oldOrder1, newOrder1, oldOrder2, newOrder2);
				}
			}
			catch (Exception ex)
			{
				SendOrderFailed(oldOrder1, ex, newOrder1.TransactionId);
				SendOrderFailed(newOrder1, ex, newOrder1.TransactionId);

				SendOrderFailed(oldOrder2, ex, newOrder2.TransactionId);
				SendOrderFailed(newOrder2, ex, newOrder2.TransactionId);
			}
		}

		/// <summary>
		/// Cancel the order.
		/// </summary>
		/// <param name="order">Order to cancel.</param>
		public void CancelOrder(Order order)
		{
			long transactionId = 0;

			try
			{
				this.AddOrderInfoLog(order, "CancelOrder");

				CheckOnOld(order);

				transactionId = TransactionIdGenerator.GetNextId();
				_entityCache.AddOrderByCancelationId(order, transactionId);

				OnCancelOrder(order, transactionId);
			}
			catch (Exception ex)
			{
				SendOrderFailed(order, ex, transactionId);
			}
		}

		private void SendOrderFailed(Order order, Exception error, long originalTransactionId)
		{
			SendOutMessage(new OrderFail
			{
				Order = order,
				Error = error,
				ServerTime = CurrentTime,
			}.ToMessage(originalTransactionId));
		}

		private static void CheckOnNew(Order order, bool checkVolume = true, bool checkTransactionId = true)
		{
			CheckOrderState(order);

			if (checkVolume)
			{
				if (order.Volume == 0)
					throw new ArgumentException(LocalizedStrings.Str894, nameof(order));

				if (order.Volume < 0)
					throw new ArgumentOutOfRangeException(nameof(order), order.Volume, LocalizedStrings.Str895);
			}

			if (order.Id != null || !order.StringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str896Params.Put(order.Id == null ? order.StringId : order.Id.To<string>()), nameof(order));

			if (!checkTransactionId)
				return;

			if (order.TransactionId != 0)
				throw new ArgumentException(LocalizedStrings.Str897Params.Put(order.TransactionId), nameof(order));

			if (order.State != OrderStates.None)
				throw new ArgumentException(LocalizedStrings.Str898Params.Put(order.State), nameof(order));
		}

		private static void CheckOnOld(Order order)
		{
			CheckOrderState(order);

			if (order.TransactionId == 0 && order.Id == null && order.StringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str899, nameof(order));
		}

		private static void CheckOrderState(Order order)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			if (order.Type == OrderTypes.Conditional && order.Condition == null)
				throw new ArgumentException(LocalizedStrings.Str889, nameof(order));

			if (order.Security == null)
				throw new ArgumentException(LocalizedStrings.Str890, nameof(order));

			if (order.Portfolio == null)
				throw new ArgumentException(LocalizedStrings.Str891, nameof(order));

			if (order.Price < 0)
				throw new ArgumentOutOfRangeException(nameof(order), order.Price, LocalizedStrings.Str892);

			if (order.Price == 0 && (order.Type == OrderTypes.Limit || order.Type == OrderTypes.ExtRepo || order.Type == OrderTypes.Repo || order.Type == OrderTypes.Rps))
				throw new ArgumentException(LocalizedStrings.Str893, nameof(order));
		}

		/// <summary>
		/// Initialize registering order (transaction id etc.).
		/// </summary>
		/// <param name="order">New order.</param>
		private void InitNewOrder(Order order)
		{
			order.Balance = order.Volume;

			if (order.ExtensionInfo == null)
				order.ExtensionInfo = new Dictionary<string, object>();

			if (order.TransactionId == 0)
				order.TransactionId = TransactionIdGenerator.GetNextId();

			//order.Connector = this;

			//if (order.Security is ContinuousSecurity)
			//	order.Security = ((ContinuousSecurity)order.Security).GetSecurity(CurrentTime);

			order.LocalTime = CurrentTime;
			order.State = order.State.CheckModification(OrderStates.Pending);

			_entityCache.AddOrderByRegistrationId(order);

			SendOutMessage(order.ToMessage());
		}

		/// <summary>
		/// Register new order.
		/// </summary>
		/// <param name="order">Registration details.</param>
		protected virtual void OnRegisterOrder(Order order)
		{
			var regMsg = order.CreateRegisterMessage(GetSecurityId(order.Security));

			//var depoName = order.Portfolio.GetValue<string>(nameof(PositionChangeTypes.DepoName));
			//if (depoName != null)
			//	regMsg.AddValue(nameof(PositionChangeTypes.DepoName), depoName);

			SendInMessage(regMsg);
		}

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="oldOrder">Cancelling order.</param>
		/// <param name="newOrder">New order to register.</param>
		protected virtual void OnReRegisterOrder(Order oldOrder, Order newOrder)
		{
			//if (IsSupportAtomicReRegister && oldOrder.Security.Board.IsSupportAtomicReRegister)
			//{
			var replaceMsg = oldOrder.CreateReplaceMessage(newOrder, GetSecurityId(newOrder.Security));
			SendInMessage(replaceMsg);
			//}
			//else
			//{
			//	CancelOrder(oldOrder);
			//	RegisterOrder(newOrder, false);
			//}
		}

		/// <summary>
		/// Reregister of pair orders.
		/// </summary>
		/// <param name="oldOrder1">First order to cancel.</param>
		/// <param name="newOrder1">First new order to register.</param>
		/// <param name="oldOrder2">Second order to cancel.</param>
		/// <param name="newOrder2">Second new order to register.</param>
		protected virtual void OnReRegisterOrderPair(Order oldOrder1, Order newOrder1, Order oldOrder2, Order newOrder2)
		{
			SendInMessage(oldOrder1.CreateReplaceMessage(newOrder1, GetSecurityId(newOrder1.Security), oldOrder2, newOrder2, GetSecurityId(newOrder2.Security)));

			//CancelOrder(oldOrder1);
			//RegisterOrder(newOrder1, false);

			//CancelOrder(oldOrder2);
			//RegisterOrder(newOrder2, false);
		}

		/// <summary>
		/// Cancel the order.
		/// </summary>
		/// <param name="order">Order to cancel.</param>
		/// <param name="transactionId">Order cancellation transaction id.</param>
		protected virtual void OnCancelOrder(Order order, long transactionId)
		{
			decimal? volume;

			switch (TransactionAdapter?.OrderCancelVolumeRequired)
			{
				case null:
					volume = null;
					break;
				case OrderCancelVolumeRequireTypes.Balance:
					volume = order.Balance;
					break;
				case OrderCancelVolumeRequireTypes.Volume:
					volume = order.Volume;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			var cancelMsg = order.CreateCancelMessage(GetSecurityId(order.Security), transactionId, volume);
			SendInMessage(cancelMsg);
		}

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
		public void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null, SecurityTypes? securityType = null, long? transactionId = null)
		{
			if (transactionId == null)
				transactionId = TransactionIdGenerator.GetNextId();

			_entityCache.AddMassCancelationId(transactionId.Value);
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
		protected virtual void OnCancelOrders(long transactionId, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null, SecurityTypes? securityType = null)
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
		public void ChangePassword(string newPassword)
		{
			var msg = new ChangePasswordMessage
			{
				NewPassword = newPassword.To<SecureString>(),
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

			// output messages from adapters goes non ordered
			if (_currentTime > message.LocalTime)
				return;

			_currentTime = message.LocalTime;

			if (_prevTime.IsDefault())
			{
				_prevTime = _currentTime;
				return;
			}

			var diff = _currentTime - _prevTime;

			if (diff >= MarketTimeChangedInterval)
			{
				_prevTime = _currentTime;
				RaiseMarketTimeChanged(diff);
			}
		}

		/// <summary>
		/// Get security by code.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>Security.</returns>
		protected Security GetSecurity(SecurityId securityId)
		{
			return GetSecurity(CreateSecurityId(securityId.SecurityCode, securityId.BoardCode), s => false);
		}

		/// <summary>
		/// To get the instrument by the code. If the instrument is not found, then the <see cref="IEntityFactory.CreateSecurity"/> is called to create an instrument.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <param name="changeSecurity">The handler changing the instrument. It returns <see langword="true" /> if the instrument has been changed and the <see cref="IConnector.SecuritiesChanged"/> should be called.</param>
		/// <returns>Security.</returns>
		private Security GetSecurity(string id, Func<Security, bool> changeSecurity)
		{
			if (id.IsEmpty())
				throw new ArgumentNullException(nameof(id));

			if (changeSecurity == null)
				throw new ArgumentNullException(nameof(changeSecurity));

			var security = _entityCache.TryAddSecurity(id, idStr =>
			{
				var idInfo = SecurityIdGenerator.Split(idStr);
				return Tuple.Create(idInfo.SecurityCode, _entityCache.ExchangeInfoProvider.GetOrCreateBoard(GetBoardCode(idInfo.BoardCode)));
			}, out var isNew);

			var isChanged = changeSecurity(security);

			if (isNew)
			{
				if (security.Board == null)
					throw new InvalidOperationException(LocalizedStrings.Str903Params.Put(id));

				_entityCache.TryAddBoard(security.Board);
				RaiseNewSecurity(security);
			}
			else if (isChanged)
				RaiseSecurityChanged(security);

			return security;
		}

		/// <summary>
		/// Get <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Security ID.</returns>
		public SecurityId GetSecurityId(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.ToSecurityId(SecurityIdGenerator);
		}

		private string GetBoardCode(string secClass)
		{
			// MarketDataAdapter can be null then loading infos from StorageAdapter.
			return MarketDataAdapter != null ? MarketDataAdapter.GetBoardCode(secClass) : secClass;
		}

		/// <summary>
		/// Generate <see cref="Security.Id"/> security.
		/// </summary>
		/// <param name="secCode">Security code.</param>
		/// <param name="secClass">Security class.</param>
		/// <returns><see cref="Security.Id"/> security.</returns>
		protected string CreateSecurityId(string secCode, string secClass)
		{
			return SecurityIdGenerator.GenerateId(secCode, GetBoardCode(secClass));
		}

		/// <summary>
		/// To get the value of market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="field">Market-data field.</param>
		/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var values = _securityValues.TryGetValue(security);
			return values?[(int)field];
		}

		/// <summary>
		/// To get a set of available fields <see cref="Level1Fields"/>, for which there is a market data for the instrument.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Possible fields.</returns>
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var values = _securityValues.TryGetValue(security);

			if (values == null)
				return Enumerable.Empty<Level1Fields>();

			var fields = new List<Level1Fields>(30);

			for (var i = 0; i < values.Length; i++)
			{
				if (values[i] != null)
					fields.Add((Level1Fields)i);
			}

			return fields;
		}

		private object[] GetSecurityValues(Security security)
		{
			return _securityValues.SafeAdd(security, key => new object[Enumerator.GetValues<Level1Fields>().Count()]);
		}

		/// <summary>
		/// Clear cache.
		/// </summary>
		public virtual void ClearCache()
		{
			_entityCache.Clear();
			_prevTime = default(DateTimeOffset);
			_currentTime = default(DateTimeOffset);

			_securityLookups.Clear();
			_boardLookups.Clear();
			_portfolioLookups.Clear();

			_lookupQueue.Clear();
			_lookupResult.Clear();

			_marketDepths.Clear();

			_nonAssociatedByIdMyTrades.Clear();
			_nonAssociatedByStringIdMyTrades.Clear();
			_nonAssociatedByTransactionIdMyTrades.Clear();

			_nonAssociatedOrderIds.Clear();
			_nonAssociatedStringOrderIds.Clear();

			ConnectionState = ConnectionStates.Disconnected;

			_adapterStates.Clear();

			_subscriptionManager.ClearCache();

			_securityValues.Clear();
			_boardStates.Clear();

			SendInMessage(new ResetMessage());

			CloseTimer();

			_cleared?.Invoke();
		}

		/// <summary>
		/// To release allocated resources. In particular, to disconnect from the trading system via <see cref="Connector.Disconnect"/>.
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

			_connectorStat.Remove(this);

			//if (ConnectionState == ConnectionStates.Disconnected || ConnectionState == ConnectionStates.Failed)
			//	TransactionAdapter = null;

			//if (ExportState == ConnectionStates.Disconnected || ExportState == ConnectionStates.Failed)
			//	MarketDataAdapter = null;

			SendInMessage(_disposeMessage);

			CloseTimer();
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			TradesKeepCount = storage.GetValue(nameof(TradesKeepCount), TradesKeepCount);
			OrdersKeepCount = storage.GetValue(nameof(OrdersKeepCount), OrdersKeepCount);
			UpdateSecurityLastQuotes = storage.GetValue(nameof(UpdateSecurityLastQuotes), true);
			UpdateSecurityByLevel1 = storage.GetValue(nameof(UpdateSecurityByLevel1), true);
			UpdateSecurityByDefinition = storage.GetValue(nameof(UpdateSecurityByDefinition), true);
			ReConnectionSettings.Load(storage.GetValue<SettingsStorage>(nameof(ReConnectionSettings)));
			OverrideSecurityData = storage.GetValue(nameof(OverrideSecurityData), OverrideSecurityData);

			if (storage.ContainsKey(nameof(RiskManager)))
				RiskManager = storage.GetValue<SettingsStorage>(nameof(RiskManager)).LoadEntire<IRiskManager>();

			Adapter.Load(storage.GetValue<SettingsStorage>(nameof(Adapter)));
			IsRestoreSubscriptionOnReconnect = storage.GetValue(nameof(IsRestoreSubscriptionOnReconnect), IsRestoreSubscriptionOnReconnect);

			//CreateDepthFromOrdersLog = storage.GetValue<bool>(nameof(CreateDepthFromOrdersLog));
			//CreateTradesFromOrdersLog = storage.GetValue<bool>(nameof(CreateTradesFromOrdersLog));
			CreateDepthFromLevel1 = storage.GetValue(nameof(CreateDepthFromLevel1), CreateDepthFromLevel1);

			MarketTimeChangedInterval = storage.GetValue<TimeSpan>(nameof(MarketTimeChangedInterval));
			CreateAssociatedSecurity = storage.GetValue(nameof(CreateAssociatedSecurity), CreateAssociatedSecurity);

			LookupMessagesOnConnect = storage.GetValue(nameof(LookupMessagesOnConnect), LookupMessagesOnConnect);
			AutoPortfoliosSubscribe = storage.GetValue(nameof(AutoPortfoliosSubscribe), AutoPortfoliosSubscribe);

			base.Load(storage);
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			storage.SetValue(nameof(TradesKeepCount), TradesKeepCount);
			storage.SetValue(nameof(OrdersKeepCount), OrdersKeepCount);
			storage.SetValue(nameof(UpdateSecurityLastQuotes), UpdateSecurityLastQuotes);
			storage.SetValue(nameof(UpdateSecurityByLevel1), UpdateSecurityByLevel1);
			storage.SetValue(nameof(UpdateSecurityByDefinition), UpdateSecurityByDefinition);
			storage.SetValue(nameof(ReConnectionSettings), ReConnectionSettings.Save());
			storage.SetValue(nameof(OverrideSecurityData), OverrideSecurityData);

			if (RiskManager != null)
				storage.SetValue(nameof(RiskManager), RiskManager.SaveEntire(false));

			storage.SetValue(nameof(Adapter), Adapter.Save());
			storage.SetValue(nameof(IsRestoreSubscriptionOnReconnect), IsRestoreSubscriptionOnReconnect);

			//storage.SetValue(nameof(CreateDepthFromOrdersLog), CreateDepthFromOrdersLog);
			//storage.SetValue(nameof(CreateTradesFromOrdersLog), CreateTradesFromOrdersLog);
			storage.SetValue(nameof(CreateDepthFromLevel1), CreateDepthFromLevel1);

			storage.SetValue(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval);
			storage.SetValue(nameof(CreateAssociatedSecurity), CreateAssociatedSecurity);

			storage.SetValue(nameof(LookupMessagesOnConnect), LookupMessagesOnConnect);
			storage.SetValue(nameof(AutoPortfoliosSubscribe), AutoPortfoliosSubscribe);

			base.Save(storage);
		}

		#region ICandleManager implementation

		int ICandleSource<Candle>.SpeedPriority => 0;

		event Action<CandleSeries, Candle> ICandleSource<Candle>.Processing
		{
			add => CandleSeriesProcessing += value;
			remove => CandleSeriesProcessing -= value;
		}

		event Action<CandleSeries> ICandleSource<Candle>.Stopped
		{
			add => CandleSeriesStopped += value;
			remove => CandleSeriesStopped -= value;
		}

		IEnumerable<Range<DateTimeOffset>> ICandleSource<Candle>.GetSupportedRanges(CandleSeries series)
			=> Enumerable.Empty<Range<DateTimeOffset>>();

		void ICandleSource<Candle>.Start(CandleSeries series, DateTimeOffset? from, DateTimeOffset? to)
			=> SubscribeCandles(series, from, to);

		void ICandleSource<Candle>.Stop(CandleSeries series) => UnSubscribeCandles(series);

		ICandleManagerContainer ICandleManager.Container { get; } = new CandleManagerContainer();

		IEnumerable<CandleSeries> ICandleManager.Series => SubscribedCandleSeries;

		IList<ICandleSource<Candle>> ICandleManager.Sources => ArrayHelper.Empty<ICandleSource<Candle>>();

		#endregion
	}
}