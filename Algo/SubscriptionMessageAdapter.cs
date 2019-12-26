namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Subscription counter adapter.
	/// </summary>
	public class SubscriptionMessageAdapter : MessageAdapterWrapper
	{
		private class SubscriptionInfo
		{
			public ISubscriptionMessage Subscription { get; }

			public SubscriptionInfo(ISubscriptionMessage subscription)
			{
				Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			}

			public SubscriptionStates State { get; set; } = SubscriptionStates.Stopped;

			public readonly CachedSynchronizedSet<long> Subscribers = new CachedSynchronizedSet<long>();

			public override string ToString() => Subscription.ToString();
		}

		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<long, ISubscriptionMessage> _historicalRequests = new Dictionary<long, ISubscriptionMessage>();
		private readonly PairSet<Tuple<DataType, SecurityId>, SubscriptionInfo> _subscriptionsByKey = new PairSet<Tuple<DataType, SecurityId>, SubscriptionInfo>();
		private readonly Dictionary<long, SubscriptionInfo> _subscriptionsById = new Dictionary<long, SubscriptionInfo>();
		private readonly Dictionary<long, long> _replaceId = new Dictionary<long, long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public SubscriptionMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <summary>
		/// Restore subscription on reconnect.
		/// </summary>
		/// <remarks>
		/// Error case like connection lost etc.
		/// </remarks>
		public bool IsRestoreSubscriptionOnErrorReconnect { get; set; }

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					ProcessReset(message);
					break;

				case MessageTypes.MarketData:
					ProcessInMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.Portfolio:
					ProcessInPortfolioMessage((PortfolioMessage)message);
					break;

				case MessageTypes.SecurityLookup:
					ProcessSecurityLookupMessage((SecurityLookupMessage)message);
					break;

				case MessageTypes.BoardLookup:
					ProcessBoardLookupMessage((BoardLookupMessage)message);
					break;

				case MessageTypes.TimeFrameLookup:
					ProcessTimeFrameLookupMessage((TimeFrameLookupMessage)message);
					break;

				case MessageTypes.UserLookup:
					ProcessUserLookupMessage((UserLookupMessage)message);
					break;

				case MessageTypes.PortfolioLookup:
					ProcessPortfolioLookupMessage((PortfolioLookupMessage)message);
					break;

				case MessageTypes.OrderStatus:
					ProcessOrderStatusMessage((OrderStatusMessage)message);
					break;

				default:
					base.OnSendInMessage(message);
					break;
			}
		}

		private void ProcessReset(Message message)
		{
			lock (_sync)
			{
				_historicalRequests.Clear();
				_subscriptionsByKey.Clear();
				_subscriptionsById.Clear();
				_replaceId.Clear();
			}

			base.OnSendInMessage(message);
		}

		private void ChangeState(SubscriptionInfo info, SubscriptionStates state)
		{
			const string text = "Subscription {0} {1}->{2}.";

			if (info.State.IsOk(state))
				this.AddInfoLog(text, info.Subscription.TransactionId, info.State, state);
			else
				this.AddWarningLog(text, info.Subscription.TransactionId, info.State, state);

			info.State = state;
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

			bool UpdateSubscriptionResult(bool isOk, Func<long, Message> createReply)
			{
				HashSet<long> subscribers = null;

				lock (_sync)
				{
					if (isOk)
					{
						if (_subscriptionsById.TryGetValue(prevOriginId, out var info))
						{
							// no need send response after re-subscribe cause response was handled prev time
							if (_replaceId.ContainsKey(newOriginId))
							{
								if (info.State != SubscriptionStates.Stopped)
									return false;
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
								_subscriptionsByKey.RemoveByValue(info);

								var set = new HashSet<long>(info.Subscribers.Cache);
								set.Remove(prevOriginId);
								subscribers = set;
							}
						}
					}
				}

				if (subscribers != null)
				{
					foreach (var subscriber in subscribers)
						base.OnInnerAdapterNewOutMessage(createReply(subscriber));
				}

				return true;
			}

			switch (message.Type)
			{
				case MessageTypes.MarketData:
				{
					var responseMsg = (MarketDataMessage)message;

					if (!UpdateSubscriptionResult(responseMsg.IsOk(), subscriber => new MarketDataMessage
					{
						OriginalTransactionId = subscriber,
						Error = responseMsg.Error,
						IsNotSupported = responseMsg.IsNotSupported,
					}))
						return;

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
				case MessageTypes.SecurityLookupResult:
				case MessageTypes.PortfolioLookupResult:
				case MessageTypes.OrderStatus:
				{
					lock (_sync)
					{
						if (_replaceId.ContainsKey(newOriginId))
							return;

						_historicalRequests.Remove(prevOriginId);
					}
					
					break;
				}

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;

					// reply on RegisterPortfolio subscription do not contains any portfolio info
					if (pfMsg.PortfolioName.IsEmpty())
					{
						if (!UpdateSubscriptionResult(pfMsg.Error == null, subscriber => new PortfolioMessage
						{
							OriginalTransactionId = subscriber,
							Error = pfMsg.Error,
						}))
							return;
					}

					break;
				}

				default:
				{
					if (message is ISubscriptionIdMessage subscrMsg)
					{
						lock (_sync)
						{
							if (subscrMsg.OriginalTransactionId != 0 && _historicalRequests.ContainsKey(subscrMsg.OriginalTransactionId))
								subscrMsg.SetSubscriptionIds(subscriptionId: subscrMsg.OriginalTransactionId);
							else
							{
								if (subscrMsg.OriginalTransactionId != 0 && _subscriptionsById.TryGetValue(subscrMsg.OriginalTransactionId, out var info))
								{
								}
								else
								{
									var dataType = message.Type.ToDataType((message as CandleMessage)?.Arg ?? (message as ExecutionMessage)?.ExecutionType);
									var secId = GetSecurityId(dataType, (subscrMsg as ISecurityIdMessage)?.SecurityId ?? default);

									if (!_subscriptionsByKey.TryGetValue(Tuple.Create(dataType, secId), out info))
										break;
								}
								
								subscrMsg.SetSubscriptionIds(info.Subscribers.Cache);
							}
						}
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);

			switch (message.Type)
			{
				case ExtendedMessageTypes.ReconnectingFinished:
				{
					Message[] subscriptions;

					lock (_sync)
					{
						_replaceId.Clear();

						subscriptions = _subscriptionsById.Values.Distinct().Select(i =>
						{
							var subscription = (ISubscriptionMessage)i.Subscription.Clone();
							subscription.TransactionId = TransactionIdGenerator.GetNextId();

							_replaceId.Add(subscription.TransactionId, i.Subscription.TransactionId);

							this.AddInfoLog("Re-map subscription: {0}->{1} for '{2}'.", i.Subscription.TransactionId, subscription.TransactionId, i.Subscription);

							return ((Message)subscription).LoopBack(this);
						}).ToArray();
					}

					foreach (var subscription in subscriptions)
						base.OnInnerAdapterNewOutMessage(subscription);

					break;
				}
			}
		}

		private void ProcessUserLookupMessage(UserLookupMessage message)
		{
			ProcessInSubscriptionMessage(message, DataType.Users, default, (id, error) => new UserLookupResultMessage
			{
				OriginalTransactionId = id,
				Error = error,
			});
		}

		private void ProcessTimeFrameLookupMessage(TimeFrameLookupMessage message)
		{
			ProcessInSubscriptionMessage(message, DataType.TimeFrames, default, (id, error) => new TimeFrameLookupResultMessage
			{
				OriginalTransactionId = id,
				Error = error,
			});
		}

		private void ProcessBoardLookupMessage(BoardLookupMessage message)
		{
			ProcessInSubscriptionMessage(message, DataType.Board, default, (id, error) => new BoardLookupResultMessage
			{
				OriginalTransactionId = id,
				Error = error,
			});
		}

		private void ProcessSecurityLookupMessage(SecurityLookupMessage message)
		{
			ProcessInSubscriptionMessage(message, DataType.Securities, default, (id, error) => new SecurityLookupResultMessage
			{
				OriginalTransactionId = id,
				Error = error,
			});
		}

		private void ProcessOrderStatusMessage(OrderStatusMessage message)
		{
			if (message.HasOrderId())
			{
				base.OnSendInMessage(message);
				return;
			}

			ProcessInSubscriptionMessage(message, DataType.Transactions, default, (id, error) => new OrderStatusMessage
			{
				OriginalTransactionId = id,
				Error = error,
			});
		}

		private void ProcessPortfolioLookupMessage(PortfolioLookupMessage message)
		{
			ProcessInSubscriptionMessage(message, DataType.PositionChanges, default, (id, error) => new PortfolioLookupResultMessage
			{
				OriginalTransactionId = id,
				Error = error,
			});
		}

		private void ProcessInPortfolioMessage(PortfolioMessage message)
		{
			ProcessInSubscriptionMessage(message, DataType.Portfolio(message.PortfolioName), default, (id, error) => new PortfolioMessage
			{
				OriginalTransactionId = id,
				Error = error,
			});
		}

		private void ProcessInMarketDataMessage(MarketDataMessage message)
		{
			var dataType = message.ToDataType();
			var secId = GetSecurityId(dataType, message.SecurityId);

			ProcessInSubscriptionMessage(message, dataType, secId, (id, error) => new MarketDataMessage
			{
				OriginalTransactionId = id,
				Error = error,
			});
		}

		private SecurityId GetSecurityId(DataType dataType, SecurityId securityId) => IsSecurityRequired(dataType) ? securityId : default;

		private void ProcessInSubscriptionMessage<TMessage>(TMessage message,
			DataType dataType, SecurityId securityId,
			Func<long, Exception, Message> createSendOut)
			where TMessage : Message, ISubscriptionMessage
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			if (createSendOut == null)
				throw new ArgumentNullException(nameof(createSendOut));

			var isSubscribe = message.IsSubscribe;
			var transId = message.TransactionId;

			Message sendInMsg = null;
			Message sendOutMsg = null;
			Message onlineMsg = null;

			lock (_sync)
			{
				if (isSubscribe)
				{
					if (_replaceId.ContainsKey(transId))
					{
						sendInMsg = message;
					}
					else if (message.To != null)
					{
						_historicalRequests.Add(transId, (ISubscriptionMessage)message.Clone());
						sendInMsg = message;
					}
					else
					{
						var key = Tuple.Create(dataType, securityId);

						if (!_subscriptionsByKey.TryGetValue(key, out var info))
						{
							sendInMsg = message;

							info = new SubscriptionInfo((ISubscriptionMessage)message.Clone());
						
							_subscriptionsByKey.Add(key, info);
						}
						else
						{
							sendOutMsg = createSendOut(transId, null);
							onlineMsg = new SubscriptionOnlineMessage { OriginalTransactionId = transId };
						}

						_subscriptionsById.Add(transId, info);
						info.Subscribers.Add(transId);
					}
				}
				else
				{
					var originId = message.OriginalTransactionId;

					TMessage MakeUnsubscribe(TMessage m)
					{
						m.IsSubscribe = false;
						m.TransactionId = transId;
						m.OriginalTransactionId = originId;

						return m;
					}

					if (_historicalRequests.TryGetValue(originId, out var subscription))
					{
						_historicalRequests.Remove(originId);

						sendInMsg = MakeUnsubscribe((TMessage)subscription);
					}
					else if (_subscriptionsById.TryGetValue(originId, out var info))
					{
						if (!info.Subscribers.Remove(originId))
						{
							sendOutMsg = createSendOut(originId, new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId)));
						}
						else
						{
							if (info.Subscribers.Count == 0)
							{
								_subscriptionsByKey.RemoveByValue(info);
								_subscriptionsById.Remove(originId);

								// copy full subscription's details into unsubscribe request
								sendInMsg = MakeUnsubscribe((TMessage)info.Subscription.Clone());
							}
							else
								sendOutMsg = createSendOut(transId, null);
						}
					}
					else
					{
						sendOutMsg = createSendOut(transId, new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(originId)));
					}
				}
			}

			if (sendInMsg != null)
			{
				this.AddInfoLog("In: {0}", sendInMsg);
				base.OnSendInMessage(sendInMsg);
			}

			if (sendOutMsg != null)
			{
				this.AddInfoLog("Out: {0}", sendOutMsg);
				RaiseNewOutMessage(sendOutMsg);
			}

			if (onlineMsg != null)
			{
				this.AddInfoLog("Out: {0}", onlineMsg);
				RaiseNewOutMessage(onlineMsg);
			}
		}

		/// <summary>
		/// Create a copy of <see cref="SubscriptionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SubscriptionMessageAdapter((IMessageAdapter)InnerAdapter.Clone())
			{
				IsRestoreSubscriptionOnErrorReconnect = IsRestoreSubscriptionOnErrorReconnect,
			};
		}
	}
}