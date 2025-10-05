namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Linear Regression R-squared calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuLinearRegRSquaredParams"/> struct.
/// </remarks>
/// <param name="length">Linear regression window length.</param>
/// <param name="priceType">Price type for regression input.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuLinearRegRSquaredParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Linear regression period length.
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

		if (indicator is LinearRegRSquared rs)
		{
			Unsafe.AsRef(in this).Length = rs.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Linear Regression R-squared indicator.
/// </summary>
public class GpuLinearRegRSquaredCalculator : GpuIndicatorCalculatorBase<LinearRegRSquared, GpuLinearRegRSquaredParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuLinearRegRSquaredParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuLinearRegRSquaredCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuLinearRegRSquaredCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
		<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuLinearRegRSquaredParams>>(LinearRegRSquaredParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuLinearRegRSquaredParams[] parameters)
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
		var offset = 0;
		for (var s = 0; s < seriesCount; s++)
		{
			var len = seriesLengths[s];
			if (len > 0)
			{
				Array.Copy(candlesSeries[s], 0, flatCandles, offset, len);
				offset += len;
			}
		}

		using var inputBuffer = Accelerator.Allocate1D(flatCandles);
		using var offsetsBuffer = Accelerator.Allocate1D(seriesOffsets);
		using var lengthsBuffer = Accelerator.Allocate1D(seriesLengths);
		using var paramsBuffer = Accelerator.Allocate1D(parameters);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
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
	/// ILGPU kernel: Linear Regression R-squared computation for multiple series and parameter sets.
	/// </summary>
	private static void LinearRegRSquaredParamsSeriesKernel(
	Index2D index,
	ArrayView<GpuCandle> flatCandles,
	ArrayView<GpuIndicatorResult> flatResults,
	ArrayView<int> offsets,
	ArrayView<int> lengths,
	ArrayView<GpuLinearRegRSquaredParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var L = prm.Length;
		if (L <= 0)
			L = 1;

		var priceType = (Level1Fields)prm.PriceType;
		var totalCandles = flatCandles.Length;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * totalCandles + globalIdx;

			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			if (i < L - 1)
				continue;

			var start = globalIdx - (L - 1);

			float sumX = 0f;
			float sumY = 0f;
			float sumXY = 0f;
			float sumX2 = 0f;

			for (var j = 0; j < L; j++)
			{
				var x = (float)j;
				var y = ExtractPrice(flatCandles[start + j], priceType);
				sumX += x;
				sumY += y;
				sumXY += x * y;
				sumX2 += x * x;
			}

			var lenF = (float)L;
			var divisor = lenF * sumX2 - sumX * sumX;
			var slope = 0f;
			if (MathF.Abs(divisor) > float.Epsilon)
				slope = (lenF * sumXY - sumX * sumY) / divisor;

			var intercept = (sumY - slope * sumX) / lenF;
			var average = sumY / lenF;

			float sumYAv2 = 0f;
			float sumErr2 = 0f;

			for (var j = 0; j < L; j++)
			{
				var y = ExtractPrice(flatCandles[start + j], priceType);
				var yEst = slope * j + intercept;
				var diffAvg = y - average;
				sumYAv2 += diffAvg * diffAvg;
				var diffErr = y - yEst;
				sumErr2 += diffErr * diffErr;
			}

			var value = 0f;
			if (sumYAv2 > 0f)
				value = 1f - sumErr2 / sumYAv2;

			flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };
		}
	}
}
