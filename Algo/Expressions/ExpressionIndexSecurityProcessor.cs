namespace StockSharp.Algo.Expressions
{
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Index securities processor for <see cref="ExpressionIndexSecurity"/>.
	/// </summary>
	public class ExpressionIndexSecurityProcessor : IndexSecurityBaseProcessor<ExpressionIndexSecurity>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExpressionIndexSecurityProcessor"/>.
		/// </summary>
		/// <param name="basketSecurity">The index, built of combination of several instruments through mathematic formula <see cref="ExpressionIndexSecurity.Expression"/>.</param>
		public ExpressionIndexSecurityProcessor(Security basketSecurity)
			: base(basketSecurity)
		{
		}

		/// <inheritdoc />
		protected override decimal OnCalculate(decimal[] values)
		{
			return BasketSecurity.Formula.Calculate(values);
		}
	}
}