namespace StockSharp.Algo.Strategies
{
	using System;

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
		public static StrategyParam<T> Param<T>(this Strategy strategy, string name, T initialValue = default)
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
		public static StrategyParam<T> Param<T>(this Strategy strategy, string id, string name, T initialValue = default)
		{
			return new StrategyParam<T>(strategy, id, name, initialValue);
		}

		/// <summary>
		/// Fill optimization parameters.
		/// </summary>
		/// <typeparam name="T">The type of the parameter value.</typeparam>
		/// <param name="param">The strategy parameter.</param>
		/// <param name="optimizeFrom">The From value at optimization.</param>
		/// <param name="optimizeTo">The To value at optimization.</param>
		/// <param name="optimizeStep">The Increment value at optimization.</param>
		/// <returns>The strategy parameter.</returns>
		public static StrategyParam<T> Optimize<T>(this StrategyParam<T> param, T optimizeFrom = default, T optimizeTo = default, T optimizeStep = default)
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
		/// <param name="type">The type of the parameter value.</param>
		/// <returns><see langword="true" />, if can optimize the parameter, otherwise, <see langword="false" />.</returns>
		public static bool CanOptimize(this Type type)
		{
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			return type.IsNumeric() && !type.IsEnum() || type == typeof(Unit);
		}
	}
}