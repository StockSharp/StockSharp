namespace StockSharp.Messages;

/// <summary>
/// <see cref="PositionChangeMessage"/> snapshots holder.
/// </summary>
/// <remarks>
/// Returns the same reference for the same position key (PortfolioName + SecurityId + StrategyId + Side + ClientCode + DepoName + LimitType).
/// </remarks>
public class PositionSnapshotHolder : BaseLogReceiver
{
	private readonly SynchronizedDictionary<(string, SecurityId, string, Sides?, string, string, TPlusLimits?), PositionChangeMessage> _snapshots = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionSnapshotHolder"/>.
	/// </summary>
	public PositionSnapshotHolder()
	{
	}

	/// <summary>
	/// Try get snapshot for the specified position key.
	/// </summary>
	/// <param name="portfolioName">Portfolio name.</param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="strategyId">Strategy ID.</param>
	/// <param name="side">Side.</param>
	/// <param name="clientCode">Client code.</param>
	/// <param name="depoName">Depo name.</param>
	/// <param name="limitType">Limit type.</param>
	/// <param name="snapshot">Snapshot if exists, otherwise <see langword="null"/>.</param>
	/// <returns><c>true</c> if snapshot exists; otherwise <c>false</c>.</returns>
	public bool TryGetSnapshot(string portfolioName, SecurityId securityId, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limitType, out PositionChangeMessage snapshot)
	{
		var key = CreateKey(portfolioName, securityId, strategyId, side, clientCode, depoName, limitType);
		return _snapshots.TryGetValue(key, out snapshot);
	}

	/// <summary>
	/// Try get snapshot for the specified position message.
	/// </summary>
	/// <param name="posMsg">Position message to extract key from.</param>
	/// <param name="snapshot">Snapshot if exists, otherwise <see langword="null"/>.</param>
	/// <returns><c>true</c> if snapshot exists; otherwise <c>false</c>.</returns>
	public bool TryGetSnapshot(PositionChangeMessage posMsg, out PositionChangeMessage snapshot)
	{
		if (posMsg is null)
			throw new ArgumentNullException(nameof(posMsg));

		return _snapshots.TryGetValue(CreateKey(posMsg), out snapshot);
	}

	/// <summary>
	/// Process <see cref="PositionChangeMessage"/> change.
	/// </summary>
	/// <param name="posMsg"><see cref="PositionChangeMessage"/> change.</param>
	/// <returns>
	/// Position snapshot. Returns the same reference for the same position key.
	/// </returns>
	public PositionChangeMessage Process(PositionChangeMessage posMsg)
	{
		if (posMsg is null)
			throw new ArgumentNullException(nameof(posMsg));

		var key = CreateKey(posMsg);

		using (_snapshots.EnterScope())
		{
			if (_snapshots.TryGetValue(key, out var snapshot))
			{
				ApplyChanges(snapshot, posMsg);
				return snapshot;
			}
			else
			{
				snapshot = (PositionChangeMessage)posMsg.Clone();
				_snapshots.Add(key, snapshot);
				return snapshot;
			}
		}
	}

	private static void ApplyChanges(PositionChangeMessage snapshot, PositionChangeMessage posMsg)
	{
		foreach (var change in posMsg.Changes)
			snapshot.Changes[change.Key] = change.Value;

		if (posMsg.ServerTime != default)
			snapshot.ServerTime = posMsg.ServerTime;

		if (posMsg.LocalTime != default)
			snapshot.LocalTime = posMsg.LocalTime;

		if (!posMsg.Description.IsEmpty())
			snapshot.Description = posMsg.Description;

		if (!posMsg.BoardCode.IsEmpty())
			snapshot.BoardCode = posMsg.BoardCode;
	}

	/// <summary>
	/// Reset snapshot for the specified position key.
	/// </summary>
	/// <param name="portfolioName">Portfolio name. Use <see langword="null"/> to clear all snapshots.</param>
	/// <param name="securityId">Security ID.</param>
	/// <param name="strategyId">Strategy ID.</param>
	/// <param name="side">Side.</param>
	/// <param name="clientCode">Client code.</param>
	/// <param name="depoName">Depo name.</param>
	/// <param name="limitType">Limit type.</param>
	public void ResetSnapshot(string portfolioName, SecurityId securityId = default, string strategyId = null, Sides? side = null, string clientCode = null, string depoName = null, TPlusLimits? limitType = null)
	{
		if (portfolioName is null)
			_snapshots.Clear();
		else
			_snapshots.Remove(CreateKey(portfolioName, securityId, strategyId, side, clientCode, depoName, limitType));
	}

	private static (string, SecurityId, string, Sides?, string, string, TPlusLimits?) CreateKey(PositionChangeMessage posMsg)
		=> CreateKey(posMsg.PortfolioName, posMsg.SecurityId, posMsg.StrategyId, posMsg.Side, posMsg.ClientCode, posMsg.DepoName, posMsg.LimitType);

	private static (string, SecurityId, string, Sides?, string, string, TPlusLimits?) CreateKey(string portfolioName, SecurityId securityId, string strategyId, Sides? side, string clientCode, string depoName, TPlusLimits? limitType)
		=> (
			portfolioName?.ToLowerInvariant() ?? string.Empty,
			securityId,
			strategyId?.ToLowerInvariant() ?? string.Empty,
			side,
			clientCode?.ToLowerInvariant() ?? string.Empty,
			depoName?.ToLowerInvariant() ?? string.Empty,
			limitType
		);
}
