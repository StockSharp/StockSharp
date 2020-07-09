namespace StockSharp.Algo
{
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter sort unsorted order books <see cref="QuoteChangeMessage.IsSorted"/>.
	/// </summary>
	public class OrderBookSortMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderBookSortMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderBookSortMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.QuoteChange:
				{
					var quotesMsg = (QuoteChangeMessage)message;

					if (!quotesMsg.IsSorted)
					{
						quotesMsg.Bids = quotesMsg.Bids.OrderByDescending(q => q.Price).ToArray();
						quotesMsg.Asks = quotesMsg.Asks.OrderBy(q => q.Price).ToArray();

						quotesMsg.IsSorted = true;
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="OrderBookSortMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderBookSortMessageAdapter(InnerAdapter.TypedClone());
		}
	}
}