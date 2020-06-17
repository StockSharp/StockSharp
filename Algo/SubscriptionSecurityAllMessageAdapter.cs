namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Security ALL subscription counter adapter.
	/// </summary>
	public class SubscriptionSecurityAllMessageAdapter : MessageAdapterWrapper
	{
		private abstract class BaseSubscription
		{
			protected BaseSubscription(MarketDataMessage origin)
			{
				Origin = origin ?? throw new ArgumentNullException(nameof(origin));
			}

			public MarketDataMessage Origin { get; }
		}

		private class ChildSubscription : BaseSubscription
		{
			public ChildSubscription(MarketDataMessage origin)
				: base(origin)
			{
			}

			public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;
			public IList<Message> Suspended { get; } = new List<Message>();
			public CachedSynchronizedDictionary<long, MarketDataMessage> Subscribers { get; } = new CachedSynchronizedDictionary<long, MarketDataMessage>();
		}

		private class ParentSubscription : BaseSubscription
		{
			public ParentSubscription(MarketDataMessage origin)
				: base(origin)
			{
			}

			public IDictionary<SecurityId, ChildSubscription> Child { get; } = new Dictionary<SecurityId, ChildSubscription>();
		}

		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<long, RefPair<long, SubscriptionStates>> _allChilds = new Dictionary<long, RefPair<long, SubscriptionStates>>();
		private readonly Dictionary<long, ParentSubscription> _parents = new Dictionary<long, ParentSubscription>();
		private readonly List<Message> _toFlush = new List<Message>();

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
				_parents.Clear();
				_allChilds.Clear();
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
							if (_allChilds.TryGetValue(transId, out var tuple))
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

								var child = _parents[tuple.First].Child[mdMsg.SecurityId];
								child.State = SubscriptionStates.Active;
								_toFlush.AddRange(child.Suspended.CopyAndClear());

								this.AddDebugLog("New ALL map (active): {0}/{1} TrId={2}", child.Origin.SecurityId, child.Origin.DataType2, mdMsg.TransactionId);
								
								RaiseNewOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = transId });
								return true;
							}
							else
							{
								if (!IsSecurityRequired(mdMsg.DataType2) || mdMsg.SecurityId == default)
								{
									var existing = _parents.FirstOrDefault(p => p.Value.Origin.DataType2 == mdMsg.DataType2).Value;

									if (existing == null)
									{
										var parent = new ParentSubscription(mdMsg.TypedClone());
										_parents.Add(transId, parent);

										// first child is parent
										_allChilds.Add(transId, RefTuple.Create(transId, SubscriptionStates.Stopped));

										// do not specify security cause adapter doesn't require it
										mdMsg.SecurityId = default;
										Extensions.AllSecurity.CopyEx(mdMsg, false);
									}
									else
									{
										var childs = existing.Child;

										if (mdMsg.SecurityId != default)
										{
											var child = childs.SafeAdd(mdMsg.SecurityId, key => new ChildSubscription(mdMsg.TypedClone()));
											child.Subscribers.Add(transId, mdMsg.TypedClone());
										}
										else
										{
											foreach (var pair in childs)
												pair.Value.Subscribers.Add(transId, mdMsg.TypedClone());
										}

										RaiseNewOutMessage(new SubscriptionResponseMessage { OriginalTransactionId = transId });
										return true;
									}
								}
							}
						}
					}
					else
					{
						var childIds = ArrayHelper.Empty<long>();

						lock (_sync)
						{
							if (_allChilds.TryGetAndRemove(mdMsg.OriginalTransactionId, out var tuple))
							{
								this.AddDebugLog("Sec ALL child {0} unsubscribe.", mdMsg.OriginalTransactionId);

								Exception error = null;

								if (!tuple.Second.IsActive())
									error = new InvalidOperationException(LocalizedStrings.SubscriptionInvalidState.Put(mdMsg.OriginalTransactionId, tuple.Second));
								else
								{
									var childs = _parents[tuple.First].Child;

									var pair = childs.FirstOrDefault(p => p.Value.Origin.TransactionId == mdMsg.OriginalTransactionId);
									var childSubscription = pair.Value;

									if (childSubscription == null)
										error = new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(mdMsg.OriginalTransactionId));
									else
									{
										if (childSubscription.Subscribers.Remove(mdMsg.OriginalTransactionId))
										{
											if (childSubscription.Subscribers.Count == 0)
												childs.Remove(pair.Key);
										}
										else
											error = new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(mdMsg.OriginalTransactionId));
									}
								}

								RaiseNewOutMessage(new SubscriptionResponseMessage
								{
									OriginalTransactionId = mdMsg.TransactionId,
									Error = error,
								});

								return true;
							}

							if (_parents.TryGetAndRemove(mdMsg.OriginalTransactionId, out var tuple2))
								childIds = tuple2.Child.Values.Select(s => s.Origin.TransactionId).ToArray();
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
							if (_parents.TryGetAndRemove(responseMsg.OriginalTransactionId, out var parent))
							{
								extra = new List<Message>();

								foreach (var child in parent.Child.Values)
								{
									var childId = child.Origin.TransactionId;

									if (_allChilds.TryGetValue(childId, out var tuple) && tuple.Second == SubscriptionStates.Stopped)
									{
										// loopback subscription not yet come, so will reply later
										tuple.Second = SubscriptionStates.Error;
									}
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
						if (_parents.TryGetAndRemove(finishMsg.OriginalTransactionId, out var parent))
						{
							extra = new List<Message>();

							foreach (var child in parent.Child.Values)
							{
								var childId = child.Origin.TransactionId;

								if (_allChilds.TryGetValue(childId, out var tuple) && tuple.Second == SubscriptionStates.Stopped)
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

		private SubscriptionSecurityAllMessage CheckSubscription(ref Message message)
		{
			lock (_sync)
			{
				if (_toFlush.Count > 0)
				{
					var toFlush = _toFlush.CopyAndClear();
					
					this.AddDebugLog("Flush {0} suspended.", toFlush.Length);

					foreach (var msg in toFlush)
						RaiseNewOutMessage(msg);
				}

				if (_parents.Count == 0)
					return null;

				if (message is ISubscriptionIdMessage subscrMsg && message is ISecurityIdMessage secIdMsg)
				{
					foreach (var parentId in subscrMsg.GetSubscriptionIds())
					{
						if (_parents.TryGetValue(parentId, out var parent))
						{
							// parent subscription has security id (not null)
							if (parent.Origin.SecurityId == secIdMsg.SecurityId)
								return null;

							SubscriptionSecurityAllMessage allMsg = null;

							if (!parent.Child.TryGetValue(secIdMsg.SecurityId, out var child))
							{
								allMsg = new SubscriptionSecurityAllMessage();

								parent.Origin.CopyTo(allMsg);

								allMsg.ParentTransactionId = parentId;
								allMsg.TransactionId = TransactionIdGenerator.GetNextId();
								allMsg.SecurityId = secIdMsg.SecurityId;

								child = new ChildSubscription(allMsg.TypedClone());
								child.Subscribers.Add(allMsg.TransactionId, child.Origin);

								parent.Child.Add(secIdMsg.SecurityId, child);

								allMsg.LoopBack(this, MessageBackModes.Chain);
								_allChilds.Add(allMsg.TransactionId, RefTuple.Create(parentId, SubscriptionStates.Stopped));

								this.AddDebugLog("New ALL map: {0}/{1} TrId={2}-{3}", child.Origin.SecurityId, child.Origin.DataType2, allMsg.ParentTransactionId, allMsg.TransactionId);
							}

							//var subscriptionIds = subscrMsg.GetSubscriptionIds().Where(i => i != parentId).Concat(child.Subscribers.Cache);
							subscrMsg.SetSubscriptionIds(child.Subscribers.CachedKeys);

							if (!child.State.IsActive())
							{
								child.Suspended.Add(message);
								message = null;

								this.AddDebugLog("ALL suspended: {0}/{1}, cnt={2}", child.Origin.SecurityId, child.Origin.DataType2, child.Suspended.Count);
							}

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
}