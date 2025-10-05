namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Linear Regression Slope calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuLinearRegSlopeParams"/> struct.
/// </remarks>
/// <param name="length">Linear regression slope length.</param>
/// <param name="priceType">Price type.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuLinearRegSlopeParams(int length, byte priceType) : IGpuIndicatorParams
{
	/// <summary>
	/// Linear regression slope window length.
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

		if (indicator is LinearRegSlope slope)
		{
			Unsafe.AsRef(in this).Length = slope.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Linear Regression Slope indicator.
/// </summary>
public class GpuLinearRegSlopeCalculator : GpuIndicatorCalculatorBase<LinearRegSlope, GpuLinearRegSlopeParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuLinearRegSlopeParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuLinearRegSlopeCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuLinearRegSlopeCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuLinearRegSlopeParams>>(LinearRegSlopeParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuLinearRegSlopeParams[] parameters)
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
	/// ILGPU kernel: Linear Regression Slope computation for multiple series and parameter sets.
	/// </summary>
	private static void LinearRegSlopeParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuLinearRegSlopeParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var prm = parameters[paramIdx];
		var length = prm.Length;
		if (length < 1)
			length = 1;

		var lengthF = (float)length;
		var sumX = 0.5f * lengthF * (lengthF - 1f);
		var sumX2 = lengthF * (lengthF - 1f) * (2f * lengthF - 1f) / 6f;
		var priceType = (Level1Fields)prm.PriceType;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			if (i < length - 1)
				continue;

			float sumY = 0f;
			float sumXY = 0f;
			var startIdx = globalIdx - (length - 1);
			for (var j = 0; j < length; j++)
			{
				var price = ExtractPrice(flatCandles[startIdx + j], priceType);
				sumY += price;
				sumXY += j * price;
			}

			var divisor = lengthF * sumX2 - sumX * sumX;
			if (divisor == 0f)
				continue;

			var slope = (lengthF * sumXY - sumX * sumY) / divisor;
			flatResults[resIndex] = new() { Time = candle.Time, Value = slope, IsFormed = 1 };
		}
	}
}
