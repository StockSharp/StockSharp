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
		private readonly PairSet<SubscriptionInfo, long> _subscriptionsById = new PairSet<SubscriptionInfo, long>();
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
			void TryReplaceOriginId(IOriginalTransactionIdMessage originIdMsg)
			{
				lock (_sync)
				{
					if (_replaceId.TryGetValue(originIdMsg.OriginalTransactionId, out var prevId))
						originIdMsg.OriginalTransactionId = prevId;
				}
			}

			if (message is IOriginalTransactionIdMessage originIdMsg1)
				TryReplaceOriginId(originIdMsg1);

			switch (message.Type)
			{
				case MessageTypes.MarketData:
				{
					var resMsg = (MarketDataMessage)message;

					if (!resMsg.IsOk())
					{
						lock (_sync)
						{
							if (!_historicalRequests.Remove(resMsg.OriginalTransactionId))
							{
								if (_subscriptionsById.TryGetKey(resMsg.OriginalTransactionId, out var info))
								{
									_subscriptionsById.Remove(info);
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
					var resMsg = (IOriginalTransactionIdMessage)message;

					lock (_sync)
						_historicalRequests.Remove(resMsg.OriginalTransactionId);
					
					break;
				}

				default:
				{
					if (message is ISubscriptionIdMessage subscrMsg)
					{
						lock (_sync)
						{
							if (subscrMsg.OriginalTransactionId != 0 && _historicalRequests.Contains(subscrMsg.OriginalTransactionId))
								subscrMsg.SubscriptionId = subscrMsg.OriginalTransactionId;
							else
							{
								if (subscrMsg.OriginalTransactionId != 0 && _subscriptionsById.TryGetKey(subscrMsg.OriginalTransactionId, out var info))
								{
								}
								else
								{
									var dataType = message.Type.ToDataType((message as CandleMessage)?.Arg ?? (message as ExecutionMessage)?.ExecutionType);
									var secId = GetSecurityId(dataType, (subscrMsg as ISecurityIdMessage)?.SecurityId ?? default);

									if (!_subscriptionsByKey.TryGetValue(Tuple.Create(dataType, secId), out info))
										break;
								}
								
								subscrMsg.SubscriptionIds = info.Subscribers.Cache;
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

						subscriptions = _subscriptionsById.Keys.Select(i =>
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
			var isSubscribe = message.IsSubscribe;
			var transId = message.TransactionId;

			Message sendInMsg = null;
			Message sendOutMsg = null;

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

					var sendIn = false;
					var key = Tuple.Create(dataType, securityId);

					if (isSubscribe)
					{
						if (!_subscriptionsByKey.TryGetValue(key, out var info))
						{
							sendIn = true;

							info = new SubscriptionInfo((ISubscriptionMessage)message.Clone());
							
							_subscriptionsByKey.Add(key, info);
							_subscriptionsById.Add(info, message.TransactionId);
						}
						
						info.Subscribers.Add(transId);
					}
					else
					{
						if (_subscriptionsById.TryGetKey(message.OriginalTransactionId, out var info))
						{
							if (!info.Subscribers.Remove(message.OriginalTransactionId))
							{
								sendOutMsg = createSendOut?.Invoke(message.OriginalTransactionId, new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(message.OriginalTransactionId)));
							}
							else
							{
								sendIn = info.Subscribers.Count == 0;

								if (sendIn)
								{
									_subscriptionsByKey.Remove(key);
									_subscriptionsById.Remove(info);

									var originId = message.OriginalTransactionId;

									// copy full subscription's details into unsubscribe request
									message = (TMessage)info.Subscription.Clone();

									message.IsSubscribe = false;
									message.TransactionId = transId;
									message.OriginalTransactionId = originId;
								}
							}
						}
						else
						{
							sendOutMsg = createSendOut?.Invoke(transId, new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(transId)));
						}
					}

					if (sendIn)
					{
						sendInMsg = message;
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