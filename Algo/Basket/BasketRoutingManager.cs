namespace StockSharp.Algo.Basket;

using StockSharp.Algo.Candles;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Default implementation of <see cref="IBasketRoutingManager"/>.
/// Manages message routing logic for basket adapter.
/// </summary>
public class BasketRoutingManager : IBasketRoutingManager
{
	private readonly AsyncLock _lock = new();
	private readonly Func<IMessageAdapter, IMessageAdapter> _getUnderlyingAdapter;
	private readonly IdGenerator _transactionIdGenerator;
	private readonly ILogReceiver _logReceiver;

	private readonly IAdapterConnectionState _connectionState;
	private readonly IAdapterConnectionManager _connectionManager;
	private readonly IPendingMessageState _pendingState;
	private readonly ISubscriptionRoutingState _subscriptionRouting;
	private readonly IParentChildMap _parentChildMap;
	private readonly IAdapterRouter _router;

	/// <summary>
	/// Creates a new instance of <see cref="BasketRoutingManager"/> with default state components.
	/// </summary>
	public static BasketRoutingManager CreateDefault(
		Func<IMessageAdapter, IMessageAdapter> getUnderlyingAdapter,
		CandleBuilderProvider candleBuilderProvider,
		Func<bool> levelExtend,
		IdGenerator transactionIdGenerator,
		ILogReceiver logReceiver = null)
	{
		var cs = new AdapterConnectionState();
		var cm = new AdapterConnectionManager(cs);
		var ps = new PendingMessageState();
		var sr = new SubscriptionRoutingState();
		var pcm = new ParentChildMap();
		var or = new OrderRoutingState();

		return new BasketRoutingManager(cs, cm, ps, sr, pcm, or,
			getUnderlyingAdapter, candleBuilderProvider, levelExtend, transactionIdGenerator, logReceiver);
	}

	/// <summary>
	/// Initializes a new instance of <see cref="BasketRoutingManager"/>.
	/// </summary>
	public BasketRoutingManager(
		IAdapterConnectionState connectionState,
		IAdapterConnectionManager connectionManager,
		IPendingMessageState pendingState,
		ISubscriptionRoutingState subscriptionRouting,
		IParentChildMap parentChildMap,
		IOrderRoutingState orderRouting,
		Func<IMessageAdapter, IMessageAdapter> getUnderlyingAdapter,
		CandleBuilderProvider candleBuilderProvider,
		Func<bool> levelExtend,
		IdGenerator transactionIdGenerator,
		ILogReceiver logReceiver = null,
		IAdapterRouter router = null)
	{
		_connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
		_connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
		_pendingState = pendingState ?? throw new ArgumentNullException(nameof(pendingState));
		_subscriptionRouting = subscriptionRouting ?? throw new ArgumentNullException(nameof(subscriptionRouting));
		_parentChildMap = parentChildMap ?? throw new ArgumentNullException(nameof(parentChildMap));
		_getUnderlyingAdapter = getUnderlyingAdapter ?? throw new ArgumentNullException(nameof(getUnderlyingAdapter));
		_transactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
		_logReceiver = logReceiver;

		_router = router ?? new AdapterRouter(orderRouting, getUnderlyingAdapter, candleBuilderProvider, levelExtend);
	}

	#region Connection Management

	/// <inheritdoc />
	public bool ConnectDisconnectEventOnFirstAdapter
	{
		get => _connectionManager.ConnectDisconnectEventOnFirstAdapter;
		set => _connectionManager.ConnectDisconnectEventOnFirstAdapter = value;
	}

	/// <inheritdoc />
	public bool HasPendingAdapters => _connectionState.HasPendingAdapters;

	/// <inheritdoc />
	public int ConnectedCount => _connectionState.ConnectedCount;

	/// <inheritdoc />
	public void BeginConnect() => _connectionManager.BeginConnect();

	/// <inheritdoc />
	public void InitializeAdapter(IMessageAdapter adapter) => _connectionManager.InitializeAdapter(adapter);

