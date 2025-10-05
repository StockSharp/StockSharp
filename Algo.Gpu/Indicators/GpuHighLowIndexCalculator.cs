namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU High Low Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuHighLowIndexParams"/> struct.
/// </remarks>
/// <param name="length">Calculation length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuHighLowIndexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// High Low Index window length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is HighLowIndex highLowIndex)
		{
			Unsafe.AsRef(in this).Length = highLowIndex.Length;
		}
	}
}

/// <summary>
/// GPU calculator for High Low Index indicator.
/// </summary>
public class GpuHighLowIndexCalculator : GpuIndicatorCalculatorBase<HighLowIndex, GpuHighLowIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHighLowIndexParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuHighLowIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuHighLowIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuHighLowIndexParams>>(HighLowIndexParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuHighLowIndexParams[] parameters)
	{
		ArgumentNullException.ThrowIfNull(candlesSeries);
		ArgumentNullException.ThrowIfNull(parameters);

		if (candlesSeries.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(candlesSeries));

		if (parameters.Length == 0)
			throw new ArgumentOutOfRangeException(nameof(parameters));

		var seriesCount = candlesSeries.Length;

		// Flatten input
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

		// Re-split [series][param][bar]
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
					var resIdx = p * flatCandles.Length + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel for High Low Index calculation for multiple series and parameters.
	/// Each thread processes one (parameter, series) pair sequentially.
	/// </summary>
	private static void HighLowIndexParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuHighLowIndexParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var length = parameters[paramIdx].Length;
		if (length <= 0)
			length = 1;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var candle = flatCandles[globalIdx];
			var resIndex = paramIdx * flatCandles.Length + globalIdx;

			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			if (i < length - 1)
				continue;

			var start = i - length + 1;
			var highestHigh = float.MinValue;
			var lowestLow = float.MaxValue;

			for (var j = 0; j < length; j++)
			{
				var c = flatCandles[offset + start + j];
				if (c.High > highestHigh)
					highestHigh = c.High;
				if (c.Low < lowestLow)
					lowestLow = c.Low;
			}

			var range = highestHigh - lowestLow;
			float value;

			if (range == 0f)
			{
				value = 50f;
			}
			else
			{
				value = (candle.High - lowestLow) / range * 100f;
			}

			flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };
		}
	}
}
