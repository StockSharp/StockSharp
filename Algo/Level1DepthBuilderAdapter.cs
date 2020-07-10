namespace StockSharp.Algo
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
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
					BuildFrom = DataType.Level1,
					Bids = bidPrice == null ? ArrayHelper.Empty<QuoteChange>() : new[] { new QuoteChange(bidPrice.Value, bidVolume ?? 0) },
					Asks = askPrice == null ? ArrayHelper.Empty<QuoteChange>() : new[] { new QuoteChange(askPrice.Value, askVolume ?? 0) },
				};
			}
		}

		private readonly SynchronizedDictionary<long, Level1DepthBuilder> _subscriptions = new SynchronizedDictionary<long, Level1DepthBuilder>();

		/// <summary>
		/// Initializes a new instance of the <see cref="Level1DepthBuilderAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		public Level1DepthBuilderAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override bool SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_subscriptions.Clear();
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.SecurityId == default || mdMsg.DataType2 != DataType.MarketDepth)
							break;

						if (mdMsg.BuildMode == MarketDataBuildModes.Load)
							break;

						if (mdMsg.BuildFrom != null && mdMsg.BuildFrom != DataType.Level1)
							break;

						_subscriptions.Add(mdMsg.TransactionId, new Level1DepthBuilder(mdMsg.SecurityId));
						
						mdMsg = mdMsg.TypedClone();
						mdMsg.DataType2 = DataType.Level1;
						message = mdMsg;

						this.AddDebugLog("L1->OB {0} added.", mdMsg.TransactionId);
					}
					else
					{
						if (_subscriptions.Remove(mdMsg.OriginalTransactionId))
						{
							this.AddDebugLog("L1->OB {0} removed.", mdMsg.OriginalTransactionId);
						}
					}

					break;
				}
			}

			return base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var responseMsg = (SubscriptionResponseMessage)message;

					if (!responseMsg.IsOk())
					{
						if (_subscriptions.Remove(responseMsg.OriginalTransactionId))
							this.AddDebugLog("L1->OB {0} removed.", responseMsg.OriginalTransactionId);
					}

					break;
				}
				case MessageTypes.SubscriptionFinished:
				{
					var finishedMsg = (SubscriptionFinishedMessage)message;
					
					if (_subscriptions.Remove(finishedMsg.OriginalTransactionId))
						this.AddDebugLog("L1->OB {0} removed.", finishedMsg.OriginalTransactionId);

					break;
				}
				case MessageTypes.Level1Change:
				{
					if (_subscriptions.Count == 0)
						break;

					var level1Msg = (Level1ChangeMessage)message;

					var ids = level1Msg.GetSubscriptionIds();

					List<QuoteChangeMessage> books = null;
					HashSet<long> leftIds = null;

					foreach (var id in ids)
					{
						if (!_subscriptions.TryGetValue(id, out var builder))
							continue;
						
						var quoteMsg = builder.Process(level1Msg);

						if (quoteMsg == null)
							continue;
							
						quoteMsg.SubscriptionId = id;

						if (books == null)
							books = new List<QuoteChangeMessage>();

						books.Add(quoteMsg);

						if (leftIds == null)
							leftIds = new HashSet<long>(ids);

						leftIds.Remove(id);
					}

					if (books != null)
					{
						foreach (var book in books)
						{
							base.OnInnerAdapterNewOutMessage(book);
						}
					}

					if (leftIds != null)
					{
						if (leftIds.Count == 0)
							return;

						level1Msg.SetSubscriptionIds(leftIds.ToArray());
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="Level1DepthBuilderAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone() => new Level1DepthBuilderAdapter(InnerAdapter.TypedClone());
	}
}