	/// <inheritdoc />
	public (IEnumerable<Message> outMessages, Message[] pendingToLoopback, Message[] notSupportedMsgs) ProcessConnect(
		IMessageAdapter adapter,
		IMessageAdapter wrapper,
		IEnumerable<MessageTypes> supportedMessages,
		Exception error)
	{
		Message[] pendingToLoopback = null;
		Message[] notSupportedMsgs = null;

		if (error == null)
		{
			foreach (var type in supportedMessages)
				_router.AddMessageTypeAdapter(type, wrapper);
		}

		var outMsgs = _connectionManager.ProcessConnect(adapter, error);

		if (!_connectionState.HasPendingAdapters)
		{
			var pending = _pendingState.GetAndClear();

			if (_connectionState.ConnectedCount > 0)
				pendingToLoopback = pending;
			else
				notSupportedMsgs = pending;
		}

		return (outMsgs, pendingToLoopback ?? [], notSupportedMsgs ?? []);
	}

	/// <inheritdoc />
	public void BeginDisconnect() => _connectionManager.BeginDisconnect();

	/// <inheritdoc />
	public IDictionary<IMessageAdapter, IMessageAdapter> GetAdaptersToDisconnect(Func<IMessageAdapter, IMessageAdapter> adapterLookup)
	{
		return _connectionState.GetAllStates()
			.Where(s => adapterLookup(s.adapter) != null &&
				(s.state == ConnectionStates.Connecting || s.state == ConnectionStates.Connected))
			.ToDictionary(
				s => adapterLookup(s.adapter),
				s =>
				{
					_connectionState.SetAdapterState(s.adapter, ConnectionStates.Disconnecting, null);
					return s.adapter;
				});
	}

	/// <inheritdoc />
	public IEnumerable<Message> ProcessDisconnect(
		IMessageAdapter adapter,
		IMessageAdapter wrapper,
		IEnumerable<MessageTypes> supportedMessages,
		Exception error)
	{
		foreach (var type in supportedMessages)
			_router.RemoveMessageTypeAdapter(type, wrapper);

		return _connectionManager.ProcessDisconnect(adapter, error);
	}

	#endregion

	#region Adapter List Management

	/// <inheritdoc />
	public void OnAdapterRemoved(IMessageAdapter adapter)
		=> _connectionState.RemoveAdapter(adapter);

	/// <inheritdoc />
	public void OnAdaptersCleared()
		=> _connectionState.Clear();

	#endregion

	#region Order Routing

	/// <inheritdoc />
	public void AddOrderAdapter(long transactionId, IMessageAdapter adapter)
		=> _router.AddOrderAdapter(transactionId, adapter);

	/// <inheritdoc />
	public bool TryGetOrderAdapter(long transactionId, out IMessageAdapter adapter)
		=> _router.TryGetOrderAdapter(transactionId, out adapter);

	/// <inheritdoc />
	public IMessageAdapter GetPortfolioAdapter(string portfolioName, Func<IMessageAdapter, IMessageAdapter> adapterLookup)
		=> _router.GetPortfolioAdapter(portfolioName, adapterLookup);

	#endregion

	#region Subscriptions

	/// <inheritdoc />
	public long[] GetSubscribers(DataType dataType)
		=> _subscriptionRouting.GetSubscribers(dataType);

