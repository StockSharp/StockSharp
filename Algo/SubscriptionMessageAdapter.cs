namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Cancel all market data subscriptions message.
	/// </summary>
	public class MarketDataCancelAllMessage : Message
	{
		/// <summary>
		/// Message type.
		/// </summary>
		public static MessageTypes MessageType => ExtendedMessageTypes.MarketDataCancelAll;

		/// <summary>
		/// Initializes a new instance of the <see cref="MarketDataCancelAllMessage"/>.
		/// </summary>
		public MarketDataCancelAllMessage()
			: base(MessageType)
		{
		}
	}

	/// <summary>
	/// Subscription counter adapter.
	/// </summary>
	public class SubscriptionMessageAdapter : MessageAdapterWrapper
	{
		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<MarketDataTypes, Dictionary<SecurityId, int>> _subscribers = new Dictionary<MarketDataTypes, Dictionary<SecurityId, int>>();
		private readonly Dictionary<string, int> _newsSubscribers = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
		private readonly Dictionary<string, int> _pfSubscribers = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
		//private readonly Dictionary<Tuple<MarketDataTypes, SecurityId>, List<MarketDataMessage>> _pendingMessages = new Dictionary<Tuple<MarketDataTypes, SecurityId>, List<MarketDataMessage>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="SubscriptionMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public SubscriptionMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:

					lock (_sync)
					{
						_subscribers.Clear();
						_newsSubscribers.Clear();
						_pfSubscribers.Clear();
						//_pendingMessages.Clear();
					}

					base.SendInMessage(message);
					break;

				case ExtendedMessageTypes.MarketDataCancelAll:
					var mgMsgs = new List<Message>();

					lock (_sync)
					{
						if (_newsSubscribers.Count > 0)
						{
							mgMsgs.Add(new MarketDataMessage
							{
								IsSubscribe = false,
								DataType = MarketDataTypes.News,
								TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId(),
							});

							_newsSubscribers.Clear();
						}

						if (_subscribers.Count > 0)
						{
							foreach (var pair in _subscribers)
							{
								mgMsgs.AddRange(pair.Value.Select(subscriber => new MarketDataMessage
								{
									IsSubscribe = false,
									DataType = pair.Key,
									SecurityId = subscriber.Key,
									TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId(),
								}));
							}

							_subscribers.Clear();
						}

						if (_pfSubscribers.Count > 0)
						{
							mgMsgs.AddRange(_pfSubscribers.Select(pair => new PortfolioMessage
							{
								IsSubscribe = false,
								PortfolioName = pair.Key,
								TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId(),
							}));

							_pfSubscribers.Clear();
						}
					}

					mgMsgs.ForEach(base.SendInMessage);

					RaiseNewOutMessage(message);

					break;

				case MessageTypes.MarketData:
					ProcessInMarketDataMessage((MarketDataMessage)message);
					break;

				case MessageTypes.Portfolio:
				{
					var pfMsg = (PortfolioMessage)message;
					var sendIn = false;

					lock (_sync)
					{
						var subscribersCount = _pfSubscribers.TryGetValue2(pfMsg.PortfolioName) ?? 0;

						if (pfMsg.IsSubscribe)
						{
							subscribersCount++;
							sendIn = subscribersCount == 1;
						}
						else
						{
							if (subscribersCount > 0)
							{
								subscribersCount--;
								sendIn = subscribersCount == 0;
							}
							//else
							//	sendOutMsg = NonExist(message);
						}

						if (subscribersCount > 0)
							_pfSubscribers[pfMsg.PortfolioName] = subscribersCount;
						else
							_pfSubscribers.Remove(pfMsg.PortfolioName);
					}
					
					if (sendIn)
						base.SendInMessage(message);

					break;
				}

				default:
					base.SendInMessage(message);
					break;
			}
		}

		///// <summary>
		///// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		///// </summary>
		///// <param name="message">The message.</param>
		//protected override void OnInnerAdapterNewOutMessage(Message message)
		//{
		//	switch (message.Type)
		//	{
		//		case MessageTypes.MarketData:
		//			ProcessOutMarketDataMessage((MarketDataMessage)message);
		//			break;
		//	}

		//	base.OnInnerAdapterNewOutMessage(message);
		//}

		private void ProcessInMarketDataMessage(MarketDataMessage message)
		{
			var sendIn = false;
			MarketDataMessage sendOutMsg = null;

			lock (_sync)
			{
				var isSubscribe = message.IsSubscribe;

				if (message.DataType == MarketDataTypes.News)
				{
					var subscriber = message.NewsId ?? string.Empty;

					var subscribersCount = _newsSubscribers.TryGetValue2(subscriber) ?? 0;

					if (isSubscribe)
					{
						subscribersCount++;
						sendIn = subscribersCount == 1;
					}
					else
					{
						if (subscribersCount > 0)
						{
							subscribersCount--;
							sendIn = subscribersCount == 0;
						}
						else
							sendOutMsg = NonExist(message);
					}

					if (sendOutMsg == null)
					{
						if (!sendIn)
						{
							sendOutMsg = new MarketDataMessage
							{
								DataType = message.DataType,
								IsSubscribe = isSubscribe,
								OriginalTransactionId = message.TransactionId,
							};
						}

						if (subscribersCount > 0)
							_newsSubscribers[subscriber] = subscribersCount;
						else
							_newsSubscribers.Remove(subscriber);
					}
				}
				else
				{
					var subscribers = _subscribers.SafeAdd(message.DataType);
					var securityId = message.SecurityId;

					var subscribersCount = subscribers.TryGetValue2(securityId) ?? 0;

					if (isSubscribe)
					{
						subscribersCount++;
						sendIn = subscribersCount == 1;
					}
					else
					{
						if (subscribersCount > 0)
						{
							subscribersCount--;
							sendIn = subscribersCount == 0;
						}
						else
							sendOutMsg = NonExist(message);
					}

					if (sendOutMsg == null)
					{
						if (!sendIn)
						{
							sendOutMsg = new MarketDataMessage
							{
								DataType = message.DataType,
								IsSubscribe = isSubscribe,
								SecurityId = securityId,
								OriginalTransactionId = message.TransactionId,
							};
						}

						if (subscribersCount > 0)
							subscribers[securityId] = subscribersCount;
						else
							subscribers.Remove(securityId);
					}
				}
			}

			if (sendIn)
				base.SendInMessage(message);

			if (sendOutMsg != null)
				RaiseNewOutMessage(sendOutMsg);
		}

		//private void ProcessOutMarketDataMessage(MarketDataMessage message)
		//{
		// TODO
		//	lock (_sync)
		//	{
		//		var pending = _pendingMessages.TryGetValue(Tuple.Create(message.DataType, message.SecurityId));
		//	}
		//}

		private static MarketDataMessage NonExist(MarketDataMessage message)
		{
			return new MarketDataMessage
			{
				DataType = message.DataType,
				IsSubscribe = false,
				SecurityId = message.SecurityId,
				OriginalTransactionId = message.TransactionId,
				Error = new InvalidOperationException(LocalizedStrings.SubscriptionNonExist),
			};
		}

		/// <summary>
		/// Create a copy of <see cref="SubscriptionMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new SubscriptionMessageAdapter(InnerAdapter);
		}
	}
}