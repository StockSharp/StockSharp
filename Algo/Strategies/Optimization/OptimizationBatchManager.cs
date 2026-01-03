namespace StockSharp.Algo.Strategies.Optimization;

/// <summary>
/// Manages batch execution of optimization iterations.
/// </summary>
public interface IOptimizationBatchManager
{
	/// <summary>
	/// Maximum concurrent iterations (batch size).
	/// </summary>
	int BatchSize { get; }

	/// <summary>
	/// Number of currently running iterations.
	/// </summary>
	int RunningCount { get; }

	/// <summary>
	/// Whether can start a new iteration.
	/// </summary>
	bool CanStartNext { get; }

	/// <summary>
	/// Whether all iterations are complete.
	/// </summary>
	bool IsFinished { get; }

	/// <summary>
	/// Register a new running iteration.
	/// </summary>
	/// <param name="iterationId">Unique iteration identifier.</param>
	void RegisterRunning(Guid iterationId);

	/// <summary>
	/// Mark iteration as completed and optionally trigger next.
	/// </summary>
	/// <param name="iterationId">Iteration identifier.</param>
	/// <returns>True if should start next iteration.</returns>
	bool CompleteIteration(Guid iterationId);

	/// <summary>
	/// Reset manager state.
	/// </summary>
	/// <param name="batchSize">Batch size.</param>
	/// <param name="totalIterations">Total iterations count.</param>
	void Reset(int batchSize, int totalIterations);
}

/// <summary>
/// Default implementation of <see cref="IOptimizationBatchManager"/>.
/// </summary>
public class OptimizationBatchManager : IOptimizationBatchManager
{
	private readonly Lock _sync = new();
	private readonly HashSet<Guid> _running = [];

	private int _batchSize;
	private int _totalIterations;
	private int _startedCount;

	/// <inheritdoc />
	public int BatchSize
	{
		get
		{
			using (_sync.EnterScope())
				return _batchSize;
		}
	}

	/// <inheritdoc />
	public int RunningCount
	{
		get
		{
			using (_sync.EnterScope())
				return _running.Count;
		}
	}

	/// <summary>
	/// Number of iterations that have been started.
	/// </summary>
	public int StartedCount
	{
		get
		{
			using (_sync.EnterScope())
				return _startedCount;
		}
	}

	/// <summary>
	/// Number of remaining iterations to start.
	/// </summary>
	public int RemainingToStart
	{
		get
		{
			using (_sync.EnterScope())
				return Math.Max(0, _totalIterations - _startedCount);
		}
	}

	/// <inheritdoc />
	public bool CanStartNext
	{
		get
		{
			using (_sync.EnterScope())
				return _running.Count < _batchSize && _startedCount < _totalIterations;
		}
	}

	/// <inheritdoc />
	public bool IsFinished
	{
		get
		{
			using (_sync.EnterScope())
				return _startedCount >= _totalIterations && _running.Count == 0;
		}
	}

	/// <inheritdoc />
	public void RegisterRunning(Guid iterationId)
	{
		using (_sync.EnterScope())
		{
			if (!_running.Add(iterationId))
				throw new InvalidOperationException($"Iteration {iterationId} already registered.");

			_startedCount++;
		}
	}

	/// <inheritdoc />
	public bool CompleteIteration(Guid iterationId)
	{
		using (_sync.EnterScope())
		{
			if (!_running.Remove(iterationId))
				throw new InvalidOperationException($"Iteration {iterationId} not found in running set.");

			// Should start next if there's room in batch and more iterations to go
			return _running.Count < _batchSize && _startedCount < _totalIterations;
		}
	}

	/// <inheritdoc />
	public void Reset(int batchSize, int totalIterations)
	{
		if (batchSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(batchSize));

		if (totalIterations < 0)
			throw new ArgumentOutOfRangeException(nameof(totalIterations));

		using (_sync.EnterScope())
		{
			_batchSize = batchSize;
			_totalIterations = totalIterations;
			_startedCount = 0;
			_running.Clear();
		}
	}

	/// <summary>
	/// Try to reserve a slot for a new iteration.
	/// </summary>
	/// <param name="iterationId">Output: new iteration ID if reserved.</param>
	/// <returns>True if slot was reserved.</returns>
	public bool TryReserveSlot(out Guid iterationId)
	{
		using (_sync.EnterScope())
		{
			if (_running.Count >= _batchSize || _startedCount >= _totalIterations)
			{
				iterationId = default;
				return false;
			}

			iterationId = Guid.NewGuid();
			_running.Add(iterationId);
			_startedCount++;
			return true;
		}
	}
}
