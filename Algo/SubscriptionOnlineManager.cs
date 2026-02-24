namespace StockSharp.Algo;

/// <summary>
/// Online subscription processing logic.
/// </summary>
public interface ISubscriptionOnlineManager
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Processing result.</returns>
	ValueTask<(Message[] toInner, Message[] toOut)> ProcessInMessageAsync(Message message, CancellationToken cancellationToken);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Processing result.</returns>
	ValueTask<(Message forward, Message[] extraOut)> ProcessOutMessageAsync(Message message, CancellationToken cancellationToken);
}

/// <summary>
/// Online subscription processing implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionOnlineManager"/>.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
/// <param name="isSecurityRequired">Check if a security id is required for the specified data type.</param>
/// <param name="state">State storage.</param>
public sealed class SubscriptionOnlineManager(ILogReceiver logReceiver, Func<DataType, bool> isSecurityRequired, ISubscriptionOnlineManagerState state) : ISubscriptionOnlineManager
{
	private readonly AsyncLock _sync = new();
	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly Func<DataType, bool> _isSecurityRequired = isSecurityRequired ?? throw new ArgumentNullException(nameof(isSecurityRequired));
	private readonly ISubscriptionOnlineManagerState _state = state ?? throw new ArgumentNullException(nameof(state));

	/// <summary>
	/// State storage.
	/// </summary>
	public ISubscriptionOnlineManagerState State => _state;

