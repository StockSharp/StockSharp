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

		type = type.GetUnderlyingType() ?? type;

		return type.IsNumeric() && !type.IsEnum() || type == typeof(bool) || type == typeof(Unit) || type == typeof(TimeSpan) || type == typeof(DataType);
	}

	private static StrategyParam<T> CreateParam<T>(string id) => new(id);

	private static readonly MethodInfo _createParamMethod = typeof(StrategyParamHelper).GetMethod(nameof(CreateParam), BindingFlags.Static | BindingFlags.NonPublic);

	/// <summary>
	/// Create parameter.
	/// </summary>
	/// <param name="type"><see cref="IStrategyParam.Type"/></param>
	/// <param name="id"><see cref="IStrategyParam.Id"/></param>
	/// <returns><see cref="IStrategyParam"/></returns>
	public static IStrategyParam CreateParam(Type type, string id)
		=> (IStrategyParam)_createParamMethod.Make(type).Invoke(null, [id]);

	/// <summary>
	/// Get the parameter name.
	/// </summary>
	/// <param name="param"><see cref="IStrategyParam"/></param>
	/// <returns>Display name.</returns>
	public static string GetName(this IStrategyParam param)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		return param.GetDisplayName().IsEmpty(param.Id);
	}
}