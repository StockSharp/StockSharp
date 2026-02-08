namespace StockSharp.Algo;

/// <summary>
/// Transaction ordering message processing logic.
/// </summary>
public interface ITransactionOrderingManager
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result with messages to forward to inner adapter and messages to send out.</returns>
	(Message[] toInner, Message[] toOut) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result with forward message (null if should not forward), extra output messages, and whether to process suspended trades.</returns>
	(Message forward, Message[] extraOut, bool processSuspended) ProcessOutMessage(Message message);

	/// <summary>
	/// Get suspended trades for the given order.
	/// </summary>
	/// <param name="execMsg">Order execution message.</param>
	/// <returns>Suspended trades to release.</returns>
	Message[] GetSuspendedTrades(ExecutionMessage execMsg);
}

/// <summary>
/// Transaction ordering message processing implementation.
/// </summary>
public sealed class TransactionOrderingManager : ITransactionOrderingManager
{
	private sealed class SubscriptionInfo(OrderStatusMessage original)
	{
		public Lock Sync { get; } = new();

		public OrderStatusMessage Original { get; } = original ?? throw new ArgumentNullException(nameof(original));
		public Dictionary<long, (List<ExecutionMessage> changes, List<ExecutionMessage> trades, long transId)> Transactions { get; } = [];
	}

	private readonly ILogReceiver _logReceiver;
	private readonly Func<bool> _isSupportTransactionLog;

	private readonly SynchronizedDictionary<long, SubscriptionInfo> _transactionLogSubscriptions = [];
	private readonly SynchronizedSet<long> _orderStatusIds = [];

	private readonly SynchronizedDictionary<long, long> _orders = [];
	private readonly SynchronizedDictionary<long, SecurityId> _secIds = [];

	private readonly SynchronizedPairSet<long, long> _orderIds = [];
	private readonly SynchronizedPairSet<string, long> _orderStringIds = new(StringComparer.InvariantCultureIgnoreCase);

	private readonly Lock _nonAssociatedLock = new();
	private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedOrderIds = [];
	private readonly Dictionary<string, List<ExecutionMessage>> _nonAssociatedStringOrderIds = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="TransactionOrderingManager"/>.
	/// </summary>
	/// <param name="logReceiver">Log receiver.</param>
	/// <param name="isSupportTransactionLog">Function to check if transaction log is supported.</param>
	public TransactionOrderingManager(ILogReceiver logReceiver, Func<bool> isSupportTransactionLog)
	{
		_logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
		_isSupportTransactionLog = isSupportTransactionLog ?? throw new ArgumentNullException(nameof(isSupportTransactionLog));
	}

	private void Reset()
	{
		_transactionLogSubscriptions.Clear();
		_orderStatusIds.Clear();

		_orders.Clear();
		_secIds.Clear();

		_orderIds.Clear();
		_orderStringIds.Clear();

		using (_nonAssociatedLock.EnterScope())
		{
			_nonAssociatedOrderIds.Clear();
			_nonAssociatedStringOrderIds.Clear();
		}
	}

	/// <inheritdoc />
	public (Message[] toInner, Message[] toOut) ProcessInMessage(Message message)
	{
		static void RemoveTrailingZeros(OrderRegisterMessage regMsg)
		{
			if (regMsg.Price != default)
				regMsg.Price = regMsg.Price.RemoveTrailingZeros();

			if (regMsg.Volume != default)
				regMsg.Volume = regMsg.Volume.RemoveTrailingZeros();

			if (regMsg.VisibleVolume != default)
				regMsg.VisibleVolume = regMsg.VisibleVolume?.RemoveTrailingZeros();
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				Reset();
				break;
			}
			case MessageTypes.OrderRegister:
			{
				var regMsg = (OrderRegisterMessage)message;

				RemoveTrailingZeros(regMsg);

				_secIds.TryAdd2(regMsg.TransactionId, regMsg.SecurityId);
				break;
			}
			case MessageTypes.OrderReplace:
			{
				var replaceMsg = (OrderReplaceMessage)message;

				RemoveTrailingZeros(replaceMsg);

				if (_secIds.TryGetValue(replaceMsg.OriginalTransactionId, out var secId))
					_secIds.TryAdd2(replaceMsg.TransactionId, secId);

				break;
			}
			case MessageTypes.OrderCancel:
			{
				var cancelMsg = (OrderCancelMessage)message;

				if (cancelMsg.Volume != default)
					cancelMsg.Volume = cancelMsg.Volume?.RemoveTrailingZeros();

				break;
			}
			case MessageTypes.OrderStatus:
			{
				var statusMsg = (OrderStatusMessage)message;

				if (statusMsg.IsSubscribe)
				{
					if (_isSupportTransactionLog())
						_transactionLogSubscriptions.Add(statusMsg.TransactionId, new SubscriptionInfo(statusMsg.TypedClone()));
					else
						_orderStatusIds.Add(statusMsg.TransactionId);
				}

				break;
			}
		}

