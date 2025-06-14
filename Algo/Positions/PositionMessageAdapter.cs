namespace StockSharp.Algo.Positions;

/// <summary>
/// The message adapter, automatically calculating position.
/// </summary>
public class PositionMessageAdapter : MessageAdapterWrapper
{
	private readonly SyncObject _sync = new();
	private readonly IPositionManager _positionManager;

	private readonly CachedSynchronizedSet<long> _subscriptions = [];
	private readonly SynchronizedDictionary<string, CachedSynchronizedSet<long>> _strategySubscriptions = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly SynchronizedDictionary<long, string> _strategyIdMap = [];

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

	private bool IsEmulate => InnerAdapter.IsPositionsEmulationRequired != null;

	/// <inheritdoc />
	public override IEnumerable<MessageTypeInfo> PossibleSupportedMessages
		=> InnerAdapter.PossibleSupportedMessages.Concat(IsEmulate ? [MessageTypes.PortfolioLookup.ToInfo()] : []).Distinct();

	/// <inheritdoc />
	public override IEnumerable<MessageTypes> NotSupportedResultMessages
		=> InnerAdapter.NotSupportedResultMessages.Concat(IsEmulate ? [MessageTypes.PortfolioLookup] : []).Distinct();

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_subscriptions.Clear();
				_strategyIdMap.Clear();
				_strategySubscriptions.Clear();

				lock (_sync)
					_positionManager.ProcessMessage(message);

				break;
			}
			case MessageTypes.PortfolioLookup:
			{
				var lookupMsg = (PortfolioLookupMessage)message;

				if (lookupMsg.IsSubscribe)
				{
					if (!lookupMsg.StrategyId.IsEmpty())
					{
						if (!lookupMsg.IsHistoryOnly())
						{
							LogDebug("Subscription (strategy='{1}') {0} added.", lookupMsg.TransactionId, lookupMsg.StrategyId);
							_strategyIdMap.Add(lookupMsg.TransactionId, lookupMsg.StrategyId);
							_strategySubscriptions.SafeAdd(lookupMsg.StrategyId).Add(lookupMsg.TransactionId);
						}
							
						RaiseNewOutMessage(lookupMsg.CreateResult());
						return true;
					}

					if (!lookupMsg.IsHistoryOnly())
					{
						LogDebug("Subscription {0} added.", lookupMsg.TransactionId);
						_subscriptions.Add(lookupMsg.TransactionId);

						lock (_sync)
							_positionManager.ProcessMessage(message);
					}

					if (IsEmulate)
					{
						RaiseNewOutMessage(lookupMsg.CreateResult());
						return true;
					}
				}
				else
				{
					if (_subscriptions.Remove(lookupMsg.OriginalTransactionId))
					{
						LogDebug("Subscription {0} removed.", lookupMsg.OriginalTransactionId);

						lock (_sync)
							_positionManager.ProcessMessage(message);
					}
					else if (_strategyIdMap.TryGetAndRemove(lookupMsg.OriginalTransactionId, out var strategyId))
					{
						_strategySubscriptions.TryGetValue(strategyId)?.Remove(lookupMsg.OriginalTransactionId);
						LogDebug("Subscription (strategy='{1}') {0} removed.", lookupMsg.OriginalTransactionId, strategyId);
						return true;
					}

					if (IsEmulate)
					{
						RaiseNewOutMessage(lookupMsg.CreateResponse());
						return true;
					}
				}

				break;
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
			var subscriptions = change.StrategyId.IsEmpty() ? _subscriptions.Cache : _strategySubscriptions.TryGetValue(change.StrategyId)?.Cache;

			if (subscriptions?.Length > 0)
				change.SetSubscriptionIds(subscriptions);

			base.OnInnerAdapterNewOutMessage(change);
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="PositionMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone() => new PositionMessageAdapter(InnerAdapter.TypedClone(), _positionManager);
}