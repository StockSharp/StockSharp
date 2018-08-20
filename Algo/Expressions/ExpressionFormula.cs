namespace StockSharp.Algo.Expressions
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Configuration;

	/// <summary>
	/// Compiled mathematical formula.
	/// </summary>
	public abstract class ExpressionFormula
	{
		/// <summary>
		/// To calculate the basket value.
		/// </summary>
		/// <param name="values">Values of basket composite instruments <see cref="BasketSecurity.InnerSecurityIds"/>.</param>
		/// <returns>The basket value.</returns>
		public abstract decimal Calculate(decimal[] values);

		/// <summary>
		/// Initializes a new instance of the <see cref="ExpressionFormula"/>.
		/// </summary>
		/// <param name="expression">The mathematic formula.</param>
		/// <param name="securityIds">IDs securities.</param>
		protected ExpressionFormula(string expression, IEnumerable<string> securityIds)
		{
			if (expression.IsEmpty())
				throw new ArgumentNullException(nameof(expression));

			Expression = expression;
			SecurityIds = securityIds ?? throw new ArgumentNullException(nameof(securityIds));
		}

		internal ExpressionFormula(string error)
		{
			if (error.IsEmpty())
				throw new ArgumentNullException(nameof(error));

			Error = error;
		}

		/// <summary>
		/// The mathematic formula.
		/// </summary>
		public string Expression { get; }

		/// <summary>
		/// Compilation error.
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// IDs securities.
		/// </summary>
		public IEnumerable<string> SecurityIds { get; }

		/// <summary>
		/// Available functions.
		/// </summary>
		public static IEnumerable<string> Functions => ExpressionHelper.Functions;

		/// <summary>
		/// Compile mathematical formula.
		/// </summary>
		/// <param name="expression">Text expression.</param>
		/// <param name="useSecurityIds">Use security ids as variables.</param>
		/// <returns>Compiled mathematical formula.</returns>
		public static ExpressionFormula Compile(string expression, bool useSecurityIds = true)
		{
			return ConfigManager.GetService<ICompilerService>().Compile(expression, useSecurityIds);
		}
	}
}