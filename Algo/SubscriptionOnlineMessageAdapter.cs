namespace StockSharp.Algo;

/// <summary>
/// Online subscription counter adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionOnlineMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
public class SubscriptionOnlineMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private class SubscriptionInfo
	{
		private readonly SubscriptionInfo _main;

		public ISubscriptionMessage Subscription { get; }

		public SubscriptionInfo(ISubscriptionMessage subscription)
		{
			Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			IsMarketData = subscription.DataType.IsMarketData;
		}

		public SubscriptionInfo(SubscriptionInfo main)
			: this(main.CheckOnNull(nameof(main)).Subscription)
		{
			_main = main;
			Subscribers = main.Subscribers;
		}

		private void CheckOnLinked()
		{
			if (_main != null)
				throw new InvalidOperationException();
		}

		private SubscriptionStates _state = SubscriptionStates.Stopped;

		public SubscriptionStates State
		{
			get => _main?.State ?? _state;
			set
			{
				CheckOnLinked();
				_state = value;
			}
		}

		public readonly HashSet<long> ExtraFilters = [];
		public readonly CachedSynchronizedDictionary<long, ISubscriptionMessage> Subscribers = [];
		public readonly CachedSynchronizedSet<long> OnlineSubscribers = [];
		public readonly SynchronizedSet<long> HistLive = [];
		public readonly bool IsMarketData;

		private readonly List<long> _linked = [];

		public List<long> Linked
		{
			get
			{
				CheckOnLinked();
				return _linked;
			}
		}

		public override string ToString() => (_main != null ? "Linked: " : string.Empty) + Subscription.ToString();
	}

	private readonly AsyncLock _sync = new();

	private readonly PairSet<(DataType, SecurityId), SubscriptionInfo> _subscriptionsByKey = [];
	private readonly Dictionary<long, SubscriptionInfo> _subscriptionsById = [];
	private readonly HashSet<long> _skipSubscriptions = [];
	private readonly HashSet<long> _unsubscribeRequests = [];

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		async ValueTask TryAddOrderSubscription(OrderMessage orderMsg)
		{
			using (await _sync.LockAsync(cancellationToken))
			{
				if (_subscriptionsByKey.TryGetValue((DataType.Transactions, default(SecurityId)), out var info))
					TryAddOrderTransaction(info, orderMsg.TransactionId);

				//if (_subscriptionsByKey.TryGetValue((DataType.Transactions, orderMsg.SecurityId), out info))
				//	TryAddOrderTransaction(info, orderMsg.TransactionId);
			}
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
				await ProcessReset(message, cancellationToken);
				break;

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			case MessageTypes.OrderGroupCancel:
			{
				var orderMsg = (OrderMessage)message;

				await TryAddOrderSubscription(orderMsg);

				await base.OnSendInMessageAsync(message, cancellationToken);
				break;
			}

			default:
			{
				if (message is ISubscriptionMessage subscrMsg)
					await ProcessInSubscriptionMessage(subscrMsg, cancellationToken);
				else
					await base.OnSendInMessageAsync(message, cancellationToken);

				break;
			}
		}
	}

	private bool ChangeState(SubscriptionInfo info, long transId, SubscriptionStates state)
	{
		// secondary hist+live cannot change main subscription state
		if (info.HistLive.Contains(transId))
			return false;

		info.State = info.State.ChangeSubscriptionState(state, info.Subscription.TransactionId, this);

		if (!state.IsActive())
		{
			_subscriptionsByKey.RemoveByValue(info);
			LogInfo(LocalizedStrings.SubscriptionRemoved, info.Subscription.TransactionId);
		}

		return true;
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Disconnect:
			case MessageTypes.ConnectionRestored:
			{
				if (message is ConnectionRestoredMessage restoredMsg && !restoredMsg.IsResetState)
					break;

				await ClearState(cancellationToken);
				break;
			}

			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				var originTransId = responseMsg.OriginalTransactionId;

				HashSet<long> subscribers = null;

				using (await _sync.LockAsync(cancellationToken))
				{
					if (responseMsg.IsOk())
					{
						if (_subscriptionsById.TryGetValue(originTransId, out var info))
						{
							if (!ChangeState(info, originTransId, _unsubscribeRequests.Contains(originTransId) ? SubscriptionStates.Stopped : SubscriptionStates.Active))
								return;
						}
					}
					else
					{
						if (_subscriptionsById.TryGetAndRemove(originTransId, out var info))
						{
							info.OnlineSubscribers.Remove(originTransId);
							info.Subscribers.Remove(originTransId);

							if (!ChangeState(info, originTransId, SubscriptionStates.Error))
							{
								info.HistLive.Remove(originTransId);
								return;
							}
						}
					}
				}

				if (subscribers != null)
				{
					foreach (var subscriber in subscribers)
					{
						LogInfo(LocalizedStrings.SubscriptionNotifySubscriber, responseMsg.OriginalTransactionId, subscriber);
						await base.OnInnerAdapterNewOutMessageAsync(subscriber.CreateSubscriptionResponse(responseMsg.Error), cancellationToken);
					}
				}

				break;
			}

			case MessageTypes.SubscriptionOnline:
			{
				var originTransId = ((SubscriptionOnlineMessage)message).OriginalTransactionId;

				using (await _sync.LockAsync(cancellationToken))
				{
					if (_subscriptionsById.TryGetValue(originTransId, out var info))
					{
						info.OnlineSubscribers.Add(originTransId);

						if (!ChangeState(info, originTransId, SubscriptionStates.Online))
							return;
					}
				}

				break;
			}

			case MessageTypes.SubscriptionFinished:
			{
				var originTransId = ((SubscriptionFinishedMessage)message).OriginalTransactionId;

				using (await _sync.LockAsync(cancellationToken))
				{
					if (_subscriptionsById.TryGetValue(originTransId, out var info))
					{
						if (!ChangeState(info, originTransId, SubscriptionStates.Finished))
						{
							info.OnlineSubscribers.Remove(originTransId);
							return;
						}
					}
				}

				break;
			}

			default:
			{
				if (message is ISubscriptionIdMessage subscrMsg)
				{
					using (await _sync.LockAsync(cancellationToken))
					{
						if (subscrMsg.OriginalTransactionId != 0 && _subscriptionsById.TryGetValue(subscrMsg.OriginalTransactionId, out var info))
						{
							if (message is ExecutionMessage execMsg &&
								execMsg.DataType == DataType.Transactions &&
								execMsg.TransactionId != 0 &&
								info.Subscription.DataType == DataType.Transactions)
							{
								TryAddOrderTransaction(info, execMsg.TransactionId,
									false // lookup history can request order changes (registered, filled, cancelled)
								);
							}
							else if (info.State != SubscriptionStates.Online)
							{
								subscrMsg.SetSubscriptionIds([subscrMsg.OriginalTransactionId]);
								break;
							}
						}
						else
						{
							var dataType = subscrMsg.DataType;
							var secId = (subscrMsg as ISecurityIdMessage)?.SecurityId ?? default;

							if (!_subscriptionsByKey.TryGetValue((dataType, secId), out info) && (secId == default || !_subscriptionsByKey.TryGetValue((dataType, default(SecurityId)), out info)))
								break;
						}

						var ids = info.IsMarketData ? info.OnlineSubscribers.Cache : info.Subscribers.CachedKeys;

						if (info.ExtraFilters.Count > 0)
						{
							var set = new HashSet<long>(ids);

							foreach (var filterId in info.ExtraFilters)
							{
								if (!subscrMsg.IsMatch(message.Type, info.Subscribers[filterId]))
									set.Remove(filterId);
							}

							if (ids.Length != set.Count)
								ids = [.. set];
						}

						subscrMsg.SetSubscriptionIds(ids);
					}
				}

				break;
			}
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	private void TryAddOrderTransaction(SubscriptionInfo statusInfo, long transactionId, bool warnOnDuplicate = true)
	{
		if (!_subscriptionsById.ContainsKey(transactionId))
		{
			var orderSubscription = new SubscriptionInfo(statusInfo);

			_subscriptionsById.Add(transactionId, orderSubscription);

			statusInfo.Linked.Add(transactionId);
		}
		else if (warnOnDuplicate)
			LogWarning("Order's transaction {0} was handled before.", transactionId);
	}

	private async ValueTask ClearState(CancellationToken cancellationToken)
	{
		using (await _sync.LockAsync(cancellationToken))
		{
			_subscriptionsByKey.Clear();
			_subscriptionsById.Clear();
			_skipSubscriptions.Clear();
			_unsubscribeRequests.Clear();
		}
	}

	private async ValueTask ProcessReset(Message message, CancellationToken cancellationToken)
	{
		await ClearState(cancellationToken);

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	private async ValueTask ProcessInSubscriptionMessage(ISubscriptionMessage message, CancellationToken cancellationToken)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var transId = message.TransactionId;
		var isSubscribe = message.IsSubscribe;

		ISubscriptionMessage sendInMsg = null;
		Message[] sendOutMsgs = null;

		using (await _sync.LockAsync(cancellationToken))
		{
			if (isSubscribe)
			{
				if (message.SpecificItemRequest || message.IsHistoryOnly())
				{
					_skipSubscriptions.Add(message.TransactionId);
					sendInMsg = message;
				}
				else
				{
					var dataType = message.DataType;
					var secId = default(SecurityId);

					var extraFilter = false;

					if (message is ISecurityIdMessage secIdMsg)
					{
						secId = secIdMsg.SecurityId;

						if (secId == default && IsSecurityRequired(dataType))
							LogWarning("Subscription {0} required security id.", dataType);
						else if (secId != default && !IsSecurityRequired(dataType))
						{
							//LogWarning("Subscription {0} doesn't required security id.", dataType);
							extraFilter = true;
							secId = default;
						}
					}

					if (!extraFilter)
						extraFilter = message.FilterEnabled;

					var key = (dataType, secId);

					if (!_subscriptionsByKey.TryGetValue(key, out var info))
					{
						LogDebug("Subscription {0} ({1}/{2}) initial.", transId, dataType, secId);

						sendInMsg = message;

						info = new(message.TypedClone());

						_subscriptionsByKey.Add(key, info);
					}
					else
					{
						if (message.From is not null)
						{
							// history+live must be processed anyway but without live part
							var clone = message.TypedClone();
							clone.To = CurrentTimeUtc;
							info.HistLive.Add(transId);
							sendInMsg = clone;
						}
						else
						{
							LogDebug("Subscription {0} joined to {1}.", transId, info.Subscription.TransactionId);

							var resultMsg = message.CreateResult();

							sendOutMsgs =
							[
								message.CreateResponse(),
								resultMsg,
							];

							info.OnlineSubscribers.Add(transId);
						}
					}

					_subscriptionsById.Add(transId, info);

					info.Subscribers.Add(transId, message.TypedClone());

					if (extraFilter)
						info.ExtraFilters.Add(transId);
				}
			}
			else
			{
				ISubscriptionMessage MakeUnsubscribe(ISubscriptionMessage m, long subscriptionId)
				{
					m.IsSubscribe = false;
					m.TransactionId = transId;
					m.OriginalTransactionId = subscriptionId;

					return m;
				}

				var originId = message.OriginalTransactionId;

				if (_subscriptionsById.TryGetValue(originId, out var info))
				{
					if (!info.Subscribers.Remove(originId))
					{
						sendOutMsgs =
						[
							originId.CreateSubscriptionResponse(new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId)))
						];
					}
					else
					{
						info.OnlineSubscribers.Remove(originId);

						info.ExtraFilters.Remove(originId);

						if (info.Linked.Count > 0)
						{
							foreach (var linked in info.Linked)
								_subscriptionsById.Remove(linked);
						}

						if (info.Subscribers.Count == 0)
						{
							_subscriptionsByKey.RemoveByValue(info);
							_subscriptionsById.Remove(originId);

							if (info.State.IsActive())
							{
								_unsubscribeRequests.Add(transId);

								// copy full subscription's details into unsubscribe request
								sendInMsg = MakeUnsubscribe(info.Subscription.TypedClone(), info.Subscription.TransactionId);
							}
							else
								LogWarning(LocalizedStrings.SubscriptionInState, originId, info.State);
						}
						else
						{
							sendOutMsgs = [message.CreateResult()];
						}
					}
				}
				else if (_skipSubscriptions.Remove(originId))
				{
					sendInMsg = message;
				}
				else
				{
					sendOutMsgs =
					[
						originId.CreateSubscriptionResponse(new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId)))
					];
				}
			}
		}

		if (sendOutMsgs != null)
		{
			foreach (var sendOutMsg in sendOutMsgs)
			{
				LogInfo("Out: {0}", sendOutMsg);
				await RaiseNewOutMessageAsync(sendOutMsg, cancellationToken);
			}
		}

		if (sendInMsg != null)
		{
			LogDebug("In: {0}", sendInMsg);
			await base.OnSendInMessageAsync((Message)sendInMsg, cancellationToken);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="SubscriptionOnlineMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new SubscriptionOnlineMessageAdapter(InnerAdapter.TypedClone());
	}
}