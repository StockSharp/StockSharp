namespace StockSharp.Algo;

/// <summary>
/// Message adapter that tracks multiple lookups requests and put them into single queue.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LookupTrackingMessageAdapter"/> with explicit state.
/// </remarks>
/// <param name="innerAdapter">Inner message adapter.</param>
/// <param name="state">State storage.</param>
public class LookupTrackingMessageAdapter(IMessageAdapter innerAdapter, ILookupTrackingManagerState state) : MessageAdapterWrapper(innerAdapter)
{
	private readonly ILookupTrackingManagerState _state = state ?? throw new ArgumentNullException(nameof(state));
	private static readonly TimeSpan _defaultTimeOut = TimeSpan.FromSeconds(10);

	private TimeSpan? _timeOut;

	/// <summary>
	/// Securities and portfolios lookup timeout.
	/// </summary>
	/// <remarks>
	/// By default is 10 seconds.
	/// </remarks>
	private TimeSpan TimeOut
	{
		get => _timeOut ?? InnerAdapter.LookupTimeout ?? _defaultTimeOut;
		set => _timeOut = value <= TimeSpan.Zero ? null : value;
	}

	/// <inheritdoc />
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_state.Clear();
				break;
			}

			default:
				if (message is ISubscriptionMessage subscrMsg && !ProcessLookupMessage(subscrMsg))
					return default;

				break;
		}

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	private bool ProcessLookupMessage(ISubscriptionMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var transId = message.TransactionId;

		var isEnqueue = false;
		var isStarted = false;

		try
		{
			if (message.IsSubscribe)
			{
				if (InnerAdapter.EnqueueSubscriptions)
				{
					if (_state.TryEnqueue(message.Type, transId, message.TypedClone()))
					{
						isEnqueue = true;
						return false;
					}
				}

				if (this.IsResultMessageNotSupported(message.Type) && TimeOut > TimeSpan.Zero)
				{
					_state.AddLookup(transId, message.TypedClone(), TimeOut);
					isStarted = true;
				}
			}

			return true;
		}
		finally
		{
			if (isEnqueue)
				LogInfo("Lookup queued {0}.", message);

			if (isStarted)
				LogInfo("Lookup timeout {0} started for {1}.", TimeOut, transId);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

		long[] ignoreIds = null;
		Message nextLookup = null;

		if (message is IOriginalTransactionIdMessage originIdMsg)
		{
			if (originIdMsg is SubscriptionFinishedMessage ||
				originIdMsg is SubscriptionOnlineMessage ||
				originIdMsg is SubscriptionResponseMessage resp && !resp.IsOk())
			{
				var id = originIdMsg.OriginalTransactionId;

				if (_state.TryGetAndRemoveLookup(id, out var info))
				{
					LogInfo("Lookup response {0}.", id);
					nextLookup = _state.TryDequeueNext(info.Type, info.TransactionId);
				}
				else
				{
					nextLookup = _state.TryDequeueFromAnyType(id);
				}
			}
			else if (message is ISubscriptionIdMessage subscrMsg)
			{
				ignoreIds = subscrMsg.GetSubscriptionIds();
				_state.IncreaseTimeOut(ignoreIds);
			}
		}

		if (nextLookup != null)
		{
			nextLookup.LoopBack(this);
			await base.OnInnerAdapterNewOutMessageAsync(nextLookup, cancellationToken);
		}

		if (message.LocalTime == default)
			return;

		List<Message> nextLookups = null;

		if (_state.PreviousTime == default)
		{
			_state.PreviousTime = message.LocalTime;
		}
		else if (message.LocalTime > _state.PreviousTime)
		{
			var diff = message.LocalTime - _state.PreviousTime;
			_state.PreviousTime = message.LocalTime;

			foreach (var (subscription, nextInQueue) in _state.ProcessTimeouts(diff, ignoreIds))
			{
				var transId = subscription.TransactionId;

				LogInfo("Lookup timeout {0}.", transId);

				await base.OnInnerAdapterNewOutMessageAsync(subscription.CreateResult(), cancellationToken);

				if (nextInQueue != null)
				{
					nextLookups ??= [];
					nextLookups.Add(nextInQueue);
				}
			}
		}

		if (nextLookups != null)
		{
			foreach (var lookup in nextLookups)
			{
				lookup.LoopBack(this);
				await base.OnInnerAdapterNewOutMessageAsync(lookup, cancellationToken);
			}
		}
	}

	/// <summary>
	/// Create a copy of <see cref="LookupTrackingMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
		=> new LookupTrackingMessageAdapter(InnerAdapter.TypedClone(), _state.GetType().CreateInstance<ILookupTrackingManagerState>()) { _timeOut = _timeOut };
}
