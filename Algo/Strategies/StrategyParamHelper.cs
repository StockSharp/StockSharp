namespace StockSharp.Algo.Strategies;

using Ecng.Reflection;

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

	private static StrategyParam<T> CreateParam<T>(string id, string name, string description, string category)
	{
		return new StrategyParam<T>(
			id,
			name,
			default
		).SetDisplay(
			name,
			description,
			category
		);
	}

	private static readonly MethodInfo _createParamMethod = typeof(StrategyParamHelper).GetMethod(nameof(CreateParam), BindingFlags.Static | BindingFlags.NonPublic);

	/// <summary>
	/// Create parameter.
	/// </summary>
	/// <param name="type"><see cref="IStrategyParam.Type"/></param>
	/// <param name="id"><see cref="IStrategyParam.Id"/></param>
	/// <param name="name"><see cref="IStrategyParam.Name"/></param>
	/// <param name="description"><see cref="IStrategyParam.Description"/></param>
	/// <param name="category"><see cref="IStrategyParam.Category"/></param>
	/// <returns><see cref="IStrategyParam"/></returns>
	public static IStrategyParam CreateParam(Type type, string id, string name, string description, string category)
		=> (IStrategyParam)_createParamMethod.Make(type).Invoke(null, [(object)id, name, description, category]);

	/// <summary>
	/// Determines whether the parameter is read-only.
	/// </summary>
	/// <param name="param"><see cref="IStrategyParam"/></param>
	/// <returns>Check result.</returns>
	public static bool IsReadOnly(this IStrategyParam param)
		=> param.Attrs<ReadOnlyAttribute>().Any(a => a.IsReadOnly);

	/// <summary>
	/// Determines whether the parameter is browsable.
	/// </summary>
	/// <param name="param"><see cref="IStrategyParam"/></param>
	/// <returns>Check result.</returns>
	public static bool IsBrowsable(this IStrategyParam param)
		=> param.Attrs<BrowsableAttribute>().All(a => a.Browsable);

	private static IEnumerable<TAttribute> Attrs<TAttribute>(this IStrategyParam param)
		=> param.CheckOnNull(nameof(param)).Attributes.OfType<TAttribute>();
}