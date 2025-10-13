namespace StockSharp.Messages;

/// <summary>
/// Interface, described snapshots holder.
/// </summary>
/// <typeparam name="TMessage">Message type.</typeparam>
public interface ISnapshotHolder<TMessage>
	where TMessage : Message
{
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
	public bool TryGetSnapshot(SecurityId securityId, out Level1ChangeMessage snapshot)
	{
		lock (_snapshots.SyncRoot)
		{
			snapshot = null;

			if (!_snapshots.TryGetValue(securityId, out var s))
				return false;

			snapshot = s.TypedClone();
			return true;
		}
	}

	/// <inheritdoc />
	public Level1ChangeMessage Process(Level1ChangeMessage level1Msg)
	{
		if (level1Msg is null)
			throw new ArgumentNullException(nameof(level1Msg));

		var secId = level1Msg.SecurityId;

		lock (_snapshots.SyncRoot)
		{
			if (_snapshots.TryGetValue(secId, out var snapshot))
			{
				var diff = new Level1ChangeMessage
				{
					SecurityId = secId,
					ServerTime = level1Msg.ServerTime,
					LocalTime = level1Msg.LocalTime,
					BuildFrom = level1Msg.BuildFrom,
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

	/// <summary>
	/// Try get current error counter for the specified security.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <returns>
	/// Error counter value if snapshot exists; otherwise <see langword="null"/>.
	/// </returns>
	public int? GetErrorCount(SecurityId securityId)
	{
		lock (_snapshots.SyncRoot)
			return _snapshots.TryGetValue(securityId, out var s) ? s.ErrorCount : null;
	}

	/// <inheritdoc />
	public bool TryGetSnapshot(SecurityId securityId, out QuoteChangeMessage snapshot)
	{
		lock (_snapshots.SyncRoot)
		{
			snapshot = null;

			if (!_snapshots.TryGetValue(securityId, out var s))
				return false;

			snapshot = s.Snapshot.TypedClone();
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

		lock (_snapshots.SyncRoot)
		{
			if (quoteMsg.State is null)
			{
				var snapshot = quoteMsg.TypedClone();
				snapshot.State = QuoteChangeStates.SnapshotComplete;

				if (_snapshots.TryGetValue(secId, out var info))
				{
					try
					{
						var delta = info.Snapshot.GetDelta(quoteMsg);

						info.Snapshot = snapshot;
						info.ErrorCount = 0;

						// reinitialize builder state to the new full snapshot
						var applied = info.Builder.TryApply(snapshot)
							?? throw new InvalidOperationException();

						result = delta;
					}
					catch (Exception ex)
					{
						if (++info.ErrorCount == _maxError)
						{
							logTurnedOff = true;
							logErrorCount = info.ErrorCount;
						}

						toThrow = new InvalidOperationException(LocalizedStrings.MessageWithError.Put(quoteMsg), ex);
					}
				}
				else
				{
					var builder = new OrderBookIncrementBuilder(secId) { Parent = this };

					if (builder.TryApply(snapshot) is null)
						toThrow = new InvalidOperationException();
					else
					{
						_snapshots.Add(secId, new() { Snapshot = snapshot, Builder = builder });
						result = snapshot.TypedClone();
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

							// reset error count (no exception)
							info.ErrorCount = 0;

							if (snapshot is null)
							{
								result = null;
							}
							else
							{
								snapshot.State = QuoteChangeStates.SnapshotComplete;
								info.Snapshot = snapshot;
								result = quoteMsg;
							}
						}
						catch (Exception ex)
						{
							if (++info.ErrorCount == _maxError)
							{
								logTurnedOff = true;
								logErrorCount = info.ErrorCount;
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

						result = snapshot;
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