namespace StockSharp.Algo.Positions;

/// <summary>
/// The message adapter, automatically calculating position.
/// </summary>
public class PositionMessageAdapter : MessageAdapterWrapper
{
	private readonly Lock _sync = new();
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
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_subscriptions.Clear();

				using (_sync.EnterScope())
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

						using (_sync.EnterScope())
							_positionManager.ProcessMessage(message);
					}

					await RaiseNewOutMessageAsync(lookupMsg.CreateResult(), cancellationToken);
				}
				else
				{
					if (_subscriptions.Remove(lookupMsg.OriginalTransactionId))
					{
						LogDebug("Subscription {0} removed.", lookupMsg.OriginalTransactionId);

						using (_sync.EnterScope())
							_positionManager.ProcessMessage(message);
					}

					await RaiseNewOutMessageAsync(lookupMsg.CreateResponse(), cancellationToken);
				}

				return;
			}

			default:
			{
				using (_sync.EnterScope())
					_positionManager.ProcessMessage(message);

				break;
			}
		}

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		PositionChangeMessage change = null;

		if (message.Type is
			not MessageTypes.Reset and
			not MessageTypes.Connect and
			not MessageTypes.Disconnect)
		{
			using (_sync.EnterScope())
				change = _positionManager.ProcessMessage(message);
		}

		if (change != null)
		{
			change.SetSubscriptionIds(_subscriptions.Cache);

			await base.OnInnerAdapterNewOutMessageAsync(change, cancellationToken);
		}

		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="PositionMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone() => new PositionMessageAdapter(InnerAdapter.TypedClone(), _positionManager.Clone());
}