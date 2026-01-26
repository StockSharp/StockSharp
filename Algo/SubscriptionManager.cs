namespace StockSharp.Algo;

/// <summary>
/// Subscription message processing logic.
/// </summary>
public interface ISubscriptionManager
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result.</returns>
	(Message[] toInner, Message[] toOut) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result.</returns>
	(Message forward, Message[] extraOut) ProcessOutMessage(Message message);

	/// <summary>
	/// Notify about a message emitted by the inner adapter before normal processing.
	/// </summary>
	/// <param name="message">Inner adapter message.</param>
	void OnInnerAdapterMessage(Message message);
}

/// <summary>
/// Subscription message processing implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionManager"/>.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
/// <param name="transactionIdGenerator">Transaction id generator.</param>
/// <param name="createProcessSuspendedMessage">Create a message that resumes processing after remapped subscriptions are enqueued.</param>
/// <param name="state">State storage.</param>
public sealed class SubscriptionManager(ILogReceiver logReceiver, IdGenerator transactionIdGenerator, Func<Message> createProcessSuspendedMessage, ISubscriptionManagerState state) : ISubscriptionManager
{
	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly IdGenerator _transactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
	private readonly Func<Message> _createProcessSuspendedMessage = createProcessSuspendedMessage ?? throw new ArgumentNullException(nameof(createProcessSuspendedMessage));
	private readonly ISubscriptionManagerState _state = state ?? throw new ArgumentNullException(nameof(state));

	/// <summary>
	/// State storage.
	/// </summary>
	public ISubscriptionManagerState State => _state;

	/// <inheritdoc />
	public void OnInnerAdapterMessage(Message message)
	{
		if (message.Type != ExtendedMessageTypes.SubscriptionSecurityAll)
			return;

		var allMsg = (SubscriptionSecurityAllMessage)message;
		_state.AddAllSecIdChild(allMsg.TransactionId);
	}