	/// <inheritdoc />
	public async ValueTask<(Message[] toInner, Message[] toOut)> ProcessInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		async ValueTask TryAddOrderSubscription(OrderMessage orderMsg)
		{
			using (await _sync.LockAsync(cancellationToken))
			{
				if (_state.TryGetSubscriptionByKey((DataType.Transactions, default(SecurityId)), out var info))
					TryAddOrderTransaction(info, orderMsg.TransactionId);
			}
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
				await ClearState(cancellationToken);
				return ([message], []);

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			case MessageTypes.OrderGroupCancel:
			{
				var orderMsg = (OrderMessage)message;

				await TryAddOrderSubscription(orderMsg);

				return ([message], []);
			}

			default:
			{
				if (message is ISubscriptionMessage subscrMsg)
					return await ProcessInSubscriptionMessage(subscrMsg, cancellationToken);

				return ([message], []);
			}
		}
	}

	/// <inheritdoc />
	public async ValueTask<(Message forward, Message[] extraOut)> ProcessOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		List<Message> extraOut = null;

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
				long[] notifySubscribers = null;

				using (await _sync.LockAsync(cancellationToken))
				{
					if (responseMsg.IsOk())
					{
						if (_state.TryGetSubscriptionById(originTransId, out var info))
						{
							if (!ChangeState(info, originTransId, _state.ContainsUnsubscribeRequest(originTransId) ? SubscriptionStates.Stopped : SubscriptionStates.Active))
								return (null, []);
						}
					}
					else
					{
						if (_state.TryGetAndRemoveSubscriptionById(originTransId, out var info))
						{
							info.OnlineSubscribers.Remove(originTransId);
							info.Subscribers.Remove(originTransId);
							notifySubscribers = info.Subscribers.CachedKeys;

							if (!ChangeState(info, originTransId, SubscriptionStates.Error))
							{
								info.HistLive.Remove(originTransId);
								return (null, []);
							}
						}
					}
				}

				if (notifySubscribers is { Length: > 0 })
				{
					extraOut ??= new List<Message>(notifySubscribers.Length);

					foreach (var subscriber in notifySubscribers)
					{
						_logReceiver.AddInfoLog(LocalizedStrings.SubscriptionNotifySubscriber, responseMsg.OriginalTransactionId, subscriber);
						extraOut.Add(subscriber.CreateSubscriptionResponse(responseMsg.Error));
					}
				}

				break;
			}

			case MessageTypes.SubscriptionOnline:
			{
				var originTransId = ((SubscriptionOnlineMessage)message).OriginalTransactionId;

				using (await _sync.LockAsync(cancellationToken))
				{
					if (_state.TryGetSubscriptionById(originTransId, out var info))
					{
						info.OnlineSubscribers.Add(originTransId);

						if (!ChangeState(info, originTransId, SubscriptionStates.Online))
								return (null, []);

						// promote hist+live subscribers whose history already finished
						foreach (var subId in info.Subscribers.CachedKeys)
						{
							if (subId != originTransId &&
								!info.OnlineSubscribers.Contains(subId) &&
								!info.HistLive.Contains(subId))
							{
								info.OnlineSubscribers.Add(subId);
								extraOut ??= [];
								extraOut.Add(new SubscriptionOnlineMessage { OriginalTransactionId = subId });
							}
						}
					}
				}

				break;
			}

			case MessageTypes.SubscriptionFinished:
			{
				var originTransId = ((SubscriptionFinishedMessage)message).OriginalTransactionId;

				using (await _sync.LockAsync(cancellationToken))
				{
					if (_state.TryGetSubscriptionById(originTransId, out var info))
					{
						if (!ChangeState(info, originTransId, SubscriptionStates.Finished))
						{
							// hist+live subscription's history finished
							info.HistLive.Remove(originTransId);

							if (info.State == SubscriptionStates.Online)
							{
								// main already online — promote this subscriber
								info.OnlineSubscribers.Add(originTransId);
								extraOut ??= [];
								extraOut.Add(new SubscriptionOnlineMessage { OriginalTransactionId = originTransId });
							}

							return (null, extraOut?.ToArray() ?? []);
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
						if (subscrMsg.OriginalTransactionId != 0 && _state.TryGetSubscriptionById(subscrMsg.OriginalTransactionId, out var info))
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
						}
						else
						{
							var dataType = subscrMsg.DataType;
							var secId = (subscrMsg as ISecurityIdMessage)?.SecurityId ?? default;

							if (!_state.TryGetSubscriptionByKey((dataType, secId), out info) && (secId == default || !_state.TryGetSubscriptionByKey((dataType, default(SecurityId)), out info)))
							{
								// Transaction messages (order responses) should pass through even without subscription
								// because they are handled dynamically and may come from nested adapters (e.g., EmulationMessageAdapter)
								if (dataType == DataType.Transactions)
									return (message, []);

								// If the subscription was skipped (e.g., history-only lookup), pass through
								// so that SubscriptionMessageAdapter can handle it
								if (subscrMsg.OriginalTransactionId != 0 && _state.ContainsSkipSubscription(subscrMsg.OriginalTransactionId))
									return (message, []);

								// No subscription found - don't forward message
								return (null, []);
							}
						}

						// For market data in Online state, use OnlineSubscribers; for historical (Active state), use all Subscribers
						var ids = info.IsMarketData && info.State == SubscriptionStates.Online
							? info.OnlineSubscribers.Cache
							: info.Subscribers.CachedKeys;

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

						// If no subscribers after filtering, don't forward
						if (ids.Length == 0)
							return (null, []);

						subscrMsg.SetSubscriptionIds(ids);
					}
				}

				break;
			}
		}

		return (message, extraOut?.ToArray() ?? []);
	}

	private bool ChangeState(ISubscriptionOnlineInfo info, long transId, SubscriptionStates state)
	{
		// secondary hist+live cannot change main subscription state
		if (info.HistLive.Contains(transId))
			return false;

		info.State = info.State.ChangeSubscriptionState(state, info.Subscription.TransactionId, _logReceiver);

		if (!state.IsActive())
		{
			_state.RemoveSubscriptionByKeyValue(info);
			_logReceiver.AddInfoLog(LocalizedStrings.SubscriptionRemoved, info.Subscription.TransactionId);
		}

		return true;
	}

	private void TryAddOrderTransaction(ISubscriptionOnlineInfo statusInfo, long transactionId, bool warnOnDuplicate = true)
	{
		if (!_state.ContainsSubscriptionById(transactionId))
		{
			var orderSubscription = _state.CreateLinkedSubscriptionInfo(statusInfo);

			_state.AddSubscriptionById(transactionId, orderSubscription);

			statusInfo.Linked.Add(transactionId);
		}
		else if (warnOnDuplicate)
			_logReceiver.AddWarningLog("Order's transaction {0} was handled before.", transactionId);
	}

	private async ValueTask ClearState(CancellationToken cancellationToken)
	{
		using (await _sync.LockAsync(cancellationToken))
			_state.Clear();
	}

	private async ValueTask<(Message[] ToInner, Message[] ToOut)> ProcessInSubscriptionMessage(ISubscriptionMessage message, CancellationToken cancellationToken)
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
				// Transaction subscriptions should NOT be skipped even when history-only
				// because we need to track order IDs for linking ExecutionMessage responses
				var shouldSkip = message.SpecificItemRequest || message.IsHistoryOnly();
				if (shouldSkip && message.DataType == DataType.Transactions)
					shouldSkip = false;

				if (shouldSkip)
				{
					_state.AddSkipSubscription(message.TransactionId);
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

						if (secId == default && _isSecurityRequired(dataType))
							_logReceiver.AddWarningLog("Subscription {0} required security id.", dataType);
						else if (secId != default && !_isSecurityRequired(dataType))
						{
							extraFilter = true;
							secId = default;
						}
					}

					if (!extraFilter)
						extraFilter = message.FilterEnabled;

					var key = (dataType, secId);

					if (!_state.TryGetSubscriptionByKey(key, out var info))
					{
						_logReceiver.AddDebugLog("Subscription {0} ({1}/{2}) initial.", transId, dataType, secId);

						sendInMsg = message;

						info = _state.CreateSubscriptionInfo(message.TypedClone());

						_state.AddSubscriptionByKey(key, info);
					}
					else
					{
						if (message.From is not null)
						{
							// history+live must be processed anyway but without live part
							var clone = message.TypedClone();
							clone.To = _logReceiver.CurrentTime;
							info.HistLive.Add(transId);
							sendInMsg = clone;
						}
						else
						{
							_logReceiver.AddDebugLog("Subscription {0} joined to {1}.", transId, info.Subscription.TransactionId);

							var resultMsg = message.CreateResult();

							sendOutMsgs =
							[
								message.CreateResponse(),
								resultMsg,
							];

							info.OnlineSubscribers.Add(transId);
						}
					}

					_state.AddSubscriptionById(transId, info);

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

				if (_state.TryGetSubscriptionById(originId, out var info))
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
								_state.RemoveSubscriptionById(linked);
						}

						if (info.Subscribers.Count == 0)
						{
							_state.RemoveSubscriptionByKeyValue(info);
							_state.RemoveSubscriptionById(originId);

							if (info.State.IsActive())
							{
								_state.AddUnsubscribeRequest(transId);

								// copy full subscription's details into unsubscribe request
								sendInMsg = MakeUnsubscribe(info.Subscription.TypedClone(), info.Subscription.TransactionId);
							}
							else
								_logReceiver.AddWarningLog(LocalizedStrings.SubscriptionInState, originId, info.State);
						}
						else
						{
							sendOutMsgs = [message.CreateResult()];
						}
					}
				}
				else if (_state.RemoveSkipSubscription(originId))
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
				_logReceiver.AddInfoLog("Out: {0}", sendOutMsg);
		}

		if (sendInMsg != null)
			_logReceiver.AddDebugLog("In: {0}", sendInMsg);

		return (ToInner: sendInMsg == null ? [] : [(Message)sendInMsg], ToOut: sendOutMsgs ?? []);
	}
}
