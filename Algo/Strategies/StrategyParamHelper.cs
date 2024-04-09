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
		private static StrategyParam<T> TryAdd<T>(this Strategy strategy, StrategyParam<T> p)
		{
			if (strategy is null)
				throw new ArgumentNullException(nameof(strategy));

			strategy.Parameters.Add(p);

			return p;
		}

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
			return strategy.TryAdd(new StrategyParam<T>(name, initialValue));
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
			return strategy.TryAdd(new StrategyParam<T>(id, name, initialValue));
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
		/// Set <see cref="StrategyParam{T}.CanOptimize"/> value.
		/// </summary>
		/// <typeparam name="T">The type of the parameter value.</typeparam>
		/// <param name="param">The strategy parameter.</param>
		/// <param name="canOptimize">The value of <see cref="StrategyParam{T}.CanOptimize"/>.</param>
		/// <returns>The strategy parameter.</returns>
		public static StrategyParam<T> CanOptimize<T>(this StrategyParam<T> param, bool canOptimize)
		{
			if (param == null)
				throw new ArgumentNullException(nameof(param));

			param.CanOptimize = canOptimize;

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

			if (type.IsNullable())
				type = type.GetUnderlyingType();

			return type.IsNumeric() && !type.IsEnum() || type == typeof(bool) || type == typeof(Unit) || type == typeof(TimeSpan);
		}

		/// <summary>
		/// Set not <see langword="null"/> validator.
		/// </summary>
		/// <typeparam name="T"><see cref="StrategyParam{T}"/> type.</typeparam>
		/// <param name="param"><see cref="StrategyParam{T}"/></param>
		/// <returns><see cref="StrategyParam{T}"/></returns>
		public static StrategyParam<T> NotNull<T>(this StrategyParam<T> param)
			=> param.SetValidator(v => v is not null);

		/// <summary>
		/// Set parameter validator.
		/// </summary>
		/// <typeparam name="T"><see cref="StrategyParam{T}"/> type.</typeparam>
		/// <param name="param"><see cref="StrategyParam{T}"/></param>
		/// <param name="validator"><see cref="StrategyParam{T}.Validator"/></param>
		/// <returns><see cref="StrategyParam{T}"/></returns>
		public static StrategyParam<T> SetValidator<T>(this StrategyParam<T> param, Func<T, bool> validator)
		{
			if (param is null)
				throw new ArgumentNullException(nameof(param));

			if (validator is null)
				throw new ArgumentNullException(nameof(validator));

			param.Validator = validator;
			return param;
		}
	}
}