namespace StockSharp.Algo;

/// <summary>
/// Subscription counter adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
public class SubscriptionMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private class SubscriptionInfo(ISubscriptionMessage subscription)
	{
		public ISubscriptionMessage Subscription { get; } = subscription ?? throw new ArgumentNullException(nameof(subscription));

		public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;

		public override string ToString() => Subscription.ToString();
	}

	private readonly SyncObject _sync = new();

	private readonly Dictionary<long, ISubscriptionMessage> _historicalRequests = [];
	private readonly Dictionary<long, SubscriptionInfo> _subscriptionsById = [];
	private readonly PairSet<long, long> _replaceId = [];
	private readonly HashSet<long> _allSecIdChilds = [];
	private readonly List<Message> _reMapSubscriptions = [];

	/// <summary>
	/// Restore subscription on reconnect.
	/// </summary>
	/// <remarks>
	/// Error case like connection lost etc.
	/// </remarks>
	public bool IsRestoreSubscriptionOnErrorReconnect { get; set; }

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				return ProcessReset(message);

			case MessageTypes.ProcessSuspended:
			{
				Message[] reMapSubscriptions;

				lock (_sync)
					reMapSubscriptions = _reMapSubscriptions.CopyAndClear();

				foreach (var reMapSubscription in reMapSubscriptions)
					base.OnSendInMessage(reMapSubscription);

				return true;
			}

			default:
			{
				if (message is ISubscriptionMessage subscrMsg)
					return ProcessInSubscriptionMessage(subscrMsg);
				else
					return base.OnSendInMessage(message);
			}
		}
	}

	private bool ProcessReset(Message message)
	{
		lock (_sync)
		{
			_historicalRequests.Clear();
			_subscriptionsById.Clear();
			_replaceId.Clear();
			_allSecIdChilds.Clear();
			_reMapSubscriptions.Clear();
		}

		return base.OnSendInMessage(message);
	}

	private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
	{
		info.State = info.State.ChangeSubscriptionState(state, info.Subscription.TransactionId, this);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		long TryReplaceOriginId(long id)
		{
			if (id == 0)
				return 0;

			lock (_sync)
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
				lock (_sync)
				{
					if (((SubscriptionResponseMessage)message).IsOk())
					{
						if (_subscriptionsById.TryGetValue(prevOriginId, out var info))
						{
							// no need send response after re-subscribe cause response was handled prev time
							if (_replaceId.ContainsKey(newOriginId))
							{
								if (info.State != SubscriptionStates.Stopped)
									return;
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
				lock (_sync)
				{
					if (!_subscriptionsById.TryGetValue(prevOriginId, out var info))
						break;

					if (_replaceId.ContainsKey(newOriginId))
					{
						// no need send response after re-subscribe cause response was handled prev time

						if (info.State == SubscriptionStates.Online)
							return;
					}
					else
						ChangeState(info, SubscriptionStates.Online);
				}

				break;
			}

			case MessageTypes.SubscriptionFinished:
			{
				lock (_sync)
				{
					if (_replaceId.ContainsKey(newOriginId))
						return;

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
					lock (_sync)
					{
						var ids = subscrMsg.GetSubscriptionIds();

						if (ids.Length == 0)
						{
							if (subscrMsg.OriginalTransactionId != 0 && _historicalRequests.ContainsKey(subscrMsg.OriginalTransactionId))
								subscrMsg.SetSubscriptionIds(subscriptionId: subscrMsg.OriginalTransactionId);
						}
						else
						{
							lock (_sync)
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

		base.OnInnerAdapterNewOutMessage(message);

		switch (message.Type)
		{
			case MessageTypes.ConnectionRestored:
			{
				if (!((ConnectionRestoredMessage)message).IsResetState)
					break;

				ProcessSuspendedMessage supended = null;

				lock (_sync)
				{
					_replaceId.Clear();
					_reMapSubscriptions.Clear();

					_reMapSubscriptions.AddRange(_subscriptionsById.Values.Distinct().Where(i => i.State.IsActive()).Select(i =>
					{
						var subscription = i.Subscription.TypedClone();
						subscription.TransactionId = TransactionIdGenerator.GetNextId();

						_replaceId.Add(subscription.TransactionId, i.Subscription.TransactionId);

						LogInfo("Re-map subscription: {0}->{1} for '{2}'.", i.Subscription.TransactionId, subscription.TransactionId, i.Subscription);

						return (Message)subscription;
					}));

					if (_reMapSubscriptions.Count > 0)
						supended = new ProcessSuspendedMessage(this);
				}

				if (supended != null)
					base.OnInnerAdapterNewOutMessage(supended);

				break;
			}
		}
	}

	/// <inheritdoc />
	protected override void InnerAdapterNewOutMessage(Message message)
	{
		switch (message.Type)
		{
			case ExtendedMessageTypes.SubscriptionSecurityAll:
			{
				var allMsg = (SubscriptionSecurityAllMessage)message;

				lock (_sync)
					_allSecIdChilds.Add(allMsg.TransactionId);

				break;
			}
		}

		base.InnerAdapterNewOutMessage(message);
	}

	private bool ProcessInSubscriptionMessage(ISubscriptionMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var transId = message.TransactionId;
		var isSubscribe = message.IsSubscribe;

		ISubscriptionMessage sendInMsg = null;
		Message[] sendOutMsgs = null;

		var isInfoLevel = true;

		lock (_sync)
		{
			if (isSubscribe)
			{
				if (message.SpecificItemRequest)
				{
					sendInMsg = message;
				}
				else
				{
					var now = CurrentTime;

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

						isInfoLevel = !_allSecIdChilds.Contains(transId);
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
						LogWarning(LocalizedStrings.SubscriptionInState, originId, info.State);
				}
				else
				{
					sendOutMsgs =
					[
						(originId.CreateSubscriptionResponse(new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId))))
					];
				}

				if (sendInMsg != null)
					isInfoLevel = !_allSecIdChilds.Contains(originId);
			}
		}

		var retVal = true;

		if (sendInMsg != null)
		{
			if (isInfoLevel)
				LogDebug("In: {0}", sendInMsg);
			else
				LogDebug("In: {0}", sendInMsg);

			retVal = base.OnSendInMessage((Message)sendInMsg);
		}

		if (sendOutMsgs != null)
		{
			foreach (var sendOutMsg in sendOutMsgs)
			{
				LogDebug("Out: {0}", sendOutMsg);
				RaiseNewOutMessage(sendOutMsg);	
			}
		}

		return retVal;
	}

	/// <summary>
	/// Create a copy of <see cref="SubscriptionMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new SubscriptionMessageAdapter(InnerAdapter.TypedClone())
		{
			IsRestoreSubscriptionOnErrorReconnect = IsRestoreSubscriptionOnErrorReconnect,
		};
	}
}