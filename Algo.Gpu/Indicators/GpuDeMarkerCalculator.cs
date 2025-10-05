namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU DeMarker calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuDeMarkerParams"/> struct.
/// </remarks>
/// <param name="length">DeMarker SMA length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuDeMarkerParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// DeMarker smoothing length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is DeMarker deMarker)
		{
			Unsafe.AsRef(in this).Length = deMarker.Length;
		}
	}
}

/// <summary>
/// GPU calculator for DeMarker indicator.
/// </summary>
public class GpuDeMarkerCalculator : GpuIndicatorCalculatorBase<DeMarker, GpuDeMarkerParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDeMarkerParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuDeMarkerCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuDeMarkerCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuDeMarkerParams>>(DeMarkerParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuDeMarkerParams[] parameters)
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
					var resIdx = p * totalSize + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: DeMarker computation for multiple series and multiple parameter sets.
	/// Each thread handles one (parameter, series) pair and iterates bars sequentially.
	/// </summary>
	private static void DeMarkerParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuDeMarkerParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var L = parameters[paramIdx].Length;
		if (L <= 0)
			L = 1;

		var prevHigh = flatCandles[offset].High;
		var prevLow = flatCandles[offset].Low;
		float deMaxSum = 0f;
		float deMinSum = 0f;

		for (var i = 0; i < len; i++)
		{
			var candleIdx = offset + i;
			var candle = flatCandles[candleIdx];
			var resIndex = paramIdx * flatCandles.Length + candleIdx;
			flatResults[resIndex] = new() { Time = candle.Time, Value = float.NaN, IsFormed = 0 };

			if (i == 0)
			{
				prevHigh = candle.High;
				prevLow = candle.Low;
				continue;
			}

			var high = candle.High;
			var low = candle.Low;
			var deMax = high > prevHigh ? high - prevHigh : 0f;
			var deMin = low < prevLow ? prevLow - low : 0f;

			deMaxSum += deMax;
			deMinSum += deMin;

			if (i > L)
			{
				var oldIdx = candleIdx - L;
				var oldCandle = flatCandles[oldIdx];
				var oldPrevCandle = flatCandles[oldIdx - 1];
				var oldDeMax = oldCandle.High > oldPrevCandle.High ? oldCandle.High - oldPrevCandle.High : 0f;
				var oldDeMin = oldCandle.Low < oldPrevCandle.Low ? oldPrevCandle.Low - oldCandle.Low : 0f;
				deMaxSum -= oldDeMax;
				deMinSum -= oldDeMin;
			}

			if (i >= L)
			{
				var denominator = deMaxSum + deMinSum;
				var value = denominator > 0f ? deMaxSum / denominator : 0.5f;
				flatResults[resIndex] = new() { Time = candle.Time, Value = value, IsFormed = 1 };
			}

			prevHigh = high;
			prevLow = low;
		}
	}
}
