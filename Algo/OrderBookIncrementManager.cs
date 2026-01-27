namespace StockSharp.Algo;

/// <summary>
/// Order book increment message processing logic.
/// </summary>
public interface IOrderBookIncrementManager : ICloneable<IOrderBookIncrementManager>
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result.</returns>
	(Message[] toInner, Message[] toOut) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result.</returns>
	(Message forward, Message[] extraOut) ProcessOutMessage(Message message);
}

/// <summary>
/// Order book increment message processing implementation.
/// Builds full order books from incremental updates <see cref="QuoteChangeStates.Increment"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderBookIncrementManager"/> with explicit state.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
/// <param name="state">State storage.</param>
public sealed class OrderBookIncrementManager(ILogReceiver logReceiver, IOrderBookIncrementManagerState state) : IOrderBookIncrementManager
{
	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly IOrderBookIncrementManagerState _state = state ?? throw new ArgumentNullException(nameof(state));

	/// <inheritdoc />
	public (Message[] toInner, Message[] toOut) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_state.Clear();
				return ([message], []);
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;

				if (mdMsg.IsSubscribe)
				{
					if (mdMsg.DataType2 == DataType.MarketDepth)
					{
						var transId = mdMsg.TransactionId;

						if (mdMsg.SecurityId == default)
						{
							if (mdMsg.DoNotBuildOrderBookIncrement)
								_state.AddAllSecPassThrough(transId);
							else
								_state.AddAllSecSubscription(transId);

							break;
						}

						if (mdMsg.DoNotBuildOrderBookIncrement)
						{
							_state.AddPassThrough(transId);
							break;
						}

						_state.AddSubscription(transId, mdMsg.SecurityId, _logReceiver);

						_logReceiver.AddInfoLog("OB incr subscribed {0}/{1}.", mdMsg.SecurityId, transId);
					}
				}
				else
				{
					RemoveSubscription(mdMsg.OriginalTransactionId);
				}

				break;
			}
		}

		return ([message], []);
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

			case MessageTypes.SubscriptionOnline:
			{
				var id = ((SubscriptionOnlineMessage)message).OriginalTransactionId;
				_state.OnSubscriptionOnline(id);
				break;
			}

			case MessageTypes.QuoteChange:
			{
				var quoteMsg = (QuoteChangeMessage)message;

				if (quoteMsg.State == null)
					break;

				if (!_state.HasAnySubscriptions)
					break;

				List<long> passThrough = null;

				foreach (var subscriptionId in quoteMsg.GetSubscriptionIds())
				{
					var newQuoteMsg = _state.TryApply(subscriptionId, quoteMsg, out var ids);

					if (newQuoteMsg != null)
					{
						clones ??= [];

						newQuoteMsg.SetSubscriptionIds(ids);
						clones.Add(newQuoteMsg);
					}
					else if (_state.IsPassThrough(subscriptionId) || _state.IsAllSecPassThrough(subscriptionId))
					{
						passThrough ??= [];
						passThrough.Add(subscriptionId);
					}
				}

				if (passThrough is null)
					message = null;
				else
					quoteMsg.SetSubscriptionIds([.. passThrough]);

				break;
			}
		}

		return (message, clones?.ToArray() ?? []);
	}

	private void RemoveSubscription(long id)
	{
		_state.RemoveSubscription(id);
		_logReceiver.AddInfoLog("Unsubscribed {0}.", id);
	}

	/// <inheritdoc/>
	public IOrderBookIncrementManager Clone()
		=> new OrderBookIncrementManager(_logReceiver, _state.GetType().CreateInstance<IOrderBookIncrementManagerState>());

	object ICloneable.Clone() => Clone();
}
