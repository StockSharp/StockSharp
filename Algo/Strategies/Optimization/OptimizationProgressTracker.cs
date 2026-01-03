namespace StockSharp.Algo.Strategies.Optimization;

/// <summary>
/// Manages optimization progress tracking.
/// </summary>
public interface IOptimizationProgressTracker
{
	/// <summary>
	/// Total number of iterations.
	/// </summary>
	int TotalIterations { get; }

	/// <summary>
	/// Number of completed iterations.
	/// </summary>
	int CompletedIterations { get; }

	/// <summary>
	/// Current total progress (0-100).
	/// </summary>
	int TotalProgress { get; }

	/// <summary>
	/// Time when optimization started.
	/// </summary>
	DateTime StartedAt { get; }

	/// <summary>
	/// Elapsed time since start.
	/// </summary>
	TimeSpan Elapsed { get; }

	/// <summary>
	/// Estimated remaining time.
	/// </summary>
	TimeSpan Remaining { get; }

	/// <summary>
	/// Mark an iteration as started.
	/// </summary>
	void IterationStarted();

	/// <summary>
	/// Mark an iteration as completed.
	/// </summary>
	void IterationCompleted();

	/// <summary>
	/// Reset tracker state.
	/// </summary>
	/// <param name="totalIterations">Total number of iterations.</param>
	void Reset(int totalIterations);
}

/// <summary>
/// Default implementation of <see cref="IOptimizationProgressTracker"/>.
/// </summary>
public class OptimizationProgressTracker : IOptimizationProgressTracker
{
	private readonly Lock _sync = new();
	private int _totalIterations;
	private int _completedIterations;
	private DateTime _startedAt;

	/// <inheritdoc />
	public int TotalIterations
	{
		get
		{
			using (_sync.EnterScope())
				return _totalIterations;
		}
	}

	/// <inheritdoc />
	public int CompletedIterations
	{
		get
		{
			using (_sync.EnterScope())
				return _completedIterations;
		}
	}

	/// <inheritdoc />
	public int TotalProgress
	{
		get
		{
			using (_sync.EnterScope())
			{
				if (_totalIterations <= 0)
					return 0;

				return Math.Min(100, (int)(_completedIterations * 100.0 / _totalIterations));
			}
		}
	}

	/// <inheritdoc />
	public DateTime StartedAt
	{
		get
		{
			using (_sync.EnterScope())
				return _startedAt;
		}
	}

	/// <inheritdoc />
	public TimeSpan Elapsed => DateTime.UtcNow - StartedAt;

	/// <inheritdoc />
	public TimeSpan Remaining
	{
		get
		{
			var progress = TotalProgress;

			if (progress <= 0)
				return TimeSpan.MaxValue;

			var elapsed = Elapsed;
			return elapsed * 100.0 / progress - elapsed;
		}
	}

	/// <inheritdoc />
	public void IterationStarted()
	{
		// Currently just a marker, could track running count
	}

	/// <inheritdoc />
	public void IterationCompleted()
	{
		using (_sync.EnterScope())
			_completedIterations++;
	}

	/// <inheritdoc />
	public void Reset(int totalIterations)
	{
		if (totalIterations < 0)
			throw new ArgumentOutOfRangeException(nameof(totalIterations));

		using (_sync.EnterScope())
		{
			_totalIterations = totalIterations;
			_completedIterations = 0;
			_startedAt = DateTime.UtcNow;
		}
	}
}
