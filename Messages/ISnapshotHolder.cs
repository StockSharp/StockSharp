namespace StockSharp.Messages;

/// <summary>
/// Interface, described snapshots holder.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
public interface ISnapshotHolder<TMessage>
	where TMessage : Message
{
	/// <summary>
	/// Securities a snapshot is held for.
	/// </summary>
	/// <remarks>
	/// So a caller that has to walk everything held - answering a subscription for the whole
	/// market, for instance - can ask rather than keep its own list of what it has passed in. Such
	/// a list is a second copy of this one and drifts from it silently.
	/// </remarks>
	IEnumerable<SecurityId> Securities { get; }

	/// <summary>
	/// Try get snapshot for the specified security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="snapshot">Snapshot if exists, otherwise <see langword="null"/>.</param>
	/// <returns><c>true</c> if snapshot exists; otherwise <c>false</c>.</returns>
	bool TryGetSnapshot(SecurityId securityId, out TMessage snapshot);

	/// <summary>
	/// Process <typeref name="TMessage"/> change.
	/// </summary>
	/// <param name="change"><typeref name="TMessage"/> change.</param>
	/// <returns><typeref name="TMessage"/> change (diff or snapshot clone on first call).</returns>
	TMessage Process(TMessage change);

	/// <summary>
	/// Reset snapshot for the specified security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	void ResetSnapshot(SecurityId securityId);
}

/// <summary>
/// <see cref="Level1ChangeMessage"/> snapshots holder.
/// </summary>
public class Level1SnapshotHolder : BaseLogReceiver, ISnapshotHolder<Level1ChangeMessage>
{
	private readonly SynchronizedDictionary<SecurityId, Level1ChangeMessage> _snapshots = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="Level1SnapshotHolder"/>.
	/// </summary>
	public Level1SnapshotHolder()
	{
	}

	/// <inheritdoc />
	public IEnumerable<SecurityId> Securities
	{
		get
		{
			using (_snapshots.EnterScope())
				return [.. _snapshots.Keys];
		}
	}

	/// <inheritdoc />
	public bool TryGetSnapshot(SecurityId securityId, out Level1ChangeMessage snapshot)
	{
		using (_snapshots.EnterScope())
		{
			snapshot = null;

			if (!_snapshots.TryGetValue(securityId, out var s))
				return false;

			snapshot = s.TypedClone();
			snapshot.OriginalTransactionId = 0;
			snapshot.SubscriptionId = 0;
			snapshot.SubscriptionIds = [];
			return true;
		}
	}

	/// <inheritdoc />
	public Level1ChangeMessage Process(Level1ChangeMessage level1Msg)
	{
		if (level1Msg is null)
			throw new ArgumentNullException(nameof(level1Msg));

		var secId = level1Msg.SecurityId;

		using (_snapshots.EnterScope())
		{
			if (_snapshots.TryGetValue(secId, out var snapshot))
			{
				var diff = new Level1ChangeMessage
				{
					SecurityId = secId,
					ServerTime = level1Msg.ServerTime,
					LocalTime = level1Msg.LocalTime,
					BuildFrom = level1Msg.BuildFrom,
					// The diff answers the same subscription the message did. A venue that names the
					// subscription and nothing else leaves a diff without it addressed to no one.
					OriginalTransactionId = level1Msg.OriginalTransactionId,
					SubscriptionId = level1Msg.SubscriptionId,
					SubscriptionIds = level1Msg.SubscriptionIds,
				};

				var changes = snapshot.Changes;

				foreach (var change in level1Msg.Changes)
				{
					if (changes.TryGetValue(change.Key, out var prevValue))
					{
						if (!Equals(prevValue, change.Value))
						{
							changes[change.Key] = change.Value;
							diff.Changes.Add(change);
						}
					}
					else
					{
						changes.Add(change);
						diff.Changes.Add(change);
					}
				}

				snapshot.LocalTime = level1Msg.LocalTime;
				snapshot.ServerTime = level1Msg.ServerTime;

				return diff;
			}
			else
			{
				_snapshots.Add(secId, level1Msg.TypedClone());

				return level1Msg;
			}
		}
	}

