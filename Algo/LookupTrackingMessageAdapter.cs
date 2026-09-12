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
	private readonly Lock _timeoutSync = new();
	private readonly Dictionary<long, (CancellationTokenSource source, TimeSpan timeout)> _timeoutSources = [];
	private static readonly TimeSpan _defaultTimeOut = TimeSpan.FromSeconds(10);

	private TimeSpan? _timeOut;
	private bool _isDisposed;

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
				using (_timeoutSync.EnterScope())
				{
					_state.Clear();
					CancelAllTimeouts();
				}

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
					var timeout = TimeOut;

					using (_timeoutSync.EnterScope())
					{
						_state.AddLookup(transId, message.TypedClone(), timeout);
						RestartTimeout(transId, timeout);
					}

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
		long[] ignoreIds = null;
		Message nextLookup = null;

		if (message is IOriginalTransactionIdMessage originIdMsg &&
			(originIdMsg is SubscriptionFinishedMessage ||
			 originIdMsg is SubscriptionOnlineMessage ||
			 originIdMsg is SubscriptionResponseMessage resp && !resp.IsOk()))
		{
			var id = originIdMsg.OriginalTransactionId;

			using (_timeoutSync.EnterScope())
			{
				CancelTimeout(id);

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
		}
		else if (message is ISubscriptionIdMessage subscrMsg)
		{
			ignoreIds = subscrMsg.GetSubscriptionIds();

			using (_timeoutSync.EnterScope())
			{
				_state.IncreaseTimeOut(ignoreIds);

				foreach (var id in ignoreIds)
				{
					if (_timeoutSources.TryGetValue(id, out var registration))
						RestartTimeout(id, registration.timeout);
				}
			}
		}

		List<(ISubscriptionMessage subscription, Message nextInQueue)> timedOut = null;

		if (message.LocalTime != default)
		{
			using (_timeoutSync.EnterScope())
			{
				if (_state.PreviousTime == default)
				{
					_state.PreviousTime = message.LocalTime;
				}
				else if (message.LocalTime > _state.PreviousTime)
				{
					var diff = message.LocalTime - _state.PreviousTime;
					_state.PreviousTime = message.LocalTime;
					timedOut = [.. _state.ProcessTimeouts(diff, ignoreIds)];

					foreach (var (subscription, _) in timedOut)
						CancelTimeout(subscription.TransactionId);
				}
			}
		}

		// Update all timeout state before invoking user handlers. A slow handler must not let a lookup
		// expire after a row arrived, and a reentrant reset must not be overwritten by this message's
		// old clock after the handler returns.
		await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

		if (nextLookup != null)
		{
			nextLookup.LoopBack(this);
			await base.OnInnerAdapterNewOutMessageAsync(nextLookup, cancellationToken);
		}

		List<Message> nextLookups = null;

		if (timedOut != null)
		{
			foreach (var (subscription, nextInQueue) in timedOut)
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

	private void RestartTimeout(long transactionId, TimeSpan timeout)
	{
		CancelTimeout(transactionId);

		if (_isDisposed)
			return;

		var source = new CancellationTokenSource();
		_timeoutSources.Add(transactionId, (source, timeout));
		_ = WaitForTimeoutAsync(transactionId, timeout, source);
	}

	private async Task WaitForTimeoutAsync(long transactionId, TimeSpan timeout, CancellationTokenSource source)
	{
		try
		{
			await Task.Delay(timeout, source.Token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		ISubscriptionMessage subscription;
		Message nextLookup;

		using (_timeoutSync.EnterScope())
		{
			if (!_timeoutSources.TryGetValue(transactionId, out var current) || !ReferenceEquals(current.source, source))
				return;

			_timeoutSources.Remove(transactionId);
			source.Dispose();

			if (!_state.TryGetAndRemoveLookup(transactionId, out subscription))
				return;

			nextLookup = _state.TryDequeueNext(subscription.Type, subscription.TransactionId);
		}

		try
		{
			LogInfo("Lookup timeout {0}.", transactionId);
			await base.OnInnerAdapterNewOutMessageAsync(subscription.CreateResult(), default);

			if (nextLookup != null)
			{
				nextLookup.LoopBack(this);
				await base.OnInnerAdapterNewOutMessageAsync(nextLookup, default);
			}
		}
		catch (Exception ex)
		{
			this.AddErrorLog(ex);
		}
	}

	private void CancelTimeout(long transactionId)
	{
		if (!_timeoutSources.Remove(transactionId, out var registration))
			return;

		try { registration.source.Cancel(); }
		catch (ObjectDisposedException) { }

		registration.source.Dispose();
	}

	private void CancelAllTimeouts()
	{
		foreach (var (source, _) in _timeoutSources.Values)
		{
			try { source.Cancel(); }
			catch (ObjectDisposedException) { }

			source.Dispose();
		}

		_timeoutSources.Clear();
	}

	/// <inheritdoc />
	public override void Dispose()
	{
		using (_timeoutSync.EnterScope())
		{
			if (_isDisposed)
				return;

			_isDisposed = true;
			_state.Clear();
			CancelAllTimeouts();
		}

		base.Dispose();
	}

	/// <summary>
	/// Create a copy of <see cref="LookupTrackingMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
		=> new LookupTrackingMessageAdapter(InnerAdapter.TypedClone(), _state.GetType().CreateInstance<ILookupTrackingManagerState>()) { _timeOut = _timeOut };
}
