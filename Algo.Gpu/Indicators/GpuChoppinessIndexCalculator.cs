namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Choppiness Index calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="GpuChoppinessIndexParams"/> struct.
/// </remarks>
/// <param name="length">Indicator length.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuChoppinessIndexParams(int length) : IGpuIndicatorParams
{
	/// <summary>
	/// Choppiness Index length.
	/// </summary>
	public int Length = length;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ChoppinessIndex choppiness)
		{
			Unsafe.AsRef(in this).Length = choppiness.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Choppiness Index.
/// </summary>
public class GpuChoppinessIndexCalculator : GpuIndicatorCalculatorBase<ChoppinessIndex, GpuChoppinessIndexParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChoppinessIndexParams>, ArrayView<float>, ArrayView<float>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref="GpuChoppinessIndexCalculator"/> class.
	/// </summary>
	/// <param name="context">ILGPU context.</param>
	/// <param name="accelerator">ILGPU accelerator.</param>
	public GpuChoppinessIndexCalculator(Context context, Accelerator accelerator)
		: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChoppinessIndexParams>, ArrayView<float>, ArrayView<float>>(ChoppinessParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuChoppinessIndexParams[] parameters)
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
		using var trueRangeBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var highLowBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View, trueRangeBuffer.View, highLowBuffer.View);
		Accelerator.Synchronize();

		var flatResults = outputBuffer.GetAsArray1D();

		var result = new GpuIndicatorResult[seriesCount][][];
		var totalCandles = flatCandles.Length;
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
					var resIdx = p * totalCandles + globalIdx;
					arr[i] = flatResults[resIdx];
				}
				result[s][p] = arr;
			}
		}

		return result;
	}

	/// <summary>
	/// ILGPU kernel: Choppiness Index computation for multiple series and parameter sets.
	/// </summary>
	private static void ChoppinessParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuChoppinessIndexParams> parameters,
		ArrayView<float> trueRangeBuffer,
		ArrayView<float> highLowBuffer)
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

		var part = length > 1 ? MathF.Log10(length) : 0f;
		var totalCandles = flatCandles.Length;
		var prevClose = 0f;
		var sumTrueRange = 0f;
		var sumHighLowRange = 0f;

		for (var i = 0; i < len; i++)
		{
			var candle = flatCandles[offset + i];
			var high = candle.High;
			var low = candle.Low;
			var close = candle.Close;

			var highLowRange = high - low;
			var trueRange = highLowRange;

			var diffHigh = MathF.Abs(high - prevClose);
			if (diffHigh > trueRange)
				trueRange = diffHigh;

			var diffLow = MathF.Abs(low - prevClose);
			if (diffLow > trueRange)
				trueRange = diffLow;

			var globalIdx = offset + i;
			var bufferIdx = paramIdx * totalCandles + globalIdx;

			trueRangeBuffer[bufferIdx] = trueRange;
			highLowBuffer[bufferIdx] = highLowRange;

			sumTrueRange += trueRange;
			sumHighLowRange += highLowRange;

			if (i >= length)
			{
				var removeIdx = paramIdx * totalCandles + (offset + i - length);
				sumTrueRange -= trueRangeBuffer[removeIdx];
				sumHighLowRange -= highLowBuffer[removeIdx];
			}

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (part > 0f && i >= length - 1 && sumTrueRange > 0f && sumHighLowRange > 0f)
			{
				var ratio = sumTrueRange / sumHighLowRange;
				if (ratio > 0f)
				{
					result.Value = 100f * MathF.Log10(ratio) / part;
					result.IsFormed = 1;
				}
			}

			flatResults[bufferIdx] = result;
			prevClose = close;
		}
	}
}
