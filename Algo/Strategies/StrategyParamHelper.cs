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

	/// <summary>
	/// Generate a random value within the optimization range for the parameter.
	/// </summary>
	/// <typeparam name="T">The type of the parameter value.</typeparam>
	/// <param name="param"><see cref="StrategyParam{T}"/></param>
	/// <returns>Random value within the optimization range.</returns>
	/// <exception cref="InvalidOperationException">Thrown when parameter cannot be optimized or optimization range is not set.</exception>
	public static T GetRandom<T>(this StrategyParam<T> param)
		=> (T)GetRandom((IStrategyParam)param);

	/// <summary>
	/// Generate a random value within the optimization range for the parameter.
	/// </summary>
	/// <param name="param"><see cref="IStrategyParam"/></param>
	/// <returns>Random value within the optimization range.</returns>
	/// <exception cref="InvalidOperationException">Thrown when parameter cannot be optimized or optimization range is not set.</exception>
	public static object GetRandom(this IStrategyParam param)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		if (!param.CanOptimize)
			throw new InvalidOperationException($"Parameter '{param.Id}' cannot be optimized. Set CanOptimize to true.");

		if (param.OptimizeFrom == null || param.OptimizeTo == null)
			throw new InvalidOperationException($"Parameter '{param.Id}' optimization range is not set. Use SetOptimize method to configure optimization range.");

		var type = param.Type.GetUnderlyingType() ?? param.Type;

		return type switch
		{
			_ when type == typeof(int) => GenerateRandomInt((int)param.OptimizeFrom, (int)param.OptimizeTo, (int?)param.OptimizeStep),
			_ when type == typeof(long) => GenerateRandomLong((long)param.OptimizeFrom, (long)param.OptimizeTo, (long?)param.OptimizeStep),
			_ when type == typeof(decimal) => GenerateRandomDecimal((decimal)param.OptimizeFrom, (decimal)param.OptimizeTo, (decimal?)param.OptimizeStep),
			_ when type == typeof(double) => GenerateRandomDouble((double)param.OptimizeFrom, (double)param.OptimizeTo, (double?)param.OptimizeStep),
			_ when type == typeof(float) => GenerateRandomFloat((float)param.OptimizeFrom, (float)param.OptimizeTo, (float?)param.OptimizeStep),
			_ when type == typeof(bool) => RandomGen.GetBool(),
			_ when type == typeof(TimeSpan) => GenerateRandomTimeSpan((TimeSpan)param.OptimizeFrom, (TimeSpan)param.OptimizeTo, (TimeSpan?)param.OptimizeStep),
			_ when type == typeof(Unit) => GenerateRandomUnit((Unit)param.OptimizeFrom, (Unit)param.OptimizeTo, (Unit)param.OptimizeStep),
			_ => param.Value
		};
	}

	private static int GenerateRandomInt(int from, int to, int? step)
	{
		if (step.HasValue && step.Value > 0)
		{
			var steps = (to - from) / step.Value;
			return from + RandomGen.GetInt(0, steps) * step.Value;
		}

		return RandomGen.GetInt(from, to);
	}

	private static long GenerateRandomLong(long from, long to, long? step)
	{
		if (step.HasValue && step.Value > 0)
		{
			var steps = (to - from) / step.Value;
			return from + RandomGen.GetInt(0, (int)steps) * step.Value;
		}

		return RandomGen.GetLong(from, to);
	}

	private static decimal GenerateRandomDecimal(decimal from, decimal to, decimal? step)
	{
		if (step.HasValue && step.Value > 0)
		{
			var steps = (int)((to - from) / step.Value);
			return from + RandomGen.GetInt(0, steps) * step.Value;
		}

		return RandomGen.GetDecimal(from, to, from.GetDecimalInfo().EffectiveScale);
	}

	private static double GenerateRandomDouble(double from, double to, double? step)
	{
		if (step.HasValue && step.Value > 0)
		{
			var steps = (int)((to - from) / step.Value);
			return from + RandomGen.GetInt(0, steps) * step.Value;
		}

		return RandomGen.GetDouble(from, to);
	}

	private static float GenerateRandomFloat(float from, float to, float? step)
	{
		if (step.HasValue && step.Value > 0)
		{
			var steps = (int)((to - from) / step.Value);
			return from + RandomGen.GetInt(0, steps) * step.Value;
		}

		return RandomGen.GetFloat(from, to);
	}

	private static TimeSpan GenerateRandomTimeSpan(TimeSpan from, TimeSpan to, TimeSpan? step)
	{
		if (step.HasValue && step.Value > TimeSpan.Zero)
		{
			var steps = (int)((to - from).Ticks / step.Value.Ticks);
			return from + TimeSpan.FromTicks(RandomGen.GetInt(0, steps) * step.Value.Ticks);
		}

		var range = to - from;
		return from + TimeSpan.FromTicks(RandomGen.GetLong(0, range.Ticks));
	}

	private static Unit GenerateRandomUnit(Unit from, Unit to, Unit step)
	{
		if (from.Type != to.Type)
			throw new ArgumentException($"Unit types must match: from.Type={from.Type}, to.Type={to.Type}");

		if (step is not null && step.Type != from.Type)
			throw new ArgumentException($"Step unit type must match range type: step.Type={step.Type}, range.Type={from.Type}");

		var randomValue = step is not null && step.Value > 0
			? GenerateRandomDecimal(from.Value, to.Value, step.Value)
			: GenerateRandomDecimal(from.Value, to.Value, null);

		return new Unit(randomValue, from.Type);
	}
}