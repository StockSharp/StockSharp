namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Online subscription counter adapter.
	/// </summary>
	public class SubscriptionOnlineMessageAdapter : MessageAdapterWrapper
	{
		private class SubscriptionInfo
		{
			private readonly SubscriptionInfo _main;

			public ISubscriptionMessage Subscription { get; }

			public SubscriptionInfo(ISubscriptionMessage subscription)
			{
				Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			}

			public SubscriptionInfo(SubscriptionInfo main)
			{
				_main = main ?? throw new ArgumentNullException(nameof(main));

				Subscription = main.Subscription;
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

			public readonly HashSet<long> ExtraFilters = new HashSet<long>();
			public readonly CachedSynchronizedDictionary<long, ISubscriptionMessage> Subscribers = new CachedSynchronizedDictionary<long, ISubscriptionMessage>();

			private readonly List<long> _linked = new List<long>();

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

		private readonly SyncObject _sync = new SyncObject();

		private readonly PairSet<Tuple<DataType, SecurityId>, SubscriptionInfo> _subscriptionsByKey = new PairSet<Tuple<DataType, SecurityId>, SubscriptionInfo>();
		private readonly Dictionary<long, SubscriptionInfo> _subscriptionsById = new Dictionary<long, SubscriptionInfo>();
		private readonly HashSet<long> _strategyPosSubscriptions = new HashSet<long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionOnlineMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public SubscriptionOnlineMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			void TryAddOrderSubscription(OrderMessage orderMsg)
			{
				lock (_sync)
				{
					if (_subscriptionsByKey.TryGetValue(Tuple.Create(DataType.Transactions, default(SecurityId)), out var info))
						TryAddOrderTransaction(info, orderMsg.TransactionId);

					//if (_subscriptionsByKey.TryGetValue(Tuple.Create(DataType.Transactions, orderMsg.SecurityId), out info))
					//	TryAddOrderTransaction(info, orderMsg.TransactionId);
				}
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
					return ProcessReset(message);

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				case MessageTypes.OrderCancel:
				case MessageTypes.OrderGroupCancel:
				{
					var orderMsg = (OrderMessage)message;

					TryAddOrderSubscription(orderMsg);

					return base.OnSendInMessage(message);
				}

				case MessageTypes.OrderPairReplace:
				{
					var pairMsg = (OrderPairReplaceMessage)message;

					TryAddOrderSubscription(pairMsg.Message1);
					TryAddOrderSubscription(pairMsg.Message2);

					return base.OnSendInMessage(message);
				}

				case MessageTypes.OrderStatus:
				{
					var statusMsg = (OrderStatusMessage)message;

					if (statusMsg.HasOrderId())
						return base.OnSendInMessage(message);

					return ProcessInSubscriptionMessage(statusMsg);
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

		private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
		{
			info.State = info.State.ChangeSubscriptionState(state, info.Subscription.TransactionId, this);

			if (!state.IsActive())
			{
				_subscriptionsByKey.RemoveByValue(info);
				this.AddInfoLog(LocalizedStrings.SubscriptionRemoved, info.Subscription.TransactionId);
			}
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			Message extra = null;

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
					var id = responseMsg.OriginalTransactionId;

					HashSet<long> subscribers = null;

					lock (_sync)
					{
						if (responseMsg.IsOk())
						{
							if (_subscriptionsById.TryGetValue(id, out var info))
							{
								ChangeState(info, SubscriptionStates.Active);

								if (!this.IsOutMessageSupported(MessageTypes.SubscriptionOnline))
								{
									extra = new SubscriptionOnlineMessage
									{
										OriginalTransactionId = id
									};
								}
							}
						}
						else
						{
							if (_subscriptionsById.TryGetAndRemove(id, out var info))
							{
								ChangeState(info, SubscriptionStates.Error);
							}
						}
					}

					if (subscribers != null)
					{
						foreach (var subscriber in subscribers)
						{
							this.AddInfoLog(LocalizedStrings.SubscriptionNotifySubscriber, responseMsg.OriginalTransactionId, subscriber);
							base.OnInnerAdapterNewOutMessage(subscriber.CreateSubscriptionResponse(responseMsg.Error));
						}
					}

					break;
				}

				case MessageTypes.SubscriptionOnline:
				{
					lock (_sync)
					{
						if (_subscriptionsById.TryGetValue(((SubscriptionOnlineMessage)message).OriginalTransactionId, out var info))
							ChangeState(info, SubscriptionStates.Online);
					}

					break;
				}

				case MessageTypes.SubscriptionFinished:
				{
					lock (_sync)
					{
						if (_subscriptionsById.TryGetValue(((SubscriptionFinishedMessage)message).OriginalTransactionId, out var info))
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
							if (subscrMsg.OriginalTransactionId != 0 && _subscriptionsById.TryGetValue(subscrMsg.OriginalTransactionId, out var info))
							{
								if (message is ExecutionMessage execMsg &&
									execMsg.ExecutionType == ExecutionTypes.Transaction &&
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

								if (!_subscriptionsByKey.TryGetValue(Tuple.Create(dataType, secId), out info) && (secId == default || !_subscriptionsByKey.TryGetValue(Tuple.Create(dataType, default(SecurityId)), out info)))
									break;
							}

							var ids = info.Subscribers.CachedKeys;

							if (info.ExtraFilters.Count > 0)
							{
								var set = new HashSet<long>(ids);

								foreach (var filterId in info.ExtraFilters)
								{
									if (!subscrMsg.IsMatch(info.Subscribers[filterId]))
										set.Remove(filterId);
								}

								if (ids.Length != set.Count)
									ids = set.ToArray();
							}

							subscrMsg.SetSubscriptionIds(ids);
						}
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);

			if (extra != null)
				base.OnInnerAdapterNewOutMessage(extra);
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
				this.AddWarningLog("Order's transaction {0} was handled before.", transactionId);
		}

		private void ClearState()
		{
			lock (_sync)
			{
				_subscriptionsByKey.Clear();
				_subscriptionsById.Clear();
				_strategyPosSubscriptions.Clear();
			}
		}

		private bool ProcessReset(Message message)
		{
			ClearState();

			return base.OnSendInMessage(message);
		}

		private bool ProcessInSubscriptionMessage(ISubscriptionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var transId = message.TransactionId;

			var isSubscribe = message.IsSubscribe;

			ISubscriptionMessage sendInMsg = null;
			Message[] sendOutMsgs = null;

			lock (_sync)
			{
				if (isSubscribe)
				{
					if (message is PortfolioLookupMessage posMsg && !posMsg.StrategyId.IsEmpty())
					{
						_strategyPosSubscriptions.Add(posMsg.TransactionId);
						sendInMsg = message;
					}
					else if (!message.IsHistoryOnly())
					{
						var dataType = message.DataType;
						var secId = default(SecurityId);

						var extraFilter = false;

						if (message is ISecurityIdMessage secIdMsg)
						{
							secId = secIdMsg.SecurityId;

							if (secId == default && IsSecurityRequired(dataType))
								this.AddWarningLog("Subscription {0} required security id.", dataType);
							else if (secId != default && !IsSecurityRequired(dataType))
							{
								//this.AddWarningLog("Subscription {0} doesn't required security id.", dataType);
								extraFilter = true;
								secId = default;
							}
						}

						if (!extraFilter)
							extraFilter = message.FilterEnabled;

						var key = Tuple.Create(dataType, secId);

						if (!_subscriptionsByKey.TryGetValue(key, out var info))
						{
							this.AddInfoLog("Subscription {0} initial.", transId);

							sendInMsg = message;

							info = new SubscriptionInfo(message.TypedClone());
						
							_subscriptionsByKey.Add(key, info);
						}
						else
						{
							this.AddInfoLog("Subscription {0} joined to {1}.", transId, info.Subscription.TransactionId);

							var resultMsg = message.CreateResult();

							if (message.Type == MessageTypes.MarketData)
							{
								sendOutMsgs = new[]
								{
									message.CreateResponse(),
									resultMsg,
								};
							}
							else
							{
								sendOutMsgs = new[] { resultMsg };
							}
						}

						_subscriptionsById.Add(transId, info);

						info.Subscribers.Add(transId, message.TypedClone());

						if (extraFilter)
							info.ExtraFilters.Add(transId);
					}
					else
						sendInMsg = message;
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
							sendOutMsgs = new[]
							{
								(Message)originId.CreateSubscriptionResponse(new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId)))
							};
						}
						else
						{
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
									// copy full subscription's details into unsubscribe request
									sendInMsg = MakeUnsubscribe(info.Subscription.TypedClone(), info.Subscription.TransactionId);
								}
								else
									this.AddWarningLog(LocalizedStrings.SubscriptionInState, originId, info.State);
							}
							else
							{
								sendOutMsgs = new[] { message.CreateResult() };
							}
						}
					}
					else if (_strategyPosSubscriptions.Remove(originId))
					{
						sendInMsg = message;
					}
					else
					{
						sendOutMsgs = new[]
						{
							(Message)originId.CreateSubscriptionResponse(new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId)))
						};
					}
				}

				if (sendOutMsgs != null)
				{
					foreach (var sendOutMsg in sendOutMsgs)
					{
						this.AddInfoLog("Out: {0}", sendOutMsg);
						RaiseNewOutMessage(sendOutMsg);
					}
				}
			}

			var retVal = true;

			if (sendInMsg != null)
			{
				this.AddInfoLog("In: {0}", sendInMsg);
				retVal = base.OnSendInMessage((Message)sendInMsg);
			}

			return retVal;
		}

		/// <summary>
		/// Create a copy of <see cref="SubscriptionOnlineMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SubscriptionOnlineMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}