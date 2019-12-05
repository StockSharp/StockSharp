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

			public readonly CachedSynchronizedSet<long> Subscribers = new CachedSynchronizedSet<long>();

			public override string ToString() => Subscription.ToString();
		}

		private readonly SyncObject _sync = new SyncObject();

		private readonly HashSet<long> _historicalRequests = new HashSet<long>();
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

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			long TryReplaceOriginId(long id)
			{
				lock (_sync)
					return _replaceId.TryGetValue(id, out var prevId) ? prevId : id;
			}

			var newOriginId = 0L;

			if (message is IOriginalTransactionIdMessage originIdMsg1)
			{
				newOriginId = originIdMsg1.OriginalTransactionId;
				originIdMsg1.OriginalTransactionId = TryReplaceOriginId(newOriginId);
			}

			switch (message.Type)
			{
				case MessageTypes.MarketData:
				{
					var responseMsg = (MarketDataMessage)message;

					var originId = responseMsg.OriginalTransactionId;

					lock (_sync)
					{
						if (responseMsg.IsOk())
						{
							// no need send response after re-subscribe cause response was handled prev time
							if (_replaceId.ContainsKey(newOriginId))
								return;
						}
						else
						{
							if (!_historicalRequests.Remove(originId))
							{
								if (_subscriptionsById.TryGetValue(originId, out var info))
								{
									_replaceId.Remove(newOriginId);
									_subscriptionsById.Remove(originId);
									_subscriptionsByKey.RemoveByValue(info);
								}
							}
						}
					}
					
					break;
				}

				case MessageTypes.MarketDataFinished:
				case MessageTypes.SecurityLookupResult:
				case MessageTypes.PortfolioLookupResult:
				case MessageTypes.OrderStatus:
				{
					var resultMsg = (IOriginalTransactionIdMessage)message;

					lock (_sync)
					{
						_replaceId.Remove(newOriginId);
						_historicalRequests.Remove(resultMsg.OriginalTransactionId);
					}
					
					break;
				}

				default:
				{
					if (message is ISubscriptionIdMessage subscrMsg)
					{
						lock (_sync)
						{
							if (subscrMsg.OriginalTransactionId != 0 && _historicalRequests.Contains(subscrMsg.OriginalTransactionId))
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

							var msg = (Message)subscription;
							msg.Adapter = this;
							msg.IsBack = true;
							return msg;
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

			try
			{
				lock (_sync)
				{
					if (isSubscribe)
					{
						if (_replaceId.ContainsKey(transId))
						{
							sendInMsg = message;
							return;
						}

						if (message.To != null)
						{
							_historicalRequests.Add(transId);
							sendInMsg = message;
							return;
						}
					}

					if (isSubscribe)
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
					else
					{
						var originId = message.OriginalTransactionId;

						if (_subscriptionsById.TryGetValue(originId, out var info))
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
									message = (TMessage)info.Subscription.Clone();

									message.IsSubscribe = false;
									message.TransactionId = transId;
									message.OriginalTransactionId = originId;

									sendInMsg = message;
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
			}
			finally
			{
				if (sendInMsg != null)
				{
					this.AddInfoLog("In: {0}", sendInMsg);
					base.OnSendInMessage(sendInMsg);
				}
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