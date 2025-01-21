namespace StockSharp.Algo.Strategies;

/// <summary>
/// The auxiliary class for <see cref="StrategyParam{T}"/>.
/// </summary>
public static class StrategyParamHelper
{
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