namespace StockSharp.Algo.Gpu;

/// <summary>
/// Marker interface for GPU calculators to expose the associated indicator type.
/// </summary>
public interface IGpuIndicatorCalculator
{
	/// <summary>
	/// <see cref="IIndicator"/> type this calculator is associated with.
	/// </summary>
	Type IndicatorType { get; }

	/// <summary>
	/// <see cref="IGpuIndicatorParams"/> struct type used as parameter set for this indicator.
	/// </summary>
	Type ParameterType { get; }

	/// <summary>
	/// GPU result struct type returned by this calculator.
	/// </summary>
	Type ResultType { get; }

	/// <summary>
	/// Calculate indicator for multiple series and multiple parameter sets in one GPU pass.
	/// </summary>
	/// <param name="candlesSeries">Input candle series array. Each series can have different length.</param>
	/// <param name="parameters">Array of parameter sets to calculate for each series.</param>
	/// <returns>3D array of GPU results: [series][parameter set][bar].</returns>
	IGpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, IGpuIndicatorParams[] parameters);
}

/// <summary>
/// Base class for GPU-based indicator calculators.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuIndicatorCalculatorBase"/> class.
/// </remarks>
/// <param name="context">ILGPU context.</param>
/// <param name="accelerator">ILGPU accelerator.</param>
public abstract class GpuIndicatorCalculatorBase(Context context, Accelerator accelerator) : IGpuIndicatorCalculator
{
	/// <summary>
	/// ILGPU context.
	/// </summary>
	protected readonly Context Context = context ?? throw new ArgumentNullException(nameof(context));

	/// <summary>
	/// ILGPU accelerator used for computations.
	/// </summary>
	protected readonly Accelerator Accelerator = accelerator ?? throw new ArgumentNullException(nameof(accelerator));

	/// <inheritdoc />
	public abstract Type IndicatorType { get; }

	/// <inheritdoc />
	public abstract Type ParameterType { get; }

	/// <inheritdoc />
	public abstract Type ResultType { get; }

	/// <summary>
	/// Extract price value from candle on GPU.
	/// </summary>
	/// <param name="candle">Candle.</param>
	/// <param name="priceType">Price type.</param>
	/// <returns>Extracted price.</returns>
	protected static float ExtractPrice(GpuCandle candle, Level1Fields priceType)
	{
		return priceType switch
		{
			Level1Fields.OpenPrice => candle.Open,
			Level1Fields.HighPrice => candle.High,
			Level1Fields.LowPrice => candle.Low,
			Level1Fields.ClosePrice => candle.Close,
			Level1Fields.AveragePrice => (candle.High + candle.Low + candle.Close) / 3f,
			Level1Fields.SpreadMiddle => (candle.High + candle.Low) / 2f,
			Level1Fields.VWAP => (candle.High + candle.Low + candle.Close + candle.Close) / 4f,
			_ => candle.Close
		};
	}

	/// <inheritdoc />
	public abstract IGpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, IGpuIndicatorParams[] parameters);
}

/// <summary>
/// Generic base that binds GPU calculator to the specific indicator type.
/// </summary>
/// <typeparam name="TIndicator"><see cref="IIndicator"/></typeparam>
/// <typeparam name="TParam">Parameter set struct implementing <see cref="IGpuIndicatorParams"/> for the given indicator type.</typeparam>
/// <typeparam name="TResult">Parameter set struct implementing <see cref="IGpuIndicatorResult"/> for the given indicator type.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuIndicatorCalculatorBase{TIndicator, TParam, TResult}"/> class.
/// </remarks>
public abstract class GpuIndicatorCalculatorBase<TIndicator, TParam, TResult>(Context context, Accelerator accelerator) : GpuIndicatorCalculatorBase(context, accelerator)
	where TIndicator : IIndicator
	where TParam : struct, IGpuIndicatorParams
	where TResult : struct, IGpuIndicatorResult
{
	/// <inheritdoc />
	public override Type IndicatorType => typeof(TIndicator);

	/// <inheritdoc />
	public override Type ParameterType => typeof(TParam);

	/// <inheritdoc />
	public override Type ResultType => typeof(TResult);

	/// <inheritdoc />
	public override IGpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, IGpuIndicatorParams[] parameters)
		=> Calculate(candlesSeries, [.. parameters.Cast<TParam>()]).Select(s => s.Select(p => p.Select(r => (IGpuIndicatorResult)r).ToArray()).ToArray()).ToArray();

	/// <summary>
	/// Calculate indicator for multiple series and multiple parameter sets in one GPU pass.
	/// </summary>
	/// <param name="candlesSeries">Input candle series array. Each series can have different length.</param>
	/// <param name="parameters">Array of parameter sets to calculate for each series.</param>
	/// <returns>3D array of GPU results: [series][parameter set][bar].</returns>
	public abstract TResult[][][] Calculate(GpuCandle[][] candlesSeries, TParam[] parameters);
}