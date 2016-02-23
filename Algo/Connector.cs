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
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

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

	using Wintellect.PowerCollections;

	/// <summary>
	/// The class to create connections to trading systems.
	/// </summary>
	public partial class Connector : BaseLogReceiver, IConnector
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

		private sealed class FilteredMarketDepthInfo
		{
			private readonly Dictionary<Tuple<Sides, decimal>, Dictionary<long, decimal>> _executions = new Dictionary<Tuple<Sides, decimal>, Dictionary<long, decimal>>();
			private readonly Dictionary<Tuple<Sides, decimal>, decimal> _ownVolumes = new Dictionary<Tuple<Sides, decimal>, decimal>();

			private readonly MarketDepth _depth;

			private QuoteChangeMessage _quote;
			private bool _needUpdate;

			public FilteredMarketDepthInfo(MarketDepth depth)
			{
				if (depth == null)
					throw new ArgumentNullException(nameof(depth));

				_depth = depth;
			}

			public void Init(MarketDepth source, IEnumerable<Order> currentOrders)
			{
				if (source == null)
					throw new ArgumentNullException(nameof(source));

				if (currentOrders == null)
					throw new ArgumentNullException(nameof(currentOrders));

				currentOrders.Select(o => o.ToMessage()).ForEach(Process);

				_depth.Update(Filter(source.Bids), Filter(source.Asks), true, source.LastChangeTime);
			}

			private IEnumerable<Quote> Filter(IEnumerable<Quote> quotes)
			{
				return quotes
					.Select(quote =>
					{
						var res = quote.Clone();
						var key = Tuple.Create(res.OrderDirection, res.Price);

						var own = _ownVolumes.TryGetValue2(key);
						if (own != null)
							res.Volume -= own.Value;

						return res.Volume <= 0 ? null : res;
					})
					.Where(q => q != null);
			}

			public void Process(QuoteChangeMessage message)
			{
				if (message == null)
					throw new ArgumentNullException(nameof(message));

				_quote = new QuoteChangeMessage
				{
					LocalTime = message.LocalTime,
					ServerTime = message.ServerTime,
					ExtensionInfo = message.ExtensionInfo,
					Bids = message.Bids,
					Asks = message.Asks,
					IsSorted = message.IsSorted,
				};
				_needUpdate = true;
			}

			public void Process(ExecutionMessage message)
			{
				if (!message.HasOrderInfo())
					return;

				var key = Tuple.Create(message.Side, message.OrderPrice);

				switch (message.OrderState)
				{
					case OrderStates.Done:
					case OrderStates.Failed:
					{
						var items = _executions.TryGetValue(key);

						if (items == null)
							break;

						items.Remove(message.OriginalTransactionId);

						if (items.Count == 0)
							_executions.Remove(key);

						break;
					}

					case OrderStates.Active:
					{
						if (message.Balance != null)
							_executions.SafeAdd(key)[message.OriginalTransactionId] = message.Balance.Value;

						break;
					}
				}

				if (_executions.ContainsKey(key))
					_ownVolumes[key] = _executions[key].Sum(o => o.Value);
				else
					_ownVolumes.Remove(key);
			}

			public MarketDepth GetDepth()
			{
				if (!_needUpdate)
					return _depth;

				_needUpdate = false;
				_depth.Update(Filter(_quote.Bids.Select(c => c.ToQuote(_depth.Security))), Filter(_quote.Asks.Select(c => c.ToQuote(_depth.Security))), _quote.IsSorted, _quote.ServerTime);
				_depth.LocalTime = _quote.LocalTime;

				return _depth;
			}
		}

		private class MarketDepthInfo : RefTriple<MarketDepth, IEnumerable<QuoteChange>, IEnumerable<QuoteChange>>
		{
			public MarketDepthInfo(MarketDepth depth)
				: base(depth, Enumerable.Empty<QuoteChange>(), Enumerable.Empty<QuoteChange>())
			{
			}

			public bool HasChanges => Second != null;
		}

		private readonly EntityCache _entityCache = new EntityCache();

		private readonly SynchronizedDictionary<Security, MarketDepthInfo> _marketDepths = new SynchronizedDictionary<Security, MarketDepthInfo>();
		private readonly SynchronizedDictionary<Security, FilteredMarketDepthInfo> _filteredMarketDepths = new SynchronizedDictionary<Security, FilteredMarketDepthInfo>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonOrderedByIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonOrderedByTransactionIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, List<ExecutionMessage>> _nonOrderedByStringIdMyTrades = new Dictionary<string, List<ExecutionMessage>>();
		private readonly MultiDictionary<Tuple<long?, string>, RefPair<Order, Action<Order, Order>>> _orderStopOrderAssociations = new MultiDictionary<Tuple<long?, string>, RefPair<Order, Action<Order, Order>>>(false);

		private readonly Dictionary<SecurityId, List<Message>> _suspendedSecurityMessages = new Dictionary<SecurityId, List<Message>>();
		private readonly object _suspendSync = new object();
		private readonly List<Security> _lookupResult = new List<Security>();
		private readonly SynchronizedQueue<SecurityLookupMessage> _lookupQueue = new SynchronizedQueue<SecurityLookupMessage>();
		private readonly SynchronizedDictionary<long, SecurityLookupMessage> _securityLookups = new SynchronizedDictionary<long, SecurityLookupMessage>();
		private readonly SynchronizedDictionary<long, PortfolioLookupMessage> _portfolioLookups = new SynchronizedDictionary<long, PortfolioLookupMessage>();
		
		private readonly SubscriptionManager _subscriptionManager;

		private readonly SynchronizedDictionary<ExchangeBoard, SessionStates> _sessionStates = new SynchronizedDictionary<ExchangeBoard, SessionStates>();
		private readonly SynchronizedDictionary<Security, object[]> _securityValues = new SynchronizedDictionary<Security, object[]>();

		private readonly IEntityRegistry _entityRegistry;
		private readonly IStorageRegistry _storageRegistry;

		private readonly SyncObject _marketTimerSync = new SyncObject();
		private Timer _marketTimer;
		private readonly TimeMessage _marketTimeMessage = new TimeMessage();
		private bool _isMarketTimeHandled;

		private bool _isDisposing;

		/// <summary>
		/// Initializes a new instance of the <see cref="Connector"/>.
		/// </summary>
		public Connector()
		{
			ReConnectionSettings = new ReConnectionSettings();

			_subscriptionManager = new SubscriptionManager(this);

			UpdateSecurityLastQuotes = UpdateSecurityByLevel1 = true;

			CreateDepthFromLevel1 = true;

			LatencyManager = new LatencyManager();
			CommissionManager = new CommissionManager();
			//PnLManager = new PnLManager();
			RiskManager = new RiskManager();
			SlippageManager = new SlippageManager();

			_connectorStat.Add(this);

			InMessageChannel = new InMemoryMessageChannel("Connector In", RaiseError);
			OutMessageChannel = new InMemoryMessageChannel("Connector Out", RaiseError);

			Adapter = new BasketMessageAdapter(new MillisecondIncrementalIdGenerator());
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Connector"/>.
		/// </summary>
		/// <param name="entityRegistry">The storage of trade objects.</param>
		/// <param name="storageRegistry">The storage of market data.</param>
		public Connector(IEntityRegistry entityRegistry, IStorageRegistry storageRegistry)
			: this()
		{
			if (entityRegistry == null)
				throw new ArgumentNullException(nameof(entityRegistry));

			if (storageRegistry == null)
				throw new ArgumentNullException(nameof(storageRegistry));

			_entityRegistry = entityRegistry;
			_storageRegistry = storageRegistry;

			Adapter = new BasketMessageAdapter(new MillisecondIncrementalIdGenerator());
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
			get { return _entityCache.EntityFactory; }
			set { _entityCache.EntityFactory = value; }
		}

		/// <summary>
		/// Number of tick trades for storage. The default is 100000. If the value is set to -1, the trades will not be deleted. If the value is set to 0, then the trades will not be stored.
		/// </summary>
		public int TradesKeepCount
		{
			get { return _entityCache.TradesKeepCount; }
			set { _entityCache.TradesKeepCount = value; }
		}

		/// <summary>
		/// The number of orders for storage. The default is 1000. If the value is set to -1, then the orders will not be deleted. If the value is set to 0, then the orders will not be stored.
		/// </summary>
		public int OrdersKeepCount
		{
			get { return _entityCache.OrdersKeepCount; }
			set { _entityCache.OrdersKeepCount = value; }
		}

		/// <summary>
		/// Transaction id generator.
		/// </summary>
		public IdGenerator TransactionIdGenerator
		{
			get { return Adapter.TransactionIdGenerator; }
			set { Adapter.TransactionIdGenerator = value; }
		}

		private SecurityIdGenerator _securityIdGenerator = new SecurityIdGenerator();

		/// <summary>
		/// The instrument identifiers generator <see cref="Security.Id"/>.
		/// </summary>
		public SecurityIdGenerator SecurityIdGenerator
		{
			get { return _securityIdGenerator; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_securityIdGenerator = value;
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
			add { _added += value; }
			remove { _added -= value; }
		}

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add { }
			remove { }
		}

		private Action _cleared;

		event Action ISecurityProvider.Cleared
		{
			add { _cleared += value; }
			remove { _cleared -= value; }
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

		/// <summary>
		/// Get native id.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Native (internal) trading system security id.</returns>
		public object GetNativeId(Security security)
		{
			return _entityCache.GetNativeId(security);
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
			return _sessionStates.TryGetValue2(board);
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
		/// Orders registration delay calculation manager.
		/// </summary>
		public ILatencyManager LatencyManager { get; set; }

		/// <summary>
		/// The profit-loss manager.
		/// </summary>
		public IPnLManager PnLManager { get; set; }

		/// <summary>
		/// Risk control manager.
		/// </summary>
		public IRiskManager RiskManager { get; set; }

		/// <summary>
		/// The commission calculating manager.
		/// </summary>
		public ICommissionManager CommissionManager { get; set; }

		/// <summary>
		/// Slippage manager.
		/// </summary>
		public ISlippageManager SlippageManager { get; set; }

		/// <summary>
		/// Connection state.
		/// </summary>
		public ConnectionStates ConnectionState { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the re-registration orders via the method <see cref="ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/> as a single transaction. The default is enabled.
		/// </summary>
		public virtual bool IsSupportAtomicReRegister { get; protected set; } = true;

		/// <summary>
		/// Use orders log to create market depths. Disabled by default.
		/// </summary>
		public virtual bool CreateDepthFromOrdersLog { get; set; }

		/// <summary>
		/// Use orders log to create ticks7. Disabled by default.
		/// </summary>
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
		/// To update the order book for the instrument when the <see cref="Level1ChangeMessage"/> message appears. By default is enabled.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str200Key)]
		[DescriptionLoc(LocalizedStrings.Str201Key)]
		public bool CreateDepthFromLevel1 { get; set; }

		/// <summary>
		/// Create a combined security for securities from different boards.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str197Key)]
		[DescriptionLoc(LocalizedStrings.Str198Key)]
		public bool CreateAssociatedSecurity { get; set; }

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
			get { return _marketTimeChangedInterval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str196);

				_marketTimeChangedInterval = value;
			}
		}

		private void TryOpenChannel()
		{
			if (OutMessageChannel.IsOpened)
				return;

			OutMessageChannel.Open();
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

				StartMarketTimer();
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

			_subscriptionManager.Stop();

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

		/// <summary>
		/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="IConnector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">The instrument whose fields will be used as a filter.</param>
		public void LookupSecurities(Security criteria)
		{
			var boardCode = criteria.Board != null ? criteria.Board.Code : string.Empty;
			var securityCode = criteria.Code ?? string.Empty;

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

			LookupSecurities(message);
		}

		/// <summary>
		/// To find instruments that match the filter <paramref name="criteria" />. Found instruments will be passed through the event <see cref="IConnector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">The criterion which fields will be used as a filter.</param>
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

		/// <summary>
		/// To find portfolios that match the filter <paramref name="criteria" />. Found portfolios will be passed through the event <see cref="IConnector.LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">The portfolio which fields will be used as a filter.</param>
		public virtual void LookupPortfolios(Portfolio criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var msg = new PortfolioLookupMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				BoardCode = criteria.Board == null ? null : criteria.Board.Code,
				Currency = criteria.Currency,
				PortfolioName = criteria.Name,
			};

			_portfolioLookups.Add(msg.TransactionId, msg);

			SendInMessage(msg);
		}

		/// <summary>
		/// To get the position by portfolio and instrument.
		/// </summary>
		/// <param name="portfolio">The portfolio on which the position should be found.</param>
		/// <param name="security">The instrument on which the position should be found.</param>
		/// <param name="depoName">The depository name where the stock is located physically. By default, an empty string is passed, which means the total position by all depositories.</param>
		/// <returns>Position.</returns>
		public Position GetPosition(Portfolio portfolio, Security security, string depoName = "")
		{
			return GetPosition(portfolio, security, depoName, TPlusLimits.T0, string.Empty);
		}

		private Position GetPosition(Portfolio portfolio, Security security, string depoName, TPlusLimits? limitType, string description)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (security == null)
				throw new ArgumentNullException(nameof(security));

			bool isNew;
			var position = _entityCache.TryAddPosition(portfolio, security, depoName, limitType, description, out isNew);

			if (isNew)
				RaiseNewPosition(position);

			return position;
		}

		/// <summary>
		/// To get the quotes order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Order book.</returns>
		public MarketDepth GetMarketDepth(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			MarketDepthInfo info;

			var isNew = false;

			lock (_marketDepths.SyncRoot)
			{
				if (!_marketDepths.TryGetValue(security, out info))
				{
					isNew = true;

					info = new MarketDepthInfo(EntityFactory.CreateMarketDepth(security));

					// стакан из лога заявок бесконечен
					if (CreateDepthFromOrdersLog)
						info.First.MaxDepth = int.MaxValue;

					_marketDepths.Add(security, info);
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

			if (isNew)
				RaiseNewMarketDepth(info.First);

			return info.First;
		}

		/// <summary>
		/// Get filtered order book.
		/// </summary>
		/// <param name="security">The instrument by which an order book should be got.</param>
		/// <returns>Filtered order book.</returns>
		public MarketDepth GetFilteredMarketDepth(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (!_subscriptionManager.IsFilteredMarketDepthRegistered(security))
				throw new InvalidOperationException(LocalizedStrings.Str1097Params.Put(security.Id));

			return GetFilteredMarketDepthInfo(security).GetDepth();
		}

		private FilteredMarketDepthInfo GetFilteredMarketDepthInfo(Security security)
		{
			return _filteredMarketDepths.SafeAdd(security, s => new FilteredMarketDepthInfo(EntityFactory.CreateMarketDepth(s)));
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

				CheckOnNew(order, order.Type != OrderTypes.Conditional, initOrder);

				var cs = order.Security as ContinuousSecurity;

				while (cs != null)
				{
					order.Security = cs.GetSecurity(CurrentTime);
					cs = order.Security as ContinuousSecurity;
				}

				if (initOrder)
				{
					if (order.Type == null)
						order.Type = order.Price > 0 ? OrderTypes.Limit : OrderTypes.Market;

					InitNewOrder(order);
				}

				OnRegisterOrder(order);
			}
			catch (Exception ex)
			{
				SendOrderFailed(order, ex);
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
					_entityCache.AddOrderByCancelTransaction(newOrder.TransactionId, oldOrder);

					OnReRegisterOrder(oldOrder, newOrder);
				}
			}
			catch (Exception ex)
			{
				SendOrderFailed(oldOrder, ex);
				SendOrderFailed(newOrder, ex);
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

					_entityCache.AddOrderByCancelTransaction(newOrder1.TransactionId, oldOrder1);
					_entityCache.AddOrderByCancelTransaction(newOrder2.TransactionId, oldOrder2);

					OnReRegisterOrderPair(oldOrder1, newOrder1, oldOrder2, newOrder2);
				}
			}
			catch (Exception ex)
			{
				SendOrderFailed(oldOrder1, ex);
				SendOrderFailed(newOrder1, ex);

				SendOrderFailed(oldOrder2, ex);
				SendOrderFailed(newOrder2, ex);
			}
		}

		/// <summary>
		/// Cancel the order.
		/// </summary>
		/// <param name="order">Order to cancel.</param>
		public void CancelOrder(Order order)
		{
			try
			{
				this.AddOrderInfoLog(order, "CancelOrder");

				CheckOnOld(order);

				var transactionId = TransactionIdGenerator.GetNextId();
				_entityCache.AddOrderByCancelTransaction(transactionId, order);

				//order.InitLatencyMonitoring(false);
				OnCancelOrder(order, transactionId);
			}
			catch (Exception ex)
			{
				SendOrderFailed(order, ex);
			}
		}

		private void SendOrderFailed(Order order, Exception error)
		{
			SendOutMessage(new OrderFail
			{
				Order = order,
				Error = error,
				ServerTime = CurrentTime,
			}.ToMessage());
		}

		private static void CheckOnNew(Order order, bool checkVolume = true, bool checkTransactionId = true)
		{
			ChechOrderState(order);

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
			ChechOrderState(order);

			if (order.TransactionId == 0 && order.Id == null && order.StringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str899, nameof(order));
		}

		private static void ChechOrderState(Order order)
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
				order.ExtensionInfo = new Dictionary<object, object>();

			//order.InitializationTime = trader.MarketTime;
			if (order.TransactionId == 0)
				order.TransactionId = TransactionIdGenerator.GetNextId();

			//order.Connector = this;

			if (order.Security is ContinuousSecurity)
				order.Security = ((ContinuousSecurity)order.Security).GetSecurity(CurrentTime);

			order.LocalTime = CurrentTime;
			order.State = OrderStates.Pending;

			if (!_entityCache.TryAddOrder(order))
				throw new ArgumentException(LocalizedStrings.Str1101Params.Put(order.TransactionId));

			//RaiseNewOrder(order);
			SendOutMessage(order.ToMessage());
		}

		/// <summary>
		/// Register new order.
		/// </summary>
		/// <param name="order">Registration details.</param>
		protected virtual void OnRegisterOrder(Order order)
		{
			var regMsg = order.CreateRegisterMessage(GetSecurityId(order.Security));

			var depoName = order.Portfolio.GetValue<string>(PositionChangeTypes.DepoName);
			if (depoName != null)
				regMsg.AddValue(PositionChangeTypes.DepoName, depoName);

			SendInMessage(regMsg);
		}

		/// <summary>
		/// Reregister the order.
		/// </summary>
		/// <param name="oldOrder">Cancelling order.</param>
		/// <param name="newOrder">New order to register.</param>
		protected virtual void OnReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (IsSupportAtomicReRegister && oldOrder.Security.Board.IsSupportAtomicReRegister)
			{
				var replaceMsg = oldOrder.CreateReplaceMessage(newOrder, GetSecurityId(newOrder.Security));
				SendInMessage(replaceMsg);
			}
			else
			{
				CancelOrder(oldOrder);
				RegisterOrder(newOrder, false);
			}
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
			CancelOrder(oldOrder1);
			RegisterOrder(newOrder1, false);

			CancelOrder(oldOrder2);
			RegisterOrder(newOrder2, false);
		}

		/// <summary>
		/// Cancel the order.
		/// </summary>
		/// <param name="order">Order to cancel.</param>
		/// <param name="transactionId">Order cancellation transaction id.</param>
		protected virtual void OnCancelOrder(Order order, long transactionId)
		{
			var cancelMsg = order.CreateCancelMessage(GetSecurityId(order.Security), transactionId, TransactionAdapter.OrderCancelVolumeRequired ? order.Balance : (decimal?)null);
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
		public void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			var transactionId = TransactionIdGenerator.GetNextId();
			_entityCache.AddOrderByCancelTransaction(transactionId, null);
			OnCancelOrders(transactionId, isStopOrder, portfolio, direction, board, security);
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
		protected virtual void OnCancelOrders(long transactionId, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			var cancelMsg = MessageConverterHelper.CreateGroupCancelMessage(transactionId, isStopOrder, portfolio, direction, board, security == null ? default(SecurityId) : GetSecurityId(security), security);
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

			// output messages from adapters goes asynchronously
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

			bool isNew;

			var security = _entityCache.TryAddSecurity(id, idStr =>
			{
				var idInfo = SecurityIdGenerator.Split(idStr);
				return Tuple.Create(idInfo.SecurityCode, ExchangeBoard.GetOrCreateBoard(GetBoardCode(idInfo.BoardCode)));
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

		/// <summary>
		/// Get <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Security ID.</returns>
		public SecurityId GetSecurityId(Security security)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			var secId = security.ToSecurityId(SecurityIdGenerator);

			lock (_suspendSync)
				secId.Native = GetNativeId(security);

			return secId;
		}

		private void ProcessMyTrades<T>(Order order, T id, Dictionary<T, List<ExecutionMessage>> nonOrderedMyTrades)
		{
			var value = nonOrderedMyTrades.TryGetValue(id);

			if (value == null)
				return;

			var retVal = new List<ExecutionMessage>();

			foreach (var message in value.ToArray())
			{
				// проверяем совпадение по дате, исключая ситуация сопоставления сделки с заявкой, имеющая неуникальный идентификатор
				if (message.ServerTime.Date != order.Time.Date)
					continue;

				retVal.Add(message);
				value.Remove(message);
			}

			if (value.IsEmpty())
				nonOrderedMyTrades.Remove(id);

			var trades = retVal
				.Select(t => _entityCache.ProcessMyTradeMessage(order.Security, t))
				.Where(t => t != null && t.Item2)
				.Select(t => t.Item1);

			foreach (var trade in trades)
			{
				RaiseNewMyTrade(trade);
			}
		}

		/// <summary>
		/// To get the portfolio by the name. If the portfolio is not registered, it is created via <see cref="IEntityFactory.CreatePortfolio"/>.
		/// </summary>
		/// <param name="name">Portfolio name.</param>
		/// <param name="changePortfolio">Portfolio handler.</param>
		/// <returns>Portfolio.</returns>
		private Portfolio GetPortfolio(string name, Func<Portfolio, bool> changePortfolio = null)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			var result = _entityCache.ProcessPortfolio(name, changePortfolio);

			var portfolio = result.Item1;
			var isNew = result.Item2;
			var isChanged = result.Item3;

			if (isNew)
			{
				this.AddInfoLog(LocalizedStrings.Str1105Params, portfolio.Name);
				RaiseNewPortfolio(portfolio);
			}
			else if (isChanged)
				RaisePortfolioChanged(portfolio);

			return portfolio;
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
			return values == null ? null : values[(int)field];
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

			_securityLookups.Clear();
			_portfolioLookups.Clear();

			_lookupQueue.Clear();
			_lookupResult.Clear();

			_marketDepths.Clear();

			_nonOrderedByIdMyTrades.Clear();
			_nonOrderedByStringIdMyTrades.Clear();
			_nonOrderedByTransactionIdMyTrades.Clear();

			ConnectionState = ConnectionStates.Disconnected;

			_adapterStates.Clear();

			_suspendedSecurityMessages.Clear();

			_subscriptionManager.ClearCache();

			_securityValues.Clear();
			_sessionStates.Clear();
			_filteredMarketDepths.Clear();
			_olBuilders.Clear();

			SendInMessage(new ResetMessage());

			_cleared.SafeInvoke();
		}

		/// <summary>
		/// To start the messages generating timer <see cref="TimeMessage"/> with the <see cref="Connector.MarketTimeChangedInterval"/> interval.
		/// </summary>
		protected virtual void StartMarketTimer()
		{
			if (null != _marketTimer)
				return;

			_isMarketTimeHandled = true;

			_marketTimer = ThreadingHelper
				.Timer(() =>
				{
					// TimeMsg required for notify invoke MarketTimeChanged event (and active time based IMarketRule-s)
					// No need to put _marketTimeMessage again, if it still in queue.

					lock (_marketTimerSync)
					{
						if (!_isMarketTimeHandled)
							return;

						_isMarketTimeHandled = false;
					}

					_marketTimeMessage.LocalTime = TimeHelper.Now;
					SendOutMessage(_marketTimeMessage);
				})
				.Interval(MarketTimeChangedInterval);
		}

		/// <summary>
		/// To stop the timer started earlier via <see cref="Connector.StartMarketTimer"/>.
		/// </summary>
		protected void StopMarketTimer()
		{
			if (null == _marketTimer)
				return;

			_marketTimer.Dispose();
			_marketTimer = null;
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

			Adapter = null;
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
			ReConnectionSettings.Load(storage.GetValue<SettingsStorage>(nameof(ReConnectionSettings)));

			if (storage.ContainsKey(nameof(LatencyManager)))
				LatencyManager = storage.GetValue<SettingsStorage>(nameof(LatencyManager)).LoadEntire<ILatencyManager>();

			if (storage.ContainsKey(nameof(CommissionManager)))
				CommissionManager = storage.GetValue<SettingsStorage>(nameof(CommissionManager)).LoadEntire<ICommissionManager>();

			if (storage.ContainsKey(nameof(PnLManager)))
				PnLManager = storage.GetValue<SettingsStorage>(nameof(PnLManager)).LoadEntire<IPnLManager>();

			if (storage.ContainsKey(nameof(SlippageManager)))
				SlippageManager = storage.GetValue<SettingsStorage>(nameof(SlippageManager)).LoadEntire<ISlippageManager>();

			if (storage.ContainsKey(nameof(RiskManager)))
				RiskManager = storage.GetValue<SettingsStorage>(nameof(RiskManager)).LoadEntire<IRiskManager>();

			Adapter.Load(storage.GetValue<SettingsStorage>(nameof(Adapter)));

			CreateDepthFromOrdersLog = storage.GetValue<bool>(nameof(CreateDepthFromOrdersLog));
			CreateTradesFromOrdersLog = storage.GetValue<bool>(nameof(CreateTradesFromOrdersLog));
			CreateDepthFromLevel1 = storage.GetValue(nameof(CreateDepthFromLevel1), CreateDepthFromLevel1);

			MarketTimeChangedInterval = storage.GetValue<TimeSpan>(nameof(MarketTimeChangedInterval));
			CreateAssociatedSecurity = storage.GetValue(nameof(CreateAssociatedSecurity), CreateAssociatedSecurity);

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
			storage.SetValue(nameof(ReConnectionSettings), ReConnectionSettings.Save());

			if (LatencyManager != null)
				storage.SetValue(nameof(LatencyManager), LatencyManager.SaveEntire(false));

			if (CommissionManager != null)
				storage.SetValue(nameof(CommissionManager), CommissionManager.SaveEntire(false));

			if (PnLManager != null)
				storage.SetValue(nameof(PnLManager), PnLManager.SaveEntire(false));

			if (SlippageManager != null)
				storage.SetValue(nameof(SlippageManager), SlippageManager.SaveEntire(false));

			if (RiskManager != null)
				storage.SetValue(nameof(RiskManager), RiskManager.SaveEntire(false));

			storage.SetValue(nameof(Adapter), Adapter.Save());

			storage.SetValue(nameof(CreateDepthFromOrdersLog), CreateDepthFromOrdersLog);
			storage.SetValue(nameof(CreateTradesFromOrdersLog), CreateTradesFromOrdersLog);
			storage.SetValue(nameof(CreateDepthFromLevel1), CreateDepthFromLevel1);

			storage.SetValue(nameof(MarketTimeChangedInterval), MarketTimeChangedInterval);
			storage.SetValue(nameof(CreateAssociatedSecurity), CreateAssociatedSecurity);

			base.Save(storage);
		}
	}
}