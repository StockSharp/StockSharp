namespace StockSharp.Algo.Strategies;

/// <summary>
/// Conditions of subsidiary strategies operation end.
/// </summary>
[Obsolete("Child strategies no longer supported.")]
public enum BasketStrategyFinishModes
{
	/// <summary>
	/// If at least one strategy ended.
	/// </summary>
	First,

	/// <summary>
	/// If all strategies ended.
	/// </summary>
	All,

	/// <summary>
	/// Subsidiary strategies do not depend on each other.
	/// </summary>
	None,
}

/// <summary>
/// The batch strategy, containing subsidiary strategies, affecting each other by their execution.
/// </summary>
[Obsolete("Child strategies no longer supported.")]
public class BasketStrategy : Strategy
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BasketStrategy"/>.
	/// </summary>
	/// <param name="finishMode">The condition of subsidiary strategies operation end.</param>
	public BasketStrategy(BasketStrategyFinishModes finishMode)
	{
		FinishMode = finishMode;

		if (FinishMode != BasketStrategyFinishModes.None)
			ChildStrategies.Added += OnChildStrategiesAdded;
	}

	/// <summary>
	/// The condition of subsidiary strategies operation end.
	/// </summary>
	public BasketStrategyFinishModes FinishMode { get; }

	/// <summary>
	/// First stopped subsidiary strategy. The property is filled at <see cref="FinishMode"/> equals to <see cref="BasketStrategyFinishModes.First"/>.
	/// </summary>
	public Strategy FirstFinishStrategy { get; private set; }

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		if (FinishMode != BasketStrategyFinishModes.None && ChildStrategies.Count == 0)
			throw new InvalidOperationException(LocalizedStrings.NoChildStrategies);

		base.OnStarted(time);
	}

	private void OnChildStrategiesAdded(Strategy strategy)
	{
		var rule = strategy
			.WhenStopped()
			.Do(s =>
			{
				if (FinishMode == BasketStrategyFinishModes.First)
				{
					if (FirstFinishStrategy == null)
					{
						FirstFinishStrategy = s;
						Stop();
					}
				}
				else
				{
					if (ChildStrategies.All(child => child.ProcessState != ProcessStates.Started))
						Stop();
				}
			})
			.Once()
			.Apply(this);

		rule.UpdateName(rule.Name + $" ({nameof(BasketStrategy)}.{nameof(OnChildStrategiesAdded)})");
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		if (FinishMode != BasketStrategyFinishModes.None)
			ChildStrategies.Added -= OnChildStrategiesAdded;

		base.DisposeManaged();
	}
}