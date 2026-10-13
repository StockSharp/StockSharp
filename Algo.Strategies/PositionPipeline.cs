namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.Statistics;

/// <summary>
/// Position event handling.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PositionPipeline"/>.
/// </remarks>
/// <param name="stats">Statistic manager.</param>
public class PositionPipeline(IStatisticManager stats)
{
	private IStatisticManager _stats = stats ?? throw new ArgumentNullException(nameof(stats));
	private readonly HashSet<(SecurityId secId, string pfName)> _seenPositions = [];

	/// <summary>
	/// Swap the statistic manager (used when <see cref="Strategy.StatisticManager"/> is reassigned).
	/// </summary>
	/// <param name="stats">The new statistic manager.</param>
	public void SetStatisticManager(IStatisticManager stats)
		=> _stats = stats ?? throw new ArgumentNullException(nameof(stats));

	/// <summary>
	/// Fires when a completely new position appears.
	/// </summary>
	public event Action<Position> NewPosition;

	/// <summary>
	/// Fires when an existing position changes.
	/// </summary>
	public event Action<Position> PositionChanged;

	/// <summary>
	/// Process a position update, auto-detecting new vs changed.
	/// </summary>
	/// <param name="position">The position.</param>
	public void Process(Position position)
	{
		ArgumentNullException.ThrowIfNull(position);

		var key = (position.Security.ToSecurityId(), position.PortfolioName);
		var isNew = _seenPositions.Add(key);

		Process(position, isNew);
	}

	/// <summary>
	/// Process a position update.
	/// </summary>
	/// <param name="position">The position.</param>
	/// <param name="isNew">Whether this is a new position (vs. update).</param>
	public void Process(Position position, bool isNew)
	{
		ArgumentNullException.ThrowIfNull(position);

		if (isNew)
			NewPosition?.Invoke(position);
		else
			PositionChanged?.Invoke(position);

		_stats.AddPosition(position.LocalTime, position.CurrentValue ?? 0);
	}
}
