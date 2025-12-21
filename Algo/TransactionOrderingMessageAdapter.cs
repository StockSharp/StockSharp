namespace StockSharp.Algo;

/// <summary>
/// Transactional messages ordering adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TransactionOrderingMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
public class TransactionOrderingMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private class SubscriptionInfo(OrderStatusMessage original)
	{
		public Lock Sync { get; } = new();

		public OrderStatusMessage Original { get; } = original ?? throw new ArgumentNullException(nameof(original));
		public Dictionary<long, (List<ExecutionMessage> changes, List<ExecutionMessage> trades, long transId)> Transactions { get; } = [];
	}

	private readonly SynchronizedDictionary<long, SubscriptionInfo> _transactionLogSubscriptions = [];
	private readonly SynchronizedSet<long> _orderStatusIds = [];

	private readonly SynchronizedDictionary<long, long> _orders = [];
	private readonly SynchronizedDictionary<long, SecurityId> _secIds = [];

	private readonly SynchronizedPairSet<long, long> _orderIds = [];
	private readonly SynchronizedPairSet<string, long> _orderStringIds = new(StringComparer.InvariantCultureIgnoreCase);

	private readonly Lock _nonAssociatedLock = new();
	private readonly Dictionary<long, List<ExecutionMessage>> _nonAssociatedOrderIds = [];
	private readonly Dictionary<string, List<ExecutionMessage>> _nonAssociatedStringOrderIds = [];

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
	public override ValueTask SendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		static void RemoteTrailingZeros(OrderRegisterMessage regMsg)
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

				RemoteTrailingZeros(regMsg);

				_secIds.TryAdd2(regMsg.TransactionId, regMsg.SecurityId);
				break;
			}
			case MessageTypes.OrderReplace:
			{
				var replaceMsg = (OrderReplaceMessage)message;

				RemoteTrailingZeros(replaceMsg);

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
					if (IsSupportTransactionLog)
						_transactionLogSubscriptions.Add(statusMsg.TransactionId, new SubscriptionInfo(statusMsg.TypedClone()));
					else
						_orderStatusIds.Add(statusMsg.TransactionId);
				}

				break;
			}
		}

		return base.SendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var processSuspended = false;

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

				(List<ExecutionMessage> orderChanges, List<ExecutionMessage> trades, long transId)[] tuples;

				using (subscription.Sync.EnterScope())
					tuples = [.. subscription.Transactions.Values];

				//var canProcessFailed = truesubscription.Original.States.Contains(OrderStates.Failed);

				foreach (var (changes, trades, transId) in tuples)
				{
					var order = changes.ToOrderSnapshot(transId, this);

					//if (order.OrderState == OrderStates.Failed && !canProcessFailed)
					//{
					//	if (trades.Count > 0)
					//		LogWarning("Order {0} has failed state but contains {1} trades.", order.TransactionId, trades.Count);

					//	continue;
					//}

					await base.OnInnerAdapterNewOutMessageAsync(order, cancellationToken);

					await ProcessSuspendedAsync(order, cancellationToken);

					foreach (var trade in trades)
						await base.OnInnerAdapterNewOutMessageAsync(trade, cancellationToken);
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
						LogWarning("Order doesn't have origin trans id: {0}", execMsg);
						break;
					}

					if (execMsg.OrderId != null)
					{
						if (_orderIds.TryGetValue(execMsg.OrderId.Value, out var originId))
							execMsg.OriginalTransactionId = originId;
						else
						{
							LogWarning("Trade doesn't have origin trans id: {0}", execMsg);
							break;
						}
					}
					else if (!execMsg.OrderStringId.IsEmpty())
					{
						if (_orderStringIds.TryGetValue(execMsg.OrderStringId, out var originId))
							execMsg.OriginalTransactionId = originId;
						else
						{
							LogWarning("Trade doesn't have origin trans id: {0}", execMsg);
							break;
						}
					}
				}

				if (execMsg.HasTradeInfo && !execMsg.HasOrderInfo)
				{
					if (execMsg.OrderId != null && !_orderIds.ContainsKey(execMsg.OrderId.Value) && (execMsg.OriginalTransactionId == 0 || !_secIds.ContainsKey(execMsg.OriginalTransactionId)))
					{
						LogInfo("{0} suspended.", execMsg);

						using (_nonAssociatedLock.EnterScope())
							_nonAssociatedOrderIds.SafeAdd(execMsg.OrderId.Value).Add(execMsg.TypedClone());

						return;
					}
					else if (!execMsg.OrderStringId.IsEmpty() && !_orderStringIds.ContainsKey(execMsg.OrderStringId) && (execMsg.OriginalTransactionId == 0 || !_secIds.ContainsKey(execMsg.OriginalTransactionId)))
					{
						LogInfo("{0} suspended.", execMsg);

						using (_nonAssociatedLock.EnterScope())
							_nonAssociatedStringOrderIds.SafeAdd(execMsg.OrderStringId).Add(execMsg.TypedClone());

						return;
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
						LogWarning("Message {0} do not contains transaction id.", execMsg);
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

				return;
			}
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

		if (processSuspended)
			await ProcessSuspendedAsync((ExecutionMessage)message, cancellationToken);
	}

	private async ValueTask ProcessSuspendedAsync(ExecutionMessage execMsg, CancellationToken cancellationToken)
	{
		if (!execMsg.HasOrderInfo)
			return;

		if (execMsg.OrderId != null)
			await ProcessSuspendedAsync(_nonAssociatedOrderIds, execMsg.OrderId.Value, cancellationToken);

		if (!execMsg.OrderStringId.IsEmpty())
			await ProcessSuspendedAsync(_nonAssociatedStringOrderIds, execMsg.OrderStringId, cancellationToken);
	}

	private async ValueTask ProcessSuspendedAsync<TKey>(Dictionary<TKey, List<ExecutionMessage>> nonAssociated, TKey key, CancellationToken cancellationToken)
	{
		List<ExecutionMessage> trades;

		using (_nonAssociatedLock.EnterScope())
		{
			if (nonAssociated.Count > 0)
			{
				if (!nonAssociated.TryGetAndRemove(key, out trades))
					return;
			}
			else
				return;
		}

		LogInfo("{0} resumed.", key);

		foreach (var trade in trades)
			await RaiseNewOutMessageAsync(trade, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="TransactionOrderingMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new TransactionOrderingMessageAdapter(InnerAdapter.TypedClone());
	}
}