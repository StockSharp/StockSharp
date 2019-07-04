namespace StockSharp.Algo
{
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter build order book and tick data from order log flow.
	/// </summary>
	public class OrderBookMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<SecurityId, int> _depths = new SynchronizedDictionary<SecurityId, int>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
					_depths.Clear();
					break;

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType == MarketDataTypes.MarketDepth)
					{
						if (mdMsg.IsSubscribe)
						{
							if (mdMsg.MaxDepth != null)
								_depths[mdMsg.SecurityId] = mdMsg.MaxDepth.Value;
						}
						else
							_depths.Remove(mdMsg.SecurityId);
					}

					break;
				}
			}

			base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;

					if (_depths.TryGetValue(quoteMsg.SecurityId, out var maxDepth))
					{
						if (quoteMsg.Bids.Length > maxDepth)
							quoteMsg.Bids = quoteMsg.Bids.Take(maxDepth).ToArray();

						if (quoteMsg.Asks.Length > maxDepth)
							quoteMsg.Asks = quoteMsg.Asks.Take(maxDepth).ToArray();
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="OrderBookMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderBookMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}