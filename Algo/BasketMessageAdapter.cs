namespace StockSharp.Algo;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Latency;
using StockSharp.Algo.PnL;
using StockSharp.Algo.Slippage;
using StockSharp.Algo.Testing;
using StockSharp.Algo.Positions;

/// <summary>
/// The interface describing the list of adapters to trading systems with which the aggregator operates.
/// </summary>
public interface IInnerAdapterList : ISynchronizedCollection<IMessageAdapter>, INotifyList<IMessageAdapter>
{
	/// <summary>
	/// Internal adapters sorted by operation speed.
	/// </summary>
	IEnumerable<IMessageAdapter> SortedAdapters { get; }

	/// <summary>
	/// The indexer through which speed priorities (the smaller the value, then adapter is faster) for internal adapters are set.
	/// </summary>
	/// <param name="adapter">The internal adapter.</param>
	/// <returns>The adapter priority. If the -1 value is set the adapter is considered to be off.</returns>
	int this[IMessageAdapter adapter] { get; set; }
}

/// <summary>
/// Adapter-aggregator that allows simultaneously to operate multiple adapters connected to different trading systems.
/// </summary>
[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BasketKey)]
public class BasketMessageAdapter : BaseLogReceiver, IMessageAdapter
{
	private sealed class InnerAdapterList(BasketMessageAdapter parent) : CachedSynchronizedList<IMessageAdapter>, IInnerAdapterList
	{
		private readonly BasketMessageAdapter _parent = parent ?? throw new ArgumentNullException(nameof(parent));
		private readonly Dictionary<IMessageAdapter, int> _enables = [];

		public IEnumerable<IMessageAdapter> SortedAdapters => Cache.Where(t => this[t] != -1).OrderBy(t => this[t]);

		protected override bool OnAdding(IMessageAdapter item)
		{
			_enables.Add(item, 0);
			return base.OnAdding(item);
		}

		protected override bool OnInserting(int index, IMessageAdapter item)
		{
			_enables.Add(item, 0);
			return base.OnInserting(index, item);
		}

		protected override bool OnRemoving(IMessageAdapter item)
		{
			_enables.Remove(item);

			if (item.Parent == _parent)
				item.Parent = null;

			lock (_parent._connectedResponseLock)
				_parent._adapterStates.Remove(item);

			return base.OnRemoving(item);
		}

		protected override bool OnClearing()
		{
			_enables.Clear();

			_parent._adapterWrappers.CachedKeys.ForEach(a =>
			{
				if (a.Parent == _parent)
					a.Parent = null;
			});
			_parent._adapterWrappers.Clear();

			lock (_parent._connectedResponseLock)
				_parent._adapterStates.Clear();

			return base.OnClearing();
		}