	/// <inheritdoc />
	public (Message[] toInner, Message[] toOut) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_state.Clear();
				return ([message], []);
			}
			case MessageTypes.ProcessSuspended:
			{
				var reMapSubscriptions = _state.GetAndClearReMapSubscriptions();
				return (reMapSubscriptions, []);
			}
			default:
			{
				if (message is ISubscriptionMessage subscrMsg)
					return ProcessInSubscriptionMessage(subscrMsg);

				return ([message], []);
			}
		}
	}

	/// <inheritdoc />
	public (Message forward, Message[] extraOut) ProcessOutMessage(Message message)
	{
		long TryReplaceOriginId(long id)
		{
			if (id == 0)
				return 0;

			return _state.TryGetOriginalId(id, out var prevId) ? prevId : id;
		}

		var prevOriginId = 0L;
		var newOriginId = 0L;

		if (message is IOriginalTransactionIdMessage originIdMsg1)
		{
			newOriginId = originIdMsg1.OriginalTransactionId;
			prevOriginId = originIdMsg1.OriginalTransactionId = TryReplaceOriginId(newOriginId);
		}

		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				if (((SubscriptionResponseMessage)message).IsOk())
				{
					if (_state.TryGetSubscription(prevOriginId, out var subscription, out var state))
					{
						// no need send response after re-subscribe cause response was handled prev time
						if (_state.ContainsReplaceId(newOriginId))
						{
							if (state != SubscriptionStates.Stopped)
								return (null, []);
						}
						else
						{
							var newState = state.ChangeSubscriptionState(SubscriptionStates.Active, subscription.TransactionId, _logReceiver);
							_state.UpdateSubscriptionState(prevOriginId, newState);
						}
					}
				}
				else
				{
					if (!_state.RemoveHistoricalRequest(prevOriginId))
					{
						if (_state.TryGetSubscription(prevOriginId, out var subscription, out var state))
						{
							var newState = state.ChangeSubscriptionState(SubscriptionStates.Error, subscription.TransactionId, _logReceiver);
							_state.UpdateSubscriptionState(prevOriginId, newState);
							_state.RemoveSubscription(prevOriginId);
							_state.RemoveReplaceId(newOriginId);
						}
					}
				}

				break;
			}
			case MessageTypes.SubscriptionOnline:
			{
				if (!_state.TryGetSubscription(prevOriginId, out var subscription, out var state))
					break;

				if (_state.ContainsReplaceId(newOriginId))
				{
					// no need send response after re-subscribe cause response was handled prev time
					if (state == SubscriptionStates.Online)
						return (null, []);
				}
				else
				{
					var newState = state.ChangeSubscriptionState(SubscriptionStates.Online, subscription.TransactionId, _logReceiver);
					_state.UpdateSubscriptionState(prevOriginId, newState);
				}

				break;
			}
			case MessageTypes.SubscriptionFinished:
			{
				if (_state.ContainsReplaceId(newOriginId))
					return (null, []);

				_state.RemoveHistoricalRequest(prevOriginId);

				if (_state.TryGetSubscription(newOriginId, out var subscription, out var state))
				{
					var newState = state.ChangeSubscriptionState(SubscriptionStates.Finished, subscription.TransactionId, _logReceiver);
					_state.UpdateSubscriptionState(newOriginId, newState);
				}

				break;
			}
			default:
			{
				if (message is ISubscriptionIdMessage subscrMsg)
				{
					var ids = subscrMsg.GetSubscriptionIds();

					if (ids.Length == 0)
					{
						if (subscrMsg.OriginalTransactionId != 0 && _state.ContainsHistoricalRequest(subscrMsg.OriginalTransactionId))
							subscrMsg.SetSubscriptionIds(subscriptionId: subscrMsg.OriginalTransactionId);
					}
					else
					{
						// Filter out unknown subscription IDs
						var validIds = new List<long>(ids.Length);
						for (var i = 0; i < ids.Length; i++)
						{
							var id = ids[i];
							var origId = _state.TryGetOriginalId(id, out var oid) ? oid : id;

							// Check if subscription exists and is active
							if (_state.TryGetSubscription(origId, out _, out var subState) && subState.IsActive())
								validIds.Add(origId);
							else if (_state.ContainsHistoricalRequest(origId))
								validIds.Add(origId);
						}

						// If no valid subscriptions, don't forward the message
						if (validIds.Count == 0)
							return (null, []);

						// Update subscription IDs with valid ones only
						if (validIds.Count != ids.Length || _state.ReplaceIdCount > 0)
							subscrMsg.SetSubscriptionIds([.. validIds]);
					}

					if (subscrMsg is ISecurityIdMessage secIdMsg &&
						secIdMsg.SecurityId == default &&
						subscrMsg.OriginalTransactionId != default)
					{
						SecurityId getSecId(ISecurityIdMessage secIdMsg)
						{
							var secId = secIdMsg.SecurityId;

							if (secId.Native is not null && !secId.SecurityCode.IsEmpty() && !secId.BoardCode.IsEmpty())
								secId.Native = null;

							return secId;
						}

						if (_state.TryGetSubscription(subscrMsg.OriginalTransactionId, out var subscription, out _) &&
							subscription is ISecurityIdMessage subscriptionMsg &&
							!subscriptionMsg.IsAllSecurity())
						{
							secIdMsg.SecurityId = getSecId(subscriptionMsg);
						}
						else if (_state.TryGetHistoricalRequest(subscrMsg.OriginalTransactionId, out var hist) &&
							hist is ISecurityIdMessage subscriptionMsg2 &&
							!subscriptionMsg2.IsAllSecurity())
						{
							secIdMsg.SecurityId = getSecId(subscriptionMsg2);
						}
					}
				}

				break;
			}
		}

		Message[] extraOut = [];

		if (message.Type == MessageTypes.ConnectionRestored)
		{
			if (!((ConnectionRestoredMessage)message).IsResetState)
				return (message, []);

			Message suspended = null;

			_state.ClearReplaceIds();
			_state.ClearReMapSubscriptions();

			foreach (var (transactionId, subscription) in _state.GetActiveSubscriptions())
			{
				var clone = subscription.TypedClone();
				clone.TransactionId = _transactionIdGenerator.GetNextId();

				_state.AddReplaceId(clone.TransactionId, transactionId);

				_logReceiver.AddInfoLog("Re-map subscription: {0}->{1} for '{2}'.", transactionId, clone.TransactionId, subscription);

				_state.AddReMapSubscription((Message)clone);
			}

			if (_state.ReMapSubscriptionCount > 0)
				suspended = _createProcessSuspendedMessage();

			if (suspended != null)
				extraOut = [suspended];
		}

		return (message, extraOut);
	}

	private (Message[] ToInner, Message[] ToOut) ProcessInSubscriptionMessage(ISubscriptionMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var transId = message.TransactionId;
		var isSubscribe = message.IsSubscribe;

		ISubscriptionMessage sendInMsg = null;
		Message[] sendOutMsgs = null;

		if (isSubscribe)
		{
			if (message.SpecificItemRequest)
			{
				sendInMsg = message;
			}
			else
			{
				var now = _logReceiver.CurrentTimeUtc;

				if (message.From > now)
				{
					message = message.TypedClone();
					message.From = now;
				}

				if (message.From >= message.To)
				{
					sendOutMsgs = [message.CreateResult()];
				}
				else
				{
					if (_state.ContainsReplaceId(transId))
					{
						sendInMsg = message;
					}
					else
					{
						var clone = message.TypedClone();

						if (message.IsHistoryOnly())
							_state.AddHistoricalRequest(transId, clone);
						else
							_state.AddSubscription(transId, clone, SubscriptionStates.Stopped);

						sendInMsg = message;
					}
				}
			}
		}
		else
		{
			ISubscriptionMessage MakeUnsubscribe(ISubscriptionMessage m)
			{
				m = m.TypedClone();

				m.IsSubscribe = false;
				m.OriginalTransactionId = m.TransactionId;
				m.TransactionId = transId;

				if (_state.TryGetNewId(m.OriginalTransactionId, out var oldOriginId))
					m.OriginalTransactionId = oldOriginId;

				return m;
			}

			var originId = message.OriginalTransactionId;

			if (_state.TryGetAndRemoveHistoricalRequest(originId, out var subscription))
			{
				sendInMsg = MakeUnsubscribe(subscription);
			}
			else if (_state.TryGetSubscription(originId, out var info, out var state))
			{
				if (state.IsActive())
				{
					// copy full subscription's details into unsubscribe request
					sendInMsg = MakeUnsubscribe(info);
					var newState = state.ChangeSubscriptionState(SubscriptionStates.Stopped, info.TransactionId, _logReceiver);
					_state.UpdateSubscriptionState(originId, newState);
				}
				else
					_logReceiver.AddWarningLog(LocalizedStrings.SubscriptionInState, originId, state);
			}
			else
			{
				sendOutMsgs =
				[
					(originId.CreateSubscriptionResponse(new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId))))
				];
			}
		}

		if (sendInMsg != null)
			_logReceiver.AddDebugLog("In: {0}", sendInMsg);

		if (sendOutMsgs != null)
		{
			foreach (var sendOutMsg in sendOutMsgs)
				_logReceiver.AddDebugLog("Out: {0}", sendOutMsg);
		}

		return (ToInner: sendInMsg == null ? [] : [(Message)sendInMsg], ToOut: sendOutMsgs ?? []);
	}
}
