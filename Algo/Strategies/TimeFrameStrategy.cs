namespace StockSharp.Algo.Strategies;

/// <summary>
/// Results of the trading strategy one iteration operation.
/// </summary>
[Obsolete]
public enum ProcessResults
{
	/// <summary>
	/// To continue the operation.
	/// </summary>
	Continue,

	/// <summary>
	/// To stop the strategy operation.
	/// </summary>
	Stop,
}

/// <summary>
/// The timeframe based trade strategy.
/// </summary>
[Obsolete("Use CreateTimer or StartTimer methods from Strategy instead of inheriting from TimeFrameStrategy.")]
public abstract class TimeFrameStrategy : Strategy
{
	/// <summary>
	/// Initialize <see cref="TimeFrameStrategy"/>.
	/// </summary>
	/// <param name="timeFrame">The strategy timeframe.</param>
	protected TimeFrameStrategy(TimeSpan timeFrame)
	{
		_interval = Param(nameof(Interval), timeFrame);
		_timeFrame = Param(nameof(TimeFrame), timeFrame);
	}

	private readonly StrategyParam<TimeSpan> _timeFrame;

	/// <summary>
	/// The strategy timeframe.
	/// </summary>
	public TimeSpan TimeFrame
        {
		get => _timeFrame.Value;
		set => _timeFrame.Value = value;
	}

	private readonly StrategyParam<TimeSpan> _interval;

	/// <summary>
	/// The strategy start-up interval. By default, it equals to <see cref="TimeFrame"/>.
	/// </summary>
	public TimeSpan Interval
	{
		get => _interval.Value;
		set => _interval.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		StartTimer(Interval, () =>
		{
			var result = OnProcess();

			if (result == ProcessResults.Stop)
				Stop();
		});
	}

	/// <summary>
	/// The implementation of the trade algorithm.
	/// </summary>
	/// <returns>The result of trade algorithm one iteration operation.</returns>
	protected abstract ProcessResults OnProcess();
}