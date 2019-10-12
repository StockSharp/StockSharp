namespace StockSharp.Algo
{
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
	/// </summary>
	public class OrderBookTruncateMessageAdapter : MessageAdapterWrapper
	{
		private readonly SynchronizedDictionary<SecurityId, int> _depths = new SynchronizedDictionary<SecurityId, int>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookTruncateMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookTruncateMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
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
							{
								var actualDepth = mdMsg.MaxDepth.Value;

								var supportedDepth = InnerAdapter.NearestSupportedDepth(actualDepth);

								if (supportedDepth != actualDepth)
								{
									mdMsg = (MarketDataMessage)mdMsg.Clone();
									mdMsg.MaxDepth = supportedDepth;

									_depths[mdMsg.SecurityId] = actualDepth;
								}
							}
						}
						else
							_depths.Remove(mdMsg.SecurityId);
					}

					break;
				}
			}

			base.OnSendInMessage(message);
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
		/// Create a copy of <see cref="OrderBookTruncateMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderBookTruncateMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}