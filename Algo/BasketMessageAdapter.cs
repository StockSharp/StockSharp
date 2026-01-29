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
				_parent._routingManager?.OnAdapterRemoved(item);

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
				_parent._routingManager?.OnAdaptersCleared();

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

	private readonly IAdapterWrapperPipelineBuilder _pipelineBuilder;
	private readonly IBasketRoutingManager _routingManager;

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketMessageAdapter"/>.
	/// </summary>
	public BasketMessageAdapter(IdGenerator transactionIdGenerator,
		CandleBuilderProvider candleBuilderProvider,
		ISecurityMessageAdapterProvider securityAdapterProvider,
		IPortfolioMessageAdapterProvider portfolioAdapterProvider,
		IStorageBuffer buffer)
		: this(transactionIdGenerator, candleBuilderProvider, securityAdapterProvider, portfolioAdapterProvider, buffer, null, null)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BasketMessageAdapter"/> with injectable dependencies.
	/// </summary>
	public BasketMessageAdapter(IdGenerator transactionIdGenerator,
		CandleBuilderProvider candleBuilderProvider,
		ISecurityMessageAdapterProvider securityAdapterProvider,
		IPortfolioMessageAdapterProvider portfolioAdapterProvider,
		IStorageBuffer buffer,
		IAdapterWrapperPipelineBuilder pipelineBuilder,
		IBasketRoutingManager routingManager)
	{
		TransactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
		_innerAdapters = new InnerAdapterList(this);
		SecurityAdapterProvider = securityAdapterProvider ?? throw new ArgumentNullException(nameof(securityAdapterProvider));
		PortfolioAdapterProvider = portfolioAdapterProvider ?? throw new ArgumentNullException(nameof(portfolioAdapterProvider));
		Buffer = buffer;
		StorageProcessor = new StorageProcessor(StorageSettings, candleBuilderProvider);

		LatencyManager = new LatencyManager(new LatencyManagerState());
		CommissionManager = new CommissionManager();
		SlippageManager = new SlippageManager(new SlippageManagerState());

		_pipelineBuilder = pipelineBuilder ?? new AdapterWrapperPipelineBuilder();
		_routingManager = routingManager ?? Basket.BasketRoutingManager.CreateDefault(
			GetUnderlyingAdapter, StorageProcessor.CandleBuilderProvider,
			() => Level1Extend, transactionIdGenerator, this);

		SecurityAdapterProvider.Changed += SecurityAdapterProviderOnChanged;
		PortfolioAdapterProvider.Changed += PortfolioAdapterProviderOnChanged;
	}

	/// <summary>
	/// Gets the routing manager.
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

    IAsyncEnumerable<DataType> IMessageAdapter.GetSupportedMarketDataTypesAsync(SecurityId securityId, DateTime? from, DateTime? to)
    {
		return Impl().Distinct();

		async IAsyncEnumerable<DataType> Impl([EnumeratorCancellation]CancellationToken cancellationToken = default)
		{
			foreach (var adapter in GetSortedAdapters())
			{
				await foreach (var date in adapter.GetSupportedMarketDataTypesAsync(securityId, from, to).WithEnforcedCancellation(cancellationToken))
					yield return date;
			}
		}
    }

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
		get => _routingManager.ConnectDisconnectEventOnFirstAdapter;
		set => _routingManager.ConnectDisconnectEventOnFirstAdapter = value;
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
	/// To get adapters <see cref="IInnerAdapterList.SortedAdapters"/> sorted by the specified priority.
	/// </summary>
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

	private async ValueTask ProcessReset(ResetMessage message, bool isConnect, CancellationToken cancellationToken)
	{
		await Wrappers.Select(async a =>
		{
			a.TryRemoveWrapper<ChannelMessageAdapter>()?.Dispose();
			await a.SendInMessageAsync(message, cancellationToken);
			a.Dispose();
		}).WhenAll();

		_adapterWrappers.Clear();

		using (await _connectedResponseLock.LockAsync(cancellationToken))
			_routingManager.Reset(!isConnect);
	}

	private ValueTask<IMessageAdapter> CreateWrappers(IMessageAdapter adapter, CancellationToken cancellationToken)
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

		return _pipelineBuilder.BuildAsync(adapter, config, cancellationToken);
	}

	private readonly Dictionary<IMessageAdapter, bool> _heartbeatFlags = [];

	private bool IsHeartbeatOn(IMessageAdapter adapter)
		=> _heartbeatFlags.TryGetValue2(adapter) ?? true;

	/// <summary>
	/// Apply on/off heartbeat mode for the specified adapter.
	/// </summary>
	public void ApplyHeartbeat(IMessageAdapter adapter, bool on)
		=> _heartbeatFlags[adapter] = on;

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
		=> OnSendInMessageAsync(message, cancellationToken);

	/// <summary>
	/// Send message.
	/// </summary>
	protected virtual ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		try
		{
			return InternalSendInMessage(message, cancellationToken);
		}
		catch (Exception ex)
		{
			return SendOutMessageAsync(message.CreateErrorResponse(ex, this, _routingManager.GetSubscribers), cancellationToken);
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
				message.UndoBack();
			else
			{
				await ProcessBackMessage(adapter, message, cancellationToken);
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
				await ProcessConnect(message, cancellationToken);
				break;

			case MessageTypes.Disconnect:
				await ProcessDisconnect(message, cancellationToken);
				break;

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
				var result = await _routingManager.ProcessInMessageAsync(message, a => _adapterWrappers.TryGetValue(a), cancellationToken);
				await ApplyRoutingResult(result, cancellationToken);
				break;
			}

			default:
				await ProcessOtherMessage(message, cancellationToken);
				break;
		}
	}

	private async ValueTask ProcessConnect(Message message, CancellationToken cancellationToken)
	{
		await ProcessReset(new ResetMessage(), true, cancellationToken);

		_routingManager.BeginConnect();

		foreach (var adapter in GetSortedAdapters())
		{
			using (await _connectedResponseLock.LockAsync(cancellationToken))
				_routingManager.InitializeAdapter(adapter);

			var wrapper = await CreateWrappers(adapter, cancellationToken);

			wrapper.NewOutMessageAsync += (m, ct) => OnInnerAdapterNewOutMessage(adapter, m, ct);

			_adapterWrappers.Add(adapter, wrapper);
		}

		if (Wrappers.Length == 0)
			throw new InvalidOperationException(LocalizedStrings.AtLeastOneConnectionMustBe);

		await Wrappers.Select(w =>
		{
			LogInfo("Connecting '{0}'.", GetUnderlyingAdapter(w));
			return w.SendInMessageAsync(message, cancellationToken);
		}).WhenAll();
	}

	private async ValueTask ProcessDisconnect(Message message, CancellationToken cancellationToken)
	{
		IDictionary<IMessageAdapter, IMessageAdapter> adapters;

		using (await _connectedResponseLock.LockAsync(cancellationToken))
		{
			_routingManager.BeginDisconnect();
			adapters = _routingManager.GetAdaptersToDisconnect(a => _adapterWrappers.TryGetValue(a));
		}

		await adapters.Select(a =>
		{
			LogInfo("Disconnecting '{0}'.", a.Value);
			return a.Key.SendInMessageAsync(message, cancellationToken);
		}).WhenAll();
	}

	private ValueTask ProcessBackMessage(IMessageAdapter adapter, Message message, CancellationToken cancellationToken)
	{
		if (message.BackMode == MessageBackModes.Chain)
			adapter = _adapterWrappers[GetUnderlyingAdapter(adapter)];

		if (message is ISubscriptionMessage subscrMsg)
			_routingManager.ProcessBackMessage(adapter, subscrMsg, a => _adapterWrappers.TryGetValue(a));

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
		foreach (var (adapter, msg) in result.RoutingDecisions)
		{
			LogDebug("Send to {0}: {1}", adapter, msg);
			await adapter.SendInMessageAsync(msg, cancellationToken);
		}

		foreach (var outMsg in result.OutMessages)
			await SendOutMessageAsync(outMsg, cancellationToken);

		foreach (var loopMsg in result.LoopbackMessages)
			await ((IMessageTransport)this).SendInMessageAsync(loopMsg.LoopBack(this), cancellationToken);
	}

	private async ValueTask ApplyRoutingOutResult(Basket.RoutingOutResult result, CancellationToken cancellationToken)
	{
		if (result.TransformedMessage != null)
			await SendOutMessageAsync(result.TransformedMessage, cancellationToken);

		foreach (var extra in result.ExtraMessages)
			await SendOutMessageAsync(extra, cancellationToken);

		foreach (var loopMsg in result.LoopbackMessages)
			await ((IMessageTransport)this).SendInMessageAsync(loopMsg.LoopBack(this), cancellationToken);
	}

	private async ValueTask ProcessPortfolioMessage(string portfolioName, OrderMessage message, CancellationToken cancellationToken)
	{
		var adapter = message.Adapter;

		if (adapter == null)
		{
			var underlyingAdapter = _routingManager.GetPortfolioAdapter(portfolioName, a => a);

			if (underlyingAdapter != null)
			{
				adapter = _adapterWrappers.TryGetValue(underlyingAdapter)
					?? throw new InvalidOperationException(LocalizedStrings.ConnectionIsNotConnected.Put(underlyingAdapter));
			}
			else
			{
				var result = await _routingManager.ProcessInMessageAsync(message, a => _adapterWrappers.TryGetValue(a), cancellationToken);
				if (result.IsPended)
					return;

				adapter = result.RoutingDecisions.FirstOrDefault().Adapter;
			}

			if (adapter == null)
			{
				LogDebug("No adapter for {0}", message);
				await SendOutMessageAsync(message.CreateReply(new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(message))), cancellationToken);
				return;
			}
		}

		if (message is OrderRegisterMessage regMsg)
			_routingManager.AddOrderAdapter(regMsg.TransactionId, adapter);

		await adapter.SendInMessageAsync(message, cancellationToken);
	}

	private async ValueTask ProcessOrderMessage(long transId, long originId, Message message, CancellationToken cancellationToken)
	{
		IMessageAdapter adapter = null;

		if (_routingManager.TryGetOrderAdapter(originId, out var orderAdapter))
			adapter = orderAdapter;

		if (adapter == null && message is OrderMessage ordMsg && !ordMsg.PortfolioName.IsEmpty())
		{
			var underlyingAdapter = _routingManager.GetPortfolioAdapter(ordMsg.PortfolioName, a => a);
			if (underlyingAdapter != null)
				adapter = _adapterWrappers.TryGetValue(underlyingAdapter);
		}

		if (adapter is null || !_adapterWrappers.TryGetValue(adapter, out var wrapper))
		{
			LogError(LocalizedStrings.UnknownTransactionId, originId);
			await SendOutMessageAsync(new ExecutionMessage
			{
				DataTypeEx = DataType.Transactions,
				HasOrderInfo = true,
				OriginalTransactionId = transId,
				Error = new InvalidOperationException(LocalizedStrings.UnknownTransactionId.Put(originId)),
			}, cancellationToken);
			return;
		}

		if (message is OrderReplaceMessage replace)
			_routingManager.AddOrderAdapter(replace.TransactionId, adapter);

		await wrapper.SendInMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Try find adapter by portfolio name.
	/// </summary>
	public bool TryGetAdapter(string porfolioName, out IMessageAdapter adapter)
	{
		adapter = _routingManager.GetPortfolioAdapter(porfolioName, a => a);
		return adapter != null;
	}

	/// <inheritdoc />
	public event Func<Message, CancellationToken, ValueTask> NewOutMessageAsync;

	/// <summary>
	/// The embedded adapter event handler.
	/// </summary>
	protected virtual async ValueTask OnInnerAdapterNewOutMessage(IMessageAdapter innerAdapter, Message message, CancellationToken cancellationToken)
	{
		List<Message> extra = null;

		if (!message.IsBack())
		{
			message.Adapter ??= innerAdapter;

			switch (message.Type)
			{
				case MessageTypes.Time:
					return;

				case MessageTypes.Connect:
					extra = [];
					await ProcessConnectResponse(innerAdapter, (ConnectMessage)message, extra, cancellationToken);
					break;

				case MessageTypes.Disconnect:
					extra = [];
					await ProcessDisconnectResponse(innerAdapter, (DisconnectMessage)message, extra, cancellationToken);
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
				case MessageTypes.PositionChange:
				{
					var pfMsg = (IPortfolioNameMessage)message;
					_routingManager.ApplyParentLookupId((ISubscriptionIdMessage)message);
					PortfolioAdapterProvider.SetAdapter(pfMsg.PortfolioName, GetUnderlyingAdapter(innerAdapter));
					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					_routingManager.ApplyParentLookupId(secMsg);
					SecurityAdapterProvider.SetAdapter(secMsg.SecurityId, null, GetUnderlyingAdapter(innerAdapter).Id);
					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					if (execMsg.DataType != DataType.Transactions)
						break;

					_routingManager.ApplyParentLookupId(execMsg);

					if (execMsg.TransactionId != default && execMsg.HasOrderInfo)
						_routingManager.AddOrderAdapter(execMsg.TransactionId, innerAdapter);

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

	private async ValueTask ProcessConnectResponse(IMessageAdapter innerAdapter, ConnectMessage message, List<Message> extra, CancellationToken cancellationToken)
	{
		var underlyingAdapter = GetUnderlyingAdapter(innerAdapter);
		var wrapper = _adapterWrappers[underlyingAdapter];
		var error = message.Error;

		if (error != null)
			LogError(LocalizedStrings.ConnectionErrorFor, underlyingAdapter, error);
		else
			LogInfo("Connected to '{0}'.", underlyingAdapter);

		Message[] notSupportedMsgs;

		using (await _connectedResponseLock.LockAsync(cancellationToken))
		{
			var (outMsgs, pendingToLoopback, notSupported) = _routingManager.ProcessConnect(
				underlyingAdapter, wrapper, innerAdapter.SupportedInMessages, error);

			extra.AddRange(outMsgs);
			extra.AddRange(pendingToLoopback.Select(m => m.LoopBack(this)));
			notSupportedMsgs = notSupported;
		}

		foreach (var notSupportedMsg in notSupportedMsgs)
			await SendOutErrorAsync(new InvalidOperationException(LocalizedStrings.NoAdapterFoundFor.Put(notSupportedMsg.Type)), cancellationToken);

		message.Adapter = underlyingAdapter;
	}

	private async ValueTask ProcessDisconnectResponse(IMessageAdapter innerAdapter, DisconnectMessage message, List<Message> extra, CancellationToken cancellationToken)
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
			var msgs = _routingManager.ProcessDisconnect(underlyingAdapter, wrapper, innerAdapter.SupportedInMessages, error);
			extra.AddRange(msgs);
		}

		message.Adapter = underlyingAdapter;
	}

	private ValueTask SendOutErrorAsync(Exception error, CancellationToken cancellationToken)
		=> SendOutMessageAsync(error.ToErrorMessage(), cancellationToken);

	/// <inheritdoc />
	public ValueTask SendOutMessageAsync(Message message, CancellationToken cancellationToken)
		=> OnSendOutMessageAsync(message, cancellationToken);

	/// <summary>
	/// Send outgoing message and raise <see cref="NewOutMessageAsync"/> event.
	/// </summary>
	protected virtual ValueTask OnSendOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		message.Adapter ??= this;
		return NewOutMessageAsync?.Invoke(message, cancellationToken) ?? default;
	}

	private void SecurityAdapterProviderOnChanged((SecurityId, DataType) key, Guid adapterId, bool isAdd)
		=> _routingManager.OnSecurityAdapterProviderChanged(key, adapterId, isAdd,
			id => InnerAdapters.SyncGet(c => c.FindById(id)));

	private void PortfolioAdapterProviderOnChanged(string portfolioName, Guid adapterId, bool isAdd)
		=> _routingManager.OnPortfolioAdapterProviderChanged(portfolioName, adapterId, isAdd,
			id => InnerAdapters.SyncGet(c => c.FindById(id)));

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

			_routingManager.LoadSecurityAdapters(
				SecurityAdapterProvider.Adapters
					.Where(p => adapters.ContainsKey(p.Value))
					.Select(p => (p.Key, adapters[p.Value])));

			_routingManager.LoadPortfolioAdapters(
				PortfolioAdapterProvider.Adapters
					.Where(p => adapters.ContainsKey(p.Value))
					.Select(p => (p.Key, adapters[p.Value])));
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
