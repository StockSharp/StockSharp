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
		private readonly PairSet<Tuple<DataType, SecurityId>, SubscriptionInfo> _subscriptionsIds = new PairSet<Tuple<DataType, SecurityId>, SubscriptionInfo>();
		private readonly PairSet<SubscriptionInfo, long> _requests = new PairSet<SubscriptionInfo, long>();

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
				_subscriptionsIds.Clear();
				_requests.Clear();
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
								if (_requests.TryGetKey(resMsg.OriginalTransactionId, out var info))
								{
									_requests.Remove(info);
									_subscriptionsIds.RemoveByValue(info);
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
								var tuple = Tuple.Create(message.Type.ToDataType((message as ExecutionMessage)?.ExecutionType), GetSecurityId((subscrMsg as ISecurityIdMessage)?.SecurityId ?? default));

								if (_subscriptionsIds.TryGetValue(tuple, out var info))
									subscrMsg.SubscriptionIds = info.Subscribers.Cache;
							}
						}
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void ProcessInMarketDataMessage(MarketDataMessage message)
		{
			var secId = GetSecurityId(message.SecurityId);

			ProcessInSubscriptionMessage(message, message.DataType.ToDataType(), secId, (id, error) => new MarketDataMessage
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

		private SecurityId GetSecurityId(SecurityId securityId) => IsSupportSubscriptionBySecurity ? securityId : default;

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

					var info = _subscriptionsIds.TryGetValue(key);

					if (isSubscribe)
					{
						if (info == null)
						{
							sendIn = true;
							info = new SubscriptionInfo();
							_subscriptionsIds.Add(key, info);
							_requests.Add(info, message.TransactionId);
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
									_subscriptionsIds.Remove(key);
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
							message.OriginalTransactionId = _requests.GetAndRemove(info);
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