		return ([message], []);
	}

	/// <inheritdoc />
	public (Message forward, Message[] extraOut, bool processSuspended) ProcessOutMessage(Message message)
	{
		var processSuspended = false;
		List<Message> extraOut = null;

		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;

				if (!responseMsg.IsOk())
				{
					_transactionLogSubscriptions.Remove(responseMsg.OriginalTransactionId);
				}

				break;
			}
			case MessageTypes.SubscriptionFinished:
			case MessageTypes.SubscriptionOnline:
			{
				var originMsg = (IOriginalTransactionIdMessage)message;

				if (!_transactionLogSubscriptions.TryGetAndRemove(originMsg.OriginalTransactionId, out var subscription))
					break;

				(List<ExecutionMessage> changes, List<ExecutionMessage> trades, long transId)[] tuples;

				using (subscription.Sync.EnterScope())
					tuples = [.. subscription.Transactions.Values];

				extraOut = [];

				foreach (var (changes, trades, transId) in tuples)
				{
					var order = changes.ToOrderSnapshot(transId, _logReceiver);

					extraOut.Add(order);

					foreach (var trade in trades)
						extraOut.Add(trade);
				}

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
					break;

				// skip cancellation cause they are reply on action and no have transaction state
				if (execMsg.IsCancellation)
					break;

				var transId = execMsg.TransactionId;

				if (transId != 0)
					_secIds.TryAdd2(transId, execMsg.SecurityId);
				else
				{
					if (execMsg.SecurityId == default && _secIds.TryGetValue(execMsg.OriginalTransactionId, out var secId))
						execMsg.SecurityId = secId;
				}

				if (transId != 0 || execMsg.OriginalTransactionId != 0)
				{
					if (transId == 0)
						transId = execMsg.OriginalTransactionId;

					if (execMsg.OrderId != null)
					{
						_orderIds.TryAdd(execMsg.OrderId.Value, transId);
					}
					else if (!execMsg.OrderStringId.IsEmpty())
					{
						_orderStringIds.TryAdd(execMsg.OrderStringId, transId);
					}
				}

				if (execMsg.TransactionId == 0 && execMsg.HasTradeInfo && _orderStatusIds.Contains(execMsg.OriginalTransactionId))
				{
					// below the code will try find order's transaction
					execMsg.OriginalTransactionId = 0;
				}

				if (/*execMsg.TransactionId == 0 && */execMsg.OriginalTransactionId == 0)
				{
					if (!execMsg.HasTradeInfo)
					{
						_logReceiver.AddWarningLog("Order doesn't have origin trans id: {0}", execMsg);
						return (null, [], false);
					}

					// Try to resolve OriginalTransactionId from known order mappings.
					// If not found, let the code continue to suspension logic below.
					if (execMsg.OrderId != null)
					{
						if (_orderIds.TryGetValue(execMsg.OrderId.Value, out var originId))
							execMsg.OriginalTransactionId = originId;
					}
					else if (!execMsg.OrderStringId.IsEmpty())
					{
						if (_orderStringIds.TryGetValue(execMsg.OrderStringId, out var originId))
							execMsg.OriginalTransactionId = originId;
					}
				}

				if (execMsg.HasTradeInfo && !execMsg.HasOrderInfo)
				{
					if (execMsg.OrderId != null && !_orderIds.ContainsKey(execMsg.OrderId.Value) && (execMsg.OriginalTransactionId == 0 || !_secIds.ContainsKey(execMsg.OriginalTransactionId)))
					{
						_logReceiver.AddInfoLog("{0} suspended.", execMsg);

						using (_nonAssociatedLock.EnterScope())
							_nonAssociatedOrderIds.SafeAdd(execMsg.OrderId.Value).Add(execMsg.TypedClone());

						return (null, [], false);
					}
					else if (!execMsg.OrderStringId.IsEmpty() && !_orderStringIds.ContainsKey(execMsg.OrderStringId) && (execMsg.OriginalTransactionId == 0 || !_secIds.ContainsKey(execMsg.OriginalTransactionId)))
					{
						_logReceiver.AddInfoLog("{0} suspended.", execMsg);

						using (_nonAssociatedLock.EnterScope())
							_nonAssociatedStringOrderIds.SafeAdd(execMsg.OrderStringId).Add(execMsg.TypedClone());

						return (null, [], false);
					}
				}

				if (_transactionLogSubscriptions.Count == 0)
				{
					processSuspended = true;
					break;
				}

				if (!_transactionLogSubscriptions.TryGetValue(execMsg.OriginalTransactionId, out var subscription))
				{
					if (!_orders.TryGetValue(execMsg.OriginalTransactionId, out var orderTransId))
						break;

					if (!_transactionLogSubscriptions.TryGetValue(orderTransId, out subscription))
						break;
				}

				if (transId == 0)
				{
					if (execMsg.HasTradeInfo)
						transId = execMsg.OriginalTransactionId;

					if (transId == 0)
					{
						_logReceiver.AddWarningLog("Message {0} do not contains transaction id.", execMsg);
						break;
					}
				}

				using (subscription.Sync.EnterScope())
				{
					if (subscription.Transactions.TryGetValue(transId, out var tuple))
					{
						if (execMsg.HasOrderInfo)
						{
							var order = execMsg.TypedClone();
							order.TradeStringId = null;
							order.TradeId = null;
							order.TradePrice = null;
							order.TradeVolume = null;
							tuple.changes.Add(order);
						}

						if (execMsg.HasTradeInfo)
						{
							var trade = execMsg.TypedClone();
							trade.HasOrderInfo = false;
							tuple.trades.Add(trade);
						}
					}
					else
					{
						_orders.Add(transId, execMsg.OriginalTransactionId);
						subscription.Transactions.Add(transId, ([execMsg.TypedClone()], [], transId));
					}
				}

				return (null, [], false);
			}
		}

		return (message, extraOut?.ToArray() ?? [], processSuspended);
	}

	/// <inheritdoc />
	public Message[] GetSuspendedTrades(ExecutionMessage execMsg)
	{
		return GetSuspendedTradesInternal(execMsg);
	}

	private Message[] GetSuspendedTradesInternal(ExecutionMessage execMsg)
	{
		if (!execMsg.HasOrderInfo)
			return [];

		var result = new List<Message>();

		if (execMsg.OrderId != null)
		{
			var trades = GetSuspendedTradesForKey(_nonAssociatedOrderIds, execMsg.OrderId.Value);
			result.AddRange(trades);
		}

		if (!execMsg.OrderStringId.IsEmpty())
		{
			var trades = GetSuspendedTradesForKey(_nonAssociatedStringOrderIds, execMsg.OrderStringId);
			result.AddRange(trades);
		}

		return [.. result];
	}

	private Message[] GetSuspendedTradesForKey<TKey>(Dictionary<TKey, List<ExecutionMessage>> nonAssociated, TKey key)
	{
		List<ExecutionMessage> trades;

		using (_nonAssociatedLock.EnterScope())
		{
			if (nonAssociated.Count > 0)
			{
				if (!nonAssociated.TryGetAndRemove(key, out trades))
					return [];
			}
			else
				return [];
		}

		_logReceiver.AddInfoLog("{0} resumed.", key);

		return [.. trades];
	}
}
