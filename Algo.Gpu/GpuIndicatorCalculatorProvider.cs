namespace StockSharp.Algo.Gpu;

using Ecng.Reflection;

/// <summary>
/// Provider that discovers and returns GPU calculators associated with indicators.
/// </summary>
public class GpuIndicatorCalculatorProvider
{
	private readonly Dictionary<Type, Type> _map = [];

	/// <summary>
	/// All discovered GPU calculator types. Key: indicator type, Value: GPU calculator type.
	/// </summary>
	public IReadOnlyDictionary<Type, Type> All => _map;

	/// <summary>
	/// Scan current assembly for GPU calculators and build the map.
	/// </summary>
	public void Init()
	{
		_map.Clear();

		var asm = typeof(GpuIndicatorCalculatorProvider).Assembly;

		foreach (var t in asm.FindImplementations<IGpuIndicatorCalculator>())
		{
			Type indicatorType = null;
			var baseType = t;

			while (baseType is not null && baseType != typeof(object))
			{
				if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == typeof(GpuIndicatorCalculatorBase<,,>))
				{
					indicatorType = baseType.GetGenericArguments()[0];
					break;
				}

				baseType = baseType.BaseType;
			}

			indicatorType ??= t.CreateInstance<IGpuIndicatorCalculator>()?.IndicatorType;
			if (indicatorType is null)
				continue;

			_map.Add(indicatorType, t);
		}
	}

	/// <summary>
	/// Manually register (or replace) a mapping between indicator and GPU calculator.
	/// </summary>
	/// <param name="indicatorType">Indicator type from <c>StockSharp.Algo.Indicators</c>.</param>
	/// <param name="calculatorType">GPU calculator type implementing <see cref="IGpuIndicatorCalculator"/>.</param>
	public void Register(Type indicatorType, Type calculatorType)
	{
		ArgumentNullException.ThrowIfNull(indicatorType);
		ArgumentNullException.ThrowIfNull(calculatorType);

		if (!typeof(IGpuIndicatorCalculator).IsAssignableFrom(calculatorType))
			throw new ArgumentException($"{calculatorType} must implement {nameof(IGpuIndicatorCalculator)}.", nameof(calculatorType));

		_map.Add(indicatorType, calculatorType);
	}

	/// <summary>
	/// Manually register (or replace) a mapping between indicator and GPU calculator.
	/// </summary>
	/// <typeparam name="TIndicator">Indicator type from <c>StockSharp.Algo.Indicators</c>.</typeparam>
	/// <typeparam name="TCalculator">GPU calculator type implementing <see cref="IGpuIndicatorCalculator"/>.</typeparam>
	public void Register<TIndicator, TCalculator>()
		where TIndicator : IIndicator
		where TCalculator : IGpuIndicatorCalculator
		=> Register(typeof(TIndicator), typeof(TCalculator));

	/// <summary>
	/// Unregister mapping by indicator type.
	/// </summary>
	/// <param name="indicatorType">Indicator type to remove mapping for.</param>
	/// <returns><see langword="true"/> if removed; otherwise <see langword="false"/>.</returns>
	public bool Unregister(Type indicatorType)
		=> _map.Remove(indicatorType);

	/// <summary>
	/// Unregister mapping by indicator type.
	/// </summary>
	/// <typeparam name="TIndicator">Indicator type to remove mapping for.</typeparam>
	/// <returns><see langword="true"/> if removed; otherwise <see langword="false"/>.</returns>
	public bool Unregister<TIndicator>()
		where TIndicator : IIndicator
		=> Unregister(typeof(TIndicator));

	/// <summary>
	/// Remove all mappings.
	/// </summary>
	public void Clear() => _map.Clear();

	/// <summary>
	/// Try get GPU calculator type by indicator type.
	/// </summary>
	/// <param name="indicatorType">Indicator type.</param>
	/// <param name="calculatorType">Found GPU calculator type or <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if mapping exists; otherwise <see langword="false"/>.</returns>
	public bool TryGetCalculatorType(Type indicatorType, out Type calculatorType)
		=> _map.TryGetValue(indicatorType, out calculatorType);

	/// <summary>
	/// Create GPU calculator for specified indicator type.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	/// <param name="calculatorType"><see cref="IGpuIndicatorCalculator"/></param>
	/// <returns>Instance implementing <see cref="IGpuIndicatorCalculator"/> or <see langword="null"/> if not found.</returns>
	public IGpuIndicatorCalculator Create(Context context, Accelerator accelerator, Type calculatorType)
		=> calculatorType.CreateInstance<IGpuIndicatorCalculator>(context, accelerator);
}