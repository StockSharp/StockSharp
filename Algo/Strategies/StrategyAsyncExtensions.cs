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
	/// <param name="extra">Extra action.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/>.</param>
	/// <returns><see cref="ValueTask"/>.</returns>
	public static async ValueTask ExecAsync(this Strategy strategy, Action extra, CancellationToken cancellationToken)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (strategy.ProcessState != ProcessStates.Stopped)
			throw new ArgumentException($"State is {strategy.ProcessState}.", nameof(strategy));

		await Task.Yield();

		var tcs = AsyncHelper.CreateTaskCompletionSource<ProcessStates>();
		var wasCancelled = false;

		using var _ = cancellationToken.Register(() =>
		{
			wasCancelled = tcs.TrySetCanceled();
		});

		void OnProcessStateChanged(Strategy s)
		{
			if (s == strategy && s.ProcessState == ProcessStates.Stopped)
			{
				if (s.LastError is null)
					tcs.TrySetResult(s.ProcessState);
				else
					tcs.TrySetException(s.LastError);
			}
		}

		strategy.ProcessStateChanged += OnProcessStateChanged;

		try
		{
			strategy.Start();

			extra?.Invoke();

			await tcs.Task;
		}
		finally
		{
			if (wasCancelled)
			{
				strategy.Stop();

				// TODO SS-274 correct stopping awaiting
			}

			strategy.ProcessStateChanged -= OnProcessStateChanged;
		}
	}
}