namespace StockSharp.Algo
{
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// Level1 depth builder adapter.
	/// </summary>
	public class Level1DepthBuilderAdapter : MessageAdapterWrapper
	{
		private sealed class Level1DepthBuilder
		{
			private readonly SecurityId _securityId;
			private decimal? _bidPrice, _askPrice, _bidVolume, _askVolume;

			public bool HasDepth { get; set; }

			public Level1DepthBuilder(SecurityId securityId)
			{
				_securityId = securityId;
			}

			public QuoteChangeMessage Process(Level1ChangeMessage message)
			{
				if (HasDepth)
					return null;

				var bidPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidPrice);
				var askPrice = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskPrice);

				if (bidPrice == null && askPrice == null)
					return null;

				var bidVolume = (decimal?)message.Changes.TryGetValue(Level1Fields.BestBidVolume);
				var askVolume = (decimal?)message.Changes.TryGetValue(Level1Fields.BestAskVolume);

				if (_bidPrice == bidPrice && _askPrice == askPrice && _bidVolume == bidVolume && _askVolume == askVolume)
					return null;

				_bidPrice = bidPrice;
				_askPrice = askPrice;
				_bidVolume = bidVolume;
				_askVolume = askVolume;

				return new QuoteChangeMessage
				{
					SecurityId = _securityId,
					ServerTime = message.ServerTime,
					LocalTime = message.LocalTime,
					IsByLevel1 = true,
					IsSorted = true,
					Bids = bidPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Buy, bidPrice.Value, bidVolume ?? 0) },
					Asks = askPrice == null ? Enumerable.Empty<QuoteChange>() : new[] { new QuoteChange(Sides.Sell, askPrice.Value, askVolume ?? 0) },
				};
			}
		}

		private readonly SynchronizedDictionary<SecurityId, Level1DepthBuilder> _level1DepthBuilders = new SynchronizedDictionary<SecurityId, Level1DepthBuilder>();

		/// <summary>
		/// Initializes a new instance of the <see cref="Level1DepthBuilderAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public Level1DepthBuilderAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message.IsBack)
			{
				base.OnInnerAdapterNewOutMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
					_level1DepthBuilders.Clear();
					break;

				case MessageTypes.Level1Change:
				{
					var level1Msg = (Level1ChangeMessage)message;

					// генерация стакана из Level1
					var quoteMsg = GetBuilder(level1Msg.SecurityId).Process(level1Msg);

					if (quoteMsg != null)
						RaiseNewOutMessage(quoteMsg);

					break;
				}

				case MessageTypes.QuoteChange:
				{
					var quoteMsg = (QuoteChangeMessage)message;
					GetBuilder(quoteMsg.SecurityId).HasDepth = true;
					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private Level1DepthBuilder GetBuilder(SecurityId securityId)
		{
			return _level1DepthBuilders.SafeAdd(securityId, c => new Level1DepthBuilder(c));
		}

		/// <summary>
		/// Create a copy of <see cref="Level1DepthBuilderAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new Level1DepthBuilderAdapter(InnerAdapter);
		}
	}
}