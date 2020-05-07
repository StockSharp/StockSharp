namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Security ALL subscription counter adapter.
	/// </summary>
	public class SubscriptionSecurityAllMessageAdapter : MessageAdapterWrapper
	{
		private class ChildSubscription
		{
			public ChildSubscription(MarketDataMessage origin)
			{
				Origin = origin ?? throw new ArgumentNullException(nameof(origin));
			}

			public MarketDataMessage Origin { get; }
			public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;
			public IList<Message> Suspended { get; } = new List<Message>();
		}

		private class ParentSubscription
		{
			public ParentSubscription(MarketDataMessage origin)
			{
				Origin = origin ?? throw new ArgumentNullException(nameof(origin));
			}

			public MarketDataMessage Origin { get; }
			public IDictionary<SecurityId, ChildSubscription> Child { get; } = new Dictionary<SecurityId, ChildSubscription>();
		}

		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<long, long> _pendingBacks = new Dictionary<long, long>();
		private readonly Dictionary<long, ParentSubscription> _map = new Dictionary<long, ParentSubscription>();
		
		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionSecurityAllMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public SubscriptionSecurityAllMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private void ClearState()
		{
			lock (_sync)
			{
				_map.Clear();
				_pendingBacks.Clear();
			}
		}

		private const long _error = -1;
		private const long _finished = -2;

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
						var secId = IsSecurityRequired(mdMsg.ToDataType()) ? mdMsg.SecurityId : default;

						var transId = mdMsg.TransactionId;
						
						IEnumerable<Message> suspended = null;

						lock (_sync)
						{
							if (_pendingBacks.TryGetAndRemove(transId, out var parentId))
							{
								// parent subscription was deleted early
								if (parentId == _error)
								{
									RaiseNewOutMessage(new SubscriptionResponseMessage
									{
										OriginalTransactionId = transId,
										Error = new InvalidOperationException(),
									});

									return true;
								}
								else if (parentId == _finished)
								{
									RaiseNewOutMessage(new SubscriptionFinishedMessage
									{
										OriginalTransactionId = transId,
									});

									return true;
								}

								var child = _map[parentId].Child[mdMsg.SecurityId];
								child.State = SubscriptionStates.Active;
								suspended = child.Suspended.CopyAndClear();

								this.AddDebugLog("New ALL map (active): {0}/{1} TrId={2}", child.Origin.SecurityId, child.Origin.DataType2, mdMsg.TransactionId);
							}
							else if (secId == default)
								_map.Add(transId, new ParentSubscription(mdMsg.TypedClone()));
						}

						if (suspended != null)
						{
							foreach (var msg in suspended)
								RaiseNewOutMessage(msg);

							return true;
						}
					}
					else
					{
						long[] childIds;

						lock (_sync)
						{
							var tuple = _map.TryGetAndRemove(mdMsg.OriginalTransactionId);

							if (tuple == null)
								break;

							childIds = tuple.Child.Values.Select(s => s.Origin.TransactionId).ToArray();
						}

						foreach (var id in childIds)
						{
							RaiseNewOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = id });
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
				case ExtendedMessageTypes.ReconnectingFinished:
				{
					ClearState();
					break;
				}
				case MessageTypes.SubscriptionResponse:
				{
					var responseMsg = (SubscriptionResponseMessage)message;

					if (responseMsg.Error != null)
					{
						lock (_sync)
						{
							if (_map.TryGetAndRemove(responseMsg.OriginalTransactionId, out var parent))
							{
								extra = new List<Message>();

								foreach (var child in parent.Child.Values)
								{
									var childId = child.Origin.TransactionId;

									if (_pendingBacks.ContainsKey(childId))
										_pendingBacks[childId] = _error;
									else
										extra.Add(new SubscriptionResponseMessage { OriginalTransactionId = childId, Error = responseMsg.Error });
								}
							}
						}
					}

					break;
				}
				case MessageTypes.SubscriptionFinished:
				{
					var finishMsg = (SubscriptionFinishedMessage)message;

					lock (_sync)
					{
						if (_map.TryGetAndRemove(finishMsg.OriginalTransactionId, out var parent))
						{
							extra = new List<Message>();

							foreach (var child in parent.Child.Values)
							{
								var childId = child.Origin.TransactionId;

								if (_pendingBacks.ContainsKey(childId))
									_pendingBacks[childId] = _finished;
								else
									extra.Add(new SubscriptionFinishedMessage { OriginalTransactionId = childId });
							}
						}
					}

					break;
				}
				default:
				{
					if (message is ISubscriptionIdMessage subscrMsg && message is ISecurityIdMessage secIdMsg)
					{
						SubscriptionSecurityAllMessage allMsg = null;

						bool CheckSubscription(long parentId)
						{
							lock (_sync)
							{
								if (_map.TryGetValue(parentId, out var parent))
								{
									if (!parent.Child.TryGetValue(secIdMsg.SecurityId, out var child))
									{
										allMsg = new SubscriptionSecurityAllMessage();

										parent.Origin.CopyTo(allMsg);

										allMsg.ParentTransactionId = parentId;
										allMsg.TransactionId = TransactionIdGenerator.GetNextId();
										allMsg.SecurityId = secIdMsg.SecurityId;

										child = new ChildSubscription(allMsg.TypedClone());
										parent.Child.Add(secIdMsg.SecurityId, child);

										allMsg.LoopBack(this, MessageBackModes.Chain);
										_pendingBacks.Add(allMsg.TransactionId, parentId);

										this.AddDebugLog("New ALL map: {0}/{1} TrId={2}-{3}", child.Origin.SecurityId, child.Origin.DataType2, allMsg.ParentTransactionId, allMsg.TransactionId);
									}

									var subscriptionIds = subscrMsg.GetSubscriptionIds().Where(i => i != parentId).Append(child.Origin.TransactionId);
									subscrMsg.SetSubscriptionIds(subscriptionIds.ToArray());

									if (!child.State.IsActive())
									{
										child.Suspended.Add(message);
										message = null;

										this.AddDebugLog("ALL suspended: {0}/{1}, cnt={2}", child.Origin.SecurityId, child.Origin.DataType2, child.Suspended.Count);
									}

									return true;
								}
							}

							return false;
						}

						if (!CheckSubscription(subscrMsg.OriginalTransactionId))
						{
							foreach (var id in subscrMsg.GetSubscriptionIds())
							{
								if (CheckSubscription(id))
									break;
							}
						}

						if (allMsg != null)
							base.OnInnerAdapterNewOutMessage(allMsg);
					}

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

		/// <summary>
		/// Create a copy of <see cref="SubscriptionSecurityAllMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SubscriptionSecurityAllMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}