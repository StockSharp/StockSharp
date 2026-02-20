namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Relative Vigor Index Average calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuRelativeVigorIndexAverageParams"/> struct.
/// </remarks>
/// <param name="length">RVI average length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuRelativeVigorIndexAverageParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// RVI average period length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is RelativeVigorIndexAverage rviAvg)
		{
			Unsafe.AsRef(in this).Length = rviAvg.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Relative Vigor Index Average.
/// </summary>
public class GpuRelativeVigorIndexAverageCalculator : GpuIndicatorCalculatorBase<RelativeVigorIndexAverage, GpuRelativeVigorIndexAverageParams, GpuIndicatorResult>
{
	private readonly Action<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuRelativeVigorIndexAverageParams>> _paramsSeriesKernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuRelativeVigorIndexAverageCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuRelativeVigorIndexAverageCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_paramsSeriesKernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index3D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuRelativeVigorIndexAverageParams>>(RelativeVigorIndexAverageKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuRelativeVigorIndexAverageParams[] parameters)
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
		_paramsSeriesKernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: Relative Vigor Index Average computation for multiple series and parameter sets.
	/// </summary>
	private static void RelativeVigorIndexAverageKernel(
		Index3D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuRelativeVigorIndexAverageParams> parameters)
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
		var L = prm.Length;
		if (L < 4)
			L = 4;

		if (candleIdx < L - 1)
			return;

		// CPU uses circular buffer of capacity L. buffer[0..3] = 4 oldest candles.
		// Weighted average: (c0 + 2*c1 + 2*c2 + c3) / 6
		var j = offset + candleIdx - L + 1; // oldest candle in buffer window
		var c0 = flatCandles[j];
		var c1 = flatCandles[j + 1];
		var c2 = flatCandles[j + 2];
		var c3 = flatCandles[j + 3];

		var valueUp = ((c0.Close - c0.Open) + 2f * (c1.Close - c1.Open) + 2f * (c2.Close - c2.Open) + (c3.Close - c3.Open)) / 6f;
		var valueDn = ((c0.High - c0.Low) + 2f * (c1.High - c1.Low) + 2f * (c2.High - c2.Low) + (c3.High - c3.Low)) / 6f;

		var value = valueDn == 0f ? valueUp : valueUp / valueDn;

		flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };
	}
}
