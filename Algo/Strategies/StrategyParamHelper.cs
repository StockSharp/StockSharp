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

		// Check for explicit values first (for Security, DataType, etc.)
		var explicitValues = param.OptimizeValues;
		if (explicitValues is not null)
		{
			var arr = explicitValues.Cast<object>().ToArray();
			if (arr.Length == 0)
				throw new InvalidOperationException($"Parameter '{param.Id}' has empty OptimizeValues.");
			return RandomGen.GetElement(arr);
		}

		if (param.OptimizeFrom == null || param.OptimizeTo == null)
			throw new InvalidOperationException($"Parameter '{param.Id}' optimization range is not set. Use SetOptimize or SetOptimizeValues method.");

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

	#region Optimization Values Generation

	/// <summary>
	/// Get the number of optimization iterations for the parameter.
	/// </summary>
	/// <param name="param"><see cref="IStrategyParam"/></param>
	/// <returns>Number of iterations.</returns>
	public static int GetIterationsCount(this IStrategyParam param)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		if (!param.CanOptimize)
			return 1;

		// Check for explicit values first (for Security, DataType, etc.)
		var explicitValues = param.OptimizeValues;
		if (explicitValues is not null)
		{
			var count = explicitValues.Cast<object>().Count();
			return count > 0 ? count : 1;
		}

		var from = param.OptimizeFrom;
		var to = param.OptimizeTo;
		var step = param.OptimizeStep;

		if (from is null || to is null)
			return 1;

		var type = param.Type.GetUnderlyingType() ?? param.Type;

		if (step is null && type != typeof(bool))
			return 1;

		static int getIterCountDec(decimal fromVal, decimal toVal, decimal stepVal)
			=> (int)Math.Ceiling((toVal - fromVal + stepVal) / stepVal);

		static int getIterCountLong(long fromVal, long toVal, long stepVal)
			=> (int)((toVal - fromVal + stepVal) / stepVal);

		if (type == typeof(bool))
			return from.Equals(to) ? 1 : 2;
		else if (type == typeof(Unit))
		{
			var fromTyped = (Unit)from;
			var toTyped = (Unit)to;
			var stepTyped = (Unit)step;

			return getIterCountDec(fromTyped.Value, toTyped.Value, stepTyped.Value);
		}
		else if (type == typeof(TimeSpan) || type.IsNumericInteger())
		{
			var fromTyped = from.To<long>();
			var toTyped = to.To<long>();
			var stepTyped = step.To<long>();

			return getIterCountLong(fromTyped, toTyped, stepTyped);
		}
		else
		{
			var fromTyped = from.To<decimal>();
			var toTyped = to.To<decimal>();
			var stepTyped = step.To<decimal>();

			return getIterCountDec(fromTyped, toTyped, stepTyped);
		}
	}

	/// <summary>
	/// Get all optimization values for the parameter.
	/// </summary>
	/// <param name="param"><see cref="IStrategyParam"/></param>
	/// <returns>Enumerable of all optimization values.</returns>
	public static IEnumerable<object> GetOptimizationValues(this IStrategyParam param)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		if (!param.CanOptimize)
		{
			yield return param.Value;
			yield break;
		}

		// Check for explicit values first (for Security, DataType, etc.)
		var explicitValues = param.OptimizeValues;
		if (explicitValues is not null)
		{
			foreach (var v in explicitValues)
				yield return v;
			yield break;
		}

		var from = param.OptimizeFrom;
		var to = param.OptimizeTo;
		var step = param.OptimizeStep;

		if (from is null || to is null)
		{
			yield return param.Value;
			yield break;
		}

		var type = param.Type.GetUnderlyingType() ?? param.Type;

		if (step is null && type != typeof(bool))
		{
			yield return param.Value;
			yield break;
		}

		if (type == typeof(decimal))
		{
			var fromTyped = (decimal)from;
			var toTyped = (decimal)to;
			var stepTyped = (decimal)step;

			if (fromTyped > toTyped)
			{
				while (fromTyped > toTyped)
				{
					yield return fromTyped;
					fromTyped += stepTyped;
				}
			}
			else
			{
				while (fromTyped <= toTyped)
				{
					yield return fromTyped;
					fromTyped += stepTyped;
				}
			}
		}
		else if (type == typeof(bool))
		{
			var fromTyped = (bool)from;
			var toTyped = (bool)to;

			yield return fromTyped;

			if (fromTyped != toTyped)
				yield return toTyped;
		}
		else if (type == typeof(Unit))
		{
			var fromTyped = (Unit)from;
			var toTyped = (Unit)to;
			var stepTyped = (Unit)step;

			if (fromTyped > toTyped)
			{
				while (fromTyped > toTyped)
				{
					yield return fromTyped;
					fromTyped += stepTyped;
				}
			}
			else
			{
				while (fromTyped <= toTyped)
				{
					yield return fromTyped;
					fromTyped += stepTyped;
				}
			}
		}
		else if (type == typeof(TimeSpan))
		{
			var fromTyped = (TimeSpan)from;
			var toTyped = (TimeSpan)to;
			var stepTyped = (TimeSpan)step;

			if (fromTyped > toTyped)
			{
				while (fromTyped > toTyped)
				{
					yield return fromTyped;
					fromTyped += stepTyped;
				}
			}
			else
			{
				while (fromTyped <= toTyped)
				{
					yield return fromTyped;
					fromTyped += stepTyped;
				}
			}
		}
		else if (type.IsPrimitive())
		{
			var fromTyped = from.To<long>();
			var toTyped = to.To<long>();
			var stepTyped = step.To<long>();

			if (fromTyped > toTyped)
			{
				while (fromTyped > toTyped)
				{
					yield return fromTyped.To(type);
					fromTyped += stepTyped;
				}
			}
			else
			{
				while (fromTyped <= toTyped)
				{
					yield return fromTyped.To(type);
					fromTyped += stepTyped;
				}
			}
		}
		else
			throw new NotSupportedException(LocalizedStrings.TypeNotSupported.Put(type));
	}

	#endregion

	#region Brute Force Optimization

	/// <summary>
	/// Generate all strategy clones with parameter permutations for brute force optimization.
	/// </summary>
	/// <param name="strategy">The base strategy to clone.</param>
	/// <param name="parameters">The parameters to optimize.</param>
	/// <param name="optimizedParams">Output: all parameters involved in optimization.</param>
	/// <param name="totalCount">Output: total number of iterations.</param>
	/// <returns>Lazy enumerable of strategy clones with their parameter arrays.</returns>
	public static IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> ToBruteForce(
		this Strategy strategy,
		IStrategyParam[] parameters,
		out IStrategyParam[] optimizedParams,
		out int totalCount)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters), "No optimization parameters.");

		var singleParams = new List<IStrategyParam>();
		var optimizeDict = new Dictionary<string, (IStrategyParam param, IEnumerable<object> values, int iterCount)>();

		foreach (var param in parameters)
		{
			if (!param.CanOptimize)
			{
				singleParams.Add(param);
				continue;
			}

			var iterCount = param.GetIterationsCount();
			var values = param.GetOptimizationValues();

			if (iterCount == 0)
				continue;
			else if (iterCount == 1)
			{
				singleParams.Add(param);
				continue;
			}
			else
				optimizeDict[param.Id] = (param, values, iterCount);
		}

		if (optimizeDict.IsEmpty() && singleParams.IsEmpty())
			throw new ArgumentException("No any params for optimize.", nameof(parameters));

		totalCount = optimizeDict.Aggregate(1, (c, p) => c * p.Value.iterCount);
		optimizedParams = [.. singleParams.Concat(optimizeDict.Values.Select(p => p.param)).Distinct()];

		IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> _()
		{
			if (optimizeDict.IsEmpty())
			{
				if (singleParams.IsEmpty())
					throw new InvalidOperationException("singleParams empty");

				yield return (strategy, singleParams.ToArray());
				yield break;
			}

			foreach (var combination in GetParameterCombinations(optimizeDict))
			{
				Strategy iter;

				using (new Scope<StrategyContext>(new() { ExcludeUI = true }))
					iter = strategy.Clone();

				var resultParams = new List<IStrategyParam>();

				foreach (var (id, value) in combination)
				{
					var param = iter.Parameters[id];
					param.Value = value;
					resultParams.Add(param);
				}

				yield return (iter, resultParams.ToArray());
			}
		}

		return _();
	}

	/// <summary>
	/// Generate all strategy clones with parameter permutations for brute force optimization.
	/// Supports explicit value lists for parameters like Security, DataType.
	/// </summary>
	/// <param name="strategy">The base strategy to clone.</param>
	/// <param name="parameters">The parameters with optional explicit values to use instead of range.</param>
	/// <param name="optimizedParams">Output: all parameters involved in optimization.</param>
	/// <param name="totalCount">Output: total number of iterations.</param>
	/// <returns>Lazy enumerable of strategy clones with their parameter arrays.</returns>
	public static IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> ToBruteForce(
		this Strategy strategy,
		IEnumerable<(IStrategyParam param, IEnumerable values)> parameters,
		out IStrategyParam[] optimizedParams,
		out int totalCount)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		var paramArr = parameters.ToArray();

		if (paramArr.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters), "No optimization parameters.");

		var singleParams = new List<IStrategyParam>();
		var optimizeDict = new Dictionary<string, (IStrategyParam param, IEnumerable<object> values, int iterCount)>();

		foreach (var (param, explicitValues) in paramArr)
		{
			if (!param.CanOptimize)
			{
				singleParams.Add(param);
				continue;
			}

			IEnumerable<object> values;
			int iterCount;

			if (explicitValues?.Cast<object>().Any() == true)
			{
				// Use explicit values (for Security, DataType, etc.)
				values = explicitValues.Cast<object>();
				iterCount = values.Count();
			}
			else
			{
				// Use range from/to/step
				iterCount = param.GetIterationsCount();
				values = param.GetOptimizationValues();
			}

			if (iterCount == 0)
				continue;
			else if (iterCount == 1)
			{
				singleParams.Add(param);
				continue;
			}
			else
				optimizeDict[param.Id] = (param, values, iterCount);
		}

		if (optimizeDict.IsEmpty() && singleParams.IsEmpty())
			throw new ArgumentException("No any params for optimize.", nameof(parameters));

		totalCount = optimizeDict.Aggregate(1, (c, p) => c * p.Value.iterCount);
		optimizedParams = [.. singleParams.Concat(optimizeDict.Values.Select(p => p.param)).Distinct()];

		IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> _()
		{
			if (optimizeDict.IsEmpty())
			{
				if (singleParams.IsEmpty())
					throw new InvalidOperationException("singleParams empty");

				yield return (strategy, singleParams.ToArray());
				yield break;
			}

			foreach (var combination in GetParameterCombinations(optimizeDict))
			{
				Strategy iter;

				using (new Scope<StrategyContext>(new() { ExcludeUI = true }))
					iter = strategy.Clone();

				var resultParams = new List<IStrategyParam>();

				foreach (var (id, value) in combination)
				{
					var clonedParam = iter.Parameters[id];
					clonedParam.Value = value;
					resultParams.Add(clonedParam);
				}

				yield return (iter, resultParams.ToArray());
			}
		}

		return _();
	}

	/// <summary>
	/// Generate all strategy clones with random parameter values for brute force optimization.
	/// </summary>
	/// <param name="strategy">The base strategy to clone.</param>
	/// <param name="parameters">The parameters to optimize.</param>
	/// <param name="randomCount">Number of random samples per parameter.</param>
	/// <param name="optimizedParams">Output: all parameters involved in optimization.</param>
	/// <param name="totalCount">Output: total number of iterations.</param>
	/// <returns>Lazy enumerable of strategy clones with their parameter arrays.</returns>
	public static IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> ToBruteForceRandom(
		this Strategy strategy,
		IStrategyParam[] parameters,
		int randomCount,
		out IStrategyParam[] optimizedParams,
		out int totalCount)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters), "No optimization parameters.");

		if (randomCount <= 0)
			throw new ArgumentOutOfRangeException(nameof(randomCount), "Random count must be positive.");

		var singleParams = new List<IStrategyParam>();
		var optimizeDict = new Dictionary<string, (IStrategyParam param, HashSet<object> values)>();

		foreach (var param in parameters)
		{
			if (!param.CanOptimize || param.OptimizeFrom is null || param.OptimizeTo is null)
			{
				singleParams.Add(param);
				continue;
			}

			var values = new HashSet<object>();

			for (var i = 0; i < randomCount; i++)
			{
				var randomValue = param.GetRandom();
				if (randomValue is not null)
					values.Add(randomValue);
			}

			if (values.Count == 0)
				singleParams.Add(param);
			else if (values.Count == 1)
			{
				strategy.Parameters[param.Id].Value = values.First();
				singleParams.Add(param);
			}
			else
				optimizeDict[param.Id] = (param, values);
		}

		if (optimizeDict.IsEmpty() && singleParams.IsEmpty())
			throw new ArgumentException("No any params for optimize.", nameof(parameters));

		totalCount = optimizeDict.Aggregate(1, (c, p) => c * p.Value.values.Count);
		optimizedParams = [.. singleParams.Concat(optimizeDict.Values.Select(p => p.param)).Distinct()];

		IEnumerable<(Strategy strategy, IStrategyParam[] parameters)> _()
		{
			if (optimizeDict.IsEmpty())
			{
				if (singleParams.IsEmpty())
					throw new InvalidOperationException("singleParams empty");

				yield return (strategy, singleParams.ToArray());
				yield break;
			}

			var dictForCombinations = optimizeDict.ToDictionary(
				p => p.Key,
				p => (p.Value.param, values: (IEnumerable<object>)p.Value.values, iterCount: p.Value.values.Count));

			foreach (var combination in GetParameterCombinations(dictForCombinations))
			{
				Strategy iter;

				using (new Scope<StrategyContext>(new() { ExcludeUI = true }))
					iter = strategy.Clone();

				var resultParams = new List<IStrategyParam>();

				foreach (var (id, value) in combination)
				{
					var param = iter.Parameters[id];
					param.Value = value;
					resultParams.Add(param);
				}

				yield return (iter, resultParams.ToArray());
			}
		}

		return _();
	}

	private static IEnumerable<(string id, object value)[]> GetParameterCombinations(
		Dictionary<string, (IStrategyParam param, IEnumerable<object> values, int iterCount)> optimizeDict)
	{
		var keys = optimizeDict.Keys.ToArray();
		var valueLists = keys.Select(k => optimizeDict[k].values.ToArray()).ToArray();
		var indices = new int[keys.Length];
		var counts = valueLists.Select(v => v.Length).ToArray();

		while (true)
		{
			var result = new (string id, object value)[keys.Length];
			for (var i = 0; i < keys.Length; i++)
				result[i] = (keys[i], valueLists[i][indices[i]]);

			yield return result;

			// Increment indices (like a multi-digit counter)
			var pos = keys.Length - 1;
			while (pos >= 0)
			{
				indices[pos]++;
				if (indices[pos] < counts[pos])
					break;

				indices[pos] = 0;
				pos--;
			}

			if (pos < 0)
				yield break;
		}
	}

	#endregion

	#region Genetic Optimization

	/// <summary>
	/// Convert strategy parameters to genetic optimizer format.
	/// </summary>
	/// <param name="strategy">The strategy with parameters.</param>
	/// <param name="parameters">The parameters to optimize.</param>
	/// <returns>Array of tuples suitable for <see cref="Optimization.GeneticOptimizer.Start"/>.</returns>
	public static (IStrategyParam param, object from, object to, object step, IEnumerable values)[] ToGeneticParameters(
		this Strategy strategy,
		IStrategyParam[] parameters)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters), "No optimization parameters.");

		var result = new List<(IStrategyParam param, object from, object to, object step, IEnumerable values)>();

		foreach (var param in parameters)
		{
			if (!param.CanOptimize)
				continue;

			// Check for explicit values first
			var explicitValues = param.OptimizeValues;
			if (explicitValues?.Cast<object>().Any() == true)
			{
				result.Add((param, null, null, param.OptimizeStep, explicitValues));
				continue;
			}

			var from = param.OptimizeFrom;
			var to = param.OptimizeTo;
			var step = param.OptimizeStep;

			if (from is null || to is null)
				continue;

			result.Add((param, from, to, step, null));
		}

		return [.. result];
	}

	/// <summary>
	/// Convert strategy parameters to genetic optimizer format with explicit values support.
	/// </summary>
	/// <param name="strategy">The strategy with parameters.</param>
	/// <param name="parameters">The parameters with optional explicit values to use instead of range.</param>
	/// <returns>Array of tuples suitable for <see cref="Optimization.GeneticOptimizer.Start"/>.</returns>
	public static (IStrategyParam param, object from, object to, object step, IEnumerable values)[] ToGeneticParameters(
		this Strategy strategy,
		IEnumerable<(IStrategyParam param, IEnumerable values)> parameters)
	{
		if (strategy is null)
			throw new ArgumentNullException(nameof(strategy));

		if (parameters is null)
			throw new ArgumentNullException(nameof(parameters));

		var result = new List<(IStrategyParam param, object from, object to, object step, IEnumerable values)>();

		foreach (var (param, explicitValues) in parameters)
		{
			if (!param.CanOptimize)
				continue;

			if (explicitValues?.Cast<object>().Any() == true)
			{
				// Use explicit values instead of range
				result.Add((param, null, null, param.OptimizeStep, explicitValues));
			}
			else
			{
				var from = param.OptimizeFrom;
				var to = param.OptimizeTo;
				var step = param.OptimizeStep;

				if (from is null || to is null)
					continue;

				result.Add((param, from, to, step, null));
			}
		}

		return [.. result];
	}

	#endregion

	#region Random Values Generation (Batch)

	/// <summary>
	/// Generate a set of unique random values for the parameter.
	/// </summary>
	/// <typeparam name="T">The type of the parameter value.</typeparam>
	/// <param name="param"><see cref="StrategyParam{T}"/></param>
	/// <param name="count">Number of random values to generate.</param>
	/// <returns>Set of unique random values.</returns>
	public static HashSet<T> GetRandomValues<T>(this StrategyParam<T> param, int count)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		if (count <= 0)
			throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

		var values = new HashSet<T>();

		// Try to generate unique values, but limit attempts to prevent infinite loops
		var maxAttempts = count * 10;
		var attempts = 0;

		while (values.Count < count && attempts < maxAttempts)
		{
			var value = param.GetRandom();
			values.Add(value);
			attempts++;
		}

		return values;
	}

	/// <summary>
	/// Generate a set of unique random values for the parameter.
	/// </summary>
	/// <param name="param"><see cref="IStrategyParam"/></param>
	/// <param name="count">Number of random values to generate.</param>
	/// <returns>Set of unique random values.</returns>
	public static HashSet<object> GetRandomValues(this IStrategyParam param, int count)
	{
		if (param is null)
			throw new ArgumentNullException(nameof(param));

		if (count <= 0)
			throw new ArgumentOutOfRangeException(nameof(count), "Count must be positive.");

		var values = new HashSet<object>();

		// Try to generate unique values, but limit attempts to prevent infinite loops
		var maxAttempts = count * 10;
		var attempts = 0;

		while (values.Count < count && attempts < maxAttempts)
		{
			var value = param.GetRandom();
			if (value is not null)
				values.Add(value);
			attempts++;
		}

		return values;
	}

	#endregion
}
