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
	/// <param name="needResponse">Need response value.</param>
	/// <returns><typeref name="TMessage"/> change.</returns>
	TMessage Process(TMessage change, bool needResponse);

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
		snapshot = null;

		if (!_snapshots.TryGetValue(securityId, out var s))
			return false;

		snapshot = s.TypedClone();
        return true;
    }

	/// <inheritdoc />
	public Level1ChangeMessage Process(Level1ChangeMessage level1Msg, bool needResponse)
	{
		if (level1Msg is null)
			throw new ArgumentNullException(nameof(level1Msg));

		var secId = level1Msg.SecurityId;

		if (_snapshots.TryGetValue(secId, out var snapshot))
		{
			var diff = needResponse ? new Level1ChangeMessage
			{
				SecurityId = secId,
				ServerTime = level1Msg.ServerTime,
				LocalTime = level1Msg.LocalTime,
				BuildFrom = level1Msg.BuildFrom,
			} : null;

			var changes = snapshot.Changes;

			foreach (var change in level1Msg.Changes)
			{
				if (changes.TryGetValue(change.Key, out var prevValue))
				{
					if (prevValue?.Equals(change.Value) != true)
					{
						changes[change.Key] = change.Value;
						diff?.Changes.Add(change);
					}
				}
				else
				{
					changes.Add(change);
					diff?.Changes.Add(change);
				}
			}

			snapshot.LocalTime = level1Msg.LocalTime;
			snapshot.ServerTime = level1Msg.ServerTime;

			return diff;
		}
		else
		{
			LogDebug(LocalizedStrings.SnapshotFormed, "L1", secId);

			_snapshots.Add(secId, level1Msg.TypedClone());

			return level1Msg;
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
	private const int _maxError = 100;
	private readonly SynchronizedDictionary<SecurityId, RefTriple<QuoteChangeMessage, OrderBookIncrementBuilder, int>> _snapshots = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderBookSnapshotHolder"/>.
	/// </summary>
	public OrderBookSnapshotHolder()
	{
	}

	/// <inheritdoc />
	public bool TryGetSnapshot(SecurityId securityId, out QuoteChangeMessage snapshot)
	{
		snapshot = null;

		if (!_snapshots.TryGetValue(securityId, out var s))
			return false;

		snapshot = s.First.TypedClone();
		return true;
	}

	/// <inheritdoc />
	public QuoteChangeMessage Process(QuoteChangeMessage quoteMsg, bool needResponse)
	{
		if (quoteMsg is null)
			throw new ArgumentNullException(nameof(quoteMsg));

		var secId = quoteMsg.SecurityId;

		lock (_snapshots.SyncRoot)
		{
			if (quoteMsg.State is null)
			{
				var snapshot = quoteMsg.TypedClone();
				snapshot.State = QuoteChangeStates.SnapshotComplete;

				if (_snapshots.TryGetValue(secId, out var tuple))
				{
					if (tuple.Third == _maxError)
						return null;

					try
					{
						var delta = needResponse ? tuple.First.GetDelta(quoteMsg) : null;
						tuple.First = snapshot;
						tuple.Third = 0;
						return delta;
					}
					catch (Exception ex)
					{
						if (++tuple.Third == _maxError)
						{
							LogError(LocalizedStrings.SnapshotTurnedOff, secId, tuple.Third, _maxError);
						}

						throw new InvalidOperationException(LocalizedStrings.MessageWithError.Put(quoteMsg), ex);
					}
				}
				else
				{
					LogDebug(LocalizedStrings.SnapshotFormed, "OB", secId);

					var builder = new OrderBookIncrementBuilder(secId) { Parent = this };

					if (builder.TryApply(snapshot) is null)
						throw new InvalidOperationException();

					_snapshots.Add(secId, RefTuple.Create(snapshot, builder, 0));
					return snapshot.TypedClone();
				}
			}
			else
			{
				if (_snapshots.TryGetValue(secId, out var tuple))
				{
					if (tuple.Third == _maxError)
						return null;

					try
					{
						var snapshot = tuple.Second.TryApply(quoteMsg);

						// reset error count (no exception)
						tuple.Third = 0;

						if (snapshot is null)
							return null;

						snapshot.State = QuoteChangeStates.SnapshotComplete;
						tuple.First = snapshot;
						return quoteMsg;
					}
					catch (Exception ex)
					{
						if (++tuple.Third == _maxError)
						{
							LogError(LocalizedStrings.SnapshotTurnedOff, secId, tuple.Third, _maxError);
						}

						throw new InvalidOperationException(LocalizedStrings.MessageWithError.Put(quoteMsg), ex);
					}
				}
				else
				{
					var builder = new OrderBookIncrementBuilder(secId) { Parent = this };

					var snapshot = builder.TryApply(quoteMsg);

					if (snapshot is null)
					{
						return null;
						//throw new InvalidOperationException($"First depth is not snapshot: {quoteMsg}");
					}

					snapshot.State = QuoteChangeStates.SnapshotComplete;

					LogDebug(LocalizedStrings.SnapshotFormed, "OB", secId);
					_snapshots.Add(secId, RefTuple.Create(snapshot, builder, 0));

					return snapshot;
				}
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