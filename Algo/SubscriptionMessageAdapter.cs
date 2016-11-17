namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Subscription counter adapter.
	/// </summary>
	public class SubscriptionMessageAdapter : MessageAdapterWrapper
	{
		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<MarketDataTypes, Dictionary<SecurityId, int>> _subscribers = new Dictionary<MarketDataTypes, Dictionary<SecurityId, int>>();
		private readonly Dictionary<string, int> _newsSubscribers = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);
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
						//_pendingMessages.Clear();
					}

					break;

				case MessageTypes.MarketData:
					ProcessInMarketDataMessage((MarketDataMessage)message);
					break;

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
						subscribersCount++;
					else
					{
						if (subscribersCount > 0)
							subscribersCount--;
						else
							sendOutMsg = NonExist(message);
					}

					if (sendOutMsg == null)
					{
						if (subscribersCount > 0)
							_newsSubscribers[subscriber] = subscribersCount;
						else
							_newsSubscribers.Remove(subscriber);

						if (subscribersCount > 1)
						{
							sendOutMsg = new MarketDataMessage
							{
								DataType = message.DataType,
								IsSubscribe = isSubscribe,
								OriginalTransactionId = message.TransactionId,
							};
						}
						else
							sendIn = true;
					}
				}
				else
				{
					var subscribers = _subscribers.SafeAdd(message.DataType);
					var securityId = message.SecurityId;

					var subscribersCount = subscribers.TryGetValue2(securityId) ?? 0;

					if (isSubscribe)
						subscribersCount++;
					else
					{
						if (subscribersCount > 0)
							subscribersCount--;
						else
							sendOutMsg = NonExist(message);
					}

					if (sendOutMsg == null)
					{
						if (subscribersCount > 0)
							subscribers[securityId] = subscribersCount;
						else
							subscribers.Remove(securityId);

						if (subscribersCount > 1)
						{
							sendOutMsg = new MarketDataMessage
							{
								DataType = message.DataType,
								IsSubscribe = isSubscribe,
								SecurityId = securityId,
								OriginalTransactionId = message.TransactionId,
							};
						}
						else
							sendIn = true;
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