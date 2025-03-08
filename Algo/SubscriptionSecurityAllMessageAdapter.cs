namespace StockSharp.Algo;

/// <summary>
/// Security ALL subscription counter adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SubscriptionSecurityAllMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
public class SubscriptionSecurityAllMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private abstract class BaseSubscription(MarketDataMessage origin)
	{
		public MarketDataMessage Origin { get; } = origin ?? throw new ArgumentNullException(nameof(origin));
	}

	private class ChildSubscription(SubscriptionSecurityAllMessageAdapter.ParentSubscription parent, MarketDataMessage origin) : BaseSubscription(origin)
	{
		public ParentSubscription Parent { get; } = parent ?? throw new ArgumentNullException(nameof(parent));
		public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;
		public List<ISubscriptionIdMessage> Suspended { get; } = [];
		public CachedSynchronizedDictionary<long, MarketDataMessage> Subscribers { get; } = [];
	}

	private class ParentSubscription(MarketDataMessage origin) : BaseSubscription(origin)
	{
		public CachedSynchronizedPairSet<long, MarketDataMessage> Alls = [];
		public SynchronizedDictionary<SecurityId, CachedSynchronizedSet<long>> NonAlls = [];
		public Dictionary<SecurityId, ChildSubscription> Child { get; } = [];
	}

	private readonly SyncObject _sync = new();

	private readonly Dictionary<long, RefPair<long, SubscriptionStates>> _pendingLoopbacks = [];
	private readonly Dictionary<long, ParentSubscription> _parents = [];
	private readonly Dictionary<long, ParentSubscription> _unsubscribes = [];
	private readonly Dictionary<long, Tuple<ParentSubscription, MarketDataMessage>> _requests = [];
	private readonly List<ChildSubscription> _toFlush = [];

	private void ClearState()
	{
		lock (_sync)
		{
			_pendingLoopbacks.Clear();
			_parents.Clear();
			_unsubscribes.Clear();
			_requests.Clear();
			_toFlush.Clear();
		}
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				ClearState();
				break;

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (mdMsg.IsSubscribe)
				{
					var transId = mdMsg.TransactionId;

					lock (_sync)
					{
						if (_pendingLoopbacks.TryGetAndRemove(transId, out var tuple))
						{
							if (tuple.Second != SubscriptionStates.Stopped)
							{
								if (tuple.Second == SubscriptionStates.Finished)
								{
									RaiseNewOutMessage(new SubscriptionFinishedMessage
									{
										OriginalTransactionId = transId,
									});
								}
								else
								{
									RaiseNewOutMessage(new SubscriptionResponseMessage
									{
										OriginalTransactionId = transId,
										Error = new InvalidOperationException(LocalizedStrings.SubscriptionInvalidState.Put(transId, tuple.Second)),
									});
								}

								return true;
							}

							var parent = _parents[tuple.First];
							var child = parent.Child[mdMsg.SecurityId];
							child.State = SubscriptionStates.Online;

							if (child.Suspended.Count > 0)
								_toFlush.Add(child);

							LogDebug("New ALL map (active): {0}/{1} TrId={2}", child.Origin.SecurityId, child.Origin.DataType2, mdMsg.TransactionId);
							
							_requests.Add(transId, Tuple.Create(parent, mdMsg.TypedClone()));

							// for child subscriptions make online (or finished) immediatelly
							RaiseNewOutMessage(mdMsg.CreateResponse());
							//RaiseNewOutMessage(mdMsg.CreateResult());
							return true;
						}
						else
						{
							if (!IsSecurityRequired(mdMsg.DataType2) || mdMsg.SecurityId == default)
							{
								mdMsg = mdMsg.TypedClone();
								
								var parent = _parents.FirstOrDefault(p => p.Value.Origin.DataType2 == mdMsg.DataType2).Value;

								void AddSubscription()
								{
									if (mdMsg.SecurityId == default)
									{
										// first ALL is initiator
										parent.Alls.Add(transId, mdMsg);
									}
									else
									{
										parent.NonAlls.SafeAdd(mdMsg.SecurityId).Add(transId);
									}

									_requests.Add(transId, Tuple.Create(parent, mdMsg));
								}

								if (parent == null)
								{
									parent = new ParentSubscription(mdMsg);
									_parents.Add(transId, parent);

									AddSubscription();

									if (mdMsg.DataType2 != DataType.News)
									{
										mdMsg = mdMsg.TypedClone();
										// do not specify security cause adapter doesn't require it
										Extensions.AllSecurity.CopyEx(mdMsg, false);
										message = mdMsg;
									}

									LogInfo("Sec ALL {0} subscribing.", transId);
								}
								else
								{
									AddSubscription();

									// for child subscriptions make online (or finished) immediatelly
									RaiseNewOutMessage(mdMsg.CreateResponse());
									//RaiseNewOutMessage(mdMsg.CreateResult());
									return true;
								}
							}
						}
					}
				}
				else
				{
					var originId = mdMsg.OriginalTransactionId;

					lock (_sync)
					{
						var found = false;

						if (!_requests.TryGetAndRemove(originId, out var tuple))
							break;

						//LogDebug("Sec ALL child {0} unsubscribe.", originId);

						var parent = tuple.Item1;
						var request = tuple.Item2;
						var secId = request.SecurityId;
						var transId = request.TransactionId;

						if (parent.Alls.RemoveByValue(request))
							found = true;
						else if (parent.NonAlls.TryGetValue(secId, out var set) && set.Remove(transId))
						{
							if (set.Count == 0)
								parent.NonAlls.Remove(secId);

							found = true;
						}
						else
						{
							if (parent.Child.TryGetValue(secId, out var child))
							{
								if (child.Subscribers.Remove(transId))
								{
									found = true;

									if (child.Subscribers.Count == 0)
										parent.Child.Remove(secId);
								}
							}
						}

						if (found)
						{
							if (parent.Alls.Count == 0 && parent.NonAlls.Count == 0 && parent.Child.Count == 0)
							{
								// last unsubscribe is not initial subscription
								if (parent.Origin.TransactionId != originId)
								{
									mdMsg = mdMsg.TypedClone();
									mdMsg.OriginalTransactionId = parent.Origin.TransactionId;
										
									message = mdMsg;
								}

								_unsubscribes.Add(mdMsg.TransactionId, parent);
									
								break;
							}
						}

						RaiseNewOutMessage(mdMsg.CreateResponse(found ? null : new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId))));
						return true;
					}
				}

				break;
			}
		}

		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		List<Message> extra = null;

		switch (message.Type)
		{
			case MessageTypes.Disconnect:
			case MessageTypes.ConnectionRestored:
			{
				if (message is ConnectionRestoredMessage restoredMsg && !restoredMsg.IsResetState)
					break;

				ClearState();
				break;
			}
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				var originId = responseMsg.OriginalTransactionId;

				if (!responseMsg.IsOk())
				{
					lock (_sync)
					{
						if (_parents.TryGetAndRemove(originId, out var parent))
						{
							LogError("Sec ALL {0} error.", parent.Origin.TransactionId);
						
							extra = [];

							foreach (var child in parent.Child.Values)
							{
								var childId = child.Origin.TransactionId;

								if (_pendingLoopbacks.TryGetValue(childId, out var tuple) && tuple.Second == SubscriptionStates.Stopped)
								{
									// loopback subscription not yet come, so will reply later
									tuple.Second = SubscriptionStates.Error;
								}
								else
									extra.Add(new SubscriptionResponseMessage { OriginalTransactionId = childId, Error = responseMsg.Error });
							}
						}
						else if (_unsubscribes.TryGetAndRemove(originId, out parent))
						{
							LogError("Sec ALL {0} unsubscribe error.", parent.Origin.TransactionId);
							_parents.Remove(parent.Origin.TransactionId);
						}
					}
				}
				else
				{
					if (_unsubscribes.TryGetAndRemove(originId, out var parent))
					{
						LogInfo("Sec ALL {0} unsubscribed.", parent.Origin.TransactionId);
						_parents.Remove(parent.Origin.TransactionId);
					}
				}

				break;
			}
			case MessageTypes.SubscriptionFinished:
			{
				var finishMsg = (SubscriptionFinishedMessage)message;

				lock (_sync)
				{
					if (_parents.TryGetAndRemove(finishMsg.OriginalTransactionId, out var parent))
					{
						extra = [];

						foreach (var child in parent.Child.Values)
						{
							var childId = child.Origin.TransactionId;

							if (_pendingLoopbacks.TryGetValue(childId, out var tuple) && tuple.Second == SubscriptionStates.Stopped)
							{
								// loopback subscription not yet come, so will reply later
								tuple.Second = SubscriptionStates.Finished;
							}
							else
								extra.Add(new SubscriptionFinishedMessage { OriginalTransactionId = childId });
						}
					}
				}

				break;
			}
			default:
			{
				var allMsg = CheckSubscription(ref message);

				if (allMsg != null)
					base.OnInnerAdapterNewOutMessage(allMsg);
				
				break;
			}
		}

		if (message != null)
			base.OnInnerAdapterNewOutMessage(message);

		if (extra != null)
		{
			foreach (var m in extra)
				base.OnInnerAdapterNewOutMessage(m);
		}
	}

	private void ApplySubscriptionIds(ISubscriptionIdMessage subscrMsg, ParentSubscription parent, long[] newIds)
	{
		var ids = subscrMsg.GetSubscriptionIds();
		var initialId = parent.Origin.TransactionId;
		newIds = newIds.Concat(parent.Alls.CachedKeys);

		if (subscrMsg is ISecurityIdMessage secIdMsg && parent.NonAlls.TryGetValue(secIdMsg.SecurityId, out var set))
		{
			newIds = newIds.Concat(set.Cache);
		}

		if (ids.Length == 1 && ids[0] == initialId)
			subscrMsg.SetSubscriptionIds(newIds);
		else
			subscrMsg.SetSubscriptionIds([.. ids.Where(id => id != initialId), .. newIds]);
	}

	private void ApplySubscriptionIds(ISubscriptionIdMessage subscrMsg, ChildSubscription child)
	{
		ApplySubscriptionIds(subscrMsg, child.Parent, child.Subscribers.CachedKeys);
	}

	private SubscriptionSecurityAllMessage CheckSubscription(ref Message message)
	{
		lock (_sync)
		{
			if (_toFlush.Count > 0)
			{
				var childs = _toFlush.CopyAndClear();
				
				foreach (var child in childs)
				{
					LogDebug("ALL flush: {0}/{1}, cnt={2}", child.Origin.SecurityId, child.Origin.DataType2, child.Suspended.Count);

					foreach (var msg in child.Suspended.CopyAndClear())
					{
						ApplySubscriptionIds(msg, child);
						RaiseNewOutMessage((Message)msg);
					}
				}
			}

			if (_parents.Count == 0)
				return null;

			if (message is ISubscriptionIdMessage subscrMsg and ISecurityIdMessage secIdMsg)
			{
				foreach (var parentId in subscrMsg.GetSubscriptionIds())
				{
					if (_parents.TryGetValue(parentId, out var parent))
					{
						// parent subscription has security id (not null)
						if (parent.Origin.SecurityId == secIdMsg.SecurityId)
						{
							ApplySubscriptionIds(subscrMsg, parent, [parent.Origin.TransactionId]);
							return null;
						}

						SubscriptionSecurityAllMessage allMsg = null;

						if (!parent.Child.TryGetValue(secIdMsg.SecurityId, out var child))
						{
							allMsg = new SubscriptionSecurityAllMessage();

							parent.Origin.CopyTo(allMsg);

							allMsg.ParentTransactionId = parentId;
							allMsg.TransactionId = TransactionIdGenerator.GetNextId();
							allMsg.SecurityId = secIdMsg.SecurityId;

							child = new ChildSubscription(parent, allMsg.TypedClone());
							child.Subscribers.Add(allMsg.TransactionId, child.Origin);

							parent.Child.Add(secIdMsg.SecurityId, child);

							allMsg.LoopBack(this, MessageBackModes.Chain);
							_pendingLoopbacks.Add(allMsg.TransactionId, RefTuple.Create(parentId, SubscriptionStates.Stopped));

							LogDebug("New ALL map: {0}/{1} TrId={2}-{3}", child.Origin.SecurityId, child.Origin.DataType2, allMsg.ParentTransactionId, allMsg.TransactionId);
						}

						if (!child.State.IsActive())
						{
							child.Suspended.Add(subscrMsg);
							message = null;

							LogDebug("ALL suspended: {0}/{1}, cnt={2}", child.Origin.SecurityId, child.Origin.DataType2, child.Suspended.Count);
						}
						else
							ApplySubscriptionIds(subscrMsg, child);

						return allMsg;
					}
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Create a copy of <see cref="SubscriptionSecurityAllMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new SubscriptionSecurityAllMessageAdapter(InnerAdapter.TypedClone());
	}
}