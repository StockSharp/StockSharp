namespace StockSharp.Algo
{
	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Subscription counter adapter.
	/// </summary>
	public class SubscriptionMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<MarketDataTypes, CachedSynchronizedDictionary<SecurityId, int>> _subscribers = new SynchronizedDictionary<MarketDataTypes, CachedSynchronizedDictionary<SecurityId, int>>();

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
			var securityId = message.SecurityId;

			var subscribersCount = _subscribers
				.SafeAdd(message.DataType)
				.ChangeSubscribers(securityId, message.IsSubscribe);

			if (subscribersCount > 1)
			{
				var msg = new MarketDataMessage
				{
					DataType = message.DataType,
					IsSubscribe = message.IsSubscribe,
					SecurityId = securityId,
					OriginalTransactionId = message.TransactionId,
				};

				//message.CopyTo(msg);
				RaiseNewOutMessage(msg);
			}
			else
				base.SendInMessage(message);
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