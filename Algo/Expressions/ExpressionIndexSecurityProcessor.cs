namespace StockSharp.Algo.Expressions;

/// <summary>
/// Index securities processor for <see cref="ExpressionIndexSecurity"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExpressionIndexSecurityProcessor"/>.
/// </remarks>
/// <param name="basketSecurity">The index, built of combination of several instruments through mathematical formula <see cref="ExpressionIndexSecurity.Expression"/>.</param>
public class ExpressionIndexSecurityProcessor(Security basketSecurity) : IndexSecurityBaseProcessor<ExpressionIndexSecurity>(basketSecurity)
{
	/// <inheritdoc />
	protected override decimal OnCalculate(decimal[] values)
	{
		return BasketSecurity.Formula.Calculate(values);
	}
}