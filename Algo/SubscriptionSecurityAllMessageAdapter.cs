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
	private class ParentSubscription(MarketDataMessage origin)
	{
		public MarketDataMessage Origin { get; } = origin ?? throw new ArgumentNullException(nameof(origin));
		public CachedSynchronizedPairSet<long, MarketDataMessage> Alls = [];
		public SynchronizedDictionary<SecurityId, CachedSynchronizedSet<long>> NonAlls = [];
	}

	private readonly AsyncLock _sync = new();

	private readonly Dictionary<long, ParentSubscription> _parents = [];
	private readonly Dictionary<long, ParentSubscription> _unsubscribes = [];
	private readonly Dictionary<long, (ParentSubscription parent, MarketDataMessage request)> _requests = [];

	private async ValueTask ClearState(CancellationToken cancellationToken)
	{
		using (await _sync.LockAsync(cancellationToken))
		{
			_parents.Clear();
			_unsubscribes.Clear();
			_requests.Clear();
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				await ClearState(cancellationToken);
				break;

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (mdMsg.IsSubscribe)
				{
					var transId = mdMsg.TransactionId;
					Message outMsg = null;

					using (await _sync.LockAsync(cancellationToken))
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

								_requests.Add(transId, (parent, mdMsg));
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
								outMsg = mdMsg.CreateResponse();
							}
						}
					}

					if (outMsg != null)
					{
						await RaiseNewOutMessageAsync(outMsg, cancellationToken);
						return;
					}
				}
				else
				{
					var originId = mdMsg.OriginalTransactionId;

					using (await _sync.LockAsync(cancellationToken))
					{
						var found = false;

						if (!_requests.TryGetAndRemove(originId, out var tuple))
							break;

						//LogDebug("Sec ALL child {0} unsubscribe.", originId);

						var parent = tuple.parent;
						var request = tuple.request;
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

						if (found)
						{
							if (parent.Alls.Count == 0 && parent.NonAlls.Count == 0)
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

						await RaiseNewOutMessageAsync(mdMsg.CreateResponse(found ? null : new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId))), cancellationToken);
						return;
					}
				}

				break;
			}
		}

		await base.OnSendInMessageAsync(message, cancellationToken);
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
				var originId = responseMsg.OriginalTransactionId;

				if (!responseMsg.IsOk())
				{
					using (await _sync.LockAsync(cancellationToken))
					{
						if (_parents.TryGetAndRemove(originId, out _))
						{
							LogError("Sec ALL {0} error.", originId);
						}
						else if (_unsubscribes.TryGetAndRemove(originId, out var parent))
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

				using (await _sync.LockAsync(cancellationToken))
					_parents.Remove(finishMsg.OriginalTransactionId);

				break;
			}
			default:
			{
				if (_parents.Count > 0 && message is ISubscriptionIdMessage subscrMsg and ISecurityIdMessage secIdMsg)
				{
					var drop = false;

					using (await _sync.LockAsync(cancellationToken))
					{
						foreach (var parentId in subscrMsg.GetSubscriptionIds())
						{
							if (_parents.TryGetValue(parentId, out var parent))
							{
								if (!ApplySubscriptionIds(subscrMsg, parent, secIdMsg.SecurityId))
									drop = true;

								break;
							}
						}
					}

					// no subscribers for this security — drop the message
					if (drop)
						return;
				}

				break;
			}
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	private static bool ApplySubscriptionIds(ISubscriptionIdMessage subscrMsg, ParentSubscription parent, SecurityId secId)
	{
		var ids = subscrMsg.GetSubscriptionIds();
		var initialId = parent.Origin.TransactionId;
		long[] newIds = parent.Alls.CachedKeys;

		if (parent.NonAlls.TryGetValue(secId, out var set))
			newIds = newIds.Concat(set.Cache);

		if (newIds.Length == 0)
		{
			// no ALL subscribers and no per-security subscribers for this secId
			var otherIds = ids.Where(id => id != initialId).ToArray();
			if (otherIds.Length == 0)
				return false;

			subscrMsg.SetSubscriptionIds(otherIds);
		}
		else if (ids.Length == 1 && ids[0] == initialId)
			subscrMsg.SetSubscriptionIds(newIds);
		else
			subscrMsg.SetSubscriptionIds([.. ids.Where(id => id != initialId), .. newIds]);

		return true;
	}

	/// <summary>
	/// Create a copy of <see cref="SubscriptionSecurityAllMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new SubscriptionSecurityAllMessageAdapter(InnerAdapter.TypedClone());
	}
}