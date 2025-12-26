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
public sealed class SubscriptionManager(ILogReceiver logReceiver, IdGenerator transactionIdGenerator, Func<Message> createProcessSuspendedMessage) : ISubscriptionManager
{
	private sealed class SubscriptionInfo(ISubscriptionMessage subscription)
	{
		public ISubscriptionMessage Subscription { get; } = subscription ?? throw new ArgumentNullException(nameof(subscription));

		public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;

		public override string ToString() => Subscription.ToString();
	}

	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly IdGenerator _transactionIdGenerator = transactionIdGenerator ?? throw new ArgumentNullException(nameof(transactionIdGenerator));
	private readonly Func<Message> _createProcessSuspendedMessage = createProcessSuspendedMessage ?? throw new ArgumentNullException(nameof(createProcessSuspendedMessage));
	private readonly Lock _sync = new();

	private readonly Dictionary<long, ISubscriptionMessage> _historicalRequests = [];
	private readonly Dictionary<long, SubscriptionInfo> _subscriptionsById = [];
	private readonly PairSet<long, long> _replaceId = [];
	private readonly HashSet<long> _allSecIdChilds = [];
	private readonly List<Message> _reMapSubscriptions = [];

	/// <inheritdoc />
	public void OnInnerAdapterMessage(Message message)
	{
		if (message.Type != ExtendedMessageTypes.SubscriptionSecurityAll)
			return;

		var allMsg = (SubscriptionSecurityAllMessage)message;

		using (_sync.EnterScope())
			_allSecIdChilds.Add(allMsg.TransactionId);
	}

