namespace StockSharp.Algo.Strategies.Optimization;

/// <summary>
/// Provider for compiling fitness formulas into evaluation functions.
/// </summary>
public interface IFitnessFormulaProvider
{
	/// <summary>
	/// Compile a fitness formula string into an evaluation function.
	/// </summary>
	/// <param name="formula">The formula string (e.g., "PnL", "PnL * SharpeRatio").</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>A function that evaluates a strategy and returns a fitness value.</returns>
	/// <exception cref="ArgumentNullException">When formula is null or empty.</exception>
	/// <exception cref="InvalidOperationException">When compilation fails.</exception>
	ValueTask<Func<Strategy, decimal>> CompileAsync(string formula, CancellationToken cancellationToken);
}
