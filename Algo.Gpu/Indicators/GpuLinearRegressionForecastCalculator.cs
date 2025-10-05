namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Linear Regression Forecast calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuLinearRegressionForecastParams"/> struct.
/// </remarks>
/// <param name="length">Linear Regression window length.</param>
/// <param name="priceType">Price type to extract from candles.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuLinearRegressionForecastParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Linear Regression window length.
	/// </summary>
	public int Length = length;

	/// <summary>
	/// Price type to extract from candles.
	/// </summary>
	public byte PriceType = priceType;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		Unsafe.AsRef(in this).PriceType = (byte)(indicator.Source ?? Level1Fields.ClosePrice);

		if (indicator is LinearRegressionForecast lrf)
		{
			Unsafe.AsRef(in this).Length = lrf.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Linear Regression Forecast indicator.
/// </summary>
public class GpuLinearRegressionForecastCalculator : GpuIndicatorCalculatorBase<LinearRegressionForecast, GpuLinearRegressionForecastParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuLinearRegressionForecastParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuLinearRegressionForecastCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuLinearRegressionForecastCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuLinearRegressionForecastParams>>(LinearRegressionForecastParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuLinearRegressionForecastParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;

		var totalSize = 0;
		var seriesOffsets = new int[seriesCount];
		var seriesLengths = new int[seriesCount];

		for (var s = 0; s < seriesCount; s++)
		{
			seriesOffsets[s] = totalSize;
			var len = candlesSeries[s]?.Length ?? 0;
			seriesLengths[s] = len;
			totalSize += len;
		}

		var flatCandles = new GpuCandle[totalSize];
		var maxLen = 0;
		var offset = 0;
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len > 0)
			{
				Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
				offset += len;
				if (len > maxLen)
					maxLen = len;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index3D(parameters.Length, seriesCount, maxLen);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuIndicatorResult[seriesCount][][];
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			result[s] = new GpuIndicatorResult[parameters.Length][];
			for (var p = 0; p < parameters.Length; p++)
			{
				var arr = new GpuIndicatorResult[len];
				for (var i = 0; i < len; i++)
				{
					var globalIdx = seriesOffsets[s] + i;
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel for Linear Regression Forecast calculation across series and parameter sets.
	/// </summary>
	private static void LinearRegressionForecastParamsSeriesKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuLinearRegressionForecastParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;
		var candleIdx = index.Z;

		var len = lengths[seriesIdx];
		if (candleIdx >= len)
			return;

		var offset = offsets[seriesIdx];
		var globalIdx = offset + candleIdx;

		var candle = flatCandles[globalIdx];
		var resIndex = paramIdx * flatCandles.Length + globalIdx;
		flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

		var prm = parameters[paramIdx];
		var window = prm.Length;
		if (window <= 1 || candleIdx < window - 1)
			return;

		var priceType = (Level1Fields)prm.PriceType;
		var startIdx = globalIdx - window + 1;

		var sumX = 0f;
		var sumY = 0f;
		var sumXY = 0f;
		var sumX2 = 0f;

		for (var i = 0; i < window; i++)
		{
			var x = (float)i;
			var y = ExtractPrice(flatCandles[startIdx + i], priceType);
			sumX += x;
			sumY += y;
			sumXY += x * y;
			sumX2 += x * x;
		}

		var lengthF = (float)window;
		var divisor = lengthF * sumX2 - sumX * sumX;
		if (divisor == 0f)
			return;

		var slope = (lengthF * sumXY - sumX * sumY) / divisor;
		var intercept = (sumY - slope * sumX) / lengthF;
		var forecast = slope * lengthF + intercept;

		flatResults[resIndex] = new() { Time = candle.Time, Value = forecast, IsFormed = 1 };
	}
}