	/// <inheritdoc />
	public void ResetSnapshot(SecurityId securityId)
	{
		if (securityId == default)
			_snapshots.Clear();
		else
			_snapshots.Remove(securityId);
	}
}

/// <summary>
/// <see cref="QuoteChangeMessage"/> snapshots holder.
/// </summary>
public class OrderBookSnapshotHolder : BaseLogReceiver, ISnapshotHolder<QuoteChangeMessage>
{
	private class SnapshotInfo
	{
		public QuoteChangeMessage Snapshot;
		public OrderBookIncrementBuilder Builder;
		public int ErrorCount;
	}

	private const int _maxError = 100;
	private readonly SynchronizedDictionary<SecurityId, SnapshotInfo> _snapshots = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookSnapshotHolder"/>.
	/// </summary>
	public OrderBookSnapshotHolder()
	{
	}

	/// <inheritdoc />
	public IEnumerable<SecurityId> Securities
	{
		get
		{
			using (_snapshots.EnterScope())
				return [.. _snapshots.Keys];
		}
	}

	/// <summary>
	/// Try get current error counter for the specified security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns>
	/// Error counter value if snapshot exists; otherwise <see langword="null"/>.
	/// </returns>
	public int? GetErrorCount(SecurityId securityId)
	{
		using (_snapshots.EnterScope())
			return _snapshots.TryGetValue(securityId, out var s) ? s.ErrorCount : null;
	}

	/// <inheritdoc />
	public bool TryGetSnapshot(SecurityId securityId, out QuoteChangeMessage snapshot)
	{
		using (_snapshots.EnterScope())
		{
			snapshot = null;

			if (!_snapshots.TryGetValue(securityId, out var s))
				return false;

			snapshot = s.Snapshot.TypedClone();
			snapshot.OriginalTransactionId = 0;
			snapshot.SubscriptionId = 0;
			snapshot.SubscriptionIds = [];
			return true;
		}
	}

	/// <summary>
	/// The current snapshot of <paramref name="securityId"/> as the holder keeps it, without
	/// copying it.
	/// </summary>
	/// <remarks>
	/// For readers that only look at the book: a whole clone per update is a price worth avoiding
	/// when the reader builds its own arrays anyway. Each update replaces the message and its
	/// quote arrays rather than editing them, so what is handed out stays a consistent view - but
	/// a caller must only read it, since everyone else is looking at the same instance. Use
	/// <see cref="TryGetSnapshot"/> to get one that can be modified or sent on.
	/// </remarks>
	/// <param name="securityId">Security ID.</param>
	/// <param name="snapshot">The holder's own snapshot instance.</param>
	/// <returns><see langword="true"/> if a snapshot exists.</returns>
	public bool TryPeekSnapshot(SecurityId securityId, out QuoteChangeMessage snapshot)
	{
		using (_snapshots.EnterScope())
		{
			if (!_snapshots.TryGetValue(securityId, out var s))
			{
				snapshot = null;
				return false;
			}

			snapshot = s.Snapshot;
			return true;
		}
	}

