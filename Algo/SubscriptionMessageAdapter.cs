namespace StockSharp.Algo
{
	using System;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Subscription counter adapter.
	/// </summary>
	public class SubscriptionMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<MarketDataTypes, CachedSynchronizedDictionary<SecurityId, int>> _subscribers = new SynchronizedDictionary<MarketDataTypes, CachedSynchronizedDictionary<SecurityId, int>>();
		private readonly SynchronizedDictionary<string, int> _newsSubscribers = new SynchronizedDictionary<string, int>(StringComparer.InvariantCultureIgnoreCase);

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
					_subscribers.Clear();
					_newsSubscribers.Clear();
					break;

				case MessageTypes.MarketData:
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;

				default:
					base.SendInMessage(message);
					break;
			}
		}

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			var isSubscribe = message.IsSubscribe;

			if (message.DataType == MarketDataTypes.News)
			{
				var subscriber = message.NewsId;

				var subscribersCount = _newsSubscribers.TryGetValue2(subscriber) ?? 0;

				if (isSubscribe)
					subscribersCount++;
				else
				{
					if (subscribersCount > 0)
						subscribersCount--;
				}

				if (subscribersCount > 0)
					_newsSubscribers[subscriber] = subscribersCount;
				else
					_newsSubscribers.Remove(subscriber);

				if (subscribersCount > 1)
				{
					var msg = new MarketDataMessage
					{
						DataType = message.DataType,
						IsSubscribe = isSubscribe,
						OriginalTransactionId = message.TransactionId,
					};

					RaiseNewOutMessage(msg);
				}
				else
					base.SendInMessage(message);
			}
			else
			{
				var subscribers = _subscribers.SafeAdd(message.DataType);
				var securityId = message.SecurityId;
				var subscribersCount = subscribers.ChangeSubscribers(securityId, isSubscribe);

				if (subscribersCount > 1)
				{
					var msg = new MarketDataMessage
					{
						DataType = message.DataType,
						IsSubscribe = isSubscribe,
						SecurityId = securityId,
						OriginalTransactionId = message.TransactionId,
					};

					RaiseNewOutMessage(msg);
				}
				else
					base.SendInMessage(message);
			}
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