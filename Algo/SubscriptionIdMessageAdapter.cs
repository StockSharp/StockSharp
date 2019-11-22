namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Message adapters initializes <see cref="ISubscriptionIdMessage.SubscriptionId"/> property.
	/// </summary>
	public class SubscriptionIdMessageAdapter : MessageAdapterWrapper
	{
		private class SubscriptionInfo
		{
			public readonly CachedSynchronizedSet<long> Subscribers = new CachedSynchronizedSet<long>();
		}

		private readonly SyncObject _sync = new SyncObject();

		private readonly HashSet<long> _historicalRequests = new HashSet<long>();
		private readonly PairSet<Tuple<DataType, SecurityId>, SubscriptionInfo> _subscriptionsByKey = new PairSet<Tuple<DataType, SecurityId>, SubscriptionInfo>();
		private readonly PairSet<SubscriptionInfo, long> _subscriptionsById = new PairSet<SubscriptionInfo, long>();

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionIdMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public SubscriptionIdMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

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
			}

			base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
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
				{
					var resMsg = (MarketDataFinishedMessage)message;

					lock (_sync)
						_historicalRequests.Remove(resMsg.OriginalTransactionId);
					
					break;
				}
				case MessageTypes.PortfolioLookupResult:
				{
					var resMsg = (PortfolioLookupResultMessage)message;

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
			ProcessInSubscriptionMessage(message, DataType.Transactions, default, (id, error) => null);
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
			ProcessInSubscriptionMessage(message, DataType.Portfolio(message.PortfolioName), default, null);
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

			if (isSubscribe)
			{
				if (message.To != null)
				{
					lock (_sync)
						_historicalRequests.Add(transId);

					return;
				}
			}

			Message sendInMsg = null;
			Message sendOutMsg = null;

			try
			{
				var key = Tuple.Create(dataType, securityId);

				lock (_sync)
				{
					var sendIn = false;

					var info = _subscriptionsByKey.TryGetValue(key);

					if (isSubscribe)
					{
						if (info == null)
						{
							sendIn = true;
							info = new SubscriptionInfo();
							_subscriptionsByKey.Add(key, info);
							_subscriptionsById.Add(info, message.TransactionId);
						}
						
						info.Subscribers.Add(transId);
					}
					else
					{
						if (info != null)
						{
							if (!info.Subscribers.Remove(message.OriginalTransactionId))
							{
								sendOutMsg = createSendOut?.Invoke(message.OriginalTransactionId, new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(message.OriginalTransactionId)));
							}
							else
							{
								sendIn = info.Subscribers.Count == 0;

								if (sendIn)
									_subscriptionsByKey.Remove(key);
							}
						}
						else
						{
							sendOutMsg = createSendOut?.Invoke(transId, new InvalidOperationException(LocalizedStrings.SubscriptionNonExist.Put(transId)));
						}
					}

					if (sendIn)
					{
						if (!isSubscribe)
						{
							message = (TMessage)message.Clone();
							message.OriginalTransactionId = _subscriptionsById.GetAndRemove(info);
						}

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
		/// Create a copy of <see cref="SubscriptionIdMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SubscriptionIdMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}