	/// <inheritdoc />
	public bool ApplyParentLookupId(ISubscriptionIdMessage msg)
	{
		if (msg == null)
			throw new ArgumentNullException(nameof(msg));

		var originIds = msg.GetSubscriptionIds();

		if (originIds.Length == 0)
			return true;

		var ids = originIds;
		var changed = false;
		var hasValidId = false;

		for (var i = 0; i < ids.Length; i++)
		{
			if (!_parentChildMap.TryGetParent(ids[i], out var parentId))
				continue;

			hasValidId = true;

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

		// If message had subscription IDs but none mapped to valid parents,
		// the subscription was removed (unsubscribed) — drop the message
		return hasValidId || originIds.Length == 0;
	}

	/// <inheritdoc />
	public void ProcessBackMessage(IMessageAdapter adapter, ISubscriptionMessage subscrMsg, Func<IMessageAdapter, IMessageAdapter> adapterLookup)
	{
		var wrapper = adapterLookup(_getUnderlyingAdapter(adapter));
		_subscriptionRouting.AddSubscription(subscrMsg.TransactionId, subscrMsg.TypedClone(), [wrapper], subscrMsg.DataType);
		_subscriptionRouting.AddRequest(subscrMsg.TransactionId, subscrMsg, _getUnderlyingAdapter(wrapper));
	}

	#endregion

	#region Provider Change Handlers

	/// <inheritdoc />
	public void OnSecurityAdapterProviderChanged(
		(SecurityId, DataType) key,
		Guid adapterId,
		bool isAdd,
		Func<Guid, IMessageAdapter> findAdapter)
	{
		if (isAdd)
		{
			var adapter = findAdapter(adapterId);
			if (adapter == null)
				_router.RemoveSecurityAdapter(key.Item1, key.Item2);
			else
				_router.SetSecurityAdapter(key.Item1, key.Item2, adapter);
		}
		else
			_router.RemoveSecurityAdapter(key.Item1, key.Item2);
	}

	/// <inheritdoc />
	public void OnPortfolioAdapterProviderChanged(
		string portfolioName,
		Guid adapterId,
		bool isAdd,
		Func<Guid, IMessageAdapter> findAdapter)
	{
		if (isAdd)
		{
			var adapter = findAdapter(adapterId);
			if (adapter == null)
				_router.RemovePortfolioAdapter(portfolioName);
			else
				_router.SetPortfolioAdapter(portfolioName, adapter);
		}
		else
			_router.RemovePortfolioAdapter(portfolioName);
	}

	#endregion

	#region Load/Save

	/// <inheritdoc />
	public void LoadSecurityAdapters(IEnumerable<((SecurityId, DataType) key, IMessageAdapter adapter)> mappings)
	{
		_router.ClearSecurityAdapters();

		foreach (var (key, adapter) in mappings)
			_router.AddSecurityAdapter(key, adapter);
	}

	/// <inheritdoc />
	public void LoadPortfolioAdapters(IEnumerable<(string portfolio, IMessageAdapter adapter)> mappings)
	{
		_router.ClearPortfolioAdapters();

		foreach (var (portfolio, adapter) in mappings)
			_router.AddPortfolioAdapter(portfolio, adapter);
	}

	#endregion

	#region Reset

	/// <inheritdoc />
	public void Reset(bool clearPending)
	{
		_router.Clear();

		if (clearPending)
			_pendingState.Clear();

		_connectionManager.Reset();
		_subscriptionRouting.Clear();
		_parentChildMap.Clear();
	}

	#endregion

	#region Message Processing

	/// <inheritdoc />
	public async ValueTask<RoutingInResult> ProcessInMessageAsync(
		Message message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.MarketData:
				return await ProcessMarketDataRequestAsync((MarketDataMessage)message, adapterLookup, cancellationToken);

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			case MessageTypes.OrderGroupCancel:
				return await ProcessOtherMessageAsync(message, adapterLookup, cancellationToken);

			default:
				if (message is ISubscriptionMessage subscrMsg)
					return await ProcessSubscriptionMessageAsync(subscrMsg, adapterLookup, cancellationToken);

				return await ProcessOtherMessageAsync(message, adapterLookup, cancellationToken);
		}
	}

