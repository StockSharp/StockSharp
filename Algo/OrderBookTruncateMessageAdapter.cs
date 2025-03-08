namespace StockSharp.Algo;

/// <summary>
/// The messages adapter build order book from incremental updates <see cref="QuoteChangeStates.Increment"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderBookTruncateMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Underlying adapter.</param>
public class OrderBookTruncateMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	private readonly SynchronizedDictionary<long, int> _depths = [];

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_depths.Clear();
				break;

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.DataType2 == DataType.MarketDepth)
					{
						if (mdMsg.SecurityId == default)
							break;

						if (mdMsg.DoNotBuildOrderBookIncrement)
							break;

						if (mdMsg.MaxDepth != null)
						{
							var actualDepth = mdMsg.MaxDepth.Value;

							var supportedDepth = InnerAdapter.NearestSupportedDepth(actualDepth);

							if (supportedDepth != actualDepth)
							{
								mdMsg = mdMsg.TypedClone();
								mdMsg.MaxDepth = supportedDepth;

								_depths.Add(mdMsg.TransactionId, actualDepth);

								LogInfo("MD truncate {0}/{1} ({2}->{3}).", mdMsg.SecurityId, mdMsg.TransactionId, actualDepth, supportedDepth);
							}
						}
					}
				}
				else
				{
					RemoveSubscription(mdMsg.OriginalTransactionId);
				}

				break;
			}
		}

		return base.OnSendInMessage(message);
	}

	private void RemoveSubscription(long id)
	{
		if (_depths.Remove(id))
			LogInfo("Unsubscribed {0}.", id);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		List<QuoteChangeMessage> clones = null;

		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;

				if (!responseMsg.IsOk())
					RemoveSubscription(responseMsg.OriginalTransactionId);

				break;
			}
			case MessageTypes.SubscriptionFinished:
			{
				RemoveSubscription(((SubscriptionFinishedMessage)message).OriginalTransactionId);
				break;
			}
			case MessageTypes.QuoteChange:
			{
				if (_depths.Count == 0)
					break;

				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State != null)
					break;

				foreach (var group in quoteMsg.GetSubscriptionIds().GroupBy(_depths.TryGetValue2))
				{
					if (group.Key == null)
						continue;

					clones ??= [];

					var maxDepth = group.Key.Value;

					var clone = quoteMsg.TypedClone();

					clone.SetSubscriptionIds([.. group]);

					if (clone.Bids.Length > maxDepth)
						clone.Bids = [.. clone.Bids.Take(maxDepth)];

					if (clone.Asks.Length > maxDepth)
						clone.Asks = [.. clone.Asks.Take(maxDepth)];

					clones.Add(clone);
				}

				if (clones != null)
				{
					var ids = quoteMsg.GetSubscriptionIds().Except(clones.SelectMany(c => c.GetSubscriptionIds())).ToArray();

					if (ids.Length > 0)
						quoteMsg.SetSubscriptionIds(ids);
					else
						message = null;
				}

				break;
			}
		}

		if (message != null)
			base.OnInnerAdapterNewOutMessage(message);

		if (clones != null)
		{
			foreach (var clone in clones)
				base.OnInnerAdapterNewOutMessage(clone);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="OrderBookTruncateMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone() => new OrderBookTruncateMessageAdapter(InnerAdapter.TypedClone());
}