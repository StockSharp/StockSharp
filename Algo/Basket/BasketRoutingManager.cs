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
	private readonly Func<bool> _levelExtend;
	private readonly ILogReceiver _logReceiver;

	/// <summary>
	/// Initializes a new instance of <see cref="BasketRoutingManager"/>.
	/// </summary>
	public BasketRoutingManager(
		IAdapterConnectionState connectionState,
		IAdapterConnectionManager connectionManager,
		IPendingMessageState pendingState,
		IPendingMessageManager pendingManager,
		ISubscriptionRoutingState subscriptionRouting,
		IParentChildMap parentChildMap,
		IOrderRoutingState orderRouting,
		Func<IMessageAdapter, IMessageAdapter> getUnderlyingAdapter,
		CandleBuilderProvider candleBuilderProvider,
		Func<bool> levelExtend,
		IdGenerator transactionIdGenerator,
		ILogReceiver logReceiver = null)
	{
		ConnectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
		ConnectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
		PendingState = pendingState ?? throw new ArgumentNullException(nameof(pendingState));
		PendingManager = pendingManager ?? throw new ArgumentNullException(nameof(pendingManager));
		SubscriptionRouting = subscriptionRouting ?? throw new ArgumentNullException(nameof(subscriptionRouting));
		ParentChildMap = parentChildMap ?? throw new ArgumentNullException(nameof(parentChildMap));
		OrderRouting = orderRouting ?? throw new ArgumentNullException(nameof(orderRouting));
		_getUnderlyingAdapter = getUnderlyingAdapter ?? throw new ArgumentNullException(nameof(getUnderlyingAdapter));
		_transactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
		_levelExtend = levelExtend ?? throw new ArgumentNullException(nameof(levelExtend));
		_logReceiver = logReceiver;

		Router = new AdapterRouter(orderRouting, getUnderlyingAdapter, candleBuilderProvider, levelExtend);
	}

	/// <inheritdoc />
	public IAdapterConnectionState ConnectionState { get; }

	/// <inheritdoc />
	public IAdapterConnectionManager ConnectionManager { get; }

	/// <inheritdoc />
	public IPendingMessageState PendingState { get; }

	/// <summary>
	/// Gets the pending message manager.
	/// </summary>
	public IPendingMessageManager PendingManager { get; }

	/// <inheritdoc />
	public ISubscriptionRoutingState SubscriptionRouting { get; }

	/// <inheritdoc />
	public IParentChildMap ParentChildMap { get; }

	/// <summary>
	/// Gets the order routing state.
	/// </summary>
	public IOrderRoutingState OrderRouting { get; }

	/// <inheritdoc />
	public IAdapterRouter Router { get; }

	/// <inheritdoc />
	public bool HasPendingAdapters => ConnectionState.HasPendingAdapters;

	/// <inheritdoc />
	public bool AllDisconnectedOrFailed => ConnectionState.AllDisconnectedOrFailed;

	/// <inheritdoc />
	public int ConnectedCount => ConnectionState.ConnectedCount;

	/// <inheritdoc />
	public int TotalCount => ConnectionState.TotalCount;

	/// <inheritdoc />
	public void AddOrderAdapter(long transactionId, IMessageAdapter adapter)
		=> Router.AddOrderAdapter(transactionId, adapter);

	/// <inheritdoc />
	public bool TryGetOrderAdapter(long transactionId, out IMessageAdapter adapter)
		=> Router.TryGetOrderAdapter(transactionId, out adapter);

	/// <inheritdoc />
	public long[] GetSubscribers(DataType dataType)
		=> SubscriptionRouting.GetSubscribers(dataType);

	/// <inheritdoc />
	public void RegisterAdapterMessageTypes(IMessageAdapter adapter, IEnumerable<MessageTypes> supportedTypes)
	{
		foreach (var type in supportedTypes)
			Router.AddMessageTypeAdapter(type, adapter);
	}

	/// <inheritdoc />
	public void UnregisterAdapterMessageTypes(IMessageAdapter adapter, IEnumerable<MessageTypes> supportedTypes)
	{
		foreach (var type in supportedTypes)
			Router.RemoveMessageTypeAdapter(type, adapter);
	}

	/// <inheritdoc />
	public void Reset(bool clearPending)
	{
		Router.Clear();

		if (clearPending)
			PendingState.Clear();

		ConnectionManager.Reset();
		SubscriptionRouting.Clear();
		ParentChildMap.Clear();
	}

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
				// Order messages are handled directly by BasketMessageAdapter
				// as they require portfolio lookup which depends on adapter wrappers
				return RoutingInResult.Empty;

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

			SubscriptionRouting.AddSubscription(mdMsg.TransactionId, (ISubscriptionMessage)mdMsg.Clone(), adapters, mdMsg.DataType2);
		}
		else
		{
			adapters = null;

			var originTransId = mdMsg.OriginalTransactionId;

			if (!SubscriptionRouting.TryGetSubscription(originTransId, out _, out _, out _))
			{
				using (await _lock.LockAsync(cancellationToken))
				{
					var suspended = PendingState.TryRemoveMarketData(originTransId);

					if (suspended != null)
						return RoutingInResult.WithOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = mdMsg.TransactionId });
				}

				LogInfo("Unsubscribe not found: {0}/{1}", originTransId, mdMsg);
				return RoutingInResult.CreateHandled();
			}
		}

		// Use unified ToChild path for all market data subscriptions
		var routing = ToChild(mdMsg, adapters);
		var decisions = routing
			.Select(pair => (pair.Value, (Message)pair.Key))
			.ToList();

		// Store request mappings
		foreach (var (msg, adapter) in routing)
		{
			SubscriptionRouting.AddRequest(msg.TransactionId, msg, _getUnderlyingAdapter(adapter));
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
				SubscriptionRouting.AddSubscription(subscrMsg.TransactionId, subscrMsg.TypedClone(), [adapter], subscrMsg.DataType);
				SubscriptionRouting.AddRequest(subscrMsg.TransactionId, subscrMsg, _getUnderlyingAdapter(adapter));
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

			SubscriptionRouting.AddSubscription(subscrMsg.TransactionId, subscrMsg.TypedClone(), adapters, subscrMsg.DataType);
		}
		else
			adapters = null;

		var routing = ToChild(subscrMsg, adapters);
		var decisions = routing
			.Select(pair => (pair.Value, (Message)pair.Key))
			.ToList();

		foreach (var (msg2, adapter) in routing)
		{
			SubscriptionRouting.AddRequest(msg2.TransactionId, msg2, _getUnderlyingAdapter(adapter));
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
			var (adapters, skipSupportedMessages) = Router.GetAdapters(message, adapterLookup);

			if (adapters == null)
			{
				if (ConnectionState.HasPendingAdapters || ConnectionState.TotalCount == 0 || ConnectionState.AllDisconnectedOrFailed)
				{
					isPended = true;
					PendingState.Add(message.Clone());
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

		adapters = Router.GetSubscriptionAdapters(mdMsg, adapters, skipSupportedMessages);

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

				ParentChildMap.AddMapping(clone.TransactionId, subscrMsg, adapter);
			}
		}
		else
		{
			var originTransId = subscrMsg.OriginalTransactionId;

			foreach (var pair in ParentChildMap.GetChild(originTransId))
			{
				var adapter = pair.Value;

				var clone = subscrMsg.TypedClone();
				clone.TransactionId = _transactionIdGenerator.GetNextId();
				clone.OriginalTransactionId = pair.Key;

				child.Add(clone, adapter);

				ParentChildMap.AddMapping(clone.TransactionId, subscrMsg, adapter);
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
					ApplyParentLookupId(subscrIdMsg);

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

		if (!SubscriptionRouting.TryGetRequest(originalTransactionId, out var originMsg, out _))
			return RoutingOutResult.PassThrough(message);

		var error = message.Error;

		if (error != null)
		{
			LogWarning("Subscription Error out: {0}", message);
			SubscriptionRouting.RemoveSubscription(originalTransactionId);
			SubscriptionRouting.RemoveRequest(originalTransactionId);
		}
		else if (!originMsg.IsSubscribe)
		{
			SubscriptionRouting.RemoveRequest(originMsg.OriginalTransactionId);
			SubscriptionRouting.RemoveRequest(originalTransactionId);
		}

		var parentId = ParentChildMap.ProcessChildResponse(originalTransactionId, error, out var needParentResponse, out var allError, out var innerErrors);

		if (parentId != null)
		{
			if (allError)
				SubscriptionRouting.RemoveSubscription(parentId.Value);

			return needParentResponse
				? RoutingOutResult.WithMessage(parentId.Value.CreateSubscriptionResponse(
					allError ? new AggregateException(LocalizedStrings.NoAdapterFoundFor.Put(originMsg), innerErrors) : null))
				: RoutingOutResult.Empty;
		}
		else
		{
			if (!originMsg.IsSubscribe)
				SubscriptionRouting.RemoveSubscription(originMsg.OriginalTransactionId);
		}

		if (message.IsNotSupported() && originMsg is ISubscriptionMessage subscrMsg)
		{
			using (await _lock.LockAsync(cancellationToken))
			{
				if (subscrMsg.IsSubscribe)
				{
					Router.AddNotSupported(originalTransactionId, adapter);

					subscrMsg.LoopBack(null); // Will be set by caller
				}
			}

			return RoutingOutResult.WithLoopback((Message)subscrMsg);
		}

		return RoutingOutResult.PassThrough(message);
	}

	private RoutingOutResult ProcessSubscriptionFinished(SubscriptionFinishedMessage message)
	{
		var originalTransactionId = message.OriginalTransactionId;

		SubscriptionRouting.RemoveRequest(originalTransactionId);

		var parentId = ParentChildMap.ProcessChildFinish(originalTransactionId, out var needParentResponse);

		if (parentId == null)
		{
			SubscriptionRouting.RemoveSubscription(originalTransactionId);
			return RoutingOutResult.PassThrough(message);
		}

		if (!needParentResponse)
			return RoutingOutResult.Empty;

		SubscriptionRouting.RemoveSubscription(parentId.Value);
		return RoutingOutResult.WithMessage(new SubscriptionFinishedMessage
		{
			OriginalTransactionId = parentId.Value,
			Body = message.Body,
		});
	}

	private RoutingOutResult ProcessSubscriptionOnline(SubscriptionOnlineMessage message)
	{
		var originalTransactionId = message.OriginalTransactionId;

		var parentId = ParentChildMap.ProcessChildOnline(originalTransactionId, out var needParentResponse);

		if (parentId == null)
			return RoutingOutResult.PassThrough(message);

		if (!needParentResponse)
			return RoutingOutResult.Empty;

		return RoutingOutResult.WithMessage(new SubscriptionOnlineMessage
		{
			OriginalTransactionId = parentId.Value,
		});
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
			if (!ParentChildMap.TryGetParent(ids[i], out var parentId))
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

	private void LogInfo(string message, params object[] args)
	{
		_logReceiver?.AddInfoLog(message, args);
	}

	private void LogWarning(string message, params object[] args)
	{
		_logReceiver?.AddWarningLog(message, args);
	}
}