	/// <inheritdoc />
	public QuoteChangeMessage Process(QuoteChangeMessage quoteMsg)
	{
		if (quoteMsg is null)
			throw new ArgumentNullException(nameof(quoteMsg));

		var secId = quoteMsg.SecurityId;

		QuoteChangeMessage result = null;
		bool logTurnedOff = false;
		int logErrorCount = 0;
		Exception toThrow = null;

		using (_snapshots.EnterScope())
		{
			if (quoteMsg.State is null)
			{
				quoteMsg = quoteMsg.TypedClone();
				quoteMsg.State = QuoteChangeStates.SnapshotComplete;
			}

			if (quoteMsg.State == QuoteChangeStates.SnapshotComplete)
			{
				if (_snapshots.TryGetValue(secId, out var info))
				{
					try
					{
						var delta = info.Snapshot.GetDelta(quoteMsg);

						// The delta answers the same subscription the message did. A venue that
						// names the subscription and nothing else leaves a delta addressed to no one.
						delta.OriginalTransactionId = quoteMsg.OriginalTransactionId;
						delta.SubscriptionId = quoteMsg.SubscriptionId;
						delta.SubscriptionIds = quoteMsg.SubscriptionIds;

						// Validate a replacement full snapshot on a fresh builder so a failed
						// positional update cannot corrupt the active builder state.
						var builder = new OrderBookIncrementBuilder(secId) { Parent = this };
						_ = builder.TryApply(quoteMsg) ?? throw new InvalidOperationException();

						info.Snapshot = quoteMsg;
						info.Builder = builder;
						info.ErrorCount = 0;

						result = delta;
					}
					catch (Exception ex)
					{
						if (info.ErrorCount < _maxError)
						{
							info.ErrorCount++;

							if (info.ErrorCount == _maxError)
							{
								logTurnedOff = true;
								logErrorCount = info.ErrorCount;
							}
						}

						toThrow = new InvalidOperationException(LocalizedStrings.MessageWithError.Put(quoteMsg), ex);
					}
				}
				else
				{
					var builder = new OrderBookIncrementBuilder(secId) { Parent = this };

					if (builder.TryApply(quoteMsg) is null)
						toThrow = new InvalidOperationException();
					else
					{
						_snapshots.Add(secId, new() { Snapshot = quoteMsg, Builder = builder });
						result = quoteMsg.TypedClone(); // return clone for safety
					}
				}
			}
			else
			{
				if (_snapshots.TryGetValue(secId, out var info))
				{
					if (info.ErrorCount == _maxError)
					{
						result = null;
					}
					else
					{
						try
						{
							var snapshot = info.Builder.TryApply(quoteMsg);

							if (snapshot is null)
							{
								// TryApply returned null - this is an error
								if (info.ErrorCount < _maxError)
								{
									info.ErrorCount++;

									if (info.ErrorCount == _maxError)
									{
										logTurnedOff = true;
										logErrorCount = info.ErrorCount;
									}
								}

								result = null;
							}
							else
							{
								// success - reset error count
								info.ErrorCount = 0;

								snapshot.State = QuoteChangeStates.SnapshotComplete;
								info.Snapshot = snapshot;
								result = quoteMsg;
							}
						}
						catch (Exception ex)
						{
							if (info.ErrorCount < _maxError)
							{
								info.ErrorCount++;

								if (info.ErrorCount == _maxError)
								{
									logTurnedOff = true;
									logErrorCount = info.ErrorCount;
								}
							}

							toThrow = new InvalidOperationException(LocalizedStrings.MessageWithError.Put(quoteMsg), ex);
						}
					}
				}
				else
				{
					var builder = new OrderBookIncrementBuilder(secId) { Parent = this };

					var snapshot = builder.TryApply(quoteMsg);

					if (snapshot is null)
					{
						result = null;
						//throw new InvalidOperationException($"First depth is not snapshot: {quoteMsg}");
					}
					else
					{
						snapshot.State = QuoteChangeStates.SnapshotComplete;

						_snapshots.Add(secId, new() { Snapshot = snapshot, Builder = builder });

						result = snapshot.TypedClone();
					}
				}
			}
		}

		if (logTurnedOff)
			LogError(LocalizedStrings.SnapshotTurnedOff, secId, logErrorCount, _maxError);

		if (toThrow != null)
			throw toThrow;

		return result;
	}

	/// <inheritdoc />
	public void ResetSnapshot(SecurityId securityId)
	{
		if (securityId == default)
			_snapshots.Clear();
		else
			_snapshots.Remove(securityId);
	}
}
