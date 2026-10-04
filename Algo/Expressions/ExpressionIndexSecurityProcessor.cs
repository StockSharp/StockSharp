namespace StockSharp.Algo.Expressions;

using Ecng.Compilation.Expressions;

/// <summary>
/// Index securities processor for <see cref="ExpressionIndexSecurity"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ExpressionIndexSecurityProcessor"/>.
/// </remarks>
/// <param name="basketSecurity">The index, built of combination of several instruments through mathematical formula <see cref="ExpressionIndexSecurity.Expression"/>.</param>
public class ExpressionIndexSecurityProcessor(Security basketSecurity) : IndexSecurityBaseProcessor<ExpressionIndexSecurity>(basketSecurity)
{
	private ExpressionFormula<decimal> _formula;

	/// <inheritdoc />
	public override async ValueTask InitAsync(CancellationToken cancellationToken)
	{
		await base.InitAsync(cancellationToken);

		var formula = await BasketSecurity.GetFormulaAsync(cancellationToken);

		if (!formula.Error.IsEmpty())
			throw new InvalidOperationException(formula.Error);

		_formula = formula;
	}

	/// <inheritdoc />
	protected override decimal OnCalculate(decimal[] values)
	{
		if (values is null)
			throw new ArgumentNullException(nameof(values));

		if (values.Length != BasketLegs.Length)
			throw new ArgumentOutOfRangeException(nameof(values));

		var formula = _formula ?? throw new InvalidOperationException("Formula is not set.");
		return formula.Calculate(values);
	}
}