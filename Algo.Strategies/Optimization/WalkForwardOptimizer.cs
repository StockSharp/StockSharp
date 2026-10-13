namespace StockSharp.Algo.Strategies.Optimization;

/// <summary>
/// One step of a walk-forward run: a stretch the parameters are fitted on, and the stretch right after it
/// they are tested on.
/// </summary>
/// <param name="InSampleFrom">Start of the stretch the parameters are fitted on.</param>
/// <param name="InSampleTo">End of the stretch the parameters are fitted on.</param>
/// <param name="OutOfSampleFrom">Start of the stretch the fitted parameters are tested on.</param>
/// <param name="OutOfSampleTo">End of the stretch the fitted parameters are tested on.</param>
public sealed record WalkForwardWindow(DateTime InSampleFrom, DateTime InSampleTo, DateTime OutOfSampleFrom, DateTime OutOfSampleTo)
{
	/// <summary>
	/// Cuts a range into consecutive windows that step forward by the out-of-sample length.
	/// </summary>
	/// <param name="from">Start of the range.</param>
	/// <param name="to">End of the range.</param>
	/// <param name="inSample">Length of the stretch each window is fitted on.</param>
	/// <param name="outOfSample">Length of the stretch each window is tested on, and the step between windows.</param>
	/// <returns>The windows, oldest first; none when the range is shorter than one window.</returns>
	public static IReadOnlyList<WalkForwardWindow> Split(DateTime from, DateTime to, TimeSpan inSample, TimeSpan outOfSample)
	{
		if (inSample <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(inSample), inSample, LocalizedStrings.InvalidValue);

		if (outOfSample <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(outOfSample), outOfSample, LocalizedStrings.InvalidValue);

		var windows = new List<WalkForwardWindow>();

		for (var start = from; start + inSample + outOfSample <= to; start += outOfSample)
			windows.Add(new(start, start + inSample, start + inSample, start + inSample + outOfSample));

		return windows;
	}
}

/// <summary>
/// What one walk-forward window came to.
/// </summary>
/// <param name="Window">The window.</param>
/// <param name="Parameters">The parameter values the in-sample search chose, by parameter id.</param>
/// <param name="InSampleFitness">The fitness the chosen values scored on the in-sample stretch.</param>
/// <param name="OutOfSample">The strategy run with the chosen values over the out-of-sample stretch.</param>
public sealed record WalkForwardStep(
	WalkForwardWindow Window,
	IReadOnlyDictionary<string, object> Parameters,
	decimal InSampleFitness,
	Strategy OutOfSample);

/// <summary>
/// Walk-forward optimization: the parameters are searched on one stretch of history and the best of them
/// tested on the stretch after it, window by window, so every out-of-sample result comes from values chosen
/// without seeing it.
/// </summary>
/// <param name="optimizer">The search run on each in-sample stretch, and the runner of each out-of-sample one.</param>
public class WalkForwardOptimizer(BruteForceOptimizer optimizer)
{
	private readonly BruteForceOptimizer _optimizer = optimizer ?? throw new ArgumentNullException(nameof(optimizer));

	/// <summary>
	/// Runs the windows in order.
	/// </summary>
	/// <param name="strategy">The strategy whose copies are run.</param>
	/// <param name="parameters">The parameters to search, with their optimization ranges.</param>
	/// <param name="windows">The windows to run.</param>
	/// <param name="fitness">Scores a strategy after its in-sample run; the highest score is chosen.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>One step per window that had anything to choose from.</returns>
	/// <remarks>
	/// Settings that score the same are told apart by their values, so the same history always chooses the
	/// same setting whatever order the runs finish in.
	/// </remarks>
	public async IAsyncEnumerable<WalkForwardStep> RunAsync(
		Strategy strategy,
		IStrategyParam[] parameters,
		IEnumerable<WalkForwardWindow> windows,
		Func<Strategy, decimal> fitness,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (windows is null)
			throw new ArgumentNullException(nameof(windows));

		if (fitness is null)
			throw new ArgumentNullException(nameof(fitness));

		foreach (var window in windows)
		{
			(Dictionary<string, object> Values, string Key, decimal Fitness)? best = null;

			await foreach (var (tried, triedParams) in _optimizer
				.RunAsync(window.InSampleFrom, window.InSampleTo, strategy.ToBruteForceAsync(parameters, out _, out _), cancellationToken)
				.WithCancellation(cancellationToken))
			{
				var values = triedParams.ToDictionary(p => p.Id, p => p.Value);
				var key = string.Join(";", values.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key}={p.Value}"));
				var score = fitness(tried);

				if (best is not { } current || score > current.Fitness || (score == current.Fitness && string.CompareOrdinal(key, current.Key) < 0))
					best = (values, key, score);
			}

			if (best is not { } chosen)
				continue;

			Strategy outOfSample;

			using (new Scope<StrategyContext>(new() { ExcludeUI = true }))
				outOfSample = await strategy.CloneAsync(cancellationToken);

			var chosenParams = new List<IStrategyParam>();

			foreach (var (id, value) in chosen.Values)
			{
				var param = outOfSample.Parameters[id];
				param.Value = value;
				chosenParams.Add(param);
			}

			Strategy tested = null;

			await foreach (var (run, _) in _optimizer
				.RunAsync(window.OutOfSampleFrom, window.OutOfSampleTo, [(outOfSample, chosenParams.ToArray())], cancellationToken)
				.WithCancellation(cancellationToken))
			{
				tested = run;
			}

			yield return new(window, chosen.Values, chosen.Fitness, tested ?? outOfSample);
		}
	}
}
