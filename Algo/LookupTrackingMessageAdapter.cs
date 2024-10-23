namespace StockSharp.Algo;

/// <summary>
/// Message adapter that tracks multiple lookups requests and put them into single queue.
/// </summary>
public class LookupTrackingMessageAdapter : MessageAdapterWrapper
{
	private class LookupInfo
	{
		private readonly TimeSpan _initLeft;
		private TimeSpan _left;

		public LookupInfo(ISubscriptionMessage subscription, TimeSpan left)
		{
			Subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
			_initLeft = left;
			_left = left;
		}

		public ISubscriptionMessage Subscription { get; }

		public bool ProcessTime(TimeSpan diff)
		{
			try
			{
				if (diff <= TimeSpan.Zero)
					return false;

				var left = _left - diff;

				if (left <= TimeSpan.Zero)
					return true;

				_left = left;
				return false;
			}
			catch (OverflowException ex)
			{
				throw new InvalidOperationException($"Left='{_left}' Diff='{diff}'", ex);
			}
		}

		public void IncreaseTimeOut()
		{
			_left = _initLeft;
		}
	}

	private readonly CachedSynchronizedDictionary<long, LookupInfo> _lookups = [];
	private readonly Dictionary<MessageTypes, Dictionary<long, ISubscriptionMessage>> _queue = [];
	private DateTimeOffset _prevTime;

	/// <summary>
	/// Initializes a new instance of the <see cref="LookupTrackingMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Inner message adapter.</param>
	public LookupTrackingMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
	}

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
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				lock (_lookups.SyncRoot)
				{
					_prevTime = default;
					_lookups.Clear();
					_queue.Clear();
				}

				break;
			}

			default:
				if (message is ISubscriptionMessage subscrMsg && !ProcessLookupMessage(subscrMsg))
					return true;

				break;
		}

		return base.OnSendInMessage(message);
	}

	private bool ProcessLookupMessage(ISubscriptionMessage message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		if (message is OrderStatusMessage orderMsg && orderMsg.HasOrderId())
			return true;

		var transId = message.TransactionId;

		var isEnqueue = false;
		var isStarted = false;

		try
		{
			if (message.IsSubscribe)
			{
				lock (_lookups.SyncRoot)
				{
					if (InnerAdapter.EnqueueSubscriptions)
					{
						var queue = _queue.SafeAdd(message.Type);

						// not prev queued lookup
						if (queue.TryAdd2(transId, message.TypedClone()))
						{
							if (queue.Count > 1)
							{
								isEnqueue = true;
								return false;
							}
						}
					}

					if (!this.IsResultMessageSupported(message.Type) && TimeOut > TimeSpan.Zero)
					{
						_lookups.Add(transId, new LookupInfo(message.TypedClone(), TimeOut));
						isStarted = true;
					}
				}
			}

			return true;
		}
		finally
		{
			if (isEnqueue)
				this.AddInfoLog("Lookup queued {0}.", message);

			if (isStarted)
				this.AddInfoLog("Lookup timeout {0} started for {1}.", TimeOut, transId);
		}
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		base.OnInnerAdapterNewOutMessage(message);

		Message TryInitNextLookup(MessageTypes type, long removingId)
		{
			if (!_queue.TryGetValue(type, out var queue) || !queue.Remove(removingId))
				return null;

			if (queue.Count == 0)
			{
				_queue.Remove(type);
				return null;
			}

			return (Message)queue.First().Value;
		}

		long[] ignoreIds = null;
		Message nextLookup = null;

		if (message is IOriginalTransactionIdMessage originIdMsg)
		{
			if (originIdMsg is SubscriptionFinishedMessage ||
			    originIdMsg is SubscriptionOnlineMessage ||
			    originIdMsg is SubscriptionResponseMessage resp && !resp.IsOk())
			{
				var id = originIdMsg.OriginalTransactionId;

				lock (_lookups.SyncRoot)
				{
					if (_lookups.TryGetAndRemove(id, out var info))
					{
						this.AddInfoLog("Lookup response {0}.", id);

						nextLookup = TryInitNextLookup(info.Subscription.Type, info.Subscription.TransactionId);
					}
					else
					{
						foreach (var type in _queue.Keys.ToArray())
						{
							nextLookup = TryInitNextLookup(type, id);

							if (nextLookup != null)
								break;
						}
					}
				}
			}
			else if (message is ISubscriptionIdMessage subscrMsg)
			{
				ignoreIds = subscrMsg.GetSubscriptionIds();

				lock (_lookups.SyncRoot)
				{
					foreach (var id in ignoreIds)
					{
						if (_lookups.TryGetValue(id, out var info))
							info.IncreaseTimeOut();
					}
				}
			}
		}

		if (nextLookup != null)
		{
			nextLookup.LoopBack(this);
			base.OnInnerAdapterNewOutMessage(nextLookup);
		}

		if (message.LocalTime == default)
			return;

		List<Message> nextLookups = null;

		if(_prevTime == default)
		{
			_prevTime = message.LocalTime;
		}
		else if (message.LocalTime > _prevTime)
		{
			var diff = message.LocalTime - _prevTime;
			_prevTime = message.LocalTime;

			foreach (var pair in _lookups.CachedPairs)
			{
				var info = pair.Value;
				var transId = info.Subscription.TransactionId;

				if (ignoreIds != null && ignoreIds.Contains(transId))
					continue;

				if (!info.ProcessTime(diff))
					continue;

				_lookups.Remove(transId);
				this.AddInfoLog("Lookup timeout {0}.", transId);

				base.OnInnerAdapterNewOutMessage(info.Subscription.CreateResult());

				if (nextLookups == null)
					nextLookups = [];

				lock (_lookups.SyncRoot)
				{
					var next = TryInitNextLookup(info.Subscription.Type, info.Subscription.TransactionId);

					if (next != null)
						nextLookups.Add(next);
				}
			}
		}

		if (nextLookups != null)
		{
			foreach (var lookup in nextLookups)
			{
				lookup.LoopBack(this);
				base.OnInnerAdapterNewOutMessage(lookup);
			}
		}
	}

	/// <summary>
	/// Create a copy of <see cref="LookupTrackingMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new LookupTrackingMessageAdapter(InnerAdapter.TypedClone()) { _timeOut = _timeOut };
	}
}