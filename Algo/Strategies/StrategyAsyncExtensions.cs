namespace StockSharp.Algo.Strategies;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

/// <summary>
/// Async extensions for <see cref="Strategy"/>.
/// </summary>
public static class StrategyAsyncExtensions
{
	/// <summary>
	/// Execute strategy.
	/// </summary>
	/// <param name="strategy"><see cref="Strategy"/>.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public static async ValueTask ExecAsync(this Strategy strategy, CancellationToken cancellationToken)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (strategy.ProcessState != ProcessStates.Stopped)
			throw new ArgumentException($"State is {strategy.ProcessState}.", nameof(strategy));

		var tcs = AsyncHelper.CreateTaskCompletionSource<ProcessStates>();

		using var _ = cancellationToken.Register(tcs.SetCanceled);

		void OnProcessStateChanged(Strategy s)
		{
			if (s == strategy && s.ProcessState == ProcessStates.Stopped)
				tcs.SetResult(s.ProcessState);
		}

		strategy.ProcessStateChanged += OnProcessStateChanged;

		try
		{
			strategy.Start();

			await tcs.Task;
		}
		finally
		{
			strategy.ProcessStateChanged -= OnProcessStateChanged;
		}
	}
}