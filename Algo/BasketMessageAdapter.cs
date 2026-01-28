namespace StockSharp.Algo;

using StockSharp.Algo.Basket;
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

		public IEnumerable<IMessageAdapter> SortedAdapters
		{
			get
			{
				using var _ = EnterScope();

				return this
					.Select(a => (Adapter: a, Priority: _enables.TryGetValue(a, out var p) ? p : -1))
					.Where(x => x.Priority != -1)
					.OrderBy(x => x.Priority)
					.Select(x => x.Adapter);
			}
		}

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
				_parent._connectionState.RemoveAdapter(item);

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
				_parent._connectionState.Clear();

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

	private readonly CachedSynchronizedDictionary<IMessageAdapter, IMessageAdapter> _adapterWrappers = [];
	private readonly AsyncLock _connectedResponseLock = new();

	private readonly IAdapterConnectionState _connectionState;
	private readonly IAdapterConnectionManager _connectionManager;
	private readonly IAdapterWrapperPipelineBuilder _pipelineBuilder;
	private readonly IBasketRoutingManager _routingManager;

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
		: this(transactionIdGenerator, candleBuilderProvider, securityAdapterProvider, portfolioAdapterProvider, buffer,
			null, null, null, null, null, null, null, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketMessageAdapter"/> with injectable state dependencies.
	/// </summary>
	public BasketMessageAdapter(IdGenerator transactionIdGenerator,
		CandleBuilderProvider candleBuilderProvider,
		ISecurityMessageAdapterProvider securityAdapterProvider,
		IPortfolioMessageAdapterProvider portfolioAdapterProvider,
		IStorageBuffer buffer,
		IAdapterConnectionState connectionState,
		IAdapterConnectionManager connectionManager,
		IPendingMessageState pendingState,
		IPendingMessageManager pendingManager,
		ISubscriptionRoutingState subscriptionRouting,
		IParentChildMap parentChildMap,
		IOrderRoutingState orderRouting,
		IAdapterWrapperPipelineBuilder pipelineBuilder,
		IBasketRoutingManager routingManager = null)
	{
		TransactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
		_innerAdapters = new InnerAdapterList(this);
		SecurityAdapterProvider = securityAdapterProvider ?? throw new ArgumentNullException(nameof(securityAdapterProvider));
		PortfolioAdapterProvider = portfolioAdapterProvider ?? throw new ArgumentNullException(nameof(portfolioAdapterProvider));
		Buffer = buffer;
		StorageProcessor = new StorageProcessor(StorageSettings, candleBuilderProvider);

		LatencyManager = new LatencyManager(new LatencyManagerState());
		CommissionManager = new CommissionManager();
		//PnLManager = new PnLManager();
		SlippageManager = new SlippageManager(new SlippageManagerState());

		var cs = connectionState ?? new AdapterConnectionState();
		_connectionState = cs;
		_connectionManager = connectionManager ?? new AdapterConnectionManager(cs);
		var ps = pendingState ?? new PendingMessageState();
		var pm = pendingManager ?? new PendingMessageManager(ps);
		var sr = subscriptionRouting ?? new SubscriptionRoutingState();
		var pcm = parentChildMap ?? new ParentChildMap();
		var or = orderRouting ?? new OrderRoutingState();

		_pipelineBuilder = pipelineBuilder ?? new AdapterWrapperPipelineBuilder();
		_routingManager = routingManager ?? new Basket.BasketRoutingManager(
			cs, _connectionManager, ps, pm, sr, pcm, or,
			GetUnderlyingAdapter, StorageProcessor.CandleBuilderProvider,
			() => Level1Extend, transactionIdGenerator, this);

		SecurityAdapterProvider.Changed += SecurityAdapterProviderOnChanged;
		PortfolioAdapterProvider.Changed += PortfolioAdapterProviderOnChanged;
	}

	/// <summary>
	/// Gets the routing manager if one was injected.
	/// </summary>
	public IBasketRoutingManager RoutingManager => _routingManager;

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

	private INativeIdStorageProvider _nativeIdStorage = new InMemoryNativeIdStorageProvider();

	/// <summary>
	/// Security native identifier storage provider.
	/// </summary>
	public INativeIdStorageProvider NativeIdStorage
	{
		get => _nativeIdStorage;
		set => _nativeIdStorage = value ?? throw new ArgumentNullException(nameof(value));
	}

	private ISecurityMappingStorageProvider _mappingProvider;

	/// <summary>
	/// Security identifier mappings storage provider.
	/// </summary>
	public ISecurityMappingStorageProvider MappingProvider
	{
		get => _mappingProvider;
		set => _mappingProvider = value ?? throw new ArgumentNullException(nameof(value));
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

	bool IMessageAdapter.HeartbeatBeforeConnect => false;

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
	public bool ConnectDisconnectEventOnFirstAdapter
	{
		get => _connectionManager.ConnectDisconnectEventOnFirstAdapter;
		set => _connectionManager.ConnectDisconnectEventOnFirstAdapter = value;
	}

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

	private void TryAddOrderAdapter(long transId, IMessageAdapter adapter)
		=> _routingManager.AddOrderAdapter(transId, adapter);

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
			_routingManager.Reset(!isConnect);
	}

	private IMessageAdapter CreateWrappers(IMessageAdapter adapter)
	{
		var config = new AdapterWrapperConfiguration
		{
			SupportOffline = SupportOffline,
			IgnoreExtraAdapters = IgnoreExtraAdapters,
			SupportCandlesCompression = SupportCandlesCompression,
			SupportBuildingFromOrderLog = SupportBuildingFromOrderLog,
			SupportOrderBookTruncate = SupportOrderBookTruncate,
			SupportLookupTracking = SupportLookupTracking,
			IsSupportTransactionLog = IsSupportTransactionLog,
			SupportSecurityAll = SupportSecurityAll,
			SupportStorage = SupportStorage,
			GenerateOrderBookFromLevel1 = GenerateOrderBookFromLevel1,
			Level1Extend = Level1Extend,
			IsRestoreSubscriptionOnErrorReconnect = IsRestoreSubscriptionOnErrorReconnect,
			SuppressReconnectingErrors = SuppressReconnectingErrors,
			SendFinishedCandlesImmediatelly = SendFinishedCandlesImmediatelly,
			UseChannels = this.UseChannels(),
			LatencyManager = LatencyManager,
			SlippageManager = SlippageManager,
			PnLManager = PnLManager,
			CommissionManager = CommissionManager,
			NativeIdStorage = NativeIdStorage,
			MappingProvider = MappingProvider,
			ExtendedInfoStorage = ExtendedInfoStorage,
			StorageProcessor = StorageProcessor,
			Buffer = Buffer,
			FillGapsBehaviour = FillGapsBehaviour,
			IsHeartbeatOn = IsHeartbeatOn,
			SendOutErrorAsync = SendOutErrorAsync,
			Parent = this,
		};

		return _pipelineBuilder.Build(adapter, config);
	}

	private readonly Dictionary<IMessageAdapter, bool> _heartbeatFlags = [];

	private bool IsHeartbeatOn(IMessageAdapter adapter)
	{
		return _heartbeatFlags.TryGetValue2(adapter) ?? true;
	}

	/// <summary>
	/// Apply on/off heartbeat mode for the specified adapter.
	/// </summary>
	/// <param name="adapter">Adapter.</param>
	/// <param name="on">Is active.</param>
	public void ApplyHeartbeat(IMessageAdapter adapter, bool on)
	{
		_heartbeatFlags[adapter] = on;
	}

	private static IMessageAdapter GetUnderlyingAdapter(IMessageAdapter adapter)
	{
		if (adapter == null)
			throw new ArgumentNullException(nameof(adapter));

		if (adapter is IMessageAdapterWrapper wrapper)
		{
			return wrapper is IEmulationMessageAdapter
				? wrapper
				: GetUnderlyingAdapter(wrapper.InnerAdapter);
		}

		return adapter;
	}

	/// <inheritdoc />
	ValueTask IMessageTransport.SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		return OnSendInMessageAsync(message, cancellationToken);
	}

	private long[] GetSubscribers(DataType dataType)
		=> _routingManager.GetSubscribers(dataType);

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

				_connectionManager.BeginConnect();

				foreach (var adapter in GetSortedAdapters())
				{
					using (await _connectedResponseLock.LockAsync(cancellationToken))
						_connectionManager.InitializeAdapter(adapter);

					var wrapper = CreateWrappers(adapter);

					if (wrapper is IMessageAdapterWrapper adapterWrapper)
					{
						adapterWrapper.NewOutMessageAsync += (m, ct) => OnInnerAdapterNewOutMessage(adapter, m, ct);
					}
					else
					{
						wrapper.NewOutMessage += m => AsyncHelper.Run(() => OnInnerAdapterNewOutMessage(adapter, m, default));
					}

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
					_connectionManager.BeginDisconnect();

					adapters = _connectionState.GetAllStates()
						.Where(s => _adapterWrappers.ContainsKey(s.adapter) && (s.state == ConnectionStates.Connecting || s.state == ConnectionStates.Connected))
						.ToDictionary(s => _adapterWrappers[s.adapter], s =>
						{
							var underlying = s.adapter;
							_connectionState.SetAdapterState(underlying, ConnectionStates.Disconnecting, null);
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
			_routingManager.SubscriptionRouting.AddSubscription(subscrMsg.TransactionId, subscrMsg.TypedClone(), new[] { adapter }, subscrMsg.DataType);
			_routingManager.SubscriptionRouting.AddRequest(subscrMsg.TransactionId, subscrMsg, GetUnderlyingAdapter(adapter));
		}

		return adapter.SendInMessageAsync(message, cancellationToken);
	}

	private async ValueTask ProcessOtherMessage(Message message, CancellationToken cancellationToken)
	{
		if (message.Adapter != null)
		{
			await message.Adapter.SendInMessageAsync(message, cancellationToken);
			return;
		}

		var result = await _routingManager.ProcessInMessageAsync(message, a => _adapterWrappers.TryGetValue(a), cancellationToken);
		await ApplyRoutingResult(result, cancellationToken);
	}

	private async ValueTask ApplyRoutingResult(Basket.RoutingInResult result, CancellationToken cancellationToken)
	{
		// Send messages to adapters
		foreach (var (adapter, message) in result.RoutingDecisions)
		{
			LogDebug("Send to {0}: {1}", adapter, message);
			await adapter.SendInMessageAsync(message, cancellationToken);
		}

		// Send out messages (responses, errors)
		foreach (var outMsg in result.OutMessages)
			await SendOutMessageAsync(outMsg, cancellationToken);

		// Loop back messages
		foreach (var loopMsg in result.LoopbackMessages)
			await ((IMessageTransport)this).SendInMessageAsync(loopMsg.LoopBack(this), cancellationToken);
	}

	private async ValueTask ApplyRoutingOutResult(Basket.RoutingOutResult result, CancellationToken cancellationToken)
	{
		// Send transformed message
		if (result.TransformedMessage != null)
			await SendOutMessageAsync(result.TransformedMessage, cancellationToken);

		// Send extra messages
		foreach (var extra in result.ExtraMessages)
			await SendOutMessageAsync(extra, cancellationToken);

		// Loop back messages for retry
		foreach (var loopMsg in result.LoopbackMessages)
			await ((IMessageTransport)this).SendInMessageAsync(loopMsg.LoopBack(this), cancellationToken);
	}

	private async ValueTask ProcessMarketDataRequest(MarketDataMessage mdMsg, CancellationToken cancellationToken)
	{
		var result = await _routingManager.ProcessInMessageAsync(mdMsg, a => _adapterWrappers.TryGetValue(a), cancellationToken);
		await ApplyRoutingResult(result, cancellationToken);
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
		IMessageAdapter adapter = null;

		if (_routingManager.TryGetOrderAdapter(originId, out var orderAdapter))
			adapter = orderAdapter;

		if (adapter == null)
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
	{
		adapter = _routingManager.Router.GetPortfolioAdapter(porfolioName, a => a);
		return adapter != null;
	}

	private async ValueTask<(IMessageAdapter adapter, bool isPended)> GetAdapter(string portfolioName, Message message, CancellationToken cancellationToken)
	{
		if (portfolioName.IsEmpty())
			throw new ArgumentNullException(nameof(portfolioName));

		if (message == null)
			throw new ArgumentNullException(nameof(message));

		// check if portfolio has a dedicated adapter
		var underlyingAdapter = _routingManager.Router.GetPortfolioAdapter(portfolioName, a => a);

		if (underlyingAdapter == null)
		{
			// Use routing manager to find adapter
			var result = await _routingManager.ProcessInMessageAsync(message, a => _adapterWrappers.TryGetValue(a), cancellationToken);
			if (result.IsPended)
				return (null, true);
			return (result.RoutingDecisions.FirstOrDefault().Adapter, false);
		}
		else
		{
			var wrapper = _adapterWrappers.TryGetValue(underlyingAdapter) ?? throw new InvalidOperationException(LocalizedStrings.ConnectionIsNotConnected.Put(underlyingAdapter));

			return (wrapper, false);
		}
	}

	/// <summary>
	/// The embedded adapter event <see cref="IMessageTransport.NewOutMessageAsync"/> handler.
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
				case MessageTypes.SubscriptionFinished:
				case MessageTypes.SubscriptionOnline:
				{
					var result = await _routingManager.ProcessOutMessageAsync(innerAdapter, message, a => _adapterWrappers.TryGetValue(a), cancellationToken);
					await ApplyRoutingOutResult(result, cancellationToken);
					return;
				}

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
					var result = await _routingManager.ProcessOutMessageAsync(innerAdapter, message, a => _adapterWrappers.TryGetValue(a), cancellationToken);
					await ApplyRoutingOutResult(result, cancellationToken);
					return;
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
			if (!_routingManager.ParentChildMap.TryGetParent(ids[i], out var parentId))
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
				_routingManager.RegisterAdapterMessageTypes(wrapper, innerAdapter.SupportedInMessages);

			var msgs = _connectionManager.ProcessConnect(underlyingAdapter, error);
			extra.AddRange(msgs);

			if (!_connectionState.HasPendingAdapters)
			{
				var pending = _routingManager.PendingState.GetAndClear();

				if (_connectionState.ConnectedCount > 0)
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
			_routingManager.UnregisterAdapterMessageTypes(wrapper, innerAdapter.SupportedInMessages);

			var msgs = _connectionManager.ProcessDisconnect(underlyingAdapter, error);
			extra.AddRange(msgs);
		}

		message.Adapter = underlyingAdapter;
	}

	private void SecurityAdapterProviderOnChanged((SecurityId, DataType) t, Guid adapterId, bool changeType)
	{
		if (changeType)
		{
			var adapter = InnerAdapters.SyncGet(c => c.FindById(adapterId));

			if (adapter == null)
				_routingManager.Router.RemoveSecurityAdapter(t.Item1, t.Item2);
			else
				_routingManager.Router.SetSecurityAdapter(t.Item1, t.Item2, adapter);
		}
		else
			_routingManager.Router.RemoveSecurityAdapter(t.Item1, t.Item2);
	}

	private void PortfolioAdapterProviderOnChanged(string key, Guid adapterId, bool changeType)
	{
		if (changeType)
		{
			var adapter = InnerAdapters.SyncGet(c => c.FindById(adapterId));

			if (adapter == null)
				_routingManager.Router.RemovePortfolioAdapter(key);
			else
				_routingManager.Router.SetPortfolioAdapter(key, adapter);
		}
		else
			_routingManager.Router.RemovePortfolioAdapter(key);
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

			_routingManager.Router.ClearSecurityAdapters();

			foreach (var pair in SecurityAdapterProvider.Adapters)
			{
				if (!adapters.TryGetValue(pair.Value, out var adapter))
					continue;

				_routingManager.Router.AddSecurityAdapter(pair.Key, adapter);
			}

			_routingManager.Router.ClearPortfolioAdapters();

			foreach (var pair in PortfolioAdapterProvider.Adapters)
			{
				if (!adapters.TryGetValue(pair.Value, out var adapter))
					continue;

				_routingManager.Router.AddPortfolioAdapter(pair.Key, adapter);
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