	/// <inheritdoc />
	public (Message[] toInner, Message[] toOut) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				ClearState();
				return ([message], []);
			}
			case MessageTypes.ProcessSuspended:
			{
				Message[] reMapSubscriptions;

				using (_sync.EnterScope())
					reMapSubscriptions = _reMapSubscriptions.CopyAndClear();

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

			using (_sync.EnterScope())
				return _replaceId.TryGetValue(id, out var prevId) ? prevId : id;
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
				using (_sync.EnterScope())
				{
					if (((SubscriptionResponseMessage)message).IsOk())
					{
						if (_subscriptionsById.TryGetValue(prevOriginId, out var info))
						{
							// no need send response after re-subscribe cause response was handled prev time
							if (_replaceId.ContainsKey(newOriginId))
							{
								if (info.State != SubscriptionStates.Stopped)
									return (null, []);
							}
							else
								ChangeState(info, SubscriptionStates.Active);
						}
					}
					else
					{
						if (!_historicalRequests.Remove(prevOriginId))
						{
							if (_subscriptionsById.TryGetAndRemove(prevOriginId, out var info))
							{
								ChangeState(info, SubscriptionStates.Error);

								_replaceId.Remove(newOriginId);
							}
						}
					}
				}

				break;
			}
			case MessageTypes.SubscriptionOnline:
			{
				using (_sync.EnterScope())
				{
					if (!_subscriptionsById.TryGetValue(prevOriginId, out var info))
						break;

					if (_replaceId.ContainsKey(newOriginId))
					{
						// no need send response after re-subscribe cause response was handled prev time

						if (info.State == SubscriptionStates.Online)
							return (null, []);
					}
					else
						ChangeState(info, SubscriptionStates.Online);
				}

				break;
			}
			case MessageTypes.SubscriptionFinished:
			{
				using (_sync.EnterScope())
				{
					if (_replaceId.ContainsKey(newOriginId))
						return (null, []);

					_historicalRequests.Remove(prevOriginId);

					if (_subscriptionsById.TryGetValue(newOriginId, out var info))
						ChangeState(info, SubscriptionStates.Finished);
				}

				break;
			}
			default:
			{
				if (message is ISubscriptionIdMessage subscrMsg)
				{
					using (_sync.EnterScope())
					{
						var ids = subscrMsg.GetSubscriptionIds();

						if (ids.Length == 0)
						{
							if (subscrMsg.OriginalTransactionId != 0 && _historicalRequests.ContainsKey(subscrMsg.OriginalTransactionId))
								subscrMsg.SetSubscriptionIds(subscriptionId: subscrMsg.OriginalTransactionId);
						}
						else
						{
							using (_sync.EnterScope())
							{
								if (_replaceId.Count > 0)
									subscrMsg.SetSubscriptionIds([.. ids.Select(id => _replaceId.TryGetValue2(id) ?? id)]);
							}
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

							if (_subscriptionsById.TryGetValue(subscrMsg.OriginalTransactionId, out var info) &&
								info.Subscription is ISecurityIdMessage subscriptionMsg &&
								!subscriptionMsg.IsAllSecurity())
							{
								secIdMsg.SecurityId = getSecId(subscriptionMsg);
							}
							else if (_historicalRequests.TryGetValue(subscrMsg.OriginalTransactionId, out var hist) &&
								hist is ISecurityIdMessage subscriptionMsg2 &&
								!subscriptionMsg2.IsAllSecurity())
							{
								secIdMsg.SecurityId = getSecId(subscriptionMsg2);
							}
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

			using (_sync.EnterScope())
			{
				_replaceId.Clear();
				_reMapSubscriptions.Clear();

				_reMapSubscriptions.AddRange(_subscriptionsById.Values.Distinct().Where(i => i.State.IsActive()).Select(i =>
				{
					var subscription = i.Subscription.TypedClone();
					subscription.TransactionId = _transactionIdGenerator.GetNextId();

					_replaceId.Add(subscription.TransactionId, i.Subscription.TransactionId);

					_logReceiver.AddInfoLog("Re-map subscription: {0}->{1} for '{2}'.", i.Subscription.TransactionId, subscription.TransactionId, i.Subscription);

					return (Message)subscription;
				}));

				if (_reMapSubscriptions.Count > 0)
					suspended = _createProcessSuspendedMessage();
			}

			if (suspended != null)
				extraOut = [suspended];
		}

		return (message, extraOut);
	}

	private void ClearState()
	{
		using (_sync.EnterScope())
		{
			_historicalRequests.Clear();
			_subscriptionsById.Clear();
			_replaceId.Clear();
			_allSecIdChilds.Clear();
			_reMapSubscriptions.Clear();
		}
	}

	private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
	{
		info.State = info.State.ChangeSubscriptionState(state, info.Subscription.TransactionId, _logReceiver);
	}

	private (Message[] ToInner, Message[] ToOut) ProcessInSubscriptionMessage(ISubscriptionMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var transId = message.TransactionId;
		var isSubscribe = message.IsSubscribe;

		ISubscriptionMessage sendInMsg = null;
		Message[] sendOutMsgs = null;

		using (_sync.EnterScope())
		{
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
						if (_replaceId.ContainsKey(transId))
						{
							sendInMsg = message;
						}
						else
						{
							var clone = message.TypedClone();

							if (message.IsHistoryOnly())
								_historicalRequests.Add(transId, clone);
							else
								_subscriptionsById.Add(transId, new SubscriptionInfo(clone));

							sendInMsg = message;
						}

						//isInfoLevel = !_allSecIdChilds.Contains(transId);
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

					if (_replaceId.TryGetKey(m.OriginalTransactionId, out var oldOriginId))
						m.OriginalTransactionId = oldOriginId;

					return m;
				}

				var originId = message.OriginalTransactionId;

				if (_historicalRequests.TryGetAndRemove(originId, out var subscription))
				{
					sendInMsg = MakeUnsubscribe(subscription);
				}
				else if (_subscriptionsById.TryGetValue(originId, out var info))
				{
					if (info.State.IsActive())
					{
						// copy full subscription's details into unsubscribe request
						sendInMsg = MakeUnsubscribe(info.Subscription);
						ChangeState(info, SubscriptionStates.Stopped);
					}
					else
						_logReceiver.AddWarningLog(LocalizedStrings.SubscriptionInState, originId, info.State);
				}
				else
				{
					sendOutMsgs =
					[
						(originId.CreateSubscriptionResponse(new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId))))
					];
				}

				//if (sendInMsg != null)
				//	isInfoLevel = !_allSecIdChilds.Contains(originId);
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
