namespace StockSharp.Algo.Positions;

/// <summary>
/// The message adapter, automatically calculating position.
/// </summary>
public class PositionMessageAdapter : MessageAdapterWrapper
{
	private readonly SyncObject _sync = new();
	private readonly IPositionManager _positionManager;

	private readonly CachedSynchronizedSet<long> _subscriptions = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
	/// <param name="positionManager">The position calculation manager..</param>
	public PositionMessageAdapter(IMessageAdapter innerAdapter, IPositionManager positionManager)
		: base(innerAdapter)
	{
		_positionManager = positionManager ?? throw new ArgumentNullException(nameof(positionManager));

		if (_positionManager is ILogSource source && source.Parent == null)
			source.Parent = this;
	}

	/// <inheritdoc />
	public override IEnumerable<MessageTypeInfo> PossibleSupportedMessages
		=> InnerAdapter.PossibleSupportedMessages.Concat([MessageTypes.PortfolioLookup.ToInfo()]).Distinct();

	/// <inheritdoc />
	public override IEnumerable<MessageTypes> NotSupportedResultMessages
		=> InnerAdapter.NotSupportedResultMessages.Concat([MessageTypes.PortfolioLookup]).Distinct();

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_subscriptions.Clear();

				lock (_sync)
					_positionManager.ProcessMessage(message);

				break;
			}
			case MessageTypes.PortfolioLookup:
			{
				var lookupMsg = (PortfolioLookupMessage)message;

				if (lookupMsg.IsSubscribe)
				{
					if (!lookupMsg.IsHistoryOnly())
					{
						LogDebug("Subscription {0} added.", lookupMsg.TransactionId);
						_subscriptions.Add(lookupMsg.TransactionId);

						lock (_sync)
							_positionManager.ProcessMessage(message);
					}

					RaiseNewOutMessage(lookupMsg.CreateResult());
				}
				else
				{
					if (_subscriptions.Remove(lookupMsg.OriginalTransactionId))
					{
						LogDebug("Subscription {0} removed.", lookupMsg.OriginalTransactionId);

						lock (_sync)
							_positionManager.ProcessMessage(message);
					}

					RaiseNewOutMessage(lookupMsg.CreateResponse());
				}

				return true;
			}

			default:
			{
				lock (_sync)
					_positionManager.ProcessMessage(message);

				break;
			}
		}
		
		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		PositionChangeMessage change = null;

		if (message.Type is
			not MessageTypes.Reset and
			not MessageTypes.Connect and
			not MessageTypes.Disconnect)
		{
			lock (_sync)
				change = _positionManager.ProcessMessage(message);
		}

		if (change != null)
		{
			change.SetSubscriptionIds(_subscriptions.Cache);

			base.OnInnerAdapterNewOutMessage(change);
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="PositionMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone() => new PositionMessageAdapter(InnerAdapter.TypedClone(), _positionManager.Clone());
}