	private async ValueTask<RoutingInResult> ProcessMarketDataRequestAsync(
		MarketDataMessage mdMsg,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken)
	{
		IMessageAdapter[] adapters;

		if (mdMsg.IsSubscribe)
		{
			var (a, isPended) = await GetSubscriptionAdaptersAsync(mdMsg, adapterLookup, cancellationToken);

			if (isPended)
				return RoutingInResult.Pended();

			adapters = a;

			if (adapters.Length == 0)
				return RoutingInResult.WithOutMessage(mdMsg.TransactionId.CreateNotSupported());

			_subscriptionRouting.AddSubscription(mdMsg.TransactionId, (ISubscriptionMessage)mdMsg.Clone(), adapters, mdMsg.DataType2);
		}
		else
		{
			adapters = null;

			var originTransId = mdMsg.OriginalTransactionId;

			if (!_subscriptionRouting.TryGetSubscription(originTransId, out _, out _, out _))
			{
				using (await _lock.LockAsync(cancellationToken))
				{
					var suspended = _pendingState.TryRemoveMarketData(originTransId);

					if (suspended != null)
						return RoutingInResult.WithOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = mdMsg.TransactionId });
				}

				LogInfo("Unsubscribe not found: {0}/{1}", originTransId, mdMsg);
				return RoutingInResult.CreateHandled();
			}
		}

		var routing = ToChild(mdMsg, adapters);
		var decisions = routing
			.Select(pair => (pair.Value, (Message)pair.Key))
			.ToList();

		foreach (var (msg, adapter) in routing)
		{
			_subscriptionRouting.AddRequest(msg.TransactionId, msg, _getUnderlyingAdapter(adapter));
		}

		return RoutingInResult.WithRouting(decisions);
	}

	private async ValueTask<RoutingInResult> ProcessSubscriptionMessageAsync(
		ISubscriptionMessage subscrMsg,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken)
	{
		if (subscrMsg is Message msg && msg.Adapter != null)
		{
			var adapter = adapterLookup(msg.Adapter);
			if (adapter != null)
			{
				_subscriptionRouting.AddSubscription(subscrMsg.TransactionId, subscrMsg.TypedClone(), [adapter], subscrMsg.DataType);
				_subscriptionRouting.AddRequest(subscrMsg.TransactionId, subscrMsg, _getUnderlyingAdapter(adapter));
				return RoutingInResult.WithRouting([(adapter, msg)]);
			}
		}

		IMessageAdapter[] adapters;

		if (subscrMsg.IsSubscribe)
		{
			var (a, isPended, _) = await GetAdaptersAsync((Message)subscrMsg, adapterLookup, cancellationToken);
			adapters = a;

			if (isPended)
				return RoutingInResult.Pended();

			if (adapters.Length == 0)
				return RoutingInResult.WithOutMessage(subscrMsg.CreateResult());

			_subscriptionRouting.AddSubscription(subscrMsg.TransactionId, subscrMsg.TypedClone(), adapters, subscrMsg.DataType);
		}
		else
			adapters = null;

		var routing = ToChild(subscrMsg, adapters);
		var decisions = routing
			.Select(pair => (pair.Value, (Message)pair.Key))
			.ToList();

		foreach (var (msg2, adapter) in routing)
		{
			_subscriptionRouting.AddRequest(msg2.TransactionId, msg2, _getUnderlyingAdapter(adapter));
		}

		return RoutingInResult.WithRouting(decisions);
	}

	private async ValueTask<RoutingInResult> ProcessOtherMessageAsync(
		Message message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken)
	{
		var (adapters, isPended, _) = await GetAdaptersAsync(message, adapterLookup, cancellationToken);

		if (isPended)
			return RoutingInResult.Pended();

		if (adapters.Length == 0)
			return RoutingInResult.CreateHandled();

		var decisions = adapters.Select(a => (a, message)).ToList();
		return RoutingInResult.WithRouting(decisions);
	}

	private async ValueTask<(IMessageAdapter[] adapters, bool isPended, bool skipSupportedMessages)> GetAdaptersAsync(
		Message message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken)
	{
		var isPended = false;

		using (await _lock.LockAsync(cancellationToken))
		{
			var (adapters, skipSupportedMessages) = _router.GetAdapters(message, adapterLookup);

			if (adapters == null)
			{
				if (_connectionState.HasPendingAdapters || _connectionState.TotalCount == 0 || _connectionState.AllDisconnectedOrFailed)
				{
					isPended = true;
					_pendingState.Add(message.Clone());
					return ([], isPended, skipSupportedMessages);
				}
			}

			adapters ??= [];

			if (adapters.Length == 0)
				LogInfo(LocalizedStrings.NoAdapterFoundFor.Put(message));

			return (adapters, isPended, skipSupportedMessages);
		}
	}

	private async ValueTask<(IMessageAdapter[] adapters, bool isPended)> GetSubscriptionAdaptersAsync(
		MarketDataMessage mdMsg,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken)
	{
		var (adapters, isPended, skipSupportedMessages) = await GetAdaptersAsync(mdMsg, adapterLookup, cancellationToken);

		adapters = await _router.GetSubscriptionAdaptersAsync(mdMsg, adapters, skipSupportedMessages, cancellationToken);

		return (adapters, isPended);
	}

	private IDictionary<ISubscriptionMessage, IMessageAdapter> ToChild(ISubscriptionMessage subscrMsg, IMessageAdapter[] adapters)
	{
		var child = new Dictionary<ISubscriptionMessage, IMessageAdapter>();

		if (subscrMsg.IsSubscribe)
		{
			foreach (var adapter in adapters)
			{
				var clone = subscrMsg.TypedClone();
				clone.TransactionId = _transactionIdGenerator.GetNextId();

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
				clone.TransactionId = _transactionIdGenerator.GetNextId();
				clone.OriginalTransactionId = pair.Key;

				child.Add(clone, adapter);

				_parentChildMap.AddMapping(clone.TransactionId, subscrMsg, adapter);

				// Remove original child mapping so data is no longer forwarded
				_parentChildMap.RemoveMapping(pair.Key);
			}
		}

		return child;
	}

	/// <inheritdoc />
	public async ValueTask<RoutingOutResult> ProcessOutMessageAsync(
		IMessageAdapter innerAdapter,
		Message message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
				return await ProcessSubscriptionResponseAsync(innerAdapter, (SubscriptionResponseMessage)message, adapterLookup, cancellationToken);

			case MessageTypes.SubscriptionFinished:
				return ProcessSubscriptionFinished((SubscriptionFinishedMessage)message);

			case MessageTypes.SubscriptionOnline:
				return ProcessSubscriptionOnline((SubscriptionOnlineMessage)message);

			default:
				if (message is ISubscriptionIdMessage subscrIdMsg)
				{
					if (!ApplyParentLookupId(subscrIdMsg))
						return RoutingOutResult.Empty;
				}

				return RoutingOutResult.PassThrough(message);
		}
	}

	private async ValueTask<RoutingOutResult> ProcessSubscriptionResponseAsync(
		IMessageAdapter adapter,
		SubscriptionResponseMessage message,
		Func<IMessageAdapter, IMessageAdapter> adapterLookup,
		CancellationToken cancellationToken)
	{
		var originalTransactionId = message.OriginalTransactionId;

		if (!_subscriptionRouting.TryGetRequest(originalTransactionId, out var originMsg, out _))
			return RoutingOutResult.PassThrough(message);

		var error = message.Error;

		if (error != null)
		{
			LogWarning("Subscription Error out: {0}", message);
			_subscriptionRouting.RemoveSubscription(originalTransactionId);
			_subscriptionRouting.RemoveRequest(originalTransactionId);
		}
		else if (!originMsg.IsSubscribe)
		{
			_subscriptionRouting.RemoveRequest(originMsg.OriginalTransactionId);
			_subscriptionRouting.RemoveRequest(originalTransactionId);
		}

		var parentId = _parentChildMap.ProcessChildResponse(originalTransactionId, error, out var needParentResponse, out var allError, out var innerErrors);

		if (parentId != null)
		{
			if (allError)
				_subscriptionRouting.RemoveSubscription(parentId.Value);

			return needParentResponse
				? RoutingOutResult.WithMessage(parentId.Value.CreateSubscriptionResponse(
					allError ? new AggregateException(LocalizedStrings.NoAdapterFoundFor.Put(originMsg), innerErrors) : null))
				: RoutingOutResult.Empty;
		}
		else
		{
			if (!originMsg.IsSubscribe)
				_subscriptionRouting.RemoveSubscription(originMsg.OriginalTransactionId);
		}

		if (message.IsNotSupported() && originMsg is ISubscriptionMessage subscrMsg)
		{
			using (await _lock.LockAsync(cancellationToken))
			{
				if (subscrMsg.IsSubscribe)
				{
					_router.AddNotSupported(originalTransactionId, adapter);

					subscrMsg.LoopBack(adapter);
				}
			}

			return RoutingOutResult.WithLoopback((Message)subscrMsg);
		}

		return RoutingOutResult.PassThrough(message);
	}

	private RoutingOutResult ProcessSubscriptionFinished(SubscriptionFinishedMessage message)
	{
		var originalTransactionId = message.OriginalTransactionId;

		_subscriptionRouting.RemoveRequest(originalTransactionId);

		var parentId = _parentChildMap.ProcessChildFinish(originalTransactionId, out var needParentResponse);

		if (parentId == null)
		{
			_subscriptionRouting.RemoveSubscription(originalTransactionId);
			return RoutingOutResult.PassThrough(message);
		}

		if (!needParentResponse)
			return RoutingOutResult.Empty;

		_subscriptionRouting.RemoveSubscription(parentId.Value);
		return RoutingOutResult.WithMessage(new SubscriptionFinishedMessage
		{
			OriginalTransactionId = parentId.Value,
			Body = message.Body,
		});
	}

	private RoutingOutResult ProcessSubscriptionOnline(SubscriptionOnlineMessage message)
	{
		var originalTransactionId = message.OriginalTransactionId;

		var parentId = _parentChildMap.ProcessChildOnline(originalTransactionId, out var needParentResponse);

		if (parentId == null)
			return RoutingOutResult.PassThrough(message);

		if (!needParentResponse)
			return RoutingOutResult.Empty;

		return RoutingOutResult.WithMessage(new SubscriptionOnlineMessage
		{
			OriginalTransactionId = parentId.Value,
		});
	}

	#endregion

	#region Logging

	private void LogInfo(string message, params object[] args)
	{
		_logReceiver?.AddInfoLog(message, args);
	}

	private void LogWarning(string message, params object[] args)
	{
		_logReceiver?.AddWarningLog(message, args);
	}

	#endregion
}
