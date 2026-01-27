namespace StockSharp.Algo;

/// <summary>
/// Order book truncation logic interface.
/// </summary>
public interface IOrderBookTruncateManager : ICloneable<IOrderBookTruncateManager>
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
/// Initializes a new instance of the <see cref="OrderBookTruncateManager"/> with explicit state.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
/// <param name="nearestSupportedDepth">Function to get nearest supported depth.</param>
/// <param name="state">State storage.</param>
public sealed class OrderBookTruncateManager(ILogReceiver logReceiver, Func<int, int?> nearestSupportedDepth, IOrderBookTruncateManagerState state) : IOrderBookTruncateManager
{
	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly Func<int, int?> _nearestSupportedDepth = nearestSupportedDepth ?? throw new ArgumentNullException(nameof(nearestSupportedDepth));
	private readonly IOrderBookTruncateManagerState _state = state ?? throw new ArgumentNullException(nameof(state));

	/// <inheritdoc />
	public (Message toInner, Message[] toOut) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
				_state.Clear();
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
								_state.AddDepth(mdMsg.TransactionId, actualDepth);

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
				if (!_state.HasDepths)
					break;

				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State != null)
					break;

				foreach (var (depth, ids) in _state.GroupByDepth(quoteMsg.GetSubscriptionIds()))
				{
					if (depth == null)
						continue;

					clones ??= [];

					var maxDepth = depth.Value;

					var clone = quoteMsg.TypedClone();

					clone.SetSubscriptionIds(ids);

					if (clone.Bids.Length > maxDepth)
						clone.Bids = [.. clone.Bids.Take(maxDepth)];

					if (clone.Asks.Length > maxDepth)
						clone.Asks = [.. clone.Asks.Take(maxDepth)];

					clones.Add(clone);
				}

				if (clones != null)
				{
					var remainingIds = quoteMsg.GetSubscriptionIds().Except(clones.SelectMany(c => c.GetSubscriptionIds())).ToArray();

					if (remainingIds.Length > 0)
						quoteMsg.SetSubscriptionIds(remainingIds);
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
		if (_state.RemoveDepth(id))
			_logReceiver.AddInfoLog("Unsubscribed {0}.", id);
	}

	/// <inheritdoc/>
	public IOrderBookTruncateManager Clone()
		=> new OrderBookTruncateManager(_logReceiver, _nearestSupportedDepth, _state.GetType().CreateInstance<IOrderBookTruncateManagerState>());

	object ICloneable.Clone() => Clone();
}