		public int this[IMessageAdapter adapter]
		{
			get
			{
				lock (SyncRoot)
					return _enables.TryGetValue2(adapter) ?? -1;
			}
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				lock (SyncRoot)
				{
					if (!Contains(adapter))
						Add(adapter);

					_enables[adapter] = value;
				}
			}
		}
	}

	private class ParentChildMap
	{
		private readonly SyncObject _syncObject = new();
		private readonly Dictionary<long, RefQuadruple<long, SubscriptionStates, IMessageAdapter, Exception>> _childToParentIds = [];

		public void AddMapping(long childId, ISubscriptionMessage parentMsg, IMessageAdapter adapter)
		{
			if (childId <= 0)
				throw new ArgumentOutOfRangeException(nameof(childId));

			if (parentMsg == null)
				throw new ArgumentNullException(nameof(parentMsg));

			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			lock (_syncObject)
				_childToParentIds.Add(childId, RefTuple.Create(parentMsg.TransactionId, SubscriptionStates.Stopped, adapter, default(Exception)));
		}

		public IDictionary<long, IMessageAdapter> GetChild(long parentId)
		{
			if (parentId <= 0)
				throw new ArgumentOutOfRangeException(nameof(parentId));

			lock (_syncObject)
				return FilterByParent(parentId).Where(p => p.Value.Second.IsActive()).ToDictionary(p => p.Key, p => p.Value.Third);
		}

		private IEnumerable<KeyValuePair<long, RefQuadruple<long, SubscriptionStates, IMessageAdapter, Exception>>> FilterByParent(long parentId) => _childToParentIds.Where(p => p.Value.First == parentId);

		public long? ProcessChildResponse(long childId, Exception error, out bool needParentResponse, out bool allError, out IEnumerable<Exception> innerErrors)
		{
			allError = true;
			needParentResponse = true;
			innerErrors = [];

			if (childId == 0)
				return null;

			lock (_syncObject)
			{
				if (!_childToParentIds.TryGetValue(childId, out var tuple))
					return null;

				var parentId = tuple.First;
				tuple.Second = error == null ? SubscriptionStates.Active : SubscriptionStates.Error;
				tuple.Fourth = error;

				var errors = new List<Exception>();

				foreach (var pair in FilterByParent(parentId))
				{
					var t = pair.Value;

					// one of adapter still not yet response.
					if (t.Second == SubscriptionStates.Stopped)
					{
						needParentResponse = false;
						break;
					}

					if (t.Second != SubscriptionStates.Error)
						allError = false;
					else if (t.Fourth != null)
						errors.Add(t.Fourth);
				}

				innerErrors = errors;
				return parentId;
			}
		}

		public long? ProcessChildFinish(long childId, out bool needParentResponse)
			=> ProcessChild(childId, SubscriptionStates.Finished, out needParentResponse);

		public long? ProcessChildOnline(long childId, out bool needParentResponse)
			=> ProcessChild(childId, SubscriptionStates.Online, out needParentResponse);

		private long? ProcessChild(long childId, SubscriptionStates state, out bool needParentResponse)
		{
			needParentResponse = true;

			lock (_syncObject)
			{
				if (!_childToParentIds.TryGetValue(childId, out var tuple))
					return null;

				var parentId = tuple.First;
				tuple.Second = state;

				foreach (var pair in FilterByParent(parentId))
				{
					var t = pair.Value;

					if (t.Second != SubscriptionStates.Error && t.Second != state)
					{
						needParentResponse = false;
						break;
					}
				}

				return parentId;
			}
		}

		public void Clear()
		{
			lock (_syncObject)
				_childToParentIds.Clear();
		}

		public bool TryGetParent(long childId, out long parentId)
		{
			parentId = default;

			lock (_syncObject)
			{
				if (!_childToParentIds.TryGetValue(childId, out var t))
					return false;

				parentId = t.First;
				return true;
			}
		}
	}

	private readonly Dictionary<long, HashSet<IMessageAdapter>> _nonSupportedAdapters = [];
	private readonly CachedSynchronizedDictionary<IMessageAdapter, IMessageAdapter> _adapterWrappers = [];
	private readonly SyncObject _connectedResponseLock = new();
	private readonly Dictionary<MessageTypes, CachedSynchronizedSet<IMessageAdapter>> _messageTypeAdapters = [];
	private readonly List<Message> _pendingMessages = [];

	private readonly Dictionary<IMessageAdapter, Tuple<ConnectionStates, Exception>> _adapterStates = [];
	private ConnectionStates _currState = ConnectionStates.Disconnected;

	private readonly SynchronizedDictionary<string, IMessageAdapter> _portfolioAdapters = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly SynchronizedDictionary<Tuple<SecurityId, DataType>, IMessageAdapter> _securityAdapters = [];

	private readonly SynchronizedDictionary<long, Tuple<ISubscriptionMessage, IMessageAdapter[], DataType>> _subscription = [];
	private readonly SynchronizedDictionary<long, Tuple<ISubscriptionMessage, IMessageAdapter>> _requestsById = [];
	private readonly ParentChildMap _parentChildMap = new();

	private readonly SynchronizedDictionary<long, IMessageAdapter> _orderAdapters = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketMessageAdapter"/>.
	/// </summary>
	/// <param name="transactionIdGenerator">Transaction id generator.</param>
	/// <param name="candleBuilderProvider">Candle builders provider.</param>
	/// <param name="securityAdapterProvider">The security based message adapter's provider.</param>
	/// <param name="portfolioAdapterProvider">The portfolio based message adapter's provider.</param>
	/// <param name="buffer">Storage buffer.</param>
	public BasketMessageAdapter(IdGenerator transactionIdGenerator,
		CandleBuilderProvider candleBuilderProvider,
		ISecurityMessageAdapterProvider securityAdapterProvider,
		IPortfolioMessageAdapterProvider portfolioAdapterProvider,
		StorageBuffer buffer)
	{
		TransactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
		_innerAdapters = new InnerAdapterList(this);
		SecurityAdapterProvider = securityAdapterProvider ?? throw new ArgumentNullException(nameof(securityAdapterProvider));
		PortfolioAdapterProvider = portfolioAdapterProvider ?? throw new ArgumentNullException(nameof(portfolioAdapterProvider));
		Buffer = buffer;
		StorageProcessor = new StorageProcessor(StorageSettings, candleBuilderProvider);

		LatencyManager = new LatencyManager();
		CommissionManager = new CommissionManager();
		//PnLManager = new PnLManager();
		SlippageManager = new SlippageManager();

		SecurityAdapterProvider.Changed += SecurityAdapterProviderOnChanged;
		PortfolioAdapterProvider.Changed += PortfolioAdapterProviderOnChanged;
	}

	/// <summary>
	/// The portfolio based message adapter's provider.
	/// </summary>
	public IPortfolioMessageAdapterProvider PortfolioAdapterProvider { get; }

	/// <summary>
	/// Storage buffer.
	/// </summary>
	public StorageBuffer Buffer { get; }

	/// <summary>
	/// The security based message adapter's provider.
	/// </summary>
	public ISecurityMessageAdapterProvider SecurityAdapterProvider { get; }

	private readonly InnerAdapterList _innerAdapters;

	/// <summary>
	/// Adapters with which the aggregator operates.
	/// </summary>
	public IInnerAdapterList InnerAdapters => _innerAdapters;

	private INativeIdStorage _nativeIdStorage = new InMemoryNativeIdStorage();

	/// <summary>
	/// Security native identifier storage.
	/// </summary>
	public INativeIdStorage NativeIdStorage
	{
		get => _nativeIdStorage;
		set => _nativeIdStorage = value ?? throw new ArgumentNullException(nameof(value));
	}

	private ISecurityMappingStorage _securityMappingStorage;

	/// <summary>
	/// Security identifier mappings storage.
	/// </summary>
	public ISecurityMappingStorage SecurityMappingStorage
	{
		get => _securityMappingStorage;
		set => _securityMappingStorage = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Extended info storage.
	/// </summary>
	public IExtendedInfoStorage ExtendedInfoStorage { get; set; }

	/// <summary>
	/// Orders registration delay calculation manager.
	/// </summary>
	public ILatencyManager LatencyManager { get; set; }

	/// <summary>
	/// The profit-loss manager.
	/// </summary>
	public IPnLManager PnLManager { get; set; }

	/// <summary>
	/// The commission calculating manager.
	/// </summary>
	public ICommissionManager CommissionManager { get; set; }

	/// <summary>
	/// Slippage manager.
	/// </summary>
	public ISlippageManager SlippageManager { get; set; }

	/// <summary>
	/// Storage settings.
	/// </summary>
	public StorageCoreSettings StorageSettings { get; } = new();

	/// <inheritdoc />
	public bool GenerateOrderBookFromLevel1 { get; set; } = true;

	private IdGenerator _transactionIdGenerator;

	/// <inheritdoc />
	public IdGenerator TransactionIdGenerator
	{
		get => _transactionIdGenerator;
		set => _transactionIdGenerator = value ?? throw new ArgumentNullException(nameof(value));
	}

	IEnumerable<MessageTypeInfo> IMessageAdapter.PossibleSupportedMessages => GetSortedAdapters().SelectMany(a => a.PossibleSupportedMessages).DistinctBy(i => i.Type);

	IEnumerable<MessageTypes> IMessageAdapter.SupportedInMessages
	{
		get => GetSortedAdapters().SelectMany(a => a.SupportedInMessages).Distinct();
		set { }
	}

	IEnumerable<MessageTypes> IMessageAdapter.NotSupportedResultMessages => GetSortedAdapters().SelectMany(a => a.NotSupportedResultMessages).Distinct();

	IEnumerable<DataType> IMessageAdapter.GetSupportedMarketDataTypes(SecurityId securityId, DateTimeOffset? from, DateTimeOffset? to)
		=> GetSortedAdapters().SelectMany(a => a.GetSupportedMarketDataTypes(securityId, from, to)).Distinct();

	IEnumerable<Level1Fields> IMessageAdapter.CandlesBuildFrom => GetSortedAdapters().SelectMany(a => a.CandlesBuildFrom).Distinct();

	bool IMessageAdapter.CheckTimeFrameByRequest => false;

	ReConnectionSettings IMessageAdapter.ReConnectionSettings { get; } = new ReConnectionSettings();

	TimeSpan IMessageAdapter.HeartbeatInterval { get; set; }

	string IMessageAdapter.StorageName => nameof(BasketMessageAdapter).Remove(nameof(MessageAdapter));

	bool IMessageAdapter.IsNativeIdentifiersPersistable => false;

	bool IMessageAdapter.IsNativeIdentifiers => false;

	bool IMessageAdapter.IsFullCandlesOnly => GetSortedAdapters().All(a => a.IsFullCandlesOnly);

	bool IMessageAdapter.IsSupportSubscriptions => true;

	bool IMessageAdapter.IsSupportCandlesUpdates(MarketDataMessage subscription) => GetSortedAdapters().Any(a => a.IsSupportCandlesUpdates(subscription));

	bool IMessageAdapter.IsSupportCandlesPriceLevels(MarketDataMessage subscription) => GetSortedAdapters().Any(a => a.IsSupportCandlesPriceLevels(subscription));

	bool IMessageAdapter.IsSupportPartialDownloading => GetSortedAdapters().Any(a => a.IsSupportPartialDownloading);

	IEnumerable<Tuple<string, Type>> IMessageAdapter.SecurityExtendedFields => GetSortedAdapters().SelectMany(a => a.SecurityExtendedFields).Distinct();

	IEnumerable<int> IMessageAdapter.SupportedOrderBookDepths => GetSortedAdapters().SelectMany(a => a.SupportedOrderBookDepths).Distinct().OrderBy();

	bool IMessageAdapter.IsSupportOrderBookIncrements => GetSortedAdapters().Any(a => a.IsSupportOrderBookIncrements);

	bool IMessageAdapter.IsSupportExecutionsPnL => GetSortedAdapters().Any(a => a.IsSupportExecutionsPnL);

	MessageAdapterCategories IMessageAdapter.Categories => GetSortedAdapters().Select(a => a.Categories).JoinMask();

	Type IMessageAdapter.OrderConditionType => null;

	bool IMessageAdapter.HeartbeatBeforConnect => false;

	Uri IMessageAdapter.Icon => GetType().TryGetIconUrl();

	bool IMessageAdapter.IsAutoReplyOnTransactonalUnsubscription => GetSortedAdapters().All(a => a.IsAutoReplyOnTransactonalUnsubscription);

	IOrderLogMarketDepthBuilder IMessageAdapter.CreateOrderLogMarketDepthBuilder(SecurityId securityId)
		=> new OrderLogMarketDepthBuilder(securityId);

	TimeSpan IMessageAdapter.GetHistoryStepSize(SecurityId securityId, DataType dataType, out TimeSpan iterationInterval)
	{
		foreach (var adapter in GetSortedAdapters())
		{
			var step = adapter.GetHistoryStepSize(securityId, dataType, out iterationInterval);

			if (step > TimeSpan.Zero)
				return step;
		}

		iterationInterval = TimeSpan.Zero;
		return TimeSpan.Zero;
	}

	int? IMessageAdapter.GetMaxCount(DataType dataType)
	{
		foreach (var adapter in GetSortedAdapters())
		{
			var count = adapter.GetMaxCount(dataType);

			if (count != null)
				return count;
		}

		return null;
	}

	bool IMessageAdapter.IsAllDownloadingSupported(DataType dataType) => GetSortedAdapters().Any(a => a.IsAllDownloadingSupported(dataType));

	bool IMessageAdapter.IsSecurityRequired(DataType dataType) => GetSortedAdapters().Any(a => a.IsSecurityRequired(dataType));

	bool IMessageAdapter.EnqueueSubscriptions
	{
		get => GetSortedAdapters().Any(a => a.EnqueueSubscriptions);
		set { }
	}

	/// <inheritdoc />
	public bool IsSecurityNewsOnly => GetSortedAdapters().All(a => a.IsSecurityNewsOnly);

	/// <summary>
	/// Restore subscription on reconnect.
	/// </summary>
	/// <remarks>
	/// Error case like connection lost etc.
	/// </remarks>
	public bool IsRestoreSubscriptionOnErrorReconnect { get; set; } = true;

	/// <summary>
	/// Suppress reconnecting errors.
	/// </summary>
	public bool SuppressReconnectingErrors { get; set; } = true;

	/// <summary>
	/// Use <see cref="CandleBuilderMessageAdapter"/>.
	/// </summary>
	public bool SupportCandlesCompression { get; set; } = true;

	/// <summary>
	/// Use <see cref="Level1ExtendBuilderAdapter"/>.
	/// </summary>
	public bool Level1Extend { get; set; }

	/// <summary>
	/// <see cref="CandleBuilderMessageAdapter.SendFinishedCandlesImmediatelly"/>.
	/// </summary>
	public bool SendFinishedCandlesImmediatelly { get; set; }

	/// <summary>
	/// Use <see cref="OrderLogMessageAdapter"/>.
	/// </summary>
	public bool SupportBuildingFromOrderLog { get; set; } = true;

	/// <summary>
	/// Use <see cref="StorageMessageAdapter"/>.
	/// </summary>
	public bool SupportStorage { get; set; } = true;

	/// <summary>
	/// Use <see cref="OrderBookTruncateMessageAdapter"/>.
	/// </summary>
	public bool SupportOrderBookTruncate { get; set; } = true;

	/// <summary>
	/// Use <see cref="PartialDownloadMessageAdapter"/>.
	/// </summary>
	public bool SupportPartialDownload { get; set; } = true;

	/// <summary>
	/// Use <see cref="LookupTrackingMessageAdapter"/>.
	/// </summary>
	public bool SupportLookupTracking { get; set; } = true;

	/// <summary>
	/// Use <see cref="OfflineMessageAdapter"/>.
	/// </summary>
	public bool SupportOffline { get; set; }

	/// <summary>
	/// Do not add extra adapters.
	/// </summary>
	public bool IgnoreExtraAdapters { get; set; }

	/// <summary>
	/// Use <see cref="SubscriptionSecurityAllMessageAdapter"/>.
	/// </summary>
	public bool SupportSecurityAll { get; set; } = true;

	/// <summary>
	/// Use <see cref="TransactionOrderingMessageAdapter"/>.
	/// </summary>
	public bool IsSupportTransactionLog { get; set; } = true;

	/// <summary>
	/// To call the <see cref="ConnectMessage"/> event when the first adapter connects to <see cref="InnerAdapters"/>.
	/// </summary>
	public bool ConnectDisconnectEventOnFirstAdapter { get; set; } = true;

	/// <summary>
	/// Storage processor.
	/// </summary>
	public StorageProcessor StorageProcessor { get; }

	/// <inheritdoc />
	public bool UseInChannel { get; set; } = true;

	/// <inheritdoc />
	public bool UseOutChannel { get; set; } = true;

	/// <summary>
	/// <see cref="IFillGapsBehaviour"/>
	/// </summary>
	public IFillGapsBehaviour FillGapsBehaviour { get; set; }

	TimeSpan IMessageAdapter.IterationInterval => default;

	TimeSpan? IMessageAdapter.LookupTimeout => default;

	string IMessageAdapter.FeatureName => string.Empty;

	bool? IMessageAdapter.IsPositionsEmulationRequired => null;

	bool IMessageAdapter.IsReplaceCommandEditCurrent => false;

	string[] IMessageAdapter.AssociatedBoards => [];

	bool IMessageAdapter.ExtraSetup => false;

	/// <summary>
	/// To get adapters <see cref="IInnerAdapterList.SortedAdapters"/> sorted by the specified priority. By default, there is no sorting.
	/// </summary>
	/// <returns>Sorted adapters.</returns>
	protected IEnumerable<IMessageAdapter> GetSortedAdapters() => _innerAdapters.SortedAdapters;

	private IMessageAdapter[] Wrappers => _adapterWrappers.CachedValues;

	private void TryAddOrderAdapter(long transId, IMessageAdapter adapter) => _orderAdapters.TryAdd2(transId, GetUnderlyingAdapter(adapter));

	private void ProcessReset(ResetMessage message, bool isConnect)
	{
		Wrappers.ForEach(a =>
		{
			// remove channel adapter to send ResetMsg in sync
			a.TryRemoveWrapper<ChannelMessageAdapter>()?.Dispose();

			a.SendInMessage(message);
			a.Dispose();
		});

		_adapterWrappers.Clear();

		lock (_connectedResponseLock)
		{
			_messageTypeAdapters.Clear();

			if (!isConnect)
				_pendingMessages.Clear();

			_nonSupportedAdapters.Clear();

			_adapterStates.Clear();
			_currState = ConnectionStates.Disconnected;
		}

		_requestsById.Clear();
		_subscription.Clear();
		_parentChildMap.Clear();
	}

	private IMessageAdapter CreateWrappers(IMessageAdapter adapter)
	{
		var first = adapter;

		IMessageAdapter ApplyOwnInner(MessageAdapterWrapper a)
		{
			a.OwnInnerAdapter = first != adapter;
			return a;
		}

		if (IsHeartbeatOn(adapter))
		{
			adapter = ApplyOwnInner(new HeartbeatMessageAdapter(adapter)
			{
				SuppressReconnectingErrors = SuppressReconnectingErrors,
				Parent = this,
			});
		}

		if (SupportOffline)
			adapter = ApplyOwnInner(new OfflineMessageAdapter(adapter));

		if (IgnoreExtraAdapters)
			return adapter;

		if (this.UseChannels() && adapter.UseChannels())
		{
			adapter = ApplyOwnInner(new ChannelMessageAdapter(adapter,
				adapter.UseInChannel ? new InMemoryMessageChannel(new MessageByOrderQueue(), $"{adapter} In", SendOutError) : new PassThroughMessageChannel(),
				adapter.UseOutChannel ? new InMemoryMessageChannel(new MessageByOrderQueue(), $"{adapter} Out", SendOutError) : new PassThroughMessageChannel()
			));
		}

		if (LatencyManager != null)
		{
			adapter = ApplyOwnInner(new LatencyMessageAdapter(adapter, LatencyManager.Clone()));
		}

		if (SlippageManager != null)
		{
			adapter = ApplyOwnInner(new SlippageMessageAdapter(adapter, SlippageManager.Clone()));
		}

		if (adapter.IsNativeIdentifiers)
		{
			adapter = ApplyOwnInner(new SecurityNativeIdMessageAdapter(adapter, NativeIdStorage));
		}

		if (SecurityMappingStorage != null)
		{
			adapter = ApplyOwnInner(new SecurityMappingMessageAdapter(adapter, SecurityMappingStorage));
		}

		if (SupportLookupTracking)
		{
			adapter = ApplyOwnInner(new LookupTrackingMessageAdapter(adapter));
		}

		if (IsSupportTransactionLog)
		{
			adapter = ApplyOwnInner(new TransactionOrderingMessageAdapter(adapter));
		}

		if (adapter.IsPositionsEmulationRequired is bool isPosEmu)
		{
			adapter = ApplyOwnInner(new PositionMessageAdapter(adapter, new PositionManager(isPosEmu)));
		}

		if (adapter.IsSupportSubscriptions)
		{
			adapter = ApplyOwnInner(new SubscriptionOnlineMessageAdapter(adapter));
		}

		if (SupportSecurityAll)
		{
			adapter = ApplyOwnInner(new SubscriptionSecurityAllMessageAdapter(adapter));
		}

		if (GenerateOrderBookFromLevel1 && adapter.GetSupportedMarketDataTypes().Contains(DataType.Level1) && !adapter.GetSupportedMarketDataTypes().Contains(DataType.MarketDepth))
		{
			adapter = ApplyOwnInner(new Level1DepthBuilderAdapter(adapter));
		}

		if (Level1Extend && !adapter.GetSupportedMarketDataTypes().Contains(DataType.Level1))
		{
			adapter = ApplyOwnInner(new Level1ExtendBuilderAdapter(adapter));
		}

		if (PnLManager != null && !adapter.IsSupportExecutionsPnL)
		{
			adapter = ApplyOwnInner(new PnLMessageAdapter(adapter, PnLManager.Clone()));
		}

		if (CommissionManager != null)
		{
			adapter = ApplyOwnInner(new CommissionMessageAdapter(adapter, CommissionManager.Clone()));
		}

		if (SupportPartialDownload && adapter.IsSupportPartialDownloading)
		{
			adapter = ApplyOwnInner(new PartialDownloadMessageAdapter(adapter));
		}

		if (adapter.IsSupportSubscriptions)
		{
			adapter = ApplyOwnInner(new SubscriptionMessageAdapter(adapter)
			{
				IsRestoreSubscriptionOnErrorReconnect = IsRestoreSubscriptionOnErrorReconnect,
			});
		}

		if (adapter.IsFullCandlesOnly)
		{
			adapter = ApplyOwnInner(new CandleHolderMessageAdapter(adapter));
		}

		if (SupportStorage && StorageProcessor.Settings.StorageRegistry != null)
		{
			adapter = ApplyOwnInner(new StorageMessageAdapter(adapter, StorageProcessor));
		}

		if (SupportBuildingFromOrderLog)
		{
			adapter = ApplyOwnInner(new OrderLogMessageAdapter(adapter));
		}

		if (SupportBuildingFromOrderLog || adapter.IsSupportOrderBookIncrements)
		{
			adapter = ApplyOwnInner(new OrderBookIncrementMessageAdapter(adapter));
		}

		if (SupportOrderBookTruncate)
		{
			adapter = ApplyOwnInner(new OrderBookTruncateMessageAdapter(adapter));
		}

		if (SupportCandlesCompression)
		{
			adapter = ApplyOwnInner(new CandleBuilderMessageAdapter(adapter, StorageProcessor.CandleBuilderProvider)
			{
				SendFinishedCandlesImmediatelly = SendFinishedCandlesImmediatelly,
				Buffer = Buffer,
			});
		}

		if (ExtendedInfoStorage != null && !adapter.SecurityExtendedFields.IsEmpty())
		{
			adapter = ApplyOwnInner(new ExtendedInfoStorageMessageAdapter(adapter, ExtendedInfoStorage));
		}

		if (FillGapsBehaviour is not null)
		{
			adapter = new FillGapsMessageAdapter(adapter, FillGapsBehaviour);
		}

		return adapter;
	}

	private readonly Dictionary<IMessageAdapter, bool> _hearbeatFlags = [];

	private bool IsHeartbeatOn(IMessageAdapter adapter)
	{
		return _hearbeatFlags.TryGetValue2(adapter) ?? true;
	}

	/// <summary>
	/// Apply on/off heartbeat mode for the specified adapter.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="on">Is active.</param>
	public void ApplyHeartbeat(IMessageAdapter adapter, bool on)
	{
		_hearbeatFlags[adapter] = on;
	}

	private static IMessageAdapter GetUnderlyingAdapter(IMessageAdapter adapter)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		if (adapter is IMessageAdapterWrapper wrapper)
		{
			return wrapper is IEmulationMessageAdapter or HistoryMessageAdapter
				? wrapper
				: GetUnderlyingAdapter(wrapper.InnerAdapter);
		}

		return adapter;
	}

	/// <inheritdoc />
	bool IMessageChannel.SendInMessage(Message message)
	{
		return OnSendInMessage(message);
	}

	private static Tuple<ConnectionStates, Exception> CreateState(ConnectionStates state, Exception error = null)
	{
		if (state == ConnectionStates.Failed && error == null)
			throw new ArgumentNullException(nameof(error));

		return Tuple.Create(state, error);
	}

	private long[] GetSubscribers(DataType dataType) => _subscription.SyncGet(c => c.Where(p => p.Value.Item3 == dataType).Select(p => p.Key).ToArray());

	/// <summary>
	/// Send message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns><see langword="true"/> if the specified message was processed successfully, otherwise, <see langword="false"/>.</returns>
	protected virtual bool OnSendInMessage(Message message)
	{
		try
		{
			return InternalSendInMessage(message);
		}
		catch (Exception ex)
		{
			SendOutMessage(message.CreateErrorResponse(ex, this, GetSubscribers));
			return false;
		}
	}

	private bool InternalSendInMessage(Message message)
	{
		LogDebug("In: {0}", message);

		if (message is ITransactionIdMessage transIdMsg && transIdMsg.TransactionId == 0)
			throw new ArgumentException(message.ToString());

		if (message.IsBack())
		{
			var adapter = message.Adapter ?? throw new InvalidOperationException();

			if (adapter == this)
			{
				message.UndoBack();
			}
			else
			{
				ProcessAdapterMessage(adapter, message);
				return true;
			}
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
				ProcessReset((ResetMessage)message, false);
				break;

			case MessageTypes.Connect:
			case MessageTypes.ChangePassword:
			{
				ProcessReset(new ResetMessage(), true);

				_currState = ConnectionStates.Connecting;

				_adapterWrappers.AddRange(GetSortedAdapters().ToDictionary(a => a, a =>
				{
					var adapter = a;

					lock (_connectedResponseLock)
						_adapterStates.Add(adapter, CreateState(ConnectionStates.Connecting));

					adapter = CreateWrappers(adapter);

					adapter.NewOutMessage += m => OnInnerAdapterNewOutMessage(adapter, m);

					return adapter;
				}));

				if (Wrappers.Length == 0)
					throw new InvalidOperationException(LocalizedStrings.AtLeastOneConnectionMustBe);

				Wrappers.ForEach(w =>
				{
					var u = GetUnderlyingAdapter(w);
					LogInfo("Connecting '{0}'.", u);

					w.SendInMessage(message);
				});

				break;
			}

			case MessageTypes.Disconnect:
			{
				IDictionary<IMessageAdapter, IMessageAdapter> adapters;

				lock (_connectedResponseLock)
				{
					_currState = ConnectionStates.Disconnecting;

					adapters = _adapterStates.ToArray().Where(p => _adapterWrappers.ContainsKey(p.Key) && (p.Value.Item1 == ConnectionStates.Connecting || p.Value.Item1 == ConnectionStates.Connected)).ToDictionary(p => _adapterWrappers[p.Key], p =>
					{
						var underlying = p.Key;
						_adapterStates[underlying] = CreateState(ConnectionStates.Disconnecting);
						return underlying;
					});
				}

				foreach (var a in adapters)
				{
					var wrapper = a.Key;
					var underlying = a.Value;

					LogInfo("Disconnecting '{0}'.", underlying);
					wrapper.SendInMessage(message);
				}

				break;
			}

			case MessageTypes.OrderRegister:
			{
				var ordMsg = (OrderMessage)message;
				ProcessPortfolioMessage(ordMsg.PortfolioName, ordMsg);
				break;
			}
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			{
				var ordMsg = (OrderMessage)message;
				ProcessOrderMessage(ordMsg.TransactionId, ordMsg.OriginalTransactionId, ordMsg);
				break;
			}
			case MessageTypes.OrderGroupCancel:
			{
				var groupMsg = (OrderGroupCancelMessage)message;

				if (groupMsg.PortfolioName.IsEmpty())
					ProcessOtherMessage(message);
				else
					ProcessPortfolioMessage(groupMsg.PortfolioName, groupMsg);

				break;
			}

			case MessageTypes.MarketData:
			{
				ProcessMarketDataRequest((MarketDataMessage)message);
				break;
			}

			default:
			{
				ProcessOtherMessage(message);
				break;
			}
		}

		return true;
	}

	/// <inheritdoc />
	public event Action<Message> NewOutMessage;

	private void ProcessAdapterMessage(IMessageAdapter adapter, Message message)
	{
		if (message.BackMode == MessageBackModes.Chain)
		{
			adapter = _adapterWrappers[GetUnderlyingAdapter(adapter)];
		}

		if (message is ISubscriptionMessage subscrMsg)
		{
			_subscription.TryAdd2(subscrMsg.TransactionId, Tuple.Create(subscrMsg.TypedClone(), new[] { adapter }, subscrMsg.DataType));
			SendRequest(subscrMsg.TypedClone(), adapter);
		}
		else
			adapter.SendInMessage(message);
	}

	private void ProcessOtherMessage(Message message)
	{
		if (message.Adapter != null)
		{
			message.Adapter.SendInMessage(message);
			return;
		}

		if (message is ISubscriptionMessage subscrMsg)
		{
			IMessageAdapter[] adapters;

			if (subscrMsg.IsSubscribe)
			{
				adapters = GetAdapters(message, out var isPended, out _);

				if (isPended)
					return;

				if (adapters.Length == 0)
				{
					SendOutMessage(subscrMsg.CreateResult());
					return;
				}

				_subscription.TryAdd2(subscrMsg.TransactionId, Tuple.Create(subscrMsg.TypedClone(), adapters, subscrMsg.DataType));
			}
			else
				adapters = null;

			foreach (var pair in ToChild(subscrMsg, adapters))
				SendRequest(pair.Key, pair.Value);
		}
		else
		{
			var adapters = GetAdapters(message, out var isPended, out _);

			if (isPended)
				return;

			if (adapters.Length == 0)
				return;

			adapters.ForEach(a => a.SendInMessage(message));
		}
	}

	private IMessageAdapter[] GetAdapters(Message message, out bool isPended, out bool skipSupportedMessages)
	{
		isPended = false;
		skipSupportedMessages = false;

		IMessageAdapter[] adapters = null;

		var adapter = message.Adapter;

		if (adapter != null)
			adapter = GetUnderlyingAdapter(adapter);

		if (adapter == null && message is MarketDataMessage mdMsg && mdMsg.DataType2.IsSecurityRequired && mdMsg.SecurityId != default)
		{
			adapter = _securityAdapters.TryGetValue(Tuple.Create(mdMsg.SecurityId, mdMsg.DataType2)) ?? _securityAdapters.TryGetValue(Tuple.Create(mdMsg.SecurityId, (DataType)null));

			if (adapter != null && !adapter.IsMessageSupported(message.Type))
			{
				adapter = null;
			}
		}

		if (adapter != null)
		{
			adapter = _adapterWrappers.TryGetValue(adapter);

			if (adapter != null)
			{
				adapters = [adapter];
				skipSupportedMessages = true;
			}
		}

		lock (_connectedResponseLock)
		{
			adapters ??= _messageTypeAdapters.TryGetValue(message.Type)?.Cache;

			if (adapters != null)
			{
				if (message.Type == MessageTypes.MarketData)
				{
					var mdMsg1 = (MarketDataMessage)message;
					var set = _nonSupportedAdapters.TryGetValue(mdMsg1.TransactionId);

					if (set != null)
					{
						adapters = [.. adapters.Where(a => !set.Contains(GetUnderlyingAdapter(a)))];
					}
					else if (mdMsg1.DataType2 == DataType.News && (mdMsg1.SecurityId == default || mdMsg1.SecurityId == SecurityId.News))
					{
						adapters = [.. adapters.Where(a => !a.IsSecurityNewsOnly)];
					}

					if (adapters.Length == 0)
						adapters = null;
				}
				else if (message.Type == MessageTypes.SecurityLookup)
				{
					var isAll = ((SecurityLookupMessage)message).IsLookupAll();

					if (isAll)
						adapters = [.. adapters.Where(a => a.IsSupportSecuritiesLookupAll())];
				}
			}

			if (adapters == null)
			{
				if (HasPendingAdapters() || _adapterStates.Count == 0 || _adapterStates.All(p => p.Value.Item1 is ConnectionStates.Disconnected or ConnectionStates.Failed))
				{
					isPended = true;
					_pendingMessages.Add(message.Clone());
					return [];
				}
			}
		}

		adapters ??= [];

		if (adapters.Length == 0)
		{
			LogInfo(LocalizedStrings.NoAdapterFoundFor.Put(message));
			//throw new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(message));
		}

		return adapters;
	}

	private bool HasPendingAdapters()
		=> _adapterStates.Any(p => p.Value.Item1 == ConnectionStates.Connecting);

	private IMessageAdapter[] GetSubscriptionAdapters(MarketDataMessage mdMsg, out bool isPended)
	{
		var adapters = GetAdapters(mdMsg, out isPended, out var skipSupportedMessages).Where(a =>
		{
			if (skipSupportedMessages)
				return true;

			if (!mdMsg.DataType2.IsTFCandles)
			{
				var isCandles = mdMsg.DataType2.IsCandles;

				if (a.IsMarketDataTypeSupported(mdMsg.DataType2) && (!isCandles || a.IsCandlesSupported(mdMsg)))
					return true;
				else
				{
					if (mdMsg.DataType2 == DataType.MarketDepth)
					{
						if (mdMsg.BuildMode == MarketDataBuildModes.Load)
							return false;

						if (mdMsg.BuildFrom == DataType.Level1 || mdMsg.BuildFrom == DataType.OrderLog)
							return a.IsMarketDataTypeSupported(mdMsg.BuildFrom);
						else if (mdMsg.BuildFrom == null)
						{
							if (a.IsMarketDataTypeSupported(DataType.OrderLog))
								mdMsg.BuildFrom = DataType.OrderLog;
							else if (a.IsMarketDataTypeSupported(DataType.Level1))
								mdMsg.BuildFrom = DataType.Level1;
							else
								return false;

							return true;
						}

						return false;
					}
					else if (mdMsg.DataType2 == DataType.Level1)
						return Level1Extend && a.IsMarketDataTypeSupported(mdMsg.BuildFrom ?? DataType.MarketDepth);
					else if (mdMsg.DataType2 == DataType.Ticks)
						return a.IsMarketDataTypeSupported(DataType.OrderLog);
					else
					{
						if (isCandles && a.TryGetCandlesBuildFrom(mdMsg, StorageProcessor.CandleBuilderProvider) != null)
							return true;

						return false;
					}
				}
			}

			var original = mdMsg.GetTimeFrame();
			var timeFrames = a.GetTimeFrames(mdMsg.SecurityId, mdMsg.From, mdMsg.To).ToArray();

			if (timeFrames.Contains(original) || a.CheckTimeFrameByRequest)
				return true;

			if (mdMsg.AllowBuildFromSmallerTimeFrame)
			{
				var smaller = timeFrames
				              .FilterSmallerTimeFrames(original)
				              .OrderByDescending()
				              .FirstOr();

				if (smaller != null)
					return true;
			}

			return a.TryGetCandlesBuildFrom(mdMsg, StorageProcessor.CandleBuilderProvider) != null;
		}).ToArray();

		//if (!isPended && adapters.Length == 0)
		//	throw new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(mdMsg));

		return adapters;
	}

	private IDictionary<ISubscriptionMessage, IMessageAdapter> ToChild(ISubscriptionMessage subscrMsg, IMessageAdapter[] adapters)
	{
		// sending to inner adapters unique child requests

		var child = new Dictionary<ISubscriptionMessage, IMessageAdapter>();

		if (subscrMsg.IsSubscribe)
		{
			foreach (var adapter in adapters)
			{
				var clone = subscrMsg.TypedClone();
				clone.TransactionId = adapter.TransactionIdGenerator.GetNextId();

				child.Add(clone, adapter);

				_parentChildMap.AddMapping(clone.TransactionId, subscrMsg, adapter);
			}
		}
		else
		{
			var originTransId = subscrMsg.OriginalTransactionId;

			foreach (var pair in _parentChildMap.GetChild(originTransId))
			{
				var adapter = pair.Value;

				var clone = subscrMsg.TypedClone();
				clone.TransactionId = adapter.TransactionIdGenerator.GetNextId();
				clone.OriginalTransactionId = pair.Key;

				child.Add(clone, adapter);

				_parentChildMap.AddMapping(clone.TransactionId, subscrMsg, adapter);
			}
		}

		return child;
	}

	private void SendRequest(ISubscriptionMessage subscrMsg, IMessageAdapter adapter)
	{
		// if the message was looped back via IsBack=true
		_requestsById.TryAdd2(subscrMsg.TransactionId, Tuple.Create(subscrMsg, GetUnderlyingAdapter(adapter)));
		LogDebug("Send to {0}: {1}", adapter, subscrMsg);
		adapter.SendInMessage((Message)subscrMsg);
	}

	private void ProcessMarketDataRequest(MarketDataMessage mdMsg)
	{
		IMessageAdapter[] GetAdapters()
		{
			if (!mdMsg.IsSubscribe)
				return null;

			var adapters = GetSubscriptionAdapters(mdMsg, out var isPended);

			if (isPended)
				return null;

			if (adapters.Length == 0)
			{
				SendOutMessage(mdMsg.TransactionId.CreateNotSupported());
				return null;
			}

			return adapters;
		}

		if (mdMsg.DataType2 == DataType.News || mdMsg.DataType2 == DataType.Board)
		{
			var adapters = GetAdapters();

			if (mdMsg.IsSubscribe)
			{
				if (adapters == null)
					return;

				_subscription.TryAdd2(mdMsg.TransactionId, Tuple.Create((ISubscriptionMessage)mdMsg.Clone(), adapters, mdMsg.DataType2));
			}

			foreach (var pair in ToChild(mdMsg, adapters))
				SendRequest(pair.Key, pair.Value);
		}
		else
		{
			IMessageAdapter adapter;

			if (mdMsg.IsSubscribe)
			{
				adapter = GetAdapters()?.First();

				if (adapter == null)
					return;

				mdMsg = mdMsg.TypedClone();
				_subscription.TryAdd2(mdMsg.TransactionId, Tuple.Create((ISubscriptionMessage)mdMsg.Clone(), new[] { adapter }, mdMsg.DataType2));
			}
			else
			{
				var originTransId = mdMsg.OriginalTransactionId;

				if (!_subscription.TryGetValue(originTransId, out var tuple))
				{
					lock (_connectedResponseLock)
					{
						var suspended = _pendingMessages.FirstOrDefault(m => m is MarketDataMessage prevMdMsg && prevMdMsg.TransactionId == originTransId);

						if (suspended != null)
						{
							_pendingMessages.Remove(suspended);
							SendOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = mdMsg.TransactionId });
							return;
						}
					}

					LogInfo("Unsubscribe not found: {0}/{1}", originTransId, mdMsg);
					return;
				}

				adapter = tuple.Item2.First();

				mdMsg = mdMsg.TypedClone();
			}

			SendRequest(mdMsg, adapter);
		}
	}

	private void ProcessPortfolioMessage(string portfolioName, OrderMessage message)
	{
		var adapter = message.Adapter;

		if (adapter == null)
		{
			adapter = GetAdapter(portfolioName, message, out var isPending);

			if (adapter == null)
			{
				if (isPending)
					return;

				LogDebug("No adapter for {0}", message);

				SendOutMessage(message.CreateReply(new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(message))));
				return;
			}
		}

		if (message is OrderRegisterMessage regMsg)
			TryAddOrderAdapter(regMsg.TransactionId, adapter);

		adapter.SendInMessage(message);
	}

	private void ProcessOrderMessage(long transId, long originId, Message message)
	{
		if (!_orderAdapters.TryGetValue(originId, out var adapter))
		{
			if (message is OrderMessage ordMsg && !ordMsg.PortfolioName.IsEmpty())
				adapter = GetAdapter(ordMsg.PortfolioName, message, out _);
		}

		void sendUnkTrans()
		{
			LogError(LocalizedStrings.UnknownTransactionId, originId);

			SendOutMessage(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				OriginalTransactionId = transId,
				Error = new InvalidOperationException(LocalizedStrings.UnknownTransactionId.Put(originId)),
			});
		}

		if (adapter is null)
		{
			sendUnkTrans();
			return;
		}

		if (!_adapterWrappers.TryGetValue(adapter, out var wrapper))
		{
			sendUnkTrans();
			return;
		}

		if (message is OrderReplaceMessage replace)
		{
			TryAddOrderAdapter(replace.TransactionId, adapter);
		}

		wrapper.SendInMessage(message);
	}

	/// <summary>
	/// Try find adapter by portfolio name.
	/// </summary>
	/// <param name="porfolioName">Portfolio name.</param>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <returns>Found <see cref="IMessageAdapter"/>.</returns>
	public bool TryGetAdapter(string porfolioName, out IMessageAdapter adapter)
		=> _portfolioAdapters.TryGetValue(porfolioName, out adapter);

	private IMessageAdapter GetAdapter(string portfolioName, Message message, out bool isPended)
	{
		if (portfolioName.IsEmpty())
			throw new ArgumentNullException(nameof(portfolioName));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (!TryGetAdapter(portfolioName, out var adapter))
		{
			return GetAdapters(message, out isPended, out _).FirstOrDefault();
		}
		else
		{
			isPended = false;

			return _adapterWrappers.TryGetValue(adapter)
				?? throw new InvalidOperationException(LocalizedStrings.ConnectionIsNotConnected.Put(adapter));
		}
	}

	/// <summary>
	/// The embedded adapter event <see cref="IMessageChannel.NewOutMessage"/> handler.
	/// </summary>
	/// <param name="innerAdapter">The embedded adapter.</param>
	/// <param name="message">Message.</param>
	protected virtual void OnInnerAdapterNewOutMessage(IMessageAdapter innerAdapter, Message message)
	{
		List<Message> extra = null;

		if (!message.IsBack())
		{
			message.Adapter ??= innerAdapter;

			switch (message.Type)
			{
				case MessageTypes.Time:
					// out time messages required for LookupTrackingMessageAdapter
					return;

				case MessageTypes.Connect:
					extra = [];
					ProcessConnectMessage(innerAdapter, (ConnectMessage)message, extra);
					break;

				case MessageTypes.Disconnect:
					extra = [];
					ProcessDisconnectMessage(innerAdapter, (DisconnectMessage)message, extra);
					break;

				case MessageTypes.SubscriptionResponse:
					message = ProcessSubscriptionResponse(innerAdapter, (SubscriptionResponseMessage)message);
					break;

				case MessageTypes.SubscriptionFinished:
					message = ProcessSubscriptionFinished((SubscriptionFinishedMessage)message);
					break;

				case MessageTypes.SubscriptionOnline:
					message = ProcessSubscriptionOnline((SubscriptionOnlineMessage)message);
					break;

				case MessageTypes.Portfolio:
				//case MessageTypes.PortfolioChange:
				case MessageTypes.PositionChange:
				{
					var pfMsg = (IPortfolioNameMessage)message;
					ApplyParentLookupId((ISubscriptionIdMessage)message);
					PortfolioAdapterProvider.SetAdapter(pfMsg.PortfolioName, GetUnderlyingAdapter(innerAdapter));
					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					ApplyParentLookupId(secMsg);
					SecurityAdapterProvider.SetAdapter(secMsg.SecurityId, null, GetUnderlyingAdapter(innerAdapter).Id);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.DataType != DataType.Transactions)
						break;

					ApplyParentLookupId(execMsg);

					if (execMsg.TransactionId != default)
					{
						if (execMsg.HasOrderInfo)
							TryAddOrderAdapter(execMsg.TransactionId, innerAdapter);
					}

					break;
				}

				default:
				{
					if (message is ISubscriptionIdMessage subscrIdMsg)
						ApplyParentLookupId(subscrIdMsg);

					break;
				}
			}
		}

		if (message != null)
			SendOutMessage(message);

		if (extra != null)
		{
			foreach (var m in extra)
				SendOutMessage(m);
		}
	}

	private void ApplyParentLookupId(ISubscriptionIdMessage msg)
	{
		if (msg == null)
			throw new ArgumentNullException(nameof(msg));

		var originIds = msg.GetSubscriptionIds();
		var ids = originIds;
		var changed = false;

		for (var i = 0; i < ids.Length; i++)
		{
			if (!_parentChildMap.TryGetParent(ids[i], out var parentId))
				continue;

			if (!changed)
			{
				ids = [.. originIds];
				changed = true;
			}

			if (msg.OriginalTransactionId == ids[i])
				msg.OriginalTransactionId = parentId;

			ids[i] = parentId;
		}

		if (changed)
			msg.SetSubscriptionIds(ids);
	}

	private void SendOutError(Exception error)
	{
		SendOutMessage(error.ToErrorMessage());
	}

	private void SendOutMessage(Message message)
	{
		OnSendOutMessage(message);
	}

	/// <summary>
	/// Send outgoing message and raise <see cref="NewOutMessage"/> event.
	/// </summary>
	/// <param name="message">Message.</param>
	protected virtual void OnSendOutMessage(Message message)
	{
		message.Adapter ??= this;

		NewOutMessage?.Invoke(message);
	}

	private void ProcessConnectMessage(IMessageAdapter innerAdapter, ConnectMessage message, List<Message> extra)
	{
		var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);
		var wrapper = _adapterWrappers[underlyingAdapter];

		var error = message.Error;

		if (error != null)
			LogError(LocalizedStrings.ConnectionErrorFor, underlyingAdapter, error);
		else
			LogInfo("Connected to '{0}'.", underlyingAdapter);

		Message[] notSupportedMsgs = null;

		lock (_connectedResponseLock)
		{
			if (error == null)
			{
				foreach (var supportedMessage in innerAdapter.SupportedInMessages)
					_messageTypeAdapters.SafeAdd(supportedMessage).Add(wrapper);
			}

			UpdateAdapterState(underlyingAdapter, true, error, extra);

			if (!HasPendingAdapters())
			{
				var pending = _pendingMessages.CopyAndClear();

				if (_adapterStates.Any(p => p.Value.Item1 == ConnectionStates.Connected))
					extra.AddRange(pending.Select(m => m.LoopBack(this)));
				else
					notSupportedMsgs = pending;
			}
		}

		if (notSupportedMsgs != null)
		{
			foreach (var notSupportedMsg in notSupportedMsgs)
			{
				SendOutError(new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(notSupportedMsg.Type)));
			}
		}

		message.Adapter = underlyingAdapter;
	}

	private void ProcessDisconnectMessage(IMessageAdapter innerAdapter, DisconnectMessage message, List<Message> extra)
	{
		var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);
		var wrapper = _adapterWrappers[underlyingAdapter];

		var error = message.Error;

		if (error == null)
			LogInfo("Disconnected from '{0}'.", underlyingAdapter);
		else
			LogError(LocalizedStrings.ErrorDisconnectFor, underlyingAdapter, error);

		lock (_connectedResponseLock)
		{
			foreach (var supportedMessage in innerAdapter.SupportedInMessages)
			{
				var list = _messageTypeAdapters.TryGetValue(supportedMessage);

				if (list == null)
					continue;

				list.Remove(wrapper);

				if (list.Count == 0)
					_messageTypeAdapters.Remove(supportedMessage);
			}

			UpdateAdapterState(underlyingAdapter, false, error, extra);
		}

		message.Adapter = underlyingAdapter;
	}

	private void UpdateAdapterState(IMessageAdapter adapter, bool isConnect, Exception error, List<Message> extra)
	{
		if (isConnect)
		{
			void CreateConnectedMsg(ConnectionStates newState, Exception e = null)
			{
				extra.Add(new ConnectMessage { Error = e });
				_currState = newState;
			}

			if (error == null)
			{
				_adapterStates[adapter] = CreateState(ConnectionStates.Connected);

				if (_currState == ConnectionStates.Connecting)
				{
					if (ConnectDisconnectEventOnFirstAdapter)
					{
						// raise Connected event only one time for the first adapter
						CreateConnectedMsg(ConnectionStates.Connected);
					}
					else
					{
						var noPending = _adapterStates.All(v => v.Value.Item1 != ConnectionStates.Connecting);

						if (noPending)
							CreateConnectedMsg(ConnectionStates.Connected);
					}
				}
			}
			else
			{
				_adapterStates[adapter] = CreateState(ConnectionStates.Failed, error);

				if (_currState is ConnectionStates.Connecting or ConnectionStates.Connected)
				{
					var allFailed = _adapterStates.All(v => v.Value.Item1 == ConnectionStates.Failed);

					if (allFailed)
					{
						var errors = _adapterStates.Select(v => v.Value.Item2).WhereNotNull().ToArray();
						CreateConnectedMsg(ConnectionStates.Failed, errors.SingleOrAggr());
					}
				}
			}
		}
		else
		{
			if (error == null)
				_adapterStates[adapter] = CreateState(ConnectionStates.Disconnected);
			else
				_adapterStates[adapter] = CreateState(ConnectionStates.Failed, error);

			var noPending = _adapterStates.All(v => v.Value.Item1 is ConnectionStates.Disconnected or ConnectionStates.Failed);

			if (noPending)
			{
				extra.Add(new DisconnectMessage());
				_currState = ConnectionStates.Disconnected;
			}
		}
	}

	private SubscriptionOnlineMessage ProcessSubscriptionOnline(SubscriptionOnlineMessage message)
	{
		var originalTransactionId = message.OriginalTransactionId;

		var parentId = _parentChildMap.ProcessChildOnline(originalTransactionId, out var needParentResponse);

		if (parentId == null)
			return message;

		if (!needParentResponse)
			return null;

		return new SubscriptionOnlineMessage { OriginalTransactionId = parentId.Value };
	}

	private SubscriptionFinishedMessage ProcessSubscriptionFinished(SubscriptionFinishedMessage message)
	{
		var originalTransactionId = message.OriginalTransactionId;

		_requestsById.Remove(originalTransactionId);

		var parentId = _parentChildMap.ProcessChildFinish(originalTransactionId, out var needParentResponse);

		if (parentId == null)
		{
			_subscription.Remove(originalTransactionId);
			return message;
		}

		if (!needParentResponse)
			return null;

		_subscription.Remove(parentId.Value);
		return new SubscriptionFinishedMessage { OriginalTransactionId = parentId.Value, Body = message.Body };
	}

	private Message ProcessSubscriptionResponse(IMessageAdapter adapter, SubscriptionResponseMessage message)
	{
		var originalTransactionId = message.OriginalTransactionId;

		if (!_requestsById.TryGetValue(originalTransactionId, out var tuple))
			return message;

		var error = message.Error;
		var originMsg = tuple.Item1;

		if (error != null)
		{
			LogWarning("Subscription Error out: {0}", message);
			_subscription.Remove(originalTransactionId);
			_requestsById.Remove(originalTransactionId);
		}
		else if (!originMsg.IsSubscribe)
		{
			// remove subscribe and unsubscribe requests
			_requestsById.Remove(originMsg.OriginalTransactionId);
			_requestsById.Remove(originalTransactionId);
		}

		var parentId = _parentChildMap.ProcessChildResponse(originalTransactionId, error, out var needParentResponse, out var allError, out var innerErrors);

		if (parentId != null)
		{
			if (allError)
				_subscription.Remove(parentId.Value);

			return needParentResponse
				? parentId.Value.CreateSubscriptionResponse(allError ? new AggregateException(LocalizedStrings.NoAdapterFoundFor.Put(originMsg), innerErrors) : null)
				: null;
		}
		else
		{
			if (!originMsg.IsSubscribe)
				_subscription.Remove(originMsg.OriginalTransactionId);
		}

		if (message.IsNotSupported() && originMsg is ISubscriptionMessage subscrMsg)
		{
			lock (_connectedResponseLock)
			{
				// try loopback only subscribe messages
				if (subscrMsg.IsSubscribe)
				{
					var set = _nonSupportedAdapters.SafeAdd(originalTransactionId, _ => []);
					set.Add(GetUnderlyingAdapter(adapter));

					subscrMsg.LoopBack(this);
				}
			}

			return (Message)subscrMsg;
		}

		return message;
	}

	private void SecurityAdapterProviderOnChanged(Tuple<SecurityId, DataType> key, Guid adapterId, bool changeType)
	{
		if (changeType)
		{
			var adapter = InnerAdapters.SyncGet(c => c.FindById(adapterId));

			if (adapter == null)
				_securityAdapters.Remove(key);
			else
				_securityAdapters[key] = adapter;
		}
		else
			_securityAdapters.Remove(key);
	}

	private void PortfolioAdapterProviderOnChanged(string key, Guid adapterId, bool changeType)
	{
		if (changeType)
		{
			var adapter = InnerAdapters.SyncGet(c => c.FindById(adapterId));

			if (adapter == null)
				_portfolioAdapters.Remove(key);
			else
				_portfolioAdapters[key] = adapter;
		}
		else
			_portfolioAdapters.Remove(key);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		lock (InnerAdapters.SyncRoot)
		{
			storage.SetValue(nameof(InnerAdapters), InnerAdapters.Select(a =>
			{
				var s = new SettingsStorage();

				s.SetValue("AdapterType", a.GetType().GetTypeName(false));
				s.SetValue("AdapterSettings", a.Save());
				s.SetValue("Priority", InnerAdapters[a]);

				return s;
			}).ToArray());
		}

		base.Save(storage);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		lock (InnerAdapters.SyncRoot)
		{
			InnerAdapters.Clear();

			var adapters = new Dictionary<Guid, IMessageAdapter>();

			foreach (var s in storage.GetValue<IEnumerable<SettingsStorage>>(nameof(InnerAdapters)))
			{
				try
				{
					var adapter = s.GetValue<Type>("AdapterType").CreateAdapter(TransactionIdGenerator);
					adapter.Load(s, "AdapterSettings");
					InnerAdapters[adapter] = s.GetValue<int>("Priority");

					adapters.Add(adapter.Id, adapter);
				}
				catch (Exception e)
				{
					LogError(e);
				}
			}

			_securityAdapters.Clear();

			foreach (var pair in SecurityAdapterProvider.Adapters)
			{
				if (!adapters.TryGetValue(pair.Value, out var adapter))
					continue;

				_securityAdapters.Add(pair.Key, adapter);
			}

			_portfolioAdapters.Clear();

			foreach (var pair in PortfolioAdapterProvider.Adapters)
			{
				if (!adapters.TryGetValue(pair.Value, out var adapter))
					continue;

				_portfolioAdapters.Add(pair.Key, adapter);
			}
		}

		base.Load(storage);
	}

	/// <summary>
	/// To release allocated resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		SecurityAdapterProvider.Changed -= SecurityAdapterProviderOnChanged;
		PortfolioAdapterProvider.Changed -= PortfolioAdapterProviderOnChanged;

		Wrappers.ForEach(a => a.Parent = null);

		base.DisposeManaged();
	}

	/// <summary>
	/// Create a copy of <see cref="BasketMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public IMessageChannel Clone()
	{
		var clone = new BasketMessageAdapter(TransactionIdGenerator, StorageProcessor.CandleBuilderProvider, SecurityAdapterProvider, PortfolioAdapterProvider, Buffer)
		{
			ExtendedInfoStorage = ExtendedInfoStorage,
			SupportCandlesCompression = SupportCandlesCompression,
			Level1Extend = Level1Extend,
			SuppressReconnectingErrors = SuppressReconnectingErrors,
			IsRestoreSubscriptionOnErrorReconnect = IsRestoreSubscriptionOnErrorReconnect,
			SupportStorage = SupportStorage,
			SupportBuildingFromOrderLog = SupportBuildingFromOrderLog,
			SupportOrderBookTruncate = SupportOrderBookTruncate,
			SupportOffline = SupportOffline,
			IgnoreExtraAdapters = IgnoreExtraAdapters,
			NativeIdStorage = NativeIdStorage,
			ConnectDisconnectEventOnFirstAdapter = ConnectDisconnectEventOnFirstAdapter,
			UseInChannel = UseInChannel,
			UseOutChannel = UseOutChannel,
			IsSupportTransactionLog = IsSupportTransactionLog,
			FillGapsBehaviour = FillGapsBehaviour,
			GenerateOrderBookFromLevel1 = GenerateOrderBookFromLevel1,
		};

		clone.Load(this.Save());

		return clone;
	}

	object ICloneable.Clone() => Clone();

	ChannelStates IMessageChannel.State => ChannelStates.Started;

	void IMessageChannel.Open()
	{
	}

	void IMessageChannel.Close()
	{
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

	event Action IMessageChannel.StateChanged
	{
		add { }
		remove { }
	}
}
