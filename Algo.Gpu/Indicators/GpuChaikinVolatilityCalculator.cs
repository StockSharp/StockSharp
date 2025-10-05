namespace StockSharp.Algo.Gpu.Indicators;

/// <summary>
/// Parameter set for GPU Chaikin Volatility calculation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref=\"GpuChaikinVolatilityParams\"/> struct.
/// </remarks>
/// <param name=\"emaLength\">Length for the EMA smoothing.</param>
/// <param name=\"rocLength\">Length for the ROC calculation.</param>
[StructLayout(LayoutKind.Sequential)]
public struct GpuChaikinVolatilityParams(int emaLength, int rocLength) : IGpuIndicatorParams
{
	/// <summary>
	/// EMA window length applied to candle ranges.
	/// </summary>
	public int EmaLength = emaLength;

	/// <summary>
	/// ROC window length applied to EMA values.
	/// </summary>
	public int RocLength = rocLength;

	/// <inheritdoc />
	public readonly void FromIndicator(IIndicator indicator)
	{
		if (indicator is ChaikinVolatility chaikin)
		{
			Unsafe.AsRef(in this).EmaLength = chaikin.Ema.Length;
			Unsafe.AsRef(in this).RocLength = chaikin.Roc.Length;
		}
	}
}

/// <summary>
/// GPU calculator for Chaikin Volatility indicator.
/// </summary>
public class GpuChaikinVolatilityCalculator : GpuIndicatorCalculatorBase<ChaikinVolatility, GpuChaikinVolatilityParams, GpuIndicatorResult>
{
	private readonly Action<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChaikinVolatilityParams>> _kernel;

	/// <summary>
	/// Initializes a new instance of the <see cref=\"GpuChaikinVolatilityCalculator\"/> class.
	/// </summary>
	/// <param name=\"context\">ILGPU context.</param>
	/// <param name=\"accelerator\">ILGPU accelerator.</param>
	public GpuChaikinVolatilityCalculator(Context context, Accelerator accelerator)
	: base(context, accelerator)
	{
		_kernel = Accelerator.LoadAutoGroupedStreamKernel
			<Index2D, ArrayView<GpuCandle>, ArrayView<GpuIndicatorResult>, ArrayView<float>, ArrayView<int>, ArrayView<int>, ArrayView<GpuChaikinVolatilityParams>>(ChaikinVolatilityParamsSeriesKernel);
	}

	/// <inheritdoc />
	public override GpuIndicatorResult[][][] Calculate(GpuCandle[][] candlesSeries, GpuChaikinVolatilityParams[] parameters)
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
		using var emaBuffer = Accelerator.Allocate1D<float>(totalSize * parameters.Length);
		using var outputBuffer = Accelerator.Allocate1D<GpuIndicatorResult>(totalSize * parameters.Length);

		var extent = new Index2D(parameters.Length, seriesCount);
		_kernel(extent, inputBuffer.View, outputBuffer.View, emaBuffer.View, offsetsBuffer.View, lengthsBuffer.View, paramsBuffer.View);
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
	/// ILGPU kernel: Chaikin Volatility computation for multiple series and parameter sets.
	/// </summary>
	private static void ChaikinVolatilityParamsSeriesKernel(
		Index2D index,
		ArrayView<GpuCandle> flatCandles,
		ArrayView<GpuIndicatorResult> flatResults,
		ArrayView<float> emaBuffer,
		ArrayView<int> offsets,
		ArrayView<int> lengths,
		ArrayView<GpuChaikinVolatilityParams> parameters)
	{
		var paramIdx = index.X;
		var seriesIdx = index.Y;

		var offset = offsets[seriesIdx];
		var len = lengths[seriesIdx];
		if (len <= 0)
			return;

		var emaLength = parameters[paramIdx].EmaLength;
		if (emaLength <= 0)
			emaLength = 1;

		var rocLength = parameters[paramIdx].RocLength;
		if (rocLength <= 0)
			rocLength = 1;

		var multiplier = 2f / (emaLength + 1f);
		var emaSum = 0f;
		var emaCount = 0;
		var emaPrev = 0f;
		var emaFormedCount = 0;
		var totalSize = flatCandles.Length;

		for (var i = 0; i < len; i++)
		{
			var globalIdx = offset + i;
			var resIndex = paramIdx * totalSize + globalIdx;
			var candle = flatCandles[globalIdx];
			var range = candle.High - candle.Low;

			float emaValue;
			if (emaCount < emaLength)
			{
				emaSum += range;
				emaCount++;
				emaValue = emaSum / emaLength;
				if (emaCount == emaLength)
					emaPrev = emaValue;
			}
			else
			{
				emaValue = (range - emaPrev) * multiplier + emaPrev;
				emaPrev = emaValue;
			}

			emaBuffer[resIndex] = emaValue;

			var result = new GpuIndicatorResult
			{
				Time = candle.Time,
				Value = float.NaN,
				IsFormed = 0
			};

			if (emaCount >= emaLength)
			{
				emaFormedCount++;
				var historyDepth = emaFormedCount > rocLength ? rocLength : emaFormedCount - 1;
				var prevIdx = globalIdx - historyDepth;
				var prevResIndex = paramIdx * totalSize + prevIdx;
				var prevEma = emaBuffer[prevResIndex];

				if (prevEma != 0f)
					result.Value = (emaValue - prevEma) / prevEma * 100f;

				result.IsFormed = (byte)(emaFormedCount > rocLength ? 1 : 0);
			}

			flatResults[resIndex] = result;
		}
	}
}
