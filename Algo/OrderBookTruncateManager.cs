namespace StockSharp.Algo;

/// <summary>
/// Order book truncation logic interface.
/// </summary>
public interface IOrderBookTruncateManager
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result: message to send to inner adapter (possibly modified) and extra messages to send out.</returns>
	(Message toInner, Message[] toOut) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result: forward message (or null to skip) and additional output messages.</returns>
	(Message forward, Message[] extraOut) ProcessOutMessage(Message message);
}

/// <summary>
/// Order book truncation logic implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderBookTruncateManager"/>.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
/// <param name="nearestSupportedDepth">Function to get nearest supported depth.</param>
public sealed class OrderBookTruncateManager(ILogReceiver logReceiver, Func<int, int?> nearestSupportedDepth) : IOrderBookTruncateManager
{
	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly Func<int, int?> _nearestSupportedDepth = nearestSupportedDepth ?? throw new ArgumentNullException(nameof(nearestSupportedDepth));
	private readonly SynchronizedDictionary<long, int> _depths = [];

	/// <inheritdoc />
	public (Message toInner, Message[] toOut) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_depths.Clear();
				return (message, []);

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.DataType2 == DataType.MarketDepth)
					{
						if (mdMsg.SecurityId == default)
							return (message, []);

						if (mdMsg.DoNotBuildOrderBookIncrement)
							return (message, []);

						if (mdMsg.MaxDepth != null)
						{
							var actualDepth = mdMsg.MaxDepth.Value;

							var supportedDepth = _nearestSupportedDepth(actualDepth);

							if (supportedDepth != actualDepth)
							{
								_depths.Add(mdMsg.TransactionId, actualDepth);

								if (supportedDepth != null)
								{
									mdMsg = mdMsg.TypedClone();
									mdMsg.MaxDepth = supportedDepth;
									message = mdMsg;

									_logReceiver.AddInfoLog("MD truncate {0}/{1} ({2}->{3}).", mdMsg.SecurityId, mdMsg.TransactionId, actualDepth, supportedDepth);
								}
								else
								{
									_logReceiver.AddInfoLog("MD truncate {0}/{1} (no supported depths, will truncate to {2}).", mdMsg.SecurityId, mdMsg.TransactionId, actualDepth);
								}
							}
						}
					}
				}
				else
				{
					RemoveSubscription(mdMsg.OriginalTransactionId);
				}

				return (message, []);
			}
		}

		return (message, []);
	}

	/// <inheritdoc />
	public (Message forward, Message[] extraOut) ProcessOutMessage(Message message)
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

		return (message, clones?.ToArray<Message>() ?? []);
	}

	private void RemoveSubscription(long id)
	{
		if (_depths.Remove(id))
			_logReceiver.AddInfoLog("Unsubscribed {0}.", id);
	}
}
