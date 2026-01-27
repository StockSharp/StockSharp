namespace StockSharp.Algo;

/// <summary>
/// Level1 depth builder message processing logic.
/// </summary>
public interface ILevel1DepthBuilderManager : ICloneable<ILevel1DepthBuilderManager>
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result with messages to forward and output messages.</returns>
	(Message[] toInner, Message[] toOut) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result with forward message and extra output messages.</returns>
	(Message forward, Message[] extraOut) ProcessOutMessage(Message message);
}

/// <summary>
/// Level1 depth builder message processing implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Level1DepthBuilderManager"/> with explicit state.
/// </remarks>
/// <param name="logReceiver">Log receiver.</param>
/// <param name="state">State storage.</param>
public sealed class Level1DepthBuilderManager(ILogReceiver logReceiver, ILevel1DepthBuilderManagerState state) : ILevel1DepthBuilderManager
{
	private readonly ILogReceiver _logReceiver = logReceiver ?? throw new ArgumentNullException(nameof(logReceiver));
	private readonly ILevel1DepthBuilderManagerState _state = state ?? throw new ArgumentNullException(nameof(state));

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
					if (mdMsg.SecurityId == default || mdMsg.DataType2 != DataType.MarketDepth)
						return ([message], []);

					if (mdMsg.BuildMode == MarketDataBuildModes.Load)
						return ([message], []);

					if (mdMsg.BuildFrom != null && mdMsg.BuildFrom != DataType.Level1)
						return ([message], []);

					var transId = mdMsg.TransactionId;

					_state.AddSubscription(transId, mdMsg.SecurityId);

					mdMsg = mdMsg.TypedClone();
					mdMsg.DataType2 = DataType.Level1;

					_logReceiver.AddDebugLog("L1->OB {0} added.", transId);

					return ([mdMsg], []);
				}
				else
				{
					RemoveSubscription(mdMsg.OriginalTransactionId);
					return ([message], []);
				}
			}

			default:
				return ([message], []);
		}
	}

	/// <inheritdoc />
	public (Message forward, Message[] extraOut) ProcessOutMessage(Message message)
	{
		List<QuoteChangeMessage> books = null;

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

			case MessageTypes.Level1Change:
			{
				if (!_state.HasAnySubscriptions)
					break;

				var level1Msg = (Level1ChangeMessage)message;

				var ids = level1Msg.GetSubscriptionIds();

				HashSet<long> leftIds = null;

				foreach (var id in ids)
				{
					var quoteMsg = _state.TryBuildDepth(id, level1Msg, out var subscriptionIds);

					if (quoteMsg == null)
						continue;

					quoteMsg.SetSubscriptionIds(subscriptionIds);

					books ??= [];

					books.Add(quoteMsg);

					leftIds ??= [.. ids];

					leftIds.RemoveRange(subscriptionIds);
				}

				if (leftIds != null)
				{
					if (leftIds.Count == 0)
					{
						return (null, books?.ToArray() ?? []);
					}

					level1Msg.SetSubscriptionIds([.. leftIds]);
				}

				break;
			}
		}

		return (message, books?.ToArray() ?? []);
	}

	private void RemoveSubscription(long id)
	{
		_state.RemoveSubscription(id);
		_logReceiver.AddDebugLog("L1->OB {0} removed.", id);
	}

	/// <inheritdoc/>
	public ILevel1DepthBuilderManager Clone()
		=> new Level1DepthBuilderManager(_logReceiver, _state.GetType().CreateInstance<ILevel1DepthBuilderManagerState>());

	object ICloneable.Clone() => Clone();
}
