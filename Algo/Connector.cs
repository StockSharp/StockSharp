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
	public partial class Connector : BaseLogReceiver, IConnector, ICandleManager, ISubscriptionProvider
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

		private class MarketDepthInfo : RefTriple<MarketDepth, QuoteChange[], QuoteChange[]>
		{
			public MarketDepthInfo(MarketDepth depth)
				: base(depth, null, null)
			{
			}

			public bool HasChanges => Second != null;
		}

		private readonly EntityCache _entityCache;

		private readonly SynchronizedDictionary<Tuple<Security, bool>, MarketDepthInfo> _marketDepths = new SynchronizedDictionary<Tuple<Security, bool>, MarketDepthInfo>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedByIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedByTransactionIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, List<ExecutionMessage>> _nonAssociatedByStringIdMyTrades = new Dictionary<string, List<ExecutionMessage>>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedOrderIds = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, List<ExecutionMessage>> _nonAssociatedStringOrderIds = new Dictionary<string, List<ExecutionMessage>>();
		//private readonly MultiDictionary<Tuple<long?, string>, RefPair<Order, Action<Order, Order>>> _orderStopOrderAssociations = new MultiDictionary<Tuple<long?, string>, RefPair<Order, Action<Order, Order>>>(false);

		private readonly HashSet<Security> _lookupResult = new HashSet<Security>();
		private readonly SynchronizedQueue<SecurityLookupMessage> _lookupQueue = new SynchronizedQueue<SecurityLookupMessage>();

		private class LookupInfo<TCriteria, TItem>
			where TCriteria : Message
		{
			public TCriteria Criteria { get; }
			public List<TItem> Items { get; } = new List<TItem>();

			public LookupInfo(TCriteria criteria)
			{
				if (criteria == null)
					throw new ArgumentNullException(nameof(criteria));

				Criteria = (TCriteria)criteria.Clone();
			}
		}

		private readonly SynchronizedDictionary<long, LookupInfo<SecurityLookupMessage, Security>> _securityLookups = new SynchronizedDictionary<long, LookupInfo<SecurityLookupMessage, Security>>();
		private readonly SynchronizedDictionary<long, LookupInfo<PortfolioLookupMessage, Portfolio>> _portfolioLookups = new SynchronizedDictionary<long, LookupInfo<PortfolioLookupMessage, Portfolio>>();
		private readonly SynchronizedDictionary<long, LookupInfo<BoardLookupMessage, ExchangeBoard>> _boardLookups = new SynchronizedDictionary<long, LookupInfo<BoardLookupMessage, ExchangeBoard>>();

		private readonly SubscriptionManager _subscriptionManager;

		private class Level1Info
		{
			public readonly object[] Values = new object[Enumerator.GetValues<Level1Fields>().Count()];
			public bool CanBestQuotes { get; private set; } = true;
			public bool CanLastTrade { get; private set; } = true;

			public void SetValue(Level1Fields field, object value)
			{
				var idx = (int)field;

				if (idx >= Values.Length)
					return;

				Values[idx] = value;
			}

			public object GetValue(Level1Fields field)
			{
				var idx = (int)field;

				if (idx >= Values.Length)
					return null;

				return Values[idx];
			}

			public void ClearBestQuotes()
			{
				if (!CanBestQuotes)
					return;

				foreach (var field in Messages.Extensions.BestBidFields.Cache)
					SetValue(field, null);

				foreach (var field in Messages.Extensions.BestAskFields.Cache)
					SetValue(field, null);

				CanBestQuotes = false;
			}

			public void ClearLastTrade()
			{
				if (!CanLastTrade)
					return;

				foreach (var field in Messages.Extensions.LastTradeFields.Cache)
					SetValue(field, null);

				CanLastTrade = false;
			}
		}

		private readonly SynchronizedDictionary<ExchangeBoard, SessionStates> _boardStates = new SynchronizedDictionary<ExchangeBoard, SessionStates>();
		private readonly SynchronizedDictionary<Security, Level1Info> _securityValues = new SynchronizedDictionary<Security, Level1Info>();

		private readonly CachedSynchronizedDictionary<long, Subscription> _subscriptions = new CachedSynchronizedDictionary<long, Subscription>();

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
			_entityCache = new EntityCache(this)
			{
				ExchangeInfoProvider = new InMemoryExchangeInfoProvider()
			};

			_supportOffline = supportOffline;
			_supportSubscriptionTracking = supportSubscriptionTracking;
			ReConnectionSettings = new ReConnectionSettings();

			_subscriptionManager = new SubscriptionManager(this);

			UpdateSecurityLastQuotes = UpdateSecurityByLevel1 = UpdateSecurityByDefinition = true;

			SupportLevel1DepthBuilder = true;
			SupportFilteredMarketDepth = true;

			if (initManagers)
			{
				//PnLManager = new PnLManager();
				RiskManager = new RiskManager();
			}

			_connectorStat.Add(this);

			if (initChannels)
			{
				InMessageChannel = new InMemoryMessageChannel($"Connector In ({Name})", RaiseError);
				OutMessageChannel = new InMemoryMessageChannel($"Connector Out ({Name})", RaiseError);
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
			Adapter = new BasketMessageAdapter(new MillisecondIncrementalIdGenerator(), new CandleBuilderProvider(_entityCache.ExchangeInfoProvider));
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public IEnumerable<ExchangeBoard> ExchangeBoards => _entityCache.ExchangeBoards;

		/// <inheritdoc />
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

		/// <inheritdoc />
		public virtual IEnumerable<Security> Lookup(SecurityLookupMessage criteria)
		{
			return Securities.Filter(criteria);
		}

		private DateTimeOffset _currentTime;

		/// <inheritdoc />
		public override DateTimeOffset CurrentTime => _currentTime;

		/// <inheritdoc />
		public SessionStates? GetSessionState(ExchangeBoard board)
		{
			return _boardStates.TryGetValue2(board);
		}

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
		public IEnumerable<Trade> Trades => _entityCache.Trades;

		/// <inheritdoc />
		[Obsolete("Use NewMyTrade event to collect data.")]
		public IEnumerable<MyTrade> MyTrades => _entityCache.MyTrades;

		/// <inheritdoc />
		[Obsolete("Use NewNews event to collect data.")]
		public IEnumerable<News> News => _entityCache.News;

		/// <inheritdoc />
		public virtual IEnumerable<Portfolio> Portfolios => _entityCache.Portfolios;

		/// <inheritdoc />
		public IEnumerable<Position> Positions => _entityCache.Positions;

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
		[Obsolete]
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
		[Obsolete]
		public bool CreateAssociatedSecurity
		{
			get => SupportAssociatedSecurity;
			set => SupportAssociatedSecurity = value;
		}

		/// <summary>
		/// The number of errors passed through the <see cref="Connector.Error"/> event.
		/// </summary>
		public int ErrorCount { get; private set; }

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

		/// <inheritdoc />
		public void Connect()
		{
			this.AddInfoLog(nameof(Connect));

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

		/// <inheritdoc />
		public void Disconnect()
		{
			this.AddInfoLog(nameof(Disconnect));

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

			var message = criteria.ToLookupMessage(criteria.ExternalId.ToSecurityId(securityCode, boardCode));
			message.Adapter = adapter;
			message.OfflineMode = offlineMode;

			LookupSecurities(message);
		}

		/// <inheritdoc />
		public void LookupSecurities(SecurityLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (criteria.TransactionId == 0)
				criteria.TransactionId = TransactionIdGenerator.GetNextId();

			this.AddInfoLog("Lookup '{0}' for '{1}'.", criteria, criteria.Adapter);

			_securityLookups.Add(criteria.TransactionId, new LookupInfo<SecurityLookupMessage, Security>(criteria));

			//если для критерия указаны код биржи и код инструмента, то сначала смотрим нет ли такого инструмента
			if (!NeedLookupSecurities(criteria.SecurityId))
			{
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
		[Obsolete("Use SubscribeOrders method.")]
		public void LookupOrders(Order criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			var msg = criteria.ToLookupCriteria();

			msg.Adapter = adapter;
			msg.OfflineMode = offlineMode;

			LookupOrders(msg);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribeOrders method.")]
		public void LookupOrders(OrderStatusMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (criteria.TransactionId == 0)
				criteria.TransactionId = TransactionIdGenerator.GetNextId();

			_entityCache.AddOrderStatusTransactionId(criteria.TransactionId);
			
			this.AddInfoLog("Lookup '{0}' for '{1}'.", criteria, criteria.Adapter);
			SendInMessage(criteria);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribePositions method.")]
		public void LookupPortfolios(Portfolio criteria, IMessageAdapter adapter = null, MessageOfflineModes offlineMode = MessageOfflineModes.None)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var msg = criteria.ToLookupCriteria();

			msg.Adapter = adapter;
			msg.OfflineMode = offlineMode;

			LookupPortfolios(msg);
		}

		/// <inheritdoc />
		[Obsolete("Use SubscribePositions method.")]
		public void LookupPortfolios(PortfolioLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (criteria.TransactionId == 0)
				criteria.TransactionId = TransactionIdGenerator.GetNextId();

			_portfolioLookups.Add(criteria.TransactionId, new LookupInfo<PortfolioLookupMessage, Portfolio>(criteria));

			this.AddInfoLog("Lookup '{0}' for '{1}'.", criteria, criteria.Adapter);
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

			_boardLookups.Add(criteria.TransactionId, new LookupInfo<BoardLookupMessage, ExchangeBoard>(criteria));

			this.AddInfoLog("Lookup '{0}' for '{1}'.", criteria, criteria.Adapter);
			SendInMessage(criteria);
		}

		/// <inheritdoc />
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

		/// <inheritdoc />
		public MarketDepth GetMarketDepth(Security security)
		{
			return GetMarketDepth(security, false);
		}

		/// <inheritdoc />
		public MarketDepth GetFilteredMarketDepth(Security security)
		{
			return GetMarketDepth(security, true);
		}

		/// <inheritdoc />
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
				var transactionId = order.TransactionId;

				if (transactionId == 0 || order.State != OrderStates.None)
					transactionId = TransactionIdGenerator.GetNextId();

				SendOrderFailed(order, false, ex, transactionId);
			}
		}

		/// <inheritdoc />
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
				var transactionId = newOrder.TransactionId;

				if (transactionId == 0 || newOrder.State != OrderStates.None)
					transactionId = TransactionIdGenerator.GetNextId();

				SendOrderFailed(oldOrder, true, ex, transactionId);
				SendOrderFailed(newOrder, false, ex, transactionId);
			}
		}

		/// <summary>
		/// Replace orders.
		/// </summary>
		/// <param name="oldOrder1">Cancelling order.</param>
		/// <param name="newOrder1">New order to register.</param>
		/// <param name="oldOrder2">Cancelling order.</param>
		/// <param name="newOrder2">New order to register.</param>
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
				var transactionId = newOrder1.TransactionId;

				if (transactionId == 0)
					transactionId = TransactionIdGenerator.GetNextId();

				SendOrderFailed(oldOrder1, true, ex, transactionId);
				SendOrderFailed(newOrder1, false, ex, transactionId);

				SendOrderFailed(oldOrder2, true, ex, transactionId);
				SendOrderFailed(newOrder2, false, ex, transactionId);
			}
		}

		/// <inheritdoc />
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
				if (transactionId == 0)
					transactionId = TransactionIdGenerator.GetNextId();

				SendOrderFailed(order, true, ex, transactionId);
			}
		}

		private void SendOrderFailed(Order order, bool isCancel, Exception error, long originalTransactionId)
		{
			var fail = EntityFactory.CreateOrderFail(order, error);
			fail.ServerTime = CurrentTime;

			_entityCache.AddOrderFailById(fail, isCancel, originalTransactionId);

			SendOutMessage(fail.ToMessage(originalTransactionId));
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

			if (order.Price == 0 && order.Type == OrderTypes.Limit)
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

			RaiseOrderInitialized(order);
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

		/// <inheritdoc />
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

		/// <inheritdoc />
		public Security GetSecurity(SecurityId securityId)
		{
			return GetSecurity(CreateSecurityId(securityId.SecurityCode, securityId.BoardCode), s => false, out _);
		}
		/// <summary>
		/// To get the instrument by the code. If the instrument is not found, then the <see cref="IEntityFactory.CreateSecurity"/> is called to create an instrument.
		/// </summary>
		/// <param name="id">Security ID.</param>
		/// <param name="changeSecurity">The handler changing the instrument. It returns <see langword="true" /> if the instrument has been changed and the <see cref="IConnector.SecuritiesChanged"/> should be called.</param>
		/// <param name="isNew">Is newly created.</param>
		/// <returns>Security.</returns>
		private Security GetSecurity(string id, Func<Security, bool> changeSecurity, out bool isNew)
		{
			if (id.IsEmpty())
				throw new ArgumentNullException(nameof(id));

			if (changeSecurity == null)
				throw new ArgumentNullException(nameof(changeSecurity));

			var security = _entityCache.TryAddSecurity(id, idStr =>
			{
				var idInfo = SecurityIdGenerator.Split(idStr);
				return Tuple.Create(idInfo.SecurityCode, _entityCache.ExchangeInfoProvider.GetOrCreateBoard(GetBoardCode(idInfo.BoardCode)));
			}, out isNew);

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

		/// <inheritdoc />
		public SecurityId GetSecurityId(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return security.ToSecurityId(SecurityIdGenerator, copyExtended: true);
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

		/// <inheritdoc />
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			return _securityValues.TryGetValue(security)?.GetValue(field);
		}

		/// <inheritdoc />
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var info = _securityValues.TryGetValue(security);

			if (info == null)
				return Enumerable.Empty<Level1Fields>();

			var fields = new List<Level1Fields>(30);

			for (var i = 0; i < info.Values.Length; i++)
			{
				if (info.Values[i] != null)
					fields.Add((Level1Fields)i);
			}

			return fields;
		}

		private Level1Info GetSecurityValues(Security security)
		{
			return _securityValues.SafeAdd(security, key => new Level1Info());
		}

		/// <summary>
		/// Clear cache.
		/// </summary>
		public virtual void ClearCache()
		{
			_entityCache.Clear();
			_prevTime = default;
			_currentTime = default;

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

			_subscriptions.Clear();

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

		/// <inheritdoc />
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
			SupportLevel1DepthBuilder = storage.GetValue(nameof(SupportLevel1DepthBuilder), SupportLevel1DepthBuilder);

			MarketTimeChangedInterval = storage.GetValue<TimeSpan>(nameof(MarketTimeChangedInterval));
			SupportAssociatedSecurity = storage.GetValue(nameof(SupportAssociatedSecurity), SupportAssociatedSecurity);

			LookupMessagesOnConnect = storage.GetValue(nameof(LookupMessagesOnConnect), LookupMessagesOnConnect);
			AutoPortfoliosSubscribe = storage.GetValue(nameof(AutoPortfoliosSubscribe), AutoPortfoliosSubscribe);

			base.Load(storage);
		}

		/// <inheritdoc />
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
			storage.SetValue(nameof(SupportLevel1DepthBuilder), SupportLevel1DepthBuilder);

			storage.SetValue(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval);
			storage.SetValue(nameof(SupportAssociatedSecurity), SupportAssociatedSecurity);

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

		#region IMessageChannel implementation

		private Action<Message> _newOutMessage;

		event Action<Message> IMessageChannel.NewOutMessage
		{
			add => _newOutMessage += value;
			remove => _newOutMessage -= value;
		}

		bool IMessageChannel.IsOpened => ConnectionState == ConnectionStates.Connected;

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
}