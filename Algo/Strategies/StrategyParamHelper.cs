namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The auxiliary class for <see cref="StrategyParam{T}"/>.
	/// </summary>
	public static class StrategyParamHelper
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the parameter value.</typeparam>
		/// <param name="strategy">Strategy.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="initialValue">The initial value.</param>
		/// <returns>The strategy parameter.</returns>
		public static StrategyParam<T> Param<T>(this Strategy strategy, string name, T initialValue = default(T))
		{
			return new StrategyParam<T>(strategy, name, initialValue);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the parameter value.</typeparam>
		/// <param name="strategy">Strategy.</param>
		/// <param name="id">Parameter identifier.</param>
		/// <param name="name">Parameter name.</param>
		/// <param name="initialValue">The initial value.</param>
		/// <returns>The strategy parameter.</returns>
		public static StrategyParam<T> Param<T>(this Strategy strategy, string id, string name, T initialValue = default(T))
		{
			return new StrategyParam<T>(strategy, id, name, initialValue);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the parameter value.</typeparam>
		/// <param name="param">The strategy parameter.</param>
		/// <param name="optimizeFrom">The From value at optimization.</param>
		/// <param name="optimizeTo">The To value at optimization.</param>
		/// <param name="optimizeStep">The Increment value at optimization.</param>
		/// <returns>The strategy parameter.</returns>
		public static StrategyParam<T> Optimize<T>(this StrategyParam<T> param, T optimizeFrom = default(T), T optimizeTo = default(T), T optimizeStep = default(T))
		{
			if (param == null)
				throw new ArgumentNullException(nameof(param));

			param.OptimizeFrom = optimizeFrom;
			param.OptimizeTo = optimizeTo;
			param.OptimizeStep = optimizeStep;

			return param;
		}

		/// <summary>
		/// Check can optimize parameter.
		/// </summary>
		/// <param name="parameter">Strategy parameter.</param>
		/// <param name="excludeParameters">Excluded parameters.</param>
		/// <returns><see langword="true" />, if can optimize the parameter, otherwise, <see langword="false" />.</returns>
		public static bool CanOptimize(this IStrategyParam parameter, ISet<string> excludeParameters)
		{
			if (parameter == null)
				throw new ArgumentNullException(nameof(parameter));

			if (excludeParameters == null)
				throw new ArgumentNullException(nameof(excludeParameters));

			var type = parameter.Value.GetType();

			return (type.IsNumeric() && !type.IsEnum() || type == typeof(Unit)) && !excludeParameters.Contains(parameter.Name);
		}
	}
}