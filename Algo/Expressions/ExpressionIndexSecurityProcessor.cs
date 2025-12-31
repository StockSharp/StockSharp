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
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		if (values.Length != BasketLegs.Length)
			throw new ArgumentOutOfRangeException(nameof(values));

		var formula = BasketSecurity.Formula ?? throw new InvalidOperationException("Formula is not set.");
		return formula.Calculate(values);
	}
}