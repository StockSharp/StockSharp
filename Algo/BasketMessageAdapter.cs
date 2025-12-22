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
public class BasketMessageAdapter : BaseLogReceiver, IMessageAdapterWrapper
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

			using (_parent._connectedResponseLock.Lock())
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

			using (_parent._connectedResponseLock.Lock())
				_parent._adapterStates.Clear();

			return base.OnClearing();
		}

		public int this[IMessageAdapter adapter]
		{
			get
			{
				using (EnterScope())
					return _enables.TryGetValue2(adapter) ?? -1;
			}
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				using (EnterScope())
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
		private readonly Lock _syncObject = new();
		private readonly Dictionary<long, RefQuadruple<long, SubscriptionStates, IMessageAdapter, Exception>> _childToParentIds = [];

		public void AddMapping(long childId, ISubscriptionMessage parentMsg, IMessageAdapter adapter)
		{
			if (childId <= 0)
				throw new ArgumentOutOfRangeException(nameof(childId));

			if (parentMsg == null)
				throw new ArgumentNullException(nameof(parentMsg));

			if (adapter == null)
				throw new ArgumentNullException(nameof(adapter));

			using (_syncObject.EnterScope())
				_childToParentIds.Add(childId, RefTuple.Create(parentMsg.TransactionId, SubscriptionStates.Stopped, adapter, default(Exception)));
		}

		public IDictionary<long, IMessageAdapter> GetChild(long parentId)
		{
			if (parentId <= 0)
				throw new ArgumentOutOfRangeException(nameof(parentId));

			using (_syncObject.EnterScope())
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

			using (_syncObject.EnterScope())
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

			using (_syncObject.EnterScope())
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
			using (_syncObject.EnterScope())
				_childToParentIds.Clear();
		}

		public bool TryGetParent(long childId, out long parentId)
		{
			parentId = default;

			using (_syncObject.EnterScope())
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
	private readonly AsyncLock _connectedResponseLock = new();
	private readonly Dictionary<MessageTypes, CachedSynchronizedSet<IMessageAdapter>> _messageTypeAdapters = [];
	private readonly List<Message> _pendingMessages = [];

	private readonly Dictionary<IMessageAdapter, (ConnectionStates state, Exception err)> _adapterStates = [];
	private ConnectionStates _currState = ConnectionStates.Disconnected;

	private readonly SynchronizedDictionary<string, IMessageAdapter> _portfolioAdapters = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly SynchronizedDictionary<(SecurityId secId, DataType dt), IMessageAdapter> _securityAdapters = [];

	private readonly SynchronizedDictionary<long, (ISubscriptionMessage subMsg, IMessageAdapter[] adapters, DataType dt)> _subscriptions = [];
	private readonly SynchronizedDictionary<long, (ISubscriptionMessage subMsg, IMessageAdapter adapter)> _requestsById = [];
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
		IStorageBuffer buffer)
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
	public IStorageBuffer Buffer { get; }

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

	IEnumerable<DataType> IMessageAdapter.GetSupportedMarketDataTypes(SecurityId securityId, DateTime? from, DateTime? to)
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

	IEnumerable<(string, Type)> IMessageAdapter.SecurityExtendedFields => GetSortedAdapters().SelectMany(a => a.SecurityExtendedFields).Distinct();

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
	public IStorageProcessor StorageProcessor { get; }

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

	TimeSpan IMessageAdapter.DisconnectTimeout => default;
	int IMessageAdapter.MaxParallelMessages { get => default; set => throw new NotSupportedException(); }
	TimeSpan IMessageAdapter.FaultDelay { get => default; set => throw new NotSupportedException(); }
    IMessageAdapter IMessageAdapterWrapper.InnerAdapter
	{
		get => null;
		set => throw new NotSupportedException();
	}

	private void TryAddOrderAdapter(long transId, IMessageAdapter adapter) => _orderAdapters.TryAdd2(transId, GetUnderlyingAdapter(adapter));

	private async ValueTask ProcessReset(ResetMessage message, bool isConnect, CancellationToken cancellationToken)
	{
		await Wrappers.Select(async a =>
		{
			// remove channel adapter to send ResetMsg in sync
			a.TryRemoveWrapper<ChannelMessageAdapter>()?.Dispose();

			await a.SendInMessageAsync(message, cancellationToken);
			a.Dispose();
		}).WhenAll();

		_adapterWrappers.Clear();

		using (await _connectedResponseLock.LockAsync(cancellationToken))
		{
			_messageTypeAdapters.Clear();

			if (!isConnect)
				_pendingMessages.Clear();

			_nonSupportedAdapters.Clear();

			_adapterStates.Clear();
			_currState = ConnectionStates.Disconnected;
		}

		_requestsById.Clear();
		_subscriptions.Clear();
		_parentChildMap.Clear();
	}

	private IMessageAdapterWrapper CreateWrappers(IMessageAdapter adapter)
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
			return (IMessageAdapterWrapper)adapter;

		if (this.UseChannels() && adapter.UseChannels())
		{
			adapter = ApplyOwnInner(new ChannelMessageAdapter(adapter,
				adapter.UseInChannel ? new AsyncMessageChannel(adapter) : new PassThroughMessageChannel(),
				adapter.UseOutChannel ? new InMemoryMessageChannel(new MessageByOrderQueue(), $"{adapter} Out", ex => SendOutErrorAsync(ex, default)) : new PassThroughMessageChannel()
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

		return (IMessageAdapterWrapper)adapter;
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
	ValueTask IMessageAdapter.SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return OnSendInMessageAsync(message, cancellationToken);
	}

	private static (ConnectionStates, Exception) CreateState(ConnectionStates state, Exception error = null)
	{
		if (state == ConnectionStates.Failed && error == null)
			throw new ArgumentNullException(nameof(error));

		return (state, error);
	}

	private long[] GetSubscribers(DataType dataType) => _subscriptions.SyncGet(c => c.Where(p => p.Value.dt == dataType).Select(p => p.Key).ToArray());

	/// <summary>
	/// Send message.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	protected virtual ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		try
		{
			return InternalSendInMessage(message, cancellationToken);
		}
		catch (Exception ex)
		{
			return SendOutMessageAsync(message.CreateErrorResponse(ex, this, GetSubscribers), cancellationToken);
		}
	}

	private async ValueTask InternalSendInMessage(Message message, CancellationToken cancellationToken)
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
				await ProcessAdapterMessage(adapter, message, cancellationToken);
				return;
			}
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
				await ProcessReset((ResetMessage)message, false, cancellationToken);
				break;

			case MessageTypes.Connect:
			case MessageTypes.ChangePassword:
			{
				await ProcessReset(new ResetMessage(), true, cancellationToken);

				_currState = ConnectionStates.Connecting;

				foreach (var adapter in GetSortedAdapters())
				{
					using (await _connectedResponseLock.LockAsync(cancellationToken))
						_adapterStates.Add(adapter, CreateState(ConnectionStates.Connecting));

					var wrapper = CreateWrappers(adapter);

					wrapper.NewOutMessageAsync += (m, ct) => OnInnerAdapterNewOutMessage(adapter, m, ct);

					_adapterWrappers.Add(adapter, wrapper);
				}

				if (Wrappers.Length == 0)
					throw new InvalidOperationException(LocalizedStrings.AtLeastOneConnectionMustBe);

				await Wrappers.Select(w =>
				{
					var u = GetUnderlyingAdapter(w);
					LogInfo("Connecting '{0}'.", u);

					return w.SendInMessageAsync(message, cancellationToken);
				}).WhenAll();
				break;
			}

			case MessageTypes.Disconnect:
			{
				IDictionary<IMessageAdapter, IMessageAdapter> adapters;

				using (await _connectedResponseLock.LockAsync(cancellationToken))
				{
					_currState = ConnectionStates.Disconnecting;

					adapters = _adapterStates.ToArray().Where(p => _adapterWrappers.ContainsKey(p.Key) && (p.Value.state == ConnectionStates.Connecting || p.Value.state == ConnectionStates.Connected)).ToDictionary(p => _adapterWrappers[p.Key], p =>
					{
						var underlying = p.Key;
						_adapterStates[underlying] = CreateState(ConnectionStates.Disconnecting);
						return underlying;
					});
				}

				await adapters.Select(a =>
				{
					var wrapper = a.Key;
					var underlying = a.Value;

					LogInfo("Disconnecting '{0}'.", underlying);
					return wrapper.SendInMessageAsync(message, cancellationToken);
				}).WhenAll();

				break;
			}

			case MessageTypes.OrderRegister:
			{
				var ordMsg = (OrderMessage)message;
				await ProcessPortfolioMessage(ordMsg.PortfolioName, ordMsg, cancellationToken);
				break;
			}
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			{
				var ordMsg = (OrderMessage)message;
				await ProcessOrderMessage(ordMsg.TransactionId, ordMsg.OriginalTransactionId, ordMsg, cancellationToken);
				break;
			}
			case MessageTypes.OrderGroupCancel:
			{
				var groupMsg = (OrderGroupCancelMessage)message;

				if (groupMsg.PortfolioName.IsEmpty())
					await ProcessOtherMessage(message, cancellationToken);
				else
					await ProcessPortfolioMessage(groupMsg.PortfolioName, groupMsg, cancellationToken);

				break;
			}

			case MessageTypes.MarketData:
			{
				await ProcessMarketDataRequest((MarketDataMessage)message, cancellationToken);
				break;
			}

			default:
			{
				await ProcessOtherMessage(message, cancellationToken);
				break;
			}
		}
	}

	/// <inheritdoc />
	[Obsolete]
	public event Action<Message> NewOutMessage;

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;

	private ValueTask ProcessAdapterMessage(IMessageAdapter adapter, Message message, CancellationToken cancellationToken)
	{
		if (message.BackMode == MessageBackModes.Chain)
		{
			adapter = _adapterWrappers[GetUnderlyingAdapter(adapter)];
		}

		if (message is ISubscriptionMessage subscrMsg)
		{
			_subscriptions.TryAdd2(subscrMsg.TransactionId, (subscrMsg.TypedClone(), new[] { adapter }, subscrMsg.DataType));
			return SendRequest(subscrMsg.TypedClone(), adapter, cancellationToken);
		}
		else
			return adapter.SendInMessageAsync(message, cancellationToken);
	}

	private async ValueTask ProcessOtherMessage(Message message, CancellationToken cancellationToken)
	{
		if (message.Adapter != null)
		{
			await message.Adapter.SendInMessageAsync(message, cancellationToken);
			return;
		}

		if (message is ISubscriptionMessage subscrMsg)
		{
			IMessageAdapter[] adapters;

			if (subscrMsg.IsSubscribe)
			{
				var (a, isPended, _) = await GetAdapters(message, cancellationToken);
				adapters = a;

				if (isPended)
					return;

				if (adapters.Length == 0)
				{
					await SendOutMessageAsync(subscrMsg.CreateResult(), cancellationToken);
					return;
				}

				_subscriptions.TryAdd2(subscrMsg.TransactionId, (subscrMsg.TypedClone(), adapters, subscrMsg.DataType));
			}
			else
				adapters = null;

			await ToChild(subscrMsg, adapters).Select(pair => SendRequest(pair.Key, pair.Value, cancellationToken)).WhenAll();
		}
		else
		{
			var (adapters, isPended, _) = await GetAdapters(message, cancellationToken);

			if (isPended)
				return;

			if (adapters.Length == 0)
				return;

			await adapters.Select(a => a.SendInMessageAsync(message, cancellationToken)).WhenAll();
		}
	}

	private async ValueTask<(IMessageAdapter[] adapters, bool isPended, bool skipSupportedMessages)> GetAdapters(Message message, CancellationToken cancellationToken)
	{
		var isPended = false;
		var skipSupportedMessages = false;

		IMessageAdapter[] adapters = null;

		var adapter = message.Adapter;

		if (adapter != null)
			adapter = GetUnderlyingAdapter(adapter);

		if (adapter == null && message is MarketDataMessage mdMsg && mdMsg.DataType2.IsSecurityRequired && mdMsg.SecurityId != default)
		{
			adapter = _securityAdapters.TryGetValue((mdMsg.SecurityId, mdMsg.DataType2)) ?? _securityAdapters.TryGetValue((mdMsg.SecurityId, (DataType)null));

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

		using (await _connectedResponseLock.LockAsync(cancellationToken))
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
				if (HasPendingAdapters() || _adapterStates.Count == 0 || _adapterStates.All(p => p.Value.state is ConnectionStates.Disconnected or ConnectionStates.Failed))
				{
					isPended = true;
					_pendingMessages.Add(message.Clone());
					return ([], isPended, skipSupportedMessages);
				}
			}
		}

		adapters ??= [];

		if (adapters.Length == 0)
		{
			LogInfo(LocalizedStrings.NoAdapterFoundFor.Put(message));
			//throw new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(message));
		}

		return (adapters, isPended, skipSupportedMessages);
	}

	private bool HasPendingAdapters()
		=> _adapterStates.Any(p => p.Value.state == ConnectionStates.Connecting);

	private async ValueTask<(IMessageAdapter[] adapters, bool isPended)> GetSubscriptionAdapters(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var (adapters, isPended, skipSupportedMessages) = await GetAdapters(mdMsg, cancellationToken);

		adapters = adapters.Where(a =>
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

		return (adapters, isPended);
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

	private ValueTask SendRequest(ISubscriptionMessage subscrMsg, IMessageAdapter adapter, CancellationToken cancellationToken)
	{
		// if the message was looped back via IsBack=true
		_requestsById.TryAdd2(subscrMsg.TransactionId, (subscrMsg, GetUnderlyingAdapter(adapter)));
		LogDebug("Send to {0}: {1}", adapter, subscrMsg);
		return adapter.SendInMessageAsync((Message)subscrMsg, cancellationToken);
	}

	private async ValueTask ProcessMarketDataRequest(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		async ValueTask<IMessageAdapter[]> GetAdapters()
		{
			if (!mdMsg.IsSubscribe)
				return null;

			var (adapters, isPended) = await GetSubscriptionAdapters(mdMsg, cancellationToken);

			if (isPended)
				return null;

			if (adapters.Length == 0)
			{
				await SendOutMessageAsync(mdMsg.TransactionId.CreateNotSupported(), cancellationToken);
				return null;
			}

			return adapters;
		}

		if (mdMsg.DataType2 == DataType.News || mdMsg.DataType2 == DataType.Board)
		{
			var adapters = await GetAdapters();

			if (mdMsg.IsSubscribe)
			{
				if (adapters == null)
					return;

				_subscriptions.TryAdd2(mdMsg.TransactionId, ((ISubscriptionMessage)mdMsg.Clone(), adapters, mdMsg.DataType2));
			}

			await ToChild(mdMsg, adapters).Select(pair => SendRequest(pair.Key, pair.Value, cancellationToken)).WhenAll();
		}
		else
		{
			IMessageAdapter adapter;

			if (mdMsg.IsSubscribe)
			{
				adapter = (await GetAdapters())?.First();

				if (adapter == null)
					return;

				mdMsg = mdMsg.TypedClone();
				_subscriptions.TryAdd2(mdMsg.TransactionId, ((ISubscriptionMessage)mdMsg.Clone(), new[] { adapter }, mdMsg.DataType2));
			}
			else
			{
				var originTransId = mdMsg.OriginalTransactionId;

				if (!_subscriptions.TryGetValue(originTransId, out var tuple))
				{
					Message outMsg = null;

					using (await _connectedResponseLock.LockAsync(cancellationToken))
					{
						var suspended = _pendingMessages.FirstOrDefault(m => m is MarketDataMessage prevMdMsg && prevMdMsg.TransactionId == originTransId);

						if (suspended != null)
						{
							_pendingMessages.Remove(suspended);
							outMsg = new SubscriptionResponseMessage { OriginalTransactionId = mdMsg.TransactionId };
						}
					}

					if (outMsg != null)
					{
						await SendOutMessageAsync(outMsg, cancellationToken);
						return;
					}

					LogInfo("Unsubscribe not found: {0}/{1}", originTransId, mdMsg);
					return;
				}

				adapter = tuple.adapters.First();

				mdMsg = mdMsg.TypedClone();
			}

			await SendRequest(mdMsg, adapter, cancellationToken);
		}
	}

	private async ValueTask ProcessPortfolioMessage(string portfolioName, OrderMessage message, CancellationToken cancellationToken)
	{
		var adapter = message.Adapter;

		if (adapter == null)
		{
			var (a, isPended) = await GetAdapter(portfolioName, message, cancellationToken);
			adapter = a;

			if (adapter == null)
			{
				if (isPended)
					return;

				LogDebug("No adapter for {0}", message);

				await SendOutMessageAsync(message.CreateReply(new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(message))), cancellationToken);
				return;
			}
		}

		if (message is OrderRegisterMessage regMsg)
			TryAddOrderAdapter(regMsg.TransactionId, adapter);

		await adapter.SendInMessageAsync(message, cancellationToken);
	}

	private async ValueTask ProcessOrderMessage(long transId, long originId, Message message, CancellationToken cancellationToken)
	{
		if (!_orderAdapters.TryGetValue(originId, out var adapter))
		{
			if (message is OrderMessage ordMsg && !ordMsg.PortfolioName.IsEmpty())
			{
				var (a, _) = await GetAdapter(ordMsg.PortfolioName, message, cancellationToken);
				adapter = a;
			}
		}

		ValueTask sendUnkTrans()
		{
			LogError(LocalizedStrings.UnknownTransactionId, originId);

			return SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				OriginalTransactionId = transId,
				Error = new InvalidOperationException(LocalizedStrings.UnknownTransactionId.Put(originId)),
			}, cancellationToken);
		}

		if (adapter is null)
		{
			await sendUnkTrans();
			return;
		}

		if (!_adapterWrappers.TryGetValue(adapter, out var wrapper))
		{
			await sendUnkTrans();
			return;
		}

		if (message is OrderReplaceMessage replace)
		{
			TryAddOrderAdapter(replace.TransactionId, adapter);
		}

		await wrapper.SendInMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Try find adapter by portfolio name.
	/// </summary>
	/// <param name="porfolioName">Portfolio name.</param>
	/// <param name="adapter"><see cref="IMessageAdapter"/></param>
	/// <returns>Found <see cref="IMessageAdapter"/>.</returns>
	public bool TryGetAdapter(string porfolioName, out IMessageAdapter adapter)
		=> _portfolioAdapters.TryGetValue(porfolioName, out adapter);

	private async ValueTask<(IMessageAdapter adapter, bool isPended)> GetAdapter(string portfolioName, Message message, CancellationToken cancellationToken)
	{
		if (portfolioName.IsEmpty())
			throw new ArgumentNullException(nameof(portfolioName));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (!TryGetAdapter(portfolioName, out var adapter))
		{
			var (adapters, isPended, _) = (await GetAdapters(message, cancellationToken));
			return (adapters.FirstOrDefault(), isPended);
		}
		else
		{
			var wrapper = _adapterWrappers.TryGetValue(adapter) ?? throw new InvalidOperationException(LocalizedStrings.ConnectionIsNotConnected.Put(adapter));

			return (wrapper, false);
		}
	}

	/// <summary>
	/// The embedded adapter event <see cref="IMessageAdapterWrapper.NewOutMessageAsync"/> handler.
	/// </summary>
	/// <param name="innerAdapter">The embedded adapter.</param>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	protected virtual async ValueTask OnInnerAdapterNewOutMessage(IMessageAdapter innerAdapter, Message message, CancellationToken cancellationToken)
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
					await ProcessConnectMessage(innerAdapter, (ConnectMessage)message, extra, cancellationToken);
					break;

				case MessageTypes.Disconnect:
					extra = [];
					await ProcessDisconnectMessage(innerAdapter, (DisconnectMessage)message, extra, cancellationToken);
					break;

				case MessageTypes.SubscriptionResponse:
					message = await ProcessSubscriptionResponse(innerAdapter, (SubscriptionResponseMessage)message, cancellationToken);
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
			await SendOutMessageAsync(message, cancellationToken);

		if (extra != null)
		{
			foreach (var m in extra)
				await SendOutMessageAsync(m, cancellationToken);
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

	private ValueTask SendOutErrorAsync(Exception error, CancellationToken cancellationToken)
	{
		return SendOutMessageAsync(error.ToErrorMessage(), cancellationToken);
	}

    void IMessageAdapter.SendOutMessage(Message message)
		=> AsyncHelper.Run(() => SendOutMessageAsync(message, default));

	/// <inheritdoc />
	public ValueTask SendOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return OnSendOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Send outgoing message and raise <see cref="NewOutMessageAsync"/> event.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	protected virtual ValueTask OnSendOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		message.Adapter ??= this;

		NewOutMessage?.Invoke(message);
		return NewOutMessageAsync?.Invoke(message, cancellationToken) ?? default;
	}

	private async ValueTask ProcessConnectMessage(IMessageAdapter innerAdapter, ConnectMessage message, List<Message> extra, CancellationToken cancellationToken)
	{
		var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);
		var wrapper = _adapterWrappers[underlyingAdapter];

		var error = message.Error;

		if (error != null)
			LogError(LocalizedStrings.ConnectionErrorFor, underlyingAdapter, error);
		else
			LogInfo("Connected to '{0}'.", underlyingAdapter);

		Message[] notSupportedMsgs = null;

		using (await _connectedResponseLock.LockAsync(cancellationToken))
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

				if (_adapterStates.Any(p => p.Value.state == ConnectionStates.Connected))
					extra.AddRange(pending.Select(m => m.LoopBack(this)));
				else
					notSupportedMsgs = pending;
			}
		}

		if (notSupportedMsgs != null)
		{
			foreach (var notSupportedMsg in notSupportedMsgs)
			{
				await SendOutErrorAsync(new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(notSupportedMsg.Type)), cancellationToken);
			}
		}

		message.Adapter = underlyingAdapter;
	}

	private async ValueTask ProcessDisconnectMessage(IMessageAdapter innerAdapter, DisconnectMessage message, List<Message> extra, CancellationToken cancellationToken)
	{
		var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);
		var wrapper = _adapterWrappers[underlyingAdapter];

		var error = message.Error;

		if (error == null)
			LogInfo("Disconnected from '{0}'.", underlyingAdapter);
		else
			LogError(LocalizedStrings.ErrorDisconnectFor, underlyingAdapter, error);

		using (await _connectedResponseLock.LockAsync(cancellationToken))
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
						var noPending = _adapterStates.All(v => v.Value.state != ConnectionStates.Connecting);

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
					var allFailed = _adapterStates.All(v => v.Value.state == ConnectionStates.Failed);

					if (allFailed)
					{
						var errors = _adapterStates.Select(v => v.Value.err).WhereNotNull().ToArray();
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

			var noPending = _adapterStates.All(v => v.Value.state is ConnectionStates.Disconnected or ConnectionStates.Failed);

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
			_subscriptions.Remove(originalTransactionId);
			return message;
		}

		if (!needParentResponse)
			return null;

		_subscriptions.Remove(parentId.Value);
		return new SubscriptionFinishedMessage { OriginalTransactionId = parentId.Value, Body = message.Body };
	}

	private async ValueTask<Message> ProcessSubscriptionResponse(IMessageAdapter adapter, SubscriptionResponseMessage message, CancellationToken cancellationToken)
	{
		var originalTransactionId = message.OriginalTransactionId;

		if (!_requestsById.TryGetValue(originalTransactionId, out var tuple))
			return message;

		var error = message.Error;
		var originMsg = tuple.subMsg;

		if (error != null)
		{
			LogWarning("Subscription Error out: {0}", message);
			_subscriptions.Remove(originalTransactionId);
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
				_subscriptions.Remove(parentId.Value);

			return needParentResponse
				? parentId.Value.CreateSubscriptionResponse(allError ? new AggregateException(LocalizedStrings.NoAdapterFoundFor.Put(originMsg), innerErrors) : null)
				: null;
		}
		else
		{
			if (!originMsg.IsSubscribe)
				_subscriptions.Remove(originMsg.OriginalTransactionId);
		}

		if (message.IsNotSupported() && originMsg is ISubscriptionMessage subscrMsg)
		{
			using (await _connectedResponseLock.LockAsync(cancellationToken))
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

	private void SecurityAdapterProviderOnChanged((SecurityId, DataType) t, Guid adapterId, bool changeType)
	{
		if (changeType)
		{
			var adapter = InnerAdapters.SyncGet(c => c.FindById(adapterId));

			if (adapter == null)
				_securityAdapters.Remove(t);
			else
				_securityAdapters[t] = adapter;
		}
		else
			_securityAdapters.Remove(t);
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
		using (InnerAdapters.EnterScope())
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
		using (InnerAdapters.EnterScope())
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
	public IMessageAdapter Clone()
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
}
